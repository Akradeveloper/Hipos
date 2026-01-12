# Script para ejecutar tests con opciones
# Uso: .\scripts\run-tests.ps1 -Category Demo -OpenReport

param(
    [string]$Category = "",
    [switch]$OpenReport,
    [string]$Configuration = "Debug"
)

Write-Host "=== Hipos Test Runner ===" -ForegroundColor Cyan

# Build
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "`nRunning tests..." -ForegroundColor Yellow

if ($Category) {
    Write-Host "Filter: Category=$Category" -ForegroundColor Gray
    dotnet test --no-build --configuration $Configuration --filter "Category=$Category"
}
else {
    dotnet test --no-build --configuration $Configuration
}

$testExitCode = $LASTEXITCODE

if ($testExitCode -eq 0) {
    Write-Host "`nAll tests passed!" -ForegroundColor Green
}
else {
    Write-Host "`nSome tests failed!" -ForegroundColor Red
}

# Open ExtentReports if requested
if ($OpenReport) {
    Write-Host "`n--- Opening ExtentReports ---" -ForegroundColor Cyan
    
    $reportPath = "src\Hipos.Tests\bin\$Configuration\net8.0-windows\reports\extent-report.html"
    
    if (Test-Path $reportPath) {
        Write-Host "Opening report: $reportPath" -ForegroundColor Green
        Invoke-Item $reportPath
    }
    else {
        Write-Host "WARNING: No report found at $reportPath" -ForegroundColor Yellow
    }
}

exit $testExitCode
