using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 试卷科目表 - 存储试卷中各科目的配置信息
/// </summary>
public class ExamSubject
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的试卷ID
    /// </summary>
    [Required]
    public int ExamId { get; set; }

    /// <summary>
    /// 科目类型
    /// </summary>
    [Required]
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// 科目名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 科目分值
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 20.0m;

    /// <summary>
    /// 科目考试时长（分钟）
    /// </summary>
    [Required]
    public int DurationMinutes { get; set; } = 30;



    /// <summary>
    /// 科目顺序
    /// </summary>
    [Required]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// 是否必考科目
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最低分数要求
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MinScore { get; set; }

    /// <summary>
    /// 科目权重（用于总分计算）
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// 科目配置（JSON格式，存储特定科目的配置）
    /// </summary>
    [Column(TypeName = "json")]
    public string? SubjectConfig { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 关联的试卷
    /// </summary>
    [JsonIgnore]
    public virtual Exam Exam { get; set; } = null!;

    /// <summary>
    /// 科目下的题目列表
    /// </summary>
    public virtual ICollection<ExamQuestion> Questions { get; set; } = [];

    /// <summary>
    /// 科目关联的操作点列表
    /// </summary>
    public virtual ICollection<ExamSubjectOperationPoint> OperationPoints { get; set; } = [];
}

/// <summary>
/// 科目类型枚举
/// </summary>
public enum SubjectType
{
    /// <summary>
    /// Excel科目
    /// </summary>
    Excel = 1,

    /// <summary>
    /// PowerPoint科目
    /// </summary>
    PowerPoint = 2,

    /// <summary>
    /// Word科目
    /// </summary>
    Word = 3,

    /// <summary>
    /// Windows科目
    /// </summary>
    Windows = 4,

    /// <summary>
    /// C#科目
    /// </summary>
    CSharp = 5,

    /// <summary>
    /// 综合科目
    /// </summary>
    Comprehensive = 6
}

/// <summary>
/// 科目配置基类
/// </summary>
public abstract class SubjectConfigBase
{
    /// <summary>
    /// 科目类型
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// 是否允许跳过
    /// </summary>
    public bool AllowSkip { get; set; } = false;

    /// <summary>
    /// 是否显示进度
    /// </summary>
    public bool ShowProgress { get; set; } = true;
}

/// <summary>
/// Excel科目配置
/// </summary>
public class ExcelSubjectConfig : SubjectConfigBase
{
    /// <summary>
    /// 允许的Excel版本
    /// </summary>
    public List<string> AllowedExcelVersions { get; set; } = ["2016", "2019", "2021"];

    /// <summary>
    /// 是否允许使用帮助
    /// </summary>
    public bool AllowHelp { get; set; } = false;

    /// <summary>
    /// 是否自动保存
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    public int AutoSaveInterval { get; set; } = 30;

    /// <summary>
    /// 操作点分类权重
    /// </summary>
    public Dictionary<string, decimal> CategoryWeights { get; set; } = new Dictionary<string, decimal>
    {
        ["BasicOperation"] = 0.5m,
        ["DataListOperation"] = 0.3m,
        ["ChartOperation"] = 0.2m
    };
}

/// <summary>
/// Windows科目配置
/// </summary>
public class WindowsSubjectConfig : SubjectConfigBase
{
    /// <summary>
    /// 允许的Windows版本
    /// </summary>
    public List<string> AllowedWindowsVersions { get; set; } = ["Windows 10", "Windows 11"];

    /// <summary>
    /// 是否允许使用帮助
    /// </summary>
    public bool AllowHelp { get; set; } = false;

    /// <summary>
    /// 是否启用文件系统监控
    /// </summary>
    public bool EnableFileSystemMonitoring { get; set; } = true;

    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int MonitoringInterval { get; set; } = 5;

    /// <summary>
    /// 操作类型权重
    /// </summary>
    public Dictionary<string, decimal> OperationTypeWeights { get; set; } = new Dictionary<string, decimal>
    {
        ["Create"] = 0.15m,
        ["Copy"] = 0.15m,
        ["Move"] = 0.15m,
        ["Delete"] = 0.10m,
        ["Rename"] = 0.15m,
        ["CreateShortcut"] = 0.10m,
        ["ModifyProperties"] = 0.10m,
        ["CopyAndRename"] = 0.10m
    };

    /// <summary>
    /// 允许的操作模式
    /// </summary>
    public List<string> AllowedOperationModes { get; set; } = ["File", "Folder", "Universal"];
}

/// <summary>
/// 科目统计信息
/// </summary>
public class SubjectStatistics
{
    /// <summary>
    /// 科目类型
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// 科目名称
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// 总分值
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 平均难度
    /// </summary>
    public decimal AverageDifficulty { get; set; }

    /// <summary>
    /// 各难度级别题目数量
    /// </summary>
    public Dictionary<int, int> DifficultyDistribution { get; set; } = [];
}
