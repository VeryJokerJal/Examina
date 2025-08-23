using System.Threading;
using Examina.Models.FileDownload;

namespace Examina.Services;

/// <summary>
/// 文件下载服务接口
/// </summary>
public interface IFileDownloadService
{
    /// <summary>
    /// 获取考试相关文件列表
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="examType">考试类型</param>
    /// <returns>文件信息列表</returns>
    Task<List<FileDownloadInfo>> GetExamFilesAsync(int examId, FileDownloadTaskType examType);

    /// <summary>
    /// 获取训练相关文件列表
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    /// <param name="trainingType">训练类型</param>
    /// <returns>文件信息列表</returns>
    Task<List<FileDownloadInfo>> GetTrainingFilesAsync(int trainingId, FileDownloadTaskType trainingType);

    /// <summary>
    /// 创建下载任务
    /// </summary>
    /// <param name="taskName">任务名称</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    /// <param name="files">文件列表</param>
    /// <returns>下载任务</returns>
    FileDownloadTask CreateDownloadTask(string taskName, FileDownloadTaskType taskType, int relatedId, List<FileDownloadInfo> files);

    /// <summary>
    /// 开始下载任务
    /// </summary>
    /// <param name="task">下载任务</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载结果</returns>
    Task<bool> StartDownloadTaskAsync(FileDownloadTask task, IProgress<FileDownloadTask>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载单个文件
    /// </summary>
    /// <param name="fileInfo">文件信息</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载结果</returns>
    Task<bool> DownloadFileAsync(FileDownloadInfo fileInfo, IProgress<FileDownloadInfo>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解压文件
    /// </summary>
    /// <param name="fileInfo">文件信息</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解压结果</returns>
    Task<bool> ExtractFileAsync(FileDownloadInfo fileInfo, IProgress<FileDownloadInfo>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消下载任务
    /// </summary>
    /// <param name="task">下载任务</param>
    void CancelDownloadTask(FileDownloadTask task);

    /// <summary>
    /// 重试下载任务
    /// </summary>
    /// <param name="task">下载任务</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重试结果</returns>
    Task<bool> RetryDownloadTaskAsync(FileDownloadTask task, IProgress<FileDownloadTask>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证文件完整性
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="expectedHash">期望的哈希值（可选）</param>
    /// <returns>验证结果</returns>
    Task<bool> ValidateFileIntegrityAsync(string filePath, string? expectedHash = null);

    /// <summary>
    /// 清理临时文件
    /// </summary>
    /// <param name="task">下载任务</param>
    Task CleanupTempFilesAsync(FileDownloadTask task);

    /// <summary>
    /// 获取下载目录
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    /// <returns>下载目录路径</returns>
    string GetDownloadDirectory(FileDownloadTaskType taskType, int relatedId);

    /// <summary>
    /// 检查磁盘空间是否足够
    /// </summary>
    /// <param name="requiredSpace">需要的空间（字节）</param>
    /// <param name="downloadPath">下载路径</param>
    /// <returns>是否有足够空间</returns>
    bool HasSufficientDiskSpace(long requiredSpace, string downloadPath);

    /// <summary>
    /// 获取支持的压缩格式
    /// </summary>
    /// <returns>支持的文件扩展名列表</returns>
    List<string> GetSupportedCompressionFormats();

    /// <summary>
    /// 检查文件是否为压缩格式
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>是否为压缩格式</returns>
    bool IsCompressedFile(string fileName);
}

/// <summary>
/// 文件下载事件参数
/// </summary>
public class FileDownloadEventArgs : EventArgs
{
    public FileDownloadTask Task { get; }
    public FileDownloadInfo? File { get; }
    public string Message { get; }

    public FileDownloadEventArgs(FileDownloadTask task, string message, FileDownloadInfo? file = null)
    {
        Task = task;
        File = file;
        Message = message;
    }
}

/// <summary>
/// 文件下载异常
/// </summary>
public class FileDownloadException : Exception
{
    public string? FileName { get; }
    public string? DownloadUrl { get; }

    public FileDownloadException(string message) : base(message)
    {
    }

    public FileDownloadException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public FileDownloadException(string message, string? fileName, string? downloadUrl) : base(message)
    {
        FileName = fileName;
        DownloadUrl = downloadUrl;
    }

    public FileDownloadException(string message, string? fileName, string? downloadUrl, Exception innerException)
        : base(message, innerException)
    {
        FileName = fileName;
        DownloadUrl = downloadUrl;
    }
}
