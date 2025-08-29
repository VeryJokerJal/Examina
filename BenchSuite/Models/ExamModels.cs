using System.Text.Json.Serialization;

namespace BenchSuite.Models;

/// <summary>
/// 试卷模型 - 统一版本，兼容ExamLab.Models.Exam
/// </summary>
public class ExamModel
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 试卷名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 试卷包含的模块
    /// </summary>
    public List<ExamModuleModel> Modules { get; set; } = [];

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 试卷总分
    /// </summary>
    public double TotalScore { get; set; } = 100.0;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 120;

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake { get; set; } = false;

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount { get; set; } = 0;

    /// <summary>
    /// 及格分数
    /// </summary>
    public double PassingScore { get; set; } = 60.0;

    /// <summary>
    /// 是否随机题目顺序
    /// </summary>
    public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 是否显示分数
    /// </summary>
    public bool ShowScore { get; set; } = true;

    /// <summary>
    /// 是否显示答案
    /// </summary>
    public bool ShowAnswers { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 试卷标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 扩展配置 (JSON格式)
    /// </summary>
    public string? ExtendedConfig { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    public string ExamType { get; set; } = "UnifiedExam";

    /// <summary>
    /// 试卷状态
    /// </summary>
    public string Status { get; set; } = "Draft";
}

/// <summary>
/// 试卷模块模型 - 统一版本，兼容ExamLab
/// </summary>
public class ExamModuleModel
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType Type { get; set; }

    /// <summary>
    /// 模块类型字符串 (ExamLab兼容)
    /// </summary>
    [JsonIgnore]
    public string TypeString
    {
        get => Type.ToString();
        set => Type = Enum.TryParse<ModuleType>(value, true, out ModuleType result) ? result : ModuleType.Windows;
    }

    /// <summary>
    /// 模块描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 模块包含的题目
    /// </summary>
    public List<QuestionModel> Questions { get; set; } = [];

    /// <summary>
    /// 是否启用该模块
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 模块排序
    /// </summary>
    public int Order { get; set; }

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 模块考试时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 模块权重
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// 最低分数要求
    /// </summary>
    public decimal? MinScore { get; set; }

    /// <summary>
    /// 是否必考模块
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 模块配置 (JSON格式)
    /// </summary>
    public string? ModuleConfig { get; set; }

    /// <summary>
    /// 模块类型 (ExamLab格式兼容)
    /// </summary>
    [JsonPropertyName("subjectType")]
    public string? SubjectType { get; set; }

    /// <summary>
    /// 排序顺序 (ExamLab兼容)
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder
    {
        get => Order;
        set => Order = value;
    }
}

/// <summary>
/// 题目模型 - 统一版本，兼容ExamLab
/// </summary>
public class QuestionModel
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目分值（已弃用，使用TotalScore）
    /// </summary>
    [Obsolete("使用TotalScore属性代替")]
    public double Score { get; set; }

    /// <summary>
    /// 题目总分值（基于所有操作点分数的总和）
    /// </summary>
    [Obsolete]
    public double TotalScore
    {
        get
        {
            // 首先检查是否有操作点，如果有操作点则优先使用操作点分数
            // 这适用于Excel、Word、PowerPoint、Windows等模块
            if (OperationPoints != null && OperationPoints.Count > 0)
            {
                return OperationPoints.Where(op => op.IsEnabled).Sum(op => op.Score);
            }

            // 如果没有操作点，则回退到Score字段
            return Score;
        }
    }

    /// <summary>
    /// 题目排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 关联的操作点
    /// </summary>
    public List<OperationPointModel> OperationPoints { get; set; } = [];

    /// <summary>
    /// 是否启用该题目
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 题目类型
    /// </summary>
    public string QuestionType { get; set; } = "Practical";

    /// <summary>
    /// 难度级别 (1-5)
    /// </summary>
    public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; } = 5;

    /// <summary>
    /// 是否必答题
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 标准答案 (JSON格式)
    /// </summary>
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 评分规则 (JSON格式)
    /// </summary>
    public string? ScoringRules { get; set; }

    /// <summary>
    /// 答案验证规则 (JSON格式)
    /// </summary>
    public string? AnswerValidationRules { get; set; }

    /// <summary>
    /// 题目配置 (JSON格式)
    /// </summary>
    public string? QuestionConfig { get; set; }

    /// <summary>
    /// 题目标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 题目备注
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// C#程序参数输入（仅C#模块使用）
    /// </summary>
    public string? ProgramInput { get; set; }

    /// <summary>
    /// C#程序预期控制台输出（仅C#模块使用）
    /// </summary>
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// C#题目类型（仅C#模块使用）
    /// </summary>
    public string? CSharpQuestionType { get; set; }

    /// <summary>
    /// C#代码文件路径（仅C#模块使用）
    /// </summary>
    public string? CodeFilePath { get; set; }

    /// <summary>
    /// C#题目直接分数（仅调试纠错和编写实现类型使用）
    /// </summary>
    public double? CSharpDirectScore { get; set; }

    /// <summary>
    /// 代码补全填空处集合（仅C#模块代码补全类型使用）
    /// </summary>
    public List<CodeBlankModel>? CodeBlanks { get; set; }

    /// <summary>
    /// C#模板代码（仅C#模块代码补全类型使用，包含NotImplementedException的完整代码模板）
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// Office文档文件路径（仅Office模块使用）
    /// </summary>
    public string? DocumentFilePath { get; set; }

    /// <summary>
    /// 排序顺序 (ExamLab兼容)
    /// </summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder
    {
        get => Order;
        set => Order = value;
    }
}

/// <summary>
/// 操作点模型 - 统一版本，兼容ExamLab
/// </summary>
public class OperationPointModel
{
    /// <summary>
    /// 操作点ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 操作点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 操作点描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 模块类型
    /// </summary>
    public ModuleType ModuleType { get; set; } = ModuleType.Windows;

    /// <summary>
    /// PPT知识点类型（当ModuleType为PowerPoint时使用）
    /// </summary>
    public string? PowerPointKnowledgeType { get; set; }

    /// <summary>
    /// Word知识点类型（当ModuleType为Word时使用）
    /// </summary>
    public string? WordKnowledgeType { get; set; }

    /// <summary>
    /// Excel知识点类型（当ModuleType为Excel时使用）
    /// </summary>
    public string? ExcelKnowledgeType { get; set; }

    /// <summary>
    /// 操作点分值
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 配置参数
    /// </summary>
    public List<ConfigurationParameterModel> Parameters { get; set; } = [];

    /// <summary>
    /// 是否启用该操作点
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 操作点排序
    /// </summary>
    public int Order { get; set; }

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 兼容ExamLab的字符串时间格式
    /// </summary>
    [JsonPropertyName("createdTime")]
    public string CreatedTimeString
    {
        get => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        set => CreatedAt = DateTime.TryParse(value, out DateTime result) ? result : DateTime.Now;
    }

    /// <summary>
    /// 操作配置 (JSON格式)
    /// </summary>
    public string? OperationConfig { get; set; }

    /// <summary>
    /// 操作点标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Windows操作类型 (ExamLab兼容)
    /// </summary>
    public string? WindowsOperationType { get; set; }
}

/// <summary>
/// 配置参数模型 - 统一版本，兼容ExamLab
/// </summary>
public class ConfigurationParameterModel
{
    /// <summary>
    /// 参数ID (ExamLab兼容)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    [JsonIgnore]
    public ParameterType Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    // === 新增字段 - 兼容ExamLab ===

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 验证规则 (JSON格式)
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 参数描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 选项列表 (用于枚举和多选类型)
    /// </summary>
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 参数类型字符串 (ExamLab兼容)
    /// </summary>
    [JsonPropertyName("type")]
    public string TypeString
    {
        get => Type.ToString();
        set => Type = Enum.TryParse<ParameterType>(value, true, out ParameterType result) ? result : ParameterType.Text;
    }
}

/// <summary>
/// 模块类型枚举
/// </summary>
public enum ModuleType
{
    Excel = 1,
    Word = 2,
    PowerPoint = 3,
    CSharp = 4,
    Windows = 5
}

/// <summary>
/// 参数类型枚举
/// </summary>
public enum ParameterType
{
    Text,           // 文本
    Number,         // 数字
    Boolean,        // 布尔值
    Enum,           // 枚举
    Color,          // 颜色
    File,           // 文件路径
    Folder,         // 文件夹路径
    Path,           // 路径（可选择文件或文件夹）
    MultipleChoice,  // 多选
    Date
}

/// <summary>
/// Excel知识点类型枚举 - 与ExamLab完全对应的51个操作点
/// </summary>
public enum ExcelKnowledgeType
{
    // 第一类：Excel基础操作（操作点1-42）
    FillOrCopyCellContent = 1,           // 操作点1：填充或复制单元格内容
    DeleteCellContent = 2,               // 操作点2：删除单元格内容
    InsertDeleteCells = 3,               // 操作点3：插入或删除单元格
    MergeCells = 4,                      // 操作点4：合并单元格
    InsertDeleteRows = 5,                // 操作点5：插入或删除行
    SetCellFont = 6,                     // 操作点6：设置指定单元格字体
    SetFontStyle = 7,                    // 操作点7：设置字型
    SetFontSize = 8,                     // 操作点8：设置字号
    SetFontColor = 9,                    // 操作点9：字体颜色
    SetInnerBorderStyle = 10,            // 操作点10：内边框样式
    SetInnerBorderColor = 11,            // 操作点11：内边框颜色
    InsertDeleteColumns = 12,            // 操作点12：插入或删除列
    SetHorizontalAlignment = 13,         // 操作点13：设置单元格区域水平对齐方式
    SetNumberFormat = 14,                // 操作点14：设置目标区域单元格数字分类格式
    UseFunction = 15,                    // 操作点15：使用函数
    SetRowHeight = 16,                   // 操作点16：设置行高
    SetColumnWidth = 17,                 // 操作点17：设置列宽
    AutoFitRowHeight = 18,               // 操作点18：自动调整行高
    AutoFitColumnWidth = 19,             // 操作点19：自动调整列宽
    SetCellFillColor = 20,               // 操作点20：设置单元格填充颜色
    SetPatternFillStyle = 21,            // 操作点21：设置图案填充样式
    SetPatternFillColor = 22,            // 操作点22：设置填充图案颜色
    WrapText = 23,                       // 操作点23：文字换行
    SetOuterBorderStyle = 24,            // 操作点24：设置外边框样式
    SetOuterBorderColor = 25,            // 操作点25：设置外边框颜色
    SetVerticalAlignment = 26,           // 操作点26：设置垂直对齐方式
    FreezePane = 27,                     // 操作点27：冻结窗格
    ModifySheetName = 28,                // 操作点28：修改sheet表名称
    AddUnderline = 29,                   // 操作点29：添加下划线
    SetBoldItalic = 30,                  // 操作点30：设置粗体斜体
    SetStrikethrough = 31,               // 操作点31：设置删除线
    SetSuperscriptSubscript = 32,        // 操作点32：设置上标下标
    ConditionalFormat = 33,              // 操作点33：条件格式
    DataValidation = 34,                 // 操作点34：数据验证
    ProtectWorksheet = 35,               // 操作点35：保护工作表
    SetCellComment = 36,                 // 操作点36：设置单元格批注
    HyperlinkInsert = 37,                // 操作点37：插入超链接
    FindReplace = 38,                    // 操作点38：查找替换
    CopyPasteSpecial = 39,               // 操作点39：选择性粘贴
    AutoSum = 40,                        // 操作点40：自动求和
    GoToSpecial = 41,                    // 操作点41：定位特殊单元格
    SetCellStyleData = 42,               // 操作点42：设置单元格样式——数据

    // 第二类：数据清单操作（操作点31-35, 71）
    Filter = 31,                         // 操作点31：筛选
    Sort = 32,                           // 操作点32：排序
    Subtotal = 35,                       // 操作点35：分类汇总
    AdvancedFilterCondition = 36,        // 操作点36：高级筛选-条件
    AdvancedFilterData = 63,             // 操作点63：高级筛选-数据
    PivotTable = 71,                     // 操作点71：数据透视表

    // 第三类：图表操作（操作点101-160）
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
/// 代码填空处模型 - 兼容ExamLab
/// </summary>
public class CodeBlankModel
{
    /// <summary>
    /// 填空处ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 填空处名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 填空处描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 填空处分数
    /// </summary>
    public double Score { get; set; } = 1.0;

    /// <summary>
    /// 填空处顺序
    /// </summary>
    public int Order { get; set; } = 1;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 标准答案
    /// </summary>
    public string? StandardAnswer { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public string CreatedTime { get; set; } = string.Empty;
}
