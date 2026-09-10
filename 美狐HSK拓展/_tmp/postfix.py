# -*- coding: utf-8 -*-
"""译后统一修正: 冒号样式、残留英文弹名、个别语义修正、全角括号统一。"""
import os, re, json, glob, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
BATCH = os.path.join(ROOT, '_tmp', 'batches')

# 整串级替换(大小写不敏感)
SUBS = [
    (r'口径\s*:', '口径：'),
    (r'rpg-7\s*grenade', 'RPG-7火箭弹'),
    (r'plasma\s*cell', '等离子电池'),
    (r'mechanite\s*psyshot', '机械素灵能弹'),
    (r'(?<![\d.])30x29mm\s*grenade', '30x29mm榴弹'),
    (r'(?<![\d.])9x19mm\s*para\b', '9x19mm Para'),
    (r'\.12\s*gauge\b', '.12 Gauge'),
    (r'(?<![\d.])12\s*gauge\b', '12 Gauge'),
    (r'(?<![\d.])\bmedium-?mat\b', '中型反机甲导弹塔'),
    (r'中型\s*MAT\b', '中型反机甲导弹塔'),
    # 弹种代号被误展开成化学名词 -> 还原为代号(HSK/CE 官方中文保留代号)
    (r'x92mmSR\s*聚氨酯黏合剂', 'x92mmSR TuF'),
    (r'聚氨酯黏合剂(?=\s*[（(])', 'TuF'),
    # 有坂/南部/法国制式: 采用 HSK 官方中文弹名用字
    (r'\s*Arisaka\b', ' 有坂'),
    (r'\s*Nambu\b', ' 南部'),
    (r'\s*French\b', ' 法国制式'),
    (r'\s*British\b', ' 英制'),
    # 专名统一
    (r'\bProdromos\b', '普罗多莫斯'),
    (r'Go-?\s*juice', '冲刺剂'),                 # 环世界官方中文: GoJuice = 冲刺剂
    (r'中型\s*MAT', '中型反机甲导弹塔'),
    (r'美狐猎人', '美狐猎手'),
    (r'药丸（猎人）', '药丸（猎手）'),
    (r'高科技部件', '高科技零部件'),
    # 异种人名统一
    (r'普罗德罗斯', '普罗多莫斯'),
    (r'阿尔提科斯', '阿尔克托斯'),
    (r'波尔涅', '波妮'),
    # 术语替换残留的中文之间空格
    (r'(?<=[一-鿿])[ \t]+(?=[一-鿿])', ''),
]
# 单条修正: (批次, id) -> 新全文 (留空表示不做)
FIX = {}

changed = 0
for f in sorted(glob.glob(os.path.join(BATCH, 'out_*.json'))):
    d = json.load(open(f, encoding='utf-8'))
    n = 0
    for sid, z in list(d.items()):
        orig = z
        for pat, rep in SUBS:
            z = re.sub(pat, rep, z, flags=re.I)
        z = re.sub(r'：\s*', '：', z) if False else z
        if sid in FIX:
            z = FIX[sid]
        if z != orig:
            d[sid] = z
            n += 1
    if n:
        json.dump(d, open(f, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
        print('%s 修正 %d 条' % (os.path.basename(f), n))
    changed += n
print('合计修正 %d 条' % changed)

# 复查残留
print('\n--- 仍含连续英文词(≥3)的条目 ---')
bad = 0
for f in sorted(glob.glob(os.path.join(BATCH, 'out_*.json'))):
    d = json.load(open(f, encoding='utf-8'))
    for sid, z in sorted(d.items()):
        if re.search(r'\b([a-z]{3,}\s+){2,}[a-z]{3,}\b', z.replace(r'\n', ' ')):
            print('  %s  %s' % (sid, z[:100]))
            bad += 1
print('共 %d 条' % bad)
