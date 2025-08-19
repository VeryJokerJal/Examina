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
                new() { Name = "SlideNumber", DisplayName = "幻灯片序号", Description = "第几张（-1代表任意一张）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = -1 },
                new() { Name = "Layout", DisplayName = "幻灯片版式", Description = "选择幻灯片版式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "标题幻灯片,标题和内容,节标题,两栏内容,比较,内容与标题,图片与标题,标题和竖排文字,垂直排列标题与文本,仅标题,空白" }
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
                new() { Name = "SlideNumber", DisplayName = "幻灯片序号", Description = "第几张（-1代表任意一张）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = -1 },
                new() { Name = "SlideIdentifier", DisplayName = "幻灯片标识符", Description = "幻灯片类型", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "普通幻灯片（ppt类型）,标题幻灯片或布局为标题的类型" }
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
                new() { Name = "Position", DisplayName = "在第几张幻灯片之后/之前插入幻灯片", Description = "插入位置", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "InsertMode", DisplayName = "插入方式", Description = "选择插入方式", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "之前,之后" },
                new() { Name = "NewSlideLayout", DisplayName = "新插入幻灯片的版式", Description = "新幻灯片的版式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "空白版式（无标题无内容）,单对象版式（可放图表、图像等）" }
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
                new() { Name = "TextBoxNumber", DisplayName = "第几个文本框", Description = "1，代表标题文本框，2代表第二个文本框（-1代表任意一个）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = -1 },
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
                new() { Name = "SlideNumbers", DisplayName = "幻灯片张数（可配置多个）", Description = "以逗号分隔，举例：1,3,5", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "TransitionEffect", DisplayName = "切换方式", Description = "选择切换效果", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "无,平滑,淡入淡出,擦入,推入,覆盖,切入,随机条纹,形状,显示,切出,变换,突出,帘式,布式,风,上拉帘幕,折叠,压碎,到达,页面卷曲,飞机,日式折纸,泡沫,蜂巢,百叶窗,时钟,涟漪,翻转,剥转,库,立方体,门,程,转盘,缩放,随机,平移,传送系统,传送,旋转,宫口,轨道,飞过" }
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
                new() { Name = "SlideNumbers", DisplayName = "幻灯片张数（可配置多个）", Description = "以逗号分隔，举例：1,3,5", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "TransitionScheme", DisplayName = "幻灯片切换方案", Description = "选择切换方案", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "细微,华丽,动感内容" },
                new() { Name = "TransitionDirection", DisplayName = "幻灯片切换效果", Description = "选择切换方向", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "向左,向右,向上,向下,从左上角,从右上角,从左下角,从右下角,水平向内,水平向外,垂直向内,垂直向外,顺时针,逆时针,从中心向外,从外向中心,随机方向" }
            ]
        };

        // 知识点7：幻灯片放映方式
        configs[PowerPointKnowledgeType.SlideshowMode] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SlideshowMode,
            Name = "幻灯片放映方式",
            Description = "设置幻灯片的放映方式",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideshowMode", DisplayName = "幻灯片放映方式", Description = "选择放映方式", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "演讲者放映（全屏）,窗口中放映,展示台放映（信息亭模式）,第二窗口放映" }
            ]
        };

        // 知识点8：幻灯片放映选项
        configs[PowerPointKnowledgeType.SlideshowOptions] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SlideshowOptions,
            Name = "幻灯片放映选项",
            Description = "设置幻灯片的放映选项",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "LoopUntilStopped", DisplayName = "是否循环播放", Description = "是否循环播放", Type = ParameterType.Enum, IsRequired = true, Order = 1,
                    EnumOptions = "是,否" },
                new() { Name = "ShowWithNarration", DisplayName = "是否使用旁白", Description = "是否使用旁白", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "是,否" },
                new() { Name = "ShowWithAnimation", DisplayName = "是否使用动画", Description = "是否使用动画", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "是,否" }
            ]
        };

        // 知识点9：幻灯片插入超链接
        configs[PowerPointKnowledgeType.InsertHyperlink] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertHyperlink,
            Name = "幻灯片插入超链接",
            Description = "为文本或对象插入超链接",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片编号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxNumber", DisplayName = "文本框编号", Description = "插入第几个文本框（-1代表任意一个）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = -1 },
                new() { Name = "HyperlinkType", DisplayName = "超链接类型", Description = "超链接类型", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "超链接到外部网页,超链接到本演示文稿中的某张幻灯片" },
                new() { Name = "LinkText", DisplayName = "中文文本值", Description = "显示的链接文字", Type = ParameterType.Text, IsRequired = true, Order = 4 },
                new() { Name = "TargetSlideNumber", DisplayName = "超链接目标幻灯片编号", Description = "目标幻灯片编号（仅当类型为本演示文稿时）", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = 1 }
            ]
        };

        // 知识点10：幻灯片切换播放声音
        configs[PowerPointKnowledgeType.SlideTransitionSound] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SlideTransitionSound,
            Name = "幻灯片切换播放声音",
            Description = "设置幻灯片切换时的声音效果",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumbers", DisplayName = "操作目标幻灯片序号（可配置多个）", Description = "以逗号分隔，举例：1,3,5", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "SoundEffect", DisplayName = "声音", Description = "切换声音", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "风铃声,叮一声,激光音效,推动声,爆炸声,快门声,鼓声,爆炸,飞驰声,掌声,风声,打字机,无声音" }
            ]
        };

        // 知识点11：幻灯片编号
        configs[PowerPointKnowledgeType.SlideNumber] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SlideNumber,
            Name = "幻灯片编号",
            Description = "设置幻灯片编号的显示",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "ShowSlideNumber", DisplayName = "显示幻灯片编号", Description = "是否显示编号", Type = ParameterType.Boolean, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点12：页脚文字
        configs[PowerPointKnowledgeType.FooterText] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.FooterText,
            Name = "页脚文字",
            Description = "设置幻灯片的页脚文字",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "FooterText", DisplayName = "设置页脚文字", Description = "页脚文字内容", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点13：幻灯片插入图片
        configs[PowerPointKnowledgeType.InsertImage] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertImage,
            Name = "幻灯片插入图片",
            Description = "在幻灯片中插入图片",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "目标幻灯片编号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 }
            ]
        };

        // 知识点14：幻灯片插入表格
        configs[PowerPointKnowledgeType.InsertTable] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertTable,
            Name = "幻灯片插入表格",
            Description = "在幻灯片中插入表格",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片编号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TableRows", DisplayName = "表格行数", Description = "表格行数", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1, MaxValue = 20 },
                new() { Name = "TableColumns", DisplayName = "表格列数", Description = "表格列数", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 1, MaxValue = 20 }
            ]
        };

        // 知识点15：幻灯片插入SmartArt图形
        configs[PowerPointKnowledgeType.InsertSmartArt] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertSmartArt,
            Name = "幻灯片插入SmartArt图形",
            Description = "在幻灯片中插入SmartArt图形",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片编号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "InsertArea", DisplayName = "SmartArt图形插入区域编号", Description = "插入区域", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "主内容区或左内容框,副区域或右侧内容框" },
                new() { Name = "SmartArtLayout", DisplayName = "SmartArt图形的具体布局样式", Description = "具体布局样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "垂直框列表,循环矩阵,射线循环,分离射线,层次结构,组织结构图" }
            ]
        };

        // 知识点16：插入备注
        configs[PowerPointKnowledgeType.InsertNote] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.InsertNote,
            Name = "插入备注",
            Description = "为幻灯片添加备注",
            Category = "幻灯片操作",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片编号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "NoteContent", DisplayName = "备注文本值", Description = "备注文字内容", Type = ParameterType.Text, IsRequired = true, Order = 2 }
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
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片（-1代表任意一张）", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = -1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框（-1代表任意一个）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = -1 },
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
                    EnumOptions = "加粗（Bold）,斜体（Italic）,下划线（Underline）,删除线（Strikethrough）" }
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
                new() { Name = "HorizontalPosition", DisplayName = "水平值（自左上角）", Description = "自左上角的水平位置", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0 },
                new() { Name = "VerticalPosition", DisplayName = "垂直值（自左上角）", Description = "自左上角的垂直位置", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0 }
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

        // 知识点23：艺术字字样
        configs[PowerPointKnowledgeType.SetWordArtStyle] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetWordArtStyle,
            Name = "艺术字字样",
            Description = "设置艺术字的样式",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "WordArtStyle", DisplayName = "艺术字样式", Description = "选择艺术字样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "样式1,样式2,样式3,样式4,样式5,样式6,样式7,样式8,样式9,样式10,样式11,样式12,样式13,样式14,样式15,样式16,样式17,样式18,样式19,样式20" }
            ]
        };

        // 知识点24：艺术字文本效果
        configs[PowerPointKnowledgeType.SetWordArtEffect] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetWordArtEffect,
            Name = "艺术字文本效果",
            Description = "设置艺术字的特殊效果",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TextEffect", DisplayName = "艺术字文本效果", Description = "选择文本变形效果", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "普通文本（无变形）,停止标志样式（方块包围）,正三角形向上,正三角形向下,尖角向上,尖角向下,内环,外环,向上弧形（曲线）,向下弧形（曲线）,完整圆形,圆角矩形曲线,向上弧形（灌注）,向下弧形（灌注）,圆形灌注,圆角矩形灌注,向上轻微弯曲,向下轻微弯曲,阶梯向上,阶梯向下,波浪样式1,波浪样式2,双波浪样式1,双波浪样式2,向外鼓起,向内收缩,底部向外鼓起,底部向内凹陷" }
            ]
        };

        // 知识点25：SmartArt颜色
        configs[PowerPointKnowledgeType.SetSmartArtColor] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetSmartArtColor,
            Name = "SmartArt颜色",
            Description = "设置SmartArt图形的颜色",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "SmartArtColor", DisplayName = "Smart颜色", Description = "选择SmartArt颜色方案", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "彩色范围-强调文字颜色1,彩色范围-强调文字颜色2,彩色范围-强调文字颜色3至4,彩色范围-强调文字颜色5到6,单色填充-灰色,渐变范围-强调文字颜色2,浅色填充-强调文字颜色3,强填充-强调文字颜色4,强边框-强调文字颜色5,彩色线框-强调文字颜色6,渐变线框-强调文字颜色4" }
            ]
        };

        // 知识点26：动画效果-方向
        configs[PowerPointKnowledgeType.SetAnimationDirection] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetAnimationDirection,
            Name = "动画效果-方向",
            Description = "设置动画效果的方向",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ElementOrder", DisplayName = "元素顺序", Description = "第几个元素", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "AnimationDirection", DisplayName = "动画效果", Description = "选择动画方向", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "无方向（默认）,向上,向下,向左,向右,左上角方向,右上角方向,左下角方向,右下角方向,水平方向（不指定左右）,水平向内,水平向外,垂直方向,垂直向内,垂直向外,顺时针,逆时针,从外向内,从内向外,从底部,从顶部,从中心,横穿,从左侧开始,从右侧开始" }
            ]
        };

        // 知识点27：动画样式
        configs[PowerPointKnowledgeType.SetAnimationStyle] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetAnimationStyle,
            Name = "动画样式",
            Description = "设置动画的样式效果",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ElementOrder", DisplayName = "元素顺序", Description = "第几个元素", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "AnimationStyle", DisplayName = "动画样式", Description = "选择动画效果样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "直接出现,飞入,擦除,拆分,随机条纹,向下浮现,旋转淡出,淡出,缩放,旋转,放大/缩小,弹跳,沿圆路径移动,向左移动" }
            ]
        };

        // 知识点28：动画持续时间
        configs[PowerPointKnowledgeType.SetAnimationDuration] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetAnimationDuration,
            Name = "动画持续时间",
            Description = "设置动画效果的持续时间",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ElementOrder", DisplayName = "元素顺序", Description = "第几个元素", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "Duration", DisplayName = "动画持续时间（秒为单位）", Description = "动画持续时间（秒）", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 0.1, MaxValue = 10 },
                new() { Name = "DelayTime", DisplayName = "动画延迟时间（秒为单位）", Description = "动画延迟时间（秒）", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0, MaxValue = 10 }
            ]
        };

        // 知识点29：文本对齐方式
        configs[PowerPointKnowledgeType.SetTextAlignment] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetTextAlignment,
            Name = "文本对齐方式",
            Description = "设置文本的对齐方式",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "TextBoxOrder", DisplayName = "文本框顺序", Description = "第几个文本框", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = 1 },
                new() { Name = "TextAlignment", DisplayName = "对齐方式", Description = "文本对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "左对齐,居中对齐,右对齐,两端对齐（自动调节空格）,均匀分布对齐（常用于中文）" }
            ]
        };

        // 知识点30：动画顺序
        configs[PowerPointKnowledgeType.SetAnimationOrder] = new PowerPointKnowledgeConfig
        {
            KnowledgeType = PowerPointKnowledgeType.SetAnimationOrder,
            Name = "动画顺序",
            Description = "设置多个元素的动画播放顺序",
            Category = "文字与字体设置",
            ParameterTemplates =
            [
                new() { Name = "SlideNumber", DisplayName = "操作目标幻灯片", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "AnimationOrderSettings", DisplayName = "动画顺序设置", Description = "多个元素的动画顺序配置，格式：元素1:顺序1,元素2:顺序2，例如：1:2,2:1,3:3", Type = ParameterType.Text, IsRequired = true, Order = 2 }
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
                new() { Name = "PatternType", DisplayName = "图案类型", Description = "选择图案填充类型", Type = ParameterType.Enum, IsRequired = false, Order = 3,
                    EnumOptions = "5%,10%,15%,20%,25%,30%,35%,40%,45%,50%,55%,60%,65%,70%,75%,80%,85%,90%,浅色下对角线,浅色上对角线,深色下对角线,深色上对角线,宽上对角线,宽下对角线,浅色竖线,浅色横线,深色竖线,深色横线,窄竖线,窄横线,横虚线,竖虚线,上对角虚线,下对角虚线,大纸屑,小纸屑,之字形,波浪线,对角砖形,横向砖形,苏格兰方格呢,编织物,球体,棚架,瓦形,点式菱形,虚线网格,草皮,小网格,大网格,小棋盘,大棋盘,轮廓式菱形,实心菱形",
                    DependsOn = "FillType", DependsOnValue = "图案填充" },
                new() { Name = "TextureType", DisplayName = "纹理类型", Description = "选择纹理类型", Type = ParameterType.Enum, IsRequired = false, Order = 4,
                    EnumOptions = "纸莎草纸,画布,斜纹布,编织物,水滴,纸袋,鱼类化石,沙滩,绿色大理石,白色大理石,褐色大理石,花岗岩,新闻纸,再生纸,羊皮纸,信纸,蓝色面巾纸,粉色面巾纸,紫色网格,花束,软木塞,胡桃,栎木,深色木质",
                    DependsOn = "FillType", DependsOnValue = "纹理填充" },
                new() { Name = "PresetGradientType", DisplayName = "预设渐变类型", Description = "选择预设渐变样式", Type = ParameterType.Enum, IsRequired = false, Order = 5,
                    EnumOptions = "红日西斜,金乌坠地,暮霭沉沉,雨后初晴,极目远眺,漫漫黄沙,碧海青天,心如止水,熊熊火焰,薄雾浓云,茵茵绿原,孔雀开屏,麦浪滚滚,羊皮纸,红木,彩虹出岫,彩虹出岫II,金色年华,金色年华II,铜黄色,铬色,铬色II,银波荡漾,宝石蓝",
                    DependsOn = "FillType", DependsOnValue = "渐变填充" },
                new() { Name = "LinearGradientDirection", DisplayName = "线性渐变方向", Description = "选择线性渐变方向", Type = ParameterType.Enum, IsRequired = false, Order = 6,
                    EnumOptions = "线性对角-左上到右下,线性对角-左下到右上,线性对角-右上到左下,线性对角-右下到左上,线性向上,线性向下,线性向左,线性向右",
                    DependsOn = "FillType", DependsOnValue = "渐变填充" },
                new() { Name = "ApplyToMaster", DisplayName = "是否应用于母版", Description = "是否应用于母版", Type = ParameterType.Boolean, IsRequired = false, Order = 7 }
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
                new() { Name = "Content", DisplayName = "内容", Description = "单元格内容，用逗号分隔。举例：一共六行、二列，内容从1.1 1.2 2.1 2.2 3.1 3.2这个顺序插入", Type = ParameterType.Text, IsRequired = true, Order = 3 }
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
                new() { Name = "TableStyle", DisplayName = "样式名称", Description = "选择表格样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
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
                new() { Name = "SmartArtStyle", DisplayName = "SmartArt样式枚举值（中文）", Description = "选择SmartArt样式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
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
                new() { Name = "SlideNumber", DisplayName = "幻灯片序号", Description = "第几张幻灯片", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 },
                new() { Name = "ObjectIndex", DisplayName = "对象序号", Description = "第几个对象（-1代表任意对象）", Type = ParameterType.Number, IsRequired = true, Order = 2, MinValue = -1 },
                new() { Name = "TriggerMode", DisplayName = "动画触发方式（开始方式）", Description = "控制动画是如何触发的", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "单击时,与上一动画同时,在上一动画之后,自动" },
                new() { Name = "DelayTime", DisplayName = "延迟时间（单位：秒）", Description = "动画开始前的延迟时间", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 0 },
                new() { Name = "Duration", DisplayName = "动画持续时间（单位：秒）", Description = "动画本身执行的时长", Type = ParameterType.Number, IsRequired = true, Order = 5, MinValue = 0.1 },
                new() { Name = "RepeatCount", DisplayName = "重复次数/播放次数", Description = "设置动画播放的循环次数", Type = ParameterType.Number, IsRequired = true, Order = 6, MinValue = 0 }
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
                    EnumOptions = "混合样式（多个对象时的默认值，程序中常用）,非预设样式（无样式或自定义背景）,预设背景样式1（通常为纯白）,预设背景样式2,预设背景样式3,预设背景样式4,预设背景样式5,预设背景样式6,预设背景样式7,预设背景样式8,预设背景样式9,预设背景样式10,预设背景样式11,预设背景样式12（深色底图+浅色字，常用于封面）" }
            ]
        };
    }
}
