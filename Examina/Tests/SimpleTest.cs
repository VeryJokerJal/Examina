using System;
using System.Text.Json;

namespace Examina.Tests;

/// <summary>
/// 专项练习进度统计DTO（复制自主项目）
/// </summary>
public class SpecialPracticeProgressDto
{
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public double CompletionPercentage { get; set; }
    public int InProgressCount { get; set; }
    public int NotStartedCount { get; set; }
    public string? LastCompletedPracticeName { get; set; }
    public DateTime? LastCompletedAt { get; set; }
}

/// <summary>
/// 简化的专项练习功能测试
/// </summary>
public class SimpleTest
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== 专项练习功能核心逻辑测试 ===\n");

        TestSpecialPracticeProgressDto();
        TestProgressCalculation();
        TestJsonSerialization();

        Console.WriteLine("=== 所有测试完成 ===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 测试专项练习进度DTO
    /// </summary>
    static void TestSpecialPracticeProgressDto()
    {
        Console.WriteLine("1. 测试专项练习进度DTO创建和属性设置");

        SpecialPracticeProgressDto progress = new()
        {
            TotalCount = 50,
            CompletedCount = 15,
            CompletionPercentage = 30.0,
            InProgressCount = 3,
            NotStartedCount = 32,
            LastCompletedPracticeName = "Windows文件管理操作",
            LastCompletedAt = DateTime.Now.AddHours(-2)
        };

        Console.WriteLine($"   总练习数量: {progress.TotalCount}");
        Console.WriteLine($"   已完成数量: {progress.CompletedCount}");
        Console.WriteLine($"   完成百分比: {progress.CompletionPercentage}%");
        Console.WriteLine($"   进行中数量: {progress.InProgressCount}");
        Console.WriteLine($"   未开始数量: {progress.NotStartedCount}");
        Console.WriteLine($"   最近完成练习: {progress.LastCompletedPracticeName}");
        Console.WriteLine($"   最近完成时间: {progress.LastCompletedAt}");

        // 验证数据一致性
        int calculatedNotStarted = progress.TotalCount - progress.CompletedCount - progress.InProgressCount;
        if (calculatedNotStarted == progress.NotStartedCount)
        {
            Console.WriteLine("   ✅ 数据一致性验证通过");
        }
        else
        {
            Console.WriteLine("   ❌ 数据一致性验证失败");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 测试进度计算逻辑
    /// </summary>
    static void TestProgressCalculation()
    {
        Console.WriteLine("2. 测试进度计算逻辑");

        // 测试用例
        (int total, int completed, double expectedPercentage)[] testCases = [
            (0, 0, 0.0),
            (50, 0, 0.0),
            (50, 15, 30.0),
            (50, 25, 50.0),
            (50, 50, 100.0),
            (100, 33, 33.0)
        ];

        foreach (var (total, completed, expectedPercentage) in testCases)
        {
            double calculatedPercentage = total > 0 ? (double)completed / total * 100 : 0;
            string progressText = $"{completed}/{total}";
            string percentageText = $"{calculatedPercentage:F1}% 完成";

            Console.WriteLine($"   {progressText} -> {percentageText}");

            if (Math.Abs(calculatedPercentage - expectedPercentage) < 0.1)
            {
                Console.WriteLine($"   ✅ 百分比计算正确");
            }
            else
            {
                Console.WriteLine($"   ❌ 百分比计算错误，期望: {expectedPercentage}%, 实际: {calculatedPercentage:F1}%");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 测试JSON序列化
    /// </summary>
    static void TestJsonSerialization()
    {
        Console.WriteLine("3. 测试JSON序列化和反序列化");

        SpecialPracticeProgressDto originalProgress = new()
        {
            TotalCount = 45,
            CompletedCount = 12,
            CompletionPercentage = 26.7,
            InProgressCount = 2,
            NotStartedCount = 31,
            LastCompletedPracticeName = "Windows系统设置与配置",
            LastCompletedAt = DateTime.Now.AddHours(-3)
        };

        try
        {
            // 序列化
            string json = JsonSerializer.Serialize(originalProgress);
            Console.WriteLine($"   JSON序列化成功，长度: {json.Length} 字符");

            // 反序列化
            SpecialPracticeProgressDto? deserializedProgress = JsonSerializer.Deserialize<SpecialPracticeProgressDto>(json);

            if (deserializedProgress != null)
            {
                Console.WriteLine("   JSON反序列化成功");

                // 验证数据完整性
                bool dataIntegrityOk = 
                    deserializedProgress.TotalCount == originalProgress.TotalCount &&
                    deserializedProgress.CompletedCount == originalProgress.CompletedCount &&
                    Math.Abs(deserializedProgress.CompletionPercentage - originalProgress.CompletionPercentage) < 0.1 &&
                    deserializedProgress.InProgressCount == originalProgress.InProgressCount &&
                    deserializedProgress.NotStartedCount == originalProgress.NotStartedCount &&
                    deserializedProgress.LastCompletedPracticeName == originalProgress.LastCompletedPracticeName;

                if (dataIntegrityOk)
                {
                    Console.WriteLine("   ✅ 序列化/反序列化数据完整性验证通过");
                }
                else
                {
                    Console.WriteLine("   ❌ 序列化/反序列化数据完整性验证失败");
                }
            }
            else
            {
                Console.WriteLine("   ❌ JSON反序列化失败，返回null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ JSON序列化/反序列化异常: {ex.Message}");
        }

        Console.WriteLine();
    }
}
