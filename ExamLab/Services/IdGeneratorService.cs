using System;
using System.Threading;

namespace ExamLab.Services;

/// <summary>
/// ID生成服务 - 提供线程安全的唯一ID生成
/// </summary>
public static class IdGeneratorService
{
    private static long _examCounter = 0;
    private static long _moduleCounter = 0;
    private static long _questionCounter = 0;
    private static long _operationCounter = 0;
    private static long _parameterCounter = 0;

    /// <summary>
    /// 生成试卷ID
    /// </summary>
    /// <returns>格式: exam-{timestamp}-{counter}</returns>
    public static string GenerateExamId()
    {
        long counter = Interlocked.Increment(ref _examCounter);
        long timestamp = DateTime.UtcNow.Ticks;
        return $"exam-{timestamp:X}-{counter:X4}";
    }

    /// <summary>
    /// 生成模块ID
    /// </summary>
    /// <returns>格式: module-{timestamp}-{counter}</returns>
    public static string GenerateModuleId()
    {
        long counter = Interlocked.Increment(ref _moduleCounter);
        long timestamp = DateTime.UtcNow.Ticks;
        return $"module-{timestamp:X}-{counter:X4}";
    }

    /// <summary>
    /// 生成题目ID
    /// </summary>
    /// <returns>格式: question-{timestamp}-{counter}</returns>
    public static string GenerateQuestionId()
    {
        long counter = Interlocked.Increment(ref _questionCounter);
        long timestamp = DateTime.UtcNow.Ticks;
        return $"question-{timestamp:X}-{counter:X4}";
    }

    /// <summary>
    /// 生成操作点ID
    /// </summary>
    /// <returns>格式: operation-{timestamp}-{counter}</returns>
    public static string GenerateOperationId()
    {
        long counter = Interlocked.Increment(ref _operationCounter);
        long timestamp = DateTime.UtcNow.Ticks;
        return $"operation-{timestamp:X}-{counter:X4}";
    }

    /// <summary>
    /// 生成参数ID
    /// </summary>
    /// <returns>格式: parameter-{timestamp}-{counter}</returns>
    public static string GenerateParameterId()
    {
        long counter = Interlocked.Increment(ref _parameterCounter);
        long timestamp = DateTime.UtcNow.Ticks;
        return $"parameter-{timestamp:X}-{counter:X4}";
    }

    /// <summary>
    /// 生成基于GUID的ID
    /// </summary>
    /// <param name="prefix">ID前缀</param>
    /// <returns>格式: {prefix}-{guid}</returns>
    public static string GenerateGuidId(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 验证ID格式是否有效
    /// </summary>
    /// <param name="id">要验证的ID</param>
    /// <param name="expectedPrefix">期望的前缀</param>
    /// <returns>是否有效</returns>
    public static bool IsValidId(string id, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return id.StartsWith($"{expectedPrefix}-", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 重置所有计数器（仅用于测试）
    /// </summary>
    public static void ResetCounters()
    {
        Interlocked.Exchange(ref _examCounter, 0);
        Interlocked.Exchange(ref _moduleCounter, 0);
        Interlocked.Exchange(ref _questionCounter, 0);
        Interlocked.Exchange(ref _operationCounter, 0);
        Interlocked.Exchange(ref _parameterCounter, 0);
    }

    /// <summary>
    /// 获取当前计数器状态（用于调试）
    /// </summary>
    /// <returns>计数器状态信息</returns>
    public static string GetCounterStatus()
    {
        return $"Exam: {_examCounter}, Module: {_moduleCounter}, Question: {_questionCounter}, " +
               $"Operation: {_operationCounter}, Parameter: {_parameterCounter}";
    }
}
