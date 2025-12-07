using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CCLS.Models;
using CCLS.Utilities;

namespace CCLS;

/// <summary>
/// 系统设置窗口
/// </summary>
public partial class SystemSettingsWindow : Window
{
    private ApplicationConfig _config = null!;
    private LockWindowConfig _lockWindowConfig = null!;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public SystemSettingsWindow()
    {
        InitializeComponent();
        
        // 初始化配置
        InitializeConfig();
        
        // 绑定事件
        BindEvents();
    }
    
    /// <summary>
    /// 初始化配置
    /// </summary>
    private void InitializeConfig()
    {
        // 加载应用程序配置
        _config = ConfigManager.LoadConfig();
        
        // 加载锁定窗口配置
        // 使用ApplicationConfig中的LockWindowConfig属性
        _lockWindowConfig = _config.LockWindowConfig;
        
        // 初始化字体选择下拉框
        InitializeFontComboBox();
        
        // 设置UI控件值
        LoadConfigToUI();
    }
    
    /// <summary>
    /// 初始化字体选择下拉框
    /// </summary>
    private void InitializeFontComboBox()
    {
        // 获取系统中所有可用的字体并按名称排序
        var fontFamilies = System.Windows.Media.Fonts.SystemFontFamilies.OrderBy(f => f.Source);
        
        // 直接添加FontFamily对象而不是Source字符串，这样可以更好地支持中文字体
        foreach (var fontFamily in fontFamilies)
        {
            FontFamilyComboBox.Items.Add(fontFamily);
        }
        
        // 设置默认选中项
        FontFamilyComboBox.SelectedIndex = 0;
    }
    
    /// <summary>
    /// 绑定事件
    /// </summary>
    private void BindEvents()
    {
        // 滑块事件
        LockWindowOpacitySlider.ValueChanged += LockWindowOpacitySlider_ValueChanged;
        BreakTimeNotificationDurationSlider.ValueChanged += BreakTimeNotificationDurationSlider_ValueChanged;
    }
    
    /// <summary>
    /// 将配置加载到UI控件
    /// </summary>
    private void LoadConfigToUI()
    {
        // 基本设置
        EnableAutoLockCheckBox.IsChecked = _config.EnableAutoLock;
        LockWindowOpacitySlider.Value = _config.LockWindowOpacity;
        LockWindowOpacityText.Text = _config.LockWindowOpacity.ToString("0.00");
        LockButtonXTextBox.Text = _config.LockButtonX.ToString();
        LockButtonYTextBox.Text = _config.LockButtonY.ToString();
        RunAtStartupCheckBox.IsChecked = _config.RunAtStartup;
        EnableProcessProtectionCheckBox.IsChecked = _config.EnableProcessProtection;
        EnableLoggingCheckBox.IsChecked = _config.EnableLogging;
        MinimizeToTrayOnCloseCheckBox.IsChecked = _config.MinimizeToTrayOnClose;
        MinimizeToTrayOnStartupCheckBox.IsChecked = _config.MinimizeToTrayOnStartup;
        
        // 锁定窗口设置
        LockWindowTitleTextBox.Text = _lockWindowConfig.Title;
        LockWindowMessageTextBox.Text = _lockWindowConfig.Message;
        ShowCountdownCheckBox.IsChecked = _lockWindowConfig.ShowCountdown;
        BackgroundColorTextBox.Text = _lockWindowConfig.BackgroundColor;
        TextColorTextBox.Text = _lockWindowConfig.TextColor;
        CountdownColorTextBox.Text = _lockWindowConfig.CountdownColor;
        
        // 通知设置
        ShowNotificationsCheckBox.IsChecked = _config.ShowNotifications;
        BreakTimeNotificationDurationSlider.Value = _config.BreakTimeNotificationDuration;
        BreakTimeNotificationDurationText.Text = _config.BreakTimeNotificationDuration.ToString();
        
        // 字体设置
        // 查找并设置字体
        foreach (FontFamily fontFamily in FontFamilyComboBox.Items)
        {
            if (fontFamily.Source.Equals(_config.FontFamily, StringComparison.OrdinalIgnoreCase))
            {
                FontFamilyComboBox.SelectedItem = fontFamily;
                break;
            }
        }
        FontSizeTextBox.Text = _config.FontSize.ToString();
    }
    
    /// <summary>
    /// 将UI控件的值保存到配置对象
    /// </summary>
    private void SaveUIToConfig()
    {
        // 基本设置
        _config.EnableAutoLock = EnableAutoLockCheckBox.IsChecked ?? false;
        _config.LockWindowOpacity = LockWindowOpacitySlider.Value;
        
        // 解析坐标值
        if (int.TryParse(LockButtonXTextBox.Text, out int x))
        {
            _config.LockButtonX = x;
        }
        
        if (int.TryParse(LockButtonYTextBox.Text, out int y))
        {
            _config.LockButtonY = y;
        }
        
        _config.RunAtStartup = RunAtStartupCheckBox.IsChecked ?? false;
        _config.EnableProcessProtection = EnableProcessProtectionCheckBox.IsChecked ?? false;
        _config.EnableLogging = EnableLoggingCheckBox.IsChecked ?? false;
        _config.MinimizeToTrayOnClose = MinimizeToTrayOnCloseCheckBox.IsChecked ?? true;
        _config.MinimizeToTrayOnStartup = MinimizeToTrayOnStartupCheckBox.IsChecked ?? false;
        
        // 锁定窗口设置
        _lockWindowConfig.Title = LockWindowTitleTextBox.Text;
        _lockWindowConfig.Message = LockWindowMessageTextBox.Text;
        _lockWindowConfig.ShowCountdown = ShowCountdownCheckBox.IsChecked ?? false;
        _lockWindowConfig.BackgroundColor = BackgroundColorTextBox.Text;
        _lockWindowConfig.TextColor = TextColorTextBox.Text;
        _lockWindowConfig.CountdownColor = CountdownColorTextBox.Text;
        
        // 通知设置
        _config.ShowNotifications = ShowNotificationsCheckBox.IsChecked ?? false;
        _config.BreakTimeNotificationDuration = (int)BreakTimeNotificationDurationSlider.Value;
        
        // 字体设置
        if (FontFamilyComboBox.SelectedItem is FontFamily selectedFontFamily)
        {
            _config.FontFamily = selectedFontFamily.Source;
        }
        
        if (int.TryParse(FontSizeTextBox.Text, out int fontSize))
        {
            _config.FontSize = fontSize;
        }
    }
    
    /// <summary>
    /// 保存按钮点击事件
    /// </summary>
    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 将UI控件的值保存到配置对象
            SaveUIToConfig();
            
            // 保存配置到文件
            if (ConfigManager.SaveConfig(_config))
            {
                MessageBox.Show("配置保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("配置保存失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
    
    /// <summary>
    /// 重置按钮点击事件
    /// </summary>
    private void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("确定要重置所有设置吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            // 创建默认配置
            _config = new ApplicationConfig();
            _lockWindowConfig = _config.LockWindowConfig;
            
            // 重新加载配置到UI
            LoadConfigToUI();
        }
    }
    
    /// <summary>
    /// 锁定窗口透明度滑块值变化事件
    /// </summary>
    private void LockWindowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        LockWindowOpacityText.Text = e.NewValue.ToString("0.00");
    }
    
    /// <summary>
    /// 课间提示时长滑块值变化事件
    /// </summary>
    private void BreakTimeNotificationDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        BreakTimeNotificationDurationText.Text = e.NewValue.ToString();
    }
    
    /// <summary>
    /// 检查颜色值是否有效
    /// </summary>
    /// <param name="colorText">颜色文本</param>
    /// <returns>是否有效</returns>
    private bool IsValidColor(string colorText)
    {
        try
        {
            ColorConverter.ConvertFromString(colorText);
            return true;
        }
        catch
        {
            return false;
        }
    }
}