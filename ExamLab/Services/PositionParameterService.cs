using System;
using System.Collections.Generic;
using System.Globalization;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// 位置参数服务类
/// </summary>
public static class PositionParameterService
{
    #region 坐标转换

    /// <summary>
    /// 将坐标从一个单位转换为另一个单位
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="fromSystem">源坐标系统</param>
    /// <param name="toSystem">目标坐标系统</param>
    /// <returns>转换后的值</returns>
    public static double ConvertCoordinate(double value, CoordinateSystem fromSystem, CoordinateSystem toSystem)
    {
        if (fromSystem == toSystem)
            return value;

        // 先转换为磅（Points），然后转换为目标单位
        double points = fromSystem switch
        {
            CoordinateSystem.Points => value,
            CoordinateSystem.Centimeters => value * 28.35, // 1 cm = 28.35 points
            CoordinateSystem.Inches => value * 72.0,       // 1 inch = 72 points
            CoordinateSystem.Pixels => value * 0.75,       // 1 pixel = 0.75 points (96 DPI)
            _ => value
        };

        return toSystem switch
        {
            CoordinateSystem.Points => points,
            CoordinateSystem.Centimeters => points / 28.35,
            CoordinateSystem.Inches => points / 72.0,
            CoordinateSystem.Pixels => points / 0.75,
            _ => points
        };
    }

    /// <summary>
    /// 获取坐标系统的单位名称
    /// </summary>
    /// <param name="system">坐标系统</param>
    /// <returns>单位名称</returns>
    public static string GetUnitName(CoordinateSystem system)
    {
        return system switch
        {
            CoordinateSystem.Points => "磅",
            CoordinateSystem.Centimeters => "厘米",
            CoordinateSystem.Inches => "英寸",
            CoordinateSystem.Pixels => "像素",
            _ => "未知"
        };
    }

    #endregion

    #region 位置参数验证

    /// <summary>
    /// 验证位置参数的有效性
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string ErrorMessage) ValidatePosition(PositionParameter? position)
    {
        if (position == null)
            return (false, "位置参数不能为空");

        switch (position.Type)
        {
            case PositionType.Absolute:
                return ValidateAbsolutePosition(position);
            case PositionType.Relative:
                return ValidateRelativePosition(position);
            case PositionType.Alignment:
                return ValidateAlignmentPosition(position);
            default:
                return (false, "未知的位置类型");
        }
    }

    /// <summary>
    /// 验证绝对位置参数
    /// </summary>
    private static (bool IsValid, string ErrorMessage) ValidateAbsolutePosition(PositionParameter position)
    {
        if (!position.X.HasValue || !position.Y.HasValue)
            return (false, "绝对位置必须指定X和Y坐标");

        if (position.X.Value < 0 || position.Y.Value < 0)
            return (false, "坐标值不能为负数");

        return (true, string.Empty);
    }

    /// <summary>
    /// 验证相对位置参数
    /// </summary>
    private static (bool IsValid, string ErrorMessage) ValidateRelativePosition(PositionParameter position)
    {
        if (!position.RelativeRef.HasValue)
            return (false, "相对位置必须指定参考点");

        if (!position.X.HasValue || !position.Y.HasValue)
            return (false, "相对位置必须指定X和Y坐标");

        return (true, string.Empty);
    }

    /// <summary>
    /// 验证对齐位置参数
    /// </summary>
    private static (bool IsValid, string ErrorMessage) ValidateAlignmentPosition(PositionParameter position)
    {
        if (!position.HorizontalAlign.HasValue || !position.VerticalAlign.HasValue)
            return (false, "对齐位置必须指定水平和垂直对齐方式");

        return (true, string.Empty);
    }

    #endregion

    #region 字符串转换

    /// <summary>
    /// 将位置参数转换为简化的字符串表示（用于向后兼容）
    /// </summary>
    /// <param name="position">位置参数</param>
    /// <returns>字符串表示</returns>
    public static Dictionary<string, string> ToLegacyParameters(PositionParameter? position)
    {
        Dictionary<string, string> parameters = new();

        if (position == null)
            return parameters;

        switch (position.Type)
        {
            case PositionType.Absolute:
                if (position.X.HasValue)
                    parameters["PositionX"] = position.X.Value.ToString(CultureInfo.InvariantCulture);
                if (position.Y.HasValue)
                    parameters["PositionY"] = position.Y.Value.ToString(CultureInfo.InvariantCulture);
                break;

            case PositionType.Relative:
                if (position.X.HasValue)
                    parameters["PositionX"] = position.X.Value.ToString(CultureInfo.InvariantCulture);
                if (position.Y.HasValue)
                    parameters["PositionY"] = position.Y.Value.ToString(CultureInfo.InvariantCulture);
                if (position.RelativeRef.HasValue)
                    parameters["RelativeReference"] = GetRelativeReferenceString(position.RelativeRef.Value);
                break;

            case PositionType.Alignment:
                if (position.HorizontalAlign.HasValue)
                    parameters["HorizontalAlignment"] = GetHorizontalAlignmentString(position.HorizontalAlign.Value);
                if (position.VerticalAlign.HasValue)
                    parameters["VerticalAlignment"] = GetVerticalAlignmentString(position.VerticalAlign.Value);
                break;
        }

        return parameters;
    }

    /// <summary>
    /// 从传统参数创建位置参数
    /// </summary>
    /// <param name="parameters">传统参数字典</param>
    /// <returns>位置参数</returns>
    public static PositionParameter? FromLegacyParameters(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;

        // 检查是否有坐标参数
        bool hasCoordinates = parameters.ContainsKey("PositionX") && parameters.ContainsKey("PositionY");
        bool hasAlignment = parameters.ContainsKey("HorizontalAlignment") || parameters.ContainsKey("VerticalAlignment");
        bool hasRelativeRef = parameters.ContainsKey("RelativeReference");

        if (hasAlignment && !hasCoordinates)
        {
            // 对齐位置
            return CreateAlignmentFromLegacy(parameters);
        }
        else if (hasCoordinates && hasRelativeRef)
        {
            // 相对位置
            return CreateRelativeFromLegacy(parameters);
        }
        else if (hasCoordinates)
        {
            // 绝对位置
            return CreateAbsoluteFromLegacy(parameters);
        }

        return null;
    }

    /// <summary>
    /// 从传统参数创建绝对位置
    /// </summary>
    private static PositionParameter? CreateAbsoluteFromLegacy(Dictionary<string, string> parameters)
    {
        if (!double.TryParse(parameters.GetValueOrDefault("PositionX"), out double x) ||
            !double.TryParse(parameters.GetValueOrDefault("PositionY"), out double y))
            return null;

        return PositionParameter.CreateAbsolute(x, y);
    }

    /// <summary>
    /// 从传统参数创建相对位置
    /// </summary>
    private static PositionParameter? CreateRelativeFromLegacy(Dictionary<string, string> parameters)
    {
        if (!double.TryParse(parameters.GetValueOrDefault("PositionX"), out double x) ||
            !double.TryParse(parameters.GetValueOrDefault("PositionY"), out double y))
            return null;

        RelativeReference reference = ParseRelativeReference(parameters.GetValueOrDefault("RelativeReference", "页面"));
        return PositionParameter.CreateRelative(reference, x, y);
    }

    /// <summary>
    /// 从传统参数创建对齐位置
    /// </summary>
    private static PositionParameter? CreateAlignmentFromLegacy(Dictionary<string, string> parameters)
    {
        HorizontalAlignment horizontal = ParseHorizontalAlignment(parameters.GetValueOrDefault("HorizontalAlignment", "左对齐"));
        VerticalAlignment vertical = ParseVerticalAlignment(parameters.GetValueOrDefault("VerticalAlignment", "顶端对齐"));

        return PositionParameter.CreateAlignment(horizontal, vertical);
    }

    #endregion

    #region 枚举转换辅助方法

    /// <summary>
    /// 获取相对参考点的字符串表示
    /// </summary>
    private static string GetRelativeReferenceString(RelativeReference reference)
    {
        return reference switch
        {
            RelativeReference.Page => "页面",
            RelativeReference.Margin => "页边距",
            RelativeReference.Paragraph => "段落",
            RelativeReference.Column => "列",
            RelativeReference.Character => "字符",
            _ => "页面"
        };
    }

    /// <summary>
    /// 获取水平对齐的字符串表示
    /// </summary>
    private static string GetHorizontalAlignmentString(HorizontalAlignment alignment)
    {
        return alignment switch
        {
            HorizontalAlignment.Left => "左对齐",
            HorizontalAlignment.Center => "居中对齐",
            HorizontalAlignment.Right => "右对齐",
            HorizontalAlignment.Justify => "两端对齐",
            HorizontalAlignment.Distribute => "分散对齐",
            _ => "左对齐"
        };
    }

    /// <summary>
    /// 获取垂直对齐的字符串表示
    /// </summary>
    private static string GetVerticalAlignmentString(VerticalAlignment alignment)
    {
        return alignment switch
        {
            VerticalAlignment.Top => "顶端对齐",
            VerticalAlignment.Middle => "居中对齐",
            VerticalAlignment.Bottom => "底端对齐",
            VerticalAlignment.Baseline => "基线对齐",
            _ => "顶端对齐"
        };
    }

    /// <summary>
    /// 解析相对参考点
    /// </summary>
    private static RelativeReference ParseRelativeReference(string value)
    {
        return value switch
        {
            "页面" => RelativeReference.Page,
            "页边距" => RelativeReference.Margin,
            "段落" => RelativeReference.Paragraph,
            "列" => RelativeReference.Column,
            "字符" => RelativeReference.Character,
            _ => RelativeReference.Page
        };
    }

    /// <summary>
    /// 解析水平对齐
    /// </summary>
    private static HorizontalAlignment ParseHorizontalAlignment(string value)
    {
        return value switch
        {
            "左对齐" => HorizontalAlignment.Left,
            "居中对齐" => HorizontalAlignment.Center,
            "右对齐" => HorizontalAlignment.Right,
            "两端对齐" => HorizontalAlignment.Justify,
            "分散对齐" => HorizontalAlignment.Distribute,
            _ => HorizontalAlignment.Left
        };
    }

    /// <summary>
    /// 解析垂直对齐
    /// </summary>
    private static VerticalAlignment ParseVerticalAlignment(string value)
    {
        return value switch
        {
            "顶端对齐" => VerticalAlignment.Top,
            "居中对齐" => VerticalAlignment.Middle,
            "底端对齐" => VerticalAlignment.Bottom,
            "基线对齐" => VerticalAlignment.Baseline,
            _ => VerticalAlignment.Top
        };
    }

    #endregion
}
