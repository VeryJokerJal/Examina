using System.Reflection;

namespace Examina.Services;

/// <summary>
/// 配置服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    public string ApiBaseUrl => "https://qiuzhenbd.com/api";

    public string StudentAuthEndpoint => "student/auth";

    public string AdminAuthEndpoint => "admin/auth";

    public string ApplicationName => "Examina Desktop Client";

    public string ApplicationVersion
    {
        get
        {
            try
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }
    }

    public bool IsDebugMode =>
#if DEBUG
            true;
#else
            return false;
#endif


    public int TokenRefreshThresholdMinutes => 30;

    public bool AutoLoginEnabled => true;
}
