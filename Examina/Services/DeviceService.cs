using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Examina.Models;

namespace Examina.Services;

/// <summary>
/// 设备服务实现
/// 生成和管理设备序列号格式的设备指纹（格式：XXXX-XXXX-XXXX）
/// </summary>
public class DeviceService : IDeviceService
{
    private const string DeviceSerialNumberFileName = "device.serial";
    private readonly string _appDataPath;

    public DeviceService()
    {
        // 获取应用程序数据目录
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Examina");

        // 确保目录存在
        _ = Directory.CreateDirectory(_appDataPath);
    }

    /// <summary>
    /// 生成设备序列号格式的设备指纹
    /// 格式：XXXX-XXXX-XXXX（四位字母数字-四位字母数字-四位字母数字）
    /// 基于设备硬件特征生成，确保同一设备上的一致性
    /// </summary>
    /// <returns>设备序列号格式的指纹，如：A1B2-C3D4-E5F6</returns>
    public string GenerateDeviceFingerprint()
    {
        // 检查是否已有保存的设备序列号
        string? savedSerialNumber = LoadDeviceFingerprint();
        if (!string.IsNullOrEmpty(savedSerialNumber) && IsValidSerialNumberFormat(savedSerialNumber))
        {
            return savedSerialNumber;
        }

        // 基于硬件特征生成设备序列号
        string hardwareFingerprint = GenerateHardwareFingerprint();
        string serialNumber = ConvertToSerialNumberFormat(hardwareFingerprint);

        // 保存设备序列号
        SaveDeviceFingerprint(serialNumber);

        return serialNumber;
    }

    /// <summary>
    /// 获取设备信息，包含设备序列号格式的指纹
    /// </summary>
    /// <returns>设备绑定请求信息</returns>
    public DeviceBindRequest GetDeviceInfo()
    {
        return new DeviceBindRequest
        {
            DeviceFingerprint = GenerateDeviceFingerprint(),
            DeviceName = GetDeviceName(),
            DeviceType = GetDeviceType(),
            OperatingSystem = GetOperatingSystem(),
            BrowserInfo = GetApplicationInfo()
        };
    }

    public string GetDeviceName()
    {
        try
        {
            return Environment.MachineName;
        }
        catch
        {
            return "Unknown Device";
        }
    }

    public string GetDeviceType()
    {
        return "Desktop";
    }

    public string GetOperatingSystem()
    {
        try
        {
            string osDescription = RuntimeInformation.OSDescription;
            string architecture = RuntimeInformation.OSArchitecture.ToString();
            return $"{osDescription} ({architecture})";
        }
        catch
        {
            return "Unknown OS";
        }
    }

    public string GetApplicationInfo()
    {
        try
        {
            string? appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string? appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            string framework = RuntimeInformation.FrameworkDescription;

            return $"{appName} {appVersion} ({framework})";
        }
        catch
        {
            return "Examina Desktop Client";
        }
    }

    /// <summary>
    /// 保存设备序列号到本地文件
    /// </summary>
    /// <param name="serialNumber">设备序列号</param>
    public void SaveDeviceFingerprint(string serialNumber)
    {
        try
        {
            string filePath = Path.Combine(_appDataPath, DeviceSerialNumberFileName);
            File.WriteAllText(filePath, serialNumber);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    /// <summary>
    /// 从本地文件加载设备序列号
    /// </summary>
    /// <returns>设备序列号，如果不存在则返回null</returns>
    public string? LoadDeviceFingerprint()
    {
        try
        {
            string filePath = Path.Combine(_appDataPath, DeviceSerialNumberFileName);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
        }
        catch
        {
            // 忽略读取错误
        }

        return null;
    }

    /// <summary>
    /// 生成基于硬件特征的指纹
    /// </summary>
    /// <returns>硬件指纹</returns>
    private static string GenerateHardwareFingerprint()
    {
        try
        {
            // 收集硬件特征信息
            string machineName = Environment.MachineName;
            string userName = Environment.UserName;
            string osVersion = Environment.OSVersion.ToString();
            string processorCount = Environment.ProcessorCount.ToString();
            string osArchitecture = RuntimeInformation.OSArchitecture.ToString();
            string frameworkDescription = RuntimeInformation.FrameworkDescription;

            // 组合硬件特征
            string input = $"{machineName}|{userName}|{osVersion}|{processorCount}|{osArchitecture}|{frameworkDescription}";

            // 生成SHA256哈希
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes);
        }
        catch
        {
            // 如果获取硬件信息失败，使用备用方案
            string fallbackInput = $"{Environment.MachineName}|{Environment.UserName}|{DateTime.UtcNow:yyyyMMdd}";
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fallbackInput));
            return Convert.ToHexString(hashBytes);
        }
    }

    /// <summary>
    /// 将硬件指纹转换为设备序列号格式
    /// 格式：XXXX-XXXX-XXXX（四位字母数字-四位字母数字-四位字母数字）
    /// </summary>
    /// <param name="hardwareFingerprint">硬件指纹</param>
    /// <returns>设备序列号格式的字符串</returns>
    private static string ConvertToSerialNumberFormat(string hardwareFingerprint)
    {
        // 使用硬件指纹的前12个字符
        string hexString = hardwareFingerprint[..Math.Min(12, hardwareFingerprint.Length)];

        // 确保有足够的字符
        if (hexString.Length < 12)
        {
            hexString = hexString.PadRight(12, '0');
        }

        // 转换为大写并格式化为 XXXX-XXXX-XXXX
        string upperHex = hexString.ToUpperInvariant();
        return $"{upperHex[..4]}-{upperHex[4..8]}-{upperHex[8..12]}";
    }

    /// <summary>
    /// 验证设备序列号格式是否有效
    /// </summary>
    /// <param name="serialNumber">设备序列号</param>
    /// <returns>是否为有效格式</returns>
    private static bool IsValidSerialNumberFormat(string serialNumber)
    {
        if (string.IsNullOrEmpty(serialNumber) || serialNumber.Length != 14)
        {
            return false;
        }

        // 检查格式：XXXX-XXXX-XXXX
        if (serialNumber[4] != '-' || serialNumber[9] != '-')
        {
            return false;
        }

        // 检查每个部分是否为有效的十六进制字符
        string[] parts = serialNumber.Split('-');
        if (parts.Length != 3)
        {
            return false;
        }

        foreach (string part in parts)
        {
            if (part.Length != 4)
            {
                return false;
            }

            foreach (char c in part)
            {
                if (!char.IsAsciiHexDigit(c))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
