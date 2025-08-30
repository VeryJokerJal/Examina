using System;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// 文档生成进度信息
/// </summary>
public class DocumentGenerationProgress
{
    /// <summary>
    /// 当前步骤描述
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// 进度百分比（0-100）
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// 已处理的项目数量
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// 总项目数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前正在处理的操作点
    /// </summary>
    public string? CurrentOperationPoint { get; set; }

    /// <summary>
    /// 额外的详细信息
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 创建进度信息
    /// </summary>
    /// <param name="currentStep">当前步骤</param>
    /// <param name="processedCount">已处理数量</param>
    /// <param name="totalCount">总数量</param>
    /// <param name="currentOperationPoint">当前操作点</param>
    /// <returns>进度信息</returns>
    public static DocumentGenerationProgress Create(string currentStep, int processedCount, int totalCount, string? currentOperationPoint = null)
    {
        int progressPercentage = totalCount > 0 ? (int)Math.Round((double)processedCount / totalCount * 100) : 0;

        return new DocumentGenerationProgress
        {
            CurrentStep = currentStep,
            ProcessedCount = processedCount,
            TotalCount = totalCount,
            ProgressPercentage = progressPercentage,
            CurrentOperationPoint = currentOperationPoint
        };
    }
}
