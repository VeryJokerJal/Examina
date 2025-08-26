# PowerShell脚本：批量清理C#文件中的调试输出语句
# 清理 System.Diagnostics.Debug.WriteLine, Console.WriteLine, Debug.WriteLine 等调试输出

param(
    [string]$Path = "Examina"
)

Write-Host "开始清理调试输出语句..." -ForegroundColor Green

# 定义要清理的调试输出模式
$debugPatterns = @(
    'System\.Diagnostics\.Debug\.WriteLine\([^)]*\);?',
    'Console\.WriteLine\([^)]*\);?',
    'Debug\.WriteLine\([^)]*\);?',
    'System\.Diagnostics\.Debug\.Print\([^)]*\);?'
)

# 获取所有C#文件
$csFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" | Where-Object { 
    $_.FullName -notlike "*\bin\*" -and 
    $_.FullName -notlike "*\obj\*" -and
    $_.FullName -notlike "*\Tests\*"
}

$totalFiles = $csFiles.Count
$processedFiles = 0
$totalRemovedLines = 0

foreach ($file in $csFiles) {
    $processedFiles++
    Write-Progress -Activity "清理调试输出" -Status "处理文件 $($file.Name)" -PercentComplete (($processedFiles / $totalFiles) * 100)
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $removedLinesInFile = 0
    
    # 应用每个调试输出模式
    foreach ($pattern in $debugPatterns) {
        $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
        $removedLinesInFile += $matches.Count
        $content = [regex]::Replace($content, $pattern, '', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }
    
    # 清理空行（连续的空行合并为单个空行）
    $content = [regex]::Replace($content, '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    
    # 如果内容有变化，保存文件
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "已清理 $($file.Name): 移除 $removedLinesInFile 行调试输出" -ForegroundColor Yellow
        $totalRemovedLines += $removedLinesInFile
    }
}

Write-Host "`n清理完成!" -ForegroundColor Green
Write-Host "处理文件数: $totalFiles" -ForegroundColor Cyan
Write-Host "移除调试输出行数: $totalRemovedLines" -ForegroundColor Cyan
