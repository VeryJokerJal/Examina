using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 班级管理ViewModel
/// </summary>
public class ClassManagementViewModel : ViewModelBase
{
    private List<OrganizationDto> _schools = new List<OrganizationDto>();
    private List<OrganizationDto> _classes = new List<OrganizationDto>();
    private OrganizationDto? _selectedSchool;
    private OrganizationDto? _selectedClass;
    private List<InvitationCode> _invitationCodes = new List<InvitationCode>();
    private List<StudentOrganizationDto> _classMembers = new List<StudentOrganizationDto>();
    private string _searchKeyword = string.Empty;
    private bool _includeInactive = false;
    private bool _isLoading = false;
    private string? _errorMessage;
    private string? _successMessage;

    /// <summary>
    /// 学校列表
    /// </summary>
    public List<OrganizationDto> Schools
    {
        get => _schools;
        set => SetProperty(ref _schools, value);
    }

    /// <summary>
    /// 班级列表
    /// </summary>
    public List<OrganizationDto> Classes
    {
        get => _classes;
        set => SetProperty(ref _classes, value);
    }

    /// <summary>
    /// 当前选中的学校
    /// </summary>
    public OrganizationDto? SelectedSchool
    {
        get => _selectedSchool;
        set => SetProperty(ref _selectedSchool, value);
    }

    /// <summary>
    /// 当前选中的班级
    /// </summary>
    public OrganizationDto? SelectedClass
    {
        get => _selectedClass;
        set => SetProperty(ref _selectedClass, value);
    }

    /// <summary>
    /// 邀请码列表
    /// </summary>
    public List<InvitationCode> InvitationCodes
    {
        get => _invitationCodes;
        set => SetProperty(ref _invitationCodes, value);
    }

    /// <summary>
    /// 班级成员列表
    /// </summary>
    public List<StudentOrganizationDto> ClassMembers
    {
        get => _classMembers;
        set => SetProperty(ref _classMembers, value);
    }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    /// <summary>
    /// 是否包含非激活的班级
    /// </summary>
    public bool IncludeInactive
    {
        get => _includeInactive;
        set => SetProperty(ref _includeInactive, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 成功消息
    /// </summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    /// <summary>
    /// 创建班级请求模型
    /// </summary>
    public CreateClassRequest CreateClassRequest { get; set; } = new CreateClassRequest();

    /// <summary>
    /// 过滤后的班级列表
    /// </summary>
    public List<OrganizationDto> FilteredClasses
    {
        get
        {
            List<OrganizationDto> filtered = Classes;

            if (SelectedSchool != null)
            {
                filtered = filtered.Where(c => c.ParentOrganizationId == SelectedSchool.Id).ToList();
            }

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                filtered = filtered.Where(c => c.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!IncludeInactive)
            {
                filtered = filtered.Where(c => c.IsActive).ToList();
            }

            return filtered;
        }
    }

    /// <summary>
    /// 清除错误和成功消息
    /// </summary>
    public void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    /// <summary>
    /// 设置错误消息
    /// </summary>
    public void SetError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = null;
    }

    /// <summary>
    /// 设置成功消息
    /// </summary>
    public void SetSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = null;
    }

    /// <summary>
    /// 重置创建班级表单
    /// </summary>
    public void ResetCreateForm()
    {
        CreateClassRequest = new CreateClassRequest();
        ClearMessages();
    }
}
