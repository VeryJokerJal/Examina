using System;
using System.Collections.Generic;

namespace BenchSuite.Services
{
    /// <summary>
    /// PowerPoint 知识点名称与参数键名的映射与规范化
    /// 依据 ExamLab.Services.PowerPointKnowledgeService.InitializeKnowledgeConfigs 中的配置名称
    /// </summary>
    public static class PowerPointKnowledgeMapping
    {
        private static readonly Dictionary<string, string> NameToType = new(StringComparer.OrdinalIgnoreCase)
        {
            // 幻灯片操作
            ["设置幻灯片版式"] = "SetSlideLayout",
            ["删除幻灯片"] = "DeleteSlide",
            ["插入幻灯片"] = "InsertSlide",
            ["设置幻灯片的字体"] = "SetSlideFont",
            ["幻灯片切换效果"] = "SlideTransitionEffect",
            ["设置幻灯片切换方式"] = "SlideTransitionMode",
            ["幻灯片放映方式"] = "SlideshowMode",
            ["幻灯片放映选项"] = "SlideshowOptions",
            ["幻灯片插入超链接"] = "InsertHyperlink",
            ["幻灯片切换播放声音"] = "SlideTransitionSound",
            ["幻灯片编号"] = "SlideNumber",
            ["页脚文字"] = "FooterText",
            ["幻灯片插入图片"] = "InsertImage",
            ["幻灯片插入表格"] = "InsertTable",
            ["幻灯片插入SmartArt图形"] = "InsertSmartArt",
            ["插入备注"] = "InsertNote",

            // 文字与字体设置
            ["幻灯片插入文本内容"] = "InsertTextContent",
            ["幻灯片插入文本字号"] = "SetTextFontSize",
            ["幻灯片插入文本颜色"] = "SetTextColor",
            ["幻灯片插入文本字形"] = "SetTextStyle",
            ["元素位置"] = "SetElementPosition",
            ["元素高度和宽度设置"] = "SetElementSize",
            ["艺术字字样"] = "SetWordArtStyle",
            ["艺术字文本效果"] = "SetWordArtEffect",
            ["SmartArt颜色"] = "SetSmartArtColor",
            ["动画效果-方向"] = "SetAnimationDirection",
            ["动画样式"] = "SetAnimationStyle",
            ["动画持续时间"] = "SetAnimationDuration",
            ["文本对齐方式"] = "SetTextAlignment",
            ["动画顺序"] = "SetAnimationOrder",

            // 背景样式与设计
            ["设置文稿应用主题"] = "ApplyTheme",
            ["设置幻灯片背景"] = "SetSlideBackground",
            ["设置背景样式"] = "SetSlideBackground", // 映射到已实现的背景检测

            // 其他
            ["单元格内容"] = "SetTableContent",
            ["表格样式"] = "SetTableStyle",
            ["SmartArt样式"] = "SetSmartArtStyle",
            ["SmartArt内容"] = "SetSmartArtContent",
            ["动画计时与延时设置"] = "SetAnimationTiming",
            ["段落行距"] = "SetParagraphSpacing"
        };

        /// <summary>
        /// 将 ExamLab 的中文知识点名称映射为内部使用的类型键名
        /// </summary>
        public static bool TryMapNameToType(string knowledgePointName, out string typeKey)
        {
            if (string.IsNullOrWhiteSpace(knowledgePointName))
            {
                typeKey = string.Empty;
                return false;
            }

            return NameToType.TryGetValue(knowledgePointName.Trim(), out typeKey);
        }

        /// <summary>
        /// 规范化参数键：将 ExamLab 的参数键名转换为本库检测函数所期望的键名
        /// 仅做键名替换，不修改值
        /// </summary>
        public static Dictionary<string, string> NormalizeParameterKeys(string knowledgeType, Dictionary<string, string> parameters)
        {
            Dictionary<string, string> normalized = new(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in parameters)
            {
                string key = kv.Key;
                string value = kv.Value;

                // 通用索引类：Number/Order -> Index
                if (key.Equals("SlideNumber", StringComparison.OrdinalIgnoreCase)) key = "SlideIndex";
                if (key.Equals("SlideNumbers", StringComparison.OrdinalIgnoreCase)) key = "SlideIndexes";
                if (key.Equals("TextBoxNumber", StringComparison.OrdinalIgnoreCase) || key.Equals("TextBoxOrder", StringComparison.OrdinalIgnoreCase)) key = "TextBoxIndex";
                if (key.Equals("ElementNumber", StringComparison.OrdinalIgnoreCase) || key.Equals("ElementOrder", StringComparison.OrdinalIgnoreCase)) key = "ElementIndex";

                // 颜色：ColorValue -> Color
                if (key.Equals("ColorValue", StringComparison.OrdinalIgnoreCase)) key = "Color";

                // 背景样式：BackgroundStyle -> BackgroundType（复用现有检测）
                if (key.Equals("BackgroundStyle", StringComparison.OrdinalIgnoreCase)) key = "BackgroundType";

                // 部分特定知识点的参数名差异修正
                if (knowledgeType.Equals("SlideTransitionEffect", StringComparison.OrdinalIgnoreCase))
                {
                    // 本库检测使用 TransitionType，ExamLab 使用 TransitionEffect
                    if (key.Equals("TransitionEffect", StringComparison.OrdinalIgnoreCase)) key = "TransitionType";
                }

                // 幻灯片切换方式的参数映射
                if (knowledgeType.Equals("SlideTransitionMode", StringComparison.OrdinalIgnoreCase))
                {
                    // 将TransitionScheme和TransitionDirection组合映射为TransitionMode
                    if (key.Equals("TransitionScheme", StringComparison.OrdinalIgnoreCase) || key.Equals("TransitionDirection", StringComparison.OrdinalIgnoreCase))
                    {
                        // 这里可以根据具体的切换方案和方向组合计算TransitionMode值
                        // 暂时保持原键名，在后续处理中进行组合
                    }
                }

                normalized[key] = value;
            }

            return normalized;
        }
    }
}

