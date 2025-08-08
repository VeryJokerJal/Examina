using ExaminaWebApplication.Models.Word;

namespace ExaminaWebApplication.Data.Word;

/// <summary>
/// Word完整操作参数数据定义类 - 包含所有67个操作点的参数配置
/// </summary>
public static class WordOperationParametersComplete
{
    /// <summary>
    /// 获取所有Word操作参数定义（共67个操作点的参数）
    /// </summary>
    /// <returns></returns>
    public static List<WordOperationParameter> GetWordOperationParameters()
    {
        List<WordOperationParameter> parameters = new List<WordOperationParameter>();
        int currentId = 1;

        // 操作点1：设置段落的字体
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 1, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 1, ParameterKey = "FontFamily", ParameterName = "字体类型", DataType = WordParameterDataType.Enum, EnumTypeId = 1, IsRequired = true, DefaultValue = "楷体", ParameterOrder = 2, Description = "设置段落的字体类型" }
        });

        // 操作点2：设置段落的字号
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 2, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 2, ParameterKey = "FontSize", ParameterName = "具体字号值", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "23", ParameterOrder = 2, Description = "设置段落的字号大小" }
        });

        // 操作点3：设置段落的字形
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 3, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 3, ParameterKey = "FontStyle", ParameterName = "字形", DataType = WordParameterDataType.Enum, EnumTypeId = 2, IsRequired = true, DefaultValue = "常规", ParameterOrder = 2, Description = "设置段落的字体样式" }
        });

        // 操作点4：设置段落字间距
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 4, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 4, ParameterKey = "CharacterSpacing", ParameterName = "字间距值", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "0", ParameterOrder = 2, Description = "字间距值，以磅为单位" }
        });

        // 操作点5：设置段落文字的颜色
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 5, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 5, ParameterKey = "FontColor", ParameterName = "颜色值", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 2, Description = "配置RGB颜色值" }
        });

        // 操作点6：设置段落对齐方式
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 6, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 6, ParameterKey = "Alignment", ParameterName = "对齐方式", DataType = WordParameterDataType.Enum, EnumTypeId = 4, IsRequired = true, DefaultValue = "左对齐", ParameterOrder = 2, Description = "设置段落的对齐方式" }
        });

        // 操作点7：设置段落缩进
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 7, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 7, ParameterKey = "FirstLineIndent", ParameterName = "首行缩进字符数", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "2", ParameterOrder = 2, Description = "首行缩进的字符数" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 7, ParameterKey = "LeftIndent", ParameterName = "左缩进字符数", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "0", ParameterOrder = 3, Description = "左缩进的字符数" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 7, ParameterKey = "RightIndent", ParameterName = "右缩进字符数", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "0", ParameterOrder = 4, Description = "右缩进的字符数" }
        });

        // 操作点8：设置行间距
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 8, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 8, ParameterKey = "LineSpacing", ParameterName = "行间距值", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "1.0", ParameterOrder = 2, Description = "行间距值" }
        });

        // 操作点9：首字下沉
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 9, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 9, ParameterKey = "DropCapType", ParameterName = "首字下沉形式", DataType = WordParameterDataType.Enum, EnumTypeId = 6, IsRequired = true, DefaultValue = "不使用下沉", ParameterOrder = 2, Description = "首字下沉的形式" }
        });

        // 操作点10：设置段落间距
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 10, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 10, ParameterKey = "SpaceBefore", ParameterName = "段前间距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "0", ParameterOrder = 2, Description = "段前间距值" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 10, ParameterKey = "SpaceAfter", ParameterName = "段后间距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "0", ParameterOrder = 3, Description = "段后间距值" }
        });

        // 操作点11：设置段落边框的颜色
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 11, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 11, ParameterKey = "BorderColor", ParameterName = "边框颜色值", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 2, Description = "边框的RGB颜色值" }
        });

        // 操作点12：设置段落边框的线型
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 12, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 12, ParameterKey = "BorderStyle", ParameterName = "边框线型", DataType = WordParameterDataType.Enum, EnumTypeId = 7, IsRequired = true, DefaultValue = "无边框", ParameterOrder = 2, Description = "边框线条的样式" }
        });

        // 操作点13：设置段落边框的线宽
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 13, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 13, ParameterKey = "BorderWidth", ParameterName = "段落边框值", DataType = WordParameterDataType.Enum, EnumTypeId = 8, IsRequired = true, DefaultValue = "1.0pt", ParameterOrder = 2, Description = "边框线条的宽度，pt为单位" }
        });

        // 操作点14：设置段落底纹
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 14, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 14, ParameterKey = "ShadingPattern", ParameterName = "图案样式", DataType = WordParameterDataType.Enum, EnumTypeId = 9, IsRequired = true, DefaultValue = "无填充", ParameterOrder = 2, Description = "底纹的图案样式" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 14, ParameterKey = "ForegroundColor", ParameterName = "前景色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 3, Description = "底纹前景色RGB值" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 14, ParameterKey = "BackgroundColor", ParameterName = "背景色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#FFFFFF", ParameterOrder = 4, Description = "底纹背景色RGB值" }
        });

        // 操作点15：设置纸张大小
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 15, ParameterKey = "PaperSize", ParameterName = "纸张类型", DataType = WordParameterDataType.Enum, EnumTypeId = 10, IsRequired = true, DefaultValue = "A4纸", ParameterOrder = 1, Description = "设置文档的纸张类型" }
        });

        // 操作点16：设置页边距
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 16, ParameterKey = "TopMargin", ParameterName = "上边距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "50", ParameterOrder = 1, Description = "上边距数值，单位：磅pt" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 16, ParameterKey = "BottomMargin", ParameterName = "下边距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "50", ParameterOrder = 2, Description = "下边距数值，单位：磅pt" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 16, ParameterKey = "LeftMargin", ParameterName = "左边距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "50", ParameterOrder = 3, Description = "左边距数值，单位：磅pt" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 16, ParameterKey = "RightMargin", ParameterName = "右边距", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "50", ParameterOrder = 4, Description = "右边距数值，单位：磅pt" }
        });

        // 操作点17：设置页眉中的文字
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 17, ParameterKey = "HeaderText", ParameterName = "页眉文字内容", DataType = WordParameterDataType.String, IsRequired = true, DefaultValue = "", ParameterOrder = 1, Description = "页眉的文字内容" }
        });

        // 操作点18：页眉中文字的字体
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 18, ParameterKey = "HeaderFontFamily", ParameterName = "页眉字体名称", DataType = WordParameterDataType.Enum, EnumTypeId = 1, IsRequired = true, DefaultValue = "宋体", ParameterOrder = 1, Description = "页眉文字的字体类型" }
        });

        // 操作点19：页眉中文字的字号
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 19, ParameterKey = "HeaderFontSize", ParameterName = "字号数值", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "16", ParameterOrder = 1, Description = "页眉文字的字号大小" }
        });

        // 操作点20：页眉中文字的对齐方式
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 20, ParameterKey = "HeaderAlignment", ParameterName = "对齐方式", DataType = WordParameterDataType.Enum, EnumTypeId = 11, IsRequired = true, DefaultValue = "左对齐", ParameterOrder = 1, Description = "页眉文字的对齐方式" }
        });

        // 操作点21：设置页脚中的文字
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 21, ParameterKey = "FooterText", ParameterName = "文字内容", DataType = WordParameterDataType.String, IsRequired = true, DefaultValue = "", ParameterOrder = 1, Description = "页脚的文字内容" }
        });

        // 操作点22：页脚中文字的字体
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 22, ParameterKey = "FooterFontFamily", ParameterName = "字体名称", DataType = WordParameterDataType.Enum, EnumTypeId = 1, IsRequired = true, DefaultValue = "宋体", ParameterOrder = 1, Description = "页脚文字的字体类型" }
        });

        // 操作点23：页脚中文字的字号
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 23, ParameterKey = "FooterFontSize", ParameterName = "字号数值", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "16", ParameterOrder = 1, Description = "页脚文字的字号大小" }
        });

        // 操作点24：页脚中文字的对齐方式
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 24, ParameterKey = "FooterAlignment", ParameterName = "对齐方式", DataType = WordParameterDataType.Enum, EnumTypeId = 11, IsRequired = true, DefaultValue = "左对齐", ParameterOrder = 1, Description = "页脚文字的对齐方式" }
        });

        // 操作点25：设置页码
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 25, ParameterKey = "EnablePageNumber", ParameterName = "启用页码", DataType = WordParameterDataType.Boolean, IsRequired = true, DefaultValue = "true", ParameterOrder = 1, Description = "是否启用页码设置" }
        });

        // 操作点26：设置页面背景
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 26, ParameterKey = "BackgroundColor", ParameterName = "背景颜色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#FFFFFF", ParameterOrder = 1, Description = "页面背景颜色RGB值" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 26, ParameterKey = "BackgroundTexture", ParameterName = "背景纹理", DataType = WordParameterDataType.String, IsRequired = false, DefaultValue = "", ParameterOrder = 2, Description = "页面背景纹理，如羊皮纸、新闻纸等" }
        });

        // 操作点27：页面边框的颜色
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 27, ParameterKey = "PageBorderColor", ParameterName = "页面颜色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 1, Description = "页面边框颜色RGB值" }
        });

        // 操作点28：页面边框的线型
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 28, ParameterKey = "PageBorderStyle", ParameterName = "边框线型", DataType = WordParameterDataType.Enum, EnumTypeId = 7, IsRequired = true, DefaultValue = "无边框", ParameterOrder = 1, Description = "页面边框的线条样式" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 28, ParameterKey = "PageBorderType", ParameterName = "页面边框类型", DataType = WordParameterDataType.Enum, EnumTypeId = 12, IsRequired = true, DefaultValue = "无边框", ParameterOrder = 2, Description = "页面边框的类型" }
        });

        // 操作点29：页面边框的线宽
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 29, ParameterKey = "PageBorderWidth", ParameterName = "线宽", DataType = WordParameterDataType.Enum, EnumTypeId = 8, IsRequired = true, DefaultValue = "1.0pt", ParameterOrder = 1, Description = "页面边框的线条宽度" }
        });

        // 操作点30：设置水印
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 30, ParameterKey = "WatermarkType", ParameterName = "水印类型", DataType = WordParameterDataType.Enum, EnumTypeId = 13, IsRequired = true, DefaultValue = "文字水印", ParameterOrder = 1, Description = "水印的类型" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 30, ParameterKey = "WatermarkText", ParameterName = "水印文字", DataType = WordParameterDataType.String, IsRequired = false, DefaultValue = "机密", ParameterOrder = 2, Description = "文字水印的内容" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 30, ParameterKey = "WatermarkLayout", ParameterName = "水印版式", DataType = WordParameterDataType.Enum, EnumTypeId = 14, IsRequired = true, DefaultValue = "对角", ParameterOrder = 3, Description = "水印的布局方式" }
        });

        // 操作点31：设置项目符号
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 31, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 31, ParameterKey = "BulletType", ParameterName = "项目符号类型", DataType = WordParameterDataType.Enum, EnumTypeId = 15, IsRequired = true, DefaultValue = "实心圆点", ParameterOrder = 2, Description = "项目符号的样式" }
        });

        // 操作点32：设置编号
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 32, ParameterKey = "ParagraphIndex", ParameterName = "段落序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的段落序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 32, ParameterKey = "NumberingType", ParameterName = "编号类型", DataType = WordParameterDataType.Enum, EnumTypeId = 16, IsRequired = true, DefaultValue = "1,2,3", ParameterOrder = 2, Description = "编号的样式" }
        });

        // 操作点33：插入表格
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 33, ParameterKey = "Rows", ParameterName = "行数", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "3", ParameterOrder = 1, Description = "表格的行数" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 33, ParameterKey = "Columns", ParameterName = "列数", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "3", ParameterOrder = 2, Description = "表格的列数" }
        });

        // 操作点34：设置表格边框
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 34, ParameterKey = "TableIndex", ParameterName = "表格序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的表格序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 34, ParameterKey = "BorderType", ParameterName = "边框类型", DataType = WordParameterDataType.Enum, EnumTypeId = 17, IsRequired = true, DefaultValue = "所有边框", ParameterOrder = 2, Description = "表格边框的类型" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 34, ParameterKey = "BorderColor", ParameterName = "边框颜色", DataType = WordParameterDataType.Color, IsRequired = true, DefaultValue = "#000000", ParameterOrder = 3, Description = "表格边框的颜色" }
        });

        // 操作点35：设置单元格对齐
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 35, ParameterKey = "TableIndex", ParameterName = "表格序号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 1, Description = "指定要操作的表格序号（从1开始）" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 35, ParameterKey = "CellRow", ParameterName = "单元格行号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 2, Description = "单元格的行号" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 35, ParameterKey = "CellColumn", ParameterName = "单元格列号", DataType = WordParameterDataType.Integer, IsRequired = true, DefaultValue = "1", ParameterOrder = 3, Description = "单元格的列号" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 35, ParameterKey = "CellAlignment", ParameterName = "对齐方式", DataType = WordParameterDataType.Enum, EnumTypeId = 18, IsRequired = true, DefaultValue = "居中", ParameterOrder = 4, Description = "单元格内容的对齐方式" }
        });

        // 操作点36：插入自选图形
        parameters.AddRange(new[]
        {
            new WordOperationParameter { Id = currentId++, OperationPointId = 36, ParameterKey = "ShapeType", ParameterName = "图形类型", DataType = WordParameterDataType.Enum, EnumTypeId = 19, IsRequired = true, DefaultValue = "矩形", ParameterOrder = 1, Description = "自选图形的类型" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 36, ParameterKey = "Width", ParameterName = "宽度", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "100", ParameterOrder = 2, Description = "图形的宽度，单位：磅pt" },
            new WordOperationParameter { Id = currentId++, OperationPointId = 36, ParameterKey = "Height", ParameterName = "高度", DataType = WordParameterDataType.Decimal, IsRequired = true, DefaultValue = "100", ParameterOrder = 3, Description = "图形的高度，单位：磅pt" }
        });

        return parameters;
    }
}
