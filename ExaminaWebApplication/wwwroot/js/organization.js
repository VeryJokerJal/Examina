// 组织管理系统JavaScript功能

// 全局变量
let loadingOverlay = null;

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeOrganizationFeatures();
});

// 初始化组织功能
function initializeOrganizationFeatures() {
    // 初始化工具提示
    initializeTooltips();
    
    // 初始化表单验证
    initializeFormValidation();
    
    // 初始化字符计数器
    initializeCharacterCounters();
    
    // 初始化确认对话框
    initializeConfirmDialogs();
    
    // 自动隐藏警告消息
    autoHideAlerts();
    
    // 初始化邀请码输入格式化
    initializeInvitationCodeFormatting();
}

// 初始化工具提示
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// 初始化表单验证
function initializeFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    
    Array.prototype.slice.call(forms).forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            
            form.classList.add('was-validated');
        }, false);
    });
}

// 初始化字符计数器
function initializeCharacterCounters() {
    const inputs = document.querySelectorAll('input[maxlength], textarea[maxlength]');
    
    inputs.forEach(function(input) {
        const maxLength = parseInt(input.getAttribute('maxlength'));
        if (maxLength > 0) {
            setupCharacterCounter(input, maxLength);
        }
    });
}

// 设置字符计数器
function setupCharacterCounter(input, maxLength) {
    const container = input.parentNode;
    let counter = container.querySelector('.character-counter');
    
    if (!counter) {
        counter = document.createElement('div');
        counter.className = 'character-counter';
        container.appendChild(counter);
    }
    
    function updateCounter() {
        const remaining = maxLength - input.value.length;
        counter.textContent = `${input.value.length}/${maxLength}`;
        
        // 更新样式
        counter.classList.remove('warning', 'danger');
        if (remaining < 20) {
            counter.classList.add('warning');
        }
        if (remaining < 5) {
            counter.classList.add('danger');
        }
    }
    
    input.addEventListener('input', updateCounter);
    updateCounter(); // 初始化
}

// 初始化确认对话框
function initializeConfirmDialogs() {
    const confirmButtons = document.querySelectorAll('[data-confirm]');
    
    confirmButtons.forEach(function(button) {
        button.addEventListener('click', function(e) {
            const message = this.getAttribute('data-confirm');
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        });
    });
}

// 自动隐藏警告消息
function autoHideAlerts() {
    const alerts = document.querySelectorAll('.alert.show');
    
    alerts.forEach(function(alert) {
        setTimeout(function() {
            if (alert.classList.contains('show')) {
                alert.classList.remove('show');
                alert.classList.add('fade');
            }
        }, 5000);
    });
}

// 初始化邀请码输入格式化
function initializeInvitationCodeFormatting() {
    const invitationInputs = document.querySelectorAll('input[name*="InvitationCode"], input[id*="InvitationCode"]');
    
    invitationInputs.forEach(function(input) {
        input.addEventListener('input', function(e) {
            // 转换为大写
            this.value = this.value.toUpperCase();
            
            // 只允许字母和数字
            this.value = this.value.replace(/[^A-Z0-9]/g, '');
            
            // 限制长度为7位
            if (this.value.length > 7) {
                this.value = this.value.substring(0, 7);
            }
        });
    });
}

// 显示加载状态
function showLoading(message = '处理中...') {
    hideLoading(); // 先隐藏现有的
    
    loadingOverlay = document.createElement('div');
    loadingOverlay.className = 'loading-overlay';
    loadingOverlay.innerHTML = `
        <div class="text-center text-white">
            <div class="loading-spinner mb-3"></div>
            <div>${message}</div>
        </div>
    `;
    
    document.body.appendChild(loadingOverlay);
}

// 隐藏加载状态
function hideLoading() {
    if (loadingOverlay) {
        document.body.removeChild(loadingOverlay);
        loadingOverlay = null;
    }
}

// 显示通知消息
function showNotification(message, type = 'info', duration = 3000) {
    const alertClass = `alert-${type}`;
    const iconClass = type === 'success' ? 'bi-check-circle' : 
                     type === 'danger' ? 'bi-exclamation-triangle' : 
                     type === 'warning' ? 'bi-exclamation-triangle' : 'bi-info-circle';
    
    const alert = document.createElement('div');
    alert.className = `alert ${alertClass} glass-alert-${type} alert-dismissible fade show position-fixed`;
    alert.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    alert.innerHTML = `
        <i class="bi ${iconClass} me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alert);
    
    // 自动隐藏
    setTimeout(function() {
        if (alert.parentNode) {
            alert.classList.remove('show');
            setTimeout(function() {
                if (alert.parentNode) {
                    document.body.removeChild(alert);
                }
            }, 150);
        }
    }, duration);
}

// 复制到剪贴板
async function copyToClipboard(text, successMessage = '已复制到剪贴板') {
    try {
        await navigator.clipboard.writeText(text);
        showNotification(successMessage, 'success');
    } catch (err) {
        // 降级方案
        const textArea = document.createElement('textarea');
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showNotification(successMessage, 'success');
    }
}

// 格式化日期
function formatDate(dateString, format = 'yyyy-MM-dd HH:mm') {
    const date = new Date(dateString);
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    
    return format
        .replace('yyyy', year)
        .replace('MM', month)
        .replace('dd', day)
        .replace('HH', hours)
        .replace('mm', minutes);
}

// 验证邀请码格式
function validateInvitationCode(code) {
    const pattern = /^[A-Z0-9]{7}$/;
    return pattern.test(code);
}

// 防抖函数
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// 节流函数
function throttle(func, limit) {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    }
}

// 表单提交处理
function handleFormSubmit(form, submitButton, loadingText = '提交中...') {
    const originalText = submitButton.innerHTML;
    
    submitButton.innerHTML = `<i class="spinner-border spinner-border-sm me-2"></i>${loadingText}`;
    submitButton.disabled = true;
    
    // 返回恢复函数
    return function restore() {
        submitButton.innerHTML = originalText;
        submitButton.disabled = false;
    };
}

// AJAX请求封装
async function makeRequest(url, options = {}) {
    const defaultOptions = {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
        }
    };
    
    // 添加防伪令牌
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        defaultOptions.headers['RequestVerificationToken'] = token;
    }
    
    const finalOptions = { ...defaultOptions, ...options };
    
    try {
        const response = await fetch(url, finalOptions);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return response;
    } catch (error) {
        console.error('Request failed:', error);
        throw error;
    }
}

// 导出全局函数
window.OrganizationManager = {
    showLoading,
    hideLoading,
    showNotification,
    copyToClipboard,
    formatDate,
    validateInvitationCode,
    debounce,
    throttle,
    handleFormSubmit,
    makeRequest
};
