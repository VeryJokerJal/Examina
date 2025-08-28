# 专项训练开始功能API文档

## 概述

本文档描述了ExaminaWebApplication项目中新实现的专项训练开始和完成功能的API端点。这些端点允许学生端应用程序标记专项训练的开始和完成状态。

## API端点

### 1. 开始专项训练

**端点：** `POST /api/student/specialized-trainings/{trainingId}/start`

**描述：** 标记指定的专项训练为开始状态

**权限：** 需要学生角色认证

**路径参数：**
- `trainingId` (int): 专项训练ID

**请求体：** 无

**响应：**

**成功响应 (200 OK):**
```json
{
  "message": "专项训练开始成功"
}
```

**失败响应 (400 Bad Request):**
```json
{
  "message": "开始专项训练失败，请检查训练是否存在或您是否有权限访问"
}
```

**服务器错误 (500 Internal Server Error):**
```json
{
  "message": "开始专项训练失败",
  "error": "具体错误信息"
}
```

### 2. 完成专项训练

**端点：** `POST /api/student/specialized-trainings/{trainingId}/complete`

**描述：** 标记指定的专项训练为完成状态，并记录评分信息

**权限：** 需要学生角色认证

**路径参数：**
- `trainingId` (int): 专项训练ID

**请求体：**
```json
{
  "score": 85.5,
  "maxScore": 100.0,
  "durationSeconds": 1800,
  "notes": "训练完成备注",
  "benchSuiteScoringResult": "BenchSuite评分结果JSON",
  "completedAt": "2025-08-23T10:30:00Z"
}
```

**请求体字段说明：**
- `score` (double?, 可选): 获得的分数
- `maxScore` (double?, 可选): 最大可能分数
- `durationSeconds` (int?, 可选): 训练用时（秒）
- `notes` (string?, 可选): 备注信息
- `benchSuiteScoringResult` (string?, 可选): BenchSuite评分结果JSON
- `completedAt` (DateTime?, 可选): 完成时间（UTC）

**响应：**

**成功响应 (200 OK):**
```json
{
  "message": "专项训练完成成功"
}
```

**失败响应 (400 Bad Request):**
```json
{
  "message": "完成专项训练失败，请检查训练是否存在或您是否有权限访问"
}
```

**服务器错误 (500 Internal Server Error):**
```json
{
  "message": "完成专项训练失败",
  "error": "具体错误信息"
}
```

## 业务逻辑

### 开始训练逻辑

1. **用户验证：** 验证当前用户是否为活跃的学生用户
2. **训练验证：** 验证专项训练是否存在且已启用
3. **状态检查：** 检查是否已有完成记录
   - 如果已完成，不允许重新开始
   - 如果已有记录但未完成，更新为进行中状态
   - 如果没有记录，创建新的开始记录
4. **数据库更新：** 保存状态变更

### 完成训练逻辑

1. **用户验证：** 验证当前用户是否为活跃的学生用户
2. **训练验证：** 验证专项训练是否存在且已启用
3. **记录处理：**
   - 如果已有记录，更新为完成状态并记录评分信息
   - 如果没有记录，创建新的完成记录
4. **百分比计算：** 如果提供了分数和最大分数，自动计算完成百分比
5. **数据库更新：** 保存完成信息

## 数据模型

### SpecialPracticeCompletion

专项训练完成记录使用 `SpecialPracticeCompletion` 模型存储：

```csharp
public class SpecialPracticeCompletion
{
    public int Id { get; set; }
    public int StudentUserId { get; set; }
    public int PracticeId { get; set; }  // 对应专项训练ID
    public SpecialPracticeCompletionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? Score { get; set; }
    public double? MaxScore { get; set; }
    public double? CompletionPercentage { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 状态枚举

```csharp
public enum SpecialPracticeCompletionStatus
{
    NotStarted = 0,    // 未开始
    InProgress = 1,    // 进行中
    Completed = 2,     // 已完成
    Abandoned = 3,     // 已放弃
    Timeout = 4        // 超时
}
```

## 错误处理

### 常见错误情况

1. **401 Unauthorized：** 用户未认证或认证信息无效
2. **400 Bad Request：** 
   - 训练不存在或未启用
   - 用户无权限访问该训练
   - 训练已完成（尝试重新开始时）
3. **500 Internal Server Error：** 服务器内部错误，如数据库连接失败

### 日志记录

所有API调用都会记录详细的日志信息，包括：
- 用户ID和训练ID
- 操作结果（成功/失败）
- 错误信息（如果有）
- 评分信息（完成时）

## 与客户端集成

### Examina.Desktop集成

这些API端点与Examina.Desktop项目中的以下方法兼容：

- `StudentSpecializedTrainingService.StartSpecializedTrainingAsync(int trainingId)`
- `StudentSpecializedTrainingService.CompleteSpecializedTrainingAsync(int trainingId, double? score, double? maxScore, int? durationSeconds, string? notes)`

### 调用示例

```csharp
// 开始训练
bool success = await _studentSpecializedTrainingService.StartSpecializedTrainingAsync(trainingId);

// 完成训练
bool success = await _studentSpecializedTrainingService.CompleteSpecializedTrainingAsync(
    trainingId, 85.5m, 100m, 1800, "训练完成");
```

## 安全考虑

1. **认证：** 所有端点都需要有效的JWT认证
2. **授权：** 只有学生角色可以访问这些端点
3. **数据验证：** 服务器端验证所有输入参数
4. **权限检查：** 确保学生只能操作自己有权限的训练

## 性能考虑

1. **数据库索引：** 在 `StudentUserId` 和 `PracticeId` 字段上建立索引
2. **事务处理：** 使用数据库事务确保数据一致性
3. **错误恢复：** 实现适当的错误处理和重试机制

## 版本信息

- **API版本：** v1.0
- **实现日期：** 2025-08-23
- **兼容性：** 与现有专项训练功能完全兼容
