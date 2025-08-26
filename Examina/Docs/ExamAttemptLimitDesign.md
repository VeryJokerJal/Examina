# 考试次数限制数据模型设计文档

## 概述

本文档描述了Examina.Desktop项目中考试次数限制功能的数据模型设计，包括考试尝试记录、次数限制验证等核心实体。

## 核心数据模型

### 1. ExamAttemptDto - 考试尝试记录

考试尝试记录是系统的核心实体，记录每次考试的详细信息。

#### 主要属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| Id | int | 考试尝试唯一标识 |
| ExamId | int | 关联的考试ID |
| StudentId | int | 学生ID |
| AttemptNumber | int | 尝试次数（从1开始） |
| AttemptType | ExamAttemptType | 尝试类型（首次/重考/练习） |
| Status | ExamAttemptStatus | 考试状态 |
| StartedAt | DateTime | 开始时间 |
| CompletedAt | DateTime? | 完成时间 |
| Score | decimal? | 得分 |
| MaxScore | decimal? | 最大得分 |
| DurationSeconds | int? | 考试用时（秒） |
| Notes | string? | 备注信息 |
| IsRanked | bool | 是否参与排名 |

#### 计算属性

- **DurationDisplay**: 格式化的用时显示（HH:MM:SS）
- **ScorePercentageDisplay**: 得分百分比显示
- **StatusDisplay**: 状态显示文本
- **AttemptTypeDisplay**: 尝试类型显示文本

### 2. ExamAttemptType - 考试尝试类型枚举

```csharp
public enum ExamAttemptType
{
    FirstAttempt = 0,  // 首次考试
    Retake = 1,        // 重考（记录分数和排名）
    Practice = 2       // 重做练习（不记录分数和排名）
}
```

### 3. ExamAttemptStatus - 考试状态枚举

```csharp
public enum ExamAttemptStatus
{
    InProgress = 0,    // 进行中
    Completed = 1,     // 已完成
    Abandoned = 2,     // 已放弃
    TimedOut = 3       // 超时
}
```

### 4. ExamAttemptLimitDto - 考试次数限制验证结果

用于验证学生是否可以开始考试，包含权限检查和统计信息。

#### 主要属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| ExamId | int | 考试ID |
| StudentId | int | 学生ID |
| CanStartExam | bool | 是否可以开始考试 |
| CanRetake | bool | 是否可以重考 |
| CanPractice | bool | 是否可以重做练习 |
| TotalAttempts | int | 总尝试次数 |
| RetakeAttempts | int | 重考次数 |
| PracticeAttempts | int | 练习次数 |
| MaxRetakeCount | int | 最大重考次数 |
| AllowRetake | bool | 是否允许重考 |
| AllowPractice | bool | 是否允许重做练习 |
| LimitReason | string? | 限制原因说明 |
| LastAttempt | ExamAttemptDto? | 最后一次考试尝试 |

#### 计算属性

- **RemainingRetakeCount**: 剩余重考次数
- **HasCompletedFirstAttempt**: 是否已完成首次考试
- **StatusDisplay**: 考试状态显示文本
- **AttemptCountDisplay**: 次数统计显示文本

### 5. ExamAttemptStatisticsDto - 考试统计信息

提供考试的整体统计数据，用于管理员查看和分析。

#### 主要属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| ExamId | int | 考试ID |
| TotalParticipants | int | 总参与人数 |
| TotalAttempts | int | 总尝试次数 |
| FirstAttempts | int | 首次尝试次数 |
| RetakeAttempts | int | 重考次数 |
| PracticeAttempts | int | 练习次数 |
| CompletedAttempts | int | 已完成次数 |
| InProgressAttempts | int | 进行中次数 |
| AbandonedAttempts | int | 放弃次数 |
| TimedOutAttempts | int | 超时次数 |
| AverageScore | decimal? | 平均得分 |
| HighestScore | decimal? | 最高得分 |
| LowestScore | decimal? | 最低得分 |
| AverageDurationSeconds | int? | 平均用时（秒） |

#### 计算属性

- **CompletionRate**: 完成率百分比
- **AverageDurationDisplay**: 格式化的平均用时显示

## 业务规则

### 考试次数限制规则

1. **首次考试**：
   - 无特殊限制，学生可以开始首次考试
   - 必须完成首次考试才能进行重考或练习

2. **重考规则**：
   - 必须先完成首次考试
   - 考试必须允许重考（AllowRetake = true）
   - 重考次数不能超过最大限制（MaxRetakeCount）
   - 重考成绩参与排名（IsRanked = true）

3. **练习规则**：
   - 必须先完成首次考试
   - 考试必须允许练习（AllowPractice = true）
   - 练习次数无限制
   - 练习成绩不参与排名（IsRanked = false）

### 状态转换规则

```
InProgress -> Completed  (正常完成)
InProgress -> Abandoned  (主动放弃)
InProgress -> TimedOut   (时间到自动提交)
```

## 数据关系

```
StudentExamDto (1) -> (N) ExamAttemptDto
    |
    +-- AllowRetake: bool
    +-- AllowPractice: bool
    +-- MaxRetakeCount: int

ExamAttemptDto -> ExamAttemptType
ExamAttemptDto -> ExamAttemptStatus
```

## 扩展性考虑

1. **审计日志**：所有状态变更都有时间戳记录
2. **灵活配置**：通过StudentExamDto配置不同考试的规则
3. **统计分析**：提供丰富的统计信息支持数据分析
4. **用户体验**：计算属性提供友好的显示文本

## 实现注意事项

1. **数据一致性**：确保考试尝试记录与考试配置的一致性
2. **并发控制**：防止同一学生同时开始多个考试
3. **性能优化**：合理使用缓存和分页查询
4. **错误处理**：完善的异常处理和用户提示

## 总结

本数据模型设计支持完整的考试次数限制功能，包括：
- 灵活的考试类型配置
- 完整的考试历史记录
- 智能的权限验证
- 丰富的统计分析
- 良好的用户体验

设计遵循MVVM模式，支持数据绑定和属性通知，确保UI的实时更新。
