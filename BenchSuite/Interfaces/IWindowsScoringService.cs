using BenchSuite.Models;

namespace BenchSuite.Interfaces;

/// <summary>
/// Windows打分服务接口
/// </summary>
public interface IWindowsScoringService : IScoringService
{
    /// <summary>
    /// 设置基础路径，用于解析相对路径
    /// </summary>
    /// <param name="basePath">基础路径</param>
    void SetBasePath(string? basePath);

    /// <summary>
    /// 检测Windows系统中的特定知识点
    /// </summary>
    /// <param name="knowledgePointType">知识点类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>知识点检测结果</returns>
    Task<KnowledgePointResult> DetectKnowledgePointAsync(string knowledgePointType, Dictionary<string, string> parameters);

    /// <summary>
    /// 批量检测Windows系统中的知识点
    /// </summary>
    /// <param name="knowledgePoints">要检测的知识点列表</param>
    /// <returns>知识点检测结果列表</returns>
    Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(List<OperationPointModel> knowledgePoints);

    /// <summary>
    /// 验证文件是否存在
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件是否存在</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// 验证文件夹是否存在
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <returns>文件夹是否存在</returns>
    bool DirectoryExists(string folderPath);

    /// <summary>
    /// 获取文件属性
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件属性信息</returns>
    FileAttributes? GetFileAttributes(string filePath);

    /// <summary>
    /// 验证注册表项是否存在
    /// </summary>
    /// <param name="rootKey">根键</param>
    /// <param name="keyPath">注册表项路径</param>
    /// <returns>注册表项是否存在</returns>
    bool RegistryKeyExists(string rootKey, string keyPath);

    /// <summary>
    /// 获取注册表值
    /// </summary>
    /// <param name="rootKey">根键</param>
    /// <param name="keyPath">注册表项路径</param>
    /// <param name="valueName">值名称</param>
    /// <returns>注册表值</returns>
    object? GetRegistryValue(string rootKey, string keyPath, string valueName);

    /// <summary>
    /// 验证服务是否存在
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务是否存在</returns>
    bool ServiceExists(string serviceName);

    /// <summary>
    /// 获取服务状态
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务状态</returns>
    string? GetServiceStatus(string serviceName);

    /// <summary>
    /// 验证进程是否正在运行
    /// </summary>
    /// <param name="processName">进程名称</param>
    /// <returns>进程是否正在运行</returns>
    bool ProcessIsRunning(string processName);

    /// <summary>
    /// 验证网络连通性
    /// </summary>
    /// <param name="hostName">主机名或IP地址</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <returns>网络是否连通</returns>
    Task<bool> PingHostAsync(string hostName, int timeout = 5000);
}
