using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
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
    public async Task<UserDto?> CreateStudentUserAsync(string username, string email, string? phoneNumber, string password, string? realName = null, int? creatorUserId = null)
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

            User user = new()
            {
                Username = username,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Student,
                RealName = realName,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AllowMultipleDevices = false,
                MaxDeviceCount = 1
            };

            _ = _context.Users.Add(user);
            _ = await _context.SaveChangesAsync();

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
    public async Task<UserDto?> CreateTeacherUserAsync(string username, string email, string? phoneNumber, string password, string? realName = null, int? schoolId = null, List<int>? classIds = null, int? creatorUserId = null)
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

            User user = new()
            {
                Username = username,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Teacher,
                RealName = realName,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AllowMultipleDevices = true,
                MaxDeviceCount = 3
            };

            _ = _context.Users.Add(user);
            _ = await _context.SaveChangesAsync();

            // 如果指定了班级，建立教师-班级关系
            if (classIds != null && classIds.Count > 0 && creatorUserId.HasValue)
            {
                ILogger<TeacherOrganizationService> teacherOrgLogger =
                    Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole())
                        .CreateLogger<TeacherOrganizationService>();
                ITeacherOrganizationService teacherOrgService = new TeacherOrganizationService(_context, teacherOrgLogger);
                foreach (int classId in classIds)
                {
                    _ = await teacherOrgService.AddTeacherToClassAsync(user.Id, classId, creatorUserId.Value);
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
    public async Task<UserDto?> UpdateUserAsync(int userId, string? email = null, string? phoneNumber = null, string? realName = null, int? updaterUserId = null)
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
            // 移除StudentId的更新逻辑，不再处理该字段

            user.UpdatedAt = DateTime.UtcNow;
            _ = await _context.SaveChangesAsync();

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
            _ = await _context.SaveChangesAsync();

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
            _ = await _context.SaveChangesAsync();

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
            _ = await _context.SaveChangesAsync();

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

            List<UserDto> userDtos = [];
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
            return [];
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

            List<UserDto> userDtos = [];
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
            return [];
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

        // 检查用户是否为组织成员
        bool isOrganizationMember = await IsUserOrganizationMemberAsync(userId);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            RealName = user.RealName,
            IsFirstLogin = user.IsFirstLogin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            AllowMultipleDevices = user.AllowMultipleDevices,
            MaxDeviceCount = user.MaxDeviceCount,
            Schools = [],
            Classes = [],
            IsOrganizationMember = isOrganizationMember
        };
    }

    /// <summary>
    /// 检查用户是否为组织成员
    /// </summary>
    public async Task<bool> IsUserOrganizationMemberAsync(int userId)
    {
        try
        {
            // 检查学生组织关系
            bool hasStudentOrganization = await _context.StudentOrganizations
                .AnyAsync(so => so.StudentId == userId && so.IsActive);

            // 检查教师组织关系
            bool hasTeacherOrganization = await _context.TeacherOrganizations
                .AnyAsync(to => to.TeacherId == userId && to.IsActive);

            return hasStudentOrganization || hasTeacherOrganization;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户组织成员身份失败: 用户ID: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 切换用户的组织成员身份
    /// </summary>
    public async Task<(bool Success, string Message)> ToggleOrganizationMembershipAsync(int userId, int operatorUserId)
    {
        try
        {
            // 验证用户存在
            User? user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("用户不存在或已停用: {UserId}", userId);
                return (false, "用户不存在或已停用");
            }

            // 验证操作者权限
            User? operatorUser = await _context.Users.FindAsync(operatorUserId);
            if (operatorUser == null || !operatorUser.IsActive ||
                (operatorUser.Role != UserRole.Administrator && operatorUser.Role != UserRole.Teacher))
            {
                _logger.LogWarning("操作者权限不足: {OperatorUserId}", operatorUserId);
                return (false, "操作者权限不足");
            }

            bool isCurrentlyMember = await IsUserOrganizationMemberAsync(userId);

            if (isCurrentlyMember)
            {
                // 移出组织：将用户从所有组织中移除，并添加到非组织成员名单
                await RemoveUserFromAllOrganizationsAsync(userId);
                await AddUserToNonOrganizationMembersAsync(userId, operatorUserId);
                _logger.LogInformation("用户已从所有组织中移除并转为非组织成员: {UserId}, 操作者: {OperatorUserId}", userId, operatorUserId);
                return (true, "用户已从所有组织中移除并转为非组织成员");
            }
            else
            {
                // 加入非组织成员名单：将用户添加到OrganizationMember表中，OrganizationId为null
                bool addResult = await AddUserToNonOrganizationMembersAsync(userId, operatorUserId);
                if (addResult)
                {
                    _logger.LogInformation("用户已添加到非组织成员名单: {UserId}, 操作者: {OperatorUserId}", userId, operatorUserId);
                    return (true, "用户已添加到非组织成员名单");
                }
                else
                {
                    _logger.LogWarning("用户已在非组织成员名单中: {UserId}", userId);
                    return (true, "用户已在非组织成员名单中");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户组织成员身份失败: 用户ID: {UserId}", userId);
            return (false, $"操作失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 将用户从所有组织中移除
    /// </summary>
    private async Task RemoveUserFromAllOrganizationsAsync(int userId)
    {
        // 移除学生组织关系
        List<StudentOrganization> studentOrganizations = await _context.StudentOrganizations
            .Where(so => so.StudentId == userId && so.IsActive)
            .ToListAsync();

        foreach (StudentOrganization so in studentOrganizations)
        {
            so.IsActive = false;
        }

        // 移除教师组织关系
        List<TeacherOrganization> teacherOrganizations = await _context.TeacherOrganizations
            .Where(to => to.TeacherId == userId && to.IsActive)
            .ToListAsync();

        foreach (TeacherOrganization to in teacherOrganizations)
        {
            to.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 将用户添加到非组织成员名单
    /// </summary>
    private async Task<bool> AddUserToNonOrganizationMembersAsync(int userId, int operatorUserId)
    {
        try
        {
            // 获取用户信息
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户不存在，无法添加到非组织成员名单: {UserId}", userId);
                return false;
            }

            // 检查用户是否已在NonOrganizationStudent表中
            NonOrganizationStudent? existingNonOrgStudent = await _context.NonOrganizationStudents
                .FirstOrDefaultAsync(nos => nos.UserId == userId && nos.IsActive);

            if (existingNonOrgStudent != null)
            {
                _logger.LogInformation("用户已在非组织学生名单中: {UserId}", userId);
                return false; // 已存在，不需要重复添加
            }

            // 检查用户是否已在OrganizationMember表中
            OrganizationMember? existingMember = await _context.OrganizationMembers
                .FirstOrDefaultAsync(om => om.UserId == userId && om.IsActive);

            if (existingMember != null)
            {
                // 如果已存在，确保OrganizationId为null（非组织成员状态）
                if (existingMember.OrganizationId == null)
                {
                    _logger.LogInformation("用户已在非组织成员名单中: {UserId}", userId);
                }
                else
                {
                    // 将现有记录转为非组织成员
                    existingMember.OrganizationId = null;
                    existingMember.UpdatedAt = DateTime.UtcNow;
                    existingMember.UpdatedBy = operatorUserId;
                    _logger.LogInformation("已将现有组织成员记录转为非组织成员: {UserId}", userId);
                }
            }
            else
            {
                // 创建新的非组织成员记录
                OrganizationMember newMember = new OrganizationMember
                {
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber,
                    RealName = user.RealName,
                    OrganizationId = null, // 非组织成员
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = operatorUserId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = operatorUserId,
                    IsActive = true
                };

                _context.OrganizationMembers.Add(newMember);
                _logger.LogInformation("创建新的非组织成员记录: {UserId}", userId);
            }

            // 同时在NonOrganizationStudent表中创建记录，以便在NonOrganizationStudent页面显示
            NonOrganizationStudent newNonOrgStudent = new NonOrganizationStudent
            {
                RealName = user.RealName ?? user.Username,
                PhoneNumber = user.PhoneNumber ?? "",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = operatorUserId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = operatorUserId,
                IsActive = true,
                Notes = "通过用户管理界面添加的非组织成员"
            };

            _context.NonOrganizationStudents.Add(newNonOrgStudent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功添加用户到非组织成员名单: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加用户到非组织成员名单失败: {UserId}", userId);
            return false;
        }
    }
}
