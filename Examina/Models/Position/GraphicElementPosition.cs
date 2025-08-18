using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.Models.Position;

/// <summary>
/// 图形元素类型枚举
/// </summary>
public enum GraphicElementType
{
    /// <summary>
    /// 形状
    /// </summary>
    Shape,
    
    /// <summary>
    /// 图表
    /// </summary>
    Chart,
    
    /// <summary>
    /// 图像
    /// </summary>
    Image,
    
    /// <summary>
    /// 文本框
    /// </summary>
    TextBox,
    
    /// <summary>
    /// 艺术字
    /// </summary>
    WordArt,
    
    /// <summary>
    /// 表格
    /// </summary>
    Table,
    
    /// <summary>
    /// 智能图形
    /// </summary>
    SmartArt
}

/// <summary>
/// 图形元素位置模型
/// </summary>
public class GraphicElementPosition : ReactiveObject
{
    /// <summary>
    /// 元素ID
    /// </summary>
    [Reactive] public string ElementId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 元素名称
    /// </summary>
    [Reactive] public string ElementName { get; set; } = string.Empty;
    
    /// <summary>
    /// 图形元素类型
    /// </summary>
    [Reactive] public GraphicElementType ElementType { get; set; } = GraphicElementType.Shape;
    
    /// <summary>
    /// 位置参数
    /// </summary>
    [Reactive] public PositionParameter Position { get; set; } = new();
    
    /// <summary>
    /// 元素宽度
    /// </summary>
    [Reactive] public double Width { get; set; } = 100;
    
    /// <summary>
    /// 元素高度
    /// </summary>
    [Reactive] public double Height { get; set; } = 100;
    
    /// <summary>
    /// 尺寸单位
    /// </summary>
    [Reactive] public PositionUnit SizeUnit { get; set; } = PositionUnit.Point;
    
    /// <summary>
    /// 旋转角度（度）
    /// </summary>
    [Reactive] public double RotationAngle { get; set; } = 0;
    
    /// <summary>
    /// Z轴顺序（层级）
    /// </summary>
    [Reactive] public int ZOrder { get; set; } = 0;
    
    /// <summary>
    /// 是否可见
    /// </summary>
    [Reactive] public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// 是否锁定位置
    /// </summary>
    [Reactive] public bool IsPositionLocked { get; set; } = false;
    
    /// <summary>
    /// 是否锁定尺寸
    /// </summary>
    [Reactive] public bool IsSizeLocked { get; set; } = false;
    
    /// <summary>
    /// 父容器ID（如果有的话）
    /// </summary>
    [Reactive] public string? ParentContainerId { get; set; }
    
    /// <summary>
    /// 边距设置
    /// </summary>
    [Reactive] public MarginSettings Margin { get; set; } = new();
    
    /// <summary>
    /// 内边距设置
    /// </summary>
    [Reactive] public PaddingSettings Padding { get; set; } = new();
    
    /// <summary>
    /// 环绕方式（用于Word文档中的图形）
    /// </summary>
    [Reactive] public WrapStyle? WrapStyle { get; set; }
    
    /// <summary>
    /// 锚点设置（用于Word文档中的图形）
    /// </summary>
    [Reactive] public AnchorSettings? Anchor { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    [Reactive] public DateTime LastModified { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 获取元素的边界矩形
    /// </summary>
    /// <returns>边界矩形</returns>
    public (double Left, double Top, double Right, double Bottom) GetBounds()
    {
        double left = Position.X;
        double top = Position.Y;
        double right = left + Width;
        double bottom = top + Height;
        
        return (left, top, right, bottom);
    }
    
    /// <summary>
    /// 检查是否与另一个元素重叠
    /// </summary>
    /// <param name="other">另一个图形元素</param>
    /// <returns>是否重叠</returns>
    public bool IsOverlapping(GraphicElementPosition other)
    {
        var (left1, top1, right1, bottom1) = GetBounds();
        var (left2, top2, right2, bottom2) = other.GetBounds();
        
        return !(right1 <= left2 || left1 >= right2 || bottom1 <= top2 || top1 >= bottom2);
    }
    
    /// <summary>
    /// 移动元素到指定位置
    /// </summary>
    /// <param name="newX">新的X坐标</param>
    /// <param name="newY">新的Y坐标</param>
    public void MoveTo(double newX, double newY)
    {
        if (IsPositionLocked) return;
        
        Position.X = newX;
        Position.Y = newY;
        LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 调整元素尺寸
    /// </summary>
    /// <param name="newWidth">新宽度</param>
    /// <param name="newHeight">新高度</param>
    public void Resize(double newWidth, double newHeight)
    {
        if (IsSizeLocked) return;
        
        if (Position.LockAspectRatio)
        {
            double aspectRatio = Width / Height;
            if (Math.Abs(newWidth / newHeight - aspectRatio) > 0.01)
            {
                // 保持宽高比，以较小的缩放比例为准
                double scaleX = newWidth / Width;
                double scaleY = newHeight / Height;
                double scale = Math.Min(scaleX, scaleY);
                
                Width *= scale;
                Height *= scale;
            }
            else
            {
                Width = newWidth;
                Height = newHeight;
            }
        }
        else
        {
            Width = newWidth;
            Height = newHeight;
        }
        
        LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 验证元素位置设置是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public bool IsValid()
    {
        // 验证基本属性
        if (Width <= 0 || Height <= 0) return false;
        if (!Position.IsValid()) return false;
        
        // 验证旋转角度
        if (RotationAngle < -360 || RotationAngle > 360) return false;
        
        return true;
    }
    
    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>元素位置的字符串表示</returns>
    public override string ToString()
    {
        return $"{ElementType} '{ElementName}': {Position} - 尺寸: {Width}x{Height} {SizeUnit}";
    }
}

/// <summary>
/// 边距设置
/// </summary>
public class MarginSettings : ReactiveObject
{
    [Reactive] public double Left { get; set; } = 0;
    [Reactive] public double Top { get; set; } = 0;
    [Reactive] public double Right { get; set; } = 0;
    [Reactive] public double Bottom { get; set; } = 0;
    [Reactive] public PositionUnit Unit { get; set; } = PositionUnit.Point;
}

/// <summary>
/// 内边距设置
/// </summary>
public class PaddingSettings : ReactiveObject
{
    [Reactive] public double Left { get; set; } = 0;
    [Reactive] public double Top { get; set; } = 0;
    [Reactive] public double Right { get; set; } = 0;
    [Reactive] public double Bottom { get; set; } = 0;
    [Reactive] public PositionUnit Unit { get; set; } = PositionUnit.Point;
}

/// <summary>
/// 环绕方式枚举
/// </summary>
public enum WrapStyle
{
    /// <summary>
    /// 嵌入型
    /// </summary>
    Inline,
    
    /// <summary>
    /// 四周型
    /// </summary>
    Square,
    
    /// <summary>
    /// 紧密型
    /// </summary>
    Tight,
    
    /// <summary>
    /// 穿越型
    /// </summary>
    Through,
    
    /// <summary>
    /// 上下型
    /// </summary>
    TopAndBottom,
    
    /// <summary>
    /// 衬于文字下方
    /// </summary>
    Behind,
    
    /// <summary>
    /// 浮于文字上方
    /// </summary>
    InFront
}

/// <summary>
/// 锚点设置
/// </summary>
public class AnchorSettings : ReactiveObject
{
    /// <summary>
    /// 锚点类型
    /// </summary>
    [Reactive] public AnchorType Type { get; set; } = AnchorType.Paragraph;
    
    /// <summary>
    /// 锚点位置描述
    /// </summary>
    [Reactive] public string Position { get; set; } = string.Empty;
}

/// <summary>
/// 锚点类型枚举
/// </summary>
public enum AnchorType
{
    /// <summary>
    /// 段落
    /// </summary>
    Paragraph,
    
    /// <summary>
    /// 页面
    /// </summary>
    Page,
    
    /// <summary>
    /// 页边距
    /// </summary>
    Margin,
    
    /// <summary>
    /// 字符
    /// </summary>
    Character
}
