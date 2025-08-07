using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Services.Exam;
using ExaminaWebApplication.Services.Excel;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 试卷管理系统测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamTestController : ControllerBase
{
    private readonly ExamService _examService;
    private readonly ExamSubjectService _examSubjectService;
    private readonly ExamQuestionService _examQuestionService;
    private readonly ExcelOperationService _excelOperationService;

    public ExamTestController(
        ExamService examService,
        ExamSubjectService examSubjectService,
        ExamQuestionService examQuestionService,
        ExcelOperationService excelOperationService)
    {
        _examService = examService;
        _examSubjectService = examSubjectService;
        _examQuestionService = examQuestionService;
        _excelOperationService = excelOperationService;
    }

    /// <summary>
    /// 创建示例试卷
    /// </summary>
    /// <returns></returns>
    [HttpPost("create-sample-exam")]
    public async Task<ActionResult<object>> CreateSampleExam()
    {
        try
        {
            // 创建示例试卷
            Models.Exam.Exam exam = new Models.Exam.Exam
            {
                Name = "2025年春季统一考试",
                Description = "Office应用程序综合考试，包含Excel、PowerPoint、Word、Windows、C#五个科目",
                ExamType = ExamType.UnifiedExam,
                TotalScore = 100.0m,
                DurationMinutes = 120,
                StartTime = DateTime.Now.AddDays(7),
                EndTime = DateTime.Now.AddDays(14),
                PassingScore = 60.0m,
                AllowRetake = true,
                MaxRetakeCount = 2,
                RandomizeQuestions = false,
                ShowScore = true,
                ShowAnswers = false,
                CreatedBy = 1, // 假设管理员用户ID为1
                Tags = "统考,Office,综合考试"
            };

            Models.Exam.Exam createdExam = await _examService.CreateExamAsync(exam);

            // 创建标准五科目结构
            List<ExamSubject> subjects = await _examSubjectService.CreateStandardSubjectsAsync(createdExam.Id);

            // 为Excel科目添加示例题目
            ExamSubject? excelSubject = subjects.FirstOrDefault(s => s.SubjectType == SubjectType.Excel);
            if (excelSubject != null)
            {
                await CreateSampleExcelQuestions(excelSubject.Id);
            }

            // 获取完整的试卷信息
            Models.Exam.Exam? fullExam = await _examService.GetExamByIdAsync(createdExam.Id);

            return Ok(new
            {
                Message = "示例试卷创建成功",
                Exam = fullExam,
                SubjectCount = subjects.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"创建示例试卷失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取试卷管理统计信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetExamManagementStatistics()
    {
        ExamStatistics examStats = await _examService.GetExamStatisticsAsync();
        
        // 获取Excel操作点统计
        var excelStats = await _excelOperationService.GetOperationStatisticsAsync();

        object result = new
        {
            ExamStatistics = examStats,
            ExcelOperationStatistics = excelStats,
            SystemStatus = new
            {
                ExamManagementEnabled = true,
                ExcelIntegrationEnabled = true,
                PowerPointIntegrationEnabled = false,
                WordIntegrationEnabled = false,
                WindowsIntegrationEnabled = false,
                CSharpIntegrationEnabled = false
            }
        };

        return Ok(result);
    }

    /// <summary>
    /// 测试Excel题目创建功能
    /// </summary>
    /// <returns></returns>
    [HttpPost("test-excel-question-creation")]
    public async Task<ActionResult<object>> TestExcelQuestionCreation()
    {
        try
        {
            // 获取第一个试卷的Excel科目
            List<Models.Exam.Exam> exams = await _examService.GetAllExamsAsync(true);
            Models.Exam.Exam? exam = exams.FirstOrDefault();
            
            if (exam == null)
            {
                return BadRequest("没有找到试卷，请先创建示例试卷");
            }

            ExamSubject? excelSubject = exam.Subjects.FirstOrDefault(s => s.SubjectType == SubjectType.Excel);
            if (excelSubject == null)
            {
                return BadRequest("没有找到Excel科目");
            }

            // 创建一个Excel操作题目
            CreateExcelQuestionRequest request = new CreateExcelQuestionRequest
            {
                ExamSubjectId = excelSubject.Id,
                OperationNumber = 1, // 填充或复制单元格内容
                Parameters = new Dictionary<string, object?>
                {
                    ["目标单元格"] = "E10",
                    ["填充内容"] = "测试内容"
                },
                Score = 10,
                DifficultyLevel = 1,
                EstimatedMinutes = 5,
                IsRequired = true,
                Tags = "基础操作,单元格填充",
                Remarks = "这是一个测试题目"
            };

            ExamQuestion question = await _examQuestionService.CreateQuestionFromExcelOperationAsync(request);

            return Ok(new
            {
                Message = "Excel题目创建测试成功",
                Question = question,
                ExamId = exam.Id,
                SubjectId = excelSubject.Id
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Excel题目创建测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证试卷发布功能
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    [HttpPost("validate-exam-publish/{examId}")]
    public async Task<ActionResult<object>> ValidateExamPublish(int examId)
    {
        try
        {
            Models.Exam.Exam? exam = await _examService.GetExamByIdAsync(examId);
            if (exam == null)
            {
                return NotFound($"试卷 ID {examId} 不存在");
            }

            ExamValidationResult validationResult = await _examService.ValidateExamForPublishAsync(exam);

            return Ok(new
            {
                ExamId = examId,
                ExamName = exam.Name,
                ValidationResult = validationResult,
                CanPublish = validationResult.IsValid,
                CurrentStatus = exam.Status.ToString(),
                SubjectCount = exam.Subjects.Count,
                TotalScore = exam.TotalScore,
                CalculatedScore = exam.Questions.Sum(q => q.Score)
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"验证试卷失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取可用的Excel操作点列表
    /// </summary>
    /// <returns></returns>
    [HttpGet("available-excel-operations")]
    public async Task<ActionResult<object>> GetAvailableExcelOperations()
    {
        var basicOperations = await _excelOperationService.GetOperationPointsByCategoryAsync(
            Models.Excel.ExcelOperationCategory.BasicOperation);
        
        var dataListOperations = await _excelOperationService.GetOperationPointsByCategoryAsync(
            Models.Excel.ExcelOperationCategory.DataListOperation);
        
        var chartOperations = await _excelOperationService.GetOperationPointsByCategoryAsync(
            Models.Excel.ExcelOperationCategory.ChartOperation);

        object result = new
        {
            BasicOperations = basicOperations.Select(op => new
            {
                op.OperationNumber,
                op.Name,
                op.Description,
                ParameterCount = op.Parameters.Count,
                SampleParameters = op.Parameters.Take(3).Select(p => new
                {
                    p.ParameterName,
                    p.DataType,
                    p.ExampleValue
                })
            }),
            DataListOperations = dataListOperations.Select(op => new
            {
                op.OperationNumber,
                op.Name,
                op.Description,
                ParameterCount = op.Parameters.Count
            }),
            ChartOperations = chartOperations.Select(op => new
            {
                op.OperationNumber,
                op.Name,
                op.Description,
                ParameterCount = op.Parameters.Count
            }),
            Summary = new
            {
                TotalOperations = basicOperations.Count + dataListOperations.Count + chartOperations.Count,
                BasicCount = basicOperations.Count,
                DataListCount = dataListOperations.Count,
                ChartCount = chartOperations.Count
            }
        };

        return Ok(result);
    }

    /// <summary>
    /// 创建示例Excel题目
    /// </summary>
    /// <param name="examSubjectId">Excel科目ID</param>
    /// <returns></returns>
    private async Task CreateSampleExcelQuestions(int examSubjectId)
    {
        // 创建几个不同类型的Excel题目
        List<CreateExcelQuestionRequest> sampleQuestions = new List<CreateExcelQuestionRequest>
        {
            new CreateExcelQuestionRequest
            {
                ExamSubjectId = examSubjectId,
                OperationNumber = 1, // 填充或复制单元格内容
                Parameters = new Dictionary<string, object?>
                {
                    ["目标单元格"] = "A1",
                    ["填充内容"] = "学生姓名"
                },
                Score = 5,
                DifficultyLevel = 1,
                EstimatedMinutes = 3,
                Tags = "基础操作"
            },
            new CreateExcelQuestionRequest
            {
                ExamSubjectId = examSubjectId,
                OperationNumber = 6, // 设置指定单元格字体
                Parameters = new Dictionary<string, object?>
                {
                    ["单元格区域"] = "A1:C1",
                    ["字体名称"] = "宋体"
                },
                Score = 5,
                DifficultyLevel = 1,
                EstimatedMinutes = 3,
                Tags = "字体设置"
            },
            new CreateExcelQuestionRequest
            {
                ExamSubjectId = examSubjectId,
                OperationNumber = 13, // 设置单元格区域水平对齐方式
                Parameters = new Dictionary<string, object?>
                {
                    ["单元格区域"] = "A1:C1",
                    ["水平对齐方式"] = "xlCenter"
                },
                Score = 5,
                DifficultyLevel = 2,
                EstimatedMinutes = 4,
                Tags = "对齐方式"
            },
            new CreateExcelQuestionRequest
            {
                ExamSubjectId = examSubjectId,
                OperationNumber = 15, // 使用函数
                Parameters = new Dictionary<string, object?>
                {
                    ["目标单元格"] = "D10",
                    ["期望值"] = "100",
                    ["公式内容"] = "=SUM(D1:D9)"
                },
                Score = 10,
                DifficultyLevel = 3,
                EstimatedMinutes = 8,
                Tags = "函数应用"
            }
        };

        foreach (CreateExcelQuestionRequest questionRequest in sampleQuestions)
        {
            await _examQuestionService.CreateQuestionFromExcelOperationAsync(questionRequest);
        }
    }
}
