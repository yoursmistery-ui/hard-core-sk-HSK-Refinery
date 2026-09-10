$ErrorActionPreference = 'Stop'
$csc = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$managed = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$harmony = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Harmony\Current\Assemblies\0Harmony.dll'
$out = 'C:\Personal\Project\ratkin-patch\QualityBuilder\Assemblies\QualityBuilder.dll'
$src = Get-ChildItem 'C:\Personal\Project\ratkin-patch\QualityBuilder\_PROJECT' -Recurse -Filter *.cs | ForEach-Object { $_.FullName }

$args = @(
    '/nologo', '/target:library', '/optimize+', '/langversion:7.3',
    ('/out:' + $out),
    ('/reference:' + $managed + '\Assembly-CSharp.dll'),
    ('/reference:' + $managed + '\UnityEngine.dll'),
    ('/reference:' + $managed + '\UnityEngine.CoreModule.dll'),
    ('/reference:' + $managed + '\UnityEngine.TextRenderingModule.dll'),
    ('/reference:' + $managed + '\UnityEngine.IMGUIModule.dll'),
    ('/reference:' + $harmony),
    '/reference:System.dll', '/reference:System.Core.dll',
    ('/reference:' + $managed + '\netstandard.dll')
) + $src

& $csc $args 2>&1 | Select-Object -First 30
Write-Host ('EXIT: ' + $LASTEXITCODE)
