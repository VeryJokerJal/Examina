using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 个人练习页面视图模型
/// </summary>
public class PracticeViewModel : ViewModelBase
{
    #region 字段

    private readonly MainViewModel? _mainViewModel;

    #endregion

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

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Reactive]
    public bool IsLoading { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [Reactive]
    public string? ErrorMessage { get; set; }

    #endregion

    #region 命令

    /// <summary>
    /// 开始练习命令
    /// </summary>
    public ICommand StartPracticeCommand { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 无参构造函数，用于设计时
    /// </summary>
    public PracticeViewModel() : this(null)
    {
    }

    /// <summary>
    /// 带参数构造函数，用于运行时依赖注入
    /// </summary>
    /// <param name="mainViewModel">主视图模型，用于导航</param>
    public PracticeViewModel(MainViewModel? mainViewModel)
    {
        _mainViewModel = mainViewModel;

        StartPracticeCommand = new DelegateCommand<PracticeTypeItem>(StartPractice, CanStartPractice);

        InitializePracticeTypes();
    }

    #endregion

    #region 事件

    /// <summary>
    /// 导航请求事件
    /// </summary>
    public event Action<string>? NavigationRequested;

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
            Description = "完整的模拟考试体验，包含Windows操作、C#编程、Office应用等所有题型，模拟真实考试环境",
            Icon = "📝",
            IsEnabled = true,
            EstimatedDuration = "120分钟",
            DifficultyLevel = "中等",
            Features = ["完整考试流程", "计时功能", "自动评分", "详细报告"]
        });

        PracticeTypes.Add(new PracticeTypeItem
        {
            Id = "comprehensive-training",
            Name = "综合实训",
            Description = "综合性实训练习，结合多个知识点进行综合训练，提升实际操作能力和解决问题的能力",
            Icon = "🎯",
            IsEnabled = true,
            EstimatedDuration = "90分钟",
            DifficultyLevel = "中等偏难",
            Features = ["多模块训练", "实际场景", "技能提升", "综合评估"]
        });

        PracticeTypes.Add(new PracticeTypeItem
        {
            Id = "special-practice",
            Name = "专项练习",
            Description = "针对特定知识点和技能的专项强化训练，可选择Windows、C#、Word、Excel、PowerPoint等单项练习",
            Icon = "🔍",
            IsEnabled = true,
            EstimatedDuration = "30-60分钟",
            DifficultyLevel = "可选择",
            Features = ["单项训练", "难度可选", "快速提升", "针对性强"]
        });

        System.Diagnostics.Debug.WriteLine($"PracticeViewModel: 初始化了 {PracticeTypes.Count} 个练习类型");
    }

    /// <summary>
    /// 开始练习
    /// </summary>
    private void StartPractice(PracticeTypeItem? practiceType)
    {
        if (practiceType == null)
        {
            ErrorMessage = "请选择练习类型";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            System.Diagnostics.Debug.WriteLine($"PracticeViewModel: 开始练习 - {practiceType.Name} ({practiceType.Id})");

            // 根据练习类型导航到相应的练习页面
            string navigationTag = practiceType.Id switch
            {
                "mock-exam" => "mock-exam",
                "comprehensive-training" => "comprehensive-training",
                "special-practice" => "special-practice",
                _ => "practice"
            };

            // 使用MainViewModel进行导航
            if (_mainViewModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"PracticeViewModel: 通过MainViewModel导航到 {navigationTag}");
                _mainViewModel.NavigateToPage(navigationTag);
            }
            else
            {
                // 如果没有MainViewModel引用，尝试通过其他方式导航
                System.Diagnostics.Debug.WriteLine("PracticeViewModel: MainViewModel为null，尝试其他导航方式");

                // 可以通过事件或消息传递的方式通知主界面进行导航
                NavigationRequested?.Invoke(navigationTag);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PracticeViewModel: 开始练习时发生异常: {ex.Message}");
            ErrorMessage = $"启动练习失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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

    /// <summary>
    /// 预计用时
    /// </summary>
    public string EstimatedDuration { get; set; } = string.Empty;

    /// <summary>
    /// 难度等级
    /// </summary>
    public string DifficultyLevel { get; set; } = string.Empty;

    /// <summary>
    /// 功能特性列表
    /// </summary>
    public List<string> Features { get; set; } = [];

    /// <summary>
    /// 是否为推荐练习
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// 练习次数统计
    /// </summary>
    public int PracticeCount { get; set; }

    /// <summary>
    /// 最佳成绩
    /// </summary>
    public double? BestScore { get; set; }
}
