# Word OpenXML 评分服务完善计划

## 📋 项目概述

本文档详细说明了Word OpenXML评分服务需要完善的内容。根据ExamLab/Services/WordKnowledgeService.cs中定义的67个Word操作点，当前BenchSuite中的Word OpenXML评分服务还需要大量的完善工作。

## ✅ 当前状态分析

### 1. 已实现的检测方法（40个）

当前Word OpenXML评分服务已经实现了40个检测方法，但这些方法主要是通用的文档操作，不是针对ExamLab定义的具体知识点。

### 2. 需要新增的知识点检测方法（67个）

根据ExamLab定义，需要实现以下67个知识点的检测方法：

#### 第一类：段落操作（14个）
1. **SetParagraphFont** - 设置段落的字体
   - 参数：ParagraphNumber（段落序号）, FontFamily（字体类型）
   - 状态：✅ 已开始实现

2. **SetParagraphFontSize** - 设置段落的字号
   - 参数：ParagraphNumber（段落序号）, FontSize（字号值）
   - 状态：✅ 已开始实现

3. **SetParagraphFontStyle** - 设置段落的字形
   - 参数：ParagraphNumber（段落序号）, FontStyle（字形）
   - 状态：✅ 已开始实现

4. **SetParagraphCharacterSpacing** - 设置段落字间距
   - 参数：ParagraphNumber（段落序号）, CharacterSpacing（字间距值）
   - 状态：❌ 需要实现

5. **SetParagraphTextColor** - 设置段落文字的颜色
   - 参数：ParagraphNumber（段落序号）, TextColor（颜色值）
   - 状态：❌ 需要实现

6. **SetParagraphAlignment** - 设置段落对齐方式
   - 参数：ParagraphNumber（段落序号）, Alignment（对齐方式）
   - 状态：❌ 需要实现

7. **SetParagraphIndentation** - 设置段落缩进
   - 参数：ParagraphNumber（段落序号）, FirstLineIndent, LeftIndent, RightIndent
   - 状态：❌ 需要实现

8. **SetParagraphLineSpacing** - 设置行间距
   - 参数：ParagraphNumber（段落序号）, LineSpacing（行间距值）
   - 状态：❌ 需要实现

9. **SetParagraphDropCap** - 首字下沉
   - 参数：ParagraphNumber（段落序号）, DropCapType（首字下沉形式）
   - 状态：❌ 需要实现

10. **SetParagraphSpacing** - 设置段落间距
    - 参数：ParagraphNumber（段落序号）, SpaceBefore, SpaceAfter
    - 状态：❌ 需要实现

11. **SetParagraphBorderColor** - 设置段落边框颜色
    - 参数：ParagraphNumber（段落序号）, BorderColor（边框颜色）
    - 状态：❌ 需要实现

12. **SetParagraphBorderStyle** - 设置段落边框样式
    - 参数：ParagraphNumber（段落序号）, BorderStyle（边框样式）
    - 状态：❌ 需要实现

13. **SetParagraphBorderWidth** - 设置段落边框宽度
    - 参数：ParagraphNumber（段落序号）, BorderWidth（边框宽度）
    - 状态：❌ 需要实现

14. **SetParagraphShading** - 设置段落底纹
    - 参数：ParagraphNumber（段落序号）, ShadingColor, ShadingPattern
    - 状态：❌ 需要实现

#### 第二类：页面设置（15个）
15. **SetPaperSize** - 设置纸张大小
    - 参数：PaperSize（纸张类型）
    - 状态：❌ 需要实现

16. **SetPageMargins** - 设置页边距
    - 参数：TopMargin, BottomMargin, LeftMargin, RightMargin
    - 状态：❌ 需要实现

17. **SetHeaderText** - 设置页眉中的文字
    - 参数：HeaderText（页眉文字内容）
    - 状态：❌ 需要实现

18. **SetHeaderFont** - 设置页眉中文字的字体
    - 参数：HeaderFont（页眉字体名称）
    - 状态：❌ 需要实现

19. **SetHeaderFontSize** - 设置页眉中文字的字号
    - 参数：HeaderFontSize（字号数值）
    - 状态：❌ 需要实现

20. **SetHeaderAlignment** - 设置页眉中文字的对齐方式
    - 参数：HeaderAlignment（对齐方式）
    - 状态：❌ 需要实现

21. **SetFooterText** - 设置页脚中的文字
    - 参数：FooterText（页脚文字）
    - 状态：❌ 需要实现

22. **SetFooterFont** - 设置页脚中文字的字体
    - 参数：FooterFont（字体类型）
    - 状态：❌ 需要实现

23. **SetFooterFontSize** - 设置页脚中文字的字号
    - 参数：FooterFontSize（字号数值）
    - 状态：❌ 需要实现

24. **SetFooterAlignment** - 设置页脚中文字的对齐方式
    - 参数：FooterAlignment（对齐方式）
    - 状态：❌ 需要实现

25. **SetPageNumber** - 设置页码
    - 参数：PageNumberPosition, PageNumberFormat
    - 状态：❌ 需要实现

26. **SetPageBackground** - 设置页面背景
    - 参数：BackgroundColor（背景颜色）
    - 状态：❌ 需要实现

27. **SetPageBorderColor** - 设置页面边框颜色
    - 参数：BorderColor（页面边框颜色）
    - 状态：❌ 需要实现

28. **SetPageBorderStyle** - 设置页面边框样式
    - 参数：BorderStyle（边框样式）
    - 状态：❌ 需要实现

29. **SetPageBorderWidth** - 设置页面边框宽度
    - 参数：BorderWidth（边框宽度）
    - 状态：❌ 需要实现

#### 第三类：水印设置（4个）
30. **SetWatermarkText** - 设置水印文字
    - 参数：WatermarkText（水印文字）
    - 状态：❌ 需要实现

31. **SetWatermarkFont** - 设置水印文字的字体
    - 参数：WatermarkFont（水印字体类型）
    - 状态：❌ 需要实现

32. **SetWatermarkFontSize** - 设置水印文字的字号
    - 参数：WatermarkFontSize（水印字号数值）
    - 状态：❌ 需要实现

33. **SetWatermarkOrientation** - 设置水印文字水平或倾斜方式
    - 参数：WatermarkAngle（水印角度）
    - 状态：❌ 需要实现

#### 第四类：项目符号与编号（1个）
34. **SetBulletNumbering** - 设置项目编号
    - 参数：ParagraphNumbers, NumberingType
    - 状态：❌ 需要实现

#### 第五类：表格操作（10个）
35. **SetTableRowsColumns** - 设置表格的行数和列数
    - 参数：Rows, Columns
    - 状态：❌ 需要实现

36. **SetTableShading** - 设置表格底纹
    - 参数：AreaType, AreaNumber, ShadingColor
    - 状态：❌ 需要实现

37. **SetTableRowHeight** - 设置表格行高
    - 参数：StartRow, EndRow, RowHeight, HeightType
    - 状态：❌ 需要实现

38. **SetTableColumnWidth** - 设置表格列宽
    - 参数：StartColumn, EndColumn, ColumnWidth, WidthType
    - 状态：❌ 需要实现

39. **SetTableCellContent** - 设置单元格内容
    - 参数：RowNumber, ColumnNumber, CellContent
    - 状态：❌ 需要实现

40. **SetTableCellAlignment** - 设置表格单元格对齐方式
    - 参数：RowNumber, ColumnNumber, HorizontalAlignment, VerticalAlignment
    - 状态：❌ 需要实现

41. **SetTableAlignment** - 设置整个表格对齐方式
    - 参数：TableAlignment, LeftIndent
    - 状态：❌ 需要实现

42. **MergeTableCells** - 合并单元格
    - 参数：StartRow, StartColumn, EndRow, EndColumn
    - 状态：❌ 需要实现

43. **SetTableHeaderContent** - 设置表头第一个单元格的内容
    - 参数：ColumnNumber, HeaderContent
    - 状态：❌ 需要实现

44. **SetTableHeaderAlignment** - 设置表头第一个单元格的对齐方式
    - 参数：ColumnNumber, HorizontalAlignment, VerticalAlignment
    - 状态：❌ 需要实现

#### 第六类：图形和图片设置（16个）
45. **InsertAutoShape** - 插入自选图形类型
    - 参数：ShapeType
    - 状态：❌ 需要实现

46. **SetAutoShapeSize** - 设置自选图形大小
    - 参数：ShapeHeight, ShapeWidth
    - 状态：❌ 需要实现

47. **SetAutoShapeLineColor** - 设置自选图形线条颜色
    - 参数：LineColor
    - 状态：❌ 需要实现

48. **SetAutoShapeFillColor** - 设置自选图形填充颜色
    - 参数：FillColor
    - 状态：❌ 需要实现

49. **SetAutoShapeTextSize** - 设置自选图形中文字大小
    - 参数：FontSize
    - 状态：❌ 需要实现

50. **SetAutoShapeTextColor** - 设置自选图形中文字颜色
    - 参数：TextColor
    - 状态：❌ 需要实现

51. **SetAutoShapeTextContent** - 设置自选图形中文字内容
    - 参数：TextContent
    - 状态：❌ 需要实现

52. **SetAutoShapePosition** - 设置自选图形的位置
    - 参数：水平和垂直位置设置
    - 状态：❌ 需要实现

53. **SetImageBorderCompoundType** - 设置插入图片边框复合类型
    - 参数：CompoundType
    - 状态：❌ 需要实现

54. **SetImageBorderDashType** - 设置插入图片边框短划线类型
    - 参数：DashType
    - 状态：❌ 需要实现

55. **SetImageBorderWidth** - 设置插入图片边框线宽
    - 参数：BorderWidth
    - 状态：❌ 需要实现

56. **SetImageBorderColor** - 设置插入图片边框颜色
    - 参数：BorderColor
    - 状态：❌ 需要实现

57. **SetImageShadow** - 设置插入图片阴影类型与颜色
    - 参数：ShadowType, ShadowColor
    - 状态：❌ 需要实现

58. **SetImageWrapStyle** - 设置插入图片环绕方式
    - 参数：WrapStyle
    - 状态：❌ 需要实现

59. **SetImageSize** - 设置插入图片的高度和宽度
    - 参数：ImageHeight, ImageWidth
    - 状态：❌ 需要实现

60. **SetImagePosition** - 设置插入图片的位置
    - 参数：水平和垂直位置设置
    - 状态：❌ 需要实现

#### 第七类：文本框设置（5个）
61. **SetTextBoxBorderColor** - 设置文本框边框颜色
    - 参数：BorderColor
    - 状态：❌ 需要实现

62. **SetTextBoxContent** - 设置文本框中文字内容
    - 参数：TextContent
    - 状态：❌ 需要实现

63. **SetTextBoxTextSize** - 设置文本框中文字大小
    - 参数：TextSize
    - 状态：❌ 需要实现

64. **SetTextBoxPosition** - 设置文本框位置
    - 参数：水平和垂直位置设置
    - 状态：❌ 需要实现

65. **SetTextBoxWrapStyle** - 设置文本框环绕方式
    - 参数：WrapStyle
    - 状态：❌ 需要实现

#### 第八类：其他操作（2个）
66. **FindAndReplace** - 查找与替换
    - 参数：FindText, ReplaceText, ReplaceCount
    - 状态：❌ 需要实现

67. **SetSpecificTextFontSize** - 设置指定文字字号
    - 参数：TargetText, FontSize
    - 状态：❌ 需要实现

## 🔧 技术实现要求

### 1. 检测方法结构

每个检测方法都需要遵循以下结构：
```csharp
private KnowledgePointResult DetectXXX(WordprocessingDocument document, Dictionary<string, string> parameters)
{
    KnowledgePointResult result = new()
    {
        KnowledgePointType = "XXX",
        Parameters = parameters
    };

    try
    {
        // 参数验证
        if (!TryGetParameter(parameters, "ParamName", out string paramValue))
        {
            SetKnowledgePointFailure(result, "缺少必要参数: ParamName");
            return result;
        }

        // 具体检测逻辑
        // ...

        result.ExpectedValue = expectedValue;
        result.ActualValue = actualValue;
        result.IsCorrect = actualValue == expectedValue;
        result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
        result.Details = $"检测详情";
    }
    catch (Exception ex)
    {
        SetKnowledgePointFailure(result, $"检测失败: {ex.Message}");
    }

    return result;
}
```

### 2. 辅助方法要求

每个检测方法都需要相应的辅助方法来解析OpenXML结构：
- 段落相关：`GetParagraphFont`, `GetParagraphFontSize`, `GetParagraphAlignment`等
- 页面相关：`GetPageMargins`, `GetPaperSize`, `GetHeaderText`等
- 表格相关：`GetTableRowCount`, `GetTableColumnCount`, `GetCellContent`等
- 图形相关：`GetAutoShapeProperties`, `GetImageProperties`等

### 3. OpenXML结构解析

需要深入理解Word OpenXML文档结构：
- **Document.Body** - 文档主体
- **Paragraph** - 段落元素
- **Run** - 文本运行
- **RunProperties** - 文本格式属性
- **ParagraphProperties** - 段落格式属性
- **Table** - 表格元素
- **SectionProperties** - 节属性
- **HeaderPart/FooterPart** - 页眉页脚部分

## 📊 工作量估算

### 代码量估算
- **检测方法**：67个 × 平均40行 = 2,680行
- **辅助方法**：约100个 × 平均20行 = 2,000行
- **总计**：约4,680行新增代码

### 时间估算
- **段落操作（14个）**：约2-3天
- **页面设置（15个）**：约3-4天
- **水印设置（4个）**：约1天
- **项目符号与编号（1个）**：约0.5天
- **表格操作（10个）**：约2-3天
- **图形和图片设置（16个）**：约3-4天
- **文本框设置（5个）**：约1-2天
- **其他操作（2个）**：约1天
- **测试和调试**：约2-3天
- **总计**：约15-20个工作日

## 🎯 实施建议

### 1. 分阶段实施
1. **第一阶段**：完成段落操作（14个）
2. **第二阶段**：完成页面设置（15个）
3. **第三阶段**：完成表格操作（10个）
4. **第四阶段**：完成图形和图片设置（16个）
5. **第五阶段**：完成其他功能（12个）

### 2. 优先级排序
1. **高优先级**：段落操作、页面设置（使用频率高）
2. **中优先级**：表格操作、水印设置（常用功能）
3. **低优先级**：图形图片、文本框、其他操作（高级功能）

### 3. 质量保证
- 每个方法都要有完整的参数验证
- 每个方法都要有详细的错误处理
- 每个方法都要有准确的OpenXML解析
- 每个方法都要有完整的单元测试

## 🎉 预期成果

完成后的Word OpenXML评分服务将：
- **✅ 功能完整性**：100%覆盖ExamLab定义的67个Word操作点
- **✅ 检测精确性**：每个知识点都有专门的检测逻辑
- **✅ 参数化支持**：支持丰富的参数验证和匹配
- **✅ 错误处理**：完整的异常处理和边界检查
- **✅ API兼容性**：100%向后兼容，保持接口一致性

这将使Word OpenXML评分服务成为最全面、最精确的Word文档分析工具，完全满足ExamLab系统的所有Word操作点检测需求。
