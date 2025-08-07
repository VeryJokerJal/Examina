/**
 * Glassmorphism Select Component
 * 玻璃拟态风格的下拉选择框组件
 */

class GlassSelect {
    constructor(element, options = {}) {
        this.element = element;

        // 检查是否已经初始化过
        if (element.hasAttribute('data-glass-initialized')) {
            console.warn('GlassSelect already initialized for element:', element);
            return element._glassSelectInstance;
        }

        this.options = {
            searchable: false,
            multiple: false,
            placeholder: '请选择...',
            noResultsText: '没有找到匹配的选项',
            loadingText: '加载中...',
            ...options
        };

        this.isOpen = false;
        this.selectedValues = [];
        this.filteredOptions = [];
        this.focusedIndex = -1;

        // 标记为已初始化
        element.setAttribute('data-glass-initialized', 'true');
        element._glassSelectInstance = this;

        this.init();
    }
    
    init() {
        this.createStructure();
        this.bindEvents();
        this.updateDisplay();
    }
    
    createStructure() {
        // 获取原始select的选项
        const originalOptions = Array.from(this.element.querySelectorAll('option'));
        
        // 创建自定义结构
        const container = document.createElement('div');
        container.className = `glass-select ${this.options.multiple ? 'multiple' : ''}`;
        
        // 创建输入框
        const input = document.createElement('div');
        input.className = 'glass-select-input';
        input.setAttribute('tabindex', '0');
        input.setAttribute('role', 'combobox');
        input.setAttribute('aria-expanded', 'false');
        input.setAttribute('aria-haspopup', 'listbox');
        
        const valueDisplay = document.createElement('span');
        valueDisplay.className = 'glass-select-value';
        
        const arrow = document.createElement('span');
        arrow.className = 'glass-select-arrow';
        
        input.appendChild(valueDisplay);
        input.appendChild(arrow);
        
        // 创建下拉框
        const dropdown = document.createElement('div');
        dropdown.className = 'glass-select-dropdown';
        dropdown.setAttribute('role', 'listbox');
        
        // 创建搜索框（如果启用）
        if (this.options.searchable) {
            const searchContainer = document.createElement('div');
            const searchInput = document.createElement('input');
            searchInput.type = 'text';
            searchInput.className = 'glass-select-search';
            searchInput.placeholder = '搜索选项...';
            searchContainer.appendChild(searchInput);
            dropdown.appendChild(searchContainer);
        }
        
        // 创建选项列表
        const optionsList = document.createElement('ul');
        optionsList.className = 'glass-select-options';
        
        // 转换原始选项
        this.originalOptions = originalOptions.map(option => ({
            value: option.value,
            text: option.textContent,
            disabled: option.disabled,
            selected: option.selected
        }));
        
        this.filteredOptions = [...this.originalOptions];
        this.renderOptions(optionsList);
        
        dropdown.appendChild(optionsList);
        container.appendChild(input);
        container.appendChild(dropdown);
        
        // 替换原始select
        this.element.style.display = 'none';
        this.element.parentNode.insertBefore(container, this.element);
        
        // 保存引用
        this.container = container;
        this.input = input;
        this.valueDisplay = valueDisplay;
        this.dropdown = dropdown;
        this.optionsList = optionsList;
        this.searchInput = dropdown.querySelector('.glass-select-search');
        
        // 初始化选中值
        this.selectedValues = this.originalOptions
            .filter(option => option.selected)
            .map(option => option.value);
    }
    
    renderOptions(container) {
        container.innerHTML = '';
        
        if (this.filteredOptions.length === 0) {
            const noResults = document.createElement('li');
            noResults.className = 'glass-select-no-results';
            noResults.textContent = this.options.noResultsText;
            container.appendChild(noResults);
            return;
        }
        
        this.filteredOptions.forEach((option, index) => {
            const li = document.createElement('li');
            li.className = 'glass-select-option';
            li.setAttribute('data-value', option.value);
            li.setAttribute('data-index', index);
            li.setAttribute('role', 'option');
            li.textContent = option.text;
            
            if (option.disabled) {
                li.classList.add('disabled');
                li.setAttribute('aria-disabled', 'true');
            }
            
            if (this.selectedValues.includes(option.value)) {
                li.classList.add('selected');
                li.setAttribute('aria-selected', 'true');
            }
            
            container.appendChild(li);
        });
    }
    
    bindEvents() {
        // 点击输入框切换下拉状态
        this.input.addEventListener('click', (e) => {
            e.preventDefault();
            this.toggle();
        });
        
        // 键盘导航
        this.input.addEventListener('keydown', (e) => {
            this.handleKeydown(e);
        });
        
        // 选项点击
        this.optionsList.addEventListener('click', (e) => {
            const option = e.target.closest('.glass-select-option');
            if (option && !option.classList.contains('disabled')) {
                this.selectOption(option.dataset.value);
            }
        });
        
        // 搜索功能
        if (this.searchInput) {
            this.searchInput.addEventListener('input', (e) => {
                this.filterOptions(e.target.value);
            });
            
            this.searchInput.addEventListener('keydown', (e) => {
                if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                    e.preventDefault();
                    this.input.focus();
                    this.handleKeydown(e);
                }
            });
        }
        
        // 点击外部关闭
        document.addEventListener('click', (e) => {
            if (!this.container.contains(e.target)) {
                this.close();
            }
        });
        
        // 失去焦点关闭
        this.input.addEventListener('blur', (e) => {
            // 延迟关闭，允许点击选项
            setTimeout(() => {
                if (!this.container.contains(document.activeElement)) {
                    this.close();
                }
            }, 150);
        });
    }
    
    handleKeydown(e) {
        switch (e.key) {
            case 'Enter':
            case ' ':
                e.preventDefault();
                if (this.isOpen) {
                    if (this.focusedIndex >= 0) {
                        const option = this.filteredOptions[this.focusedIndex];
                        if (option && !option.disabled) {
                            this.selectOption(option.value);
                        }
                    }
                } else {
                    this.open();
                }
                break;
                
            case 'Escape':
                e.preventDefault();
                this.close();
                break;
                
            case 'ArrowDown':
                e.preventDefault();
                if (!this.isOpen) {
                    this.open();
                } else {
                    this.focusNext();
                }
                break;
                
            case 'ArrowUp':
                e.preventDefault();
                if (this.isOpen) {
                    this.focusPrevious();
                }
                break;
                
            case 'Home':
                e.preventDefault();
                if (this.isOpen) {
                    this.focusedIndex = 0;
                    this.updateFocus();
                }
                break;
                
            case 'End':
                e.preventDefault();
                if (this.isOpen) {
                    this.focusedIndex = this.filteredOptions.length - 1;
                    this.updateFocus();
                }
                break;
        }
    }
    
    focusNext() {
        const availableOptions = this.filteredOptions.filter(opt => !opt.disabled);
        if (availableOptions.length === 0) return;
        
        do {
            this.focusedIndex = (this.focusedIndex + 1) % this.filteredOptions.length;
        } while (this.filteredOptions[this.focusedIndex]?.disabled);
        
        this.updateFocus();
    }
    
    focusPrevious() {
        const availableOptions = this.filteredOptions.filter(opt => !opt.disabled);
        if (availableOptions.length === 0) return;
        
        do {
            this.focusedIndex = this.focusedIndex <= 0 
                ? this.filteredOptions.length - 1 
                : this.focusedIndex - 1;
        } while (this.filteredOptions[this.focusedIndex]?.disabled);
        
        this.updateFocus();
    }
    
    updateFocus() {
        const options = this.optionsList.querySelectorAll('.glass-select-option');
        options.forEach((option, index) => {
            option.classList.toggle('focused', index === this.focusedIndex);
        });
        
        // 滚动到焦点选项
        if (this.focusedIndex >= 0 && options[this.focusedIndex]) {
            options[this.focusedIndex].scrollIntoView({
                block: 'nearest',
                behavior: 'smooth'
            });
        }
    }
    
    filterOptions(searchTerm) {
        const term = searchTerm.toLowerCase();
        this.filteredOptions = this.originalOptions.filter(option =>
            option.text.toLowerCase().includes(term)
        );
        
        this.renderOptions(this.optionsList);
        this.focusedIndex = -1;
    }
    
    selectOption(value) {
        if (this.options.multiple) {
            const index = this.selectedValues.indexOf(value);
            if (index > -1) {
                this.selectedValues.splice(index, 1);
            } else {
                this.selectedValues.push(value);
            }
        } else {
            this.selectedValues = [value];
            this.close();
        }
        
        this.updateOriginalSelect();
        this.updateDisplay();
        this.renderOptions(this.optionsList);
        
        // 触发change事件
        this.element.dispatchEvent(new Event('change', { bubbles: true }));
    }
    
    updateOriginalSelect() {
        const options = this.element.querySelectorAll('option');
        options.forEach(option => {
            option.selected = this.selectedValues.includes(option.value);
        });
    }
    
    updateDisplay() {
        if (this.selectedValues.length === 0) {
            this.valueDisplay.textContent = this.options.placeholder;
            this.valueDisplay.className = 'glass-select-value glass-select-placeholder';
        } else {
            const selectedTexts = this.selectedValues.map(value => {
                const option = this.originalOptions.find(opt => opt.value === value);
                return option ? option.text : value;
            });
            
            this.valueDisplay.textContent = selectedTexts.join(', ');
            this.valueDisplay.className = 'glass-select-value';
        }
    }
    
    open() {
        if (this.isOpen) return;
        
        this.isOpen = true;
        this.container.classList.add('is-open');
        this.input.setAttribute('aria-expanded', 'true');
        
        // 重置搜索
        if (this.searchInput) {
            this.searchInput.value = '';
            this.filterOptions('');
            setTimeout(() => this.searchInput.focus(), 100);
        }
        
        this.focusedIndex = this.selectedValues.length > 0 
            ? this.filteredOptions.findIndex(opt => opt.value === this.selectedValues[0])
            : -1;
        
        this.updateFocus();
    }
    
    close() {
        if (!this.isOpen) return;
        
        this.isOpen = false;
        this.container.classList.remove('is-open');
        this.input.setAttribute('aria-expanded', 'false');
        this.focusedIndex = -1;
        
        // 清除焦点状态
        const options = this.optionsList.querySelectorAll('.glass-select-option');
        options.forEach(option => option.classList.remove('focused'));
    }
    
    toggle() {
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
    }
    
    // 公共API方法
    setValue(value) {
        if (Array.isArray(value)) {
            this.selectedValues = [...value];
        } else {
            this.selectedValues = value ? [value] : [];
        }
        
        this.updateOriginalSelect();
        this.updateDisplay();
        this.renderOptions(this.optionsList);
    }
    
    getValue() {
        return this.options.multiple ? this.selectedValues : this.selectedValues[0] || null;
    }
    
    disable() {
        this.container.classList.add('disabled');
        this.input.setAttribute('tabindex', '-1');
        this.input.setAttribute('aria-disabled', 'true');
    }
    
    enable() {
        this.container.classList.remove('disabled');
        this.input.setAttribute('tabindex', '0');
        this.input.removeAttribute('aria-disabled');
    }
    
    destroy() {
        // 清理DOM
        if (this.container) {
            this.container.remove();
        }

        // 恢复原始元素
        this.element.style.display = '';

        // 清理初始化标记
        this.element.removeAttribute('data-glass-initialized');
        delete this.element._glassSelectInstance;
    }
}

// 自动初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeGlassSelects();
});

// 初始化Glass-Select组件的函数
function initializeGlassSelects(container = document) {
    // 初始化所有带有 data-glass-select 属性且未初始化的 select 元素
    const selects = container.querySelectorAll('select[data-glass-select]:not([data-glass-initialized])');
    selects.forEach(select => {
        // 检查是否使用原生实现
        const useNative = select.hasAttribute('data-use-native') ||
                         select.hasAttribute('data-glass-native') ||
                         window.glassSelectUseNative === true;

        if (useNative && typeof GlassSelectNative !== 'undefined') {
            // 使用新的原生实现
            const options = {
                variant: select.getAttribute('data-variant') || 'default',
                size: select.getAttribute('data-size') || 'default',
                enhanced: select.hasAttribute('data-searchable') || select.hasAttribute('data-enhanced')
            };

            // 添加原生标记
            select.setAttribute('data-glass-select-native', 'true');
            new GlassSelectNative(select, options);
        } else {
            // 使用原有的div实现
            const options = {
                searchable: select.hasAttribute('data-searchable'),
                multiple: select.hasAttribute('multiple'),
                placeholder: select.getAttribute('data-placeholder') || '请选择...'
            };

            new GlassSelect(select, options);
        }
    });
}

// 清理重复的Glass-Select元素
function cleanupDuplicateGlassSelects(container = document) {
    const glassContainers = container.querySelectorAll('.glass-select');
    glassContainers.forEach(glassContainer => {
        const inputs = glassContainer.querySelectorAll('.glass-select-input');
        if (inputs.length > 1) {
            console.warn('发现重复的glass-select-input，正在清理...');
            // 保留第一个，移除其余的
            for (let i = 1; i < inputs.length; i++) {
                inputs[i].remove();
            }
        }
    });
}

// 全局配置
window.glassSelectConfig = {
    useNative: false, // 全局开关：是否默认使用原生实现
    autoInit: true    // 是否自动初始化
};

// 设置全局使用原生实现
function setGlassSelectUseNative(useNative = true) {
    window.glassSelectConfig.useNative = useNative;
    window.glassSelectUseNative = useNative; // 向后兼容
}

// 兼容性函数：将现有select转换为原生实现
function convertToNativeGlassSelect(selector) {
    const selects = document.querySelectorAll(selector);
    selects.forEach(select => {
        // 如果已经是div实现，先销毁
        if (select._glassSelectInstance) {
            select._glassSelectInstance.destroy();
        }

        // 添加原生标记并初始化
        select.setAttribute('data-glass-select-native', 'true');
        select.setAttribute('data-use-native', 'true');

        if (typeof GlassSelectNative !== 'undefined') {
            const options = {
                variant: select.getAttribute('data-variant') || 'default',
                size: select.getAttribute('data-size') || 'default',
                enhanced: select.hasAttribute('data-searchable') || select.hasAttribute('data-enhanced')
            };
            new GlassSelectNative(select, options);
        }
    });
}

// 导出类供手动使用（避免重复声明）
if (typeof window.GlassSelect === 'undefined') {
    window.GlassSelect = GlassSelect;
    window.initializeGlassSelects = initializeGlassSelects;
    window.cleanupDuplicateGlassSelects = cleanupDuplicateGlassSelects;
    window.setGlassSelectUseNative = setGlassSelectUseNative;
    window.convertToNativeGlassSelect = convertToNativeGlassSelect;
}
