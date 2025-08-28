using System.Reactive;
using Examina.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
    public new string SecondaryStatusMessage
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
                return ExamType switch
                {
                    ExamType.MockExam => $"感谢您的参与",
                    ExamType.ComprehensiveTraining => $"感谢您的参与",
                    ExamType.FormalExam => $"感谢您的参与",
                    ExamType.Practice => $"感谢您的参与",
                    ExamType.SpecialPractice => $"感谢您的参与",
                    ExamType.SpecializedTraining => $"感谢您的参与",
                    _ => $"感谢您的参与",
                };
            }
            else
            {
                return ExamType == ExamType.FormalExam ? "考试已成功提交，成绩将在稍后公布" : "感谢您的参与";
            }
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
    /// 是否显示成绩信息（简单版本）
    /// </summary>
    public new bool ShowScoreInfo => IsSubmissionSuccessful && (Score.HasValue || IsScoring || ExamType == ExamType.FormalExam);

    /// <summary>
    /// 是否显示详细分数信息（全屏模式下强制禁用）
    /// </summary>
    public new bool ShowDetailedScore => false;

    /// <summary>
    /// 得分显示文本（重写以支持正式考试的特殊显示）
    /// </summary>
    public new string ScoreText
    {
        get
        {
            if (IsScoring)
            {
                return "计算中...";
            }
            else if (Score.HasValue)
            {
                return $"{Score:F1}";
            }
            else
            {
                return ExamType == ExamType.FormalExam && IsSubmissionSuccessful ? "已提交" : "暂无评分";
            }
        }
    }

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
    public bool ShowNotesInfo => false/*!string.IsNullOrEmpty(Notes)*/;

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
        _ = this.WhenAnyValue(x => x.ExamType)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));

        _ = this.WhenAnyValue(x => x.IsSubmissionSuccessful)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(PrimaryStatusMessage));
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
                this.RaisePropertyChanged(nameof(StatusIcon));
                this.RaisePropertyChanged(nameof(StatusColor));
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
                this.RaisePropertyChanged(nameof(ShowErrorInfo));
            });

        _ = this.WhenAnyValue(x => x.ShowDetailedScore)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
            });

        _ = this.WhenAnyValue(x => x.Score, x => x.IsScoring)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
                this.RaisePropertyChanged(nameof(ScoreText));
            });

        _ = this.WhenAnyValue(x => x.ActualDurationMinutes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ShowDurationInfo)));

        _ = this.WhenAnyValue(x => x.ErrorMessage)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
                this.RaisePropertyChanged(nameof(ShowErrorInfo));
            });

        _ = this.WhenAnyValue(x => x.Notes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ShowNotesInfo)));

        _ = this.WhenAnyValue(x => x.ExamType, x => x.IsSubmissionSuccessful)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
                this.RaisePropertyChanged(nameof(ScoreText));
            });
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
        double? score = null, double? totalScore = null, string errorMessage = "", string notes = "",
        bool showContinue = true, bool showClose = true)
    {
        // 调用基类方法设置基本数据
        SetExamResult(examName, examType, isSuccessful, startTime, endTime, durationMinutes, score, totalScore, errorMessage, notes);

        // 设置按钮显示状态
        SetButtonVisibility(showContinue, showClose);

        System.Diagnostics.Debug.WriteLine($"FullScreenExamResultViewModel: 设置全屏考试结果 - {examName}, 成功: {isSuccessful}");
    }
}
