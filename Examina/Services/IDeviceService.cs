using Examina.Models;

namespace Examina.Services;

/// <summary>
/// 设备服务接口
/// 提供设备序列号格式的设备指纹生成和管理功能
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// 生成设备序列号格式的设备指纹
    /// 格式：XXXX-XXXX-XXXX（四位字母数字-四位字母数字-四位字母数字）
    /// 基于设备硬件特征生成，确保同一设备上的一致性
    /// </summary>
    /// <returns>设备序列号格式的指纹，如：A1B2-C3D4-E5F6</returns>
    string GenerateDeviceFingerprint();

    /// <summary>
    /// 获取设备信息
    /// </summary>
    /// <returns>设备绑定请求信息</returns>
    DeviceBindRequest GetDeviceInfo();

    /// <summary>
    /// 获取设备名称
    /// </summary>
    /// <returns>设备名称</returns>
    string GetDeviceName();

    /// <summary>
    /// 获取设备类型
    /// </summary>
    /// <returns>设备类型</returns>
    string GetDeviceType();

    /// <summary>
    /// 获取操作系统信息
    /// </summary>
    /// <returns>操作系统信息</returns>
    string GetOperatingSystem();

    /// <summary>
    /// 获取应用程序信息（模拟浏览器信息）
    /// </summary>
    /// <returns>应用程序信息</returns>
    string GetApplicationInfo();

    /// <summary>
    /// 保存设备序列号到本地
    /// </summary>
    /// <param name="fingerprint">设备序列号格式的指纹</param>
    void SaveDeviceFingerprint(string fingerprint);

    /// <summary>
    /// 从本地加载设备序列号
    /// </summary>
    /// <returns>设备序列号格式的指纹，如果不存在则返回null</returns>
    string? LoadDeviceFingerprint();
}
