using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Data.Excel;

/// <summary>
/// Excel基础操作点数据定义类
/// </summary>
public static class ExcelBasicOperationData
{
    /// <summary>
    /// 获取所有基础操作点定义
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationPoint> GetBasicOperationPoints()
    {
        return new List<ExcelOperationPoint>
        {
            // 操作点1：填充或复制单元格内容
            new() {
                Id = 1,
                OperationNumber = 1,
                Name = "填充或复制单元格内容",
                Description = "通过判断单元格内的值，来鉴定是否完成操作",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点4：合并单元格
            new() {
                Id = 2,
                OperationNumber = 4,
                Name = "合并单元格",
                Description = "合并指定范围的单元格",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点6：设置指定单元格字体
            new() {
                Id = 3,
                OperationNumber = 6,
                Name = "设置指定单元格字体",
                Description = "设置单元格区域的字体",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点10：内边框样式
            new() {
                Id = 4,
                OperationNumber = 10,
                Name = "内边框样式",
                Description = "设置单元格区域的内边框样式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点11：内边框颜色
            new() {
                Id = 5,
                OperationNumber = 11,
                Name = "内边框颜色",
                Description = "设置单元格区域的内边框颜色",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点13：设置单元格区域水平对齐方式
            new() {
                Id = 6,
                OperationNumber = 13,
                Name = "设置单元格区域水平对齐方式",
                Description = "设置单元格区域的水平对齐方式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点14：设置目标区域单元格数字分类格式
            new() {
                Id = 7,
                OperationNumber = 14,
                Name = "设置目标区域单元格数字分类格式",
                Description = "设置单元格区域的数字格式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点15：使用函数
            new() {
                Id = 8,
                OperationNumber = 15,
                Name = "使用函数",
                Description = "在单元格中使用Excel函数",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点16：设置行高
            new() {
                Id = 9,
                OperationNumber = 16,
                Name = "设置行高",
                Description = "设置指定行的行高",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点17：设置列宽
            new() {
                Id = 10,
                OperationNumber = 17,
                Name = "设置列宽",
                Description = "设置指定列的列宽",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点20：设置单元格填充颜色
            new() {
                Id = 11,
                OperationNumber = 20,
                Name = "设置单元格填充颜色",
                Description = "设置单元格区域的填充颜色",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点24：设置外边框样式
            new() {
                Id = 12,
                OperationNumber = 24,
                Name = "设置外边框样式",
                Description = "设置单元格区域的外边框样式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点25：设置外边框颜色
            new() {
                Id = 13,
                OperationNumber = 25,
                Name = "设置外边框颜色",
                Description = "设置单元格区域的外边框颜色",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点26：设置垂直对齐方式
            new() {
                Id = 14,
                OperationNumber = 26,
                Name = "设置垂直对齐方式",
                Description = "设置单元格区域的垂直对齐方式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点28：修改sheet表名称
            new() {
                Id = 15,
                OperationNumber = 28,
                Name = "修改sheet表名称",
                Description = "修改工作表的名称",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Workbook
            },
            // 操作点29：添加下划线
            new() {
                Id = 16,
                OperationNumber = 29,
                Name = "添加下划线",
                Description = "为单元格区域添加下划线",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点7：设置字型
            new() {
                Id = 17,
                OperationNumber = 7,
                Name = "设置字型",
                Description = "设置单元格区域的字体样式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点8：设置字号
            new() {
                Id = 18,
                OperationNumber = 8,
                Name = "设置字号",
                Description = "设置单元格区域的字体大小",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点9：字体颜色
            new() {
                Id = 19,
                OperationNumber = 9,
                Name = "字体颜色",
                Description = "设置单元格区域的字体颜色",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点21：设置图案填充样式
            new() {
                Id = 20,
                OperationNumber = 21,
                Name = "设置图案填充样式",
                Description = "设置单元格区域的图案填充样式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点22：设置填充图案颜色
            new() {
                Id = 21,
                OperationNumber = 22,
                Name = "设置填充图案颜色",
                Description = "设置单元格区域的填充图案颜色",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点33：条件格式
            new() {
                Id = 22,
                OperationNumber = 33,
                Name = "条件格式",
                Description = "设置单元格区域的条件格式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点83：设置单元格样式——数据
            new() {
                Id = 23,
                OperationNumber = 83,
                Name = "设置单元格样式——数据",
                Description = "设置单元格区域的预定义样式",
                OperationType = "A",
                Category = ExcelOperationCategory.BasicOperation,
                TargetType = ExcelTargetType.Worksheet
            }
        };
    }

    /// <summary>
    /// 获取基础操作点的参数配置
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationParameter> GetBasicOperationParameters()
    {
        List<ExcelOperationParameter> parameters = new();

        // 操作点1：填充或复制单元格内容的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 1,
                OperationPointId = 1,
                ParameterOrder = 1,
                ParameterName = "目标单元格",
                ParameterDescription = "要填充内容的单元格位置",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "E10"
            },
            new ExcelOperationParameter
            {
                Id = 2,
                OperationPointId = 1,
                ParameterOrder = 2,
                ParameterName = "填充内容",
                ParameterDescription = "要填充的具体内容",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "我的天啊"
            }
        });

        // 操作点4：合并单元格的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 3,
                OperationPointId = 2,
                ParameterOrder = 1,
                ParameterName = "起始单元格",
                ParameterDescription = "合并区域的起始单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1"
            },
            new ExcelOperationParameter
            {
                Id = 4,
                OperationPointId = 2,
                ParameterOrder = 2,
                ParameterName = "结束单元格",
                ParameterDescription = "合并区域的结束单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "C3"
            }
        });

        // 操作点6：设置指定单元格字体的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 5,
                OperationPointId = 3,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置字体的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 6,
                OperationPointId = 3,
                ParameterOrder = 2,
                ParameterName = "字体名称",
                ParameterDescription = "字体名称",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "宋体"
            }
        });

        // 操作点10：内边框样式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 7,
                OperationPointId = 4,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置内边框的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 8,
                OperationPointId = 4,
                ParameterOrder = 2,
                ParameterName = "边框样式",
                ParameterDescription = "内边框线样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 3, // BorderStyle
                IsRequired = true,
                ExampleValue = "xlContinuous"
            }
        });

        // 操作点11：内边框颜色的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 9,
                OperationPointId = 5,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置内边框颜色的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 10,
                OperationPointId = 5,
                ParameterOrder = 2,
                ParameterName = "边框颜色",
                ParameterDescription = "内边框颜色RGB值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "16711680"
            }
        });

        // 操作点13：设置单元格区域水平对齐方式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 11,
                OperationPointId = 6,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置水平对齐的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 12,
                OperationPointId = 6,
                ParameterOrder = 2,
                ParameterName = "水平对齐方式",
                ParameterDescription = "水平对齐方式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 1, // HorizontalAlignment
                IsRequired = true,
                ExampleValue = "xlCenter"
            }
        });

        return parameters;
    }
}
