using System;

namespace Examina.Tests;

/// <summary>
/// 考试分数详情测试运行器
/// </summary>
public static class RunExamScoreDetailTest
{
    /// <summary>
    /// 主测试方法
    /// </summary>
    public static void Main()
    {
        try
        {
            Console.WriteLine("开始运行考试分数详情功能测试...");
            Console.WriteLine($"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            ExamScoreDetailTest.RunAllTests();

            Console.WriteLine();
            Console.WriteLine("测试运行完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试运行异常: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
