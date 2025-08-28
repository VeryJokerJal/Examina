using BenchSuite.Interfaces;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// C#编程题打分服务 - 支持三种评分模式，包括AI逻辑性判分
/// </summary>
public class CSharpScoringService : ICSharpScoringService
{
    private readonly IAILogicalScoringService? _aiScoringService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="aiScoringService">AI逻辑性判分服务（可选）</param>
    public CSharpScoringService(IAILogicalScoringService? aiScoringService = null)
    {
        _aiScoringService = aiScoringService;
    }
    /// <summary>
    /// 对C#代码进行评分
    /// </summary>
    /// <param name="templateCode">模板代码（包含NotImplementedException的填空）</param>
    /// <param name="studentCode">学生提交的代码</param>
    /// <param name="expectedImplementations">期望的实现代码列表</param>
    /// <param name="mode">评分模式</param>
    /// <returns>评分结果</returns>
    public async Task<CSharpScoringResult> ScoreCodeAsync(string templateCode, string studentCode, List<string> expectedImplementations, CSharpScoringMode mode)
    {
        CSharpScoringResult result = new()
        {
            Mode = mode,
            StartTime = DateTime.Now
        };

        try
        {
            switch (mode)
            {
                case CSharpScoringMode.CodeCompletion:
                    await ScoreCodeCompletionAsync(result, templateCode, studentCode, expectedImplementations);
                    break;

                case CSharpScoringMode.Debugging:
                    await ScoreDebuggingAsync(result, templateCode, studentCode, expectedImplementations);
                    break;

                case CSharpScoringMode.Implementation:
                    await ScoreImplementationAsync(result, studentCode, expectedImplementations);
                    break;

                default:
                    result.IsSuccess = false;
                    result.ErrorMessage = $"不支持的评分模式: {mode}";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"评分过程中发生异常: {ex.Message}";
            result.Details = ex.StackTrace ?? "";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检测代码补全填空
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <param name="studentCode">学生代码</param>
    /// <param name="expectedImplementations">期望实现</param>
    /// <returns>填空检测结果</returns>
    public async Task<List<FillBlankResult>> DetectFillBlanksAsync(string templateCode, string studentCode, List<string> expectedImplementations)
    {
        return await Task.Run(() => CSharpCodeCompletionGrader.Grade(templateCode, expectedImplementations, studentCode));
    }

    /// <summary>
    /// 编译检查代码
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="references">引用程序集</param>
    /// <returns>编译结果</returns>
    public async Task<CompilationResult> CompileCodeAsync(string sourceCode, List<string>? references = null)
    {
        return await Task.Run(() => CSharpCompilationChecker.CompileCode(sourceCode, references));
    }

    /// <summary>
    /// 运行单元测试
    /// </summary>
    /// <param name="studentCode">学生代码</param>
    /// <param name="testCode">测试代码</param>
    /// <param name="references">引用程序集</param>
    /// <returns>测试结果</returns>
    public async Task<UnitTestResult> RunUnitTestsAsync(string studentCode, string testCode, List<string>? references = null)
    {
        return await CSharpUnitTestRunner.RunUnitTestsAsync(studentCode, testCode, references);
    }

    /// <summary>
    /// 调试纠错评分
    /// </summary>
    /// <param name="buggyCode">包含错误的代码</param>
    /// <param name="studentFixedCode">学生修复后的代码</param>
    /// <param name="expectedErrors">期望发现的错误列表</param>
    /// <param name="testCode">验证修复的测试代码</param>
    /// <returns>调试结果</returns>
    public async Task<DebuggingResult> DebugCodeAsync(string buggyCode, string studentFixedCode, List<string> expectedErrors, string? testCode = null)
    {
        return await CSharpDebuggingGrader.DebugCodeAsync(buggyCode, studentFixedCode, expectedErrors, testCode);
    }

    /// <summary>
    /// 代码补全模式评分
    /// </summary>
    private async Task ScoreCodeCompletionAsync(CSharpScoringResult result, string templateCode, string studentCode, List<string> expectedImplementations)
    {
        List<FillBlankResult> fillResults = await DetectFillBlanksAsync(templateCode, studentCode, expectedImplementations);
        
        result.FillBlankResults = fillResults;
        result.TotalScore = fillResults.Count;
        result.AchievedScore = fillResults.Count(f => f.Matched);
        result.IsSuccess = true;
        
        int totalBlanks = fillResults.Count;
        int correctBlanks = fillResults.Count(f => f.Matched);
        
        result.Details = $"代码补全评分完成。总填空数: {totalBlanks}, 正确填空数: {correctBlanks}";
        
        if (correctBlanks == 0 && totalBlanks > 0)
        {
            result.Details += "\n所有填空都不正确，请检查代码实现。";
        }
        else if (correctBlanks < totalBlanks)
        {
            List<int> incorrectBlanks = [.. fillResults
                .Where(f => !f.Matched)
                .Select(f => f.BlankIndex + 1)];
            result.Details += $"\n错误的填空位置: {string.Join(", ", incorrectBlanks)}";
        }
    }



    /// <summary>
    /// 调试纠错模式评分
    /// </summary>
    private async Task ScoreDebuggingAsync(CSharpScoringResult result, string buggyCode, string studentCode, List<string> expectedImplementations)
    {
        if (expectedImplementations.Count == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "调试纠错模式需要提供期望的错误列表";
            return;
        }

        // 第一个元素作为期望错误列表，第二个元素（如果有）作为测试代码
        List<string> expectedErrors = [.. expectedImplementations[0].Split('\n', StringSplitOptions.RemoveEmptyEntries)];
        string? testCode = expectedImplementations.Count > 1 ? expectedImplementations[1] : null;

        DebuggingResult debuggingResult = await DebugCodeAsync(buggyCode, studentCode, expectedErrors, testCode);

        result.DebuggingResult = debuggingResult;
        result.TotalScore = debuggingResult.TotalErrors;
        result.AchievedScore = debuggingResult.FixedErrors;
        result.IsSuccess = true;
        result.Details = debuggingResult.Details;

        // 同时进行编译检查
        CompilationResult compilationResult = await CompileCodeAsync(studentCode);
        result.CompilationResult = compilationResult;

        if (!compilationResult.IsSuccess)
        {
            result.Details += $"\n⚠️ 修复后的代码仍有编译错误: {compilationResult.Errors.Count} 个";
        }
    }

    /// <summary>
    /// 编写实现模式评分 - 包含AI逻辑性判分
    /// </summary>
    private async Task ScoreImplementationAsync(CSharpScoringResult result, string studentCode, List<string> expectedImplementations)
    {
        if (expectedImplementations.Count == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "编写实现模式需要提供测试代码";
            return;
        }

        // 1. 首先进行编译检查
        CompilationResult compilationResult = await CompileCodeAsync(studentCode);
        result.CompilationResult = compilationResult;

        if (!compilationResult.IsSuccess)
        {
            result.TotalScore = 1;
            result.AchievedScore = 0;
            result.IsSuccess = true;
            result.Details = $"编译失败。错误数量: {compilationResult.Errors.Count}";
            return;
        }

        // 2. 运行单元测试
        string testCode = expectedImplementations[0]; // 第一个元素作为测试代码
        UnitTestResult testResult = await RunUnitTestsAsync(studentCode, testCode);

        result.UnitTestResult = testResult;

        // 3. AI逻辑性判分（如果可用）
        AILogicalScoringResult? aiResult = null;
        if (_aiScoringService != null)
        {
            try
            {
                // 构建题目描述（从测试代码中提取或使用默认描述）
                string problemDescription = ExtractProblemDescriptionFromTest(testCode);

                aiResult = await _aiScoringService.ScoreLogicalReasoningAsync(
                    studentCode,
                    problemDescription,
                    null, // 期望输出
                    null  // 测试用例
                );

                result.AILogicalResult = aiResult;
            }
            catch (Exception ex)
            {
                // AI判分失败不影响整体评分，只记录错误
                result.Details += $"\nAI逻辑性判分失败: {ex.Message}";
            }
        }

        // 4. 计算综合评分
        CalculateComprehensiveScore(result, testResult, aiResult);

        // 5. 生成详细报告
        GenerateImplementationDetails(result, testResult, aiResult);
    }

    /// <summary>
    /// 从测试代码中提取题目描述
    /// </summary>
    private static string ExtractProblemDescriptionFromTest(string testCode)
    {
        // 简化实现：从测试代码的注释中提取描述，或使用默认描述
        if (testCode.Contains("//") && testCode.Contains("题目") || testCode.Contains("问题"))
        {
            string[] lines = testCode.Split('\n');
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("//") && (line.Contains("题目") || line.Contains("问题")))
                {
                    return line.Trim().TrimStart('/').Trim();
                }
            }
        }

        return "C#编程实现题：请根据测试用例实现相应的功能";
    }

    /// <summary>
    /// 计算综合评分（结合单元测试和AI逻辑性判分）
    /// </summary>
    private static void CalculateComprehensiveScore(CSharpScoringResult result, UnitTestResult testResult, AILogicalScoringResult? aiResult)
    {
        // 基础评分：编译 + 单元测试
        double baseScore = testResult.TotalTests + 1; // +1 for compilation
        double achievedBaseScore = testResult.PassedTests + 1; // +1 for successful compilation

        if (aiResult?.IsSuccess == true)
        {
            // 如果有AI判分结果，将其纳入综合评分
            // AI逻辑性评分权重为30%，单元测试权重为70%
            double testWeight = 0.7;
            double aiWeight = 0.3;

            double testScoreRatio = baseScore > 0 ? achievedBaseScore / baseScore : 0;
            double aiScoreRatio = aiResult.LogicalScore / 100.0;

            double comprehensiveRatio = (testScoreRatio * testWeight) + (aiScoreRatio * aiWeight);

            result.TotalScore = baseScore;
            result.AchievedScore = Math.Round(baseScore * comprehensiveRatio, 2);
        }
        else
        {
            // 没有AI判分结果，使用原有评分方式
            result.TotalScore = baseScore;
            result.AchievedScore = achievedBaseScore;
        }

        result.IsSuccess = true;
    }

    /// <summary>
    /// 生成实现模式的详细报告
    /// </summary>
    private static void GenerateImplementationDetails(CSharpScoringResult result, UnitTestResult testResult, AILogicalScoringResult? aiResult)
    {
        List<string> details = [];

        // 基础测试结果
        if (testResult.IsSuccess)
        {
            details.Add($"✅ 实现完成。编译成功，所有测试通过。总测试数: {testResult.TotalTests}, 执行耗时: {testResult.ExecutionTimeMs}ms");
        }
        else
        {
            details.Add($"⚠️ 实现部分完成。编译成功，但部分测试失败。通过: {testResult.PassedTests}/{testResult.TotalTests}");

            if (!string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                details.Add($"错误信息: {testResult.ErrorMessage}");
            }

            List<TestCaseResult> failedTests = [.. testResult.TestCaseResults.Where(t => !t.Passed).Take(3)];
            if (failedTests.Count > 0)
            {
                details.Add("失败的测试:");
                foreach (TestCaseResult failedTest in failedTests)
                {
                    details.Add($"  {failedTest.TestName}: {failedTest.ErrorMessage}");
                }
            }
        }

        // AI逻辑性判分结果
        if (aiResult?.IsSuccess == true)
        {
            details.Add($"\n🤖 AI逻辑性分析:");
            details.Add($"逻辑性评分: {aiResult.LogicalScore}/100");
            details.Add($"处理耗时: {aiResult.ProcessingTimeMs}ms");

            if (aiResult.Steps.Count > 0)
            {
                details.Add("主要分析步骤:");
                foreach (ReasoningStep step in aiResult.Steps.Take(3))
                {
                    details.Add($"  • {step.Explanation}");
                }
            }

            if (!string.IsNullOrEmpty(aiResult.FinalAnswer))
            {
                details.Add($"AI评估结论: {aiResult.FinalAnswer}");
            }
        }
        else if (aiResult != null && !aiResult.IsSuccess)
        {
            details.Add($"\n⚠️ AI逻辑性分析失败: {aiResult.ErrorMessage}");
        }

        result.Details = string.Join("\n", details);
    }

    #region IScoringService 基础接口实现

    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 验证文件是否可以处理
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"无法处理文件: {filePath}。请确保文件存在且为支持的C#源代码文件格式。";
                return result;
            }

            // 读取学生代码文件
            string studentCode = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(studentCode))
            {
                result.ErrorMessage = "学生代码文件为空";
                return result;
            }

            // 处理试卷中的所有C#题目
            List<KnowledgePointResult> allResults = [];
            double totalScore = 0;
            double achievedScore = 0;

            // 从所有模块中获取C#题目
            var csharpQuestions = examModel.Modules
                .Where(m => m.Type == ModuleType.CSharp)
                .SelectMany(m => m.Questions);

            foreach (QuestionModel question in csharpQuestions)
            {
                ScoringResult questionResult = await ScoreQuestionAsync(filePath, question, configuration);

                // 合并结果
                allResults.AddRange(questionResult.KnowledgePointResults);
                totalScore += questionResult.TotalScore;
                achievedScore += questionResult.AchievedScore;

                // 如果某个题目评分失败，记录但继续处理其他题目
                if (!questionResult.IsSuccess && string.IsNullOrEmpty(result.ErrorMessage))
                {
                    result.ErrorMessage = questionResult.ErrorMessage;
                }
            }

            // 设置最终结果
            result.KnowledgePointResults = allResults;
            result.TotalScore = totalScore;
            result.AchievedScore = achievedScore;
            result.IsSuccess = allResults.Count > 0; // 只要有结果就算成功

            if (allResults.Count == 0)
            {
                result.ErrorMessage = "试卷中没有找到C#编程题目";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"评分过程中发生异常: {ex.Message}";
            result.IsSuccess = false;
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        try
        {
            // 调用异步版本并等待结果
            return ScoreFileAsync(filePath, examModel, configuration).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return new ScoringResult
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                IsSuccess = false,
                ErrorMessage = $"同步评分失败: {ex.Message}",
                KnowledgePointResults = [],
                TotalScore = 0,
                AchievedScore = 0
            };
        }
    }

    public async Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 验证文件是否可以处理
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"无法处理文件: {filePath}";
                return result;
            }

            // 读取学生代码文件
            string studentCode = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(studentCode))
            {
                result.ErrorMessage = "学生代码文件为空";
                return result;
            }

            // 从题目中提取C#相关信息
            CSharpQuestionInfo? questionInfo = ExtractCSharpQuestionInfo(question);
            if (questionInfo == null)
            {
                result.ErrorMessage = "无法从题目中提取C#编程信息";
                return result;
            }

            // 根据题目类型调用相应的评分方法
            CSharpScoringResult csharpResult = await ScoreCodeAsync(
                questionInfo.TemplateCode,
                studentCode,
                questionInfo.ExpectedImplementations,
                questionInfo.ScoringMode);

            // 转换为ScoringResult
            result = ConvertToScoringResult(csharpResult, question);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"题目评分过程中发生异常: {ex.Message}";
            result.IsSuccess = false;
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    public bool CanProcessFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        // 检查文件是否存在
        if (!File.Exists(filePath))
            return false;

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return GetSupportedExtensions().Contains(extension);
    }

    public IEnumerable<string> GetSupportedExtensions()
    {
        return [".cs", ".txt", ".csharp"]; // 支持C#源代码文件和文本文件
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 从题目模型中提取C#编程题信息
    /// </summary>
    /// <param name="question">题目模型</param>
    /// <returns>C#题目信息</returns>
    private static CSharpQuestionInfo? ExtractCSharpQuestionInfo(QuestionModel question)
    {
        try
        {
            // 从题目的操作点中提取C#相关信息
            var csharpOperationPoints = question.OperationPoints
                .Where(op => op.ModuleType == ModuleType.CSharp)
                .ToList();

            if (csharpOperationPoints.Count == 0)
            {
                return null;
            }

            // 确定评分模式
            CSharpScoringMode scoringMode = DetermineScoringMode(question);

            // 提取模板代码
            string templateCode = ExtractTemplateCode(question, csharpOperationPoints);

            // 提取期望实现
            List<string> expectedImplementations = ExtractExpectedImplementations(question, csharpOperationPoints, scoringMode);

            return new CSharpQuestionInfo
            {
                ScoringMode = scoringMode,
                TemplateCode = templateCode,
                ExpectedImplementations = expectedImplementations,
                QuestionId = question.Id,
                QuestionTitle = question.Title
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 确定C#题目的评分模式
    /// </summary>
    /// <param name="question">题目模型</param>
    /// <returns>评分模式</returns>
    private static CSharpScoringMode DetermineScoringMode(QuestionModel question)
    {
        // 从题目类型或操作点中确定模式
        string questionType = question.QuestionType?.ToLowerInvariant() ?? "";

        return questionType switch
        {
            "codecompletion" => CSharpScoringMode.CodeCompletion,
            "debugging" => CSharpScoringMode.Debugging,
            "implementation" => CSharpScoringMode.Implementation,
            _ => CSharpScoringMode.CodeCompletion // 默认为代码补全模式
        };
    }

    /// <summary>
    /// 提取模板代码
    /// </summary>
    /// <param name="question">题目模型</param>
    /// <param name="operationPoints">操作点列表</param>
    /// <returns>模板代码</returns>
    private static string ExtractTemplateCode(QuestionModel question, List<OperationPointModel> operationPoints)
    {
        // 优先从操作点参数中查找模板代码
        foreach (var op in operationPoints)
        {
            var templateParam = op.Parameters?.FirstOrDefault(p =>
                p.Name.Equals("TemplateCode", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("Template", StringComparison.OrdinalIgnoreCase));

            if (templateParam != null && !string.IsNullOrWhiteSpace(templateParam.Value))
            {
                return templateParam.Value;
            }
        }

        // 如果没有找到，尝试从题目内容中提取
        if (!string.IsNullOrWhiteSpace(question.Content))
        {
            // 查找代码块标记
            var codeBlockMatch = System.Text.RegularExpressions.Regex.Match(
                question.Content,
                @"```(?:csharp|cs|c#)?\s*\n(.*?)\n```",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (codeBlockMatch.Success)
            {
                return codeBlockMatch.Groups[1].Value.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 提取期望实现
    /// </summary>
    /// <param name="question">题目模型</param>
    /// <param name="operationPoints">操作点列表</param>
    /// <param name="scoringMode">评分模式</param>
    /// <returns>期望实现列表</returns>
    private static List<string> ExtractExpectedImplementations(QuestionModel question, List<OperationPointModel> operationPoints, CSharpScoringMode scoringMode)
    {
        List<string> implementations = [];

        foreach (var op in operationPoints)
        {
            // 根据评分模式查找不同的参数
            string[] paramNames = scoringMode switch
            {
                CSharpScoringMode.CodeCompletion => ["ExpectedImplementation", "Expected", "Implementation"],
                CSharpScoringMode.Debugging => ["ExpectedErrors", "Errors", "BugList"],
                CSharpScoringMode.Implementation => ["TestCode", "Tests", "UnitTests"],
                _ => ["ExpectedImplementation", "Expected"]
            };

            foreach (string paramName in paramNames)
            {
                var param = op.Parameters?.FirstOrDefault(p =>
                    p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

                if (param != null && !string.IsNullOrWhiteSpace(param.Value))
                {
                    implementations.Add(param.Value);
                    break; // 找到一个就跳出内层循环
                }
            }
        }

        return implementations;
    }

    /// <summary>
    /// 将CSharpScoringResult转换为ScoringResult
    /// </summary>
    /// <param name="csharpResult">C#评分结果</param>
    /// <param name="question">题目模型</param>
    /// <returns>通用评分结果</returns>
    private static ScoringResult ConvertToScoringResult(CSharpScoringResult csharpResult, QuestionModel question)
    {
        var result = new ScoringResult
        {
            StartTime = csharpResult.StartTime,
            EndTime = csharpResult.EndTime,
            IsSuccess = csharpResult.IsSuccess,
            ErrorMessage = csharpResult.ErrorMessage,
            TotalScore = csharpResult.TotalScore,
            AchievedScore = csharpResult.AchievedScore,
            KnowledgePointResults = []
        };

        // 根据评分模式创建知识点结果
        switch (csharpResult.Mode)
        {
            case CSharpScoringMode.CodeCompletion:
                foreach (var fillResult in csharpResult.FillBlankResults)
                {
                    result.KnowledgePointResults.Add(new KnowledgePointResult
                    {
                        KnowledgePointType = "CodeCompletion",
                        IsCorrect = fillResult.Matched,
                        TotalScore = 1,
                        AchievedScore = fillResult.Matched ? 1 : 0,
                        Details = fillResult.Message,
                        ExpectedValue = fillResult.ExpectedText,
                        ActualValue = fillResult.StudentText,
                        Parameters = new Dictionary<string, string>
                        {
                            ["BlankIndex"] = fillResult.BlankIndex.ToString(),
                            ["Location"] = fillResult.Descriptor.LocationSummary
                        }
                    });
                }
                break;

            case CSharpScoringMode.Debugging:
                if (csharpResult.DebuggingResult != null)
                {
                    result.KnowledgePointResults.Add(new KnowledgePointResult
                    {
                        KnowledgePointType = "Debugging",
                        IsCorrect = csharpResult.DebuggingResult.IsSuccess,
                        TotalScore = csharpResult.DebuggingResult.TotalErrors,
                        AchievedScore = csharpResult.DebuggingResult.FixedErrors,
                        Details = csharpResult.Details,
                        Parameters = new Dictionary<string, string>
                        {
                            ["TotalErrors"] = csharpResult.DebuggingResult.TotalErrors.ToString(),
                            ["FixedErrors"] = csharpResult.DebuggingResult.FixedErrors.ToString(),
                            ["RemainingErrors"] = csharpResult.DebuggingResult.RemainingErrors.ToString()
                        }
                    });
                }
                break;

            case CSharpScoringMode.Implementation:
                result.KnowledgePointResults.Add(new KnowledgePointResult
                {
                    KnowledgePointType = "Implementation",
                    IsCorrect = csharpResult.IsSuccess && csharpResult.CompilationResult?.IsSuccess == true,
                    TotalScore = csharpResult.TotalScore,
                    AchievedScore = csharpResult.AchievedScore,
                    Details = csharpResult.Details,
                    Parameters = new Dictionary<string, string>
                    {
                        ["CompilationSuccess"] = (csharpResult.CompilationResult?.IsSuccess ?? false).ToString(),
                        ["TestsPassed"] = (csharpResult.UnitTestResult?.PassedTests ?? 0).ToString(),
                        ["TotalTests"] = (csharpResult.UnitTestResult?.TotalTests ?? 0).ToString()
                    }
                });
                break;
        }

        return result;
    }

    #endregion
}

/// <summary>
/// C#题目信息
/// </summary>
internal class CSharpQuestionInfo
{
    /// <summary>
    /// 评分模式
    /// </summary>
    public CSharpScoringMode ScoringMode { get; set; }

    /// <summary>
    /// 模板代码
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 期望实现列表
    /// </summary>
    public List<string> ExpectedImplementations { get; set; } = [];

    /// <summary>
    /// 题目ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    public string QuestionTitle { get; set; } = string.Empty;
}
