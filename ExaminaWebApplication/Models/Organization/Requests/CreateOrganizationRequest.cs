using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization.Requests;

/// <summary>
/// 创建组织请求模型
/// </summary>
public class CreateOrganizationRequest
{
    /// <summary>
    /// 组织名称
    /// </summary>
    [Required(ErrorMessage = "组织名称不能为空")]
    [StringLength(100, ErrorMessage = "组织名称长度不能超过100个字符")]
    [Display(Name = "组织名称")]
    public string Name { get; set; } = string.Empty;





    /// <summary>
    /// 是否自动生成邀请码
    /// </summary>
    [Display(Name = "自动生成邀请码")]
    public bool GenerateInvitationCode { get; set; } = true;

    /// <summary>
    /// 邀请码过期时间（可选，null表示永不过期）
    /// </summary>
    [Display(Name = "过期时间")]
    public DateTime? InvitationCodeExpiresAt { get; set; }

    /// <summary>
    /// 邀请码最大使用次数（可选，null表示无限制）
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "最大使用次数必须大于0")]
    [Display(Name = "最大使用次数")]
    public int? InvitationCodeMaxUsage { get; set; }
}
