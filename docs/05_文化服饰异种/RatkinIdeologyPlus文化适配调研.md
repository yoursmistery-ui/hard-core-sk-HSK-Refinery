# Ratkin Ideology+ & Ratkin Apparel Expanded 调研报告
## —— 鼠族HSK拓展 文化(Ideology)适配重做方案

> 调研日期: 2026-08-21
> 调研对象(本地 workshop 文件):
> - **3606759141 = Ratkin Ideology+**(`yonata.RatkinIdeologyPatch`,鼠族文化兼容包)
> - **3468042322 = Ratkin Apparel Expanded / RAE**(`MoManaCha.RatkinApparelExpanded`,鼠族服饰扩展,是 Ideology+ 的前置)
> 目标 mod: `local.ratkin.clothesweapons`(鼠族HSK拓展 / Core SK 适配)

---

## 一、两个 mod 是什么

### 1.1 Ratkin Apparel Expanded(前置,3468042322)
- Ratkin Apparel+ 的非官方维护版(原版 id=2634067380),作者 快乐柠萌茶 & Lancy
- 提供 **47 个 ThingDef**(46 件服饰 + 1 件钻机工具),含 Defs/Textures/汉化/CE 兼容
- 依赖 `Solaris.RatkinRaceMod`(NewRatkinPlus),HSK 环境下即 RatkinRaceHSK

### 1.2 Ratkin Ideology+(3606759141)
- 鼠族文化(Ideology)兼容包,作者 YonataAragaki
- 核心功能:**让鼠族服饰成为文化职位(领袖/专家/传道者等)的职位服饰**,且**仅鼠族文化派系可用**(避免人类领袖穿鼠族衣服)
- 附带:鼠族军阀文化定义、派系/人名命名 RulePack、VME(Vanilla Ideology Expanded - Memes)适配、RW+(Ratkin Weapons+)零食适配、CE 适配
- LoadFolders 结构(v1.6):
  - `RulePackMod/FactionRulePack`(常载): 文化定义 + 派系命名
  - `RulePackMod/PawnRuleString`(常载): 小人名字库
  - `VME`(仅 VanillaExpanded.VMemesE 激活): VME 适配
  - `RW+`(仅 bbb.ratkinweapon.morefailure 激活): 零食思想适配
  - `/`(常载): 主补丁

---

## 二、RAE 衣物清单与本地对比(未整合清单)

### 2.1 本地已整合(14 件,同名 def 已在本地 Defs,无需动)
RK_Circlet / RK_Crusader / RK_Gambeson / RK_GambesonB / RK_GuardsmanA / RK_GuardsmanB / RK_MilUniform / RK_Officer / RK_Oriental / RK_OuterPlate / RK_Rifleman / RK_Scarf / RK_Sleeves / RK_Vest

### 2.2 RAE 独有 33 件 —— 整合建议

| 分类 | defName | 中文名 | 层级 | 材质 | 建议 |
|---|---|---|---|---|---|
| **文化职位必需(8)** | RK_Hood | 鼠族兜帽 | EyeCover | 布/皮 | **必整合**(通用职位服饰) |
| | RK_MedalHonor | 鼠族荣誉勋章 | Belt | 布 | **必整合**(通用职位服饰) |
| | RK_WhiteCloak | 鼠族披风 | Shell | 布/皮 | **必整合**(通用职位服饰) |
| | RK_Respirator | 鼠族口罩 | Overhead | 布/皮 | **必整合**(医药专家) |
| | RK_WorkApron | 鼠族工作围裙 | Middle | 布/皮 | **必整合**(生产专家) |
| | RK_MinerHelmet | 鼠族矿工帽 | Overhead | 金属/木 | **必整合**(采矿专家) |
| | RK_CaesarCrownA | 鼠族桂冠(金) | Overhead | - | **必整合**(领袖专属) |
| | RK_CaesarCrownB | 鼠族桂冠(银) | Overhead | - | **必整合**(通用职位服饰) |
| **推荐整合(18)** | RK_MaidA | 服务女仆装 | OnSkin | 布/皮 | 推荐(仪式/仆人装) |
| | RK_MaidB | 侍从女仆装 | OnSkin | 布/皮 | 推荐 |
| | RK_GuardUniform | 卫兵制服 | OnSkin+Middle | 布/皮 | 推荐 |
| | RK_GuardHat | 卫兵礼帽 | Overhead | 布/皮 | 推荐 |
| | RK_GovernorUniform | 执政官制服 | OnSkin+Middle | 布/皮 | 推荐(领袖候选) |
| | RK_XiongJia | 胸甲 | Middle | 金属 | 推荐(近战/防卫) |
| | RK_Kuabao | 小挎包 | BackPack | 布/皮 | 推荐 |
| | RK_YaoBao | 小腰包 | BackPack | 布/皮 | 推荐 |
| | RK_WeijingA | 围巾 | Overhead | 布/皮 | 推荐 |
| | RK_EyeMask | 眼罩 | EyeCover | 布/皮 | 推荐 |
| | RK_GlassesR/B | 圆/方框眼镜 | EyeCover | 金属 | 推荐 |
| | RK_Monocle | 单片眼镜 | EyeCover | 金属 | 推荐 |
| | RK_BigBeard | 大胡子 | Overhead | 布/皮 | 推荐(装饰) |
| | RK_LargePipe | 长烟斗 | Overhead | - | 推荐(装饰) |
| | RK_StageCostume | 万圣节装扮 | Overhead | 布/皮 | 推荐(节日) |
| | RK_Rainproof | 雨披(带防雨思想) | Shell | 布/皮 | 推荐(带 Hediff+Thought) |
| | RK_Bandage | 绷带 | EyeCover | 布/皮 | 推荐(装饰) |
| | RK_SafetyTether | 安全绳 | Belt | - | 推荐(太空装饰) |
| **太空系列(4)** | RK_LightArmorVest | 轻型护甲背心 | Middle | - | 评估(本地已有太空甲) |
| | RK_SpaceCirclet | 太空头环 | Overhead | - | 评估 |
| | RK_Spacesuit | 太空服 | OnSkin | - | 评估 |
| | RK_SurvivalBackpack | 维生背包 | BackPack | - | 评估 |
| **玩具/工具(3)** | RK_TestLabel | 实验标签(耳标) | EyeCover | - | 可跳过(玩具) |
| | RAE_DrillingBit | 采矿钻机(武器) | - | 金属 | 可跳过(与本地工具重复) |
| | RK_NORain | 防雨 Thought(非衣物) | - | - | 随雨披一起带 |

> 结论: **文化职位必需 8 件必须整合**,否则文化适配只能像现在一样"缺兜帽/勋章/披风/口罩/围裙/矿工帽/桂冠"这些经典职位服饰。推荐 18 件补全服饰体系。太空 4 件与本地已有太空装甲(RK_Apparel_SpaceArmor)定位部分重叠,建议至少整合轻型护甲背心。玩具/工具 2-3 件可跳过。

---

## 三、Ratkin Ideology+ 文化适配实现机制(完整流程)

### 3.1 核心原理

原版 RimWorld 文化职位服饰机制:
- 每个 `PreceptDef`(职位,如 `IdeoRole_Leader`)有 `roleApparelRequirements` 列表
- 列表每个 `<li>` 是一套"着装选项",结构:
  ```xml
  <li>
    <allowedFactionCategoryTags><li>RatkinStory</li><li>RatkinPlayer</li></allowedFactionCategoryTags>
    <disallowedFactionCategoryTags>...</disallowedFactionCategoryTags>  <!-- 可选 -->
    <anyMemeRequired>...</anyMemeRequired>                            <!-- 可选,VME 迷因门控 -->
    <requirement>
      <bodyPartGroupsMatchAny><li>Torso</li><li>Neck</li>...</bodyPartGroupsMatchAny>
      <requiredDefs><li>RK_RoyalRobe</li>...</requiredDefs>
    </requirement>
  </li>
  ```
- 派系匹配:`allowedFactionCategoryTags` 决定哪些派系(categoryTag)的文化受此要求约束
- 部位匹配:`bodyPartGroupsMatchAny` + `requiredDefs` 决定该职位要求穿哪几件衣服

### 3.2 步骤 1:修复/创建派系 categoryTag(最关键前提)

鼠族作者把 `RatkinFactionBase` 的 tag 写成了 `Empire`(HSK 实测确认),玩家派系继承原版 FactionBase 是 `Player`。Ideology+ 的做法:
```xml
<!-- 玩家派系补 tag -->
<Operation Class="PatchOperationAdd">
    <xpath>/Defs/FactionDef[@Name="RK_PlayerFactionBase"]</xpath>
    <value><categoryTag>RatkinPlayer</categoryTag></value>
</Operation>
<!-- 修正鼠族 NPC 派系基类 -->
<Operation Class="PatchOperationReplace">
    <xpath>Defs/FactionDef[@Name="RatkinFactionBase"]/categoryTag</xpath>
    <value><categoryTag>RatkinStory</categoryTag></value>
</Operation>
```
(若装 Ratkin Faction+,其 Bad 派系基类 `RatkinFactionBadBase` 同样修正)

### 3.3 步骤 2:通用职位服饰(基类 PreceptRoleMultiBase / SingleBase)

所有职位都继承两个抽象基类:
- `/Defs/PreceptDef[@Name="PreceptRoleMultiBase"]/roleApparelRequirements`(多角色职位基类)
- `/Defs/PreceptDef[@Name="PreceptRoleSingleBase"]/roleApparelRequirements`(单角色职位基类)

Ideology+ 往这两个基类 Add 通用服饰(兜帽/头巾/胸针/勋章/袖子/白外套/披风/银桂冠),**只需写一次,所有职位自动继承**。这是"像原版一样所有职位共享通用服饰"的实现。

### 3.4 步骤 3:各职位专属服饰(具体 PreceptDef)

| 职位 defName | 专属鼠族服饰 |
|---|---|
| IdeoRole_MedicalSpecialist 医药专家 | RK_Respirator 口罩 |
| IdeoRole_ProductionSpecialist 生产专家 | RK_WorkApron 工作围裙 |
| IdeoRole_MiningSpecialist 采矿专家 | RK_MinerHelmet 矿工帽 |
| IdeoRole_PlantSpecialist 种植专家 | RK_StrawHat 草帽 |
| IdeoRole_ShootingSpecialist 射击专家 | RK_Circlet 圆环 + RK_HeadBand 头带 |
| IdeoRole_MeleeSpecialist 近战专家 | RK_Circlet 圆环 + RK_HeadBand 头带 |
| IdeoRole_Leader 领袖 | RK_CaesarCrownA 金桂冠 + RK_RoyalCrown 皇冠 + RK_RoyalRobe 皇袍 |
| IdeoRole_Moralist 传道者 | RK_SistersVeil 修女头巾 + RK_SistersDerss 修女服 + RK_SantaHat 圣诞帽 + RK_SantaRobe 圣诞袍 |

全部带 `allowedFactionCategoryTags: RatkinStory + RatkinPlayer`。

### 3.5 步骤 4:VME(Vanilla Ideology Expanded - Memes)适配

VME 文件夹仅当 `VanillaExpanded.VMemesE` 激活时通过 LoadFolders 加载:

a) **VME 职位补鼠族服饰**(Ratkin_PreceptRoles_Patch_VCE.xml):
- 甜点师 `VME_IdeoRole_Patissier` → 加 RK_ChefHat 厨师帽(allowed 鼠族)
- 结构用 PatchOperationFindMod 门控 Vanilla Cooking Expanded

b) **VME 迷因职位补人类服饰但排除鼠族**(Core_PreceptRoles_Patch.xml):
- 给 `PreceptRoleMultiBase`/`IdeoRole_Leader` 加原版/皇家/VFEM2 服饰(高礼帽/贵族马甲/皱边衬衫/冠冕/皇冠/皇袍/王冠/王袍)
- **关键**: 用 `disallowedFactionCategoryTags: [Tribal, RatkinStory, Ratkinplayer]` 把鼠族排除 —— 鼠族领袖不会被迫穿人类皇冠
- 用 `anyMemeRequired: [VME_Aristocratic, VME_Royal, VME_GodEmperor]` 只在贵族/皇家/神皇迷因下生效

c) **VAE 服饰进 VME 职位**(VCE_PreceptRoles_Patch_VAE.xml):
- 甜点师加 VAE 厨师帽(disallowed 鼠族,与鼠族厨师帽互斥)

### 3.6 步骤 5:其他配套(视需要)
- **CE 适配**: [Stat] 补丁在 Ratkin CE Patch Collection 存在时给 RK_Kuabao/RK_YaoBao/RAE_DrillingBit 补 CE 属性(CarryBulk/CarryWeight/MiningSpeed 等)
- **RW+ 零食适配**(与衣物无关,不搬): 甜点/茶点 precept + thought + 汉化
- **文化定义**: 新增鼠族军阀 CultureDef + 命名 RulePack + 小人名字库(可选搬)

---

## 四、本地现有文化适配的问题诊断

本地 `Patches/03_文化职位适配.xml`(964 行)现状:
- 前半: 给 ~90 件鼠族服饰加 `RatkinClothes` apparel tag(实际职位要求用 requiredDefs,没用 tag —— 冗余)
- 后半: 给原版 11 职位 + VME 12 职位加 roleApparelRequirements(requiredDefs 方式),含金鼠族 FindMod 追加

**核心问题(为什么"做得不行"):**

1. **完全没有派系限制(最严重)**: 所有 `li` 都没有 `allowedFactionCategoryTags`。职位要求对**所有派系**生效,但鼠族衣服被 raceRestriction 限制只有鼠族能穿 → **人类/帝国/部落文化的领袖和专家会被要求穿穿不上的鼠族衣服,长期"着装要求未满足"不满,职位收益受损**。
2. **派系 tag 没修**: HSK 的 `RatkinFactionBase` 仍是 `Empire` tag(作者写错),玩家派系是 `Player` tag。即使加限制也无法正确匹配鼠族。本地只给自建派系(Rakinia_WhiteRoseKingdom)打了 RatkinStory,杯水车薪。
3. **RAE 8 件核心文化服饰缺失**: 兜帽/勋章/披风/口罩/围裙/矿工帽/金桂冠/银桂冠本地都没有 def → 文化职位缺"经典职位服饰",只能拿现有衣服硬凑,观感差。
4. **VME 职位同样无派系限制**: 同样的"所有派系被要求穿鼠族衣服"问题。
5. **缺"通用服饰"层**: 没利用 PreceptRoleMultiBase/SingleBase 基类,而是每个职位手写整份列表,重复且易漏。

---

## 五、重新适配方案

### 5.1 整合 RAE 衣物(先决条件)
把 RAE 的必需 8 件(+推荐项)整合进本地 mod:
- Defs: 33 件 ThingDef(参照 RAE 定义,继承本地/HSK 基类 RK_ApparelMakeableBase 等)
- Textures: 复制 RAE `Textures/Apparel/<名称>/` 全部贴图
- 汉化: 复制 RAE 简中汉化(放 `Languages/ChineseSimplified (简体中文)/`)
- HSK 适配: 材质(stuffCategories)、科技档、配方(RecipeDef 挂鼠族缝纫台/锻造台)、研究前置、CE 属性、携带重量
- 补充: RK_NORain Thought + RK_Rainproof Hediff(雨披防雨思想)

### 5.2 重写文化职位补丁(抄 Ideology+ 实现)
1. **新增派系 tag 修复补丁**:
   - `RK_PlayerFactionBase` Add `categoryTag=RatkinPlayer`
   - `RatkinFactionBase` Replace `categoryTag=Empire → RatkinStory`
2. **重写 03 文化职位补丁** 为"基类 + 专属"结构:
   - 通用服饰 Add 到 `PreceptRoleMultiBase` + `PreceptRoleSingleBase`(兜帽/勋章/披风/围巾/袖子/桂冠银等,带 allowedFactionCategoryTags)
   - 原版 11 职位专属服饰(保留现有精选列表 + 补 RAE 新件,全部加 allowedFactionCategoryTags)
   - 金鼠族追加保留(FindMod 门控)
3. **VME 适配重写**:
   - 12 个 VME 职位:现有列表 + allowedFactionCategoryTags
   - 新增 VME 迷因排除(disallowedFactionCategoryTags 鼠族 → 鼠族不穿人类皇冠/高礼帽)
   - 甜点师 RK_ChefHat(已有)保留
4. **校验**: ET.parse 全部 XML;确认无重复 def、无 <tag> 未转义、xpath 全部命中;双目录同步部署。

### 5.3 同步
工作区 `C:\Personal\Project\ratkin-patch\鼠族HSK拓展\` ↔ 部署 `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\鼠族HSK拓展\` 双份同步。

---

## 六、实施结果(2026-08-21 已完成)

### 6.1 RAE 衣物整合(27 件 + 2 配套)
| 交付物 | 内容 |
|---|---|
| `Defs/ThingDef/RKGW/RAE_新增27件.xml` | 27 件 ThingDef(必需8 + 推荐19),继承 HSK 基类 RK_ApparelMakeableBase 等 |
| `Defs/ThingDef/RKGW/RAE_Rainproof_HediffThought.xml` | 雨披 RK_Rainproof Hediff + RK_NORain 防雨 Thought |
| `Textures/Apparel/<27件>/` | 27 组贴图(4-9 张/组,含 Thin 变体) |
| `Languages/.../ThingDef/Apparels/RAE_新增27件.xml` | 27 件汉化(56 条) |
| `Languages/.../HediffDef/RAE_Rainproof.xml` | Hediff/Thought 汉化 |
| `Patches/80_RAE新增服饰HSK适配.xml` | 27 件 HSK 适配:种族 apparelList、关闭自动配方+手写配方(鼠族缝纫台/锻造台/加工台,HSK 研究线)、SoakingWet 防雨关联、CE CarryWeight |

配方研究分配: 基础装饰/衣物→Ratkin_Apparel_B1;女仆装/披风/制服→B2A;执政官制服/安全绳→C1/C5;胸甲/矿工帽→Ratkin_Armor_B1;金/银桂冠→C2(GoldBar/SilverBar 合金锭)。
未整合: RK_TestLabel(玩具)、RAE_DrillingBit(钻机,与本地工具重复)、太空 4 件(与本地太空甲重叠,待评估)。

### 6.2 文化职位补丁重写(`Patches/03_文化职位适配.xml`,v3)
照抄 Ratkin Ideology+ 实现,结构:
- **A. 派系 tag 修复**: RK_PlayerFactionBase Add RatkinPlayer;RatkinFactionBase Empire→RatkinStory;RatkinFactionBadBase→RatkinStory
- **B. 通用服饰**: PreceptRoleMultiBase(原版自带 roleApparelRequirements,追加 li)+ PreceptRoleSingleBase(原版无节点,Add 创建 roleApparelRequirements)——所有职位自动获得兜帽/勋章/披风/银桂冠等通用服饰
- **C. 原版 11 职位**: 6 个直接 Add(Leader/Moralist/Shooting/Research/Plant)+ 5 个 Conditional(Melee/Production/Mining/Animals/Medical,**原版无 roleApparelRequirements 节点**,旧版 Add 静默失败是"适配不行"主因之一)
- **D. VME 12 职位**: 保留精选服饰 + 全部带 allowedFactionCategoryTags
- **E. VME 人类服饰排除鼠族**: `li[position()<last()]` 加 disallowedFactionCategoryTags(鼠族不穿 VME 人类服饰)
- **F. 金鼠族追加**: 独立 FindMod(goldenhsk)块(原版职位直接 Add + VME 职位 Conditional 保护)

核心修复: 所有鼠族服饰 li 均带 `allowedFactionCategoryTags=[RatkinStory, RatkinPlayer]`,**仅鼠族文化派系被要求穿鼠族服饰**,人类/帝国/部落派系不再被迫穿穿不上的鼠族衣服(旧版最大问题)。

### 6.3 验证结果
- 73 个 XML(Patches + 新增 Defs)ET 解析全部通过
- 85 个职位服饰引用全部有定义(本地 1002 def 池无缺失)
- 研究/工作台/基类引用全部存在(TableMachining 为原版 1.6 加工台)
- 12 号关自动配方补丁为白名单,与 80 号无冲突
- 双目录已同步,`local.ratkin.clothesweapons` 在 ModsConfig 激活
- 游戏日志旧版 0 failed(补丁应用成功但无派系限制 → 所有派系被要求穿鼠族衣服,体验差),新版已根治

---

## 七、v8 主题化重做(2026-08-25)

### 7.1 用户诉求与诊断
v7 每个职位是"头池/身池/全池"3 个 li,用户反馈"选择太少、很多衣物被归类到一起、每职业可选项不多"。
盘点全 mod 209 件衣物 + 22 职位池后确认核心问题:
- **同层衣物堆一池不构成"多选项"**: OnSkin 层 40+ 件上衣塞一池,角色一次只能穿 1 件,其余全是"同一档位里挑一件",视觉/换装维度窄。
- **主题被埋没**: 14 国军服、和风羽织、重甲、职业装等鲜明主题混在大池里,看不出职位该有的形象。
- **配饰/头饰混在身池**: 帽子、面具、背包、勋章等被当"上衣"处理,无法自由搭配。

### 7.2 机制确认(1.6 反编译)
- `roleApparelRequirements` 的 li 之间 **OR**(满足任意一套即可)。
- 单 li 内 `requirement.requiredDefs` 是 **OR**: 穿上覆盖 `bodyPartGroupsMatchAny` 任一部位的任意一件即满足该 li。
- 每层同时只能穿一件 → 同层多件 = "替换件挑一件穿",跨层 = 可同时穿。
- 结论: "每套造型 = 头+身+配饰混合 li",套间主题不同 → 角色能成套穿出多种完整 look。

### 7.3 v8 结构
- **每职位 2-4 套主题 li**(Leader: 皇权盛装/官服朝仪/华服礼装/军事统帅; Moralist: 法衣经纶/面具法会/白袍清修/苦行; Shooting: 欧陆铁血/英伦同盟/红色阵营/国家卫队; Melee: 重甲骑士/圣武士/东瀛武士/铁血战甲; 生产/医疗/研究等 2-3 套)。
- **每套 = 头 2-7 件 + 身 3-8 件 + 配饰 1-3 件**,同一主题内跨层自由组合,同层替换件挑一件。
- **li[last()] 全池兜底** = 全部主题并集 + 通用件(挎包/腰包/围巾/勋章/安全绳/披风/围裙等),宽松满足 + 金鼠族 F 段 Add 目标不变。
- 未被 v7 使用的 39 件全部纳入: 金鼠族外 27 件(大胡子/烟斗/酒葫芦/挎包/腰包/勋章/围巾/藤牌/障牌/弹药袋/装具/指物/儿童服×3/香囊/玉佩/耳坠/空甲 3 套含头盔/护盾芯片/力盾/先驱者×3/麦克斯韦×4/鹦鹉/挂包),12 件儿童/金鼠族专用不参与。
- 金鼠族 OA_RK 追加 F 段、G 段 RK_CultureRole tag、PawnKind 预置照旧。

### 7.4 验证
- `_tmp/patch_simulator.py` 模拟执行(纳入 Unified.xml 补外部 def 缺口): **75 步 0 失败**。
- 22 职位最终 li 数 3-5,兜底 29-60 件。
- 引用审计: 225 个非金鼠族 defs 全部存在(本池 210 + RatkinRaceHSK + Unified),0 缺失。
- 双目录已同步。
- 生成脚本 `_tmp/gen_03_v8.py`(数据驱动, 改主题直接重生成)。
