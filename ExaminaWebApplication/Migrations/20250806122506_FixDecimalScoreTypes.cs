using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalScoreTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionCount",
                table: "ExamSubjects");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalScore",
                table: "SpecializedPractices",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "PassingScore",
                table: "SpecializedPractices",
                type: "decimal(6,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "SimplifiedQuestions",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 10.0m,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Requirements",
                table: "SimplifiedQuestions",
                type: "varchar(5000)",
                maxLength: 5000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "QuestionType",
                table: "SimplifiedQuestions",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "SimplifiedQuestions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "PracticeQuestions",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "ExamSubjects",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 20.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinScore",
                table: "ExamSubjects",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalScore",
                table: "Exams",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 100.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "PassingScore",
                table: "Exams",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 60.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 60);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "ExamQuestions",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 10.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 10);

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_CreatedAt",
                table: "SimplifiedQuestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_IsEnabled",
                table: "SimplifiedQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_OperationType",
                table: "SimplifiedQuestions",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_QuestionType",
                table: "SimplifiedQuestions",
                column: "QuestionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimplifiedQuestions_CreatedAt",
                table: "SimplifiedQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SimplifiedQuestions_IsEnabled",
                table: "SimplifiedQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SimplifiedQuestions_OperationType",
                table: "SimplifiedQuestions");

            migrationBuilder.DropIndex(
                name: "IX_SimplifiedQuestions_QuestionType",
                table: "SimplifiedQuestions");

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "SpecializedPractices",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<int>(
                name: "PassingScore",
                table: "SpecializedPractices",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "SimplifiedQuestions",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldDefaultValue: 10.0m);

            migrationBuilder.AlterColumn<string>(
                name: "Requirements",
                table: "SimplifiedQuestions",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5000)",
                oldMaxLength: 5000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "QuestionType",
                table: "SimplifiedQuestions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "SimplifiedQuestions",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "PracticeQuestions",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ExamSubjects",
                type: "int",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldDefaultValue: 20.0m);

            migrationBuilder.AlterColumn<int>(
                name: "MinScore",
                table: "ExamSubjects",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionCount",
                table: "ExamSubjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldDefaultValue: 100.0m);

            migrationBuilder.AlterColumn<int>(
                name: "PassingScore",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 60,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldDefaultValue: 60.0m);

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ExamQuestions",
                type: "int",
                nullable: false,
                defaultValue: 10,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldDefaultValue: 10.0m);
        }
    }
}
