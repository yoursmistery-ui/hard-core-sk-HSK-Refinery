# -*- coding: utf-8 -*-
"""汉化完整性审计(扁平 DefInjected 键格式)。

输出:
  A. 中文层里值仍为纯英文的条目(未翻译)
  B. 中文层里疑似词表替换破坏(中文夹在英文单词中间)
  C. 英文层有、中文层缺失的 key
  D. Defs/Patches 内联英文、中文层无覆盖的 key
  E. Keyed 缺失/未翻译
"""
import os, re, json, glob
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ACTIVE = ['1.6', 'Cont', 'Seedsplease', 'SeedspleaseTranslation', 'Odyssey', 'HSK', 'HSK_1.6']

def has_cjk(s):
    return bool(re.search(r'[\u4e00-\u9fff]', s or ''))

def find_lang_dirs():
    zh, en = [], []
    for a in ACTIVE:
        base = os.path.join(ROOT, a, 'Languages')
        if not os.path.isdir(base):
            continue
        for lang in sorted(os.listdir(base)):
            p = os.path.join(base, lang)
            if not os.path.isdir(p):
                continue
            low = lang.lower()
            if low.startswith('chinese'):
                zh.append((a, lang, p))
            elif low.startswith('english'):
                en.append((a, lang, p))
    return zh, en

def collect(lang_dirs):
    """返回 keys: {(folder,defType,defName,field): (value, srcfile)}; keyed: {(folder,key): (value,file)}"""
    inj, keyed = {}, {}
    for folder, lang, base in lang_dirs:
        for f in sorted(glob.glob(os.path.join(base, '**', '*.xml'), recursive=True)):
            try:
                root = etree.parse(f).getroot()
            except Exception as e:
                print('PARSE FAIL', f, e)
                continue
            rel = os.path.relpath(f, base).replace('\\', '/')
            if rel.startswith('Keyed/'):
                for c in root:
                    if isinstance(c.tag, str):
                        keyed[(folder, c.tag)] = ((c.text or '').strip(), f)
                continue
            # DefInjected/<DefType>/<file>.xml
            parts = rel.split('/')
            deftype = parts[1] if len(parts) >= 3 else '?'
            for c in root:
                if not isinstance(c.tag, str):
                    continue
                seg = c.tag.split('.')
                dname = seg[0]
                field = '.'.join(seg[1:]) or 'label'
                val = (c.text or '').strip()
                if not val and len(c) == 0:
                    continue
                inj[(folder, deftype, dname, field)] = (val, f)
    return inj, keyed

zh_dirs, en_dirs = find_lang_dirs()
zh_inj, zh_keyed = collect(zh_dirs)
en_inj, en_keyed = collect(en_dirs)

print('=' * 72)
print('中文层目录:', [d[1] for d in zh_dirs])
print('英文层目录:', [d[1] for d in en_dirs])
print('ZH DefInjected keys: %d | EN DefInjected keys: %d' % (len(zh_inj), len(en_inj)))
print('ZH Keyed: %d | EN Keyed: %d' % (len(zh_keyed), len(en_keyed)))
print('=' * 72)

# ---- A: 中文层值仍纯英文 ----
A = [(k, v) for k, v in sorted(zh_inj.items()) if not has_cjk(v[0]) and k[3] in ('label', 'description')]
# ---- B: 破坏检测 ----
B = [(k, v) for k, v in sorted(zh_inj.items())
     if has_cjk(v[0]) and re.search(r'[A-Za-z][\u4e00-\u9fff][A-Za-z]', v[0])]
# ---- C: 英文有中文缺 ----
C = [(k, en_inj[k]) for k in sorted(set(en_inj) - set(zh_inj))]
# ---- E: Keyed ----
E_missing = [(k, en_keyed[k]) for k in sorted(set(en_keyed) - set(zh_keyed))]
E_untrans = [(k, v) for k, v in sorted(zh_keyed.items()) if not has_cjk(v[0])]

# ---- D: Defs/Patches 内联英文 ----
inline = {}
def add(deftype, dname, field, val, src):
    inline.setdefault((deftype, dname, field), (val, src))

for a in ACTIVE:
    for f in sorted(glob.glob(os.path.join(ROOT, a, 'Defs', '**', '*.xml'), recursive=True)):
        try:
            root = etree.parse(f).getroot()
        except Exception:
            continue
        for node in root:
            if not isinstance(node.tag, str):
                continue
            dn = node.findtext('defName')
            if not dn:
                continue
            for fld in node.iter():
                if not isinstance(fld.tag, str) or fld.tag not in ('label', 'description'):
                    continue
                v = (fld.text or '').strip()
                if not v:
                    continue
                path, p = [], fld
                while p is not None and p is not node:
                    par = p.getparent()
                    if par is None:
                        break
                    sibs = [c for c in par if isinstance(c.tag, str)]
                    path.append(str(sibs.index(p)) if p.tag == 'li' else p.tag)
                    p = par
                path.reverse()
                add(node.tag, dn, '.'.join(path), v, f)
    # Patches: 任何带 defName 的元素下的 label/description
    for f in sorted(glob.glob(os.path.join(ROOT, a, 'Patches', '**', '*.xml'), recursive=True)):
        try:
            root = etree.parse(f).getroot()
        except Exception:
            continue
        for el in root.iter():
            if not isinstance(el.tag, str):
                continue
            dn = el.findtext('defName')
            if not dn:
                continue
            for fld in el.iter('label', 'description'):
                v = (fld.text or '').strip()
                if v:
                    add('__patch__', dn, fld.tag, v, f)

zh_lookup = {}
for (folder, dt, dn, fld), (v, f) in zh_inj.items():
    zh_lookup.setdefault((dt, dn, fld), []).append((v, folder))
    zh_lookup.setdefault(('ANY', dn, fld), []).append((v, folder))

D = []
for (dt, dn, fld), (v, src) in sorted(inline.items()):
    if has_cjk(v):
        continue
    got = zh_lookup.get((dt, dn, fld)) or zh_lookup.get(('ANY', dn, fld))
    if not got or all(not has_cjk(g[0]) for g in got):
        D.append(((dt, dn, fld), v, src))

print('【A】中文层条目值仍为纯英文(未翻译): %d' % len(A))
print('     label %d | description %d' % (
    sum(1 for k, v in A if k[3] == 'label'), sum(1 for k, v in A if k[3] == 'description')))
print('【B】中文夹英文单词内部(词表替换破坏): %d' % len(B))
for k, v in B[:15]:
    print('     %-45s %s' % ('.'.join(k[1:]), v[0][:60]))
print('【C】英文有、中文缺失的 key: %d' % len(C))
bytype = {}
for k, v in C:
    bytype.setdefault((k[0], k[1]), []).append(k)
for t in sorted(bytype):
    print('     %-14s %-40s %d' % (t[0], t[1], len(bytype[t])))
print('【D】Defs/Patches 内联英文且中文无覆盖: %d' % len(D))
bt2 = {}
for (dt, dn, fld), v, src in D:
    bt2.setdefault(dt, set()).add(dn)
for dt in sorted(bt2):
    print('     %-40s %d defs' % (dt, len(bt2[dt])))
print('【E】Keyed 中文缺失: %d | 中文 Keyed 仍英文: %d' % (len(E_missing), len(E_untrans)))
for k, v in E_missing[:20]:
    print('     ', k, '=>', v[0][:60])

json.dump(
    {'A': [[list(k), v[0], os.path.basename(v[1])] for k, v in A],
     'B': [[list(k), v[0]] for k, v in B],
     'C': [[list(k), v[0], os.path.basename(v[1])] for (k, v) in C],
     'D': [[list(dt), v, os.path.relpath(src, ROOT).replace('\\', '/')] for dt, v, src in D],
     'E_missing': [[list(k), v[0]] for k, v in E_missing],
     'E_untranslated': [[list(k), v[0]] for k, v in E_untrans],
     'en_inj': {'|'.join(k): v[0] for k, v in en_inj.items()},
     'zh_inj': {'|'.join(k): v[0] for k, v in zh_inj.items()},
     },
    open(os.path.join(ROOT, '_tmp/audit_lang.json'), 'w', encoding='utf-8'),
    ensure_ascii=False, indent=1)
print('\n明细 -> _tmp/audit_lang.json')
