using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowsQuestionTemplateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputDescription",
                table: "WindowsQuestionTemplates",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InputExample",
                table: "WindowsQuestionTemplates",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OutputDescription",
                table: "WindowsQuestionTemplates",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OutputExample",
                table: "WindowsQuestionTemplates",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                table: "WindowsQuestionTemplates",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputDescription",
                table: "WindowsQuestionTemplates");

            migrationBuilder.DropColumn(
                name: "InputExample",
                table: "WindowsQuestionTemplates");

            migrationBuilder.DropColumn(
                name: "OutputDescription",
                table: "WindowsQuestionTemplates");

            migrationBuilder.DropColumn(
                name: "OutputExample",
                table: "WindowsQuestionTemplates");

            migrationBuilder.DropColumn(
                name: "Requirements",
                table: "WindowsQuestionTemplates");
        }
    }
}
