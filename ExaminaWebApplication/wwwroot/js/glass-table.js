/**
 * Enhanced Glass Table Component
 * 增强的玻璃拟态表格组件
 */

class GlassTable {
    constructor(element, options = {}) {
        this.element = element;
        this.options = {
            sortable: true,
            selectable: true,
            searchable: false,
            pagination: false,
            emptyMessage: '暂无数据',
            loadingMessage: '加载中...',
            ...options
        };
        
        this.sortColumn = null;
        this.sortDirection = 'asc';
        this.selectedRows = new Set();
        this.isLoading = false;
        
        this.init();
    }
    
    init() {
        this.setupTable();
        this.bindEvents();
        this.updateDisplay();
    }
    
    setupTable() {
        // 添加表格容器类
        this.element.classList.add('glass-table');
        
        // 设置可排序的表头
        if (this.options.sortable) {
            this.setupSortableHeaders();
        }
        
        // 设置可选择的行
        if (this.options.selectable) {
            this.setupSelectableRows();
        }
        
        // 添加空状态检查
        this.checkEmptyState();
    }
    
    setupSortableHeaders() {
        const headers = this.element.querySelectorAll('thead th[data-sortable]');
        headers.forEach(header => {
            header.classList.add('sortable');
            header.setAttribute('tabindex', '0');
            header.setAttribute('role', 'button');
            header.setAttribute('aria-label', `排序 ${header.textContent.trim()}`);
        });
    }
    
    setupSelectableRows() {
        const rows = this.element.querySelectorAll('tbody tr');
        rows.forEach((row, index) => {
            row.setAttribute('data-row-index', index);
            row.setAttribute('tabindex', '0');
            row.setAttribute('role', 'button');
            row.setAttribute('aria-label', `选择第 ${index + 1} 行`);
        });
    }
    
    bindEvents() {
        // 排序事件
        if (this.options.sortable) {
            this.element.addEventListener('click', this.handleSort.bind(this));
            this.element.addEventListener('keydown', this.handleSortKeyboard.bind(this));
        }
        
        // 选择事件
        if (this.options.selectable) {
            this.element.addEventListener('click', this.handleRowSelect.bind(this));
            this.element.addEventListener('keydown', this.handleRowSelectKeyboard.bind(this));
        }
        
        // 全选事件
        const selectAllCheckbox = this.element.querySelector('#selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.addEventListener('change', this.handleSelectAll.bind(this));
        }
    }
    
    handleSort(event) {
        const header = event.target.closest('th[data-sortable]');
        if (!header) return;
        
        const column = header.dataset.sortable;
        this.sortTable(column, header);
    }
    
    handleSortKeyboard(event) {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            this.handleSort(event);
        }
    }
    
    sortTable(column, headerElement) {
        // 更新排序状态
        if (this.sortColumn === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortColumn = column;
            this.sortDirection = 'asc';
        }
        
        // 更新表头样式
        this.updateSortHeaders(headerElement);
        
        // 执行排序
        this.performSort(column);
        
        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-table:sort', {
            detail: { column, direction: this.sortDirection }
        }));
    }
    
    updateSortHeaders(activeHeader) {
        // 清除所有排序样式
        const headers = this.element.querySelectorAll('thead th');
        headers.forEach(header => {
            header.classList.remove('sort-asc', 'sort-desc');
        });
        
        // 添加当前排序样式
        activeHeader.classList.add(`sort-${this.sortDirection}`);
    }
    
    performSort(column) {
        const tbody = this.element.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        
        rows.sort((a, b) => {
            const aValue = this.getCellValue(a, column);
            const bValue = this.getCellValue(b, column);
            
            let comparison = 0;
            if (aValue > bValue) comparison = 1;
            if (aValue < bValue) comparison = -1;
            
            return this.sortDirection === 'asc' ? comparison : -comparison;
        });
        
        // 重新排列行
        rows.forEach(row => tbody.appendChild(row));
        
        // 添加排序动画
        this.animateSortedRows(rows);
    }
    
    getCellValue(row, column) {
        const cell = row.querySelector(`[data-column="${column}"]`) || 
                    row.cells[parseInt(column)] ||
                    row.querySelector(`td:nth-child(${parseInt(column) + 1})`);
        
        if (!cell) return '';
        
        const value = cell.textContent.trim();
        
        // 尝试转换为数字
        const numValue = parseFloat(value);
        if (!isNaN(numValue)) return numValue;
        
        // 尝试转换为日期
        const dateValue = new Date(value);
        if (!isNaN(dateValue.getTime())) return dateValue.getTime();
        
        return value.toLowerCase();
    }
    
    animateSortedRows(rows) {
        rows.forEach((row, index) => {
            row.style.transform = 'translateX(-20px)';
            row.style.opacity = '0.7';
            
            setTimeout(() => {
                row.style.transform = '';
                row.style.opacity = '';
            }, index * 50 + 100);
        });
    }
    
    handleRowSelect(event) {
        const row = event.target.closest('tbody tr');
        if (!row) return;
        
        const rowIndex = row.dataset.rowIndex;
        this.toggleRowSelection(row, rowIndex);
    }
    
    handleRowSelectKeyboard(event) {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            this.handleRowSelect(event);
        }
    }
    
    toggleRowSelection(row, rowIndex) {
        const checkbox = row.querySelector('input[type="checkbox"]');
        
        if (this.selectedRows.has(rowIndex)) {
            this.selectedRows.delete(rowIndex);
            row.classList.remove('selected');
            if (checkbox) checkbox.checked = false;
        } else {
            this.selectedRows.add(rowIndex);
            row.classList.add('selected');
            if (checkbox) checkbox.checked = true;
        }
        
        this.updateSelectAllState();
        
        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-table:select', {
            detail: { 
                selectedRows: Array.from(this.selectedRows),
                totalSelected: this.selectedRows.size
            }
        }));
    }
    
    handleSelectAll(event) {
        const isChecked = event.target.checked;
        const rows = this.element.querySelectorAll('tbody tr');
        
        if (isChecked) {
            rows.forEach((row, index) => {
                this.selectedRows.add(index.toString());
                row.classList.add('selected');
                const checkbox = row.querySelector('input[type="checkbox"]');
                if (checkbox) checkbox.checked = true;
            });
        } else {
            this.selectedRows.clear();
            rows.forEach(row => {
                row.classList.remove('selected');
                const checkbox = row.querySelector('input[type="checkbox"]');
                if (checkbox) checkbox.checked = false;
            });
        }
        
        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-table:select-all', {
            detail: { 
                selectedRows: Array.from(this.selectedRows),
                totalSelected: this.selectedRows.size,
                isSelectAll: isChecked
            }
        }));
    }
    
    updateSelectAllState() {
        const selectAllCheckbox = this.element.querySelector('#selectAll');
        if (!selectAllCheckbox) return;
        
        const totalRows = this.element.querySelectorAll('tbody tr').length;
        const selectedCount = this.selectedRows.size;
        
        if (selectedCount === 0) {
            selectAllCheckbox.checked = false;
            selectAllCheckbox.indeterminate = false;
        } else if (selectedCount === totalRows) {
            selectAllCheckbox.checked = true;
            selectAllCheckbox.indeterminate = false;
        } else {
            selectAllCheckbox.checked = false;
            selectAllCheckbox.indeterminate = true;
        }
    }
    
    checkEmptyState() {
        const tbody = this.element.querySelector('tbody');
        const rows = tbody.querySelectorAll('tr');
        
        if (rows.length === 0) {
            this.showEmptyState();
        }
    }
    
    showEmptyState() {
        const tbody = this.element.querySelector('tbody');
        const colCount = this.element.querySelectorAll('thead th').length;
        
        const emptyRow = document.createElement('tr');
        emptyRow.innerHTML = `
            <td colspan="${colCount}" class="glass-table-empty">
                <i class="bi bi-inbox"></i>
                <h5>暂无数据</h5>
                <p>${this.options.emptyMessage}</p>
            </td>
        `;
        
        tbody.appendChild(emptyRow);
    }
    
    setLoading(loading) {
        this.isLoading = loading;
        
        if (loading) {
            this.element.classList.add('loading');
        } else {
            this.element.classList.remove('loading');
        }
    }
    
    updateDisplay() {
        // 更新选择状态
        this.updateSelectAllState();
    }
    
    // 公共API方法
    getSelectedRows() {
        return Array.from(this.selectedRows);
    }
    
    clearSelection() {
        this.selectedRows.clear();
        const rows = this.element.querySelectorAll('tbody tr');
        rows.forEach(row => {
            row.classList.remove('selected');
            const checkbox = row.querySelector('input[type="checkbox"]');
            if (checkbox) checkbox.checked = false;
        });
        this.updateSelectAllState();
    }
    
    selectRow(rowIndex) {
        const row = this.element.querySelector(`tbody tr[data-row-index="${rowIndex}"]`);
        if (row) {
            this.toggleRowSelection(row, rowIndex.toString());
        }
    }
    
    refresh() {
        this.setupTable();
        this.updateDisplay();
    }
}

// 自动初始化
document.addEventListener('DOMContentLoaded', function() {
    const tables = document.querySelectorAll('table[data-glass-table]');
    tables.forEach(table => {
        const options = {
            sortable: table.hasAttribute('data-sortable'),
            selectable: table.hasAttribute('data-selectable'),
            searchable: table.hasAttribute('data-searchable'),
            pagination: table.hasAttribute('data-pagination')
        };
        
        new GlassTable(table, options);
    });
});

// 导出类供手动使用
window.GlassTable = GlassTable;
