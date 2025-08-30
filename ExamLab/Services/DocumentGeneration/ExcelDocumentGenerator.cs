using ExamLab.Models;
using ExamLab.Services.DocumentGeneration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;

namespace ExamLab.Services.DocumentGeneration;

/// <summary>
/// Excel文档生成器
/// </summary>
public class ExcelDocumentGenerator : IDocumentGenerationService
{
    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    public ModuleType GetSupportedModuleType() => ModuleType.Excel;

    /// <summary>
    /// 获取推荐的文件扩展名
    /// </summary>
    public string GetRecommendedFileExtension() => ".xlsx";

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    public string GetFileTypeDescription() => "Excel工作簿";

    /// <summary>
    /// 验证模块是否可以生成文档
    /// </summary>
    public DocumentValidationResult ValidateModule(ExamModule module)
    {
        DocumentValidationResult result = new() { IsValid = true };

        // 验证模块类型
        if (module.Type != ModuleType.Excel)
        {
            result.AddError($"模块类型不匹配，期望Excel模块，实际为{module.Type}");
            return result;
        }

        // 验证模块是否启用
        if (!module.IsEnabled)
        {
            result.AddWarning("模块未启用");
        }

        // 验证是否有题目
        if (module.Questions.Count == 0)
        {
            result.AddError("模块中没有题目");
            return result;
        }

        // 验证题目中是否有Excel操作点
        int totalExcelOperationPoints = 0;
        foreach (Question question in module.Questions)
        {
            int excelOperationPoints = question.OperationPoints.Count(op => 
                op.ModuleType == ModuleType.Excel && op.IsEnabled);
            totalExcelOperationPoints += excelOperationPoints;
        }

        if (totalExcelOperationPoints == 0)
        {
            result.AddError("模块中没有启用的Excel操作点");
            return result;
        }

        result.Details = $"验证通过：{module.Questions.Count}个题目，{totalExcelOperationPoints}个Excel操作点";
        return result;
    }

    /// <summary>
    /// 异步生成Excel文档
    /// </summary>
    public async Task<DocumentGenerationResult> GenerateDocumentAsync(ExamModule module, string filePath, IProgress<DocumentGenerationProgress>? progress = null)
    {
        DateTime startTime = DateTime.Now;
        
        try
        {
            // 验证模块
            DocumentValidationResult validation = ValidateModule(module);
            if (!validation.IsValid)
            {
                return DocumentGenerationResult.Failure($"模块验证失败：{string.Join(", ", validation.ErrorMessages)}");
            }

            // 收集所有Excel操作点
            List<OperationPoint> allExcelOperationPoints = [];
            foreach (Question question in module.Questions)
            {
                List<OperationPoint> excelOps = question.OperationPoints
                    .Where(op => op.ModuleType == ModuleType.Excel && op.IsEnabled)
                    .ToList();
                allExcelOperationPoints.AddRange(excelOps);
            }

            int totalOperationPoints = allExcelOperationPoints.Count;
            int processedCount = 0;

            // 报告初始进度
            progress?.Report(DocumentGenerationProgress.Create("开始生成Excel文档", 0, totalOperationPoints));

            // 创建Excel文档
            await Task.Run(() =>
            {
                using SpreadsheetDocument document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
                
                // 创建工作簿部分
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                
                // 创建工作表部分
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());
                
                // 创建工作表集合
                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new()
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = module.Name
                };
                sheets.Append(sheet);

                // 获取工作表数据
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

                // 添加标题行
                progress?.Report(DocumentGenerationProgress.Create("添加工作表标题", processedCount, totalOperationPoints));
                AddWorksheetTitle(sheetData, module.Name);

                uint currentRow = 3; // 从第3行开始添加操作点数据

                // 处理每个操作点
                foreach (OperationPoint operationPoint in allExcelOperationPoints)
                {
                    string operationName = GetOperationDisplayName(operationPoint);
                    progress?.Report(DocumentGenerationProgress.Create("处理操作点", processedCount, totalOperationPoints, operationName));

                    try
                    {
                        currentRow = ApplyOperationPoint(sheetData, operationPoint, currentRow);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续处理其他操作点
                        Debug.WriteLine($"处理操作点 {operationName} 时出错: {ex.Message}");
                        // 添加错误信息到工作表
                        AddErrorRow(sheetData, currentRow, operationName, ex.Message);
                        currentRow++;
                    }

                    processedCount++;
                    progress?.Report(DocumentGenerationProgress.Create("处理操作点", processedCount, totalOperationPoints, operationName));
                }

                // 保存文档
                progress?.Report(DocumentGenerationProgress.Create("保存文档", processedCount, totalOperationPoints));
                workbookPart.Workbook.Save();
            });

            TimeSpan duration = DateTime.Now - startTime;
            string details = $"成功生成Excel文档，包含{module.Questions.Count}个题目的{totalOperationPoints}个操作点";
            
            return DocumentGenerationResult.Success(filePath, processedCount, totalOperationPoints, duration, details);
        }
        catch (Exception ex)
        {
            return DocumentGenerationResult.Failure($"生成Excel文档时发生错误：{ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// 添加工作表标题
    /// </summary>
    private static void AddWorksheetTitle(SheetData sheetData, string title)
    {
        // 添加标题行
        Row titleRow = new() { RowIndex = 1 };
        Cell titleCell = CreateTextCell("A", 1, title);
        titleRow.Append(titleCell);
        sheetData.Append(titleRow);

        // 添加空行
        Row emptyRow = new() { RowIndex = 2 };
        sheetData.Append(emptyRow);
    }

    /// <summary>
    /// 应用操作点到工作表
    /// </summary>
    private static uint ApplyOperationPoint(SheetData sheetData, OperationPoint operationPoint, uint currentRow)
    {
        // 根据Excel知识点类型应用不同的操作
        if (operationPoint.ExcelKnowledgeType.HasValue)
        {
            switch (operationPoint.ExcelKnowledgeType.Value)
            {
                case ExcelKnowledgeType.SetCellValue:
                    return ApplyCellValue(sheetData, operationPoint, currentRow);
                case ExcelKnowledgeType.SetCellFormat:
                    return ApplyCellFormat(sheetData, operationPoint, currentRow);
                case ExcelKnowledgeType.CreateChart:
                    return ApplyChart(sheetData, operationPoint, currentRow);
                case ExcelKnowledgeType.SetFormula:
                    return ApplyFormula(sheetData, operationPoint, currentRow);
                default:
                    // 对于未实现的操作点，添加说明文本
                    return AddOperationDescription(sheetData, operationPoint, currentRow);
            }
        }
        else
        {
            // 如果没有指定Excel知识点类型，添加通用说明
            return AddOperationDescription(sheetData, operationPoint, currentRow);
        }
    }

    /// <summary>
    /// 应用单元格值设置
    /// </summary>
    private static uint ApplyCellValue(SheetData sheetData, OperationPoint operationPoint, uint currentRow)
    {
        string cellAddress = GetParameterValue(operationPoint, "CellAddress", "A1");
        string cellValue = GetParameterValue(operationPoint, "CellValue", "示例值");
        
        Row row = new() { RowIndex = currentRow };
        Cell descCell = CreateTextCell("A", currentRow, $"设置单元格 {cellAddress} 的值为：{cellValue}");
        Cell valueCell = CreateTextCell("B", currentRow, cellValue);
        
        row.Append(descCell);
        row.Append(valueCell);
        sheetData.Append(row);
        
        return currentRow + 1;
    }

    /// <summary>
    /// 应用单元格格式设置
    /// </summary>
    private static uint ApplyCellFormat(SheetData sheetData, OperationPoint operationPoint, uint currentRow)
    {
        string cellAddress = GetParameterValue(operationPoint, "CellAddress", "A1");
        string formatType = GetParameterValue(operationPoint, "FormatType", "常规");
        
        Row row = new() { RowIndex = currentRow };
        Cell descCell = CreateTextCell("A", currentRow, $"设置单元格 {cellAddress} 的格式为：{formatType}");
        Cell formatCell = CreateTextCell("B", currentRow, $"[格式：{formatType}]");
        
        row.Append(descCell);
        row.Append(formatCell);
        sheetData.Append(row);
        
        return currentRow + 1;
    }

    /// <summary>
    /// 应用图表创建
    /// </summary>
    private static uint ApplyChart(SheetData sheetData, OperationPoint operationPoint, uint currentRow)
    {
        string chartType = GetParameterValue(operationPoint, "ChartType", "柱状图");
        string dataRange = GetParameterValue(operationPoint, "DataRange", "A1:B5");
        
        Row row = new() { RowIndex = currentRow };
        Cell descCell = CreateTextCell("A", currentRow, $"创建{chartType}，数据范围：{dataRange}");
        Cell chartCell = CreateTextCell("B", currentRow, $"[图表：{chartType}]");
        
        row.Append(descCell);
        row.Append(chartCell);
        sheetData.Append(row);
        
        return currentRow + 1;
    }

    /// <summary>
    /// 应用公式设置
    /// </summary>
    private static uint ApplyFormula(SheetData sheetData, OperationPoint operationPoint, uint currentRow)
    {
        string cellAddress = GetParameterValue(operationPoint, "CellAddress", "A1");
        string formula = GetParameterValue(operationPoint, "Formula", "=SUM(A1:A10)");
        
        Row row = new() { RowIndex = currentRow };
        Cell descCell = CreateTextCell("A", currentRow, $"在单元格 {cellAddress} 设置公式：{formula}");
        Cell formulaCell = CreateTextCell("B", currentRow, formula);
        
        row.Append(descCell);
        row.Append(formulaCell);
        sheetData.Append(row);
        
        return currentRow + 1;
    }

    /// <summary>
    /// 添加操作点描述
    /// </summary>
    private static uint AddOperationDescription(SheetData sheetData, OperationPoint operationPoint, uint currentRow)
    {
        Row row = new() { RowIndex = currentRow };
        Cell descCell = CreateTextCell("A", currentRow, $"操作点：{operationPoint.Name}");
        Cell detailCell = CreateTextCell("B", currentRow, operationPoint.Description);
        
        row.Append(descCell);
        row.Append(detailCell);
        sheetData.Append(row);
        
        return currentRow + 1;
    }

    /// <summary>
    /// 添加错误行
    /// </summary>
    private static void AddErrorRow(SheetData sheetData, uint currentRow, string operationName, string errorMessage)
    {
        Row row = new() { RowIndex = currentRow };
        Cell errorCell = CreateTextCell("A", currentRow, $"错误：{operationName}");
        Cell messageCell = CreateTextCell("B", currentRow, errorMessage);
        
        row.Append(errorCell);
        row.Append(messageCell);
        sheetData.Append(row);
    }

    /// <summary>
    /// 创建文本单元格
    /// </summary>
    private static Cell CreateTextCell(string columnName, uint rowIndex, string text)
    {
        Cell cell = new()
        {
            CellReference = columnName + rowIndex,
            DataType = CellValues.InlineString
        };
        
        InlineString inlineString = new();
        Text textElement = new(text);
        inlineString.Append(textElement);
        cell.Append(inlineString);
        
        return cell;
    }

    /// <summary>
    /// 获取操作点的显示名称
    /// </summary>
    private static string GetOperationDisplayName(OperationPoint operationPoint)
    {
        return !string.IsNullOrEmpty(operationPoint.Name) ? operationPoint.Name : 
               operationPoint.ExcelKnowledgeType?.ToString() ?? "未知操作";
    }

    /// <summary>
    /// 获取参数值
    /// </summary>
    private static string GetParameterValue(OperationPoint operationPoint, string parameterName, string defaultValue)
    {
        ConfigurationParameter? parameter = operationPoint.Parameters.FirstOrDefault(p => p.Name == parameterName);
        return parameter?.Value ?? defaultValue;
    }
}
