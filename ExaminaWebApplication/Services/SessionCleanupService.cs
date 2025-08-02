namespace ExaminaWebApplication.Services;

/// <summary>
/// 会话清理后台服务
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // 每小时清理一次

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("会话清理服务启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "会话清理服务执行异常");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 出错后等待5分钟再重试
            }
        }

        _logger.LogInformation("会话清理服务停止");
    }

    private async Task PerformCleanupAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

        try
        {
            // 清理过期会话
            var expiredSessions = await sessionService.CleanupExpiredSessionsAsync();
            if (expiredSessions > 0)
            {
                _logger.LogInformation("清理过期会话: {Count} 个", expiredSessions);
            }

            // 清理过期设备
            var expiredDevices = await deviceService.CleanupExpiredDevicesAsync();
            if (expiredDevices > 0)
            {
                _logger.LogInformation("清理过期设备: {Count} 个", expiredDevices);
            }

            // 清理过期验证码
            var expiredCodes = await smsService.CleanupExpiredCodesAsync();
            if (expiredCodes > 0)
            {
                _logger.LogInformation("清理过期验证码: {Count} 个", expiredCodes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行清理任务时发生异常");
        }
    }
}
