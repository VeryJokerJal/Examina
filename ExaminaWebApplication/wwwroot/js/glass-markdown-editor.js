/**
 * Glass Markdown Editor
 * 玻璃拟态风格的Markdown编辑器组件
 */

class GlassMarkdownEditor {
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            placeholder: '请输入内容...',
            autoPreview: true,
            toolbar: true,
            ...options
        };
        
        this.currentMode = 'edit'; // edit, preview, split
        this.textarea = null;
        this.preview = null;
        
        this.init();
    }
    
    init() {
        this.setupElements();
        this.bindEvents();
        this.updatePreview();
    }
    
    setupElements() {
        this.textarea = this.container.querySelector('.markdown-textarea');
        this.preview = this.container.querySelector('.markdown-preview');
        this.tabs = this.container.querySelectorAll('.markdown-tab');
        this.toolbarBtns = this.container.querySelectorAll('.toolbar-btn');
        this.editPane = this.container.querySelector('.edit-pane');
        this.previewPane = this.container.querySelector('.preview-pane');
        this.content = this.container.querySelector('.markdown-editor-content');
    }
    
    bindEvents() {
        // 标签页切换
        this.tabs.forEach(tab => {
            tab.addEventListener('click', (e) => {
                e.preventDefault();
                const mode = tab.dataset.tab;
                this.switchMode(mode);
            });
        });
        
        // 工具栏按钮
        this.toolbarBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                const action = btn.dataset.action;
                this.executeAction(action);
            });
        });
        
        // 文本区域输入事件
        if (this.textarea) {
            this.textarea.addEventListener('input', () => {
                if (this.options.autoPreview) {
                    this.updatePreview();
                }
            });

            // 支持Tab键缩进
            this.textarea.addEventListener('keydown', (e) => {
                if (e.key === 'Tab') {
                    e.preventDefault();
                    this.insertText('    '); // 4个空格缩进
                }


            });
        }
    }
    
    switchMode(mode) {
        this.currentMode = mode;
        
        // 更新标签页状态
        this.tabs.forEach(tab => {
            tab.classList.toggle('active', tab.dataset.tab === mode);
        });
        
        // 更新面板显示
        if (mode === 'split') {
            this.content.classList.add('split-mode');
            this.editPane.classList.add('active');
            this.previewPane.classList.add('active');
            this.updatePreview();
        } else {
            this.content.classList.remove('split-mode');
            this.editPane.classList.toggle('active', mode === 'edit');
            this.previewPane.classList.toggle('active', mode === 'preview');
            
            if (mode === 'preview') {
                this.updatePreview();
            }
        }
    }
    
    executeAction(action) {
        const textarea = this.textarea;
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const selectedText = textarea.value.substring(start, end);

        let replacement = '';
        let cursorOffset = 0;

        switch (action) {
            case 'bold':
                replacement = `**${selectedText || '粗体文本'}**`;
                cursorOffset = selectedText ? 0 : -4;
                break;

            case 'italic':
                replacement = `*${selectedText || '斜体文本'}*`;
                cursorOffset = selectedText ? 0 : -3;
                break;

            case 'heading':
                const lines = (selectedText || '标题文本').split('\n');
                replacement = lines.map(line => `## ${line}`).join('\n');
                cursorOffset = selectedText ? 0 : -4;
                break;

            case 'list':
                const listLines = (selectedText || '列表项').split('\n');
                replacement = listLines.map(line => `- ${line}`).join('\n');
                cursorOffset = selectedText ? 0 : -3;
                break;

            case 'link':
                replacement = `[${selectedText || '链接文本'}](URL)`;
                cursorOffset = selectedText ? -5 : -9;
                break;

            case 'code':
                if (selectedText.includes('\n')) {
                    replacement = `\`\`\`\n${selectedText || '代码块'}\n\`\`\``;
                    cursorOffset = selectedText ? 0 : -7;
                } else {
                    replacement = `\`${selectedText || '代码'}\``;
                    cursorOffset = selectedText ? 0 : -2;
                }
                break;


        }

        if (replacement) {
            this.replaceSelection(replacement);

            // 设置光标位置
            if (cursorOffset !== 0) {
                const newPosition = start + replacement.length + cursorOffset;
                textarea.setSelectionRange(newPosition, newPosition);
            }

            textarea.focus();
            this.updatePreview();
        }
    }
    
    replaceSelection(text) {
        const textarea = this.textarea;
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        
        textarea.value = textarea.value.substring(0, start) + text + textarea.value.substring(end);
        
        // 触发input事件
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }
    
    insertText(text) {
        const textarea = this.textarea;
        const start = textarea.selectionStart;
        
        textarea.value = textarea.value.substring(0, start) + text + textarea.value.substring(start);
        textarea.setSelectionRange(start + text.length, start + text.length);
        
        // 触发input事件
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }
    
    updatePreview() {
        if (!this.preview) return;
        
        const markdown = this.textarea.value.trim();
        
        if (!markdown) {
            this.preview.innerHTML = `
                <div class="preview-placeholder">
                    <i class="bi bi-eye-slash text-muted"></i>
                    <p class="text-muted mb-0">在编辑区域输入内容以查看预览</p>
                </div>
            `;
            return;
        }
        
        // 简单的Markdown解析
        const html = this.parseMarkdown(markdown);
        this.preview.innerHTML = html;
    }
    
    parseMarkdown(markdown) {
        let html = markdown;
        
        // 标题
        html = html.replace(/^### (.*$)/gim, '<h3>$1</h3>');
        html = html.replace(/^## (.*$)/gim, '<h2>$1</h2>');
        html = html.replace(/^# (.*$)/gim, '<h1>$1</h1>');
        
        // 粗体和斜体
        html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');
        
        // 代码
        html = html.replace(/`([^`]+)`/g, '<code>$1</code>');
        html = html.replace(/```([^`]+)```/g, '<pre><code>$1</code></pre>');
        
        // 链接
        html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank">$1</a>');
        
        // 列表
        html = html.replace(/^\- (.+$)/gim, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>)/s, '<ul>$1</ul>');
        
        html = html.replace(/^\d+\. (.+$)/gim, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>)/s, '<ol>$1</ol>');
        
        // 引用
        html = html.replace(/^> (.+$)/gim, '<blockquote>$1</blockquote>');
        
        // 水平线
        html = html.replace(/^---$/gim, '<hr>');
        
        // 段落
        html = html.replace(/\n\n/g, '</p><p>');
        html = '<p>' + html + '</p>';
        
        // 清理空段落
        html = html.replace(/<p><\/p>/g, '');
        html = html.replace(/<p>(<h[1-6]>)/g, '$1');
        html = html.replace(/(<\/h[1-6]>)<\/p>/g, '$1');
        html = html.replace(/<p>(<ul>|<ol>|<blockquote>|<hr>)/g, '$1');
        html = html.replace(/(<\/ul>|<\/ol>|<\/blockquote>|<hr>)<\/p>/g, '$1');
        
        return html;
    }
    
    getValue() {
        return this.textarea ? this.textarea.value : '';
    }
    
    setValue(value) {
        if (this.textarea) {
            this.textarea.value = value;
            this.updatePreview();
        }
    }
    
    focus() {
        if (this.textarea) {
            this.textarea.focus();
        }
    }



    // 获取字符统计信息
    getStats() {
        const content = this.getValue();
        return {
            characters: content.length,
            charactersNoSpaces: content.replace(/\s/g, '').length,
            words: content.trim() ? content.trim().split(/\s+/).length : 0,
            lines: content.split('\n').length,
            paragraphs: content.split(/\n\s*\n/).filter(p => p.trim()).length
        };
    }
}

// 自动初始化
document.addEventListener('DOMContentLoaded', function() {
    const editors = document.querySelectorAll('.glass-markdown-editor');
    editors.forEach(editor => {
        new GlassMarkdownEditor(editor);
    });
});

// 导出类供手动使用
window.GlassMarkdownEditor = GlassMarkdownEditor;
