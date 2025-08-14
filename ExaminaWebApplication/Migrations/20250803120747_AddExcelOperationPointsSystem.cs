using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExaminaWebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddExcelOperationPointsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExcelEnumTypes",
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
                    table.PrimaryKey("PK_ExcelEnumTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcelOperationPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OperationNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TargetType = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelOperationPoints", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcelEnumValues",
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
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ExtendedProperties = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelEnumValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcelEnumValues_ExcelEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "ExcelEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcelOperationParameters",
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
                    table.PrimaryKey("PK_ExcelOperationParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcelOperationParameters_ExcelEnumTypes_EnumTypeId",
                        column: x => x.EnumTypeId,
                        principalTable: "ExcelEnumTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExcelOperationParameters_ExcelOperationPoints_OperationPoint~",
                        column: x => x.OperationPointId,
                        principalTable: "ExcelOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcelQuestionTemplates",
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
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelQuestionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcelQuestionTemplates_ExcelOperationPoints_OperationPointId",
                        column: x => x.OperationPointId,
                        principalTable: "ExcelOperationPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExcelQuestionTemplates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExcelQuestionInstances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    QuestionTitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionDescription = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualParameters = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnswerValidationRules = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelQuestionInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcelQuestionInstances_ExcelQuestionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ExcelQuestionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExcelQuestionInstances_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ExcelEnumTypes",
                columns: ["Id", "Category", "CreatedAt", "Description", "IsEnabled", "TypeName", "UpdatedAt"],
                values: new object[,]
                {
                    { 1, "对齐方式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "水平对齐方式", true, "HorizontalAlignment", null },
                    { 2, "对齐方式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "垂直对齐方式", true, "VerticalAlignment", null },
                    { 3, "边框样式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "边框线样式", true, "BorderStyle", null },
                    { 4, "字体样式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "字体样式", true, "FontStyle", null },
                    { 5, "字体样式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "下划线样式", true, "UnderlineStyle", null },
                    { 6, "数字格式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "数字分类格式", true, "NumberFormat", null },
                    { 7, "填充样式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图案填充样式", true, "PatternStyle", null },
                    { 8, "图表", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图表类型", true, "ChartType", null },
                    { 9, "图表", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图例位置", true, "LegendPosition", null },
                    { 10, "图表", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "数据标签位置", true, "DataLabelPosition", null },
                    { 11, "填充样式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "填充类型", true, "FillType", null },
                    { 12, "样式", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "单元格样式", true, "CellStyle", null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationPoints",
                columns: ["Id", "Category", "CreatedAt", "Description", "IsEnabled", "Name", "OperationNumber", "OperationType", "TargetType", "UpdatedAt"],
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "通过判断单元格内的值，来鉴定是否完成操作", true, "填充或复制单元格内容", 1, "A", 1, null },
                    { 2, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "合并指定范围的单元格", true, "合并单元格", 4, "A", 1, null },
                    { 3, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的字体", true, "设置指定单元格字体", 6, "A", 1, null },
                    { 4, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的内边框样式", true, "内边框样式", 10, "A", 1, null },
                    { 5, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的内边框颜色", true, "内边框颜色", 11, "A", 1, null },
                    { 6, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的水平对齐方式", true, "设置单元格区域水平对齐方式", 13, "A", 1, null },
                    { 7, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的数字格式", true, "设置目标区域单元格数字分类格式", 14, "A", 1, null },
                    { 8, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "在单元格中使用Excel函数", true, "使用函数", 15, "A", 1, null },
                    { 9, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置指定行的行高", true, "设置行高", 16, "A", 1, null },
                    { 10, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置指定列的列宽", true, "设置列宽", 17, "A", 1, null },
                    { 11, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的填充颜色", true, "设置单元格填充颜色", 20, "A", 1, null },
                    { 12, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的外边框样式", true, "设置外边框样式", 24, "A", 1, null },
                    { 13, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的外边框颜色", true, "设置外边框颜色", 25, "A", 1, null },
                    { 14, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的垂直对齐方式", true, "设置垂直对齐方式", 26, "A", 1, null },
                    { 15, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "修改工作表的名称", true, "修改sheet表名称", 28, "A", 3, null },
                    { 16, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "为单元格区域添加下划线", true, "添加下划线", 29, "A", 1, null },
                    { 17, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的字体样式", true, "设置字型", 7, "A", 1, null },
                    { 18, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的字体大小", true, "设置字号", 8, "A", 1, null },
                    { 19, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的字体颜色", true, "字体颜色", 9, "A", 1, null },
                    { 20, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的图案填充样式", true, "设置图案填充样式", 21, "A", 1, null },
                    { 21, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的填充图案颜色", true, "设置填充图案颜色", 22, "A", 1, null },
                    { 22, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的条件格式", true, "条件格式", 33, "A", 1, null },
                    { 23, 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置单元格区域的预定义样式", true, "设置单元格样式——数据", 83, "A", 1, null },
                    { 24, 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "对数据清单进行筛选操作", true, "筛选", 31, "A", 1, null },
                    { 25, 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "对数据清单进行排序操作", true, "排序", 32, "A", 1, null },
                    { 26, 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "对数据清单进行分类汇总操作", true, "分类汇总", 35, "A", 1, null },
                    { 27, 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "使用条件区域进行高级筛选", true, "高级筛选-条件", 36, "A", 1, null },
                    { 28, 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "高级筛选的数据处理", true, "高级筛选-数据", 63, "A", 1, null },
                    { 29, 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "创建和配置数据透视表", true, "数据透视表", 71, "A", 1, null },
                    { 30, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表的类型", true, "图表类型", 101, "B", 2, null },
                    { 31, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表的样式", true, "图表样式", 102, "B", 2, null },
                    { 32, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "移动图表到指定位置", true, "图表移动", 103, "B", 2, null },
                    { 33, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表横轴（分类轴）上每个刻度对应的标签", true, "分类轴数据区域", 104, "B", 2, null },
                    { 34, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表纵轴的数据区域", true, "数值轴数据区域", 105, "B", 2, null },
                    { 35, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表标题", true, "图表标题", 107, "B", 2, null },
                    { 36, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表标题的格式", true, "图表标题格式", 108, "B", 2, null },
                    { 37, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置主要横坐标轴标题", true, "主要横坐标轴标题", 112, "B", 2, null },
                    { 38, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置主要横坐标轴标题的格式", true, "主要横坐标轴标题格式", 113, "B", 2, null },
                    { 39, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图例的位置", true, "设置图例位置", 122, "B", 2, null },
                    { 40, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图例的格式", true, "设置图例格式", 123, "B", 2, null },
                    { 41, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置主要纵坐标轴的各种选项", true, "设置主要纵坐标轴选项", 139, "B", 2, null },
                    { 42, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置主要横网格线", true, "设置网格线——主要横网格线", 140, "B", 2, null },
                    { 43, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置次要横网格线", true, "设置网格线——次要横网格线", 141, "B", 2, null },
                    { 44, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置主要纵网格线", true, "主要纵网格线", 142, "B", 2, null },
                    { 45, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置次要纵网格线", true, "次要纵网格线", 143, "B", 2, null },
                    { 46, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置数据系列的格式", true, "设置数据系列格式", 145, "B", 2, null },
                    { 47, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "为图表添加数据标签", true, "添加数据标签", 154, "B", 2, null },
                    { 48, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置数据标签的格式", true, "设置数据标签格式", 155, "B", 2, null },
                    { 49, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表区域的格式", true, "设置图表区域格式", 156, "B", 2, null },
                    { 50, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表基底的颜色", true, "显示图表基底颜色", 159, "B", 2, null },
                    { 51, 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "设置图表的边框线", true, "设置图表边框线", 160, "B", 2, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "数值右对齐，文本左对齐", "默认", "xlGeneral", 1, 1, null, true, 1, null },
                    { 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "左对齐", "xlLeft", 1, -4131, null, true, 2, null },
                    { 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "居中对齐", "xlCenter", 1, -4108, null, true, 3, null },
                    { 4, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "右对齐", "xlRight", 1, -4152, null, true, 4, null },
                    { 5, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "内容重复填满单元格", "填充", "xlFill", 1, 5, null, true, 5, null },
                    { 6, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "多行内容适用", "两端对齐", "xlJustify", 1, -4130, null, true, 6, null },
                    { 7, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "非合并单元格", "跨列居中", "xlCenterAcrossSelection", 1, 7, null, true, 7, null },
                    { 8, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "需启用自动换行", "分散对齐", "xlDistributed", 1, -4117, null, true, 8, null },
                    { 9, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "内容贴近单元格上边缘", "顶端对齐", "xlTop", 2, -4160, null, true, 1, null },
                    { 10, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "内容居中于上下边缘之间", "垂直居中对齐", "xlCenter", 2, -4108, null, true, 2, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [11, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "内容贴近单元格下边缘（默认）", "底端对齐", "xlBottom", 2, -4107, null, true, true, 3, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 12, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "内容在上下方向也平均分布", "两端对齐", "xlJustify", 2, -4130, null, true, 4, null },
                    { 13, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "类似两端对齐但分布更均匀", "分散对齐", "xlDistributed", 2, -4117, null, true, 5, null },
                    { 14, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "无边框", "xlLineStyleNone", 3, -4142, null, true, 1, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [15, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "实线", "xlContinuous", 3, 1, null, true, true, 2, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 16, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "虚线", "xlDash", 3, -4115, null, true, 3, null },
                    { 17, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "点线", "xlDot", 3, -4118, null, true, 4, null },
                    { 18, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "点划线", "xlDashDot", 3, 4, null, true, 5, null },
                    { 19, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "双点划线", "xlDashDotDot", 3, 5, null, true, 6, null },
                    { 20, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "双线", "xlDouble", 3, -4119, null, true, 7, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [21, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "默认字体样式", "常规", "Regular", 4, null, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 22, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "加粗显示", "粗体", "Bold", 4, null, null, true, 2, null },
                    { 23, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "右倾斜显示", "斜体", "Italic", 4, null, null, true, 3, null },
                    { 24, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "加粗 + 斜体", "粗斜体", "BoldItalic", 4, null, null, true, 4, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [25, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "默认，不添加任何下划线", "无下划线", "xlUnderlineStyleNone", 5, null, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 26, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "添加一条常规下划线", "单下划线", "xlUnderlineStyleSingle", 5, null, null, true, 2, null },
                    { 27, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "添加两条平行下划线", "双下划线", "xlUnderlineStyleDouble", 5, null, null, true, 3, null },
                    { 28, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "用于财务，位置稍低", "会计用单下划线", "xlUnderlineStyleSingleAccounting", 5, null, null, true, 4, null },
                    { 29, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "双下划线，财务专用风格", "会计用双下划线", "xlUnderlineStyleDoubleAccounting", 5, null, null, true, 5, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [30, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "自动识别显示（默认格式）", "常规", "General", 6, 0, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 31, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1234.56（可设置小数位数/千位分隔）", "数值", "Number", 6, 1, null, true, 2, null },
                    { 32, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "¥1,234.56", "货币", "Currency", 6, 2, null, true, 3, null },
                    { 33, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "¥ 1,234.56", "会计专用", "Accounting", 6, 4, null, true, 4, null },
                    { 34, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2025/07/31", "日期（短）", "Date", 6, 14, null, true, 5, null },
                    { 35, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "13:30:55", "时间", "Time", 6, 20, null, true, 6, null },
                    { 36, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "85%", "百分比", "Percentage", 6, 9, null, true, 7, null },
                    { 37, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1 1/4", "分数", "Fraction", 6, 5, null, true, 8, null },
                    { 38, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "1.23E+03", "科学计数法", "Scientific", 6, 11, null, true, 9, null },
                    { 39, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "保留输入内容格式", "文本", "Text", 6, 49, null, true, 10, null },
                    { 40, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "如邮政编码", "特殊格式", "Special", 6, 12, null, true, 11, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [41, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "不使用图案", "无填充", "xlPatternNone", 7, null, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 42, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "纯色填充", "实心填充", "xlSolid", 7, null, null, true, 2, null },
                    { 43, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "小点分布", "细点状", "xlGray8", 7, null, null, true, 3, null },
                    { 44, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "点稍密一些", "中等点状", "xlGray6", 7, null, null, true, 4, null },
                    { 45, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "点更密", "密集点状", "xlGray5", 7, null, null, true, 5, null },
                    { 46, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "从左下到右上的斜线", "斜线（右上）", "xlUp", 7, null, null, true, 6, null },
                    { 47, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "从右下到左上的斜线", "斜线（左上）", "xlDown", 7, null, null, true, 7, null },
                    { 48, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "横竖线交叉", "十字交叉线", "xlCrissCross", 7, null, null, true, 8, null },
                    { 49, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "网格状线条", "网格线", "xlGrid", 7, null, null, true, 9, null },
                    { 50, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "横向线条", "水平线", "xlHorizontal", 7, null, null, true, 10, null },
                    { 51, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "纵向线条", "垂直线", "xlVertical", 7, null, null, true, 11, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [52, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "簇状柱形图", "xlColumnClustered", 8, 51, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 53, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "堆积柱形图", "xlColumnStacked", 8, 52, null, true, 2, null },
                    { 54, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "百分比堆积柱形图", "xlColumnStacked100", 8, 53, null, true, 3, null },
                    { 55, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "簇状条形图", "xlBarClustered", 8, 57, null, true, 4, null },
                    { 56, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "堆积条形图", "xlBarStacked", 8, 58, null, true, 5, null },
                    { 57, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "百分比堆积条形图", "xlBarStacked100", 8, 59, null, true, 6, null },
                    { 58, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "折线图", "xlLine", 8, 4, null, true, 7, null },
                    { 59, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "带数据标记的折线图", "xlLineMarkers", 8, 65, null, true, 8, null },
                    { 60, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "饼图", "xlPie", 8, 5, null, true, 9, null },
                    { 61, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "分离型饼图", "xlPieExploded", 8, 69, null, true, 10, null },
                    { 62, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "圆环图", "xlDoughnut", 8, -4120, null, true, 11, null },
                    { 63, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "面积图", "xlArea", 8, 1, null, true, 12, null },
                    { 64, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "散点图", "xlXYScatter", 8, -4169, null, true, 13, null },
                    { 65, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "气泡图", "xlBubble", 8, 15, null, true, 14, null },
                    { 66, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "不显示图例", "无图例", "None", 9, null, null, true, 1, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [67, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图例显示在图表右侧（默认）", "图表右侧", "Right", 9, null, null, true, true, 2, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 68, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图例显示在图表上方", "图表顶部", "Top", 9, null, null, true, 3, null },
                    { 69, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图例显示在图表下方", "图表底部", "Bottom", 9, null, null, true, 4, null },
                    { 70, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "图例显示在图表左侧", "图表左侧", "Left", 9, null, null, true, 5, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [71, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "内部末端", "xlLabelPositionInsideEnd", 10, null, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 72, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "外部末端", "xlLabelPositionOutsideEnd", 10, null, null, true, 2, null },
                    { 73, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "居中", "xlLabelPositionCenter", 10, null, null, true, 3, null },
                    { 74, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "上方", "xlLabelPositionAbove", 10, null, null, true, 4, null },
                    { 75, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "下方", "xlLabelPositionBelow", 10, null, null, true, 5, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [76, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "单一颜色填充", "实心填充", "msoFillSolid", 11, null, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 77, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "多种颜色渐变过渡填充", "渐变填充", "msoFillGradient", 11, null, null, true, 2, null },
                    { 78, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "可选图案（点线格等）填充", "图案填充", "msoFillPatterned", 11, null, null, true, 3, null },
                    { 79, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "使用内置图片纹理填充", "纹理填充", "msoFillTextured", 11, null, null, true, 4, null },
                    { 80, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "使用外部图片作为背景", "图片填充", "msoFillPicture", 11, null, null, true, 5, null },
                    { 81, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "不填充（完全透明）", "无填充", "msoFillNone", 11, null, null, true, 6, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsDefault", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: [82, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "默认样式，常规字体，无格式", "常规", "Normal", 12, null, null, true, true, 1, null]);

            migrationBuilder.InsertData(
                table: "ExcelEnumValues",
                columns: ["Id", "CreatedAt", "Description", "DisplayName", "EnumKey", "EnumTypeId", "EnumValue", "ExtendedProperties", "IsEnabled", "SortOrder", "UpdatedAt"],
                values: new object[,]
                {
                    { 83, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "红色填充，用于表示错误值", "错误", "Bad", 12, null, null, true, 2, null },
                    { 84, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "绿色填充，用于表示通过、合格等", "正确", "Good", 12, null, null, true, 3, null },
                    { 85, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "灰色填充，用于中性状态", "中性", "Neutral", 12, null, null, true, 4, null },
                    { 86, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "强调计算字段", "计算", "Calculation", 12, null, null, true, 5, null },
                    { 87, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黄色填充，用于提示检查的单元格", "检查单元格", "CheckCell", 12, null, null, true, 6, null },
                    { 88, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "灰色斜体小字样式", "说明文字", "ExplanatoryText", 12, null, null, true, 7, null },
                    { 89, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "浅黄色填充，表示用户可编辑单元格", "输入", "Input", 12, null, null, true, 8, null },
                    { 90, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "蓝色字体，用于表示与外部链接的单元格", "关联单元格", "LinkedCell", 12, null, null, true, 9, null },
                    { 91, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "灰色背景，表示说明、注释内容", "注释", "Note", 12, null, null, true, 10, null },
                    { 92, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "绿色背景，表示输出结果或最终值", "输出", "Output", 12, null, null, true, 11, null },
                    { 93, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黑体 14 号，大标题", "标题1", "Heading1", 12, null, null, true, 12, null },
                    { 94, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黑体 12 号，副标题", "标题2", "Heading2", 12, null, null, true, 13, null },
                    { 95, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黑体 11 号，小标题", "标题3", "Heading3", 12, null, null, true, 14, null },
                    { 96, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黑体 10 号，最小标题", "标题4", "Heading4", 12, null, null, true, 15, null },
                    { 97, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "默认大号标题，蓝色背景，白色字体", "标题", "Title", 12, null, null, true, 16, null },
                    { 98, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "加粗底纹表示汇总单元格", "总计", "Total", 12, null, null, true, 17, null },
                    { 99, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "数字样式，带千分位，不显示小数", "千分位格式", "Comma", 12, null, null, true, 18, null },
                    { 100, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "数字加货币符号，如￥、$等", "货币", "Currency", 12, null, null, true, 19, null },
                    { 101, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "格式为百分比", "百分比", "Percent", 12, null, null, true, 20, null },
                    { 102, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "红色粗体，通常用于高亮警告信息", "警告文字", "WarningText", 12, null, null, true, 21, null },
                    { 103, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "蓝色填充，白色字体", "强调1", "Emphasis1", 12, null, null, true, 22, null },
                    { 104, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "灰色填充，黑色字体", "强调2", "Emphasis2", 12, null, null, true, 23, null },
                    { 105, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "浅紫色填充，白色字体", "强调3", "Emphasis3", 12, null, null, true, 24, null },
                    { 106, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "蓝色字体，带下划线", "超链接", "Hyperlink", 12, null, null, true, 25, null },
                    { 107, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "紫色字体，带下划线", "已访问的超链接", "FollowedHyperlink", 12, null, null, true, 26, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 1, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "E10", true, true, 1, "要填充内容的单元格位置", "目标单元格", 1, null, null },
                    { 2, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "我的天啊", true, true, 1, "要填充的具体内容", "填充内容", 2, null, null },
                    { 3, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1", true, true, 2, "合并区域的起始单元格", "起始单元格", 1, null, null },
                    { 4, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "C3", true, true, 2, "合并区域的结束单元格", "结束单元格", 2, null, null },
                    { 5, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 3, "要设置字体的单元格区域", "单元格区域", 1, null, null },
                    { 6, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "宋体", true, true, 3, "字体名称", "字体名称", 2, null, null },
                    { 7, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 4, "要设置内边框的单元格区域", "单元格区域", 1, null, null },
                    { 8, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 3, "xlContinuous", true, true, 4, "内边框线样式", "边框样式", 2, null, null },
                    { 9, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 5, "要设置内边框颜色的单元格区域", "单元格区域", 1, null, null },
                    { 10, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "16711680", true, true, 5, "内边框颜色RGB值", "边框颜色", 2, null, null },
                    { 11, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 6, "要设置水平对齐的单元格区域", "单元格区域", 1, null, null },
                    { 12, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 1, "xlCenter", true, true, 6, "水平对齐方式", "水平对齐方式", 2, null, null },
                    { 13, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 7, "要设置数字格式的单元格区域", "单元格区域", 1, null, null },
                    { 14, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 6, "Currency", true, true, 7, "数字分类格式", "数字格式", 2, null, null },
                    { 15, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "D1", true, true, 8, "要输入函数的单元格", "目标单元格", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [16, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "100", true, 8, "函数计算的期望结果", "期望值", 2, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [17, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, null, null, "=SUM(A1:A10)", true, true, 8, "Excel函数公式", "公式内容", 3, null, "{\"allowedFunctions\":[\"VLOOKUP\",\"IF\",\"SUMIF\",\"ROUND\",\"TEXT\",\"AVERAGE\",\"COUNTIF\"]}"]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "AllowMultipleValues", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [18, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "1,3,5", true, true, 9, "要设置行高的行号（可多个，用逗号分隔）", "行号", 1, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [19, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null, "20.5", true, true, 9, "行高数值（磅）", "行高值", 2, null, "{\"minValue\":0,\"maxValue\":409.5}"]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "AllowMultipleValues", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [20, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "A,C,E", true, true, 10, "要设置列宽的列号（可多个，用逗号分隔）", "列号", 1, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 21, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, null, "15.5", true, true, 10, "列宽数值", "列宽值", 2, null, "{\"minValue\":0,\"maxValue\":255}" },
                    { 22, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 11, "要设置填充颜色的单元格区域", "单元格区域", 1, null, null },
                    { 23, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "16777215", true, true, 11, "填充颜色RGB值", "填充颜色", 2, null, null },
                    { 24, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 12, "要设置外边框的单元格区域", "单元格区域", 1, null, null },
                    { 25, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 3, "xlContinuous", true, true, 12, "外边框线样式", "外边框样式", 2, null, null },
                    { 26, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 13, "要设置外边框颜色的单元格区域", "单元格区域", 1, null, null },
                    { 27, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "16711680", true, true, 13, "外边框颜色RGB值", "外边框颜色", 2, null, null },
                    { 28, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 14, "要设置垂直对齐的单元格区域", "单元格区域", 1, null, null },
                    { 29, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 2, "xlCenter", true, true, 14, "垂直对齐方式", "垂直对齐方式", 2, null, null },
                    { 30, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "Sheet1", true, true, 15, "要修改的工作表原名称", "原表名", 1, null, null },
                    { 31, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "数据统计表", true, true, 15, "修改后的工作表名称", "新表名", 2, null, "{\"maxLength\":31}" },
                    { 32, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 16, "要添加下划线的单元格区域", "单元格区域", 1, null, null },
                    { 33, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 5, "xlUnderlineStyleSingle", true, true, 16, "下划线样式类型", "下划线类型", 2, null, null },
                    { 34, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 17, "要设置字型的单元格区域", "单元格区域", 1, null, null },
                    { 35, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 4, "Bold", true, true, 17, "字型（字体样式）", "字体样式", 2, null, null },
                    { 36, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 18, "要设置字号的单元格区域", "单元格区域", 1, null, null },
                    { 37, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "12", true, true, 18, "字体大小（磅）", "字号大小", 2, null, "{\"minValue\":1,\"maxValue\":409}" },
                    { 38, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 19, "要设置字体颜色的单元格区域", "单元格区域", 1, null, null },
                    { 39, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "16711680", true, true, 19, "字体颜色RGB值", "字体颜色", 2, null, null },
                    { 40, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 20, "要设置图案填充的单元格区域", "单元格区域", 1, null, null },
                    { 41, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 7, "xlGray8", true, true, 20, "图案填充样式", "图案样式", 2, null, null },
                    { 42, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 21, "要设置填充图案颜色的单元格区域", "单元格区域", 1, null, null },
                    { 43, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "16777215", true, true, 21, "填充图案颜色RGB值", "图案颜色", 2, null, null },
                    { 44, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "D5:D27", true, true, 22, "条件格式应用的单元格范围", "应用区域", 1, null, null },
                    { 45, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 22, "条件格式类型", "条件类型", 2, null, "{\"minValue\":1,\"maxValue\":6}" },
                    { 46, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, ">", true, true, 22, "条件判断操作符", "判断方式", 3, null, null },
                    { 47, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "80", true, true, 22, "条件比较值", "条件值", 4, null, null },
                    { 48, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "FontColor", true, true, 22, "应用的格式类型", "格式类型", 5, null, null },
                    { 49, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "16711680", true, true, 22, "格式的具体值", "格式值", 6, null, null },
                    { 50, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:C3", true, true, 23, "要设置样式的单元格区域", "单元格区域", 1, null, null },
                    { 51, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 12, "Good", true, true, 23, "预定义的单元格样式", "单元格样式", 2, null, null },
                    { 52, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, null, null, "{\"2\":\"=工商管理xlOr=计算机\",\"5\":\"<90xlAnd>70\",\"6\":\"<90xlAnd>70\"}", true, true, 24, "筛选条件配置（键值对方式）", "筛选条件", 1, null, "{\"description\":\"列索引:条件值，支持xlOr和xlAnd逻辑操作\"}" }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "AllowMultipleValues", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 53, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "A,B,C", true, true, 25, "要排序的列（可多个，用逗号分隔）", "排序列", 1, null, null },
                    { 54, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "ASC,DESC,ASC", true, true, 25, "排序方式（升序/降序，对应排序列）", "排序方式", 2, null, "{\"allowedValues\":[\"ASC\",\"DESC\"]}" },
                    { 55, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "E12;E34;F22;F34;G12;G33", true, true, 26, "分类汇总结果的位置（多个备选位置）", "汇总位置", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 56, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "SUBTOTAL(4,*)", true, true, 26, "SUBTOTAL函数配置", "汇总函数", 2, null, "{\"description\":\"识别函数编号为4的SUBTOTAL函数\"}" },
                    { 57, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "J5:L7", true, true, 27, "筛选的条件区域", "条件区域", 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [58, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "J10", true, 27, "筛选后复制结果的起始单元格区域", "复制目标区域", 2, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "AllowMultipleValues", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [59, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "Subject,Class,Chinese", true, true, 27, "参与筛选的字段（列标题名称）", "筛选字段", 3, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 60, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "计算机,1班,>80;工商管理,2班,>80", true, true, 27, "多组条件，分号分隔表示或关系", "筛选条件", 4, null, null },
                    { 61, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "True", null, null, true, true, 27, "是否启用高级筛选", "启用高级筛选", 5, null, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [62, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "False", null, null, true, 27, "是否只显示唯一值", "仅显示唯一值", 6, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [63, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1:F100", true, true, 28, "高级筛选的数据源区域", "数据源区域", 1, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "AllowMultipleValues", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [64, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "学历,职务", true, true, 29, "设置为透视表的行字段", "行字段", 1, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "AllowMultipleValues", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [65, true, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, null, true, 29, "设置为透视表的列字段（可空）", "列字段", 2, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 66, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "年龄", true, true, 29, "用于聚合的字段名称", "数据字段", 3, null, null },
                    { 67, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "Average", true, true, 29, "聚合函数类型", "聚合函数", 4, null, "{\"allowedValues\":[\"Sum\",\"Average\",\"Count\",\"Max\",\"Min\"]}" },
                    { 68, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "$C$28", true, true, 29, "插入透视表的起始单元格位置", "插入位置", 5, null, null }
                });

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: [69, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "学历职务年龄统计表", true, 29, "透视表名称（可选）", "透视表名称", 6, null, null]);

            migrationBuilder.InsertData(
                table: "ExcelOperationParameters",
                columns: ["Id", "CreatedAt", "DataType", "DefaultValue", "EnumTypeId", "ExampleValue", "IsEnabled", "IsRequired", "OperationPointId", "ParameterDescription", "ParameterName", "ParameterOrder", "UpdatedAt", "ValidationRules"],
                values: new object[,]
                {
                    { 70, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 30, "目标图表的编号", "图表编号", 1, null, null },
                    { 71, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 8, "xlColumnClustered", true, true, 30, "图表类型枚举值", "图表类型", 2, null, null },
                    { 72, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 31, "目标图表的编号", "图表编号", 1, null, null },
                    { 73, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "5", true, true, 31, "图表样式编号（1-48）", "样式编号", 2, null, "{\"minValue\":1,\"maxValue\":48}" },
                    { 74, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 32, "要移动的图表编号", "图表编号", 1, null, null },
                    { 75, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A1", true, true, 32, "图表移动的起始位置", "起始单元格", 2, null, null },
                    { 76, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "F10", true, true, 32, "图表移动的结束位置", "结束单元格", 3, null, null },
                    { 77, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 33, "目标图表的编号", "图表编号", 1, null, null },
                    { 78, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "Sheet1", true, true, 33, "数据源工作簿名称", "目标工作簿", 2, null, null },
                    { 79, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A2", true, true, 33, "分类轴数据的起始单元格", "起始单元格", 3, null, null },
                    { 80, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "A10", true, true, 33, "分类轴数据的终止单元格", "终止单元格", 4, null, null },
                    { 81, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 34, "目标图表的编号", "图表编号", 1, null, null },
                    { 82, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "Sheet1", true, true, 34, "数据源工作簿名称", "目标工作簿", 2, null, null },
                    { 83, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "B2", true, true, 34, "数值轴数据的起始单元格", "起始单元格", 3, null, null },
                    { 84, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, null, "B10", true, true, 34, "数值轴数据的终止单元格", "终止单元格", 4, null, null },
                    { 85, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 35, "目标图表的编号", "图表编号", 1, null, null },
                    { 86, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "销售数据统计图", true, true, 35, "图表标题文本", "图表标题", 2, null, null },
                    { 87, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 36, "目标图表的编号", "图表编号", 1, null, null },
                    { 88, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "宋体", true, true, 36, "图表标题字体", "标题字体", 2, null, null },
                    { 89, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 4, "Bold", true, true, 36, "图表标题字体样式", "字体样式", 3, null, null },
                    { 90, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "14", true, true, 36, "图表标题字号", "字号", 4, null, "{\"minValue\":1,\"maxValue\":409}" },
                    { 91, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "16711680", true, true, 36, "图表标题颜色值", "字体颜色", 5, null, null },
                    { 92, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 37, "目标图表的编号", "图表编号", 1, null, null },
                    { 93, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "月份", true, true, 37, "横坐标轴标题文本", "横坐标轴标题", 2, null, null },
                    { 94, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 38, "目标图表的编号", "图表编号", 1, null, null },
                    { 95, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "宋体", true, true, 38, "横坐标轴标题字体", "轴标题字体", 2, null, null },
                    { 96, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 4, "Regular", true, true, 38, "横坐标轴标题字体样式", "字体样式", 3, null, null },
                    { 97, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "12", true, true, 38, "横坐标轴标题字号", "字号", 4, null, "{\"minValue\":1,\"maxValue\":409}" },
                    { 98, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "0", true, true, 38, "横坐标轴标题颜色值", "字体颜色", 5, null, null },
                    { 99, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 39, "目标图表的编号", "图表编号", 1, null, null },
                    { 100, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 9, "Right", true, true, 39, "图例显示位置", "图例位置", 2, null, null },
                    { 101, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "1", true, true, 40, "目标图表的编号", "图表编号", 1, null, null },
                    { 102, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null, "宋体", true, true, 40, "图例字体", "图例字体", 2, null, null },
                    { 103, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, null, 4, "Regular", true, true, 40, "图例字体样式", "字体样式", 3, null, null },
                    { 104, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, null, "10", true, true, 40, "图例字号", "字号", 4, null, "{\"minValue\":1,\"maxValue\":409}" },
                    { 105, new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, null, null, "0", true, true, 40, "图例颜色值", "字体颜色", 5, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumTypes_Category",
                table: "ExcelEnumTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumTypes_IsEnabled",
                table: "ExcelEnumTypes",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumTypes_TypeName",
                table: "ExcelEnumTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumValues_EnumTypeId",
                table: "ExcelEnumValues",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumValues_EnumTypeId_EnumKey",
                table: "ExcelEnumValues",
                columns: ["EnumTypeId", "EnumKey"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumValues_IsDefault",
                table: "ExcelEnumValues",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumValues_IsEnabled",
                table: "ExcelEnumValues",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelEnumValues_SortOrder",
                table: "ExcelEnumValues",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameters_DataType",
                table: "ExcelOperationParameters",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameters_EnumTypeId",
                table: "ExcelOperationParameters",
                column: "EnumTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameters_IsEnabled",
                table: "ExcelOperationParameters",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameters_OperationPointId",
                table: "ExcelOperationParameters",
                column: "OperationPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationParameters_OperationPointId_ParameterOrder",
                table: "ExcelOperationParameters",
                columns: ["OperationPointId", "ParameterOrder"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationPoints_Category",
                table: "ExcelOperationPoints",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationPoints_CreatedAt",
                table: "ExcelOperationPoints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationPoints_IsEnabled",
                table: "ExcelOperationPoints",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationPoints_OperationNumber",
                table: "ExcelOperationPoints",
                column: "OperationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationPoints_OperationType",
                table: "ExcelOperationPoints",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelOperationPoints_TargetType",
                table: "ExcelOperationPoints",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionInstances_CreatedAt",
                table: "ExcelQuestionInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionInstances_CreatedBy",
                table: "ExcelQuestionInstances",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionInstances_Status",
                table: "ExcelQuestionInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionInstances_TemplateId",
                table: "ExcelQuestionInstances",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionTemplates_CreatedAt",
                table: "ExcelQuestionTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionTemplates_CreatedBy",
                table: "ExcelQuestionTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionTemplates_DifficultyLevel",
                table: "ExcelQuestionTemplates",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionTemplates_IsEnabled",
                table: "ExcelQuestionTemplates",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelQuestionTemplates_OperationPointId",
                table: "ExcelQuestionTemplates",
                column: "OperationPointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcelEnumValues");

            migrationBuilder.DropTable(
                name: "ExcelOperationParameters");

            migrationBuilder.DropTable(
                name: "ExcelQuestionInstances");

            migrationBuilder.DropTable(
                name: "ExcelEnumTypes");

            migrationBuilder.DropTable(
                name: "ExcelQuestionTemplates");

            migrationBuilder.DropTable(
                name: "ExcelOperationPoints");
        }
    }
}
