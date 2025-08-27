using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using Examina.Models.FileDownload;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Examina.Services;

/// <summary>
/// 文件下载服务实现
/// </summary>
public class FileDownloadService : IFileDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileDownloadService> _logger;
    private readonly string _baseDownloadPath;
    private readonly List<string> _supportedCompressionFormats =
    [
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz"
    ];

    public FileDownloadService(HttpClient httpClient, ILogger<FileDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseDownloadPath = @"C:\河北对口计算机";

        // 确保下载目录存在
        _ = Directory.CreateDirectory(_baseDownloadPath);
    }

    public async Task<List<FileDownloadInfo>> GetExamFilesAsync(int examId, FileDownloadTaskType examType)
    {
        try
        {
            string endpoint = examType switch
            {
                FileDownloadTaskType.MockExam => $"/api/fileupload/exam/{examId}/files",
                FileDownloadTaskType.OnlineExam => $"/api/fileupload/exam/{examId}/files",
                _ => throw new ArgumentException($"不支持的考试类型: {examType}")
            };

            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取考试文件列表失败: {StatusCode}", response.StatusCode);
                return [];
            }

            string content = await response.Content.ReadAsStringAsync();
            ApiResponse<List<ExamFileDto>>? apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ExamFileDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            List<ExamFileDto>? fileData = apiResponse?.Data;

            return fileData?.Select(f => new FileDownloadInfo
            {
                FileName = f.OriginalFileName,
                DownloadUrl = f.DownloadUrl,
                TotalSize = f.FileSize,
                IsCompressed = IsCompressedFile(f.OriginalFileName)
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试文件列表时发生错误: ExamId={ExamId}, ExamType={ExamType}", examId, examType);
            return [];
        }
    }

    public async Task<List<FileDownloadInfo>> GetTrainingFilesAsync(int trainingId, FileDownloadTaskType trainingType)
    {
        try
        {
            // 对于MockExam类型，使用映射的ComprehensiveTrainingId
            int actualTrainingId = trainingType == FileDownloadTaskType.MockExam
                ? ViewModels.Pages.MockExamIdMapping.GetComprehensiveTrainingId(trainingId)
                : trainingId;

            string endpoint = trainingType switch
            {
                FileDownloadTaskType.MockExam => $"/api/fileupload/comprehensive-training/{actualTrainingId}/files",
                FileDownloadTaskType.ComprehensiveTraining => $"/api/fileupload/comprehensive-training/{actualTrainingId}/files",
                FileDownloadTaskType.SpecializedTraining => $"/api/fileupload/specialized-training/{actualTrainingId}/files",
                _ => $"/api/fileupload/comprehensive-training/{actualTrainingId}/files"
            };

            HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("获取训练文件列表失败: {StatusCode}", response.StatusCode);
                return [];
            }

            string content = await response.Content.ReadAsStringAsync();
            ApiResponse<List<TrainingFileDto>>? apiResponse = JsonSerializer.Deserialize<ApiResponse<List<TrainingFileDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            List<TrainingFileDto>? fileData = apiResponse?.Data;

            return fileData?.Select(f => new FileDownloadInfo
            {
                FileName = f.OriginalFileName,
                DownloadUrl = f.DownloadUrl,
                TotalSize = f.FileSize,
                IsCompressed = IsCompressedFile(f.OriginalFileName)
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取训练文件列表时发生错误: TrainingId={TrainingId}, TrainingType={TrainingType}", trainingId, trainingType);
            return [];
        }
    }

    public FileDownloadTask CreateDownloadTask(string taskName, FileDownloadTaskType taskType, int relatedId, List<FileDownloadInfo> files)
    {
        FileDownloadTask task = new()
        {
            TaskName = taskName,
            TaskType = taskType,
            RelatedId = relatedId,
            Status = FileDownloadTaskStatus.Pending,
            StatusMessage = "准备下载...",
            CancellationTokenSource = new CancellationTokenSource()
        };

        // 设置文件的本地路径和解压路径
        string downloadDir = GetDownloadDirectory(taskType, relatedId);
        foreach (FileDownloadInfo file in files)
        {
            file.LocalFilePath = Path.Combine(downloadDir, file.FileName);
            if (file.IsCompressed)
            {
                file.ExtractPath = Path.Combine(downloadDir, Path.GetFileNameWithoutExtension(file.FileName));
            }
            task.AddFile(file);
        }

        return task;
    }

    public async Task<bool> StartDownloadTaskAsync(FileDownloadTask task, IProgress<FileDownloadTask>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            task.Status = FileDownloadTaskStatus.Running;
            task.StatusMessage = "开始下载...";
            task.StartTime = DateTime.Now;
            progress?.Report(task);

            // 检查磁盘空间
            long totalSize = task.Files.Sum(f => f.TotalSize);
            string downloadDir = GetDownloadDirectory(task.TaskType, task.RelatedId);
            if (!HasSufficientDiskSpace(totalSize * 2, downloadDir)) // 预留双倍空间用于解压
            {
                task.Status = FileDownloadTaskStatus.Failed;
                task.ErrorMessage = "磁盘空间不足";
                task.StatusMessage = "磁盘空间不足";
                progress?.Report(task);
                return false;
            }

            // 确保下载目录存在
            _ = Directory.CreateDirectory(downloadDir);

            // 下载所有文件
            foreach (FileDownloadInfo file in task.Files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    task.Status = FileDownloadTaskStatus.Cancelled;
                    task.StatusMessage = "下载已取消";
                    progress?.Report(task);
                    return false;
                }

                Progress<FileDownloadInfo> fileProgress = new(f =>
                {
                    task.UpdateOverallProgress();
                    progress?.Report(task);
                });

                bool downloadResult = await DownloadFileAsync(file, fileProgress, cancellationToken);
                if (!downloadResult)
                {
                    task.Status = FileDownloadTaskStatus.Failed;
                    task.ErrorMessage = $"文件下载失败: {file.FileName}";
                    task.StatusMessage = $"文件下载失败: {file.FileName}";
                    progress?.Report(task);
                    return false;
                }

                // 如果是压缩文件，进行解压
                if (file.IsCompressed)
                {
                    bool extractResult = await ExtractFileAsync(file, fileProgress, cancellationToken);
                    if (!extractResult)
                    {
                        task.Status = FileDownloadTaskStatus.Failed;
                        task.ErrorMessage = $"文件解压失败: {file.FileName}";
                        task.StatusMessage = $"文件解压失败: {file.FileName}";
                        progress?.Report(task);
                        return false;
                    }
                }
            }

            task.Status = FileDownloadTaskStatus.Completed;
            task.StatusMessage = "下载完成";
            task.EndTime = DateTime.Now;
            progress?.Report(task);

            _logger.LogInformation("下载任务完成: {TaskName}", task.TaskName);
            return true;
        }
        catch (OperationCanceledException)
        {
            task.Status = FileDownloadTaskStatus.Cancelled;
            task.StatusMessage = "下载已取消";
            task.EndTime = DateTime.Now;
            progress?.Report(task);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载任务失败: {TaskName}", task.TaskName);
            task.Status = FileDownloadTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.StatusMessage = "下载失败";
            task.EndTime = DateTime.Now;
            progress?.Report(task);
            return false;
        }
    }

    public async Task<bool> DownloadFileAsync(FileDownloadInfo fileInfo, IProgress<FileDownloadInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            fileInfo.Status = FileDownloadStatus.Downloading;
            fileInfo.StatusMessage = "正在下载...";
            fileInfo.StartTime = DateTime.Now;
            progress?.Report(fileInfo);

            // 确保目录存在
            string? directory = Path.GetDirectoryName(fileInfo.LocalFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            using HttpResponseMessage response = await _httpClient.GetAsync(fileInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _ = response.EnsureSuccessStatusCode();

            // 获取文件大小
            if (response.Content.Headers.ContentLength.HasValue)
            {
                fileInfo.TotalSize = response.Content.Headers.ContentLength.Value;
            }

            using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using FileStream fileStream = new(fileInfo.LocalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            byte[] buffer = new byte[100 * 1024 * 1024];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                fileInfo.DownloadedSize += bytesRead;
                progress?.Report(fileInfo);
            }

            fileInfo.Status = FileDownloadStatus.Downloaded;
            fileInfo.StatusMessage = "下载完成";
            fileInfo.EndTime = DateTime.Now;
            progress?.Report(fileInfo);

            _logger.LogInformation("文件下载完成: {FileName}", fileInfo.FileName);
            return true;
        }
        catch (OperationCanceledException)
        {
            fileInfo.Status = FileDownloadStatus.Cancelled;
            fileInfo.StatusMessage = "下载已取消";
            fileInfo.EndTime = DateTime.Now;
            progress?.Report(fileInfo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件下载失败: {FileName}", fileInfo.FileName);
            fileInfo.Status = FileDownloadStatus.Failed;
            fileInfo.ErrorMessage = ex.Message;
            fileInfo.StatusMessage = "下载失败";
            fileInfo.EndTime = DateTime.Now;
            progress?.Report(fileInfo);
            return false;
        }
    }

    public string GetDownloadDirectory(FileDownloadTaskType taskType, int relatedId)
    {
        string typeFolder = taskType switch
        {
            FileDownloadTaskType.MockExam => "MockExams",
            FileDownloadTaskType.OnlineExam => "OnlineExams",
            FileDownloadTaskType.ComprehensiveTraining => "ComprehensiveTraining",
            FileDownloadTaskType.SpecializedTraining => "SpecializedTraining",
            _ => "Unknown"
        };

        return Path.Combine(_baseDownloadPath, typeFolder, relatedId.ToString());
    }

    public bool HasSufficientDiskSpace(long requiredSpace, string downloadPath)
    {
        try
        {
            DriveInfo drive = new(Path.GetPathRoot(downloadPath) ?? "C:");
            return drive.AvailableFreeSpace > requiredSpace;
        }
        catch
        {
            return false;
        }
    }

    public List<string> GetSupportedCompressionFormats()
    {
        return [.. _supportedCompressionFormats];
    }

    public bool IsCompressedFile(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _supportedCompressionFormats.Contains(extension);
    }

    public async Task<bool> ExtractFileAsync(FileDownloadInfo fileInfo, IProgress<FileDownloadInfo>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            fileInfo.Status = FileDownloadStatus.Extracting;
            fileInfo.StatusMessage = "正在解压...";
            progress?.Report(fileInfo);

            // 确保解压目录存在
            _ = Directory.CreateDirectory(fileInfo.ExtractPath);

            string extension = Path.GetExtension(fileInfo.LocalFilePath).ToLowerInvariant();

            if (extension == ".zip")
            {
                await ExtractZipFileAsync(fileInfo.LocalFilePath, fileInfo.ExtractPath, cancellationToken);
            }
            else
            {
                // 使用SharpCompress处理其他格式
                await ExtractWithSharpCompressAsync(fileInfo.LocalFilePath, fileInfo.ExtractPath, cancellationToken);
            }

            fileInfo.Status = FileDownloadStatus.Completed;
            fileInfo.StatusMessage = "解压完成";
            progress?.Report(fileInfo);

            _logger.LogInformation("文件解压完成: {FileName}", fileInfo.FileName);
            return true;
        }
        catch (OperationCanceledException)
        {
            fileInfo.Status = FileDownloadStatus.Cancelled;
            fileInfo.StatusMessage = "解压已取消";
            progress?.Report(fileInfo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件解压失败: {FileName}", fileInfo.FileName);
            fileInfo.Status = FileDownloadStatus.Failed;
            fileInfo.ErrorMessage = ex.Message;
            fileInfo.StatusMessage = "解压失败";
            progress?.Report(fileInfo);
            return false;
        }
    }

    private async Task ExtractZipFileAsync(string zipPath, string extractPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using ZipArchive archive = ZipFile.OpenRead(zipPath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                string destinationPath = Path.Combine(extractPath, entry.FullName);
                string? destinationDir = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(destinationDir))
                {
                    _ = Directory.CreateDirectory(destinationDir);
                }

                entry.ExtractToFile(destinationPath, true);
            }
        }, cancellationToken);
    }

    private async Task ExtractWithSharpCompressAsync(string archivePath, string extractPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using IArchive archive = ArchiveFactory.Open(archivePath);
            foreach (IArchiveEntry? entry in archive.Entries.Where(entry => !entry.IsDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                entry.WriteToDirectory(extractPath, new ExtractionOptions()
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
        }, cancellationToken);
    }

    public void CancelDownloadTask(FileDownloadTask task)
    {
        try
        {
            task.CancellationTokenSource?.Cancel();
            task.Status = FileDownloadTaskStatus.Cancelled;
            task.StatusMessage = "下载已取消";
            task.EndTime = DateTime.Now;

            _logger.LogInformation("下载任务已取消: {TaskName}", task.TaskName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消下载任务时发生错误: {TaskName}", task.TaskName);
        }
    }

    public async Task<bool> RetryDownloadTaskAsync(FileDownloadTask task, IProgress<FileDownloadTask>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 重置任务状态
            task.Status = FileDownloadTaskStatus.Pending;
            task.StatusMessage = "准备重试...";
            task.ErrorMessage = null;
            task.CancellationTokenSource = new CancellationTokenSource();

            // 重置文件状态
            foreach (FileDownloadInfo file in task.Files)
            {
                if (file.Status is FileDownloadStatus.Failed or FileDownloadStatus.Cancelled)
                {
                    file.Status = FileDownloadStatus.Pending;
                    file.StatusMessage = "等待下载...";
                    file.ErrorMessage = null;
                    file.DownloadedSize = 0;
                    file.StartTime = null;
                    file.EndTime = null;
                }
            }

            return await StartDownloadTaskAsync(task, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重试下载任务失败: {TaskName}", task.TaskName);
            return false;
        }
    }

    public async Task<bool> ValidateFileIntegrityAsync(string filePath, string? expectedHash = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            // 如果没有提供期望的哈希值，只检查文件是否存在且可读
            if (string.IsNullOrEmpty(expectedHash))
            {
                using FileStream readStream = File.OpenRead(filePath);
                return readStream.CanRead;
            }

            // 计算文件的MD5哈希值
            using MD5 md5 = MD5.Create();
            using FileStream stream = File.OpenRead(filePath);
            byte[] hash = await Task.Run(() => md5.ComputeHash(stream));
            string hashString = Convert.ToHexString(hash);

            return string.Equals(hashString, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证文件完整性失败: {FilePath}", filePath);
            return false;
        }
    }

    public async Task CleanupTempFilesAsync(FileDownloadTask task)
    {
        try
        {
            await Task.Run(() =>
            {
                foreach (FileDownloadInfo file in task.Files)
                {
                    // 删除下载的压缩文件（保留解压后的文件）
                    if (file.IsCompressed && File.Exists(file.LocalFilePath))
                    {
                        try
                        {
                            File.Delete(file.LocalFilePath);
                            _logger.LogDebug("已删除临时文件: {FilePath}", file.LocalFilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "删除临时文件失败: {FilePath}", file.LocalFilePath);
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理临时文件失败: {TaskName}", task.TaskName);
        }
    }
}

/// <summary>
/// API响应包装类
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// 考试文件DTO
/// </summary>
public class ExamFileDto
{
    public int FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public int DownloadCount { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

/// <summary>
/// 训练文件DTO
/// </summary>
public class TrainingFileDto
{
    public int FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public int DownloadCount { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}
