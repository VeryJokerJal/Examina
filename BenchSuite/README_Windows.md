# Windows 打分服务

## 概述

Windows打分服务 (`WindowsScoringService`) 是BenchSuite项目中专门用于检测和评估Windows文件系统操作的服务。该服务能够自动检测学生在Windows环境中执行的各种文件和文件夹操作，并根据预设的标准进行打分。

## 主要功能

### 支持的操作类型

1. **创建操作** (`QuickCreate`, `CreateOperation`)
   - 检测文件或文件夹的创建
   - 支持内容验证
   - 支持创建类型指定（文件/文件夹）

2. **删除操作** (`DeleteOperation`)
   - 检测文件或文件夹的删除
   - 支持回收站检查
   - 验证目标是否已被移除

3. **复制操作** (`CopyOperation`)
   - 检测文件或文件夹的复制
   - 验证源文件和目标文件的存在性
   - 支持文件大小一致性检查

4. **移动操作** (`MoveOperation`)
   - 检测文件或文件夹的移动
   - 验证源文件不存在且目标文件存在
   - 区分移动和复制操作

5. **重命名操作** (`RenameOperation`)
   - 检测文件或文件夹的重命名
   - 验证原名不存在且新名存在
   - 支持同目录重命名检测

6. **快捷方式操作** (`ShortcutOperation`)
   - 检测快捷方式文件的创建
   - 自动添加.lnk扩展名
   - 支持目标路径验证

7. **属性修改操作** (`FilePropertyModification`)
   - 检测文件或文件夹属性的修改
   - 支持只读属性检查
   - 支持隐藏属性检查

8. **复制重命名操作** (`CopyRenameOperation`)
   - 检测复制并重命名的组合操作
   - 验证源文件和目标文件都存在
   - 确保文件名不同且内容一致

## 使用方法

### 基本使用

```csharp
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;
using Microsoft.Extensions.Logging;

// 创建日志记录器（可选）
ILogger<WindowsScoringService> logger = loggerFactory.CreateLogger<WindowsScoringService>();

// 创建打分服务
IWindowsScoringService scoringService = new WindowsScoringService(logger);

// 创建试卷模型（基于ExamLab格式）
ExamModel exam = CreateExamModel();

// 执行打分
ScoringResult result = await scoringService.ScoreFileAsync(
    @"C:\Users\Student\Desktop", 
    exam);

// 查看结果
Console.WriteLine($"总分: {result.TotalScore}");
Console.WriteLine($"得分: {result.AchievedScore}");
Console.WriteLine($"得分率: {result.ScoreRate:P2}");
```

### 单个操作检测

```csharp
// 检测文件创建操作
Dictionary<string, string> parameters = new()
{
    ["TargetPath"] = "新建文件.txt",
    ["CreateType"] = "File",
    ["ExpectedContent"] = "Hello World"
};

KnowledgePointResult result = await scoringService.DetectWindowsOperationAsync(
    @"C:\Users\Student\Desktop",
    "CreateOperation",
    parameters);

Console.WriteLine($"检测结果: {result.IsCorrect}");
Console.WriteLine($"详情: {result.Details}");
```

### 批量操作检测

```csharp
// 创建操作点列表
List<OperationPointModel> operationPoints = new()
{
    new OperationPointModel
    {
        Id = "op1",
        Name = "创建文件",
        WindowsOperationType = "CreateOperation",
        Parameters = new List<ConfigurationParameterModel>
        {
            new() { Name = "TargetPath", Value = "test.txt" },
            new() { Name = "CreateType", Value = "File" }
        }
    }
};

// 批量检测
List<KnowledgePointResult> results = await scoringService.DetectWindowsOperationsAsync(
    @"C:\Users\Student\Desktop",
    operationPoints);

foreach (var result in results)
{
    Console.WriteLine($"{result.KnowledgePointName}: {result.IsCorrect}");
}
```

## 配置参数

### 通用参数

- **TargetPath**: 目标文件或文件夹路径
- **SourcePath**: 源文件或文件夹路径
- **CreateType**: 创建类型（File/Folder/Directory）

### 创建操作参数

**CreateOperation（创建操作）**：
- **FileType**: 文件类型（枚举：文件,文件夹）
- **ItemName**: 项目名称（文本，必填）
- **ExpectedContent**: 期望的文件内容（仅文件，可选）

**QuickCreate（快捷创建）**：
- **FileType**: 文件类型（枚举：文件,文件夹）
- **ItemName**: 项目名称（文本，必填）
- **CreatePath**: 创建路径（文本，必填）

### 删除操作参数

- **FileType**: 文件类型（枚举：文件,文件夹）
- **TargetPath**: 要删除的文件或文件夹路径
- **CheckRecycleBin**: 是否检查回收站（true/false，可选）

### 复制/移动操作参数

- **FileType**: 文件类型（枚举：文件,文件夹）
- **SourcePath**: 源文件或文件夹路径
- **DestinationPath**: 目标文件或文件夹路径（推荐）
- **TargetPath**: 目标文件或文件夹路径（兼容旧格式）

### 重命名操作参数

- **FileType**: 文件类型（枚举：文件,文件夹）
- **OriginalFileName**: 原始文件或文件夹名称
- **NewFileName**: 新的文件或文件夹名称

### 快捷方式操作参数

- **FileType**: 文件类型（枚举：文件,文件夹）
- **TargetPath**: 目标文件路径
- **ShortcutPath**: 快捷方式路径

### 属性修改操作参数

- **FileType**: 文件类型（枚举：文件,文件夹）
- **FilePath**: 文件路径（推荐）
- **TargetPath**: 目标文件或文件夹路径（兼容旧格式）
- **PropertyType**: 属性类型（枚举：只读,隐藏,系统,存档）
- **PropertyValue**: 属性值（布尔，必填）

### 复制重命名操作参数

- **FileType**: 文件类型（枚举：文件,文件夹）
- **SourcePath**: 原文件路径
- **DestinationPath**: 目标文件路径

## 错误处理

服务提供了完整的错误处理机制：

1. **参数验证**: 自动验证必需参数的存在性
2. **路径验证**: 检查文件和文件夹路径的有效性
3. **异常捕获**: 捕获并记录所有操作异常
4. **详细日志**: 提供详细的操作日志和错误信息

## 日志记录

服务支持Microsoft.Extensions.Logging框架：

- **Information**: 记录操作开始、完成和统计信息
- **Warning**: 记录配置问题和非致命错误
- **Error**: 记录异常和致命错误

## 架构特点

1. **模块化设计**: 每种操作类型都有独立的检测方法
2. **异步支持**: 所有主要方法都支持异步操作
3. **可扩展性**: 易于添加新的操作类型
4. **类型安全**: 使用强类型参数和返回值
5. **资源管理**: 自动处理文件系统资源

## 注意事项

1. **权限要求**: 服务需要足够的文件系统访问权限
2. **路径格式**: 支持相对路径和绝对路径
3. **大小写敏感**: 文件名比较默认不区分大小写
4. **并发安全**: 服务设计为线程安全

## 示例场景

### 场景1: 文件管理基础操作

检测学生是否能够：
1. 在桌面创建一个名为"作业"的文件夹
2. 在该文件夹中创建一个文本文件
3. 复制该文件到另一个位置
4. 重命名原文件

### 场景2: 文件属性操作

检测学生是否能够：
1. 创建一个文件
2. 将文件设置为只读
3. 将文件设置为隐藏
4. 创建文件的快捷方式

## 版本信息

- **当前版本**: 1.0.0
- **兼容性**: .NET 8.0+
- **依赖项**: Microsoft.Extensions.Logging, System.Text.Json

## 相关文档

- [PowerPoint打分服务](README_PowerPoint.md)
- [BenchSuite架构文档](README.md)
