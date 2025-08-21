using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.DTOs.MockExam;

/// <summary>
/// 模拟考试提交响应DTO
/// </summary>
public class MockExamSubmissionResponseDto
{
    /// <summary>
    /// 提交是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 考试完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 实际用时（分钟）
    /// </summary>
    public int? ActualDurationMinutes { get; set; }

    /// <summary>
    /// 考试状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 时间状态描述
    /// </summary>
    public string TimeStatusDescription { get; set; } = string.Empty;
}
