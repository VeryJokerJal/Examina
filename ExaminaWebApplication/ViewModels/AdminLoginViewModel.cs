using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 管理员登录视图模型
/// </summary>
public class AdminLoginViewModel
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    [Required(ErrorMessage = "请输入用户名或邮箱")]
    [Display(Name = "用户名/邮箱")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 记住我
    /// </summary>
    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }
}
