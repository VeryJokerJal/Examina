using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Data.Excel;

/// <summary>
/// Excel基础操作点参数配置扩展类
/// </summary>
public static class ExcelBasicOperationParametersExtended
{
    /// <summary>
    /// 获取剩余基础操作点的参数配置
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationParameter> GetExtendedBasicOperationParameters()
    {
        List<ExcelOperationParameter> parameters = new List<ExcelOperationParameter>();

        // 操作点14：设置目标区域单元格数字分类格式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 13,
                OperationPointId = 7,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置数字格式的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 14,
                OperationPointId = 7,
                ParameterOrder = 2,
                ParameterName = "数字格式",
                ParameterDescription = "数字分类格式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 6, // NumberFormat
                IsRequired = true,
                ExampleValue = "Currency"
            }
        });

        // 操作点15：使用函数的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 15,
                OperationPointId = 8,
                ParameterOrder = 1,
                ParameterName = "目标单元格",
                ParameterDescription = "要输入函数的单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "D1"
            },
            new ExcelOperationParameter
            {
                Id = 16,
                OperationPointId = 8,
                ParameterOrder = 2,
                ParameterName = "期望值",
                ParameterDescription = "函数计算的期望结果",
                DataType = ExcelParameterDataType.String,
                IsRequired = false,
                ExampleValue = "100"
            },
            new ExcelOperationParameter
            {
                Id = 17,
                OperationPointId = 8,
                ParameterOrder = 3,
                ParameterName = "公式内容",
                ParameterDescription = "Excel函数公式",
                DataType = ExcelParameterDataType.Formula,
                IsRequired = true,
                ExampleValue = "=SUM(A1:A10)",
                ValidationRules = "{\"allowedFunctions\":[\"VLOOKUP\",\"IF\",\"SUMIF\",\"ROUND\",\"TEXT\",\"AVERAGE\",\"COUNTIF\"]}"
            }
        });

        // 操作点16：设置行高的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 18,
                OperationPointId = 9,
                ParameterOrder = 1,
                ParameterName = "行号",
                ParameterDescription = "要设置行高的行号（可多个，用逗号分隔）",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "1,3,5"
            },
            new ExcelOperationParameter
            {
                Id = 19,
                OperationPointId = 9,
                ParameterOrder = 2,
                ParameterName = "行高值",
                ParameterDescription = "行高数值（磅）",
                DataType = ExcelParameterDataType.Decimal,
                IsRequired = true,
                ExampleValue = "20.5",
                ValidationRules = "{\"minValue\":0,\"maxValue\":409.5}"
            }
        });

        // 操作点17：设置列宽的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 20,
                OperationPointId = 10,
                ParameterOrder = 1,
                ParameterName = "列号",
                ParameterDescription = "要设置列宽的列号（可多个，用逗号分隔）",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "A,C,E"
            },
            new ExcelOperationParameter
            {
                Id = 21,
                OperationPointId = 10,
                ParameterOrder = 2,
                ParameterName = "列宽值",
                ParameterDescription = "列宽数值",
                DataType = ExcelParameterDataType.Decimal,
                IsRequired = true,
                ExampleValue = "15.5",
                ValidationRules = "{\"minValue\":0,\"maxValue\":255}"
            }
        });

        // 操作点20：设置单元格填充颜色的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 22,
                OperationPointId = 11,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置填充颜色的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 23,
                OperationPointId = 11,
                ParameterOrder = 2,
                ParameterName = "填充颜色",
                ParameterDescription = "填充颜色RGB值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "16777215"
            }
        });

        // 操作点24：设置外边框样式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 24,
                OperationPointId = 12,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置外边框的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 25,
                OperationPointId = 12,
                ParameterOrder = 2,
                ParameterName = "外边框样式",
                ParameterDescription = "外边框线样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 3, // BorderStyle
                IsRequired = true,
                ExampleValue = "xlContinuous"
            }
        });

        // 操作点25：设置外边框颜色的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 26,
                OperationPointId = 13,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置外边框颜色的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 27,
                OperationPointId = 13,
                ParameterOrder = 2,
                ParameterName = "外边框颜色",
                ParameterDescription = "外边框颜色RGB值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "16711680"
            }
        });

        // 操作点26：设置垂直对齐方式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 28,
                OperationPointId = 14,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置垂直对齐的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 29,
                OperationPointId = 14,
                ParameterOrder = 2,
                ParameterName = "垂直对齐方式",
                ParameterDescription = "垂直对齐方式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 2, // VerticalAlignment
                IsRequired = true,
                ExampleValue = "xlCenter"
            }
        });

        // 操作点28：修改sheet表名称的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 30,
                OperationPointId = 15,
                ParameterOrder = 1,
                ParameterName = "原表名",
                ParameterDescription = "要修改的工作表原名称",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "Sheet1"
            },
            new ExcelOperationParameter
            {
                Id = 31,
                OperationPointId = 15,
                ParameterOrder = 2,
                ParameterName = "新表名",
                ParameterDescription = "修改后的工作表名称",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "数据统计表",
                ValidationRules = "{\"maxLength\":31}"
            }
        });

        // 操作点29：添加下划线的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 32,
                OperationPointId = 16,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要添加下划线的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 33,
                OperationPointId = 16,
                ParameterOrder = 2,
                ParameterName = "下划线类型",
                ParameterDescription = "下划线样式类型",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 5, // UnderlineStyle
                IsRequired = true,
                ExampleValue = "xlUnderlineStyleSingle"
            }
        });

        // 操作点7：设置字型的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 34,
                OperationPointId = 17,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置字型的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 35,
                OperationPointId = 17,
                ParameterOrder = 2,
                ParameterName = "字体样式",
                ParameterDescription = "字型（字体样式）",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 4, // FontStyle
                IsRequired = true,
                ExampleValue = "Bold"
            }
        });

        // 操作点8：设置字号的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 36,
                OperationPointId = 18,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置字号的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 37,
                OperationPointId = 18,
                ParameterOrder = 2,
                ParameterName = "字号大小",
                ParameterDescription = "字体大小（磅）",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "12",
                ValidationRules = "{\"minValue\":1,\"maxValue\":409}"
            }
        });

        // 操作点9：字体颜色的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 38,
                OperationPointId = 19,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置字体颜色的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 39,
                OperationPointId = 19,
                ParameterOrder = 2,
                ParameterName = "字体颜色",
                ParameterDescription = "字体颜色RGB值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "16711680"
            }
        });

        // 操作点21：设置图案填充样式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 40,
                OperationPointId = 20,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置图案填充的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 41,
                OperationPointId = 20,
                ParameterOrder = 2,
                ParameterName = "图案样式",
                ParameterDescription = "图案填充样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 7, // PatternStyle
                IsRequired = true,
                ExampleValue = "xlGray8"
            }
        });

        // 操作点22：设置填充图案颜色的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 42,
                OperationPointId = 21,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置填充图案颜色的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 43,
                OperationPointId = 21,
                ParameterOrder = 2,
                ParameterName = "图案颜色",
                ParameterDescription = "填充图案颜色RGB值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "16777215"
            }
        });

        // 操作点33：条件格式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 44,
                OperationPointId = 22,
                ParameterOrder = 1,
                ParameterName = "应用区域",
                ParameterDescription = "条件格式应用的单元格范围",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "D5:D27"
            },
            new ExcelOperationParameter
            {
                Id = 45,
                OperationPointId = 22,
                ParameterOrder = 2,
                ParameterName = "条件类型",
                ParameterDescription = "条件格式类型",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1",
                ValidationRules = "{\"minValue\":1,\"maxValue\":6}"
            },
            new ExcelOperationParameter
            {
                Id = 46,
                OperationPointId = 22,
                ParameterOrder = 3,
                ParameterName = "判断方式",
                ParameterDescription = "条件判断操作符",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = ">"
            },
            new ExcelOperationParameter
            {
                Id = 47,
                OperationPointId = 22,
                ParameterOrder = 4,
                ParameterName = "条件值",
                ParameterDescription = "条件比较值",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "80"
            },
            new ExcelOperationParameter
            {
                Id = 48,
                OperationPointId = 22,
                ParameterOrder = 5,
                ParameterName = "格式类型",
                ParameterDescription = "应用的格式类型",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "FontColor"
            },
            new ExcelOperationParameter
            {
                Id = 49,
                OperationPointId = 22,
                ParameterOrder = 6,
                ParameterName = "格式值",
                ParameterDescription = "格式的具体值",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "16711680"
            }
        });

        // 操作点83：设置单元格样式——数据的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 50,
                OperationPointId = 23,
                ParameterOrder = 1,
                ParameterName = "单元格区域",
                ParameterDescription = "要设置样式的单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:C3"
            },
            new ExcelOperationParameter
            {
                Id = 51,
                OperationPointId = 23,
                ParameterOrder = 2,
                ParameterName = "单元格样式",
                ParameterDescription = "预定义的单元格样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 12, // CellStyle
                IsRequired = true,
                ExampleValue = "Good"
            }
        });

        return parameters;
    }
}
