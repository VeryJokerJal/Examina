using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;
using Examina.Models;
using Examina.Services;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 上机统考页面视图模型
/// </summary>
public class ExamViewModel : ViewModelBase
{
    private readonly IAuthenticationService? _authenticationService;

    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "上机统考";

    /// <summary>
    /// 考试状态
    /// </summary>
    [Reactive]
    public string ExamStatus { get; set; } = "暂无正在进行的考试";

    /// <summary>
    /// 是否有正在进行的考试
    /// </summary>
    [Reactive]
    public bool HasActiveExam { get; set; } = false;

    /// <summary>
    /// 考试名称
    /// </summary>
    [Reactive]
    public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    [Reactive]
    public DateTime? ExamStartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    [Reactive]
    public DateTime? ExamEndTime { get; set; }

    /// <summary>
    /// 剩余时间
    /// </summary>
    [Reactive]
    public TimeSpan? RemainingTime { get; set; }

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    [Reactive]
    public bool HasFullAccess { get; set; }

    /// <summary>
    /// 开始考试按钮文本
    /// </summary>
    public string StartExamButtonText => HasFullAccess ? "开始考试" : "解锁";

    #endregion

    #region 命令

    /// <summary>
    /// 开始考试命令
    /// </summary>
    public ICommand StartExamCommand { get; }

    /// <summary>
    /// 继续考试命令
    /// </summary>
    public ICommand ContinueExamCommand { get; }

    /// <summary>
    /// 刷新考试状态命令
    /// </summary>
    public ICommand RefreshExamStatusCommand { get; }

    #endregion

    #region 构造函数

    public ExamViewModel(IAuthenticationService? authenticationService = null)
    {
        _authenticationService = authenticationService;

        StartExamCommand = new DelegateCommand(StartExam, CanStartExam);
        ContinueExamCommand = new DelegateCommand(ContinueExam, CanContinueExam);
        RefreshExamStatusCommand = new DelegateCommand(RefreshExamStatus);

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
        }

        LoadExamStatus();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 加载考试状态
    /// </summary>
    private void LoadExamStatus()
    {
        // TODO: 从服务加载实际考试状态
        HasActiveExam = false;
        ExamStatus = "暂无正在进行的考试";
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    private void StartExam()
    {
        if (!HasFullAccess)
        {
            // 用户没有完整权限，显示解锁提示
            System.Diagnostics.Debug.WriteLine("用户尝试开始考试但没有完整权限");
            // TODO: 显示权限提示对话框或导航到绑定页面
            return;
        }

        // TODO: 实现开始考试逻辑
        System.Diagnostics.Debug.WriteLine("开始考试");
    }

    /// <summary>
    /// 是否可以开始考试
    /// </summary>
    private bool CanStartExam()
    {
        return !HasActiveExam;
    }

    /// <summary>
    /// 继续考试
    /// </summary>
    private void ContinueExam()
    {
        // TODO: 实现继续考试逻辑
    }

    /// <summary>
    /// 是否可以继续考试
    /// </summary>
    private bool CanContinueExam()
    {
        return HasActiveExam;
    }

    /// <summary>
    /// 刷新考试状态
    /// </summary>
    private void RefreshExamStatus()
    {
        LoadExamStatus();
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
    {
        UserInfo? currentUser = _authenticationService?.CurrentUser;
        HasFullAccess = currentUser?.HasFullAccess ?? false;

        System.Diagnostics.Debug.WriteLine($"ExamViewModel: 用户权限状态更新 - HasFullAccess: {HasFullAccess}");
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        UpdateUserPermissions();
    }

    #endregion
}
