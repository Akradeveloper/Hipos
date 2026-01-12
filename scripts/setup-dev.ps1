# Script de configuración inicial del entorno de desarrollo
# Uso: .\scripts\setup-dev.ps1

Write-Host "=== Hipos Development Setup ===" -ForegroundColor Cyan

# Verificar .NET
Write-Host "`nChecking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = & dotnet --version 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found" -ForegroundColor Red
    Write-Host "Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Green

# Verificar versión mínima (8.0)
$majorVersion = [int]($dotnetVersion.Split('.')[0])
if ($majorVersion -lt 8) {
    Write-Host "WARNING: .NET 8 or higher is required. Current: $dotnetVersion" -ForegroundColor Yellow
}

# Restaurar dependencias .NET
Write-Host "`nRestoring .NET dependencies..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore .NET dependencies" -ForegroundColor Red
    exit 1
}

Write-Host ".NET dependencies restored successfully" -ForegroundColor Green

# Build solución
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "Solution built successfully" -ForegroundColor Green

# Verificar Node.js para docs
Write-Host "`nChecking Node.js (for documentation)..." -ForegroundColor Yellow
$nodeVersion = & node --version 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Node.js not found. Required for Docusaurus documentation." -ForegroundColor Yellow
    Write-Host "Download from: https://nodejs.org/" -ForegroundColor Yellow
}
else {
    Write-Host "Node.js version: $nodeVersion" -ForegroundColor Green
    
    # Instalar dependencias de Docusaurus
    if (Test-Path "website\package.json") {
        Write-Host "`nInstalling Docusaurus dependencies..." -ForegroundColor Yellow
        Push-Location website
        npm install
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Docusaurus dependencies installed successfully" -ForegroundColor Green
        }
        else {
            Write-Host "WARNING: Failed to install Docusaurus dependencies" -ForegroundColor Yellow
        }
        
        Pop-Location
    }
}

# Resumen
Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Run tests: .\scripts\run-tests.ps1 -Category Demo" -ForegroundColor White
Write-Host "  2. Open report: .\scripts\run-tests.ps1 -Category Demo -OpenReport" -ForegroundColor White
Write-Host "  3. Start docs: cd website && npm start" -ForegroundColor White

Write-Host "`nHappy testing!" -ForegroundColor Green
