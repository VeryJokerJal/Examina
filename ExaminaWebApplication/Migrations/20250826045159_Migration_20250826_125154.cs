using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250826_125154 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CSharpDirectScore",
                table: "ImportedSpecializedTrainingQuestions",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeFilePath",
                table: "ImportedSpecializedTrainingQuestions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentFilePath",
                table: "ImportedSpecializedTrainingQuestions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedOutput",
                table: "ImportedSpecializedTrainingQuestions",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProgramInput",
                table: "ImportedSpecializedTrainingQuestions",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "CSharpDirectScore",
                table: "ImportedQuestions",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeFilePath",
                table: "ImportedQuestions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentFilePath",
                table: "ImportedQuestions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "CSharpDirectScore",
                table: "ImportedComprehensiveTrainingQuestions",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeFilePath",
                table: "ImportedComprehensiveTrainingQuestions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentFilePath",
                table: "ImportedComprehensiveTrainingQuestions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CSharpDirectScore",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "CodeFilePath",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "DocumentFilePath",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "ExpectedOutput",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "ProgramInput",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "CSharpDirectScore",
                table: "ImportedQuestions");

            migrationBuilder.DropColumn(
                name: "CodeFilePath",
                table: "ImportedQuestions");

            migrationBuilder.DropColumn(
                name: "DocumentFilePath",
                table: "ImportedQuestions");

            migrationBuilder.DropColumn(
                name: "CSharpDirectScore",
                table: "ImportedComprehensiveTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "CodeFilePath",
                table: "ImportedComprehensiveTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "DocumentFilePath",
                table: "ImportedComprehensiveTrainingQuestions");
        }
    }
}
