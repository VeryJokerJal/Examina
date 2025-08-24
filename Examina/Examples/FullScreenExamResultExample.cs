using System;
using System.Threading.Tasks;
using Examina.Models;
using Examina.Views.Dialogs;

namespace Examina.Examples;

/// <summary>
/// 全屏考试结果窗口使用示例
/// </summary>
public static class FullScreenExamResultExample
{
    /// <summary>
    /// 显示模拟考试成功结果示例
    /// </summary>
    public static async Task ShowMockExamSuccessExample()
    {
        try
        {
            Console.WriteLine("🎯 显示模拟考试成功结果示例");

            await FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName: "计算机二级模拟考试",
                examType: ExamType.MockExam,
                isSuccessful: true,
                startTime: DateTime.Now.AddHours(-2),
                endTime: DateTime.Now,
                durationMinutes: 120,
                score: null, // 模拟考试不显示分数
                totalScore: null,
                errorMessage: "",
                notes: "模拟考试提交成功，感谢您的参与！",
                showContinue: true,
                showClose: false
            );

            Console.WriteLine("✅ 模拟考试成功结果窗口已关闭");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 显示模拟考试成功结果失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示上机统考成功结果示例（带评分）
    /// </summary>
    public static async Task ShowFormalExamSuccessExample()
    {
        try
        {
            Console.WriteLine("🎯 显示上机统考成功结果示例");

            // 创建窗口并显示
            FullScreenExamResultWindow window = FullScreenExamResultWindow.ShowFullScreenExamResult(
                examName: "全国计算机等级考试二级C语言",
                examType: ExamType.FormalExam,
                isSuccessful: true,
                startTime: DateTime.Now.AddHours(-2.5),
                endTime: DateTime.Now,
                durationMinutes: 150,
                score: null, // 初始为空，模拟异步评分
                totalScore: 100,
                errorMessage: "",
                notes: "考试提交成功，正在计算成绩...",
                showContinue: true,
                showClose: false
            );

            // 开始评分计算
            window.StartScoring();

            // 模拟异步评分过程
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000); // 模拟评分耗时

                // 模拟评分完成
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    window.UpdateScore(85.5m, 100m, "BenchSuite自动评分完成");
                });
            });

            // 等待窗口关闭
            await window.WaitForCloseAsync();

            Console.WriteLine("✅ 上机统考成功结果窗口已关闭");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 显示上机统考成功结果失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示考试失败结果示例
    /// </summary>
    public static async Task ShowExamFailureExample()
    {
        try
        {
            Console.WriteLine("🎯 显示考试失败结果示例");

            await FullScreenExamResultWindow.ShowFullScreenExamResultAsync(
                examName: "计算机二级模拟考试",
                examType: ExamType.MockExam,
                isSuccessful: false,
                startTime: DateTime.Now.AddMinutes(-30),
                endTime: DateTime.Now,
                durationMinutes: 30,
                score: null,
                totalScore: null,
                errorMessage: "网络连接超时，考试提交失败",
                notes: "请检查网络连接或联系管理员",
                showContinue: true,
                showClose: false
            );

            Console.WriteLine("✅ 考试失败结果窗口已关闭");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 显示考试失败结果失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示评分失败结果示例
    /// </summary>
    public static async Task ShowScoringFailureExample()
    {
        try
        {
            Console.WriteLine("🎯 显示评分失败结果示例");

            // 创建窗口并显示
            FullScreenExamResultWindow window = FullScreenExamResultWindow.ShowFullScreenExamResult(
                examName: "全国计算机等级考试二级C语言",
                examType: ExamType.FormalExam,
                isSuccessful: true,
                startTime: DateTime.Now.AddHours(-2.5),
                endTime: DateTime.Now,
                durationMinutes: 150,
                score: null,
                totalScore: 100,
                errorMessage: "",
                notes: "考试提交成功，正在计算成绩...",
                showContinue: true,
                showClose: false
            );

            // 开始评分计算
            window.StartScoring();

            // 模拟异步评分失败
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // 模拟评分耗时

                // 模拟评分失败
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    window.ScoringFailed("BenchSuite评分服务不可用，请联系管理员");
                });
            });

            // 等待窗口关闭
            await window.WaitForCloseAsync();

            Console.WriteLine("✅ 评分失败结果窗口已关闭");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 显示评分失败结果失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行所有示例
    /// </summary>
    public static async Task RunAllExamples()
    {
        Console.WriteLine("🚀 开始运行全屏考试结果窗口示例");
        Console.WriteLine("=" * 50);

        await ShowMockExamSuccessExample();
        Console.WriteLine();

        await ShowFormalExamSuccessExample();
        Console.WriteLine();

        await ShowExamFailureExample();
        Console.WriteLine();

        await ShowScoringFailureExample();
        Console.WriteLine();

        Console.WriteLine("🎉 所有示例运行完成");
    }
}
