using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Examina.Services;

namespace Examina.Views;

/// <summary>
/// ToolbarWindow 组件测试示例
/// </summary>
public static class ToolbarWindowTestExample
{
    /// <summary>
    /// 创建基本工具栏测试
    /// </summary>
    /// <returns>配置好的工具栏窗口</returns>
    public static ToolbarWindow CreateBasicToolbar()
    {
        // 创建窗口
        ToolbarWindow toolbarWindow = new ToolbarWindow();

        // 配置工具栏属性
        toolbarWindow.ToolbarTitle = "测试工具栏";
        toolbarWindow.ToolbarHeight = 50;
        toolbarWindow.ToolbarOpacity = 0.8;
        toolbarWindow.IsScreenReservationEnabled = true;

        return toolbarWindow;
    }

    /// <summary>
    /// 创建带自定义内容的工具栏测试
    /// </summary>
    /// <returns>配置好的工具栏窗口</returns>
    public static ToolbarWindow CreateToolbarWithCustomContent()
    {
        // 创建窗口
        ToolbarWindow toolbarWindow = new ToolbarWindow();

        // 配置工具栏属性
        toolbarWindow.ToolbarTitle = "考试系统工具栏";
        toolbarWindow.ToolbarHeight = 60;
        toolbarWindow.ToolbarOpacity = 0.9;
        toolbarWindow.IsScreenReservationEnabled = true;

        // 创建自定义内容
        Grid customContent = CreateCustomToolbarContent();
        toolbarWindow.SetToolbarContent(customContent);

        return toolbarWindow;
    }

    /// <summary>
    /// 创建带依赖注入的工具栏测试
    /// </summary>
    /// <param name="screenReservationService">屏幕预留服务</param>
    /// <returns>配置好的工具栏窗口</returns>
    public static ToolbarWindow CreateToolbarWithDependencyInjection(ScreenReservationService screenReservationService)
    {
        // 创建窗口（使用依赖注入）
        ToolbarWindow toolbarWindow = new ToolbarWindow(screenReservationService);

        // 配置工具栏属性
        toolbarWindow.ToolbarTitle = "依赖注入工具栏";
        toolbarWindow.ToolbarHeight = 55;
        toolbarWindow.ToolbarOpacity = 0.85;
        toolbarWindow.IsScreenReservationEnabled = true;

        return toolbarWindow;
    }

    /// <summary>
    /// 创建自定义工具栏内容
    /// </summary>
    /// <returns>自定义内容控件</returns>
    private static Grid CreateCustomToolbarContent()
    {
        Grid grid = new Grid();
        
        // 定义列
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        // 左侧按钮组
        StackPanel leftPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        Button startButton = new Button
        {
            Content = "开始考试",
            Background = new SolidColorBrush(Colors.Green),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Avalonia.Thickness(15, 5),
            CornerRadius = new Avalonia.CornerRadius(3)
        };

        Button pauseButton = new Button
        {
            Content = "暂停",
            Background = new SolidColorBrush(Colors.Orange),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Avalonia.Thickness(15, 5),
            CornerRadius = new Avalonia.CornerRadius(3)
        };

        leftPanel.Children.Add(startButton);
        leftPanel.Children.Add(pauseButton);

        // 中间状态显示
        StackPanel centerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20
        };

        TextBlock timeLabel = new TextBlock
        {
            Text = "剩余时间:",
            Foreground = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Medium
        };

        TextBlock timeValue = new TextBlock
        {
            Text = "120:00",
            Foreground = new SolidColorBrush(Colors.LightGreen),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold,
            FontSize = 16
        };

        TextBlock statusLabel = new TextBlock
        {
            Text = "状态:",
            Foreground = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Medium
        };

        TextBlock statusValue = new TextBlock
        {
            Text = "准备就绪",
            Foreground = new SolidColorBrush(Colors.LightBlue),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.Bold
        };

        centerPanel.Children.Add(timeLabel);
        centerPanel.Children.Add(timeValue);
        centerPanel.Children.Add(statusLabel);
        centerPanel.Children.Add(statusValue);

        // 右侧按钮组
        StackPanel rightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        Button settingsButton = new Button
        {
            Content = "设置",
            Background = new SolidColorBrush(Colors.Gray),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Avalonia.Thickness(15, 5),
            CornerRadius = new Avalonia.CornerRadius(3)
        };

        Button helpButton = new Button
        {
            Content = "帮助",
            Background = new SolidColorBrush(Colors.Blue),
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Avalonia.Thickness(15, 5),
            CornerRadius = new Avalonia.CornerRadius(3)
        };

        rightPanel.Children.Add(settingsButton);
        rightPanel.Children.Add(helpButton);

        // 设置网格列
        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(centerPanel, 1);
        Grid.SetColumn(rightPanel, 2);

        // 添加到网格
        grid.Children.Add(leftPanel);
        grid.Children.Add(centerPanel);
        grid.Children.Add(rightPanel);

        return grid;
    }

    /// <summary>
    /// 测试工具栏功能
    /// </summary>
    /// <param name="toolbarWindow">要测试的工具栏窗口</param>
    public static void TestToolbarFunctionality(ToolbarWindow toolbarWindow)
    {
        // 测试显示/隐藏
        Console.WriteLine("测试工具栏显示/隐藏功能...");
        toolbarWindow.ShowToolbar();
        
        // 测试切换可见性
        Console.WriteLine("测试切换可见性功能...");
        toolbarWindow.ToggleToolbarVisibility();
        
        // 测试尺寸更新
        Console.WriteLine("测试尺寸更新功能...");
        toolbarWindow.UpdateSize(1920, 70);
        
        // 测试位置更新
        Console.WriteLine("测试位置更新功能...");
        toolbarWindow.UpdatePosition(0, 0);
        
        // 测试屏幕预留控制
        Console.WriteLine("测试屏幕预留控制功能...");
        toolbarWindow.SetScreenReservationEnabled(false);
        toolbarWindow.SetScreenReservationEnabled(true);
        
        Console.WriteLine("工具栏功能测试完成！");
    }

    /// <summary>
    /// 创建完整的测试场景
    /// </summary>
    /// <returns>配置好的工具栏窗口</returns>
    public static ToolbarWindow CreateCompleteTestScenario()
    {
        // 创建带自定义内容的工具栏
        ToolbarWindow toolbarWindow = CreateToolbarWithCustomContent();

        // 执行功能测试
        TestToolbarFunctionality(toolbarWindow);

        return toolbarWindow;
    }

    /// <summary>
    /// 创建完整的依赖注入测试场景
    /// </summary>
    /// <param name="screenReservationService">屏幕预留服务</param>
    /// <returns>配置好的工具栏窗口</returns>
    public static ToolbarWindow CreateCompleteTestScenarioWithDI(ScreenReservationService screenReservationService)
    {
        // 创建带依赖注入的工具栏
        ToolbarWindow toolbarWindow = CreateToolbarWithDependencyInjection(screenReservationService);

        // 添加自定义内容
        Grid customContent = CreateCustomToolbarContent();
        toolbarWindow.SetToolbarContent(customContent);

        // 执行功能测试
        TestToolbarFunctionality(toolbarWindow);

        return toolbarWindow;
    }

    /// <summary>
    /// 验证重构后的功能
    /// </summary>
    /// <returns>验证是否成功</returns>
    public static bool ValidateRefactoredFunctionality()
    {
        try
        {
            Console.WriteLine("开始验证重构后的ToolbarWindow功能...");

            // 测试1: 基本实例化
            ToolbarWindow basicWindow = new ToolbarWindow();
            Console.WriteLine("✓ 基本实例化成功");

            // 测试2: 依赖注入实例化
            ScreenReservationService service = new ScreenReservationService();
            ToolbarWindow diWindow = new ToolbarWindow(service);
            Console.WriteLine("✓ 依赖注入实例化成功");

            // 测试3: 属性设置
            diWindow.ToolbarTitle = "测试标题";
            diWindow.ToolbarHeight = 60;
            diWindow.ToolbarOpacity = 0.9;
            diWindow.IsScreenReservationEnabled = true;
            Console.WriteLine("✓ 属性设置成功");

            // 测试4: 方法调用
            diWindow.UpdateSize(1920, 70);
            diWindow.SetToolbarOpacity(0.8);
            diWindow.SetToolbarTopmost(true);
            Console.WriteLine("✓ 方法调用成功");

            // 测试5: 内容设置
            TextBlock testContent = new TextBlock { Text = "测试内容" };
            diWindow.SetToolbarContent(testContent);
            Console.WriteLine("✓ 内容设置成功");

            Console.WriteLine("所有功能验证通过！重构成功。");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 验证失败: {ex.Message}");
            return false;
        }
    }
}
