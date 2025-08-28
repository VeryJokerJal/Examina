namespace Examina.Models;

/// <summary>
/// 完成专项练习请求DTO
/// </summary>
public class CompletePracticeRequest
{
    /// <summary>
    /// 得分（可选）
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
    /// </summary>
    public double? MaxScore { get; set; }

    /// <summary>
    /// 用时（秒，可选）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Notes { get; set; }
}
