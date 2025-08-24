using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Examina.Models;

namespace Examina.ViewModels.Dialogs;

/// <summary>
/// 考试结果显示对话框视图模型
/// </summary>
public class ExamResultViewModel : ViewModelBase
{
    /// <summary>
    /// 确认命令
    /// </summary>
    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }

    /// <summary>
    /// 考试名称
    /// </summary>
    [Reactive] public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 考试类型
    /// </summary>
    [Reactive] public ExamType ExamType { get; set; }

    /// <summary>
    /// 考试类型显示文本
    /// </summary>
    public string ExamTypeText => ExamType switch
    {
        ExamType.FormalExam => "上机统考",
        ExamType.MockExam => "模拟考试",
        ExamType.ComprehensiveTraining => "综合实训",
        ExamType.SpecializedTraining => "专项训练",
        _ => "考试"
    };

    /// <summary>
    /// 是否提交成功
    /// </summary>
    [Reactive] public bool IsSubmissionSuccessful { get; set; }

    /// <summary>
    /// 提交状态文本
    /// </summary>
    public string SubmissionStatusText => IsSubmissionSuccessful ? "提交成功" : "提交失败";

    /// <summary>
    /// 提交状态图标
    /// </summary>
    public string SubmissionStatusIcon => IsSubmissionSuccessful ? "✅" : "❌";

    /// <summary>
    /// 提交状态颜色
    /// </summary>
    public string SubmissionStatusColor => IsSubmissionSuccessful ? "#4CAF50" : "#F44336";

    /// <summary>
    /// 考试开始时间
    /// </summary>
    [Reactive] public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    [Reactive] public DateTime? EndTime { get; set; }

    /// <summary>
    /// 实际用时（分钟）
    /// </summary>
    [Reactive] public int? ActualDurationMinutes { get; set; }

    /// <summary>
    /// 实际用时显示文本
    /// </summary>
    public string ActualDurationText
    {
        get
        {
            if (!ActualDurationMinutes.HasValue)
                return "未知";

            int minutes = ActualDurationMinutes.Value;
            int hours = minutes / 60;
            int remainingMinutes = minutes % 60;

            if (hours > 0)
                return $"{hours}小时{remainingMinutes}分钟";
            else
                return $"{remainingMinutes}分钟";
        }
    }

    /// <summary>
    /// 得分
    /// </summary>
    [Reactive] public decimal? Score { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [Reactive] public decimal? TotalScore { get; set; }

    /// <summary>
    /// 是否正在评分
    /// </summary>
    [Reactive] public bool IsScoring { get; set; } = false;

    /// <summary>
    /// 得分显示文本（只显示得分，不显示总分）
    /// </summary>
    public string ScoreText
    {
        get
        {
            if (IsScoring)
                return "计算中...";
            else if (Score.HasValue)
                return $"{Score:F1}";
            else
                return "暂无评分";
        }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive] public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// 备注信息
    /// </summary>
    [Reactive] public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// 是否有备注
    /// </summary>
    public bool HasNotes => !string.IsNullOrEmpty(Notes);

    public ExamResultViewModel()
    {
        // 初始化命令
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("ExamResultViewModel: 确认命令被执行");
            return true;
        });

        // 监听属性变化，更新计算属性
        this.WhenAnyValue(x => x.Score, x => x.IsScoring)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ScoreText));
            });

        this.WhenAnyValue(x => x.ActualDurationMinutes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ActualDurationText)));

        this.WhenAnyValue(x => x.IsSubmissionSuccessful)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SubmissionStatusText));
                this.RaisePropertyChanged(nameof(SubmissionStatusIcon));
                this.RaisePropertyChanged(nameof(SubmissionStatusColor));
            });

        this.WhenAnyValue(x => x.ExamType)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ExamTypeText)));

        this.WhenAnyValue(x => x.ErrorMessage)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasError)));

        this.WhenAnyValue(x => x.Notes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNotes)));
    }

    /// <summary>
    /// 设置考试结果数据
    /// </summary>
    public void SetExamResult(string examName, ExamType examType, bool isSuccessful,
        DateTime? startTime = null, DateTime? endTime = null, int? durationMinutes = null,
        decimal? score = null, decimal? totalScore = null, string errorMessage = "", string notes = "")
    {
        ExamName = examName;
        ExamType = examType;
        IsSubmissionSuccessful = isSuccessful;
        StartTime = startTime;
        EndTime = endTime;
        ActualDurationMinutes = durationMinutes;
        Score = score;
        TotalScore = totalScore;
        ErrorMessage = errorMessage;
        Notes = notes;

        System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: 设置考试结果 - {examName}, 成功: {isSuccessful}, 得分: {score}");
    }

    /// <summary>
    /// 开始评分计算
    /// </summary>
    public void StartScoring()
    {
        IsScoring = true;
        Score = null;
        TotalScore = null;
        System.Diagnostics.Debug.WriteLine("ExamResultViewModel: 开始评分计算");
    }

    /// <summary>
    /// 更新评分结果
    /// </summary>
    public void UpdateScore(decimal? score, decimal? totalScore = null, string notes = "")
    {
        IsScoring = false;
        Score = score;
        TotalScore = totalScore;
        if (!string.IsNullOrEmpty(notes))
        {
            Notes = notes;
        }
        System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: 评分更新完成 - 得分: {score}");
    }

    /// <summary>
    /// 评分失败
    /// </summary>
    public void ScoringFailed(string errorMessage)
    {
        IsScoring = false;
        Score = null;
        TotalScore = null;
        ErrorMessage = errorMessage;
        System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: 评分失败 - {errorMessage}");
    }
}
