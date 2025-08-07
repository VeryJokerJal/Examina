using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Data.Excel;

/// <summary>
/// Excel枚举数据定义类 - 包含所有Excel操作中使用的枚举值
/// </summary>
public static class ExcelEnumData
{
    /// <summary>
    /// 获取所有枚举类型定义
    /// </summary>
    /// <returns></returns>
    public static List<ExcelEnumType> GetEnumTypes()
    {
        return new List<ExcelEnumType>
        {
            // 对齐方式
            new ExcelEnumType
            {
                Id = 1,
                TypeName = "HorizontalAlignment",
                Description = "水平对齐方式",
                Category = "对齐方式"
            },
            new ExcelEnumType
            {
                Id = 2,
                TypeName = "VerticalAlignment",
                Description = "垂直对齐方式",
                Category = "对齐方式"
            },
            // 边框样式
            new ExcelEnumType
            {
                Id = 3,
                TypeName = "BorderStyle",
                Description = "边框线样式",
                Category = "边框样式"
            },
            // 字体样式
            new ExcelEnumType
            {
                Id = 4,
                TypeName = "FontStyle",
                Description = "字体样式",
                Category = "字体样式"
            },
            new ExcelEnumType
            {
                Id = 5,
                TypeName = "UnderlineStyle",
                Description = "下划线样式",
                Category = "字体样式"
            },
            // 数字格式
            new ExcelEnumType
            {
                Id = 6,
                TypeName = "NumberFormat",
                Description = "数字分类格式",
                Category = "数字格式"
            },
            // 填充图案
            new ExcelEnumType
            {
                Id = 7,
                TypeName = "PatternStyle",
                Description = "图案填充样式",
                Category = "填充样式"
            },
            // 图表类型
            new ExcelEnumType
            {
                Id = 8,
                TypeName = "ChartType",
                Description = "图表类型",
                Category = "图表"
            },
            // 图例位置
            new ExcelEnumType
            {
                Id = 9,
                TypeName = "LegendPosition",
                Description = "图例位置",
                Category = "图表"
            },
            // 数据标签位置
            new ExcelEnumType
            {
                Id = 10,
                TypeName = "DataLabelPosition",
                Description = "数据标签位置",
                Category = "图表"
            },
            // 填充类型
            new ExcelEnumType
            {
                Id = 11,
                TypeName = "FillType",
                Description = "填充类型",
                Category = "填充样式"
            },
            // 单元格样式
            new ExcelEnumType
            {
                Id = 12,
                TypeName = "CellStyle",
                Description = "单元格样式",
                Category = "样式"
            }
        };
    }

    /// <summary>
    /// 获取所有枚举值定义
    /// </summary>
    /// <returns></returns>
    public static List<ExcelEnumValue> GetEnumValues()
    {
        List<ExcelEnumValue> enumValues = new List<ExcelEnumValue>();

        // 水平对齐方式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 1, EnumTypeId = 1, EnumKey = "xlGeneral", EnumValue = 1, DisplayName = "默认", Description = "数值右对齐，文本左对齐", SortOrder = 1 },
            new ExcelEnumValue { Id = 2, EnumTypeId = 1, EnumKey = "xlLeft", EnumValue = -4131, DisplayName = "左对齐", SortOrder = 2 },
            new ExcelEnumValue { Id = 3, EnumTypeId = 1, EnumKey = "xlCenter", EnumValue = -4108, DisplayName = "居中对齐", SortOrder = 3 },
            new ExcelEnumValue { Id = 4, EnumTypeId = 1, EnumKey = "xlRight", EnumValue = -4152, DisplayName = "右对齐", SortOrder = 4 },
            new ExcelEnumValue { Id = 5, EnumTypeId = 1, EnumKey = "xlFill", EnumValue = 5, DisplayName = "填充", Description = "内容重复填满单元格", SortOrder = 5 },
            new ExcelEnumValue { Id = 6, EnumTypeId = 1, EnumKey = "xlJustify", EnumValue = -4130, DisplayName = "两端对齐", Description = "多行内容适用", SortOrder = 6 },
            new ExcelEnumValue { Id = 7, EnumTypeId = 1, EnumKey = "xlCenterAcrossSelection", EnumValue = 7, DisplayName = "跨列居中", Description = "非合并单元格", SortOrder = 7 },
            new ExcelEnumValue { Id = 8, EnumTypeId = 1, EnumKey = "xlDistributed", EnumValue = -4117, DisplayName = "分散对齐", Description = "需启用自动换行", SortOrder = 8 }
        });

        // 垂直对齐方式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 9, EnumTypeId = 2, EnumKey = "xlTop", EnumValue = -4160, DisplayName = "顶端对齐", Description = "内容贴近单元格上边缘", SortOrder = 1 },
            new ExcelEnumValue { Id = 10, EnumTypeId = 2, EnumKey = "xlCenter", EnumValue = -4108, DisplayName = "垂直居中对齐", Description = "内容居中于上下边缘之间", SortOrder = 2 },
            new ExcelEnumValue { Id = 11, EnumTypeId = 2, EnumKey = "xlBottom", EnumValue = -4107, DisplayName = "底端对齐", Description = "内容贴近单元格下边缘（默认）", SortOrder = 3, IsDefault = true },
            new ExcelEnumValue { Id = 12, EnumTypeId = 2, EnumKey = "xlJustify", EnumValue = -4130, DisplayName = "两端对齐", Description = "内容在上下方向也平均分布", SortOrder = 4 },
            new ExcelEnumValue { Id = 13, EnumTypeId = 2, EnumKey = "xlDistributed", EnumValue = -4117, DisplayName = "分散对齐", Description = "类似两端对齐但分布更均匀", SortOrder = 5 }
        });

        // 边框样式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 14, EnumTypeId = 3, EnumKey = "xlLineStyleNone", EnumValue = -4142, DisplayName = "无边框", SortOrder = 1 },
            new ExcelEnumValue { Id = 15, EnumTypeId = 3, EnumKey = "xlContinuous", EnumValue = 1, DisplayName = "实线", SortOrder = 2, IsDefault = true },
            new ExcelEnumValue { Id = 16, EnumTypeId = 3, EnumKey = "xlDash", EnumValue = -4115, DisplayName = "虚线", SortOrder = 3 },
            new ExcelEnumValue { Id = 17, EnumTypeId = 3, EnumKey = "xlDot", EnumValue = -4118, DisplayName = "点线", SortOrder = 4 },
            new ExcelEnumValue { Id = 18, EnumTypeId = 3, EnumKey = "xlDashDot", EnumValue = 4, DisplayName = "点划线", SortOrder = 5 },
            new ExcelEnumValue { Id = 19, EnumTypeId = 3, EnumKey = "xlDashDotDot", EnumValue = 5, DisplayName = "双点划线", SortOrder = 6 },
            new ExcelEnumValue { Id = 20, EnumTypeId = 3, EnumKey = "xlDouble", EnumValue = -4119, DisplayName = "双线", SortOrder = 7 }
        });

        // 字体样式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 21, EnumTypeId = 4, EnumKey = "Regular", DisplayName = "常规", Description = "默认字体样式", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 22, EnumTypeId = 4, EnumKey = "Bold", DisplayName = "粗体", Description = "加粗显示", SortOrder = 2 },
            new ExcelEnumValue { Id = 23, EnumTypeId = 4, EnumKey = "Italic", DisplayName = "斜体", Description = "右倾斜显示", SortOrder = 3 },
            new ExcelEnumValue { Id = 24, EnumTypeId = 4, EnumKey = "BoldItalic", DisplayName = "粗斜体", Description = "加粗 + 斜体", SortOrder = 4 }
        });

        // 下划线样式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 25, EnumTypeId = 5, EnumKey = "xlUnderlineStyleNone", DisplayName = "无下划线", Description = "默认，不添加任何下划线", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 26, EnumTypeId = 5, EnumKey = "xlUnderlineStyleSingle", DisplayName = "单下划线", Description = "添加一条常规下划线", SortOrder = 2 },
            new ExcelEnumValue { Id = 27, EnumTypeId = 5, EnumKey = "xlUnderlineStyleDouble", DisplayName = "双下划线", Description = "添加两条平行下划线", SortOrder = 3 },
            new ExcelEnumValue { Id = 28, EnumTypeId = 5, EnumKey = "xlUnderlineStyleSingleAccounting", DisplayName = "会计用单下划线", Description = "用于财务，位置稍低", SortOrder = 4 },
            new ExcelEnumValue { Id = 29, EnumTypeId = 5, EnumKey = "xlUnderlineStyleDoubleAccounting", DisplayName = "会计用双下划线", Description = "双下划线，财务专用风格", SortOrder = 5 }
        });

        // 数字格式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 30, EnumTypeId = 6, EnumKey = "General", EnumValue = 0, DisplayName = "常规", Description = "自动识别显示（默认格式）", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 31, EnumTypeId = 6, EnumKey = "Number", EnumValue = 1, DisplayName = "数值", Description = "1234.56（可设置小数位数/千位分隔）", SortOrder = 2 },
            new ExcelEnumValue { Id = 32, EnumTypeId = 6, EnumKey = "Currency", EnumValue = 2, DisplayName = "货币", Description = "¥1,234.56", SortOrder = 3 },
            new ExcelEnumValue { Id = 33, EnumTypeId = 6, EnumKey = "Accounting", EnumValue = 4, DisplayName = "会计专用", Description = "¥ 1,234.56", SortOrder = 4 },
            new ExcelEnumValue { Id = 34, EnumTypeId = 6, EnumKey = "Date", EnumValue = 14, DisplayName = "日期（短）", Description = "2025/07/31", SortOrder = 5 },
            new ExcelEnumValue { Id = 35, EnumTypeId = 6, EnumKey = "Time", EnumValue = 20, DisplayName = "时间", Description = "13:30:55", SortOrder = 6 },
            new ExcelEnumValue { Id = 36, EnumTypeId = 6, EnumKey = "Percentage", EnumValue = 9, DisplayName = "百分比", Description = "85%", SortOrder = 7 },
            new ExcelEnumValue { Id = 37, EnumTypeId = 6, EnumKey = "Fraction", EnumValue = 5, DisplayName = "分数", Description = "1 1/4", SortOrder = 8 },
            new ExcelEnumValue { Id = 38, EnumTypeId = 6, EnumKey = "Scientific", EnumValue = 11, DisplayName = "科学计数法", Description = "1.23E+03", SortOrder = 9 },
            new ExcelEnumValue { Id = 39, EnumTypeId = 6, EnumKey = "Text", EnumValue = 49, DisplayName = "文本", Description = "保留输入内容格式", SortOrder = 10 },
            new ExcelEnumValue { Id = 40, EnumTypeId = 6, EnumKey = "Special", EnumValue = 12, DisplayName = "特殊格式", Description = "如邮政编码", SortOrder = 11 }
        });

        // 图案填充样式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 41, EnumTypeId = 7, EnumKey = "xlPatternNone", DisplayName = "无填充", Description = "不使用图案", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 42, EnumTypeId = 7, EnumKey = "xlSolid", DisplayName = "实心填充", Description = "纯色填充", SortOrder = 2 },
            new ExcelEnumValue { Id = 43, EnumTypeId = 7, EnumKey = "xlGray8", DisplayName = "细点状", Description = "小点分布", SortOrder = 3 },
            new ExcelEnumValue { Id = 44, EnumTypeId = 7, EnumKey = "xlGray6", DisplayName = "中等点状", Description = "点稍密一些", SortOrder = 4 },
            new ExcelEnumValue { Id = 45, EnumTypeId = 7, EnumKey = "xlGray5", DisplayName = "密集点状", Description = "点更密", SortOrder = 5 },
            new ExcelEnumValue { Id = 46, EnumTypeId = 7, EnumKey = "xlUp", DisplayName = "斜线（右上）", Description = "从左下到右上的斜线", SortOrder = 6 },
            new ExcelEnumValue { Id = 47, EnumTypeId = 7, EnumKey = "xlDown", DisplayName = "斜线（左上）", Description = "从右下到左上的斜线", SortOrder = 7 },
            new ExcelEnumValue { Id = 48, EnumTypeId = 7, EnumKey = "xlCrissCross", DisplayName = "十字交叉线", Description = "横竖线交叉", SortOrder = 8 },
            new ExcelEnumValue { Id = 49, EnumTypeId = 7, EnumKey = "xlGrid", DisplayName = "网格线", Description = "网格状线条", SortOrder = 9 },
            new ExcelEnumValue { Id = 50, EnumTypeId = 7, EnumKey = "xlHorizontal", DisplayName = "水平线", Description = "横向线条", SortOrder = 10 },
            new ExcelEnumValue { Id = 51, EnumTypeId = 7, EnumKey = "xlVertical", DisplayName = "垂直线", Description = "纵向线条", SortOrder = 11 }
        });

        // 图表类型
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 52, EnumTypeId = 8, EnumKey = "xlColumnClustered", EnumValue = 51, DisplayName = "簇状柱形图", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 53, EnumTypeId = 8, EnumKey = "xlColumnStacked", EnumValue = 52, DisplayName = "堆积柱形图", SortOrder = 2 },
            new ExcelEnumValue { Id = 54, EnumTypeId = 8, EnumKey = "xlColumnStacked100", EnumValue = 53, DisplayName = "百分比堆积柱形图", SortOrder = 3 },
            new ExcelEnumValue { Id = 55, EnumTypeId = 8, EnumKey = "xlBarClustered", EnumValue = 57, DisplayName = "簇状条形图", SortOrder = 4 },
            new ExcelEnumValue { Id = 56, EnumTypeId = 8, EnumKey = "xlBarStacked", EnumValue = 58, DisplayName = "堆积条形图", SortOrder = 5 },
            new ExcelEnumValue { Id = 57, EnumTypeId = 8, EnumKey = "xlBarStacked100", EnumValue = 59, DisplayName = "百分比堆积条形图", SortOrder = 6 },
            new ExcelEnumValue { Id = 58, EnumTypeId = 8, EnumKey = "xlLine", EnumValue = 4, DisplayName = "折线图", SortOrder = 7 },
            new ExcelEnumValue { Id = 59, EnumTypeId = 8, EnumKey = "xlLineMarkers", EnumValue = 65, DisplayName = "带数据标记的折线图", SortOrder = 8 },
            new ExcelEnumValue { Id = 60, EnumTypeId = 8, EnumKey = "xlPie", EnumValue = 5, DisplayName = "饼图", SortOrder = 9 },
            new ExcelEnumValue { Id = 61, EnumTypeId = 8, EnumKey = "xlPieExploded", EnumValue = 69, DisplayName = "分离型饼图", SortOrder = 10 },
            new ExcelEnumValue { Id = 62, EnumTypeId = 8, EnumKey = "xlDoughnut", EnumValue = -4120, DisplayName = "圆环图", SortOrder = 11 },
            new ExcelEnumValue { Id = 63, EnumTypeId = 8, EnumKey = "xlArea", EnumValue = 1, DisplayName = "面积图", SortOrder = 12 },
            new ExcelEnumValue { Id = 64, EnumTypeId = 8, EnumKey = "xlXYScatter", EnumValue = -4169, DisplayName = "散点图", SortOrder = 13 },
            new ExcelEnumValue { Id = 65, EnumTypeId = 8, EnumKey = "xlBubble", EnumValue = 15, DisplayName = "气泡图", SortOrder = 14 }
        });

        // 图例位置
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 66, EnumTypeId = 9, EnumKey = "None", DisplayName = "无图例", Description = "不显示图例", SortOrder = 1 },
            new ExcelEnumValue { Id = 67, EnumTypeId = 9, EnumKey = "Right", DisplayName = "图表右侧", Description = "图例显示在图表右侧（默认）", SortOrder = 2, IsDefault = true },
            new ExcelEnumValue { Id = 68, EnumTypeId = 9, EnumKey = "Top", DisplayName = "图表顶部", Description = "图例显示在图表上方", SortOrder = 3 },
            new ExcelEnumValue { Id = 69, EnumTypeId = 9, EnumKey = "Bottom", DisplayName = "图表底部", Description = "图例显示在图表下方", SortOrder = 4 },
            new ExcelEnumValue { Id = 70, EnumTypeId = 9, EnumKey = "Left", DisplayName = "图表左侧", Description = "图例显示在图表左侧", SortOrder = 5 }
        });

        // 数据标签位置
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 71, EnumTypeId = 10, EnumKey = "xlLabelPositionInsideEnd", DisplayName = "内部末端", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 72, EnumTypeId = 10, EnumKey = "xlLabelPositionOutsideEnd", DisplayName = "外部末端", SortOrder = 2 },
            new ExcelEnumValue { Id = 73, EnumTypeId = 10, EnumKey = "xlLabelPositionCenter", DisplayName = "居中", SortOrder = 3 },
            new ExcelEnumValue { Id = 74, EnumTypeId = 10, EnumKey = "xlLabelPositionAbove", DisplayName = "上方", SortOrder = 4 },
            new ExcelEnumValue { Id = 75, EnumTypeId = 10, EnumKey = "xlLabelPositionBelow", DisplayName = "下方", SortOrder = 5 }
        });

        // 填充类型
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 76, EnumTypeId = 11, EnumKey = "msoFillSolid", DisplayName = "实心填充", Description = "单一颜色填充", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 77, EnumTypeId = 11, EnumKey = "msoFillGradient", DisplayName = "渐变填充", Description = "多种颜色渐变过渡填充", SortOrder = 2 },
            new ExcelEnumValue { Id = 78, EnumTypeId = 11, EnumKey = "msoFillPatterned", DisplayName = "图案填充", Description = "可选图案（点线格等）填充", SortOrder = 3 },
            new ExcelEnumValue { Id = 79, EnumTypeId = 11, EnumKey = "msoFillTextured", DisplayName = "纹理填充", Description = "使用内置图片纹理填充", SortOrder = 4 },
            new ExcelEnumValue { Id = 80, EnumTypeId = 11, EnumKey = "msoFillPicture", DisplayName = "图片填充", Description = "使用外部图片作为背景", SortOrder = 5 },
            new ExcelEnumValue { Id = 81, EnumTypeId = 11, EnumKey = "msoFillNone", DisplayName = "无填充", Description = "不填充（完全透明）", SortOrder = 6 }
        });

        // 单元格样式
        enumValues.AddRange(new[]
        {
            new ExcelEnumValue { Id = 82, EnumTypeId = 12, EnumKey = "Normal", DisplayName = "常规", Description = "默认样式，常规字体，无格式", SortOrder = 1, IsDefault = true },
            new ExcelEnumValue { Id = 83, EnumTypeId = 12, EnumKey = "Bad", DisplayName = "错误", Description = "红色填充，用于表示错误值", SortOrder = 2 },
            new ExcelEnumValue { Id = 84, EnumTypeId = 12, EnumKey = "Good", DisplayName = "正确", Description = "绿色填充，用于表示通过、合格等", SortOrder = 3 },
            new ExcelEnumValue { Id = 85, EnumTypeId = 12, EnumKey = "Neutral", DisplayName = "中性", Description = "灰色填充，用于中性状态", SortOrder = 4 },
            new ExcelEnumValue { Id = 86, EnumTypeId = 12, EnumKey = "Calculation", DisplayName = "计算", Description = "强调计算字段", SortOrder = 5 },
            new ExcelEnumValue { Id = 87, EnumTypeId = 12, EnumKey = "CheckCell", DisplayName = "检查单元格", Description = "黄色填充，用于提示检查的单元格", SortOrder = 6 },
            new ExcelEnumValue { Id = 88, EnumTypeId = 12, EnumKey = "ExplanatoryText", DisplayName = "说明文字", Description = "灰色斜体小字样式", SortOrder = 7 },
            new ExcelEnumValue { Id = 89, EnumTypeId = 12, EnumKey = "Input", DisplayName = "输入", Description = "浅黄色填充，表示用户可编辑单元格", SortOrder = 8 },
            new ExcelEnumValue { Id = 90, EnumTypeId = 12, EnumKey = "LinkedCell", DisplayName = "关联单元格", Description = "蓝色字体，用于表示与外部链接的单元格", SortOrder = 9 },
            new ExcelEnumValue { Id = 91, EnumTypeId = 12, EnumKey = "Note", DisplayName = "注释", Description = "灰色背景，表示说明、注释内容", SortOrder = 10 },
            new ExcelEnumValue { Id = 92, EnumTypeId = 12, EnumKey = "Output", DisplayName = "输出", Description = "绿色背景，表示输出结果或最终值", SortOrder = 11 },
            new ExcelEnumValue { Id = 93, EnumTypeId = 12, EnumKey = "Heading1", DisplayName = "标题1", Description = "黑体 14 号，大标题", SortOrder = 12 },
            new ExcelEnumValue { Id = 94, EnumTypeId = 12, EnumKey = "Heading2", DisplayName = "标题2", Description = "黑体 12 号，副标题", SortOrder = 13 },
            new ExcelEnumValue { Id = 95, EnumTypeId = 12, EnumKey = "Heading3", DisplayName = "标题3", Description = "黑体 11 号，小标题", SortOrder = 14 },
            new ExcelEnumValue { Id = 96, EnumTypeId = 12, EnumKey = "Heading4", DisplayName = "标题4", Description = "黑体 10 号，最小标题", SortOrder = 15 },
            new ExcelEnumValue { Id = 97, EnumTypeId = 12, EnumKey = "Title", DisplayName = "标题", Description = "默认大号标题，蓝色背景，白色字体", SortOrder = 16 },
            new ExcelEnumValue { Id = 98, EnumTypeId = 12, EnumKey = "Total", DisplayName = "总计", Description = "加粗底纹表示汇总单元格", SortOrder = 17 },
            new ExcelEnumValue { Id = 99, EnumTypeId = 12, EnumKey = "Comma", DisplayName = "千分位格式", Description = "数字样式，带千分位，不显示小数", SortOrder = 18 },
            new ExcelEnumValue { Id = 100, EnumTypeId = 12, EnumKey = "Currency", DisplayName = "货币", Description = "数字加货币符号，如￥、$等", SortOrder = 19 },
            new ExcelEnumValue { Id = 101, EnumTypeId = 12, EnumKey = "Percent", DisplayName = "百分比", Description = "格式为百分比", SortOrder = 20 },
            new ExcelEnumValue { Id = 102, EnumTypeId = 12, EnumKey = "WarningText", DisplayName = "警告文字", Description = "红色粗体，通常用于高亮警告信息", SortOrder = 21 },
            new ExcelEnumValue { Id = 103, EnumTypeId = 12, EnumKey = "Emphasis1", DisplayName = "强调1", Description = "蓝色填充，白色字体", SortOrder = 22 },
            new ExcelEnumValue { Id = 104, EnumTypeId = 12, EnumKey = "Emphasis2", DisplayName = "强调2", Description = "灰色填充，黑色字体", SortOrder = 23 },
            new ExcelEnumValue { Id = 105, EnumTypeId = 12, EnumKey = "Emphasis3", DisplayName = "强调3", Description = "浅紫色填充，白色字体", SortOrder = 24 },
            new ExcelEnumValue { Id = 106, EnumTypeId = 12, EnumKey = "Hyperlink", DisplayName = "超链接", Description = "蓝色字体，带下划线", SortOrder = 25 },
            new ExcelEnumValue { Id = 107, EnumTypeId = 12, EnumKey = "FollowedHyperlink", DisplayName = "已访问的超链接", Description = "紫色字体，带下划线", SortOrder = 26 }
        });

        return enumValues;
    }
}
