# 编译 zz.AM.KnockbackFix.dll(近战动画HSK 击退飞行器零飞行时间除零修复,独立 Harmony,不改原 zAnimationMod.dll)。
# 纯反射,不编译期依赖 zAnimationMod。实际构建走 csc.exe(免 NuGet)。
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
    # IntVec3 的 operator+ / operator* 在 1.6 走 Unity.Mathematics(int3),用到就得引用
    (Join-Path $game 'RimWorldWin64_Data\Managed\Unity.Mathematics.dll'),
    (Join-Path $game 'Mods\Harmony\Current\Assemblies\0Harmony.dll')
)

$srcFiles = (Get-ChildItem $root -Filter '*.cs').FullName
$out = Join-Path $root '..\Assemblies\zz.AM.KnockbackFix.dll'
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null

$args = New-Object System.Collections.Generic.List[string]
$args.Add('/nologo')
$args.Add('/target:library')
$args.Add('/optimize+')
$args.Add('/out:"' + $out + '"')
foreach ($r in $refs) { $args.Add('/r:"' + $r + '"') }
foreach ($f in $srcFiles) { $args.Add('"' + $f + '"') }

& $csc $args.ToArray()
if ($LASTEXITCODE -ne 0) { throw "Compile failed with exit code $LASTEXITCODE" }
Write-Host "Built: $out"
