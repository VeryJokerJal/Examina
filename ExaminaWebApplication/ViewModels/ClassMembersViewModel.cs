using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 班级成员管理ViewModel
/// </summary>
public class ClassMembersViewModel : ViewModelBase
{
    /// <summary>
    /// 班级ID
    /// </summary>
    public int ClassId { get; set; }

    /// <summary>
    /// 班级信息
    /// </summary>
    public OrganizationDto? ClassInfo { get; set; }

    /// <summary>
    /// 班级成员列表
    /// </summary>
    public List<StudentOrganizationDto> Members { get; set; } = new List<StudentOrganizationDto>();

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)Members.Count / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchKeyword { get; set; } = string.Empty;

    /// <summary>
    /// 是否包含非激活成员
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}
