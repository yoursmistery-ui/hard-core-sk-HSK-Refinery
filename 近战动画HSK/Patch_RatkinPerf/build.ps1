# build zz.AM.RatkinPerfPatch.dll - ProcessStartInfo based (reliable)
$ErrorActionPreference = 'Continue'
$log = 'C:\Personal\Project\ratkin-patch\_tmp\restore_test\build_log.txt'
try { Remove-Item $log -Force -ErrorAction SilentlyContinue } catch {}
function W($msg) { Add-Content -Path $log -Value $msg -Encoding utf8 }

W "START BUILD"

$root = $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

$refs = @(
    (Join-Path $game 'RimWorldWin64_Data\Managed\Assembly-CSharp.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll'),
    (Join-Path $game 'RimWorldWin64_Data\Managed\netstandard.dll'),
    (Join-Path $game 'Mods\Harmony\Current\Assemblies\0Harmony.dll')
)

$srcDir = Join-Path $root '1.6\Source'
$srcFiles = @()
Get-ChildItem $srcDir -Filter '*.cs' -ErrorAction SilentlyContinue | ForEach-Object { $srcFiles += $_.FullName }
W "src count: $($srcFiles.Count)"

$out = Join-Path $root '1.6\Assemblies\zz.AM.RatkinPerfPatch.dll'
$outDir = Split-Path $out
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$argBuilder = New-Object System.Text.StringBuilder
[void]$argBuilder.Append('/nologo /target:library /optimize+ /codepage:65001 /out:"' + $out + '"')
foreach ($r in $refs) {
    if (Test-Path $r) { [void]$argBuilder.Append(' /r:"' + $r + '"') } else { W "MISSING REF: $r" }
}
foreach ($f in $srcFiles) { [void]$argBuilder.Append(' "' + $f + '"') }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $csc
$psi.Arguments = $argBuilder.ToString()
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
$psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8

W "cmd: $csc $($psi.Arguments)"
$p = [System.Diagnostics.Process]::Start($psi)
$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
$p.WaitForExit()

W "exitcode=$($p.ExitCode)"
W "=== stdout ==="
W $stdout
W "=== stderr ==="
W $stderr
W "out exists: $(Test-Path $out)"
W "BUILD END"