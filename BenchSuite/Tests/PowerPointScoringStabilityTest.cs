using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Tests
{
    /// <summary>
    /// PowerPoint评分稳定性测试
    /// </summary>
    public class PowerPointScoringStabilityTest
    {
        private readonly PowerPointScoringService _scoringService;
        private readonly string _testFilePath;

        public PowerPointScoringStabilityTest()
        {
            _scoringService = new PowerPointScoringService();
            _testFilePath = "test_presentation.pptx"; // 需要提供一个测试PPT文件
        }

        /// <summary>
        /// 测试参数解析的一致性
        /// </summary>
        public void TestParameterResolutionConsistency()
        {
            Console.WriteLine("=== 参数解析一致性测试 ===");

            // 创建测试操作点，包含-1参数
            List<OperationPointModel> testOperationPoints = CreateTestOperationPoints();

            // 多次运行相同的评分，检查结果是否一致
            List<List<KnowledgePointResult>> allResults = new();
            
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"第 {i + 1} 次评分...");
                
                try
                {
                    var results = _scoringService.DetectKnowledgePointsAsync(_testFilePath, testOperationPoints).Result;
                    allResults.Add(results);
                    
                    // 输出每次的评分结果
                    Console.WriteLine($"  结果数量: {results.Count}");
                    foreach (var result in results)
                    {
                        Console.WriteLine($"    {result.KnowledgePointName}: {result.AchievedScore}/{result.TotalScore}");
                        if (!string.IsNullOrEmpty(result.Details) && result.Details.Contains("参数解析"))
                        {
                            Console.WriteLine($"      {result.Details}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  评分失败: {ex.Message}");
                }
                
                Console.WriteLine();
            }

            // 分析结果一致性
            AnalyzeConsistency(allResults);
        }

        /// <summary>
        /// 测试特定文件的评分稳定性
        /// </summary>
        public void TestScoringStability(string filePath)
        {
            Console.WriteLine($"=== 评分稳定性测试: {Path.GetFileName(filePath)} ===");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"测试文件不存在: {filePath}");
                return;
            }

            // 创建包含-1参数的测试题目
            ExamModel testExam = CreateTestExam();

            List<ScoringResult> results = new();

            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine($"第 {i + 1} 次完整评分...");
                
                try
                {
                    ScoringResult result = _scoringService.ScoreFile(filePath, testExam);
                    results.Add(result);
                    
                    Console.WriteLine($"  总分: {result.TotalScore}, 得分: {result.AchievedScore}, 得分率: {(result.AchievedScore / result.TotalScore * 100):F2}%");
                    Console.WriteLine($"  知识点数量: {result.KnowledgePointResults.Count}");
                    
                    // 显示每个知识点的结果
                    foreach (var kpResult in result.KnowledgePointResults)
                    {
                        string status = kpResult.IsCorrect ? "✓" : "✗";
                        Console.WriteLine($"    {status} {kpResult.KnowledgePointName}: {kpResult.AchievedScore}/{kpResult.TotalScore}");
                        if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
                        {
                            Console.WriteLine($"      错误: {kpResult.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  评分失败: {ex.Message}");
                }
                
                Console.WriteLine();
            }

            // 分析评分稳定性
            AnalyzeScoringStability(results);
        }

        /// <summary>
        /// 创建测试操作点
        /// </summary>
        private List<OperationPointModel> CreateTestOperationPoints()
        {
            return new List<OperationPointModel>
            {
                new OperationPointModel
                {
                    Id = "test_slide_font",
                    Name = "设置幻灯片字体",
                    PowerPointKnowledgeType = "SetSlideFont",
                    Score = 10,
                    Parameters = new List<ParameterModel>
                    {
                        new ParameterModel { Name = "SlideIndex", Value = "-1" }, // 任意幻灯片
                        new ParameterModel { Name = "FontName", Value = "Arial" }
                    }
                },
                new OperationPointModel
                {
                    Id = "test_insert_image",
                    Name = "插入图片",
                    PowerPointKnowledgeType = "InsertImage",
                    Score = 10,
                    Parameters = new List<ParameterModel>
                    {
                        new ParameterModel { Name = "SlideIndex", Value = "-1" }, // 任意幻灯片
                        new ParameterModel { Name = "ExpectedImageCount", Value = "1" }
                    }
                },
                new OperationPointModel
                {
                    Id = "test_insert_table",
                    Name = "插入表格",
                    PowerPointKnowledgeType = "InsertTable",
                    Score = 10,
                    Parameters = new List<ParameterModel>
                    {
                        new ParameterModel { Name = "SlideIndex", Value = "-1" }, // 任意幻灯片
                        new ParameterModel { Name = "ExpectedRows", Value = "3" },
                        new ParameterModel { Name = "ExpectedColumns", Value = "4" }
                    }
                }
            };
        }

        /// <summary>
        /// 创建测试试卷
        /// </summary>
        private ExamModel CreateTestExam()
        {
            return new ExamModel
            {
                Id = "stability_test",
                Title = "稳定性测试试卷",
                Modules = new List<ExamModuleModel>
                {
                    new ExamModuleModel
                    {
                        Type = ModuleType.PowerPoint,
                        Questions = new List<QuestionModel>
                        {
                            new QuestionModel
                            {
                                Id = "q1",
                                Title = "PowerPoint操作测试",
                                OperationPoints = CreateTestOperationPoints()
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 分析结果一致性
        /// </summary>
        private void AnalyzeConsistency(List<List<KnowledgePointResult>> allResults)
        {
            Console.WriteLine("=== 一致性分析 ===");

            if (allResults.Count < 2)
            {
                Console.WriteLine("结果数量不足，无法进行一致性分析");
                return;
            }

            // 检查每个知识点的结果是否一致
            var firstResults = allResults[0];
            bool isConsistent = true;

            for (int i = 0; i < firstResults.Count; i++)
            {
                var firstResult = firstResults[i];
                bool pointConsistent = true;

                for (int j = 1; j < allResults.Count; j++)
                {
                    if (i >= allResults[j].Count)
                    {
                        Console.WriteLine($"知识点 {firstResult.KnowledgePointName}: 结果数量不一致");
                        pointConsistent = false;
                        break;
                    }

                    var otherResult = allResults[j][i];
                    if (firstResult.AchievedScore != otherResult.AchievedScore ||
                        firstResult.IsCorrect != otherResult.IsCorrect)
                    {
                        Console.WriteLine($"知识点 {firstResult.KnowledgePointName}: 评分不一致");
                        Console.WriteLine($"  第1次: {firstResult.AchievedScore}/{firstResult.TotalScore} ({firstResult.IsCorrect})");
                        Console.WriteLine($"  第{j+1}次: {otherResult.AchievedScore}/{otherResult.TotalScore} ({otherResult.IsCorrect})");
                        pointConsistent = false;
                    }
                }

                if (pointConsistent)
                {
                    Console.WriteLine($"知识点 {firstResult.KnowledgePointName}: ✓ 一致");
                }
                else
                {
                    isConsistent = false;
                }
            }

            Console.WriteLine($"\n总体一致性: {(isConsistent ? "✓ 一致" : "✗ 不一致")}");
        }

        /// <summary>
        /// 分析评分稳定性
        /// </summary>
        private void AnalyzeScoringStability(List<ScoringResult> results)
        {
            Console.WriteLine("=== 评分稳定性分析 ===");

            if (results.Count < 2)
            {
                Console.WriteLine("结果数量不足，无法进行稳定性分析");
                return;
            }

            // 检查总分和得分的稳定性
            var scores = results.Select(r => r.AchievedScore).ToList();
            var totalScores = results.Select(r => r.TotalScore).ToList();

            bool scoresConsistent = scores.All(s => s == scores[0]);
            bool totalScoresConsistent = totalScores.All(s => s == totalScores[0]);

            Console.WriteLine($"总分一致性: {(totalScoresConsistent ? "✓" : "✗")} ({string.Join(", ", totalScores)})");
            Console.WriteLine($"得分一致性: {(scoresConsistent ? "✓" : "✗")} ({string.Join(", ", scores)})");

            if (!scoresConsistent)
            {
                double avgScore = scores.Average();
                double variance = scores.Select(s => Math.Pow(s - avgScore, 2)).Average();
                double stdDev = Math.Sqrt(variance);
                
                Console.WriteLine($"得分统计: 平均={avgScore:F2}, 标准差={stdDev:F2}");
            }

            Console.WriteLine($"\n评分稳定性: {(scoresConsistent && totalScoresConsistent ? "✓ 稳定" : "✗ 不稳定")}");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public void RunAllTests(string testFilePath = null)
        {
            Console.WriteLine("PowerPoint评分稳定性测试开始");
            Console.WriteLine("=====================================");

            // 测试参数解析一致性
            TestParameterResolutionConsistency();

            // 测试评分稳定性
            if (!string.IsNullOrEmpty(testFilePath))
            {
                TestScoringStability(testFilePath);
            }

            Console.WriteLine("=====================================");
            Console.WriteLine("测试完成");
        }
    }
}
