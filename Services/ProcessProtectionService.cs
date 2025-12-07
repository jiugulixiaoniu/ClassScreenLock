using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using CCLS.Models;

namespace CCLS.Services;

/// <summary>
/// 进程保护服务 - 防止应用程序被意外终止
/// </summary>
public class ProcessProtectionService : IDisposable
{
    // 定时器，用于定期检查进程状态
    private readonly System.Timers.Timer _checkTimer;
    
    // 当前进程ID
    private readonly int _currentProcessId;
    
    // 配置信息
    private ApplicationConfig _config;
    
    // 是否已释放资源
    private bool _disposed = false;
    
    // Windows API函数导入
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessShutdownParameters(int level, int flags);
    
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);
    
    // 禁用任务管理器的常量
    private const int SPI_SETSCREENSAVERRUNNING = 97;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">应用程序配置</param>
    public ProcessProtectionService(ApplicationConfig config)
    {
        _config = config;
        _currentProcessId = Process.GetCurrentProcess().Id;
        
        // 初始化定时器，每15秒检查一次
        _checkTimer = new System.Timers.Timer(15000);
        _checkTimer.Elapsed += OnCheckTimerElapsed;
        
        // 设置进程保护
        EnableProcessProtection();
    }
    
    /// <summary>
    /// 启动进程保护
    /// </summary>
    public void Start()
    {
        if (_config.EnableProcessProtection)
        {
            _checkTimer.Start();
        }
    }
    
    /// <summary>
    /// 停止进程保护
    /// </summary>
    public void Stop()
    {
        _checkTimer.Stop();
        DisableProcessProtection();
    }
    
    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="config">新的配置信息</param>
    public void UpdateConfig(ApplicationConfig config)
    {
        _config = config;
        
        // 重新应用进程保护设置
        if (_config.EnableProcessProtection)
        {
            EnableProcessProtection();
        }
        else
        {
            DisableProcessProtection();
        }
    }
    
    /// <summary>
    /// 启用进程保护
    /// </summary>
    private void EnableProcessProtection()
    {
        try
        {
            // 设置进程关闭级别，提高进程优先级
            SetProcessShutdownParameters(0x100, 0);
            
            // 禁用任务管理器（仅在锁定状态下）
            if (_config.EnableProcessProtection)
            {
                SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 1, IntPtr.Zero, 0);
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常
            System.Diagnostics.Debug.WriteLine($"启用进程保护时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 禁用进程保护
    /// </summary>
    private void DisableProcessProtection()
    {
        try
        {
            // 恢复任务管理器
            SystemParametersInfo(SPI_SETSCREENSAVERRUNNING, 0, IntPtr.Zero, 0);
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常
            System.Diagnostics.Debug.WriteLine($"禁用进程保护时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 检查定时器事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // 检查当前进程是否仍在运行
            var currentProcess = Process.GetProcessById(_currentProcessId);
            if (currentProcess != null && !currentProcess.HasExited)
            {
                // 进程仍在运行，重新应用保护设置
                EnableProcessProtection();
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常
            System.Diagnostics.Debug.WriteLine($"检查进程状态时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _checkTimer?.Stop();
            _checkTimer?.Dispose();
            DisableProcessProtection();
            _disposed = true;
        }
    }
}