using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250828_045054 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "TotalScore",
                table: "MockExams",
                type: "double",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "TotalScore",
                table: "MockExamConfigurations",
                type: "double",
                nullable: false,
                defaultValue: 100.0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<double>(
                name: "TotalScore",
                table: "ImportedSpecializedTrainings",
                type: "double",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "ImportedSpecializedTrainingModules",
                type: "double",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "ImportedModules",
                type: "double",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "ImportedComprehensiveTrainingModules",
                type: "double",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "MockExams",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "MockExamConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(double),
                oldType: "double",
                oldDefaultValue: 100.0);

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "ImportedSpecializedTrainings",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ImportedSpecializedTrainingModules",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ImportedModules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(double),
                oldType: "double",
                oldDefaultValue: 0.0);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ImportedComprehensiveTrainingModules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(double),
                oldType: "double",
                oldDefaultValue: 0.0);
        }
    }
}
