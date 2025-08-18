using System.Reactive;
using ReactiveUI;

namespace Examina.ViewModels.Dialogs;

/// <summary>
/// 模拟考试规则说明对话框视图模型
/// </summary>
public class MockExamRulesViewModel : ViewModelBase
{
    /// <summary>
    /// 确认开始命令
    /// </summary>
    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public ReactiveCommand<Unit, bool> CancelCommand { get; }

    /// <summary>
    /// 考试规则信息
    /// </summary>
    public MockExamRulesInfo RulesInfo { get; }

    public MockExamRulesViewModel()
    {
        // 初始化规则信息
        RulesInfo = new MockExamRulesInfo();

        // 初始化命令
        ConfirmCommand = ReactiveCommand.Create(() => true);
        CancelCommand = ReactiveCommand.Create(() => false);
    }
}

/// <summary>
/// 模拟考试规则信息
/// </summary>
public class MockExamRulesInfo
{
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 总分值
    /// </summary>
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    public int PassingScore { get; set; } = 60;

    /// <summary>
    /// 题目总数
    /// </summary>
    public int TotalQuestions { get; set; } = 10;

    /// <summary>
    /// 编程题数量
    /// </summary>
    public int ProgrammingQuestions { get; set; } = 5;

    /// <summary>
    /// 编程题分值
    /// </summary>
    public int ProgrammingScore { get; set; } = 15;

    /// <summary>
    /// 操作题数量
    /// </summary>
    public int OperationQuestions { get; set; } = 5;

    /// <summary>
    /// 操作题分值
    /// </summary>
    public int OperationScore { get; set; } = 5;

    /// <summary>
    /// 考试规则列表
    /// </summary>
    public List<string> Rules { get; set; } = new()
    {
        "考试时间为2小时（120分钟），请合理安排答题时间",
        "考试总分100分，及格分数为60分",
        "题目从综合训练题库中随机抽取，确保公平性",
        "包含C#编程题5道（每道15分）和操作题5道（每道5分）",
        "题目顺序已随机打乱，请仔细阅读每道题目",
        "考试过程中请保持网络连接稳定",
        "考试开始后不能暂停，请确保有足够的时间完成",
        "系统会自动保存答题进度，避免意外丢失",
        "考试结束后可查看详细的成绩报告和解析"
    };

    /// <summary>
    /// 注意事项列表
    /// </summary>
    public List<string> Notes { get; set; } = new()
    {
        "请确保计算机性能良好，避免卡顿影响答题",
        "建议使用Chrome或Edge浏览器以获得最佳体验",
        "考试期间请关闭其他不必要的应用程序",
        "如遇到技术问题，请及时联系技术支持",
        "请诚信考试，独立完成所有题目",
        "考试结果将作为学习评估的重要参考"
    };

    /// <summary>
    /// 操作指南列表
    /// </summary>
    public List<string> OperationGuide { get; set; } = new()
    {
        "点击题目可查看详细内容和要求",
        "编程题需要在代码编辑器中完成代码编写",
        "操作题需要按照步骤完成相应的操作任务",
        "可以随时切换题目，系统会自动保存当前进度",
        "完成所有题目后，点击'提交考试'结束考试",
        "提交前请仔细检查所有题目的完成情况"
    };
}
