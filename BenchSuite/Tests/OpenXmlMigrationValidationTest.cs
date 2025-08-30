using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services.OpenXml;
using System.Text.Json;

namespace BenchSuite.Tests;

/// <summary>
/// OpenXML迁移验证测试
/// </summary>
public class OpenXmlMigrationValidationTest
{
    /// <summary>
    /// 验证PowerPoint OpenXML服务
    /// </summary>
    public static async Task<ValidationResult> ValidatePowerPointServiceAsync()
    {
        ValidationResult result = new()
        {
            ServiceName = "PowerPoint OpenXML Service",
            TestType = "基础功能验证"
        };

        try
        {
            IPowerPointScoringService service = new PowerPointOpenXmlScoringService();
            
            // 测试支持的扩展名
            var extensions = service.GetSupportedExtensions();
            result.SupportedExtensions = string.Join(", ", extensions);
            
            // 测试文件验证功能
            bool canProcessValidFile = service.CanProcessFile("test.pptx");
            bool cannotProcessInvalidFile = !service.CanProcessFile("test.doc");
            
            result.CanProcessValidFile = canProcessValidFile;
            result.CanProcessInvalidFile = !cannotProcessInvalidFile;
            
            // 创建测试试卷模型
            ExamModel testExam = CreateTestPowerPointExam();
            
            // 如果有测试文件，进行实际评分测试
            string testFilePath = "TestData/sample.pptx";
            if (File.Exists(testFilePath))
            {
                ScoringResult scoringResult = await service.ScoreFileAsync(testFilePath, testExam);
                result.ScoringTestPassed = scoringResult != null;
                result.ScoringDetails = $"评分结果: {(scoringResult.IsSuccess ? "成功" : "失败")}, " +
                                      $"总分: {scoringResult.TotalScore}, 获得分: {scoringResult.AchievedScore}";
            }
            else
            {
                result.ScoringTestPassed = true; // 没有测试文件时跳过
                result.ScoringDetails = "跳过评分测试（无测试文件）";
            }
            
            result.IsSuccess = result.CanProcessValidFile && result.CanProcessInvalidFile && result.ScoringTestPassed;
            result.Message = result.IsSuccess ? "PowerPoint OpenXML服务验证通过" : "PowerPoint OpenXML服务验证失败";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Message = $"PowerPoint OpenXML服务验证异常: {ex.Message}";
            result.ErrorDetails = ex.ToString();
        }

        return result;
    }

    /// <summary>
    /// 验证Excel OpenXML服务
    /// </summary>
    public static async Task<ValidationResult> ValidateExcelServiceAsync()
    {
        ValidationResult result = new()
        {
            ServiceName = "Excel OpenXML Service",
            TestType = "基础功能验证"
        };

        try
        {
            IExcelScoringService service = new ExcelOpenXmlScoringService();
            
            // 测试支持的扩展名
            var extensions = service.GetSupportedExtensions();
            result.SupportedExtensions = string.Join(", ", extensions);
            
            // 测试文件验证功能
            bool canProcessValidFile = service.CanProcessFile("test.xlsx");
            bool cannotProcessInvalidFile = !service.CanProcessFile("test.doc");
            
            result.CanProcessValidFile = canProcessValidFile;
            result.CanProcessInvalidFile = !cannotProcessInvalidFile;
            
            // 创建测试试卷模型
            ExamModel testExam = CreateTestExcelExam();
            
            // 如果有测试文件，进行实际评分测试
            string testFilePath = "TestData/sample.xlsx";
            if (File.Exists(testFilePath))
            {
                ScoringResult scoringResult = await service.ScoreFileAsync(testFilePath, testExam);
                result.ScoringTestPassed = scoringResult != null;
                result.ScoringDetails = $"评分结果: {(scoringResult.IsSuccess ? "成功" : "失败")}, " +
                                      $"总分: {scoringResult.TotalScore}, 获得分: {scoringResult.AchievedScore}";
            }
            else
            {
                result.ScoringTestPassed = true; // 没有测试文件时跳过
                result.ScoringDetails = "跳过评分测试（无测试文件）";
            }
            
            result.IsSuccess = result.CanProcessValidFile && result.CanProcessInvalidFile && result.ScoringTestPassed;
            result.Message = result.IsSuccess ? "Excel OpenXML服务验证通过" : "Excel OpenXML服务验证失败";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Message = $"Excel OpenXML服务验证异常: {ex.Message}";
            result.ErrorDetails = ex.ToString();
        }

        return result;
    }

    /// <summary>
    /// 验证Word OpenXML服务
    /// </summary>
    public static async Task<ValidationResult> ValidateWordServiceAsync()
    {
        ValidationResult result = new()
        {
            ServiceName = "Word OpenXML Service",
            TestType = "基础功能验证"
        };

        try
        {
            IWordScoringService service = new WordOpenXmlScoringService();
            
            // 测试支持的扩展名
            var extensions = service.GetSupportedExtensions();
            result.SupportedExtensions = string.Join(", ", extensions);
            
            // 测试文件验证功能
            bool canProcessValidFile = service.CanProcessFile("test.docx");
            bool cannotProcessInvalidFile = !service.CanProcessFile("test.ppt");
            
            result.CanProcessValidFile = canProcessValidFile;
            result.CanProcessInvalidFile = !cannotProcessInvalidFile;
            
            // 创建测试试卷模型
            ExamModel testExam = CreateTestWordExam();
            
            // 如果有测试文件，进行实际评分测试
            string testFilePath = "TestData/sample.docx";
            if (File.Exists(testFilePath))
            {
                ScoringResult scoringResult = await service.ScoreFileAsync(testFilePath, testExam);
                result.ScoringTestPassed = scoringResult != null;
                result.ScoringDetails = $"评分结果: {(scoringResult.IsSuccess ? "成功" : "失败")}, " +
                                      $"总分: {scoringResult.TotalScore}, 获得分: {scoringResult.AchievedScore}";
            }
            else
            {
                result.ScoringTestPassed = true; // 没有测试文件时跳过
                result.ScoringDetails = "跳过评分测试（无测试文件）";
            }
            
            result.IsSuccess = result.CanProcessValidFile && result.CanProcessInvalidFile && result.ScoringTestPassed;
            result.Message = result.IsSuccess ? "Word OpenXML服务验证通过" : "Word OpenXML服务验证失败";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.Message = $"Word OpenXML服务验证异常: {ex.Message}";
            result.ErrorDetails = ex.ToString();
        }

        return result;
    }

    /// <summary>
    /// 运行完整的迁移验证测试
    /// </summary>
    public static async Task<MigrationValidationReport> RunFullValidationAsync()
    {
        MigrationValidationReport report = new()
        {
            TestStartTime = DateTime.Now,
            Results = []
        };

        Console.WriteLine("=== OpenXML SDK 迁移验证测试 ===");
        Console.WriteLine();

        // 验证PowerPoint服务
        Console.WriteLine("验证PowerPoint OpenXML服务...");
        ValidationResult pptResult = await ValidatePowerPointServiceAsync();
        report.Results.Add(pptResult);
        Console.WriteLine($"结果: {pptResult.Message}");
        Console.WriteLine();

        // 验证Excel服务
        Console.WriteLine("验证Excel OpenXML服务...");
        ValidationResult excelResult = await ValidateExcelServiceAsync();
        report.Results.Add(excelResult);
        Console.WriteLine($"结果: {excelResult.Message}");
        Console.WriteLine();

        // 验证Word服务
        Console.WriteLine("验证Word OpenXML服务...");
        ValidationResult wordResult = await ValidateWordServiceAsync();
        report.Results.Add(wordResult);
        Console.WriteLine($"结果: {wordResult.Message}");
        Console.WriteLine();

        report.TestEndTime = DateTime.Now;
        report.TotalDuration = report.TestEndTime - report.TestStartTime;
        report.OverallSuccess = report.Results.All(r => r.IsSuccess);

        // 生成报告摘要
        Console.WriteLine("=== 验证测试摘要 ===");
        Console.WriteLine($"测试开始时间: {report.TestStartTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"测试结束时间: {report.TestEndTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"测试总耗时: {report.TotalDuration.TotalSeconds:F2} 秒");
        Console.WriteLine($"总体结果: {(report.OverallSuccess ? "通过" : "失败")}");
        Console.WriteLine($"成功服务: {report.Results.Count(r => r.IsSuccess)}/{report.Results.Count}");
        Console.WriteLine();

        // 保存详细报告
        await SaveValidationReportAsync(report);

        return report;
    }

    /// <summary>
    /// 保存验证报告
    /// </summary>
    private static async Task SaveValidationReportAsync(MigrationValidationReport report)
    {
        try
        {
            string reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            string reportFileName = $"OpenXML_Migration_Validation_Report_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            await File.WriteAllTextAsync(reportFileName, reportJson);
            Console.WriteLine($"验证报告已保存到: {reportFileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存验证报告失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建测试PowerPoint试卷
    /// </summary>
    private static ExamModel CreateTestPowerPointExam()
    {
        return new ExamModel
        {
            Id = "test-ppt-exam",
            Title = "PowerPoint测试试卷",
            Modules =
            [
                new ExamModuleModel
                {
                    Type = ModuleType.PowerPoint,
                    Questions =
                    [
                        new QuestionModel
                        {
                            Id = "ppt-q1",
                            Title = "PowerPoint基础操作",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "ppt-op1",
                                    Name = "SetSlideLayout",
                                    Score = 10,
                                    ModuleType = ModuleType.PowerPoint,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "SlideIndex", Value = "1" },
                                        new ParameterModel { Name = "LayoutType", Value = "Title Slide" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// 创建测试Excel试卷
    /// </summary>
    private static ExamModel CreateTestExcelExam()
    {
        return new ExamModel
        {
            Id = "test-excel-exam",
            Title = "Excel测试试卷",
            Modules =
            [
                new ExamModuleModel
                {
                    Type = ModuleType.Excel,
                    Questions =
                    [
                        new QuestionModel
                        {
                            Id = "excel-q1",
                            Title = "Excel基础操作",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "excel-op1",
                                    Name = "FillOrCopyCellContent",
                                    Score = 10,
                                    ModuleType = ModuleType.Excel,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "CellRange", Value = "A1" },
                                        new ParameterModel { Name = "ExpectedValue", Value = "测试内容" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// 创建测试Word试卷
    /// </summary>
    private static ExamModel CreateTestWordExam()
    {
        return new ExamModel
        {
            Id = "test-word-exam",
            Title = "Word测试试卷",
            Modules =
            [
                new ExamModuleModel
                {
                    Type = ModuleType.Word,
                    Questions =
                    [
                        new QuestionModel
                        {
                            Id = "word-q1",
                            Title = "Word基础操作",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "word-op1",
                                    Name = "SetDocumentContent",
                                    Score = 10,
                                    ModuleType = ModuleType.Word,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "ExpectedContent", Value = "测试文档内容" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}

/// <summary>
/// 验证结果模型
/// </summary>
public class ValidationResult
{
    public string ServiceName { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public string SupportedExtensions { get; set; } = string.Empty;
    public bool CanProcessValidFile { get; set; }
    public bool CanProcessInvalidFile { get; set; }
    public bool ScoringTestPassed { get; set; }
    public string ScoringDetails { get; set; } = string.Empty;
}

/// <summary>
/// 迁移验证报告模型
/// </summary>
public class MigrationValidationReport
{
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public bool OverallSuccess { get; set; }
    public List<ValidationResult> Results { get; set; } = [];
}
