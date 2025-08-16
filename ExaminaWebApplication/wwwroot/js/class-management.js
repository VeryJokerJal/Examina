// 班级管理JavaScript功能

let currentClassId = null;

$(document).ready(function() {
    // 初始化页面
    initializeClassManagement();
});

// 初始化班级管理功能
function initializeClassManagement() {
    // 绑定创建班级表单提交事件
    $('#createClassForm').on('submit', function(e) {
        e.preventDefault();
        createClass();
    });

    // 绑定编辑班级表单提交事件
    $('#editClassForm').on('submit', function(e) {
        e.preventDefault();
        updateClass();
    });

    // 绑定搜索输入框回车事件
    $('#searchKeyword').on('keypress', function(e) {
        if (e.which === 13) {
            searchClasses();
        }
    });

    // 绑定学校筛选变化事件
    $('#schoolFilter').on('change', function() {
        searchClasses();
    });

    // 绑定包含非激活状态复选框变化事件
    $('#includeInactive').on('change', function() {
        searchClasses();
    });
}

// 搜索班级
function searchClasses() {
    const schoolId = $('#schoolFilter').val();
    const keyword = $('#searchKeyword').val().trim();
    const includeInactive = $('#includeInactive').is(':checked');
    
    showLoading('#classesContainer');
    
    let url = '/api/ClassManagementApi';
    if (schoolId) {
        url = `/api/SchoolManagementApi/${schoolId}/classes`;
    }
    
    $.ajax({
        url: url,
        method: 'GET',
        data: {
            includeInactive: includeInactive
        },
        success: function(classes) {
            // 客户端过滤（如果需要关键词搜索）
            let filteredClasses = classes;
            if (keyword) {
                filteredClasses = classes.filter(classOrg => 
                    classOrg.name.toLowerCase().includes(keyword.toLowerCase())
                );
            }
            
            renderClassList(filteredClasses);
            $('#classCount').text(filteredClasses.length);
        },
        error: function(xhr) {
            hideLoading('#classesContainer');
            showErrorMessage('获取班级列表失败：' + getErrorMessage(xhr));
        }
    });
}

// 渲染班级列表
function renderClassList(classes) {
    const container = $('#classesContainer');
    
    if (classes.length === 0) {
        container.html(`
            <div class="text-center py-5">
                <i class="bi bi-people display-1 text-muted"></i>
                <h5 class="text-muted mt-3">暂无班级数据</h5>
                <p class="text-muted">点击上方"创建班级"按钮添加第一个班级</p>
            </div>
        `);
        return;
    }

    let html = '<div class="row g-3">';
    classes.forEach(classOrg => {
        html += `
            <div class="col-md-6 col-lg-4">
                <div class="glass-card glass-card-secondary h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-3">
                            <div>
                                <h6 class="card-title mb-1">${escapeHtml(classOrg.name)}</h6>
                                <small class="text-muted">
                                    <i class="bi bi-building me-1"></i>${escapeHtml(classOrg.parentOrganizationName || '未知学校')}
                                </small>
                            </div>
                            <div class="dropdown">
                                <button class="glass-btn glass-btn-sm" type="button" data-bs-toggle="dropdown">
                                    <i class="bi bi-three-dots-vertical"></i>
                                </button>
                                <ul class="dropdown-menu glass-dropdown-menu">
                                    <li><a class="dropdown-item glass-dropdown-item" href="#" onclick="editClass(${classOrg.id})">
                                        <i class="bi bi-pencil me-2"></i>编辑
                                    </a></li>
                                    <li><a class="dropdown-item glass-dropdown-item" href="#" onclick="viewInvitationCodes(${classOrg.id})">
                                        <i class="bi bi-qr-code me-2"></i>邀请码
                                    </a></li>
                                    <li><a class="dropdown-item glass-dropdown-item" href="#" onclick="viewClassMembers(${classOrg.id})">
                                        <i class="bi bi-people me-2"></i>成员管理
                                    </a></li>
                                    ${classOrg.isActive ? `
                                        <li><hr class="dropdown-divider"></li>
                                        <li><a class="dropdown-item glass-dropdown-item text-danger" href="#" onclick="deactivateClass(${classOrg.id})">
                                            <i class="bi bi-x-circle me-2"></i>停用
                                        </a></li>
                                    ` : ''}
                                </ul>
                            </div>
                        </div>
                        
                        <div class="row g-2 mb-3">
                            <div class="col-6">
                                <div class="glass-stat-card">
                                    <div class="glass-stat-number">${classOrg.studentCount || 0}</div>
                                    <div class="glass-stat-label">学生数量</div>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="glass-stat-card">
                                    <div class="glass-stat-number">${classOrg.invitationCodeCount || 0}</div>
                                    <div class="glass-stat-label">邀请码数量</div>
                                </div>
                            </div>
                        </div>

                        <div class="d-flex justify-content-between align-items-center">
                            <small class="text-muted">
                                <i class="bi bi-person me-1"></i>${escapeHtml(classOrg.creatorUsername)}
                            </small>
                            <span class="badge ${classOrg.isActive ? 'glass-badge-success' : 'glass-badge-danger'}">
                                ${classOrg.isActive ? '激活' : '已停用'}
                            </span>
                        </div>
                        
                        <small class="text-muted d-block mt-1">
                            <i class="bi bi-calendar me-1"></i>${formatDateTime(classOrg.createdAt)}
                        </small>
                    </div>
                </div>
            </div>
        `;
    });
    html += '</div>';
    
    container.html(html);
}

// 创建班级
function createClass() {
    const form = $('#createClassForm');
    const formData = {
        name: $('#className').val().trim(),
        schoolId: parseInt($('#classSchoolId').val()),
        generateInvitationCode: $('#generateInvitationCode').is(':checked')
    };

    // 验证表单
    if (!formData.name) {
        showFieldError('#className', '班级名称不能为空');
        return;
    }
    if (!formData.schoolId) {
        showFieldError('#classSchoolId', '请选择所属学校');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>创建中...').prop('disabled', true);

    $.ajax({
        url: '/api/ClassManagementApi',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(classOrg) {
            // 关闭模态框
            $('#createClassModal').modal('hide');
            
            // 重置表单
            form[0].reset();
            
            // 显示成功消息
            showSuccessMessage('班级创建成功！');
            
            // 刷新班级列表
            searchClasses();
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

// 编辑班级
function editClass(classId) {
    // 获取班级详情
    $.ajax({
        url: `/api/ClassManagementApi/${classId}`,
        method: 'GET',
        success: function(classOrg) {
            // 填充编辑表单
            $('#editClassId').val(classOrg.id);
            $('#editClassName').val(classOrg.name);
            $('#editClassSchoolId').val(classOrg.parentOrganizationId);
            
            // 显示编辑模态框
            $('#editClassModal').modal('show');
        },
        error: function(xhr) {
            showErrorMessage('获取班级信息失败：' + getErrorMessage(xhr));
        }
    });
}

// 更新班级
function updateClass() {
    const form = $('#editClassForm');
    const classId = $('#editClassId').val();
    const formData = {
        name: $('#editClassName').val().trim(),
        schoolId: parseInt($('#editClassSchoolId').val())
    };

    // 验证表单
    if (!formData.name) {
        showFieldError('#editClassName', '班级名称不能为空');
        return;
    }
    if (!formData.schoolId) {
        showFieldError('#editClassSchoolId', '请选择所属学校');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>保存中...').prop('disabled', true);

    $.ajax({
        url: `/api/ClassManagementApi/${classId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(classOrg) {
            // 关闭模态框
            $('#editClassModal').modal('hide');
            
            // 显示成功消息
            showSuccessMessage('班级信息更新成功！');
            
            // 刷新班级列表
            searchClasses();
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

// 停用班级
function deactivateClass(classId) {
    if (!confirm('确定要停用这个班级吗？停用后学生将无法通过邀请码加入该班级。')) {
        return;
    }

    $.ajax({
        url: `/api/ClassManagementApi/${classId}`,
        method: 'DELETE',
        success: function() {
            showSuccessMessage('班级已停用');
            searchClasses();
        },
        error: function(xhr) {
            showErrorMessage('停用班级失败：' + getErrorMessage(xhr));
        }
    });
}

// 查看邀请码
function viewInvitationCodes(classId) {
    currentClassId = classId;
    
    // 显示模态框
    $('#invitationCodesModal').modal('show');
    
    // 加载邀请码列表
    loadInvitationCodes(classId);
}

// 加载邀请码列表
function loadInvitationCodes(classId) {
    showLoading('#invitationCodesContainer');
    
    $.ajax({
        url: `/api/ClassManagementApi/${classId}/invitation-codes`,
        method: 'GET',
        success: function(invitationCodes) {
            renderInvitationCodes(invitationCodes);
        },
        error: function(xhr) {
            $('#invitationCodesContainer').html(`
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    加载邀请码失败：${getErrorMessage(xhr)}
                </div>
            `);
        }
    });
}

// 渲染邀请码列表
function renderInvitationCodes(invitationCodes) {
    const container = $('#invitationCodesContainer');
    
    if (invitationCodes.length === 0) {
        container.html(`
            <div class="text-center py-4">
                <i class="bi bi-qr-code display-4 text-muted"></i>
                <h6 class="text-muted mt-3">暂无邀请码</h6>
                <p class="text-muted">点击"生成新邀请码"按钮创建第一个邀请码</p>
            </div>
        `);
        return;
    }

    let html = '<div class="row g-3">';
    invitationCodes.forEach(code => {
        const isExpired = code.expiresAt && new Date(code.expiresAt) < new Date();
        const isMaxUsed = code.maxUsage && code.usageCount >= code.maxUsage;
        const isInactive = !code.isActive || isExpired || isMaxUsed;
        
        html += `
            <div class="col-md-6">
                <div class="glass-card glass-card-secondary">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-3">
                            <div>
                                <h6 class="card-title mb-1 font-monospace">${code.code}</h6>
                                <small class="text-muted">7位邀请码</small>
                            </div>
                            <span class="badge ${isInactive ? 'glass-badge-danger' : 'glass-badge-success'}">
                                ${isInactive ? '已失效' : '有效'}
                            </span>
                        </div>
                        
                        <div class="row g-2 mb-3">
                            <div class="col-6">
                                <div class="glass-stat-card">
                                    <div class="glass-stat-number">${code.usageCount || 0}</div>
                                    <div class="glass-stat-label">使用次数</div>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="glass-stat-card">
                                    <div class="glass-stat-number">${code.maxUsage || '∞'}</div>
                                    <div class="glass-stat-label">最大使用</div>
                                </div>
                            </div>
                        </div>

                        ${code.expiresAt ? `
                            <small class="text-muted d-block">
                                <i class="bi bi-clock me-1"></i>过期时间：${formatDateTime(code.expiresAt)}
                            </small>
                        ` : ''}
                        
                        <small class="text-muted d-block">
                            <i class="bi bi-calendar me-1"></i>创建时间：${formatDateTime(code.createdAt)}
                        </small>
                        
                        <div class="mt-3 d-flex gap-2 flex-wrap">
                            <button class="glass-btn glass-btn-sm glass-btn-secondary" onclick="copyInvitationCode('${code.code}')">
                                <i class="bi bi-clipboard me-1"></i>复制
                            </button>
                            <button class="glass-btn glass-btn-sm glass-btn-primary" onclick="editInvitationCode(${code.id})">
                                <i class="bi bi-pencil me-1"></i>编辑
                            </button>
                            <button class="glass-btn glass-btn-sm ${code.isActive ? 'glass-btn-warning' : 'glass-btn-success'}"
                                    onclick="toggleInvitationCodeStatus(${code.id}, ${!code.isActive})">
                                <i class="bi ${code.isActive ? 'bi-pause' : 'bi-play'} me-1"></i>
                                ${code.isActive ? '停用' : '激活'}
                            </button>
                            <button class="glass-btn glass-btn-sm glass-btn-danger" onclick="deleteInvitationCode(${code.id})">
                                <i class="bi bi-trash me-1"></i>删除
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    });
    html += '</div>';
    
    container.html(html);
}

// 显示创建邀请码模态框
function showCreateInvitationCodeModal() {
    const modalHtml = `
        <div class="modal fade glass-modal" id="createInvitationCodeModal" tabindex="-1" aria-labelledby="createInvitationCodeModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header glass-modal-header">
                        <h5 class="modal-title" id="createInvitationCodeModalLabel">
                            <i class="bi bi-plus-circle me-2"></i>生成新邀请码
                        </h5>
                        <button type="button" class="glass-btn-close" data-bs-dismiss="modal" aria-label="Close">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                    <form id="createInvitationCodeForm">
                        <div class="modal-body">
                            <div class="mb-3">
                                <label for="createMaxUsage" class="glass-form-label">最大使用次数</label>
                                <input type="number" class="glass-form-control" id="createMaxUsage" name="MaxUsage"
                                       min="1" placeholder="留空表示无限制">
                                <div class="invalid-feedback"></div>
                                <small class="text-muted">设置邀请码的最大使用次数，留空表示无限制</small>
                            </div>
                            <div class="mb-3">
                                <label for="createExpiresAt" class="glass-form-label">过期时间</label>
                                <input type="datetime-local" class="glass-form-control" id="createExpiresAt" name="ExpiresAt">
                                <div class="invalid-feedback"></div>
                                <small class="text-muted">设置邀请码的过期时间，留空表示永不过期</small>
                            </div>
                        </div>
                        <div class="modal-footer glass-modal-footer">
                            <button type="button" class="glass-btn glass-btn-secondary" data-bs-dismiss="modal">
                                <i class="bi bi-x-circle me-2"></i>取消
                            </button>
                            <button type="submit" class="glass-btn glass-btn-primary">
                                <i class="bi bi-check-circle me-2"></i>生成
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `;

    // 移除已存在的模态框
    $('#createInvitationCodeModal').remove();

    // 添加新模态框
    $('body').append(modalHtml);

    // 绑定表单提交事件
    $('#createInvitationCodeForm').on('submit', function(e) {
        e.preventDefault();
        createInvitationCode();
    });

    // 显示模态框
    $('#createInvitationCodeModal').modal('show');
}

// 创建邀请码
function createInvitationCode() {
    if (!currentClassId) {
        showErrorMessage('无法确定班级ID');
        return;
    }

    const form = $('#createInvitationCodeForm');
    const formData = {
        maxUsage: $('#createMaxUsage').val() ? parseInt($('#createMaxUsage').val()) : null,
        expiresAt: $('#createExpiresAt').val() ? new Date($('#createExpiresAt').val()).toISOString() : null
    };

    // 清除之前的错误
    clearFormErrors(form);

    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/invitation-codes`,
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function() {
            // 关闭模态框
            $('#createInvitationCodeModal').modal('hide');

            // 显示成功消息
            showSuccessMessage('邀请码生成成功！');

            // 刷新邀请码列表
            loadInvitationCodes(currentClassId);
        },
        error: function(xhr) {
            handleFormError(xhr, form);
        }
    });
}

// 复制邀请码
function copyInvitationCode(code) {
    navigator.clipboard.writeText(code).then(function() {
        showSuccessMessage('邀请码已复制到剪贴板');
    }).catch(function() {
        // 降级方案
        const textArea = document.createElement('textarea');
        textArea.value = code;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showSuccessMessage('邀请码已复制到剪贴板');
    });
}

// 编辑邀请码
function editInvitationCode(invitationCodeId) {
    if (!currentClassId) {
        showErrorMessage('无法确定班级ID');
        return;
    }

    // 获取邀请码信息
    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/invitation-codes`,
        method: 'GET',
        success: function(invitationCodes) {
            const code = invitationCodes.find(c => c.id === invitationCodeId);
            if (!code) {
                showErrorMessage('邀请码不存在');
                return;
            }
            showEditInvitationCodeModal(code);
        },
        error: function(xhr) {
            showErrorMessage('获取邀请码信息失败：' + getErrorMessage(xhr));
        }
    });
}

// 显示编辑邀请码模态框
function showEditInvitationCodeModal(code) {
    const modalHtml = `
        <div class="modal fade glass-modal" id="editInvitationCodeModal" tabindex="-1" aria-labelledby="editInvitationCodeModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header glass-modal-header">
                        <h5 class="modal-title" id="editInvitationCodeModalLabel">
                            <i class="bi bi-pencil me-2"></i>编辑邀请码
                        </h5>
                        <button type="button" class="glass-btn-close" data-bs-dismiss="modal" aria-label="Close">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                    <form id="editInvitationCodeForm">
                        <div class="modal-body">
                            <div class="mb-3">
                                <label class="glass-form-label">邀请码</label>
                                <input type="text" class="glass-form-control" value="${code.code}" readonly>
                            </div>
                            <div class="mb-3">
                                <label for="editMaxUsage" class="glass-form-label">最大使用次数</label>
                                <input type="number" class="glass-form-control" id="editMaxUsage" name="MaxUsage"
                                       value="${code.maxUsage || ''}" min="1" placeholder="留空表示无限制">
                                <div class="invalid-feedback"></div>
                            </div>
                            <div class="mb-3">
                                <label for="editExpiresAt" class="glass-form-label">过期时间</label>
                                <input type="datetime-local" class="glass-form-control" id="editExpiresAt" name="ExpiresAt"
                                       value="${code.expiresAt ? new Date(code.expiresAt).toISOString().slice(0, 16) : ''}">
                                <div class="invalid-feedback"></div>
                            </div>
                            <div class="mb-3">
                                <div class="glass-form-check">
                                    <input class="glass-form-check-input" type="checkbox" id="editIsActive" name="IsActive" ${code.isActive ? 'checked' : ''}>
                                    <label class="glass-form-check-label" for="editIsActive">
                                        激活状态
                                    </label>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer glass-modal-footer">
                            <button type="button" class="glass-btn glass-btn-secondary" data-bs-dismiss="modal">
                                <i class="bi bi-x-circle me-2"></i>取消
                            </button>
                            <button type="submit" class="glass-btn glass-btn-primary">
                                <i class="bi bi-check-circle me-2"></i>保存
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `;

    // 移除已存在的模态框
    $('#editInvitationCodeModal').remove();

    // 添加新模态框
    $('body').append(modalHtml);

    // 绑定表单提交事件
    $('#editInvitationCodeForm').on('submit', function(e) {
        e.preventDefault();
        updateInvitationCode(code.id);
    });

    // 显示模态框
    $('#editInvitationCodeModal').modal('show');
}

// 更新邀请码
function updateInvitationCode(invitationCodeId) {
    const form = $('#editInvitationCodeForm');
    const formData = {
        maxUsage: $('#editMaxUsage').val() ? parseInt($('#editMaxUsage').val()) : null,
        expiresAt: $('#editExpiresAt').val() ? new Date($('#editExpiresAt').val()).toISOString() : null,
        isActive: $('#editIsActive').is(':checked')
    };

    // 清除之前的错误
    clearFormErrors(form);

    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/invitation-codes/${invitationCodeId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function() {
            // 关闭模态框
            $('#editInvitationCodeModal').modal('hide');

            // 显示成功消息
            showSuccessMessage('邀请码更新成功！');

            // 刷新邀请码列表
            loadInvitationCodes(currentClassId);
        },
        error: function(xhr) {
            handleFormError(xhr, form);
        }
    });
}

// 删除邀请码
function deleteInvitationCode(invitationCodeId) {
    if (!confirm('确定要删除这个邀请码吗？删除后无法恢复。')) {
        return;
    }

    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/invitation-codes/${invitationCodeId}`,
        method: 'DELETE',
        success: function() {
            showSuccessMessage('邀请码删除成功！');
            loadInvitationCodes(currentClassId);
        },
        error: function(xhr) {
            showErrorMessage('删除邀请码失败：' + getErrorMessage(xhr));
        }
    });
}

// 切换邀请码状态
function toggleInvitationCodeStatus(invitationCodeId, newStatus) {
    const action = newStatus ? '激活' : '停用';

    if (!confirm(`确定要${action}这个邀请码吗？`)) {
        return;
    }

    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/invitation-codes/${invitationCodeId}/status`,
        method: 'PATCH',
        contentType: 'application/json',
        data: JSON.stringify({ isActive: newStatus }),
        success: function() {
            showSuccessMessage(`邀请码${action}成功！`);
            loadInvitationCodes(currentClassId);
        },
        error: function(xhr) {
            showErrorMessage(`${action}邀请码失败：` + getErrorMessage(xhr));
        }
    });
}

// 查看班级成员
function viewClassMembers(classId) {
    // 跳转到成员管理页面或显示成员列表模态框
    window.location.href = `/ClassMembers?classId=${classId}`;
}

// 工具函数（复用学校管理的函数）
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
