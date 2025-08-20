using System;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Examina.ViewModels;

/// <summary>
/// 工具栏窗口的ViewModel
/// </summary>
public class ToolbarWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 工具栏是否可见
    /// </summary>
    [Reactive] public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 工具栏透明度
    /// </summary>
    [Reactive] public double Opacity { get; set; } = 0.8;

    /// <summary>
    /// 工具栏高度
    /// </summary>
    [Reactive] public double ToolbarHeight { get; set; } = 50;

    /// <summary>
    /// 工具栏宽度
    /// </summary>
    [Reactive] public double ToolbarWidth { get; set; }

    /// <summary>
    /// 是否置顶
    /// </summary>
    [Reactive] public bool IsTopmost { get; set; } = true;

    /// <summary>
    /// 是否启用屏幕预留
    /// </summary>
    [Reactive] public bool IsScreenReservationEnabled { get; set; } = true;

    /// <summary>
    /// 工具栏标题
    /// </summary>
    [Reactive] public string ToolbarTitle { get; set; } = "工具栏";

    /// <summary>
    /// 显示工具栏命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowCommand { get; }

    /// <summary>
    /// 隐藏工具栏命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> HideCommand { get; }

    /// <summary>
    /// 切换可见性命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleVisibilityCommand { get; }

    /// <summary>
    /// 关闭工具栏命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    /// <summary>
    /// 最小化命令（防止最小化）
    /// </summary>
    public ReactiveCommand<Unit, Unit> PreventMinimizeCommand { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ToolbarWindowViewModel()
    {
        // 初始化命令
        ShowCommand = ReactiveCommand.Create(Show);
        HideCommand = ReactiveCommand.Create(Hide);
        ToggleVisibilityCommand = ReactiveCommand.Create(ToggleVisibility);
        CloseCommand = ReactiveCommand.Create(Close);
        PreventMinimizeCommand = ReactiveCommand.Create(PreventMinimize);

        // 设置默认工具栏宽度为屏幕宽度
        SetDefaultWidth();
    }

    /// <summary>
    /// 显示工具栏
    /// </summary>
    private void Show()
    {
        IsVisible = true;
    }

    /// <summary>
    /// 隐藏工具栏
    /// </summary>
    private void Hide()
    {
        IsVisible = false;
    }

    /// <summary>
    /// 切换工具栏可见性
    /// </summary>
    private void ToggleVisibility()
    {
        IsVisible = !IsVisible;
    }

    /// <summary>
    /// 关闭工具栏
    /// </summary>
    private void Close()
    {
        // 触发关闭事件，由View处理实际的关闭逻辑
        CloseRequested?.Invoke();
    }

    /// <summary>
    /// 防止最小化
    /// </summary>
    private void PreventMinimize()
    {
        // 这个方法由View调用，用于防止窗口最小化
        // 实际的防止最小化逻辑在View中实现
    }

    /// <summary>
    /// 设置默认宽度
    /// </summary>
    private void SetDefaultWidth()
    {
        try
        {
            // 获取主屏幕宽度
            Avalonia.Platform.Screen? primaryScreen = Avalonia.Controls.Screens.Primary;
            if (primaryScreen != null)
            {
                ToolbarWidth = primaryScreen.Bounds.Width;
            }
            else
            {
                // 如果无法获取屏幕信息，使用默认值
                ToolbarWidth = 1920;
            }
        }
        catch
        {
            // 异常情况下使用默认值
            ToolbarWidth = 1920;
        }
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
    /// 关闭请求事件
    /// </summary>
    public event Action? CloseRequested;
}
