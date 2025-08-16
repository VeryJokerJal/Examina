using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Data.Excel;

/// <summary>
/// Excel图表操作点参数配置类
/// </summary>
public static class ExcelChartOperationParameters
{
    /// <summary>
    /// 获取图表操作点的参数配置（第一部分）
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationParameter> GetChartOperationParametersPart1()
    {
        List<ExcelOperationParameter> parameters = new();

        // 操作点101：图表类型的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 70,
                OperationPointId = 30,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 71,
                OperationPointId = 30,
                ParameterOrder = 2,
                ParameterName = "图表类型",
                ParameterDescription = "图表类型枚举值",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 8, // ChartType
                IsRequired = true,
                ExampleValue = "xlColumnClustered"
            }
        });

        // 操作点102：图表样式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 72,
                OperationPointId = 31,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 73,
                OperationPointId = 31,
                ParameterOrder = 2,
                ParameterName = "样式编号",
                ParameterDescription = "图表样式编号（1-48）",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "5",
                ValidationRules = "{\"minValue\":1,\"maxValue\":48}"
            }
        });

        // 操作点103：图表移动的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 74,
                OperationPointId = 32,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "要移动的图表编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 75,
                OperationPointId = 32,
                ParameterOrder = 2,
                ParameterName = "起始单元格",
                ParameterDescription = "图表移动的起始位置",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1"
            },
            new ExcelOperationParameter
            {
                Id = 76,
                OperationPointId = 32,
                ParameterOrder = 3,
                ParameterName = "结束单元格",
                ParameterDescription = "图表移动的结束位置",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "F10"
            }
        });

        // 操作点104：分类轴数据区域的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 77,
                OperationPointId = 33,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 78,
                OperationPointId = 33,
                ParameterOrder = 2,
                ParameterName = "目标工作簿",
                ParameterDescription = "数据源工作簿名称",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "Sheet1"
            },
            new ExcelOperationParameter
            {
                Id = 79,
                OperationPointId = 33,
                ParameterOrder = 3,
                ParameterName = "起始单元格",
                ParameterDescription = "分类轴数据的起始单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A2"
            },
            new ExcelOperationParameter
            {
                Id = 80,
                OperationPointId = 33,
                ParameterOrder = 4,
                ParameterName = "终止单元格",
                ParameterDescription = "分类轴数据的终止单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A10"
            }
        });

        // 操作点105：数值轴数据区域的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 81,
                OperationPointId = 34,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 82,
                OperationPointId = 34,
                ParameterOrder = 2,
                ParameterName = "目标工作簿",
                ParameterDescription = "数据源工作簿名称",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "Sheet1"
            },
            new ExcelOperationParameter
            {
                Id = 83,
                OperationPointId = 34,
                ParameterOrder = 3,
                ParameterName = "起始单元格",
                ParameterDescription = "数值轴数据的起始单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "B2"
            },
            new ExcelOperationParameter
            {
                Id = 84,
                OperationPointId = 34,
                ParameterOrder = 4,
                ParameterName = "终止单元格",
                ParameterDescription = "数值轴数据的终止单元格",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "B10"
            }
        });

        // 操作点107：图表标题的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 85,
                OperationPointId = 35,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 86,
                OperationPointId = 35,
                ParameterOrder = 2,
                ParameterName = "图表标题",
                ParameterDescription = "图表标题文本",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "销售数据统计图"
            }
        });

        // 操作点108：图表标题格式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 87,
                OperationPointId = 36,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 88,
                OperationPointId = 36,
                ParameterOrder = 2,
                ParameterName = "标题字体",
                ParameterDescription = "图表标题字体",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "宋体"
            },
            new ExcelOperationParameter
            {
                Id = 89,
                OperationPointId = 36,
                ParameterOrder = 3,
                ParameterName = "字体样式",
                ParameterDescription = "图表标题字体样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 4, // FontStyle
                IsRequired = true,
                ExampleValue = "Bold"
            },
            new ExcelOperationParameter
            {
                Id = 90,
                OperationPointId = 36,
                ParameterOrder = 4,
                ParameterName = "字号",
                ParameterDescription = "图表标题字号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "14",
                ValidationRules = "{\"minValue\":1,\"maxValue\":409}"
            },
            new ExcelOperationParameter
            {
                Id = 91,
                OperationPointId = 36,
                ParameterOrder = 5,
                ParameterName = "字体颜色",
                ParameterDescription = "图表标题颜色值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "16711680"
            }
        });

        return parameters;
    }

    /// <summary>
    /// 获取图表操作点的参数配置（第二部分）
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationParameter> GetChartOperationParametersPart2()
    {
        List<ExcelOperationParameter> parameters = new();

        // 操作点112：主要横坐标轴标题的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 92,
                OperationPointId = 37,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 93,
                OperationPointId = 37,
                ParameterOrder = 2,
                ParameterName = "横坐标轴标题",
                ParameterDescription = "横坐标轴标题文本",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "月份"
            }
        });

        // 操作点113：主要横坐标轴标题格式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 94,
                OperationPointId = 38,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 95,
                OperationPointId = 38,
                ParameterOrder = 2,
                ParameterName = "轴标题字体",
                ParameterDescription = "横坐标轴标题字体",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "宋体"
            },
            new ExcelOperationParameter
            {
                Id = 96,
                OperationPointId = 38,
                ParameterOrder = 3,
                ParameterName = "字体样式",
                ParameterDescription = "横坐标轴标题字体样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 4, // FontStyle
                IsRequired = true,
                ExampleValue = "Regular"
            },
            new ExcelOperationParameter
            {
                Id = 97,
                OperationPointId = 38,
                ParameterOrder = 4,
                ParameterName = "字号",
                ParameterDescription = "横坐标轴标题字号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "12",
                ValidationRules = "{\"minValue\":1,\"maxValue\":409}"
            },
            new ExcelOperationParameter
            {
                Id = 98,
                OperationPointId = 38,
                ParameterOrder = 5,
                ParameterName = "字体颜色",
                ParameterDescription = "横坐标轴标题颜色值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "0"
            }
        });

        // 操作点122：设置图例位置的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 99,
                OperationPointId = 39,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 100,
                OperationPointId = 39,
                ParameterOrder = 2,
                ParameterName = "图例位置",
                ParameterDescription = "图例显示位置",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 9, // LegendPosition
                IsRequired = true,
                ExampleValue = "Right"
            }
        });

        // 操作点123：设置图例格式的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 101,
                OperationPointId = 40,
                ParameterOrder = 1,
                ParameterName = "图表编号",
                ParameterDescription = "目标图表的编号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "1"
            },
            new ExcelOperationParameter
            {
                Id = 102,
                OperationPointId = 40,
                ParameterOrder = 2,
                ParameterName = "图例字体",
                ParameterDescription = "图例字体",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "宋体"
            },
            new ExcelOperationParameter
            {
                Id = 103,
                OperationPointId = 40,
                ParameterOrder = 3,
                ParameterName = "字体样式",
                ParameterDescription = "图例字体样式",
                DataType = ExcelParameterDataType.Enum,
                EnumTypeId = 4, // FontStyle
                IsRequired = true,
                ExampleValue = "Regular"
            },
            new ExcelOperationParameter
            {
                Id = 104,
                OperationPointId = 40,
                ParameterOrder = 4,
                ParameterName = "字号",
                ParameterDescription = "图例字号",
                DataType = ExcelParameterDataType.Integer,
                IsRequired = true,
                ExampleValue = "10",
                ValidationRules = "{\"minValue\":1,\"maxValue\":409}"
            },
            new ExcelOperationParameter
            {
                Id = 105,
                OperationPointId = 40,
                ParameterOrder = 5,
                ParameterName = "字体颜色",
                ParameterDescription = "图例颜色值",
                DataType = ExcelParameterDataType.Color,
                IsRequired = true,
                ExampleValue = "0"
            }
        });

        return parameters;
    }

    /// <summary>
    /// 获取所有图表操作点的参数配置
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationParameter> GetAllChartOperationParameters()
    {
        List<ExcelOperationParameter> allParameters = new();
        allParameters.AddRange(GetChartOperationParametersPart1());
        allParameters.AddRange(GetChartOperationParametersPart2());
        // 这里可以继续添加更多部分的参数
        return allParameters;
    }
}
