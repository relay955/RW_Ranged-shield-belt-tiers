@echo off
setlocal
cd /d "%~dp0"

set "RIMWORLD_PATH=%RIMWORLD_DIR%"
if not defined RIMWORLD_PATH set "RIMWORLD_PATH=C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
if not exist "%RIMWORLD_PATH%\RimWorld.exe" (
    echo RimWorld.exe was not found. Set RIMWORLD_DIR to the RimWorld installation directory.
    exit /b 2
)

xcopy /E /I /Y "%~dp0*.*" "%RIMWORLD_PATH%\Mods\RW_Ranged-shield-belt-tiers\" /EXCLUDE:exclude.txt
if errorlevel 1 exit /b %errorlevel%
echo Files copied successfully to RimWorld mods folder.
exit /b 0
