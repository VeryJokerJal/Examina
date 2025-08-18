using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExamLab.Models;
using ReactiveUI;

namespace ExamLab.Services;

/// <summary>
/// 位置参数控制器 - 管理位置参数的动态显示逻辑
/// </summary>
public class PositionParameterController : ReactiveObject
{
    /// <summary>
    /// 水平位置类型参数名称
    /// </summary>
    public const string HorizontalPositionTypeParam = "HorizontalPositionType";

    /// <summary>
    /// 垂直位置类型参数名称
    /// </summary>
    public const string VerticalPositionTypeParam = "VerticalPositionType";

    /// <summary>
    /// 位置参数依赖关系映射
    /// </summary>
    private static readonly Dictionary<string, PositionParameterRule> ParameterRules = new()
    {
        // 水平位置参数规则
        ["HorizontalAlignment"] = new("HorizontalPositionType", "对齐方式"),
        ["HorizontalRelativeTo"] = new("HorizontalPositionType", new[] { "对齐方式", "相对位置" }),
        ["HorizontalAbsolutePosition"] = new("HorizontalPositionType", "绝对位置"),
        ["HorizontalRelativePosition"] = new("HorizontalPositionType", "相对位置"),

        // 垂直位置参数规则
        ["VerticalAlignment"] = new("VerticalPositionType", "对齐方式"),
        ["VerticalRelativeTo"] = new("VerticalPositionType", new[] { "对齐方式", "相对位置" }),
        ["VerticalAbsolutePosition"] = new("VerticalPositionType", "绝对位置"),
        ["VerticalRelativePosition"] = new("VerticalPositionType", "相对位置")
    };

    /// <summary>
    /// 初始化位置参数的依赖关系
    /// </summary>
    /// <param name="parameters">参数列表</param>
    public static void InitializePositionParameters(ObservableCollection<ConfigurationParameter> parameters)
    {
        foreach (ConfigurationParameter parameter in parameters)
        {
            if (ParameterRules.TryGetValue(parameter.Name, out PositionParameterRule? rule))
            {
                parameter.DependsOn = rule.DependsOn;
                parameter.DependsOnValue = string.Join(",", rule.RequiredValues);
                parameter.IsVisible = false; // 初始状态隐藏，等待依赖参数设置
            }
        }

        // 为位置类型参数添加变更监听
        ConfigurationParameter? horizontalTypeParam = parameters.FirstOrDefault(p => p.Name == HorizontalPositionTypeParam);
        ConfigurationParameter? verticalTypeParam = parameters.FirstOrDefault(p => p.Name == VerticalPositionTypeParam);

        if (horizontalTypeParam != null)
        {
            horizontalTypeParam.WhenAnyValue(x => x.Value)
                .Subscribe(value => UpdateParameterVisibility(parameters, HorizontalPositionTypeParam, value));
        }

        if (verticalTypeParam != null)
        {
            verticalTypeParam.WhenAnyValue(x => x.Value)
                .Subscribe(value => UpdateParameterVisibility(parameters, VerticalPositionTypeParam, value));
        }
    }

    /// <summary>
    /// 更新参数可见性
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <param name="changedParameterName">变更的参数名称</param>
    /// <param name="newValue">新值</param>
    public static void UpdateParameterVisibility(ObservableCollection<ConfigurationParameter> parameters, string changedParameterName, string? newValue)
    {
        // 获取依赖于此参数的所有参数
        List<ConfigurationParameter> dependentParameters = parameters
            .Where(p => p.DependsOn == changedParameterName)
            .ToList();

        foreach (ConfigurationParameter parameter in dependentParameters)
        {
            bool shouldBeVisible = ShouldParameterBeVisible(parameter, newValue);
            
            if (parameter.IsVisible != shouldBeVisible)
            {
                parameter.IsVisible = shouldBeVisible;
                
                // 如果参数变为不可见，清空其值
                if (!shouldBeVisible)
                {
                    parameter.Value = null;
                }
            }
        }
    }

    /// <summary>
    /// 判断参数是否应该可见
    /// </summary>
    /// <param name="parameter">参数</param>
    /// <param name="dependencyValue">依赖参数的值</param>
    /// <returns>是否应该可见</returns>
    private static bool ShouldParameterBeVisible(ConfigurationParameter parameter, string? dependencyValue)
    {
        if (string.IsNullOrEmpty(dependencyValue) || string.IsNullOrEmpty(parameter.DependsOnValue))
        {
            return false;
        }

        string[] requiredValues = parameter.DependsOnValue.Split(',');
        return requiredValues.Contains(dependencyValue);
    }

    /// <summary>
    /// 获取位置参数分组
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <returns>分组后的参数</returns>
    public static Dictionary<string, List<ConfigurationParameter>> GetPositionParameterGroups(ObservableCollection<ConfigurationParameter> parameters)
    {
        Dictionary<string, List<ConfigurationParameter>> groups = new()
        {
            ["水平位置"] = [],
            ["垂直位置"] = [],
            ["选项设置"] = [],
            ["其他"] = []
        };

        foreach (ConfigurationParameter parameter in parameters)
        {
            string groupName = GetParameterGroup(parameter.Name);
            groups[groupName].Add(parameter);
        }

        return groups;
    }

    /// <summary>
    /// 获取参数所属分组
    /// </summary>
    /// <param name="parameterName">参数名称</param>
    /// <returns>分组名称</returns>
    private static string GetParameterGroup(string parameterName)
    {
        return parameterName switch
        {
            "HorizontalPositionType" or "HorizontalAlignment" or "HorizontalRelativeTo" or 
            "HorizontalAbsolutePosition" or "HorizontalRelativePosition" => "水平位置",
            
            "VerticalPositionType" or "VerticalAlignment" or "VerticalRelativeTo" or 
            "VerticalAbsolutePosition" or "VerticalRelativePosition" => "垂直位置",
            
            "MoveWithText" or "LockAnchor" or "AllowOverlap" or "LayoutInTableCell" => "选项设置",
            
            _ => "其他"
        };
    }

    /// <summary>
    /// 验证位置参数配置
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <returns>验证结果</returns>
    public static (bool IsValid, string ErrorMessage) ValidatePositionParameters(ObservableCollection<ConfigurationParameter> parameters)
    {
        // 检查水平位置类型是否已选择
        ConfigurationParameter? horizontalTypeParam = parameters.FirstOrDefault(p => p.Name == HorizontalPositionTypeParam);
        if (horizontalTypeParam?.IsRequired == true && string.IsNullOrEmpty(horizontalTypeParam.Value))
        {
            return (false, "请选择水平位置类型");
        }

        // 检查垂直位置类型是否已选择
        ConfigurationParameter? verticalTypeParam = parameters.FirstOrDefault(p => p.Name == VerticalPositionTypeParam);
        if (verticalTypeParam?.IsRequired == true && string.IsNullOrEmpty(verticalTypeParam.Value))
        {
            return (false, "请选择垂直位置类型");
        }

        // 检查必填的子参数
        foreach (ConfigurationParameter parameter in parameters.Where(p => p.IsRequired && p.IsVisible))
        {
            if (string.IsNullOrEmpty(parameter.Value))
            {
                return (false, $"参数 '{parameter.DisplayName}' 为必填项");
            }
        }

        return (true, string.Empty);
    }
}

/// <summary>
/// 位置参数规则
/// </summary>
public class PositionParameterRule
{
    /// <summary>
    /// 依赖的参数名称
    /// </summary>
    public string DependsOn { get; }

    /// <summary>
    /// 需要的值列表
    /// </summary>
    public string[] RequiredValues { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dependsOn">依赖的参数名称</param>
    /// <param name="requiredValue">需要的值</param>
    public PositionParameterRule(string dependsOn, string requiredValue)
    {
        DependsOn = dependsOn;
        RequiredValues = [requiredValue];
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dependsOn">依赖的参数名称</param>
    /// <param name="requiredValues">需要的值列表</param>
    public PositionParameterRule(string dependsOn, string[] requiredValues)
    {
        DependsOn = dependsOn;
        RequiredValues = requiredValues;
    }
}
