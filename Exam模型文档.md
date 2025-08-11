# Exam模型文档

## 概述

Exam模型是考试系统的核心实体，用于管理试卷的基本信息、配置和关联关系。系统中存在两个版本的Exam模型：

1. **Web应用版本** (`ExaminaWebApplication`) - 用于数据库存储的完整实体模型
2. **ExamLab版本** (`ExamLab`) - 用于桌面应用的响应式UI模型

## 数据结构定义

### 1. Web应用版本 (ExaminaWebApplication.Models.Exam.Exam)

#### 基本属性

| 字段名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| Id | int | ✓ | - | 主键ID |
| Name | string(200) | ✓ | "" | 试卷名称 |
| Description | string(1000) | ✗ | null | 试卷描述 |
| ExamType | ExamType | ✓ | UnifiedExam | 试卷类型 |
| Status | ExamStatus | ✓ | Draft | 试卷状态 |
| TotalScore | decimal(6,2) | ✓ | 100.0 | 总分 |
| DurationMinutes | int | ✓ | 120 | 考试时长（分钟） |
| StartTime | DateTime? | ✗ | null | 考试开始时间 |
| EndTime | DateTime? | ✗ | null | 考试结束时间 |
| AllowRetake | bool | ✗ | false | 是否允许重考 |
| MaxRetakeCount | int | ✗ | 0 | 最大重考次数 |
| PassingScore | decimal(6,2) | ✗ | 60.0 | 及格分数 |
| RandomizeQuestions | bool | ✗ | false | 是否随机题目顺序 |
| ShowScore | bool | ✗ | true | 是否显示分数 |
| ShowAnswers | bool | ✗ | false | 是否显示答案 |
| CreatedBy | int | ✓ | - | 创建者ID |
| CreatedAt | DateTime | ✓ | UtcNow | 创建时间 |
| UpdatedAt | DateTime? | ✗ | null | 更新时间 |
| PublishedAt | DateTime? | ✗ | null | 发布时间 |
| PublishedBy | int? | ✗ | null | 发布者ID |
| IsEnabled | bool | ✗ | true | 是否启用 |
| Tags | string(500) | ✗ | null | 试卷标签 |
| ExtendedConfig | string(json) | ✗ | null | 扩展配置 |

#### 导航属性

| 属性名 | 类型 | 关系 | 说明 |
|--------|------|------|------|
| Creator | User | 多对一 | 创建者用户 |
| Publisher | User? | 多对一 | 发布者用户 |
| Subjects | ICollection&lt;ExamSubject&gt; | 一对多 | 试卷科目列表 |
| Questions | ICollection&lt;ExamQuestion&gt; | 一对多 | 试卷题目列表 |
| ExcelOperationPoints | ICollection&lt;ExamExcelOperationPoint&gt; | 一对多 | Excel操作点列表 |

### 2. ExamLab版本 (ExamLab.Models.Exam)

#### 基本属性

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| Id | string | "exam-1" | 试卷ID |
| Name | string | "" | 试卷名称 |
| Description | string | "" | 试卷描述 |
| CreatedTime | string | "2025-08-10" | 创建时间 |
| LastModifiedTime | string | "2025-08-10" | 最后修改时间 |
| Modules | ObservableCollection&lt;ExamModule&gt; | new() | 试卷包含的模块 |

*注：所有属性都使用 `[Reactive]` 特性，继承自 `ReactiveObject`*

## 枚举类型定义

### ExamType (试卷类型)
```csharp
public enum ExamType
{
    UnifiedExam = 1,    // 统一考试
    MockExam = 2        // 模拟考试
}
```

### ExamStatus (试卷状态)
```csharp
public enum ExamStatus
{
    Draft = 1,          // 草稿状态
    UnderReview = 2,    // 审核中
    Published = 3,      // 已发布
    InProgress = 4,     // 进行中
    Completed = 5,      // 已结束
    Suspended = 6,      // 已暂停
    Cancelled = 7,      // 已取消
    Archived = 8        // 已归档
}
```

### SubjectType (科目类型)
```csharp
public enum SubjectType
{
    Excel = 1,          // Excel科目
    PowerPoint = 2,     // PowerPoint科目
    Word = 3,           // Word科目
    Windows = 4,        // Windows科目
    CSharp = 5,         // C#科目
    Comprehensive = 6   // 综合科目
}
```

### QuestionType (题目类型)
```csharp
public enum QuestionType
{
    ExcelOperation = 1,         // Excel操作题
    PowerPointOperation = 2,    // PowerPoint操作题
    WordOperation = 3,          // Word操作题
    WindowsOperation = 4,       // Windows操作题
    CSharpProgramming = 5,      // C#编程题
    MultipleChoice = 6,         // 选择题
    FillInBlank = 7,           // 填空题
    ShortAnswer = 8,           // 简答题
    Comprehensive = 9          // 综合题
}
```

### ModuleType (模块类型 - ExamLab版本)
```csharp
public enum ModuleType
{
    Windows,        // Windows操作
    CSharp,         // C#编程
    PowerPoint,     // PowerPoint操作
    Excel,          // Excel操作
    Word            // Word操作
}
```

## 关联实体关系

### 核心关系图

```
Exam (试卷)
├── User (创建者) [多对一]
├── User (发布者) [多对一，可选]
├── ExamSubject (科目) [一对多]
│   ├── ExamQuestion (题目) [一对多]
│   └── ExamSubjectOperationPoint (操作点) [一对多]
├── ExamQuestion (题目) [一对多]
└── ExamExcelOperationPoint (Excel操作点) [一对多]
    └── ExamExcelOperationParameter (参数) [一对多]
```

### 详细关系说明

#### 1. Exam ↔ User
- **创建者关系**: `Exam.CreatedBy` → `User.Id` (必填)
- **发布者关系**: `Exam.PublishedBy` → `User.Id` (可选)
- **删除策略**: 创建者删除时限制删除，发布者删除时设为NULL

#### 2. Exam ↔ ExamSubject
- **关系**: 一对多
- **外键**: `ExamSubject.ExamId` → `Exam.Id`
- **删除策略**: 级联删除

#### 3. ExamSubject ↔ ExamQuestion
- **关系**: 一对多
- **外键**: `ExamQuestion.ExamSubjectId` → `ExamSubject.Id`
- **删除策略**: 级联删除

#### 4. Exam ↔ ExamExcelOperationPoint
- **关系**: 一对多
- **外键**: `ExamExcelOperationPoint.ExamId` → `Exam.Id`
- **删除策略**: 级联删除

## Excel操作相关枚举

### ExcelOperationCategory (操作分类)
```csharp
public enum ExcelOperationCategory
{
    BasicOperation = 1,     // 基础操作（23个操作点）
    DataListOperation = 2,  // 数据清单操作（6个操作点）
    ChartOperation = 3      // 图表操作（22个操作点）
}
```

### ExcelTargetType (目标对象类型)
```csharp
public enum ExcelTargetType
{
    Worksheet = 1,  // 工作表
    Chart = 2,      // 图表
    Workbook = 3    // 工作簿
}
```

### ExcelParameterDataType (参数数据类型)
```csharp
public enum ExcelParameterDataType
{
    String = 1,     // 字符串类型
    Integer = 2,    // 整数类型
    Decimal = 3,    // 小数类型
    Boolean = 4,    // 布尔类型
    Enum = 5,       // 枚举类型
    CellRange = 6,  // 单元格范围类型
    Color = 7,      // 颜色值类型
    Formula = 8,    // 公式类型
    JsonObject = 9  // JSON对象类型
}
```

## 用户角色定义

### UserRole (用户角色)
```csharp
public enum UserRole
{
    Student = 1,        // 学生
    Teacher = 2,        // 教师
    Administrator = 3   // 管理员
}
```

## 数据库约束和索引

### 主要索引
- `Exam.Name` - 试卷名称索引
- `Exam.ExamType` - 试卷类型索引
- `Exam.Status` - 试卷状态索引
- `Exam.CreatedBy` - 创建者索引
- `Exam.StartTime` - 开始时间索引
- `Exam.EndTime` - 结束时间索引
- `Exam.IsEnabled` - 启用状态索引
- `Exam.CreatedAt` - 创建时间索引

### 外键约束
- `FK_Exams_Users_CreatedBy` - 创建者外键约束
- `FK_Exams_Users_PublishedBy` - 发布者外键约束
- `FK_ExamSubjects_Exams_ExamId` - 科目外键约束
- `FK_ExamQuestions_Exams_ExamId` - 题目外键约束

## 配置类定义

### SubjectConfigBase (科目配置基类)
```csharp
public abstract class SubjectConfigBase
{
    public SubjectType SubjectType { get; set; }
    public bool AllowSkip { get; set; } = false;
    public bool ShowProgress { get; set; } = true;
}
```

### ExcelSubjectConfig (Excel科目配置)
```csharp
public class ExcelSubjectConfig : SubjectConfigBase
{
    public List<string> AllowedExcelVersions { get; set; } = ["2016", "2019", "2021"];
    public bool AllowHelp { get; set; } = false;
    public bool AutoSave { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 30;
    public Dictionary<string, decimal> CategoryWeights { get; set; } = new()
    {
        ["BasicOperation"] = 0.5m,
        ["DataListOperation"] = 0.3m,
        ["ChartOperation"] = 0.2m
    };
}
```

## 使用示例

### 创建试卷实例
```csharp
var exam = new Exam
{
    Name = "2025年春季期末考试",
    Description = "包含Excel、Word、PowerPoint三个科目的综合考试",
    ExamType = ExamType.UnifiedExam,
    Status = ExamStatus.Draft,
    TotalScore = 100.0m,
    DurationMinutes = 180,
    PassingScore = 60.0m,
    CreatedBy = 1,
    Tags = "期末考试,综合考试,2025春季"
};
```

### 添加科目
```csharp
var excelSubject = new ExamSubject
{
    ExamId = exam.Id,
    SubjectType = SubjectType.Excel,
    SubjectName = "Excel电子表格",
    Description = "Excel基础操作和图表制作",
    Score = 40.0m,
    DurationMinutes = 60,
    SortOrder = 1,
    IsRequired = true
};

exam.Subjects.Add(excelSubject);
```

### 添加题目
```csharp
var question = new ExamQuestion
{
    ExamId = exam.Id,
    ExamSubjectId = excelSubject.Id,
    Title = "数据透视表制作",
    Content = "根据给定数据创建数据透视表",
    QuestionType = QuestionType.ExcelOperation,
    Score = 10.0m,
    DifficultyLevel = 3,
    EstimatedMinutes = 15,
    SortOrder = 1,
    IsRequired = true
};

exam.Questions.Add(question);
```

## 导入导出指南

### JSON格式导出示例

#### 完整试卷导出格式
```json
{
  "exam": {
    "id": 1,
    "name": "2025年春季期末考试",
    "description": "包含Excel、Word、PowerPoint三个科目的综合考试",
    "examType": 1,
    "status": 3,
    "totalScore": 100.0,
    "durationMinutes": 180,
    "startTime": "2025-06-15T09:00:00Z",
    "endTime": "2025-06-15T12:00:00Z",
    "allowRetake": false,
    "maxRetakeCount": 0,
    "passingScore": 60.0,
    "randomizeQuestions": false,
    "showScore": true,
    "showAnswers": false,
    "createdBy": 1,
    "createdAt": "2025-01-15T08:00:00Z",
    "publishedAt": "2025-06-01T10:00:00Z",
    "publishedBy": 2,
    "isEnabled": true,
    "tags": "期末考试,综合考试,2025春季",
    "extendedConfig": null,
    "subjects": [
      {
        "id": 1,
        "examId": 1,
        "subjectType": 1,
        "subjectName": "Excel电子表格",
        "description": "Excel基础操作和图表制作",
        "score": 40.0,
        "durationMinutes": 60,
        "sortOrder": 1,
        "isRequired": true,
        "isEnabled": true,
        "minScore": 24.0,
        "weight": 1.0,
        "subjectConfig": {
          "subjectType": 1,
          "allowSkip": false,
          "showProgress": true,
          "allowedExcelVersions": ["2016", "2019", "2021"],
          "allowHelp": false,
          "autoSave": true,
          "autoSaveInterval": 30,
          "categoryWeights": {
            "BasicOperation": 0.5,
            "DataListOperation": 0.3,
            "ChartOperation": 0.2
          }
        },
        "questions": [
          {
            "id": 1,
            "examId": 1,
            "examSubjectId": 1,
            "title": "数据透视表制作",
            "content": "根据给定数据创建数据透视表，要求包含销售额汇总和地区分组",
            "questionType": 1,
            "score": 10.0,
            "difficultyLevel": 3,
            "estimatedMinutes": 15,
            "sortOrder": 1,
            "isRequired": true,
            "excelOperationPointId": 25,
            "questionConfig": {
              "operationNumber": 25,
              "parameters": {
                "sourceRange": "A1:E100",
                "pivotTableLocation": "H1",
                "rowFields": ["地区"],
                "columnFields": ["产品类别"],
                "valueFields": ["销售额"]
              },
              "allowMultipleSolutions": false,
              "partialScoring": {
                "创建透视表": 3,
                "设置行字段": 2,
                "设置列字段": 2,
                "设置值字段": 3
              },
              "hints": [
                "选择数据源范围",
                "插入数据透视表",
                "拖拽字段到相应区域"
              ]
            },
            "answerValidationRules": {
              "pivotTableExists": true,
              "correctFields": ["地区", "产品类别", "销售额"],
              "correctLocation": "H1"
            },
            "standardAnswer": {
              "pivotTable": {
                "sourceRange": "A1:E100",
                "location": "H1",
                "rowFields": ["地区"],
                "columnFields": ["产品类别"],
                "valueFields": ["销售额"]
              }
            },
            "scoringRules": {
              "fullScore": 10,
              "partialScoring": {
                "pivotTableCreated": 3,
                "rowFieldsCorrect": 2,
                "columnFieldsCorrect": 2,
                "valueFieldsCorrect": 3
              }
            },
            "tags": "数据透视表,数据分析",
            "remarks": "注意检查数据源范围的准确性",
            "isEnabled": true,
            "createdAt": "2025-01-15T08:30:00Z"
          }
        ]
      }
    ],
    "excelOperationPoints": [
      {
        "id": 1,
        "examId": 1,
        "operationNumber": 25,
        "name": "数据透视表",
        "description": "创建和配置数据透视表",
        "operationType": "A",
        "category": 2,
        "targetType": 1,
        "templateId": 25,
        "isEnabled": true,
        "createdAt": "2025-01-15T08:00:00Z",
        "parameters": [
          {
            "id": 1,
            "examOperationPointId": 1,
            "parameterName": "sourceRange",
            "displayName": "数据源范围",
            "description": "透视表的数据源单元格范围",
            "dataType": 6,
            "isRequired": true,
            "defaultValue": "A1:E100",
            "validationRules": "^[A-Z]+[0-9]+:[A-Z]+[0-9]+$",
            "sortOrder": 1,
            "isEnabled": true
          }
        ]
      }
    ]
  },
  "metadata": {
    "exportVersion": "1.0",
    "exportDate": "2025-08-11T10:00:00Z",
    "exportedBy": "system",
    "totalSubjects": 1,
    "totalQuestions": 1,
    "totalOperationPoints": 1
  }
}
```

#### 简化版导出格式（仅基本信息）
```json
{
  "exam": {
    "name": "2025年春季期末考试",
    "description": "包含Excel、Word、PowerPoint三个科目的综合考试",
    "examType": "UnifiedExam",
    "totalScore": 100.0,
    "durationMinutes": 180,
    "passingScore": 60.0,
    "subjects": [
      {
        "subjectType": "Excel",
        "subjectName": "Excel电子表格",
        "score": 40.0,
        "durationMinutes": 60,
        "questionCount": 4
      },
      {
        "subjectType": "Word",
        "subjectName": "Word文档处理",
        "score": 30.0,
        "durationMinutes": 50,
        "questionCount": 3
      },
      {
        "subjectType": "PowerPoint",
        "subjectName": "PowerPoint演示文稿",
        "score": 30.0,
        "durationMinutes": 70,
        "questionCount": 3
      }
    ]
  }
}
```

### XML格式导出示例

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ExamExport>
  <Metadata>
    <ExportVersion>1.0</ExportVersion>
    <ExportDate>2025-08-11T10:00:00Z</ExportDate>
    <ExportedBy>system</ExportedBy>
  </Metadata>

  <Exam>
    <Id>1</Id>
    <Name>2025年春季期末考试</Name>
    <Description>包含Excel、Word、PowerPoint三个科目的综合考试</Description>
    <ExamType>UnifiedExam</ExamType>
    <Status>Published</Status>
    <TotalScore>100.0</TotalScore>
    <DurationMinutes>180</DurationMinutes>
    <PassingScore>60.0</PassingScore>
    <CreatedBy>1</CreatedBy>
    <CreatedAt>2025-01-15T08:00:00Z</CreatedAt>
    <Tags>期末考试,综合考试,2025春季</Tags>

    <Subjects>
      <Subject>
        <SubjectType>Excel</SubjectType>
        <SubjectName>Excel电子表格</SubjectName>
        <Score>40.0</Score>
        <DurationMinutes>60</DurationMinutes>
        <SortOrder>1</SortOrder>
        <IsRequired>true</IsRequired>

        <Questions>
          <Question>
            <Title>数据透视表制作</Title>
            <Content>根据给定数据创建数据透视表</Content>
            <QuestionType>ExcelOperation</QuestionType>
            <Score>10.0</Score>
            <DifficultyLevel>3</DifficultyLevel>
            <EstimatedMinutes>15</EstimatedMinutes>
            <SortOrder>1</SortOrder>
          </Question>
        </Questions>
      </Subject>
    </Subjects>
  </Exam>
</ExamExport>
```

### 导入最佳实践

#### 1. 数据验证
```csharp
public class ExamImportValidator
{
    public ValidationResult ValidateExam(ExamImportModel model)
    {
        var result = new ValidationResult();

        // 基本字段验证
        if (string.IsNullOrWhiteSpace(model.Name))
            result.AddError("试卷名称不能为空");

        if (model.TotalScore <= 0)
            result.AddError("总分必须大于0");

        if (model.DurationMinutes <= 0)
            result.AddError("考试时长必须大于0");

        // 科目验证
        if (model.Subjects?.Any() != true)
            result.AddError("试卷必须包含至少一个科目");

        var totalSubjectScore = model.Subjects?.Sum(s => s.Score) ?? 0;
        if (Math.Abs(totalSubjectScore - model.TotalScore) > 0.01m)
            result.AddError("科目总分与试卷总分不匹配");

        return result;
    }
}
```

#### 2. 导入处理流程
```csharp
public async Task<ImportResult> ImportExamAsync(string jsonData)
{
    try
    {
        // 1. 反序列化
        var importModel = JsonSerializer.Deserialize<ExamImportModel>(jsonData);

        // 2. 数据验证
        var validation = _validator.ValidateExam(importModel);
        if (!validation.IsValid)
            return ImportResult.Failed(validation.Errors);

        // 3. 转换为实体
        var exam = _mapper.Map<Exam>(importModel);

        // 4. 保存到数据库
        using var transaction = await _context.Database.BeginTransactionAsync();

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return ImportResult.Success(exam.Id);
    }
    catch (Exception ex)
    {
        return ImportResult.Failed($"导入失败: {ex.Message}");
    }
}
```

#### 3. 字段映射配置
```csharp
public class ExamMappingProfile : Profile
{
    public ExamMappingProfile()
    {
        CreateMap<ExamImportModel, Exam>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ExamStatus.Draft))
            .ForMember(dest => dest.ExamType, opt => opt.MapFrom(src =>
                Enum.Parse<ExamType>(src.ExamType, true)));

        CreateMap<SubjectImportModel, ExamSubject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectType, opt => opt.MapFrom(src =>
                Enum.Parse<SubjectType>(src.SubjectType, true)));
    }
}
```

### 导出最佳实践

#### 1. 分层导出策略
```csharp
public class ExamExportService
{
    public async Task<string> ExportExamAsync(int examId, ExportLevel level)
    {
        var query = _context.Exams.Where(e => e.Id == examId);

        switch (level)
        {
            case ExportLevel.Basic:
                query = query.Include(e => e.Subjects);
                break;

            case ExportLevel.Complete:
                query = query
                    .Include(e => e.Subjects)
                        .ThenInclude(s => s.Questions)
                    .Include(e => e.ExcelOperationPoints)
                        .ThenInclude(op => op.Parameters);
                break;

            case ExportLevel.WithAnswers:
                // 仅管理员可导出答案
                if (!_currentUser.IsAdmin)
                    throw new UnauthorizedAccessException();

                query = query
                    .Include(e => e.Subjects)
                        .ThenInclude(s => s.Questions)
                    .Include(e => e.ExcelOperationPoints);
                break;
        }

        var exam = await query.FirstOrDefaultAsync();
        return JsonSerializer.Serialize(exam, _jsonOptions);
    }
}

public enum ExportLevel
{
    Basic,      // 仅基本信息和科目
    Complete,   // 包含题目但不含答案
    WithAnswers // 包含完整答案（仅管理员）
}
```

#### 2. 安全考虑
- 导出时移除敏感信息（答案、评分规则等）
- 根据用户权限控制导出内容
- 记录导出操作日志
- 对导出文件进行加密（如需要）

#### 3. 性能优化
- 使用分页导出大量数据
- 异步处理导出任务
- 缓存常用的导出模板
- 压缩导出文件

## 版本兼容性

### 导入版本检查
```csharp
public class VersionCompatibilityChecker
{
    private readonly Dictionary<string, string[]> _supportedVersions = new()
    {
        ["1.0"] = new[] { "1.0" },
        ["1.1"] = new[] { "1.0", "1.1" },
        ["2.0"] = new[] { "1.0", "1.1", "2.0" }
    };

    public bool IsCompatible(string importVersion, string currentVersion)
    {
        return _supportedVersions.ContainsKey(currentVersion) &&
               _supportedVersions[currentVersion].Contains(importVersion);
    }
}
```

### 数据迁移策略
- 提供版本升级脚本
- 保持向后兼容性
- 记录数据结构变更历史
- 提供数据迁移工具

## 注意事项

1. **数据完整性**: 导入时确保外键关系正确
2. **权限控制**: 根据用户角色限制导入导出功能
3. **数据验证**: 严格验证导入数据的格式和内容
4. **错误处理**: 提供详细的错误信息和回滚机制
5. **性能考虑**: 大数据量时使用批量操作和分页处理
6. **安全性**: 防止SQL注入和恶意数据导入
7. **审计日志**: 记录所有导入导出操作
8. **备份策略**: 导入前自动备份现有数据
