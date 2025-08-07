/**
 * 测试导入导出小数分值功能的JavaScript脚本
 * 用于验证WindowsQuestionManagement页面的导入导出功能
 */

// 测试数据 - 包含各种小数分值
const testDecimalScores = [
    { operationType: 'Create', score: 10.5, description: '正常小数分值测试' },
    { operationType: 'Delete', score: 15.25, description: '两位小数测试' },
    { operationType: 'Copy', score: 0.1, description: '最小边界值测试' },
    { operationType: 'Move', score: 100.0, description: '最大边界值测试' },
    { operationType: 'Rename', score: 99.99, description: '精度测试' },
    { operationType: 'CreateShortcut', score: 20.75, description: '快捷方式创建测试' },
    { operationType: 'ModifyProperties', score: 8.33, description: '属性修改测试' },
    { operationType: 'CopyAndRename', score: 12.67, description: '复制重命名测试' }
];

// 无效分值测试数据
const invalidScores = [
    { operationType: 'Create', score: 0.05, description: '小于最小值' },
    { operationType: 'Delete', score: 150.0, description: '大于最大值' },
    { operationType: 'Copy', score: -5.0, description: '负数值' },
    { operationType: 'Move', score: 'abc', description: '非数字值' }
];

/**
 * 测试分值验证功能
 */
function testScoreValidation() {
    console.log('开始测试分值验证功能...');
    
    const results = [];
    
    // 测试有效分值
    testDecimalScores.forEach(test => {
        const isValid = validateDecimalScore(test.score);
        results.push({
            score: test.score,
            expected: true,
            actual: isValid,
            passed: isValid === true,
            description: test.description
        });
    });
    
    // 测试无效分值
    invalidScores.forEach(test => {
        const isValid = validateDecimalScore(test.score);
        results.push({
            score: test.score,
            expected: false,
            actual: isValid,
            passed: isValid === false,
            description: test.description
        });
    });
    
    // 输出结果
    console.log('分值验证测试结果：');
    results.forEach(result => {
        const status = result.passed ? '✅ 通过' : '❌ 失败';
        console.log(`${status} - 分值: ${result.score}, 描述: ${result.description}`);
    });
    
    const passedCount = results.filter(r => r.passed).length;
    console.log(`总计: ${results.length} 个测试, ${passedCount} 个通过, ${results.length - passedCount} 个失败`);
    
    return results;
}

/**
 * 验证小数分值
 */
function validateDecimalScore(score) {
    const numScore = parseFloat(score);
    return !isNaN(numScore) && numScore >= 0.1 && numScore <= 100.0;
}

/**
 * 测试导入功能
 */
async function testImportFunction() {
    console.log('开始测试导入功能...');
    
    try {
        // 创建测试Excel数据
        const testData = createTestExcelData();
        
        // 模拟文件上传
        const formData = new FormData();
        const blob = new Blob([testData], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        formData.append('file', blob, 'test_decimal_scores.xlsx');
        
        // 发送导入请求（需要实际的subjectId）
        const subjectId = getSubjectId(); // 需要从页面获取
        const response = await fetch(`/api/SimplifiedQuestion/import?subjectId=${subjectId}`, {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        console.log('导入测试结果：', result);
        
        if (response.ok) {
            console.log(`✅ 导入成功: ${result.successCount} 个题目`);
            if (result.failCount > 0) {
                console.log(`⚠️ 导入失败: ${result.failCount} 个题目`);
                result.errors.forEach(error => console.log(`   - ${error}`));
            }
        } else {
            console.log(`❌ 导入失败: ${result.message}`);
        }
        
        return result;
    } catch (error) {
        console.error('导入测试失败:', error);
        return null;
    }
}

/**
 * 测试导出功能
 */
async function testExportFunction() {
    console.log('开始测试导出功能...');
    
    try {
        const subjectId = getSubjectId();
        const response = await fetch(`/api/SimplifiedQuestion/export?subjectId=${subjectId}&enabledOnly=false`);
        
        if (response.ok) {
            const blob = await response.blob();
            console.log(`✅ 导出成功: 文件大小 ${blob.size} 字节`);
            
            // 创建下载链接进行验证
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `test_export_${Date.now()}.xlsx`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
            
            return true;
        } else {
            console.log('❌ 导出失败:', response.statusText);
            return false;
        }
    } catch (error) {
        console.error('导出测试失败:', error);
        return false;
    }
}

/**
 * 测试模板下载功能
 */
async function testTemplateDownload() {
    console.log('开始测试模板下载功能...');
    
    try {
        const response = await fetch('/api/SimplifiedQuestion/template');
        
        if (response.ok) {
            const blob = await response.blob();
            console.log(`✅ 模板下载成功: 文件大小 ${blob.size} 字节`);
            
            // 创建下载链接
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `template_test_${Date.now()}.xlsx`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
            
            return true;
        } else {
            console.log('❌ 模板下载失败:', response.statusText);
            return false;
        }
    } catch (error) {
        console.error('模板下载测试失败:', error);
        return false;
    }
}

/**
 * 创建测试Excel数据（模拟）
 */
function createTestExcelData() {
    // 这里应该创建实际的Excel数据
    // 为了简化，返回一个模拟的字符串
    const csvData = [
        'Create,10.5,File,是,测试文件.txt,C:\\Users\\Desktop',
        'Delete,15.25,Folder,否,测试文件夹,C:\\Users\\Desktop',
        'Copy,0.1,File,是,源文件.txt,C:\\Users\\Documents',
        'Move,100.0,File,是,移动文件.txt,C:\\Users\\Downloads'
    ].join('\n');
    
    return csvData;
}

/**
 * 获取当前页面的科目ID
 */
function getSubjectId() {
    // 从页面中获取科目ID
    const subjectElement = document.querySelector('[data-subject-id]');
    if (subjectElement) {
        return subjectElement.getAttribute('data-subject-id');
    }
    
    // 如果没有找到，尝试从URL或其他地方获取
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('subjectId') || '1'; // 默认值
}

/**
 * 运行所有测试
 */
async function runAllTests() {
    console.log('🚀 开始运行所有导入导出小数分值测试...');
    console.log('='.repeat(50));
    
    // 1. 测试分值验证
    const validationResults = testScoreValidation();
    console.log('');
    
    // 2. 测试模板下载
    const templateResult = await testTemplateDownload();
    console.log('');
    
    // 3. 测试导出功能
    const exportResult = await testExportFunction();
    console.log('');
    
    // 4. 测试导入功能（可选，因为需要实际文件）
    // const importResult = await testImportFunction();
    
    console.log('='.repeat(50));
    console.log('📊 测试总结:');
    console.log(`   分值验证: ${validationResults.filter(r => r.passed).length}/${validationResults.length} 通过`);
    console.log(`   模板下载: ${templateResult ? '✅ 成功' : '❌ 失败'}`);
    console.log(`   数据导出: ${exportResult ? '✅ 成功' : '❌ 失败'}`);
    console.log('🎉 测试完成!');
}

/**
 * 在页面加载完成后自动运行测试
 */
if (typeof window !== 'undefined') {
    // 浏览器环境
    window.testDecimalScoreImportExport = {
        runAllTests,
        testScoreValidation,
        testImportFunction,
        testExportFunction,
        testTemplateDownload,
        validateDecimalScore
    };
    
    console.log('小数分值导入导出测试工具已加载');
    console.log('使用 testDecimalScoreImportExport.runAllTests() 运行所有测试');
} else {
    // Node.js环境
    module.exports = {
        runAllTests,
        testScoreValidation,
        validateDecimalScore
    };
}
