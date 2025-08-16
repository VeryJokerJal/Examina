using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization.Requests;

/// <summary>
/// 创建邀请码请求模型
/// </summary>
public class CreateInvitationCodeRequest
{
    /// <summary>
    /// 过期时间（可选，null表示永不过期）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 最大使用次数（可选，null表示无限制）
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "最大使用次数必须大于0")]
    public int? MaxUsage { get; set; }
}
