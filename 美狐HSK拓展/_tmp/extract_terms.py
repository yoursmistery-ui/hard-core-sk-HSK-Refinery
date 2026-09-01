# -*- coding: utf-8 -*-
"""HSK 官方中文术语对照: ZH key (X.label) × Core_SK/Ratkin Defs 内联英文 label。
输出 _tmp/hsk_terms.json  {英文术语(小写): 中文}
"""
import os, re, glob, json
from lxml import etree

MODS = r'C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods'
ZH_BASE = os.path.join(MODS, '1.6HSK核心汉化', '1.6', 'Languages', 'ChineseSimplified (简体中文)')
SRC_DEFS = ['Core_SK', 'Core_SK_CoreModule', 'RatkinRaceHSK', 'ResearchTree_SK']

# 1) 索引 defs: defName -> {label/description/...}
defs = {}
for m in SRC_DEFS:
    for f in glob.glob(os.path.join(MODS, m, '**', 'Defs', '**', '*.xml'), recursive=True):
        try: root = etree.parse(f).getroot()
        except Exception: continue
        for node in root:
            if not isinstance(node.tag, str): continue
            dn = node.findtext('defName')
            if not dn: continue
            d = defs.setdefault(dn, {})
            lbl = node.findtext('label')
            if lbl: d.setdefault('label', lbl.strip())
            desc = node.findtext('description')
            if desc: d.setdefault('description', desc.strip())
print('索引 defs: %d' % len(defs))

# 2) 读中文层
pairs = {}
n_zh = 0
for f in glob.glob(os.path.join(ZH_BASE, 'DefInjected', '**', '*.xml'), recursive=True):
    try: root = etree.parse(f).getroot()
    except Exception: continue
    for c in root:
        if not isinstance(c.tag, str) or '.' not in c.tag: continue
        v = (c.text or '').strip()
        if not v or not re.search(r'[一-鿿]', v): continue
        n_zh += 1
        dn, fld = c.tag.split('.', 1)
        if fld not in ('label', 'description'): continue
        en = defs.get(dn, {}).get(fld)
        if not en: continue
        pairs.setdefault(en.lower(), set()).add(v)
print('中文键 %d, 可比对术语 %d' % (n_zh, len(pairs)))

out = {}
for e, zs in pairs.items():
    if len(zs) != 1: continue
    z = list(zs)[0]
    if len(e) <= 60:
        out[e] = z
json.dump(dict(sorted(out.items())), open('_tmp/hsk_terms.json', 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
print('一对一术语: %d -> _tmp/hsk_terms.json' % len(out))

# 3) 打印与美狐内容相关的术语
import collections
tab = json.load(open('_tmp/en_table.json', encoding='utf-8'))
alltext = ' '.join(v['en'] for v in tab.values()).lower()
hit = [(e, z) for e, z in out.items() if len(e) >= 4 and e in alltext]
hit.sort(key=lambda x: -len(x[0]))
print('\n美狐文本中命中的 HSK 既有术语 (%d):' % len(hit))
for e, z in hit[:200]:
    print('  %-46s %s' % (e[:46], z[:40]))
