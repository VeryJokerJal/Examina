using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250829_073955 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CSharpQuestionType",
                table: "ImportedSpecializedTrainingQuestions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeBlanks",
                table: "ImportedSpecializedTrainingQuestions",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CSharpQuestionType",
                table: "ImportedComprehensiveTrainingQuestions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeBlanks",
                table: "ImportedComprehensiveTrainingQuestions",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CSharpQuestionType",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "CodeBlanks",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "CSharpQuestionType",
                table: "ImportedComprehensiveTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "CodeBlanks",
                table: "ImportedComprehensiveTrainingQuestions");
        }
    }
}
