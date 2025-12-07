using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CCLS.Models;

namespace CCLS.Forms;

/// <summary>
/// 锁定按钮类 - 课间显示的锁定按钮
/// </summary>
public class LockButton : Window
{
    // 配置信息
    private readonly ApplicationConfig _config;
    
    /// <summary>
    /// 锁定按钮点击事件
    /// </summary>
    public event EventHandler? LockClicked;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">应用程序配置</param>
    public LockButton(ApplicationConfig config)
    {
        _config = config;
        InitializeComponent();
        Loaded += OnLoaded;
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponent()
    {
        // 设置窗口属性
        Title = "锁定屏幕";
        Width = 120;
        Height = 60;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(255, 220, 53, 69)); // 红色
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        
        // 创建按钮内容
        var buttonContent = new System.Windows.Controls.Button
        {
            Content = "双击锁定",
            Width = 110,
            Height = 50,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.Bold
        };
        // 使用双击事件代替单击事件，防止误触
        buttonContent.MouseDoubleClick += OnButtonDoubleClick;
        
        // 设置窗口内容
        Content = buttonContent;
        
        // 启用鼠标拖动
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }
    
    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 使用配置中的坐标定位按钮
        if (_config.LockButtonX > 0 && _config.LockButtonY > 0)
        {
            Left = _config.LockButtonX;
            Top = _config.LockButtonY;
        }
        else
        {
            // 如果没有配置坐标，使用默认位置（屏幕右下角）
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 20;
            Top = screen.Bottom - Height - 20;
        }
    }
    
    /// <summary>
    /// 按钮双击事件（防止误触）
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnButtonDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LockClicked?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 鼠标左键按下事件（用于拖动窗口）
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}