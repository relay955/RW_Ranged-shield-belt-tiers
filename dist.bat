@echo off
rmdir /s /q "D:\SteamLibrary\steamapps\common\RimWorld\Mods\RW_Ranged-shield-belt-tiers" 2>nul
mkdir "D:\SteamLibrary\steamapps\common\RimWorld\Mods\RW_Ranged-shield-belt-tiers"

xcopy /E /I /Y ".\*.*" "D:\SteamLibrary\steamapps\common\RimWorld\Mods\RW_Ranged-shield-belt-tiers" /EXCLUDE:exclude.txt
echo Files copied successfully to RimWorld mods folder.
pause