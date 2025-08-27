using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 设备管理ViewModel
/// </summary>
public class DeviceManagementViewModel : ViewModelBase
{
    private List<UserDto> _users = [];
    private List<DeviceInfo> _devices = [];
    private UserDto? _selectedUser;
    private DeviceInfo? _selectedDevice;
    private string _searchKeyword = string.Empty;
    private bool _includeInactive = false;
    private bool _isLoading = false;
    private UserRole? _selectedUserRole;
    private string? _errorMessage;
    private string? _successMessage;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalDevices = 0;
    private int _activeDevices = 0;
    private int _trustedDevices = 0;
    private int _expiredDevices = 0;

    /// <summary>
    /// 用户列表
    /// </summary>
    public List<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    /// <summary>
    /// 设备列表
    /// </summary>
    public List<DeviceInfo> Devices
    {
        get => _devices;
        set => SetProperty(ref _devices, value);
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
    /// 当前选中的设备
    /// </summary>
    public DeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
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
    /// 是否包含非激活的设备
    /// </summary>
    public bool IncludeInactive
    {
        get => _includeInactive;
        set => SetProperty(ref _includeInactive, value);
    }

    /// <summary>
    /// 选中的用户角色筛选
    /// </summary>
    public UserRole? SelectedUserRole
    {
        get => _selectedUserRole;
        set => SetProperty(ref _selectedUserRole, value);
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
    /// 设备总数
    /// </summary>
    public int TotalDevices
    {
        get => _totalDevices;
        set => SetProperty(ref _totalDevices, value);
    }

    /// <summary>
    /// 活跃设备数
    /// </summary>
    public int ActiveDevices
    {
        get => _activeDevices;
        set => SetProperty(ref _activeDevices, value);
    }

    /// <summary>
    /// 受信任设备数
    /// </summary>
    public int TrustedDevices
    {
        get => _trustedDevices;
        set => SetProperty(ref _trustedDevices, value);
    }

    /// <summary>
    /// 过期设备数
    /// </summary>
    public int ExpiredDevices
    {
        get => _expiredDevices;
        set => SetProperty(ref _expiredDevices, value);
    }

    /// <summary>
    /// 设备类型过滤选项
    /// </summary>
    public List<(string? Value, string Text)> DeviceTypeFilterOptions { get; } =
    [
        (null, "全部类型"),
        ("Desktop", "桌面端"),
        ("Mobile", "移动端"),
        ("Web", "网页端"),
        ("Tablet", "平板端")
    ];

    /// <summary>
    /// 设备状态过滤选项
    /// </summary>
    public List<(bool? Value, string Text)> DeviceStatusFilterOptions { get; } =
    [
        (null, "全部状态"),
        (true, "活跃"),
        (false, "非活跃")
    ];

    /// <summary>
    /// 用户角色筛选选项
    /// </summary>
    public List<(UserRole? Value, string Text)> UserRoleFilterOptions { get; } =
    [
        (null, "全部用户"),
        (UserRole.Student, "学生"),
        (UserRole.Teacher, "教师"),
        (UserRole.Administrator, "管理员")
    ];

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
    /// 重置分页到第一页
    /// </summary>
    public void ResetPaging()
    {
        CurrentPage = 1;
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    public void UpdateStatistics()
    {
        TotalDevices = Devices.Count;
        ActiveDevices = Devices.Count(d => d.IsActive);
        TrustedDevices = Devices.Count(d => d.IsTrusted);
        ExpiredDevices = Devices.Count(d => !d.IsActive);
    }
}
