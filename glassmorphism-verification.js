const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

// Glassmorphism验证配置
const config = {
    baseURL: 'https://localhost:7125',
    screenshotDir: 'glassmorphism-verification-screenshots',
    reportDir: 'glassmorphism-verification-report',
    viewports: [
        { name: 'desktop', width: 1920, height: 1080 },
        { name: 'tablet', width: 768, height: 1024 },
        { name: 'mobile', width: 375, height: 667 }
    ],
    pages: [
        { name: 'home', url: '/', title: '首页' },
        { name: 'exam-management', url: '/ExamManagement', title: '考试管理面板' },
        { name: 'create-exam', url: '/ExamManagement/CreateExam', title: '创建考试' },
        { name: 'exam-list', url: '/ExamManagement/ExamList', title: '考试列表' },
        { name: 'question-bank', url: '/ExamManagement/QuestionBank', title: '题库管理' },

        { name: 'excel-question-management', url: '/ExamManagement/ExcelQuestionManagement', title: 'Excel题目管理' },
        { name: 'windows-question-management', url: '/ExamManagement/WindowsQuestionManagement', title: 'Windows题目管理' },
        { name: 'excel-operations', url: '/ExamManagement/ExcelOperations', title: 'Excel操作' },
        { name: 'validate-exam', url: '/ExamManagement/ValidateExam', title: '验证考试' }
    ]
};

// 创建目录
function ensureDirectoryExists(dirPath) {
    if (!fs.existsSync(dirPath)) {
        fs.mkdirSync(dirPath, { recursive: true });
    }
}

// Glassmorphism验证器
class GlassmorphismValidator {
    constructor() {
        this.issues = [];
    }

    async validateGlassElements(page, pageName, viewport) {
        const issues = [];

        try {
            // 验证玻璃卡片
            const glassCards = await page.evaluate(() => {
                const cards = document.querySelectorAll('.glass-card');
                const results = [];
                
                cards.forEach((card, index) => {
                    const styles = window.getComputedStyle(card);
                    const hasBackdropFilter = styles.backdropFilter && styles.backdropFilter !== 'none';
                    const hasTransparentBg = styles.backgroundColor.includes('rgba') || 
                                           styles.background.includes('rgba');
                    const hasBoxShadow = styles.boxShadow && styles.boxShadow !== 'none';
                    const hasBorderRadius = parseFloat(styles.borderRadius) > 0;
                    
                    results.push({
                        index,
                        className: card.className,
                        hasBackdropFilter,
                        hasTransparentBg,
                        hasBoxShadow,
                        hasBorderRadius,
                        backdropFilter: styles.backdropFilter,
                        backgroundColor: styles.backgroundColor,
                        boxShadow: styles.boxShadow,
                        borderRadius: styles.borderRadius
                    });
                });
                
                return results;
            });

            // 检查玻璃卡片问题
            glassCards.forEach(card => {
                if (!card.hasBackdropFilter) {
                    issues.push({
                        type: 'glassmorphism',
                        severity: 'high',
                        element: 'glass-card',
                        description: `玻璃卡片 ${card.index} 缺少backdrop-filter模糊效果`,
                        details: card,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
                
                if (!card.hasTransparentBg) {
                    issues.push({
                        type: 'glassmorphism',
                        severity: 'high',
                        element: 'glass-card',
                        description: `玻璃卡片 ${card.index} 缺少半透明背景`,
                        details: card,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
                
                if (!card.hasBoxShadow) {
                    issues.push({
                        type: 'glassmorphism',
                        severity: 'medium',
                        element: 'glass-card',
                        description: `玻璃卡片 ${card.index} 缺少阴影效果`,
                        details: card,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
            });

            // 验证导航栏
            const navbarInfo = await page.evaluate(() => {
                const navbar = document.querySelector('.glass-navbar');
                if (!navbar) return null;
                
                const styles = window.getComputedStyle(navbar);
                return {
                    hasBackdropFilter: styles.backdropFilter && styles.backdropFilter !== 'none',
                    hasTransparentBg: styles.backgroundColor.includes('rgba'),
                    position: styles.position,
                    zIndex: styles.zIndex,
                    marginBottom: styles.marginBottom,
                    backdropFilter: styles.backdropFilter,
                    backgroundColor: styles.backgroundColor
                };
            });

            if (navbarInfo) {
                if (!navbarInfo.hasBackdropFilter) {
                    issues.push({
                        type: 'glassmorphism',
                        severity: 'high',
                        element: 'glass-navbar',
                        description: '导航栏缺少backdrop-filter模糊效果',
                        details: navbarInfo,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
                
                if (navbarInfo.position !== 'sticky') {
                    issues.push({
                        type: 'layout',
                        severity: 'medium',
                        element: 'glass-navbar',
                        description: '导航栏未设置sticky定位',
                        details: navbarInfo,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
            }

            // 验证按钮
            const buttonInfo = await page.evaluate(() => {
                const buttons = document.querySelectorAll('.glass-btn');
                const results = [];
                
                buttons.forEach((btn, index) => {
                    const styles = window.getComputedStyle(btn);
                    results.push({
                        index,
                        hasBackdropFilter: styles.backdropFilter && styles.backdropFilter !== 'none',
                        hasTransparentBg: styles.backgroundColor.includes('rgba'),
                        hasTransition: styles.transition && styles.transition !== 'none',
                        backdropFilter: styles.backdropFilter,
                        backgroundColor: styles.backgroundColor
                    });
                });
                
                return results;
            });

            buttonInfo.forEach(btn => {
                if (!btn.hasBackdropFilter) {
                    issues.push({
                        type: 'glassmorphism',
                        severity: 'medium',
                        element: 'glass-btn',
                        description: `玻璃按钮 ${btn.index} 缺少backdrop-filter效果`,
                        details: btn,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
            });

            // 验证表单控件
            const formControlInfo = await page.evaluate(() => {
                const controls = document.querySelectorAll('.glass-form-control');
                const results = [];
                
                controls.forEach((control, index) => {
                    const styles = window.getComputedStyle(control);
                    results.push({
                        index,
                        hasBackdropFilter: styles.backdropFilter && styles.backdropFilter !== 'none',
                        hasTransparentBg: styles.backgroundColor.includes('rgba'),
                        backdropFilter: styles.backdropFilter,
                        backgroundColor: styles.backgroundColor
                    });
                });
                
                return results;
            });

            formControlInfo.forEach(control => {
                if (!control.hasBackdropFilter) {
                    issues.push({
                        type: 'glassmorphism',
                        severity: 'medium',
                        element: 'glass-form-control',
                        description: `表单控件 ${control.index} 缺少backdrop-filter效果`,
                        details: control,
                        page: pageName,
                        viewport: viewport.name
                    });
                }
            });

        } catch (error) {
            issues.push({
                type: 'error',
                severity: 'high',
                description: `Glassmorphism验证时发生错误: ${error.message}`,
                page: pageName,
                viewport: viewport.name
            });
        }

        return issues;
    }

    async validateLayout(page, pageName, viewport) {
        const issues = [];

        try {
            // 检查导航栏与内容重叠
            const layoutInfo = await page.evaluate(() => {
                const navbar = document.querySelector('.glass-navbar');
                const mainContent = document.querySelector('main, .container');
                
                if (!navbar || !mainContent) return null;
                
                const navbarRect = navbar.getBoundingClientRect();
                const contentRect = mainContent.getBoundingClientRect();
                
                return {
                    navbarHeight: navbarRect.height,
                    navbarBottom: navbarRect.bottom,
                    contentTop: contentRect.top,
                    overlap: contentRect.top < navbarRect.bottom,
                    gap: contentRect.top - navbarRect.bottom
                };
            });

            if (layoutInfo && layoutInfo.overlap) {
                issues.push({
                    type: 'layout',
                    severity: 'high',
                    element: 'navbar-content',
                    description: '导航栏与主体内容重叠',
                    details: layoutInfo,
                    page: pageName,
                    viewport: viewport.name
                });
            }

        } catch (error) {
            issues.push({
                type: 'error',
                severity: 'high',
                description: `布局验证时发生错误: ${error.message}`,
                page: pageName,
                viewport: viewport.name
            });
        }

        return issues;
    }
}

// 主验证函数
async function runGlassmorphismVerification() {
    console.log('🔍 开始Glassmorphism设计验证...');
    
    // 创建必要的目录
    ensureDirectoryExists(config.screenshotDir);
    ensureDirectoryExists(config.reportDir);
    
    const browser = await chromium.launch({
        headless: false,
        slowMo: 1000,
        args: ['--ignore-certificate-errors', '--ignore-ssl-errors']
    });
    
    const validator = new GlassmorphismValidator();
    const verificationResults = {
        timestamp: new Date().toISOString(),
        summary: {
            totalPages: config.pages.length,
            totalViewports: config.viewports.length,
            totalTests: config.pages.length * config.viewports.length
        },
        results: [],
        issues: [],
        glassmorphismScore: 0
    };

    try {
        for (const viewport of config.viewports) {
            console.log(`\n📱 验证视口: ${viewport.name} (${viewport.width}x${viewport.height})`);
            
            const context = await browser.newContext({
                viewport: { width: viewport.width, height: viewport.height },
                ignoreHTTPSErrors: true
            });
            
            const page = await context.newPage();
            
            for (const pageConfig of config.pages) {
                console.log(`  📄 验证页面: ${pageConfig.title}`);
                
                try {
                    // 导航到页面
                    await page.goto(`${config.baseURL}${pageConfig.url}`, {
                        waitUntil: 'networkidle',
                        timeout: 30000
                    });
                    
                    // 等待页面加载完成
                    await page.waitForTimeout(3000);
                    
                    // 截图
                    const screenshotPath = path.join(
                        config.screenshotDir, 
                        `${pageConfig.name}-${viewport.name}.png`
                    );
                    await page.screenshot({ 
                        path: screenshotPath, 
                        fullPage: true 
                    });
                    
                    // 验证Glassmorphism效果
                    const glassIssues = await validator.validateGlassElements(page, pageConfig.name, viewport);
                    
                    // 验证布局
                    const layoutIssues = await validator.validateLayout(page, pageConfig.name, viewport);
                    
                    const allIssues = [...glassIssues, ...layoutIssues];
                    
                    const result = {
                        page: pageConfig.name,
                        title: pageConfig.title,
                        url: pageConfig.url,
                        viewport: viewport.name,
                        screenshot: screenshotPath,
                        issues: allIssues,
                        glassmorphismCompliant: allIssues.filter(i => i.type === 'glassmorphism').length === 0,
                        layoutValid: allIssues.filter(i => i.type === 'layout').length === 0,
                        timestamp: new Date().toISOString()
                    };
                    
                    verificationResults.results.push(result);
                    verificationResults.issues.push(...allIssues);
                    
                    const statusIcon = allIssues.length === 0 ? '✅' : '⚠️';
                    console.log(`    ${statusIcon} ${pageConfig.title} - 发现 ${allIssues.length} 个问题`);
                    
                } catch (error) {
                    console.log(`    ❌ ${pageConfig.title} - 错误: ${error.message}`);
                    verificationResults.results.push({
                        page: pageConfig.name,
                        title: pageConfig.title,
                        url: pageConfig.url,
                        viewport: viewport.name,
                        error: error.message,
                        timestamp: new Date().toISOString()
                    });
                }
            }
            
            await context.close();
        }
        
    } finally {
        await browser.close();
    }
    
    // 计算Glassmorphism得分
    const totalElements = verificationResults.results.length;
    const compliantElements = verificationResults.results.filter(r => r.glassmorphismCompliant).length;
    verificationResults.glassmorphismScore = Math.round((compliantElements / totalElements) * 100);
    
    // 生成报告
    await generateVerificationReport(verificationResults);
    
    console.log('\n🎉 Glassmorphism验证完成！');
    console.log(`📊 总共验证了 ${verificationResults.results.length} 个页面/视口组合`);
    console.log(`🐛 发现 ${verificationResults.issues.length} 个问题`);
    console.log(`🏆 Glassmorphism符合度: ${verificationResults.glassmorphismScore}%`);
    console.log(`📁 截图保存在: ${config.screenshotDir}`);
    console.log(`📋 报告保存在: ${config.reportDir}`);
    
    return verificationResults;
}

// 生成验证报告
async function generateVerificationReport(results) {
    // 生成JSON报告
    const jsonReportPath = path.join(config.reportDir, 'glassmorphism-verification-report.json');
    fs.writeFileSync(jsonReportPath, JSON.stringify(results, null, 2));
    
    // 生成HTML报告
    const htmlReportPath = path.join(config.reportDir, 'glassmorphism-verification-report.html');
    
    const issuesByType = results.issues.reduce((acc, issue) => {
        acc[issue.type] = (acc[issue.type] || 0) + 1;
        return acc;
    }, {});
    
    const html = `
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Glassmorphism设计验证报告</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; margin: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; }
        .container { max-width: 1200px; margin: 0 auto; background: rgba(255,255,255,0.1); backdrop-filter: blur(20px); padding: 30px; border-radius: 20px; box-shadow: 0 8px 32px rgba(0,0,0,0.3); }
        .header { text-align: center; margin-bottom: 40px; }
        .score { font-size: 3em; font-weight: bold; color: #4CAF50; }
        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 40px; }
        .summary-card { background: rgba(255,255,255,0.15); padding: 20px; border-radius: 15px; text-align: center; backdrop-filter: blur(10px); }
        .issue { margin: 15px 0; padding: 15px; border-radius: 10px; border-left: 4px solid; backdrop-filter: blur(10px); }
        .issue.high { background: rgba(244, 67, 54, 0.2); border-color: #f44336; }
        .issue.medium { background: rgba(255, 152, 0, 0.2); border-color: #ff9800; }
        .issue.low { background: rgba(76, 175, 80, 0.2); border-color: #4caf50; }
        .page-result { margin: 20px 0; padding: 20px; background: rgba(255,255,255,0.1); border-radius: 15px; backdrop-filter: blur(10px); }
        .screenshot { max-width: 300px; border-radius: 10px; }
        .viewport-badge { display: inline-block; padding: 4px 12px; background: rgba(255,255,255,0.2); border-radius: 20px; font-size: 12px; margin: 2px; }
        .compliant { color: #4CAF50; }
        .non-compliant { color: #f44336; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🎨 Glassmorphism设计验证报告</h1>
            <div class="score">${results.glassmorphismScore}%</div>
            <p>Glassmorphism设计符合度</p>
            <p>生成时间: ${results.timestamp}</p>
        </div>
        
        <div class="summary">
            <div class="summary-card">
                <h3>总页面数</h3>
                <div style="font-size: 2em; color: #4299e1;">${results.summary.totalPages}</div>
            </div>
            <div class="summary-card">
                <h3>测试视口</h3>
                <div style="font-size: 2em; color: #48bb78;">${results.summary.totalViewports}</div>
            </div>
            <div class="summary-card">
                <h3>总测试数</h3>
                <div style="font-size: 2em; color: #ed8936;">${results.summary.totalTests}</div>
            </div>
            <div class="summary-card">
                <h3>发现问题</h3>
                <div style="font-size: 2em; color: #e53e3e;">${results.issues.length}</div>
            </div>
        </div>
        
        <h2>🐛 问题分类统计</h2>
        ${Object.entries(issuesByType).map(([type, count]) => `
            <div class="issue medium">
                <strong>${type.toUpperCase()}</strong>: ${count} 个问题
            </div>
        `).join('')}
        
        <h2>📄 页面验证结果</h2>
        ${results.results.map(result => `
            <div class="page-result">
                <h3>${result.title} <span class="viewport-badge">${result.viewport}</span></h3>
                <p><strong>URL:</strong> ${result.url}</p>
                <p><strong>Glassmorphism符合:</strong> <span class="${result.glassmorphismCompliant ? 'compliant' : 'non-compliant'}">${result.glassmorphismCompliant ? '✅ 是' : '❌ 否'}</span></p>
                <p><strong>布局有效:</strong> <span class="${result.layoutValid ? 'compliant' : 'non-compliant'}">${result.layoutValid ? '✅ 是' : '❌ 否'}</span></p>
                ${result.screenshot ? `<p><strong>截图:</strong> <br><img src="../${result.screenshot}" class="screenshot" alt="Screenshot"></p>` : ''}
                ${result.error ? `<p style="color: #f44336;"><strong>错误:</strong> ${result.error}</p>` : ''}
                <p><strong>问题数量:</strong> ${result.issues ? result.issues.length : 0}</p>
                ${result.issues && result.issues.length > 0 ? `
                    <div style="margin-top: 15px;">
                        <strong>具体问题:</strong>
                        ${result.issues.map(issue => `
                            <div class="issue ${issue.severity}">
                                <strong>${issue.element || issue.type}</strong>: ${issue.description}
                            </div>
                        `).join('')}
                    </div>
                ` : ''}
            </div>
        `).join('')}
    </div>
</body>
</html>`;

    fs.writeFileSync(htmlReportPath, html);
}

// 运行验证
if (require.main === module) {
    runGlassmorphismVerification().catch(console.error);
}

module.exports = { runGlassmorphismVerification, GlassmorphismValidator };
