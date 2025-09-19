Write-Host "=== Testing Connection String Manager ===" -ForegroundColor Green
Write-Host ""

Write-Host "1. Switching to Development..." -ForegroundColor Yellow
connstr -f appsettings.json -e Development
Write-Host "Running app:" -ForegroundColor Cyan
dotnet run
Write-Host ""

Write-Host "2. Switching to QA..." -ForegroundColor Yellow
connstr -f appsettings.json -e QA
Write-Host "Running app:" -ForegroundColor Cyan
dotnet run
Write-Host ""

Write-Host "3. Switching to Production..." -ForegroundColor Yellow
connstr -f appsettings.json -e Production
Write-Host "Running app:" -ForegroundColor Cyan
dotnet run
Write-Host ""

Write-Host "=== Testing Complete ===" -ForegroundColor Green