using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 创建用户请求模型
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 用户角色
    /// </summary>
    [Required(ErrorMessage = "用户角色不能为空")]
    public UserRole Role { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号（学生）或工号（教师/管理员）
    /// </summary>
    [StringLength(50, ErrorMessage = "学号/工号长度不能超过50个字符")]
    public string? StudentId { get; set; }

    /// <summary>
    /// 所属学校ID（教师必填）
    /// </summary>
    public int? SchoolId { get; set; }

    /// <summary>
    /// 所属班级ID列表（教师可选）
    /// </summary>
    public List<int>? ClassIds { get; set; }
}
