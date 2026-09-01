# 编译 ImperialCoinFix.dll(工作区 Source 全部 .cs;含并入的"科技差距贸易溢价"TechGap 源)。
# 用法: powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

$refs = @(
    (Join-Path $game 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\netstandard.dll'),
    (Join-Path $game 'Mods\Harmony\Current\Assemblies\0Harmony.dll'),
    (Join-Path $game 'Mods\CombatExtended\Assemblies\CombatExtended.dll')
)

$srcFiles = (Get-ChildItem (Join-Path $root 'Source') -Filter '*.cs').FullName
$out = Join-Path $root 'Assemblies\ImperialCoinFix.dll'
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null

$args = New-Object System.Collections.Generic.List[string]
$args.Add('/nologo')
$args.Add('/target:library')
$args.Add('/out:"' + $out + '"')
foreach ($r in $refs) { $args.Add('/r:"' + $r + '"') }
foreach ($f in $srcFiles) { $args.Add('"' + $f + '"') }

$log = Join-Path $root 'build.log'
& $csc $args.ToArray() *> $log
if ($LASTEXITCODE -ne 0) {
    Get-Content $log | Select-Object -First 60
    throw "Compile failed with exit code $LASTEXITCODE (see $log)"
}
Write-Host "Built: $out"
