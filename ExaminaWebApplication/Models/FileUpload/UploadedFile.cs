using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.FileUpload;

/// <summary>
/// 上传文件实体
/// </summary>
public class UploadedFile
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文件名（系统生成的唯一文件名）
    /// </summary>
    [Required]
    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [Required]
    [StringLength(10)]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// 文件MIME类型
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件存储路径
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件哈希值（用于去重和完整性验证）
    /// </summary>
    [StringLength(64)]
    public string? FileHash { get; set; }

    /// <summary>
    /// 上传状态
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UploadStatus { get; set; } = "Uploading";

    /// <summary>
    /// 上传进度（0-100）
    /// </summary>
    public int UploadProgress { get; set; } = 0;

    /// <summary>
    /// 上传错误信息
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 文件描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 文件标签
    /// </summary>
    [StringLength(200)]
    public string? Tags { get; set; }

    /// <summary>
    /// 是否为公开文件
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 上传者ID
    /// </summary>
    [Required]
    public int UploadedBy { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// 是否已删除（软删除）
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 删除者ID
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// 上传者用户
    /// </summary>
    [ForeignKey(nameof(UploadedBy))]
    public virtual User? Uploader { get; set; }

    /// <summary>
    /// 删除者用户
    /// </summary>
    [ForeignKey(nameof(DeletedBy))]
    public virtual User? Deleter { get; set; }

    /// <summary>
    /// 关联的考试文件
    /// </summary>
    public virtual ICollection<ExamFileAssociation> ExamAssociations { get; set; } = [];

    /// <summary>
    /// 关联的综合训练文件
    /// </summary>
    public virtual ICollection<ComprehensiveTrainingFileAssociation> ComprehensiveTrainingAssociations { get; set; } = [];

    /// <summary>
    /// 关联的专项训练文件
    /// </summary>
    public virtual ICollection<SpecializedTrainingFileAssociation> SpecializedTrainingAssociations { get; set; } = [];
}

/// <summary>
/// 上传状态枚举
/// </summary>
public static class UploadStatus
{
    public const string Uploading = "Uploading";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Processing = "Processing";
    public const string Cancelled = "Cancelled";
}
