# -*- coding: utf-8 -*-
import re

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
raw = open(UNIFIED, 'r', encoding='utf-16').read().encode('utf-8')

PAT = re.compile(rb'<ResearchProjectDef\b[^>]*>(.*?)</ResearchProjectDef>', flags=re.S)
def get(block, tag):
    global _br
    try:
        m = re.search(rb'<' + tag.encode() + rb'>([^<]+)</' + tag.encode() + rb'>', block)
        return m.group(1).decode() if m else ''
    except: return ''

print("=== 所有 ResearchProjectDef 中 ratio/ratkin 相关节点 ===")
for m in PAT.finditer(raw):
    block = m.group(1)
    dn = get(block, 'defName')
    if not dn: continue
    if re.search(r'Ratkin|RK_|瑞', dn):
        label = get(block, 'label')
        tab = get(block, 'tab')
        x = get(block, 'researchViewX')
        y = get(block, 'researchViewY')
        cost = get(block, 'baseCost')
        prereq = re.findall(rb'<prerequisites>\s*<li>([^<]+)</li>', block)
        prereq = [p.decode() for p in prereq if re.match(rb'[A-Za-z]', p)]
        tags = get(block, 'tags')
        print(f"{dn}\tlabel={label}\ttab={tab}\tX={x}\tY={y}\tcost={cost}\tprereq={prereq}\ttags={tags}")