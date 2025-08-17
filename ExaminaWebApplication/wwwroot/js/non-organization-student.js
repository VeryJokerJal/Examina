// 非组织学生管理JavaScript功能

let currentPage = 1;
let pageSize = 50;

$(document).ready(function() {
    // 初始化页面
    initializeNonOrganizationStudent();
});

// 初始化非组织学生管理功能
function initializeNonOrganizationStudent() {
    // 绑定创建学生表单提交事件
    $('#createStudentForm').on('submit', function(e) {
        e.preventDefault();
        createStudent();
    });

    // 绑定编辑学生表单提交事件
    $('#editStudentForm').on('submit', function(e) {
        e.preventDefault();
        updateStudent();
    });

    // 绑定搜索输入框回车事件
    $('#searchKeyword').on('keypress', function(e) {
        if (e.which === 13) {
            searchStudents();
        }
    });

    // 绑定搜索类型变化事件
    $('#searchType').on('change', function() {
        updateSearchPlaceholder();
    });

    // 绑定包含非激活状态复选框变化事件
    $('#includeInactive').on('change', function() {
        searchStudents();
    });

    // 初始化搜索占位符
    updateSearchPlaceholder();
}

// 更新搜索占位符
function updateSearchPlaceholder() {
    const searchType = $('#searchType').val();
    const placeholder = searchType === 'phone' ? '请输入手机号码...' : '请输入学生姓名...';
    $('#searchKeyword').attr('placeholder', placeholder);
}

// 搜索学生
function searchStudents() {
    const searchType = $('#searchType').val();
    const keyword = $('#searchKeyword').val().trim();
    const includeInactive = $('#includeInactive').is(':checked');
    
    currentPage = 1; // 重置到第一页
    
    if (keyword) {
        // 使用搜索API
        const url = searchType === 'phone' ?
            '/api/NonOrganizationStudentApi/search/phone' :
            '/api/NonOrganizationStudentApi/search/name';
        
        showLoading('#studentsContainer');
        
        $.ajax({
            url: url,
            method: 'GET',
            data: {
                [searchType === 'phone' ? 'phoneNumber' : 'realName']: keyword,
                includeInactive: includeInactive
            },
            success: function(students) {
                renderStudentTable(students);
                updateStatistics(students.length, students.length, 1, 1);
            },
            error: function(xhr) {
                hideLoading('#studentsContainer');
                showErrorMessage('搜索学生失败：' + getErrorMessage(xhr));
            }
        });
    } else {
        // 获取所有学生
        loadStudents(currentPage, includeInactive);
    }
}

// 加载学生列表
function loadStudents(page = 1, includeInactive = false) {
    currentPage = page;
    showLoading('#studentsContainer');
    
    $.ajax({
        url: '/api/NonOrganizationStudentApi',
        method: 'GET',
        data: {
            pageNumber: page,
            pageSize: pageSize,
            includeInactive: includeInactive
        },
        success: function(students) {
            renderStudentTable(students);
            
            // 获取总数
            $.ajax({
                url: '/api/NonOrganizationStudentApi/count',
                method: 'GET',
                data: { includeInactive: includeInactive },
                success: function(totalCount) {
                    const totalPages = Math.ceil(totalCount / pageSize);
                    updateStatistics(totalCount, students.length, page, totalPages);
                    renderPagination(page, totalPages);
                }
            });
        },
        error: function(xhr) {
            hideLoading('#studentsContainer');
            showErrorMessage('获取学生列表失败：' + getErrorMessage(xhr));
        }
    });
}

// 渲染学生表格
function renderStudentTable(students) {
    const container = $('#studentsContainer');
    
    if (students.length === 0) {
        container.html(`
            <div class="text-center py-5">
                <i class="bi bi-person-plus display-1 text-muted"></i>
                <h5 class="text-muted mt-3">暂无学生数据</h5>
                <p class="text-muted">点击上方"添加学生"按钮添加第一个学生</p>
            </div>
        `);
        return;
    }

    let html = `
        <div class="table-responsive">
            <table class="table glass-table">
                <thead>
                    <tr>
                        <th>姓名</th>
                        <th>手机号码</th>
                        <th>创建时间</th>
                        <th>创建者</th>
                        <th>状态</th>
                        <th>操作</th>
                    </tr>
                </thead>
                <tbody>
    `;

    students.forEach(student => {
        html += `
            <tr>
                <td>
                    <strong>${escapeHtml(student.realName)}</strong>
                    ${student.notes ? `<br><small class="text-muted">${escapeHtml(student.notes)}</small>` : ''}
                </td>
                <td>
                    <span class="font-monospace">${escapeHtml(student.phoneNumber)}</span>
                </td>
                <td>
                    <small>${formatDateTime(student.createdAt)}</small>
                </td>
                <td>
                    <small>${escapeHtml(student.creatorUsername)}</small>
                </td>
                <td>
                    <span class="badge ${student.isActive ? 'glass-badge-success' : 'glass-badge-danger'}">
                        ${student.isActive ? '正常' : '已删除'}
                    </span>
                </td>
                <td>
                    <div class="btn-group" role="group">
                        <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-primary" onclick="editStudent(${student.id})" title="编辑">
                            <i class="bi bi-pencil"></i>
                        </button>
                        ${student.isActive ? `
                            <button type="button" class="glass-btn glass-btn-sm glass-btn-outline-danger" onclick="deleteStudent(${student.id})" title="删除">
                                <i class="bi bi-trash"></i>
                            </button>
                        ` : ''}

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
    $('#studentCount').text(students.length);
}

// 更新统计信息
function updateStatistics(totalCount, currentPageCount, currentPageNum, totalPages) {
    $('#totalStudents').text(totalCount);
    $('#currentPageStudents').text(currentPageCount);
    $('.glass-stat-number').eq(2).text(currentPageNum);
    $('.glass-stat-number').eq(3).text(totalPages);
}

// 渲染分页
function renderPagination(currentPageNum, totalPages) {
    if (totalPages <= 1) return;

    let html = `
        <nav aria-label="学生列表分页" class="mt-4">
            <ul class="pagination glass-pagination justify-content-center">
                <li class="page-item ${currentPageNum <= 1 ? 'disabled' : ''}">
                    <a class="page-link glass-page-link" href="#" onclick="changePage(${currentPageNum - 1})">
                        <i class="bi bi-chevron-left"></i>
                    </a>
                </li>
    `;

    const startPage = Math.max(1, currentPageNum - 2);
    const endPage = Math.min(totalPages, currentPageNum + 2);

    for (let i = startPage; i <= endPage; i++) {
        html += `
            <li class="page-item ${i === currentPageNum ? 'active' : ''}">
                <a class="page-link glass-page-link" href="#" onclick="changePage(${i})">${i}</a>
            </li>
        `;
    }

    html += `
                <li class="page-item ${currentPageNum >= totalPages ? 'disabled' : ''}">
                    <a class="page-link glass-page-link" href="#" onclick="changePage(${currentPageNum + 1})">
                        <i class="bi bi-chevron-right"></i>
                    </a>
                </li>
            </ul>
        </nav>
    `;

    $('#studentsContainer').append(html);
}

// 切换页面
function changePage(page) {
    if (page < 1) return;
    const includeInactive = $('#includeInactive').is(':checked');
    loadStudents(page, includeInactive);
}

// 重置搜索
function resetSearch() {
    $('#searchKeyword').val('');
    $('#searchType').val('name');
    $('#includeInactive').prop('checked', false);
    updateSearchPlaceholder();
    loadStudents(1, false);
}

// 创建学生
function createStudent() {
    const form = $('#createStudentForm');
    const formData = {
        realName: $('#studentRealName').val().trim(),
        phoneNumber: $('#studentPhoneNumber').val().trim(),
        notes: $('#studentNotes').val().trim()
    };

    // 验证表单
    if (!formData.realName) {
        showFieldError('#studentRealName', '学生姓名不能为空');
        return;
    }
    if (!formData.phoneNumber) {
        showFieldError('#studentPhoneNumber', '手机号码不能为空');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>添加中...').prop('disabled', true);

    $.ajax({
        url: '/api/NonOrganizationStudentApi',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(student) {
            // 关闭模态框
            $('#createStudentModal').modal('hide');
            
            // 重置表单
            form[0].reset();
            
            // 显示成功消息
            showSuccessMessage('学生添加成功！');
            
            // 刷新学生列表
            const includeInactive = $('#includeInactive').is(':checked');
            loadStudents(currentPage, includeInactive);
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

// 编辑学生
function editStudent(studentId) {
    // 获取学生详情
    $.ajax({
        url: `/api/NonOrganizationStudentApi/${studentId}`,
        method: 'GET',
        success: function(student) {
            // 填充编辑表单
            $('#editStudentId').val(student.id);
            $('#editStudentRealName').val(student.realName);
            $('#editStudentPhoneNumber').val(student.phoneNumber);
            $('#editStudentNotes').val(student.notes || '');
            
            // 显示编辑模态框
            $('#editStudentModal').modal('show');
        },
        error: function(xhr) {
            showErrorMessage('获取学生信息失败：' + getErrorMessage(xhr));
        }
    });
}

// 更新学生
function updateStudent() {
    const form = $('#editStudentForm');
    const studentId = $('#editStudentId').val();
    const formData = {
        realName: $('#editStudentRealName').val().trim(),
        phoneNumber: $('#editStudentPhoneNumber').val().trim(),
        notes: $('#editStudentNotes').val().trim()
    };

    // 验证表单
    if (!formData.realName) {
        showFieldError('#editStudentRealName', '学生姓名不能为空');
        return;
    }
    if (!formData.phoneNumber) {
        showFieldError('#editStudentPhoneNumber', '手机号码不能为空');
        return;
    }

    // 清除之前的错误
    clearFieldErrors(form);
    
    // 显示加载状态
    const submitBtn = form.find('button[type="submit"]');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>保存中...').prop('disabled', true);

    $.ajax({
        url: `/api/NonOrganizationStudentApi/${studentId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(student) {
            // 关闭模态框
            $('#editStudentModal').modal('hide');
            
            // 显示成功消息
            showSuccessMessage('学生信息更新成功！');
            
            // 刷新学生列表
            const includeInactive = $('#includeInactive').is(':checked');
            loadStudents(currentPage, includeInactive);
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

// 删除学生
function deleteStudent(studentId) {
    if (!confirm('确定要删除这个学生吗？删除后可以在"包含已删除"选项中查看。')) {
        return;
    }

    $.ajax({
        url: `/api/NonOrganizationStudentApi/${studentId}`,
        method: 'DELETE',
        success: function() {
            showSuccessMessage('学生已删除');
            const includeInactive = $('#includeInactive').is(':checked');
            loadStudents(currentPage, includeInactive);
        },
        error: function(xhr) {
            showErrorMessage('删除学生失败：' + getErrorMessage(xhr));
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
