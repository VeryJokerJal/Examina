# 完整删除所有冗余"文本题目描述"参数的脚本

$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"

# 读取文件内容
$content = Get-Content $filePath -Raw -Encoding UTF8

# 使用正则表达式删除所有包含"文本题目描述"的参数行及其前面的逗号
# 匹配模式：逗号+换行+空格+整个参数定义
$pattern = ',\s*\r?\n\s*new\(\)\s*\{\s*Name\s*=\s*"Description",\s*DisplayName\s*=\s*"文本题目描述"[^}]*\}'

$newContent = $content -replace $pattern, ''

# 同时统一所有"目标图表"为"目标工作簿"
$newContent = $newContent -replace 'DisplayName\s*=\s*"目标图表"', 'DisplayName = "目标工作簿"'

# 写回文件
$newContent | Set-Content $filePath -NoNewline -Encoding UTF8

# 验证结果
$originalLines = ($content -split "`r?`n").Count
$newLines = ($newContent -split "`r?`n").Count
$deletedLines = $originalLines - $newLines

Write-Host "删除完成！"
Write-Host "原始行数: $originalLines"
Write-Host "修改后行数: $newLines"
Write-Host "删除的行数: $deletedLines"

# 检查是否还有剩余的"文本题目描述"
$remainingMatches = ($newContent | Select-String "文本题目描述" -AllMatches).Matches.Count
Write-Host "剩余的'文本题目描述'参数: $remainingMatches"
