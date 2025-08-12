using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Examples;

/// <summary>
/// PowerPoint打分系统使用示例
/// </summary>
public class PowerPointScoringExample
{
    /// <summary>
    /// 基本使用示例
    /// </summary>
    public static async Task<ScoringResult> BasicUsageExample()
    {
        // 创建PPT打分服务
        IPowerPointScoringService scoringService = new PowerPointScoringService();

        // 创建试卷模型
        ExamModel exam = CreateSampleExam();

        // 配置打分选项
        ScoringConfiguration configuration = new()
        {
            EnablePartialScoring = true,
            ErrorTolerance = 0.1m,
            TimeoutSeconds = 30,
            EnableDetailedLogging = true
        };

        // 执行打分
        string pptFilePath = @"C:\TestFiles\sample.pptx";
        ScoringResult result = await scoringService.ScoreFileAsync(pptFilePath, exam, configuration);

        // 输出结果
        Console.WriteLine($"打分完成: {result.IsSuccess}");
        Console.WriteLine($"总分: {result.TotalScore}");
        Console.WriteLine($"获得分数: {result.AchievedScore}");
        Console.WriteLine($"得分率: {result.ScoreRate:P2}");
        Console.WriteLine($"耗时: {result.ElapsedMilliseconds}ms");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"错误信息: {result.ErrorMessage}");
        }

        // 输出知识点检测详情
        foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
        {
            Console.WriteLine($"知识点: {kpResult.KnowledgePointName}");
            Console.WriteLine($"  类型: {kpResult.KnowledgePointType}");
            Console.WriteLine($"  分数: {kpResult.AchievedScore}/{kpResult.TotalScore}");
            Console.WriteLine($"  正确: {kpResult.IsCorrect}");
            Console.WriteLine($"  详情: {kpResult.Details}");
            
            if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
            {
                Console.WriteLine($"  错误: {kpResult.ErrorMessage}");
            }
            
            Console.WriteLine();
        }

        return result;
    }

    /// <summary>
    /// 文本字形检测示例
    /// </summary>
    public static async Task<KnowledgePointResult> TextStyleExample()
    {
        IPowerPointScoringService scoringService = new PowerPointScoringService();

        string pptFilePath = @"C:\TestFiles\sample.pptx";
        string knowledgePointType = "SetTextStyle";
        
        Dictionary<string, string> parameters = new()
        {
            ["SlideIndex"] = "1",
            ["TextBoxIndex"] = "1",
            ["StyleType"] = "1" // 1=加粗, 2=斜体, 3=下划线, 4=删除线
        };

        KnowledgePointResult result = await scoringService.DetectKnowledgePointAsync(
            pptFilePath, 
            knowledgePointType, 
            parameters);

        Console.WriteLine($"文本字形检测结果:");
        Console.WriteLine($"类型: {result.KnowledgePointType}");
        Console.WriteLine($"正确: {result.IsCorrect}");
        Console.WriteLine($"期望值: {result.ExpectedValue}");
        Console.WriteLine($"实际值: {result.ActualValue}");
        Console.WriteLine($"详情: {result.Details}");

        return result;
    }

    /// <summary>
    /// 元素位置检测示例
    /// </summary>
    public static async Task<KnowledgePointResult> ElementPositionExample()
    {
        IPowerPointScoringService scoringService = new PowerPointScoringService();

        string pptFilePath = @"C:\TestFiles\sample.pptx";
        
        Dictionary<string, string> parameters = new()
        {
            ["SlideIndex"] = "1",
            ["ElementIndex"] = "1",
            ["Left"] = "100",
            ["Top"] = "50"
        };

        KnowledgePointResult result = await scoringService.DetectKnowledgePointAsync(
            pptFilePath, 
            "SetElementPosition", 
            parameters);

        Console.WriteLine($"元素位置检测结果:");
        Console.WriteLine($"正确: {result.IsCorrect}");
        Console.WriteLine($"期望位置: {result.ExpectedValue}");
        Console.WriteLine($"实际位置: {result.ActualValue}");
        Console.WriteLine($"详情: {result.Details}");

        return result;
    }

    /// <summary>
    /// 表格内容检测示例
    /// </summary>
    public static async Task<KnowledgePointResult> TableContentExample()
    {
        IPowerPointScoringService scoringService = new PowerPointScoringService();

        string pptFilePath = @"C:\TestFiles\sample.pptx";
        
        Dictionary<string, string> parameters = new()
        {
            ["SlideIndex"] = "2",
            ["Rows"] = "3",
            ["Columns"] = "2",
            ["Content"] = "应用领域,辐照产品,灭菌,肉类及其制品,灭虫,谷物、干果等"
        };

        KnowledgePointResult result = await scoringService.DetectKnowledgePointAsync(
            pptFilePath, 
            "SetTableContent", 
            parameters);

        Console.WriteLine($"表格内容检测结果:");
        Console.WriteLine($"正确: {result.IsCorrect}");
        Console.WriteLine($"期望内容: {result.ExpectedValue}");
        Console.WriteLine($"实际内容: {result.ActualValue}");
        Console.WriteLine($"详情: {result.Details}");

        return result;
    }

    /// <summary>
    /// 创建示例试卷
    /// </summary>
    private static ExamModel CreateSampleExam()
    {
        ExamModel exam = new()
        {
            Id = "sample-exam-1",
            Name = "PowerPoint综合操作考试",
            Description = "测试PowerPoint多种操作技能"
        };

        // 创建PowerPoint模块
        ExamModuleModel pptModule = new()
        {
            Id = "ppt-module-1",
            Name = "PowerPoint操作",
            Type = ModuleType.PowerPoint,
            Description = "PowerPoint综合操作测试",
            Score = 100
        };

        // 创建题目1：幻灯片版式设置
        QuestionModel question1 = new()
        {
            Id = "question-1",
            Title = "设置幻灯片版式",
            Content = "将第1张幻灯片的版式设置为标题版式",
            Score = 15
        };

        OperationPointModel op1 = new()
        {
            Id = "op-1",
            Name = "设置幻灯片版式",
            Description = "设置第1张幻灯片为标题版式",
            PowerPointKnowledgeType = "SetSlideLayout",
            Score = 15,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "SlideIndex", Value = "1", Type = ParameterType.Number },
                new() { Name = "LayoutType", Value = "ppLayoutTitle", Type = ParameterType.Text }
            }
        };

        question1.OperationPoints.Add(op1);

        // 创建题目2：插入文本内容
        QuestionModel question2 = new()
        {
            Id = "question-2",
            Title = "插入文本内容",
            Content = "在第1张幻灯片中插入标题文本'PowerPoint操作技能测试'",
            Score = 10
        };

        OperationPointModel op2 = new()
        {
            Id = "op-2",
            Name = "插入文本内容",
            Description = "在第1张幻灯片插入指定文本",
            PowerPointKnowledgeType = "InsertTextContent",
            Score = 10,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "SlideIndex", Value = "1", Type = ParameterType.Number },
                new() { Name = "TextContent", Value = "PowerPoint操作技能测试", Type = ParameterType.Text }
            }
        };

        question2.OperationPoints.Add(op2);

        // 创建题目3：设置文本字形
        QuestionModel question3 = new()
        {
            Id = "question-3",
            Title = "设置文本字形",
            Content = "将标题文字设置为加粗",
            Score = 10
        };

        OperationPointModel op3 = new()
        {
            Id = "op-3",
            Name = "设置文本字形",
            Description = "设置文字为加粗",
            PowerPointKnowledgeType = "SetTextStyle",
            Score = 10,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "SlideIndex", Value = "1", Type = ParameterType.Number },
                new() { Name = "TextBoxIndex", Value = "1", Type = ParameterType.Number },
                new() { Name = "StyleType", Value = "1", Type = ParameterType.Number } // 1=加粗
            }
        };

        question3.OperationPoints.Add(op3);

        // 创建题目4：设置文本对齐方式
        QuestionModel question4 = new()
        {
            Id = "question-4",
            Title = "设置文本对齐方式",
            Content = "将标题文字设置为居中对齐",
            Score = 10
        };

        OperationPointModel op4 = new()
        {
            Id = "op-4",
            Name = "设置文本对齐方式",
            Description = "设置文字居中对齐",
            PowerPointKnowledgeType = "SetTextAlignment",
            Score = 10,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "SlideIndex", Value = "1", Type = ParameterType.Number },
                new() { Name = "TextBoxIndex", Value = "1", Type = ParameterType.Number },
                new() { Name = "Alignment", Value = "2", Type = ParameterType.Number } // 2=居中对齐
            }
        };

        question4.OperationPoints.Add(op4);

        // 创建题目5：插入图片
        QuestionModel question5 = new()
        {
            Id = "question-5",
            Title = "插入图片",
            Content = "在第2张幻灯片中插入一张图片",
            Score = 15
        };

        OperationPointModel op5 = new()
        {
            Id = "op-5",
            Name = "插入图片",
            Description = "在指定幻灯片插入图片",
            PowerPointKnowledgeType = "InsertImage",
            Score = 15,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "SlideIndex", Value = "2", Type = ParameterType.Number },
                new() { Name = "ExpectedImageCount", Value = "1", Type = ParameterType.Number }
            }
        };

        question5.OperationPoints.Add(op5);

        // 创建题目6：插入表格
        QuestionModel question6 = new()
        {
            Id = "question-6",
            Title = "插入表格",
            Content = "在第3张幻灯片中插入3行2列的表格",
            Score = 20
        };

        OperationPointModel op6 = new()
        {
            Id = "op-6",
            Name = "插入表格",
            Description = "插入指定尺寸的表格",
            PowerPointKnowledgeType = "InsertTable",
            Score = 20,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "SlideIndex", Value = "3", Type = ParameterType.Number },
                new() { Name = "ExpectedRows", Value = "3", Type = ParameterType.Number },
                new() { Name = "ExpectedColumns", Value = "2", Type = ParameterType.Number }
            }
        };

        question6.OperationPoints.Add(op6);

        // 创建题目7：应用主题
        QuestionModel question7 = new()
        {
            Id = "question-7",
            Title = "应用主题",
            Content = "为演示文稿应用'Office'主题",
            Score = 20
        };

        OperationPointModel op7 = new()
        {
            Id = "op-7",
            Name = "应用主题",
            Description = "应用指定主题",
            PowerPointKnowledgeType = "ApplyTheme",
            Score = 20,
            Parameters = new List<ConfigurationParameterModel>
            {
                new() { Name = "ThemeName", Value = "Office", Type = ParameterType.Text }
            }
        };

        question7.OperationPoints.Add(op7);

        // 添加题目到模块
        pptModule.Questions.AddRange(new[] { question1, question2, question3, question4, question5, question6, question7 });

        // 添加模块到试卷
        exam.Modules.Add(pptModule);

        return exam;
    }

    /// <summary>
    /// 批量处理示例
    /// </summary>
    public static async Task BatchProcessingExample()
    {
        IPowerPointScoringService scoringService = new PowerPointScoringService();
        ExamModel exam = CreateSampleExam();

        string[] pptFiles = {
            @"C:\TestFiles\student1.pptx",
            @"C:\TestFiles\student2.pptx",
            @"C:\TestFiles\student3.pptx"
        };

        List<ScoringResult> results = new();

        foreach (string filePath in pptFiles)
        {
            if (File.Exists(filePath))
            {
                Console.WriteLine($"正在处理: {Path.GetFileName(filePath)}");
                
                ScoringResult result = await scoringService.ScoreFileAsync(filePath, exam);
                results.Add(result);
                
                Console.WriteLine($"完成 - 得分: {result.AchievedScore}/{result.TotalScore} ({result.ScoreRate:P2})");
            }
            else
            {
                Console.WriteLine($"文件不存在: {filePath}");
            }
        }

        // 统计结果
        if (results.Count > 0)
        {
            decimal avgScore = results.Average(r => r.AchievedScore);
            decimal avgRate = results.Average(r => r.ScoreRate);
            
            Console.WriteLine($"\n批量处理完成:");
            Console.WriteLine($"处理文件数: {results.Count}");
            Console.WriteLine($"平均得分: {avgScore:F2}");
            Console.WriteLine($"平均得分率: {avgRate:P2}");
        }
    }
}
