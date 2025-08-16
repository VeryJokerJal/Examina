using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250817_034829 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportedComprehensiveTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalComprehensiveTrainingId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComprehensiveTrainingType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "UnifiedTraining")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(6,2)", nullable: false, defaultValue: 100.0m),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AllowRetake = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxRetakeCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ImportedComprehensiveTrainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainings_Users_ImportedBy",
                        column: x => x.ImportedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedComprehensiveTrainingModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalModuleId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComprehensiveTrainingId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ImportedComprehensiveTrainingModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingModules_ImportedComprehensiveTr~",
                        column: x => x.ComprehensiveTrainingId,
                        principalTable: "ImportedComprehensiveTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedComprehensiveTrainingSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalSubjectId = table.Column<int>(type: "int", nullable: false),
                    ComprehensiveTrainingId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ImportedComprehensiveTrainingSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingSubjects_ImportedComprehensiveT~",
                        column: x => x.ComprehensiveTrainingId,
                        principalTable: "ImportedComprehensiveTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedComprehensiveTrainingQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalQuestionId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComprehensiveTrainingId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ImportedComprehensiveTrainingQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingQuestions_ImportedComprehensive~",
                        column: x => x.ComprehensiveTrainingId,
                        principalTable: "ImportedComprehensiveTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingQuestions_ImportedComprehensiv~1",
                        column: x => x.ModuleId,
                        principalTable: "ImportedComprehensiveTrainingModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingQuestions_ImportedComprehensiv~2",
                        column: x => x.SubjectId,
                        principalTable: "ImportedComprehensiveTrainingSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedComprehensiveTrainingOperationPoints",
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
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedTime = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedComprehensiveTrainingOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingOperationPoints_ImportedCompreh~",
                        column: x => x.QuestionId,
                        principalTable: "ImportedComprehensiveTrainingQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImportedComprehensiveTrainingParameters",
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
                    table.PrimaryKey("PK_ImportedComprehensiveTrainingParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedComprehensiveTrainingParameters_ImportedComprehensiv~",
                        column: x => x.OperationPointId,
                        principalTable: "ImportedComprehensiveTrainingOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingModules_ComprehensiveTrainingId",
                table: "ImportedComprehensiveTrainingModules",
                column: "ComprehensiveTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingModules_ImportedAt",
                table: "ImportedComprehensiveTrainingModules",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingModules_Order",
                table: "ImportedComprehensiveTrainingModules",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingModules_OriginalModuleId",
                table: "ImportedComprehensiveTrainingModules",
                column: "OriginalModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingModules_Type",
                table: "ImportedComprehensiveTrainingModules",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingOperationPoints_ImportedAt",
                table: "ImportedComprehensiveTrainingOperationPoints",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingOperationPoints_ModuleType",
                table: "ImportedComprehensiveTrainingOperationPoints",
                column: "ModuleType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingOperationPoints_Order",
                table: "ImportedComprehensiveTrainingOperationPoints",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingOperationPoints_OriginalOperati~",
                table: "ImportedComprehensiveTrainingOperationPoints",
                column: "OriginalOperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingOperationPoints_QuestionId",
                table: "ImportedComprehensiveTrainingOperationPoints",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingParameters_ImportedAt",
                table: "ImportedComprehensiveTrainingParameters",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingParameters_Name",
                table: "ImportedComprehensiveTrainingParameters",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingParameters_OperationPointId",
                table: "ImportedComprehensiveTrainingParameters",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingParameters_Order",
                table: "ImportedComprehensiveTrainingParameters",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingParameters_Type",
                table: "ImportedComprehensiveTrainingParameters",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_ComprehensiveTraining~",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "ComprehensiveTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_ImportedAt",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_ModuleId",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_OriginalQuestionId",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "OriginalQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_QuestionType",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "QuestionType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_SortOrder",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingQuestions_SubjectId",
                table: "ImportedComprehensiveTrainingQuestions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_ComprehensiveTrainingType",
                table: "ImportedComprehensiveTrainings",
                column: "ComprehensiveTrainingType");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_ImportedAt",
                table: "ImportedComprehensiveTrainings",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_ImportedBy",
                table: "ImportedComprehensiveTrainings",
                column: "ImportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_ImportStatus",
                table: "ImportedComprehensiveTrainings",
                column: "ImportStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_Name",
                table: "ImportedComprehensiveTrainings",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_OriginalComprehensiveTraining~",
                table: "ImportedComprehensiveTrainings",
                column: "OriginalComprehensiveTrainingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainings_Status",
                table: "ImportedComprehensiveTrainings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingSubjects_ComprehensiveTrainingId",
                table: "ImportedComprehensiveTrainingSubjects",
                column: "ComprehensiveTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingSubjects_ImportedAt",
                table: "ImportedComprehensiveTrainingSubjects",
                column: "ImportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingSubjects_OriginalSubjectId",
                table: "ImportedComprehensiveTrainingSubjects",
                column: "OriginalSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingSubjects_SortOrder",
                table: "ImportedComprehensiveTrainingSubjects",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedComprehensiveTrainingSubjects_SubjectType",
                table: "ImportedComprehensiveTrainingSubjects",
                column: "SubjectType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedComprehensiveTrainingParameters");

            migrationBuilder.DropTable(
                name: "ImportedComprehensiveTrainingOperationPoints");

            migrationBuilder.DropTable(
                name: "ImportedComprehensiveTrainingQuestions");

            migrationBuilder.DropTable(
                name: "ImportedComprehensiveTrainingModules");

            migrationBuilder.DropTable(
                name: "ImportedComprehensiveTrainingSubjects");

            migrationBuilder.DropTable(
                name: "ImportedComprehensiveTrainings");
        }
    }
}
