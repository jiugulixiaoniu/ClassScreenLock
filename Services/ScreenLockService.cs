using System.Timers;
using Timer = System.Timers.Timer;
using CCLS.Enums;
using CCLS.Models;
using CCLS.Forms;

namespace CCLS.Services;

/// <summary>
/// 屏幕锁定服务 - 负责管理锁定按钮和锁定窗口
/// </summary>
public class ScreenLockService : IDisposable
{
    // 当前应用程序状态
    private ApplicationState _currentState = ApplicationState.Idle;
    
    // 配置信息
    private ApplicationConfig _config;
    
    // 时间调度服务
    private readonly ScheduleService _scheduleService;
    
    // 密码服务
    private readonly PasswordService _passwordService;
    
    // 锁定按钮
    private LockButton? _lockButton;
    
    // 锁定窗口
    private LockWindow? _lockWindow;
    
    // 锁定窗口定时器（用于定期检查窗口状态）
    private Timer? _lockWindowTimer;
    
    // 存储的密码列表
    private List<PasswordInfo> _passwords = new List<PasswordInfo>();
    
    // 标志位：是否已经在当前课间休息时间锁定过屏幕
    private bool _hasLockedThisBreakTime = false;
    
    /// <summary>
    /// 应用程序状态变化事件
    /// </summary>
    public event EventHandler<ApplicationState>? StateChanged;
    
    /// <summary>
    /// 屏幕锁定事件
    /// </summary>
    public event EventHandler? ScreenLocked;
    
    /// <summary>
    /// 屏幕解锁事件
    /// </summary>
    public event EventHandler<ScreenUnlockEventArgs>? ScreenUnlocked;
    
    /// <summary>
    /// 屏幕解锁事件参数
    /// </summary>
    public class ScreenUnlockEventArgs : EventArgs
    {
        /// <summary>
        /// 解锁方式
        /// </summary>
        public UnlockMethod UnlockMethod { get; set; }
        
        /// <summary>
        /// 匹配的密码信息（如果是密码解锁）
        /// </summary>
        public PasswordInfo? MatchedPassword { get; set; }
    }
    
    /// <summary>
    /// 当前应用程序状态
    /// </summary>
    public ApplicationState CurrentState
    {
        get { return _currentState; }
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                StateChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">应用程序配置</param>
    /// <param name="scheduleService">时间调度服务</param>
    /// <param name="passwordService">密码服务</param>
    public ScreenLockService(ApplicationConfig config, ScheduleService scheduleService, PasswordService passwordService)
    {
        _config = config;
        _scheduleService = scheduleService;
        _passwordService = passwordService;
        
        // 订阅时间调度服务的事件
        _scheduleService.BreakTimeStarted += OnBreakTimeStarted;
        _scheduleService.ClassTimeStarted += OnClassTimeStarted;
        _scheduleService.AutoUnlockTriggered += OnAutoUnlockTriggered;
        
        // 初始化锁定窗口定时器
        _lockWindowTimer = new Timer(1000); // 1秒
        _lockWindowTimer.Elapsed += OnLockWindowTimerElapsed;
        _lockWindowTimer.AutoReset = true;
    }
    
    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="config">新的配置信息</param>
    public void UpdateConfig(ApplicationConfig config)
    {
        _config = config;
        
        // 重新创建锁定窗口以应用新配置
        if (_lockWindow != null)
        {
            _lockWindow.Hide();
            _lockWindow.UnlockRequested -= OnUnlockRequested;
            _lockWindow = null;
        }
        
        // 重新创建锁定按钮以应用新配置
        if (_lockButton != null)
        {
            _lockButton.Hide();
            _lockButton.LockClicked -= OnLockButtonClicked;
            _lockButton = null;
        }
        
        // 如果当前处于课间时间，重新显示锁定按钮
        if (_scheduleService.CurrentTimeType == TimeType.BreakTime && _config.EnableAutoLock)
        {
            ShowLockButton();
        }
    }
    
    /// <summary>
    /// 设置密码列表
    /// </summary>
    /// <param name="passwords">密码列表</param>
    public void SetPasswords(List<PasswordInfo> passwords)
    {
        _passwords = passwords;
    }
    
    /// <summary>
    /// 显示锁定按钮
    /// </summary>
    public void ShowLockButton()
    {
        Console.WriteLine("[ScreenLockService] ShowLockButton 方法调用");
        
        if (_lockButton == null)
        {
            Console.WriteLine("[ScreenLockService] 创建新的锁定按钮");
            _lockButton = new LockButton(_config);
            _lockButton.LockClicked += OnLockButtonClicked;
        }
        else
        {
            Console.WriteLine("[ScreenLockService] 使用现有锁定按钮");
        }
        
        Console.WriteLine($"[ScreenLockService] 显示锁定按钮，位置: ({_config.LockButtonX}, {_config.LockButtonY})");
        _lockButton.Show();
        _lockButton.Activate();
        
        CurrentState = ApplicationState.BreakTime;
    }
    
    /// <summary>
    /// 隐藏锁定按钮
    /// </summary>
    public void HideLockButton()
    {
        if (_lockButton != null)
        {
            _lockButton.Hide();
        }
    }
    
    /// <summary>
    /// 锁定屏幕
    /// </summary>
    public void LockScreen()
    {
        // 隐藏锁定按钮
        HideLockButton();
        
        // 设置标志位，当前课间休息时间不再显示锁定按钮
        _hasLockedThisBreakTime = true;
        
        // 显示锁定窗口
        if (_lockWindow == null)
        {
            _lockWindow = new LockWindow(_config, _scheduleService);
            _lockWindow.UnlockRequested += OnUnlockRequested;
        }
        
        _lockWindow.Show();
        _lockWindow.Activate();
        
        // 启动锁定窗口定时器
        _lockWindowTimer?.Start();
        
        CurrentState = ApplicationState.Locked;
        ScreenLocked?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 解锁屏幕
    /// </summary>
    /// <param name="method">解锁方式</param>
    /// <param name="matchedPassword">匹配的密码信息（如果是密码解锁）</param>
    public void UnlockScreen(UnlockMethod method, PasswordInfo? matchedPassword = null)
    {
        // 隐藏锁定窗口
        if (_lockWindow != null)
        {
            _lockWindow.Hide();
        }
        
        // 停止锁定窗口定时器
        _lockWindowTimer?.Stop();
        
        CurrentState = ApplicationState.Idle;
        ScreenUnlocked?.Invoke(this, new ScreenUnlockEventArgs
        {
            UnlockMethod = method,
            MatchedPassword = matchedPassword
        });
    }
    
    /// <summary>
    /// 验证解锁密码
    /// </summary>
    /// <param name="password">待验证的密码</param>
    /// <returns>验证结果</returns>
    public PasswordVerificationResult VerifyUnlockPassword(string password)
    {
        return _passwordService.VerifyPassword(password, _passwords);
    }
    
    /// <summary>
    /// 课间时间开始事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnBreakTimeStarted(object? sender, EventArgs e)
    {
        Console.WriteLine($"[ScreenLockService] 课间时间开始事件触发，自动锁定启用: {_config.EnableAutoLock}");
        
        // 始终设置状态为课间休息
        CurrentState = ApplicationState.BreakTime;
        
        // 每次课间休息开始时，重置锁定标志位
        // 这样每次课间休息都会显示锁定按钮（如果启用了自动锁定）
        _hasLockedThisBreakTime = false;
        
        // 如果启用自动锁定，则显示锁定按钮
        if (_config.EnableAutoLock && !_hasLockedThisBreakTime)
        {
            Console.WriteLine("[ScreenLockService] 条件满足，显示锁定按钮");
            ShowLockButton();
        }
        else
        {
            Console.WriteLine($"[ScreenLockService] 不显示锁定按钮，原因: 自动锁定={_config.EnableAutoLock}, 已锁定={_hasLockedThisBreakTime}");
        }
    }
    
    /// <summary>
    /// 上课时间开始事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClassTimeStarted(object? sender, EventArgs e)
    {
        HideLockButton();
        
        // 重置标志位，下一次课间休息可以再次显示锁定按钮
        _hasLockedThisBreakTime = false;
        
        // 如果当前处于锁定状态，自动解锁
        if (CurrentState == ApplicationState.Locked)
        {
            UnlockScreen(UnlockMethod.Auto);
        }
        
        // 始终设置状态为上课时间
        CurrentState = ApplicationState.ClassTime;
    }
    
    /// <summary>
    /// 自动解锁事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnAutoUnlockTriggered(object? sender, EventArgs e)
    {
        if (CurrentState == ApplicationState.Locked)
        {
            UnlockScreen(UnlockMethod.Auto);
        }
    }
    
    /// <summary>
    /// 锁定按钮点击事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnLockButtonClicked(object? sender, EventArgs e)
    {
        LockScreen();
    }
    
    /// <summary>
    /// 解锁请求事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="password">输入的密码</param>
    private void OnUnlockRequested(object? sender, string password)
    {
        var result = VerifyUnlockPassword(password);
        if (result.IsValid)
        {
            // 保存更新后的密码列表（包含使用次数和最后使用时间）
            CCLS.Utilities.ConfigManager.SavePasswords(_passwords);
            
            UnlockScreen(UnlockMethod.AdminPassword, result.MatchedPassword);
        }
        else
        {
            // 通知锁定窗口密码错误
            _lockWindow?.ShowErrorMessage(result.ErrorMessage);
        }
    }
    
    /// <summary>
    /// 锁定窗口定时器事件处理（用于定期检查窗口状态）
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnLockWindowTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 确保锁定窗口始终在最前面
        if (_lockWindow != null && _lockWindow.IsVisible)
        {
            // 使用Dispatcher确保UI操作在主线程执行
            _lockWindow.Dispatcher.Invoke(() =>
            {
                if (_lockWindow != null && _lockWindow.IsVisible)
                {
                    _lockWindow.Activate();
                }
            });
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 停止定时器
        _lockWindowTimer?.Stop();
        _lockWindowTimer?.Dispose();
        
        // 关闭窗口
        _lockButton?.Close();
        _lockWindow?.Close();
        
        // 取消事件订阅
        if (_scheduleService != null)
        {
            _scheduleService.BreakTimeStarted -= OnBreakTimeStarted;
            _scheduleService.ClassTimeStarted -= OnClassTimeStarted;
            _scheduleService.AutoUnlockTriggered -= OnAutoUnlockTriggered;
        }
    }
}