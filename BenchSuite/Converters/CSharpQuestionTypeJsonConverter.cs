using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchSuite.Converters;

/// <summary>
/// C#题目类型字符串的JSON转换器
/// 用于处理ExamLab中的CSharpQuestionType字符串字段
/// </summary>
public class CSharpQuestionTypeJsonConverter : JsonConverter<string>
{
    /// <summary>
    /// 从JSON读取C#题目类型字符串值
    /// </summary>
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        return "CodeCompletion"; // 默认值
                    }

                    // 标准化字符串值
                    return stringValue switch
                    {
                        "CodeCompletion" or "codecompletion" or "代码补全" => "CodeCompletion",
                        "Debugging" or "debugging" or "调试纠错" => "Debugging", 
                        "Implementation" or "implementation" or "编写实现" => "Implementation",
                        _ => stringValue // 保持原值
                    };
                }

            case JsonTokenType.Number:
                {
                    int intValue = reader.GetInt32();
                    
                    // 将数字转换为对应的字符串
                    return intValue switch
                    {
                        0 => "CodeCompletion",
                        1 => "Debugging",
                        2 => "Implementation",
                        _ => "CodeCompletion"
                    };
                }

            default:
                return "CodeCompletion"; // 默认值
        }
    }

    /// <summary>
    /// 将C#题目类型字符串值写入JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // 序列化为字符串格式
        writer.WriteStringValue(value ?? "CodeCompletion");
    }
}
