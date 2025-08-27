using System.Collections.Generic;
using ReactiveUI.Fody.Helpers;

namespace Examina.Models;

/// <summary>
/// 考试分数详情模型
/// </summary>
public class ExamScoreDetail
{
    /// <summary>
    /// 总分数
    /// </summary>
    [Reactive] public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    [Reactive] public decimal AchievedScore { get; set; }

    /// <summary>
    /// 得分率（0-1）
    /// </summary>
    public decimal ScoreRate => TotalScore > 0 ? AchievedScore / TotalScore : 0;

    /// <summary>
    /// 得分百分比（0-100）
    /// </summary>
    public decimal ScorePercentage => ScoreRate * 100;

    /// <summary>
    /// 是否通过考试
    /// </summary>
    [Reactive] public bool IsPassed { get; set; }

    /// <summary>
    /// 通过分数线（百分比）
    /// </summary>
    [Reactive] public decimal PassThreshold { get; set; } = 60; // 默认60%及格

    /// <summary>
    /// 成绩等级
    /// </summary>
    public string Grade
    {
        get
        {
            decimal percentage = ScorePercentage;
            return percentage switch
            {
                >= 90 => "优秀",
                >= 80 => "良好", 
                >= 70 => "中等",
                >= 60 => "及格",
                _ => "不及格"
            };
        }
    }

    /// <summary>
    /// 成绩等级颜色
    /// </summary>
    public string GradeColor
    {
        get
        {
            decimal percentage = ScorePercentage;
            return percentage switch
            {
                >= 90 => "#4CAF50", // 绿色 - 优秀
                >= 80 => "#8BC34A", // 浅绿色 - 良好
                >= 70 => "#FFC107", // 黄色 - 中等
                >= 60 => "#FF9800", // 橙色 - 及格
                _ => "#F44336" // 红色 - 不及格
            };
        }
    }

    /// <summary>
    /// 统计信息
    /// </summary>
    public ScoreStatistics Statistics { get; set; } = new();
}



/// <summary>
/// 分数统计信息
/// </summary>
public class ScoreStatistics
{
    /// <summary>
    /// 总题目数量
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 正确题目数量
    /// </summary>
    public int CorrectQuestions { get; set; }

    /// <summary>
    /// 错误题目数量
    /// </summary>
    public int IncorrectQuestions => TotalQuestions - CorrectQuestions;

    /// <summary>
    /// 正确率
    /// </summary>
    public decimal AccuracyRate => TotalQuestions > 0 ? (decimal)CorrectQuestions / TotalQuestions : 0;

    /// <summary>
    /// 正确率百分比
    /// </summary>
    public decimal AccuracyPercentage => AccuracyRate * 100;
}
