namespace ExaminaWebApplication.Models.FileUpload;

/// <summary>
/// 文件上传配置
/// </summary>
public class FileUploadConfiguration
{
    /// <summary>
    /// 最大文件大小（字节）
    /// </summary>
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// 最大同时上传文件数量
    /// </summary>
    public int MaxFileCount { get; set; } = 10;

    /// <summary>
    /// 允许的文件扩展名
    /// </summary>
    public string[] AllowedExtensions { get; set; } = 
    {
        ".zip", ".rar", ".7z",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".rtf", ".json", ".xml",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp",
        ".mp4", ".avi", ".mov", ".wmv",
        ".mp3", ".wav", ".wma"
    };

    /// <summary>
    /// 允许的MIME类型
    /// </summary>
    public string[] AllowedMimeTypes { get; set; } = 
    {
        "application/zip",
        "application/x-rar-compressed",
        "application/x-7z-compressed",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "application/rtf",
        "application/json",
        "application/xml",
        "text/xml",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "video/mp4",
        "video/x-msvideo",
        "video/quicktime",
        "video/x-ms-wmv",
        "audio/mpeg",
        "audio/wav",
        "audio/x-ms-wma"
    };

    /// <summary>
    /// 上传目录路径
    /// </summary>
    public string UploadPath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// 临时上传目录路径
    /// </summary>
    public string TempUploadPath { get; set; } = "wwwroot/uploads/temp";

    /// <summary>
    /// 是否启用文件哈希验证
    /// </summary>
    public bool EnableHashValidation { get; set; } = true;

    /// <summary>
    /// 是否启用病毒扫描
    /// </summary>
    public bool EnableVirusScanning { get; set; } = false;

    /// <summary>
    /// 文件保留天数（0表示永久保留）
    /// </summary>
    public int FileRetentionDays { get; set; } = 0;

    /// <summary>
    /// 是否启用文件压缩
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// 压缩质量（1-100）
    /// </summary>
    public int CompressionQuality { get; set; } = 85;
}

/// <summary>
/// 文件验证结果
/// </summary>
public class FileValidationResult
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 警告信息
    /// </summary>
    public string? WarningMessage { get; set; }
}

/// <summary>
/// 文件上传结果
/// </summary>
public class FileUploadResult
{
    /// <summary>
    /// 是否上传成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 上传的文件信息
    /// </summary>
    public UploadedFile? UploadedFile { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 文件URL
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// 上传进度
    /// </summary>
    public int Progress { get; set; }
}
