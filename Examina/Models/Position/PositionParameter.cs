using System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.Models.Position;

/// <summary>
/// 位置参数类型枚举
/// </summary>
public enum PositionType
{
    /// <summary>
    /// 绝对位置（基于坐标系统）
    /// </summary>
    Absolute,
    
    /// <summary>
    /// 相对位置（基于父容器）
    /// </summary>
    Relative,
    
    /// <summary>
    /// 对齐位置（基于对齐方式）
    /// </summary>
    Alignment
}

/// <summary>
/// 水平对齐方式枚举
/// </summary>
public enum HorizontalAlignment
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
    Justify,
    
    /// <summary>
    /// 均匀分布
    /// </summary>
    Distribute
}

/// <summary>
/// 垂直对齐方式枚举
/// </summary>
public enum VerticalAlignment
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
/// 位置单位枚举
/// </summary>
public enum PositionUnit
{
    /// <summary>
    /// 像素
    /// </summary>
    Pixel,
    
    /// <summary>
    /// 磅（Point）
    /// </summary>
    Point,
    
    /// <summary>
    /// 厘米
    /// </summary>
    Centimeter,
    
    /// <summary>
    /// 英寸
    /// </summary>
    Inch,
    
    /// <summary>
    /// 百分比
    /// </summary>
    Percentage
}

/// <summary>
/// 位置参数模型
/// </summary>
public class PositionParameter : ReactiveObject
{
    /// <summary>
    /// 位置参数ID
    /// </summary>
    [Reactive] public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 位置类型
    /// </summary>
    [Reactive] public PositionType Type { get; set; } = PositionType.Absolute;
    
    /// <summary>
    /// X坐标（水平位置）
    /// </summary>
    [Reactive] public double X { get; set; } = 0;
    
    /// <summary>
    /// Y坐标（垂直位置）
    /// </summary>
    [Reactive] public double Y { get; set; } = 0;
    
    /// <summary>
    /// 位置单位
    /// </summary>
    [Reactive] public PositionUnit Unit { get; set; } = PositionUnit.Point;
    
    /// <summary>
    /// 水平对齐方式（当Type为Alignment时使用）
    /// </summary>
    [Reactive] public HorizontalAlignment? HorizontalAlign { get; set; }
    
    /// <summary>
    /// 垂直对齐方式（当Type为Alignment时使用）
    /// </summary>
    [Reactive] public VerticalAlignment? VerticalAlign { get; set; }
    
    /// <summary>
    /// 相对于的父元素ID（当Type为Relative时使用）
    /// </summary>
    [Reactive] public string? RelativeToElementId { get; set; }
    
    /// <summary>
    /// 是否锁定宽高比
    /// </summary>
    [Reactive] public bool LockAspectRatio { get; set; } = false;
    
    /// <summary>
    /// 最小X坐标限制
    /// </summary>
    [Reactive] public double? MinX { get; set; }
    
    /// <summary>
    /// 最大X坐标限制
    /// </summary>
    [Reactive] public double? MaxX { get; set; }
    
    /// <summary>
    /// 最小Y坐标限制
    /// </summary>
    [Reactive] public double? MinY { get; set; }
    
    /// <summary>
    /// 最大Y坐标限制
    /// </summary>
    [Reactive] public double? MaxY { get; set; }
    
    /// <summary>
    /// 位置描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用网格对齐
    /// </summary>
    [Reactive] public bool EnableGridSnap { get; set; } = false;
    
    /// <summary>
    /// 网格大小（当EnableGridSnap为true时使用）
    /// </summary>
    [Reactive] public double GridSize { get; set; } = 10;
    
    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>位置的字符串表示</returns>
    public override string ToString()
    {
        return Type switch
        {
            PositionType.Absolute => $"绝对位置: ({X}, {Y}) {Unit}",
            PositionType.Relative => $"相对位置: ({X}, {Y}) {Unit} (相对于 {RelativeToElementId})",
            PositionType.Alignment => $"对齐位置: {HorizontalAlign} - {VerticalAlign}",
            _ => $"位置: ({X}, {Y})"
        };
    }
    
    /// <summary>
    /// 验证位置参数是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public bool IsValid()
    {
        // 检查坐标范围
        if (MinX.HasValue && X < MinX.Value) return false;
        if (MaxX.HasValue && X > MaxX.Value) return false;
        if (MinY.HasValue && Y < MinY.Value) return false;
        if (MaxY.HasValue && Y > MaxY.Value) return false;
        
        // 检查相对位置的父元素ID
        if (Type == PositionType.Relative && string.IsNullOrEmpty(RelativeToElementId))
            return false;
        
        // 检查对齐位置的对齐方式
        if (Type == PositionType.Alignment && (!HorizontalAlign.HasValue || !VerticalAlign.HasValue))
            return false;
        
        return true;
    }
    
    /// <summary>
    /// 克隆位置参数
    /// </summary>
    /// <returns>克隆的位置参数</returns>
    public PositionParameter Clone()
    {
        return new PositionParameter
        {
            Id = Guid.NewGuid().ToString(),
            Type = Type,
            X = X,
            Y = Y,
            Unit = Unit,
            HorizontalAlign = HorizontalAlign,
            VerticalAlign = VerticalAlign,
            RelativeToElementId = RelativeToElementId,
            LockAspectRatio = LockAspectRatio,
            MinX = MinX,
            MaxX = MaxX,
            MinY = MinY,
            MaxY = MaxY,
            Description = Description,
            EnableGridSnap = EnableGridSnap,
            GridSize = GridSize
        };
    }
}
