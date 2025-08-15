# 批量删除剩余的冗余"文本题目描述"参数

$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"

# 使用UTF-8编码读取文件内容
$content = Get-Content $filePath -Raw -Encoding UTF8

# 删除所有包含"文本题目描述"的行及其前面的逗号
# 使用更精确的正则表达式匹配整行
$lines = $content -split "`r?`n"
$newLines = @()
$deletedCount = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    
    # 检查当前行是否包含"文本题目描述"
    if ($line -match '.*DisplayName\s*=\s*"文本题目描述".*') {
        # 删除这一行，并检查前一行是否需要删除逗号
        if ($newLines.Count -gt 0) {
            $lastLineIndex = $newLines.Count - 1
            $lastLine = $newLines[$lastLineIndex]
            # 如果前一行以逗号结尾，删除逗号
            if ($lastLine -match ',\s*$') {
                $newLines[$lastLineIndex] = $lastLine -replace ',\s*$', ''
            }
        }
        $deletedCount++
        continue
    }
    
    # 继续统一"目标图表"为"目标工作簿"
    $line = $line -replace '"目标图表"', '"目标工作簿"'
    
    $newLines += $line
}

# 使用UTF-8编码写回文件
$newContent = $newLines -join "`r`n"
$newContent | Set-Content $filePath -NoNewline -Encoding UTF8

Write-Host "成功删除 $deletedCount 个冗余的'文本题目描述'参数"
Write-Host "文件行数从 $($lines.Count) 减少到 $($newLines.Count)"
