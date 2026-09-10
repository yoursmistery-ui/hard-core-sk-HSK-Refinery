# 把工作区编译好的 HSKFixPack.dll 同步到游戏 Mods 目录。
# 用法: 先关闭 RimWorld,再运行本脚本(powershell -ExecutionPolicy Bypass -File sync_deploy.ps1)。
$ErrorActionPreference = "Stop"

$src = Join-Path $PSScriptRoot "Assemblies\HSKFixPack.dll"
$dst = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\HSK修复整合\Assemblies\HSKFixPack.dll"

if (-not (Test-Path -LiteralPath $src)) {
    Write-Host "未找到工作区 DLL: $src" -ForegroundColor Red
    exit 1
}
if (Get-Process -Name "RimWorld*" -ErrorAction SilentlyContinue) {
    Write-Host "检测到 RimWorld 仍在运行,DLL 被占用。请先关闭游戏再运行本脚本。" -ForegroundColor Yellow
    exit 1
}

Copy-Item -LiteralPath $src -Destination $dst -Force
$new = Get-Item -LiteralPath $dst
Write-Host "已同步: $($new.FullName)" -ForegroundColor Green
Write-Host "时间: $($new.LastWriteTime) 大小: $($new.Length)"
