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

// 配置Kestrel服务器以支持大文件上传
builder.WebHost.ConfigureKestrel(options =>
{
    // 从配置中读取最大请求体大小，默认为500MB
    long maxRequestBodySize = builder.Configuration.GetValue<long>("Performance:MaxRequestBodySize", 524288000);
    options.Limits.MaxRequestBodySize = maxRequestBodySize;

    // 配置最大请求头大小
    options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB

    // 配置最大请求头数量
    options.Limits.MaxRequestHeaderCount = 100;

    // 从配置中读取请求超时，默认为10分钟
    int requestTimeoutSeconds = builder.Configuration.GetValue<int>("Performance:RequestTimeoutSeconds", 600);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(requestTimeoutSeconds);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
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

// 配置表单选项以支持大文件上传
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    // 从配置中读取最大文件大小，优先使用FileUpload配置，其次使用Performance配置
    long maxFileSize = builder.Configuration.GetValue<long>("FileUpload:MaxFileSize",
        builder.Configuration.GetValue<long>("Performance:MaxRequestBodySize", 524288000));

    // 设置最大请求体大小
    options.MultipartBodyLengthLimit = maxFileSize;

    // 设置单个文件大小限制
    options.ValueLengthLimit = (int)maxFileSize;

    // 设置表单键数量限制
    options.KeyLengthLimit = 2048;

    // 设置表单值数量限制
    options.ValueCountLimit = 1024;

    // 设置内存缓冲区阈值（超过此大小将写入磁盘）
    options.MemoryBufferThreshold = 1048576; // 1MB

    // 设置缓冲区大小
    options.BufferBodyLengthLimit = 134217728; // 128MB
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
{
    _ = options.UseMySql(
        connectionString,
        ServerVersion.Parse("8.0.0-mysql"),
        mysqlOptions =>
        {
            _ = mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }
    );

    // 配置查询分割行为以提升多集合导航查询性能
    // 注意：全局配置可能影响所有查询，建议在具体查询中使用 AsSplitQuery()
});

// 注册服务
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IWeChatService, WeChatService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<UserDataFixService>();

// 注册考试导入相关服务
builder.Services.AddScoped<ExamImportService>();

// 注册综合训练导入相关服务
builder.Services.AddScoped<ExaminaWebApplication.Services.ImportedComprehensiveTraining.ComprehensiveTrainingImportService>();
builder.Services.AddScoped<ExaminaWebApplication.Services.ImportedComprehensiveTraining.EnhancedComprehensiveTrainingService>();

// 注册专项训练导入相关服务
builder.Services.AddScoped<ExaminaWebApplication.Services.ImportedSpecializedTraining.SpecializedTrainingImportService>();

// 注册组织相关服务
builder.Services.AddScoped<IInvitationCodeService, InvitationCodeService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ITeacherOrganizationService, TeacherOrganizationService>();
builder.Services.AddScoped<INonOrganizationStudentService, NonOrganizationStudentService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// 注册学校权限验证服务
builder.Services.AddScoped<ExaminaWebApplication.Services.School.ISchoolPermissionService, ExaminaWebApplication.Services.School.SchoolPermissionService>();

// 注册学生端服务
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentExamService, ExaminaWebApplication.Services.Student.StudentExamService>();
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentComprehensiveTrainingService, ExaminaWebApplication.Services.Student.StudentComprehensiveTrainingService>();
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentSpecializedTrainingService, ExaminaWebApplication.Services.Student.StudentSpecializedTrainingService>();
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentSpecialPracticeService, ExaminaWebApplication.Services.Student.StudentSpecialPracticeService>();
builder.Services.AddScoped<ExaminaWebApplication.Services.Student.IStudentMockExamService, ExaminaWebApplication.Services.Student.StudentMockExamService>();

// 注册管理员端服务
builder.Services.AddScoped<ExaminaWebApplication.Services.Admin.IAdminExamManagementService, ExaminaWebApplication.Services.Admin.AdminExamManagementService>();

// 注册排行榜服务
builder.Services.AddScoped<RankingService>();

// 注册用户导入相关服务
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<UserImportService>();

// 注册文件上传相关服务
builder.Services.AddScoped<ExaminaWebApplication.Services.FileUpload.IFileUploadService, ExaminaWebApplication.Services.FileUpload.FileUploadService>();
builder.Services.Configure<ExaminaWebApplication.Models.FileUpload.FileUploadConfiguration>(
    builder.Configuration.GetSection("FileUpload"));

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
        _ = policy.WithOrigins("http://localhost:5000", "https://qiuzhenbd.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

WebApplication app = builder.Build();

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

            // 创建测试考试数据
            await SeedTestExamData.SeedAsync(db);
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

// 排行榜相关路由
app.MapControllerRoute(
    name: "ranking",
    pattern: "Ranking/{action=Index}/{id?}",
    defaults: new { controller = "Ranking" });

app.MapControllerRoute(
    name: "examRanking",
    pattern: "exam-ranking",
    defaults: new { controller = "Ranking", action = "ExamRanking" });

app.MapControllerRoute(
    name: "mockExamRanking",
    pattern: "mock-exam-ranking",
    defaults: new { controller = "Ranking", action = "MockExamRanking" });

app.MapControllerRoute(
    name: "trainingRanking",
    pattern: "training-ranking",
    defaults: new { controller = "Ranking", action = "TrainingRanking" });

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
