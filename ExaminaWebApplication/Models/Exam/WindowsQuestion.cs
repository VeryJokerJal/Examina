using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// Windows题目模型 - 新的层级结构，一个题目包含多个操作点
/// </summary>
public class WindowsQuestion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的科目ID
    /// </summary>
    [Required]
    public int SubjectId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 题目总分值
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal TotalScore { get; set; } = 10.0m;

    /// <summary>
    /// 题目要求（支持Markdown格式）
    /// </summary>
    [StringLength(2000)]
    public string? Requirements { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的科目
    /// </summary>
    [JsonIgnore]
    public virtual ExamSubject Subject { get; set; } = null!;

    /// <summary>
    /// 包含的操作点列表
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<WindowsQuestionOperationPoint> OperationPoints { get; set; } = [];
}

/// <summary>
/// Windows题目操作点模型 - 具体的操作配置
/// </summary>
public class WindowsQuestionOperationPoint
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 5.0m;

    /// <summary>
    /// 操作配置（JSON格式，存储具体的操作参数）
    /// </summary>
    [Required]
    [Column(TypeName = "json")]
    public string OperationConfig { get; set; } = "{}";

    /// <summary>
    /// 操作点在题目中的顺序
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联的题目
    /// </summary>
    [JsonIgnore]
    public virtual WindowsQuestion Question { get; set; } = null!;
}

/// <summary>
/// Windows题目创建请求模型
/// </summary>
public class CreateWindowsQuestionRequest
{
    /// <summary>
    /// 科目ID
    /// </summary>
    [Required]
    public int SubjectId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 题目要求
    /// </summary>
    [StringLength(2000)]
    public string? Requirements { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    [Required]
    public List<CreateWindowsQuestionOperationPointRequest> OperationPoints { get; set; } = [];
}

/// <summary>
/// Windows题目操作点创建请求模型
/// </summary>
public class CreateWindowsQuestionOperationPointRequest
{
    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    [Required]
    [Range(0.1, 100.0)]
    public decimal Score { get; set; } = 5.0m;

    /// <summary>
    /// 操作配置
    /// </summary>
    [Required]
    public object OperationConfig { get; set; } = new();

    /// <summary>
    /// 操作点顺序
    /// </summary>
    public int OrderIndex { get; set; } = 0;
}

/// <summary>
/// Windows题目响应模型
/// </summary>
public class WindowsQuestionResponse
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 科目ID
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 题目总分值
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 题目要求
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 操作点列表
    /// </summary>
    public List<WindowsQuestionOperationPointResponse> OperationPoints { get; set; } = [];
}

/// <summary>
/// Windows题目操作点响应模型
/// </summary>
public class WindowsQuestionOperationPointResponse
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作点分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 操作配置
    /// </summary>
    public object OperationConfig { get; set; } = new();

    /// <summary>
    /// 自动生成的操作点标题
    /// </summary>
    public string GeneratedTitle { get; set; } = string.Empty;

    /// <summary>
    /// 自动生成的操作点描述
    /// </summary>
    public string GeneratedDescription { get; set; } = string.Empty;

    /// <summary>
    /// 操作点顺序
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
