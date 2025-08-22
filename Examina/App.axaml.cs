using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Examina.Services;
using Examina.ViewModels;
using Examina.ViewModels.Pages;
using Examina.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Examina;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        ServiceCollection services = new();

        // 配置HttpClient以正确处理HTTPS和重定向
        _ = services.AddHttpClient<IAuthenticationService, AuthenticationService>(client =>
        {
            // 强制使用HTTPS基础地址（不带尾部斜杠，避免URL构建问题）
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);

            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"=== HttpClient依赖注入配置 ===");
            System.Diagnostics.Debug.WriteLine($"配置的BaseAddress: {client.BaseAddress}");
            System.Diagnostics.Debug.WriteLine($"协议: {client.BaseAddress.Scheme}");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                // 禁用自动重定向，我们要确保直接发送HTTPS请求
                AllowAutoRedirect = false,
                // 使用系统代理
                UseProxy = true,
                // 启用SSL/TLS
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // 在生产环境中应该进行适当的证书验证
                    // 这里为了解决开发/测试环境的证书问题
                    System.Diagnostics.Debug.WriteLine($"SSL证书验证: {message.RequestUri}");
                    return true;
                }
            };
        });

        // 为OrganizationService配置HttpClient
        _ = services.AddHttpClient<IOrganizationService, OrganizationService>(client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    System.Diagnostics.Debug.WriteLine($"SSL证书验证: {message.RequestUri}");
                    return true;
                }
            };
        });

        // 注册其他服务
        _ = services.AddSingleton<IConfigurationService, ConfigurationService>();
        _ = services.AddSingleton<IDeviceService, DeviceService>();
        _ = services.AddSingleton<ISecureStorageService, SecureStorageService>();

        // 为学生端试卷服务配置HttpClient
        _ = services.AddHttpClient<IStudentExamService, StudentExamService>(client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        });

        _ = services.AddHttpClient<IStudentComprehensiveTrainingService, StudentComprehensiveTrainingService>(client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        });

        // 为学生端模拟考试服务配置HttpClient
        _ = services.AddHttpClient<IStudentMockExamService, StudentMockExamService>(client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 为排行榜服务配置HttpClient
        _ = services.AddHttpClient<RankingService>(client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 为学生端正式考试服务配置HttpClient
        _ = services.AddHttpClient<IStudentFormalExamService, StudentFormalExamService>(client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        });

        // 确保AuthenticationService为单例
        _ = services.AddSingleton<IAuthenticationService>(provider =>
        {
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AuthenticationService));
            IDeviceService deviceService = provider.GetRequiredService<IDeviceService>();
            ISecureStorageService secureStorage = provider.GetRequiredService<ISecureStorageService>();
            return new AuthenticationService(httpClient, deviceService, secureStorage);
        });

        // 注册窗口管理服务
        _ = services.AddSingleton<IWindowManagerService, WindowManagerService>();

        // 注册ViewModels
        _ = services.AddTransient<LoginViewModel>();
        _ = services.AddTransient<MainViewModel>(provider =>
        {
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            IWindowManagerService windowManager = provider.GetRequiredService<IWindowManagerService>();
            LeaderboardViewModel leaderboardFactory() => provider.GetRequiredService<LeaderboardViewModel>();
            return new MainViewModel(authService, windowManager, leaderboardFactory);
        });
        _ = services.AddTransient<UserInfoCompletionViewModel>();
        _ = services.AddTransient<LoadingViewModel>();
        _ = services.AddTransient<ProfileViewModel>();
        _ = services.AddTransient<ChangePasswordViewModel>();
        _ = services.AddTransient<SchoolBindingViewModel>();
        _ = services.AddTransient<ExamListViewModel>(provider =>
        {
            IStudentExamService examService = provider.GetRequiredService<IStudentExamService>();
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            return new ExamListViewModel(examService, authService);
        });
        _ = services.AddTransient<ComprehensiveTrainingListViewModel>(provider =>
        {
            IStudentComprehensiveTrainingService trainingService = provider.GetRequiredService<IStudentComprehensiveTrainingService>();
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            return new ComprehensiveTrainingListViewModel(trainingService, authService);
        });
        _ = services.AddTransient<MockExamViewModel>(provider =>
        {
            IStudentMockExamService mockExamService = provider.GetRequiredService<IStudentMockExamService>();
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            return new MockExamViewModel(mockExamService, authService);
        });
        _ = services.AddTransient<ExamViewModel>(provider =>
        {
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            return new ExamViewModel(authService);
        });
        _ = services.AddTransient<LeaderboardViewModel>(provider =>
        {
            RankingService rankingService = provider.GetRequiredService<RankingService>();
            ILogger<LeaderboardViewModel> logger = provider.GetRequiredService<ILogger<LeaderboardViewModel>>();
            return new LeaderboardViewModel(rankingService, logger);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 创建加载窗口，开始自动认证流程
            LoadingViewModel? loadingViewModel = _serviceProvider?.GetService<LoadingViewModel>();
            desktop.MainWindow = new LoadingWindow(loadingViewModel!);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }
}
