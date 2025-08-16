using ExaminaWebApplication.Models.Excel;

namespace ExaminaWebApplication.Data.Excel;

/// <summary>
/// Excel图表操作点数据定义类
/// </summary>
public static class ExcelChartOperationData
{
    /// <summary>
    /// 获取所有图表操作点定义
    /// </summary>
    /// <returns></returns>
    public static List<ExcelOperationPoint> GetChartOperationPoints()
    {
        return new List<ExcelOperationPoint>
        {
            // 操作点101：图表类型
            new() {
                Id = 30,
                OperationNumber = 101,
                Name = "图表类型",
                Description = "设置图表的类型",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点102：图表样式
            new() {
                Id = 31,
                OperationNumber = 102,
                Name = "图表样式",
                Description = "设置图表的样式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点103：图表移动
            new() {
                Id = 32,
                OperationNumber = 103,
                Name = "图表移动",
                Description = "移动图表到指定位置",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点104：分类轴数据区域
            new() {
                Id = 33,
                OperationNumber = 104,
                Name = "分类轴数据区域",
                Description = "设置图表横轴（分类轴）上每个刻度对应的标签",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点105：数值轴数据区域
            new() {
                Id = 34,
                OperationNumber = 105,
                Name = "数值轴数据区域",
                Description = "设置图表纵轴的数据区域",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点107：图表标题
            new() {
                Id = 35,
                OperationNumber = 107,
                Name = "图表标题",
                Description = "设置图表标题",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点108：图表标题格式
            new() {
                Id = 36,
                OperationNumber = 108,
                Name = "图表标题格式",
                Description = "设置图表标题的格式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点112：主要横坐标轴标题
            new() {
                Id = 37,
                OperationNumber = 112,
                Name = "主要横坐标轴标题",
                Description = "设置主要横坐标轴标题",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点113：主要横坐标轴标题格式
            new() {
                Id = 38,
                OperationNumber = 113,
                Name = "主要横坐标轴标题格式",
                Description = "设置主要横坐标轴标题的格式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点122：设置图例位置
            new() {
                Id = 39,
                OperationNumber = 122,
                Name = "设置图例位置",
                Description = "设置图例的位置",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点123：设置图例格式
            new() {
                Id = 40,
                OperationNumber = 123,
                Name = "设置图例格式",
                Description = "设置图例的格式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点139：设置主要纵坐标轴选项
            new() {
                Id = 41,
                OperationNumber = 139,
                Name = "设置主要纵坐标轴选项",
                Description = "设置主要纵坐标轴的各种选项",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点140：设置网格线——主要横网格线
            new() {
                Id = 42,
                OperationNumber = 140,
                Name = "设置网格线——主要横网格线",
                Description = "设置主要横网格线",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点141：设置网格线——次要横网格线
            new() {
                Id = 43,
                OperationNumber = 141,
                Name = "设置网格线——次要横网格线",
                Description = "设置次要横网格线",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点142：主要纵网格线
            new() {
                Id = 44,
                OperationNumber = 142,
                Name = "主要纵网格线",
                Description = "设置主要纵网格线",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点143：次要纵网格线
            new() {
                Id = 45,
                OperationNumber = 143,
                Name = "次要纵网格线",
                Description = "设置次要纵网格线",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点145：设置数据系列格式
            new() {
                Id = 46,
                OperationNumber = 145,
                Name = "设置数据系列格式",
                Description = "设置数据系列的格式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点154：添加数据标签
            new() {
                Id = 47,
                OperationNumber = 154,
                Name = "添加数据标签",
                Description = "为图表添加数据标签",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点155：设置数据标签格式
            new() {
                Id = 48,
                OperationNumber = 155,
                Name = "设置数据标签格式",
                Description = "设置数据标签的格式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点156：设置图表区域格式
            new() {
                Id = 49,
                OperationNumber = 156,
                Name = "设置图表区域格式",
                Description = "设置图表区域的格式",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点159：显示图表基底颜色
            new() {
                Id = 50,
                OperationNumber = 159,
                Name = "显示图表基底颜色",
                Description = "设置图表基底的颜色",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            },
            // 操作点160：设置图表边框线
            new() {
                Id = 51,
                OperationNumber = 160,
                Name = "设置图表边框线",
                Description = "设置图表的边框线",
                OperationType = "B",
                Category = ExcelOperationCategory.ChartOperation,
                TargetType = ExcelTargetType.Chart
            }
        };
    }
}
