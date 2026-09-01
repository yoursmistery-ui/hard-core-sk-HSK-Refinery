# -*- coding: utf-8 -*-
import re, collections

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
raw = open(UNIFIED, 'r', encoding='utf-16').read().encode('utf-8')

recipes = {}
for m in re.finditer(rb'<RecipeDef\b[^>]*>(.*?)</RecipeDef>', raw, flags=re.S):
    block = m.group(0)
    dn = re.search(rb'<defName>([^<]+)</defName>', block)
    if not dn: continue
    dn = dn.group(1).decode()
    prod = None
    pm = re.search(rb'<products>(.*?)</products>', block, flags=re.S)
    if pm:
        ps = re.search(rb'<([A-Za-z_][\w]*)>', pm.group(1))
        if ps: prod = ps.group(1).decode()
    if not prod:
        pm = re.search(rb'<product>([^<]+)</product>', block)
        if pm: prod = pm.group(1).decode()
    res = None
    rm = re.search(rb'<researchPrerequisite>([^<]+)</researchPrerequisite>', block)
    if rm: res = rm.group(1).decode()
    recipes[dn] = {'product': prod, 'research': res}

# 应用91号补丁的迁移
patch = open(r'c:\Personal\Project\ratkin-patch\鼠族HSK拓展\Patches\91_鼠族衣物科技树拆分.xml', encoding='utf-8-sig').read()
# 提取 Replace 操作: RecipeDef[defName="X"]/researchPrerequisite -> Y
pattern = r'RecipeDef\[defName="([^"]+)"\]/researchPrerequisite.*?<researchPrerequisite>([^<]+)</researchPrerequisite>'
moves = re.findall(pattern, patch, flags=re.S)
applied = 0
miss = []
for rdn, tgt in moves:
    if rdn in recipes:
        if recipes[rdn]['research'] != tgt:
            recipes[rdn]['research'] = tgt
            applied += 1
    else:
        miss.append(rdn)
print(f"迁移操作数: {len(moves)}, 实际应用: {applied}, 找不到配方: {len(miss)}")
if miss:
    for m in miss: print(f"  缺失: {m}")

# 统计最终态各鼠族节点件数
nodes = collections.Counter()
details = collections.defaultdict(list)
for dn, r in recipes.items():
    res = r['research']
    if res and ('Ratkin_' in res or res == 'Melee_Ultra'):
        nodes[res] += 1
        details[res].append((dn, r['product']))

print()
print("=== 拆分后鼠族科技节点件数 ===")
for k in sorted(nodes):
    v = nodes[k]
    flag = ' ⚠️超载' if v > 10 else (' ✅' if v >= 5 else ' 偏少')
    print(f"  {k}: {v}件{flag}")

print()
print("=== 各节点具体配方（供检查）===")
for k in sorted(details):
    print(f"\n[{k}] {len(details[k])}件")
    for dn, p in sorted(details[k]):
        print(f"  {dn} -> {p}")