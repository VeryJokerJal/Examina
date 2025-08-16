using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 教师组织详情视图模型
/// </summary>
public class TeacherOrganizationDetailsViewModel
{
    /// <summary>
    /// 组织信息
    /// </summary>
    public OrganizationDto Organization { get; set; } = new();

    /// <summary>
    /// 成员列表
    /// </summary>
    public List<StudentOrganizationDto> Members { get; set; } = [];
}
