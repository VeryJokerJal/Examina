using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Examina.Models;
using Examina.Services;
using Examina.ViewModels.Pages;
using FluentAvalonia.UI.Controls;
using Prism.Commands;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    #region 字段

    private readonly IAuthenticationService? _authenticationService;
    private readonly IWindowManagerService? _windowManagerService;
    private readonly Func<LeaderboardViewModel>? _leaderboardViewModelFactory;
    private readonly Func<string, LeaderboardViewModel>? _leaderboardViewModelWithTypeFactory;

    #endregion

    #region 属性

    /// <summary>
    /// 导航项目集合
    /// </summary>
    public ObservableCollection<NavigationViewItem> NavigationItems { get; } = [];

    /// <summary>
    /// 底部导航项目集合
    /// </summary>
    public ObservableCollection<NavigationViewItem> FooterNavigationItems { get; } = [];

    /// <summary>
    /// 选中的导航项
    /// </summary>
    [Reactive]
    public NavigationViewItem? SelectedNavigationItem { get; set; }

    /// <summary>
    /// 当前页面视图模型
    /// </summary>
    public ViewModelBase? CurrentPageViewModel
    {
        get => _currentPageViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentPageViewModel, value);
        }
    }
    private ViewModelBase? _currentPageViewModel;

    /// <summary>
    /// 当前用户
    /// </summary>
    [Reactive]
    public UserInfo? CurrentUser { get; set; }

    /// <summary>
    /// NavigationView是否展开
    /// </summary>
    [Reactive]
    public bool IsNavigationPaneOpen { get; set; } = true;

    #endregion

    #region 命令

    /// <summary>
    /// 退出登录命令
    /// </summary>
    public ICommand LogoutCommand { get; }

    /// <summary>
    /// 解锁广告命令
    /// </summary>
    public ICommand UnlockAdsCommand { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 无参构造函数，用于设计时或直接实例化
    /// </summary>
    public MainViewModel() : this(null!)
    {
    }

    public MainViewModel(IAuthenticationService? authenticationService = null, IWindowManagerService? windowManagerService = null, Func<LeaderboardViewModel>? leaderboardViewModelFactory = null, Func<string, LeaderboardViewModel>? leaderboardViewModelWithTypeFactory = null)
    {
        _authenticationService = authenticationService;
        _windowManagerService = windowManagerService;
        _leaderboardViewModelFactory = leaderboardViewModelFactory;
        _leaderboardViewModelWithTypeFactory = leaderboardViewModelWithTypeFactory;

        LogoutCommand = new DelegateCommand(Logout);
        UnlockAdsCommand = new DelegateCommand(UnlockAds);

        InitializeNavigation();

        // 监听用户信息更新事件
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
        }

        // 监听概览页面刷新请求事件
        MockExamViewModel.OverviewPageRefreshRequested += OnOverviewPageRefreshRequested;

        // 监听排行榜页面刷新请求事件
        MockExamViewModel.LeaderboardPageRefreshRequested += OnLeaderboardPageRefreshRequested;

        // 监听导航项选择变化
        _ = this.WhenAnyValue(x => x.SelectedNavigationItem)
            .Where(item => item != null)
            .Subscribe(item => OnNavigationSelectionChanged(item!));

        // 异步加载用户信息
        _ = LoadCurrentUserAsync();
    }

    private void UnlockAds()
    {

    }

    #endregion

    #region 方法

    /// <summary>
    /// 初始化导航
    /// </summary>
    private void InitializeNavigation()
    {
        NavigationItems.Clear();

        // 概览
        NavigationItems.Add(new NavigationViewItem
        {
            Content = "概览",
            IconSource = new SymbolIconSource { Symbol = Symbol.Home },
            Tag = "overview"
        });

        // 上机统考
        NavigationItems.Add(new NavigationViewItem
        {
            Content = "上机统考",
            IconSource = new SymbolIconSource { Symbol = Symbol.Document },
            Tag = "exam"
        });

        // 个人练习（带子菜单）
        NavigationViewItem practiceItem = new()
        {
            Content = "个人练习",
            IconSource = new SymbolIconSource { Symbol = Symbol.Edit },
            Tag = "practice"
        };

        practiceItem.MenuItems.Add(new NavigationViewItem
        {
            Content = "模拟考试",
            IconSource = new SymbolIconSource { Symbol = Symbol.Document },
            Tag = "mock-exam"
        });

        practiceItem.MenuItems.Add(new NavigationViewItem
        {
            Content = "综合实训",
            IconSource = new SymbolIconSource { Symbol = Symbol.Target },
            Tag = "comprehensive-training"
        });

        practiceItem.MenuItems.Add(new NavigationViewItem
        {
            Content = "专项训练",
            IconSource = new SymbolIconSource { Symbol = Symbol.Find },
            Tag = "special-practice"
        });

        NavigationItems.Add(practiceItem);

        // 排行榜（带子菜单）
        NavigationViewItem leaderboardItem = new()
        {
            Content = "排行榜",
            IconSource = new SymbolIconSource { Symbol = Symbol.List },
            Tag = "leaderboard"
        };

        leaderboardItem.MenuItems.Add(new NavigationViewItem
        {
            Content = "上机统考排行",
            IconSource = new SymbolIconSource { Symbol = Symbol.List },
            Tag = "exam-ranking"
        });

        leaderboardItem.MenuItems.Add(new NavigationViewItem
        {
            Content = "模拟考试排行",
            IconSource = new SymbolIconSource { Symbol = Symbol.View },
            Tag = "mock-exam-ranking"
        });

        leaderboardItem.MenuItems.Add(new NavigationViewItem
        {
            Content = "综合实训排行",
            IconSource = new SymbolIconSource { Symbol = Symbol.Target },
            Tag = "training-ranking"
        });

        NavigationItems.Add(leaderboardItem);

        // 初始化底部导航项
        InitializeFooterNavigation();

        // 默认选择第一个项目
        SelectedNavigationItem = NavigationItems.FirstOrDefault();
        NavigateToPageInternal("overview");
    }

    /// <summary>
    /// 初始化底部导航项
    /// </summary>
    private void InitializeFooterNavigation()
    {
        FooterNavigationItems.Clear();

        // 根据用户权限状态决定是否显示"加入学校"
        if (CurrentUser?.HasFullAccess != true)
        {
            // 用户没有完整权限，显示"加入学校"选项
            FooterNavigationItems.Add(new NavigationViewItem
            {
                Content = "加入学校",
                IconSource = new SymbolIconSource { Symbol = Symbol.Library },
                Tag = "school-binding"
            });
        }

        // 个人信息
        FooterNavigationItems.Add(new NavigationViewItem
        {
            Content = "个人信息",
            IconSource = new SymbolIconSource { Symbol = Symbol.Contact },
            Tag = "profile"
        });
    }

    /// <summary>
    /// 异步加载当前用户信息
    /// </summary>
    private async Task LoadCurrentUserAsync()
    {
        try
        {
            if (_authenticationService == null)
            {
                return;
            }

            // 首先尝试从AuthenticationService获取当前用户
            CurrentUser = _authenticationService.CurrentUser;

            // 如果CurrentUser为null，尝试从本地存储加载
            if (CurrentUser == null)
            {
                // 检查是否有本地登录数据
                PersistentLoginData? loginData = await _authenticationService.LoadLoginDataAsync();
                if (loginData != null && loginData.User != null)
                {
                    CurrentUser = loginData.User;
                }
            }

            if (CurrentUser != null)
            {
                // 重新初始化底部导航以反映用户权限状态
                InitializeFooterNavigation();
            }
        }
        catch (Exception ex)
        {
            // 加载用户信息时发生异常，静默处理
        }
    }

    /// <summary>
    /// 加载当前用户信息（同步版本，保留向后兼容性）
    /// </summary>
    private void LoadCurrentUser()
    {
        CurrentUser = _authenticationService?.CurrentUser;
    }

    /// <summary>
    /// 初始化MainViewModel（在导航到MainWindow时调用）
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCurrentUserAsync();
    }

    /// <summary>
    /// 测试方法：导航到SchoolBindingView
    /// </summary>
    public void TestNavigateToSchoolBinding()
    {
        NavigateToPageInternal("school-binding");
    }

    /// <summary>
    /// 测试方法：设置简单的测试ViewModel
    /// </summary>
    public void TestSetSimpleViewModel()
    {
        CurrentPageViewModel = new OverviewViewModel();
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="userInfo">更新后的用户信息</param>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        bool previousHasFullAccess = CurrentUser?.HasFullAccess ?? false;
        CurrentUser = userInfo;
        bool currentHasFullAccess = userInfo?.HasFullAccess ?? false;

        // 如果用户权限状态发生变化，重新初始化底部导航
        if (previousHasFullAccess != currentHasFullAccess)
        {
            InitializeFooterNavigation();
        }
    }

    /// <summary>
    /// 概览页面刷新请求事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private async void OnOverviewPageRefreshRequested(object? sender, EventArgs e)
    {
        try
        {
            // 如果当前页面是概览页面，刷新数据
            if (CurrentPageViewModel is OverviewViewModel overviewViewModel)
            {
                await overviewViewModel.RefreshStatisticsAsync();
            }
        }
        catch (Exception ex)
        {
            // 处理概览页面刷新请求异常，静默处理
        }
    }

    /// <summary>
    /// 排行榜页面刷新请求事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private async void OnLeaderboardPageRefreshRequested(object? sender, EventArgs e)
    {
        try
        {
            // 如果当前页面是排行榜页面，刷新数据
            if (CurrentPageViewModel is LeaderboardViewModel leaderboardViewModel)
            {
                // 使用Task.Run来异步调用刷新方法
                await Task.Run(() => leaderboardViewModel.RefreshLeaderboardCommand?.Execute(null));
            }
        }
        catch (Exception ex)
        {
            // 处理排行榜页面刷新请求异常，静默处理
        }
    }

    /// <summary>
    /// 导航选择改变事件处理
    /// </summary>
    private void OnNavigationSelectionChanged(NavigationViewItem selectedItem)
    {
        string? tag = selectedItem.Tag?.ToString();
        if (!string.IsNullOrEmpty(tag))
        {
            NavigateToPageInternal(tag);
        }
    }

    /// <summary>
    /// 导航到指定页面（公共方法）
    /// </summary>
    /// <param name="pageTag">页面标签</param>
    public void NavigateToPage(string pageTag)
    {
        NavigateToPageInternal(pageTag);
    }

    /// <summary>
    /// 导航到指定页面（内部实现）
    /// </summary>
    private void NavigateToPageInternal(string pageTag)
    {
        try
        {
            CurrentPageViewModel = pageTag switch
            {
                "overview" => CreateOverviewViewModel(),
                "exam" => CreateUnifiedExamViewModel(),
                "practice" => CreatePracticeViewModel(),
                "mock-exam" => CreateMockExamViewModel(),

                "comprehensive-training" => CreateComprehensiveTrainingListViewModel(),
                "special-practice" => CreateSpecializedTrainingListViewModel(),
                "leaderboard" => CreateLeaderboardViewModel(),
                "exam-ranking" => CreateLeaderboardViewModel("exam-ranking"),
                "mock-exam-ranking" => CreateLeaderboardViewModel("mock-exam-ranking"),
                "training-ranking" => CreateLeaderboardViewModel("training-ranking"),
                "school-binding" => CreateSchoolBindingViewModel(),
                "profile" => CreateProfileViewModel(),
                "exam-view" => CreateExamViewModel(),
                _ => CreateOverviewViewModel()
            };
        }
        catch (Exception ex)
        {
            CurrentPageViewModel = new OverviewViewModel(); // 回退到概览页面
        }
    }

    /// <summary>
    /// 创建OverviewViewModel实例
    /// </summary>
    private ViewModelBase? CreateOverviewViewModel()
    {
        try
        {
            // 获取综合实训服务
            IStudentComprehensiveTrainingService? comprehensiveTrainingService = ((App)Application.Current!).GetService<IStudentComprehensiveTrainingService>();

            // 获取学生考试服务（用于专项练习）
            IStudentExamService? studentExamService = ((App)Application.Current!).GetService<IStudentExamService>();

            // 获取模拟考试服务
            IStudentMockExamService? studentMockExamService = ((App)Application.Current!).GetService<IStudentMockExamService>();

            if (comprehensiveTrainingService != null && studentExamService != null && studentMockExamService != null)
            {
                return new OverviewViewModel(comprehensiveTrainingService, studentExamService, studentMockExamService);
            }
            else if (comprehensiveTrainingService != null && studentExamService != null)
            {
                return new OverviewViewModel(comprehensiveTrainingService, studentExamService);
            }
            else if (comprehensiveTrainingService != null)
            {
                return new OverviewViewModel(comprehensiveTrainingService);
            }
            else
            {
                return new OverviewViewModel();
            }
        }
        catch (Exception ex)
        {
            return new OverviewViewModel();
        }
    }

    /// <summary>
    /// 创建SchoolBindingViewModel实例
    /// </summary>
    private ViewModelBase? CreateSchoolBindingViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            SchoolBindingViewModel? viewModel = ((App)Application.Current!).GetService<SchoolBindingViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            if (_authenticationService != null)
            {
                IOrganizationService? organizationService = ((App)Application.Current!).GetService<IOrganizationService>();
                if (organizationService != null)
                {
                    return new SchoolBindingViewModel(organizationService, _authenticationService);
                }
            }
        }
        catch (Exception ex)
        {
            // 创建SchoolBindingViewModel时发生异常，静默处理
        }

        return null;
    }

    /// <summary>
    /// 创建ProfileViewModel实例
    /// </summary>
    private ViewModelBase? CreateProfileViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            ProfileViewModel? viewModel = ((App)Application.Current!).GetService<ProfileViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            if (_authenticationService != null)
            {
                return new ProfileViewModel(_authenticationService);
            }
        }
        catch (Exception ex)
        {
            // 创建ProfileViewModel时发生异常，静默处理
        }

        return null;
    }

    /// <summary>
    /// 创建ExamListViewModel实例
    /// </summary>
    private ViewModelBase? CreateExamListViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            ExamListViewModel? viewModel = ((App)Application.Current!).GetService<ExamListViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentExamService? examService = ((App)Application.Current!).GetService<IStudentExamService>();
            if (examService != null && _authenticationService != null)
            {
                IStudentFormalExamService? formalExamService = AppServiceManager.GetService<IStudentFormalExamService>();
                EnhancedExamToolbarService? enhancedService = AppServiceManager.GetService<EnhancedExamToolbarService>();

                if (formalExamService != null)
                {
                    return new ExamListViewModel(examService, formalExamService, _authenticationService, enhancedService);
                }
            }
        }
        catch (Exception ex)
        {
            // 创建ExamListViewModel时发生异常，静默处理
        }

        return null;
    }

    /// <summary>
    /// 创建UnifiedExamViewModel实例
    /// </summary>
    private ViewModelBase? CreateUnifiedExamViewModel()
    {
        try
        {
            // 直接创建UnifiedExamViewModel，避免循环依赖
            IStudentExamService? examService = ((App)Application.Current!).GetService<IStudentExamService>();
            IExamAttemptService? examAttemptService = ((App)Application.Current!).GetService<IExamAttemptService>();

            if (examService != null && _authenticationService != null)
            {
                return new UnifiedExamViewModel(examService, _authenticationService, examAttemptService, this);
            }
        }
        catch (Exception ex)
        {
            // 创建UnifiedExamViewModel时发生异常，静默处理
        }

        return null;
    }



    /// <summary>
    /// 创建ComprehensiveTrainingListViewModel实例
    /// </summary>
    private ViewModelBase? CreateComprehensiveTrainingListViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            ComprehensiveTrainingListViewModel? viewModel = ((App)Application.Current!).GetService<ComprehensiveTrainingListViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentComprehensiveTrainingService? trainingService = ((App)Application.Current!).GetService<IStudentComprehensiveTrainingService>();
            if (trainingService != null && _authenticationService != null)
            {
                return new ComprehensiveTrainingListViewModel(trainingService, _authenticationService);
            }
        }
        catch (Exception ex)
        {
            // 创建ComprehensiveTrainingListViewModel时发生异常，静默处理
        }

        return null;
    }

    /// <summary>
    /// 创建SpecializedTrainingListViewModel实例
    /// </summary>
    private ViewModelBase? CreateSpecializedTrainingListViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            SpecializedTrainingListViewModel? viewModel = ((App)Application.Current!).GetService<SpecializedTrainingListViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentSpecializedTrainingService? trainingService = ((App)Application.Current!).GetService<IStudentSpecializedTrainingService>();
            if (trainingService != null && _authenticationService != null)
            {
                SpecializedTrainingListViewModel newViewModel = new(trainingService, _authenticationService);
                return newViewModel;
            }
        }
        catch (Exception ex)
        {
            // 创建SpecializedTrainingListViewModel时发生异常，静默处理
        }

        return null;
    }



    /// <summary>
    /// 创建MockExamViewModel实例
    /// </summary>
    private ViewModelBase? CreateMockExamViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            MockExamViewModel? viewModel = ((App)Application.Current!).GetService<MockExamViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentMockExamService? mockExamService = ((App)Application.Current!).GetService<IStudentMockExamService>();
            if (mockExamService != null && _authenticationService != null)
            {
                return new MockExamViewModel(mockExamService, _authenticationService);
            }
        }
        catch (Exception ex)
        {
            // 创建MockExamViewModel时发生异常，静默处理
        }

        return null;
    }

    /// <summary>
    /// 创建PracticeViewModel实例
    /// </summary>
    private ViewModelBase CreatePracticeViewModel()
    {
        try
        {
            return new PracticeViewModel(this);
        }
        catch (Exception ex)
        {
            // 如果创建失败，返回无参构造的实例
            return new PracticeViewModel();
        }
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    private async void Logout()
    {
        try
        {
            // 调用认证服务退出登录
            if (_authenticationService != null)
            {
                await _authenticationService.LogoutAsync();
            }

            // 使用窗口管理服务导航回登录页面
            if (_windowManagerService != null)
            {
                _windowManagerService.NavigateToLogin();
            }
        }
        catch (Exception ex)
        {
            // 即使出现错误，也尝试导航到登录窗口
            try
            {
                _windowManagerService?.NavigateToLogin();
            }
            catch (Exception navEx)
            {
                // 导航到登录窗口失败，静默处理
            }
        }
    }

    /// <summary>
    /// 创建ExamViewModel实例
    /// </summary>
    private ViewModelBase? CreateExamViewModel()
    {
        try
        {
            // 首先尝试从DI容器获取
            ExamViewModel? viewModel = ((App)Application.Current!).GetService<ExamViewModel>();
            if (viewModel != null)
            {
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            return new ExamViewModel(_authenticationService);
        }
        catch (Exception ex)
        {
            // 创建ExamViewModel时发生异常，静默处理
        }

        return null;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 创建LeaderboardViewModel实例
    /// </summary>
    private ViewModelBase? CreateLeaderboardViewModel(string? rankingTypeId = null)
    {
        try
        {
            // 优先使用带类型的工厂方法，确保一次性正确初始化
            if (!string.IsNullOrEmpty(rankingTypeId) && _leaderboardViewModelWithTypeFactory != null)
            {
                LeaderboardViewModel viewModel = _leaderboardViewModelWithTypeFactory(rankingTypeId);
                return viewModel;
            }
            else if (_leaderboardViewModelFactory != null)
            {
                LeaderboardViewModel viewModel = _leaderboardViewModelFactory();

                // 如果指定了排行榜类型，设置对应的类型
                if (!string.IsNullOrEmpty(rankingTypeId))
                {
                    viewModel.SetRankingType(rankingTypeId);
                }
                else
                {
                    // 如果没有指定类型，手动触发初始数据加载
                    viewModel.LoadInitialData();
                }

                return viewModel;
            }
            else
            {
                // 尝试手动获取服务
                RankingService? rankingService = ((App)Application.Current!).GetService<RankingService>();
                IStudentComprehensiveTrainingService? comprehensiveTrainingService = ((App)Application.Current!).GetService<IStudentComprehensiveTrainingService>();
                IStudentExamService? studentExamService = ((App)Application.Current!).GetService<IStudentExamService>();
                IStudentMockExamService? studentMockExamService = ((App)Application.Current!).GetService<IStudentMockExamService>();

                LeaderboardViewModel viewModel;
                if (rankingService != null && comprehensiveTrainingService != null && studentExamService != null)
                {
                    viewModel = new LeaderboardViewModel(rankingService, null, comprehensiveTrainingService, studentExamService, rankingTypeId, studentMockExamService);
                }
                else
                {
                    viewModel = new LeaderboardViewModel();
                }

                // 如果指定了排行榜类型，设置对应的类型
                if (!string.IsNullOrEmpty(rankingTypeId))
                {
                    viewModel.SetRankingType(rankingTypeId);
                }
                else
                {
                    // 如果没有指定类型，手动触发初始数据加载
                    viewModel.LoadInitialData();
                }

                return viewModel;
            }
        }
        catch (Exception ex)
        {
            // 尝试手动获取服务创建回退实例
            try
            {
                RankingService? rankingService = ((App)Application.Current!).GetService<RankingService>();
                IStudentComprehensiveTrainingService? comprehensiveTrainingService = ((App)Application.Current!).GetService<IStudentComprehensiveTrainingService>();
                IStudentExamService? studentExamService = ((App)Application.Current!).GetService<IStudentExamService>();
                IStudentMockExamService? studentMockExamService = ((App)Application.Current!).GetService<IStudentMockExamService>();

                LeaderboardViewModel fallbackViewModel;
                if (rankingService != null && comprehensiveTrainingService != null && studentExamService != null)
                {
                    fallbackViewModel = new LeaderboardViewModel(rankingService, null, comprehensiveTrainingService, studentExamService, rankingTypeId, studentMockExamService);
                }
                else
                {
                    fallbackViewModel = new LeaderboardViewModel();
                }

                fallbackViewModel.LoadInitialData(); // 确保回退实例也能加载数据
                return fallbackViewModel;
            }
            catch (Exception fallbackEx)
            {
                LeaderboardViewModel defaultViewModel = new LeaderboardViewModel();
                defaultViewModel.LoadInitialData();
                return defaultViewModel;
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated -= OnUserInfoUpdated;
        }

        // 取消概览页面刷新请求事件订阅
        MockExamViewModel.OverviewPageRefreshRequested -= OnOverviewPageRefreshRequested;

        GC.SuppressFinalize(this);
    }

    #endregion
}
