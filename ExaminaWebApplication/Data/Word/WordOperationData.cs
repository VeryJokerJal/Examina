using ExaminaWebApplication.Models.Word;

namespace ExaminaWebApplication.Data.Word;

/// <summary>
/// Word段落操作点数据定义类
/// </summary>
public static class WordOperationData
{
    /// <summary>
    /// 获取所有Word操作点定义（共67项）
    /// </summary>
    /// <returns></returns>
    public static List<WordOperationPoint> GetWordOperationPoints()
    {
        return WordOperationDataComplete.GetWordOperationPoints();
    }

    /// <summary>
    /// 获取原始的段落操作点定义（第一类：段落操作14项）
    /// </summary>
    /// <returns></returns>
    public static List<WordOperationPoint> GetParagraphOperationPoints()
    {
        return new List<WordOperationPoint>
        {
            // 第一类：段落操作（共14项）
            // （一）段落文字样式（字体、字号、字形、颜色等）
            new() {
                Id = 1,
                OperationNumber = 1,
                Name = "设置段落的字体",
                Description = "设置指定段落的字体类型",
                Category = WordOperationCategory.ParagraphTextStyle
            },
            new() {
                Id = 2,
                OperationNumber = 2,
                Name = "设置段落的字号",
                Description = "设置指定段落的字体大小",
                Category = WordOperationCategory.ParagraphTextStyle
            },
            new() {
                Id = 3,
                OperationNumber = 3,
                Name = "设置段落的字形",
                Description = "设置指定段落的字体样式（粗体、斜体等）",
                Category = WordOperationCategory.ParagraphTextStyle
            },
            new() {
                Id = 4,
                OperationNumber = 4,
                Name = "设置段落字间距",
                Description = "设置指定段落的字符间距",
                Category = WordOperationCategory.ParagraphTextStyle
            },
            new() {
                Id = 5,
                OperationNumber = 5,
                Name = "设置段落文字的颜色",
                Description = "设置指定段落的文字颜色",
                Category = WordOperationCategory.ParagraphTextStyle
            },

            // （二）段落格式设置（文字位置与布局）
            new() {
                Id = 6,
                OperationNumber = 6,
                Name = "设置段落对齐方式",
                Description = "设置指定段落的对齐方式",
                Category = WordOperationCategory.ParagraphFormat
            },
            new() {
                Id = 7,
                OperationNumber = 7,
                Name = "设置段落缩进",
                Description = "设置指定段落的左右缩进和首行缩进",
                Category = WordOperationCategory.ParagraphFormat
            },
            new() {
                Id = 8,
                OperationNumber = 8,
                Name = "设置行间距",
                Description = "设置指定段落的行间距",
                Category = WordOperationCategory.ParagraphFormat
            },
            new() {
                Id = 9,
                OperationNumber = 9,
                Name = "首字下沉",
                Description = "设置指定段落的首字下沉效果",
                Category = WordOperationCategory.ParagraphFormat
            },

            // （三）段落间距与边框
            new() {
                Id = 10,
                OperationNumber = 10,
                Name = "设置段落间距",
                Description = "设置指定段落的段前段后间距",
                Category = WordOperationCategory.ParagraphSpacingBorder
            },
            new() {
                Id = 11,
                OperationNumber = 11,
                Name = "设置段落边框颜色",
                Description = "设置指定段落的边框颜色",
                Category = WordOperationCategory.ParagraphSpacingBorder
            },
            new() {
                Id = 12,
                OperationNumber = 12,
                Name = "设置段落边框线型",
                Description = "设置指定段落的边框线条样式",
                Category = WordOperationCategory.ParagraphSpacingBorder
            },
            new() {
                Id = 13,
                OperationNumber = 13,
                Name = "设置段落边框线宽",
                Description = "设置指定段落的边框线条宽度",
                Category = WordOperationCategory.ParagraphSpacingBorder
            },

            // （四）段落背景设置
            new() {
                Id = 14,
                OperationNumber = 14,
                Name = "设置段落底纹",
                Description = "设置指定段落的背景底纹",
                Category = WordOperationCategory.ParagraphBackground
            }
        };
    }

    /// <summary>
    /// 获取所有Word操作参数定义（共67个操作点的参数）
    /// </summary>
    /// <returns></returns>
    public static List<WordOperationParameter> GetWordOperationParameters()
    {
        return WordOperationParametersComplete.GetWordOperationParameters();
    }

    /// <summary>
    /// 获取原始的段落操作参数定义
    /// </summary>
    /// <returns></returns>
    public static List<WordOperationParameter> GetParagraphOperationParameters()
    {
        return new List<WordOperationParameter>
        {
            // 通用参数：段落序号（所有操作都需要）
            new() { Id = 1, OperationPointId = 1, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 2, OperationPointId = 2, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 3, OperationPointId = 3, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 4, OperationPointId = 4, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 5, OperationPointId = 5, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 6, OperationPointId = 6, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 7, OperationPointId = 7, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 8, OperationPointId = 8, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 9, OperationPointId = 9, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 10, OperationPointId = 10, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 11, OperationPointId = 11, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 12, OperationPointId = 12, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 13, OperationPointId = 13, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new() { Id = 14, OperationPointId = 14, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },

            // 知识点1：设置段落的字体
            new() { Id = 15, OperationPointId = 1, ParameterKey = "FontFamily", ParameterName = "字体", DataType = WordParameterDataType.Enum, EnumTypeId = 1, IsRequired = true, DefaultValue = "宋体", ParameterOrder = 2, Description = "选择字体类型" },

            // 知识点2：设置段落的字号
            new() { Id = 16, OperationPointId = 2, ParameterKey = "FontSize", ParameterName = "字号", DataType = WordParameterDataType.Enum, EnumTypeId = 3, IsRequired = true, DefaultValue = "12", ParameterOrder = 2, Description = "选择字体大小（磅）" },

            // 知识点3：设置段落的字形
            new() { Id = 17, OperationPointId = 3, ParameterKey = "FontStyle", ParameterName = "字形", DataType = WordParameterDataType.Enum, EnumTypeId = 2, IsRequired = true, DefaultValue = "Regular", ParameterOrder = 2, Description = "选择字体样式" },

            // 知识点4：设置段落字间距
            new() { Id = 18, OperationPointId = 4, ParameterKey = "CharacterSpacing", ParameterName = "字间距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "0", ParameterOrder = 2, Description = "设置字符间距（磅，正值为加宽，负值为紧缩）" },

            // 知识点5：设置段落文字的颜色
            new() { Id = 19, OperationPointId = 5, ParameterKey = "FontColor", ParameterName = "文字颜色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 2, Description = "设置文字颜色（十六进制格式）" },

            // 知识点6：设置段落对齐方式
            new() { Id = 20, OperationPointId = 6, ParameterKey = "ParagraphAlignment", ParameterName = "对齐方式", DataType = WordParameterDataType.Enum, EnumTypeId = 4, IsRequired = true, DefaultValue = "Left", ParameterOrder = 2, Description = "选择段落对齐方式" },

            // 知识点7：设置段落缩进
            new() { Id = 21, OperationPointId = 7, ParameterKey = "LeftIndent", ParameterName = "左缩进", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "0", ParameterOrder = 2, Description = "设置左缩进距离（厘米）" },
            new() { Id = 22, OperationPointId = 7, ParameterKey = "RightIndent", ParameterName = "右缩进", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "0", ParameterOrder = 3, Description = "设置右缩进距离（厘米）" },
            new() { Id = 23, OperationPointId = 7, ParameterKey = "FirstLineIndent", ParameterName = "首行缩进", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "0", ParameterOrder = 4, Description = "设置首行缩进距离（厘米，正值为首行缩进，负值为悬挂缩进）" },

            // 知识点8：设置行间距
            new() { Id = 24, OperationPointId = 8, ParameterKey = "LineSpacingType", ParameterName = "行距类型", DataType = WordParameterDataType.Enum, EnumTypeId = 5, IsRequired = true, DefaultValue = "Single", ParameterOrder = 2, Description = "选择行距类型" },
            new() { Id = 25, OperationPointId = 8, ParameterKey = "LineSpacingValue", ParameterName = "行距值", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "1", ParameterOrder = 3, Description = "设置行距值（当类型为多倍、最小值或固定值时使用）" },

            // 知识点9：首字下沉
            new() { Id = 26, OperationPointId = 9, ParameterKey = "DropCapEnabled", ParameterName = "启用首字下沉", DataType = WordParameterDataType.Boolean, IsRequired = true, DefaultValue = "false", ParameterOrder = 2, Description = "是否启用首字下沉效果" },
            new() { Id = 27, OperationPointId = 9, ParameterKey = "DropCapLines", ParameterName = "下沉行数", DataType = WordParameterDataType.Integer, IsRequired = false, DefaultValue = "3", ParameterOrder = 3, Description = "首字下沉的行数" },
            new() { Id = 28, OperationPointId = 9, ParameterKey = "DropCapDistance", ParameterName = "距正文距离", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "0", ParameterOrder = 4, Description = "首字下沉与正文的距离（厘米）" },

            // 知识点10：设置段落间距
            new() { Id = 29, OperationPointId = 10, ParameterKey = "SpaceBefore", ParameterName = "段前间距", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "0", ParameterOrder = 2, Description = "设置段前间距（磅）" },
            new() { Id = 30, OperationPointId = 10, ParameterKey = "SpaceAfter", ParameterName = "段后间距", DataType = WordParameterDataType.Decimal, IsRequired = false, DefaultValue = "0", ParameterOrder = 3, Description = "设置段后间距（磅）" },

            // 知识点11：设置段落边框颜色
            new() { Id = 31, OperationPointId = 11, ParameterKey = "BorderColor", ParameterName = "边框颜色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 2, Description = "设置边框颜色（十六进制格式）" },

            // 知识点12：设置段落边框线型
            new() { Id = 32, OperationPointId = 12, ParameterKey = "BorderStyle", ParameterName = "边框线型", DataType = WordParameterDataType.Enum, EnumTypeId = 6, IsRequired = true, DefaultValue = "Solid", ParameterOrder = 2, Description = "选择边框线条样式" },

            // 知识点13：设置段落边框线宽
            new() { Id = 33, OperationPointId = 13, ParameterKey = "BorderWidth", ParameterName = "边框线宽", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "0.5", ParameterOrder = 2, Description = "设置边框线条宽度（磅）" },

            // 知识点14：设置段落底纹
            new() { Id = 34, OperationPointId = 14, ParameterKey = "ShadingColor", ParameterName = "底纹颜色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#FFFFFF", ParameterOrder = 2, Description = "设置底纹填充颜色（十六进制格式）" },
            new() { Id = 35, OperationPointId = 14, ParameterKey = "ShadingPattern", ParameterName = "底纹图案", DataType = WordParameterDataType.Enum, EnumTypeId = 7, IsRequired = false, DefaultValue = "Solid", ParameterOrder = 3, Description = "选择底纹图案样式" }
        };
    }
}
