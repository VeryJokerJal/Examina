using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddExamCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BenchSuiteScoringResult",
                table: "ComprehensiveTrainingCompletions",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentUserId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    CompletionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BenchSuiteScoringResult = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamCompletions_ImportedExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ImportedExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamCompletions_Users_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_CompletedAt",
                table: "ExamCompletions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_CreatedAt",
                table: "ExamCompletions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_ExamId",
                table: "ExamCompletions",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_IsActive",
                table: "ExamCompletions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_Status",
                table: "ExamCompletions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_StudentUserId",
                table: "ExamCompletions",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCompletions_StudentUserId_ExamId",
                table: "ExamCompletions",
                columns: new[] { "StudentUserId", "ExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamCompletions");

            migrationBuilder.DropColumn(
                name: "BenchSuiteScoringResult",
                table: "ComprehensiveTrainingCompletions");
        }
    }
}
