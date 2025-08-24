# 模拟考试权限问题排查指南

## 问题描述

当用户提交模拟考试时出现"无权限访问该模拟考试"错误，错误代码400，状态"Unauthorized"。

## 常见原因

1. **用户ID不匹配**：模拟考试的StudentId与当前用户ID不一致
2. **用户角色错误**：用户不是Student角色
3. **用户账号未激活**：用户的IsActive状态为false
4. **模拟考试不存在**：指定的模拟考试ID在数据库中不存在
5. **JWT令牌问题**：令牌过期或无效导致无法正确获取用户ID

## 诊断步骤

### 1. 使用诊断API

#### 检查模拟考试详细信息
```http
GET /api/student/mock-exams/{id}/diagnose
Authorization: Bearer {your-jwt-token}
```

这个API会返回详细的诊断信息，包括：
- 当前用户信息
- 模拟考试信息
- 权限检查结果
- 发现的问题列表

#### 获取用户的所有模拟考试
```http
GET /api/MockExamDiagnostic/my-mock-exams
Authorization: Bearer {your-jwt-token}
```

#### 获取特定模拟考试的详细信息
```http
GET /api/MockExamDiagnostic/mock-exam/{id}/details
Authorization: Bearer {your-jwt-token}
```

### 2. 检查日志

查看应用程序日志中的以下信息：
- `开始检查模拟考试访问权限`
- `权限验证失败`
- `用户验证通过`
- `模拟考试存在`

### 3. 数据库检查

#### 检查用户信息
```sql
SELECT Id, Username, Email, Role, IsActive, CreatedAt 
FROM Users 
WHERE Id = {user_id};
```

#### 检查模拟考试信息
```sql
SELECT Id, StudentId, Name, Status, CreatedAt, StartedAt, CompletedAt 
FROM MockExams 
WHERE Id = {mock_exam_id};
```

#### 检查用户与模拟考试的关联
```sql
SELECT u.Id as UserId, u.Username, u.Role, u.IsActive,
       me.Id as MockExamId, me.StudentId, me.Name, me.Status
FROM Users u
LEFT JOIN MockExams me ON u.Id = me.StudentId
WHERE u.Id = {user_id} AND me.Id = {mock_exam_id};
```

## 修复方法

### 1. 自动修复（开发环境）

使用修复API（仅限开发环境）：

#### 预览修复操作
```http
POST /api/MockExamDiagnostic/mock-exam/{id}/fix-permission?dryRun=true
Authorization: Bearer {your-jwt-token}
```

#### 执行修复操作
```http
POST /api/MockExamDiagnostic/mock-exam/{id}/fix-permission?dryRun=false
Authorization: Bearer {your-jwt-token}
```

### 2. 手动修复

#### 修复用户角色问题
```sql
UPDATE Users 
SET Role = 'Student', IsActive = 1 
WHERE Id = {user_id};
```

#### 修复模拟考试归属问题
```sql
UPDATE MockExams 
SET StudentId = {correct_user_id} 
WHERE Id = {mock_exam_id};
```

### 3. 预防措施

1. **在模拟考试创建时添加验证**
   - 确保用户ID正确传递
   - 验证用户角色和状态

2. **在提交前进行预检查**
   - 客户端已添加权限预检查
   - 提供更友好的错误信息

3. **增强日志记录**
   - 记录详细的权限验证过程
   - 便于问题排查

## 代码改进

### 已实现的改进

1. **增强日志记录**
   - `HasAccessToMockExamAsync`方法添加详细日志
   - `SubmitMockExamAsync`方法添加权限验证日志

2. **权限预检查**
   - 客户端在提交前检查权限
   - 提供详细的诊断信息

3. **诊断API**
   - `/api/student/mock-exams/{id}/diagnose`
   - `/api/MockExamDiagnostic/my-mock-exams`
   - `/api/MockExamDiagnostic/mock-exam/{id}/details`

4. **修复工具**
   - 自动检测和修复常见权限问题
   - 支持预览模式

### 建议的后续改进

1. **权限缓存**
   - 缓存用户权限信息
   - 减少数据库查询

2. **权限恢复机制**
   - 自动检测和修复权限问题
   - 提供用户友好的错误处理

3. **监控和告警**
   - 监控权限验证失败率
   - 自动告警异常情况

## 使用示例

### 排查考试ID 60的权限问题

1. **获取诊断信息**
```bash
curl -X GET "https://your-api-domain/api/student/mock-exams/60/diagnose" \
     -H "Authorization: Bearer your-jwt-token"
```

2. **检查用户的所有模拟考试**
```bash
curl -X GET "https://your-api-domain/api/MockExamDiagnostic/my-mock-exams" \
     -H "Authorization: Bearer your-jwt-token"
```

3. **如果需要修复（开发环境）**
```bash
curl -X POST "https://your-api-domain/api/MockExamDiagnostic/mock-exam/60/fix-permission?dryRun=false" \
     -H "Authorization: Bearer your-jwt-token"
```

## 注意事项

1. **生产环境安全**
   - 修复API仅在开发环境使用
   - 生产环境需要手动修复

2. **数据一致性**
   - 修复前备份相关数据
   - 确保修复操作的原子性

3. **用户体验**
   - 提供清晰的错误信息
   - 指导用户如何解决问题
