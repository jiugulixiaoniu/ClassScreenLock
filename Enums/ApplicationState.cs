namespace CCLS.Enums;

/// <summary>
/// 应用程序状态枚举
/// </summary>
public enum ApplicationState
{
    /// <summary>
    /// 空闲状态
    /// </summary>
    Idle,
    
    /// <summary>
    /// 课间状态（显示锁定按钮）
    /// </summary>
    BreakTime,
    
    /// <summary>
    /// 锁定状态
    /// </summary>
    Locked,
    
    /// <summary>
    /// 配置状态
    /// </summary>
    Configuring
}

/// <summary>
/// 时间类型枚举
/// </summary>
public enum TimeType
{
    /// <summary>
    /// 上课时间
    /// </summary>
    ClassTime,
    
    /// <summary>
    /// 课间时间
    /// </summary>
    BreakTime
}

/// <summary>
/// 解锁方式枚举
/// </summary>
public enum UnlockMethod
{
    /// <summary>
    /// 自动解锁（时间到）
    /// </summary>
    Auto,
    
    /// <summary>
    /// 管理员密码解锁
    /// </summary>
    AdminPassword,
    
    /// <summary>
    /// 紧急解锁
    /// </summary>
    Emergency
}