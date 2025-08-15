using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedExamTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamExcelOperationParameters");

            migrationBuilder.DropTable(
                name: "ExamQuestions");

            migrationBuilder.DropTable(
                name: "ExamSubjectOperationPoints");

            migrationBuilder.DropTable(
                name: "PracticeQuestions");

            migrationBuilder.DropTable(
                name: "SimplifiedQuestions");

            migrationBuilder.DropTable(
                name: "WindowsQuestionOperationPoints");

            migrationBuilder.DropTable(
                name: "WordQuestionOperationPoints");

            migrationBuilder.DropTable(
                name: "ExamExcelOperationPoints");

            migrationBuilder.DropTable(
                name: "SpecializedPractices");

            migrationBuilder.DropTable(
                name: "WindowsQuestions");

            migrationBuilder.DropTable(
                name: "WordQuestions");

            migrationBuilder.DropTable(
                name: "ExamSubjects");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.CreateTable(
                name: "IdentityUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedUserName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedEmail = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUser", x => x.Id);
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
                    ImportedBy = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                        name: "FK_ImportedExams_IdentityUser_ImportedBy",
                        column: x => x.ImportedBy,
                        principalTable: "IdentityUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    DefaultValue = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedParameters");

            migrationBuilder.DropTable(
                name: "ImportedOperationPoints");

            migrationBuilder.DropTable(
                name: "ImportedQuestions");

            migrationBuilder.DropTable(
                name: "ImportedModules");

            migrationBuilder.DropTable(
                name: "ImportedSubjects");

            migrationBuilder.DropTable(
                name: "ImportedExams");

            migrationBuilder.DropTable(
                name: "IdentityUser");

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    PublishedBy = table.Column<int>(type: "int", nullable: true),
                    AllowRetake = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExamType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ExtendedConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    MaxRetakeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PassingScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false, defaultValue: 60.0m),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RandomizeQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowAnswers = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowScore = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false, defaultValue: 100.0m),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exams_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_Users_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpecializedPractices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    PublishedBy = table.Column<int>(type: "int", nullable: true),
                    AllowRetake = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    ExtendedConfig = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxRetakeCount = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PassingScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RandomizeQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowAnswers = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowScore = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubjectType = table.Column<int>(type: "int", nullable: false, defaultValue: 4),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecializedPractices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecializedPractices_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecializedPractices_Users_PublishedBy",
                        column: x => x.PublishedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamExcelOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamExcelOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationPoints_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationPoints_ExcelOperationTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExcelOperationTemplates",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    MinScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 20.0m),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubjectConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(65,30)", nullable: false, defaultValue: 1.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSubjects_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PracticeQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PracticeId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OperationConfig = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    Requirements = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeQuestions_SpecializedPractices_PracticeId",
                        column: x => x.PracticeId,
                        principalTable: "SpecializedPractices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamExcelOperationParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EnumTypeId = table.Column<int>(type: "int", nullable: true),
                    ExamOperationPointId = table.Column<int>(type: "int", nullable: false),
                    ParameterTemplateId = table.Column<int>(type: "int", nullable: true),
                    AllowMultipleValues = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    DefaultValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExampleValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ParameterDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterOrder = table.Column<int>(type: "int", nullable: false),
                    ParameterValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamExcelOperationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationParameters_ExamExcelOperationPoints_ExamOp~",
                        column: x => x.ExamOperationPointId,
                        principalTable: "ExamExcelOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationParameters_ExcelEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "ExcelEnumTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationParameters_ExcelOperationParameterTemplate~",
                        column: x => x.ParameterTemplateId,
                        principalTable: "ExcelOperationParameterTemplates",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ExamSubjectId = table.Column<int>(type: "int", nullable: false),
                    ExcelOperationPointId = table.Column<int>(type: "int", nullable: true),
                    ExcelQuestionInstanceId = table.Column<int>(type: "int", nullable: true),
                    ExcelQuestionTemplateId = table.Column<int>(type: "int", nullable: true),
                    AnswerValidationRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    QuestionConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Remarks = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 10.0m),
                    ScoringRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    StandardAnswer = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_ExamSubjects_ExamSubjectId",
                        column: x => x.ExamSubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_ExcelOperationPoints_ExcelOperationPointId",
                        column: x => x.ExcelOperationPointId,
                        principalTable: "ExcelOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_ExcelQuestionInstances_ExcelQuestionInstanceId",
                        column: x => x.ExcelQuestionInstanceId,
                        principalTable: "ExcelQuestionInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_ExcelQuestionTemplates_ExcelQuestionTemplateId",
                        column: x => x.ExcelQuestionTemplateId,
                        principalTable: "ExcelQuestionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamSubjectOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamSubjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    OperationName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    OperationSubjectType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(65,30)", nullable: false, defaultValue: 1.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSubjectOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSubjectOperationPoints_ExamSubjects_ExamSubjectId",
                        column: x => x.ExamSubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SimplifiedQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InputDescription = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InputExample = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    OperationConfig = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OutputDescription = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OutputExample = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Requirements = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 10.0m),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimplifiedQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimplifiedQuestions_ExamSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Requirements = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 10.0m),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsQuestions_ExamSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Requirements = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordQuestions_ExamSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsQuestionOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    OperationConfig = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 5.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsQuestionOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsQuestionOperationPoints_WindowsQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WindowsQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordQuestionOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    OperationConfig = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordQuestionOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordQuestionOperationPoints_WordQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WordQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationParameters_EnumTypeId",
                table: "ExamExcelOperationParameters",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationParameters_ExamOperationPointId",
                table: "ExamExcelOperationParameters",
                column: "ExamOperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationParameters_ParameterTemplateId",
                table: "ExamExcelOperationParameters",
                column: "ParameterTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationPoints_ExamId",
                table: "ExamExcelOperationPoints",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationPoints_TemplateId",
                table: "ExamExcelOperationPoints",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_DifficultyLevel",
                table: "ExamQuestions",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamId",
                table: "ExamQuestions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamSubjectId",
                table: "ExamQuestions",
                column: "ExamSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExcelOperationPointId",
                table: "ExamQuestions",
                column: "ExcelOperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExcelQuestionInstanceId",
                table: "ExamQuestions",
                column: "ExcelQuestionInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExcelQuestionTemplateId",
                table: "ExamQuestions",
                column: "ExcelQuestionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_IsEnabled",
                table: "ExamQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_QuestionNumber",
                table: "ExamQuestions",
                column: "QuestionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_QuestionType",
                table: "ExamQuestions",
                column: "QuestionType");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_SortOrder",
                table: "ExamQuestions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedAt",
                table: "Exams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CreatedBy",
                table: "Exams",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_EndTime",
                table: "Exams",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamType",
                table: "Exams",
                column: "ExamType");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_IsEnabled",
                table: "Exams",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_Name",
                table: "Exams",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_PublishedBy",
                table: "Exams",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_StartTime",
                table: "Exams",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_Status",
                table: "Exams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_ExamSubjectId",
                table: "ExamSubjectOperationPoints",
                column: "ExamSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_IsEnabled",
                table: "ExamSubjectOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_OperationNumber",
                table: "ExamSubjectOperationPoints",
                column: "OperationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_OperationSubjectType",
                table: "ExamSubjectOperationPoints",
                column: "OperationSubjectType");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_SortOrder",
                table: "ExamSubjectOperationPoints",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_ExamId",
                table: "ExamSubjects",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_IsEnabled",
                table: "ExamSubjects",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_SortOrder",
                table: "ExamSubjects",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjects_SubjectType",
                table: "ExamSubjects",
                column: "SubjectType");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeQuestions_PracticeId",
                table: "PracticeQuestions",
                column: "PracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_CreatedAt",
                table: "SimplifiedQuestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_IsEnabled",
                table: "SimplifiedQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_OperationType",
                table: "SimplifiedQuestions",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_QuestionType",
                table: "SimplifiedQuestions",
                column: "QuestionType");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_SubjectId",
                table: "SimplifiedQuestions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializedPractices_CreatedBy",
                table: "SpecializedPractices",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializedPractices_PublishedBy",
                table: "SpecializedPractices",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_CreatedAt",
                table: "WindowsQuestionOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_IsEnabled",
                table: "WindowsQuestionOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_OperationType",
                table: "WindowsQuestionOperationPoints",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_OrderIndex",
                table: "WindowsQuestionOperationPoints",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_QuestionId",
                table: "WindowsQuestionOperationPoints",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestions_CreatedAt",
                table: "WindowsQuestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestions_IsEnabled",
                table: "WindowsQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestions_SubjectId",
                table: "WindowsQuestions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_CreatedAt",
                table: "WordQuestionOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_IsEnabled",
                table: "WordQuestionOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_OperationType",
                table: "WordQuestionOperationPoints",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_OrderIndex",
                table: "WordQuestionOperationPoints",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_QuestionId",
                table: "WordQuestionOperationPoints",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestions_CreatedAt",
                table: "WordQuestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestions_IsEnabled",
                table: "WordQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestions_SubjectId",
                table: "WordQuestions",
                column: "SubjectId");
        }
    }
}
