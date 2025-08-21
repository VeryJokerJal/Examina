using System.Collections.Generic;

namespace Examina.Models;

/// <summary>
/// 综合实训规则信息
/// </summary>
public class ComprehensiveTrainingRulesInfo
{
    /// <summary>
    /// 训练时长（分钟）
    /// </summary>
    public int DurationMinutes { get; set; } = 180;

    /// <summary>
    /// 总分值
    /// </summary>
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    public int PassingScore { get; set; } = 60;

    /// <summary>
    /// 题目总数
    /// </summary>
    public int TotalQuestions { get; set; } = 8;

    /// <summary>
    /// 编程题数量
    /// </summary>
    public int ProgrammingQuestions { get; set; } = 4;

    /// <summary>
    /// 编程题分值
    /// </summary>
    public int ProgrammingScore { get; set; } = 20;

    /// <summary>
    /// 操作题数量
    /// </summary>
    public int OperationQuestions { get; set; } = 4;

    /// <summary>
    /// 操作题分值
    /// </summary>
    public int OperationScore { get; set; } = 5;

    /// <summary>
    /// 训练规则列表
    /// </summary>
    public List<string> Rules { get; set; } = new()
    {
        "训练时间为3小时（180分钟），请合理安排训练时间",
        "训练总分100分，及格分数为60分",
        "题目来自综合实训题库，涵盖多个技能模块",
        "包含C#编程题4道（每道20分）和操作题4道（每道5分）",
        "题目按照模块分组，请按顺序完成各模块训练",
        "训练过程中请保持网络连接稳定",
        "训练开始后可以暂停，但建议连续完成以保持思路连贯",
        "系统会自动保存训练进度，避免意外丢失",
        "训练结束后可查看详细的成绩报告和技能评估"
    };

    /// <summary>
    /// 注意事项列表
    /// </summary>
    public List<string> Notes { get; set; } = new()
    {
        "请确保计算机性能良好，避免卡顿影响训练效果",
        "建议使用Chrome或Edge浏览器以获得最佳体验",
        "训练期间请关闭其他不必要的应用程序",
        "如遇到技术问题，请及时联系技术支持",
        "请认真对待每个训练环节，注重实际操作能力的提升",
        "训练结果将作为技能水平评估的重要参考"
    };

    /// <summary>
    /// 操作指南列表
    /// </summary>
    public List<string> OperationGuide { get; set; } = new()
    {
        "点击题目可查看详细内容和训练要求",
        "编程题需要在代码编辑器中完成代码编写和调试",
        "操作题需要按照步骤完成相应的实际操作任务",
        "可以随时切换题目，系统会自动保存当前进度",
        "建议按模块顺序完成训练，确保知识点的连贯性",
        "完成所有题目后，点击'提交训练'结束训练"
    };

    /// <summary>
    /// 训练特色说明
    /// </summary>
    public List<string> TrainingFeatures { get; set; } = new()
    {
        "模块化训练设计，循序渐进提升技能水平",
        "实战导向的题目设置，贴近实际工作场景",
        "智能评分系统，提供详细的技能分析报告",
        "支持多种编程语言和开发环境",
        "提供实时反馈和指导建议",
        "可重复训练，持续改进和提高"
    };
}
