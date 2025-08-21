using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddMockExamCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockExamCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentUserId = table.Column<int>(type: "int", nullable: false),
                    MockExamId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MockExamCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockExamCompletions_MockExams_MockExamId",
                        column: x => x.MockExamId,
                        principalTable: "MockExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MockExamCompletions_Users_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_CompletedAt",
                table: "MockExamCompletions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_CreatedAt",
                table: "MockExamCompletions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_IsActive",
                table: "MockExamCompletions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_MockExamId",
                table: "MockExamCompletions",
                column: "MockExamId");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_Status",
                table: "MockExamCompletions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_StudentUserId",
                table: "MockExamCompletions",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamCompletions_StudentUserId_MockExamId",
                table: "MockExamCompletions",
                columns: new[] { "StudentUserId", "MockExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockExamCompletions");
        }
    }
}
