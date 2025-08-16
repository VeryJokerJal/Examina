using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 个人练习页面视图模型
/// </summary>
public class PracticeViewModel : ViewModelBase
{
    #region 属性

    /// <summary>
    /// 页面标题
    /// </summary>
    [Reactive]
    public string PageTitle { get; set; } = "个人练习";

    /// <summary>
    /// 练习类型列表
    /// </summary>
    public ObservableCollection<PracticeTypeItem> PracticeTypes { get; } = [];

    /// <summary>
    /// 选中的练习类型
    /// </summary>
    [Reactive]
    public PracticeTypeItem? SelectedPracticeType { get; set; }

    #endregion

    #region 命令

    /// <summary>
    /// 开始练习命令
    /// </summary>
    public ICommand StartPracticeCommand { get; }

    #endregion

    #region 构造函数

    public PracticeViewModel()
    {
        StartPracticeCommand = new DelegateCommand<PracticeTypeItem>(StartPractice, CanStartPractice);

        InitializePracticeTypes();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 初始化练习类型
    /// </summary>
    private void InitializePracticeTypes()
    {
        PracticeTypes.Clear();
        
        PracticeTypes.Add(new PracticeTypeItem
        {
            Id = "mock-exam",
            Name = "模拟考试",
            Description = "完整的模拟考试，包含所有题型",
            Icon = "📝",
            IsEnabled = true
        });

        PracticeTypes.Add(new PracticeTypeItem
        {
            Id = "comprehensive-training",
            Name = "综合实训",
            Description = "综合性实训练习，提升综合能力",
            Icon = "🎯",
            IsEnabled = true
        });

        PracticeTypes.Add(new PracticeTypeItem
        {
            Id = "special-practice",
            Name = "专项练习",
            Description = "针对特定知识点的专项练习",
            Icon = "🔍",
            IsEnabled = true
        });
    }

    /// <summary>
    /// 开始练习
    /// </summary>
    private void StartPractice(PracticeTypeItem? practiceType)
    {
        if (practiceType == null) return;

        // TODO: 实现开始练习逻辑
        // 根据练习类型导航到相应的练习页面
    }

    /// <summary>
    /// 是否可以开始练习
    /// </summary>
    private bool CanStartPractice(PracticeTypeItem? practiceType)
    {
        return practiceType?.IsEnabled == true;
    }

    #endregion
}

/// <summary>
/// 练习类型项目
/// </summary>
public class PracticeTypeItem
{
    /// <summary>
    /// 练习类型ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 练习类型名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 练习类型描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
