using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamLab.Models;

namespace ExamLab.Converters;

/// <summary>
/// CSharpQuestionType枚举的JSON转换器
/// 支持字符串和数字两种格式的序列化和反序列化
/// </summary>
public class CSharpQuestionTypeJsonConverter : JsonConverter<CSharpQuestionType>
{
    /// <summary>
    /// 从JSON读取CSharpQuestionType值
    /// </summary>
    public override CSharpQuestionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        return CSharpQuestionType.CodeCompletion; // 默认值
                    }

                    // 尝试按名称解析枚举
                    if (Enum.TryParse<CSharpQuestionType>(stringValue, true, out CSharpQuestionType result))
                    {
                        return result;
                    }

                    // 如果解析失败，返回默认值
                    return CSharpQuestionType.CodeCompletion;
                }

            case JsonTokenType.Number:
                {
                    int intValue = reader.GetInt32();

                    // 检查是否为有效的枚举值
                    if (Enum.IsDefined(typeof(CSharpQuestionType), intValue))
                    {
                        return (CSharpQuestionType)intValue;
                    }

                    // 如果不是有效值，返回默认值
                    return CSharpQuestionType.CodeCompletion;
                }

            default:
                return CSharpQuestionType.CodeCompletion; // 默认值
        }
    }

    /// <summary>
    /// 将CSharpQuestionType值写入JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, CSharpQuestionType value, JsonSerializerOptions options)
    {
        // 序列化为字符串格式
        writer.WriteStringValue(value.ToString());
    }
}
