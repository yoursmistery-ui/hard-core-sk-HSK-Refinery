# 金鼠族 HSK版本 — 适配说明

> 整合对象: `[OA]Ratkin Faction: Oberonia aurea`(金鸢尾兰鼠族派系, workshop 3159926804)
> 适配环境: RimWorld 1.6 + Core_SK(HSK)+ RatkinRaceHSK + Combat Extended 16.7 + 全 DLC
> 最后更新: 2026-08-20

## 1. 这个 mod 是什么

将金鸢尾兰鼠族派系(金鼠族)整体打包为**单个本地 mod**,开箱即用:
- 1 个新鼠族派系(特殊袭击方式 + 声望系统)
- 2 种新作物、25 种枪械、9 把近战武器、29 件配件/头饰、34 件服装/外套、23 种建筑
- 27 项科技、2 个文化模因、3 个基因、230 个背景故事、2 位故事叙述者

## 2. 自包含(框架处理)

原版金鼠族要求单独订阅 `[OA]Oberonia Aurea Framework`(workshop 3713589764)。
本整合版把框架代码与定义**内嵌**进 mod(1.6/Frame_Temp):

- 未安装 Framework mod → 自动使用内嵌框架(loadFolders 条件加载)
- 已安装 Framework mod → 自动复用外部框架,不重复加载,无冲突

同理,未安装 Weather Apparel Framework 时使用内嵌兼容 dll。

## 3. 科技树适配(核验结论:全兼容)

金鼠族 28 个研究节点保留在鼠族研究 tab(`RK_ResearchTab_Default`,由 RatkinRaceHSK 定义)。
逐一核验了所有前置节点在 HSK 环境的存在性:

| 前置(原版名) | HSK 环境 | 说明 |
|---|---|---|
| Electricity / Batteries | ✅ 存在 | Core_SK 保留 |
| Fabrication | ✅ 存在 | Core_SK 定义 |
| MicroelectronicsBasics | ✅ 存在 | Core_SK 定义 |
| HeavyTurrets | ✅ 存在 | 原版 Core 保留 |
| BlowbackOperation / GasOperation | ✅ 存在 | 原版 Core 保留(HSK 自身武器研究也引用) |
| NobleApparel / RoyalApparel | ✅ 存在 | Core_SK 定义 |
| Hydroponics | ✅ 存在 | Core_SK 定义 |

### 3.1 科技树挂钩(改动:01_科技树挂钩.xml,2026-08-20 追加)

为满足"一环套一环、每节点互相依赖"的游玩需求,对金鼠族与鼠族科技做了双向挂钩:

**① 修复鼠族科技断链**(原:鼠族衣物 II→III、盔甲 I/II→III 之间无依赖,可跳研):
- `Ratkin_Apparel_C1`(鼠族衣物 III): 前置 `Fabrication` → **+ Ratkin_Apparel_B2A**(必须先研鼠族衣物 II)
- `Ratkin_Armor_D1`(鼠族盔甲 III): 前置 `Apparel_D1` → **+ Ratkin_Armor_B2**(必须先研鼠族盔甲 II)

**② 金鼠族 28 节点移入 Apparel_SK tab**(与鼠族科技同屏,不再用独立 RK tab),坐标 y=44 工程 / 45 种植 / 46 衣物 / 47 武器,逐节点挂靠鼠族科技:

| 金鼠族节点 | 新增鼠族前置 |
|---|---|
| OA_RK_Oberonia_Aurea_Research(起点) | Ratkin_Apparel_B1(鼠族衣物 I) |
| OA_RK_Cloth_Processing(缝纫设施) | Ratkin_Apparel_B2A(鼠族衣物 II) |
| OA_RK_Apparel_A(基础衣物) | Ratkin_Apparel_B2A |
| OA_RK_Apparel_B(宫廷服) | Ratkin_Apparel_Uniform(鼠族制服) |
| OA_RK_Apparel_C(闪电战斗服) | Ratkin_Apparel_Uniform |
| OA_RK_Apparel_E(技术制服) | Ratkin_Apparel_Uniform |
| OA_RK_Apparel_G(全防护突击装甲) | Ratkin_Armor_D1(鼠族盔甲 III) |
| OA_RK_Apparel_F(重型突击装甲) | Ratkin_Armor_D3(鼠族盔甲 IV) |
| OA_RK_Weapon_A(古典武器) | Ratkin_Apparel_C1(鼠族衣物 III) |

由此形成完整链条: **鼠族衣物 I → 金鸢尾兰研究 → 金鼠族工程 → 金鼠族衣物 → 金鼠族装甲(挂鼠族盔甲线)**,以及 **鼠族衣物 I/II/III → 金鼠族武器线**,武器线内部 A→B→C→D/G→H/E→F 本身即链式。所有节点均从鼠族科技出发,无孤岛、无跳研。

### 3.2 配方科技链(改动:02_配方科技链.xml,2026-08-20 追加)

金鼠族**武器/衣物/头饰/配件/装甲配方**的研究解锁(recipeMaker researchPrerequisite)从金鼠族自研节点改挂**鼠族/HSK 科技链**,与「鼠族HSK拓展」完全一致的玩法:研究鼠族科技直接解锁金鼠族装备,不再需要先点完金鼠族研究线(尤其绕开带科技印痕的节点)。共改 **116 处**(105 主配方 + 11 CE 弹药):

| 金鼠族装备类型 | 解锁研究 |
|---|---|
| 基础衣物/头饰/旅行者装(20 件) | Ratkin_Apparel_B2A(鼠族衣物 II) |
| 宫廷/贵族衣物、高级头饰(24 件) | Ratkin_Apparel_C1(鼠族衣物 III) |
| 职业/军用制服、战斗配件(15 件) | Ratkin_Apparel_Uniform(鼠族制服) |
| 科技腰带/组件/装备(15 件) | Ratkin_Apparel_C2(鼠族衣物 III 装备) |
| 突击装甲/全防护头盔(3 件) | Ratkin_Armor_D1(鼠族盔甲 III) |
| 重型突击装甲(2 件) | Ratkin_Armor_D3(鼠族盔甲 IV) |
| 古典步枪(1) | Rifles_C1 |
| 制式/原型闪电武器(8) | Charge_weapons_D |
| 先进/狙击/不稳定充能(7) | Charge_weapons_E |
| 反重甲/重型突击(6) | Heavy_C3 |
| 霰弹枪(2) | Shotgun_C2 |
| 冲锋枪(1) | SMG_C2 |
| 登山杖近战(9) | Melee_C2 / Melee_D1 / Melee_Ultra |
| 炮塔弹药(1) | Heavy_turrets_C2 |
| CE 弹药 11 条 | 随对应武器科技(Charge_weapons_D/E、Heavy_C3、Shotgun_C2、Rifles_C1) |

**保留走金鼠族研究线**(短链、无印痕,已挂鼠族科技): 茶/花饼/药物(12 条 RecipeDef)、纤维合成。
金鼠族 28 个研究节点保留但只承担**建筑解锁**职责(恒温培育、火箭阵列、EMP、凝光暖辉、缝纫设施等),装备不再依赖它们——想专心玩装备的玩家可以完全无视金鼠族研究线。

## 4. 材料适配(改动:00_材料适配.xml)

按 HSK 冶炼惯例,固定配方成本中的贵金属改为合金锭:

| 物品 | 原配方 | 新配方 |
|---|---|---|
| OA_RK_Hat_B / C / E(头饰) | Silver 13/11/15 | SilverBar 13/11/15 |
| OA_RK_New_Hat_A | Silver 3 | SilverBar 3 |
| OA_RK_Professional_Uniforms_B(祭服) | Gold 6 | GoldBar 6 |
| OA_RK_Mountain_Stick(登山杖) | Silver 90 | SilverBar 90 |
| OA_RK_Floor(自清洁地毯) | Silver 15 | SilverBar 15 |

其余材料经逐项核验与 HSK 命名一致,**无需改动**:
- 铀(Uranium)、钢(Steel)、钛铁合金(Plasteel)→ HSK 保留原名(高科合金)
- 零部件: ComponentIndustrial / ComponentSpacer → HSK 通过 DefOverwrite 保留
- 合成纤维(Synthread)、布(Cloth)、化工燃料(Chemfuel)、恶魔纤维(Hyperweave)、医药 → 均存在
- 衣物材质类 Fabric / Leathery → HSK 保留;弹药 FSX → CE 自带

## 5. 工作台适配(核验结论:全兼容)

| 配方类型 | 工作台 | HSK 环境 |
|---|---|---|
| 枪械(原型/制式) | TableMachining / FabricationBench | ✅ HSK DefOverwrite 保留(加工台/电动装配台) |
| 衣物 | RK_ElectricTailoringBench / RK_HandTailoringBench / OA_RK_Tailor | ✅ 鼠族缝纫台 |
| 药品 | DrugLab | ✅ |
| 食物/茶 | ElectricStove / FueledStove | ✅ |
| 纤维合成 | BiofuelRefinery | ✅ |

金鼠族衣物基类直接挂在鼠族缝纫台体系上,与 HSK 的鼠族裁缝体系天然一致。

## 6. CE 适配(改动:ZZ_CE武器修复.xml / ZZ_CE防具修复.xml)

**原版 CE 与 HSK CE 的前置差异**: 金鼠族的官方 CE 加强补丁 `[OA]Ratkin Oberonia aurea CE Patch+`
依赖 D-FunctionalAmmunition Library(workshop 3460442482),而 HSK 环境**未安装**该库。

处理方式:
1. **核验**: CE Patch+ 的 XML 实际全部为标准 CE 结构(CombatExtended.AmmoSetDef/AmmoDef/
   ProjectilePropertiesCE),DwS 库仅作为声明的依赖存在;但按其"仅作参考"的要求,不整包并入。
2. **移植 bugfix**(标准 CE,不依赖弹药库):
   - `ZZ_CE武器修复.xml`: 反器材步枪标准射击命令、AT 武器射界修复、霰弹枪 AI 标签、
     4 座炮塔成本修正(去除 CE 下无意义的化工燃料/弹药)、EMP 火箭弹道修正等
   - `ZZ_CE防具修复.xml`: 重型头盔穿着音效/层位/夜视、电台包 CE 扩展、
     负重(CarryWeight/CarryBulk)全面修正
3. **排除**(依赖 D-FA 或属作者个人平衡,不并入):
   - 弹药重构(OARatkin_Ammo_*): 引入大量新弹药变体并重接枪械 ammoSet
   - 武器/防具数值再平衡(OARatkin_Weapon_Rebalance / Apparel_Rebalance)
   - 商人弹药补丁(Trade_Patch)
4. **弹药复用(2026-08-20 重大改造)**: 按用户要求**删除金鼠族全部 14 套自创贫化铀弹药**
   (7.92x64mm/12.7x108mm/20x169mm/28x183mm/不稳定充能等,CE_AmmoDefs 目录仅保留
   钩索投射物 GouSuo.xml),**不再新增任何弹药种类**,全部改挂 HSK/CE 原有弹药:

   | 金鼠族武器 | 弹药(CE 标准) |
   |---|---|
   | 古典步枪 ProtocolRifle | AmmoSet_303British(.303 英式步枪弹) |
   | 原型闪电步枪 | AmmoSet_762x39mmSoviet |
   | 制式/先进闪电步枪 | AmmoSet_556x45mmNATO |
   | 重型突击步枪 / 轻机枪 / 狙击 | AmmoSet_762x51mmNATO |
   | 闪电冲锋枪 / 技术冲锋枪 | AmmoSet_9x19mmPara |
   | 重型冲锋枪 | AmmoSet_45ACP |
   | 反器材步枪 / 重机枪 | AmmoSet_50BMG(.50) |
   | 重型反器材 | AmmoSet_145x114mm(14.5mm) |
   | 霰弹枪 A/B/C | AmmoSet_12Gauge |
   | 反坦克 OA_RK_AT | AmmoSet_RPG7Grenade |
   | 电荷狙击 / 不稳定充能 | **AmmoSet_LightbulletA/B(鼠族HSK拓展的光储能弹)** |
   | 火箭炮塔 A/CA/CB | AmmoSet_80mmRocket_direct(HE/HEAT/温压) |
   | EMP 火箭炮塔 B | AmmoSet_40x46mmGrenade(含 EMP 弹种) |

   配套: 炮塔建造成本中的弹药改用 CE 弹药(Ammo_80mmRocket_HE);场景起始弹药改用
   CE 标准弹;ZZ_CE武器修复中引用已删弹药的 4 个操作移除;02_配方科技链中 CE 弹药配方
   的 11 个研究挂载操作删除(弹药配方由 CE 注入,无需自定义研究)。
   弹药配方/生产/商人流通全部走 CE 与鼠族 mod 现有体系,零新增。

## 7. 加载顺序建议

1. Core_SK(HSK)
2. RatkinRaceHSK(NewRatkinPlus)
3. 鼠族HSK拓展(local.ratkin.clothesweapons)
4. Combat Extended
5. **金鼠族 HSK版本(本 mod)**
6. 汉化包等外围

> 注意: 请勿与 workshop 原版金鸢尾兰(OARK.RatkinFaction.OberoniaAurea)及其 CE Patch+
> 同时启用,以免 defName/补丁冲突。原版 4 个 workshop mod 中仅保留本整合版即可,
> D-FunctionalAmmunition Library 不再需要。

## 8. 目录结构

```
金鼠族 HSK版本/
├── About/            # mod 元数据(本地 packageId: local.ratkin.goldenhsk)
├── loadFolders.xml   # 1.6 加载规则(自包含框架/CE/DLC 条件加载)
├── 1.6/
│   ├── Assemblies/   # 金鼠族本体代码(OberoniaAurea_Hyacinth / OberoniaAureaYH)
│   ├── Defs/         # 金鼠族全部定义(原版 1.6 内容)
│   ├── Patches/      # 金鼠族补丁 + HSK适配/00_材料适配.xml
│   ├── Frame_Temp/   # 内嵌框架(代码+定义+补丁+多语言)
│   └── Languages/    # 中/英/日/韩
├── Mods/             # Ideology / Biotech / Odyssey / CE / 天气服装框架(条件加载)
├── Sounds/           # 武器音效
└── Textures/         # 贴图
```

## 9. RatkinRaceHSK 基因体系更新适配(2026-08-20)

RatkinRaceHSK 本次更新内容(Ratkin_XenotypeGeneration_Patch.xml 基因族、
Scenario_NoBiotech_Patch 禁用、LoadFolders DLC 条件化)经逐项核验,影响与处理如下:

| 更新项 | 对金鼠族的影响 | 处理 |
|---|---|---|
| 新增 Ratkin_XenotypeGeneration_Patch(鼠族基因族: 小体型/灵巧/视力差/低痛阈/采矿园艺天赋/外观基因) | `SK.PatchOperationReplaceExtended` 为**深度合并语义**——raceRestriction 的 apparelList/weaponList 保留,金鼠族装备挂载不受影响;鼠族基因自动作用于金鼠族小人(同种族) | 无需改动 |
| 同上补丁设置 `onlyHaveRaceRestrictedGenesEndo` + 12 基因白名单 | 金鼠族 Ratkin_OA 异种型(inheritable)的 13 个特色基因(免疫弱/灵能迟钝/行动缓慢/乐观/低温适应/低睡眠/发色等)**不在白名单**,会被 endogene 限制过滤 | **新增 `HSK_基因白名单.xml`**: 将 Ratkin_OA 基因追加进 whiteGeneListEndo |
| Scenario_NoBiotech_Patch → .disabled(默认禁用异种型开局) | 金鼠族场景继承自 vanilla ScenarioBase,不依赖 RK_Scenario_Settler | 无需改动 |
| LoadFolders.xml DLC 条件加载 | 金鼠族整合版独立自包含,不受鼠族 loadFolders 影响 | 无需改动 |
| HSK 环境 raceRestriction 无 whiteXenotypeList(被 HSK_Generated 整体替换) | 金鼠族 Biotech_Race_Patch 直接 Add whiteXenotypeList 会**报错** | **改写 Biotech_Race_Patch.xml** 为条件化: 有 whiteXenotypeList(原版)→加白名单;无(HSK)→追加 xenotypeList |

> 注: 若 HAR 对预设异种型的 endogene 过滤实际不生效(仅影响生成时的基因组合),
> 白名单追加补丁依然无害(纯白名单扩展),可放心保留。

## 10. 派系武器预算修复(2026-08-20,03_派系武器预算.xml)

CE 下武器价格上升(含弹药成本),金鼠族 8 个 PawnKindDef 的 weaponMoney 下限
买不起同 weaponTags 最便宜的武器,报 `Cheapest weapon ... costs X but weaponMoney
min is Y` config error,小人可能空手。将下限提高到日志实测最便宜武器价之上(留余量):

| PawnKind | 原预算下限 | 新预算下限(第一档) | 最终下限(第二档,再提高) | (最便宜武器价) |
|---|---|---|---|---|
| OA_RK_Traveller(旅行者) | 220 | 400 | **650~1400** | 380 |
| OA_RK_Court_Member_D(宫廷成员D) | 500 | 1100 | **1600~2200** | 1085 |
| OA_RK_Assault_B/C(突击B/C) | 1400 | 1600 | **2300~5000** | 1550 |
| OA_RK_Assault_D(突击D) | 2200 | 3000 | **4000~6000** | 2995 |
| OA_RK_Assault_E(突击E) | 2600 | 2700 | **4000~22000** | 2615 |
| OA_RK_Guard_Member(守卫) | 1000 | 1100 | **1600~2200** | 1085 |
| OA_RK_Guard_Captain(守卫队长) | 1000 | 1100 | **2200~7000** | 1085 |

下限为最便宜武器价的 1.5~1.8 倍(用户要求再提高一档,2026-08-20 二次调整)。

## 11. Universal Fermenter 适配(2026-08-20,04_UF发酵器适配.xml)

HSK 的 BiofuelRefinery(生物燃料精炼器)已被 Universal Fermenter 扩展为
ThingDef_UF,挂其上的 RecipeDef 必须声明 `Class="UniversalFermenterSK.RecipeDef_UF"`,
否则 UF_Utility:CheckForErrors 报 "should have Class=..." 错误。
金鼠族唯一挂 BiofuelRefinery 的配方 = 纤维合成(OA_RK_Make_Hecheng,75 布 + 硝普胺
→ 60 合成纤维),用 PatchOperationAttributeSet 补上 Class 属性(与 CSK 配方范例一致)。

## 12. 家具与生产设施工业线适配(2026-08-26,09_家具生产设施适配.xml)

金鼠族家具/生产设施的解锁研究统一挂到 **HSK 工业线(Core_SK Industrial 档节点)**,
不再依赖金鼠族自研节点(OA_RK_Apparel_I)或 Spacer 档节点:

| 建筑 | 原研究 | 新研究(HSK 工业线) |
|---|---|---|
| 缝纫台 OA_RK_Tailor | OA_RK_Apparel_I(金鼠族自研) | Fabrication(服装生产 III) |
| 布料精加工 OA_RK_Cloth_Processing | OA_RK_Apparel_I | Fabrication |
| 布料回收 OA_RK_Cloth_Processing_B | OA_RK_Apparel_I | Fabrication |
| 电路调控器 OA_RK_CircuitRegulator | Generators_D1(电力工程 V,Spacer) | Electrical_engineering_C2(电气网络 II) |
| 光学柱 OA_RK_Zhuzi_New | Research_table_D2(科技 VI,Spacer) | Research_table_C1(科学技术 III) |
| 技术部地板 OA_RK_Floor_Jishu | Metal_floor_D1(金属地板 V,Spacer) | Metal_floor_C1(金属地板 III) |
| 花糕机 OA_OberoniaCakeProducter | (无门槛) | OA_RK_Oberonia_Aurea_Research_B(金鼠族传统食物) |

保留(已合理挂 HSK / 非工业线 / 非家具设施):
- 照明柱 OA_RK_Zhuzi、自清洁地毯 OA_RK_Floor → Construction_B3(2026-08-23 已适配)
- ~~基因储存箱 OAGene_OAGeneBank → Storage_B1(存储 II)~~ 已迁移至鼠族家具拓展(local.ratkin.furniture, 2026-08-27)
- 凝光暖辉 OA_RK_SkyWarmth_S → Medicine_D1(医疗线高级设施)
- 暴风雪营火/冰雪收集器 → 无门槛(篝火类/暴风雪扩展)
- 霸权旗(Ideology 文化)、EMP 发生器(空投物)、Odyssey 设施(剧情链)

目标节点 Fabrication / Electrical_engineering_C2 / Research_table_C1 / Metal_floor_C1
均由 Core_SK 定义(工业档),本 mod loadAfter skyarkhangel.HSK,加载序安全。
