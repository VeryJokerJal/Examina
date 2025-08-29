using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateCodeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "ImportedSpecializedTrainingQuestions",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "ImportedQuestions",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "ImportedComprehensiveTrainingQuestions",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "ImportedSpecializedTrainingQuestions");

            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "ImportedQuestions");

            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "ImportedComprehensiveTrainingQuestions");
        }
    }
}
