using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class CreatePreConfiguredUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreConfiguredUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RealName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StudentId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsApplied = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AppliedToUserId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreConfiguredUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreConfiguredUsers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreConfiguredUsers_Users_AppliedToUserId",
                        column: x => x.AppliedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreConfiguredUsers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_AppliedToUserId",
                table: "PreConfiguredUsers",
                column: "AppliedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_CreatedAt",
                table: "PreConfiguredUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_CreatedBy",
                table: "PreConfiguredUsers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_IsApplied",
                table: "PreConfiguredUsers",
                column: "IsApplied");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_OrganizationId",
                table: "PreConfiguredUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_PhoneNumber",
                table: "PreConfiguredUsers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PreConfiguredUsers_Username_OrganizationId",
                table: "PreConfiguredUsers",
                columns: new[] { "Username", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreConfiguredUsers");
        }
    }
}
