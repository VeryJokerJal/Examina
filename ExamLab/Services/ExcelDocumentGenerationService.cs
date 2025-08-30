using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExamLab.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;
using System.Globalization;

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
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellValues = GetParameterValue(operationPoint, "CellValues", "");

            if (string.IsNullOrEmpty(cellValues))
                return false;

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 解析单元格值格式：A1:内容
            string[] cellValuePairs = cellValues.Split(',');
            foreach (string cellValuePair in cellValuePairs)
            {
                string[] parts = cellValuePair.Split(':');
                if (parts.Length == 2)
                {
                    string cellAddress = parts[0].Trim();
                    string cellValue = parts[1].Trim();

                    SetCellValue(worksheetPart, cellAddress, cellValue);
                }
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteFillOrCopyCellContent error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行合并单元格操作
    /// </summary>
    private async Task<bool> ExecuteMergeCells(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;

            // 获取或创建MergeCells元素
            MergeCells? mergeCells = worksheet.Elements<MergeCells>().FirstOrDefault();
            if (mergeCells == null)
            {
                mergeCells = new MergeCells();
                SheetData? sheetData = worksheet.Elements<SheetData>().FirstOrDefault();
                if (sheetData != null)
                {
                    worksheet.InsertAfter(mergeCells, sheetData);
                }
                else
                {
                    worksheet.AppendChild(mergeCells);
                }
            }

            // 创建合并单元格
            MergeCell mergeCell = new MergeCell() { Reference = cellRange };
            mergeCells.Append(mergeCell);

            // 更新合并单元格计数
            mergeCells.Count = (uint)mergeCells.Elements<MergeCell>().Count();

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteMergeCells error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 执行设置单元格字体操作
    /// </summary>
    private async Task<bool> ExecuteSetCellFont(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string fontFamily = GetParameterValue(operationPoint, "FontFamily", "宋体");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 创建字体
            Font font = new Font();
            font.Append(new FontName() { Val = fontFamily });
            font.Append(new FontSize() { Val = 11 });

            // 添加字体到字体集合
            Fonts fonts = stylesheet.Fonts ?? new Fonts();
            if (stylesheet.Fonts == null)
                stylesheet.Fonts = fonts;

            fonts.Append(font);
            fonts.Count = (uint)fonts.Elements<Font>().Count();

            uint fontIndex = fonts.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FontId = fontIndex,
                ApplyFont = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetCellFont error: {ex.Message}");
            return false;
        }
    }

    // Excel基础操作方法实现
    private async Task<bool> ExecuteSetHorizontalAlignment(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string horizontalAlignment = GetParameterValue(operationPoint, "HorizontalAlignment", "默认");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射对齐方式
            HorizontalAlignmentValues alignmentValue = MapHorizontalAlignment(horizontalAlignment);

            // 创建对齐设置
            Alignment alignment = new Alignment()
            {
                Horizontal = alignmentValue
            };

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                Alignment = alignment,
                ApplyAlignment = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetHorizontalAlignment error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetInnerBorderStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:C3");
            string borderStyle = GetParameterValue(operationPoint, "BorderStyle", "无边框");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射边框样式
            BorderStyleValues borderStyleValue = MapBorderStyle(borderStyle);

            // 创建边框
            Border border = new Border();
            border.Append(new LeftBorder() { Style = borderStyleValue });
            border.Append(new RightBorder() { Style = borderStyleValue });
            border.Append(new TopBorder() { Style = borderStyleValue });
            border.Append(new BottomBorder() { Style = borderStyleValue });
            border.Append(new DiagonalBorder());

            // 添加边框到边框集合
            Borders borders = stylesheet.Borders ?? new Borders();
            if (stylesheet.Borders == null)
                stylesheet.Borders = borders;

            borders.Append(border);
            borders.Count = (uint)borders.Elements<Border>().Count();

            uint borderIndex = borders.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                BorderId = borderIndex,
                ApplyBorder = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetInnerBorderStyle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetInnerBorderColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:C3");
            string borderColor = GetParameterValue(operationPoint, "BorderColor", "#000000");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 解析颜色
            string colorValue = ParseColor(borderColor);

            // 创建边框
            Border border = new Border();

            LeftBorder leftBorder = new LeftBorder() { Style = BorderStyleValues.Thin };
            leftBorder.Append(new Color() { Rgb = colorValue });
            border.Append(leftBorder);

            RightBorder rightBorder = new RightBorder() { Style = BorderStyleValues.Thin };
            rightBorder.Append(new Color() { Rgb = colorValue });
            border.Append(rightBorder);

            TopBorder topBorder = new TopBorder() { Style = BorderStyleValues.Thin };
            topBorder.Append(new Color() { Rgb = colorValue });
            border.Append(topBorder);

            BottomBorder bottomBorder = new BottomBorder() { Style = BorderStyleValues.Thin };
            bottomBorder.Append(new Color() { Rgb = colorValue });
            border.Append(bottomBorder);

            border.Append(new DiagonalBorder());

            // 添加边框到边框集合
            Borders borders = stylesheet.Borders ?? new Borders();
            if (stylesheet.Borders == null)
                stylesheet.Borders = borders;

            borders.Append(border);
            borders.Count = (uint)borders.Elements<Border>().Count();

            uint borderIndex = borders.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                BorderId = borderIndex,
                ApplyBorder = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetInnerBorderColor error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteUseFunction(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellAddress = GetParameterValue(operationPoint, "CellAddress", "A1");
            string formulaContent = GetParameterValue(operationPoint, "FormulaContent", "");
            string expectedValue = GetParameterValue(operationPoint, "ExpectedValue", "");

            if (string.IsNullOrEmpty(formulaContent))
                return false;

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData == null)
            {
                sheetData = new SheetData();
                worksheet.AppendChild(sheetData);
            }

            // 获取或创建单元格
            Cell cell = GetCell(sheetData, cellAddress);

            // 设置公式
            if (!formulaContent.StartsWith("="))
                formulaContent = "=" + formulaContent;

            cell.CellFormula = new CellFormula(formulaContent);
            cell.DataType = new EnumValue<CellValues>(CellValues.Number);

            // 如果提供了期望值，也设置单元格值
            if (!string.IsNullOrEmpty(expectedValue))
            {
                cell.CellValue = new CellValue(expectedValue);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteUseFunction error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetRowHeight(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string rowNumbers = GetParameterValue(operationPoint, "RowNumbers", "1");
            string rowHeightStr = GetParameterValue(operationPoint, "RowHeight", "20");

            if (!double.TryParse(rowHeightStr, out double rowHeight))
                rowHeight = 20;

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData? sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData == null)
            {
                sheetData = new SheetData();
                worksheet.AppendChild(sheetData);
            }

            // 解析行号
            string[] rowNumberArray = rowNumbers.Split(',');
            foreach (string rowNumberStr in rowNumberArray)
            {
                if (uint.TryParse(rowNumberStr.Trim(), out uint rowIndex))
                {
                    Row? row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
                    if (row == null)
                    {
                        row = new Row() { RowIndex = rowIndex };
                        sheetData.Append(row);
                    }

                    row.Height = rowHeight;
                    row.CustomHeight = true;
                }
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetRowHeight error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetColumnWidth(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string columnLetters = GetParameterValue(operationPoint, "ColumnLetters", "A");
            string columnWidthStr = GetParameterValue(operationPoint, "ColumnWidth", "15");

            if (!double.TryParse(columnWidthStr, out double columnWidth))
                columnWidth = 15;

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;

            // 获取或创建列设置
            Columns? columns = worksheet.Elements<Columns>().FirstOrDefault();
            if (columns == null)
            {
                columns = new Columns();
                SheetData? sheetData = worksheet.GetFirstChild<SheetData>();
                if (sheetData != null)
                {
                    worksheet.InsertBefore(columns, sheetData);
                }
                else
                {
                    worksheet.AppendChild(columns);
                }
            }

            // 解析列字母
            string[] columnLetterArray = columnLetters.Split(',');
            foreach (string columnLetter in columnLetterArray)
            {
                uint columnIndex = GetColumnIndex(columnLetter.Trim());

                Column column = new Column()
                {
                    Min = columnIndex,
                    Max = columnIndex,
                    Width = columnWidth,
                    CustomWidth = true
                };

                columns.Append(column);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetColumnWidth error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetCellFillColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string fillColor = GetParameterValue(operationPoint, "FillColor", "#FFFF00");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 解析颜色
            string colorValue = ParseColor(fillColor);

            // 创建填充
            Fill fill = new Fill();
            PatternFill patternFill = new PatternFill()
            {
                PatternType = PatternValues.Solid
            };
            patternFill.Append(new ForegroundColor() { Rgb = colorValue });
            fill.Append(patternFill);

            // 添加填充到填充集合
            Fills fills = stylesheet.Fills ?? new Fills();
            if (stylesheet.Fills == null)
                stylesheet.Fills = fills;

            fills.Append(fill);
            fills.Count = (uint)fills.Elements<Fill>().Count();

            uint fillIndex = fills.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FillId = fillIndex,
                ApplyFill = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetCellFillColor error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetVerticalAlignment(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string verticalAlignment = GetParameterValue(operationPoint, "VerticalAlignment", "顶端对齐");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射对齐方式
            VerticalAlignmentValues alignmentValue = MapVerticalAlignment(verticalAlignment);

            // 创建对齐设置
            Alignment alignment = new Alignment()
            {
                Vertical = alignmentValue
            };

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                Alignment = alignment,
                ApplyAlignment = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetVerticalAlignment error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteModifySheetName(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string originalSheetName = GetParameterValue(operationPoint, "OriginalSheetName", "");
            string newSheetName = GetParameterValue(operationPoint, "NewSheetName", "");

            if (string.IsNullOrEmpty(originalSheetName) || string.IsNullOrEmpty(newSheetName))
                return false;

            WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart;
            if (workbookPart?.Workbook?.Sheets == null)
                return false;

            // 查找要重命名的工作表
            Sheet? sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
                .FirstOrDefault(s => s.Name?.Value == originalSheetName);

            if (sheet == null)
            {
                // 如果找不到原始名称，尝试使用目标工作表名称
                sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
                    .FirstOrDefault(s => s.Name?.Value == targetWorksheet);
            }

            if (sheet != null)
            {
                sheet.Name = newSheetName;
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteModifySheetName error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetFontStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string fontStyle = GetParameterValue(operationPoint, "FontStyle", "常规");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 创建字体
            Font font = new Font();
            font.Append(new FontName() { Val = "Calibri" });
            font.Append(new FontSize() { Val = 11 });

            // 根据字体样式设置属性
            switch (fontStyle)
            {
                case "粗体":
                    font.Append(new Bold());
                    break;
                case "斜体":
                    font.Append(new Italic());
                    break;
                case "粗斜体":
                    font.Append(new Bold());
                    font.Append(new Italic());
                    break;
            }

            // 添加字体到字体集合
            Fonts fonts = stylesheet.Fonts ?? new Fonts();
            if (stylesheet.Fonts == null)
                stylesheet.Fonts = fonts;

            fonts.Append(font);
            fonts.Count = (uint)fonts.Elements<Font>().Count();

            uint fontIndex = fonts.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FontId = fontIndex,
                ApplyFont = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetFontStyle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetFontSize(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string fontSizeStr = GetParameterValue(operationPoint, "FontSize", "12");

            if (!double.TryParse(fontSizeStr, out double fontSize))
                fontSize = 12;

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 创建字体
            Font font = new Font();
            font.Append(new FontName() { Val = "Calibri" });
            font.Append(new FontSize() { Val = fontSize });

            // 添加字体到字体集合
            Fonts fonts = stylesheet.Fonts ?? new Fonts();
            if (stylesheet.Fonts == null)
                stylesheet.Fonts = fonts;

            fonts.Append(font);
            fonts.Count = (uint)fonts.Elements<Font>().Count();

            uint fontIndex = fonts.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FontId = fontIndex,
                ApplyFont = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetFontSize error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetFontColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string fontColor = GetParameterValue(operationPoint, "FontColor", "#000000");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 解析颜色
            string colorValue = ParseColor(fontColor);

            // 创建字体
            Font font = new Font();
            font.Append(new FontName() { Val = "Calibri" });
            font.Append(new FontSize() { Val = 11 });
            font.Append(new Color() { Rgb = colorValue });

            // 添加字体到字体集合
            Fonts fonts = stylesheet.Fonts ?? new Fonts();
            if (stylesheet.Fonts == null)
                stylesheet.Fonts = fonts;

            fonts.Append(font);
            fonts.Count = (uint)fonts.Elements<Font>().Count();

            uint fontIndex = fonts.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FontId = fontIndex,
                ApplyFont = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetFontColor error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetNumberFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string numberFormat = GetParameterValue(operationPoint, "NumberFormat", "常规");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射数字格式
            string formatCode = MapNumberFormat(numberFormat);

            // 获取或创建数字格式
            NumberingFormats numberingFormats = stylesheet.NumberingFormats ?? new NumberingFormats();
            if (stylesheet.NumberingFormats == null)
                stylesheet.NumberingFormats = numberingFormats;

            // 查找是否已存在相同格式
            NumberingFormat? existingFormat = numberingFormats.Elements<NumberingFormat>()
                .FirstOrDefault(nf => nf.FormatCode?.Value == formatCode);

            uint numberFormatId;
            if (existingFormat?.NumberFormatId?.Value != null)
            {
                numberFormatId = existingFormat.NumberFormatId.Value;
            }
            else
            {
                // 创建新的数字格式
                numberFormatId = 164; // 自定义格式从164开始
                if (numberingFormats.Elements<NumberingFormat>().Any())
                {
                    uint maxId = numberingFormats.Elements<NumberingFormat>()
                        .Where(nf => nf.NumberFormatId?.Value != null)
                        .Max(nf => nf.NumberFormatId!.Value);
                    numberFormatId = maxId + 1;
                }

                NumberingFormat newFormat = new NumberingFormat()
                {
                    NumberFormatId = numberFormatId,
                    FormatCode = formatCode
                };
                numberingFormats.Append(newFormat);
                numberingFormats.Count = (uint)numberingFormats.Elements<NumberingFormat>().Count();
            }

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                NumberFormatId = numberFormatId,
                ApplyNumberFormat = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetNumberFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetPatternFillStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string patternStyle = GetParameterValue(operationPoint, "PatternStyle", "无");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射图案样式
            PatternValues patternValue = MapPatternStyle(patternStyle);

            // 创建填充
            Fill fill = new Fill();
            PatternFill patternFill = new PatternFill()
            {
                PatternType = patternValue
            };
            fill.Append(patternFill);

            // 添加填充到填充集合
            Fills fills = stylesheet.Fills ?? new Fills();
            if (stylesheet.Fills == null)
                stylesheet.Fills = fills;

            fills.Append(fill);
            fills.Count = (uint)fills.Elements<Fill>().Count();

            uint fillIndex = fills.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FillId = fillIndex,
                ApplyFill = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetPatternFillStyle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetPatternFillColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string patternColor = GetParameterValue(operationPoint, "PatternColor", "#808080");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 解析颜色
            string colorValue = ParseColor(patternColor);

            // 创建填充
            Fill fill = new Fill();
            PatternFill patternFill = new PatternFill()
            {
                PatternType = PatternValues.Solid
            };
            patternFill.Append(new ForegroundColor() { Rgb = colorValue });
            fill.Append(patternFill);

            // 添加填充到填充集合
            Fills fills = stylesheet.Fills ?? new Fills();
            if (stylesheet.Fills == null)
                stylesheet.Fills = fills;

            fills.Append(fill);
            fills.Count = (uint)fills.Elements<Fill>().Count();

            uint fillIndex = fills.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FillId = fillIndex,
                ApplyFill = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetPatternFillColor error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetOuterBorderStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string borderStyle = GetParameterValue(operationPoint, "BorderStyle", "无边框");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射边框样式
            BorderStyleValues borderStyleValue = MapBorderStyle(borderStyle);

            // 创建边框（只设置外边框）
            Border border = new Border();
            border.Append(new LeftBorder() { Style = borderStyleValue });
            border.Append(new RightBorder() { Style = borderStyleValue });
            border.Append(new TopBorder() { Style = borderStyleValue });
            border.Append(new BottomBorder() { Style = borderStyleValue });
            border.Append(new DiagonalBorder());

            // 添加边框到边框集合
            Borders borders = stylesheet.Borders ?? new Borders();
            if (stylesheet.Borders == null)
                stylesheet.Borders = borders;

            borders.Append(border);
            borders.Count = (uint)borders.Elements<Border>().Count();

            uint borderIndex = borders.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                BorderId = borderIndex,
                ApplyBorder = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetOuterBorderStyle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetOuterBorderColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:C3");
            string borderColor = GetParameterValue(operationPoint, "BorderColor", "#000000");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 解析颜色
            string colorValue = ParseColor(borderColor);

            // 创建边框（只设置外边框）
            Border border = new Border();

            LeftBorder leftBorder = new LeftBorder() { Style = BorderStyleValues.Thin };
            leftBorder.Append(new Color() { Rgb = colorValue });
            border.Append(leftBorder);

            RightBorder rightBorder = new RightBorder() { Style = BorderStyleValues.Thin };
            rightBorder.Append(new Color() { Rgb = colorValue });
            border.Append(rightBorder);

            TopBorder topBorder = new TopBorder() { Style = BorderStyleValues.Thin };
            topBorder.Append(new Color() { Rgb = colorValue });
            border.Append(topBorder);

            BottomBorder bottomBorder = new BottomBorder() { Style = BorderStyleValues.Thin };
            bottomBorder.Append(new Color() { Rgb = colorValue });
            border.Append(bottomBorder);

            border.Append(new DiagonalBorder());

            // 添加边框到边框集合
            Borders borders = stylesheet.Borders ?? new Borders();
            if (stylesheet.Borders == null)
                stylesheet.Borders = borders;

            borders.Append(border);
            borders.Count = (uint)borders.Elements<Border>().Count();

            uint borderIndex = borders.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                BorderId = borderIndex,
                ApplyBorder = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetOuterBorderColor error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteAddUnderline(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string underlineType = GetParameterValue(operationPoint, "UnderlineType", "无");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 映射下划线类型
            UnderlineValues underlineValue = MapUnderlineType(underlineType);

            // 创建字体
            Font font = new Font();
            font.Append(new FontName() { Val = "Calibri" });
            font.Append(new FontSize() { Val = 11 });

            if (underlineValue != UnderlineValues.None)
            {
                font.Append(new Underline() { Val = underlineValue });
            }

            // 添加字体到字体集合
            Fonts fonts = stylesheet.Fonts ?? new Fonts();
            if (stylesheet.Fonts == null)
                stylesheet.Fonts = fonts;

            fonts.Append(font);
            fonts.Count = (uint)fonts.Elements<Font>().Count();

            uint fontIndex = fonts.Count - 1;

            // 创建单元格格式
            CellFormat cellFormat = new CellFormat()
            {
                FontId = fontIndex,
                ApplyFont = true
            };

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteAddUnderline error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteConditionalFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string conditionType = GetParameterValue(operationPoint, "ConditionType", "突出显示单元格规则");
            string conditionValue = GetParameterValue(operationPoint, "ConditionValue", "");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;

            // 获取或创建条件格式
            ConditionalFormatting? conditionalFormatting = worksheet.Elements<ConditionalFormatting>()
                .FirstOrDefault(cf => cf.SequenceOfReferences?.InnerText == cellRange);

            if (conditionalFormatting == null)
            {
                conditionalFormatting = new ConditionalFormatting();
                conditionalFormatting.SequenceOfReferences = new ListValue<StringValue>() { InnerText = cellRange };
                SheetData? sheetData = worksheet.Elements<SheetData>().FirstOrDefault();
                if (sheetData != null)
                {
                    worksheet.InsertAfter(conditionalFormatting, sheetData);
                }
                else
                {
                    worksheet.AppendChild(conditionalFormatting);
                }
            }

            // 创建条件格式规则
            ConditionalFormattingRule rule = new ConditionalFormattingRule()
            {
                Type = ConditionalFormatValues.CellIs,
                Operator = ConditionalFormattingOperatorValues.GreaterThan,
                FormatId = 0,
                Priority = 1
            };

            // 添加条件公式
            if (!string.IsNullOrEmpty(conditionValue))
            {
                Formula formula = new Formula(conditionValue);
                rule.Append(formula);
            }

            conditionalFormatting.Append(rule);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteConditionalFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSetCellStyleData(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string cellRange = GetParameterValue(operationPoint, "CellRange", "A1:A1");
            string styleName = GetParameterValue(operationPoint, "StyleName", "常规");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 获取或创建样式表
            WorkbookStylesPart stylesPart = GetOrCreateStylesPart(spreadsheetDocument);
            Stylesheet stylesheet = stylesPart.Stylesheet;

            // 根据样式名称创建预定义样式
            CellFormat cellFormat = CreatePredefinedCellStyle(styleName, stylesheet);

            // 添加单元格格式到格式集合
            CellFormats cellFormats = stylesheet.CellFormats ?? new CellFormats();
            if (stylesheet.CellFormats == null)
                stylesheet.CellFormats = cellFormats;

            cellFormats.Append(cellFormat);
            cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();

            uint styleIndex = cellFormats.Count - 1;

            // 应用样式到单元格区域
            ApplyStyleToCellRange(worksheetPart, cellRange, styleIndex);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSetCellStyleData error: {ex.Message}");
            return false;
        }
    }

    // 数据清单操作方法实现
    private async Task<bool> ExecuteFilter(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string filterConditions = GetParameterValue(operationPoint, "FilterConditions", "A:条件值");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;

            // 解析筛选条件 (格式: A:条件值)
            string[] conditionParts = filterConditions.Split(':');
            if (conditionParts.Length != 2)
                return false;

            string columnLetter = conditionParts[0].Trim();
            string filterValue = conditionParts[1].Trim();

            // 获取数据范围（简化实现，假设数据从A1开始）
            string dataRange = "A1:Z1000"; // 可以根据实际数据调整

            // 创建自动筛选
            AutoFilter autoFilter = new AutoFilter()
            {
                Reference = dataRange
            };

            // 获取列索引
            uint columnIndex = GetColumnIndex(columnLetter);

            // 创建筛选列
            FilterColumn filterColumn = new FilterColumn()
            {
                ColumnId = columnIndex - 1 // 0-based index
            };

            // 创建自定义筛选
            CustomFilters customFilters = new CustomFilters();
            CustomFilter customFilter = new CustomFilter()
            {
                Operator = FilterOperatorValues.Equal,
                Val = filterValue
            };
            customFilters.Append(customFilter);
            filterColumn.Append(customFilters);

            autoFilter.Append(filterColumn);

            // 移除现有的自动筛选
            AutoFilter? existingAutoFilter = worksheet.Elements<AutoFilter>().FirstOrDefault();
            if (existingAutoFilter != null)
            {
                existingAutoFilter.Remove();
            }

            // 添加新的自动筛选
            SheetData? sheetData = worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData != null)
            {
                worksheet.InsertAfter(autoFilter, sheetData);
            }
            else
            {
                worksheet.AppendChild(autoFilter);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteFilter error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSort(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string sortColumn = GetParameterValue(operationPoint, "SortColumn", "A");
            string sortOrder = GetParameterValue(operationPoint, "SortOrder", "升序");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;

            // 获取数据范围（简化实现）
            string dataRange = "A1:Z1000";

            // 创建排序状态
            SortState sortState = new SortState()
            {
                Reference = dataRange
            };

            // 获取列索引
            uint columnIndex = GetColumnIndex(sortColumn);

            // 创建排序条件
            SortCondition sortCondition = new SortCondition()
            {
                Reference = $"{sortColumn}:{sortColumn}",
                Descending = sortOrder == "降序"
            };

            sortState.Append(sortCondition);

            // 移除现有的排序状态
            SortState? existingSortState = worksheet.Elements<SortState>().FirstOrDefault();
            if (existingSortState != null)
            {
                existingSortState.Remove();
            }

            // 添加新的排序状态
            SheetData? sheetData = worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData != null)
            {
                worksheet.InsertAfter(sortState, sheetData);
            }
            else
            {
                worksheet.AppendChild(sortState);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSort error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecutePivotTable(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string sourceRange = GetParameterValue(operationPoint, "SourceRange", "A1:D10");
            string pivotLocation = GetParameterValue(operationPoint, "PivotLocation", "F1");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 注意：完整的数据透视表实现需要创建PivotTablePart和相关的XML结构
            // 这里提供一个简化的实现框架

            // 创建数据透视表缓存定义（简化）
            // 在实际实现中，需要：
            // 1. 创建PivotCacheDefinitionPart
            // 2. 创建PivotTablePart
            // 3. 设置数据源和字段配置
            // 4. 定义行字段、列字段、数据字段等

            Worksheet worksheet = worksheetPart.Worksheet;

            // 在指定位置添加一个标记，表示数据透视表位置
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            if (sheetData != null)
            {
                Cell pivotCell = GetCell(sheetData, pivotLocation);
                pivotCell.CellValue = new CellValue("数据透视表");
                pivotCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            // 注意：完整的数据透视表实现需要大量的XML结构
            // 建议使用专门的Excel库或手动创建完整的PivotTable XML

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecutePivotTable error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteSubtotal(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string groupByColumn = GetParameterValue(operationPoint, "GroupByColumn", "A");
            string summaryFunction = GetParameterValue(operationPoint, "SummaryFunction", "求和");
            string summaryColumn = GetParameterValue(operationPoint, "SummaryColumn", "B");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                // 简化的分类汇总实现
                // 在实际应用中，需要：
                // 1. 分析数据并按分组列排序
                // 2. 识别分组边界
                // 3. 插入汇总行
                // 4. 应用SUBTOTAL函数

                // 这里添加一个示例汇总行
                uint lastRowIndex = 10; // 假设数据到第10行
                Cell subtotalCell = GetCell(sheetData, $"{summaryColumn}{lastRowIndex + 1}");

                string functionName = MapSummaryFunction(summaryFunction);
                string formula = $"=SUBTOTAL({GetSubtotalFunctionNumber(functionName)},{summaryColumn}1:{summaryColumn}{lastRowIndex})";

                subtotalCell.CellFormula = new CellFormula(formula);
                subtotalCell.DataType = new EnumValue<CellValues>(CellValues.Number);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteSubtotal error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteAdvancedFilterCondition(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string criteriaRange = GetParameterValue(operationPoint, "CriteriaRange", "A1:A2");
            string conditionType = GetParameterValue(operationPoint, "ConditionType", "等于");
            string conditionValue = GetParameterValue(operationPoint, "ConditionValue", "");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null && !string.IsNullOrEmpty(conditionValue))
            {
                // 解析条件区域
                (string startCell, string endCell) = ParseCellRange(criteriaRange);

                // 在条件区域设置筛选条件
                Cell conditionCell = GetCell(sheetData, endCell);

                // 根据条件类型设置值
                string criteriaValue = FormatFilterCriteria(conditionType, conditionValue);
                conditionCell.CellValue = new CellValue(criteriaValue);
                conditionCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteAdvancedFilterCondition error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteAdvancedFilterData(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string dataRange = GetParameterValue(operationPoint, "DataRange", "A1:D100");
            string criteriaRange = GetParameterValue(operationPoint, "CriteriaRange", "A1:A2");
            string outputRange = GetParameterValue(operationPoint, "OutputRange", "F1");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            Worksheet worksheet = worksheetPart.Worksheet;

            // 创建高级筛选（简化实现）
            // 在实际应用中，高级筛选需要复杂的数据处理逻辑
            AutoFilter autoFilter = new AutoFilter()
            {
                Reference = dataRange
            };

            // 移除现有的自动筛选
            AutoFilter existingAutoFilter = worksheet.Elements<AutoFilter>().FirstOrDefault();
            if (existingAutoFilter != null)
            {
                existingAutoFilter.Remove();
            }

            // 添加新的自动筛选
            worksheet.InsertAfter(autoFilter, worksheet.Elements<SheetData>().First());

            // 在输出区域添加标记
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            if (sheetData != null)
            {
                Cell outputCell = GetCell(sheetData, outputRange);
                outputCell.CellValue = new CellValue("筛选结果");
                outputCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteAdvancedFilterData error: {ex.Message}");
            return false;
        }
    }

    // 图表操作方法实现
    private async Task<bool> ExecuteChartType(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string chartType = GetParameterValue(operationPoint, "ChartType", "柱形图");
            string dataRange = GetParameterValue(operationPoint, "DataRange", "A1:B5");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 注意：完整的图表实现需要创建ChartPart和DrawingsPart
            // 这里提供一个简化的实现框架

            // 在实际实现中，需要：
            // 1. 创建DrawingsPart
            // 2. 创建ChartPart
            // 3. 设置图表类型和数据源
            // 4. 配置图表样式和布局

            Worksheet worksheet = worksheetPart.Worksheet;

            // 添加一个标记表示图表位置（简化实现）
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            if (sheetData != null)
            {
                Cell chartCell = GetCell(sheetData, "E1");
                chartCell.CellValue = new CellValue($"图表类型: {chartType}");
                chartCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            // 注意：完整的图表实现需要大量的XML结构
            // 建议使用专门的图表库或手动创建完整的Chart XML

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartType error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartStyle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string chartStyle = GetParameterValue(operationPoint, "ChartStyle", "样式1");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加样式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell styleCell = GetCell(sheetData, "E2");
                styleCell.CellValue = new CellValue($"图表样式: {chartStyle}");
                styleCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartStyle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartTitle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string chartTitle = GetParameterValue(operationPoint, "ChartTitle", "图表标题");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加标题标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell titleCell = GetCell(sheetData, "E3");
                titleCell.CellValue = new CellValue($"图表标题: {chartTitle}");
                titleCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartTitle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteLegendPosition(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string legendPosition = GetParameterValue(operationPoint, "LegendPosition", "右侧");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加图例位置标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell legendCell = GetCell(sheetData, "E4");
                legendCell.CellValue = new CellValue($"图例位置: {legendPosition}");
                legendCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteLegendPosition error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartMove(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string newPosition = GetParameterValue(operationPoint, "NewPosition", "F1");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在新位置添加图表移动标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell moveCell = GetCell(sheetData, newPosition);
                moveCell.CellValue = new CellValue("图表已移动到此位置");
                moveCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartMove error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteCategoryAxisDataRange(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string dataRange = GetParameterValue(operationPoint, "DataRange", "A1:A5");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加分类轴数据范围标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell axisCell = GetCell(sheetData, "E7");
                axisCell.CellValue = new CellValue($"分类轴数据: {dataRange}");
                axisCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteCategoryAxisDataRange error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteValueAxisDataRange(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string dataRange = GetParameterValue(operationPoint, "DataRange", "B1:B5");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加数值轴数据范围标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell axisCell = GetCell(sheetData, "E8");
                axisCell.CellValue = new CellValue($"数值轴数据: {dataRange}");
                axisCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteValueAxisDataRange error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartTitleFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string titleFormat = GetParameterValue(operationPoint, "TitleFormat", "粗体");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加标题格式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell formatCell = GetCell(sheetData, "F1");
                formatCell.CellValue = new CellValue($"标题格式: {titleFormat}");
                formatCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartTitleFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteHorizontalAxisTitle(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string axisTitle = GetParameterValue(operationPoint, "AxisTitle", "横坐标轴");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加横轴标题标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell axisTitleCell = GetCell(sheetData, "F2");
                axisTitleCell.CellValue = new CellValue($"横轴标题: {axisTitle}");
                axisTitleCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteHorizontalAxisTitle error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteHorizontalAxisTitleFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string titleFormat = GetParameterValue(operationPoint, "TitleFormat", "常规");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加横轴标题格式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell formatCell = GetCell(sheetData, "F3");
                formatCell.CellValue = new CellValue($"横轴标题格式: {titleFormat}");
                formatCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteHorizontalAxisTitleFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteLegendFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string legendFormat = GetParameterValue(operationPoint, "LegendFormat", "常规");
            string fontColor = GetParameterValue(operationPoint, "FontColor", "#000000");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加图例格式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell formatCell = GetCell(sheetData, "F7");
                formatCell.CellValue = new CellValue($"图例格式: {legendFormat}, 颜色: {fontColor}");
                formatCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteLegendFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteVerticalAxisOptions(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string axisOptions = GetParameterValue(operationPoint, "AxisOptions", "自动");
            string minValue = GetParameterValue(operationPoint, "MinValue", "");
            string maxValue = GetParameterValue(operationPoint, "MaxValue", "");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加纵轴选项标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell optionsCell = GetCell(sheetData, "F8");
                string optionsInfo = $"纵轴选项: {axisOptions}";
                if (!string.IsNullOrEmpty(minValue) || !string.IsNullOrEmpty(maxValue))
                {
                    optionsInfo += $", 范围: {minValue}-{maxValue}";
                }
                optionsCell.CellValue = new CellValue(optionsInfo);
                optionsCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteVerticalAxisOptions error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteMajorHorizontalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string showGridlines = GetParameterValue(operationPoint, "ShowGridlines", "是");
            string gridlineStyle = GetParameterValue(operationPoint, "GridlineStyle", "实线");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart?.Worksheet?.GetFirstChild<SheetData>() != null)
            {
                Cell gridlineCell = GetCell(worksheetPart.Worksheet.GetFirstChild<SheetData>(), "F9");
                gridlineCell.CellValue = new CellValue($"主要横向网格线: {showGridlines}, 样式: {gridlineStyle}");
                gridlineCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteMajorHorizontalGridlines error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteMinorHorizontalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string showGridlines = GetParameterValue(operationPoint, "ShowGridlines", "否");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart?.Worksheet?.GetFirstChild<SheetData>() != null)
            {
                Cell gridlineCell = GetCell(worksheetPart.Worksheet.GetFirstChild<SheetData>(), "F10");
                gridlineCell.CellValue = new CellValue($"次要横向网格线: {showGridlines}");
                gridlineCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteMinorHorizontalGridlines error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteMajorVerticalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string showGridlines = GetParameterValue(operationPoint, "ShowGridlines", "否");
            string gridlineStyle = GetParameterValue(operationPoint, "GridlineStyle", "实线");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart?.Worksheet?.GetFirstChild<SheetData>() != null)
            {
                Cell gridlineCell = GetCell(worksheetPart.Worksheet.GetFirstChild<SheetData>(), "F11");
                gridlineCell.CellValue = new CellValue($"主要纵向网格线: {showGridlines}, 样式: {gridlineStyle}");
                gridlineCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteMajorVerticalGridlines error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteMinorVerticalGridlines(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string showGridlines = GetParameterValue(operationPoint, "ShowGridlines", "否");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart?.Worksheet?.GetFirstChild<SheetData>() != null)
            {
                Cell gridlineCell = GetCell(worksheetPart.Worksheet.GetFirstChild<SheetData>(), "F12");
                gridlineCell.CellValue = new CellValue($"次要纵向网格线: {showGridlines}");
                gridlineCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteMinorVerticalGridlines error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteDataSeriesFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string seriesName = GetParameterValue(operationPoint, "SeriesName", "系列1");
            string seriesColor = GetParameterValue(operationPoint, "SeriesColor", "#0070C0");
            string seriesStyle = GetParameterValue(operationPoint, "SeriesStyle", "实线");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加数据系列格式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell seriesCell = GetCell(sheetData, "F13");
                seriesCell.CellValue = new CellValue($"数据系列: {seriesName}, 颜色: {seriesColor}, 样式: {seriesStyle}");
                seriesCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteDataSeriesFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteAddDataLabels(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string labelType = GetParameterValue(operationPoint, "LabelType", "值");
            string labelPosition = GetParameterValue(operationPoint, "LabelPosition", "数据点上方");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加数据标签标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell labelCell = GetCell(sheetData, "F14");
                labelCell.CellValue = new CellValue($"数据标签: {labelType}, 位置: {labelPosition}");
                labelCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteAddDataLabels error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteDataLabelsFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string labelFormat = GetParameterValue(operationPoint, "LabelFormat", "常规");
            string fontColor = GetParameterValue(operationPoint, "FontColor", "#000000");
            string fontSize = GetParameterValue(operationPoint, "FontSize", "9");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加数据标签格式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell formatCell = GetCell(sheetData, "F15");
                formatCell.CellValue = new CellValue($"标签格式: {labelFormat}, 颜色: {fontColor}, 大小: {fontSize}");
                formatCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteDataLabelsFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartAreaFormat(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string areaColor = GetParameterValue(operationPoint, "AreaColor", "#FFFFFF");
            string borderStyle = GetParameterValue(operationPoint, "BorderStyle", "无边框");
            string borderColor = GetParameterValue(operationPoint, "BorderColor", "#000000");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加图表区域格式标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell areaCell = GetCell(sheetData, "F16");
                areaCell.CellValue = new CellValue($"图表区域: 背景{areaColor}, 边框{borderStyle}({borderColor})");
                areaCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartAreaFormat error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartFloorColor(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string floorColor = GetParameterValue(operationPoint, "FloorColor", "#F2F2F2");
            string transparency = GetParameterValue(operationPoint, "Transparency", "0%");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加图表底面颜色标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell floorCell = GetCell(sheetData, "F17");
                floorCell.CellValue = new CellValue($"图表底面: 颜色{floorColor}, 透明度{transparency}");
                floorCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartFloorColor error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteChartBorder(OperationPoint operationPoint, SpreadsheetDocument spreadsheetDocument)
    {
        try
        {
            string targetWorksheet = GetParameterValue(operationPoint, "TargetWorksheet", "Sheet1");
            string borderStyle = GetParameterValue(operationPoint, "BorderStyle", "实线");
            string borderColor = GetParameterValue(operationPoint, "BorderColor", "#000000");
            string borderWidth = GetParameterValue(operationPoint, "BorderWidth", "1pt");

            WorksheetPart worksheetPart = GetWorksheetPart(spreadsheetDocument, targetWorksheet);
            if (worksheetPart == null)
                return false;

            // 简化实现：在工作表中添加图表边框标记
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                Cell borderCell = GetCell(sheetData, "F18");
                borderCell.CellValue = new CellValue($"图表边框: {borderStyle}, 颜色{borderColor}, 宽度{borderWidth}");
                borderCell.DataType = new EnumValue<CellValues>(CellValues.String);
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ExecuteChartBorder error: {ex.Message}");
            return false;
        }
    }

    #region 辅助方法

    /// <summary>
    /// 获取参数值
    /// </summary>
    private string GetParameterValue(OperationPoint operationPoint, string parameterName, string defaultValue = "")
    {
        ConfigurationParameter parameter = operationPoint.Parameters.FirstOrDefault(p => p.Name == parameterName);
        return parameter?.Value ?? defaultValue;
    }

    /// <summary>
    /// 获取工作表部分
    /// </summary>
    private WorksheetPart? GetWorksheetPart(SpreadsheetDocument spreadsheetDocument, string worksheetName)
    {
        WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart;
        if (workbookPart?.Workbook?.Sheets == null)
            return null;

        Sheet? sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
            .FirstOrDefault(s => s.Name?.Value == worksheetName);

        if (sheet == null)
        {
            // 如果找不到指定名称的工作表，使用第一个工作表
            sheet = workbookPart.Workbook.Sheets.Elements<Sheet>().FirstOrDefault();
        }

        if (sheet?.Id?.Value == null)
            return null;

        return (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
    }

    /// <summary>
    /// 设置单元格值
    /// </summary>
    private void SetCellValue(WorksheetPart worksheetPart, string cellAddress, string value)
    {
        Worksheet worksheet = worksheetPart.Worksheet;
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();

        if (sheetData == null)
        {
            sheetData = new SheetData();
            worksheet.AppendChild(sheetData);
        }

        Cell cell = GetCell(sheetData, cellAddress);
        cell.CellValue = new CellValue(value);
        cell.DataType = new EnumValue<CellValues>(CellValues.String);
    }

    /// <summary>
    /// 获取或创建单元格
    /// </summary>
    private Cell GetCell(SheetData sheetData, string cellAddress)
    {
        uint rowIndex = GetRowIndex(cellAddress);
        string columnName = GetColumnName(cellAddress);

        Row? row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
        if (row == null)
        {
            row = new Row() { RowIndex = rowIndex };
            sheetData.Append(row);
        }

        Cell? cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference?.Value == cellAddress);
        if (cell == null)
        {
            cell = new Cell() { CellReference = cellAddress };
            row.Append(cell);
        }

        return cell;
    }

    /// <summary>
    /// 从单元格地址获取行索引
    /// </summary>
    private uint GetRowIndex(string cellAddress)
    {
        Match match = Regex.Match(cellAddress, @"\d+");
        return match.Success ? uint.Parse(match.Value) : 1;
    }

    /// <summary>
    /// 从单元格地址获取列名
    /// </summary>
    private string GetColumnName(string cellAddress)
    {
        Match match = Regex.Match(cellAddress, @"[A-Z]+");
        return match.Success ? match.Value : "A";
    }

    /// <summary>
    /// 解析单元格区域
    /// </summary>
    private (string startCell, string endCell) ParseCellRange(string cellRange)
    {
        if (string.IsNullOrEmpty(cellRange))
            return ("A1", "A1");

        string[] parts = cellRange.Split(':');
        if (parts.Length == 2)
            return (parts[0].Trim(), parts[1].Trim());

        return (cellRange.Trim(), cellRange.Trim());
    }

    /// <summary>
    /// 解析颜色值
    /// </summary>
    private string ParseColor(string colorValue)
    {
        if (string.IsNullOrEmpty(colorValue))
            return "000000";

        // 移除#号
        colorValue = colorValue.TrimStart('#');

        // 确保是6位十六进制
        if (colorValue.Length == 6 && Regex.IsMatch(colorValue, @"^[0-9A-Fa-f]{6}$"))
            return colorValue.ToUpperInvariant();

        return "000000"; // 默认黑色
    }

    /// <summary>
    /// 获取或创建样式表部分
    /// </summary>
    private WorkbookStylesPart GetOrCreateStylesPart(SpreadsheetDocument spreadsheetDocument)
    {
        WorkbookPart? workbookPart = spreadsheetDocument.WorkbookPart;
        if (workbookPart == null)
            throw new InvalidOperationException("WorkbookPart cannot be null");

        WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;

        if (stylesPart == null)
        {
            stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = CreateDefaultStylesheet();
        }

        return stylesPart;
    }

    /// <summary>
    /// 创建默认样式表
    /// </summary>
    private Stylesheet CreateDefaultStylesheet()
    {
        Stylesheet stylesheet = new Stylesheet();

        // 字体
        Fonts fonts = new Fonts();
        Font defaultFont = new Font();
        defaultFont.Append(new FontName() { Val = "Calibri" });
        defaultFont.Append(new FontSize() { Val = 11 });
        fonts.Append(defaultFont);
        fonts.Count = 1;
        stylesheet.Fonts = fonts;

        // 填充
        Fills fills = new Fills();
        Fill defaultFill = new Fill();
        defaultFill.Append(new PatternFill() { PatternType = PatternValues.None });
        fills.Append(defaultFill);
        fills.Count = 1;
        stylesheet.Fills = fills;

        // 边框
        Borders borders = new Borders();
        Border defaultBorder = new Border();
        defaultBorder.Append(new LeftBorder());
        defaultBorder.Append(new RightBorder());
        defaultBorder.Append(new TopBorder());
        defaultBorder.Append(new BottomBorder());
        defaultBorder.Append(new DiagonalBorder());
        borders.Append(defaultBorder);
        borders.Count = 1;
        stylesheet.Borders = borders;

        // 单元格格式
        CellFormats cellFormats = new CellFormats();
        CellFormat defaultCellFormat = new CellFormat();
        cellFormats.Append(defaultCellFormat);
        cellFormats.Count = 1;
        stylesheet.CellFormats = cellFormats;

        return stylesheet;
    }

    /// <summary>
    /// 应用样式到单元格区域
    /// </summary>
    private void ApplyStyleToCellRange(WorksheetPart worksheetPart, string cellRange, uint styleIndex)
    {
        (string startCell, string endCell) = ParseCellRange(cellRange);

        // 简化实现：只应用到起始单元格
        // 完整实现需要遍历整个区域
        Worksheet worksheet = worksheetPart.Worksheet;
        SheetData? sheetData = worksheet.GetFirstChild<SheetData>();

        if (sheetData != null)
        {
            Cell cell = GetCell(sheetData, startCell);
            cell.StyleIndex = styleIndex;
        }
    }

    /// <summary>
    /// 映射水平对齐方式
    /// </summary>
    private HorizontalAlignmentValues MapHorizontalAlignment(string alignment)
    {
        return alignment switch
        {
            "左对齐" => HorizontalAlignmentValues.Left,
            "居中对齐" => HorizontalAlignmentValues.Center,
            "右对齐" => HorizontalAlignmentValues.Right,
            "填充" => HorizontalAlignmentValues.Fill,
            "两端对齐" => HorizontalAlignmentValues.Justify,
            "跨列居中" => HorizontalAlignmentValues.CenterContinuous,
            "分散对齐" => HorizontalAlignmentValues.Distributed,
            _ => HorizontalAlignmentValues.General
        };
    }

    /// <summary>
    /// 映射垂直对齐方式
    /// </summary>
    private VerticalAlignmentValues MapVerticalAlignment(string alignment)
    {
        return alignment switch
        {
            "顶端对齐" => VerticalAlignmentValues.Top,
            "垂直居中对齐" => VerticalAlignmentValues.Center,
            "底端对齐" => VerticalAlignmentValues.Bottom,
            "两端对齐" => VerticalAlignmentValues.Justify,
            "分散对齐" => VerticalAlignmentValues.Distributed,
            _ => VerticalAlignmentValues.Bottom
        };
    }

    /// <summary>
    /// 获取列索引（A=1, B=2, ...）
    /// </summary>
    private uint GetColumnIndex(string columnLetter)
    {
        uint result = 0;
        for (int i = 0; i < columnLetter.Length; i++)
        {
            result = result * 26 + (uint)(columnLetter[i] - 'A' + 1);
        }
        return result;
    }

    /// <summary>
    /// 映射边框样式
    /// </summary>
    private BorderStyleValues MapBorderStyle(string borderStyle)
    {
        return borderStyle switch
        {
            "单实线" => BorderStyleValues.Thin,
            "双线" => BorderStyleValues.Double,
            "点线" => BorderStyleValues.Dotted,
            "短划线" => BorderStyleValues.Dashed,
            "长划线" => BorderStyleValues.DashDot,
            "划线+点" => BorderStyleValues.DashDot,
            "划线+两个点" => BorderStyleValues.DashDotDot,
            "三线" => BorderStyleValues.Thick,
            _ => BorderStyleValues.None
        };
    }

    /// <summary>
    /// 映射数字格式
    /// </summary>
    private string MapNumberFormat(string numberFormat)
    {
        return numberFormat switch
        {
            "常规" => "General",
            "数值" => "0.00",
            "货币" => "\"¥\"#,##0.00",
            "会计专用" => "_-\"¥\"* #,##0.00_-;-\"¥\"* #,##0.00_-;_-\"¥\"* \"-\"??_-;_-@_-",
            "日期" => "yyyy/m/d",
            "时间" => "h:mm:ss",
            "百分比" => "0.00%",
            "分数" => "# ?/?",
            "科学记数" => "0.00E+00",
            "文本" => "@",
            "特殊" => "00000",
            "自定义" => "General",
            _ => "General"
        };
    }

    /// <summary>
    /// 映射图案样式
    /// </summary>
    private PatternValues MapPatternStyle(string patternStyle)
    {
        return patternStyle switch
        {
            "无" => PatternValues.None,
            "实心" => PatternValues.Solid,
            "5%灰色" => PatternValues.Gray0625,
            "10%灰色" => PatternValues.Gray125,
            "20%灰色" => PatternValues.LightGray,
            "25%灰色" => PatternValues.LightGray,
            "30%灰色" => PatternValues.LightGray,
            "40%灰色" => PatternValues.MediumGray,
            "50%灰色" => PatternValues.MediumGray,
            "60%灰色" => PatternValues.DarkGray,
            "75%灰色" => PatternValues.DarkGray,
            "水平条纹" => PatternValues.DarkHorizontal,
            "垂直条纹" => PatternValues.DarkVertical,
            "反向对角条纹" => PatternValues.DarkDown,
            "对角条纹" => PatternValues.DarkUp,
            "对角十字线" => PatternValues.DarkGrid,
            "粗对角十字线" => PatternValues.DarkTrellis,
            _ => PatternValues.None
        };
    }

    /// <summary>
    /// 映射下划线类型
    /// </summary>
    private UnderlineValues MapUnderlineType(string underlineType)
    {
        return underlineType switch
        {
            "无" => UnderlineValues.None,
            "单下划线" => UnderlineValues.Single,
            "双下划线" => UnderlineValues.Double,
            "会计用单下划线" => UnderlineValues.SingleAccounting,
            "会计用双下划线" => UnderlineValues.DoubleAccounting,
            _ => UnderlineValues.None
        };
    }

    /// <summary>
    /// 创建预定义单元格样式
    /// </summary>
    private CellFormat CreatePredefinedCellStyle(string styleName, Stylesheet stylesheet)
    {
        CellFormat cellFormat = new CellFormat();

        switch (styleName)
        {
            case "好":
                // 绿色背景
                cellFormat.FillId = CreateColorFill(stylesheet, "00C000");
                cellFormat.ApplyFill = true;
                break;
            case "差":
                // 红色背景
                cellFormat.FillId = CreateColorFill(stylesheet, "FF0000");
                cellFormat.ApplyFill = true;
                break;
            case "中性":
                // 黄色背景
                cellFormat.FillId = CreateColorFill(stylesheet, "FFFF00");
                cellFormat.ApplyFill = true;
                break;
            case "标题1":
                // 大字体，粗体
                cellFormat.FontId = CreateTitleFont(stylesheet, 18, true);
                cellFormat.ApplyFont = true;
                break;
            case "标题2":
                // 中等字体，粗体
                cellFormat.FontId = CreateTitleFont(stylesheet, 14, true);
                cellFormat.ApplyFont = true;
                break;
            case "标题3":
                // 小标题字体，粗体
                cellFormat.FontId = CreateTitleFont(stylesheet, 12, true);
                cellFormat.ApplyFont = true;
                break;
            default:
                // 常规样式
                break;
        }

        return cellFormat;
    }

    /// <summary>
    /// 创建颜色填充
    /// </summary>
    private uint CreateColorFill(Stylesheet stylesheet, string colorValue)
    {
        Fill fill = new Fill();
        PatternFill patternFill = new PatternFill()
        {
            PatternType = PatternValues.Solid
        };
        patternFill.Append(new ForegroundColor() { Rgb = colorValue });
        fill.Append(patternFill);

        Fills fills = stylesheet.Fills ?? new Fills();
        if (stylesheet.Fills == null)
            stylesheet.Fills = fills;

        fills.Append(fill);
        fills.Count = (uint)fills.Elements<Fill>().Count();

        return fills.Count - 1;
    }

    /// <summary>
    /// 创建标题字体
    /// </summary>
    private uint CreateTitleFont(Stylesheet stylesheet, double fontSize, bool bold)
    {
        Font font = new Font();
        font.Append(new FontName() { Val = "Calibri" });
        font.Append(new FontSize() { Val = fontSize });
        if (bold)
        {
            font.Append(new Bold());
        }

        Fonts fonts = stylesheet.Fonts ?? new Fonts();
        if (stylesheet.Fonts == null)
            stylesheet.Fonts = fonts;

        fonts.Append(font);
        fonts.Count = (uint)fonts.Elements<Font>().Count();

        return fonts.Count - 1;
    }

    /// <summary>
    /// 映射汇总函数
    /// </summary>
    private string MapSummaryFunction(string summaryFunction)
    {
        return summaryFunction switch
        {
            "求和" => "SUM",
            "计数" => "COUNT",
            "平均值" => "AVERAGE",
            "最大值" => "MAX",
            "最小值" => "MIN",
            "乘积" => "PRODUCT",
            "标准偏差" => "STDEV",
            "方差" => "VAR",
            _ => "SUM"
        };
    }

    /// <summary>
    /// 获取SUBTOTAL函数编号
    /// </summary>
    private int GetSubtotalFunctionNumber(string functionName)
    {
        return functionName switch
        {
            "AVERAGE" => 1,
            "COUNT" => 2,
            "COUNTA" => 3,
            "MAX" => 4,
            "MIN" => 5,
            "PRODUCT" => 6,
            "STDEV" => 7,
            "SUM" => 9,
            "VAR" => 10,
            _ => 9 // 默认求和
        };
    }

    /// <summary>
    /// 格式化筛选条件
    /// </summary>
    private string FormatFilterCriteria(string conditionType, string conditionValue)
    {
        return conditionType switch
        {
            "等于" => conditionValue,
            "不等于" => $"<>{conditionValue}",
            "大于" => $">{conditionValue}",
            "大于等于" => $">={conditionValue}",
            "小于" => $"<{conditionValue}",
            "小于等于" => $"<={conditionValue}",
            "包含" => $"*{conditionValue}*",
            "不包含" => $"<>*{conditionValue}*",
            "开始于" => $"{conditionValue}*",
            "结束于" => $"*{conditionValue}",
            _ => conditionValue
        };
    }

    #endregion
}
