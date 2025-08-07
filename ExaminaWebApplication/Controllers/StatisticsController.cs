using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(ApplicationDbContext context, ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 统计分析主页
        public async Task<IActionResult> Index()
        {
            try
            {
                var model = new StatisticsViewModel
                {
                    SystemStatistics = await GetSystemStatisticsAsync(),
                    ExamStatistics = await GetExamStatisticsAsync(),
                    UserStatistics = await GetUserStatisticsAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计数据时发生错误");
                return View(new StatisticsViewModel());
            }
        }

        // 系统统计页面
        public async Task<IActionResult> SystemStatistics()
        {
            try
            {
                var model = await GetSystemStatisticsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统统计数据时发生错误");
                return View(new SystemStatisticsModel());
            }
        }

        // 试卷统计页面
        public async Task<IActionResult> ExamStatistics()
        {
            try
            {
                var model = await GetExamStatisticsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取试卷统计数据时发生错误");
                return View(new ExamStatisticsModel());
            }
        }

        // API: 获取统计数据
        [HttpGet]
        public async Task<IActionResult> GetStatisticsData()
        {
            try
            {
                var data = new
                {
                    SystemStatistics = await GetSystemStatisticsAsync(),
                    ExamStatistics = await GetExamStatisticsAsync(),
                    UserStatistics = await GetUserStatisticsAsync()
                };

                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计数据API时发生错误");
                return Json(new { error = "获取数据失败" });
            }
        }

        private async Task<SystemStatisticsModel> GetSystemStatisticsAsync()
        {
            var totalExams = await _context.Exams.CountAsync();
            var totalSubjects = await _context.ExamSubjects.CountAsync();
            var totalQuestions = await _context.SimplifiedQuestions.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            var examsByStatus = await _context.Exams
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var questionsByType = await _context.SimplifiedQuestions
                .GroupBy(q => q.OperationType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return new SystemStatisticsModel
            {
                TotalExams = totalExams,
                TotalSubjects = totalSubjects,
                TotalQuestions = totalQuestions,
                TotalUsers = totalUsers,
                ExamsByStatus = examsByStatus.ToDictionary(x => x.Status.ToString(), x => x.Count),
                QuestionsByType = questionsByType.ToDictionary(x => x.Type, x => x.Count)
            };
        }

        private async Task<ExamStatisticsModel> GetExamStatisticsAsync()
        {
            var exams = await _context.Exams
                .Include(e => e.Subjects)
                .ThenInclude(s => s.Questions)
                .ToListAsync();

            var examsByType = exams
                .GroupBy(e => e.ExamType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var examsByMonth = exams
                .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new { Date = $"{g.Key.Year}-{g.Key.Month:D2}", Count = g.Count() })
                .ToList();

            var averageQuestionsPerExam = exams.Any() ? 
                exams.Average(e => e.Subjects.Sum(s => s.Questions.Count)) : 0;

            return new ExamStatisticsModel
            {
                TotalExams = exams.Count,
                ExamsByType = examsByType,
                ExamsByMonth = examsByMonth.ToDictionary(x => x.Date, x => x.Count),
                AverageQuestionsPerExam = Math.Round(averageQuestionsPerExam, 2),
                TotalQuestions = exams.Sum(e => e.Subjects.Sum(s => s.Questions.Count))
            };
        }

        private async Task<UserStatisticsModel> GetUserStatisticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users
                .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value > DateTime.Now.AddDays(-30))
                .CountAsync();

            var usersByRole = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            return new UserStatisticsModel
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                UsersByRole = usersByRole.ToDictionary(x => x.Role.ToString(), x => x.Count)
            };
        }
    }

    // 统计数据模型
    public class StatisticsViewModel
    {
        public SystemStatisticsModel SystemStatistics { get; set; } = new();
        public ExamStatisticsModel ExamStatistics { get; set; } = new();
        public UserStatisticsModel UserStatistics { get; set; } = new();
    }

    public class SystemStatisticsModel
    {
        public int TotalExams { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalUsers { get; set; }
        public Dictionary<string, int> ExamsByStatus { get; set; } = new();
        public Dictionary<string, int> QuestionsByType { get; set; } = new();
    }

    public class ExamStatisticsModel
    {
        public int TotalExams { get; set; }
        public Dictionary<string, int> ExamsByType { get; set; } = new();
        public Dictionary<string, int> ExamsByMonth { get; set; } = new();
        public double AverageQuestionsPerExam { get; set; }
        public int TotalQuestions { get; set; }
    }

    public class UserStatisticsModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new();
    }
}
