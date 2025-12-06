namespace CCLS.Models;

/// <summary>
/// 密码信息模型
/// </summary>
public class PasswordInfo
{
    /// <summary>
    /// 密码ID
    /// </summary>
    public int PasswordId { get; set; }
    
    /// <summary>
    /// 密码所有者姓名
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码角色
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 加密后的密码
    /// </summary>
    public string EncryptedPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedTime { get; set; }
    
    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;
}

/// <summary>
/// 密码验证结果模型
/// </summary>
public class PasswordVerificationResult
{
    /// <summary>
    /// 是否验证成功
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// 匹配的密码信息
    /// </summary>
    public PasswordInfo? MatchedPassword { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// 密码生成请求模型
/// </summary>
public class PasswordGenerationRequest
{
    /// <summary>
    /// 密码长度
    /// </summary>
    public int Length { get; set; } = 15;
}