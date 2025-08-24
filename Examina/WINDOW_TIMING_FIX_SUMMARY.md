# 训练/考试窗口显示时序修复总结

## 🎯 修复目标
解决所有训练/考试类型中主窗口过早显示的问题，确保正确的窗口显示时序：
**训练/考试提交 → 显示结果窗口 → 用户确认关闭结果窗口后 → 显示主窗口**

## 🔍 问题识别

### 修复前的问题
所有训练/考试类型都存在主窗口过早显示的问题：

1. **专项训练**: 显示结果窗口后立即调用`CloseTrainingAndShowMainWindow()`
2. **综合训练**: 显示结果窗口后立即调用`CloseTrainingAndShowMainWindow()`
3. **模拟考试**: 提交成功后直接调用`CloseExamAndShowMainWindow()`，没有结果窗口
4. **正式考试**: 显示结果窗口后立即调用`ShowMainWindowAndRefresh()`

## ✅ 修复方案

### 统一的窗口管理模式

#### 1. 专项训练 (SpecializedTrainingListViewModel)
```csharp
// 修复前
await ShowTrainingResultAsync(training.Name, scoringResult);
CloseTrainingAndShowMainWindow(); // ❌ 过早显示主窗口

// 修复后
await ShowTrainingResultAsync(training.Name, scoringResult);
// 结果窗口关闭后会自动显示主窗口

// ShowTrainingResultAsync内部
await resultWindow.WaitForCloseAsync();
CloseTrainingAndShowMainWindow(); // ✅ 窗口关闭后显示主窗口
```

#### 2. 综合训练 (ComprehensiveTrainingListViewModel)
```csharp
// 修复前
await ShowTrainingResultAsync(trainingId, examType, scoringResult);
CloseTrainingAndShowMainWindow(); // ❌ 过早显示主窗口

// 修复后
await ShowTrainingResultAsync(trainingId, examType, scoringResult);
// 结果窗口关闭后会自动显示主窗口

// ShowDetailedTrainingResultAsync内部
await resultWindow.WaitForCloseAsync();
CloseTrainingAndShowMainWindow(); // ✅ 窗口关闭后显示主窗口
```

#### 3. 模拟考试 (MockExamViewModel)
```csharp
// 修复前
if (submitResult) {
    CloseExamAndShowMainWindow(); // ❌ 没有结果窗口就直接显示主窗口
}

// 修复后
if (submitResult) {
    await ShowExamResultAsync(examId, examType, true, actualDurationSeconds);
}

// ShowExamResultAsync内部（模态窗口）
await ExamResultWindow.ShowExamResultAsync(...);
CloseExamAndShowMainWindow(); // ✅ 模态窗口关闭后显示主窗口
```

#### 4. 正式考试 (ExamListViewModel)
```csharp
// 修复前
await ShowExamResultWithAsyncScoringAsync(...);
ShowMainWindowAndRefresh(); // ❌ 过早显示主窗口

// 修复后
await ShowExamResultWithAsyncScoringAsync(...);
// 结果窗口关闭后会自动显示主窗口

// ShowExamResultWithAsyncScoringAsync内部（非模态窗口）
resultWindow.Closed += (sender, e) => {
    ShowMainWindowAndRefresh(); // ✅ 窗口关闭事件触发主窗口显示
};
resultWindow.Show();
```

### 窗口类型和处理方式

| 训练/考试类型 | 结果窗口类型 | 显示方式 | 关闭回调方式 |
|--------------|-------------|----------|-------------|
| 专项训练 | TrainingResultWindow | Show() + WaitForCloseAsync() | await等待关闭 |
| 综合训练 | TrainingResultWindow | Show() + WaitForCloseAsync() | await等待关闭 |
| 模拟考试 | ExamResultWindow | ShowDialog() | 模态窗口自动等待 |
| 正式考试 | ExamResultWindow | Show() + Closed事件 | 事件回调 |

## 🛠️ 技术实现细节

### 1. TrainingResultWindow.WaitForCloseAsync()
```csharp
public Task WaitForCloseAsync()
{
    TaskCompletionSource<bool> tcs = new();
    Closed += (sender, e) => tcs.SetResult(true);
    return tcs.Task;
}
```

### 2. ExamResultWindow模态显示
```csharp
public static async Task<bool?> ShowExamResultAsync(Window? owner, ...)
{
    // 模态对话框，自动等待用户关闭
    return await window.ShowDialog<bool?>(owner);
}
```

### 3. 正式考试非模态窗口事件处理
```csharp
resultWindow.Closed += (sender, e) => {
    ShowMainWindowAndRefresh();
};
resultWindow.Show();
```

## 🔧 异常处理

所有类型都添加了完善的异常处理，确保即使在错误情况下也能正确显示主窗口：

```csharp
try {
    // 显示结果窗口
    await ShowResultWindow(...);
    // 窗口关闭后显示主窗口
    ShowMainWindow();
} catch (Exception ex) {
    // 如果显示结果窗口失败，也要显示主窗口
    ShowMainWindow();
}
```

## ✅ 修复效果

### 修复后的正确流程
1. **训练/考试提交完成**
2. **立即显示结果窗口**（前台显示，不被主窗口干扰）
3. **用户查看结果并点击确认/关闭**
4. **结果窗口关闭后自动显示主窗口**

### 用户体验改善
- ✅ 结果窗口始终显示在前台
- ✅ 不会被主窗口覆盖或干扰
- ✅ 用户可以完整查看训练/考试结果
- ✅ 关闭结果窗口后自然返回主界面
- ✅ 所有训练/考试类型行为一致

## 📁 修改的文件
- `Examina/ViewModels/Pages/SpecializedTrainingListViewModel.cs`
- `Examina/ViewModels/Pages/ComprehensiveTrainingListViewModel.cs`
- `Examina/ViewModels/Pages/MockExamViewModel.cs`
- `Examina/ViewModels/Pages/ExamListViewModel.cs`

## 🎯 总结
通过统一的窗口管理模式，所有训练/考试类型现在都遵循相同的窗口显示时序逻辑，确保用户体验的一致性和流畅性。主窗口不再过早显示，结果窗口能够正确地在前台展示给用户。
