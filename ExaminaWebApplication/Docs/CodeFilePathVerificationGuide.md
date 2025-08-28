# CodeFilePath 字段验证指南

## 📋 概述

本指南提供了完整的步骤来验证 ExaminaWebApplication (EW) 项目中 CodeFilePath 和 DocumentFilePath 字段是否正确映射和返回。

## 🔍 验证步骤

### 1. 数据库数据验证

#### 1.1 检查数据库统计
```http
GET /api/debug/codefilepath/summary
```

**预期响应**：
```json
{
  "codeFilePath": {
    "comprehensiveTraining": 150,
    "specializedTraining": 75,
    "formalExam": 200,
    "total": 425
  },
  "documentFilePath": {
    "comprehensiveTraining": 100,
    "specializedTraining": 50,
    "formalExam": 120,
    "total": 270
  }
}
```

#### 1.2 检查具体数据类型
```http
GET /api/debug/codefilepath/comprehensive-training
GET /api/debug/codefilepath/specialized-training
GET /api/debug/codefilepath/formal-exam
```

### 2. API 响应验证

#### 2.1 模拟考试 API 验证
```http
GET /api/student-mock-exam/{mockExamId}
```

**关键检查点**：
- 响应 JSON 中是否包含 `codeFilePath` 字段
- 响应 JSON 中是否包含 `documentFilePath` 字段
- 字段值是否为有效的文件路径

**示例响应片段**：
```json
{
  "id": 1,
  "name": "模拟考试",
  "questions": [
    {
      "originalQuestionId": 123,
      "title": "C# 编程题",
      "codeFilePath": "C:\\Code\\Program.cs",
      "documentFilePath": "C:\\Documents\\Document.docx",
      "programInput": "test input",
      "expectedOutput": "test output"
    }
  ]
}
```

#### 2.2 综合实训 API 验证
```http
GET /api/student-comprehensive-training/{trainingId}
```

#### 2.3 专项训练 API 验证
```http
GET /api/student-specialized-training/{trainingId}
```

### 3. 调试 API 验证

#### 3.1 使用调试控制器验证响应
```http
GET /api/debug/response/mock-exam/{mockExamId}
GET /api/debug/response/comprehensive-training/{trainingId}
GET /api/debug/response/specialized-training/{trainingId}
```

**预期响应包含**：
- `codeFilePathAnalysis` - 分析 CodeFilePath 字段的统计
- `sampleQuestions` - 包含 CodeFilePath 字段的示例题目
- `rawJsonSample` - 完整的 JSON 序列化示例

#### 3.2 数据流跟踪验证
```http
GET /api/debug/tracking/mock-exam/{mockExamId}
GET /api/debug/tracking/comprehensive-training/{trainingId}
```

### 4. 浏览器开发者工具验证

#### 4.1 Network 面板检查
1. 打开浏览器开发者工具 (F12)
2. 切换到 Network 面板
3. 访问相关页面触发 API 调用
4. 查看 API 响应的 JSON 数据
5. 搜索 `codeFilePath` 和 `documentFilePath` 字段

#### 4.2 Console 面板验证
```javascript
// 在浏览器控制台中执行
fetch('/api/student-mock-exam/1')
  .then(response => response.json())
  .then(data => {
    console.log('API Response:', data);
    
    // 检查 CodeFilePath 字段
    const questionsWithCodeFilePath = data.questions?.filter(q => q.codeFilePath) || [];
    console.log('Questions with CodeFilePath:', questionsWithCodeFilePath.length);
    
    // 显示示例
    if (questionsWithCodeFilePath.length > 0) {
      console.log('Sample CodeFilePath:', questionsWithCodeFilePath[0].codeFilePath);
    }
  });
```

### 5. Postman 验证

#### 5.1 创建 Postman Collection
```json
{
  "info": {
    "name": "CodeFilePath Verification",
    "description": "验证 CodeFilePath 字段映射"
  },
  "item": [
    {
      "name": "Mock Exam Details",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/student-mock-exam/1"
      },
      "event": [
        {
          "listen": "test",
          "script": {
            "exec": [
              "pm.test('Response has CodeFilePath field', function () {",
              "    const jsonData = pm.response.json();",
              "    const hasCodeFilePath = jsonData.questions?.some(q => q.codeFilePath);",
              "    pm.expect(hasCodeFilePath).to.be.true;",
              "});"
            ]
          }
        }
      ]
    }
  ]
}
```

#### 5.2 验证脚本
```javascript
// Postman Tests 脚本
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains questions", function () {
    const jsonData = pm.response.json();
    pm.expect(jsonData.questions).to.be.an('array');
});

pm.test("Questions have CodeFilePath field", function () {
    const jsonData = pm.response.json();
    const questionsWithCodeFilePath = jsonData.questions?.filter(q => q.codeFilePath) || [];
    pm.expect(questionsWithCodeFilePath.length).to.be.greaterThan(0);
});

pm.test("CodeFilePath is valid path", function () {
    const jsonData = pm.response.json();
    const firstQuestionWithCodeFilePath = jsonData.questions?.find(q => q.codeFilePath);
    if (firstQuestionWithCodeFilePath) {
        pm.expect(firstQuestionWithCodeFilePath.codeFilePath).to.match(/^[A-Za-z]:\\.+\.(cs|txt)$/);
    }
});
```

### 6. 常见问题排查

#### 6.1 字段缺失问题
**症状**：API 响应中没有 `codeFilePath` 字段

**排查步骤**：
1. 检查 DTO 类是否包含字段定义
2. 检查服务层映射是否包含字段映射
3. 检查数据库中是否有实际数据

#### 6.2 字段值为 null 问题
**症状**：`codeFilePath` 字段存在但值为 null

**排查步骤**：
1. 检查数据库原始数据
2. 检查题目抽取逻辑
3. 检查 JSON 序列化/反序列化

#### 6.3 数据不一致问题
**症状**：不同 API 返回的 `codeFilePath` 值不一致

**排查步骤**：
1. 使用数据流跟踪 API 检查一致性
2. 对比原始数据库数据
3. 检查缓存问题

### 7. 验证清单

- [ ] 数据库中存在 CodeFilePath 数据
- [ ] 模拟考试 API 返回 CodeFilePath 字段
- [ ] 综合实训 API 返回 CodeFilePath 字段
- [ ] 专项训练 API 返回 CodeFilePath 字段
- [ ] 字段值为有效的文件路径格式
- [ ] 不同模块的数据一致性
- [ ] JSON 序列化正确处理字段
- [ ] BenchSuite 能够接收到字段值

### 8. 成功标准

✅ **完全成功**：
- 所有 API 都正确返回 CodeFilePath 字段
- 字段值与数据库数据一致
- BenchSuite 能够正确接收和处理

⚠️ **部分成功**：
- 大部分 API 正确返回字段
- 少数数据不一致问题

❌ **失败**：
- API 响应中缺失 CodeFilePath 字段
- 字段值全部为 null
- 数据严重不一致

### 9. 下一步行动

根据验证结果：

1. **如果验证成功**：继续测试 BenchSuite 集成
2. **如果部分成功**：修复发现的具体问题
3. **如果验证失败**：回到代码映射层面进行深度调试

### 10. 联系信息

如果遇到问题，请检查：
- 应用程序日志
- 数据库连接状态
- 服务注册是否正确
- 环境配置是否正确
