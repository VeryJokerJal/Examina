using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization.Requests;

/// <summary>
/// 加入组织请求模型
/// </summary>
public class JoinOrganizationRequest
{
    /// <summary>
    /// 7位邀请码
    /// </summary>
    [Required(ErrorMessage = "邀请码不能为空")]
    [StringLength(7, MinimumLength = 7, ErrorMessage = "邀请码必须为7位字符")]
    [RegularExpression(@"^[A-Za-z0-9]{7}$", ErrorMessage = "邀请码只能包含字母和数字")]
    public string InvitationCode { get; set; } = string.Empty;
}
