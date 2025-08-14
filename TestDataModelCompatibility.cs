using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BenchSuite.Models;
using BenchSuite.Services;
using BenchSuite.Console.Services;

namespace DataModelTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
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

                // 测试4: 文件格式自动识别
                await TestFileFormatDetection();
                Console.WriteLine();

                // 测试5: 完整的往返转换
                TestRoundTripConversion();
                Console.WriteLine();

                Console.WriteLine("✅ 所有兼容性测试完成!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
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
                string json = JsonSerializer.Serialize(examModel, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                Console.WriteLine($"✅ JSON序列化成功，长度: {json.Length}字符");
                
                // 反序列化测试
                var deserializedModel = JsonSerializer.Deserialize<ExamModel>(json);
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
                    startTime = DateTime.Now,
                    endTime = DateTime.Now.AddMinutes(90),
                    allowRetake = false,
                    maxRetakeCount = 0,
                    passingScore = 48.0m,
                    randomizeQuestions = true,
                    showScore = true,
                    showAnswers = false,
                    createdAt = DateTime.UtcNow.AddDays(-7),
                    updatedAt = DateTime.UtcNow.AddDays(-1),
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
                            questions = new[]
                            {
                                new
                                {
                                    id = "examlab-question-001",
                                    title = "创建Word文档",
                                    content = "创建一个包含标题和段落的Word文档",
                                    questionType = "Practical",
                                    score = 20.0m,
                                    difficultyLevel = 2,
                                    estimatedMinutes = 15,
                                    sortOrder = 1,
                                    isRequired = true,
                                    isEnabled = true,
                                    operationPoints = new[]
                                    {
                                        new
                                        {
                                            id = "examlab-op-001",
                                            name = "设置文档标题",
                                            description = "为文档添加标题",
                                            moduleType = "Word",
                                            wordKnowledgeType = "DocumentFormatting",
                                            score = 10.0m,
                                            order = 1,
                                            isEnabled = true,
                                            createdTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                            parameters = new[]
                                            {
                                                new
                                                {
                                                    id = "examlab-param-001",
                                                    name = "title",
                                                    displayName = "标题文本",
                                                    value = "测试文档",
                                                    type = "Text",
                                                    isRequired = true,
                                                    description = "文档的主标题"
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                metadata = new
                {
                    exportVersion = "1.0",
                    exportDate = DateTime.UtcNow,
                    exportedBy = "ExamLab",
                    totalSubjects = 1,
                    totalQuestions = 1,
                    totalOperationPoints = 1,
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
                    Console.WriteLine($"✅ 模块题目数: {firstModule.Questions.Count}");
                    
                    if (firstModule.Questions.Count > 0)
                    {
                        var firstQuestion = firstModule.Questions[0];
                        Console.WriteLine($"✅ 第一个题目: {firstQuestion.Title}");
                        Console.WriteLine($"✅ 操作点数量: {firstQuestion.OperationPoints.Count}");
                    }
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

        static async Task TestFileFormatDetection()
        {
            Console.WriteLine("--- 测试4: 文件格式自动识别 ---");

            // 测试BenchSuite JSON格式检测
            string benchSuiteJson = @"{
                ""id"": ""test-001"",
                ""name"": ""测试试卷"",
                ""description"": ""BenchSuite格式测试"",
                ""modules"": []
            }";

            // 测试ExamLab JSON格式检测
            string examLabJson = @"{
                ""exam"": {
                    ""id"": ""test-002"",
                    ""name"": ""ExamLab测试试卷"",
                    ""modules"": []
                },
                ""metadata"": {
                    ""exportVersion"": ""1.0"",
                    ""exportedBy"": ""ExamLab""
                }
            }";

            // 创建临时文件进行测试
            string tempDir = Path.GetTempPath();
            string benchSuiteFile = Path.Combine(tempDir, "benchsuite-test.json");
            string examLabFile = Path.Combine(tempDir, "examlab-test.json");

            try
            {
                // 写入测试文件
                await File.WriteAllTextAsync(benchSuiteFile, benchSuiteJson);
                await File.WriteAllTextAsync(examLabFile, examLabJson);

                // 测试BenchSuite格式检测
                var benchSuiteResult = await ExamModelLoader.LoadAsync(benchSuiteFile, verbose: true);
                Console.WriteLine($"BenchSuite格式检测: {(benchSuiteResult.DetectedFormat == ExamModelLoader.FileFormat.Json ? "✅" : "❌")}");

                // 测试ExamLab格式检测
                var examLabResult = await ExamModelLoader.LoadAsync(examLabFile, verbose: true);
                Console.WriteLine($"ExamLab格式检测: {(examLabResult.DetectedFormat == ExamModelLoader.FileFormat.Json ? "✅" : "❌")}");

                // 清理临时文件
                File.Delete(benchSuiteFile);
                File.Delete(examLabFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 文件格式检测测试失败: {ex.Message}");
            }
        }

        static void TestRoundTripConversion()
        {
            Console.WriteLine("--- 测试5: 完整的往返转换 ---");

            try
            {
                // 创建原始BenchSuite数据
                var originalModel = new ExamModel
                {
                    Id = "roundtrip-001",
                    Name = "往返转换测试",
                    Description = "测试数据完整性",
                    TotalScore = 100.0m,
                    DurationMinutes = 90,
                    AllowRetake = true,
                    PassingScore = 60.0m,
                    Tags = "往返测试,数据完整性"
                };

                Console.WriteLine($"原始数据: {originalModel.Name}");

                // 第一步: BenchSuite → ExamLab
                var examLabData = ExamModelConverter.ToExamLabExport(originalModel, "Complete");
                Console.WriteLine("✅ 第一步转换: BenchSuite → ExamLab");

                // 第二步: ExamLab → JSON → JsonElement
                string json = JsonSerializer.Serialize(examLabData);
                var jsonDoc = JsonDocument.Parse(json);
                Console.WriteLine("✅ 第二步序列化: ExamLab → JSON");

                // 第三步: JsonElement → BenchSuite
                var convertedModel = ExamModelConverter.FromExamLabExport(jsonDoc.RootElement);
                Console.WriteLine("✅ 第三步转换: ExamLab → BenchSuite");

                // 验证数据完整性
                bool dataIntegrityCheck = 
                    originalModel.Id == convertedModel.Id &&
                    originalModel.Name == convertedModel.Name &&
                    originalModel.Description == convertedModel.Description &&
                    originalModel.TotalScore == convertedModel.TotalScore &&
                    originalModel.DurationMinutes == convertedModel.DurationMinutes &&
                    originalModel.AllowRetake == convertedModel.AllowRetake &&
                    originalModel.PassingScore == convertedModel.PassingScore &&
                    originalModel.Tags == convertedModel.Tags;

                Console.WriteLine($"数据完整性检查: {(dataIntegrityCheck ? "✅ 通过" : "❌ 失败")}");

                if (!dataIntegrityCheck)
                {
                    Console.WriteLine("详细对比:");
                    Console.WriteLine($"  ID: {originalModel.Id} → {convertedModel.Id}");
                    Console.WriteLine($"  名称: {originalModel.Name} → {convertedModel.Name}");
                    Console.WriteLine($"  总分: {originalModel.TotalScore} → {convertedModel.TotalScore}");
                    Console.WriteLine($"  时长: {originalModel.DurationMinutes} → {convertedModel.DurationMinutes}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 往返转换测试失败: {ex.Message}");
            }
        }
    }
}
