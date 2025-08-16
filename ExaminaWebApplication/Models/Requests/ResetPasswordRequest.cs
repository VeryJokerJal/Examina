using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 重置密码请求模型
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
    public string NewPassword { get; set; } = string.Empty;
}
