using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddWindowsOperationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WindowsEnumTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TypeName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsEnumTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OperationMode = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsOperationPoints", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsEnumValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EnumTypeId = table.Column<int>(type: "int", nullable: false),
                    EnumKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnumValue = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsEnumValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsEnumValues_WindowsEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "WindowsEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsOperationParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationPointId = table.Column<int>(type: "int", nullable: false),
                    ParameterOrder = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    AllowMultipleValues = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    EnumTypeId = table.Column<int>(type: "int", nullable: true),
                    ValidationRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExampleValue = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsOperationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsOperationParameters_WindowsEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "WindowsEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WindowsOperationParameters_WindowsOperationPoints_OperationP~",
                        column: x => x.OperationPointId,
                        principalTable: "WindowsOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsQuestionTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationPointId = table.Column<int>(type: "int", nullable: false),
                    TemplateName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionTemplate = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterConfiguration = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsQuestionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsQuestionTemplates_WindowsOperationPoints_OperationPoi~",
                        column: x => x.OperationPointId,
                        principalTable: "WindowsOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsQuestionInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionTemplateId = table.Column<int>(type: "int", nullable: false),
                    InstanceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionContent = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterValues = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpectedAnswer = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScoringCriteria = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsQuestionInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsQuestionInstances_WindowsQuestionTemplates_QuestionTe~",
                        column: x => x.QuestionTemplateId,
                        principalTable: "WindowsQuestionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumTypes_Category",
                table: "WindowsEnumTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumTypes_IsEnabled",
                table: "WindowsEnumTypes",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumTypes_TypeName",
                table: "WindowsEnumTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumValues_EnumKey",
                table: "WindowsEnumValues",
                column: "EnumKey");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumValues_EnumTypeId",
                table: "WindowsEnumValues",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumValues_IsDefault",
                table: "WindowsEnumValues",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumValues_IsEnabled",
                table: "WindowsEnumValues",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsEnumValues_SortOrder",
                table: "WindowsEnumValues",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationParameters_DataType",
                table: "WindowsOperationParameters",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationParameters_EnumTypeId",
                table: "WindowsOperationParameters",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationParameters_IsEnabled",
                table: "WindowsOperationParameters",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationParameters_IsRequired",
                table: "WindowsOperationParameters",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationParameters_OperationPointId",
                table: "WindowsOperationParameters",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationParameters_ParameterOrder",
                table: "WindowsOperationParameters",
                column: "ParameterOrder");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationPoints_CreatedAt",
                table: "WindowsOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationPoints_IsEnabled",
                table: "WindowsOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationPoints_OperationMode",
                table: "WindowsOperationPoints",
                column: "OperationMode");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationPoints_OperationNumber",
                table: "WindowsOperationPoints",
                column: "OperationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WindowsOperationPoints_OperationType",
                table: "WindowsOperationPoints",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionInstances_IsEnabled",
                table: "WindowsQuestionInstances",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionInstances_QuestionTemplateId",
                table: "WindowsQuestionInstances",
                column: "QuestionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionInstances_UsageCount",
                table: "WindowsQuestionInstances",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionTemplates_DifficultyLevel",
                table: "WindowsQuestionTemplates",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionTemplates_IsEnabled",
                table: "WindowsQuestionTemplates",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionTemplates_OperationPointId",
                table: "WindowsQuestionTemplates",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionTemplates_UsageCount",
                table: "WindowsQuestionTemplates",
                column: "UsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WindowsEnumValues");

            migrationBuilder.DropTable(
                name: "WindowsOperationParameters");

            migrationBuilder.DropTable(
                name: "WindowsQuestionInstances");

            migrationBuilder.DropTable(
                name: "WindowsEnumTypes");

            migrationBuilder.DropTable(
                name: "WindowsQuestionTemplates");

            migrationBuilder.DropTable(
                name: "WindowsOperationPoints");
        }
    }
}
