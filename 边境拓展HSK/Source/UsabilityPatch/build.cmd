@echo off
rem 编译 BordersOfTheRim.HSKPatch.dll 到 ..\Assemblies\ (用 dotnet SDK 自带 Roslyn, 无需 msbuild/nuget)
setlocal
set GAME=C:\Program Files (x86)\Steam\steamapps\common\RimWorld
for /d %%d in ("%ProgramFiles%\dotnet\sdk\*") do if exist "%%d\Roslyn\bincore\csc.dll" set CSC=%%d\Roslyn\bincore\csc.dll
if "%CSC%"=="" (echo 未找到 dotnet SDK Roslyn csc.dll & exit /b 1)
dotnet exec "%CSC%" -nologo -t:library -optimize+ -langversion:latest ^
 -r:"%GAME%\RimWorldWin64_Data\Managed\mscorlib.dll" ^
 -r:"%GAME%\RimWorldWin64_Data\Managed\System.dll" ^
 -r:"%GAME%\RimWorldWin64_Data\Managed\System.Core.dll" ^
 -r:"%GAME%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" ^
 -r:"%GAME%\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll" ^
 -r:"%GAME%\Mods\Harmony\Current\Assemblies\0Harmony.dll" ^
 -r:"%~dp0..\..\Assemblies\BordersOfTheRim.dll" ^
 -out:"%~dp0..\..\Assemblies\BordersOfTheRim.HSKPatch.dll" ^
 "%~dp0UsabilityPatch.cs"
endlocal
