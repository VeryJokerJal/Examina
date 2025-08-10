using System;
using System.Collections.Generic;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// PPT知识点配置服务
/// </summary>
public class PowerPointKnowledgeService
{
    public static PowerPointKnowledgeService Instance { get; } = new();

    private readonly Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> _knowledgeConfigs;

    private PowerPointKnowledgeService()
    {
        _knowledgeConfigs = InitializeKnowledgeConfigs();
    }

    /// <summary>
    /// 获取所有知识点配置
    /// </summary>
    public IEnumerable<PowerPointKnowledgeConfig> GetAllKnowledgeConfigs()
    {
        return _knowledgeConfigs.Values;
    }

    /// <summary>
    /// 根据类型获取知识点配置
    /// </summary>
    public PowerPointKnowledgeConfig? GetKnowledgeConfig(PowerPointKnowledgeType type)
    {
        return _knowledgeConfigs.TryGetValue(type, out PowerPointKnowledgeConfig? config) ? config : null;
    }

    /// <summary>
    /// 根据知识点配置创建操作点
    /// </summary>
    public OperationPoint CreateOperationPoint(PowerPointKnowledgeType type)
    {
        PowerPointKnowledgeConfig? config = GetKnowledgeConfig(type);
        if (config == null)
        {
            throw new ArgumentException($"未找到知识点类型 {type} 的配置");
        }

        OperationPoint operationPoint = new()
        {
            Name = config.Name,
            Description = config.Description,
            ModuleType = ModuleType.PowerPoint,
            PowerPointKnowledgeType = type
        };

        // 根据模板创建参数
        foreach (ConfigurationParameterTemplate template in config.ParameterTemplates)
        {
            ConfigurationParameter parameter = new()
            {
                Name = template.Name,
                DisplayName = template.DisplayName,
                Description = template.Description,
                Type = template.Type,
                DefaultValue = template.DefaultValue,
                IsRequired = template.IsRequired,
                Order = template.Order,
                EnumOptions = template.EnumOptions,
                ValidationRule = template.ValidationRule,
                ValidationErrorMessage = template.ValidationErrorMessage,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                Value = template.DefaultValue
            };
            operationPoint.Parameters.Add(parameter);
        }

        return operationPoint;
    }

    private Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> InitializeKnowledgeConfigs()
    {
        Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> configs = [];

        // 第一类：幻灯片操作
        InitializeSlideOperations(configs);

        // 第二类：文字与字体设置
        InitializeTextAndFontSettings(configs);

        // 第三类：背景样式与设计
        InitializeBackgroundAndDesign(configs);

        // 第四类：母版与主题设置
        InitializeMasterAndTheme(configs);

        // 第五类：其他
        InitializeOtherSettings(configs);

        return configs;
    }

    private void InitializeSlideOperations(Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> configs)
    {
        // 知识点1：设置幻灯片版式
        configs[PowerPointKnowledgeType.SetSlideLayout] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetSlideLayout,
            Name = "设置幻灯片版式",
            Description = "设置指定幻灯片的版式布局",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "幻灯片序号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "Layout", DisplayName = "幻灯片版式", Description = "选择幻灯片版式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "标题幻灯片,标题和双栏文本,两栏文本,表格,文本和图表,图表和文本,组织结构图,图表,文本和剪贴画,剪贴画和文本,仅标题,空白幻灯片,文本和对象,对象和文本,大对象,单个对象,文本和媒体剪辑,媒体剪辑和文本,对象在文本之上,文本在对象之上,文本和两个对象,两个对象和文本,两个对象在文本之上,四个对象,垂直文本,垂直标题和文本,垂直标题和图表,标题母版,文本母版,居中标题,图示和标题,对比布局,内容加标题" }
            ]
        };

        // 知识点2：删除幻灯片
        configs[PowerPointKnowledgeType.DeleteSlide] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.DeleteSlide,
            Name = "删除幻灯片",
            Description = "删除指定的幻灯片",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "幻灯片序号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "SlideIdentifier", DisplayName = "幻灯片标识符", Description = "幻灯片类型标识", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "普通幻灯片,标题幻灯片或布局为标题的类型" }
            ]
        };

        // 知识点3：插入幻灯片
        configs[PowerPointKnowledgeType.InsertSlide] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertSlide,
            Name = "插入幻灯片",
            Description = "在指定位置插入新幻灯片",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "Position", DisplayName = "插入位置", Description = "在第几张幻灯片之后/之前插入", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "InsertMode", DisplayName = "插入方式", Description = "选择插入方式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "之前,之后" },
                new() { Name = "NewSlideLayout", DisplayName = "新插入幻灯片的版式", Description = "新幻灯片的版式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "空白版式,单对象版式" }
            ]
        };

        // 知识点4：设置幻灯片的字体
        configs[PowerPointKnowledgeType.SetSlideFont] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetSlideFont,
            Name = "设置幻灯片的字体",
            Description = "设置指定幻灯片文本框的字体",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "第几张幻灯片", Description = "目标幻灯片编号", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxNumber", DisplayName = "第几个文本框", Description = "1代表标题文本框，2代表第二个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "FontName", DisplayName = "字体", Description = "选择字体", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "仿宋,华文隶书,华文细黑,黑体,华文行楷,隶书,华文彩云,华文楷体" }
            ]
        };

        // 知识点5：幻灯片切换效果
        configs[PowerPointKnowledgeType.SlideTransitionEffect] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SlideTransitionEffect,
            Name = "幻灯片切换效果",
            Description = "设置幻灯片的切换效果",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumbers", DisplayName = "幻灯片张数", Description = "以逗号分隔，如：1,3,5", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "TransitionEffect", DisplayName = "切换方式", Description = "选择切换效果", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "无切换效果,剪切,黑色剪切,平滑淡出,淡出,推动向左,推动向右,推动向上,推动向下,覆盖向左,覆盖向右,覆盖向上,覆盖向下,揭开向左,揭开向右,揭开向上,揭开向下,擦除向左,擦除向右,擦除向上,擦除向下,水平中间向外展开,水平两边向中间合拢,垂直中间向外展开,垂直两边向中间合拢,条纹左上角弹出,条纹右上角弹出,条纹左下角弹出,条纹右下角弹出,水平随机条纹,垂直随机条纹,棋盘交错横向,棋盘交错纵向,闪光灯效果,摩天轮向右旋转,轮状反向1辐射,平移向左,平移向右" }
            ]
        };

        // 知识点6：设置幻灯片切换方式
        configs[PowerPointKnowledgeType.SlideTransitionMode] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SlideTransitionMode,
            Name = "设置幻灯片切换方式",
            Description = "设置幻灯片的切换方案和效果",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumbers", DisplayName = "幻灯片张数", Description = "以逗号分隔，如：1,3,5", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "TransitionScheme", DisplayName = "幻灯片切换方案", Description = "选择切换方案", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "无效果,推入,淡出,覆盖,随机条纹,棋盘格,摩天轮,闪光灯,平移" },
                new() { Name = "TransitionDirection", DisplayName = "幻灯片切换效果", Description = "选择切换方向", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "推入向左,推入向右,推入向上,推入向下,淡出,平滑淡出,覆盖向左,覆盖向右,覆盖向上,覆盖向下,棋盘交错纵向,棋盘交错横向,水平随机条纹,垂直随机条纹,摩天轮向右,闪光灯,平移向左,平移向右,轮状反向1辐射" }
            ]
        };
    }

    private void InitializeTextAndFontSettings(Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> configs)
    {
        // 知识点17：幻灯片插入文本内容
        configs[PowerPointKnowledgeType.InsertTextContent] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertTextContent,
            Name = "幻灯片插入文本内容",
            Description = "在指定幻灯片的文本框中插入文本内容",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TextContent", DisplayName = "文本值内容", Description = "要插入的文本内容", Type = ParameterType.Text, IsRequired = true, Order = 3 }
            ]
        };

        // 知识点18：幻灯片插入文本字号
        configs[PowerPointKnowledgeType.SetTextFontSize] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetTextFontSize,
            Name = "幻灯片插入文本字号",
            Description = "设置指定文本框的字号大小",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "FontSize", DisplayName = "字号值", Description = "字体大小", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 8, MaxValue = 72 }
            ]
        };

        // 知识点19：幻灯片插入文本颜色
        configs[PowerPointKnowledgeType.SetTextColor] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetTextColor,
            Name = "幻灯片插入文本颜色",
            Description = "设置指定文本框的文本颜色",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "ColorValue", DisplayName = "颜色值", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 3 }
            ]
        };

        // 知识点20：幻灯片插入文本字形
        configs[PowerPointKnowledgeType.SetTextStyle] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetTextStyle,
            Name = "幻灯片插入文本字形",
            Description = "设置指定文本框的文本样式",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TextStyle", DisplayName = "字形", Description = "选择文本样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "加粗,斜体,下划线,删除线" }
            ]
        };

        // 知识点21：元素位置
        configs[PowerPointKnowledgeType.SetElementPosition] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetElementPosition,
            Name = "元素位置",
            Description = "设置元素在幻灯片中的位置",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "HorizontalPosition", DisplayName = "水平值", Description = "自左上角的水平位置", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0 },
                new() { Name = "VerticalPosition", DisplayName = "垂直值", Description = "自左上角的垂直位置", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0 }
            ]
        };

        // 知识点22：元素高度和宽度设置
        configs[PowerPointKnowledgeType.SetElementSize] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetElementSize,
            Name = "元素高度和宽度设置",
            Description = "设置元素的尺寸大小",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "ElementHeight", DisplayName = "元素高度", Description = "元素的高度", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "ElementWidth", DisplayName = "元素宽度", Description = "元素的宽度", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 1 }
            ]
        };
    }

    private void InitializeBackgroundAndDesign(Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> configs)
    {
        // 知识点31：设置文稿应用主题
        configs[PowerPointKnowledgeType.ApplyTheme] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.ApplyTheme,
            Name = "设置文稿应用主题",
            Description = "为整个演示文稿应用指定主题",
            Category = "背景样式与设计",
            ParameterTemplates =
            [
                new() { Name = "ThemeName", DisplayName = "主题值", Description = "选择要应用的主题", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "Office主题,切面,聚合,离子,有机,回顾,切片,微风,图库,新闻印刷,都市,平衡,天体,框架,蒸汽轨迹" }
            ]
        };
    }

    private void InitializeMasterAndTheme(Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> configs)
    {
        // 知识点32：设置幻灯片背景
        configs[PowerPointKnowledgeType.SetSlideBackground] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetSlideBackground,
            Name = "设置幻灯片背景",
            Description = "设置指定幻灯片的背景样式",
            Category = "母版与主题设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "FillType", DisplayName = "填充类型", Description = "选择填充类型", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "实心颜色填充,图案填充,纹理填充,渐变填充,背景自动填充" },
                new() { Name = "TextureType", DisplayName = "预设纹理类型", Description = "选择纹理类型", Type = ParameterType.Enum, IsRequired = false, Order = 3,
                    EnumOptions = "混合,莎草纸,画布,牛仔布,编织垫,水珠,纸袋纹理,鱼化石,沙粒,绿色大理石,白色大理石,棕色大理石,花岗岩,报纸纹理,再生纸,羊皮纸,信纸纹理,蓝色薄纸,粉色薄纸,紫色网格,花束纹理,软木纹理,白色花岗岩,胡桃木纹理" },
                new() { Name = "ApplyToMaster", DisplayName = "是否应用于母版", Description = "是否应用于母版", Type = ParameterType.Boolean, IsRequired = false, Order = 4 }
            ]
        };
    }

    private void InitializeOtherSettings(Dictionary<PowerPointKnowledgeType, PowerPointKnowledgeConfig> configs)
    {
        // 知识点33：单元格内容
        configs[PowerPointKnowledgeType.SetTableContent] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetTableContent,
            Name = "单元格内容",
            Description = "设置表格单元格的内容",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "Rows", DisplayName = "行数", Description = "表格行数", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "Columns", DisplayName = "列数", Description = "表格列数", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "Content", DisplayName = "内容", Description = "单元格内容，用逗号分隔", Type = ParameterType.Text, IsRequired = true, Order = 3 }
            ]
        };

        // 知识点34：表格样式
        configs[PowerPointKnowledgeType.SetTableStyle] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetTableStyle,
            Name = "表格样式",
            Description = "设置表格的样式",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "Rows", DisplayName = "行数", Description = "表格行数", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "Columns", DisplayName = "列数", Description = "表格列数", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TableStyle", DisplayName = "表格样式", Description = "选择表格样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "轻度样式1,轻度样式2,轻度样式3,轻度样式4,轻度样式5,轻度样式6,轻度样式7,中度样式1-强调1,中度样式1-强调2,中度样式1-强调3,中度样式2-强调1,中度样式2-强调2,中度样式2-强调3,中度样式3-强调1,中度样式3-强调2,中度样式3-强调3,深度样式1-强调1,深度样式1-强调2,深度样式1-强调3,深度样式2-强调1,深度样式2-强调2,深度样式2-强调3" }
            ]
        };

        // 知识点35：SmartArt样式
        configs[PowerPointKnowledgeType.SetSmartArtStyle] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetSmartArtStyle,
            Name = "SmartArt样式",
            Description = "设置SmartArt图形的样式",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ElementOrder", DisplayName = "元素顺序", Description = "第几个元素", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "SmartArtStyle", DisplayName = "SmartArt样式", Description = "选择SmartArt样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "无样式,粗边框,微弱边框,细微效果,强调边框,强调效果,三维边框,三维平面,三维强调边框,三维强调效果,卡通,渐变边框,光亮边框,金属边框" }
            ]
        };

        // 知识点36：SmartArt内容
        configs[PowerPointKnowledgeType.SetSmartArtContent] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetSmartArtContent,
            Name = "SmartArt内容",
            Description = "设置SmartArt图形的文本内容",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ElementOrder", DisplayName = "元素顺序", Description = "第几个元素", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TextValue", DisplayName = "文本值", Description = "SmartArt的文本内容", Type = ParameterType.Text, IsRequired = true, Order = 3 }
            ]
        };

        // 知识点37：动画计时与延时设置
        configs[PowerPointKnowledgeType.SetAnimationTiming] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetAnimationTiming,
            Name = "动画计时与延时设置",
            Description = "设置动画的计时和延时参数",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "TriggerMode", DisplayName = "动画触发方式", Description = "动画开始方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "单击时,与上一动画同时,在上一动画之后,自动" },
                new() { Name = "DelayTime", DisplayName = "延迟时间", Description = "动画开始前的延迟时间（秒）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 0 },
                new() { Name = "Duration", DisplayName = "动画持续时间", Description = "动画执行的时长（秒）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0.1 },
                new() { Name = "RepeatCount", DisplayName = "重复次数", Description = "动画播放的循环次数", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0 }
            ]
        };

        // 知识点38：段落行距
        configs[PowerPointKnowledgeType.SetParagraphSpacing] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetParagraphSpacing,
            Name = "段落行距",
            Description = "设置文本段落的行间距",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ElementOrder", DisplayName = "元素顺序", Description = "第几个元素", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "LineSpacing", DisplayName = "行间距值", Description = "行间距的数值", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0.5, MaxValue = 5.0 }
            ]
        };

        // 知识点39：设置背景样式
        configs[PowerPointKnowledgeType.SetBackgroundStyle] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetBackgroundStyle,
            Name = "设置背景样式",
            Description = "设置幻灯片的预设背景样式",
            Category = "其他",
            ParameterTemplates =
            [
                new() { Name = "BackgroundStyle", DisplayName = "背景样式", Description = "选择预设背景样式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "混合样式,非预设样式,预设背景样式1,预设背景样式2,预设背景样式3,预设背景样式4,预设背景样式5,预设背景样式6,预设背景样式7,预设背景样式8,预设背景样式9,预设背景样式10,预设背景样式11,预设背景样式12" }
            ]
        };
    }
}
