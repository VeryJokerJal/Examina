using System;
using System.Collections.Generic;
using System.Linq;
using ExamLab.Models;
using ExamLab.Models.ImportExport;

namespace ExamLab.Services;

/// <summary>
/// 试卷映射服务 - 负责ExamLab模型与导入导出DTO之间的转换
/// </summary>
public static class ExamMappingService
{
    /// <summary>
    /// 将ExamLab的Exam模型转换为导出DTO
    /// </summary>
    /// <param name="exam">ExamLab试卷模型</param>
    /// <param name="exportLevel">导出级别</param>
    /// <returns>导出DTO</returns>
    public static ExamExportDto ToExportDto(Exam exam, ExportLevel exportLevel = ExportLevel.Complete)
    {
        ExamDto examDto = new()
        {
            Id = exam.Id,
            Name = exam.Name,
            Description = exam.Description,
            ExamType = "UnifiedExam", // ExamLab默认为统一考试
            Status = "Draft", // ExamLab默认为草稿状态
            TotalScore = exam.TotalScore,
            DurationMinutes = exam.Duration,
            CreatedAt = DateTime.TryParse(exam.CreatedTime, out DateTime createdTime) ? createdTime : DateTime.UtcNow,
            UpdatedAt = DateTime.TryParse(exam.LastModifiedTime, out DateTime modifiedTime) ? modifiedTime : null,
            IsEnabled = true,
            Tags = string.Join(",", GetExamTags(exam)),
            Modules = exam.Modules.Select(ToModuleDto).ToList()
        };

        // 根据导出级别决定包含的内容
        if (exportLevel == ExportLevel.Basic)
        {
            // 基本级别：清除详细配置信息
            foreach (ModuleDto module in examDto.Modules)
            {
                foreach (QuestionDto question in module.Questions)
                {
                    question.QuestionConfig = null;
                    question.AnswerValidationRules = null;
                    question.StandardAnswer = null;
                    question.ScoringRules = null;
                    question.OperationPoints.Clear();
                }
            }
        }
        else if (exportLevel == ExportLevel.WithoutAnswers)
        {
            // 不含答案级别：移除答案相关信息
            foreach (ModuleDto module in examDto.Modules)
            {
                foreach (QuestionDto question in module.Questions)
                {
                    question.StandardAnswer = null;
                    question.ScoringRules = null;
                }
            }
        }

        ExportMetadataDto metadata = new()
        {
            ExportDate = DateTime.UtcNow,
            ExportedBy = "ExamLab",
            TotalSubjects = exam.Modules.Count,
            TotalQuestions = exam.Modules.Sum(m => m.Questions.Count),
            TotalOperationPoints = exam.Modules.Sum(m => m.Questions.Sum(q => q.OperationPoints.Count)),
            ExportLevel = exportLevel.ToString()
        };

        return new ExamExportDto
        {
            Exam = examDto,
            Metadata = metadata
        };
    }

    /// <summary>
    /// 将导入DTO转换为ExamLab的Exam模型
    /// </summary>
    /// <param name="exportDto">导入的DTO</param>
    /// <returns>ExamLab试卷模型</returns>
    public static Exam FromExportDto(ExamExportDto exportDto)
    {
        ExamDto examDto = exportDto.Exam;

        Exam exam = new()
        {
            Id = string.IsNullOrEmpty(examDto.Id) ? GenerateExamId() : examDto.Id,
            Name = examDto.Name,
            Description = examDto.Description ?? string.Empty,
            TotalScore = (int)examDto.TotalScore,
            Duration = examDto.DurationMinutes,
            CreatedTime = examDto.CreatedAt.ToString("yyyy-MM-dd"),
            LastModifiedTime = (examDto.UpdatedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd")
        };

        // 转换模块
        foreach (ModuleDto moduleDto in examDto.Modules)
        {
            exam.Modules.Add(FromModuleDto(moduleDto));
        }

        // 如果没有模块但有科目，则从科目转换为模块
        if (exam.Modules.Count == 0 && examDto.Subjects.Count > 0)
        {
            foreach (SubjectDto subjectDto in examDto.Subjects)
            {
                exam.Modules.Add(FromSubjectDto(subjectDto));
            }
        }

        return exam;
    }

    /// <summary>
    /// 将ExamLab的ExamModule转换为ModuleDto
    /// </summary>
    private static ModuleDto ToModuleDto(ExamModule module)
    {
        return new ModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            Type = module.Type.ToString(),
            Description = module.Description,
            Score = module.Score,
            Order = module.Order,
            IsEnabled = module.IsEnabled,
            Questions = module.Questions.Select(ToQuestionDto).ToList()
        };
    }

    /// <summary>
    /// 将ModuleDto转换为ExamLab的ExamModule
    /// </summary>
    private static ExamModule FromModuleDto(ModuleDto moduleDto)
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
            module.Questions.Add(FromQuestionDto(questionDto));
        }

        return module;
    }

    /// <summary>
    /// 将SubjectDto转换为ExamLab的ExamModule
    /// </summary>
    private static ExamModule FromSubjectDto(SubjectDto subjectDto)
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
            module.Questions.Add(FromQuestionDto(questionDto));
        }

        return module;
    }

    /// <summary>
    /// 将ExamLab的Question转换为QuestionDto
    /// </summary>
    private static QuestionDto ToQuestionDto(Question question)
    {
        return new QuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Content = question.Content,
            QuestionType = GetQuestionTypeFromModule(question),
            Score = (decimal)question.TotalScore,
            DifficultyLevel = 1, // ExamLab没有难度级别，默认为1
            EstimatedMinutes = 5, // ExamLab没有预计时间，默认为5分钟
            SortOrder = question.Order,
            IsRequired = true, // ExamLab默认为必答
            Tags = string.Empty,
            Remarks = string.Empty,
            IsEnabled = question.IsEnabled,
            CreatedAt = DateTime.TryParse(question.CreatedTime, out DateTime createdTime) ? createdTime : DateTime.UtcNow,
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput,
            OperationPoints = question.OperationPoints.Select(ToOperationPointDto).ToList()
        };
    }

    /// <summary>
    /// 将QuestionDto转换为ExamLab的Question
    /// </summary>
    private static Question FromQuestionDto(QuestionDto questionDto)
    {
        Question question = new()
        {
            Id = questionDto.Id,
            Title = questionDto.Title,
            Content = questionDto.Content,

            Order = questionDto.SortOrder,
            IsEnabled = questionDto.IsEnabled,
            CreatedTime = questionDto.CreatedAt.ToString("yyyy-MM-dd"),
            ProgramInput = questionDto.ProgramInput,
            ExpectedOutput = questionDto.ExpectedOutput
        };

        // 转换操作点
        foreach (OperationPointDto operationPointDto in questionDto.OperationPoints)
        {
            question.OperationPoints.Add(FromOperationPointDto(operationPointDto));
        }

        return question;
    }

    /// <summary>
    /// 将ExamLab的OperationPoint转换为OperationPointDto
    /// </summary>
    private static OperationPointDto ToOperationPointDto(OperationPoint operationPoint)
    {
        return new OperationPointDto
        {
            Id = operationPoint.Id,
            Name = operationPoint.Name,
            Description = operationPoint.Description,
            ModuleType = operationPoint.ModuleType.ToString(),
            Score = operationPoint.Score,
            Order = operationPoint.Order,
            IsEnabled = operationPoint.IsEnabled,
            CreatedTime = operationPoint.CreatedTime,
            Parameters = operationPoint.Parameters.Select(ToParameterDto).ToList()
        };
    }

    /// <summary>
    /// 将OperationPointDto转换为ExamLab的OperationPoint
    /// </summary>
    private static OperationPoint FromOperationPointDto(OperationPointDto operationPointDto)
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
            operationPoint.Parameters.Add(FromParameterDto(parameterDto));
        }

        return operationPoint;
    }

    /// <summary>
    /// 将ExamLab的ConfigurationParameter转换为ParameterDto
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
            MaxValue = parameter.MaxValue,
            IsEnabled = parameter.IsEnabled
        };
    }

    /// <summary>
    /// 将ParameterDto转换为ExamLab的ConfigurationParameter
    /// </summary>
    private static ConfigurationParameter FromParameterDto(ParameterDto parameterDto)
    {
        ConfigurationParameter parameter = new()
        {
            Name = parameterDto.Name,
            DisplayName = parameterDto.DisplayName,
            Description = parameterDto.Description,
            Value = parameterDto.Value,
            DefaultValue = parameterDto.DefaultValue,
            IsRequired = parameterDto.IsRequired,
            Order = parameterDto.Order,
            EnumOptions = parameterDto.EnumOptions,
            ValidationRule = parameterDto.ValidationRule,
            ValidationErrorMessage = parameterDto.ValidationErrorMessage,
            MinValue = parameterDto.MinValue,
            MaxValue = parameterDto.MaxValue,
            IsEnabled = parameterDto.IsEnabled
        };

        // 解析参数类型
        if (Enum.TryParse<ParameterType>(parameterDto.Type, true, out ParameterType parameterType))
        {
            parameter.Type = parameterType;
        }

        return parameter;
    }

    /// <summary>
    /// 根据题目所属模块获取题目类型
    /// </summary>
    private static string GetQuestionTypeFromModule(Question question)
    {
        // 这里需要根据题目的上下文确定类型，暂时返回默认值
        return "Comprehensive";
    }

    /// <summary>
    /// 获取试卷标签
    /// </summary>
    private static List<string> GetExamTags(Exam exam)
    {
        List<string> tags = [];

        // 根据模块类型添加标签
        foreach (ExamModule module in exam.Modules)
        {
            if (module.IsEnabled)
            {
                tags.Add(module.Type.ToString());
            }
        }

        return tags;
    }

    /// <summary>
    /// 生成新的试卷ID
    /// </summary>
    private static string GenerateExamId()
    {
        return $"exam-{DateTime.Now.Ticks}";
    }
}

/// <summary>
/// 导出级别枚举
/// </summary>
public enum ExportLevel
{
    /// <summary>
    /// 基本信息 - 仅包含试卷和模块基本信息
    /// </summary>
    Basic,

    /// <summary>
    /// 完整内容但不含答案 - 包含题目但不含标准答案和评分规则
    /// </summary>
    WithoutAnswers,

    /// <summary>
    /// 完整内容 - 包含所有信息
    /// </summary>
    Complete
}
