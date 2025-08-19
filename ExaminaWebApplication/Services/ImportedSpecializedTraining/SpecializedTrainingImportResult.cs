namespace ExaminaWebApplication.Services.ImportedSpecializedTraining;

/// <summary>
/// 专项训练导入结果
/// </summary>
public class SpecializedTrainingImportResult
{
    /// <summary>
    /// 是否导入成功
    /// </summary>
    public bool IsSuccess { get; set; } = false;

    /// <summary>
    /// 导入的专项训练ID
    /// </summary>
    public int? ImportedSpecializedTrainingId { get; set; }

    /// <summary>
    /// 导入的专项训练名称
    /// </summary>
    public string? ImportedSpecializedTrainingName { get; set; }

    /// <summary>
    /// 导入的文件名
    /// </summary>
    public string? FileName { get; set; }

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
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 导入统计信息
    /// </summary>
    public SpecializedTrainingImportStatistics Statistics { get; set; } = new();

    /// <summary>
    /// 处理时长
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}

/// <summary>
/// 专项训练导入统计信息
/// </summary>
public class SpecializedTrainingImportStatistics
{
    /// <summary>
    /// 导入的模块数量
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// 导入的题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 导入的操作点数量
    /// </summary>
    public int OperationPointCount { get; set; }

    /// <summary>
    /// 导入的参数数量
    /// </summary>
    public int ParameterCount { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }
}
