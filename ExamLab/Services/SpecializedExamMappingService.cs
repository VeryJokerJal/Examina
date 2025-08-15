using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ExamLab.Models;
using ExamLab.Models.ImportExport;
using ExamLab.Services;

namespace ExamLab.Services;

/// <summary>
/// 专项试卷映射服务 - 处理SpecializedExam与ExamExportDto之间的转换
/// </summary>
public static class SpecializedExamMappingService
{
    /// <summary>
    /// 将SpecializedExam转换为ExamExportDto
    /// </summary>
    /// <param name="specializedExam">专项试卷</param>
    /// <param name="exportLevel">导出级别</param>
    /// <returns>导出DTO</returns>
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
    /// 将ExamExportDto转换为SpecializedExam
    /// </summary>
    /// <param name="exportDto">导出DTO</param>
    /// <returns>专项试卷</returns>
    public static SpecializedExam FromExportDto(ExamExportDto exportDto)
    {
        if (exportDto?.Exam == null)
        {
            throw new ArgumentNullException(nameof(exportDto));
        }

        ExamDto examDto = exportDto.Exam;

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
        if (!string.IsNullOrEmpty((string?)exportDto.Exam.ExtendedConfig))
        {
            try
            {
                using System.Text.Json.JsonDocument doc = JsonDocument.Parse(exportDto.Exam.ExtendedConfig);
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
            Id = parameterDto.Id,
            Name = parameterDto.Name,
            DisplayName = parameterDto.DisplayName,
            Description = parameterDto.Description,
            Value = parameterDto.Value,
            DefaultValue = parameterDto.DefaultValue,
            IsRequired = parameterDto.IsRequired,
            Order = parameterDto.Order,
            IsEnabled = parameterDto.IsEnabled
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
}
