using System.Collections.ObjectModel;
using System.Linq;
using Examina.Models.Exam;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels;

/// <summary>
/// 考试题目详情视图模型（通用版本，支持正式考试和模拟考试）
/// </summary>
public class ExamQuestionDetailsViewModel : ViewModelBase
{
    private StudentExamDto? _examData;
    private StudentModuleDto? _selectedModule;
    private StudentQuestionDto? _selectedQuestion;
    private string _examTitle = string.Empty;
    private string _examDescription = string.Empty;

    /// <summary>
    /// 考试数据
    /// </summary>
    public StudentExamDto? ExamData
    {
        get => _examData;
        set => this.RaiseAndSetIfChanged(ref _examData, value);
    }

    /// <summary>
    /// 选中的模块
    /// </summary>
    public StudentModuleDto? SelectedModule
    {
        get => _selectedModule;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedModule, value);
            // 当选择模块时，清空选中的题目
            SelectedQuestion = null;
        }
    }

    /// <summary>
    /// 选中的题目
    /// </summary>
    public StudentQuestionDto? SelectedQuestion
    {
        get => _selectedQuestion;
        set => this.RaiseAndSetIfChanged(ref _selectedQuestion, value);
    }

    /// <summary>
    /// 考试标题
    /// </summary>
    public string ExamTitle
    {
        get => _examTitle;
        set => this.RaiseAndSetIfChanged(ref _examTitle, value);
    }

    /// <summary>
    /// 考试描述
    /// </summary>
    public string ExamDescription
    {
        get => _examDescription;
        set => this.RaiseAndSetIfChanged(ref _examDescription, value);
    }

    /// <summary>
    /// 模块列表
    /// </summary>
    public ObservableCollection<StudentModuleDto> Modules { get; } = [];

    /// <summary>
    /// 当前模块的题目列表
    /// </summary>
    public ObservableCollection<StudentQuestionDto> CurrentModuleQuestions { get; } = [];

    /// <summary>
    /// 是否有选中的题目
    /// </summary>
    public bool HasSelectedQuestion => SelectedQuestion != null;

    /// <summary>
    /// 是否有选中的模块
    /// </summary>
    public bool HasSelectedModule => SelectedModule != null;

    /// <summary>
    /// 模块总数
    /// </summary>
    public int ModuleCount => Modules.Count;

    /// <summary>
    /// 题目总数
    /// </summary>
    public int TotalQuestionCount => Modules.Sum(m => m.Questions.Count);

    /// <summary>
    /// 当前模块题目数
    /// </summary>
    public int CurrentModuleQuestionCount => CurrentModuleQuestions.Count;

    /// <summary>
    /// 模块信息文本
    /// </summary>
    public string ModuleInfoText => HasSelectedModule 
        ? $"{SelectedModule!.Name} - {CurrentModuleQuestionCount} 道题目"
        : "请选择模块";

    /// <summary>
    /// 题目信息文本
    /// </summary>
    public string QuestionInfoText => HasSelectedQuestion 
        ? $"题目 {SelectedQuestion!.SortOrder}: {SelectedQuestion.Title}"
        : "请选择题目";

    public ExamQuestionDetailsViewModel()
    {
        // 监听选中模块的变化，更新题目列表
        this.WhenAnyValue(x => x.SelectedModule)
            .Subscribe(module =>
            {
                CurrentModuleQuestions.Clear();
                if (module?.Questions != null)
                {
                    foreach (StudentQuestionDto question in module.Questions)
                    {
                        CurrentModuleQuestions.Add(question);
                    }
                }
                
                // 触发属性更新
                this.RaisePropertyChanged(nameof(HasSelectedModule));
                this.RaisePropertyChanged(nameof(CurrentModuleQuestionCount));
                this.RaisePropertyChanged(nameof(ModuleInfoText));
            });

        // 监听选中题目的变化
        this.WhenAnyValue(x => x.SelectedQuestion)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasSelectedQuestion));
                this.RaisePropertyChanged(nameof(QuestionInfoText));
            });
    }

    /// <summary>
    /// 设置考试数据
    /// </summary>
    public void SetExamData(StudentExamDto examData)
    {
        try
        {
            ExamData = examData;
            ExamTitle = examData.Name;
            ExamDescription = examData.Description ?? "无描述";

            // 更新模块列表
            Modules.Clear();
            foreach (StudentModuleDto module in examData.Modules)
            {
                Modules.Add(module);
            }

            // 默认选择第一个模块
            if (Modules.Count > 0)
            {
                SelectedModule = Modules[0];
            }

            // 触发属性更新
            this.RaisePropertyChanged(nameof(ModuleCount));
            this.RaisePropertyChanged(nameof(TotalQuestionCount));

            System.Diagnostics.Debug.WriteLine($"ExamQuestionDetailsViewModel: 设置考试数据 - {examData.Name}, 模块数: {Modules.Count}, 总题目数: {TotalQuestionCount}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExamQuestionDetailsViewModel: 设置考试数据异常: {ex.Message}");
        }
    }
}
