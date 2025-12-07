namespace CCLS.Models;

/// <summary>
/// 课程信息模型
/// </summary>
public class ClassInfo
{
    /// <summary>
    /// 课程ID
    /// </summary>
    public int ClassId { get; set; }
    
    /// <summary>
    /// 课程名称
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public TimeSpan StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public TimeSpan EndTime { get; set; }
    
    /// <summary>
    /// 星期几（1-7，周一到周日）
    /// </summary>
    public int DayOfWeek { get; set; }
    
    /// <summary>
    /// 教室
    /// </summary>
    public string Classroom { get; set; } = string.Empty;
    
    /// <summary>
    /// 教师
    /// </summary>
    public string Teacher { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否为课间休息
    /// </summary>
    public bool IsBreakTime { get; set; } = false;
}

/// <summary>
/// 课表模型
/// </summary>
public class Schedule
{
    /// <summary>
    /// 课程列表
    /// </summary>
    public List<ClassInfo> Classes { get; set; } = new List<ClassInfo>();
    
    /// <summary>
    /// 自动解锁提前时间（分钟）
    /// </summary>
    public int AutoUnlockAdvanceMinutes { get; set; } = 3;
    
    /// <summary>
    /// 获取当天的课程列表
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns>当天课程列表</returns>
    public List<ClassInfo> GetClassesForDay(DateTime date)
    {
        // 转换DateTime.DayOfWeek（0-6，0表示星期日）为1-7（1表示星期一）
        int dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7; // 将星期日转换为7
        
        return Classes.Where(c => c.DayOfWeek == dayOfWeek).OrderBy(c => c.StartTime).ToList();
    }
    
    /// <summary>
    /// 获取当前时间的下一节课
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>下一节课信息</returns>
    public ClassInfo? GetNextClass(DateTime currentTime)
    {
        var todayClasses = GetClassesForDay(currentTime);
        return todayClasses.FirstOrDefault(c => c.StartTime > currentTime.TimeOfDay);
    }
    
    /// <summary>
    /// 判断当前是否为课间时间
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>是否为课间时间</returns>
    public bool IsBreakTime(DateTime currentTime)
    {
        var todayClasses = GetClassesForDay(currentTime);
        if (todayClasses.Count == 0)
            return true; // 当天没有课程，默认视为课间
        
        var currentTimeOfDay = currentTime.TimeOfDay;
        
        // 检查是否在第一节课之前
        if (currentTimeOfDay < todayClasses.First().StartTime)
            return true;
        
        // 检查是否在最后一节课之后
        if (currentTimeOfDay > todayClasses.Last().EndTime)
            return true;
        
        // 检查是否在课程之间的间隙
        for (int i = 0; i < todayClasses.Count - 1; i++)
        {
            if (currentTimeOfDay > todayClasses[i].EndTime && currentTimeOfDay < todayClasses[i + 1].StartTime)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取当前时间状态信息
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <returns>时间状态信息</returns>
    public TimeStatusInfo GetCurrentTimeStatus(DateTime currentTime)
    {
        var todayClasses = GetClassesForDay(currentTime);
        var currentTimeOfDay = currentTime.TimeOfDay;
        
        var isBreakTime = IsBreakTime(currentTime);
        
        // 如果是课间时间
        if (isBreakTime)
        {
            if (todayClasses.Count == 0)
            {
                // 当天没有课程
                return new TimeStatusInfo
                {
                    IsBreakTime = true,
                    CurrentClass = null,
                    PreviousClass = null,
                    NextClass = null,
                    StatusDescription = "今天没有课程"
                };
            }
            
            if (currentTimeOfDay < todayClasses.First().StartTime)
            {
                // 当前时间在第一节课之前
                return new TimeStatusInfo
                {
                    IsBreakTime = true,
                    CurrentClass = null,
                    PreviousClass = null,
                    NextClass = todayClasses.First(),
                    StatusDescription = "第一节课开始前"
                };
            }
            
            if (currentTimeOfDay > todayClasses.Last().EndTime)
            {
                // 当前时间在最后一节课之后
                return new TimeStatusInfo
                {
                    IsBreakTime = true,
                    CurrentClass = null,
                    PreviousClass = todayClasses.Last(),
                    NextClass = null,
                    StatusDescription = "最后一节课结束后"
                };
            }
            
            // 当前时间在课程之间的间隙
            for (int i = 0; i < todayClasses.Count - 1; i++)
            {
                if (currentTimeOfDay > todayClasses[i].EndTime && currentTimeOfDay < todayClasses[i + 1].StartTime)
                {
                    return new TimeStatusInfo
                    {
                        IsBreakTime = true,
                        CurrentClass = null,
                        PreviousClass = todayClasses[i],
                        NextClass = todayClasses[i + 1],
                        StatusDescription = $"第{i + 1}节课与第{i + 2}节课之间的课间休息"
                    };
                }
            }
        }
        
        // 如果是上课时间，找到当前正在上的课程
        var currentClass = todayClasses.FirstOrDefault(c => 
            currentTimeOfDay >= c.StartTime && currentTimeOfDay <= c.EndTime);
        
        if (currentClass != null)
        {
            var classIndex = todayClasses.IndexOf(currentClass);
            
            return new TimeStatusInfo
            {
                IsBreakTime = false,
                CurrentClass = currentClass,
                PreviousClass = classIndex > 0 ? todayClasses[classIndex - 1] : null,
                NextClass = classIndex < todayClasses.Count - 1 ? todayClasses[classIndex + 1] : null,
                StatusDescription = $"正在上第{classIndex + 1}节课：{currentClass.ClassName}"
            };
        }
        
        // 默认情况
        return new TimeStatusInfo
        {
            IsBreakTime = true,
            CurrentClass = null,
            PreviousClass = null,
            NextClass = null,
            StatusDescription = "未知状态"
        };
    }
}

/// <summary>
/// 时间状态信息类
/// 用于更详细地描述当前的时间状态（上课或课间休息）
/// </summary>
public class TimeStatusInfo
{
    /// <summary>
    /// 是否为课间时间
    /// </summary>
    public bool IsBreakTime { get; set; }
    
    /// <summary>
    /// 当前课程（如果是上课时间）
    /// </summary>
    public ClassInfo? CurrentClass { get; set; }
    
    /// <summary>
    /// 上一节课（如果是课间休息时间）
    /// </summary>
    public ClassInfo? PreviousClass { get; set; }
    
    /// <summary>
    /// 下一节课（如果是课间休息时间）
    /// </summary>
    public ClassInfo? NextClass { get; set; }
    
    /// <summary>
    /// 状态描述
    /// </summary>
    public string StatusDescription { get; set; } = string.Empty;
}