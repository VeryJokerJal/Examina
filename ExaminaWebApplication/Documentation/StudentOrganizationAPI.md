# 学生组织管理 API 文档

## 概述
学生组织管理API提供学生加入学校组织、查看组织信息等功能。

## 认证
所有API端点都需要JWT Bearer认证，且用户必须具有Student角色。

## API端点

### 1. 加入组织
**POST** `/api/student/organization/join`

#### 请求头
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

#### 请求体
```json
{
  "invitationCode": "ABC1234"
}
```

#### 请求参数
- `invitationCode` (string, required): 7位字母数字组合的邀请码

#### 响应
**成功 (200 OK):**
```json
{
  "success": true,
  "errorMessage": null,
  "organization": {
    "id": 1,
    "name": "示例学校",
    "type": "School",
    "description": "这是一所示例学校"
  },
  "studentOrganization": {
    "id": 123,
    "joinedAt": "2024-01-15T10:30:00Z",
    "isActive": true,
    "role": "Student"
  }
}
```

**失败 (400 Bad Request):**
```json
{
  "success": false,
  "errorMessage": "邀请码无效",
  "organization": null,
  "studentOrganization": null
}
```

#### 错误情况
- `400 Bad Request`: 邀请码无效、已加入其他学校、邀请码过期等
- `401 Unauthorized`: 用户身份验证失败
- `500 Internal Server Error`: 系统错误

### 2. 获取我的组织列表
**GET** `/api/student/organization/my-organizations`

#### 请求头
```
Authorization: Bearer {jwt_token}
```

#### 响应
**成功 (200 OK):**
```json
[
  {
    "id": 123,
    "userId": 456,
    "organizationId": 1,
    "organizationName": "示例学校",
    "organizationType": "School",
    "organizationDescription": "这是一所示例学校",
    "joinedAt": "2024-01-15T10:30:00Z",
    "isActive": true,
    "role": "Student"
  }
]
```

### 3. 检查学校绑定状态
**GET** `/api/student/organization/school-status`

#### 请求头
```
Authorization: Bearer {jwt_token}
```

#### 响应
**成功 (200 OK):**
```json
{
  "isSchoolBound": true,
  "currentSchool": "示例学校",
  "schoolId": 1,
  "joinedAt": "2024-01-15T10:30:00Z"
}
```

## 业务规则

### 加入组织规则
1. 学生只能加入一个学校组织
2. 邀请码必须是7位字母数字组合
3. 邀请码必须有效且未过期
4. 邀请码不能超过使用上限
5. 组织必须处于激活状态
6. 学生不能重复加入同一个组织

### 邀请码验证
- 格式：7位字母数字组合 (例如: ABC1234)
- 状态：必须是激活状态
- 过期时间：不能超过设定的过期时间
- 使用次数：不能超过最大使用次数限制

## 错误代码说明

| 错误消息 | 说明 |
|---------|------|
| "用户身份验证失败" | JWT token无效或用户不存在 |
| "用户不存在或权限不足" | 用户不是学生角色或账户被禁用 |
| "您已经加入了其他学校，无法重复加入" | 学生已经加入了其他学校组织 |
| "邀请码无效" | 邀请码不存在或格式错误 |
| "邀请码已过期或已达到使用上限" | 邀请码不可用 |
| "组织不存在或已停用" | 邀请码对应的组织无效 |
| "您已经在该组织中" | 学生已经在目标组织中 |
| "系统错误，请稍后重试" | 服务器内部错误 |

## 使用示例

### C# HttpClient 示例
```csharp
// 加入组织
var request = new JoinOrganizationRequest { InvitationCode = "ABC1234" };
var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await httpClient.PostAsync("/api/student/organization/join", content);
var result = await response.Content.ReadFromJsonAsync<JoinOrganizationResponse>();
```

### JavaScript Fetch 示例
```javascript
// 加入组织
const response = await fetch('/api/student/organization/join', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    invitationCode: 'ABC1234'
  })
});

const result = await response.json();
```
