// Glass Notification System
// 玻璃拟态通知系统

class GlassNotification {
    constructor() {
        this.container = null;
        this.notifications = new Map();
        this.init();
    }

    init() {
        // 创建通知容器
        this.container = document.createElement('div');
        this.container.className = 'glass-notification-container';
        document.body.appendChild(this.container);
    }

    show(message, type = 'info', options = {}) {
        const defaults = {
            duration: 5000,
            dismissible: true,
            icon: true,
            position: 'top-right'
        };

        const config = { ...defaults, ...options };
        const id = this.generateId();

        // 创建通知元素
        const notification = this.createElement(message, type, config, id);
        
        // 添加到容器
        this.container.appendChild(notification);
        this.notifications.set(id, notification);

        // 触发显示动画
        requestAnimationFrame(() => {
            notification.classList.add('show');
        });

        // 自动关闭
        if (config.duration > 0) {
            setTimeout(() => {
                this.hide(id);
            }, config.duration);
        }

        return id;
    }

    createElement(message, type, config, id) {
        const notification = document.createElement('div');
        notification.className = `glass-notification glass-alert-${type} alert-dismissible fade`;
        notification.setAttribute('data-notification-id', id);

        // 创建内容容器
        const content = document.createElement('div');
        content.className = 'glass-notification-content';

        // 添加图标（如果启用）
        if (config.icon) {
            const icon = this.getIcon(type);
            content.innerHTML = `<i class="bi ${icon} me-2"></i>${message}`;
        } else {
            content.textContent = message;
        }

        notification.appendChild(content);

        // 添加关闭按钮（如果可关闭）
        if (config.dismissible) {
            const closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'glass-btn-close';
            closeBtn.setAttribute('aria-label', 'Close');
            closeBtn.innerHTML = '<i class="bi bi-x"></i>';
            
            closeBtn.addEventListener('click', () => {
                this.hide(id);
            });

            notification.appendChild(closeBtn);
        }

        return notification;
    }

    getIcon(type) {
        const icons = {
            success: 'bi-check-circle-fill',
            error: 'bi-exclamation-triangle-fill',
            warning: 'bi-exclamation-triangle-fill',
            info: 'bi-info-circle-fill'
        };
        return icons[type] || icons.info;
    }

    hide(id) {
        const notification = this.notifications.get(id);
        if (!notification) return;

        // 添加退出动画
        notification.classList.add('fade-out');

        // 动画完成后移除元素
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
            this.notifications.delete(id);
        }, 300);
    }

    hideAll() {
        this.notifications.forEach((notification, id) => {
            this.hide(id);
        });
    }

    generateId() {
        return 'notification-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
    }

    // 便捷方法
    success(message, options = {}) {
        return this.show(message, 'success', options);
    }

    error(message, options = {}) {
        return this.show(message, 'error', options);
    }

    warning(message, options = {}) {
        return this.show(message, 'warning', options);
    }

    info(message, options = {}) {
        return this.show(message, 'info', options);
    }
}

// 创建全局实例
window.glassNotification = new GlassNotification();

// 兼容性函数 - 与现有代码集成
window.showNotification = function(message, type = 'info', options = {}) {
    return window.glassNotification.show(message, type, options);
};

window.showSuccessMessage = function(message, options = {}) {
    return window.glassNotification.success(message, options);
};

window.showErrorMessage = function(message, options = {}) {
    return window.glassNotification.error(message, options);
};

window.showWarningMessage = function(message, options = {}) {
    return window.glassNotification.warning(message, options);
};

window.showInfoMessage = function(message, options = {}) {
    return window.glassNotification.info(message, options);
};

// 示例用法：
// showNotification('操作成功！', 'success');
// showErrorMessage('操作失败：创建学校失败');
// glassNotification.warning('请注意！', { duration: 3000 });

// 页面卸载时清理
window.addEventListener('beforeunload', () => {
    if (window.glassNotification) {
        window.glassNotification.hideAll();
    }
});

// DOM加载完成后初始化
document.addEventListener('DOMContentLoaded', () => {
    // 如果页面上已有旧式通知，转换为新式通知
    const existingAlerts = document.querySelectorAll('.alert:not(.glass-notification)');
    existingAlerts.forEach(alert => {
        const message = alert.textContent.trim();
        let type = 'info';
        
        if (alert.classList.contains('alert-success')) type = 'success';
        else if (alert.classList.contains('alert-danger')) type = 'error';
        else if (alert.classList.contains('alert-warning')) type = 'warning';
        
        // 移除旧通知
        alert.remove();
        
        // 显示新通知
        if (message) {
            showNotification(message, type);
        }
    });
});
