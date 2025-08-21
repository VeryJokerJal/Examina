using System.Collections.ObjectModel;
using ReactiveUI;
using Examina.Models.MockExam;

namespace Examina.ViewModels;

/// <summary>
/// 模拟考试题目详情视图模型
/// </summary>
public class MockExamQuestionDetailsViewModel : ViewModelBase
{
    private MockExamComprehensiveTrainingDto? _mockExamData;
    private MockExamModuleDto? _selectedModule;
    private MockExamQuestionDto? _selectedQuestion;
    private string _examTitle = string.Empty;
    private string _examDescription = string.Empty;

    /// <summary>
    /// 模拟考试数据
    /// </summary>
    public MockExamComprehensiveTrainingDto? MockExamData
    {
        get => _mockExamData;
        set => this.RaiseAndSetIfChanged(ref _mockExamData, value);
    }

    /// <summary>
    /// 选中的模块
    /// </summary>
    public MockExamModuleDto? SelectedModule
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
    public MockExamQuestionDto? SelectedQuestion
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
    public ObservableCollection<MockExamModuleDto> Modules { get; } = [];

    /// <summary>
    /// 当前模块的题目列表
    /// </summary>
    public ObservableCollection<MockExamQuestionDto> CurrentModuleQuestions { get; } = [];

    /// <summary>
    /// 总题目数
    /// </summary>
    public int TotalQuestions => MockExamData?.Modules.Sum(m => m.Questions.Count) ?? 0;

    /// <summary>
    /// 总分值
    /// </summary>
    public decimal TotalScore => MockExamData?.TotalScore ?? 0;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes => MockExamData?.DurationMinutes ?? 0;

    /// <summary>
    /// 模块数量
    /// </summary>
    public int ModuleCount => MockExamData?.Modules.Count ?? 0;

    public MockExamQuestionDetailsViewModel()
    {
        // 监听选中模块的变化，更新题目列表
        this.WhenAnyValue(x => x.SelectedModule)
            .Subscribe(module =>
            {
                CurrentModuleQuestions.Clear();
                if (module?.Questions != null)
                {
                    foreach (MockExamQuestionDto question in module.Questions)
                    {
                        CurrentModuleQuestions.Add(question);
                    }
                }
            });
    }

    /// <summary>
    /// 设置模拟考试数据
    /// </summary>
    public void SetMockExamData(MockExamComprehensiveTrainingDto mockExamData)
    {
        try
        {
            MockExamData = mockExamData;
            ExamTitle = mockExamData.Name;
            ExamDescription = mockExamData.Description ?? "无描述";

            // 更新模块列表
            Modules.Clear();
            foreach (MockExamModuleDto module in mockExamData.Modules)
            {
                Modules.Add(module);
            }

            // 默认选择第一个模块
            if (Modules.Count > 0)
            {
                SelectedModule = Modules[0];
            }

            // 通知属性变化
            this.RaisePropertyChanged(nameof(TotalQuestions));
            this.RaisePropertyChanged(nameof(TotalScore));
            this.RaisePropertyChanged(nameof(DurationMinutes));
            this.RaisePropertyChanged(nameof(ModuleCount));

            System.Diagnostics.Debug.WriteLine($"MockExamQuestionDetailsViewModel: 数据已设置");
            System.Diagnostics.Debug.WriteLine($"  考试名称: {ExamTitle}");
            System.Diagnostics.Debug.WriteLine($"  模块数量: {ModuleCount}");
            System.Diagnostics.Debug.WriteLine($"  总题目数: {TotalQuestions}");
            System.Diagnostics.Debug.WriteLine($"  总分值: {TotalScore}");

            // 记录每个模块的详细信息
            foreach (MockExamModuleDto module in mockExamData.Modules)
            {
                System.Diagnostics.Debug.WriteLine($"  模块: {module.Name} ({module.Type})");
                System.Diagnostics.Debug.WriteLine($"    描述: {module.Description ?? "无描述"}");
                System.Diagnostics.Debug.WriteLine($"    题目数: {module.Questions.Count}");
                System.Diagnostics.Debug.WriteLine($"    分值: {module.Score}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamQuestionDetailsViewModel: 设置数据异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取模块显示名称
    /// </summary>
    public string GetModuleDisplayName(MockExamModuleDto module)
    {
        if (module == null) return "未知模块";
        
        string displayName = module.Name;
        if (!string.IsNullOrEmpty(module.Description))
        {
            displayName += $" - {module.Description}";
        }
        
        return displayName;
    }

    /// <summary>
    /// 获取模块类型显示名称
    /// </summary>
    public string GetModuleTypeDisplayName(string moduleType)
    {
        return moduleType?.ToLower() switch
        {
            "ppt" or "powerpoint" => "PowerPoint演示文稿",
            "word" => "Word文档处理",
            "excel" => "Excel电子表格",
            "csharp" => "C#编程",
            "windows" => "Windows操作系统",
            _ => moduleType ?? "未知类型"
        };
    }

    // 题目类型和难度显示方法已移除，不再需要显示这些信息

    /// <summary>
    /// 格式化题目内容（限制长度）
    /// </summary>
    public string FormatQuestionContent(string content, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(content))
            return "无内容";

        if (content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...";
    }
}
