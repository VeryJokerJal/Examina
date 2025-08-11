using System;
using System.Collections.Generic;
using System.Linq;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// Excel知识点配置服务
/// </summary>
public class ExcelKnowledgeService
{
    public static ExcelKnowledgeService Instance { get; } = new();

    private readonly Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> _knowledgeConfigs;

    private ExcelKnowledgeService()
    {
        _knowledgeConfigs = InitializeKnowledgeConfigs();
    }

    /// <summary>
    /// 获取所有知识点配置
    /// </summary>
    public IEnumerable<ExcelKnowledgeConfig> GetAllKnowledgeConfigs()
    {
        return _knowledgeConfigs.Values;
    }

    /// <summary>
    /// 根据类型获取知识点配置
    /// </summary>
    public ExcelKnowledgeConfig? GetKnowledgeConfig(ExcelKnowledgeType type)
    {
        return _knowledgeConfigs.TryGetValue(type, out ExcelKnowledgeConfig? config) ? config : null;
    }

    /// <summary>
    /// 根据知识点配置创建操作点
    /// </summary>
    public OperationPoint CreateOperationPoint(ExcelKnowledgeType type)
    {
        ExcelKnowledgeConfig? config = GetKnowledgeConfig(type);
        if (config == null)
        {
            throw new ArgumentException($"未找到知识点类型 {type} 的配置");
        }

        OperationPoint operationPoint = new()
        {
            Name = config.Name,
            Description = config.Description,
            ModuleType = ModuleType.Excel,
            ExcelKnowledgeType = type
        };

        // 根据参数模板创建配置参数
        foreach (ConfigurationParameterTemplate template in config.ParameterTemplates)
        {
            ConfigurationParameter parameter = new()
            {
                Name = template.Name,
                DisplayName = template.DisplayName,
                Description = template.Description,
                Type = template.Type,
                IsRequired = template.IsRequired,
                Order = template.Order,
                EnumOptions = template.EnumOptions,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                DefaultValue = template.DefaultValue
            };

            operationPoint.Parameters.Add(parameter);
        }

        return operationPoint;
    }

    private Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> InitializeKnowledgeConfigs()
    {
        Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> configs = [];

        // 第一类：Excel基础操作
        InitializeBasicOperations(configs);

        // 第二类：数据清单操作
        InitializeDataListOperations(configs);

        // 第三类：图表操作
        InitializeChartOperations(configs);

        return configs;
    }

    private void InitializeBasicOperations(Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> configs)
    {
        // 操作点1：填充或复制单元格内容
        configs[ExcelKnowledgeType.FillOrCopyCellContent] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.FillOrCopyCellContent,
            Name = "填充或复制单元格内容",
            Description = "通过判断单元格内的值，来鉴定是否完成操作",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellValues", DisplayName = "目标区域个别单元格的值", Description = "用单元格+值的方式匹配，比如：E10：我的天啊", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点4：合并单元格
        configs[ExcelKnowledgeType.MergeCells] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.MergeCells,
            Name = "合并单元格",
            Description = "合并指定的单元格区域",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "起始单元和结束单元格", Description = "用:号分隔，添加合并操作", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点6：设置指定单元格字体
        configs[ExcelKnowledgeType.SetCellFont] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetCellFont,
            Name = "设置指定单元格字体",
            Description = "设置单元格区域的字体",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "起始单元格和结束单元格", Description = "单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "FontFamily", DisplayName = "字体", Description = "选择字体", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };

        // 操作点13：设置单元格区域水平对齐方式
        configs[ExcelKnowledgeType.SetHorizontalAlignment] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetHorizontalAlignment,
            Name = "设置单元格区域水平对齐方式",
            Description = "设置单元格的水平对齐方式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "起始值", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "默认,左对齐,居中对齐,右对齐,填充,两端对齐,跨列居中,分散对齐" },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };

        // 操作点15：使用函数
        configs[ExcelKnowledgeType.UseFunction] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.UseFunction,
            Name = "使用函数",
            Description = "在单元格中使用Excel函数",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellAddress", DisplayName = "单元格", Description = "函数所在单元格", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "ExpectedValue", DisplayName = "期望值", Description = "函数计算的期望结果", Type = ParameterType.Text, IsRequired = true, Order = 4 },
                new() { Name = "FormulaContent", DisplayName = "公式内容", Description = "Excel函数公式", Type = ParameterType.Text, IsRequired = true, Order = 5 },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 6 }
            ]
        };
    }

    private void InitializeDataListOperations(Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> configs)
    {
        // 操作点31：筛选
        configs[ExcelKnowledgeType.Filter] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.Filter,
            Name = "筛选",
            Description = "对数据进行筛选操作",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "FilterConditions", DisplayName = "筛选条件", Description = "键值对方式，哪一列：筛选的值", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点71：数据透视表
        configs[ExcelKnowledgeType.PivotTable] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.PivotTable,
            Name = "数据透视表",
            Description = "创建和配置数据透视表",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "PivotRowFields", DisplayName = "行字段", Description = "设置为透视表的行字段", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "PivotColumnFields", DisplayName = "列字段", Description = "设置为透视表的列字段", Type = ParameterType.Text, IsRequired = false, Order = 4 },
                new() { Name = "PivotDataField", DisplayName = "数据字段", Description = "用于聚合的字段名称", Type = ParameterType.Text, IsRequired = true, Order = 5 },
                new() { Name = "PivotFunction", DisplayName = "聚合函数", Description = "聚合函数", Type = ParameterType.Enum, IsRequired = true, Order = 6,
                    EnumOptions = "Sum,Average,Count,Max,Min" },
                new() { Name = "PivotInsertCell", DisplayName = "插入位置", Description = "插入透视表的起始单元格位置", Type = ParameterType.Text, IsRequired = true, Order = 7 },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 8 }
            ]
        };
    }

    private void InitializeChartOperations(Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> configs)
    {
        // 操作点101：图表类型
        configs[ExcelKnowledgeType.ChartType] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartType,
            Name = "图表类型",
            Description = "设置图表的类型",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartType", DisplayName = "图表类型", Description = "选择图表类型", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "簇状柱形图,堆积柱形图,百分比堆积柱形图,簇状条形图,堆积条形图,百分比堆积条形图,折线图,带数据标记的折线图,饼图,分离型饼图,圆环图,面积图,散点图,气泡图,雷达图,曲面图,股票图,组合图" },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点107：图表标题
        configs[ExcelKnowledgeType.ChartTitle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartTitle,
            Name = "图表标题",
            Description = "设置图表的标题",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorkbook", DisplayName = "目标图表", Description = "目标工作簿", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "ChartTitle", DisplayName = "图表标题", Description = "图表标题文本值", Type = ParameterType.Text, IsRequired = true, Order = 4 },
                new() { Name = "Description", DisplayName = "文本题目描述", Description = "题目描述", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };
    }
}
