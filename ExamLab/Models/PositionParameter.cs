using System;
using System.Collections.Generic;
using System.Text.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// 位置类型枚举
/// </summary>
public enum PositionType
{
    /// <summary>
    /// 绝对位置（使用具体坐标）
    /// </summary>
    Absolute,
    
    /// <summary>
    /// 相对位置（相对于页面、段落等）
    /// </summary>
    Relative,
    
    /// <summary>
    /// 对齐位置（左对齐、居中、右对齐等）
    /// </summary>
    Alignment
}

/// <summary>
/// 坐标系统枚举
/// </summary>
public enum CoordinateSystem
{
    /// <summary>
    /// 磅（Points）- Word 默认单位
    /// </summary>
    Points,
    
    /// <summary>
    /// 厘米
    /// </summary>
    Centimeters,
    
    /// <summary>
    /// 英寸
    /// </summary>
    Inches,
    
    /// <summary>
    /// 像素
    /// </summary>
    Pixels
}

/// <summary>
/// 水平对齐方式枚举
/// </summary>
public enum HorizontalAlignment
{
    Left,       // 左对齐
    Center,     // 居中对齐
    Right,      // 右对齐
    Justify,    // 两端对齐
    Distribute  // 分散对齐
}

/// <summary>
/// 垂直对齐方式枚举
/// </summary>
public enum VerticalAlignment
{
    Top,        // 顶端对齐
    Middle,     // 居中对齐
    Bottom,     // 底端对齐
    Baseline    // 基线对齐
}

/// <summary>
/// 相对位置参考点枚举
/// </summary>
public enum RelativeReference
{
    Page,       // 相对于页面
    Margin,     // 相对于页边距
    Paragraph,  // 相对于段落
    Column,     // 相对于列
    Character   // 相对于字符
}

/// <summary>
/// 位置参数模型
/// </summary>
public class PositionParameter : ReactiveObject
{
    /// <summary>
    /// 位置类型
    /// </summary>
    [Reactive] public PositionType Type { get; set; } = PositionType.Absolute;
    
    /// <summary>
    /// 坐标系统
    /// </summary>
    [Reactive] public CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Points;
    
    /// <summary>
    /// X坐标（水平位置）
    /// </summary>
    [Reactive] public double? X { get; set; }
    
    /// <summary>
    /// Y坐标（垂直位置）
    /// </summary>
    [Reactive] public double? Y { get; set; }
    
    /// <summary>
    /// 水平对齐方式
    /// </summary>
    [Reactive] public HorizontalAlignment? HorizontalAlign { get; set; }
    
    /// <summary>
    /// 垂直对齐方式
    /// </summary>
    [Reactive] public VerticalAlignment? VerticalAlign { get; set; }
    
    /// <summary>
    /// 相对位置参考点
    /// </summary>
    [Reactive] public RelativeReference? RelativeRef { get; set; }
    
    /// <summary>
    /// 是否锁定纵横比
    /// </summary>
    [Reactive] public bool LockAspectRatio { get; set; } = false;
    
    /// <summary>
    /// 附加属性（用于存储特定类型的额外信息）
    /// </summary>
    [Reactive] public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    
    /// <summary>
    /// 将位置参数转换为JSON字符串
    /// </summary>
    /// <returns>JSON字符串</returns>
    public string ToJson()
    {
        try
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch
        {
            return "{}";
        }
    }
    
    /// <summary>
    /// 从JSON字符串创建位置参数
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>位置参数对象</returns>
    public static PositionParameter? FromJson(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
                return null;
                
            return JsonSerializer.Deserialize<PositionParameter>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 创建绝对位置参数
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="coordinateSystem">坐标系统</param>
    /// <returns>位置参数对象</returns>
    public static PositionParameter CreateAbsolute(double x, double y, CoordinateSystem coordinateSystem = CoordinateSystem.Points)
    {
        return new PositionParameter
        {
            Type = PositionType.Absolute,
            CoordinateSystem = coordinateSystem,
            X = x,
            Y = y
        };
    }
    
    /// <summary>
    /// 创建对齐位置参数
    /// </summary>
    /// <param name="horizontal">水平对齐</param>
    /// <param name="vertical">垂直对齐</param>
    /// <returns>位置参数对象</returns>
    public static PositionParameter CreateAlignment(HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        return new PositionParameter
        {
            Type = PositionType.Alignment,
            HorizontalAlign = horizontal,
            VerticalAlign = vertical
        };
    }
    
    /// <summary>
    /// 创建相对位置参数
    /// </summary>
    /// <param name="reference">参考点</param>
    /// <param name="x">相对X坐标</param>
    /// <param name="y">相对Y坐标</param>
    /// <param name="coordinateSystem">坐标系统</param>
    /// <returns>位置参数对象</returns>
    public static PositionParameter CreateRelative(RelativeReference reference, double x, double y, CoordinateSystem coordinateSystem = CoordinateSystem.Points)
    {
        return new PositionParameter
        {
            Type = PositionType.Relative,
            RelativeRef = reference,
            CoordinateSystem = coordinateSystem,
            X = x,
            Y = y
        };
    }
    
    /// <summary>
    /// 获取位置描述字符串
    /// </summary>
    /// <returns>位置描述</returns>
    public string GetDescription()
    {
        return Type switch
        {
            PositionType.Absolute => $"绝对位置: ({X}, {Y}) {CoordinateSystem}",
            PositionType.Alignment => $"对齐: {HorizontalAlign} + {VerticalAlign}",
            PositionType.Relative => $"相对位置: 相对于{RelativeRef} ({X}, {Y}) {CoordinateSystem}",
            _ => "未知位置类型"
        };
    }
}
