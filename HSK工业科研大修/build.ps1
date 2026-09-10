$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

$refs = @(
    (Join-Path $game 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\netstandard.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\Unity.Mathematics.dll'),
    (Join-Path $game 'Mods\Harmony\Current\Assemblies\0Harmony.dll')
)

$srcFiles = (Get-ChildItem (Join-Path $root 'Source') -Filter '*.cs').FullName
$out = Join-Path $root '1.6\Assemblies\BlueprintUnlockHSK.dll'

$args = New-Object System.Collections.Generic.List[string]
$args.Add('/nologo')
$args.Add('/target:library')
$args.Add('/out:"' + $out + '"')
foreach ($r in $refs) { $args.Add('/r:"' + $r + '"') }
foreach ($f in $srcFiles) { $args.Add('"' + $f + '"') }

# csc 输出落盘到工作区全局 _tmp/ (mod 根目录不留临时文件; 独立部署时 _tmp 不存在则跳过写日志)
$logPath = Join-Path (Split-Path $root -Parent) '_tmp\csc_last_build.log'
if (Test-Path (Split-Path $logPath -Parent)) {
    & $csc $args.ToArray() 2>&1 | Out-File $logPath -Encoding utf8
} else {
    & $csc $args.ToArray()
}
if ($LASTEXITCODE -ne 0) { throw "Compile failed with exit code $LASTEXITCODE (log: $logPath)" }
Write-Host "Built: $out"
