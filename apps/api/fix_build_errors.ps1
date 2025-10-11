# GameGuild Build Error Fix Script (PowerShell)
# Fixes common error patterns in C# files

Write-Host "GameGuild Build Error Fix Script" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

$sourceDir = Join-Path $PSScriptRoot "Source"
$stats = @{
    FilesProcessed    = 0
    FilesModified     = 0
    TotalReplacements = 0
}

Write-Host "Scanning directory: $sourceDir"
Write-Host ""

# Get all C# files
$csFiles = Get-ChildItem -Path $sourceDir -Filter "*.cs" -Recurse

Write-Host "Found $($csFiles.Count) C# files"
Write-Host ""

foreach ($file in $csFiles) {
    $stats.FilesProcessed++
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $fileReplacements = 0
    
    # Fix 1: Change init-only properties to set for FailoverResult and FailoverStep
    if ($content -match "class Failover") {
        $before = $content
        $content = $content -replace 'public\s+\w+\s+Status\s*\{\s*get;\s*init;', 'public string Status { get; set;'
        $content = $content -replace 'public\s+DateTime\?\s+CompletedAt\s*\{\s*get;\s*init;', 'public DateTime? CompletedAt { get; set;'
        $content = $content -replace 'public\s+string\?\s+ErrorMessage\s*\{\s*get;\s*init;', 'public string? ErrorMessage { get; set;'
        if ($content -ne $before) {
            $diff = $before.Length - $content.Length
            $fileReplacements += 3
        }
    }
    
    # Write changes if any
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $stats.FilesModified++
        $stats.TotalReplacements += $fileReplacements
        $relativePath = $file.FullName.Replace($sourceDir + "\", "")
        Write-Host "OK $relativePath : $fileReplacements fixes" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files processed: $($stats.FilesProcessed)"
Write-Host "  Files modified:  $($stats.FilesModified)"
Write-Host "  Total fixes:     $($stats.TotalReplacements)"
Write-Host ""
Write-Host "Done! Run dotnet build to verify fixes." -ForegroundColor Green
