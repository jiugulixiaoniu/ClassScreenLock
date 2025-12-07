using System;
using System.IO;
using System.Text.Json;
using CCLS.Models;

namespace CCLS.Utilities;

/// <summary>
/// 配置管理器 - 负责加载和保存配置文件
/// </summary>
public static class ConfigManager
{
    // 配置文件路径
    private static readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    
    // 课表文件路径
    private static readonly string _scheduleFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lessontime", "schedule.json");
    
    // 密码文件路径
    private static readonly string _passwordFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "passwords.json");
    
    // JSON序列化选项
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    /// <summary>
    /// 加载应用程序配置
    /// </summary>
    /// <returns>应用程序配置</returns>
    public static ApplicationConfig LoadConfig()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<ApplicationConfig>(json, _jsonOptions) ?? new ApplicationConfig();
            }
            catch (Exception)
            {
                // 如果加载失败，返回默认配置
                return new ApplicationConfig();
            }
        }
        
        // 如果文件不存在，返回默认配置
        return new ApplicationConfig();
    }
    
    /// <summary>
    /// 保存应用程序配置
    /// </summary>
    /// <param name="config">应用程序配置</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveConfig(ApplicationConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configFilePath, json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// 加载课表
    /// </summary>
    /// <returns>课表信息</returns>
    public static Schedule LoadSchedule()
    {
        if (File.Exists(_scheduleFilePath))
        {
            try
            {
                var json = File.ReadAllText(_scheduleFilePath);
                return JsonSerializer.Deserialize<Schedule>(json, _jsonOptions) ?? new Schedule();
            }
            catch (Exception)
            {
                // 如果加载失败，返回默认课表
                return new Schedule();
            }
        }
        
        // 如果文件不存在，返回默认课表
        return new Schedule();
    }
    
    /// <summary>
    /// 保存课表
    /// </summary>
    /// <param name="schedule">课表信息</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveSchedule(Schedule schedule)
    {
        try
        {
            // 确保lessontime文件夹存在
            var lessontimeDir = Path.GetDirectoryName(_scheduleFilePath);
            if (!string.IsNullOrEmpty(lessontimeDir) && !Directory.Exists(lessontimeDir))
            {
                Directory.CreateDirectory(lessontimeDir);
            }
            
            // 序列化前检查数据
            if (schedule == null)
            {
                Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 课表对象为null");
                return false;
            }
            
            if (schedule.Classes == null)
            {
                Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 课程列表为null");
                return false;
            }
            
            Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 开始保存课表，包含 {schedule.Classes.Count} 门课程");
            
            var json = JsonSerializer.Serialize(schedule, _jsonOptions);
            Console.WriteLine($"[{DateTime.Now}] SaveSchedule: JSON序列化成功，长度 {json.Length} 字符");
            
            File.WriteAllText(_scheduleFilePath, json);
            Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 文件保存成功到 {_scheduleFilePath}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 保存失败 - {ex.Message}");
            Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 异常类型 - {ex.GetType().Name}");
            Console.WriteLine($"[{DateTime.Now}] SaveSchedule: 堆栈跟踪 - {ex.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// 加载密码列表
    /// </summary>
    /// <returns>密码列表</returns>
    public static List<PasswordInfo> LoadPasswords()
    {
        if (File.Exists(_passwordFilePath))
        {
            try
            {
                var json = File.ReadAllText(_passwordFilePath);
                return JsonSerializer.Deserialize<List<PasswordInfo>>(json, _jsonOptions) ?? new List<PasswordInfo>();
            }
            catch (Exception)
            {
                // 如果加载失败，返回空列表
                return new List<PasswordInfo>();
            }
        }
        
        // 如果文件不存在，返回空列表
        return new List<PasswordInfo>();
    }
    
    /// <summary>
    /// 保存密码列表
    /// </summary>
    /// <param name="passwords">密码列表</param>
    /// <returns>是否保存成功</returns>
    public static bool SavePasswords(List<PasswordInfo> passwords)
    {
        try
        {
            var json = JsonSerializer.Serialize(passwords, _jsonOptions);
            File.WriteAllText(_passwordFilePath, json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// 备份配置文件
    /// </summary>
    /// <returns>是否备份成功</returns>
    public static bool BackupConfig()
    {
        try
        {
            // 创建备份目录
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
            Directory.CreateDirectory(backupDir);
            
            // 生成备份文件名（包含时间戳）
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            
            // 备份配置文件
            if (File.Exists(_configFilePath))
            {
                var backupPath = Path.Combine(backupDir, $"config_{timestamp}.json");
                File.Copy(_configFilePath, backupPath);
            }
            
            // 备份课表文件
            if (File.Exists(_scheduleFilePath))
            {
                var backupPath = Path.Combine(backupDir, $"schedule_{timestamp}.json");
                File.Copy(_scheduleFilePath, backupPath);
            }
            
            // 备份密码文件
            if (File.Exists(_passwordFilePath))
            {
                var backupPath = Path.Combine(backupDir, $"passwords_{timestamp}.json");
                File.Copy(_passwordFilePath, backupPath);
            }
            
            // 清理旧备份（只保留最近7天的备份）
            CleanupOldBackups(backupDir);
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// 清理旧备份
    /// </summary>
    /// <param name="backupDir">备份目录</param>
    private static void CleanupOldBackups(string backupDir)
    {
        try
        {
            var files = Directory.GetFiles(backupDir)
                .Where(f => f.EndsWith(".json"))
                .Select(f => new FileInfo(f))
                .Where(f => f.CreationTime < DateTime.Now.AddDays(-7))
                .ToList();
            
            foreach (var file in files)
            {
                file.Delete();
            }
        }
        catch (Exception)
        {
            // 忽略清理错误
        }
    }
    
    /// <summary>
    /// 检查是否存在初始配置
    /// </summary>
    /// <returns>是否存在初始配置</returns>
    public static bool HasInitialConfig()
    {
        return File.Exists(_configFilePath) && File.Exists(_scheduleFilePath) && File.Exists(_passwordFilePath);
    }
}