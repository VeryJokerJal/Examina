namespace Examina.Models.Ranking;

/// <summary>
/// 排行榜条目DTO
/// </summary>
public class RankingEntryDto
{
    /// <summary>
    /// 排名
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    public int StudentUserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 最大得分
    /// </summary>
    public decimal MaxScore { get; set; }

    /// <summary>
    /// 完成百分比
    /// </summary>
    public decimal CompletionPercentage { get; set; }

    /// <summary>
    /// 用时（秒）
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// 用时显示文本
    /// </summary>
    public string DurationText { get; set; } = string.Empty;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// 考试/训练名称
    /// </summary>
    public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 学校名称
    /// </summary>
    public string? SchoolName { get; set; }

    /// <summary>
    /// 班级名称
    /// </summary>
    public string? ClassName { get; set; }
}

/// <summary>
/// 排行榜响应DTO
/// </summary>
public class RankingResponseDto
{
    /// <summary>
    /// 排行榜类型
    /// </summary>
    public RankingType Type { get; set; }

    /// <summary>
    /// 排行榜类型名称
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 排行榜条目列表
    /// </summary>
    public List<RankingEntryDto> Entries { get; set; } = [];

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 排行榜类型枚举
/// </summary>
public enum RankingType
{
    /// <summary>
    /// 上机统考排行榜
    /// </summary>
    ExamRanking = 1,

    /// <summary>
    /// 模拟考试排行榜
    /// </summary>
    MockExamRanking = 2,

    /// <summary>
    /// 综合实训排行榜
    /// </summary>
    TrainingRanking = 3
}
