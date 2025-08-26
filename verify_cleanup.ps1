# Verify debug cleanup
$csFiles = Get-ChildItem -Path "Examina" -Recurse -Filter "*.cs" | Where-Object { 
    $_.FullName -notlike "*\bin\*" -and 
    $_.FullName -notlike "*\obj\*" -and
    $_.FullName -notlike "*\Tests\*"
}

$foundDebugFiles = @()

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    if ($content -match 'System\.Diagnostics\.Debug\.WriteLine|Console\.WriteLine|Debug\.WriteLine') {
        $foundDebugFiles += $file.FullName
    }
}

if ($foundDebugFiles.Count -eq 0) {
    Write-Host "SUCCESS: No debug output statements found!" -ForegroundColor Green
} else {
    Write-Host "Found debug statements in:" -ForegroundColor Red
    foreach ($file in $foundDebugFiles) {
        Write-Host "  $file" -ForegroundColor Yellow
    }
}

Write-Host "Total files checked: $($csFiles.Count)" -ForegroundColor Cyan
