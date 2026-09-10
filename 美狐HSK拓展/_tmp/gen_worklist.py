# -*- coding: utf-8 -*-
"""生成待翻译工作清单: (folder, defType, key, field, en_text)
来源 = 英文 DefInjected 全量 ∪ Defs/Patches 内联英文(未被前者优先覆盖的部分)
"""
import os, re, json, glob
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ACTIVE = ['1.6', 'Cont', 'Seedsplease', 'SeedspleaseTranslation', 'Odyssey', 'HSK', 'HSK_1.6']
def has_cjk(s): return bool(re.search(r'[\u4e00-\u9fff]', s or ''))

def lang_dirs(kind):
    out = []
    for a in ACTIVE:
        b = os.path.join(ROOT, a, 'Languages')
        if not os.path.isdir(b): continue
        for lg in sorted(os.listdir(b)):
            if os.path.isdir(os.path.join(b, lg)) and lg.lower().startswith(kind):
                out.append((a, os.path.join(b, lg)))
    return out

def collect(base_list):
    inj, keyed = {}, {}
    for folder, base in base_list:
        for f in sorted(glob.glob(os.path.join(base, '**', '*.xml'), recursive=True)):
            try: root = etree.parse(f).getroot()
            except Exception as e: print('FAIL', f, e); continue
            rel = os.path.relpath(f, base).replace('\\', '/')
            if rel.startswith('Keyed/'):
                for c in root:
                    if isinstance(c.tag, str):
                        keyed[(folder, c.tag)] = (c.text or '').strip()
                continue
            dt = rel.split('/')[1] if len(rel.split('/')) >= 3 else '?'
            for c in root:
                if not isinstance(c.tag, str): continue
                seg = c.tag.split('.')
                inj[(folder, dt, seg[0], '.'.join(seg[1:]) or 'label')] = (c.text or '').strip()
    return inj, keyed

en_inj, en_keyed = collect(lang_dirs('english'))
zh_inj, zh_keyed = collect(lang_dirs('chinese'))

# 内联 defs
inline = {}
for a in ACTIVE:
    for f in sorted(glob.glob(os.path.join(ROOT, a, 'Defs', '**', '*.xml'), recursive=True)):
        try: root = etree.parse(f).getroot()
        except Exception: continue
        for node in root:
            if not isinstance(node.tag, str): continue
            dn = node.findtext('defName')
            if not dn: continue
            for fld in node.iter():
                if not isinstance(fld.tag, str) or fld.tag not in ('label', 'description'): continue
                v = (fld.text or '').strip()
                if not v: continue
                path, p = [], fld
                while p is not None and p is not node:
                    par = p.getparent()
                    if par is None: break
                    sibs = [c for c in par if isinstance(c.tag, str)]
                    path.append(str(sibs.index(p)) if p.tag == 'li' else p.tag)
                    p = par
                path.reverse()
                inline.setdefault((a, node.tag, dn, '.'.join(path)), v)
    for f in sorted(glob.glob(os.path.join(ROOT, a, 'Patches', '**', '*.xml'), recursive=True)):
        try: root = etree.parse(f).getroot()
        except Exception: continue
        for el in root.iter():
            if not isinstance(el.tag, str): continue
            dn = el.findtext('defName')
            if not dn: continue
            for fld in el.iter('label', 'description'):
                v = (fld.text or '').strip()
                if v:
                    inline.setdefault((a, '__patch__', dn, fld.tag), v)

# 有效中文(值含 CJK 才算已翻译)
zh_ok = {k: v for k, v in zh_inj.items() if has_cjk(v)}

# 工作清单: 英文层所有 key, 中文未有效覆盖
work = []
seen = set()
for k, v in sorted(en_inj.items()):
    if not v or k in zh_ok: continue
    work.append({'folder': k[0], 'defType': k[1], 'def': k[2], 'field': k[3], 'en': v})
    seen.add((k[1], k[2], k[3])); seen.add((k[0], k[1], k[2], k[3]))

# 内联英文: 无任何中文覆盖, 且英文层也没有同 defName.field(避免重复)
inline_only = []
for (a, dt, dn, fld), v in sorted(inline.items()):
    if has_cjk(v): continue
    if (a, dt, dn, fld) in zh_ok: continue
    if any((dt, dn, fld) == s or ('ANY', dn, fld) == s for s in [x[:4] for x in seen]): continue
    if any(k[2] == dn and k[3] == fld for k in zh_ok): continue   # 同名 def 已有中文
    if any(k[2] == dn and k[3] == fld for k in en_inj): continue  # 英文层会覆盖
    inline_only.append({'folder': a, 'defType': dt, 'def': dn, 'field': fld, 'en': v})
work += inline_only

# 分组统计
def bucket(w):
    dt, d = w['defType'], w['def']
    if dt == 'AlienRace.AlienBackstoryDef' or 'Backstory' in d: return '背景故事'
    if 'rulesStrings' in w['field'] or dt == 'RulePackDef': return '命名词库(RulePack)'
    if w['field'] == 'description': return '描述'
    return '短标签/其它'

stat = {}
for w in work:
    b = bucket(w)
    stat.setdefault(b, {'n': 0, 'chars': 0})
    stat[b]['n'] += 1
    stat[b]['chars'] += len(w['en'])
print('待翻译条目: %d (其中仅内联无英文层的 %d)' % (len(work), len(inline_only)))
for b, s in sorted(stat.items()):
    print('  %-18s %5d 条  英文字符 %7d' % (b, s['n'], s['chars']))

byfile = {}
for w in work:
    byfile.setdefault((w['folder'], w['defType']), []).append(w)
print('\n按 defType 分布:')
for (fo, dt), lst in sorted(byfile.items()):
    print('  %-12s %-34s %5d' % (fo, dt, len(lst)))

json.dump(work, open(os.path.join(ROOT, '_tmp/work_list.json'), 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
print('\n-> _tmp/work_list.json')
