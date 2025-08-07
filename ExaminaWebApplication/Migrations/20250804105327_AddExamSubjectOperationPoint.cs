using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSubjectOperationPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSubjectOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamSubjectId = table.Column<int>(type: "int", nullable: false),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    OperationName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationSubjectType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Weight = table.Column<decimal>(type: "decimal(65,30)", nullable: false, defaultValue: 1.0m),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ParameterConfig = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSubjectOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSubjectOperationPoints_ExamSubjects_ExamSubjectId",
                        column: x => x.ExamSubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_ExamSubjectId",
                table: "ExamSubjectOperationPoints",
                column: "ExamSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_IsEnabled",
                table: "ExamSubjectOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_OperationNumber",
                table: "ExamSubjectOperationPoints",
                column: "OperationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_OperationSubjectType",
                table: "ExamSubjectOperationPoints",
                column: "OperationSubjectType");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubjectOperationPoints_SortOrder",
                table: "ExamSubjectOperationPoints",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSubjectOperationPoints");
        }
    }
}
