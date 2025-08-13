using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExaminaWebApplication.Models.Excel;

/// <summary>
/// Excel参数配置基类
/// </summary>
public abstract class ExcelParameterConfigurationBase
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 验证参数值是否有效
    /// </summary>
    /// <returns></returns>
    public abstract bool IsValid();

    /// <summary>
    /// 获取参数值的字符串表示
    /// </summary>
    /// <returns></returns>
    public virtual string GetValueString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// 字符串参数配置
/// </summary>
public class StringParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 最小长度
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 正则表达式验证
    /// </summary>
    public string? RegexPattern { get; set; }

    public override bool IsValid()
    {
        if (IsRequired && (Value == null || string.IsNullOrEmpty(Value.ToString())))
            return false;

        if (Value != null)
        {
            string stringValue = Value.ToString()!;
            
            if (MinLength.HasValue && stringValue.Length < MinLength.Value)
                return false;
                
            if (MaxLength.HasValue && stringValue.Length > MaxLength.Value)
                return false;

            if (!string.IsNullOrEmpty(RegexPattern))
            {
                return System.Text.RegularExpressions.Regex.IsMatch(stringValue, RegexPattern);
            }
        }

        return true;
    }
}

/// <summary>
/// 整数参数配置
/// </summary>
public class IntegerParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 最小值
    /// </summary>
    public int? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public int? MaxValue { get; set; }

    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            if (!int.TryParse(Value.ToString(), out int intValue))
                return false;

            if (MinValue.HasValue && intValue < MinValue.Value)
            {
                // 如果值为-1，则允许（-1代表通配符，匹配任意值）
                if (intValue != -1)
                    return false;
            }

            if (MaxValue.HasValue && intValue > MaxValue.Value)
                return false;
        }

        return true;
    }
}

/// <summary>
/// 小数参数配置
/// </summary>
public class DecimalParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 最小值
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalPlaces { get; set; }

    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            if (!decimal.TryParse(Value.ToString(), out decimal decimalValue))
                return false;

            if (MinValue.HasValue && decimalValue < MinValue.Value)
            {
                // 如果值为-1，则允许（-1代表通配符，匹配任意值）
                if (Math.Abs(decimalValue - (-1)) >= 0.001m)
                    return false;
            }

            if (MaxValue.HasValue && decimalValue > MaxValue.Value)
                return false;
        }

        return true;
    }
}

/// <summary>
/// 布尔参数配置
/// </summary>
public class BooleanParameterConfiguration : ExcelParameterConfigurationBase
{
    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            return bool.TryParse(Value.ToString(), out _);
        }

        return true;
    }
}

/// <summary>
/// 枚举参数配置
/// </summary>
public class EnumParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 枚举类型ID
    /// </summary>
    public int EnumTypeId { get; set; }

    /// <summary>
    /// 允许的枚举值列表
    /// </summary>
    public List<string> AllowedValues { get; set; } = new List<string>();

    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            string stringValue = Value.ToString()!;
            return AllowedValues.Contains(stringValue);
        }

        return true;
    }
}

/// <summary>
/// 单元格范围参数配置
/// </summary>
public class CellRangeParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 是否允许单个单元格
    /// </summary>
    public bool AllowSingleCell { get; set; } = true;

    /// <summary>
    /// 是否允许多个区域
    /// </summary>
    public bool AllowMultipleRanges { get; set; } = false;

    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            string stringValue = Value.ToString()!;
            
            // 简单的单元格范围验证（可以扩展为更复杂的验证）
            if (AllowSingleCell && System.Text.RegularExpressions.Regex.IsMatch(stringValue, @"^[A-Z]+\d+$"))
                return true;

            if (System.Text.RegularExpressions.Regex.IsMatch(stringValue, @"^[A-Z]+\d+:[A-Z]+\d+$"))
                return true;

            if (AllowMultipleRanges && stringValue.Contains(","))
            {
                string[] ranges = stringValue.Split(',');
                return ranges.All(range => 
                    System.Text.RegularExpressions.Regex.IsMatch(range.Trim(), @"^[A-Z]+\d+(:[A-Z]+\d+)?$"));
            }
        }

        return true;
    }
}

/// <summary>
/// 颜色参数配置
/// </summary>
public class ColorParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 颜色格式（默认为HEX十六进制格式）
    /// </summary>
    public string ColorFormat { get; set; } = "HEX";

    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            string stringValue = Value.ToString()!;

            // 统一使用十六进制格式验证 #RRGGBB 或 #RGB
            return IsValidHexColor(stringValue);
        }

        return true;
    }

    /// <summary>
    /// 验证十六进制颜色格式
    /// </summary>
    /// <param name="color">颜色值</param>
    /// <returns>是否有效</returns>
    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        // 支持 #RRGGBB 和 #RGB 格式
        return System.Text.RegularExpressions.Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}

/// <summary>
/// 公式参数配置
/// </summary>
public class FormulaParameterConfiguration : ExcelParameterConfigurationBase
{
    /// <summary>
    /// 允许的函数列表
    /// </summary>
    public List<string> AllowedFunctions { get; set; } = new List<string>();

    public override bool IsValid()
    {
        if (IsRequired && Value == null)
            return false;

        if (Value != null)
        {
            string stringValue = Value.ToString()!;
            
            // 简单的公式验证
            if (!stringValue.StartsWith("="))
                return false;

            if (AllowedFunctions.Count != 0)
            {
                return AllowedFunctions.Any(func => 
                    stringValue.ToUpper().Contains(func.ToUpper()));
            }
        }

        return true;
    }
}
