using BenchSuite.Models;
using System.Collections.Generic;

namespace BenchSuite.Services;

/// <summary>
/// Word知识点服务 - 提供Word操作点的参数模板和默认值配置
/// </summary>
public class WordKnowledgeService
{
    private static readonly Lazy<WordKnowledgeService> _instance = new(() => new WordKnowledgeService());
    public static WordKnowledgeService Instance => _instance.Value;

    private readonly Dictionary<string, WordOperationConfig> _operationConfigs;

    private WordKnowledgeService()
    {
        _operationConfigs = InitializeOperationConfigs();
    }

    /// <summary>
    /// 获取操作点配置
    /// </summary>
    public WordOperationConfig? GetOperationConfig(string operationName)
    {
        return _operationConfigs.TryGetValue(operationName, out WordOperationConfig? config) ? config : null;
    }

    /// <summary>
    /// 获取所有操作点配置
    /// </summary>
    public Dictionary<string, WordOperationConfig> GetAllOperationConfigs()
    {
        return new Dictionary<string, WordOperationConfig>(_operationConfigs);
    }

    /// <summary>
    /// 创建带有默认值的操作点
    /// </summary>
    public OperationPointModel CreateOperationPoint(string operationName)
    {
        WordOperationConfig? config = GetOperationConfig(operationName);
        if (config == null)
        {
            throw new ArgumentException($"未找到操作点 {operationName} 的配置");
        }

        OperationPointModel operationPoint = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = operationName,
            Description = config.Description,
            ModuleType = ModuleType.Word,
            WordKnowledgeType = operationName,
            Score = 5.0,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        // 根据参数模板创建配置参数
        foreach (ParameterTemplate template in config.ParameterTemplates)
        {
            ConfigurationParameterModel parameter = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = template.Name,
                DisplayName = template.DisplayName,
                Value = template.DefaultValue ?? "",
                Type = template.Type,
                IsRequired = template.IsRequired,
                DefaultValue = template.DefaultValue
            };

            operationPoint.Parameters.Add(parameter);
        }

        return operationPoint;
    }

    private Dictionary<string, WordOperationConfig> InitializeOperationConfigs()
    {
        Dictionary<string, WordOperationConfig> configs = [];

        // 第一类：段落操作
        InitializeParagraphOperations(configs);

        // 第二类：页面设置
        InitializePageSettings(configs);

        // 第三类：水印设置
        InitializeWatermarkSettings(configs);

        // 第四类：项目符号与编号
        InitializeBulletNumbering(configs);

        // 第五类：表格操作
        InitializeTableOperations(configs);

        // 第六类：图形和图片设置
        InitializeGraphicsAndImages(configs);

        // 第七类：文本框设置
        InitializeTextBoxSettings(configs);

        // 第八类：其他操作
        InitializeOtherOperations(configs);

        return configs;
    }

    private void InitializeParagraphOperations(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点1：设置段落的字体
        configs["SetParagraphFont"] = new WordOperationConfig
        {
            Name = "设置段落的字体",
            Description = "设置指定段落的字体类型",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "FontFamily", DisplayName = "字体类型", Description = "选择字体", Type = ParameterType.Enum, IsRequired = true, Order = 2, DefaultValue = "宋体",
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" }
            ]
        };

        // 知识点2：设置段落字号
        configs["SetParagraphFontSize"] = new WordOperationConfig
        {
            Name = "设置段落字号",
            Description = "设置指定段落的字体大小",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "FontSize", DisplayName = "字号值", Description = "字体大小", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "12", MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点3：设置段落字形
        configs["SetParagraphFontStyle"] = new WordOperationConfig
        {
            Name = "设置段落字形",
            Description = "设置指定段落的字体样式",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "FontStyle", DisplayName = "字形", Description = "选择字体样式", Type = ParameterType.Enum, IsRequired = true, Order = 2, DefaultValue = "常规",
                    EnumOptions = "常规,加粗,斜体,加粗+斜体,下划线,删除线" }
            ]
        };

        // 知识点4：设置段落文字颜色
        configs["SetParagraphTextColor"] = new WordOperationConfig
        {
            Name = "设置段落文字颜色",
            Description = "设置指定段落的文字颜色",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "TextColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 2, DefaultValue = "#000000" }
            ]
        };

        // 知识点5：设置段落字间距
        configs["SetParagraphCharacterSpacing"] = new WordOperationConfig
        {
            Name = "设置段落字间距",
            Description = "设置指定段落的字符间距",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "CharacterSpacing", DisplayName = "字间距值", Description = "字符间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "0", MinValue = -10, MaxValue = 50 }
            ]
        };

        // 知识点6：设置段落对齐方式
        configs["SetParagraphAlignment"] = new WordOperationConfig
        {
            Name = "设置段落对齐方式",
            Description = "设置指定段落的对齐方式",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "Alignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 2, DefaultValue = "左对齐",
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐" }
            ]
        };

        // 知识点7：设置段落缩进
        configs["SetParagraphIndentation"] = new WordOperationConfig
        {
            Name = "设置段落缩进",
            Description = "设置指定段落的缩进",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "FirstLineIndent", DisplayName = "首行缩进字符数", Description = "首行缩进值", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "2", MinValue = 0, MaxValue = 10 },
                new() { Name = "LeftIndent", DisplayName = "左缩进字符数", Description = "左缩进值", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "0", MinValue = 0, MaxValue = 10 },
                new() { Name = "RightIndent", DisplayName = "右缩进字符数", Description = "右缩进值", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "0", MinValue = 0, MaxValue = 10 }
            ]
        };

        // 知识点8：设置段落行间距
        configs["SetParagraphLineSpacing"] = new WordOperationConfig
        {
            Name = "设置段落行间距",
            Description = "设置指定段落的行间距",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "LineSpacing", DisplayName = "行间距值", Description = "行间距", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1.5", MinValue = 0.5, MaxValue = 5.0 }
            ]
        };

        // 知识点9：设置段落间距
        configs["SetParagraphSpacing"] = new WordOperationConfig
        {
            Name = "设置段落间距",
            Description = "设置指定段落的段前段后间距",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "SpaceBefore", DisplayName = "段前间距", Description = "段前间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "0", MinValue = 0, MaxValue = 100 },
                new() { Name = "SpaceAfter", DisplayName = "段后间距", Description = "段后间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "6", MinValue = 0, MaxValue = 100 }
            ]
        };

        // 知识点10：设置段落边框颜色
        configs["SetParagraphBorderColor"] = new WordOperationConfig
        {
            Name = "设置段落边框颜色",
            Description = "设置指定段落的边框颜色",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "边框颜色", Type = ParameterType.Enum, IsRequired = true, Order = 2, DefaultValue = "黑色",
                    EnumOptions = "黑色,红色,蓝色,绿色,黄色,紫色,橙色,灰色,自动" }
            ]
        };

        // 知识点11：设置段落底纹
        configs["SetParagraphShading"] = new WordOperationConfig
        {
            Name = "设置段落底纹",
            Description = "设置指定段落的底纹",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "-1" },
                new() { Name = "ShadingColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 5, DefaultValue = "#FFFF00" }
            ]
        };
    }

    private void InitializePageSettings(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点15：设置纸张大小
        configs["SetPaperSize"] = new WordOperationConfig
        {
            Name = "设置纸张大小",
            Description = "设置文档的纸张尺寸",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "PaperSize", DisplayName = "纸张类型", Description = "选择纸张尺寸", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "A4纸",
                    EnumOptions = "A4纸,A3纸,B5纸,法律纸尺寸" }
            ]
        };

        // 知识点16：设置页边距
        configs["SetPageMargins"] = new WordOperationConfig
        {
            Name = "设置页边距",
            Description = "设置文档的页边距",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "TopMargin", DisplayName = "上边距", Description = "上边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "72", MinValue = 0, MaxValue = 200 },
                new() { Name = "BottomMargin", DisplayName = "下边距", Description = "下边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "72", MinValue = 0, MaxValue = 200 },
                new() { Name = "LeftMargin", DisplayName = "左边距", Description = "左边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "90", MinValue = 0, MaxValue = 200 },
                new() { Name = "RightMargin", DisplayName = "右边距", Description = "右边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "90", MinValue = 0, MaxValue = 200 }
            ]
        };

        // 知识点17：设置页眉
        configs["SetHeader"] = new WordOperationConfig
        {
            Name = "设置页眉",
            Description = "设置文档的页眉内容和格式",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "HeaderText", DisplayName = "页眉文字内容", Description = "页眉中显示的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "页眉内容" },
                new() { Name = "HeaderAlignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "居中对齐",
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐" },
                new() { Name = "HeaderFont", DisplayName = "页眉字体名称", Description = "选择页眉字体", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "宋体",
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" },
                new() { Name = "HeaderFontSize", DisplayName = "字号数值", Description = "页眉字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "10", MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点18：设置页脚
        configs["SetFooter"] = new WordOperationConfig
        {
            Name = "设置页脚",
            Description = "设置文档的页脚内容和格式",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "FooterText", DisplayName = "页脚文字", Description = "页脚文字内容", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "页脚内容" },
                new() { Name = "FooterAlignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "居中对齐",
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐" },
                new() { Name = "FooterFont", DisplayName = "字体类型", Description = "页脚字体", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "宋体",
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" },
                new() { Name = "FooterFontSize", DisplayName = "字号数值", Description = "页脚字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "10", MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点19：设置页码
        configs["SetPageNumber"] = new WordOperationConfig
        {
            Name = "设置页码",
            Description = "设置文档的页码格式和位置",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "PageNumberPosition", DisplayName = "页码位置", Description = "页码显示位置", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "页面底端居中",
                    EnumOptions = "页面顶端居中,页面顶端左侧,页面顶端右侧,页面底端居中,页面底端左侧,页面底端右侧" },
                new() { Name = "PageNumberFormat", DisplayName = "页码格式", Description = "页码数字格式", Type = ParameterType.Enum, IsRequired = true, Order = 2, DefaultValue = "1,2,3...",
                    EnumOptions = "1,2,3...,a,b,c...,A,B,C...,i,ii,iii...,I,II,III..." }
            ]
        };

        // 知识点20：设置页面背景颜色
        configs["SetPageBackgroundColor"] = new WordOperationConfig
        {
            Name = "设置页面背景颜色",
            Description = "设置文档的页面背景颜色",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "BackgroundColor", DisplayName = "背景颜色", Description = "页面背景颜色", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "无填充",
                    EnumOptions = "无填充,白色,浅灰色,灰色,深灰色,黑色,红色,蓝色,绿色,黄色,紫色,橙色" }
            ]
        };

        // 知识点21：设置页面边框
        configs["SetPageBorder"] = new WordOperationConfig
        {
            Name = "设置页面边框",
            Description = "设置文档的页面边框",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "页面边框颜色", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "黑色",
                    EnumOptions = "黑色,红色,蓝色,绿色,黄色,紫色,橙色,灰色,自动" },
                new() { Name = "BorderStyle", DisplayName = "边框样式", Description = "页面边框线型", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "单实线",
                    EnumOptions = "无,单实线,双线,点线,虚线,粗线,细线,波浪线,艺术型边框" },
                new() { Name = "BorderWidth", DisplayName = "边框宽度", Description = "页面边框线宽", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "1磅",
                    EnumOptions = "0.25磅,0.5磅,0.75磅,1磅,1.5磅,2.25磅,3磅,4.5磅,6磅" }
            ]
        };
    }

    private void InitializeWatermarkSettings(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点30：设置水印文字
        configs["SetWatermarkText"] = new WordOperationConfig
        {
            Name = "设置水印文字",
            Description = "设置文档的水印文字内容",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkText", DisplayName = "水印文字", Description = "水印显示的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "机密" }
            ]
        };

        // 知识点31：设置水印字体
        configs["SetWatermarkFont"] = new WordOperationConfig
        {
            Name = "设置水印字体",
            Description = "设置水印的字体样式",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkFont", DisplayName = "字体", Description = "选择水印字体", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "宋体",
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" }
            ]
        };

        // 知识点32：设置水印字号
        configs["SetWatermarkFontSize"] = new WordOperationConfig
        {
            Name = "设置水印字号",
            Description = "设置水印的字体大小",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkFontSize", DisplayName = "字号值", Description = "水印字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "36", MinValue = 8, MaxValue = 144 }
            ]
        };

        // 知识点33：设置水印倾斜角度
        configs["SetWatermarkAngle"] = new WordOperationConfig
        {
            Name = "设置水印倾斜角度",
            Description = "设置水印文字的倾斜角度",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkAngle", DisplayName = "水印文字倾斜角度", Description = "倾斜角度（度）", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "45", MinValue = -90, MaxValue = 90 }
            ]
        };
    }

    private void InitializeBulletNumbering(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点34：设置项目编号
        configs["SetBulletNumbering"] = new WordOperationConfig
        {
            Name = "设置项目编号",
            Description = "为指定段落设置项目符号或编号",
            Category = "项目符号与编号",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumbers", DisplayName = "段落编号ID", Description = "用#分隔的段落编号，如：11#12#13", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "1#2#3" },
                new() { Name = "NumberingType", DisplayName = "项目编号类型", Description = "选择编号类型", Type = ParameterType.Enum, IsRequired = true, Order = 2, DefaultValue = "数字编号" }
            ]
        };
    }

    private void InitializeTableOperations(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点35：设置表格的行数和列数
        configs["SetTableRowsColumns"] = new WordOperationConfig
        {
            Name = "设置表格的行数和列数",
            Description = "创建指定行数和列数的表格",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "Rows", DisplayName = "行数", Description = "表格行数", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "3", MinValue = 1, MaxValue = 50 },
                new() { Name = "Columns", DisplayName = "列数", Description = "表格列数", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "3", MinValue = 1, MaxValue = 20 }
            ]
        };

        // 知识点36：设置表格底纹
        configs["SetTableShading"] = new WordOperationConfig
        {
            Name = "设置表格底纹",
            Description = "设置表格指定区域的底纹颜色",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "AreaType", DisplayName = "行和列划分区域", Description = "选择区域类型", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "row",
                    EnumOptions = "row,column" },
                new() { Name = "AreaNumber", DisplayName = "第几行和第几列", Description = "按照参数一来判定", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1", MinValue = 1 },
                new() { Name = "StartPosition", DisplayName = "起始列或起始行", Description = "起始位置", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "1", MinValue = 1 },
                new() { Name = "EndPosition", DisplayName = "终止列或终止行", Description = "终止位置", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "1", MinValue = 1 },
                new() { Name = "ShadingColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 5, DefaultValue = "#FFFF00" }
            ]
        };

        // 知识点37：设置表格行高
        configs["SetTableRowHeight"] = new WordOperationConfig
        {
            Name = "设置表格行高",
            Description = "设置表格指定行的行高",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "StartRow", DisplayName = "起始行", Description = "起始行号", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "1", MinValue = 1 },
                new() { Name = "EndRow", DisplayName = "终止行", Description = "终止行号", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1", MinValue = 1 },
                new() { Name = "RowHeight", DisplayName = "行高", Description = "行高（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "20", MinValue = 10, MaxValue = 200 },
                new() { Name = "HeightType", DisplayName = "行高类型", Description = "选择行高类型", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "自动",
                    EnumOptions = "自动,最小值,固定值" }
            ]
        };

        // 知识点38：设置表格列宽
        configs["SetTableColumnWidth"] = new WordOperationConfig
        {
            Name = "设置表格列宽",
            Description = "设置表格指定列的列宽",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "StartColumn", DisplayName = "起始列", Description = "起始列号", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "1", MinValue = 1 },
                new() { Name = "EndColumn", DisplayName = "终止列", Description = "终止列号", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1", MinValue = 1 },
                new() { Name = "ColumnWidth", DisplayName = "列宽", Description = "列宽（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "100", MinValue = 10, MaxValue = 500 },
                new() { Name = "WidthType", DisplayName = "宽度类型", Description = "选择宽度类型", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "自动",
                    EnumOptions = "自动,磅单位,百分比" }
            ]
        };

        // 知识点39：设置表格单元格内容
        configs["SetTableCellContent"] = new WordOperationConfig
        {
            Name = "设置表格单元格内容",
            Description = "设置表格指定单元格的内容",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "RowNumber", DisplayName = "行号", Description = "单元格所在行", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "1", MinValue = 1 },
                new() { Name = "ColumnNumber", DisplayName = "列号", Description = "单元格所在列", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1", MinValue = 1 },
                new() { Name = "CellContent", DisplayName = "单元格内容", Description = "要设置的文字内容", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "内容" }
            ]
        };

        // 知识点40：设置表格单元格对齐方式
        configs["SetTableCellAlignment"] = new WordOperationConfig
        {
            Name = "设置表格单元格对齐方式",
            Description = "设置表格指定单元格的对齐方式",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "RowNumber", DisplayName = "行号", Description = "单元格所在行", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "1", MinValue = 1 },
                new() { Name = "ColumnNumber", DisplayName = "列号", Description = "单元格所在列", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1", MinValue = 1 },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 3, DefaultValue = "左对齐",
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "居中对齐",
                    EnumOptions = "顶端对齐,居中对齐,底端对齐" }
            ]
        };

        // 知识点41：设置表格对齐方式
        configs["SetTableAlignment"] = new WordOperationConfig
        {
            Name = "设置表格对齐方式",
            Description = "设置表格的对齐方式",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "TableAlignment", DisplayName = "表格对齐", Description = "表格对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "左对齐",
                    EnumOptions = "左对齐,居中对齐,右对齐" },
                new() { Name = "LeftIndent", DisplayName = "左缩进", Description = "表格左缩进（磅）", Type = ParameterType.Number, IsRequired = false, Order = 2, DefaultValue = "0", MinValue = 0, MaxValue = 100 }
            ]
        };

        // 知识点42：合并表格单元格
        configs["MergeTableCells"] = new WordOperationConfig
        {
            Name = "合并表格单元格",
            Description = "合并表格指定区域的单元格",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "StartRow", DisplayName = "起始行", Description = "合并区域起始行", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "1", MinValue = 1 },
                new() { Name = "StartColumn", DisplayName = "起始列", Description = "合并区域起始列", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "1", MinValue = 1 },
                new() { Name = "EndRow", DisplayName = "结束行", Description = "合并区域结束行", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "2", MinValue = 1 },
                new() { Name = "EndColumn", DisplayName = "结束列", Description = "合并区域结束列", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "2", MinValue = 1 }
            ]
        };
    }

    private void InitializeGraphicsAndImages(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点45：插入自选图形类型
        configs["InsertAutoShape"] = new WordOperationConfig
        {
            Name = "插入自选图形类型",
            Description = "插入指定类型的自选图形",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ShapeType", DisplayName = "图形类型", Description = "选择自选图形类型", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "矩形",
                    EnumOptions = "矩形,圆角矩形,椭圆,右箭头,下箭头,左箭头,上箭头,双向箭头,折弯箭头,尖括号,块弧形,爱心形状,笑脸,五角星,16角星,爆炸形状1,爆炸形状2,云形状" }
            ]
        };

        // 知识点46：设置图片大小
        configs["SetImageSize"] = new WordOperationConfig
        {
            Name = "设置图片大小",
            Description = "设置图片的高度和宽度",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ImageHeight", DisplayName = "高度", Description = "图片高度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "200", MinValue = 10, MaxValue = 1000 },
                new() { Name = "ImageWidth", DisplayName = "宽度", Description = "图片宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "200", MinValue = 10, MaxValue = 1000 }
            ]
        };

        // 知识点47：设置自选图形大小
        configs["SetShapeSize"] = new WordOperationConfig
        {
            Name = "设置自选图形大小",
            Description = "设置自选图形的高度和宽度",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ShapeHeight", DisplayName = "高度", Description = "图形高度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "100", MinValue = 10, MaxValue = 1000 },
                new() { Name = "ShapeWidth", DisplayName = "宽度", Description = "图形宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "100", MinValue = 10, MaxValue = 1000 }
            ]
        };

        // 知识点48：设置自选图形线条颜色
        configs["SetShapeLineColor"] = new WordOperationConfig
        {
            Name = "设置自选图形线条颜色",
            Description = "设置自选图形的线条颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "LineColor", DisplayName = "线条颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点49：设置自选图形填充颜色
        configs["SetShapeFillColor"] = new WordOperationConfig
        {
            Name = "设置自选图形填充颜色",
            Description = "设置自选图形的填充颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "FillColor", DisplayName = "填充颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#FFFFFF" }
            ]
        };

        // 知识点50：设置自选图形文字颜色
        configs["SetShapeTextColor"] = new WordOperationConfig
        {
            Name = "设置自选图形文字颜色",
            Description = "设置自选图形中文字的颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "TextColor", DisplayName = "文字颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点51：设置自选图形文字内容
        configs["SetShapeTextContent"] = new WordOperationConfig
        {
            Name = "设置自选图形文字内容",
            Description = "设置自选图形中的文字内容",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "TextContent", DisplayName = "文字内容", Description = "要设置的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "文字内容" }
            ]
        };

        // 知识点52：设置自选图形文字大小
        configs["SetShapeTextSize"] = new WordOperationConfig
        {
            Name = "设置自选图形文字大小",
            Description = "设置自选图形中文字的大小",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "FontSize", DisplayName = "字号", Description = "文字字号", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "12", MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点53：设置图片边框颜色
        configs["SetImageBorderColor"] = new WordOperationConfig
        {
            Name = "设置图片边框颜色",
            Description = "设置图片的边框颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点54：设置图片边框宽度
        configs["SetImageBorderWidth"] = new WordOperationConfig
        {
            Name = "设置图片边框宽度",
            Description = "设置图片的边框宽度",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "BorderWidth", DisplayName = "边框宽度", Description = "边框宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "1", MinValue = 0.25, MaxValue = 6 }
            ]
        };

        // 知识点55：设置图片阴影
        configs["SetImageShadow"] = new WordOperationConfig
        {
            Name = "设置图片阴影",
            Description = "设置图片的阴影效果",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ShadowType", DisplayName = "阴影类型", Description = "阴影样式", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "无阴影",
                    EnumOptions = "无阴影,偏移对角右下,偏移对角右上,偏移对角左下,偏移对角左上,偏移右,偏移下,偏移左,偏移上" },
                new() { Name = "ShadowColor", DisplayName = "阴影颜色", Description = "阴影颜色", Type = ParameterType.Color, IsRequired = false, Order = 2, DefaultValue = "#808080" }
            ]
        };

        // 知识点56：设置图片环绕方式
        configs["SetImageWrapStyle"] = new WordOperationConfig
        {
            Name = "设置图片环绕方式",
            Description = "设置图片的文字环绕方式",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "WrapStyle", DisplayName = "环绕方式", Description = "文字环绕样式", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "嵌入型",
                    EnumOptions = "嵌入型,四周型,紧密型,穿越型,上下型,衬于文字下方,浮于文字上方" }
            ]
        };
    }

    private void InitializeTextBoxSettings(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点61：设置文本框边框颜色
        configs["SetTextBoxBorderColor"] = new WordOperationConfig
        {
            Name = "设置文本框边框颜色",
            Description = "设置文本框的边框颜色",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "BorderColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点62：设置文本框文字内容
        configs["SetTextBoxContent"] = new WordOperationConfig
        {
            Name = "设置文本框文字内容",
            Description = "设置文本框中的文字内容",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "TextContent", DisplayName = "文字值", Description = "文本框中的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "文本框内容" }
            ]
        };

        // 知识点63：设置文本框文字大小
        configs["SetTextBoxTextSize"] = new WordOperationConfig
        {
            Name = "设置文本框文字大小",
            Description = "设置文本框中文字的大小",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "TextSize", DisplayName = "字号值", Description = "文字大小", Type = ParameterType.Number, IsRequired = true, Order = 1, DefaultValue = "12", MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点64：设置文本框环绕方式
        configs["SetTextBoxWrapStyle"] = new WordOperationConfig
        {
            Name = "设置文本框环绕方式",
            Description = "设置文本框的文字环绕方式",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "WrapStyle", DisplayName = "环绕方式", Description = "文字环绕样式", Type = ParameterType.Enum, IsRequired = true, Order = 1, DefaultValue = "嵌入型",
                    EnumOptions = "嵌入型,四周型,紧密型,穿越型,上下型,衬于文字下方,浮于文字上方" }
            ]
        };
    }

    private void InitializeOtherOperations(Dictionary<string, WordOperationConfig> configs)
    {
        // 知识点66：查找与替换
        configs["FindAndReplace"] = new WordOperationConfig
        {
            Name = "查找与替换",
            Description = "在文档中查找并替换指定文本",
            Category = "其他操作",
            ParameterTemplates =
            [
                new() { Name = "FindText", DisplayName = "要查找的文本内容", Description = "需要查找的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "查找内容" },
                new() { Name = "ReplaceText", DisplayName = "替换为的文本内容", Description = "替换后的文字", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "替换内容" },
                new() { Name = "ReplaceCount", DisplayName = "应替换的总数量", Description = "目标替换次数", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "1", MinValue = 1, MaxValue = 100 }
            ]
        };

        // 知识点67：设置指定文字字号
        configs["SetSpecificTextFontSize"] = new WordOperationConfig
        {
            Name = "设置指定文字字号",
            Description = "设置文档中指定文字的字号",
            Category = "其他操作",
            ParameterTemplates =
            [
                new() { Name = "TargetText", DisplayName = "要设置字号的指定文字", Description = "目标文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "目标文字" },
                new() { Name = "FontSize", DisplayName = "应设置的字号大小", Description = "字号大小（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, DefaultValue = "12", MinValue = 8, MaxValue = 72 }
            ]
        };
    }
}
