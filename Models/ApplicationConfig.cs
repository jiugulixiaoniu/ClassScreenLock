using System.ComponentModel;

namespace CCLS.Models;

/// <summary>
/// 应用程序配置模型
/// </summary>
public class ApplicationConfig : INotifyPropertyChanged
{
    private bool _enableAutoLock = true;
    private double _lockWindowOpacity = 0.95;
    private int _lockButtonX = 100;
    private int _lockButtonY = 100;
    private bool _runAtStartup = true;
    private bool _enableProcessProtection = true;
    private bool _enableLogging = true;
    private string _logLevel = "Info";
    private bool _showNotifications = true;
    private int _breakTimeNotificationDuration = 5;
    private string _fontFamily = "Segoe UI";
    private double _fontSize = 14;
    private bool _minimizeToTrayOnClose = true;
    private bool _minimizeToTrayOnStartup = false;
    private LockWindowConfig _lockWindowConfig = new LockWindowConfig();
    
    /// <summary>
    /// 是否启用自动锁定
    /// </summary>
    public bool EnableAutoLock 
    { 
        get => _enableAutoLock; 
        set 
        { 
            if (_enableAutoLock != value) 
            { 
                _enableAutoLock = value; 
                OnPropertyChanged(nameof(EnableAutoLock)); 
            } 
        } 
    }
    
    /// <summary>
    /// 锁定窗口透明度（0.0-1.0）
    /// </summary>
    public double LockWindowOpacity 
    { 
        get => _lockWindowOpacity; 
        set 
        { 
            if (_lockWindowOpacity != value) 
            { 
                _lockWindowOpacity = value; 
                OnPropertyChanged(nameof(LockWindowOpacity)); 
            } 
        } 
    }
    
    /// <summary>
    /// 锁定按钮位置（X坐标）
    /// </summary>
    public int LockButtonX 
    { 
        get => _lockButtonX; 
        set 
        { 
            if (_lockButtonX != value) 
            { 
                _lockButtonX = value; 
                OnPropertyChanged(nameof(LockButtonX)); 
            } 
        } 
    }
    
    /// <summary>
    /// 锁定按钮位置（Y坐标）
    /// </summary>
    public int LockButtonY 
    { 
        get => _lockButtonY; 
        set 
        { 
            if (_lockButtonY != value) 
            { 
                _lockButtonY = value; 
                OnPropertyChanged(nameof(LockButtonY)); 
            } 
        } 
    }
    
    /// <summary>
    /// 是否开机自启
    /// </summary>
    public bool RunAtStartup 
    { 
        get => _runAtStartup; 
        set 
        { 
            if (_runAtStartup != value) 
            { 
                _runAtStartup = value; 
                OnPropertyChanged(nameof(RunAtStartup)); 
            } 
        } 
    }
    
    /// <summary>
    /// 是否启用进程保护
    /// </summary>
    public bool EnableProcessProtection 
    { 
        get => _enableProcessProtection; 
        set 
        { 
            if (_enableProcessProtection != value) 
            { 
                _enableProcessProtection = value; 
                OnPropertyChanged(nameof(EnableProcessProtection)); 
            } 
        } 
    }
    
    /// <summary>
    /// 是否记录操作日志
    /// </summary>
    public bool EnableLogging 
    { 
        get => _enableLogging; 
        set 
        { 
            if (_enableLogging != value) 
            { 
                _enableLogging = value; 
                OnPropertyChanged(nameof(EnableLogging)); 
            } 
        } 
    }
    
    /// <summary>
    /// 日志级别
    /// </summary>
    public string LogLevel 
    { 
        get => _logLevel; 
        set 
        { 
            if (_logLevel != value) 
            { 
                _logLevel = value; 
                OnPropertyChanged(nameof(LogLevel)); 
            } 
        } 
    }
    
    /// <summary>
    /// 是否显示操作提示
    /// </summary>
    public bool ShowNotifications 
    { 
        get => _showNotifications; 
        set 
        { 
            if (_showNotifications != value) 
            { 
                _showNotifications = value; 
                OnPropertyChanged(nameof(ShowNotifications)); 
            } 
        } 
    }
    
    /// <summary>
    /// 课间提示显示时长（秒）
    /// </summary>
    public int BreakTimeNotificationDuration 
    { 
        get => _breakTimeNotificationDuration; 
        set 
        { 
            if (_breakTimeNotificationDuration != value) 
            { 
                _breakTimeNotificationDuration = value; 
                OnPropertyChanged(nameof(BreakTimeNotificationDuration)); 
            } 
        } 
    }
    
    /// <summary>
    /// 应用程序字体
    /// </summary>
    public string FontFamily 
    { 
        get => _fontFamily; 
        set 
        { 
            if (_fontFamily != value) 
            { 
                _fontFamily = value; 
                OnPropertyChanged(nameof(FontFamily)); 
            } 
        } 
    }
    
    /// <summary>
    /// 应用程序字体大小
    /// </summary>
    public double FontSize 
    { 
        get => _fontSize; 
        set 
        { 
            if (_fontSize != value) 
            { 
                _fontSize = value; 
                OnPropertyChanged(nameof(FontSize)); 
            } 
        } 
    }
    
    /// <summary>
    /// 点击关闭按钮时是否最小化到托盘
    /// </summary>
    public bool MinimizeToTrayOnClose 
    { 
        get => _minimizeToTrayOnClose; 
        set 
        { 
            if (_minimizeToTrayOnClose != value) 
            { 
                _minimizeToTrayOnClose = value; 
                OnPropertyChanged(nameof(MinimizeToTrayOnClose)); 
            } 
        } 
    }
    
    /// <summary>
    /// 启动时是否直接最小化到托盘
    /// </summary>
    public bool MinimizeToTrayOnStartup 
    { 
        get => _minimizeToTrayOnStartup; 
        set 
        { 
            if (_minimizeToTrayOnStartup != value) 
            { 
                _minimizeToTrayOnStartup = value; 
                OnPropertyChanged(nameof(MinimizeToTrayOnStartup)); 
            } 
        } 
    }
    
    /// <summary>
    /// 锁定窗口配置
    /// </summary>
    public LockWindowConfig LockWindowConfig 
    { 
        get => _lockWindowConfig; 
        set 
        { 
            if (_lockWindowConfig != value) 
            { 
                _lockWindowConfig = value; 
                OnPropertyChanged(nameof(LockWindowConfig)); 
            } 
        } 
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
}

/// <summary>
/// 锁定窗口配置模型
/// </summary>
public class LockWindowConfig
{
    /// <summary>
    /// 窗口标题
    /// </summary>
    public string Title { get; set; } = "班级屏幕锁";
    
    /// <summary>
    /// 显示的提示信息
    /// </summary>
    public string Message { get; set; } = "课间休息时间，请勿操作电脑。上课前3分钟自动解锁。";
    
    /// <summary>
    /// 是否显示倒计时
    /// </summary>
    public bool ShowCountdown { get; set; } = true;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    public string BackgroundColor { get; set; } = "#000000";
    
    /// <summary>
    /// 文字颜色
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";
    
    /// <summary>
    /// 倒计时颜色
    /// </summary>
    public string CountdownColor { get; set; } = "#FF0000";
}