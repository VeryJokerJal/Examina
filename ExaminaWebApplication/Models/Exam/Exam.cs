using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 试卷主表 - 存储试卷的基本信息
/// </summary>
public class Exam
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型（统考、模拟考试等）
    /// </summary>
    [Required]
    public ExamType ExamType { get; set; } = ExamType.UnifiedExam;

    /// <summary>
    /// 试卷状态
    /// </summary>
    [Required]
    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    /// <summary>
    /// 总分
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(6,2)")]
    public decimal TotalScore { get; set; } = 100.0m;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Required]
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    [Column(TypeName = "decimal(6,2)")]
    public decimal PassingScore { get; set; } = 60.0m;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 发布者ID
    /// </summary>
    public int? PublishedBy { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 试卷标签（用于分类和搜索）
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置（JSON格式）
    /// </summary>
    [Column(TypeName = "json")]
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 创建者用户
    /// </summary>
    [JsonIgnore]
    public virtual User Creator { get; set; } = null!;

    /// <summary>
    /// 发布者用户
    /// </summary>
    [JsonIgnore]
    public virtual User? Publisher { get; set; }

    /// <summary>
    /// 试卷科目列表
    /// </summary>
    public virtual ICollection<ExamSubject> Subjects { get; set; } = new List<ExamSubject>();

    /// <summary>
    /// 试卷题目列表
    /// </summary>
    public virtual ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();

    /// <summary>
    /// 试卷Excel操作点列表
    /// </summary>
    public virtual ICollection<ExamExcelOperationPoint> ExcelOperationPoints { get; set; } = new List<ExamExcelOperationPoint>();
}

/// <summary>
/// 试卷类型枚举
/// </summary>
public enum ExamType
{
    /// <summary>
    /// 统一考试
    /// </summary>
    UnifiedExam = 1,

    /// <summary>
    /// 模拟考试
    /// </summary>
    MockExam = 2
}

/// <summary>
/// 试卷状态枚举
/// </summary>
public enum ExamStatus
{
    /// <summary>
    /// 草稿状态
    /// </summary>
    Draft = 1,

    /// <summary>
    /// 审核中
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 3,

    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 4,

    /// <summary>
    /// 已结束
    /// </summary>
    Completed = 5,

    /// <summary>
    /// 已暂停
    /// </summary>
    Suspended = 6,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 7,

    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 8
}
