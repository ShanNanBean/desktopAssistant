using System.Security.Cryptography;
using System.Text;

namespace PowerShellHelper.Services;

/// <summary>
/// 加密解密工具(使用Windows DPAPI)
/// </summary>
public static class EncryptionHelper
{
    /// <summary>
    /// 加密字符串
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.CurrentUser
            );
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            return plainText; // 加密失败返回原文
        }
    }

    /// <summary>
    /// 解密字符串
    /// </summary>
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return encryptedText; // 解密失败返回原文
        }
    }
}
