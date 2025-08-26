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
            {
                return "开始考试";
            }

            // 如果没有完成首次考试，显示"开始考试"
            if (!AttemptLimit.HasCompletedFirstAttempt)
            {
                return "开始考试";
            }

            // 如果已完成首次考试，根据可用选项显示文本
            if (AttemptLimit.CanRetake)
            {
                return "重新考试";
            }

            if (AttemptLimit.CanPractice)
            {
                return "练习模式";
            }

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

            bool statusValid = Exam.Status is "Published" or "InProgress";

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
            {
                return "";
            }

            if (!IsPrimaryButtonVisible)
            {
                if (DateTime.Now < Exam.StartTime)
                {
                    return "考试尚未开始";
                }

                return DateTime.Now > Exam.EndTime ? "考试已结束" : "";
            }

            // 不显示技术性的限制原因和详细信息，保持界面简洁
            return "";
        }
    }

    /// <summary>
    /// 考试次数统计文本
    /// </summary>
    public string AttemptCountText
    {
        get
        {
            // 不显示考试次数统计，保持界面简洁
            return "";
        }
    }

    /// <summary>
    /// 获取推荐的考试模式
    /// </summary>
    public ExamMode GetRecommendedMode()
    {
        if (AttemptLimit == null)
        {
            return ExamMode.Normal;
        }

        if (!AttemptLimit.HasCompletedFirstAttempt)
        {
            return ExamMode.Normal;
        }

        if (AttemptLimit.CanRetake)
        {
            return ExamMode.Retake;
        }

        return AttemptLimit.CanPractice ? ExamMode.Practice : ExamMode.Normal;
    }
}


