using Examina.Models;
using Examina.Models.Exam;

namespace Examina.Services;

/// <summary>
/// 学生端考试服务接口
/// </summary>
public interface IStudentExamService
{
    /// <summary>
    /// 获取学生可访问的考试列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>考试列表</returns>
    Task<List<StudentExamDto>> GetAvailableExamsAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试详情，如果无权限访问则返回null</returns>
    Task<StudentExamDto?> GetExamDetailsAsync(int examId);

    /// <summary>
    /// 检查是否有权限访问指定考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToExamAsync(int examId);

    /// <summary>
    /// 获取学生可访问的考试总数
    /// </summary>
    /// <returns>考试总数</returns>
    Task<int> GetAvailableExamCountAsync();

    /// <summary>
    /// 获取学生专项练习进度统计
    /// </summary>
    /// <returns>专项练习进度统计</returns>
    Task<SpecialPracticeProgressDto> GetSpecialPracticeProgressAsync();

    /// <summary>
    /// 获取学生可访问的专项练习总数
    /// </summary>
    /// <returns>专项练习总数</returns>
    Task<int> GetAvailableSpecialPracticeCountAsync();
}

/// <summary>
/// 学生端综合训练服务接口
/// </summary>
public interface IStudentComprehensiveTrainingService
{
    /// <summary>
    /// 获取学生可访问的综合训练列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>综合训练列表</returns>
    Task<List<StudentComprehensiveTrainingDto>> GetAvailableTrainingsAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <returns>综合训练详情，如果无权限访问则返回null</returns>
    Task<StudentComprehensiveTrainingDto?> GetTrainingDetailsAsync(int trainingId);

    /// <summary>
    /// 检查是否有权限访问指定综合训练
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToTrainingAsync(int trainingId);

    /// <summary>
    /// 获取学生可访问的综合训练总数
    /// </summary>
    /// <returns>综合训练总数</returns>
    Task<int> GetAvailableTrainingCountAsync();

    /// <summary>
    /// 获取学生综合训练进度统计
    /// </summary>
    /// <returns>综合训练进度统计</returns>
    Task<ComprehensiveTrainingProgressDto> GetTrainingProgressAsync();
}
