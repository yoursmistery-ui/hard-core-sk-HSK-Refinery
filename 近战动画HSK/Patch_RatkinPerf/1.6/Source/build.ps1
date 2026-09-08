# 编译 zz.AM.RatkinPerfPatch.dll(近战动画HSK 性能补丁,独立 Harmony,不改原 zAnimationMod.dll)。
# csproj 保留作 IDE 参考;实际构建走本脚本(csc.exe 直编,免 .NET Framework 开发者包)。
# 用法: powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

$refs = @(
    (Join-Path $game 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\netstandard.dll'),
    (Join-Path $game 'Mods\Harmony\Current\Assemblies\0Harmony.dll'),
    (Join-Path $root '..\..\..\1.6\Assemblies\zAnimationMod.dll')
)

$srcFiles = (Get-ChildItem $root -Filter '*.cs').FullName
$out = Join-Path $root '..\Assemblies\zz.AM.RatkinPerfPatch.dll'
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
