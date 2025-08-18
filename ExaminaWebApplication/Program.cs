using System.Text;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Filters;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using ExaminaWebApplication.Services.ImportedExam;
using ExaminaWebApplication.Services.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 配置服务器端口 - 解决发布后无法访问的问题
builder.WebHost.ConfigureKestrel(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // 开发环境使用launchSettings.json中的配置
        options.ListenLocalhost(5117); // HTTP
        // 开发环境的HTTPS配置由launchSettings.json处理
    }
    else
    {
        // 生产环境只配置HTTP端口，避免HTTPS证书问题
        options.ListenAnyIP(5000); // HTTP - 监听所有IP地址
        options.ListenLocalhost(8080); // HTTP localhost备用端口

        // 如果需要HTTPS，请先配置证书：
        // dotnet dev-certs https --trust
        // 然后取消注释下面的代码：
        // options.ListenAnyIP(5001, listenOptions =>
        // {
        //     listenOptions.UseHttps(); // HTTPS
        // });
    }
});

// 配置日志记录
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new RequireLoginAttribute());
});

// 添加API控制器并配置JSON序列化选项
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new RequireLoginAttribute());
})
    .AddJsonOptions(options =>
    {
        // 配置JSON序列化以支持CamelCase命名策略
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 允许在反序列化时忽略大小写
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // 允许读取注释
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
        // 允许尾随逗号
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        // 处理循环引用 - 忽略循环引用
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // 设置最大深度以防止无限递归
        options.JsonSerializerOptions.MaxDepth = 32;

        // 添加自定义转换器
        options.JsonSerializerOptions.Converters.Add(new ExaminaWebApplication.Converters.UserRoleJsonConverter());
        //options.JsonSerializerOptions.Converters.Add(new BenchSuite.Converters.ModuleTypeJsonConverter());
    });

// 配置MySQL数据库
string? connectionString = builder.Configuration.GetConnectionString("MySqlConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.Parse("8.0.0-mysql"),
        mysqlOptions =>
        {
            _ = mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }
    ));

// 注册服务
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IWeChatService, WeChatService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// 注册考试导入相关服务
builder.Services.AddScoped<ExamImportService>();

// 注册综合训练导入相关服务
builder.Services.AddScoped<ExaminaWebApplication.Services.ImportedComprehensiveTraining.ComprehensiveTrainingImportService>();

// 注册组织相关服务
builder.Services.AddScoped<IInvitationCodeService, InvitationCodeService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ITeacherOrganizationService, TeacherOrganizationService>();
builder.Services.AddScoped<INonOrganizationStudentService, NonOrganizationStudentService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// 注册学生端服务
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentExamService, ExaminaWebApplication.Services.Student.StudentExamService>();
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentComprehensiveTrainingService, ExaminaWebApplication.Services.Student.StudentComprehensiveTrainingService>();



// 添加后台服务
builder.Services.AddHostedService<SessionCleanupService>();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 添加会话支持
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ExaminaSession";
});

// 添加HttpClient
builder.Services.AddHttpClient();

// 配置混合认证策略
IConfigurationSection jwtSettings = builder.Configuration.GetSection("Jwt");
string secretKey = jwtSettings["SecretKey"] ?? "ExaminaSecretKey2024!@#$%^&*()_+1234567890";
string issuer = jwtSettings["Issuer"] ?? "ExaminaApp";
string audience = jwtSettings["Audience"] ?? "ExaminaUsers";

builder.Services.AddAuthentication(options =>
{
    // 默认使用JWT认证
    options.DefaultAuthenticateScheme = "MultiScheme";
    options.DefaultChallengeScheme = "MultiScheme";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddCookie("Cookie", options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.Name = "ExaminaAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddPolicyScheme("MultiScheme", "Multi Scheme", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // 根据请求路径决定使用哪种认证方案
        string path = context.Request.Path.Value?.ToLower() ?? "";

        // 非 API 页面统一用 Cookie（便于未登录重定向到登录页）
        if (!path.StartsWith("/api"))
        {
            return "Cookie";
        }

        // 学生API使用JWT认证
        if (path.StartsWith("/api/student/"))
        {
            return "JwtBearer";
        }

        // 管理员API使用Cookie认证
        if (path.StartsWith("/api/admin/"))
        {
            return "Cookie";
        }

        // 其他API路径根据Authorization头或路径决定
        if (path.StartsWith("/api/"))
        {
            string authHeader = context.Request.Headers.Authorization.ToString();
            return authHeader.StartsWith("Bearer ") ? "JwtBearer" : "Cookie";
        }

        // 默认回退 Cookie
        return "Cookie";
    };
});

// 配置授权策略（默认由自定义过滤器 RequireLoginAttribute 保护页面；策略用于角色控制与API）
builder.Services.AddAuthorization(options =>
{
    // 学生策略（JWT）
    options.AddPolicy("StudentPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("JwtBearer");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireRole("Student");
    });

    // 教师策略（Cookie）
    options.AddPolicy("TeacherPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("Cookie");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireRole("Teacher");
    });

    // 管理员策略（Cookie）
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("Cookie");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireRole("Administrator");
    });

    // 教师或管理员策略（Cookie）
    options.AddPolicy("TeacherOrAdminPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("Cookie");
        _ = policy.RequireAuthenticatedUser();
        _ = policy.RequireRole("Teacher", "Administrator");
    });

    // 通用 API 访问策略（JWT）
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("JwtBearer");
        _ = policy.RequireAuthenticatedUser();
    });
});

// 配置HTTPS重定向
builder.Services.AddHttpsRedirection(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // 开发环境配置 - 使用launchSettings.json中的HTTPS端口
        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
        options.HttpsPort = 7125; // 开发环境HTTPS端口
    }
    else
    {
        // 生产环境配置 - 使用307临时重定向而不是308永久重定向
        // 这样可以避免客户端缓存重定向导致的问题
        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
        options.HttpsPort = 443; // 生产环境标准HTTPS端口
    }
});

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        _ = policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("StudentFrontend", policy =>
    {
        _ = policy.WithOrigins("http://localhost:3000", "https://qiuzhenbd.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

WebApplication app = builder.Build();

// 配置静态Web资产处理
try
{
    // 尝试使用静态Web资产，如果失败则跳过
    if (app.Environment.IsDevelopment())
    {
        // 在开发环境中，静态Web资产可能不存在，这是正常的
        app.Logger.LogInformation("开发环境：跳过静态Web资产配置");
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "静态Web资产配置失败，继续运行");
}

// 获取日志记录器
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

// 记录应用程序启动信息
logger.LogInformation("=== Examina Web Application 正在启动 ===");
logger.LogInformation("环境: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("应用程序名称: {ApplicationName}", app.Environment.ApplicationName);
logger.LogInformation("内容根路径: {ContentRoot}", app.Environment.ContentRootPath);
logger.LogInformation("启动时间: {StartTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

// 启动前自动迁移数据库 - 增强版实现
await EnsureDatabaseAsync(app.Services, logger);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}
else
{
    // 开发环境配置
    _ = app.UseDeveloperExceptionPage();
}

// 添加API请求调试中间件
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        app.Logger.LogInformation("API请求详情: {Method} {Scheme}://{Host}{Path} IsHttps:{IsHttps}",
            context.Request.Method,
            context.Request.Scheme,
            context.Request.Host,
            context.Request.Path,
            context.Request.IsHttps);

        // 记录请求体内容（仅用于调试）
        if (context.Request.Method == "POST" && context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
            using StreamReader reader = new(context.Request.Body, leaveOpen: true);
            string requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            app.Logger.LogInformation("API请求体: {RequestBody}", requestBody);
        }
    }

    await next();
});

// 完全禁用HTTPS重定向以避免重定向循环问题
// 如果需要HTTPS，请在生产环境中通过反向代理（如Nginx）处理
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

// 启用CORS
app.UseCors("AllowAll");

// 启用会话
app.UseSession();

// 启用认证和授权
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// 映射API控制器
app.MapControllers();

// 应用启动时进行简单的种子数据检查，确保至少存在一个管理员用户
using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // 仅在数据库可用时执行
        if (db.Database.CanConnect())
        {
            bool hasAnyUser = db.Users.Any();
            if (!hasAnyUser)
            {
                User admin = new()
                {
                    Username = "admin",
                    Email = "admin@hbexam.com",
                    // 管理员初始强密码（至少12位，包含大小写字母、数字、特殊字符）：Adm!n#2025$ExaMina
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Adm!n#2025$ExaMina"),
                    Role = UserRole.Administrator,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    AllowMultipleDevices = true,
                    MaxDeviceCount = 10
                };
                _ = db.Users.Add(admin);
                _ = db.SaveChanges();
            }
        }
    }
    catch (Exception seedingEx)
    {
        // 仅记录，不阻止应用启动
        logger.LogWarning(seedingEx, "用户种子初始化失败，可能导致外键导入失败。请确保至少存在一个有效用户。");
    }
}

// 组织管理相关路由
app.MapControllerRoute(
    name: "schoolManagement",
    pattern: "SchoolManagement/{action=Index}/{id?}",
    defaults: new { controller = "SchoolManagement" });

app.MapControllerRoute(
    name: "classManagement",
    pattern: "ClassManagement/{action=Index}/{id?}",
    defaults: new { controller = "ClassManagement" });

app.MapControllerRoute(
    name: "nonOrganizationStudent",
    pattern: "NonOrganizationStudent/{action=Index}/{id?}",
    defaults: new { controller = "NonOrganizationStudent" });

app.MapControllerRoute(
    name: "userManagement",
    pattern: "UserManagement/{action=Index}/{id?}",
    defaults: new { controller = "UserManagement" });

// 添加根路径重定向到登录页面
app.MapGet("/", context =>
{
    // 如果用户已登录，重定向到首页；否则重定向到登录页面
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Home");
        return Task.CompletedTask;
    }
    else
    {
        context.Response.Redirect("/Login");
        return Task.CompletedTask;
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// 记录应用程序即将启动的信息
logger.LogInformation("=== 应用程序配置完成，即将启动监听 ===");
logger.LogInformation("监听地址将在启动后显示");

try
{
    logger.LogInformation("=== Examina Web Application 启动成功 ===");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "=== 应用程序启动失败 ===");
    throw;
}
finally
{
    logger.LogInformation("=== Examina Web Application 已停止 ===");
}

/// <summary>
/// 确保数据库存在并自动迁移 - 增强版实现
/// </summary>
/// <param name="services">服务提供器</param>
/// <param name="logger">日志记录器</param>
static async Task EnsureDatabaseAsync(IServiceProvider services, ILogger logger)
{
    const int maxRetryAttempts = 5;
    const int baseDelayMs = 1000; // 基础延迟1秒

    for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
    {
        try
        {
            using IServiceScope scope = services.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            logger.LogInformation("=== 开始数据库自动迁移（第 {Attempt}/{MaxAttempts} 次尝试）===", attempt, maxRetryAttempts);

            // 1. 检查数据库连接
            logger.LogInformation("正在测试数据库连接...");
            bool canConnect = await TestDatabaseConnectionAsync(context, logger);

            if (!canConnect)
            {
                throw new InvalidOperationException("数据库连接测试失败");
            }

            // 2. 检查待迁移的项目
            logger.LogInformation("正在检查待迁移的项目...");
            IEnumerable<string> pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            IEnumerable<string> appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            logger.LogInformation("已应用的迁移数量: {AppliedCount}", appliedMigrations.Count());
            logger.LogInformation("待应用的迁移数量: {PendingCount}", pendingMigrations.Count());

            if (pendingMigrations.Any())
            {
                logger.LogInformation("待应用的迁移列表:");
                foreach (string migration in pendingMigrations)
                {
                    logger.LogInformation("  - {Migration}", migration);
                }
            }

            // 3. 应用数据库迁移
            if (pendingMigrations.Any())
            {
                logger.LogInformation("正在应用数据库迁移...");
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ 数据库迁移成功完成");
            }
            else
            {
                logger.LogInformation("✅ 数据库已是最新版本，无需迁移");
            }

            // 4. 验证数据库状态
            logger.LogInformation("正在验证数据库状态...");
            await ValidateDatabaseStateAsync(context, logger);

            // 5. 初始化测试数据
            logger.LogInformation("正在初始化测试数据...");
            await InitializeTestDataAsync(context, logger);

            logger.LogInformation("=== 数据库自动迁移完成 ===");
            return; // 成功完成，退出重试循环

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库自动迁移失败（第 {Attempt}/{MaxAttempts} 次尝试）: {ErrorMessage}",
                attempt, maxRetryAttempts, ex.Message);

            if (attempt == maxRetryAttempts)
            {
                logger.LogCritical("=== 数据库自动迁移最终失败，应用程序无法启动 ===");
                throw new InvalidOperationException($"数据库迁移失败，已尝试 {maxRetryAttempts} 次", ex);
            }

            // 指数退避延迟
            int delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
            logger.LogWarning("将在 {DelayMs}ms 后进行第 {NextAttempt} 次重试...", delayMs, attempt + 1);
            await Task.Delay(delayMs);
        }
    }
}

/// <summary>
/// 测试数据库连接
/// </summary>
/// <param name="context">数据库上下文</param>
/// <param name="logger">日志记录器</param>
/// <returns>连接是否成功</returns>
static async Task<bool> TestDatabaseConnectionAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        // 使用异步方法测试连接
        _ = await context.Database.CanConnectAsync();

        // 获取数据库信息
        string? connectionString = context.Database.GetConnectionString();
        string databaseName = context.Database.GetDbConnection().Database;

        logger.LogInformation("✅ 数据库连接成功");
        logger.LogInformation("数据库名称: {DatabaseName}", databaseName);
        logger.LogDebug("连接字符串: {ConnectionString}",
            connectionString?.Replace(context.Database.GetDbConnection().ConnectionString.Split(';')
                .FirstOrDefault(s => s.Contains("password", StringComparison.OrdinalIgnoreCase))?.Split('=')[1] ?? "", "***"));

        return true;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ 数据库连接失败: {ErrorMessage}", ex.Message);
        return false;
    }
}

/// <summary>
/// 验证数据库状态
/// </summary>
/// <param name="context">数据库上下文</param>
/// <param name="logger">日志记录器</param>
static async Task ValidateDatabaseStateAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        // 检查关键表是否存在并可访问
        int userCount = await context.Users.CountAsync();

        logger.LogInformation("数据库状态验证成功:");
        logger.LogInformation("  - 用户数量: {UserCount}", userCount);

        // 检查数据库版本信息
        string? lastMigration = (await context.Database.GetAppliedMigrationsAsync()).LastOrDefault();
        logger.LogInformation("  - 最新迁移: {LastMigration}", lastMigration ?? "无");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "数据库状态验证出现警告: {ErrorMessage}", ex.Message);
    }
}

/// <summary>
/// 初始化测试数据
/// </summary>
/// <param name="context">数据库上下文</param>
/// <param name="logger">日志记录器</param>
static async Task InitializeTestDataAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        // 按要求：不再创建任何测试学生用户
        logger.LogInformation("跳过测试数据初始化：不创建测试学生用户");
        await Task.CompletedTask;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ 初始化测试数据失败: {ErrorMessage}", ex.Message);
    }
}
