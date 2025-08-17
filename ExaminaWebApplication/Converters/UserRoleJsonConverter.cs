using System.Text.Json;
using System.Text.Json.Serialization;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Converters;

/// <summary>
/// UserRole枚举的JSON转换器
/// 支持字符串和数字两种格式的序列化和反序列化
/// </summary>
public class UserRoleJsonConverter : JsonConverter<UserRole>
{
    /// <summary>
    /// 从JSON读取UserRole值
    /// </summary>
    public override UserRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        throw new JsonException("UserRole字符串值不能为空");
                    }

                    // 尝试按名称解析枚举（忽略大小写）
                    if (Enum.TryParse<UserRole>(stringValue, true, out UserRole result))
                    {
                        return result;
                    }

                    throw new JsonException($"无法将字符串 '{stringValue}' 转换为 UserRole 枚举");
                }

            case JsonTokenType.Number:
                {
                    int intValue = reader.GetInt32();

                    // 检查是否为有效的枚举值
                    if (Enum.IsDefined(typeof(UserRole), intValue))
                    {
                        return (UserRole)intValue;
                    }

                    throw new JsonException($"数值 {intValue} 不是有效的 UserRole 枚举值");
                }

            default:
                throw new JsonException($"无法从 {reader.TokenType} 类型转换为 UserRole");
        }
    }

    /// <summary>
    /// 将UserRole值写入JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, UserRole value, JsonSerializerOptions options)
    {
        // 序列化为字符串格式
        writer.WriteStringValue(value.ToString());
    }
}
