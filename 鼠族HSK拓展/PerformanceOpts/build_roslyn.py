import subprocess, os, glob
dotnet = r'C:\Program Files\dotnet\dotnet.exe'
csc_dll = r'C:\Personal\Project\ratkin-patch\tmp\rkperf_build\roslyn\tasks\netcore\bincore\csc.dll'
game = r'C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
managed = os.path.join(game, 'RimWorldWin64_Data', 'Managed')
refs = []
# 基础库
for f in sorted(glob.glob(os.path.join(managed, 'mscorlib.dll'))) + \
         sorted(glob.glob(os.path.join(managed, 'netstandard.dll'))) + \
         sorted(glob.glob(os.path.join(managed, 'System*.dll'))):
    refs.append(f)
# 游戏主程序集
refs.append(os.path.join(managed, 'Assembly-CSharp.dll'))
# UnityEngine 模块
for f in sorted(glob.glob(os.path.join(managed, 'UnityEngine*.dll'))):
    refs.append(f)
for f in sorted(glob.glob(os.path.join(managed, 'Unity*.dll'))):
    if f not in refs:
        refs.append(f)
# Harmony
refs.append(os.path.join(game, 'Mods', 'Harmony', 'Current', 'Assemblies', '0Harmony.dll'))

src = r'C:\Personal\Project\ratkin-patch\tmp\rkperf_build\Source\RKPerf.cs'
out = r'C:\Personal\Project\ratkin-patch\tmp\rkperf_build\RKPerf.dll'
cmd = [dotnet, csc_dll, '/nologo', '/target:library', '/out:' + out]
for r in refs:
    cmd.append('/r:' + r)
cmd.append(src)
p = subprocess.run(cmd, capture_output=True, errors='replace', text=True)
print('EXIT:', p.returncode)
print(p.stdout[-4000:] if p.stdout else '(no stdout)')
print('OUT EXISTS:', os.path.exists(out))
