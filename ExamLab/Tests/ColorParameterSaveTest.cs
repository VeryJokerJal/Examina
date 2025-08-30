using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using ExamLab.Models;
using ExamLab.Services;

namespace ExamLab.Tests;

/// <summary>
/// 颜色参数保存和加载测试
/// </summary>
public static class ColorParameterSaveTest
{
    /// <summary>
    /// 测试颜色参数的保存和加载功能
    /// </summary>
    public static void RunTest()
    {
        Console.WriteLine("=== 颜色参数保存和加载测试 ===");
        
        try
        {
            // 1. 创建测试数据
            OperationPoint operationPoint = CreateTestOperationPoint();
            Console.WriteLine("✅ 创建测试操作点成功");
            
            // 2. 测试JSON序列化
            TestJsonSerialization(operationPoint);
            
            // 3. 测试XML序列化
            TestXmlSerialization(operationPoint);
            
            // 4. 测试颜色格式转换
            TestColorFormatConversion();
            
            Console.WriteLine("✅ 所有颜色参数测试通过！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败：{ex.Message}");
            Console.WriteLine($"详细信息：{ex}");
        }
    }
    
    /// <summary>
    /// 创建包含各种颜色参数的测试操作点
    /// </summary>
    private static OperationPoint CreateTestOperationPoint()
    {
        OperationPoint operationPoint = new()
        {
            Id = "test-op-001",
            Name = "设置段落底纹",
            Description = "测试段落底纹颜色设置",
            ModuleType = ModuleType.Word,
            Score = 5
        };
        
        // 添加底纹颜色参数
        ConfigurationParameter shadingColorParam = new()
        {
            Id = "param-001",
            Name = "ShadingColor",
            DisplayName = "底纹颜色",
            Description = "段落底纹填充颜色",
            Type = ParameterType.Color,
            Value = "#FF5733", // 橙红色
            DefaultValue = "#FFFF00", // 黄色
            IsRequired = true
        };
        operationPoint.Parameters.Add(shadingColorParam);
        
        // 添加文字颜色参数
        ConfigurationParameter textColorParam = new()
        {
            Id = "param-002",
            Name = "TextColor",
            DisplayName = "文字颜色",
            Description = "段落文字颜色",
            Type = ParameterType.Color,
            Value = "#0066CC", // 蓝色
            DefaultValue = "#000000", // 黑色
            IsRequired = true
        };
        operationPoint.Parameters.Add(textColorParam);
        
        // 添加边框颜色参数
        ConfigurationParameter borderColorParam = new()
        {
            Id = "param-003",
            Name = "BorderColor",
            DisplayName = "边框颜色",
            Description = "段落边框颜色",
            Type = ParameterType.Color,
            Value = "#00FF00", // 绿色
            DefaultValue = "#000000", // 黑色
            IsRequired = false
        };
        operationPoint.Parameters.Add(borderColorParam);
        
        return operationPoint;
    }
    
    /// <summary>
    /// 测试JSON序列化和反序列化
    /// </summary>
    private static void TestJsonSerialization(OperationPoint operationPoint)
    {
        Console.WriteLine("--- 测试JSON序列化 ---");
        
        // 序列化
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        string json = JsonSerializer.Serialize(operationPoint, options);
        Console.WriteLine($"JSON序列化成功，长度：{json.Length}字符");
        
        // 验证颜色值在JSON中的存在
        if (json.Contains("#FF5733") && json.Contains("#0066CC") && json.Contains("#00FF00"))
        {
            Console.WriteLine("✅ 颜色值在JSON中正确保存");
        }
        else
        {
            throw new Exception("❌ 颜色值在JSON中丢失");
        }
        
        // 反序列化
        OperationPoint? deserializedOp = JsonSerializer.Deserialize<OperationPoint>(json, options);
        if (deserializedOp == null)
        {
            throw new Exception("JSON反序列化失败");
        }
        
        // 验证颜色参数
        ValidateColorParameters(deserializedOp, operationPoint);
        Console.WriteLine("✅ JSON序列化测试通过");
    }
    
    /// <summary>
    /// 测试XML序列化和反序列化
    /// </summary>
    private static void TestXmlSerialization(OperationPoint operationPoint)
    {
        Console.WriteLine("--- 测试XML序列化 ---");
        
        // 转换为导出DTO
        ExamExportDto exportDto = new()
        {
            Name = "颜色测试试卷",
            Description = "测试颜色参数保存",
            Modules = []
        };
        
        ModuleDto moduleDto = new()
        {
            Name = "Word模块",
            Type = "Word",
            Questions = []
        };
        
        QuestionDto questionDto = new()
        {
            Name = "颜色测试题目",
            Description = "测试颜色参数",
            OperationPoints = []
        };
        
        OperationPointDto opDto = new()
        {
            Id = operationPoint.Id,
            Name = operationPoint.Name,
            Description = operationPoint.Description,
            ModuleType = operationPoint.ModuleType.ToString(),
            Score = operationPoint.Score,
            Parameters = []
        };
        
        // 转换参数
        foreach (ConfigurationParameter param in operationPoint.Parameters)
        {
            ParameterDto paramDto = new()
            {
                Name = param.Name,
                DisplayName = param.DisplayName,
                Description = param.Description,
                Type = param.Type.ToString(),
                Value = param.Value,
                DefaultValue = param.DefaultValue,
                IsRequired = param.IsRequired
            };
            opDto.Parameters.Add(paramDto);
        }
        
        questionDto.OperationPoints.Add(opDto);
        moduleDto.Questions.Add(questionDto);
        exportDto.Modules.Add(moduleDto);
        
        // XML序列化
        string xml = XmlSerializationService.SerializeToXml(exportDto);
        Console.WriteLine($"XML序列化成功，长度：{xml.Length}字符");
        
        // 验证颜色值在XML中的存在
        if (xml.Contains("#FF5733") && xml.Contains("#0066CC") && xml.Contains("#00FF00"))
        {
            Console.WriteLine("✅ 颜色值在XML中正确保存");
        }
        else
        {
            throw new Exception("❌ 颜色值在XML中丢失");
        }
        
        // XML反序列化
        ExamExportDto deserializedDto = XmlSerializationService.DeserializeFromXml(xml);
        
        // 验证颜色参数
        ParameterDto? shadingParam = deserializedDto.Modules[0].Questions[0].OperationPoints[0].Parameters
            .FirstOrDefault(p => p.Name == "ShadingColor");
        
        if (shadingParam?.Value != "#FF5733")
        {
            throw new Exception($"XML反序列化后底纹颜色不匹配：期望 #FF5733，实际 {shadingParam?.Value}");
        }
        
        Console.WriteLine("✅ XML序列化测试通过");
    }
    
    /// <summary>
    /// 验证颜色参数
    /// </summary>
    private static void ValidateColorParameters(OperationPoint actual, OperationPoint expected)
    {
        foreach (ConfigurationParameter expectedParam in expected.Parameters)
        {
            ConfigurationParameter? actualParam = actual.Parameters
                .FirstOrDefault(p => p.Name == expectedParam.Name);
            
            if (actualParam == null)
            {
                throw new Exception($"参数 {expectedParam.Name} 在反序列化后丢失");
            }
            
            if (actualParam.Value != expectedParam.Value)
            {
                throw new Exception($"参数 {expectedParam.Name} 值不匹配：期望 {expectedParam.Value}，实际 {actualParam.Value}");
            }
            
            if (actualParam.Type != expectedParam.Type)
            {
                throw new Exception($"参数 {expectedParam.Name} 类型不匹配：期望 {expectedParam.Type}，实际 {actualParam.Type}");
            }
        }
    }
    
    /// <summary>
    /// 测试颜色格式转换
    /// </summary>
    private static void TestColorFormatConversion()
    {
        Console.WriteLine("--- 测试颜色格式转换 ---");
        
        // 测试十六进制颜色解析
        string[] testColors = { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };
        
        foreach (string color in testColors)
        {
            if (!IsValidHexColor(color))
            {
                throw new Exception($"颜色格式验证失败：{color}");
            }
        }
        
        Console.WriteLine("✅ 颜色格式转换测试通过");
    }
    
    /// <summary>
    /// 验证十六进制颜色格式
    /// </summary>
    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color) || !color.StartsWith("#") || color.Length != 7)
        {
            return false;
        }
        
        string hex = color[1..];
        return hex.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }
}
