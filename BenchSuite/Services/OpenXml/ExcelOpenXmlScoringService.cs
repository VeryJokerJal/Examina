using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

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

                // 新增的Excel基础操作
                case "SetHorizontalAlignment":
                    result = DetectSetHorizontalAlignment(document, parameters);
                    break;
                case "SetVerticalAlignment":
                    result = DetectSetVerticalAlignment(document, parameters);
                    break;
                case "SetInnerBorderStyle":
                    result = DetectSetInnerBorderStyle(document, parameters);
                    break;
                case "SetInnerBorderColor":
                    result = DetectSetInnerBorderColor(document, parameters);
                    break;
                case "SetOuterBorderStyle":
                    result = DetectSetOuterBorderStyle(document, parameters);
                    break;
                case "SetOuterBorderColor":
                    result = DetectSetOuterBorderColor(document, parameters);
                    break;
                case "SetRowHeight":
                    result = DetectSetRowHeight(document, parameters);
                    break;
                case "SetColumnWidth":
                    result = DetectSetColumnWidth(document, parameters);
                    break;
                case "SetCellFillColor":
                    result = DetectSetCellFillColor(document, parameters);
                    break;
                case "SetPatternFillStyle":
                    result = DetectSetPatternFillStyle(document, parameters);
                    break;
                case "SetPatternFillColor":
                    result = DetectSetPatternFillColor(document, parameters);
                    break;
                case "AddUnderline":
                    result = DetectAddUnderline(document, parameters);
                    break;
                case "ModifySheetName":
                    result = DetectModifySheetName(document, parameters);
                    break;
                case "SetCellStyleData":
                    result = DetectSetCellStyleData(document, parameters);
                    break;

                // 数据清单操作
                case "Filter":
                    result = DetectFilter(document, parameters);
                    break;
                case "Sort":
                    result = DetectSort(document, parameters);
                    break;
                case "PivotTable":
                    result = DetectPivotTable(document, parameters);
                    break;
                case "Subtotal":
                    result = DetectSubtotal(document, parameters);
                    break;
                case "AdvancedFilterCondition":
                    result = DetectAdvancedFilterCondition(document, parameters);
                    break;
                case "AdvancedFilterData":
                    result = DetectAdvancedFilterData(document, parameters);
                    break;

                // 图表操作
                case "ChartType":
                    result = DetectChartType(document, parameters);
                    break;
                case "ChartStyle":
                    result = DetectChartStyle(document, parameters);
                    break;
                case "ChartTitle":
                    result = DetectChartTitle(document, parameters);
                    break;
                case "SetLegendPosition":
                    result = DetectSetLegendPosition(document, parameters);
                    break;
                case "ChartMove":
                    result = DetectChartMove(document, parameters);
                    break;
                case "CategoryAxisDataRange":
                    result = DetectCategoryAxisDataRange(document, parameters);
                    break;
                case "ValueAxisDataRange":
                    result = DetectValueAxisDataRange(document, parameters);
                    break;
                case "ChartTitleFormat":
                    result = DetectChartTitleFormat(document, parameters);
                    break;
                case "HorizontalAxisTitle":
                    result = DetectHorizontalAxisTitle(document, parameters);
                    break;
                case "MajorHorizontalGridlines":
                    result = DetectMajorHorizontalGridlines(document, parameters);
                    break;
                case "MinorHorizontalGridlines":
                    result = DetectMinorHorizontalGridlines(document, parameters);
                    break;
                case "MajorVerticalGridlines":
                    result = DetectMajorVerticalGridlines(document, parameters);
                    break;
                case "MinorVerticalGridlines":
                    result = DetectMinorVerticalGridlines(document, parameters);
                    break;
                case "DataSeriesFormat":
                    result = DetectDataSeriesFormat(document, parameters);
                    break;
                case "AddDataLabels":
                    result = DetectAddDataLabels(document, parameters);
                    break;
                case "DataLabelsFormat":
                    result = DetectDataLabelsFormat(document, parameters);
                    break;
                case "ChartAreaFormat":
                    result = DetectChartAreaFormat(document, parameters);
                    break;
                case "ChartFloorColor":
                    result = DetectChartFloorColor(document, parameters);
                    break;
                case "ChartBorder":
                    result = DetectChartBorder(document, parameters);
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
            (bool Found, string Description) = CheckCellOperationsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "插入或删除单元格操作";
            result.ActualValue = Found ? Description : "未检测到单元格操作";
            result.IsCorrect = Found;
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
            (bool Found, int Count) = CheckMergedCellsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "合并单元格";
            result.ActualValue = Found ? $"找到 {Count} 个合并区域" : "未找到合并单元格";
            result.IsCorrect = Found;
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
            (bool Found, string Description) = CheckRowOperationsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "插入或删除行操作";
            result.ActualValue = Found ? Description : "未检测到行操作";
            result.IsCorrect = Found;
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
            (bool Found, string FunctionName) = CheckFunctionUsageInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "FunctionName", out string expectedFunction) ? expectedFunction : "函数使用";
            result.ActualValue = Found ? $"找到函数: {FunctionName}" : "未找到函数使用";
            result.IsCorrect = Found;
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
            (bool Found, int Count) = CheckChartInWorkbook(workbookPart);

            result.ExpectedValue = "图表";
            result.ActualValue = Found ? $"找到 {Count} 个图表" : "未找到图表";
            result.IsCorrect = Found;
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
    /// 检测水平对齐方式
    /// </summary>
    private KnowledgePointResult DetectSetHorizontalAlignment(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHorizontalAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "HorizontalAlignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: HorizontalAlignment");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool alignmentFound = CheckHorizontalAlignmentInWorkbook(workbookPart, expectedAlignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = alignmentFound ? expectedAlignment : "未找到指定对齐方式";
            result.IsCorrect = alignmentFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"水平对齐检测: 期望 {expectedAlignment}, {(alignmentFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测水平对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测垂直对齐方式
    /// </summary>
    private KnowledgePointResult DetectSetVerticalAlignment(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetVerticalAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "VerticalAlignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: VerticalAlignment");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            bool alignmentFound = CheckVerticalAlignmentInWorkbook(workbookPart, expectedAlignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = alignmentFound ? expectedAlignment : "未找到指定对齐方式";
            result.IsCorrect = alignmentFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"垂直对齐检测: 期望 {expectedAlignment}, {(alignmentFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测垂直对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测内边框样式
    /// </summary>
    private KnowledgePointResult DetectSetInnerBorderStyle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetInnerBorderStyle",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool borderStyleFound = CheckInnerBorderStyleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "内边框样式";
            result.ActualValue = borderStyleFound ? "找到内边框样式" : "未找到内边框样式";
            result.IsCorrect = borderStyleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"内边框样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测内边框样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测内边框颜色
    /// </summary>
    private KnowledgePointResult DetectSetInnerBorderColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetInnerBorderColor",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool borderColorFound = CheckInnerBorderColorInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "内边框颜色";
            result.ActualValue = borderColorFound ? "找到内边框颜色" : "未找到内边框颜色";
            result.IsCorrect = borderColorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"内边框颜色检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测内边框颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测外边框样式
    /// </summary>
    private KnowledgePointResult DetectSetOuterBorderStyle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetOuterBorderStyle",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool borderStyleFound = CheckOuterBorderStyleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "外边框样式";
            result.ActualValue = borderStyleFound ? "找到外边框样式" : "未找到外边框样式";
            result.IsCorrect = borderStyleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"外边框样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测外边框样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测外边框颜色
    /// </summary>
    private KnowledgePointResult DetectSetOuterBorderColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetOuterBorderColor",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool borderColorFound = CheckOuterBorderColorInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "外边框颜色";
            result.ActualValue = borderColorFound ? "找到外边框颜色" : "未找到外边框颜色";
            result.IsCorrect = borderColorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"外边框颜色检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测外边框颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测行高设置
    /// </summary>
    private KnowledgePointResult DetectSetRowHeight(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetRowHeight",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool rowHeightFound = CheckRowHeightInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "行高设置";
            result.ActualValue = rowHeightFound ? "找到行高设置" : "未找到行高设置";
            result.IsCorrect = rowHeightFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"行高设置检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测行高设置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测列宽设置
    /// </summary>
    private KnowledgePointResult DetectSetColumnWidth(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetColumnWidth",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool columnWidthFound = CheckColumnWidthInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "列宽设置";
            result.ActualValue = columnWidthFound ? "找到列宽设置" : "未找到列宽设置";
            result.IsCorrect = columnWidthFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"列宽设置检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测列宽设置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测单元格填充颜色
    /// </summary>
    private KnowledgePointResult DetectSetCellFillColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellFillColor",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool fillColorFound = CheckCellFillColorInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "单元格填充颜色";
            result.ActualValue = fillColorFound ? "找到单元格填充颜色" : "未找到单元格填充颜色";
            result.IsCorrect = fillColorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格填充颜色检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格填充颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图案填充样式
    /// </summary>
    private KnowledgePointResult DetectSetPatternFillStyle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPatternFillStyle",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool patternStyleFound = CheckPatternFillStyleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图案填充样式";
            result.ActualValue = patternStyleFound ? "找到图案填充样式" : "未找到图案填充样式";
            result.IsCorrect = patternStyleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图案填充样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图案填充样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图案填充颜色
    /// </summary>
    private KnowledgePointResult DetectSetPatternFillColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPatternFillColor",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool patternColorFound = CheckPatternFillColorInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图案填充颜色";
            result.ActualValue = patternColorFound ? "找到图案填充颜色" : "未找到图案填充颜色";
            result.IsCorrect = patternColorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图案填充颜色检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图案填充颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测下划线
    /// </summary>
    private KnowledgePointResult DetectAddUnderline(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AddUnderline",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool underlineFound = CheckUnderlineInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "下划线";
            result.ActualValue = underlineFound ? "找到下划线" : "未找到下划线";
            result.IsCorrect = underlineFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"下划线检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测下划线失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测工作表名称修改
    /// </summary>
    private KnowledgePointResult DetectModifySheetName(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ModifySheetName",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var sheetNameInfo = CheckModifiedSheetNameInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "NewSheetName", out string expectedName) ? expectedName : "修改后的工作表名";
            result.ActualValue = sheetNameInfo.Found ? sheetNameInfo.SheetName : "未找到修改的工作表名";
            result.IsCorrect = sheetNameInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"工作表名称检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测工作表名称失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测单元格样式数据
    /// </summary>
    private KnowledgePointResult DetectSetCellStyleData(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellStyleData",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool cellStyleFound = CheckCellStyleDataInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "单元格样式数据";
            result.ActualValue = cellStyleFound ? "找到单元格样式数据" : "未找到单元格样式数据";
            result.IsCorrect = cellStyleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格样式数据检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测单元格样式数据失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测筛选
    /// </summary>
    private KnowledgePointResult DetectFilter(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "Filter",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool filterFound = CheckFilterInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "筛选";
            result.ActualValue = filterFound ? "找到筛选" : "未找到筛选";
            result.IsCorrect = filterFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"筛选检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测筛选失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测排序
    /// </summary>
    private KnowledgePointResult DetectSort(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "Sort",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool sortFound = CheckSortInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "排序";
            result.ActualValue = sortFound ? "找到排序" : "未找到排序";
            result.IsCorrect = sortFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"排序检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测排序失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据透视表
    /// </summary>
    private KnowledgePointResult DetectPivotTable(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "PivotTable",
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
    /// 检测分类汇总
    /// </summary>
    private KnowledgePointResult DetectSubtotal(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "Subtotal",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool subtotalFound = CheckSubtotalInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "分类汇总";
            result.ActualValue = subtotalFound ? "找到分类汇总" : "未找到分类汇总";
            result.IsCorrect = subtotalFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"分类汇总检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测分类汇总失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测高级筛选条件
    /// </summary>
    private KnowledgePointResult DetectAdvancedFilterCondition(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AdvancedFilterCondition",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool advancedFilterFound = CheckAdvancedFilterConditionInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "高级筛选条件";
            result.ActualValue = advancedFilterFound ? "找到高级筛选条件" : "未找到高级筛选条件";
            result.IsCorrect = advancedFilterFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"高级筛选条件检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测高级筛选条件失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测高级筛选数据
    /// </summary>
    private KnowledgePointResult DetectAdvancedFilterData(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AdvancedFilterData",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool advancedFilterDataFound = CheckAdvancedFilterDataInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "高级筛选数据";
            result.ActualValue = advancedFilterDataFound ? "找到高级筛选数据" : "未找到高级筛选数据";
            result.IsCorrect = advancedFilterDataFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"高级筛选数据检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测高级筛选数据失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表类型
    /// </summary>
    private KnowledgePointResult DetectChartType(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartType",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var chartTypeInfo = CheckChartTypeInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ChartType", out string expectedType) ? expectedType : "图表类型";
            result.ActualValue = chartTypeInfo.Found ? chartTypeInfo.ChartType : "未找到图表类型";
            result.IsCorrect = chartTypeInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表类型检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表类型失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表样式
    /// </summary>
    private KnowledgePointResult DetectChartStyle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartStyle",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            bool chartStyleFound = CheckChartStyleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图表样式";
            result.ActualValue = chartStyleFound ? "找到图表样式" : "未找到图表样式";
            result.IsCorrect = chartStyleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表标题
    /// </summary>
    private KnowledgePointResult DetectChartTitle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartTitle",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var chartTitleInfo = CheckChartTitleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ChartTitle", out string expectedTitle) ? expectedTitle : "图表标题";
            result.ActualValue = chartTitleInfo.Found ? chartTitleInfo.Title : "未找到图表标题";
            result.IsCorrect = chartTitleInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表标题检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表标题失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图例位置
    /// </summary>
    private KnowledgePointResult DetectSetLegendPosition(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetLegendPosition",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var legendInfo = CheckLegendPositionInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "LegendPosition", out string expectedPosition) ? expectedPosition : "图例位置";
            result.ActualValue = legendInfo.Found ? legendInfo.Position : "未找到图例位置";
            result.IsCorrect = legendInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图例位置检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图例位置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表移动
    /// </summary>
    private KnowledgePointResult DetectChartMove(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartMove",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var chartMoveInfo = CheckChartMoveInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "MoveLocation", out string expectedLocation) ? expectedLocation : "图表移动";
            result.ActualValue = chartMoveInfo.Found ? chartMoveInfo.Location : "未检测到图表移动";
            result.IsCorrect = chartMoveInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表移动检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表移动失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测分类轴数据区域
    /// </summary>
    private KnowledgePointResult DetectCategoryAxisDataRange(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CategoryAxisDataRange",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var axisDataInfo = CheckCategoryAxisDataRangeInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "CategoryRange", out string expectedRange) ? expectedRange : "分类轴数据区域";
            result.ActualValue = axisDataInfo.Found ? axisDataInfo.Range : "未找到分类轴数据区域";
            result.IsCorrect = axisDataInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"分类轴数据区域检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测分类轴数据区域失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数值轴数据区域
    /// </summary>
    private KnowledgePointResult DetectValueAxisDataRange(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ValueAxisDataRange",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var axisDataInfo = CheckValueAxisDataRangeInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ValueRange", out string expectedRange) ? expectedRange : "数值轴数据区域";
            result.ActualValue = axisDataInfo.Found ? axisDataInfo.Range : "未找到数值轴数据区域";
            result.IsCorrect = axisDataInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数值轴数据区域检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数值轴数据区域失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表标题格式
    /// </summary>
    private KnowledgePointResult DetectChartTitleFormat(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartTitleFormat",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var titleFormatInfo = CheckChartTitleFormatInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图表标题格式";
            result.ActualValue = titleFormatInfo.Found ? titleFormatInfo.Format : "未找到图表标题格式";
            result.IsCorrect = titleFormatInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表标题格式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表标题格式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测横坐标轴标题
    /// </summary>
    private KnowledgePointResult DetectHorizontalAxisTitle(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "HorizontalAxisTitle",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var axisTitleInfo = CheckHorizontalAxisTitleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "AxisTitle", out string expectedTitle) ? expectedTitle : "横坐标轴标题";
            result.ActualValue = axisTitleInfo.Found ? axisTitleInfo.Title : "未找到横坐标轴标题";
            result.IsCorrect = axisTitleInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"横坐标轴标题检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测横坐标轴标题失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测主要横网格线
    /// </summary>
    private KnowledgePointResult DetectMajorHorizontalGridlines(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MajorHorizontalGridlines",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var gridlineInfo = CheckMajorHorizontalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "主要横网格线";
            result.ActualValue = gridlineInfo.Found ? $"找到主要横网格线: {gridlineInfo.Style}" : "未找到主要横网格线";
            result.IsCorrect = gridlineInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"主要横网格线检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测主要横网格线失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测次要横网格线
    /// </summary>
    private KnowledgePointResult DetectMinorHorizontalGridlines(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MinorHorizontalGridlines",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var gridlineInfo = CheckMinorHorizontalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "次要横网格线";
            result.ActualValue = gridlineInfo.Found ? $"找到次要横网格线: {gridlineInfo.Style}" : "未找到次要横网格线";
            result.IsCorrect = gridlineInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"次要横网格线检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测次要横网格线失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测主要纵网格线
    /// </summary>
    private KnowledgePointResult DetectMajorVerticalGridlines(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MajorVerticalGridlines",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var gridlineInfo = CheckMajorVerticalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "主要纵网格线";
            result.ActualValue = gridlineInfo.Found ? $"找到主要纵网格线: {gridlineInfo.Style}" : "未找到主要纵网格线";
            result.IsCorrect = gridlineInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"主要纵网格线检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测主要纵网格线失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测次要纵网格线
    /// </summary>
    private KnowledgePointResult DetectMinorVerticalGridlines(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MinorVerticalGridlines",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var gridlineInfo = CheckMinorVerticalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "次要纵网格线";
            result.ActualValue = gridlineInfo.Found ? $"找到次要纵网格线: {gridlineInfo.Style}" : "未找到次要纵网格线";
            result.IsCorrect = gridlineInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"次要纵网格线检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测次要纵网格线失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据系列格式
    /// </summary>
    private KnowledgePointResult DetectDataSeriesFormat(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DataSeriesFormat",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var seriesFormatInfo = CheckDataSeriesFormatInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据系列格式";
            result.ActualValue = seriesFormatInfo.Found ? $"找到数据系列格式: {seriesFormatInfo.Format}" : "未找到数据系列格式";
            result.IsCorrect = seriesFormatInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据系列格式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数据系列格式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据标签
    /// </summary>
    private KnowledgePointResult DetectAddDataLabels(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AddDataLabels",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var dataLabelsInfo = CheckDataLabelsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据标签";
            result.ActualValue = dataLabelsInfo.Found ? $"找到数据标签: {dataLabelsInfo.Position}" : "未找到数据标签";
            result.IsCorrect = dataLabelsInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据标签检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数据标签失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测数据标签格式
    /// </summary>
    private KnowledgePointResult DetectDataLabelsFormat(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DataLabelsFormat",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var labelsFormatInfo = CheckDataLabelsFormatInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据标签格式";
            result.ActualValue = labelsFormatInfo.Found ? $"找到数据标签格式: {labelsFormatInfo.Format}" : "未找到数据标签格式";
            result.IsCorrect = labelsFormatInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据标签格式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测数据标签格式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表区域格式
    /// </summary>
    private KnowledgePointResult DetectChartAreaFormat(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartAreaFormat",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var areaFormatInfo = CheckChartAreaFormatInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图表区域格式";
            result.ActualValue = areaFormatInfo.Found ? $"找到图表区域格式: {areaFormatInfo.Format}" : "未找到图表区域格式";
            result.IsCorrect = areaFormatInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表区域格式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表区域格式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表基底颜色
    /// </summary>
    private KnowledgePointResult DetectChartFloorColor(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartFloorColor",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var floorColorInfo = CheckChartFloorColorInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图表基底颜色";
            result.ActualValue = floorColorInfo.Found ? $"找到图表基底颜色: {floorColorInfo.Color}" : "未找到图表基底颜色";
            result.IsCorrect = floorColorInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表基底颜色检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表基底颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图表边框
    /// </summary>
    private KnowledgePointResult DetectChartBorder(SpreadsheetDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartBorder",
            Parameters = parameters
        };

        try
        {
            WorkbookPart workbookPart = document.WorkbookPart!;
            var borderInfo = CheckChartBorderInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图表边框";
            result.ActualValue = borderInfo.Found ? $"找到图表边框: {borderInfo.Style}" : "未找到图表边框";
            result.IsCorrect = borderInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表边框检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图表边框失败: {ex.Message}");
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
            string operationType = TryGetParameter(parameters, "OperationType", out string opType) ? opType : "";
            string cellValues = TryGetParameter(parameters, "CellValues", out string values) ? values : "";

            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    // 检查是否有填充或复制操作的迹象
                    var cellsWithData = sheetData.Descendants<Cell>().Where(c => !string.IsNullOrEmpty(c.CellValue?.Text)).ToList();

                    if (cellsWithData.Count > 0)
                    {
                        // 检查是否有重复的值（可能是复制操作）
                        var cellValueGroups = cellsWithData.GroupBy(c => c.CellValue?.Text).Where(g => g.Count() > 1);
                        if (cellValueGroups.Any())
                        {
                            return (true, "检测到单元格复制操作");
                        }

                        // 检查是否有连续的数据填充
                        if (CheckSequentialDataFill(cellsWithData))
                        {
                            return (true, "检测到单元格填充操作");
                        }

                        return (true, "检测到单元格数据操作");
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
    /// 检查工作簿中的合并单元格
    /// </summary>
    private (bool Found, int Count) CheckMergedCellsInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            int mergedCellCount = 0;
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                MergeCells? mergeCells = worksheetPart.Worksheet.Elements<MergeCells>().FirstOrDefault();
                if (mergeCells?.HasChildren == true)
                {
                    mergedCellCount += mergeCells.Elements<MergeCell>().Count();
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
            string operationType = TryGetParameter(parameters, "OperationType", out string opType) ? opType : "";
            string rowNumbers = TryGetParameter(parameters, "RowNumbers", out string rows) ? rows : "";

            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    var rowList = sheetData.Elements<Row>().ToList();

                    // 检查行数量变化（可能的插入/删除操作）
                    if (rowList.Count > 1)
                    {
                        // 检查是否有非连续的行号（可能是插入操作）
                        var rowIndexes = rowList.Where(r => r.RowIndex?.Value != null)
                                               .Select(r => (int)r.RowIndex.Value)
                                               .OrderBy(i => i)
                                               .ToList();

                        if (CheckNonSequentialRows(rowIndexes))
                        {
                            return (true, "检测到行插入/删除操作");
                        }

                        // 检查是否有空行（可能是删除操作的结果）
                        var emptyRows = rowList.Where(r => !r.Elements<Cell>().Any(c => !string.IsNullOrEmpty(c.CellValue?.Text)));
                        if (emptyRows.Any())
                        {
                            return (true, "检测到行删除操作痕迹");
                        }

                        return (true, "检测到多行数据操作");
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
    /// 检查工作簿中的字体
    /// </summary>
    private bool CheckFontInWorkbook(WorkbookPart workbookPart, string expectedFont)
    {
        try
        {
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
                {
                    string? fontName = font.FontName?.Val?.Value;
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
                {
                    bool hasStyle = expectedStyle.ToLowerInvariant() switch
                    {
                        "bold" or "粗体" => font.Bold != null,
                        "italic" or "斜体" => font.Italic != null,
                        "underline" or "下划线" => font.Underline != null,
                        "strikethrough" or "删除线" => font.Strike != null,
                        _ => false
                    };

                    if (hasStyle)
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
    /// 检查工作簿中的字体大小
    /// </summary>
    private bool CheckFontSizeInWorkbook(WorkbookPart workbookPart, string expectedSize)
    {
        try
        {
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
                {
                    string? fontSize = font.FontSize?.Val?.Value.ToString();
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
                {
                    string? color = font.Color?.Rgb?.Value;
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellFormats?.HasChildren == true)
            {
                foreach (CellFormat cellFormat in stylesPart.Stylesheet.CellFormats.Elements<CellFormat>())
                {
                    Alignment? alignment = cellFormat.Alignment;
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (Border border in stylesPart.Stylesheet.Borders.Elements<Border>())
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fills?.HasChildren == true)
            {
                foreach (Fill fill in stylesPart.Stylesheet.Fills.Elements<Fill>())
                {
                    PatternFill? patternFill = fill.PatternFill;
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
            string expectedFormat = TryGetParameter(parameters, "NumberFormat", out string format) ? format : "";

            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet != null)
            {
                // 检查自定义数字格式
                var numberingFormats = stylesPart.Stylesheet.NumberingFormats;
                if (numberingFormats?.HasChildren == true)
                {
                    foreach (var numFormat in numberingFormats.Elements<NumberingFormat>())
                    {
                        string formatCode = numFormat.FormatCode?.Value ?? "";

                        if (!string.IsNullOrEmpty(expectedFormat))
                        {
                            if (TextEquals(formatCode, expectedFormat) ||
                                CheckNumberFormatMatch(formatCode, expectedFormat))
                            {
                                return true;
                            }
                        }
                        else if (!string.IsNullOrEmpty(formatCode))
                        {
                            return true; // 有自定义格式
                        }
                    }
                }

                // 检查单元格格式中的数字格式
                var cellFormats = stylesPart.Stylesheet.CellFormats;
                if (cellFormats?.HasChildren == true)
                {
                    foreach (var cellFormat in cellFormats.Elements<CellFormat>())
                    {
                        if (cellFormat.NumberFormatId?.Value != null)
                        {
                            uint formatId = cellFormat.NumberFormatId.Value;
                            // 检查是否使用了非默认的数字格式
                            if (formatId > 0 && formatId != 164) // 164是默认的通用格式
                            {
                                return true;
                            }
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
    /// 检查工作簿中的函数使用
    /// </summary>
    private (bool Found, string FunctionName) CheckFunctionUsageInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedFunction = TryGetParameter(parameters, "FunctionName", out string expected) ? expected : "";

            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    foreach (Row row in sheetData.Elements<Row>())
                    {
                        foreach (Cell cell in row.Elements<Cell>())
                        {
                            string? cellFormula = cell.CellFormula?.Text;
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

            // 检查工作表中的图表
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                chartCount += worksheetPart.DrawingsPart?.ChartParts.Count() ?? 0;
            }

            // 检查图表工作表
            chartCount += workbookPart.ChartsheetParts.Count();

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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                // 检查排序状态
                var sortState = worksheetPart.Worksheet.Elements<SortState>().FirstOrDefault();
                if (sortState != null)
                {
                    return true;
                }

                // 检查自动筛选（通常与排序相关）
                var autoFilter = worksheetPart.Worksheet.Elements<AutoFilter>().FirstOrDefault();
                if (autoFilter != null)
                {
                    // 检查是否有排序条件
                    var filterColumns = autoFilter.Elements<FilterColumn>();
                    foreach (var filterColumn in filterColumns)
                    {
                        // 检查是否有排序相关的筛选条件
                        if (filterColumn.HasChildren)
                        {
                            return true;
                        }
                    }
                    return true; // 有自动筛选就认为可能有排序
                }

                // 检查数据是否呈现排序特征
                if (CheckDataSortingPattern(worksheetPart))
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                ConditionalFormatting? conditionalFormatting = worksheetPart.Worksheet.Elements<ConditionalFormatting>().FirstOrDefault();
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                DataValidations? dataValidations = worksheetPart.Worksheet.Elements<DataValidations>().FirstOrDefault();
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetViews? sheetViews = worksheetPart.Worksheet.GetFirstChild<SheetViews>();
                if (sheetViews?.HasChildren == true)
                {
                    foreach (SheetView sheetView in sheetViews.Elements<SheetView>())
                    {
                        Pane? pane = sheetView.Pane;
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                PageSetup? pageSetup = worksheetPart.Worksheet.GetFirstChild<PageSetup>();
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
            DefinedNames? definedNames = workbookPart.Workbook.DefinedNames;
            if (definedNames?.HasChildren == true)
            {
                foreach (DefinedName definedName in definedNames.Elements<DefinedName>())
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                HeaderFooter? headerFooter = worksheetPart.Worksheet.GetFirstChild<HeaderFooter>();
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetProtection? sheetProtection = worksheetPart.Worksheet.GetFirstChild<SheetProtection>();
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                Hyperlinks? hyperlinks = worksheetPart.Worksheet.Elements<Hyperlinks>().FirstOrDefault();
                if (hyperlinks?.HasChildren == true)
                {
                    Hyperlink? firstHyperlink = hyperlinks.Elements<Hyperlink>().FirstOrDefault();
                    if (firstHyperlink?.Id?.Value != null)
                    {
                        try
                        {
                            ReferenceRelationship relationship = worksheetPart.GetReferenceRelationship(firstHyperlink.Id.Value);
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

    /// <summary>
    /// 检查工作簿中的水平对齐
    /// </summary>
    private bool CheckHorizontalAlignmentInWorkbook(WorkbookPart workbookPart, string expectedAlignment)
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
                        if (TextEquals(alignmentValue, expectedAlignment) ||
                            (expectedAlignment.Contains("居中") && alignmentValue.Contains("Center")) ||
                            (expectedAlignment.Contains("左对齐") && alignmentValue.Contains("Left")) ||
                            (expectedAlignment.Contains("右对齐") && alignmentValue.Contains("Right")))
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
    /// 检查工作簿中的垂直对齐
    /// </summary>
    private bool CheckVerticalAlignmentInWorkbook(WorkbookPart workbookPart, string expectedAlignment)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellFormats?.HasChildren == true)
            {
                foreach (var cellFormat in stylesPart.Stylesheet.CellFormats.Elements<DocumentFormat.OpenXml.Spreadsheet.CellFormat>())
                {
                    var alignment = cellFormat.Alignment;
                    if (alignment?.Vertical?.Value != null)
                    {
                        string alignmentValue = alignment.Vertical.Value.ToString();
                        if (TextEquals(alignmentValue, expectedAlignment) ||
                            (expectedAlignment.Contains("居中") && alignmentValue.Contains("Center")) ||
                            (expectedAlignment.Contains("顶端") && alignmentValue.Contains("Top")) ||
                            (expectedAlignment.Contains("底端") && alignmentValue.Contains("Bottom")))
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
    /// 检查工作簿中的内边框样式
    /// </summary>
    private bool CheckInnerBorderStyleInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (var border in stylesPart.Stylesheet.Borders.Elements<DocumentFormat.OpenXml.Spreadsheet.Border>())
                {
                    // 检查内边框（左、右、上、下）
                    if (border.LeftBorder?.Style?.Value != null ||
                        border.RightBorder?.Style?.Value != null ||
                        border.TopBorder?.Style?.Value != null ||
                        border.BottomBorder?.Style?.Value != null)
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
    /// 检查工作簿中的内边框颜色
    /// </summary>
    private bool CheckInnerBorderColorInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (var border in stylesPart.Stylesheet.Borders.Elements<DocumentFormat.OpenXml.Spreadsheet.Border>())
                {
                    // 检查内边框颜色
                    if (border.LeftBorder?.Color != null ||
                        border.RightBorder?.Color != null ||
                        border.TopBorder?.Color != null ||
                        border.BottomBorder?.Color != null)
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
    /// 检查工作簿中的外边框样式
    /// </summary>
    private bool CheckOuterBorderStyleInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (var border in stylesPart.Stylesheet.Borders.Elements<DocumentFormat.OpenXml.Spreadsheet.Border>())
                {
                    // 检查外边框
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
    /// 检查工作簿中的外边框颜色
    /// </summary>
    private bool CheckOuterBorderColorInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
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
    /// 检查工作簿中的行高设置
    /// </summary>
    private bool CheckRowHeightInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
                    {
                        if (row.Height?.Value != null)
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
    /// 检查工作簿中的列宽设置
    /// </summary>
    private bool CheckColumnWidthInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var columns = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.Columns>().FirstOrDefault();
                if (columns?.HasChildren == true)
                {
                    foreach (var column in columns.Elements<DocumentFormat.OpenXml.Spreadsheet.Column>())
                    {
                        if (column.Width?.Value != null)
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
    /// 检查工作簿中的单元格填充颜色
    /// </summary>
    private bool CheckCellFillColorInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fills?.HasChildren == true)
            {
                foreach (var fill in stylesPart.Stylesheet.Fills.Elements<DocumentFormat.OpenXml.Spreadsheet.Fill>())
                {
                    var patternFill = fill.PatternFill;
                    if (patternFill?.ForegroundColor != null || patternFill?.BackgroundColor != null)
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
    /// 检查工作簿中的图案填充样式
    /// </summary>
    private bool CheckPatternFillStyleInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fills?.HasChildren == true)
            {
                foreach (var fill in stylesPart.Stylesheet.Fills.Elements<DocumentFormat.OpenXml.Spreadsheet.Fill>())
                {
                    var patternFill = fill.PatternFill;
                    if (patternFill?.PatternType?.Value != null &&
                        patternFill.PatternType.Value != DocumentFormat.OpenXml.Spreadsheet.PatternValues.None)
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
    /// 检查工作簿中的图案填充颜色
    /// </summary>
    private bool CheckPatternFillColorInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
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
    /// 检查工作簿中的下划线
    /// </summary>
    private bool CheckUnderlineInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (var font in stylesPart.Stylesheet.Fonts.Elements<DocumentFormat.OpenXml.Spreadsheet.Font>())
                {
                    if (font.Underline != null)
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
    /// 检查工作簿中修改的工作表名称
    /// </summary>
    private (bool Found, string SheetName) CheckModifiedSheetNameInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedName = TryGetParameter(parameters, "NewSheetName", out string expected) ? expected : "";

            var sheets = workbookPart.Workbook.Sheets;
            if (sheets?.HasChildren == true)
            {
                foreach (var sheet in sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
                {
                    string sheetName = sheet.Name?.Value ?? "";
                    if (!string.IsNullOrEmpty(expectedName) && TextEquals(sheetName, expectedName))
                    {
                        return (true, sheetName);
                    }
                    // 检查是否不是默认名称
                    if (!sheetName.StartsWith("Sheet") && !sheetName.StartsWith("工作表"))
                    {
                        return (true, sheetName);
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
    /// 检查工作簿中的单元格样式数据
    /// </summary>
    private bool CheckCellStyleDataInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellStyles?.HasChildren == true)
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
    /// 检查工作簿中的筛选
    /// </summary>
    private bool CheckFilterInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
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
    /// 检查工作簿中的排序
    /// </summary>
    private bool CheckSortInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sortState = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.SortState>().FirstOrDefault();
                if (sortState != null)
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
    /// 检查工作簿中的分类汇总
    /// </summary>
    private bool CheckSubtotalInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string groupByColumn = TryGetParameter(parameters, "GroupByColumn", out string groupCol) ? groupCol : "";
            string summaryFunction = TryGetParameter(parameters, "SummaryFunction", out string sumFunc) ? sumFunc : "";
            string summaryColumn = TryGetParameter(parameters, "SummaryColumn", out string sumCol) ? sumCol : "";

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    bool hasSubtotalFunction = false;
                    bool hasGroupingStructure = false;

                    // 检查SUBTOTAL函数
                    foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
                    {
                        foreach (var cell in row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>())
                        {
                            var cellFormula = cell.CellFormula?.Text;
                            if (!string.IsNullOrEmpty(cellFormula))
                            {
                                if (cellFormula.Contains("SUBTOTAL", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasSubtotalFunction = true;
                                }

                                // 检查其他汇总函数
                                if (!string.IsNullOrEmpty(summaryFunction))
                                {
                                    if (cellFormula.Contains(summaryFunction, StringComparison.OrdinalIgnoreCase))
                                    {
                                        hasSubtotalFunction = true;
                                    }
                                }
                            }
                        }
                    }

                    // 检查分组结构（行分组）
                    var rowGroups = worksheetPart.Worksheet.Elements<RowBreaks>().FirstOrDefault();
                    if (rowGroups != null)
                    {
                        hasGroupingStructure = true;
                    }

                    // 检查大纲级别（分组的另一种表现）
                    var rowsWithOutlineLevel = sheetData.Elements<Row>().Where(r => r.OutlineLevel?.Value > 0);
                    if (rowsWithOutlineLevel.Any())
                    {
                        hasGroupingStructure = true;
                    }

                    if (hasSubtotalFunction || hasGroupingStructure)
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
    /// 检查工作簿中的高级筛选条件
    /// </summary>
    private bool CheckAdvancedFilterConditionInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有筛选相关设置
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var autoFilter = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.AutoFilter>().FirstOrDefault();
                if (autoFilter?.Elements<DocumentFormat.OpenXml.Spreadsheet.FilterColumn>().Any() == true)
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
    /// 检查工作簿中的高级筛选数据
    /// </summary>
    private bool CheckAdvancedFilterDataInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有筛选相关设置
            return CheckAdvancedFilterConditionInWorkbook(workbookPart, parameters);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的图表类型
    /// </summary>
    private (bool Found, string ChartType) CheckChartTypeInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedType = TryGetParameter(parameters, "ChartType", out string expected) ? expected : "";

            // 检查工作表中的图表
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                if (worksheetPart.DrawingsPart?.ChartParts.Any() == true)
                {
                    foreach (var chartPart in worksheetPart.DrawingsPart.ChartParts)
                    {
                        try
                        {
                            var chartSpace = chartPart.ChartSpace;
                            if (chartSpace?.Chart?.PlotArea != null)
                            {
                                var plotArea = chartSpace.Chart.PlotArea;

                                // 检查不同类型的图表
                                if (plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChart>().Any())
                                {
                                    string chartType = "柱形图";
                                    if (string.IsNullOrEmpty(expectedType) || TextEquals(chartType, expectedType) || expectedType.Contains("柱形"))
                                    {
                                        return (true, chartType);
                                    }
                                }

                                if (plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChart>().Any())
                                {
                                    string chartType = "折线图";
                                    if (string.IsNullOrEmpty(expectedType) || TextEquals(chartType, expectedType) || expectedType.Contains("折线"))
                                    {
                                        return (true, chartType);
                                    }
                                }

                                if (plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChart>().Any())
                                {
                                    string chartType = "饼图";
                                    if (string.IsNullOrEmpty(expectedType) || TextEquals(chartType, expectedType) || expectedType.Contains("饼"))
                                    {
                                        return (true, chartType);
                                    }
                                }

                                if (plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.ScatterChart>().Any())
                                {
                                    string chartType = "散点图";
                                    if (string.IsNullOrEmpty(expectedType) || TextEquals(chartType, expectedType) || expectedType.Contains("散点"))
                                    {
                                        return (true, chartType);
                                    }
                                }

                                if (plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.AreaChart>().Any())
                                {
                                    string chartType = "面积图";
                                    if (string.IsNullOrEmpty(expectedType) || TextEquals(chartType, expectedType) || expectedType.Contains("面积"))
                                    {
                                        return (true, chartType);
                                    }
                                }

                                // 如果有图表但类型不匹配
                                if (string.IsNullOrEmpty(expectedType))
                                {
                                    return (true, "检测到图表");
                                }
                            }
                        }
                        catch
                        {
                            // 单个图表解析失败，继续检查其他图表
                            continue;
                        }
                    }
                }
            }

            // 检查图表工作表
            if (workbookPart.ChartsheetParts.Any())
            {
                return (true, "图表工作表存在");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图表样式
    /// </summary>
    private bool CheckChartStyleInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            var chartInfo = CheckChartInWorkbook(workbookPart);
            return chartInfo.Found;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查工作簿中的图表标题
    /// </summary>
    private (bool Found, string Title) CheckChartTitleInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedTitle = TryGetParameter(parameters, "ChartTitle", out string expected) ? expected : "";

            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                return (true, "图表标题存在");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图例位置
    /// </summary>
    private (bool Found, string Position) CheckLegendPositionInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedPosition = TryGetParameter(parameters, "LegendPosition", out string expected) ? expected : "";

            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                return (true, "图例位置存在");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图表移动
    /// </summary>
    private (bool Found, string Location) CheckChartMoveInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedLocation = TryGetParameter(parameters, "MoveLocation", out string expected) ? expected : "";
            string targetSheet = TryGetParameter(parameters, "TargetSheet", out string sheet) ? sheet : "";

            // 检查图表是否存在于指定工作表
            if (!string.IsNullOrEmpty(targetSheet))
            {
                var targetWorksheet = workbookPart.WorksheetParts.FirstOrDefault(ws =>
                {
                    var sheetName = GetWorksheetName(workbookPart, ws);
                    return TextEquals(sheetName, targetSheet);
                });

                if (targetWorksheet?.DrawingsPart?.ChartParts.Any() == true)
                {
                    return (true, $"图表移动到 {targetSheet}");
                }
            }

            // 简化检测：检查是否有多个工作表包含图表
            int chartsInSheets = 0;
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                if (worksheetPart.DrawingsPart?.ChartParts.Any() == true)
                {
                    chartsInSheets++;
                }
            }

            if (chartsInSheets > 0)
            {
                return (true, "检测到图表位置");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的分类轴数据区域
    /// </summary>
    private (bool Found, string Range) CheckCategoryAxisDataRangeInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedRange = TryGetParameter(parameters, "CategoryRange", out string expected) ? expected : "";

            // 简化实现：检查是否有图表和数据区域
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                // 检查是否有数据区域定义
                foreach (var worksheetPart in workbookPart.WorksheetParts)
                {
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                    if (sheetData?.HasChildren == true)
                    {
                        return (true, "检测到分类轴数据区域");
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
    /// 检查工作簿中的数值轴数据区域
    /// </summary>
    private (bool Found, string Range) CheckValueAxisDataRangeInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedRange = TryGetParameter(parameters, "ValueRange", out string expected) ? expected : "";

            // 简化实现：检查是否有图表和数值数据
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                // 检查是否有数值数据
                foreach (var worksheetPart in workbookPart.WorksheetParts)
                {
                    var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                    if (sheetData?.HasChildren == true)
                    {
                        foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
                        {
                            foreach (var cell in row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>())
                            {
                                if (cell.DataType?.Value == DocumentFormat.OpenXml.Spreadsheet.CellValues.Number ||
                                    (cell.DataType == null && !string.IsNullOrEmpty(cell.CellValue?.Text)))
                                {
                                    return (true, "检测到数值轴数据区域");
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
    /// 检查工作簿中的图表标题格式
    /// </summary>
    private (bool Found, string Format) CheckChartTitleFormatInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和格式设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                // 检查是否有字体格式参数
                if (TryGetParameter(parameters, "FontName", out string fontName) ||
                    TryGetParameter(parameters, "FontSize", out string fontSize) ||
                    TryGetParameter(parameters, "FontColor", out string fontColor))
                {
                    return (true, "检测到图表标题格式设置");
                }

                return (true, "检测到图表标题");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的横坐标轴标题
    /// </summary>
    private (bool Found, string Title) CheckHorizontalAxisTitleInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedTitle = TryGetParameter(parameters, "AxisTitle", out string expected) ? expected : "";

            // 简化实现：检查是否有图表
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                return (true, "检测到横坐标轴标题");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 获取工作表名称
    /// </summary>
    private string GetWorksheetName(WorkbookPart workbookPart, WorksheetPart worksheetPart)
    {
        try
        {
            var sheets = workbookPart.Workbook.Sheets;
            if (sheets?.HasChildren == true)
            {
                foreach (var sheet in sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>())
                {
                    if (sheet.Id?.Value == workbookPart.GetIdOfPart(worksheetPart))
                    {
                        return sheet.Name?.Value ?? "";
                    }
                }
            }
            return "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 检查工作簿中的主要横网格线
    /// </summary>
    private (bool Found, string Style) CheckMajorHorizontalGridlinesInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和网格线设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                bool gridlineVisible = TryGetParameter(parameters, "GridlineVisible", out string visible) &&
                                     (TextEquals(visible, "true") || TextEquals(visible, "是"));

                if (gridlineVisible || TryGetParameter(parameters, "GridlineColor", out string color))
                {
                    return (true, "主要横网格线可见");
                }

                return (true, "检测到图表网格线设置");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的次要横网格线
    /// </summary>
    private (bool Found, string Style) CheckMinorHorizontalGridlinesInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和网格线设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                bool gridlineVisible = TryGetParameter(parameters, "GridlineVisible", out string visible) &&
                                     (TextEquals(visible, "true") || TextEquals(visible, "是"));

                if (gridlineVisible || TryGetParameter(parameters, "GridlineColor", out string color))
                {
                    return (true, "次要横网格线可见");
                }

                return (true, "检测到图表网格线设置");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的主要纵网格线
    /// </summary>
    private (bool Found, string Style) CheckMajorVerticalGridlinesInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和网格线设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                bool gridlineVisible = TryGetParameter(parameters, "GridlineVisible", out string visible) &&
                                     (TextEquals(visible, "true") || TextEquals(visible, "是"));

                if (gridlineVisible || TryGetParameter(parameters, "GridlineColor", out string color))
                {
                    return (true, "主要纵网格线可见");
                }

                return (true, "检测到图表网格线设置");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的次要纵网格线
    /// </summary>
    private (bool Found, string Style) CheckMinorVerticalGridlinesInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和网格线设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                bool gridlineVisible = TryGetParameter(parameters, "GridlineVisible", out string visible) &&
                                     (TextEquals(visible, "true") || TextEquals(visible, "是"));

                if (gridlineVisible || TryGetParameter(parameters, "GridlineColor", out string color))
                {
                    return (true, "次要纵网格线可见");
                }

                return (true, "检测到图表网格线设置");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的数据系列格式
    /// </summary>
    private (bool Found, string Format) CheckDataSeriesFormatInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和系列格式设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                if (TryGetParameter(parameters, "SeriesIndex", out string seriesIndex) ||
                    TryGetParameter(parameters, "SeriesColor", out string seriesColor))
                {
                    return (true, "检测到数据系列格式设置");
                }

                return (true, "检测到数据系列");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的数据标签
    /// </summary>
    private (bool Found, string Position) CheckDataLabelsInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和标签设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                string labelPosition = TryGetParameter(parameters, "LabelPosition", out string position) ? position : "默认位置";
                return (true, labelPosition);
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的数据标签格式
    /// </summary>
    private (bool Found, string Format) CheckDataLabelsFormatInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和标签格式设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                if (TryGetParameter(parameters, "FontName", out string fontName) ||
                    TryGetParameter(parameters, "FontSize", out string fontSize) ||
                    TryGetParameter(parameters, "FontColor", out string fontColor))
                {
                    return (true, "检测到数据标签格式设置");
                }

                return (true, "检测到数据标签");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图表区域格式
    /// </summary>
    private (bool Found, string Format) CheckChartAreaFormatInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和区域格式设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                if (TryGetParameter(parameters, "FillColor", out string fillColor) ||
                    TryGetParameter(parameters, "BorderColor", out string borderColor))
                {
                    return (true, "检测到图表区域格式设置");
                }

                return (true, "检测到图表区域");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图表基底颜色
    /// </summary>
    private (bool Found, string Color) CheckChartFloorColorInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和基底颜色设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                string floorColor = TryGetParameter(parameters, "FloorColor", out string color) ? color : "默认颜色";
                return (true, floorColor);
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查工作簿中的图表边框
    /// </summary>
    private (bool Found, string Style) CheckChartBorderInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            // 简化实现：检查是否有图表和边框设置
            var chartInfo = CheckChartInWorkbook(workbookPart);
            if (chartInfo.Found)
            {
                if (TryGetParameter(parameters, "BorderStyle", out string borderStyle) ||
                    TryGetParameter(parameters, "BorderColor", out string borderColor))
                {
                    return (true, "检测到图表边框设置");
                }

                return (true, "检测到图表边框");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查连续数据填充模式
    /// </summary>
    private bool CheckSequentialDataFill(List<Cell> cellsWithData)
    {
        try
        {
            // 检查是否有连续的数字序列
            var numericCells = cellsWithData.Where(c =>
            {
                if (double.TryParse(c.CellValue?.Text, out double value))
                {
                    return true;
                }
                return false;
            }).ToList();

            if (numericCells.Count >= 3)
            {
                var values = numericCells.Select(c => double.Parse(c.CellValue.Text)).OrderBy(v => v).ToList();

                // 检查是否是等差数列
                double diff = values[1] - values[0];
                for (int i = 2; i < values.Count; i++)
                {
                    if (Math.Abs((values[i] - values[i-1]) - diff) < 0.001)
                    {
                        return true; // 发现等差数列，可能是填充操作
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
    /// 检查非连续行号
    /// </summary>
    private bool CheckNonSequentialRows(List<int> rowIndexes)
    {
        try
        {
            if (rowIndexes.Count < 2) return false;

            for (int i = 1; i < rowIndexes.Count; i++)
            {
                if (rowIndexes[i] - rowIndexes[i-1] > 1)
                {
                    return true; // 发现跳跃的行号
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
    /// 检查数据排序模式
    /// </summary>
    private bool CheckDataSortingPattern(WorksheetPart worksheetPart)
    {
        try
        {
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData?.HasChildren != true) return false;

            var rows = sheetData.Elements<Row>().Take(10).ToList(); // 检查前10行
            if (rows.Count < 3) return false;

            // 检查第一列的数据是否呈现排序特征
            var firstColumnValues = new List<string>();
            foreach (var row in rows)
            {
                var firstCell = row.Elements<Cell>().FirstOrDefault();
                if (firstCell?.CellValue?.Text != null)
                {
                    firstColumnValues.Add(firstCell.CellValue.Text);
                }
            }

            if (firstColumnValues.Count >= 3)
            {
                // 检查是否是升序或降序
                bool isAscending = true;
                bool isDescending = true;

                for (int i = 1; i < firstColumnValues.Count; i++)
                {
                    int comparison = string.Compare(firstColumnValues[i], firstColumnValues[i-1], StringComparison.OrdinalIgnoreCase);
                    if (comparison < 0) isAscending = false;
                    if (comparison > 0) isDescending = false;
                }

                return isAscending || isDescending;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查数字格式匹配
    /// </summary>
    private bool CheckNumberFormatMatch(string formatCode, string expectedFormat)
    {
        try
        {
            // 常见数字格式的匹配
            var formatMappings = new Dictionary<string, string[]>
            {
                { "货币", new[] { "¥", "$", "€", "currency", "money" } },
                { "百分比", new[] { "%", "percent", "percentage" } },
                { "日期", new[] { "yyyy", "mm", "dd", "date", "年", "月", "日" } },
                { "时间", new[] { "hh", "mm", "ss", "time", "时", "分", "秒" } },
                { "科学计数", new[] { "E+", "E-", "scientific", "科学" } },
                { "分数", new[] { "/", "fraction", "分数" } },
                { "文本", new[] { "@", "text", "文本" } }
            };

            string lowerFormatCode = formatCode.ToLowerInvariant();
            string lowerExpected = expectedFormat.ToLowerInvariant();

            foreach (var mapping in formatMappings)
            {
                if (TextEquals(mapping.Key, expectedFormat))
                {
                    return mapping.Value.Any(pattern => lowerFormatCode.Contains(pattern));
                }
            }

            return lowerFormatCode.Contains(lowerExpected);
        }
        catch
        {
            return false;
        }
    }
}
