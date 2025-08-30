using ExamLab.Models;
using ExamLab.Services.DocumentGeneration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// Word文档生成器
/// </summary>
public class WordDocumentGenerator : IDocumentGenerationService
{
    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    public ModuleType GetSupportedModuleType() => ModuleType.Word;

    /// <summary>
    /// 获取推荐的文件扩展名
    /// </summary>
    public string GetRecommendedFileExtension() => ".docx";

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    public string GetFileTypeDescription() => "Word文档";

    /// <summary>
    /// 验证模块是否可以生成文档
    /// </summary>
    public DocumentValidationResult ValidateModule(ExamModule module)
    {
        DocumentValidationResult result = new() { IsValid = true };

        // 验证模块类型
        if (module.Type != ModuleType.Word)
        {
            result.AddError($"模块类型不匹配，期望Word模块，实际为{module.Type}");
            return result;
        }

        // 验证模块是否启用
        if (!module.IsEnabled)
        {
            result.AddWarning("模块未启用");
        }

        // 验证是否有题目
        if (module.Questions.Count == 0)
        {
            result.AddError("模块中没有题目");
            return result;
        }

        // 验证题目中是否有Word操作点
        int totalWordOperationPoints = 0;
        foreach (Question question in module.Questions)
        {
            int wordOperationPoints = question.OperationPoints.Count(op => 
                op.ModuleType == ModuleType.Word && op.IsEnabled);
            totalWordOperationPoints += wordOperationPoints;
        }

        if (totalWordOperationPoints == 0)
        {
            result.AddError("模块中没有启用的Word操作点");
            return result;
        }

        result.Details = $"验证通过：{module.Questions.Count}个题目，{totalWordOperationPoints}个Word操作点";
        return result;
    }

    /// <summary>
    /// 异步生成Word文档
    /// </summary>
    public async Task<DocumentGenerationResult> GenerateDocumentAsync(ExamModule module, string filePath, IProgress<DocumentGenerationProgress>? progress = null)
    {
        DateTime startTime = DateTime.Now;
        
        try
        {
            // 验证模块
            DocumentValidationResult validation = ValidateModule(module);
            if (!validation.IsValid)
            {
                return DocumentGenerationResult.Failure($"模块验证失败：{string.Join(", ", validation.ErrorMessages)}");
            }

            // 收集所有Word操作点
            List<OperationPoint> allWordOperationPoints = [];
            foreach (Question question in module.Questions)
            {
                List<OperationPoint> wordOps = question.OperationPoints
                    .Where(op => op.ModuleType == ModuleType.Word && op.IsEnabled)
                    .ToList();
                allWordOperationPoints.AddRange(wordOps);
            }

            int totalOperationPoints = allWordOperationPoints.Count;
            int processedCount = 0;

            // 报告初始进度
            progress?.Report(DocumentGenerationProgress.Create("开始生成Word文档", 0, totalOperationPoints));

            // 创建Word文档
            await Task.Run(() =>
            {
                using WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                
                // 创建主文档部分
                MainDocumentPart mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // 添加文档标题
                progress?.Report(DocumentGenerationProgress.Create("添加文档标题", processedCount, totalOperationPoints));
                AddDocumentTitle(body, module.Name);

                // 处理每个操作点
                foreach (OperationPoint operationPoint in allWordOperationPoints)
                {
                    string operationName = GetOperationDisplayName(operationPoint);
                    progress?.Report(DocumentGenerationProgress.Create("处理操作点", processedCount, totalOperationPoints, operationName));

                    try
                    {
                        ApplyOperationPoint(document, body, operationPoint);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续处理其他操作点
                        Debug.WriteLine($"处理操作点 {operationName} 时出错: {ex.Message}");
                    }

                    processedCount++;
                    progress?.Report(DocumentGenerationProgress.Create("处理操作点", processedCount, totalOperationPoints, operationName));
                }

                // 保存文档
                progress?.Report(DocumentGenerationProgress.Create("保存文档", processedCount, totalOperationPoints));
                document.Save();
            });

            TimeSpan duration = DateTime.Now - startTime;
            string details = $"成功生成Word文档，包含{module.Questions.Count}个题目的{totalOperationPoints}个操作点";
            
            return DocumentGenerationResult.Success(filePath, processedCount, totalOperationPoints, duration, details);
        }
        catch (Exception ex)
        {
            return DocumentGenerationResult.Failure($"生成Word文档时发生错误：{ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 添加文档标题
    /// </summary>
    private static void AddDocumentTitle(Body body, string title)
    {
        Paragraph titleParagraph = new();
        Run titleRun = new();
        RunProperties titleRunProperties = new();
        
        titleRunProperties.Append(new Bold());
        titleRunProperties.Append(new FontSize() { Val = "28" }); // 14pt = 28 half-points
        titleRunProperties.Append(new RunFonts() { Ascii = "微软雅黑" });
        
        titleRun.Append(titleRunProperties);
        titleRun.Append(new Text(title));
        
        titleParagraph.Append(titleRun);
        
        ParagraphProperties titleParagraphProperties = new();
        titleParagraphProperties.Append(new Justification() { Val = JustificationValues.Center });
        titleParagraph.PrependChild(titleParagraphProperties);
        
        body.Append(titleParagraph);
        
        // 添加空行
        body.Append(new Paragraph());
    }

    /// <summary>
    /// 应用操作点到文档
    /// </summary>
    private static void ApplyOperationPoint(WordprocessingDocument document, Body body, OperationPoint operationPoint)
    {
        // 根据Word知识点类型应用不同的操作
        if (operationPoint.WordKnowledgeType.HasValue)
        {
            switch (operationPoint.WordKnowledgeType.Value)
            {
                case WordKnowledgeType.SetParagraphFont:
                    ApplyParagraphFont(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphFontSize:
                    ApplyParagraphFontSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphAlignment:
                    ApplyParagraphAlignment(body, operationPoint);
                    break;
                case WordKnowledgeType.SetWatermarkText:
                    ApplyWatermark(document, operationPoint);
                    break;
                case WordKnowledgeType.SetTableRowsColumns:
                    ApplyTable(body, operationPoint);
                    break;
                default:
                    // 对于未实现的操作点，添加说明文本
                    AddOperationDescription(body, operationPoint);
                    break;
            }
        }
        else
        {
            // 如果没有指定Word知识点类型，添加通用说明
            AddOperationDescription(body, operationPoint);
        }
    }

    /// <summary>
    /// 应用段落字体设置
    /// </summary>
    private static void ApplyParagraphFont(Body body, OperationPoint operationPoint)
    {
        string fontFamily = GetParameterValue(operationPoint, "FontFamily", "宋体");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");
        
        // 创建示例段落
        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();
        
        runProperties.Append(new RunFonts() { Ascii = fontFamily });
        run.Append(runProperties);
        run.Append(new Text($"这是第{paragraphNumberStr}段落，字体设置为：{fontFamily}"));
        
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落字号设置
    /// </summary>
    private static void ApplyParagraphFontSize(Body body, OperationPoint operationPoint)
    {
        string fontSize = GetParameterValue(operationPoint, "FontSize", "12");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");
        
        // 转换字号为半点单位
        if (int.TryParse(fontSize, out int fontSizeInt))
        {
            Paragraph paragraph = new();
            Run run = new();
            RunProperties runProperties = new();
            
            runProperties.Append(new FontSize() { Val = (fontSizeInt * 2).ToString() });
            run.Append(runProperties);
            run.Append(new Text($"这是第{paragraphNumberStr}段落，字号设置为：{fontSize}磅"));
            
            paragraph.Append(run);
            body.Append(paragraph);
        }
    }

    /// <summary>
    /// 应用段落对齐方式
    /// </summary>
    private static void ApplyParagraphAlignment(Body body, OperationPoint operationPoint)
    {
        string alignment = GetParameterValue(operationPoint, "Alignment", "左对齐");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");
        
        JustificationValues justification = alignment switch
        {
            "左对齐" => JustificationValues.Left,
            "居中对齐" => JustificationValues.Center,
            "右对齐" => JustificationValues.Right,
            "两端对齐" => JustificationValues.Both,
            _ => JustificationValues.Left
        };
        
        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();
        paragraphProperties.Append(new Justification() { Val = justification });
        paragraph.Append(paragraphProperties);
        
        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，对齐方式设置为：{alignment}"));
        paragraph.Append(run);
        
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用水印
    /// </summary>
    private static void ApplyWatermark(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string watermarkText = GetParameterValue(operationPoint, "WatermarkText", "机密");
        
        // 简化的水印实现 - 在文档中添加说明
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();
        
        runProperties.Append(new Color() { Val = "C0C0C0" }); // 灰色
        runProperties.Append(new Italic());
        run.Append(runProperties);
        run.Append(new Text($"[水印文字：{watermarkText}]"));
        
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格
    /// </summary>
    private static void ApplyTable(Body body, OperationPoint operationPoint)
    {
        string rowsStr = GetParameterValue(operationPoint, "Rows", "3");
        string columnsStr = GetParameterValue(operationPoint, "Columns", "3");
        
        if (int.TryParse(rowsStr, out int rows) && int.TryParse(columnsStr, out int columns))
        {
            Table table = new();
            
            // 创建表格属性
            TableProperties tableProperties = new();
            TableBorders tableBorders = new();
            tableBorders.Append(new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 });
            tableBorders.Append(new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 });
            tableBorders.Append(new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 });
            tableBorders.Append(new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 });
            tableBorders.Append(new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 });
            tableBorders.Append(new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 });
            tableProperties.Append(tableBorders);
            table.Append(tableProperties);
            
            // 创建表格行和单元格
            for (int i = 0; i < rows; i++)
            {
                TableRow tableRow = new();
                for (int j = 0; j < columns; j++)
                {
                    TableCell tableCell = new();
                    Paragraph cellParagraph = new();
                    Run cellRun = new();
                    cellRun.Append(new Text($"行{i + 1}列{j + 1}"));
                    cellParagraph.Append(cellRun);
                    tableCell.Append(cellParagraph);
                    tableRow.Append(tableCell);
                }
                table.Append(tableRow);
            }
            
            body.Append(table);
        }
    }

    /// <summary>
    /// 添加操作点描述
    /// </summary>
    private static void AddOperationDescription(Body body, OperationPoint operationPoint)
    {
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"操作点：{operationPoint.Name} - {operationPoint.Description}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 获取操作点的显示名称
    /// </summary>
    private static string GetOperationDisplayName(OperationPoint operationPoint)
    {
        return !string.IsNullOrEmpty(operationPoint.Name) ? operationPoint.Name : 
               operationPoint.WordKnowledgeType?.ToString() ?? "未知操作";
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    private static string GetParameterValue(OperationPoint operationPoint, string parameterName, string defaultValue)
    {
        ConfigurationParameter? parameter = operationPoint.Parameters.FirstOrDefault(p => p.Name == parameterName);
        return parameter?.Value ?? defaultValue;
    }
}
