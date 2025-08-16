using System;
using System.Collections.Generic;
using System.Linq;
using BenchSuite.Models;
using Console = System.Console;

namespace BenchSuite.Services;

/// <summary>
/// ID冲突解决服务 - 检测和修复ExamModel中的ID冲突
/// </summary>
public static class IdConflictResolver
{
    /// <summary>
    /// 解决ExamModel中的所有ID冲突
    /// </summary>
    /// <param name="examModel">要检查的试卷模型</param>
    /// <returns>修复的冲突数量</returns>
    public static int ResolveConflicts(ExamModel examModel)
    {
        if (examModel == null)
            throw new ArgumentNullException(nameof(examModel));

        int conflictCount = 0;
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var idMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. 检查试卷ID
        if (string.IsNullOrWhiteSpace(examModel.Id) || !usedIds.Add(examModel.Id))
        {
            string oldId = examModel.Id;
            string newId = GenerateUniqueId("exam", usedIds);
            LogIdReplacement("Exam", oldId, newId);

            if (!string.IsNullOrWhiteSpace(oldId))
                idMappings[oldId] = newId;

            examModel.Id = newId;
            usedIds.Add(newId);
            conflictCount++;
        }

        // 2. 检查模块ID
        foreach (var module in examModel.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Id) || !usedIds.Add(module.Id))
            {
                string oldId = module.Id;
                string newId = GenerateUniqueId("module", usedIds);
                LogIdReplacement("Module", oldId, newId);

                if (!string.IsNullOrWhiteSpace(oldId))
                    idMappings[oldId] = newId;

                module.Id = newId;
                usedIds.Add(newId);
                conflictCount++;
            }

            // 3. 检查题目ID
            foreach (var question in module.Questions)
            {
                if (string.IsNullOrWhiteSpace(question.Id) || !usedIds.Add(question.Id))
                {
                    string oldId = question.Id;
                    string newId = GenerateUniqueId("question", usedIds);
                    LogIdReplacement("Question", oldId, newId);

                    if (!string.IsNullOrWhiteSpace(oldId))
                        idMappings[oldId] = newId;

                    question.Id = newId;
                    usedIds.Add(newId);
                    conflictCount++;
                }

                // 4. 检查操作点ID
                foreach (var operation in question.OperationPoints)
                {
                    if (string.IsNullOrWhiteSpace(operation.Id) || !usedIds.Add(operation.Id))
                    {
                        string oldId = operation.Id;
                        string newId = GenerateUniqueId("operation", usedIds);
                        LogIdReplacement("OperationPoint", oldId, newId);

                        if (!string.IsNullOrWhiteSpace(oldId))
                            idMappings[oldId] = newId;

                        operation.Id = newId;
                        usedIds.Add(newId);
                        conflictCount++;
                    }
                }
            }
        }

        // 5. 更新所有引用关系
        if (idMappings.Count > 0)
        {
            UpdateReferences(examModel, idMappings);
        }

        return conflictCount;
    }

    /// <summary>
    /// 生成唯一ID
    /// </summary>
    /// <param name="prefix">ID前缀</param>
    /// <param name="usedIds">已使用的ID集合</param>
    /// <returns>唯一ID</returns>
    private static string GenerateUniqueId(string prefix, HashSet<string> usedIds)
    {
        string baseId;
        int counter = 1;

        do
        {
            long timestamp = DateTime.UtcNow.Ticks;
            baseId = $"{prefix}-{timestamp:X}-{counter:X4}";
            counter++;
        }
        while (usedIds.Contains(baseId));

        return baseId;
    }

    /// <summary>
    /// 记录ID替换操作
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="oldId">原ID</param>
    /// <param name="newId">新ID</param>
    private static void LogIdReplacement(string entityType, string oldId, string newId)
    {
        string message = string.IsNullOrWhiteSpace(oldId) 
            ? $"[ID冲突修复] {entityType}: 空ID -> {newId}"
            : $"[ID冲突修复] {entityType}: {oldId} -> {newId}";
        
        Console.WriteLine(message);
        
        // 可以在这里添加更详细的日志记录
        // 例如写入日志文件或发送到日志服务
    }

    /// <summary>
    /// 验证ExamModel中所有ID的唯一性
    /// </summary>
    /// <param name="examModel">要验证的试卷模型</param>
    /// <returns>验证结果</returns>
    public static IdValidationResult ValidateIds(ExamModel examModel)
    {
        if (examModel == null)
            throw new ArgumentNullException(nameof(examModel));

        var result = new IdValidationResult();
        var seenIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // 收集所有ID
        CollectId(seenIds, examModel.Id, "Exam");

        foreach (var module in examModel.Modules)
        {
            CollectId(seenIds, module.Id, $"Module[{module.Name}]");

            foreach (var question in module.Questions)
            {
                CollectId(seenIds, question.Id, $"Question[{question.Title}]");

                foreach (var operation in question.OperationPoints)
                {
                    CollectId(seenIds, operation.Id, $"Operation[{operation.Name}]");
                }
            }
        }

        // 检查重复
        foreach (var kvp in seenIds)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                result.EmptyIds.AddRange(kvp.Value);
            }
            else if (kvp.Value.Count > 1)
            {
                result.DuplicateIds.Add(kvp.Key, kvp.Value);
            }
        }

        result.IsValid = result.EmptyIds.Count == 0 && result.DuplicateIds.Count == 0;
        return result;
    }

    /// <summary>
    /// 更新所有引用关系
    /// </summary>
    /// <param name="examModel">试卷模型</param>
    /// <param name="idMappings">ID映射关系</param>
    private static void UpdateReferences(ExamModel examModel, Dictionary<string, string> idMappings)
    {
        // 目前BenchSuite.Models中的实体没有明显的引用关系字段
        // 但为了扩展性，保留此方法用于将来可能的引用关系更新

        // 例如，如果OperationPoint有引用Question的字段：
        // foreach (var module in examModel.Modules)
        // {
        //     foreach (var question in module.Questions)
        //     {
        //         foreach (var operation in question.OperationPoints)
        //         {
        //             if (!string.IsNullOrWhiteSpace(operation.QuestionId) &&
        //                 idMappings.ContainsKey(operation.QuestionId))
        //             {
        //                 operation.QuestionId = idMappings[operation.QuestionId];
        //             }
        //         }
        //     }
        // }

        // 记录引用更新操作
        if (idMappings.Count > 0)
        {
            Console.WriteLine($"[引用更新] 已更新 {idMappings.Count} 个ID的引用关系");
        }
    }

    /// <summary>
    /// 收集ID到字典中
    /// </summary>
    private static void CollectId(Dictionary<string, List<string>> seenIds, string id, string entityInfo)
    {
        string key = id ?? string.Empty;
        if (!seenIds.ContainsKey(key))
        {
            seenIds[key] = [];
        }
        seenIds[key].Add(entityInfo);
    }
}

/// <summary>
/// ID验证结果
/// </summary>
public class IdValidationResult
{
    /// <summary>
    /// 是否有效（无冲突）
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 重复的ID及其使用位置
    /// </summary>
    public Dictionary<string, List<string>> DuplicateIds { get; set; } = [];

    /// <summary>
    /// 空ID的实体位置
    /// </summary>
    public List<string> EmptyIds { get; set; } = [];

    /// <summary>
    /// 获取验证结果摘要
    /// </summary>
    public string GetSummary()
    {
        if (IsValid)
            return "所有ID验证通过，无冲突";

        var issues = new List<string>();
        
        if (EmptyIds.Count > 0)
            issues.Add($"空ID: {EmptyIds.Count}个");
            
        if (DuplicateIds.Count > 0)
            issues.Add($"重复ID: {DuplicateIds.Count}个");

        return $"发现问题: {string.Join(", ", issues)}";
    }
}
