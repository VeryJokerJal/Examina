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
            // 添加调试输出
            System.Diagnostics.Debug.WriteLine($"=== PrimaryButtonText 计算调试 ===");
            System.Diagnostics.Debug.WriteLine($"考试ID: {Exam.Id}, 考试名称: {Exam.Name}");
            System.Diagnostics.Debug.WriteLine($"AttemptLimit == null: {AttemptLimit == null}");

            if (AttemptLimit == null)
            {
                System.Diagnostics.Debug.WriteLine($"AttemptLimit为null，返回'开始考试'");
                return "开始考试";
            }

            System.Diagnostics.Debug.WriteLine($"HasCompletedFirstAttempt: {AttemptLimit.HasCompletedFirstAttempt}");
            System.Diagnostics.Debug.WriteLine($"CanRetake: {AttemptLimit.CanRetake}");
            System.Diagnostics.Debug.WriteLine($"CanPractice: {AttemptLimit.CanPractice}");

            // 如果没有完成首次考试，显示"开始考试"
            if (!AttemptLimit.HasCompletedFirstAttempt)
            {
                System.Diagnostics.Debug.WriteLine($"未完成首次考试，返回'开始考试'");
                return "开始考试";
            }

            // 如果已完成首次考试，根据可用选项显示文本
            if (AttemptLimit.CanRetake)
            {
                System.Diagnostics.Debug.WriteLine($"可以重考，返回'重新考试'");
                return "重新考试";
            }

            if (AttemptLimit.CanPractice)
            {
                System.Diagnostics.Debug.WriteLine($"可以练习，返回'练习模式'");
                return "练习模式";
            }

            // 如果都不能，显示完成状态
            System.Diagnostics.Debug.WriteLine($"都不能，返回'考试已完成'");
            System.Diagnostics.Debug.WriteLine($"=== PrimaryButtonText 计算调试结束 ===");
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

            // 按钮应该始终可见，让用户看到当前状态
            // 即使没有权限，也应该显示"考试已完成"等状态
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


