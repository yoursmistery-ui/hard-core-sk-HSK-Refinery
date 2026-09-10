# 编译 HSKFixPack.dll(工作区 Source 全部 .cs,排除 HSKVEFNamespaceFix.cs)。
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
    (Join-Path $game 'RimWorldWin64_Data\Managed\netstandard.dll'),
    (Join-Path $game 'Mods\Harmony\Current\Assemblies\0Harmony.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll'),
    (Join-Path $game 'Mods\Core_SK\Assemblies\Core_SK.dll'),
    (Join-Path $game 'Mods\Dubs-Bad-Hygiene\Assemblies\BadHygiene.dll'),
    (Join-Path $game 'Mods\Dubs-Bad-Hygiene\Assemblies\0DubCore.dll'),
    (Join-Path $game 'Mods\Dubs-Bad-Hygiene\Assemblies\0MultiplayerAPI.dll')
)

$srcFiles = (Get-ChildItem (Join-Path $root 'Source') -Filter '*.cs' | Where-Object { $_.Name -ne 'HSKVEFNamespaceFix.cs' }).FullName
$out = Join-Path $root 'Assemblies\HSKFixPack.dll'
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
