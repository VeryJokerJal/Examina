using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Examina.Services;
using Examina.ViewModels;
using Examina.ViewModels.FileDownload;
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

        // 为AuthenticationService注册命名HttpClient（避免重复注册IAuthenticationService导致多实例）
        _ = services.AddHttpClient(nameof(AuthenticationService), client =>
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

        // 注册日志服务
        _ = services.AddLogging(builder =>
        {
            _ = builder.SetMinimumLevel(LogLevel.Information);
        });

        // 注册BenchSuite相关服务
        _ = services.ConfigureExaminaServices();

        // 注册其他服务
        _ = services.AddSingleton<IConfigurationService, ConfigurationService>();
        _ = services.AddSingleton<IDeviceService, DeviceService>();
        _ = services.AddSingleton<ISecureStorageService, SecureStorageService>();

        // 为文件下载服务配置HttpClient
        _ = services.AddHttpClient("FileDownloadService", client =>
        {
            client.BaseAddress = new Uri("https://qiuzhenbd.com");
            client.DefaultRequestHeaders.Add("User-Agent", "Examina-Desktop-Client/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(10); // 文件下载需要更长的超时时间
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = true, // 文件下载允许重定向
                UseProxy = true,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        });

        // 注册文件下载服务为单例
        _ = services.AddSingleton<IFileDownloadService>(provider =>
        {
            HttpClient httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("FileDownloadService");
            ILogger<FileDownloadService> logger = provider.GetRequiredService<ILogger<FileDownloadService>>();
            return new FileDownloadService(httpClient, logger);
        });

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

        // 为学生端专项训练服务配置HttpClient
        _ = services.AddHttpClient<IStudentSpecializedTrainingService, StudentSpecializedTrainingService>(client =>
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
            LeaderboardViewModel leaderboardFactory()
            {
                return provider.GetRequiredService<LeaderboardViewModel>();
            }

            Func<string, LeaderboardViewModel> leaderboardWithTypeFactory = provider.GetRequiredService<Func<string, LeaderboardViewModel>>();

            return new MainViewModel(authService, windowManager, leaderboardFactory, leaderboardWithTypeFactory);
        });
        _ = services.AddTransient<UserInfoCompletionViewModel>();
        _ = services.AddTransient<LoadingViewModel>();
        _ = services.AddTransient<ProfileViewModel>();
        _ = services.AddTransient<ChangePasswordViewModel>();
        _ = services.AddTransient<SchoolBindingViewModel>();
        _ = services.AddTransient<ExamListViewModel>(provider =>
        {
            IStudentExamService examService = provider.GetRequiredService<IStudentExamService>();
            IStudentFormalExamService formalExamService = provider.GetRequiredService<IStudentFormalExamService>();
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            EnhancedExamToolbarService? enhancedService = provider.GetService<EnhancedExamToolbarService>();
            return new ExamListViewModel(examService, formalExamService, authService, enhancedService);
        });
        // UnifiedExamViewModel不在DI容器中注册，由MainViewModel直接创建以避免循环依赖
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
            IStudentComprehensiveTrainingService comprehensiveTrainingService = provider.GetRequiredService<IStudentComprehensiveTrainingService>();
            IStudentExamService studentExamService = provider.GetRequiredService<IStudentExamService>();
            IStudentMockExamService studentMockExamService = provider.GetRequiredService<IStudentMockExamService>();

            // 直接创建带所有依赖的实例，避免无参构造函数的双重初始化问题
            return new LeaderboardViewModel(rankingService, logger, comprehensiveTrainingService, studentExamService, studentMockExamService);
        });

        // 添加带排行榜类型的工厂方法
        _ = services.AddTransient<Func<string, LeaderboardViewModel>>(provider => rankingTypeId =>
        {
            RankingService rankingService = provider.GetRequiredService<RankingService>();
            ILogger<LeaderboardViewModel> logger = provider.GetRequiredService<ILogger<LeaderboardViewModel>>();
            IStudentComprehensiveTrainingService comprehensiveTrainingService = provider.GetRequiredService<IStudentComprehensiveTrainingService>();
            IStudentExamService studentExamService = provider.GetRequiredService<IStudentExamService>();
            IStudentMockExamService studentMockExamService = provider.GetRequiredService<IStudentMockExamService>();

            // 直接创建带特定排行榜类型的实例，确保一次性正确初始化
            return new LeaderboardViewModel(rankingService, logger, comprehensiveTrainingService, studentExamService, rankingTypeId, studentMockExamService);
        });
        _ = services.AddTransient<SpecializedTrainingListViewModel>(provider =>
        {
            IStudentSpecializedTrainingService trainingService = provider.GetRequiredService<IStudentSpecializedTrainingService>();
            IAuthenticationService authService = provider.GetRequiredService<IAuthenticationService>();
            return new SpecializedTrainingListViewModel(trainingService, authService);
        });

        // 注册文件下载相关的ViewModels
        _ = services.AddTransient<FileDownloadPreparationViewModel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 初始化AppServiceManager
        if (_serviceProvider != null)
        {
            AppServiceManager.Initialize(_serviceProvider);

            // 测试BenchSuiteDirectoryService是否正确注册
            #if DEBUG
            Tests.BenchSuiteDirectoryServiceTest.TestServiceAvailability();
            Tests.BenchSuiteDirectoryServiceTest.TestExamToolbarViewModelDependency();

            // 测试LeaderboardViewModel依赖注入
            Tests.LeaderboardViewModelDependencyTest.TestLeaderboardViewModelDependencies();

            // 测试排行榜类型初始化一致性
            Tests.LeaderboardViewModelDependencyTest.TestLeaderboardTypeInitializationConsistency();

            // 测试API URL构建
            Tests.ApiUrlVerificationTest.RunAllTests();

            // 测试ExamAttemptService功能
            _ = Task.Run(async () =>
            {
                await Tests.ExamAttemptServiceTest.RunAllTests();

                // 延迟一段时间后运行数据一致性验证（等待用户登录）
                await Task.Delay(10000); // 等待10秒
                await Tests.DataConsistencyVerification.RunAllVerifications();

                // 测试开始考试按钮功能
                await Task.Delay(2000); // 等待2秒
                await Tests.ExamButtonFunctionalityTest.RunAllTests();
            });
            #endif
        }

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
