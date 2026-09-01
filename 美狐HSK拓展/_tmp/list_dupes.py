# -*- coding: utf-8 -*-
"""列出中文层里同一键出现多个不同译文的冲突(加载顺序决定谁生效, 不确定)。"""
import glob, os, json
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
BASES = [os.path.join(ROOT, 'HSK', 'Languages', 'ChineseSimplified (简体中文)'),
         os.path.join(ROOT, 'HSK_1.6', 'Languages', 'ChineseSimplified (简体中文)')]
ALIAS = {'FactionDefs': 'FactionDef', 'GeneDefs': 'GeneDef', 'MemeDefs': 'MemeDef'}
tab = json.load(open(os.path.join(ROOT, '_tmp/en_table.json'), encoding='utf-8'))

occ = {}
for base in BASES:
    if not os.path.isdir(base): continue
    for f in glob.glob(os.path.join(base, '**', '*.xml'), recursive=True):
        rel = os.path.relpath(f, base).replace(os.sep, '/')
        parts = rel.split('/')
        if len(parts) < 3 or parts[0] != 'DefInjected': continue
        dt = ALIAS.get(parts[1], parts[1])
        try:
            r = etree.parse(f).getroot()
        except Exception:
            continue
        for c in r:
            if not isinstance(c.tag, str): continue
            seg = c.tag.split('.')
            occ.setdefault((dt, seg[0], '.'.join(seg[1:]) or 'label'), []).append(
                ((c.text or '').strip(), rel))

# 英文表里该键的来源优先级(HSK > Cont)
rank = {'1.6': 0, 'Cont': 1, 'Seedsplease': 2, 'SeedspleaseTranslation': 3,
        'Odyssey': 4, 'HSK': 5, 'HSK_1.6': 6}
def en_src_rank(dt, dn, fld):
    for cand in (dt, [k for k, v in rank.items()]):
        pass
    best = -1
    for tk, tv in tab.items():
        t, d, fl = tk.split('|')
        if d == dn and fl == fld and ALIAS.get(t, t) == dt:
            best = max(best, rank.get(tv['src'], -1))
    return best

n = 0
for k, v in sorted(occ.items()):
    if len(v) > 1 and len({x[0] for x in v}) > 1:
        n += 1
        print('%s | %s | %s' % k)
        for val, src in v:
            print('    [%s] %s' % (os.path.basename(src)[:34], val[:70]))
print('冲突键数:', n)
