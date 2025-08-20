# ExamToolbarWindow 考试工具栏组件使用指南

## 概述

`ExamToolbarWindow` 是专门为Examina.Desktop项目设计的考试工具栏组件，基于重构后的`ToolbarWindow`组件构建。该组件专门用于模拟考试和综合实训场景，提供实时倒计时、自动提交、学生信息显示等功能。

## 组件特性

### 核心功能
- ✅ 实时倒计时显示（HH:MM:SS格式）
- ✅ 时间到达时自动提交考试
- ✅ 手动提交考试（带确认对话框）
- ✅ 学生信息显示（姓名、学号）
- ✅ 考试状态实时更新
- ✅ 题目信息显示
- ✅ 网络状态监控

### 窗口特性
- ✅ 位于屏幕顶部，高度60像素
- ✅ 无系统装饰，始终置顶
- ✅ 半透明背景效果
- ✅ 防止最小化和意外关闭
- ✅ 自动屏幕区域预留

### 支持的考试类型
- 🎯 模拟考试（MockExam）
- 🎯 综合实训（ComprehensiveTraining）
- 🎯 正式考试（FormalExam）

## 基本用法

### 1. 创建考试工具栏

```csharp
// 注入必要的服务
IAuthenticationService authService = serviceProvider.GetRequiredService<IAuthenticationService>();
ILogger<ExamToolbarViewModel> viewModelLogger = serviceProvider.GetRequiredService<ILogger<ExamToolbarViewModel>>();
ILogger<ExamToolbarWindow> windowLogger = serviceProvider.GetRequiredService<ILogger<ExamToolbarWindow>>();

// 创建ViewModel
ExamToolbarViewModel viewModel = new ExamToolbarViewModel(authService, viewModelLogger);

// 创建屏幕预留服务
ScreenReservationService screenService = new ScreenReservationService();

// 创建考试工具栏窗口
ExamToolbarWindow examToolbar = new ExamToolbarWindow(viewModel, screenService, windowLogger);
```

### 2. 开始考试

```csharp
// 开始模拟考试
examToolbar.StartExam(
    ExamType.MockExam,      // 考试类型
    examId: 123,            // 考试ID
    examName: "期中模拟考试", // 考试名称
    totalQuestions: 50,     // 题目总数
    durationSeconds: 7200   // 考试时长（2小时）
);

// 显示工具栏
examToolbar.Show();
```

### 3. 处理考试事件

```csharp
// 订阅考试事件
examToolbar.ExamAutoSubmitted += (sender, e) =>
{
    // 处理自动提交（时间到）
    Console.WriteLine("考试时间到，自动提交");
    HandleExamSubmission(isAutoSubmit: true);
};

examToolbar.ExamManualSubmitted += (sender, e) =>
{
    // 处理手动提交
    Console.WriteLine("学生手动提交考试");
    HandleExamSubmission(isAutoSubmit: false);
};

examToolbar.ViewQuestionsRequested += (sender, e) =>
{
    // 处理查看题目请求
    Console.WriteLine("学生请求查看题目");
    ShowQuestionsWindow();
};
```

## 高级用法

### 1. 集成考试服务

```csharp
// 创建考试服务
ExamToolbarService examService = new ExamToolbarService(
    studentExamService,
    studentMockExamService,
    studentComprehensiveTrainingService,
    authenticationService,
    logger);

// 开始模拟考试
bool success = await examService.StartMockExamAsync(mockExamId, viewModel);
if (success)
{
    examToolbar.Show();
}
```

### 2. 错误处理和重试机制

```csharp
// 创建错误处理器
ExamErrorHandler errorHandler = new ExamErrorHandler(logger);

// 处理提交错误
try
{
    await examService.SubmitMockExamAsync(examId);
}
catch (Exception ex)
{
    ExamSubmitErrorResult result = await errorHandler.HandleExamSubmitErrorAsync(ex, "MockExam", examId);
    
    if (result.IsRetryable)
    {
        // 重试提交
        bool retrySuccess = await examService.RetrySubmitExamAsync(ExamType.MockExam, examId);
        if (!retrySuccess)
        {
            ShowErrorMessage(result.ErrorMessage, result.SuggestedAction);
        }
    }
    else
    {
        ShowErrorMessage(result.ErrorMessage, result.SuggestedAction);
    }
}
```

### 3. 自定义时间警告

```csharp
// 设置时间警告阈值（默认5分钟）
viewModel.TimeWarningThreshold = 600; // 10分钟

// 监听时间紧急状态变化
viewModel.WhenAnyValue(x => x.IsTimeUrgent)
    .Subscribe(isUrgent =>
    {
        if (isUrgent)
        {
            // 显示时间紧急提醒
            ShowTimeUrgentNotification();
        }
    });
```

## 依赖注入配置

### 在App.axaml.cs中注册服务

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 注册核心服务
    services.AddSingleton<IAuthenticationService, AuthenticationService>();
    services.AddSingleton<ScreenReservationService>();
    
    // 注册考试相关服务
    services.AddScoped<IStudentExamService, StudentExamService>();
    services.AddScoped<IStudentMockExamService, StudentMockExamService>();
    services.AddScoped<IStudentComprehensiveTrainingService, StudentComprehensiveTrainingService>();
    
    // 注册考试工具栏服务
    services.AddScoped<ExamToolbarService>();
    services.AddScoped<ExamErrorHandler>();
    
    // 注册ViewModel和Window
    services.AddTransient<ExamToolbarViewModel>();
    services.AddTransient<ExamToolbarWindow>();
}
```

### 从DI容器获取

```csharp
// 从服务提供者获取
ExamToolbarWindow examToolbar = serviceProvider.GetRequiredService<ExamToolbarWindow>();
ExamToolbarService examService = serviceProvider.GetRequiredService<ExamToolbarService>();
```

## 样式自定义

### 自定义颜色主题

```xml
<!-- 在App.axaml中重写资源 -->
<Application.Resources>
    <!-- 考试工具栏背景色 -->
    <SolidColorBrush x:Key="ExamToolbarBackgroundBrush" Color="#E6001122" />
    
    <!-- 紧急时间颜色 -->
    <SolidColorBrush x:Key="UrgentTimeBrush" Color="#FFFF0000" />
    
    <!-- 提交按钮背景色 -->
    <SolidColorBrush x:Key="ExamSubmitButtonBackgroundBrush" Color="#FF007ACC" />
</Application.Resources>
```

## 最佳实践

### 1. 资源管理

```csharp
// 确保正确释放资源
public void CleanupExamToolbar()
{
    examToolbar?.Dispose();
    examService?.Dispose();
    errorHandler = null;
}
```

### 2. 异常处理

```csharp
// 全局异常处理
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    if (e.ExceptionObject is Exception ex)
    {
        logger.LogCritical(ex, "考试工具栏发生未处理异常");
        
        // 尝试保存考试状态
        TrySaveExamState();
    }
};
```

### 3. 网络状态监控

```csharp
// 定期检查网络状态
Timer networkCheckTimer = new Timer(async _ =>
{
    bool isConnected = await errorHandler.CheckNetworkConnectivityAsync();
    viewModel.IsNetworkConnected = isConnected;
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
```

## 故障排除

### 常见问题

1. **工具栏不显示**
   - 检查屏幕预留服务是否正常工作
   - 确认窗口属性设置正确

2. **倒计时不准确**
   - 检查系统时间是否正确
   - 确认Timer没有被意外释放

3. **自动提交失败**
   - 检查网络连接状态
   - 查看日志中的错误信息
   - 使用重试机制

4. **内存泄漏**
   - 确保正确调用Dispose方法
   - 检查事件订阅是否正确取消

### 调试技巧

```csharp
// 启用详细日志
logger.LogDebug("考试工具栏状态: {Status}, 剩余时间: {Time}", 
    viewModel.CurrentExamStatus, viewModel.RemainingTimeSeconds);

// 监控内存使用
GC.Collect();
long memoryBefore = GC.GetTotalMemory(false);
// ... 执行操作
long memoryAfter = GC.GetTotalMemory(false);
logger.LogInformation("内存使用变化: {Delta} bytes", memoryAfter - memoryBefore);
```

## 注意事项

1. **考试安全性**
   - 工具栏具有防最小化功能
   - 意外关闭会触发自动提交
   - 所有操作都有日志记录

2. **性能考虑**
   - 倒计时使用高精度Timer
   - 网络检查有合理的间隔
   - 避免频繁的UI更新

3. **兼容性**
   - 支持不同分辨率的屏幕
   - 兼容多显示器环境
   - 适配不同的DPI设置

4. **数据安全**
   - 考试数据实时同步
   - 提交失败时自动重试
   - 本地缓存机制保护数据

通过遵循本指南，您可以有效地使用ExamToolbarWindow组件来构建稳定、安全的考试系统界面。
