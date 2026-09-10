@echo off
setlocal
set RW=C:\Program Files (x86)\Steam\steamapps\common\RimWorld
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:library ^
  /r:"%RW%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" ^
  /r:"%RW%\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll" ^
  /r:"%RW%\RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll" ^
  /r:"%RW%\RimWorldWin64_Data\Managed\netstandard.dll" ^
  /r:"%~dp0..\Assemblies\RimHUD.dll" ^
  /out:"%~dp0..\Assemblies\RimHUDRobotsAdapt.dll" "%~dp0RimHUDRobotsAdapt.cs"
endlocal
