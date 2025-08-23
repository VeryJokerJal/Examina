# 文件上传功能说明

## 功能概述

ExaminaWebApplication项目已成功集成了完整的文件上传功能，支持为三种试卷类型（考试试卷、综合实训试卷、专项训练试卷）上传和管理相关文件。

## 主要特性

### 1. 多文件上传支持
- 支持同时上传多个文件
- 拖拽上传功能
- 实时上传进度显示
- 文件大小和格式验证

### 2. 支持的文件格式
- **压缩包**: ZIP, RAR, 7Z
- **文档**: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX
- **文本**: TXT, RTF, JSON, XML
- **图片**: JPG, JPEG, PNG, GIF, BMP
- **视频**: MP4, AVI, MOV, WMV
- **音频**: MP3, WAV, WMA

### 3. 文件管理功能
- 文件列表查看（列表视图/网格视图）
- 文件搜索和筛选
- 文件下载
- 文件删除（软删除）
- 文件详情查看

### 4. 文件关联功能
- 自动关联上传的文件到对应的试卷
- 支持不同文件类型标记（主文件、附件、参考资料等）
- 文件用途描述

## 使用方法

### 1. 在试卷详情页面上传文件

1. 进入任意试卷的详情页面（考试详情、综合训练详情、专项训练详情）
2. 滚动到"文件管理"部分
3. 可以通过以下方式上传文件：
   - 点击"选择文件"按钮
   - 直接拖拽文件到上传区域
4. 选择文件后，点击"上传"按钮
5. 上传完成后，文件会自动关联到当前试卷

### 2. 使用文件管理页面

1. 在导航菜单中点击"文件管理"
2. 查看所有已上传的文件
3. 使用搜索和筛选功能快速找到需要的文件
4. 可以下载、查看详情或删除文件

### 3. API接口使用

#### 上传单个文件
```
POST /api/fileupload/upload
Content-Type: multipart/form-data

参数:
- file: 上传的文件
- description: 文件描述（可选）
- tags: 文件标签（可选）
```

#### 上传多个文件
```
POST /api/fileupload/upload-multiple
Content-Type: multipart/form-data

参数:
- files: 上传的文件列表
- description: 文件描述（可选）
- tags: 文件标签（可选）
```

#### 关联文件到考试
```
POST /api/fileupload/associate/exam/{examId}/file/{fileId}
Content-Type: multipart/form-data

参数:
- fileType: 文件类型（默认: Attachment）
- purpose: 文件用途（可选）
```

#### 获取考试关联的文件
```
GET /api/fileupload/exam/{examId}/files
```

#### 下载文件
```
GET /api/fileupload/download/{fileId}
```

#### 删除文件
```
DELETE /api/fileupload/{fileId}
```

## 配置说明

文件上传相关配置位于 `appsettings.json` 的 `FileUpload` 部分：

```json
{
  "FileUpload": {
    "MaxFileSize": 104857600,           // 最大文件大小（100MB）
    "MaxFileCount": 10,                 // 最大同时上传文件数量
    "AllowedExtensions": [...],         // 允许的文件扩展名
    "AllowedMimeTypes": [...],          // 允许的MIME类型
    "UploadPath": "wwwroot/uploads",    // 上传目录路径
    "EnableHashValidation": true,       // 是否启用文件哈希验证
    "EnableVirusScanning": false,       // 是否启用病毒扫描
    "FileRetentionDays": 0              // 文件保留天数（0表示永久保留）
  }
}
```

## 数据库结构

### 主要表结构

1. **UploadedFiles** - 上传文件主表
   - 存储文件基本信息（文件名、大小、路径、哈希值等）
   - 支持软删除
   - 记录上传者和下载统计

2. **ExamFileAssociations** - 考试文件关联表
3. **ComprehensiveTrainingFileAssociations** - 综合训练文件关联表
4. **SpecializedTrainingFileAssociations** - 专项训练文件关联表

### 数据库迁移

已创建数据库迁移文件 `AddFileUploadTables`，包含所有必要的表结构和索引。

## 安全特性

1. **文件类型验证** - 基于扩展名和MIME类型双重验证
2. **文件大小限制** - 可配置的文件大小上限
3. **文件哈希验证** - 防止重复上传和确保文件完整性
4. **软删除** - 删除的文件不会立即从磁盘移除
5. **访问控制** - 基于用户权限的文件访问控制

## 前端组件

### FileUploadComponent
可重用的JavaScript文件上传组件，支持：
- 拖拽上传
- 多文件选择
- 实时进度显示
- 文件预览
- 错误处理

### 使用示例
```html
@{
    ViewData["ContainerId"] = "my-file-upload";
    ViewData["Multiple"] = true;
    ViewData["AutoUpload"] = false;
}
@await Html.PartialAsync("_FileUpload")
```

## 注意事项

1. 确保 `wwwroot/uploads` 目录存在且有写入权限
2. 大文件上传可能需要调整服务器超时设置
3. 生产环境建议启用病毒扫描功能
4. 定期清理软删除的文件以释放磁盘空间
5. 考虑使用CDN或对象存储服务来存储大量文件

## 故障排除

### 常见问题

1. **上传失败** - 检查文件大小和格式是否符合要求
2. **权限错误** - 确保上传目录有正确的读写权限
3. **数据库错误** - 确保已运行数据库迁移
4. **API调用失败** - 检查用户认证状态和权限

### 日志查看

文件上传相关的日志会记录在应用程序日志中，可以通过以下方式查看：
- 开发环境：控制台输出
- 生产环境：配置的日志提供程序（如文件、数据库等）

## 扩展功能

未来可以考虑添加的功能：
1. 文件版本管理
2. 文件分享链接
3. 文件预览功能
4. 批量操作
5. 文件同步到云存储
6. 文件加密存储
7. 文件访问日志
8. 自动文件分类
