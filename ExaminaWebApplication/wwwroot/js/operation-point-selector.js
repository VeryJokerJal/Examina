/**
 * 操作点选择器组件
 * 支持Excel和Windows操作点的选择和配置
 */
class OperationPointSelector {
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            subjectId: null,
            subjectType: null,
            onSelectionChange: null,
            allowMultiple: true,
            ...options
        };
        this.selectedOperationPoints = new Map();
        this.operationPointsData = null;
        
        this.init();
    }

    /**
     * 初始化组件
     */
    init() {
        this.bindEvents();
        if (this.options.subjectId) {
            this.loadOperationPoints(this.options.subjectId);
        }
    }

    /**
     * 绑定事件
     */
    bindEvents() {
        // 操作点选择事件
        this.container.addEventListener('change', (e) => {
            if (e.target.classList.contains('operation-checkbox')) {
                this.handleOperationPointSelection(e.target);
            }
        });

        // 查看详情事件
        this.container.addEventListener('click', (e) => {
            if (e.target.closest('.view-details-btn')) {
                e.preventDefault();
                e.stopPropagation();
                const operationItem = e.target.closest('.operation-point-item');
                this.viewOperationDetails(operationItem);
            }
        });
    }

    /**
     * 加载操作点数据
     */
    async loadOperationPoints(subjectId) {
        try {
            this.showLoading();
            
            const response = await fetch(`/ExamManagement/GetSubjectOperationPoints?subjectId=${subjectId}`);
            const result = await response.json();
            
            if (result.success) {
                this.operationPointsData = result.operationPoints;
                this.options.subjectType = result.subjectType;
                this.renderOperationPoints();
            } else {
                this.showError(result.message || '加载操作点失败');
            }
        } catch (error) {
            console.error('加载操作点失败:', error);
            this.showError('加载操作点失败');
        }
    }

    /**
     * 渲染操作点
     */
    renderOperationPoints() {
        this.hideLoading();

        const subjectType = this.options.subjectType;

        // 清空容器并创建新的结构
        this.container.innerHTML = '';

        if (subjectType === 1) { // Excel
            this.createExcelSelectorStructure();
            this.renderExcelOperationPoints();
        } else if (subjectType === 4) { // Windows
            this.createWindowsSelectorStructure();
            this.renderWindowsOperationPoints();
        } else {
            this.createUnsupportedSubjectStructure();
        }
    }

    /**
     * 渲染Excel操作点
     */
    renderExcelOperationPoints() {
        const data = this.operationPointsData;
        
        this.renderOperationPointCategory('.excel-selector [data-category="basic"]', data.basic || []);
        this.renderOperationPointCategory('.excel-selector [data-category="dataList"]', data.dataList || []);
        this.renderOperationPointCategory('.excel-selector [data-category="chart"]', data.chart || []);
    }

    /**
     * 渲染Windows操作点
     */
    renderWindowsOperationPoints() {
        const data = this.operationPointsData;
        
        this.renderOperationPointCategory('.windows-selector [data-category="create"]', data.create || []);
        this.renderOperationPointCategory('.windows-selector [data-category="copy"]', data.copy || []);
        this.renderOperationPointCategory('.windows-selector [data-category="move"]', data.move || []);
        this.renderOperationPointCategory('.windows-selector [data-category="delete"]', data.delete || []);
        this.renderOperationPointCategory('.windows-selector [data-category="rename"]', data.rename || []);
        this.renderOperationPointCategory('.windows-selector [data-category="shortcut"]', data.shortcut || []);
        this.renderOperationPointCategory('.windows-selector [data-category="property"]', data.property || []);
        this.renderOperationPointCategory('.windows-selector [data-category="copyRename"]', data.copyRename || []);
    }

    /**
     * 渲染操作点分类
     */
    renderOperationPointCategory(selector, operationPoints) {
        const container = this.container.querySelector(selector);
        if (!container) return;

        container.innerHTML = '';

        if (operationPoints.length === 0) {
            container.innerHTML = '<div class="p-3 text-center text-muted"><small>暂无操作点</small></div>';
            return;
        }

        operationPoints.forEach(op => {
            const item = this.createOperationPointItem(op);
            container.appendChild(item);
        });
    }

    /**
     * 创建操作点项目
     */
    createOperationPointItem(operationPoint) {
        const div = document.createElement('div');
        div.className = 'operation-point-item mb-2 p-2 border rounded';
        div.setAttribute('data-operation-number', operationPoint.operationNumber || operationPoint.id);
        div.setAttribute('data-operation-name', operationPoint.name);

        const operationNumber = operationPoint.operationNumber || operationPoint.id;
        const operationName = operationPoint.name || operationPoint.operationName || '未知操作';
        const operationDesc = operationPoint.description || operationPoint.operationDescription || '';
        const operationType = operationPoint.operationType || operationPoint.operationMode || '';
        const paramCount = operationPoint.parameters?.length || 0;

        div.innerHTML = `
            <div class="form-check">
                <input class="form-check-input operation-checkbox" type="checkbox"
                       value="${operationNumber}" id="op-${operationNumber}">
                <label class="form-check-label" for="op-${operationNumber}">
                    <div class="operation-title fw-bold">操作点 ${operationNumber}</div>
                    <div class="operation-description text-muted small">${operationDesc || operationName}</div>
                    <div class="d-flex justify-content-between align-items-center mt-1">
                        <span class="operation-type badge bg-secondary">${operationType}</span>
                        <span class="operation-params text-muted small">${paramCount} 参数</span>
                    </div>
                </label>
            </div>
        `;

        return div;
    }

    /**
     * 处理操作点选择
     */
    handleOperationPointSelection(checkbox) {
        const operationItem = checkbox.closest('.operation-point-item');
        const operationNumber = parseInt(checkbox.value);
        const operationName = operationItem.getAttribute('data-operation-name');

        if (checkbox.checked) {
            // 如果不允许多选，先清除其他选择
            if (!this.options.allowMultiple) {
                this.clearAllSelections();
            }
            
            // 添加选择
            this.selectedOperationPoints.set(operationNumber, {
                operationNumber,
                name: operationName,
                weight: 1.0,
                isEnabled: true
            });
            
            operationItem.classList.add('selected');
        } else {
            // 移除选择
            this.selectedOperationPoints.delete(operationNumber);
            operationItem.classList.remove('selected');
        }

        // 触发选择变化事件
        if (this.options.onSelectionChange) {
            this.options.onSelectionChange(Array.from(this.selectedOperationPoints.values()));
        }
    }

    /**
     * 查看操作点详情
     */
    async viewOperationDetails(operationItem) {
        const operationNumber = parseInt(operationItem.getAttribute('data-operation-number'));
        
        try {
            const response = await fetch(`/ExamManagement/GetOperationPointDetails?subjectType=${this.options.subjectType}&operationNumber=${operationNumber}`);
            const result = await response.json();
            
            if (result.success) {
                this.showOperationDetailsModal(result.operationPoint);
            } else {
                alert('获取操作点详情失败：' + result.message);
            }
        } catch (error) {
            console.error('获取操作点详情失败:', error);
            alert('获取操作点详情失败');
        }
    }

    /**
     * 显示操作点详情模态框
     */
    showOperationDetailsModal(operationPoint) {
        // 这里可以显示一个模态框来展示操作点详情
        // 暂时使用alert代替
        const details = `
操作点编号: ${operationPoint.operationNumber}
操作名称: ${operationPoint.name}
操作描述: ${operationPoint.description || '无'}
参数数量: ${operationPoint.parameters?.length || 0}
        `;
        alert(details);
    }

    /**
     * 清除所有选择
     */
    clearAllSelections() {
        this.selectedOperationPoints.clear();
        this.container.querySelectorAll('.operation-checkbox').forEach(checkbox => {
            checkbox.checked = false;
        });
        this.container.querySelectorAll('.operation-point-item').forEach(item => {
            item.classList.remove('selected');
        });
    }

    /**
     * 获取选中的操作点
     */
    getSelectedOperationPoints() {
        return Array.from(this.selectedOperationPoints.values());
    }

    /**
     * 设置选中的操作点
     */
    setSelectedOperationPoints(operationPoints) {
        this.clearAllSelections();
        
        operationPoints.forEach(op => {
            const checkbox = this.container.querySelector(`input[value="${op.operationNumber}"]`);
            if (checkbox) {
                checkbox.checked = true;
                this.selectedOperationPoints.set(op.operationNumber, op);
                checkbox.closest('.operation-point-item').classList.add('selected');
            }
        });
    }

    /**
     * 显示加载状态
     */
    showLoading() {
        const loadingElement = this.container.querySelector('.loading-state');
        if (loadingElement) {
            loadingElement.style.display = 'block';
        } else {
            // 如果没有找到loading-state元素，创建一个
            this.createLoadingElement();
        }

        // 隐藏其他元素
        this.container.querySelectorAll('.excel-selector, .windows-selector, .unsupported-subject').forEach(el => {
            el.style.display = 'none';
        });
    }

    /**
     * 隐藏加载状态
     */
    hideLoading() {
        const loadingElement = this.container.querySelector('.loading-state');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
    }

    /**
     * 创建加载元素
     */
    createLoadingElement() {
        const loadingHtml = `
            <div class="loading-state text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">加载中...</span>
                </div>
                <p class="mt-2 text-muted">正在加载操作点...</p>
            </div>
        `;
        this.container.innerHTML = loadingHtml;
    }

    /**
     * 显示错误信息
     */
    showError(message) {
        this.hideLoading();
        this.container.innerHTML = `
            <div class="text-center py-4">
                <i class="bi bi-exclamation-triangle text-danger" style="font-size: 3rem;"></i>
                <h5 class="text-danger mt-2">加载失败</h5>
                <p class="text-muted">${message}</p>
                <button class="btn btn-outline-primary" onclick="location.reload()">重新加载</button>
            </div>
        `;
    }

    /**
     * 创建Excel选择器结构
     */
    createExcelSelectorStructure() {
        this.container.innerHTML = `
            <div class="excel-selector">
                <div class="row">
                    <div class="col-md-4">
                        <div class="glass-card border-success mb-3">
                            <div class="card-header bg-success text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-gear me-2"></i>基础操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="basic">
                                <!-- 基础操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="glass-card border-primary mb-3">
                            <div class="card-header bg-primary text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-list-ul me-2"></i>数据清单操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="dataList">
                                <!-- 数据清单操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="glass-card border-warning mb-3">
                            <div class="card-header bg-warning text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-bar-chart me-2"></i>图表操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="chart">
                                <!-- 图表操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * 创建Windows选择器结构
     */
    createWindowsSelectorStructure() {
        this.container.innerHTML = `
            <div class="windows-selector">
                <div class="row">
                    <div class="col-md-6 col-lg-4 mb-3">
                        <div class="glass-card border-success">
                            <div class="card-header bg-success text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-plus-circle me-2"></i>创建操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="create">
                                <!-- 创建操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-4 mb-3">
                        <div class="glass-card border-primary">
                            <div class="card-header bg-primary text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-files me-2"></i>复制操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="copy">
                                <!-- 复制操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-4 mb-3">
                        <div class="glass-card border-warning">
                            <div class="card-header bg-warning text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-arrow-right-square me-2"></i>移动操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="move">
                                <!-- 移动操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-4 mb-3">
                        <div class="glass-card border-danger">
                            <div class="card-header bg-danger text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-trash me-2"></i>删除操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="delete">
                                <!-- 删除操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-4 mb-3">
                        <div class="glass-card border-info">
                            <div class="card-header bg-info text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-pencil-square me-2"></i>重命名操作
                                </h6>
                            </div>
                            <div class="card-body" data-category="rename">
                                <!-- 重命名操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-4 mb-3">
                        <div class="glass-card border-secondary">
                            <div class="card-header bg-secondary text-white">
                                <h6 class="mb-0">
                                    <i class="bi bi-link me-2"></i>快捷方式
                                </h6>
                            </div>
                            <div class="card-body" data-category="shortcut">
                                <!-- 快捷方式操作点将在这里渲染 -->
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * 创建不支持科目的结构
     */
    createUnsupportedSubjectStructure() {
        this.container.innerHTML = `
            <div class="unsupported-subject text-center py-4">
                <i class="bi bi-exclamation-triangle text-warning" style="font-size: 3rem;"></i>
                <h5 class="text-warning mt-2">暂不支持此科目类型</h5>
                <p class="text-muted">当前科目类型暂不支持操作点选择功能</p>
            </div>
        `;
    }
}

// 全局函数，供模板中的按钮调用
window.viewOperationDetails = function(button) {
    // 这个函数会被操作点选择器的事件处理器接管
};

// 导出类
window.OperationPointSelector = OperationPointSelector;
