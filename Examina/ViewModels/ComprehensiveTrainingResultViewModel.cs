using System;
using System.Collections.ObjectModel;
using System.Linq;
using BenchSuite.Models;
using Examina.Models.BenchSuite;
using ReactiveUI;

namespace Examina.ViewModels;

/// <summary>
/// 综合实训结果视图模型（完整分数版本）
/// </summary>
public class ComprehensiveTrainingResultViewModel : ReactiveObject
{
    private string _title = string.Empty;
    private string _trainingName = string.Empty;
    private double _totalScore = 0;
    private double _achievedScore = 0;
    private double _scoreRate = 0;
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
    public double TotalScore
    {
        get => _totalScore;
        set => this.RaiseAndSetIfChanged(ref _totalScore, value);
    }

    /// <summary>
    /// 得分
    /// </summary>
    public double AchievedScore
    {
        get => _achievedScore;
        set => this.RaiseAndSetIfChanged(ref _achievedScore, value);
    }

    /// <summary>
    /// 得分率（百分比）
    /// </summary>
    public double ScoreRate
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
    public double CorrectRate => TotalQuestions > 0 ? CorrectQuestions / (double)TotalQuestions * 100 : 0;

    /// <summary>
    /// 模块结果列表
    /// </summary>
    public ObservableCollection<ComprehensiveModuleResultItem> ModuleResults { get; } = [];

    /// <summary>
    /// 题目结果列表
    /// </summary>
    public ObservableCollection<ComprehensiveQuestionResultItem> QuestionResults { get; } = [];

    /// <summary>
    /// 详细评分信息文本
    /// </summary>
    public string DetailedScoringInfo { get; private set; } = string.Empty;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ComprehensiveTrainingResultViewModel()
    {
        Title = "综合实训结果";
    }

    /// <summary>
    /// 设置训练结果数据（BenchSuiteScoringResult版本）
    /// </summary>
    /// <param name="trainingName">训练名称</param>
    /// <param name="benchSuiteResult">BenchSuite评分结果</param>
    /// <param name="startTime">训练开始时间</param>
    public void SetTrainingResult(string trainingName, BenchSuiteScoringResult benchSuiteResult, DateTime startTime)
    {
        TrainingName = trainingName;
        Title = $"综合实训结果 - {trainingName}";

        // 计算总分和得分
        double totalScore = benchSuiteResult.TotalScore;
        double achievedScore = benchSuiteResult.AchievedScore;

        TotalScore = totalScore;
        AchievedScore = achievedScore;
        ScoreRate = totalScore > 0 ? achievedScore / totalScore * 100 : 0;

        // 设置完成时间和耗时
        CompletionTime = DateTime.Now;
        Duration = CompletionTime - startTime;

        // 计算成绩等级
        Grade = CalculateGrade(ScoreRate);

        // 清空现有数据
        ModuleResults.Clear();
        QuestionResults.Clear();

        // 处理模块结果
        foreach (BenchSuite.Models.ModuleResult moduleResult in benchSuiteResult.ModuleResults)
        {
            ComprehensiveModuleResultItem moduleItem = new()
            {
                ModuleName = GetModuleDisplayName(moduleResult.ModuleType),
                ModuleType = moduleResult.ModuleType,
                TotalScore = moduleResult.TotalScore,
                AchievedScore = moduleResult.AchievedScore,
                ScoreRate = moduleResult.TotalScore > 0 ? moduleResult.AchievedScore / moduleResult.TotalScore * 100 : 0,
                TotalQuestions = moduleResult.Questions.Count,
                CorrectQuestions = moduleResult.Questions.Count(q => q.IsCorrect),
                IncorrectQuestions = moduleResult.Questions.Count(q => !q.IsCorrect),
                Details = moduleResult.Details ?? string.Empty,
                ErrorMessage = moduleResult.ErrorMessage ?? string.Empty
            };

            // 处理AI分析结果（如果是C#模块）
            if (moduleResult.ModuleType == BenchSuite.Models.ModuleType.CSharp)
            {
                ProcessAIAnalysisResult(moduleItem, moduleResult);
            }

            ModuleResults.Add(moduleItem);

            // 处理题目结果
            foreach (BenchSuite.Models.QuestionResult question in moduleResult.Questions)
            {
                ComprehensiveQuestionResultItem questionItem = new()
                {
                    QuestionTitle = question.Title,
                    TotalScore = question.TotalScore,
                    AchievedScore = question.AchievedScore,
                    ScoreRate = question.TotalScore > 0 ? question.AchievedScore / question.TotalScore * 100 : 0,
                    IsCorrect = question.IsCorrect,
                    StatusIcon = question.IsCorrect ? "✅" : "❌",
                    ModuleName = GetModuleDisplayName(moduleResult.ModuleType),
                    Details = question.Details ?? string.Empty,
                    ErrorMessage = question.ErrorMessage ?? string.Empty
                };

                QuestionResults.Add(questionItem);
            }
        }

        // 更新统计信息
        UpdateStatistics();

        // 生成详细评分信息
        GenerateDetailedScoringInfo();
    }

    /// <summary>
    /// 处理AI分析结果
    /// </summary>
    private static void ProcessAIAnalysisResult(ComprehensiveModuleResultItem moduleItem, BenchSuite.Models.ModuleResult moduleResult)
    {
        // 目前ModuleResult不包含AI分析结果，这个功能暂时禁用
        // 如果需要AI分析结果，需要从其他地方获取或者修改ModuleResult结构
        moduleItem.HasAIAnalysis = false;
    }

    /// <summary>
    /// 计算AI评分等级
    /// </summary>
    private static string CalculateAIGrade(int score)
    {
        return score switch
        {
            >= 90 => "优秀",
            >= 80 => "良好",
            >= 70 => "中等", 
            >= 60 => "及格",
            _ => "不及格"
        };
    }

    /// <summary>
    /// 计算成绩等级
    /// </summary>
    private static string CalculateGrade(double scoreRate)
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
    /// 获取模块显示名称
    /// </summary>
    private static string GetModuleDisplayName(BenchSuite.Models.ModuleType moduleType)
    {
        return moduleType switch
        {
            BenchSuite.Models.ModuleType.CSharp => "C#",
            BenchSuite.Models.ModuleType.Excel => "Excel",
            BenchSuite.Models.ModuleType.Word => "Word",
            BenchSuite.Models.ModuleType.PowerPoint => "PowerPoint",
            BenchSuite.Models.ModuleType.Windows => "Windows",
            _ => moduleType.ToString()
        };
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
    /// 生成详细评分信息
    /// </summary>
    private void GenerateDetailedScoringInfo()
    {
        System.Text.StringBuilder sb = new();
        
        sb.AppendLine($"训练名称: {TrainingName}");
        sb.AppendLine($"总分: {TotalScore:F1}");
        sb.AppendLine($"得分: {AchievedScore:F1}");
        sb.AppendLine($"得分率: {ScoreRate:F1}%");
        sb.AppendLine($"成绩等级: {Grade}");
        sb.AppendLine();
        
        sb.AppendLine("模块详情:");
        foreach (ComprehensiveModuleResultItem module in ModuleResults)
        {
            sb.AppendLine($"  {module.ModuleName}: {module.AchievedScore:F1}/{module.TotalScore:F1} ({module.ScoreRate:F1}%)");
        }
        
        DetailedScoringInfo = sb.ToString();
    }
}

/// <summary>
/// 综合实训模块结果项
/// </summary>
public class ComprehensiveModuleResultItem : ReactiveObject
{
    private string _moduleName = string.Empty;
    private BenchSuite.Models.ModuleType _moduleType;
    private double _totalScore;
    private double _achievedScore;
    private double _scoreRate;
    private int _totalQuestions;
    private int _correctQuestions;
    private int _incorrectQuestions;
    private string _details = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasAIAnalysis;
    private int _aiLogicalScore;
    private string _aiScoreGrade = string.Empty;
    private long _aiProcessingTime;

    public string ModuleName
    {
        get => _moduleName;
        set => this.RaiseAndSetIfChanged(ref _moduleName, value);
    }

    public BenchSuite.Models.ModuleType ModuleType
    {
        get => _moduleType;
        set => this.RaiseAndSetIfChanged(ref _moduleType, value);
    }

    public double TotalScore
    {
        get => _totalScore;
        set => this.RaiseAndSetIfChanged(ref _totalScore, value);
    }

    public double AchievedScore
    {
        get => _achievedScore;
        set => this.RaiseAndSetIfChanged(ref _achievedScore, value);
    }

    public double ScoreRate
    {
        get => _scoreRate;
        set => this.RaiseAndSetIfChanged(ref _scoreRate, value);
    }

    public int TotalQuestions
    {
        get => _totalQuestions;
        set => this.RaiseAndSetIfChanged(ref _totalQuestions, value);
    }

    public int CorrectQuestions
    {
        get => _correctQuestions;
        set => this.RaiseAndSetIfChanged(ref _correctQuestions, value);
    }

    public int IncorrectQuestions
    {
        get => _incorrectQuestions;
        set => this.RaiseAndSetIfChanged(ref _incorrectQuestions, value);
    }

    public string Details
    {
        get => _details;
        set => this.RaiseAndSetIfChanged(ref _details, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasAIAnalysis
    {
        get => _hasAIAnalysis;
        set => this.RaiseAndSetIfChanged(ref _hasAIAnalysis, value);
    }

    public int AILogicalScore
    {
        get => _aiLogicalScore;
        set => this.RaiseAndSetIfChanged(ref _aiLogicalScore, value);
    }

    public string AIScoreGrade
    {
        get => _aiScoreGrade;
        set => this.RaiseAndSetIfChanged(ref _aiScoreGrade, value);
    }

    public long AIProcessingTime
    {
        get => _aiProcessingTime;
        set => this.RaiseAndSetIfChanged(ref _aiProcessingTime, value);
    }

    public ObservableCollection<AIReasoningStepItem> AIReasoningSteps { get; } = [];
}

/// <summary>
/// 综合实训题目结果项
/// </summary>
public class ComprehensiveQuestionResultItem : ReactiveObject
{
    private string _questionTitle = string.Empty;
    private double _totalScore;
    private double _achievedScore;
    private double _scoreRate;
    private bool _isCorrect;
    private string _statusIcon = string.Empty;
    private string _moduleName = string.Empty;
    private string _details = string.Empty;
    private string _errorMessage = string.Empty;

    public string QuestionTitle
    {
        get => _questionTitle;
        set => this.RaiseAndSetIfChanged(ref _questionTitle, value);
    }

    public double TotalScore
    {
        get => _totalScore;
        set => this.RaiseAndSetIfChanged(ref _totalScore, value);
    }

    public double AchievedScore
    {
        get => _achievedScore;
        set => this.RaiseAndSetIfChanged(ref _achievedScore, value);
    }

    public double ScoreRate
    {
        get => _scoreRate;
        set => this.RaiseAndSetIfChanged(ref _scoreRate, value);
    }

    public bool IsCorrect
    {
        get => _isCorrect;
        set => this.RaiseAndSetIfChanged(ref _isCorrect, value);
    }

    public string StatusIcon
    {
        get => _statusIcon;
        set => this.RaiseAndSetIfChanged(ref _statusIcon, value);
    }

    public string ModuleName
    {
        get => _moduleName;
        set => this.RaiseAndSetIfChanged(ref _moduleName, value);
    }

    public string Details
    {
        get => _details;
        set => this.RaiseAndSetIfChanged(ref _details, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}
