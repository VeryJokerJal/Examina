# BenchSuite Word知识点服务

## 概述

WordKnowledgeService 是 BenchSuite 项目中新增的 Word 操作点配置服务，提供了标准化的 Word 操作点参数模板和默认值配置。该服务与 ExamLab 项目保持一致，为 Word 操作点提供了完整的默认数据功能。

## 主要功能

### 🎯 核心特性

1. **默认值配置**：为所有参数类型提供合理的默认值
2. **颜色控件优化**：使用专业的颜色选择器控件（ParameterType.Color）
3. **参数类型支持**：支持数字、文本、枚举、颜色等多种参数类型
4. **分类管理**：按功能分类组织操作点（段落操作、页面设置、表格操作等）

### 📋 支持的操作点分类

- **段落操作**：字体、字号、颜色、对齐、缩进、行间距等
- **页面设置**：纸张大小、页边距、页眉页脚、页码等
- **水印设置**：水印文字、字体、大小、角度等
- **项目符号与编号**：编号类型、格式等
- **表格操作**：行列设置、单元格内容、对齐、合并等
- **图形和图片设置**：图形类型、大小、颜色、位置等
- **文本框设置**：内容、样式、环绕方式等
- **其他操作**：查找替换、指定文字格式等

## 使用方法

### 基本用法

```csharp
using BenchSuite.Services;
using BenchSuite.Models;

// 获取服务实例
WordKnowledgeService service = WordKnowledgeService.Instance;

// 创建带有默认值的操作点
OperationPointModel operation = service.CreateOperationPoint("SetParagraphFont");

// 查看参数默认值
foreach (var param in operation.Parameters)
{
    Console.WriteLine($"{param.DisplayName}: {param.DefaultValue}");
}
```

### 获取操作点配置

```csharp
// 获取特定操作点配置
WordOperationConfig? config = service.GetOperationConfig("SetParagraphFont");

// 获取所有操作点配置
var allConfigs = service.GetAllOperationConfigs();

// 按分类查看
var categories = allConfigs.Values.GroupBy(c => c.Category);
```

### 参数类型和默认值

#### 颜色参数（ParameterType.Color）
```csharp
// 文字颜色默认值：#000000（黑色）
// 填充颜色默认值：#FFFFFF（白色）
// 高亮颜色默认值：#FFFF00（黄色）
```

#### 数字参数（ParameterType.Number）
```csharp
// 段落序号默认值：-1（任意段落）
// 字号默认值：12磅
// 字间距默认值：0磅
// 首行缩进默认值：2字符
// 行间距默认值：1.5倍
// 页边距默认值：上下72磅，左右90磅
```

#### 枚举参数（ParameterType.Enum）
```csharp
// 字体类型默认值：宋体
// 字体样式默认值：常规
// 对齐方式默认值：左对齐
// 纸张大小默认值：A4纸
```

#### 文本参数（ParameterType.Text）
```csharp
// 水印文字默认值：机密
// 页眉内容默认值：页眉内容
// 页脚内容默认值：页脚内容
// 单元格内容默认值：内容
```

## 默认值设计原则

### 🎨 颜色默认值
- **文字颜色**：#000000（黑色）- 标准文字颜色
- **边框颜色**：#000000（黑色）- 清晰可见
- **填充颜色**：#FFFFFF（白色）- 保持简洁
- **高亮颜色**：#FFFF00（黄色）- 突出显示
- **阴影颜色**：#808080（灰色）- 自然阴影效果

### 📏 数字默认值
- **字号**：12磅 - 标准阅读字号
- **段落序号**：-1 - 表示任意段落，提高灵活性
- **缩进**：首行2字符，左右0字符 - 符合中文排版习惯
- **行间距**：1.5倍 - 提高可读性
- **表格尺寸**：3行3列 - 常用表格大小
- **图片尺寸**：200x200磅 - 适中的显示尺寸

### 📝 枚举默认值
- **字体**：宋体 - 中文标准字体
- **对齐**：左对齐 - 符合阅读习惯
- **纸张**：A4纸 - 最常用的纸张规格
- **环绕**：嵌入型 - 最简单的布局方式

## 示例代码

### 完整示例

```csharp
// 运行示例程序
WordKnowledgeServiceExample.RunExample();

// 演示参数修改
WordKnowledgeServiceExample.DemonstrateParameterModification();
```

### 创建不同类型的操作点

```csharp
// 段落操作
var fontOp = service.CreateOperationPoint("SetParagraphFont");
var colorOp = service.CreateOperationPoint("SetParagraphTextColor");

// 表格操作
var tableOp = service.CreateOperationPoint("SetTableRowsColumns");
var cellOp = service.CreateOperationPoint("SetTableCellContent");

// 图形操作
var shapeOp = service.CreateOperationPoint("InsertAutoShape");
var imageOp = service.CreateOperationPoint("SetImageSize");
```

## 测试

运行单元测试验证功能：

```bash
dotnet test BenchSuite.Tests.WordKnowledgeServiceTests
```

测试覆盖：
- ✅ 操作点配置获取
- ✅ 操作点创建
- ✅ 默认值设置
- ✅ 参数类型验证
- ✅ 颜色控件类型
- ✅ 异常处理

## 与 ExamLab 的一致性

该实现与 ExamLab 项目保持高度一致：

1. **参数模板结构**：使用相同的参数模板定义
2. **默认值策略**：采用相同的默认值设置原则
3. **颜色控件**：统一使用 ParameterType.Color 类型
4. **命名规范**：保持一致的参数命名和分类

## 扩展性

该服务设计具有良好的扩展性：

1. **新增操作点**：在相应的初始化方法中添加配置
2. **参数类型扩展**：支持新的参数类型定义
3. **默认值策略**：可根据需要调整默认值策略
4. **分类管理**：支持新增操作点分类

## 注意事项

1. **单例模式**：WordKnowledgeService 使用单例模式，确保配置一致性
2. **线程安全**：服务实例是线程安全的
3. **内存效率**：配置在启动时加载，运行时只读访问
4. **错误处理**：对不存在的操作点会抛出 ArgumentException

---

*该文档描述了 BenchSuite 项目中 WordKnowledgeService 的完整功能和使用方法。*
