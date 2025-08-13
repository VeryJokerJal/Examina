using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchSuite.Services
{
    /// <summary>
    /// 参数解析上下文，存储解析后的参数值
    /// </summary>
    public class ParameterResolutionContext
    {
        private readonly Dictionary<string, string> _resolvedParameters = new();
        private readonly Random _random = new();

        /// <summary>
        /// 获取解析后的参数值
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>解析后的参数值</returns>
        public string GetResolvedParameter(string parameterName)
        {
            return _resolvedParameters.TryGetValue(parameterName, out string? value) ? value : string.Empty;
        }

        /// <summary>
        /// 设置解析后的参数值
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="resolvedValue">解析后的值</param>
        public void SetResolvedParameter(string parameterName, string resolvedValue)
        {
            _resolvedParameters[parameterName] = resolvedValue;
        }

        /// <summary>
        /// 检查参数是否已解析
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>是否已解析</returns>
        public bool IsParameterResolved(string parameterName)
        {
            return _resolvedParameters.ContainsKey(parameterName);
        }

        /// <summary>
        /// 生成随机数
        /// </summary>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（包含）</param>
        /// <returns>随机数</returns>
        public int GenerateRandomNumber(int min, int max)
        {
            if (min > max)
                throw new ArgumentException("最小值不能大于最大值");
            
            return _random.Next(min, max + 1);
        }

        /// <summary>
        /// 获取所有解析后的参数
        /// </summary>
        /// <returns>解析后的参数字典</returns>
        public Dictionary<string, string> GetAllResolvedParameters()
        {
            return new Dictionary<string, string>(_resolvedParameters);
        }
    }

    /// <summary>
    /// 参数解析器，处理-1参数的解析
    /// </summary>
    public static class ParameterResolver
    {
        /// <summary>
        /// 编号类型参数名称模式
        /// </summary>
        private static readonly string[] IndexParameterPatterns = 
        {
            "SlideIndex", "SlideNumber", "SlideIndexes",
            "TextBoxIndex", "TextBoxOrder", "TextBoxNumber",
            "ElementIndex", "ElementNumber", "ElementOrder",
            "ShapeIndex", "ShapeNumber", "ShapeOrder",
            "TableIndex", "TableNumber", "TableOrder",
            "ImageIndex", "ImageNumber", "ImageOrder",
            "ChartIndex", "ChartNumber", "ChartOrder"
        };

        /// <summary>
        /// 检查参数是否为编号类型
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>是否为编号类型</returns>
        public static bool IsIndexParameter(string parameterName)
        {
            return IndexParameterPatterns.Any(pattern => 
                parameterName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                parameterName.Contains("Index", StringComparison.OrdinalIgnoreCase) ||
                parameterName.Contains("Number", StringComparison.OrdinalIgnoreCase) ||
                parameterName.Contains("Order", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 解析参数值，将-1转换为随机值（适用于所有数值参数）
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="parameterValue">参数值</param>
        /// <param name="maxValue">最大值</param>
        /// <param name="context">解析上下文</param>
        /// <returns>解析后的参数值</returns>
        public static string ResolveParameter(string parameterName, string parameterValue, int maxValue, ParameterResolutionContext context)
        {
            // 对于所有数值参数，如果值为-1，则进行通配符处理

            // 如果已经解析过，返回缓存的值
            if (context.IsParameterResolved(parameterName))
            {
                return context.GetResolvedParameter(parameterName);
            }

            // 尝试解析为整数
            if (!int.TryParse(parameterValue, out int intValue))
            {
                // 不是数字，直接返回原值
                context.SetResolvedParameter(parameterName, parameterValue);
                return parameterValue;
            }

            // 如果是-1（通配符），生成随机值
            if (intValue == -1)
            {
                if (maxValue <= 0)
                {
                    throw new ArgumentException($"无法为参数 '{parameterName}' 生成随机值，最大值为 {maxValue}");
                }

                int randomValue = context.GenerateRandomNumber(1, maxValue);
                string resolvedValue = randomValue.ToString();
                context.SetResolvedParameter(parameterName, resolvedValue);
                return resolvedValue;
            }

            // 普通数字，直接返回
            context.SetResolvedParameter(parameterName, parameterValue);
            return parameterValue;
        }

        /// <summary>
        /// 解析多个编号参数（用逗号分隔）
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="parameterValue">参数值（逗号分隔）</param>
        /// <param name="maxValue">最大值</param>
        /// <param name="context">解析上下文</param>
        /// <returns>解析后的参数值</returns>
        public static string ResolveMultipleParameters(string parameterName, string parameterValue, int maxValue, ParameterResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(parameterValue))
            {
                return parameterValue;
            }

            // 如果已经解析过，返回缓存的值
            if (context.IsParameterResolved(parameterName))
            {
                return context.GetResolvedParameter(parameterName);
            }

            string[] values = parameterValue.Split(',');
            List<string> resolvedValues = new();

            foreach (string value in values)
            {
                string trimmedValue = value.Trim();
                if (int.TryParse(trimmedValue, out int intValue) && intValue == -1)
                {
                    if (maxValue <= 0)
                    {
                        throw new ArgumentException($"无法为参数 '{parameterName}' 生成随机编号，最大值为 {maxValue}");
                    }
                    
                    int randomValue = context.GenerateRandomNumber(1, maxValue);
                    resolvedValues.Add(randomValue.ToString());
                }
                else
                {
                    resolvedValues.Add(trimmedValue);
                }
            }

            string resolvedValue = string.Join(",", resolvedValues);
            context.SetResolvedParameter(parameterName, resolvedValue);
            return resolvedValue;
        }

        /// <summary>
        /// 获取解析后的整数值
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="context">解析上下文</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>解析后的整数值</returns>
        public static int GetResolvedIntValue(string parameterName, ParameterResolutionContext context, int defaultValue = 0)
        {
            string resolvedValue = context.GetResolvedParameter(parameterName);
            return int.TryParse(resolvedValue, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// 获取解析后的整数数组
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="context">解析上下文</param>
        /// <returns>解析后的整数数组</returns>
        public static int[] GetResolvedIntArray(string parameterName, ParameterResolutionContext context)
        {
            string resolvedValue = context.GetResolvedParameter(parameterName);
            if (string.IsNullOrWhiteSpace(resolvedValue))
            {
                return Array.Empty<int>();
            }

            return resolvedValue.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToArray();
        }
    }
}
