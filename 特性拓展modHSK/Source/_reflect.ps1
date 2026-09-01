$ErrorActionPreference='SilentlyContinue'
$managed='C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
$modsRoot='C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods'
$cache=@{}
$handler=[System.ResolveEventHandler]{
  param($s,$e)
  $n=$e.Name.Split(',')[0]
  if($cache.ContainsKey($n)){ return $cache[$n] }
  $cand=$null
  $p1=Join-Path $managed ($n+'.dll'); if(Test-Path $p1){ $cand=$p1 }
  if(-not $cand){ $f=Get-ChildItem -Path $modsRoot -Recurse -Filter ($n+'.dll') -ErrorAction SilentlyContinue | Select-Object -First 1; if($f){$cand=$f.FullName} }
  if($cand){ $a=[System.Reflection.Assembly]::LoadFrom($cand); $cache[$n]=$a; return $a }
  return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
$target='C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RatkinRaceHSK\1.6\Assemblies\NewRatkin.dll'
$f=Get-ChildItem -Path $target -Recurse -Filter '*.dll' | Select-Object -First 1
"Probing: " + $f.FullName
$a=[System.Reflection.Assembly]::LoadFrom($f.FullName)
$a.GetReferencedAssemblies() | ForEach-Object { $_.Name } | Sort-Object -Unique