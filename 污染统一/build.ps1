# 编译 污染统一.dll（Source 全部 .cs）。
# 用法: powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

$refs = @(
	(Join-Path $game 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'),
	(Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'),
	(Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'),
	(Join-Path $game 'RimWorldWin64_Data\Managed\Unity.Mathematics.dll'),
	(Join-Path $game 'RimWorldWin64_Data\Managed\netstandard.dll')
)

$srcFiles = (Get-ChildItem (Join-Path $root 'Source') -Filter '*.cs').FullName
$out = Join-Path $root 'Assemblies\PollutionUnify.dll'
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null

$args = New-Object System.Collections.Generic.List[string]
$args.Add('/nologo')
$args.Add('/target:library')
$args.Add('/out:"' + $out + '"')
foreach ($r in $refs) { $args.Add('/r:"' + $r + '"') }
foreach ($f in $srcFiles) { $args.Add('"' + $f + '"') }

& $csc $args.ToArray()
if ($LASTEXITCODE -ne 0) { throw "Compile failed with exit code $LASTEXITCODE" }
Write-Host "Built: $out"