using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 非组织学生管理ViewModel
/// </summary>
public class NonOrganizationStudentViewModel : ViewModelBase
{
    private List<NonOrganizationStudentDto> _students = new List<NonOrganizationStudentDto>();
    private NonOrganizationStudentDto? _selectedStudent;
    private string _searchKeyword = string.Empty;
    private string _searchType = "name"; // "name" or "phone"
    private bool _includeInactive = false;
    private bool _isLoading = false;
    private string? _errorMessage;
    private string? _successMessage;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount = 0;

    /// <summary>
    /// 学生列表
    /// </summary>
    public List<NonOrganizationStudentDto> Students
    {
        get => _students;
        set => SetProperty(ref _students, value);
    }

    /// <summary>
    /// 当前选中的学生
    /// </summary>
    public NonOrganizationStudentDto? SelectedStudent
    {
        get => _selectedStudent;
        set => SetProperty(ref _selectedStudent, value);
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
    /// 搜索类型（姓名或手机号）
    /// </summary>
    public string SearchType
    {
        get => _searchType;
        set => SetProperty(ref _searchType, value);
    }

    /// <summary>
    /// 是否包含非激活的学生
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
    /// 总数量
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    /// <summary>
    /// 创建学生请求模型
    /// </summary>
    public CreateNonOrganizationStudentRequest CreateStudentRequest { get; set; } = new CreateNonOrganizationStudentRequest();

    /// <summary>
    /// 更新学生请求模型
    /// </summary>
    public CreateNonOrganizationStudentRequest UpdateStudentRequest { get; set; } = new CreateNonOrganizationStudentRequest();

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 搜索类型选项
    /// </summary>
    public List<(string Value, string Text)> SearchTypeOptions { get; } = new List<(string, string)>
    {
        ("name", "按姓名搜索"),
        ("phone", "按手机号搜索")
    };

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
    /// 重置创建学生表单
    /// </summary>
    public void ResetCreateForm()
    {
        CreateStudentRequest = new CreateNonOrganizationStudentRequest();
        ClearMessages();
    }

    /// <summary>
    /// 重置更新学生表单
    /// </summary>
    public void ResetUpdateForm()
    {
        UpdateStudentRequest = new CreateNonOrganizationStudentRequest();
        ClearMessages();
    }

    /// <summary>
    /// 从选中的学生填充更新表单
    /// </summary>
    public void PopulateUpdateForm()
    {
        if (SelectedStudent != null)
        {
            UpdateStudentRequest = new CreateNonOrganizationStudentRequest
            {
                RealName = SelectedStudent.RealName,
                PhoneNumber = SelectedStudent.PhoneNumber,
                Notes = SelectedStudent.Notes
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
