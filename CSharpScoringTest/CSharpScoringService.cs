using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace CSharpScoringTest;

/// <summary>
/// C#代码评分服务实现
/// </summary>
public class CSharpScoringService
{
    private readonly string _tempDirectory;

    public CSharpScoringService()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "CSharpScoring");
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// 对C#代码进行综合评分
    /// </summary>
    public async Task<CSharpScoringResult> ScoreCSharpCodeAsync(string sourceCode, string programInput, string expectedOutput, CSharpScoringConfiguration configuration)
    {
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
                configuration.ExecutionTimeoutSeconds);

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
                configuration.IgnoreCase,
                configuration.IgnoreWhitespace);

            if (!result.OutputMatches)
            {
                result.ErrorMessage = "程序输出与期望输出不匹配";
                result.FinalScore = 0;
                result.IsSuccess = true;
                return result;
            }

            // 步骤4：AI代码质量评分
            if (configuration.EnableAIScoring)
            {
                result.ScoringStage = "AI质量评分";
                result.AIScoringResult = await ScoreCodeQualityAsync(
                    sourceCode,
                    result.ExecutionResult.Output,
                    expectedOutput,
                    configuration);

                if (result.AIScoringResult.IsSuccess)
                {
                    result.FinalScore = result.AIScoringResult.TotalScore;
                }
                else
                {
                    result.ErrorMessage = "AI评分失败，使用默认分数";
                    result.FinalScore = configuration.MaxAIScore * 0.8m; // 给予80%的默认分数
                }
            }
            else
            {
                result.FinalScore = configuration.MaxAIScore; // 如果不启用AI评分，给满分
            }

            result.TotalScore = configuration.MaxAIScore;
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
            // 不在这里清理临时文件，让每个测试都能使用独立的文件
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
                string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                List<MetadataReference> references = new()
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Console.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Private.CoreLib.dll"))
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

                // 编译到内存流
                using MemoryStream ms = new();
                EmitResult emitResult = compilation.Emit(ms);

                if (emitResult.Success)
                {
                    result.IsSuccess = true;
                    // 将编译结果保存到文件
                    string executablePath = Path.Combine(_tempDirectory, $"{assemblyName}.dll");
                    Directory.CreateDirectory(Path.GetDirectoryName(executablePath)!);

                    ms.Seek(0, SeekOrigin.Begin);
                    using FileStream fs = new(executablePath, FileMode.Create);
                    ms.CopyTo(fs);

                    // 创建runtimeconfig.json文件
                    string runtimeConfigPath = Path.Combine(_tempDirectory, $"{assemblyName}.runtimeconfig.json");
                    string runtimeConfig = @"{
  ""runtimeOptions"": {
    ""tfm"": ""net8.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""8.0.0""
    }
  }
}";
                    File.WriteAllText(runtimeConfigPath, runtimeConfig);

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
                FileName = "dotnet",
                Arguments = $"\"{executablePath}\"",
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
}
