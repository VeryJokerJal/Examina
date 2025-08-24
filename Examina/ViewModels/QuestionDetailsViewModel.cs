using System.Collections.ObjectModel;
using ReactiveUI;

namespace Examina.ViewModels;

/// <summary>
/// 题目详情视图模型
/// </summary>
public class QuestionDetailsViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _examName = string.Empty;

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// 考试名称
    /// </summary>
    public string ExamName
    {
        get => _examName;
        set => this.RaiseAndSetIfChanged(ref _examName, value);
    }

    /// <summary>
    /// 模块列表
    /// </summary>
    public ObservableCollection<ModuleItem> Modules { get; } = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionDetailsViewModel()
    {
        Title = "题目详情";
    }

    /// <summary>
    /// 设置题目详情数据
    /// </summary>
    /// <param name="examName">考试名称</param>
    /// <param name="modules">模块列表</param>
    public void SetQuestionDetailsData(string examName, IEnumerable<ModuleItem> modules)
    {
        ExamName = examName;
        Title = $"题目详情 - {examName}";
        
        Modules.Clear();
        foreach (ModuleItem module in modules)
        {
            Modules.Add(module);
        }
    }
}

/// <summary>
/// 模块项
/// </summary>
public class ModuleItem
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
