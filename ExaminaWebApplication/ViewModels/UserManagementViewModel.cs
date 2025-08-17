using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 用户管理ViewModel
/// </summary>
public class UserManagementViewModel : ViewModelBase
{
    private List<UserDto> _users = [];
    private List<OrganizationDto> _schools = [];
    private List<OrganizationDto> _classes = [];
    private UserDto? _selectedUser;
    private string _searchKeyword = string.Empty;
    private UserRole? _selectedRole;
    private bool _includeInactive = false;
    private bool _isLoading = false;
    private string? _errorMessage;
    private string? _successMessage;
    private int _currentPage = 1;
    private int _pageSize = 50;

    /// <summary>
    /// 用户列表
    /// </summary>
    public List<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

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
    /// 当前选中的用户
    /// </summary>
    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
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
    /// 选中的角色过滤
    /// </summary>
    public UserRole? SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    /// <summary>
    /// 是否包含非激活的用户
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
    /// 当前页码
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    /// <summary>
    /// 创建用户请求模型
    /// </summary>
    public CreateUserRequest CreateUserRequest { get; set; } = new CreateUserRequest();

    /// <summary>
    /// 更新用户请求模型
    /// </summary>
    public UpdateUserRequest UpdateUserRequest { get; set; } = new UpdateUserRequest();

    /// <summary>
    /// 重置密码请求模型
    /// </summary>
    public ResetPasswordRequest ResetPasswordRequest { get; set; } = new ResetPasswordRequest();

    /// <summary>
    /// 用户角色选项
    /// </summary>
    public List<(UserRole Value, string Text)> UserRoleOptions { get; } =
    [
        (UserRole.Student, "学生"),
        (UserRole.Teacher, "教师"),
        (UserRole.Administrator, "管理员")
    ];

    /// <summary>
    /// 角色过滤选项
    /// </summary>
    public List<(UserRole? Value, string Text)> RoleFilterOptions { get; } =
    [
        (null, "全部角色"),
        (UserRole.Student, "学生"),
        (UserRole.Teacher, "教师"),
        (UserRole.Administrator, "管理员")
    ];

    /// <summary>
    /// 可用的班级列表（根据选中的学校过滤）
    /// </summary>
    public List<OrganizationDto> AvailableClasses => CreateUserRequest.SchoolId == null ? [] : Classes.Where(c => c.ParentOrganizationId == CreateUserRequest.SchoolId).ToList();

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
    /// 重置创建用户表单
    /// </summary>
    public void ResetCreateForm()
    {
        CreateUserRequest = new CreateUserRequest();
        ClearMessages();
    }

    /// <summary>
    /// 重置更新用户表单
    /// </summary>
    public void ResetUpdateForm()
    {
        UpdateUserRequest = new UpdateUserRequest();
        ClearMessages();
    }

    /// <summary>
    /// 重置密码表单
    /// </summary>
    public void ResetPasswordForm()
    {
        ResetPasswordRequest = new ResetPasswordRequest();
        ClearMessages();
    }

    /// <summary>
    /// 从选中的用户填充更新表单
    /// </summary>
    public void PopulateUpdateForm()
    {
        if (SelectedUser != null)
        {
            UpdateUserRequest = new UpdateUserRequest
            {
                Email = SelectedUser.Email,
                PhoneNumber = SelectedUser.PhoneNumber,
                RealName = SelectedUser.RealName
            };
        }
    }

    /// <summary>
    /// 重置分页到第一页
    /// </summary>
    public void ResetPaging()
    {
        CurrentPage = 1;
    }
}
