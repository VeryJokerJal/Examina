// 用户管理JavaScript功能

let currentPage = 1;
let pageSize = 50;

$(document).ready(function() {
    // 初始化页面
    initializeUserManagement();
});

// 初始化用户管理功能
function initializeUserManagement() {
    // 绑定创建用户表单提交事件
    $('#createUserForm').on('submit', function(e) {
        e.preventDefault();
        createUser();
    });

    // 绑定编辑用户表单提交事件
    $('#editUserForm').on('submit', function(e) {
        e.preventDefault();
        updateUser();
    });

    // 绑定重置密码表单提交事件
    $('#resetPasswordForm').on('submit', function(e) {
        e.preventDefault();
        resetUserPassword();
    });

    // 绑定搜索输入框回车事件
    $('#searchKeyword').on('keypress', function(e) {
        if (e.which === 13) {
            searchUsers();
        }
    });

    // 绑定角色筛选变化事件
    $('#roleFilter').on('change', function() {
        searchUsers();
    });

    // 绑定包含非激活状态复选框变化事件
    $('#includeInactive').on('change', function() {
        searchUsers();
    });

    // 绑定用户角色变化事件
    $('#userRole').on('change', function() {
        toggleRoleSpecificFields();
    });

    // 绑定学校选择变化事件
    $('#userSchoolId').on('change', function() {
        updateClassOptions();
    });

    // 绑定确认密码验证
    $('#confirmPassword').on('input', function() {
        validatePasswordConfirmation();
    });

    // 绑定创建用户模态框显示事件
    $('#createUserModal').on('show.bs.modal', function() {
        resetCreateUserForm();
    });
}

// 重置创建用户表单
function resetCreateUserForm() {
    // 清空表单
    $('#createUserForm')[0].reset();

    // 清除错误状态
    clearFieldErrors($('#createUserForm'));

    // 隐藏角色特定字段
    $('#schoolSelection').hide();
    $('#classSelection').hide();
    $('#schoolRequired').hide();

    // 清空班级选择
    $('#classCheckboxes').empty();

    // 移除学校字段的必填要求
    $('#userSchoolId').removeAttr('required');
}

// 切换角色特定字段显示
function toggleRoleSpecificFields() {
    const role = $('#userRole').val();
    const schoolSelection = $('#schoolSelection');
    const classSelection = $('#classSelection');
    const schoolRequired = $('#schoolRequired');

    // 清空学校和班级选择
    $('#userSchoolId').val('');
    $('#classCheckboxes').empty();

    if (role === 'Teacher') {
        // 教师需要选择学校和班级
        schoolSelection.show();
        classSelection.show();
        schoolRequired.show(); // 显示必填标识

        // 设置学校字段为必填
        $('#userSchoolId').attr('required', true);
    } else if (role === 'Student') {
        // 学生不需要预先指定学校（通过邀请码加入）
        schoolSelection.hide();
        classSelection.hide();
        schoolRequired.hide(); // 隐藏必填标识

        // 移除学校字段的必填要求
        $('#userSchoolId').removeAttr('required');
    } else {
        // 管理员等其他角色
        schoolSelection.hide();
        classSelection.hide();
        schoolRequired.hide(); // 隐藏必填标识

        // 移除学校字段的必填要求
        $('#userSchoolId').removeAttr('required');
    }
}

// 更新班级选项
function updateClassOptions() {
    const schoolId = $('#userSchoolId').val();
    const container = $('#classCheckboxes');

    if (!schoolId) {
        container.empty();
        container.html('<div class="col-12"><p class="text-muted">请先选择学校</p></div>');
        return;
    }

    // 显示加载状态
    container.html('<div class="col-12"><p class="text-muted"><i class="bi bi-hourglass-split me-2"></i>加载班级列表...</p></div>');

    // 通过API获取指定学校的班级列表
    $.ajax({
        url: `/api/SchoolManagementApi/${schoolId}/classes`,
        method: 'GET',
        data: {
            includeInactive: false
        },
        success: function(classes) {
            renderClassCheckboxes(classes, container);
        },
        error: function(xhr) {
            container.html('<div class="col-12"><p class="text-danger"><i class="bi bi-exclamation-triangle me-2"></i>加载班级列表失败</p></div>');
            console.error('获取班级列表失败：', getErrorMessage(xhr));
        }
    });
}

// 渲染班级复选框
function renderClassCheckboxes(classes, container) {
    if (classes.length === 0) {
        container.html('<div class="col-12"><p class="text-muted">该学校暂无班级</p></div>');
        return;
    }

    let html = '';
    classes.forEach(classOrg => {
        html += `
            <div class="col-md-6">
                <div class="glass-form-check">
                    <input class="glass-form-check-input" type="checkbox" id="class_${classOrg.id}" name="ClassIds" value="${classOrg.id}">
                    <label class="glass-form-check-label" for="class_${classOrg.id}">
                        ${escapeHtml(classOrg.name)}
                    </label>
                </div>
            </div>
        `;
    });

    container.html(html);
}

// 验证密码确认
function validatePasswordConfirmation() {
    const password = $('#newPassword').val();
    const confirmPassword = $('#confirmPassword').val();
    const confirmField = $('#confirmPassword');
    
    if (confirmPassword && password !== confirmPassword) {
        confirmField.addClass('is-invalid');
        confirmField.siblings('.invalid-feedback').text('两次输入的密码不一致');
    } else {
        confirmField.removeClass('is-invalid');
        confirmField.siblings('.invalid-feedback').text('');
    }
}

// 搜索用户
function searchUsers() {
    const role = $('#roleFilter').val();
    const keyword = $('#searchKeyword').val().trim();
    const includeInactive = $('#includeInactive').is(':checked');
    
    currentPage = 1; // 重置到第一页
    
    showLoading('#usersContainer');
    
    $.ajax({
        url: '/api/UserManagementApi',
        method: 'GET',
        data: {
            role: role || null,
            keyword: keyword || null,
            includeInactive: includeInactive,
            pageNumber: currentPage,
            pageSize: pageSize
        },
        success: function(users) {
            renderUserTable(users);
            updateStatistics(users);
        },
        error: function(xhr) {
            hideLoading('#usersContainer');
            showErrorMessage('获取用户列表失败：' + getErrorMessage(xhr));
        }
    });
}

// 重置搜索
function resetSearch() {
    $('#searchKeyword').val('');
    $('#roleFilter').val('');
    $('#includeInactive').prop('checked', false);
    searchUsers();
}

// 渲染用户表格
function renderUserTable(users) {
    const container = $('#usersContainer');
    
    if (users.length === 0) {
        container.html(`
            <div class="text-center py-5">
                <i class="bi bi-people-fill display-1 text-muted"></i>
                <h5 class="text-muted mt-3">暂无用户数据</h5>
                <p class="text-muted">点击上方"创建用户"按钮添加第一个用户</p>
            </div>
        `);
        return;
    }

    let html = `
        <div>
            <table class="table glass-table">
                <thead>
                    <tr>
                        <th>用户信息</th>
                        <th>角色</th>
                        <th>联系方式</th>
                        <th>注册时间</th>
                        <th>状态</th>
                        <th>操作</th>
                    </tr>
                </thead>
                <tbody>
    `;

    users.forEach(user => {
        const roleText = user.role === 'Administrator' ? '管理员' : user.role === 'Teacher' ? '教师' : '学生';
        const roleBadgeClass = user.role === 'Administrator' ? 'glass-badge-danger' : user.role === 'Teacher' ? 'glass-badge-warning' : 'glass-badge-info';
        
        html += `
            <tr>
                <td>
                    <div>
                        <strong>${escapeHtml(user.username)}</strong>
                        ${user.realName ? `<br><small class="text-muted">${escapeHtml(user.realName)}</small>` : ''}
                    </div>
                </td>
                <td>
                    <span class="badge ${roleBadgeClass}">
                        ${roleText}
                    </span>
                </td>
                <td>
                    <div>
                        <small>${escapeHtml(user.email)}</small>
                        ${user.phoneNumber ? `<br><small class="text-muted font-monospace">${escapeHtml(user.phoneNumber)}</small>` : ''}
                    </div>
                </td>

                <td>
                    <small>${formatDateTime(user.createdAt)}</small>
                    ${user.lastLoginAt ? `<br><small class="text-muted">最后登录：${formatDateTime(user.lastLoginAt)}</small>` : ''}
                </td>
                <td>
                    <div>
                        <span class="badge ${user.isActive ? 'glass-badge-success' : 'glass-badge-danger'}">
                            ${user.isActive ? '正常' : '已停用'}
                        </span>
                        ${user.isFirstLogin ? '<br><span class="badge glass-badge-warning mt-1">首次登录</span>' : ''}
                    </div>
                </td>
                <td>
                    <div class="btn-group" role="group">
                        <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-primary" onclick="editUser(${user.id})" title="编辑">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-warning" onclick="resetPassword(${user.id})" title="重置密码">
                            <i class="bi bi-key"></i>
                        </button>
                        ${user.isActive ? `
                            <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-danger" onclick="deactivateUser(${user.id})" title="停用">
                                <i class="bi bi-person-x"></i>
                            </button>
                        ` : `
                            <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-success" onclick="activateUser(${user.id})" title="激活">
                                <i class="bi bi-person-check"></i>
                            </button>
                        `}
                    </div>
                </td>
            </tr>
        `;
    });

    html += `
                </tbody>
            </table>
        </div>
    `;
    
    container.html(html);
    $('#userCount').text(users.length);
}

// 更新统计信息
function updateStatistics(users) {
    const totalUsers = users.length;
    const teacherCount = users.filter(u => u.role === 'Teacher').length;
    const studentCount = users.filter(u => u.role === 'Student').length;
    const adminCount = users.filter(u => u.role === 'Administrator').length;
    
    $('#totalUsers').text(totalUsers);
    $('#teacherCount').text(teacherCount);
    $('#studentCount').text(studentCount);
    $('#adminCount').text(adminCount);
}

// 创建用户
function createUser() {
    const form = $('#createUserForm');
    const formData = {
        username: $('#userUsername').val().trim(),
        email: $('#userEmail').val().trim(),
        password: $('#userPassword').val(),
        role: $('#userRole').val(),
        phoneNumber: $('#userPhoneNumber').val().trim() || null,
        realName: $('#userRealName').val().trim() || null,
        schoolId: $('#userSchoolId').val() ? parseInt($('#userSchoolId').val()) : null,
        classIds: []
    };

    // 获取选中的班级
    $('#classCheckboxes input[type="checkbox"]:checked').each(function() {
        formData.classIds.push(parseInt($(this).val()));
    });

    // 验证表单
    if (!formData.username) {
        showFieldError('#userUsername', '用户名不能为空');
        return;
    }
    if (!formData.email) {
        showFieldError('#userEmail', '邮箱不能为空');
        return;
    }
    if (!formData.password) {
        showFieldError('#userPassword', '密码不能为空');
        return;
    }
    if (!formData.role) {
        showFieldError('#userRole', '请选择用户角色');
        return;
    }
    // 只有教师角色需要选择学校
    if (formData.role === 'Teacher' && !formData.schoolId) {
        showFieldError('#userSchoolId', '请选择所属学校');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>创建中...').prop('disabled', true);

    $.ajax({
        url: '/api/UserManagementApi',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(user) {
            // 关闭模态框
            $('#createUserModal').modal('hide');
            
            // 重置表单
            form[0].reset();
            toggleRoleSpecificFields();
            
            // 显示成功消息
            showSuccessMessage('用户创建成功！');
            
            // 刷新用户列表
            searchUsers();
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

// 编辑用户
function editUser(userId) {
    // 获取用户详情
    $.ajax({
        url: `/api/UserManagementApi/${userId}`,
        method: 'GET',
        success: function(user) {
            // 填充编辑表单
            $('#editUserId').val(user.id);
            $('#editUserEmail').val(user.email);
            $('#editUserPhoneNumber').val(user.phoneNumber || '');
            $('#editUserRealName').val(user.realName || '');
            
            // 显示编辑模态框
            $('#editUserModal').modal('show');
        },
        error: function(xhr) {
            showErrorMessage('获取用户信息失败：' + getErrorMessage(xhr));
        }
    });
}

// 更新用户
function updateUser() {
    const form = $('#editUserForm');
    const userId = $('#editUserId').val();
    const formData = {
        email: $('#editUserEmail').val().trim(),
        phoneNumber: $('#editUserPhoneNumber').val().trim() || null,
        realName: $('#editUserRealName').val().trim() || null
    };

    // 验证表单
    if (!formData.email) {
        showFieldError('#editUserEmail', '邮箱不能为空');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>保存中...').prop('disabled', true);

    $.ajax({
        url: `/api/UserManagementApi/${userId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(user) {
            // 关闭模态框
            $('#editUserModal').modal('hide');
            
            // 显示成功消息
            showSuccessMessage('用户信息更新成功！');
            
            // 刷新用户列表
            searchUsers();
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

// 重置密码
function resetPassword(userId) {
    $('#resetPasswordUserId').val(userId);
    $('#resetPasswordModal').modal('show');
}

// 重置用户密码
function resetUserPassword() {
    const form = $('#resetPasswordForm');
    const userId = $('#resetPasswordUserId').val();
    const newPassword = $('#newPassword').val();
    const confirmPassword = $('#confirmPassword').val();

    // 验证表单
    if (!newPassword) {
        showFieldError('#newPassword', '新密码不能为空');
        return;
    }
    if (newPassword !== confirmPassword) {
        showFieldError('#confirmPassword', '两次输入的密码不一致');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>重置中...').prop('disabled', true);

    $.ajax({
        url: `/api/UserManagementApi/${userId}/reset-password`,
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ newPassword: newPassword }),
        success: function() {
            // 关闭模态框
            $('#resetPasswordModal').modal('hide');
            
            // 重置表单
            form[0].reset();
            
            // 显示成功消息
            showSuccessMessage('密码重置成功！');
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

// 停用用户
function deactivateUser(userId) {
    if (!confirm('确定要停用这个用户吗？停用后用户将无法登录系统。')) {
        return;
    }

    $.ajax({
        url: `/api/UserManagementApi/${userId}/deactivate`,
        method: 'POST',
        success: function() {
            showSuccessMessage('用户已停用');
            searchUsers();
        },
        error: function(xhr) {
            showErrorMessage('停用用户失败：' + getErrorMessage(xhr));
        }
    });
}

// 激活用户
function activateUser(userId) {
    $.ajax({
        url: `/api/UserManagementApi/${userId}/activate`,
        method: 'POST',
        success: function() {
            showSuccessMessage('用户已激活');
            searchUsers();
        },
        error: function(xhr) {
            showErrorMessage('激活用户失败：' + getErrorMessage(xhr));
        }
    });
}

// 工具函数（复用之前的函数）
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
    if (typeof showNotification === 'function') {
        showNotification(message, 'success');
    } else {
        alert(message);
    }
}

function showErrorMessage(message) {
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

// 切换组织成员身份
function toggleOrganizationMembership(userId, isJoining) {
    const action = isJoining ? '加入组织' : '移出组织';
    const confirmMessage = isJoining
        ? '确定要将此用户加入组织吗？'
        : '确定要将此用户从所有组织中移除吗？';

    if (!confirm(confirmMessage)) {
        return;
    }

    // 显示加载状态
    showLoadingMessage(`正在${action}...`);

    $.ajax({
        url: `/api/UserManagementApi/${userId}/toggle-organization-membership`,
        method: 'POST',
        success: function(response) {
            // 显示成功消息
            showSuccessMessage(response.message || `${action}成功！`);

            // 刷新用户列表
            searchUsers();
        },
        error: function(xhr) {
            const errorMessage = getErrorMessage(xhr);
            if (xhr.status === 400 && errorMessage.includes('需要通过组织管理界面')) {
                showErrorMessage('该用户当前不是组织成员，请通过组织管理界面手动添加用户到具体组织。');
            } else {
                showErrorMessage(`${action}失败：${errorMessage}`);
            }
        }
    });
}

// 显示加载消息
function showLoadingMessage(message) {
    // 移除现有的消息
    $('.alert').remove();

    // 创建加载消息
    const alertHtml = `
        <div class="alert alert-info alert-dismissible fade show" role="alert">
            <i class="bi bi-hourglass-split me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    // 在页面顶部显示
    $('.container-fluid').prepend(alertHtml);
}
