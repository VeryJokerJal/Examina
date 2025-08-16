using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 更新用户请求模型
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// 邮箱
    /// </summary>
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
    public string? RealName { get; set; }
}
