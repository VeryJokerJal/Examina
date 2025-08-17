namespace ExaminaWebApplication.Models.Organization.Dto;

/// <summary>
/// 用户DTO
/// </summary>
public class UserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 是否首次登录
    /// </summary>
    public bool IsFirstLogin { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 是否允许多设备登录
    /// </summary>
    public bool AllowMultipleDevices { get; set; }

    /// <summary>
    /// 最大设备数量
    /// </summary>
    public int MaxDeviceCount { get; set; }

    /// <summary>
    /// 所属学校列表（教师可能属于多个学校）
    /// </summary>
    public List<OrganizationDto> Schools { get; set; } = [];

    /// <summary>
    /// 所属班级列表（教师可能属于多个班级）
    /// </summary>
    public List<OrganizationDto> Classes { get; set; } = [];
}
