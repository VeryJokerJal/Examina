# ToolbarWindow 工具栏窗口组件使用说明（重构版）

## 概述

`ToolbarWindow` 是一个可重复使用的工具栏窗口组件，专为 Examina.Desktop 项目设计。该组件已重构为使用直接依赖注入模式，简化了实现并移除了ViewModel层。

## 组件特性

- 无系统装饰（SystemDecorations.None）
- 始终置顶（Topmost = true）
- 半透明背景效果（AcrylicBlur）
- 不可调整大小
- 防止最小化
- 集成屏幕预留服务
- 支持依赖注入
- 直接属性访问，无需ViewModel

## 基本用法

### 1. 创建简单的工具栏窗口

```csharp
// 创建窗口（使用默认构造函数）
ToolbarWindow toolbarWindow = new ToolbarWindow();

// 配置工具栏属性
toolbarWindow.ToolbarTitle = "我的工具栏";
toolbarWindow.ToolbarHeight = 50;
toolbarWindow.ToolbarOpacity = 0.8;
toolbarWindow.IsScreenReservationEnabled = true;

// 显示工具栏
toolbarWindow.ShowToolbar();
```

### 2. 使用依赖注入创建工具栏

```csharp
// 创建或注入屏幕预留服务
ScreenReservationService screenService = new ScreenReservationService();

// 创建窗口（使用依赖注入构造函数）
ToolbarWindow toolbarWindow = new ToolbarWindow(screenService);

// 配置属性
toolbarWindow.ToolbarTitle = "依赖注入工具栏";
toolbarWindow.ToolbarHeight = 60;

// 显示工具栏
toolbarWindow.ShowToolbar();
```

### 3. 添加自定义内容

```csharp
// 创建自定义内容
StackPanel customContent = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Children =
    {
        new Button { Content = "开始", Margin = new Thickness(5) },
        new Button { Content = "暂停", Margin = new Thickness(5) },
        new TextBlock { Text = "状态：准备", VerticalAlignment = VerticalAlignment.Center }
    }
};

// 设置工具栏内容
toolbarWindow.SetToolbarContent(customContent);
```

## 属性说明

| 属性名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `IsToolbarVisible` | bool | true | 工具栏是否可见 |
| `ToolbarOpacity` | double | 0.8 | 工具栏透明度 |
| `ToolbarHeight` | double | 50 | 工具栏高度 |
| `ToolbarWidth` | double | 屏幕宽度 | 工具栏宽度 |
| `IsToolbarTopmost` | bool | true | 是否置顶 |
| `IsScreenReservationEnabled` | bool | true | 是否启用屏幕预留 |
| `ToolbarTitle` | string | "工具栏" | 工具栏标题 |

## 方法说明

| 方法名 | 说明 |
|--------|------|
| `ShowToolbar()` | 显示工具栏 |
| `HideToolbar()` | 隐藏工具栏 |
| `ToggleToolbarVisibility()` | 切换可见性 |
| `SetToolbarContent(Control)` | 设置工具栏内容 |
| `UpdatePosition(int, int)` | 更新工具栏位置 |
| `UpdateSize(double, double)` | 更新工具栏尺寸 |
| `SetScreenReservationEnabled(bool)` | 启用/禁用屏幕预留 |
| `SetToolbarOpacity(double)` | 设置透明度 |
| `SetToolbarTopmost(bool)` | 设置置顶状态 |

## 高级用法

### 1. 动态调整工具栏属性

```csharp
// 动态调整尺寸
toolbarWindow.UpdateSize(1920, 70);

// 动态调整透明度
toolbarWindow.SetToolbarOpacity(0.9);

// 动态调整置顶状态
toolbarWindow.SetToolbarTopmost(false);

// 动态调整位置
toolbarWindow.UpdatePosition(0, 0);
```

### 2. 控制屏幕预留

```csharp
// 禁用屏幕预留
toolbarWindow.SetScreenReservationEnabled(false);

// 重新启用屏幕预留
toolbarWindow.SetScreenReservationEnabled(true);
```

### 3. 响应用户交互

```csharp
// 工具栏内置了切换可见性和关闭按钮
// 这些按钮会自动处理相应的操作

// 也可以通过代码控制
toolbarWindow.ToggleToolbarVisibility();
```

## 依赖注入集成

### 在DI容器中注册

```csharp
// 在App.axaml.cs或Startup中注册服务
services.AddSingleton<ScreenReservationService>();
services.AddTransient<ToolbarWindow>();
```

### 从DI容器获取

```csharp
// 从服务提供者获取
ScreenReservationService screenService = serviceProvider.GetRequiredService<ScreenReservationService>();
ToolbarWindow toolbarWindow = new ToolbarWindow(screenService);
```

## 完整示例

```csharp
public class ToolbarExample
{
    public void CreateExamToolbar()
    {
        // 创建工具栏
        ToolbarWindow toolbarWindow = new ToolbarWindow();
        
        // 配置属性
        toolbarWindow.ToolbarTitle = "考试系统工具栏";
        toolbarWindow.ToolbarHeight = 55;
        toolbarWindow.ToolbarOpacity = 0.9;
        toolbarWindow.IsScreenReservationEnabled = true;

        // 创建考试相关的工具栏内容
        Grid examContent = new Grid();
        examContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        examContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        examContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        // 左侧：考试控制按钮
        StackPanel leftPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };
        leftPanel.Children.Add(new Button { Content = "开始考试", Background = Brushes.Green });
        leftPanel.Children.Add(new Button { Content = "暂停", Background = Brushes.Orange });

        // 中间：考试状态信息
        StackPanel centerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20
        };
        centerPanel.Children.Add(new TextBlock { Text = "剩余时间: 120:00", Foreground = Brushes.White });
        centerPanel.Children.Add(new TextBlock { Text = "状态: 进行中", Foreground = Brushes.LightBlue });

        // 右侧：系统功能按钮
        StackPanel rightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };
        rightPanel.Children.Add(new Button { Content = "设置" });
        rightPanel.Children.Add(new Button { Content = "帮助" });

        // 设置网格布局
        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(centerPanel, 1);
        Grid.SetColumn(rightPanel, 2);

        examContent.Children.Add(leftPanel);
        examContent.Children.Add(centerPanel);
        examContent.Children.Add(rightPanel);

        // 设置内容并显示
        toolbarWindow.SetToolbarContent(examContent);
        toolbarWindow.ShowToolbar();
    }
}
```

## 注意事项

1. 工具栏会自动预留屏幕区域，确保其他窗口不会覆盖
2. 窗口具有防最小化功能，确保工具栏始终可见
3. 支持依赖注入，便于单元测试和服务管理
4. 组件已简化，移除了ViewModel层，直接操作Window属性
5. 保持了所有原有功能，但使用更直接的API
6. 记得在不需要时正确释放资源，避免内存泄漏

## 迁移指南

如果您之前使用的是带ViewModel的版本，迁移步骤如下：

### 旧版本（带ViewModel）
```csharp
ToolbarWindowViewModel viewModel = new ToolbarWindowViewModel { ... };
ToolbarWindow window = new ToolbarWindow(viewModel);
```

### 新版本（直接属性）
```csharp
ToolbarWindow window = new ToolbarWindow();
window.ToolbarTitle = "...";
window.ToolbarHeight = 50;
// 其他属性设置...
```

新版本更简洁，减少了抽象层，提高了性能和可维护性。
