# 把工作区内容同步到游戏 Mods 目录(部署目录)。
# 用法: 先关闭 RimWorld,再运行(powershell -ExecutionPolicy Bypass -File sync_deploy.ps1)
$ErrorActionPreference = "Stop"

$src = $PSScriptRoot
$dst = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Ignorance is Bliss"

if (Get-Process -Name "RimWorld*" -ErrorAction SilentlyContinue) {
    Write-Host "检测到 RimWorld 仍在运行,DLL 被占用。请先关闭游戏再运行本脚本。" -ForegroundColor Yellow
    exit 1
}

# 需要同步的目录/文件(排除源码与脚本,只发运行时资产)
$items = @('About', 'Assemblies', 'Languages')

foreach ($it in $items) {
    $s = Join-Path $src $it
    if (-not (Test-Path -LiteralPath $s)) { continue }
    $d = Join-Path $dst $it
    New-Item -ItemType Directory -Force -Path $d | Out-Null
    Copy-Item -LiteralPath $s -Destination (Split-Path $d) -Recurse -Force
    Write-Host "已同步: $it" -ForegroundColor Green
}

$dll = Join-Path $dst 'Assemblies\HskRaidTech.dll'
if (Test-Path -LiteralPath $dll) {
    $f = Get-Item -LiteralPath $dll
    Write-Host "HskRaidTech.dll 时间: $($f.LastWriteTime) 大小: $($f.Length)"
} else {
    Write-Host "警告: 未找到 HskRaidTech.dll,请先运行 build.ps1。" -ForegroundColor Yellow
}
