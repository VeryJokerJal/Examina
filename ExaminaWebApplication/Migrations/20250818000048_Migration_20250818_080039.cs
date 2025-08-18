using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250818_080039 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NonOrganizationUserWhitelists");

            migrationBuilder.DropColumn(
                name: "HasFullAccess",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasFullAccess",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "NonOrganizationUserWhitelists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
    }
}
