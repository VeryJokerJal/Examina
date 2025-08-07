using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScoreToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 更新 SimplifiedQuestions 表的 Score 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "SimplifiedQuestions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10.0m,
                oldClrType: typeof(int),
                oldType: "int");

            // 更新 ExamQuestions 表的 Score 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "ExamQuestions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 10);

            // 更新 ExamSubjects 表的 Score 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "ExamSubjects",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 20.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 20);

            // 更新 Exams 表的 TotalScore 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalScore",
                table: "Exams",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 100.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            // 更新 Exams 表的 PassingScore 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "PassingScore",
                table: "Exams",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                defaultValue: 60.0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 60);

            // 更新 ExamSubjects 表的 MinScore 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "MinScore",
                table: "ExamSubjects",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 更新 PracticeQuestions 表的 Score 字段
            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "PracticeQuestions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10.0m,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚 SimplifiedQuestions 表的 Score 字段
            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "SimplifiedQuestions",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 10.0m);

            // 回滚 ExamQuestions 表的 Score 字段
            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ExamQuestions",
                type: "int",
                nullable: false,
                defaultValue: 10,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 10.0m);

            // 回滚 ExamSubjects 表的 Score 字段
            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "ExamSubjects",
                type: "int",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 20.0m);

            // 回滚 Exams 表的 TotalScore 字段
            migrationBuilder.AlterColumn<int>(
                name: "TotalScore",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldDefaultValue: 100.0m);

            // 回滚 Exams 表的 PassingScore 字段
            migrationBuilder.AlterColumn<int>(
                name: "PassingScore",
                table: "Exams",
                type: "int",
                nullable: true,
                defaultValue: 60,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldNullable: true,
                oldDefaultValue: 60.0m);

            // 回滚 ExamSubjects 表的 MinScore 字段
            migrationBuilder.AlterColumn<int>(
                name: "MinScore",
                table: "ExamSubjects",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            // 回滚 PracticeQuestions 表的 Score 字段
            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "PracticeQuestions",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 10.0m);
        }
    }
}
