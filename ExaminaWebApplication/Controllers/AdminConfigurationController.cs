using ExaminaWebApplication.Filters;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Admin;
using ExaminaWebApplication.Services.Admin;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员配置管理控制器
/// </summary>
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[RequireLogin]
public class AdminConfigurationController : Controller
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly ILogger<AdminConfigurationController> _logger;

    public AdminConfigurationController(
        ISystemConfigurationService configurationService,
        ILogger<AdminConfigurationController> logger)
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
    /// 配置管理主页
    /// </summary>
    public async Task<IActionResult> Index()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        try
        {
            AdminConfigurationViewModel viewModel = new();

            // 获取所有配置
            List<SystemConfigurationDto> allConfigurations = await _configurationService.GetAllConfigurationsAsync();
            viewModel.AllConfigurations = allConfigurations;

            // 获取设备限制配置
            DeviceLimitConfigurationModel deviceConfig = await _configurationService.GetDeviceLimitConfigurationAsync();
            viewModel.DeviceLimitConfiguration = deviceConfig;

            // 按分类分组配置
            viewModel.ConfigurationsByCategory = allConfigurations
                .GroupBy(c => c.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置管理页面失败");
            TempData["ErrorMessage"] = "加载配置管理页面失败，请稍后重试";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// 设备限制配置页面
    /// </summary>
    public async Task<IActionResult> DeviceLimit()
    {
        if (!IsAdministrator())
        {
            return Forbid();
        }

        try
        {
            DeviceLimitConfigurationModel configuration = await _configurationService.GetDeviceLimitConfigurationAsync();
            return View(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载设备限制配置页面失败");
            TempData["ErrorMessage"] = "加载设备限制配置页面失败，请稍后重试";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// 更新设备限制配置
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeviceLimit(DeviceLimitConfigurationModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "配置参数无效，请检查输入";
                return View("DeviceLimit", model);
            }

            // 获取当前用户ID
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "用户身份验证失败";
                return RedirectToAction("Index");
            }

            bool success = await _configurationService.UpdateDeviceLimitConfigurationAsync(model, userId);

            if (success)
            {
                TempData["SuccessMessage"] = "设备限制配置更新成功";
                _logger.LogInformation("管理员 {UserId} 更新设备限制配置成功", userId);
            }
            else
            {
                TempData["ErrorMessage"] = "设备限制配置更新失败，请稍后重试";
                _logger.LogWarning("管理员 {UserId} 更新设备限制配置失败", userId);
            }

            return RedirectToAction("DeviceLimit");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备限制配置失败");
            TempData["ErrorMessage"] = "更新设备限制配置失败，请稍后重试";
            return View("DeviceLimit", model);
        }
    }

    /// <summary>
    /// 系统配置列表页面
    /// </summary>
    public async Task<IActionResult> SystemConfigurations(string? category = null)
    {
        try
        {
            List<SystemConfigurationDto> configurations;

            if (string.IsNullOrEmpty(category))
            {
                configurations = await _configurationService.GetAllConfigurationsAsync();
            }
            else
            {
                configurations = await _configurationService.GetConfigurationsByCategoryAsync(category);
            }

            ViewBag.CurrentCategory = category;
            ViewBag.Categories = configurations.Select(c => c.Category).Distinct().OrderBy(c => c).ToList();

            return View(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载系统配置列表失败，分类: {Category}", category);
            TempData["ErrorMessage"] = "加载系统配置列表失败，请稍后重试";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// 编辑配置页面
    /// </summary>
    public async Task<IActionResult> EditConfiguration(string configKey)
    {
        try
        {
            if (string.IsNullOrEmpty(configKey))
            {
                TempData["ErrorMessage"] = "配置键名不能为空";
                return RedirectToAction("SystemConfigurations");
            }

            List<SystemConfigurationDto> allConfigurations = await _configurationService.GetAllConfigurationsAsync();
            SystemConfigurationDto? configuration = allConfigurations.FirstOrDefault(c => c.ConfigKey == configKey);

            if (configuration == null)
            {
                TempData["ErrorMessage"] = "配置不存在";
                return RedirectToAction("SystemConfigurations");
            }

            UpdateSystemConfigurationRequest model = new()
            {
                ConfigKey = configuration.ConfigKey,
                ConfigValue = configuration.ConfigValue,
                Description = configuration.Description
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载编辑配置页面失败，配置键: {ConfigKey}", configKey);
            TempData["ErrorMessage"] = "加载编辑配置页面失败，请稍后重试";
            return RedirectToAction("SystemConfigurations");
        }
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateConfiguration(UpdateSystemConfigurationRequest model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "配置参数无效，请检查输入";
                return View("EditConfiguration", model);
            }

            // 获取当前用户ID
            Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["ErrorMessage"] = "用户身份验证失败";
                return RedirectToAction("SystemConfigurations");
            }

            bool success = await _configurationService.UpdateConfigurationAsync(
                model.ConfigKey, model.ConfigValue, userId, model.Description);

            if (success)
            {
                TempData["SuccessMessage"] = "配置更新成功";
                _logger.LogInformation("管理员 {UserId} 更新配置成功，键名: {ConfigKey}", userId, model.ConfigKey);
            }
            else
            {
                TempData["ErrorMessage"] = "配置更新失败，请稍后重试";
                _logger.LogWarning("管理员 {UserId} 更新配置失败，键名: {ConfigKey}", userId, model.ConfigKey);
            }

            return RedirectToAction("SystemConfigurations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新配置失败，键名: {ConfigKey}", model.ConfigKey);
            TempData["ErrorMessage"] = "更新配置失败，请稍后重试";
            return View("EditConfiguration", model);
        }
    }
}
