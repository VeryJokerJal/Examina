using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddExamManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExamType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TotalScore = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AllowRetake = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    MaxRetakeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PassingScore = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    RandomizeQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowScore = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowAnswers = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PublishedBy = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtendedConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                name: "ExamSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubjectName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    QuestionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    MinScore = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(65,30)", nullable: false, defaultValue: 1.0m),
                    SubjectConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                name: "ExamQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ExamSubjectId = table.Column<int>(type: "int", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Score = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ExcelOperationPointId = table.Column<int>(type: "int", nullable: true),
                    ExcelQuestionTemplateId = table.Column<int>(type: "int", nullable: true),
                    ExcelQuestionInstanceId = table.Column<int>(type: "int", nullable: true),
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
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamQuestions");

            migrationBuilder.DropTable(
                name: "ExamSubjects");

            migrationBuilder.DropTable(
                name: "Exams");
        }
    }
}
