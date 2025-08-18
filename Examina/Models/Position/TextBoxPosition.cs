using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.Models.Position;

/// <summary>
/// 文本框位置模型
/// </summary>
public class TextBoxPosition : GraphicElementPosition
{
    /// <summary>
    /// 文本内容
    /// </summary>
    [Reactive] public string TextContent { get; set; } = string.Empty;
    
    /// <summary>
    /// 字体名称
    /// </summary>
    [Reactive] public string FontFamily { get; set; } = "宋体";
    
    /// <summary>
    /// 字体大小
    /// </summary>
    [Reactive] public double FontSize { get; set; } = 12;
    
    /// <summary>
    /// 字体颜色
    /// </summary>
    [Reactive] public string FontColor { get; set; } = "#000000";
    
    /// <summary>
    /// 是否粗体
    /// </summary>
    [Reactive] public bool IsBold { get; set; } = false;
    
    /// <summary>
    /// 是否斜体
    /// </summary>
    [Reactive] public bool IsItalic { get; set; } = false;
    
    /// <summary>
    /// 是否下划线
    /// </summary>
    [Reactive] public bool IsUnderline { get; set; } = false;
    
    /// <summary>
    /// 文本水平对齐方式
    /// </summary>
    [Reactive] public TextHorizontalAlignment TextHorizontalAlign { get; set; } = TextHorizontalAlignment.Left;
    
    /// <summary>
    /// 文本垂直对齐方式
    /// </summary>
    [Reactive] public TextVerticalAlignment TextVerticalAlign { get; set; } = TextVerticalAlignment.Top;
    
    /// <summary>
    /// 行间距
    /// </summary>
    [Reactive] public double LineSpacing { get; set; } = 1.0;
    
    /// <summary>
    /// 段落间距
    /// </summary>
    [Reactive] public double ParagraphSpacing { get; set; } = 0;
    
    /// <summary>
    /// 文本框边框设置
    /// </summary>
    [Reactive] public TextBoxBorder Border { get; set; } = new();
    
    /// <summary>
    /// 文本框背景设置
    /// </summary>
    [Reactive] public TextBoxBackground Background { get; set; } = new();
    
    /// <summary>
    /// 是否自动调整大小
    /// </summary>
    [Reactive] public bool AutoResize { get; set; } = false;
    
    /// <summary>
    /// 最小宽度
    /// </summary>
    [Reactive] public double MinWidth { get; set; } = 50;
    
    /// <summary>
    /// 最小高度
    /// </summary>
    [Reactive] public double MinHeight { get; set; } = 20;
    
    /// <summary>
    /// 最大宽度
    /// </summary>
    [Reactive] public double? MaxWidth { get; set; }
    
    /// <summary>
    /// 最大高度
    /// </summary>
    [Reactive] public double? MaxHeight { get; set; }
    
    /// <summary>
    /// 文本溢出处理方式
    /// </summary>
    [Reactive] public TextOverflowMode OverflowMode { get; set; } = TextOverflowMode.Clip;
    
    /// <summary>
    /// 是否允许换行
    /// </summary>
    [Reactive] public bool AllowWordWrap { get; set; } = true;
    
    /// <summary>
    /// 文本缩进
    /// </summary>
    [Reactive] public double TextIndent { get; set; } = 0;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public TextBoxPosition()
    {
        ElementType = GraphicElementType.TextBox;
        ElementName = "文本框";
    }
    
    /// <summary>
    /// 计算文本实际需要的尺寸
    /// </summary>
    /// <returns>计算出的尺寸</returns>
    public (double Width, double Height) CalculateTextSize()
    {
        // 简化的文本尺寸计算逻辑
        // 实际应用中需要根据字体、字号等进行精确计算
        
        if (string.IsNullOrEmpty(TextContent))
            return (MinWidth, MinHeight);
        
        // 估算字符宽度和高度
        double charWidth = FontSize * 0.6; // 简化估算
        double lineHeight = FontSize * LineSpacing;
        
        var lines = TextContent.Split('\n');
        double maxLineWidth = 0;
        
        foreach (var line in lines)
        {
            double lineWidth = line.Length * charWidth;
            if (lineWidth > maxLineWidth)
                maxLineWidth = lineWidth;
        }
        
        double calculatedWidth = Math.Max(maxLineWidth + Padding.Left + Padding.Right, MinWidth);
        double calculatedHeight = Math.Max(lines.Length * lineHeight + Padding.Top + Padding.Bottom, MinHeight);
        
        // 应用最大尺寸限制
        if (MaxWidth.HasValue && calculatedWidth > MaxWidth.Value)
            calculatedWidth = MaxWidth.Value;
        if (MaxHeight.HasValue && calculatedHeight > MaxHeight.Value)
            calculatedHeight = MaxHeight.Value;
        
        return (calculatedWidth, calculatedHeight);
    }
    
    /// <summary>
    /// 自动调整文本框尺寸
    /// </summary>
    public void AutoResizeToFitText()
    {
        if (AutoResize)
        {
            var (calculatedWidth, calculatedHeight) = CalculateTextSize();
            Resize(calculatedWidth, calculatedHeight);
        }
    }
    
    /// <summary>
    /// 验证文本框位置设置是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public override bool IsValid()
    {
        if (!base.IsValid()) return false;
        
        // 验证字体大小
        if (FontSize <= 0) return false;
        
        // 验证尺寸限制
        if (Width < MinWidth || Height < MinHeight) return false;
        if (MaxWidth.HasValue && Width > MaxWidth.Value) return false;
        if (MaxHeight.HasValue && Height > MaxHeight.Value) return false;
        
        return true;
    }
    
    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>文本框位置的字符串表示</returns>
    public override string ToString()
    {
        return $"文本框 '{ElementName}': {Position} - 尺寸: {Width}x{Height} {SizeUnit} - 内容: '{TextContent.Substring(0, Math.Min(20, TextContent.Length))}{(TextContent.Length > 20 ? "..." : "")}'";
    }
}

/// <summary>
/// 文本水平对齐方式枚举
/// </summary>
public enum TextHorizontalAlignment
{
    /// <summary>
    /// 左对齐
    /// </summary>
    Left,
    
    /// <summary>
    /// 居中对齐
    /// </summary>
    Center,
    
    /// <summary>
    /// 右对齐
    /// </summary>
    Right,
    
    /// <summary>
    /// 两端对齐
    /// </summary>
    Justify
}

/// <summary>
/// 文本垂直对齐方式枚举
/// </summary>
public enum TextVerticalAlignment
{
    /// <summary>
    /// 顶部对齐
    /// </summary>
    Top,
    
    /// <summary>
    /// 居中对齐
    /// </summary>
    Middle,
    
    /// <summary>
    /// 底部对齐
    /// </summary>
    Bottom
}

/// <summary>
/// 文本溢出处理方式枚举
/// </summary>
public enum TextOverflowMode
{
    /// <summary>
    /// 裁剪
    /// </summary>
    Clip,
    
    /// <summary>
    /// 省略号
    /// </summary>
    Ellipsis,
    
    /// <summary>
    /// 自动换行
    /// </summary>
    Wrap,
    
    /// <summary>
    /// 滚动
    /// </summary>
    Scroll
}

/// <summary>
/// 文本框边框设置
/// </summary>
public class TextBoxBorder : ReactiveObject
{
    /// <summary>
    /// 是否显示边框
    /// </summary>
    [Reactive] public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// 边框颜色
    /// </summary>
    [Reactive] public string Color { get; set; } = "#000000";
    
    /// <summary>
    /// 边框宽度
    /// </summary>
    [Reactive] public double Width { get; set; } = 1;
    
    /// <summary>
    /// 边框样式
    /// </summary>
    [Reactive] public BorderStyle Style { get; set; } = BorderStyle.Solid;
}

/// <summary>
/// 文本框背景设置
/// </summary>
public class TextBoxBackground : ReactiveObject
{
    /// <summary>
    /// 是否显示背景
    /// </summary>
    [Reactive] public bool IsVisible { get; set; } = false;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    [Reactive] public string Color { get; set; } = "#FFFFFF";
    
    /// <summary>
    /// 背景透明度（0-1）
    /// </summary>
    [Reactive] public double Opacity { get; set; } = 1.0;
}

/// <summary>
/// 边框样式枚举
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// 实线
    /// </summary>
    Solid,
    
    /// <summary>
    /// 虚线
    /// </summary>
    Dashed,
    
    /// <summary>
    /// 点线
    /// </summary>
    Dotted,
    
    /// <summary>
    /// 双线
    /// </summary>
    Double,
    
    /// <summary>
    /// 无边框
    /// </summary>
    None
}
