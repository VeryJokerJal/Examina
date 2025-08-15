using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ExamLab.Models;
using ExamLab.Models.ImportExport;
using ExamLab.Services;

namespace ExamLab.Services;

/// <summary>
/// 数据源类型枚举 - 用于识别ExamExportDto中的数据类型
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// 未知类型 - 无法确定数据来源
    /// </summary>
    Unknown,

    /// <summary>
    /// 考试试卷 - 包含多个模块的综合试卷
    /// </summary>
    RegularExam,

    /// <summary>
    /// 专项试卷 - 单一模块类型的专项练习试卷
    /// </summary>
    SpecializedExam
}

/// <summary>
/// 专项试卷映射服务 - 处理SpecializedExam与ExamExportDto之间的转换
/// </summary>
public static class SpecializedExamMappingService
{
    /// <summary>
    /// 将SpecializedExam转换为专项试卷专用的导出DTO
    /// </summary>
    /// <param name="specializedExam">专项试卷</param>
    /// <param name="exportLevel">导出级别</param>
    /// <returns>专项试卷导出DTO</returns>
    public static SpecializedExamExportDto ToSpecializedExportDto(SpecializedExam specializedExam, ExportLevel exportLevel = ExportLevel.Complete)
    {
        if (specializedExam == null)
            throw new ArgumentNullException(nameof(specializedExam));

        SpecializedExamDto specializedExamDto = new()
        {
            Id = specializedExam.Id,
            Name = specializedExam.Name,
            Description = specializedExam.Description,
            ModuleType = specializedExam.ModuleType.ToString(),
            DifficultyLevel = specializedExam.DifficultyLevel,
            RandomizeQuestions = specializedExam.RandomizeQuestions,
            Tags = specializedExam.Tags,
            TotalScore = specializedExam.TotalScore,
            Duration = specializedExam.Duration,
            CreatedTime = DateTime.TryParse(specializedExam.CreatedTime, out DateTime createdTime) ? createdTime : DateTime.Now,
            LastModifiedTime = DateTime.TryParse(specializedExam.LastModifiedTime, out DateTime modifiedTime) ? modifiedTime : DateTime.Now,
            IsEnabled = true
        };

        // 转换模块
        foreach (ExamModule module in specializedExam.Modules)
        {
            specializedExamDto.Modules.Add(ToModuleDto(module, exportLevel));
        }

        ExportMetadataDto metadata = new()
        {
            ExportDate = DateTime.UtcNow,
            ExportedBy = "ExamLab",
            TotalSubjects = specializedExam.Modules.Count,
            TotalQuestions = specializedExam.TotalQuestionCount,
            TotalOperationPoints = specializedExam.TotalOperationPointCount,
            ExportLevel = exportLevel.ToString()
        };

        return new SpecializedExamExportDto
        {
            SpecializedExam = specializedExamDto,
            Metadata = metadata,
            FormatVersion = "1.0",
            DataType = "SpecializedExam"
        };
    }

    /// <summary>
    /// 将SpecializedExam转换为通用的ExamExportDto（向后兼容）
    /// </summary>
    /// <param name="specializedExam">专项试卷</param>
    /// <param name="exportLevel">导出级别</param>
    /// <returns>通用导出DTO</returns>
    public static ExamExportDto ToExportDto(SpecializedExam specializedExam, ExportLevel exportLevel = ExportLevel.Complete)
    {
        if (specializedExam == null)
        {
            throw new ArgumentNullException(nameof(specializedExam));
        }

        // 创建临时的Exam对象用于复用现有的映射逻辑
        Exam tempExam = new()
        {
            Id = specializedExam.Id,
            Name = specializedExam.Name,
            Description = specializedExam.Description,
            CreatedTime = specializedExam.CreatedTime,
            LastModifiedTime = specializedExam.LastModifiedTime,
            TotalScore = specializedExam.TotalScore,
            Duration = specializedExam.Duration
        };

        // 复制模块
        foreach (ExamModule module in specializedExam.Modules)
        {
            tempExam.Modules.Add(module);
        }

        // 使用现有的ExamMappingService进行转换
        ExamExportDto exportDto = ExamMappingService.ToExportDto(tempExam, exportLevel);

        // 添加专项试卷特有的信息到扩展配置中
        if (exportDto.Exam != null)
        {
            exportDto.Exam.ExtendedConfig = JsonSerializer.Serialize(new
            {
                IsSpecializedExam = true,
                ModuleType = specializedExam.ModuleType.ToString(),
                DifficultyLevel = specializedExam.DifficultyLevel,
                RandomizeQuestions = specializedExam.RandomizeQuestions,
                Tags = specializedExam.Tags
            });
        }

        return exportDto;
    }

    /// <summary>
    /// 从通用ExamExportDto转换为SpecializedExam（已弃用）
    /// 注意：此方法已弃用，建议使用SmartImport()进行智能导入，或使用FromSpecializedExportDto()处理专用格式
    /// </summary>
    /// <param name="exportDto">通用导出DTO</param>
    /// <returns>专项试卷</returns>
    /// <exception cref="ArgumentNullException">当exportDto或其Exam属性为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当数据不是专项试卷格式时抛出</exception>
    [Obsolete("此方法已弃用。请使用SmartImport()进行智能导入，或使用FromSpecializedExportDto()处理专用格式。", false)]
    public static SpecializedExam FromExportDto(ExamExportDto exportDto)
    {
        if (exportDto?.Exam == null)
        {
            throw new ArgumentNullException(nameof(exportDto), "导入数据不能为空");
        }

        ExamDto examDto = exportDto.Exam;

        // 1. 检查数据类型 - 验证是否为专项试卷数据
        DataSourceType dataSourceType = DetectDataSourceType(examDto);

        switch (dataSourceType)
        {
            case DataSourceType.SpecializedExam:
                // 确认是专项试卷数据，继续转换
                break;

            case DataSourceType.RegularExam:
                throw new InvalidOperationException(
                    "检测到考试试卷格式的数据。请使用ExamMappingService.FromExportDto()方法导入考试试卷，" +
                    "或者使用ConvertRegularExamToSpecialized()方法将考试试卷转换为专项试卷。");

            case DataSourceType.Unknown:
                // 数据类型未知，尝试作为通用格式处理，但给出警告
                // 这种情况下我们假设用户知道自己在做什么，继续转换
                break;

            default:
                throw new InvalidOperationException($"不支持的数据源类型：{dataSourceType}");
        }

        // 直接创建SpecializedExam，避免中间转换
        SpecializedExam specializedExam = new()
        {
            Id = string.IsNullOrEmpty(examDto.Id) ? IdGeneratorService.GenerateExamId() : examDto.Id,
            Name = examDto.Name,
            Description = examDto.Description ?? string.Empty,
            TotalScore = (int)examDto.TotalScore,
            Duration = examDto.DurationMinutes,
            CreatedTime = examDto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = (examDto.UpdatedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 直接转换模块，确保数据完整性
        foreach (ModuleDto moduleDto in examDto.Modules)
        {
            specializedExam.Modules.Add(FromModuleDtoToExamModule(moduleDto));
        }

        // 如果没有模块但有科目，则从科目转换为模块
        if (specializedExam.Modules.Count == 0 && examDto.Subjects.Count > 0)
        {
            foreach (SubjectDto subjectDto in examDto.Subjects)
            {
                specializedExam.Modules.Add(FromSubjectDtoToExamModule(subjectDto));
            }
        }

        // 尝试从扩展配置中恢复专项试卷特有的信息
        string? extendedConfigString = exportDto.Exam.ExtendedConfig?.ToString();
        if (!string.IsNullOrEmpty(extendedConfigString))
        {
            try
            {
                using System.Text.Json.JsonDocument doc = JsonDocument.Parse(extendedConfigString);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("IsSpecializedExam", out JsonElement isSpecializedElement) &&
                    isSpecializedElement.GetBoolean())
                {
                    // 恢复ModuleType
                    if (root.TryGetProperty("ModuleType", out JsonElement moduleTypeElement))
                    {
                        if (Enum.TryParse<ModuleType>(moduleTypeElement.GetString(), out ModuleType moduleType))
                        {
                            specializedExam.ModuleType = moduleType;
                        }
                    }

                    // 恢复DifficultyLevel
                    if (root.TryGetProperty("DifficultyLevel", out JsonElement difficultyElement))
                    {
                        specializedExam.DifficultyLevel = difficultyElement.GetInt32();
                    }

                    // 恢复RandomizeQuestions
                    if (root.TryGetProperty("RandomizeQuestions", out JsonElement randomizeElement))
                    {
                        specializedExam.RandomizeQuestions = randomizeElement.GetBoolean();
                    }

                    // 恢复Tags
                    if (root.TryGetProperty("Tags", out JsonElement tagsElement))
                    {
                        specializedExam.Tags = tagsElement.GetString() ?? string.Empty;
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // 如果解析失败，使用默认值
            }
        }

        // 如果没有从扩展配置中恢复ModuleType，尝试从第一个模块推断
        if (specializedExam.ModuleType == ModuleType.Windows && specializedExam.Modules.Count > 0)
        {
            specializedExam.ModuleType = specializedExam.Modules.First().Type;
        }

        return specializedExam;
    }

    /// <summary>
    /// 验证专项试卷数据
    /// </summary>
    /// <param name="specializedExam">专项试卷</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidateSpecializedExam(SpecializedExam specializedExam)
    {
        if (specializedExam == null)
        {
            return new ValidationResult(false, ["专项试卷对象为空"]);
        }

        List<string> errors = new();

        // 基本信息验证
        if (string.IsNullOrWhiteSpace(specializedExam.Name))
        {
            errors.Add("专项试卷名称不能为空");
        }

        if (specializedExam.Modules.Count == 0)
        {
            errors.Add("专项试卷必须包含至少一个模块");
        }

        // 专项试卷特有验证：应该只包含一种模块类型
        if (specializedExam.Modules.Count > 0)
        {
            ModuleType primaryType = specializedExam.ModuleType;
            if (specializedExam.Modules.Any(m => m.Type != primaryType))
            {
                errors.Add($"专项试卷应该只包含{primaryType}类型的模块");
            }
        }

        // 难度等级验证
        if (specializedExam.DifficultyLevel is < 1 or > 5)
        {
            errors.Add("难度等级必须在1-5之间");
        }

        // 时长验证
        if (specializedExam.Duration <= 0)
        {
            errors.Add("考试时长必须大于0");
        }

        // 分数验证
        if (specializedExam.TotalScore <= 0)
        {
            errors.Add("总分必须大于0");
        }

        // 使用现有的ValidationService验证模块和题目
        foreach (ExamModule module in specializedExam.Modules)
        {
            // 这里可以调用现有的模块验证逻辑
            // 暂时简化处理
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                errors.Add($"模块名称不能为空");
            }
        }

        return new ValidationResult(errors.Count == 0,  errors);
    }

    /// <summary>
    /// 创建专项试卷的摘要信息
    /// </summary>
    /// <param name="specializedExam">专项试卷</param>
    /// <returns>摘要信息</returns>
    public static string CreateSummaryInfo(SpecializedExam specializedExam)
    {
        return specializedExam == null
            ? "无效的专项试卷"
            : $"专项试卷名称：{specializedExam.Name}\n" +
               $"模块类型：{specializedExam.ModuleTypeName}\n" +
               $"难度等级：{specializedExam.DifficultyLevel}\n" +
               $"模块数量：{specializedExam.Modules.Count}\n" +
               $"题目总数：{specializedExam.TotalQuestionCount}\n" +
               $"操作点总数：{specializedExam.TotalOperationPointCount}\n" +
               $"总分：{specializedExam.TotalScore}\n" +
               $"时长：{specializedExam.Duration}分钟";
    }

    /// <summary>
    /// 将ModuleDto转换为ExamModule
    /// </summary>
    private static ExamModule FromModuleDtoToExamModule(ModuleDto moduleDto)
    {
        ExamModule module = new()
        {
            Id = moduleDto.Id,
            Name = moduleDto.Name,
            Description = moduleDto.Description,
            Score = moduleDto.Score,
            Order = moduleDto.Order,
            IsEnabled = moduleDto.IsEnabled
        };

        // 解析模块类型
        if (Enum.TryParse<ModuleType>(moduleDto.Type, true, out ModuleType moduleType))
        {
            module.Type = moduleType;
        }

        // 转换题目
        foreach (QuestionDto questionDto in moduleDto.Questions)
        {
            module.Questions.Add(FromQuestionDtoToQuestion(questionDto));
        }

        return module;
    }

    /// <summary>
    /// 将SubjectDto转换为ExamModule
    /// </summary>
    private static ExamModule FromSubjectDtoToExamModule(SubjectDto subjectDto)
    {
        ExamModule module = new()
        {
            Id = $"module-{subjectDto.Id}",
            Name = subjectDto.SubjectName,
            Description = subjectDto.Description ?? string.Empty,
            Score = (int)subjectDto.Score,
            Order = subjectDto.SortOrder,
            IsEnabled = subjectDto.IsEnabled,
            // 根据科目类型映射模块类型
            Type = subjectDto.SubjectType.ToLower() switch
            {
                "excel" => ModuleType.Excel,
                "word" => ModuleType.Word,
                "powerpoint" => ModuleType.PowerPoint,
                "windows" => ModuleType.Windows,
                "csharp" => ModuleType.CSharp,
                _ => ModuleType.Windows
            }
        };

        // 转换题目
        foreach (QuestionDto questionDto in subjectDto.Questions)
        {
            module.Questions.Add(FromQuestionDtoToQuestion(questionDto));
        }

        return module;
    }

    /// <summary>
    /// 将QuestionDto转换为Question
    /// </summary>
    private static Question FromQuestionDtoToQuestion(QuestionDto questionDto)
    {
        Question question = new()
        {
            Id = questionDto.Id,
            Title = questionDto.Title,
            Content = questionDto.Content, // 使用Content而不是Description
            Order = questionDto.SortOrder,
            IsEnabled = questionDto.IsEnabled,
            CreatedTime = questionDto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            ProgramInput = questionDto.ProgramInput,
            ExpectedOutput = questionDto.ExpectedOutput
        };

        // 转换操作点
        foreach (OperationPointDto operationPointDto in questionDto.OperationPoints)
        {
            question.OperationPoints.Add(FromOperationPointDtoToOperationPoint(operationPointDto));
        }

        return question;
    }

    /// <summary>
    /// 将OperationPointDto转换为OperationPoint
    /// </summary>
    private static OperationPoint FromOperationPointDtoToOperationPoint(OperationPointDto operationPointDto)
    {
        OperationPoint operationPoint = new()
        {
            Id = operationPointDto.Id,
            Name = operationPointDto.Name,
            Description = operationPointDto.Description,
            Score = operationPointDto.Score,
            Order = operationPointDto.Order,
            IsEnabled = operationPointDto.IsEnabled,
            CreatedTime = operationPointDto.CreatedTime
        };

        // 解析模块类型
        if (Enum.TryParse<ModuleType>(operationPointDto.ModuleType, true, out ModuleType moduleType))
        {
            operationPoint.ModuleType = moduleType;
        }

        // 转换参数
        foreach (ParameterDto parameterDto in operationPointDto.Parameters)
        {
            operationPoint.Parameters.Add(FromParameterDtoToConfigurationParameter(parameterDto));
        }

        return operationPoint;
    }

    /// <summary>
    /// 将ParameterDto转换为ConfigurationParameter
    /// </summary>
    private static ConfigurationParameter FromParameterDtoToConfigurationParameter(ParameterDto parameterDto)
    {
        ConfigurationParameter parameter = new()
        {
            Id = IdGeneratorService.GenerateParameterId(), // ParameterDto没有Id，生成新的
            Name = parameterDto.Name,
            DisplayName = parameterDto.DisplayName,
            Description = parameterDto.Description,
            Value = parameterDto.Value,
            DefaultValue = parameterDto.DefaultValue,
            IsRequired = parameterDto.IsRequired,
            Order = parameterDto.Order,
            IsEnabled = true // ParameterDto没有IsEnabled属性，默认为true
        };

        // 解析参数类型
        if (Enum.TryParse<ParameterType>(parameterDto.Type, true, out ParameterType parameterType))
        {
            parameter.Type = parameterType;
        }

        // 设置枚举选项
        if (!string.IsNullOrEmpty(parameterDto.EnumOptions))
        {
            parameter.EnumOptions = parameterDto.EnumOptions;
        }

        // 设置验证规则
        if (!string.IsNullOrEmpty(parameterDto.ValidationRule))
        {
            parameter.ValidationRule = parameterDto.ValidationRule;
            parameter.ValidationErrorMessage = parameterDto.ValidationErrorMessage;
        }

        // 设置数值范围
        parameter.MinValue = parameterDto.MinValue;
        parameter.MaxValue = parameterDto.MaxValue;

        return parameter;
    }

    /// <summary>
    /// 检测ExamExportDto中的数据源类型
    /// </summary>
    /// <param name="examDto">试卷DTO</param>
    /// <returns>数据源类型</returns>
    private static DataSourceType DetectDataSourceType(ExamDto examDto)
    {
        // 1. 首先检查ExtendedConfig中的明确标识
        string? extendedConfigString = examDto.ExtendedConfig?.ToString();
        if (!string.IsNullOrEmpty(extendedConfigString))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(extendedConfigString);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("IsSpecializedExam", out JsonElement isSpecializedElement))
                {
                    if (isSpecializedElement.GetBoolean())
                    {
                        return DataSourceType.SpecializedExam;
                    }
                    else
                    {
                        return DataSourceType.RegularExam;
                    }
                }
            }
            catch (JsonException)
            {
                // ExtendedConfig解析失败，继续使用其他方法检测
            }
        }

        // 2. 基于数据特征进行启发式检测
        return DetectDataSourceByHeuristics(examDto);
    }

    /// <summary>
    /// 基于数据特征启发式检测数据源类型
    /// </summary>
    /// <param name="examDto">试卷DTO</param>
    /// <returns>数据源类型</returns>
    private static DataSourceType DetectDataSourceByHeuristics(ExamDto examDto)
    {
        // 获取所有模块类型
        List<string> moduleTypes = examDto.Modules.Select(m => m.Type).Distinct().ToList();

        // 如果没有模块，检查科目
        if (moduleTypes.Count == 0 && examDto.Subjects.Count > 0)
        {
            moduleTypes = examDto.Subjects.Select(s => s.SubjectType).Distinct().ToList();
        }

        // 专项试卷特征：
        // 1. 只有一种模块类型
        // 2. 名称通常包含"专项"、"练习"等关键词
        // 3. 模块数量通常较少（1-2个）
        if (moduleTypes.Count == 1)
        {
            string examName = examDto.Name.ToLower();
            bool hasSpecializedKeywords = examName.Contains("专项") ||
                                        examName.Contains("练习") ||
                                        examName.Contains("专门") ||
                                        examName.Contains("单项");

            int totalModules = examDto.Modules.Count + examDto.Subjects.Count;
            bool hasLimitedModules = totalModules <= 2;

            if (hasSpecializedKeywords || hasLimitedModules)
            {
                return DataSourceType.SpecializedExam;
            }
        }

        // 考试试卷特征：
        // 1. 多种模块类型
        // 2. 名称通常包含"试卷"、"考试"等关键词
        // 3. 模块数量较多
        if (moduleTypes.Count > 1)
        {
            return DataSourceType.RegularExam;
        }

        // 无法确定类型
        return DataSourceType.Unknown;
    }

    /// <summary>
    /// 将考试试卷格式转换为专项试卷格式
    /// 此方法用于处理用户明确要求将考试试卷转换为专项试卷的情况
    /// </summary>
    /// <param name="exportDto">考试试卷格式的导出DTO</param>
    /// <param name="targetModuleType">目标模块类型（如果为null，使用第一个模块的类型）</param>
    /// <returns>专项试卷</returns>
    public static SpecializedExam ConvertRegularExamToSpecialized(ExamExportDto exportDto, ModuleType? targetModuleType = null)
    {
        if (exportDto?.Exam == null)
        {
            throw new ArgumentNullException(nameof(exportDto), "导入数据不能为空");
        }

        ExamDto examDto = exportDto.Exam;

        // 创建专项试卷
        SpecializedExam specializedExam = new()
        {
            Id = string.IsNullOrEmpty(examDto.Id) ? IdGeneratorService.GenerateExamId() : examDto.Id,
            Name = $"{examDto.Name} (转换为专项试卷)",
            Description = examDto.Description ?? string.Empty,
            TotalScore = (int)examDto.TotalScore,
            Duration = Math.Min(examDto.DurationMinutes, 120), // 专项试卷时长通常较短
            CreatedTime = examDto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            DifficultyLevel = 1, // 默认难度
            RandomizeQuestions = false,
            Tags = "从考试试卷转换"
        };

        // 确定目标模块类型
        ModuleType selectedModuleType = targetModuleType ?? DetermineTargetModuleType(examDto);
        specializedExam.ModuleType = selectedModuleType;

        // 只转换匹配目标类型的模块
        foreach (ModuleDto moduleDto in examDto.Modules)
        {
            if (Enum.TryParse<ModuleType>(moduleDto.Type, true, out ModuleType moduleType) &&
                moduleType == selectedModuleType)
            {
                specializedExam.Modules.Add(FromModuleDtoToExamModule(moduleDto));
            }
        }

        // 处理科目数据
        foreach (SubjectDto subjectDto in examDto.Subjects)
        {
            ModuleType subjectModuleType = subjectDto.SubjectType.ToLower() switch
            {
                "excel" => ModuleType.Excel,
                "word" => ModuleType.Word,
                "powerpoint" => ModuleType.PowerPoint,
                "windows" => ModuleType.Windows,
                "csharp" => ModuleType.CSharp,
                _ => ModuleType.Windows
            };

            if (subjectModuleType == selectedModuleType)
            {
                specializedExam.Modules.Add(FromSubjectDtoToExamModule(subjectDto));
            }
        }

        // 如果没有找到匹配的模块，创建一个默认模块
        if (specializedExam.Modules.Count == 0)
        {
            specializedExam.CreateDefaultModule();
        }

        return specializedExam;
    }

    /// <summary>
    /// 确定目标模块类型（选择第一个可用的模块类型）
    /// </summary>
    private static ModuleType DetermineTargetModuleType(ExamDto examDto)
    {
        // 从模块中获取第一个类型
        if (examDto.Modules.Count > 0)
        {
            if (Enum.TryParse<ModuleType>(examDto.Modules[0].Type, true, out ModuleType moduleType))
            {
                return moduleType;
            }
        }

        // 从科目中获取第一个类型
        if (examDto.Subjects.Count > 0)
        {
            return examDto.Subjects[0].SubjectType.ToLower() switch
            {
                "excel" => ModuleType.Excel,
                "word" => ModuleType.Word,
                "powerpoint" => ModuleType.PowerPoint,
                "windows" => ModuleType.Windows,
                "csharp" => ModuleType.CSharp,
                _ => ModuleType.Windows
            };
        }

        // 默认返回Windows类型
        return ModuleType.Windows;
    }

    /// <summary>
    /// 从专项试卷导出DTO转换为SpecializedExam
    /// </summary>
    /// <param name="exportDto">专项试卷导出DTO</param>
    /// <returns>专项试卷</returns>
    public static SpecializedExam FromSpecializedExportDto(SpecializedExamExportDto exportDto)
    {
        if (exportDto?.SpecializedExam == null)
            throw new ArgumentNullException(nameof(exportDto));

        SpecializedExamDto examDto = exportDto.SpecializedExam;

        SpecializedExam specializedExam = new()
        {
            Id = string.IsNullOrEmpty(examDto.Id) ? IdGeneratorService.GenerateExamId() : examDto.Id,
            Name = examDto.Name,
            Description = examDto.Description,
            TotalScore = examDto.TotalScore,
            Duration = examDto.Duration,
            DifficultyLevel = examDto.DifficultyLevel,
            RandomizeQuestions = examDto.RandomizeQuestions,
            Tags = examDto.Tags,
            CreatedTime = examDto.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = examDto.LastModifiedTime.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 解析模块类型
        if (Enum.TryParse<ModuleType>(examDto.ModuleType, true, out ModuleType moduleType))
        {
            specializedExam.ModuleType = moduleType;
        }

        // 转换模块
        foreach (ModuleDto moduleDto in examDto.Modules)
        {
            specializedExam.Modules.Add(FromModuleDtoToExamModule(moduleDto));
        }

        return specializedExam;
    }

    /// <summary>
    /// 智能导入：检测格式并转换为SpecializedExam
    /// </summary>
    /// <param name="exportDto">通用导出DTO</param>
    /// <returns>转换结果</returns>
    public static SpecializedExamImportResult SmartImport(ExamExportDto exportDto)
    {
        if (exportDto?.Exam == null)
            return SpecializedExamImportResult.Failure("导入数据不能为空");

        try
        {
            // 检测数据格式
            DataFormatDetectionResult detection = DetectDataFormat(exportDto);

            switch (detection.FormatType)
            {
                case DataFormatType.SpecializedExam:
                    // 直接转换专项试卷
                    SpecializedExam specializedExam = FromExportDto(exportDto);
                    return SpecializedExamImportResult.Success(specializedExam);

                case DataFormatType.GenericExam:
                    // 检查是否可以转换
                    if (detection.CanConvertToSpecialized)
                    {
                        SpecializedExam convertedExam = ConvertGenericToSpecialized(exportDto);
                        SpecializedExamImportResult result = SpecializedExamImportResult.Success(convertedExam, true);
                        result.AddWarning("数据来源为通用试卷格式，已自动转换为专项试卷");
                        if (!string.IsNullOrEmpty(detection.ConversionSuggestion))
                        {
                            result.AddWarning(detection.ConversionSuggestion);
                        }
                        return result;
                    }
                    else
                    {
                        return SpecializedExamImportResult.Failure(
                            "检测到通用试卷格式，但无法转换为专项试卷。" +
                            "专项试卷要求只包含一种模块类型，而当前数据包含多种模块类型。");
                    }

                case DataFormatType.Unknown:
                default:
                    return SpecializedExamImportResult.Failure(
                        $"无法识别的数据格式。检测置信度：{detection.Confidence:P0}。" +
                        $"检测依据：{detection.DetectionReason}");
            }
        }
        catch (Exception ex)
        {
            return SpecializedExamImportResult.Failure($"导入过程中发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 检测数据格式类型
    /// </summary>
    /// <param name="exportDto">导出DTO</param>
    /// <returns>检测结果</returns>
    public static DataFormatDetectionResult DetectDataFormat(ExamExportDto exportDto)
    {
        if (exportDto?.Exam == null)
        {
            return new DataFormatDetectionResult
            {
                FormatType = DataFormatType.Unknown,
                Confidence = 0.0,
                DetectionReason = "数据为空",
                CanConvertToSpecialized = false
            };
        }

        ExamDto examDto = exportDto.Exam;
        double confidence = 0.0;
        List<string> reasons = new();
        bool canConvert = false;

        // 检查扩展配置中的专项试卷标识
        string? extendedConfigString = examDto.ExtendedConfig?.ToString();
        if (!string.IsNullOrEmpty(extendedConfigString))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(extendedConfigString);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("IsSpecializedExam", out JsonElement isSpecializedElement) &&
                    isSpecializedElement.GetBoolean())
                {
                    confidence = 0.9;
                    reasons.Add("ExtendedConfig中包含IsSpecializedExam=true标识");
                    canConvert = true;

                    return new DataFormatDetectionResult
                    {
                        FormatType = DataFormatType.SpecializedExam,
                        Confidence = confidence,
                        DetectionReason = string.Join("; ", reasons),
                        CanConvertToSpecialized = canConvert
                    };
                }
            }
            catch (JsonException)
            {
                // 忽略JSON解析错误
            }
        }

        // 检查模块类型一致性
        List<string> moduleTypes = examDto.Modules.Select(m => m.Type).Distinct().ToList();
        if (moduleTypes.Count == 0 && examDto.Subjects.Count > 0)
        {
            moduleTypes = examDto.Subjects.Select(s => s.SubjectType).Distinct().ToList();
        }

        if (moduleTypes.Count == 1)
        {
            confidence += 0.6;
            reasons.Add($"只包含一种模块类型：{moduleTypes[0]}");
            canConvert = true;
        }
        else if (moduleTypes.Count > 1)
        {
            confidence += 0.3;
            reasons.Add($"包含多种模块类型：{string.Join(", ", moduleTypes)}");
            canConvert = false; // 多模块类型无法转换为专项试卷
        }

        // 检查试卷名称特征
        if (examDto.Name.Contains("专项") || examDto.Name.Contains("练习"))
        {
            confidence += 0.2;
            reasons.Add("试卷名称包含专项试卷特征词汇");
        }

        // 检查题目数量（专项试卷通常题目较少）
        int totalQuestions = examDto.Modules.Sum(m => m.Questions.Count) +
                           examDto.Subjects.Sum(s => s.Questions.Count);
        if (totalQuestions <= 20)
        {
            confidence += 0.1;
            reasons.Add($"题目数量较少（{totalQuestions}题），符合专项试卷特征");
        }

        // 确定格式类型
        DataFormatType formatType;
        if (confidence >= 0.7)
        {
            formatType = DataFormatType.SpecializedExam;
        }
        else if (confidence >= 0.3)
        {
            formatType = DataFormatType.GenericExam;
        }
        else
        {
            formatType = DataFormatType.Unknown;
        }

        return new DataFormatDetectionResult
        {
            FormatType = formatType,
            Confidence = confidence,
            DetectionReason = string.Join("; ", reasons),
            CanConvertToSpecialized = canConvert,
            ConversionSuggestion = canConvert ? null : "建议手动创建专项试卷并导入单个模块"
        };
    }

    /// <summary>
    /// 将通用试卷格式转换为专项试卷
    /// </summary>
    /// <param name="exportDto">通用导出DTO</param>
    /// <returns>专项试卷</returns>
    private static SpecializedExam ConvertGenericToSpecialized(ExamExportDto exportDto)
    {
        if (exportDto?.Exam == null)
            throw new ArgumentNullException(nameof(exportDto));

        ExamDto examDto = exportDto.Exam;

        // 创建专项试卷
        SpecializedExam specializedExam = new()
        {
            Id = string.IsNullOrEmpty(examDto.Id) ? IdGeneratorService.GenerateExamId() : examDto.Id,
            Name = examDto.Name,
            Description = examDto.Description ?? string.Empty,
            TotalScore = (int)examDto.TotalScore,
            Duration = examDto.DurationMinutes,
            CreatedTime = examDto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = (examDto.UpdatedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 转换模块
        foreach (ModuleDto moduleDto in examDto.Modules)
        {
            specializedExam.Modules.Add(FromModuleDtoToExamModule(moduleDto));
        }

        // 如果没有模块但有科目，则从科目转换
        if (specializedExam.Modules.Count == 0 && examDto.Subjects.Count > 0)
        {
            foreach (SubjectDto subjectDto in examDto.Subjects)
            {
                specializedExam.Modules.Add(FromSubjectDtoToExamModule(subjectDto));
            }
        }

        // 设置专项试卷的模块类型（取第一个模块的类型）
        if (specializedExam.Modules.Count > 0)
        {
            specializedExam.ModuleType = specializedExam.Modules.First().Type;
        }

        return specializedExam;
    }

    /// <summary>
    /// 将ExamModule转换为ModuleDto
    /// </summary>
    private static ModuleDto ToModuleDto(ExamModule module, ExportLevel exportLevel)
    {
        ModuleDto moduleDto = new()
        {
            Id = module.Id,
            Name = module.Name,
            Type = module.Type.ToString(),
            Description = module.Description,
            Score = module.Score,
            Order = module.Order,
            IsEnabled = module.IsEnabled
        };

        // 转换题目
        foreach (Question question in module.Questions)
        {
            moduleDto.Questions.Add(ToQuestionDto(question, exportLevel));
        }

        return moduleDto;
    }

    /// <summary>
    /// 将Question转换为QuestionDto
    /// </summary>
    private static QuestionDto ToQuestionDto(Question question, ExportLevel exportLevel)
    {
        QuestionDto questionDto = new()
        {
            Id = question.Id,
            Title = question.Title,
            Content = question.Content,
            QuestionType = "Practice", // 专项试卷默认为练习类型
            Score = (decimal)question.TotalScore,
            DifficultyLevel = 1,
            EstimatedMinutes = 5,
            SortOrder = question.Order,
            IsRequired = true,
            Tags = string.Empty,
            Remarks = string.Empty,
            IsEnabled = question.IsEnabled,
            CreatedAt = DateTime.TryParse(question.CreatedTime, out DateTime createdTime) ? createdTime : DateTime.UtcNow,
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput
        };

        // 根据导出级别决定是否包含操作点
        if (exportLevel != ExportLevel.Basic)
        {
            foreach (OperationPoint operationPoint in question.OperationPoints)
            {
                questionDto.OperationPoints.Add(ToOperationPointDto(operationPoint, exportLevel));
            }
        }

        return questionDto;
    }

    /// <summary>
    /// 将OperationPoint转换为OperationPointDto
    /// </summary>
    private static OperationPointDto ToOperationPointDto(OperationPoint operationPoint, ExportLevel exportLevel)
    {
        OperationPointDto operationPointDto = new()
        {
            Id = operationPoint.Id,
            Name = operationPoint.Name,
            Description = operationPoint.Description,
            ModuleType = operationPoint.ModuleType.ToString(),
            Score = operationPoint.Score,
            Order = operationPoint.Order,
            IsEnabled = operationPoint.IsEnabled,
            CreatedTime = operationPoint.CreatedTime
        };

        // 根据导出级别决定是否包含参数
        if (exportLevel == ExportLevel.Complete)
        {
            foreach (ConfigurationParameter parameter in operationPoint.Parameters)
            {
                operationPointDto.Parameters.Add(ToParameterDto(parameter));
            }
        }

        return operationPointDto;
    }

    /// <summary>
    /// 将ConfigurationParameter转换为ParameterDto
    /// </summary>
    private static ParameterDto ToParameterDto(ConfigurationParameter parameter)
    {
        return new ParameterDto
        {
            Name = parameter.Name,
            DisplayName = parameter.DisplayName,
            Description = parameter.Description,
            Type = parameter.Type.ToString(),
            Value = parameter.Value ?? string.Empty,
            DefaultValue = parameter.DefaultValue ?? string.Empty,
            IsRequired = parameter.IsRequired,
            Order = parameter.Order,
            EnumOptions = parameter.EnumOptions,
            ValidationRule = parameter.ValidationRule,
            ValidationErrorMessage = parameter.ValidationErrorMessage,
            MinValue = parameter.MinValue,
            MaxValue = parameter.MaxValue
        };
    }
}
