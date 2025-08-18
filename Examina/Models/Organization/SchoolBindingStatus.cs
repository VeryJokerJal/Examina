namespace Examina.Models.Organization;

/// <summary>
/// 学校绑定状态
/// </summary>
public class SchoolBindingStatus
{
    /// <summary>
    /// 是否已绑定学校
    /// </summary>
    public bool IsSchoolBound { get; set; }

    /// <summary>
    /// 当前学校名称
    /// </summary>
    public string? CurrentSchool { get; set; }

    /// <summary>
    /// 学校ID
    /// </summary>
    public int? SchoolId { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime? JoinedAt { get; set; }
}
