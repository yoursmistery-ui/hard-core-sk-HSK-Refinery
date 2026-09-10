# -*- coding: utf-8 -*-
# 生成: 每个待迁移配方 recipe defName + 当前research + 目标外观(方案)
# 输出映射供构建补丁
import re, collections

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
raw = open(UNIFIED, 'r', encoding='utf-16').read().encode('utf-8')

# 产品 -> 中文label
prod_label = {}
for m in re.finditer(rb'<ThingDef\b[^>]*>(.*?)</ThingDef>', raw, flags=re.S):
    b = m.group(1)
    dn = re.search(rb'<defName>([^<]+)</defName>', b)
    lab = re.search(rb'<label>([^<]+)</label>', b)
    if dn and lab: prod_label[dn.group(1).decode()] = lab.group(1).decode()

# 全部RecipeDef
recipes = {}
for m in re.finditer(rb'<RecipeDef\b[^>]*>(.*?)</RecipeDef>', raw, flags=re.S):
    block = m.group(0)
    dn = re.search(rb'<defName>([^<]+)</defName>', block)
    if not dn: continue
    dn = dn.group(1).decode()
    prod = None
    pm = re.search(rb'<products>(.*?)</products>', block, flags=re.S)
    if pm:
        ps = re.search(rb'<([A-Za-z_][\w]*)>', pm.group(1))
        if ps: prod = ps.group(1).decode()
    if not prod:
        pm = re.search(rb'<product>([^<]+)</product>', block)
        if pm: prod = pm.group(1).decode()
    res = None
    rm = re.search(rb'<researchPrerequisite>([^<]+)</researchPrerequisite>', block)
    if rm: res = rm.group(1).decode()
    if not res:
        rm = re.search(rb'<researchPrerequisites>\s*<li>([^<]+)</li>', block)
        if rm: res = rm.group(1).decode() + '|LIST'
    recipes[dn] = {'product': prod, 'research': res}

# 方案的目标分组: 产品 -> 目标节点
# 按产品 defName 前缀规则 + 精确清单(从 cat_plan / 方案)
# 手工定义目标映射 (产品defName -> 新节点)
target = {}
def add_group(node, prods):
    for p in prods: target[p] = node

# B1b 头脸配饰
add_group('Ratkin_Apparel_B1b', ['RK_Bandage','RK_EyeMask','RK_Respirator','RK_WeijingA','RK_BigBeard',
    'RK_MedalHonor','RK_YaoBao','RK_Kuabao','RK_WorkApron','RK_LargePipe'])
# B2A 礼服装(留在B2A)
add_group('Ratkin_Apparel_B2A', ['RK_FrillOnepiece','RK_SistersDerss','RK_SistersVeil','RK_Crusader',
    'RA_DetectiveDress','RA_DetectiveHat','RK_FlatColorCoat','RK_RibbonHairBand','RK_HairCorsage','RK_ResearchGown'])
# B2B 冬暖装
add_group('Ratkin_Apparel_B2B', ['RK_WhiteCoat','RK_WhiteCloak','baonuanneiyi','RK_WinterRobe','xiaokb',
    'yiliaob','RK_ExplorerWear','RK_ExplorerHat'])
# B2C 职业装备
add_group('Ratkin_Apparel_B2C', ['RK_ResearchGlasses','RK_GlassesB','RK_GlassesR','RK_Monocle','RK_ChefHat',
    'RK_ChefSuit','RK_MaidA','RK_MaidB','RK_GuardUniform','RK_GuardHat'])
# B2D 软甲防御
add_group('Ratkin_Apparel_B2D', ['RK_Gambeson','RK_GambesonB','RK_OuterPlate','RK_Vest','RK_Rainproof'])
# C1 民用(留在C1)
add_group('Ratkin_Apparel_C1', ['RK_Backpack','RK_Cardigan','RK_Coif','RK_SummerDress','RK_WoolenHat',
    'RK_WorkerWear','RK_RoyalRobe','RK_GaurdenUniform','RK_GovernorUniform'])
# C2 头部防御(留在C2)
add_group('Ratkin_Apparel_C2', ['RK_BulletProofHelmet','RK_Circlet','RK_CaesarCrownA','RK_CaesarCrownB',
    'RK_EarCostume','RK_beileimao','RK_PaladinCoronet','RK_PaladinHood'])
# C2b 军用大衣
add_group('Ratkin_Apparel_C2b', ['RK_fengye','RK_fengyewaitao','RK_junfu','RK_OutdoorBackpack','RK_yingwu',
    'RK_zsyg','RK_PaladinArmor'])
# 军服分区: 按产品国家前缀
def add_mil(zone, prefixes):
    for r in recipes.values():
        p = r['product']
        if not p: continue
        m = re.match(r'RK_([A-Z]{2})_', p)
        if m and m.group(1) in prefixes:
            target[p] = zone
add_mil('Ratkin_Apparel_C1M1', ['AF','EU','IR','KB'])
add_mil('Ratkin_Apparel_C1M2', ['KO','KP','LR','LS'])
add_mil('Ratkin_Apparel_C1M3', ['MR','NK','NR','PK','RR'])
add_mil('Ratkin_Apparel_C1M4', ['SA','SD','VK','WR','YE'])

# 打印迁移计划
print("=== 配方迁移计划 (recipe defName | 当前research | 目标节点 | 产品 | label) ===")
data = []
for dn, r in recipes.items():
    p = r['product']
    if p in target and target[p] != r['research']:
        data.append((dn, r['research'], target[p], p, prod_label.get(p,'?')))
# 排序by目标
data.sort(key=lambda x:(x[2],x[0]))
for dn, cur, tgt, p, lab in data:
    flag = '' if cur else ' [NO-RES!]'
    print(f"{dn}\t{cur}\t->\t{tgt}\t| {p} {lab}{flag}")
print(f"\n迁移配方数: {len(data)}")
# 统计每个目标
cc = collections.Counter([t[2] for t in data])
for k,v in cc.items(): print(f"  {k}: {v}条")