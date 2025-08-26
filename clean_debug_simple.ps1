# Simple PowerShell script to clean debug output
param([string]$Path = "Examina")

Write-Host "Starting debug cleanup..." -ForegroundColor Green

$csFiles = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" | Where-Object { 
    $_.FullName -notlike "*\bin\*" -and 
    $_.FullName -notlike "*\obj\*" -and
    $_.FullName -notlike "*\Tests\*"
}

$totalRemovedLines = 0

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Remove debug statements
    $content = $content -replace 'System\.Diagnostics\.Debug\.WriteLine\([^)]*\);?', ''
    $content = $content -replace 'Console\.WriteLine\([^)]*\);?', ''
    $content = $content -replace 'Debug\.WriteLine\([^)]*\);?', ''
    
    # Clean up empty lines
    $content = $content -replace '\r?\n\s*\r?\n\s*\r?\n', "`r`n`r`n"
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Cleaned: $($file.Name)" -ForegroundColor Yellow
        $totalRemovedLines++
    }
}

Write-Host "Cleanup complete! Processed files: $totalRemovedLines" -ForegroundColor Green
