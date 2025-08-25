using System;
using System.Threading.Tasks;

namespace ExamLab.Tests;

/// <summary>
/// 测试运行器
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static async Task RunAllTestsAsync()
    {
        try
        {
            Console.WriteLine("开始运行分值更新功能测试...\n");

            // 运行基本分值更新测试
            await ScoreUpdateTest.TestScoreUpdateChainAsync();

            // 运行反序列化测试
            await ScoreUpdateTest.TestDeserializationScoreUpdateAsync();

            Console.WriteLine("\n=== 测试完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n测试过程中发生错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
}
