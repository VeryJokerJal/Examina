using System.Text.Json;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Console;

/// <summary>
/// PowerPoint评分控制台应用程序
/// </summary>
internal class Program
{
    /// <summary>
    /// 程序入口点
    /// </summary>
    /// <param name="args">命令行参数：[PPT文件路径] [ExamModel JSON文件路径] 或 [--test-csharp]</param>
    private static async Task<int> Main(string[] args)
    {
        try
        {
            // 检查是否是C#评分测试
            if (args.Length == 1 && args[0] == "--test-csharp")
            {
                await CSharpScoringTest.RunTestAsync();
                return 0;
            }

            //args = args.Length == 0 ? new string[] { "C:\\Users\\Jal\\Downloads\\B套素材PPT2.pptx", "D:\\Users\\Jal\\source\\repos\\Examina\\BenchSuite.Console\\TestData\\sample-exam.json" } : args;

#if DEBUG
            args = args.Length == 0 ? new string[] { "C:\\Users\\Jal\\Downloads\\B套素材PPT2.pptx", "C:\\Users\\Jal\\xwechat_files\\wxid_i7zuqi6yhnjn22_b6ff\\msg\\file\\2025-08\\计算机应用基础考试_20250813_220218.json" } : args;
#endif

            // 显示程序信息
            System.Console.WriteLine("=== PowerPoint评分系统 ===");
            System.Console.WriteLine("版本: 1.0.0");
            System.Console.WriteLine("描述: 基于ExamModel对PowerPoint文件进行自动评分");
            System.Console.WriteLine();

            // 验证命令行参数
            if (args.Length != 2)
            {
                ShowUsage();
                return 1;
            }

            string pptFilePath = args[0];
            string examModelJsonPath = args[1];

            // 验证文件路径
            if (!ValidateFilePaths(pptFilePath, examModelJsonPath))
            {
                return 1;
            }

            // 读取并解析ExamModel
            ExamModel? examModel = await LoadExamModelAsync(examModelJsonPath);
            if (examModel == null)
            {
                return 1;
            }

            // 执行评分
            ScoringResult result = await PerformScoringAsync(pptFilePath, examModel);

            // 输出结果
            DisplayScoringResult(result);

            return result.IsSuccess ? 0 : 1;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"程序执行过程中发生未处理的异常: {ex.Message}");
            System.Console.WriteLine($"详细信息: {ex}");
            return 1;
        }
    }

    /// <summary>
    /// 显示程序使用说明
    /// </summary>
    private static void ShowUsage()
    {
        System.Console.WriteLine("使用方法:");
        System.Console.WriteLine("  BenchSuite.Console.exe <PPT文件路径> <ExamModel JSON文件路径>");
        System.Console.WriteLine("  BenchSuite.Console.exe --test-csharp");
        System.Console.WriteLine();
        System.Console.WriteLine("参数说明:");
        System.Console.WriteLine("  PPT文件路径        - 要评分的PowerPoint文件路径 (.ppt 或 .pptx)");
        System.Console.WriteLine("  ExamModel JSON文件路径 - 包含试卷模型的JSON文件路径");
        System.Console.WriteLine("  --test-csharp      - 运行C#代码评分系统测试");
        System.Console.WriteLine();
        System.Console.WriteLine("示例:");
        System.Console.WriteLine("  BenchSuite.Console.exe \"C:\\test\\sample.pptx\" \"C:\\test\\exam.json\"");
        System.Console.WriteLine("  BenchSuite.Console.exe --test-csharp");
    }

    /// <summary>
    /// 验证文件路径
    /// </summary>
    private static bool ValidateFilePaths(string pptFilePath, string examModelJsonPath)
    {
        // 验证PPT文件
        if (!File.Exists(pptFilePath))
        {
            System.Console.WriteLine($"错误: PPT文件不存在: {pptFilePath}");
            return false;
        }

        string pptExtension = Path.GetExtension(pptFilePath).ToLowerInvariant();
        if (pptExtension is not ".ppt" and not ".pptx")
        {
            System.Console.WriteLine($"错误: 不支持的PPT文件格式: {pptExtension}");
            System.Console.WriteLine("支持的格式: .ppt, .pptx");
            return false;
        }

        // 验证JSON文件
        if (!File.Exists(examModelJsonPath))
        {
            System.Console.WriteLine($"错误: ExamModel JSON文件不存在: {examModelJsonPath}");
            return false;
        }

        string jsonExtension = Path.GetExtension(examModelJsonPath).ToLowerInvariant();
        if (jsonExtension != ".json")
        {
            System.Console.WriteLine($"错误: ExamModel文件必须是JSON格式: {jsonExtension}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 加载ExamModel从JSON文件
    /// </summary>
    private static async Task<ExamModel?> LoadExamModelAsync(string jsonFilePath)
    {
        try
        {
            System.Console.WriteLine($"正在读取ExamModel文件: {jsonFilePath}");

            string jsonContent = await File.ReadAllTextAsync(jsonFilePath);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                System.Console.WriteLine("错误: ExamModel JSON文件为空");
                return null;
            }

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // 添加自定义转换器
            options.Converters.Add(new BenchSuite.Converters.ModuleTypeJsonConverter());
            options.Converters.Add(new BenchSuite.Converters.ParameterTypeJsonConverter());

            // 直接反序列化为新的 ExamModel（结构对齐 ExamExportDto：包含 exam 与 metadata）
            ExamModel? examModel = JsonSerializer.Deserialize<ExamModel>(jsonContent, options);
            if (examModel == null)
            {
                System.Console.WriteLine("错误: 无法解析ExamModel JSON文件");
                return null;
            }

            if (!ValidateExamModel(examModel))
            {
                return null;
            }

            System.Console.WriteLine($"成功加载ExamModel: {examModel.Exam.Name}");
            System.Console.WriteLine($"包含模块数量: {examModel.Exam.Modules.Count}");

            return examModel;
        }
        catch (JsonException ex)
        {
            System.Console.WriteLine($"错误: JSON格式无效: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"错误: 读取ExamModel文件失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 验证ExamModel的有效性
    /// </summary>
    private static bool ValidateExamModel(ExamModel examModel)
    {
        if (examModel.Exam == null)
        {
            System.Console.WriteLine("错误: ExamModel缺少 exam 节点");
            return false;
        }

        if (string.IsNullOrWhiteSpace(examModel.Exam.Id))
        {
            System.Console.WriteLine("错误: ExamModel缺少ID");
            return false;
        }

        if (string.IsNullOrWhiteSpace(examModel.Exam.Name))
        {
            System.Console.WriteLine("错误: ExamModel缺少名称");
            return false;
        }

        if (examModel.Exam.Modules == null || examModel.Exam.Modules.Count == 0)
        {
            System.Console.WriteLine("错误: ExamModel没有包含任何模块");
            return false;
        }

        // 查找PowerPoint模块
        ExamModuleModel? pptModule = examModel.Exam.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
        if (pptModule == null)
        {
            System.Console.WriteLine("错误: ExamModel中未找到PowerPoint模块");
            return false;
        }

        if (pptModule.Questions == null || pptModule.Questions.Count == 0)
        {
            System.Console.WriteLine("错误: PowerPoint模块没有包含任何题目");
            return false;
        }

        // 验证是否有操作点
        int totalOperationPoints = pptModule.Questions.Sum(q => q.OperationPoints?.Count ?? 0);
        if (totalOperationPoints == 0)
        {
            System.Console.WriteLine("错误: PowerPoint模块没有包含任何操作点");
            return false;
        }

        System.Console.WriteLine($"PowerPoint模块验证通过: {pptModule.Name}");
        System.Console.WriteLine($"题目数量: {pptModule.Questions.Count}");
        System.Console.WriteLine($"操作点数量: {totalOperationPoints}");

        return true;
    }

    /// <summary>
    /// 执行PowerPoint评分
    /// </summary>
    private static async Task<ScoringResult> PerformScoringAsync(string pptFilePath, ExamModel examModel)
    {
        try
        {
            System.Console.WriteLine();
            System.Console.WriteLine("=== 开始评分 ===");
            System.Console.WriteLine($"PPT文件: {pptFilePath}");
            System.Console.WriteLine($"试卷: {examModel.Exam.Name}");
            System.Console.WriteLine();

            PowerPointScoringService scoringService = new();

            ScoringConfiguration configuration = new()
            {
                EnablePartialScoring = true,
                ErrorTolerance = 0.1m,
                TimeoutSeconds = 60,
                EnableDetailedLogging = true
            };

            System.Console.WriteLine("正在执行评分，请稍候...");

            ScoringResult result = await scoringService.ScoreFileAsync(pptFilePath, examModel, configuration);

            System.Console.WriteLine("评分完成!");
            System.Console.WriteLine();

            return result;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"评分过程中发生错误: {ex.Message}");

            return new ScoringResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 显示评分结果
    /// </summary>
    private static void DisplayScoringResult(ScoringResult result)
    {
        System.Console.WriteLine("=== 评分结果 ===");

        if (!result.IsSuccess)
        {
            System.Console.WriteLine("评分失败");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                System.Console.WriteLine($"错误信息: {result.ErrorMessage}");
            }
            return;
        }

        System.Console.WriteLine("评分成功");
        System.Console.WriteLine();

        // 基本信息
        System.Console.WriteLine("总体结果:");
        System.Console.WriteLine($"   总分: {result.TotalScore:F1}");
        System.Console.WriteLine($"   得分: {result.AchievedScore:F1}");
        System.Console.WriteLine($"   得分率: {result.ScoreRate:P2}");
        System.Console.WriteLine($"   耗时: {result.ElapsedMilliseconds}ms");
        System.Console.WriteLine();

        // 详细的题目结果
        if (result.QuestionResults != null && result.QuestionResults.Count > 0)
        {
            System.Console.WriteLine("详细检测结果:");
            System.Console.WriteLine();

            int correctCount = 0;
            int totalCount = result.QuestionResults.Count;

            foreach (QuestionScoreResult questionResult in result.QuestionResults)
            {
                string status = questionResult.IsCorrect ? "正确" : "-";
                string name = !string.IsNullOrEmpty(questionResult.QuestionTitle)
                    ? questionResult.QuestionTitle
                    : $"题目 {questionResult.QuestionId}";

                System.Console.WriteLine($"   {status} {name}");
                System.Console.WriteLine($"      分数: {questionResult.AchievedScore:F1}/{questionResult.TotalScore:F1}");

                // 显示该题目下的操作点详情
                List<KnowledgePointResult> questionKnowledgePoints = result.KnowledgePointResults
                    .Where(kp => kp.QuestionId == questionResult.QuestionId)
                    .ToList();

                foreach (KnowledgePointResult kpResult in questionKnowledgePoints)
                {
                    string kpStatus = kpResult.IsCorrect ? "正确" : "-";
                    string kpName = !string.IsNullOrEmpty(kpResult.KnowledgePointName)
                        ? kpResult.KnowledgePointName
                        : kpResult.KnowledgePointType;

                    System.Console.WriteLine($"        {kpStatus} {kpName}");

                    if (!string.IsNullOrEmpty(kpResult.Details))
                    {
                        System.Console.WriteLine($"          详情: {kpResult.Details}");
                    }

                    if (!string.IsNullOrEmpty(kpResult.ExpectedValue) && !string.IsNullOrEmpty(kpResult.ActualValue))
                    {
                        System.Console.WriteLine($"          期望值: {kpResult.ExpectedValue}");
                        System.Console.WriteLine($"          实际值: {kpResult.ActualValue}");
                    }

                    if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
                    {
                        System.Console.WriteLine($"          错误: {kpResult.ErrorMessage}");
                    }
                }

                if (questionResult.IsCorrect)
                {
                    correctCount++;
                }

                System.Console.WriteLine();
            }

            System.Console.WriteLine($"检测统计:");
            System.Console.WriteLine($"   正确题目: {correctCount}/{totalCount}");
            System.Console.WriteLine($"   正确率: {(double)correctCount / totalCount:P2}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("=== 评分完成 ===");
    }
}
