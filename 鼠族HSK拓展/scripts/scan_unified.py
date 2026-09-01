import os, re, glob
from collections import OrderedDict

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
with open(UNIFIED, 'r', encoding='utf-16') as f:
    raw = f.read().encode('utf-8')

print(f"Unified.xml 解码后大小: {len(raw)} bytes")

# ThingDef 中文 label 映射
label_map = {}
for m in re.finditer(rb'<ThingDef\b[^>]*>(.*?)</ThingDef>', raw, flags=re.S):
    block = m.group(1)
    dn = re.search(rb'<defName>([^<]+)</defName>', block)
    if not dn: continue
    dn = dn.group(1).decode()
    lab = re.search(rb'<label>([^<]+)</label>', block)
    if lab:
        label_map[dn] = lab.group(1).decode()

print(f"ThingDef label 映射数: {len(label_map)}")

# 扫描所有 RecipeDef
recipes = OrderedDict()
for m in re.finditer(rb'<RecipeDef\b[^>]*>(.*?)</RecipeDef>', raw, flags=re.S):
    block = m.group(0)
    dn = re.search(rb'<defName>([^<]+)</defName>', block)
    if not dn: continue
    dn = dn.group(1).decode()
    # products
    prod = None
    pm = re.search(rb'<products>(.*?)</products>', block, flags=re.S)
    if pm:
        psub = re.search(rb'<([A-Za-z_][\w]*)>', pm.group(1))
        if psub: prod = psub.group(1).decode()
    if not prod:
        pm = re.search(rb'<product>([^<]+)</product>', block)
        if pm: prod = pm.group(1).decode()
    # research (单数优先, 复数其次)
    res = None
    rm = re.search(rb'<researchPrerequisite>([^<]+)</researchPrerequisite>', block)
    if rm: res = rm.group(1).decode()
    if not res:
        rm = re.search(rb'<researchPrerequisites>\s*<li>([^<]+)</li>', block)
        if rm: res = rm.group(1).decode() + ' (list)'
    recipes[dn] = {'defName': dn, 'product': prod, 'research': res}

# 只关心鼠族科技节点
RK_NODES = re.compile(r'Ratkin_|Melee_Ultra')
groups = OrderedDict()
uncat = []
for r in recipes.values():
    if r['research']:
        groups.setdefault(r['research'], []).append(r)
    else:
        uncat.append(r)

print(f"\n总RecipeDef: {len(recipes)}  (含源文件+补丁最终态)")

def pl(p):
    if not p: return '?'
    return f"{p} | {label_map.get(p,'?')}"

# 输出鼠族节点
interest = [k for k in groups if RK_NODES.search(k)]
interest.sort()
for key in interest:
    items = groups[key]
    print(f"\n{'='*72}")
    print(f"[{key}]  {len(items)}条")
    for r in sorted(items, key=lambda x:x['defName']):
        print(f"   {r['defName']:<48} -> {pl(r['product'])}")

# 非鼠族节点仅汇总
other = [(k, len(v)) for k,v in groups.items() if not RK_NODES.search(k)]
print("\n" + "="*72)
print("非鼠族科技节点分布(供参考):")
for k,v in sorted(other, key=lambda x:-x[1]):
    print(f"  {v:3d}  {k}")