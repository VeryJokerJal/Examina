using System;
using System.Collections.ObjectModel;
using System.Linq;
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

    private readonly IAuthenticationService _authenticationService;

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
    [Reactive]
    public ViewModelBase? CurrentPageViewModel { get; set; }

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

    public MainViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;

        LogoutCommand = new DelegateCommand(Logout);
        UnlockAdsCommand = new DelegateCommand(UnlockAds);

        InitializeNavigation();
        LoadCurrentUser();

        // 监听用户信息更新事件
        _authenticationService.UserInfoUpdated += OnUserInfoUpdated;

        // 监听导航项选择变化
        _ = this.WhenAnyValue(x => x.SelectedNavigationItem)
            .Where(item => item != null)
            .Subscribe(item => OnNavigationSelectionChanged(item!));
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

        // 加入学校
        FooterNavigationItems.Add(new NavigationViewItem
        {
            Content = "加入学校",
            IconSource = new SymbolIconSource { Symbol = Symbol.Library },
            Tag = "school-binding"
        });

        // 个人信息
        FooterNavigationItems.Add(new NavigationViewItem
        {
            Content = "个人信息",
            IconSource = new SymbolIconSource { Symbol = Symbol.Contact },
            Tag = "profile"
        });
    }

    /// <summary>
    /// 加载当前用户信息
    /// </summary>
    private void LoadCurrentUser()
    {
        CurrentUser = _authenticationService.CurrentUser;
    }

    /// <summary>
    /// 用户信息更新事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="userInfo">更新后的用户信息</param>
    private void OnUserInfoUpdated(object? sender, UserInfo? userInfo)
    {
        CurrentUser = userInfo;
        System.Diagnostics.Debug.WriteLine($"MainViewModel: 用户信息已更新，用户名={userInfo?.Username}");
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
            "school-binding" => new SchoolBindingViewModel(),
            "profile" => ((App)Application.Current!).GetService<ProfileViewModel>() ?? new ProfileViewModel(_authenticationService),
            _ => new OverviewViewModel()
        };
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    private async void Logout()
    {
        try
        {
            await _authenticationService.LogoutAsync();

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
        _authenticationService.UserInfoUpdated -= OnUserInfoUpdated;
    }

    #endregion
}
