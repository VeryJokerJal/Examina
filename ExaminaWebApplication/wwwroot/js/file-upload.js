/**
 * 文件上传组件
 * 支持拖拽上传、多文件选择、进度显示等功能
 */
class FileUploadComponent {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        if (!this.container) {
            throw new Error(`Container with id '${containerId}' not found`);
        }

        // 默认配置
        this.options = {
            maxFileSize: 100 * 1024 * 1024, // 100MB
            maxFileCount: 10,
            allowedExtensions: ['.zip', '.rar', '.7z', '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx', '.txt', '.rtf', '.json', '.xml', '.jpg', '.jpeg', '.png', '.gif', '.bmp', '.mp4', '.avi', '.mov', '.wmv', '.mp3', '.wav', '.wma'],
            uploadUrl: '/api/fileupload/upload',
            multipleUploadUrl: '/api/fileupload/upload-multiple',
            multiple: true,
            autoUpload: false,
            showPreview: true,
            showProgress: true,
            ...options
        };

        this.files = [];
        this.uploadedFiles = [];
        this.init();
    }

    init() {
        this.createHTML();
        this.bindEvents();
    }

    createHTML() {
        this.container.innerHTML = `
            <div class="file-upload-wrapper">
                <div class="file-upload-area" id="${this.container.id}-upload-area">
                    <div class="upload-icon">
                        <i class="bi bi-cloud-upload-fill"></i>
                    </div>
                    <div class="upload-text">
                        <h5>拖拽文件到此处或点击选择文件</h5>
                        <p>支持 ${this.options.allowedExtensions.join(', ')} 格式</p>
                        <p>最大文件大小: ${this.formatFileSize(this.options.maxFileSize)}</p>
                    </div>
                    <input type="file" id="${this.container.id}-file-input" 
                           ${this.options.multiple ? 'multiple' : ''} 
                           accept="${this.options.allowedExtensions.join(',')}" 
                           style="display: none;">
                    <button type="button" class="glass-btn glass-btn-primary" id="${this.container.id}-select-btn">
                        <i class="bi bi-folder2-open me-1"></i>选择文件
                    </button>
                </div>
                
                <div class="file-list" id="${this.container.id}-file-list" style="display: none;">
                    <div class="file-list-header">
                        <h6>已选择的文件</h6>
                        <div class="file-list-actions">
                            <button type="button" class="glass-btn glass-btn-sm" id="${this.container.id}-clear-btn">
                                <i class="bi bi-trash me-1"></i>清空
                            </button>
                            <button type="button" class="glass-btn glass-btn-primary glass-btn-sm" id="${this.container.id}-upload-btn">
                                <i class="bi bi-upload me-1"></i>上传
                            </button>
                        </div>
                    </div>
                    <div class="file-items" id="${this.container.id}-file-items"></div>
                </div>

                <div class="upload-progress" id="${this.container.id}-progress" style="display: none;">
                    <div class="progress-header">
                        <span class="progress-text">上传进度</span>
                        <span class="progress-percentage">0%</span>
                    </div>
                    <div class="progress">
                        <div class="progress-bar" role="progressbar" style="width: 0%"></div>
                    </div>
                </div>

                <div class="uploaded-files" id="${this.container.id}-uploaded-files" style="display: none;">
                    <div class="uploaded-files-header">
                        <h6>已上传的文件</h6>
                    </div>
                    <div class="uploaded-items" id="${this.container.id}-uploaded-items"></div>
                </div>
            </div>
        `;
    }

    bindEvents() {
        const uploadArea = document.getElementById(`${this.container.id}-upload-area`);
        const fileInput = document.getElementById(`${this.container.id}-file-input`);
        const selectBtn = document.getElementById(`${this.container.id}-select-btn`);
        const clearBtn = document.getElementById(`${this.container.id}-clear-btn`);
        const uploadBtn = document.getElementById(`${this.container.id}-upload-btn`);

        // 拖拽事件
        uploadArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            uploadArea.classList.add('drag-over');
        });

        uploadArea.addEventListener('dragleave', (e) => {
            e.preventDefault();
            uploadArea.classList.remove('drag-over');
        });

        uploadArea.addEventListener('drop', (e) => {
            e.preventDefault();
            uploadArea.classList.remove('drag-over');
            const files = Array.from(e.dataTransfer.files);
            this.handleFiles(files);
        });

        // 点击选择文件
        selectBtn.addEventListener('click', () => {
            fileInput.click();
        });

        uploadArea.addEventListener('click', (e) => {
            if (e.target === uploadArea || e.target.closest('.upload-text')) {
                fileInput.click();
            }
        });

        // 文件选择事件
        fileInput.addEventListener('change', (e) => {
            const files = Array.from(e.target.files);
            this.handleFiles(files);
        });

        // 清空文件
        clearBtn.addEventListener('click', () => {
            this.clearFiles();
        });

        // 上传文件
        uploadBtn.addEventListener('click', () => {
            this.uploadFiles();
        });
    }

    handleFiles(files) {
        const validFiles = [];
        const errors = [];

        files.forEach(file => {
            const validation = this.validateFile(file);
            if (validation.valid) {
                validFiles.push(file);
            } else {
                errors.push(`${file.name}: ${validation.error}`);
            }
        });

        if (errors.length > 0) {
            this.showError(errors.join('\n'));
        }

        if (validFiles.length > 0) {
            this.addFiles(validFiles);
        }
    }

    validateFile(file) {
        // 检查文件大小
        if (file.size > this.options.maxFileSize) {
            return {
                valid: false,
                error: `文件大小超过限制 (${this.formatFileSize(this.options.maxFileSize)})`
            };
        }

        // 检查文件扩展名
        const extension = '.' + file.name.split('.').pop().toLowerCase();
        if (!this.options.allowedExtensions.includes(extension)) {
            return {
                valid: false,
                error: `不支持的文件类型 (${extension})`
            };
        }

        // 检查文件数量
        if (this.files.length >= this.options.maxFileCount) {
            return {
                valid: false,
                error: `文件数量超过限制 (${this.options.maxFileCount})`
            };
        }

        return { valid: true };
    }

    addFiles(files) {
        files.forEach(file => {
            // 避免重复添加
            if (!this.files.find(f => f.name === file.name && f.size === file.size)) {
                this.files.push(file);
            }
        });

        this.updateFileList();
        this.showFileList();

        if (this.options.autoUpload) {
            this.uploadFiles();
        }
    }

    updateFileList() {
        const fileItems = document.getElementById(`${this.container.id}-file-items`);
        fileItems.innerHTML = '';

        this.files.forEach((file, index) => {
            const fileItem = document.createElement('div');
            fileItem.className = 'file-item';
            fileItem.innerHTML = `
                <div class="file-info">
                    <div class="file-icon">
                        <i class="bi ${this.getFileIcon(file.name)}"></i>
                    </div>
                    <div class="file-details">
                        <div class="file-name">${file.name}</div>
                        <div class="file-size">${this.formatFileSize(file.size)}</div>
                    </div>
                </div>
                <div class="file-actions">
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="fileUpload.removeFile(${index})">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            `;
            fileItems.appendChild(fileItem);
        });
    }

    removeFile(index) {
        this.files.splice(index, 1);
        this.updateFileList();

        if (this.files.length === 0) {
            this.hideFileList();
        }
    }

    clearFiles() {
        this.files = [];
        this.hideFileList();
    }

    showFileList() {
        document.getElementById(`${this.container.id}-file-list`).style.display = 'block';
    }

    hideFileList() {
        document.getElementById(`${this.container.id}-file-list`).style.display = 'none';
    }

    async uploadFiles() {
        if (this.files.length === 0) {
            this.showError('请先选择文件');
            return;
        }

        this.showProgress();
        
        try {
            const formData = new FormData();
            this.files.forEach(file => {
                formData.append('files', file);
            });

            const response = await fetch(this.options.multipleUploadUrl, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                this.handleUploadSuccess(result.data);
            } else {
                this.showError(result.message || '上传失败');
            }
        } catch (error) {
            this.showError('上传过程中发生错误: ' + error.message);
        } finally {
            this.hideProgress();
        }
    }

    handleUploadSuccess(data) {
        this.uploadedFiles = [...this.uploadedFiles, ...data.successFiles];
        this.updateUploadedFilesList();
        this.showUploadedFiles();
        this.clearFiles();
        
        this.showSuccess(`上传完成！成功 ${data.successCount} 个，失败 ${data.failedCount} 个`);
        
        if (data.failedFiles.length > 0) {
            const errors = data.failedFiles.map(f => `${f.fileName}: ${f.error}`).join('\n');
            this.showError('部分文件上传失败:\n' + errors);
        }

        // 触发自定义事件
        this.container.dispatchEvent(new CustomEvent('filesUploaded', {
            detail: { uploadedFiles: data.successFiles, failedFiles: data.failedFiles }
        }));
    }

    updateUploadedFilesList() {
        const uploadedItems = document.getElementById(`${this.container.id}-uploaded-items`);
        uploadedItems.innerHTML = '';

        this.uploadedFiles.forEach(file => {
            const fileItem = document.createElement('div');
            fileItem.className = 'uploaded-file-item';
            fileItem.innerHTML = `
                <div class="file-info">
                    <div class="file-icon">
                        <i class="bi ${this.getFileIcon(file.fileName)}"></i>
                    </div>
                    <div class="file-details">
                        <div class="file-name">${file.fileName}</div>
                        <div class="file-size">${this.formatFileSize(file.fileSize)}</div>
                        <div class="file-date">${new Date(file.uploadedAt).toLocaleString()}</div>
                    </div>
                </div>
                <div class="file-actions">
                    <a href="${file.fileUrl}" class="btn btn-sm btn-outline-primary" download>
                        <i class="bi bi-download"></i>
                    </a>
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="fileUpload.deleteUploadedFile(${file.fileId})">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            `;
            uploadedItems.appendChild(fileItem);
        });
    }

    showUploadedFiles() {
        document.getElementById(`${this.container.id}-uploaded-files`).style.display = 'block';
    }

    async deleteUploadedFile(fileId) {
        if (!confirm('确定要删除这个文件吗？')) {
            return;
        }

        try {
            const response = await fetch(`/api/fileupload/${fileId}`, {
                method: 'DELETE'
            });

            const result = await response.json();

            if (result.success) {
                this.uploadedFiles = this.uploadedFiles.filter(f => f.fileId !== fileId);
                this.updateUploadedFilesList();
                this.showSuccess('文件删除成功');
            } else {
                this.showError(result.message || '删除失败');
            }
        } catch (error) {
            this.showError('删除过程中发生错误: ' + error.message);
        }
    }

    showProgress() {
        document.getElementById(`${this.container.id}-progress`).style.display = 'block';
    }

    hideProgress() {
        document.getElementById(`${this.container.id}-progress`).style.display = 'none';
    }

    updateProgress(percentage) {
        const progressBar = this.container.querySelector('.progress-bar');
        const progressText = this.container.querySelector('.progress-percentage');
        
        progressBar.style.width = percentage + '%';
        progressText.textContent = percentage + '%';
    }

    getFileIcon(fileName) {
        const extension = fileName.split('.').pop().toLowerCase();
        const iconMap = {
            'pdf': 'bi-file-earmark-pdf',
            'doc': 'bi-file-earmark-word',
            'docx': 'bi-file-earmark-word',
            'xls': 'bi-file-earmark-excel',
            'xlsx': 'bi-file-earmark-excel',
            'ppt': 'bi-file-earmark-ppt',
            'pptx': 'bi-file-earmark-ppt',
            'zip': 'bi-file-earmark-zip',
            'rar': 'bi-file-earmark-zip',
            '7z': 'bi-file-earmark-zip',
            'jpg': 'bi-file-earmark-image',
            'jpeg': 'bi-file-earmark-image',
            'png': 'bi-file-earmark-image',
            'gif': 'bi-file-earmark-image',
            'bmp': 'bi-file-earmark-image',
            'mp4': 'bi-file-earmark-play',
            'avi': 'bi-file-earmark-play',
            'mov': 'bi-file-earmark-play',
            'wmv': 'bi-file-earmark-play',
            'mp3': 'bi-file-earmark-music',
            'wav': 'bi-file-earmark-music',
            'wma': 'bi-file-earmark-music',
            'txt': 'bi-file-earmark-text',
            'rtf': 'bi-file-earmark-text',
            'json': 'bi-file-earmark-code',
            'xml': 'bi-file-earmark-code'
        };
        
        return iconMap[extension] || 'bi-file-earmark';
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    showSuccess(message) {
        this.showNotification(message, 'success');
    }

    showError(message) {
        this.showNotification(message, 'error');
    }

    showNotification(message, type) {
        // 这里可以集成现有的通知系统
        if (type === 'error') {
            alert('错误: ' + message);
        } else {
            alert('成功: ' + message);
        }
    }
}

// 全局变量，用于在HTML中调用
let fileUpload;
