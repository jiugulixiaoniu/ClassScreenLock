using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CCLS.Models;
using CCLS.Services;
using CCLS.Controls;

namespace CCLS.Forms;

/// <summary>
/// 锁定窗口类 - 显示锁定界面并处理密码输入
/// </summary>
public class LockWindow : Window
{
    // 配置信息
    private readonly ApplicationConfig _config;
    
    // 时间调度服务
    private readonly ScheduleService _scheduleService;
    
    // 倒计时显示文本
    private TextBlock? _countdownText;
    
    // 密码输入框
    private PasswordBox? _passwordBox;
    
    // 解锁按钮
    private Button? _unlockButton;
    
    // 错误信息文本
    private TextBlock? _errorText;
    
    // 虚拟键盘
    private VirtualKeyboard? _virtualKeyboard;
    
    // 显示虚拟键盘按钮
    private Button? _showKeyboardButton;
    
    // 倒计时定时器
    private DispatcherTimer _countdownTimer;
    
    /// <summary>
    /// 解锁请求事件
    /// </summary>
    public event EventHandler<string>? UnlockRequested;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">应用程序配置</param>
    /// <param name="scheduleService">时间调度服务</param>
    public LockWindow(ApplicationConfig config, ScheduleService scheduleService)
    {
        _config = config;
        _scheduleService = scheduleService;
        
        InitializeComponent();
        Loaded += OnLoaded;
        
        // 初始化倒计时定时器
        _countdownTimer = new DispatcherTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += OnCountdownTimerTick;
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponent()
    {
        // 设置窗口属性
        Title = "班级屏幕锁";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        // 将窗口背景改为"几乎透明但能接收事件" 
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // Alpha=1而非0
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        
        // 确保窗口能够获取焦点并捕获所有鼠标事件
        Focusable = true;
        AddHandler(MouseDownEvent, new MouseButtonEventHandler((sender, e) => e.Handled = true), true);
        AddHandler(MouseUpEvent, new MouseButtonEventHandler((sender, e) => e.Handled = true), true);
        AddHandler(MouseMoveEvent, new MouseEventHandler((sender, e) => e.Handled = true), true);
        AddHandler(MouseWheelEvent, new MouseWheelEventHandler((sender, e) => e.Handled = true), true);
        
        // 窗口覆盖整个屏幕
        WindowState = WindowState.Maximized;
        
        // 创建布局容器
        var grid = new Grid();
        // 同时修改Grid背景 
        grid.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)); // Alpha=1而非0
        grid.HorizontalAlignment = HorizontalAlignment.Center;
        grid.VerticalAlignment = VerticalAlignment.Center;
        
        // 添加鼠标事件处理，防止点击透明区域时穿透到其他窗口
        grid.MouseDown += (sender, e) => e.Handled = true;
        grid.MouseUp += (sender, e) => e.Handled = true;
        grid.MouseMove += (sender, e) => e.Handled = true;
        grid.MouseWheel += (sender, e) => e.Handled = true;
        
        // 创建边框容器
        var border = new Border();
        border.HorizontalAlignment = HorizontalAlignment.Center;
        border.VerticalAlignment = VerticalAlignment.Center;
        border.Margin = new Thickness(50);
        border.Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)); // Metro风格白色背景
        border.CornerRadius = new CornerRadius(8);
        border.Padding = new Thickness(30);
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Metro风格蓝色边框
        border.BorderThickness = new Thickness(2);
        
        // 创建垂直布局容器
        var stackPanel = new StackPanel();
        
        // 创建标题文本
        var titleText = new TextBlock
        {
            Text = _config.LockWindowConfig?.Title ?? "班级屏幕锁",
            FontSize = 36,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        stackPanel.Children.Add(titleText);
        
        // 创建提示信息文本 
        var messageText = new TextBlock 
        { 
            Text = _config.LockWindowConfig?.Message ?? "课间休息时间，请勿操作电脑。上课前3分钟自动解锁。", 
            FontSize = 18, 
            Foreground = Brushes.Black,  // ✅ 改为纯黑色 
            HorizontalAlignment = HorizontalAlignment.Center, 
            TextWrapping = TextWrapping.Wrap, 
            TextAlignment = TextAlignment.Center, 
            Margin = new Thickness(0, 0, 0, 30) 
        }; 
        stackPanel.Children.Add(messageText);
        
        // 创建倒计时文本 
        _countdownText = new TextBlock 
        { 
            Text = "距离解锁还有: --:--", 
            FontSize = 24, 
            FontWeight = FontWeights.Bold, 
            Foreground = Brushes.Black,  // ✅ 改为纯黑色 
            HorizontalAlignment = HorizontalAlignment.Center, 
            Margin = new Thickness(0, 0, 0, 30) 
        };
        stackPanel.Children.Add(_countdownText);
        
        // 创建密码输入标签
        var passwordLabel = new TextBlock
        {
            Text = "管理员密码解锁:",
            FontSize = 16,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        stackPanel.Children.Add(passwordLabel);
        
        // 创建密码输入框
        _passwordBox = new PasswordBox
        {
            Width = 300,
            Height = 40,
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _passwordBox.KeyDown += OnPasswordBoxKeyDown;
        stackPanel.Children.Add(_passwordBox);
        
        // 创建按钮面板
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        
        // 创建解锁按钮
        _unlockButton = new Button
        {
            Name = "unlockButton",
            Content = "解锁",
            Width = 150,
            Height = 40,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)), // Metro风格蓝色背景
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 10, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 90, 180)), // 深蓝色边框
            BorderThickness = new Thickness(1)
        };
        _unlockButton.Click += OnUnlockButtonClick;
        buttonPanel.Children.Add(_unlockButton);
        
        // 创建显示虚拟键盘按钮
        _showKeyboardButton = new Button
        {
            Content = "显示虚拟键盘",
            Width = 150,
            Height = 40,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)), // 灰色背景
            Foreground = Brushes.White,
            Margin = new Thickness(10, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)), // 深灰色边框
            BorderThickness = new Thickness(1)
        };
        _showKeyboardButton.Click += OnShowKeyboardButtonClick;
        buttonPanel.Children.Add(_showKeyboardButton);
        
        stackPanel.Children.Add(buttonPanel);
        
        // 创建虚拟键盘容器
        var keyboardContainer = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 20),
            Visibility = Visibility.Collapsed,
            Background = new SolidColorBrush(Color.FromArgb(240, 240, 240, 240)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5)
        };
        
        // 创建虚拟键盘
        _virtualKeyboard = new VirtualKeyboard();
        _virtualKeyboard.SetTargetPasswordBox(_passwordBox);
        
        keyboardContainer.Child = _virtualKeyboard;
        stackPanel.Children.Add(keyboardContainer);
        
        // 创建错误信息文本
        _errorText = new TextBlock
        {
            Text = "",
            FontSize = 14,
            Foreground = Brushes.Red,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        stackPanel.Children.Add(_errorText);
        
        // 将stackPanel添加到border中
        border.Child = stackPanel;
        
        // 将border添加到grid中
        grid.Children.Add(border);
        Content = grid;
        
        // 拦截系统快捷键
        PreviewKeyDown += OnPreviewKeyDown;
    }
    
    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 启动倒计时定时器
        _countdownTimer.Start();
        UpdateCountdown();
        
        // 自动获取密码框焦点
        _passwordBox?.Focus();
    }
    
    /// <summary>
    /// 更新倒计时显示
    /// </summary>
    private void UpdateCountdown()
    {
        var remainingSeconds = _scheduleService.GetRemainingTimeUntilAutoUnlock();
        if (_countdownText != null)
        {
            if (remainingSeconds > 0)
            {
                var remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                _countdownText.Text = $"距离解锁还有: {remainingTime.Minutes:00}:{remainingTime.Seconds:00}";
            }
            else
            {
                _countdownText.Text = "即将自动解锁...";
            }
        }
    }
    
    /// <summary>
    /// 倒计时定时器刻度事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnCountdownTimerTick(object? sender, EventArgs e)
    {
        UpdateCountdown();
    }
    
    /// <summary>
    /// 解锁按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnUnlockButtonClick(object sender, RoutedEventArgs e)
    {
        RequestUnlock();
    }
    
    /// <summary>
    /// 显示虚拟键盘按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnShowKeyboardButtonClick(object sender, RoutedEventArgs e)
    {
        if (_virtualKeyboard != null)
        {
            // 切换虚拟键盘的可见性
            var parentBorder = _virtualKeyboard.Parent as Border;
            if (parentBorder != null)
            {
                if (parentBorder.Visibility == Visibility.Visible)
                {
                    parentBorder.Visibility = Visibility.Collapsed;
                    if (_showKeyboardButton != null)
                    {
                        _showKeyboardButton.Content = "显示虚拟键盘";
                    }
                }
                else
                {
                    parentBorder.Visibility = Visibility.Visible;
                    if (_showKeyboardButton != null)
                    {
                        _showKeyboardButton.Content = "隐藏虚拟键盘";
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 密码框按键事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnPasswordBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RequestUnlock();
        }
    }
    
    /// <summary>
    /// 请求解锁
    /// </summary>
    private void RequestUnlock()
    {
        if (_passwordBox != null)
        {
            var password = _passwordBox.Password;
            if (!string.IsNullOrWhiteSpace(password))
            {
                UnlockRequested?.Invoke(this, password);
                _passwordBox.Clear();
            }
        }
    }
    
    /// <summary>
    /// 显示错误信息
    /// </summary>
    /// <param name="message">错误信息</param>
    public void ShowErrorMessage(string message)
    {
        if (_errorText != null)
        {
            _errorText.Text = message;
            _errorText.Visibility = Visibility.Visible;
            
            // 3秒后自动隐藏错误信息
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (sender, e) =>
            {
                if (_errorText != null)
                {
                    _errorText.Visibility = Visibility.Collapsed;
                }
                timer.Stop();
            };
            timer.Start();
        }
    }
    
    /// <summary>
    /// 预览按键事件（用于拦截系统快捷键）
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 拦截常见的系统快捷键
        switch (e.Key)
        {
            case Key.Escape:
            case Key.F4:
            case Key.Tab:
            case Key.LWin:
            case Key.RWin:
                e.Handled = true;
                break;
        }
        
        // 拦截Alt键组合
        if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            e.Handled = true;
        }
        
        // 拦截Ctrl+Alt组合键
        if ((e.KeyboardDevice.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// 重写WndProc方法拦截系统消息
    /// </summary>
    /// <param name="m">消息</param>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // 可以在这里添加更多的系统消息拦截代码
        // 例如使用HwndSource.AddHook来处理Windows消息
    }
    
    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    /// <param name="e">事件参数</param>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 停止倒计时定时器
        _countdownTimer.Stop();
        
        base.OnClosing(e);
    }
}