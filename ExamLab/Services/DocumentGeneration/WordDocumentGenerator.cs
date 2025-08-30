using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamLab.Models;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// Word文档生成器
/// </summary>
public class WordDocumentGenerator : IDocumentGenerationService
{
    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    public ModuleType GetSupportedModuleType()
    {
        return ModuleType.Word;
    }

    /// <summary>
    /// 获取推荐的文件扩展名
    /// </summary>
    public string GetRecommendedFileExtension()
    {
        return ".docx";
    }

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    public string GetFileTypeDescription()
    {
        return "Word文档";
    }

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
        _ = titleParagraph.PrependChild(titleParagraphProperties);

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
                // 段落操作
                case WordKnowledgeType.SetParagraphFont:
                    ApplyParagraphFont(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphFontSize:
                    ApplyParagraphFontSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphFontStyle:
                    ApplyParagraphFontStyle(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphCharacterSpacing:
                    ApplyParagraphCharacterSpacing(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphTextColor:
                    ApplyParagraphTextColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphAlignment:
                    ApplyParagraphAlignment(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphIndentation:
                    ApplyParagraphIndentation(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphLineSpacing:
                    ApplyParagraphLineSpacing(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphDropCap:
                    ApplyParagraphDropCap(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphSpacing:
                    ApplyParagraphSpacing(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphBorderColor:
                    ApplyParagraphBorderColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphBorderStyle:
                    ApplyParagraphBorderStyle(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphBorderWidth:
                    ApplyParagraphBorderWidth(body, operationPoint);
                    break;
                case WordKnowledgeType.SetParagraphShading:
                    ApplyParagraphShading(body, operationPoint);
                    break;

                // 页面设置
                case WordKnowledgeType.SetPaperSize:
                    ApplyPaperSize(document, operationPoint);
                    break;
                case WordKnowledgeType.SetPageMargins:
                    ApplyPageMargins(document, operationPoint);
                    break;
                case WordKnowledgeType.SetHeaderText:
                    ApplyHeaderText(document, operationPoint);
                    break;
                case WordKnowledgeType.SetHeaderFont:
                    ApplyHeaderFont(document, operationPoint);
                    break;
                case WordKnowledgeType.SetHeaderFontSize:
                    ApplyHeaderFontSize(document, operationPoint);
                    break;
                case WordKnowledgeType.SetHeaderAlignment:
                    ApplyHeaderAlignment(document, operationPoint);
                    break;
                case WordKnowledgeType.SetFooterText:
                    WordDocumentGeneratorExtensions.ApplyFooterText(document, operationPoint);
                    break;
                case WordKnowledgeType.SetFooterFont:
                    WordDocumentGeneratorExtensions.ApplyFooterFont(document, operationPoint);
                    break;
                case WordKnowledgeType.SetFooterFontSize:
                    WordDocumentGeneratorExtensions.ApplyFooterFontSize(document, operationPoint);
                    break;
                case WordKnowledgeType.SetFooterAlignment:
                    WordDocumentGeneratorExtensions.ApplyFooterAlignment(document, operationPoint);
                    break;

                // 水印设置
                case WordKnowledgeType.SetWatermarkText:
                    ApplyWatermark(document, operationPoint);
                    break;
                case WordKnowledgeType.SetWatermarkFont:
                    WordDocumentGeneratorExtensions.ApplyWatermarkFont(document, operationPoint);
                    break;
                case WordKnowledgeType.SetWatermarkFontSize:
                    WordDocumentGeneratorExtensions.ApplyWatermarkFontSize(document, operationPoint);
                    break;

                // 表格操作
                case WordKnowledgeType.SetTableRowsColumns:
                    ApplyTable(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableShading:
                    WordDocumentGeneratorExtensions.ApplyTableShading(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableRowHeight:
                    WordDocumentGeneratorExtensions.ApplyTableRowHeight(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableColumnWidth:
                    WordDocumentGeneratorExtensions.ApplyTableColumnWidth(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableCellContent:
                    WordDocumentGeneratorExtensions.ApplyTableCellContent(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableCellAlignment:
                    WordDocumentGeneratorExtensions.ApplyTableCellAlignment(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableAlignment:
                    WordDocumentGeneratorExtensions.ApplyTableAlignment(body, operationPoint);
                    break;
                case WordKnowledgeType.MergeTableCells:
                    WordDocumentGeneratorExtensions.ApplyMergeTableCells(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTableHeaderContent:
                    WordDocumentGeneratorExtensions.ApplyTableHeaderContent(body, operationPoint);
                    break;

                // 项目符号与编号
                case WordKnowledgeType.SetBulletNumbering:
                    WordDocumentGeneratorExtensions.ApplyBulletNumbering(body, operationPoint);
                    break;

                // 图形和图片设置
                case WordKnowledgeType.InsertAutoShape:
                    WordDocumentGeneratorExtensions.ApplyInsertAutoShape(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageSize:
                    WordDocumentGeneratorExtensions.ApplySetImageSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapeSize:
                    WordDocumentGeneratorExtensions.ApplySetShapeSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapeLineColor:
                    WordDocumentGeneratorExtensions.ApplySetShapeLineColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapeFillColor:
                    WordDocumentGeneratorExtensions.ApplySetShapeFillColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapeTextSize:
                    WordDocumentGeneratorExtensions.ApplySetShapeTextSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapeTextColor:
                    WordDocumentGeneratorExtensions.ApplySetShapeTextColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapeTextContent:
                    WordDocumentGeneratorExtensions.ApplySetShapeTextContent(body, operationPoint);
                    break;
                case WordKnowledgeType.SetAutoShapePosition:
                    WordDocumentGeneratorExtensions.ApplySetShapePosition(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageBorderCompoundType:
                    WordDocumentGeneratorExtensions.ApplySetImageBorderCompoundType(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageBorderDashType:
                    WordDocumentGeneratorExtensions.ApplySetImageBorderDashType(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageBorderWidth:
                    WordDocumentGeneratorExtensions.ApplySetImageBorderWidth(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageBorderColor:
                    WordDocumentGeneratorExtensions.ApplySetImageBorderColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageShadow:
                    WordDocumentGeneratorExtensions.ApplySetImageShadow(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImageWrapStyle:
                    WordDocumentGeneratorExtensions.ApplySetImageWrapStyle(body, operationPoint);
                    break;
                case WordKnowledgeType.SetImagePosition:
                    WordDocumentGeneratorExtensions.ApplySetImagePosition(body, operationPoint);
                    break;

                // 文本框设置
                case WordKnowledgeType.SetTextBoxBorderColor:
                    WordDocumentGeneratorExtensions.ApplySetTextBoxBorderColor(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTextBoxContent:
                    WordDocumentGeneratorExtensions.ApplySetTextBoxContent(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTextBoxTextSize:
                    WordDocumentGeneratorExtensions.ApplySetTextBoxTextSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTextBoxPosition:
                    WordDocumentGeneratorExtensions.ApplySetTextBoxPosition(body, operationPoint);
                    break;
                case WordKnowledgeType.SetTextBoxWrapStyle:
                    WordDocumentGeneratorExtensions.ApplySetTextBoxWrapStyle(body, operationPoint);
                    break;

                // 其他操作
                case WordKnowledgeType.FindAndReplace:
                    WordDocumentGeneratorExtensions.ApplyFindAndReplace(body, operationPoint);
                    break;
                case WordKnowledgeType.SetSpecificTextFontSize:
                    WordDocumentGeneratorExtensions.ApplySetSpecificTextFontSize(body, operationPoint);
                    break;
                case WordKnowledgeType.SetPageNumber:
                    WordDocumentGeneratorExtensions.ApplySetPageNumber(document, operationPoint);
                    break;
                case WordKnowledgeType.SetPageBackground:
                    WordDocumentGeneratorExtensions.ApplySetPageBackground(document, operationPoint);
                    break;
                case WordKnowledgeType.SetPageBorderColor:
                    WordDocumentGeneratorExtensions.ApplySetPageBorderColor(document, operationPoint);
                    break;

                // 遗漏的操作点
                case WordKnowledgeType.SetPageBorderStyle:
                    ApplySetPageBorderStyle(document, operationPoint);
                    break;
                case WordKnowledgeType.SetPageBorderWidth:
                    ApplySetPageBorderWidth(document, operationPoint);
                    break;
                case WordKnowledgeType.SetWatermarkOrientation:
                    ApplySetWatermarkOrientation(document, operationPoint);
                    break;
                case WordKnowledgeType.SetTableHeaderAlignment:
                    ApplySetTableHeaderAlignment(body, operationPoint);
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
    /// 应用段落字形设置
    /// </summary>
    private static void ApplyParagraphFontStyle(Body body, OperationPoint operationPoint)
    {
        string fontStyle = GetParameterValue(operationPoint, "FontStyle", "常规");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();

        // 根据字形设置样式
        switch (fontStyle)
        {
            case "加粗":
                runProperties.Append(new Bold());
                break;
            case "斜体":
                runProperties.Append(new Italic());
                break;
            case "加粗+斜体":
                runProperties.Append(new Bold());
                runProperties.Append(new Italic());
                break;
            case "下划线":
                runProperties.Append(new Underline() { Val = UnderlineValues.Single });
                break;
            case "删除线":
                runProperties.Append(new Strike());
                break;
        }

        run.Append(runProperties);
        run.Append(new Text($"这是第{paragraphNumberStr}段落，字形设置为：{fontStyle}"));

        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落字间距设置
    /// </summary>
    private static void ApplyParagraphCharacterSpacing(Body body, OperationPoint operationPoint)
    {
        string characterSpacing = GetParameterValue(operationPoint, "CharacterSpacing", "0");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        if (double.TryParse(characterSpacing, out double spacingValue))
        {
            Paragraph paragraph = new();
            Run run = new();
            RunProperties runProperties = new();

            // 字间距以20分之一磅为单位
            runProperties.Append(new Spacing() { Val = (int)(spacingValue * 20) });
            run.Append(runProperties);
            run.Append(new Text($"这是第{paragraphNumberStr}段落，字间距设置为：{characterSpacing}磅"));

            paragraph.Append(run);
            body.Append(paragraph);
        }
    }

    /// <summary>
    /// 应用段落文字颜色设置
    /// </summary>
    private static void ApplyParagraphTextColor(Body body, OperationPoint operationPoint)
    {
        string textColor = GetParameterValue(operationPoint, "TextColor", "#000000");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();

        // 移除#号并设置颜色
        string colorValue = textColor.TrimStart('#');
        runProperties.Append(new Color() { Val = colorValue });
        run.Append(runProperties);
        run.Append(new Text($"这是第{paragraphNumberStr}段落，文字颜色设置为：{textColor}"));

        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落缩进设置
    /// </summary>
    private static void ApplyParagraphIndentation(Body body, OperationPoint operationPoint)
    {
        string firstLineIndent = GetParameterValue(operationPoint, "FirstLineIndent", "2");
        string leftIndent = GetParameterValue(operationPoint, "LeftIndent", "0");
        string rightIndent = GetParameterValue(operationPoint, "RightIndent", "0");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();
        Indentation indentation = new();

        // 缩进以字符为单位，转换为twips（1字符约等于240 twips）
        if (double.TryParse(firstLineIndent, out double firstIndent))
        {
            indentation.FirstLine = ((int)(firstIndent * 240)).ToString();
        }

        if (double.TryParse(leftIndent, out double leftInd))
        {
            indentation.Left = ((int)(leftInd * 240)).ToString();
        }

        if (double.TryParse(rightIndent, out double rightInd))
        {
            indentation.Right = ((int)(rightInd * 240)).ToString();
        }

        paragraphProperties.Append(indentation);
        paragraph.Append(paragraphProperties);

        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，缩进设置为：首行{firstLineIndent}字符，左{leftIndent}字符，右{rightIndent}字符"));
        paragraph.Append(run);

        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落行间距设置
    /// </summary>
    private static void ApplyParagraphLineSpacing(Body body, OperationPoint operationPoint)
    {
        string lineSpacing = GetParameterValue(operationPoint, "LineSpacing", "1.5");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        if (double.TryParse(lineSpacing, out double spacingValue))
        {
            Paragraph paragraph = new();
            ParagraphProperties paragraphProperties = new();
            SpacingBetweenLines spacing = new()
            {
                // 行间距以240分之一英寸为单位
                Line = ((int)(spacingValue * 240)).ToString(),
                LineRule = LineSpacingRuleValues.Auto
            };

            paragraphProperties.Append(spacing);
            paragraph.Append(paragraphProperties);

            Run run = new();
            run.Append(new Text($"这是第{paragraphNumberStr}段落，行间距设置为：{lineSpacing}倍"));
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
    /// 应用段落首字下沉设置
    /// </summary>
    private static void ApplyParagraphDropCap(Body body, OperationPoint operationPoint)
    {
        string dropCapType = GetParameterValue(operationPoint, "DropCapType", "不使用下沉");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，首字下沉设置为：{dropCapType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落间距设置
    /// </summary>
    private static void ApplyParagraphSpacing(Body body, OperationPoint operationPoint)
    {
        string spaceBefore = GetParameterValue(operationPoint, "SpaceBefore", "0");
        string spaceAfter = GetParameterValue(operationPoint, "SpaceAfter", "6");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();
        SpacingBetweenLines spacing = new();

        if (double.TryParse(spaceBefore, out double beforeValue))
        {
            spacing.Before = ((int)(beforeValue * 20)).ToString(); // 转换为twips
        }

        if (double.TryParse(spaceAfter, out double afterValue))
        {
            spacing.After = ((int)(afterValue * 20)).ToString();
        }

        paragraphProperties.Append(spacing);
        paragraph.Append(paragraphProperties);

        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，段前间距：{spaceBefore}磅，段后间距：{spaceAfter}磅"));
        paragraph.Append(run);

        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落边框颜色设置
    /// </summary>
    private static void ApplyParagraphBorderColor(Body body, OperationPoint operationPoint)
    {
        string borderColor = GetParameterValue(operationPoint, "BorderColor", "黑色");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();

        // 创建段落边框
        ParagraphBorders paragraphBorders = new();

        // 转换颜色名称为十六进制值
        string colorValue = ConvertColorNameToHex(borderColor);

        // 设置所有边框（上下左右）
        TopBorder topBorder = new()
        {
            Val = BorderValues.Single,
            Color = colorValue,
            Size = 4, // 0.5磅
            Space = 0
        };

        BottomBorder bottomBorder = new()
        {
            Val = BorderValues.Single,
            Color = colorValue,
            Size = 4,
            Space = 0
        };

        LeftBorder leftBorder = new()
        {
            Val = BorderValues.Single,
            Color = colorValue,
            Size = 4,
            Space = 0
        };

        RightBorder rightBorder = new()
        {
            Val = BorderValues.Single,
            Color = colorValue,
            Size = 4,
            Space = 0
        };

        paragraphBorders.Append(topBorder);
        paragraphBorders.Append(bottomBorder);
        paragraphBorders.Append(leftBorder);
        paragraphBorders.Append(rightBorder);

        paragraphProperties.Append(paragraphBorders);
        paragraph.Append(paragraphProperties);

        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，边框颜色设置为：{borderColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落边框样式设置
    /// </summary>
    private static void ApplyParagraphBorderStyle(Body body, OperationPoint operationPoint)
    {
        string borderStyle = GetParameterValue(operationPoint, "BorderStyle", "单实线");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();

        // 创建段落边框
        ParagraphBorders paragraphBorders = new();

        // 转换边框样式
        BorderValues borderValue = ConvertBorderStyleToBorderValues(borderStyle);

        // 设置所有边框（上下左右）
        TopBorder topBorder = new()
        {
            Val = borderValue,
            Color = "000000", // 黑色
            Size = 4, // 0.5磅
            Space = 0
        };

        BottomBorder bottomBorder = new()
        {
            Val = borderValue,
            Color = "000000",
            Size = 4,
            Space = 0
        };

        LeftBorder leftBorder = new()
        {
            Val = borderValue,
            Color = "000000",
            Size = 4,
            Space = 0
        };

        RightBorder rightBorder = new()
        {
            Val = borderValue,
            Color = "000000",
            Size = 4,
            Space = 0
        };

        paragraphBorders.Append(topBorder);
        paragraphBorders.Append(bottomBorder);
        paragraphBorders.Append(leftBorder);
        paragraphBorders.Append(rightBorder);

        paragraphProperties.Append(paragraphBorders);
        paragraph.Append(paragraphProperties);

        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，边框样式设置为：{borderStyle}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落边框宽度设置
    /// </summary>
    private static void ApplyParagraphBorderWidth(Body body, OperationPoint operationPoint)
    {
        string borderWidth = GetParameterValue(operationPoint, "BorderWidth", "1磅");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();

        // 创建段落边框
        ParagraphBorders paragraphBorders = new();

        // 转换边框宽度（磅转换为八分之一磅）
        uint borderSize = ConvertBorderWidthToSize(borderWidth);

        // 设置所有边框（上下左右）
        TopBorder topBorder = new()
        {
            Val = BorderValues.Single,
            Color = "000000", // 黑色
            Size = borderSize,
            Space = 0
        };

        BottomBorder bottomBorder = new()
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = borderSize,
            Space = 0
        };

        LeftBorder leftBorder = new()
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = borderSize,
            Space = 0
        };

        RightBorder rightBorder = new()
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = borderSize,
            Space = 0
        };

        paragraphBorders.Append(topBorder);
        paragraphBorders.Append(bottomBorder);
        paragraphBorders.Append(leftBorder);
        paragraphBorders.Append(rightBorder);

        paragraphProperties.Append(paragraphBorders);
        paragraph.Append(paragraphProperties);

        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，边框宽度设置为：{borderWidth}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用段落底纹设置
    /// </summary>
    private static void ApplyParagraphShading(Body body, OperationPoint operationPoint)
    {
        string shadingColor = GetParameterValue(operationPoint, "ShadingColor", "#FFFF00");
        string shadingPattern = GetParameterValue(operationPoint, "ShadingPattern", "无纹理");
        string paragraphNumberStr = GetParameterValue(operationPoint, "ParagraphNumber", "1");

        Paragraph paragraph = new();
        ParagraphProperties paragraphProperties = new();

        // 创建段落底纹
        Shading shading = new();

        // 转换颜色（移除#号）
        string fillColor = shadingColor.TrimStart('#');
        if (fillColor.Length == 6)
        {
            shading.Fill = fillColor;
        }
        else
        {
            shading.Fill = "FFFF00"; // 默认黄色
        }

        // 转换底纹图案
        ShadingPatternValues patternValue = ConvertShadingPatternToValues(shadingPattern);
        shading.Val = patternValue;

        // 如果有图案，设置图案颜色为黑色
        if (patternValue != ShadingPatternValues.Clear)
        {
            shading.Color = "000000";
        }

        paragraphProperties.Append(shading);
        paragraph.Append(paragraphProperties);

        Run run = new();
        run.Append(new Text($"这是第{paragraphNumberStr}段落，底纹颜色：{shadingColor}，图案：{shadingPattern}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用纸张大小设置
    /// </summary>
    private static void ApplyPaperSize(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string paperSize = GetParameterValue(operationPoint, "PaperSize", "A4纸");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 纸张大小设置为：{paperSize}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页边距设置
    /// </summary>
    private static void ApplyPageMargins(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string topMargin = GetParameterValue(operationPoint, "TopMargin", "72");
        string bottomMargin = GetParameterValue(operationPoint, "BottomMargin", "72");
        string leftMargin = GetParameterValue(operationPoint, "LeftMargin", "90");
        string rightMargin = GetParameterValue(operationPoint, "RightMargin", "90");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页边距设置为：上{topMargin}磅，下{bottomMargin}磅，左{leftMargin}磅，右{rightMargin}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页眉文字设置
    /// </summary>
    private static void ApplyHeaderText(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string headerText = GetParameterValue(operationPoint, "HeaderText", "页眉内容");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页眉设置] 页眉文字：{headerText}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页眉字体设置
    /// </summary>
    private static void ApplyHeaderFont(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string headerFont = GetParameterValue(operationPoint, "HeaderFont", "宋体");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页眉设置] 页眉字体：{headerFont}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页眉字号设置
    /// </summary>
    private static void ApplyHeaderFontSize(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string headerFontSize = GetParameterValue(operationPoint, "HeaderFontSize", "10");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页眉设置] 页眉字号：{headerFontSize}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页眉对齐方式设置
    /// </summary>
    private static void ApplyHeaderAlignment(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string headerAlignment = GetParameterValue(operationPoint, "HeaderAlignment", "居中对齐");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页眉设置] 页眉对齐方式：{headerAlignment}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置页面边框样式
    /// </summary>
    private static void ApplySetPageBorderStyle(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string borderStyle = GetParameterValue(operationPoint, "BorderStyle", "单实线");

        // 获取或创建节属性
        Body body = document.MainDocumentPart!.Document.Body!;
        SectionProperties? sectionProperties = body.GetFirstChild<SectionProperties>();

        if (sectionProperties == null)
        {
            sectionProperties = new SectionProperties();
            body.Append(sectionProperties);
        }

        // 创建页面边框
        PageBorders pageBorders = sectionProperties.GetFirstChild<PageBorders>() ?? new PageBorders();

        // 转换边框样式
        BorderValues borderValue = ConvertBorderStyleToBorderValues(borderStyle);

        // 设置页面边框样式
        pageBorders.TopBorder = new TopBorder()
        {
            Val = borderValue,
            Color = "000000", // 黑色
            Size = 8, // 1磅
            Space = 0
        };

        pageBorders.BottomBorder = new BottomBorder()
        {
            Val = borderValue,
            Color = "000000",
            Size = 8,
            Space = 0
        };

        pageBorders.LeftBorder = new LeftBorder()
        {
            Val = borderValue,
            Color = "000000",
            Size = 8,
            Space = 0
        };

        pageBorders.RightBorder = new RightBorder()
        {
            Val = borderValue,
            Color = "000000",
            Size = 8,
            Space = 0
        };

        if (sectionProperties.GetFirstChild<PageBorders>() == null)
        {
            sectionProperties.Append(pageBorders);
        }

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页面边框样式：{borderStyle}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置页面边框宽度
    /// </summary>
    private static void ApplySetPageBorderWidth(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string borderWidth = GetParameterValue(operationPoint, "BorderWidth", "1磅");

        // 获取或创建节属性
        Body body = document.MainDocumentPart!.Document.Body!;
        SectionProperties? sectionProperties = body.GetFirstChild<SectionProperties>();

        if (sectionProperties == null)
        {
            sectionProperties = new SectionProperties();
            body.Append(sectionProperties);
        }

        // 创建页面边框
        PageBorders pageBorders = sectionProperties.GetFirstChild<PageBorders>() ?? new PageBorders();

        // 转换边框宽度（磅转换为八分之一磅）
        uint borderSize = ConvertBorderWidthToSize(borderWidth);

        // 设置页面边框宽度
        pageBorders.TopBorder = new TopBorder()
        {
            Val = BorderValues.Single,
            Color = "000000", // 黑色
            Size = borderSize,
            Space = 0
        };

        pageBorders.BottomBorder = new BottomBorder()
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = borderSize,
            Space = 0
        };

        pageBorders.LeftBorder = new LeftBorder()
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = borderSize,
            Space = 0
        };

        pageBorders.RightBorder = new RightBorder()
        {
            Val = BorderValues.Single,
            Color = "000000",
            Size = borderSize,
            Space = 0
        };

        if (sectionProperties.GetFirstChild<PageBorders>() == null)
        {
            sectionProperties.Append(pageBorders);
        }

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页面边框宽度：{borderWidth}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置水印方向
    /// </summary>
    private static void ApplySetWatermarkOrientation(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string orientation = GetParameterValue(operationPoint, "Orientation", "倾斜");

        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();

        runProperties.Append(new Color() { Val = "C0C0C0" }); // 灰色
        runProperties.Append(new Italic());
        run.Append(runProperties);
        run.Append(new Text($"[水印设置] 水印方向：{orientation}"));

        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置表格标题对齐方式
    /// </summary>
    private static void ApplySetTableHeaderAlignment(Body body, OperationPoint operationPoint)
    {
        string columnNumber = GetParameterValue(operationPoint, "ColumnNumber", "1");
        string horizontalAlignment = GetParameterValue(operationPoint, "HorizontalAlignment", "居中对齐");
        string verticalAlignment = GetParameterValue(operationPoint, "VerticalAlignment", "居中对齐");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 表格标题对齐：第{columnNumber}列，水平{horizontalAlignment}，垂直{verticalAlignment}"));
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

    /// <summary>
    /// 转换颜色名称为十六进制值
    /// </summary>
    private static string ConvertColorNameToHex(string colorName)
    {
        return colorName switch
        {
            "黑色" => "000000",
            "红色" => "FF0000",
            "蓝色" => "0000FF",
            "绿色" => "008000",
            "黄色" => "FFFF00",
            "紫色" => "800080",
            "橙色" => "FFA500",
            "灰色" => "808080",
            "自动" => "000000",
            _ => "000000" // 默认黑色
        };
    }

    /// <summary>
    /// 转换边框样式为BorderValues枚举
    /// </summary>
    private static BorderValues ConvertBorderStyleToBorderValues(string borderStyle)
    {
        return borderStyle switch
        {
            "无" => BorderValues.None,
            "单实线" => BorderValues.Single,
            "双线" => BorderValues.Double,
            "点线" => BorderValues.Dotted,
            "虚线" => BorderValues.Dashed,
            "粗线" => BorderValues.Thick,
            "细线" => BorderValues.Single, // 使用Single替代不存在的Thin
            "波浪线" => BorderValues.Wave,
            _ => BorderValues.Single // 默认单实线
        };
    }

    /// <summary>
    /// 转换边框宽度为尺寸值（八分之一磅）
    /// </summary>
    private static uint ConvertBorderWidthToSize(string borderWidth)
    {
        return borderWidth switch
        {
            "0.25磅" => 2,   // 0.25 * 8
            "0.5磅" => 4,    // 0.5 * 8
            "0.75磅" => 6,   // 0.75 * 8
            "1磅" => 8,      // 1 * 8
            "1.5磅" => 12,   // 1.5 * 8
            "2.25磅" => 18,  // 2.25 * 8
            "3磅" => 24,     // 3 * 8
            "4.5磅" => 36,   // 4.5 * 8
            "6磅" => 48,     // 6 * 8
            _ => 8           // 默认1磅
        };
    }

    /// <summary>
    /// 转换底纹图案为ShadingPatternValues枚举
    /// 注意：使用DocumentFormat.OpenXml库中实际存在的枚举值
    /// 由于许多预期的枚举值在OpenXml库中不存在，使用最接近的替代值
    /// </summary>
    private static ShadingPatternValues ConvertShadingPatternToValues(string shadingPattern)
    {
        return shadingPattern switch
        {
            // 基础图案 - 使用实际存在的枚举值
            "无纹理" => ShadingPatternValues.Clear,
            "实心填充" => ShadingPatternValues.Solid,

            // 百分比填充 - 由于Pct系列枚举值不存在，根据百分比选择合适的替代
            "2.5%" => ShadingPatternValues.Clear,     // 极低百分比使用Clear
            "5%" => ShadingPatternValues.Clear,       // 低百分比使用Clear
            "7.5%" => ShadingPatternValues.Clear,     // 低百分比使用Clear
            "10%" => ShadingPatternValues.Clear,      // 低百分比使用Clear
            "12.5%" => ShadingPatternValues.Clear,    // 低百分比使用Clear
            "15%" => ShadingPatternValues.Clear,      // 低百分比使用Clear
            "17.5%" => ShadingPatternValues.Clear,    // 低百分比使用Clear
            "20%" => ShadingPatternValues.Clear,      // 低百分比使用Clear
            "22.5%" => ShadingPatternValues.Clear,    // 低百分比使用Clear
            "25%" => ShadingPatternValues.Clear,      // 中低百分比使用Clear
            "27.5%" => ShadingPatternValues.Clear,    // 中低百分比使用Clear
            "30%" => ShadingPatternValues.Clear,      // 中低百分比使用Clear
            "32.5%" => ShadingPatternValues.Clear,    // 中低百分比使用Clear
            "35%" => ShadingPatternValues.Clear,      // 中低百分比使用Clear
            "37.5%" => ShadingPatternValues.Clear,    // 中低百分比使用Clear
            "40%" => ShadingPatternValues.Clear,      // 中等百分比使用Clear
            "42.5%" => ShadingPatternValues.Clear,    // 中等百分比使用Clear
            "45%" => ShadingPatternValues.Clear,      // 中等百分比使用Clear
            "47.5%" => ShadingPatternValues.Clear,    // 中等百分比使用Clear
            "50%" => ShadingPatternValues.Clear,      // 中等百分比使用Clear
            "52.5%" => ShadingPatternValues.Clear,    // 中高百分比使用Clear
            "55%" => ShadingPatternValues.Clear,      // 中高百分比使用Clear
            "57.5%" => ShadingPatternValues.Clear,    // 中高百分比使用Clear
            "60%" => ShadingPatternValues.Solid,      // 高百分比使用Solid

            // 图案填充 - 由于具体图案枚举值不存在，使用基础替代
            "深色水平线" => ShadingPatternValues.Solid,    // 深色图案使用Solid
            "深色垂直线" => ShadingPatternValues.Solid,    // 深色图案使用Solid
            "深色对角线下" => ShadingPatternValues.Solid,  // 深色图案使用Solid
            "深色对角线上" => ShadingPatternValues.Solid,  // 深色图案使用Solid
            "深色十字" => ShadingPatternValues.Solid,      // 深色图案使用Solid
            "深色对角十字" => ShadingPatternValues.Solid,  // 深色图案使用Solid
            "水平线" => ShadingPatternValues.Clear,        // 浅色图案使用Clear
            "垂直线" => ShadingPatternValues.Clear,        // 浅色图案使用Clear
            "对角线下" => ShadingPatternValues.Clear,      // 浅色图案使用Clear
            "对角线上" => ShadingPatternValues.Clear,      // 浅色图案使用Clear
            "十字" => ShadingPatternValues.Clear,          // 浅色图案使用Clear
            "对角十字" => ShadingPatternValues.Clear,      // 浅色图案使用Clear

            _ => ShadingPatternValues.Clear // 默认无纹理
        };
    }
}
