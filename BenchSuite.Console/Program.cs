using BenchSuite.Console.Services;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Console;

/// <summary>
/// BenchSuite 自动评分控制台应用程序
/// </summary>
internal class Program
{
    /// <summary>
    /// 程序入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    private static async Task Main(string[] args)
    {
        // 检查是否是Windows测试模式
        if (args.Length > 0 && args[0].Equals("windows", StringComparison.OrdinalIgnoreCase))
        {
            // 移除第一个参数（"windows"），传递剩余参数给Windows测试程序
            string[] windowsArgs = args.Skip(1).ToArray();
            await WindowsTestProgram.RunWindowsTestAsync(windowsArgs);
            return;
        }

        System.Console.WriteLine("=== BenchSuite PowerPoint 自动评分系统 ===");
        System.Console.WriteLine();

        if (args.Length == 0)
        {
            args =
            [
                "C:\\Users\\Jal\\Downloads\\计算机应用基础考试 - 副本_20250813_035202.json",
                "C:\\Users\\Jal\\Downloads\\B套素材PPT2.pptx"
            ];
        }

        try
        {
            // 解析命令行参数
            (string examFilePath, string pptFilePath) = ParseCommandLineArguments(args);

            // 加载试卷模型
            ExamModel examModel = await LoadExamModelAsync(examFilePath);

            // 执行 PowerPoint 稳定性测试（30次评分）
            await RunStabilityTestAsync(pptFilePath, examModel);
        }
        catch (FileNotFoundException ex)
        {
            System.Console.WriteLine($"文件未找到: {ex.Message}");
            System.Console.WriteLine("请检查文件路径是否正确。");
            Environment.Exit(1);
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"参数错误: {ex.Message}");
            ShowUsage();
            Environment.Exit(1);
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"操作失败: {ex.Message}");
            Environment.Exit(1);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.WriteLine($"访问被拒绝: {ex.Message}");
            System.Console.WriteLine("请检查文件权限或确保文件未被其他程序占用。");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"程序执行失败: {ex.Message}");
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

        System.Console.WriteLine($"试卷文件: {examFilePath}");
        System.Console.WriteLine($"PowerPoint 文件: {pptFilePath}");
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
        System.Console.WriteLine("正在加载试卷模型...");

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

        System.Console.WriteLine($"试卷模型加载成功: {loadResult.ExamModel.Name}");
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
        System.Console.WriteLine("正在执行 PowerPoint 评分...");

        try
        {
            // 创建 PowerPoint 评分服务
            PowerPointScoringService scoringService = new();

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
            System.Console.WriteLine($"PowerPoint COM 组件错误: {ex.Message}");
            System.Console.WriteLine("可能的原因:");
            System.Console.WriteLine("- PowerPoint 未安装或版本不兼容");
            System.Console.WriteLine("- PowerPoint 文件已损坏");
            System.Console.WriteLine("- 系统权限不足");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.WriteLine($"文件访问被拒绝: {ex.Message}");
            System.Console.WriteLine("请确保:");
            System.Console.WriteLine("- PowerPoint 文件未被其他程序打开");
            System.Console.WriteLine("- 具有读取文件的权限");
            throw;
        }
        catch (FileNotFoundException ex)
        {
            System.Console.WriteLine($"PowerPoint 文件未找到: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"PowerPoint 评分失败: {ex.Message}");
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
        System.Console.WriteLine("=== 评分结果 ===");
        System.Console.WriteLine();

        if (!result.IsSuccess)
        {
            System.Console.WriteLine($"评分失败: {result.ErrorMessage}");
            return;
        }

        // 显示总体结果
        System.Console.WriteLine($"总分: {result.TotalScore:F1}");
        System.Console.WriteLine($"得分: {result.AchievedScore:F1}");
        System.Console.WriteLine($"得分率: {result.ScoreRate:P2}");
        System.Console.WriteLine($"评分耗时: {(result.EndTime - result.StartTime).TotalSeconds:F2} 秒");
        System.Console.WriteLine();

        // 显示知识点详细结果
        System.Console.WriteLine("知识点详细结果:");
        System.Console.WriteLine(new string('-', 80));

        int correctCount = 0;
        int totalCount = result.KnowledgePointResults.Count;

        foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
        {
            string scoreInfo = $"{kpResult.AchievedScore:F1}/{kpResult.TotalScore:F1}";

            System.Console.WriteLine($"{kpResult.QuestionId} {kpResult.KnowledgePointName} ({scoreInfo})");

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
        System.Console.WriteLine($"知识点统计: {correctCount}/{totalCount} 正确");

        // 显示评分等级
        string grade = GetGrade(result.ScoreRate);
        System.Console.WriteLine($"评分等级: {grade}");
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
    /// 运行稳定性测试（30次评分）
    /// </summary>
    /// <param name="pptFilePath">PowerPoint 文件路径</param>
    /// <param name="examModel">试卷模型</param>
    private static async Task RunStabilityTestAsync(string pptFilePath, ExamModel examModel)
    {
        const int testRuns = 30;
        System.Console.WriteLine($"正在执行 PowerPoint 稳定性测试（{testRuns}次评分）...");
        System.Console.WriteLine();

        List<ScoringResult> allResults = [];

        // 使用真实的PowerPoint评分服务
        PowerPointScoringService scoringService = new();

        // 配置评分选项
        ScoringConfiguration configuration = new()
        {
            EnablePartialScoring = true,
            ErrorTolerance = 0.1m,
            TimeoutSeconds = 60,
            EnableDetailedLogging = true
        };

        // 执行30次评分
        for (int i = 1; i <= testRuns; i++)
        {
            System.Console.Write($"第 {i:D2} 次评分... ");

            try
            {
                ScoringResult result = await scoringService.ScoreFileAsync(pptFilePath, examModel, configuration);
                allResults.Add(result);

                if (result.IsSuccess)
                {
                    System.Console.WriteLine($"完成 - 得分: {result.AchievedScore:F1}/{result.TotalScore:F1} ({result.ScoreRate:P1})");
                }
                else
                {
                    System.Console.WriteLine($"失败 - {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"异常 - {ex.Message}");

                // 创建失败结果记录
                ScoringResult failedResult = new()
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    KnowledgePointResults = []
                };
                allResults.Add(failedResult);
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine("=== 稳定性测试结果分析 ===");

        // 显示第一次评分的详细信息用于诊断
        if (allResults.Count > 0 && allResults[0].IsSuccess)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("第一次评分详细信息（用于诊断）:");
            System.Console.WriteLine(new string('-', 80));

            foreach (KnowledgePointResult kpResult in allResults[0].KnowledgePointResults.Take(5)) // 只显示前5个
            {
                System.Console.WriteLine($"知识点: {kpResult.KnowledgePointName}");
                System.Console.WriteLine($"  得分: {kpResult.AchievedScore}/{kpResult.TotalScore}");
                System.Console.WriteLine($"  是否正确: {kpResult.IsCorrect}");

                if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
                {
                    System.Console.WriteLine($"  错误: {kpResult.ErrorMessage}");
                }

                if (!string.IsNullOrEmpty(kpResult.Details))
                {
                    string shortDetails = kpResult.Details.Length > 150 ? kpResult.Details[..150] + "..." : kpResult.Details;
                    System.Console.WriteLine($"  详情: {shortDetails}");
                }

                System.Console.WriteLine();
            }

            if (allResults[0].KnowledgePointResults.Count > 5)
            {
                System.Console.WriteLine($"... 还有 {allResults[0].KnowledgePointResults.Count - 5} 个知识点");
            }

            System.Console.WriteLine(new string('-', 80));
        }

        // 分析结果
        AnalyzeStabilityResults(allResults);
    }

    /// <summary>
    /// 分析稳定性测试结果
    /// </summary>
    /// <param name="allResults">所有评分结果</param>
    private static void AnalyzeStabilityResults(List<ScoringResult> allResults)
    {
        List<ScoringResult> successfulResults = allResults.Where(r => r.IsSuccess).ToList();

        System.Console.WriteLine($"成功评分次数: {successfulResults.Count}/{allResults.Count}");

        if (successfulResults.Count == 0)
        {
            System.Console.WriteLine("没有成功的评分结果，无法进行稳定性分析。");
            return;
        }

        // 分析总分稳定性
        List<decimal> totalScores = successfulResults.Select(r => r.TotalScore).ToList();
        List<decimal> achievedScores = successfulResults.Select(r => r.AchievedScore).ToList();

        bool totalScoreStable = totalScores.All(s => Math.Abs(s - totalScores[0]) < 0.01m);
        bool achievedScoreStable = achievedScores.All(s => Math.Abs(s - achievedScores[0]) < 0.01m);

        System.Console.WriteLine($"总分稳定性: {(totalScoreStable ? "✓ 稳定" : "✗ 不稳定")} (范围: {totalScores.Min():F1} - {totalScores.Max():F1})");
        System.Console.WriteLine($"得分稳定性: {(achievedScoreStable ? "✓ 稳定" : "✗ 不稳定")} (范围: {achievedScores.Min():F1} - {achievedScores.Max():F1})");

        if (!achievedScoreStable)
        {
            decimal avgScore = achievedScores.Average();
            decimal variance = achievedScores.Select(s => (decimal)Math.Pow((double)(s - avgScore), 2)).Average();
            decimal stdDev = (decimal)Math.Sqrt((double)variance);

            System.Console.WriteLine($"得分统计: 平均={avgScore:F2}, 标准差={stdDev:F2}");
        }

        System.Console.WriteLine();

        // 分析知识点稳定性
        AnalyzeKnowledgePointStability(successfulResults);
    }

    /// <summary>
    /// 分析知识点稳定性
    /// </summary>
    /// <param name="successfulResults">成功的评分结果</param>
    private static void AnalyzeKnowledgePointStability(List<ScoringResult> successfulResults)
    {
        System.Console.WriteLine("知识点稳定性分析:");
        System.Console.WriteLine(new string('-', 100));

        // 收集所有知识点的结果
        Dictionary<string, List<KnowledgePointResult>> knowledgePointResults = [];

        foreach (ScoringResult result in successfulResults)
        {
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                string key = $"{kpResult.KnowledgePointName}";
                if (!knowledgePointResults.ContainsKey(key))
                {
                    knowledgePointResults[key] = [];
                }
                knowledgePointResults[key].Add(kpResult);
            }
        }

        // 分析每个知识点的稳定性
        List<(string name, int correctCount, int totalCount, List<decimal> scores)> unstableKnowledgePoints = [];

        foreach (KeyValuePair<string, List<KnowledgePointResult>> kvp in knowledgePointResults)
        {
            string knowledgePointName = kvp.Key;
            List<KnowledgePointResult> results = kvp.Value;

            if (results.Count == 0)
            {
                continue;
            }

            int correctCount = results.Count(r => r.IsCorrect);
            int totalCount = results.Count;
            List<decimal> scores = results.Select(r => r.AchievedScore).ToList();

            bool isStable = scores.All(s => Math.Abs(s - scores[0]) < 0.01m);
            bool hasVariation = correctCount > 0 && correctCount < totalCount; // 既有正确也有错误

            string status;
            if (isStable)
            {
                status = correctCount == totalCount ? "✓ 稳定(全对)" :
                        correctCount == 0 ? "✓ 稳定(全错)" : "✓ 稳定";
            }
            else
            {
                status = "✗ 不稳定";
                unstableKnowledgePoints.Add((knowledgePointName, correctCount, totalCount, scores));
            }

            decimal avgScore = scores.Average();
            decimal minScore = scores.Min();
            decimal maxScore = scores.Max();

            System.Console.WriteLine($"{knowledgePointName,-40} {status,-12} {correctCount,2}/{totalCount,2} 正确  得分范围: {minScore:F1}-{maxScore:F1} (平均: {avgScore:F1})");

            // 显示详细信息（包括全错的情况）
            if (hasVariation || correctCount == 0)
            {
                IOrderedEnumerable<IGrouping<decimal, KnowledgePointResult>> scoreGroups = results.GroupBy(r => r.AchievedScore).OrderBy(g => g.Key);
                string scoreDistribution = string.Join(", ", scoreGroups.Select(g => $"{g.Key:F1}分×{g.Count()}次"));
                System.Console.WriteLine($"{"",42} 得分分布: {scoreDistribution}");

                // 显示错误信息（如果有）
                List<string?> errorMessages = results.Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
                                          .Select(r => r.ErrorMessage)
                                          .Distinct()
                                          .ToList();
                if (errorMessages.Count > 0)
                {
                    System.Console.WriteLine($"{"",42} 错误信息: {string.Join("; ", errorMessages)}");
                }

                // 显示详细信息（如果有）
                List<string> detailMessages = results.Where(r => !string.IsNullOrEmpty(r.Details))
                                           .Select(r => r.Details!)
                                           .Distinct()
                                           .Take(2) // 只显示前2个不同的详情
                                           .ToList();
                if (detailMessages.Count > 0)
                {
                    foreach (string detail in detailMessages)
                    {
                        // 截断过长的详情信息
                        string shortDetail = detail.Length > 100 ? detail[..100] + "..." : detail;
                        System.Console.WriteLine($"{"",42} 详情: {shortDetail}");
                    }
                }
            }
        }

        System.Console.WriteLine(new string('-', 100));

        // 总结不稳定的知识点
        if (unstableKnowledgePoints.Count > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"发现 {unstableKnowledgePoints.Count} 个不稳定的知识点:");

            foreach ((string name, int correctCount, int totalCount, List<decimal> scores) in unstableKnowledgePoints.OrderBy(x => (double)x.correctCount / x.totalCount))
            {
                decimal successRate = (decimal)correctCount / totalCount;
                decimal variance = scores.Select(s => (decimal)Math.Pow((double)(s - scores.Average()), 2)).Average();
                decimal stdDev = (decimal)Math.Sqrt((double)variance);

                System.Console.WriteLine($"  • {name}");
                System.Console.WriteLine($"    成功率: {successRate:P1} ({correctCount}/{totalCount})");
                System.Console.WriteLine($"    得分标准差: {stdDev:F2}");
            }
        }
        else
        {
            System.Console.WriteLine();
            System.Console.WriteLine("✓ 所有知识点评分都很稳定！");
        }
    }

    /// <summary>
    /// 显示程序使用说明
    /// </summary>
    private static void ShowUsage()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("使用说明:");
        System.Console.WriteLine("BenchSuite.Console.exe [模式] [参数...]");
        System.Console.WriteLine();
        System.Console.WriteLine("支持的模式:");
        System.Console.WriteLine("  1. PowerPoint模式 (默认):");
        System.Console.WriteLine("     BenchSuite.Console.exe [试卷文件路径] [PowerPoint文件路径]");
        System.Console.WriteLine("  2. Windows模式:");
        System.Console.WriteLine("     BenchSuite.Console.exe windows [试卷文件路径] [选项]");
        System.Console.WriteLine();
        System.Console.WriteLine("参数说明:");
        System.Console.WriteLine("  试卷文件路径    - 试卷模型文件路径 (支持 JSON 格式)");
        System.Console.WriteLine("  PowerPoint文件路径 - 要评分的 PowerPoint 文件路径 (.pptx)");
        System.Console.WriteLine();
        System.Console.WriteLine("Windows模式选项:");
        System.Console.WriteLine("  --base-path, -bp <路径>  - 指定基础路径，用于解析相对路径");
        System.Console.WriteLine();
        System.Console.WriteLine("示例:");
        System.Console.WriteLine("  BenchSuite.Console.exe TestData\\sample-exam.json C:\\MyPresentation.pptx");
        System.Console.WriteLine("  BenchSuite.Console.exe windows TestData\\windows-test-exam.json");
        System.Console.WriteLine("  BenchSuite.Console.exe windows exam.json --base-path \"C:\\TestEnvironment\"");
        System.Console.WriteLine();
        System.Console.WriteLine("如果不提供参数，程序将使用默认的测试数据。");
        System.Console.WriteLine();
        System.Console.WriteLine("注意: 程序将自动执行30次评分以测试稳定性。");
    }
}
