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
        private readonly Dictionary<string, string> _resolvedParameters = [];
        private readonly Random _random;
        private readonly string _contextId;

        /// <summary>
        /// 使用默认随机种子初始化上下文
        /// </summary>
        public ParameterResolutionContext() : this(Environment.TickCount.ToString())
        {
        }

        /// <summary>
        /// 使用指定的上下文ID初始化，确保相同ID产生相同的随机序列
        /// </summary>
        /// <param name="contextId">上下文标识符，用于生成确定性随机种子</param>
        public ParameterResolutionContext(string contextId)
        {
            _contextId = contextId ?? Environment.TickCount.ToString();

            // 使用上下文ID的哈希值作为随机种子，确保相同ID产生相同的随机序列
            int seed = _contextId.GetHashCode();
            _random = new Random(seed);
        }

        /// <summary>
        /// 获取上下文ID
        /// </summary>
        public string ContextId => _contextId;

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

        /// <summary>
        /// 重置解析上下文，清除所有已解析的参数
        /// </summary>
        public void Reset()
        {
            _resolvedParameters.Clear();
        }

        /// <summary>
        /// 获取解析日志信息，用于调试
        /// </summary>
        /// <returns>解析日志字符串</returns>
        public string GetResolutionLog()
        {
            if (_resolvedParameters.Count == 0)
            {
                return "无参数解析记录";
            }

            List<string> logEntries = [$"上下文ID: {_contextId}", $"随机种子: {_contextId.GetHashCode()}"];

            foreach (KeyValuePair<string, string> param in _resolvedParameters)
            {
                logEntries.Add($"  {param.Key}: {param.Value}");
            }

            return string.Join(Environment.NewLine, logEntries);
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
        [
            "SlideIndex", "SlideNumber", "SlideIndexes",
            "TextBoxIndex", "TextBoxOrder", "TextBoxNumber",
            "ElementIndex", "ElementNumber", "ElementOrder",
            "ShapeIndex", "ShapeNumber", "ShapeOrder",
            "TableIndex", "TableNumber", "TableOrder",
            "ImageIndex", "ImageNumber", "ImageOrder",
            "ChartIndex", "ChartNumber", "ChartOrder"
        ];

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
            // 参数验证
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException("参数名称不能为空", nameof(parameterName));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "解析上下文不能为null");
            }

            // 如果已经解析过，返回缓存的值
            if (context.IsParameterResolved(parameterName))
            {
                string cachedValue = context.GetResolvedParameter(parameterName);
                return cachedValue;
            }

            // 处理空值或null
            if (string.IsNullOrEmpty(parameterValue))
            {
                context.SetResolvedParameter(parameterName, string.Empty);
                return string.Empty;
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
                    string errorMsg = $"无法为参数 '{parameterName}' 生成随机值，最大值为 {maxValue}。上下文ID: {context.ContextId}";
                    throw new ArgumentException(errorMsg);
                }

                int randomValue = context.GenerateRandomNumber(1, maxValue);
                string resolvedValue = randomValue.ToString();
                context.SetResolvedParameter(parameterName, resolvedValue);

                return resolvedValue;
            }

            // 验证数值范围
            if (intValue < 0)
            {
                throw new ArgumentException($"参数 '{parameterName}' 的值 {intValue} 无效，不支持负数（除-1外）");
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
            // 参数验证
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException("参数名称不能为空", nameof(parameterName));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "解析上下文不能为null");
            }

            if (string.IsNullOrWhiteSpace(parameterValue))
            {
                context.SetResolvedParameter(parameterName, string.Empty);
                return string.Empty;
            }

            // 如果已经解析过，返回缓存的值
            if (context.IsParameterResolved(parameterName))
            {
                return context.GetResolvedParameter(parameterName);
            }

            string[] values = parameterValue.Split(',');
            List<string> resolvedValues = [];
            List<string> errors = [];

            foreach (string value in values)
            {
                string trimmedValue = value.Trim();

                if (string.IsNullOrEmpty(trimmedValue))
                {
                    errors.Add("发现空值");
                    continue;
                }

                if (int.TryParse(trimmedValue, out int intValue))
                {
                    if (intValue == -1)
                    {
                        if (maxValue <= 0)
                        {
                            errors.Add($"无法为值 '{trimmedValue}' 生成随机数，最大值为 {maxValue}");
                            continue;
                        }

                        int randomValue = context.GenerateRandomNumber(1, maxValue);
                        resolvedValues.Add(randomValue.ToString());
                    }
                    else if (intValue < 0)
                    {
                        errors.Add($"值 '{trimmedValue}' 无效，不支持负数（除-1外）");
                        continue;
                    }
                    else
                    {
                        resolvedValues.Add(trimmedValue);
                    }
                }
                else
                {
                    // 非数字值，直接保留
                    resolvedValues.Add(trimmedValue);
                }
            }

            if (errors.Count > 0)
            {
                string errorMsg = $"解析参数 '{parameterName}' 时发生错误: {string.Join("; ", errors)}。上下文ID: {context.ContextId}";
                throw new ArgumentException(errorMsg);
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
