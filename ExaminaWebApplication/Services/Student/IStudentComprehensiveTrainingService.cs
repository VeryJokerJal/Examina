using ExaminaWebApplication.Models.Api.Student;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端综合训练服务接口
/// </summary>
public interface IStudentComprehensiveTrainingService
{
    /// <summary>
    /// 获取学生可访问的综合训练列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>综合训练列表</returns>
    Task<List<StudentComprehensiveTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>综合训练详情，如果学生无权限访问则返回null</returns>
    Task<StudentComprehensiveTrainingDto?> GetTrainingDetailsAsync(int trainingId, int studentUserId);

    /// <summary>
    /// 检查学生是否有权限访问指定综合训练
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToTrainingAsync(int trainingId, int studentUserId);

    /// <summary>
    /// 获取学生可访问的综合训练总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>综合训练总数</returns>
    Task<int> GetAvailableTrainingCountAsync(int studentUserId);

    /// <summary>
    /// 获取学生综合训练进度统计
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>综合训练进度统计</returns>
    Task<ComprehensiveTrainingProgressDto> GetTrainingProgressAsync(int studentUserId);
}
