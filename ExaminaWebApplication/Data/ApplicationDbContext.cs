using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.ImportedExam;
using ExaminaWebApplication.Models.Organization;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

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
    public DbSet<PreConfiguredUser> PreConfiguredUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置User实体
        _ = modelBuilder.Entity<User>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.Username).IsUnique();
            _ = entity.HasIndex(e => e.Email).IsUnique();
            _ = entity.HasIndex(e => e.WeChatOpenId).IsUnique();
            _ = entity.HasIndex(e => e.PhoneNumber).IsUnique();
            _ = entity.HasIndex(e => e.StudentId);
            _ = entity.HasIndex(e => e.Role);
            _ = entity.HasIndex(e => e.IsActive);
            _ = entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            _ = entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            _ = entity.Property(e => e.WeChatOpenId).HasMaxLength(100);
            _ = entity.Property(e => e.RealName).HasMaxLength(50);
            _ = entity.Property(e => e.StudentId).HasMaxLength(50);
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.IsActive).HasDefaultValue(true);
            _ = entity.Property(e => e.IsFirstLogin).HasDefaultValue(true);
            _ = entity.Property(e => e.AllowMultipleDevices).HasDefaultValue(false);
            _ = entity.Property(e => e.MaxDeviceCount).HasDefaultValue(1);

            // 配置枚举 - 设置哨兵值以避免EF Core警告
            _ = entity.Property(e => e.Role)
                  .HasConversion<int>()
                  .HasDefaultValue(UserRole.Student)
                  .HasSentinel(0); // 设置哨兵值为0，当值为0时使用数据库默认值
        });

        // 配置UserDevice实体
        _ = modelBuilder.Entity<UserDevice>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.DeviceFingerprint).IsUnique();
            _ = entity.HasIndex(e => new { e.UserId, e.DeviceFingerprint }).IsUnique();
            _ = entity.HasIndex(e => e.UserId);
            _ = entity.HasIndex(e => e.IsActive);
            _ = entity.HasIndex(e => e.CreatedAt);
            _ = entity.HasIndex(e => e.LastUsedAt);
            _ = entity.HasIndex(e => e.ExpiresAt);
            _ = entity.HasIndex(e => e.IsTrusted);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.DeviceFingerprint).IsRequired().HasMaxLength(255);
            _ = entity.Property(e => e.DeviceName).HasMaxLength(100).HasDefaultValue("");
            _ = entity.Property(e => e.DeviceType).HasMaxLength(50).HasDefaultValue("");
            _ = entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            _ = entity.Property(e => e.BrowserInfo).HasMaxLength(200);
            _ = entity.Property(e => e.IpAddress).HasMaxLength(45);
            _ = entity.Property(e => e.Location).HasMaxLength(200);
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.IsActive).HasDefaultValue(true);
            _ = entity.Property(e => e.IsTrusted).HasDefaultValue(false);

            // 配置外键关系
            _ = entity.HasOne(e => e.User)
                  .WithMany(u => u.Devices)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置UserSession实体
        _ = modelBuilder.Entity<UserSession>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.SessionToken).IsUnique();
            _ = entity.HasIndex(e => e.RefreshToken).IsUnique();
            _ = entity.HasIndex(e => e.UserId);
            _ = entity.HasIndex(e => e.DeviceId);
            _ = entity.HasIndex(e => e.IsActive);
            _ = entity.HasIndex(e => e.CreatedAt);
            _ = entity.HasIndex(e => e.ExpiresAt);
            _ = entity.HasIndex(e => e.LastActivityAt);
            _ = entity.HasIndex(e => e.SessionType);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(500);
            _ = entity.Property(e => e.RefreshToken).HasMaxLength(500);
            _ = entity.Property(e => e.IpAddress).HasMaxLength(45);
            _ = entity.Property(e => e.UserAgent).HasMaxLength(500);
            _ = entity.Property(e => e.Location).HasMaxLength(200);
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.LastActivityAt).IsRequired();
            _ = entity.Property(e => e.IsActive).HasDefaultValue(true);

            // 配置枚举 - 设置哨兵值以避免EF Core警告
            _ = entity.Property(e => e.SessionType)
                  .HasConversion<int>()
                  .HasDefaultValue(SessionType.JwtToken)
                  .HasSentinel(0); // 设置哨兵值为0，当值为0时使用数据库默认值

            // 配置外键关系
            _ = entity.HasOne(e => e.User)
                  .WithMany(u => u.Sessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 配置Organization实体（显式指定 Creator 外键为 CreatedBy，避免生成影子列 CreatorId）
        _ = modelBuilder.Entity<Organization>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 索引与属性
            _ = entity.HasIndex(e => e.Name);
            _ = entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Creator 导航 -> 使用 CreatedBy 作为外键
            _ = entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Cascade);
        });


        // 配置导入考试相关实体
        ConfigureImportedExamEntities(modelBuilder);
    }

    /// <summary>
    /// 配置导入考试相关实体
    /// </summary>
    private static void ConfigureImportedExamEntities(ModelBuilder modelBuilder)
    {
        // 配置ImportedExam实体
        _ = modelBuilder.Entity<ImportedExam>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.OriginalExamId).IsUnique();
            _ = entity.HasIndex(e => e.ImportedBy);
            _ = entity.HasIndex(e => e.ImportedAt);
            _ = entity.HasIndex(e => e.Name);
            _ = entity.HasIndex(e => e.ExamType);
            _ = entity.HasIndex(e => e.Status);
            _ = entity.HasIndex(e => e.ImportStatus);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.OriginalExamId).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            _ = entity.Property(e => e.Description).HasMaxLength(1000);
            _ = entity.Property(e => e.ExamType).IsRequired().HasMaxLength(50).HasDefaultValue("UnifiedExam");
            _ = entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Draft");
            _ = entity.Property(e => e.TotalScore).IsRequired().HasColumnType("decimal(6,2)").HasDefaultValue(100.0m);
            _ = entity.Property(e => e.DurationMinutes).IsRequired().HasDefaultValue(120);
            _ = entity.Property(e => e.PassingScore).IsRequired().HasColumnType("decimal(6,2)").HasDefaultValue(60.0m);
            _ = entity.Property(e => e.AllowRetake).HasDefaultValue(false);
            _ = entity.Property(e => e.MaxRetakeCount).HasDefaultValue(0);
            _ = entity.Property(e => e.RandomizeQuestions).HasDefaultValue(false);
            _ = entity.Property(e => e.ShowScore).HasDefaultValue(true);
            _ = entity.Property(e => e.ShowAnswers).HasDefaultValue(false);
            _ = entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            _ = entity.Property(e => e.Tags).HasMaxLength(500);
            _ = entity.Property(e => e.ExtendedConfig).HasColumnType("json");
            _ = entity.Property(e => e.ImportedBy).IsRequired();
            _ = entity.Property(e => e.ImportedAt).IsRequired();
            _ = entity.Property(e => e.OriginalCreatedBy).IsRequired();
            _ = entity.Property(e => e.OriginalCreatedAt).IsRequired();
            _ = entity.Property(e => e.ImportFileName).HasMaxLength(255);
            _ = entity.Property(e => e.ImportVersion).HasMaxLength(20).HasDefaultValue("1.0");
            _ = entity.Property(e => e.ImportStatus).IsRequired().HasMaxLength(50).HasDefaultValue("Success");
            _ = entity.Property(e => e.ImportErrorMessage).HasMaxLength(2000);

            // 配置外键关系
            _ = entity.HasOne(e => e.Importer)
                  .WithMany()
                  .HasForeignKey(e => e.ImportedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 配置ImportedSubject实体
        _ = modelBuilder.Entity<ImportedSubject>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.ExamId);
            _ = entity.HasIndex(e => e.OriginalSubjectId);
            _ = entity.HasIndex(e => e.SubjectType);
            _ = entity.HasIndex(e => e.SortOrder);
            _ = entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.OriginalSubjectId).IsRequired();
            _ = entity.Property(e => e.ExamId).IsRequired();
            _ = entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.Description).HasMaxLength(500);
            _ = entity.Property(e => e.Score).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(20.0m);
            _ = entity.Property(e => e.DurationMinutes).IsRequired().HasDefaultValue(30);
            _ = entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(1);
            _ = entity.Property(e => e.IsRequired).HasDefaultValue(true);
            _ = entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            _ = entity.Property(e => e.MinScore).HasColumnType("decimal(5,2)");
            _ = entity.Property(e => e.Weight).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(1.0m);
            _ = entity.Property(e => e.SubjectConfig).HasColumnType("json");
            _ = entity.Property(e => e.QuestionCount).IsRequired().HasDefaultValue(0);
            _ = entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            _ = entity.HasOne(e => e.Exam)
                  .WithMany(ex => ex.Subjects)
                  .HasForeignKey(e => e.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedModule实体
        _ = modelBuilder.Entity<ImportedModule>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.ExamId);
            _ = entity.HasIndex(e => e.OriginalModuleId);
            _ = entity.HasIndex(e => e.Type);
            _ = entity.HasIndex(e => e.Order);
            _ = entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.OriginalModuleId).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.ExamId).IsRequired();
            _ = entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.Description).HasMaxLength(500);
            _ = entity.Property(e => e.Score).IsRequired().HasDefaultValue(0);
            _ = entity.Property(e => e.Order).IsRequired().HasDefaultValue(1);
            _ = entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            _ = entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            _ = entity.HasOne(e => e.Exam)
                  .WithMany(ex => ex.Modules)
                  .HasForeignKey(e => e.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedQuestion实体
        _ = modelBuilder.Entity<ImportedQuestion>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.ExamId);
            _ = entity.HasIndex(e => e.SubjectId);
            _ = entity.HasIndex(e => e.ModuleId);
            _ = entity.HasIndex(e => e.OriginalQuestionId);
            _ = entity.HasIndex(e => e.QuestionType);
            _ = entity.HasIndex(e => e.SortOrder);
            _ = entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.OriginalQuestionId).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.ExamId).IsRequired();
            _ = entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            _ = entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            _ = entity.Property(e => e.QuestionType).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.Score).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(10.0m);
            _ = entity.Property(e => e.DifficultyLevel).IsRequired().HasDefaultValue(1);
            _ = entity.Property(e => e.EstimatedMinutes).IsRequired().HasDefaultValue(5);
            _ = entity.Property(e => e.SortOrder).IsRequired().HasDefaultValue(1);
            _ = entity.Property(e => e.IsRequired).HasDefaultValue(true);
            _ = entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            _ = entity.Property(e => e.QuestionConfig).HasColumnType("json");
            _ = entity.Property(e => e.AnswerValidationRules).HasColumnType("json");
            _ = entity.Property(e => e.StandardAnswer).HasColumnType("json");
            _ = entity.Property(e => e.ScoringRules).HasColumnType("json");
            _ = entity.Property(e => e.Tags).HasMaxLength(500);
            _ = entity.Property(e => e.Remarks).HasMaxLength(1000);
            _ = entity.Property(e => e.ProgramInput).HasMaxLength(1000);
            _ = entity.Property(e => e.ExpectedOutput).HasMaxLength(2000);
            _ = entity.Property(e => e.OriginalCreatedAt).IsRequired();
            _ = entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            _ = entity.HasOne(e => e.Exam)
                  .WithMany()
                  .HasForeignKey(e => e.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne(e => e.Subject)
                  .WithMany(s => s.Questions)
                  .HasForeignKey(e => e.SubjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne(e => e.Module)
                  .WithMany(m => m.Questions)
                  .HasForeignKey(e => e.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedOperationPoint实体
        _ = modelBuilder.Entity<ImportedOperationPoint>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.QuestionId);
            _ = entity.HasIndex(e => e.OriginalOperationPointId);
            _ = entity.HasIndex(e => e.ModuleType);
            _ = entity.HasIndex(e => e.Order);
            _ = entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.OriginalOperationPointId).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.QuestionId).IsRequired();
            _ = entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.Description).HasMaxLength(500);
            _ = entity.Property(e => e.ModuleType).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.Score).IsRequired().HasColumnType("decimal(5,2)").HasDefaultValue(0.0m);
            _ = entity.Property(e => e.Order).IsRequired().HasDefaultValue(1);
            _ = entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            _ = entity.Property(e => e.CreatedTime).HasMaxLength(50);
            _ = entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            _ = entity.HasOne(e => e.Question)
                  .WithMany(q => q.OperationPoints)
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置ImportedParameter实体
        _ = modelBuilder.Entity<ImportedParameter>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => e.OperationPointId);
            _ = entity.HasIndex(e => e.Name);
            _ = entity.HasIndex(e => e.Type);
            _ = entity.HasIndex(e => e.Order);
            _ = entity.HasIndex(e => e.ImportedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.OperationPointId).IsRequired();
            _ = entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            _ = entity.Property(e => e.Description).HasMaxLength(500);
            _ = entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.Value).HasMaxLength(1000);
            _ = entity.Property(e => e.DefaultValue).IsRequired().HasMaxLength(1000).HasDefaultValue(string.Empty);
            _ = entity.Property(e => e.IsRequired).HasDefaultValue(false);
            _ = entity.Property(e => e.Order).IsRequired().HasDefaultValue(1);
            _ = entity.Property(e => e.EnumOptions).HasMaxLength(2000);
            _ = entity.Property(e => e.ValidationRule).HasMaxLength(500);
            _ = entity.Property(e => e.ValidationErrorMessage).HasMaxLength(200);
            _ = entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            _ = entity.Property(e => e.ImportedAt).IsRequired();

            // 配置外键关系
            _ = entity.HasOne(e => e.OperationPoint)
                  .WithMany(op => op.Parameters)
                  .HasForeignKey(e => e.OperationPointId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 配置PreConfiguredUser实体
        _ = modelBuilder.Entity<PreConfiguredUser>(entity =>
        {
            _ = entity.HasKey(e => e.Id);

            // 配置索引
            _ = entity.HasIndex(e => new { e.Username, e.OrganizationId }).IsUnique();
            _ = entity.HasIndex(e => e.PhoneNumber);
            _ = entity.HasIndex(e => e.IsApplied);
            _ = entity.HasIndex(e => e.CreatedAt);

            // 配置属性
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
            _ = entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            _ = entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            _ = entity.Property(e => e.RealName).HasMaxLength(100);
            _ = entity.Property(e => e.StudentId).HasMaxLength(50);
            _ = entity.Property(e => e.CreatedAt).IsRequired();
            _ = entity.Property(e => e.IsApplied).HasDefaultValue(false);
            _ = entity.Property(e => e.Notes).HasMaxLength(500);

            // 配置外键关系
            _ = entity.HasOne(e => e.Organization)
                  .WithMany()
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            _ = entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            _ = entity.HasOne(e => e.AppliedToUser)
                  .WithMany()
                  .HasForeignKey(e => e.AppliedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
