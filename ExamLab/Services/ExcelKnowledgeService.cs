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
    /// 创建带有默认值的操作点
    /// </summary>
    public OperationPoint CreateOperationPoint(ExcelKnowledgeType type)
    {
        ExcelKnowledgeConfig? config = GetKnowledgeConfig(type);
        if (config == null)
        {
            throw new ArgumentException($"未找到Excel操作点 {type} 的配置");
        }

        OperationPoint operationPoint = new()
        {
            Id = IdGeneratorService.GenerateOperationId(),
            Name = config.Name,
            Description = config.Description,
            ModuleType = ModuleType.Excel,
            ExcelKnowledgeType = type,
            Score = 5.0,
            IsEnabled = true,
            CreatedTime = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        // 根据参数模板创建配置参数
        foreach (ConfigurationParameterTemplate template in config.ParameterTemplates)
        {
            ConfigurationParameter parameter = new()
            {
                Id = IdGeneratorService.GenerateParameterId(),
                Name = template.Name,
                DisplayName = template.DisplayName,
                Description = template.Description,
                Value = template.DefaultValue ?? "",
                Type = template.Type,
                IsRequired = template.IsRequired,
                DefaultValue = template.DefaultValue,
                EnumOptions = template.EnumOptions,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                Order = template.Order
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
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellValues", DisplayName = "目标区域个别单元格的值", Description = "用单元格+值的方式匹配，比如：E10：我的天啊", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:内容" }
            ]
        };

        // 操作点2：合并单元格
        configs[ExcelKnowledgeType.MergeCells] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.MergeCells,
            Name = "合并单元格",
            Description = "合并指定的单元格区域",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "起始单元和结束单元格", Description = "用:号分隔，添加合并操作", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:C1" }
            ]
        };

        // 操作点3：设置指定单元格字体
        configs[ExcelKnowledgeType.SetCellFont] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetCellFont,
            Name = "设置指定单元格字体",
            Description = "设置单元格区域的字体",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "起始单元格和结束单元格", Description = "单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "FontFamily", DisplayName = "字体", Description = "选择字体", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "宋体",
                    EnumOptions = "宋体,黑体,楷体,仿宋,微软雅黑,Arial,Times New Roman,Calibri" }
            ]
        };

        // 操作点4：设置单元格区域水平对齐方式
        configs[ExcelKnowledgeType.SetHorizontalAlignment] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetHorizontalAlignment,
            Name = "设置单元格区域水平对齐方式",
            Description = "设置单元格的水平对齐方式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "起始值", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "HorizontalAlignment", DisplayName = "水平对齐方式", Description = "选择对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "默认",
                    EnumOptions = "默认,左对齐,居中对齐,右对齐,填充,两端对齐,跨列居中,分散对齐" }
            ]
        };

        // 操作点5：内边框样式
        configs[ExcelKnowledgeType.SetInnerBorderStyle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetInnerBorderStyle,
            Name = "内边框样式",
            Description = "设置单元格区域的内边框样式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "包含起始值", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:C3" },
                new() { Name = "BorderStyle", DisplayName = "边框线样式", Description = "选择边框样式", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "无边框",
                    EnumOptions = "无边框,单实线,双线,点线,短划线,长划线,划线+点,划线+两个点,三线" }
            ]
        };

        // 操作点6：内边框颜色
        configs[ExcelKnowledgeType.SetInnerBorderColor] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetInnerBorderColor,
            Name = "内边框颜色",
            Description = "设置单元格区域的内边框颜色",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "包含起始值", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:C3" },
                new() { Name = "BorderColor", DisplayName = "边框线颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 4, DefaultValue = "#000000" }
            ]
        };

        // 操作点7：使用函数
        configs[ExcelKnowledgeType.UseFunction] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.UseFunction,
            Name = "使用函数",
            Description = "在单元格中使用Excel函数",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellAddress", DisplayName = "单元格", Description = "函数所在单元格", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "ExpectedValue", DisplayName = "期望值", Description = "函数计算的期望结果", Type = ParameterType.Text, IsRequired = true, Order = 4 },
                new() { Name = "FormulaContent", DisplayName = "公式内容", Description = "Excel函数公式", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };

        // 操作点8：设置行高
        configs[ExcelKnowledgeType.SetRowHeight] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetRowHeight,
            Name = "设置行高",
            Description = "设置指定行的高度",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "RowNumbers", DisplayName = "行数", Description = "可配置多个行号", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "1" },
                new() { Name = "RowHeight", DisplayName = "行高值", Description = "行高（磅为单位）", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "20", MinValue = 10, MaxValue = 200 }
            ]
        };

        // 操作点9：设置列宽
        configs[ExcelKnowledgeType.SetColumnWidth] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetColumnWidth,
            Name = "设置列宽",
            Description = "设置指定列的宽度",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "ColumnLetters", DisplayName = "列宽", Description = "可配置多个列字母", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A" },
                new() { Name = "ColumnWidth", DisplayName = "列宽值", Description = "列宽值", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "15", MinValue = 5, MaxValue = 100 }
            ]
        };

        // 操作点10：设置单元格填充颜色
        configs[ExcelKnowledgeType.SetCellFillColor] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetCellFillColor,
            Name = "设置单元格填充颜色",
            Description = "设置单元格的背景填充颜色",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "起始值", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "FillColor", DisplayName = "颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 4, DefaultValue = "#FFFF00" }
            ]
        };

        // 操作点11：设置垂直对齐方式
        configs[ExcelKnowledgeType.SetVerticalAlignment] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetVerticalAlignment,
            Name = "设置垂直对齐方式",
            Description = "设置单元格的垂直对齐方式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "起始值", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "VerticalAlignment", DisplayName = "垂直对齐方式", Description = "选择垂直对齐方式", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "顶端对齐",
                    EnumOptions = "顶端对齐,垂直居中对齐,底端对齐,两端对齐,分散对齐" }
            ]
        };

        // 操作点12：修改sheet表名称
        configs[ExcelKnowledgeType.ModifySheetName] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ModifySheetName,
            Name = "修改sheet表名称",
            Description = "修改工作表的名称",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "OriginalSheetName", DisplayName = "sheet表起始值", Description = "原始工作表名称", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "NewSheetName", DisplayName = "修改后的目标值", Description = "新的工作表名称", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点13：设置字型
        configs[ExcelKnowledgeType.SetFontStyle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetFontStyle,
            Name = "设置字型",
            Description = "设置单元格的字体样式（粗体、斜体等）",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "FontStyle", DisplayName = "字型", Description = "字体样式", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "常规",
                    EnumOptions = "常规,粗体,斜体,粗斜体" }
            ]
        };

        // 操作点14：设置字号
        configs[ExcelKnowledgeType.SetFontSize] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetFontSize,
            Name = "设置字号",
            Description = "设置单元格的字体大小",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "FontSize", DisplayName = "字号", Description = "字体大小", Type = ParameterType.Number, IsRequired = true, Order = 4, DefaultValue = "12", MinValue = 8, MaxValue = 72 }
            ]
        };

        // 操作点15：字体颜色
        configs[ExcelKnowledgeType.SetFontColor] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetFontColor,
            Name = "字体颜色",
            Description = "设置单元格的字体颜色",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "FontColor", DisplayName = "字体颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 4, DefaultValue = "#000000" }
            ]
        };

        // 操作点16：设置目标区域单元格数字分类格式
        configs[ExcelKnowledgeType.SetNumberFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetNumberFormat,
            Name = "设置目标区域单元格数字分类格式",
            Description = "设置单元格的数字格式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "NumberFormat", DisplayName = "数字格式", Description = "数字分类格式", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "常规",
                    EnumOptions = "常规,数值,货币,会计专用,日期,时间,百分比,分数,科学记数,文本,特殊,自定义" }
            ]
        };

        // 操作点17：设置图案填充样式
        configs[ExcelKnowledgeType.SetPatternFillStyle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetPatternFillStyle,
            Name = "设置图案填充样式",
            Description = "设置单元格的图案填充样式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "PatternStyle", DisplayName = "图案样式", Description = "图案填充样式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "无,实心,5%灰色,10%灰色,20%灰色,25%灰色,30%灰色,40%灰色,50%灰色,60%灰色,75%灰色,水平条纹,垂直条纹,反向对角条纹,对角条纹,对角十字线,粗对角十字线" }
            ]
        };

        // 操作点18：设置填充图案颜色
        configs[ExcelKnowledgeType.SetPatternFillColor] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetPatternFillColor,
            Name = "设置填充图案颜色",
            Description = "设置单元格填充图案的颜色",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:A1" },
                new() { Name = "PatternColor", DisplayName = "图案颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 4, DefaultValue = "#808080" }
            ]
        };

        // 操作点19：设置外边框样式
        configs[ExcelKnowledgeType.SetOuterBorderStyle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetOuterBorderStyle,
            Name = "设置外边框样式",
            Description = "设置单元格区域的外边框样式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "BorderStyle", DisplayName = "边框样式", Description = "外边框样式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "无边框,单实线,双线,点线,短划线,长划线,划线+点,划线+两个点,三线" }
            ]
        };

        // 操作点20：设置外边框颜色
        configs[ExcelKnowledgeType.SetOuterBorderColor] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetOuterBorderColor,
            Name = "设置外边框颜色",
            Description = "设置单元格区域的外边框颜色",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A1:C3" },
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "RGB颜色值", Type = ParameterType.Color, IsRequired = true, Order = 4, DefaultValue = "#000000" }
            ]
        };

        // 操作点21：添加下划线
        configs[ExcelKnowledgeType.AddUnderline] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.AddUnderline,
            Name = "添加下划线",
            Description = "为单元格文字添加下划线",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "UnderlineType", DisplayName = "下划线类型", Description = "下划线样式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "无,单下划线,双下划线,会计用单下划线,会计用双下划线" }
            ]
        };

        // 操作点22：条件格式
        configs[ExcelKnowledgeType.ConditionalFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ConditionalFormat,
            Name = "条件格式",
            Description = "设置单元格的条件格式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "ConditionType", DisplayName = "条件类型", Description = "条件格式类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "突出显示单元格规则,项目选取规则,数据条,色阶,图标集" },
                new() { Name = "ConditionValue", DisplayName = "条件值", Description = "条件判断值", Type = ParameterType.Text, IsRequired = true, Order = 5 },
                new() { Name = "FormatStyle", DisplayName = "格式样式", Description = "应用的格式样式", Type = ParameterType.Text, IsRequired = true, Order = 6 }
            ]
        };

        // 操作点23：设置单元格样式——数据
        configs[ExcelKnowledgeType.SetCellStyleData] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.SetCellStyleData,
            Name = "设置单元格样式——数据",
            Description = "设置单元格的数据样式",
            Category = "Excel基础操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "CellRange", DisplayName = "单元格区域", Description = "目标单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "StyleName", DisplayName = "样式名称", Description = "预定义的单元格样式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "常规,好,差,中性,输入,输出,计算,检查单元格,解释性文本,警告文本,标题1,标题2,标题3,标题4,20%强调文字色1,40%强调文字色1,60%强调文字色1" }
            ]
        };
    }

    private void InitializeDataListOperations(Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> configs)
    {
        // 操作点24：筛选
        configs[ExcelKnowledgeType.Filter] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.Filter,
            Name = "筛选",
            Description = "对数据进行筛选操作",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "FilterConditions", DisplayName = "筛选条件", Description = "键值对方式，哪一列：筛选的值", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A:条件值" }
            ]
        };

        // 操作点25：排序
        configs[ExcelKnowledgeType.Sort] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.Sort,
            Name = "排序",
            Description = "对数据进行排序操作",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "SortColumn", DisplayName = "哪一列", Description = "排序的列", Type = ParameterType.Text, IsRequired = true, Order = 3, DefaultValue = "A" },
                new() { Name = "SortOrder", DisplayName = "升序还是降序", Description = "排序顺序", Type = ParameterType.Enum, IsRequired = true, Order = 4, DefaultValue = "升序",
                    EnumOptions = "升序,降序" },
                new() { Name = "HasHeader", DisplayName = "是否包含标题", Description = "数据是否包含标题行", Type = ParameterType.Boolean, IsRequired = true, Order = 5, DefaultValue = "true" }
            ]
        };

        // 操作点26：数据透视表
        configs[ExcelKnowledgeType.PivotTable] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.PivotTable,
            Name = "数据透视表",
            Description = "创建和配置数据透视表",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "PivotRowFields", DisplayName = "行字段", Description = "设置为透视表的行字段", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "PivotColumnFields", DisplayName = "列字段", Description = "设置为透视表的列字段", Type = ParameterType.Text, IsRequired = false, Order = 4 },
                new() { Name = "PivotDataField", DisplayName = "数据字段", Description = "用于聚合的字段名称", Type = ParameterType.Text, IsRequired = true, Order = 5 },
                new() { Name = "PivotFunction", DisplayName = "聚合函数", Description = "聚合函数", Type = ParameterType.Enum, IsRequired = true, Order = 6,
                    EnumOptions = "Sum,Average,Count,Max,Min" },
                new() { Name = "PivotInsertCell", DisplayName = "插入位置", Description = "插入透视表的起始单元格位置", Type = ParameterType.Text, IsRequired = true, Order = 7 }
            ]
        };

        // 操作点27：分类汇总
        configs[ExcelKnowledgeType.Subtotal] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.Subtotal,
            Name = "分类汇总",
            Description = "对数据进行分类汇总",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "GroupByColumn", DisplayName = "分类字段", Description = "按哪一列分类", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "SummaryFunction", DisplayName = "汇总函数", Description = "汇总函数类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "求和,计数,平均值,最大值,最小值,乘积" },
                new() { Name = "SummaryColumn", DisplayName = "汇总字段", Description = "对哪一列进行汇总", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };

        // 操作点28：高级筛选-条件
        configs[ExcelKnowledgeType.AdvancedFilterCondition] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.AdvancedFilterCondition,
            Name = "高级筛选-条件",
            Description = "设置高级筛选的条件区域",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "ConditionRange", DisplayName = "条件区域", Description = "筛选条件的单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "FilterField", DisplayName = "筛选字段", Description = "要筛选的字段名", Type = ParameterType.Text, IsRequired = true, Order = 4 },
                new() { Name = "FilterValue", DisplayName = "筛选值", Description = "筛选条件值", Type = ParameterType.Text, IsRequired = true, Order = 5 }
            ]
        };

        // 操作点29：高级筛选-数据
        configs[ExcelKnowledgeType.AdvancedFilterData] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.AdvancedFilterData,
            Name = "高级筛选-数据",
            Description = "执行高级筛选操作",
            Category = "数据清单操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "A" },
                new() { Name = "DataRange", DisplayName = "数据区域", Description = "要筛选的数据区域", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "CriteriaRange", DisplayName = "条件区域", Description = "筛选条件区域", Type = ParameterType.Text, IsRequired = true, Order = 4 },
                new() { Name = "CopyToRange", DisplayName = "复制到", Description = "筛选结果复制到的区域", Type = ParameterType.Text, IsRequired = false, Order = 5 }
            ]
        };
    }

    private void InitializeChartOperations(Dictionary<ExcelKnowledgeType, ExcelKnowledgeConfig> configs)
    {
        // 操作点30：图表类型
        configs[ExcelKnowledgeType.ChartType] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartType,
            Name = "图表类型",
            Description = "设置图表的类型",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartType", DisplayName = "图表类型", Description = "选择图表类型", Type = ParameterType.Enum, IsRequired = true, Order = 3, DefaultValue = "簇状柱形图",
                    EnumOptions = "簇状柱形图,堆积柱形图,百分比堆积柱形图,簇状条形图,堆积条形图,百分比堆积条形图,折线图,带数据标记的折线图,饼图,分离型饼图,圆环图,面积图,散点图,气泡图,雷达图,曲面图,股票图,组合图" }
            ]
        };

        // 操作点31：图表样式
        configs[ExcelKnowledgeType.ChartStyle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartStyle,
            Name = "图表样式",
            Description = "设置图表的样式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "StyleNumber", DisplayName = "样式编号", Description = "图表样式编号（1-48）", Type = ParameterType.Number, IsRequired = true, Order = 3, DefaultValue = "1", MinValue = 1, MaxValue = 48 }
            ]
        };

        // 操作点32：图表标题
        configs[ExcelKnowledgeType.ChartTitle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartTitle,
            Name = "图表标题",
            Description = "设置图表的标题",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "ChartTitle", DisplayName = "图表标题", Description = "图表标题文本值", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点33：设置图例位置
        configs[ExcelKnowledgeType.LegendPosition] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.LegendPosition,
            Name = "设置图例位置",
            Description = "设置图表图例的位置",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "LegendPosition", DisplayName = "图例位置", Description = "图例显示位置", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "无图例,图表右侧,图表顶部,图表底部,图表左侧,顶端右侧重叠,图表区域中浮动" }
            ]
        };

        // 操作点34：图表移动
        configs[ExcelKnowledgeType.ChartMove] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartMove,
            Name = "图表移动",
            Description = "移动图表到指定位置",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "MoveLocation", DisplayName = "移动位置", Description = "图表移动的目标位置", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "新工作表,作为对象插入" },
                new() { Name = "TargetSheet", DisplayName = "目标工作表", Description = "移动到的工作表名称", Type = ParameterType.Text, IsRequired = false, Order = 5 }
            ]
        };

        // 操作点35：分类轴数据区域
        configs[ExcelKnowledgeType.CategoryAxisDataRange] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.CategoryAxisDataRange,
            Name = "分类轴数据区域",
            Description = "设置图表分类轴的数据区域",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "CategoryRange", DisplayName = "分类轴区域", Description = "分类轴数据的单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点36：数值轴数据区域
        configs[ExcelKnowledgeType.ValueAxisDataRange] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ValueAxisDataRange,
            Name = "数值轴数据区域",
            Description = "设置图表数值轴的数据区域",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "ValueRange", DisplayName = "数值轴区域", Description = "数值轴数据的单元格区域", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点37：图表标题格式
        configs[ExcelKnowledgeType.ChartTitleFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartTitleFormat,
            Name = "图表标题格式",
            Description = "设置图表标题的格式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "FontName", DisplayName = "字体", Description = "标题字体", Type = ParameterType.Text, IsRequired = false, Order = 4 },
                new() { Name = "FontSize", DisplayName = "字号", Description = "标题字号", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = 8, MaxValue = 72 },
                new() { Name = "FontColor", DisplayName = "字体颜色", Description = "标题字体颜色", Type = ParameterType.Color, IsRequired = false, Order = 6, DefaultValue = "#000000" }
            ]
        };

        // 操作点38：主要横坐标轴标题
        configs[ExcelKnowledgeType.HorizontalAxisTitle] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.HorizontalAxisTitle,
            Name = "主要横坐标轴标题",
            Description = "设置图表主要横坐标轴的标题",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "AxisTitle", DisplayName = "轴标题", Description = "横坐标轴标题文本", Type = ParameterType.Text, IsRequired = true, Order = 4 }
            ]
        };

        // 操作点39：主要横坐标轴标题格式
        configs[ExcelKnowledgeType.HorizontalAxisTitleFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.HorizontalAxisTitleFormat,
            Name = "主要横坐标轴标题格式",
            Description = "设置图表主要横坐标轴标题的格式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "FontName", DisplayName = "字体", Description = "轴标题字体", Type = ParameterType.Text, IsRequired = false, Order = 4 },
                new() { Name = "FontSize", DisplayName = "字号", Description = "轴标题字号", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = 8, MaxValue = 72 },
                new() { Name = "FontColor", DisplayName = "字体颜色", Description = "轴标题字体颜色", Type = ParameterType.Color, IsRequired = false, Order = 6, DefaultValue = "#000000" }
            ]
        };

        // 操作点40：设置图例格式
        configs[ExcelKnowledgeType.LegendFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.LegendFormat,
            Name = "设置图例格式",
            Description = "设置图表图例的格式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "FontName", DisplayName = "字体", Description = "图例字体", Type = ParameterType.Text, IsRequired = false, Order = 4 },
                new() { Name = "FontSize", DisplayName = "字号", Description = "图例字号", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = 8, MaxValue = 72 },
                new() { Name = "FontColor", DisplayName = "字体颜色", Description = "图例字体颜色", Type = ParameterType.Color, IsRequired = false, Order = 6, DefaultValue = "#000000" }
            ]
        };

        // 操作点41：设置主要纵坐标轴选项
        configs[ExcelKnowledgeType.VerticalAxisOptions] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.VerticalAxisOptions,
            Name = "设置主要纵坐标轴选项",
            Description = "设置图表主要纵坐标轴的选项",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "MinValue", DisplayName = "最小值", Description = "纵轴最小值", Type = ParameterType.Number, IsRequired = false, Order = 4 },
                new() { Name = "MaxValue", DisplayName = "最大值", Description = "纵轴最大值", Type = ParameterType.Number, IsRequired = false, Order = 5 },
                new() { Name = "MajorUnit", DisplayName = "主要刻度单位", Description = "主要刻度间隔", Type = ParameterType.Number, IsRequired = false, Order = 6 }
            ]
        };

        // 操作点42：设置网格线——主要横网格线
        configs[ExcelKnowledgeType.MajorHorizontalGridlines] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.MajorHorizontalGridlines,
            Name = "设置网格线——主要横网格线",
            Description = "设置图表的主要横网格线",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "GridlineVisible", DisplayName = "网格线可见性", Description = "是否显示主要横网格线", Type = ParameterType.Boolean, IsRequired = true, Order = 4 },
                new() { Name = "GridlineColor", DisplayName = "网格线颜色", Description = "网格线颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#808080" }
            ]
        };

        // 操作点43：设置网格线——次要横网格线
        configs[ExcelKnowledgeType.MinorHorizontalGridlines] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.MinorHorizontalGridlines,
            Name = "设置网格线——次要横网格线",
            Description = "设置图表的次要横网格线",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "GridlineVisible", DisplayName = "网格线可见性", Description = "是否显示次要横网格线", Type = ParameterType.Boolean, IsRequired = true, Order = 4 },
                new() { Name = "GridlineColor", DisplayName = "网格线颜色", Description = "网格线颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#808080" }
            ]
        };

        // 操作点44：主要纵网格线
        configs[ExcelKnowledgeType.MajorVerticalGridlines] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.MajorVerticalGridlines,
            Name = "主要纵网格线",
            Description = "设置图表的主要纵网格线",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "GridlineVisible", DisplayName = "网格线可见性", Description = "是否显示主要纵网格线", Type = ParameterType.Boolean, IsRequired = true, Order = 4 },
                new() { Name = "GridlineColor", DisplayName = "网格线颜色", Description = "网格线颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#808080" }
            ]
        };

        // 操作点45：次要纵网格线
        configs[ExcelKnowledgeType.MinorVerticalGridlines] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.MinorVerticalGridlines,
            Name = "次要纵网格线",
            Description = "设置图表的次要纵网格线",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "GridlineVisible", DisplayName = "网格线可见性", Description = "是否显示次要纵网格线", Type = ParameterType.Boolean, IsRequired = true, Order = 4 },
                new() { Name = "GridlineColor", DisplayName = "网格线颜色", Description = "网格线颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#808080" }
            ]
        };

        // 操作点46：设置数据系列格式
        configs[ExcelKnowledgeType.DataSeriesFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.DataSeriesFormat,
            Name = "设置数据系列格式",
            Description = "设置图表数据系列的格式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "SeriesIndex", DisplayName = "系列索引", Description = "数据系列索引", Type = ParameterType.Number, IsRequired = true, Order = 4, MinValue = 1 },
                new() { Name = "SeriesColor", DisplayName = "系列颜色", Description = "数据系列颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#0070C0" }
            ]
        };

        // 操作点47：添加数据标签
        configs[ExcelKnowledgeType.AddDataLabels] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.AddDataLabels,
            Name = "添加数据标签",
            Description = "为图表添加数据标签",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "LabelPosition", DisplayName = "标签位置", Description = "数据标签位置", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "居中,内侧结尾,内侧基底,外侧结尾,数据标注引导线" }
            ]
        };

        // 操作点48：设置数据标签格式
        configs[ExcelKnowledgeType.DataLabelsFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.DataLabelsFormat,
            Name = "设置数据标签格式",
            Description = "设置图表数据标签的格式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "FontName", DisplayName = "字体", Description = "数据标签字体", Type = ParameterType.Text, IsRequired = false, Order = 4 },
                new() { Name = "FontSize", DisplayName = "字号", Description = "数据标签字号", Type = ParameterType.Number, IsRequired = false, Order = 5, MinValue = 8, MaxValue = 72 },
                new() { Name = "FontColor", DisplayName = "字体颜色", Description = "数据标签字体颜色", Type = ParameterType.Color, IsRequired = false, Order = 6, DefaultValue = "#000000" }
            ]
        };

        // 操作点49：设置图表区域格式
        configs[ExcelKnowledgeType.ChartAreaFormat] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartAreaFormat,
            Name = "设置图表区域格式",
            Description = "设置图表区域的格式",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "FillColor", DisplayName = "填充颜色", Description = "图表区域填充颜色", Type = ParameterType.Color, IsRequired = false, Order = 4, DefaultValue = "#FFFFFF" },
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "图表区域边框颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#000000" }
            ]
        };

        // 操作点50：显示图表基底颜色
        configs[ExcelKnowledgeType.ChartFloorColor] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartFloorColor,
            Name = "显示图表基底颜色",
            Description = "设置3D图表的基底颜色",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "FloorColor", DisplayName = "基底颜色", Description = "图表基底颜色", Type = ParameterType.Color, IsRequired = true, Order = 4, DefaultValue = "#F2F2F2" }
            ]
        };

        // 操作点51：设置图表边框线
        configs[ExcelKnowledgeType.ChartBorder] = new ExcelKnowledgeConfig
        {
            KnowledgeType = ExcelKnowledgeType.ChartBorder,
            Name = "设置图表边框线",
            Description = "设置图表的边框线",
            Category = "图表操作",
            ParameterTemplates =
            [
                new() { Name = "TargetWorksheet", DisplayName = "目标工作表", Description = "目标工作表", Type = ParameterType.Text, IsRequired = true, Order = 1, DefaultValue = "Sheet1" },
                new() { Name = "OperationType", DisplayName = "操作类型", Description = "操作类型", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = "B" },
                new() { Name = "ChartNumber", DisplayName = "图表编号", Description = "图表编号", Type = ParameterType.Number, IsRequired = true, Order = 3, MinValue = 1 },
                new() { Name = "BorderStyle", DisplayName = "边框样式", Description = "边框线样式", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "无边框,单实线,双线,点线,虚线,粗线" },
                new() { Name = "BorderColor", DisplayName = "边框颜色", Description = "边框线颜色", Type = ParameterType.Color, IsRequired = false, Order = 5, DefaultValue = "#000000" }
            ]
        };
    }
}



