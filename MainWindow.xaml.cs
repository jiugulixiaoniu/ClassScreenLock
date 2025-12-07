using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Timers;
using System.Media;
using CCLS.Models;
using CCLS.Services;
using CCLS.Utilities;
using CCLS.Enums;
using System.IO;
using Microsoft.Win32;

namespace CCLS;

/// <summary>
/// 主窗口类 - 应用程序主界面
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    // 服务实例
    private ScheduleService? _scheduleService;
    private PasswordService? _passwordService;
    private ScreenLockService? _screenLockService;
    private ProcessProtectionService? _processProtectionService;
    
    // 配置和数据
    private ApplicationConfig? _config;
    private Schedule? _schedule;
    private List<PasswordInfo>? _passwords;
    
    // 定时器
    private DispatcherTimer? _uiUpdateTimer;
    private System.Timers.Timer? _timeCheckTimer;
    
    // 当前状态
    private ApplicationState _currentState = ApplicationState.Idle;
    private TimeType _currentTimeSlot = TimeType.BreakTime;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            // 检查并创建默认配置（必须在服务初始化前调用）
            if (!ConfigManager.HasInitialConfig())
            {
                CreateDefaultConfiguration();
            }
            
            // 初始化配置和数据
            InitializeData();
            
            // 初始化服务
            InitializeServices();
            
            // 初始化UI
            InitializeUI();
            
            // 初始化定时器
            InitializeTimers();
            
            // 订阅事件
            SubscribeToEvents();
            
            // 添加窗口状态改变事件处理
            this.StateChanged += MainWindow_StateChanged;
            
            // 记录日志
            LogMessage("系统启动完成");
            
            // 检查是否需要在启动时最小化到托盘
            if (_config?.MinimizeToTrayOnStartup == true)
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
                ShowBalloonTip("班级屏幕锁", "程序已在系统托盘运行");
                LogMessage("程序已最小化到系统托盘");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"主窗口初始化失败：{ex.Message}\n\n堆栈跟踪：\n{ex.StackTrace}", "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    /// <summary>
    /// 创建默认配置
    /// </summary>
    private void CreateDefaultConfiguration()
    {
        try
        {
            LogMessage("创建默认配置...");
            
            // 创建默认配置
            _config = new ApplicationConfig();
            ConfigManager.SaveConfig(_config);
            
            // 创建默认课表
            ConfigManager.SaveSchedule(_schedule);
            
            // 创建默认密码
            _passwords = CreateDefaultPasswords();
            ConfigManager.SavePasswords(_passwords);
            
            LogMessage("默认配置创建完成");
        }
        catch (Exception ex)
        {
            LogMessage($"创建默认配置失败: {ex.Message}");
            MessageBox.Show($"创建默认配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 初始化配置和数据
    /// </summary>
    private void InitializeData()
    {
        // 加载配置
        _config = ConfigManager.LoadConfig();
        App.CurrentConfig = _config;
        
        // 加载课表
        _schedule = ConfigManager.LoadSchedule();
        
        // 加载密码
        _passwords = ConfigManager.LoadPasswords();
    }

    /// <summary>
    /// 创建默认密码
    /// </summary>
    /// <returns>默认密码列表</returns>
    private List<PasswordInfo> CreateDefaultPasswords()
    {
        var passwordRequest = new PasswordGenerationRequest
        {
            Length = 15
        };
        
        var password = _passwordService!.GeneratePassword(passwordRequest);
        var cleanPassword = password.Replace(" ", "");
        var encryptedPassword = _passwordService.EncryptPassword(cleanPassword);
        
        return new List<PasswordInfo>
        {
            new PasswordInfo
            {
                PasswordId = 1,
                OwnerName = "管理员",
                Role = "管理员",
                EncryptedPassword = encryptedPassword,
                CreatedTime = DateTime.Now,
                UsageCount = 0
            }
        };
    }
    
    /// <summary>
    /// 初始化服务
    /// </summary>
    private void InitializeServices()
    {
        // 确保配置已加载
        if (_config == null)
        {
            LogMessage("配置未加载，重新加载配置...");
            _config = ConfigManager.LoadConfig();
        }
        
        // 创建服务实例（使用已加载的配置）
        _scheduleService = new ScheduleService(_config);
        _passwordService = new PasswordService();
        _screenLockService = new ScreenLockService(_config, _scheduleService, _passwordService);
        _processProtectionService = new ProcessProtectionService(_config);
        
        // 订阅屏幕锁定服务的状态变化事件
        _screenLockService.StateChanged += OnStateChanged;
        
        // 确保课表已加载
        if (_schedule == null || _schedule.Classes.Count == 0)
        {
            LogMessage("课表为空，尝试重新加载...");
            _schedule = ConfigManager.LoadSchedule();
        }
        
        // 设置课表
        _scheduleService.SetSchedule(_schedule);
        
        // 确保密码已加载
        if (_passwords == null || _passwords.Count == 0)
        {
            LogMessage("密码列表为空，尝试重新加载...");
            _passwords = ConfigManager.LoadPasswords();
            
        }
        
        // 设置密码列表
        _screenLockService.SetPasswords(_passwords);
        
        // 启动服务
        _scheduleService.Start();
        Console.WriteLine("[MainWindow] ScheduleService已启动");
        
        // 立即执行一次状态检查
        _scheduleService.ManualCheck();
        Console.WriteLine("[MainWindow] 已执行手动状态检查");
        
        _processProtectionService.Start();
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置窗口标题
        Title = $"班级屏幕锁管理系统 v{GetApplicationVersion()}";
        
        // 更新状态显示
        UpdateStatusDisplay();
        
        // 更新配置显示
        UpdateConfigDisplay();
    }
    
    /// <summary>
    /// 初始化定时器
    /// </summary>
    private void InitializeTimers()
    {
        // UI更新定时器
        _uiUpdateTimer = new DispatcherTimer();
        _uiUpdateTimer.Interval = TimeSpan.FromSeconds(1);
        _uiUpdateTimer.Tick += OnUiUpdateTimerTick;
        _uiUpdateTimer.Start();
        Console.WriteLine("[MainWindow] UI更新定时器已启动 (1秒间隔)");
        
        // 时间检查定时器
        _timeCheckTimer = new System.Timers.Timer(30000); // 每30秒检查一次
        _timeCheckTimer.Elapsed += OnTimeCheckTimerElapsed;
        _timeCheckTimer.Start();
        Console.WriteLine("[MainWindow] 时间检查定时器已启动 (30秒间隔)");
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 订阅时间调度服务事件
        if (_scheduleService != null)
        {
            _scheduleService.BreakTimeStarted += OnBreakTimeStarted;
            _scheduleService.ClassTimeStarted += OnClassTimeStarted;
            _scheduleService.AutoUnlockTriggered += OnAutoUnlockTriggered;
            Console.WriteLine("[MainWindow] 已订阅ScheduleService事件");
        }
        else
        {
            Console.WriteLine("[MainWindow] ScheduleService为null，无法订阅事件");
        }
        
        // 订阅屏幕锁定服务事件
        if (_screenLockService != null)
        {
            _screenLockService.ScreenLocked += OnScreenLocked;
            _screenLockService.ScreenUnlocked += OnScreenUnlocked;
            _screenLockService.StateChanged += OnStateChanged;
            Console.WriteLine("[MainWindow] 已订阅ScreenLockService事件");
        }
        else
        {
            Console.WriteLine("[MainWindow] ScreenLockService为null，无法订阅事件");
        }
    }
    
    /// <summary>
    /// 获取应用程序版本
    /// </summary>
    /// <returns>版本号</returns>
    private string GetApplicationVersion()
    {
        return "1.0.0";
    }
    
    /// <summary>
    /// 记录日志消息
    /// </summary>
    /// <param name="message">日志消息</param>
    private void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        // 保存日志到文件
        App.LogMessage(message);
        
        // 在UI线程更新日志显示
        Dispatcher.Invoke(() =>
        {
            LogText.Text = logEntry + "\n" + LogText.Text;
            
            // 限制日志长度
            var lines = LogText.Text.Split('\n');
            if (lines.Length > 100)
            {
                LogText.Text = string.Join("\n", lines.Take(100));
            }
            
            // 滚动到顶部
            LogScrollViewer.ScrollToTop();
        });
    }
    
    /// <summary>
    /// 更新状态显示
    /// </summary>
    private void UpdateStatusDisplay()
    {
        // 更新当前状态
        StatusText.Text = _currentState switch
        {
            ApplicationState.Idle => "空闲",
            ApplicationState.BreakTime => "课间休息",
            ApplicationState.Locked => "屏幕已锁定",
            ApplicationState.Configuring => "配置中",
            ApplicationState.ClassTime => "上课时间",
            _ => "未知状态"
        };
        
        // 更新当前时间
        var now = DateTime.Now;
        CurrentTimeText.Text = now.ToString("HH:mm:ss");
        CurrentDateText.Text = now.ToString("yyyy年M月d日 dddd");
        TimeStatusBarText.Text = now.ToString("HH:mm:ss");
        
        // 从ScheduleService获取最新的时间状态
        _currentTimeSlot = _scheduleService?.CurrentTimeType ?? TimeType.BreakTime;
        TimeSlotText.Text = _currentTimeSlot switch
        {
            TimeType.ClassTime => "上课时间",
            TimeType.BreakTime => "课间休息",
            _ => "未知时段"
        };
        
    }
    
    /// <summary>
    /// 更新配置显示
    /// </summary>
    private void UpdateConfigDisplay()
    {// 更新配置显示
        if (_config != null)
        {
            AutoLockCheckBox.IsChecked = _config.EnableAutoLock;
            ProcessProtectionCheckBox.IsChecked = _config.EnableProcessProtection;
            TaskManagerBlockCheckBox.IsChecked = _config.EnableProcessProtection;
        }
    }
    
    /// <summary>
    /// UI更新定时器事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnUiUpdateTimerTick(object? sender, EventArgs e)
    {
        // 使用Dispatcher.Invoke确保UI更新在主线程执行
        Dispatcher.Invoke(() =>
        {
            UpdateStatusDisplay();
        });
    }
    
    /// <summary>
    /// 时间检查定时器事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnTimeCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 定期检查并更新状态
        Dispatcher.Invoke(UpdateStatusDisplay);
    }
    
    /// <summary>
    /// 课间时间开始事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnBreakTimeStarted(object? sender, EventArgs e)
    {
        Console.WriteLine("[MainWindow] 接收到课间时间开始事件");
        LogMessage("课间休息时间开始");
        StatusBarText.Text = "课间休息时间";
    }
    
    /// <summary>
    /// 上课时间开始事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClassTimeStarted(object? sender, EventArgs e)
    {
        Console.WriteLine("[MainWindow] 接收到上课时间开始事件");
        LogMessage("上课时间开始");
        StatusBarText.Text = "上课时间";
    }
    
    /// <summary>
    /// 自动解锁触发事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnAutoUnlockTriggered(object? sender, EventArgs e)
    {
        LogMessage("自动解锁触发");
        StatusBarText.Text = "自动解锁";
    }
    
    /// <summary>
    /// 屏幕锁定事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnScreenLocked(object? sender, EventArgs e)
    {
        LogMessage("屏幕已锁定");
        StatusBarText.Text = "屏幕已锁定";
    }
    
    /// <summary>
    /// 屏幕解锁事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="args">解锁事件参数</param>
    private void OnScreenUnlocked(object? sender, ScreenLockService.ScreenUnlockEventArgs args)
    {
        var methodText = args.UnlockMethod switch
        {
            UnlockMethod.Auto => "自动解锁",
            UnlockMethod.AdminPassword => "管理员密码解锁",
            UnlockMethod.Emergency => "紧急解锁",
            _ => "未知方式"
        };
        
        // 记录使用的密钥所有者（如果是密码解锁）
        if (args.UnlockMethod == UnlockMethod.AdminPassword && args.MatchedPassword != null)
        {
            var ownerName = args.MatchedPassword.OwnerName;
            var role = args.MatchedPassword.Role;
            LogMessage($"屏幕已解锁 ({methodText})，使用的密钥所有者: {ownerName} ({role})");
            StatusBarText.Text = $"屏幕已解锁 ({methodText})，使用的密钥所有者: {ownerName} ({role})";
        }
        else
        {
            LogMessage($"屏幕已解锁 ({methodText})");
            StatusBarText.Text = $"屏幕已解锁 ({methodText})";
        }
    }
    
    /// <summary>
    /// 状态改变事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="newState">新状态</param>
    private void OnStateChanged(object? sender, ApplicationState newState)
    {
        _currentState = newState;
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// 显示锁定按钮按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ShowLockButtonBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _screenLockService?.ShowLockButton();
            LogMessage("手动显示锁定按钮");
            StatusBarText.Text = "已显示锁定按钮";
        }
        catch (Exception ex)
        {
            LogMessage($"显示锁定按钮失败: {ex.Message}");
            MessageBox.Show($"显示锁定按钮失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 隐藏锁定按钮按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void HideLockButtonBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _screenLockService?.HideLockButton();
            LogMessage("手动隐藏锁定按钮");
            StatusBarText.Text = "已隐藏锁定按钮";
        }
        catch (Exception ex)
        {
            LogMessage($"隐藏锁定按钮失败: {ex.Message}");
            MessageBox.Show($"隐藏锁定按钮失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 锁定屏幕按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void LockScreenBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _screenLockService?.LockScreen();
            LogMessage("手动锁定屏幕");
            StatusBarText.Text = "已锁定屏幕";
        }
        catch (Exception ex)
        {
            LogMessage($"锁定屏幕失败: {ex.Message}");
            MessageBox.Show($"锁定屏幕失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 解锁屏幕按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void UnlockScreenBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _screenLockService?.UnlockScreen(UnlockMethod.Emergency);
            LogMessage("手动解锁屏幕");
            StatusBarText.Text = "已解锁屏幕";
        }
        catch (Exception ex)
        {
            LogMessage($"解锁屏幕失败: {ex.Message}");
            MessageBox.Show($"解锁屏幕失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 生成密码按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void GeneratePasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 直接打开密码管理窗口，功能已整合到密码管理窗口中
            ShowPasswordManager();
        }
        catch (Exception ex)
        {
            LogMessage($"打开密码管理失败: {ex.Message}");
            MessageBox.Show($"打开密码管理失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 验证密码按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void VerifyPasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowPasswordVerificationWindow();
    }
    
    /// <summary>
    /// 显示密码验证窗口
    /// </summary>
    private void ShowPasswordVerificationWindow()
    {
        var window = new Window
        {
            Title = "密码验证",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // 标题
        var title = new TextBlock
        {
            Text = "输入密码进行验证",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(10, 10, 10, 10)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);
        
        // 提示信息
        var hintText = new TextBlock
        {
            Text = "密码格式：15位大写字母和数字，格式为XXXXX XXXXX XXXXX（每5位自动添加空格）",
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(10, 0, 10, 5)
        };
        Grid.SetRow(hintText, 1);
        grid.Children.Add(hintText);
        
        // 密码输入区域容器
        var passwordContainer = new StackPanel
        {
            Margin = new Thickness(10, 10, 10, 10)
        };
        
        var passwordBox = new TextBox
        {
            FontSize = 14,
            MaxLength = 17 // 15位字符 + 2个空格的最大长度
        };
        
        // 密码格式提示和计数
        var passwordHint = new TextBlock
        {
            Text = "已输入 0/15 位字符",
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 5, 0, 0)
        };
        
        // 密码输入处理 - 自动格式化（每5位添加空格）
        passwordBox.LostFocus += (s, e) =>
        {
            var cleanText = passwordBox.Text.Replace(" ", "").ToUpper();
            if (cleanText.Length > 0 && cleanText.Length <= 15)
            {
                var formattedText = new StringBuilder();
                for (int i = 0; i < cleanText.Length; i++)
                {
                    if (i > 0 && i % 5 == 0)
                    {
                        formattedText.Append(' ');
                    }
                    formattedText.Append(cleanText[i]);
                }
                passwordBox.Text = formattedText.ToString();
            }
        };
        
        // 实时更新提示信息
        passwordBox.TextChanged += (s, e) =>
        {
            // 计算当前有效字符数（不包括空格）
            var cleanText = passwordBox.Text.Replace(" ", "");
            
            // 更新提示文本（显示实际字符数，不包括空格）
            passwordHint.Text = $"已输入 {cleanText.Length}/15 位字符";
            
            // 根据输入长度改变颜色
            if (cleanText.Length == 15)
            {
                passwordHint.Foreground = Brushes.Green;
            }
            else if (cleanText.Length > 10)
            {
                passwordHint.Foreground = Brushes.DarkOrange;
            }
            else
            {
                passwordHint.Foreground = Brushes.Gray;
            }
        };
        
        // 密码输入事件处理
        passwordBox.TextChanged += (s, e) =>
        {
            // 获取当前文本和光标位置
            var currentText = passwordBox.Text;
            var cursorPos = passwordBox.SelectionStart;
            
            // 移除所有空格，然后转换为大写
            var cleanText = currentText.Replace(" ", "").ToUpper();
            
            // 每5个字符添加一个空格
            var formattedText = new StringBuilder();
            for (int i = 0; i < cleanText.Length; i++)
            {
                if (i > 0 && i % 5 == 0)
                {
                    formattedText.Append(' ');
                }
                formattedText.Append(cleanText[i]);
            }
            
            // 更新文本框内容
            var newText = formattedText.ToString();
            if (passwordBox.Text != newText)
            {
                passwordBox.Text = newText;
                // 调整光标位置，考虑添加的空格
                var spacesBefore = (int)Math.Floor((double)cursorPos / 5);
                passwordBox.SelectionStart = Math.Min(newText.Length, cursorPos + spacesBefore);
            }
            
            // 更新提示文本（显示实际字符数，不包括空格）
            passwordHint.Text = $"已输入 {cleanText.Length}/15 位字符";
            
            // 根据输入长度改变颜色
            if (cleanText.Length == 15)
            {
                passwordHint.Foreground = Brushes.Green;
            }
            else if (cleanText.Length > 10)
            {
                passwordHint.Foreground = Brushes.DarkOrange;
            }
            else
            {
                passwordHint.Foreground = Brushes.Gray;
            }
        };
        
        passwordContainer.Children.Add(passwordBox);
        passwordContainer.Children.Add(passwordHint);
        Grid.SetRow(passwordContainer, 2);
        grid.Children.Add(passwordContainer);
        
        // 结果显示
        var resultText = new TextBlock
        {
            Margin = new Thickness(10, 10, 10, 10),
            FontSize = 14
        };
        Grid.SetRow(resultText, 3);
        grid.Children.Add(resultText);
        
        // 按钮面板
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10, 10, 10, 10)
        };
        
        var verifyButton = new Button
        {
            Content = "验证",
            Margin = new Thickness(0, 0, 10, 0),
            Padding = new Thickness(10, 5, 10, 5)
        };
        verifyButton.Click += (s, e) => 
        {
            var password = passwordBox.Text.Trim().ToUpper(); // 转换为大写以匹配存储的密码
            var result = _passwordService?.VerifyPassword(password, _passwords ?? new List<PasswordInfo>());
            
            if (result != null && result.IsValid)
            {
                resultText.Text = "密码验证成功！";
                resultText.Foreground = Brushes.Green;
                LogMessage($"密码验证成功，使用者: {result.MatchedPassword?.OwnerName}");
                ConfigManager.SavePasswords(_passwords ?? new List<PasswordInfo>());
            }
            else
            {
                resultText.Text = $"验证失败: {result?.ErrorMessage}";
                resultText.Foreground = Brushes.Red;
                LogMessage($"密码验证失败: {result?.ErrorMessage}");
            }
        };
        
        var closeButton = new Button
        {
            Content = "关闭",
            Padding = new Thickness(10, 5, 10, 5)
        };
        closeButton.Click += (s, e) => window.Close();
        
        buttonPanel.Children.Add(verifyButton);
        buttonPanel.Children.Add(closeButton);
        Grid.SetRow(buttonPanel, 4);
        grid.Children.Add(buttonPanel);
        
        window.Content = grid;
        window.ShowDialog();
    }

    /// <summary>
        /// 最小化到托盘按钮点击事件
        /// </summary>
        private void MinimizeToTrayBtn_Click(object sender, RoutedEventArgs e)
        {
            // 最小化窗口并隐藏到托盘
            this.WindowState = WindowState.Minimized;
            this.Hide();
            
            // 显示托盘提示
            ShowBalloonTip("班级屏幕锁", "程序已最小化到系统托盘，双击托盘图标可重新打开窗口。");
        }
        
        /// <summary>
        /// 密码管理按钮点击事件
        /// </summary>
    private void PasswordManagerBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowPasswordManager();
    }
    
    /// <summary>
    /// 显示密码管理窗口
    /// </summary>
    private void ShowPasswordManager()
    {
        var window = new Window
        {
            Title = "密码管理",
            Width = 800,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Style = (Style)FindResource("MetroWindowStyle")
        };
        
        // 添加资源字典
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("Styles/MetroStyles.xaml", UriKind.Relative) });
        window.Resources = resources;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // 标题
        var title = new TextBlock
        {
            Text = "密码管理",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(10, 10, 10, 10),
            Style = (Style)FindResource("MetroTextBlockBoldStyle")
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);
        
        // 操作按钮区域
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 0, 10, 10),
            Style = (Style)FindResource("MetroStackPanelStyle")
        };
        
        var addButton = new Button
        {
            Content = "添加密码",
            Margin = new Thickness(0, 0, 10, 0),
            Width = 120,
            Height = 40,
            Style = (Style)FindResource("MetroButtonStyle")
        };
        
        var deleteButton = new Button
        {
            Content = "删除选中",
            Margin = new Thickness(0, 0, 10, 0),
            Width = 120,
            Height = 40,
            Style = (Style)FindResource("MetroDangerButtonStyle")
        };
        
        var refreshButton = new Button
        {
            Content = "刷新列表",
            Margin = new Thickness(0, 0, 10, 0),
            Width = 120,
            Height = 40,
            Style = (Style)FindResource("MetroButtonStyle")
        };
        
        // 密码列表
        var listView = new ListView
        {
            Margin = new Thickness(10, 10, 10, 10),
            Name = "passwordListView",
            Style = (Style)FindResource("MetroListViewStyle")
        };
        
        var view = new GridView();
        listView.View = view;
        
        view.Columns.Add(new GridViewColumn { Header = "ID", DisplayMemberBinding = new Binding("PasswordId"), Width = 50 });
        view.Columns.Add(new GridViewColumn { Header = "所有者", DisplayMemberBinding = new Binding("OwnerName"), Width = 100 });
        view.Columns.Add(new GridViewColumn { Header = "角色", DisplayMemberBinding = new Binding("Role"), Width = 80 });
        view.Columns.Add(new GridViewColumn { Header = "密码", DisplayMemberBinding = new Binding("DecryptedPassword"), Width = 150 });
        view.Columns.Add(new GridViewColumn { Header = "创建时间", DisplayMemberBinding = new Binding("CreatedTime"), Width = 120 });
        view.Columns.Add(new GridViewColumn { Header = "使用次数", DisplayMemberBinding = new Binding("UsageCount"), Width = 80 });
        
        // 初始化列表
        RefreshPasswordList(listView);
        
        // 按钮事件处理
        addButton.Click += (s, e) =>
        {
            ShowAddPasswordWindow();
            RefreshPasswordList(listView);
        };
        
        deleteButton.Click += (s, e) =>
        {
            if (listView.SelectedItem == null)
            {
                MessageBox.Show("请先选择要删除的密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 防误触确认对话框
            var result = MessageBox.Show("确定要删除选中的密码吗？此操作不可恢复！", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var selectedItem = listView.SelectedItem;
                    var passwordIdProperty = selectedItem.GetType().GetProperty("PasswordId");
                    if (passwordIdProperty != null)
                    {
                        var passwordIdValue = passwordIdProperty.GetValue(selectedItem);
                        if (passwordIdValue != null && int.TryParse(passwordIdValue.ToString(), out int passwordId))
                        {
                            var passwordToDelete = _passwords?.FirstOrDefault(p => p.PasswordId == passwordId);
                            
                            if (passwordToDelete != null)
                            {
                                _passwords?.Remove(passwordToDelete);
                                ConfigManager.SavePasswords(_passwords ?? new List<PasswordInfo>());
                                RefreshPasswordList(listView);
                                MessageBox.Show("密码删除成功", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("无法获取密码ID", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除密码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        };
        
        refreshButton.Click += (s, e) => RefreshPasswordList(listView);
        
        buttonPanel.Children.Add(addButton);
        buttonPanel.Children.Add(deleteButton);
        buttonPanel.Children.Add(refreshButton);
        
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        Grid.SetRow(listView, 3);
        grid.Children.Add(listView);
        
        // 关闭按钮
        var closeButton = new Button
        {
            Content = "关闭",
            Margin = new Thickness(10, 10, 10, 10),
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            Style = (Style)FindResource("MetroButtonStyle")
        };
        closeButton.Click += (s, e) => window.Close();
        Grid.SetRow(closeButton, 4);
        grid.Children.Add(closeButton);
        
        window.Content = grid;
        window.ShowDialog();
    }
    
    /// <summary>
    /// 显示添加密码窗口
    /// </summary>
    private void ShowAddPasswordWindow()
    {
        var window = new Window
        {
            Title = "添加密码",
            Width = 500,
            Height = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Style = (Style)FindResource("MetroWindowStyle")
        };
        
        // 添加资源字典
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("Styles/MetroStyles.xaml", UriKind.Relative) });
        window.Resources = resources;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // 标题
        var title = new TextBlock
        {
            Text = "添加新密码",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(10, 10, 10, 10),
            Style = (Style)FindResource("MetroTextBlockBoldStyle")
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);
        
        // 所有者输入
        var ownerLabel = new TextBlock
        {
            Text = "所有者:",
            Margin = new Thickness(10, 5, 5, 5),
            Style = (Style)FindResource("MetroTextBlockStyle")
        };
        Grid.SetRow(ownerLabel, 1);
        grid.Children.Add(ownerLabel);
        
        var ownerTextBox = new TextBox
        {
            Margin = new Thickness(10, 5, 10, 5),
            Style = (Style)FindResource("MetroTextBoxStyle")
        };
        Grid.SetRow(ownerTextBox, 1);
        Grid.SetColumn(ownerTextBox, 1);
        grid.Children.Add(ownerTextBox);
        
        // 角色输入
        var roleLabel = new TextBlock
        {
            Text = "角色:",
            Margin = new Thickness(10, 5, 5, 5),
            Style = (Style)FindResource("MetroTextBlockStyle")
        };
        Grid.SetRow(roleLabel, 2);
        grid.Children.Add(roleLabel);
        
        var roleTextBox = new TextBox
        {
            Margin = new Thickness(10, 5, 10, 5),
            Style = (Style)FindResource("MetroTextBoxStyle")
        };
        Grid.SetRow(roleTextBox, 2);
        Grid.SetColumn(roleTextBox, 1);
        grid.Children.Add(roleTextBox);
        
        // 密码输入区域
        var passwordLabel = new TextBlock
        {
            Text = "密码:",
            Margin = new Thickness(10, 5, 5, 5),
            Style = (Style)FindResource("MetroTextBlockStyle")
        };
        Grid.SetRow(passwordLabel, 3);
        grid.Children.Add(passwordLabel);
        
        // 密码生成和输入区域
        var passwordContainer = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(10, 5, 10, 10)
        };
        
        // 生成密码和显示区域
        var generateRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var generateButton = new Button
        {
            Content = "生成随机密码",
            Style = (Style)FindResource("MetroButtonStyle"),
            MinWidth = 120
        };
        
        var passwordTextBox = new TextBox
        {
            Margin = new Thickness(10, 0, 10, 0),
            Style = (Style)FindResource("MetroTextBoxStyle"),
            FontSize = 14,
            MinWidth = 200,
            MaxLength = 17 // 15位字符 + 2个空格
        };
        
        var copyButton = new Button
        {
            Content = "复制密码",
            Style = (Style)FindResource("MetroButtonStyle"),
            MinWidth = 100
        };
        
        generateRow.Children.Add(generateButton);
        generateRow.Children.Add(passwordTextBox);
        generateRow.Children.Add(copyButton);
        
        // 密码格式提示
        var passwordHint = new TextBlock
        {
            Text = "已输入 0/15 位字符",
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 5, 0, 0),
            Style = (Style)FindResource("MetroTextBlockStyle")
        };
        
        passwordContainer.Children.Add(generateRow);
        passwordContainer.Children.Add(passwordHint);
        
        Grid.SetRow(passwordContainer, 4);
        Grid.SetColumnSpan(passwordContainer, 2);
        grid.Children.Add(passwordContainer);
        
        // 密码输入事件处理
        passwordTextBox.PreviewTextInput += (s, e) =>
        {
            // 只允许输入大写字母和数字
            if (!char.IsUpper(e.Text[0]) && !char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }
            
            // 计算当前有效字符数（不包括空格）
            var currentCleanText = passwordTextBox.Text.Replace(" ", "");
            
            // 限制最多15位有效字符
            if (currentCleanText.Length >= 15)
            {
                e.Handled = true;
                return;
            }
        };
        
        // 失去焦点时格式化
        passwordTextBox.LostFocus += (s, e) =>
        {
            // 移除所有空格，然后转换为大写
            var cleanText = passwordTextBox.Text.Replace(" ", "").ToUpper();
            
            // 限制最多15位有效字符
            if (cleanText.Length > 15)
            {
                cleanText = cleanText.Substring(0, 15);
            }
            
            // 每5个字符添加一个空格
            var formattedText = new StringBuilder();
            for (int i = 0; i < cleanText.Length; i++)
            {
                if (i > 0 && i % 5 == 0)
                {
                    formattedText.Append(' ');
                }
                formattedText.Append(cleanText[i]);
            }
            
            // 更新文本
            passwordTextBox.Text = formattedText.ToString();
        };
        
        // 实时更新提示信息
        passwordTextBox.TextChanged += (s, e) =>
        {
            // 计算当前有效字符数（不包括空格）
            var cleanText = passwordTextBox.Text.Replace(" ", "");
            
            // 更新提示文本（显示实际字符数，不包括空格）
            passwordHint.Text = $"已输入 {cleanText.Length}/15 位字符";
            
            // 根据输入长度改变颜色
            if (cleanText.Length == 15)
            {
                passwordHint.Foreground = Brushes.Green;
            }
            else if (cleanText.Length > 10)
            {
                passwordHint.Foreground = Brushes.DarkOrange;
            }
            else
            {
                passwordHint.Foreground = Brushes.Gray;
            }
        };
        
        generateButton.Click += (s, e) =>
        {
            try
            {
                var passwordRequest = new PasswordGenerationRequest { Length = 15 };
                var password = _passwordService?.GeneratePassword(passwordRequest) ?? string.Empty;
                passwordTextBox.Text = password;
                
                // 自动复制到剪贴板
                Clipboard.SetText(password);
                
                MessageBox.Show("密码已生成并复制到剪贴板", "密码生成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成密码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        
        copyButton.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(passwordTextBox.Text))
            {
                Clipboard.SetText(passwordTextBox.Text);
                MessageBox.Show("密码已复制到剪贴板", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };
        
        // 按钮面板
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10, 10, 10, 10),
            Style = (Style)FindResource("MetroStackPanelStyle")
        };
        
        var saveButton = new Button
        {
            Content = "保存",
            Margin = new Thickness(0, 0, 10, 0),
            Style = (Style)FindResource("MetroButtonStyle")
        };
        
        var cancelButton = new Button
        {
            Content = "取消",
            Margin = new Thickness(0, 0, 0, 0),
            Style = (Style)FindResource("MetroButtonStyle")
        };
        
        saveButton.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(ownerTextBox.Text) || string.IsNullOrEmpty(roleTextBox.Text) || string.IsNullOrEmpty(passwordTextBox.Text))
            {
                MessageBox.Show("请填写所有字段", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                var passwordText = passwordTextBox.Text;
                var cleanPassword = passwordText.Replace(" ", "");
                var encryptedPassword = _passwordService?.EncryptPassword(cleanPassword) ?? string.Empty;
                
                var passwordInfo = new PasswordInfo
                {
                    PasswordId = (_passwords?.Count ?? 0) + 1,
                    OwnerName = ownerTextBox.Text,
                    Role = roleTextBox.Text,
                    EncryptedPassword = encryptedPassword,
                    CreatedTime = DateTime.Now,
                    UsageCount = 0
                };
                
                _passwords?.Add(passwordInfo);
                ConfigManager.SavePasswords(_passwords ?? new List<PasswordInfo>());
                
                MessageBox.Show("密码添加成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加密码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        
        cancelButton.Click += (s, e) => window.Close();
        
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        
        Grid.SetRow(buttonPanel, 6);
        grid.Children.Add(buttonPanel);
        
        // 设置Grid列定义
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        window.Content = grid;
        window.ShowDialog();
    }
    
    /// <summary>
    /// 刷新密码列表
    /// </summary>
    private void RefreshPasswordList(ListView listView)
    {
        // 解密密码显示
        var passwords = _passwords ?? new List<PasswordInfo>();
        var decryptedPasswords = passwords.Select(p => new
        {
            p.PasswordId,
            p.OwnerName,
            p.Role,
            DecryptedPassword = _passwordService?.DecryptPassword(p.EncryptedPassword) ?? string.Empty,
            p.CreatedTime,
            p.UsageCount
        }).ToList();
        
        listView.ItemsSource = decryptedPasswords;
    }

    /// <summary>
    /// 生成新密码
    /// </summary>
    private void GenerateNewPassword()
    {
        try
        {
            var passwordRequest = new PasswordGenerationRequest
            {
                Length = 15
            };
            
            var password = _passwordService?.GeneratePassword(passwordRequest) ?? string.Empty;
            var cleanPassword = password.Replace(" ", "");
            var encryptedPassword = _passwordService?.EncryptPassword(cleanPassword) ?? string.Empty;
            
            var passwordInfo = new PasswordInfo
            {
                PasswordId = (_passwords?.Count ?? 0) + 1,
                OwnerName = "管理员",
                Role = "教师",
                EncryptedPassword = encryptedPassword,
                CreatedTime = DateTime.Now
            };
            
            _passwords?.Add(passwordInfo);
            ConfigManager.SavePasswords(_passwords ?? new List<PasswordInfo>());
            
            LogMessage($"生成新密码: {password}");
            MessageBox.Show($"新密码: {password}", "密码生成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"生成密码失败: {ex.Message}");
            MessageBox.Show($"生成密码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
        /// 编辑课表按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void EditScheduleBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("打开课表编辑窗口");
                var scheduleEditWindow = new ScheduleEditWindow();
                scheduleEditWindow.Owner = this;
                scheduleEditWindow.ShowDialog();
                
                // 重新加载课表以应用更改
                _scheduleService?.SetSchedule(ConfigManager.LoadSchedule());
                
                // 重新加载密码
                _passwords = ConfigManager.LoadPasswords();
                _screenLockService?.SetPasswords(_passwords);
                
                LogMessage("课表编辑完成");
            }
            catch (Exception ex)
            {
                LogMessage($"打开课表编辑窗口失败: {ex.Message}");
                MessageBox.Show($"打开课表编辑窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    
    /// <summary>
    /// 系统设置按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void SystemSettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        LogMessage("打开系统设置窗口");
        
        try
        {
            var settingsWindow = new SystemSettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                LogMessage("系统设置已保存，重新加载配置...");
                
                // 重新加载配置
                _config = ConfigManager.LoadConfig();
                
                // 更新应用程序当前配置
                App.CurrentConfig = _config;
                
                // 更新动态资源（确保字体设置生效）
                App.UpdateDynamicResources();
                
                // 更新服务配置
                _scheduleService?.UpdateConfig(_config);
                _screenLockService?.UpdateConfig(_config);
                _processProtectionService?.UpdateConfig(_config);
                
                // 更新UI显示
                UpdateStatusDisplay();
                
                LogMessage("配置更新完成");
                MessageBox.Show("系统设置已保存并生效！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                LogMessage("系统设置已取消");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"打开系统设置窗口时发生错误: {ex.Message}");
            MessageBox.Show($"打开系统设置窗口时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 新建配置菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void NewConfigMenuItem_Click(object sender, RoutedEventArgs e)
    {
        LogMessage("新建配置功能");
        MessageBox.Show("新建配置功能", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    /// <summary>
    /// 导入配置菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ImportConfigMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "导入配置文件"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                // 这里可以添加导入配置的逻辑
                LogMessage($"导入配置文件: {openFileDialog.FileName}");
                MessageBox.Show("配置导入成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"导入配置失败: {ex.Message}");
            MessageBox.Show($"导入配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 导出配置菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ExportConfigMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "导出配置文件",
                FileName = $"CCLS_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                // 这里可以添加导出配置的逻辑
                LogMessage($"导出配置文件: {saveFileDialog.FileName}");
                MessageBox.Show("配置导出成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"导出配置失败: {ex.Message}");
            MessageBox.Show($"导出配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 退出菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    /// <summary>
    /// 生成密码菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void GeneratePasswordMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // 功能已整合到密码管理窗口中
        ShowPasswordManager();
    }
    
    /// <summary>
    /// 验证密码菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void VerifyPasswordMenuItem_Click(object sender, RoutedEventArgs e)
    {
        VerifyPasswordBtn_Click(sender, e);
    }
    
    /// <summary>
    /// 编辑课表菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void EditScheduleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        EditScheduleBtn_Click(sender, e);
    }
    
    /// <summary>
    /// 系统设置菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void SystemSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SystemSettingsBtn_Click(sender, e);
    }
    
    /// <summary>
    /// 关于菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show($"班级屏幕锁管理系统 v{GetApplicationVersion()}\n\n© 2024 班级屏幕锁系统\n\n本系统用于管理班级电脑屏幕锁定，确保课间休息时间学生不使用电脑。", 
            "关于", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    /// <summary>
    /// 日志查询菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void LogViewerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LogMessage("打开日志查询窗口...");
            var logViewerWindow = new LogViewerWindow();
            logViewerWindow.Owner = this;
            logViewerWindow.ShowDialog();
            LogMessage("日志查询窗口已关闭");
        }
        catch (Exception ex)
        {
            LogMessage($"打开日志查询窗口失败: {ex.Message}");
            System.Windows.MessageBox.Show($"打开日志查询窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 清空日志菜单项点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ClearLogsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = System.Windows.MessageBox.Show("确定要清空所有日志文件吗？\n此操作不可恢复！", "确认清空日志", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var logDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "C", "Logs");
                if (Directory.Exists(logDirectory))
                {
                    var logFiles = Directory.GetFiles(logDirectory, "*.log");
                    foreach (var file in logFiles)
                    {
                        File.Delete(file);
                    }
                    LogMessage("所有日志文件已清空");
                    System.Windows.MessageBox.Show("日志文件清空成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LogMessage("日志目录不存在，无需清空");
                    System.Windows.MessageBox.Show("日志目录不存在，无需清空", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                LogMessage("用户取消清空日志操作");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"清空日志失败: {ex.Message}");
            System.Windows.MessageBox.Show($"清空日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnClosing(CancelEventArgs e)
    {
        try
        {
            // 根据配置决定是退出程序还是最小化到托盘
            if (_config?.MinimizeToTrayOnClose == true)
            {
                // 取消关闭事件
                e.Cancel = true;
                
                // 最小化到托盘
                WindowState = WindowState.Minimized;
                Hide();
                ShowBalloonTip("班级屏幕锁管理系统", "程序已最小化到系统托盘，双击托盘图标可恢复窗口。");
                LogMessage("窗口关闭事件被拦截，程序已最小化到系统托盘");
            }
            else
            {
                // 停止定时器
                _uiUpdateTimer?.Stop();
                _timeCheckTimer?.Stop();
                
                // 释放服务资源
                _scheduleService?.Dispose();
                _screenLockService?.Dispose();
                _processProtectionService?.Dispose();
                
                LogMessage("系统关闭");
                
                base.OnClosing(e);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"窗口关闭事件处理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 窗口状态改变事件处理程序
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        try
        {
            // 最小化按钮不再自动隐藏到托盘，而是正常最小化
            // 用户可以通过点击X按钮来最小化到托盘（根据配置）
            LogMessage($"窗口状态改变: {WindowState}");
        }
        catch (Exception ex)
        {
            LogMessage($"窗口状态改变处理失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 显示托盘气泡提示
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="text">内容</param>
    private void ShowBalloonTip(string title, string text)
    {
        try
        {
            if (App.NotifyIcon != null)
            {
                App.NotifyIcon.ShowBalloonTip(3000, title, text, System.Windows.Forms.ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"显示托盘气泡提示失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 属性更改事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// 触发属性更改事件
    /// </summary>
    /// <param name="propertyName">属性名</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}