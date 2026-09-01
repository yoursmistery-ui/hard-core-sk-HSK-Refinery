@echo off
rem Build RatkinFurniture.dll (dynamic furniture + style icon fix)
rem Run inside Source dir; output goes to Source\RatkinFurniture.dll,
rem then copy it over ..\Assemblies\RatkinFurniture.dll.
setlocal
set RW=C:\Program Files (x86)\Steam\steamapps\common\RimWorld
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library ^
  /r:"%RW%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" ^
  /r:"%RW%\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll" ^
  /r:"%RW%\RimWorldWin64_Data\Managed\netstandard.dll" ^
  /r:"%RW%\Mods\Harmony\Current\Assemblies\0Harmony.dll" ^
  /out:"%~dp0RatkinFurniture.dll" "%~dp0RatkinFurniture.cs" "%~dp0StyleIconFix.cs"
endlocal
