using System.ComponentModel.DataAnnotations;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Models.Admin.Requests;

/// <summary>
/// 管理员创建用户请求模型
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "请选择用户类型")]
    [Display(Name = "用户类型")]
    public UserRole Role { get; set; } = UserRole.Student;

    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    [Display(Name = "用户名")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
    [Display(Name = "邮箱")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    [Display(Name = "手机号")]
    public string? PhoneNumber { get; set; }

    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    [Display(Name = "姓名")]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号（学生）或工号（教师）
    /// </summary>
    [StringLength(50, ErrorMessage = "学号/工号长度不能超过50个字符")]
    [Display(Name = "学号/工号")]
    public string? StudentId { get; set; }

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度至少8位")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    [Display(Name = "确认密码")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// 邀请码（学生可选；教师必填）
    /// </summary>
    [StringLength(7, MinimumLength = 7, ErrorMessage = "邀请码必须为7位字符")]
    [Display(Name = "邀请码")]
    public string? InvitationCode { get; set; }
}

