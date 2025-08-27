using ExaminaWebApplication.Filters;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Admin;
using ExaminaWebApplication.Services.Admin;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers.Api;

/// <summary>
/// 管理员配置管理API控制器
/// </summary>
[ApiController]
[Route("api/admin/configuration")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[RequireLogin]
public class AdminConfigurationApiController : ControllerBase
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly ILogger<AdminConfigurationApiController> _logger;

    public AdminConfigurationApiController(
        ISystemConfigurationService configurationService,
        ILogger<AdminConfigurationApiController> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// 检查用户是否为管理员
    /// </summary>
    private bool IsAdministrator()
    {
        return User.IsInRole(UserRole.Administrator.ToString());
    }

    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SystemConfigurationDto>>> GetAllConfigurations()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        try
        {
            List<SystemConfigurationDto> configurations = await _configurationService.GetAllConfigurationsAsync();
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有系统配置失败");
            return StatusCode(500, new { message = "获取系统配置失败" });
        }
    }

    /// <summary>
    /// 根据分类获取系统配置
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<List<SystemConfigurationDto>>> GetConfigurationsByCategory(string category)
    {
        try
        {
            List<SystemConfigurationDto> configurations = await _configurationService.GetConfigurationsByCategoryAsync(category);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据分类获取系统配置失败，分类: {Category}", category);
            return StatusCode(500, new { message = "获取系统配置失败" });
        }
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    [HttpGet("value/{configKey}")]
    public async Task<ActionResult<string>> GetConfigurationValue(string configKey)
    {
        try
        {
            string? value = await _configurationService.GetConfigurationValueAsync(configKey);
            if (value == null)
            {
                return NotFound(new { message = "配置不存在" });
            }

            return Ok(new { configKey, value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配置值失败，键名: {ConfigKey}", configKey);
            return StatusCode(500, new { message = "获取配置值失败" });
        }
    }

    /// <summary>
    /// 获取设备限制配置
    /// </summary>
    [HttpGet("device-limit")]
    public async Task<ActionResult<DeviceLimitConfigurationModel>> GetDeviceLimitConfiguration()
    {
        try
        {
            DeviceLimitConfigurationModel configuration = await _configurationService.GetDeviceLimitConfigurationAsync();
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备限制配置失败");
            return StatusCode(500, new { message = "获取设备限制配置失败" });
        }
    }

    /// <summary>
    /// 更新设备限制配置
    /// </summary>
    [HttpPost("device-limit")]
    public async Task<ActionResult> UpdateDeviceLimitConfiguration([FromBody] DeviceLimitConfigurationModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "配置参数无效", errors = ModelState });
            }

            // 获取当前用户ID
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            bool success = await _configurationService.UpdateDeviceLimitConfigurationAsync(model, userId);

            if (success)
            {
                _logger.LogInformation("管理员 {UserId} 通过API更新设备限制配置成功", userId);
                return Ok(new { message = "设备限制配置更新成功" });
            }
            else
            {
                _logger.LogWarning("管理员 {UserId} 通过API更新设备限制配置失败", userId);
                return StatusCode(500, new { message = "设备限制配置更新失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过API更新设备限制配置失败");
            return StatusCode(500, new { message = "更新设备限制配置失败" });
        }
    }

    /// <summary>
    /// 更新单个配置
    /// </summary>
    [HttpPut("{configKey}")]
    public async Task<ActionResult> UpdateConfiguration(string configKey, [FromBody] UpdateSystemConfigurationRequest request)
    {
        try
        {
            if (configKey != request.ConfigKey)
            {
                return BadRequest(new { message = "配置键名不匹配" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "配置参数无效", errors = ModelState });
            }

            // 获取当前用户ID
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            bool success = await _configurationService.UpdateConfigurationAsync(
                request.ConfigKey, request.ConfigValue, userId, request.Description);

            if (success)
            {
                _logger.LogInformation("管理员 {UserId} 通过API更新配置成功，键名: {ConfigKey}", userId, configKey);
                return Ok(new { message = "配置更新成功" });
            }
            else
            {
                _logger.LogWarning("管理员 {UserId} 通过API更新配置失败，键名: {ConfigKey}", userId, configKey);
                return StatusCode(500, new { message = "配置更新失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过API更新配置失败，键名: {ConfigKey}", configKey);
            return StatusCode(500, new { message = "更新配置失败" });
        }
    }

    /// <summary>
    /// 批量更新配置
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult> UpdateConfigurations([FromBody] List<UpdateSystemConfigurationRequest> requests)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "配置参数无效", errors = ModelState });
            }

            // 获取当前用户ID
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            bool success = await _configurationService.UpdateConfigurationsAsync(requests, userId);

            if (success)
            {
                _logger.LogInformation("管理员 {UserId} 通过API批量更新配置成功，数量: {Count}", userId, requests.Count);
                return Ok(new { message = "配置批量更新成功" });
            }
            else
            {
                _logger.LogWarning("管理员 {UserId} 通过API批量更新配置失败，数量: {Count}", userId, requests.Count);
                return StatusCode(500, new { message = "配置批量更新失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过API批量更新配置失败");
            return StatusCode(500, new { message = "批量更新配置失败" });
        }
    }

    /// <summary>
    /// 创建新配置
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateConfiguration([FromBody] CreateConfigurationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "配置参数无效", errors = ModelState });
            }

            // 获取当前用户ID
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "用户身份验证失败" });
            }

            bool success = await _configurationService.CreateConfigurationAsync(
                request.ConfigKey, request.ConfigValue, request.Description, request.Category, userId);

            if (success)
            {
                _logger.LogInformation("管理员 {UserId} 通过API创建配置成功，键名: {ConfigKey}", userId, request.ConfigKey);
                return Ok(new { message = "配置创建成功" });
            }
            else
            {
                _logger.LogWarning("管理员 {UserId} 通过API创建配置失败，键名: {ConfigKey}", userId, request.ConfigKey);
                return StatusCode(500, new { message = "配置创建失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过API创建配置失败");
            return StatusCode(500, new { message = "创建配置失败" });
        }
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    [HttpDelete("{configKey}")]
    public async Task<ActionResult> DeleteConfiguration(string configKey)
    {
        try
        {
            bool success = await _configurationService.DeleteConfigurationAsync(configKey);

            if (success)
            {
                _logger.LogInformation("通过API删除配置成功，键名: {ConfigKey}", configKey);
                return Ok(new { message = "配置删除成功" });
            }
            else
            {
                _logger.LogWarning("通过API删除配置失败，键名: {ConfigKey}", configKey);
                return StatusCode(500, new { message = "配置删除失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过API删除配置失败，键名: {ConfigKey}", configKey);
            return StatusCode(500, new { message = "删除配置失败" });
        }
    }
}

/// <summary>
/// 创建配置请求模型
/// </summary>
public class CreateConfigurationRequest
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
}
