using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Threading;
using CCLS.Models;
using CCLS.Services;
using CCLS.Utilities;
using System.ComponentModel;

namespace CCLS;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application, INotifyPropertyChanged
{
    private static ApplicationConfig? _currentConfig;
    
    /// <summary>
    /// 当前应用程序配置
    /// </summary>
    public static ApplicationConfig? CurrentConfig
    {
        get => _currentConfig;
        set
        {
            if (_currentConfig != null)
            {
                _currentConfig.PropertyChanged -= OnCurrentConfigPropertyChanged;
            }
            
            _currentConfig = value;
            
            if (_currentConfig != null)
            {
                _currentConfig.PropertyChanged += OnCurrentConfigPropertyChanged;
                UpdateDynamicResources();
            }
            
            // 触发属性更改事件
            var app = Current as App;
            app?.OnPropertyChanged(nameof(CurrentConfig));
        }
    }
    
    /// <summary>
    /// 当前配置属性变化事件处理程序
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private static void OnCurrentConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateDynamicResources();
    }
    
    /// <summary>
    /// 更新动态资源
    /// </summary>
    private static void UpdateDynamicResources()
    {
        if (CurrentConfig == null) return;
        
        var app = Current as App;
        if (app == null) return;
        
        // 更新字体资源
        app.Resources["CurrentConfigFontFamily"] = new FontFamily(CurrentConfig.FontFamily);
        app.Resources["CurrentConfigFontSize"] = CurrentConfig.FontSize;
    }
    
    /// <summary>
    /// 属性更改事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
     /// 触发属性更改事件
     /// </summary>
     /// <param name="propertyName">属性名称</param>
     protected virtual void OnPropertyChanged(string propertyName)
     {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
     }
     
     /// <summary>
    /// 应用程序启动事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // 设置全局异常处理
            SetupGlobalExceptionHandling();
            
            // 确保数据目录存在
            EnsureDataDirectoryExists();
            
            // 检查并创建默认配置
            CheckAndCreateDefaultConfig();
            
            // 加载配置并设置为当前配置
            CurrentConfig = ConfigManager.LoadConfig();
            
            // 启动主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用程序启动失败：{ex.Message}\n\n堆栈跟踪：\n{ex.StackTrace}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 设置全局异常处理
    /// </summary>
    private void SetupGlobalExceptionHandling()
    {
        // 处理UI线程异常
        this.DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // 处理非UI线程异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }
    
    /// <summary>
    /// UI线程异常处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            // 记录异常
            LogException(e.Exception);
            
            // 显示错误消息
            MessageBox.Show(
                $"应用程序发生未处理的异常：\n\n{e.Exception.Message}\n\n详细信息已记录到日志文件。",
                "系统错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            // 标记异常已处理，防止应用程序崩溃
            e.Handled = true;
        }
        catch (Exception ex)
        {
            // 如果异常处理也出错，记录到系统事件日志
            System.Diagnostics.EventLog.WriteEntry(
                "班级屏幕锁系统",
                $"严重错误：{ex.Message}\n\n原始异常：{e.Exception.Message}",
                System.Diagnostics.EventLogEntryType.Error);
        }
    }
    
    /// <summary>
    /// 非UI线程异常处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception exception)
            {
                // 记录异常
                LogException(exception);
                
                // 显示错误消息
                MessageBox.Show(
                    $"应用程序发生严重错误：\n\n{exception.Message}\n\n详细信息已记录到日志文件。",
                    "严重错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            // 如果异常处理也出错，记录到系统事件日志
            System.Diagnostics.EventLog.WriteEntry(
                "班级屏幕锁系统",
                $"严重错误：{ex.Message}",
                System.Diagnostics.EventLogEntryType.Error);
        }
        
        // 如果是严重异常，终止应用程序
        if (e.IsTerminating)
        {
            MessageBox.Show(
                "应用程序即将终止，请重启程序。",
                "系统终止",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);
        }
    }
    
    /// <summary>
    /// 记录异常到日志文件
    /// </summary>
    /// <param name="exception">异常对象</param>
    private void LogException(Exception exception)
    {
        try
        {
            // 使用应用程序当前目录作为日志目录（与exe同目录）
            var logDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs");
            
            // 确保日志目录存在
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            // 创建日志文件路径
            var logFile = Path.Combine(logDir, $"Error_{DateTime.Now:yyyyMMdd}.log");
            
            // 记录异常信息
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 异常信息:\n" +
                             $"类型: {exception.GetType().Name}\n" +
                             $"消息: {exception.Message}\n" +
                             $"源: {exception.Source}\n" +
                             $"堆栈跟踪: {exception.StackTrace}\n" +
                             $"----------------------------------------\n";
            
            File.AppendAllText(logFile, logMessage);
        }
        catch
        {
            // 如果日志记录失败，忽略异常，避免无限循环
        }
    }
    
    /// <summary>
    /// 记录日志消息到文件
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="level">日志级别（信息/警告/错误）</param>
    public static void LogMessage(string message, string level = "信息")
    {
        try
        {
            // 使用应用程序当前目录作为日志目录（与exe同目录）
            var logDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs");
            
            // 确保日志目录存在
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            // 创建日志文件路径（按日期）
            var logFile = Path.Combine(logDir, $"App_{DateTime.Now:yyyyMMdd}.log");
            
            // 记录日志信息
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}\n";
            
            File.AppendAllText(logFile, logMessage);
        }
        catch
        {
            // 如果日志记录失败，忽略异常，避免无限循环
        }
    }
    
    /// <summary>
    /// 确保数据目录存在
    /// </summary>
    private void EnsureDataDirectoryExists()
    {
        try
        {
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClassScreenLockCCLS",
                "Data");
            
            // 如果目录不存在，创建目录
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"创建数据目录失败：{ex.Message}",
                "初始化错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 检查并创建默认配置
    /// </summary>
    private void CheckAndCreateDefaultConfig()
    {
        try
        {
            // 检查是否有初始配置
            if (!ConfigManager.HasInitialConfig())
            {
                // 创建默认配置
                var defaultConfig = new ApplicationConfig();
                ConfigManager.SaveConfig(defaultConfig);
                
                // 创建默认课表
                var defaultSchedule = new Schedule();
                ConfigManager.SaveSchedule(defaultSchedule);
                
                // 创建默认密码
                var defaultPasswords = new System.Collections.Generic.List<PasswordInfo>
                {
                    new PasswordInfo
                    {
                        PasswordId = 1,
                        OwnerName = "管理员",
                        Role = "教师",
                        EncryptedPassword = new PasswordService().EncryptPassword(new PasswordService().GeneratePassword(new PasswordGenerationRequest()).Replace(" ", "")),
                        CreatedTime = DateTime.Now
                    }
                };
                ConfigManager.SavePasswords(defaultPasswords);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"创建默认配置失败：{ex.Message}",
                "初始化错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 应用程序退出事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void Application_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            // 清理资源
            // 这里可以添加其他清理逻辑
        }
        catch (Exception ex)
        {
            // 记录退出时的异常
            LogException(ex);
        }
    }
}