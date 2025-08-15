using System.Collections.ObjectModel;
using ExamLab.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// 专项试卷模型 - 专门用于单一模块类型的专项练习试卷
/// </summary>
public class SpecializedExam : ReactiveObject
{
    /// <summary>
    /// 专项试卷ID
    /// </summary>
    [Reactive] public string Id { get; set; } = IdGeneratorService.GenerateExamId();

    /// <summary>
    /// 专项试卷名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 专项试卷描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 专项模块类型 - 专项试卷只包含一种模块类型
    /// </summary>
    [Reactive] public ModuleType ModuleType { get; set; } = ModuleType.Windows;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Reactive] public string CreatedTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [Reactive] public string LastModifiedTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 专项试卷包含的模块（通常只有一个）
    /// </summary>
    public ObservableCollection<ExamModule> Modules { get; set; } = new();

    /// <summary>
    /// 试卷总分
    /// </summary>
    [Reactive] public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [Reactive] public int Duration { get; set; } = 60; // 专项试卷默认时长较短

    /// <summary>
    /// 难度等级（1-5）
    /// </summary>
    [Reactive] public int DifficultyLevel { get; set; } = 1;

    /// <summary>
    /// 是否启用随机题目顺序
    /// </summary>
    [Reactive] public bool RandomizeQuestions { get; set; } = false;

    /// <summary>
    /// 专项试卷标签
    /// </summary>
    [Reactive] public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// 获取主要模块（专项试卷通常只有一个模块）
    /// </summary>
    public ExamModule? PrimaryModule => Modules.FirstOrDefault();

    /// <summary>
    /// 获取题目总数
    /// </summary>
    public int TotalQuestionCount => Modules.Sum(m => m.Questions.Count);

    /// <summary>
    /// 获取操作点总数
    /// </summary>
    public int TotalOperationPointCount => Modules.Sum(m => m.Questions.Sum(q => q.OperationPoints.Count));

    /// <summary>
    /// 获取模块类型的显示名称
    /// </summary>
    public string ModuleTypeName => ModuleType switch
    {
        ModuleType.Windows => "Windows操作",
        ModuleType.CSharp => "C#编程",
        ModuleType.PowerPoint => "PowerPoint操作",
        ModuleType.Excel => "Excel操作",
        ModuleType.Word => "Word操作",
        _ => "未知模块"
    };

    /// <summary>
    /// 创建专项试卷的默认模块
    /// </summary>
    public void CreateDefaultModule()
    {
        if (Modules.Count == 0)
        {
            ExamModule module = new()
            {
                Id = IdGeneratorService.GenerateModuleId(),
                Name = ModuleTypeName,
                Type = ModuleType,
                Description = GetDefaultModuleDescription(),
                Score = TotalScore,
                Order = 1,
                IsEnabled = true
            };

            Modules.Add(module);
        }
    }

    /// <summary>
    /// 获取默认模块描述
    /// </summary>
    private string GetDefaultModuleDescription()
    {
        return ModuleType switch
        {
            ModuleType.Windows => "Windows系统操作和文件管理相关题目",
            ModuleType.CSharp => "C#编程语言基础和应用开发题目",
            ModuleType.PowerPoint => "PowerPoint演示文稿制作和设计题目",
            ModuleType.Excel => "Excel电子表格操作和数据分析题目",
            ModuleType.Word => "Word文档编辑和排版设计题目",
            _ => "专项练习题目"
        };
    }

    /// <summary>
    /// 更新最后修改时间
    /// </summary>
    public void UpdateLastModifiedTime()
    {
        LastModifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 验证专项试卷数据
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (Modules.Count == 0)
            return false;

        // 专项试卷应该只包含一种模块类型
        if (Modules.Any(m => m.Type != ModuleType))
            return false;

        return true;
    }

    /// <summary>
    /// 克隆专项试卷
    /// </summary>
    public SpecializedExam Clone(string? newName = null)
    {
        SpecializedExam cloned = new()
        {
            Id = IdGeneratorService.GenerateExamId(),
            Name = newName ?? $"{Name} - 副本",
            Description = Description,
            ModuleType = ModuleType,
            TotalScore = TotalScore,
            Duration = Duration,
            DifficultyLevel = DifficultyLevel,
            RandomizeQuestions = RandomizeQuestions,
            Tags = Tags,
            CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            LastModifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 克隆模块
        foreach (ExamModule module in Modules)
        {
            ExamModule clonedModule = new()
            {
                Id = IdGeneratorService.GenerateModuleId(),
                Name = module.Name,
                Type = module.Type,
                Description = module.Description,
                Score = module.Score,
                Order = module.Order,
                IsEnabled = module.IsEnabled
            };

            // 克隆题目
            foreach (Question question in module.Questions)
            {
                Question clonedQuestion = new()
                {
                    Id = IdGeneratorService.GenerateQuestionId(),
                    Title = question.Title,
                    Description = question.Description,
                    Type = question.Type,
                    Score = question.Score,
                    Order = question.Order,
                    IsEnabled = question.IsEnabled
                };

                // 克隆操作点
                foreach (OperationPoint operationPoint in question.OperationPoints)
                {
                    OperationPoint clonedOperationPoint = new()
                    {
                        Id = IdGeneratorService.GenerateOperationPointId(),
                        Name = operationPoint.Name,
                        Description = operationPoint.Description,
                        Score = operationPoint.Score,
                        Order = operationPoint.Order,
                        IsRequired = operationPoint.IsRequired,
                        OperationType = operationPoint.OperationType
                    };

                    // 克隆参数
                    foreach (OperationParameter parameter in operationPoint.Parameters)
                    {
                        OperationParameter clonedParameter = new()
                        {
                            Id = IdGeneratorService.GenerateParameterId(),
                            Name = parameter.Name,
                            Type = parameter.Type,
                            Value = parameter.Value,
                            IsRequired = parameter.IsRequired,
                            Description = parameter.Description
                        };

                        clonedOperationPoint.Parameters.Add(clonedParameter);
                    }

                    clonedQuestion.OperationPoints.Add(clonedOperationPoint);
                }

                clonedModule.Questions.Add(clonedQuestion);
            }

            cloned.Modules.Add(clonedModule);
        }

        return cloned;
    }
}
