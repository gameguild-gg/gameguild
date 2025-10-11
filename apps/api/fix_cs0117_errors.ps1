# Fix CS0117 Errors - Add Missing Properties
# This script adds backward compatibility properties to entities

$fixes = @{
    "DeveloperOnboarding" = @{
        File        = "Source\Modules\DeveloperPortal\Entities\DeveloperOnboarding.cs"
        Properties  = @(
            "public Guid DeveloperId { get; set; }"
        )
        InsertAfter = "public class DeveloperOnboarding"
    }
    "ResourceUsageRecord" = @{
        File        = "Source\Modules\Resources\Entities\ResourceUsageRecord.cs"
        Properties  = @(
            "public DateTime RecordedAt { get; set; } = DateTime.UtcNow;"
        )
        InsertAfter = "public class ResourceUsageRecord"
    }
}

Write-Host "Fixing CS0117 Errors - Adding Missing Properties" -ForegroundColor Cyan
Write-Host "=" * 60

$totalFixed = 0

foreach ($entity in $fixes.Keys) {
    $fix = $fixes[$entity]
    $filePath = Join-Path $PSScriptRoot $fix.File
    
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw
        
        $insertPoint = $content.IndexOf($fix.InsertAfter)
        if ($insertPoint -gt 0) {
            # Find end of class declaration line
            $lineEnd = $content.IndexOf("`n", $insertPoint)
            if ($content[$lineEnd + 1] -eq '{') {
                $lineEnd++
            }
            
            $propertiesText = "`n    // Backward compatibility properties`n"
            foreach ($prop in $fix.Properties) {
                $propertiesText += "    $prop`n"
            }
            
            $newContent = $content.Insert($lineEnd + 1, $propertiesText)
            Set-Content -Path $filePath -Value $newContent -NoNewline
            
            Write-Host "  Fixed: $entity ($($fix.Properties.Count) properties)" -ForegroundColor Green
            $totalFixed += $fix.Properties.Count
        }
    }
}

Write-Host ""
Write-Host "Total properties added: $totalFixed" -ForegroundColor Green
