using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InvitationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxUsage = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitationCodes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudentOrganizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    InvitationCodeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentOrganizations_InvitationCodes_InvitationCodeId",
                        column: x => x.InvitationCodeId,
                        principalTable: "InvitationCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOrganizations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentOrganizations_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "Name", "Type" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "河北省重点示范高中，专注于计算机教育", true, "河北省示范高中", 0 },
                    { 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "专业的计算机技能培训机构", true, "计算机培训机构", 1 }
                });

            migrationBuilder.InsertData(
                table: "InvitationCodes",
                columns: new[] { "Id", "Code", "CreatedAt", "ExpiresAt", "IsActive", "MaxUsage", "OrganizationId" },
                values: new object[,]
                {
                    { 1, "SCHOOL1", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, 1 },
                    { 2, "INST001", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), true, 100, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCodes_Code",
                table: "InvitationCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCodes_CreatedAt",
                table: "InvitationCodes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCodes_ExpiresAt",
                table: "InvitationCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCodes_IsActive",
                table: "InvitationCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCodes_OrganizationId",
                table: "InvitationCodes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedAt",
                table: "Organizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedBy",
                table: "Organizations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_IsActive",
                table: "Organizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Type",
                table: "Organizations",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_InvitationCodeId",
                table: "StudentOrganizations",
                column: "InvitationCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_IsActive",
                table: "StudentOrganizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_JoinedAt",
                table: "StudentOrganizations",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_OrganizationId",
                table: "StudentOrganizations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_StudentId",
                table: "StudentOrganizations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentOrganizations_StudentId_OrganizationId",
                table: "StudentOrganizations",
                columns: new[] { "StudentId", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentOrganizations");

            migrationBuilder.DropTable(
                name: "InvitationCodes");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
