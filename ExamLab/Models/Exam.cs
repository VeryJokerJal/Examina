using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using ExamLab.Services;

namespace ExamLab.Models;

/// <summary>
/// 试卷模型 - 支持考试试卷和专项试卷两种类型
/// </summary>
public class Exam : ReactiveObject
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [Reactive] public string Id { get; set; } = IdGeneratorService.GenerateExamId();

    /// <summary>
    /// 试卷名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 试卷类型 - 区分考试试卷和专项试卷
    /// </summary>
    [Reactive] public ExamType ExamType { get; set; } = ExamType.Regular;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public string CreatedTime { get; set; } = "2025-08-10";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [Reactive] public string LastModifiedTime { get; set; } = "2025-08-10";

    /// <summary>
    /// 试卷包含的模块
    /// </summary>
    public ObservableCollection<ExamModule> Modules { get; set; } = new();

    /// <summary>
    /// 试卷总分
    /// </summary>
    [Reactive] public int TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Reactive] public int Duration { get; set; } = 120;

    /// <summary>
    /// 判断是否为专项试卷
    /// </summary>
    public bool IsSpecializedExam => ExamType == ExamType.Specialized;

    /// <summary>
    /// 判断是否为考试试卷
    /// </summary>
    public bool IsRegularExam => ExamType == ExamType.Regular;
}

/// <summary>
/// 试卷类型枚举
/// </summary>
public enum ExamType
{
    /// <summary>
    /// 考试试卷 - 包含多个模块的综合试卷
    /// </summary>
    Regular = 0,

    /// <summary>
    /// 专项试卷 - 针对单一模块类型的专项练习试卷
    /// </summary>
    Specialized = 1
}
