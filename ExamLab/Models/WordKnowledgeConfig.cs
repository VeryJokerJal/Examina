using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// Word知识点配置模型
/// </summary>
public class WordKnowledgeConfig : ReactiveObject
{
    /// <summary>
    /// 知识点类型
    /// </summary>
    [Reactive] public WordKnowledgeType KnowledgeType { get; set; }

    /// <summary>
    /// 知识点名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 知识点描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 知识点分类
    /// </summary>
    [Reactive] public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 配置参数模板
    /// </summary>
    public ObservableCollection<ConfigurationParameterTemplate> ParameterTemplates { get; set; } = [];
}
