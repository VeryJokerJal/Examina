using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

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

        // 种子数据
        SeedData(modelBuilder);
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
    }
}
