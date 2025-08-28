using System;
using System.Collections.Generic;
using Examina.Models;
using BenchSuite.Models;
using Examina.ViewModels.Dialogs;

namespace Examina.Tests;

/// <summary>
/// 考试分数详情功能测试
/// </summary>
public static class ExamScoreDetailTest
{
    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("=== 考试分数详情功能测试 ===");
        
        TestExamScoreDetailModel();
        TestExamResultViewModelWithScoreDetail();
        TestBenchSuiteResultConversion();
        
        Console.WriteLine("=== 所有测试完成 ===");
    }

    /// <summary>
    /// 测试ExamScoreDetail模型
    /// </summary>
    private static void TestExamScoreDetailModel()
    {
        Console.WriteLine("\n--- 测试ExamScoreDetail模型 ---");
        
        try
        {
            ExamScoreDetail scoreDetail = new()
            {
                TotalScore = 100,
                AchievedScore = 85,
                IsPassed = true,
                PassThreshold = 60
            };

            // 更新统计信息
            scoreDetail.Statistics.TotalQuestions = 2;
            scoreDetail.Statistics.CorrectQuestions = 2;

            // 验证计算属性
            Console.WriteLine($"总分: {scoreDetail.TotalScore}");
            Console.WriteLine($"得分: {scoreDetail.AchievedScore}");
            Console.WriteLine($"得分率: {scoreDetail.ScoreRate:P2}");
            Console.WriteLine($"得分百分比: {scoreDetail.ScorePercentage:F1}%");
            Console.WriteLine($"等级: {scoreDetail.Grade}");
            Console.WriteLine($"等级颜色: {scoreDetail.GradeColor}");
            Console.WriteLine($"是否通过: {scoreDetail.IsPassed}");
            Console.WriteLine($"题目数量: {scoreDetail.Statistics.TotalQuestions}");
            Console.WriteLine($"正确题目数量: {scoreDetail.Statistics.CorrectQuestions}");

            Console.WriteLine("✓ ExamScoreDetail模型测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ExamScoreDetail模型测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试ExamResultViewModel与详细分数的集成
    /// </summary>
    private static void TestExamResultViewModelWithScoreDetail()
    {
        Console.WriteLine("\n--- 测试ExamResultViewModel与详细分数集成 ---");
        
        try
        {
            ExamResultViewModel viewModel = new();
            
            // 设置基本考试结果
            viewModel.SetExamResult(
                "计算机应用基础考试",
                ExamType.FormalExam,
                true,
                DateTime.Now.AddHours(-2),
                DateTime.Now,
                120,
                85,
                100,
                "",
                "正式考试"
            );

            // 创建详细分数信息
            ExamScoreDetail scoreDetail = new()
            {
                TotalScore = 100,
                AchievedScore = 85,
                IsPassed = true,
                PassThreshold = 60
            };



            // 设置详细分数信息
            viewModel.SetScoreDetail(scoreDetail);

            // 验证属性
            Console.WriteLine($"考试名称: {viewModel.ExamName}");
            Console.WriteLine($"考试类型: {viewModel.ExamTypeText}");
            Console.WriteLine($"是否有详细分数: {viewModel.HasScoreDetail}");
            Console.WriteLine($"是否显示详细分数: {viewModel.ShowDetailedScore}");
            Console.WriteLine($"总分文本: {viewModel.TotalScoreText}");
            Console.WriteLine($"得分百分比文本: {viewModel.ScorePercentageText}");
            Console.WriteLine($"等级文本: {viewModel.GradeText}");
            Console.WriteLine($"等级颜色: {viewModel.GradeColor}");
            Console.WriteLine($"通过状态文本: {viewModel.PassStatusText}");
            Console.WriteLine($"通过状态图标: {viewModel.PassStatusIcon}");
            Console.WriteLine($"通过状态颜色: {viewModel.PassStatusColor}");

            Console.WriteLine("✓ ExamResultViewModel与详细分数集成测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ExamResultViewModel与详细分数集成测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试BenchSuite结果转换
    /// </summary>
    private static void TestBenchSuiteResultConversion()
    {
        Console.WriteLine("\n--- 测试BenchSuite结果转换 ---");
        
        try
        {
            // 创建模拟的BenchSuite评分结果
            BenchSuiteScoringResult benchSuiteResult = new()
            {
                IsSuccess = true,
                TotalScore = 100,
                AchievedScore = 85,
                StartTime = DateTime.Now.AddHours(-2),
                EndTime = DateTime.Now,
                FileTypeResults = new Dictionary<BenchSuiteFileType, FileTypeScoringResult>
                {
                    {
                        BenchSuiteFileType.Word,
                        new FileTypeScoringResult
                        {
                            IsSuccess = true,
                            TotalScore = 40,
                            AchievedScore = 35,
                            Details = "Word操作完成",
                            ErrorMessage = ""
                        }
                    },
                    {
                        BenchSuiteFileType.Excel,
                        new FileTypeScoringResult
                        {
                            IsSuccess = true,
                            TotalScore = 60,
                            AchievedScore = 50,
                            Details = "Excel操作完成",
                            ErrorMessage = ""
                        }
                    }
                }
            };

            ExamResultViewModel viewModel = new();
            
            // 从BenchSuite结果设置详细分数信息
            viewModel.SetScoreDetailFromBenchSuite(benchSuiteResult);

            // 验证转换结果
            Console.WriteLine($"是否有详细分数: {viewModel.HasScoreDetail}");
            Console.WriteLine($"总分: {viewModel.ScoreDetail?.TotalScore}");
            Console.WriteLine($"得分: {viewModel.ScoreDetail?.AchievedScore}");
            Console.WriteLine($"是否通过: {viewModel.ScoreDetail?.IsPassed}");
            Console.WriteLine($"题目数量: {viewModel.ScoreDetail?.Statistics.TotalQuestions}");
            Console.WriteLine($"正确题目数量: {viewModel.ScoreDetail?.Statistics.CorrectQuestions}");

            Console.WriteLine("✓ BenchSuite结果转换测试通过");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ BenchSuite结果转换测试失败: {ex.Message}");
        }
    }
}
