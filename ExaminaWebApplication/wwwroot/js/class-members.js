// 班级成员管理JavaScript功能

let currentClassId = null;
let currentPage = 1;
let pageSize = 50;

$(document).ready(function() {
    // 初始化页面
    initializeClassMembers();
});

// 初始化班级成员管理功能
function initializeClassMembers() {
    // 获取班级ID
    currentClassId = window.classId;

    if (!currentClassId) {
        showErrorMessage('无法获取班级ID');
        return;
    }



    // 绑定添加成员表单提交事件
    $('#addMemberForm').on('submit', function(e) {
        e.preventDefault();
        addMember();
    });

    // 绑定搜索输入框回车事件
    $('#searchKeyword').on('keypress', function(e) {
        if (e.which === 13) {
            searchMembers();
        }
    });

    // 绑定包含非激活状态复选框变化事件
    $('#includeInactive').on('change', function() {
        searchMembers();
    });

    // 绑定添加成员模态框显示事件
    $('#addMemberModal').on('show.bs.modal', function() {
        loadInvitationCodes();
        resetAddMemberForm();
    });



    // 初始加载成员列表
    loadMembers();
}

// 加载班级成员列表
function loadMembers(includeInactive = false) {
    showLoading('#membersContainer');

    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/members`,
        method: 'GET',
        data: {
            includeInactive: includeInactive
        },
        success: function(members) {
            renderMemberTable(members);
            updateMemberCount(members.length);
        },
        error: function(xhr) {
            hideLoading('#membersContainer');
            showErrorMessage('获取班级成员列表失败：' + getErrorMessage(xhr));
        }
    });
}

// 渲染成员表格
function renderMemberTable(members) {
    const container = $('#membersContainer');

    if (members.length === 0) {
        container.html(`
            <div class="text-center py-5">
                <i class="bi bi-people display-1 text-muted"></i>
                <h5 class="text-muted mt-3">暂无班级成员</h5>
                <p class="text-muted">点击"添加成员"按钮开始添加学生到班级</p>
            </div>
        `);
        return;
    }

    let html = `
        <div class="table-responsive">
            <table class="table glass-table">
                <thead>
                    <tr>
                        <th>学生信息</th>
                        <th>联系方式</th>
                        <th>加入时间</th>
                        <th>邀请码</th>
                        <th>状态</th>
                        <th>操作</th>
                    </tr>
                </thead>
                <tbody>
    `;

    members.forEach(member => {
        html += `
            <tr>
                <td>
                    <div class="d-flex align-items-center">
                        <div class="glass-avatar glass-avatar-sm me-3">
                            <i class="bi bi-person"></i>
                        </div>
                        <div>
                            <div class="fw-medium">${escapeHtml(member.studentUsername)}</div>
                            ${member.studentRealName ? `<small class="text-muted">${escapeHtml(member.studentRealName)}</small>` : ''}
                        </div>
                    </div>
                </td>
                <td>
                    <div>
                        ${member.studentPhoneNumber ? `<small class="text-muted font-monospace">${escapeHtml(member.studentPhoneNumber)}</small>` : '<span class="text-muted">-</span>'}
                    </div>
                </td>
                <td>
                    <small>${formatDateTime(member.joinedAt)}</small>
                </td>
                <td>
                    ${member.invitationCode ? `<span class="font-monospace text-muted">${escapeHtml(member.invitationCode)}</span>` : '<span class="text-muted">-</span>'}
                </td>
                <td>
                    <span class="badge ${member.isActive ? 'glass-badge-success' : 'glass-badge-danger'}">
                        ${member.isActive ? '正常' : '已移除'}
                    </span>
                </td>
                <td>
                    <div class="glass-btn-group">
                        ${member.isActive ? `
                            <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-danger" onclick="removeMember(${member.id})" title="移除成员">
                                <i class="bi bi-person-dash"></i>
                            </button>
                        ` : `
                            <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-success" onclick="restoreMember(${member.id})" title="恢复成员">
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
}

// 搜索班级成员
function searchMembers() {
    const keyword = $('#searchKeyword').val().trim();
    const includeInactive = $('#includeInactive').is(':checked');

    // 如果有搜索关键词，进行客户端过滤
    if (keyword) {
        const filteredMembers = window.membersData.filter(member =>
            member.studentUsername.toLowerCase().includes(keyword.toLowerCase()) ||
            (member.studentRealName && member.studentRealName.toLowerCase().includes(keyword.toLowerCase())) ||
            (member.studentPhoneNumber && member.studentPhoneNumber.includes(keyword))
        );
        renderMemberTable(filteredMembers);
        updateMemberCount(filteredMembers.length);
    } else {
        // 重新加载完整列表
        loadMembers(includeInactive);
    }
}

// 加载邀请码列表
function loadInvitationCodes() {
    $.ajax({
        url: `/api/ClassManagementApi/${currentClassId}/invitation-codes`,
        method: 'GET',
        data: {
            includeInactive: false
        },
        success: function(invitationCodes) {
            const select = $('#invitationCodeSelect');
            select.empty();
            select.append('<option value="">选择邀请码（可选）</option>');

            invitationCodes.forEach(code => {
                if (code.isActive) {
                    select.append(`<option value="${code.id}">${code.code}</option>`);
                }
            });
        },
        error: function(xhr) {
            console.error('加载邀请码失败：', getErrorMessage(xhr));
        }
    });
}

// 添加班级成员
function addMember() {
    const form = $('#addMemberForm');
    const formData = {
        studentId: parseInt($('#studentSelect').val()),
        invitationCodeId: $('#invitationCodeSelect').val() ? parseInt($('#invitationCodeSelect').val()) : null
    };

    // 验证表单
    if (!formData.studentId) {
        showFieldError('#studentSelect', '请选择学生');
        return;
    }

    // 清除之前的错误
    clearFormErrors(form);

    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>添加中...').prop('disabled', true);

    $.ajax({
        url: `/api/ClassMembersApi/${currentClassId}/members`,
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function() {
            // 关闭模态框
            $('#addMemberModal').modal('hide');

            // 显示成功消息
            showSuccessMessage('成员添加成功！');

            // 刷新成员列表
            loadMembers();
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




// 移除班级成员
function removeMember(memberId) {
    if (!confirm('确定要移除这个班级成员吗？')) {
        return;
    }

    $.ajax({
        url: `/api/ClassMembersApi/${currentClassId}/members/${memberId}`,
        method: 'DELETE',
        success: function() {
            showSuccessMessage('成员移除成功！');
            loadMembers();
        },
        error: function(xhr) {
            showErrorMessage('移除成员失败：' + getErrorMessage(xhr));
        }
    });
}

// 恢复班级成员
function restoreMember(memberId) {
    if (!confirm('确定要恢复这个班级成员吗？')) {
        return;
    }

    $.ajax({
        url: `/api/ClassMembersApi/${currentClassId}/members/${memberId}/restore`,
        method: 'POST',
        success: function() {
            showSuccessMessage('成员恢复成功！');
            loadMembers();
        },
        error: function(xhr) {
            showErrorMessage('恢复成员失败：' + getErrorMessage(xhr));
        }
    });
}



// 更新成员数量显示
function updateMemberCount(count) {
    $('#memberCount').text(count);
}

// 重置添加成员表单
function resetAddMemberForm() {
    const form = $('#addMemberForm');
    form[0].reset();
    clearFormErrors(form);
}

// 工具函数
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatDateTime(dateString) {
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
    // 实现成功消息显示
    alert(message); // 临时实现，可以替换为更好的通知组件
}

function showErrorMessage(message) {
    // 实现错误消息显示
    alert(message); // 临时实现，可以替换为更好的通知组件
}

function getErrorMessage(xhr) {
    if (xhr.responseJSON && xhr.responseJSON.message) {
        return xhr.responseJSON.message;
    }
    return xhr.statusText || '未知错误';
}

function showFieldError(selector, message) {
    const field = $(selector);
    field.addClass('is-invalid');
    field.siblings('.invalid-feedback').text(message);
}

function clearFormErrors(form) {
    form.find('.is-invalid').removeClass('is-invalid');
    form.find('.invalid-feedback').text('');
}

function handleFormError(xhr, form) {
    const response = xhr.responseJSON;
    if (response && response.errors) {
        Object.keys(response.errors).forEach(field => {
            const fieldSelector = `#${field.toLowerCase()}`;
            showFieldError(fieldSelector, response.errors[field][0]);
        });
    } else {
        showErrorMessage('操作失败：' + getErrorMessage(xhr));
    }
}
