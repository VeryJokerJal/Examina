using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExaminaWebApplication.Services.Exam;

/// <summary>
/// 简化题目服务 - 处理新的简化题目创建流程
/// </summary>
public class SimplifiedQuestionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SimplifiedQuestionService> _logger;

    public SimplifiedQuestionService(ApplicationDbContext context, ILogger<SimplifiedQuestionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 创建简化题目
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建的题目</returns>
    public async Task<SimplifiedQuestionResponse> CreateSimplifiedQuestionAsync(CreateSimplifiedQuestionRequest request)
    {
        try
        {
            // 验证科目是否存在
            ExamSubject? subject = await _context.ExamSubjects
                .FirstOrDefaultAsync(s => s.Id == request.SubjectId);

            if (subject == null)
            {
                throw new ArgumentException($"科目ID {request.SubjectId} 不存在");
            }

            // 确定题目类型
            QuestionType questionType = DetermineQuestionType((int)subject.SubjectType);

            // 序列化操作配置
            string operationConfigJson = JsonSerializer.Serialize(request.OperationConfig);

            // 创建简化题目
            SimplifiedQuestion question = new SimplifiedQuestion
            {
                SubjectId = request.SubjectId,
                OperationType = request.OperationType,
                Score = request.Score,
                OperationConfig = operationConfigJson,
                Title = request.Title ?? "自动生成题目",
                Description = request.Description ?? "自动生成描述",
                InputExample = request.InputExample,
                InputDescription = request.InputDescription,
                OutputExample = request.OutputExample,
                OutputDescription = request.OutputDescription,
                Requirements = request.Requirements,
                QuestionType = questionType,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.SimplifiedQuestions.Add(question);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"成功创建简化题目，ID: {question.Id}，操作类型: {request.OperationType}");

            return new SimplifiedQuestionResponse
            {
                Id = question.Id,
                SubjectId = question.SubjectId,
                OperationType = question.OperationType,
                Score = question.Score,
                Title = question.Title,
                Description = question.Description,
                InputExample = question.InputExample,
                InputDescription = question.InputDescription,
                OutputExample = question.OutputExample,
                OutputDescription = question.OutputDescription,
                Requirements = question.Requirements,
                OperationConfig = request.OperationConfig,
                IsEnabled = question.IsEnabled,
                CreatedAt = question.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"创建简化题目失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取简化题目列表
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns>题目列表</returns>
    public async Task<List<SimplifiedQuestionResponse>> GetSimplifiedQuestionsAsync(int subjectId)
    {
        try
        {
            List<SimplifiedQuestion> questions = await _context.SimplifiedQuestions
                .Where(q => q.SubjectId == subjectId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return questions.Select(q => new SimplifiedQuestionResponse
            {
                Id = q.Id,
                SubjectId = q.SubjectId,
                OperationType = q.OperationType,
                Score = q.Score,
                Title = q.Title,
                Description = q.Description,
                InputExample = q.InputExample,
                InputDescription = q.InputDescription,
                OutputExample = q.OutputExample,
                OutputDescription = q.OutputDescription,
                Requirements = q.Requirements,
                OperationConfig = JsonSerializer.Deserialize<object>(q.OperationConfig) ?? new(),
                IsEnabled = q.IsEnabled,
                CreatedAt = q.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取简化题目列表失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 获取简化题目详情
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>题目详情</returns>
    public async Task<SimplifiedQuestionResponse?> GetSimplifiedQuestionAsync(int questionId)
    {
        try
        {
            SimplifiedQuestion? question = await _context.SimplifiedQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return null;
            }

            return new SimplifiedQuestionResponse
            {
                Id = question.Id,
                SubjectId = question.SubjectId,
                OperationType = question.OperationType,
                Score = question.Score,
                Title = question.Title,
                Description = question.Description,
                InputExample = question.InputExample,
                InputDescription = question.InputDescription,
                OutputExample = question.OutputExample,
                OutputDescription = question.OutputDescription,
                Requirements = question.Requirements,
                OperationConfig = JsonSerializer.Deserialize<object>(question.OperationConfig) ?? new(),
                IsEnabled = question.IsEnabled,
                CreatedAt = question.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取简化题目详情失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 更新简化题目
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的题目</returns>
    public async Task<SimplifiedQuestionResponse?> UpdateSimplifiedQuestionAsync(int questionId, CreateSimplifiedQuestionRequest request)
    {
        try
        {
            SimplifiedQuestion? question = await _context.SimplifiedQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return null;
            }

            // 更新题目信息
            question.OperationType = request.OperationType;
            question.Score = request.Score;
            question.OperationConfig = JsonSerializer.Serialize(request.OperationConfig);
            question.Title = request.Title ?? question.Title;
            question.Description = request.Description ?? question.Description;
            question.InputExample = request.InputExample;
            question.InputDescription = request.InputDescription;
            question.OutputExample = request.OutputExample;
            question.OutputDescription = request.OutputDescription;
            question.Requirements = request.Requirements;
            question.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"成功更新简化题目，ID: {questionId}");

            return new SimplifiedQuestionResponse
            {
                Id = question.Id,
                SubjectId = question.SubjectId,
                OperationType = question.OperationType,
                Score = question.Score,
                Title = question.Title,
                Description = question.Description,
                InputExample = question.InputExample,
                InputDescription = question.InputDescription,
                OutputExample = question.OutputExample,
                OutputDescription = question.OutputDescription,
                Requirements = question.Requirements,
                OperationConfig = request.OperationConfig,
                IsEnabled = question.IsEnabled,
                CreatedAt = question.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新简化题目失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 切换简化题目状态
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>是否切换成功</returns>
    public async Task<bool> ToggleQuestionStatusAsync(int questionId)
    {
        try
        {
            SimplifiedQuestion? question = await _context.SimplifiedQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return false;
            }

            question.IsEnabled = !question.IsEnabled;
            question.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"成功切换简化题目状态，ID: {questionId}，新状态: {(question.IsEnabled ? "启用" : "禁用")}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"切换简化题目状态失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 删除简化题目
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>是否删除成功</returns>
    public async Task<bool> DeleteSimplifiedQuestionAsync(int questionId)
    {
        try
        {
            SimplifiedQuestion? question = await _context.SimplifiedQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return false;
            }

            _context.SimplifiedQuestions.Remove(question);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"成功删除简化题目，ID: {questionId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除简化题目失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 根据科目类型确定题目类型
    /// </summary>
    /// <param name="subjectType">科目类型</param>
    /// <returns>题目类型</returns>
    private static QuestionType DetermineQuestionType(int subjectType)
    {
        return subjectType switch
        {
            1 => QuestionType.ExcelOperation,        // Excel
            2 => QuestionType.PowerPointOperation,   // PowerPoint
            3 => QuestionType.WordOperation,         // Word
            4 => QuestionType.WindowsOperation,      // Windows
            5 => QuestionType.CSharpProgramming,     // C#
            _ => QuestionType.Comprehensive          // 其他
        };
    }
}
