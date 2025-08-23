using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.FileUpload;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace ExaminaWebApplication.Services.FileUpload;

/// <summary>
/// 文件上传服务实现
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly FileUploadConfiguration _config;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IWebHostEnvironment _environment;

    public FileUploadService(
        ApplicationDbContext context,
        IOptions<FileUploadConfiguration> config,
        ILogger<FileUploadService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _config = config.Value;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 上传单个文件
    /// </summary>
    public async Task<FileUploadResult> UploadFileAsync(IFormFile file, int uploadedBy, string? description = null, string? tags = null)
    {
        FileUploadResult result = new();

        try
        {
            // 验证文件
            FileValidationResult validation = ValidateFile(file);
            if (!validation.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = validation.ErrorMessage;
                return result;
            }

            // 生成唯一文件名
            string uniqueFileName = GenerateUniqueFileName(file.FileName);
            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // 确保上传目录存在
            string uploadPath = Path.Combine(_environment.WebRootPath, _config.UploadPath.Replace("wwwroot/", ""));
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string filePath = Path.Combine(uploadPath, uniqueFileName);

            // 计算文件哈希值
            string fileHash;
            using (Stream fileStream = file.OpenReadStream())
            {
                fileHash = await ComputeFileHashAsync(fileStream);
            }

            // 检查是否已存在相同哈希的文件
            UploadedFile? existingFile = await _context.UploadedFiles
                .FirstOrDefaultAsync(f => f.FileHash == fileHash && !f.IsDeleted);

            if (existingFile != null)
            {
                _logger.LogInformation("文件已存在，返回现有文件信息: {FileHash}", fileHash);
                result.IsSuccess = true;
                result.UploadedFile = existingFile;
                result.FileUrl = GetFileDownloadUrl(existingFile.Id);
                result.Progress = 100;
                return result;
            }

            // 保存文件到磁盘
            using (FileStream stream = new(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 创建数据库记录
            UploadedFile uploadedFile = new()
            {
                OriginalFileName = file.FileName,
                StoredFileName = uniqueFileName,
                FileExtension = fileExtension,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FilePath = filePath,
                FileHash = fileHash,
                UploadStatus = UploadStatus.Completed,
                UploadProgress = 100,
                Description = description,
                Tags = tags,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow
            };

            _context.UploadedFiles.Add(uploadedFile);
            await _context.SaveChangesAsync();

            result.IsSuccess = true;
            result.UploadedFile = uploadedFile;
            result.FileUrl = GetFileDownloadUrl(uploadedFile.Id);
            result.Progress = 100;

            _logger.LogInformation("文件上传成功: {FileName} -> {StoredFileName}", file.FileName, uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败: {FileName}", file.FileName);
            result.IsSuccess = false;
            result.ErrorMessage = $"文件上传失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 上传多个文件
    /// </summary>
    public async Task<List<FileUploadResult>> UploadFilesAsync(IFormFileCollection files, int uploadedBy, string? description = null, string? tags = null)
    {
        List<FileUploadResult> results = [];

        foreach (IFormFile file in files)
        {
            FileUploadResult result = await UploadFileAsync(file, uploadedBy, description, tags);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 验证文件
    /// </summary>
    public FileValidationResult ValidateFile(IFormFile file)
    {
        FileValidationResult result = new() { IsValid = true };

        if (file == null || file.Length == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = "文件不能为空";
            return result;
        }

        // 检查文件大小
        if (file.Length > _config.MaxFileSize)
        {
            result.IsValid = false;
            result.ErrorMessage = $"文件大小不能超过 {_config.MaxFileSize / (1024 * 1024)} MB";
            return result;
        }

        // 检查文件扩展名
        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_config.AllowedExtensions.Contains(fileExtension))
        {
            result.IsValid = false;
            result.ErrorMessage = $"不支持的文件类型: {fileExtension}";
            return result;
        }

        // 检查MIME类型
        if (!_config.AllowedMimeTypes.Contains(file.ContentType))
        {
            result.WarningMessage = $"文件MIME类型可能不正确: {file.ContentType}";
        }

        return result;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public async Task<bool> DeleteFileAsync(int fileId, int deletedBy)
    {
        try
        {
            UploadedFile? file = await _context.UploadedFiles.FindAsync(fileId);
            if (file == null || file.IsDeleted)
            {
                return false;
            }

            // 软删除
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            file.DeletedBy = deletedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("文件删除成功: {FileId}", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除失败: {FileId}", fileId);
            return false;
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    public async Task<UploadedFile?> GetFileAsync(int fileId)
    {
        return await _context.UploadedFiles
            .Include(f => f.Uploader)
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
    }

    /// <summary>
    /// 获取用户上传的文件列表
    /// </summary>
    public async Task<(List<UploadedFile> Files, int TotalCount)> GetUserFilesAsync(int userId, int pageIndex = 0, int pageSize = 20)
    {
        IQueryable<UploadedFile> query = _context.UploadedFiles
            .Where(f => f.UploadedBy == userId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt);

        int totalCount = await query.CountAsync();
        List<UploadedFile> files = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Include(f => f.Uploader)
            .ToListAsync();

        return (files, totalCount);
    }

    /// <summary>
    /// 计算文件哈希值
    /// </summary>
    public async Task<string> ComputeFileHashAsync(Stream stream)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// 生成唯一文件名
    /// </summary>
    public string GenerateUniqueFileName(string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        string fileName = Path.GetFileNameWithoutExtension(originalFileName);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string guid = Guid.NewGuid().ToString("N")[..8];
        return $"{fileName}_{timestamp}_{guid}{extension}";
    }

    /// <summary>
    /// 获取文件下载URL
    /// </summary>
    public string GetFileDownloadUrl(int fileId)
    {
        return $"/api/fileupload/download/{fileId}";
    }

    /// <summary>
    /// 关联文件到考试
    /// </summary>
    public async Task<bool> AssociateFileToExamAsync(int examId, int fileId, string fileType, int createdBy, string? purpose = null)
    {
        try
        {
            // 检查是否已存在关联
            bool exists = await _context.ExamFileAssociations
                .AnyAsync(a => a.ExamId == examId && a.FileId == fileId);

            if (exists)
            {
                return false;
            }

            ExamFileAssociation association = new()
            {
                ExamId = examId,
                FileId = fileId,
                FileType = fileType,
                Purpose = purpose,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.ExamFileAssociations.Add(association);
            await _context.SaveChangesAsync();

            _logger.LogInformation("文件关联到考试成功: ExamId={ExamId}, FileId={FileId}", examId, fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件关联到考试失败: ExamId={ExamId}, FileId={FileId}", examId, fileId);
            return false;
        }
    }

    /// <summary>
    /// 关联文件到综合训练
    /// </summary>
    public async Task<bool> AssociateFileToComprehensiveTrainingAsync(int comprehensiveTrainingId, int fileId, string fileType, int createdBy, string? purpose = null)
    {
        try
        {
            // 检查是否已存在关联
            bool exists = await _context.ComprehensiveTrainingFileAssociations
                .AnyAsync(a => a.ComprehensiveTrainingId == comprehensiveTrainingId && a.FileId == fileId);

            if (exists)
            {
                return false;
            }

            ComprehensiveTrainingFileAssociation association = new()
            {
                ComprehensiveTrainingId = comprehensiveTrainingId,
                FileId = fileId,
                FileType = fileType,
                Purpose = purpose,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComprehensiveTrainingFileAssociations.Add(association);
            await _context.SaveChangesAsync();

            _logger.LogInformation("文件关联到综合训练成功: ComprehensiveTrainingId={ComprehensiveTrainingId}, FileId={FileId}", comprehensiveTrainingId, fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件关联到综合训练失败: ComprehensiveTrainingId={ComprehensiveTrainingId}, FileId={FileId}", comprehensiveTrainingId, fileId);
            return false;
        }
    }

    /// <summary>
    /// 关联文件到专项训练
    /// </summary>
    public async Task<bool> AssociateFileToSpecializedTrainingAsync(int specializedTrainingId, int fileId, string fileType, int createdBy, string? purpose = null)
    {
        try
        {
            // 检查是否已存在关联
            bool exists = await _context.SpecializedTrainingFileAssociations
                .AnyAsync(a => a.SpecializedTrainingId == specializedTrainingId && a.FileId == fileId);

            if (exists)
            {
                return false;
            }

            SpecializedTrainingFileAssociation association = new()
            {
                SpecializedTrainingId = specializedTrainingId,
                FileId = fileId,
                FileType = fileType,
                Purpose = purpose,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.SpecializedTrainingFileAssociations.Add(association);
            await _context.SaveChangesAsync();

            _logger.LogInformation("文件关联到专项训练成功: SpecializedTrainingId={SpecializedTrainingId}, FileId={FileId}", specializedTrainingId, fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件关联到专项训练失败: SpecializedTrainingId={SpecializedTrainingId}, FileId={FileId}", specializedTrainingId, fileId);
            return false;
        }
    }

    /// <summary>
    /// 获取考试关联的文件列表
    /// </summary>
    public async Task<List<UploadedFile>> GetExamFilesAsync(int examId)
    {
        return await _context.ExamFileAssociations
            .Where(a => a.ExamId == examId)
            .Include(a => a.File)
            .Where(a => a.File != null && !a.File.IsDeleted)
            .Select(a => a.File!)
            .ToListAsync();
    }

    /// <summary>
    /// 获取综合训练关联的文件列表
    /// </summary>
    public async Task<List<UploadedFile>> GetComprehensiveTrainingFilesAsync(int comprehensiveTrainingId)
    {
        return await _context.ComprehensiveTrainingFileAssociations
            .Where(a => a.ComprehensiveTrainingId == comprehensiveTrainingId)
            .Include(a => a.File)
            .Where(a => a.File != null && !a.File.IsDeleted)
            .Select(a => a.File!)
            .ToListAsync();
    }

    /// <summary>
    /// 获取专项训练关联的文件列表
    /// </summary>
    public async Task<List<UploadedFile>> GetSpecializedTrainingFilesAsync(int specializedTrainingId)
    {
        return await _context.SpecializedTrainingFileAssociations
            .Where(a => a.SpecializedTrainingId == specializedTrainingId)
            .Include(a => a.File)
            .Where(a => a.File != null && !a.File.IsDeleted)
            .Select(a => a.File!)
            .ToListAsync();
    }

    /// <summary>
    /// 更新文件下载统计
    /// </summary>
    public async Task<bool> UpdateDownloadStatsAsync(int fileId)
    {
        try
        {
            UploadedFile? file = await _context.UploadedFiles.FindAsync(fileId);
            if (file == null || file.IsDeleted)
            {
                return false;
            }

            file.DownloadCount++;
            file.LastAccessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新文件下载统计失败: {FileId}", fileId);
            return false;
        }
    }
}
