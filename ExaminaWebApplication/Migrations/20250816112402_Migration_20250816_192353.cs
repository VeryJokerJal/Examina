using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250816_192353 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WeChatOpenId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RealName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StudentId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsFirstLogin = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    AllowMultipleDevices = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    MaxDeviceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedExams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalExamId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExamType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "UnifiedExam")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false, defaultValue: 100.0m),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AllowRetake = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    MaxRetakeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PassingScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false, defaultValue: 60.0m),
                    RandomizeQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowScore = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowAnswers = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtendedConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportedBy = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OriginalCreatedBy = table.Column<int>(type: "int", nullable: false),
                    OriginalCreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OriginalUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OriginalPublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OriginalPublishedBy = table.Column<int>(type: "int", nullable: true),
                    ImportFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportFileSize = table.Column<long>(type: "bigint", nullable: false),
                    ImportVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "1.0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Success")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedExams_Users_ImportedBy",
                        column: x => x.ImportedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperatingSystem = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BrowserInfo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsTrusted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalModuleId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedModules_ImportedExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ImportedExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalSubjectId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SubjectType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 20.0m),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    MinScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 1.0m),
                    SubjectConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedSubjects_ImportedExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ImportedExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InvitationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    MaxUsage = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationCodes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: true),
                    SessionToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    LogoutAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_UserDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "UserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalQuestionId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    ModuleId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 10.0m),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    QuestionConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnswerValidationRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StandardAnswer = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScoringRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgramInput = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpectedOutput = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalCreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OriginalUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedQuestions_ImportedExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ImportedExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedQuestions_ImportedModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "ImportedModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedQuestions_ImportedSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ImportedSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudentOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    InvitationCodeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentOrganizations_InvitationCodes_InvitationCodeId",
                        column: x => x.InvitationCodeId,
                        principalTable: "InvitationCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentOrganizations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentOrganizations_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalOperationPointId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModuleType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0.0m),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedTime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedOperationPoints_ImportedQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ImportedQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationPointId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultValue = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EnumOptions = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidationRule = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidationErrorMessage = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinValue = table.Column<double>(type: "double", nullable: true),
                    MaxValue = table.Column<double>(type: "double", nullable: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedParameters_ImportedOperationPoints_OperationPointId",
                        column: x => x.OperationPointId,
                        principalTable: "ImportedOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_ExamType",
                table: "ImportedExams",
                column: "ExamType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_ImportedAt",
                table: "ImportedExams",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_ImportedBy",
                table: "ImportedExams",
                column: "ImportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_ImportStatus",
                table: "ImportedExams",
                column: "ImportStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_Name",
                table: "ImportedExams",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_OriginalExamId",
                table: "ImportedExams",
                column: "OriginalExamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedExams_Status",
                table: "ImportedExams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedModules_ExamId",
                table: "ImportedModules",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedModules_ImportedAt",
                table: "ImportedModules",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedModules_Order",
                table: "ImportedModules",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedModules_OriginalModuleId",
                table: "ImportedModules",
                column: "OriginalModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedModules_Type",
                table: "ImportedModules",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOperationPoints_ImportedAt",
                table: "ImportedOperationPoints",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOperationPoints_ModuleType",
                table: "ImportedOperationPoints",
                column: "ModuleType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOperationPoints_Order",
                table: "ImportedOperationPoints",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOperationPoints_OriginalOperationPointId",
                table: "ImportedOperationPoints",
                column: "OriginalOperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOperationPoints_QuestionId",
                table: "ImportedOperationPoints",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedParameters_ImportedAt",
                table: "ImportedParameters",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedParameters_Name",
                table: "ImportedParameters",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedParameters_OperationPointId",
                table: "ImportedParameters",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedParameters_Order",
                table: "ImportedParameters",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedParameters_Type",
                table: "ImportedParameters",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_ExamId",
                table: "ImportedQuestions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_ImportedAt",
                table: "ImportedQuestions",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_ModuleId",
                table: "ImportedQuestions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_OriginalQuestionId",
                table: "ImportedQuestions",
                column: "OriginalQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_QuestionType",
                table: "ImportedQuestions",
                column: "QuestionType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_SortOrder",
                table: "ImportedQuestions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedQuestions_SubjectId",
                table: "ImportedQuestions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSubjects_ExamId",
                table: "ImportedSubjects",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSubjects_ImportedAt",
                table: "ImportedSubjects",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSubjects_OriginalSubjectId",
                table: "ImportedSubjects",
                column: "OriginalSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSubjects_SortOrder",
                table: "ImportedSubjects",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSubjects_SubjectType",
                table: "ImportedSubjects",
                column: "SubjectType");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCodes_OrganizationId",
                table: "InvitationCodes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatorId",
                table: "Organizations",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_InvitationCodeId",
                table: "StudentOrganizations",
                column: "InvitationCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_OrganizationId",
                table: "StudentOrganizations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_StudentId",
                table: "StudentOrganizations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_CreatedAt",
                table: "UserDevices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_DeviceFingerprint",
                table: "UserDevices",
                column: "DeviceFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_ExpiresAt",
                table: "UserDevices",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_IsActive",
                table: "UserDevices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_IsTrusted",
                table: "UserDevices",
                column: "IsTrusted");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_LastUsedAt",
                table: "UserDevices",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId_DeviceFingerprint",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentId",
                table: "Users",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_WeChatOpenId",
                table: "Users",
                column: "WeChatOpenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_CreatedAt",
                table: "UserSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeviceId",
                table: "UserSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastActivityAt",
                table: "UserSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_RefreshToken",
                table: "UserSessions",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionToken",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionType",
                table: "UserSessions",
                column: "SessionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedParameters");

            migrationBuilder.DropTable(
                name: "StudentOrganizations");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "ImportedOperationPoints");

            migrationBuilder.DropTable(
                name: "InvitationCodes");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "ImportedQuestions");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "ImportedModules");

            migrationBuilder.DropTable(
                name: "ImportedSubjects");

            migrationBuilder.DropTable(
                name: "ImportedExams");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
