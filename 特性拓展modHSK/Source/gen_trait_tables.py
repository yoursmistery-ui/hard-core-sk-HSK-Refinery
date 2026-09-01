# -*- coding: utf-8 -*-
"""
特性拓展modHSK —— 基石表生成器 v2
从真实 XML 数据源抽取「特性总表」与「背景表」。
表为唯一数据源, 输出 CSV + HTML 可视化。
"""
import os, csv, glob, html
from lxml import etree

BASE = r"c:\Personal\Project\ratkin-patch\特性拓展modHSK"
OUT  = os.path.join(BASE, "表")
os.makedirs(OUT, exist_ok=True)

RIM  = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
DATA = os.path.join(RIM, "Data")
MODS = os.path.join(RIM, "Mods")
WS   = r"C:\Program Files (x86)\Steam\steamapps\workshop\content\294100"
NS   = {"x": etree.XMLParser(recover=True, remove_blank_text=True)}

def parse(p):
    if not os.path.exists(p):
        return None
    try:
        return etree.parse(p, NS["x"])
    except Exception:
        return None

def localname(el):
    return etree.QName(el).localname

def walk_xml_files(dirs, recursive=True, forbidden=('Languages','DefInjected','Upload_Workspace')):
    for d in dirs:
        if not os.path.isdir(d):
            continue
        for f in sorted(glob.glob(os.path.join(d, '**', '*.xml'), recursive=recursive)):
            if any(x in f for x in forbidden):
                continue
            yield f

# ============================================================== 1. 特性抽取
def extract_trait(file, source):
    r = parse(file)
    if r is None:
        return []
    got = []
    for d in r.xpath('/Defs/*'):
        if localname(d) != 'TraitDef':
            continue
        dn = (d.findtext('defName') or '').strip()
        if not dn:
            continue
        degs = []
        dg = d.find('degreeDatas')
        if dg is not None:
            for i, li in enumerate(dg.findall('li')):
                degs.append({'idx': i,
                             'label': (li.findtext('label') or '').strip(),
                             'desc': (li.findtext('description') or '').strip()})
        conflict = [c.text for c in d.iterfind('.//conflictingTraits')] or []
        cfl = []
        cnode = d.find('conflictingTraits')
        if cnode is not None:
            cfl = [x.text for x in cnode.findall('li') if x.text]
        perks = []
        for perf in ('statOffsets','statFactors'):
            sn = d.find('.//'+perf)
            if sn is not None:
                perks.append(perf + ':' + ','.join(x.tag for x in sn))
        got.append({'defName': dn, 'source': source,
                    'commonality': (d.findtext('commonality') or '').strip(),
                    'conflicts': cfl,
                    'degrees': degs,
                    'degreeLabels': ' / '.join(x['label'] for x in degs),
                    'modifiers': ';'.join(perks)})
    return got

traits = {}
for f in walk_xml_files([
        os.path.join(DATA,"Core","Defs","TraitDefs"),
        os.path.join(DATA,"Biotech","Defs","TraitDefs"),
        os.path.join(DATA,"Anomaly","Defs","TraitDefs"),
        os.path.join(WS,"2296404655","1.6","Defs","TraitDefs") if os.path.isdir(os.path.join(WS,"2296404655")) else os.path.join(BASE, "1.6", "Defs", "TraitDefs"),
        os.path.join(MODS,"RatkinRaceHSK","1.6","Defs","TraitDefs"),
        os.path.join(MODS,"Core_SK","Defs","TraitDefs")]):
    src = ('原版Biotech' if 'Biotech' in f else
           '原版Anomaly' if 'Anomaly' in f else
           'HSK' if 'HSK_Traits' in f else
           'VTE' if ('2296404655' in f or '特性拓展modHSK' in f) else
           '鼠族' if 'RatkinRaceHSK' in f else
           '核SK' if 'Core_SK' in f else '原版Core')
    for t in extract_trait(f, src):
        traits.setdefault(t['defName'], t)

# 补充互斥(落地于 1.6/Patches/ModPatches/ConflictingTraits.xml, 非 Defs 原生声明,
# 仅补文档, 不影响 categorize 分类——该函数未使用 conflicts 参数)
CONFLICT_SUPPLEMENT = {
    "Reaver": ["Wimp", "Weak", "VTE_Coward"],
    "Wimp": ["Reaver", "Nerves", "Tough"],
    "Weak": ["Reaver", "Brawler"],
    "VTE_Coward": ["Reaver"],
    "Slow": ["EatingSpeed", "VTE_Workaholic", "Industriousness"],
    "EatingSpeed": ["Slow"],
    "NeatFreak": ["VTE_Slob"],
    "VTE_Slob": ["NeatFreak"],
    "Aesthete": ["Psychopath"],
    "Psychopath": ["Aesthete"],
    "Bipolar": ["Nerves", "Tough"],
    "Nerves": ["Bipolar", "Wimp", "VTE_Anxious", "VTE_Schizoid"],
    "Tough": ["Bipolar", "Wimp", "VTE_Anxious", "VTE_Schizoid"],
    "Confident": ["VTE_Desensitized", "VTE_WorldWeary"],
    "VTE_Desensitized": ["Confident"],
    "VTE_WorldWeary": ["Confident"],
    "VTE_Anxious": ["Nerves", "Tough"],
    "VTE_Schizoid": ["Nerves", "Tough", "Religious", "VTE_Prude"],
    "Religious": ["VTE_Insatiable", "VTE_AbsentMinded", "VTE_Schizoid"],
    "VTE_Insatiable": ["Religious"],
    "VTE_AbsentMinded": ["Religious", "VTE_Prude"],
    "VTE_Prude": ["VTE_AbsentMinded", "VTE_Schizoid"],
    "Bloodlust": ["Fragile", "Delicate"],
    "Fragile": ["Bloodlust"],
    "Delicate": ["Bloodlust"],
    "VTE_Workaholic": ["Slow"],
    "Industriousness": ["Slow"],
    "Brawler": ["Weak", "VTE_Coward"],
}
for dn, adds in CONFLICT_SUPPLEMENT.items():
    if dn in traits:
        for a in adds:
            if a not in traits[dn]['conflicts']:
                traits[dn]['conflicts'].append(a)

# ============================================================== 2. 背景抽取
def extract_backstory(file, source):
    r = parse(file)
    if r is None:
        return []
    got = []
    for d in r.xpath('/Defs/*'):
        tag = localname(d)
        if tag not in ('BackstoryDef','AlienRace.AlienBackstoryDef','BackstoryDef'):
            continue
        dn = (d.findtext('defName') or '').strip()
        if not dn:
            continue
        slot = (d.findtext('slot') or '').strip()
        # 强制特质
        forced = []
        fnode = d.find('forcedTraits')
        if fnode is not None:
            forced += [x.text for x in fnode.findall('li') if x.text and not x.get('Inherit')]
        ftc = d.find('forcedTraitsChance')
        if ftc is not None:
            for x in ftc.findall('li'):
                df = (x.findtext('defName') or '').strip()
                if df:
                    forced.append(df + ((':'+(x.findtext('chance') or '100')) if x.findtext('chance') else ''))
        dis = []
        dnode = d.find('disallowedTraits')
        if dnode is not None:
            dis += [x.text for x in dnode.findall('li') if x.text]
        dtc = d.find('disallowedTraitsChance')
        if dtc is not None:
            dis += [(x.findtext('defName') or '') for x in dtc.findall('li')]
        dis = [x for x in dis if x]
        # 技能
        skills = []
        sn = d.find('skillGains')
        if sn is not None:
            skills = [f"{x.tag}{x.text}" for x in sn]
        got.append({'defName': dn, 'source': source,
                    'title': (d.findtext('title') or '').strip(),
                    'titleShort': (d.findtext('titleShort') or '').strip(),
                    'slot': slot,
                    'forcedTraits': forced, 'disallowedTraits': dis,
                    'skills': skills})
    return got

backstories = {}
for f in walk_xml_files([
        os.path.join(DATA,"Core","Defs","BackstoryDefs"),
        os.path.join(DATA,"Royalty","Defs","BackstoryDefs"),
        os.path.join(DATA,"Anomaly","Defs","BackstoryDefs"),
        os.path.join(MODS,"RatkinRaceHSK","1.6","Defs","BackStoryDefs"),
        os.path.join(MODS,"RatkinBackStoryExpandedHSK","1.6","Defs","BackStoryDefs")]):
    src = ('原版Royalty' if 'Royalty' in f else
           '原版Anomaly' if 'Anomaly' in f else
           '鼠族扩展' if 'RatkinBackStoryExpandedHSK' in f else
           '鼠族' if 'RatkinRaceHSK' in f else '原版Core')
    for b in extract_backstory(f, src):
        backstories.setdefault(b['defName'], b)

# ============================================================== 3. 中文标签
def build_labels(leafnames):
    mp = {}
    roots = []
    for base in (MODS, WS):
        for lp in glob.glob(os.path.join(base, '*', '**', 'DefInjected'), recursive=True):
            la = lp.replace('\\', '/')
            if any(x in la for x in ('ChineseSimplified','简体中文')):
                roots.append(lp)
    for root in roots:
        for f in glob.glob(os.path.join(root, '**', '*.xml'), recursive=True):
            leaf = f.replace('\\','/').split('/')[-2]
            if leaf not in leafnames:
                continue
            r = parse(f)
            if r is None:
                continue
            for el in r.iter():
                if el.tag is etree.Comment or not el.tag:
                    continue
                t = etree.QName(el).localname
                parts = t.split('.')
                if len(parts) < 2:
                    continue
                if parts[-1] not in ('label','labelShort','title'):
                    continue
                key = parts[0]
                val = (el.text or '').strip()
                if val:
                    mp.setdefault(key, {})[parts[-1]] = val
    return mp

trait_zh = build_labels({'TraitDef'})
back_zh  = build_labels({'BackstoryDef','AlienRace.AlienBackstoryDef','BackstoryDef','AlienRace.AlienBackstoryDef'})

def zh(dmap, dn):
    m = dmap.get(dn)
    return (m.get('label') or m.get('labelShort') or m.get('title') or '') if m else ''

FALLBACK = {
 'Brawler':'好斗','Bloodlust':'嗜血','Nudist':'裸体主义','Ascetic':'禁欲主义','Cannibal':'食人癖',
 'Psychopath':'精神变态','Masochist':'受虐狂','Nerves':'心理坚韧','Wimp':'懦夫','Pyromaniac':'纵火狂',
 'IronWilled':'钢铁意志','Industrious':'勤奋','Greedy':'贪婪','Optimist':'乐观主义','Pessimist':'悲观主义',
 'Sanguine':'乐天派','Gloomy':'忧郁症','Depressive':'抑郁症','Tough':'坚韧','Sickly':'体弱多病',
 'ShootingAccuracy':'射击神经','MeleeWeaponTough':'格斗好手','GreatShooter':'神枪手','GreatMelee':'格斗大师',
 'Jogger':'慢跑者','FastWalker':'疾行者','Prosthophile':'义体痴迷','Prosthophobe':'义体恐惧','Nimble':'身手敏捷',
 'CarefulShooter':'谨慎射手','TriggerHappy':'上瘾枪手','TouchSensitive':'触觉敏感','ChemicalInterest':'嗜毒',
 'ChemicalFascination':'化学迷','QuickSleeper':'睡眠轻浅','Undergrounder':'深居简出','BodilyPurist':'身体纯净主义者',
 'Transhumanist':'超人类主义者','Teetotaler':'禁酒主义','Abasia':'站立动摇','Downed':'倒下',
 'Bloodthirsty':'嗜血','Sensitive':'敏感','Wimp':'懦夫','FastLearner':'悟性高','SlowLearner':'迟钝学习',
 'Kind':'善良','Jealous':'嫉妒','Cocky':'自大','Lazy':'懒惰','Anxious':'焦虑',
 'HappyThatPawnDied':'幸灾乐祸','Idealist':'理想主义','Steadfast':'刚毅',
 # 原版Core 频谱/特性兜底
 'Asexual':'无性恋','Beauty':'魅力','Bisexual':'双性恋','BodyPurist':'身体纯净','DislikesMen':'厌恶男性',
 'DislikesWomen':'厌恶女性','Gay':'同性恋','Gourmand':'美食家','Neurotic':'神经质','NightOwl':'夜猫子',
 'SpeedOffset':'敏捷','TooSmart':'过于聪明','TorturedArtist':'受折磨的艺术家','GreatMemory':'过目不忘',
 'NaturalMood':'情绪稳定','CreepyBreathing':'毛骨悚然','Industriousness':'勤奋','PsychicSensitivity':'灵能敏感',
 'DrugDesire':'嗜药','Abrasive':'尖刻','AnnoyingVoice':'恼人的嗓音','Immunity':'免疫力','PsychicRead':'读心',
 'PsychicShock':'灵能休克','PsychicHearing':'灵能听觉','Spinal':'脊柱','Bionic':'仿生','Neural':'神经',
 'Beautiful':'美丽','Ugly':'丑陋','Stereotypical':'刻板',
 # 核SK(Core_SK)职业类特质兜底
 'Butcher':'屠夫','BrownThumb':'园艺不通','Aptitude':'迟钝','Chemist':'药剂师','Gourmet':'美厨',
 'Inventor':'发明家','Eyesight':'眼力','NeatFreak':'洁癖','Perfectionist':'完美主义','Medic':'医者',
 'Rockhound':'岩痴','Claustrophobic':'幽闭恐惧','ColdLover':'畏热喜冷','HeatLover':'畏冷喜热',
 'DeepSleeper':'沉睡','PainThreshold':'耐痛','Dodging':'闪避','Trader':'商人','Diplomat':'外交家',
 'Constitution':'体质','EatingSpeed':'进食速度','Aesthete':'唯美家','Bipolar':'躁郁症',
 'Nyctophobe':'恐黑','MedicSkill':'医者'}

PERKS = {
 'compassionate':'同情','empath':'同理心','guardian':'守护','protector':'保护','brave':'勇敢',
 'noble':'高尚','just':'正义','mastery':'精通','devoted':'忠诚','careful':'谨慎','stoic':'坚忍',
 'fearless':'无畏','stoic':'坚忍','mentor':'导师','leader':'领袖','careful':'细致','selfless':'无私',
 'loyal':'忠诚'}

# ============================================================== 4. 分类/倾向启发
POS_KW = ['optimist','sanguine','industrious','hard','tough','irons','steel','nimble','great','fast',
          'kind','faith','positive','stalwart','resilient','brave','fearless','prodigy','genius','gifted',
          'nimble','quick','stellar','master','devoted','charitable','stoic','disciplined','diligent',
          'attractive','lucky','rich','tycoon','workaholic','thick','immune','clever','wise','tenacious',
          'loyal','compassion','guardian','pure','blessed','spirited','eagle','refined','love','menagerist']
NEG_KW = ['wimp','nerves','sickly','pessimist','gloomy','depressive','abrasive','annoying','lazy','greedy',
          'pyromaniac','bloodlust','neg','clumsy','dunce','dull','tech','animalhater','coward','desensitized',
          'schizoid','slob','squeamish','thin','submissive','vengeful','world','anxious','insomnia','paranoid',
          'drug','addict','stoner','jealous','slob','greedy','narcissist','shame','fear']

def categorize(dn, lab, conflicts):
    base = (dn + ' ' + lab).lower()
    combos = [('战斗类',['shoot','melee','brawler','gun','trigger','careful','attack','fight','iron','tough','brave','martial','sword']),
              ('社交类',['social','abras','annoy','polit','talk','friend','charitable','kind','greedy','jealous','empath','pervasive' ]),
              ('信仰类',['faith','pious','devout','religious','spiritual','ceremon']),
              ('情爱类',['lovin','insatiable','prude','lust','attract','romanc','flirt']),
              ('工作类',['industr','hard','work','lazy','diligent','labor','craft','prodigy','tycoon','bills','store']),
              ('生活类',['couch','tv','gourmet','gastronom','wine','smoke','drug','alcohol','drink','food','chef','stomach']),
              ('身体类',['sickly','nimble','big','strong','sturdy','nudist','masoch','cannibal','sleep','immune','cold','heat','dry','iron stomach','vanity']),
              ('灵能类',['psych','psyt','insight'])]
    cat = '其他'
    for cn, kws in combos:
        if any(k in base for k in kws):
            cat = cn
            break
    pol = '正面' if any(k in base for k in POS_KW) else ('负面' if any(k in base for k in NEG_KW) else '中性')
    return cat, pol

# ============================================================== 5. 写 CSV + HTML
def to_csv(rows, fn, fields):
    with open(os.path.join(OUT, fn), 'w', newline='', encoding='utf-8-sig') as fp:
        w = csv.DictWriter(fp, fieldnames=fields, extrasaction='ignore')
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print(f"[OK] {fn}: {len(rows)} 行")

def zh_list(dmap, l):
    return '、'.join(zh(dmap, x.split(':')[0]) or x for x in l)

# ---- 特性总表
trait_rows = []
for t in traits.values():
    dn = t['defName']
    deg = t['degrees'][0] if t['degrees'] else {}
    zh_name = zh(trait_zh, dn) or FALLBACK.get(dn, '') or deg.get('label','')
    cat, pol = categorize(dn, deg.get('label',''), t['conflicts'])
    trait_rows.append({
        'defName': dn, '中文名': zh_name, '英文名': deg.get('label',''), '来源': t['source'],
        '程度数': len(t['degrees']), '常见度': t['commonality'],
        '类别': cat, '倾向': pol,
        '冲突特质': zh_list(trait_zh, t['conflicts']),
        '全程度': t['degreeLabels'],
        '加成/惩罚': t['modifiers'],
        '描述': deg.get('desc','')[:100],
        '因果经历标签': ''})
trait_rows.sort(key=lambda r: (r['类别'], r['倾向'], r['defName']))
TR_FIELDS = list(trait_rows[0].keys())
to_csv(trait_rows, '特性总表.csv', TR_FIELDS)

# ---- 背景表
back_rows = []
for b in backstories.values():
    dn = b['defName']
    zh_name = zh(back_zh, dn) or b['title']
    slot = {'Adulthood':'成年','Childhood':'童年'}.get(b['slot'], b['slot'])
    back_rows.append({
        'defName': dn, '中文名': zh_name,
        '英文标题': b['title'], '简称': b['titleShort'],
        '阶段': slot, '来源': b['source'],
        '强制/倾向特质': zh_list(trait_zh, b['forcedTraits']),
        '禁用特质': zh_list(trait_zh, b['disallowedTraits']),
        '技能加成': '、'.join(b['skills']),
        '关联经历': ''})
back_rows.sort(key=lambda r: (r['来源'], r['阶段'], r['defName']))
BK_FIELDS = list(back_rows[0].keys())
to_csv(back_rows, '背景表.csv', BK_FIELDS)

# ============================================================== HTML 渲染
cat_color = {'战斗类':'#e74c3c','社交类':'#9b59b6','信仰类':'#8e44ad','情爱类':'#e91e63',
             '工作类':'#2ecc71','生活类':'#f39c12','身体类':'#16a085','灵能类':'#34495e','其他':'#7f8c8d'}
pol_icon = {'正面':'▲','负面':'▼','中性':'◆'}

def render(rows, fields, title, outfn, primary='defName'):
    c = ['<style>', 'body{font-family:"Microsoft YaHei",sans-serif;margin:24px;color:#2c3e50}',
         'h1{font-size:22px;border-bottom:2px solid #3498db;padding-bottom:8px}',
         'h2{color:#7f8c8d;font-weight:normal;font-size:14px;margin-top:-8px}',
         'table{border-collapse:collapse;width:100%;font-size:13px;margin-top:14px}',
         'th{background:#34495e;color:#fff;padding:6px 8px;text-align:left;position:sticky;top:0}',
         'td{border:1px solid #ddd;padding:5px 8px;vertical-align:top}',
         'tr:nth-child(even){background:#f8f9fa}', 'td.cat{border-radius:4px;color:#fff;text-align:center;font-weight:bold}',
         'td.pol{text-align:center}', '.aur{color:#8e44ad;font-weight:bold}',
         '.btn{display:inline-block;margin:6px 6px 0 0;padding:4px 10px;border-radius:14px;color:#fff;cursor:pointer;font-size:12px}',
         '</style>']
    summary = {}
    for r in rows:
        summary.setdefault(r.get('类别','其他'), 0)
        summary[r.get('类别','其他')] += 1
    c.append(f'<h1>{title}</h1><h2>共 {len(rows)} 条 | ' + ' | '.join(f'{k} {summary[k]}' for k in summary) + '</h2>')
    for k, n in summary.items():
        c.append(f'<span class="btn" style="background:{cat_color.get(k,"#999")}">{k} {n}</span>')
    c.append('<table><tr>' + ''.join(f'<th>{h}</th>' for h in fields) + '</tr>')
    for r in rows:
        tds = []
        for h in fields:
            if h == '类别':
                cc = cat_color.get(r.get(h,''), '#999')
                tds.append(f'<td class="cat" style="background:{cc}">{r.get(h,"")}</td>')
            elif h == '倾向':
                ic = pol_icon.get(r.get(h,''), '')
                tds.append(f'<td class="pol">{ic}{r.get(h,"")}</td>')
            else:
                v = r.get(h, '')
                v = html.escape(str(v))
                if h == 'defName':
                    v = f'<b>{v}</b>'
                elif h in ('描述','加成/惩罚') and len(str(v)) > 60:
                    v = v[:58] + '…'
                tds.append(f'<td>{v}</td>')
        c.append('<tr>' + ''.join(tds) + '</tr>')
    c.append('</table>')
    open(os.path.join(OUT, outfn), 'w', encoding='utf-8').write('\n'.join(c))
    print(f"[OK] {outfn}")

render(trait_rows, TR_FIELDS, '特性总表(基石A · 唯一数据源 特性总表.csv)', '特性总表.html')
render(back_rows, BK_FIELDS, '背景表(基石B · 唯一数据源 背景表.csv)', '背景表.html')

print('\n—— 完成 ——')
print('来源统计(特性):', end=' ')
from collections import Counter
tr_c = Counter(r['来源'] for r in trait_rows)
bk_c = Counter(r['来源'] for r in back_rows)
print(dict(tr_c))
print('来源统计(背景):', dict(bk_c))