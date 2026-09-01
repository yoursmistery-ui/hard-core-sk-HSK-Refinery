# -*- coding: utf-8 -*-
"""
特性拓展modHSK —— C# 数据生成器
读取 规则-生活习惯特性.csv 与 规则-背景关联特质池.csv,
生成 Source/HSKGeneratedData.cs (全 ASCII 内嵌数据)。
中文一律不进代码, 由 DefInjected 翻译提供显示名。
"""
import os, csv, io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE = r"c:\Personal\Project\ratkin-patch\特性拓展modHSK"
OUT  = os.path.join(BASE, "Source", "HSKGeneratedData.cs")
TAB  = os.path.join(BASE, "表")

# 人格域 -> ASCII key (机制一用)
DOMAIN_KEY = {
    "战斗域":"Combat","社交域":"Social","信仰域":"Faith","情爱域":"Love",
    "工作域":"Work","生活域":"Life","身体域":"Body","灵能域":"Psy",
}

# 行为维度 -> (triggerType, triggerTarget)。None = 暂未接线(不触发)
TRIGGER = {
    "园艺":("Plant",""),            "屠宰":("Job","ButcherCorpse"),
    "看电视":("JoyKind","Television"), "饮酒":("Drug","Alcohol"),
    "吸烟":("Drug","Smokeleaf"),     "药物滥用":("Drug","Other"),
    "制药/化学":("Job","DoBills"),   "烹饪":("Job","CookFood"),
    "研究":("Job","Research"),        "医疗":("Job","TendPatient"),
    "手工/工匠":("Job","SmithWeapon"),"清洁/整洁":("Job","Clean"),
    "建造":("WorkType","Construction"), "岩矿":("Job","Mine"),
    "近战":("Job","AttackMelee"),     "闪避/肉搏":("Job","AttackMelee"),
    "射击":("Job","AttackRanged"),    "击杀人类":("Kill","Humanlike"),
    "残忍手术":("Job","DoSurgery"),   "情爱":("Job","Lovin"),
    "社交闲聊":("Job","SocialRelax"), "商贸/谈判":("Job","Trade"),
    "外交":("Job","Negotiate"),       "动物":("Job","Tame"),
    "偷窃":("Job","Steal"),           "餐饮讲究":("Eat",""),
    "战后创伤":("None",""),           "耐痛/战场":("None",""),
    "体质抗病":("None",""),
    "温度-耐热":("TempHot",""),       "温度-耐寒":("TempCold",""),
    "进食习惯":("Eat",""),            "孤独倾向":("None",""),
    "压力崩溃":("MentalBreak",""),     "唯美追求":("WorkType","Art"),
    # ★ 作息拆成两个独立计数器: 旧表"作息/熬夜"单计数器同时挂 NightOwl(正)/EarlyRiser(负),
    #   导致"熬夜越多越可能拿到早起鸟", 语义完全反了。
    "作息-夜行":("Night",""),         "作息-晨行":("Dawn",""),
    "阅读":("Job","ReadBook"),
    "音乐演奏":("Job","PlayMusic"),   "健身":("Job","Workout"),
    "冥想":("Job","Meditate"),        "棋牌":("Job","PlayBilliards"),
    "饮食健康":("Eat",""),            "饮食放纵":("None",""),   # 放纵无可靠数据源, 暂不接线
    "动物虐待":("AbuseAnimal",""),
}

def split_trait(s):
    """'BrownThumb:1' -> ('BrownThumb',1); '--'/'' -> ('',0)"""
    s = (s or '').strip()
    if not s or s == '--':
        return ('', 0)
    if ':' in s:
        n, d = s.split(':', 1)
        try: deg = int(d)
        except Exception: deg = 0
        return (n.strip(), deg)
    return (s, 0)

import re
def parse_int(s):
    """从 '120次成熟收获' 提取前导数字; '--'/'' -> 0"""
    s = (s or '').strip()
    m = re.match(r'\s*(\d+)', s)
    if m:
        return int(m.group(1))
    return 0

def parse_float(s):
    """解析授予概率(0~1); 缺失/-- -> 0.1(默认降10倍)"""
    s = (s or '').strip()
    if not s or s == '--':
        return 0.1
    try:
        v = float(s)
        return v if 0.0 <= v <= 1.0 else 0.1
    except Exception:
        return 0.1

life = []
with open(os.path.join(TAB, "规则-生活习惯特性.csv"), encoding='utf-8-sig') as f:
    for r in csv.DictReader(f):
        key = r['行为维度'].strip()
        ttype, ttarget = TRIGGER.get(key, ("None",""))
        pthr = parse_int(r['正向阈值']); pdn, pdg = split_trait(r['正向特性'])
        nthr = parse_int(r['负向阈值']); ndn, ndg = split_trait(r['负向特性'])
        ch   = parse_float(r.get('授予概率'))
        life.append((key, ttype, ttarget, pthr, pdn, pdg, nthr, ndn, ndg, parse_int(r['冷却天数']), ch))

pool = []
with open(os.path.join(TAB, "规则-背景关联特质池.csv"), encoding='utf-8-sig') as f:
    for r in csv.DictReader(f):
        dk = DOMAIN_KEY.get(r['人格域'].strip(), r['人格域'].strip())
        pool.append((dk, r['候选特质defName'].strip(),
                     parse_int(r['基础权重']), r['强耦合技能域'].strip()))

# 语义冲突对: 从 规则-特性冲突.csv 抽取 状态=需要做 且 声明来源含"补充语义" 的跨mod冲突,
# 运行时启动把双方 TraitDef conflictingTraits 双向注入(HSK自定义已写在 HSK_Traits.xml, 不重复发射)。
conflicts = []
with open(os.path.join(TAB, "规则-特性冲突.csv"), encoding='utf-8-sig') as f:
    for r in csv.DictReader(f):
        state = (r.get('状态') or '').strip()
        src   = (r.get('声明来源') or '').strip()
        if state != '需要做':
            continue
        if '补充语义' not in src:
            continue
        a = (r.get('特性A') or '').strip()
        b = (r.get('特性B') or '').strip()
        if a and b:
            conflicts.append((a, b))

def esc(s):
    return s.replace('\\','\\\\').replace('"','\\"')

L = []
L.append('// 由 Source/gen_csharp_data.py 生成,勿手改。')
L.append('namespace HSKTraitExt')
L.append('{')
L.append('    public static class HSKData')
L.append('    {')
L.append('        public struct LifestyleRule')
L.append('        {')
L.append('            public string Key, TriggerType, TriggerTarget;')
L.append('            public int PosThreshold, PosDegree; public string PosTrait;')
L.append('            public int NegThreshold, NegDegree; public string NegTrait;')
L.append('            public int CooldownDays;')
L.append('            public float Chance; // 达标后授予概率(0~1), 默认0.1=原100%降10倍; 毒品类0.25')
L.append('        }')
L.append('        public static readonly LifestyleRule[] LifestyleRules = new LifestyleRule[]')
L.append('        {')
for (key,tt,tgt,pthr,pdn,pdg,nthr,ndn,ndg,cd,ch) in life:
    L.append('            new LifestyleRule{ Key="%s", TriggerType="%s", TriggerTarget="%s", PosThreshold=%d, PosTrait="%s", PosDegree=%d, NegThreshold=%d, NegTrait="%s", NegDegree=%d, CooldownDays=%d, Chance=%sf },' % (
        esc(key), esc(tt), esc(tgt), pthr, esc(pdn), pdg, nthr, esc(ndn), ndg, cd, ch))
L.append('        };')
L.append('        public struct DomainRow { public string Domain, TraitDef, Coupling; public int Weight; }')
L.append('        public static readonly DomainRow[] DomainPool = new DomainRow[]')
L.append('        {')
for (dk, dn, w, cp) in pool:
    L.append('            new DomainRow{ Domain="%s", TraitDef="%s", Weight=%d, Coupling="%s" },' % (esc(dk), esc(dn), w, esc(cp)))
L.append('        };')
L.append('        // 语义冲突对(跨mod补充语义): 运行时双向注入双方 conflictingTraits')
L.append('        public static readonly string[][] ConflictPairs = new string[][]')
L.append('        {')
for (a, b) in conflicts:
    L.append('            new string[] { "%s", "%s" },' % (esc(a), esc(b)))
L.append('        };')
L.append('    }')
L.append('}')

with open(OUT, 'w', encoding='utf-8-sig') as f:
    f.write('\n'.join(L) + '\n')
print('OK', OUT, 'lifestyle=', len(life), 'domainpool=', len(pool), 'conflicts=', len(conflicts))