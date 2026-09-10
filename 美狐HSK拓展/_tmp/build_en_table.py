# -*- coding: utf-8 -*-
"""构建"生效英文表": 按加载顺序取最终英文文本, 去重, 按领域分批输出给翻译代理。
加载优先级(后者覆盖前者): 1.6 < Cont < Seedsplease < SeedspleaseTranslation < Odyssey < HSK < HSK_1.6
"""
import os, re, json, glob
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ORDER = ['1.6', 'Cont', 'Seedsplease', 'SeedspleaseTranslation', 'Odyssey', 'HSK', 'HSK_1.6']
def has_cjk(s): return bool(re.search(r'[一-鿿]', s or ''))

def lang_dir(folder, kind):
    b = os.path.join(ROOT, folder, 'Languages')
    if not os.path.isdir(b): return None
    for lg in sorted(os.listdir(b)):
        if os.path.isdir(os.path.join(b, lg)) and lg.lower().startswith(kind):
            return os.path.join(b, lg)
    return None

table = {}   # (defType, defName, field) -> {'en':, 'src':folder, 'file':relname, 'kind':'inj'}
for rank, folder in enumerate(ORDER):
    base = lang_dir(folder, 'english')
    if not base: continue
    for f in sorted(glob.glob(os.path.join(base, '**', '*.xml'), recursive=True)):
        try: root = etree.parse(f).getroot()
        except Exception as e: print('FAIL', f, e); continue
        rel = os.path.relpath(f, base).replace('\\', '/')
        if rel.startswith('Keyed/'):
            for c in root:
                if isinstance(c.tag, str) and (c.text or '').strip():
                    table[('KEYED', c.tag, '')] = {
                        'en': (c.text or '').strip(), 'src': folder,
                        'file': 'Keyed/' + os.path.basename(rel), 'rank': rank}
            continue
        dt = rel.split('/')[1] if len(rel.split('/')) >= 3 else '?'
        for c in root:
            if not isinstance(c.tag, str): continue
            seg = c.tag.split('.')
            val = (c.text or '').strip()
            if not val: continue
            table[(dt, seg[0], '.'.join(seg[1:]) or 'label')] = {
                'en': val, 'src': folder,
                'file': rel, 'rank': rank}

# 内联 Defs/Patches(仅当该 key 没有任何英文 DefInjected 时)
INLINE_FIELDS = ('label', 'description', 'jobString', 'deathMessage', 'labelNoun',
                 'gerund', 'labelShort', 'nameFlowTitle', 'reportString',
                 'pawnSingular', 'pawnsPlural', 'ingestCommandString',
                 'ingestReportString', 'letterLabel', 'letterText', 'title', 'titleShort')
for rank, folder in enumerate(ORDER):
    for kind, sub in (('defs', 'Defs'), ('patch', 'Patches')):
        for f in sorted(glob.glob(os.path.join(ROOT, folder, sub, '**', '*.xml'), recursive=True)):
            try: root = etree.parse(f).getroot()
            except Exception: continue
            for node in root:
                if not isinstance(node.tag, str): continue
                if node.get('Abstract') in ('true', 'True'): continue
                dn = node.findtext('defName')
                if not dn: continue
                for fld in node.iter():
                    if not isinstance(fld.tag, str) or fld.tag not in INLINE_FIELDS:
                        continue
                    v = (fld.text or '').strip()
                    if not v or has_cjk(v): continue
                    path, p = [], fld
                    while p is not None and p is not node:
                        par = p.getparent()
                        if par is None: break
                        sibs = [c for c in par if isinstance(c.tag, str)]
                        path.append(str(sibs.index(p)) if p.tag == 'li' else p.tag)
                        p = par
                    path.reverse()
                    key = (node.tag, dn, '.'.join(path))
                    if key in table: continue
                    table[key] = {'en': v, 'src': folder,
                                  'file': 'Inline_%s.xml' % folder, 'rank': rank}

print('生效英文 key 总数: %d' % len(table))
uniq = {}
for k, v in table.items():
    uniq.setdefault(v['en'], []).append(k)
print('去重后唯一英文串: %d (压缩率 %.0f%%)' % (len(uniq), 100 * (1 - len(uniq) / len(table))))

# ---- 分批 ----
def cat(k, v):
    dt, dn, fld = k
    if dt == 'KEYED': return 'keyed'
    if dt == 'AlienRace.AlienBackstoryDef': return 'backstory'
    if fld.startswith('rulePack.rulesStrings') or dt == 'RulePackDef': return 'namer'
    if dt in ('ThingDef',) and re.search(r'(Under|Middle|OnSkin|Hat|Special|Shell|Seasonal|Celestial)', dn):
        return 'apparel'
    if dt in ('ThingDef',) and re.search(r'Weapon|Turret|Apparel_Gun|MeleeWeapon', dn): return 'weapon'
    if dt in ('CombatExtended.AmmoSetDef', 'CombatExtended.AmmoCategoryDef', 'AmmoSetDef'): return 'weapon'
    if fld == 'description': return 'desc_other'
    return 'label_other'

buckets = {}
for k, v in table.items():
    buckets.setdefault(cat(k, v), []).append((k, v))
for b in sorted(buckets, key=lambda x: -len(buckets[x])):
    u = len({v['en'] for k, v in buckets[b]})
    print('  %-14s %5d keys / %5d uniq' % (b, len(buckets[b]), u))

json.dump({'|'.join(k): v for k, v in table.items()},
          open(os.path.join(ROOT, '_tmp/en_table.json'), 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
json.dump({'|'.join(k): v['en'] for k, v in table.items()},
          open(os.path.join(ROOT, '_tmp/en_flat.json'), 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
print('-> _tmp/en_table.json / en_flat.json')
