# ExaminaWebApplication 生产环境部署指南 - 500MB大文件上传支持

## 🎯 概述

本指南详细说明如何在生产环境中部署ExaminaWebApplication并确保500MB大文件上传功能正常工作。

## 📋 部署检查清单

### 1. 应用程序配置

#### ✅ appsettings.Production.json 配置
确保生产环境配置文件包含以下设置：

```json
{
  "Performance": {
    "MaxRequestBodySize": 524288000,    // 500MB
    "RequestTimeoutSeconds": 600        // 10分钟
  },
  "FileUpload": {
    "MaxFileSize": 524288000,           // 500MB
    "MaxFileCount": 10,
    // ... 其他文件上传配置
  }
}
```

#### ✅ web.config 配置
确保IIS配置文件存在并包含：

```xml
<system.webServer>
  <security>
    <requestFiltering>
      <requestLimits maxAllowedContentLength="524288000" />
    </requestFiltering>
  </security>
  <httpRuntime maxRequestLength="512000" executionTimeout="3600" />
</system.webServer>
```

### 2. Nginx 反向代理配置

#### ✅ 关键配置项
在Nginx配置文件中确保包含以下设置：

```nginx
http {
    # 全局配置
    client_max_body_size 500M;
    client_body_timeout 600s;
    proxy_connect_timeout 600s;
    proxy_send_timeout 600s;
    proxy_read_timeout 600s;
    
    server {
        # 文件上传API特殊配置
        location /api/fileupload/ {
            proxy_pass http://examina_backend;
            proxy_request_buffering off;     # 关键：禁用请求缓冲
            proxy_buffering off;
            client_max_body_size 500M;
            client_body_timeout 600s;
        }
    }
}
```

### 3. 系统级配置

#### ✅ 磁盘空间检查
```bash
# 检查可用磁盘空间
df -h /var/www/examina/wwwroot/uploads/

# 确保至少有足够空间存储上传的文件
# 建议至少保留10GB以上的可用空间
```

#### ✅ 文件权限设置
```bash
# 设置上传目录权限
sudo chown -R www-data:www-data /var/www/examina/wwwroot/uploads/
sudo chmod -R 755 /var/www/examina/wwwroot/uploads/

# 创建临时上传目录
sudo mkdir -p /var/www/examina/wwwroot/uploads/temp
sudo chown -R www-data:www-data /var/www/examina/wwwroot/uploads/temp
```

## 🚀 部署步骤

### 步骤1: 停止应用程序
```bash
# 停止ASP.NET Core应用
sudo systemctl stop examina-web

# 停止Nginx（如果需要更新配置）
sudo systemctl stop nginx
```

### 步骤2: 更新应用程序文件
```bash
# 备份当前版本
sudo cp -r /var/www/examina /var/www/examina.backup.$(date +%Y%m%d_%H%M%S)

# 部署新版本
sudo cp -r /path/to/new/release/* /var/www/examina/

# 确保配置文件正确
sudo cp appsettings.Production.json /var/www/examina/
sudo cp web.config /var/www/examina/
```

### 步骤3: 更新Nginx配置
```bash
# 备份当前Nginx配置
sudo cp /etc/nginx/nginx.conf /etc/nginx/nginx.conf.backup

# 更新Nginx配置
sudo cp nginx.conf /etc/nginx/nginx.conf

# 测试Nginx配置
sudo nginx -t
```

### 步骤4: 启动服务
```bash
# 启动Nginx
sudo systemctl start nginx

# 启动ASP.NET Core应用
sudo systemctl start examina-web

# 检查服务状态
sudo systemctl status nginx
sudo systemctl status examina-web
```

### 步骤5: 验证部署
```bash
# 检查应用程序日志
sudo journalctl -u examina-web -f

# 测试文件上传API
curl -X POST https://qiuzhenbd.com/api/fileupload/upload \
  -F "file=@test_file.zip" \
  -F "description=部署测试"
```

## 🔧 故障排除

### 问题1: 仍然出现413错误

#### 可能原因和解决方案：

1. **Nginx配置未生效**
   ```bash
   # 检查Nginx配置
   sudo nginx -t
   
   # 重新加载配置
   sudo nginx -s reload
   
   # 检查Nginx错误日志
   sudo tail -f /var/log/nginx/error.log
   ```

2. **应用程序配置未更新**
   ```bash
   # 检查应用程序是否使用了正确的配置文件
   sudo cat /var/www/examina/appsettings.Production.json | grep MaxFileSize
   
   # 重启应用程序
   sudo systemctl restart examina-web
   ```

3. **CDN或负载均衡器限制**
   ```bash
   # 如果使用CDN，检查CDN的文件大小限制
   # 如果使用负载均衡器，检查其配置
   ```

### 问题2: 上传超时

#### 解决方案：
```bash
# 增加Nginx超时设置
client_body_timeout 1200s;
proxy_connect_timeout 1200s;
proxy_send_timeout 1200s;
proxy_read_timeout 1200s;

# 增加应用程序超时设置
"Performance": {
  "RequestTimeoutSeconds": 1200
}
```

### 问题3: 磁盘空间不足

#### 解决方案：
```bash
# 清理旧的上传文件
sudo find /var/www/examina/wwwroot/uploads/ -type f -mtime +30 -delete

# 监控磁盘使用情况
sudo du -sh /var/www/examina/wwwroot/uploads/*
```

## 📊 监控和维护

### 1. 日志监控
```bash
# 监控应用程序日志
sudo journalctl -u examina-web -f | grep -i "upload\|413\|error"

# 监控Nginx访问日志
sudo tail -f /var/log/nginx/access.log | grep "POST.*fileupload"

# 监控Nginx错误日志
sudo tail -f /var/log/nginx/error.log
```

### 2. 性能监控
```bash
# 监控系统资源使用
htop

# 监控磁盘I/O
iotop

# 监控网络连接
netstat -tulpn | grep :80
```

### 3. 定期维护
```bash
# 每周清理临时文件
sudo find /var/www/examina/wwwroot/uploads/temp/ -type f -mtime +7 -delete

# 每月备份上传文件
sudo tar -czf /backup/uploads_$(date +%Y%m).tar.gz /var/www/examina/wwwroot/uploads/

# 检查磁盘空间
df -h | grep -E "(uploads|var)"
```

## 🔒 安全考虑

### 1. 文件类型验证
确保应用程序严格验证上传的文件类型：
```json
"FileUpload": {
  "AllowedExtensions": [".zip", ".rar", ".7z", ".pdf", ...],
  "AllowedMimeTypes": ["application/zip", ...]
}
```

### 2. 病毒扫描
考虑集成病毒扫描功能：
```bash
# 安装ClamAV
sudo apt-get install clamav clamav-daemon

# 配置定期扫描
sudo crontab -e
# 添加：0 2 * * * /usr/bin/clamscan -r /var/www/examina/wwwroot/uploads/
```

### 3. 访问控制
确保上传目录的安全：
```nginx
# 禁止直接访问上传文件
location /uploads/ {
    internal;  # 只允许内部重定向访问
}
```

## 📈 性能优化建议

### 1. 使用SSD存储
- 将上传目录放在SSD上以提高I/O性能
- 考虑使用专门的文件存储服务

### 2. 负载均衡
- 如果有多台服务器，确保文件上传负载均衡配置正确
- 考虑使用粘性会话或共享存储

### 3. CDN集成
- 考虑将大文件存储到CDN或对象存储服务
- 实施文件上传到云存储的功能

## ✅ 部署验证清单

部署完成后，请验证以下项目：

- [ ] 应用程序正常启动，无错误日志
- [ ] Nginx配置正确，无语法错误
- [ ] 可以访问应用程序主页
- [ ] 可以访问文件上传测试页面 `/Home/FileUploadTest`
- [ ] 可以成功上传小文件（<10MB）
- [ ] 可以成功上传大文件（100-500MB）
- [ ] 上传进度显示正常
- [ ] 文件下载功能正常
- [ ] 错误处理和日志记录正常
- [ ] 系统资源使用正常

## 📞 技术支持

如果在部署过程中遇到问题，请：

1. 检查应用程序日志：`sudo journalctl -u examina-web -f`
2. 检查Nginx日志：`sudo tail -f /var/log/nginx/error.log`
3. 验证配置文件语法：`sudo nginx -t`
4. 确认服务状态：`sudo systemctl status examina-web nginx`
5. 测试网络连接：`curl -I https://qiuzhenbd.com/api/fileupload/upload`

记录详细的错误信息和系统环境信息以便进一步诊断。
