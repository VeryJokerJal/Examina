namespace ExaminaWebApplication.Models.Dto;

/// <summary>
/// 综合训练完成记录DTO
/// </summary>
public class ComprehensiveTrainingCompletionDto
{
    /// <summary>
    /// 完成记录ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 训练ID
    /// </summary>
    public int TrainingId { get; set; }

    /// <summary>
    /// 训练名称
    /// </summary>
    public string TrainingName { get; set; } = string.Empty;

    /// <summary>
    /// 训练描述
    /// </summary>
    public string? TrainingDescription { get; set; }

    /// <summary>
    /// 完成状态
    /// </summary>
    public ComprehensiveTrainingCompletionStatus Status { get; set; }

    /// <summary>
    /// 得分（可选）
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
    /// </summary>
    public double? MaxScore { get; set; }

    /// <summary>
    /// 完成百分比（0-100）
    /// </summary>
    public double? CompletionPercentage { get; set; }

    /// <summary>
    /// 用时（秒）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 用时显示文本
    /// </summary>
    public string DurationText => DurationSeconds.HasValue ? FormatDuration(DurationSeconds.Value) : "未记录";

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        ComprehensiveTrainingCompletionStatus.NotStarted => "未开始",
        ComprehensiveTrainingCompletionStatus.InProgress => "进行中",
        ComprehensiveTrainingCompletionStatus.Completed => "已完成",
        ComprehensiveTrainingCompletionStatus.Abandoned => "已放弃",
        ComprehensiveTrainingCompletionStatus.Timeout => "超时",
        _ => "未知状态"
    };

    /// <summary>
    /// 格式化用时显示
    /// </summary>
    /// <param name="seconds">总秒数</param>
    /// <returns>格式化的时间字符串</returns>
    private static string FormatDuration(int seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds}秒";
        }

        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;

        if (minutes < 60)
        {
            return remainingSeconds > 0 ? $"{minutes}分钟{remainingSeconds}秒" : $"{minutes}分钟";
        }

        int hours = minutes / 60;
        int remainingMinutes = minutes % 60;

        if (remainingMinutes > 0 && remainingSeconds > 0)
        {
            return $"{hours}小时{remainingMinutes}分钟{remainingSeconds}秒";
        }
        else if (remainingMinutes > 0)
        {
            return $"{hours}小时{remainingMinutes}分钟";
        }
        else if (remainingSeconds > 0)
        {
            return $"{hours}小时{remainingSeconds}秒";
        }
        else
        {
            return $"{hours}小时";
        }
    }
}
