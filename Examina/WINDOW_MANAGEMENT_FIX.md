# 专项练习主窗口隐藏功能修复

## 问题描述
当用户启动专项练习训练后，主窗口（SpecializedTrainingListViewModel对应的窗口）没有被隐藏，导致桌面上同时显示多个窗口，影响用户体验。

## 修复内容

### 1. 问题分析
- **现状**：专项练习启动后，工具栏窗口正确显示，但主窗口仍然可见
- **对比**：综合练习已经正确实现了窗口管理功能
- **影响**：用户界面混乱，训练环境不够清洁

### 2. 修复方案
参考综合练习的实现，在专项练习中添加相同的窗口管理逻辑：

#### 2.1 启动训练时隐藏主窗口
在`StartBenchSuiteTrainingAsync`方法中，文件预下载完成后立即隐藏主窗口：
```csharp
// 隐藏主窗口
desktop.MainWindow.Hide();
System.Diagnostics.Debug.WriteLine("SpecializedTrainingListViewModel: 主窗口已隐藏");
```

#### 2.2 训练结束时恢复主窗口
添加`CloseTrainingAndShowMainWindow`方法，在训练提交完成后调用：
```csharp
private void CloseTrainingAndShowMainWindow()
{
    // 显示主窗口
    if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
        desktop.MainWindow != null)
    {
        desktop.MainWindow.Show();
        desktop.MainWindow.Activate();
    }
    
    // 刷新训练列表
    _ = RefreshAsync();
}
```

### 3. 修改的文件
- `Examina/ViewModels/Pages/SpecializedTrainingListViewModel.cs`
  - 在`StartBenchSuiteTrainingAsync`方法中添加主窗口隐藏逻辑
  - 在`SubmitTrainingWithBenchSuiteAsync`方法中添加窗口恢复调用
  - 新增`CloseTrainingAndShowMainWindow`方法

### 4. 实现细节

#### 4.1 窗口隐藏时机
- **位置**：文件预下载完成后，创建工具栏ViewModel之前
- **条件**：确保能够获取到主窗口实例
- **日志**：添加调试日志确认操作成功

#### 4.2 窗口恢复时机
- **自动提交**：`OnTrainingAutoSubmitted` → `SubmitTrainingWithBenchSuiteAsync` → `CloseTrainingAndShowMainWindow`
- **手动提交**：`OnTrainingManualSubmitted` → `SubmitTrainingWithBenchSuiteAsync` → `CloseTrainingAndShowMainWindow`
- **功能**：显示主窗口、激活窗口、刷新训练列表

#### 4.3 与综合练习的一致性
现在专项练习和综合练习都实现了相同的窗口管理逻辑：
- 启动训练时隐藏主窗口
- 训练结束时恢复主窗口
- 提供清洁的训练环境

## 验证方法

### 1. 功能测试
1. **启动专项练习**：
   - 在专项练习列表中选择一个训练
   - 点击"开始训练"按钮
   - 验证主窗口是否被隐藏
   - 验证工具栏窗口是否正确显示

2. **训练结束**：
   - 在工具栏窗口中提交训练（自动或手动）
   - 验证主窗口是否重新显示
   - 验证主窗口是否被激活（获得焦点）
   - 验证训练列表是否刷新

### 2. 调试验证
查看调试输出窗口，应该看到以下日志：

#### 启动训练时：
```
SpecializedTrainingListViewModel: 文件预下载完成，继续启动专项训练
SpecializedTrainingListViewModel: 主窗口已隐藏
专项训练工具栏窗口已显示
```

#### 训练结束时：
```
专项训练已通过BenchSuite完成
SpecializedTrainingListViewModel: 主窗口已显示
SpecializedTrainingListViewModel: 训练列表刷新已启动
```

### 3. 边界情况测试
1. **无法获取主窗口**：
   - 验证在无法获取主窗口时不会崩溃
   - 检查相应的调试日志

2. **异常处理**：
   - 验证窗口操作异常时的错误处理
   - 确保应用程序继续正常运行

## 预期效果

### 修复前
- ❌ 启动专项练习后，主窗口和工具栏窗口同时显示
- ❌ 桌面窗口混乱，影响用户体验
- ❌ 与综合练习行为不一致

### 修复后
- ✅ 启动专项练习后，只显示工具栏窗口
- ✅ 提供清洁的训练环境
- ✅ 训练结束后主窗口自动恢复
- ✅ 与综合练习行为保持一致

## 技术要点

### 1. 窗口引用获取
```csharp
if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
    desktop.MainWindow != null)
{
    // 窗口操作
}
```

### 2. 窗口操作方法
- `Hide()`：隐藏窗口但不关闭
- `Show()`：显示隐藏的窗口
- `Activate()`：激活窗口并获得焦点

### 3. 异常处理
所有窗口操作都包含在try-catch块中，确保异常不会影响应用程序的正常运行。

## 相关文件
- `Examina/ViewModels/Pages/SpecializedTrainingListViewModel.cs` - 主要修改文件
- `Examina/ViewModels/Pages/ComprehensiveTrainingListViewModel.cs` - 参考实现
- `Examina/WINDOW_MANAGEMENT_FIX.md` - 本文档
