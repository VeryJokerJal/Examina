using ExaminaWebApplication.Models.Api.Admin;
using ExaminaWebApplication.Models.ImportedExam;

namespace ExaminaWebApplication.Services.Admin;

/// <summary>
/// 管理员考试管理服务接口
/// </summary>
public interface IAdminExamManagementService
{
    /// <summary>
    /// 获取管理员的考试列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>考试列表</returns>
    Task<List<AdminExamDto>> GetExamsAsync(int userId, int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试详情，如果无权限访问则返回null</returns>
    Task<AdminExamDto?> GetExamDetailsAsync(int examId, int userId);

    /// <summary>
    /// 设置考试时间
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>是否成功</returns>
    Task<bool> SetExamScheduleAsync(int examId, int userId, DateTime startTime, DateTime endTime);

    /// <summary>
    /// 更新考试状态
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="status">新状态</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateExamStatusAsync(int examId, int userId, string status);

    /// <summary>
    /// 更新考试类型
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="category">考试类型</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateExamCategoryAsync(int examId, int userId, ExamCategory category);

    /// <summary>
    /// 发布考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> PublishExamAsync(int examId, int userId);

    /// <summary>
    /// 开始考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> StartExamAsync(int examId, int userId);

    /// <summary>
    /// 结束考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> EndExamAsync(int examId, int userId);

    /// <summary>
    /// 取消考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功</returns>
    Task<bool> CancelExamAsync(int examId, int userId);

    /// <summary>
    /// 检查用户是否有权限管理指定考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasManagePermissionAsync(int examId, int userId);

    /// <summary>
    /// 获取考试统计信息
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>统计信息</returns>
    Task<ExamStatisticsDto?> GetExamStatisticsAsync(int examId, int userId);

    /// <summary>
    /// 更新考试设置（重考和重做）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="settingName">设置名称（AllowRetake或AllowPractice）</param>
    /// <param name="value">设置值</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateExamSettingAsync(int examId, int userId, string settingName, bool value);

    /// <summary>
    /// 更新试卷名称
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="newName">新的试卷名称</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateExamNameAsync(int examId, int userId, string newName);
}

/// <summary>
/// 考试统计信息DTO
/// </summary>
public class ExamStatisticsDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public int ExamId { get; set; }

    /// <summary>
    /// 考试名称
    /// </summary>
    public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 总参与人数
    /// </summary>
    public int TotalParticipants { get; set; }

    /// <summary>
    /// 已完成人数
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// 进行中人数
    /// </summary>
    public int InProgressCount { get; set; }

    /// <summary>
    /// 未开始人数
    /// </summary>
    public int NotStartedCount { get; set; }

    /// <summary>
    /// 平均分
    /// </summary>
    public decimal? AverageScore { get; set; }

    /// <summary>
    /// 最高分
    /// </summary>
    public decimal? HighestScore { get; set; }

    /// <summary>
    /// 最低分
    /// </summary>
    public decimal? LowestScore { get; set; }

    /// <summary>
    /// 及格率
    /// </summary>
    public decimal PassRate { get; set; }

    /// <summary>
    /// 完成率
    /// </summary>
    public decimal CompletionRate { get; set; }
}
