using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models.Organization.Requests;

/// <summary>
/// 更新成员信息请求模型
/// </summary>
public class UpdateMemberInfoRequest
{
    /// <summary>
    /// 成员ID
    /// </summary>
    [Required(ErrorMessage = "成员ID不能为空")]
    public int MemberId { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    [StringLength(11, ErrorMessage = "手机号长度不能超过11个字符")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// 批量添加成员请求模型
/// </summary>
public class BatchAddMembersRequest
{
    /// <summary>
    /// 成员数据列表
    /// </summary>
    [Required(ErrorMessage = "成员数据不能为空")]
    public List<MemberEntry> MemberEntries { get; set; } = new();

    /// <summary>
    /// 是否覆盖已存在的成员
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// 成员条目
/// </summary>
public class MemberEntry
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    [Required(ErrorMessage = "真实姓名不能为空")]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }
}
