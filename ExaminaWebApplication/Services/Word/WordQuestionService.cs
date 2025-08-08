using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using System.Text.Json;

namespace ExaminaWebApplication.Services.Word;

/// <summary>
/// Word题目服务 - 提供Word题目与操作点的业务逻辑
/// </summary>
public class WordQuestionService
{
    private readonly ApplicationDbContext _context;

    public WordQuestionService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取指定科目的所有Word题目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<List<WordQuestion>> GetQuestionsBySubjectIdAsync(int subjectId)
    {
        List<WordQuestion> questions = await _context.WordQuestions
            .Include(q => q.OperationPoints)
            .Where(q => q.SubjectId == subjectId)
            .AsNoTracking()
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();

        foreach (WordQuestion q in questions)
        {
            q.OperationPoints = q.OperationPoints.OrderBy(op => op.OrderIndex).ToList();
        }

        return questions;
    }

    /// <summary>
    /// 根据ID获取Word题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    public async Task<WordQuestion?> GetQuestionByIdAsync(int id)
    {
        WordQuestion? question = await _context.WordQuestions
            .Include(q => q.OperationPoints)
            .Include(q => q.Subject)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question != null)
        {
            question.OperationPoints = question.OperationPoints.OrderBy(op => op.OrderIndex).ToList();
        }

        return question;
    }

    /// <summary>
    /// 创建Word题目
    /// </summary>
    /// <param name="question">题目信息</param>
    /// <returns></returns>
    public async Task<WordQuestion> CreateQuestionAsync(WordQuestion question)
    {
        question.CreatedAt = DateTime.UtcNow;
        _context.WordQuestions.Add(question);
        await _context.SaveChangesAsync();
        return question;
    }

    /// <summary>
    /// 更新Word题目
    /// </summary>
    /// <param name="question">题目信息</param>
    /// <returns></returns>
    public async Task<bool> UpdateQuestionAsync(WordQuestion question)
    {
        WordQuestion? existingQuestion = await _context.WordQuestions.FindAsync(question.Id);
        if (existingQuestion == null)
        {
            return false;
        }

        existingQuestion.Title = question.Title;
        existingQuestion.Description = question.Description;
        existingQuestion.TotalScore = question.TotalScore;
        existingQuestion.Requirements = question.Requirements;
        existingQuestion.IsEnabled = question.IsEnabled;
        existingQuestion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 删除Word题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteQuestionAsync(int id)
    {
        WordQuestion? question = await _context.WordQuestions
            .Include(q => q.OperationPoints)
            .FirstOrDefaultAsync(q => q.Id == id);
        
        if (question == null)
        {
            return false;
        }

        _context.WordQuestions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 向题目添加操作点
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    public async Task<WordQuestionOperationPoint> AddOperationPointToQuestionAsync(int questionId, WordQuestionOperationPoint operationPoint)
    {
        operationPoint.QuestionId = questionId;
        operationPoint.CreatedAt = DateTime.UtcNow;
        
        // 设置排序索引
        int maxOrderIndex = await _context.WordQuestionOperationPoints
            .Where(op => op.QuestionId == questionId)
            .MaxAsync(op => (int?)op.OrderIndex) ?? 0;
        operationPoint.OrderIndex = maxOrderIndex + 1;

        _context.WordQuestionOperationPoints.Add(operationPoint);
        await _context.SaveChangesAsync();

        // 更新题目总分
        await UpdateQuestionTotalScoreAsync(questionId);

        return operationPoint;
    }

    /// <summary>
    /// 更新题目操作点
    /// </summary>
    /// <param name="operationPoint">操作点信息</param>
    /// <returns></returns>
    public async Task<bool> UpdateQuestionOperationPointAsync(WordQuestionOperationPoint operationPoint)
    {
        WordQuestionOperationPoint? existingOperationPoint = await _context.WordQuestionOperationPoints.FindAsync(operationPoint.Id);
        if (existingOperationPoint == null)
        {
            return false;
        }

        existingOperationPoint.OperationType = operationPoint.OperationType;
        existingOperationPoint.Score = operationPoint.Score;
        existingOperationPoint.OperationConfig = operationPoint.OperationConfig;
        existingOperationPoint.OrderIndex = operationPoint.OrderIndex;
        existingOperationPoint.IsEnabled = operationPoint.IsEnabled;
        existingOperationPoint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 更新题目总分
        await UpdateQuestionTotalScoreAsync(existingOperationPoint.QuestionId);

        return true;
    }

    /// <summary>
    /// 删除题目操作点
    /// </summary>
    /// <param name="operationPointId">操作点ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteQuestionOperationPointAsync(int operationPointId)
    {
        WordQuestionOperationPoint? operationPoint = await _context.WordQuestionOperationPoints.FindAsync(operationPointId);
        if (operationPoint == null)
        {
            return false;
        }

        int questionId = operationPoint.QuestionId;
        _context.WordQuestionOperationPoints.Remove(operationPoint);
        await _context.SaveChangesAsync();

        // 更新题目总分
        await UpdateQuestionTotalScoreAsync(questionId);

        return true;
    }

    /// <summary>
    /// 调整题目操作点顺序
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="operationPointOrders">操作点ID和新顺序的映射</param>
    /// <returns></returns>
    public async Task<bool> ReorderQuestionOperationPointsAsync(int questionId, Dictionary<int, int> operationPointOrders)
    {
        List<WordQuestionOperationPoint> operationPoints = await _context.WordQuestionOperationPoints
            .Where(op => op.QuestionId == questionId)
            .ToListAsync();

        foreach (WordQuestionOperationPoint operationPoint in operationPoints)
        {
            if (operationPointOrders.TryGetValue(operationPoint.Id, out int newOrder))
            {
                operationPoint.OrderIndex = newOrder;
                operationPoint.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 更新题目总分（根据操作点分值自动计算）
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns></returns>
    private async Task UpdateQuestionTotalScoreAsync(int questionId)
    {
        WordQuestion? question = await _context.WordQuestions.FindAsync(questionId);
        if (question == null)
        {
            return;
        }

        decimal totalScore = await _context.WordQuestionOperationPoints
            .Where(op => op.QuestionId == questionId && op.IsEnabled)
            .SumAsync(op => op.Score);

        question.TotalScore = totalScore;
        question.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 切换题目状态
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns></returns>
    public async Task<bool> ToggleQuestionStatusAsync(int id)
    {
        WordQuestion? question = await _context.WordQuestions.FindAsync(id);
        if (question == null)
        {
            return false;
        }

        question.IsEnabled = !question.IsEnabled;
        question.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 获取题目统计信息
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<object> GetQuestionStatisticsAsync(int subjectId)
    {
        int totalCount = await _context.WordQuestions.CountAsync(q => q.SubjectId == subjectId);
        int enabledCount = await _context.WordQuestions.CountAsync(q => q.SubjectId == subjectId && q.IsEnabled);
        decimal totalScore = await _context.WordQuestions
            .Where(q => q.SubjectId == subjectId && q.IsEnabled)
            .SumAsync(q => q.TotalScore);

        return new
        {
            TotalCount = totalCount,
            EnabledCount = enabledCount,
            TotalScore = totalScore,
            AverageScore = enabledCount > 0 ? totalScore / enabledCount : 0
        };
    }

    /// <summary>
    /// 生成题目描述（基于操作点配置）
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns></returns>
    public async Task<string> GenerateQuestionDescriptionAsync(int questionId)
    {
        List<WordQuestionOperationPoint> operationPoints = await _context.WordQuestionOperationPoints
            .Where(op => op.QuestionId == questionId && op.IsEnabled)
            .OrderBy(op => op.OrderIndex)
            .ToListAsync();

        if (!operationPoints.Any())
        {
            return "暂无操作点配置";
        }

        List<string> descriptions = new List<string>();
        foreach (WordQuestionOperationPoint operationPoint in operationPoints)
        {
            try
            {
                JsonDocument config = JsonDocument.Parse(operationPoint.OperationConfig);
                JsonElement root = config.RootElement;
                
                string paragraphIndex = root.TryGetProperty("ParagraphIndex", out JsonElement paragraphElement) 
                    ? paragraphElement.GetString() ?? "1" 
                    : "1";

                string operationDesc = operationPoint.OperationType switch
                {
                    "1" => $"设置第{paragraphIndex}段的字体",
                    "2" => $"设置第{paragraphIndex}段的字号",
                    "3" => $"设置第{paragraphIndex}段的字形",
                    "4" => $"设置第{paragraphIndex}段的字间距",
                    "5" => $"设置第{paragraphIndex}段的文字颜色",
                    "6" => $"设置第{paragraphIndex}段的对齐方式",
                    "7" => $"设置第{paragraphIndex}段的缩进",
                    "8" => $"设置第{paragraphIndex}段的行间距",
                    "9" => $"设置第{paragraphIndex}段的首字下沉",
                    "10" => $"设置第{paragraphIndex}段的段落间距",
                    "11" => $"设置第{paragraphIndex}段的边框颜色",
                    "12" => $"设置第{paragraphIndex}段的边框线型",
                    "13" => $"设置第{paragraphIndex}段的边框线宽",
                    "14" => $"设置第{paragraphIndex}段的底纹",
                    _ => $"执行第{paragraphIndex}段的操作"
                };

                descriptions.Add($"{descriptions.Count + 1}. {operationDesc}（{operationPoint.Score}分）");
            }
            catch
            {
                descriptions.Add($"{descriptions.Count + 1}. 操作点配置错误（{operationPoint.Score}分）");
            }
        }

        return string.Join("\n", descriptions);
    }
}
