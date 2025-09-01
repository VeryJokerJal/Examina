using System;
using System.Collections.ObjectModel;
using System.Linq;
using Examina.Models.BenchSuite;
using Examina.Models.Enums;
using Examina.ViewModels.Base;

namespace Examina.ViewModels;

/// <summary>
/// 专项训练结果视图模型（无分数版本）
/// </summary>
public class SpecializedTrainingResultViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _trainingName = string.Empty;
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
    public ObservableCollection<SpecializedModuleResultItem> ModuleResults { get; } = [];

    /// <summary>
    /// 题目结果列表
    /// </summary>
    public ObservableCollection<SpecializedQuestionResultItem> QuestionResults { get; } = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    public SpecializedTrainingResultViewModel()
    {
        Title = "专项训练结果";
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
        Title = $"专项训练结果 - {trainingName}";

        // 计算正确率
        int totalQuestions = scoringResults.Values.Sum(r => r.Questions.Count);
        int correctQuestions = scoringResults.Values.Sum(r => r.Questions.Count(q => q.IsCorrect));

        TotalQuestions = totalQuestions;
        CorrectQuestions = correctQuestions;
        IncorrectQuestions = totalQuestions - correctQuestions;

        // 设置完成时间和耗时
        CompletionTime = DateTime.Now;
        Duration = CompletionTime - startTime;

        // 根据正确率计算等级（不显示分数，只显示等级）
        Grade = CalculateGrade(CorrectRate);

        // 清空现有数据
        ModuleResults.Clear();
        QuestionResults.Clear();

        // 处理模块结果
        foreach (KeyValuePair<ModuleType, ScoringResult> kvp in scoringResults)
        {
            ModuleType moduleType = kvp.Key;
            ScoringResult result = kvp.Value;

            SpecializedModuleResultItem moduleItem = new()
            {
                ModuleName = GetModuleDisplayName(moduleType),
                ModuleType = moduleType,
                TotalQuestions = result.Questions.Count,
                CorrectQuestions = result.Questions.Count(q => q.IsCorrect),
                IncorrectQuestions = result.Questions.Count(q => !q.IsCorrect),
                CorrectRate = result.Questions.Count > 0 ? result.Questions.Count(q => q.IsCorrect) / (double)result.Questions.Count * 100 : 0,
                Details = result.Details ?? string.Empty,
                ErrorMessage = result.ErrorMessage ?? string.Empty
            };

            ModuleResults.Add(moduleItem);

            // 处理题目结果
            foreach (QuestionResult question in result.Questions)
            {
                SpecializedQuestionResultItem questionItem = new()
                {
                    QuestionTitle = question.Title,
                    IsCorrect = question.IsCorrect,
                    StatusIcon = question.IsCorrect ? "✅" : "❌",
                    ModuleName = GetModuleDisplayName(moduleType),
                    Details = question.Details ?? string.Empty,
                    ErrorMessage = question.ErrorMessage ?? string.Empty
                };

                QuestionResults.Add(questionItem);
            }
        }
    }

    /// <summary>
    /// 计算成绩等级（基于正确率）
    /// </summary>
    private static string CalculateGrade(double correctRate)
    {
        return correctRate switch
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
    private static string GetModuleDisplayName(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.CSharp => "C#",
            ModuleType.Database => "数据库",
            ModuleType.Web => "Web开发",
            ModuleType.Algorithm => "算法",
            _ => moduleType.ToString()
        };
    }
}

/// <summary>
/// 专项训练模块结果项
/// </summary>
public class SpecializedModuleResultItem : ViewModelBase
{
    private string _moduleName = string.Empty;
    private ModuleType _moduleType;
    private int _totalQuestions;
    private int _correctQuestions;
    private int _incorrectQuestions;
    private double _correctRate;
    private string _details = string.Empty;
    private string _errorMessage = string.Empty;

    public string ModuleName
    {
        get => _moduleName;
        set => this.RaiseAndSetIfChanged(ref _moduleName, value);
    }

    public ModuleType ModuleType
    {
        get => _moduleType;
        set => this.RaiseAndSetIfChanged(ref _moduleType, value);
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

    public double CorrectRate
    {
        get => _correctRate;
        set => this.RaiseAndSetIfChanged(ref _correctRate, value);
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

/// <summary>
/// 专项训练题目结果项
/// </summary>
public class SpecializedQuestionResultItem : ViewModelBase
{
    private string _questionTitle = string.Empty;
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
