using System;
using System.Collections.Generic;
using System.IO;
using BenchSuite.Models;
using BenchSuite.Services.OpenXml;

namespace BenchSuite.Services
{
    /// <summary>
    /// PowerPoint文本输入框检测功能测试类
    /// </summary>
    public class PowerPointTextInputBoxDetectionTest
    {
        private readonly PowerPointOpenXmlScoringService _scoringService;

        public PowerPointTextInputBoxDetectionTest()
        {
            _scoringService = new PowerPointOpenXmlScoringService();
        }

        /// <summary>
        /// 测试文本输入框检测功能
        /// </summary>
        /// <param name="filePath">PowerPoint文件路径</param>
        /// <returns>检测结果</returns>
        public TestResult TestTextInputBoxDetection(string filePath)
        {
            TestResult result = new()
            {
                TestName = "文本输入框检测测试",
                FilePath = filePath,
                StartTime = DateTime.Now
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"文件不存在: {filePath}";
                    return result;
                }

                // 测试1: 检测所有幻灯片的文本输入框
                Dictionary<string, string> parameters1 = new();
                KnowledgePointResult allSlidesResult = _scoringService.DetectKnowledgePointAsync(
                    filePath, "DetectTextInputBox", parameters1).Result;

                result.TestCases.Add(new TestCase
                {
                    Name = "检测所有幻灯片文本输入框",
                    Expected = "找到文本输入框",
                    Actual = allSlidesResult.ActualValue ?? "无结果",
                    IsSuccess = allSlidesResult.IsCorrect,
                    Details = allSlidesResult.Details ?? "无详细信息"
                });

                // 测试2: 检测指定幻灯片的文本输入框
                Dictionary<string, string> parameters2 = new()
                {
                    ["SlideNumber"] = "1"
                };
                KnowledgePointResult slideResult = _scoringService.DetectKnowledgePointAsync(
                    filePath, "DetectTextInputBox", parameters2).Result;

                result.TestCases.Add(new TestCase
                {
                    Name = "检测第1张幻灯片文本输入框",
                    Expected = "找到文本输入框",
                    Actual = slideResult.ActualValue ?? "无结果",
                    IsSuccess = slideResult.IsCorrect,
                    Details = slideResult.Details ?? "无详细信息"
                });

                // 测试3: 检测期望数量的文本输入框
                Dictionary<string, string> parameters3 = new()
                {
                    ["ExpectedCount"] = "2"
                };
                KnowledgePointResult countResult = _scoringService.DetectKnowledgePointAsync(
                    filePath, "DetectTextInputBox", parameters3).Result;

                result.TestCases.Add(new TestCase
                {
                    Name = "检测至少2个文本输入框",
                    Expected = "至少2个文本输入框",
                    Actual = countResult.ActualValue ?? "无结果",
                    IsSuccess = countResult.IsCorrect,
                    Details = countResult.Details ?? "无详细信息"
                });

                // 计算总体成功率
                int successCount = 0;
                foreach (TestCase testCase in result.TestCases)
                {
                    if (testCase.IsSuccess)
                        successCount++;
                }

                result.IsSuccess = successCount > 0;
                result.SuccessRate = (double)successCount / result.TestCases.Count;
                result.Summary = $"测试完成，成功率: {result.SuccessRate:P0} ({successCount}/{result.TestCases.Count})";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"测试执行失败: {ex.Message}";
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        /// <summary>
        /// 生成测试报告
        /// </summary>
        /// <param name="result">测试结果</param>
        /// <returns>测试报告</returns>
        public string GenerateTestReport(TestResult result)
        {
            List<string> report = 
            [
                "=== PowerPoint文本输入框检测测试报告 ===",
                $"测试名称: {result.TestName}",
                $"文件路径: {result.FilePath}",
                $"开始时间: {result.StartTime:yyyy-MM-dd HH:mm:ss}",
                $"结束时间: {result.EndTime:yyyy-MM-dd HH:mm:ss}",
                $"执行时长: {result.Duration.TotalSeconds:F2}秒",
                $"总体结果: {(result.IsSuccess ? "成功" : "失败")}",
                $"成功率: {result.SuccessRate:P0}",
                ""
            ];

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                report.Add($"错误信息: {result.ErrorMessage}");
                report.Add("");
            }

            if (!string.IsNullOrEmpty(result.Summary))
            {
                report.Add($"总结: {result.Summary}");
                report.Add("");
            }

            report.Add("=== 详细测试用例 ===");
            for (int i = 0; i < result.TestCases.Count; i++)
            {
                TestCase testCase = result.TestCases[i];
                report.Add($"测试用例 {i + 1}: {testCase.Name}");
                report.Add($"  期望结果: {testCase.Expected}");
                report.Add($"  实际结果: {testCase.Actual}");
                report.Add($"  测试状态: {(testCase.IsSuccess ? "通过" : "失败")}");
                report.Add($"  详细信息: {testCase.Details}");
                report.Add("");
            }

            return string.Join(Environment.NewLine, report);
        }
    }

    /// <summary>
    /// 测试结果模型
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSuccess { get; set; }
        public double SuccessRate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<TestCase> TestCases { get; set; } = [];
    }

    /// <summary>
    /// 测试用例模型
    /// </summary>
    public class TestCase
    {
        public string Name { get; set; } = string.Empty;
        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}
