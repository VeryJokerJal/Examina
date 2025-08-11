using System.Collections.Generic;
using System.Linq;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// Word知识点配置服务
/// </summary>
public class WordKnowledgeService
{
    public static WordKnowledgeService Instance { get; } = new();

    private readonly Dictionary<WordKnowledgeType, WordKnowledgeConfig> _knowledgeConfigs;

    private WordKnowledgeService()
    {
        _knowledgeConfigs = InitializeKnowledgeConfigs();
    }

    /// <summary>
    /// 获取所有知识点配置
    /// </summary>
    public IEnumerable<WordKnowledgeConfig> GetAllKnowledgeConfigs()
    {
        return _knowledgeConfigs.Values;
    }

    /// <summary>
    /// 根据类型获取知识点配置
    /// </summary>
    public WordKnowledgeConfig? GetKnowledgeConfig(WordKnowledgeType type)
    {
        return _knowledgeConfigs.TryGetValue(type, out WordKnowledgeConfig? config) ? config : null;
    }

    /// <summary>
    /// 根据知识点配置创建操作点
    /// </summary>
    public OperationPoint CreateOperationPoint(WordKnowledgeType type)
    {
        WordKnowledgeConfig? config = GetKnowledgeConfig(type);
        if (config == null)
        {
            throw new ArgumentException($"未找到知识点类型 {type} 的配置");
        }

        OperationPoint operationPoint = new()
        {
            Name = config.Name,
            Description = config.Description,
            ModuleType = ModuleType.Word,
            WordKnowledgeType = type
        };

        // 根据参数模板创建配置参数
        foreach (ConfigurationParameterTemplate template in config.ParameterTemplates)
        {
            ConfigurationParameter parameter = new()
            {
                Name = template.Name,
                DisplayName = template.DisplayName,
                Description = template.Description,
                Type = template.Type,
                IsRequired = template.IsRequired,
                Order = template.Order,
                EnumOptions = template.EnumOptions,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                DefaultValue = template.DefaultValue
            };

            operationPoint.Parameters.Add(parameter);
        }

        return operationPoint;
    }

    private Dictionary<WordKnowledgeType, WordKnowledgeConfig> InitializeKnowledgeConfigs()
    {
        Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs = [];

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

    private void InitializeParagraphOperations(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点1：设置段落的字体
        configs[WordKnowledgeType.SetParagraphFont] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphFont,
            Name = "设置段落的字体",
            Description = "设置指定段落的字体类型",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "FontFamily", DisplayName = "字体类型", Description = "选择字体", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" }
            ]
        };

        // 知识点2：设置段落的字号
        configs[WordKnowledgeType.SetParagraphFontSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphFontSize,
            Name = "设置段落的字号",
            Description = "设置指定段落的字体大小",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "FontSize", DisplayName = "字号值", Description = "字体大小", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点3：设置段落的字形
        configs[WordKnowledgeType.SetParagraphFontStyle] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphFontStyle,
            Name = "设置段落的字形",
            Description = "设置指定段落的字体样式",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "FontStyle", DisplayName = "字形", Description = "选择字体样式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "常规,加粗,斜体,加粗+斜体,下划线,删除线" }
            ]
        };

        // 知识点4：设置段落字间距
        configs[WordKnowledgeType.SetParagraphCharacterSpacing] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphCharacterSpacing,
            Name = "设置段落字间距",
            Description = "设置指定段落的字符间距",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "CharacterSpacing", DisplayName = "字间距值", Description = "字符间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = -10, MaxValue = 50 }
            ]
        };

        // 知识点5：设置段落文字的颜色
        configs[WordKnowledgeType.SetParagraphTextColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphTextColor,
            Name = "设置段落文字的颜色",
            Description = "设置指定段落的文字颜色",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Text, IsRequired = true, Order = 2 }
            ]
        };

        // 知识点6：设置段落对齐方式
        configs[WordKnowledgeType.SetParagraphAlignment] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphAlignment,
            Name = "设置段落对齐方式",
            Description = "设置指定段落的对齐方式",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "Alignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐" }
            ]
        };

        // 知识点7：设置段落缩进
        configs[WordKnowledgeType.SetParagraphIndentation] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphIndentation,
            Name = "设置段落缩进",
            Description = "设置指定段落的缩进",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "FirstLineIndent", DisplayName = "首行缩进字符数", Description = "首行缩进值", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0, MaxValue = 10 },
                new() { Name = "LeftIndent", DisplayName = "左缩进字符数", Description = "左缩进值", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0, MaxValue = 10 },
                new() { Name = "RightIndent", DisplayName = "右缩进字符数", Description = "右缩进值", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0, MaxValue = 10 }
            ]
        };

        // 知识点8：设置行间距
        configs[WordKnowledgeType.SetParagraphLineSpacing] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphLineSpacing,
            Name = "设置行间距",
            Description = "设置指定段落的行间距",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "LineSpacing", DisplayName = "行间距值", Description = "行间距", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0.5, MaxValue = 5.0 }
            ]
        };

        // 知识点9：首字下沉
        configs[WordKnowledgeType.SetParagraphDropCap] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphDropCap,
            Name = "首字下沉",
            Description = "设置指定段落的首字下沉",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "DropCapType", DisplayName = "首字下沉形式", Description = "选择下沉形式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "不使用下沉,首字下沉到段落中,首字下沉到页边距" }
            ]
        };

        // 知识点10：设置段落间距
        configs[WordKnowledgeType.SetParagraphSpacing] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphSpacing,
            Name = "设置段落间距",
            Description = "设置指定段落的段前段后间距",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "SpaceBefore", DisplayName = "段前间距", Description = "段前间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0, MaxValue = 100 },
                new() { Name = "SpaceAfter", DisplayName = "段后间距", Description = "段后间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0, MaxValue = 100 }
            ]
        };
    }

    private void InitializePageSettings(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点15：设置纸张大小
        configs[WordKnowledgeType.SetPaperSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPaperSize,
            Name = "设置纸张大小",
            Description = "设置文档的纸张尺寸",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "PaperSize", DisplayName = "纸张类型", Description = "选择纸张尺寸", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "A4纸,A3纸,B5纸,法律纸尺寸" }
            ]
        };

        // 知识点16：设置页边距
        configs[WordKnowledgeType.SetPageMargins] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPageMargins,
            Name = "设置页边距",
            Description = "设置文档的页边距",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "TopMargin", DisplayName = "上边距", Description = "上边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 0, MaxValue = 200 },
                new() { Name = "BottomMargin", DisplayName = "下边距", Description = "下边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0, MaxValue = 200 },
                new() { Name = "LeftMargin", DisplayName = "左边距", Description = "左边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0, MaxValue = 200 },
                new() { Name = "RightMargin", DisplayName = "右边距", Description = "右边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0, MaxValue = 200 }
            ]
        };

        // 知识点17：设置页眉中的文字
        configs[WordKnowledgeType.SetHeaderText] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetHeaderText,
            Name = "设置页眉中的文字",
            Description = "设置页眉的文字内容",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "HeaderText", DisplayName = "页眉文字内容", Description = "页眉中显示的文字", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点18：设置页眉中文字的字体
        configs[WordKnowledgeType.SetHeaderFont] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetHeaderFont,
            Name = "设置页眉中文字的字体",
            Description = "设置页眉文字的字体",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "HeaderFont", DisplayName = "页眉字体名称", Description = "选择页眉字体", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" }
            ]
        };

        // 知识点19：设置页眉中文字的字号
        configs[WordKnowledgeType.SetHeaderFontSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetHeaderFontSize,
            Name = "设置页眉中文字的字号",
            Description = "设置页眉文字的字号",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "HeaderFontSize", DisplayName = "字号数值", Description = "页眉字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点20：设置页眉中文字的对齐方式
        configs[WordKnowledgeType.SetHeaderAlignment] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetHeaderAlignment,
            Name = "设置页眉中文字的对齐方式",
            Description = "设置页眉文字的对齐方式",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "HeaderAlignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐" }
            ]
        };
    }

    private void InitializeWatermarkSettings(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点30：设置水印文字
        configs[WordKnowledgeType.SetWatermarkText] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetWatermarkText,
            Name = "设置水印文字",
            Description = "设置文档的水印文字内容",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkText", DisplayName = "水印文字", Description = "水印显示的文字", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点31：设置水印文字的字体
        configs[WordKnowledgeType.SetWatermarkFont] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetWatermarkFont,
            Name = "设置水印文字的字体",
            Description = "设置水印文字的字体类型",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkFont", DisplayName = "字体", Description = "选择水印字体", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" }
            ]
        };

        // 知识点32：设置水印文字的字号
        configs[WordKnowledgeType.SetWatermarkFontSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetWatermarkFontSize,
            Name = "设置水印文字的字号",
            Description = "设置水印文字的字号大小",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkFontSize", DisplayName = "字号值", Description = "水印字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 144 }
            ]
        };

        // 知识点33：设置水印文字水平或倾斜方式
        configs[WordKnowledgeType.SetWatermarkOrientation] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetWatermarkOrientation,
            Name = "设置水印文字水平或倾斜方式",
            Description = "设置水印文字的倾斜角度",
            Category = "水印设置",
            ParameterTemplates =
            [
                new() { Name = "WatermarkAngle", DisplayName = "水印文字倾斜角度", Description = "倾斜角度（度）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = -90, MaxValue = 90 }
            ]
        };
    }

    private void InitializeBulletNumbering(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点34：设置项目编号
        configs[WordKnowledgeType.SetBulletNumbering] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetBulletNumbering,
            Name = "设置项目编号",
            Description = "为指定段落设置项目符号或编号",
            Category = "项目符号与编号",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumbers", DisplayName = "段落编号ID", Description = "用#分隔的段落编号，如：11#12#13", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "NumberingType", DisplayName = "项目编号类型", Description = "选择编号类型", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "项目符号,数字编号,多级编号,简单数字,小写字母编号,大写字母编号" }
            ]
        };
    }

    private void InitializeTableOperations(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点35：设置表格的行数和列数
        configs[WordKnowledgeType.SetTableRowsColumns] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableRowsColumns,
            Name = "设置表格的行数和列数",
            Description = "创建指定行数和列数的表格",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "Rows", DisplayName = "行数", Description = "表格行数", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, MaxValue = 50 },
                new() { Name = "Columns", DisplayName = "列数", Description = "表格列数", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, MaxValue = 20 }
            ]
        };

        // 知识点36：设置表格底纹
        configs[WordKnowledgeType.SetTableShading] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableShading,
            Name = "设置表格底纹",
            Description = "设置表格指定区域的底纹颜色",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "AreaType", DisplayName = "行和列划分区域", Description = "选择区域类型", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "row,column" },
                new() { Name = "AreaNumber", DisplayName = "第几行和第几列", Description = "按照参数一来判定", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "StartPosition", DisplayName = "起始列或起始行", Description = "起始位置", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "EndPosition", DisplayName = "终止列或终止行", Description = "终止位置", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 1 },
                new() { Name = "ShadingColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };

        // 知识点37：设置表格行高
        configs[WordKnowledgeType.SetTableRowHeight] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableRowHeight,
            Name = "设置表格行高",
            Description = "设置表格指定行的高度",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "StartRow", DisplayName = "起始行", Description = "起始行号", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "EndRow", DisplayName = "终止行", Description = "终止行号", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "RowHeight", DisplayName = "行高", Description = "行高（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 10, MaxValue = 200 },
                new() { Name = "HeightType", DisplayName = "行高类型", Description = "选择行高类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "自动,最小值,固定值" }
            ]
        };

        // 知识点38：设置表格列宽
        configs[WordKnowledgeType.SetTableColumnWidth] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableColumnWidth,
            Name = "设置表格列宽",
            Description = "设置表格指定列的宽度",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "StartColumn", DisplayName = "起始列", Description = "起始列号", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "EndColumn", DisplayName = "终止列", Description = "终止列号", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "ColumnWidth", DisplayName = "列宽", Description = "列宽（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 10, MaxValue = 500 },
                new() { Name = "WidthType", DisplayName = "宽度类型", Description = "选择宽度类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "自动,磅单位,百分比" }
            ]
        };
    }

    private void InitializeGraphicsAndImages(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点45：插入自选图形类型
        configs[WordKnowledgeType.InsertAutoShape] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.InsertAutoShape,
            Name = "插入自选图形类型",
            Description = "插入指定类型的自选图形",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ShapeType", DisplayName = "图形类型", Description = "选择自选图形类型", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "矩形,圆角矩形,椭圆,右箭头,下箭头,左箭头,上箭头,双向箭头,折弯箭头,尖括号,块弧形,爱心形状,笑脸,五角星,16角星,爆炸形状1,爆炸形状2,云形状" }
            ]
        };

        // 知识点59：设置插入图片的高度和宽度
        configs[WordKnowledgeType.SetImageSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageSize,
            Name = "设置插入图片的高度和宽度",
            Description = "设置插入图片的尺寸",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ImageHeight", DisplayName = "高度", Description = "图片高度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 10, MaxValue = 1000 },
                new() { Name = "ImageWidth", DisplayName = "宽度", Description = "图片宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 10, MaxValue = 1000 }
            ]
        };
    }

    private void InitializeTextBoxSettings(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点61：设置文本框边框颜色
        configs[WordKnowledgeType.SetTextBoxBorderColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTextBoxBorderColor,
            Name = "设置文本框边框颜色",
            Description = "设置文本框的边框颜色",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "BorderColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点62：设置文本框中文字内容
        configs[WordKnowledgeType.SetTextBoxContent] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTextBoxContent,
            Name = "设置文本框中文字内容",
            Description = "设置文本框中的文字内容",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "TextContent", DisplayName = "文字值", Description = "文本框中的文字", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点63：设置文本框中文字大小
        configs[WordKnowledgeType.SetTextBoxTextSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTextBoxTextSize,
            Name = "设置文本框中文字大小",
            Description = "设置文本框中文字的字号",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "TextSize", DisplayName = "字号值", Description = "文字大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 72 }
            ]
        };
    }

    private void InitializeOtherOperations(Dictionary<WordKnowledgeType, WordKnowledgeConfig> configs)
    {
        // 知识点66：查找与替换
        configs[WordKnowledgeType.FindAndReplace] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.FindAndReplace,
            Name = "查找与替换",
            Description = "查找并替换文档中的文字",
            Category = "其他操作",
            ParameterTemplates =
            [
                new() { Name = "FindText", DisplayName = "要查找的文本内容", Description = "需要查找的文字", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "ReplaceText", DisplayName = "替换为的文本内容", Description = "替换后的文字", Type = ParameterType.Text, IsRequired = true, Order = 2 },
                new() { Name = "ReplaceCount", DisplayName = "应替换的总数量", Description = "目标替换次数", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1, MaxValue = 100 }
            ]
        };

        // 知识点67：设置指定文字字号
        configs[WordKnowledgeType.SetSpecificTextFontSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetSpecificTextFontSize,
            Name = "设置指定文字字号",
            Description = "设置文档中指定文字的字号",
            Category = "其他操作",
            ParameterTemplates =
            [
                new() { Name = "TargetText", DisplayName = "要设置字号的指定文字", Description = "目标文字", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "FontSize", DisplayName = "应设置的字号大小", Description = "字号大小（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 8, MaxValue = 72 }
            ]
        };
    }
}
