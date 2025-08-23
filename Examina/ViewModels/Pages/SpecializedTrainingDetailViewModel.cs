using System.Reactive;
using Examina.Models.SpecializedTraining;
using Examina.Services;
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

            // TODO: 实现BenchSuite集成
            // 1. 将专项训练数据转换为BenchSuite格式
            // 2. 启动BenchSuite执行训练
            // 3. 监听训练完成事件
            // 4. 提交训练结果

            System.Diagnostics.Debug.WriteLine($"启动BenchSuite专项训练: {Training.Name}");
            System.Diagnostics.Debug.WriteLine($"模块类型: {Training.ModuleType}");
            System.Diagnostics.Debug.WriteLine($"题目数量: {Training.QuestionCount}");
            System.Diagnostics.Debug.WriteLine($"预计时长: {Training.Duration}分钟");

            // 模拟训练过程
            await Task.Delay(1000);
            
            // 训练完成后的处理
            await OnTrainingCompletedAsync(85.5m, 100m, 1800); // 示例：得分85.5，满分100，用时1800秒
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启动BenchSuite训练失败: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 训练完成处理
    /// </summary>
    private async Task OnTrainingCompletedAsync(decimal score, decimal maxScore, int durationSeconds)
    {
        try
        {
            if (Training == null) return;

            bool success = await _studentSpecializedTrainingService.CompleteSpecializedTrainingAsync(
                Training.Id, score, maxScore, durationSeconds, "专项训练完成");

            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"专项训练完成记录提交成功，得分: {score}/{maxScore}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("专项训练完成记录提交失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提交专项训练完成记录失败: {ex}");
        }
    }

    /// <summary>
    /// 返回
    /// </summary>
    private void GoBack()
    {
        // TODO: 实现返回导航逻辑
        System.Diagnostics.Debug.WriteLine("返回专项训练列表");
    }

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
