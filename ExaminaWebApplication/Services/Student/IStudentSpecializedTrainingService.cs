using ExaminaWebApplication.Models.Api.Student;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端专项训练服务接口
/// </summary>
public interface IStudentSpecializedTrainingService
{
    /// <summary>
    /// 获取学生可访问的专项训练列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取专项训练详情
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>专项训练详情，如果学生无权限访问则返回null</returns>
    Task<StudentSpecializedTrainingDto?> GetTrainingDetailsAsync(int trainingId, int studentUserId);

    /// <summary>
    /// 检查学生是否有权限访问指定专项训练
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToTrainingAsync(int trainingId, int studentUserId);

    /// <summary>
    /// 获取学生可访问的专项训练总数
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>专项训练总数</returns>
    Task<int> GetAvailableTrainingCountAsync(int studentUserId);

    /// <summary>
    /// 根据模块类型获取专项训练列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="moduleType">模块类型（如：Windows、Office、Programming等）</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> GetTrainingsByModuleTypeAsync(int studentUserId, string moduleType, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 根据难度等级获取专项训练列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="difficultyLevel">难度等级（1-5）</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> GetTrainingsByDifficultyAsync(int studentUserId, int difficultyLevel, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 搜索专项训练
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="searchKeyword">搜索关键词</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项训练列表</returns>
    Task<List<StudentSpecializedTrainingDto>> SearchTrainingsAsync(int studentUserId, string searchKeyword, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取所有可用的模块类型列表
    /// </summary>
    /// <param name="studentUserId">学生用户ID</param>
    /// <returns>模块类型列表</returns>
    Task<List<string>> GetAvailableModuleTypesAsync(int studentUserId);
}
