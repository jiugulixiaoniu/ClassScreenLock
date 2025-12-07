using System.Timers;
using Timer = System.Timers.Timer;
using CCLS.Enums;
using CCLS.Models;

namespace CCLS.Services;

/// <summary>
/// 时间调度服务 - 负责管理课程表和时间相关功能
/// </summary>
public class ScheduleService
{
    // 定时器，每分钟检查一次时间状态
    private readonly Timer _timer;
    
    // 当前课表
    private Schedule _currentSchedule;
    
    // 配置信息
    private ApplicationConfig _config;
    
    /// <summary>
    /// 课间时间开始事件
    /// </summary>
    public event EventHandler? BreakTimeStarted;
    
    /// <summary>
    /// 上课时间开始事件
    /// </summary>
    public event EventHandler? ClassTimeStarted;
    
    /// <summary>
    /// 自动解锁事件
    /// </summary>
    public event EventHandler? AutoUnlockTriggered;
    
    /// <summary>
    /// 当前时间类型
    /// </summary>
    public TimeType CurrentTimeType { get; private set; }
    
    /// <summary>
    /// 下一节课信息
    /// </summary>
    public ClassInfo? NextClass { get; private set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">应用程序配置</param>
    public ScheduleService(ApplicationConfig config)
    {
        _config = config;
        
        // 尝试加载时间表
        try
        {
            _currentSchedule = CCLS.Utilities.ConfigManager.LoadSchedule();
            Console.WriteLine($"[ScheduleService] 成功加载时间表，包含 {_currentSchedule.Classes.Count} 个课程");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ScheduleService] 加载时间表失败: {ex.Message}");
            _currentSchedule = new Schedule();
        }
        
        // 初始化定时器，每30秒执行一次检查，提高响应速度
        _timer = new Timer(30000); // 30秒
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        
        // 立即执行一次检查
        CheckCurrentTimeStatus();
    }
    
    /// <summary>
    /// 设置课表
    /// </summary>
    /// <param name="schedule">课表信息</param>
    public void SetSchedule(Schedule schedule)
    {
        _currentSchedule = schedule;
        CheckCurrentTimeStatus();
    }
    
    /// <summary>
    /// 更新配置
    /// </summary>
    /// <param name="config">新的配置信息</param>
    public void UpdateConfig(ApplicationConfig config)
    {
        _config = config;
        // 重新检查当前时间状态以确保新配置生效
        CheckCurrentTimeStatus();
    }
    
    /// <summary>
    /// 启动定时器
    /// </summary>
    public void Start()
    {
        _timer.Start();
    }
    
    /// <summary>
    /// 停止定时器
    /// </summary>
    public void Stop()
    {
        _timer.Stop();
    }
    
    /// <summary>
    /// 检查当前时间状态
    /// </summary>
    private void CheckCurrentTimeStatus()
    {
        var now = DateTime.Now;
        NextClass = _currentSchedule.GetNextClass(now);
        
        bool isBreakTime = _currentSchedule.IsBreakTime(now);
        var previousTimeType = CurrentTimeType;
        
        Console.WriteLine($"[ScheduleService] 当前时间: {now:HH:mm:ss}, 判断为课间: {isBreakTime}, 上一状态: {previousTimeType}");
        
        if (isBreakTime)
        {
            CurrentTimeType = TimeType.BreakTime;
            
            // 如果时间类型发生变化，触发课间时间开始事件
            if (previousTimeType != TimeType.BreakTime)
            {
                Console.WriteLine("[ScheduleService] 触发课间时间开始事件");
                BreakTimeStarted?.Invoke(this, EventArgs.Empty);
            }
            
            // 检查是否需要触发自动解锁
            CheckAutoUnlock(now);
        }
        else
        {
            CurrentTimeType = TimeType.ClassTime;
            
            // 如果时间类型发生变化，触发上课时间开始事件
            if (previousTimeType != TimeType.ClassTime)
            {
                Console.WriteLine("[ScheduleService] 触发上课时间开始事件");
                ClassTimeStarted?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    
    /// <summary>
    /// 检查是否需要自动解锁
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    private void CheckAutoUnlock(DateTime currentTime)
    {
        if (NextClass == null)
            return;
        
        // 计算上课时间减去提前解锁时间
        var unlockTime = currentTime.Date + NextClass.StartTime - TimeSpan.FromMinutes(_currentSchedule.AutoUnlockAdvanceMinutes);
        
        // 检查当前时间是否已经过了解锁时间，并且在上课时间之前
        if (currentTime >= unlockTime && currentTime < currentTime.Date + NextClass.StartTime)
        {
            AutoUnlockTriggered?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// 获取距离自动解锁的剩余时间
    /// </summary>
    /// <returns>剩余时间（秒）</returns>
    public int GetRemainingTimeUntilAutoUnlock()
    {
        if (NextClass == null)
            return -1;
        
        var now = DateTime.Now;
        var unlockTime = now.Date + NextClass.StartTime - TimeSpan.FromMinutes(_currentSchedule.AutoUnlockAdvanceMinutes);
        
        if (now >= unlockTime)
            return 0;
        
        return (int)(unlockTime - now).TotalSeconds;
    }
    
    /// <summary>
    /// 定时器事件处理
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        CheckCurrentTimeStatus();
    }
    
    /// <summary>
    /// 手动检查时间状态
    /// </summary>
    public void ManualCheck()
    {
        CheckCurrentTimeStatus();
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }
}