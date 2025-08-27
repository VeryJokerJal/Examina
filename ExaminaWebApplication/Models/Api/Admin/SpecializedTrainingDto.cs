namespace ExaminaWebApplication.Models.Api.Admin;

/// <summary>
/// 更新专项训练名称请求DTO
/// </summary>
public class UpdateSpecializedTrainingNameRequestDto
{
    /// <summary>
    /// 新的专项训练名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 更新专项训练名称响应DTO
/// </summary>
public class UpdateSpecializedTrainingNameResponseDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 更新后的专项训练名称
    /// </summary>
    public string? UpdatedName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
