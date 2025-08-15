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
            "填充或复制单元格内容" => "FillOrCopyCellContent",
            "合并单元格" => "MergeCells",
            "设置指定单元格字体" => "SetCellFont",
            "内边框样式" => "SetInnerBorderStyle",
            "内边框颜色" => "SetInnerBorderColor",
            "设置单元格区域水平对齐方式" => "SetHorizontalAlignment",
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
            "筛选" => "Filter",
            "排序" => "Sort",
            "数据透视表" => "PivotTable",
            "图表类型" => "ChartType",
            "图表标题" => "ChartTitle",
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
                case "MergeCells":
                    result = DetectMergeCells(workbook, parameters);
                    break;
                case "SetCellFont":
                    result = DetectSetCellFont(workbook, parameters);
                    break;
                case "SetInnerBorderStyle":
                    result = DetectSetInnerBorderStyle(workbook, parameters);
                    break;
                case "SetInnerBorderColor":
                    result = DetectSetInnerBorderColor(workbook, parameters);
                    break;
                case "SetHorizontalAlignment":
                    result = DetectSetHorizontalAlignment(workbook, parameters);
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
                case "SetCellFillColor":
                    result = DetectSetCellFillColor(workbook, parameters);
                    break;

                // 第二类：数据清单操作
                case "Filter":
                    result = DetectFilter(workbook, parameters);
                    break;
                case "Sort":
                    result = DetectSort(workbook, parameters);
                    break;
                case "PivotTable":
                    result = DetectPivotTable(workbook, parameters);
                    break;

                // 第三类：图表操作
                case "ChartType":
                    result = DetectChartType(workbook, parameters);
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

    // 以下是其他检测方法的占位符，将在后续任务中实现

    private KnowledgePointResult DetectSetInnerBorderStyle(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "SetInnerBorderStyle",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
    }

    private KnowledgePointResult DetectSetInnerBorderColor(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "SetInnerBorderColor",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
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

    private KnowledgePointResult DetectSetCellFillColor(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "SetCellFillColor",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
    }

    private KnowledgePointResult DetectFilter(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "Filter",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
    }

    private KnowledgePointResult DetectSort(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "Sort",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
    }

    private KnowledgePointResult DetectPivotTable(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "PivotTable",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
    }

    private KnowledgePointResult DetectChartType(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "ChartType",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
    }

    private KnowledgePointResult DetectChartTitle(Excel.Workbook workbook, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = "ChartTitle",
            Parameters = parameters,
            ErrorMessage = "此功能尚未实现",
            IsCorrect = false
        };
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
}
