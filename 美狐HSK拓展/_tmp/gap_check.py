# -*- coding: utf-8 -*-
"""终检补漏: 扫描仍然存在的 Defs/Patches, 找出"英文可见文本但中文层没有键"的字段。
覆盖 DefInjected 常见可译字段 + RulePack 内联 rulesStrings。"""
import glob, os, re, json
from lxml import etree

ROOT = os.path.abspath(os.path.dirname(os.path.join(__file__, '..')) + '/.')
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ACTIVE = ['1.6', 'Cont', 'Seedsplease', 'SeedspleaseTranslation', 'Odyssey', 'HSK', 'HSK_1.6']
ALIAS = {'FactionDefs': 'FactionDef', 'GeneDefs': 'GeneDef', 'MemeDefs': 'MemeDef'}

FIELDS = ('label', 'description', 'labelShort', 'labelNoun', 'gerund', 'verb', 'nameFlowTitle',
          'reportString', 'jobString', 'deathMessage', 'letterLabel', 'letterText',
          'ingestCommandString', 'ingestReportString', 'pawnSingular', 'pawnsPlural',
          'title', 'titleShort', 'descriptionHyperlinks', 'officialName', 'setButton',
          'uiCategory', 'tabTitle', 'labelGetter', 'askerCantAnswerResponse')

def has_cjk(s): return bool(re.search(r'[一-鿿]', s or ''))
def looks_text(s):
    s = (s or '').strip()
    return len(s) > 1 and bool(re.search(r'[A-Za-z\u4e00-\u9fff]', s))

zh = {}
for base in ['HSK/Languages/ChineseSimplified (简体中文)',
             'HSK_1.6/Languages/ChineseSimplified (简体中文)']:
    b = os.path.join(ROOT, base)
    if not os.path.isdir(b): continue
    for f in glob.glob(os.path.join(b, '**', '*.xml'), recursive=True):
        rel = os.path.relpath(f, b).replace(os.sep, '/')
        parts = rel.split('/')
        try: r = etree.parse(f).getroot()
        except Exception: continue
        for c in r:
            if not isinstance(c.tag, str): continue
            if parts[0] == 'Keyed':
                zh.setdefault(('KEYED', c.tag, ''), []).append((c.text or '').strip())
            else:
                dt = ALIAS.get(parts[1], parts[1]); seg = c.tag.split('.')
                zh.setdefault((dt, seg[0], '.'.join(seg[1:]) or 'label'), []).append((c.text or '').strip())
zh_any = {}
for (dt, dn, fld), vs in zh.items():
    zh_any.setdefault((dn, fld), []).extend(vs)

problems = []
for a in ACTIVE:
    for sub in ('Defs', 'Patches'):
        for f in glob.glob(os.path.join(ROOT, a, sub, '**', '*.xml'), recursive=True):
            raw = re.sub(r'<!--.*?-->', '', open(f, encoding='utf-8-sig', errors='ignore').read(), flags=re.S)
            try: root = etree.fromstring(raw.encode('utf-8'))
            except Exception: continue
            for node in root:
                if not isinstance(node.tag, str): continue
                dn = node.findtext('defName')
                if not dn: continue
                # 普通字段
                for fld in node.iter():
                    if not isinstance(fld.tag, str) or fld.tag not in FIELDS: continue
                    v = (fld.text or '').strip()
                    if not looks_text(v) or has_cjk(v): continue
                    # 构造 DefInjected 点分路径(li 用索引), 与写入端一致
                    path, p = [], fld
                    while p is not None and p is not node:
                        par = p.getparent()
                        if par is None: break
                        sibs = [c for c in par if isinstance(c.tag, str)]
                        path.append(str(sibs.index(p)) if p.tag == 'li' else p.tag)
                        p = par
                    path.reverse()
                    fkey = '.'.join(path)
                    if (ALIAS.get(node.tag, node.tag), dn, fkey) in zh or (dn, fkey) in zh_any:
                        continue
                    if fkey != fld.tag and (ALIAS.get(node.tag, node.tag), dn, fld.tag) in zh:
                        continue
                    problems.append((a, node.tag, dn, fkey, v[:70], os.path.relpath(f, ROOT)))
                # RulePack 内联 rulesStrings(<rules> 下的 li, 形如 name->value)
                rules = node.find('rulePack')
                if rules is not None:
                    rl = rules.find('rules')
                    if rl is not None:
                        for i, li in enumerate([c for c in rl if isinstance(c.tag, str)]):
                            v = (li.text or '').strip()
                            if not v or has_cjk(v): continue
                            key = (ALIAS.get(node.tag, node.tag), dn, 'rulePack.rulesStrings.%d' % i)
                            if key in zh or (dn, 'rulePack.rulesStrings.%d' % i) in zh_any: continue
                            problems.append((a, node.tag, dn, 'rulesStrings[%d]' % i, v[:70],
                                             os.path.relpath(f, ROOT)))

print('疑似遗漏(中文层无对应键): %d 条' % len(problems))
seen = set()
for p in problems:
    sig = (p[1], p[4][:24])
    if sig in seen: continue
    seen.add(sig)
    print('  [%s] %-28s %-40s %s' % (p[0], p[1], p[3], p[2]))
    print('        EN: %s' % p[4])
    print('        文件: %s' % p[5])
json.dump([list(p) for p in problems], open(os.path.join(ROOT, '_tmp/gap_report.json'), 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
