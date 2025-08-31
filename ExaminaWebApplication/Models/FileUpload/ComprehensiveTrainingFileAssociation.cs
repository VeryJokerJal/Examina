using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Models.FileUpload;

/// <summary>
/// 综合训练文件关联实体
/// </summary>
public class ComprehensiveTrainingFileAssociation
{
    /// <summary>
    /// 关联ID
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 综合训练ID
    /// </summary>
    [Required]
    public int ComprehensiveTrainingId { get; set; }

    /// <summary>
    /// 文件ID
    /// </summary>
    [Required]
    public int FileId { get; set; }

    /// <summary>
    /// 文件类型（主文件、附件、参考资料等）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FileType { get; set; } = "Attachment";

    /// <summary>
    /// 文件用途描述
    /// </summary>
    [StringLength(200)]
    public string? Purpose { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否必需文件
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 关联创建时间
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联创建者ID
    /// </summary>
    [Required]
    public int CreatedBy { get; set; }

    /// <summary>
    /// 关联的综合训练
    /// </summary>
    [ForeignKey(nameof(ComprehensiveTrainingId))]
    public virtual ImportedComprehensiveTraining.ImportedComprehensiveTraining? ComprehensiveTraining { get; set; }

    /// <summary>
    /// 关联的文件
    /// </summary>
    [ForeignKey(nameof(FileId))]
    public virtual UploadedFile? File { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }
}

/// <summary>
/// 综合训练文件类型枚举
/// </summary>
public static class ComprehensiveTrainingFileType
{
    public const string MainFile = "MainFile";
    public const string Attachment = "Attachment";
    public const string Reference = "Reference";
    public const string Instruction = "Instruction";
    public const string Template = "Template";
    public const string Resource = "Resource";
    public const string TrainingMaterial = "TrainingMaterial";
}
