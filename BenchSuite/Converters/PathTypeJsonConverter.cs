using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchSuite.Models;

namespace BenchSuite.Converters;

/// <summary>
/// PathType枚举的JSON转换器
/// 支持字符串和数字两种格式的序列化和反序列化
/// </summary>
public class PathTypeJsonConverter : JsonConverter<PathType>
{
    /// <summary>
    /// 从JSON读取PathType值
    /// </summary>
    public override PathType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        throw new JsonException("PathType字符串值不能为空");
                    }

                    // 尝试按名称解析枚举
                    if (Enum.TryParse<PathType>(stringValue, true, out PathType result))
                    {
                        return result;
                    }

                    throw new JsonException($"无法将字符串 '{stringValue}' 转换为 PathType 枚举");
                }

            case JsonTokenType.Number:
                {
                    int intValue = reader.GetInt32();

                    // 检查是否为有效的枚举值
                    if (Enum.IsDefined(typeof(PathType), intValue))
                    {
                        return (PathType)intValue;
                    }

                    throw new JsonException($"数值 {intValue} 不是有效的 PathType 枚举值");
                }

            default:
                throw new JsonException($"无法从 {reader.TokenType} 类型转换为 PathType");
        }
    }

    /// <summary>
    /// 将PathType值写入JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, PathType value, JsonSerializerOptions options)
    {
        // 序列化为字符串格式
        writer.WriteStringValue(value.ToString());
    }
}
