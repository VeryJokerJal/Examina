using System.Collections.ObjectModel;
using ReactiveUI;

namespace Examina.ViewModels;

/// <summary>
/// 答案解析视图模型
/// </summary>
public class AnswerAnalysisViewModel : ViewModelBase
{
    private string _title = string.Empty;
    private string _content = string.Empty;
    private string _examName = string.Empty;
    private string _questionTitle = string.Empty;

    /// <summary>
    /// 窗口标题
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// 答案解析内容
    /// </summary>
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
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
    /// 题目标题
    /// </summary>
    public string QuestionTitle
    {
        get => _questionTitle;
        set => this.RaiseAndSetIfChanged(ref _questionTitle, value);
    }

    /// <summary>
    /// 题目列表
    /// </summary>
    public ObservableCollection<QuestionItem> Questions { get; } = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    public AnswerAnalysisViewModel()
    {
        Title = "答案解析";
    }

    /// <summary>
    /// 设置答案解析数据
    /// </summary>
    /// <param name="examName">考试名称</param>
    /// <param name="questions">题目列表</param>
    public void SetAnswerAnalysisData(string examName, IEnumerable<QuestionItem> questions)
    {
        ExamName = examName;
        Title = $"答案解析 - {examName}";

        Questions.Clear();
        foreach (QuestionItem question in questions)
        {
            Questions.Add(question);
        }
    }
}

/// <summary>
/// 题目项
/// </summary>
public class QuestionItem
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容（答案解析）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int SortOrder { get; set; }
}
