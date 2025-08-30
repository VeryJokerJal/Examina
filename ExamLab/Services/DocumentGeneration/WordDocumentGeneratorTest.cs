using ExamLab.Models;
using ExamLab.Services.DocumentGeneration;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// Word文档生成器测试类
/// </summary>
public static class WordDocumentGeneratorTest
{
    /// <summary>
    /// 创建测试模块
    /// </summary>
    /// <returns>测试用的ExamModule</returns>
    public static ExamModule CreateTestModule()
    {
        ExamModule testModule = new()
        {
            Id = Guid.NewGuid(),
            Name = "Word文档生成测试模块",
            Type = ModuleType.Word,
            IsEnabled = true,
            Questions = []
        };

        // 创建测试题目
        Question testQuestion = new()
        {
            Id = Guid.NewGuid(),
            Title = "Word操作测试题目",
            Description = "测试各种Word操作点的功能",
            IsEnabled = true,
            OperationPoints = []
        };

        // 添加各种类型的操作点进行测试
        
        // 1. 段落操作测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetParagraphFont,
            "设置段落字体",
            "设置第1段落字体为微软雅黑",
            ("ParagraphNumber", "1"),
            ("FontFamily", "微软雅黑")
        ));

        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetParagraphFontSize,
            "设置段落字号",
            "设置第1段落字号为14磅",
            ("ParagraphNumber", "1"),
            ("FontSize", "14")
        ));

        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetParagraphAlignment,
            "设置段落对齐",
            "设置第1段落居中对齐",
            ("ParagraphNumber", "1"),
            ("Alignment", "居中对齐")
        ));

        // 2. 页面设置测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetPaperSize,
            "设置纸张大小",
            "设置纸张为A4",
            ("PaperSize", "A4纸")
        ));

        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetPageMargins,
            "设置页边距",
            "设置页边距",
            ("TopMargin", "72"),
            ("BottomMargin", "72"),
            ("LeftMargin", "90"),
            ("RightMargin", "90")
        ));

        // 3. 水印设置测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetWatermarkText,
            "设置水印文字",
            "设置水印文字为机密",
            ("WatermarkText", "机密")
        ));

        // 4. 表格操作测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetTableRowsColumns,
            "创建表格",
            "创建3行3列的表格",
            ("Rows", "3"),
            ("Columns", "3")
        ));

        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetTableCellContent,
            "设置单元格内容",
            "设置第1行第1列内容",
            ("RowNumber", "1"),
            ("ColumnNumber", "1"),
            ("CellContent", "标题")
        ));

        // 5. 图形设置测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.InsertAutoShape,
            "插入自选图形",
            "插入矩形图形",
            ("ShapeType", "矩形")
        ));

        // 6. 文本框设置测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.SetTextBoxContent,
            "设置文本框内容",
            "设置文本框内容为测试文字",
            ("TextContent", "这是测试文本框")
        ));

        // 7. 其他操作测试
        testQuestion.OperationPoints.Add(CreateOperationPoint(
            WordKnowledgeType.FindAndReplace,
            "查找替换",
            "查找测试并替换为示例",
            ("FindText", "测试"),
            ("ReplaceText", "示例"),
            ("ReplaceCount", "全部")
        ));

        testModule.Questions.Add(testQuestion);
        return testModule;
    }

    /// <summary>
    /// 创建操作点
    /// </summary>
    private static OperationPoint CreateOperationPoint(
        WordKnowledgeType knowledgeType,
        string name,
        string description,
        params (string Name, string Value)[] parameters)
    {
        OperationPoint operationPoint = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            WordKnowledgeType = knowledgeType,
            ModuleType = ModuleType.Word,
            IsEnabled = true,
            Parameters = []
        };

        foreach ((string paramName, string paramValue) in parameters)
        {
            operationPoint.Parameters.Add(new ConfigurationParameter
            {
                Id = Guid.NewGuid(),
                Name = paramName,
                Value = paramValue
            });
        }

        return operationPoint;
    }

    /// <summary>
    /// 运行测试
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>测试结果</returns>
    public static async Task<DocumentGenerationResult> RunTestAsync(string outputPath)
    {
        WordDocumentGenerator generator = new();
        ExamModule testModule = CreateTestModule();
        
        // 创建进度报告器
        Progress<DocumentGenerationProgress> progress = new(p =>
        {
            Console.WriteLine($"进度: {p.ProgressPercentage}% - {p.CurrentStep}");
            if (!string.IsNullOrEmpty(p.CurrentOperationPoint))
            {
                Console.WriteLine($"当前操作: {p.CurrentOperationPoint}");
            }
        });

        // 验证模块
        DocumentValidationResult validation = generator.ValidateModule(testModule);
        if (!validation.IsValid)
        {
            Console.WriteLine($"模块验证失败: {string.Join(", ", validation.ErrorMessages)}");
            return DocumentGenerationResult.Failure("模块验证失败");
        }

        Console.WriteLine($"模块验证成功: {validation.Details}");

        // 生成文档
        DocumentGenerationResult result = await generator.GenerateDocumentAsync(testModule, outputPath, progress);
        
        if (result.IsSuccess)
        {
            Console.WriteLine($"文档生成成功!");
            Console.WriteLine($"文件路径: {result.FilePath}");
            Console.WriteLine($"处理操作点: {result.ProcessedOperationPoints}/{result.TotalOperationPoints}");
            Console.WriteLine($"耗时: {result.Duration.TotalSeconds:F2}秒");
            if (!string.IsNullOrEmpty(result.Details))
            {
                Console.WriteLine($"详细信息: {result.Details}");
            }
        }
        else
        {
            Console.WriteLine($"文档生成失败: {result.ErrorMessage}");
            if (!string.IsNullOrEmpty(result.Details))
            {
                Console.WriteLine($"错误详情: {result.Details}");
            }
        }

        return result;
    }
}
