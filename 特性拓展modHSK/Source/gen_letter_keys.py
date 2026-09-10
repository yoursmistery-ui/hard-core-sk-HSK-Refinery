# -*- coding: utf-8 -*-
"""特性拓展modHSK —— 授予文案 Keyed 生成器 (v3 · 动作清单法)

数据源: 表/规则-获取文案.csv  列 = 类型,键,变体,文本
  Head        成因标题(4 套/成因)  —— 必须是「动词+的+名词」结构
  Scene       成因正文(4 套/成因)  —— 「先…然后…接着…」三个连续动作, 涉及三个不同物件
  Habit       特性习惯句(≥2 套/特性, 机制二可达特性须≥4) —— "…再也改不掉了"式动作定型
  FrameHead   机制一外壳标题(4 套)
  FrameScene  机制一外壳正文(4 套/外壳)
  Exit        退场正文(≥4 套)
  Abstain     禁毒退场正文(≥4 套)
  收束句不进文案表: 代码固定拼「{PAWN_labelShort}现在是{TRAIT}。」(硬规: 不用"获得/染上")

产出: 三语 Keyed/VTE_TraitLetters.xml + Source/HSKLetterData.cs(维度→slug + 每槽变体数)
校验不过直接非零退出, 不允许半成品进 DLL。
"""
import csv, io, os, re, sys
import lxml.etree as ET

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
BASE = r"C:\Personal\Project\ratkin-patch\特性拓展modHSK"
TAB  = os.path.join(BASE, "表")
KEYDIR = os.path.join(BASE, "1.6", "Languages")

# 行为维度(与 规则-生活习惯特性.csv 的"行为维度"一致) -> Keyed 用 ASCII slug
SLUG = {
    "园艺":"gardening", "屠宰":"butchery", "看电视":"television",
    "饮酒":"alcohol", "吸烟":"smokeleaf", "药物滥用":"harddrugs",
    "制药/化学":"chemistry", "烹饪":"cooking", "研究":"research", "医疗":"medicine",
    "手工/工匠":"handcraft", "清洁/整洁":"cleaning", "建造":"construction", "岩矿":"mining",
    "近战":"melee", "闪避/肉搏":"dodging", "射击":"shooting", "击杀人类":"killing",
    "残忍手术":"cruelty", "情爱":"lovemaking", "社交闲聊":"socializing", "商贸/谈判":"trading",
    "外交":"diplomacy", "动物":"animals", "动物虐待":"animalabuse", "偷窃":"stealing",
    "餐饮讲究":"finefood", "战后创伤":"trauma", "耐痛/战场":"battlefield", "体质抗病":"immunity",
    "温度-耐热":"heatwork", "温度-耐寒":"coldwork", "进食习惯":"eatingspeed", "孤独倾向":"solitude",
    "压力崩溃":"breakdown", "唯美追求":"artistry", "作息-夜行":"nightshift", "作息-晨行":"dawnshift",
    "阅读":"reading", "音乐演奏":"music", "健身":"workout", "冥想":"meditation",
    "棋牌":"boardgames", "饮食健康":"healthymeals", "饮食放纵":"indulgence",
}
UNWIRED = {"战后创伤", "耐痛/战场", "体质抗病", "抗病", "孤独倾向", "饮食放纵"}  # TriggerType=None, 不要求 4 套
FRAME_N = 4          # 机制一外壳数
VARS = 4             # 每槽标准变体数
PH_OK = re.compile(r'\{([A-Za-z_][A-Za-z0-9_]*)(?:_[A-Za-z][A-Za-z0-9_]*)?\}')
ALLOWED_PH = {"PAWN", "TRAIT", "COUNT"}

def read(fn):
    with open(os.path.join(TAB, fn), encoding='utf-8-sig') as f:
        return list(csv.DictReader(f))

texts = read('规则-获取文案.csv')
rules = read('规则-生活习惯特性.csv')

cnt = {}   # (类型,键) -> {变体号: 文本}
for r in texts:
    t = (r['类型'] or '').strip()
    k = (r['键'] or '').strip()
    v = (r['变体'] or '').strip()
    body = (r['文本'] or '').strip()
    if not t or not k or not v or not body:
        continue
    cnt.setdefault((t, k), {})[int(v)] = body

def need(t, k, i):
    return 'HSKTrait.%s.%s.%d' % (t, k, i)

errs, store, kinds = [], {}, {}
def put(key, txt):
    if key in store and store[key] != txt:
        errs.append('同键不同文: ' + key)
    store[key] = txt

wired_traits = {}      # slug -> 该成因授予的特性(取正向;无则负向)
for r in rules:
    dim = r['行为维度'].strip()
    pos = (r.get('正向特性') or '').strip()
    neg = (r.get('负向特性') or '').strip()
    if dim not in SLUG:
        errs.append('维度未登记 slug: ' + dim); continue
    s = SLUG[dim]
    unwired = dim in UNWIRED
    got = 0
    for tr in (pos, neg):
        if tr and tr != '--':
            got += 1
            wired_traits.setdefault(s, tr.split(':')[0])
    if unwired:
        continue
    if not got:
        continue
    for i in range(1, VARS + 1):
        h = cnt.get(('Head', s), {}).get(i)
        sc = cnt.get(('Scene', s), {}).get(i)
        if not h: errs.append('缺 Head: %s 变体%d' % (s, i))
        if not sc: errs.append('缺 Scene: %s 变体%d' % (s, i))
        if h: put(need('Head', s, i), h)
        if sc: put(need('Scene', s, i), sc)

for s, d in wired_traits.items():
    for i in range(1, VARS + 1):
        t = cnt.get(('Habit', d), {}).get(i)
        if not t: errs.append('机制二可达特性缺 Habit: %s 变体%d' % (d, i))
        elif t: put(need('Habit', d, i), t)

# 机制一外壳
for n in range(1, FRAME_N + 1):
    fh = cnt.get(('FrameHead', str(n)), {}).get(1)
    if not fh: errs.append('缺 FrameHead %d' % n)
    else: put('HSKTrait.FrameHead.%d' % n, fh)
    for i in range(1, VARS + 1):
        t = cnt.get(('FrameScene', str(n)), {}).get(i)
        if not t: errs.append('缺 FrameScene: 外壳%d 变体%d' % (n, i))
        elif t: put(need('FrameScene', str(n), i), t)

for kind, lo in (('Exit', VARS), ('Abstain', VARS)):
    for i in range(1, lo + 1):
        t = cnt.get((kind, '_'), {}).get(i)
        if not t: errs.append('缺 %s 变体%d' % (kind, i))
        elif t: put('HSKTrait.%s.%d' % (kind, i), t)

# 其余机制一池可达特性: 至少 2 套习惯句
gen_cs = os.path.join(BASE, 'Source', 'HSKGeneratedData.cs')
pool = set()
if os.path.exists(gen_cs):
    s = open(gen_cs, encoding='utf-8-sig').read()
    pool = set(re.findall(r'TraitDef="([A-Za-z0-9_]+)"', s))
for t in sorted(pool - set(wired_traits.values())):
    got = cnt.get(('Habit', t), {})
    for i in (1, 2):
        if i not in got: errs.append('机制一池特性缺 Habit: %s 变体%d' % (t, i))
    for i, txt in got.items():
        put(need('Habit', t, i), txt)

# 占位符白名单 + 收束禁词
for k, v in store.items():
    for ph in PH_OK.findall(v):
        if ph.split('_')[0] not in ALLOWED_PH:
            errs.append('非法占位符 %s in %s' % (ph, k))
    for w in ('获得', '染上'):
        if w in v and k.startswith('HSKTrait.Habit'):
            errs.append('习惯句出现禁用词「%s」: %s' % (w, k))

GEN = {
 'HSKTrait.Generic.grantTitle': {'zh':'性格变了','tw':'性格變了','en':'A Trait Changed'},
 'HSKTrait.Generic.revokeTitle':{'zh':'习惯停了','tw':'習慣停了','en':'A Habit Stopped'},
 'HSKTrait.Generic.grantBody': {
   'zh':'{PAWN_labelShort}长期的生活习惯改变了{PAWN_objective}。{PAWN_labelShort}现在是{TRAIT}。',
   'tw':'{PAWN_labelShort}長期生活習慣改變了{PAWN_objective}。{PAWN_labelShort}現在是{TRAIT}。',
   'en':'Long-standing habits have changed {PAWN_objective}. {PAWN_labelShort} is now {TRAIT}.'},
 'HSKTrait.Generic.revokeBody': {
   'zh':'{PAWN_labelShort} 不再维持那种生活了。{PAWN_labelShort}不再是{TRAIT}。',
   'tw':'{PAWN_labelShort} 不再維持那種生活了。{PAWN_labelShort}現在不再是{TRAIT}。',
   'en':'{PAWN_labelShort} no longer lives that way. {PAWN_labelShort} is no longer {TRAIT}.'},
 'HSKTrait.Generic.causeTitleDefault':{'zh':'一段很长的日子','tw':'一段很長的日子','en':'A Long Stretch of Days'},
 'HSKTrait.Generic.abstainTitle':{'zh':'戒掉的那些年','tw':'戒掉的那些年','en':'The Sober Years'},
 'HSKTrait.Generic.closerGain':{'zh':'{PAWN_labelShort}现在是','tw':'{PAWN_labelShort}現在是','en':'{PAWN_labelShort} is now'},
 'HSKTrait.Generic.closerLose':{'zh':'{PAWN_labelShort}不再是','tw':'{PAWN_labelShort}現在不再是','en':'{PAWN_labelShort} is no longer'},
}
for k, v in GEN.items():
    store[k] = v['zh']

if errs:
    print('\n'.join('  - ' + e for e in errs[:80]))
    sys.exit('校验失败 %d 项(仅列前 80)' % len(errs))

def esc(s):
    return s.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

def write_lang(dirname, pick, generic_only=False):
    d = os.path.join(KEYDIR, dirname, 'Keyed'); os.makedirs(d, exist_ok=True)
    lines = ['<?xml version="1.0" encoding="utf-8"?>', '<LanguageData>',
             '  <!-- 由 Source/gen_letter_keys.py 生成, 勿手改。数据源: 表/规则-获取文案.csv -->']
    n = 0
    for k in sorted(store):
        if generic_only and not k.startswith('HSKTrait.Generic.'):
            continue
        txt = pick(k)
        if txt is None:
            continue
        lines.append('  <%s>%s</%s>' % (k, esc(txt), k)); n += 1
    lines.append('</LanguageData>')
    open(os.path.join(d, 'VTE_TraitLetters.xml'), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    print('  写出 %-22s %4d 条' % (dirname, n))

print('[Keyed]')
write_lang('ChineseSimplified', lambda k: store[k])
write_lang('ChineseTraditional', lambda k: GEN.get(k, {}).get('tw'), True)
write_lang('English', lambda k: GEN.get(k, {}).get('en'), True)

# ---- HSKLetterData.cs ----
def variant_list(t, key, cap=VARS):
    got = cnt.get((t, key), {})
    return max(got) if got else 0

L = ['// 由 Source/gen_letter_keys.py 生成,勿手改。维度→slug + 每个文案槽的变体数(供随机取用)。',
     'namespace HSKTraitExt', '{',
     '    public static class HSKLetterData', '    {',
     '        // 行为维度(与规则表"行为维度"列一致) -> Keyed slug',
     '        public static readonly string[][] CauseSlug = new string[][]', '        {']
for dim in sorted(SLUG):
    L.append('            new string[] { "%s", "%s" },' % (dim, SLUG[dim]))
L += ['        };',
      '        // 成因 -> 可用变体套数',
      '        public static readonly string[][] SceneVariants = new string[][]', '        {']
for s in sorted(set(SLUG.values())):
    n = variant_list('Scene', s)
    if n: L.append('            new string[] { "%s", "%d" },' % (s, n))
L += ['        };',
      '        // 特性 -> 可用习惯句套数',
      '        public static readonly string[][] HabitVariants = new string[][]', '        {']
for t in sorted({d for d in wired_traits.values()} | pool):
    n = variant_list('Habit', t)
    if n: L.append('            new string[] { "%s", "%d" },' % (t, n))
L += ['        };',
      '        public const int FrameCount = %d;' % FRAME_N,
      '        public static readonly string[] FrameHeads = new string[]', '        {']
for n in range(1, FRAME_N + 1):
    L.append('            "HSKTrait.FrameHead.%d",' % n)
L += ['        };',
      '        public static readonly string[][] FrameSceneVariants = new string[][]', '        {']
for n in range(1, FRAME_N + 1):
    L.append('            new string[] { "%d", "%d" },' % (n, variant_list('FrameScene', str(n))))
L += ['        };',
      '        public const int ExitVariants = %d;' % variant_list('Exit', '_'),
      '        public const int AbstainVariants = %d;' % variant_list('Abstain', '_'),
      '    }', '}']
open(os.path.join(BASE, 'Source', 'HSKLetterData.cs'), 'w', encoding='utf-8-sig').write('\n'.join(L) + '\n')
print('  写出 HSKLetterData.cs (成因 %d / 场景有文 %d / 习惯句特性 %d)'
      % (len(SLUG), len([s for s in set(SLUG.values()) if variant_list('Scene', s)]),
         len([t for t in sorted({d for d in wired_traits.values()} | pool) if variant_list('Habit', t)])))
print('[OK] Keyed %d 条 / 成因 %d / 机制一池特性 %d' % (len(store), len(wired_traits), len(pool)))
