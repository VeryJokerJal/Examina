$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"
$lines = Get-Content $filePath -Encoding UTF8
$newLines = @()
$i = 0

while ($i -lt $lines.Count) {
    $line = $lines[$i]
    
    if ($line -like '*DisplayName = "文本题目描述"*') {
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
    
    $line = $line -replace '"目标图表"', '"目标工作簿"'
    $newLines += $line
    $i++
}

$newLines | Set-Content $filePath -Encoding UTF8
$deletedCount = $lines.Count - $newLines.Count
Write-Host "Deleted $deletedCount lines"
