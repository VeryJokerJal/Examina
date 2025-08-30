using ExamLab.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// Word文档生成器扩展方法
/// </summary>
public static class WordDocumentGeneratorExtensions
{
    /// <summary>
    /// 应用页脚文字设置
    /// </summary>
    public static void ApplyFooterText(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string footerText = GetParameterValue(operationPoint, "FooterText", "页脚内容");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页脚设置] 页脚文字：{footerText}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页脚字体设置
    /// </summary>
    public static void ApplyFooterFont(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string footerFont = GetParameterValue(operationPoint, "FooterFont", "宋体");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页脚设置] 页脚字体：{footerFont}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页脚字号设置
    /// </summary>
    public static void ApplyFooterFontSize(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string footerFontSize = GetParameterValue(operationPoint, "FooterFontSize", "10");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页脚设置] 页脚字号：{footerFontSize}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用页脚对齐方式设置
    /// </summary>
    public static void ApplyFooterAlignment(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string footerAlignment = GetParameterValue(operationPoint, "FooterAlignment", "居中对齐");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页脚设置] 页脚对齐方式：{footerAlignment}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用水印字体设置
    /// </summary>
    public static void ApplyWatermarkFont(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string watermarkFont = GetParameterValue(operationPoint, "WatermarkFont", "宋体");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();
        
        runProperties.Append(new Color() { Val = "C0C0C0" }); // 灰色
        runProperties.Append(new Italic());
        run.Append(runProperties);
        run.Append(new Text($"[水印设置] 水印字体：{watermarkFont}"));
        
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用水印字号设置
    /// </summary>
    public static void ApplyWatermarkFontSize(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string watermarkFontSize = GetParameterValue(operationPoint, "WatermarkFontSize", "36");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        RunProperties runProperties = new();
        
        runProperties.Append(new Color() { Val = "C0C0C0" }); // 灰色
        runProperties.Append(new Italic());
        run.Append(runProperties);
        run.Append(new Text($"[水印设置] 水印字号：{watermarkFontSize}磅"));
        
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格底纹设置
    /// </summary>
    public static void ApplyTableShading(Body body, OperationPoint operationPoint)
    {
        string areaType = GetParameterValue(operationPoint, "AreaType", "行");
        string areaNumber = GetParameterValue(operationPoint, "AreaNumber", "1");
        string startPosition = GetParameterValue(operationPoint, "StartPosition", "1");
        string endPosition = GetParameterValue(operationPoint, "EndPosition", "1");
        string shadingColor = GetParameterValue(operationPoint, "ShadingColor", "#FFFF00");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 表格底纹：{areaType}区域{areaNumber}，从{startPosition}到{endPosition}，颜色{shadingColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格行高设置
    /// </summary>
    public static void ApplyTableRowHeight(Body body, OperationPoint operationPoint)
    {
        string startRow = GetParameterValue(operationPoint, "StartRow", "1");
        string endRow = GetParameterValue(operationPoint, "EndRow", "1");
        string rowHeight = GetParameterValue(operationPoint, "RowHeight", "20");
        string heightType = GetParameterValue(operationPoint, "HeightType", "固定值");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 表格行高：第{startRow}行到第{endRow}行，高度{rowHeight}磅，类型{heightType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格列宽设置
    /// </summary>
    public static void ApplyTableColumnWidth(Body body, OperationPoint operationPoint)
    {
        string startColumn = GetParameterValue(operationPoint, "StartColumn", "1");
        string endColumn = GetParameterValue(operationPoint, "EndColumn", "1");
        string columnWidth = GetParameterValue(operationPoint, "ColumnWidth", "100");
        string widthType = GetParameterValue(operationPoint, "WidthType", "固定宽度");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 表格列宽：第{startColumn}列到第{endColumn}列，宽度{columnWidth}磅，类型{widthType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格单元格内容设置
    /// </summary>
    public static void ApplyTableCellContent(Body body, OperationPoint operationPoint)
    {
        string rowNumber = GetParameterValue(operationPoint, "RowNumber", "1");
        string columnNumber = GetParameterValue(operationPoint, "ColumnNumber", "1");
        string cellContent = GetParameterValue(operationPoint, "CellContent", "单元格内容");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 单元格内容：第{rowNumber}行第{columnNumber}列，内容：{cellContent}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格单元格对齐方式设置
    /// </summary>
    public static void ApplyTableCellAlignment(Body body, OperationPoint operationPoint)
    {
        string rowNumber = GetParameterValue(operationPoint, "RowNumber", "1");
        string columnNumber = GetParameterValue(operationPoint, "ColumnNumber", "1");
        string horizontalAlignment = GetParameterValue(operationPoint, "HorizontalAlignment", "左对齐");
        string verticalAlignment = GetParameterValue(operationPoint, "VerticalAlignment", "居中对齐");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 单元格对齐：第{rowNumber}行第{columnNumber}列，水平{horizontalAlignment}，垂直{verticalAlignment}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格对齐方式设置
    /// </summary>
    public static void ApplyTableAlignment(Body body, OperationPoint operationPoint)
    {
        string tableAlignment = GetParameterValue(operationPoint, "TableAlignment", "居中");
        string leftIndent = GetParameterValue(operationPoint, "LeftIndent", "0");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 表格对齐：{tableAlignment}，左缩进{leftIndent}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用合并表格单元格设置
    /// </summary>
    public static void ApplyMergeTableCells(Body body, OperationPoint operationPoint)
    {
        string startRow = GetParameterValue(operationPoint, "StartRow", "1");
        string startColumn = GetParameterValue(operationPoint, "StartColumn", "1");
        string endRow = GetParameterValue(operationPoint, "EndRow", "1");
        string endColumn = GetParameterValue(operationPoint, "EndColumn", "1");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 合并单元格：从第{startRow}行第{startColumn}列到第{endRow}行第{endColumn}列"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用表格标题内容设置
    /// </summary>
    public static void ApplyTableHeaderContent(Body body, OperationPoint operationPoint)
    {
        string columnNumber = GetParameterValue(operationPoint, "ColumnNumber", "1");
        string headerContent = GetParameterValue(operationPoint, "HeaderContent", "标题内容");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[表格设置] 表格标题：第{columnNumber}列，标题：{headerContent}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用项目符号与编号设置
    /// </summary>
    public static void ApplyBulletNumbering(Body body, OperationPoint operationPoint)
    {
        string paragraphNumbers = GetParameterValue(operationPoint, "ParagraphNumbers", "1#2#3");
        string numberingType = GetParameterValue(operationPoint, "NumberingType", "数字编号");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[编号设置] 段落编号：{paragraphNumbers}，编号类型：{numberingType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用插入自选图形
    /// </summary>
    public static void ApplyInsertAutoShape(Body body, OperationPoint operationPoint)
    {
        string shapeType = GetParameterValue(operationPoint, "ShapeType", "矩形");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 插入自选图形：{shapeType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片高度和宽度
    /// </summary>
    public static void ApplySetImageSize(Body body, OperationPoint operationPoint)
    {
        string imageHeight = GetParameterValue(operationPoint, "ImageHeight", "100");
        string imageWidth = GetParameterValue(operationPoint, "ImageWidth", "100");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片尺寸：高度{imageHeight}磅，宽度{imageWidth}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形大小
    /// </summary>
    public static void ApplySetShapeSize(Body body, OperationPoint operationPoint)
    {
        string shapeHeight = GetParameterValue(operationPoint, "ShapeHeight", "50");
        string shapeWidth = GetParameterValue(operationPoint, "ShapeWidth", "100");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形尺寸：高度{shapeHeight}磅，宽度{shapeWidth}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形线条颜色
    /// </summary>
    public static void ApplySetShapeLineColor(Body body, OperationPoint operationPoint)
    {
        string lineColor = GetParameterValue(operationPoint, "LineColor", "#000000");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形线条颜色：{lineColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形填充颜色
    /// </summary>
    public static void ApplySetShapeFillColor(Body body, OperationPoint operationPoint)
    {
        string fillColor = GetParameterValue(operationPoint, "FillColor", "#FFFFFF");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形填充颜色：{fillColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形文字大小
    /// </summary>
    public static void ApplySetShapeTextSize(Body body, OperationPoint operationPoint)
    {
        string fontSize = GetParameterValue(operationPoint, "FontSize", "12");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形文字大小：{fontSize}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形文字颜色
    /// </summary>
    public static void ApplySetShapeTextColor(Body body, OperationPoint operationPoint)
    {
        string textColor = GetParameterValue(operationPoint, "TextColor", "#000000");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形文字颜色：{textColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形文字内容
    /// </summary>
    public static void ApplySetShapeTextContent(Body body, OperationPoint operationPoint)
    {
        string textContent = GetParameterValue(operationPoint, "TextContent", "图形文字");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形文字内容：{textContent}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置自选图形位置
    /// </summary>
    public static void ApplySetShapePosition(Body body, OperationPoint operationPoint)
    {
        string horizontalPositionType = GetParameterValue(operationPoint, "HorizontalPositionType", "对齐方式");
        string horizontalAlignment = GetParameterValue(operationPoint, "HorizontalAlignment", "左对齐");
        string verticalPositionType = GetParameterValue(operationPoint, "VerticalPositionType", "对齐方式");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图形设置] 自选图形位置：水平{horizontalPositionType}-{horizontalAlignment}，垂直{verticalPositionType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片边框复合类型
    /// </summary>
    public static void ApplySetImageBorderCompoundType(Body body, OperationPoint operationPoint)
    {
        string compoundType = GetParameterValue(operationPoint, "CompoundType", "单线");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片边框复合类型：{compoundType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片边框虚线类型
    /// </summary>
    public static void ApplySetImageBorderDashType(Body body, OperationPoint operationPoint)
    {
        string dashType = GetParameterValue(operationPoint, "DashType", "实线");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片边框虚线类型：{dashType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片边框宽度
    /// </summary>
    public static void ApplySetImageBorderWidth(Body body, OperationPoint operationPoint)
    {
        string borderWidth = GetParameterValue(operationPoint, "BorderWidth", "1");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片边框宽度：{borderWidth}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片边框颜色
    /// </summary>
    public static void ApplySetImageBorderColor(Body body, OperationPoint operationPoint)
    {
        string borderColor = GetParameterValue(operationPoint, "BorderColor", "#000000");

        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片边框颜色：{borderColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
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
