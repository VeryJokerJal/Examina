using System.Collections.ObjectModel;
using System.Text;
using ReactiveUI;
using BenchSuite.Models;
using BenchSuite.Interfaces;
using Examina.Models;
using Examina.Models.BenchSuite;

namespace Examina.ViewModels;

/// <summary>
/// 训练结果视图模型
/// </summary>
public class TrainingResultViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _trainingName = string.Empty;
    private decimal _totalScore = 0;
    private decimal _achievedScore = 0;
    private decimal _scoreRate = 0;
    private string _grade = string.Empty;
    private DateTime _completionTime = DateTime.Now;
    private TimeSpan _duration = TimeSpan.Zero;
    private int _totalQuestions = 0;
    private int _correctQuestions = 0;
    private int _incorrectQuestions = 0;

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// 训练名称
    /// </summary>
    public string TrainingName
    {
        get => _trainingName;
        set => this.RaiseAndSetIfChanged(ref _trainingName, value);
    }

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore
    {
        get => _totalScore;
        set => this.RaiseAndSetIfChanged(ref _totalScore, value);
    }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore
    {
        get => _achievedScore;
        set => this.RaiseAndSetIfChanged(ref _achievedScore, value);
    }

    /// <summary>
    /// 得分率（百分比）
    /// </summary>
    public decimal ScoreRate
    {
        get => _scoreRate;
        set => this.RaiseAndSetIfChanged(ref _scoreRate, value);
    }

    /// <summary>
    /// 成绩等级
    /// </summary>
    public string Grade
    {
        get => _grade;
        set => this.RaiseAndSetIfChanged(ref _grade, value);
    }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletionTime
    {
        get => _completionTime;
        set => this.RaiseAndSetIfChanged(ref _completionTime, value);
    }

    /// <summary>
    /// 训练耗时
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        set => this.RaiseAndSetIfChanged(ref _duration, value);
    }

    /// <summary>
    /// 总题目数
    /// </summary>
    public int TotalQuestions
    {
        get => _totalQuestions;
        set => this.RaiseAndSetIfChanged(ref _totalQuestions, value);
    }

    /// <summary>
    /// 正确题目数
    /// </summary>
    public int CorrectQuestions
    {
        get => _correctQuestions;
        set => this.RaiseAndSetIfChanged(ref _correctQuestions, value);
    }

    /// <summary>
    /// 错误题目数
    /// </summary>
    public int IncorrectQuestions
    {
        get => _incorrectQuestions;
        set => this.RaiseAndSetIfChanged(ref _incorrectQuestions, value);
    }

    /// <summary>
    /// 正确率（百分比）
    /// </summary>
    public decimal CorrectRate => TotalQuestions > 0 ? (decimal)CorrectQuestions / TotalQuestions * 100 : 0;

    /// <summary>
    /// 模块结果列表
    /// </summary>
    public ObservableCollection<ModuleResultItem> ModuleResults { get; } = [];

    /// <summary>
    /// 题目结果列表
    /// </summary>
    public ObservableCollection<QuestionResultItem> QuestionResults { get; } = [];

    /// <summary>
    /// 详细评分信息文本
    /// </summary>
    public string DetailedScoringInfo { get; private set; } = string.Empty;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TrainingResultViewModel()
    {
        Title = "训练结果";
    }

    /// <summary>
    /// 设置训练结果数据
    /// </summary>
    /// <param name="trainingName">训练名称</param>
    /// <param name="scoringResults">BenchSuite评分结果字典（按模块类型分组）</param>
    /// <param name="startTime">训练开始时间</param>
    public void SetTrainingResult(string trainingName, Dictionary<ModuleType, ScoringResult> scoringResults, DateTime startTime)
    {
        TrainingName = trainingName;
        Title = $"训练结果 - {trainingName}";

        // 计算总分和得分
        decimal totalScore = scoringResults.Values.Sum(r => r.TotalScore);
        decimal achievedScore = scoringResults.Values.Sum(r => r.AchievedScore);

        TotalScore = totalScore;
        AchievedScore = achievedScore;
        ScoreRate = totalScore > 0 ? (achievedScore / totalScore) * 100 : 0; // 转换为百分比

        // 获取最晚的结束时间
        DateTime endTime = scoringResults.Values.Max(r => r.EndTime);
        CompletionTime = endTime;
        Duration = endTime - startTime;

        // 计算成绩等级
        Grade = CalculateGrade(ScoreRate);

        // 处理模块结果
        ProcessModuleResults(scoringResults);

        // 处理题目结果
        ProcessQuestionResults(scoringResults);

        // 更新统计信息
        UpdateStatistics();

        // 生成详细评分信息
        GenerateDetailedScoringInfo(scoringResults);
    }

    /// <summary>
    /// 设置训练结果数据（BenchSuiteScoringResult重载）
    /// </summary>
    /// <param name="trainingName">训练名称</param>
    /// <param name="benchSuiteResult">BenchSuite评分结果</param>
    /// <param name="startTime">训练开始时间</param>
    public void SetTrainingResult(string trainingName, BenchSuiteScoringResult benchSuiteResult, DateTime startTime)
    {
        // 将BenchSuiteScoringResult转换为按模块类型分组的ScoringResult字典
        Dictionary<ModuleType, ScoringResult> scoringResults = benchSuiteResult.ToModuleResults();

        // 调用原有的SetTrainingResult方法
        SetTrainingResult(trainingName, scoringResults, startTime);
    }

    /// <summary>
    /// 计算成绩等级
    /// </summary>
    private string CalculateGrade(decimal scoreRate)
    {
        return scoreRate switch
        {
            >= 90 => "优秀",
            >= 80 => "良好", 
            >= 70 => "中等",
            >= 60 => "及格",
            _ => "不及格"
        };
    }

    /// <summary>
    /// 生成详细评分信息（参考WindowsTestProgram.cs格式）
    /// </summary>
    private void GenerateDetailedScoringInfo(Dictionary<ModuleType, ScoringResult> scoringResults)
    {
        StringBuilder sb = new();

        sb.AppendLine("评分详细信息（用于诊断）:");
        sb.AppendLine(new string('-', 80));

        foreach (KeyValuePair<ModuleType, ScoringResult> kvp in scoringResults)
        {
            ScoringResult scoringResult = kvp.Value;
            string moduleName = GetModuleTypeDisplayName(kvp.Key);

            sb.AppendLine($"模块: {moduleName}");
            sb.AppendLine($"  总分: {scoringResult.TotalScore}");
            sb.AppendLine($"  得分: {scoringResult.AchievedScore}");
            sb.AppendLine($"  成功: {scoringResult.IsSuccess}");

            if (!string.IsNullOrEmpty(scoringResult.ErrorMessage))
            {
                sb.AppendLine($"  错误: {scoringResult.ErrorMessage}");
            }

            sb.AppendLine();

            // 显示知识点详细信息
            foreach (KnowledgePointResult kpResult in scoringResult.KnowledgePointResults)
            {
                sb.AppendLine($"知识点: {kpResult.KnowledgePointType}");
                sb.AppendLine($"  得分: {kpResult.AchievedScore}/{kpResult.TotalScore}");
                sb.AppendLine($"  是否正确: {kpResult.IsCorrect}");

                if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
                {
                    sb.AppendLine($"  错误: {kpResult.ErrorMessage}");
                }

                if (!string.IsNullOrEmpty(kpResult.Details))
                {
                    string shortDetails = kpResult.Details.Length > 150 ? kpResult.Details[..150] + "..." : kpResult.Details;
                    sb.AppendLine($"  详情: {shortDetails}");
                }

                sb.AppendLine();
            }

            if (scoringResult.KnowledgePointResults.Count == 0)
            {
                sb.AppendLine("  无知识点详细信息");
                sb.AppendLine();
            }

            sb.AppendLine(new string('-', 40));
        }

        DetailedScoringInfo = sb.ToString();
        this.RaisePropertyChanged(nameof(DetailedScoringInfo));
    }

    /// <summary>
    /// 处理模块结果
    /// </summary>
    private void ProcessModuleResults(Dictionary<ModuleType, ScoringResult> scoringResults)
    {
        ModuleResults.Clear();

        foreach (KeyValuePair<ModuleType, ScoringResult> kvp in scoringResults)
        {
            ScoringResult scoringResult = kvp.Value;

            ModuleResultItem moduleItem = new()
            {
                ModuleName = GetModuleTypeDisplayName(kvp.Key),
                TotalScore = scoringResult.TotalScore,
                AchievedScore = scoringResult.AchievedScore,
                ScoreRate = scoringResult.TotalScore > 0 ? scoringResult.AchievedScore / scoringResult.TotalScore * 100 : 0,
                IsSuccess = scoringResult.IsSuccess,
                Details = scoringResult.ErrorMessage ?? string.Empty,
                ErrorMessage = scoringResult.ErrorMessage,
                ModuleType = kvp.Key
            };

            ModuleResults.Add(moduleItem);
        }
    }

    /// <summary>
    /// 处理题目结果
    /// </summary>
    private void ProcessQuestionResults(Dictionary<ModuleType, ScoringResult> scoringResults)
    {
        QuestionResults.Clear();

        // 从各个模块结果中提取真实的题目信息
        foreach (KeyValuePair<ModuleType, ScoringResult> kvp in scoringResults)
        {
            ScoringResult scoringResult = kvp.Value;
            string moduleName = GetModuleTypeDisplayName(kvp.Key);

            // 从知识点结果中提取题目信息
            foreach (KnowledgePointResult kpResult in scoringResult.KnowledgePointResults)
            {
                QuestionResultItem questionItem = new()
                {
                    QuestionId = kpResult.KnowledgePointId,
                    QuestionTitle = !string.IsNullOrEmpty(kpResult.KnowledgePointName)
                        ? kpResult.KnowledgePointName
                        : $"{moduleName} - {kpResult.KnowledgePointType}",
                    ModuleName = moduleName,
                    TotalScore = kpResult.TotalScore,
                    AchievedScore = kpResult.AchievedScore,
                    IsCorrect = kpResult.IsCorrect,
                    Details = kpResult.Details,
                    ErrorMessage = kpResult.ErrorMessage,
                    ScoreRate = kpResult.TotalScore > 0 ? kpResult.AchievedScore / kpResult.TotalScore * 100 : 0
                };

                QuestionResults.Add(questionItem);
            }

            // 如果没有知识点结果，创建基于模块的虚拟题目（向后兼容）
            if (scoringResult.KnowledgePointResults.Count == 0)
            {
                QuestionResultItem questionItem = new()
                {
                    QuestionId = $"{kvp.Key}",
                    QuestionTitle = $"{moduleName}操作题",
                    ModuleName = moduleName,
                    TotalScore = scoringResult.TotalScore,
                    AchievedScore = scoringResult.AchievedScore,
                    IsCorrect = scoringResult.IsSuccess && scoringResult.AchievedScore >= scoringResult.TotalScore * 0.6m, // 60%及格
                    Details = scoringResult.ErrorMessage ?? string.Empty,
                    ErrorMessage = scoringResult.ErrorMessage,
                    ScoreRate = scoringResult.TotalScore > 0 ? scoringResult.AchievedScore / scoringResult.TotalScore * 100 : 0
                };

                QuestionResults.Add(questionItem);
            }
        }
    }

    /// <summary>
    /// 处理C# AI分析结果
    /// </summary>
    /// <param name="moduleItem">模块结果项</param>
    /// <param name="csharpResult">C#评分结果</param>
    private static void ProcessCSharpAIAnalysis(ModuleResultItem moduleItem, CSharpScoringResult csharpResult)
    {
        if (csharpResult.AILogicalResult?.IsSuccess == true)
        {
            AILogicalScoringResult aiResult = csharpResult.AILogicalResult;

            // 设置AI分析信息
            moduleItem.HasAIAnalysis = true;
            moduleItem.AILogicalScore = aiResult.LogicalScore;
            moduleItem.AIFinalAnswer = aiResult.FinalAnswer;
            moduleItem.AIProcessingTime = aiResult.ProcessingTimeMs;

            // 处理推理步骤
            moduleItem.AIReasoningSteps.Clear();
            foreach (ReasoningStep step in aiResult.Steps)
            {
                moduleItem.AIReasoningSteps.Add(new AIReasoningStepItem
                {
                    Explanation = step.Explanation,
                    Output = step.Output
                });
            }

            // 增强详细信息，包含AI分析
            string enhancedDetails = moduleItem.Details;
            if (!string.IsNullOrEmpty(enhancedDetails))
            {
                enhancedDetails += "\n\n";
            }

            enhancedDetails += $"🤖 AI逻辑性分析:\n";
            enhancedDetails += $"逻辑性评分: {aiResult.LogicalScore}/100\n";
            enhancedDetails += $"处理耗时: {aiResult.ProcessingTimeMs}ms\n";

            if (aiResult.Steps.Count > 0)
            {
                enhancedDetails += "主要分析步骤:\n";
                foreach (ReasoningStep step in aiResult.Steps.Take(3))
                {
                    enhancedDetails += $"  • {step.Explanation}\n";
                }
            }

            if (!string.IsNullOrEmpty(aiResult.FinalAnswer))
            {
                enhancedDetails += $"AI评估结论: {aiResult.FinalAnswer}";
            }

            moduleItem.Details = enhancedDetails;
        }
        else if (csharpResult.AILogicalResult != null && !csharpResult.AILogicalResult.IsSuccess)
        {
            // AI分析失败的情况
            moduleItem.HasAIAnalysis = false;
            string enhancedDetails = moduleItem.Details;
            if (!string.IsNullOrEmpty(enhancedDetails))
            {
                enhancedDetails += "\n\n";
            }
            enhancedDetails += $"⚠️ AI逻辑性分析失败: {csharpResult.AILogicalResult.ErrorMessage}";
            moduleItem.Details = enhancedDetails;
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        TotalQuestions = QuestionResults.Count;
        CorrectQuestions = QuestionResults.Count(q => q.IsCorrect);
        IncorrectQuestions = TotalQuestions - CorrectQuestions;
    }

    /// <summary>
    /// 获取模块类型显示名称
    /// </summary>
    private static string GetModuleTypeDisplayName(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Word => "Word文档",
            ModuleType.Excel => "Excel表格",
            ModuleType.PowerPoint => "PowerPoint演示文稿",
            ModuleType.CSharp => "C#编程",
            ModuleType.Windows => "Windows操作",
            _ => moduleType.ToString()
        };
    }
}

/// <summary>
/// 模块结果项
/// </summary>
public class ModuleResultItem
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 得分率（百分比）
    /// </summary>
    public decimal ScoreRate { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType ModuleType { get; set; }

    /// <summary>
    /// 是否有AI分析结果
    /// </summary>
    public bool HasAIAnalysis { get; set; }

    /// <summary>
    /// AI逻辑性评分（0-100）
    /// </summary>
    public decimal AILogicalScore { get; set; }

    /// <summary>
    /// AI最终答案
    /// </summary>
    public string AIFinalAnswer { get; set; } = string.Empty;

    /// <summary>
    /// AI处理耗时（毫秒）
    /// </summary>
    public long AIProcessingTime { get; set; }

    /// <summary>
    /// AI推理步骤列表
    /// </summary>
    public ObservableCollection<AIReasoningStepItem> AIReasoningSteps { get; } = [];

    /// <summary>
    /// 是否为C#模块
    /// </summary>
    public bool IsCSharpModule => ModuleType == ModuleType.CSharp;

    /// <summary>
    /// AI评分等级描述
    /// </summary>
    public string AIScoreGrade
    {
        get
        {
            if (!HasAIAnalysis) return "无AI分析";

            return AILogicalScore switch
            {
                >= 90 => "优秀",
                >= 80 => "良好",
                >= 70 => "中等",
                >= 60 => "及格",
                _ => "不及格"
            };
        }
    }
}

/// <summary>
/// 题目结果项
/// </summary>
public class QuestionResultItem
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    public string QuestionTitle { get; set; } = string.Empty;

    /// <summary>
    /// 所属模块
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 得分率（百分比）
    /// </summary>
    public decimal ScoreRate { get; set; }

    /// <summary>
    /// 是否正确
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 状态图标
    /// </summary>
    public string StatusIcon => IsCorrect ? "✓" : "✗";

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => IsCorrect ? "Green" : "Red";
}

/// <summary>
/// AI推理步骤项
/// </summary>
public class AIReasoningStepItem
{
    /// <summary>
    /// 步骤说明
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 步骤输出
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 步骤类型（可选）
    /// </summary>
    public string StepType { get; set; } = string.Empty;

    /// <summary>
    /// 格式化的步骤描述
    /// </summary>
    public string FormattedDescription => $"{Explanation}: {Output}";
}
