using ExamLab.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// Word文档生成器扩展方法2
/// </summary>
public static class WordDocumentGeneratorExtensions2
{
    /// <summary>
    /// 应用设置图片阴影
    /// </summary>
    public static void ApplySetImageShadow(Body body, OperationPoint operationPoint)
    {
        string shadowType = GetParameterValue(operationPoint, "ShadowType", "无阴影");
        string shadowColor = GetParameterValue(operationPoint, "ShadowColor", "#808080");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片阴影：类型{shadowType}，颜色{shadowColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片环绕方式
    /// </summary>
    public static void ApplySetImageWrapStyle(Body body, OperationPoint operationPoint)
    {
        string wrapStyle = GetParameterValue(operationPoint, "WrapStyle", "嵌入型");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片环绕方式：{wrapStyle}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置图片位置
    /// </summary>
    public static void ApplySetImagePosition(Body body, OperationPoint operationPoint)
    {
        string horizontalPositionType = GetParameterValue(operationPoint, "HorizontalPositionType", "对齐方式");
        string horizontalAlignment = GetParameterValue(operationPoint, "HorizontalAlignment", "左对齐");
        string verticalPositionType = GetParameterValue(operationPoint, "VerticalPositionType", "对齐方式");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 图片位置：水平{horizontalPositionType}-{horizontalAlignment}，垂直{verticalPositionType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置文本框边框颜色
    /// </summary>
    public static void ApplySetTextBoxBorderColor(Body body, OperationPoint operationPoint)
    {
        string borderColor = GetParameterValue(operationPoint, "BorderColor", "#000000");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[文本框设置] 文本框边框颜色：{borderColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置文本框中文字内容
    /// </summary>
    public static void ApplySetTextBoxContent(Body body, OperationPoint operationPoint)
    {
        string textContent = GetParameterValue(operationPoint, "TextContent", "文本框内容");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[文本框设置] 文本框内容：{textContent}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置文本框中文字大小
    /// </summary>
    public static void ApplySetTextBoxTextSize(Body body, OperationPoint operationPoint)
    {
        string textSize = GetParameterValue(operationPoint, "TextSize", "12");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[文本框设置] 文本框文字大小：{textSize}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置文本框位置
    /// </summary>
    public static void ApplySetTextBoxPosition(Body body, OperationPoint operationPoint)
    {
        string horizontalPositionType = GetParameterValue(operationPoint, "HorizontalPositionType", "对齐方式");
        string verticalPositionType = GetParameterValue(operationPoint, "VerticalPositionType", "对齐方式");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[文本框设置] 文本框位置：水平{horizontalPositionType}，垂直{verticalPositionType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置文本框环绕方式
    /// </summary>
    public static void ApplySetTextBoxWrapStyle(Body body, OperationPoint operationPoint)
    {
        string wrapStyle = GetParameterValue(operationPoint, "WrapStyle", "嵌入型");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[文本框设置] 文本框环绕方式：{wrapStyle}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用查找与替换
    /// </summary>
    public static void ApplyFindAndReplace(Body body, OperationPoint operationPoint)
    {
        string findText = GetParameterValue(operationPoint, "FindText", "查找内容");
        string replaceText = GetParameterValue(operationPoint, "ReplaceText", "替换内容");
        string replaceCount = GetParameterValue(operationPoint, "ReplaceCount", "全部");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[其他操作] 查找与替换：查找'{findText}'，替换为'{replaceText}'，替换{replaceCount}次"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置指定文字字号
    /// </summary>
    public static void ApplySetSpecificTextFontSize(Body body, OperationPoint operationPoint)
    {
        string targetText = GetParameterValue(operationPoint, "TargetText", "目标文字");
        string fontSize = GetParameterValue(operationPoint, "FontSize", "14");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[其他操作] 设置指定文字字号：文字'{targetText}'，字号{fontSize}磅"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置页码
    /// </summary>
    public static void ApplySetPageNumber(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string pageNumberPosition = GetParameterValue(operationPoint, "PageNumberPosition", "页面底端");
        string pageNumberFormat = GetParameterValue(operationPoint, "PageNumberFormat", "数字");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页码设置：位置{pageNumberPosition}，格式{pageNumberFormat}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置页面背景
    /// </summary>
    public static void ApplySetPageBackground(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string backgroundColor = GetParameterValue(operationPoint, "BackgroundColor", "无填充");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页面背景：{backgroundColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置页面边框颜色
    /// </summary>
    public static void ApplySetPageBorderColor(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string borderColor = GetParameterValue(operationPoint, "BorderColor", "黑色");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页面边框颜色：{borderColor}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用插入图片
    /// </summary>
    public static void ApplyInsertImage(Body body, OperationPoint operationPoint)
    {
        string imagePath = GetParameterValue(operationPoint, "ImagePath", "图片路径");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[图片设置] 插入图片：{imagePath}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用插入文本框
    /// </summary>
    public static void ApplyInsertTextBox(Body body, OperationPoint operationPoint)
    {
        string textContent = GetParameterValue(operationPoint, "TextContent", "文本框内容");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[文本框设置] 插入文本框：{textContent}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置页面方向
    /// </summary>
    public static void ApplySetPageOrientation(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string orientation = GetParameterValue(operationPoint, "Orientation", "纵向");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 页面方向：{orientation}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置分栏
    /// </summary>
    public static void ApplySetColumns(WordprocessingDocument document, OperationPoint operationPoint)
    {
        string columnCount = GetParameterValue(operationPoint, "ColumnCount", "1");
        string columnSpacing = GetParameterValue(operationPoint, "ColumnSpacing", "1.25");
        
        Body body = document.MainDocumentPart!.Document.Body!;
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[页面设置] 分栏设置：{columnCount}栏，间距{columnSpacing}厘米"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置分页符
    /// </summary>
    public static void ApplyInsertPageBreak(Body body, OperationPoint operationPoint)
    {
        string breakType = GetParameterValue(operationPoint, "BreakType", "分页符");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[其他操作] 插入分页符：{breakType}"));
        paragraph.Append(run);
        body.Append(paragraph);
    }

    /// <summary>
    /// 应用设置分节符
    /// </summary>
    public static void ApplyInsertSectionBreak(Body body, OperationPoint operationPoint)
    {
        string breakType = GetParameterValue(operationPoint, "BreakType", "下一页");
        
        Paragraph paragraph = new();
        Run run = new();
        run.Append(new Text($"[其他操作] 插入分节符：{breakType}"));
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
