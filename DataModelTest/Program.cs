using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BenchSuite.Models;
using BenchSuite.Services;
using BenchSuite.Console.Services;

try
{
    Console.WriteLine("=== 数据模型兼容性测试 ===");
    Console.WriteLine();

    // 测试1: BenchSuite模型扩展验证
    TestBenchSuiteModelExtensions();
    Console.WriteLine();

    // 测试2: ExamLab到BenchSuite转换
    TestExamLabToBenchSuiteConversion();
    Console.WriteLine();

    // 测试3: BenchSuite到ExamLab转换
    TestBenchSuiteToExamLabConversion();
    Console.WriteLine();

    Console.WriteLine("✅ 主要兼容性测试完成!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 测试失败: {ex.Message}");
    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
}

static void TestBenchSuiteModelExtensions()
{
    Console.WriteLine("--- 测试1: BenchSuite模型扩展验证 ---");

    // 创建扩展的ExamModel
    var examModel = new ExamModel
    {
        Id = "exam-test-001",
        Name = "测试试卷",
        Description = "用于验证模型扩展的测试试卷",

        // 新增字段测试
        TotalScore = 100.0m,
        DurationMinutes = 120,
        StartTime = DateTime.Now,
        EndTime = DateTime.Now.AddMinutes(120),
        AllowRetake = true,
        MaxRetakeCount = 3,
        PassingScore = 60.0m,
        RandomizeQuestions = false,
        ShowScore = true,
        ShowAnswers = false,
        CreatedAt = DateTime.UtcNow,
        IsEnabled = true,
        Tags = "测试,兼容性,PowerPoint",
        ExamType = "UnifiedExam",
        Status = "Published"
    };

    // 添加模块
    var module = new ExamModuleModel
    {
        Id = "module-test-001",
        Name = "PowerPoint操作",
        Type = ModuleType.PowerPoint,
        Description = "PowerPoint基础操作测试",
        Score = 50.0m,
        Order = 1,
        IsEnabled = true,

        // 新增字段测试
        DurationMinutes = 60,
        Weight = 1.0m,
        MinScore = 30.0m,
        IsRequired = true,
        ModuleConfig = "{\"allowSkip\": false}",
        SubjectType = "PowerPoint"
    };

    examModel.Modules.Add(module);

    // 验证字段
    Console.WriteLine($"✅ 试卷ID: {examModel.Id}");
    Console.WriteLine($"✅ 总分: {examModel.TotalScore}");
    Console.WriteLine($"✅ 考试时长: {examModel.DurationMinutes}分钟");
    Console.WriteLine($"✅ 允许重考: {examModel.AllowRetake}");
    Console.WriteLine($"✅ 模块数量: {examModel.Modules.Count}");
    Console.WriteLine($"✅ 模块权重: {module.Weight}");
    Console.WriteLine($"✅ 模块配置: {module.ModuleConfig}");

    // JSON序列化测试
    try
    {
        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // 添加自定义转换器
        jsonOptions.Converters.Add(new BenchSuite.Converters.ModuleTypeJsonConverter());
        jsonOptions.Converters.Add(new BenchSuite.Converters.ParameterTypeJsonConverter());
        jsonOptions.Converters.Add(new BenchSuite.Converters.CSharpQuestionTypeJsonConverter());

        string json = JsonSerializer.Serialize(examModel, jsonOptions);

        Console.WriteLine($"✅ JSON序列化成功，长度: {json.Length}字符");

        // 反序列化测试
        var deserializedModel = JsonSerializer.Deserialize<ExamModel>(json, jsonOptions);
        Console.WriteLine($"✅ JSON反序列化成功: {deserializedModel?.Name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ JSON序列化测试失败: {ex.Message}");
    }
}

static void TestExamLabToBenchSuiteConversion()
{
    Console.WriteLine("--- 测试2: ExamLab到BenchSuite转换 ---");

    // 模拟ExamLab导出格式
    var examLabData = new
    {
        exam = new
        {
            id = "examlab-001",
            name = "ExamLab测试试卷",
            description = "从ExamLab导出的测试数据",
            examType = "StandardExam",
            status = "Published",
            totalScore = 80.0m,
            durationMinutes = 90,
            allowRetake = false,
            maxRetakeCount = 0,
            passingScore = 48.0m,
            randomizeQuestions = true,
            showScore = true,
            showAnswers = false,
            createdAt = DateTime.UtcNow.AddDays(-7),
            isEnabled = true,
            tags = "ExamLab,导入测试",
            modules = new[]
            {
                new
                {
                    id = "examlab-module-001",
                    name = "Word文档处理",
                    type = "Word",
                    description = "Word基础操作",
                    score = 40.0m,
                    order = 1,
                    isEnabled = true,
                    durationMinutes = 45,
                    weight = 1.0m,
                    isRequired = true,
                    questions = new object[0] // 空数组
                }
            }
        },
        metadata = new
        {
            exportVersion = "1.0",
            exportDate = DateTime.UtcNow,
            exportedBy = "ExamLab",
            totalSubjects = 1,
            totalQuestions = 0,
            totalOperationPoints = 0,
            exportLevel = "Complete",
            exportFormat = "JSON"
        }
    };

    try
    {
        // 转换为JSON字符串
        string jsonString = JsonSerializer.Serialize(examLabData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // 解析为JsonElement
        var jsonDocument = JsonDocument.Parse(jsonString);
        var rootElement = jsonDocument.RootElement;

        // 使用转换器转换
        var convertedModel = ExamModelConverter.FromExamLabExport(rootElement);

        Console.WriteLine($"✅ 转换成功: {convertedModel.Name}");
        Console.WriteLine($"✅ 试卷ID: {convertedModel.Id}");
        Console.WriteLine($"✅ 总分: {convertedModel.TotalScore}");
        Console.WriteLine($"✅ 考试时长: {convertedModel.DurationMinutes}分钟");
        Console.WriteLine($"✅ 模块数量: {convertedModel.Modules.Count}");

        if (convertedModel.Modules.Count > 0)
        {
            var firstModule = convertedModel.Modules[0];
            Console.WriteLine($"✅ 第一个模块: {firstModule.Name} ({firstModule.Type})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ExamLab转换失败: {ex.Message}");
    }
}

static void TestBenchSuiteToExamLabConversion()
{
    Console.WriteLine("--- 测试3: BenchSuite到ExamLab转换 ---");

    // 创建BenchSuite格式的数据
    var examModel = new ExamModel
    {
        Id = "benchsuite-001",
        Name = "BenchSuite测试试卷",
        Description = "用于转换测试的BenchSuite数据",
        TotalScore = 100.0m,
        DurationMinutes = 120,
        AllowRetake = true,
        MaxRetakeCount = 2,
        PassingScore = 60.0m,
        ExamType = "UnifiedExam",
        Status = "Draft",
        CreatedAt = DateTime.UtcNow,
        IsEnabled = true,
        Tags = "BenchSuite,转换测试"
    };

    // 添加PowerPoint模块
    var pptModule = new ExamModuleModel
    {
        Id = "benchsuite-module-001",
        Name = "PowerPoint演示",
        Type = ModuleType.PowerPoint,
        Description = "PowerPoint基础操作",
        Score = 50.0m,
        Order = 1,
        IsEnabled = true,
        DurationMinutes = 60,
        Weight = 1.0m,
        IsRequired = true
    };

    examModel.Modules.Add(pptModule);

    try
    {
        // 转换为ExamLab格式
        var examLabData = ExamModelConverter.ToExamLabExport(examModel, "Complete");

        // 序列化为JSON验证
        string json = JsonSerializer.Serialize(examLabData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        Console.WriteLine($"✅ 转换为ExamLab格式成功");
        Console.WriteLine($"✅ JSON长度: {json.Length}字符");

        // 验证结构
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("exam", out var exam) && root.TryGetProperty("metadata", out var metadata))
        {
            Console.WriteLine($"✅ 包含exam和metadata节点");

            if (exam.TryGetProperty("name", out var name))
            {
                Console.WriteLine($"✅ 试卷名称: {name.GetString()}");
            }

            if (metadata.TryGetProperty("exportedBy", out var exportedBy))
            {
                Console.WriteLine($"✅ 导出工具: {exportedBy.GetString()}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ BenchSuite转换失败: {ex.Message}");
    }
}
