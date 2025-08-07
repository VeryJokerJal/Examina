using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddSimplifiedQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExcelOperationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelOperationTemplates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SimplifiedQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    OperationConfig = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimplifiedQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimplifiedQuestions_ExamSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamExcelOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamExcelOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationPoints_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationPoints_ExcelOperationTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExcelOperationTemplates",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcelOperationParameterTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationTemplateId = table.Column<int>(type: "int", nullable: false),
                    ParameterOrder = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowMultipleValues = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DefaultValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExampleValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnumTypeId = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelOperationParameterTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcelOperationParameterTemplates_ExcelEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "ExcelEnumTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExcelOperationParameterTemplates_ExcelOperationTemplates_Ope~",
                        column: x => x.OperationTemplateId,
                        principalTable: "ExcelOperationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamExcelOperationParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamOperationPointId = table.Column<int>(type: "int", nullable: false),
                    ParameterOrder = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowMultipleValues = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ParameterValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExampleValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnumTypeId = table.Column<int>(type: "int", nullable: true),
                    ParameterTemplateId = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamExcelOperationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationParameters_ExamExcelOperationPoints_ExamOp~",
                        column: x => x.ExamOperationPointId,
                        principalTable: "ExamExcelOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationParameters_ExcelEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "ExcelEnumTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamExcelOperationParameters_ExcelOperationParameterTemplate~",
                        column: x => x.ParameterTemplateId,
                        principalTable: "ExcelOperationParameterTemplates",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationParameters_EnumTypeId",
                table: "ExamExcelOperationParameters",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationParameters_ExamOperationPointId",
                table: "ExamExcelOperationParameters",
                column: "ExamOperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationParameters_ParameterTemplateId",
                table: "ExamExcelOperationParameters",
                column: "ParameterTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationPoints_ExamId",
                table: "ExamExcelOperationPoints",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamExcelOperationPoints_TemplateId",
                table: "ExamExcelOperationPoints",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameterTemplates_EnumTypeId",
                table: "ExcelOperationParameterTemplates",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameterTemplates_OperationTemplateId",
                table: "ExcelOperationParameterTemplates",
                column: "OperationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SimplifiedQuestions_SubjectId",
                table: "SimplifiedQuestions",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamExcelOperationParameters");

            migrationBuilder.DropTable(
                name: "SimplifiedQuestions");

            migrationBuilder.DropTable(
                name: "ExamExcelOperationPoints");

            migrationBuilder.DropTable(
                name: "ExcelOperationParameterTemplates");

            migrationBuilder.DropTable(
                name: "ExcelOperationTemplates");
        }
    }
}
