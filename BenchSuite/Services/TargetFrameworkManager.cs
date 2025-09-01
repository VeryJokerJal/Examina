using System.Runtime.InteropServices;

namespace BenchSuite.Services;

/// <summary>
/// 目标框架管理器 - 管理和验证不同的.NET目标框架
/// </summary>
public static class TargetFrameworkManager
{
    /// <summary>
    /// 支持的目标框架信息
    /// </summary>
    public class TargetFrameworkInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Version Version { get; set; } = new();
        public bool IsLTS { get; set; }
        public bool IsSupported { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredPackages { get; set; } = [];
    }

    /// <summary>
    /// 获取所有支持的目标框架
    /// </summary>
    /// <returns>目标框架信息列表</returns>
    public static List<TargetFrameworkInfo> GetSupportedFrameworks()
    {
        return 
        [
            new TargetFrameworkInfo
            {
                Name = "net9.0",
                DisplayName = ".NET 9.0",
                Version = new Version(9, 0),
                IsLTS = false,
                IsSupported = true,
                Description = "最新的.NET版本，包含最新功能和性能改进",
                RequiredPackages = []
            },
            new TargetFrameworkInfo
            {
                Name = "net8.0",
                DisplayName = ".NET 8.0 (LTS)",
                Version = new Version(8, 0),
                IsLTS = true,
                IsSupported = true,
                Description = "长期支持版本，推荐用于生产环境",
                RequiredPackages = []
            },
            new TargetFrameworkInfo
            {
                Name = "net7.0",
                DisplayName = ".NET 7.0",
                Version = new Version(7, 0),
                IsLTS = false,
                IsSupported = true,
                Description = "标准支持版本",
                RequiredPackages = []
            },
            new TargetFrameworkInfo
            {
                Name = "net6.0",
                DisplayName = ".NET 6.0 (LTS)",
                Version = new Version(6, 0),
                IsLTS = true,
                IsSupported = true,
                Description = "长期支持版本，广泛使用",
                RequiredPackages = []
            },
            new TargetFrameworkInfo
            {
                Name = "netstandard2.1",
                DisplayName = ".NET Standard 2.1",
                Version = new Version(2, 1),
                IsLTS = false,
                IsSupported = true,
                Description = "跨平台标准库，兼容性好",
                RequiredPackages = []
            },
            new TargetFrameworkInfo
            {
                Name = "netstandard2.0",
                DisplayName = ".NET Standard 2.0",
                Version = new Version(2, 0),
                IsLTS = false,
                IsSupported = true,
                Description = "广泛兼容的标准库版本",
                RequiredPackages = []
            }
        ];
    }

    /// <summary>
    /// 获取默认目标框架
    /// </summary>
    /// <returns>默认目标框架名称</returns>
    public static string GetDefaultTargetFramework()
    {
        // 根据当前运行时环境选择默认框架
        string runtimeVersion = Environment.Version.ToString();
        
        if (runtimeVersion.StartsWith("9."))
            return "net9.0";
        else if (runtimeVersion.StartsWith("8."))
            return "net8.0";
        else if (runtimeVersion.StartsWith("7."))
            return "net7.0";
        else if (runtimeVersion.StartsWith("6."))
            return "net6.0";
        else
            return "net8.0"; // 默认使用LTS版本
    }

    /// <summary>
    /// 验证目标框架是否受支持
    /// </summary>
    /// <param name="targetFramework">目标框架名称</param>
    /// <returns>是否受支持</returns>
    public static bool IsFrameworkSupported(string targetFramework)
    {
        if (string.IsNullOrEmpty(targetFramework)) return false;
        
        return GetSupportedFrameworks()
            .Any(f => f.Name.Equals(targetFramework, StringComparison.OrdinalIgnoreCase) && f.IsSupported);
    }

    /// <summary>
    /// 获取目标框架信息
    /// </summary>
    /// <param name="targetFramework">目标框架名称</param>
    /// <returns>目标框架信息</returns>
    public static TargetFrameworkInfo? GetFrameworkInfo(string targetFramework)
    {
        if (string.IsNullOrEmpty(targetFramework)) return null;
        
        return GetSupportedFrameworks()
            .FirstOrDefault(f => f.Name.Equals(targetFramework, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取推荐的目标框架
    /// </summary>
    /// <param name="preferLTS">是否优先选择LTS版本</param>
    /// <returns>推荐的目标框架名称</returns>
    public static string GetRecommendedFramework(bool preferLTS = true)
    {
        var frameworks = GetSupportedFrameworks()
            .Where(f => f.IsSupported)
            .OrderByDescending(f => f.Version);

        if (preferLTS)
        {
            var ltsFramework = frameworks.FirstOrDefault(f => f.IsLTS);
            if (ltsFramework != null)
                return ltsFramework.Name;
        }

        return frameworks.First().Name;
    }

    /// <summary>
    /// 检测当前环境支持的最高框架版本
    /// </summary>
    /// <returns>支持的最高框架版本</returns>
    public static string DetectHighestSupportedFramework()
    {
        try
        {
            // 检测当前运行时版本
            string runtimeVersion = RuntimeInformation.FrameworkDescription;
            
            // 解析版本号
            if (runtimeVersion.Contains(".NET 9"))
                return "net9.0";
            else if (runtimeVersion.Contains(".NET 8"))
                return "net8.0";
            else if (runtimeVersion.Contains(".NET 7"))
                return "net7.0";
            else if (runtimeVersion.Contains(".NET 6"))
                return "net6.0";
            else if (runtimeVersion.Contains(".NET Core"))
                return "netstandard2.1";
            else
                return GetDefaultTargetFramework();
        }
        catch
        {
            return GetDefaultTargetFramework();
        }
    }

    /// <summary>
    /// 获取框架兼容性信息
    /// </summary>
    /// <param name="sourceFramework">源框架</param>
    /// <param name="targetFramework">目标框架</param>
    /// <returns>兼容性信息</returns>
    public static (bool IsCompatible, string Reason) CheckCompatibility(string sourceFramework, string targetFramework)
    {
        var sourceInfo = GetFrameworkInfo(sourceFramework);
        var targetInfo = GetFrameworkInfo(targetFramework);

        if (sourceInfo == null)
            return (false, $"不支持的源框架: {sourceFramework}");
        
        if (targetInfo == null)
            return (false, $"不支持的目标框架: {targetFramework}");

        // .NET Standard 兼容性检查
        if (sourceFramework.StartsWith("netstandard") && targetFramework.StartsWith("net"))
        {
            return (true, ".NET Standard 兼容 .NET");
        }

        if (targetFramework.StartsWith("netstandard") && sourceFramework.StartsWith("net"))
        {
            return (false, ".NET 不能直接兼容到 .NET Standard");
        }

        // 同类型框架版本兼容性
        if (sourceFramework.StartsWith("net") && targetFramework.StartsWith("net"))
        {
            if (sourceInfo.Version <= targetInfo.Version)
                return (true, "向上兼容");
            else
                return (false, "不支持向下兼容");
        }

        return (true, "基本兼容");
    }

    /// <summary>
    /// 生成框架选择建议
    /// </summary>
    /// <param name="requirements">需求描述</param>
    /// <returns>框架选择建议</returns>
    public static string GenerateFrameworkRecommendation(string requirements = "")
    {
        var recommendations = new List<string>();

        recommendations.Add("🎯 目标框架选择建议:");
        recommendations.Add("");

        if (requirements.Contains("生产") || requirements.Contains("稳定"))
        {
            recommendations.Add("📋 生产环境推荐:");
            recommendations.Add("  • net8.0 (LTS) - 长期支持，稳定可靠");
            recommendations.Add("  • net6.0 (LTS) - 广泛支持，成熟稳定");
        }
        else if (requirements.Contains("最新") || requirements.Contains("性能"))
        {
            recommendations.Add("🚀 最新功能推荐:");
            recommendations.Add("  • net9.0 - 最新功能和性能优化");
            recommendations.Add("  • net8.0 - 平衡性能和稳定性");
        }
        else if (requirements.Contains("兼容") || requirements.Contains("库"))
        {
            recommendations.Add("🔗 兼容性推荐:");
            recommendations.Add("  • netstandard2.1 - 最大兼容性");
            recommendations.Add("  • netstandard2.0 - 广泛兼容");
        }
        else
        {
            recommendations.Add("💡 通用推荐:");
            recommendations.Add("  • net8.0 (LTS) - 推荐用于大多数项目");
            recommendations.Add("  • net9.0 - 如需最新功能");
            recommendations.Add("  • netstandard2.1 - 如需跨平台兼容");
        }

        recommendations.Add("");
        recommendations.Add("ℹ️ 当前环境支持: " + DetectHighestSupportedFramework());

        return string.Join("\n", recommendations);
    }

    /// <summary>
    /// 获取框架特定的编译选项
    /// </summary>
    /// <param name="targetFramework">目标框架</param>
    /// <returns>编译选项字典</returns>
    public static Dictionary<string, string> GetFrameworkSpecificOptions(string targetFramework)
    {
        var options = new Dictionary<string, string>();

        switch (targetFramework.ToLowerInvariant())
        {
            case "net9.0":
                options["LangVersion"] = "latest";
                options["Nullable"] = "enable";
                options["ImplicitUsings"] = "enable";
                break;
            
            case "net8.0":
                options["LangVersion"] = "12.0";
                options["Nullable"] = "enable";
                options["ImplicitUsings"] = "enable";
                break;
            
            case "net7.0":
                options["LangVersion"] = "11.0";
                options["Nullable"] = "enable";
                break;
            
            case "net6.0":
                options["LangVersion"] = "10.0";
                options["Nullable"] = "enable";
                break;
            
            case "netstandard2.1":
                options["LangVersion"] = "8.0";
                break;
            
            case "netstandard2.0":
                options["LangVersion"] = "7.3";
                break;
        }

        return options;
    }
}
