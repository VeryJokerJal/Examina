using System;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// 文档生成结果
/// </summary>
public class DocumentGenerationResult
{
    /// <summary>
    /// 是否生成成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 生成的文件路径
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 已处理的操作点数量
    /// </summary>
    public int ProcessedOperationPoints { get; set; }

    /// <summary>
    /// 总操作点数量
    /// </summary>
    public int TotalOperationPoints { get; set; }

    /// <summary>
    /// 生成耗时
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 创建成功的结果
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="processedOperationPoints">已处理操作点数量</param>
    /// <param name="totalOperationPoints">总操作点数量</param>
    /// <param name="duration">耗时</param>
    /// <param name="details">详细信息</param>
    /// <returns>生成结果</returns>
    public static DocumentGenerationResult Success(string filePath, int processedOperationPoints, int totalOperationPoints, TimeSpan duration, string? details = null)
    {
        DateTime now = DateTime.Now;
        return new DocumentGenerationResult
        {
            IsSuccess = true,
            FilePath = filePath,
            ProcessedOperationPoints = processedOperationPoints,
            TotalOperationPoints = totalOperationPoints,
            Duration = duration,
            Details = details,
            StartTime = now - duration,
            EndTime = now
        };
    }

    /// <summary>
    /// 创建失败的结果
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="details">详细信息</param>
    /// <returns>生成结果</returns>
    public static DocumentGenerationResult Failure(string errorMessage, string? details = null)
    {
        DateTime now = DateTime.Now;
        return new DocumentGenerationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Details = details,
            StartTime = now,
            EndTime = now,
            Duration = TimeSpan.Zero
        };
    }
}
