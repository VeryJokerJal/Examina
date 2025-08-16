using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20250816_195845 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Users_CreatorId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_CreatorId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Organizations");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Organizations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedBy",
                table: "Organizations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Users_CreatedBy",
                table: "Organizations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Users_CreatedBy",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_CreatedBy",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Name",
                table: "Organizations");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Organizations",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatorId",
                table: "Organizations",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Users_CreatorId",
                table: "Organizations",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
