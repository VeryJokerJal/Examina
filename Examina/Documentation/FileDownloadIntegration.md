# 文件预下载和解压功能集成指南

## 概述

本文档说明如何在Examina.Desktop项目中集成文件预下载和解压功能，确保在考试/训练开始前所有必要的文件都已准备就绪。

## 功能特性

### 支持的场景
- 综合实训开始前
- 上机统考（正式考试）开始前  
- 模拟考试开始前
- 专项训练开始前

### 核心功能
- 从ExaminaWebApplication服务器获取文件列表
- 支持多种文件格式（ZIP、RAR、7Z、PDF、DOC等）
- 实时下载进度显示和错误处理
- 支持断点续传和重试机制
- 自动识别并解压压缩包
- 文件完整性验证
- 拟态玻璃(glassmorphism)设计风格

## 架构组件

### 1. 数据模型
- `FileDownloadInfo` - 单个文件下载信息
- `FileDownloadTask` - 下载任务容器
- `FileDownloadTaskType` - 任务类型枚举

### 2. 服务层
- `IFileDownloadService` - 文件下载服务接口
- `FileDownloadService` - 文件下载服务实现

### 3. 视图模型
- `FileDownloadPreparationViewModel` - 文件下载准备视图模型

### 4. 视图
- `FileDownloadPreparationView` - 文件下载准备用户控件
- `FileDownloadPreparationWindow` - 文件下载准备窗口

### 5. 扩展方法
- `FileDownloadExtensions` - 便捷的集成扩展方法
- `FileDownloadHelper` - 文件下载帮助类

## 集成方法

### 方法1：使用扩展方法（推荐）

```csharp
using Examina.Extensions;

// 在考试开始前调用
public async Task StartExamAsync(int examId, string examName)
{
    // 获取当前窗口
    var window = GetCurrentWindow();

    // 准备文件（模拟考试）
    bool filesReady = await window.PrepareFilesForMockExamAsync(examId, examName);

    if (filesReady)
    {
        // 文件准备完成，开始考试
        await StartExamInternal(examId);
    }
    else
    {
        // 文件准备失败，显示错误消息
        ShowErrorMessage("文件准备失败，无法开始考试");
    }
}

// 其他类型的考试/训练
await window.PrepareFilesForOnlineExamAsync(examId, examName);
await window.PrepareFilesForComprehensiveTrainingAsync(trainingId, trainingName);
await window.PrepareFilesForSpecializedTrainingAsync(trainingId, trainingName);
```

## ✅ 已完成的集成

### 1. 上机统考 (OnlineExam)
**文件**: `Examina/ViewModels/Pages/ExamListViewModel.cs`
**方法**: `StartFormalExamAsync`
**集成位置**: 在获取考试详情后，启动考试界面前
**扩展方法**: `PrepareFilesForOnlineExamAsync`

### 2. 模拟考试 (MockExam)
**文件**: `Examina/ViewModels/Pages/MockExamViewModel.cs`
**方法**: `StartMockExamInterfaceAsync`
**集成位置**: 在隐藏主窗口前
**扩展方法**: `PrepareFilesForMockExamAsync`
**覆盖路径**:
- `QuickStartMockExamAsync` → `StartMockExamInterfaceAsync`

### 3. 综合实训 (ComprehensiveTraining)
**文件**: `Examina/ViewModels/Pages/ComprehensiveTrainingListViewModel.cs`
**方法**: `StartTrainingInterfaceAsync`
**集成位置**: 在隐藏主窗口前
**扩展方法**: `PrepareFilesForComprehensiveTrainingAsync`

### 4. 专项训练 (SpecializedTraining)
**文件**: `Examina/ViewModels/Pages/SpecializedTrainingListViewModel.cs`
**方法**: `StartBenchSuiteTrainingAsync`
**集成位置**: 在创建考试工具栏前
**扩展方法**: `PrepareFilesForSpecializedTrainingAsync`

### 方法2：直接使用窗口

```csharp
using Examina.Views.FileDownload;
using Examina.Models.FileDownload;

public async Task StartTrainingAsync(int trainingId, string trainingName)
{
    var window = GetCurrentWindow();
    
    bool filesReady = await FileDownloadPreparationWindow.ShowDownloadPreparationAsync(
        window, 
        $"综合实训: {trainingName}", 
        FileDownloadTaskType.ComprehensiveTraining, 
        trainingId);
    
    if (filesReady)
    {
        await StartTrainingInternal(trainingId);
    }
}
```

### 方法3：检查是否需要下载

```csharp
using Examina.Extensions;

public async Task<bool> CheckFilesBeforeExamAsync(int examId)
{
    // 检查是否有文件需要下载
    bool hasFiles = await FileDownloadHelper.HasFilesToDownloadAsync(
        FileDownloadTaskType.MockExam, examId);
    
    if (!hasFiles)
    {
        // 没有文件需要下载，直接开始考试
        return true;
    }
    
    // 有文件需要下载，显示下载窗口
    var window = GetCurrentWindow();
    return await window.PrepareFilesForMockExamAsync(examId, "模拟考试");
}
```

## 在现有ViewModel中集成

### 示例：在ExamListViewModel中集成

```csharp
// 在ExamListViewModel.cs中添加
using Examina.Extensions;
using Examina.Models.FileDownload;

private async Task StartExamWithFilePreparationAsync(StudentExamDto exam)
{
    try
    {
        IsLoading = true;
        
        // 获取主窗口
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;
        
        // 准备文件
        bool filesReady = await mainWindow.PrepareFilesForOnlineExamAsync(
            exam.Id, exam.Title);
        
        if (filesReady)
        {
            // 文件准备完成，开始考试
            await StartExamInternal(exam);
        }
        else
        {
            ErrorMessage = "文件准备失败，无法开始考试";
        }
    }
    catch (Exception ex)
    {
        ErrorMessage = $"启动考试失败: {ex.Message}";
    }
    finally
    {
        IsLoading = false;
    }
}

private Window? GetMainWindow()
{
    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        return desktop.MainWindow;
    }
    return null;
}
```

## 配置和自定义

### 1. 下载目录配置

默认下载目录：`C:\河北对口计算机\`

目录结构：
```
C:\河北对口计算机\
├── MockExams/
│   └── {examId}/
├── OnlineExams/
│   └── {examId}/
├── ComprehensiveTraining/
│   └── {trainingId}/
└── SpecializedTraining/
    └── {trainingId}/
```

### 2. 支持的文件格式

**压缩包格式：**
- ZIP (.zip)
- RAR (.rar)
- 7-Zip (.7z)
- TAR (.tar)
- GZIP (.gz)
- BZIP2 (.bz2)
- XZ (.xz)

**文档格式：**
- PDF (.pdf)
- Word (.doc, .docx)
- Excel (.xls, .xlsx)
- PowerPoint (.ppt, .pptx)
- 文本文件 (.txt, .rtf)
- 数据文件 (.json, .xml)

**媒体格式：**
- 图片 (.jpg, .jpeg, .png, .gif, .bmp)
- 视频 (.mp4, .avi, .mov, .wmv)
- 音频 (.mp3, .wav, .wma)

### 3. 错误处理

系统会自动处理以下错误情况：
- 网络连接失败
- 文件下载中断
- 解压失败
- 磁盘空间不足
- 文件损坏

用户可以选择重试下载或跳过失败的文件。

## 用户界面

### 文件下载准备窗口特性

1. **实时进度显示**
   - 整体下载进度
   - 单个文件进度
   - 下载速度和剩余时间

2. **状态指示器**
   - 等待中（灰色）
   - 下载中（黄色）
   - 已完成（绿色）
   - 失败（红色）

3. **操作控制**
   - 开始下载
   - 取消下载
   - 重试失败的文件
   - 显示/隐藏详细进度

4. **拟态玻璃设计**
   - 半透明背景
   - 模糊效果
   - 优雅的边框和阴影
   - 响应式布局

## 性能优化

### 1. 并发下载
- 支持多文件并发下载
- 自动限制并发数量避免过载

### 2. 内存管理
- 流式下载大文件
- 及时释放资源
- 避免内存泄漏

### 3. 磁盘空间管理
- 下载前检查可用空间
- 自动清理临时文件
- 压缩包解压后删除原文件

## 故障排除

### 常见问题

1. **下载失败**
   - 检查网络连接
   - 验证服务器地址
   - 确认文件权限

2. **解压失败**
   - 检查文件完整性
   - 验证压缩包格式
   - 确认磁盘空间

3. **界面无响应**
   - 所有操作都在后台线程执行
   - UI线程不会被阻塞
   - 可以随时取消操作

### 日志记录

系统会记录详细的操作日志，包括：
- 下载开始/结束时间
- 文件大小和传输速度
- 错误信息和堆栈跟踪
- 用户操作记录

## 扩展开发

### 添加新的文件格式支持

```csharp
// 在FileDownloadService中添加新格式
private readonly List<string> _supportedCompressionFormats = new()
{
    ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
    ".新格式" // 添加新格式
};
```

### 自定义下载逻辑

```csharp
// 继承FileDownloadService并重写方法
public class CustomFileDownloadService : FileDownloadService
{
    public override async Task<bool> DownloadFileAsync(
        FileDownloadInfo fileInfo, 
        IProgress<FileDownloadInfo>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        // 自定义下载逻辑
        return await base.DownloadFileAsync(fileInfo, progress, cancellationToken);
    }
}
```

## 总结

文件预下载和解压功能为Examina.Desktop项目提供了完整的文件管理解决方案，确保用户在开始考试或训练前所有必要的文件都已准备就绪。通过简单的扩展方法调用，现有的页面可以轻松集成这一功能，提供流畅的用户体验。
