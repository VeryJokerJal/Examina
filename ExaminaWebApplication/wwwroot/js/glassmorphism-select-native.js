/**
 * Glassmorphism Native Select Component
 * 基于原生select元素的玻璃拟态风格组件
 * 简化版本，直接增强原生select而不是替换为div结构
 */

class GlassSelectNative {
    constructor(element, options = {}) {
        this.element = element;

        // 检查是否已经初始化过
        if (element.hasAttribute('data-glass-native-initialized')) {
            console.warn('GlassSelectNative already initialized for element:', element);
            return element._glassSelectNativeInstance;
        }

        this.options = {
            variant: 'default', // default, primary, success, warning, danger
            size: 'default', // sm, default, lg
            enhanced: false, // 是否启用增强功能（搜索等）
            ...options
        };

        // 标记为已初始化
        element.setAttribute('data-glass-native-initialized', 'true');
        element._glassSelectNativeInstance = this;

        this.init();
    }
    
    init() {
        this.applyStyles();
        this.bindEvents();
        
        // 如果启用增强功能，添加额外特性
        if (this.options.enhanced) {
            this.addEnhancedFeatures();
        }
    }
    
    applyStyles() {
        // 添加基础样式类
        this.element.classList.add('glass-select-native');
        
        // 添加尺寸变体
        if (this.options.size !== 'default') {
            this.element.classList.add(`glass-select-${this.options.size}`);
        }
        
        // 添加颜色变体
        if (this.options.variant !== 'default') {
            this.element.classList.add(`glass-select-${this.options.variant}`);
        }
    }
    
    bindEvents() {
        // 基础事件绑定
        this.element.addEventListener('focus', this.handleFocus.bind(this));
        this.element.addEventListener('blur', this.handleBlur.bind(this));
        this.element.addEventListener('change', this.handleChange.bind(this));
        
        // 键盘导航增强
        this.element.addEventListener('keydown', this.handleKeydown.bind(this));
    }
    
    handleFocus(event) {
        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-select-focus', {
            detail: { value: this.element.value, element: this.element }
        }));
    }
    
    handleBlur(event) {
        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-select-blur', {
            detail: { value: this.element.value, element: this.element }
        }));
    }
    
    handleChange(event) {
        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-select-change', {
            detail: { 
                value: this.element.value, 
                selectedOptions: Array.from(this.element.selectedOptions),
                element: this.element 
            }
        }));
    }
    
    handleKeydown(event) {
        // 增强键盘导航
        switch (event.key) {
            case 'Enter':
                // 可以添加自定义Enter行为
                break;
            case 'Escape':
                this.element.blur();
                break;
        }
    }
    
    addEnhancedFeatures() {
        // 为需要搜索功能的select添加增强特性
        if (this.element.hasAttribute('data-searchable')) {
            this.addSearchFeature();
        }
        
        // 为多选添加增强特性
        if (this.element.multiple) {
            this.addMultiSelectFeatures();
        }
    }
    
    addSearchFeature() {
        // 简化的搜索功能实现
        // 注意：原生select的搜索功能有限，这里主要是键盘快速选择
        let searchTimeout;
        let searchString = '';
        
        this.element.addEventListener('keypress', (event) => {
            if (event.key.length === 1) {
                searchString += event.key.toLowerCase();
                
                // 查找匹配的选项
                const options = Array.from(this.element.options);
                const matchingOption = options.find(option => 
                    option.text.toLowerCase().startsWith(searchString)
                );
                
                if (matchingOption) {
                    this.element.value = matchingOption.value;
                    this.element.dispatchEvent(new Event('change'));
                }
                
                // 清除搜索字符串
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    searchString = '';
                }, 1000);
            }
        });
    }
    
    addMultiSelectFeatures() {
        // 检查是否需要自定义多选实现
        if (this.element.hasAttribute('data-custom-multiselect')) {
            this.createCustomMultiSelect();
            return;
        }

        // 为原生多选添加一些增强功能
        this.element.addEventListener('change', () => {
            const selectedCount = this.element.selectedOptions.length;
            const totalCount = this.element.options.length;

            // 触发多选状态变化事件
            this.element.dispatchEvent(new CustomEvent('glass-select-multichange', {
                detail: {
                    selectedCount,
                    totalCount,
                    selectedValues: Array.from(this.element.selectedOptions).map(opt => opt.value),
                    element: this.element
                }
            }));
        });
    }

    createCustomMultiSelect() {
        // 隐藏原生select
        this.element.style.display = 'none';

        // 创建自定义多选结构
        const container = document.createElement('div');
        container.className = 'glass-multiselect';

        // 创建显示区域
        const display = document.createElement('div');
        display.className = 'glass-multiselect-display';
        display.setAttribute('tabindex', '0');
        display.setAttribute('role', 'combobox');
        display.setAttribute('aria-expanded', 'false');
        display.setAttribute('aria-haspopup', 'listbox');

        // 创建下拉选项容器
        const dropdown = document.createElement('div');
        dropdown.className = 'glass-multiselect-dropdown';
        dropdown.setAttribute('role', 'listbox');
        dropdown.setAttribute('aria-multiselectable', 'true');

        // 创建选项列表
        const optionsList = document.createElement('div');
        optionsList.className = 'glass-multiselect-options';

        // 获取原始选项并创建自定义选项
        const originalOptions = Array.from(this.element.querySelectorAll('option'));
        this.multiSelectOptions = originalOptions.map(option => ({
            value: option.value,
            text: option.textContent,
            selected: option.selected,
            disabled: option.disabled
        }));

        this.renderMultiSelectOptions(optionsList);

        dropdown.appendChild(optionsList);
        container.appendChild(display);
        container.appendChild(dropdown);

        // 插入到DOM中
        this.element.parentNode.insertBefore(container, this.element);

        // 保存引用
        this.multiSelectContainer = container;
        this.multiSelectDisplay = display;
        this.multiSelectDropdown = dropdown;
        this.multiSelectOptionsList = optionsList;

        // 绑定事件
        this.bindMultiSelectEvents();

        // 初始化显示
        this.updateMultiSelectDisplay();
    }

    renderMultiSelectOptions(container) {
        container.innerHTML = '';

        this.multiSelectOptions.forEach((option, index) => {
            if (!option.value) return; // 跳过空值选项

            const optionElement = document.createElement('div');
            optionElement.className = `glass-multiselect-option ${option.selected ? 'selected' : ''}`;
            optionElement.setAttribute('data-value', option.value);
            optionElement.setAttribute('data-index', index);
            optionElement.setAttribute('role', 'option');
            optionElement.setAttribute('aria-selected', option.selected);

            if (option.disabled) {
                optionElement.classList.add('disabled');
                optionElement.setAttribute('aria-disabled', 'true');
            }

            optionElement.innerHTML = `
                <div class="glass-multiselect-checkbox"></div>
                <span class="glass-multiselect-option-text">${option.text}</span>
            `;

            container.appendChild(optionElement);
        });
    }

    bindMultiSelectEvents() {
        // 点击显示区域切换下拉状态
        this.multiSelectDisplay.addEventListener('click', (e) => {
            e.preventDefault();
            this.toggleMultiSelect();
        });

        // 键盘导航
        this.multiSelectDisplay.addEventListener('keydown', (e) => {
            this.handleMultiSelectKeydown(e);
        });

        // 选项点击
        this.multiSelectOptionsList.addEventListener('click', (e) => {
            const option = e.target.closest('.glass-multiselect-option');
            if (option && !option.classList.contains('disabled')) {
                this.toggleMultiSelectOption(option.dataset.value);
            }
        });

        // 点击外部关闭
        document.addEventListener('click', (e) => {
            if (!this.multiSelectContainer.contains(e.target)) {
                this.closeMultiSelect();
            }
        });

        // 标签删除事件
        this.multiSelectDisplay.addEventListener('click', (e) => {
            if (e.target.classList.contains('glass-multiselect-tag-remove')) {
                e.stopPropagation();
                const value = e.target.closest('.glass-multiselect-tag').dataset.value;
                this.toggleMultiSelectOption(value);
            }
        });
    }

    toggleMultiSelect() {
        const isOpen = this.multiSelectContainer.classList.contains('is-open');
        if (isOpen) {
            this.closeMultiSelect();
        } else {
            this.openMultiSelect();
        }
    }

    openMultiSelect() {
        this.multiSelectContainer.classList.add('is-open');
        this.multiSelectDisplay.setAttribute('aria-expanded', 'true');
    }

    closeMultiSelect() {
        this.multiSelectContainer.classList.remove('is-open');
        this.multiSelectDisplay.setAttribute('aria-expanded', 'false');
    }

    toggleMultiSelectOption(value) {
        const option = this.multiSelectOptions.find(opt => opt.value === value);
        if (!option || option.disabled) return;

        option.selected = !option.selected;

        // 更新原生select
        const nativeOption = this.element.querySelector(`option[value="${value}"]`);
        if (nativeOption) {
            nativeOption.selected = option.selected;
        }

        // 重新渲染选项和显示
        this.renderMultiSelectOptions(this.multiSelectOptionsList);
        this.updateMultiSelectDisplay();

        // 触发change事件
        this.element.dispatchEvent(new Event('change'));

        // 触发自定义事件
        this.element.dispatchEvent(new CustomEvent('glass-multiselect-change', {
            detail: {
                value: value,
                selected: option.selected,
                selectedValues: this.getMultiSelectValues(),
                element: this.element
            }
        }));
    }

    updateMultiSelectDisplay() {
        const selectedOptions = this.multiSelectOptions.filter(opt => opt.selected);

        if (selectedOptions.length === 0) {
            this.multiSelectDisplay.innerHTML = `
                <span class="glass-multiselect-placeholder">请选择选项...</span>
            `;
        } else {
            const tagsHtml = selectedOptions.map(option => `
                <div class="glass-multiselect-tag" data-value="${option.value}">
                    <span class="glass-multiselect-tag-text" title="${option.text}">${option.text}</span>
                    <button type="button" class="glass-multiselect-tag-remove" aria-label="移除 ${option.text}">×</button>
                </div>
            `).join('');

            this.multiSelectDisplay.innerHTML = tagsHtml;
        }
    }

    getMultiSelectValues() {
        return this.multiSelectOptions.filter(opt => opt.selected).map(opt => opt.value);
    }

    handleMultiSelectKeydown(event) {
        switch (event.key) {
            case 'Enter':
            case ' ':
                event.preventDefault();
                this.toggleMultiSelect();
                break;
            case 'Escape':
                this.closeMultiSelect();
                break;
        }
    }
    
    // 公共方法
    getValue() {
        if (this.multiSelectOptions) {
            // 自定义多选实现
            return this.getMultiSelectValues();
        } else if (this.element.multiple) {
            // 原生多选
            return Array.from(this.element.selectedOptions).map(opt => opt.value);
        }
        return this.element.value;
    }

    setValue(value) {
        if (this.multiSelectOptions) {
            // 自定义多选实现
            this.setMultiSelectValues(Array.isArray(value) ? value : [value]);
        } else if (this.element.multiple && Array.isArray(value)) {
            // 原生多选
            Array.from(this.element.options).forEach(opt => opt.selected = false);
            value.forEach(val => {
                const option = this.element.querySelector(`option[value="${val}"]`);
                if (option) option.selected = true;
            });
        } else {
            this.element.value = value;
        }
        this.element.dispatchEvent(new Event('change'));
    }

    setMultiSelectValues(values) {
        if (!this.multiSelectOptions) return;

        // 重置所有选项
        this.multiSelectOptions.forEach(option => {
            option.selected = values.includes(option.value);
        });

        // 更新原生select
        Array.from(this.element.options).forEach(opt => {
            opt.selected = values.includes(opt.value);
        });

        // 重新渲染
        this.renderMultiSelectOptions(this.multiSelectOptionsList);
        this.updateMultiSelectDisplay();
    }
    
    disable() {
        this.element.disabled = true;
    }
    
    enable() {
        this.element.disabled = false;
    }
    
    destroy() {
        // 清理自定义多选组件
        if (this.multiSelectContainer) {
            this.multiSelectContainer.remove();
            this.element.style.display = '';
        }

        // 移除样式类
        this.element.classList.remove('glass-select-native');
        if (this.options.size !== 'default') {
            this.element.classList.remove(`glass-select-${this.options.size}`);
        }
        if (this.options.variant !== 'default') {
            this.element.classList.remove(`glass-select-${this.options.variant}`);
        }

        // 清理初始化标记
        this.element.removeAttribute('data-glass-native-initialized');
        delete this.element._glassSelectNativeInstance;
    }
}

// 自动初始化函数
function initializeGlassSelectsNative(container = document) {
    // 初始化所有带有 data-glass-select-native 属性的 select 元素
    const selects = container.querySelectorAll('select[data-glass-select-native]:not([data-glass-native-initialized])');
    selects.forEach(select => {
        const options = {
            variant: select.getAttribute('data-variant') || 'default',
            size: select.getAttribute('data-size') || 'default',
            enhanced: select.hasAttribute('data-enhanced')
        };

        new GlassSelectNative(select, options);
    });
}

// 自动初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeGlassSelectsNative();
});

// 导出供手动使用
if (typeof window.GlassSelectNative === 'undefined') {
    window.GlassSelectNative = GlassSelectNative;
    window.initializeGlassSelectsNative = initializeGlassSelectsNative;
}
