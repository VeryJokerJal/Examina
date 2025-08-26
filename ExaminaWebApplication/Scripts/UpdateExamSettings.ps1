# 更新考试设置的PowerShell脚本
# 这个脚本将执行数据库迁移，更新现有考试的重考和练习设置

param(
    [string]$ConnectionString = "Data Source=examina.db",
    [switch]$WhatIf = $false
)

Write-Host "=== 考试设置更新脚本 ===" -ForegroundColor Green
Write-Host "连接字符串: $ConnectionString" -ForegroundColor Yellow

if ($WhatIf) {
    Write-Host "*** 这是预览模式，不会实际修改数据库 ***" -ForegroundColor Magenta
}

try {
    # 检查SQLite是否可用
    $sqliteCommand = Get-Command sqlite3 -ErrorAction SilentlyContinue
    if (-not $sqliteCommand) {
        Write-Host "错误: 未找到sqlite3命令。请确保SQLite已安装并在PATH中。" -ForegroundColor Red
        exit 1
    }

    # 获取迁移脚本路径
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $migrationScript = Join-Path $scriptDir "..\Migrations\UpdateExamRetakeSettings.sql"
    
    if (-not (Test-Path $migrationScript)) {
        Write-Host "错误: 未找到迁移脚本: $migrationScript" -ForegroundColor Red
        exit 1
    }

    Write-Host "找到迁移脚本: $migrationScript" -ForegroundColor Green

    # 检查数据库文件
    $dbPath = $ConnectionString -replace "Data Source=", ""
    if (-not (Test-Path $dbPath)) {
        Write-Host "错误: 数据库文件不存在: $dbPath" -ForegroundColor Red
        exit 1
    }

    Write-Host "数据库文件: $dbPath" -ForegroundColor Green

    if ($WhatIf) {
        Write-Host "预览模式: 显示将要执行的SQL命令" -ForegroundColor Magenta
        Get-Content $migrationScript | Write-Host -ForegroundColor Cyan
        return
    }

    # 备份数据库
    $backupPath = "$dbPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "创建数据库备份: $backupPath" -ForegroundColor Yellow
    Copy-Item $dbPath $backupPath

    # 执行迁移脚本
    Write-Host "执行数据库迁移..." -ForegroundColor Yellow
    $result = & sqlite3 $dbPath ".read `"$migrationScript`""
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ 数据库迁移成功完成" -ForegroundColor Green
        Write-Host "备份文件保存在: $backupPath" -ForegroundColor Green
    } else {
        Write-Host "✗ 数据库迁移失败，退出代码: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "正在恢复备份..." -ForegroundColor Yellow
        Copy-Item $backupPath $dbPath -Force
        Write-Host "数据库已恢复到迁移前状态" -ForegroundColor Yellow
        exit 1
    }

    # 验证更新结果
    Write-Host "验证更新结果..." -ForegroundColor Yellow
    $verifyQuery = @"
SELECT 
    COUNT(*) as TotalExams,
    SUM(CASE WHEN AllowRetake = 1 THEN 1 ELSE 0 END) as ExamsWithRetake,
    SUM(CASE WHEN AllowPractice = 1 THEN 1 ELSE 0 END) as ExamsWithPractice
FROM ImportedExams 
WHERE IsEnabled = 1;
"@

    $verifyResult = & sqlite3 $dbPath $verifyQuery
    Write-Host "更新统计: $verifyResult" -ForegroundColor Green

} catch {
    Write-Host "脚本执行出错: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "=== 脚本执行完成 ===" -ForegroundColor Green
