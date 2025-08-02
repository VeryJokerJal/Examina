using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Examina.Services;

/// <summary>
/// 安全存储服务实现
/// 使用AES加密存储敏感数据到本地文件
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
    private readonly byte[] _encryptionKey;

    public SecureStorageService()
    {
        // 获取应用数据目录
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "Examina");
        _storageFilePath = Path.Combine(_storageDirectory, "secure_storage.dat");

        // 生成或获取加密密钥
        _encryptionKey = GetOrCreateEncryptionKey();

        // 确保目录存在
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<bool> SetAsync(string key, string value)
    {
        try
        {
            Dictionary<string, string> storage = await LoadStorageAsync();
            storage[key] = value;
            await SaveStorageAsync(storage);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage SetAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            Dictionary<string, string> storage = await LoadStorageAsync();
            return storage.TryGetValue(key, out string? value) ? value : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage GetAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            Dictionary<string, string> storage = await LoadStorageAsync();
            bool removed = storage.Remove(key);
            if (removed)
            {
                await SaveStorageAsync(storage);
            }
            return removed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage RemoveAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ContainsKeyAsync(string key)
    {
        try
        {
            Dictionary<string, string> storage = await LoadStorageAsync();
            return storage.ContainsKey(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage ContainsKeyAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ClearAsync()
    {
        try
        {
            await SaveStorageAsync(new Dictionary<string, string>());
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage ClearAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从文件加载存储数据
    /// </summary>
    private async Task<Dictionary<string, string>> LoadStorageAsync()
    {
        if (!File.Exists(_storageFilePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            byte[] encryptedData = await File.ReadAllBytesAsync(_storageFilePath);
            string decryptedJson = DecryptData(encryptedData);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedJson) 
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            // 如果解密失败，返回空字典
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// 保存存储数据到文件
    /// </summary>
    private async Task SaveStorageAsync(Dictionary<string, string> storage)
    {
        string json = JsonSerializer.Serialize(storage);
        byte[] encryptedData = EncryptData(json);
        await File.WriteAllBytesAsync(_storageFilePath, encryptedData);
    }

    /// <summary>
    /// 加密数据
    /// </summary>
    private byte[] EncryptData(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // 将IV和加密数据组合
        byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return result;
    }

    /// <summary>
    /// 解密数据
    /// </summary>
    private string DecryptData(byte[] encryptedData)
    {
        using Aes aes = Aes.Create();
        aes.Key = _encryptionKey;

        // 提取IV
        byte[] iv = new byte[aes.IV.Length];
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
        aes.IV = iv;

        // 提取加密数据
        byte[] cipherBytes = new byte[encryptedData.Length - iv.Length];
        Array.Copy(encryptedData, iv.Length, cipherBytes, 0, cipherBytes.Length);

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// 获取或创建加密密钥
    /// </summary>
    private byte[] GetOrCreateEncryptionKey()
    {
        string keyFilePath = Path.Combine(_storageDirectory, "key.dat");

        if (File.Exists(keyFilePath))
        {
            try
            {
                return File.ReadAllBytes(keyFilePath);
            }
            catch
            {
                // 如果读取失败，重新生成密钥
            }
        }

        // 生成新的256位密钥
        byte[] key = new byte[32];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        try
        {
            Directory.CreateDirectory(_storageDirectory);
            File.WriteAllBytes(keyFilePath, key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save encryption key: {ex.Message}");
        }

        return key;
    }
}
