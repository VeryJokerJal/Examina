using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchSuite.Models;

namespace BenchSuite.Converters;

/// <summary>
/// ParameterType 枚举的 JSON 转换器
/// 支持：
/// - 直接的枚举名称（不区分大小写），如：Text/Number/Boolean/Enum/Date
/// - 常见同义词（中英文、技术别名），如：string/整数/bool/枚举/日期 等
/// - 数字与数字字符串（如 1 或 "1"）
/// </summary>
public class ParameterTypeJsonConverter : JsonConverter<ParameterType>
{
    public override ParameterType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        throw new JsonException("ParameterType 字符串值不能为空");
                    }

                    string normalized = stringValue.Trim();

                    // 1) 直接按名称解析（大小写不敏感）
                    if (Enum.TryParse<ParameterType>(normalized, ignoreCase: true, out ParameterType direct))
                    {
                        return direct;
                    }

                    // 2) 常见同义词映射（中英文、技术别名）
                    Dictionary<string, ParameterType> synonyms = new Dictionary<string, ParameterType>(StringComparer.OrdinalIgnoreCase)
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
                        ["time"] = ParameterType.Date,

                        // Color
                        ["color"] = ParameterType.Color,
                        ["colour"] = ParameterType.Color,
                        ["颜色"] = ParameterType.Color
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
                    if (Enum.IsDefined(typeof(ParameterType), intValue))
                    {
                        return (ParameterType)intValue;
                    }

                    throw new JsonException($"数值 {intValue} 不是有效的 ParameterType 枚举值");
                }

            default:
                throw new JsonException($"无法从 {reader.TokenType} 类型转换为 ParameterType");
        }
    }

    public override void Write(Utf8JsonWriter writer, ParameterType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
