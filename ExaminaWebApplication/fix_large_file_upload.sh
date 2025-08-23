#!/bin/bash

# ExaminaWebApplication 大文件上传修复脚本
# 用于在生产环境中快速应用500MB文件上传支持

set -e  # 遇到错误时退出

echo "=== ExaminaWebApplication 大文件上传修复脚本 ==="
echo "此脚本将配置服务器以支持500MB文件上传"
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 日志函数
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查是否以root权限运行
check_root() {
    if [[ $EUID -ne 0 ]]; then
        log_error "此脚本需要root权限运行"
        echo "请使用: sudo $0"
        exit 1
    fi
}

# 备份文件
backup_file() {
    local file=$1
    if [[ -f "$file" ]]; then
        local backup="${file}.backup.$(date +%Y%m%d_%H%M%S)"
        cp "$file" "$backup"
        log_info "已备份 $file 到 $backup"
    fi
}

# 检查服务状态
check_service() {
    local service=$1
    if systemctl is-active --quiet "$service"; then
        log_success "$service 服务正在运行"
        return 0
    else
        log_warning "$service 服务未运行"
        return 1
    fi
}

# 更新Nginx配置
update_nginx_config() {
    log_info "更新Nginx配置..."
    
    local nginx_conf="/etc/nginx/nginx.conf"
    local nginx_site="/etc/nginx/sites-available/default"
    
    # 备份现有配置
    backup_file "$nginx_conf"
    
    # 检查是否已经配置了大文件上传
    if grep -q "client_max_body_size.*500M" "$nginx_conf" 2>/dev/null; then
        log_success "Nginx已配置大文件上传支持"
        return 0
    fi
    
    # 在http块中添加大文件上传配置
    if grep -q "http {" "$nginx_conf"; then
        sed -i '/http {/a\
    # 大文件上传配置\
    client_max_body_size 500M;\
    client_body_timeout 600s;\
    client_header_timeout 60s;\
    proxy_connect_timeout 600s;\
    proxy_send_timeout 600s;\
    proxy_read_timeout 600s;\
' "$nginx_conf"
        log_success "已更新Nginx全局配置"
    else
        log_warning "未找到Nginx http配置块，请手动配置"
    fi
    
    # 检查并更新站点配置
    if [[ -f "$nginx_site" ]]; then
        backup_file "$nginx_site"
        
        # 添加文件上传API特殊配置
        if ! grep -q "/api/fileupload/" "$nginx_site"; then
            sed -i '/location \/ {/i\
        # 文件上传API特殊配置\
        location /api/fileupload/ {\
            proxy_pass http://localhost:5000;\
            proxy_http_version 1.1;\
            proxy_set_header Upgrade $http_upgrade;\
            proxy_set_header Connection keep-alive;\
            proxy_set_header Host $host;\
            proxy_set_header X-Real-IP $remote_addr;\
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\
            proxy_set_header X-Forwarded-Proto $scheme;\
            proxy_cache_bypass $http_upgrade;\
            proxy_request_buffering off;\
            proxy_buffering off;\
            client_max_body_size 500M;\
            client_body_timeout 600s;\
        }\
\
' "$nginx_site"
            log_success "已更新Nginx站点配置"
        fi
    fi
    
    # 测试Nginx配置
    if nginx -t 2>/dev/null; then
        log_success "Nginx配置语法正确"
        return 0
    else
        log_error "Nginx配置语法错误，请检查配置文件"
        return 1
    fi
}

# 更新应用程序配置
update_app_config() {
    log_info "更新应用程序配置..."
    
    local app_dir="/var/www/examina"
    local prod_config="$app_dir/appsettings.Production.json"
    
    if [[ ! -d "$app_dir" ]]; then
        log_error "应用程序目录不存在: $app_dir"
        return 1
    fi
    
    # 备份配置文件
    if [[ -f "$prod_config" ]]; then
        backup_file "$prod_config"
    fi
    
    # 检查是否已经配置了大文件上传
    if [[ -f "$prod_config" ]] && grep -q '"MaxFileSize": 524288000' "$prod_config"; then
        log_success "应用程序已配置大文件上传支持"
        return 0
    fi
    
    # 创建或更新生产环境配置
    cat > "$prod_config" << 'EOF'
{
  "Performance": {
    "MaxRequestBodySize": 524288000,
    "RequestTimeoutSeconds": 600
  },
  "FileUpload": {
    "MaxFileSize": 524288000,
    "MaxFileCount": 10,
    "AllowedExtensions": [
      ".zip", ".rar", ".7z",
      ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
      ".txt", ".rtf", ".json", ".xml",
      ".jpg", ".jpeg", ".png", ".gif", ".bmp",
      ".mp4", ".avi", ".mov", ".wmv",
      ".mp3", ".wav", ".wma"
    ],
    "UploadPath": "wwwroot/uploads",
    "TempUploadPath": "wwwroot/uploads/temp",
    "EnableHashValidation": true,
    "EnableVirusScanning": false,
    "FileRetentionDays": 0,
    "EnableCompression": false,
    "CompressionQuality": 85
  }
}
EOF
    
    log_success "已更新应用程序配置文件"
    
    # 确保上传目录存在并设置正确权限
    local upload_dir="$app_dir/wwwroot/uploads"
    local temp_dir="$upload_dir/temp"
    
    mkdir -p "$upload_dir" "$temp_dir"
    chown -R www-data:www-data "$upload_dir"
    chmod -R 755 "$upload_dir"
    
    log_success "已创建并配置上传目录权限"
}

# 重启服务
restart_services() {
    log_info "重启服务..."
    
    # 重启Nginx
    if systemctl restart nginx; then
        log_success "Nginx重启成功"
    else
        log_error "Nginx重启失败"
        return 1
    fi
    
    # 重启应用程序服务
    local app_service="examina-web"
    if systemctl list-units --type=service | grep -q "$app_service"; then
        if systemctl restart "$app_service"; then
            log_success "$app_service 重启成功"
        else
            log_error "$app_service 重启失败"
            return 1
        fi
    else
        log_warning "未找到 $app_service 服务，请手动重启应用程序"
    fi
    
    # 等待服务启动
    sleep 5
    
    # 检查服务状态
    check_service nginx
    check_service "$app_service" || true
}

# 验证配置
verify_config() {
    log_info "验证配置..."
    
    # 检查Nginx配置
    if nginx -t 2>/dev/null; then
        log_success "Nginx配置验证通过"
    else
        log_error "Nginx配置验证失败"
        return 1
    fi
    
    # 检查应用程序是否响应
    local app_url="http://localhost:5000"
    if curl -s -o /dev/null -w "%{http_code}" "$app_url" | grep -q "200\|302"; then
        log_success "应用程序响应正常"
    else
        log_warning "应用程序可能未正常启动，请检查日志"
    fi
    
    # 测试文件上传API
    local upload_url="http://localhost/api/fileupload/upload"
    if curl -s -I "$upload_url" | grep -q "HTTP"; then
        log_success "文件上传API可访问"
    else
        log_warning "文件上传API可能不可访问"
    fi
}

# 显示后续步骤
show_next_steps() {
    echo ""
    log_success "=== 修复完成 ==="
    echo ""
    echo "后续步骤："
    echo "1. 检查应用程序日志: sudo journalctl -u examina-web -f"
    echo "2. 检查Nginx日志: sudo tail -f /var/log/nginx/error.log"
    echo "3. 测试文件上传: 访问 https://qiuzhenbd.com/Home/FileUploadTest"
    echo "4. 监控系统资源使用情况"
    echo ""
    echo "如果仍然遇到413错误，请检查："
    echo "- CDN或负载均衡器的文件大小限制"
    echo "- 防火墙或安全组配置"
    echo "- 应用程序是否正确读取了配置文件"
    echo ""
}

# 主函数
main() {
    echo "开始执行修复..."
    echo ""
    
    # 检查权限
    check_root
    
    # 更新配置
    update_nginx_config || exit 1
    update_app_config || exit 1
    
    # 重启服务
    restart_services || exit 1
    
    # 验证配置
    verify_config
    
    # 显示后续步骤
    show_next_steps
    
    log_success "大文件上传修复脚本执行完成！"
}

# 执行主函数
main "$@"
