# Entity Framework Core 性能优化修复

## 概述

本文档记录了对 Examina Web 应用程序中 Entity Framework Core 相关性能问题的修复，主要解决了查询分割行为警告和 MySQL 执行策略事务冲突问题。

## 修复的问题

### 1. 查询分割行为警告 (QuerySplittingBehavior Warning)

#### 问题描述
```
warn: Microsoft.EntityFrameworkCore.Query[20504]
Compiling a query which loads related collections for more than one collection navigation, either via 'Include' or through projection, but no 'QuerySplittingBehavior' has been configured.
```

#### 问题原因
当使用多个 `Include` 语句加载相关集合时，Entity Framework Core 默认使用 `SingleQuery` 模式，这可能导致：
- 生成复杂的 JOIN 查询
- 数据重复传输（笛卡尔积问题）
- 查询性能下降

#### 解决方案
在涉及多个集合导航的查询中添加 `.AsSplitQuery()` 方法：

**修复的文件：**
1. `ExaminaWebApplication/Services/ImportedSpecializedTraining/SpecializedTrainingImportService.cs`
2. `ExaminaWebApplication/Services/ImportedComprehensiveTraining/ComprehensiveTrainingImportService.cs`

**修复示例：**
```csharp
// 修复前
return await _context.ImportedSpecializedTrainings
    .Where(st => st.ImportedBy == userId)
    .Include(st => st.Modules)
    .Include(st => st.Questions)
    .OrderByDescending(st => st.ImportedAt)
    .ToListAsync();

// 修复后
return await _context.ImportedSpecializedTrainings
    .Where(st => st.ImportedBy == userId)
    .Include(st => st.Modules)
    .Include(st => st.Questions)
    .AsSplitQuery() // 使用分割查询提升性能
    .OrderByDescending(st => st.ImportedAt)
    .ToListAsync();
```

### 2. MySQL 执行策略事务冲突

#### 问题描述
```
fail: Microsoft.EntityFrameworkCore.Update[10000]
System.InvalidOperationException: The configured execution strategy 'MySqlRetryingExecutionStrategy' does not support user-initiated transactions.
```

#### 问题原因
MySQL 的重试执行策略 (`MySqlRetryingExecutionStrategy`) 与用户手动启动的事务冲突。当启用重试机制时，EF Core 需要控制整个事务的生命周期。

#### 解决方案
使用 `DbContext.Database.CreateExecutionStrategy()` 创建执行策略来包装事务操作：

**修复的文件：**
- `ExaminaWebApplication/Services/ImportedSpecializedTraining/SpecializedTrainingImportService.cs`

**修复示例：**
```csharp
// 修复前
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    _context.ImportedSpecializedTrainings.Add(importedSpecializedTraining);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    throw;
}

// 修复后
var strategy = _context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        _context.ImportedSpecializedTrainings.Add(importedSpecializedTraining);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw; // 重新抛出异常以便执行策略处理
    }
});
```

## 性能优化效果

### 查询分割的优势

#### 1. 减少数据传输
- **单查询模式**：可能产生大量重复数据（笛卡尔积）
- **分割查询模式**：每个集合使用独立查询，减少数据重复

#### 2. 提升查询性能
- 避免复杂的多表 JOIN 操作
- 减少数据库服务器的内存使用
- 提高查询执行效率

#### 3. 更好的可维护性
- 查询逻辑更清晰
- 更容易调试和优化
- 减少意外的性能问题

### 执行策略的优势

#### 1. 事务可靠性
- 支持自动重试机制
- 处理临时网络问题
- 提高数据一致性

#### 2. 错误恢复
- 自动处理连接中断
- 智能重试策略
- 减少因网络问题导致的失败

## 配置说明

### 数据库配置
在 `Program.cs` 中的 MySQL 配置保持不变：

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    _ = options.UseMySql(
        connectionString,
        ServerVersion.Parse("8.0.0-mysql"),
        mysqlOptions =>
        {
            _ = mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }
    );
    
    // 注意：全局配置可能影响所有查询，建议在具体查询中使用 AsSplitQuery()
});
```

### 查询优化建议

#### 1. 何时使用分割查询
- 查询包含多个 `Include` 语句
- 涉及多个一对多关系
- 查询结果集较大
- 性能测试显示单查询模式较慢

#### 2. 何时避免分割查询
- 只有一个 `Include` 语句
- 结果集较小
- 需要严格的数据一致性（在同一事务中）

#### 3. 性能监控
建议在生产环境中监控以下指标：
- 查询执行时间
- 数据库连接数
- 内存使用情况
- 网络传输量

## 最佳实践

### 1. 查询优化
```csharp
// 推荐：明确指定分割查询
var result = await context.Entities
    .Include(e => e.Collection1)
    .Include(e => e.Collection2)
    .AsSplitQuery()
    .ToListAsync();

// 推荐：对于简单查询使用单查询
var result = await context.Entities
    .Include(e => e.SingleNavigation)
    .ToListAsync();
```

### 2. 事务处理
```csharp
// 推荐：使用执行策略包装事务
var strategy = context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    using var transaction = await context.Database.BeginTransactionAsync();
    try
    {
        // 数据库操作
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
});
```

### 3. 错误处理
```csharp
// 推荐：详细的错误日志
try
{
    await strategy.ExecuteAsync(async () => { /* 操作 */ });
}
catch (Exception ex)
{
    _logger.LogError(ex, "数据库操作失败：{Operation}", "ImportSpecializedTraining");
    throw;
}
```

## 测试验证

### 1. 功能测试
- ✅ 专项训练导入功能正常
- ✅ 综合训练查询功能正常
- ✅ 事务回滚机制正常
- ✅ 错误处理机制正常

### 2. 性能测试
建议进行以下测试：
- 大数据量导入测试
- 并发查询测试
- 网络中断恢复测试
- 内存使用监控

## 总结

通过这些修复，我们解决了：
1. **查询性能警告**：使用 `AsSplitQuery()` 优化多集合查询
2. **事务冲突问题**：使用执行策略正确处理 MySQL 重试机制
3. **代码可维护性**：提供清晰的错误处理和日志记录

这些优化提升了应用程序的性能、稳定性和可维护性，为用户提供更好的体验。
