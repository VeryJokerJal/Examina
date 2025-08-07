using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExaminaWebApplication.Models.Exam;

namespace ExaminaWebApplication.Models.Practice
{
    /// <summary>
    /// 专项练习主表 - 针对单一科目的练习模式
    /// </summary>
    public class SpecializedPractice
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 练习名称
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 练习描述
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// 科目类型（只能选择一个科目）
        /// </summary>
        [Required]
        public SubjectType SubjectType { get; set; }

        /// <summary>
        /// 练习状态
        /// </summary>
        [Required]
        public PracticeStatus Status { get; set; } = PracticeStatus.Draft;

        /// <summary>
        /// 总分
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalScore { get; set; } = 100.0m;

        /// <summary>
        /// 练习时长（分钟）
        /// </summary>
        [Required]
        public int DurationMinutes { get; set; } = 60;

        /// <summary>
        /// 及格分数
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(6,2)")]
        public decimal PassingScore { get; set; } = 60.0m;

        /// <summary>
        /// 是否允许重做
        /// </summary>
        public bool AllowRetake { get; set; } = true;

        /// <summary>
        /// 最大重做次数（0表示无限制）
        /// </summary>
        public int MaxRetakeCount { get; set; } = 0;

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
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
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
        /// 创建者ID
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }

        /// <summary>
        /// 发布者ID
        /// </summary>
        public int? PublishedBy { get; set; }

        /// <summary>
        /// 标签（用逗号分隔）
        /// </summary>
        [StringLength(500)]
        public string? Tags { get; set; }

        /// <summary>
        /// 扩展配置（JSON格式）
        /// </summary>
        public string? ExtendedConfig { get; set; }

        // 导航属性
        /// <summary>
        /// 创建者
        /// </summary>
        public virtual User? Creator { get; set; }

        /// <summary>
        /// 发布者
        /// </summary>
        public virtual User? Publisher { get; set; }

        /// <summary>
        /// 练习题目列表
        /// </summary>
        public virtual ICollection<PracticeQuestion> Questions { get; set; } = [];
    }

    /// <summary>
    /// 专项练习题目表
    /// </summary>
    public class PracticeQuestion
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 专项练习ID
        /// </summary>
        [Required]
        public int PracticeId { get; set; }

        /// <summary>
        /// 题目标题
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 题目内容/描述
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型（Windows操作或C#编程）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// 分值
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Score { get; set; } = 10.0m;

        /// <summary>
        /// 难度等级（1-5）
        /// </summary>
        [Required]
        public int DifficultyLevel { get; set; } = 1;

        /// <summary>
        /// 预计完成时间（分钟）
        /// </summary>
        [Required]
        public int EstimatedMinutes { get; set; } = 5;

        /// <summary>
        /// 题目序号
        /// </summary>
        [Required]
        public int QuestionNumber { get; set; } = 1;

        /// <summary>
        /// 排序顺序
        /// </summary>
        [Required]
        public int SortOrder { get; set; } = 1;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 题目要求（Markdown格式）
        /// </summary>
        public string? Requirements { get; set; }

        /// <summary>
        /// 操作配置（JSON格式）
        /// </summary>
        public string? OperationConfig { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // 导航属性
        /// <summary>
        /// 所属专项练习
        /// </summary>
        public virtual SpecializedPractice? Practice { get; set; }
    }

    /// <summary>
    /// 专项练习状态枚举
    /// </summary>
    public enum PracticeStatus
    {
        /// <summary>
        /// 草稿状态
        /// </summary>
        Draft = 1,

        /// <summary>
        /// 已发布
        /// </summary>
        Published = 2,

        /// <summary>
        /// 已暂停
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// 已归档
        /// </summary>
        Archived = 4
    }
}
