using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Data.Excel;

/// <summary>
/// Excel数据清单操作点数据定义类
/// </summary>
public static class ExcelDataListOperationData
{
    /// <summary>
    /// 获取所有数据清单操作点定义
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationPoint> GetDataListOperationPoints()
    {
        return new List<ExcelOperationPoint>
        {
            // 操作点31：筛选
            new ExcelOperationPoint
            {
                Id = 24,
                OperationNumber = 31,
                Name = "筛选",
                Description = "对数据清单进行筛选操作",
                OperationType = "A",
                Category = ExcelOperationCategory.DataListOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点32：排序
            new ExcelOperationPoint
            {
                Id = 25,
                OperationNumber = 32,
                Name = "排序",
                Description = "对数据清单进行排序操作",
                OperationType = "A",
                Category = ExcelOperationCategory.DataListOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点35：分类汇总
            new ExcelOperationPoint
            {
                Id = 26,
                OperationNumber = 35,
                Name = "分类汇总",
                Description = "对数据清单进行分类汇总操作",
                OperationType = "A",
                Category = ExcelOperationCategory.DataListOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点36：高级筛选-条件
            new ExcelOperationPoint
            {
                Id = 27,
                OperationNumber = 36,
                Name = "高级筛选-条件",
                Description = "使用条件区域进行高级筛选",
                OperationType = "A",
                Category = ExcelOperationCategory.DataListOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点63：高级筛选-数据
            new ExcelOperationPoint
            {
                Id = 28,
                OperationNumber = 63,
                Name = "高级筛选-数据",
                Description = "高级筛选的数据处理",
                OperationType = "A",
                Category = ExcelOperationCategory.DataListOperation,
                TargetType = ExcelTargetType.Worksheet
            },
            // 操作点71：数据透视表
            new ExcelOperationPoint
            {
                Id = 29,
                OperationNumber = 71,
                Name = "数据透视表",
                Description = "创建和配置数据透视表",
                OperationType = "A",
                Category = ExcelOperationCategory.DataListOperation,
                TargetType = ExcelTargetType.Worksheet
            }
        };
    }

    /// <summary>
    /// 获取数据清单操作点的参数配置
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationParameter> GetDataListOperationParameters()
    {
        List<ExcelOperationParameter> parameters = new List<ExcelOperationParameter>();

        // 操作点31：筛选的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 52,
                OperationPointId = 24,
                ParameterOrder = 1,
                ParameterName = "筛选条件",
                ParameterDescription = "筛选条件配置（键值对方式）",
                DataType = ExcelParameterDataType.JsonObject,
                IsRequired = true,
                ExampleValue = "{\"2\":\"=工商管理xlOr=计算机\",\"5\":\"<90xlAnd>70\",\"6\":\"<90xlAnd>70\"}",
                ValidationRules = "{\"description\":\"列索引:条件值，支持xlOr和xlAnd逻辑操作\"}"
            }
        });

        // 操作点32：排序的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 53,
                OperationPointId = 25,
                ParameterOrder = 1,
                ParameterName = "排序列",
                ParameterDescription = "要排序的列（可多个，用逗号分隔）",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "A,B,C"
            },
            new ExcelOperationParameter
            {
                Id = 54,
                OperationPointId = 25,
                ParameterOrder = 2,
                ParameterName = "排序方式",
                ParameterDescription = "排序方式（升序/降序，对应排序列）",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "ASC,DESC,ASC",
                ValidationRules = "{\"allowedValues\":[\"ASC\",\"DESC\"]}"
            }
        });

        // 操作点35：分类汇总的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 55,
                OperationPointId = 26,
                ParameterOrder = 1,
                ParameterName = "汇总位置",
                ParameterDescription = "分类汇总结果的位置（多个备选位置）",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "E12;E34;F22;F34;G12;G33"
            },
            new ExcelOperationParameter
            {
                Id = 56,
                OperationPointId = 26,
                ParameterOrder = 2,
                ParameterName = "汇总函数",
                ParameterDescription = "SUBTOTAL函数配置",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "SUBTOTAL(4,*)",
                ValidationRules = "{\"description\":\"识别函数编号为4的SUBTOTAL函数\"}"
            }
        });

        // 操作点36：高级筛选-条件的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 57,
                OperationPointId = 27,
                ParameterOrder = 1,
                ParameterName = "条件区域",
                ParameterDescription = "筛选的条件区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "J5:L7"
            },
            new ExcelOperationParameter
            {
                Id = 58,
                OperationPointId = 27,
                ParameterOrder = 2,
                ParameterName = "复制目标区域",
                ParameterDescription = "筛选后复制结果的起始单元格区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = false,
                ExampleValue = "J10"
            },
            new ExcelOperationParameter
            {
                Id = 59,
                OperationPointId = 27,
                ParameterOrder = 3,
                ParameterName = "筛选字段",
                ParameterDescription = "参与筛选的字段（列标题名称）",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "Subject,Class,Chinese"
            },
            new ExcelOperationParameter
            {
                Id = 60,
                OperationPointId = 27,
                ParameterOrder = 4,
                ParameterName = "筛选条件",
                ParameterDescription = "多组条件，分号分隔表示或关系",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "计算机,1班,>80;工商管理,2班,>80"
            },
            new ExcelOperationParameter
            {
                Id = 61,
                OperationPointId = 27,
                ParameterOrder = 5,
                ParameterName = "启用高级筛选",
                ParameterDescription = "是否启用高级筛选",
                DataType = ExcelParameterDataType.Boolean,
                IsRequired = true,
                DefaultValue = "True"
            },
            new ExcelOperationParameter
            {
                Id = 62,
                OperationPointId = 27,
                ParameterOrder = 6,
                ParameterName = "仅显示唯一值",
                ParameterDescription = "是否只显示唯一值",
                DataType = ExcelParameterDataType.Boolean,
                IsRequired = false,
                DefaultValue = "False"
            }
        });

        // 操作点63：高级筛选-数据的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 63,
                OperationPointId = 28,
                ParameterOrder = 1,
                ParameterName = "数据源区域",
                ParameterDescription = "高级筛选的数据源区域",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "A1:F100"
            }
        });

        // 操作点71：数据透视表的参数
        parameters.AddRange(new[]
        {
            new ExcelOperationParameter
            {
                Id = 64,
                OperationPointId = 29,
                ParameterOrder = 1,
                ParameterName = "行字段",
                ParameterDescription = "设置为透视表的行字段",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                AllowMultipleValues = true,
                ExampleValue = "学历,职务"
            },
            new ExcelOperationParameter
            {
                Id = 65,
                OperationPointId = 29,
                ParameterOrder = 2,
                ParameterName = "列字段",
                ParameterDescription = "设置为透视表的列字段（可空）",
                DataType = ExcelParameterDataType.String,
                IsRequired = false,
                AllowMultipleValues = true
            },
            new ExcelOperationParameter
            {
                Id = 66,
                OperationPointId = 29,
                ParameterOrder = 3,
                ParameterName = "数据字段",
                ParameterDescription = "用于聚合的字段名称",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "年龄"
            },
            new ExcelOperationParameter
            {
                Id = 67,
                OperationPointId = 29,
                ParameterOrder = 4,
                ParameterName = "聚合函数",
                ParameterDescription = "聚合函数类型",
                DataType = ExcelParameterDataType.String,
                IsRequired = true,
                ExampleValue = "Average",
                ValidationRules = "{\"allowedValues\":[\"Sum\",\"Average\",\"Count\",\"Max\",\"Min\"]}"
            },
            new ExcelOperationParameter
            {
                Id = 68,
                OperationPointId = 29,
                ParameterOrder = 5,
                ParameterName = "插入位置",
                ParameterDescription = "插入透视表的起始单元格位置",
                DataType = ExcelParameterDataType.CellRange,
                IsRequired = true,
                ExampleValue = "$C$28"
            },
            new ExcelOperationParameter
            {
                Id = 69,
                OperationPointId = 29,
                ParameterOrder = 6,
                ParameterName = "透视表名称",
                ParameterDescription = "透视表名称（可选）",
                DataType = ExcelParameterDataType.String,
                IsRequired = false,
                ExampleValue = "学历职务年龄统计表"
            }
        });

        return parameters;
    }
}
