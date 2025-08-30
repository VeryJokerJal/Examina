using System.Text.Json;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// ExamLab与BenchSuite数据模型转换器
/// </summary>
public static class ExamModelConverter
{
    /// <summary>
    /// 从ExamLab ExamExportDto转换为BenchSuite ExamModel
    /// </summary>
    /// <param name="exportDto">ExamLab导出数据</param>
    /// <returns>BenchSuite试卷模型</returns>
    [Obsolete]
    public static ExamModel FromExamLabExport(dynamic exportDto)
    {
        try
        {
            // 解析JSON对象
            dynamic examData = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(exportDto));
            dynamic exam = examData.GetProperty("exam");

            string examId = GetStringProperty(exam, "id");
            string examName = GetStringProperty(exam, "name");
            string examDescription = GetStringProperty(exam, "description");

            // 调试输出
            Console.WriteLine($"[调试] 解析试卷信息: ID='{examId}', Name='{examName}', Description='{examDescription}'");

            ExamModel examModel = new()
            {
                Id = examId,
                Name = examName,
                Description = examDescription,

                // 映射ExamLab特有字段
                TotalScore = GetDoubleProperty(exam, "totalScore", 100.0),
                DurationMinutes = GetIntProperty(exam, "durationMinutes", 120),
                StartTime = GetDateTimeProperty(exam, "startTime"),
                EndTime = GetDateTimeProperty(exam, "endTime"),
                AllowRetake = GetBoolProperty(exam, "allowRetake", false),
                MaxRetakeCount = GetIntProperty(exam, "maxRetakeCount", 0),
                PassingScore = GetDoubleProperty(exam, "passingScore", 60.0),
                RandomizeQuestions = GetBoolProperty(exam, "randomizeQuestions", false),
                ShowScore = GetBoolProperty(exam, "showScore", true),
                ShowAnswers = GetBoolProperty(exam, "showAnswers", false),
                CreatedAt = GetDateTimeProperty(exam, "createdAt") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTimeProperty(exam, "updatedAt"),
                PublishedAt = GetDateTimeProperty(exam, "publishedAt"),
                IsEnabled = GetBoolProperty(exam, "isEnabled", true),
                Tags = GetStringProperty(exam, "tags"),
                ExamType = GetStringProperty(exam, "examType", "UnifiedExam"),
                Status = GetStringProperty(exam, "status", "Draft")
            };

            // 转换模块 (优先使用modules，回退到subjects)
            if (exam.TryGetProperty("modules", out JsonElement modules) && modules.ValueKind == JsonValueKind.Array)
            {
                examModel.Modules = [.. ConvertModulesFromExamLab(modules)];
            }
            else if (exam.TryGetProperty("subjects", out JsonElement subjects) && subjects.ValueKind == JsonValueKind.Array)
            {
                examModel.Modules = [.. ConvertSubjectsToModules(subjects)];
            }

            return examModel;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"转换ExamLab数据失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从BenchSuite ExamModel转换为ExamLab格式
    /// </summary>
    /// <param name="examModel">BenchSuite试卷模型</param>
    /// <param name="exportLevel">导出级别</param>
    /// <returns>ExamLab格式的JSON对象</returns>
    [Obsolete]
    public static object ToExamLabExport(ExamModel examModel, string exportLevel = "Complete")
    {
        var examDto = new
        {
            id = examModel.Id,
            name = examModel.Name,
            description = examModel.Description,
            examType = examModel.ExamType,
            status = examModel.Status,
            totalScore = examModel.TotalScore,
            durationMinutes = examModel.DurationMinutes,
            startTime = examModel.StartTime,
            endTime = examModel.EndTime,
            allowRetake = examModel.AllowRetake,
            maxRetakeCount = examModel.MaxRetakeCount,
            passingScore = examModel.PassingScore,
            randomizeQuestions = examModel.RandomizeQuestions,
            showScore = examModel.ShowScore,
            showAnswers = examModel.ShowAnswers,
            createdAt = examModel.CreatedAt,
            updatedAt = examModel.UpdatedAt,
            publishedAt = examModel.PublishedAt,
            isEnabled = examModel.IsEnabled,
            tags = examModel.Tags,
            extendedConfig = examModel.ExtendedConfig,
            modules = ConvertModulesToExamLab(examModel.Modules, exportLevel),
            subjects = new object[0] // 空数组，保持兼容性
        };

        var metadata = new
        {
            exportVersion = "2.0",
            exportDate = DateTime.UtcNow,
            exportedBy = "BenchSuite",
            totalSubjects = examModel.Modules.Count,
            totalQuestions = examModel.Modules.Sum(m => m.Questions.Count),
            totalOperationPoints = examModel.Modules.Sum(m => m.Questions.Sum(q => q.OperationPoints.Count)),
            exportLevel,
            exportFormat = "JSON"
        };

        return new
        {
            exam = examDto,
            metadata
        };
    }

    /// <summary>
    /// 转换ExamLab模块为BenchSuite模块
    /// </summary>
    [Obsolete]
    private static IEnumerable<ExamModuleModel> ConvertModulesFromExamLab(JsonElement modules)
    {
        foreach (JsonElement moduleElement in modules.EnumerateArray())
        {
            ExamModuleModel module = new()
            {
                Id = GetStringProperty(moduleElement, "id"),
                Name = GetStringProperty(moduleElement, "name"),
                Description = GetStringProperty(moduleElement, "description"),
                Score = GetDoubleProperty(moduleElement, "score", 0),
                Order = GetIntProperty(moduleElement, "order", 0),
                IsEnabled = GetBoolProperty(moduleElement, "isEnabled", true),

                // 映射ExamLab特有字段
                DurationMinutes = GetIntProperty(moduleElement, "durationMinutes", 30),
                Weight = GetDoubleProperty(moduleElement, "weight", 1.0),
                MinScore = GetNullableDecimalProperty(moduleElement, "minScore"),
                IsRequired = GetBoolProperty(moduleElement, "isRequired", true),
                ModuleConfig = GetStringProperty(moduleElement, "moduleConfig"),
                SubjectType = GetStringProperty(moduleElement, "subjectType")
            };

            // 解析模块类型
            string typeString = GetStringProperty(moduleElement, "type");
            if (Enum.TryParse(typeString, true, out ModuleType moduleType))
            {
                module.Type = moduleType;
            }

            // 转换题目
            if (moduleElement.TryGetProperty("questions", out JsonElement questions) && questions.ValueKind == JsonValueKind.Array)
            {
                module.Questions = [.. ConvertQuestionsFromExamLab(questions)];
            }

            yield return module;
        }
    }

    /// <summary>
    /// 转换ExamLab科目为BenchSuite模块
    /// </summary>
    [Obsolete]
    private static IEnumerable<ExamModuleModel> ConvertSubjectsToModules(JsonElement subjects)
    {
        foreach (JsonElement subjectElement in subjects.EnumerateArray())
        {
            ExamModuleModel module = new()
            {
                Id = GetIntProperty(subjectElement, "id", 0).ToString(),
                Name = GetStringProperty(subjectElement, "subjectName"),
                Description = GetStringProperty(subjectElement, "description"),
                Score = GetDoubleProperty(subjectElement, "score", 20.0),
                Order = GetIntProperty(subjectElement, "sortOrder", 1),
                IsEnabled = GetBoolProperty(subjectElement, "isEnabled", true),
                DurationMinutes = GetIntProperty(subjectElement, "durationMinutes", 30),
                Weight = GetDoubleProperty(subjectElement, "weight", 1.0),
                MinScore = GetNullableDecimalProperty(subjectElement, "minScore"),
                IsRequired = GetBoolProperty(subjectElement, "isRequired", true),
                SubjectType = GetStringProperty(subjectElement, "subjectType")
            };

            // 根据科目类型推断模块类型
            string subjectType = GetStringProperty(subjectElement, "subjectType").ToLowerInvariant();
            module.Type = subjectType switch
            {
                "powerpoint" or "ppt" => ModuleType.PowerPoint,
                "word" => ModuleType.Word,
                "excel" => ModuleType.Excel,
                "csharp" or "c#" => ModuleType.CSharp,
                _ => ModuleType.Windows
            };

            // 转换题目
            if (subjectElement.TryGetProperty("questions", out JsonElement questions) && questions.ValueKind == JsonValueKind.Array)
            {
                module.Questions = [.. ConvertQuestionsFromExamLab(questions)];
            }

            yield return module;
        }
    }

    /// <summary>
    /// 转换ExamLab题目为BenchSuite题目
    /// </summary>
    [Obsolete]
    private static IEnumerable<QuestionModel> ConvertQuestionsFromExamLab(JsonElement questions)
    {
        foreach (JsonElement questionElement in questions.EnumerateArray())
        {
            QuestionModel question = new()
            {
                Id = GetStringProperty(questionElement, "id"),
                Title = GetStringProperty(questionElement, "title"),
                Content = GetStringProperty(questionElement, "content"),
                Score = GetDoubleProperty(questionElement, "score", 10.0),
                Order = GetIntProperty(questionElement, "sortOrder", 1),
                IsEnabled = GetBoolProperty(questionElement, "isEnabled", true),

                // 映射ExamLab特有字段
                QuestionType = GetStringProperty(questionElement, "questionType", "Practical"),
                DifficultyLevel = GetIntProperty(questionElement, "difficultyLevel", 1),
                EstimatedMinutes = GetIntProperty(questionElement, "estimatedMinutes", 5),
                IsRequired = GetBoolProperty(questionElement, "isRequired", true),
                StandardAnswer = GetStringProperty(questionElement, "standardAnswer"),
                ScoringRules = GetStringProperty(questionElement, "scoringRules"),
                AnswerValidationRules = GetStringProperty(questionElement, "answerValidationRules"),
                QuestionConfig = GetStringProperty(questionElement, "questionConfig"),
                Tags = GetStringProperty(questionElement, "tags"),
                Remarks = GetStringProperty(questionElement, "remarks"),
                CreatedAt = GetDateTimeProperty(questionElement, "createdAt") ?? DateTime.UtcNow,
                UpdatedAt = GetDateTimeProperty(questionElement, "updatedAt"),
                ProgramInput = GetStringProperty(questionElement, "programInput"),
                ExpectedOutput = GetStringProperty(questionElement, "expectedOutput"),

                // C#编程题目特有字段
                CSharpQuestionType = GetStringProperty(questionElement, "csharpQuestionType"),
                CodeFilePath = GetStringProperty(questionElement, "codeFilePath"),
                CSharpDirectScore = GetNullableDoubleProperty(questionElement, "csharpDirectScore"),

                // Office文档题目特有字段
                DocumentFilePath = GetStringProperty(questionElement, "documentFilePath")
            };

            // 转换操作点
            if (questionElement.TryGetProperty("operationPoints", out JsonElement operationPoints) && operationPoints.ValueKind == JsonValueKind.Array)
            {
                question.OperationPoints = [.. ConvertOperationPointsFromExamLab(operationPoints)];
            }

            // 转换代码填空处
            if (questionElement.TryGetProperty("codeBlanks", out JsonElement codeBlanks) && codeBlanks.ValueKind == JsonValueKind.Array)
            {
                question.CodeBlanks = [.. ConvertCodeBlanksFromExamLab(codeBlanks)];
            }

            yield return question;
        }
    }

    /// <summary>
    /// 转换ExamLab操作点为BenchSuite操作点
    /// </summary>
    private static IEnumerable<OperationPointModel> ConvertOperationPointsFromExamLab(JsonElement operationPoints)
    {
        foreach (JsonElement opElement in operationPoints.EnumerateArray())
        {
            OperationPointModel operationPoint = new()
            {
                Id = GetStringProperty(opElement, "id"),
                Name = GetStringProperty(opElement, "name"),
                Description = GetStringProperty(opElement, "description"),
                Score = GetDoubleProperty(opElement, "score", 1.0),
                Order = GetIntProperty(opElement, "order", 1),
                IsEnabled = GetBoolProperty(opElement, "isEnabled", true),

                // 映射知识点类型
                PowerPointKnowledgeType = GetStringProperty(opElement, "powerPointKnowledgeType"),
                WordKnowledgeType = GetStringProperty(opElement, "wordKnowledgeType"),
                ExcelKnowledgeType = GetStringProperty(opElement, "excelKnowledgeType"),
                WindowsOperationType = GetStringProperty(opElement, "windowsOperationType"),

                // 映射时间字段
                CreatedTimeString = GetStringProperty(opElement, "createdTime"),
                OperationConfig = GetStringProperty(opElement, "operationConfig"),
                Tags = GetStringProperty(opElement, "tags")
            };

            // 解析模块类型
            string moduleTypeString = GetStringProperty(opElement, "moduleType");
            if (Enum.TryParse(moduleTypeString, true, out ModuleType moduleType))
            {
                operationPoint.ModuleType = moduleType;
            }

            // 转换参数
            if (opElement.TryGetProperty("parameters", out JsonElement parameters) && parameters.ValueKind == JsonValueKind.Array)
            {
                operationPoint.Parameters = [.. ConvertParametersFromExamLab(parameters)];
            }

            yield return operationPoint;
        }
    }

    /// <summary>
    /// 转换ExamLab参数为BenchSuite参数
    /// </summary>
    private static IEnumerable<ConfigurationParameterModel> ConvertParametersFromExamLab(JsonElement parameters)
    {
        foreach (JsonElement paramElement in parameters.EnumerateArray())
        {
            ConfigurationParameterModel parameter = new()
            {
                Id = GetStringProperty(paramElement, "id"),
                Name = GetStringProperty(paramElement, "name"),
                DisplayName = GetStringProperty(paramElement, "displayName"),
                Value = GetStringProperty(paramElement, "value"),
                IsRequired = GetBoolProperty(paramElement, "isRequired", false),

                // 映射ExamLab特有字段
                DefaultValue = GetStringProperty(paramElement, "defaultValue"),
                ValidationRules = GetStringProperty(paramElement, "validationRules"),
                Description = GetStringProperty(paramElement, "description"),
                Order = GetIntProperty(paramElement, "order", 0),
                IsVisible = GetBoolProperty(paramElement, "isVisible", true)
            };

            // 解析参数类型
            string typeString = GetStringProperty(paramElement, "type");
            if (Enum.TryParse(typeString, true, out ParameterType parameterType))
            {
                parameter.Type = parameterType;
            }

            // 解析选项列表
            if (paramElement.TryGetProperty("options", out JsonElement options) && options.ValueKind == JsonValueKind.Array)
            {
                // 处理 JSON 数组格式的选项
                parameter.Options = [.. options.EnumerateArray().Select(o => o.GetString() ?? string.Empty)];
            }
            else if (paramElement.TryGetProperty("enumOptions", out JsonElement enumOptions) && enumOptions.ValueKind == JsonValueKind.String)
            {
                // 处理 ExamLab 的 EnumOptions 字符串格式，应用智能解析逻辑
                string enumOptionsString = enumOptions.GetString() ?? string.Empty;
                parameter.Options = ParseEnumOptionsString(enumOptionsString);
            }

            yield return parameter;
        }
    }

    /// <summary>
    /// 转换BenchSuite模块为ExamLab格式
    /// </summary>
    [Obsolete]
    private static object[] ConvertModulesToExamLab(List<ExamModuleModel> modules, string exportLevel)
    {
        return modules.Select(module => new
        {
            id = module.Id,
            name = module.Name,
            type = module.Type.ToString(),
            description = module.Description,
            score = (int)module.Score,
            order = module.Order,
            isEnabled = module.IsEnabled,
            durationMinutes = module.DurationMinutes,
            weight = module.Weight,
            minScore = module.MinScore,
            isRequired = module.IsRequired,
            moduleConfig = module.ModuleConfig,
            subjectType = module.SubjectType,
            questions = ConvertQuestionsToExamLab(module.Questions, exportLevel)
        }).ToArray();
    }

    /// <summary>
    /// 转换BenchSuite题目为ExamLab格式
    /// </summary>
    [Obsolete]
    private static object[] ConvertQuestionsToExamLab(List<QuestionModel> questions, string exportLevel)
    {
        return questions.Select(question =>
        {
            var questionObj = new
            {
                id = question.Id,
                title = question.Title,
                content = question.Content,
                questionType = question.QuestionType,
                score = question.Score,
                difficultyLevel = question.DifficultyLevel,
                estimatedMinutes = question.EstimatedMinutes,
                sortOrder = question.Order,
                isRequired = question.IsRequired,
                isEnabled = question.IsEnabled,
                tags = question.Tags,
                remarks = question.Remarks,
                createdAt = question.CreatedAt,
                updatedAt = question.UpdatedAt,
                programInput = question.ProgramInput,
                expectedOutput = question.ExpectedOutput,

                // C#编程题目特有字段
                csharpQuestionType = question.CSharpQuestionType,
                codeFilePath = question.CodeFilePath,
                csharpDirectScore = question.CSharpDirectScore,
                codeBlanks = question.CodeBlanks != null ? ConvertCodeBlanksToExamLab(question.CodeBlanks) : null,

                // Office文档题目特有字段
                documentFilePath = question.DocumentFilePath,

                operationPoints = ConvertOperationPointsToExamLab(question.OperationPoints),

                // 根据导出级别决定是否包含答案
                standardAnswer = exportLevel == "Complete" ? question.StandardAnswer : null,
                scoringRules = exportLevel == "Complete" ? question.ScoringRules : null,
                answerValidationRules = exportLevel == "Complete" ? question.AnswerValidationRules : null,
                questionConfig = question.QuestionConfig
            };

            return questionObj;
        }).ToArray();
    }

    /// <summary>
    /// 转换BenchSuite操作点为ExamLab格式
    /// </summary>
    private static object[] ConvertOperationPointsToExamLab(List<OperationPointModel> operationPoints)
    {
        return operationPoints.Select(op => new
        {
            id = op.Id,
            name = op.Name,
            description = op.Description,
            moduleType = op.ModuleType.ToString(),
            powerPointKnowledgeType = op.PowerPointKnowledgeType,
            wordKnowledgeType = op.WordKnowledgeType,
            excelKnowledgeType = op.ExcelKnowledgeType,
            windowsOperationType = op.WindowsOperationType,
            score = op.Score,
            order = op.Order,
            isEnabled = op.IsEnabled,
            createdTime = op.CreatedTimeString,
            operationConfig = op.OperationConfig,
            tags = op.Tags,
            parameters = ConvertParametersToExamLab(op.Parameters)
        }).ToArray();
    }

    /// <summary>
    /// 转换BenchSuite参数为ExamLab格式
    /// </summary>
    private static object[] ConvertParametersToExamLab(List<ConfigurationParameterModel> parameters)
    {
        return parameters.Select(param => new
        {
            id = param.Id,
            name = param.Name,
            displayName = param.DisplayName,
            value = param.Value,
            type = param.Type.ToString(),
            isRequired = param.IsRequired,
            defaultValue = param.DefaultValue,
            validationRules = param.ValidationRules,
            description = param.Description,
            options = param.Options.ToArray(),
            enumOptions = ConvertOptionsToEnumString(param.Options), // 为 ExamLab 兼容性添加
            order = param.Order,
            isVisible = param.IsVisible
        }).ToArray();
    }

    // === 辅助方法 ===

    private static string GetStringProperty(JsonElement element, string propertyName, string defaultValue = "")
    {
        if (element.TryGetProperty(propertyName, out JsonElement prop))
        {
            Console.WriteLine($"[调试] 属性 '{propertyName}' 存在，类型: {prop.ValueKind}");

            if (prop.ValueKind == JsonValueKind.String)
            {
                string? value = prop.GetString();
                Console.WriteLine($"[调试] 属性 '{propertyName}' 值: '{value}'");
                return value ?? defaultValue;
            }
            else
            {
                Console.WriteLine($"[调试] 属性 '{propertyName}' 不是字符串类型");
                return defaultValue;
            }
        }
        else
        {
            Console.WriteLine($"[调试] 属性 '{propertyName}' 不存在");
            return defaultValue;
        }
    }

    private static int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
    {
        return element.TryGetProperty(propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : defaultValue;
    }

    private static decimal GetDecimalProperty(JsonElement element, string propertyName, decimal defaultValue = 0)
    {
        return element.TryGetProperty(propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetDecimal()
            : defaultValue;
    }

    private static decimal? GetNullableDecimalProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetDecimal()
            : null;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue = false)
    {
        return element.TryGetProperty(propertyName, out JsonElement prop)
            ? prop.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => defaultValue
            }
            : defaultValue;
    }

    private static DateTime? GetDateTimeProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement prop) && prop.ValueKind == JsonValueKind.String)
        {
            string dateString = prop.GetString() ?? string.Empty;
            return DateTime.TryParse(dateString, out DateTime result) ? result : null;
        }
        return null;
    }

    /// <summary>
    /// 解析 ExamLab 的 EnumOptions 字符串，特殊处理页码格式等包含逗号的选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>解析后的选项列表</returns>
    private static List<string> ParseEnumOptionsString(string enumOptions)
    {
        if (string.IsNullOrEmpty(enumOptions))
        {
            return [];
        }

        // 特殊处理页码格式：识别 "数字,数字,数字..." 这样的模式
        if (IsPageNumberFormatOptions(enumOptions))
        {
            return ParsePageNumberFormatOptions(enumOptions);
        }

        // 默认按逗号分割
        return [.. enumOptions.Split(',').Select(s => s.Trim())];
    }

    /// <summary>
    /// 判断是否为页码格式选项
    /// </summary>
    /// <param name="enumOptions">枚举选项字符串</param>
    /// <returns>是否为页码格式选项</returns>
    private static bool IsPageNumberFormatOptions(string enumOptions)
    {
        // 检查是否包含页码格式的特征模式
        return enumOptions.Contains("1,2,3...") ||
               enumOptions.Contains("a,b,c...") ||
               enumOptions.Contains("A,B,C...") ||
               enumOptions.Contains("i,ii,iii...") ||
               enumOptions.Contains("I,II,III...");
    }

    /// <summary>
    /// 解析页码格式选项
    /// </summary>
    /// <param name="enumOptions">页码格式选项字符串</param>
    /// <returns>解析后的页码格式选项列表</returns>
    private static List<string> ParsePageNumberFormatOptions(string enumOptions)
    {
        List<string> options = [];

        // 定义页码格式模式
        string[] patterns = ["1,2,3...", "a,b,c...", "A,B,C...", "i,ii,iii...", "I,II,III..."];

        string remaining = enumOptions;

        foreach (string pattern in patterns)
        {
            if (remaining.Contains(pattern))
            {
                options.Add(pattern);
                // 从剩余字符串中移除已处理的模式
                remaining = remaining.Replace(pattern, "").Replace(",,", ",");
            }
        }

        // 处理剩余的选项（如果有的话）
        if (!string.IsNullOrEmpty(remaining))
        {
            string[] remainingOptions = remaining.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string option in remainingOptions)
            {
                string trimmed = option.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !options.Contains(trimmed))
                {
                    options.Add(trimmed);
                }
            }
        }

        return options;
    }

    /// <summary>
    /// 将选项列表转换为 ExamLab 兼容的 EnumOptions 字符串
    /// </summary>
    /// <param name="options">选项列表</param>
    /// <returns>逗号分隔的选项字符串</returns>
    private static string ConvertOptionsToEnumString(List<string> options)
    {
        if (options == null || options.Count == 0)
        {
            return string.Empty;
        }

        // 直接用逗号连接，因为选项已经是正确解析的格式
        return string.Join(",", options);
    }

    /// <summary>
    /// 转换ExamLab代码填空处为BenchSuite代码填空处
    /// </summary>
    private static IEnumerable<CodeBlankModel> ConvertCodeBlanksFromExamLab(JsonElement codeBlanks)
    {
        foreach (JsonElement codeBlankElement in codeBlanks.EnumerateArray())
        {
            CodeBlankModel codeBlank = new()
            {
                Id = GetStringProperty(codeBlankElement, "id"),
                Name = GetStringProperty(codeBlankElement, "name"),
                Description = GetStringProperty(codeBlankElement, "description"),
                Score = GetDoubleProperty(codeBlankElement, "score", 1.0),
                Order = GetIntProperty(codeBlankElement, "order", 1),
                IsEnabled = GetBoolProperty(codeBlankElement, "isEnabled", true),
                StandardAnswer = GetStringProperty(codeBlankElement, "standardAnswer"),
                CreatedTime = GetStringProperty(codeBlankElement, "createdTime")
            };

            yield return codeBlank;
        }
    }

    /// <summary>
    /// 获取可空的double属性值
    /// </summary>
    private static double? GetNullableDoubleProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property))
        {
            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDouble();
            }
            else if (property.ValueKind == JsonValueKind.String)
            {
                string? stringValue = property.GetString();
                if (double.TryParse(stringValue, out double result))
                {
                    return result;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 获取double属性值
    /// </summary>
    private static double GetDoubleProperty(JsonElement element, string propertyName, double defaultValue = 0.0)
    {
        return GetNullableDoubleProperty(element, propertyName) ?? defaultValue;
    }

    /// <summary>
    /// 转换BenchSuite代码填空处为ExamLab格式
    /// </summary>
    private static object[] ConvertCodeBlanksToExamLab(List<CodeBlankModel> codeBlanks)
    {
        return codeBlanks.Select(codeBlank => new
        {
            id = codeBlank.Id,
            name = codeBlank.Name,
            description = codeBlank.Description,
            score = codeBlank.Score,
            order = codeBlank.Order,
            isEnabled = codeBlank.IsEnabled,
            standardAnswer = codeBlank.StandardAnswer,
            createdTime = codeBlank.CreatedTime
        }).ToArray();
    }
}
