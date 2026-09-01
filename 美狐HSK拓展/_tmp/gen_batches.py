# -*- coding: utf-8 -*-
"""按领域把"唯一英文串"切分成翻译批次, 供并行代理使用。
产出: _tmp/batches/in_NN.json  (id -> {en, ctx, field, old})
       _tmp/batches/map_NN.json(id -> [defType|defName|field, ...])
       _tmp/batches/index.json (批次清单)
"""
import os, re, json, glob, collections

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, '_tmp', 'batches')
os.makedirs(OUT, exist_ok=True)
for old in glob.glob(os.path.join(OUT, '*.json')):
    os.remove(old)

tab = json.load(open(os.path.join(ROOT, '_tmp/en_table.json'), encoding='utf-8'))
# 旧译参考
zh = {}
for f in glob.glob(os.path.join(ROOT, 'HSK/Languages/ChineseSimplified (简体中文)/**/*.xml'), recursive=True):
    import lxml.etree as ET
    try:
        r = ET.parse(f).getroot()
    except Exception:
        continue
    for c in r:
        if isinstance(c.tag, str) and (c.text or '').strip():
            zh.setdefault(c.tag, (c.text or '').strip())

def kind_of(dt, dn, fld):
    if dt == 'KEYED': return 'keyed'
    if dt == 'AlienRace.AlienBackstoryDef': return 'backstory'
    if fld.startswith('rulePack.rulesStrings') or dt == 'RulePackDef': return 'namer'
    if dt == 'ThingDef' and re.search(r'(Under|Middle|OnSkin|Hat|Special|Shell|Seasonal|Celestial|Apparel)', dn): return 'apparel'
    if dt in ('ThingDef', 'CombatExtended.AmmoSetDef', 'CombatExtended.AmmoCategoryDef', 'AmmoSetDef') and \
       re.search(r'Weapon|Turret|Gun|Melee|Ammo|Bullet|Shell|Projectile|Mortar|Artillery', dn): return 'weapon'
    if fld in ('description', 'baseDesc', 'deathMessage', 'jobString', 'reportString',
               'letterText', 'ingestReportString'): return 'desc_' + dt
    return 'label_' + dt

groups = collections.defaultdict(list)   # kind -> [key...]
for k in tab:
    dt, dn, fld = k.split('|')
    groups[kind_of(dt, dn, fld)].append(k)

# 合并小 defType 到通用桶
merged = collections.defaultdict(list)
for kind, keys in groups.items():
    if kind.startswith('desc_') and kind != 'desc_ThingDef':
        merged['desc_misc'].extend(keys)
    elif kind.startswith('label_') and kind not in ('label_ThingDef', 'label_HediffDef',
                                                    'label_AbilityDef', 'label_RulePackDef'):
        merged['label_misc'].extend(keys)
    else:
        merged[kind].extend(keys)

for kind in merged:
    merged[kind] = sorted(set(merged[kind]))

TARGET = 190   # 每批唯一串上限
plan = []
for kind in sorted(merged, key=lambda x: -len(merged[x])):
    keys = merged[kind]
    uniq = {}
    for k in keys:
        uniq.setdefault(tab[k]['en'], []).append(k)
    us = sorted(uniq)
    nparts = max(1, -(-len(us) // TARGET))
    size = -(-len(us) // nparts)
    for i in range(nparts):
        part = us[i * size:(i + 1) * size]
        if not part: continue
        plan.append((kind, i + 1, nparts, part, uniq))

idx = []
gid = 0
for seq, (kind, part, nparts, uniq_list, uniq) in enumerate(plan, 1):
    gid += 1
    bid = 'b%02d' % gid
    inbox, mapping = {}, {}
    for j, en in enumerate(uniq_list, 1):
        sid = '%s_%04d' % (bid, j)
        first = uniq[en][0]
        dt, dn, fld = first.split('|')
        inbox[sid] = {'en': en, 'ctx': '%s / %s' % (dt, dn), 'field': fld,
                      'n': len(uniq[en])}
        old = zh.get('%s.%s' % (dn, fld), '')
        if old: inbox[sid]['old'] = old
        mapping[sid] = uniq[en]
    json.dump(inbox, open(os.path.join(OUT, 'in_%s.json' % bid), 'w', encoding='utf-8'),
              ensure_ascii=False, indent=1)
    json.dump(mapping, open(os.path.join(OUT, 'map_%s.json' % bid), 'w', encoding='utf-8'),
              ensure_ascii=False)
    idx.append({'id': bid, 'kind': kind, 'part': part, 'of': nparts,
                'n': len(uniq_list), 'keys': sum(len(v) for v in mapping.values()),
                'in': '_tmp/batches/in_%s.json' % bid, 'out': '_tmp/batches/out_%s.json' % bid})

json.dump(idx, open(os.path.join(OUT, 'index.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('批次数: %d' % len(idx))
tot_s = tot_k = 0
for b in idx:
    tot_s += b['n']; tot_k += b['keys']
    print('  %s %-14s %d/%d  唯一串 %3d  键 %4d  %s' % (b['id'], b['kind'], b['part'], b['of'], b['n'], b['keys'], b['out']))
print('合计 唯一串 %d, 覆盖键 %d' % (tot_s, tot_k))
