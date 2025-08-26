# 考试次数验证服务实现文档

## 概述

本文档描述了Examina.Desktop项目中考试次数验证服务（ExamAttemptService）的实现细节，包括接口定义、核心逻辑和使用方法。

## 服务接口 - IExamAttemptService

### 核心方法

#### 1. 考试权限验证
```csharp
Task<ExamAttemptLimitDto> CheckExamAttemptLimitAsync(int examId, int studentId);
```
- **功能**：检查学生是否可以开始考试
- **返回**：包含权限检查结果和统计信息的DTO
- **核心逻辑**：
  - 验证考试是否存在
  - 统计各类型尝试次数
  - 检查重考和练习权限
  - 返回详细的限制信息

#### 2. 考试尝试管理
```csharp
Task<ExamAttemptDto?> StartExamAttemptAsync(int examId, int studentId, ExamAttemptType attemptType);
Task<bool> CompleteExamAttemptAsync(int attemptId, decimal? score, decimal? maxScore, int? durationSeconds, string? notes);
Task<bool> AbandonExamAttemptAsync(int attemptId, string? reason);
Task<bool> TimeoutExamAttemptAsync(int attemptId, decimal? score, decimal? maxScore, int? durationSeconds);
```

#### 3. 历史记录查询
```csharp
Task<List<ExamAttemptDto>> GetExamAttemptHistoryAsync(int examId, int studentId);
Task<List<ExamAttemptDto>> GetStudentExamAttemptHistoryAsync(int studentId, int pageNumber, int pageSize);
Task<ExamAttemptDto?> GetCurrentExamAttemptAsync(int studentId);
```

#### 4. 统计分析
```csharp
Task<ExamAttemptStatisticsDto> GetExamAttemptStatisticsAsync(int examId);
```

## 服务实现 - ExamAttemptService

### 依赖注入
```csharp
public ExamAttemptService(
    IStudentExamService studentExamService,
    IConfigurationService configurationService)
```

### 核心验证逻辑

#### 考试权限检查流程
1. **获取考试详情**：验证考试是否存在和可访问
2. **统计尝试次数**：
   - 总尝试次数
   - 重考次数
   - 练习次数
3. **检查活跃考试**：确保没有进行中的考试
4. **权限验证**：
   - 首次考试：无限制条件
   - 重考：需完成首次考试且未超过最大次数
   - 练习：需完成首次考试且允许练习

#### 权限验证代码示例
```csharp
// 检查是否已完成首次考试
bool hasCompletedFirstAttempt = attempts.Any(a => 
    a.AttemptType == ExamAttemptType.FirstAttempt && 
    a.Status == ExamAttemptStatus.Completed);

// 检查重考权限
if (exam.AllowRetake && retakeAttempts < exam.MaxRetakeCount)
{
    canRetake = hasCompletedFirstAttempt;
}

// 检查练习权限
if (exam.AllowPractice)
{
    canPractice = hasCompletedFirstAttempt;
}
```

### 状态管理

#### 考试尝试状态转换
```
创建 -> InProgress -> Completed/Abandoned/TimedOut
```

#### 状态更新方法
- **CompleteExamAttemptAsync**：正常完成考试
- **AbandonExamAttemptAsync**：学生主动放弃
- **TimeoutExamAttemptAsync**：时间到自动提交

### 数据存储

当前实现使用内存存储（模拟）：
```csharp
private readonly List<ExamAttemptDto> _examAttempts;
private int _nextAttemptId = 1;
```

**注意**：生产环境中应替换为数据库存储。

## 服务注册

在 `ServiceCollectionExtensions.cs` 中注册：
```csharp
services.AddSingleton<IExamAttemptService, ExamAttemptService>();
```

## 使用示例

### 1. 检查考试权限
```csharp
ExamAttemptLimitDto limit = await _examAttemptService.CheckExamAttemptLimitAsync(examId, studentId);

if (limit.CanStartExam)
{
    if (!limit.HasCompletedFirstAttempt)
    {
        // 可以开始首次考试
    }
    else if (limit.CanRetake)
    {
        // 可以重考
    }
    else if (limit.CanPractice)
    {
        // 可以练习
    }
}
else
{
    // 显示限制原因
    Console.WriteLine(limit.LimitReason);
}
```

### 2. 开始考试
```csharp
// 验证权限
(bool isValid, string? errorMessage) = await _examAttemptService
    .ValidateExamAttemptPermissionAsync(examId, studentId, ExamAttemptType.FirstAttempt);

if (isValid)
{
    // 开始考试
    ExamAttemptDto? attempt = await _examAttemptService
        .StartExamAttemptAsync(examId, studentId, ExamAttemptType.FirstAttempt);
    
    if (attempt != null)
    {
        // 考试开始成功
        Console.WriteLine($"考试开始，ID: {attempt.Id}");
    }
}
```

### 3. 完成考试
```csharp
bool success = await _examAttemptService.CompleteExamAttemptAsync(
    attemptId, 
    score: 85, 
    maxScore: 100, 
    durationSeconds: 3600, 
    notes: "正常完成"
);
```

## 错误处理

### 常见错误场景
1. **考试不存在**：返回错误信息
2. **用户无权限**：返回限制原因
3. **重复开始考试**：检查活跃考试状态
4. **超过重考次数**：返回次数限制信息

### 异常处理模式
```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    return new ExamAttemptLimitDto
    {
        CanStartExam = false,
        LimitReason = $"检查考试权限时发生错误: {ex.Message}"
    };
}
```

## 性能考虑

### 优化建议
1. **缓存考试配置**：减少重复查询
2. **分页查询**：历史记录使用分页
3. **异步操作**：所有方法都是异步的
4. **批量操作**：支持批量查询和更新

### 并发控制
- 检查活跃考试状态防止重复开始
- 使用原子操作更新考试状态

## 扩展性

### 未来扩展点
1. **数据库集成**：替换内存存储
2. **缓存层**：添加Redis缓存
3. **事件通知**：考试状态变化事件
4. **审计日志**：记录所有操作
5. **权限插件**：可插拔的权限验证

### 配置化支持
通过 `IConfigurationService` 支持：
- 默认重考次数限制
- 考试超时设置
- 权限验证规则

## 测试支持

提供了完整的测试类：
- `ExamAttemptServiceTest`：功能测试
- `ExamStatusDisplayTest`：状态显示测试

## 总结

ExamAttemptService 提供了完整的考试次数限制功能：
- ✅ 灵活的权限验证
- ✅ 完整的状态管理
- ✅ 丰富的查询接口
- ✅ 良好的错误处理
- ✅ 可扩展的架构设计

服务遵循SOLID原则，支持依赖注入，具有良好的可测试性和可维护性。
