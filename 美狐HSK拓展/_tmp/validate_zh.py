# -*- coding: utf-8 -*-
"""汉化终检: 覆盖率 / 占位符 / 换行 / XML 合法性 / 重复键 / 残留英文 / 旧词表拼接痕迹。"""
import os, re, sys, json, glob
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ZH_BASES = [os.path.join(ROOT, 'HSK', 'Languages', 'ChineseSimplified (简体中文)'),
            os.path.join(ROOT, 'HSK_1.6', 'Languages', 'ChineseSimplified (简体中文)')]
tab = json.load(open(os.path.join(ROOT, '_tmp/en_table.json'), encoding='utf-8'))
# 英文层有些目录名是复数(FactionDefs/GeneDefs/MemeDefs), 中文层已规范为单数; 比对时归一
ALIAS = {'FactionDefs': 'FactionDef', 'GeneDefs': 'GeneDef', 'MemeDefs': 'MemeDef'}
def norm(dt): return ALIAS.get(dt, dt)
tab = {'|'.join([norm(p.split('|')[0])] + p.split('|')[1:]): v for p, v in tab.items()}

def tokens(s):
    return sorted(re.findall(r'\{[^{}]*\}|\[[^\[\]]*\]', s or ''))

def br_count(s):
    return (s or r'').count(r'\n') + (s or '').count('\n')

# ---- 读中文层 ----
zh, dup, badxml = {}, [], []
for base in ZH_BASES:
    if not os.path.isdir(base): continue
    for f in glob.glob(os.path.join(base, '**', '*.xml'), recursive=True):
        raw = open(f, encoding='utf-8-sig').read()
        try:
            root = etree.fromstring(raw.encode('utf-8'))
        except Exception as e:
            badxml.append((os.path.relpath(f, ROOT), str(e))); continue
        rel = os.path.relpath(f, base).replace('\\', '/')
        for c in root:
            if not isinstance(c.tag, str): continue
            v = (c.text or '').strip()
            if rel.startswith('Keyed/'):
                k = '|'.join(['KEYED', c.tag, ''])
            else:
                dt = rel.split('/')[1]
                seg = c.tag.split('.')
                k = '|'.join([dt, seg[0], '.'.join(seg[1:]) or 'label'])
            if k in zh and zh[k][0] != v:
                dup.append((k, zh[k][1], v, os.path.relpath(f, ROOT)))
            zh[k] = (v, os.path.relpath(f, ROOT))

print('=' * 68)
print('中文层条目: %d (含 HSK_1.6)' % len(zh))
print('=' * 68)
if badxml:
    print('!! XML 解析失败 %d 个:' % len(badxml))
    for f, e in badxml: print('   ', f, e[:120])

# ---- 1. 覆盖率 ----
miss, nocjk, ph, br, resid, glue = [], [], [], [], [], []
ALLOW_EN = re.compile(r'^[\s\dA-Za-z\.\-\+/&amp;:;\(\)\[\]\{\}%,!\'"“”·、x×øø\*_]*$')
for k, info in tab.items():
    en, v = info['en'], zh.get(k)
    val = v[0] if v else ''
    if not v:
        miss.append(k); continue
    if not re.search(r'[一-鿿]', val):
        # 允许: 纯代号 label / 命名词库照抄条目
        if not (ALLOW_EN.match(val) and (len(val) < 28 or '->' in val)):
            nocjk.append((k, en, val)); continue
    if tokens(en) != tokens(val):
        ph.append((k, en, val))
    if br_count(en) != br_count(val):
        br.append((k, en, val))
    # 残留整句英文(5 个以上连续英文词)
    if re.search(r'([A-Za-z]{3,}\s+){5,}[A-Za-z]{3,}', val.replace(r'\n', ' ')):
        resid.append((k, val))
    # 旧词表拼接痕迹: 中文夹在英文单词中间
    if re.search(r'[A-Za-z][一-鿿][A-Za-z]', val):
        glue.append((k, val))
    # 双重转义(parsed 值里再出现 &amp; 说明源文本被二次转义)
    if '&amp;' in val or '\r' in val:
        resid.append((k, '[转义/空白异常] ' + val))

def rpt(title, lst, n=12):
    print('\n【%s】%d 条' % (title, len(lst)))
    for x in lst[:n]:
        if isinstance(x, str):
            print('   ', x)
        elif len(x) == 3:
            print('   %s\n      EN: %s\n      ZH: %s' % (x[0], x[1][:90], x[2][:90]))
        else:
            print('   ', x[0], '|', str(x[1])[:90])

rpt('缺中文', miss, 20)
if miss:
    for k in miss[:20]: print('   ', k, '=>', tab[k]['en'][:70])
rpt('无中文且非代号', nocjk)
rpt('占位符不一致', ph)
rpt('换行 \\n 数量不一致', br)
rpt('残留整句英文', [(k, v) for k, v in resid])
rpt('疑似词表拼接(中文夹英文词内)', [(k, v) for k, v in glue])

if dup:
    print('\n【重复键(值不同)】%d 处' % len(dup))
    for k, a, b, f in dup[:10]:
        print('   ', k, '\n      旧:', a[:60], '\n      新:', b[:60], '\n      文件:', f)

# ---- 孤儿键: 中文有但英文表没有 ----
orph = [k for k in zh if k not in tab]
print('\n【中文层孤儿键(英文表无此键, 可能无效/多余)】%d 条' % len(orph))
for k in orph[:20]: print('   ', k, '=>', zh[k][0][:50])

total = len(tab)
ok = total - len(miss) - len(nocjk) - len(ph) - len(br)
print('\n' + '=' * 68)
print('总键 %d | 有中文 %d | 合格 %d (%.1f%%)' % (total, total - len(miss), ok, 100.0 * ok / max(total, 1)))
print('=' * 68)
sys.exit(0 if not (badxml or miss or nocjk or ph or br) else 2)
