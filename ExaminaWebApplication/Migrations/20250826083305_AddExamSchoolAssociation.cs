using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSchoolAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSchoolAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSchoolAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSchoolAssociations_ImportedExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ImportedExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSchoolAssociations_Organizations_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSchoolAssociations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchoolAssociations_CreatedAt",
                table: "ExamSchoolAssociations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchoolAssociations_CreatedBy",
                table: "ExamSchoolAssociations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchoolAssociations_ExamId",
                table: "ExamSchoolAssociations",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchoolAssociations_ExamId_SchoolId",
                table: "ExamSchoolAssociations",
                columns: new[] { "ExamId", "SchoolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchoolAssociations_IsActive",
                table: "ExamSchoolAssociations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchoolAssociations_SchoolId",
                table: "ExamSchoolAssociations",
                column: "SchoolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSchoolAssociations");
        }
    }
}
