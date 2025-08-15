# 简单的PowerShell脚本：删除ExcelKnowledgeService.cs中的冗余"文本题目描述"参数

$filePath = "ExamLab\Services\ExcelKnowledgeService.cs"

# 使用UTF-8编码读取文件内容
$lines = Get-Content $filePath -Encoding UTF8

$newLines = @()
$deletedCount = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]

    # 检查当前行是否包含"文本题目描述"
    if ($line -match '文本题目描述') {
        # 跳过这一行，并且检查前一行是否需要删除逗号
        if ($newLines.Count -gt 0) {
            $lastLine = $newLines[-1]
            # 如果前一行以逗号结尾，删除逗号
            if ($lastLine -match ',\s*$') {
                $newLines[-1] = $lastLine -replace ',\s*$', ''
            }
        }
        # 跳过当前行（包含"文本题目描述"的行）
        $deletedCount++
        continue
    }

    # 修正"目标图表"为"目标工作簿"
    $line = $line -replace '"目标图表"', '"目标工作簿"'

    $newLines += $line
}

# 使用UTF-8编码写回文件
$newLines | Set-Content $filePath -Encoding UTF8

Write-Host "已成功删除所有冗余的'文本题目描述'参数并统一了TargetWorkbook的DisplayName"
Write-Host "删除的参数数量：$deletedCount"
