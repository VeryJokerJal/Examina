using BenchSuite.Models;
using System.Text;

namespace BenchSuite.Services;

/// <summary>
/// AI扣分评估服务 - 生成详细的扣分评估结果
/// </summary>
public static class AIDeductionAssessmentService
{
    /// <summary>
    /// 扣分评估结果
    /// </summary>
    public class DeductionAssessmentResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// AI扣分评估响应
        /// </summary>
        public AIDeductionAssessmentResponse? AssessmentResponse { get; set; }

        /// <summary>
        /// 生成的CSharpScoringResult列表
        /// </summary>
        public List<CSharpScoringResult> ScoringResults { get; set; } = [];

        /// <summary>
        /// 评估耗时（毫秒）
        /// </summary>
        public long AssessmentTimeMs { get; set; }

        /// <summary>
        /// 总扣分数
        /// </summary>
        public double TotalDeduction { get; set; }

        /// <summary>
        /// 扣分点数量
        /// </summary>
        public int DeductionPointCount { get; set; }
    }

    /// <summary>
    /// 对项目代码进行扣分评估
    /// </summary>
    /// <param name="projectFilePath">项目文件路径</param>
    /// <param name="assessmentOptions">评估选项</param>
    /// <returns>扣分评估结果</returns>
    public static async Task<DeductionAssessmentResult> AssessProjectDeductionsAsync(
        string projectFilePath,
        DeductionAssessmentOptions? assessmentOptions = null)
    {
        DateTime startTime = DateTime.Now;
        DeductionAssessmentResult result = new();

        try
        {
            assessmentOptions ??= new DeductionAssessmentOptions();

            // 读取项目代码
            ProjectFileReaderService.ProjectCodeResult projectCode = 
                await ProjectFileReaderService.ReadProjectCodeAsync(projectFilePath);

            if (!projectCode.IsSuccess)
            {
                result.ErrorMessage = $"读取项目代码失败: {projectCode.ErrorMessage}";
                return result;
            }

            // 执行AI扣分评估
            AIDeductionAssessmentResponse? assessmentResponse = 
                await PerformAIDeductionAssessmentAsync(projectCode, assessmentOptions);

            if (assessmentResponse == null || !assessmentResponse.IsSuccess)
            {
                result.ErrorMessage = assessmentResponse?.ErrorMessage ?? "AI扣分评估失败";
                return result;
            }

            // 转换为CSharpScoringResult列表
            List<CSharpScoringResult> scoringResults = 
                ConvertToScoringResults(assessmentResponse.DeductionPoints, projectCode.ProjectName);

            result.IsSuccess = true;
            result.AssessmentResponse = assessmentResponse;
            result.ScoringResults = scoringResults;
            result.TotalDeduction = assessmentResponse.TotalDeduction;
            result.DeductionPointCount = assessmentResponse.DeductionPoints.Count;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"扣分评估过程中发生异常: {ex.Message}";
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.AssessmentTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 对单个代码文件进行扣分评估
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="fileName">文件名</param>
    /// <param name="assessmentOptions">评估选项</param>
    /// <returns>扣分评估结果</returns>
    public static async Task<DeductionAssessmentResult> AssessSingleCodeDeductionsAsync(
        string sourceCode,
        string fileName = "Code.cs",
        DeductionAssessmentOptions? assessmentOptions = null)
    {
        DateTime startTime = DateTime.Now;
        DeductionAssessmentResult result = new();

        try
        {
            assessmentOptions ??= new DeductionAssessmentOptions();

            // 创建模拟的项目代码结果
            ProjectFileReaderService.ProjectCodeResult projectCode = new()
            {
                IsSuccess = true,
                ProjectName = Path.GetFileNameWithoutExtension(fileName),
                TargetFramework = "net9.0",
                CombinedSourceCode = sourceCode,
                Statistics = new ProjectFileReaderService.CodeStatistics
                {
                    TotalFiles = 1,
                    TotalLines = sourceCode.Split('\n').Length,
                    TotalCharacters = sourceCode.Length
                }
            };

            // 执行AI扣分评估
            AIDeductionAssessmentResponse? assessmentResponse = 
                await PerformAIDeductionAssessmentAsync(projectCode, assessmentOptions);

            if (assessmentResponse == null || !assessmentResponse.IsSuccess)
            {
                result.ErrorMessage = assessmentResponse?.ErrorMessage ?? "AI扣分评估失败";
                return result;
            }

            // 转换为CSharpScoringResult列表
            List<CSharpScoringResult> scoringResults = 
                ConvertToScoringResults(assessmentResponse.DeductionPoints, projectCode.ProjectName);

            result.IsSuccess = true;
            result.AssessmentResponse = assessmentResponse;
            result.ScoringResults = scoringResults;
            result.TotalDeduction = assessmentResponse.TotalDeduction;
            result.DeductionPointCount = assessmentResponse.DeductionPoints.Count;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"扣分评估过程中发生异常: {ex.Message}";
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.AssessmentTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 执行AI扣分评估
    /// </summary>
    /// <param name="projectCode">项目代码</param>
    /// <param name="options">评估选项</param>
    /// <returns>AI扣分评估响应</returns>
    private static async Task<AIDeductionAssessmentResponse?> PerformAIDeductionAssessmentAsync(
        ProjectFileReaderService.ProjectCodeResult projectCode,
        DeductionAssessmentOptions options)
    {
        try
        {
            // 这里应该调用实际的AI服务
            // 目前返回模拟结果用于测试
            await Task.Delay(200); // 模拟AI调用延迟

            return new AIDeductionAssessmentResponse
            {
                IsSuccess = true,
                DeductionPoints = GenerateMockDeductionPoints(projectCode),
                TotalDeduction = 15.5,
                AssessmentSummary = $"代码评估完成。发现 {GenerateMockDeductionPoints(projectCode).Count} 个问题点，总扣分 15.5 分。",
                OverallQualityGrade = "B",
                IssueCategoryStats = new Dictionary<string, int>
                {
                    ["logic_error"] = 1,
                    ["performance_issue"] = 2,
                    ["code_style"] = 1
                }
            };
        }
        catch (Exception ex)
        {
            return new AIDeductionAssessmentResponse
            {
                IsSuccess = false,
                ErrorMessage = $"AI扣分评估失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 生成模拟扣分点（用于测试）
    /// </summary>
    /// <param name="projectCode">项目代码</param>
    /// <returns>扣分点列表</returns>
    private static List<DeductionPoint> GenerateMockDeductionPoints(ProjectFileReaderService.ProjectCodeResult projectCode)
    {
        List<DeductionPoint> deductionPoints = [];

        // 模拟一些常见的扣分点
        if (projectCode.CombinedSourceCode.Contains("string.Concat") || 
            projectCode.CombinedSourceCode.Contains("+=") && projectCode.CombinedSourceCode.Contains("string"))
        {
            deductionPoints.Add(new DeductionPoint
            {
                DeductionId = "PERF_001",
                IssueType = "字符串拼接性能问题",
                IssueCategory = "performance_issue",
                Severity = "medium",
                DeductionScore = 5.0,
                Description = "使用字符串拼接操作可能导致性能问题",
                LocationInfo = new LocationInfo
                {
                    FileName = "源代码",
                    LineNumber = 10,
                    MethodName = "示例方法",
                    CodeSnippet = "string result = str1 + str2;"
                },
                DeductionReason = "在循环中使用字符串拼接会创建大量临时对象，影响性能",
                ImprovementSuggestion = "建议使用StringBuilder或string.Join()方法",
                ExampleCode = "StringBuilder sb = new StringBuilder(); sb.Append(str1).Append(str2);",
                ImpactScope = "method",
                IsCritical = false
            });
        }

        if (!projectCode.CombinedSourceCode.Contains("try") && !projectCode.CombinedSourceCode.Contains("catch"))
        {
            deductionPoints.Add(new DeductionPoint
            {
                DeductionId = "ERR_001",
                IssueType = "缺乏错误处理",
                IssueCategory = "error_handling",
                Severity = "high",
                DeductionScore = 8.0,
                Description = "代码缺乏适当的错误处理机制",
                LocationInfo = new LocationInfo
                {
                    FileName = "源代码",
                    MethodName = "Main",
                    CodeSnippet = "整个方法"
                },
                DeductionReason = "没有使用try-catch块处理可能的异常",
                ImprovementSuggestion = "添加适当的异常处理机制",
                ExampleCode = "try { /* 代码 */ } catch (Exception ex) { /* 处理异常 */ }",
                ImpactScope = "method",
                IsCritical = true
            });
        }

        if (projectCode.CombinedSourceCode.Contains("var "))
        {
            deductionPoints.Add(new DeductionPoint
            {
                DeductionId = "STYLE_001",
                IssueType = "使用var关键字",
                IssueCategory = "code_style",
                Severity = "low",
                DeductionScore = 2.5,
                Description = "使用了var关键字而非显式类型声明",
                LocationInfo = new LocationInfo
                {
                    FileName = "源代码",
                    LineNumber = 5,
                    CodeSnippet = "var result = ..."
                },
                DeductionReason = "项目编码规范要求使用显式类型声明",
                ImprovementSuggestion = "将var替换为具体的类型名称",
                ExampleCode = "string result = ...; // 而不是 var result = ...;",
                ImpactScope = "local",
                IsCritical = false
            });
        }

        return deductionPoints;
    }

    /// <summary>
    /// 将扣分点转换为CSharpScoringResult列表
    /// </summary>
    /// <param name="deductionPoints">扣分点列表</param>
    /// <param name="projectName">项目名称</param>
    /// <returns>CSharpScoringResult列表</returns>
    private static List<CSharpScoringResult> ConvertToScoringResults(
        List<DeductionPoint> deductionPoints, 
        string projectName)
    {
        List<CSharpScoringResult> scoringResults = [];

        foreach (DeductionPoint deductionPoint in deductionPoints)
        {
            CSharpScoringResult scoringResult = new()
            {
                // TotalScore表示该扣分点的扣分数值
                TotalScore = deductionPoint.DeductionScore,
                
                // Details字段详细描述该扣分点的具体问题
                Details = GenerateDetailedDescription(deductionPoint),
                
                // 其他相关信息
                IsSuccess = true,
                CompilationTimeMs = 0, // 扣分评估不涉及编译时间
                ExecutionTimeMs = 0    // 扣分评估不涉及执行时间
            };

            scoringResults.Add(scoringResult);
        }

        return scoringResults;
    }

    /// <summary>
    /// 生成详细的扣分描述
    /// </summary>
    /// <param name="deductionPoint">扣分点</param>
    /// <returns>详细描述</returns>
    private static string GenerateDetailedDescription(DeductionPoint deductionPoint)
    {
        StringBuilder details = new();

        details.AppendLine($"🔍 扣分点ID: {deductionPoint.DeductionId}");
        details.AppendLine($"📋 问题类型: {deductionPoint.IssueType}");
        details.AppendLine($"📂 问题类别: {GetCategoryDisplayName(deductionPoint.IssueCategory)}");
        details.AppendLine($"⚠️ 严重程度: {GetSeverityDisplayName(deductionPoint.Severity)}");
        details.AppendLine($"📉 扣分数值: {deductionPoint.DeductionScore} 分");
        details.AppendLine();

        details.AppendLine("📝 问题描述:");
        details.AppendLine($"   {deductionPoint.Description}");
        details.AppendLine();

        if (deductionPoint.LocationInfo != null)
        {
            details.AppendLine("📍 位置信息:");
            if (!string.IsNullOrEmpty(deductionPoint.LocationInfo.FileName))
                details.AppendLine($"   文件: {deductionPoint.LocationInfo.FileName}");
            if (deductionPoint.LocationInfo.LineNumber.HasValue)
                details.AppendLine($"   行号: {deductionPoint.LocationInfo.LineNumber}");
            if (!string.IsNullOrEmpty(deductionPoint.LocationInfo.ClassName))
                details.AppendLine($"   类名: {deductionPoint.LocationInfo.ClassName}");
            if (!string.IsNullOrEmpty(deductionPoint.LocationInfo.MethodName))
                details.AppendLine($"   方法: {deductionPoint.LocationInfo.MethodName}");
            if (!string.IsNullOrEmpty(deductionPoint.LocationInfo.CodeSnippet))
                details.AppendLine($"   代码片段: {deductionPoint.LocationInfo.CodeSnippet}");
            details.AppendLine();
        }

        details.AppendLine("🔍 扣分理由:");
        details.AppendLine($"   {deductionPoint.DeductionReason}");
        details.AppendLine();

        details.AppendLine("💡 改进建议:");
        details.AppendLine($"   {deductionPoint.ImprovementSuggestion}");

        if (!string.IsNullOrEmpty(deductionPoint.ExampleCode))
        {
            details.AppendLine();
            details.AppendLine("📋 示例代码:");
            details.AppendLine($"   {deductionPoint.ExampleCode}");
        }

        details.AppendLine();
        details.AppendLine($"🎯 影响范围: {GetImpactScopeDisplayName(deductionPoint.ImpactScope)}");
        details.AppendLine($"🚨 关键问题: {(deductionPoint.IsCritical ? "是" : "否")}");

        return details.ToString();
    }

    /// <summary>
    /// 获取类别显示名称
    /// </summary>
    /// <param name="category">类别</param>
    /// <returns>显示名称</returns>
    private static string GetCategoryDisplayName(string category)
    {
        return category switch
        {
            "logic_error" => "逻辑错误",
            "performance_issue" => "性能问题",
            "code_style" => "代码风格",
            "security_vulnerability" => "安全漏洞",
            "maintainability" => "可维护性",
            "readability" => "可读性",
            "best_practice_violation" => "最佳实践违反",
            "algorithm_efficiency" => "算法效率",
            "error_handling" => "错误处理",
            "design_pattern" => "设计模式",
            _ => category
        };
    }

    /// <summary>
    /// 获取严重程度显示名称
    /// </summary>
    /// <param name="severity">严重程度</param>
    /// <returns>显示名称</returns>
    private static string GetSeverityDisplayName(string severity)
    {
        return severity switch
        {
            "critical" => "严重",
            "high" => "高",
            "medium" => "中等",
            "low" => "低",
            _ => severity
        };
    }

    /// <summary>
    /// 获取影响范围显示名称
    /// </summary>
    /// <param name="impactScope">影响范围</param>
    /// <returns>显示名称</returns>
    private static string GetImpactScopeDisplayName(string impactScope)
    {
        return impactScope switch
        {
            "system" => "整个系统",
            "module" => "整个模块",
            "class" => "整个类",
            "method" => "单个方法",
            "local" => "局部代码",
            _ => impactScope
        };
    }
}

/// <summary>
/// 扣分评估选项
/// </summary>
public class DeductionAssessmentOptions
{
    /// <summary>
    /// 是否启用严格模式
    /// </summary>
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// 最大扣分点数量
    /// </summary>
    public int MaxDeductionPoints { get; set; } = 50;

    /// <summary>
    /// 最小扣分阈值
    /// </summary>
    public double MinDeductionThreshold { get; set; } = 1.0;

    /// <summary>
    /// 是否包含代码风格检查
    /// </summary>
    public bool IncludeStyleCheck { get; set; } = true;

    /// <summary>
    /// 是否包含性能检查
    /// </summary>
    public bool IncludePerformanceCheck { get; set; } = true;

    /// <summary>
    /// 是否包含安全检查
    /// </summary>
    public bool IncludeSecurityCheck { get; set; } = true;

    /// <summary>
    /// AI服务超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
