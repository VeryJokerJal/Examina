using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.ObjectModel;
using ExamLab.Services;

namespace ExamLab.Models;

/// <summary>
/// Windows操作类型枚举
/// 注意：已删除CreateOperation，因为它与QuickCreate功能重复
/// </summary>
public enum WindowsOperationType
{
    QuickCreate,        // 快捷创建（包含完整的创建路径参数）
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
    SetSlideLayout = 1,           // 知识点1：设置幻灯片版式
    DeleteSlide = 2,              // 知识点2：删除幻灯片
    InsertSlide = 3,              // 知识点3：插入幻灯片
    SetSlideFont = 4,             // 知识点4：设置幻灯片的字体
    SlideTransitionEffect = 5,    // 知识点5：幻灯片切换效果
    SlideTransitionMode = 6,      // 知识点6：设置幻灯片切换方式
    SlideshowMode = 7,            // 知识点7：幻灯片放映方式
    SlideshowOptions = 8,         // 知识点8：幻灯片放映选项
    InsertHyperlink = 9,          // 知识点9：幻灯片插入超链接
    SlideTransitionSound = 10,    // 知识点10：幻灯片切换播放声音
    SlideNumber = 11,             // 知识点11：幻灯片编号
    FooterText = 12,              // 知识点12：页脚文字
    InsertImage = 13,             // 知识点13：幻灯片插入图片
    InsertTable = 14,             // 知识点14：幻灯片插入表格
    InsertSmartArt = 15,          // 知识点15：幻灯片插入SmartArt图形
    InsertNote = 16,              // 知识点16：插入备注

    // 第二类：文字与字体设置
    InsertTextContent = 17,       // 知识点17：幻灯片插入文本内容
    SetTextFontSize = 18,         // 知识点18：幻灯片插入文本字号
    SetTextColor = 19,            // 知识点19：幻灯片插入文本颜色
    SetTextStyle = 20,            // 知识点20：幻灯片插入文本字形
    SetElementPosition = 21,      // 知识点21：元素位置
    SetElementSize = 22,          // 知识点22：元素高度和宽度设置
    SetWordArtStyle = 23,         // 知识点23：艺术字字样
    SetWordArtEffect = 24,        // 知识点24：艺术字文本效果
    SetSmartArtColor = 25,        // 知识点25：SmartArt颜色
    SetAnimationDirection = 26,   // 知识点26：动画效果-方向
    SetAnimationStyle = 27,       // 知识点27：动画样式
    SetAnimationDuration = 28,    // 知识点28：动画持续时间
    SetTextAlignment = 29,        // 知识点29：文本对齐方式
    SetAnimationOrder = 30,       // 知识点30：动画顺序

    // 第三类：背景样式与设计
    ApplyTheme = 31,              // 知识点31：设置文稿应用主题

    // 第四类：母版与主题设置
    SetSlideBackground = 32,      // 知识点32：设置幻灯片背景

    // 第五类：其他
    SetTableContent = 33,         // 知识点33：单元格内容
    SetTableStyle = 34,           // 知识点34：表格样式
    SetSmartArtStyle = 35,        // 知识点35：SmartArt样式
    SetSmartArtContent = 36,      // 知识点36：SmartArt内容
    SetAnimationTiming = 37,      // 知识点37：动画计时与延时设置
    SetParagraphSpacing = 38,     // 知识点38：段落行距
    SetBackgroundStyle = 39       // 知识点39：设置背景样式
}

/// <summary>
/// Word知识点类型枚举
/// </summary>
public enum WordKnowledgeType
{
    // 第一类：段落操作（共14项）
    // （一）段落文字样式（字体、字号、字形、颜色等）
    SetParagraphFont = 1,                    // 知识点1：设置段落的字体
    SetParagraphFontSize = 2,                // 知识点2：设置段落的字号
    SetParagraphFontStyle = 3,               // 知识点3：设置段落的字形
    SetParagraphCharacterSpacing = 4,        // 知识点4：设置段落字间距
    SetParagraphTextColor = 5,               // 知识点5：设置段落文字的颜色

    // （二）段落格式设置（文字位置与布局）
    SetParagraphAlignment = 6,               // 知识点6：设置段落对齐方式
    SetParagraphIndentation = 7,             // 知识点7：设置段落缩进
    SetParagraphLineSpacing = 8,             // 知识点8：设置行间距
    SetParagraphDropCap = 9,                 // 知识点9：首字下沉

    // （三）段落间距与边框
    SetParagraphSpacing = 10,                // 知识点10：设置段落间距
    SetParagraphBorderColor = 11,            // 知识点11：设置段落边框颜色
    SetParagraphBorderStyle = 12,            // 知识点12：设置段落边框线型
    SetParagraphBorderWidth = 13,            // 知识点13：设置段落边框线宽

    // （四）段落背景设置
    SetParagraphShading = 14,                // 知识点14：设置段落底纹

    // 第二类：页面设置（共15项）
    // （一）页面尺寸与边距设置
    SetPaperSize = 15,                       // 知识点15：设置纸张大小
    SetPageMargins = 16,                     // 知识点16：设置页边距

    // （二）页眉设置
    SetHeaderText = 17,                      // 知识点17：设置页眉中的文字
    SetHeaderFont = 18,                      // 知识点18：设置页眉中文字的字体
    SetHeaderFontSize = 19,                  // 知识点19：设置页眉中文字的字号
    SetHeaderAlignment = 20,                 // 知识点20：设置页眉中文字的对齐方式

    // （三）页脚设置
    SetFooterText = 21,                      // 知识点21：设置页脚中的文字
    SetFooterFont = 22,                      // 知识点22：设置页脚中文字的字体
    SetFooterFontSize = 23,                  // 知识点23：设置页脚中文字的字号
    SetFooterAlignment = 24,                 // 知识点24：设置页脚中文字的对齐方式

    // （四）页码与背景设置
    SetPageNumber = 25,                      // 知识点25：设置页码
    SetPageBackground = 26,                  // 知识点26：设置页面背景

    // （五）页面边框设置
    SetPageBorderColor = 27,                 // 知识点27：设置页面边框颜色
    SetPageBorderStyle = 28,                 // 知识点28：设置页面边框线型
    SetPageBorderWidth = 29,                 // 知识点29：设置页面边框线宽

    // 第三类：水印设置（共4项）
    SetWatermarkText = 30,                   // 知识点30：设置水印文字
    SetWatermarkFont = 31,                   // 知识点31：设置水印文字的字体
    SetWatermarkFontSize = 32,               // 知识点32：设置水印文字的字号
    SetWatermarkOrientation = 33,            // 知识点33：设置水印文字水平或倾斜方式

    // 第四类：项目符号与编号（共1项）
    SetBulletNumbering = 34,                 // 知识点34：设置项目编号

    // 第五类：表格操作（共10项）
    SetTableRowsColumns = 35,                // 知识点35：设置表格的行数和列数
    SetTableShading = 36,                    // 知识点36：设置表格底纹
    SetTableRowHeight = 37,                  // 知识点37：设置表格行高
    SetTableColumnWidth = 38,                // 知识点38：设置表格列宽
    SetTableCellContent = 39,                // 知识点39：设置单元格内容
    SetTableCellAlignment = 40,              // 知识点40：设置表格单元格对齐方式
    SetTableAlignment = 41,                  // 知识点41：设置整个表格对齐方式
    MergeTableCells = 42,                    // 知识点42：合并单元格
    SetTableHeaderContent = 43,              // 知识点43：设置表头第一个单元格的内容
    SetTableHeaderAlignment = 44,            // 知识点44：设置表头第一个单元格的对齐方式

    // 第六类：图形和图片设置（共16项）
    // （一）自选图形插入与样式设置
    InsertAutoShape = 45,                    // 知识点45：插入自选图形类型
    SetAutoShapeSize = 46,                   // 知识点46：设置自选图形大小
    SetAutoShapeLineColor = 47,              // 知识点47：设置自选图形线条颜色
    SetAutoShapeFillColor = 48,              // 知识点48：设置自选图形填充颜色

    // （二）自选图形文字设置
    SetAutoShapeTextSize = 49,               // 知识点49：设置自选图形中文字大小
    SetAutoShapeTextColor = 50,              // 知识点50：设置自选图形中文字颜色
    SetAutoShapeTextContent = 51,            // 知识点51：设置自选图形中文字内容

    // （三）自选图形位置设置
    SetAutoShapePosition = 52,               // 知识点52：设置自选图形的位置

    // （四）插入图片样式设置
    SetImageBorderCompoundType = 53,         // 知识点53：设置插入图片边框复合类型
    SetImageBorderDashType = 54,             // 知识点54：设置插入图片边框短划线类型
    SetImageBorderWidth = 55,                // 知识点55：设置插入图片边框线宽
    SetImageBorderColor = 56,                // 知识点56：设置插入图片边框颜色
    SetImageShadow = 57,                     // 知识点57：设置插入图片阴影类型与颜色
    SetImageWrapStyle = 58,                  // 知识点58：设置插入图片环绕方式

    // （五）插入图片尺寸与位置设置
    SetImageSize = 59,                       // 知识点59：设置插入图片的高度和宽度
    SetImagePosition = 60,                   // 知识点60：设置插入图片的位置

    // 第七类：文本框设置（共5项）
    SetTextBoxBorderColor = 61,              // 知识点61：设置文本框边框颜色
    SetTextBoxContent = 62,                  // 知识点62：设置文本框中文字内容
    SetTextBoxTextSize = 63,                 // 知识点63：设置文本框中文字大小
    SetTextBoxPosition = 64,                 // 知识点64：设置文本框位置
    SetTextBoxWrapStyle = 65,                // 知识点65：设置文本框环绕方式

    // 第八类：其他操作（共2项）
    FindAndReplace = 66,                     // 知识点66：查找与替换
    SetSpecificTextFontSize = 67             // 知识点67：设置指定文字字号
}

/// <summary>
/// Excel知识点类型枚举
/// </summary>
public enum ExcelKnowledgeType
{
    // 第一类：Excel基础操作（操作点1-23）
    FillOrCopyCellContent = 1,           // 操作点1：填充或复制单元格内容
    MergeCells = 4,                      // 操作点4：合并单元格
    SetCellFont = 6,                     // 操作点6：设置指定单元格字体
    SetInnerBorderStyle = 10,            // 操作点10：内边框样式
    SetInnerBorderColor = 11,            // 操作点11：内边框颜色
    SetHorizontalAlignment = 13,         // 操作点13：设置单元格区域水平对齐方式
    SetNumberFormat = 14,                // 操作点14：设置目标区域单元格数字分类格式
    UseFunction = 15,                    // 操作点15：使用函数
    SetRowHeight = 16,                   // 操作点16：设置行高
    SetColumnWidth = 17,                 // 操作点17：设置列宽
    SetCellFillColor = 20,               // 操作点20：设置单元格填充颜色
    SetOuterBorderStyle = 24,            // 操作点24：设置外边框样式
    SetOuterBorderColor = 25,            // 操作点25：设置外边框颜色
    SetVerticalAlignment = 26,           // 操作点26：设置垂直对齐方式
    ModifySheetName = 28,                // 操作点28：修改sheet表名称
    AddUnderline = 29,                   // 操作点29：添加下划线
    SetFontStyle = 7,                    // 操作点7：设置字型
    SetFontSize = 8,                     // 操作点8：设置字号
    SetFontColor = 9,                    // 操作点9：字体颜色
    SetPatternFillStyle = 21,            // 操作点21：设置图案填充样式
    SetPatternFillColor = 22,            // 操作点22：设置填充图案颜色
    ConditionalFormat = 33,              // 操作点33：条件格式
    SetCellStyleData = 83,               // 操作点83：设置单元格样式——数据

    // 第二类：数据清单操作（操作点24-29）
    Filter = 31,                         // 操作点31：筛选
    Sort = 32,                           // 操作点32：排序
    Subtotal = 35,                       // 操作点35：分类汇总
    AdvancedFilterCondition = 36,        // 操作点36：高级筛选-条件
    AdvancedFilterData = 63,             // 操作点63：高级筛选-数据
    PivotTable = 71,                     // 操作点71：数据透视表

    // 第三类：图表操作（操作点30-51）
    ChartType = 101,                     // 操作点101：图表类型
    ChartStyle = 102,                    // 操作点102：图表样式
    ChartMove = 103,                     // 操作点103：图表移动
    CategoryAxisDataRange = 104,         // 操作点104：分类轴数据区域
    ValueAxisDataRange = 105,            // 操作点105：数值轴数据区域
    ChartTitle = 107,                    // 操作点107：图表标题
    ChartTitleFormat = 108,              // 操作点108：图表标题格式
    HorizontalAxisTitle = 112,           // 操作点112：主要横坐标轴标题
    HorizontalAxisTitleFormat = 113,     // 操作点113：主要横坐标轴标题格式
    LegendPosition = 122,                // 操作点122：设置图例位置
    LegendFormat = 123,                  // 操作点123：设置图例格式
    VerticalAxisOptions = 139,           // 操作点139：设置主要纵坐标轴选项
    MajorHorizontalGridlines = 140,      // 操作点140：设置网格线——主要横网格线
    MinorHorizontalGridlines = 141,      // 操作点141：设置网格线——次要横网格线
    MajorVerticalGridlines = 142,        // 操作点142：主要纵网格线
    MinorVerticalGridlines = 143,        // 操作点143：次要纵网格线
    DataSeriesFormat = 145,              // 操作点145：设置数据系列格式
    AddDataLabels = 154,                 // 操作点154：添加数据标签
    DataLabelsFormat = 155,              // 操作点155：设置数据标签格式
    ChartAreaFormat = 156,               // 操作点156：设置图表区域格式
    ChartFloorColor = 159,               // 操作点159：显示图表基底颜色
    ChartBorder = 160                    // 操作点160：设置图表边框线
}

/// <summary>
/// 操作点模型
/// </summary>
public class OperationPoint : ReactiveObject
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    [Reactive] public string Id { get; set; } = IdGeneratorService.GenerateOperationId();

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
    [Reactive] public ModuleType ModuleType { get; set; } = ModuleType.Windows;

    /// <summary>
    /// Windows操作类型（当ModuleType为Windows时使用）
    /// </summary>
    [Reactive] public WindowsOperationType? WindowsOperationType { get; set; }

    /// <summary>
    /// PPT知识点类型（当ModuleType为PowerPoint时使用）
    /// </summary>
    [Reactive] public PowerPointKnowledgeType? PowerPointKnowledgeType { get; set; }

    /// <summary>
    /// Word知识点类型（当ModuleType为Word时使用）
    /// </summary>
    [Reactive] public WordKnowledgeType? WordKnowledgeType { get; set; }

    /// <summary>
    /// Excel知识点类型（当ModuleType为Excel时使用）
    /// </summary>
    [Reactive] public ExcelKnowledgeType? ExcelKnowledgeType { get; set; }

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
