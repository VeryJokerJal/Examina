// Playwright配置文件
module.exports = {
  // 测试目录
  testDir: './tests',
  
  // 全局超时设置
  timeout: 30000,
  
  // 期望超时
  expect: {
    timeout: 5000
  },
  
  // 失败时重试次数
  retries: 2,
  
  // 并行工作进程数
  workers: 1,
  
  // 报告器配置
  reporter: [
    ['html', { outputFolder: 'test-results/html-report' }],
    ['json', { outputFile: 'test-results/results.json' }]
  ],
  
  // 全局设置
  use: {
    // 基础URL
    baseURL: 'http://localhost:5117',
    
    // 浏览器设置
    headless: false,
    
    // 视口大小
    viewport: { width: 1280, height: 720 },
    
    // 忽略HTTPS错误
    ignoreHTTPSErrors: true,
    
    // 截图设置
    screenshot: 'only-on-failure',
    
    // 视频录制
    video: 'retain-on-failure',
    
    // 跟踪
    trace: 'on-first-retry'
  },
  
  // 项目配置 - 不同浏览器和设备
  projects: [
    {
      name: 'chromium-desktop',
      use: {
        ...require('playwright').devices['Desktop Chrome'],
        viewport: { width: 1920, height: 1080 }
      }
    },
    {
      name: 'chromium-tablet',
      use: {
        ...require('playwright').devices['iPad Pro'],
      }
    },
    {
      name: 'chromium-mobile',
      use: {
        ...require('playwright').devices['iPhone 12'],
      }
    }
  ],
  
  // 输出目录
  outputDir: 'test-results/artifacts'
};
