using BenchSuite.Interfaces;
using BenchSuite.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace BenchSuite.Services;

/// <summary>
/// C#代码评分服务实现
/// </summary>
public class CSharpScoringService : ICSharpScoringService
{
    private readonly CSharpScoringConfiguration _defaultConfiguration;
    private readonly string _tempDirectory;

    public CSharpScoringService()
    {
        _defaultConfiguration = new CSharpScoringConfiguration();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "CSharpScoring", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// 对C#代码进行综合评分
    /// </summary>
    public async Task<CSharpScoringResult> ScoreCSharpCodeAsync(string sourceCode, string programInput, string expectedOutput, CSharpScoringConfiguration? configuration = null)
    {
        CSharpScoringConfiguration config = configuration ?? _defaultConfiguration;
        CSharpScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false,
            ScoringStage = "开始评分"
        };

        try
        {
            // 步骤1：编译代码
            result.ScoringStage = "编译代码";
            result.CompilationResult = await CompileCodeAsync(sourceCode);

            if (!result.CompilationResult.IsSuccess)
            {
                result.ErrorMessage = "代码编译失败";
                result.FinalScore = 0;
                result.IsSuccess = true; // 评分过程成功，但得分为0
                return result;
            }

            // 步骤2：执行代码
            result.ScoringStage = "执行代码";
            result.ExecutionResult = await ExecuteCodeAsync(
                result.CompilationResult.ExecutablePath!,
                programInput,
                config.ExecutionTimeoutSeconds);

            if (!result.ExecutionResult.IsSuccess)
            {
                result.ErrorMessage = "代码执行失败";
                result.FinalScore = 0;
                result.IsSuccess = true;
                return result;
            }

            // 步骤3：检查输出
            result.ScoringStage = "检查输出";
            result.OutputMatches = CompareOutput(
                result.ExecutionResult.Output,
                expectedOutput,
                config.IgnoreCase,
                config.IgnoreWhitespace);

            if (!result.OutputMatches)
            {
                result.ErrorMessage = "程序输出与期望输出不匹配";
                result.FinalScore = 0;
                result.IsSuccess = true;
                return result;
            }

            // 步骤4：AI代码质量评分
            if (config.EnableAIScoring)
            {
                result.ScoringStage = "AI质量评分";
                result.AIScoringResult = await ScoreCodeQualityAsync(
                    sourceCode,
                    result.ExecutionResult.Output,
                    expectedOutput,
                    config);

                if (result.AIScoringResult.IsSuccess)
                {
                    result.FinalScore = result.AIScoringResult.TotalScore;
                }
                else
                {
                    result.ErrorMessage = "AI评分失败，使用默认分数";
                    result.FinalScore = config.MaxAIScore * 0.8m; // 给予80%的默认分数
                }
            }
            else
            {
                result.FinalScore = config.MaxAIScore; // 如果不启用AI评分，给满分
            }

            result.TotalScore = config.MaxAIScore;
            result.AchievedScore = result.FinalScore;
            result.IsSuccess = true;
            result.ScoringStage = "评分完成";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"评分过程中发生错误: {ex.Message}";
            result.IsSuccess = false;
        }
        finally
        {
            result.EndTime = DateTime.Now;
            // 清理临时文件
            CleanupTempFiles();
        }

        return result;
    }

    /// <summary>
    /// 编译C#代码
    /// </summary>
    public async Task<CompilationResult> CompileCodeAsync(string sourceCode)
    {
        return await Task.Run(() =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            CompilationResult result = new();

            try
            {
                // 创建语法树
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

                // 获取基础引用
                List<MetadataReference> references = new()
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location)
                };

                // 创建编译选项
                CSharpCompilationOptions compilationOptions = new(
                    OutputKind.ConsoleApplication,
                    optimizationLevel: OptimizationLevel.Release);

                // 创建编译
                string assemblyName = $"Program_{Guid.NewGuid():N}";
                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    new[] { syntaxTree },
                    references,
                    compilationOptions);

                // 生成可执行文件
                string executablePath = Path.Combine(_tempDirectory, $"{assemblyName}.exe");
                EmitResult emitResult = compilation.Emit(executablePath);

                if (emitResult.Success)
                {
                    result.IsSuccess = true;
                    result.ExecutablePath = executablePath;
                }
                else
                {
                    result.IsSuccess = false;
                    foreach (Diagnostic diagnostic in emitResult.Diagnostics)
                    {
                        if (diagnostic.Severity == DiagnosticSeverity.Error)
                        {
                            result.Errors.Add(diagnostic.GetMessage());
                        }
                        else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                        {
                            result.Warnings.Add(diagnostic.GetMessage());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"编译过程中发生异常: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.CompilationTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        });
    }

    /// <summary>
    /// 执行编译后的程序
    /// </summary>
    public async Task<ExecutionResult> ExecuteCodeAsync(string executablePath, string input, int timeoutSeconds = 10)
    {
        ExecutionResult result = new();
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using Process process = new() { StartInfo = startInfo };
            process.Start();

            // 写入输入
            if (!string.IsNullOrEmpty(input))
            {
                await process.StandardInput.WriteAsync(input);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();
            }

            // 等待进程完成或超时
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                await process.WaitForExitAsync(cts.Token);
                result.ExitCode = process.ExitCode;
                result.Output = await process.StandardOutput.ReadToEndAsync();
                result.ErrorOutput = await process.StandardError.ReadToEndAsync();
                result.IsSuccess = process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                result.IsTimeout = true;
                result.IsSuccess = false;
                try
                {
                    process.Kill(true);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ExceptionMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 比较程序输出
    /// </summary>
    private static bool CompareOutput(string actualOutput, string expectedOutput, bool ignoreCase, bool ignoreWhitespace)
    {
        string actual = actualOutput ?? string.Empty;
        string expected = expectedOutput ?? string.Empty;

        if (ignoreWhitespace)
        {
            actual = actual.Trim();
            expected = expected.Trim();
        }

        StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(actual, expected, comparison);
    }

    /// <summary>
    /// 使用AI对代码质量进行评分
    /// </summary>
    public async Task<AIScoringResult> ScoreCodeQualityAsync(string sourceCode, string actualOutput, string expectedOutput, CSharpScoringConfiguration configuration)
    {
        AIScoringResult result = new();

        try
        {
            if (string.IsNullOrEmpty(configuration.OpenAIApiKey))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "OpenAI API密钥未配置";
                return result;
            }

            OpenAIClient client = new(configuration.OpenAIApiKey);

            string prompt = CreateAIScoringPrompt(sourceCode, actualOutput, expectedOutput);

            List<ChatMessage> messages = new()
            {
                new UserChatMessage(prompt)
            };

            ChatCompletionOptions options = new()
            {
                Temperature = 0.1f,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            ChatCompletion completion = await client.GetChatClient(configuration.OpenAIModel)
                .CompleteChatAsync(messages, options);

            string responseContent = completion.Content[0].Text;
            AIScoringResponse? aiResponse = JsonSerializer.Deserialize<AIScoringResponse>(responseContent);

            if (aiResponse != null)
            {
                result.TotalScore = Math.Min(aiResponse.Score, configuration.MaxAIScore);
                result.LogicScore = Math.Min(aiResponse.LogicScore, 10);
                result.RedundancyScore = Math.Min(aiResponse.RedundancyScore, 10);
                result.StructureScore = Math.Min(aiResponse.StructureScore, 5);
                result.EfficiencyScore = Math.Min(aiResponse.EfficiencyScore, 5);
                result.Issues = aiResponse.Issues ?? new List<string>();
                result.Suggestions = aiResponse.Suggestions ?? new List<string>();
                result.DetailedFeedback = aiResponse.DetailedFeedback ?? string.Empty;
                result.IsSuccess = true;
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorMessage = "无法解析AI响应";
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"AI评分失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 创建AI评分提示词
    /// </summary>
    private static string CreateAIScoringPrompt(string sourceCode, string actualOutput, string expectedOutput)
    {
        return $@"你是一个专业的C#代码评审专家。请对以下C#代码进行质量评分（满分30分）。

评分标准：
1. 代码逻辑性和正确性（10分）- 评估代码逻辑是否清晰、正确
2. 代码冗余检测（10分）- 重点检查重复代码、不必要的变量声明、冗余的逻辑等
3. 代码结构和可读性（5分）- 评估代码组织结构、命名规范、注释等
4. 代码效率（5分）- 评估算法效率、资源使用等

代码：
```csharp
{sourceCode}
```

程序输出：
实际输出：{actualOutput}
期望输出：{expectedOutput}

请严格按照以下JSON格式返回评分结果：
{{
  ""score"": 总分(0-30),
  ""logicScore"": 逻辑性得分(0-10),
  ""redundancyScore"": 冗余检测得分(0-10),
  ""structureScore"": 结构得分(0-5),
  ""efficiencyScore"": 效率得分(0-5),
  ""issues"": [""问题1"", ""问题2""],
  ""suggestions"": [""建议1"", ""建议2""],
  ""detailedFeedback"": ""详细反馈说明""
}}

注意：
- 发现严重代码冗余或逻辑问题时应大幅扣分
- 严重情况下可给0分或接近0分
- 请提供具体的问题位置和改进建议";
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    private void CleanupTempFiles()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    #region IScoringService 基础接口实现

    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        // 查找C#模块
        ExamModuleModel? csharpModule = examModel.Exam.Modules?.FirstOrDefault(m => m.Type == ModuleType.CSharp);

        if (csharpModule == null)
        {
            return new ScoringResult
            {
                IsSuccess = false,
                ErrorMessage = "试卷中未找到C#模块",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
        }

        // 读取源代码文件
        if (!File.Exists(filePath))
        {
            return new ScoringResult
            {
                IsSuccess = false,
                ErrorMessage = $"源代码文件不存在: {filePath}",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
        }

        string sourceCode = await File.ReadAllTextAsync(filePath);
        CSharpScoringConfiguration csharpConfig = configuration as CSharpScoringConfiguration ?? _defaultConfiguration;

        decimal totalScore = 0M;
        decimal achievedScore = 0M;
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            foreach (QuestionModel question in csharpModule.Questions)
            {
                if (!question.IsEnabled)
                {
                    continue;
                }

                totalScore += question.TotalScore;

                // 对每个C#题目进行评分
                CSharpScoringResult csharpResult = await ScoreCSharpCodeAsync(
                    sourceCode,
                    question.ProgramInput ?? string.Empty,
                    question.ExpectedOutput ?? string.Empty,
                    csharpConfig);

                if (csharpResult.IsSuccess)
                {
                    // 按题目分数比例计算得分
                    decimal questionAchievedScore = (csharpResult.FinalScore / csharpConfig.MaxAIScore) * question.TotalScore;
                    achievedScore += questionAchievedScore;

                    result.QuestionResults.Add(new QuestionScoreResult
                    {
                        QuestionId = question.Id,
                        QuestionTitle = question.Title,
                        TotalScore = question.TotalScore,
                        AchievedScore = questionAchievedScore,
                        IsCorrect = questionAchievedScore > 0
                    });
                }
                else
                {
                    result.QuestionResults.Add(new QuestionScoreResult
                    {
                        QuestionId = question.Id,
                        QuestionTitle = question.Title,
                        TotalScore = question.TotalScore,
                        AchievedScore = 0M,
                        IsCorrect = false
                    });
                }
            }

            result.TotalScore = totalScore;
            result.AchievedScore = achievedScore;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"C#代码评分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        return ScoreFileAsync(filePath, examModel, configuration).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 检查是否可以处理指定文件
    /// </summary>
    public bool CanProcessFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".cs" || extension == ".txt";
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return new[] { ".cs", ".txt" };
    }

    #endregion
}

/// <summary>
/// AI评分响应模型
/// </summary>
internal class AIScoringResponse
{
    public decimal Score { get; set; }
    public decimal LogicScore { get; set; }
    public decimal RedundancyScore { get; set; }
    public decimal StructureScore { get; set; }
    public decimal EfficiencyScore { get; set; }
    public List<string>? Issues { get; set; }
    public List<string>? Suggestions { get; set; }
    public string? DetailedFeedback { get; set; }
}
