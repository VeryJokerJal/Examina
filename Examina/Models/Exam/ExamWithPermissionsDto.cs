using Examina.Models.ExamAttempt;
using Examina.ViewModels.Pages;

namespace Examina.Models.Exam;

/// <summary>
/// 包含权限信息的考试DTO
/// </summary>
public class ExamWithPermissionsDto
{
    /// <summary>
    /// 考试信息
    /// </summary>
    public StudentExamDto Exam { get; set; } = new();

    /// <summary>
    /// 考试权限限制信息
    /// </summary>
    public ExamAttemptLimitDto? AttemptLimit { get; set; }

    /// <summary>
    /// 是否可以开始考试（任何模式）
    /// </summary>
    public bool CanStartAnyMode => AttemptLimit?.CanStartExam == true || 
                                   AttemptLimit?.CanRetake == true || 
                                   AttemptLimit?.CanPractice == true;

    /// <summary>
    /// 主要按钮文本
    /// </summary>
    public string PrimaryButtonText
    {
        get
        {
            if (AttemptLimit == null)
                return "加载中...";

            // 如果没有完成首次考试，显示"开始考试"
            if (!AttemptLimit.HasCompletedFirstAttempt)
                return "开始考试";

            // 如果已完成首次考试，根据可用选项显示文本
            if (AttemptLimit.CanRetake)
                return "重新考试";
            
            if (AttemptLimit.CanPractice)
                return "练习模式";

            // 如果都不能，显示完成状态
            return "考试已完成";
        }
    }

    /// <summary>
    /// 主要按钮是否可用
    /// </summary>
    public bool IsPrimaryButtonEnabled => CanStartAnyMode;

    /// <summary>
    /// 主要按钮是否可见
    /// </summary>
    public bool IsPrimaryButtonVisible
    {
        get
        {
            // 检查考试时间和状态
            bool timeValid = Exam.StartTime.HasValue && Exam.EndTime.HasValue &&
                           DateTime.Now >= Exam.StartTime.Value && DateTime.Now <= Exam.EndTime.Value;
            
            bool statusValid = Exam.Status == "Published" || Exam.Status == "InProgress";
            
            return timeValid && statusValid;
        }
    }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage
    {
        get
        {
            if (AttemptLimit == null)
                return "正在检查权限...";

            if (!IsPrimaryButtonVisible)
            {
                if (DateTime.Now < Exam.StartTime)
                    return "考试尚未开始";
                if (DateTime.Now > Exam.EndTime)
                    return "考试已结束";
                return "考试不可用";
            }

            if (!CanStartAnyMode)
                return AttemptLimit.LimitReason ?? "无法参加考试";

            if (AttemptLimit.HasCompletedFirstAttempt)
            {
                var options = new List<string>();
                if (AttemptLimit.CanRetake)
                    options.Add($"重考({AttemptLimit.RetakeAttempts}/{Exam.MaxRetakeCount})");
                if (AttemptLimit.CanPractice)
                    options.Add("练习");
                
                return options.Count > 0 ? $"可选择: {string.Join(", ", options)}" : "考试已完成";
            }

            return "可以开始考试";
        }
    }

    /// <summary>
    /// 考试次数统计文本
    /// </summary>
    public string AttemptCountText
    {
        get
        {
            if (AttemptLimit == null)
                return "";

            if (AttemptLimit.TotalAttempts == 0)
                return "尚未参加";

            var parts = new List<string> { $"总计{AttemptLimit.TotalAttempts}次" };
            
            if (AttemptLimit.RetakeAttempts > 0)
                parts.Add($"重考{AttemptLimit.RetakeAttempts}次");
            
            if (AttemptLimit.PracticeAttempts > 0)
                parts.Add($"练习{AttemptLimit.PracticeAttempts}次");

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// 获取推荐的考试模式
    /// </summary>
    public ExamMode GetRecommendedMode()
    {
        if (AttemptLimit == null)
            return ExamMode.Normal;

        if (!AttemptLimit.HasCompletedFirstAttempt)
            return ExamMode.Normal;

        if (AttemptLimit.CanRetake)
            return ExamMode.Retake;

        if (AttemptLimit.CanPractice)
            return ExamMode.Practice;

        return ExamMode.Normal;
    }
}


