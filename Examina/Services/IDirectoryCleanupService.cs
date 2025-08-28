using System;
using System.Threading.Tasks;

namespace Examina.Services;

/// <summary>
/// 目录清理服务接口
/// </summary>
public interface IDirectoryCleanupService
{
    /// <summary>
    /// 清理指定目录下的所有文件和文件夹
    /// </summary>
    /// <param name="directoryPath">要清理的目录路径</param>
    /// <returns>清理结果</returns>
    Task<DirectoryCleanupResult> CleanupDirectoryAsync(string directoryPath);

    /// <summary>
    /// 清理考试/训练目录（默认为'C:\河北对口计算机'）
    /// </summary>
    /// <returns>清理结果</returns>
    Task<DirectoryCleanupResult> CleanupExamDirectoryAsync();

    /// <summary>
    /// 检查目录是否存在且可访问
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <returns>是否可访问</returns>
    bool IsDirectoryAccessible(string directoryPath);
}

/// <summary>
/// 目录清理结果
/// </summary>
public class DirectoryCleanupResult
{
    /// <summary>
    /// 清理是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 清理的文件数量
    /// </summary>
    public int DeletedFileCount { get; set; }

    /// <summary>
    /// 清理的文件夹数量
    /// </summary>
    public int DeletedDirectoryCount { get; set; }

    /// <summary>
    /// 跳过的文件数量（由于占用或权限问题）
    /// </summary>
    public int SkippedFileCount { get; set; }

    /// <summary>
    /// 跳过的文件夹数量（由于占用或权限问题）
    /// </summary>
    public int SkippedDirectoryCount { get; set; }

    /// <summary>
    /// 清理耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 详细的错误信息列表
    /// </summary>
    public List<string> DetailedErrors { get; set; } = new();

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static DirectoryCleanupResult Success(int deletedFiles, int deletedDirectories, long elapsedMs)
    {
        return new DirectoryCleanupResult
        {
            IsSuccess = true,
            DeletedFileCount = deletedFiles,
            DeletedDirectoryCount = deletedDirectories,
            ElapsedMilliseconds = elapsedMs
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static DirectoryCleanupResult Failure(string errorMessage, List<string>? detailedErrors = null)
    {
        return new DirectoryCleanupResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            DetailedErrors = detailedErrors ?? new List<string>()
        };
    }
}
