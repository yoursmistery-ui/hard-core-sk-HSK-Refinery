import os, re, sys, glob
from xml.etree import ElementTree as ET

ROOT = r"c:\Personal\Project\ratkin-patch\鼠族HSK拓展"
PATCH = os.path.join(ROOT, "Patches")

# PatchOperationAdd 可能用 <value><RecipeDef ...> 或 <RecipeDef> 顶层
# 我们扫描所有被 Add 的 RecipeDef 以及已有 Definition
# 先看看 01 号补丁用的是哪种形态

def scan_file(path):
    results = []
    try:
        with open(path, 'rb') as f:
            raw = f.read()
    except Exception as e:
        return results
    for m in re.finditer(rb'<RecipeDef\b[^>]*>(.*?)</RecipeDef>', raw, flags=re.S):
        block = m.group(0)
        dname = re.search(rb'<defName>([^<]+)</defName>', block)
        if not dname: continue
        dname = dname.group(1).decode()
        prod = re.findall(rb'<product>([^<]+)</product>', block)
        res = re.search(rb'<researchPrerequisite>([^<]+)</researchPrerequisite>', block)
        reses = re.findall(rb'<researchPrerequisites>\s*<li>([^<]+)</li>', block)
        results.append({
            'file': os.path.basename(path),
            'defName': dname,
            'products': [p.decode() for p in prod],
            'research': res.group(1).decode() if res else None,
            'researches': [p.decode() for p in reses],
        })
    return results

all_rec = {}
for p in sorted(glob.glob(os.path.join(PATCH, "*.xml"))):
    for r in scan_file(p):
        all_rec.setdefault(r['defName'], r)

print(f"总 RecipeDef Add 数: {len(all_rec)}")
# 分类: 有research / 无research
nores = [r for r in all_rec.values() if not r['research'] and not r['researches']]
hasres = [r for r in all_rec.values() if r['research'] or r['researches']]
print(f"有研究引用: {len(hasres)}  无研究引用: {len(nores)}")

# 列出所有研究的分布
from collections import Counter
c = Counter()
for r in hasres:
    key = r['research'] if r['research'] else (','.join(r['researches']))
    c[key] += 1
print("\n=== 研究引用分布 ===")
for k,v in sorted(c.items(), key=lambda x:-x[1]):
    print(f"  {v:3d}  {k}")