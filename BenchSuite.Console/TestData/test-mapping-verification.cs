using System;
using System.Collections.Generic;

namespace BenchSuite.Console.TestData
{
    /// <summary>
    /// 验证知识点映射的测试类
    /// </summary>
    public static class MappingVerificationTest
    {
        /// <summary>
        /// 测试EL导出的操作名称映射
        /// </summary>
        public static void TestELOperationMapping()
        {
            // 模拟WindowsScoringService中的映射逻辑
            Dictionary<string, string> nameToTypeMapping = new()
            {
                { "创建文件", "CreateFile" },
                { "删除文件", "DeleteFile" },
                { "复制文件", "CopyFile" },
                { "移动文件", "MoveFile" },
                { "重命名文件", "RenameFile" },
                { "创建文件夹", "CreateFolder" },
                { "删除文件夹", "DeleteFolder" },
                { "复制文件夹", "CopyFolder" },
                { "移动文件夹", "MoveFolder" },
                { "重命名文件夹", "RenameFolder" },
                { "设置文件属性", "SetFileAttributes" },
                { "设置文件权限", "SetFilePermissions" },
                { "写入文本到文件", "WriteTextToFile" },
                { "追加文本到文件", "AppendTextToFile" },
                { "创建快捷方式", "CreateShortcut" },
                { "设置环境变量", "SetEnvironmentVariable" },
                { "创建注册表项", "CreateRegistryKey" },
                { "设置注册表值", "SetRegistryValue" },
                { "删除注册表项", "DeleteRegistryKey" },
                { "启动服务", "StartService" },
                { "停止服务", "StopService" },
                { "启动进程", "StartProcess" },
                { "终止进程", "KillProcess" },
                { "Ping主机", "PingHost" },
                { "下载文件", "DownloadFile" },
                { "创建ZIP压缩包", "CreateZipArchive" },
                { "解压ZIP压缩包", "ExtractZipArchive" },
                // 添加EL导出格式的映射支持
                { "删除操作", "DeleteFile" },
                { "复制重命名操作", "CopyAndRename" }
            };

            // 测试EL导出的操作名称
            string[] elOperationNames = { "删除操作", "复制重命名操作" };
            
            Console.WriteLine("=== EL操作名称映射测试 ===");
            
            foreach (string operationName in elOperationNames)
            {
                if (nameToTypeMapping.TryGetValue(operationName, out string? mappedType))
                {
                    Console.WriteLine($"✅ '{operationName}' -> '{mappedType}'");
                }
                else
                {
                    Console.WriteLine($"❌ '{operationName}' -> 未找到映射");
                }
            }

            // 测试支持的知识点类型
            string[] supportedTypes = { "DeleteFile", "CopyAndRename" };
            
            Console.WriteLine("\n=== 支持的知识点类型测试 ===");
            
            foreach (string knowledgeType in supportedTypes)
            {
                bool isSupported = IsSupportedKnowledgeType(knowledgeType);
                Console.WriteLine($"{(isSupported ? "✅" : "❌")} '{knowledgeType}' {(isSupported ? "已支持" : "未支持")}");
            }
        }

        /// <summary>
        /// 检查知识点类型是否被支持
        /// </summary>
        private static bool IsSupportedKnowledgeType(string knowledgePointType)
        {
            // 模拟WindowsScoringService中的switch语句
            return knowledgePointType switch
            {
                "CreateFile" => true,
                "DeleteFile" => true,
                "CopyFile" => true,
                "MoveFile" => true,
                "RenameFile" => true,
                "CreateFolder" => true,
                "DeleteFolder" => true,
                "CopyFolder" => true,
                "MoveFolder" => true,
                "RenameFolder" => true,
                "SetFileAttributes" => true,
                "WriteTextToFile" => true,
                "AppendTextToFile" => true,
                "CreateShortcut" => true,
                "SetEnvironmentVariable" => true,
                "CreateRegistryKey" => true,
                "SetRegistryValue" => true,
                "DeleteRegistryKey" => true,
                "StartService" => true,
                "StopService" => true,
                "StartProcess" => true,
                "KillProcess" => true,
                "PingHost" => true,
                "DownloadFile" => true,
                "CreateZipArchive" => true,
                "ExtractZipArchive" => true,
                "CopyAndRename" => true, // 新添加的类型
                _ => false
            };
        }
    }
}
