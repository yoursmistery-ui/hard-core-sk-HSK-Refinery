# -*- coding: utf-8 -*-
"""字段普查: 找出 Defs/Patches 里所有"像人话"的英文字符串字段, 判断是否需要汉化。"""
import os, re, glob, json
from lxml import etree
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ORDER = ['1.6', 'Cont', 'Seedsplease', 'SeedspleaseTranslation', 'Odyssey', 'HSK', 'HSK_1.6']
def has_cjk(s): return bool(re.search(r'[一-鿿]', s or ''))
def is_prose(s):
    s = (s or '').strip()
    if len(s) < 3: return False
    if not re.search(r'[A-Za-z]', s): return False
    if re.fullmatch(r'[\d_.\-/xX%°]+', s): return False
    if re.search(r'^(Base|Stat|Def|Thing|Texture|Sound|ShaderVector|graphicPath|tex|Miho_\w+$)', s): return False
    return bool(re.search(r'[a-z]{3,}', s))
cnt = {}
samples = {}
for folder in ORDER:
    for sub in ('Defs', 'Patches'):
        for f in glob.glob(os.path.join(ROOT, folder, sub, '**', '*.xml'), recursive=True):
            try: root = etree.parse(f).getroot()
            except Exception: continue
            for el in root.iter():
                if not isinstance(el.tag, str): continue
                v = (el.text or '')
                if not is_prose(v) or has_cjk(v): continue
                # 跳过数值型/路径型
                if re.search(r'(Path|Texture|Sound|Class|Name|Def|Key|Index|Angle|Scale|Factor|Volume|Speed|Chance|Amount|Range|Count|Tick|Layer|Group|Type|ID|URL|File|Dir)$', el.tag) and ' ' not in v.strip():
                    continue
                cnt[el.tag] = cnt.get(el.tag, 0) + 1
                samples.setdefault(el.tag, []).append((folder, os.path.basename(f), v.strip()[:110]))
print('%-32s %6s' % ('字段', '次数'))
for t in sorted(cnt, key=lambda x: -cnt[x]):
    print('%-32s %6d   e.g. %s' % (t, cnt[t], samples[t][0][2]))
