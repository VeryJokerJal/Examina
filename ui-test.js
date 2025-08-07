const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

// 测试配置
const config = {
  baseURL: 'http://localhost:5117',
  screenshotDir: 'screenshots',
  reportDir: 'ui-test-report',
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

    { name: 'validate-exam', url: '/ExamManagement/ValidateExam', title: '验证考试' }
  ]
};

// 创建目录
function ensureDirectoryExists(dirPath) {
  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
  }
}

// UI问题检测器
class UIIssueDetector {
  constructor() {
    this.issues = [];
  }

  async detectLayoutIssues(page, pageName, viewport) {
    const issues = [];

    try {
      // 检查导航栏是否被遮挡
      const navbar = await page.locator('.navbar, .glass-navbar').first();
      if (await navbar.count() > 0) {
        const navbarBox = await navbar.boundingBox();
        const mainContent = await page.locator('main, .container, .main-content').first();
        if (await mainContent.count() > 0) {
          const contentBox = await mainContent.boundingBox();
          if (navbarBox && contentBox && contentBox.y < navbarBox.y + navbarBox.height) {
            issues.push({
              type: 'layout',
              severity: 'high',
              description: '主体内容可能被导航栏遮挡',
              element: 'navbar/main-content',
              viewport: viewport.name
            });
          }
        }
      }

      // 检查元素溢出
      const overflowElements = await page.evaluate(() => {
        const elements = document.querySelectorAll('*');
        const overflowing = [];
        elements.forEach(el => {
          if (el.scrollWidth > el.clientWidth || el.scrollHeight > el.clientHeight) {
            overflowing.push({
              tagName: el.tagName,
              className: el.className,
              id: el.id
            });
          }
        });
        return overflowing;
      });

      if (overflowElements.length > 0) {
        issues.push({
          type: 'layout',
          severity: 'medium',
          description: `发现 ${overflowElements.length} 个元素可能存在溢出问题`,
          elements: overflowElements,
          viewport: viewport.name
        });
      }

    } catch (error) {
      issues.push({
        type: 'error',
        severity: 'high',
        description: `布局检测时发生错误: ${error.message}`,
        viewport: viewport.name
      });
    }

    return issues;
  }

  async detectGlassmorphismIssues(page, pageName, viewport) {
    const issues = [];

    try {
      // 检查玻璃拟态效果
      const glassElements = await page.evaluate(() => {
        const elements = document.querySelectorAll('.glass-card, .glass-btn, .glass-navbar, .glass-form');
        const results = [];
        
        elements.forEach(el => {
          const styles = window.getComputedStyle(el);
          const hasBackdropFilter = styles.backdropFilter && styles.backdropFilter !== 'none';
          const hasTransparentBg = styles.backgroundColor.includes('rgba') || 
                                   styles.background.includes('rgba');
          
          results.push({
            className: el.className,
            hasBackdropFilter,
            hasTransparentBg,
            backdropFilter: styles.backdropFilter,
            backgroundColor: styles.backgroundColor
          });
        });
        
        return results;
      });

      glassElements.forEach(el => {
        if (!el.hasBackdropFilter) {
          issues.push({
            type: 'glassmorphism',
            severity: 'medium',
            description: `元素缺少backdrop-filter模糊效果`,
            element: el.className,
            viewport: viewport.name
          });
        }
        
        if (!el.hasTransparentBg) {
          issues.push({
            type: 'glassmorphism',
            severity: 'medium',
            description: `元素缺少半透明背景效果`,
            element: el.className,
            viewport: viewport.name
          });
        }
      });

    } catch (error) {
      issues.push({
        type: 'error',
        severity: 'high',
        description: `Glassmorphism检测时发生错误: ${error.message}`,
        viewport: viewport.name
      });
    }

    return issues;
  }

  async detectVisualIssues(page, pageName, viewport) {
    const issues = [];

    try {
      // 检查字体一致性
      const fontIssues = await page.evaluate(() => {
        const elements = document.querySelectorAll('h1, h2, h3, h4, h5, h6, p, span, div');
        const fontFamilies = new Set();
        const fontSizes = new Set();
        
        elements.forEach(el => {
          const styles = window.getComputedStyle(el);
          fontFamilies.add(styles.fontFamily);
          fontSizes.add(styles.fontSize);
        });
        
        return {
          fontFamilyCount: fontFamilies.size,
          fontSizeCount: fontSizes.size,
          fontFamilies: Array.from(fontFamilies),
          fontSizes: Array.from(fontSizes)
        };
      });

      if (fontIssues.fontFamilyCount > 3) {
        issues.push({
          type: 'visual',
          severity: 'low',
          description: `字体系列过多 (${fontIssues.fontFamilyCount}种)，可能影响一致性`,
          details: fontIssues.fontFamilies,
          viewport: viewport.name
        });
      }

    } catch (error) {
      issues.push({
        type: 'error',
        severity: 'high',
        description: `视觉检测时发生错误: ${error.message}`,
        viewport: viewport.name
      });
    }

    return issues;
  }

  async analyzePageIssues(page, pageName, viewport) {
    const layoutIssues = await this.detectLayoutIssues(page, pageName, viewport);
    const glassIssues = await this.detectGlassmorphismIssues(page, pageName, viewport);
    const visualIssues = await this.detectVisualIssues(page, pageName, viewport);
    
    return [...layoutIssues, ...glassIssues, ...visualIssues];
  }
}

// 主测试函数
async function runUITests() {
  console.log('🚀 开始UI自动化测试...');
  
  // 创建必要的目录
  ensureDirectoryExists(config.screenshotDir);
  ensureDirectoryExists(config.reportDir);
  
  const browser = await chromium.launch({ 
    headless: false,
    slowMo: 1000 // 减慢操作速度以便观察
  });
  
  const detector = new UIIssueDetector();
  const testResults = {
    timestamp: new Date().toISOString(),
    summary: {
      totalPages: config.pages.length,
      totalViewports: config.viewports.length,
      totalTests: config.pages.length * config.viewports.length
    },
    results: [],
    issues: []
  };

  try {
    for (const viewport of config.viewports) {
      console.log(`\n📱 测试视口: ${viewport.name} (${viewport.width}x${viewport.height})`);
      
      const context = await browser.newContext({
        viewport: { width: viewport.width, height: viewport.height }
      });
      
      const page = await context.newPage();
      
      for (const pageConfig of config.pages) {
        console.log(`  📄 测试页面: ${pageConfig.title}`);
        
        try {
          // 导航到页面
          await page.goto(`${config.baseURL}${pageConfig.url}`, { 
            waitUntil: 'networkidle',
            timeout: 30000 
          });
          
          // 等待页面加载完成
          await page.waitForTimeout(2000);
          
          // 截图
          const screenshotPath = path.join(
            config.screenshotDir, 
            `${pageConfig.name}-${viewport.name}.png`
          );
          await page.screenshot({ 
            path: screenshotPath, 
            fullPage: true 
          });
          
          // 分析UI问题
          const pageIssues = await detector.analyzePageIssues(page, pageConfig.name, viewport);
          
          const result = {
            page: pageConfig.name,
            title: pageConfig.title,
            url: pageConfig.url,
            viewport: viewport.name,
            screenshot: screenshotPath,
            issues: pageIssues,
            timestamp: new Date().toISOString()
          };
          
          testResults.results.push(result);
          testResults.issues.push(...pageIssues);
          
          console.log(`    ✅ 完成 - 发现 ${pageIssues.length} 个问题`);
          
        } catch (error) {
          console.log(`    ❌ 错误: ${error.message}`);
          testResults.results.push({
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
  
  // 生成报告
  await generateReport(testResults);
  
  console.log('\n🎉 UI测试完成！');
  console.log(`📊 总共测试了 ${testResults.results.length} 个页面/视口组合`);
  console.log(`🐛 发现 ${testResults.issues.length} 个问题`);
  console.log(`📁 截图保存在: ${config.screenshotDir}`);
  console.log(`📋 报告保存在: ${config.reportDir}`);
}

// 生成HTML报告
async function generateReport(testResults) {
  const reportPath = path.join(config.reportDir, 'ui-test-report.html');
  
  const html = `
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Examina UI测试报告</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; margin: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { text-align: center; margin-bottom: 30px; }
        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 30px; }
        .summary-card { background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: center; }
        .issue { margin: 10px 0; padding: 15px; border-radius: 5px; border-left: 4px solid; }
        .issue.high { background: #fff5f5; border-color: #e53e3e; }
        .issue.medium { background: #fffbf0; border-color: #dd6b20; }
        .issue.low { background: #f0fff4; border-color: #38a169; }
        .page-result { margin: 20px 0; padding: 20px; background: #f8f9fa; border-radius: 8px; }
        .screenshot { max-width: 300px; border-radius: 4px; }
        .viewport-badge { display: inline-block; padding: 4px 8px; background: #e2e8f0; border-radius: 4px; font-size: 12px; margin: 2px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🎨 Examina UI测试报告</h1>
            <p>生成时间: ${testResults.timestamp}</p>
        </div>
        
        <div class="summary">
            <div class="summary-card">
                <h3>总页面数</h3>
                <div style="font-size: 2em; color: #4299e1;">${testResults.summary.totalPages}</div>
            </div>
            <div class="summary-card">
                <h3>测试视口</h3>
                <div style="font-size: 2em; color: #48bb78;">${testResults.summary.totalViewports}</div>
            </div>
            <div class="summary-card">
                <h3>总测试数</h3>
                <div style="font-size: 2em; color: #ed8936;">${testResults.summary.totalTests}</div>
            </div>
            <div class="summary-card">
                <h3>发现问题</h3>
                <div style="font-size: 2em; color: #e53e3e;">${testResults.issues.length}</div>
            </div>
        </div>
        
        <h2>🐛 问题汇总</h2>
        ${testResults.issues.map(issue => `
            <div class="issue ${issue.severity}">
                <strong>${issue.type.toUpperCase()}</strong> - ${issue.description}
                <br><small>视口: ${issue.viewport} | 严重程度: ${issue.severity}</small>
                ${issue.element ? `<br><small>元素: ${issue.element}</small>` : ''}
            </div>
        `).join('')}
        
        <h2>📄 页面测试结果</h2>
        ${testResults.results.map(result => `
            <div class="page-result">
                <h3>${result.title} <span class="viewport-badge">${result.viewport}</span></h3>
                <p><strong>URL:</strong> ${result.url}</p>
                ${result.screenshot ? `<p><strong>截图:</strong> <br><img src="../${result.screenshot}" class="screenshot" alt="Screenshot"></p>` : ''}
                ${result.error ? `<p style="color: red;"><strong>错误:</strong> ${result.error}</p>` : ''}
                <p><strong>问题数量:</strong> ${result.issues ? result.issues.length : 0}</p>
            </div>
        `).join('')}
    </div>
</body>
</html>`;

  fs.writeFileSync(reportPath, html);
  
  // 同时生成JSON报告
  const jsonReportPath = path.join(config.reportDir, 'ui-test-report.json');
  fs.writeFileSync(jsonReportPath, JSON.stringify(testResults, null, 2));
}

// 运行测试
if (require.main === module) {
  runUITests().catch(console.error);
}

module.exports = { runUITests, UIIssueDetector };
