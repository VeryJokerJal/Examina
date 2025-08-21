using BenchSuite.Interfaces;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// Windows 评分服务的模拟实现 - 用于演示和测试
/// </summary>
public class MockWindowsScoringService : IScoringService
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
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 查找Windows模块
            ExamModuleModel? windowsModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Windows);
            if (windowsModule == null)
            {
                result.ErrorMessage = "试卷中没有找到Windows模块";
                return result;
            }

            // 获取所有操作点
            List<OperationPointModel> allOperationPoints = [];
            foreach (QuestionModel question in windowsModule.Questions)
            {
                allOperationPoints.AddRange(question.OperationPoints);
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "Windows 模块没有包含任何操作点";
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
    public ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            // 获取题目的操作点（只处理Windows相关的操作点）
            List<OperationPointModel> windowsOperationPoints = question.OperationPoints
                .Where(op => op.ModuleType == ModuleType.Windows && op.IsEnabled)
                .ToList();

            if (windowsOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何Windows操作点";
                return result;
            }

            // 模拟评分过程
            result.KnowledgePointResults = SimulateKnowledgePointDetection(windowsOperationPoints, filePath);

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            // 计算总分和获得分数
            result.TotalScore = windowsOperationPoints.Sum(op => op.Score);
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
    /// 验证是否可以处理指定的文件类型
    /// </summary>
    public bool CanProcessFile(string filePath)
    {
        // Windows打分服务不依赖特定文件，可以处理任何路径
        return true;
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        // Windows打分服务不依赖特定文件扩展名，返回空列表
        return [];
    }

    /// <summary>
    /// 模拟知识点检测过程
    /// </summary>
    private static List<KnowledgePointResult> SimulateKnowledgePointDetection(List<OperationPointModel> operationPoints, string filePath)
    {
        List<KnowledgePointResult> results = [];
        Random random = new();

        foreach (OperationPointModel operationPoint in operationPoints)
        {
            // 模拟80%的成功率
            bool isCorrect = random.NextDouble() > 0.2;

            KnowledgePointResult result = new()
            {
                KnowledgePointId = operationPoint.Id,
                KnowledgePointType = operationPoint.Name,
                IsCorrect = isCorrect,
                AchievedScore = isCorrect ? operationPoint.Score : 0,
                Parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value),
                Details = isCorrect ? $"模拟检测成功: {operationPoint.Name}" : $"模拟检测失败: {operationPoint.Name}"
            };

            results.Add(result);
        }

        return results;
    }
}
