import os, re, glob
from collections import OrderedDict

ROOT = r"c:\Personal\Project\ratkin-patch\鼠族HSK拓展"

# 1. 收集所有 ThingDef 产物中文 label
label_map = {}  # defName -> (label, thingClass)
for d in ['Defs','Patches']:
    for p in glob.glob(os.path.join(ROOT, d, "**", "*.xml"), recursive=True):
        try:
            with open(p,'rb') as f: raw=f.read()
        except: continue
        for m in re.finditer(rb'<ThingDef\b[^>]*>(.*?)</ThingDef>', raw, flags=re.S):
            block=m.group(1)
            dn=re.search(rb'<defName>([^<]+)</defName>', block)
            if not dn: continue
            dn=dn.group(1).decode()
            tc=re.search(rb'<thingClass>([^<]+)</thingClass>', block)
            lab=re.search(rb'<label>([^<]+)</label>', block)
            label_map.setdefault(dn,{
                'label': lab.group(1).decode() if lab else dn,
                'thingClass': tc.group(1).decode() if tc else '',
                'src': os.path.basename(p)
            })

# 2. 扫描所有配方
recipes=OrderedDict()
for p in sorted(glob.glob(os.path.join(ROOT,'Patches',"*.xml"))):
    try:
        with open(p,'rb') as f: raw=f.read()
    except: continue
    for m in re.finditer(rb'<RecipeDef\b[^>]*>(.*?)</RecipeDef>', raw, flags=re.S):
        block=m.group(0)
        dn=re.search(rb'<defName>([^<]+)</defName>', block)
        if not dn: continue
        dn=dn.group(1).decode()
        prod=None
        pm=re.search(rb'<products>(.*?)</products>', block, flags=re.S)
        if pm:
            psub=re.search(rb'<([A-Za-z_][\w]*)>', pm.group(1))
            if psub: prod=psub.group(1).decode()
        if not prod:
            pm=re.search(rb'<product>([^<]+)</product>', block)
            if pm: prod=pm.group(1).decode()
        res=None; rm=re.search(rb'<researchPrerequisite>([^<]+)</researchPrerequisite>', block)
        if rm: res=rm.group(1).decode()
        recipes[dn]={'defName':dn,'product':prod,'research':res,'file':os.path.basename(p)}

# 组成研究分组
groups=OrderedDict()
for r in recipes.values():
    key=r['research'] or '(NO RESEARCH)'
    groups.setdefault(key,[]).append(r)

def prod_label(p):
    if not p: return '?'
    info=label_map.get(p)
    return f"{p} ({info['label']})" if info else f"{p} (?)"

width=55
for key,items in groups.items():
    print(f"\n{'='*70}")
    print(f"研究: {key}   [{len(items)}条配方]")
    for r in sorted(items, key=lambda x: x['defName']):
        print(f"  {r['defName']:<55} -> {prod_label(r['product'])}")

print(f"\n\n总计配方 {len(recipes)} 条, 分属 {len(groups)} 个研究节点")
# 无产物(可能jobString类)
noprod=[r for r in recipes.values() if not r['product']]
print(f"无product的配方: {len(noprod)}")