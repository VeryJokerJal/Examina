using System.Text.Json;
using System.Text.Json.Serialization;
using BenchSuite.Models;

namespace BenchSuite.Converters;

/// <summary>
/// ParameterType枚举的JSON转换器
/// 支持字符串、数字与常见同义词（含中英文）格式的序列化和反序列化
/// </summary>
public class ParameterTypeJsonConverter : JsonConverter<ParameterType>
{
    /// <summary>
    /// 从JSON读取ParameterType值
    /// </summary>
    public override ParameterType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        throw new JsonException("ParameterType字符串值不能为空");
                    }

                    string normalized = stringValue.Trim();

                    // 1) 直接按名称解析（大小写不敏感），例如：Text/Number/Boolean/Enum/Date
                    if (Enum.TryParse(normalized, ignoreCase: true, out ParameterType directResult))
                    {
                        return directResult;
                    }

                    // 2) 常见同义词映射（中英文、技术别名）
                    Dictionary<string, ParameterType> synonyms = new(StringComparer.OrdinalIgnoreCase)
                    {
                        // Text
                        ["text"] = ParameterType.Text,
                        ["string"] = ParameterType.Text,
                        ["str"] = ParameterType.Text,
                        ["字符串"] = ParameterType.Text,
                        ["文本"] = ParameterType.Text,

                        // Number
                        ["number"] = ParameterType.Number,
                        ["int"] = ParameterType.Number,
                        ["integer"] = ParameterType.Number,
                        ["float"] = ParameterType.Number,
                        ["double"] = ParameterType.Number,
                        ["decimal"] = ParameterType.Number,
                        ["数字"] = ParameterType.Number,
                        ["数值"] = ParameterType.Number,

                        // Boolean
                        ["bool"] = ParameterType.Boolean,
                        ["boolean"] = ParameterType.Boolean,
                        ["布尔"] = ParameterType.Boolean,
                        ["布尔值"] = ParameterType.Boolean,

                        // Enum
                        ["enum"] = ParameterType.Enum,
                        ["enumeration"] = ParameterType.Enum,
                        ["枚举"] = ParameterType.Enum,
                        ["枚举值"] = ParameterType.Enum,

                        // Date
                        ["date"] = ParameterType.Date,
                        ["datetime"] = ParameterType.Date,
                        ["日期"] = ParameterType.Date,
                        ["时间"] = ParameterType.Date,
                        ["time"] = ParameterType.Date
                    };

                    if (synonyms.TryGetValue(normalized, out ParameterType mapped))
                    {
                        return mapped;
                    }

                    // 3) 数字字符串（例如 "1"）
                    if (int.TryParse(normalized, out int numeric))
                    {
                        if (Enum.IsDefined(typeof(ParameterType), numeric))
                        {
                            return (ParameterType)numeric;
                        }
                    }

                    throw new JsonException($"无法将字符串 '{stringValue}' 转换为 ParameterType 枚举");
                }

            case JsonTokenType.Number:
                {
                    int intValue = reader.GetInt32();

                    // 检查是否为有效的枚举值
                    return Enum.IsDefined(typeof(ParameterType), intValue)
                        ? (ParameterType)intValue
                        : throw new JsonException($"数值 {intValue} 不是有效的 ParameterType 枚举值");
                }

            default:
                throw new JsonException($"无法从 {reader.TokenType} 类型转换为 ParameterType");
        }
    }

    /// <summary>
    /// 将ParameterType值写入JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ParameterType value, JsonSerializerOptions options)
    {
        // 序列化为字符串格式
        writer.WriteStringValue(value.ToString());
    }
}
