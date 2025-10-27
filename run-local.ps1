# PowerShell script to run locally with correct base href
Write-Host "Setting base href for local development..." -ForegroundColor Green
(Get-Content wwwroot\index.html) -replace '{{BASE_HREF}}', '/' | Set-Content wwwroot\index.html
Write-Host "Starting application..." -ForegroundColor Green
dotnet run