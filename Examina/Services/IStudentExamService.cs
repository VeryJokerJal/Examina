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
    /// 按考试类型获取学生可访问的考试列表
    /// </summary>
    /// <param name="examCategory">考试类型（全省统考或学校统考）</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>指定类型的考试列表</returns>
    Task<List<StudentExamDto>> GetAvailableExamsByCategoryAsync(ExamCategory examCategory, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 按考试类型获取学生可访问的考试总数
    /// </summary>
    /// <param name="examCategory">考试类型（全省统考或学校统考）</param>
    /// <returns>指定类型的考试总数</returns>
    Task<int> GetAvailableExamCountByCategoryAsync(ExamCategory examCategory);

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

    /// <summary>
    /// 标记专项练习为开始状态
    /// </summary>
    /// <param name="practiceId">练习ID</param>
    /// <returns>是否成功</returns>
    Task<bool> StartSpecialPracticeAsync(int practiceId);

    /// <summary>
    /// 提交专项练习成绩并标记为完成
    /// </summary>
    /// <param name="practiceId">练习ID</param>
    /// <param name="request">完成信息</param>
    /// <returns>是否成功</returns>
    Task<bool> CompleteSpecialPracticeAsync(int practiceId, CompletePracticeRequest request);

    /// <summary>
    /// 获取专项练习完成记录
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>专项练习完成记录列表</returns>
    Task<List<SpecialPracticeCompletionDto>> GetSpecialPracticeCompletionsAsync(int pageNumber = 1, int pageSize = 20);
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

    /// <summary>
    /// 标记综合训练为开始状态
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    /// <returns>是否成功</returns>
    Task<bool> StartComprehensiveTrainingAsync(int trainingId);

    /// <summary>
    /// 提交综合训练成绩并标记为完成
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    /// <param name="request">完成信息</param>
    /// <returns>是否成功</returns>
    Task<bool> CompleteComprehensiveTrainingAsync(int trainingId, CompleteTrainingRequest request);

    /// <summary>
    /// 标记综合训练为已完成
    /// </summary>
    /// <param name="trainingId">训练ID</param>
    /// <param name="score">得分（可选）</param>
    /// <param name="maxScore">最大得分（可选）</param>
    /// <param name="durationSeconds">用时（秒，可选）</param>
    /// <param name="notes">备注（可选）</param>
    /// <returns>是否标记成功</returns>
    Task<bool> MarkTrainingAsCompletedAsync(int trainingId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null);

    /// <summary>
    /// 获取综合训练完成记录
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>综合训练完成记录列表</returns>
    Task<List<ComprehensiveTrainingCompletionDto>> GetComprehensiveTrainingCompletionsAsync(int pageNumber = 1, int pageSize = 20);
}
