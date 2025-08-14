using BenchSuite.Console.Services;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Console;

/// <summary>
/// PowerPoint 演示文稿自动评分控制台应用程序
/// </summary>
internal class Program
{
    /// <summary>
    /// 程序入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    private static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== BenchSuite PowerPoint 自动评分系统 ===");
        System.Console.WriteLine();

        if (args.Length == 0)
        {
            args = new string[]
            {
                "C:\\Users\\Jal\\Downloads\\计算机应用基础考试 - 副本_20250813_035202.json",
                "C:\\Users\\Jal\\Downloads\\B套素材PPT2.pptx"
            };
        }

        try
        {
            // 检查是否为测试模式
            if (args.Length > 0 && args[0] == "--test-json")
            {
                if (args.Length < 2)
                {
                    System.Console.WriteLine("请提供要测试的 JSON 文件路径");
                    return;
                }

                await TestJsonParsing.TestParseJsonAsync(args[1]);
                return;
            }

            // 解析命令行参数
            (string examFilePath, string pptFilePath) = ParseCommandLineArguments(args);

            // 加载试卷模型
            ExamModel examModel = await LoadExamModelAsync(examFilePath);

            // 执行 PowerPoint 评分
            await ScorePowerPointFileAsync(pptFilePath, examModel);
        }
        catch (FileNotFoundException ex)
        {
            System.Console.WriteLine($"❌ 文件未找到: {ex.Message}");
            System.Console.WriteLine("请检查文件路径是否正确。");
            Environment.Exit(1);
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"❌ 参数错误: {ex.Message}");
            ShowUsage();
            Environment.Exit(1);
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"❌ 操作失败: {ex.Message}");
            Environment.Exit(1);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.WriteLine($"❌ 访问被拒绝: {ex.Message}");
            System.Console.WriteLine("请检查文件权限或确保文件未被其他程序占用。");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ 程序执行失败: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"内部错误: {ex.InnerException.Message}");
            }
            System.Console.WriteLine($"错误类型: {ex.GetType().Name}");
            System.Console.WriteLine("如果问题持续存在，请联系技术支持。");
            Environment.Exit(1);
        }

        System.Console.WriteLine();
        System.Console.WriteLine("按任意键退出...");
        _ = System.Console.ReadKey();
    }

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>试卷文件路径和PowerPoint文件路径</returns>
    private static (string examFilePath, string pptFilePath) ParseCommandLineArguments(string[] args)
    {
        string examFilePath;
        string pptFilePath;

        if (args.Length >= 2)
        {
            examFilePath = args[0];
            pptFilePath = args[1];
        }
        else
        {
            // 使用默认的测试数据
            examFilePath = Path.Combine("TestData", "sample-exam.json");

            System.Console.WriteLine("未指定命令行参数，使用默认配置:");
            System.Console.WriteLine($"试卷文件: {examFilePath}");
            System.Console.Write("请输入 PowerPoint 文件路径: ");

            string? inputPath = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException("必须指定 PowerPoint 文件路径");
            }

            pptFilePath = inputPath.Trim('"');
        }

        // 验证文件存在性
        if (!File.Exists(examFilePath))
        {
            throw new FileNotFoundException($"试卷文件不存在: {examFilePath}");
        }

        if (!File.Exists(pptFilePath))
        {
            throw new FileNotFoundException($"PowerPoint 文件不存在: {pptFilePath}");
        }

        System.Console.WriteLine($"✅ 试卷文件: {examFilePath}");
        System.Console.WriteLine($"✅ PowerPoint 文件: {pptFilePath}");
        System.Console.WriteLine();

        return (examFilePath, pptFilePath);
    }

    /// <summary>
    /// 加载试卷模型
    /// </summary>
    /// <param name="examFilePath">试卷文件路径</param>
    /// <returns>试卷模型</returns>
    private static async Task<ExamModel> LoadExamModelAsync(string examFilePath)
    {
        System.Console.WriteLine("📋 正在加载试卷模型...");

        ExamModelLoader.LoadResult loadResult = await ExamModelLoader.LoadAsync(examFilePath, verbose: true);

        if (!loadResult.IsSuccess)
        {
            throw new InvalidOperationException($"加载试卷模型失败: {loadResult.ErrorMessage}");
        }

        if (loadResult.ExamModel == null)
        {
            throw new InvalidOperationException("试卷模型为空");
        }

        // 验证试卷模型
        (bool isValid, string errorMessage) = ExamModelLoader.ValidateExamModel(loadResult.ExamModel, verbose: true);
        if (!isValid)
        {
            throw new InvalidOperationException($"试卷模型验证失败: {errorMessage}");
        }

        System.Console.WriteLine($"✅ 试卷模型加载成功: {loadResult.ExamModel.Name}");
        System.Console.WriteLine();

        return loadResult.ExamModel;
    }

    /// <summary>
    /// 执行 PowerPoint 文件评分
    /// </summary>
    /// <param name="pptFilePath">PowerPoint 文件路径</param>
    /// <param name="examModel">试卷模型</param>
    private static async Task ScorePowerPointFileAsync(string pptFilePath, ExamModel examModel)
    {
        System.Console.WriteLine("🎯 正在执行 PowerPoint 评分...");

        try
        {
            // 创建 PowerPoint 评分服务（使用模拟版本进行演示）
            MockPowerPointScoringService scoringService = new();

            // 配置评分选项
            ScoringConfiguration configuration = new()
            {
                EnablePartialScoring = true,
                ErrorTolerance = 0.1m,
                TimeoutSeconds = 60,
                EnableDetailedLogging = true
            };

            // 执行评分
            ScoringResult result = await scoringService.ScoreFileAsync(pptFilePath, examModel, configuration);

            // 显示评分结果
            DisplayScoringResult(result);
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            System.Console.WriteLine($"❌ PowerPoint COM 组件错误: {ex.Message}");
            System.Console.WriteLine("可能的原因:");
            System.Console.WriteLine("- PowerPoint 未安装或版本不兼容");
            System.Console.WriteLine("- PowerPoint 文件已损坏");
            System.Console.WriteLine("- 系统权限不足");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.WriteLine($"❌ 文件访问被拒绝: {ex.Message}");
            System.Console.WriteLine("请确保:");
            System.Console.WriteLine("- PowerPoint 文件未被其他程序打开");
            System.Console.WriteLine("- 具有读取文件的权限");
            throw;
        }
        catch (FileNotFoundException ex)
        {
            System.Console.WriteLine($"❌ PowerPoint 文件未找到: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ PowerPoint 评分失败: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"详细错误: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// 显示评分结果
    /// </summary>
    /// <param name="result">评分结果</param>
    private static void DisplayScoringResult(ScoringResult result)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("📊 === 评分结果 ===");
        System.Console.WriteLine();

        if (!result.IsSuccess)
        {
            System.Console.WriteLine($"❌ 评分失败: {result.ErrorMessage}");
            return;
        }

        // 显示总体结果
        System.Console.WriteLine($"📋 总分: {result.TotalScore:F1}");
        System.Console.WriteLine($"🎯 得分: {result.AchievedScore:F1}");
        System.Console.WriteLine($"📈 得分率: {result.ScoreRate:P2}");
        System.Console.WriteLine($"⏱️ 评分耗时: {(result.EndTime - result.StartTime).TotalSeconds:F2} 秒");
        System.Console.WriteLine();

        // 显示知识点详细结果
        System.Console.WriteLine("📝 知识点详细结果:");
        System.Console.WriteLine(new string('-', 80));

        int correctCount = 0;
        int totalCount = result.KnowledgePointResults.Count;

        foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
        {
            string status = kpResult.IsCorrect ? "✅" : "❌";
            string scoreInfo = $"{kpResult.AchievedScore:F1}/{kpResult.TotalScore:F1}";

            System.Console.WriteLine($"{status} {kpResult.KnowledgePointName} ({scoreInfo})");

            if (!string.IsNullOrEmpty(kpResult.Details))
            {
                System.Console.WriteLine($"   详情: {kpResult.Details}");
            }

            if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
            {
                System.Console.WriteLine($"   错误: {kpResult.ErrorMessage}");
            }

            if (kpResult.IsCorrect)
            {
                correctCount++;
            }

            System.Console.WriteLine();
        }

        System.Console.WriteLine(new string('-', 80));
        System.Console.WriteLine($"📊 知识点统计: {correctCount}/{totalCount} 正确");

        // 显示评分等级
        string grade = GetGrade(result.ScoreRate);
        System.Console.WriteLine($"🏆 评分等级: {grade}");
    }

    /// <summary>
    /// 根据得分率获取评分等级
    /// </summary>
    /// <param name="scoreRate">得分率</param>
    /// <returns>评分等级</returns>
    private static string GetGrade(decimal scoreRate)
    {
        return scoreRate switch
        {
            >= 0.9m => "优秀 (A)",
            >= 0.8m => "良好 (B)",
            >= 0.7m => "中等 (C)",
            >= 0.6m => "及格 (D)",
            _ => "不及格 (F)"
        };
    }

    /// <summary>
    /// 显示程序使用说明
    /// </summary>
    private static void ShowUsage()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("📖 使用说明:");
        System.Console.WriteLine("BenchSuite.Console.exe [试卷文件路径] [PowerPoint文件路径]");
        System.Console.WriteLine();
        System.Console.WriteLine("参数说明:");
        System.Console.WriteLine("  试卷文件路径    - 试卷模型文件路径 (支持 JSON 格式)");
        System.Console.WriteLine("  PowerPoint文件路径 - 要评分的 PowerPoint 文件路径 (.pptx)");
        System.Console.WriteLine();
        System.Console.WriteLine("示例:");
        System.Console.WriteLine("  BenchSuite.Console.exe TestData\\sample-exam.json C:\\MyPresentation.pptx");
        System.Console.WriteLine();
        System.Console.WriteLine("如果不提供参数，程序将使用默认的测试数据并提示输入 PowerPoint 文件路径。");
    }
}
