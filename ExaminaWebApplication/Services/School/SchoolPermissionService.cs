using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.ImportedExam;

namespace ExaminaWebApplication.Services.School;

/// <summary>
/// 学校权限验证服务实现
/// </summary>
public class SchoolPermissionService : ISchoolPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SchoolPermissionService> _logger;

    public SchoolPermissionService(
        ApplicationDbContext context,
        ILogger<SchoolPermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 检查学生是否有权限访问指定的学校统考
    /// </summary>
    public async Task<bool> HasAccessToSchoolExamAsync(int studentUserId, int examId)
    {
        try
        {
            // 获取学生所属的学校ID
            int? studentSchoolId = await GetStudentSchoolIdAsync(studentUserId);
            if (studentSchoolId == null)
            {
                _logger.LogWarning("学生未加入任何学校，无法访问学校统考，学生ID: {StudentUserId}, 考试ID: {ExamId}", 
                    studentUserId, examId);
                return false;
            }

            // 检查考试是否对该学校开放
            bool isAvailable = await IsExamAvailableForSchoolAsync(examId, studentSchoolId.Value);
            
            _logger.LogInformation("学校统考权限检查结果，学生ID: {StudentUserId}, 考试ID: {ExamId}, 学校ID: {SchoolId}, 有权限: {HasAccess}",
                studentUserId, examId, studentSchoolId.Value, isAvailable);

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查学校统考权限失败，学生ID: {StudentUserId}, 考试ID: {ExamId}", 
                studentUserId, examId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生所属的学校ID
    /// </summary>
    public async Task<int?> GetStudentSchoolIdAsync(int studentUserId)
    {
        try
        {
            StudentOrganization? studentOrganization = await _context.StudentOrganizations
                .Include(so => so.Organization)
                .FirstOrDefaultAsync(so => so.StudentId == studentUserId &&
                                          so.IsActive &&
                                          so.Organization.Type == OrganizationType.School &&
                                          so.Organization.IsActive);

            return studentOrganization?.OrganizationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生所属学校ID失败，学生ID: {StudentUserId}", studentUserId);
            return null;
        }
    }

    /// <summary>
    /// 获取学生所属的学校信息
    /// </summary>
    public async Task<Organization?> GetStudentSchoolAsync(int studentUserId)
    {
        try
        {
            StudentOrganization? studentOrganization = await _context.StudentOrganizations
                .Include(so => so.Organization)
                .FirstOrDefaultAsync(so => so.StudentId == studentUserId &&
                                          so.IsActive &&
                                          so.Organization.Type == OrganizationType.School &&
                                          so.Organization.IsActive);

            return studentOrganization?.Organization;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生所属学校信息失败，学生ID: {StudentUserId}", studentUserId);
            return null;
        }
    }

    /// <summary>
    /// 检查考试是否对指定学校开放
    /// </summary>
    public async Task<bool> IsExamAvailableForSchoolAsync(int examId, int schoolId)
    {
        try
        {
            // 检查考试是否存在且为学校统考
            ImportedExam? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && 
                                         e.IsEnabled && 
                                         e.ExamCategory == ExamCategory.School);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或不是学校统考，考试ID: {ExamId}", examId);
                return false;
            }

            // 检查是否存在考试与学校的关联
            bool hasAssociation = await _context.ExamSchoolAssociations
                .AnyAsync(esa => esa.ExamId == examId && 
                                esa.SchoolId == schoolId && 
                                esa.IsActive);

            return hasAssociation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查考试学校可用性失败，考试ID: {ExamId}, 学校ID: {SchoolId}", 
                examId, schoolId);
            return false;
        }
    }

    /// <summary>
    /// 获取考试关联的所有学校列表
    /// </summary>
    public async Task<List<Organization>> GetExamAssociatedSchoolsAsync(int examId)
    {
        try
        {
            List<Organization> schools = await _context.ExamSchoolAssociations
                .Where(esa => esa.ExamId == examId && esa.IsActive)
                .Include(esa => esa.School)
                .Select(esa => esa.School)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            _logger.LogInformation("获取考试关联学校列表成功，考试ID: {ExamId}, 学校数量: {Count}", 
                examId, schools.Count);

            return schools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试关联学校列表失败，考试ID: {ExamId}", examId);
            return [];
        }
    }

    /// <summary>
    /// 为考试添加学校关联
    /// </summary>
    public async Task<bool> AddExamSchoolAssociationAsync(int examId, int schoolId, int createdBy, string? remarks = null)
    {
        try
        {
            // 检查是否已存在关联
            bool exists = await _context.ExamSchoolAssociations
                .AnyAsync(esa => esa.ExamId == examId && esa.SchoolId == schoolId);

            if (exists)
            {
                _logger.LogWarning("考试学校关联已存在，考试ID: {ExamId}, 学校ID: {SchoolId}", examId, schoolId);
                return false;
            }

            // 验证考试和学校是否存在
            bool examExists = await _context.ImportedExams
                .AnyAsync(e => e.Id == examId && e.IsEnabled);
            bool schoolExists = await _context.Organizations
                .AnyAsync(o => o.Id == schoolId && o.Type == OrganizationType.School && o.IsActive);

            if (!examExists || !schoolExists)
            {
                _logger.LogWarning("考试或学校不存在，考试ID: {ExamId}, 学校ID: {SchoolId}", examId, schoolId);
                return false;
            }

            // 创建关联
            ExamSchoolAssociation association = new()
            {
                ExamId = examId,
                SchoolId = schoolId,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Remarks = remarks
            };

            _context.ExamSchoolAssociations.Add(association);
            await _context.SaveChangesAsync();

            _logger.LogInformation("考试学校关联添加成功，考试ID: {ExamId}, 学校ID: {SchoolId}, 创建者: {CreatedBy}", 
                examId, schoolId, createdBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加考试学校关联失败，考试ID: {ExamId}, 学校ID: {SchoolId}", examId, schoolId);
            return false;
        }
    }

    /// <summary>
    /// 移除考试的学校关联
    /// </summary>
    public async Task<bool> RemoveExamSchoolAssociationAsync(int examId, int schoolId)
    {
        try
        {
            ExamSchoolAssociation? association = await _context.ExamSchoolAssociations
                .FirstOrDefaultAsync(esa => esa.ExamId == examId && esa.SchoolId == schoolId);

            if (association == null)
            {
                _logger.LogWarning("考试学校关联不存在，考试ID: {ExamId}, 学校ID: {SchoolId}", examId, schoolId);
                return false;
            }

            _context.ExamSchoolAssociations.Remove(association);
            await _context.SaveChangesAsync();

            _logger.LogInformation("考试学校关联移除成功，考试ID: {ExamId}, 学校ID: {SchoolId}", examId, schoolId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除考试学校关联失败，考试ID: {ExamId}, 学校ID: {SchoolId}", examId, schoolId);
            return false;
        }
    }

    /// <summary>
    /// 批量添加考试的学校关联
    /// </summary>
    public async Task<int> BatchAddExamSchoolAssociationsAsync(int examId, List<int> schoolIds, int createdBy, string? remarks = null)
    {
        try
        {
            int successCount = 0;

            foreach (int schoolId in schoolIds)
            {
                bool success = await AddExamSchoolAssociationAsync(examId, schoolId, createdBy, remarks);
                if (success)
                {
                    successCount++;
                }
            }

            _logger.LogInformation("批量添加考试学校关联完成，考试ID: {ExamId}, 成功数量: {SuccessCount}/{TotalCount}", 
                examId, successCount, schoolIds.Count);

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加考试学校关联失败，考试ID: {ExamId}", examId);
            return 0;
        }
    }

    /// <summary>
    /// 批量移除考试的学校关联
    /// </summary>
    public async Task<int> BatchRemoveExamSchoolAssociationsAsync(int examId, List<int> schoolIds)
    {
        try
        {
            int successCount = 0;

            foreach (int schoolId in schoolIds)
            {
                bool success = await RemoveExamSchoolAssociationAsync(examId, schoolId);
                if (success)
                {
                    successCount++;
                }
            }

            _logger.LogInformation("批量移除考试学校关联完成，考试ID: {ExamId}, 成功数量: {SuccessCount}/{TotalCount}", 
                examId, successCount, schoolIds.Count);

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量移除考试学校关联失败，考试ID: {ExamId}", examId);
            return 0;
        }
    }

    /// <summary>
    /// 获取学生可访问的学校统考ID列表
    /// </summary>
    public async Task<List<int>> GetAccessibleSchoolExamIdsAsync(int studentUserId)
    {
        try
        {
            // 获取学生所属的学校ID
            int? studentSchoolId = await GetStudentSchoolIdAsync(studentUserId);
            if (studentSchoolId == null)
            {
                _logger.LogInformation("学生未加入学校，无可访问的学校统考，学生ID: {StudentUserId}", studentUserId);
                return [];
            }

            // 获取该学校可访问的所有学校统考ID
            List<int> examIds = await _context.ExamSchoolAssociations
                .Where(esa => esa.SchoolId == studentSchoolId.Value && esa.IsActive)
                .Include(esa => esa.Exam)
                .Where(esa => esa.Exam.IsEnabled && esa.Exam.ExamCategory == ExamCategory.School)
                .Select(esa => esa.ExamId)
                .ToListAsync();

            _logger.LogInformation("获取学生可访问的学校统考ID列表成功，学生ID: {StudentUserId}, 学校ID: {SchoolId}, 考试数量: {Count}", 
                studentUserId, studentSchoolId.Value, examIds.Count);

            return examIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问的学校统考ID列表失败，学生ID: {StudentUserId}", studentUserId);
            return [];
        }
    }
}
