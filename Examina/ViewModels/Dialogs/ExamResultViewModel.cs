using System.Reactive;
using Examina.Models;
using Examina.Models.BenchSuite;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
            {
                return "未知";
            }

            int minutes = ActualDurationMinutes.Value;
            int hours = minutes / 60;
            int remainingMinutes = minutes % 60;

            return hours > 0 ? $"{hours}小时{remainingMinutes}分钟" : $"{remainingMinutes}分钟";
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
            {
                return "计算中...";
            }
            else
            {
                return Score.HasValue ? $"{Score:F1}" : "暂无评分";
            }
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

    /// <summary>
    /// 详细分数信息
    /// </summary>
    [Reactive] public ExamScoreDetail? ScoreDetail { get; set; }

    /// <summary>
    /// 是否有详细分数信息
    /// </summary>
    public bool HasScoreDetail => ScoreDetail != null;

    /// <summary>
    /// 是否显示详细分数区域
    /// </summary>
    public bool ShowDetailedScore => HasScoreDetail && IsSubmissionSuccessful;

    /// <summary>
    /// 总分显示文本
    /// </summary>
    public string TotalScoreText
    {
        get
        {
            if (ScoreDetail != null)
            {
                return $"{ScoreDetail.AchievedScore:F1} / {ScoreDetail.TotalScore:F1}";
            }
            else if (Score.HasValue && TotalScore.HasValue)
            {
                return $"{Score:F1} / {TotalScore:F1}";
            }
            else
            {
                return Score.HasValue ? $"{Score:F1}" : "暂无评分";
            }
        }
    }

    /// <summary>
    /// 得分百分比文本
    /// </summary>
    public string ScorePercentageText
    {
        get
        {
            if (ScoreDetail != null)
            {
                return $"{ScoreDetail.ScorePercentage:F1}%";
            }
            else
            {
                return Score.HasValue && TotalScore.HasValue && TotalScore > 0 ? $"{Score.Value / TotalScore.Value * 100:F1}%" : "暂无评分";
            }
        }
    }

    /// <summary>
    /// 成绩等级文本
    /// </summary>
    public string GradeText => ScoreDetail?.Grade ?? "暂无评分";

    /// <summary>
    /// 成绩等级颜色
    /// </summary>
    public string GradeColor => ScoreDetail?.GradeColor ?? "#666666";

    /// <summary>
    /// 通过状态文本
    /// </summary>
    public string PassStatusText => ScoreDetail != null ? ScoreDetail.IsPassed ? "通过" : "未通过" : "暂无评分";

    /// <summary>
    /// 通过状态图标
    /// </summary>
    public string PassStatusIcon => ScoreDetail != null ? ScoreDetail.IsPassed ? "✓" : "✗" : "?";

    /// <summary>
    /// 通过状态颜色
    /// </summary>
    public string PassStatusColor => ScoreDetail != null ? ScoreDetail.IsPassed ? "#4CAF50" : "#F44336" : "#666666";

    /// <summary>
    /// 是否显示分数信息区域
    /// 对于上机统考（正式考试、重考）显示分数区域，对于练习模式不显示
    /// </summary>
    public bool ShowScoreInfo
    {
        get
        {
            // 上机统考包括正式考试和重考，都应该显示分数区域
            bool isOnlineExam = ExamType == ExamType.FormalExam;
            bool result = isOnlineExam && IsSubmissionSuccessful;
            System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: ShowScoreInfo - ExamType: {ExamType}, IsSubmissionSuccessful: {IsSubmissionSuccessful}, IsOnlineExam: {isOnlineExam}, Result: {result}");
            return result;
        }
    }

    /// <summary>
    /// 次要状态消息
    /// </summary>
    public string SecondaryStatusMessage
    {
        get
        {
            string result;
            if (ExamType == ExamType.FormalExam && IsSubmissionSuccessful)
            {
                // 上机统考（包括正式考试和重考）成功提交时的消息
                result = "考试已成功提交，成绩将在稍后公布";
            }
            else if (ExamType == ExamType.Practice && IsSubmissionSuccessful)
            {
                // 练习模式成功提交时的消息
                result = "练习已完成，感谢您的参与";
            }
            else
            {
                // 其他情况（提交失败或其他考试类型）
                result = "感谢您的参与";
            }

            System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: SecondaryStatusMessage - ExamType: {ExamType}, IsSubmissionSuccessful: {IsSubmissionSuccessful}, Result: {result}");
            return result;
        }
    }

    public ExamResultViewModel()
    {
        // 初始化命令
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("ExamResultViewModel: 确认命令被执行");
            return true;
        });

        // 监听属性变化，更新计算属性
        _ = this.WhenAnyValue(x => x.Score, x => x.IsScoring)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ScoreText));
            });

        _ = this.WhenAnyValue(x => x.ActualDurationMinutes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ActualDurationText)));

        _ = this.WhenAnyValue(x => x.IsSubmissionSuccessful)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(SubmissionStatusText));
                this.RaisePropertyChanged(nameof(SubmissionStatusIcon));
                this.RaisePropertyChanged(nameof(SubmissionStatusColor));
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
            });

        _ = this.WhenAnyValue(x => x.ExamType)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ExamTypeText));
                this.RaisePropertyChanged(nameof(ShowScoreInfo));
                this.RaisePropertyChanged(nameof(SecondaryStatusMessage));
            });

        _ = this.WhenAnyValue(x => x.ErrorMessage)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasError)));

        _ = this.WhenAnyValue(x => x.Notes)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasNotes)));

        _ = this.WhenAnyValue(x => x.ScoreDetail)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasScoreDetail));
                this.RaisePropertyChanged(nameof(ShowDetailedScore));
                this.RaisePropertyChanged(nameof(TotalScoreText));
                this.RaisePropertyChanged(nameof(ScorePercentageText));
                this.RaisePropertyChanged(nameof(GradeText));
                this.RaisePropertyChanged(nameof(GradeColor));
                this.RaisePropertyChanged(nameof(PassStatusText));
                this.RaisePropertyChanged(nameof(PassStatusIcon));
                this.RaisePropertyChanged(nameof(PassStatusColor));
            });

        System.Diagnostics.Debug.WriteLine("ExamResultViewModel: 初始化完成");
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

        // 手动触发计算属性的更新通知
        this.RaisePropertyChanged(nameof(ShowScoreInfo));
        this.RaisePropertyChanged(nameof(SecondaryStatusMessage));

        System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: 设置考试结果 - {examName}, 类型: {examType}, 成功: {isSuccessful}, 得分: {score}");
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

    /// <summary>
    /// 设置详细分数信息
    /// </summary>
    public void SetScoreDetail(ExamScoreDetail scoreDetail)
    {
        ScoreDetail = scoreDetail;

        // 同时更新基础分数信息
        if (scoreDetail != null)
        {
            Score = scoreDetail.AchievedScore;
            TotalScore = scoreDetail.TotalScore;
        }

        System.Diagnostics.Debug.WriteLine($"ExamResultViewModel: 设置详细分数信息 - 总分: {scoreDetail?.TotalScore}, 得分: {scoreDetail?.AchievedScore}");
    }

    /// <summary>
    /// 从BenchSuite评分结果创建详细分数信息
    /// </summary>
    public void SetScoreDetailFromBenchSuite(BenchSuiteScoringResult benchSuiteResult, decimal passThreshold = 60)
    {
        if (benchSuiteResult == null)
        {
            return;
        }

        ExamScoreDetail scoreDetail = new()
        {
            TotalScore = benchSuiteResult.TotalScore,
            AchievedScore = benchSuiteResult.AchievedScore,
            IsPassed = benchSuiteResult.ScoreRate * 100 >= passThreshold,
            PassThreshold = passThreshold
        };

        // 更新统计信息
        scoreDetail.Statistics.TotalQuestions = benchSuiteResult.FileTypeResults.Count;
        scoreDetail.Statistics.CorrectQuestions = benchSuiteResult.FileTypeResults.Count(kvp => kvp.Value.IsSuccess);

        SetScoreDetail(scoreDetail);
    }
}
