using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Excel;

using ExaminaWebApplication.Models.Windows;
using ExaminaWebApplication.Models.Word;
using ExaminaWebApplication.Models.ImportedExam;
using ExaminaWebApplication.Models.Organization;

using ExaminaWebApplication.Data.Excel;
using ExaminaWebApplication.Data.Windows;

namespace ExaminaWebApplication.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    // Excel操作点相关实体
    public DbSet<ExcelOperationPoint> ExcelOperationPoints { get; set; }
    public DbSet<ExcelOperationParameter> ExcelOperationParameters { get; set; }
    public DbSet<ExcelOperationTemplate> ExcelOperationTemplates { get; set; }
    public DbSet<ExcelOperationParameterTemplate> ExcelOperationParameterTemplates { get; set; }
    public DbSet<ExcelEnumType> ExcelEnumTypes { get; set; }
    public DbSet<ExcelEnumValue> ExcelEnumValues { get; set; }
    public DbSet<ExcelQuestionTemplate> ExcelQuestionTemplates { get; set; }
    public DbSet<ExcelQuestionInstance> ExcelQuestionInstances { get; set; }

    // Windows操作点相关实体
    public DbSet<WindowsOperationPoint> WindowsOperationPoints { get; set; }
    public DbSet<WindowsOperationParameter> WindowsOperationParameters { get; set; }
    public DbSet<WindowsEnumType> WindowsEnumTypes { get; set; }
    public DbSet<WindowsEnumValue> WindowsEnumValues { get; set; }
    public DbSet<WindowsQuestionTemplate> WindowsQuestionTemplates { get; set; }
    public DbSet<WindowsQuestionInstance> WindowsQuestionInstances { get; set; }



    // Word操作点相关实体
    public DbSet<WordOperationPoint> WordOperationPoints { get; set; }
    public DbSet<WordOperationParameter> WordOperationParameters { get; set; }
    public DbSet<WordEnumType> WordEnumTypes { get; set; }
    public DbSet<WordEnumValue> WordEnumValues { get; set; }
    public DbSet<WordQuestionTemplate> WordQuestionTemplates { get; set; }
    public DbSet<WordQuestionInstance> WordQuestionInstances { get; set; }

    // 导入考试相关实体
    public DbSet<ImportedExam> ImportedExams { get; set; }
    public DbSet<ImportedSubject> ImportedSubjects { get; set; }
    public DbSet<ImportedModule> ImportedModules { get; set; }
    public DbSet<ImportedQuestion> ImportedQuestions { get; set; }
    public DbSet<ImportedOperationPoint> ImportedOperationPoints { get; set; }
    public DbSet<ImportedParameter> ImportedParameters { get; set; }

    // 组织相关实体
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<InvitationCode> InvitationCodes { get; set; }
    public DbSet<StudentOrganization> StudentOrganizations { get; set; }







    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置User实体
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.WeChatOpenId).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.WeChatOpenId).HasMaxLength(100);
            entity.Property(e => e.RealName).HasMaxLength(50);
            entity.Property(e => e.StudentId).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsFirstLogin).HasDefaultValue(true);
            entity.Property(e => e.AllowMultipleDevices).HasDefaultValue(false);
            entity.Property(e => e.MaxDeviceCount).HasDefaultValue(1);

            // 配置枚举 - 设置哨兵值以避免EF Core警告
            entity.Property(e => e.Role)
                  .HasConversion<int>()
                  .HasDefaultValue(UserRole.Student)
                  .HasSentinel(0); // 设置哨兵值为0，当值为0时使用数据库默认值
        });

        // 配置UserDevice实体
        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.DeviceFingerprint).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.DeviceFingerprint }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.LastUsedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsTrusted);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DeviceFingerprint).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DeviceName).HasMaxLength(100).HasDefaultValue("");
            entity.Property(e => e.DeviceType).HasMaxLength(50).HasDefaultValue("");
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.BrowserInfo).HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsTrusted).HasDefaultValue(false);

            // 配置外键关系
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Devices)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置UserSession实体
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.RefreshToken).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.LastActivityAt);
            entity.HasIndex(e => e.SessionType);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastActivityAt).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // 配置枚举 - 设置哨兵值以避免EF Core警告
            entity.Property(e => e.SessionType)
                  .HasConversion<int>()
                  .HasDefaultValue(SessionType.JwtToken)
                  .HasSentinel(0); // 设置哨兵值为0，当值为0时使用数据库默认值

            // 配置外键关系
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Sessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置Excel实体
        ConfigureExcelEntities(modelBuilder);

        // 配置Windows实体
        ConfigureWindowsEntities(modelBuilder);

        // 配置Word实体
        ConfigureWordEntities(modelBuilder);

        // 配置导入考试相关实体
        ConfigureImportedExamEntities(modelBuilder);



        // 种子数据
        SeedData(modelBuilder);
    }

    private static void ConfigureExcelEntities(ModelBuilder modelBuilder)
    {
        // 配置ExcelOperationPoint实体
        modelBuilder.Entity<ExcelOperationPoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationNumber).IsUnique();
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.TargetType);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationNumber).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(1);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置枚举
            entity.Property(e => e.Category)
                  .HasConversion<int>()
                  .HasDefaultValue(ExcelOperationCategory.BasicOperation)
                  .HasSentinel(0);

            entity.Property(e => e.TargetType)
                  .HasConversion<int>()
                  .HasDefaultValue(ExcelTargetType.Worksheet)
                  .HasSentinel(0);
        });

        // 配置ExcelOperationParameter实体
        modelBuilder.Entity<ExcelOperationParameter>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => new { e.OperationPointId, e.ParameterOrder }).IsUnique();
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.DataType);
            entity.HasIndex(e => e.EnumTypeId);
            entity.HasIndex(e => e.IsEnabled);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationPointId).IsRequired();
            entity.Property(e => e.ParameterOrder).IsRequired();
            entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParameterDescription).HasMaxLength(500);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.AllowMultipleValues).HasDefaultValue(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.ExampleValue).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置枚举
            entity.Property(e => e.DataType)
                  .HasConversion<int>()
                  .HasDefaultValue(ExcelParameterDataType.String)
                  .HasSentinel(0);

            // 配置外键关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.Parameters)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EnumType)
                  .WithMany(et => et.Parameters)
                  .HasForeignKey(e => e.EnumTypeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置ExcelEnumType实体
        modelBuilder.Entity<ExcelEnumType>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.TypeName).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsEnabled);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // 配置ExcelEnumValue实体
        modelBuilder.Entity<ExcelEnumValue>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => new { e.EnumTypeId, e.EnumKey }).IsUnique();
            entity.HasIndex(e => e.EnumTypeId);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => e.IsEnabled);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.EnumTypeId).IsRequired();
            entity.Property(e => e.EnumKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.EnumType)
                  .WithMany(et => et.EnumValues)
                  .HasForeignKey(e => e.EnumTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ExcelQuestionTemplate实体
        modelBuilder.Entity<ExcelQuestionTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.DifficultyLevel);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationPointId).IsRequired();
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuestionTemplate).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ParameterConfiguration).IsRequired();
            entity.Property(e => e.DifficultyLevel).HasDefaultValue(1);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.QuestionTemplates)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置ExcelQuestionInstance实体
        modelBuilder.Entity<ExcelQuestionInstance>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TemplateId).IsRequired();
            entity.Property(e => e.QuestionTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.QuestionDescription).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ActualParameters).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置枚举
            entity.Property(e => e.Status)
                  .HasConversion<int>()
                  .HasDefaultValue(ExcelQuestionStatus.Draft)
                  .HasSentinel(0);

            // 配置外键关系
            entity.HasOne(e => e.Template)
                  .WithMany()
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }



    private static void ConfigureWindowsEntities(ModelBuilder modelBuilder)
    {
        // 配置WindowsOperationPoint实体
        modelBuilder.Entity<WindowsOperationPoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationNumber).IsUnique();
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.OperationMode);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationNumber).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置枚举
            entity.Property(e => e.OperationType)
                  .HasConversion<int>()
                  .HasDefaultValue(WindowsOperationType.Create)
                  .HasSentinel(0);

            entity.Property(e => e.OperationMode)
                  .HasConversion<int>()
                  .HasDefaultValue(WindowsOperationMode.Universal)
                  .HasSentinel(0);
        });

        // 配置WindowsOperationParameter实体
        modelBuilder.Entity<WindowsOperationParameter>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.ParameterOrder);
            entity.HasIndex(e => e.DataType);
            entity.HasIndex(e => e.IsRequired);
            entity.HasIndex(e => e.IsEnabled);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationPointId).IsRequired();
            entity.Property(e => e.ParameterOrder).IsRequired();
            entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParameterDescription).HasMaxLength(500);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.AllowMultipleValues).HasDefaultValue(false);
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.ExampleValue).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置枚举
            entity.Property(e => e.DataType)
                  .HasConversion<int>()
                  .HasDefaultValue(WindowsParameterDataType.String)
                  .HasSentinel(0);

            // 配置外键关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.Parameters)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EnumType)
                  .WithMany(et => et.Parameters)
                  .HasForeignKey(e => e.EnumTypeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置WindowsEnumType实体
        modelBuilder.Entity<WindowsEnumType>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.TypeName).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsEnabled);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // 配置WindowsEnumValue实体
        modelBuilder.Entity<WindowsEnumValue>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.EnumTypeId);
            entity.HasIndex(e => e.EnumKey);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => e.IsEnabled);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.EnumTypeId).IsRequired();
            entity.Property(e => e.EnumKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasDefaultValue(1);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.EnumType)
                  .WithMany(et => et.EnumValues)
                  .HasForeignKey(e => e.EnumTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置WindowsQuestionTemplate实体
        modelBuilder.Entity<WindowsQuestionTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.DifficultyLevel);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.UsageCount);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationPointId).IsRequired();
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuestionTemplate).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ParameterConfiguration).IsRequired();
            entity.Property(e => e.DifficultyLevel).HasDefaultValue(1);
            entity.Property(e => e.InputExample).HasMaxLength(500);
            entity.Property(e => e.InputDescription).HasMaxLength(1000);
            entity.Property(e => e.OutputExample).HasMaxLength(500);
            entity.Property(e => e.OutputDescription).HasMaxLength(1000);
            entity.Property(e => e.Requirements).HasMaxLength(2000);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.QuestionTemplates)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置WindowsQuestionInstance实体
        modelBuilder.Entity<WindowsQuestionInstance>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.QuestionTemplateId);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.UsageCount);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.QuestionTemplateId).IsRequired();
            entity.Property(e => e.InstanceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuestionContent).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ParameterValues).IsRequired();
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.QuestionTemplate)
                  .WithMany(qt => qt.QuestionInstances)
                  .HasForeignKey(e => e.QuestionTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置组织相关实体
        ConfigureOrganizationEntities(modelBuilder);
    }

    /// <summary>
    /// 配置组织相关实体
    /// </summary>
    private static void ConfigureOrganizationEntities(ModelBuilder modelBuilder)
    {
        // 配置Organization实体
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置属性
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasConversion<int>();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // 配置索引
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            // 配置外键关系
            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置InvitationCode实体
        modelBuilder.Entity<InvitationCode>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置属性
            entity.Property(e => e.Code).IsRequired().HasMaxLength(7);
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.UsageCount).IsRequired().HasDefaultValue(0);

            // 配置索引
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.CreatedAt);

            // 配置外键关系
            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.InvitationCodes)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置StudentOrganization实体
        modelBuilder.Entity<StudentOrganization>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置属性
            entity.Property(e => e.StudentId).IsRequired();
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.JoinedAt).IsRequired();
            entity.Property(e => e.InvitationCodeId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // 配置索引
            entity.HasIndex(e => new { e.StudentId, e.OrganizationId }).IsUnique();
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.InvitationCodeId);
            entity.HasIndex(e => e.JoinedAt);
            entity.HasIndex(e => e.IsActive);

            // 配置外键关系
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.StudentOrganizations)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitationCode)
                  .WithMany(ic => ic.StudentOrganizations)
                  .HasForeignKey(e => e.InvitationCodeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // 使用固定的时间戳避免迁移时的动态值问题
        var seedDate = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        // 创建测试用户
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@examina.com",
                PhoneNumber = "13800138000",
                PasswordHash = "$2a$11$8K1p/a0dL2LkqvQOuiOX2uy7YIr1w1yuBVMxVVehboYg7iYfZb4W2", // admin123
                Role = UserRole.Administrator,
                RealName = "系统管理员",
                StudentId = "ADMIN001",
                IsFirstLogin = false,
                CreatedAt = seedDate,
                IsActive = true,
                AllowMultipleDevices = true,
                MaxDeviceCount = 5
            },
            new User
            {
                Id = 2,
                Username = "student001",
                Email = "student001@examina.com",
                PhoneNumber = "13800138001",
                PasswordHash = "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi", // 123456
                Role = UserRole.Student,
                RealName = "张三",
                StudentId = "2024001",
                IsFirstLogin = true,
                CreatedAt = seedDate,
                IsActive = true,
                AllowMultipleDevices = false,
                MaxDeviceCount = 1
            },
            new User
            {
                Id = 3,
                Username = "teacher001",
                Email = "teacher001@examina.com",
                PhoneNumber = "13800138002",
                PasswordHash = "$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi", // 123456
                Role = UserRole.Teacher,
                RealName = "李老师",
                StudentId = "TEACHER001",
                IsFirstLogin = false,
                CreatedAt = seedDate,
                IsActive = true,
                AllowMultipleDevices = true,
                MaxDeviceCount = 3
            }
        );

        // 添加Excel相关种子数据
        SeedExcelData(modelBuilder, seedDate);
    }

    private static void SeedExcelData(ModelBuilder modelBuilder, DateTime seedDate)
    {
        // 添加Excel枚举类型
        modelBuilder.Entity<ExcelEnumType>().HasData(ExcelEnumData.GetEnumTypes().Select(et =>
        {
            et.CreatedAt = seedDate;
            return et;
        }));

        // 添加Excel枚举值
        modelBuilder.Entity<ExcelEnumValue>().HasData(ExcelEnumData.GetEnumValues().Select(ev =>
        {
            ev.CreatedAt = seedDate;
            return ev;
        }));

        // 添加Excel基础操作点
        modelBuilder.Entity<ExcelOperationPoint>().HasData(ExcelBasicOperationData.GetBasicOperationPoints().Select(op =>
        {
            op.CreatedAt = seedDate;
            return op;
        }));

        // 添加Excel数据清单操作点
        modelBuilder.Entity<ExcelOperationPoint>().HasData(ExcelDataListOperationData.GetDataListOperationPoints().Select(op =>
        {
            op.CreatedAt = seedDate;
            return op;
        }));

        // 添加Excel图表操作点
        modelBuilder.Entity<ExcelOperationPoint>().HasData(ExcelChartOperationData.GetChartOperationPoints().Select(op =>
        {
            op.CreatedAt = seedDate;
            return op;
        }));

        // 添加Excel基础操作参数
        modelBuilder.Entity<ExcelOperationParameter>().HasData(ExcelBasicOperationData.GetBasicOperationParameters().Select(param =>
        {
            param.CreatedAt = seedDate;
            return param;
        }));

        // 添加Excel基础操作扩展参数
        modelBuilder.Entity<ExcelOperationParameter>().HasData(ExcelBasicOperationParametersExtended.GetExtendedBasicOperationParameters().Select(param =>
        {
            param.CreatedAt = seedDate;
            return param;
        }));

        // 添加Excel数据清单操作参数
        modelBuilder.Entity<ExcelOperationParameter>().HasData(ExcelDataListOperationData.GetDataListOperationParameters().Select(param =>
        {
            param.CreatedAt = seedDate;
            return param;
        }));

        // 添加Excel图表操作参数
        modelBuilder.Entity<ExcelOperationParameter>().HasData(ExcelChartOperationParameters.GetAllChartOperationParameters().Select(param =>
        {
            param.CreatedAt = seedDate;
            return param;
        }));

        // 添加组织相关种子数据
        SeedOrganizationData(modelBuilder, seedDate);
    }

    /// <summary>
    /// 添加组织相关种子数据
    /// </summary>
    private static void SeedOrganizationData(ModelBuilder modelBuilder, DateTime seedDate)
    {
        // 创建测试组织
        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = 1,
                Name = "河北省示范高中",
                Type = OrganizationType.School,
                Description = "河北省重点示范高中，专注于计算机教育",
                CreatedAt = seedDate,
                CreatedBy = 1, // admin用户
                IsActive = true
            },
            new Organization
            {
                Id = 2,
                Name = "计算机培训机构",
                Type = OrganizationType.Institution,
                Description = "专业的计算机技能培训机构",
                CreatedAt = seedDate,
                CreatedBy = 1, // admin用户
                IsActive = true
            }
        );

        // 创建测试邀请码
        modelBuilder.Entity<InvitationCode>().HasData(
            new InvitationCode
            {
                Id = 1,
                Code = "SCHOOL1",
                OrganizationId = 1,
                CreatedAt = seedDate,
                ExpiresAt = null, // 永不过期
                IsActive = true,
                UsageCount = 0,
                MaxUsage = null // 无限制
            },
            new InvitationCode
            {
                Id = 2,
                Code = "INST001",
                OrganizationId = 2,
                CreatedAt = seedDate,
                ExpiresAt = seedDate.AddDays(30), // 30天后过期
                IsActive = true,
                UsageCount = 0,
                MaxUsage = 100 // 最多100人使用
            }
        );
    }



    private static void ConfigureWordEntities(ModelBuilder modelBuilder)
    {
        // 配置WordOperationPoint实体
        modelBuilder.Entity<WordOperationPoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationNumber).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationNumber).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // 配置WordOperationParameter实体
        modelBuilder.Entity<WordOperationParameter>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.ParameterKey);
            entity.HasIndex(e => e.DataType);
            entity.HasIndex(e => e.IsRequired);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ParameterKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ParameterName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DefaultValue).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.Parameters)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.EnumType)
                  .WithMany(et => et.Parameters)
                  .HasForeignKey(e => e.EnumTypeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置WordEnumType实体
        modelBuilder.Entity<WordEnumType>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.TypeName).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // 配置WordEnumValue实体
        modelBuilder.Entity<WordEnumValue>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.EnumTypeId);
            entity.HasIndex(e => e.Value);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Value).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置关系
            entity.HasOne(e => e.EnumType)
                  .WithMany(et => et.EnumValues)
                  .HasForeignKey(e => e.EnumTypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置WordQuestionTemplate实体
        modelBuilder.Entity<WordQuestionTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.DifficultyLevel);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuestionTemplate).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ParameterConfiguration).IsRequired().HasColumnType("json");
            entity.Property(e => e.Tags).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.QuestionTemplates)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置WordQuestionInstance实体
        modelBuilder.Entity<WordQuestionInstance>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.QuestionTemplateId);
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.UsageCount);
            entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.InstanceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuestionContent).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ParameterValues).IsRequired().HasColumnType("json");
            entity.Property(e => e.ExpectedAnswer).HasColumnType("json");
            entity.Property(e => e.ScoringCriteria).HasColumnType("json");
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            // 配置关系
            entity.HasOne(e => e.QuestionTemplate)
                  .WithMany(qt => qt.QuestionInstances)
                  .HasForeignKey(e => e.QuestionTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// 配置导入考试相关实体
    /// </summary>
    private static void ConfigureImportedExamEntities(ModelBuilder modelBuilder)
    {
        // 配置ImportedExam实体
        modelBuilder.Entity<ImportedExam>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OriginalExamId).IsUnique();
            entity.HasIndex(e => e.ImportedBy);
            entity.HasIndex(e => e.ImportedAt);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ExamType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ImportStatus);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OriginalExamId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ExamType).IsRequired().HasMaxLength(50).HasDefaultValue("UnifiedExam");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Draft");
            entity.Property(e => e.TotalScore).IsRequired().HasColumnType("decimal(6,2)").HasDefaultValue(100.0m);
            entity.Property(e => e.DurationMinutes).IsRequired().HasDefaultValue(120);
            entity.Property(e => e.PassingScore).IsRequired().HasColumnType("decimal(6,2)").HasDefaultValue(60.0m);
            entity.Property(e => e.AllowRetake).HasDefaultValue(false);
            entity.Property(e => e.MaxRetakeCount).HasDefaultValue(0);
            entity.Property(e => e.RandomizeQuestions).HasDefaultValue(false);
            entity.Property(e => e.ShowScore).HasDefaultValue(true);
            entity.Property(e => e.ShowAnswers).HasDefaultValue(false);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.ExtendedConfig).HasColumnType("json");
            entity.Property(e => e.ImportedBy).IsRequired();
            entity.Property(e => e.ImportedAt).IsRequired();
            entity.Property(e => e.OriginalCreatedBy).IsRequired();
            entity.Property(e => e.OriginalCreatedAt).IsRequired();
            entity.Property(e => e.ImportFileName).HasMaxLength(255);
            entity.Property(e => e.ImportVersion).HasMaxLength(20).HasDefaultValue("1.0");
            entity.Property(e => e.ImportStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Success");
            entity.Property(e => e.ImportErrorMessage).HasMaxLength(2000);

            // 配置外键关系
            entity.HasOne(e => e.Importer)
                  .WithMany()
                  .HasForeignKey(e => e.ImportedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置ImportedSubject实体
        modelBuilder.Entity<ImportedSubject>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.ExamId);
            entity.HasIndex(e => e.OriginalSubjectId);
            entity.HasIndex(e => e.SubjectType);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OriginalSubjectId).IsRequired();
            entity.Property(e => e.ExamId).IsRequired();
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Score).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(20.0m);
            entity.Property(e => e.DurationMinutes).IsRequired().HasDefaultValue(30);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.MinScore).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Weight).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(1.0m);
            entity.Property(e => e.SubjectConfig).HasColumnType("json");
            entity.Property(e => e.QuestionCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.Exam)
                  .WithMany(ex => ex.Subjects)
                  .HasForeignKey(e => e.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedModule实体
        modelBuilder.Entity<ImportedModule>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.ExamId);
            entity.HasIndex(e => e.OriginalModuleId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Order);
            entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OriginalModuleId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExamId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Score).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.Order).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.Exam)
                  .WithMany(ex => ex.Modules)
                  .HasForeignKey(e => e.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedQuestion实体
        modelBuilder.Entity<ImportedQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.ExamId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.ModuleId);
            entity.HasIndex(e => e.OriginalQuestionId);
            entity.HasIndex(e => e.QuestionType);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OriginalQuestionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExamId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.QuestionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Score).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(10.0m);
            entity.Property(e => e.DifficultyLevel).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.EstimatedMinutes).IsRequired().HasDefaultValue(5);
            entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.IsRequired).HasDefaultValue(true);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.QuestionConfig).HasColumnType("json");
            entity.Property(e => e.AnswerValidationRules).HasColumnType("json");
            entity.Property(e => e.StandardAnswer).HasColumnType("json");
            entity.Property(e => e.ScoringRules).HasColumnType("json");
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.ProgramInput).HasMaxLength(1000);
            entity.Property(e => e.ExpectedOutput).HasMaxLength(2000);
            entity.Property(e => e.OriginalCreatedAt).IsRequired();
            entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.Exam)
                  .WithMany()
                  .HasForeignKey(e => e.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subject)
                  .WithMany(s => s.Questions)
                  .HasForeignKey(e => e.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Module)
                  .WithMany(m => m.Questions)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedOperationPoint实体
        modelBuilder.Entity<ImportedOperationPoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.QuestionId);
            entity.HasIndex(e => e.OriginalOperationPointId);
            entity.HasIndex(e => e.ModuleType);
            entity.HasIndex(e => e.Order);
            entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OriginalOperationPointId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.QuestionId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ModuleType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Score).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(0.0m);
            entity.Property(e => e.Order).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedTime).HasMaxLength(50);
            entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.Question)
                  .WithMany(q => q.OperationPoints)
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedParameter实体
        modelBuilder.Entity<ImportedParameter>(entity =>
        {
            entity.HasKey(e => e.Id);

            // 配置索引
            entity.HasIndex(e => e.OperationPointId);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Order);
            entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.OperationPointId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Value).HasMaxLength(1000);
            entity.Property(e => e.DefaultValue).IsRequired().HasMaxLength(1000).HasDefaultValue(string.Empty);
            entity.Property(e => e.IsRequired).HasDefaultValue(false);
            entity.Property(e => e.Order).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.EnumOptions).HasMaxLength(2000);
            entity.Property(e => e.ValidationRule).HasMaxLength(500);
            entity.Property(e => e.ValidationErrorMessage).HasMaxLength(200);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.Parameters)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
