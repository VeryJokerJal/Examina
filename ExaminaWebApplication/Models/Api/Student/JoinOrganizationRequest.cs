using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Api.Student;

/// <summary>
/// 学生加入组织请求模型
/// </summary>
public class JoinOrganizationRequest
{
    /// <summary>
    /// 邀请码（7位字母数字组合）
    /// </summary>
    [Required(ErrorMessage = "邀请码不能为空")]
    [StringLength(7, MinimumLength = 7, ErrorMessage = "邀请码必须为7位")]
    [RegularExpression(@"^[A-Za-z0-9]{7}$", ErrorMessage = "邀请码只能包含字母和数字")]
    public string InvitationCode { get; set; } = string.Empty;
}
