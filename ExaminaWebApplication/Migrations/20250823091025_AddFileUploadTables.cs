using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddFileUploadTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoredFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileExtension = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadProgress = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPublic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ComprehensiveTrainingFileAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComprehensiveTrainingId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprehensiveTrainingFileAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprehensiveTrainingFileAssociations_ImportedComprehensiveT~",
                        column: x => x.ComprehensiveTrainingId,
                        principalTable: "ImportedComprehensiveTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprehensiveTrainingFileAssociations_UploadedFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprehensiveTrainingFileAssociations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExamFileAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamFileAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamFileAssociations_ImportedExams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ImportedExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamFileAssociations_UploadedFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamFileAssociations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpecializedTrainingFileAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SpecializedTrainingId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecializedTrainingFileAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecializedTrainingFileAssociations_ImportedSpecializedTrain~",
                        column: x => x.SpecializedTrainingId,
                        principalTable: "ImportedSpecializedTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecializedTrainingFileAssociations_UploadedFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecializedTrainingFileAssociations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensiveTrainingFileAssociations_ComprehensiveTrainingI~",
                table: "ComprehensiveTrainingFileAssociations",
                columns: ["ComprehensiveTrainingId", "FileId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensiveTrainingFileAssociations_CreatedBy",
                table: "ComprehensiveTrainingFileAssociations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensiveTrainingFileAssociations_FileId",
                table: "ComprehensiveTrainingFileAssociations",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprehensiveTrainingFileAssociations_FileType",
                table: "ComprehensiveTrainingFileAssociations",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_ExamFileAssociations_CreatedBy",
                table: "ExamFileAssociations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExamFileAssociations_ExamId_FileId",
                table: "ExamFileAssociations",
                columns: ["ExamId", "FileId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamFileAssociations_FileId",
                table: "ExamFileAssociations",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamFileAssociations_FileType",
                table: "ExamFileAssociations",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializedTrainingFileAssociations_CreatedBy",
                table: "SpecializedTrainingFileAssociations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializedTrainingFileAssociations_FileId",
                table: "SpecializedTrainingFileAssociations",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializedTrainingFileAssociations_FileType",
                table: "SpecializedTrainingFileAssociations",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializedTrainingFileAssociations_SpecializedTrainingId_Fi~",
                table: "SpecializedTrainingFileAssociations",
                columns: ["SpecializedTrainingId", "FileId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_DeletedBy",
                table: "UploadedFiles",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_FileHash",
                table: "UploadedFiles",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_IsDeleted",
                table: "UploadedFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_StoredFileName",
                table: "UploadedFiles",
                column: "StoredFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UploadedAt",
                table: "UploadedFiles",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UploadedBy",
                table: "UploadedFiles",
                column: "UploadedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprehensiveTrainingFileAssociations");

            migrationBuilder.DropTable(
                name: "ExamFileAssociations");

            migrationBuilder.DropTable(
                name: "SpecializedTrainingFileAssociations");

            migrationBuilder.DropTable(
                name: "UploadedFiles");
        }
    }
}
