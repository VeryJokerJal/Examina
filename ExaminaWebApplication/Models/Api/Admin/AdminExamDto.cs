using ExaminaWebApplication.Models.ImportedExam;

namespace ExaminaWebApplication.Models.Api.Admin;

/// <summary>
/// 管理员考试DTO
/// </summary>
public class AdminExamDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 考试名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 考试描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 考试类型
    /// </summary>
    public string ExamType { get; set; } = string.Empty;

    /// <summary>
    /// 考试状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考（记录分数和排名）
    /// </summary>
    public bool AllowRetake { get; set; }

    /// <summary>
    /// 是否允许重做（不记录分数和排名，类似模拟考试）
    /// </summary>
    public bool AllowPractice { get; set; }

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; }

    /// <summary>
    /// 及格分数
    /// </summary>
    public decimal PassingScore { get; set; }

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; }

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; }

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 考试类型（全省统考/学校统考）
    /// </summary>
    public ExamCategory ExamCategory { get; set; }

    /// <summary>
    /// 考试标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 导入时间
    /// </summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// 导入文件名
    /// </summary>
    public string? ImportFileName { get; set; }

    /// <summary>
    /// 科目数量
    /// </summary>
    public int SubjectCount { get; set; }

    /// <summary>
    /// 模块数量
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 参与考试的学生数量
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// 已完成考试的学生数量
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 考试状态显示文本
    /// </summary>
    public string StatusDisplayText => GetStatusDisplayText();

    /// <summary>
    /// 考试类型显示文本
    /// </summary>
    public string CategoryDisplayText => ExamCategory == ExamCategory.Provincial ? "全省统考" : "学校统考";

    /// <summary>
    /// 是否可以编辑时间
    /// </summary>
    public bool CanEditSchedule => Status == "Draft" || Status == "Scheduled";

    /// <summary>
    /// 是否可以发布
    /// </summary>
    public bool CanPublish => Status == "Draft" && StartTime.HasValue && EndTime.HasValue;

    /// <summary>
    /// 是否可以开始
    /// </summary>
    public bool CanStart => Status == "Published" && StartTime.HasValue && DateTime.Now >= StartTime.Value;

    /// <summary>
    /// 是否已结束
    /// </summary>
    public bool IsEnded => EndTime.HasValue && DateTime.Now > EndTime.Value;

    /// <summary>
    /// 是否正在进行中
    /// </summary>
    public bool IsInProgress => Status == "InProgress" || 
        (Status == "Published" && StartTime.HasValue && EndTime.HasValue && 
         DateTime.Now >= StartTime.Value && DateTime.Now <= EndTime.Value);

    /// <summary>
    /// 获取状态显示文本
    /// </summary>
    private string GetStatusDisplayText()
    {
        return Status switch
        {
            "Draft" => "草稿",
            "Scheduled" => "已安排",
            "Published" => "已发布",
            "InProgress" => "进行中",
            "Completed" => "已结束",
            "Cancelled" => "已取消",
            _ => "未知状态"
        };
    }
}

/// <summary>
/// 设置考试时间请求DTO
/// </summary>
public class SetExamScheduleRequestDto
{
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }
}

/// <summary>
/// 更新考试状态请求DTO
/// </summary>
public class UpdateExamStatusRequestDto
{
    /// <summary>
    /// 考试状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 更新考试类型请求DTO
/// </summary>
public class UpdateExamCategoryRequestDto
{
    /// <summary>
    /// 考试类型
    /// </summary>
    public ExamCategory Category { get; set; }
}

/// <summary>
/// 更新试卷名称请求DTO
/// </summary>
public class UpdateExamNameRequestDto
{
    /// <summary>
    /// 新的试卷名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 更新试卷名称响应DTO
/// </summary>
public class UpdateExamNameResponseDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 更新后的试卷名称
    /// </summary>
    public string? UpdatedName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
