# -*- coding: utf-8 -*-
"""双目录同步: 工作区 美狐HSK拓展 -> 部署 Mods/美狐HSK拓展
- 逐文件复制(中文路径安全, 不用 bash 循环)
- 只清理"中文汉化目录"里工作区已不存在的陈旧文件(移入回收站, 不永久删除)
- 复制后逐文件字节比对, 报告差异
"""
import os, sys, shutil, subprocess, filecmp

SRC = r'C:/Personal/Project/ratkin-patch/美狐HSK拓展'
DST = r'C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods/美狐HSK拓展'
PRUNE_SCOPE = ('HSK/Languages/ChineseSimplified', 'HSK_1.6/Languages/ChineseSimplified')
# 工作区专用目录, 不进部署
EXCLUDE_PREFIX = ('_tmp/', '.git/', '__pycache__/')

def excl(r):
    return r.startswith(EXCLUDE_PREFIX)

def tree(root):
    out = {}
    for dirpath, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in ('.git', '.trash', '__pycache__')]
        for fn in files:
            p = os.path.join(dirpath, fn)
            r = os.path.relpath(p, root).replace('\\', '/')
            if excl(r):
                continue
            out[r] = p
    return out

if not os.path.isdir(DST):
    print('!! 部署目录不存在:', DST); sys.exit(1)

s, d = tree(SRC), tree(DST)
to_copy = [r for r in s if r not in d or not filecmp.cmp(s[r], d[r], shallow=False)]
prune = [r for r in d if r not in s and r.startswith(PRUNE_SCOPE)]
other_only = [r for r in d if r not in s and not r.startswith(PRUNE_SCOPE)]

print('工作区文件 %d | 部署文件 %d' % (len(s), len(d)))
print('需复制 %d 个 | 需清理(仅中文层) %d 个 | 其它仅部署存在 %d 个' %
      (len(to_copy), len(prune), len(other_only)))
for r in to_copy[:400]:
    print('  +', r)
if '--dry' in sys.argv:
    sys.exit(0)

for r in prune:
    print('  回收站:', r)
    ps = ('Add-Type -AssemblyName Microsoft.VisualBasic;'
          "[Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile('%s','OnlyErrorDialogs','SendToRecycleBin')"
          % d[r].replace('/', '\\'))
    p = subprocess.run(['powershell.exe', '-NoProfile', '-Command', ps],
                       capture_output=True, text=True)
    if p.returncode != 0 or os.path.exists(d[r]):
        print('    !! 回收失败:', p.stderr.strip()[:200])

for r in to_copy:
    dstf = os.path.join(DST, *r.split('/'))
    os.makedirs(os.path.dirname(dstf), exist_ok=True)
    shutil.copy2(s[r], dstf)

# 校验
s2, d2 = tree(SRC), tree(DST)
only_src = sorted(set(s2) - set(d2))
only_dst = [r for r in sorted(set(d2) - set(s2)) if not r.startswith(PRUNE_SCOPE)]
diff = [r for r in s2 if r in d2 and not filecmp.cmp(s2[r], d2[r], shallow=False)]
print('\n同步后校验: 仅工作区 %d | 仅部署 %d | 内容不一致 %d' %
      (len(only_src), len(only_dst), len(diff)))
for r in only_src[:20]: print('   缺:', r)
for r in only_dst[:20]: print('   多:', r)
for r in diff[:20]: print('   异:', r)
print('\n部署中文层文件数:',
      len([r for r in d2 if r.startswith(PRUNE_SCOPE)]),
      '| 工作区中文层文件数:', len([r for r in s2 if r.startswith(PRUNE_SCOPE)]))
nest = os.path.join(DST, '美狐HSK拓展')
print('嵌套目录残留检查:', '存在!!' if os.path.isdir(nest) else '无')
