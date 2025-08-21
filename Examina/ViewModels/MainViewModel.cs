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
            System.Diagnostics.Debug.WriteLine($"MainViewModel: CurrentPageViewModel setter called, old: {_currentPageViewModel?.GetType().Name ?? "null"}, new: {value?.GetType().Name ?? "null"}");
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

    public MainViewModel(IAuthenticationService? authenticationService = null, IWindowManagerService? windowManagerService = null, Func<LeaderboardViewModel>? leaderboardViewModelFactory = null)
    {
        _authenticationService = authenticationService;
        _windowManagerService = windowManagerService;
        _leaderboardViewModelFactory = leaderboardViewModelFactory;

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
            Content = "专项练习",
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: AuthenticationService为null");
                return;
            }

            // 首先尝试从AuthenticationService获取当前用户
            CurrentUser = _authenticationService.CurrentUser;

            // 如果CurrentUser为null，尝试从本地存储加载
            if (CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: CurrentUser为null，尝试从本地存储加载");

                // 检查是否有本地登录数据
                PersistentLoginData? loginData = await _authenticationService.LoadLoginDataAsync();
                if (loginData != null && loginData.User != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MainViewModel: 从本地存储加载到用户信息: {loginData.User.Username}");
                    CurrentUser = loginData.User;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainViewModel: 本地存储中没有用户信息");
                }
            }

            if (CurrentUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"MainViewModel: 成功加载用户信息: {CurrentUser.Username}, HasFullAccess: {CurrentUser.HasFullAccess}");

                // 重新初始化底部导航以反映用户权限状态
                InitializeFooterNavigation();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 无法加载用户信息");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 加载用户信息时发生异常: {ex.Message}");
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
        System.Diagnostics.Debug.WriteLine("MainViewModel.InitializeAsync called");
        await LoadCurrentUserAsync();
    }

    /// <summary>
    /// 测试方法：导航到SchoolBindingView
    /// </summary>
    public void TestNavigateToSchoolBinding()
    {
        System.Diagnostics.Debug.WriteLine("MainViewModel: TestNavigateToSchoolBinding called");
        NavigateToPageInternal("school-binding");
        System.Diagnostics.Debug.WriteLine($"MainViewModel: CurrentPageViewModel after navigation = {CurrentPageViewModel?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// 测试方法：设置简单的测试ViewModel
    /// </summary>
    public void TestSetSimpleViewModel()
    {
        System.Diagnostics.Debug.WriteLine("MainViewModel: TestSetSimpleViewModel called");
        CurrentPageViewModel = new OverviewViewModel();
        System.Diagnostics.Debug.WriteLine($"MainViewModel: CurrentPageViewModel set to = {CurrentPageViewModel?.GetType().Name ?? "null"}");
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

        System.Diagnostics.Debug.WriteLine($"MainViewModel: 用户信息已更新，用户名={userInfo?.Username}，权限状态={currentHasFullAccess}");

        // 如果用户权限状态发生变化，重新初始化底部导航
        if (previousHasFullAccess != currentHasFullAccess)
        {
            InitializeFooterNavigation();
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 用户权限状态变化，重新初始化底部导航");
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
            System.Diagnostics.Debug.WriteLine("MainViewModel: 收到概览页面刷新请求");

            // 如果当前页面是概览页面，刷新数据
            if (CurrentPageViewModel is OverviewViewModel overviewViewModel)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 当前页面是概览页面，开始刷新统计数据");
                await overviewViewModel.RefreshStatisticsAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 当前页面不是概览页面，跳过刷新");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 处理概览页面刷新请求异常: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 导航到页面 {pageTag}");

            CurrentPageViewModel = pageTag switch
            {
                "overview" => CreateOverviewViewModel(),
                "exam" => CreateExamListViewModel(),
                "practice" => CreatePracticeViewModel(),
                "mock-exam" => CreateMockExamViewModel(),

                "comprehensive-training" => CreateComprehensiveTrainingListViewModel(),
                "special-practice" => CreatePracticeViewModel(),
                "leaderboard" => CreateLeaderboardViewModel(),
                "exam-ranking" => CreateLeaderboardViewModel(),
                "mock-exam-ranking" => CreateLeaderboardViewModel(),
                "training-ranking" => CreateLeaderboardViewModel(),
                "school-binding" => CreateSchoolBindingViewModel(),
                "profile" => CreateProfileViewModel(),
                "exam-view" => CreateExamViewModel(),
                _ => CreateOverviewViewModel()
            };

            System.Diagnostics.Debug.WriteLine($"MainViewModel: 成功创建页面ViewModel: {CurrentPageViewModel?.GetType().Name ?? "null"}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 导航到页面 {pageTag} 时发生异常: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("MainViewModel: 开始创建OverviewViewModel");

            // 获取综合实训服务
            IStudentComprehensiveTrainingService? comprehensiveTrainingService = ((App)Application.Current!).GetService<IStudentComprehensiveTrainingService>();

            // 获取学生考试服务（用于专项练习）
            IStudentExamService? studentExamService = ((App)Application.Current!).GetService<IStudentExamService>();

            // 获取模拟考试服务
            IStudentMockExamService? studentMockExamService = ((App)Application.Current!).GetService<IStudentMockExamService>();

            if (comprehensiveTrainingService != null && studentExamService != null && studentMockExamService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 成功获取所有服务，创建带完整服务注入的OverviewViewModel");
                return new OverviewViewModel(comprehensiveTrainingService, studentExamService, studentMockExamService);
            }
            else if (comprehensiveTrainingService != null && studentExamService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 获取到综合实训和考试服务，创建带部分服务注入的OverviewViewModel");
                return new OverviewViewModel(comprehensiveTrainingService, studentExamService);
            }
            else if (comprehensiveTrainingService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 仅获取到综合实训服务，创建部分服务注入的OverviewViewModel");
                return new OverviewViewModel(comprehensiveTrainingService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 无法获取服务，创建默认OverviewViewModel");
                return new OverviewViewModel();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建OverviewViewModel失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: 从DI容器成功获取SchoolBindingViewModel");
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            if (_authenticationService != null)
            {
                IOrganizationService? organizationService = ((App)Application.Current!).GetService<IOrganizationService>();
                if (organizationService != null)
                {
                    System.Diagnostics.Debug.WriteLine("MainViewModel: 手动创建SchoolBindingViewModel");
                    return new SchoolBindingViewModel(organizationService, _authenticationService);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainViewModel: 无法获取IOrganizationService，无法创建SchoolBindingViewModel");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: AuthenticationService为null，无法创建SchoolBindingViewModel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建SchoolBindingViewModel时发生异常: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: 从DI容器成功获取ProfileViewModel");
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            if (_authenticationService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 手动创建ProfileViewModel");
                return new ProfileViewModel(_authenticationService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: AuthenticationService为null，无法创建ProfileViewModel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建ProfileViewModel时发生异常: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: 从DI容器成功获取ExamListViewModel");
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentExamService? examService = ((App)Application.Current!).GetService<IStudentExamService>();
            if (examService != null && _authenticationService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 手动创建ExamListViewModel");
                return new ExamListViewModel(examService, _authenticationService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 无法获取IStudentExamService，无法创建ExamListViewModel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建ExamListViewModel时发生异常: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: 从DI容器成功获取ComprehensiveTrainingListViewModel");
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentComprehensiveTrainingService? trainingService = ((App)Application.Current!).GetService<IStudentComprehensiveTrainingService>();
            if (trainingService != null && _authenticationService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 手动创建ComprehensiveTrainingListViewModel");
                return new ComprehensiveTrainingListViewModel(trainingService, _authenticationService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 无法获取IStudentComprehensiveTrainingService，无法创建ComprehensiveTrainingListViewModel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建ComprehensiveTrainingListViewModel时发生异常: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: 从DI容器成功获取MockExamViewModel");
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            IStudentMockExamService? mockExamService = ((App)Application.Current!).GetService<IStudentMockExamService>();
            if (mockExamService != null && _authenticationService != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 手动创建MockExamViewModel");
                return new MockExamViewModel(mockExamService, _authenticationService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 无法获取IStudentMockExamService，无法创建MockExamViewModel");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建MockExamViewModel时发生异常: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("MainViewModel: 创建PracticeViewModel，传递MainViewModel引用");
            return new PracticeViewModel(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建PracticeViewModel时发生异常: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("开始退出登录流程");

            // 调用认证服务退出登录
            if (_authenticationService != null)
            {
                await _authenticationService.LogoutAsync();
                System.Diagnostics.Debug.WriteLine("认证服务退出登录完成");
            }

            // 使用窗口管理服务导航回登录页面
            if (_windowManagerService != null)
            {
                _windowManagerService.NavigateToLogin();
                System.Diagnostics.Debug.WriteLine("已导航到登录窗口");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("窗口管理服务未注入，无法导航到登录窗口");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"退出登录失败: {ex.Message}");

            // 即使出现错误，也尝试导航到登录窗口
            try
            {
                _windowManagerService?.NavigateToLogin();
            }
            catch (Exception navEx)
            {
                System.Diagnostics.Debug.WriteLine($"导航到登录窗口失败: {navEx.Message}");
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
                System.Diagnostics.Debug.WriteLine("MainViewModel: 从DI容器成功获取ExamViewModel");
                return viewModel;
            }

            // 如果DI容器无法提供，手动创建
            System.Diagnostics.Debug.WriteLine("MainViewModel: 手动创建ExamViewModel");
            return new ExamViewModel(_authenticationService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建ExamViewModel时发生异常: {ex.Message}");
        }

        return null;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 创建LeaderboardViewModel实例
    /// </summary>
    private ViewModelBase? CreateLeaderboardViewModel()
    {
        try
        {
            if (_leaderboardViewModelFactory != null)
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 使用工厂创建LeaderboardViewModel");
                return _leaderboardViewModelFactory();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainViewModel: 工厂未注入，创建默认LeaderboardViewModel");
                return new LeaderboardViewModel();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 创建LeaderboardViewModel失败: {ex.Message}");
            return new LeaderboardViewModel(); // 回退到默认实例
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
