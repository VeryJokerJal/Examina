# PowerShell脚本：删除ExcelKnowledgeService.cs中的冗余"文本题目描述"参数

$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"

# 使用UTF-8编码读取文件内容
$content = Get-Content $filePath -Raw -Encoding UTF8

# 删除所有的"文本题目描述"参数行（包括前面的逗号）
# 匹配模式：逗号 + 换行 + 空格 + new() { Name = "Description", DisplayName = "文本题目描述"... }
$pattern = ',\s*\r?\n\s*new\(\)\s*\{\s*Name\s*=\s*"Description",\s*DisplayName\s*=\s*"文本题目描述"[^}]*\}'

$newContent = $content -replace $pattern, ''

# 同时修正所有"目标图表"为"目标工作簿"
$newContent = $newContent -replace 'DisplayName\s*=\s*"目标图表"', 'DisplayName = "目标工作簿"'

# 使用UTF-8编码写回文件
$newContent | Set-Content $filePath -NoNewline -Encoding UTF8

Write-Host "已成功删除所有冗余的'文本题目描述'参数并统一了TargetWorkbook的DisplayName"
