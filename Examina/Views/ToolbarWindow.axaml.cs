using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Examina.Services;

namespace Examina.Views;

/// <summary>
/// 可重复使用的工具栏窗口组件
/// </summary>
public partial class ToolbarWindow : Window
{
    private readonly ScreenReservationService _screenReservation;

    // 工具栏属性
    private bool _isToolbarVisible = true;
    private double _toolbarOpacity = 0.8;
    private double _toolbarHeight = 50;
    private double _toolbarWidth = 1920;
    private bool _isTopmost = true;
    private string _toolbarTitle = "工具栏";

    /// <summary>
    /// 工具栏是否可见
    /// </summary>
    public bool IsToolbarVisible
    {
        get => _isToolbarVisible;
        set
        {
            _isToolbarVisible = value;
            IsVisible = value;
        }
    }

    /// <summary>
    /// 工具栏透明度
    /// </summary>
    public double ToolbarOpacity
    {
        get => _toolbarOpacity;
        set
        {
            _toolbarOpacity = value;
            Opacity = value;
        }
    }

    /// <summary>
    /// 工具栏高度
    /// </summary>
    public double ToolbarHeight
    {
        get => _toolbarHeight;
        set
        {
            _toolbarHeight = value;
            Height = value;
        }
    }

    /// <summary>
    /// 工具栏宽度
    /// </summary>
    public double ToolbarWidth
    {
        get => _toolbarWidth;
        set
        {
            _toolbarWidth = value;
            Width = value;
        }
    }

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsToolbarTopmost
    {
        get => _isTopmost;
        set
        {
            _isTopmost = value;
            Topmost = value;
        }
    }

    /// <summary>
    /// 是否启用屏幕预留
    /// </summary>
    public bool IsScreenReservationEnabled { get; set; } = true;

    /// <summary>
    /// 工具栏标题
    /// </summary>
    public string ToolbarTitle
    {
        get => _toolbarTitle;
        set
        {
            _toolbarTitle = value;
            Title = value;
            TextBlock? titleTextBlock = this.FindControl<TextBlock>("ToolbarTitleTextBlock");
            if (titleTextBlock != null)
            {
                titleTextBlock.Text = value;
            }
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ToolbarWindow() : this(new ScreenReservationService())
    {
    }

    /// <summary>
    /// 带依赖注入的构造函数
    /// </summary>
    /// <param name="screenReservationService">屏幕预留服务</param>
    public ToolbarWindow(ScreenReservationService screenReservationService)
    {
        _screenReservation = screenReservationService ?? throw new ArgumentNullException(nameof(screenReservationService));

        InitializeComponent();
        InitializeWindow();
        SetupEventHandlers();
    }

    /// <summary>
    /// 初始化窗口属性
    /// </summary>
    private void InitializeWindow()
    {
        // 设置窗口基本属性
        SystemDecorations = SystemDecorations.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Topmost = true;
        Background = new SolidColorBrush(new Color(128, 60, 60, 60));
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = -1;
        TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
        CanResize = false;
    }

    /// <summary>
    /// 设置事件处理程序
    /// </summary>
    private void SetupEventHandlers()
    {
        // 防止窗口最小化
        PropertyChanged += ToolbarWindow_PropertyChanged;

        // 窗口打开时的处理
        Opened += ToolbarWindow_Opened;

        // 窗口关闭时的处理
        Closing += ToolbarWindow_Closing;

        // 设置按钮事件处理
        Button? toggleButton = this.FindControl<Button>("ToggleVisibilityButton");
        if (toggleButton != null)
        {
            toggleButton.Click += ToggleVisibilityButton_Click;
        }

        Button? closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.Click += CloseButton_Click;
        }
    }

    /// <summary>
    /// 窗口属性变化事件处理
    /// </summary>
    private void ToolbarWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // 防止窗口最小化
        if (e.Property.Name == nameof(WindowState) && WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
    }

    /// <summary>
    /// 窗口打开事件处理
    /// </summary>
    private void ToolbarWindow_Opened(object? sender, EventArgs e)
    {
        // 设置窗口位置到屏幕顶部
        Position = new PixelPoint(0, 0);

        // 设置窗口区域
        SetupWindowArea();
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void ToolbarWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        // 释放屏幕预留区域
        _screenReservation.Dispose();
    }

    /// <summary>
    /// 切换可见性按钮点击事件
    /// </summary>
    private void ToggleVisibilityButton_Click(object? sender, RoutedEventArgs e)
    {
        ToggleToolbarVisibility();
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 设置窗口区域和屏幕预留
    /// </summary>
    private void SetupWindowArea()
    {
        PixelSize? screenSize = Screens.Primary?.Bounds.Size;

        if (screenSize.HasValue)
        {
            double screenWidth = screenSize.Value.Width;
            double toolbarHeight = ToolbarHeight;

            ToolbarWidth = screenWidth;
            ToolbarHeight = toolbarHeight;

            // 预留屏幕区域（如果启用）
            if (IsScreenReservationEnabled)
            {
                bool reservationResult = _screenReservation.ReserveAreaOnSide((int)toolbarHeight, DockPosition.Top);

                if (!reservationResult)
                {
                    System.Diagnostics.Debug.WriteLine("ToolbarWindow: 屏幕区域预留失败");
                }
            }
        }
    }

    /// <summary>
    /// 设置工具栏内容
    /// </summary>
    /// <param name="content">要显示的内容控件</param>
    public void SetToolbarContent(Control content)
    {
        ContentPresenter? contentPresenter = this.FindControl<ContentPresenter>("ToolbarContentPresenter");
        if (contentPresenter != null)
        {
            contentPresenter.Content = content;
        }
    }

    /// <summary>
    /// 显示工具栏
    /// </summary>
    public void ShowToolbar()
    {
        IsToolbarVisible = true;
        Show();
    }

    /// <summary>
    /// 隐藏工具栏
    /// </summary>
    public void HideToolbar()
    {
        IsToolbarVisible = false;
        Hide();
    }

    /// <summary>
    /// 切换工具栏可见性
    /// </summary>
    public void ToggleToolbarVisibility()
    {
        if (IsToolbarVisible)
        {
            HideToolbar();
        }
        else
        {
            ShowToolbar();
        }
    }

    /// <summary>
    /// 更新工具栏位置
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    public void UpdatePosition(int x, int y)
    {
        Position = new PixelPoint(x, y);
    }

    /// <summary>
    /// 更新工具栏尺寸
    /// </summary>
    /// <param name="width">新的宽度</param>
    /// <param name="height">新的高度</param>
    public void UpdateSize(double width, double height)
    {
        ToolbarWidth = width;
        ToolbarHeight = height;
    }

    /// <summary>
    /// 启用或禁用屏幕预留
    /// </summary>
    /// <param name="enabled">是否启用屏幕预留</param>
    public void SetScreenReservationEnabled(bool enabled)
    {
        IsScreenReservationEnabled = enabled;

        if (enabled)
        {
            // 重新设置屏幕预留
            SetupWindowArea();
        }
        else
        {
            // 释放屏幕预留
            _screenReservation.Dispose();
        }
    }

    /// <summary>
    /// 设置工具栏透明度
    /// </summary>
    /// <param name="opacity">透明度值（0.0-1.0）</param>
    public void SetToolbarOpacity(double opacity)
    {
        ToolbarOpacity = Math.Clamp(opacity, 0.0, 1.0);
    }

    /// <summary>
    /// 设置工具栏置顶状态
    /// </summary>
    /// <param name="topmost">是否置顶</param>
    public void SetToolbarTopmost(bool topmost)
    {
        IsToolbarTopmost = topmost;
    }
}
