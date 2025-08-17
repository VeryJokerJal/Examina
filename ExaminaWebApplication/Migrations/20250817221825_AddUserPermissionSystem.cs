using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasFullAccess",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "NonOrganizationStudentOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NonOrganizationStudentId = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonOrganizationStudentOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonOrganizationStudentOrganizations_NonOrganizationStudents_~",
                        column: x => x.NonOrganizationStudentId,
                        principalTable: "NonOrganizationStudents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonOrganizationStudentOrganizations_Organizations_Organizati~",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NonOrganizationStudentOrganizations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NonOrganizationUserWhitelists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonOrganizationUserWhitelists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonOrganizationUserWhitelists_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonOrganizationUserWhitelists_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonOrganizationUserWhitelists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationStudentOrganizations_CreatedAt",
                table: "NonOrganizationStudentOrganizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationStudentOrganizations_CreatedBy",
                table: "NonOrganizationStudentOrganizations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationStudentOrganizations_IsActive",
                table: "NonOrganizationStudentOrganizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationStudentOrganizations_NonOrganizationStudentId",
                table: "NonOrganizationStudentOrganizations",
                column: "NonOrganizationStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationStudentOrganizations_NonOrganizationStudentId~",
                table: "NonOrganizationStudentOrganizations",
                columns: new[] { "NonOrganizationStudentId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationStudentOrganizations_OrganizationId",
                table: "NonOrganizationStudentOrganizations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationUserWhitelists_CreatedAt",
                table: "NonOrganizationUserWhitelists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationUserWhitelists_CreatedBy",
                table: "NonOrganizationUserWhitelists",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationUserWhitelists_IsActive",
                table: "NonOrganizationUserWhitelists",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationUserWhitelists_UpdatedBy",
                table: "NonOrganizationUserWhitelists",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NonOrganizationUserWhitelists_UserId",
                table: "NonOrganizationUserWhitelists",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NonOrganizationStudentOrganizations");

            migrationBuilder.DropTable(
                name: "NonOrganizationUserWhitelists");

            migrationBuilder.DropColumn(
                name: "HasFullAccess",
                table: "Users");
        }
    }
}
