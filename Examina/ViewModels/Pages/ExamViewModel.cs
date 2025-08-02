using ReactiveUI.Fody.Helpers;
using System.Windows.Input;
using Prism.Commands;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 上机统考页面视图模型
/// </summary>
public class ExamViewModel : ViewModelBase
{
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

    public ExamViewModel()
    {
        StartExamCommand = new DelegateCommand(StartExam, CanStartExam);
        ContinueExamCommand = new DelegateCommand(ContinueExam, CanContinueExam);
        RefreshExamStatusCommand = new DelegateCommand(RefreshExamStatus);

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
        // TODO: 实现开始考试逻辑
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

    #endregion
}
