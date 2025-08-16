namespace ExaminaWebApplication.Services.ImportedExam;

/// <summary>
/// 考试导入结果
/// </summary>
public class ExamImportResult
{
    /// <summary>
    /// 是否导入成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 导入的考试ID
    /// </summary>
    public int? ImportedExamId { get; set; }

    /// <summary>
    /// 导入的考试名称
    /// </summary>
    public string? ImportedExamName { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 导入者ID
    /// </summary>
    public int ImportedBy { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 导入耗时
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// 总科目数
    /// </summary>
    public int TotalSubjects { get; set; }

    /// <summary>
    /// 总模块数
    /// </summary>
    public int TotalModules { get; set; }

    /// <summary>
    /// 总题目数
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 总操作点数
    /// </summary>
    public int TotalOperationPoints { get; set; }
}
