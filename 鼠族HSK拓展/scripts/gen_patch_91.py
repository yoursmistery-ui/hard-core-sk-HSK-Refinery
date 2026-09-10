# -*- coding: utf-8 -*-
# 生成 91_鼠族衣物科技树拆分.xml
#  v2: WR_TrenchCoat 从 M4 挪到 C2b; Synthread 材料合成从 B2A 移到 Fabrication
import re

UNIFIED = r"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Unified.xml"
raw = open(UNIFIED, 'r', encoding='utf-16').read().encode('utf-8')

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
    recipes[dn] = {'product': prod, 'research': res}

# ====== 方案定义 ======
nodes = [
    # B1b: 头脸配饰 10件
    {
        'defName': 'Ratkin_Apparel_B1b',
        'label': '鼠族衣物I·头脸配饰',
        'X': '5.50', 'Y': '44.00',
        'cost': '300',
        'prereq': ['Ratkin_Apparel_B1'],
        'products': ['RK_Bandage','RK_EyeMask','RK_Respirator','RK_WeijingA','RK_BigBeard',
                     'RK_MedalHonor','RK_YaoBao','RK_Kuabao','RK_WorkApron','RK_LargePipe'],
    },
    # B2B: 冬暖装 8件
    {
        'defName': 'Ratkin_Apparel_B2B',
        'label': '鼠族衣物II·冬暖装',
        'X': '6.50', 'Y': '44.00',
        'cost': '400',
        'prereq': ['Ratkin_Apparel_B2A'],
        'products': ['RK_WhiteCoat','RK_WhiteCloak','baonuanneiyi','RK_WinterRobe','xiaokb',
                     'yiliaob','RK_ExplorerWear','RK_ExplorerHat'],
    },
    # B2C: 职业装备 10件
    {
        'defName': 'Ratkin_Apparel_B2C',
        'label': '鼠族衣物II·职业装备',
        'X': '7.50', 'Y': '44.00',
        'cost': '450',
        'prereq': ['Ratkin_Apparel_B2B'],
        'products': ['RK_ResearchGlasses','RK_GlassesB','RK_GlassesR','RK_Monocle','RK_ChefHat',
                     'RK_ChefSuit','RK_MaidA','RK_MaidB','RK_GuardUniform','RK_GuardHat'],
    },
    # B2D: 软甲防御 5件
    {
        'defName': 'Ratkin_Apparel_B2D',
        'label': '鼠族衣物II·软甲防御',
        'X': '8.50', 'Y': '44.00',
        'cost': '500',
        'prereq': ['Ratkin_Apparel_B2C'],
        'products': ['RK_Gambeson','RK_GambesonB','RK_OuterPlate','RK_Vest','RK_Rainproof'],
    },
    # C1M1: 军服A区 10件 (AF/EU/IR/KB)
    {
        'defName': 'Ratkin_Apparel_C1M1',
        'label': '鼠族军服·A区',
        'X': '9.30', 'Y': '44.00',
        'cost': '800',
        'prereq': ['Ratkin_Apparel_C1'],
        'products': ['MIL:AF','MIL:EU','MIL:IR','MIL:KB'],
    },
    # C1M2: 军服B区 9件 (KO/KP/LR/LS)
    {
        'defName': 'Ratkin_Apparel_C1M2',
        'label': '鼠族军服·B区',
        'X': '10.30', 'Y': '44.00',
        'cost': '800',
        'prereq': ['Ratkin_Apparel_C1M1'],
        'products': ['MIL:KO','MIL:KP','MIL:LR','MIL:LS'],
    },
    # C1M3: 军服C区 10件 (MR/NK/NR/PK/RR)
    {
        'defName': 'Ratkin_Apparel_C1M3',
        'label': '鼠族军服·C区',
        'X': '11.30', 'Y': '44.00',
        'cost': '800',
        'prereq': ['Ratkin_Apparel_C1M2'],
        'products': ['MIL:MR','MIL:NK','MIL:NR','MIL:PK','MIL:RR'],
    },
    # C1M4: 军服D区 10件 (SA/SD/VK/WR(2件)/YE) — WR_TrenchCoat 挪去 C2b
    {
        'defName': 'Ratkin_Apparel_C1M4',
        'label': '鼠族军服·D区',
        'X': '12.30', 'Y': '44.00',
        'cost': '800',
        'prereq': ['Ratkin_Apparel_C1M3'],
        'products': ['MIL:SA','MIL:SD','MIL:VK','MIL:WR','MIL:YE'],
        'exclude_products': ['RK_WR_TrenchCoat'],
    },
    # C2b: 军用大衣 8件 (原7件 + WR_TrenchCoat)
    {
        'defName': 'Ratkin_Apparel_C2b',
        'label': '鼠族装备·军用大衣',
        'X': '10.80', 'Y': '45.00',
        'cost': '1000',
        'prereq': ['Ratkin_Apparel_C2'],
        'products': ['RK_OutdoorBackpack','fengye','fengyewaitao','junfu','yingwu','zsyg',
                     'RA_PaladinArmor','RK_WR_TrenchCoat'],
    },
    # 元迁移: Synthread 材料合成从 B2A 移到 Fabrication
    {
        'defName': 'Fabrication',
        'label': '(材料配方迁移)',
        'is_meta': True,
        'products': ['Synthread'],
    },
]

def expand_mil(prefixes, src_research='Ratkin_Apparel_C1', exclude=None):
    res = []
    exclude = exclude or []
    for r in recipes.values():
        p = r['product']
        if not p or p in exclude: continue
        m = re.match(r'RK_([A-Z]{2})_', p)
        if m and m.group(1) in prefixes and r['research'] == src_research:
            res.append(p)
    res.sort()
    return res

def find_recipes_by_product(prod):
    out = []
    for dn, r in recipes.items():
        if r['product'] == prod:
            out.append(dn)
    out.sort()
    return out

# ====== 生成 XML ======
out = []
out.append('<?xml version="1.0" encoding="utf-8"?>')
out.append('<Patch>')
out.append('')
out.append('  <!-- ============================================================ -->')
out.append('  <!--  91_鼠族衣物科技树拆分.xml                                  -->')
out.append('  <!--  把 B1/B2A/C1/C2 超载节点按品类拆分出 9 个独立科技节点      -->')
out.append('  <!--  每节点 5-10 件衣物/装备, 统一按品类归类管理               -->')
out.append('  <!-- ============================================================ -->')
out.append('')

# 1. 新增研究节点定义
out.append('  <!-- 1. 新增研究节点定义 -->')
out.append('  <Operation Class="PatchOperationAdd">')
out.append('    <xpath>/Defs</xpath>')
out.append('    <value>')

for n in nodes:
    if n.get('is_meta'):
        continue
    out.append('      <ResearchProjectDef>')
    out.append(f'        <defName>{n["defName"]}</defName>')
    out.append(f'        <label>{n["label"]}</label>')
    out.append('        <description>鼠族特色服饰。</description>')
    out.append('        <tab>Apparel_SK</tab>')
    out.append(f'        <researchViewX>{n["X"]}</researchViewX>')
    out.append(f'        <researchViewY>{n["Y"]}</researchViewY>')
    out.append(f'        <baseCost>{n["cost"]}</baseCost>')
    out.append('        <prerequisites>')
    for p in n['prereq']:
        out.append(f'          <li>{p}</li>')
    out.append('        </prerequisites>')
    out.append('        <techLevel>Industrial</techLevel>')
    out.append('      </ResearchProjectDef>')
    out.append('')

out.append('    </value>')
out.append('  </Operation>')
out.append('')

# 2. 重命名主管线节点 label
out.append('  <!-- 2. 重命名被拆分的主管线节点 label, 体现拆分后语义 -->')
rename_map = [
    ('Ratkin_Apparel_B1', '鼠族衣物I·日常衣装'),
    ('Ratkin_Apparel_B2A', '鼠族衣物II·礼服装'),
    ('Ratkin_Apparel_C1', '鼠族衣物III·民用日常'),
    ('Ratkin_Apparel_C2', '鼠族装备·头部防御'),
]
for dn, lab in rename_map:
    out.append('  <Operation Class="PatchOperationReplace">')
    out.append(f'    <xpath>/Defs/ResearchProjectDef[defName="{dn}"]/label</xpath>')
    out.append('    <value>')
    out.append(f'      <label>{lab}</label>')
    out.append('    </value>')
    out.append('  </Operation>')
out.append('')

# 3. 配方研究引用迁移
out.append('  <!-- 3. 配方研究引用迁移: 把各品类配方改到新节点 -->')
mig_count = 0
for n in nodes:
    prods = []
    exclude = n.get('exclude_products', [])
    for p in n['products']:
        if p.startswith('MIL:'):
            prefix = p[4:]
            prods += expand_mil([prefix], exclude=exclude)
        else:
            prods.append(p)
    for prod in prods:
        recs = find_recipes_by_product(prod)
        for rdn in recs:
            cur = recipes.get(rdn, {}).get('research')
            if cur == n['defName']:
                continue
            out.append(f'  <Operation Class="PatchOperationReplace">')
            out.append(f'    <xpath>/Defs/RecipeDef[defName="{rdn}"]/researchPrerequisite</xpath>')
            out.append(f'    <value>')
            out.append(f'      <researchPrerequisite>{n["defName"]}</researchPrerequisite>')
            out.append(f'    </value>')
            out.append(f'  </Operation>')
            mig_count += 1

out.append('')
out.append('</Patch>')

xml = '\n'.join(out)
path = r'c:\Personal\Project\ratkin-patch\鼠族HSK拓展\Patches\91_鼠族衣物科技树拆分.xml'
with open(path, 'w', encoding='utf-8-sig') as f:
    f.write(xml)

print(f"生成补丁: {path}")
print(f"迁移配方操作数: {mig_count}")
node_count = sum(1 for n in nodes if not n.get('is_meta'))
print(f"新增节点数: {node_count}")