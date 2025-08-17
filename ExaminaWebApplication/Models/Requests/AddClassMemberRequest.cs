using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 添加班级成员请求模型
/// </summary>
public class AddClassMemberRequest
{
    /// <summary>
    /// 学生用户ID
    /// </summary>
    [Required(ErrorMessage = "学生ID不能为空")]
    public int StudentId { get; set; }

    /// <summary>
    /// 邀请码ID（可选，如果不指定则使用默认邀请码）
    /// </summary>
    public int? InvitationCodeId { get; set; }
}
