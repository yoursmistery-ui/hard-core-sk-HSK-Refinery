# -*- coding: utf-8 -*-
# v2: 军服分区只迁移当前已在 Ratkin_Apparel_C1 的配方（制服/头盔/护甲），
#     武器节点(Gunsmithing等)的来源配方一律不动。
import re, collections

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
raw = open(UNIFIED, 'r', encoding='utf-16').read().encode('utf-8')

prod_label = {}
for m in re.finditer(rb'<ThingDef\b[^>]*>(.*?)</ThingDef>', raw, flags=re.S):
    b = m.group(1)
    dn = re.search(rb'<defName>([^<]+)</defName>', b)
    lab = re.search(rb'<label>([^<]+)</label>', b)
    if dn and lab: prod_label[dn.group(1).decode()] = lab.group(1).decode()

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

# ======= 目标分组 v2 =======
target = {}
def assign(node, prods):
    for p in prods: target[p] = node

# B1b 头脸配饰 (从 B1 迁移)
assign('Ratkin_Apparel_B1b', ['RK_Bandage','RK_EyeMask','RK_Respirator','RK_WeijingA','RK_BigBeard',
    'RK_MedalHonor','RK_YaoBao','RK_Kuabao','RK_WorkApron','RK_LargePipe'])
# B2B 冬暖装 (从 B2A)
assign('Ratkin_Apparel_B2B', ['RK_WhiteCoat','RK_WhiteCloak','baonuanneiyi','RK_WinterRobe','xiaokb',
    'yiliaob','RK_ExplorerWear','RK_ExplorerHat'])
# B2C 职业装备 (从 B2A)
assign('Ratkin_Apparel_B2C', ['RK_ResearchGlasses','RK_GlassesB','RK_GlassesR','RK_Monocle','RK_ChefHat',
    'RK_ChefSuit','RK_MaidA','RK_MaidB','RK_GuardUniform','RK_GuardHat'])
# B2D 软甲防御 (从 B2A)
assign('Ratkin_Apparel_B2D', ['RK_Gambeson','RK_GambesonB','RK_OuterPlate','RK_Vest','RK_Rainproof'])
# C2b 军用大衣 + 圣骑士 (从 C2)
assign('Ratkin_Apparel_C2b', ['RK_OutdoorBackpack','fengye','fengyewaitao','junfu','yingwu','zsyg','RA_PaladinArmor'])

# 军服分区 v2: 仅当前挂在 C1 的军服进入分区
MIL = {'Ratkin_Apparel_C1M1':['AF','EU','IR','KB'],
       'Ratkin_Apparel_C1M2':['KO','KP','LR','LS'],
       'Ratkin_Apparel_C1M3':['MR','NK','NR','PK','RR'],
       'Ratkin_Apparel_C1M4':['SA','SD','VK','WR','YE']}
for zone,pre in MIL.items():
    for r in recipes.values():
        p = r['product']
        if not p: continue
        m = re.match(r'RK_([A-Z]{2})_', p)
        if m and m.group(1) in pre and r['research'] == 'Ratkin_Apparel_C1':
            target[p] = zone

# ======= 输出 =======
print("=== 迁移计划 v2 (仅衣物类; 武器节点来源已排除) ===")
data=[]
for dn,r in recipes.items():
    tgt=target.get(r['product'])
    if tgt and tgt!=r['research']:
        data.append((dn,r['research'],tgt,r['product'],prod_label.get(r['product'],'?')))
data.sort(key=lambda x:(x[2],x[0]))
for dn,cur,tgt,p,lab in data:
    print(f"{dn}\t{cur}\t->\t{tgt}\t| {p} {lab}")
print(f"\n实际迁移配方数: {len(data)}")
cc=collections.Counter(x[2] for x in data)
for k,v in cc.items(): print(f"  {k}: {v}")

# ======= 对照检查: 原计划中被武器捕获而应排除的 =======
print("\n=== 已排除的武器配方(当前武器节点，不入衣物账号) ===")
excluded=[]
for dn,r in recipes.items():
    p=r['product']
    if not p or re.match(r'RK_[A-Z]{2}_',p): 
        if r['research'] in ('Gunsmithing','BlowbackOperation') and any(p.startswith('RK_'+x+'_') for x in ['AF','EU','IR','KB','KO','KP','LR','LS','MR','NK','NR','PK','RR','SA','SD','VK','WR','YE']):
            excluded.append((dn,p,r['research'],prod_label.get(p,'?')))
for dn,p,res,lab in excluded:
    print(f"{dn}\t{res}\t| {p} {lab}")
print(f"排除武器配方数: {len(excluded)}")