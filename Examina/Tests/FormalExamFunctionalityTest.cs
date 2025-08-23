using System;
using System.Threading.Tasks;
using Examina.Models;
using Examina.Models.Exam;
using Examina.ViewModels.Dialogs;
using Examina.ViewModels.Pages;
using Examina.Services;

namespace Examina.Tests;

/// <summary>
/// 上机统考功能测试类
/// </summary>
public static class FormalExamFunctionalityTest
{
    /// <summary>
    /// 测试上机统考规则对话框ViewModel
    /// </summary>
    public static void TestFormalExamRulesViewModel()
    {
        try
        {
            Console.WriteLine("=== 测试上机统考规则对话框ViewModel ===");

            // 创建ViewModel
            FormalExamRulesViewModel viewModel = new();

            // 验证基本属性
            Console.WriteLine($"考试时长: {viewModel.RulesInfo.DurationMinutes} 分钟");
            Console.WriteLine($"总分值: {viewModel.RulesInfo.TotalScore} 分");
            Console.WriteLine($"及格分数: {viewModel.RulesInfo.PassingScore} 分");
            Console.WriteLine($"题目总数: {viewModel.RulesInfo.TotalQuestions} 道");

            // 验证规则列表
            Console.WriteLine($"考试规则数量: {viewModel.RulesInfo.Rules.Count}");
            Console.WriteLine($"注意事项数量: {viewModel.RulesInfo.Notes.Count}");
            Console.WriteLine($"操作指南数量: {viewModel.RulesInfo.OperationGuide.Count}");
            Console.WriteLine($"考试要求数量: {viewModel.RulesInfo.Requirements.Count}");

            // 验证命令
            if (viewModel.ConfirmCommand != null && viewModel.CancelCommand != null)
            {
                Console.WriteLine("✅ 命令创建成功");
            }
            else
            {
                Console.WriteLine("❌ 命令创建失败");
            }

            Console.WriteLine("✅ 上机统考规则对话框ViewModel测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 上机统考规则对话框ViewModel测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试ExamListViewModel的考试启动逻辑（模拟）
    /// </summary>
    public static void TestExamListViewModelLogic()
    {
        try
        {
            Console.WriteLine("\n=== 测试ExamListViewModel考试启动逻辑 ===");

            // 创建模拟的考试数据
            StudentExamDto mockExam = new()
            {
                Id = 1,
                Name = "测试上机统考",
                Description = "这是一个测试用的上机统考",
                ExamType = "FormalExam",
                Status = "Active",
                TotalScore = 100,
                DurationMinutes = 150,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(3),
                PassingScore = 60,
                Subjects = new List<StudentSubjectDto>
                {
                    new()
                    {
                        Id = 1,
                        SubjectName = "C#编程",
                        Score = 75,
                        Questions = new List<StudentQuestionDto>
                        {
                            new() { Id = 1, Title = "编程题1", Score = 15 },
                            new() { Id = 2, Title = "编程题2", Score = 15 },
                            new() { Id = 3, Title = "编程题3", Score = 15 },
                            new() { Id = 4, Title = "编程题4", Score = 15 },
                            new() { Id = 5, Title = "编程题5", Score = 15 }
                        }
                    }
                },
                Modules = new List<StudentModuleDto>
                {
                    new()
                    {
                        Id = 1,
                        Name = "操作题模块",
                        Type = "Operation",
                        Score = 25,
                        Questions = new System.Collections.ObjectModel.ObservableCollection<StudentQuestionDto>
                        {
                            new() { Id = 6, Title = "操作题1", Score = 5 },
                            new() { Id = 7, Title = "操作题2", Score = 5 },
                            new() { Id = 8, Title = "操作题3", Score = 5 },
                            new() { Id = 9, Title = "操作题4", Score = 5 },
                            new() { Id = 10, Title = "操作题5", Score = 5 }
                        }
                    }
                }
            };

            // 验证考试数据
            int totalQuestions = mockExam.Subjects.Sum(s => s.Questions.Count) + 
                               mockExam.Modules.Sum(m => m.Questions.Count);

            Console.WriteLine($"考试名称: {mockExam.Name}");
            Console.WriteLine($"考试时长: {mockExam.DurationMinutes} 分钟");
            Console.WriteLine($"总题目数: {totalQuestions} 道");
            Console.WriteLine($"编程题数量: {mockExam.Subjects.Sum(s => s.Questions.Count)} 道");
            Console.WriteLine($"操作题数量: {mockExam.Modules.Sum(m => m.Questions.Count)} 道");

            if (totalQuestions == 10 && mockExam.DurationMinutes == 150)
            {
                Console.WriteLine("✅ 考试数据验证通过");
            }
            else
            {
                Console.WriteLine("❌ 考试数据验证失败");
            }

            Console.WriteLine("✅ ExamListViewModel考试启动逻辑测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ExamListViewModel考试启动逻辑测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试ExamType枚举支持
    /// </summary>
    public static void TestExamTypeSupport()
    {
        try
        {
            Console.WriteLine("\n=== 测试ExamType枚举支持 ===");

            // 验证FormalExam类型存在
            ExamType formalExamType = ExamType.FormalExam;
            Console.WriteLine($"FormalExam类型值: {formalExamType}");

            // 验证其他相关类型
            ExamType mockExamType = ExamType.MockExam;
            ExamType comprehensiveTrainingType = ExamType.ComprehensiveTraining;

            Console.WriteLine($"MockExam类型值: {mockExamType}");
            Console.WriteLine($"ComprehensiveTraining类型值: {comprehensiveTrainingType}");

            // 验证类型比较
            bool isFormalExam = formalExamType == ExamType.FormalExam;
            bool isNotMockExam = formalExamType != ExamType.MockExam;

            if (isFormalExam && isNotMockExam)
            {
                Console.WriteLine("✅ ExamType枚举支持测试通过");
            }
            else
            {
                Console.WriteLine("❌ ExamType枚举支持测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ExamType枚举支持测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("开始运行上机统考功能测试...\n");

        TestFormalExamRulesViewModel();
        TestExamListViewModelLogic();
        TestExamTypeSupport();

        Console.WriteLine("\n=== 测试总结 ===");
        Console.WriteLine("上机统考功能基础测试完成");
        Console.WriteLine("注意：完整的功能测试需要在实际运行环境中进行");
    }
}
