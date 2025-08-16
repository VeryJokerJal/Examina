using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.Organization;

/// <summary>
/// 用户管理服务实现
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(ApplicationDbContext context, ILogger<UserManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 创建学生用户
    /// </summary>
    public async Task<UserDto?> CreateStudentUserAsync(string username, string email, string? phoneNumber, string password, string? realName = null, string? studentId = null, int? creatorUserId = null)
    {
        try
        {
            // 检查用户名和邮箱是否已存在
            bool userExists = await _context.Users
                .AnyAsync(u => u.Username == username || u.Email == email);
            if (userExists)
            {
                _logger.LogWarning("用户名或邮箱已存在: {Username}, {Email}", username, email);
                return null;
            }

            User user = new User
            {
                Username = username,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Student,
                RealName = realName,
                StudentId = studentId,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AllowMultipleDevices = false,
                MaxDeviceCount = 1
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("创建学生用户成功: {Username}, 创建者: {CreatorUserId}", username, creatorUserId);

            return await GetUserDtoAsync(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建学生用户失败: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// 创建教师用户
    /// </summary>
    public async Task<UserDto?> CreateTeacherUserAsync(string username, string email, string? phoneNumber, string password, string? realName = null, string? employeeId = null, int? schoolId = null, List<int>? classIds = null, int? creatorUserId = null)
    {
        try
        {
            // 检查用户名和邮箱是否已存在
            bool userExists = await _context.Users
                .AnyAsync(u => u.Username == username || u.Email == email);
            if (userExists)
            {
                _logger.LogWarning("用户名或邮箱已存在: {Username}, {Email}", username, email);
                return null;
            }

            User user = new User
            {
                Username = username,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Teacher,
                RealName = realName,
                StudentId = employeeId, // 对于教师，这个字段存储工号
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AllowMultipleDevices = true,
                MaxDeviceCount = 3
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 如果指定了班级，建立教师-班级关系
            if (classIds != null && classIds.Count > 0 && creatorUserId.HasValue)
            {
                ILogger<TeacherOrganizationService> teacherOrgLogger =
                    Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole())
                        .CreateLogger<TeacherOrganizationService>();
                ITeacherOrganizationService teacherOrgService = new TeacherOrganizationService(_context, teacherOrgLogger);
                foreach (int classId in classIds)
                {
                    await teacherOrgService.AddTeacherToClassAsync(user.Id, classId, creatorUserId.Value);
                }
            }

            _logger.LogInformation("创建教师用户成功: {Username}, 学校ID: {SchoolId}, 创建者: {CreatorUserId}", 
                username, schoolId, creatorUserId);

            return await GetUserDtoAsync(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建教师用户失败: {Username}", username);
            throw;
        }
    }

    /// <summary>
    /// 更新用户基本信息
    /// </summary>
    public async Task<UserDto?> UpdateUserAsync(int userId, string? email = null, string? phoneNumber = null, string? realName = null, string? studentId = null, int? updaterUserId = null)
    {
        try
        {
            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("用户不存在或已停用: {UserId}", userId);
                return null;
            }

            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
            }
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                user.PhoneNumber = phoneNumber;
            }
            if (!string.IsNullOrEmpty(realName))
            {
                user.RealName = realName;
            }
            if (!string.IsNullOrEmpty(studentId))
            {
                user.StudentId = studentId;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("更新用户成功: {UserId}, 更新者: {UpdaterUserId}", userId, updaterUserId);

            return await GetUserDtoAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    public async Task<bool> DeactivateUserAsync(int userId, int updaterUserId)
    {
        try
        {
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", userId);
                return false;
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("停用用户成功: {UserId}, 操作者: {UpdaterUserId}", userId, updaterUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用用户失败: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    public async Task<bool> ActivateUserAsync(int userId, int updaterUserId)
    {
        try
        {
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", userId);
                return false;
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("激活用户成功: {UserId}, 操作者: {UpdaterUserId}", userId, updaterUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活用户失败: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword, int updaterUserId)
    {
        try
        {
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("重置用户密码成功: {UserId}, 操作者: {UpdaterUserId}", userId, updaterUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置用户密码失败: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<List<UserDto>> GetUsersAsync(UserRole? role = null, bool includeInactive = false, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            IQueryable<User> query = _context.Users;

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            List<User> users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<UserDto> userDtos = new List<UserDto>();
            foreach (User user in users)
            {
                UserDto? dto = await GetUserDtoAsync(user.Id);
                if (dto != null)
                {
                    userDtos.Add(dto);
                }
            }

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return new List<UserDto>();
        }
    }

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            return await GetUserDtoAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详情失败: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// 搜索用户
    /// </summary>
    public async Task<List<UserDto>> SearchUsersAsync(string keyword, UserRole? role = null, bool includeInactive = false)
    {
        try
        {
            IQueryable<User> query = _context.Users
                .Where(u => u.Username.Contains(keyword) || 
                           u.Email.Contains(keyword) || 
                           (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword)) ||
                           (u.RealName != null && u.RealName.Contains(keyword)));

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(u => u.IsActive);
            }

            List<User> users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            List<UserDto> userDtos = new List<UserDto>();
            foreach (User user in users)
            {
                UserDto? dto = await GetUserDtoAsync(user.Id);
                if (dto != null)
                {
                    userDtos.Add(dto);
                }
            }

            return userDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
            return new List<UserDto>();
        }
    }

    /// <summary>
    /// 获取教师用户列表
    /// </summary>
    public async Task<List<UserDto>> GetTeachersAsync(bool includeInactive = false)
    {
        return await GetUsersAsync(UserRole.Teacher, includeInactive);
    }

    /// <summary>
    /// 获取学生用户列表
    /// </summary>
    public async Task<List<UserDto>> GetStudentsAsync(bool includeInactive = false)
    {
        return await GetUsersAsync(UserRole.Student, includeInactive);
    }

    /// <summary>
    /// 获取用户DTO
    /// </summary>
    private async Task<UserDto?> GetUserDtoAsync(int userId)
    {
        User? user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            RealName = user.RealName,
            StudentId = user.StudentId,
            IsFirstLogin = user.IsFirstLogin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            AllowMultipleDevices = user.AllowMultipleDevices,
            MaxDeviceCount = user.MaxDeviceCount,
            Schools = new List<OrganizationDto>(),
            Classes = new List<OrganizationDto>()
        };
    }
}
