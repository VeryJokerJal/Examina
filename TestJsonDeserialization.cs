using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ExaminaWebApplication.Services.ImportedComprehensiveTraining;

namespace TestImportFix
{
    /// <summary>
    /// 测试JSON反序列化修复
    /// </summary>
    public class TestJsonDeserialization
    {
        public static async Task<bool> TestDeserializationAsync()
        {
            try
            {
                // 读取测试JSON文件
                string jsonContent = await File.ReadAllTextAsync("TestImportFix.json");
                
                // 配置JSON序列化选项
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                // 尝试反序列化
                ComprehensiveTrainingExportDto? result = JsonSerializer.Deserialize<ComprehensiveTrainingExportDto>(jsonContent, options);
                
                if (result == null)
                {
                    Console.WriteLine("❌ 反序列化失败：结果为null");
                    return false;
                }

                // 验证关键字段
                Console.WriteLine("✅ JSON反序列化成功！");
                Console.WriteLine($"📋 综合训练名称: {result.ComprehensiveTraining.Name}");
                Console.WriteLine($"🔧 原始examType: {result.ComprehensiveTraining.ExamType}");
                Console.WriteLine($"🎯 映射后comprehensiveTrainingType: {result.ComprehensiveTraining.ComprehensiveTrainingType}");
                Console.WriteLine($"📊 总分: {result.ComprehensiveTraining.TotalScore}");
                Console.WriteLine($"📁 模块数量: {result.ComprehensiveTraining.Modules.Count}");
                
                if (result.ComprehensiveTraining.Modules.Count > 0)
                {
                    var firstModule = result.ComprehensiveTraining.Modules[0];
                    Console.WriteLine($"📝 第一个模块: {firstModule.Name}");
                    Console.WriteLine($"❓ 题目数量: {firstModule.Questions.Count}");
                    
                    if (firstModule.Questions.Count > 0)
                    {
                        var firstQuestion = firstModule.Questions[0];
                        Console.WriteLine($"🎯 第一道题目: {firstQuestion.Title}");
                        Console.WriteLine($"💻 C#题目类型: {firstQuestion.CsharpQuestionType}");
                        Console.WriteLine($"🔢 C#直接分数: {firstQuestion.CsharpDirectScore}");
                        Console.WriteLine($"⚙️ 操作点数量: {firstQuestion.OperationPoints.Count}");
                    }
                }

                Console.WriteLine($"📋 导出元数据 - 版本: {result.Metadata.ExportVersion}");
                Console.WriteLine($"👤 导出者: {result.Metadata.ExportedBy}");
                
                return true;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ JSON解析异常: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 其他异常: {ex.Message}");
                return false;
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 开始测试EW导入综合实训修复...");
            Console.WriteLine();
            
            bool success = await TestDeserializationAsync();
            
            Console.WriteLine();
            if (success)
            {
                Console.WriteLine("🎉 测试通过！修复后的DTO可以正确处理ExamLab导出的JSON格式");
            }
            else
            {
                Console.WriteLine("💥 测试失败！需要进一步修复");
            }
        }
    }
}
