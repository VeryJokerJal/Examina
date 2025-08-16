// 学校管理JavaScript功能

$(document).ready(function() {
    // 初始化页面
    initializeSchoolManagement();
});

// 初始化学校管理功能
function initializeSchoolManagement() {
    // 绑定创建学校表单提交事件
    $('#createSchoolForm').on('submit', function(e) {
        e.preventDefault();
        createSchool();
    });

    // 绑定编辑学校表单提交事件
    $('#editSchoolForm').on('submit', function(e) {
        e.preventDefault();
        updateSchool();
    });

    // 绑定搜索输入框回车事件
    $('#searchKeyword').on('keypress', function(e) {
        if (e.which === 13) {
            searchSchools();
        }
    });

    // 绑定包含非激活状态复选框变化事件
    $('#includeInactive').on('change', function() {
        searchSchools();
    });
}

// 搜索学校
function searchSchools() {
    const keyword = $('#searchKeyword').val().trim();
    const includeInactive = $('#includeInactive').is(':checked');
    
    showLoading('#schoolsContainer');
    
    $.ajax({
        url: '/api/SchoolManagementApi',
        method: 'GET',
        data: {
            includeInactive: includeInactive
        },
        success: function(schools) {
            // 客户端过滤（如果需要关键词搜索）
            let filteredSchools = schools;
            if (keyword) {
                filteredSchools = schools.filter(school => 
                    school.name.toLowerCase().includes(keyword.toLowerCase())
                );
            }
            
            renderSchoolList(filteredSchools);
            $('#schoolCount').text(filteredSchools.length);
        },
        error: function(xhr) {
            hideLoading('#schoolsContainer');
            showErrorMessage('获取学校列表失败：' + getErrorMessage(xhr));
        }
    });
}

// 渲染学校列表
function renderSchoolList(schools) {
    const container = $('#schoolsContainer');
    
    if (schools.length === 0) {
        container.html(`
            <div class="text-center py-5">
                <i class="bi bi-building display-1 text-muted"></i>
                <h5 class="text-muted mt-3">暂无学校数据</h5>
                <p class="text-muted">点击上方"创建学校"按钮添加第一个学校</p>
            </div>
        `);
        return;
    }

    let html = '<div class="row g-3">';
    schools.forEach(school => {
        html += `
            <div class="col-md-6 col-lg-4">
                <div class="glass-card glass-card-secondary h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-3">
                            <h6 class="card-title mb-0">${escapeHtml(school.name)}</h6>
                            <div class="dropdown">
                                <button class="glass-btn glass-btn-sm" type="button" data-bs-toggle="dropdown">
                                    <i class="bi bi-three-dots-vertical"></i>
                                </button>
                                <ul class="dropdown-menu glass-dropdown-menu">
                                    <li><a class="dropdown-item glass-dropdown-item" href="#" onclick="editSchool(${school.id})">
                                        <i class="bi bi-pencil me-2"></i>编辑
                                    </a></li>
                                    <li><a class="dropdown-item glass-dropdown-item" href="#" onclick="viewSchoolClasses(${school.id})">
                                        <i class="bi bi-people me-2"></i>查看班级
                                    </a></li>
                                    ${school.isActive ? `
                                        <li><hr class="dropdown-divider"></li>
                                        <li><a class="dropdown-item glass-dropdown-item text-danger" href="#" onclick="deactivateSchool(${school.id})">
                                            <i class="bi bi-x-circle me-2"></i>停用
                                        </a></li>
                                    ` : ''}
                                </ul>
                            </div>
                        </div>
                        
                        <div class="row g-2 mb-3">
                            <div class="col-6">
                                <div class="glass-stat-card">
                                    <div class="glass-stat-number">${school.childOrganizationCount || 0}</div>
                                    <div class="glass-stat-label">班级数量</div>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="glass-stat-card">
                                    <div class="glass-stat-number">${school.studentCount || 0}</div>
                                    <div class="glass-stat-label">学生数量</div>
                                </div>
                            </div>
                        </div>

                        <div class="d-flex justify-content-between align-items-center">
                            <small class="text-muted">
                                <i class="bi bi-person me-1"></i>${escapeHtml(school.creatorUsername)}
                            </small>
                            <span class="badge ${school.isActive ? 'glass-badge-success' : 'glass-badge-danger'}">
                                ${school.isActive ? '激活' : '已停用'}
                            </span>
                        </div>
                        
                        <small class="text-muted d-block mt-1">
                            <i class="bi bi-calendar me-1"></i>${formatDateTime(school.createdAt)}
                        </small>
                    </div>
                </div>
            </div>
        `;
    });
    html += '</div>';
    
    container.html(html);
}

// 创建学校
function createSchool() {
    const form = $('#createSchoolForm');
    const formData = {
        name: $('#schoolName').val().trim()
    };

    // 验证表单
    if (!formData.name) {
        showFieldError('#schoolName', '学校名称不能为空');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>创建中...').prop('disabled', true);

    $.ajax({
        url: '/api/SchoolManagementApi',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(school) {
            // 关闭模态框
            $('#createSchoolModal').modal('hide');
            
            // 重置表单
            form[0].reset();
            
            // 显示成功消息
            showSuccessMessage('学校创建成功！');
            
            // 刷新学校列表
            searchSchools();
        },
        error: function(xhr) {
            handleFormError(xhr, form);
        },
        complete: function() {
            // 恢复按钮状态
            submitBtn.html(originalText).prop('disabled', false);
        }
    });
}

// 编辑学校
function editSchool(schoolId) {
    // 获取学校详情
    $.ajax({
        url: `/api/SchoolManagementApi/${schoolId}`,
        method: 'GET',
        success: function(school) {
            // 填充编辑表单
            $('#editSchoolId').val(school.id);
            $('#editSchoolName').val(school.name);
            
            // 显示编辑模态框
            $('#editSchoolModal').modal('show');
        },
        error: function(xhr) {
            showErrorMessage('获取学校信息失败：' + getErrorMessage(xhr));
        }
    });
}

// 更新学校
function updateSchool() {
    const form = $('#editSchoolForm');
    const schoolId = $('#editSchoolId').val();
    const formData = {
        name: $('#editSchoolName').val().trim()
    };

    // 验证表单
    if (!formData.name) {
        showFieldError('#editSchoolName', '学校名称不能为空');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>保存中...').prop('disabled', true);

    $.ajax({
        url: `/api/SchoolManagementApi/${schoolId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(school) {
            // 关闭模态框
            $('#editSchoolModal').modal('hide');
            
            // 显示成功消息
            showSuccessMessage('学校信息更新成功！');
            
            // 刷新学校列表
            searchSchools();
        },
        error: function(xhr) {
            handleFormError(xhr, form);
        },
        complete: function() {
            // 恢复按钮状态
            submitBtn.html(originalText).prop('disabled', false);
        }
    });
}

// 停用学校
function deactivateSchool(schoolId) {
    if (!confirm('确定要停用这个学校吗？停用后该学校下的所有班级也将无法使用。')) {
        return;
    }

    $.ajax({
        url: `/api/SchoolManagementApi/${schoolId}`,
        method: 'DELETE',
        success: function() {
            showSuccessMessage('学校已停用');
            searchSchools();
        },
        error: function(xhr) {
            showErrorMessage('停用学校失败：' + getErrorMessage(xhr));
        }
    });
}

// 查看学校班级
function viewSchoolClasses(schoolId) {
    // 跳转到班级管理页面，并筛选该学校的班级
    window.location.href = `/ClassManagement?schoolId=${schoolId}`;
}

// 工具函数
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatDateTime(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function showLoading(selector) {
    $(selector).html(`
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">加载中...</span>
            </div>
            <p class="text-muted mt-3">加载中...</p>
        </div>
    `);
}

function hideLoading(selector) {
    // 由其他函数处理内容替换
}

function showSuccessMessage(message) {
    // 使用现有的通知系统或创建临时通知
    if (typeof showNotification === 'function') {
        showNotification(message, 'success');
    } else {
        alert(message);
    }
}

function showErrorMessage(message) {
    // 使用现有的通知系统或创建临时通知
    if (typeof showNotification === 'function') {
        showNotification(message, 'error');
    } else {
        alert(message);
    }
}

function showFieldError(selector, message) {
    const field = $(selector);
    field.addClass('is-invalid');
    field.siblings('.invalid-feedback').text(message);
}

function clearFieldErrors(form) {
    form.find('.is-invalid').removeClass('is-invalid');
    form.find('.invalid-feedback').text('');
}

function handleFormError(xhr, form) {
    const response = xhr.responseJSON;
    if (response && response.errors) {
        // 处理验证错误
        Object.keys(response.errors).forEach(field => {
            const fieldSelector = `[name="${field}"]`;
            showFieldError(fieldSelector, response.errors[field][0]);
        });
    } else {
        showErrorMessage('操作失败：' + getErrorMessage(xhr));
    }
}

function getErrorMessage(xhr) {
    if (xhr.responseJSON && xhr.responseJSON.message) {
        return xhr.responseJSON.message;
    }
    return xhr.statusText || '未知错误';
}
