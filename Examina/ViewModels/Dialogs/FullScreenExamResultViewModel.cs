using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Examina.Models;

namespace Examina.ViewModels.Dialogs;

/// <summary>
/// 全屏考试结果显示窗口视图模型
/// </summary>
public class FullScreenExamResultViewModel : ExamResultViewModel
{
    /// <summary>
    /// 关闭窗口命令
    /// </summary>
    public ReactiveCommand<Unit, bool> CloseCommand { get; }

    /// <summary>
    /// 继续命令（用于继续下一步操作）
    /// </summary>
    public ReactiveCommand<Unit, bool> ContinueCommand { get; }

    /// <summary>
    /// 是否显示继续按钮
    /// </summary>
    [Reactive] public bool ShowContinueButton { get; set; } = true;

    /// <summary>
    /// 是否显示关闭按钮
    /// </summary>
    [Reactive] public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string WindowTitle => $"{ExamTypeText} - 考试结果";

    /// <summary>
    /// 主要状态消息
    /// </summary>
    public string PrimaryStatusMessage => IsSubmissionSuccessful ? "考试已成功提交" : "考试提交失败";

    /// <summary>
    /// 次要状态消息
    /// </summary>
    public string SecondaryStatusMessage
    {
        get
        {
            if (!IsSubmissionSuccessful)
            {
                return !string.IsNullOrEmpty(ErrorMessage) ? ErrorMessage : "请检查网络连接或联系管理员";
            }

            if (IsScoring)
            {
                return "正在计算成绩，请稍候...";
            }

            if (Score.HasValue)
            {
                return $"您的成绩：{ScoreText}分";
            }

            return "感谢您的参与";
        }
    }

    /// <summary>
    /// 状态图标
    /// </summary>
    public string StatusIcon => IsSubmissionSuccessful ? "🎉" : "⚠️";

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => IsSubmissionSuccessful ? "#4CAF50" : "#FF5722";

    /// <summary>
    /// 是否显示成绩信息
    /// </summary>
    public bool ShowScoreInfo => IsSubmissionSuccessful && (Score.HasValue || IsScoring);

    /// <summary>
    /// 是否显示用时信息
    /// </summary>
    public bool ShowDurationInfo => ActualDurationMinutes.HasValue;

    /// <summary>
    /// 是否显示错误信息
    /// </summary>
    public bool ShowErrorInfo => !IsSubmissionSuccessful && !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 是否显示备注信息
    /// </summary>
    public bool ShowNotesInfo => !string.IsNullOrEmpty(Notes);

    public FullScreenExamResultViewModel()
    {
        // 初始化命令
        CloseCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("FullScreenExamResultViewModel: 关闭命令被执行");
            return true;
        });

        ContinueCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("FullScreenExamResultViewModel: 继续命令被执行");
            return true;
        });

        // 监听属性变化，更新计算属性
        this.WhenAnyValue(x => x.ExamType)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));

        this.WhenAnyValue(x => x.IsSubmissionSuccessful)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(PrimaryStatusMessage));
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
                this.RaisePropertyChanged(nameof(StatusIcon));
                this.RaisePropertyChanged(nameof(StatusColor));
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
                this.RaisePropertyChanged(nameof(ShowErrorInfo));
            });

        this.WhenAnyValue(x => x.Score, x => x.IsScoring)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
            });

        this.WhenAnyValue(x => x.ActualDurationMinutes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ShowDurationInfo)));

        this.WhenAnyValue(x => x.ErrorMessage)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
                this.RaisePropertyChanged(nameof(ShowErrorInfo));
            });

        this.WhenAnyValue(x => x.Notes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ShowNotesInfo)));
    }

    /// <summary>
    /// 设置按钮显示状态
    /// </summary>
    /// <param name="showContinue">是否显示继续按钮</param>
    /// <param name="showClose">是否显示关闭按钮</param>
    public void SetButtonVisibility(bool showContinue = true, bool showClose = true)
    {
        ShowContinueButton = showContinue;
        ShowCloseButton = showClose;
    }

    /// <summary>
    /// 设置全屏考试结果数据
    /// </summary>
    public void SetFullScreenExamResult(string examName, ExamType examType, bool isSuccessful,
        DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        decimal? score = null, decimal? totalScore = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        // 调用基类方法设置基本数据
        SetExamResult(examName, examType, isSuccessful, startTime, endTime, durationMinutes, score, totalScore, errorMessage, notes);
        
        // 设置按钮显示状态
        SetButtonVisibility(showContinue, showClose);

        System.Diagnostics.Debug.WriteLine($"FullScreenExamResultViewModel: 设置全屏考试结果 - {examName}, 成功: {isSuccessful}");
    }
}
