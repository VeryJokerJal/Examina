using ExaminaWebApplication.Models.FileUpload;
using ExaminaWebApplication.Services.FileUpload;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers.Api;

/// <summary>
/// 文件上传API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// 上传单个文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="description">文件描述</param>
    /// <param name="tags">文件标签</param>
    /// <returns>上传结果</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(524288000)] // 500MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500MB
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string? description = null, [FromForm] string? tags = null)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            FileUploadResult result = await _fileUploadService.UploadFileAsync(file, userId, description, tags);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    message = "文件上传成功",
                    data = new
                    {
                        fileId = result.UploadedFile?.Id,
                        fileName = result.UploadedFile?.OriginalFileName,
                        fileSize = result.UploadedFile?.FileSize,
                        fileUrl = result.FileUrl,
                        uploadedAt = result.UploadedFile?.UploadedAt
                    }
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传API异常");
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 上传多个文件
    /// </summary>
    /// <param name="files">上传的文件列表</param>
    /// <param name="description">文件描述</param>
    /// <param name="tags">文件标签</param>
    /// <returns>上传结果列表</returns>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(524288000)] // 500MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)] // 500MB
    public async Task<IActionResult> UploadFiles(IFormFileCollection files, [FromForm] string? description = null, [FromForm] string? tags = null)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            List<FileUploadResult> results = await _fileUploadService.UploadFilesAsync(files, userId, description, tags);

            List<object> successFiles = [];
            List<object> failedFiles = [];

            foreach (FileUploadResult result in results)
            {
                if (result.IsSuccess)
                {
                    successFiles.Add(new
                    {
                        fileId = result.UploadedFile?.Id,
                        fileName = result.UploadedFile?.OriginalFileName,
                        fileSize = result.UploadedFile?.FileSize,
                        fileUrl = result.FileUrl,
                        uploadedAt = result.UploadedFile?.UploadedAt
                    });
                }
                else
                {
                    failedFiles.Add(new
                    {
                        fileName = result.UploadedFile?.OriginalFileName ?? "未知文件",
                        error = result.ErrorMessage
                    });
                }
            }

            return Ok(new
            {
                success = true,
                message = $"上传完成，成功 {successFiles.Count} 个，失败 {failedFiles.Count} 个",
                data = new
                {
                    successFiles,
                    failedFiles,
                    totalCount = results.Count,
                    successCount = successFiles.Count,
                    failedCount = failedFiles.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多文件上传API异常");
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(int fileId)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            bool success = await _fileUploadService.DeleteFileAsync(fileId, userId);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "文件删除成功"
                });
            }
            else
            {
                return NotFound(new
                {
                    success = false,
                    message = "文件不存在或已被删除"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除API异常: FileId={FileId}", fileId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件信息</returns>
    [HttpGet("{fileId}")]
    public async Task<IActionResult> GetFile(int fileId)
    {
        try
        {
            UploadedFile? file = await _fileUploadService.GetFileAsync(fileId);

            if (file == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "文件不存在"
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    fileId = file.Id,
                    originalFileName = file.OriginalFileName,
                    fileSize = file.FileSize,
                    contentType = file.ContentType,
                    uploadedAt = file.UploadedAt,
                    uploadedBy = file.UploadedBy,
                    uploaderName = file.Uploader?.Username,
                    description = file.Description,
                    tags = file.Tags,
                    downloadCount = file.DownloadCount,
                    downloadUrl = _fileUploadService.GetFileDownloadUrl(file.Id)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息API异常: FileId={FileId}", fileId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取用户文件列表
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>文件列表</returns>
    [HttpGet("my-files")]
    public async Task<IActionResult> GetMyFiles([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 20)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            (List<UploadedFile> files, int totalCount) = await _fileUploadService.GetUserFilesAsync(userId, pageIndex, pageSize);

            var fileList = files.Select(f => new
            {
                fileId = f.Id,
                originalFileName = f.OriginalFileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                uploadedAt = f.UploadedAt,
                description = f.Description,
                tags = f.Tags,
                downloadCount = f.DownloadCount,
                downloadUrl = _fileUploadService.GetFileDownloadUrl(f.Id)
            }).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    files = fileList,
                    totalCount,
                    pageIndex,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户文件列表API异常");
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件流</returns>
    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        try
        {
            UploadedFile? file = await _fileUploadService.GetFileAsync(fileId);

            if (file == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "文件不存在"
                });
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound(new
                {
                    success = false,
                    message = "文件已被移动或删除"
                });
            }

            // 更新下载次数和最后访问时间
            await _fileUploadService.UpdateDownloadStatsAsync(fileId);

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
            return File(fileBytes, file.ContentType, file.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件下载API异常: FileId={FileId}", fileId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 关联文件到考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="purpose">文件用途</param>
    /// <returns>关联结果</returns>
    [HttpPost("associate/exam/{examId}/file/{fileId}")]
    public async Task<IActionResult> AssociateFileToExam(int examId, int fileId, [FromForm] string fileType = "Attachment", [FromForm] string? purpose = null)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            bool success = await _fileUploadService.AssociateFileToExamAsync(examId, fileId, fileType, userId, purpose);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "文件关联成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "文件关联失败，可能已存在关联"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件关联到考试API异常: ExamId={ExamId}, FileId={FileId}", examId, fileId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 关联文件到综合训练
    /// </summary>
    /// <param name="comprehensiveTrainingId">综合训练ID</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="purpose">文件用途</param>
    /// <returns>关联结果</returns>
    [HttpPost("associate/comprehensive-training/{comprehensiveTrainingId}/file/{fileId}")]
    public async Task<IActionResult> AssociateFileToComprehensiveTraining(int comprehensiveTrainingId, int fileId, [FromForm] string fileType = "Attachment", [FromForm] string? purpose = null)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            bool success = await _fileUploadService.AssociateFileToComprehensiveTrainingAsync(comprehensiveTrainingId, fileId, fileType, userId, purpose);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "文件关联成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "文件关联失败，可能已存在关联"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件关联到综合训练API异常: ComprehensiveTrainingId={ComprehensiveTrainingId}, FileId={FileId}", comprehensiveTrainingId, fileId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 关联文件到专项训练
    /// </summary>
    /// <param name="specializedTrainingId">专项训练ID</param>
    /// <param name="fileId">文件ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="purpose">文件用途</param>
    /// <returns>关联结果</returns>
    [HttpPost("associate/specialized-training/{specializedTrainingId}/file/{fileId}")]
    public async Task<IActionResult> AssociateFileToSpecializedTraining(int specializedTrainingId, int fileId, [FromForm] string fileType = "Attachment", [FromForm] string? purpose = null)
    {
        try
        {
            // 暂时使用固定的用户ID，后续可以从认证信息获取
            int userId = 1;

            bool success = await _fileUploadService.AssociateFileToSpecializedTrainingAsync(specializedTrainingId, fileId, fileType, userId, purpose);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "文件关联成功"
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "文件关联失败，可能已存在关联"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件关联到专项训练API异常: SpecializedTrainingId={SpecializedTrainingId}, FileId={FileId}", specializedTrainingId, fileId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取考试关联的文件列表
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>文件列表</returns>
    [HttpGet("exam/{examId}/files")]
    public async Task<IActionResult> GetExamFiles(int examId)
    {
        try
        {
            List<UploadedFile> files = await _fileUploadService.GetExamFilesAsync(examId);

            var fileList = files.Select(f => new
            {
                fileId = f.Id,
                originalFileName = f.OriginalFileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                uploadedAt = f.UploadedAt,
                description = f.Description,
                tags = f.Tags,
                downloadCount = f.DownloadCount,
                downloadUrl = _fileUploadService.GetFileDownloadUrl(f.Id)
            }).ToList();

            return Ok(new
            {
                success = true,
                data = fileList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试文件列表API异常: ExamId={ExamId}", examId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取综合训练关联的文件列表
    /// </summary>
    /// <param name="comprehensiveTrainingId">综合训练ID</param>
    /// <returns>文件列表</returns>
    [HttpGet("comprehensive-training/{comprehensiveTrainingId}/files")]
    public async Task<IActionResult> GetComprehensiveTrainingFiles(int comprehensiveTrainingId)
    {
        try
        {
            List<UploadedFile> files = await _fileUploadService.GetComprehensiveTrainingFilesAsync(comprehensiveTrainingId);

            var fileList = files.Select(f => new
            {
                fileId = f.Id,
                originalFileName = f.OriginalFileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                uploadedAt = f.UploadedAt,
                description = f.Description,
                tags = f.Tags,
                downloadCount = f.DownloadCount,
                downloadUrl = _fileUploadService.GetFileDownloadUrl(f.Id)
            }).ToList();

            return Ok(new
            {
                success = true,
                data = fileList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合训练文件列表API异常: ComprehensiveTrainingId={ComprehensiveTrainingId}", comprehensiveTrainingId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取专项训练关联的文件列表
    /// </summary>
    /// <param name="specializedTrainingId">专项训练ID</param>
    /// <returns>文件列表</returns>
    [HttpGet("specialized-training/{specializedTrainingId}/files")]
    public async Task<IActionResult> GetSpecializedTrainingFiles(int specializedTrainingId)
    {
        try
        {
            List<UploadedFile> files = await _fileUploadService.GetSpecializedTrainingFilesAsync(specializedTrainingId);

            var fileList = files.Select(f => new
            {
                fileId = f.Id,
                originalFileName = f.OriginalFileName,
                fileSize = f.FileSize,
                contentType = f.ContentType,
                uploadedAt = f.UploadedAt,
                description = f.Description,
                tags = f.Tags,
                downloadCount = f.DownloadCount,
                downloadUrl = _fileUploadService.GetFileDownloadUrl(f.Id)
            }).ToList();

            return Ok(new
            {
                success = true,
                data = fileList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练文件列表API异常: SpecializedTrainingId={SpecializedTrainingId}", specializedTrainingId);
            return StatusCode(500, new
            {
                success = false,
                message = "服务器内部错误"
            });
        }
    }
}
