using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 创建非组织学生请求模型
/// </summary>
public class CreateNonOrganizationStudentRequest
{
    /// <summary>
    /// 学生真实姓名
    /// </summary>
    [Required(ErrorMessage = "学生真实姓名不能为空")]
    [StringLength(50, ErrorMessage = "学生真实姓名长度不能超过50个字符")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [Required(ErrorMessage = "手机号码不能为空")]
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
    [Phone(ErrorMessage = "手机号码格式不正确")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 备注信息
    /// </summary>
    [StringLength(500, ErrorMessage = "备注信息长度不能超过500个字符")]
    public string? Notes { get; set; }
}
