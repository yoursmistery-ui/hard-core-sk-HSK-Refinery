# -*- coding: utf-8 -*-
"""规范化中文层目录名: 复数类型目录合并进单数类型目录(FactionDefs->FactionDef 等),
避免 RimWorld 按目录名解析 def 类型时静默丢翻译或重复注入。"""
import os, re, glob, shutil
from lxml import etree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
DST = os.path.join(ROOT, 'HSK', 'Languages', 'ChineseSimplified (简体中文)', 'DefInjected')
MAP = {'FactionDefs': 'FactionDef', 'GeneDefs': 'GeneDef', 'MemeDefs': 'MemeDef'}

def read_entries(path):
    root = etree.parse(path).getroot()
    out = []
    for c in root:
        if isinstance(c.tag, str):
            out.append((c.tag, (c.text or '').strip()))
    return out

moved = 0
for plural, singular in MAP.items():
    src = os.path.join(DST, plural)
    if not os.path.isdir(src):
        continue
    for f in sorted(glob.glob(os.path.join(src, '*.xml'))):
        base = os.path.basename(f)
        tgt = os.path.join(DST, singular, base)
        os.makedirs(os.path.dirname(tgt), exist_ok=True)
        entries = read_entries(f)
        have = read_entries(tgt) if os.path.isfile(tgt) else []
        seen = {t for t, _ in have}
        add = [(t, v) for t, v in entries if t not in seen]
        merged = have + add
        lines = ['<?xml version="1.0" encoding="utf-8"?>', '<LanguageData>', '']
        for t, v in merged:
            v = v.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
            lines.append('\t<%s>%s</%s>' % (t, v, t))
        lines += ['', '</LanguageData>', '']
        open(tgt, 'w', encoding='utf-8').write('\n'.join(lines))
        moved += len(add)
        print('  %s/%s -> %s/%s (+%d, 合计 %d)' % (plural, base, singular, base, len(add), len(merged)))
    trash = os.path.join(ROOT, '_tmp', '合并掉的复数目录')
    os.makedirs(trash, exist_ok=True)
    shutil.move(src, os.path.join(trash, plural))
print('合并写入 %d 条, 复数目录已移入 %s' % (moved, '_tmp/合并掉的复数目录'))
print('\n现存中文层目录:')
print(' ', sorted(os.listdir(DST)))
