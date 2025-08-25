using System;
using System.IO;
using System.Text;
using System.Text.Json;
using BenchSuite.Models;
using BenchSuite.Converters;

try
{
    Console.WriteLine("=== JSON反序列化测试 ===");

    string jsonFilePath = @"BenchSuite.Console\TestData\sample-exam.json";

    if (!File.Exists(jsonFilePath))
    {
        Console.WriteLine($"错误: 文件不存在: {jsonFilePath}");
        return;
    }

    Console.WriteLine($"正在读取文件: {jsonFilePath}");

    // 使用UTF-8编码读取文件
    string jsonContent = await File.ReadAllTextAsync(jsonFilePath, Encoding.UTF8);

    if (string.IsNullOrWhiteSpace(jsonContent))
    {
        Console.WriteLine("错误: JSON文件为空");
        return;
    }

    Console.WriteLine($"文件大小: {jsonContent.Length} 字符");

    // 配置JSON序列化选项
    JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    // 添加自定义转换器
    options.Converters.Add(new ModuleTypeJsonConverter());
    options.Converters.Add(new ParameterTypeJsonConverter());
    options.Converters.Add(new BenchSuite.Converters.CSharpQuestionTypeJsonConverter());

    Console.WriteLine("开始反序列化...");

    // 尝试反序列化
    ExamModel? examModel = JsonSerializer.Deserialize<ExamModel>(jsonContent, options);

    if (examModel == null)
    {
        Console.WriteLine("错误: 反序列化结果为null");
        return;
    }

    Console.WriteLine("✅ JSON反序列化成功!");
    Console.WriteLine($"试卷名称: {examModel.Name}");
    Console.WriteLine($"模块数量: {examModel.Modules.Count}");

    // 检查PowerPoint模块
    var powerPointModule = examModel.Modules.FirstOrDefault(m => m.Type.ToString() == "PowerPoint");
    if (powerPointModule != null)
    {
        Console.WriteLine($"PowerPoint模块: {powerPointModule.Name}");
        Console.WriteLine($"题目数量: {powerPointModule.Questions.Count}");

        foreach (var question in powerPointModule.Questions)
        {
            Console.WriteLine($"  题目: {question.Title}");
            foreach (var operation in question.OperationPoints)
            {
                Console.WriteLine($"    操作: {operation.Name}");
                foreach (var parameter in operation.Parameters)
                {
                    Console.WriteLine($"      参数: {parameter.Name} (类型: {parameter.Type})");
                }
            }
        }
    }

    Console.WriteLine("测试完成!");
}
catch (JsonException ex)
{
    Console.WriteLine($"❌ JSON反序列化错误: {ex.Message}");
    Console.WriteLine($"路径: {ex.Path}");
    Console.WriteLine($"行号: {ex.LineNumber}");
    Console.WriteLine($"位置: {ex.BytePositionInLine}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 其他错误: {ex.Message}");
    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
}
