using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.Models.Position;

/// <summary>
/// 操作点类型枚举
/// </summary>
public enum OperationPointType
{
    /// <summary>
    /// 设置图形位置
    /// </summary>
    SetGraphicPosition,
    
    /// <summary>
    /// 设置文本框位置
    /// </summary>
    SetTextBoxPosition,
    
    /// <summary>
    /// 设置图像位置
    /// </summary>
    SetImagePosition,
    
    /// <summary>
    /// 设置元素位置
    /// </summary>
    SetElementPosition,
    
    /// <summary>
    /// 设置形状位置
    /// </summary>
    SetShapePosition,
    
    /// <summary>
    /// 设置图表位置
    /// </summary>
    SetChartPosition
}

/// <summary>
/// 参数类型枚举
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// 数字
    /// </summary>
    Number,
    
    /// <summary>
    /// 文本
    /// </summary>
    Text,
    
    /// <summary>
    /// 枚举
    /// </summary>
    Enum,
    
    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean,
    
    /// <summary>
    /// 颜色
    /// </summary>
    Color,
    
    /// <summary>
    /// 位置
    /// </summary>
    Position
}

/// <summary>
/// 配置参数模型
/// </summary>
public class ConfigurationParameter : ReactiveObject
{
    /// <summary>
    /// 参数ID
    /// </summary>
    [Reactive] public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 参数名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    [Reactive] public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 参数类型
    /// </summary>
    [Reactive] public ParameterType Type { get; set; } = ParameterType.Text;
    
    /// <summary>
    /// 参数值
    /// </summary>
    [Reactive] public string? Value { get; set; }
    
    /// <summary>
    /// 默认值
    /// </summary>
    [Reactive] public string? DefaultValue { get; set; }
    
    /// <summary>
    /// 是否必填
    /// </summary>
    [Reactive] public bool IsRequired { get; set; } = false;
    
    /// <summary>
    /// 参数顺序
    /// </summary>
    [Reactive] public int Order { get; set; } = 0;
    
    /// <summary>
    /// 最小值（用于数字类型）
    /// </summary>
    [Reactive] public double? MinValue { get; set; }
    
    /// <summary>
    /// 最大值（用于数字类型）
    /// </summary>
    [Reactive] public double? MaxValue { get; set; }
    
    /// <summary>
    /// 枚举选项（用于枚举类型）
    /// </summary>
    [Reactive] public string? EnumOptions { get; set; }
    
    /// <summary>
    /// 位置参数（用于位置类型）
    /// </summary>
    [Reactive] public PositionParameter? PositionValue { get; set; }
    
    /// <summary>
    /// 验证规则
    /// </summary>
    [Reactive] public string? ValidationRules { get; set; }
    
    /// <summary>
    /// 是否可见
    /// </summary>
    [Reactive] public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// 获取枚举选项列表
    /// </summary>
    public ObservableCollection<string> EnumOptionsList
    {
        get
        {
            if (string.IsNullOrEmpty(EnumOptions))
                return [];
            
            // 使用与 ExamLab 相同的智能解析逻辑
            var options = ParseEnumOptions(EnumOptions);
            return new ObservableCollection<string>(options);
        }
    }
    
    /// <summary>
    /// 解析枚举选项，特殊处理位置相关的选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>解析后的选项列表</returns>
    private static string[] ParseEnumOptions(string enumOptions)
    {
        if (string.IsNullOrEmpty(enumOptions))
            return [];
        
        // 特殊处理位置相关的选项
        if (IsPositionRelatedOptions(enumOptions))
        {
            return ParsePositionOptions(enumOptions);
        }
        
        // 默认按逗号分割
        return enumOptions.Split(',').Select(s => s.Trim()).ToArray();
    }
    
    /// <summary>
    /// 判断是否为位置相关的选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>是否为位置相关选项</returns>
    private static bool IsPositionRelatedOptions(string enumOptions)
    {
        // 检查是否包含位置相关的特征模式
        return enumOptions.Contains("左对齐") ||
               enumOptions.Contains("居中") ||
               enumOptions.Contains("右对齐") ||
               enumOptions.Contains("顶端") ||
               enumOptions.Contains("底端") ||
               enumOptions.Contains("页面") ||
               enumOptions.Contains("段落");
    }
    
    /// <summary>
    /// 解析位置相关选项
    /// </summary>
    /// <param name="enumOptions">位置选项字符串</param>
    /// <returns>解析后的位置选项列表</returns>
    private static string[] ParsePositionOptions(string enumOptions)
    {
        // 定义位置相关的完整选项模式
        string[] patterns = [
            "页面顶端居中", "页面顶端左侧", "页面顶端右侧",
            "页面底端居中", "页面底端左侧", "页面底端右侧",
            "段落左对齐", "段落居中对齐", "段落右对齐",
            "相对于页面", "相对于段落", "相对于页边距"
        ];
        
        var options = new List<string>();
        string remaining = enumOptions;
        
        foreach (string pattern in patterns)
        {
            if (remaining.Contains(pattern))
            {
                options.Add(pattern);
                remaining = remaining.Replace(pattern, "").Replace(",,", ",");
            }
        }
        
        // 处理剩余的选项
        if (!string.IsNullOrEmpty(remaining))
        {
            string[] remainingOptions = remaining.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string option in remainingOptions)
            {
                string trimmed = option.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !options.Contains(trimmed))
                {
                    options.Add(trimmed);
                }
            }
        }
        
        return options.ToArray();
    }
    
    /// <summary>
    /// 验证参数值是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public bool IsValid()
    {
        // 检查必填参数
        if (IsRequired && string.IsNullOrEmpty(Value))
            return false;
        
        // 根据类型验证
        switch (Type)
        {
            case ParameterType.Number:
                if (!double.TryParse(Value, out double numValue))
                    return false;
                if (MinValue.HasValue && numValue < MinValue.Value)
                    return false;
                if (MaxValue.HasValue && numValue > MaxValue.Value)
                    return false;
                break;
                
            case ParameterType.Position:
                if (PositionValue != null && !PositionValue.IsValid())
                    return false;
                break;
                
            case ParameterType.Enum:
                if (!string.IsNullOrEmpty(Value) && !EnumOptionsList.Contains(Value))
                    return false;
                break;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取参数的显示值
    /// </summary>
    /// <returns>显示值</returns>
    public string GetDisplayValue()
    {
        return Type switch
        {
            ParameterType.Position => PositionValue?.ToString() ?? "未设置",
            ParameterType.Boolean => Value == "true" ? "是" : "否",
            _ => Value ?? DefaultValue ?? "未设置"
        };
    }
}

/// <summary>
/// 操作点配置模型
/// </summary>
public class OperationPointConfiguration : ReactiveObject
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [Reactive] public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 操作点名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作点描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作点类型
    /// </summary>
    [Reactive] public OperationPointType Type { get; set; } = OperationPointType.SetGraphicPosition;
    
    /// <summary>
    /// 目标元素
    /// </summary>
    [Reactive] public GraphicElementPosition? TargetElement { get; set; }
    
    /// <summary>
    /// 配置参数列表
    /// </summary>
    public ObservableCollection<ConfigurationParameter> Parameters { get; set; } = [];
    
    /// <summary>
    /// 操作点分值
    /// </summary>
    [Reactive] public decimal Score { get; set; } = 5.0m;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    [Reactive] public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    [Reactive] public DateTime LastModified { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public bool IsValid()
    {
        // 检查基本属性
        if (string.IsNullOrEmpty(Name)) return false;
        if (Score < 0) return false;
        
        // 检查所有参数
        return Parameters.All(p => p.IsValid());
    }
    
    /// <summary>
    /// 添加参数
    /// </summary>
    /// <param name="parameter">要添加的参数</param>
    public void AddParameter(ConfigurationParameter parameter)
    {
        parameter.Order = Parameters.Count;
        Parameters.Add(parameter);
        LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 移除参数
    /// </summary>
    /// <param name="parameter">要移除的参数</param>
    public void RemoveParameter(ConfigurationParameter parameter)
    {
        Parameters.Remove(parameter);
        
        // 重新排序
        for (int i = 0; i < Parameters.Count; i++)
        {
            Parameters[i].Order = i;
        }
        
        LastModified = DateTime.Now;
    }
    
    /// <summary>
    /// 获取参数值
    /// </summary>
    /// <param name="parameterName">参数名称</param>
    /// <returns>参数值</returns>
    public string? GetParameterValue(string parameterName)
    {
        return Parameters.FirstOrDefault(p => p.Name == parameterName)?.Value;
    }
    
    /// <summary>
    /// 设置参数值
    /// </summary>
    /// <param name="parameterName">参数名称</param>
    /// <param name="value">参数值</param>
    public void SetParameterValue(string parameterName, string value)
    {
        var parameter = Parameters.FirstOrDefault(p => p.Name == parameterName);
        if (parameter != null)
        {
            parameter.Value = value;
            LastModified = DateTime.Now;
        }
    }
}
