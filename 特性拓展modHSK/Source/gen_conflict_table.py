# -*- coding: utf-8 -*-
"""
特性拓展modHSK —— 特性冲突总表生成器
从真实 XML 源抽取各 TraitDef 的 <conflictingTraits> 成互斥对,
合并 HSK_ 自定义互斥对, 输出完整「规则-特性冲突.csv」,
并标注 状态(已做/需做) 与 归属/处理。
"""
import os, csv, glob, io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
from lxml import etree

BASE = r"c:\Personal\Project\ratkin-patch\特性拓展modHSK"
OUT  = os.path.join(BASE, "表", "规则-特性冲突.csv")

RIM  = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
DATA = os.path.join(RIM, "Data")
MODS = os.path.join(RIM, "Mods")
WS   = r"C:\Program Files (x86)\Steam\steamapps\workshop\content\294100"
P    = etree.XMLParser(recover=True)

SOURCES = [
    ("原版Core",   os.path.join(DATA,"Core","Defs","TraitDefs")),
    ("原版Biotech",os.path.join(DATA,"Biotech","Defs","TraitDefs")),
    ("原版Anomaly",os.path.join(DATA,"Anomaly","Defs","TraitDefs")),
    ("VTE",        os.path.join(WS,"2296404655","1.6","Defs","TraitDefs")),
    ("鼠族",       os.path.join(MODS,"RatkinRaceHSK","1.6","Defs","TraitDefs")),
    ("核SK",       os.path.join(MODS,"Core_SK","Defs","TraitDefs")),
]

def walk(d):
    if not os.path.isdir(d): return []
    return [f for f in glob.glob(os.path.join(d,'**','*.xml'), recursive=True)
            if 'DefInjected' not in f and 'Languages' not in f]

# defName -> 中文(粗略, 用英文原label兜底)
zh = {}
try:
    with open(os.path.join(BASE,"表","特性总表.csv"), encoding='utf-8-sig') as f:
        for r in csv.DictReader(f):
            zh[r['defName']] = r.get('中文名') or r.get('英文名') or r['defName']
except Exception:
    pass

# 1. 抽取原生互斥对 (声明者 -> 每个被禁者)
pairs = {}          # (A,B) sorted -> 信息
owning = {}         # defName -> 来源mod
for src, d in SOURCES:
    for f in walk(d):
        try: r = etree.parse(f, P)
        except Exception: continue
        for el in r.xpath('/Defs/*'):
            local = etree.QName(el).localname
            if local != 'TraitDef': continue
            dn = (el.findtext('defName') or '').strip()
            if not dn: continue
            owning.setdefault(dn, src)
            cnode = el.find('conflictingTraits')
            if cnode is None: continue
            for x in cnode.findall('li'):
                b = (x.text or '').strip()
                if not b: continue
                A, B = sorted([dn, b])
                pairs[(A,B)] = {
                    'A':A, 'B':B,
                    'srcA': owning.get(A, src), 'srcB': owning.get(B, src),
                    'declarer': dn, 'origin':'原生声明',
                }

# 2. HSK_ 自定义互斥对 (需在新建 TraitDef 时写入 conflictingTraits)
custom = [
    ("HSK_NightOwl","HSK_EarlyRiser","正负互斥","昼夜作息二选一","自定义"),
    ("HSK_HealthNut","HSK_Glutton","正负互斥","饮食路线二选一","自定义"),
    ("HSK_HealthNut","Gourmand","语义冲突","养生与贪嘴对立","自定义"),
    ("HSK_Glutton","Ascetic","语义冲突","暴食与禁欲对立","自定义"),
    ("HSK_Meditator","Bloodlust","语义冲突","禅修与嗜杀对立","自定义"),
    ("HSK_GymRat","Wimp","语义冲突","锻炼体魄与懦弱对立","自定义"),
    ("HSK_GymRat","VTE_ThinSkinned","语义冲突","锻炼体魄与娇嫩对立","自定义"),
    ("HSK_Bookworm","VTE_Dunce","语义冲突","博闻与智缺对立","自定义"),
    ("HSK_Bookworm","Ignorant","语义冲突","博闻与愚钝对立","自定义"),
]
# 3. 补充语义冲突对 (遍历发现: 原生未声明的语义对立, 拉平跨mod组合)
supplement = [
    ("NeatFreak","VTE_Slob","整洁对立","挑刺洁癖与邋遢鬼对立"),
    ("Perfectionist","VTE_Slob","整洁对立","完美主义洁癖与邋遢鬼对立"),
    ("DeepSleeper","QuickSleeper","作息对立","沉眠者与浅眠者对立"),
    ("DeepSleeper","VTE_Insomniac","作息对立","沉眠者与失眠症对立"),
    ("VTE_ColdInclined","VTE_HeatInclined","冷热对立","嗜冷与嗜热互斥"),
    ("ColdLover","VTE_HeatInclined","冷热对立","低温嗜好与嗜热冲突"),
    ("HeatLover","VTE_ColdInclined","冷热对立","高温嗜好与嗜冷冲突"),
    ("Kind","Bloodlust","善恶对立","善良与嗜血理念对立"),
    ("Kind","Cannibal","善恶对立","善良与食人癖对立"),
    ("Kind","VTE_MadSurgeon","善恶对立","善良与虐待行医对立"),
    ("Recluse","Extrovert","社性对立","孤僻与活泼外向对立"),
    ("Neurotic","NaturalMood","情绪对立","神经质与情绪稳定对立"),
    ("VTE_MadSurgeon","Medic","行医对立","虐待医师与济世名医对立"),
    ("Butcher","VTE_AnimalLover","亲疏对立","屠宰工匠与爱宠者对立"),
    ("Ascetic","VTE_FunLoving","生活哲学","禁欲苦行与贪玩享乐对立"),
    ("Reaver","Wimp","怯懦对立","格斗者与懦夫对立"),
    ("Reaver","Weak","怯懦对立","格斗者与柔弱对立"),
    ("Reaver","VTE_Coward","怯懦对立","格斗者与胆小鬼对立"),
    ("Slow","EatingSpeed","进食对立","慢性子与狼吞虎咽对立"),
    # ---- 2026-08-24 批量补充(统一补丁 ConflictingTraits.xml) ----
    ("Aesthete","Psychopath","善恶对立","唯美主义与心理变态对立"),
    ("Bipolar","Nerves","精神对立","躁郁抑郁与意志坚定(钢铁意志)对立"),
    ("Bipolar","Tough","精神对立","躁郁抑郁与坚韧对立"),
    ("Confident","VTE_Desensitized","情绪对立","乐观(自信)与冷漠对立"),
    ("Confident","VTE_WorldWeary","情绪对立","自信与悲观(厌世)对立"),
    ("VTE_Anxious","Nerves","精神对立","崩溃艺术家(忧虑)与钢铁意志对立"),
    ("VTE_Anxious","Tough","精神对立","崩溃艺术家(忧虑)与坚韧对立"),
    ("VTE_Schizoid","Nerves","精神对立","精神分裂与钢铁意志对立"),
    ("VTE_Schizoid","Tough","精神对立","精神分裂与坚韧对立"),
    ("Religious","VTE_Insatiable","信仰对立","虔诚与花心对立"),
    ("Religious","VTE_AbsentMinded","信仰对立","虔诚与胡思乱想对立"),
    ("Religious","VTE_Schizoid","信仰对立","虔诚与精神分裂对立"),
    ("VTE_Prude","VTE_AbsentMinded","信仰对立","专一与胡思乱想对立"),
    ("VTE_Prude","VTE_Schizoid","信仰对立","专一与精神分裂对立"),
    ("Bloodlust","Fragile","善恶对立","嗜血与脆弱对立"),
    ("Bloodlust","Delicate","善恶对立","嗜血与脆弱(Biotech)对立"),
    ("VTE_Workaholic","Slow","工作对立","工作狂与慢性子对立"),
    ("Industriousness","Slow","工作对立","勤奋与慢性子对立"),
    ("Wimp","Nerves","精神对立","懦弱与钢铁意志对立"),
    ("Wimp","Tough","精神对立","懦弱与坚韧对立"),
    ("Brawler","Weak","怯懦对立","好斗与柔弱对立"),
]
# 已落地为 1.6/Patches/ModPatches/ConflictingTraits.xml 双向 conflictingTraits 声明的补充语义对
DONE_SUPPLEMENT = {
    ("Aesthete","Psychopath"),
    ("Bipolar","Nerves"),
    ("Bipolar","Tough"),
    ("Bloodlust","Delicate"),
    ("Bloodlust","Fragile"),
    ("Confident","VTE_Desensitized"),
    ("Confident","VTE_WorldWeary"),
    ("Industriousness","Slow"),
    ("NeatFreak","VTE_Slob"),
    ("Reaver","VTE_Coward"),
    ("Reaver","Weak"),
    ("Reaver","Wimp"),
    ("Religious","VTE_AbsentMinded"),
    ("Religious","VTE_Insatiable"),
    ("Religious","VTE_Schizoid"),
    ("EatingSpeed","Slow"),
    ("Nerves","VTE_Anxious"),
    ("Tough","VTE_Anxious"),
    ("VTE_AbsentMinded","VTE_Prude"),
    ("VTE_Prude","VTE_Schizoid"),
    ("Nerves","VTE_Schizoid"),
    ("Tough","VTE_Schizoid"),
    ("Slow","VTE_Workaholic"),
    ("Nerves","Wimp"),
    ("Tough","Wimp"),
    ("Brawler","Weak"),
}
for a,b,typ,why in supplement:
    A,B = sorted([a,b])
    pairs.setdefault((A,B), {
        'A':A,'B':B,
        'srcA':owning.get(A,'?'), 'srcB':owning.get(B,'?'),
        'declarer':'','typ':typ,'why':why,'origin':'补充语义'})

zh_hsk = {
    "HSK_NightOwl":"夜猫子","HSK_EarlyRiser":"早起鸟",
    "HSK_HealthNut":"养生者","HSK_Glutton":"暴食者",
    "HSK_Meditator":"禅修者","HSK_GymRat":"健身狂",
    "HSK_Bookworm":"书虫","HSK_Musician":"乐手","HSK_Tactician":"策略家",
}
for a,b,typ,why,_ in custom:
    A,B = sorted([a,b])
    pairs.setdefault((A,B), {'A':A,'B':B,'srcA':'HSK','srcB':'HSK','declarer':'','typ':typ,'why':why,'origin':'HSK自定义'})

def z(dn): return zh.get(dn) or zh_hsk.get(dn, dn)

def ctype(row):
    if row.get('origin') in ('HSK自定义','补充语义'):
        return row.get('typ','语义冲突')
    return '原生互斥'

def status(row):
    isHSK = (row['srcA']=='HSK') or (row['srcB']=='HSK')
    if row.get('origin') in ('HSK自定义','补充语义'):
        if (row['A'], row['B']) in DONE_SUPPLEMENT:
            return '已做(补丁双向声明)'
        return '需要做'
    if not isHSK and row.get('origin')=='原生声明':
        return '已做(原生内建)'
    return '需要做'

def owner(row):
    if row.get('origin')=='补充语义':
        if (row['A'], row['B']) in DONE_SUPPLEMENT:
            return '特性拓展mod ConflictingTraits_*.xml 双向 conflictingTraits 声明'
        return '补充:向双方TraitDef双向加装 conflictingTraits + 生成期兜底校验'
    if row.get('origin')=='HSK自定义':
        return '写入HSK_* TraitDef 的 conflictingTraits'
    aHSK, bHSK = row['srcA']=='HSK', row['srcB']=='HSK'
    if aHSK or bHSK:
        other = row['B'] if aHSK else row['A']
        return f"XML补丁向 {other}({z(other)}) 加装 HSK_* 冲突"
    if row['srcA']==row['srcB']:
        return f"{row['srcA']} 原样已内建,无需改"
    return f"{row['srcA']}×{row['srcB']} 跨mod交叉声明(生成期兜底校验)"

rows = []
for (A,B), row in pairs.items():
    rows.append({
        '特性A':A, '特性A中文':z(A),
        '特性B':B, '特性B中文':z(B),
        '冲突类型':ctype(row),
        '第几方声明':row.get('declarer','')+' / '+row.get('origin',''),
        '状态':status(row),
        '归属/处理':owner(row),
    })
rows.sort(key=lambda r:(r['状态']=='已做(原生内建)', r['特性A'], r['特性B']))

with open(OUT,'w',newline='',encoding='utf-8-sig') as f:
    w=csv.writer(f)
    w.writerow(['特性A','特性A中文','特性B','特性B中文','冲突类型','声明来源','状态','归属/处理'])
    for r in rows:
        w.writerow([r['特性A'],r['特性A中文'],r['特性B'],r['特性B中文'],r['冲突类型'],r['第几方声明'],r['状态'],r['归属/处理']])

done = sum(1 for r in rows if r['状态'].startswith('已做'))
need = sum(1 for r in rows if r['状态']=='需要做')
print(f"完成, 总互斥对 {len(rows)} 组; 已做(原生内建) {done}; 需要做 {need}")