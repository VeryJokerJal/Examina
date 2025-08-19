using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableTrialToComprehensiveTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableTrial",
                table: "ImportedComprehensiveTrainings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MockExamConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    TotalScore = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    PassingScore = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    RandomizeQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ExtractionRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockExamConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockExamConfigurations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MockExams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConfigurationId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<int>(type: "int", nullable: false),
                    PassingScore = table.Column<int>(type: "int", nullable: false),
                    RandomizeQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExtractedQuestions = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "Created")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockExams_MockExamConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "MockExamConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MockExams_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamConfigurations_CreatedAt",
                table: "MockExamConfigurations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamConfigurations_CreatedBy",
                table: "MockExamConfigurations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MockExamConfigurations_IsEnabled",
                table: "MockExamConfigurations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_CompletedAt",
                table: "MockExams",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_ConfigurationId",
                table: "MockExams",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_CreatedAt",
                table: "MockExams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_ExpiresAt",
                table: "MockExams",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_StartedAt",
                table: "MockExams",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_Status",
                table: "MockExams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MockExams_StudentId",
                table: "MockExams",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockExams");

            migrationBuilder.DropTable(
                name: "MockExamConfigurations");

            migrationBuilder.DropColumn(
                name: "EnableTrial",
                table: "ImportedComprehensiveTrainings");
        }
    }
}
