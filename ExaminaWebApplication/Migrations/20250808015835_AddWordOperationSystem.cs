using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddWordOperationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WindowsQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 10.0m),
                    Requirements = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsQuestions_ExamSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordEnumTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TypeName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordEnumTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordOperationPoints", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Requirements = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordQuestions_ExamSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "ExamSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WindowsQuestionOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 5.0m),
                    OperationConfig = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsQuestionOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WindowsQuestionOperationPoints_WindowsQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WindowsQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordEnumValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EnumTypeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordEnumValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordEnumValues_WordEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "WordEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordOperationParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationPointId = table.Column<int>(type: "int", nullable: false),
                    ParameterKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParameterName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DefaultValue = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnumTypeId = table.Column<int>(type: "int", nullable: true),
                    ParameterOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordOperationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordOperationParameters_WordEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "WordEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WordOperationParameters_WordOperationPoints_OperationPointId",
                        column: x => x.OperationPointId,
                        principalTable: "WordOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordQuestionTemplates",
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
                    DifficultyLevel = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordQuestionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordQuestionTemplates_WordOperationPoints_OperationPointId",
                        column: x => x.OperationPointId,
                        principalTable: "WordOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordQuestionOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OperationConfig = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordQuestionOperationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordQuestionOperationPoints_WordQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "WordQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WordQuestionInstances",
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
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordQuestionInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordQuestionInstances_WordQuestionTemplates_QuestionTemplate~",
                        column: x => x.QuestionTemplateId,
                        principalTable: "WordQuestionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_CreatedAt",
                table: "WindowsQuestionOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_IsEnabled",
                table: "WindowsQuestionOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_OperationType",
                table: "WindowsQuestionOperationPoints",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_OrderIndex",
                table: "WindowsQuestionOperationPoints",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestionOperationPoints_QuestionId",
                table: "WindowsQuestionOperationPoints",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestions_CreatedAt",
                table: "WindowsQuestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestions_IsEnabled",
                table: "WindowsQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WindowsQuestions_SubjectId",
                table: "WindowsQuestions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumTypes_Category",
                table: "WordEnumTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumTypes_CreatedAt",
                table: "WordEnumTypes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumTypes_IsEnabled",
                table: "WordEnumTypes",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumTypes_TypeName",
                table: "WordEnumTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumValues_CreatedAt",
                table: "WordEnumValues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumValues_EnumTypeId",
                table: "WordEnumValues",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumValues_IsDefault",
                table: "WordEnumValues",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumValues_IsEnabled",
                table: "WordEnumValues",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumValues_SortOrder",
                table: "WordEnumValues",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_WordEnumValues_Value",
                table: "WordEnumValues",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_CreatedAt",
                table: "WordOperationParameters",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_DataType",
                table: "WordOperationParameters",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_EnumTypeId",
                table: "WordOperationParameters",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_IsEnabled",
                table: "WordOperationParameters",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_IsRequired",
                table: "WordOperationParameters",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_OperationPointId",
                table: "WordOperationParameters",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationParameters_ParameterKey",
                table: "WordOperationParameters",
                column: "ParameterKey");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationPoints_Category",
                table: "WordOperationPoints",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationPoints_CreatedAt",
                table: "WordOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationPoints_IsEnabled",
                table: "WordOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordOperationPoints_OperationNumber",
                table: "WordOperationPoints",
                column: "OperationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionInstances_CreatedAt",
                table: "WordQuestionInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionInstances_IsEnabled",
                table: "WordQuestionInstances",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionInstances_QuestionTemplateId",
                table: "WordQuestionInstances",
                column: "QuestionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionInstances_UsageCount",
                table: "WordQuestionInstances",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_CreatedAt",
                table: "WordQuestionOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_IsEnabled",
                table: "WordQuestionOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_OperationType",
                table: "WordQuestionOperationPoints",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_OrderIndex",
                table: "WordQuestionOperationPoints",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionOperationPoints_QuestionId",
                table: "WordQuestionOperationPoints",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestions_CreatedAt",
                table: "WordQuestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestions_IsEnabled",
                table: "WordQuestions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestions_SubjectId",
                table: "WordQuestions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionTemplates_CreatedAt",
                table: "WordQuestionTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionTemplates_DifficultyLevel",
                table: "WordQuestionTemplates",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionTemplates_IsEnabled",
                table: "WordQuestionTemplates",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionTemplates_OperationPointId",
                table: "WordQuestionTemplates",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_WordQuestionTemplates_UsageCount",
                table: "WordQuestionTemplates",
                column: "UsageCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WindowsQuestionOperationPoints");

            migrationBuilder.DropTable(
                name: "WordEnumValues");

            migrationBuilder.DropTable(
                name: "WordOperationParameters");

            migrationBuilder.DropTable(
                name: "WordQuestionInstances");

            migrationBuilder.DropTable(
                name: "WordQuestionOperationPoints");

            migrationBuilder.DropTable(
                name: "WindowsQuestions");

            migrationBuilder.DropTable(
                name: "WordEnumTypes");

            migrationBuilder.DropTable(
                name: "WordQuestionTemplates");

            migrationBuilder.DropTable(
                name: "WordQuestions");

            migrationBuilder.DropTable(
                name: "WordOperationPoints");
        }
    }
}
