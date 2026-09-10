# -*- coding: utf-8 -*-
"""从英文 DefInjected 生成中文汉化层(核心 def 人工翻译术语表 + 规则翻译)"""
import os, re, shutil

SRC = 'HSK/Languages/English/DefInjected'
DST = 'HSK/Languages/ChineseSimplified (简体中文)/DefInjected'

# ===== 核心词表(人工翻译) =====
GLOSSARY = {
    # 衣物
    'Miho': '美狐',
    'Shirt': '衬衫', 'Hoodie': '连帽衫', 'Dress': '长裙', 'Clothes': '衣物', 'Clothe': '衣物',
    'Regular': '普通', 'Simple': '简约', 'Short': '短款', 'Common': '日常',
    'Underwear': '内衣', 'Tribal': '部落', 'Striped': '条纹', 'Black': '黑色',
    'Protective': '防护', 'Seal Talisman': '封印护符', 'Eltex': '埃尔泰克斯',
    'Tribe': '部落', 'Armor': '护甲', 'Maid': '女仆', 'French': '法式',
    'Katyusha': '卡秋莎', 'Beanie': '针织帽', 'Hat': '帽子', 'Mask': '面具',
    'Sorceress': '女巫', 'Officer': '军官', 'Cap': '军帽', 'Tactical': '战术',
    'Trooper': '士兵', 'Helmet': '头盔', 'Ears': '耳罩', 'Camo': '迷彩',
    'Medic': '医疗', 'Worker': '工人', 'Hard Hat': '安全帽', 'Elite': '精英',
    'Pariah': '帕里亚', 'Battle': '战斗', 'Assassin': '刺客', 'Asura': '阿修罗',
    'Synapse': '突触', 'Activator': '激活器', 'Middle': '中层', 'Tan': '棕褐色',
    'Vest': '背心', 'Exoskeleton': '外骨骼', 'Heavy': '重型', 'Mechanitor': '机械师',
    'OnSkin': '贴身', 'Captain': '队长', 'Apparel': '衣物', 'inner': '内层',
    'Outfit': '套装', 'Arcane': '奥术', 'Shield': '护盾', 'Security': '安保',
    'PMC': '佣兵', 'Suit': '作战服', 'Fighting': '战斗', 'Hunter': '猎人',
    'Hunting': '狩猎', 'Santa': '圣诞', 'Costume': '服装', 'Winter': '冬季',
    'Poncho': '斗篷', 'Padded': '加衬', 'Parka': '派克大衣', 'Military': '军用',
    'Coat': '大衣', 'Cloak': '斗篷', 'Magnifier': '放大镜', 'Aim-Assist': '瞄准辅助',
    'Computer': '电脑', 'Glasses': '眼镜', 'Visor': '面罩', 'Gas Mask': '防毒面具',
    'Missile Launcher': '导弹发射器', 'Ballistic': '弹道', 'Seasonal': '季节',
    'Nomadic': '游牧', 'Celestial': '天界', 'Militia': '民兵', 'Ornated': '华丽',
    'Shell': '外壳', 'Special': '特殊', 'Under': '内衬', 'Powered': '动力',
    # 武器
    'Weapon': '武器', 'Power': '动力', 'Claw': '爪', 'Hammer': '锤',
    'Hwando': '环刀', 'Nodachi': '野太刀', 'Sapper': '工兵', 'Plasma': '等离子',
    'Rod': '法杖', 'Axe': '斧', 'Battle Axe': '战斧', 'Rifle': '步枪',
    'Submachinegun': '冲锋枪', 'Nail Gun': '钉枪', 'Infantry': '步兵',
    'Grenade': '榴弹', 'Launcher': '发射器', 'LMG': '轻机枪', 'Old Fashioned': '老式',
    'Shotgun': '霰弹枪', 'Hunting Rifle': '猎枪', 'Heavy Shotgun': '重型霰弹枪',
    'Sniper': '狙击', 'Medium': '中型', 'Mechanite': '机械体', 'Accelerator': '加速器',
    'Hadron': '强子', 'Collider': '对撞机', 'Recoilless': '无后坐力', 'Artillery': '火炮',
    'Pistol': '手枪', 'SMG': '冲锋枪',
    # 建筑
    'Tatami': '榻榻米', 'General': '综合', 'Workbench': '工作台', 'Crafting Spot': '制作点',
    'Work Assistant': '工作辅助', 'AI': '人工智能', 'Mech': '机械', 'Factory': '工厂',
    'Shaft Furnace': '高炉', 'Celestial Forge': '天界熔炉', 'Brazier': '火盆',
    'Foxfire': '狐火', 'Turret': '炮塔', 'Machine Gun': '机枪', 'HMG': '重机枪',
    'Medium-MAT': '中型反机甲导弹', 'Missile': '导弹', 'Guided': '制导', 'Incendiary': '燃烧',
    'Plasma Missile': '等离子导弹', 'Infantry Gun': '步兵炮', 'Autocannon': '自动炮',
    # 材料
    'Ceramic': '陶瓷', 'Dough': '面团', 'Military Grade': '军用级', 'Celestial Scale': '天界鳞',
    'Exotic matter': '异质物质', 'Large Mech Core': '大型机械核心', 'Yubu Sushi': '稻荷寿司',
    'Tech Documentation': '科技文档', 'Heat Ceramic': '耐热陶瓷', 'Ebonsilk': '乌木丝绸',
    'Ebony': '乌木', 'Mulberry': '桑葚', 'tree': '树', 'SilkCloth': '丝绸',
    # 机械体
    'Ornithopter': '扑翼机', 'agriculture': '农业', 'Recycling Bot': '回收机器人',
    'Bulgasal': '布尔加萨尔', 'siege type': '攻城型', 'grenade type': '榴弹型',
    'Multi-Purpose': '多用途', 'Cannon': '加农炮', 'Howitzer': '榴弹炮',
    'Coaxial': '同轴', 'Balbari': '巴尔巴里', 'Sapsal': '萨普萨尔', 'support type': '支援型',
    'Cataphract': '铁骑', 'Thermobaric': '温压', 'Rocket': '火箭', 'Battery': '电池',
    'Cluster': '集束', 'Clibanar': '克利巴纳', 'Miclic': '清扫者', 'Wasteland': '废土',
    'Reclamation': '回收', 'Heavy': '重型', 'Rocket Core': '火箭核心',
    'Portal': '传送门', 'Bladefly': '刃蝇', 'Hawk': '鹰', 'Helper Fox': '辅助狐',
    'Thrumbo': '碎颅兽', 'Arcane': '奥术', 'Mechanite Crystals': '机械体晶体',
    'Fox Shield': '狐盾', 'Sun Lamp': '太阳灯', 'Glowing Metal': '发光金属',
    'Unstable': '不稳定', 'Power Cell': '电池', 'Fox': '狐',
    # 研究
    'Basic': '基础', 'Heavy Industry': '重工业', 'Hacking': '黑客', 'Large': '大型',
    'Core': '核心', 'Harmonic': '谐波', 'Object': '物体', 'Reduction': '还原',
    'Theory': '理论', 'Archotech': '超凡科技', 'Mass': '质量', 'Weaving': '编织',
    'Drone': '无人机', 'Combat Drone': '战斗无人机', 'Advanced': '高级', 'Support': '支援',
    'Artillery Drone': '火炮无人机', 'Assault': '突击', 'Vermin': '害虫', 'Control': '控制',
    'Combat Armor': '战斗护甲', 'Assassin Armor': '刺客护甲', 'Sorceress Armor': '女巫护甲',
    'Apparel': '衣物', 'Silk': '丝绸', 'Factory': '工厂',
    # 派系/其他
    'Faction': '派系', 'Story': '故事', 'Player': '玩家', 'Pirate': '海盗',
    'Desert': '沙漠', 'Supremacist': '霸权', 'Colony': '殖民地',
    'Backstory': '背景故事', 'Scenario': '剧本', 'Storyteller': '故事叙述者',
    'Race': '种族', 'Gene': '基因', 'Xenotype': '异种人', 'Hediff': '健康状态',
    'Thought': '想法', 'Ability': '能力', 'Job': '工作', 'Meme': '模因',
}

def translate_label(en):
    """规则翻译: 词表逐词替换"""
    # 保持原顺序,按最长词优先
    s = en
    for word in sorted(GLOSSARY, key=len, reverse=True):
        if word.lower() in s.lower():
            s = re.sub(re.escape(word), GLOSSARY[word], s, flags=re.I)
    # 清理
    s = s.replace('  ', ' ').strip()
    return s

def translate_desc(en):
    # 描述做轻量翻译(短语替换)
    s = en
    for word in sorted(GLOSSARY, key=len, reverse=True):
        if word.lower() in s.lower():
            s = re.sub(re.escape(word), GLOSSARY[word], s, flags=re.I)
    return s

# 需要翻译的 def 类型(核心可见内容)
CORE_TYPES = {
    'ThingDef', 'ResearchProjectDef', 'ResearchTabDef', 'RecipeDef', 'ThingCategoryDef',
    'TerrainDef', 'DesignationCategoryDef', 'FactionDefs', 'ScenarioDef', 'PawnKindDef',
    'HediffDef', 'AbilityDef', 'DamageDef', 'XenotypeDef', 'GeneDefs', 'WorkGiverDef',
    'MemeDefs', 'StorytellerDef', 'TraderKindDef', 'AlienRace.ThingDef_AlienRace',
    'IncidentDef', 'ChemicalDef', 'NeedDef', 'ThoughtDef', 'TraitDef',
}

def esc(s):
    return s.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

count = 0
for dirpath, dirs, files in os.walk(SRC):
    dname = os.path.basename(dirpath)
    if dname not in CORE_TYPES:
        continue
    for f in files:
        if not f.endswith('.xml'): continue
        srcf = os.path.join(dirpath, f)
        t = open(srcf, encoding='utf-8-sig').read()
        # 提取所有 <Def.field>value</Def.field> 条目
        entries = re.findall(r'<([\w.]+\.(?:label|description))>([^<]*)</\1>', t)
        if not entries: continue
        out = ['<?xml version="1.0" encoding="utf-8"?>', '<LanguageData>', '']
        for key, val in entries:
            field = key.split('.')[-1]
            if field == 'label':
                zh = translate_label(val)
            else:
                zh = translate_desc(val)
            out.append(f'\t<{key}>{esc(zh)}</{key}>')
        out.append('')
        out.append('</LanguageData>')
        dstd = os.path.join(DST, dname)
        os.makedirs(dstd, exist_ok=True)
        dstf = os.path.join(dstd, f)
        open(dstf, 'w', encoding='utf-8').write('\n'.join(out))
        count += len(entries)
print(f"生成中文翻译 {count} 条")
