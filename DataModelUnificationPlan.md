# 数据模型统一方案

## 概述

本文档定义了BenchSuite和ExamLab项目间数据模型的统一方案，确保两个项目能够无缝交换数据。

## 统一原则

1. **向后兼容**: 现有的BenchSuite ExamModel保持兼容
2. **渐进增强**: 逐步添加ExamLab的高级功能字段
3. **类型安全**: 统一数据类型，避免转换错误
4. **可扩展性**: 支持未来功能扩展

## 统一数据模型设计

### 1. 试卷模型 (ExamModel)

#### 现有字段保持不变
```csharp
public class ExamModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ExamModuleModel> Modules { get; set; } = [];
}
```

#### 新增字段 (兼容ExamLab)
```csharp
// 考试管理字段
public decimal TotalScore { get; set; } = 100.0m;
public int DurationMinutes { get; set; } = 120;
public DateTime? StartTime { get; set; }
public DateTime? EndTime { get; set; }

// 考试配置
public bool AllowRetake { get; set; } = false;
public int MaxRetakeCount { get; set; } = 0;
public decimal PassingScore { get; set; } = 60.0m;
public bool RandomizeQuestions { get; set; } = false;
public bool ShowScore { get; set; } = true;
public bool ShowAnswers { get; set; } = false;

// 元数据
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedAt { get; set; }
public DateTime? PublishedAt { get; set; }
public bool IsEnabled { get; set; } = true;
public string? Tags { get; set; }

// 扩展配置 (JSON格式)
public string? ExtendedConfig { get; set; }
```

### 2. 模块模型 (ExamModuleModel)

#### 类型统一
```csharp
// 统一模块类型为枚举，但支持字符串转换
public ModuleType Type { get; set; }

// 新增字符串类型支持 (用于ExamLab兼容)
[JsonIgnore]
public string TypeString 
{
    get => Type.ToString();
    set => Type = Enum.TryParse<ModuleType>(value, true, out var result) ? result : ModuleType.Windows;
}
```

#### 新增字段
```csharp
// 时间管理
public int DurationMinutes { get; set; } = 30;
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedAt { get; set; }

// 权重和要求
public decimal Weight { get; set; } = 1.0m;
public decimal? MinScore { get; set; }
public bool IsRequired { get; set; } = true;

// 配置扩展
public string? ModuleConfig { get; set; }
```

### 3. 题目模型 (QuestionModel)

#### 新增字段
```csharp
// 题目分类
public string QuestionType { get; set; } = "Practical";
public int DifficultyLevel { get; set; } = 1;
public int EstimatedMinutes { get; set; } = 5;

// 答案和评分
public string? StandardAnswer { get; set; }
public string? ScoringRules { get; set; }
public string? AnswerValidationRules { get; set; }

// 配置和元数据
public string? QuestionConfig { get; set; }
public string? Tags { get; set; }
public string? Remarks { get; set; }
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedAt { get; set; }

// C#特有字段
public string? ProgramInput { get; set; }
public string? ExpectedOutput { get; set; }
```

### 4. 操作点模型 (OperationPointModel)

#### 新增字段
```csharp
// 时间管理
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedAt { get; set; }

// 兼容ExamLab的字符串时间格式
[JsonPropertyName("createdTime")]
public string CreatedTimeString 
{
    get => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    set => CreatedAt = DateTime.TryParse(value, out var result) ? result : DateTime.UtcNow;
}

// 配置扩展
public string? OperationConfig { get; set; }
public string? Tags { get; set; }
```

### 5. 参数模型 (ConfigurationParameterModel)

#### 新增字段
```csharp
// 参数ID (ExamLab兼容)
public string Id { get; set; } = string.Empty;

// 参数配置
public string? DefaultValue { get; set; }
public string? ValidationRules { get; set; }
public string? Description { get; set; }

// 选项支持 (用于枚举和多选类型)
public List<string> Options { get; set; } = [];

// 显示配置
public int Order { get; set; }
public bool IsVisible { get; set; } = true;
```

## 数据转换策略

### 1. ExamLab → BenchSuite 转换

```csharp
public static class ExamLabToBenchSuiteConverter
{
    public static ExamModel Convert(ExamExportDto exportDto)
    {
        var examModel = new ExamModel
        {
            Id = exportDto.Exam.Id,
            Name = exportDto.Exam.Name,
            Description = exportDto.Exam.Description ?? string.Empty,
            
            // 新增字段映射
            TotalScore = exportDto.Exam.TotalScore,
            DurationMinutes = exportDto.Exam.DurationMinutes,
            StartTime = exportDto.Exam.StartTime,
            EndTime = exportDto.Exam.EndTime,
            AllowRetake = exportDto.Exam.AllowRetake,
            MaxRetakeCount = exportDto.Exam.MaxRetakeCount,
            PassingScore = exportDto.Exam.PassingScore,
            RandomizeQuestions = exportDto.Exam.RandomizeQuestions,
            ShowScore = exportDto.Exam.ShowScore,
            ShowAnswers = exportDto.Exam.ShowAnswers,
            CreatedAt = exportDto.Exam.CreatedAt,
            UpdatedAt = exportDto.Exam.UpdatedAt,
            PublishedAt = exportDto.Exam.PublishedAt,
            IsEnabled = exportDto.Exam.IsEnabled,
            Tags = exportDto.Exam.Tags,
            ExtendedConfig = JsonSerializer.Serialize(exportDto.Exam.ExtendedConfig)
        };

        // 转换模块 (优先使用Modules，回退到Subjects)
        if (exportDto.Exam.Modules.Any())
        {
            examModel.Modules = exportDto.Exam.Modules.Select(ConvertModule).ToList();
        }
        else if (exportDto.Exam.Subjects.Any())
        {
            examModel.Modules = exportDto.Exam.Subjects.Select(ConvertSubjectToModule).ToList();
        }

        return examModel;
    }
}
```

### 2. BenchSuite → ExamLab 转换

```csharp
public static class BenchSuiteToExamLabConverter
{
    public static ExamExportDto Convert(ExamModel examModel, ExportLevel exportLevel = ExportLevel.Complete)
    {
        var examDto = new ExamDto
        {
            Id = examModel.Id,
            Name = examModel.Name,
            Description = examModel.Description,
            
            // 映射新增字段
            TotalScore = examModel.TotalScore,
            DurationMinutes = examModel.DurationMinutes,
            StartTime = examModel.StartTime,
            EndTime = examModel.EndTime,
            // ... 其他字段映射
            
            Modules = examModel.Modules.Select(ConvertModule).ToList()
        };

        // 根据导出级别过滤敏感信息
        if (exportLevel == ExportLevel.BasicInfo)
        {
            // 移除题目详细信息
            foreach (var module in examDto.Modules)
            {
                module.Questions.Clear();
            }
        }
        else if (exportLevel == ExportLevel.WithoutAnswers)
        {
            // 移除答案信息
            foreach (var module in examDto.Modules)
            {
                foreach (var question in module.Questions)
                {
                    question.StandardAnswer = null;
                    question.ScoringRules = null;
                    question.AnswerValidationRules = null;
                }
            }
        }

        return new ExamExportDto
        {
            Exam = examDto,
            Metadata = GenerateMetadata(examModel, exportLevel)
        };
    }
}
```

## 实现计划

### 阶段1: 扩展BenchSuite模型
1. 更新ExamModel类，添加新字段
2. 保持向后兼容性
3. 添加JSON序列化特性

### 阶段2: 创建转换器
1. 实现双向数据转换器
2. 处理类型转换和默认值
3. 支持不同导出级别

### 阶段3: 集成到应用
1. 更新BenchSuite.Console支持ExamLab格式
2. 在ExamLab中集成BenchSuite格式导出
3. 添加格式自动检测

### 阶段4: 测试和验证
1. 创建测试用例
2. 验证数据完整性
3. 性能测试

## 兼容性保证

1. **现有代码**: 不影响现有BenchSuite代码
2. **JSON格式**: 保持现有JSON格式兼容
3. **默认值**: 新字段提供合理默认值
4. **可选字段**: 新字段标记为可选，避免破坏性变更

## 扩展性考虑

1. **版本控制**: 支持模型版本标识
2. **插件架构**: 支持自定义字段扩展
3. **配置驱动**: 通过配置控制字段映射
4. **国际化**: 支持多语言字段名称
