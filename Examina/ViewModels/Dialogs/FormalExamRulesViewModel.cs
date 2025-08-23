using System.Reactive;
using ReactiveUI;

namespace Examina.ViewModels.Dialogs;

/// <summary>
/// 上机统考规则说明对话框视图模型
/// </summary>
public class FormalExamRulesViewModel : ViewModelBase
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
    public FormalExamRulesInfo RulesInfo { get; }

    public FormalExamRulesViewModel()
    {
        // 初始化规则信息
        RulesInfo = new FormalExamRulesInfo();

        // 初始化命令
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("FormalExamRulesViewModel: 确认命令被执行");
            return true;
        });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            System.Diagnostics.Debug.WriteLine("FormalExamRulesViewModel: 取消命令被执行");
            return false;
        });
    }
}

/// <summary>
/// 上机统考规则信息
/// </summary>
public class FormalExamRulesInfo
{
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 150;

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
        "考试时间为2.5小时（150分钟），请合理安排答题时间",
        "考试总分100分，及格分数为60分",
        "本次为正式上机统考，成绩将记入正式档案",
        "包含C#编程题5道（每道15分）和操作题5道（每道5分）",
        "题目顺序已随机打乱，请仔细阅读每道题目",
        "考试过程中必须保持网络连接稳定",
        "考试开始后不能暂停或重新开始，请确保有足够的时间完成",
        "系统会自动保存答题进度，避免意外丢失",
        "考试时间结束后系统将自动提交，无法继续答题",
        "考试结束后可查看详细的成绩报告"
    };

    /// <summary>
    /// 注意事项列表
    /// </summary>
    public List<string> Notes { get; set; } = new()
    {
        "请确保计算机性能良好，避免卡顿影响答题",
        "建议使用Chrome或Edge浏览器以获得最佳体验",
        "考试期间请关闭其他不必要的应用程序",
        "考试期间禁止使用任何外部资料或工具",
        "如遇到技术问题，请立即举手联系监考老师",
        "请诚信考试，独立完成所有题目，严禁作弊",
        "考试结果将作为正式成绩记录",
        "迟到超过30分钟将不允许进入考场"
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
        "建议先浏览所有题目，合理分配答题时间",
        "完成所有题目后，点击'提交考试'结束考试",
        "提交前请仔细检查所有题目的完成情况",
        "考试提交后无法修改答案，请谨慎操作"
    };

    /// <summary>
    /// 考试要求列表
    /// </summary>
    public List<string> Requirements { get; set; } = new()
    {
        "考生必须携带有效身份证件进入考场",
        "考试开始前请确认个人信息无误",
        "考试期间不得离开座位，如有特殊情况请举手示意",
        "考试期间手机等电子设备必须关闭并上交",
        "考试结束后请等待监考老师确认提交成功",
        "违反考试纪律的行为将按相关规定严肃处理"
    };
}
