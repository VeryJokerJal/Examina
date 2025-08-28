using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 学校管理ViewModel
/// </summary>
public class SchoolManagementViewModel : ViewModelBase
{
    private List<OrganizationDto> _schools = new List<OrganizationDto>();
    private OrganizationDto? _selectedSchool;
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
    /// 当前选中的学校
    /// </summary>
    public OrganizationDto? SelectedSchool
    {
        get => _selectedSchool;
        set => SetProperty(ref _selectedSchool, value);
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
    /// 是否包含非激活的学校
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
    /// 创建学校请求模型
    /// </summary>
    public CreateSchoolRequest CreateSchoolRequest { get; set; } = new CreateSchoolRequest();

    /// <summary>
    /// 过滤后的学校列表
    /// </summary>
    public List<OrganizationDto> FilteredSchools
    {
        get
        {
            List<OrganizationDto> filtered = Schools;

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                filtered = [.. filtered.Where(s => s.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))];
            }

            if (!IncludeInactive)
            {
                filtered = [.. filtered.Where(s => s.IsActive)];
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
    /// 重置创建学校表单
    /// </summary>
    public void ResetCreateForm()
    {
        CreateSchoolRequest = new CreateSchoolRequest();
        ClearMessages();
    }
}
