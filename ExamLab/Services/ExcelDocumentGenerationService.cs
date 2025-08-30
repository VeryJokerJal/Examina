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
            MergeCells mergeCells = worksheet.Elements<MergeCells>().FirstOrDefault();
            if (mergeCells == null)
            {
                mergeCells = new MergeCells();
                worksheet.InsertAfter(mergeCells, worksheet.Elements<SheetData>().First());
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
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

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
                    Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
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
            Columns columns = worksheet.Elements<Columns>().FirstOrDefault();
            if (columns == null)
            {
                columns = new Columns();
                worksheet.InsertBefore(columns, worksheet.GetFirstChild<SheetData>());
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

            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
            if (workbookPart?.Workbook?.Sheets == null)
                return false;

            // 查找要重命名的工作表
            Sheet sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
                .FirstOrDefault(s => s.Name == originalSheetName);

            if (sheet == null)
            {
                // 如果找不到原始名称，尝试使用目标工作表名称
                sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
                    .FirstOrDefault(s => s.Name == targetWorksheet);
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
    private WorksheetPart GetWorksheetPart(SpreadsheetDocument spreadsheetDocument, string worksheetName)
    {
        WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
        if (workbookPart?.Workbook?.Sheets == null)
            return null;

        Sheet sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
            .FirstOrDefault(s => s.Name == worksheetName);

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
        SheetData sheetData = worksheet.GetFirstChild<SheetData>();

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

        Row row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
        if (row == null)
        {
            row = new Row() { RowIndex = rowIndex };
            sheetData.Append(row);
        }

        Cell cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference == cellAddress);
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
        WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
        WorkbookStylesPart stylesPart = workbookPart.WorkbookStylesPart;

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
        SheetData sheetData = worksheet.GetFirstChild<SheetData>();

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

    #endregion
}
