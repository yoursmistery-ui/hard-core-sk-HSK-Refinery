# -*- coding: utf-8 -*-
"""术语一致性检查: 英文含某术语的条目, 中文必须含对应的既定译法。"""
import os, re, json, glob, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
tab = json.load(open(os.path.join(ROOT, '_tmp/en_table.json'), encoding='utf-8'))
maps, zh_by_id = {}, {}
for f in glob.glob(os.path.join(ROOT, '_tmp/batches', 'map_*.json')):
    maps.update(json.load(open(f, encoding='utf-8')))
for f in glob.glob(os.path.join(ROOT, '_tmp/batches', 'out_*.json')):
    zh_by_id.update(json.load(open(f, encoding='utf-8')))

TERM = {
    r'\bmiho\b': '美狐', r'foxfire': '狐火', r'celestial': '天界', r'\beltex\b': '灵素',
    r'mechanoid': '机械体', r'mechanite': '机械素', r'persona core': '人格核心',
    r'archotech': '超凡科技', r'\bplasma\b': '等离子', r'thermobaric': '温压',
    r'\bincendiary\b': '燃烧', r'\bsabot\b': '脱壳穿甲', r'\bemp\b': '电磁脉冲',
    r'full metal jacket|\bfmj\b': '全金属被甲', r'hollow point|\bhp\b': '空尖',
    r'high explosive|\bhe\b': '高爆', r'armor piercing|\bap\b': '穿甲',
    r'submachine|\bsmg\b': '冲锋枪', r'shotgun': '霰弹枪', r'sniper': '狙击',
    r'infantry gun': '步兵炮', r'recoilless': '无后坐力', r'tatami': '榻榻米',
    r'ceramic': '陶瓷', r'\bkevlar\b': '凯夫拉', r'synthread': '超织纤维',
    r'devilstrand': '魔鬼布', r'plasteel': '塑钢', r'titanium': '钛合金',
    r'\bhoodie\b': '连帽衫', r'\bdress\b': '连衣裙', r'\bshirt\b': '衬衫',
    r'\bhelmet\b': '头盔', r'\bbeanie\b': '毛线帽', r'gas mask': '防毒面具',
    r'\bsorceress\b': '女巫', r'\bshamaness\b': '巫女', r'temptress': '妖女',
    r'huntress|\bhunter\b': '猎手', r'grenade': '榴弹', r'missile': '导弹',
    r'\bturret\b': '炮塔', r'\bmagazine\b': '弹匣', r'reload': '装填',
    r'\bresearch\b': '研究', r'\bcomponent': '零部件', r'weapon parts': '武器配件',
    r'underbarrel': '下挂', r'\bbulk\b': '体积', r'concealment': '隐蔽',
    r'ebony': '乌木', r'mulberry': '桑', r'\bpersona\b': '人格',
}
# 专名类: 只在 label 类短文本里要求一致
stats = {k: [0, 0, []] for k in TERM}   # [命中数, 合规数, 违规样本]
checked = 0
for sid, keys in maps.items():
    zh = zh_by_id.get(sid)
    if not zh: continue
    info = tab[keys[0]]
    en = info['en']
    checked += 1
    low = en.lower()
    for pat, want in TERM.items():
        if not re.search(pat, low): continue
        stats[pat][0] += 1
        if want in zh:
            stats[pat][1] += 1
        else:
            stats[pat][2].append((sid, en[:60], zh[:60]))

print('已检查唯一串: %d' % checked)
bad = 0
for pat, (n, ok, samples) in sorted(stats.items(), key=lambda x: -x[1][0]):
    if n == 0: continue
    flag = '' if ok == n else '   <-- %d 处待核' % (n - ok)
    print('  %-34s %-8s 命中 %3d 合规 %3d%s' % (pat[:34], TERM[pat], n, ok, flag))
    if ok != n:
        bad += n - ok
print('\n术语不一致合计 %d 处' % bad)
if '--show' in sys.argv:
    for pat, (n, ok, samples) in stats.items():
        for s in samples[:6]:
            print('  [%s->%s] %s\n      EN %s\n      ZH %s' % (pat, TERM[pat], s[0], s[1], s[2]))
