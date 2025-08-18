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

    public MainViewModel(IAuthenticationService? authenticationService = null)
    {
        _authenticationService = authenticationService;

        LogoutCommand = new DelegateCommand(Logout);
        UnlockAdsCommand = new DelegateCommand(UnlockAds);

        InitializeNavigation();

        // 监听用户信息更新事件
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated += OnUserInfoUpdated;
        }

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
        NavigateToPage("overview");
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

        // 测试：强制导航到SchoolBindingView
        System.Diagnostics.Debug.WriteLine("MainViewModel: 测试导航到SchoolBindingView");
        TestNavigateToSchoolBinding();
    }

    /// <summary>
    /// 测试方法：导航到SchoolBindingView
    /// </summary>
    public void TestNavigateToSchoolBinding()
    {
        System.Diagnostics.Debug.WriteLine("MainViewModel: TestNavigateToSchoolBinding called");
        NavigateToPage("school-binding");
        System.Diagnostics.Debug.WriteLine($"MainViewModel: CurrentPageViewModel after navigation = {CurrentPageViewModel?.GetType().Name ?? "null"}");
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
    /// 导航选择改变事件处理
    /// </summary>
    private void OnNavigationSelectionChanged(NavigationViewItem selectedItem)
    {
        string? tag = selectedItem.Tag?.ToString();
        if (!string.IsNullOrEmpty(tag))
        {
            NavigateToPage(tag);
        }
    }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    private void NavigateToPage(string pageTag)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel: 导航到页面 {pageTag}");

            CurrentPageViewModel = pageTag switch
            {
                "overview" => new OverviewViewModel(),
                "exam" => new ExamViewModel(),
                "practice" => new PracticeViewModel(),
                "mock-exam" => new PracticeViewModel(), // 可以传递参数区分类型
                "comprehensive-training" => new PracticeViewModel(),
                "special-practice" => new PracticeViewModel(),
                "leaderboard" => new LeaderboardViewModel(),
                "exam-ranking" => new LeaderboardViewModel(),
                "mock-exam-ranking" => new LeaderboardViewModel(),
                "training-ranking" => new LeaderboardViewModel(),
                "school-binding" => CreateSchoolBindingViewModel(),
                "profile" => CreateProfileViewModel(),
                _ => new OverviewViewModel()
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
    /// 退出登录
    /// </summary>
    private async void Logout()
    {
        try
        {
            if (_authenticationService != null)
            {
                await _authenticationService.LogoutAsync();
            }

            // TODO: 导航回登录页面
            // 这里需要通过应用程序的主窗口管理器来切换窗口
        }
        catch (Exception ex)
        {
            // TODO: 显示错误消息
            System.Diagnostics.Debug.WriteLine($"退出登录失败: {ex.Message}");
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_authenticationService != null)
        {
            _authenticationService.UserInfoUpdated -= OnUserInfoUpdated;
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}
