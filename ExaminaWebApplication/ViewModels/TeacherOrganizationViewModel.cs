using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 教师组织首页视图模型
/// </summary>
public class TeacherOrganizationViewModel
{
    /// <summary>
    /// 已加入的组织列表
    /// </summary>
    public List<StudentOrganizationDto> JoinedOrganizations { get; set; } = [];

    /// <summary>
    /// 可加入的学校组织列表
    /// </summary>
    public List<OrganizationDto> AvailableSchoolOrganizations { get; set; } = [];
}
