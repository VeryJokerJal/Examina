# ExamLab Word文档生成功能

## 概述

ExamLab项目的Word文档生成功能提供了完整的Word文档自动生成能力，支持67个不同的Word操作点，涵盖了段落操作、页面设置、水印、表格、图形、文本框等各个方面。

## 功能特性

### 支持的操作点类别

1. **段落操作（14个操作点）**
   - 段落字体、字号、字形设置
   - 字间距、文字颜色设置
   - 段落对齐方式、缩进设置
   - 行间距、首字下沉
   - 段落间距、边框设置
   - 段落底纹设置

2. **页面设置（15个操作点）**
   - 纸张大小、页边距设置
   - 页眉/页脚文字、字体、字号、对齐方式
   - 页码设置
   - 页面背景、边框设置

3. **水印设置（4个操作点）**
   - 水印文字、字体、字号
   - 水印方向设置

4. **项目符号与编号（1个操作点）**
   - 项目编号设置

5. **表格操作（10个操作点）**
   - 表格行数列数、底纹设置
   - 行高、列宽设置
   - 单元格内容、对齐方式
   - 表格对齐、单元格合并
   - 表格标题设置

6. **图形和图片设置（16个操作点）**
   - 自选图形插入、大小设置
   - 线条颜色、填充颜色
   - 图形文字设置
   - 图形位置设置
   - 图片边框、阴影、环绕方式
   - 图片尺寸、位置设置

7. **文本框设置（5个操作点）**
   - 文本框边框颜色
   - 文字内容、大小
   - 位置、环绕方式

8. **其他操作（2个操作点）**
   - 查找与替换
   - 指定文字字号设置

## 核心组件

### 1. IDocumentGenerationService 接口
定义了文档生成服务的标准接口，包括：
- 模块验证
- 文档生成
- 文件类型信息

### 2. WordDocumentGenerator 类
Word文档生成器的主要实现类，负责：
- 验证Word模块
- 创建Word文档
- 应用各种操作点
- 进度报告

### 3. 扩展类
- `WordDocumentGeneratorExtensions`: 包含表格、图形等操作方法
- `WordDocumentGeneratorExtensions2`: 包含文本框、其他操作等方法

### 4. 模型类
- `DocumentValidationResult`: 文档验证结果
- `DocumentGenerationProgress`: 文档生成进度
- `DocumentGenerationResult`: 文档生成结果

## 使用方法

### 基本用法

```csharp
// 创建Word文档生成器
WordDocumentGenerator generator = new();

// 验证模块
DocumentValidationResult validation = generator.ValidateModule(module);
if (!validation.IsValid)
{
    // 处理验证错误
    Console.WriteLine($"验证失败: {string.Join(", ", validation.ErrorMessages)}");
    return;
}

// 创建进度报告器
Progress<DocumentGenerationProgress> progress = new(p =>
{
    Console.WriteLine($"进度: {p.ProgressPercentage}% - {p.CurrentStep}");
});

// 生成文档
DocumentGenerationResult result = await generator.GenerateDocumentAsync(
    module, 
    filePath, 
    progress
);

if (result.IsSuccess)
{
    Console.WriteLine($"文档生成成功: {result.FilePath}");
}
else
{
    Console.WriteLine($"生成失败: {result.ErrorMessage}");
}
```

### 测试功能

项目包含了完整的测试功能：

```csharp
// 运行测试
await TestWordGeneration.TestAsync();
```

测试将创建一个包含各种操作点的示例文档，验证所有功能是否正常工作。

## 技术实现

### 依赖项
- **DocumentFormat.OpenXml**: 用于创建和操作Office文档
- **System.IO**: 文件操作
- **System.Threading.Tasks**: 异步操作支持

### 架构设计
- **MVVM模式**: 符合ExamLab项目的整体架构
- **模块化设计**: 不同类别的操作点分离到不同的扩展类
- **异步支持**: 所有文档生成操作都是异步的
- **进度报告**: 支持实时进度更新
- **错误处理**: 完善的异常处理和错误报告

### 代码规范
- 使用显式类型声明（避免var关键字）
- 遵循C#命名约定
- 完整的XML文档注释
- 单一职责原则

## 扩展性

### 添加新的操作点
1. 在`WordKnowledgeType`枚举中添加新的操作点类型
2. 在`WordDocumentGenerator`的switch语句中添加新的case
3. 实现对应的操作方法
4. 更新测试用例

### 自定义文档格式
可以通过继承`IDocumentGenerationService`接口来实现其他文档格式的生成器，如PDF、RTF等。

## 注意事项

1. **文件路径**: 确保输出路径有写入权限
2. **内存使用**: 大型文档可能消耗较多内存
3. **异常处理**: 建议在调用时添加try-catch块
4. **参数验证**: 操作点参数会使用默认值，但建议提供完整参数

## 版本历史

- **v1.0**: 初始版本，支持基本的Word操作点
- **v2.0**: 扩展支持所有67个操作点，完整实现WordKnowledgeService规范
- **v2.1**: 添加测试功能和文档说明

## 许可证

本功能是ExamLab项目的一部分，遵循项目的整体许可证协议。
