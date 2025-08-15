# Excel打分功能说明

## 概述

BenchSuite中的Excel打分功能提供了对Excel文档的自动化评分能力，支持检测51个Excel操作点，包括Excel基础操作、数据清单操作和图表操作等多种知识点，与ExamLab中的ExcelKnowledgeService功能完全对应。

## 功能特性

### 支持的文件格式
- `.xls` - Excel 97-2003 工作簿
- `.xlsx` - Excel 2007及以上版本工作簿

### 支持的知识点类型（51个操作点）

#### 第一类：Excel基础操作（42个操作点）

1. **填充或复制单元格内容** (`FillOrCopyCellContent`)
   - 检测指定单元格是否包含期望的值
   - 参数：`TargetWorkbook`, `CellValues`

2. **删除单元格内容** (`DeleteCellContent`)
   - 检测单元格内容是否已删除
   - 参数：`TargetWorkbook`, `CellRange`

3. **插入或删除单元格** (`InsertDeleteCells`)
   - 检测单元格的插入或删除操作
   - 参数：`TargetWorkbook`, `OperationAction`, `CellRange`

4. **合并单元格** (`MergeCells`)
   - 检测指定区域的单元格是否已合并
   - 参数：`TargetWorkbook`, `CellRange`

5. **插入或删除行** (`InsertDeleteRows`)
   - 检测行的插入或删除操作
   - 参数：`TargetWorkbook`, `OperationAction`, `RowNumbers`

6. **设置指定单元格字体** (`SetCellFont`)
   - 检测单元格区域的字体设置
   - 参数：`TargetWorkbook`, `CellRange`, `FontFamily`

7. **设置字型** (`SetFontStyle`)
   - 检测字体样式（粗体、斜体等）
   - 参数：`TargetWorkbook`, `CellRange`, `FontStyle`

8. **设置字号** (`SetFontSize`)
   - 检测字体大小设置
   - 参数：`TargetWorkbook`, `CellRange`, `FontSize`

9. **字体颜色** (`SetFontColor`)
   - 检测字体颜色设置
   - 参数：`TargetWorkbook`, `CellRange`, `FontColor`

10. **内边框样式** (`SetInnerBorderStyle`)
    - 检测内边框样式设置
    - 参数：`TargetWorkbook`, `CellRange`, `BorderStyle`

11. **内边框颜色** (`SetInnerBorderColor`)
    - 检测内边框颜色设置
    - 参数：`TargetWorkbook`, `CellRange`, `BorderColor`

12. **插入或删除列** (`InsertDeleteColumns`)
    - 检测列的插入或删除操作
    - 参数：`TargetWorkbook`, `OperationAction`, `ColumnLetters`

13. **设置水平对齐方式** (`SetHorizontalAlignment`)
    - 检测单元格的水平对齐设置
    - 参数：`TargetWorkbook`, `CellRange`, `HorizontalAlignment`

14. **设置数字格式** (`SetNumberFormat`)
    - 检测数字格式设置
    - 参数：`TargetWorkbook`, `CellRange`, `NumberFormat`

15. **使用函数** (`UseFunction`)
    - 检测单元格是否包含公式且计算结果正确
    - 参数：`TargetWorkbook`, `CellAddress`, `ExpectedValue`

16. **设置行高** (`SetRowHeight`)
    - 检测指定行的高度设置
    - 参数：`TargetWorkbook`, `RowNumbers`, `RowHeight`

17. **设置列宽** (`SetColumnWidth`)
    - 检测指定列的宽度设置
    - 参数：`TargetWorkbook`, `ColumnLetters`, `ColumnWidth`

18. **自动调整行高** (`AutoFitRowHeight`)
    - 检测行高是否自动调整
    - 参数：`TargetWorkbook`, `RowNumbers`

19. **自动调整列宽** (`AutoFitColumnWidth`)
    - 检测列宽是否自动调整
    - 参数：`TargetWorkbook`, `ColumnLetters`

20. **设置单元格填充颜色** (`SetCellFillColor`)
    - 检测单元格填充颜色设置
    - 参数：`TargetWorkbook`, `CellRange`, `FillColor`

21. **设置垂直对齐方式** (`SetVerticalAlignment`)
    - 检测单元格的垂直对齐设置
    - 参数：`TargetWorkbook`, `CellRange`, `VerticalAlignment`

*注：其他基础操作包括图案填充、边框设置、文字换行、冻结窗格、工作表保护、批注、超链接等功能的占位符已实现，可根据需要扩展具体检测逻辑。*

#### 第二类：数据清单操作（6个操作点）

1. **筛选** (`Filter`)
   - 检测数据筛选是否已应用
   - 参数：`TargetWorkbook`, `FilterConditions`

2. **排序** (`Sort`)
   - 检测数据是否按指定条件排序
   - 参数：`TargetWorkbook`, `SortColumn`, `SortOrder`, `HasHeader`

3. **分类汇总** (`Subtotal`)
   - 检测分类汇总是否已创建
   - 参数：`TargetWorkbook`, `GroupByColumn`, `SummaryFunction`, `SummaryColumn`

4. **高级筛选-条件** (`AdvancedFilterCondition`)
   - 检测高级筛选条件设置
   - 参数：`TargetWorkbook`, `ConditionRange`, `FilterField`, `FilterValue`

5. **高级筛选-数据** (`AdvancedFilterData`)
   - 检测高级筛选数据操作
   - 参数：`TargetWorkbook`, `DataRange`, `CriteriaRange`, `CopyToRange`

6. **数据透视表** (`PivotTable`)
   - 检测数据透视表的配置
   - 参数：`TargetWorkbook`, `PivotRowFields`, `PivotDataField`, `PivotFunction`

#### 第三类：图表操作（22个操作点）

1. **图表类型** (`ChartType`)
   - 检测图表的类型设置
   - 参数：`TargetWorkbook`, `ChartType`, `ChartNumber`

2. **图表样式** (`ChartStyle`)
   - 检测图表样式设置
   - 参数：`TargetWorkbook`, `StyleNumber`, `ChartNumber`

3. **图表移动** (`ChartMove`)
   - 检测图表移动操作
   - 参数：`TargetWorkbook`, `MoveLocation`, `ChartNumber`, `TargetSheet`

4. **分类轴数据区域** (`CategoryAxisDataRange`)
   - 检测分类轴数据区域设置
   - 参数：`TargetWorkbook`, `CategoryRange`, `ChartNumber`

5. **数值轴数据区域** (`ValueAxisDataRange`)
   - 检测数值轴数据区域设置
   - 参数：`TargetWorkbook`, `ValueRange`, `ChartNumber`

6. **图表标题** (`ChartTitle`)
   - 检测图表标题的设置
   - 参数：`TargetWorkbook`, `ChartNumber`, `ChartTitle`

*注：其他图表操作包括图表标题格式、坐标轴标题、图例设置、网格线、数据标签、系列格式等功能的占位符已实现，可根据需要扩展具体检测逻辑。*

## 使用方法

### 基本用法

```csharp
// 创建Excel打分服务实例
IExcelScoringService excelScoringService = new ExcelScoringService();

// 检测单个知识点
Dictionary<string, string> parameters = new()
{
    ["TargetWorkbook"] = "工作簿名称",
    ["CellValues"] = "A1：期望值"
};

KnowledgePointResult result = await excelScoringService.DetectKnowledgePointAsync(
    "path/to/excel/file.xlsx",
    "FillOrCopyCellContent",
    parameters);

// 检查结果
if (result.IsCorrect)
{
    Console.WriteLine($"检测通过，得分：{result.AchievedScore}");
}
else
{
    Console.WriteLine($"检测失败：{result.ErrorMessage}");
}
```

### 试卷打分

```csharp
// 创建试卷模型
ExamModel examModel = new ExamModel
{
    // ... 试卷配置
};

// 对整个试卷进行打分
ScoringResult scoringResult = await excelScoringService.ScoreFileAsync(
    "path/to/excel/file.xlsx",
    examModel);

Console.WriteLine($"总分：{scoringResult.TotalScore}");
Console.WriteLine($"得分：{scoringResult.AchievedScore}");
Console.WriteLine($"得分率：{scoringResult.ScoreRate:P}");
```

## 参数说明

### 通用参数
- `TargetWorkbook`: 目标工作簿名称
- `OperationType`: 操作类型（通常为"A"表示工作表操作，"B"表示图表操作）

### 单元格相关参数
- `CellRange`: 单元格区域（如"A1:B10"）
- `CellAddress`: 单元格地址（如"A1"）
- `CellValues`: 单元格值配置（格式："A1：期望值"）

### 格式设置参数
- `FontFamily`: 字体名称
- `HorizontalAlignment`: 水平对齐方式
- `RowHeight`: 行高（磅为单位）
- `ColumnWidth`: 列宽

### 数据操作参数
- `FilterConditions`: 筛选条件（格式："列名：筛选值"）
- `SortColumn`: 排序列
- `SortOrder`: 排序顺序（"升序"或"降序"）
- `HasHeader`: 是否包含标题行

### 图表参数
- `ChartType`: 图表类型
- `ChartNumber`: 图表编号
- `ChartTitle`: 图表标题

## 注意事项

1. **文件路径**: 确保Excel文件路径正确且文件存在
2. **权限**: 确保程序有读取Excel文件的权限
3. **Excel版本**: 需要安装Microsoft Office Excel或兼容的Excel组件
4. **资源管理**: 服务会自动管理Excel COM对象的资源释放
5. **并发**: 建议避免同时对同一个Excel文件进行多次操作

## 错误处理

服务提供了完善的错误处理机制：

- 文件不存在或无法访问
- 不支持的文件格式
- Excel COM对象操作异常
- 参数验证失败
- 知识点检测逻辑错误

所有错误信息都会在返回结果的`ErrorMessage`属性中提供详细说明。

## 扩展性

当前实现提供了基础的Excel知识点检测功能，可以通过以下方式扩展：

1. 添加新的知识点类型到`ExcelKnowledgeType`枚举
2. 在`DetectSpecificKnowledgePoint`方法中添加新的检测逻辑
3. 实现对应的检测方法
4. 更新参数映射和验证逻辑

## 测试

运行测试用例：

```csharp
ExcelScoringServiceTests tests = new();
await tests.RunAllTests();
```

测试包括：
- 文件扩展名验证
- 支持的文件类型检查
- 知识点检测功能
- 试卷打分功能
- 错误处理机制
