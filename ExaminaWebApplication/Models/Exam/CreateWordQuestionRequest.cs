using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Exam;

/// <summary>
/// 创建Word题目请求（支持简化表单，仅题目要求 + 分值）
/// </summary>
public class CreateWordQuestionRequest
{
    /// <summary>
    /// 科目ID（必须 > 0）
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "SubjectId 必须大于 0")]
    public int SubjectId { get; set; }

    /// <summary>
    /// 题目要求（可选，前端要求必填）
    /// </summary>
    [StringLength(2000)]
    public string? Requirements { get; set; }

    /// <summary>
    /// 题目分值（0.1 - 100.0）
    /// </summary>
    [Required]
    [Range(0.1, 100.0)]
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 可选：题目标题（若未提供，后端自动生成）
    /// </summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// 可选：题目描述（若未提供，后端自动生成）
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }
}

