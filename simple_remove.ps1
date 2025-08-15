# 简单的删除脚本

$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"

# 读取所有行
$lines = Get-Content $filePath -Encoding UTF8

$newLines = @()
$i = 0

while ($i -lt $lines.Count) {
    $line = $lines[$i]
    
    # 如果当前行包含"文本题目描述"，跳过这一行
    if ($line -like '*文本题目描述*') {
        # 检查前一行是否需要删除逗号
        if ($newLines.Count -gt 0) {
            $lastIndex = $newLines.Count - 1
            $lastLine = $newLines[$lastIndex]
            if ($lastLine -match ',\s*$') {
                $newLines[$lastIndex] = $lastLine -replace ',\s*$', ''
            }
        }
        $i++
        continue
    }
    
    # 替换"目标图表"为"目标工作簿"
    $line = $line -replace '"目标图表"', '"目标工作簿"'
    
    $newLines += $line
    $i++
}

# 写回文件
$newLines | Set-Content $filePath -Encoding UTF8

$deletedCount = $lines.Count - $newLines.Count
Write-Host "处理完成！删除了 $deletedCount 行"
