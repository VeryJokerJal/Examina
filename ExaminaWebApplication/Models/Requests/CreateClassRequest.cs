using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 创建班级请求模型
/// </summary>
public class CreateClassRequest
{
    /// <summary>
    /// 班级名称
    /// </summary>
    [Required(ErrorMessage = "班级名称不能为空")]
    [StringLength(100, ErrorMessage = "班级名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 所属学校ID
    /// </summary>
    [Required(ErrorMessage = "所属学校ID不能为空")]
    public int SchoolId { get; set; }

    /// <summary>
    /// 是否自动生成邀请码
    /// </summary>
    public bool GenerateInvitationCode { get; set; } = true;

    /// <summary>
    /// 邀请码过期时间（可选）
    /// </summary>
    public DateTime? InvitationCodeExpiresAt { get; set; }

    /// <summary>
    /// 邀请码最大使用次数（可选）
    /// </summary>
    public int? InvitationCodeMaxUsage { get; set; }
}
