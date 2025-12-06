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
        private ObservableCollection<ClassInfo> _mondayClasses;
        private ObservableCollection<ClassInfo> _tuesdayClasses;
        private ObservableCollection<ClassInfo> _wednesdayClasses;
        private ObservableCollection<ClassInfo> _thursdayClasses;
        private ObservableCollection<ClassInfo> _fridayClasses;
        
        public Schedule Schedule
        {
            get => _schedule;
            set
            {
                _schedule = value;
                OnPropertyChanged(nameof(Schedule));
            }
        }
        
        public ObservableCollection<ClassInfo> MondayClasses
        {
            get => _mondayClasses;
            set
            {
                _mondayClasses = value;
                OnPropertyChanged(nameof(MondayClasses));
            }
        }
        
        public ObservableCollection<ClassInfo> TuesdayClasses
        {
            get => _tuesdayClasses;
            set
            {
                _tuesdayClasses = value;
                OnPropertyChanged(nameof(TuesdayClasses));
            }
        }
        
        public ObservableCollection<ClassInfo> WednesdayClasses
        {
            get => _wednesdayClasses;
            set
            {
                _wednesdayClasses = value;
                OnPropertyChanged(nameof(WednesdayClasses));
            }
        }
        
        public ObservableCollection<ClassInfo> ThursdayClasses
        {
            get => _thursdayClasses;
            set
            {
                _thursdayClasses = value;
                OnPropertyChanged(nameof(ThursdayClasses));
            }
        }
        
        public ObservableCollection<ClassInfo> FridayClasses
        {
            get => _fridayClasses;
            set
            {
                _fridayClasses = value;
                OnPropertyChanged(nameof(FridayClasses));
            }
        }
        
        public ScheduleEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            _schedule = new Schedule();
            
            // 初始化ObservableCollection字段
            _mondayClasses = new ObservableCollection<ClassInfo>();
            _tuesdayClasses = new ObservableCollection<ClassInfo>();
            _wednesdayClasses = new ObservableCollection<ClassInfo>();
            _thursdayClasses = new ObservableCollection<ClassInfo>();
            _fridayClasses = new ObservableCollection<ClassInfo>();
            
            // 加载当前课表
            LoadSchedule();
        }
        
        /// <summary>
        /// 加载课表
        /// </summary>
        private void LoadSchedule()
        {
            try
            {
                Schedule = ConfigManager.LoadSchedule();
                
                // 初始化ObservableCollection
                MondayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Monday).OrderBy(c => c.StartTime));
                TuesdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Tuesday).OrderBy(c => c.StartTime));
                WednesdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Wednesday).OrderBy(c => c.StartTime));
                ThursdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Thursday).OrderBy(c => c.StartTime));
                FridayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Friday).OrderBy(c => c.StartTime));
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
                // 合并所有课程
                var allClasses = new List<ClassInfo>();
                allClasses.AddRange(MondayClasses);
                allClasses.AddRange(TuesdayClasses);
                allClasses.AddRange(WednesdayClasses);
                allClasses.AddRange(ThursdayClasses);
                allClasses.AddRange(FridayClasses);
                
                Schedule.Classes = allClasses;
                
                // 保存到配置文件
                ConfigManager.SaveSchedule(Schedule);
                
                MessageBox.Show("课表保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存课表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 添加课程
        /// </summary>
        private void AddClass(DayOfWeek dayOfWeek)
        {
            var newClass = new ClassInfo
            {
                ClassId = new Random().Next(1000, 9999),
                ClassName = "新课程",
                DayOfWeek = (int)dayOfWeek,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(8, 45, 0),
                Classroom = "未指定",
                Teacher = "未指定"
            };
            
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    MondayClasses.Add(newClass);
                    break;
                case DayOfWeek.Tuesday:
                    TuesdayClasses.Add(newClass);
                    break;
                case DayOfWeek.Wednesday:
                    WednesdayClasses.Add(newClass);
                    break;
                case DayOfWeek.Thursday:
                    ThursdayClasses.Add(newClass);
                    break;
                case DayOfWeek.Friday:
                    FridayClasses.Add(newClass);
                    break;
            }
        }
        
        /// <summary>
        /// 删除课程
        /// </summary>
        private void DeleteClass(ClassInfo classInfo)
        {
            MondayClasses.Remove(classInfo);
            TuesdayClasses.Remove(classInfo);
            WednesdayClasses.Remove(classInfo);
            ThursdayClasses.Remove(classInfo);
            FridayClasses.Remove(classInfo);
        }
        
        /// <summary>
        /// 从JSON导入课表
        /// </summary>
        private void ImportFromJson()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "导入课表文件"
                };
                
                if (openFileDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openFileDialog.FileName);
                    
                    // 尝试先按新格式解析
                    var newFormatSchedule = JsonSerializer.Deserialize<NewScheduleFormat>(json);
                    
                    if (newFormatSchedule != null && newFormatSchedule.TimeLayouts.Count > 0 && newFormatSchedule.ClassPlans.Count > 0)
                    {
                        // 转换新格式到旧格式
                        var convertedSchedule = ConvertNewFormatToOld(newFormatSchedule);
                        
                        if (convertedSchedule != null)
                        {
                            Schedule = convertedSchedule;
                            // 直接更新各个星期的课程集合，而不是从配置文件重新加载
                            MondayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Monday).OrderBy(c => c.StartTime));
                            TuesdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Tuesday).OrderBy(c => c.StartTime));
                            WednesdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Wednesday).OrderBy(c => c.StartTime));
                            ThursdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Thursday).OrderBy(c => c.StartTime));
                            FridayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Friday).OrderBy(c => c.StartTime));
                            MessageBox.Show("课表(新格式)导入成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                    
                    // 尝试按旧格式解析
                    var importedSchedule = JsonSerializer.Deserialize<Schedule>(json);
                    
                    if (importedSchedule != null)
                    {
                        Schedule = importedSchedule;
                        // 直接更新各个星期的课程集合，而不是从配置文件重新加载
                        MondayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Monday).OrderBy(c => c.StartTime));
                        TuesdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Tuesday).OrderBy(c => c.StartTime));
                        WednesdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Wednesday).OrderBy(c => c.StartTime));
                        ThursdayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Thursday).OrderBy(c => c.StartTime));
                        FridayClasses = new ObservableCollection<ClassInfo>(Schedule.Classes.Where(c => c.DayOfWeek == (int)DayOfWeek.Friday).OrderBy(c => c.StartTime));
                        MessageBox.Show("课表(旧格式)导入成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("无法识别课表格式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入课表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 将新格式的课表转换为旧格式
        /// </summary>
        /// <param name="newFormat">新格式的课表</param>
        /// <returns>旧格式的课表</returns>
        private Schedule? ConvertNewFormatToOld(NewScheduleFormat newFormat)
        {
            try
            {
                var schedule = new Schedule();
                
                // 获取默认的时间布局
                var defaultTimeLayout = newFormat.TimeLayouts.Values.FirstOrDefault(tl => tl.IsActive);
                if (defaultTimeLayout == null)
                {
                    defaultTimeLayout = newFormat.TimeLayouts.Values.FirstOrDefault();
                }
                
                if (defaultTimeLayout == null)
                {
                    MessageBox.Show("无法找到时间布局", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                
                // 获取所有上课时间段（TimeType=0）
                var classTimeSlots = defaultTimeLayout.Layouts.Where(l => l.TimeType == 0 && l.IsActive).OrderBy(l => l.StartSecond).ToList();
                
                if (classTimeSlots.Count == 0)
                {
                    MessageBox.Show("无法找到上课时间段", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                
                // 处理每个班级计划
                foreach (var classPlan in newFormat.ClassPlans.Values.Where(cp => cp.IsEnabled && cp.IsActive))
                {
                    var weekDay = classPlan.TimeRule.WeekDay;
                    
                    // 遍历每个课程
                    for (int i = 0; i < classPlan.Classes.Count; i++)
                    {
                        var classItem = classPlan.Classes[i];
                        
                        if (!classItem.IsEnabled || string.IsNullOrEmpty(classItem.SubjectId))
                            continue;
                        
                        // 获取对应的课程信息
                        if (newFormat.Subjects.TryGetValue(classItem.SubjectId, out var subject))
                        {
                            // 获取对应的时间段
                            if (i < classTimeSlots.Count)
                            {
                                var timeSlot = classTimeSlots[i];
                                
                                var classInfo = new ClassInfo
                                {
                                    ClassId = new Random().Next(1000, 9999),
                                    ClassName = subject.Name,
                                    DayOfWeek = weekDay,
                                    StartTime = timeSlot.StartSecond.TimeOfDay,
                                    EndTime = timeSlot.EndSecond.TimeOfDay,
                                    Classroom = "未指定", // 新格式中没有教室信息
                                    Teacher = subject.TeacherName
                                };
                                
                                schedule.Classes.Add(classInfo);
                            }
                        }
                    }
                }
                
                return schedule;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"课表格式转换失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        
        /// <summary>
        /// 导出课表为JSON
        /// </summary>
        private void ExportToJson()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "导出课表文件",
                    FileName = $"Schedule_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    // 合并所有课程
                    var allClasses = new List<ClassInfo>();
                    allClasses.AddRange(MondayClasses);
                    allClasses.AddRange(TuesdayClasses);
                    allClasses.AddRange(WednesdayClasses);
                    allClasses.AddRange(ThursdayClasses);
                    allClasses.AddRange(FridayClasses);
                    
                    Schedule.Classes = allClasses;
                    
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    
                    var json = JsonSerializer.Serialize(Schedule, options);
                    File.WriteAllText(saveFileDialog.FileName, json);
                    
                    MessageBox.Show("课表导出成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出课表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // 事件处理程序
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSchedule();
        }
        
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportFromJson();
        }
        
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToJson();
        }
        
        private void AddMondayClassButton_Click(object sender, RoutedEventArgs e)
        {
            AddClass(DayOfWeek.Monday);
        }
        
        private void AddTuesdayClassButton_Click(object sender, RoutedEventArgs e)
        {
            AddClass(DayOfWeek.Tuesday);
        }
        
        private void AddWednesdayClassButton_Click(object sender, RoutedEventArgs e)
        {
            AddClass(DayOfWeek.Wednesday);
        }
        
        private void AddThursdayClassButton_Click(object sender, RoutedEventArgs e)
        {
            AddClass(DayOfWeek.Thursday);
        }
        
        private void AddFridayClassButton_Click(object sender, RoutedEventArgs e)
        {
            AddClass(DayOfWeek.Friday);
        }
        
        private void DeleteClassButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ClassInfo classInfo)
            {
                DeleteClass(classInfo);
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}