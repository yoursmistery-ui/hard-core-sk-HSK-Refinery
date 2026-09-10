# -*- coding: utf-8 -*-
# 产出: 鼠族衣物/装备/武器全物品清单, 标注类别, 作为拆分规划依据
import re

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
with open(UNIFIED, 'r', encoding='utf-16') as f:
    raw = f.read().encode('utf-8')

label_map = {}
for m in re.finditer(rb'<ThingDef\b[^>]*>(.*?)</ThingDef>', raw, flags=re.S):
    block = m.group(1)
    dn = re.search(rb'<defName>([^<]+)</defName>', block)
    if not dn: continue
    dn = dn.group(1).decode()
    lab = re.search(rb'<label>([^<]+)</label>', block)
    category = re.search(rb'<category>([^<]+)</category>', block)
    thingClass = re.search(rb'<thingClass>([^<]+)</thingClass>', block)
    if lab:
        label_map[dn] = {
            'label': lab.group(1).decode(),
            'category': category.group(1).decode() if category else '',
            'thingClass': thingClass.group(1).decode() if thingClass else ''
        }

recipes = {}
for m in re.finditer(rb'<RecipeDef\b[^>]*>(.*?)</RecipeDef>', raw, flags=re.S):
    block = m.group(0)
    dn = re.search(rb'<defName>([^<]+)</defName>', block)
    if not dn: continue
    dn = dn.group(1).decode()
    prod = None
    pm = re.search(rb'<products>(.*?)</products>', block, flags=re.S)
    if pm:
        psub = re.search(rb'<([A-Za-z_][\w]*)>', pm.group(1))
        if psub: prod = psub.group(1).decode()
    if not prod:
        pm = re.search(rb'<product>([^<]+)</product>', block)
        if pm: prod = pm.group(1).decode()
    res = None
    rm = re.search(rb'<researchPrerequisite>([^<]+)</researchPrerequisite>', block)
    if rm: res = rm.group(1).decode()
    if not res:
        rm = re.search(rb'<researchPrerequisites>\s*<li>([^<]+)</li>', block)
        if rm: res = rm.group(1).decode() + ' (list)'
    recipes[dn] = {'product': prod, 'research': res}

RK_NODES = re.compile(r'Ratkin_|Melee_Ultra')
print("=== 鼠族科技线上全部物品 (research -> 物品) ===")
for r in recipes.values():
    if r['research'] and RK_NODES.search(r['research']):
        p = r['product']
        info = label_map.get(p, {})
        print(f"{r['research']}\t{p}\t{info.get('label','?')}\t{info.get('category','')}\t{info.get('thingClass','')}")