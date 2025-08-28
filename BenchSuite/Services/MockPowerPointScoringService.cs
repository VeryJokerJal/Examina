using BenchSuite.Interfaces;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// PowerPoint 评分服务的模拟实现 - 用于演示和测试
/// </summary>
public class MockPowerPointScoringService : IScoringService
{
    /// <summary>
    /// 对指定文件进行打分（异步版本）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">打分配置</param>
    /// <returns>打分结果</returns>
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
    /// 对指定文件进行打分（同步版本）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">打分配置</param>
    /// <returns>打分结果</returns>
    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = new()
        {
            StartTime = DateTime.Now
        };

        try
        {
            // 验证文件存在
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = $"PowerPoint 文件不存在: {filePath}";
                result.IsSuccess = false;
                return result;
            }

            // 验证文件扩展名
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension is not ".pptx" and not ".ppt")
            {
                result.ErrorMessage = $"不支持的文件格式: {extension}。支持的格式: .pptx, .ppt";
                result.IsSuccess = false;
                return result;
            }

            // 获取 PowerPoint 模块
            ExamModuleModel? pptModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
            if (pptModule == null)
            {
                result.ErrorMessage = "试卷模型中未找到 PowerPoint 模块，跳过PowerPoint评分";
                result.IsSuccess = true; // 设置为成功，但没有评分结果
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
                return result;
            }

            // 获取所有操作点
            List<OperationPointModel> allOperationPoints = [];
            foreach (QuestionModel question in pptModule.Questions)
            {
                allOperationPoints.AddRange(question.OperationPoints);
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "PowerPoint 模块没有包含任何操作点";
                result.IsSuccess = false;
                return result;
            }

            // 模拟评分过程
            result.KnowledgePointResults = SimulateKnowledgePointDetection(allOperationPoints, filePath);

            // 计算总分和获得分数
            result.TotalScore = allOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"评分过程中发生错误: {ex.Message}";
            result.IsSuccess = false;
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
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 验证文件是否存在
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = $"文件不存在: {filePath}";
                return result;
            }

            // 验证文件扩展名
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"不支持的文件类型: {Path.GetExtension(filePath)}";
                return result;
            }

            // 获取题目的操作点（只处理PowerPoint相关的操作点）
            List<OperationPointModel> pptOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.PowerPoint && op.IsEnabled)];

            if (pptOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何PowerPoint操作点";
                return result;
            }

            // 模拟评分过程
            result.KnowledgePointResults = SimulateKnowledgePointDetection(pptOperationPoints, filePath);

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            // 计算总分和获得分数
            result.TotalScore = pptOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"评分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检查是否可以处理指定文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否可以处理</returns>
    public bool CanProcessFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".pptx" or ".ppt";
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    /// <returns>支持的扩展名列表</returns>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return [".pptx", ".ppt"];
    }

    /// <summary>
    /// 模拟知识点检测
    /// </summary>
    /// <param name="operationPoints">操作点列表</param>
    /// <param name="filePath">文件路径</param>
    /// <returns>知识点检测结果</returns>
    private List<KnowledgePointResult> SimulateKnowledgePointDetection(List<OperationPointModel> operationPoints, string filePath)
    {
        List<KnowledgePointResult> results = [];
        Random random = new();

        foreach (OperationPointModel operationPoint in operationPoints)
        {
            KnowledgePointResult result = new()
            {
                KnowledgePointId = operationPoint.Id,
                OperationPointId = operationPoint.Id,
                KnowledgePointName = operationPoint.Name,
                KnowledgePointType = operationPoint.ModuleType.ToString(),
                TotalScore = operationPoint.Score,
                Parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value)
            };

            // 模拟检测逻辑 - 随机成功率为 80%
            bool isCorrect = random.NextDouble() > 0.2;

            result.IsCorrect = isCorrect;
            result.AchievedScore = isCorrect ? result.TotalScore : 0;

            // 根据操作点类型生成模拟的检测详情
            result.Details = GenerateSimulatedDetails(operationPoint, isCorrect);

            if (!isCorrect)
            {
                result.ErrorMessage = GenerateSimulatedErrorMessage(operationPoint);
            }

            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 生成模拟的检测详情
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <param name="isCorrect">是否正确</param>
    /// <returns>检测详情</returns>
    private string GenerateSimulatedDetails(OperationPointModel operationPoint, bool isCorrect)
    {
        string operationName = operationPoint.Name;

        return isCorrect
            ? operationName switch
            {
                "设置文稿应用主题" => "检测到演示文稿已应用指定主题",
                "设置幻灯片的字体" => "检测到指定幻灯片的字体设置正确",
                "插入幻灯片" => "检测到已在指定位置插入新幻灯片",
                "幻灯片插入文本内容" => "检测到指定位置已插入正确的文本内容",
                "设置幻灯片背景" => "检测到幻灯片背景设置正确",
                "幻灯片插入图片" => "检测到已在指定位置插入图片",
                "幻灯片插入SmartArt图形" => "检测到已插入指定类型的SmartArt图形",
                "幻灯片插入表格" => "检测到已插入指定规格的表格",
                "删除幻灯片" => "检测到指定幻灯片已被删除",
                "设置幻灯片切换方式" => "检测到幻灯片切换效果设置正确",
                _ => $"{operationName} 检测通过"
            }
            : operationName switch
            {
                "设置文稿应用主题" => "未检测到指定主题的应用",
                "设置幻灯片的字体" => "字体设置不符合要求",
                "插入幻灯片" => "未在指定位置找到新插入的幻灯片",
                "幻灯片插入文本内容" => "未找到指定的文本内容",
                "设置幻灯片背景" => "幻灯片背景设置不正确",
                "幻灯片插入图片" => "未在指定位置找到图片",
                "幻灯片插入SmartArt图形" => "未找到指定类型的SmartArt图形",
                "幻灯片插入表格" => "表格规格不符合要求",
                "删除幻灯片" => "指定幻灯片未被删除",
                "设置幻灯片切换方式" => "幻灯片切换效果设置不正确",
                _ => $"{operationName} 检测失败"
            };
    }

    /// <summary>
    /// 生成模拟的错误消息
    /// </summary>
    /// <param name="operationPoint">操作点</param>
    /// <returns>错误消息</returns>
    private string GenerateSimulatedErrorMessage(OperationPointModel operationPoint)
    {
        return operationPoint.Name switch
        {
            "设置文稿应用主题" => "请检查是否正确应用了指定的主题",
            "设置幻灯片的字体" => "请检查字体名称和应用范围是否正确",
            "插入幻灯片" => "请检查插入位置和版式设置",
            "幻灯片插入文本内容" => "请检查文本内容和插入位置",
            "设置幻灯片背景" => "请检查背景类型和样式设置",
            "幻灯片插入图片" => "请检查图片插入位置和来源",
            "幻灯片插入SmartArt图形" => "请检查SmartArt类型和布局",
            "幻灯片插入表格" => "请检查表格行列数和插入位置",
            "删除幻灯片" => "请检查是否删除了正确的幻灯片",
            "设置幻灯片切换方式" => "请检查切换方案和效果设置",
            _ => "请检查操作是否按要求完成"
        };
    }
}
