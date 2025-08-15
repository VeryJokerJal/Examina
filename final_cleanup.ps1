$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"
$content = Get-Content $filePath -Raw -Encoding UTF8

# Remove all lines containing "文本题目描述" and their preceding commas
$pattern = ',\s*\r?\n\s*new\(\)\s*\{\s*Name\s*=\s*"Description",\s*DisplayName\s*=\s*"文本题目描述"[^}]*\}'
$newContent = $content -replace $pattern, ''

# Unify all "目标图表" to "目标工作簿"
$newContent = $newContent -replace '"目标图表"', '"目标工作簿"'

# Write back to file
$newContent | Set-Content $filePath -NoNewline -Encoding UTF8

$originalLines = ($content -split "`r?`n").Count
$newLines = ($newContent -split "`r?`n").Count
$deletedLines = $originalLines - $newLines

Write-Host "Cleanup completed!"
Write-Host "Original lines: $originalLines"
Write-Host "New lines: $newLines"
Write-Host "Deleted lines: $deletedLines"
