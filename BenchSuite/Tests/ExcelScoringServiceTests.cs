using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Tests;

/// <summary>
/// Excel打分服务测试类
/// </summary>
public class ExcelScoringServiceTests
{
    private readonly IExcelScoringService _excelScoringService;

    public ExcelScoringServiceTests()
    {
        _excelScoringService = new ExcelScoringService();
    }

    /// <summary>
    /// 测试文件扩展名验证
    /// </summary>
    public void TestCanProcessFile()
    {
        // 测试支持的文件类型
        bool canProcessXls = _excelScoringService.CanProcessFile("test.xls");
        bool canProcessXlsx = _excelScoringService.CanProcessFile("test.xlsx");
        bool canProcessDoc = _excelScoringService.CanProcessFile("test.doc");

        Console.WriteLine($"Can process .xls: {canProcessXls}");
        Console.WriteLine($"Can process .xlsx: {canProcessXlsx}");
        Console.WriteLine($"Can process .doc: {canProcessDoc}");

        if (canProcessXls && canProcessXlsx && !canProcessDoc)
        {
            Console.WriteLine("✓ 文件扩展名验证测试通过");
        }
        else
        {
            Console.WriteLine("✗ 文件扩展名验证测试失败");
        }
    }

    /// <summary>
    /// 测试支持的文件扩展名
    /// </summary>
    public void TestGetSupportedExtensions()
    {
        IEnumerable<string> extensions = _excelScoringService.GetSupportedExtensions();
        string[] extensionArray = extensions.ToArray();

        Console.WriteLine($"支持的文件扩展名: {string.Join(", ", extensionArray)}");

        if (extensionArray.Contains(".xls") && extensionArray.Contains(".xlsx"))
        {
            Console.WriteLine("✓ 支持的文件扩展名测试通过");
        }
        else
        {
            Console.WriteLine("✗ 支持的文件扩展名测试失败");
        }
    }

    /// <summary>
    /// 测试单个知识点检测（模拟）
    /// </summary>
    public async Task TestDetectKnowledgePointAsync()
    {
        // 测试基础操作知识点
        await TestBasicOperationKnowledgePoint();

        // 测试数据清单操作知识点
        await TestDataListOperationKnowledgePoint();

        // 测试图表操作知识点
        await TestChartOperationKnowledgePoint();
    }

    /// <summary>
    /// 测试基础操作知识点
    /// </summary>
    private async Task TestBasicOperationKnowledgePoint()
    {
        // 创建测试参数
        Dictionary<string, string> parameters = new()
        {
            ["TargetWorkbook"] = "测试工作簿",
            ["CellValues"] = "A1：测试值"
        };

        try
        {
            // 注意：这里使用一个不存在的文件路径进行测试
            // 在实际测试中，应该使用真实的Excel文件
            KnowledgePointResult result = await _excelScoringService.DetectKnowledgePointAsync(
                "nonexistent.xlsx",
                "FillOrCopyCellContent",
                parameters);

            Console.WriteLine($"基础操作知识点检测结果:");
            Console.WriteLine($"  知识点类型: {result.KnowledgePointType}");
            Console.WriteLine($"  是否正确: {result.IsCorrect}");
            Console.WriteLine($"  错误信息: {result.ErrorMessage}");

            // 由于文件不存在，应该返回错误
            if (!result.IsCorrect && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine("✓ 基础操作知识点检测错误处理测试通过");
            }
            else
            {
                Console.WriteLine("✗ 基础操作知识点检测错误处理测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 基础操作知识点检测测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试数据清单操作知识点
    /// </summary>
    private async Task TestDataListOperationKnowledgePoint()
    {
        Dictionary<string, string> parameters = new()
        {
            ["TargetWorkbook"] = "测试工作簿",
            ["GroupByColumn"] = "类别",
            ["SummaryFunction"] = "求和",
            ["SummaryColumn"] = "金额"
        };

        try
        {
            KnowledgePointResult result = await _excelScoringService.DetectKnowledgePointAsync(
                "nonexistent.xlsx",
                "Subtotal",
                parameters);

            Console.WriteLine($"数据清单操作知识点检测结果:");
            Console.WriteLine($"  知识点类型: {result.KnowledgePointType}");
            Console.WriteLine($"  是否正确: {result.IsCorrect}");
            Console.WriteLine($"  错误信息: {result.ErrorMessage}");

            if (!result.IsCorrect && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine("✓ 数据清单操作知识点检测错误处理测试通过");
            }
            else
            {
                Console.WriteLine("✗ 数据清单操作知识点检测错误处理测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 数据清单操作知识点检测测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试图表操作知识点
    /// </summary>
    private async Task TestChartOperationKnowledgePoint()
    {
        Dictionary<string, string> parameters = new()
        {
            ["TargetWorkbook"] = "测试工作簿",
            ["ChartNumber"] = "1",
            ["StyleNumber"] = "5"
        };

        try
        {
            KnowledgePointResult result = await _excelScoringService.DetectKnowledgePointAsync(
                "nonexistent.xlsx",
                "ChartStyle",
                parameters);

            Console.WriteLine($"图表操作知识点检测结果:");
            Console.WriteLine($"  知识点类型: {result.KnowledgePointType}");
            Console.WriteLine($"  是否正确: {result.IsCorrect}");
            Console.WriteLine($"  错误信息: {result.ErrorMessage}");

            if (!result.IsCorrect && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine("✓ 图表操作知识点检测错误处理测试通过");
            }
            else
            {
                Console.WriteLine("✗ 图表操作知识点检测错误处理测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 图表操作知识点检测测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试试卷打分（模拟）
    /// </summary>
    public async Task TestScoreFileAsync()
    {
        // 创建测试试卷模型
        ExamModel examModel = CreateTestExamModel();

        try
        {
            // 注意：这里使用一个不存在的文件路径进行测试
            ScoringResult result = await _excelScoringService.ScoreFileAsync(
                "nonexistent.xlsx", 
                examModel);

            Console.WriteLine($"试卷打分结果:");
            Console.WriteLine($"  是否成功: {result.IsSuccess}");
            Console.WriteLine($"  总分: {result.TotalScore}");
            Console.WriteLine($"  获得分数: {result.AchievedScore}");
            Console.WriteLine($"  错误信息: {result.ErrorMessage}");
            Console.WriteLine($"  知识点结果数量: {result.KnowledgePointResults.Count}");

            // 由于文件不存在，应该返回错误
            if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine("✓ 试卷打分错误处理测试通过");
            }
            else
            {
                Console.WriteLine("✗ 试卷打分错误处理测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 试卷打分测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建测试用的试卷模型
    /// </summary>
    private static ExamModel CreateTestExamModel()
    {
        return new ExamModel
        {
            Id = "test-exam-001",
            Name = "Excel测试试卷",
            Description = "用于测试Excel打分功能的试卷",
            Modules = 
            [
                new ExamModuleModel
                {
                    Id = "excel-module-001",
                    Name = "Excel操作模块",
                    Type = ModuleType.Excel,
                    Questions = 
                    [
                        new QuestionModel
                        {
                            Id = "question-001",
                            Title = "Excel基础操作",
                            Content = "完成Excel基础操作任务",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "op-001",
                                    Name = "填充或复制单元格内容",
                                    Description = "检测单元格内容是否正确填充",
                                    ModuleType = ModuleType.Excel,
                                    ExcelKnowledgeType = "FillOrCopyCellContent",
                                    Score = 10,
                                    Parameters =
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TargetWorkbook",
                                            Value = "测试工作簿"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "CellValues",
                                            Value = "A1：测试值"
                                        }
                                    ]
                                },
                                new OperationPointModel
                                {
                                    Id = "op-002",
                                    Name = "设置字体样式",
                                    Description = "检测字体样式设置",
                                    ModuleType = ModuleType.Excel,
                                    ExcelKnowledgeType = "SetFontStyle",
                                    Score = 8,
                                    Parameters =
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TargetWorkbook",
                                            Value = "测试工作簿"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "CellRange",
                                            Value = "A1:B2"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "FontStyle",
                                            Value = "粗体"
                                        }
                                    ]
                                }
                            ]
                        },
                        new QuestionModel
                        {
                            Id = "question-002",
                            Title = "数据清单操作",
                            Content = "完成数据清单操作任务",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "op-003",
                                    Name = "分类汇总",
                                    Description = "检测分类汇总功能",
                                    ModuleType = ModuleType.Excel,
                                    ExcelKnowledgeType = "Subtotal",
                                    Score = 15,
                                    Parameters =
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TargetWorkbook",
                                            Value = "测试工作簿"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "GroupByColumn",
                                            Value = "类别"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "SummaryFunction",
                                            Value = "求和"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "SummaryColumn",
                                            Value = "金额"
                                        }
                                    ]
                                }
                            ]
                        },
                        new QuestionModel
                        {
                            Id = "question-003",
                            Title = "图表操作",
                            Content = "完成图表操作任务",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "op-004",
                                    Name = "图表样式",
                                    Description = "检测图表样式设置",
                                    ModuleType = ModuleType.Excel,
                                    ExcelKnowledgeType = "ChartStyle",
                                    Score = 12,
                                    Parameters =
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TargetWorkbook",
                                            Value = "测试工作簿"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "ChartNumber",
                                            Value = "1"
                                        },
                                        new ConfigurationParameterModel
                                        {
                                            Name = "StyleNumber",
                                            Value = "5"
                                        }
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
    /// 运行所有测试
    /// </summary>
    public async Task RunAllTests()
    {
        Console.WriteLine("=== Excel打分服务测试开始 ===");
        Console.WriteLine();

        TestCanProcessFile();
        Console.WriteLine();

        TestGetSupportedExtensions();
        Console.WriteLine();

        await TestDetectKnowledgePointAsync();
        Console.WriteLine();

        await TestScoreFileAsync();
        Console.WriteLine();

        Console.WriteLine("=== Excel打分服务测试结束 ===");
    }
}

/// <summary>
/// 测试程序入口点
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        ExcelScoringServiceTests tests = new();
        await tests.RunAllTests();

        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
