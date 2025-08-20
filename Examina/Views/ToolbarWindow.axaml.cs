using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Examina.Services;
using Examina.ViewModels;

namespace Examina.Views;

/// <summary>
/// 可重复使用的工具栏窗口组件
/// </summary>
public partial class ToolbarWindow : Window
{
    private readonly ScreenReservationService _screenReservation = new();
    private ToolbarWindowViewModel? _viewModel;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ToolbarWindow()
    {
        InitializeComponent();
        InitializeWindow();
        SetupEventHandlers();
    }

    /// <summary>
    /// 带ViewModel的构造函数
    /// </summary>
    /// <param name="viewModel">工具栏窗口的ViewModel</param>
    public ToolbarWindow(ToolbarWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // 订阅ViewModel的关闭请求事件
        _viewModel.CloseRequested += OnCloseRequested;
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
        
        // 取消订阅ViewModel事件
        if (_viewModel != null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }
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
            double toolbarHeight = _viewModel?.ToolbarHeight ?? 50;
            
            Width = screenWidth;
            Height = toolbarHeight;

            // 更新ViewModel中的尺寸信息
            _viewModel?.UpdateSize(screenWidth, toolbarHeight);

            // 预留屏幕区域（如果启用）
            if (_viewModel?.IsScreenReservationEnabled == true)
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
    /// 处理ViewModel的关闭请求
    /// </summary>
    private void OnCloseRequested()
    {
        Close();
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
        Show();
        _viewModel?.ShowCommand.Execute().Subscribe();
    }

    /// <summary>
    /// 隐藏工具栏
    /// </summary>
    public void HideToolbar()
    {
        Hide();
        _viewModel?.HideCommand.Execute().Subscribe();
    }

    /// <summary>
    /// 切换工具栏可见性
    /// </summary>
    public void ToggleToolbarVisibility()
    {
        if (IsVisible)
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
        Width = width;
        Height = height;
        _viewModel?.UpdateSize(width, height);
    }

    /// <summary>
    /// 启用或禁用屏幕预留
    /// </summary>
    /// <param name="enabled">是否启用屏幕预留</param>
    public void SetScreenReservationEnabled(bool enabled)
    {
        if (_viewModel != null)
        {
            _viewModel.IsScreenReservationEnabled = enabled;
            
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
    }
}
