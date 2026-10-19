@echo off
call "%~dp0build.bat"
if errorlevel 1 exit /b %errorlevel%

call "%~dp0dist.bat"
if errorlevel 1 exit /b %errorlevel%

start "" "steam://rungameid/294100"
