using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

namespace BenchSuite.Services.OpenXml;

/// <summary>
/// Excel OpenXML评分服务实现
/// </summary>
public class ExcelOpenXmlScoringService : OpenXmlScoringServiceBase, IExcelScoringService
{
    protected override string[] SupportedExtensions => [".xlsx"];

    /// <summary>
    /// 验证Excel文档格式
    /// </summary>
    protected override bool ValidateDocumentFormat(string filePath)
    {
        try
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false);
            return document.WorkbookPart != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 对Excel文件进行打分（同步版本）
    /// </summary>
    public override ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = CreateBaseScoringResult();

        try
        {
            if (!ValidateDocument(filePath))
            {
                result.ErrorMessage = $"无效的Excel文档: {filePath}";
                return result;
            }

            // 获取Excel模块
            ExamModuleModel? excelModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Excel);
            if (excelModule == null)
            {
                result.ErrorMessage = "试卷中未找到Excel模块，跳过Excel评分";
                result.IsSuccess = true;
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
                return result;
            }

            // 收集所有操作点并记录题目关联关系
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in excelModule.Questions)
            {
                foreach (OperationPointModel operationPoint in question.OperationPoints)
                {
                    allOperationPoints.Add(operationPoint);
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "Excel模块中未找到操作点";
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, allOperationPoints).Result;

            // 为每个知识点结果设置题目关联信息
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                if (operationPointToQuestionMap.TryGetValue(kpResult.KnowledgePointId, out string? questionId))
                {
                    kpResult.QuestionId = questionId;
                    // 查找题目标题
                    QuestionModel? question = excelModule.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (question != null)
                    {
                        // 可以在这里添加更多题目信息，如果需要的话
                    }
                }
            }

            FinalizeScoringResult(result, allOperationPoints);
        }
        catch (Exception ex)
        {
            HandleException(ex, result);
        }

        return result;
    }

    /// <summary>
    /// 对单个题目进行评分（同步版本）
    /// </summary>
    protected override ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = CreateBaseScoringResult(question.Id, question.Title);

        try
        {
            if (!ValidateDocument(filePath))
            {
                result.ErrorMessage = $"无效的Excel文档: {filePath}";
                return result;
            }

            // 获取题目的操作点（只处理Excel相关的操作点）
            List<OperationPointModel> excelOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.Excel && op.IsEnabled)];

            if (excelOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何Excel操作点";
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, excelOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            FinalizeScoringResult(result, excelOperationPoints);
        }
        catch (Exception ex)
        {
            HandleException(ex, result);
        }

        return result;
    }

    /// <summary>
    /// 检测Excel中的特定知识点
    /// </summary>
    public async Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters)
    {
        return await Task.Run(() =>
        {
            KnowledgePointResult result = new()
            {
                KnowledgePointType = knowledgePointType,
                Parameters = parameters
            };

            try
            {
                if (!ValidateDocument(filePath))
                {
                    result.ErrorMessage = "无效的Excel文档";
                    return result;
                }

                using SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false);
                result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测知识点时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }

            return result;
        });
    }

    /// <summary>
    /// 批量检测Excel中的知识点
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];

            try
            {
                if (!ValidateDocument(filePath))
                {
                    // 为所有知识点返回错误结果
                    foreach (OperationPointModel operationPoint in knowledgePoints)
                    {
                        KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.ExcelKnowledgeType ?? string.Empty);
                        SetKnowledgePointFailure(errorResult, "无效的Excel文档");
                        results.Add(errorResult);
                    }
                    return results;
                }

                using SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false);

                // 逐个检测知识点
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    try
                    {
                        Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                        string knowledgePointType = MapOperationPointNameToKnowledgeType(operationPoint.Name);

                        KnowledgePointResult result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);
                        
                        result.KnowledgePointId = operationPoint.Id;
                        result.OperationPointId = operationPoint.Id;
                        result.KnowledgePointName = operationPoint.Name;
                        result.TotalScore = operationPoint.Score;

                        // 重新计算得分，确保使用正确的TotalScore
                        if (result.IsCorrect && result.AchievedScore == 0)
                        {
                            result.AchievedScore = result.TotalScore;
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.ExcelKnowledgeType ?? string.Empty);
                        SetKnowledgePointFailure(errorResult, $"检测失败: {ex.Message}");
                        results.Add(errorResult);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果无法打开文件，为所有知识点返回错误结果
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.ExcelKnowledgeType ?? string.Empty);
                    SetKnowledgePointFailure(errorResult, $"无法打开Excel文件: {ex.Message}");
                    results.Add(errorResult);
                }
            }

            return results;
        });
    }

    /// <summary>
    /// 映射Excel操作点名称到知识点类型
    /// </summary>
    protected override string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        return operationPointName switch
        {
            // Excel特定映射
            var name when name.Contains("FillOrCopy") => "FillOrCopyCellContent",
            var name when name.Contains("DeleteCell") => "DeleteCellContent",
            var name when name.Contains("InsertDelete") && name.Contains("Cells") => "InsertDeleteCells",
            var name when name.Contains("MergeCells") => "MergeCells",
            var name when name.Contains("InsertDelete") && name.Contains("Rows") => "InsertDeleteRows",
            var name when name.Contains("CellFont") => "SetCellFont",
            var name when name.Contains("FontStyle") => "SetFontStyle",
            var name when name.Contains("FontSize") => "SetFontSize",
            var name when name.Contains("FontColor") => "SetFontColor",
            var name when name.Contains("CellAlignment") => "SetCellAlignment",
            var name when name.Contains("CellBorder") => "SetCellBorder",
            var name when name.Contains("CellBackground") => "SetCellBackgroundColor",
            var name when name.Contains("NumberFormat") => "SetNumberFormat",
            var name when name.Contains("Formula") => "SetFormula",
            var name when name.Contains("Function") => "UseFunction",
            var name when name.Contains("Chart") => "CreateChart",
            var name when name.Contains("Filter") => "SetAutoFilter",
            var name when name.Contains("Sort") => "SortData",
            var name when name.Contains("PivotTable") => "CreatePivotTable",
            var name when name.Contains("ConditionalFormat") => "SetConditionalFormatting",
            var name when name.Contains("DataValidation") => "SetDataValidation",
            var name when name.Contains("Freeze") => "FreezePanes",
            var name when name.Contains("PageSetup") => "SetPageSetup",
            var name when name.Contains("PrintArea") => "SetPrintArea",
            var name when name.Contains("HeaderFooter") => "SetHeaderFooter",
            var name when name.Contains("Worksheet") => "ManageWorksheet",
            var name when name.Contains("Protection") => "SetWorksheetProtection",
            _ => base.MapOperationPointNameToKnowledgeType(operationPointName)
        };
    }

    /// <summary>
    /// 检测特定知识点
    /// </summary>
    private KnowledgePointResult DetectSpecificKnowledgePoint(SpreadsheetDocument document, string knowledgePointType, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = knowledgePointType,
            Parameters = parameters
        };

        try
        {
            // 根据知识点类型进行具体检测
            switch (knowledgePointType)
            {
                // 第一类：Excel基础操作
                case "FillOrCopyCellContent":
                    result = DetectFillOrCopyCellContent(document, parameters);
                    break;
                case "DeleteCellContent":
                    result = DetectDeleteCellContent(document, parameters);
                    break;
                case "InsertDeleteCells":
                    result = DetectInsertDeleteCells(document, parameters);
                    break;
                case "MergeCells":
                    result = DetectMergeCells(document, parameters);
                    break;
                case "InsertDeleteRows":
                    result = DetectInsertDeleteRows(document, parameters);
                    break;
                case "SetCellFont":
                    result = DetectSetCellFont(document, parameters);
                    break;
                case "SetFontStyle":
                    result = DetectSetFontStyle(document, parameters);
                    break;
                case "SetFontSize":
                    result = DetectSetFontSize(document, parameters);
                    break;
                case "SetFontColor":
                    result = DetectSetFontColor(document, parameters);
                    break;
                case "SetCellAlignment":
                    result = DetectSetCellAlignment(document, parameters);
                    break;
                case "SetCellBorder":
                    result = DetectSetCellBorder(document, parameters);
                    break;
                case "SetCellBackgroundColor":
                    result = DetectSetCellBackgroundColor(document, parameters);
                    break;
                case "SetNumberFormat":
                    result = DetectSetNumberFormat(document, parameters);
                    break;
                case "SetFormula":
                    result = DetectSetFormula(document, parameters);
                    break;
                case "UseFunction":
                    result = DetectUseFunction(document, parameters);
                    break;
                case "CreateChart":
                    result = DetectCreateChart(document, parameters);
                    break;
                case "SetAutoFilter":
                    result = DetectSetAutoFilter(document, parameters);
                    break;
                case "SortData":
                    result = DetectSortData(document, parameters);
                    break;
                case "CreatePivotTable":
                    result = DetectCreatePivotTable(document, parameters);
                    break;
                case "SetConditionalFormatting":
                    result = DetectSetConditionalFormatting(document, parameters);
                    break;
                case "SetDataValidation":
                    result = DetectSetDataValidation(document, parameters);
                    break;
                case "FreezePanes":
                    result = DetectFreezePanes(document, parameters);
                    break;
                case "SetPageSetup":
                    result = DetectSetPageSetup(document, parameters);
                    break;
                case "SetPrintArea":
                    result = DetectSetPrintArea(document, parameters);
                    break;
                case "SetHeaderFooter":
                    result = DetectSetHeaderFooter(document, parameters);
                    break;
                case "ManageWorksheet":
                    result = DetectManageWorksheet(document, parameters);
                    break;
                case "SetWorksheetProtection":
                    result = DetectSetWorksheetProtection(document, parameters);
                    break;
                default:
                    result.ErrorMessage = $"不支持的知识点类型: {knowledgePointType}";
                    result.IsCorrect = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测知识点 {knowledgePointType} 时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测单元格内容填充或复制
    /// </summary>
    private KnowledgePointResult DetectFillOrCopyCellContent(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "FillOrCopyCellContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "CellRange", out string cellRange) ||
                !TryGetParameter(parameters, "ExpectedValue", out string expectedValue))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: CellRange 或 ExpectedValue");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            WorksheetPart? worksheetPart = GetActiveWorksheet(workbookPart);

            if (worksheetPart == null)
            {
                SetKnowledgePointFailure(result, "无法获取活动工作表");
                return result;
            }

            string actualValue = GetCellValue(worksheetPart, cellRange, workbookPart);

            result.ExpectedValue = expectedValue;
            result.ActualValue = actualValue;
            result.IsCorrect = TextEquals(actualValue, expectedValue);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格 {cellRange} 内容检测: 期望 '{expectedValue}', 实际 '{actualValue}'";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测删除单元格内容
    /// </summary>
    private KnowledgePointResult DetectDeleteCellContent(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DeleteCellContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "CellRange", out string cellRange))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: CellRange");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            WorksheetPart? worksheetPart = GetActiveWorksheet(workbookPart);

            if (worksheetPart == null)
            {
                SetKnowledgePointFailure(result, "无法获取活动工作表");
                return result;
            }

            string actualValue = GetCellValue(worksheetPart, cellRange, workbookPart);
            bool isEmpty = string.IsNullOrEmpty(actualValue);

            result.ExpectedValue = "空单元格";
            result.ActualValue = isEmpty ? "空单元格" : $"'{actualValue}'";
            result.IsCorrect = isEmpty;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格 {cellRange} 删除检测: 期望为空, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测删除单元格内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取活动工作表
    /// </summary>
    private WorksheetPart? GetActiveWorksheet(WorkbookPart workbookPart)
    {
        try
        {
            // 获取第一个工作表作为活动工作表
            WorksheetPart? worksheetPart = workbookPart.WorksheetParts.FirstOrDefault();
            return worksheetPart;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取指定单元格的值
    /// </summary>
    private string GetCellValue(WorksheetPart worksheetPart, string cellReference, WorkbookPart workbookPart)
    {
        try
        {
            Cell? cell = GetCell(worksheetPart.Worksheet, cellReference);
            if (cell?.CellValue?.Text == null)
            {
                return string.Empty;
            }

            string value = cell.CellValue.Text;

            // 如果是共享字符串，需要从共享字符串表中获取实际值
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                SharedStringTablePart? sharedStringPart = workbookPart.SharedStringTablePart;
                if (sharedStringPart?.SharedStringTable != null && int.TryParse(value, out int index))
                {
                    SharedStringItem? item = sharedStringPart.SharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(index);
                    if (item?.Text?.Text != null)
                    {
                        return item.Text.Text;
                    }
                }
            }

            return value;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取指定单元格
    /// </summary>
    private Cell? GetCell(Worksheet worksheet, string cellReference)
    {
        try
        {
            return worksheet.Descendants<Cell>().FirstOrDefault(c => c.CellReference?.Value == cellReference);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 检测公式设置
    /// </summary>
    private KnowledgePointResult DetectSetFormula(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFormula",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "CellRange", out string cellRange) ||
                !TryGetParameter(parameters, "ExpectedFormula", out string expectedFormula))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: CellRange 或 ExpectedFormula");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            WorksheetPart? worksheetPart = GetActiveWorksheet(workbookPart);

            if (worksheetPart == null)
            {
                SetKnowledgePointFailure(result, "无法获取活动工作表");
                return result;
            }

            Cell? cell = GetCell(worksheetPart.Worksheet, cellRange);
            string actualFormula = cell?.CellFormula?.Text ?? string.Empty;

            result.ExpectedValue = expectedFormula;
            result.ActualValue = actualFormula;
            result.IsCorrect = TextEquals(actualFormula, expectedFormula);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格 {cellRange} 公式检测: 期望 '{expectedFormula}', 实际 '{actualFormula}'";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测公式设置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自动筛选
    /// </summary>
    private KnowledgePointResult DetectSetAutoFilter(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoFilter",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            WorksheetPart? worksheetPart = GetActiveWorksheet(workbookPart);

            if (worksheetPart == null)
            {
                SetKnowledgePointFailure(result, "无法获取活动工作表");
                return result;
            }

            AutoFilter? autoFilter = worksheetPart.Worksheet.Elements<AutoFilter>().FirstOrDefault();
            bool hasAutoFilter = autoFilter != null;

            if (TryGetParameter(parameters, "DataRange", out string expectedRange))
            {
                string actualRange = autoFilter?.Reference?.Value ?? string.Empty;
                result.ExpectedValue = expectedRange;
                result.ActualValue = actualRange;
                result.IsCorrect = hasAutoFilter && TextEquals(actualRange, expectedRange);
                result.Details = $"自动筛选检测: 期望范围 '{expectedRange}', 实际范围 '{actualRange}'";
            }
            else
            {
                result.ExpectedValue = "存在自动筛选";
                result.ActualValue = hasAutoFilter ? "存在自动筛选" : "无自动筛选";
                result.IsCorrect = hasAutoFilter;
                result.Details = $"自动筛选检测: {result.ActualValue}";
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自动筛选失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入删除单元格
    /// </summary>
    private KnowledgePointResult DetectInsertDeleteCells(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertDeleteCells",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var cellOperationInfo = CheckCellOperationsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "插入或删除单元格操作";
            result.ActualValue = cellOperationInfo.Found ? cellOperationInfo.Description : "未检测到单元格操作";
            result.IsCorrect = cellOperationInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格操作检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入删除单元格失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测合并单元格
    /// </summary>
    private KnowledgePointResult DetectMergeCells(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MergeCells",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var mergedCellsInfo = CheckMergedCellsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "合并单元格";
            result.ActualValue = mergedCellsInfo.Found ? $"找到 {mergedCellsInfo.Count} 个合并区域" : "未找到合并单元格";
            result.IsCorrect = mergedCellsInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"合并单元格检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测合并单元格失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入删除行
    /// </summary>
    private KnowledgePointResult DetectInsertDeleteRows(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertDeleteRows",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var rowOperationInfo = CheckRowOperationsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "插入或删除行操作";
            result.ActualValue = rowOperationInfo.Found ? rowOperationInfo.Description : "未检测到行操作";
            result.IsCorrect = rowOperationInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"行操作检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入删除行失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测单元格字体
    /// </summary>
    private KnowledgePointResult DetectSetCellFont(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontName", out string expectedFont))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontName");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool fontFound = CheckFontInWorkbook(workbookPart, expectedFont);

            result.ExpectedValue = expectedFont;
            result.ActualValue = fontFound ? expectedFont : "未找到指定字体";
            result.IsCorrect = fontFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格字体检测: 期望 {expectedFont}, {(fontFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格字体失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测字体样式
    /// </summary>
    private KnowledgePointResult DetectSetFontStyle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "StyleType", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: StyleType");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool styleFound = CheckFontStyleInWorkbook(workbookPart, expectedStyle);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = styleFound ? expectedStyle : "未找到指定样式";
            result.IsCorrect = styleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体样式检测: 期望 {expectedStyle}, {(styleFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测字体样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测字体大小
    /// </summary>
    private KnowledgePointResult DetectSetFontSize(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontSize", out string expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontSize");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool sizeFound = CheckFontSizeInWorkbook(workbookPart, expectedSize);

            result.ExpectedValue = expectedSize;
            result.ActualValue = sizeFound ? expectedSize : "未找到指定字号";
            result.IsCorrect = sizeFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体大小检测: 期望 {expectedSize}, {(sizeFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测字体大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测字体颜色
    /// </summary>
    private KnowledgePointResult DetectSetFontColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontColor");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool colorFound = CheckFontColorInWorkbook(workbookPart, expectedColor);

            result.ExpectedValue = expectedColor;
            result.ActualValue = colorFound ? expectedColor : "未找到指定颜色";
            result.IsCorrect = colorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体颜色检测: 期望 {expectedColor}, {(colorFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测字体颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测单元格对齐
    /// </summary>
    private KnowledgePointResult DetectSetCellAlignment(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "Alignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: Alignment");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool alignmentFound = CheckCellAlignmentInWorkbook(workbookPart, expectedAlignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = alignmentFound ? expectedAlignment : "未找到指定对齐方式";
            result.IsCorrect = alignmentFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格对齐检测: 期望 {expectedAlignment}, {(alignmentFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测单元格边框
    /// </summary>
    private KnowledgePointResult DetectSetCellBorder(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellBorder",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool borderFound = CheckCellBorderInWorkbook(workbookPart);

            result.ExpectedValue = "单元格边框";
            result.ActualValue = borderFound ? "找到单元格边框" : "未找到单元格边框";
            result.IsCorrect = borderFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格边框检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格边框失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测单元格背景色
    /// </summary>
    private KnowledgePointResult DetectSetCellBackgroundColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellBackgroundColor",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool backgroundFound = CheckCellBackgroundColorInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "单元格背景色";
            result.ActualValue = backgroundFound ? "找到单元格背景色" : "未找到单元格背景色";
            result.IsCorrect = backgroundFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格背景色检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格背景色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数字格式
    /// </summary>
    private KnowledgePointResult DetectSetNumberFormat(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetNumberFormat",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool numberFormatFound = CheckNumberFormatInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数字格式设置";
            result.ActualValue = numberFormatFound ? "找到数字格式设置" : "未找到数字格式设置";
            result.IsCorrect = numberFormatFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数字格式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数字格式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测函数使用
    /// </summary>
    private KnowledgePointResult DetectUseFunction(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "UseFunction",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var functionInfo = CheckFunctionUsageInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "FunctionName", out string expectedFunction) ? expectedFunction : "函数使用";
            result.ActualValue = functionInfo.Found ? $"找到函数: {functionInfo.FunctionName}" : "未找到函数使用";
            result.IsCorrect = functionInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"函数使用检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测函数使用失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表创建
    /// </summary>
    private KnowledgePointResult DetectCreateChart(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CreateChart",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var chartInfo = CheckChartInWorkbook(workbookPart);

            result.ExpectedValue = "图表";
            result.ActualValue = chartInfo.Found ? $"找到 {chartInfo.Count} 个图表" : "未找到图表";
            result.IsCorrect = chartInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表创建检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表创建失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据排序
    /// </summary>
    private KnowledgePointResult DetectSortData(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SortData",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool sortFound = CheckDataSortInWorkbook(workbookPart);

            result.ExpectedValue = "数据排序";
            result.ActualValue = sortFound ? "找到数据排序" : "未找到数据排序";
            result.IsCorrect = sortFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据排序检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数据排序失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据透视表
    /// </summary>
    private KnowledgePointResult DetectCreatePivotTable(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CreatePivotTable",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool pivotTableFound = CheckPivotTableInWorkbook(workbookPart);

            result.ExpectedValue = "数据透视表";
            result.ActualValue = pivotTableFound ? "找到数据透视表" : "未找到数据透视表";
            result.IsCorrect = pivotTableFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据透视表检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数据透视表失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测条件格式
    /// </summary>
    private KnowledgePointResult DetectSetConditionalFormatting(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetConditionalFormatting",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool conditionalFormattingFound = CheckConditionalFormattingInWorkbook(workbookPart);

            result.ExpectedValue = "条件格式";
            result.ActualValue = conditionalFormattingFound ? "找到条件格式" : "未找到条件格式";
            result.IsCorrect = conditionalFormattingFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"条件格式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测条件格式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据验证
    /// </summary>
    private KnowledgePointResult DetectSetDataValidation(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetDataValidation",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool dataValidationFound = CheckDataValidationInWorkbook(workbookPart);

            result.ExpectedValue = "数据验证";
            result.ActualValue = dataValidationFound ? "找到数据验证" : "未找到数据验证";
            result.IsCorrect = dataValidationFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据验证检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数据验证失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测冻结窗格
    /// </summary>
    private KnowledgePointResult DetectFreezePanes(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "FreezePanes",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool freezePanesFound = CheckFreezePanesInWorkbook(workbookPart);

            result.ExpectedValue = "冻结窗格";
            result.ActualValue = freezePanesFound ? "找到冻结窗格" : "未找到冻结窗格";
            result.IsCorrect = freezePanesFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"冻结窗格检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测冻结窗格失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面设置
    /// </summary>
    private KnowledgePointResult DetectSetPageSetup(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageSetup",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool pageSetupFound = CheckPageSetupInWorkbook(workbookPart);

            result.ExpectedValue = "页面设置";
            result.ActualValue = pageSetupFound ? "找到页面设置" : "未找到页面设置";
            result.IsCorrect = pageSetupFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面设置检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面设置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测打印区域
    /// </summary>
    private KnowledgePointResult DetectSetPrintArea(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPrintArea",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool printAreaFound = CheckPrintAreaInWorkbook(workbookPart);

            result.ExpectedValue = "打印区域";
            result.ActualValue = printAreaFound ? "找到打印区域设置" : "未找到打印区域设置";
            result.IsCorrect = printAreaFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"打印区域检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测打印区域失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页眉页脚
    /// </summary>
    private KnowledgePointResult DetectSetHeaderFooter(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderFooter",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool headerFooterFound = CheckHeaderFooterInWorkbook(workbookPart);

            result.ExpectedValue = "页眉页脚";
            result.ActualValue = headerFooterFound ? "找到页眉页脚设置" : "未找到页眉页脚设置";
            result.IsCorrect = headerFooterFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉页脚检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页眉页脚失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测工作表管理
    /// </summary>
    private KnowledgePointResult DetectManageWorksheet(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ManageWorksheet",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            int worksheetCount = GetWorksheetCountInWorkbook(workbookPart);

            result.ExpectedValue = "多个工作表";
            result.ActualValue = $"工作簿包含 {worksheetCount} 个工作表";
            result.IsCorrect = worksheetCount > 1;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"工作表管理检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测工作表管理失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测工作表保护
    /// </summary>
    private KnowledgePointResult DetectSetWorksheetProtection(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWorksheetProtection",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool protectionFound = CheckWorksheetProtectionInWorkbook(workbookPart);

            result.ExpectedValue = "工作表保护";
            result.ActualValue = protectionFound ? "找到工作表保护" : "未找到工作表保护";
            result.IsCorrect = protectionFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"工作表保护检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测工作表保护失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检查工作簿中的单元格操作
    /// </summary>
    private (bool Found, string Description) CheckCellOperationsInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有数据，认为有数据操作
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    return (true, "检测到单元格数据操作");
                }
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的合并单元格
    /// </summary>
    private (bool Found, int Count) CheckMergedCellsInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            int mergedCellCount = 0;
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var mergeCells = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.MergeCells>().FirstOrDefault();
                if (mergeCells?.HasChildren == true)
                {
                    mergedCellCount += mergeCells.Elements<DocumentFormat.OpenXml.Spreadsheet.MergeCell>().Count();
                }
            }
            return (mergedCellCount > 0, mergedCellCount);
        }
        catch
        {
            return (false, 0);
        }
    }

    /// <summary>
    /// 检查工作簿中的行操作
    /// </summary>
    private (bool Found, string Description) CheckRowOperationsInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有多行数据
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                if (sheetData?.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().Count() > 1)
                {
                    return (true, "检测到多行数据操作");
                }
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的字体
    /// </summary>
    private bool CheckFontInWorkbook(WorkbookPart workbookPart, string expectedFont)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (var font in stylesPart.Stylesheet.Fonts.Elements<DocumentFormat.OpenXml.Spreadsheet.Font>())
                {
                    var fontName = font.FontName?.Val?.Value;
                    if (fontName != null && TextEquals(fontName, expectedFont))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的字体样式
    /// </summary>
    private bool CheckFontStyleInWorkbook(WorkbookPart workbookPart, string expectedStyle)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (var font in stylesPart.Stylesheet.Fonts.Elements<DocumentFormat.OpenXml.Spreadsheet.Font>())
                {
                    bool hasStyle = expectedStyle.ToLowerInvariant() switch
                    {
                        "bold" or "粗体" => font.Bold != null,
                        "italic" or "斜体" => font.Italic != null,
                        "underline" or "下划线" => font.Underline != null,
                        "strikethrough" or "删除线" => font.Strike != null,
                        _ => false
                    };

                    if (hasStyle) return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的字体大小
    /// </summary>
    private bool CheckFontSizeInWorkbook(WorkbookPart workbookPart, string expectedSize)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (var font in stylesPart.Stylesheet.Fonts.Elements<DocumentFormat.OpenXml.Spreadsheet.Font>())
                {
                    var fontSize = font.FontSize?.Val?.Value.ToString();
                    if (fontSize != null && TextEquals(fontSize, expectedSize))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的字体颜色
    /// </summary>
    private bool CheckFontColorInWorkbook(WorkbookPart workbookPart, string expectedColor)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (var font in stylesPart.Stylesheet.Fonts.Elements<DocumentFormat.OpenXml.Spreadsheet.Font>())
                {
                    var color = font.Color?.Rgb?.Value;
                    if (color != null && (TextEquals(color, expectedColor) || TextEquals("#" + color, expectedColor)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的单元格对齐
    /// </summary>
    private bool CheckCellAlignmentInWorkbook(WorkbookPart workbookPart, string expectedAlignment)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellFormats?.HasChildren == true)
            {
                foreach (var cellFormat in stylesPart.Stylesheet.CellFormats.Elements<DocumentFormat.OpenXml.Spreadsheet.CellFormat>())
                {
                    var alignment = cellFormat.Alignment;
                    if (alignment?.Horizontal?.Value != null)
                    {
                        string alignmentValue = alignment.Horizontal.Value.ToString();
                        if (TextEquals(alignmentValue, expectedAlignment))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的单元格边框
    /// </summary>
    private bool CheckCellBorderInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (var border in stylesPart.Stylesheet.Borders.Elements<DocumentFormat.OpenXml.Spreadsheet.Border>())
                {
                    if (border.HasChildren)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的单元格背景色
    /// </summary>
    private bool CheckCellBackgroundColorInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fills?.HasChildren == true)
            {
                foreach (var fill in stylesPart.Stylesheet.Fills.Elements<DocumentFormat.OpenXml.Spreadsheet.Fill>())
                {
                    var patternFill = fill.PatternFill;
                    if (patternFill?.ForegroundColor != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的数字格式
    /// </summary>
    private bool CheckNumberFormatInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.NumberingFormats?.HasChildren == true)
            {
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的函数使用
    /// </summary>
    private (bool Found, string FunctionName) CheckFunctionUsageInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedFunction = TryGetParameter(parameters, "FunctionName", out string expected) ? expected : "";

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
                    {
                        foreach (var cell in row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>())
                        {
                            var cellFormula = cell.CellFormula?.Text;
                            if (!string.IsNullOrEmpty(cellFormula))
                            {
                                if (string.IsNullOrEmpty(expectedFunction) || cellFormula.Contains(expectedFunction, StringComparison.OrdinalIgnoreCase))
                                {
                                    return (true, cellFormula);
                                }
                            }
                        }
                    }
                }
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图表
    /// </summary>
    private (bool Found, int Count) CheckChartInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            int chartCount = 0;
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                chartCount += worksheetPart.ChartsheetParts.Count();
                chartCount += worksheetPart.DrawingsPart?.ChartParts.Count() ?? 0;
            }
            return (chartCount > 0, chartCount);
        }
        catch
        {
            return (false, 0);
        }
    }

    /// <summary>
    /// 检查工作簿中的数据排序
    /// </summary>
    private bool CheckDataSortInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            // 简化实现：检查是否有排序状态
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var autoFilter = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.AutoFilter>().FirstOrDefault();
                if (autoFilter != null)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的数据透视表
    /// </summary>
    private bool CheckPivotTableInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            return workbookPart.PivotTableCacheDefinitionParts.Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的条件格式
    /// </summary>
    private bool CheckConditionalFormattingInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var conditionalFormatting = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.ConditionalFormatting>().FirstOrDefault();
                if (conditionalFormatting != null)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的数据验证
    /// </summary>
    private bool CheckDataValidationInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var dataValidations = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.DataValidations>().FirstOrDefault();
                if (dataValidations?.HasChildren == true)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的冻结窗格
    /// </summary>
    private bool CheckFreezePanesInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetViews = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetViews>();
                if (sheetViews?.HasChildren == true)
                {
                    foreach (var sheetView in sheetViews.Elements<DocumentFormat.OpenXml.Spreadsheet.SheetView>())
                    {
                        var pane = sheetView.Pane;
                        if (pane?.State?.Value == DocumentFormat.OpenXml.Spreadsheet.PaneStateValues.Frozen)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的页面设置
    /// </summary>
    private bool CheckPageSetupInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var pageSetup = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.PageSetup>();
                if (pageSetup != null)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的打印区域
    /// </summary>
    private bool CheckPrintAreaInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            var definedNames = workbookPart.Workbook.DefinedNames;
            if (definedNames?.HasChildren == true)
            {
                foreach (var definedName in definedNames.Elements<DocumentFormat.OpenXml.Spreadsheet.DefinedName>())
                {
                    if (definedName.Name?.Value == "Print_Area")
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的页眉页脚
    /// </summary>
    private bool CheckHeaderFooterInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var headerFooter = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.HeaderFooter>();
                if (headerFooter?.HasChildren == true)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取工作簿中的工作表数量
    /// </summary>
    private int GetWorksheetCountInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            return workbookPart.WorksheetParts.Count();
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// 检查工作簿中的工作表保护
    /// </summary>
    private bool CheckWorksheetProtectionInWorkbook(WorkbookPart workbookPart)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetProtection = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetProtection>();
                if (sheetProtection != null)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的超链接
    /// </summary>
    private (bool Found, string Url) CheckHyperlinkInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var hyperlinks = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.Hyperlinks>().FirstOrDefault();
                if (hyperlinks?.HasChildren == true)
                {
                    var firstHyperlink = hyperlinks.Elements<DocumentFormat.OpenXml.Spreadsheet.Hyperlink>().FirstOrDefault();
                    if (firstHyperlink?.Id?.Value != null)
                    {
                        try
                        {
                            var relationship = worksheetPart.GetReferenceRelationship(firstHyperlink.Id.Value);
                            return (true, relationship?.Uri?.ToString() ?? "超链接存在");
                        }
                        catch
                        {
                            return (true, "超链接存在");
                        }
                    }
                    return (true, "超链接存在");
                }
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }
}
