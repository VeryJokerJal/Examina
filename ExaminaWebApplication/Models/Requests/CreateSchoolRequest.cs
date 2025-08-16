using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Requests;

/// <summary>
/// 创建学校请求模型
/// </summary>
public class CreateSchoolRequest
{
    /// <summary>
    /// 学校名称
    /// </summary>
    [Required(ErrorMessage = "学校名称不能为空")]
    [StringLength(100, ErrorMessage = "学校名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
}
