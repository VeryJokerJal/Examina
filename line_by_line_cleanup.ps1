$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"
$lines = Get-Content $filePath -Encoding UTF8
$newLines = @()
$deletedCount = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    
    # Check if current line contains "文本题目描述"
    if ($line -match 'DisplayName.*=.*"文本题目描述"') {
        # Remove the comma from the previous line if it exists
        if ($newLines.Count -gt 0) {
            $lastIndex = $newLines.Count - 1
            $lastLine = $newLines[$lastIndex]
            if ($lastLine -match ',\s*$') {
                $newLines[$lastIndex] = $lastLine -replace ',\s*$', ''
            }
        }
        $deletedCount++
        continue  # Skip this line
    }
    
    # Replace "目标图表" with "目标工作簿"
    $line = $line -replace '"目标图表"', '"目标工作簿"'
    
    $newLines += $line
}

# Write back to file
$newLines | Set-Content $filePath -Encoding UTF8

Write-Host "Cleanup completed!"
Write-Host "Original lines: $($lines.Count)"
Write-Host "New lines: $($newLines.Count)"
Write-Host "Deleted parameters: $deletedCount"
