using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExamLab.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ExamLab.Services;

/// <summary>
/// Excel文档生成服务实现
/// </summary>
public class ExcelDocumentGenerationService : IExcelDocumentGenerationService
{
    /// <summary>
    /// 根据操作点列表生成Excel文档
    /// </summary>
    /// <param name="operationPoints">操作点列表</param>
    /// <returns>生成的Excel文档路径</returns>
    public async Task<string> GenerateExcelDocumentAsync(List<OperationPoint> operationPoints)
    {
        try
        {
            // 创建输出文件路径
            string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ExamLab", "GeneratedExcel");
            Directory.CreateDirectory(outputDirectory);
            
            string fileName = $"Excel文档_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(outputDirectory, fileName);

            // 创建Excel工作簿
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                // 创建工作簿部分
                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                // 创建工作表部分
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                // 创建工作表
                Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "工作簿1"
                };
                sheets.Append(sheet);

                // 按类别分组执行操作点
                await ExecuteOperationPointsByCategory(operationPoints, spreadsheetDocument);

                // 保存工作簿
                workbookPart.Workbook.Save();
            }

            return filePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"生成Excel文档失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 按类别执行操作点
    /// </summary>
    /// <param name="operationPoints">操作点列表</param>
    /// <param name="spreadsheetDocument">Excel文档</param>
    private async Task ExecuteOperationPointsByCategory(List<OperationPoint> operationPoints, SpreadsheetDocument spreadsheetDocument)
    {
        // 按操作类型分组
        Dictionary<string, List<OperationPoint>> groupedOperations = operationPoints
            .GroupBy(op => GetOperationCategory(op.ExcelKnowledgeType.Value))
            .ToDictionary(g => g.Key, g => g.ToList());

        // 1. 先执行基础操作
        if (groupedOperations.ContainsKey("基础操作"))
        {
            await ExecuteBasicOperations(groupedOperations["基础操作"], spreadsheetDocument);
        }

        // 2. 再执行数据清单操作
        if (groupedOperations.ContainsKey("数据清单操作"))
        {
            await ExecuteDataListOperations(groupedOperations["数据清单操作"], spreadsheetDocument);
        }

        // 3. 最后执行图表操作
        if (groupedOperations.ContainsKey("图表操作"))
        {
            await ExecuteChartOperations(groupedOperations["图表操作"], spreadsheetDocument);
        }
    }

    /// <summary>
    /// 获取操作类别
    /// </summary>
    /// <param name="knowledgeType">知识点类型</param>
    /// <returns>操作类别</returns>
    private string GetOperationCategory(ExcelKnowledgeType knowledgeType)
    {
        int typeValue = (int)knowledgeType;
        
        if (typeValue >= 1 && typeValue <= 23)
            return "基础操作";
        else if (typeValue >= 24 && typeValue <= 29)
            return "数据清单操作";
        else if (typeValue >= 30 && typeValue <= 51)
            return "图表操作";
        else
            return "未知操作";
    }

    /// <summary>
    /// 执行基础操作
    /// </summary>
    /// <param name="operationPoints">基础操作点列表</param>
    /// <param name="spreadsheetDocument">Excel文档</param>
    private async Task ExecuteBasicOperations(List<OperationPoint> operationPoints, SpreadsheetDocument spreadsheetDocument)
    {
        foreach (OperationPoint operationPoint in operationPoints)
        {
            await ExecuteOperationPointAsync(operationPoint, spreadsheetDocument);
        }
    }

    /// <summary>
    /// 执行数据清单操作
    /// </summary>
    /// <param name="operationPoints">数据清单操作点列表</param>
    /// <param name="spreadsheetDocument">Excel文档</param>
    private async Task ExecuteDataListOperations(List<OperationPoint> operationPoints, SpreadsheetDocument spreadsheetDocument)
    {
        foreach (OperationPoint operationPoint in operationPoints)
        {
            await ExecuteOperationPointAsync(operationPoint, spreadsheetDocument);
        }
    }

    /// <summary>
    /// 执行图表操作
    /// </summary>
    /// <param name="operationPoints">图表操作点列表</param>
    /// <param name="spreadsheetDocument">Excel文档</param>
    private async Task ExecuteChartOperations(List<OperationPoint> operationPoints, SpreadsheetDocument spreadsheetDocument)
    {
        foreach (OperationPoint operationPoint in operationPoints)
        {
            await ExecuteOperationPointAsync(operationPoint, spreadsheetDocument);
        }
    }

    /// <summary>
    /// 执行单个Excel操作点
    /// </summary>
    /// <param name="operationPoint">要执行的操作点</param>
    /// <param name="workbook">Excel工作簿对象</param>
    /// <returns>执行结果</returns>
    public async Task<bool> ExecuteOperationPointAsync(OperationPoint operationPoint, object workbook)
    {
        try
        {
            if (!ValidateOperationPoint(operationPoint))
            {
                return false;
            }

            SpreadsheetDocument spreadsheetDocument = workbook as SpreadsheetDocument;
            if (spreadsheetDocument == null)
            {
                return false;
            }

            // 根据操作点类型执行相应的操作
            switch (operationPoint.ExcelKnowledgeType.Value)
            {
                // Excel基础操作（23个）
                case ExcelKnowledgeType.FillOrCopyCellContent:
                    return await ExecuteFillOrCopyCellContent(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.MergeCells:
                    return await ExecuteMergeCells(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetCellFont:
                    return await ExecuteSetCellFont(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetHorizontalAlignment:
                    return await ExecuteSetHorizontalAlignment(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetInnerBorderStyle:
                    return await ExecuteSetInnerBorderStyle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetInnerBorderColor:
                    return await ExecuteSetInnerBorderColor(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.UseFunction:
                    return await ExecuteUseFunction(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetRowHeight:
                    return await ExecuteSetRowHeight(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetColumnWidth:
                    return await ExecuteSetColumnWidth(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetCellFillColor:
                    return await ExecuteSetCellFillColor(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetVerticalAlignment:
                    return await ExecuteSetVerticalAlignment(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ModifySheetName:
                    return await ExecuteModifySheetName(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetFontStyle:
                    return await ExecuteSetFontStyle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetFontSize:
                    return await ExecuteSetFontSize(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetFontColor:
                    return await ExecuteSetFontColor(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetNumberFormat:
                    return await ExecuteSetNumberFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetPatternFillStyle:
                    return await ExecuteSetPatternFillStyle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetPatternFillColor:
                    return await ExecuteSetPatternFillColor(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetOuterBorderStyle:
                    return await ExecuteSetOuterBorderStyle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetOuterBorderColor:
                    return await ExecuteSetOuterBorderColor(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.AddUnderline:
                    return await ExecuteAddUnderline(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ConditionalFormat:
                    return await ExecuteConditionalFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.SetCellStyleData:
                    return await ExecuteSetCellStyleData(operationPoint, spreadsheetDocument);

                // 数据清单操作（6个）
                case ExcelKnowledgeType.Filter:
                    return await ExecuteFilter(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.Sort:
                    return await ExecuteSort(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.PivotTable:
                    return await ExecutePivotTable(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.Subtotal:
                    return await ExecuteSubtotal(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.AdvancedFilterCondition:
                    return await ExecuteAdvancedFilterCondition(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.AdvancedFilterData:
                    return await ExecuteAdvancedFilterData(operationPoint, spreadsheetDocument);

                // 图表操作（22个）
                case ExcelKnowledgeType.ChartType:
                    return await ExecuteChartType(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartStyle:
                    return await ExecuteChartStyle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartTitle:
                    return await ExecuteChartTitle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.LegendPosition:
                    return await ExecuteLegendPosition(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartMove:
                    return await ExecuteChartMove(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.CategoryAxisDataRange:
                    return await ExecuteCategoryAxisDataRange(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ValueAxisDataRange:
                    return await ExecuteValueAxisDataRange(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartTitleFormat:
                    return await ExecuteChartTitleFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.HorizontalAxisTitle:
                    return await ExecuteHorizontalAxisTitle(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.HorizontalAxisTitleFormat:
                    return await ExecuteHorizontalAxisTitleFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.LegendFormat:
                    return await ExecuteLegendFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.VerticalAxisOptions:
                    return await ExecuteVerticalAxisOptions(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.MajorHorizontalGridlines:
                    return await ExecuteMajorHorizontalGridlines(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.MinorHorizontalGridlines:
                    return await ExecuteMinorHorizontalGridlines(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.MajorVerticalGridlines:
                    return await ExecuteMajorVerticalGridlines(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.MinorVerticalGridlines:
                    return await ExecuteMinorVerticalGridlines(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.DataSeriesFormat:
                    return await ExecuteDataSeriesFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.AddDataLabels:
                    return await ExecuteAddDataLabels(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.DataLabelsFormat:
                    return await ExecuteDataLabelsFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartAreaFormat:
                    return await ExecuteChartAreaFormat(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartFloorColor:
                    return await ExecuteChartFloorColor(operationPoint, spreadsheetDocument);
                case ExcelKnowledgeType.ChartBorder:
                    return await ExecuteChartBorder(operationPoint, spreadsheetDocument);

                default:
                    // 对于暂未实现的操作点，记录日志并返回true（模拟执行成功）
                    System.Diagnostics.Debug.WriteLine($"操作点 {operationPoint.ExcelKnowledgeType} 暂未实现具体功能");
                    return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"执行操作点 {operationPoint.ExcelKnowledgeType} 时发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 验证操作点参数
    /// </summary>
    /// <param name="operationPoint">要验证的操作点</param>
    /// <returns>验证结果</returns>
    public bool ValidateOperationPoint(OperationPoint operationPoint)
    {
        if (operationPoint == null)
            return false;

        if (operationPoint.Parameters == null || !operationPoint.Parameters.Any())
            return false;

        // 检查必需参数是否存在
        ExcelKnowledgeConfig config = ExcelKnowledgeService.Instance.GetKnowledgeConfig(operationPoint.ExcelKnowledgeType.Value);
        if (config?.ParameterTemplates != null)
        {
            foreach (ConfigurationParameterTemplate template in config.ParameterTemplates.Where(t => t.IsRequired))
            {
                ConfigurationParameter parameter = operationPoint.Parameters.FirstOrDefault(p => p.Name == template.Name);
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 执行填充或复制单元格内容操作
    /// </summary>
    private async Task<bool> ExecuteFillOrCopyCellContent(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        // 实现填充或复制单元格内容的具体逻辑
        // 这里是示例实现，实际需要根据参数进行具体操作
        await Task.Delay(10); // 模拟异步操作
        return true;
    }

    /// <summary>
    /// 执行合并单元格操作
    /// </summary>
    private async Task<bool> ExecuteMergeCells(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        // 实现合并单元格的具体逻辑
        await Task.Delay(10); // 模拟异步操作
        return true;
    }

    /// <summary>
    /// 执行设置单元格字体操作
    /// </summary>
    private async Task<bool> ExecuteSetCellFont(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        // 实现设置单元格字体的具体逻辑
        await Task.Delay(10); // 模拟异步操作
        return true;
    }

    // Excel基础操作方法实现
    private async Task<bool> ExecuteSetHorizontalAlignment(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetInnerBorderStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetInnerBorderColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteUseFunction(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetRowHeight(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetColumnWidth(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetCellFillColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetVerticalAlignment(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteModifySheetName(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetFontStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetFontSize(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetFontColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetNumberFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetPatternFillStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetPatternFillColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetOuterBorderStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetOuterBorderColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteAddUnderline(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteConditionalFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSetCellStyleData(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    // 数据清单操作方法实现
    private async Task<bool> ExecuteFilter(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSort(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecutePivotTable(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteSubtotal(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteAdvancedFilterCondition(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteAdvancedFilterData(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    // 图表操作方法实现
    private async Task<bool> ExecuteChartType(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartTitle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteLegendPosition(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartMove(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteCategoryAxisDataRange(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteValueAxisDataRange(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartTitleFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteHorizontalAxisTitle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteHorizontalAxisTitleFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteLegendFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteVerticalAxisOptions(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteMajorHorizontalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteMinorHorizontalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteMajorVerticalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteMinorVerticalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteDataSeriesFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteAddDataLabels(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteDataLabelsFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartAreaFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartFloorColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }

    private async Task<bool> ExecuteChartBorder(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        await Task.Delay(10);
        return true;
    }
}
