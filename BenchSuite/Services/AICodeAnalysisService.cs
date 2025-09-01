using BenchSuite.Models;
using System.Text;
using System.Text.Json;

namespace BenchSuite.Services;

/// <summary>
/// AI代码分析服务 - 集成AI服务进行代码分析
/// </summary>
public static class AICodeAnalysisService
{
    /// <summary>
    /// 代码分析结果
    /// </summary>
    public class CodeAnalysisResult
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
        /// 结构分析结果
        /// </summary>
        public CodeStructureAnalysisResponse? StructureAnalysis { get; set; }

        /// <summary>
        /// 质量评估结果
        /// </summary>
        public CodeQualityAssessmentResponse? QualityAssessment { get; set; }

        /// <summary>
        /// 代码解释结果
        /// </summary>
        public CodeExplanationResponse? CodeExplanation { get; set; }

        /// <summary>
        /// 改进建议结果
        /// </summary>
        public CodeImprovementResponse? ImprovementSuggestions { get; set; }

        /// <summary>
        /// 扣分评估结果
        /// </summary>
        public AIDeductionAssessmentResponse? DeductionAssessment { get; set; }

        /// <summary>
        /// 生成的CSharpScoringResult列表
        /// </summary>
        public List<CSharpScoringResult> ScoringResults { get; set; } = [];

        /// <summary>
        /// 分析耗时（毫秒）
        /// </summary>
        public long AnalysisTimeMs { get; set; }

        /// <summary>
        /// 分析的代码块数量
        /// </summary>
        public int CodeChunkCount { get; set; }
    }

    /// <summary>
    /// 分析项目代码
    /// </summary>
    /// <param name="projectFilePath">项目文件路径</param>
    /// <param name="analysisOptions">分析选项</param>
    /// <returns>代码分析结果</returns>
    public static async Task<CodeAnalysisResult> AnalyzeProjectCodeAsync(
        string projectFilePath, 
        CodeAnalysisOptions? analysisOptions = null)
    {
        DateTime startTime = DateTime.Now;
        CodeAnalysisResult result = new();

        try
        {
            // 设置默认分析选项
            analysisOptions ??= new CodeAnalysisOptions();

            // 读取项目代码
            ProjectFileReaderService.ProjectCodeResult projectCode = 
                await ProjectFileReaderService.ReadProjectCodeAsync(projectFilePath);

            if (!projectCode.IsSuccess)
            {
                result.ErrorMessage = $"读取项目代码失败: {projectCode.ErrorMessage}";
                return result;
            }

            // 分块处理代码
            List<string> codeChunks = SplitCodeIntoChunks(projectCode.CombinedSourceCode, analysisOptions.MaxChunkSize);
            result.CodeChunkCount = codeChunks.Count;

            // 执行不同类型的分析
            if (analysisOptions.IncludeStructureAnalysis)
            {
                result.StructureAnalysis = await AnalyzeCodeStructureAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeQualityAssessment)
            {
                result.QualityAssessment = await AssessCodeQualityAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeCodeExplanation)
            {
                result.CodeExplanation = await ExplainCodeAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeImprovementSuggestions)
            {
                result.ImprovementSuggestions = await GenerateImprovementSuggestionsAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeDeductionAssessment)
            {
                AIDeductionAssessmentService.DeductionAssessmentResult deductionResult =
                    await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(
                        projectCode.CombinedSourceCode,
                        projectCode.ProjectName + ".cs");

                if (deductionResult.IsSuccess)
                {
                    result.DeductionAssessment = deductionResult.AssessmentResponse;
                    result.ScoringResults = deductionResult.ScoringResults;
                }
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"代码分析过程中发生异常: {ex.Message}";
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.AnalysisTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 分析单个代码文件
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="fileName">文件名</param>
    /// <param name="analysisOptions">分析选项</param>
    /// <returns>代码分析结果</returns>
    public static async Task<CodeAnalysisResult> AnalyzeSingleCodeAsync(
        string sourceCode,
        string fileName = "Code.cs",
        CodeAnalysisOptions? analysisOptions = null)
    {
        DateTime startTime = DateTime.Now;
        CodeAnalysisResult result = new();

        try
        {
            analysisOptions ??= new CodeAnalysisOptions();

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

            List<string> codeChunks = SplitCodeIntoChunks(sourceCode, analysisOptions.MaxChunkSize);
            result.CodeChunkCount = codeChunks.Count;

            // 执行分析
            if (analysisOptions.IncludeStructureAnalysis)
            {
                result.StructureAnalysis = await AnalyzeCodeStructureAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeQualityAssessment)
            {
                result.QualityAssessment = await AssessCodeQualityAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeCodeExplanation)
            {
                result.CodeExplanation = await ExplainCodeAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeImprovementSuggestions)
            {
                result.ImprovementSuggestions = await GenerateImprovementSuggestionsAsync(
                    projectCode, codeChunks, analysisOptions);
            }

            if (analysisOptions.IncludeDeductionAssessment)
            {
                AIDeductionAssessmentService.DeductionAssessmentResult deductionResult =
                    await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(
                        sourceCode, fileName);

                if (deductionResult.IsSuccess)
                {
                    result.DeductionAssessment = deductionResult.AssessmentResponse;
                    result.ScoringResults = deductionResult.ScoringResults;
                }
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"代码分析过程中发生异常: {ex.Message}";
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            result.AnalysisTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 将代码分割成块
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="maxChunkSize">最大块大小</param>
    /// <returns>代码块列表</returns>
    private static List<string> SplitCodeIntoChunks(string sourceCode, int maxChunkSize)
    {
        List<string> chunks = [];

        if (sourceCode.Length <= maxChunkSize)
        {
            chunks.Add(sourceCode);
            return chunks;
        }

        // 按文件分割
        string[] fileSections = sourceCode.Split("// ===== 文件:", StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string section in fileSections)
        {
            if (string.IsNullOrWhiteSpace(section)) continue;

            if (section.Length <= maxChunkSize)
            {
                chunks.Add(section);
            }
            else
            {
                // 进一步分割大文件
                chunks.AddRange(SplitLargeSection(section, maxChunkSize));
            }
        }

        return chunks.Count > 0 ? chunks : [sourceCode];
    }

    /// <summary>
    /// 分割大的代码段
    /// </summary>
    /// <param name="section">代码段</param>
    /// <param name="maxChunkSize">最大块大小</param>
    /// <returns>分割后的块</returns>
    private static List<string> SplitLargeSection(string section, int maxChunkSize)
    {
        List<string> chunks = [];
        string[] lines = section.Split('\n');
        StringBuilder currentChunk = new();

        foreach (string line in lines)
        {
            if (currentChunk.Length + line.Length + 1 > maxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
            }
            
            currentChunk.AppendLine(line);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
        }

        return chunks;
    }

    /// <summary>
    /// 分析代码结构
    /// </summary>
    /// <param name="projectCode">项目代码</param>
    /// <param name="codeChunks">代码块</param>
    /// <param name="options">分析选项</param>
    /// <returns>结构分析结果</returns>
    private static async Task<CodeStructureAnalysisResponse> AnalyzeCodeStructureAsync(
        ProjectFileReaderService.ProjectCodeResult projectCode,
        List<string> codeChunks,
        CodeAnalysisOptions options)
    {
        try
        {
            // 这里应该调用实际的AI服务
            // 目前返回模拟结果
            await Task.Delay(100); // 模拟AI调用延迟

            return new CodeStructureAnalysisResponse
            {
                StructureOverview = $"项目 {projectCode.ProjectName} 包含 {projectCode.Statistics.TotalFiles} 个文件，总计 {projectCode.Statistics.TotalLines} 行代码。",
                ArchitectureScore = 85,
                DesignPatterns = ["Repository Pattern", "Dependency Injection"],
                AnalysisSteps = 
                [
                    new CodeAnalysisStep
                    {
                        StepType = "架构分析",
                        Explanation = "分析项目整体架构",
                        Finding = "项目采用了分层架构模式"
                    }
                ],
                OrganizationSuggestions = ["考虑将业务逻辑进一步抽象", "增加接口定义以提高可测试性"]
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"代码结构分析失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 评估代码质量
    /// </summary>
    /// <param name="projectCode">项目代码</param>
    /// <param name="codeChunks">代码块</param>
    /// <param name="options">分析选项</param>
    /// <returns>质量评估结果</returns>
    private static async Task<CodeQualityAssessmentResponse> AssessCodeQualityAsync(
        ProjectFileReaderService.ProjectCodeResult projectCode,
        List<string> codeChunks,
        CodeAnalysisOptions options)
    {
        try
        {
            // 这里应该调用实际的AI服务
            await Task.Delay(100);

            return new CodeQualityAssessmentResponse
            {
                OverallQualityScore = 78,
                DimensionScores = new QualityDimensionScores
                {
                    Readability = 80,
                    Maintainability = 75,
                    Performance = 85,
                    Security = 70,
                    Testability = 65
                },
                AssessmentSteps = 
                [
                    new QualityAssessmentStep
                    {
                        Dimension = "可读性",
                        Explanation = "代码命名规范，结构清晰",
                        AssessmentResult = "良好",
                        DimensionScore = 80
                    }
                ],
                CodeIssues = [],
                ImprovementRecommendations = ["增加单元测试", "改进错误处理"],
                BestPracticesCompliance = []
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"代码质量评估失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 解释代码
    /// </summary>
    /// <param name="projectCode">项目代码</param>
    /// <param name="codeChunks">代码块</param>
    /// <param name="options">分析选项</param>
    /// <returns>代码解释结果</returns>
    private static async Task<CodeExplanationResponse> ExplainCodeAsync(
        ProjectFileReaderService.ProjectCodeResult projectCode,
        List<string> codeChunks,
        CodeAnalysisOptions options)
    {
        try
        {
            await Task.Delay(100);

            return new CodeExplanationResponse
            {
                OverallExplanation = $"这是一个 {projectCode.ProjectName} 项目，主要功能是...",
                LineByLineExplanations = [],
                KeyConcepts = [],
                CommentSuggestions = [],
                ComplexityAnalysis = new ComplexityAnalysis
                {
                    TimeComplexity = "O(n)",
                    SpaceComplexity = "O(1)",
                    ComplexityExplanation = "算法复杂度分析...",
                    OptimizationSuggestions = []
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"代码解释失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 生成改进建议
    /// </summary>
    /// <param name="projectCode">项目代码</param>
    /// <param name="codeChunks">代码块</param>
    /// <param name="options">分析选项</param>
    /// <returns>改进建议结果</returns>
    private static async Task<CodeImprovementResponse> GenerateImprovementSuggestionsAsync(
        ProjectFileReaderService.ProjectCodeResult projectCode,
        List<string> codeChunks,
        CodeAnalysisOptions options)
    {
        try
        {
            await Task.Delay(100);

            return new CodeImprovementResponse
            {
                ImprovementSuggestions = [],
                RefactoringSuggestions = [],
                PerformanceImprovements = [],
                StyleImprovements = [],
                PriorityRanking = []
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"改进建议生成失败: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// 代码分析选项
/// </summary>
public class CodeAnalysisOptions
{
    /// <summary>
    /// 是否包含结构分析
    /// </summary>
    public bool IncludeStructureAnalysis { get; set; } = true;

    /// <summary>
    /// 是否包含质量评估
    /// </summary>
    public bool IncludeQualityAssessment { get; set; } = true;

    /// <summary>
    /// 是否包含代码解释
    /// </summary>
    public bool IncludeCodeExplanation { get; set; } = false;

    /// <summary>
    /// 是否包含改进建议
    /// </summary>
    public bool IncludeImprovementSuggestions { get; set; } = true;

    /// <summary>
    /// 是否包含扣分评估
    /// </summary>
    public bool IncludeDeductionAssessment { get; set; } = true;

    /// <summary>
    /// 最大代码块大小（字符数）
    /// </summary>
    public int MaxChunkSize { get; set; } = 8000;

    /// <summary>
    /// AI服务超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
