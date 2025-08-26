using System.Diagnostics;
using System.Text.Json;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace BenchSuite.Services;

/// <summary>
/// AI逻辑性判分服务实现
/// </summary>
public class AILogicalScoringService : IAILogicalScoringService
{
    private readonly ChatClient _chatClient;
    private readonly AIServiceConfiguration _configuration;

    public AILogicalScoringService(AIServiceConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (!_configuration.IsValid())
        {
            throw new ArgumentException("AI服务配置无效", nameof(configuration));
        }

        if (!_configuration.IsValidEndpoint())
        {
            throw new ArgumentException("API端点地址无效", nameof(configuration));
        }

        // 创建OpenAI客户端，支持自定义端点
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = new Uri(_configuration.ApiEndpoint)
        };

        ApiKeyCredential credential = new(_configuration.ApiKey);
        OpenAIClient openAIClient = new(credential, clientOptions);
        _chatClient = openAIClient.GetChatClient(_configuration.ModelName);
    }

    /// <summary>
    /// 使用AI进行逻辑性判分，返回结构化的JSON格式结果
    /// </summary>
    public async Task<AILogicalScoringResult> ScoreLogicalReasoningAsync(
        string sourceCode,
        string problemDescription,
        string? expectedOutput = null,
        List<string>? testCases = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        AILogicalScoringResult result = new();

        try
        {
            // 构建提示词
            string prompt = BuildLogicalAnalysisPrompt(sourceCode, problemDescription, expectedOutput, testCases);

            // 配置聊天完成选项
            ChatCompletionOptions options = new()
            {
                MaxOutputTokenCount = _configuration.MaxTokens,
                Temperature = (float)_configuration.Temperature
            };

            // 如果启用结构化输出，添加JSON Schema
            if (_configuration.EnableStructuredOutput)
            {
                options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "csharp_logical_analysis",
                    jsonSchema: BinaryData.FromBytes(AIJsonSchemas.CSharpLogicalAnalysisSchema.ToCharArray().Select(c => (byte)c).ToArray()),
                    jsonSchemaIsStrict: true);
            }

            // 调用OpenAI API
            List<ChatMessage> messages = [new UserChatMessage(prompt)];
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            string responseContent = completion.Content[0].Text;
            result.RawJsonResponse = responseContent;

            // 解析结构化JSON响应
            CSharpLogicalAnalysisResponse? analysisResponse = JsonSerializer.Deserialize<CSharpLogicalAnalysisResponse>(responseContent);

            if (analysisResponse != null)
            {
                // 转换为结果格式
                result.Steps = analysisResponse.Steps.Select(step => new ReasoningStep
                {
                    Explanation = step.Explanation,
                    Output = step.Output
                }).ToList();

                result.FinalAnswer = analysisResponse.FinalAnswer;
                result.LogicalScore = analysisResponse.LogicalScore;
                result.Details = GenerateDetailedAnalysis(analysisResponse);
                result.IsSuccess = true;
            }
            else
            {
                result.ErrorMessage = "无法解析AI响应的JSON格式";
                result.IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"AI逻辑性判分失败: {ex.Message}";
            result.IsSuccess = false;
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 使用AI分析代码的逻辑错误
    /// </summary>
    public async Task<AILogicalAnalysisResult> AnalyzeLogicalErrorsAsync(string sourceCode, string problemDescription)
    {
        AILogicalAnalysisResult result = new();

        try
        {
            // 调用逻辑性判分方法
            AILogicalScoringResult scoringResult = await ScoreLogicalReasoningAsync(sourceCode, problemDescription);

            if (scoringResult.IsSuccess)
            {
                // 解析原始JSON响应以获取详细的错误信息
                CSharpLogicalAnalysisResponse? analysisResponse = JsonSerializer.Deserialize<CSharpLogicalAnalysisResponse>(scoringResult.RawJsonResponse);

                if (analysisResponse != null)
                {
                    result.LogicalErrors = analysisResponse.LogicalErrors.Select(error => new AIDetectedLogicalError
                    {
                        ErrorType = error.ErrorType,
                        Description = error.Description,
                        Severity = ParseSeverity(error.Severity),
                        LineNumber = error.LineNumber,
                        FixSuggestion = error.FixSuggestion
                    }).ToList();

                    result.ImprovementSuggestions = analysisResponse.ImprovementSuggestions;
                    result.QualityAssessment = new CodeQualityAssessment
                    {
                        OverallScore = analysisResponse.LogicalScore,
                        LogicalClarity = CalculateLogicalClarity(analysisResponse),
                        AlgorithmEfficiency = CalculateAlgorithmEfficiency(analysisResponse),
                        CodeStructure = CalculateCodeStructure(analysisResponse),
                        ErrorHandling = CalculateErrorHandling(analysisResponse)
                    };

                    result.IsSuccess = true;
                }
                else
                {
                    result.ErrorMessage = "无法解析AI分析结果";
                    result.IsSuccess = false;
                }
            }
            else
            {
                result.ErrorMessage = scoringResult.ErrorMessage;
                result.IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"AI逻辑错误分析失败: {ex.Message}";
            result.IsSuccess = false;
        }

        return result;
    }

    /// <summary>
    /// 验证AI服务连接状态
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            // 发送一个简单的测试请求
            List<ChatMessage> testMessages = [new UserChatMessage("测试连接")];
            ChatCompletionOptions options = new()
            {
                MaxOutputTokenCount = 1000,
                Temperature = 0.1f
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(testMessages, options);
            return !string.IsNullOrEmpty(completion.Content[0].Text);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取AI服务配置信息
    /// </summary>
    public AIServiceConfiguration GetConfiguration()
    {
        return _configuration;
    }

    /// <summary>
    /// 构建逻辑分析提示词
    /// </summary>
    private static string BuildLogicalAnalysisPrompt(string sourceCode, string problemDescription, string? expectedOutput, List<string>? testCases)
    {
        string expectedOutputSection = string.IsNullOrEmpty(expectedOutput)
            ? ""
            : $"\n期望输出：\n{expectedOutput}\n";

        if (testCases?.Count > 0)
        {
            expectedOutputSection += $"\n测试用例：\n{string.Join("\n", testCases.Select((tc, i) => $"{i + 1}. {tc}"))}\n";
        }

        return AIPromptTemplates.CSharpLogicalAnalysisPrompt
            .Replace("{problemDescription}", problemDescription)
            .Replace("{sourceCode}", sourceCode)
            .Replace("{expectedOutputSection}", expectedOutputSection);
    }

    /// <summary>
    /// 生成详细分析报告
    /// </summary>
    private static string GenerateDetailedAnalysis(CSharpLogicalAnalysisResponse response)
    {
        List<string> details = [];

        details.Add($"逻辑性评分: {response.LogicalScore}/100");
        details.Add($"分析步骤数: {response.Steps.Count}");
        details.Add($"检测到的逻辑错误数: {response.LogicalErrors.Count}");

        if (response.LogicalErrors.Count > 0)
        {
            details.Add("\n主要逻辑错误:");
            foreach (StructuredLogicalError error in response.LogicalErrors.Take(3))
            {
                details.Add($"- {error.ErrorType}: {error.Description}");
            }
        }

        if (response.ImprovementSuggestions.Count > 0)
        {
            details.Add("\n改进建议:");
            foreach (string suggestion in response.ImprovementSuggestions.Take(3))
            {
                details.Add($"- {suggestion}");
            }
        }

        return string.Join("\n", details);
    }

    /// <summary>
    /// 解析严重程度
    /// </summary>
    private static LogicalErrorSeverity ParseSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "minor" => LogicalErrorSeverity.Minor,
            "moderate" => LogicalErrorSeverity.Moderate,
            "severe" => LogicalErrorSeverity.Severe,
            "critical" => LogicalErrorSeverity.Critical,
            _ => LogicalErrorSeverity.Moderate
        };
    }

    /// <summary>
    /// 计算逻辑清晰度评分
    /// </summary>
    private static decimal CalculateLogicalClarity(CSharpLogicalAnalysisResponse response)
    {
        // 基于逻辑错误数量和严重程度计算
        int severityPenalty = response.LogicalErrors.Sum(e => e.Severity.ToLower() switch
        {
            "critical" => 30,
            "severe" => 20,
            "moderate" => 10,
            "minor" => 5,
            _ => 10
        });

        return Math.Max(0, response.LogicalScore - severityPenalty);
    }

    /// <summary>
    /// 计算算法效率评分
    /// </summary>
    private static decimal CalculateAlgorithmEfficiency(CSharpLogicalAnalysisResponse response)
    {
        // 简化实现，基于整体评分
        return Math.Max(0, response.LogicalScore - 10);
    }

    /// <summary>
    /// 计算代码结构评分
    /// </summary>
    private static decimal CalculateCodeStructure(CSharpLogicalAnalysisResponse response)
    {
        // 简化实现，基于整体评分
        return Math.Max(0, response.LogicalScore - 5);
    }

    /// <summary>
    /// 计算错误处理评分
    /// </summary>
    private static decimal CalculateErrorHandling(CSharpLogicalAnalysisResponse response)
    {
        // 基于是否有异常处理相关的改进建议
        bool hasErrorHandlingSuggestions = response.ImprovementSuggestions
            .Any(s => s.Contains("异常") || s.Contains("错误处理") || s.Contains("try") || s.Contains("catch"));

        return hasErrorHandlingSuggestions ? Math.Max(0, response.LogicalScore - 20) : response.LogicalScore;
    }
}
