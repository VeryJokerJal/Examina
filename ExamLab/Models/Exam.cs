using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using ExamLab.Services;

namespace ExamLab.Models;

/// <summary>
/// 试卷模型 - 用于考试试卷（包含多个模块的综合试卷）
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
    public ObservableCollection<ExamModule> Modules { get; set; } = [];

    /// <summary>
    /// 试卷总分
    /// </summary>
    [Reactive] public int TotalScore { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Reactive] public int Duration { get; set; } = 120;

    /// <summary>
    /// 重新初始化事件监听（用于反序列化后）
    /// </summary>
    public void ReinitializeEventListeners()
    {
        // 为所有模块重新初始化事件监听
        foreach (ExamModule module in Modules)
        {
            module.ReinitializeEventListeners();
        }
    }
}
