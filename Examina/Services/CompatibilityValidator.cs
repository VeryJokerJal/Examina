using System;
using System.Collections.Generic;
using System.Linq;
using Examina.Models.Position;

namespace Examina.Services;

/// <summary>
/// 兼容性验证器
/// </summary>
public class CompatibilityValidator
{
    /// <summary>
    /// 验证位置参数与现有功能的兼容性
    /// </summary>
    /// <param name="parameters">位置参数列表</param>
    /// <returns>兼容性验证结果</returns>
    public CompatibilityValidationResult ValidatePositionParametersCompatibility(IEnumerable<ConfigurationParameter> parameters)
    {
        var result = new CompatibilityValidationResult();
        var paramList = parameters.ToList();
        
        try
        {
            // 检查参数名称冲突
            ValidateParameterNameConflicts(paramList, result);
            
            // 检查枚举选项格式兼容性
            ValidateEnumOptionsCompatibility(paramList, result);
            
            // 检查数值参数范围兼容性
            ValidateNumericParameterCompatibility(paramList, result);
            
            // 检查位置参数特有的兼容性
            ValidatePositionSpecificCompatibility(paramList, result);
            
            result.IsCompatible = result.Issues.Count == 0;
        }
        catch (Exception ex)
        {
            result.IsCompatible = false;
            result.Issues.Add(new CompatibilityIssue
            {
                Severity = IssueSeverity.Error,
                Category = "验证异常",
                Message = $"兼容性验证过程中发生异常: {ex.Message}",
                Suggestion = "请检查参数格式是否正确"
            });
        }
        
        return result;
    }
    
    /// <summary>
    /// 验证参数名称冲突
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="result">验证结果</param>
    private static void ValidateParameterNameConflicts(List<ConfigurationParameter> parameters, CompatibilityValidationResult result)
    {
        var duplicateNames = parameters
            .GroupBy(p => p.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var duplicateName in duplicateNames)
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Severity = IssueSeverity.Error,
                Category = "参数名称冲突",
                Message = $"参数名称 '{duplicateName}' 重复",
                Suggestion = "请确保每个参数名称唯一"
            });
        }
    }
    
    /// <summary>
    /// 验证枚举选项格式兼容性
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="result">验证结果</param>
    private static void ValidateEnumOptionsCompatibility(List<ConfigurationParameter> parameters, CompatibilityValidationResult result)
    {
        var enumParameters = parameters.Where(p => p.Type == ParameterType.Enum && !string.IsNullOrEmpty(p.EnumOptions));
        
        foreach (var param in enumParameters)
        {
            try
            {
                // 测试枚举选项解析
                var options = param.EnumOptionsList;
                
                // 检查是否包含位置相关的特殊格式
                if (ContainsPositionRelatedOptions(param.EnumOptions!))
                {
                    ValidatePositionEnumOptions(param, result);
                }
                
                // 检查选项是否为空
                if (options.Count == 0)
                {
                    result.Issues.Add(new CompatibilityIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = "枚举选项",
                        Message = $"参数 '{param.Name}' 的枚举选项为空",
                        Suggestion = "请提供有效的枚举选项"
                    });
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Severity = IssueSeverity.Error,
                    Category = "枚举选项解析",
                    Message = $"参数 '{param.Name}' 的枚举选项解析失败: {ex.Message}",
                    Suggestion = "请检查枚举选项格式是否正确"
                });
            }
        }
    }
    
    /// <summary>
    /// 验证数值参数兼容性
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="result">验证结果</param>
    private static void ValidateNumericParameterCompatibility(List<ConfigurationParameter> parameters, CompatibilityValidationResult result)
    {
        var numericParameters = parameters.Where(p => p.Type == ParameterType.Number);
        
        foreach (var param in numericParameters)
        {
            // 检查默认值
            if (!string.IsNullOrEmpty(param.DefaultValue) && !double.TryParse(param.DefaultValue, out _))
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Severity = IssueSeverity.Error,
                    Category = "数值参数",
                    Message = $"参数 '{param.Name}' 的默认值 '{param.DefaultValue}' 不是有效数值",
                    Suggestion = "请提供有效的数值默认值"
                });
            }
            
            // 检查当前值
            if (!string.IsNullOrEmpty(param.Value) && !double.TryParse(param.Value, out _))
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Severity = IssueSeverity.Error,
                    Category = "数值参数",
                    Message = $"参数 '{param.Name}' 的当前值 '{param.Value}' 不是有效数值",
                    Suggestion = "请提供有效的数值"
                });
            }
            
            // 检查范围
            if (param.MinValue.HasValue && param.MaxValue.HasValue && param.MinValue.Value > param.MaxValue.Value)
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Severity = IssueSeverity.Error,
                    Category = "数值范围",
                    Message = $"参数 '{param.Name}' 的最小值大于最大值",
                    Suggestion = "请确保最小值不大于最大值"
                });
            }
        }
    }
    
    /// <summary>
    /// 验证位置参数特有的兼容性
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="result">验证结果</param>
    private static void ValidatePositionSpecificCompatibility(List<ConfigurationParameter> parameters, CompatibilityValidationResult result)
    {
        var positionParameters = parameters.Where(p => p.Type == ParameterType.Position);
        
        foreach (var param in positionParameters)
        {
            if (param.PositionValue != null && !param.PositionValue.IsValid())
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Severity = IssueSeverity.Error,
                    Category = "位置参数",
                    Message = $"参数 '{param.Name}' 的位置值无效",
                    Suggestion = "请检查位置参数的坐标和单位设置"
                });
            }
        }
        
        // 检查位置参数的组合有效性
        var positionX = parameters.FirstOrDefault(p => p.Name == "PositionX");
        var positionY = parameters.FirstOrDefault(p => p.Name == "PositionY");
        
        if (positionX != null && positionY == null)
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "位置参数组合",
                Message = "只设置了 PositionX 而没有 PositionY",
                Suggestion = "建议同时设置 X 和 Y 坐标"
            });
        }
        
        if (positionY != null && positionX == null)
        {
            result.Issues.Add(new CompatibilityIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "位置参数组合",
                Message = "只设置了 PositionY 而没有 PositionX",
                Suggestion = "建议同时设置 X 和 Y 坐标"
            });
        }
    }
    
    /// <summary>
    /// 检查是否包含位置相关选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>是否包含位置相关选项</returns>
    private static bool ContainsPositionRelatedOptions(string enumOptions)
    {
        return enumOptions.Contains("左对齐") ||
               enumOptions.Contains("居中") ||
               enumOptions.Contains("右对齐") ||
               enumOptions.Contains("顶端") ||
               enumOptions.Contains("底端") ||
               enumOptions.Contains("页面") ||
               enumOptions.Contains("段落");
    }
    
    /// <summary>
    /// 验证位置枚举选项
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <param name="result">验证结果</param>
    private static void ValidatePositionEnumOptions(ConfigurationParameter parameter, CompatibilityValidationResult result)
    {
        var options = parameter.EnumOptionsList;
        
        // 检查位置选项的完整性
        if (parameter.Name.Contains("Alignment") || parameter.Name.Contains("Position"))
        {
            if (options.Count == 0)
            {
                result.Issues.Add(new CompatibilityIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = "位置枚举选项",
                    Message = $"位置参数 '{parameter.Name}' 缺少对齐选项",
                    Suggestion = "请提供完整的对齐选项"
                });
            }
        }
    }
}

/// <summary>
/// 兼容性验证结果
/// </summary>
public class CompatibilityValidationResult
{
    /// <summary>
    /// 是否兼容
    /// </summary>
    public bool IsCompatible { get; set; } = true;
    
    /// <summary>
    /// 兼容性问题列表
    /// </summary>
    public List<CompatibilityIssue> Issues { get; set; } = [];
    
    /// <summary>
    /// 获取错误数量
    /// </summary>
    public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);
    
    /// <summary>
    /// 获取警告数量
    /// </summary>
    public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
    
    /// <summary>
    /// 获取信息数量
    /// </summary>
    public int InfoCount => Issues.Count(i => i.Severity == IssueSeverity.Info);
}

/// <summary>
/// 兼容性问题
/// </summary>
public class CompatibilityIssue
{
    /// <summary>
    /// 问题严重程度
    /// </summary>
    public IssueSeverity Severity { get; set; }
    
    /// <summary>
    /// 问题类别
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 问题描述
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 解决建议
    /// </summary>
    public string Suggestion { get; set; } = string.Empty;
}

/// <summary>
/// 问题严重程度枚举
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误
    /// </summary>
    Error
}
