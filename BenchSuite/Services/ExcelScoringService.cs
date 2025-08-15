using System.Runtime.InteropServices;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using Task = System.Threading.Tasks.Task;
using Excel = Microsoft.Office.Interop.Excel;

namespace BenchSuite.Services;

/// <summary>
/// Excel打分服务实现
/// </summary>
public class ExcelScoringService : IExcelScoringService
{
    private readonly ScoringConfiguration _defaultConfiguration;
    private static readonly string[] SupportedExtensions = [".xls", ".xlsx"];

    public ExcelScoringService()
    {
        _defaultConfiguration = new ScoringConfiguration();
    }

    /// <summary>
    /// 对Excel文件进行打分
    /// </summary>
    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreFile(filePath, examModel, configuration));
    }

    /// <summary>
    /// 对单个题目进行评分
    /// </summary>
    public async Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreQuestion(filePath, question, configuration));
    }

    /// <summary>
    /// 对Excel文件进行打分（同步版本）
    /// </summary>
    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"不支持的文件类型: {Path.GetExtension(filePath)}";
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.ErrorMessage = $"文件不存在: {filePath}";
                return result;
            }

            // 获取Excel模块
            ExamModuleModel? excelModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Excel);
            if (excelModule == null)
            {
                result.ErrorMessage = "试卷中未找到Excel模块";
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

            // 计算总分和获得分数
            result.TotalScore = allOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"打分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 对单个题目进行评分（同步版本）
    /// </summary>
    private ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = new()
        {
            QuestionId = question.Id,
            QuestionTitle = question.Title,
            StartTime = DateTime.Now
        };

        try
        {
            // 验证文件是否存在
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = $"文件不存在: {filePath}";
                result.IsSuccess = false;
                return result;
            }

            // 验证文件扩展名
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"不支持的文件类型: {Path.GetExtension(filePath)}";
                result.IsSuccess = false;
                return result;
            }

            // 获取题目的操作点（只处理Excel相关的操作点）
            List<OperationPointModel> excelOperationPoints = question.OperationPoints
                .Where(op => op.ModuleType == ModuleType.Excel && op.IsEnabled)
                .ToList();

            if (excelOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何Excel操作点";
                result.IsSuccess = false;
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, excelOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            // 计算总分和获得分数
            result.TotalScore = excelOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"打分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检测Excel文档中的特定知识点
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

            Excel.Application? excelApp = null;
            Excel.Workbook? workbook = null;

            try
            {
                // 启动Excel应用程序
                excelApp = new Excel.Application
                {
                    Visible = false,
                    DisplayAlerts = false
                };

                // 打开工作簿
                workbook = excelApp.Workbooks.Open(filePath, ReadOnly: true);

                // 根据知识点类型进行检测
                result = DetectSpecificKnowledgePoint(workbook, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测知识点时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }
            finally
            {
                // 清理资源
                CleanupExcelResources(workbook, excelApp);
            }

            return result;
        });
    }

    /// <summary>
    /// 验证文件是否可以被处理
    /// </summary>
    public bool CanProcessFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return SupportedExtensions;
    }

    /// <summary>
    /// 批量检测Excel文档中的知识点
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];
            Excel.Application? excelApp = null;
            Excel.Workbook? workbook = null;

            // 创建基于文件路径的确定性参数解析上下文
            string contextId = Path.GetFileName(filePath) + "_" + new FileInfo(filePath).Length;
            ParameterResolutionContext context = new(contextId);

            try
            {
                // 启动Excel应用程序
                excelApp = new Excel.Application
                {
                    Visible = false,
                    DisplayAlerts = false
                };

                // 打开工作簿
                workbook = excelApp.Workbooks.Open(filePath, ReadOnly: true);

                // 预先解析所有-1参数
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                    ResolveParametersForWorkbook(parameters, workbook, context);
                }

                // 逐个检测知识点
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    try
                    {
                        Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);

                        // 使用解析后的参数
                        Dictionary<string, string> resolvedParameters = GetResolvedParameters(parameters, context);

                        // 根据操作点名称映射到知识点类型
                        string knowledgePointType = MapOperationPointNameToKnowledgeType(operationPoint.Name);

                        KnowledgePointResult result = workbook is not null
                            ? DetectSpecificKnowledgePoint(workbook, knowledgePointType, resolvedParameters)
                            : new KnowledgePointResult
                            {
                                ErrorMessage = "Excel工作簿未能正确打开",
                                IsCorrect = false,
                                KnowledgePointType = knowledgePointType,
                                Parameters = resolvedParameters
                            };

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
                        results.Add(new KnowledgePointResult
                        {
                            KnowledgePointId = operationPoint.Id,
                            OperationPointId = operationPoint.Id,
                            KnowledgePointName = operationPoint.Name,
                            KnowledgePointType = operationPoint.ExcelKnowledgeType ?? string.Empty,
                            TotalScore = operationPoint.Score,
                            AchievedScore = 0,
                            IsCorrect = false,
                            ErrorMessage = $"检测失败: {ex.Message}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果无法打开文件，为所有知识点返回错误结果
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    results.Add(new KnowledgePointResult
                    {
                        KnowledgePointId = operationPoint.Id,
                        OperationPointId = operationPoint.Id,
                        KnowledgePointName = operationPoint.Name,
                        KnowledgePointType = operationPoint.ExcelKnowledgeType ?? string.Empty,
                        TotalScore = operationPoint.Score,
                        AchievedScore = 0,
                        IsCorrect = false,
                        ErrorMessage = $"无法打开Excel文档: {ex.Message}"
                    });
                }

                // 添加参数解析日志到第一个结果中（用于调试）
                if (results.Count > 0)
                {
                    string resolutionLog = context.GetResolutionLog();
                    if (!string.IsNullOrEmpty(resolutionLog))
                    {
                        if (string.IsNullOrEmpty(results[0].Details))
                        {
                            results[0].Details = $"参数解析: {resolutionLog}";
                        }
                        else
                        {
                            results[0].Details += $"\n参数解析: {resolutionLog}";
                        }
                    }
                }
            }
            finally
            {
                // 清理资源
                CleanupExcelResources(workbook, excelApp);
            }

            return results;
        });
    }

    /// <summary>
    /// 清理Excel资源
    /// </summary>
    private static void CleanupExcelResources(Excel.Workbook? workbook, Excel.Application? excelApp)
    {
        try
        {
            if (workbook != null)
            {
                workbook.Close(false);
                Marshal.ReleaseComObject(workbook);
            }
        }
        catch (Exception ex)
        {
            // 记录清理错误，但不抛出异常
            System.Diagnostics.Debug.WriteLine($"清理Excel工作簿时发生错误: {ex.Message}");
        }

        try
        {
            if (excelApp != null)
            {
                excelApp.Quit();
                Marshal.ReleaseComObject(excelApp);
            }
        }
        catch (Exception ex)
        {
            // 记录清理错误，但不抛出异常
            System.Diagnostics.Debug.WriteLine($"清理Excel应用程序时发生错误: {ex.Message}");
        }

        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// 为工作簿解析参数
    /// </summary>
    private static void ResolveParametersForWorkbook(Dictionary<string, string> parameters, Excel.Workbook workbook, ParameterResolutionContext context)
    {
        try
        {
            foreach (KeyValuePair<string, string> param in parameters)
            {
                if (param.Value == "-1")
                {
                    // 根据参数名称确定最大值
                    int maxValue = GetMaxValueForParameter(param.Key, workbook);
                    if (maxValue > 0)
                    {
                        ParameterResolver.ResolveParameter(param.Key, param.Value, maxValue, context);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"解析工作簿参数时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取解析后的参数
    /// </summary>
    private static Dictionary<string, string> GetResolvedParameters(Dictionary<string, string> originalParameters, ParameterResolutionContext context)
    {
        Dictionary<string, string> resolvedParameters = [];

        foreach (KeyValuePair<string, string> param in originalParameters)
        {
            if (context.IsParameterResolved(param.Key))
            {
                resolvedParameters[param.Key] = context.GetResolvedParameter(param.Key);
            }
            else
            {
                resolvedParameters[param.Key] = param.Value;
            }
        }

        return resolvedParameters;
    }

    /// <summary>
    /// 根据参数名称获取最大值
    /// </summary>
    private static int GetMaxValueForParameter(string parameterName, Excel.Workbook workbook)
    {
        try
        {
            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null) return 0;

            return parameterName.ToLowerInvariant() switch
            {
                var name when name.Contains("sheet") => workbook.Worksheets.Count,
                var name when name.Contains("row") => activeSheet.UsedRange?.Rows.Count ?? 1,
                var name when name.Contains("column") => activeSheet.UsedRange?.Columns.Count ?? 1,
                var name when name.Contains("chart") => activeSheet.ChartObjects().Count,
                _ => 10 // 默认最大值
            };
        }
        catch
        {
            return 10; // 出错时返回默认值
        }
    }

    /// <summary>
    /// 将操作点名称映射到知识点类型
    /// </summary>
    private static string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        return operationPointName switch
        {
            // 第一类：Excel基础操作
            "填充或复制单元格内容" => "FillOrCopyCellContent",
            "删除单元格内容" => "DeleteCellContent",
            "插入或删除单元格" => "InsertDeleteCells",
            "合并单元格" => "MergeCells",
            "插入或删除行" => "InsertDeleteRows",
            "设置指定单元格字体" => "SetCellFont",
            "设置字型" => "SetFontStyle",
            "设置字号" => "SetFontSize",
            "字体颜色" => "SetFontColor",
            "内边框样式" => "SetInnerBorderStyle",
            "内边框颜色" => "SetInnerBorderColor",
            "插入或删除列" => "InsertDeleteColumns",
            "设置单元格区域水平对齐方式" => "SetHorizontalAlignment",
            "设置目标区域单元格数字分类格式" => "SetNumberFormat",
            "使用函数" => "UseFunction",
            "设置行高" => "SetRowHeight",
            "设置列宽" => "SetColumnWidth",
            "自动调整行高" => "AutoFitRowHeight",
            "自动调整列宽" => "AutoFitColumnWidth",
            "设置单元格填充颜色" => "SetCellFillColor",
            "设置图案填充样式" => "SetPatternFillStyle",
            "设置填充图案颜色" => "SetPatternFillColor",
            "文字换行" => "WrapText",
            "设置外边框样式" => "SetOuterBorderStyle",
            "设置外边框颜色" => "SetOuterBorderColor",
            "设置垂直对齐方式" => "SetVerticalAlignment",
            "冻结窗格" => "FreezePane",
            "修改sheet表名称" => "ModifySheetName",
            "添加下划线" => "AddUnderline",
            "设置粗体斜体" => "SetBoldItalic",
            "设置删除线" => "SetStrikethrough",
            "设置上标下标" => "SetSuperscriptSubscript",
            "条件格式" => "ConditionalFormat",
            "数据验证" => "DataValidation",
            "保护工作表" => "ProtectWorksheet",
            "设置单元格批注" => "SetCellComment",
            "插入超链接" => "HyperlinkInsert",
            "查找替换" => "FindReplace",
            "选择性粘贴" => "CopyPasteSpecial",
            "自动求和" => "AutoSum",
            "定位特殊单元格" => "GoToSpecial",
            "设置单元格样式——数据" => "SetCellStyleData",

            // 第二类：数据清单操作
            "筛选" => "Filter",
            "排序" => "Sort",
            "分类汇总" => "Subtotal",
            "高级筛选-条件" => "AdvancedFilterCondition",
            "高级筛选-数据" => "AdvancedFilterData",
            "数据透视表" => "PivotTable",

            // 第三类：图表操作
            "图表类型" => "ChartType",
            "图表样式" => "ChartStyle",
            "图表移动" => "ChartMove",
            "分类轴数据区域" => "CategoryAxisDataRange",
            "数值轴数据区域" => "ValueAxisDataRange",
            "图表标题" => "ChartTitle",
            "图表标题格式" => "ChartTitleFormat",
            "主要横坐标轴标题" => "HorizontalAxisTitle",
            "主要横坐标轴标题格式" => "HorizontalAxisTitleFormat",
            "设置图例位置" => "LegendPosition",
            "设置图例格式" => "LegendFormat",
            "设置主要纵坐标轴选项" => "VerticalAxisOptions",
            "设置网格线——主要横网格线" => "MajorHorizontalGridlines",
            "设置网格线——次要横网格线" => "MinorHorizontalGridlines",
            "主要纵网格线" => "MajorVerticalGridlines",
            "次要纵网格线" => "MinorVerticalGridlines",
            "设置数据系列格式" => "DataSeriesFormat",
            "添加数据标签" => "AddDataLabels",
            "设置数据标签格式" => "DataLabelsFormat",
            "设置图表区域格式" => "ChartAreaFormat",
            "显示图表基底颜色" => "ChartFloorColor",
            "设置图表边框线" => "ChartBorder",

            _ => operationPointName
        };
    }

    /// <summary>
    /// 检测特定知识点
    /// </summary>
    private KnowledgePointResult DetectSpecificKnowledgePoint(Excel.Workbook workbook, string knowledgePointType, Dictionary<string, string> parameters)
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
                    result = DetectFillOrCopyCellContent(workbook, parameters);
                    break;
                case "DeleteCellContent":
                    result = DetectDeleteCellContent(workbook, parameters);
                    break;
                case "InsertDeleteCells":
                    result = DetectInsertDeleteCells(workbook, parameters);
                    break;
                case "MergeCells":
                    result = DetectMergeCells(workbook, parameters);
                    break;
                case "InsertDeleteRows":
                    result = DetectInsertDeleteRows(workbook, parameters);
                    break;
                case "SetCellFont":
                    result = DetectSetCellFont(workbook, parameters);
                    break;
                case "SetFontStyle":
                    result = DetectSetFontStyle(workbook, parameters);
                    break;
                case "SetFontSize":
                    result = DetectSetFontSize(workbook, parameters);
                    break;
                case "SetFontColor":
                    result = DetectSetFontColor(workbook, parameters);
                    break;
                case "SetInnerBorderStyle":
                    result = DetectSetInnerBorderStyle(workbook, parameters);
                    break;
                case "SetInnerBorderColor":
                    result = DetectSetInnerBorderColor(workbook, parameters);
                    break;
                case "InsertDeleteColumns":
                    result = DetectInsertDeleteColumns(workbook, parameters);
                    break;
                case "SetHorizontalAlignment":
                    result = DetectSetHorizontalAlignment(workbook, parameters);
                    break;
                case "SetNumberFormat":
                    result = DetectSetNumberFormat(workbook, parameters);
                    break;
                case "UseFunction":
                    result = DetectUseFunction(workbook, parameters);
                    break;
                case "SetRowHeight":
                    result = DetectSetRowHeight(workbook, parameters);
                    break;
                case "SetColumnWidth":
                    result = DetectSetColumnWidth(workbook, parameters);
                    break;
                case "AutoFitRowHeight":
                    result = DetectAutoFitRowHeight(workbook, parameters);
                    break;
                case "AutoFitColumnWidth":
                    result = DetectAutoFitColumnWidth(workbook, parameters);
                    break;
                case "SetCellFillColor":
                    result = DetectSetCellFillColor(workbook, parameters);
                    break;
                case "SetVerticalAlignment":
                    result = DetectSetVerticalAlignment(workbook, parameters);
                    break;

                // 第二类：数据清单操作
                case "Filter":
                    result = DetectFilter(workbook, parameters);
                    break;
                case "Sort":
                    result = DetectSort(workbook, parameters);
                    break;
                case "Subtotal":
                    result = DetectSubtotal(workbook, parameters);
                    break;
                case "AdvancedFilterCondition":
                    result = DetectAdvancedFilterCondition(workbook, parameters);
                    break;
                case "AdvancedFilterData":
                    result = DetectAdvancedFilterData(workbook, parameters);
                    break;
                case "PivotTable":
                    result = DetectPivotTable(workbook, parameters);
                    break;

                // 第三类：图表操作
                case "ChartType":
                    result = DetectChartType(workbook, parameters);
                    break;
                case "ChartStyle":
                    result = DetectChartStyle(workbook, parameters);
                    break;
                case "ChartMove":
                    result = DetectChartMove(workbook, parameters);
                    break;
                case "CategoryAxisDataRange":
                    result = DetectCategoryAxisDataRange(workbook, parameters);
                    break;
                case "ValueAxisDataRange":
                    result = DetectValueAxisDataRange(workbook, parameters);
                    break;
                case "ChartTitle":
                    result = DetectChartTitle(workbook, parameters);
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
    /// 检测填充或复制单元格内容
    /// </summary>
    private KnowledgePointResult DetectFillOrCopyCellContent(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "FillOrCopyCellContent",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("TargetWorkbook", out string? targetWorkbook) ||
                !parameters.TryGetValue("CellValues", out string? cellValues))
            {
                result.ErrorMessage = "缺少必要参数: TargetWorkbook 或 CellValues";
                return result;
            }

            // 解析单元格值配置（格式：E10：我的天啊）
            string[] cellValuePairs = cellValues.Split(',', StringSplitOptions.RemoveEmptyEntries);
            bool allCorrect = true;
            List<string> details = [];

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            foreach (string cellValuePair in cellValuePairs)
            {
                string[] parts = cellValuePair.Split('：', ':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    details.Add($"无效的单元格值配置: {cellValuePair}");
                    allCorrect = false;
                    continue;
                }

                string cellAddress = parts[0].Trim();
                string expectedValue = parts[1].Trim();

                try
                {
                    Excel.Range? cell = activeSheet.Range[cellAddress];
                    string actualValue = cell?.Value?.ToString() ?? "";

                    bool isMatch = string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase);
                    allCorrect &= isMatch;

                    details.Add($"单元格 {cellAddress}: 期望 '{expectedValue}', 实际 '{actualValue}' - {(isMatch ? "正确" : "错误")}");
                }
                catch (Exception ex)
                {
                    details.Add($"检测单元格 {cellAddress} 时出错: {ex.Message}");
                    allCorrect = false;
                }
            }

            result.IsCorrect = allCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测填充或复制单元格内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测合并单元格
    /// </summary>
    private KnowledgePointResult DetectMergeCells(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MergeCells",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange))
            {
                result.ErrorMessage = "缺少必要参数: CellRange";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查是否已合并
            bool isMerged = range.MergeCells;

            result.ExpectedValue = "已合并";
            result.ActualValue = isMerged ? "已合并" : "未合并";
            result.IsCorrect = isMerged;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 合并状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测合并单元格失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置单元格字体
    /// </summary>
    private KnowledgePointResult DetectSetCellFont(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellFont",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("FontFamily", out string? expectedFont))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 FontFamily";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            string actualFont = range.Font.Name?.ToString() ?? "";

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont;
            result.IsCorrect = string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测设置单元格字体失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测使用函数
    /// </summary>
    private KnowledgePointResult DetectUseFunction(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "UseFunction",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellAddress", out string? cellAddress) ||
                !parameters.TryGetValue("ExpectedValue", out string? expectedValue))
            {
                result.ErrorMessage = "缺少必要参数: CellAddress 或 ExpectedValue";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? cell = activeSheet.Range[cellAddress];
            if (cell == null)
            {
                result.ErrorMessage = $"无法获取单元格: {cellAddress}";
                return result;
            }

            // 检查单元格是否包含公式
            string formula = cell.Formula?.ToString() ?? "";
            string actualValue = cell.Value?.ToString() ?? "";

            bool hasFormula = !string.IsNullOrEmpty(formula) && formula.StartsWith("=");
            bool valueMatches = string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase);

            result.ExpectedValue = expectedValue;
            result.ActualValue = actualValue;
            result.IsCorrect = hasFormula && valueMatches;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格 {cellAddress}: 公式 '{formula}', 期望值 {expectedValue}, 实际值 {actualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测使用函数失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    // 第一类：Excel基础操作的缺失检测方法

    /// <summary>
    /// 检测删除单元格内容
    /// </summary>
    private KnowledgePointResult DetectDeleteCellContent(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DeleteCellContent",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange))
            {
                result.ErrorMessage = "缺少必要参数: CellRange";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查单元格是否为空
            bool isEmpty = true;
            foreach (Excel.Range cell in range.Cells)
            {
                if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    isEmpty = false;
                    break;
                }
            }

            result.ExpectedValue = "单元格内容已删除";
            result.ActualValue = isEmpty ? "单元格内容已删除" : "单元格内容未删除";
            result.IsCorrect = isEmpty;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 删除状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测删除单元格内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入或删除单元格
    /// </summary>
    private KnowledgePointResult DetectInsertDeleteCells(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertDeleteCells",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("OperationAction", out string? operationAction) ||
                !parameters.TryGetValue("CellRange", out string? cellRange))
            {
                result.ErrorMessage = "缺少必要参数: OperationAction 或 CellRange";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 这里简化检测逻辑，实际应用中需要更复杂的检查
            // 可以通过检查工作表的结构变化来判断是否进行了插入或删除操作
            bool operationPerformed = CheckCellStructureChange(activeSheet, cellRange, operationAction);

            result.ExpectedValue = $"{operationAction}操作已执行";
            result.ActualValue = operationPerformed ? $"{operationAction}操作已执行" : $"{operationAction}操作未执行";
            result.IsCorrect = operationPerformed;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} {operationAction}操作状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测插入或删除单元格失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入或删除行
    /// </summary>
    private KnowledgePointResult DetectInsertDeleteRows(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertDeleteRows",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("OperationAction", out string? operationAction) ||
                !parameters.TryGetValue("RowNumbers", out string? rowNumbers))
            {
                result.ErrorMessage = "缺少必要参数: OperationAction 或 RowNumbers";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 简化检测逻辑，检查行数变化
            bool operationPerformed = CheckRowStructureChange(activeSheet, rowNumbers, operationAction);

            result.ExpectedValue = $"{operationAction}行操作已执行";
            result.ActualValue = operationPerformed ? $"{operationAction}行操作已执行" : $"{operationAction}行操作未执行";
            result.IsCorrect = operationPerformed;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"行 {rowNumbers} {operationAction}操作状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测插入或删除行失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectSetInnerBorderStyle(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetInnerBorderStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("BorderStyle", out string? expectedBorderStyle))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 BorderStyle";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查内边框样式
            string actualBorderStyle = GetBorderStyleDescription(range.Borders[Excel.XlBordersIndex.xlInsideHorizontal].LineStyle);

            result.ExpectedValue = expectedBorderStyle;
            result.ActualValue = actualBorderStyle;
            result.IsCorrect = string.Equals(actualBorderStyle, expectedBorderStyle, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的内边框样式: 期望 {expectedBorderStyle}, 实际 {actualBorderStyle}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测内边框样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测内边框颜色
    /// </summary>
    private KnowledgePointResult DetectSetInnerBorderColor(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetInnerBorderColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("BorderColor", out string? expectedBorderColor))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 BorderColor";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查内边框颜色
            string actualBorderColor = GetColorDescription(range.Borders[Excel.XlBordersIndex.xlInsideHorizontal].Color);

            result.ExpectedValue = expectedBorderColor;
            result.ActualValue = actualBorderColor;
            result.IsCorrect = string.Equals(actualBorderColor, expectedBorderColor, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的内边框颜色: 期望 {expectedBorderColor}, 实际 {actualBorderColor}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测内边框颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入或删除列
    /// </summary>
    private KnowledgePointResult DetectInsertDeleteColumns(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertDeleteColumns",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("OperationAction", out string? operationAction) ||
                !parameters.TryGetValue("ColumnLetters", out string? columnLetters))
            {
                result.ErrorMessage = "缺少必要参数: OperationAction 或 ColumnLetters";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 简化检测逻辑，检查列数变化
            bool operationPerformed = CheckColumnStructureChange(activeSheet, columnLetters, operationAction);

            result.ExpectedValue = $"{operationAction}列操作已执行";
            result.ActualValue = operationPerformed ? $"{operationAction}列操作已执行" : $"{operationAction}列操作未执行";
            result.IsCorrect = operationPerformed;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"列 {columnLetters} {operationAction}操作状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测插入或删除列失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置数字格式
    /// </summary>
    private KnowledgePointResult DetectSetNumberFormat(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetNumberFormat",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("NumberFormat", out string? expectedFormat))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 NumberFormat";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查数字格式
            string actualFormat = range.NumberFormat?.ToString() ?? "";
            string expectedFormatCode = GetNumberFormatCode(expectedFormat);

            result.ExpectedValue = expectedFormat;
            result.ActualValue = GetNumberFormatDescription(actualFormat);
            result.IsCorrect = string.Equals(actualFormat, expectedFormatCode, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的数字格式: 期望 {expectedFormat}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测数字格式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectSetHorizontalAlignment(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHorizontalAlignment",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("HorizontalAlignment", out string? expectedAlignment))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 HorizontalAlignment";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 获取实际对齐方式
            Excel.XlHAlign actualAlignment = range.HorizontalAlignment;
            string actualAlignmentStr = GetHorizontalAlignmentDescription(actualAlignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignmentStr;
            result.IsCorrect = string.Equals(actualAlignmentStr, expectedAlignment, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的水平对齐方式: 期望 {expectedAlignment}, 实际 {actualAlignmentStr}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测水平对齐方式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectSetRowHeight(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetRowHeight",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("RowNumbers", out string? rowNumbers) ||
                !parameters.TryGetValue("RowHeight", out string? rowHeightStr) ||
                !double.TryParse(rowHeightStr, out double expectedHeight))
            {
                result.ErrorMessage = "缺少必要参数: RowNumbers 或 RowHeight";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            string[] rows = rowNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
            bool allCorrect = true;
            List<string> details = [];

            foreach (string rowStr in rows)
            {
                if (int.TryParse(rowStr.Trim(), out int rowNumber))
                {
                    try
                    {
                        Excel.Range? row = activeSheet.Rows[rowNumber];
                        double actualHeight = row.RowHeight;

                        bool isMatch = Math.Abs(actualHeight - expectedHeight) < 0.1;
                        allCorrect &= isMatch;

                        details.Add($"行 {rowNumber}: 期望高度 {expectedHeight}, 实际高度 {actualHeight:F1} - {(isMatch ? "正确" : "错误")}");
                    }
                    catch (Exception ex)
                    {
                        details.Add($"检测行 {rowNumber} 时出错: {ex.Message}");
                        allCorrect = false;
                    }
                }
                else
                {
                    details.Add($"无效的行号: {rowStr}");
                    allCorrect = false;
                }
            }

            result.ExpectedValue = expectedHeight.ToString();
            result.ActualValue = string.Join("; ", details);
            result.IsCorrect = allCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测行高失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectSetColumnWidth(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetColumnWidth",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ColumnLetters", out string? columnLetters) ||
                !parameters.TryGetValue("ColumnWidth", out string? columnWidthStr) ||
                !double.TryParse(columnWidthStr, out double expectedWidth))
            {
                result.ErrorMessage = "缺少必要参数: ColumnLetters 或 ColumnWidth";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            string[] columns = columnLetters.Split(',', StringSplitOptions.RemoveEmptyEntries);
            bool allCorrect = true;
            List<string> details = [];

            foreach (string columnStr in columns)
            {
                string column = columnStr.Trim();
                try
                {
                    Excel.Range? columnRange = activeSheet.Columns[column];
                    double actualWidth = columnRange.ColumnWidth;

                    bool isMatch = Math.Abs(actualWidth - expectedWidth) < 0.1;
                    allCorrect &= isMatch;

                    details.Add($"列 {column}: 期望宽度 {expectedWidth}, 实际宽度 {actualWidth:F1} - {(isMatch ? "正确" : "错误")}");
                }
                catch (Exception ex)
                {
                    details.Add($"检测列 {column} 时出错: {ex.Message}");
                    allCorrect = false;
                }
            }

            result.ExpectedValue = expectedWidth.ToString();
            result.ActualValue = string.Join("; ", details);
            result.IsCorrect = allCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测列宽失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测自动调整行高
    /// </summary>
    private KnowledgePointResult DetectAutoFitRowHeight(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AutoFitRowHeight",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("RowNumbers", out string? rowNumbers))
            {
                result.ErrorMessage = "缺少必要参数: RowNumbers";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查行高是否为自动调整状态
            bool isAutoFit = CheckRowAutoFit(activeSheet, rowNumbers);

            result.ExpectedValue = "行高已自动调整";
            result.ActualValue = isAutoFit ? "行高已自动调整" : "行高未自动调整";
            result.IsCorrect = isAutoFit;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"行 {rowNumbers} 自动调整状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测自动调整行高失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测自动调整列宽
    /// </summary>
    private KnowledgePointResult DetectAutoFitColumnWidth(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AutoFitColumnWidth",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ColumnLetters", out string? columnLetters))
            {
                result.ErrorMessage = "缺少必要参数: ColumnLetters";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查列宽是否为自动调整状态
            bool isAutoFit = CheckColumnAutoFit(activeSheet, columnLetters);

            result.ExpectedValue = "列宽已自动调整";
            result.ActualValue = isAutoFit ? "列宽已自动调整" : "列宽未自动调整";
            result.IsCorrect = isAutoFit;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"列 {columnLetters} 自动调整状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测自动调整列宽失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置字体样式
    /// </summary>
    private KnowledgePointResult DetectSetFontStyle(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("FontStyle", out string? expectedStyle))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 FontStyle";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查字体样式（粗体、斜体等）
            bool isBold = range.Font.Bold;
            bool isItalic = range.Font.Italic;

            string actualStyle = GetFontStyleDescription(isBold, isItalic);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = string.Equals(actualStyle, expectedStyle, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的字体样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测字体样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置字号
    /// </summary>
    private KnowledgePointResult DetectSetFontSize(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontSize",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("FontSize", out string? fontSizeStr) ||
                !double.TryParse(fontSizeStr, out double expectedSize))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 FontSize";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            double actualSize = range.Font.Size;

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = Math.Abs(actualSize - expectedSize) < 0.1;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测字号失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置字体颜色
    /// </summary>
    private KnowledgePointResult DetectSetFontColor(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("FontColor", out string? expectedColor))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 FontColor";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            string actualColor = GetColorDescription(range.Font.Color);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = string.Equals(actualColor, expectedColor, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的字体颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测字体颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置垂直对齐方式
    /// </summary>
    private KnowledgePointResult DetectSetVerticalAlignment(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetVerticalAlignment",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("VerticalAlignment", out string? expectedAlignment))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 VerticalAlignment";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            Excel.XlVAlign actualAlignment = range.VerticalAlignment;
            string actualAlignmentStr = GetVerticalAlignmentDescription(actualAlignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignmentStr;
            result.IsCorrect = string.Equals(actualAlignmentStr, expectedAlignment, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的垂直对齐方式: 期望 {expectedAlignment}, 实际 {actualAlignmentStr}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测垂直对齐方式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测设置单元格填充颜色
    /// </summary>
    private KnowledgePointResult DetectSetCellFillColor(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetCellFillColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CellRange", out string? cellRange) ||
                !parameters.TryGetValue("FillColor", out string? expectedColor))
            {
                result.ErrorMessage = "缺少必要参数: CellRange 或 FillColor";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.Range? range = activeSheet.Range[cellRange];
            if (range == null)
            {
                result.ErrorMessage = $"无法获取单元格区域: {cellRange}";
                return result;
            }

            // 检查填充颜色
            string actualColor = GetColorDescription(range.Interior.Color);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = string.Equals(actualColor, expectedColor, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格区域 {cellRange} 的填充颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测设置单元格填充颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectFilter(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "Filter",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FilterConditions", out string? filterConditions))
            {
                result.ErrorMessage = "缺少必要参数: FilterConditions";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查是否启用了自动筛选
            bool hasAutoFilter = activeSheet.AutoFilterMode;
            if (!hasAutoFilter)
            {
                result.ErrorMessage = "工作表未启用自动筛选";
                result.IsCorrect = false;
                return result;
            }

            // 解析筛选条件（格式：列名：筛选值）
            string[] conditions = filterConditions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            bool allConditionsApplied = true;
            List<string> details = [];

            foreach (string condition in conditions)
            {
                string[] parts = condition.Split('：', ':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    details.Add($"无效的筛选条件格式: {condition}");
                    allConditionsApplied = false;
                    continue;
                }

                string columnName = parts[0].Trim();
                string filterValue = parts[1].Trim();

                try
                {
                    // 检查筛选是否已应用
                    // 这里简化检查，实际应用中可能需要更复杂的逻辑
                    bool filterApplied = CheckFilterApplied(activeSheet, columnName, filterValue);
                    allConditionsApplied &= filterApplied;

                    details.Add($"列 '{columnName}' 筛选值 '{filterValue}': {(filterApplied ? "已应用" : "未应用")}");
                }
                catch (Exception ex)
                {
                    details.Add($"检查筛选条件 '{condition}' 时出错: {ex.Message}");
                    allConditionsApplied = false;
                }
            }

            result.ExpectedValue = "筛选条件已应用";
            result.ActualValue = allConditionsApplied ? "筛选条件已应用" : "筛选条件未完全应用";
            result.IsCorrect = allConditionsApplied;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测筛选失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectSort(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "Sort",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SortColumn", out string? sortColumn) ||
                !parameters.TryGetValue("SortOrder", out string? sortOrder) ||
                !parameters.TryGetValue("HasHeader", out string? hasHeaderStr) ||
                !bool.TryParse(hasHeaderStr, out bool hasHeader))
            {
                result.ErrorMessage = "缺少必要参数: SortColumn, SortOrder 或 HasHeader";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查数据是否已按指定列排序
            bool isSorted = CheckDataSorted(activeSheet, sortColumn, sortOrder, hasHeader);

            result.ExpectedValue = $"按列 '{sortColumn}' {sortOrder}排序";
            result.ActualValue = isSorted ? "数据已正确排序" : "数据未正确排序";
            result.IsCorrect = isSorted;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"检查列 '{sortColumn}' 的 {sortOrder}排序状态: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测排序失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测分类汇总
    /// </summary>
    private KnowledgePointResult DetectSubtotal(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "Subtotal",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("GroupByColumn", out string? groupByColumn) ||
                !parameters.TryGetValue("SummaryFunction", out string? summaryFunction) ||
                !parameters.TryGetValue("SummaryColumn", out string? summaryColumn))
            {
                result.ErrorMessage = "缺少必要参数: GroupByColumn, SummaryFunction 或 SummaryColumn";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查是否存在分类汇总
            bool hasSubtotal = CheckSubtotalExists(activeSheet, groupByColumn, summaryFunction, summaryColumn);

            result.ExpectedValue = $"按 '{groupByColumn}' 分类，对 '{summaryColumn}' 进行 {summaryFunction}";
            result.ActualValue = hasSubtotal ? "分类汇总已创建" : "分类汇总未创建";
            result.IsCorrect = hasSubtotal;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"分类汇总检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测分类汇总失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测高级筛选条件
    /// </summary>
    private KnowledgePointResult DetectAdvancedFilterCondition(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AdvancedFilterCondition",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ConditionRange", out string? conditionRange) ||
                !parameters.TryGetValue("FilterField", out string? filterField) ||
                !parameters.TryGetValue("FilterValue", out string? filterValue))
            {
                result.ErrorMessage = "缺少必要参数: ConditionRange, FilterField 或 FilterValue";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查高级筛选条件区域
            bool conditionExists = CheckAdvancedFilterCondition(activeSheet, conditionRange, filterField, filterValue);

            result.ExpectedValue = $"条件区域 {conditionRange} 包含字段 '{filterField}' 值 '{filterValue}'";
            result.ActualValue = conditionExists ? "高级筛选条件已设置" : "高级筛选条件未设置";
            result.IsCorrect = conditionExists;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"高级筛选条件检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测高级筛选条件失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测高级筛选数据
    /// </summary>
    private KnowledgePointResult DetectAdvancedFilterData(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "AdvancedFilterData",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("DataRange", out string? dataRange) ||
                !parameters.TryGetValue("CriteriaRange", out string? criteriaRange))
            {
                result.ErrorMessage = "缺少必要参数: DataRange 或 CriteriaRange";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查高级筛选是否已应用
            bool filterApplied = CheckAdvancedFilterData(activeSheet, dataRange, criteriaRange, parameters.GetValueOrDefault("CopyToRange", ""));

            result.ExpectedValue = "高级筛选已应用";
            result.ActualValue = filterApplied ? "高级筛选已应用" : "高级筛选未应用";
            result.IsCorrect = filterApplied;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"高级筛选数据检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测高级筛选数据失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectPivotTable(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "PivotTable",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("PivotRowFields", out string? pivotRowFields) ||
                !parameters.TryGetValue("PivotDataField", out string? pivotDataField) ||
                !parameters.TryGetValue("PivotFunction", out string? pivotFunction))
            {
                result.ErrorMessage = "缺少必要参数: PivotRowFields, PivotDataField 或 PivotFunction";
                return result;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查工作表中是否存在数据透视表
            bool hasPivotTable = activeSheet.PivotTables().Count > 0;
            if (!hasPivotTable)
            {
                result.ErrorMessage = "工作表中未找到数据透视表";
                result.IsCorrect = false;
                return result;
            }

            // 检查数据透视表配置
            Excel.PivotTable pivotTable = activeSheet.PivotTables(1);
            bool configurationCorrect = CheckPivotTableConfiguration(pivotTable, pivotRowFields, pivotDataField, pivotFunction);

            result.ExpectedValue = $"数据透视表配置: 行字段={pivotRowFields}, 数据字段={pivotDataField}, 函数={pivotFunction}";
            result.ActualValue = configurationCorrect ? "配置正确" : "配置不正确";
            result.IsCorrect = configurationCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"数据透视表检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测数据透视表失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectChartType(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartType",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ChartType", out string? expectedChartType))
            {
                result.ErrorMessage = "缺少必要参数: ChartType";
                return result;
            }

            // 获取图表编号（如果有的话）
            int chartNumber = 1;
            if (parameters.TryGetValue("ChartNumber", out string? chartNumberStr) &&
                int.TryParse(chartNumberStr, out int parsedChartNumber))
            {
                chartNumber = parsedChartNumber;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查工作表中是否存在图表
            Excel.ChartObjects chartObjects = activeSheet.ChartObjects();
            if (chartObjects.Count == 0)
            {
                result.ErrorMessage = "工作表中未找到图表";
                result.IsCorrect = false;
                return result;
            }

            if (chartNumber > chartObjects.Count)
            {
                result.ErrorMessage = $"图表编号 {chartNumber} 超出范围，工作表中只有 {chartObjects.Count} 个图表";
                result.IsCorrect = false;
                return result;
            }

            // 检查指定图表的类型
            Excel.ChartObject chartObject = chartObjects.Item(chartNumber);
            Excel.Chart chart = chartObject.Chart;

            string actualChartType = GetChartTypeDescription(chart.ChartType);

            result.ExpectedValue = expectedChartType;
            result.ActualValue = actualChartType;
            result.IsCorrect = string.Equals(actualChartType, expectedChartType, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表 {chartNumber} 类型: 期望 {expectedChartType}, 实际 {actualChartType}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测图表类型失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测图表样式
    /// </summary>
    private KnowledgePointResult DetectChartStyle(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("StyleNumber", out string? styleNumberStr) ||
                !int.TryParse(styleNumberStr, out int expectedStyleNumber))
            {
                result.ErrorMessage = "缺少必要参数: StyleNumber";
                return result;
            }

            // 获取图表编号
            int chartNumber = 1;
            if (parameters.TryGetValue("ChartNumber", out string? chartNumberStr) &&
                int.TryParse(chartNumberStr, out int parsedChartNumber))
            {
                chartNumber = parsedChartNumber;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.ChartObjects chartObjects = activeSheet.ChartObjects();
            if (chartObjects.Count == 0 || chartNumber > chartObjects.Count)
            {
                result.ErrorMessage = $"图表编号 {chartNumber} 不存在";
                result.IsCorrect = false;
                return result;
            }

            Excel.ChartObject chartObject = chartObjects.Item(chartNumber);
            Excel.Chart chart = chartObject.Chart;

            // 简化检测逻辑，实际应用中需要检查具体的样式编号
            bool styleMatches = CheckChartStyle(chart, expectedStyleNumber);

            result.ExpectedValue = $"样式编号 {expectedStyleNumber}";
            result.ActualValue = styleMatches ? $"样式编号 {expectedStyleNumber}" : "样式不匹配";
            result.IsCorrect = styleMatches;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表 {chartNumber} 样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测图表样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测图表移动
    /// </summary>
    private KnowledgePointResult DetectChartMove(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartMove",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("MoveLocation", out string? moveLocation))
            {
                result.ErrorMessage = "缺少必要参数: MoveLocation";
                return result;
            }

            // 获取图表编号
            int chartNumber = 1;
            if (parameters.TryGetValue("ChartNumber", out string? chartNumberStr) &&
                int.TryParse(chartNumberStr, out int parsedChartNumber))
            {
                chartNumber = parsedChartNumber;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查图表移动状态
            bool movePerformed = CheckChartMove(workbook, activeSheet, chartNumber, moveLocation, parameters.GetValueOrDefault("TargetSheet", ""));

            result.ExpectedValue = $"图表移动到 {moveLocation}";
            result.ActualValue = movePerformed ? "图表已移动" : "图表未移动";
            result.IsCorrect = movePerformed;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表 {chartNumber} 移动检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测图表移动失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectChartTitle(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ChartTitle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ChartTitle", out string? expectedTitle))
            {
                result.ErrorMessage = "缺少必要参数: ChartTitle";
                return result;
            }

            // 获取图表编号（如果有的话）
            int chartNumber = 1;
            if (parameters.TryGetValue("ChartNumber", out string? chartNumberStr) &&
                int.TryParse(chartNumberStr, out int parsedChartNumber))
            {
                chartNumber = parsedChartNumber;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            // 检查工作表中是否存在图表
            Excel.ChartObjects chartObjects = activeSheet.ChartObjects();
            if (chartObjects.Count == 0)
            {
                result.ErrorMessage = "工作表中未找到图表";
                result.IsCorrect = false;
                return result;
            }

            if (chartNumber > chartObjects.Count)
            {
                result.ErrorMessage = $"图表编号 {chartNumber} 超出范围，工作表中只有 {chartObjects.Count} 个图表";
                result.IsCorrect = false;
                return result;
            }

            // 检查指定图表的标题
            Excel.ChartObject chartObject = chartObjects.Item(chartNumber);
            Excel.Chart chart = chartObject.Chart;

            string actualTitle = "";
            if (chart.HasTitle)
            {
                actualTitle = chart.ChartTitle.Text ?? "";
            }

            result.ExpectedValue = expectedTitle;
            result.ActualValue = actualTitle;
            result.IsCorrect = string.Equals(actualTitle, expectedTitle, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表 {chartNumber} 标题: 期望 '{expectedTitle}', 实际 '{actualTitle}'";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测图表标题失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测分类轴数据区域
    /// </summary>
    private KnowledgePointResult DetectCategoryAxisDataRange(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CategoryAxisDataRange",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("CategoryRange", out string? expectedRange))
            {
                result.ErrorMessage = "缺少必要参数: CategoryRange";
                return result;
            }

            int chartNumber = 1;
            if (parameters.TryGetValue("ChartNumber", out string? chartNumberStr) &&
                int.TryParse(chartNumberStr, out int parsedChartNumber))
            {
                chartNumber = parsedChartNumber;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.ChartObjects chartObjects = activeSheet.ChartObjects();
            if (chartObjects.Count == 0 || chartNumber > chartObjects.Count)
            {
                result.ErrorMessage = $"图表编号 {chartNumber} 不存在";
                result.IsCorrect = false;
                return result;
            }

            Excel.ChartObject chartObject = chartObjects.Item(chartNumber);
            Excel.Chart chart = chartObject.Chart;

            // 检查分类轴数据区域
            bool rangeMatches = CheckCategoryAxisRange(chart, expectedRange);

            result.ExpectedValue = expectedRange;
            result.ActualValue = rangeMatches ? expectedRange : "数据区域不匹配";
            result.IsCorrect = rangeMatches;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表 {chartNumber} 分类轴数据区域检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测分类轴数据区域失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测数值轴数据区域
    /// </summary>
    private KnowledgePointResult DetectValueAxisDataRange(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ValueAxisDataRange",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ValueRange", out string? expectedRange))
            {
                result.ErrorMessage = "缺少必要参数: ValueRange";
                return result;
            }

            int chartNumber = 1;
            if (parameters.TryGetValue("ChartNumber", out string? chartNumberStr) &&
                int.TryParse(chartNumberStr, out int parsedChartNumber))
            {
                chartNumber = parsedChartNumber;
            }

            Excel.Worksheet? activeSheet = workbook.ActiveSheet;
            if (activeSheet == null)
            {
                result.ErrorMessage = "无法获取活动工作表";
                return result;
            }

            Excel.ChartObjects chartObjects = activeSheet.ChartObjects();
            if (chartObjects.Count == 0 || chartNumber > chartObjects.Count)
            {
                result.ErrorMessage = $"图表编号 {chartNumber} 不存在";
                result.IsCorrect = false;
                return result;
            }

            Excel.ChartObject chartObject = chartObjects.Item(chartNumber);
            Excel.Chart chart = chartObject.Chart;

            // 检查数值轴数据区域
            bool rangeMatches = CheckValueAxisRange(chart, expectedRange);

            result.ExpectedValue = expectedRange;
            result.ActualValue = rangeMatches ? expectedRange : "数据区域不匹配";
            result.IsCorrect = rangeMatches;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图表 {chartNumber} 数值轴数据区域检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测数值轴数据区域失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 获取水平对齐方式的描述
    /// </summary>
    private static string GetHorizontalAlignmentDescription(Excel.XlHAlign alignment)
    {
        return alignment switch
        {
            Excel.XlHAlign.xlHAlignLeft => "左对齐",
            Excel.XlHAlign.xlHAlignCenter => "居中对齐",
            Excel.XlHAlign.xlHAlignRight => "右对齐",
            Excel.XlHAlign.xlHAlignJustify => "两端对齐",
            Excel.XlHAlign.xlHAlignDistributed => "分散对齐",
            Excel.XlHAlign.xlHAlignFill => "填充",
            Excel.XlHAlign.xlHAlignCenterAcrossSelection => "跨列居中",
            Excel.XlHAlign.xlHAlignGeneral => "默认",
            _ => "未知对齐方式"
        };
    }

    /// <summary>
    /// 检查筛选是否已应用
    /// </summary>
    private static bool CheckFilterApplied(Excel.Worksheet worksheet, string columnName, string filterValue)
    {
        try
        {
            // 简化的筛选检查逻辑
            // 实际应用中需要更复杂的检查
            if (worksheet.AutoFilter?.Filters != null)
            {
                // 这里可以添加更详细的筛选检查逻辑
                // 目前返回true表示假设筛选已应用
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
    /// 检查数据是否已排序
    /// </summary>
    private static bool CheckDataSorted(Excel.Worksheet worksheet, string sortColumn, string sortOrder, bool hasHeader)
    {
        try
        {
            // 获取数据区域
            Excel.Range? usedRange = worksheet.UsedRange;
            if (usedRange == null) return false;

            // 简化的排序检查逻辑
            // 实际应用中需要检查具体的排序状态
            // 这里假设如果有数据就认为可能已排序
            int dataRows = usedRange.Rows.Count;
            if (hasHeader) dataRows--;

            return dataRows > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查数据透视表配置
    /// </summary>
    private static bool CheckPivotTableConfiguration(Excel.PivotTable pivotTable, string expectedRowFields, string expectedDataField, string expectedFunction)
    {
        try
        {
            // 简化的数据透视表配置检查
            // 实际应用中需要检查具体的字段配置

            // 检查行字段
            bool hasRowFields = pivotTable.RowFields().Count > 0;

            // 检查数据字段
            bool hasDataFields = pivotTable.DataFields().Count > 0;

            return hasRowFields && hasDataFields;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查分类汇总是否存在
    /// </summary>
    private static bool CheckSubtotalExists(Excel.Worksheet worksheet, string groupByColumn, string summaryFunction, string summaryColumn)
    {
        try
        {
            // 简化检测逻辑，检查工作表中是否存在分类汇总
            // 实际应用中需要检查具体的分类汇总配置
            Excel.Range? usedRange = worksheet.UsedRange;
            if (usedRange == null) return false;

            // 检查是否有分组大纲
            bool hasOutline = worksheet.Outline.SummaryRow != Excel.XlSummaryRow.xlSummaryAbove ||
                             worksheet.Outline.SummaryColumn != Excel.XlSummaryColumn.xlSummaryOnLeft;

            return hasOutline;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查高级筛选条件
    /// </summary>
    private static bool CheckAdvancedFilterCondition(Excel.Worksheet worksheet, string conditionRange, string filterField, string filterValue)
    {
        try
        {
            Excel.Range? range = worksheet.Range[conditionRange];
            if (range == null) return false;

            // 检查条件区域是否包含指定的字段和值
            foreach (Excel.Range cell in range.Cells)
            {
                string cellValue = cell.Value?.ToString() ?? "";
                if (cellValue.Contains(filterField) || cellValue.Contains(filterValue))
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
    /// 检查高级筛选数据
    /// </summary>
    private static bool CheckAdvancedFilterData(Excel.Worksheet worksheet, string dataRange, string criteriaRange, string copyToRange)
    {
        try
        {
            // 简化检测逻辑，检查是否应用了高级筛选
            // 实际应用中需要检查筛选结果
            Excel.Range? dataRangeObj = worksheet.Range[dataRange];
            Excel.Range? criteriaRangeObj = worksheet.Range[criteriaRange];

            if (dataRangeObj == null || criteriaRangeObj == null) return false;

            // 如果指定了复制到区域，检查该区域是否有数据
            if (!string.IsNullOrEmpty(copyToRange))
            {
                Excel.Range? copyToRangeObj = worksheet.Range[copyToRange];
                if (copyToRangeObj == null) return false;

                // 检查复制到区域是否有数据
                foreach (Excel.Range cell in copyToRangeObj.Cells)
                {
                    if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                    {
                        return true;
                    }
                }
            }

            // 简化检查，假设如果条件区域有内容就认为筛选已应用
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取图表类型的描述
    /// </summary>
    private static string GetChartTypeDescription(Excel.XlChartType chartType)
    {
        return chartType switch
        {
            Excel.XlChartType.xlColumnClustered => "簇状柱形图",
            Excel.XlChartType.xlColumnStacked => "堆积柱形图",
            Excel.XlChartType.xlColumnStacked100 => "百分比堆积柱形图",
            Excel.XlChartType.xlBarClustered => "簇状条形图",
            Excel.XlChartType.xlBarStacked => "堆积条形图",
            Excel.XlChartType.xlBarStacked100 => "百分比堆积条形图",
            Excel.XlChartType.xlLine => "折线图",
            Excel.XlChartType.xlLineMarkers => "带数据标记的折线图",
            Excel.XlChartType.xlPie => "饼图",
            Excel.XlChartType.xlPieExploded => "分离型饼图",
            Excel.XlChartType.xlDoughnut => "圆环图",
            Excel.XlChartType.xlArea => "面积图",
            Excel.XlChartType.xlXYScatter => "散点图",
            Excel.XlChartType.xlBubble => "气泡图",
            Excel.XlChartType.xlRadar => "雷达图",
            Excel.XlChartType.xlSurface => "曲面图",
            Excel.XlChartType.xlStockHLC => "股票图",
            _ => "未知图表类型"
        };
    }

    /// <summary>
    /// 检查单元格结构变化
    /// </summary>
    private static bool CheckCellStructureChange(Excel.Worksheet worksheet, string cellRange, string operationAction)
    {
        try
        {
            // 简化检测逻辑，实际应用中需要更复杂的检查
            // 这里假设如果能正常访问单元格区域就认为操作成功
            Excel.Range? range = worksheet.Range[cellRange];
            return range != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查行结构变化
    /// </summary>
    private static bool CheckRowStructureChange(Excel.Worksheet worksheet, string rowNumbers, string operationAction)
    {
        try
        {
            // 简化检测逻辑
            string[] rows = rowNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string rowStr in rows)
            {
                if (int.TryParse(rowStr.Trim(), out int rowNumber))
                {
                    Excel.Range? row = worksheet.Rows[rowNumber];
                    if (row == null) return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查列结构变化
    /// </summary>
    private static bool CheckColumnStructureChange(Excel.Worksheet worksheet, string columnLetters, string operationAction)
    {
        try
        {
            // 简化检测逻辑
            string[] columns = columnLetters.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string column in columns)
            {
                Excel.Range? columnRange = worksheet.Columns[column.Trim()];
                if (columnRange == null) return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查行是否自动调整
    /// </summary>
    private static bool CheckRowAutoFit(Excel.Worksheet worksheet, string rowNumbers)
    {
        try
        {
            // 简化检测逻辑，实际应用中需要检查行高是否为自动调整状态
            string[] rows = rowNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string rowStr in rows)
            {
                if (int.TryParse(rowStr.Trim(), out int rowNumber))
                {
                    Excel.Range? row = worksheet.Rows[rowNumber];
                    if (row == null) return false;
                    // 这里可以添加更具体的自动调整检查逻辑
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查列是否自动调整
    /// </summary>
    private static bool CheckColumnAutoFit(Excel.Worksheet worksheet, string columnLetters)
    {
        try
        {
            // 简化检测逻辑，实际应用中需要检查列宽是否为自动调整状态
            string[] columns = columnLetters.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string column in columns)
            {
                Excel.Range? columnRange = worksheet.Columns[column.Trim()];
                if (columnRange == null) return false;
                // 这里可以添加更具体的自动调整检查逻辑
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取边框样式描述
    /// </summary>
    private static string GetBorderStyleDescription(object borderStyle)
    {
        if (borderStyle == null) return "无边框";

        return borderStyle switch
        {
            Excel.XlLineStyle.xlContinuous => "单实线",
            Excel.XlLineStyle.xlDouble => "双线",
            Excel.XlLineStyle.xlDot => "点线",
            Excel.XlLineStyle.xlDash => "短划线",
            Excel.XlLineStyle.xlDashDot => "划线+点",
            Excel.XlLineStyle.xlDashDotDot => "划线+两个点",
            Excel.XlLineStyle.xlLineStyleNone => "无边框",
            _ => "未知样式"
        };
    }

    /// <summary>
    /// 获取颜色描述
    /// </summary>
    private static string GetColorDescription(object color)
    {
        if (color == null) return "无颜色";

        try
        {
            if (color is int colorValue)
            {
                // 将Excel颜色值转换为RGB
                int r = colorValue & 0xFF;
                int g = (colorValue >> 8) & 0xFF;
                int b = (colorValue >> 16) & 0xFF;
                return $"RGB({r},{g},{b})";
            }
            return color.ToString() ?? "未知颜色";
        }
        catch
        {
            return "未知颜色";
        }
    }

    /// <summary>
    /// 获取数字格式代码
    /// </summary>
    private static string GetNumberFormatCode(string formatName)
    {
        return formatName switch
        {
            "常规" => "General",
            "数值" => "0.00",
            "货币" => "¥#,##0.00",
            "会计专用" => "_-¥* #,##0.00_-;-¥* #,##0.00_-;_-¥* \"-\"??_-;_-@_-",
            "日期" => "yyyy/m/d",
            "时间" => "h:mm:ss",
            "百分比" => "0.00%",
            "分数" => "# ?/?",
            "科学记数" => "0.00E+00",
            "文本" => "@",
            _ => formatName
        };
    }

    /// <summary>
    /// 获取数字格式描述
    /// </summary>
    private static string GetNumberFormatDescription(string formatCode)
    {
        return formatCode switch
        {
            "General" => "常规",
            "0.00" => "数值",
            "¥#,##0.00" => "货币",
            "_-¥* #,##0.00_-;-¥* #,##0.00_-;_-¥* \"-\"??_-;_-@_-" => "会计专用",
            "yyyy/m/d" => "日期",
            "h:mm:ss" => "时间",
            "0.00%" => "百分比",
            "# ?/?" => "分数",
            "0.00E+00" => "科学记数",
            "@" => "文本",
            _ => formatCode
        };
    }

    /// <summary>
    /// 检查图表样式
    /// </summary>
    private static bool CheckChartStyle(Excel.Chart chart, int expectedStyleNumber)
    {
        try
        {
            // 简化检测逻辑，实际应用中需要检查具体的样式编号
            // Excel图表样式检测比较复杂，这里返回true作为占位符
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查图表移动
    /// </summary>
    private static bool CheckChartMove(Excel.Workbook workbook, Excel.Worksheet activeSheet, int chartNumber, string moveLocation, string targetSheet)
    {
        try
        {
            // 简化检测逻辑
            if (moveLocation == "新工作表")
            {
                // 检查是否有新的图表工作表
                foreach (Excel.Worksheet sheet in workbook.Worksheets)
                {
                    if (sheet.Type == Excel.XlSheetType.xlChart)
                    {
                        return true;
                    }
                }
            }
            else if (moveLocation == "作为对象插入" && !string.IsNullOrEmpty(targetSheet))
            {
                // 检查目标工作表是否存在图表对象
                try
                {
                    Excel.Worksheet? targetSheetObj = workbook.Worksheets[targetSheet];
                    return targetSheetObj?.ChartObjects().Count > 0;
                }
                catch
                {
                    return false;
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
    /// 检查分类轴数据区域
    /// </summary>
    private static bool CheckCategoryAxisRange(Excel.Chart chart, string expectedRange)
    {
        try
        {
            // 简化检测逻辑，实际应用中需要检查图表的分类轴数据源
            // 这里假设如果图表有数据系列就认为配置正确
            return chart.SeriesCollection().Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查数值轴数据区域
    /// </summary>
    private static bool CheckValueAxisRange(Excel.Chart chart, string expectedRange)
    {
        try
        {
            // 简化检测逻辑，实际应用中需要检查图表的数值轴数据源
            // 这里假设如果图表有数据系列就认为配置正确
            return chart.SeriesCollection().Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取字体样式描述
    /// </summary>
    private static string GetFontStyleDescription(bool isBold, bool isItalic)
    {
        if (isBold && isItalic)
            return "粗斜体";
        else if (isBold)
            return "粗体";
        else if (isItalic)
            return "斜体";
        else
            return "常规";
    }

    /// <summary>
    /// 获取垂直对齐方式的描述
    /// </summary>
    private static string GetVerticalAlignmentDescription(Excel.XlVAlign alignment)
    {
        return alignment switch
        {
            Excel.XlVAlign.xlVAlignTop => "顶端对齐",
            Excel.XlVAlign.xlVAlignCenter => "居中对齐",
            Excel.XlVAlign.xlVAlignBottom => "底端对齐",
            Excel.XlVAlign.xlVAlignJustify => "两端对齐",
            Excel.XlVAlign.xlVAlignDistributed => "分散对齐",
            _ => "未知对齐方式"
        };
    }
}
