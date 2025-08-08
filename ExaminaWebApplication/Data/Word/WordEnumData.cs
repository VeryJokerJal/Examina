using ExaminaWebApplication.Models.Word;

namespace ExaminaWebApplication.Data.Word;

/// <summary>
/// Word枚举数据定义类
/// </summary>
public static class WordEnumData
{
    /// <summary>
    /// 获取所有Word枚举类型定义（共25个枚举类型）
    /// </summary>
    /// <returns></returns>
    public static List<WordEnumType> GetEnumTypes()
    {
        return WordEnumDataComplete.GetEnumTypes();
    }

    /// <summary>
    /// 获取原始的基础枚举类型定义
    /// </summary>
    /// <returns></returns>
    public static List<WordEnumType> GetBasicEnumTypes()
    {
        return new List<WordEnumType>
        {
            // 字体相关枚举
            new WordEnumType { Id = 1, TypeName = "FontFamily", DisplayName = "字体", Description = "Word文档中可用的字体类型", Category = "字体样式" },
            new WordEnumType { Id = 2, TypeName = "FontStyle", DisplayName = "字形", Description = "字体样式（粗体、斜体、下划线等）", Category = "字体样式" },
            new WordEnumType { Id = 3, TypeName = "FontSize", DisplayName = "字号", Description = "字体大小", Category = "字体样式" },

            // 段落格式相关枚举
            new WordEnumType { Id = 4, TypeName = "ParagraphAlignment", DisplayName = "段落对齐方式", Description = "段落文字的对齐方式", Category = "段落格式" },
            new WordEnumType { Id = 5, TypeName = "LineSpacingType", DisplayName = "行距类型", Description = "段落行距的类型", Category = "段落格式" },
            new WordEnumType { Id = 6, TypeName = "DropCapType", DisplayName = "首字下沉类型", Description = "首字下沉的形式", Category = "段落格式" },

            // 边框相关枚举
            new WordEnumType { Id = 7, TypeName = "BorderStyle", DisplayName = "边框线型", Description = "段落边框的线条样式", Category = "边框样式" },
            new WordEnumType { Id = 8, TypeName = "BorderWidth", DisplayName = "边框线宽", Description = "边框线条宽度", Category = "边框样式" },

            // 底纹相关枚举
            new WordEnumType { Id = 9, TypeName = "ShadingPattern", DisplayName = "底纹图案", Description = "段落底纹的图案样式", Category = "背景样式" },

            // 页面设置相关枚举
            new WordEnumType { Id = 10, TypeName = "PaperSize", DisplayName = "纸张大小", Description = "页面纸张尺寸", Category = "页面设置" },
            new WordEnumType { Id = 11, TypeName = "HeaderFooterAlignment", DisplayName = "页眉页脚对齐", Description = "页眉页脚文字对齐方式", Category = "页面设置" },
            new WordEnumType { Id = 12, TypeName = "PageBorderType", DisplayName = "页面边框类型", Description = "页面边框的类型", Category = "页面设置" },

            // 水印相关枚举
            new WordEnumType { Id = 13, TypeName = "WatermarkOrientation", DisplayName = "水印方向", Description = "水印文字的倾斜角度", Category = "水印设置" },

            // 项目符号与编号
            new WordEnumType { Id = 14, TypeName = "ListType", DisplayName = "列表类型", Description = "项目符号与编号类型", Category = "列表设置" },

            // 表格相关枚举
            new WordEnumType { Id = 15, TypeName = "RowHeightType", DisplayName = "行高类型", Description = "表格行高设置类型", Category = "表格设置" },
            new WordEnumType { Id = 16, TypeName = "ColumnWidthType", DisplayName = "列宽类型", Description = "表格列宽设置类型", Category = "表格设置" },
            new WordEnumType { Id = 17, TypeName = "CellVerticalAlignment", DisplayName = "单元格垂直对齐", Description = "表格单元格垂直对齐方式", Category = "表格设置" },
            new WordEnumType { Id = 18, TypeName = "TableAlignment", DisplayName = "表格对齐", Description = "整个表格的对齐方式", Category = "表格设置" },

            // 图形相关枚举
            new WordEnumType { Id = 19, TypeName = "ShapeType", DisplayName = "图形类型", Description = "自选图形的类型", Category = "图形设置" },
            new WordEnumType { Id = 20, TypeName = "HorizontalPosition", DisplayName = "水平参考位置", Description = "图形水平定位参考", Category = "图形设置" },
            new WordEnumType { Id = 21, TypeName = "VerticalPosition", DisplayName = "垂直参考位置", Description = "图形垂直定位参考", Category = "图形设置" },
            new WordEnumType { Id = 22, TypeName = "WrapType", DisplayName = "环绕方式", Description = "图片文本框环绕方式", Category = "图形设置" },

            // 图片边框相关枚举
            new WordEnumType { Id = 23, TypeName = "LineCompoundType", DisplayName = "边框复合类型", Description = "图片边框的复合线型", Category = "图片设置" },
            new WordEnumType { Id = 24, TypeName = "LineDashType", DisplayName = "短划线类型", Description = "图片边框的短划线样式", Category = "图片设置" },
            new WordEnumType { Id = 25, TypeName = "ShadowType", DisplayName = "阴影类型", Description = "图片阴影效果类型", Category = "图片设置" }
        };
    }

    /// <summary>
    /// 获取所有Word枚举值定义（共约300个枚举值）
    /// </summary>
    /// <returns></returns>
    public static List<WordEnumValue> GetEnumValues()
    {
        return WordEnumDataComplete.GetEnumValues();
    }

    /// <summary>
    /// 获取原始的基础枚举值定义
    /// </summary>
    /// <returns></returns>
    public static List<WordEnumValue> GetBasicEnumValues()
    {
        return new List<WordEnumValue>
        {
            // 字体枚举值
            new WordEnumValue { Id = 1, EnumTypeId = 1, Value = "宋体", DisplayName = "宋体", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = 2, EnumTypeId = 1, Value = "黑体", DisplayName = "黑体", SortOrder = 2 },
            new WordEnumValue { Id = 3, EnumTypeId = 1, Value = "楷体", DisplayName = "楷体", SortOrder = 3 },
            new WordEnumValue { Id = 4, EnumTypeId = 1, Value = "仿宋", DisplayName = "仿宋", SortOrder = 4 },
            new WordEnumValue { Id = 5, EnumTypeId = 1, Value = "微软雅黑", DisplayName = "微软雅黑", SortOrder = 5 },
            new WordEnumValue { Id = 6, EnumTypeId = 1, Value = "等线", DisplayName = "等线", SortOrder = 6 },
            new WordEnumValue { Id = 7, EnumTypeId = 1, Value = "Times New Roman", DisplayName = "Times New Roman", SortOrder = 7 },
            new WordEnumValue { Id = 8, EnumTypeId = 1, Value = "Arial", DisplayName = "Arial", SortOrder = 8 },
            new WordEnumValue { Id = 9, EnumTypeId = 1, Value = "Calibri", DisplayName = "Calibri", SortOrder = 9 },

            // 字形枚举值
            new WordEnumValue { Id = 10, EnumTypeId = 2, Value = "Regular", DisplayName = "常规", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = 11, EnumTypeId = 2, Value = "Bold", DisplayName = "粗体", SortOrder = 2 },
            new WordEnumValue { Id = 12, EnumTypeId = 2, Value = "Italic", DisplayName = "斜体", SortOrder = 3 },
            new WordEnumValue { Id = 13, EnumTypeId = 2, Value = "BoldItalic", DisplayName = "粗斜体", SortOrder = 4 },
            new WordEnumValue { Id = 14, EnumTypeId = 2, Value = "Underline", DisplayName = "下划线", SortOrder = 5 },
            new WordEnumValue { Id = 15, EnumTypeId = 2, Value = "Strikethrough", DisplayName = "删除线", SortOrder = 6 },

            // 字号枚举值
            new WordEnumValue { Id = 16, EnumTypeId = 3, Value = "8", DisplayName = "8磅", SortOrder = 1 },
            new WordEnumValue { Id = 17, EnumTypeId = 3, Value = "9", DisplayName = "9磅", SortOrder = 2 },
            new WordEnumValue { Id = 18, EnumTypeId = 3, Value = "10", DisplayName = "10磅", SortOrder = 3 },
            new WordEnumValue { Id = 19, EnumTypeId = 3, Value = "10.5", DisplayName = "10.5磅", SortOrder = 4 },
            new WordEnumValue { Id = 20, EnumTypeId = 3, Value = "11", DisplayName = "11磅", SortOrder = 5 },
            new WordEnumValue { Id = 21, EnumTypeId = 3, Value = "12", DisplayName = "12磅", SortOrder = 6, IsDefault = true },
            new WordEnumValue { Id = 22, EnumTypeId = 3, Value = "14", DisplayName = "14磅", SortOrder = 7 },
            new WordEnumValue { Id = 23, EnumTypeId = 3, Value = "16", DisplayName = "16磅", SortOrder = 8 },
            new WordEnumValue { Id = 24, EnumTypeId = 3, Value = "18", DisplayName = "18磅", SortOrder = 9 },
            new WordEnumValue { Id = 25, EnumTypeId = 3, Value = "20", DisplayName = "20磅", SortOrder = 10 },
            new WordEnumValue { Id = 26, EnumTypeId = 3, Value = "22", DisplayName = "22磅", SortOrder = 11 },
            new WordEnumValue { Id = 27, EnumTypeId = 3, Value = "24", DisplayName = "24磅", SortOrder = 12 },

            // 段落对齐方式枚举值
            new WordEnumValue { Id = 28, EnumTypeId = 4, Value = "Left", DisplayName = "左对齐", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = 29, EnumTypeId = 4, Value = "Center", DisplayName = "居中", SortOrder = 2 },
            new WordEnumValue { Id = 30, EnumTypeId = 4, Value = "Right", DisplayName = "右对齐", SortOrder = 3 },
            new WordEnumValue { Id = 31, EnumTypeId = 4, Value = "Justify", DisplayName = "两端对齐", SortOrder = 4 },
            new WordEnumValue { Id = 32, EnumTypeId = 4, Value = "Distributed", DisplayName = "分散对齐", SortOrder = 5 },

            // 行距类型枚举值
            new WordEnumValue { Id = 33, EnumTypeId = 5, Value = "Single", DisplayName = "单倍行距", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = 34, EnumTypeId = 5, Value = "1.5", DisplayName = "1.5倍行距", SortOrder = 2 },
            new WordEnumValue { Id = 35, EnumTypeId = 5, Value = "Double", DisplayName = "2倍行距", SortOrder = 3 },
            new WordEnumValue { Id = 36, EnumTypeId = 5, Value = "Multiple", DisplayName = "多倍行距", SortOrder = 4 },
            new WordEnumValue { Id = 37, EnumTypeId = 5, Value = "AtLeast", DisplayName = "最小值", SortOrder = 5 },
            new WordEnumValue { Id = 38, EnumTypeId = 5, Value = "Exactly", DisplayName = "固定值", SortOrder = 6 },

            // 边框线型枚举值
            new WordEnumValue { Id = 39, EnumTypeId = 6, Value = "None", DisplayName = "无边框", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = 40, EnumTypeId = 6, Value = "Solid", DisplayName = "实线", SortOrder = 2 },
            new WordEnumValue { Id = 41, EnumTypeId = 6, Value = "Dotted", DisplayName = "点线", SortOrder = 3 },
            new WordEnumValue { Id = 42, EnumTypeId = 6, Value = "Dashed", DisplayName = "虚线", SortOrder = 4 },
            new WordEnumValue { Id = 43, EnumTypeId = 6, Value = "Double", DisplayName = "双线", SortOrder = 5 },
            new WordEnumValue { Id = 44, EnumTypeId = 6, Value = "Thick", DisplayName = "粗线", SortOrder = 6 },

            // 底纹图案枚举值
            new WordEnumValue { Id = 45, EnumTypeId = 7, Value = "Clear", DisplayName = "无填充", SortOrder = 1, IsDefault = true },
            new WordEnumValue { Id = 46, EnumTypeId = 7, Value = "Solid", DisplayName = "实心", SortOrder = 2 },
            new WordEnumValue { Id = 47, EnumTypeId = 7, Value = "Percent5", DisplayName = "5%", SortOrder = 3 },
            new WordEnumValue { Id = 48, EnumTypeId = 7, Value = "Percent10", DisplayName = "10%", SortOrder = 4 },
            new WordEnumValue { Id = 49, EnumTypeId = 7, Value = "Percent20", DisplayName = "20%", SortOrder = 5 },
            new WordEnumValue { Id = 50, EnumTypeId = 7, Value = "Percent25", DisplayName = "25%", SortOrder = 6 },
            new WordEnumValue { Id = 51, EnumTypeId = 7, Value = "Percent30", DisplayName = "30%", SortOrder = 7 },
            new WordEnumValue { Id = 52, EnumTypeId = 7, Value = "Percent40", DisplayName = "40%", SortOrder = 8 },
            new WordEnumValue { Id = 53, EnumTypeId = 7, Value = "Percent50", DisplayName = "50%", SortOrder = 9 },
            new WordEnumValue { Id = 54, EnumTypeId = 7, Value = "Percent60", DisplayName = "60%", SortOrder = 10 },
            new WordEnumValue { Id = 55, EnumTypeId = 7, Value = "Percent70", DisplayName = "70%", SortOrder = 11 },
            new WordEnumValue { Id = 56, EnumTypeId = 7, Value = "Percent75", DisplayName = "75%", SortOrder = 12 },
            new WordEnumValue { Id = 57, EnumTypeId = 7, Value = "Percent80", DisplayName = "80%", SortOrder = 13 },
            new WordEnumValue { Id = 58, EnumTypeId = 7, Value = "Percent90", DisplayName = "90%", SortOrder = 14 }
        };
    }
}
