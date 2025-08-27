using System.Collections.ObjectModel;
using System.Linq;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
    /// 试卷总分（手动设置的固定值）
    /// </summary>
    [Reactive] public double TotalScore { get; set; }

    /// <summary>
    /// 动态计算的总分（基于所有模块的实际分值）
    /// </summary>
    public double CalculatedTotalScore => Modules.Sum(m => m.TotalScore);

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
