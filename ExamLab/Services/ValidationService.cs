using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// 验证服务
/// </summary>
public static class ValidationService
{
    /// <summary>
    /// 验证试卷
    /// </summary>
    public static ValidationResult ValidateExam(Exam exam)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(exam.Name))
        {
            errors.Add("试卷名称不能为空");
        }

        if (exam.Duration <= 0)
        {
            errors.Add("考试时长必须大于0");
        }

        if (exam.Modules.Count == 0)
        {
            errors.Add("试卷必须包含至少一个模块");
        }

        foreach (ExamModule module in exam.Modules)
        {
            ValidationResult moduleResult = ValidateModule(module);
            if (!moduleResult.IsValid)
            {
                errors.AddRange(moduleResult.Errors.Select(e => $"模块 '{module.Name}': {e}"));
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证模块
    /// </summary>
    public static ValidationResult ValidateModule(ExamModule module)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(module.Name))
        {
            errors.Add("模块名称不能为空");
        }

        if (module.Score < 0)
        {
            errors.Add("模块分值不能为负数");
        }

        if (module.Questions.Count == 0)
        {
            errors.Add("模块必须包含至少一个题目");
        }

        foreach (Question question in module.Questions)
        {
            ValidationResult questionResult = ValidateQuestion(question);
            if (!questionResult.IsValid)
            {
                errors.AddRange(questionResult.Errors.Select(e => $"题目 '{question.Title}': {e}"));
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证题目
    /// </summary>
    public static ValidationResult ValidateQuestion(Question question)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(question.Title))
        {
            errors.Add("题目标题不能为空");
        }

        if (string.IsNullOrWhiteSpace(question.Content))
        {
            errors.Add("题目内容不能为空");
        }

        if (question.Score < 0)
        {
            errors.Add("题目分值不能为负数");
        }

        if (question.OperationPoints.Count == 0)
        {
            errors.Add("题目必须包含至少一个操作点");
        }

        foreach (OperationPoint operationPoint in question.OperationPoints)
        {
            ValidationResult operationResult = ValidateOperationPoint(operationPoint);
            if (!operationResult.IsValid)
            {
                errors.AddRange(operationResult.Errors.Select(e => $"操作点 '{operationPoint.Name}': {e}"));
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证操作点
    /// </summary>
    public static ValidationResult ValidateOperationPoint(OperationPoint operationPoint)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(operationPoint.Name))
        {
            errors.Add("操作点名称不能为空");
        }

        if (operationPoint.Score < 0)
        {
            errors.Add("操作点分值不能为负数");
        }

        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
        {
            ValidationResult parameterResult = ValidateParameter(parameter);
            if (!parameterResult.IsValid)
            {
                errors.AddRange(parameterResult.Errors.Select(e => $"参数 '{parameter.DisplayName}': {e}"));
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证配置参数
    /// </summary>
    public static ValidationResult ValidateParameter(ConfigurationParameter parameter)
    {
        List<string> errors = [];

        if (parameter.IsRequired && string.IsNullOrWhiteSpace(parameter.Value))
        {
            errors.Add("必填参数不能为空");
        }

        if (!string.IsNullOrWhiteSpace(parameter.Value))
        {
            switch (parameter.Type)
            {
                case ParameterType.Number:
                    if (!double.TryParse(parameter.Value, out double numberValue))
                    {
                        errors.Add("数值格式不正确");
                    }
                    else
                    {
                        if (parameter.MinValue.HasValue && numberValue < parameter.MinValue.Value)
                        {
                            errors.Add($"数值不能小于 {parameter.MinValue.Value}");
                        }
                        if (parameter.MaxValue.HasValue && numberValue > parameter.MaxValue.Value)
                        {
                            errors.Add($"数值不能大于 {parameter.MaxValue.Value}");
                        }
                    }
                    break;

                case ParameterType.Boolean:
                    if (!bool.TryParse(parameter.Value, out _))
                    {
                        errors.Add("布尔值格式不正确");
                    }
                    break;

                case ParameterType.Enum:
                    if (!string.IsNullOrEmpty(parameter.EnumOptions))
                    {
                        List<string> options = parameter.EnumOptionsList;
                        if (!options.Contains(parameter.Value))
                        {
                            errors.Add($"枚举值必须是以下选项之一：{string.Join(", ", options)}");
                        }
                    }
                    break;

                case ParameterType.Color:
                    // 简单的颜色格式验证
                    if (!IsValidColorFormat(parameter.Value))
                    {
                        errors.Add("颜色格式不正确，请使用 #RRGGBB 格式");
                    }
                    break;
            }

            // 验证规则检查
            if (!string.IsNullOrEmpty(parameter.ValidationRule))
            {
                try
                {
                    if (!Regex.IsMatch(parameter.Value, parameter.ValidationRule))
                    {
                        errors.Add(parameter.ValidationErrorMessage ?? "参数格式不符合要求");
                    }
                }
                catch (Exception)
                {
                    errors.Add("验证规则格式错误");
                }
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证颜色格式
    /// </summary>
    private static bool IsValidColorFormat(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return false;
        }

        // 支持 #RGB 和 #RRGGBB 格式
        return Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public List<string> Errors { get; }

    public ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors ?? [];
    }

    public string GetErrorMessage()
    {
        return string.Join("\n", Errors);
    }
}
