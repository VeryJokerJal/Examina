using System;
using System.Linq;
using ExamLab.Services;
using ExamLab.Models;

namespace ExamLab
{
    /// <summary>
    /// PPT知识点配置验证测试
    /// </summary>
    public class PPTKnowledgeTest
    {
        public static void ValidateKnowledgePoints()
        {
            Console.WriteLine("=== PPT知识点配置验证 ===");
            
            var service = PowerPointKnowledgeService.Instance;
            var allConfigs = service.GetAllKnowledgeConfigs().ToList();
            
            Console.WriteLine($"总共配置的知识点数量: {allConfigs.Count}");
            
            // 验证是否有39个知识点
            if (allConfigs.Count != 39)
            {
                Console.WriteLine($"❌ 错误：应该有39个知识点，但实际有{allConfigs.Count}个");
                return;
            }
            
            Console.WriteLine("✅ 知识点数量正确：39个");
            
            // 按分类统计
            var categories = allConfigs.GroupBy(c => c.Category).ToList();
            Console.WriteLine("\n=== 按分类统计 ===");
            foreach (var category in categories)
            {
                Console.WriteLine($"{category.Key}: {category.Count()}个知识点");
                foreach (var config in category.OrderBy(c => (int)c.KnowledgeType))
                {
                    Console.WriteLine($"  - 知识点{(int)config.KnowledgeType}: {config.Name}");
                }
            }
            
            // 验证枚举值连续性
            Console.WriteLine("\n=== 验证枚举值连续性 ===");
            var enumValues = allConfigs.Select(c => (int)c.KnowledgeType).OrderBy(x => x).ToList();
            bool isSequential = true;
            for (int i = 0; i < enumValues.Count; i++)
            {
                if (enumValues[i] != i + 1)
                {
                    Console.WriteLine($"❌ 错误：枚举值不连续，期望{i + 1}，实际{enumValues[i]}");
                    isSequential = false;
                }
            }
            
            if (isSequential)
            {
                Console.WriteLine("✅ 枚举值连续性正确：1-39");
            }
            
            // 验证每个知识点的配置参数
            Console.WriteLine("\n=== 验证配置参数 ===");
            int validConfigs = 0;
            foreach (var config in allConfigs.OrderBy(c => (int)c.KnowledgeType))
            {
                if (config.ParameterTemplates.Count > 0)
                {
                    validConfigs++;
                }
                else
                {
                    Console.WriteLine($"❌ 知识点{(int)config.KnowledgeType}({config.Name})缺少配置参数");
                }
            }
            
            Console.WriteLine($"✅ 有效配置的知识点: {validConfigs}/{allConfigs.Count}");
            
            // 验证用户要求的关键知识点
            Console.WriteLine("\n=== 验证关键知识点 ===");
            var keyKnowledgePoints = new[]
            {
                (1, "设置幻灯片版式"),
                (2, "删除幻灯片"),
                (3, "插入幻灯片"),
                (31, "设置文稿应用主题"),
                (32, "设置幻灯片背景"),
                (33, "单元格内容"),
                (39, "设置背景样式")
            };
            
            foreach (var (id, name) in keyKnowledgePoints)
            {
                var config = allConfigs.FirstOrDefault(c => (int)c.KnowledgeType == id);
                if (config != null && config.Name == name)
                {
                    Console.WriteLine($"✅ 知识点{id}: {name} - 配置正确");
                }
                else
                {
                    Console.WriteLine($"❌ 知识点{id}: {name} - 配置错误或缺失");
                }
            }
            
            Console.WriteLine("\n=== 验证完成 ===");
        }
    }
}
