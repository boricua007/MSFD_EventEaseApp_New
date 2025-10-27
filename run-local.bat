@echo off
REM Set base href for local development (Windows)
powershell -Command "(Get-Content wwwroot\index.html) -replace '{{BASE_HREF}}', '/' | Set-Content wwwroot\index.html"
echo Base href set for local development
dotnet run