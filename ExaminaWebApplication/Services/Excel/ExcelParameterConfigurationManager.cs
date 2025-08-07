using System.Text.Json;
using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Services.Excel;

/// <summary>
/// Excel参数配置管理器
/// </summary>
public class ExcelParameterConfigurationManager
{
    /// <summary>
    /// 创建参数配置实例
    /// </summary>
    /// <param name="parameter">参数定义</param>
    /// <param name="value">参数值</param>
    /// <returns></returns>
    public static ExcelParameterConfigurationBase CreateParameterConfiguration(
        ExcelOperationParameter parameter, 
        object? value = null)
    {
        ExcelParameterConfigurationBase config = parameter.DataType switch
        {
            ExcelParameterDataType.String => new StringParameterConfiguration(),
            ExcelParameterDataType.Integer => new IntegerParameterConfiguration(),
            ExcelParameterDataType.Decimal => new DecimalParameterConfiguration(),
            ExcelParameterDataType.Boolean => new BooleanParameterConfiguration(),
            ExcelParameterDataType.Enum => new EnumParameterConfiguration(),
            ExcelParameterDataType.CellRange => new CellRangeParameterConfiguration(),
            ExcelParameterDataType.Color => new ColorParameterConfiguration(),
            ExcelParameterDataType.Formula => new FormulaParameterConfiguration(),
            _ => new StringParameterConfiguration()
        };

        config.ParameterName = parameter.ParameterName;
        config.IsRequired = parameter.IsRequired;
        config.Value = value ?? parameter.DefaultValue;

        // 根据验证规则配置参数
        if (!string.IsNullOrEmpty(parameter.ValidationRules))
        {
            ConfigureValidationRules(config, parameter.ValidationRules);
        }

        return config;
    }

    /// <summary>
    /// 配置验证规则
    /// </summary>
    /// <param name="config">参数配置</param>
    /// <param name="validationRulesJson">验证规则JSON</param>
    private static void ConfigureValidationRules(ExcelParameterConfigurationBase config, string validationRulesJson)
    {
        try
        {
            JsonDocument doc = JsonDocument.Parse(validationRulesJson);
            JsonElement root = doc.RootElement;

            switch (config)
            {
                case StringParameterConfiguration stringConfig:
                    if (root.TryGetProperty("minLength", out JsonElement minLength))
                        stringConfig.MinLength = minLength.GetInt32();
                    if (root.TryGetProperty("maxLength", out JsonElement maxLength))
                        stringConfig.MaxLength = maxLength.GetInt32();
                    if (root.TryGetProperty("regexPattern", out JsonElement regexPattern))
                        stringConfig.RegexPattern = regexPattern.GetString();
                    break;

                case IntegerParameterConfiguration intConfig:
                    if (root.TryGetProperty("minValue", out JsonElement intMinValue))
                        intConfig.MinValue = intMinValue.GetInt32();
                    if (root.TryGetProperty("maxValue", out JsonElement intMaxValue))
                        intConfig.MaxValue = intMaxValue.GetInt32();
                    break;

                case DecimalParameterConfiguration decimalConfig:
                    if (root.TryGetProperty("minValue", out JsonElement decMinValue))
                        decimalConfig.MinValue = decMinValue.GetDecimal();
                    if (root.TryGetProperty("maxValue", out JsonElement decMaxValue))
                        decimalConfig.MaxValue = decMaxValue.GetDecimal();
                    if (root.TryGetProperty("decimalPlaces", out JsonElement decimalPlaces))
                        decimalConfig.DecimalPlaces = decimalPlaces.GetInt32();
                    break;

                case EnumParameterConfiguration enumConfig:
                    if (root.TryGetProperty("enumTypeId", out JsonElement enumTypeId))
                        enumConfig.EnumTypeId = enumTypeId.GetInt32();
                    if (root.TryGetProperty("allowedValues", out JsonElement allowedValues))
                    {
                        enumConfig.AllowedValues = allowedValues.EnumerateArray()
                            .Select(x => x.GetString() ?? string.Empty)
                            .ToList();
                    }
                    break;

                case CellRangeParameterConfiguration cellRangeConfig:
                    if (root.TryGetProperty("allowSingleCell", out JsonElement allowSingleCell))
                        cellRangeConfig.AllowSingleCell = allowSingleCell.GetBoolean();
                    if (root.TryGetProperty("allowMultipleRanges", out JsonElement allowMultipleRanges))
                        cellRangeConfig.AllowMultipleRanges = allowMultipleRanges.GetBoolean();
                    break;

                case ColorParameterConfiguration colorConfig:
                    if (root.TryGetProperty("colorFormat", out JsonElement colorFormat))
                        colorConfig.ColorFormat = colorFormat.GetString() ?? "RGB";
                    break;

                case FormulaParameterConfiguration formulaConfig:
                    if (root.TryGetProperty("allowedFunctions", out JsonElement allowedFunctions))
                    {
                        formulaConfig.AllowedFunctions = allowedFunctions.EnumerateArray()
                            .Select(x => x.GetString() ?? string.Empty)
                            .ToList();
                    }
                    break;
            }
        }
        catch (JsonException)
        {
            // 忽略JSON解析错误，使用默认配置
        }
    }

    /// <summary>
    /// 验证参数配置列表
    /// </summary>
    /// <param name="configurations">参数配置列表</param>
    /// <returns>验证结果</returns>
    public static ParameterValidationResult ValidateParameters(
        List<ExcelParameterConfigurationBase> configurations)
    {
        ParameterValidationResult result = new ParameterValidationResult();

        foreach (ExcelParameterConfigurationBase config in configurations)
        {
            if (!config.IsValid())
            {
                result.IsValid = false;
                result.Errors.Add($"参数 '{config.ParameterName}' 验证失败");
            }
        }

        return result;
    }

    /// <summary>
    /// 将参数配置列表序列化为JSON
    /// </summary>
    /// <param name="configurations">参数配置列表</param>
    /// <returns>JSON字符串</returns>
    public static string SerializeParameters(List<ExcelParameterConfigurationBase> configurations)
    {
        Dictionary<string, object?> parameterDict = configurations.ToDictionary(
            config => config.ParameterName,
            config => config.Value
        );

        return JsonSerializer.Serialize(parameterDict, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// 从JSON反序列化参数配置
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <param name="parameters">参数定义列表</param>
    /// <returns>参数配置列表</returns>
    public static List<ExcelParameterConfigurationBase> DeserializeParameters(
        string json, 
        List<ExcelOperationParameter> parameters)
    {
        List<ExcelParameterConfigurationBase> configurations = new List<ExcelParameterConfigurationBase>();

        try
        {
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            foreach (ExcelOperationParameter parameter in parameters)
            {
                object? value = null;
                if (root.TryGetProperty(parameter.ParameterName, out JsonElement valueElement))
                {
                    value = parameter.DataType switch
                    {
                        ExcelParameterDataType.String => valueElement.GetString(),
                        ExcelParameterDataType.Integer => valueElement.GetInt32(),
                        ExcelParameterDataType.Decimal => valueElement.GetDecimal(),
                        ExcelParameterDataType.Boolean => valueElement.GetBoolean(),
                        ExcelParameterDataType.Enum => valueElement.GetString(),
                        ExcelParameterDataType.CellRange => valueElement.GetString(),
                        ExcelParameterDataType.Color => valueElement.GetString(),
                        ExcelParameterDataType.Formula => valueElement.GetString(),
                        _ => valueElement.GetString()
                    };
                }

                ExcelParameterConfigurationBase config = CreateParameterConfiguration(parameter, value);
                configurations.Add(config);
            }
        }
        catch (JsonException)
        {
            // 如果JSON解析失败，返回默认配置
            configurations = parameters.Select(p => CreateParameterConfiguration(p)).ToList();
        }

        return configurations;
    }
}

/// <summary>
/// 参数验证结果
/// </summary>
public class ParameterValidationResult
{
    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
}
