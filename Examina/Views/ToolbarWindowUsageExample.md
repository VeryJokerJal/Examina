# ToolbarWindow 工具栏窗口组件使用说明

## 概述

`ToolbarWindow` 是一个可重复使用的工具栏窗口组件，专为 Examina.Desktop 项目设计。该组件提供了一个位于屏幕顶部的半透明工具栏，具有以下特性：

- 无系统装饰（SystemDecorations.None）
- 始终置顶（Topmost = true）
- 半透明背景效果
- 不可调整大小
- 防止最小化
- 集成屏幕预留服务

## 基本用法

### 1. 创建简单的工具栏窗口

```csharp
// 创建ViewModel
ToolbarWindowViewModel viewModel = new ToolbarWindowViewModel
{
    ToolbarTitle = "我的工具栏",
    ToolbarHeight = 50,
    IsScreenReservationEnabled = true
};

// 创建窗口
ToolbarWindow toolbarWindow = new ToolbarWindow(viewModel);

// 显示工具栏
toolbarWindow.ShowToolbar();
```

### 2. 添加自定义内容

```csharp
// 创建自定义内容
StackPanel customContent = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Children =
    {
        new Button { Content = "按钮1", Margin = new Thickness(5) },
        new Button { Content = "按钮2", Margin = new Thickness(5) },
        new TextBlock { Text = "状态信息", VerticalAlignment = VerticalAlignment.Center }
    }
};

// 设置工具栏内容
toolbarWindow.SetToolbarContent(customContent);
```

### 3. 响应ViewModel命令

```csharp
// 订阅ViewModel的属性变化
viewModel.WhenAnyValue(x => x.IsVisible)
    .Subscribe(isVisible =>
    {
        if (isVisible)
        {
            toolbarWindow.ShowToolbar();
        }
        else
        {
            toolbarWindow.HideToolbar();
        }
    });

// 手动执行命令
viewModel.ToggleVisibilityCommand.Execute().Subscribe();
```

## 高级用法

### 1. 自定义工具栏尺寸和位置

```csharp
// 更新工具栏尺寸
toolbarWindow.UpdateSize(1920, 60);

// 更新工具栏位置
toolbarWindow.UpdatePosition(0, 0);
```

### 2. 控制屏幕预留

```csharp
// 启用屏幕预留
toolbarWindow.SetScreenReservationEnabled(true);

// 禁用屏幕预留
toolbarWindow.SetScreenReservationEnabled(false);
```

### 3. 处理窗口事件

```csharp
// 处理关闭请求
viewModel.CloseRequested += () =>
{
    // 执行清理操作
    Console.WriteLine("工具栏即将关闭");
};
```

## ViewModel 属性说明

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `IsVisible` | bool | true | 工具栏是否可见 |
| `Opacity` | double | 0.8 | 工具栏透明度 |
| `ToolbarHeight` | double | 50 | 工具栏高度 |
| `ToolbarWidth` | double | 屏幕宽度 | 工具栏宽度 |
| `IsTopmost` | bool | true | 是否置顶 |
| `IsScreenReservationEnabled` | bool | true | 是否启用屏幕预留 |
| `ToolbarTitle` | string | "工具栏" | 工具栏标题 |

## ViewModel 命令说明

| 命令名 | 说明 |
|--------|------|
| `ShowCommand` | 显示工具栏 |
| `HideCommand` | 隐藏工具栏 |
| `ToggleVisibilityCommand` | 切换可见性 |
| `CloseCommand` | 关闭工具栏 |
| `PreventMinimizeCommand` | 防止最小化 |

## 样式自定义

工具栏使用了以下资源键，可以在应用程序级别重写：

- `ToolbarBackgroundBrush` - 工具栏背景色
- `ToolbarBorderBrush` - 工具栏边框色
- `ToolbarForegroundBrush` - 工具栏前景色
- `ToolbarButtonHoverBrush` - 按钮悬停背景色
- `ToolbarButtonPressedBrush` - 按钮按下背景色

## 注意事项

1. 工具栏窗口会自动预留屏幕区域，确保其他窗口不会覆盖工具栏
2. 窗口具有防最小化功能，确保工具栏始终可见
3. 使用半透明效果，需要系统支持 AcrylicBlur 透明度级别
4. 组件遵循 MVVM 模式，便于单元测试和维护
5. 记得在不需要时正确释放资源，避免内存泄漏

## 完整示例

```csharp
public class ExampleUsage
{
    public void CreateToolbar()
    {
        // 创建ViewModel
        ToolbarWindowViewModel viewModel = new ToolbarWindowViewModel
        {
            ToolbarTitle = "考试系统工具栏",
            ToolbarHeight = 50,
            Opacity = 0.9,
            IsScreenReservationEnabled = true
        };

        // 创建窗口
        ToolbarWindow toolbarWindow = new ToolbarWindow(viewModel);

        // 创建工具栏内容
        Grid toolbarContent = new Grid();
        toolbarContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        toolbarContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        toolbarContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        // 添加控件
        Button startExamButton = new Button { Content = "开始考试" };
        Grid.SetColumn(startExamButton, 0);
        
        TextBlock statusText = new TextBlock { Text = "准备就绪" };
        Grid.SetColumn(statusText, 1);
        
        Button settingsButton = new Button { Content = "设置" };
        Grid.SetColumn(settingsButton, 2);

        toolbarContent.Children.Add(startExamButton);
        toolbarContent.Children.Add(statusText);
        toolbarContent.Children.Add(settingsButton);

        // 设置内容
        toolbarWindow.SetToolbarContent(toolbarContent);

        // 显示工具栏
        toolbarWindow.ShowToolbar();
    }
}
```
