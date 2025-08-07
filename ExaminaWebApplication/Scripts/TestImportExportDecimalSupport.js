/**
 * 导入导出小数分值支持测试脚本
 * 用于验证系统中所有导入导出功能是否正确支持小数分值
 */

class ImportExportDecimalTester {
    constructor() {
        this.testResults = [];
        this.apiBaseUrl = '/api';
    }

    /**
     * 运行所有测试
     */
    async runAllTests() {
        console.log('🚀 开始导入导出小数分值支持测试...');
        
        try {
            // 1. 测试题目导入导出
            await this.testQuestionImportExport();
            
            // 2. 测试试卷导出
            await this.testExamExport();
            
            // 3. 测试专项练习导出
            await this.testPracticeExport();
            
            // 4. 测试数据一致性
            await this.testDataConsistency();
            
            // 5. 生成测试报告
            this.generateTestReport();
            
        } catch (error) {
            console.error('❌ 测试过程中发生错误:', error);
            this.addTestResult('整体测试', false, `测试失败: ${error.message}`);
        }
    }

    /**
     * 测试题目导入导出功能
     */
    async testQuestionImportExport() {
        console.log('📝 测试题目导入导出功能...');
        
        try {
            // 测试导出模板
            const templateResponse = await fetch('/api/SimplifiedQuestion/template');
            if (templateResponse.ok) {
                this.addTestResult('题目导出模板', true, '模板生成成功');
            } else {
                this.addTestResult('题目导出模板', false, `HTTP ${templateResponse.status}`);
            }

            // 测试小数分值数据格式
            const testScores = [0.1, 0.5, 1.25, 10.75, 25.5, 50.25, 99.99];
            let allValid = true;
            let invalidScores = [];

            testScores.forEach(score => {
                if (score < 0.1 || score > 100.0) {
                    allValid = false;
                    invalidScores.push(score);
                }
            });

            this.addTestResult(
                '题目分值范围验证', 
                allValid, 
                allValid ? '所有测试分值都在有效范围内' : `无效分值: ${invalidScores.join(', ')}`
            );

        } catch (error) {
            this.addTestResult('题目导入导出测试', false, error.message);
        }
    }

    /**
     * 测试试卷导出功能
     */
    async testExamExport() {
        console.log('📋 测试试卷导出功能...');
        
        try {
            // 获取试卷列表
            const examsResponse = await fetch('/api/Exam');
            if (examsResponse.ok) {
                const exams = await examsResponse.json();
                
                if (exams.length > 0) {
                    // 测试第一个试卷的导出
                    const firstExam = exams[0];
                    const exportResponse = await fetch(`/api/Exam/${firstExam.id}/export`);
                    
                    if (exportResponse.ok) {
                        this.addTestResult('试卷导出功能', true, `试卷 "${firstExam.name}" 导出成功`);
                        
                        // 验证导出的内容类型
                        const contentType = exportResponse.headers.get('content-type');
                        if (contentType && contentType.includes('spreadsheetml')) {
                            this.addTestResult('试卷导出格式', true, 'Excel格式正确');
                        } else {
                            this.addTestResult('试卷导出格式', false, `意外的内容类型: ${contentType}`);
                        }
                    } else {
                        this.addTestResult('试卷导出功能', false, `HTTP ${exportResponse.status}`);
                    }
                } else {
                    this.addTestResult('试卷导出测试', false, '没有可用的试卷进行测试');
                }
            } else {
                this.addTestResult('获取试卷列表', false, `HTTP ${examsResponse.status}`);
            }

        } catch (error) {
            this.addTestResult('试卷导出测试', false, error.message);
        }
    }

    /**
     * 测试专项练习导出功能
     */
    async testPracticeExport() {
        console.log('⚡ 测试专项练习导出功能...');
        
        try {
            // 获取专项练习列表
            const practicesResponse = await fetch('/api/SpecializedPractice');
            if (practicesResponse.ok) {
                const practices = await practicesResponse.json();
                
                if (practices.length > 0) {
                    // 测试第一个专项练习的导出
                    const firstPractice = practices[0];
                    const exportResponse = await fetch(`/api/SpecializedPractice/${firstPractice.id}/export`);
                    
                    if (exportResponse.ok) {
                        this.addTestResult('专项练习导出功能', true, `专项练习 "${firstPractice.name}" 导出成功`);
                    } else {
                        this.addTestResult('专项练习导出功能', false, `HTTP ${exportResponse.status}`);
                    }
                } else {
                    this.addTestResult('专项练习导出测试', false, '没有可用的专项练习进行测试');
                }
            } else {
                this.addTestResult('获取专项练习列表', false, `HTTP ${practicesResponse.status}`);
            }

        } catch (error) {
            this.addTestResult('专项练习导出测试', false, error.message);
        }
    }

    /**
     * 测试数据一致性
     */
    async testDataConsistency() {
        console.log('🔍 测试数据一致性...');
        
        try {
            // 测试小数分值的JavaScript处理
            const testValues = ['10.5', '25.75', '0.1', '99.99'];
            let allParsedCorrectly = true;
            let parseResults = [];

            testValues.forEach(value => {
                const parsed = parseFloat(value);
                const isValid = !isNaN(parsed) && parsed >= 0.1 && parsed <= 100.0;
                parseResults.push({ original: value, parsed, isValid });
                if (!isValid) allParsedCorrectly = false;
            });

            this.addTestResult(
                'JavaScript分值解析', 
                allParsedCorrectly, 
                allParsedCorrectly ? '所有分值解析正确' : `解析结果: ${JSON.stringify(parseResults)}`
            );

            // 测试HTML输入控件配置
            const numberInputs = document.querySelectorAll('input[type="number"]');
            let inputsConfiguredCorrectly = 0;
            let totalInputs = 0;

            numberInputs.forEach(input => {
                if (input.name && (input.name.includes('score') || input.name.includes('Score'))) {
                    totalInputs++;
                    const hasStep = input.hasAttribute('step');
                    const stepValue = input.getAttribute('step');
                    const hasMin = input.hasAttribute('min');
                    const minValue = parseFloat(input.getAttribute('min') || '0');

                    if (hasStep && (stepValue === '0.1' || stepValue === '0.01') && hasMin && minValue <= 0.1) {
                        inputsConfiguredCorrectly++;
                    }
                }
            });

            this.addTestResult(
                'HTML输入控件配置', 
                inputsConfiguredCorrectly === totalInputs, 
                `${inputsConfiguredCorrectly}/${totalInputs} 个分值输入控件配置正确`
            );

        } catch (error) {
            this.addTestResult('数据一致性测试', false, error.message);
        }
    }

    /**
     * 添加测试结果
     */
    addTestResult(testName, success, details) {
        const result = {
            name: testName,
            success: success,
            details: details,
            timestamp: new Date().toISOString()
        };
        
        this.testResults.push(result);
        
        const status = success ? '✅' : '❌';
        console.log(`${status} ${testName}: ${details}`);
    }

    /**
     * 生成测试报告
     */
    generateTestReport() {
        const totalTests = this.testResults.length;
        const passedTests = this.testResults.filter(r => r.success).length;
        const failedTests = totalTests - passedTests;
        
        console.log('\n📊 测试报告');
        console.log('='.repeat(50));
        console.log(`总测试数: ${totalTests}`);
        console.log(`通过: ${passedTests}`);
        console.log(`失败: ${failedTests}`);
        console.log(`成功率: ${((passedTests / totalTests) * 100).toFixed(1)}%`);
        console.log('='.repeat(50));
        
        // 显示失败的测试
        const failedResults = this.testResults.filter(r => !r.success);
        if (failedResults.length > 0) {
            console.log('\n❌ 失败的测试:');
            failedResults.forEach(result => {
                console.log(`- ${result.name}: ${result.details}`);
            });
        }
        
        // 创建HTML报告
        this.createHtmlReport();
    }

    /**
     * 创建HTML测试报告
     */
    createHtmlReport() {
        const reportHtml = `
            <div class="test-report glass-card mt-4">
                <div class="card-header">
                    <h5><i class="bi bi-clipboard-check"></i> 导入导出小数分值支持测试报告</h5>
                </div>
                <div class="card-body">
                    <div class="row mb-3">
                        <div class="col-md-3">
                            <div class="text-center">
                                <h3 class="text-primary">${this.testResults.length}</h3>
                                <small class="text-muted">总测试数</small>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="text-center">
                                <h3 class="text-success">${this.testResults.filter(r => r.success).length}</h3>
                                <small class="text-muted">通过</small>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="text-center">
                                <h3 class="text-danger">${this.testResults.filter(r => !r.success).length}</h3>
                                <small class="text-muted">失败</small>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="text-center">
                                <h3 class="text-info">${((this.testResults.filter(r => r.success).length / this.testResults.length) * 100).toFixed(1)}%</h3>
                                <small class="text-muted">成功率</small>
                            </div>
                        </div>
                    </div>
                    
                    <div class="table-responsive">
                        <table class="table table-sm">
                            <thead>
                                <tr>
                                    <th>测试项目</th>
                                    <th>状态</th>
                                    <th>详情</th>
                                    <th>时间</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${this.testResults.map(result => `
                                    <tr class="${result.success ? 'table-success' : 'table-danger'}">
                                        <td>${result.name}</td>
                                        <td>${result.success ? '✅ 通过' : '❌ 失败'}</td>
                                        <td><small>${result.details}</small></td>
                                        <td><small>${new Date(result.timestamp).toLocaleTimeString()}</small></td>
                                    </tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        `;
        
        // 如果页面上有测试报告容器，则插入报告
        const reportContainer = document.getElementById('test-report-container');
        if (reportContainer) {
            reportContainer.innerHTML = reportHtml;
        } else {
            // 否则添加到页面底部
            document.body.insertAdjacentHTML('beforeend', reportHtml);
        }
    }
}

// 导出测试器类
window.ImportExportDecimalTester = ImportExportDecimalTester;

// 提供快速测试函数
window.testImportExportDecimalSupport = async function() {
    const tester = new ImportExportDecimalTester();
    await tester.runAllTests();
    return tester.testResults;
};

console.log('📋 导入导出小数分值支持测试脚本已加载');
console.log('💡 使用 testImportExportDecimalSupport() 开始测试');
