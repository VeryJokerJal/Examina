using BenchSuite.Console.Services;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Console;

/// <summary>
/// Windows 系统操作自动评分控制台应用程序
/// </summary>
public static class WindowsTestProgram
{
    /// <summary>
    /// Windows测试程序入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    public static async Task RunWindowsTestAsync(string[] args)
    {
        System.Console.WriteLine("=== BenchSuite Windows 自动评分系统 ===");
        System.Console.WriteLine();

        try
        {
            // 解析命令行参数
            (string examFilePath, string? basePath) = ParseCommandLineArguments(args);

            // 测试参数解析功能
            await TestParameterResolutionAsync();

            // 加载试卷模型
            ExamModel examModel = await LoadExamModelAsync(examFilePath);

            // 执行 Windows 稳定性测试（30次评分）
            await RunWindowsStabilityTestAsync(examModel, basePath);
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
            ShowWindowsUsage();
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
            System.Console.WriteLine("请检查系统权限或确保以管理员身份运行。");
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
    /// <returns>试卷文件路径和基础路径</returns>
    private static (string examFilePath, string? basePath) ParseCommandLineArguments(string[] args)
    {
        string examFilePath;
        string? basePath = null;

        if (args.Length >= 1)
        {
            examFilePath = args[0];

            // 解析可选的基础路径参数
            for (int i = 1; i < args.Length; i++)
            {
                if ((args[i] == "--base-path" || args[i] == "-bp") && i + 1 < args.Length)
                {
                    basePath = args[i + 1];
                    break;
                }
            }
        }
        else
        {
            // 使用默认的Windows测试数据
            examFilePath = Path.Combine("TestData", "windows-test-exam.json");

            System.Console.WriteLine("未指定命令行参数，使用默认配置:");
            System.Console.WriteLine($"试卷文件: {examFilePath}");
        }

        // 验证文件存在性
        if (!File.Exists(examFilePath))
        {
            throw new FileNotFoundException($"试卷文件不存在: {examFilePath}");
        }

        System.Console.WriteLine($"试卷文件: {examFilePath}");
        if (!string.IsNullOrEmpty(basePath))
        {
            System.Console.WriteLine($"基础路径: {basePath}");
        }
        System.Console.WriteLine();

        return (examFilePath, basePath);
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
    /// 运行Windows稳定性测试（30次评分）
    /// </summary>
    /// <param name="examModel">试卷模型</param>
    /// <param name="basePath">基础路径</param>
    private static async Task RunWindowsStabilityTestAsync(ExamModel examModel, string? basePath)
    {
        const int testRuns = 30;
        System.Console.WriteLine($"正在执行 Windows 稳定性测试（{testRuns}次评分）...");
        System.Console.WriteLine("注意：Windows测试不依赖特定文件，将检测系统当前状态");
        System.Console.WriteLine();

        List<ScoringResult> allResults = [];

        // 使用真实的Windows评分服务
        WindowsScoringService scoringService = new();

        // 设置基础路径（如果提供）
        if (!string.IsNullOrEmpty(basePath))
        {
            scoringService.SetBasePath(basePath);
            System.Console.WriteLine($"使用基础路径: {basePath}");
            System.Console.WriteLine();
        }

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
                // Windows评分不依赖特定文件，使用空字符串作为文件路径
                ScoringResult result = await scoringService.ScoreFileAsync("", examModel, configuration);
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
        System.Console.WriteLine("=== Windows 稳定性测试结果分析 ===");

        // 显示第一次评分的详细信息用于诊断
        if (allResults.Count > 0 && allResults[0].IsSuccess)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("第一次评分详细信息（用于诊断）:");
            System.Console.WriteLine(new string('-', 80));

            foreach (KnowledgePointResult kpResult in allResults[0].KnowledgePointResults.Take(10)) // 显示前10个
            {
                System.Console.WriteLine($"知识点: {kpResult.KnowledgePointType}");
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

            if (allResults[0].KnowledgePointResults.Count > 10)
            {
                System.Console.WriteLine($"... 还有 {allResults[0].KnowledgePointResults.Count - 10} 个知识点");
            }

            System.Console.WriteLine(new string('-', 80));
        }

        // 分析结果
        AnalyzeWindowsStabilityResults(allResults);
    }

    /// <summary>
    /// 分析Windows稳定性测试结果
    /// </summary>
    /// <param name="allResults">所有评分结果</param>
    private static void AnalyzeWindowsStabilityResults(List<ScoringResult> allResults)
    {
        List<ScoringResult> successfulResults = [.. allResults.Where(r => r.IsSuccess)];

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
            double avgScore = achievedScores.Average();
            double variance = achievedScores.Select(s => (decimal)Math.Pow((double)(s - avgScore), 2)).Average();
            double stdDev = (decimal)Math.Sqrt((double)variance);

            System.Console.WriteLine($"得分统计: 平均={avgScore:F2}, 标准差={stdDev:F2}");
        }

        System.Console.WriteLine();

        // 分析知识点稳定性
        AnalyzeWindowsKnowledgePointStability(successfulResults);

        // 分析性能
        AnalyzeWindowsPerformance(successfulResults);
    }

    /// <summary>
    /// 显示程序使用说明
    /// </summary>
    private static void ShowWindowsUsage()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Windows测试使用说明:");
        System.Console.WriteLine("BenchSuite.Console.exe windows [试卷文件路径] [选项]");
        System.Console.WriteLine();
        System.Console.WriteLine("参数说明:");
        System.Console.WriteLine("  试卷文件路径    - Windows测试试卷模型文件路径 (支持 JSON 格式)");
        System.Console.WriteLine();
        System.Console.WriteLine("选项:");
        System.Console.WriteLine("  --base-path, -bp <路径>  - 指定基础路径，用于解析相对路径");
        System.Console.WriteLine();
        System.Console.WriteLine("示例:");
        System.Console.WriteLine("  BenchSuite.Console.exe windows TestData\\windows-test-exam.json");
        System.Console.WriteLine("  BenchSuite.Console.exe windows exam.json --base-path \"C:\\TestEnvironment\"");
        System.Console.WriteLine("  BenchSuite.Console.exe windows exam.json -bp \"D:\\Projects\\Test\"");
        System.Console.WriteLine();
        System.Console.WriteLine("如果不提供参数，程序将使用默认的Windows测试数据。");
        System.Console.WriteLine("基础路径用于将EL导出的相对路径（如\\WINDOWS\\calc.exe）转换为绝对路径。");
        System.Console.WriteLine();
        System.Console.WriteLine("注意: 程序将自动执行30次评分以测试Windows系统操作检测的稳定性。");
    }

    /// <summary>
    /// 分析Windows知识点稳定性
    /// </summary>
    /// <param name="successfulResults">成功的评分结果</param>
    private static void AnalyzeWindowsKnowledgePointStability(List<ScoringResult> successfulResults)
    {
        System.Console.WriteLine("Windows知识点稳定性分析:");
        System.Console.WriteLine(new string('-', 120));

        // 收集所有知识点的结果
        Dictionary<string, List<KnowledgePointResult>> knowledgePointResults = [];

        foreach (ScoringResult result in successfulResults)
        {
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                string key = kpResult.KnowledgePointType ?? "Unknown";
                if (!knowledgePointResults.ContainsKey(key))
                {
                    knowledgePointResults[key] = [];
                }
                knowledgePointResults[key].Add(kpResult);
            }
        }

        // 按知识点类型分组分析
        var categoryGroups = new Dictionary<string, List<(string type, List<KnowledgePointResult> results)>>
        {
            ["文件操作"] = [],
            ["文件夹操作"] = [],
            ["文件属性操作"] = [],
            ["文件内容操作"] = [],
            ["系统操作"] = [],
            ["注册表操作"] = [],
            ["服务操作"] = [],
            ["进程操作"] = [],
            ["网络操作"] = [],
            ["压缩操作"] = []
        };

        // 分类知识点
        foreach (var kvp in knowledgePointResults)
        {
            string knowledgeType = kvp.Key;
            var results = kvp.Value;

            string category = GetKnowledgePointCategory(knowledgeType);
            if (categoryGroups.ContainsKey(category))
            {
                categoryGroups[category].Add((knowledgeType, results));
            }
        }

        // 分析每个类别
        List<(string name, int correctCount, int totalCount, List<decimal> scores)> unstableKnowledgePoints = [];

        foreach (var categoryGroup in categoryGroups)
        {
            string category = categoryGroup.Key;
            var knowledgePoints = categoryGroup.Value;

            if (knowledgePoints.Count == 0) continue;

            System.Console.WriteLine($"\n【{category}】");
            System.Console.WriteLine(new string('-', 80));

            foreach (var (knowledgeType, results) in knowledgePoints.OrderBy(x => x.type))
            {
                if (results.Count == 0) continue;

                int correctCount = results.Count(r => r.IsCorrect);
                int totalCount = results.Count;
                List<decimal> scores = results.Select(r => r.AchievedScore).ToList();

                bool isStable = scores.All(s => Math.Abs(s - scores[0]) < 0.01m);
                bool hasVariation = correctCount > 0 && correctCount < totalCount;

                string status;
                if (isStable)
                {
                    status = correctCount == totalCount ? "✓ 稳定(全对)" :
                            correctCount == 0 ? "✓ 稳定(全错)" : "✓ 稳定";
                }
                else
                {
                    status = "✗ 不稳定";
                    unstableKnowledgePoints.Add((knowledgeType, correctCount, totalCount, scores));
                }

                double avgScore = scores.Average();
                double minScore = scores.Min();
                double maxScore = scores.Max();

                System.Console.WriteLine($"  {knowledgeType,-25} {status,-12} {correctCount,2}/{totalCount,2} 正确  得分: {minScore:F1}-{maxScore:F1} (平均: {avgScore:F1})");

                // 显示详细信息（包括全错的情况）
                if (hasVariation || correctCount == 0)
                {
                    var scoreGroups = results.GroupBy(r => r.AchievedScore).OrderBy(g => g.Key);
                    string scoreDistribution = string.Join(", ", scoreGroups.Select(g => $"{g.Key:F1}分×{g.Count()}次"));
                    System.Console.WriteLine($"    {"",27} 得分分布: {scoreDistribution}");

                    // 显示错误信息（如果有）
                    var errorMessages = results.Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
                                              .Select(r => r.ErrorMessage)
                                              .Distinct()
                                              .Take(2)
                                              .ToList();
                    if (errorMessages.Count > 0)
                    {
                        foreach (string error in errorMessages)
                        {
                            string shortError = error!.Length > 80 ? error[..80] + "..." : error;
                            System.Console.WriteLine($"    {"",27} 错误: {shortError}");
                        }
                    }

                    // 显示详细信息（如果有）
                    var detailMessages = results.Where(r => !string.IsNullOrEmpty(r.Details))
                                               .Select(r => r.Details!)
                                               .Distinct()
                                               .Take(1)
                                               .ToList();
                    if (detailMessages.Count > 0)
                    {
                        foreach (string detail in detailMessages)
                        {
                            string shortDetail = detail.Length > 80 ? detail[..80] + "..." : detail;
                            System.Console.WriteLine($"    {"",27} 详情: {shortDetail}");
                        }
                    }
                }
            }
        }

        System.Console.WriteLine(new string('-', 120));

        // 总结不稳定的知识点
        if (unstableKnowledgePoints.Count > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"发现 {unstableKnowledgePoints.Count} 个不稳定的知识点:");

            foreach (var (name, correctCount, totalCount, scores) in unstableKnowledgePoints.OrderBy(x => (double)x.correctCount / x.totalCount))
            {
                double successRate = (decimal)correctCount / totalCount;
                double variance = scores.Select(s => (decimal)Math.Pow((double)(s - scores.Average()), 2)).Average();
                double stdDev = (decimal)Math.Sqrt((double)variance);

                System.Console.WriteLine($"  • {name}");
                System.Console.WriteLine($"    成功率: {successRate:P1} ({correctCount}/{totalCount})");
                System.Console.WriteLine($"    得分标准差: {stdDev:F2}");
            }
        }
        else
        {
            System.Console.WriteLine();
            System.Console.WriteLine("✓ 所有Windows知识点评分都很稳定！");
        }
    }

    /// <summary>
    /// 获取知识点类别
    /// </summary>
    /// <param name="knowledgeType">知识点类型</param>
    /// <returns>知识点类别</returns>
    private static string GetKnowledgePointCategory(string knowledgeType)
    {
        return knowledgeType switch
        {
            "CreateFile" or "DeleteFile" or "CopyFile" or "MoveFile" or "RenameFile" => "文件操作",
            "CreateFolder" or "DeleteFolder" or "CopyFolder" or "MoveFolder" or "RenameFolder" => "文件夹操作",
            "SetFileAttributes" or "SetFilePermissions" => "文件属性操作",
            "WriteTextToFile" or "AppendTextToFile" => "文件内容操作",
            "CreateShortcut" or "SetEnvironmentVariable" => "系统操作",
            "CreateRegistryKey" or "SetRegistryValue" or "DeleteRegistryKey" => "注册表操作",
            "StartService" or "StopService" => "服务操作",
            "StartProcess" or "KillProcess" => "进程操作",
            "PingHost" or "DownloadFile" => "网络操作",
            "CreateZipArchive" or "ExtractZipArchive" => "压缩操作",
            _ => "其他操作"
        };
    }

    /// <summary>
    /// 分析Windows性能
    /// </summary>
    /// <param name="successfulResults">成功的评分结果</param>
    private static void AnalyzeWindowsPerformance(List<ScoringResult> successfulResults)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Windows性能分析:");
        System.Console.WriteLine(new string('-', 80));

        // 计算评分耗时
        var durations = successfulResults.Select(r => (r.EndTime - r.StartTime).TotalSeconds).ToList();

        if (durations.Count > 0)
        {
            double avgDuration = durations.Average();
            double minDuration = durations.Min();
            double maxDuration = durations.Max();
            double variance = durations.Select(d => Math.Pow(d - avgDuration, 2)).Average();
            double stdDev = Math.Sqrt(variance);

            System.Console.WriteLine($"评分耗时统计:");
            System.Console.WriteLine($"  平均耗时: {avgDuration:F2} 秒");
            System.Console.WriteLine($"  最短耗时: {minDuration:F2} 秒");
            System.Console.WriteLine($"  最长耗时: {maxDuration:F2} 秒");
            System.Console.WriteLine($"  标准差: {stdDev:F2} 秒");

            // 性能等级评估
            string performanceGrade = avgDuration switch
            {
                < 1.0 => "优秀 (< 1秒)",
                < 3.0 => "良好 (1-3秒)",
                < 5.0 => "一般 (3-5秒)",
                < 10.0 => "较慢 (5-10秒)",
                _ => "缓慢 (> 10秒)"
            };

            System.Console.WriteLine($"  性能等级: {performanceGrade}");
        }

        // 分析知识点数量
        if (successfulResults.Count > 0)
        {
            var knowledgePointCounts = successfulResults.Select(r => r.KnowledgePointResults.Count).ToList();
            double avgKnowledgePoints = knowledgePointCounts.Average();
            int minKnowledgePoints = knowledgePointCounts.Min();
            int maxKnowledgePoints = knowledgePointCounts.Max();

            System.Console.WriteLine();
            System.Console.WriteLine($"知识点数量统计:");
            System.Console.WriteLine($"  平均知识点数: {avgKnowledgePoints:F1} 个");
            System.Console.WriteLine($"  最少知识点数: {minKnowledgePoints} 个");
            System.Console.WriteLine($"  最多知识点数: {maxKnowledgePoints} 个");

            if (avgKnowledgePoints > 0)
            {
                double avgTimePerKnowledgePoint = durations.Average() / avgKnowledgePoints;
                System.Console.WriteLine($"  平均每个知识点耗时: {avgTimePerKnowledgePoint:F3} 秒");
            }
        }

        System.Console.WriteLine(new string('-', 80));
    }

    /// <summary>
    /// 测试参数解析功能
    /// </summary>
    private static async Task TestParameterResolutionAsync()
    {
        System.Console.WriteLine("=== 测试参数解析功能 ===");
        System.Console.WriteLine();

        try
        {
            // 创建Windows评分服务
            WindowsScoringService service = new();

            // 模拟快捷创建操作点
            OperationPointModel quickCreateOp = new()
            {
                Id = "test-quick-create",
                Name = "快捷创建",
                Parameters = new List<ParameterModel>
                {
                    new() { Name = "TargetPath", Value = "\\New Text Document.txt" },
                    new() { Name = "ItemType", Value = "文件" }
                }
            };

            System.Console.WriteLine("测试快捷创建操作点参数解析:");
            System.Console.WriteLine($"  操作点名称: {quickCreateOp.Name}");
            System.Console.WriteLine("  原始参数:");
            foreach (var param in quickCreateOp.Parameters)
            {
                System.Console.WriteLine($"    {param.Name}: {param.Value}");
            }

            // 测试知识点类型映射
            List<OperationPointModel> testOperations = [quickCreateOp];
            List<KnowledgePointResult> results = await service.DetectKnowledgePointsAsync(testOperations);

            System.Console.WriteLine();
            System.Console.WriteLine("  检测结果:");
            foreach (var result in results)
            {
                System.Console.WriteLine($"    知识点类型: {result.KnowledgePointType}");
                System.Console.WriteLine($"    检测状态: {(result.IsCorrect ? "正确" : "错误")}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    System.Console.WriteLine($"    错误信息: {result.ErrorMessage}");
                }
                if (!string.IsNullOrEmpty(result.Details))
                {
                    System.Console.WriteLine($"    详细信息: {result.Details}");
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine("✓ 参数解析测试完成");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"✗ 参数解析测试失败: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"  内部错误: {ex.InnerException.Message}");
            }
        }

        System.Console.WriteLine();
    }
}
