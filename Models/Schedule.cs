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
        return Classes.Where(c => c.DayOfWeek == (int)date.DayOfWeek).OrderBy(c => c.StartTime).ToList();
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
}