# PowerPoint打分系统使用指南

## 概述

BenchSuite PowerPoint打分系统是一个基于Microsoft Office Interop的自动化评分工具，能够检测PowerPoint文件中的各种知识点操作并给出相应分数。系统支持39个知识点，涵盖幻灯片操作、文字设置、背景设计、表格处理等多个方面。

## 功能特性

- ✅ 支持39种PowerPoint知识点检测
- ✅ 基于ExamLab模型的配置化评分
- ✅ 异步和同步两种调用方式
- ✅ 详细的检测结果和错误信息
- ✅ 支持批量文件处理
- ✅ 可配置的评分规则
- ✅ 支持部分分数和容错机制

## 支持的知识点类型

### 第一类：幻灯片操作（16个知识点）
1. `SetSlideLayout` - 设置幻灯片版式
2. `DeleteSlide` - 删除幻灯片
3. `InsertSlide` - 插入幻灯片
4. `SetSlideFont` - 设置幻灯片字体
5. `SlideTransitionEffect` - 幻灯片切换效果
6. `SlideTransitionMode` - 幻灯片切换方式
7. `InsertHyperlink` - 插入超链接
8. `SetSlideNumber` - 设置幻灯片编号
9. `SetFooterText` - 设置页脚文字
10. `InsertImage` - 插入图片
11. `InsertTable` - 插入表格
12. `InsertSmartArt` - 插入SmartArt图形
13. `InsertNote` - 插入备注

### 第二类：文字与字体设置（14个知识点）
17. `InsertTextContent` - 插入文本内容
18. `SetTextFontSize` - 设置文本字号
19. `SetTextColor` - 设置文本颜色
20. `SetTextStyle` - 设置文本字形（加粗、斜体、下划线、删除线）
21. `SetElementPosition` - 设置元素位置
22. `SetElementSize` - 设置元素尺寸
29. `SetTextAlignment` - 设置文本对齐方式

### 第三类：背景样式与设计（1个知识点）
31. `ApplyTheme` - 应用主题

### 第四类：母版与主题设置（1个知识点）
32. `SetSlideBackground` - 设置幻灯片背景

### 第五类：其他（7个知识点）
33. `SetTableContent` - 设置表格内容
34. `SetTableStyle` - 设置表格样式

## 快速开始

### 1. 基本使用

```csharp
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;

// 创建打分服务
IPowerPointScoringService scoringService = new PowerPointScoringService();

// 创建试卷模型（基于ExamLab格式）
ExamModel exam = CreateExamModel();

// 执行打分
ScoringResult result = await scoringService.ScoreFileAsync(
    @"C:\path\to\presentation.pptx", 
    exam);

// 查看结果
Console.WriteLine($"总分: {result.TotalScore}");
Console.WriteLine($"得分: {result.AchievedScore}");
Console.WriteLine($"得分率: {result.ScoreRate:P2}");
```

### 2. 单个知识点检测

```csharp
// 检测文本字形
Dictionary<string, string> parameters = new()
{
    ["SlideIndex"] = "1",
    ["TextBoxIndex"] = "1",
    ["StyleType"] = "1" // 1=加粗, 2=斜体, 3=下划线, 4=删除线
};

KnowledgePointResult result = await scoringService.DetectKnowledgePointAsync(
    @"C:\path\to\presentation.pptx",
    "SetTextStyle",
    parameters);

Console.WriteLine($"检测结果: {result.IsCorrect}");
Console.WriteLine($"详情: {result.Details}");
```

### 3. 配置评分选项

```csharp
ScoringConfiguration configuration = new()
{
    EnablePartialScoring = true,    // 启用部分分数
    ErrorTolerance = 0.1m,          // 错误容忍度
    TimeoutSeconds = 30,            // 超时时间
    EnableDetailedLogging = true    // 详细日志
};

ScoringResult result = await scoringService.ScoreFileAsync(
    filePath, exam, configuration);
```

## 知识点参数说明

### SetSlideLayout（设置幻灯片版式）
- `SlideIndex`: 幻灯片索引（从1开始）
- `LayoutType`: 版式类型（如"ppLayoutTitle", "ppLayoutTwoColumnText"等）

### SetTextStyle（设置文本字形）
- `SlideIndex`: 幻灯片索引
- `TextBoxIndex`: 文本框索引
- `StyleType`: 字形类型（1=加粗, 2=斜体, 3=下划线, 4=删除线）

### SetElementPosition（设置元素位置）
- `SlideIndex`: 幻灯片索引
- `ElementIndex`: 元素索引
- `Left`: 水平位置（像素）
- `Top`: 垂直位置（像素）

### SetElementSize（设置元素尺寸）
- `SlideIndex`: 幻灯片索引
- `ElementIndex`: 元素索引
- `Width`: 宽度（像素）
- `Height`: 高度（像素）

### SetTextAlignment（设置文本对齐方式）
- `SlideIndex`: 幻灯片索引
- `TextBoxIndex`: 文本框索引
- `Alignment`: 对齐方式（1=左对齐, 2=居中, 3=右对齐, 4=两端对齐, 5=均匀分布）

### InsertHyperlink（插入超链接）
- `SlideIndex`: 幻灯片索引
- `TextBoxIndex`: 文本框索引
- `HyperlinkType`: 超链接类型（1=外部网页, 2=本演示文稿幻灯片）
- `TextValue`: 超链接文本（可选）
- `TargetSlideIndex`: 目标幻灯片索引（类型2时使用）

### SetTableContent（设置表格内容）
- `SlideIndex`: 幻灯片索引
- `Rows`: 表格行数
- `Columns`: 表格列数
- `Content`: 表格内容（逗号分隔，按行列顺序）

### SlideTransitionMode（幻灯片切换方式）
- `SlideIndexes`: 幻灯片索引列表（逗号分隔，如"1,3,5"）
- `TransitionMode`: 切换方案编号（1-9）

## 返回结果说明

### ScoringResult（打分结果）
- `TotalScore`: 总分
- `AchievedScore`: 获得分数
- `ScoreRate`: 得分率
- `IsSuccess`: 是否成功
- `ErrorMessage`: 错误信息
- `KnowledgePointResults`: 知识点检测结果列表
- `ElapsedMilliseconds`: 耗时

### KnowledgePointResult（知识点结果）
- `KnowledgePointId`: 知识点ID
- `KnowledgePointName`: 知识点名称
- `KnowledgePointType`: 知识点类型
- `TotalScore`: 该知识点总分
- `AchievedScore`: 该知识点获得分数
- `IsCorrect`: 是否正确
- `ExpectedValue`: 期望值
- `ActualValue`: 实际值
- `Details`: 检测详情
- `ErrorMessage`: 错误信息

## 注意事项

1. **Office版本兼容性**: 需要安装Microsoft Office PowerPoint，支持2016及以上版本
2. **文件格式**: 支持.ppt和.pptx格式
3. **权限要求**: 需要有读取PowerPoint文件的权限
4. **资源管理**: 系统会自动管理PowerPoint应用程序的生命周期
5. **异常处理**: 建议使用try-catch包装调用代码
6. **性能考虑**: 大文件或复杂检测可能需要较长时间
7. **位置和尺寸**: 位置和尺寸检测允许5像素的误差范围

## 错误处理

```csharp
try
{
    ScoringResult result = await scoringService.ScoreFileAsync(filePath, exam);
    
    if (!result.IsSuccess)
    {
        Console.WriteLine($"打分失败: {result.ErrorMessage}");
    }
    
    // 检查各个知识点的错误
    foreach (var kpResult in result.KnowledgePointResults)
    {
        if (!string.IsNullOrEmpty(kpResult.ErrorMessage))
        {
            Console.WriteLine($"知识点 {kpResult.KnowledgePointName} 检测失败: {kpResult.ErrorMessage}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"系统错误: {ex.Message}");
}
```

## 扩展开发

如需添加新的知识点检测，请：

1. 在`DetectSpecificKnowledgePoint`方法中添加新的case分支
2. 实现具体的检测方法
3. 更新文档和示例
4. 确保参数验证和错误处理

## 示例代码

完整的使用示例请参考 `BenchSuite/Examples/PowerPointScoringExample.cs` 文件。
