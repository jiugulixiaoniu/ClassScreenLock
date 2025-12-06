using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CCLS.Enums;
using CCLS.Models;

namespace CCLS.Services;

/// <summary>
/// 密码服务类 - 负责密码的生成、加密和验证
/// </summary>
public class PasswordService
{
    // 用于加密的密钥（使用SHA256生成固定长度的32字节密钥）
    private readonly byte[] _encryptionKey;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public PasswordService()
    {
        // 使用SHA256哈希生成固定长度的32字节密钥
        using var sha256 = SHA256.Create();
        _encryptionKey = sha256.ComputeHash(Encoding.UTF8.GetBytes("ClassScreenLock-Encryption-Key-2024"));
    }
    
    // 大写字符集（排除易混淆字符）
    private readonly string _validUppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";
    
    /// <summary>
    /// 生成随机密码
    /// </summary>
    /// <param name="request">密码生成请求</param>
    /// <returns>生成的密码（带空格格式）</returns>
    public string GeneratePassword(PasswordGenerationRequest request)
    {
        var random = new Random();
        var passwordBuilder = new StringBuilder();
        
        // 生成15位连续字符
        for (int i = 0; i < request.Length; i++)
        {
            passwordBuilder.Append(_validUppercaseChars[random.Next(_validUppercaseChars.Length)]);
        }
        
        var password = passwordBuilder.ToString();
        
        // 每5个字符添加一个空格，提高可读性
        var formattedPassword = new StringBuilder();
        for (int i = 0; i < password.Length; i++)
        {
            if (i > 0 && i % 5 == 0)
            {
                formattedPassword.Append(' ');
            }
            formattedPassword.Append(password[i]);
        }
        
        return formattedPassword.ToString();
    }
    
    /// <summary>
    /// 验证密码格式
    /// </summary>
    /// <param name="password">待验证的密码</param>
    /// <returns>是否符合格式要求</returns>
    public bool ValidatePasswordFormat(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;
        
        // 移除所有空格后验证格式
        var cleanPassword = password.Replace(" ", "");
        
        // 检查是否为15位连续字符，且只包含大写字母和数字
        var formatRegex = new Regex(@"^[A-Z0-9]{15}$");
        return formatRegex.IsMatch(cleanPassword);
    }
    
    /// <summary>
    /// 加密密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>加密后的密码</returns>
    public string EncryptPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return string.Empty;
        
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream();
        
        // 先写入IV
        memoryStream.Write(aes.IV, 0, aes.IV.Length);
        
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(cryptoStream);
        streamWriter.Write(password);
        
        streamWriter.Close();
        cryptoStream.Close();
        
        return Convert.ToBase64String(memoryStream.ToArray());
    }
    
    /// <summary>
    /// 解密密码
    /// </summary>
    /// <param name="encryptedPassword">加密后的密码</param>
    /// <returns>解密后的明文密码</returns>
    public string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrWhiteSpace(encryptedPassword))
            return string.Empty;
        
        try
        {
            var cipherText = Convert.FromBase64String(encryptedPassword);
            
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            
            // 从密文中读取IV
            var iv = new byte[aes.IV.Length];
            Array.Copy(cipherText, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            
            return streamReader.ReadToEnd();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="password">待验证的密码</param>
    /// <param name="storedPasswords">存储的密码列表</param>
    /// <returns>验证结果</returns>
    public PasswordVerificationResult VerifyPassword(string password, List<PasswordInfo> storedPasswords)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordVerificationResult
            {
                IsValid = false,
                ErrorMessage = "密码不能为空"
            };
        }
        
        // 移除所有空格
        var cleanPassword = password.Replace(" ", "");
        
        if (!ValidatePasswordFormat(cleanPassword))
        {
            return new PasswordVerificationResult
            {
                IsValid = false,
                ErrorMessage = "密码格式不正确，应为15位大写字母和数字（格式：XXXXX XXXXX XXXXX）"
            };
        }
        
        foreach (var storedPassword in storedPasswords)
        {
            var decryptedPassword = DecryptPassword(storedPassword.EncryptedPassword);
            if (cleanPassword == decryptedPassword)
            {
                // 更新密码使用信息
                storedPassword.LastUsedTime = DateTime.Now;
                storedPassword.UsageCount++;
                
                return new PasswordVerificationResult
                {
                    IsValid = true,
                    MatchedPassword = storedPassword
                };
            }
        }
        
        return new PasswordVerificationResult
        {
            IsValid = false,
            ErrorMessage = "密码不正确"
        };
    }
    
    /// <summary>
    /// 创建新的密码信息
    /// </summary>
    /// <param name="ownerName">所有者姓名</param>
    /// <param name="role">角色</param>
    /// <returns>创建的密码信息</returns>
    public PasswordInfo CreatePasswordInfo(string ownerName, string role)
    {
        var password = GeneratePassword(new PasswordGenerationRequest());
        // 移除空格后加密保存
        var cleanPassword = password.Replace(" ", "");
        var encryptedPassword = EncryptPassword(cleanPassword);
        
        return new PasswordInfo
        {
            OwnerName = ownerName,
            Role = role,
            EncryptedPassword = encryptedPassword,
            CreatedTime = DateTime.Now,
            LastUsedTime = null,
            UsageCount = 0
        };
    }
}