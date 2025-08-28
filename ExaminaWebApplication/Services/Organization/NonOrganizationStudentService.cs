using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 非组织学生管理服务实现
/// </summary>
public class NonOrganizationStudentService : INonOrganizationStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NonOrganizationStudentService> _logger;

    public NonOrganizationStudentService(ApplicationDbContext context, ILogger<NonOrganizationStudentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 创建非组织学生
    /// </summary>
    public async Task<NonOrganizationStudentDto?> CreateStudentAsync(string realName, string phoneNumber, int creatorUserId, string? notes = null)
    {
        try
        {
            // 验证创建者
            User? creator = await _context.Users.FindAsync(creatorUserId);
            if (creator == null)
            {
                throw new ArgumentException("创建者用户不存在", nameof(creatorUserId));
            }

            // 检查手机号是否已存在
            bool phoneExists = await _context.NonOrganizationStudents
                .AnyAsync(s => s.PhoneNumber == phoneNumber && s.IsActive);
            if (phoneExists)
            {
                _logger.LogWarning("手机号已存在: {PhoneNumber}", phoneNumber);
                return null;
            }

            NonOrganizationStudent student = new NonOrganizationStudent
            {
                RealName = realName,
                PhoneNumber = phoneNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUserId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = creatorUserId,
                IsActive = true,
                Notes = notes
            };

            _context.NonOrganizationStudents.Add(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("创建非组织学生成功: {RealName}, {PhoneNumber}, 创建者: {CreatorUserId}", 
                realName, phoneNumber, creatorUserId);

            return await GetStudentDtoAsync(student.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建非组织学生失败: {RealName}, {PhoneNumber}", realName, phoneNumber);
            throw;
        }
    }

    /// <summary>
    /// 更新非组织学生信息
    /// </summary>
    public async Task<NonOrganizationStudentDto?> UpdateStudentAsync(int studentId, string realName, string phoneNumber, int updaterUserId, string? notes = null)
    {
        try
        {
            NonOrganizationStudent? student = await _context.NonOrganizationStudents
                .FirstOrDefaultAsync(s => s.Id == studentId && s.IsActive);
            if (student == null)
            {
                _logger.LogWarning("非组织学生不存在: {StudentId}", studentId);
                return null;
            }

            // 验证更新者
            User? updater = await _context.Users.FindAsync(updaterUserId);
            if (updater == null)
            {
                throw new ArgumentException("更新者用户不存在", nameof(updaterUserId));
            }

            // 检查手机号是否已被其他学生使用
            bool phoneExists = await _context.NonOrganizationStudents
                .AnyAsync(s => s.PhoneNumber == phoneNumber && s.Id != studentId && s.IsActive);
            if (phoneExists)
            {
                _logger.LogWarning("手机号已被其他学生使用: {PhoneNumber}", phoneNumber);
                return null;
            }

            student.RealName = realName;
            student.PhoneNumber = phoneNumber;
            student.UpdatedAt = DateTime.UtcNow;
            student.UpdatedBy = updaterUserId;
            student.Notes = notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("更新非组织学生成功: {StudentId}, 更新者: {UpdaterUserId}", studentId, updaterUserId);

            return await GetStudentDtoAsync(studentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新非组织学生失败: {StudentId}", studentId);
            throw;
        }
    }

    /// <summary>
    /// 删除非组织学生（软删除）
    /// </summary>
    public async Task<bool> DeleteStudentAsync(int studentId, int updaterUserId)
    {
        try
        {
            NonOrganizationStudent? student = await _context.NonOrganizationStudents
                .FirstOrDefaultAsync(s => s.Id == studentId && s.IsActive);
            if (student == null)
            {
                _logger.LogWarning("非组织学生不存在: {StudentId}", studentId);
                return false;
            }

            // 验证更新者
            User? updater = await _context.Users.FindAsync(updaterUserId);
            if (updater == null)
            {
                throw new ArgumentException("更新者用户不存在", nameof(updaterUserId));
            }

            student.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;
            student.UpdatedBy = updaterUserId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("删除非组织学生成功: {StudentId}, 操作者: {UpdaterUserId}", studentId, updaterUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除非组织学生失败: {StudentId}", studentId);
            return false;
        }
    }

    /// <summary>
    /// 获取非组织学生列表
    /// </summary>
    public async Task<List<NonOrganizationStudentDto>> GetStudentsAsync(bool includeInactive = false, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            IQueryable<NonOrganizationStudent> query = _context.NonOrganizationStudents
                .Include(s => s.Creator)
                .Include(s => s.Updater);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            List<NonOrganizationStudent> students = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return [.. students.Select(MapToDto)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取非组织学生列表失败");
            return new List<NonOrganizationStudentDto>();
        }
    }

    /// <summary>
    /// 根据ID获取非组织学生
    /// </summary>
    public async Task<NonOrganizationStudentDto?> GetStudentByIdAsync(int studentId)
    {
        try
        {
            return await GetStudentDtoAsync(studentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取非组织学生详情失败: {StudentId}", studentId);
            return null;
        }
    }

    /// <summary>
    /// 根据手机号搜索非组织学生
    /// </summary>
    public async Task<List<NonOrganizationStudentDto>> SearchStudentsByPhoneAsync(string phoneNumber, bool includeInactive = false)
    {
        try
        {
            IQueryable<NonOrganizationStudent> query = _context.NonOrganizationStudents
                .Include(s => s.Creator)
                .Include(s => s.Updater)
                .Where(s => s.PhoneNumber.Contains(phoneNumber));

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            List<NonOrganizationStudent> students = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return [.. students.Select(MapToDto)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据手机号搜索非组织学生失败: {PhoneNumber}", phoneNumber);
            return new List<NonOrganizationStudentDto>();
        }
    }

    /// <summary>
    /// 根据姓名搜索非组织学生
    /// </summary>
    public async Task<List<NonOrganizationStudentDto>> SearchStudentsByNameAsync(string realName, bool includeInactive = false)
    {
        try
        {
            IQueryable<NonOrganizationStudent> query = _context.NonOrganizationStudents
                .Include(s => s.Creator)
                .Include(s => s.Updater)
                .Where(s => s.RealName.Contains(realName));

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            List<NonOrganizationStudent> students = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return [.. students.Select(MapToDto)];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据姓名搜索非组织学生失败: {RealName}", realName);
            return new List<NonOrganizationStudentDto>();
        }
    }



    /// <summary>
    /// 获取非组织学生总数
    /// </summary>
    public async Task<int> GetStudentCountAsync(bool includeInactive = false)
    {
        try
        {
            IQueryable<NonOrganizationStudent> query = _context.NonOrganizationStudents;

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取非组织学生总数失败");
            return 0;
        }
    }

    /// <summary>
    /// 获取学生DTO
    /// </summary>
    private async Task<NonOrganizationStudentDto?> GetStudentDtoAsync(int studentId)
    {
        NonOrganizationStudent? student = await _context.NonOrganizationStudents
            .Include(s => s.Creator)
            .Include(s => s.Updater)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        return student == null ? null : MapToDto(student);
    }

    /// <summary>
    /// 将非组织学生实体映射为DTO
    /// </summary>
    private static NonOrganizationStudentDto MapToDto(NonOrganizationStudent student)
    {
        return new NonOrganizationStudentDto
        {
            Id = student.Id,
            RealName = student.RealName,
            PhoneNumber = student.PhoneNumber,
            CreatedAt = student.CreatedAt,
            CreatorUsername = student.Creator?.Username ?? "未知",
            UpdatedAt = student.UpdatedAt,
            UpdaterUsername = student.Updater?.Username ?? "未知",
            IsActive = student.IsActive,
            UserId = null, // 不再关联用户
            Username = null, // 不再关联用户
            Notes = student.Notes
        };
    }
}
