using System.Reactive;
using Examina.Models;
using Examina.Models.SpecializedTraining;
using Examina.Services;
using Examina.Views;
using ReactiveUI;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 专项训练详情ViewModel
/// </summary>
public class SpecializedTrainingDetailViewModel : ViewModelBase
{
    private readonly IStudentSpecializedTrainingService _studentSpecializedTrainingService;
    private readonly IAuthenticationService _authenticationService;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private StudentSpecializedTrainingDto? _training;
    private bool _hasFullAccess;
    private int _trainingId;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// 专项训练详情
    /// </summary>
    public StudentSpecializedTrainingDto? Training
    {
        get => _training;
        set => this.RaiseAndSetIfChanged(ref _training, value);
    }

    /// <summary>
    /// 用户是否拥有完整功能权限
    /// </summary>
    public bool HasFullAccess
    {
        get => _hasFullAccess;
        set => this.RaiseAndSetIfChanged(ref _hasFullAccess, value);
    }

    /// <summary>
    /// 训练ID
    /// </summary>
    public int TrainingId
    {
        get => _trainingId;
        set => this.RaiseAndSetIfChanged(ref _trainingId, value);
    }

    /// <summary>
    /// 是否有训练数据
    /// </summary>
    public bool HasTraining => Training != null;

    /// <summary>
    /// 开始训练按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始训练" : "解锁";

    /// <summary>
    /// 是否可以开始训练
    /// </summary>
    public bool CanStartTraining => HasFullAccess && HasTraining;

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    /// <summary>
    /// 开始训练命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartTrainingCommand { get; }

    /// <summary>
    /// 返回命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    public SpecializedTrainingDetailViewModel(
        IStudentSpecializedTrainingService studentSpecializedTrainingService,
        IAuthenticationService authenticationService)
    {
        _studentSpecializedTrainingService = studentSpecializedTrainingService;
        _authenticationService = authenticationService;

        // 初始化命令
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));
        StartTrainingCommand = ReactiveCommand.CreateFromTask(StartTrainingAsync, this.WhenAnyValue(x => x.CanStartTraining, x => x.IsLoading, (canStart, loading) => canStart && !loading));
        BackCommand = ReactiveCommand.Create(GoBack);

        // 初始化用户权限状态
        UpdateUserPermissions();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 监听属性变化
        this.WhenAnyValue(x => x.Training)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasTraining)));

        this.WhenAnyValue(x => x.HasFullAccess, x => x.HasTraining)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(CanStartTraining)));
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    public async Task InitializeAsync(int trainingId)
    {
        TrainingId = trainingId;
        await RefreshAsync();
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            StudentSpecializedTrainingDto? training = await _studentSpecializedTrainingService.GetTrainingDetailsAsync(TrainingId);
            if (training != null)
            {
                Training = training;
                System.Diagnostics.Debug.WriteLine($"加载专项训练详情成功: {training.Name}");
            }
            else
            {
                ErrorMessage = "专项训练不存在或您没有权限查看";
                System.Diagnostics.Debug.WriteLine($"专项训练不存在或无权限访问，训练ID: {TrainingId}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"加载专项训练详情失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 开始训练
    /// </summary>
    private async Task StartTrainingAsync()
    {
        if (!HasFullAccess)
        {
            ErrorMessage = "您需要解锁权限才能开始专项训练。请加入学校组织或联系管理员进行解锁。";
            return;
        }

        if (Training == null)
        {
            ErrorMessage = "训练数据不存在";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            bool success = await _studentSpecializedTrainingService.StartSpecializedTrainingAsync(Training.Id);
            if (success)
            {
                // TODO: 启动BenchSuite进行实际训练
                System.Diagnostics.Debug.WriteLine($"开始专项训练: {Training.Name}");
                
                // 这里应该集成BenchSuite来执行实际的训练
                // 参考MockExamViewModel中的实现方式
                await StartBenchSuiteTrainingAsync();
            }
            else
            {
                ErrorMessage = "开始训练失败，请稍后重试";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"开始训练失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"开始专项训练失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 启动BenchSuite训练
    /// </summary>
    private async Task StartBenchSuiteTrainingAsync()
    {
        try
        {
            if (Training == null) return;

            System.Diagnostics.Debug.WriteLine($"启动BenchSuite专项训练: {Training.Name}");
            System.Diagnostics.Debug.WriteLine($"模块类型: {Training.ModuleType}");
            System.Diagnostics.Debug.WriteLine($"题目数量: {Training.QuestionCount}");
            System.Diagnostics.Debug.WriteLine($"预计时长: {Training.Duration}分钟");

            // 创建考试工具栏ViewModel
            ExamToolbarViewModel toolbarViewModel = new()
            {
                ExamId = Training.Id,
                ExamName = Training.Name,
                TotalQuestions = Training.QuestionCount,
                DurationMinutes = Training.Duration,
                CurrentExamType = ExamType.SpecializedTraining,
                IsTimerEnabled = true,
                ShowQuestionDetails = true
            };

            // 设置模块信息
            await SetSpecializedTrainingModuleInformationAsync(toolbarViewModel, Training);

            // 创建考试工具栏窗口
            ExamToolbarWindow examToolbar = new();
            examToolbar.SetViewModel(toolbarViewModel);

            System.Diagnostics.Debug.WriteLine($"专项训练工具栏已配置 - 训练ID: {Training.Id}, 题目数: {Training.QuestionCount}, 时长: {Training.Duration}分钟");

            // 订阅考试事件
            examToolbar.ExamAutoSubmitted += OnTrainingAutoSubmitted;
            examToolbar.ExamManualSubmitted += OnTrainingManualSubmitted;
            examToolbar.ViewQuestionsRequested += (sender, e) => OnViewQuestionsRequested(Training);

            System.Diagnostics.Debug.WriteLine("已订阅专项训练工具栏事件");

            // 显示工具栏窗口
            examToolbar.Show();
            System.Diagnostics.Debug.WriteLine("专项训练工具栏窗口已显示");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启动BenchSuite训练失败: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 设置专项训练模块信息
    /// </summary>
    private async Task SetSpecializedTrainingModuleInformationAsync(ExamToolbarViewModel toolbarViewModel, StudentSpecializedTrainingDto training)
    {
        try
        {
            // 将专项训练的模块和题目信息转换为工具栏可用的格式
            List<MockExamQuestionDetailsViewModel> questionDetails = [];

            // 处理模块中的题目
            foreach (StudentSpecializedTrainingModuleDto module in training.Modules)
            {
                foreach (StudentSpecializedTrainingQuestionDto question in module.Questions)
                {
                    questionDetails.Add(new MockExamQuestionDetailsViewModel
                    {
                        QuestionId = question.Id,
                        Title = question.Title,
                        Content = question.Content,
                        ModuleName = module.Name,
                        ModuleType = module.Type,
                        Score = (int)question.Score,
                        EstimatedMinutes = question.EstimatedMinutes,
                        IsRequired = question.IsRequired
                    });
                }
            }

            // 处理直接的题目（不在模块中的）
            foreach (StudentSpecializedTrainingQuestionDto question in training.Questions)
            {
                questionDetails.Add(new MockExamQuestionDetailsViewModel
                {
                    QuestionId = question.Id,
                    Title = question.Title,
                    Content = question.Content,
                    ModuleName = training.ModuleType,
                    ModuleType = training.ModuleType,
                    Score = (int)question.Score,
                    EstimatedMinutes = question.EstimatedMinutes,
                    IsRequired = question.IsRequired
                });
            }

            toolbarViewModel.QuestionDetails = questionDetails;
            System.Diagnostics.Debug.WriteLine($"专项训练模块信息设置完成，共 {questionDetails.Count} 道题目");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"设置专项训练模块信息失败: {ex}");
        }
    }

    /// <summary>
    /// 处理训练自动提交事件
    /// </summary>
    private async void OnTrainingAutoSubmitted(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"专项训练自动提交，ID: {viewModel.ExamId}");
                await SubmitTrainingWithBenchSuiteAsync(viewModel.ExamId, isAutoSubmit: true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理专项训练自动提交事件失败: {ex}");
        }
    }

    /// <summary>
    /// 处理训练手动提交事件
    /// </summary>
    private async void OnTrainingManualSubmitted(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ExamToolbarWindow examToolbar && examToolbar.DataContext is ExamToolbarViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"专项训练手动提交，ID: {viewModel.ExamId}");
                await SubmitTrainingWithBenchSuiteAsync(viewModel.ExamId, isAutoSubmit: false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理专项训练手动提交事件失败: {ex}");
        }
    }

    /// <summary>
    /// 使用BenchSuite评分提交训练
    /// </summary>
    private async Task SubmitTrainingWithBenchSuiteAsync(int trainingId, bool isAutoSubmit)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"开始提交专项训练，ID: {trainingId}, 自动提交: {isAutoSubmit}");

            // TODO: 集成BenchSuite评分系统
            // 这里应该调用BenchSuite来获取实际的评分结果

            // 模拟评分结果
            decimal score = 85.5m;
            decimal maxScore = 100m;
            int durationSeconds = 1800;

            bool success = await _studentSpecializedTrainingService.CompleteSpecializedTrainingAsync(
                trainingId, score, maxScore, durationSeconds, isAutoSubmit ? "自动提交" : "手动提交");

            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"专项训练提交成功，得分: {score}/{maxScore}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("专项训练提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交专项训练失败: {ex}");
        }
    }

    /// <summary>
    /// 处理查看题目详情请求
    /// </summary>
    private void OnViewQuestionsRequested(StudentSpecializedTrainingDto training)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练题目详情: {training.Name}");
            // TODO: 实现题目详情窗口
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"查看专项训练题目详情失败: {ex}");
        }
    }



    /// <summary>
    /// 返回
    /// </summary>
    private void GoBack()
    {
        System.Diagnostics.Debug.WriteLine("返回专项训练列表");

        // 触发返回事件，由主窗口处理导航
        BackRequested?.Invoke();
    }

    /// <summary>
    /// 返回请求事件
    /// </summary>
    public event Action? BackRequested;

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private void UpdateUserPermissions()
    {
        if (_authenticationService.IsAuthenticated && _authenticationService.CurrentUser != null)
        {
            HasFullAccess = _authenticationService.CurrentUser.HasFullAccess;
        }
        else
        {
            HasFullAccess = false;
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated()
    {
        UpdateUserPermissions();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _authenticationService.UserInfoUpdated -= OnUserInfoUpdated;
    }
}
