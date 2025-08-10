using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;

namespace ExamLab.Models;

/// <summary>
/// Windows操作类型枚举
/// </summary>
public enum WindowsOperationType
{
    QuickCreate,        // 快捷创建
    CreateOperation,    // 创建操作
    DeleteOperation,    // 删除操作
    CopyOperation,      // 复制操作
    MoveOperation,      // 移动操作
    RenameOperation,    // 重命名操作
    ShortcutOperation,  // 快捷方式操作
    FilePropertyModification, // 文件属性修改操作
    CopyRenameOperation // 复制重命名操作
}

/// <summary>
/// PPT知识点类型枚举
/// </summary>
public enum PowerPointKnowledgeType
{
    // 第一类：幻灯片操作
    SetSlideLayout = 1,           // 设置幻灯片版式
    DeleteSlide = 2,              // 删除幻灯片
    InsertSlide = 3,              // 插入幻灯片
    SetSlideFont = 4,             // 设置幻灯片的字体
    SlideTransitionEffect = 5,    // 幻灯片切换效果
    SlideTransitionMode = 6,      // 设置幻灯片切换方式
    SlideshowMode = 7,            // 幻灯片放映方式
    SlideshowOptions = 8,         // 幻灯片放映选项
    InsertHyperlink = 9,          // 幻灯片插入超链接
    SlideTransitionSound = 10,    // 幻灯片切换播放声音
    SlideNumber = 11,             // 幻灯片编号
    FooterText = 12,              // 页脚文字
    InsertImage = 13,             // 幻灯片插入图片
    InsertTable = 14,             // 幻灯片插入表格
    InsertSmartArt = 15,          // 幻灯片插入SmartArt图形
    InsertNote = 16,              // 插入备注

    // 第二类：文字与字体设置
    InsertTextContent = 17,       // 幻灯片插入文本内容
    SetTextFontSize = 18,         // 幻灯片插入文本字号
    SetTextColor = 19,            // 幻灯片插入文本颜色
    SetTextStyle = 20,            // 幻灯片插入文本字形
    SetElementPosition = 21,      // 元素位置
    SetElementSize = 22,          // 元素高度和宽度设置
    SetWordArtStyle = 23,         // 艺术字字样
    SetWordArtEffect = 24,        // 艺术字文本效果
    SetSmartArtColor = 25,        // SmartArt颜色
    SetAnimationDirection = 26,   // 动画效果-方向
    SetAnimationStyle = 27,       // 动画样式
    SetAnimationDuration = 28,    // 动画持续时间
    SetTextAlignment = 29,        // 文本对齐方式
    SetAnimationOrder = 30,       // 动画顺序

    // 第三类：背景样式与设计
    ApplyTheme = 31,              // 设置文稿应用主题

    // 第四类：母版与主题设置
    SetSlideBackground = 32,      // 设置幻灯片背景

    // 第五类：其他
    SetTableContent = 33,         // 单元格内容
    SetTableStyle = 34,           // 表格样式
    SetSmartArtStyle = 35,        // SmartArt样式
    SetSmartArtContent = 36,      // SmartArt内容
    SetAnimationTiming = 37,      // 动画计时与延时设置
    SetParagraphSpacing = 38,     // 段落行距
    SetBackgroundStyle = 39       // 设置背景样式
}

/// <summary>
/// 操作点模型
/// </summary>
public class OperationPoint : ReactiveObject
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    [Reactive] public string Id { get; set; } = "operation-1";

    /// <summary>
    /// 操作点名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    [Reactive] public ModuleType ModuleType { get; set; }

    /// <summary>
    /// Windows操作类型（当ModuleType为Windows时使用）
    /// </summary>
    [Reactive] public WindowsOperationType? WindowsOperationType { get; set; }

    /// <summary>
    /// PPT知识点类型（当ModuleType为PowerPoint时使用）
    /// </summary>
    [Reactive] public PowerPointKnowledgeType? PowerPointKnowledgeType { get; set; }

    /// <summary>
    /// 操作点分值
    /// </summary>
    [Reactive] public int Score { get; set; }

    /// <summary>
    /// 关联的评分题目ID
    /// </summary>
    [Reactive] public string? ScoringQuestionId { get; set; }

    /// <summary>
    /// 配置参数
    /// </summary>
    public ObservableCollection<ConfigurationParameter> Parameters { get; set; } = new();

    /// <summary>
    /// 是否启用该操作点
    /// </summary>
    [Reactive] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 操作点排序
    /// </summary>
    [Reactive] public int Order { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public string CreatedTime { get; set; } = "2025-08-10";
}
