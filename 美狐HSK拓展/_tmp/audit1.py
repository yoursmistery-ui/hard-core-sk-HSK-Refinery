import re, glob, os
# 1. 所有衣物 defName
apparels = set()
for f in glob.glob('1.6/Defs/Apparels/*.xml'):
    txt = open(f, encoding='utf-8-sig').read()
    for m in re.finditer(r'<ThingDef[^>]*>\s*<defName>([^<]+)</defName>', txt):
        apparels.add(m.group(1))
print(f"衣物总数: {len(apparels)}")
# 2. 附件补丁覆盖
patch_txt = open('HSK_1.6/Patches/ThingDefs_Misc/Miho_Apparel.xml', encoding='utf-8').read()
patched = set(re.findall(r'defName="([^"]+)"', patch_txt))
patched = {p for p in patched if 'Miho_Apparel' in p}
print(f"补丁覆盖: {len(patched)}")
uncovered = apparels - patched
print(f"未覆盖: {len(uncovered)}")
for u in sorted(uncovered): print(" ", u)
