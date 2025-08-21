using Examina.Models.Api;
using Examina.Models.MockExam;

namespace Examina.Services;

/// <summary>
/// 学生端模拟考试服务接口
/// </summary>
public interface IStudentMockExamService
{
    /// <summary>
    /// 快速开始模拟考试（使用预设规则自动生成并开始）
    /// </summary>
    /// <returns>创建并开始的模拟考试</returns>
    Task<StudentMockExamDto?> QuickStartMockExamAsync();

    /// <summary>
    /// 快速开始模拟考试（返回综合训练格式，包含模块结构）
    /// </summary>
    /// <returns>创建并开始的模拟考试（综合训练格式）</returns>
    Task<MockExamComprehensiveTrainingDto?> QuickStartMockExamComprehensiveTrainingAsync();

    /// <summary>
    /// 创建模拟考试
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建的模拟考试</returns>
    Task<StudentMockExamDto?> CreateMockExamAsync(CreateMockExamRequestDto request);

    /// <summary>
    /// 获取学生的模拟考试列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>模拟考试列表</returns>
    Task<List<StudentMockExamDto>> GetStudentMockExamsAsync(int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// 获取模拟考试详情
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <returns>模拟考试详情，如果无权限访问则返回null</returns>
    Task<StudentMockExamDto?> GetMockExamDetailsAsync(int mockExamId);

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <returns>是否成功开始</returns>
    Task<bool> StartMockExamAsync(int mockExamId);

    /// <summary>
    /// 完成模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <returns>是否成功完成</returns>
    Task<bool> CompleteMockExamAsync(int mockExamId);

    /// <summary>
    /// 提交模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <returns>提交结果，包含时间状态信息</returns>
    Task<MockExamSubmissionResponseDto?> SubmitMockExamAsync(int mockExamId);

    /// <summary>
    /// 提交模拟考试成绩
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <param name="scoreRequest">成绩数据</param>
    /// <returns>是否成功提交</returns>
    Task<bool> SubmitMockExamScoreAsync(int mockExamId, SubmitMockExamScoreRequestDto scoreRequest);

    /// <summary>
    /// 获取模拟考试成绩列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>模拟考试成绩列表</returns>
    Task<List<MockExamCompletionDto>> GetMockExamCompletionsAsync(int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// 删除模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <returns>是否成功删除</returns>
    Task<bool> DeleteMockExamAsync(int mockExamId);

    /// <summary>
    /// 获取学生可访问的模拟考试总数
    /// </summary>
    /// <returns>模拟考试总数</returns>
    Task<int> GetStudentMockExamCountAsync();

    /// <summary>
    /// 获取学生已完成的模拟考试数量
    /// </summary>
    /// <returns>已完成的模拟考试数量</returns>
    Task<int> GetCompletedMockExamCountAsync();

    /// <summary>
    /// 检查是否有权限访问指定模拟考试
    /// </summary>
    /// <param name="mockExamId">模拟考试ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasAccessToMockExamAsync(int mockExamId);
}
