using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Examina.Models.Position;

namespace Examina.Services;

/// <summary>
/// 文本框位置服务实现
/// </summary>
public class TextBoxPositionService : ITextBoxPositionService
{
    private readonly IPositionService _positionService;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="positionService">位置服务</param>
    public TextBoxPositionService(IPositionService positionService)
    {
        _positionService = positionService;
    }
    
    #region 创建和验证
    
    /// <summary>
    /// 创建文本框位置配置
    /// </summary>
    /// <param name="name">文本框名称</param>
    /// <param name="content">文本内容</param>
    /// <param name="position">位置参数</param>
    /// <returns>文本框位置配置</returns>
    public TextBoxPosition CreateTextBoxPosition(string name, string content, PositionParameter position)
    {
        var textBox = new TextBoxPosition
        {
            ElementName = name,
            TextContent = content,
            Position = position.Clone()
        };
        
        // 自动调整尺寸以适应内容
        textBox.AutoResizeToFitText();
        
        return textBox;
    }
    
    /// <summary>
    /// 验证文本框位置配置
    /// </summary>
    /// <param name="textBox">文本框位置配置</param>
    /// <returns>验证结果</returns>
    public bool ValidateTextBoxPosition(TextBoxPosition textBox)
    {
        return textBox?.IsValid() ?? false;
    }
    
    #endregion
    
    #region 位置设置
    
    /// <summary>
    /// 设置文本框位置
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="unit">位置单位</param>
    public void SetTextBoxPosition(TextBoxPosition textBox, double x, double y, PositionUnit unit = PositionUnit.Point)
    {
        if (textBox == null || textBox.IsPositionLocked) return;
        
        textBox.Position.X = x;
        textBox.Position.Y = y;
        textBox.Position.Unit = unit;
        textBox.LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 设置文本框对齐方式
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="horizontalAlign">水平对齐</param>
    /// <param name="verticalAlign">垂直对齐</param>
    public void SetTextBoxAlignment(TextBoxPosition textBox, TextHorizontalAlignment horizontalAlign, TextVerticalAlignment verticalAlign)
    {
        if (textBox == null) return;
        
        textBox.TextHorizontalAlign = horizontalAlign;
        textBox.TextVerticalAlign = verticalAlign;
        textBox.LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 自动调整文本框尺寸以适应内容
    /// </summary>
    /// <param name="textBox">文本框</param>
    public void AutoResizeTextBox(TextBoxPosition textBox)
    {
        textBox?.AutoResizeToFitText();
    }
    
    #endregion
    
    #region 样式设置
    
    /// <summary>
    /// 设置文本框字体属性
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="fontFamily">字体名称</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="fontColor">字体颜色</param>
    public void SetTextBoxFont(TextBoxPosition textBox, string fontFamily, double fontSize, string fontColor)
    {
        if (textBox == null) return;
        
        textBox.FontFamily = fontFamily;
        textBox.FontSize = fontSize;
        textBox.FontColor = fontColor;
        textBox.LastModified = DateTime.Now;
        
        // 字体改变后可能需要重新计算尺寸
        if (textBox.AutoResize)
        {
            textBox.AutoResizeToFitText();
        }
    }
    
    /// <summary>
    /// 设置文本框样式
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="isBold">是否粗体</param>
    /// <param name="isItalic">是否斜体</param>
    /// <param name="isUnderline">是否下划线</param>
    public void SetTextBoxStyle(TextBoxPosition textBox, bool isBold, bool isItalic, bool isUnderline)
    {
        if (textBox == null) return;
        
        textBox.IsBold = isBold;
        textBox.IsItalic = isItalic;
        textBox.IsUnderline = isUnderline;
        textBox.LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 设置文本框边框
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="isVisible">是否显示边框</param>
    /// <param name="color">边框颜色</param>
    /// <param name="width">边框宽度</param>
    /// <param name="style">边框样式</param>
    public void SetTextBoxBorder(TextBoxPosition textBox, bool isVisible, string color, double width, BorderStyle style)
    {
        if (textBox == null) return;
        
        textBox.Border.IsVisible = isVisible;
        textBox.Border.Color = color;
        textBox.Border.Width = width;
        textBox.Border.Style = style;
        textBox.LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 设置文本框背景
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="isVisible">是否显示背景</param>
    /// <param name="color">背景颜色</param>
    /// <param name="opacity">透明度</param>
    public void SetTextBoxBackground(TextBoxPosition textBox, bool isVisible, string color, double opacity)
    {
        if (textBox == null) return;
        
        textBox.Background.IsVisible = isVisible;
        textBox.Background.Color = color;
        textBox.Background.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        textBox.LastModified = DateTime.Now;
    }
    
    #endregion
    
    #region 尺寸计算
    
    /// <summary>
    /// 计算文本框内容所需的尺寸
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <returns>计算出的尺寸</returns>
    public (double Width, double Height) CalculateTextBoxSize(TextBoxPosition textBox)
    {
        return textBox?.CalculateTextSize() ?? (0, 0);
    }
    
    #endregion
    
    #region 批量操作
    
    /// <summary>
    /// 批量设置文本框位置
    /// </summary>
    /// <param name="textBoxes">文本框列表</param>
    /// <param name="arrangement">排列方式</param>
    /// <param name="spacing">间距</param>
    /// <returns>设置任务</returns>
    public async Task BatchSetTextBoxPositions(IEnumerable<TextBoxPosition> textBoxes, ElementArrangement arrangement, double spacing)
    {
        var elements = textBoxes.Cast<GraphicElementPosition>();
        await _positionService.AutoArrangeElements(elements, arrangement, spacing);
    }
    
    #endregion
    
    #region 对齐操作
    
    /// <summary>
    /// 对齐文本框到容器
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="containerWidth">容器宽度</param>
    /// <param name="containerHeight">容器高度</param>
    /// <param name="horizontalAlign">水平对齐方式</param>
    /// <param name="verticalAlign">垂直对齐方式</param>
    public void AlignTextBoxToContainer(
        TextBoxPosition textBox,
        double containerWidth,
        double containerHeight,
        HorizontalAlignment horizontalAlign,
        VerticalAlignment verticalAlign)
    {
        if (textBox == null || textBox.IsPositionLocked) return;
        
        var alignedPosition = _positionService.GetAlignedPosition(
            textBox, horizontalAlign, verticalAlign, containerWidth, containerHeight);
        
        textBox.Position = alignedPosition;
        textBox.LastModified = DateTime.Now;
    }
    
    #endregion
    
    #region 参数模板
    
    /// <summary>
    /// 创建文本框位置参数模板
    /// </summary>
    /// <returns>参数模板列表</returns>
    public List<ConfigurationParameter> CreateTextBoxPositionParameterTemplates()
    {
        return _positionService.CreatePositionParameterTemplates(OperationPointType.SetTextBoxPosition);
    }
    
    /// <summary>
    /// 从配置参数创建文本框位置
    /// </summary>
    /// <param name="parameters">配置参数列表</param>
    /// <returns>文本框位置配置</returns>
    public TextBoxPosition CreateTextBoxFromParameters(IEnumerable<ConfigurationParameter> parameters)
    {
        var paramList = parameters.ToList();
        
        var textBox = new TextBoxPosition();
        
        foreach (var param in paramList)
        {
            switch (param.Name)
            {
                case "PositionX":
                    if (double.TryParse(param.Value, out double x))
                        textBox.Position.X = x;
                    break;
                    
                case "PositionY":
                    if (double.TryParse(param.Value, out double y))
                        textBox.Position.Y = y;
                    break;
                    
                case "HorizontalAlignment":
                    if (Enum.TryParse<TextHorizontalAlignment>(param.Value, out var hAlign))
                        textBox.TextHorizontalAlign = hAlign;
                    break;
                    
                case "VerticalAlignment":
                    if (Enum.TryParse<TextVerticalAlignment>(param.Value, out var vAlign))
                        textBox.TextVerticalAlign = vAlign;
                    break;
                    
                case "FontFamily":
                    textBox.FontFamily = param.Value ?? "宋体";
                    break;
                    
                case "FontSize":
                    if (double.TryParse(param.Value, out double fontSize))
                        textBox.FontSize = fontSize;
                    break;
                    
                case "FontColor":
                    textBox.FontColor = param.Value ?? "#000000";
                    break;
                    
                case "TextContent":
                    textBox.TextContent = param.Value ?? string.Empty;
                    break;
            }
        }
        
        return textBox;
    }
    
    /// <summary>
    /// 将文本框位置转换为配置参数
    /// </summary>
    /// <param name="textBox">文本框位置</param>
    /// <returns>配置参数列表</returns>
    public List<ConfigurationParameter> ConvertTextBoxToParameters(TextBoxPosition textBox)
    {
        var parameters = new List<ConfigurationParameter>();
        
        if (textBox == null) return parameters;
        
        parameters.AddRange([
            new ConfigurationParameter
            {
                Name = "PositionX",
                DisplayName = "水平位置",
                Type = ParameterType.Number,
                Value = textBox.Position.X.ToString(),
                Order = 1
            },
            new ConfigurationParameter
            {
                Name = "PositionY",
                DisplayName = "垂直位置",
                Type = ParameterType.Number,
                Value = textBox.Position.Y.ToString(),
                Order = 2
            },
            new ConfigurationParameter
            {
                Name = "HorizontalAlignment",
                DisplayName = "水平对齐",
                Type = ParameterType.Enum,
                Value = textBox.TextHorizontalAlign.ToString(),
                Order = 3
            },
            new ConfigurationParameter
            {
                Name = "VerticalAlignment",
                DisplayName = "垂直对齐",
                Type = ParameterType.Enum,
                Value = textBox.TextVerticalAlign.ToString(),
                Order = 4
            }
        ]);
        
        return parameters;
    }
    
    #endregion

    #region 区域检测和移动

    /// <summary>
    /// 检查文本框是否在指定区域内
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="areaLeft">区域左边界</param>
    /// <param name="areaTop">区域上边界</param>
    /// <param name="areaRight">区域右边界</param>
    /// <param name="areaBottom">区域下边界</param>
    /// <returns>是否在区域内</returns>
    public bool IsTextBoxInArea(TextBoxPosition textBox, double areaLeft, double areaTop, double areaRight, double areaBottom)
    {
        if (textBox == null) return false;

        var (left, top, right, bottom) = textBox.GetBounds();

        return left >= areaLeft && top >= areaTop && right <= areaRight && bottom <= areaBottom;
    }

    /// <summary>
    /// 移动文本框到指定位置
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="targetX">目标X坐标</param>
    /// <param name="targetY">目标Y坐标</param>
    /// <param name="animationDuration">动画持续时间（毫秒）</param>
    /// <returns>移动任务</returns>
    public async Task MoveTextBoxToPosition(TextBoxPosition textBox, double targetX, double targetY, int animationDuration = 0)
    {
        if (textBox == null || textBox.IsPositionLocked) return;

        if (animationDuration <= 0)
        {
            // 直接移动
            textBox.MoveTo(targetX, targetY);
        }
        else
        {
            // 动画移动
            await AnimateTextBoxMovement(textBox, targetX, targetY, animationDuration);
        }
    }

    /// <summary>
    /// 动画移动文本框
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="targetX">目标X坐标</param>
    /// <param name="targetY">目标Y坐标</param>
    /// <param name="duration">动画持续时间（毫秒）</param>
    /// <returns>动画任务</returns>
    private static async Task AnimateTextBoxMovement(TextBoxPosition textBox, double targetX, double targetY, int duration)
    {
        double startX = textBox.Position.X;
        double startY = textBox.Position.Y;
        double deltaX = targetX - startX;
        double deltaY = targetY - startY;

        int steps = Math.Max(1, duration / 16); // 约60fps
        double stepX = deltaX / steps;
        double stepY = deltaY / steps;

        for (int i = 0; i < steps; i++)
        {
            textBox.Position.X = startX + stepX * i;
            textBox.Position.Y = startY + stepY * i;
            await Task.Delay(16); // 约60fps
        }

        // 确保最终位置准确
        textBox.Position.X = targetX;
        textBox.Position.Y = targetY;
        textBox.LastModified = DateTime.Now;
    }

    #endregion

    #region 复制和锚点

    /// <summary>
    /// 复制文本框位置配置
    /// </summary>
    /// <param name="sourceTextBox">源文本框</param>
    /// <param name="newName">新文本框名称</param>
    /// <returns>复制的文本框配置</returns>
    public TextBoxPosition CloneTextBoxPosition(TextBoxPosition sourceTextBox, string newName)
    {
        if (sourceTextBox == null) return new TextBoxPosition();

        var clonedTextBox = new TextBoxPosition
        {
            ElementName = newName,
            ElementType = sourceTextBox.ElementType,
            Position = sourceTextBox.Position.Clone(),
            Width = sourceTextBox.Width,
            Height = sourceTextBox.Height,
            SizeUnit = sourceTextBox.SizeUnit,
            RotationAngle = sourceTextBox.RotationAngle,
            ZOrder = sourceTextBox.ZOrder,
            IsVisible = sourceTextBox.IsVisible,

            // 文本框特有属性
            TextContent = sourceTextBox.TextContent,
            FontFamily = sourceTextBox.FontFamily,
            FontSize = sourceTextBox.FontSize,
            FontColor = sourceTextBox.FontColor,
            IsBold = sourceTextBox.IsBold,
            IsItalic = sourceTextBox.IsItalic,
            IsUnderline = sourceTextBox.IsUnderline,
            TextHorizontalAlign = sourceTextBox.TextHorizontalAlign,
            TextVerticalAlign = sourceTextBox.TextVerticalAlign,
            LineSpacing = sourceTextBox.LineSpacing,
            ParagraphSpacing = sourceTextBox.ParagraphSpacing,
            AutoResize = sourceTextBox.AutoResize,
            MinWidth = sourceTextBox.MinWidth,
            MinHeight = sourceTextBox.MinHeight,
            MaxWidth = sourceTextBox.MaxWidth,
            MaxHeight = sourceTextBox.MaxHeight,
            OverflowMode = sourceTextBox.OverflowMode,
            AllowWordWrap = sourceTextBox.AllowWordWrap,
            TextIndent = sourceTextBox.TextIndent,

            // 边框和背景
            Border = new TextBoxBorder
            {
                IsVisible = sourceTextBox.Border.IsVisible,
                Color = sourceTextBox.Border.Color,
                Width = sourceTextBox.Border.Width,
                Style = sourceTextBox.Border.Style
            },
            Background = new TextBoxBackground
            {
                IsVisible = sourceTextBox.Background.IsVisible,
                Color = sourceTextBox.Background.Color,
                Opacity = sourceTextBox.Background.Opacity
            }
        };

        return clonedTextBox;
    }

    /// <summary>
    /// 获取文本框的锚点位置
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="anchorType">锚点类型</param>
    /// <returns>锚点位置</returns>
    public (double X, double Y) GetTextBoxAnchorPosition(TextBoxPosition textBox, AnchorType anchorType)
    {
        if (textBox == null) return (0, 0);

        return anchorType switch
        {
            AnchorType.Paragraph => (textBox.Position.X, textBox.Position.Y),
            AnchorType.Page => (textBox.Position.X, textBox.Position.Y),
            AnchorType.Margin => (textBox.Position.X, textBox.Position.Y),
            AnchorType.Character => (textBox.Position.X, textBox.Position.Y),
            _ => (textBox.Position.X, textBox.Position.Y)
        };
    }

    /// <summary>
    /// 设置文本框的环绕方式
    /// </summary>
    /// <param name="textBox">文本框</param>
    /// <param name="wrapStyle">环绕方式</param>
    public void SetTextBoxWrapStyle(TextBoxPosition textBox, WrapStyle wrapStyle)
    {
        if (textBox == null) return;

        textBox.WrapStyle = wrapStyle;
        textBox.LastModified = DateTime.Now;
    }

    #endregion

    #region 验证

    /// <summary>
    /// 验证文本框位置参数的有效性
    /// </summary>
    /// <param name="parameters">位置参数</param>
    /// <returns>验证结果和错误信息</returns>
    public (bool IsValid, string ErrorMessage) ValidateTextBoxPositionParameters(IEnumerable<ConfigurationParameter> parameters)
    {
        var paramList = parameters.ToList();

        // 检查必需参数
        var positionXParam = paramList.FirstOrDefault(p => p.Name == "PositionX");
        var positionYParam = paramList.FirstOrDefault(p => p.Name == "PositionY");

        if (positionXParam == null)
            return (false, "缺少必需参数: PositionX");

        if (positionYParam == null)
            return (false, "缺少必需参数: PositionY");

        // 验证数值参数
        if (!double.TryParse(positionXParam.Value, out double x))
            return (false, "PositionX 参数值无效");

        if (!double.TryParse(positionYParam.Value, out double y))
            return (false, "PositionY 参数值无效");

        if (x < 0)
            return (false, "PositionX 不能为负数");

        if (y < 0)
            return (false, "PositionY 不能为负数");

        // 验证枚举参数
        var hAlignParam = paramList.FirstOrDefault(p => p.Name == "HorizontalAlignment");
        if (hAlignParam != null && !string.IsNullOrEmpty(hAlignParam.Value))
        {
            if (!Enum.TryParse<TextHorizontalAlignment>(hAlignParam.Value, out _))
                return (false, "HorizontalAlignment 参数值无效");
        }

        var vAlignParam = paramList.FirstOrDefault(p => p.Name == "VerticalAlignment");
        if (vAlignParam != null && !string.IsNullOrEmpty(vAlignParam.Value))
        {
            if (!Enum.TryParse<TextVerticalAlignment>(vAlignParam.Value, out _))
                return (false, "VerticalAlignment 参数值无效");
        }

        return (true, string.Empty);
    }

    #endregion
}
