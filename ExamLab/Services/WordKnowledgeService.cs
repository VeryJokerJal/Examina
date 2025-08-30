using System;
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

    /// <summary>
    /// 修复现有操作点的WordKnowledgeType（用于升级现有数据）
    /// </summary>
    /// <param name="operationPoint">要修复的操作点</param>
    public void FixOperationPointWordKnowledgeType(OperationPoint operationPoint)
    {
        if (operationPoint.ModuleType != ModuleType.Word)
        {
            return; // 只处理Word模块的操作点
        }

        if (operationPoint.WordKnowledgeType.HasValue)
        {
            return; // 已经有WordKnowledgeType，无需修复
        }

        // 根据操作点名称推断WordKnowledgeType
        WordKnowledgeType? inferredType = InferWordKnowledgeTypeFromName(operationPoint.Name);
        if (inferredType.HasValue)
        {
            operationPoint.WordKnowledgeType = inferredType.Value;
            System.Diagnostics.Debug.WriteLine($"修复操作点 '{operationPoint.Name}' 的WordKnowledgeType为 {inferredType.Value}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"无法推断操作点 '{operationPoint.Name}' 的WordKnowledgeType");
        }
    }

    /// <summary>
    /// 根据操作点名称推断WordKnowledgeType
    /// </summary>
    private WordKnowledgeType? InferWordKnowledgeTypeFromName(string operationPointName)
    {
        // 遍历所有知识点配置，找到名称匹配的
        foreach (var kvp in _knowledgeConfigs)
        {
            if (kvp.Value.Name == operationPointName)
            {
                return kvp.Key;
            }
        }

        // 如果没有完全匹配，尝试部分匹配
        return operationPointName switch
        {
            var name when name.Contains("字体") && name.Contains("段落") => WordKnowledgeType.SetParagraphFont,
            var name when name.Contains("字号") && name.Contains("段落") => WordKnowledgeType.SetParagraphFontSize,
            var name when name.Contains("字形") && name.Contains("段落") => WordKnowledgeType.SetParagraphFontStyle,
            var name when name.Contains("字间距") && name.Contains("段落") => WordKnowledgeType.SetParagraphCharacterSpacing,
            var name when name.Contains("颜色") && name.Contains("段落") => WordKnowledgeType.SetParagraphTextColor,
            var name when name.Contains("对齐") && name.Contains("段落") => WordKnowledgeType.SetParagraphAlignment,
            var name when name.Contains("缩进") && name.Contains("段落") => WordKnowledgeType.SetParagraphIndentation,
            var name when name.Contains("行间距") && name.Contains("段落") => WordKnowledgeType.SetParagraphLineSpacing,
            var name when name.Contains("首字下沉") => WordKnowledgeType.SetParagraphDropCap,
            var name when name.Contains("间距") && name.Contains("段落") => WordKnowledgeType.SetParagraphSpacing,
            var name when name.Contains("边框") && name.Contains("段落") => WordKnowledgeType.SetParagraphBorderColor,
            var name when name.Contains("底纹") && name.Contains("段落") => WordKnowledgeType.SetParagraphShading,
            var name when name.Contains("纸张") => WordKnowledgeType.SetPaperSize,
            var name when name.Contains("页边距") => WordKnowledgeType.SetPageMargins,
            var name when name.Contains("页眉") && name.Contains("文字") => WordKnowledgeType.SetHeaderText,
            var name when name.Contains("页眉") && name.Contains("字体") => WordKnowledgeType.SetHeaderFont,
            var name when name.Contains("页眉") && name.Contains("字号") => WordKnowledgeType.SetHeaderFontSize,
            var name when name.Contains("页眉") && name.Contains("对齐") => WordKnowledgeType.SetHeaderAlignment,
            var name when name.Contains("页脚") && name.Contains("文字") => WordKnowledgeType.SetFooterText,
            var name when name.Contains("页脚") && name.Contains("字体") => WordKnowledgeType.SetFooterFont,
            var name when name.Contains("页脚") && name.Contains("字号") => WordKnowledgeType.SetFooterFontSize,
            var name when name.Contains("页脚") && name.Contains("对齐") => WordKnowledgeType.SetFooterAlignment,
            var name when name.Contains("水印") && name.Contains("文字") => WordKnowledgeType.SetWatermarkText,
            var name when name.Contains("水印") && name.Contains("字体") => WordKnowledgeType.SetWatermarkFont,
            var name when name.Contains("水印") && name.Contains("字号") => WordKnowledgeType.SetWatermarkFontSize,
            var name when name.Contains("水印") && name.Contains("颜色") => WordKnowledgeType.SetWatermarkColor,
            _ => null
        };
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "FontFamily", DisplayName = "字体类型", Description = "选择字体", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri", DefaultValue = "宋体" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "FontSize", DisplayName = "字号值", Description = "字体大小", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 8, MaxValue = 72, DefaultValue = "12" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "FontStyle", DisplayName = "字形", Description = "选择字体样式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "常规,加粗,斜体,加粗+斜体,下划线,删除线", DefaultValue = "常规" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "CharacterSpacing", DisplayName = "字间距值", Description = "字符间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = -10, MaxValue = 50, DefaultValue = "0" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "TextColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 2, DefaultValue = "#000000" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "Alignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐", DefaultValue = "左对齐" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "FirstLineIndent", DisplayName = "首行缩进字符数", Description = "首行缩进值", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0, MaxValue = 10, DefaultValue = "2" },
                new() { Name = "LeftIndent", DisplayName = "左缩进字符数", Description = "左缩进值", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0, MaxValue = 10, DefaultValue = "0" },
                new() { Name = "RightIndent", DisplayName = "右缩进字符数", Description = "右缩进值", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0, MaxValue = 10, DefaultValue = "0" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "LineSpacing", DisplayName = "行间距值", Description = "行间距", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0.5, MaxValue = 5.0, DefaultValue = "1.5" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "DropCapType", DisplayName = "首字下沉形式", Description = "选择下沉形式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "不使用下沉,首字下沉到段落中,首字下沉到页边距", DefaultValue = "不使用下沉" }
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
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "SpaceBefore", DisplayName = "段前间距", Description = "段前间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0, MaxValue = 100, DefaultValue = "0" },
                new() { Name = "SpaceAfter", DisplayName = "段后间距", Description = "段后间距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0, MaxValue = 100, DefaultValue = "6" }
            ]
        };

        // 知识点11：设置段落边框颜色
        configs[WordKnowledgeType.SetParagraphBorderColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphBorderColor,
            Name = "设置段落边框颜色",
            Description = "设置指定段落的边框颜色",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "边框颜色", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "黑色,红色,蓝色,绿色,黄色,紫色,橙色,灰色,自动", DefaultValue = "黑色" }
            ]
        };

        // 知识点12：设置段落边框样式
        configs[WordKnowledgeType.SetParagraphBorderStyle] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphBorderStyle,
            Name = "设置段落边框样式",
            Description = "设置指定段落的边框样式",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "BorderStyle", DisplayName = "边框样式", Description = "边框线型", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "无,单实线,双线,点线,虚线,粗线,细线,波浪线", DefaultValue = "单实线" }
            ]
        };

        // 知识点13：设置段落边框宽度
        configs[WordKnowledgeType.SetParagraphBorderWidth] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphBorderWidth,
            Name = "设置段落边框宽度",
            Description = "设置指定段落的边框宽度",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "BorderWidth", DisplayName = "边框宽度", Description = "边框线宽", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "0.25磅,0.5磅,0.75磅,1磅,1.5磅,2.25磅,3磅,4.5磅,6磅", DefaultValue = "1磅" }
            ]
        };

        // 知识点14：设置段落底纹
        configs[WordKnowledgeType.SetParagraphShading] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetParagraphShading,
            Name = "设置段落底纹",
            Description = "设置指定段落的底纹填充",
            Category = "段落操作",
            ParameterTemplates =
            [
                new() { Name = "ParagraphNumber", DisplayName = "段落序号", Description = "第几个段落", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "-1" },
                new() { Name = "ShadingColor", DisplayName = "底纹颜色", Description = "底纹填充颜色", Type = ParameterType.Color, IsRequired = true, Order = 2, DefaultValue = "#FFFF00" },
                new() { Name = "ShadingPattern", DisplayName = "底纹图案", Description = "底纹图案样式（基于WdTextureIndex枚举）", Type = ParameterType.Enum, IsRequired = false, Order = 3,
                    EnumOptions = "无纹理,2.5%,5%,7.5%,10%,12.5%,15%,17.5%,20%,22.5%,25%,27.5%,30%,32.5%,35%,37.5%,40%,42.5%,45%,47.5%,50%,52.5%,55%,57.5%,60%,62.5%,65%,67.5%,70%,72.5%,75%,77.5%,80%,82.5%,85%,87.5%,90%,92.5%,95%,97.5%,实心填充,深色水平线,深色垂直线,深色对角线下,深色对角线上,深色十字,深色对角十字,水平线,垂直线,对角线下,对角线上,十字,对角十字", DefaultValue = "无纹理" }
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
                    EnumOptions = "A4纸,A3纸,B5纸,法律纸尺寸", DefaultValue = "A4纸" }
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
                new() { Name = "TopMargin", DisplayName = "上边距", Description = "上边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 0, MaxValue = 200, DefaultValue = "72" },
                new() { Name = "BottomMargin", DisplayName = "下边距", Description = "下边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0, MaxValue = 200, DefaultValue = "72" },
                new() { Name = "LeftMargin", DisplayName = "左边距", Description = "左边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0, MaxValue = 200, DefaultValue = "90" },
                new() { Name = "RightMargin", DisplayName = "右边距", Description = "右边距（磅）", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0, MaxValue = 200, DefaultValue = "90" }
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
                new() { Name = "HeaderText", DisplayName = "页眉文字内容", Description = "页眉中显示的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "页眉内容" }
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
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri", DefaultValue = "宋体" }
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
                new() { Name = "HeaderFontSize", DisplayName = "字号数值", Description = "页眉字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 72, DefaultValue = "10" }
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
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐", DefaultValue = "居中对齐" }
            ]
        };

        // 知识点21：设置页脚中的文字
        configs[WordKnowledgeType.SetFooterText] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetFooterText,
            Name = "设置页脚中的文字",
            Description = "设置页脚的文字内容",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "FooterText", DisplayName = "页脚文字", Description = "页脚文字内容", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "页脚内容" }
            ]
        };

        // 知识点22：设置页脚中文字的字体
        configs[WordKnowledgeType.SetFooterFont] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetFooterFont,
            Name = "设置页脚中文字的字体",
            Description = "设置页脚文字的字体",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "FooterFont", DisplayName = "字体类型", Description = "页脚字体", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri", DefaultValue = "宋体" }
            ]
        };

        // 知识点23：设置页脚中文字的字号
        configs[WordKnowledgeType.SetFooterFontSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetFooterFontSize,
            Name = "设置页脚中文字的字号",
            Description = "设置页脚文字的字号",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "FooterFontSize", DisplayName = "字号数值", Description = "页脚字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 72, DefaultValue = "10" }
            ]
        };

        // 知识点24：设置页脚中文字的对齐方式
        configs[WordKnowledgeType.SetFooterAlignment] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetFooterAlignment,
            Name = "设置页脚中文字的对齐方式",
            Description = "设置页脚文字的对齐方式",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "FooterAlignment", DisplayName = "对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐,分散对齐", DefaultValue = "居中对齐" }
            ]
        };

        // 知识点25：设置页码
        configs[WordKnowledgeType.SetPageNumber] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPageNumber,
            Name = "设置页码",
            Description = "设置文档的页码格式和位置",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "PageNumberPosition", DisplayName = "页码位置", Description = "页码显示位置", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "页面顶端居中,页面顶端左侧,页面顶端右侧,页面底端居中,页面底端左侧,页面底端右侧", DefaultValue = "页面底端居中" },
                new() { Name = "PageNumberFormat", DisplayName = "页码格式", Description = "页码数字格式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "1,2,3...,a,b,c...,A,B,C...,i,ii,iii...,I,II,III...", DefaultValue = "1,2,3..." }
            ]
        };

        // 知识点26：设置页面背景
        configs[WordKnowledgeType.SetPageBackground] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPageBackground,
            Name = "设置页面背景",
            Description = "设置文档的页面背景颜色",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "BackgroundColor", DisplayName = "背景颜色", Description = "页面背景颜色", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "无填充,白色,浅灰色,灰色,深灰色,黑色,红色,蓝色,绿色,黄色,紫色,橙色", DefaultValue = "无填充" }
            ]
        };

        // 知识点27：设置页面边框颜色
        configs[WordKnowledgeType.SetPageBorderColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPageBorderColor,
            Name = "设置页面边框颜色",
            Description = "设置页面边框的颜色",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "页面边框颜色", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "黑色,红色,蓝色,绿色,黄色,紫色,橙色,灰色,自动", DefaultValue = "黑色" }
            ]
        };

        // 知识点28：设置页面边框样式
        configs[WordKnowledgeType.SetPageBorderStyle] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPageBorderStyle,
            Name = "设置页面边框样式",
            Description = "设置页面边框的样式",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "BorderStyle", DisplayName = "边框样式", Description = "页面边框线型", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "无,单实线,双线,点线,虚线,粗线,细线,波浪线,艺术型边框", DefaultValue = "单实线" }
            ]
        };

        // 知识点29：设置页面边框宽度
        configs[WordKnowledgeType.SetPageBorderWidth] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetPageBorderWidth,
            Name = "设置页面边框宽度",
            Description = "设置页面边框的宽度",
            Category = "页面设置",
            ParameterTemplates =
            [
                new() { Name = "BorderWidth", DisplayName = "边框宽度", Description = "页面边框线宽", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "0.25磅,0.5磅,0.75磅,1磅,1.5磅,2.25磅,3磅,4.5磅,6磅", DefaultValue = "1磅" }
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
                new() { Name = "WatermarkText", DisplayName = "水印文字", Description = "水印显示的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "机密" }
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
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri", DefaultValue = "宋体" }
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
                new() { Name = "WatermarkFontSize", DisplayName = "字号值", Description = "水印字体大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 144, DefaultValue = "36" }
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
                new() { Name = "WatermarkAngle", DisplayName = "水印文字倾斜角度", Description = "倾斜角度（度）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = -90, MaxValue = 90, DefaultValue = "45" }
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
                new() { Name = "ParagraphNumbers", DisplayName = "段落编号ID", Description = "用#分隔的段落编号，如：11#12#13", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "1#2#3" },
                new() { Name = "NumberingType", DisplayName = "项目编号类型", Description = "选择编号类型", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "项目符号,数字编号,多级编号,简单数字,小写字母编号,大写字母编号", DefaultValue = "数字编号" }
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
                new() { Name = "Rows", DisplayName = "行数", Description = "表格行数", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, MaxValue = 50, DefaultValue = "3" },
                new() { Name = "Columns", DisplayName = "列数", Description = "表格列数", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, MaxValue = 20, DefaultValue = "3" }
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
                    EnumOptions = "row,column", DefaultValue = "row" },
                new() { Name = "AreaNumber", DisplayName = "第几行和第几列", Description = "按照参数一来判定", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, DefaultValue = "1" },
                new() { Name = "StartPosition", DisplayName = "起始列或起始行", Description = "起始位置", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1, DefaultValue = "1" },
                new() { Name = "EndPosition", DisplayName = "终止列或终止行", Description = "终止位置", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 1, DefaultValue = "1" },
                new() { Name = "ShadingColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 5, DefaultValue = "#FFFF00" }
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
                new() { Name = "StartRow", DisplayName = "起始行", Description = "起始行号", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "EndRow", DisplayName = "终止行", Description = "终止行号", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, DefaultValue = "1" },
                new() { Name = "RowHeight", DisplayName = "行高", Description = "行高（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 10, MaxValue = 200, DefaultValue = "20" },
                new() { Name = "HeightType", DisplayName = "行高类型", Description = "选择行高类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "自动,最小值,固定值", DefaultValue = "自动" }
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
                new() { Name = "StartColumn", DisplayName = "起始列", Description = "起始列号", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "EndColumn", DisplayName = "终止列", Description = "终止列号", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, DefaultValue = "1" },
                new() { Name = "ColumnWidth", DisplayName = "列宽", Description = "列宽（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 10, MaxValue = 500, DefaultValue = "100" },
                new() { Name = "WidthType", DisplayName = "宽度类型", Description = "选择宽度类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "自动,磅单位,百分比", DefaultValue = "自动" }
            ]
        };

        // 知识点39：设置表格单元格内容
        configs[WordKnowledgeType.SetTableCellContent] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableCellContent,
            Name = "设置表格单元格内容",
            Description = "设置表格指定单元格的内容",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "RowNumber", DisplayName = "行号", Description = "单元格所在行", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "ColumnNumber", DisplayName = "列号", Description = "单元格所在列", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, DefaultValue = "1" },
                new() { Name = "CellContent", DisplayName = "单元格内容", Description = "要设置的文字内容", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "内容" }
            ]
        };

        // 知识点40：设置表格单元格对齐方式
        configs[WordKnowledgeType.SetTableCellAlignment] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableCellAlignment,
            Name = "设置表格单元格对齐方式",
            Description = "设置表格单元格的对齐方式",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "RowNumber", DisplayName = "行号", Description = "单元格所在行", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "ColumnNumber", DisplayName = "列号", Description = "单元格所在列", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, DefaultValue = "1" },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐", DefaultValue = "左对齐" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "顶端对齐,居中对齐,底端对齐", DefaultValue = "居中对齐" }
            ]
        };

        // 知识点41：设置表格对齐方式
        configs[WordKnowledgeType.SetTableAlignment] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableAlignment,
            Name = "设置表格对齐方式",
            Description = "设置整个表格的对齐方式",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "TableAlignment", DisplayName = "表格对齐", Description = "表格对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "左对齐,居中对齐,右对齐", DefaultValue = "左对齐" },
                new() { Name = "LeftIndent", DisplayName = "左缩进", Description = "表格左缩进（磅）", Type = ParameterType.Number, IsRequired = false, Order = 2, MinValue = 0, MaxValue = 100, DefaultValue = "0" }
            ]
        };

        // 知识点42：合并表格单元格
        configs[WordKnowledgeType.MergeTableCells] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.MergeTableCells,
            Name = "合并表格单元格",
            Description = "合并表格中的单元格",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "StartRow", DisplayName = "起始行", Description = "合并区域起始行", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "StartColumn", DisplayName = "起始列", Description = "合并区域起始列", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1, DefaultValue = "1" },
                new() { Name = "EndRow", DisplayName = "结束行", Description = "合并区域结束行", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1, DefaultValue = "2" },
                new() { Name = "EndColumn", DisplayName = "结束列", Description = "合并区域结束列", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 1, DefaultValue = "2" }
            ]
        };

        // 知识点43：设置表格标题内容
        configs[WordKnowledgeType.SetTableHeaderContent] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableHeaderContent,
            Name = "设置表格标题内容",
            Description = "设置表格标题行的内容",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "ColumnNumber", DisplayName = "列号", Description = "标题所在列", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "HeaderContent", DisplayName = "标题内容", Description = "标题文字内容", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "标题" }
            ]
        };

        // 知识点44：设置表格标题对齐方式
        configs[WordKnowledgeType.SetTableHeaderAlignment] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTableHeaderAlignment,
            Name = "设置表格标题对齐方式",
            Description = "设置表格标题行的对齐方式",
            Category = "表格操作",
            ParameterTemplates =
            [
                new() { Name = "ColumnNumber", DisplayName = "列号", Description = "标题所在列", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1, DefaultValue = "1" },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐", DefaultValue = "居中对齐" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "顶端对齐,居中对齐,底端对齐", DefaultValue = "居中对齐" }
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
                    EnumOptions = "矩形,圆角矩形,椭圆,右箭头,下箭头,左箭头,上箭头,双向箭头,折弯箭头,尖括号,块弧形,爱心形状,笑脸,五角星,16角星,爆炸形状1,爆炸形状2,云形状", DefaultValue = "矩形" }
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
                new() { Name = "ImageHeight", DisplayName = "高度", Description = "图片高度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 10, MaxValue = 1000, DefaultValue = "200" },
                new() { Name = "ImageWidth", DisplayName = "宽度", Description = "图片宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 10, MaxValue = 1000, DefaultValue = "200" }
            ]
        };

        // 知识点45：设置自选图形大小
        configs[WordKnowledgeType.SetAutoShapeSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapeSize,
            Name = "设置自选图形大小",
            Description = "设置自选图形的高度和宽度",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ShapeHeight", DisplayName = "高度", Description = "图形高度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 10, MaxValue = 1000, DefaultValue = "100" },
                new() { Name = "ShapeWidth", DisplayName = "宽度", Description = "图形宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 10, MaxValue = 1000, DefaultValue = "100" }
            ]
        };

        // 知识点46：设置自选图形线条颜色
        configs[WordKnowledgeType.SetAutoShapeLineColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapeLineColor,
            Name = "设置自选图形线条颜色",
            Description = "设置自选图形的线条颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "LineColor", DisplayName = "线条颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点47：设置自选图形填充颜色
        configs[WordKnowledgeType.SetAutoShapeFillColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapeFillColor,
            Name = "设置自选图形填充颜色",
            Description = "设置自选图形的填充颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "FillColor", DisplayName = "填充颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#FFFFFF" }
            ]
        };

        // 知识点48：设置自选图形文字大小
        configs[WordKnowledgeType.SetAutoShapeTextSize] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapeTextSize,
            Name = "设置自选图形文字大小",
            Description = "设置自选图形中文字的大小",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "FontSize", DisplayName = "字号", Description = "文字字号", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 72, DefaultValue = "12" }
            ]
        };

        // 知识点49：设置自选图形文字颜色
        configs[WordKnowledgeType.SetAutoShapeTextColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapeTextColor,
            Name = "设置自选图形文字颜色",
            Description = "设置自选图形中文字的颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "TextColor", DisplayName = "文字颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点50：设置自选图形文字内容
        configs[WordKnowledgeType.SetAutoShapeTextContent] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapeTextContent,
            Name = "设置自选图形文字内容",
            Description = "设置自选图形中的文字内容",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "TextContent", DisplayName = "文字内容", Description = "要设置的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "文字内容" }
            ]
        };

        // 知识点51：设置自选图形位置
        configs[WordKnowledgeType.SetAutoShapePosition] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetAutoShapePosition,
            Name = "设置自选图形位置",
            Description = "设置自选图形的位置",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                // 水平位置设置
                new() { Name = "HorizontalPositionType", DisplayName = "水平位置类型", Description = "选择水平位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "对齐方式,书签位置,绝对位置,相对位置", Group = "水平位置", DefaultValue = "对齐方式" },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐方式", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 2,
                    EnumOptions = "左对齐,居中对齐,右对齐,内部,外部,左侧对齐,右侧对齐", Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "对齐方式", DefaultValue = "左对齐" },
                new() { Name = "HorizontalRelativeTo", DisplayName = "水平相对于", Description = "水平位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 3,
                    EnumOptions = "页面,页边距,列,字符,左边距,右边距,内边距,外边距", Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "对齐方式,相对位置", DefaultValue = "页面" },
                new() { Name = "HorizontalAbsolutePosition", DisplayName = "水平绝对位置", Description = "水平绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 4, MinValue = -50, MaxValue = 50,
                    Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "绝对位置" },
                new() { Name = "HorizontalRelativePosition", DisplayName = "水平相对位置", Description = "水平相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = -999, MaxValue = 999,
                    Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "相对位置" },

                // 垂直位置设置
                new() { Name = "VerticalPositionType", DisplayName = "垂直位置类型", Description = "选择垂直位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 6,
                    EnumOptions = "对齐方式,绝对位置,相对位置", Group = "垂直位置", DefaultValue = "对齐方式" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐方式", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 7,
                    EnumOptions = "顶端对齐,居中对齐,底端对齐,内部,外部,顶端,底端", Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "对齐方式", DefaultValue = "顶端对齐" },
                new() { Name = "VerticalRelativeTo", DisplayName = "垂直相对于", Description = "垂直位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 8,
                    EnumOptions = "页面,页边距,段落,行,上边距,下边距,内边距,外边距", Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "对齐方式,相对位置", DefaultValue = "页面" },
                new() { Name = "VerticalAbsolutePosition", DisplayName = "垂直绝对位置", Description = "垂直绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 9, MinValue = -50, MaxValue = 50,
                    Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "绝对位置" },
                new() { Name = "VerticalRelativePosition", DisplayName = "垂直相对位置", Description = "垂直相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 10, MinValue = -999, MaxValue = 999,
                    Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "相对位置" },

                // 选项设置
                new() { Name = "MoveWithText", DisplayName = "对象随文字移动", Description = "对象是否随文字移动", Type = ParameterType.Boolean, IsRequired = false, Order = 11, Group = "选项设置" },
                new() { Name = "LockAnchor", DisplayName = "锁定锚点", Description = "是否锁定锚点", Type = ParameterType.Boolean, IsRequired = false, Order = 12, Group = "选项设置" },
                new() { Name = "AllowOverlap", DisplayName = "允许重叠", Description = "是否允许与其他对象重叠", Type = ParameterType.Boolean, IsRequired = false, Order = 13, Group = "选项设置" },
                new() { Name = "LayoutInTableCell", DisplayName = "在表格单元格中的版式", Description = "在表格单元格中的版式设置", Type = ParameterType.Boolean, IsRequired = false, Order = 14, Group = "选项设置" }
            ]
        };

        // 知识点52：设置图片边框复合类型
        configs[WordKnowledgeType.SetImageBorderCompoundType] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageBorderCompoundType,
            Name = "设置图片边框复合类型",
            Description = "设置图片边框的复合线型",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "CompoundType", DisplayName = "复合类型", Description = "边框复合线型", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "单线,双线,粗细线,细粗线,三线", DefaultValue = "单线" }
            ]
        };

        // 知识点53：设置图片边框虚线类型
        configs[WordKnowledgeType.SetImageBorderDashType] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageBorderDashType,
            Name = "设置图片边框虚线类型",
            Description = "设置图片边框的虚线样式",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "DashType", DisplayName = "虚线类型", Description = "边框虚线样式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "实线,圆点,方点,短划线,长划线,短划线点,长划线点,长划线点点", DefaultValue = "实线" }
            ]
        };

        // 知识点54：设置图片边框宽度
        configs[WordKnowledgeType.SetImageBorderWidth] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageBorderWidth,
            Name = "设置图片边框宽度",
            Description = "设置图片边框的宽度",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "BorderWidth", DisplayName = "边框宽度", Description = "边框宽度（磅）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 0.25, MaxValue = 6, DefaultValue = "1" }
            ]
        };

        // 知识点55：设置图片边框颜色
        configs[WordKnowledgeType.SetImageBorderColor] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageBorderColor,
            Name = "设置图片边框颜色",
            Description = "设置图片边框的颜色",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
            ]
        };

        // 知识点56：设置图片阴影
        configs[WordKnowledgeType.SetImageShadow] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageShadow,
            Name = "设置图片阴影",
            Description = "设置图片的阴影效果",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "ShadowType", DisplayName = "阴影类型", Description = "阴影样式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "无阴影,偏移对角右下,偏移对角右上,偏移对角左下,偏移对角左上,偏移右,偏移下,偏移左,偏移上", DefaultValue = "无阴影" },
                new() { Name = "ShadowColor", DisplayName = "阴影颜色", Description = "阴影颜色", Type = ParameterType.Color, IsRequired = false, Order = 2, DefaultValue = "#808080" }
            ]
        };

        // 知识点57：设置图片环绕方式
        configs[WordKnowledgeType.SetImageWrapStyle] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImageWrapStyle,
            Name = "设置图片环绕方式",
            Description = "设置图片的文字环绕方式",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                new() { Name = "WrapStyle", DisplayName = "环绕方式", Description = "文字环绕样式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "嵌入型,四周型,紧密型,穿越型,上下型,衬于文字下方,浮于文字上方", DefaultValue = "嵌入型" }
            ]
        };

        // 知识点58：设置图片位置
        configs[WordKnowledgeType.SetImagePosition] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetImagePosition,
            Name = "设置图片位置",
            Description = "设置图片的位置",
            Category = "图形和图片设置",
            ParameterTemplates =
            [
                // 水平位置设置
                new() { Name = "HorizontalPositionType", DisplayName = "水平位置类型", Description = "选择水平位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "对齐方式,书签位置,绝对位置,相对位置", Group = "水平位置", DefaultValue = "对齐方式" },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐方式", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 2,
                    EnumOptions = "左对齐,居中对齐,右对齐,内部,外部,左侧对齐,右侧对齐", Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "对齐方式", DefaultValue = "左对齐" },
                new() { Name = "HorizontalRelativeTo", DisplayName = "水平相对于", Description = "水平位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 3,
                    EnumOptions = "页面,页边距,列,字符,左边距,右边距,内边距,外边距", Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "对齐方式,相对位置", DefaultValue = "页面" },
                new() { Name = "HorizontalAbsolutePosition", DisplayName = "水平绝对位置", Description = "水平绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 4, MinValue = -50, MaxValue = 50,
                    Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "绝对位置" },
                new() { Name = "HorizontalRelativePosition", DisplayName = "水平相对位置", Description = "水平相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = -999, MaxValue = 999,
                    Group = "水平位置", DependsOn = "HorizontalPositionType", DependsOnValue = "相对位置" },

                // 垂直位置设置
                new() { Name = "VerticalPositionType", DisplayName = "垂直位置类型", Description = "选择垂直位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 6,
                    EnumOptions = "对齐方式,绝对位置,相对位置", Group = "垂直位置", DefaultValue = "对齐方式" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐方式", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 7,
                    EnumOptions = "顶端对齐,居中对齐,底端对齐,内部,外部,顶端,底端", Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "对齐方式", DefaultValue = "顶端对齐" },
                new() { Name = "VerticalRelativeTo", DisplayName = "垂直相对于", Description = "垂直位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 8,
                    EnumOptions = "页面,页边距,段落,行,上边距,下边距,内边距,外边距", Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "对齐方式,相对位置", DefaultValue = "页面" },
                new() { Name = "VerticalAbsolutePosition", DisplayName = "垂直绝对位置", Description = "垂直绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 9, MinValue = -50, MaxValue = 50,
                    Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "绝对位置" },
                new() { Name = "VerticalRelativePosition", DisplayName = "垂直相对位置", Description = "垂直相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 10, MinValue = -999, MaxValue = 999,
                    Group = "垂直位置", DependsOn = "VerticalPositionType", DependsOnValue = "相对位置" },

                // 选项设置
                new() { Name = "MoveWithText", DisplayName = "对象随文字移动", Description = "对象是否随文字移动", Type = ParameterType.Boolean, IsRequired = false, Order = 11, Group = "选项设置" },
                new() { Name = "LockAnchor", DisplayName = "锁定锚点", Description = "是否锁定锚点", Type = ParameterType.Boolean, IsRequired = false, Order = 12, Group = "选项设置" },
                new() { Name = "AllowOverlap", DisplayName = "允许重叠", Description = "是否允许与其他对象重叠", Type = ParameterType.Boolean, IsRequired = false, Order = 13, Group = "选项设置" },
                new() { Name = "LayoutInTableCell", DisplayName = "在表格单元格中的版式", Description = "在表格单元格中的版式设置", Type = ParameterType.Boolean, IsRequired = false, Order = 14, Group = "选项设置" }
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
                new() { Name = "BorderColor", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 1, DefaultValue = "#000000" }
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
                new() { Name = "TextContent", DisplayName = "文字值", Description = "文本框中的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "文本框内容" }
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
                new() { Name = "TextSize", DisplayName = "字号值", Description = "文字大小", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 8, MaxValue = 72, DefaultValue = "12" }
            ]
        };

        // 知识点64：设置文本框位置
        configs[WordKnowledgeType.SetTextBoxPosition] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTextBoxPosition,
            Name = "设置文本框位置",
            Description = "设置文本框的位置",
            Category = "文本框设置",
            ParameterTemplates =
            [
                // 水平位置设置
                new() { Name = "HorizontalPositionType", DisplayName = "水平位置类型", Description = "选择水平位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "对齐方式,书签位置,绝对位置,相对位置", DefaultValue = "对齐方式" },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐方式", Description = "水平对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 2,
                    EnumOptions = "左对齐,居中对齐,右对齐,内部,外部,左侧对齐,右侧对齐", DefaultValue = "左对齐" },
                new() { Name = "HorizontalRelativeTo", DisplayName = "水平相对于", Description = "水平位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 3,
                    EnumOptions = "页面,页边距,列,字符,左边距,右边距,内边距,外边距", DefaultValue = "页面" },
                new() { Name = "HorizontalAbsolutePosition", DisplayName = "水平绝对位置", Description = "水平绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 4, MinValue = -50, MaxValue = 50 },
                new() { Name = "HorizontalRelativePosition", DisplayName = "水平相对位置", Description = "水平相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = -999, MaxValue = 999 },

                // 垂直位置设置
                new() { Name = "VerticalPositionType", DisplayName = "垂直位置类型", Description = "选择垂直位置设置方式", Type = ParameterType.Enum, IsRequired = true, Order = 6,
                    EnumOptions = "对齐方式,绝对位置,相对位置", DefaultValue = "对齐方式" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐方式", Description = "垂直对齐方式", Type = ParameterType.Enum, IsRequired = false, Order = 7,
                    EnumOptions = "顶端对齐,居中对齐,底端对齐,内部,外部,顶端,底端", DefaultValue = "顶端对齐" },
                new() { Name = "VerticalRelativeTo", DisplayName = "垂直相对于", Description = "垂直位置相对参考点", Type = ParameterType.Enum, IsRequired = false, Order = 8,
                    EnumOptions = "页面,页边距,段落,行,上边距,下边距,内边距,外边距", DefaultValue = "页面" },
                new() { Name = "VerticalAbsolutePosition", DisplayName = "垂直绝对位置", Description = "垂直绝对位置（厘米）", Type = ParameterType.Number, IsRequired = false, Order = 9, MinValue = -50, MaxValue = 50 },
                new() { Name = "VerticalRelativePosition", DisplayName = "垂直相对位置", Description = "垂直相对位置（百分比）", Type = ParameterType.Number, IsRequired = false, Order = 10, MinValue = -999, MaxValue = 999 },

                // 选项设置
                new() { Name = "MoveWithText", DisplayName = "对象随文字移动", Description = "对象是否随文字移动", Type = ParameterType.Boolean, IsRequired = false, Order = 11 },
                new() { Name = "LockAnchor", DisplayName = "锁定锚点", Description = "是否锁定锚点", Type = ParameterType.Boolean, IsRequired = false, Order = 12 },
                new() { Name = "AllowOverlap", DisplayName = "允许重叠", Description = "是否允许与其他对象重叠", Type = ParameterType.Boolean, IsRequired = false, Order = 13 },
                new() { Name = "LayoutInTableCell", DisplayName = "在表格单元格中的版式", Description = "在表格单元格中的版式设置", Type = ParameterType.Boolean, IsRequired = false, Order = 14 }
            ]
        };

        // 知识点65：设置文本框环绕方式
        configs[WordKnowledgeType.SetTextBoxWrapStyle] = new WordKnowledgeConfig
        {
            KnowledgeType = WordKnowledgeType.SetTextBoxWrapStyle,
            Name = "设置文本框环绕方式",
            Description = "设置文本框的文字环绕方式",
            Category = "文本框设置",
            ParameterTemplates =
            [
                new() { Name = "WrapStyle", DisplayName = "环绕方式", Description = "文字环绕样式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "嵌入型,四周型,紧密型,穿越型,上下型,衬于文字下方,浮于文字上方", DefaultValue = "嵌入型" }
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
                new() { Name = "FindText", DisplayName = "要查找的文本内容", Description = "需要查找的文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "查找内容" },
                new() { Name = "ReplaceText", DisplayName = "替换为的文本内容", Description = "替换后的文字", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "替换内容" },
                new() { Name = "ReplaceCount", DisplayName = "应替换的总数量", Description = "目标替换次数", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1, MaxValue = 100, DefaultValue = "1" }
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
                new() { Name = "TargetText", DisplayName = "要设置字号的指定文字", Description = "目标文字", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "目标文字" },
                new() { Name = "FontSize", DisplayName = "应设置的字号大小", Description = "字号大小（磅）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 8, MaxValue = 72, DefaultValue = "12" }
            ]
        };
    }
}
