using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 教师组织关系服务实现
/// </summary>
public class TeacherOrganizationService : ITeacherOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeacherOrganizationService> _logger;

    public TeacherOrganizationService(ApplicationDbContext context, ILogger<TeacherOrganizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 教师加入班级
    /// </summary>
    public async Task<TeacherOrganizationDto?> AddTeacherToClassAsync(int teacherId, int classId, int creatorUserId, string? notes = null)
    {
        try
        {
            // 验证教师用户
            User? teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == teacherId && u.Role == UserRole.Teacher && u.IsActive);
            if (teacher == null)
            {
                _logger.LogWarning("教师用户不存在或已停用: {TeacherId}", teacherId);
                return null;
            }

            // 验证班级
            Models.Organization.Organization? classOrg = await _context.Organizations
                .Include(o => o.ParentOrganization)
                .FirstOrDefaultAsync(o => o.Id == classId && o.Type == OrganizationType.Class && o.IsActive);
            if (classOrg == null)
            {
                _logger.LogWarning("班级不存在或已停用: {ClassId}", classId);
                return null;
            }

            // 检查是否已存在关系
            bool exists = await _context.TeacherOrganizations
                .AnyAsync(to => to.TeacherId == teacherId && to.OrganizationId == classId && to.IsActive);
            if (exists)
            {
                _logger.LogWarning("教师已在班级中: {TeacherId}, {ClassId}", teacherId, classId);
                return null;
            }

            // 验证创建者
            User? creator = await _context.Users.FindAsync(creatorUserId);
            if (creator == null)
            {
                throw new ArgumentException("创建者用户不存在", nameof(creatorUserId));
            }

            TeacherOrganization teacherOrganization = new TeacherOrganization
            {
                TeacherId = teacherId,
                OrganizationId = classId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUserId,
                Notes = notes
            };

            _context.TeacherOrganizations.Add(teacherOrganization);
            await _context.SaveChangesAsync();

            _logger.LogInformation("教师加入班级成功: {TeacherId} -> {ClassId}, 创建者: {CreatorUserId}", 
                teacherId, classId, creatorUserId);

            // 获取完整的关系信息
            return await GetTeacherOrganizationDtoAsync(teacherOrganization.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "教师加入班级失败: {TeacherId} -> {ClassId}", teacherId, classId);
            throw;
        }
    }

    /// <summary>
    /// 教师退出班级
    /// </summary>
    public async Task<bool> RemoveTeacherFromClassAsync(int teacherId, int classId)
    {
        try
        {
            TeacherOrganization? teacherOrganization = await _context.TeacherOrganizations
                .FirstOrDefaultAsync(to => to.TeacherId == teacherId && to.OrganizationId == classId && to.IsActive);

            if (teacherOrganization == null)
            {
                _logger.LogWarning("教师组织关系不存在: {TeacherId}, {ClassId}", teacherId, classId);
                return false;
            }

            teacherOrganization.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("教师退出班级成功: {TeacherId} -> {ClassId}", teacherId, classId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "教师退出班级失败: {TeacherId} -> {ClassId}", teacherId, classId);
            return false;
        }
    }

    /// <summary>
    /// 获取教师的班级列表
    /// </summary>
    public async Task<List<TeacherOrganizationDto>> GetTeacherClassesAsync(int teacherId, bool includeInactive = false)
    {
        try
        {
            IQueryable<TeacherOrganization> query = _context.TeacherOrganizations
                .Include(to => to.Teacher)
                .Include(to => to.Organization)
                .ThenInclude(o => o.ParentOrganization)
                .Include(to => to.Creator)
                .Where(to => to.TeacherId == teacherId);

            if (!includeInactive)
            {
                query = query.Where(to => to.IsActive);
            }

            List<TeacherOrganization> teacherOrganizations = await query
                .OrderByDescending(to => to.JoinedAt)
                .ToListAsync();

            return [.. teacherOrganizations.Select(MapToDto)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取教师班级列表失败: {TeacherId}", teacherId);
            return new List<TeacherOrganizationDto>();
        }
    }

    /// <summary>
    /// 获取班级的教师列表
    /// </summary>
    public async Task<List<TeacherOrganizationDto>> GetClassTeachersAsync(int classId, bool includeInactive = false)
    {
        try
        {
            IQueryable<TeacherOrganization> query = _context.TeacherOrganizations
                .Include(to => to.Teacher)
                .Include(to => to.Organization)
                .ThenInclude(o => o.ParentOrganization)
                .Include(to => to.Creator)
                .Where(to => to.OrganizationId == classId);

            if (!includeInactive)
            {
                query = query.Where(to => to.IsActive);
            }

            List<TeacherOrganization> teacherOrganizations = await query
                .OrderByDescending(to => to.JoinedAt)
                .ToListAsync();

            return [.. teacherOrganizations.Select(MapToDto)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取班级教师列表失败: {ClassId}", classId);
            return new List<TeacherOrganizationDto>();
        }
    }

    /// <summary>
    /// 批量添加教师到班级
    /// </summary>
    public async Task<int> AddTeachersToClassAsync(List<int> teacherIds, int classId, int creatorUserId)
    {
        try
        {
            int successCount = 0;

            foreach (int teacherId in teacherIds)
            {
                TeacherOrganizationDto? result = await AddTeacherToClassAsync(teacherId, classId, creatorUserId);
                if (result != null)
                {
                    successCount++;
                }
            }

            _logger.LogInformation("批量添加教师到班级完成: {SuccessCount}/{TotalCount}, 班级ID: {ClassId}", 
                successCount, teacherIds.Count, classId);

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加教师到班级失败: 班级ID: {ClassId}", classId);
            throw;
        }
    }

    /// <summary>
    /// 检查教师是否在班级中
    /// </summary>
    public async Task<bool> IsTeacherInClassAsync(int teacherId, int classId)
    {
        try
        {
            return await _context.TeacherOrganizations
                .AnyAsync(to => to.TeacherId == teacherId && to.OrganizationId == classId && to.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查教师是否在班级中失败: {TeacherId}, {ClassId}", teacherId, classId);
            return false;
        }
    }

    /// <summary>
    /// 获取教师组织关系DTO
    /// </summary>
    private async Task<TeacherOrganizationDto?> GetTeacherOrganizationDtoAsync(int teacherOrganizationId)
    {
        TeacherOrganization? teacherOrganization = await _context.TeacherOrganizations
            .Include(to => to.Teacher)
            .Include(to => to.Organization)
            .ThenInclude(o => o.ParentOrganization)
            .Include(to => to.Creator)
            .FirstOrDefaultAsync(to => to.Id == teacherOrganizationId);

        return teacherOrganization == null ? null : MapToDto(teacherOrganization);
    }

    /// <summary>
    /// 将教师组织关系实体映射为DTO
    /// </summary>
    private static TeacherOrganizationDto MapToDto(TeacherOrganization teacherOrganization)
    {
        return new TeacherOrganizationDto
        {
            Id = teacherOrganization.Id,
            TeacherId = teacherOrganization.TeacherId,
            TeacherUsername = teacherOrganization.Teacher?.Username ?? "未知",
            TeacherRealName = teacherOrganization.Teacher?.RealName,
            TeacherEmail = teacherOrganization.Teacher?.Email ?? string.Empty,
            TeacherPhoneNumber = teacherOrganization.Teacher?.PhoneNumber,
            OrganizationId = teacherOrganization.OrganizationId,
            OrganizationName = teacherOrganization.Organization?.Name ?? "未知",
            OrganizationType = teacherOrganization.Organization?.Type ?? OrganizationType.Class,
            ParentOrganizationId = teacherOrganization.Organization?.ParentOrganizationId,
            ParentOrganizationName = teacherOrganization.Organization?.ParentOrganization?.Name,
            JoinedAt = teacherOrganization.JoinedAt,
            IsActive = teacherOrganization.IsActive,
            CreatedAt = teacherOrganization.CreatedAt,
            CreatorUsername = teacherOrganization.Creator?.Username ?? "未知",
            Notes = teacherOrganization.Notes
        };
    }
}
