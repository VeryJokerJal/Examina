// 组织管理相关JavaScript功能

// 全局变量
let currentOrganization = null;
let organizationMembers = [];

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeOrganizationFeatures();
});

// 初始化组织功能
function initializeOrganizationFeatures() {
    // 初始化搜索功能
    initializeSearch();
    
    // 初始化表单验证
    initializeFormValidation();
    
    // 初始化工具提示
    initializeTooltips();
    
    // 初始化复制功能
    initializeCopyFeatures();
}

// 初始化搜索功能
function initializeSearch() {
    const searchInput = document.getElementById('organizationSearch');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(handleSearch, 300));
        
        // 点击外部关闭搜索结果
        document.addEventListener('click', function(e) {
            if (!e.target.closest('.org-search-container')) {
                hideSearchResults();
            }
        });
    }
}

// 搜索处理函数
function handleSearch(event) {
    const query = event.target.value.trim();
    if (query.length < 2) {
        hideSearchResults();
        return;
    }
    
    // 这里可以添加实际的搜索逻辑
    // 目前只是示例
    showSearchResults([
        { id: 1, name: '示例组织1', type: '学校' },
        { id: 2, name: '示例组织2', type: '企业' }
    ]);
}

// 显示搜索结果
function showSearchResults(results) {
    const container = document.querySelector('.org-search-results');
    if (!container) return;
    
    container.innerHTML = '';
    
    results.forEach(result => {
        const item = document.createElement('div');
        item.className = 'org-search-item';
        item.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="fw-bold">${result.name}</div>
                    <small class="text-muted">${result.type}</small>
                </div>
                <button class="btn btn-sm btn-outline-primary" onclick="selectOrganization(${result.id})">
                    选择
                </button>
            </div>
        `;
        container.appendChild(item);
    });
    
    container.style.display = 'block';
}

// 隐藏搜索结果
function hideSearchResults() {
    const container = document.querySelector('.org-search-results');
    if (container) {
        container.style.display = 'none';
    }
}

// 选择组织
function selectOrganization(organizationId) {
    console.log('选择组织:', organizationId);
    hideSearchResults();
    // 这里添加选择组织的逻辑
}

// 初始化表单验证
function initializeFormValidation() {
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(form => {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });
}

// 初始化工具提示
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// 初始化复制功能
function initializeCopyFeatures() {
    const copyButtons = document.querySelectorAll('[data-copy]');
    copyButtons.forEach(button => {
        button.addEventListener('click', function() {
            const textToCopy = this.getAttribute('data-copy');
            copyToClipboard(textToCopy);
        });
    });
}

// 复制到剪贴板
function copyToClipboard(text) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            showNotification('已复制到剪贴板', 'success');
        }).catch(() => {
            fallbackCopyToClipboard(text);
        });
    } else {
        fallbackCopyToClipboard(text);
    }
}

// 备用复制方法
function fallbackCopyToClipboard(text) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        document.execCommand('copy');
        showNotification('已复制到剪贴板', 'success');
    } catch (err) {
        showNotification('复制失败，请手动复制', 'error');
    }
    
    document.body.removeChild(textArea);
}

// 显示通知
function showNotification(message, type = 'info', duration = 3000) {
    // 移除现有通知
    const existingNotification = document.querySelector('.org-notification');
    if (existingNotification) {
        existingNotification.remove();
    }
    
    // 创建新通知
    const notification = document.createElement('div');
    notification.className = `org-notification ${type}`;
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    // 显示通知
    setTimeout(() => {
        notification.classList.add('show');
    }, 100);
    
    // 自动隐藏
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, duration);
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

// 格式化日期
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// 生成随机邀请码
function generateInvitationCode(length = 8) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let result = '';
    for (let i = 0; i < length; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
}

// 验证邀请码格式
function validateInvitationCode(code) {
    const pattern = /^[A-Z0-9]{6,12}$/;
    return pattern.test(code);
}

// 组织成员管理
const OrganizationMembers = {
    // 添加成员
    add: function(memberData) {
        console.log('添加成员:', memberData);
        // 这里添加实际的添加逻辑
    },
    
    // 移除成员
    remove: function(memberId) {
        if (confirm('确定要移除此成员吗？')) {
            console.log('移除成员:', memberId);
            // 这里添加实际的移除逻辑
        }
    },
    
    // 更新成员角色
    updateRole: function(memberId, newRole) {
        console.log('更新成员角色:', memberId, newRole);
        // 这里添加实际的更新逻辑
    }
};

// 组织设置管理
const OrganizationSettings = {
    // 更新组织信息
    update: function(organizationData) {
        console.log('更新组织信息:', organizationData);
        // 这里添加实际的更新逻辑
    },
    
    // 删除组织
    delete: function(organizationId) {
        if (confirm('确定要删除此组织吗？此操作不可撤销！')) {
            console.log('删除组织:', organizationId);
            // 这里添加实际的删除逻辑
        }
    }
};

// 导出全局函数供HTML使用
window.showNotification = showNotification;
window.copyToClipboard = copyToClipboard;
window.OrganizationMembers = OrganizationMembers;
window.OrganizationSettings = OrganizationSettings;
