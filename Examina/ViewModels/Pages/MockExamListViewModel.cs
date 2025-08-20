using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Examina.Models.MockExam;
using Examina.Models;
using Examina.Services;
using Examina.Views.Dialogs;
using Examina.ViewModels.Dialogs;

namespace Examina.ViewModels.Pages;

/// <summary>
/// 模拟考试列表视图模型
/// </summary>
public class MockExamListViewModel : ViewModelBase
{
    private readonly IStudentMockExamService _mockExamService;
    private readonly IAuthenticationService _authenticationService;

    private bool _isLoading;
    private string? _errorMessage;
    private bool _hasFullAccess;
    private bool _isUpdatingPermissions = false;

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
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
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
    /// 开始按钮文本
    /// </summary>
    public string StartButtonText => HasFullAccess ? "开始模拟考试" : "解锁";

    /// <summary>
    /// 开始模拟考试命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartMockExamCommand { get; }

    public MockExamListViewModel(IStudentMockExamService mockExamService, IAuthenticationService authenticationService)
    {
        _mockExamService = mockExamService;
        _authenticationService = authenticationService;

        // 初始化命令
        StartMockExamCommand = ReactiveCommand.CreateFromTask(StartMockExamAsync, this.WhenAnyValue(x => x.IsLoading).Select(loading => !loading));

        // 初始化用户权限状态
        _ = UpdateUserPermissionsAsync();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
    }

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    private async Task StartMockExamAsync()
    {
        try
        {
            if (!HasFullAccess)
            {
                // 用户没有完整权限，显示解锁提示
                ErrorMessage = "您需要解锁权限才能开始模拟考试。请加入学校组织或联系管理员进行解锁。";
                System.Diagnostics.Debug.WriteLine("用户尝试开始模拟考试但没有完整权限");
                return;
            }

            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 准备开始模拟考试");

            // 显示规则说明对话框
            MockExamRulesViewModel rulesViewModel = new();
            MockExamRulesDialog dialog = new(rulesViewModel);

            // 设置对话框的父窗口
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 准备显示规则对话框");

                bool? result = await dialog.ShowDialog<bool?>(desktop.MainWindow);

                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 对话框返回结果: {result}");

                if (result == true)
                {
                    System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 用户确认开始模拟考试");
                    // 用户确认开始，调用快速开始API
                    await QuickStartMockExamAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 用户取消了模拟考试");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 无法获取主窗口");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 开始模拟考试异常: {ex.Message}");
            ErrorMessage = "开始模拟考试失败，请稍后重试";
        }
    }

    /// <summary>
    /// 快速开始模拟考试
    /// </summary>
    private async Task QuickStartMockExamAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 开始快速生成模拟考试");

            StudentMockExamDto? mockExam = await _mockExamService.QuickStartMockExamAsync();
            if (mockExam != null)
            {
                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 成功生成模拟考试，ID: {mockExam.Id}");

                // TODO: 导航到考试页面
                // 这里应该导航到实际的考试界面
                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 模拟考试已开始，包含 {mockExam.Questions.Count} 道题目");
            }
            else
            {
                ErrorMessage = "生成模拟考试失败，请检查题库或稍后重试";
                System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 快速开始模拟考试失败");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 快速开始模拟考试异常: {ex.Message}");
            ErrorMessage = "生成模拟考试失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 更新用户权限状态
    /// </summary>
    private async Task UpdateUserPermissionsAsync()
    {
        // 防重入机制：如果正在更新权限状态，则跳过
        if (_isUpdatingPermissions)
        {
            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 权限状态正在更新中，跳过重复调用");
            return;
        }

        try
        {
            _isUpdatingPermissions = true;
            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 开始更新用户权限状态");

            // 主动刷新用户信息以获取最新状态
            bool refreshSuccess = await _authenticationService.RefreshUserInfoAsync();

            if (refreshSuccess)
            {
                // 刷新成功，获取最新的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 用户信息刷新成功 - HasFullAccess: {HasFullAccess}");
            }
            else
            {
                // 刷新失败，使用当前缓存的用户信息
                UserInfo? currentUser = _authenticationService.CurrentUser;
                HasFullAccess = currentUser?.HasFullAccess ?? false;

                System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 用户信息刷新失败，使用缓存信息 - HasFullAccess: {HasFullAccess}");
            }

            // 通知UI更新按钮文本
            this.RaisePropertyChanged(nameof(StartButtonText));
        }
        catch (Exception ex)
        {
            // 异常处理：使用默认值
            HasFullAccess = false;
            this.RaisePropertyChanged(nameof(StartButtonText));

            System.Diagnostics.Debug.WriteLine($"MockExamListViewModel: 更新用户权限状态异常: {ex.Message}");
        }
        finally
        {
            _isUpdatingPermissions = false;
            System.Diagnostics.Debug.WriteLine("MockExamListViewModel: 权限状态更新完成");
        }
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        _ = UpdateUserPermissionsAsync();
    }
}
