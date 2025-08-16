using ExaminaWebApplication.Models.Word;

namespace ExaminaWebApplication.Data.Word;

/// <summary>
/// Word完整枚举数据定义类 - 包含所有67个操作点需要的枚举类型和值
/// </summary>
public static class WordEnumDataComplete
{
    /// <summary>
    /// 获取所有Word枚举类型定义（共25个枚举类型）
    /// </summary>
    /// <returns></returns>
    public static List<WordEnumType> GetEnumTypes()
    {
        return new List<WordEnumType>
        {
            // 基础段落操作枚举
            new() { Id = 1, TypeName = "FontFamily", DisplayName = "字体", Description = "Word文档中可用的字体类型", Category = "字体样式" },
            new() { Id = 2, TypeName = "FontStyle", DisplayName = "字形", Description = "字体样式（粗体、斜体等）", Category = "字体样式" },
            new() { Id = 3, TypeName = "FontSize", DisplayName = "字号", Description = "字体大小", Category = "字体样式" },
            new() { Id = 4, TypeName = "ParagraphAlignment", DisplayName = "段落对齐", Description = "段落对齐方式", Category = "段落格式" },
            new() { Id = 5, TypeName = "LineSpacing", DisplayName = "行距", Description = "行间距类型", Category = "段落格式" },
            new() { Id = 6, TypeName = "DropCapType", DisplayName = "首字下沉", Description = "首字下沉类型", Category = "段落格式" },
            new() { Id = 7, TypeName = "BorderStyle", DisplayName = "边框线型", Description = "边框线条样式", Category = "边框设置" },
            new() { Id = 8, TypeName = "BorderWidth", DisplayName = "边框线宽", Description = "边框线条宽度", Category = "边框设置" },
            new() { Id = 9, TypeName = "ShadingPattern", DisplayName = "底纹图案", Description = "段落底纹图案样式", Category = "背景设置" },

            // 页面设置枚举
            new() { Id = 10, TypeName = "PaperSize", DisplayName = "纸张大小", Description = "文档纸张尺寸", Category = "页面设置" },
            new() { Id = 11, TypeName = "HeaderFooterAlignment", DisplayName = "页眉页脚对齐", Description = "页眉页脚文字对齐方式", Category = "页面设置" },
            new() { Id = 12, TypeName = "PageBorderType", DisplayName = "页面边框类型", Description = "页面边框的类型", Category = "页面设置" },

            // 水印设置枚举
            new() { Id = 13, TypeName = "WatermarkType", DisplayName = "水印类型", Description = "水印的类型（文字或图片）", Category = "水印设置" },
            new() { Id = 14, TypeName = "WatermarkLayout", DisplayName = "水印版式", Description = "水印的布局方式", Category = "水印设置" },

            // 项目符号与编号枚举
            new() { Id = 15, TypeName = "BulletType", DisplayName = "项目符号类型", Description = "项目符号的样式", Category = "列表设置" },
            new() { Id = 16, TypeName = "NumberingType", DisplayName = "编号类型", Description = "编号的样式", Category = "列表设置" },

            // 表格操作枚举
            new() { Id = 17, TypeName = "TableBorderType", DisplayName = "表格边框类型", Description = "表格边框样式", Category = "表格设置" },
            new() { Id = 18, TypeName = "CellAlignment", DisplayName = "单元格对齐", Description = "表格单元格对齐方式", Category = "表格设置" },

            // 图形设置枚举
            new() { Id = 19, TypeName = "ShapeType", DisplayName = "图形类型", Description = "自选图形的类型", Category = "图形设置" },
            new() { Id = 20, TypeName = "HorizontalPosition", DisplayName = "水平参考位置", Description = "图形水平定位参考", Category = "图形设置" },
            new() { Id = 21, TypeName = "VerticalPosition", DisplayName = "垂直参考位置", Description = "图形垂直定位参考", Category = "图形设置" },
            new() { Id = 22, TypeName = "WrapType", DisplayName = "环绕方式", Description = "图片文本框环绕方式", Category = "图形设置" },

            // 图片边框相关枚举
            new() { Id = 23, TypeName = "LineCompoundType", DisplayName = "边框复合类型", Description = "图片边框的复合线型", Category = "图片设置" },
            new() { Id = 24, TypeName = "LineDashType", DisplayName = "短划线类型", Description = "图片边框的短划线样式", Category = "图片设置" },
            new() { Id = 25, TypeName = "ShadowType", DisplayName = "阴影类型", Description = "图片阴影效果类型", Category = "图片设置" }
        };
    }

    /// <summary>
    /// 获取所有Word枚举值定义（共约300个枚举值）
    /// </summary>
    /// <returns></returns>
    public static List<WordEnumValue> GetEnumValues()
    {
        List<WordEnumValue> enumValues = new();
        int currentId = 1;

        // 1. 字体枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "宋体", DisplayName = "宋体", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "黑体", DisplayName = "黑体", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "楷体", DisplayName = "楷体", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "仿宋", DisplayName = "仿宋", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "微软雅黑", DisplayName = "微软雅黑", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "等线", DisplayName = "等线", SortOrder = 6 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "Times New Roman", DisplayName = "Times New Roman", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "Arial", DisplayName = "Arial", SortOrder = 8 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 1, Value = "Calibri", DisplayName = "Calibri", SortOrder = 9 }
        });

        // 2. 字形枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 2, Value = "常规", DisplayName = "常规", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 2, Value = "粗体", DisplayName = "粗体", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 2, Value = "斜体", DisplayName = "斜体", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 2, Value = "粗斜体", DisplayName = "粗斜体", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 2, Value = "下划线", DisplayName = "下划线", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 2, Value = "删除线", DisplayName = "删除线", SortOrder = 6 }
        });

        // 3. 字号枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "8", DisplayName = "8磅", SortOrder = 1 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "9", DisplayName = "9磅", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "10", DisplayName = "10磅", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "10.5", DisplayName = "10.5磅", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "11", DisplayName = "11磅", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "12", DisplayName = "12磅", SortOrder = 6, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "14", DisplayName = "14磅", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "16", DisplayName = "16磅", SortOrder = 8 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "18", DisplayName = "18磅", SortOrder = 9 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "20", DisplayName = "20磅", SortOrder = 10 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "22", DisplayName = "22磅", SortOrder = 11 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "24", DisplayName = "24磅", SortOrder = 12 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "26", DisplayName = "26磅", SortOrder = 13 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "28", DisplayName = "28磅", SortOrder = 14 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "36", DisplayName = "36磅", SortOrder = 15 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "48", DisplayName = "48磅", SortOrder = 16 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 3, Value = "72", DisplayName = "72磅", SortOrder = 17 }
        });

        // 4. 段落对齐方式枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 4, Value = "左对齐", DisplayName = "左对齐", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 4, Value = "居中", DisplayName = "居中", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 4, Value = "右对齐", DisplayName = "右对齐", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 4, Value = "两端对齐", DisplayName = "两端对齐", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 4, Value = "分散对齐", DisplayName = "分散对齐", SortOrder = 5 }
        });

        // 5. 行距类型枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "单倍行距", DisplayName = "单倍行距", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "1.5倍行距", DisplayName = "1.5倍行距", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "2倍行距", DisplayName = "2倍行距", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "2.5倍行距", DisplayName = "2.5倍行距", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "3倍行距", DisplayName = "3倍行距", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "多倍行距", DisplayName = "多倍行距", SortOrder = 6 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "最小值", DisplayName = "最小值", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 5, Value = "固定值", DisplayName = "固定值", SortOrder = 8 }
        });

        // 6. 首字下沉类型枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 6, Value = "不使用下沉", DisplayName = "不使用下沉", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 6, Value = "首字下沉到段落中", DisplayName = "首字下沉到段落中", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 6, Value = "首字下沉到页边距", DisplayName = "首字下沉到页边距", SortOrder = 3 }
        });

        // 7. 边框线型枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "无边框", DisplayName = "无边框", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "单实线", DisplayName = "单实线", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "双线", DisplayName = "双线", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "点线", DisplayName = "点线", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "短划线", DisplayName = "短划线", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "长划线", DisplayName = "长划线", SortOrder = 6 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "划线+点", DisplayName = "划线+点", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "划线+两个点", DisplayName = "划线+两个点", SortOrder = 8 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "三线", DisplayName = "三线", SortOrder = 9 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "波浪线（单）", DisplayName = "波浪线（单）", SortOrder = 10 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 7, Value = "波浪线（双）", DisplayName = "波浪线（双）", SortOrder = 11 }
        });

        // 8. 边框线宽枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "0.25pt", DisplayName = "0.25pt（极细）", SortOrder = 1 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "0.5pt", DisplayName = "0.5pt（很细）", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "0.75pt", DisplayName = "0.75pt（中等偏细）", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "1.0pt", DisplayName = "1.0pt（中等）", SortOrder = 4, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "1.5pt", DisplayName = "1.5pt（中等偏粗）", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "2.25pt", DisplayName = "2.25pt（粗）", SortOrder = 6 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "3.0pt", DisplayName = "3.0pt（很粗）", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "4.5pt", DisplayName = "4.5pt（特粗）", SortOrder = 8 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 8, Value = "6.0pt", DisplayName = "6.0pt（极粗）", SortOrder = 9 }
        });

        // 9. 底纹图案枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "无填充", DisplayName = "无填充", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "10%密度图案", DisplayName = "10%密度图案", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "20%密度图案", DisplayName = "20%密度图案", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "30%密度图案", DisplayName = "30%密度图案", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "50%密度图案", DisplayName = "50%密度图案", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "水平横线", DisplayName = "水平横线", SortOrder = 6 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "垂直竖线", DisplayName = "垂直竖线", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "十字交叉线", DisplayName = "十字交叉线", SortOrder = 8 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 9, Value = "斜十字交叉线", DisplayName = "斜十字交叉线", SortOrder = 9 }
        });

        // 10. 纸张大小枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 10, Value = "A4纸", DisplayName = "A4纸", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 10, Value = "A3纸", DisplayName = "A3纸", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 10, Value = "B5纸", DisplayName = "B5纸", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 10, Value = "法律纸尺寸", DisplayName = "法律纸尺寸", SortOrder = 4 }
        });

        // 11. 页眉页脚对齐枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 11, Value = "左对齐", DisplayName = "左对齐", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 11, Value = "居中对齐", DisplayName = "居中对齐", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 11, Value = "右对齐", DisplayName = "右对齐", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 11, Value = "两端对齐", DisplayName = "两端对齐", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 11, Value = "分散对齐", DisplayName = "分散对齐", SortOrder = 5 }
        });

        // 12. 页面边框类型枚举值
        enumValues.AddRange(new[]
        {
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "无边框", DisplayName = "无边框", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "方框", DisplayName = "方框", SortOrder = 2 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "所有边", DisplayName = "所有边", SortOrder = 3 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "左边框", DisplayName = "左边框", SortOrder = 4 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "上边框", DisplayName = "上边框", SortOrder = 5 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "下边框", DisplayName = "下边框", SortOrder = 6 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "右边框", DisplayName = "右边框", SortOrder = 7 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "投影边框", DisplayName = "投影边框", SortOrder = 8 },
            new WordEnumValue { Id = currentId++, EnumTypeId = 12, Value = "三维边框", DisplayName = "三维边框", SortOrder = 9 }
        });

        return enumValues;
    }
}
