# 学生端专项训练API文档

## 概述

学生端专项训练API提供了学生获取和访问专项训练（专项试卷）的功能。该API遵循RESTful设计原则，支持分页查询、权限验证和多种筛选方式。

## 基础信息

- **基础路径**: `/api/student/specialized-trainings`
- **认证方式**: JWT Bearer Token
- **权限要求**: 学生角色 (Student)
- **响应格式**: JSON

## API端点

### 1. 获取专项训练列表

**GET** `/api/student/specialized-trainings`

获取学生可访问的专项训练列表，支持分页查询。

#### 请求参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageNumber | int | 否 | 1 | 页码，最小值为1 |
| pageSize | int | 否 | 50 | 页大小，范围1-100 |

#### 响应示例

```json
[
  {
    "id": 1,
    "name": "Windows 基础操作专项训练",
    "description": "针对 Windows 操作系统基础操作的专项训练",
    "moduleType": "Windows",
    "totalScore": 100,
    "duration": 60,
    "difficultyLevel": 2,
    "randomizeQuestions": false,
    "tags": "Windows,基础操作,文件管理",
    "originalCreatedTime": "2024-01-15T10:30:00Z",
    "originalLastModifiedTime": "2024-01-20T14:45:00Z",
    "importedAt": "2024-08-20T08:00:00Z",
    "moduleCount": 2,
    "questionCount": 10,
    "modules": [],
    "questions": []
  }
]
```

### 2. 获取专项训练详情

**GET** `/api/student/specialized-trainings/{trainingId}`

获取指定专项训练的详细信息，包含模块和题目信息。

#### 路径参数

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| trainingId | int | 是 | 专项训练ID |

#### 响应示例

```json
{
  "id": 1,
  "name": "Windows 基础操作专项训练",
  "description": "针对 Windows 操作系统基础操作的专项训练",
  "moduleType": "Windows",
  "totalScore": 100,
  "duration": 60,
  "difficultyLevel": 2,
  "randomizeQuestions": false,
  "tags": "Windows,基础操作,文件管理",
  "originalCreatedTime": "2024-01-15T10:30:00Z",
  "originalLastModifiedTime": "2024-01-20T14:45:00Z",
  "importedAt": "2024-08-20T08:00:00Z",
  "moduleCount": 2,
  "questionCount": 10,
  "modules": [
    {
      "id": 1,
      "name": "文件管理模块",
      "type": "Windows",
      "description": "Windows 文件和文件夹管理操作",
      "score": 50,
      "order": 1,
      "questions": [...]
    }
  ],
  "questions": [
    {
      "id": 1,
      "title": "创建文件夹结构",
      "content": "在桌面上创建一个名为'项目文档'的文件夹",
      "questionType": "Practical",
      "score": 15.0,
      "difficultyLevel": 1,
      "estimatedMinutes": 5,
      "order": 1,
      "isRequired": true,
      "operationPoints": [...]
    }
  ]
}
```

### 3. 检查访问权限

**GET** `/api/student/specialized-trainings/{trainingId}/access`

检查学生是否有权限访问指定的专项训练。

#### 路径参数

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| trainingId | int | 是 | 专项训练ID |

#### 响应示例

```json
true
```

### 4. 获取专项训练总数

**GET** `/api/student/specialized-trainings/count`

获取学生可访问的专项训练总数。

#### 响应示例

```json
25
```

### 5. 按模块类型筛选

**GET** `/api/student/specialized-trainings/by-module-type/{moduleType}`

根据模块类型获取专项训练列表。

#### 路径参数

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| moduleType | string | 是 | 模块类型（如：Windows、Office、Programming等） |

#### 请求参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageNumber | int | 否 | 1 | 页码，最小值为1 |
| pageSize | int | 否 | 50 | 页大小，范围1-100 |

### 6. 按难度等级筛选

**GET** `/api/student/specialized-trainings/by-difficulty/{difficultyLevel}`

根据难度等级获取专项训练列表。

#### 路径参数

| 参数名 | 类型 | 必填 | 说明 |
|--------|------|------|------|
| difficultyLevel | int | 是 | 难度等级（1-5） |

#### 请求参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| pageNumber | int | 否 | 1 | 页码，最小值为1 |
| pageSize | int | 否 | 50 | 页大小，范围1-100 |

### 7. 搜索专项训练

**GET** `/api/student/specialized-trainings/search`

根据关键词搜索专项训练。

#### 请求参数

| 参数名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| keyword | string | 是 | - | 搜索关键词 |
| pageNumber | int | 否 | 1 | 页码，最小值为1 |
| pageSize | int | 否 | 50 | 页大小，范围1-100 |

### 8. 获取模块类型列表

**GET** `/api/student/specialized-trainings/module-types`

获取所有可用的模块类型列表。

#### 响应示例

```json
[
  "Windows",
  "Office",
  "Programming",
  "Database"
]
```

## 错误响应

所有API端点在发生错误时都会返回统一的错误格式：

```json
{
  "message": "错误描述",
  "error": "详细错误信息（仅开发环境）"
}
```

### 常见HTTP状态码

- **200 OK**: 请求成功
- **400 Bad Request**: 请求参数错误
- **401 Unauthorized**: 未认证或认证失败
- **403 Forbidden**: 权限不足
- **404 Not Found**: 资源不存在
- **500 Internal Server Error**: 服务器内部错误

## 使用示例

### JavaScript/Fetch示例

```javascript
// 获取专项训练列表
const response = await fetch('/api/student/specialized-trainings?pageNumber=1&pageSize=20', {
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  }
});
const trainings = await response.json();

// 获取专项训练详情
const detailResponse = await fetch('/api/student/specialized-trainings/1', {
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  }
});
const trainingDetail = await detailResponse.json();

// 搜索专项训练
const searchResponse = await fetch('/api/student/specialized-trainings/search?keyword=Windows&pageNumber=1', {
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  }
});
const searchResults = await searchResponse.json();
```

## 注意事项

1. **权限验证**: 所有API都需要学生角色的JWT认证
2. **分页限制**: 页大小最大为100，超出会自动调整为50
3. **数据权限**: 目前所有启用的专项训练对学生可见，后续可能会根据组织关系进行权限控制
4. **缓存策略**: 建议客户端对模块类型列表等相对静态的数据进行适当缓存
5. **错误处理**: 建议客户端实现适当的错误处理和重试机制

## 更新日志

- **v1.0.0** (2024-08-20): 初始版本，实现基础的专项训练查询功能
