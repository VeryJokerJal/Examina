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
                result.Details = $"无效的Excel文档: {filePath}";
                return result;
            }

            // 获取Excel模块
            ExamModuleModel? excelModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Excel);
            if (excelModule == null)
            {
                result.Details = "试卷中未找到Excel模块，跳过Excel评分";
                result.IsSuccess = true;
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
                return result;
            }

            // 收集所有Excel相关的操作点并记录题目关联关系
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in excelModule.Questions)
            {
                // 只处理Excel相关且启用的操作点
                List<OperationPointModel> excelOperationPoints = question.OperationPoints.Where(op => op.ModuleType == ModuleType.Excel && op.IsEnabled).ToList();

                System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 题目 '{question.Title}' (ID: {question.Id}) 包含 {excelOperationPoints.Count} 个Excel操作点");

                foreach (OperationPointModel operationPoint in excelOperationPoints)
                {
                    allOperationPoints.Add(operationPoint);
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                    System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 添加操作点: {operationPoint.Name} (ID: {operationPoint.Id}) -> 题目: {question.Id}");
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.Details = "Excel模块中未找到启用的Excel操作点";
                System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 警告: Excel模块包含 {excelModule.Questions.Count} 个题目，但没有找到启用的Excel操作点");
                return result;
            }

            System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 总共收集到 {allOperationPoints.Count} 个Excel操作点，来自 {excelModule.Questions.Count} 个题目");

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, allOperationPoints).Result;

            // 为每个知识点结果设置题目关联信息
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                if (operationPointToQuestionMap.TryGetValue(kpResult.KnowledgePointId, out string? questionId))
                {
                    kpResult.QuestionId = questionId;

                    // 查找题目标题用于调试信息（KnowledgePointResult模型中没有QuestionTitle属性）
                    QuestionModel? question = excelModule.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (question != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 知识点 '{kpResult.KnowledgePointName}' 关联到题目 '{question.Title}' (ID: {questionId})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 警告: 无法找到ID为 {questionId} 的题目");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 警告: 知识点 '{kpResult.KnowledgePointName}' (ID: {kpResult.KnowledgePointId}) 没有找到对应的题目映射");
                }
            }

            FinalizeScoringResult(result, allOperationPoints);

            System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 评分完成: 总分 {result.TotalScore}, 获得分数 {result.AchievedScore}, 成功率 {(result.TotalScore > 0 ? (result.AchievedScore / result.TotalScore * 100):0):F1}%");
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
                result.Details = $"无效的Excel文档: {filePath}";
                return result;
            }

            // 获取题目的操作点（只处理Excel相关的操作点）
            List<OperationPointModel> excelOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.Excel && op.IsEnabled)];

            if (excelOperationPoints.Count == 0)
            {
                result.Details = "题目没有包含任何Excel操作点";
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
                    result.Details = "无效的Excel文档";
                    return result;
                }

                using SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false);
                result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.Details = $"检测知识点时发生错误: {ex.Message}";
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
            // Excel基础操作 - 中文操作点名称映射
            "填充或复制单元格内容" => "FillOrCopyCellContent",
            "合并单元格" => "MergeCells",
            "设置指定单元格字体" => "SetCellFont",
            "设置单元格区域水平对齐方式" => "SetHorizontalAlignment",
            "内边框样式" => "SetInnerBorderStyle",
            "内边框颜色" => "SetInnerBorderColor",
            "使用函数" => "UseFunction",
            "设置行高" => "SetRowHeight",
            "设置列宽" => "SetColumnWidth",
            "设置单元格填充颜色" => "SetCellFillColor",
            "设置垂直对齐方式" => "SetVerticalAlignment",
            "修改sheet表名称" => "ModifySheetName",
            "设置字型" => "SetFontStyle",
            "设置字号" => "SetFontSize",
            "字体颜色" => "SetFontColor",
            "设置目标区域单元格数字分类格式" => "SetNumberFormat",
            "设置图案填充样式" => "SetPatternFillStyle",
            "设置填充图案颜色" => "SetPatternFillColor",
            "设置外边框样式" => "SetOuterBorderStyle",
            "设置外边框颜色" => "SetOuterBorderColor",
            "添加下划线" => "AddUnderline",
            "条件格式" => "ConditionalFormat",
            "设置单元格样式——数据" => "SetCellStyleData",

            // 数据清单操作
            "筛选" => "Filter",
            "排序" => "Sort",
            "数据透视表" => "PivotTable",
            "分类汇总" => "Subtotal",
            "高级筛选-条件" => "AdvancedFilterCondition",
            "高级筛选-数据" => "AdvancedFilterData",

            // 图表操作
            "图表类型" => "ChartType",
            "图表样式" => "ChartStyle",
            "图表标题" => "ChartTitle",
            "设置图例位置" => "LegendPosition",
            "图表移动" => "ChartMove",
            "分类轴数据区域" => "CategoryAxisDataRange",
            "数值轴数据区域" => "ValueAxisDataRange",
            "图表标题格式" => "ChartTitleFormat",
            "主要横坐标轴标题" => "HorizontalAxisTitle",
            "主要横坐标轴标题格式" => "HorizontalAxisTitleFormat",
            "设置图例格式" => "LegendFormat",
            "设置主要纵坐标轴选项" => "VerticalAxisOptions",
            "设置网格线——主要横网格线" => "MajorHorizontalGridlines",
            "主要纵网格线" => "MajorVerticalGridlines",
            "设置网格线——次要横网格线" => "MinorHorizontalGridlines",
            "次要纵网格线" => "MinorVerticalGridlines",
            "设置数据系列格式" => "DataSeriesFormat",
            "添加数据标签" => "AddDataLabels",
            "设置数据标签格式" => "DataLabelsFormat",
            "设置图表区域格式" => "ChartAreaFormat",
            "显示图表基底颜色" => "ChartFloorColor",
            "设置图表边框线" => "ChartBorder",

            _ => base.MapOperationPointNameToKnowledgeType(operationPointName)
        };
    }

    /// <summary>
    /// 验证必需参数并生成调试信息
    /// </summary>
    private bool ValidateRequiredParameters(Dictionary<string, string> parameters, string knowledgePointType, string[] requiredParams, out string errorDetails)
    {
        List<string> missingParams = [];

        foreach (string param in requiredParams)
        {
            if (!TryGetParameter(parameters, param, out string _))
            {
                missingParams.Add(param);
            }
        }

        if (missingParams.Count > 0)
        {
            string missingParamsList = string.Join("', '", missingParams);
            errorDetails = $"缺少必需参数: '{missingParamsList}'";

            // 输出详细的调试信息
            System.Diagnostics.Debug.WriteLine($"[ExcelOpenXmlScoringService] 知识点检测失败 - {knowledgePointType}: {errorDetails}");

            return false;
        }

        errorDetails = string.Empty;
        return true;
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
                    result.Details = $"不支持的知识点类型: {knowledgePointType}";
                    result.IsCorrect = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Details = $"检测知识点 {knowledgePointType} 时发生错误: {ex.Message}";
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
            (bool Found, string Details) = CheckFontInWorkbook(workbookPart, expectedFont, parameters);

            result.ExpectedValue = expectedFont;
            result.ActualValue = Found ? $"找到字体: {Details}" : "未找到指定字体";
            result.IsCorrect = Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格字体检测: 期望 {expectedFont}, {(Found ? "找到" : "未找到")}";
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
            (bool Found, string Details) = CheckFontStyleInWorkbook(workbookPart, expectedStyle, parameters);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = Found ? $"找到样式: {Details}" : "未找到指定样式";
            result.IsCorrect = Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体样式检测: 期望 {expectedStyle}, {(Found ? "找到" : "未找到")}";
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
            (bool Found, string Details) = CheckCellAlignmentInWorkbook(workbookPart, expectedAlignment, parameters);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = Found ? $"找到对齐方式: {Details}" : "未找到指定对齐方式";
            result.IsCorrect = Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格对齐检测: 期望 {expectedAlignment}, {(Found ? "找到" : "未找到")}";
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
            (bool Found, string Details) = CheckNumberFormatInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数字格式设置";
            result.ActualValue = Found ? $"找到数字格式设置: {Details}" : "未找到数字格式设置";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckDataSortInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据排序";
            result.ActualValue = Found ? $"找到数据排序: {Details}" : "未找到数据排序";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckPivotTableInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据透视表";
            result.ActualValue = Found ? $"找到数据透视表: {Details}" : "未找到数据透视表";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckConditionalFormattingInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "条件格式";
            result.ActualValue = Found ? $"找到条件格式: {Details}" : "未找到条件格式";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckDataValidationInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据验证";
            result.ActualValue = Found ? $"找到数据验证: {Details}" : "未找到数据验证";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckFreezePanesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "冻结窗格";
            result.ActualValue = Found ? $"找到冻结窗格: {Details}" : "未找到冻结窗格";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckPageSetupInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "页面设置";
            result.ActualValue = Found ? $"找到页面设置: {Details}" : "未找到页面设置";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckPrintAreaInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "打印区域";
            result.ActualValue = Found ? $"找到打印区域设置: {Details}" : "未找到打印区域设置";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckHeaderFooterInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "页眉页脚";
            result.ActualValue = Found ? $"找到页眉页脚设置: {Details}" : "未找到页眉页脚设置";
            result.IsCorrect = Found;
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
            (bool Found, string Details) = CheckWorksheetProtectionInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "工作表保护";
            result.ActualValue = Found ? $"找到工作表保护: {Details}" : "未找到工作表保护";
            result.IsCorrect = Found;
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
            // 验证必需参数
            string[] requiredParams = ["CellRange", "BorderStyle"];
            if (!ValidateRequiredParameters(parameters, "SetInnerBorderStyle", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["CellRange", "BorderColor"];
            if (!ValidateRequiredParameters(parameters, "SetInnerBorderColor", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["CellRange", "BorderStyle"];
            if (!ValidateRequiredParameters(parameters, "SetOuterBorderStyle", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            if (!TryGetIntParameter(parameters, "RowNumber", out int rowNumber) ||
                !TryGetDoubleParameter(parameters, "Height", out double expectedHeight))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: RowNumber 或 Height");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            WorksheetPart? worksheetPart = GetActiveWorksheet(workbookPart);

            if (worksheetPart == null)
            {
                SetKnowledgePointFailure(result, "无法获取活动工作表");
                return result;
            }

            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null)
            {
                SetKnowledgePointFailure(result, "工作表中没有数据");
                return result;
            }

            List<Row> rows = sheetData.Elements<Row>().Where(r => r.Height?.Value != null).ToList();

            // 创建行高比较函数
            bool HeightMatches(double? actual, double? expected) =>
                actual.HasValue && expected.HasValue && Math.Abs(actual.Value - expected.Value) < 0.1;

            bool isMatch = FindMatchingElement(
                rows,
                rowNumber,
                (double?)expectedHeight,
                row => row.Height?.Value,
                HeightMatches,
                out Row? matchedRow,
                out double? actualHeight,
                out string errorMessage,
                "行");

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedHeight.ToString("F1");
            result.ActualValue = actualHeight?.ToString("F1") ?? "0";
            result.IsCorrect = true;
            result.AchievedScore = result.TotalScore;
            result.Details = rowNumber == -1
                ? $"行高设置检测(任意匹配): 找到匹配的行高 {actualHeight:F1}"
                : $"行高设置检测(行{matchedRow?.RowIndex?.Value}): 期望 {expectedHeight:F1}, 实际 {actualHeight:F1}";
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
            // 验证必需参数
            if (!TryGetParameter(parameters, "ColumnLetter", out string columnLetter) ||
                !TryGetDoubleParameter(parameters, "Width", out double expectedWidth))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ColumnLetter 或 Width");
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            WorksheetPart? worksheetPart = GetActiveWorksheet(workbookPart);

            if (worksheetPart == null)
            {
                SetKnowledgePointFailure(result, "无法获取活动工作表");
                return result;
            }

            Columns? columnsElement = worksheetPart.Worksheet.Elements<Columns>().FirstOrDefault();
            if (columnsElement == null)
            {
                SetKnowledgePointFailure(result, "工作表中没有列宽设置");
                return result;
            }

            List<Column> columns = columnsElement.Elements<Column>().Where(c => c.Width?.Value != null).ToList();

            // 如果指定了具体列字母且不是-1模式，则按列字母匹配
            if (columnLetter != "-1")
            {
                // 将列字母转换为列索引
                uint columnIndex = ColumnLetterToIndex(columnLetter);

                Column? matchedColumn = columns.FirstOrDefault(c =>
                    c.Min?.Value <= columnIndex && c.Max?.Value >= columnIndex);

                if (matchedColumn?.Width?.Value != null)
                {
                    double actualWidth = matchedColumn.Width.Value;
                    bool isMatch = Math.Abs(actualWidth - expectedWidth) < 0.1;

                    result.ExpectedValue = expectedWidth.ToString("F1");
                    result.ActualValue = actualWidth.ToString("F1");
                    result.IsCorrect = isMatch;
                    result.AchievedScore = isMatch ? result.TotalScore : 0;
                    result.Details = $"列宽设置检测(列{columnLetter}): 期望 {expectedWidth:F1}, 实际 {actualWidth:F1}";
                }
                else
                {
                    SetKnowledgePointFailure(result, $"列 {columnLetter} 没有设置列宽");
                }
            }
            else
            {
                // -1 模式：任意匹配
                bool HeightMatches(double? actual, double? expected) =>
                    actual.HasValue && expected.HasValue && Math.Abs(actual.Value - expected.Value) < 0.1;

                bool isMatch = FindMatchingElement(
                    columns,
                    -1,
                    (double?)expectedWidth,
                    column => column.Width?.Value,
                    HeightMatches,
                    out Column? matchedColumn,
                    out double? actualWidth,
                    out string errorMessage,
                    "列");

                if (!isMatch)
                {
                    SetKnowledgePointFailure(result, errorMessage);
                    return result;
                }

                result.ExpectedValue = expectedWidth.ToString("F1");
                result.ActualValue = actualWidth?.ToString("F1") ?? "0";
                result.IsCorrect = true;
                result.AchievedScore = result.TotalScore;
                result.Details = $"列宽设置检测(任意匹配): 找到匹配的列宽 {actualWidth:F1}";
            }
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
            // 验证必需参数
            string[] requiredParams = ["CellRange", "FillColor"];
            if (!ValidateRequiredParameters(parameters, "SetCellFillColor", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["NewSheetName"];
            if (!ValidateRequiredParameters(parameters, "ModifySheetName", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            (bool Found, string SheetName) sheetNameInfo = CheckModifiedSheetNameInWorkbook(workbookPart, parameters);

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
            // 验证必需参数
            string[] requiredParams = ["CellRange", "StyleType"];
            if (!ValidateRequiredParameters(parameters, "SetCellStyleData", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["DataRange"];
            if (!ValidateRequiredParameters(parameters, "Filter", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["SortColumn", "SortOrder"];
            if (!ValidateRequiredParameters(parameters, "Sort", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["SourceRange", "TargetLocation"];
            if (!ValidateRequiredParameters(parameters, "PivotTable", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            (bool Found, string Details) = CheckPivotTableInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据透视表";
            result.ActualValue = Found ? $"找到数据透视表: {Details}" : "未找到数据透视表";
            result.IsCorrect = Found;
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
            // 验证必需参数
            string[] requiredParams = ["GroupByColumn", "SummaryFunction"];
            if (!ValidateRequiredParameters(parameters, "Subtotal", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["ConditionRange", "CriteriaRange"];
            if (!ValidateRequiredParameters(parameters, "AdvancedFilterCondition", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            // 验证必需参数
            string[] requiredParams = ["DataRange", "OutputRange"];
            if (!ValidateRequiredParameters(parameters, "AdvancedFilterData", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            (bool Found, string ChartType) = CheckChartTypeInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ChartType", out string expectedType) ? expectedType : "图表类型";
            result.ActualValue = Found ? ChartType : "未找到图表类型";
            result.IsCorrect = Found;
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
            // 验证必需参数
            string[] requiredParams = ["StyleType"];
            if (!ValidateRequiredParameters(parameters, "ChartStyle", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

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
            (bool Found, string Title) = CheckChartTitleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ChartTitle", out string expectedTitle) ? expectedTitle : "图表标题";
            result.ActualValue = Found ? Title : "未找到图表标题";
            result.IsCorrect = Found;
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
            (bool Found, string Position) = CheckLegendPositionInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "LegendPosition", out string expectedPosition) ? expectedPosition : "图例位置";
            result.ActualValue = Found ? Position : "未找到图例位置";
            result.IsCorrect = Found;
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
            // 验证必需参数
            string[] requiredParams = ["TargetSheet"];
            if (!ValidateRequiredParameters(parameters, "ChartMove", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            WorkbookPart workbookPart = document.WorkbookPart!;
            (bool Found, string Location) chartMoveInfo = CheckChartMoveInWorkbook(workbookPart, parameters);

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
            (bool Found, string Range) axisDataInfo = CheckCategoryAxisDataRangeInWorkbook(workbookPart, parameters);

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
            (bool Found, string Range) axisDataInfo = CheckValueAxisDataRangeInWorkbook(workbookPart, parameters);

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
            (bool Found, string Format) titleFormatInfo = CheckChartTitleFormatInWorkbook(workbookPart, parameters);

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
            (bool Found, string Title) = CheckHorizontalAxisTitleInWorkbook(workbookPart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "AxisTitle", out string expectedTitle) ? expectedTitle : "横坐标轴标题";
            result.ActualValue = Found ? Title : "未找到横坐标轴标题";
            result.IsCorrect = Found;
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
            (bool Found, string Style) = CheckMajorHorizontalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "主要横网格线";
            result.ActualValue = Found ? $"找到主要横网格线: {Style}" : "未找到主要横网格线";
            result.IsCorrect = Found;
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
            (bool Found, string Style) = CheckMinorHorizontalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "次要横网格线";
            result.ActualValue = Found ? $"找到次要横网格线: {Style}" : "未找到次要横网格线";
            result.IsCorrect = Found;
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
            (bool Found, string Style) = CheckMajorVerticalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "主要纵网格线";
            result.ActualValue = Found ? $"找到主要纵网格线: {Style}" : "未找到主要纵网格线";
            result.IsCorrect = Found;
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
            (bool Found, string Style) = CheckMinorVerticalGridlinesInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "次要纵网格线";
            result.ActualValue = Found ? $"找到次要纵网格线: {Style}" : "未找到次要纵网格线";
            result.IsCorrect = Found;
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
            (bool Found, string Format) seriesFormatInfo = CheckDataSeriesFormatInWorkbook(workbookPart, parameters);

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
            (bool Found, string Position) = CheckDataLabelsInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "数据标签";
            result.ActualValue = Found ? $"找到数据标签: {Position}" : "未找到数据标签";
            result.IsCorrect = Found;
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
            (bool Found, string Format) labelsFormatInfo = CheckDataLabelsFormatInWorkbook(workbookPart, parameters);

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
            (bool Found, string Format) areaFormatInfo = CheckChartAreaFormatInWorkbook(workbookPart, parameters);

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
            (bool Found, string Color) floorColorInfo = CheckChartFloorColorInWorkbook(workbookPart, parameters);

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
            (bool Found, string Style) = CheckChartBorderInWorkbook(workbookPart, parameters);

            result.ExpectedValue = "图表边框";
            result.ActualValue = Found ? $"找到图表边框: {Style}" : "未找到图表边框";
            result.IsCorrect = Found;
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
                    List<Cell> cellsWithData = sheetData.Descendants<Cell>().Where(c => !string.IsNullOrEmpty(c.CellValue?.Text)).ToList();

                    if (cellsWithData.Count > 0)
                    {
                        // 检查是否有重复的值（可能是复制操作）
                        IEnumerable<IGrouping<string?, Cell>> cellValueGroups = cellsWithData.GroupBy(c => c.CellValue?.Text).Where(g => g.Count() > 1);
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
                    List<Row> rowList = sheetData.Elements<Row>().ToList();

                    // 检查行数量变化（可能的插入/删除操作）
                    if (rowList.Count > 1)
                    {
                        // 检查是否有非连续的行号（可能是插入操作）
                        List<uint> rowIndexes = rowList.Where(r => r.RowIndex?.Value != null)
                                               .Select(r => r.RowIndex?.Value ?? 0)
                                               .OrderBy(i => i)
                                               .ToList();

                        if (CheckNonSequentialRows(rowIndexes))
                        {
                            return (true, "检测到行插入/删除操作");
                        }

                        // 检查是否有空行（可能是删除操作的结果）
                        IEnumerable<Row> emptyRows = rowList.Where(r => !r.Elements<Cell>().Any(c => !string.IsNullOrEmpty(c.CellValue?.Text)));
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
    private (bool Found, string Details) CheckFontInWorkbook(WorkbookPart workbookPart, string expectedFont, Dictionary<string, string> parameters)
    {
        try
        {
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;
            List<string> fontDetails = [];

            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
                {
                    string? fontName = font.FontName?.Val?.Value;
                    if (fontName != null)
                    {
                        if (string.IsNullOrEmpty(expectedFont) || TextEquals(fontName, expectedFont))
                        {
                            List<string> fontInfo = [$"字体: {fontName}"];

                            // 检查字体大小
                            if (font.FontSize?.Val?.Value != null)
                            {
                                fontInfo.Add($"大小: {font.FontSize.Val.Value}pt");
                            }

                            // 检查字体颜色
                            if (font.Color?.Rgb?.Value != null)
                            {
                                fontInfo.Add($"颜色: #{font.Color.Rgb.Value}");
                            }

                            // 检查字体样式
                            if (font.Bold?.Val?.Value == true)
                            {
                                fontInfo.Add("粗体");
                            }
                            if (font.Italic?.Val?.Value == true)
                            {
                                fontInfo.Add("斜体");
                            }
                            if (font.Underline != null)
                            {
                                fontInfo.Add("下划线");
                            }

                            fontDetails.Add(string.Join(", ", fontInfo));
                        }
                    }
                }
            }

            if (fontDetails.Count > 0)
            {
                return (true, string.Join("; ", fontDetails));
            }

            return (false, "未找到匹配的字体");
        }
        catch
        {
            return (false, "检测字体时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的字体样式
    /// </summary>
    private (bool Found, string Details) CheckFontStyleInWorkbook(WorkbookPart workbookPart, string expectedStyle, Dictionary<string, string> parameters)
    {
        try
        {
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;
            List<string> styleDetails = [];

            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
                {
                    List<string> foundStyles = [];

                    if (font.Bold != null)
                        foundStyles.Add("粗体");
                    if (font.Italic != null)
                        foundStyles.Add("斜体");
                    if (font.Underline != null)
                        foundStyles.Add("下划线");
                    if (font.Strike != null)
                        foundStyles.Add("删除线");

                    if (foundStyles.Count > 0)
                    {
                        bool matches = false;
                        if (string.IsNullOrEmpty(expectedStyle))
                        {
                            matches = true;
                        }
                        else
                        {
                            string lowerExpected = expectedStyle.ToLowerInvariant();
                            foreach (string style in foundStyles)
                            {
                                if (lowerExpected.Contains("bold") && style == "粗体" ||
                                    lowerExpected.Contains("粗体") && style == "粗体" ||
                                    lowerExpected.Contains("italic") && style == "斜体" ||
                                    lowerExpected.Contains("斜体") && style == "斜体" ||
                                    lowerExpected.Contains("underline") && style == "下划线" ||
                                    lowerExpected.Contains("下划线") && style == "下划线" ||
                                    lowerExpected.Contains("strikethrough") && style == "删除线" ||
                                    lowerExpected.Contains("删除线") && style == "删除线")
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }

                        if (matches)
                        {
                            string fontName = font.FontName?.Val?.Value ?? "未知字体";
                            styleDetails.Add($"{fontName}: {string.Join(", ", foundStyles)}");
                        }
                    }
                }
            }

            if (styleDetails.Count > 0)
            {
                return (true, string.Join("; ", styleDetails));
            }

            return (false, "未找到匹配的字体样式");
        }
        catch
        {
            return (false, "检测字体样式时发生错误");
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
    private (bool Found, string Details) CheckCellAlignmentInWorkbook(WorkbookPart workbookPart, string expectedAlignment, Dictionary<string, string> parameters)
    {
        try
        {
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;
            List<string> alignmentDetails = [];

            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellFormats?.HasChildren == true)
            {
                foreach (CellFormat cellFormat in stylesPart.Stylesheet.CellFormats.Elements<CellFormat>())
                {
                    Alignment? alignment = cellFormat.Alignment;
                    if (alignment != null)
                    {
                        List<string> alignmentInfo = [];

                        // 检查水平对齐
                        if (alignment.Horizontal?.Value != null)
                        {
                            string horizontalValue = alignment.Horizontal.Value.ToString();
                            string horizontalText = horizontalValue switch
                            {
                                "Left" => "左对齐",
                                "Center" => "居中对齐",
                                "Right" => "右对齐",
                                "Justify" => "两端对齐",
                                _ => horizontalValue
                            };
                            alignmentInfo.Add($"水平: {horizontalText}");
                        }

                        // 检查垂直对齐
                        if (alignment.Vertical?.Value != null)
                        {
                            string verticalValue = alignment.Vertical.Value.ToString();
                            string verticalText = verticalValue switch
                            {
                                "Top" => "顶端对齐",
                                "Center" => "居中对齐",
                                "Bottom" => "底端对齐",
                                _ => verticalValue
                            };
                            alignmentInfo.Add($"垂直: {verticalText}");
                        }

                        if (alignmentInfo.Count > 0)
                        {
                            bool matches = false;
                            if (string.IsNullOrEmpty(expectedAlignment))
                            {
                                matches = true;
                            }
                            else
                            {
                                string alignmentText = string.Join(", ", alignmentInfo);
                                matches = alignmentText.Contains(expectedAlignment, StringComparison.OrdinalIgnoreCase) ||
                                         TextEquals(alignment.Horizontal?.Value.ToString() ?? "", expectedAlignment);
                            }

                            if (matches)
                            {
                                alignmentDetails.Add(string.Join(", ", alignmentInfo));
                            }
                        }
                    }
                }
            }

            if (alignmentDetails.Count > 0)
            {
                return (true, string.Join("; ", alignmentDetails.Distinct()));
            }

            return (false, "未找到匹配的对齐方式");
        }
        catch
        {
            return (false, "检测对齐方式时发生错误");
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
    private (bool Found, string Details) CheckNumberFormatInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedFormat = TryGetParameter(parameters, "NumberFormat", out string format) ? format : "";
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            List<string> formatDetails = [];

            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet != null)
            {
                // 检查自定义数字格式
                NumberingFormats? numberingFormats = stylesPart.Stylesheet.NumberingFormats;
                if (numberingFormats?.HasChildren == true)
                {
                    foreach (NumberingFormat numFormat in numberingFormats.Elements<NumberingFormat>())
                    {
                        string formatCode = numFormat.FormatCode?.Value ?? "";
                        uint formatId = numFormat.NumberFormatId?.Value ?? 0;

                        if (!string.IsNullOrEmpty(expectedFormat))
                        {
                            if (TextEquals(formatCode, expectedFormat) ||
                                CheckNumberFormatMatch(formatCode, expectedFormat))
                            {
                                formatDetails.Add($"自定义格式 (ID:{formatId}): {formatCode}");
                            }
                        }
                        else if (!string.IsNullOrEmpty(formatCode))
                        {
                            formatDetails.Add($"自定义格式 (ID:{formatId}): {formatCode}");
                        }
                    }
                }

                // 检查单元格格式中的数字格式
                CellFormats? cellFormats = stylesPart.Stylesheet.CellFormats;
                if (cellFormats?.HasChildren == true)
                {
                    HashSet<uint> usedFormatIds = [];
                    foreach (CellFormat cellFormat in cellFormats.Elements<CellFormat>())
                    {
                        if (cellFormat.NumberFormatId?.Value != null)
                        {
                            uint formatId = cellFormat.NumberFormatId.Value;
                            // 检查是否使用了非默认的数字格式
                            if (formatId is > 0 and not 164) // 164是默认的通用格式
                            {
                                usedFormatIds.Add(formatId);
                            }
                        }
                    }

                    foreach (uint formatId in usedFormatIds)
                    {
                        string builtInFormat = GetBuiltInNumberFormat(formatId);
                        if (!string.IsNullOrEmpty(builtInFormat))
                        {
                            if (string.IsNullOrEmpty(expectedFormat) ||
                                builtInFormat.Contains(expectedFormat, StringComparison.OrdinalIgnoreCase))
                            {
                                formatDetails.Add($"内置格式 (ID:{formatId}): {builtInFormat}");
                            }
                        }
                        else
                        {
                            formatDetails.Add($"数字格式 (ID:{formatId})");
                        }
                    }
                }
            }

            if (formatDetails.Count > 0)
            {
                return (true, string.Join("; ", formatDetails));
            }

            return (false, "未找到匹配的数字格式");
        }
        catch
        {
            return (false, "检测数字格式时发生错误");
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
    private (bool Found, string Details) CheckDataSortInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                // 检查排序状态
                SortState? sortState = worksheetPart.Worksheet.Elements<SortState>().FirstOrDefault();
                if (sortState != null)
                {
                    return (true, "找到排序状态");
                }

                // 检查自动筛选（通常与排序相关）
                AutoFilter? autoFilter = worksheetPart.Worksheet.Elements<AutoFilter>().FirstOrDefault();
                if (autoFilter != null)
                {
                    // 检查是否有排序条件
                    IEnumerable<FilterColumn> filterColumns = autoFilter.Elements<FilterColumn>();
                    foreach (FilterColumn filterColumn in filterColumns)
                    {
                        // 检查是否有排序相关的筛选条件
                        if (filterColumn.HasChildren)
                        {
                            return (true, "找到筛选排序条件");
                        }
                    }
                    return (true, "找到自动筛选（可能有排序）"); // 有自动筛选就认为可能有排序
                }

                // 检查数据是否呈现排序特征
                if (CheckDataSortingPattern(worksheetPart))
                {
                    return (true, "检测到数据排序模式");
                }
            }
            return (false, "未找到数据排序");
        }
        catch
        {
            return (false, "检测数据排序时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的数据透视表
    /// </summary>
    private (bool Found, string Details) CheckPivotTableInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            var pivotCacheParts = workbookPart.PivotTableCacheDefinitionParts.ToList();
            if (pivotCacheParts.Count == 0)
            {
                return (false, "未找到数据透视表");
            }

            List<string> pivotDetails = [];

            foreach (var pivotCachePart in pivotCacheParts)
            {
                var cacheDefinition = pivotCachePart.PivotCacheDefinition;
                if (cacheDefinition != null)
                {
                    List<string> cacheInfo = ["数据透视表"];

                    // 检查数据源
                    if (cacheDefinition.CacheSource?.WorksheetSource?.Reference?.Value != null)
                    {
                        cacheInfo.Add($"数据源: {cacheDefinition.CacheSource.WorksheetSource.Reference.Value}");
                    }

                    // 检查记录数
                    if (cacheDefinition.RecordCount?.Value != null)
                    {
                        cacheInfo.Add($"记录数: {cacheDefinition.RecordCount.Value}");
                    }

                    pivotDetails.Add(string.Join(", ", cacheInfo));
                }
            }

            if (pivotDetails.Count > 0)
            {
                return (true, string.Join("; ", pivotDetails));
            }

            return (true, $"找到 {pivotCacheParts.Count} 个数据透视表");
        }
        catch
        {
            return (false, "检测数据透视表时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的条件格式
    /// </summary>
    private (bool Found, string Details) CheckConditionalFormattingInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedType = TryGetParameter(parameters, "FormatType", out string formatType) ? formatType : "";
            string expectedRange = TryGetParameter(parameters, "Range", out string range) ? range : "";
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            List<WorksheetPart> worksheets = workbookPart.WorksheetParts.ToList();
            if (worksheets.Count == 0)
            {
                return (false, "未找到工作表");
            }

            // 如果指定了具体的工作表编号且不是-1
            if (worksheetNumber != -1)
            {
                if (worksheetNumber < 1 || worksheetNumber > worksheets.Count)
                {
                    return (false, $"工作表编号 {worksheetNumber} 超出范围");
                }

                WorksheetPart worksheetPart = worksheets[worksheetNumber - 1];
                string formatDetails = GetConditionalFormattingDetails(worksheetPart, expectedType, expectedRange);
                return (!string.IsNullOrEmpty(formatDetails), formatDetails);
            }

            // -1 模式：任意匹配，检查所有工作表
            foreach (WorksheetPart worksheetPart in worksheets)
            {
                string formatDetails = GetConditionalFormattingDetails(worksheetPart, expectedType, expectedRange);
                if (!string.IsNullOrEmpty(formatDetails))
                {
                    return (true, formatDetails);
                }
            }

            return (false, "未找到匹配的条件格式");
        }
        catch
        {
            return (false, "检测条件格式时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的数据验证
    /// </summary>
    private (bool Found, string Details) CheckDataValidationInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedType = TryGetParameter(parameters, "ValidationType", out string validationType) ? validationType : "";
            string expectedRange = TryGetParameter(parameters, "Range", out string range) ? range : "";
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            List<WorksheetPart> worksheets = workbookPart.WorksheetParts.ToList();
            if (worksheets.Count == 0)
            {
                return (false, "未找到工作表");
            }

            // 如果指定了具体的工作表编号且不是-1
            if (worksheetNumber != -1)
            {
                if (worksheetNumber < 1 || worksheetNumber > worksheets.Count)
                {
                    return (false, $"工作表编号 {worksheetNumber} 超出范围");
                }

                WorksheetPart worksheetPart = worksheets[worksheetNumber - 1];
                string validationDetails = GetDataValidationDetails(worksheetPart, expectedType, expectedRange);
                return (!string.IsNullOrEmpty(validationDetails), validationDetails);
            }

            // -1 模式：任意匹配，检查所有工作表
            foreach (WorksheetPart worksheetPart in worksheets)
            {
                string validationDetails = GetDataValidationDetails(worksheetPart, expectedType, expectedRange);
                if (!string.IsNullOrEmpty(validationDetails))
                {
                    return (true, validationDetails);
                }
            }

            return (false, "未找到匹配的数据验证");
        }
        catch
        {
            return (false, "检测数据验证时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的冻结窗格
    /// </summary>
    private (bool Found, string Details) CheckFreezePanesInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;
            List<string> freezeDetails = [];

            List<WorksheetPart> worksheets = workbookPart.WorksheetParts.ToList();
            if (worksheets.Count == 0)
            {
                return (false, "未找到工作表");
            }

            // 如果指定了具体的工作表编号且不是-1
            if (worksheetNumber != -1)
            {
                if (worksheetNumber < 1 || worksheetNumber > worksheets.Count)
                {
                    return (false, $"工作表编号 {worksheetNumber} 超出范围");
                }

                var worksheetPart = worksheets[worksheetNumber - 1];
                string freezeInfo = GetFreezePanesDetails(worksheetPart);
                return (!string.IsNullOrEmpty(freezeInfo), freezeInfo);
            }

            // -1 模式：任意匹配，检查所有工作表
            foreach (var worksheetPart in worksheets)
            {
                string freezeInfo = GetFreezePanesDetails(worksheetPart);
                if (!string.IsNullOrEmpty(freezeInfo))
                {
                    freezeDetails.Add(freezeInfo);
                }
            }

            if (freezeDetails.Count > 0)
            {
                return (true, string.Join("; ", freezeDetails));
            }

            return (false, "未找到冻结窗格");
        }
        catch
        {
            return (false, "检测冻结窗格时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的页面设置
    /// </summary>
    private (bool Found, string Details) CheckPageSetupInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedOrientation = TryGetParameter(parameters, "Orientation", out string orientation) ? orientation : "";
            string expectedPaperSize = TryGetParameter(parameters, "PaperSize", out string paperSize) ? paperSize : "";
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            List<WorksheetPart> worksheets = workbookPart.WorksheetParts.ToList();
            if (worksheets.Count == 0)
            {
                return (false, "未找到工作表");
            }

            // 如果指定了具体的工作表编号且不是-1
            if (worksheetNumber != -1)
            {
                if (worksheetNumber < 1 || worksheetNumber > worksheets.Count)
                {
                    return (false, $"工作表编号 {worksheetNumber} 超出范围");
                }

                var worksheetPart = worksheets[worksheetNumber - 1];
                string setupDetails = GetPageSetupDetails(worksheetPart, expectedOrientation, expectedPaperSize);
                return (!string.IsNullOrEmpty(setupDetails), setupDetails);
            }

            // -1 模式：任意匹配，检查所有工作表
            foreach (var worksheetPart in worksheets)
            {
                string setupDetails = GetPageSetupDetails(worksheetPart, expectedOrientation, expectedPaperSize);
                if (!string.IsNullOrEmpty(setupDetails))
                {
                    return (true, setupDetails);
                }
            }

            return (false, "未找到匹配的页面设置");
        }
        catch
        {
            return (false, "检测页面设置时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的打印区域
    /// </summary>
    private (bool Found, string Details) CheckPrintAreaInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedRange = TryGetParameter(parameters, "PrintRange", out string range) ? range : "";
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            List<string> printAreaDetails = [];

            DefinedNames? definedNames = workbookPart.Workbook.DefinedNames;
            if (definedNames?.HasChildren == true)
            {
                foreach (DefinedName definedName in definedNames.Elements<DefinedName>())
                {
                    if (definedName.Name?.Value == "Print_Area")
                    {
                        string printAreaValue = definedName.Text ?? "";

                        // 检查是否匹配指定的工作表
                        if (worksheetNumber != -1)
                        {
                            // 解析打印区域中的工作表引用
                            if (!printAreaValue.Contains($"Sheet{worksheetNumber}") &&
                                !printAreaValue.Contains($"工作表{worksheetNumber}"))
                            {
                                continue; // 不是指定的工作表
                            }
                        }

                        // 检查是否匹配指定的范围
                        if (!string.IsNullOrEmpty(expectedRange) &&
                            !printAreaValue.Contains(expectedRange, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // 范围不匹配
                        }

                        printAreaDetails.Add($"打印区域: {printAreaValue}");
                    }
                }
            }

            if (printAreaDetails.Count > 0)
            {
                return (true, string.Join("; ", printAreaDetails));
            }

            return (false, "未找到匹配的打印区域");
        }
        catch
        {
            return (false, "检测打印区域时发生错误");
        }
    }

    /// <summary>
    /// 检查工作簿中的页眉页脚
    /// </summary>
    private (bool Found, string Details) CheckHeaderFooterInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedContent = TryGetParameter(parameters, "HeaderFooterContent", out string content) ? content : "";
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;

            List<WorksheetPart> worksheets = workbookPart.WorksheetParts.ToList();
            if (worksheets.Count == 0)
            {
                return (false, "未找到工作表");
            }

            // 如果指定了具体的工作表编号且不是-1
            if (worksheetNumber != -1)
            {
                if (worksheetNumber < 1 || worksheetNumber > worksheets.Count)
                {
                    return (false, $"工作表编号 {worksheetNumber} 超出范围");
                }

                var worksheetPart = worksheets[worksheetNumber - 1];
                string headerFooterDetails = GetHeaderFooterDetails(worksheetPart, expectedContent);
                return (!string.IsNullOrEmpty(headerFooterDetails), headerFooterDetails);
            }

            // -1 模式：任意匹配，检查所有工作表
            foreach (var worksheetPart in worksheets)
            {
                string headerFooterDetails = GetHeaderFooterDetails(worksheetPart, expectedContent);
                if (!string.IsNullOrEmpty(headerFooterDetails))
                {
                    return (true, headerFooterDetails);
                }
            }

            return (false, "未找到匹配的页眉页脚");
        }
        catch
        {
            return (false, "检测页眉页脚时发生错误");
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
    private (bool Found, string Details) CheckWorksheetProtectionInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            int worksheetNumber = TryGetIntParameter(parameters, "WorksheetNumber", out int wsNum) ? wsNum : -1;
            List<string> protectionDetails = [];

            List<WorksheetPart> worksheets = workbookPart.WorksheetParts.ToList();
            if (worksheets.Count == 0)
            {
                return (false, "未找到工作表");
            }

            // 如果指定了具体的工作表编号且不是-1
            if (worksheetNumber != -1)
            {
                if (worksheetNumber < 1 || worksheetNumber > worksheets.Count)
                {
                    return (false, $"工作表编号 {worksheetNumber} 超出范围");
                }

                var worksheetPart = worksheets[worksheetNumber - 1];
                string protectionInfo = GetWorksheetProtectionDetails(worksheetPart);
                return (!string.IsNullOrEmpty(protectionInfo), protectionInfo);
            }

            // -1 模式：任意匹配，检查所有工作表
            foreach (var worksheetPart in worksheets)
            {
                string protectionInfo = GetWorksheetProtectionDetails(worksheetPart);
                if (!string.IsNullOrEmpty(protectionInfo))
                {
                    protectionDetails.Add(protectionInfo);
                }
            }

            if (protectionDetails.Count > 0)
            {
                return (true, string.Join("; ", protectionDetails));
            }

            return (false, "未找到工作表保护");
        }
        catch
        {
            return (false, "检测工作表保护时发生错误");
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellFormats?.HasChildren == true)
            {
                foreach (CellFormat cellFormat in stylesPart.Stylesheet.CellFormats.Elements<CellFormat>())
                {
                    Alignment? alignment = cellFormat.Alignment;
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.CellFormats?.HasChildren == true)
            {
                foreach (CellFormat cellFormat in stylesPart.Stylesheet.CellFormats.Elements<CellFormat>())
                {
                    Alignment? alignment = cellFormat.Alignment;
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (Border border in stylesPart.Stylesheet.Borders.Elements<Border>())
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (Border border in stylesPart.Stylesheet.Borders.Elements<Border>())
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Borders?.HasChildren == true)
            {
                foreach (Border border in stylesPart.Stylesheet.Borders.Elements<Border>())
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
    /// 检查工作簿中的行高设置
    /// </summary>
    private bool CheckRowHeightInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    foreach (Row row in sheetData.Elements<Row>())
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                Columns? columns = worksheetPart.Worksheet.Elements<Columns>().FirstOrDefault();
                if (columns?.HasChildren == true)
                {
                    foreach (Column column in columns.Elements<Column>())
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fills?.HasChildren == true)
            {
                foreach (Fill fill in stylesPart.Stylesheet.Fills.Elements<Fill>())
                {
                    PatternFill? patternFill = fill.PatternFill;
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fills?.HasChildren == true)
            {
                foreach (Fill fill in stylesPart.Stylesheet.Fills.Elements<Fill>())
                {
                    PatternFill? patternFill = fill.PatternFill;
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
    /// 检查工作簿中的下划线
    /// </summary>
    private bool CheckUnderlineInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            if (stylesPart?.Stylesheet?.Fonts?.HasChildren == true)
            {
                foreach (Font font in stylesPart.Stylesheet.Fonts.Elements<Font>())
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

            Sheets? sheets = workbookPart.Workbook.Sheets;
            if (sheets?.HasChildren == true)
            {
                foreach (Sheet sheet in sheets.Elements<Sheet>())
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
            WorkbookStylesPart? stylesPart = workbookPart.WorkbookStylesPart;
            return stylesPart?.Stylesheet?.CellStyles?.HasChildren == true;
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                AutoFilter? autoFilter = worksheetPart.Worksheet.Elements<AutoFilter>().FirstOrDefault();
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
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SortState? sortState = worksheetPart.Worksheet.Elements<SortState>().FirstOrDefault();
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

            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData?.HasChildren == true)
                {
                    bool hasSubtotalFunction = false;
                    bool hasGroupingStructure = false;

                    // 检查SUBTOTAL函数
                    foreach (Row row in sheetData.Elements<Row>())
                    {
                        foreach (Cell cell in row.Elements<Cell>())
                        {
                            string? cellFormula = cell.CellFormula?.Text;
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
                    RowBreaks? rowGroups = worksheetPart.Worksheet.Elements<RowBreaks>().FirstOrDefault();
                    if (rowGroups != null)
                    {
                        hasGroupingStructure = true;
                    }

                    // 检查大纲级别（分组的另一种表现）
                    IEnumerable<Row> rowsWithOutlineLevel = sheetData.Elements<Row>().Where(r => r.OutlineLevel?.Value > 0);
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
            // 获取参数
            string conditionRange = TryGetParameter(parameters, "ConditionRange", out string condRange) ? condRange : "";
            string criteriaRange = TryGetParameter(parameters, "CriteriaRange", out string critRange) ? critRange : "";
            string filterField = TryGetParameter(parameters, "FilterField", out string field) ? field : "";
            string filterValue = TryGetParameter(parameters, "FilterValue", out string value) ? value : "";

            List<WorksheetPart> worksheetParts = workbookPart.WorksheetParts.ToList();

            // 如果指定了具体的条件范围，检查该范围
            if (!string.IsNullOrEmpty(conditionRange) || !string.IsNullOrEmpty(criteriaRange))
            {
                foreach (WorksheetPart worksheetPart in worksheetParts)
                {
                    if (CheckSpecificFilterCondition(worksheetPart, conditionRange, criteriaRange, filterField, filterValue))
                    {
                        return true;
                    }
                }
                return false;
            }

            // 任意匹配模式：检查是否有任何高级筛选设置
            foreach (WorksheetPart worksheetPart in worksheetParts)
            {
                // 检查自动筛选
                AutoFilter? autoFilter = worksheetPart.Worksheet.Elements<AutoFilter>().FirstOrDefault();
                if (autoFilter?.Elements<FilterColumn>().Any() == true)
                {
                    // 检查是否有复杂的筛选条件
                    foreach (FilterColumn filterColumn in autoFilter.Elements<FilterColumn>())
                    {
                        if (filterColumn.HasChildren &&
                            (filterColumn.Elements<CustomFilters>().Any() ||
                             filterColumn.Elements<Filters>().Any()))
                        {
                            return true;
                        }
                    }
                }

                // 检查是否有筛选状态
                if (CheckFilterState(worksheetPart))
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
            // 获取参数
            string dataRange = TryGetParameter(parameters, "DataRange", out string dRange) ? dRange : "";
            string outputRange = TryGetParameter(parameters, "OutputRange", out string oRange) ? oRange : "";
            string copyToRange = TryGetParameter(parameters, "CopyToRange", out string cRange) ? cRange : "";

            List<WorksheetPart> worksheetParts = [.. workbookPart.WorksheetParts];

            // 如果指定了具体的数据范围和输出范围，检查这些范围
            if (!string.IsNullOrEmpty(dataRange) && !string.IsNullOrEmpty(outputRange))
            {
                foreach (WorksheetPart worksheetPart in worksheetParts)
                {
                    if (CheckAdvancedFilterDataRanges(worksheetPart, dataRange, outputRange, copyToRange))
                    {
                        return true;
                    }
                }
                return false;
            }

            // 任意匹配模式：检查是否有高级筛选的数据输出
            foreach (WorksheetPart worksheetPart in worksheetParts)
            {
                // 检查是否有筛选条件设置
                if (CheckAdvancedFilterConditionInWorkbook(workbookPart, parameters))
                {
                    // 检查是否有筛选结果数据
                    if (CheckFilterResultData(worksheetPart))
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
    /// 检查工作簿中的图表类型
    /// </summary>
    private (bool Found, string ChartType) CheckChartTypeInWorkbook(WorkbookPart workbookPart, Dictionary<string, string> parameters)
    {
        try
        {
            string expectedType = TryGetParameter(parameters, "ChartType", out string expected) ? expected : "";

            // 检查工作表中的图表
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                if (worksheetPart.DrawingsPart?.ChartParts.Any() == true)
                {
                    foreach (ChartPart chartPart in worksheetPart.DrawingsPart.ChartParts)
                    {
                        try
                        {
                            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
                            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
                            if (chart?.PlotArea != null)
                            {
                                DocumentFormat.OpenXml.Drawing.Charts.PlotArea plotArea = chart.PlotArea;

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
            (bool Found, int Count) = CheckChartInWorkbook(workbookPart);
            return Found;
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
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string title = GetChartTitle(chartPart);

                if (!string.IsNullOrEmpty(expectedTitle))
                {
                    bool matches = !string.IsNullOrEmpty(title) && title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase);
                    return (matches, title);
                }

                return (!string.IsNullOrEmpty(title), title);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string title = GetChartTitle(chartPart);

                if (!string.IsNullOrEmpty(title))
                {
                    if (string.IsNullOrEmpty(expectedTitle) ||
                        title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, title);
                    }
                }
            }

            return (false, "未找到匹配的图表标题");
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
            string expectedPosition = TryGetParameter(parameters, "LegendPosition", out string position) ? position : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string legendPosition = GetLegendPosition(chartPart);

                if (!string.IsNullOrEmpty(expectedPosition))
                {
                    bool matches = !string.IsNullOrEmpty(legendPosition) && legendPosition.Contains(expectedPosition, StringComparison.OrdinalIgnoreCase);
                    return (matches, legendPosition);
                }

                return (!string.IsNullOrEmpty(legendPosition), legendPosition);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string legendPosition = GetLegendPosition(chartPart);

                if (!string.IsNullOrEmpty(legendPosition))
                {
                    if (string.IsNullOrEmpty(expectedPosition) ||
                        legendPosition.Contains(expectedPosition, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, legendPosition);
                    }
                }
            }

            return (false, "未找到匹配的图例位置");
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
                WorksheetPart? targetWorksheet = workbookPart.WorksheetParts.FirstOrDefault(ws =>
                {
                    string sheetName = GetWorksheetName(workbookPart, ws);
                    return TextEquals(sheetName, targetSheet);
                });

                if (targetWorksheet?.DrawingsPart?.ChartParts.Any() == true)
                {
                    return (true, $"图表移动到 {targetSheet}");
                }
            }

            // 详细检测：检查所有工作表中的图表位置
            List<string> chartPositions = [];
            int sheetIndex = 1;

            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                if (worksheetPart.DrawingsPart?.ChartParts.Any() == true)
                {
                    int chartCount = worksheetPart.DrawingsPart.ChartParts.Count();
                    string sheetName = GetWorksheetName(workbookPart, worksheetPart) ?? $"工作表{sheetIndex}";
                    chartPositions.Add($"{sheetName}: {chartCount}个图表");
                }
                sheetIndex++;
            }

            if (chartPositions.Count > 0)
            {
                return (true, string.Join("; ", chartPositions));
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
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string range = GetCategoryAxisDataRange(chartPart, expectedRange);
                return (!string.IsNullOrEmpty(range), range);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string range = GetCategoryAxisDataRange(chartPart, expectedRange);
                if (!string.IsNullOrEmpty(range))
                {
                    return (true, range);
                }
            }

            return (false, "未找到分类轴数据区域");
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
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string range = GetValueAxisDataRange(chartPart, expectedRange);
                return (!string.IsNullOrEmpty(range), range);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string range = GetValueAxisDataRange(chartPart, expectedRange);
                if (!string.IsNullOrEmpty(range))
                {
                    return (true, range);
                }
            }

            return (false, "未找到数值轴数据区域");
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
            string expectedFontName = TryGetParameter(parameters, "FontName", out string fontName) ? fontName : "";
            string expectedFontSize = TryGetParameter(parameters, "FontSize", out string fontSize) ? fontSize : "";
            string expectedFontColor = TryGetParameter(parameters, "FontColor", out string fontColor) ? fontColor : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string format = GetChartTitleFormat(chartPart, expectedFontName, expectedFontSize, expectedFontColor);
                return (!string.IsNullOrEmpty(format), format);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string format = GetChartTitleFormat(chartPart, expectedFontName, expectedFontSize, expectedFontColor);
                if (!string.IsNullOrEmpty(format))
                {
                    return (true, format);
                }
            }

            return (false, "未找到匹配的图表标题格式");
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
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string axisTitle = GetHorizontalAxisTitle(chartPart);

                if (!string.IsNullOrEmpty(expectedTitle))
                {
                    bool matches = !string.IsNullOrEmpty(axisTitle) && axisTitle.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase);
                    return (matches, axisTitle);
                }

                return (!string.IsNullOrEmpty(axisTitle), axisTitle);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string axisTitle = GetHorizontalAxisTitle(chartPart);

                if (!string.IsNullOrEmpty(axisTitle))
                {
                    if (string.IsNullOrEmpty(expectedTitle) ||
                        axisTitle.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, axisTitle);
                    }
                }
            }

            return (false, "未找到匹配的横坐标轴标题");
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
            Sheets? sheets = workbookPart.Workbook.Sheets;
            if (sheets?.HasChildren == true)
            {
                foreach (Sheet sheet in sheets.Elements<Sheet>())
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
            bool expectedVisible = !TryGetParameter(parameters, "GridlineVisible", out string visible) || (TextEquals(visible, "true") || TextEquals(visible, "是"));
            string expectedColor = TryGetParameter(parameters, "GridlineColor", out string color) ? color : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                (bool hasGridlines, string style) = CheckMajorHorizontalGridlines(chartPart, expectedColor);

                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }

                return (false, expectedVisible ? "主要横网格线不可见" : "主要横网格线可见");
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                (bool hasGridlines, string style) = CheckMajorHorizontalGridlines(chartPart, expectedColor);
                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }
            }

            return (false, "未找到匹配的主要横网格线设置");
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
            bool expectedVisible = !TryGetParameter(parameters, "GridlineVisible", out string visible) || (TextEquals(visible, "true") || TextEquals(visible, "是"));
            string expectedColor = TryGetParameter(parameters, "GridlineColor", out string color) ? color : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                (bool hasGridlines, string style) = CheckMinorHorizontalGridlines(chartPart, expectedColor);

                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }

                return (false, expectedVisible ? "次要横网格线不可见" : "次要横网格线可见");
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                (bool hasGridlines, string style) = CheckMinorHorizontalGridlines(chartPart, expectedColor);
                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }
            }

            return (false, "未找到匹配的次要横网格线设置");
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
            bool expectedVisible = !TryGetParameter(parameters, "GridlineVisible", out string visible) || (TextEquals(visible, "true") || TextEquals(visible, "是"));
            string expectedColor = TryGetParameter(parameters, "GridlineColor", out string color) ? color : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                (bool hasGridlines, string style) = CheckMajorVerticalGridlines(chartPart, expectedColor);

                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }

                return (false, expectedVisible ? "主要纵网格线不可见" : "主要纵网格线可见");
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                (bool hasGridlines, string style) = CheckMajorVerticalGridlines(chartPart, expectedColor);
                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }
            }

            return (false, "未找到匹配的主要纵网格线设置");
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
            bool expectedVisible = !TryGetParameter(parameters, "GridlineVisible", out string visible) || (TextEquals(visible, "true") || TextEquals(visible, "是"));
            string expectedColor = TryGetParameter(parameters, "GridlineColor", out string color) ? color : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                (bool hasGridlines, string style) = CheckMinorVerticalGridlines(chartPart, expectedColor);

                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }

                return (false, expectedVisible ? "次要纵网格线不可见" : "次要纵网格线可见");
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                (bool hasGridlines, string style) = CheckMinorVerticalGridlines(chartPart, expectedColor);
                if (hasGridlines == expectedVisible)
                {
                    return (true, style);
                }
            }

            return (false, "未找到匹配的次要纵网格线设置");
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
            string expectedColor = TryGetParameter(parameters, "SeriesColor", out string seriesColor) ? seriesColor : "";
            int seriesIndex = TryGetIntParameter(parameters, "SeriesIndex", out int index) ? index : -1;
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string format = GetDataSeriesFormat(chartPart, seriesIndex, expectedColor);
                return (!string.IsNullOrEmpty(format), format);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string format = GetDataSeriesFormat(chartPart, seriesIndex, expectedColor);
                if (!string.IsNullOrEmpty(format))
                {
                    return (true, format);
                }
            }

            return (false, "未找到匹配的数据系列格式");
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
            string expectedPosition = TryGetParameter(parameters, "LabelPosition", out string position) ? position : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string labelInfo = GetDataLabelsInfo(chartPart, expectedPosition);
                return (!string.IsNullOrEmpty(labelInfo), labelInfo);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string labelInfo = GetDataLabelsInfo(chartPart, expectedPosition);
                if (!string.IsNullOrEmpty(labelInfo))
                {
                    return (true, labelInfo);
                }
            }

            return (false, "未找到匹配的数据标签");
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
            string expectedFontName = TryGetParameter(parameters, "FontName", out string fontName) ? fontName : "";
            string expectedFontSize = TryGetParameter(parameters, "FontSize", out string fontSize) ? fontSize : "";
            string expectedFontColor = TryGetParameter(parameters, "FontColor", out string fontColor) ? fontColor : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string format = GetDataLabelsFormat(chartPart, expectedFontName, expectedFontSize, expectedFontColor);
                return (!string.IsNullOrEmpty(format), format);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string format = GetDataLabelsFormat(chartPart, expectedFontName, expectedFontSize, expectedFontColor);
                if (!string.IsNullOrEmpty(format))
                {
                    return (true, format);
                }
            }

            return (false, "未找到匹配的数据标签格式");
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
            string expectedFillColor = TryGetParameter(parameters, "FillColor", out string fillColor) ? fillColor : "";
            string expectedBorderColor = TryGetParameter(parameters, "BorderColor", out string borderColor) ? borderColor : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string format = GetChartAreaFormat(chartPart, expectedFillColor, expectedBorderColor);
                return (!string.IsNullOrEmpty(format), format);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string format = GetChartAreaFormat(chartPart, expectedFillColor, expectedBorderColor);
                if (!string.IsNullOrEmpty(format))
                {
                    return (true, format);
                }
            }

            return (false, "未找到匹配的图表区域格式");
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
            string expectedColor = TryGetParameter(parameters, "FloorColor", out string color) ? color : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string floorColor = GetChartFloorColor(chartPart, expectedColor);
                return (!string.IsNullOrEmpty(floorColor), floorColor);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string floorColor = GetChartFloorColor(chartPart, expectedColor);
                if (!string.IsNullOrEmpty(floorColor))
                {
                    return (true, floorColor);
                }
            }

            return (false, "未找到匹配的图表基底颜色");
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
            string expectedStyle = TryGetParameter(parameters, "BorderStyle", out string borderStyle) ? borderStyle : "";
            string expectedColor = TryGetParameter(parameters, "BorderColor", out string borderColor) ? borderColor : "";
            int chartNumber = TryGetIntParameter(parameters, "ChartNumber", out int chartNum) ? chartNum : -1;

            // 获取所有图表
            List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> charts = GetAllCharts(workbookPart);

            if (charts.Count == 0)
            {
                return (false, "未找到图表");
            }

            // 如果指定了具体的图表编号且不是-1
            if (chartNumber != -1)
            {
                if (chartNumber < 1 || chartNumber > charts.Count)
                {
                    return (false, $"图表编号 {chartNumber} 超出范围");
                }

                (WorksheetPart worksheetPart, ChartPart chartPart) = charts[chartNumber - 1];
                string borderInfo = GetChartBorder(chartPart, expectedStyle, expectedColor);
                return (!string.IsNullOrEmpty(borderInfo), borderInfo);
            }

            // -1 模式：任意匹配，检查所有图表
            foreach ((WorksheetPart worksheetPart, ChartPart chartPart) in charts)
            {
                string borderInfo = GetChartBorder(chartPart, expectedStyle, expectedColor);
                if (!string.IsNullOrEmpty(borderInfo))
                {
                    return (true, borderInfo);
                }
            }

            return (false, "未找到匹配的图表边框");
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
            List<Cell> numericCells = cellsWithData.Where(c =>
            {
                return double.TryParse(c.CellValue?.Text, out double value);
            }).ToList();

            if (numericCells.Count >= 3)
            {
                List<double> values = numericCells.Select(c => double.Parse(c.CellValue?.Text ?? "0")).OrderBy(v => v).ToList();

                // 检查是否是等差数列
                double diff = values[1] - values[0];
                for (int i = 2; i < values.Count; i++)
                {
                    if (Math.Abs(values[i] - values[i - 1] - diff) < 0.001)
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
    private bool CheckNonSequentialRows(List<uint> rowIndexes)
    {
        try
        {
            if (rowIndexes.Count < 2)
            {
                return false;
            }

            for (int i = 1; i < rowIndexes.Count; i++)
            {
                if (rowIndexes[i] - rowIndexes[i - 1] > 1)
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
            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData?.HasChildren != true)
            {
                return false;
            }

            List<Row> rows = sheetData.Elements<Row>().Take(10).ToList(); // 检查前10行
            if (rows.Count < 3)
            {
                return false;
            }

            // 检查第一列的数据是否呈现排序特征
            List<string> firstColumnValues = [];
            foreach (Row? row in rows)
            {
                Cell? firstCell = row.Elements<Cell>().FirstOrDefault();
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
                    int comparison = string.Compare(firstColumnValues[i], firstColumnValues[i - 1], StringComparison.OrdinalIgnoreCase);
                    if (comparison < 0)
                    {
                        isAscending = false;
                    }

                    if (comparison > 0)
                    {
                        isDescending = false;
                    }
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
            Dictionary<string, string[]> formatMappings = new()
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

            foreach (KeyValuePair<string, string[]> mapping in formatMappings)
            {
                if (TextEquals(mapping.Key, expectedFormat))
                {
                    return mapping.Value.Any(lowerFormatCode.Contains);
                }
            }

            return lowerFormatCode.Contains(lowerExpected);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 在元素集合中搜索匹配指定条件的元素（支持编号-1的任意匹配模式）
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <typeparam name="TValue">期望值的类型</typeparam>
    /// <param name="elements">元素集合</param>
    /// <param name="elementNumber">元素编号（-1表示搜索所有元素，找到任意一个匹配的即可）</param>
    /// <param name="expectedValue">期望值</param>
    /// <param name="getActualValue">获取元素实际值的函数</param>
    /// <param name="comparer">比较函数，如果为null则使用默认相等比较</param>
    /// <param name="matchedElement">匹配的元素</param>
    /// <param name="actualValue">实际找到的值</param>
    /// <param name="errorMessage">错误信息</param>
    /// <param name="elementTypeName">元素类型名称（用于错误信息）</param>
    /// <returns>是否找到匹配的元素</returns>
    private static bool FindMatchingElement<TElement, TValue>(
        IList<TElement>? elements,
        int elementNumber,
        TValue expectedValue,
        Func<TElement, TValue> getActualValue,
        Func<TValue, TValue, bool>? comparer,
        out TElement? matchedElement,
        out TValue? actualValue,
        out string errorMessage,
        string elementTypeName = "元素")
    {
        matchedElement = default;
        actualValue = default;
        errorMessage = string.Empty;

        if (elements == null || elements.Count == 0)
        {
            errorMessage = $"没有找到任何{elementTypeName}";
            return false;
        }

        // 使用默认比较器如果没有提供
        comparer ??= EqualityComparer<TValue>.Default.Equals;

        // 如果指定了具体元素索引
        if (elementNumber != -1)
        {
            if (elementNumber < 1 || elementNumber > elements.Count)
            {
                errorMessage = $"{elementTypeName}索引超出范围: {elementNumber}，有效范围: 1-{elements.Count}";
                return false;
            }

            matchedElement = elements[elementNumber - 1];
            actualValue = getActualValue(matchedElement);
            bool isMatch = comparer(actualValue, expectedValue);

            if (!isMatch)
            {
                errorMessage = $"{elementTypeName} {elementNumber} 的值不匹配期望值";
            }

            return isMatch;
        }

        // -1 表示搜索所有元素，找到任意一个匹配的即可
        List<string> debugInfo = [];
        for (int i = 0; i < elements.Count; i++)
        {
            TElement element = elements[i];
            TValue currentValue = getActualValue(element);
            debugInfo.Add($"{elementTypeName}{i + 1}: {currentValue}");

            if (comparer(currentValue, expectedValue))
            {
                matchedElement = element;
                actualValue = currentValue;
                System.Diagnostics.Debug.WriteLine($"[{elementTypeName}匹配成功] 在{elementTypeName}{i + 1}找到匹配值: {currentValue}");
                return true;
            }
        }

        // 没有找到匹配的元素，返回第一个元素的值作为实际值
        if (elements.Count > 0)
        {
            matchedElement = elements[0];
            actualValue = getActualValue(matchedElement);
        }

        string allValues = string.Join("; ", debugInfo);
        errorMessage = $"在所有{elementTypeName}中都没有找到匹配期望值'{expectedValue}'的{elementTypeName}。实际值: {allValues}";
        System.Diagnostics.Debug.WriteLine($"[{elementTypeName}匹配失败] 期望: {expectedValue}, 实际检查的{elementTypeName}: {allValues}");
        return false;
    }

    /// <summary>
    /// 将列字母转换为列索引（A=1, B=2, ..., Z=26, AA=27, etc.）
    /// </summary>
    private static uint ColumnLetterToIndex(string columnLetter)
    {
        if (string.IsNullOrEmpty(columnLetter))
            return 1;

        uint result = 0;
        for (int i = 0; i < columnLetter.Length; i++)
        {
            result = result * 26 + (uint)(char.ToUpper(columnLetter[i]) - 'A' + 1);
        }
        return result;
    }

    /// <summary>
    /// 检查特定的筛选条件
    /// </summary>
    private bool CheckSpecificFilterCondition(WorksheetPart worksheetPart, string conditionRange, string criteriaRange, string filterField, string filterValue)
    {
        try
        {
            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null) return false;

            // 检查条件范围是否包含指定的字段和值
            if (!string.IsNullOrEmpty(conditionRange))
            {
                if (CheckRangeContainsValue(sheetData, conditionRange, filterField) ||
                    CheckRangeContainsValue(sheetData, conditionRange, filterValue))
                {
                    return true;
                }
            }

            // 检查条件范围
            if (!string.IsNullOrEmpty(criteriaRange))
            {
                if (CheckRangeContainsValue(sheetData, criteriaRange, filterField) ||
                    CheckRangeContainsValue(sheetData, criteriaRange, filterValue))
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
    /// 检查筛选状态
    /// </summary>
    private bool CheckFilterState(WorksheetPart worksheetPart)
    {
        try
        {
            // 检查是否有隐藏的行（筛选的结果）
            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData?.HasChildren == true)
            {
                foreach (Row row in sheetData.Elements<Row>())
                {
                    if (row.Hidden?.Value == true)
                    {
                        return true; // 有隐藏行，可能是筛选结果
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
    /// 检查指定范围是否包含特定值
    /// </summary>
    private bool CheckRangeContainsValue(SheetData sheetData, string range, string value)
    {
        try
        {
            if (string.IsNullOrEmpty(range) || string.IsNullOrEmpty(value))
                return false;

            // 详细实现：检查指定范围内的单元格值
            foreach (Row row in sheetData.Elements<Row>())
            {
                foreach (Cell cell in row.Elements<Cell>())
                {
                    string cellRef = cell.CellReference?.Value ?? "";

                    // 如果指定了范围，检查单元格是否在范围内
                    bool inRange = true;
                    if (!string.IsNullOrEmpty(range))
                    {
                        if (range.Contains(":"))
                        {
                            // 范围格式如 "A1:B10"，简单检查是否包含单元格引用
                            string[] rangeParts = range.Split(':');
                            if (rangeParts.Length == 2)
                            {
                                // 简化检查：如果单元格引用在范围的字母数字范围内
                                inRange = IsSimpleRangeMatch(cellRef, rangeParts[0], rangeParts[1]);
                            }
                        }
                        else
                        {
                            // 单个单元格
                            inRange = cellRef.Equals(range, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    if (inRange)
                    {
                        string cellValue = cell.CellValue?.Text ?? "";
                        if (cellValue.Contains(value, StringComparison.OrdinalIgnoreCase))
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
    /// 检查高级筛选数据范围
    /// </summary>
    private bool CheckAdvancedFilterDataRanges(WorksheetPart worksheetPart, string dataRange, string outputRange, string copyToRange)
    {
        try
        {
            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null) return false;

            // 检查数据范围是否有数据
            bool hasDataInRange = CheckRangeHasData(sheetData, dataRange);

            // 检查输出范围是否有数据
            bool hasOutputData = CheckRangeHasData(sheetData, outputRange);

            // 如果指定了复制到范围，也检查该范围
            if (!string.IsNullOrEmpty(copyToRange))
            {
                bool hasCopyToData = CheckRangeHasData(sheetData, copyToRange);
                return hasDataInRange && (hasOutputData || hasCopyToData);
            }

            return hasDataInRange && hasOutputData;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查筛选结果数据
    /// </summary>
    private bool CheckFilterResultData(WorksheetPart worksheetPart)
    {
        try
        {
            SheetData? sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData?.HasChildren != true) return false;

            // 检查是否有隐藏行（筛选结果的特征）
            bool hasHiddenRows = false;
            bool hasVisibleRows = false;

            foreach (Row row in sheetData.Elements<Row>())
            {
                if (row.Hidden?.Value == true)
                {
                    hasHiddenRows = true;
                }
                else
                {
                    hasVisibleRows = true;
                }
            }

            // 如果既有隐藏行又有可见行，说明可能进行了筛选
            return hasHiddenRows && hasVisibleRows;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查指定范围是否有数据
    /// </summary>
    private bool CheckRangeHasData(SheetData sheetData, string range)
    {
        try
        {
            if (string.IsNullOrEmpty(range)) return false;

            // 详细实现：检查指定范围内是否有数据
            foreach (Row row in sheetData.Elements<Row>())
            {
                foreach (Cell cell in row.Elements<Cell>())
                {
                    string cellRef = cell.CellReference?.Value ?? "";

                    // 检查单元格是否在指定范围内
                    bool inRange = true;
                    if (!string.IsNullOrEmpty(range))
                    {
                        if (range.Contains(":"))
                        {
                            // 范围格式如 "A1:B10"
                            string[] rangeParts = range.Split(':');
                            if (rangeParts.Length == 2)
                            {
                                inRange = IsSimpleRangeMatch(cellRef, rangeParts[0], rangeParts[1]);
                            }
                        }
                        else
                        {
                            // 单个单元格
                            inRange = cellRef.Equals(range, StringComparison.OrdinalIgnoreCase);
                        }
                    }

                    if (inRange && !string.IsNullOrEmpty(cell.CellValue?.Text))
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
    /// 获取工作簿中的所有图表
    /// </summary>
    private List<(WorksheetPart WorksheetPart, ChartPart ChartPart)> GetAllCharts(WorkbookPart workbookPart)
    {
        List<(WorksheetPart, ChartPart)> charts = [];

        try
        {
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                if (worksheetPart.DrawingsPart?.ChartParts != null)
                {
                    foreach (ChartPart chartPart in worksheetPart.DrawingsPart.ChartParts)
                    {
                        charts.Add((worksheetPart, chartPart));
                    }
                }
            }
        }
        catch
        {
            // 忽略错误，返回已找到的图表
        }

        return charts;
    }

    /// <summary>
    /// 获取分类轴数据区域
    /// </summary>
    private string GetCategoryAxisDataRange(ChartPart chartPart, string expectedRange)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            // 检查不同类型的图表系列
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.BarChart> barCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.LineChart> lineCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.PieChart> pieCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChart>();

            // 从柱形图中获取分类轴数据
            foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChart barChart in barCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries series in barChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.CategoryAxisData? categoryAxisData = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxisData>().FirstOrDefault();
                    if (categoryAxisData?.StringReference?.Formula?.Text != null)
                    {
                        string range = categoryAxisData.StringReference.Formula.Text;
                        if (string.IsNullOrEmpty(expectedRange) || range.Contains(expectedRange))
                        {
                            return range;
                        }
                    }
                }
            }

            // 从折线图中获取分类轴数据
            foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChart lineChart in lineCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries series in lineChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.CategoryAxisData? categoryAxisData = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxisData>().FirstOrDefault();
                    if (categoryAxisData?.StringReference?.Formula?.Text != null)
                    {
                        string range = categoryAxisData.StringReference.Formula.Text;
                        if (string.IsNullOrEmpty(expectedRange) || range.Contains(expectedRange))
                        {
                            return range;
                        }
                    }
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取数值轴数据区域
    /// </summary>
    private string GetValueAxisDataRange(ChartPart chartPart, string expectedRange)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            // 检查不同类型的图表系列
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.BarChart> barCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.LineChart> lineCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.PieChart> pieCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChart>();

            // 从柱形图中获取数值轴数据
            foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChart barChart in barCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries series in barChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.Values? valueAxisData = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.Values>().FirstOrDefault();
                    if (valueAxisData?.NumberReference?.Formula?.Text != null)
                    {
                        string range = valueAxisData.NumberReference.Formula.Text;
                        if (string.IsNullOrEmpty(expectedRange) || range.Contains(expectedRange))
                        {
                            return range;
                        }
                    }
                }
            }

            // 从折线图中获取数值轴数据
            foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChart lineChart in lineCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries series in lineChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.Values? valueAxisData = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.Values>().FirstOrDefault();
                    if (valueAxisData?.NumberReference?.Formula?.Text != null)
                    {
                        string range = valueAxisData.NumberReference.Formula.Text;
                        if (string.IsNullOrEmpty(expectedRange) || range.Contains(expectedRange))
                        {
                            return range;
                        }
                    }
                }
            }

            // 从饼图中获取数值数据
            foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChart pieChart in pieCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries series in pieChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.Values? valueAxisData = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.Values>().FirstOrDefault();
                    if (valueAxisData?.NumberReference?.Formula?.Text != null)
                    {
                        string range = valueAxisData.NumberReference.Formula.Text;
                        if (string.IsNullOrEmpty(expectedRange) || range.Contains(expectedRange))
                        {
                            return range;
                        }
                    }
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取图表标题
    /// </summary>
    private string GetChartTitle(ChartPart chartPart)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.Title? title = chart?.Title;

            if (title?.ChartText?.RichText != null)
            {
                // 尝试从RichText中获取文本
                DocumentFormat.OpenXml.Drawing.Charts.RichText richText = title.ChartText.RichText;
                IEnumerable<DocumentFormat.OpenXml.Drawing.Paragraph> paragraphs = richText.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>();

                foreach (DocumentFormat.OpenXml.Drawing.Paragraph paragraph in paragraphs)
                {
                    IEnumerable<DocumentFormat.OpenXml.Drawing.Run> runs = paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>();
                    foreach (DocumentFormat.OpenXml.Drawing.Run run in runs)
                    {
                        string? text = run.Text?.Text;
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }
                }
            }

            // 尝试从StringReference获取标题
            if (title?.ChartText?.StringReference?.StringCache != null)
            {
                DocumentFormat.OpenXml.Drawing.Charts.StringCache stringCache = title.ChartText.StringReference.StringCache;
                IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.StringPoint> stringPoints = stringCache.Elements<DocumentFormat.OpenXml.Drawing.Charts.StringPoint>();
                DocumentFormat.OpenXml.Drawing.Charts.StringPoint? firstPoint = stringPoints.FirstOrDefault();
                if (firstPoint?.NumericValue?.Text != null)
                {
                    return firstPoint.NumericValue.Text;
                }
            }

            // 如果有标题元素但没有文本，返回默认标题
            if (title != null)
            {
                return "图表标题";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取图表标题格式
    /// </summary>
    private string GetChartTitleFormat(ChartPart chartPart, string expectedFontName, string expectedFontSize, string expectedFontColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.Title? title = chart?.Title;

            if (title?.ChartText?.RichText != null)
            {
                DocumentFormat.OpenXml.Drawing.Charts.RichText richText = title.ChartText.RichText;
                IEnumerable<DocumentFormat.OpenXml.Drawing.Paragraph> paragraphs = richText.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>();

                foreach (DocumentFormat.OpenXml.Drawing.Paragraph paragraph in paragraphs)
                {
                    IEnumerable<DocumentFormat.OpenXml.Drawing.Run> runs = paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>();
                    foreach (DocumentFormat.OpenXml.Drawing.Run run in runs)
                    {
                        DocumentFormat.OpenXml.Drawing.RunProperties? runProperties = run.RunProperties;
                        if (runProperties != null)
                        {
                            List<string> formatInfo = [];

                            // 检查字体名称
                            DocumentFormat.OpenXml.Drawing.LatinFont? latinFont = runProperties.Elements<DocumentFormat.OpenXml.Drawing.LatinFont>().FirstOrDefault();
                            if (latinFont?.Typeface?.Value != null)
                            {
                                string fontName = latinFont.Typeface.Value;
                                formatInfo.Add($"字体: {fontName}");

                                if (!string.IsNullOrEmpty(expectedFontName) &&
                                    !fontName.Contains(expectedFontName, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 字体不匹配，继续检查下一个
                                }
                            }

                            // 检查字体大小
                            if (runProperties.FontSize?.Value != null)
                            {
                                int fontSize = runProperties.FontSize.Value / 100; // OpenXML中字体大小是以百分点为单位
                                formatInfo.Add($"大小: {fontSize}pt");

                                if (!string.IsNullOrEmpty(expectedFontSize) &&
                                    !fontSize.ToString().Contains(expectedFontSize))
                                {
                                    continue; // 字体大小不匹配，继续检查下一个
                                }
                            }

                            // 检查字体颜色
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = runProperties.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                            if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                            {
                                string color = solidFill.RgbColorModelHex.Val.Value;
                                formatInfo.Add($"颜色: #{color}");

                                if (!string.IsNullOrEmpty(expectedFontColor) &&
                                    !color.Contains(expectedFontColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 颜色不匹配，继续检查下一个
                                }
                            }

                            if (formatInfo.Count > 0)
                            {
                                return string.Join(", ", formatInfo);
                            }
                        }
                    }
                }
            }

            // 如果有标题但没有找到格式信息，返回基本信息
            if (title != null)
            {
                return "图表标题格式";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 检查图表的主要横网格线
    /// </summary>
    private (bool HasGridlines, string Style) CheckMajorHorizontalGridlines(ChartPart chartPart, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return (false, string.Empty);

            // 检查值轴的主要网格线
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.ValueAxis> valueAxes = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.ValueAxis>();
            foreach (DocumentFormat.OpenXml.Drawing.Charts.ValueAxis valueAxis in valueAxes)
            {
                DocumentFormat.OpenXml.Drawing.Charts.MajorGridlines? majorGridlines = valueAxis.Elements<DocumentFormat.OpenXml.Drawing.Charts.MajorGridlines>().FirstOrDefault();
                if (majorGridlines != null)
                {
                    // 检查网格线样式
                    DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = majorGridlines.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
                    if (shapeProperties != null)
                    {
                        List<string> styleInfo = ["主要横网格线可见"];

                        // 检查颜色
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                        if (outline != null)
                        {
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                            if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                            {
                                string color = solidFill.RgbColorModelHex.Val.Value;
                                styleInfo.Add($"颜色: #{color}");

                                if (!string.IsNullOrEmpty(expectedColor) &&
                                    !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 颜色不匹配
                                }
                            }
                        }

                        return (true, string.Join(", ", styleInfo));
                    }

                    return (true, "主要横网格线可见");
                }
            }

            return (false, "主要横网格线不可见");
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查图表的次要横网格线
    /// </summary>
    private (bool HasGridlines, string Style) CheckMinorHorizontalGridlines(ChartPart chartPart, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return (false, string.Empty);

            // 检查值轴的次要网格线
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.ValueAxis> valueAxes = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.ValueAxis>();
            foreach (DocumentFormat.OpenXml.Drawing.Charts.ValueAxis valueAxis in valueAxes)
            {
                DocumentFormat.OpenXml.Drawing.Charts.MinorGridlines? minorGridlines = valueAxis.Elements<DocumentFormat.OpenXml.Drawing.Charts.MinorGridlines>().FirstOrDefault();
                if (minorGridlines != null)
                {
                    // 检查网格线样式
                    DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = minorGridlines.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
                    if (shapeProperties != null)
                    {
                        List<string> styleInfo = ["次要横网格线可见"];

                        // 检查颜色
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                        if (outline != null)
                        {
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                            if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                            {
                                string color = solidFill.RgbColorModelHex.Val.Value;
                                styleInfo.Add($"颜色: #{color}");

                                if (!string.IsNullOrEmpty(expectedColor) &&
                                    !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 颜色不匹配
                                }
                            }
                        }

                        return (true, string.Join(", ", styleInfo));
                    }

                    return (true, "次要横网格线可见");
                }
            }

            return (false, "次要横网格线不可见");
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查图表的主要纵网格线
    /// </summary>
    private (bool HasGridlines, string Style) CheckMajorVerticalGridlines(ChartPart chartPart, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return (false, string.Empty);

            // 检查分类轴的主要网格线
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis> categoryAxes = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis>();
            foreach (DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis categoryAxis in categoryAxes)
            {
                DocumentFormat.OpenXml.Drawing.Charts.MajorGridlines? majorGridlines = categoryAxis.Elements<DocumentFormat.OpenXml.Drawing.Charts.MajorGridlines>().FirstOrDefault();
                if (majorGridlines != null)
                {
                    // 检查网格线样式
                    DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = majorGridlines.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
                    if (shapeProperties != null)
                    {
                        List<string> styleInfo = ["主要纵网格线可见"];

                        // 检查颜色
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                        if (outline != null)
                        {
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                            if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                            {
                                string color = solidFill.RgbColorModelHex.Val.Value;
                                styleInfo.Add($"颜色: #{color}");

                                if (!string.IsNullOrEmpty(expectedColor) &&
                                    !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 颜色不匹配
                                }
                            }
                        }

                        return (true, string.Join(", ", styleInfo));
                    }

                    return (true, "主要纵网格线可见");
                }
            }

            return (false, "主要纵网格线不可见");
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查图表的次要纵网格线
    /// </summary>
    private (bool HasGridlines, string Style) CheckMinorVerticalGridlines(ChartPart chartPart, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return (false, string.Empty);

            // 检查分类轴的次要网格线
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis> categoryAxes = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis>();
            foreach (DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis categoryAxis in categoryAxes)
            {
                DocumentFormat.OpenXml.Drawing.Charts.MinorGridlines? minorGridlines = categoryAxis.Elements<DocumentFormat.OpenXml.Drawing.Charts.MinorGridlines>().FirstOrDefault();
                if (minorGridlines != null)
                {
                    // 检查网格线样式
                    DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = minorGridlines.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
                    if (shapeProperties != null)
                    {
                        List<string> styleInfo = ["次要纵网格线可见"];

                        // 检查颜色
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                        if (outline != null)
                        {
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                            if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                            {
                                string color = solidFill.RgbColorModelHex.Val.Value;
                                styleInfo.Add($"颜色: #{color}");

                                if (!string.IsNullOrEmpty(expectedColor) &&
                                    !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 颜色不匹配
                                }
                            }
                        }

                        return (true, string.Join(", ", styleInfo));
                    }

                    return (true, "次要纵网格线可见");
                }
            }

            return (false, "次要纵网格线不可见");
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 获取图表图例位置
    /// </summary>
    private string GetLegendPosition(ChartPart chartPart)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.Legend? legend = chart?.Legend;

            if (legend?.LegendPosition?.Val?.Value != null)
            {
                DocumentFormat.OpenXml.Drawing.Charts.LegendPositionValues position = legend.LegendPosition.Val.Value;

                if (position == DocumentFormat.OpenXml.Drawing.Charts.LegendPositionValues.Bottom)
                    return "底部";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.LegendPositionValues.Top)
                    return "顶部";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.LegendPositionValues.Left)
                    return "左侧";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.LegendPositionValues.Right)
                    return "右侧";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.LegendPositionValues.TopRight)
                    return "右上角";
                else
                    return position.ToString();
            }

            // 如果有图例但没有明确位置，返回默认位置
            if (legend != null)
            {
                return "右侧"; // Excel默认图例位置
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取图表横坐标轴标题
    /// </summary>
    private string GetHorizontalAxisTitle(ChartPart chartPart)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            // 检查分类轴标题
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis> categoryAxes = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis>();
            foreach (DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis categoryAxis in categoryAxes)
            {
                DocumentFormat.OpenXml.Drawing.Charts.Title? title = categoryAxis.Title;
                if (title?.ChartText?.RichText != null)
                {
                    DocumentFormat.OpenXml.Drawing.Charts.RichText richText = title.ChartText.RichText;
                    IEnumerable<DocumentFormat.OpenXml.Drawing.Paragraph> paragraphs = richText.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>();

                    foreach (DocumentFormat.OpenXml.Drawing.Paragraph paragraph in paragraphs)
                    {
                        IEnumerable<DocumentFormat.OpenXml.Drawing.Run> runs = paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>();
                        foreach (DocumentFormat.OpenXml.Drawing.Run run in runs)
                        {
                            string? text = run.Text?.Text;
                            if (!string.IsNullOrEmpty(text))
                            {
                                return text;
                            }
                        }
                    }
                }

                // 如果有标题元素但没有文本，返回默认标题
                if (title != null)
                {
                    return "横坐标轴标题";
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取数据系列格式
    /// </summary>
    private string GetDataSeriesFormat(ChartPart chartPart, int seriesIndex, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            List<string> formatInfo = [];

            // 检查不同类型的图表系列
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.BarChart> barCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.LineChart> lineCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.PieChart> pieCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChart>();

            // 检查柱形图系列
            foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChart barChart in barCharts)
            {
                List<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries> series = barChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries>().ToList();
                string format = CheckSeriesFormat(series, seriesIndex, expectedColor);
                if (!string.IsNullOrEmpty(format))
                {
                    formatInfo.Add($"柱形图系列: {format}");
                }
            }

            // 检查折线图系列
            foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChart lineChart in lineCharts)
            {
                List<DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries> series = lineChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries>().ToList();
                string format = CheckSeriesFormat(series, seriesIndex, expectedColor);
                if (!string.IsNullOrEmpty(format))
                {
                    formatInfo.Add($"折线图系列: {format}");
                }
            }

            // 检查饼图系列
            foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChart pieChart in pieCharts)
            {
                List<DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries> series = pieChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries>().ToList();
                string format = CheckSeriesFormat(series, seriesIndex, expectedColor);
                if (!string.IsNullOrEmpty(format))
                {
                    formatInfo.Add($"饼图系列: {format}");
                }
            }

            return formatInfo.Count > 0 ? string.Join(", ", formatInfo) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 检查系列格式
    /// </summary>
    private string CheckSeriesFormat<T>(List<T> series, int seriesIndex, string expectedColor) where T : DocumentFormat.OpenXml.OpenXmlElement
    {
        try
        {
            if (series.Count == 0) return string.Empty;

            // 如果指定了具体系列索引且不是-1
            if (seriesIndex != -1)
            {
                if (seriesIndex < 1 || seriesIndex > series.Count)
                {
                    return string.Empty;
                }

                T targetSeries = series[seriesIndex - 1];
                return GetSeriesFormatInfo(targetSeries, expectedColor);
            }

            // -1 模式：检查所有系列
            foreach (T seriesItem in series)
            {
                string format = GetSeriesFormatInfo(seriesItem, expectedColor);
                if (!string.IsNullOrEmpty(format))
                {
                    return format;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取系列格式信息
    /// </summary>
    private string GetSeriesFormatInfo(DocumentFormat.OpenXml.OpenXmlElement series, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
            if (shapeProperties != null)
            {
                List<string> formatDetails = [];

                // 检查填充颜色
                DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                {
                    string color = solidFill.RgbColorModelHex.Val.Value;
                    formatDetails.Add($"颜色: #{color}");

                    if (!string.IsNullOrEmpty(expectedColor) &&
                        !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return string.Empty; // 颜色不匹配
                    }
                }

                // 检查边框
                DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                if (outline != null)
                {
                    formatDetails.Add("有边框");
                }

                return formatDetails.Count > 0 ? string.Join(", ", formatDetails) : "数据系列格式";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取数据标签信息
    /// </summary>
    private string GetDataLabelsInfo(ChartPart chartPart, string expectedPosition)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            List<string> labelInfo = [];

            // 检查不同类型的图表系列中的数据标签
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.BarChart> barCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.LineChart> lineCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.PieChart> pieCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChart>();

            // 检查柱形图系列的数据标签
            foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChart barChart in barCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries series in barChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.DataLabels? dataLabels = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabels>().FirstOrDefault();
                    if (dataLabels != null)
                    {
                        string position = GetDataLabelPosition(dataLabels);
                        if (string.IsNullOrEmpty(expectedPosition) ||
                            position.Contains(expectedPosition, StringComparison.OrdinalIgnoreCase))
                        {
                            labelInfo.Add($"柱形图数据标签: {position}");
                        }
                    }
                }
            }

            // 检查折线图系列的数据标签
            foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChart lineChart in lineCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries series in lineChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.DataLabels? dataLabels = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabels>().FirstOrDefault();
                    if (dataLabels != null)
                    {
                        string position = GetDataLabelPosition(dataLabels);
                        if (string.IsNullOrEmpty(expectedPosition) ||
                            position.Contains(expectedPosition, StringComparison.OrdinalIgnoreCase))
                        {
                            labelInfo.Add($"折线图数据标签: {position}");
                        }
                    }
                }
            }

            // 检查饼图系列的数据标签
            foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChart pieChart in pieCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries series in pieChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.DataLabels? dataLabels = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabels>().FirstOrDefault();
                    if (dataLabels != null)
                    {
                        string position = GetDataLabelPosition(dataLabels);
                        if (string.IsNullOrEmpty(expectedPosition) ||
                            position.Contains(expectedPosition, StringComparison.OrdinalIgnoreCase))
                        {
                            labelInfo.Add($"饼图数据标签: {position}");
                        }
                    }
                }
            }

            return labelInfo.Count > 0 ? string.Join(", ", labelInfo) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取数据标签位置
    /// </summary>
    private string GetDataLabelPosition(DocumentFormat.OpenXml.Drawing.Charts.DataLabels dataLabels)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.DataLabelPosition? dataLabelPosition = dataLabels.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabelPosition>().FirstOrDefault();
            if (dataLabelPosition?.Val?.Value != null)
            {
                DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues position = dataLabelPosition.Val.Value;

                if (position == DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues.Center)
                    return "居中";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues.InsideEnd)
                    return "内侧末端";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues.InsideBase)
                    return "内侧基部";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues.OutsideEnd)
                    return "外侧末端";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues.Left)
                    return "左侧";
                else if (position == DocumentFormat.OpenXml.Drawing.Charts.DataLabelPositionValues.Right)
                    return "右侧";
                else
                    return position.ToString();
            }

            // 如果有数据标签但没有明确位置，返回默认位置
            return "默认位置";
        }
        catch
        {
            return "数据标签";
        }
    }

    /// <summary>
    /// 获取数据标签格式
    /// </summary>
    private string GetDataLabelsFormat(ChartPart chartPart, string expectedFontName, string expectedFontSize, string expectedFontColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            List<string> formatInfo = [];

            // 检查不同类型的图表系列中的数据标签格式
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.BarChart> barCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.LineChart> lineCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChart>();
            IEnumerable<DocumentFormat.OpenXml.Drawing.Charts.PieChart> pieCharts = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChart>();

            // 检查柱形图系列的数据标签格式
            foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChart barChart in barCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries series in barChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.BarChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.DataLabels? dataLabels = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabels>().FirstOrDefault();
                    if (dataLabels != null)
                    {
                        string format = GetDataLabelFormatInfo(dataLabels, expectedFontName, expectedFontSize, expectedFontColor);
                        if (!string.IsNullOrEmpty(format))
                        {
                            formatInfo.Add($"柱形图数据标签格式: {format}");
                        }
                    }
                }
            }

            // 检查折线图系列的数据标签格式
            foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChart lineChart in lineCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries series in lineChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.LineChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.DataLabels? dataLabels = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabels>().FirstOrDefault();
                    if (dataLabels != null)
                    {
                        string format = GetDataLabelFormatInfo(dataLabels, expectedFontName, expectedFontSize, expectedFontColor);
                        if (!string.IsNullOrEmpty(format))
                        {
                            formatInfo.Add($"折线图数据标签格式: {format}");
                        }
                    }
                }
            }

            // 检查饼图系列的数据标签格式
            foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChart pieChart in pieCharts)
            {
                foreach (DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries series in pieChart.Elements<DocumentFormat.OpenXml.Drawing.Charts.PieChartSeries>())
                {
                    DocumentFormat.OpenXml.Drawing.Charts.DataLabels? dataLabels = series.Elements<DocumentFormat.OpenXml.Drawing.Charts.DataLabels>().FirstOrDefault();
                    if (dataLabels != null)
                    {
                        string format = GetDataLabelFormatInfo(dataLabels, expectedFontName, expectedFontSize, expectedFontColor);
                        if (!string.IsNullOrEmpty(format))
                        {
                            formatInfo.Add($"饼图数据标签格式: {format}");
                        }
                    }
                }
            }

            return formatInfo.Count > 0 ? string.Join(", ", formatInfo) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取数据标签格式信息
    /// </summary>
    private string GetDataLabelFormatInfo(DocumentFormat.OpenXml.Drawing.Charts.DataLabels dataLabels, string expectedFontName, string expectedFontSize, string expectedFontColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.TextProperties? textProperties = dataLabels.Elements<DocumentFormat.OpenXml.Drawing.Charts.TextProperties>().FirstOrDefault();
            if (textProperties != null)
            {
                List<string> formatDetails = [];

                // 检查字体属性
                DocumentFormat.OpenXml.Drawing.BodyProperties? bodyProperties = textProperties.Elements<DocumentFormat.OpenXml.Drawing.BodyProperties>().FirstOrDefault();
                DocumentFormat.OpenXml.Drawing.ListStyle? listStyle = textProperties.Elements<DocumentFormat.OpenXml.Drawing.ListStyle>().FirstOrDefault();
                IEnumerable<DocumentFormat.OpenXml.Drawing.Paragraph> paragraphs = textProperties.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>();

                foreach (DocumentFormat.OpenXml.Drawing.Paragraph paragraph in paragraphs)
                {
                    DocumentFormat.OpenXml.Drawing.ParagraphProperties? paragraphProperties = paragraph.Elements<DocumentFormat.OpenXml.Drawing.ParagraphProperties>().FirstOrDefault();
                    IEnumerable<DocumentFormat.OpenXml.Drawing.Run> runs = paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>();

                    foreach (DocumentFormat.OpenXml.Drawing.Run run in runs)
                    {
                        DocumentFormat.OpenXml.Drawing.RunProperties? runProperties = run.RunProperties;
                        if (runProperties != null)
                        {
                            // 检查字体名称
                            DocumentFormat.OpenXml.Drawing.LatinFont? latinFont = runProperties.Elements<DocumentFormat.OpenXml.Drawing.LatinFont>().FirstOrDefault();
                            if (latinFont?.Typeface?.Value != null)
                            {
                                string fontName = latinFont.Typeface.Value;
                                formatDetails.Add($"字体: {fontName}");

                                if (!string.IsNullOrEmpty(expectedFontName) &&
                                    !fontName.Contains(expectedFontName, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 字体不匹配
                                }
                            }

                            // 检查字体大小
                            if (runProperties.FontSize?.Value != null)
                            {
                                int fontSize = runProperties.FontSize.Value / 100;
                                formatDetails.Add($"大小: {fontSize}pt");

                                if (!string.IsNullOrEmpty(expectedFontSize) &&
                                    !fontSize.ToString().Contains(expectedFontSize))
                                {
                                    continue; // 字体大小不匹配
                                }
                            }

                            // 检查字体颜色
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = runProperties.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                            if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                            {
                                string color = solidFill.RgbColorModelHex.Val.Value;
                                formatDetails.Add($"颜色: #{color}");

                                if (!string.IsNullOrEmpty(expectedFontColor) &&
                                    !color.Contains(expectedFontColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // 颜色不匹配
                                }
                            }

                            if (formatDetails.Count > 0)
                            {
                                return string.Join(", ", formatDetails);
                            }
                        }
                    }
                }
            }

            // 如果有数据标签但没有找到格式信息，返回基本信息
            return "数据标签格式";
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取图表区域格式
    /// </summary>
    private string GetChartAreaFormat(ChartPart chartPart, string expectedFillColor, string expectedBorderColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace? chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            List<string> formatInfo = [];

            // 检查图表区域的形状属性
            DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
            if (shapeProperties != null)
            {
                // 检查填充颜色
                DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                {
                    string fillColor = solidFill.RgbColorModelHex.Val.Value;
                    formatInfo.Add($"填充颜色: #{fillColor}");

                    if (!string.IsNullOrEmpty(expectedFillColor) &&
                        !fillColor.Contains(expectedFillColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return string.Empty; // 填充颜色不匹配
                    }
                }

                // 检查边框颜色
                DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                if (outline != null)
                {
                    DocumentFormat.OpenXml.Drawing.SolidFill? outlineSolidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                    if (outlineSolidFill?.RgbColorModelHex?.Val?.Value != null)
                    {
                        string borderColor = outlineSolidFill.RgbColorModelHex.Val.Value;
                        formatInfo.Add($"边框颜色: #{borderColor}");

                        if (!string.IsNullOrEmpty(expectedBorderColor) &&
                            !borderColor.Contains(expectedBorderColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            return string.Empty; // 边框颜色不匹配
                        }
                    }

                    formatInfo.Add("有边框");
                }

                if (formatInfo.Count > 0)
                {
                    return string.Join(", ", formatInfo);
                }
            }

            // 检查图表空间的形状属性
            DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? chartSpaceShapeProperties = chartSpace?.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
            if (chartSpaceShapeProperties != null)
            {
                formatInfo.Add("图表区域有格式设置");
                return string.Join(", ", formatInfo);
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取图表基底颜色
    /// </summary>
    private string GetChartFloorColor(ChartPart chartPart, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            // 检查3D图表的基底（Floor）
            DocumentFormat.OpenXml.Drawing.Charts.Floor? floor = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.Floor>().FirstOrDefault();
            if (floor != null)
            {
                DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? shapeProperties = floor.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
                if (shapeProperties != null)
                {
                    // 检查填充颜色
                    DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = shapeProperties.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                    if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                    {
                        string color = solidFill.RgbColorModelHex.Val.Value;

                        if (string.IsNullOrEmpty(expectedColor) ||
                            color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            return $"基底颜色: #{color}";
                        }
                    }

                    // 如果有基底但没有颜色信息
                    if (string.IsNullOrEmpty(expectedColor))
                    {
                        return "图表基底存在";
                    }
                }
            }

            // 检查图表区域的背景颜色作为基底颜色
            DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? plotAreaShapeProperties = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
            if (plotAreaShapeProperties != null)
            {
                DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = plotAreaShapeProperties.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                {
                    string color = solidFill.RgbColorModelHex.Val.Value;

                    if (string.IsNullOrEmpty(expectedColor) ||
                        color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return $"图表区域背景颜色: #{color}";
                    }
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取图表边框信息
    /// </summary>
    private string GetChartBorder(ChartPart chartPart, string expectedStyle, string expectedColor)
    {
        try
        {
            DocumentFormat.OpenXml.Drawing.Charts.ChartSpace? chartSpace = chartPart.ChartSpace;
            DocumentFormat.OpenXml.Drawing.Charts.Chart? chart = chartSpace?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Chart>();
            DocumentFormat.OpenXml.Drawing.Charts.PlotArea? plotArea = chart?.PlotArea;

            if (plotArea == null) return string.Empty;

            List<string> borderInfo = [];

            // 检查图表区域的边框
            DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? plotAreaShapeProperties = plotArea.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
            if (plotAreaShapeProperties != null)
            {
                DocumentFormat.OpenXml.Drawing.Outline? outline = plotAreaShapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                if (outline != null)
                {
                    borderInfo.Add("图表区域有边框");

                    // 检查边框颜色
                    DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                    if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                    {
                        string color = solidFill.RgbColorModelHex.Val.Value;
                        borderInfo.Add($"边框颜色: #{color}");

                        if (!string.IsNullOrEmpty(expectedColor) &&
                            !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            return string.Empty; // 颜色不匹配
                        }
                    }

                    // 检查边框宽度
                    if (outline.Width?.Value != null)
                    {
                        int width = outline.Width.Value / 12700; // 转换为点
                        borderInfo.Add($"边框宽度: {width}pt");
                    }
                }
            }

            // 检查图表空间的边框
            DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties? chartSpaceShapeProperties = chartSpace?.Elements<DocumentFormat.OpenXml.Drawing.Charts.ChartShapeProperties>().FirstOrDefault();
            if (chartSpaceShapeProperties != null)
            {
                DocumentFormat.OpenXml.Drawing.Outline? outline = chartSpaceShapeProperties.Elements<DocumentFormat.OpenXml.Drawing.Outline>().FirstOrDefault();
                if (outline != null)
                {
                    if (borderInfo.Count == 0)
                    {
                        borderInfo.Add("图表有边框");
                    }

                    // 检查边框颜色
                    DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                    if (solidFill?.RgbColorModelHex?.Val?.Value != null)
                    {
                        string color = solidFill.RgbColorModelHex.Val.Value;
                        if (!borderInfo.Any(info => info.Contains("边框颜色")))
                        {
                            borderInfo.Add($"边框颜色: #{color}");
                        }

                        if (!string.IsNullOrEmpty(expectedColor) &&
                            !color.Contains(expectedColor.Replace("#", ""), StringComparison.OrdinalIgnoreCase))
                        {
                            return string.Empty; // 颜色不匹配
                        }
                    }
                }
            }

            return borderInfo.Count > 0 ? string.Join(", ", borderInfo) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取条件格式详细信息
    /// </summary>
    private string GetConditionalFormattingDetails(WorksheetPart worksheetPart, string expectedType, string expectedRange)
    {
        try
        {
            IEnumerable<ConditionalFormatting> conditionalFormattings = worksheetPart.Worksheet.Elements<ConditionalFormatting>();
            List<string> formatDetails = [];

            foreach (ConditionalFormatting conditionalFormatting in conditionalFormattings)
            {
                // 检查范围
                string range = conditionalFormatting.SequenceOfReferences?.InnerText ?? "";
                if (!string.IsNullOrEmpty(expectedRange) &&
                    !range.Contains(expectedRange, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 范围不匹配
                }

                // 检查条件格式规则
                IEnumerable<ConditionalFormattingRule> conditionalFormattingRules = conditionalFormatting.Elements<ConditionalFormattingRule>();
                foreach (ConditionalFormattingRule rule in conditionalFormattingRules)
                {
                    List<string> ruleDetails = [];

                    // 添加范围信息
                    if (!string.IsNullOrEmpty(range))
                    {
                        ruleDetails.Add($"范围: {range}");
                    }

                    // 检查条件格式类型
                    if (rule.Type?.Value != null)
                    {
                        string ruleType = rule.Type.Value.ToString();
                        ruleDetails.Add($"类型: {ruleType}");

                        if (!string.IsNullOrEmpty(expectedType) &&
                            !ruleType.Contains(expectedType, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // 类型不匹配
                        }
                    }

                    // 检查优先级
                    if (rule.Priority?.Value != null)
                    {
                        ruleDetails.Add($"优先级: {rule.Priority.Value}");
                    }

                    // 检查格式
                    if (rule.FormatId?.Value != null)
                    {
                        ruleDetails.Add($"格式ID: {rule.FormatId.Value}");
                    }

                    // 检查公式
                    IEnumerable<Formula> formulas = rule.Elements<Formula>();
                    foreach (Formula formula in formulas)
                    {
                        if (!string.IsNullOrEmpty(formula.Text))
                        {
                            ruleDetails.Add($"公式: {formula.Text}");
                        }
                    }

                    if (ruleDetails.Count > 0)
                    {
                        formatDetails.Add(string.Join(", ", ruleDetails));
                    }
                }
            }

            return formatDetails.Count > 0 ? string.Join("; ", formatDetails) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取数据验证详细信息
    /// </summary>
    private string GetDataValidationDetails(WorksheetPart worksheetPart, string expectedType, string expectedRange)
    {
        try
        {
            DataValidations? dataValidations = worksheetPart.Worksheet.Elements<DataValidations>().FirstOrDefault();
            if (dataValidations?.HasChildren != true)
            {
                return string.Empty;
            }

            List<string> validationDetails = [];

            foreach (DataValidation dataValidation in dataValidations.Elements<DataValidation>())
            {
                List<string> validationInfo = [];

                // 检查范围
                string range = dataValidation.SequenceOfReferences?.InnerText ?? "";
                if (!string.IsNullOrEmpty(expectedRange) &&
                    !range.Contains(expectedRange, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 范围不匹配
                }

                if (!string.IsNullOrEmpty(range))
                {
                    validationInfo.Add($"范围: {range}");
                }

                // 检查验证类型
                if (dataValidation.Type?.Value != null)
                {
                    string validationType = dataValidation.Type.Value.ToString();
                    validationInfo.Add($"类型: {validationType}");

                    if (!string.IsNullOrEmpty(expectedType) &&
                        !validationType.Contains(expectedType, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // 类型不匹配
                    }
                }

                // 检查操作符
                if (dataValidation.Operator?.Value != null)
                {
                    validationInfo.Add($"操作符: {dataValidation.Operator.Value}");
                }

                // 检查公式1
                if (!string.IsNullOrEmpty(dataValidation.Formula1?.Text))
                {
                    validationInfo.Add($"公式1: {dataValidation.Formula1.Text}");
                }

                // 检查公式2
                if (!string.IsNullOrEmpty(dataValidation.Formula2?.Text))
                {
                    validationInfo.Add($"公式2: {dataValidation.Formula2.Text}");
                }

                // 检查提示信息
                if (!string.IsNullOrEmpty(dataValidation.Prompt?.Value))
                {
                    validationInfo.Add($"提示: {dataValidation.Prompt.Value}");
                }

                // 检查错误信息
                if (!string.IsNullOrEmpty(dataValidation.Error?.Value))
                {
                    validationInfo.Add($"错误信息: {dataValidation.Error.Value}");
                }

                if (validationInfo.Count > 0)
                {
                    validationDetails.Add(string.Join(", ", validationInfo));
                }
            }

            return validationDetails.Count > 0 ? string.Join("; ", validationDetails) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取内置数字格式
    /// </summary>
    private string GetBuiltInNumberFormat(uint formatId)
    {
        return formatId switch
        {
            0 => "常规",
            1 => "0",
            2 => "0.00",
            3 => "#,##0",
            4 => "#,##0.00",
            9 => "0%",
            10 => "0.00%",
            11 => "0.00E+00",
            12 => "# ?/?",
            13 => "# ??/??",
            14 => "m/d/yy",
            15 => "d-mmm-yy",
            16 => "d-mmm",
            17 => "mmm-yy",
            18 => "h:mm AM/PM",
            19 => "h:mm:ss AM/PM",
            20 => "h:mm",
            21 => "h:mm:ss",
            22 => "m/d/yy h:mm",
            37 => "#,##0 ;(#,##0)",
            38 => "#,##0 ;[Red](#,##0)",
            39 => "#,##0.00;(#,##0.00)",
            40 => "#,##0.00;[Red](#,##0.00)",
            45 => "mm:ss",
            46 => "[h]:mm:ss",
            47 => "mmss.0",
            48 => "##0.0E+0",
            49 => "@",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 获取页面设置详细信息
    /// </summary>
    private string GetPageSetupDetails(WorksheetPart worksheetPart, string expectedOrientation, string expectedPaperSize)
    {
        try
        {
            PageSetup? pageSetup = worksheetPart.Worksheet.GetFirstChild<PageSetup>();
            if (pageSetup == null)
            {
                return string.Empty;
            }

            List<string> setupDetails = [];

            // 检查页面方向
            if (pageSetup.Orientation?.Value != null)
            {
                string orientation = pageSetup.Orientation.Value.ToString();
                string orientationText = orientation switch
                {
                    "Portrait" => "纵向",
                    "Landscape" => "横向",
                    _ => orientation
                };
                setupDetails.Add($"方向: {orientationText}");

                if (!string.IsNullOrEmpty(expectedOrientation) &&
                    !orientationText.Contains(expectedOrientation, StringComparison.OrdinalIgnoreCase) &&
                    !orientation.Contains(expectedOrientation, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty; // 方向不匹配
                }
            }

            // 检查纸张大小
            if (pageSetup.PaperSize?.Value != null)
            {
                uint paperSizeValue = pageSetup.PaperSize.Value;
                string paperSizeText = GetPaperSizeName(paperSizeValue);
                setupDetails.Add($"纸张: {paperSizeText}");

                if (!string.IsNullOrEmpty(expectedPaperSize) &&
                    !paperSizeText.Contains(expectedPaperSize, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty; // 纸张大小不匹配
                }
            }

            // 检查其他设置
            if (pageSetup.FitToWidth?.Value != null)
            {
                setupDetails.Add($"适合宽度: {pageSetup.FitToWidth.Value}页");
            }
            if (pageSetup.FitToHeight?.Value != null)
            {
                setupDetails.Add($"适合高度: {pageSetup.FitToHeight.Value}页");
            }

            // 检查缩放
            if (pageSetup.Scale?.Value != null)
            {
                setupDetails.Add($"缩放: {pageSetup.Scale.Value}%");
            }

            return setupDetails.Count > 0 ? string.Join(", ", setupDetails) : "页面设置存在";
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取纸张大小名称
    /// </summary>
    private string GetPaperSizeName(uint paperSize)
    {
        return paperSize switch
        {
            1 => "Letter (8.5 x 11 in)",
            5 => "Legal (8.5 x 14 in)",
            8 => "A3 (297 x 420 mm)",
            9 => "A4 (210 x 297 mm)",
            11 => "A5 (148 x 210 mm)",
            13 => "B4 (250 x 354 mm)",
            17 => "B5 (182 x 257 mm)",
            20 => "Envelope #10 (4.125 x 9.5 in)",
            27 => "Envelope DL (110 x 220 mm)",
            28 => "Envelope C5 (162 x 229 mm)",
            34 => "Envelope B5 (176 x 250 mm)",
            37 => "Envelope Monarch (3.875 x 7.5 in)",
            _ => $"纸张大小 {paperSize}"
        };
    }

    /// <summary>
    /// 获取页眉页脚详细信息
    /// </summary>
    private string GetHeaderFooterDetails(WorksheetPart worksheetPart, string expectedContent)
    {
        try
        {
            HeaderFooter? headerFooter = worksheetPart.Worksheet.GetFirstChild<HeaderFooter>();
            if (headerFooter?.HasChildren != true)
            {
                return string.Empty;
            }

            List<string> headerFooterDetails = [];

            // 检查奇数页页眉
            if (headerFooter.OddHeader?.Text != null)
            {
                string oddHeader = headerFooter.OddHeader.Text;
                if (string.IsNullOrEmpty(expectedContent) ||
                    oddHeader.Contains(expectedContent, StringComparison.OrdinalIgnoreCase))
                {
                    headerFooterDetails.Add($"奇数页页眉: {oddHeader}");
                }
            }

            // 检查奇数页页脚
            if (headerFooter.OddFooter?.Text != null)
            {
                string oddFooter = headerFooter.OddFooter.Text;
                if (string.IsNullOrEmpty(expectedContent) ||
                    oddFooter.Contains(expectedContent, StringComparison.OrdinalIgnoreCase))
                {
                    headerFooterDetails.Add($"奇数页页脚: {oddFooter}");
                }
            }

            // 检查偶数页页眉
            if (headerFooter.EvenHeader?.Text != null)
            {
                string evenHeader = headerFooter.EvenHeader.Text;
                if (string.IsNullOrEmpty(expectedContent) ||
                    evenHeader.Contains(expectedContent, StringComparison.OrdinalIgnoreCase))
                {
                    headerFooterDetails.Add($"偶数页页眉: {evenHeader}");
                }
            }

            // 检查偶数页页脚
            if (headerFooter.EvenFooter?.Text != null)
            {
                string evenFooter = headerFooter.EvenFooter.Text;
                if (string.IsNullOrEmpty(expectedContent) ||
                    evenFooter.Contains(expectedContent, StringComparison.OrdinalIgnoreCase))
                {
                    headerFooterDetails.Add($"偶数页页脚: {evenFooter}");
                }
            }

            // 检查首页页眉
            if (headerFooter.FirstHeader?.Text != null)
            {
                string firstHeader = headerFooter.FirstHeader.Text;
                if (string.IsNullOrEmpty(expectedContent) ||
                    firstHeader.Contains(expectedContent, StringComparison.OrdinalIgnoreCase))
                {
                    headerFooterDetails.Add($"首页页眉: {firstHeader}");
                }
            }

            // 检查首页页脚
            if (headerFooter.FirstFooter?.Text != null)
            {
                string firstFooter = headerFooter.FirstFooter.Text;
                if (string.IsNullOrEmpty(expectedContent) ||
                    firstFooter.Contains(expectedContent, StringComparison.OrdinalIgnoreCase))
                {
                    headerFooterDetails.Add($"首页页脚: {firstFooter}");
                }
            }

            return headerFooterDetails.Count > 0 ? string.Join("; ", headerFooterDetails) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取冻结窗格详细信息
    /// </summary>
    private string GetFreezePanesDetails(WorksheetPart worksheetPart)
    {
        try
        {
            SheetViews? sheetViews = worksheetPart.Worksheet.GetFirstChild<SheetViews>();
            if (sheetViews?.HasChildren == true)
            {
                foreach (SheetView sheetView in sheetViews.Elements<SheetView>())
                {
                    Pane? pane = sheetView.Pane;
                    if (pane?.State?.Value == DocumentFormat.OpenXml.Spreadsheet.PaneStateValues.Frozen)
                    {
                        List<string> freezeInfo = ["冻结窗格"];

                        // 检查冻结位置
                        if (pane.TopLeftCell?.Value != null)
                        {
                            freezeInfo.Add($"冻结位置: {pane.TopLeftCell.Value}");
                        }

                        // 检查水平分割位置
                        if (pane.HorizontalSplit?.Value != null)
                        {
                            freezeInfo.Add($"水平分割: {pane.HorizontalSplit.Value}行");
                        }

                        // 检查垂直分割位置
                        if (pane.VerticalSplit?.Value != null)
                        {
                            freezeInfo.Add($"垂直分割: {pane.VerticalSplit.Value}列");
                        }

                        return string.Join(", ", freezeInfo);
                    }
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取工作表保护详细信息
    /// </summary>
    private string GetWorksheetProtectionDetails(WorksheetPart worksheetPart)
    {
        try
        {
            SheetProtection? sheetProtection = worksheetPart.Worksheet.GetFirstChild<SheetProtection>();
            if (sheetProtection == null)
            {
                return string.Empty;
            }

            List<string> protectionInfo = ["工作表保护"];

            // 检查密码保护
            if (sheetProtection.Password?.Value != null)
            {
                protectionInfo.Add("密码保护");
            }

            // 检查保护选项
            if (sheetProtection.Sheet?.Value == true)
            {
                protectionInfo.Add("保护工作表");
            }

            if (sheetProtection.Objects?.Value == true)
            {
                protectionInfo.Add("保护对象");
            }

            if (sheetProtection.Scenarios?.Value == true)
            {
                protectionInfo.Add("保护方案");
            }

            if (sheetProtection.FormatCells?.Value == false)
            {
                protectionInfo.Add("允许格式化单元格");
            }

            if (sheetProtection.FormatColumns?.Value == false)
            {
                protectionInfo.Add("允许格式化列");
            }

            if (sheetProtection.FormatRows?.Value == false)
            {
                protectionInfo.Add("允许格式化行");
            }

            if (sheetProtection.InsertColumns?.Value == false)
            {
                protectionInfo.Add("允许插入列");
            }

            if (sheetProtection.InsertRows?.Value == false)
            {
                protectionInfo.Add("允许插入行");
            }

            if (sheetProtection.InsertHyperlinks?.Value == false)
            {
                protectionInfo.Add("允许插入超链接");
            }

            if (sheetProtection.DeleteColumns?.Value == false)
            {
                protectionInfo.Add("允许删除列");
            }

            if (sheetProtection.DeleteRows?.Value == false)
            {
                protectionInfo.Add("允许删除行");
            }

            if (sheetProtection.SelectLockedCells?.Value == false)
            {
                protectionInfo.Add("禁止选择锁定单元格");
            }

            if (sheetProtection.Sort?.Value == false)
            {
                protectionInfo.Add("允许排序");
            }

            if (sheetProtection.AutoFilter?.Value == false)
            {
                protectionInfo.Add("允许自动筛选");
            }

            if (sheetProtection.PivotTables?.Value == false)
            {
                protectionInfo.Add("允许数据透视表");
            }

            return string.Join(", ", protectionInfo);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Excel范围匹配检查
    /// </summary>
    private bool IsSimpleRangeMatch(string cellRef, string startCell, string endCell)
    {
        try
        {
            if (string.IsNullOrEmpty(cellRef) || string.IsNullOrEmpty(startCell) || string.IsNullOrEmpty(endCell))
                return false;

            // 详细实现：解析Excel单元格引用并进行精确的范围匹配
            var cellPos = ParseExcelCellReference(cellRef);
            var startPos = ParseExcelCellReference(startCell);
            var endPos = ParseExcelCellReference(endCell);

            if (cellPos.HasValue && startPos.HasValue && endPos.HasValue)
            {
                var (cellRow, cellCol) = cellPos.Value;
                var (startRow, startCol) = startPos.Value;
                var (endRow, endCol) = endPos.Value;

                return cellRow >= startRow && cellRow <= endRow &&
                       cellCol >= startCol && cellCol <= endCol;
            }

            // 回退到字符串比较（用于非标准格式）
            return string.Compare(cellRef, startCell, StringComparison.OrdinalIgnoreCase) >= 0 &&
                   string.Compare(cellRef, endCell, StringComparison.OrdinalIgnoreCase) <= 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析Excel单元格引用（如A1, B10等）
    /// </summary>
    private (int Row, int Column)? ParseExcelCellReference(string cellRef)
    {
        try
        {
            if (string.IsNullOrEmpty(cellRef))
                return null;

            // 分离字母和数字部分
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
                i++;

            if (i == 0 || i == cellRef.Length)
                return null;

            string columnPart = cellRef.Substring(0, i);
            string rowPart = cellRef.Substring(i);

            if (!int.TryParse(rowPart, out int row))
                return null;

            // 将列字母转换为数字（A=1, B=2, ..., Z=26, AA=27等）
            int column = 0;
            for (int j = 0; j < columnPart.Length; j++)
            {
                column = column * 26 + (char.ToUpper(columnPart[j]) - 'A' + 1);
            }

            return (row, column);
        }
        catch
        {
            return null;
        }
    }
}
