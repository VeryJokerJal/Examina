using System.Collections.ObjectModel;
using ReactiveUI;
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
    /// <param name="scoringResult">BenchSuite评分结果</param>
    /// <param name="startTime">训练开始时间</param>
    public void SetTrainingResult(string trainingName, BenchSuiteScoringResult scoringResult, DateTime startTime)
    {
        TrainingName = trainingName;
        Title = $"训练结果 - {trainingName}";
        
        TotalScore = scoringResult.TotalScore;
        AchievedScore = scoringResult.AchievedScore;
        ScoreRate = scoringResult.ScoreRate * 100; // 转换为百分比
        
        CompletionTime = scoringResult.EndTime;
        Duration = scoringResult.EndTime - startTime;
        
        // 计算成绩等级
        Grade = CalculateGrade(ScoreRate);
        
        // 处理模块结果
        ProcessModuleResults(scoringResult);
        
        // 处理题目结果
        ProcessQuestionResults(scoringResult);
        
        // 更新统计信息
        UpdateStatistics();
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
    /// 处理模块结果
    /// </summary>
    private void ProcessModuleResults(BenchSuiteScoringResult scoringResult)
    {
        ModuleResults.Clear();
        
        foreach (KeyValuePair<BenchSuiteFileType, FileTypeScoringResult> kvp in scoringResult.FileTypeResults)
        {
            FileTypeScoringResult fileResult = kvp.Value;
            
            ModuleResultItem moduleItem = new()
            {
                ModuleName = GetFileTypeDisplayName(kvp.Key),
                TotalScore = fileResult.TotalScore,
                AchievedScore = fileResult.AchievedScore,
                ScoreRate = fileResult.TotalScore > 0 ? fileResult.AchievedScore / fileResult.TotalScore * 100 : 0,
                IsSuccess = fileResult.IsSuccess,
                Details = fileResult.Details,
                ErrorMessage = fileResult.ErrorMessage
            };
            
            ModuleResults.Add(moduleItem);
        }
    }

    /// <summary>
    /// 处理题目结果
    /// </summary>
    private void ProcessQuestionResults(BenchSuiteScoringResult scoringResult)
    {
        QuestionResults.Clear();
        
        // 从各个文件类型结果中提取题目信息
        foreach (KeyValuePair<BenchSuiteFileType, FileTypeScoringResult> kvp in scoringResult.FileTypeResults)
        {
            FileTypeScoringResult fileResult = kvp.Value;
            string moduleName = GetFileTypeDisplayName(kvp.Key);
            
            // 创建题目结果项（基于文件类型）
            QuestionResultItem questionItem = new()
            {
                QuestionId = $"{kvp.Key}",
                QuestionTitle = $"{moduleName}操作题",
                ModuleName = moduleName,
                TotalScore = fileResult.TotalScore,
                AchievedScore = fileResult.AchievedScore,
                IsCorrect = fileResult.IsSuccess && fileResult.AchievedScore >= fileResult.TotalScore * 0.6m, // 60%及格
                Details = fileResult.Details,
                ErrorMessage = fileResult.ErrorMessage,
                ScoreRate = fileResult.TotalScore > 0 ? fileResult.AchievedScore / fileResult.TotalScore * 100 : 0
            };
            
            QuestionResults.Add(questionItem);
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
    /// 获取文件类型显示名称
    /// </summary>
    private static string GetFileTypeDisplayName(BenchSuiteFileType fileType)
    {
        return fileType switch
        {
            BenchSuiteFileType.Word => "Word文档",
            BenchSuiteFileType.Excel => "Excel表格",
            BenchSuiteFileType.PowerPoint => "PowerPoint演示文稿",
            BenchSuiteFileType.CSharp => "C#编程",
            BenchSuiteFileType.Windows => "Windows操作",
            _ => fileType.ToString()
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
