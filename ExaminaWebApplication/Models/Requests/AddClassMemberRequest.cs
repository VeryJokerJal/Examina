using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 添加班级成员请求模型
/// </summary>
public class AddClassMemberRequest
{
    /// <summary>
    /// 学生真实姓名
    /// </summary>
    [Required(ErrorMessage = "学生姓名不能为空")]
    [StringLength(50, ErrorMessage = "学生姓名长度不能超过50个字符")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 学生手机号码
    /// </summary>
    [Required(ErrorMessage = "手机号码不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "请输入正确的11位手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500, ErrorMessage = "备注信息长度不能超过500个字符")]
    public string? Notes { get; set; }

    /// <summary>
    /// 邀请码ID（可选，如果不指定则使用默认邀请码）
    /// </summary>
    public int? InvitationCodeId { get; set; }
}
