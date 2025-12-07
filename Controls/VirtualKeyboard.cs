using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CCLS.Controls;

/// <summary>
/// 虚拟键盘控件 - 用于在没有实体键盘时输入密码
/// </summary>
public class VirtualKeyboard : UserControl
{
    // 主网格布局
    private Grid _mainGrid = null!;
    
    // 密码输入框
    private PasswordBox? _targetPasswordBox;
    
    // 密码显示文本框
    private TextBlock? _passwordDisplay;
    
    // 当前输入的密码
    private string _currentPassword = string.Empty;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public VirtualKeyboard()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponent()
    {
        // 设置控件属性
        Width = 600;
        Height = 400;
        Background = new SolidColorBrush(Color.FromArgb(240, 240, 240, 240));
        
        // 创建主网格布局
        _mainGrid = new Grid();
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) }); // 密码显示行
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // 间隔
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 数字键盘行
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // 间隔
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 字母键盘行
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // 间隔
        _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 功能按钮行
        
        // 创建密码显示区域
        var passwordBorder = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(5)
        };
        
        _passwordDisplay = new TextBlock
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "请输入密码"
        };
        
        passwordBorder.Child = _passwordDisplay;
        _mainGrid.Children.Add(passwordBorder);
        Grid.SetRow(passwordBorder, 0);
        
        // 创建数字键盘
        CreateNumberKeyboard();
        
        // 创建字母键盘
        CreateLetterKeyboard();
        
        // 创建功能按钮
        CreateFunctionButtons();
        
        // 添加主网格到控件
        Content = _mainGrid;
    }
    
    /// <summary>
    /// 创建数字键盘
    /// </summary>
    private void CreateNumberKeyboard()
    {
        var numberPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        // 添加数字按钮 1-9
        for (int i = 1; i <= 9; i++)
        {
            int currentNumber = i; // 创建局部变量以捕获正确的值
            var button = CreateKeyButton(currentNumber.ToString());
            button.Click += (sender, e) => AddCharacterToPassword(currentNumber.ToString());
            numberPanel.Children.Add(button);
        }
        
        // 添加0按钮，宽度是其他按钮的两倍
        var zeroButton = CreateKeyButton("0");
        zeroButton.Width = 120;
        zeroButton.Click += (sender, e) => AddCharacterToPassword("0");
        numberPanel.Children.Add(zeroButton);
        
        _mainGrid.Children.Add(numberPanel);
        Grid.SetRow(numberPanel, 2);
    }
    
    /// <summary>
    /// 创建字母键盘
    /// </summary>
    private void CreateLetterKeyboard()
    {
        var letterPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        // 第一行字母 QWERTYUIOP
        var firstRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5)
        };
        
        foreach (char c in "QWERTYUIOP")
        {
            var button = CreateKeyButton(c.ToString());
            button.Click += (sender, e) => AddCharacterToPassword(c.ToString());
            firstRow.Children.Add(button);
        }
        
        // 第二行字母 ASDFGHJKL
        var secondRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 5)
        };
        
        foreach (char c in "ASDFGHJKL")
        {
            var button = CreateKeyButton(c.ToString());
            button.Click += (sender, e) => AddCharacterToPassword(c.ToString());
            secondRow.Children.Add(button);
        }
        
        // 第三行字母 ZXCVBNM
        var thirdRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        foreach (char c in "ZXCVBNM")
        {
            var button = CreateKeyButton(c.ToString());
            button.Click += (sender, e) => AddCharacterToPassword(c.ToString());
            thirdRow.Children.Add(button);
        }
        
        letterPanel.Children.Add(firstRow);
        letterPanel.Children.Add(secondRow);
        letterPanel.Children.Add(thirdRow);
        
        _mainGrid.Children.Add(letterPanel);
        Grid.SetRow(letterPanel, 4);
    }
    
    /// <summary>
    /// 创建功能按钮
    /// </summary>
    private void CreateFunctionButtons()
    {
        var functionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        // 退格按钮
        var backButton = CreateFunctionButton("退格");
        backButton.Background = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // 橙色
        backButton.Click += (sender, e) => RemoveLastCharacter();
        functionPanel.Children.Add(backButton);
        
        // 清空按钮
        var clearButton = CreateFunctionButton("清空");
        clearButton.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 红色
        clearButton.Click += (sender, e) => ClearPassword();
        functionPanel.Children.Add(clearButton);
        
        // 确认按钮
        var confirmButton = CreateFunctionButton("确认");
        confirmButton.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 绿色
        confirmButton.Click += (sender, e) => ConfirmPassword();
        functionPanel.Children.Add(confirmButton);
        
        _mainGrid.Children.Add(functionPanel);
        Grid.SetRow(functionPanel, 6);
    }
    
    /// <summary>
    /// 创建键盘按钮
    /// </summary>
    /// <param name="content">按钮内容</param>
    /// <returns>按钮控件</returns>
    private Button CreateKeyButton(string content)
    {
        var button = new Button
        {
            Content = content,
            Width = 50,
            Height = 50,
            Margin = new Thickness(2),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1)
        };
        
        // 禁用按钮音效
        button.ClickMode = ClickMode.Press;
        
        return button;
    }
    
    /// <summary>
    /// 创建功能按钮
    /// </summary>
    /// <param name="content">按钮内容</param>
    /// <returns>按钮控件</returns>
    private Button CreateFunctionButton(string content)
    {
        var button = new Button
        {
            Content = content,
            Width = 100,
            Height = 40,
            Margin = new Thickness(5),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1)
        };
        
        // 禁用按钮音效
        button.ClickMode = ClickMode.Press;
        
        return button;
    }
    
    /// <summary>
    /// 设置目标密码框
    /// </summary>
    /// <param name="passwordBox">目标密码框</param>
    public void SetTargetPasswordBox(PasswordBox passwordBox)
    {
        _targetPasswordBox = passwordBox;
    }
    
    /// <summary>
    /// 添加字符到密码
    /// </summary>
    /// <param name="character">要添加的字符</param>
    private void AddCharacterToPassword(string character)
    {
        _currentPassword += character;
        UpdatePasswordDisplay();
        UpdateTargetPasswordBox();
    }
    
    /// <summary>
    /// 移除最后一个字符
    /// </summary>
    private void RemoveLastCharacter()
    {
        if (_currentPassword.Length > 0)
        {
            _currentPassword = _currentPassword.Substring(0, _currentPassword.Length - 1);
            UpdatePasswordDisplay();
            UpdateTargetPasswordBox();
        }
    }
    
    /// <summary>
    /// 清空密码
    /// </summary>
    private void ClearPassword()
    {
        _currentPassword = string.Empty;
        UpdatePasswordDisplay();
        UpdateTargetPasswordBox();
    }
    
    /// <summary>
    /// 确认密码
    /// </summary>
    private void ConfirmPassword()
    {
        if (_targetPasswordBox != null)
        {
            // 触发解锁按钮点击事件而不是模拟键盘输入
            var parentWindow = Window.GetWindow(_targetPasswordBox);
            if (parentWindow != null)
            {
                // 查找解锁按钮
                var unlockButton = FindChild<Button>(parentWindow, "unlockButton");
                if (unlockButton != null)
                {
                    // 触发解锁按钮点击事件
                    unlockButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    
                    // 确认后清空密码
                    ClearPassword();
                }
            }
        }
    }
    
    /// <summary>
    /// 查找子元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="parent">父元素</param>
    /// <param name="childName">子元素名称</param>
    /// <returns>找到的子元素</returns>
    private T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        // 确认父元素不为空
        if (parent == null) return null;

        // 检查父元素是否是目标类型
        if (parent is T && !string.IsNullOrEmpty(childName))
        {
            var frameworkElement = parent as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Name == childName)
            {
                return (T)parent;
            }
        }

        // 获取子元素数量
        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        
        // 遍历所有子元素
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindChild<T>(child, childName);
            if (result != null) return result;
        }

        return null;
    }
    
    /// <summary>
    /// 更新密码显示
    /// </summary>
    private void UpdatePasswordDisplay()
    {
        if (_passwordDisplay != null)
        {
            if (string.IsNullOrEmpty(_currentPassword))
            {
                _passwordDisplay.Text = "请输入密码";
            }
            else
            {
                // 显示为星号
                var passwordBuilder = new StringBuilder();
                for (int i = 0; i < _currentPassword.Length; i++)
                {
                    passwordBuilder.Append("●");
                }
                _passwordDisplay.Text = passwordBuilder.ToString();
            }
        }
    }
    
    /// <summary>
    /// 更新目标密码框
    /// </summary>
    private void UpdateTargetPasswordBox()
    {
        if (_targetPasswordBox != null)
        {
            // 先清空密码框，然后设置新密码，避免重复添加
            _targetPasswordBox.Clear();
            _targetPasswordBox.Password = _currentPassword;
        }
    }
}