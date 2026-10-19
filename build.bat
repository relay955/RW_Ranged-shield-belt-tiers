@echo off
setlocal

cd /d "%~dp0"

dotnet build "Source\Ranged Shield Belt Tiers.sln" --configuration Release %*
if errorlevel 1 exit /b %errorlevel%

echo Build completed: 1.7\Assemblies\Ranged_Shield_Belt_Tiers.dll
exit /b 0
