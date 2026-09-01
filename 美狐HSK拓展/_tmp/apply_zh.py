# -*- coding: utf-8 -*-
"""把各批次译文合并写回 HSK/Languages/ChineseSimplified (简体中文)/。
- 先备份并清空旧中文 DefInjected(全部重做)
- 保留 HSK_1.6 里已有的高质量中文(加载更靠后, 会覆盖同名键)
- 文件布局镜像英文层; 内联无英文层的键写入 DefInjected/<类型>/ZH_Inline_<来源>.xml
"""
import os, re, sys, json, glob, shutil, datetime
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
BATCH = os.path.join(ROOT, '_tmp', 'batches')
DST = os.path.join(ROOT, 'HSK', 'Languages', 'ChineseSimplified (简体中文)')
BAK = os.path.join(ROOT, '_tmp', '旧中文备份_%s' % datetime.datetime.now().strftime('%H%M%S'))
PREVIEW = '--preview' in sys.argv
if PREVIEW:
    DST = os.path.join(ROOT, '_tmp', 'preview_zh')

tab = json.load(open(os.path.join(ROOT, '_tmp/en_table.json'), encoding='utf-8'))
# 目录名规范化: 英文层有复数类型目录(FactionDefs/GeneDefs/MemeDefs), 写中文时用规范单数
ALIAS = {'FactionDefs': 'FactionDef', 'GeneDefs': 'GeneDef', 'MemeDefs': 'MemeDef'}
RANK = {'1.6': 0, 'Cont': 1, 'Seedsplease': 2, 'SeedspleaseTranslation': 3,
        'Odyssey': 4, 'HSK': 5, 'HSK_1.6': 6}

# HSK_1.6 里已有手写中文的键不再重复写入(加载更靠后, 会覆盖)
skip = set()
h16 = os.path.join(ROOT, 'HSK_1.6', 'Languages', 'ChineseSimplified (简体中文)')
if os.path.isdir(h16):
    for f in glob.glob(os.path.join(h16, '**', '*.xml'), recursive=True):
        rel = os.path.relpath(f, h16).replace('\\', '/')
        try:
            r = etree.parse(f).getroot()
        except Exception:
            continue
        for c in r:
            if not isinstance(c.tag, str): continue
            if rel.startswith('Keyed/'):
                skip.add('|'.join(['KEYED', c.tag, '']))
            else:
                dt = ALIAS.get(rel.split('/')[1], rel.split('/')[1])
                seg = c.tag.split('.')
                skip.add('|'.join([dt, seg[0], '.'.join(seg[1:]) or 'label']))
print('HSK_1.6 已有中文, 跳过 %d 键' % len(skip))

# 规范化表: 同一名义键取加载优先级最高的来源
ntab = {}
for k, info in tab.items():
    dt, dn, fld = k.split('|')
    nk = '|'.join([ALIAS.get(dt, dt), dn, fld])
    if nk in ntab and RANK.get(ntab[nk]['src'], -1) >= RANK.get(info['src'], -1):
        continue
    ntab[nk] = info
tab = ntab
print('规范化后键数: %d' % len(tab))

# ---- 1. 收集译文 ----
id2zh, maps = {}, {}
for f in sorted(glob.glob(os.path.join(BATCH, 'out_*.json'))):
    bid = os.path.basename(f)[4:-5]
    try:
        d = json.load(open(f, encoding='utf-8'))
    except Exception as e:
        print('!! 无法解析', f, e); continue
    id2zh.update({'%s|%s' % (bid, k): v for k, v in d.items()})
    mf = os.path.join(BATCH, 'map_%s.json' % bid)
    if os.path.isfile(mf):
        maps.update(json.load(open(mf, encoding='utf-8')))

# id -> zh  (id 形如 b01_0001, 上面加了批次前缀防冲突; map 里键是原始 id)
zh_by_id = {}
for k, v in id2zh.items():
    zh_by_id[k.split('|', 1)[1]] = v

# 兼容: 有些代理可能直接用 "bNN_XXXX" 写入, map 的键也是 "bNN_XXXX"
missing_ids = []
raw2zh = {}
for sid, keys in maps.items():
    zh = zh_by_id.get(sid)
    if zh is None:
        missing_ids.append(sid); continue
    zh = str(zh).strip()
    for kk in keys:
        raw2zh[kk] = zh

# 规范化键并解决冲突: 同一规范键若有多份译文, 取与"生效英文"一致的那份
cand = {}
for rk, zh in raw2zh.items():
    dt, dn, fld = rk.split('|')
    nk = '|'.join([ALIAS.get(dt, dt), dn, fld])
    if nk in skip:
        continue
    cand.setdefault(nk, []).append((rk, zh))

key2zh = {}
for nk, lst in cand.items():
    win = tab.get(nk, {}).get('en')
    pick = None
    for rk, zh in lst:
        if tab.get(rk, {}).get('en') == win:
            pick = zh
            break
    key2zh[nk] = pick if pick is not None else lst[0][1]
print('批次文件 %d 个, 唯一串 %d, 缺译文 %d' %
      (len(glob.glob(os.path.join(BATCH, 'out_*.json'))), len(maps), len(missing_ids)))
if missing_ids:
    print('  缺失 id 示例:', missing_ids[:10])

covered = set(key2zh)
allkeys = set(tab) - skip
notcov = sorted(allkeys - covered)
print('键覆盖: %d / %d' % (len(allkeys) - len(notcov), len(allkeys)))
if notcov:
    for k in notcov[:15]:
        print('  未覆盖:', k, '=>', tab[k]['en'][:60])
    if '--force' not in sys.argv:
        print('\n存在未覆盖键, 中止写入(用 --force 强制写入已有部分)')
        sys.exit(1)

# ---- 2. 备份旧中文 ----
if os.path.isdir(DST):
    os.makedirs(BAK, exist_ok=True)
    shutil.copytree(DST, os.path.join(BAK, 'HSK_ChineseSimplified'), dirs_exist_ok=True)
    for sub in ('DefInjected', 'Keyed'):
        p = os.path.join(DST, sub)
        if os.path.isdir(p):
            shutil.rmtree(p)
    print('旧中文层已备份 ->', BAK)
os.makedirs(os.path.join(DST, 'DefInjected'), exist_ok=True)
os.makedirs(os.path.join(DST, 'Keyed'), exist_ok=True)

def esc(s):
    return (s.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;'))

# ---- 3. 按目标文件分组 ----
files = {}      # reldst -> [(tag, value), ...]
for k, zh in key2zh.items():
    dt, dn, fld = k.split('|')
    info = tab[k]
    if dt == 'KEYED':
        reldst = 'Keyed/%s' % os.path.basename(info['file'])
        tag = dn
    else:
        if info['file'].startswith('Inline_'):
            src = info['file'][len('Inline_'):-len('.xml')]
            reldst = 'DefInjected/%s/ZH_Inline_%s.xml' % (dt, src)
        else:
            reldst = 'DefInjected/%s/%s' % (dt, os.path.basename(info['file']))
        tag = '%s.%s' % (dn, fld)
    files.setdefault(reldst, []).append((tag, zh, info['en']))

# 同一目标文件里同 tag 冲突检测(取第一个并报告)
conflicts = []
written = 0
for reldst, items in sorted(files.items()):
    seen, rows = {}, []
    for tag, zh, en in items:
        if tag in seen:
            if seen[tag] != zh:
                conflicts.append((reldst, tag, seen[tag], zh))
            continue
        seen[tag] = zh
        rows.append((tag, zh))
    path = os.path.join(DST, *reldst.split('/'))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    lines = ['<?xml version="1.0" encoding="utf-8"?>', '<LanguageData>', '']
    for tag, zh in sorted(rows):
        lines.append('\t<%s>%s</%s>' % (tag, esc(zh), tag))
    lines += ['', '</LanguageData>', '']
    open(path, 'w', encoding='utf-8').write('\n'.join(lines))
    written += len(rows)
print('写出文件 %d 个, 条目 %d 条' % (len(files), written))
if conflicts:
    print('同键冲突 %d 处(已保留首个):' % len(conflicts))
    for c in conflicts[:10]:
        print('   ', c[0], c[1], '|', c[2][:40], '<>', c[3][:40])
