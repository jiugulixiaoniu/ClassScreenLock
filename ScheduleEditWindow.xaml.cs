using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using CCLS.Models;
using CCLS.Services;
using CCLS.Utilities;
using Microsoft.Win32;

namespace CCLS
{
    /// <summary>
    /// ScheduleEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScheduleEditWindow : Window, INotifyPropertyChanged
    {
        private Schedule _schedule;
        private ObservableCollection<ClassInfo> _allClasses;
        
        // 时间设置相关字段
        private TimeSpan _defaultStartTime = new TimeSpan(8, 0, 0); // 默认上课时间 8:00
        private int _classDurationMinutes = 45; // 默认课程时长45分钟
        private int _breakDurationMinutes = 10; // 默认课间休息10分钟
        private DispatcherTimer? _timer;
        
        public Schedule Schedule
        {
            get => _schedule;
            set
            {
                _schedule = value;
                OnPropertyChanged(nameof(Schedule));
            }
        }
        
        public ObservableCollection<ClassInfo> AllClasses
        {
            get => _allClasses;
            set
            {
                _allClasses = value;
                OnPropertyChanged(nameof(AllClasses));
            }
        }
        
        public ScheduleEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            _schedule = new Schedule();
            _allClasses = new ObservableCollection<ClassInfo>();
            
            // 初始化默认时间设置
            InitializeDefaultTimes();
            
            // 加载现有课表
            LoadExistingSchedule();
            
            // 初始化计时器
            InitializeTimer();
        }
        
        /// <summary>
        /// 初始化默认时间设置
        /// </summary>
        private void InitializeDefaultTimes()
        {
            DefaultStartTimeTextBox.Text = "08:00";
            ClassDurationTextBox.Text = "45";
            BreakDurationTextBox.Text = "10";
        }
        
        /// <summary>
        /// 初始化计时器
        /// </summary>
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        
        /// <summary>
        /// 计时器事件处理
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            // 更新当前时间显示（如果界面上有时钟控件）
        }
        
        /// <summary>
        /// 加载现有课表
        /// </summary>
        private void LoadExistingSchedule()
        {
            try
            {
                // 使用ConfigManager加载课表
                Schedule = ConfigManager.LoadSchedule();
                
                // 更新所有课程集合
                AllClasses.Clear();
                foreach (var classInfo in Schedule.Classes.OrderBy(c => c.DayOfWeek).ThenBy(c => c.StartTime))
                {
                    AllClasses.Add(classInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载课表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 保存课表
        /// </summary>
        private void SaveSchedule()
        {
            try
            {
                // 检查课表数据
                if (Schedule == null)
                {
                    MessageBox.Show("课表数据为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (Schedule.Classes == null)
                {
                    MessageBox.Show("课程列表为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 显示要保存的数据信息
                var classInfo = $"要保存的课程数量: {Schedule.Classes.Count}\n";
                if (Schedule.Classes.Count > 0)
                {
                    classInfo += $"第一门课程: {Schedule.Classes[0].ClassName}, IsBreakTime: {Schedule.Classes[0].IsBreakTime}\n";
                    if (Schedule.Classes.Count > 1)
                    {
                        classInfo += $"最后一门课程: {Schedule.Classes[Schedule.Classes.Count - 1].ClassName}, IsBreakTime: {Schedule.Classes[Schedule.Classes.Count - 1].IsBreakTime}";
                    }
                }
                
                // 使用ConfigManager保存课表
                bool success = ConfigManager.SaveSchedule(Schedule);
                if (success)
                {
                    MessageBox.Show($"课表保存成功\n{classInfo}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"保存课表失败\n{classInfo}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存课表失败: {ex.Message}\n异常类型: {ex.GetType().Name}\n堆栈跟踪: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 导出课表为JSON
        /// </summary>
        private void ExportToJson()
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON文件 (*.json)|*.json",
                    Title = "导出课表"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    string json = JsonSerializer.Serialize(Schedule, new JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json);
                    
                    MessageBox.Show("课表导出成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出课表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        
        /// <summary>
        /// 删除时间段
        /// </summary>
        /// <param name="classInfo">要删除的课程信息</param>
        private void DeleteTimeSlot(ClassInfo classInfo)
        {
            if (Schedule.Classes.Contains(classInfo))
            {
                Schedule.Classes.Remove(classInfo);
                AllClasses.Remove(classInfo);
            }
        }
        
        // 事件处理程序
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSchedule();
        }
        

        
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToJson();
        }
        
        private void DeleteTimeSlotButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ClassInfo classInfo)
            {
                DeleteTimeSlot(classInfo);
            }
        }
        
        /// <summary>
        /// 保存默认时间设置
        /// </summary>
        private void SaveDefaultTimesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TimeSpan.TryParse(DefaultStartTimeTextBox.Text, out _defaultStartTime))
                {
                    if (int.TryParse(ClassDurationTextBox.Text, out _classDurationMinutes))
                    {
                        if (int.TryParse(BreakDurationTextBox.Text, out _breakDurationMinutes))
                        {
                            MessageBox.Show("默认时间设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("课间休息时长格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("课程时长格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("默认上课时间格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存默认时间设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 加载默认时间设置
        /// </summary>
        private void LoadDefaultTimesButton_Click(object sender, RoutedEventArgs e)
        {
            DefaultStartTimeTextBox.Text = _defaultStartTime.ToString(@"hh\:mm");
            ClassDurationTextBox.Text = _classDurationMinutes.ToString();
            BreakDurationTextBox.Text = _breakDurationMinutes.ToString();
        }
        
        /// <summary>
        /// 添加上课时间段
        /// </summary>
        private void AddClassButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否已有时间段存在
                TimeSpan startTime;
                if (Schedule.Classes.Count > 0)
                {
                    // 如果有，使用最后一个时间段的结束时间作为新时间段的开始时间
                    startTime = Schedule.Classes.Max(c => c.EndTime);
                }
                else if (TimeSpan.TryParse(DefaultStartTimeTextBox.Text, out TimeSpan defaultStartTime))
                {
                    // 如果没有，使用默认的开始时间
                    startTime = defaultStartTime;
                }
                else
                {
                    MessageBox.Show("默认时间格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (int.TryParse(ClassDurationTextBox.Text, out int classDuration))
                {
                    var endTime = startTime.Add(TimeSpan.FromMinutes(classDuration));
                    
                    var newClass = new ClassInfo
                    {
                        ClassId = Schedule.Classes.Count > 0 ? Schedule.Classes.Max(c => c.ClassId) + 1 : 1,
                        ClassName = "课程",
                        StartTime = startTime,
                        EndTime = endTime,
                        DayOfWeek = (int)DateTime.Now.DayOfWeek == 0 ? 7 : (int)DateTime.Now.DayOfWeek, // 今天是周几
                        Classroom = "未指定",
                        Teacher = "未指定",
                        IsBreakTime = false // 上课时间段
                    };
                    
                    Schedule.Classes.Add(newClass);
                    AllClasses.Add(newClass);
                    
                    // 强制刷新DataGrid显示
                    AllClassesDataGrid.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("课程时长格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加上课时间段失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 添加课间休息时间段
        /// </summary>
        private void AddBreakButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否已有时间段存在
                TimeSpan startTime;
                if (Schedule.Classes.Count > 0)
                {
                    // 如果有，使用最后一个时间段的结束时间作为课间休息的开始时间
                    startTime = Schedule.Classes.Max(c => c.EndTime);
                }
                else if (TimeSpan.TryParse(DefaultStartTimeTextBox.Text, out TimeSpan defaultStartTime) && 
                         int.TryParse(ClassDurationTextBox.Text, out int classDuration))
                {
                    // 如果没有，使用默认开始时间加上课程时长后的时间作为开始时间
                    startTime = defaultStartTime.Add(TimeSpan.FromMinutes(classDuration));
                }
                else
                {
                    MessageBox.Show("时间格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (int.TryParse(BreakDurationTextBox.Text, out int breakMinutes))
                {
                    var endTime = startTime.Add(TimeSpan.FromMinutes(breakMinutes));
                    
                    var newBreak = new ClassInfo
                    {
                        ClassId = Schedule.Classes.Count > 0 ? Schedule.Classes.Max(c => c.ClassId) + 1 : 1,
                        ClassName = "课间休息",
                        StartTime = startTime,
                        EndTime = endTime,
                        DayOfWeek = (int)DateTime.Now.DayOfWeek == 0 ? 7 : (int)DateTime.Now.DayOfWeek, // 今天是周几
                        Classroom = "未指定",
                        Teacher = "未指定",
                        IsBreakTime = true // 课间休息时间段
                    };
                    
                    Schedule.Classes.Add(newBreak);
                    AllClasses.Add(newBreak);
                    
                    // 强制刷新DataGrid显示
                    AllClassesDataGrid.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("课间休息时长格式不正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加课间休息时间段失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}