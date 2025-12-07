using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace CCLS;

/// <summary>
/// LogViewerWindow.xaml 的交互逻辑
/// </summary>
public partial class LogViewerWindow : Window
{
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private readonly List<LogEntry> _allLogEntries = new();
    private string _logDirectory;
    
    /// <summary>
    /// 日志条目类
    /// </summary>
    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FullMessage { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public LogViewerWindow()
    {
        InitializeComponent();
        
        // 设置日志目录（与exe同目录的Logs文件夹）
        _logDirectory = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Logs");
        
        // 初始化数据绑定
        LogDataGrid.ItemsSource = _logEntries;
        
        // 设置默认日期范围（最近7天）
        EndDatePicker.SelectedDate = DateTime.Now;
        StartDatePicker.SelectedDate = DateTime.Now.AddDays(-7);
        
        // 加载日志
        LoadLogs();
    }
    
    /// <summary>
    /// 加载日志文件
    /// </summary>
    private void LoadLogs()
    {
        try
        {
            _allLogEntries.Clear();
            
            // 确保日志目录存在
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
                StatusText.Text = "日志目录不存在，已创建";
                return;
            }
            
            // 获取所有日志文件
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            
            foreach (var logFile in logFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(logFile);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                            
                        // 解析日志行
                        var logEntry = ParseLogLine(line, logFile);
                        if (logEntry != null)
                        {
                            _allLogEntries.Add(logEntry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取日志文件 {logFile} 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
            // 按时间倒序排序
            _allLogEntries.Sort((x, y) => 
            {
                if (DateTime.TryParse(x.Timestamp, out var xTime) && DateTime.TryParse(y.Timestamp, out var yTime))
                    return DateTime.Compare(yTime, xTime);
                return 0;
            });
            
            // 应用默认筛选
            ApplyFilters();
            
            StatusText.Text = $"已加载 {_allLogEntries.Count} 条日志记录";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "加载日志失败";
        }
    }
    
    /// <summary>
    /// 解析日志行
    /// </summary>
    /// <param name="line">日志行</param>
    /// <param name="fileName">文件名</param>
    /// <returns>解析后的日志条目</returns>
    private LogEntry? ParseLogLine(string line, string fileName)
    {
        try
        {
            // 尝试解析标准日志格式: [时间戳] [级别] 消息
            if (line.StartsWith("[") && line.Contains("]"))
            {
                var firstEndIndex = line.IndexOf(']');
                if (firstEndIndex > 1)
                {
                    var timestampStr = line.Substring(1, firstEndIndex - 1);
                    var remainingPart = line.Substring(firstEndIndex + 1).Trim();
                    
                    // 尝试解析时间戳
                    if (DateTime.TryParse(timestampStr, out var timestamp))
                    {
                        // 尝试提取日志级别
                        string level = "信息";
                        string message = remainingPart;
                        
                        // 检查是否有级别信息 [级别]
                        if (remainingPart.StartsWith("[") && remainingPart.Contains("]"))
                        {
                            var secondEndIndex = remainingPart.IndexOf(']');
                            if (secondEndIndex > 1)
                            {
                                var levelStr = remainingPart.Substring(1, secondEndIndex - 1);
                                if (!string.IsNullOrWhiteSpace(levelStr))
                                {
                                    level = levelStr;
                                    message = remainingPart.Substring(secondEndIndex + 1).Trim();
                                }
                            }
                        }
                        else
                        {
                            // 从消息内容推断级别
                            level = DetermineLogLevel(remainingPart, fileName);
                        }
                        
                        return new LogEntry
                        {
                            Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            Level = level,
                            Message = TruncateMessage(message, 100),
                            FullMessage = message
                        };
                    }
                }
            }
            
            // 如果不是标准格式，尝试其他解析方式
            return new LogEntry
            {
                Timestamp = File.GetLastWriteTime(fileName).ToString("yyyy-MM-dd HH:mm:ss"),
                Level = DetermineLogLevel(line, fileName),
                Message = TruncateMessage(line, 100),
                FullMessage = line
            };
        }
        catch
        {
            // 解析失败，返回基本条目
            return new LogEntry
            {
                Timestamp = File.GetLastWriteTime(fileName).ToString("yyyy-MM-dd HH:mm:ss"),
                Level = "未知",
                Message = TruncateMessage(line, 100),
                FullMessage = line
            };
        }
    }
    
    /// <summary>
    /// 确定日志级别
    /// </summary>
    /// <param name="message">日志消息</param>
    /// <param name="fileName">文件名</param>
    /// <returns>日志级别</returns>
    private string DetermineLogLevel(string message, string fileName)
    {
        // 根据文件名判断
        if (fileName.Contains("Error"))
            return "错误";
            
        // 根据消息内容判断
        if (message.Contains("错误") || message.Contains("失败") || message.Contains("异常"))
            return "错误";
        else if (message.Contains("警告") || message.Contains("注意"))
            return "警告";
        else
            return "信息";
    }
    
    /// <summary>
    /// 截断消息
    /// </summary>
    /// <param name="message">原始消息</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>截断后的消息</returns>
    private string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
            return message;
            
        return message.Substring(0, maxLength) + "...";
    }
    
    /// <summary>
    /// 应用筛选条件
    /// </summary>
    private void ApplyFilters()
    {
        try
        {
            _logEntries.Clear();
            
            var filteredLogs = _allLogEntries.AsEnumerable();
            
            // 日期筛选
            if (StartDatePicker.SelectedDate.HasValue)
            {
                var startDate = StartDatePicker.SelectedDate.Value.Date;
                filteredLogs = filteredLogs.Where(log => 
                {
                    if (DateTime.TryParse(log.Timestamp, out var timestamp))
                        return timestamp.Date >= startDate;
                    return false;
                });
            }
            
            if (EndDatePicker.SelectedDate.HasValue)
            {
                var endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                filteredLogs = filteredLogs.Where(log => 
                {
                    if (DateTime.TryParse(log.Timestamp, out var timestamp))
                        return timestamp <= endDate;
                    return false;
                });
            }
            
            // 日志级别筛选
            var selectedLevel = (LogLevelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedLevel != null && selectedLevel != "全部")
            {
                filteredLogs = filteredLogs.Where(log => log.Level == selectedLevel);
            }
            
            // 关键词筛选
            var keyword = KeywordTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(keyword))
            {
                filteredLogs = filteredLogs.Where(log => 
                    log.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    log.FullMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }
            
            // 添加到结果集
            foreach (var log in filteredLogs)
            {
                _logEntries.Add(log);
            }
            
            CountText.Text = $"共 {_logEntries.Count} 条记录 (筛选自 {_allLogEntries.Count} 条)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用筛选条件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 查询按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
        StatusText.Text = "查询完成";
    }
    
    /// <summary>
    /// 清除条件按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        // 重置筛选条件
        StartDatePicker.SelectedDate = DateTime.Now.AddDays(-7);
        EndDatePicker.SelectedDate = DateTime.Now;
        LogLevelComboBox.SelectedIndex = 0; // 全部
        KeywordTextBox.Text = string.Empty;
        
        // 应用筛选
        ApplyFilters();
        StatusText.Text = "已清除筛选条件";
    }
    
    /// <summary>
    /// 导出按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                Title = "导出日志",
                FileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                var isCsv = saveFileDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
                var sb = new StringBuilder();
                
                if (isCsv)
                {
                    // CSV格式
                    sb.AppendLine("时间,级别,消息");
                    foreach (var log in _logEntries)
                    {
                        sb.AppendLine($"\"{log.Timestamp}\",\"{log.Level}\",\"{log.FullMessage.Replace("\"", "\"\"")}\"");
                    }
                }
                else
                {
                    // 文本格式
                    foreach (var log in _logEntries)
                    {
                        sb.AppendLine($"[{log.Timestamp}] [{log.Level}] {log.FullMessage}");
                    }
                }
                
                File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"日志已导出到: {saveFileDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "导出成功";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// 刷新按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
        StatusText.Text = "已刷新日志";
    }
    
    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = LogDataGrid.SelectedItems.Cast<LogEntry>().ToList();
        if (selectedItems.Count == 0)
        {
            MessageBox.Show("请先选择要删除的日志条目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var result = MessageBox.Show($"确定要删除选中的 {selectedItems.Count} 条日志吗？\n注意：此操作不可撤销！", 
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // 从显示列表中移除
                foreach (var item in selectedItems)
                {
                    _logEntries.Remove(item);
                    _allLogEntries.Remove(item);
                }
                
                CountText.Text = $"共 {_logEntries.Count} 条记录 (筛选自 {_allLogEntries.Count} 条)";
                StatusText.Text = $"已删除 {selectedItems.Count} 条日志";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    /// <summary>
    /// 日志双击事件 - 显示详细信息
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void LogDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LogDataGrid.SelectedItem is LogEntry selectedLog)
        {
            var detailWindow = new Window
            {
                Title = "日志详细信息",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                FontFamily = new System.Windows.Media.FontFamily(App.CurrentConfig.FontFamily),
                FontSize = App.CurrentConfig.FontSize
            };
            
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };
            
            var stackPanel = new StackPanel();
            
            // 时间
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"时间: {selectedLog.Timestamp}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            // 级别
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"级别: {selectedLog.Level}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            // 消息
            stackPanel.Children.Add(new TextBlock
            {
                Text = "消息:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            stackPanel.Children.Add(new TextBox
            {
                Text = selectedLog.FullMessage,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 200,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily(App.CurrentConfig.FontFamily),
                FontSize = App.CurrentConfig.FontSize
            });
            
            scrollViewer.Content = stackPanel;
            detailWindow.Content = scrollViewer;
            
            detailWindow.ShowDialog();
        }
    }
}