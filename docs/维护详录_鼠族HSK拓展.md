# 维护详录 — 鼠族HSK拓展

> 本文档承接 AGENTS.md 中所有**冗长/冗余/一次性详细记录**,按主题分类归档,作为查询型详录。
> AGENTS.md 仅保留精炼索引与铁律;改动项目时先读本文档对应章节,再动手。

---

## 1. 项目全景与双目录铁律

- Mod: `local.ratkin.clothesweapons`;147 件(鼠 89+维 44+追 14),配方/研究/CE/文化全适配。
- 环境: 1.6.4871 + Core_SK + RatkinRaceHSK + CE + Vile's MS + ResearchTreeSK。
- **双目录**: 工作区 `C:\Personal\Project\ratkin-patch` ↔ 部署 `...\RimWorld\Mods\鼠族HSK拓展`。**改动必须双份同步,否则游戏不生效。**
- **工作区根目录**: 根目录只放完整 mod 文件夹(整文件夹便于整体同步到部署目录);临时文件/中间产物一律整理进 `_tmp/` 独立目录,不散落根目录。
- **同步铁律**: About.xml/补丁文件逐文件复制(勿整目录覆盖,避免中文/空格路径在 bash 循环里拆词;含中文/空格路径用 Python glob+shutil 或 find -print0 处理),复制后核对双目录 diff,防 `Mods/<mod>/<mod>/` 嵌套目录残留(嵌套目录里只有补丁时直接删)。
- **部署目录可能有历史别名**: 科技蓝图 → `研究材料消耗`(About 仍显示科技蓝图与逆向工程HSK)、RimHUD适配 → `RimHUD`。改动后按 packageId 找真实部署文件夹再同步。

### 1.1 西风骑士团近战武器整合(2026-08-26)

- 源 Steam 3768887719(markamell.favoiusweaponandarmour,「Knights of Favonius」)。
- 整合 5 把近战(KOFS 基础剑/KOFR 细剑/Favonius_Sword 西风之剑/Greatsword 大剑/Lance 长枪)+ **西风书 Favonius_Codex**(修女/传教士手持工具,单手,近战钝/锐甲穿均 1mm,大幅灵能属性 PsychicSensitivityOffset 0.5/PsychicEntropyRecoveryRate 0.2/MeditationFocusGain 0.15 + 社交 SocialImpact 0.3/NegotiationAbility 0.1,参照原版灵能法杖 eltex staff)+ **4 件装甲**(内衣/板甲/重裙/军官板甲,原设计意图"通用装甲人类/鼠族均可穿,不入 79 号限制";**但 00 号把它们写进了鼠族 apparelList,而 apparelList 的语义是全局白名单→等于鼠族专属**,与意图相反。2026-08-28 按"衣物只给鼠族/金鼠族穿"的新口径**维持锁定**;真要给人类穿须把 4 件从 Ratkin 白名单移除(移走后对所有种族开放,需补 _Female/_Male 贴图)并同步改 §7.11)。
- **石化时代(后工业)档**: 材料 ComponentIndustrial/SyntheticFibers;研究 RKHSK_FavoniusArmory(Weapon_SK x=13 y=29,前置 Melee_C2 + **Oil_Industry_C6**(石化,解锁合成纤维 MakeSyntheticFibers),HiTechResearchBench+研究终端,写法照抄 RKHSK_Tools_Industrial2)。**全部 10 个配方统一电力锻造台 RK_ElectricSmithy(武器+装甲+书同台)**。
- **文件**:
  - `Defs/ThingsDefs/RKMS_Weapon_Favonius.xml`(武器,父类 BaseMeleeWeapon_Sharp_Quality,自带 recipeMaker Inherit=False IsNull=True 关自动配方,CE ToolCE+Bulk+单双手tag,SYS WeaponExtention 位置偏移)
  - `Defs/ThingDef/Favonius_Armor.xml`(装甲,父类 ApparelMakeableBase,CE 护甲值 4/3~16/14 + Bulk/WornBulk + CarryBulk/CarryWeight)
  - 两个 RecipeDef 文件 + 汉化。
  - 贴图 `Textures/Weapon/Favonius/`(武器)与 `Textures/Things/Apparel/Favonius/`(装甲,含 Fat/Female/Hulk/Male/Thin 各体型变体)。
- **体型适配核心**: `Source/FavoniusBodyScale.cs` → `Assemblies/FavoniusBodyScale.dll`,Harmony Postfix patch `Verse.PawnRenderNodeWorker.ScaleFor(PawnRenderNode node, PawnDrawParms parms)`——命中条件(双保险): ①node.Props.texPath 前缀 `Weapon/Favonius` ②parms.pawn.equipment.Primary.def.defName 前缀 `RKHSK_Favonius`;命中后 `__result *= pawn.RaceProps.baseBodySize`(人类 1.0 不变,鼠族 0.8 → 武器 0.8,1:1 体型缩放)。2026-08-30 增命中条件 d: 盔甲节点 `node.apparel` 非 null 且装备 defName 前缀 `DankPyon_`/`DankPoyn_`(中世纪大修全系盔甲头盔, 原作有 typo)→ 乘 baseBodySize, 修鼠族穿 MO 板甲/大头盔偏大(身体节点 Worker 先调 base 可传导, 头部节点直接走基类)。反编译结论: 原版 1.6 ScaleFor 不按 baseBodySize 缩放(只乘 drawSize/bodyType/儿童动画),SYS WeaponExtention 也只做偏移——故需自写 patch。csc 编译需额外 `/r:` netstandard.dll + UnityEngine.dll。

---

### 1.2 书籍拓展HSK 整合(2026-08-27,独立 mod「书籍拓展HSK」)

- **三个源 mod**:
  - 2193152410 Vanilla Books Expanded(VBooksE,1.6 原生,主身份) —— 写作台/打字机台/报纸/12 技能书配方/图书馆房间。1.6 版 DLL 仅 13KB(补丁类),围绕**原版 1.6 书籍体系**(BookBase/TextBook/原版 Reading job)重写,不再有 1.4 的独立技能书 def。
  - 2894816192 Vanilla Books Expanded Expanded(VBEE,仅 1.4) —— 印刷机(手动/电动)/书籍复印/焚书/Ideology 意识形态书/书架。**DLL 引用 VBooksE 1.4 专属类(CompBook/ITab_Book/SkillBook/RoomRoleWorker_Library),在 VBooksE 1.6 DLL 中已全部删除 → VBEE 1.4 DLL 无法在 1.6 运行**。
  - 3789304104 [sbz] Bookcase(1.6 原生) —— 4 种书架(TwoTier/NarrowTwoTier/Bookshelf/BookshelfSmall),自带 sbzBookcase.dll(渲染层),直接可用。
- **整合决策**:
  - 名称/packageId/简介图沿用 2193152410(`VanillaExpanded.VBooksE`)——保留原 packageId 使 Vanilla Skills Expanded 的 `VSE_Writing` Expertise(MayRequire=VanillaExpanded.VBooksE,引用 VBE_WritingSpeed/ReadingSpeed stat)自动联动。
  - VBEE 仅移植**纯 XML 功能**: 印刷机(手动/电动)+ 焚书配方。**复印/意识形态书/书架无法移植**(依赖失效 DLL 类)。
  - sbz 书架整体并入(Defs/DLL/贴图/英文 Keyed)。
- **HSK 适配**:
  - 研究并入 `Craft_SK` tab(Core_SK 提供),挂六档门禁: VBE_Writing=中世纪档(SimpleResearchBench)、VBE_Printing=前工业档(LabTerminal,前置 Electricity)、VBE_PrintingPress(新增,前工业档,前置 VBE_Printing)。
  - 二级菜单: VBE_SubCategory_Bookcases(书架,挂 Furniture)+ VBE_SubCategory_WritingAndPrinting(写作与印刷,挂 Production)。
  - 年龄限制: RestrictWorkTypesByAge 补丁给 Human + Ratkin 均加 `VBE_Writing>13`(鼠族基类 defName=Ratkin,AlienRace.ThingDef_AlienRace)。
  - 焚书配方挂 Campfire + ElectricCrematorium(原版 BurnDrugs 所在建筑),过滤原版 `Books` 分类(VBEE 1.4 用自定义 VBE_Books 分类,1.6 改原版)。
  - 报纸配方挂打字机台+印刷机,加 `researchPrerequisites>VBE_Printing`,补 tradeTag Book。
  - HSK Mod Assistant 标记(NativeAddon)。
- **清理**: 删除 1.4 遗留 26 张贴图(Book*/Map/TechBlueprint 技能书贴图与 Motes,1.6 用原版 TextBook 贴图)、VBEE FurnitureProps/Bookshelf 贴图(DeepStorage 书架专用,未整合)。词库 16 个 txt 放 `Languages/English/Strings/Words/BookWords/`(1.6 版官方位置,RulesFiles 走语言系统解析;`1.6/Words/` 仅 4 个是官方冗余)。
- **验证**: `_tmp/sim_book_hsk.py`(patch_simulator 变体)5 补丁 8 步全过;双目录 124 文件 SHA1 一致;ModsConfig 已启用。

---

### 1.3 边境拓展HSK(工坊 mod「Borders of the Rim」本地化,2026-08-27)

- **源**: Steam 3780770870「Borders of the Rim」,作者 **NehsModsForDev**,packageId **`NehsModsForDev.bordersoftherim`**,1.6,DLL mod(`Assemblies/BordersOfTheRim.dll` ~582KB)。玩法:派系散布据点→带首都/大城/城镇/哨站的地理疆域、可见边界、领地战争/联盟/附庸/内战、世界路线上可见的战争兵团与商队、玩家六类战争任务/自主殖民地等。
- **模式**: **整 mod 直搬本地化**(非重写成 HSK 内容)。用户要求**保留原 packageId + 作者**,便于后续 diff 工坊原版 / 修 bug。ModsConfig 当前**未启用**工坊版 → 本地同 id 无冲突,启用后即取代工坊条目、存档与加载顺序不变。
- **HSK 三步改造**(AGENTS.md §2.2):
  1. 整文件夹 `shutil.copytree` 复制(平铺结构,无 LoadFolders/版本子目录: About/Assemblies/Defs/Languages/Patches/Textures)→ 根目录 `touch HSK` 空标记(约定)。
  2. `Patches/00_ModAssistant标记.xml`: 复用 Reel 模板骨架,`PatchOperationAdd MayRequire="DimonSever000.ModIndicator.Specific"` → xpath `Defs/AMA.ModListerSettingsDef[defName="ModListerSettingsDef"]/modIndicators/NativeAddon`,注入 `<li>` 含 **`<id>NehsModsForDev.bordersoftherim</id>` + `<HSKMod>true</HSKMod>`(HSK 集成标记)+ `<loadAfter>`(Core SK / Solaris.RatkinRaceMod / rimthemeslite / factionalwarcontinued)**,格式对齐 def 内 EndMod 组条目(如 krkr.rocketman 的 id+HSKMod+loadAfter)。
  3. `About.xml`: **保留** packageId/author/name/description 不动;`modDependencies` 在 Harmony 后补 **Core SK(skyarkhangel.HSK)**;`loadAfter` 追加 `skyarkhangel.HSK` + `skyarkhangel.rimthemeslite` + `Solaris.RatkinRaceMod` + `sr.modrimworld.factionalwarcontinued` + `DimonSever000.ModIndicator.Specific`(原 Harmony/odyssey/vanillaexpanded.gravship/sk.gravshipraids 保留)。**加载位序结论**: 本 mod 是世界地图派系疆域/战争系统,注入 Surface `worldGenSteps`(聚类派系据点)+ `MapGeneratorDef[Base_Faction]` + `WorldObjectDef[Settlement]/comps`,须在世界生成与派系/种族 def 定稿后加载 → 排在 Core SK/RatkinRaceHSK/RimThemes(改世界生成)与 Factional War(同派系战争生态)之后;ModsConfig 手动置于 EndMod 组(`mlie.wikirim`)前(pos 221),已天然晚于以上全部,亦晚于 Raid Extension(raidforces 交叉)。
- **验证**: 两个 Patch 文件 ET.parse OK;标记补丁与 Reel 已知良好模板仅 id 一行差异;原 `Patch_WorldGenerationAndDisplay.xml`(纯原版 PlanetLayerDef/MapGeneratorDef/WorldObjectDef 挂载,MayRequire 无关)未改动 → 无需跨 mod 模拟;About ET.parse 通过。
- **双目录**: 工作区 `边境拓展HSK` ↔ 部署 `Mods\边境拓展HSK`,20 文件(18 原 + HSK + 标记补丁)SHA256 逐一致,无 `Mods/<mod>/<mod>/` 嵌套。已在 ModsConfig `<activeMods>` 启用,位置在 `mlie.wikirim`(EndMod 组)之前、ModIndicator/Core SK 之后(pos 221/228);备份 `_tmp/ModsConfig_backup_borders_20260827.xml`。
- **已知交叉点(修 bug 重点)**: Factional War(`sr.modrimworld.factionalwarcontinued`)/Raid Extension(`sr.modrimworld.raidextension`)与 Borders 都做派系战争/袭击兵团;RimThemesLite 与 Borders 都改 Surface 世界生成。已把 Borders 排其后减轻覆盖冲突,若仍有异常优先查这几处。
- **修 bug 备忘**: 后续若发现玩法冲突,先与 `content/294100/3780770870` 工坊原版逐文件 diff;DLL 问题走反编译 ilspycmd;与 HSK 世界地图/派系生成有交叉时重点查 `Defs/WorldGeneration/WorldGenerator.xml` 与本 mod `Patch_WorldGenerationAndDisplay.xml`(往 Surface 层 `worldGenSteps/worldDrawLayers/worldTabs` 注入)。

---

### 1.4 1.6HSK核心全ai汉化 做 HSK 标记 + 排位(2026-08-27)

- **对象**: 部署目录 `Mods\1.6HSK核心全ai汉化`,packageId **`AITranslation.Pack`**(ModsConfig 里小写 `aitranslation.pack`),AI 自动生成的大体量翻译包(含 Assemblies/Cont/Languages/Backups/Upload_Workspace)。
- **目标**: ①按 §2.2 三步法做 HSK 标记;②排位到 **HSK 段末尾第一位**(HSK修复整合之后、EndMod 组之前)——即"和 runtimeGC 那批一样压尾,但汉化是末尾组前面的第一个"。
- **改动**:
  1. `Patches/07_ModAssistant标记.xml`: 原为 `modIndicators/Core` + `<li><id>`(2026-08-14 当"环世界核心"标);改为 `modIndicators/NativeAddon` + `<id>AITranslation.Pack</id>` + `<HSKMod>true</HSKMod>` + `<loadAfter>skyarkhangel.hsk / local.hskfixpack</loadAfter>`(模板同边境拓展HSK 00 号,MayRequire 门控)。
  2. `About/About.xml`: 原本只有 name/author/packageId/supportedVersions;补 `modDependencies`(Harmony + Core SK)、`loadAfter`(`skyarkhangel.HSK` + `local.hskfixpack` + `DimonSever000.ModIndicator.Specific`)、纯文本 `description`(无未转义 tag,ET.parse 通过)。packageId 保持 `AITranslation.Pack` 不变。
  3. 根目录 `touch HSK` 空标记(发布约定)。**工作区镜像**: `ratkin-patch\1.6HSK核心全ai汉化\` 只放改动的 3 项(About.xml / 07 标记补丁 / HSK),不整搬 Languages 大目录。
  4. `ModsConfig.xml`: `aitranslation.pack` 由 pos 192 → **183(紧跟 local.hskfixpack=182)**,总 227 条不变,EndMod 组(222-226)仍在其后。
- **踩到的坑(重要)**: HSK修复整合 About 的 loadAfter 里原本有一条 `aitranslation.pack`(历史遗留,方向与"修复整合在前"相反)。加了汉化侧 `loadAfter local.hskfixpack` 后即成 **2-环**(汉化→修复整合→汉化)。`_tmp/cycle_all.py` 原样跑与"表边∪About边"合并跑都能报出该环 → **删除 hskfixpack About 里的 `aitranslation.pack`**,双目录(工作区 + 部署)同步改,SHA 一致 `cd4fcf685a18`;两口径复跑均 **0 环**。
- **验证**: 两个改动文件 ET.parse OK;lxml 注入模拟 → NativeAddon 条目数 26→27、汉化条目含 HSKMod+loadAfter、`Core` 段无残留。备份在 `_tmp/backup_aitranslation_hskmark_20260827/`(About / 07 补丁 / hskfixpack About ×2 / ModsConfig ×2)。

---

## 2. 加载顺序与依赖

### 2.1 本 mod 配方来源

Core_SK → RatkinRaceHSK(HSK_Generated 整体替换鼠族基类,基类自带 recipeMaker 自动配方;删 RK_Research_*)→ 本 mod(最末)。旧"自动配方"=基类 recipeMaker 产物,须最终层补丁关闭。

### 2.2 HSK 标记与 load/依赖规范(2026-08-26 实测)

**HSK 标记的运行时判定** = `HSK Autosort and Mod Assistant`(packageId `DimonSever000.ModIndicator.Specific`,DLL: 1.5 版 `ModIndicator.dll` / 1.6 版 `AutosortModAssistant.dll`)读取它自己的 `Defs/ModListerSettingsDef/ModListerSettingsDefs.xml` 映射表,按 packageId 给 mod 分类/排序/显示兼容性。`HSKMod=true` 或所属类型组(如 NativeAddon/EndMod)= "HSK 集成/适配 mod"标记。**判定不读取 mod 根目录的空 `HSK` 文件**(DLL 字符串无文件系统读取逻辑);根目录空 `HSK` 文件只是 Hardcore-SK 发布流程约定(203 个 mod 中 161 个带),保留无坏处但非生效依据。Core_SK.dll 也无读取该文件的逻辑。

**给新本地 mod 加 HSK 标记的标准动作(三步,缺一不可)**:
1. 复制 mod 整文件夹到 `...\RimWorld\Mods`(保留 About/packageId/版本目录),根目录 `touch HSK` 建空文件(约定,可选);
2. **在 mod 的 Patches 里加 ModAssistant 标记补丁**(NativeAddon 注入,模板见各 mod `*_ModAssistant标记.xml`/`99_ModAssistant标记.xml`): `PatchOperationAdd MayRequire="DimonSever000.ModIndicator.Specific"` → xpath `Defs/AMA.ModListerSettingsDef[defName="ModListerSettingsDef"]/modIndicators/NativeAddon` → value `<li><id>本mod packageId</id></li>`。**MayRequire 门控写法沿用既有补丁(本环境 FindMod 不生效)**;
3. About.xml 补 `modDependencies`(真依赖: Harmony/Core SK/RatkinRaceHSK 等)+ `loadAfter`(被依赖 mod + `DimonSever000.ModIndicator.Specific` 压尾)+ 必要时 `loadBefore`(本 mod 修复/被其覆盖的 mod,如 HSK修复整合 loadBefore 特性拓展/酒馆/帝国银币,鼠族HSK拓展 loadBefore 金鼠族/家具/徽章)。

**新增本地 mod 默认声明**: modDependencies ≥ Harmony(brrainz.harmony) + Core SK(skyarkhangel.HSK);loadAfter ≥ 依赖项 + ModIndicator;鼠族系 mod 另依赖 `Solaris.RatkinRaceMod`(NewRatkinPlus/RatkinRaceHSK)。

**EndMod 组(永远最后,由 Mod Assistant 排序)**: trinity.runtimegcfixed / dubwise.dubsperformanceanalyzer.steam / mlie.wikirim / dimonsever000.wiki.specific / taranchuk.performanceoptimizer / krkr.rocketman。Performance Optimizer 本地化 = workshop 完整版(含 1.6 DLL,与 1.5 适配版零代码差异)+ HSK 标记(表里已登记 EndMod+HSKMod=true,见 ModListerSettingsDefs.xml ~992 行)。

**loadBefore 用 packageId 不用文件夹名**(实例: 鼠族HSK拓展曾写 `PawnBadgeHSKFix` 应为 `saucypigeon.pawnbadge`)。

**加载顺序与依赖总表**: 完整依赖/加载顺序见 `docs/00_mod加载顺序与依赖.md`(用户确认骨架: HSK 一堆 mod → 我的大修[贸易→工业→科研] → skill/特性拓展 → 种族 mod → 修复整合压尾)。新增/调整任何本地 mod 的 About.xml 依赖时必须先查该表,防循环依赖。

**科研大修方案**: 科技蓝图与逆向工程HSK 与 hsk工业大修 已于 2026-08-27 合并为「HSK工业与科研大修」(ratkinpatch.HSKIndustrialResearchOverhaul),长期科研线(蓝图门控 + 图纸书书架阅读 + 科研任务闭环 + 太空/极致门控扩展)完整方案见 `docs/科研大修方案_v2.md`。MO 科研解锁机制(RequiredSchematic/图纸书/任务闭环)为借鉴对象,见 `docs/中世纪大修内容梳理.md` + `docs/科研门禁统一方案_中世纪大修x科技蓝图.md`(v1 废弃路线)。

---

## 3. 关键坑与踩坑记录(全部实测)

> 写法规则类铁律已抽到 `08_铁律/`;此处保留详细语境、实例与排障方法。

### 3.1 PatchOperation / 补丁编写坑
- **关自动配方**: Replace recipeMaker 为 IsNull **不生效**,须 Add `<recipeMaker Inherit="False" IsNull="True"/>` 到 ThingDef(12 号: 先条件 Remove 再 Add)。
- **改自动配方工作台**: 整体 Replace/Add recipeMaker + `Inherit="false"`(深度合并,否则 recipeUsers 合并挂两台)。
- PatchOperation 操作**原始 XML,不解析继承**: 子类无自有节点(继承 Abstract 基类)时 xpath 报 "Failed to find a node",须打基类 `[Name="XXXBase"]` 或 Add 到 ThingDef 自身。
- **xpath 不支持 union(`A|B`)**,须拆多 Operation;Conditional 的 nomatch Add 目标必须存在。
- **补丁执行顺序=文件名(字符串)序,`1xx` 是陷阱编号**: `105_` 首字符 '1' < '3'/'5'/'9',实际排在 11/32/52/91 号**之前**(与 100-103 军阀补丁同段)。**永远不要把 1xx 当"最终层补丁"**——它引用 32/52/91 号 Add 的节点时,目标节点尚未存在,Replace 全报 "Failed to find a node"(实例: 105_科技坐标上移 21 个 Replace 全失败,已并入 91 号末尾)。要"最后执行"必须用 >91 的两位数编号或并入 92 号末尾;引用其它补丁 Add 的节点时,须验证**目标补丁的文件名序在自身之前**,不能只看节点全局存在性。
- **1.6 起 Operation 级 `MayRequire` 已失效**: 反编译确认 1.6.4871 的 PatchOperation 字段仅 sourceFile/neverSucceeded/success,无 MayRequire;ApplyPatches 直调 ApplyWorker,Apply/Conditional 均不检查该属性(原版 Data 里 2009 处 MayRequire 全是 Def 节点级属性,由 DirectXmlLoader 过滤,非 Operation 级)。→ **Operation 上写 MayRequire 不再拦截**,未装目标 mod 时操作仍执行 → 报错(实例: 100⑤ Anty 未装仍执行 nomatch、103 未装载具仍 Add RatkinLiaison_* 交叉引用失败)。**根治: 一律用 xpath 存在性门控**(外层 PatchOperationConditional 检查目标 def 节点是否存在,不存在整段跳过)或 PatchOperationFindMod(mods 列表)。注意 Def 节点级 MayRequire(如 `<li MayRequire="Ludeon.RimWorld.Biotech">`)仍有效,勿删。存量: 全项目 269 处 Operation 级 MayRequire(HSK修复整合 130 + 其它 139)目标 mod 均常驻激活暂无害,迁移时再逐个改。
- **禁用 FindMod+Sequence 嵌套**(1.6 实测不生效): v4/v5 的 03_文化职位适配 C/D 部分用 `FindMod(Ideology)+Sequence` 包职位 li → **职位池从未被应用**(Unified.xml 导出验证仍是原版状态);用户看到的"两列头冠/重复"其实是 RatkinRaceHSK 自带 `1.6/Patches/PreceptDef_RoleApparel_Patch.xml`(leaderRole=true 门控,Conditional 生效)Add 的 2 条与原版叠加所致。**根治: 一律顶层独立 Operation**——原版职位用顶层 `PatchOperationConditional`(xpath=rar 存在性,match=Replace/nomatch=Add);VME/可选 mod 职位用顶层 Conditional(xpath=职位 def 存在门控)+match=Sequence[内层 Conditional(Replace/Add)];可选内容(如金鼠族)用顶层 Conditional(xpath=该 mod 特有 def 存在门控)+Sequence。**改补丁后必须用 `_tmp/patch_simulator.py`(lxml)模拟执行验证**——lxml 与 .NET 语义差异: xpath 无前导 / 时统一加 /(SelectSingleNode 从文档根评估);Sequence operations 子元素 tag 是 li;Conditional/FindMod 的 match 是单个 Operation 整体递归。
- **RecipeDef 同挂 researchPrerequisite+researchPrerequisites 告警**: 继承原版 `BaseMakeableGun`(其 recipeMaker 带单个 `researchPrerequisite=Gunsmithing`)的枪,若子类 recipeMaker 另写复数 `researchPrerequisites` 列表,深度合并后**单个 Gunsmithing 仍被继承保留**,与列表并存 → RecipeDef 同时含两个字段,ResearchTreeSK 启动报 "Research X has both researchPrerequisite & researchPrerequisites set"。**无法用 xpath 删继承节点**(原始 XML 无该节点),根治: 在子类 recipeMaker 内 Add 空节点 `<researchPrerequisite Inherit="false" />` 屏蔽继承的单个前置,复数列表保留(多头科研如沈鸡弩 RecurveBow+Mortars 必须用列表)。实例: 5 把 RK 工业枪 RK_ExoticPistol/RK_ExoticLMG/RK_WallGun/RK_ShenJiCrossBow(Defs/ThingsDefs/RKMS_Weapon_Range.xml)+RK_HuntingRifle_toxic(Defs/RKMD/ThingsDefs/RKMD_Weapons.xml),已就地整改。
- **ResearchTreeSK 重复解锁警告**: 对每个研究统计解锁项,来源五路: ①ThingDef.researchPrerequisites ②TerrainDef ③植物 sowResearchPrerequisites ④RecipeDef.researchPrerequisites(列表) ⑤RecipeDef.researchPrerequisite(单个) ⑥**工作台遍历**: 对 researchPrerequisites 非空且 AllRecipes 非空的工作台,把其上**自身无研究门槛**的配方,按「工作台 × 1」加入该工作台每个前置研究的解锁列表。→ **同一无门槛配方挂 2 个以上同研究解锁的工作台,启动即报 "The research X have duplicate unlocked defs ..."**。根治: 给配方补 `<researchPrerequisite>该研究</researchPrerequisite>`(被工作台遍历跳过、改由显式路径计入 1 次,游戏内配方也随之正确改为研究解锁),或从其中一个工作台删除该配方/recipeUsers 条目。实例: 71 号 RKHSKButcherFish(TableButcher+RKFC_GL_TableButcher 双挂)、Hearth/OpenHearth 的 Make_BabyFood/Make_BabyFoodBulk/MakeRushlight(HSK修复整合 43 号)。验证以游戏日志为准(Unified.xml 导出可能含 DefOverwrite 双份导致误报)。
- **类名可见性铁律**: 补丁里引用 `PawnRenderNodeProperties_EarHideByApparelTag`(Verse 自定义类,在 RatkinRaceHSK 的 NewRatkin.dll)等跨mod类,只要目标 mod 已加载即可全局解析(GenTypes 全局搜类);但依赖该类的补丁**必须 loadAfter 该 mod**(本mod About.xml 已 loadAfter `Solaris.RatkinRaceMod`)。

### 3.2 汉化 / 贴图坑
- **汉化文件夹必须 `ChineseSimplified (简体中文)`**(带后缀),否则 Languages 不加载(已批量改名 Mods 目录 95 个无后缀 `ChineseSimplified` 文件夹 + 删 2 重复副本)。
- **汉化 DefInjected 结构铁律**: 翻译文件根节点必须 `<LanguageData>`,defName 作**节点名**(`<ThingDef><X><label>..</label></X></ThingDef>`);写成 `<Defs>`+`<defName>值</defName>`(Def 定义式)会被游戏**静默跳过、日志无报错**(酒馆工具整合HSK 149 条即此病,已批量转换)。
- **DefInjected 全不加载的兜底**: 酒馆工具整合HSK 即使格式全对、汉化放根目录+`1.6/` 双位置,游戏仍无视其 DefInjected(机制未定位,疑似 1.6 版本目录+汉化包组合问题)→ **兜底方案 = 把中文 label/description 直接注入 Defs 文件**(脚本 `_tmp/酒馆汉化修复/inject_labels.py`: 文本级替换具体 Def 自身节点、跳过 JobToolDef 等玩家不可见道具、保留 DefInjected 不冲突)。本地整合 mod(无多语言分享需求)汉化失效时优先走此方案,100% 生效。
- About.xml/description **不能含未转义 `<tag>`**(整 mod 被静默移出 ModsConfig);写完 ET.parse 验证。
- **统一贴图方向命名**: 机制A渲染的耳朵贴图需 `_east/_north/_south/_west` 四方向;特殊异种耳贴图原缺 `_west`(机制B下无此需),改走机制A后**必须补全**。耳朵 `_west`=`_east` 水平镜像(已验证 `RK_Texture_EarLeft_west` 与 east 镜像平均每通道差值 1.4)。
- **发型贴图缩放坑(2026-08-27 修复)**: 社区发型(Ratkin Hairstyle Expanded/Ratkin Hair Plus,原贴图 1024²)整合进本 mod 时"缩放到 256 基准"实际重排了画布内容(alpha bbox 漂移最大 0.2+,如 RK_Twin_I 内容宽 0.42→0.78),症状="鼠族能选发型但位置/大小不对";原 mod 单独启用正常。**原理(反编译 Assembly-CSharp 确认)**: 1.6 发型由 `Verse.PawnRenderNode_Hair` 走 `HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn` = `headType.hairMeshSize` 方形网格(HAR 再乘种族 `customHeadDrawSize`),**整张贴图拉伸铺满网格,渲染大小与贴图分辨率无关、只取决于内容在画布中的相对位置**→ 缩放必须严格等比不得移动内容。修复 = 从工坊原图重新预乘 alpha + LANCZOS 等比缩到 256(旧图备份 `_tmp/hair_tex_backup_20260827`),校验 = 同尺寸缩放下 bbox drift<0.02 全 30 张通过。**教训: 整合外部贴图必须用「等比缩放后 bbox/IoU 像素对比」验证,勿凭肉眼。** 同法排查其余批次(2026-08-27): RGE 批(RKGP/RKMD/Short01/02/Wolftail/Braided)与原版逐字节一致,仅 `RK_Mai_south.png` 被重绘成 1024 且内容下移约 4%(与其余 256 方向错位)、`RK_Mai_southm.png` 被放大成 1024,已从 `_tmp/RGE_orig` 恢复原版 256 并双目录同步;鼠邦批(RKMS_ 九个发型)全 256、三方向内容方差在原生发型基准内(原生 RK_twin5 高度方差即 0.23),无篡改迹象。注意 `*m` 后缀贴图是头部遮罩、整画布覆盖属正常,勿当错位处理。

### 3.3 DLL / Harmony 坑
- **Assemblies 目录禁止遗留同名测试 DLL**(实锤): RimWorld 会加载某 mod `Assemblies/` 下的**全部 .dll**。若目录里残留调试补丁产物(`VSE.dll.new`/`VSE.dll.p5`/`*.bak`/`*.p10` 等,assembly 名与真 DLL 相同),会被一并加载 → 同名装配体类型冲突 → **小人角色信息页空白 + `Mouse position stack is not empty / BeginScrollView > EndScrollView` 滚动栈泄漏**(看似"该 mod 本身/defs/补丁"的锅,实为残留 dll)。本次坑: Vanilla Skills Expanded 目录 8 个残留 dll 制造假象,曾误导"关 mod 才恢复、改 defs/补丁皆无效"。**根治/预防: 改 DLL 补丁时只在工作区用临时文件名产出并部署单份;部署目录 Assemblies 只保留正式 dll(VSE.dll + VSEHSKFix.dll),残留一律清掉。** 排查"某 mod 在场才出的 GUI 泄漏"时,第一件事就是检查该 mod `Assemblies/` 有无同名单残留文件。详见 `docs/07_排查记录/VSE角色信息页空白问题排查结论_20260825.md`。
- **Harmony 声明式补丁重载歧义**: `[HarmonyPatch(typeof(X), "方法名")]` 不指定参数类型时,若目标方法有**多个重载**(如 `Storyteller.MakeIncidentsForInterval`、`IncidentQueue.Add`)→ `AmbiguousMatchException` 且**连带同程序集整个 PatchAll 崩**(HSK修复整合 FacilityCrashFixInit 被带崩)。**根治: 只 patch 无重载方法(如 `Storyteller.TryFire`),或用 `[HarmonyPatch(typeof(X), "方法名", new Type[]{...})]` 指定参数类型**。
- **VSE 激情越界 patch 陷阱**: `VSE.Passions.PassionManager.PassionToDef` 用 passion 枚举值(0-5)直接索引静态数组 `Passions`(无边界检查),自定义激情 VSE_Apathy/Natural/Critical=3/4/5。**改 `.cctor` 数组长度(DefCount→16)会引入 null 空位 → `AllPassions` 遍历破坏 → 所有激情消失、角色编辑器加不上**;只改 `PassionToDef` 加边界检查(越界返回 `Passions[0]`)则激情正常。**最终 VSE.dll 保持原始版未 patch**(激情完整性优先,理论越界未实测触发)。Mono.Cecil patch 铁律: 分支失败路径若栈上残留 `dup` 的数组副本,ret 时栈不平衡 → 游戏报 `InvalidProgramException: blt IL_0018`(已踩坑两次);栈平衡必须逐路径模拟验证。
- **GameComponent 子类须写 `public X(Game game)`**。
- 改补丁后清 MissileGirl XML 缓存。

### 3.4 DLL 构建(酒馆工具整合HSK JobEffects)
- 源码在 `酒馆工具整合HSK/1.6/Source/`(2026-08-30 起迁至 `工作动画HSK/1.6/Source/`,见 §7.15)。**本机已装 .NET SDK 8.0.424(`C:\Program Files\dotnet`,dotnet 已进 PATH)**,编译命令: `dotnet build "C:\Personal\Project\ratkin-patch\酒馆工具整合HSK\1.6\Source\JobEffects.csproj" -c Release`,产物自动落到 `1.6\Assemblies\JobEffects.dll(+pdb)`。
- 注意: ①csproj 引用路径已改为真实位置(`RimWorldWin64_Data\Managed\` + `Mods\Harmony\Current\Assemblies\0Harmony.dll`),勿改回旧 `D:\SteamLibrary`;②`Source\NuGet.Config` 指向 nuget.org(用户全局源为空会还原失败),直接 `dotnet build` 即可,不需 `--source`;③**编译后必须整文件夹同步到 `...\RimWorld\Mods\工作动画HSK`(2026-08-30 起源码已迁至该独立 mod)**,否则游戏不生效(双目录铁律);④旧 DLL 备份用非 `.dll` 后缀(如 `_JobEffects.dll.bak_YYYYMMDD`),避免被游戏误加载。
- **鼠族等小体型种族随 `baseBodySize` 自动缩放**: 工具**统一缩放(大小与位置一起,不分高度)** = `Mathf.Lerp(1, baseBodySize, 0.25f)`(人类 baseBodySize 1.0→1.0 不变;鼠族 0.8→**0.95**)在 `ArmRenderer.cs` `BodyDrawScale`(`factor *= Mathf.Lerp(1f, RaceProps.baseBodySize, 0.25f)`),`ToolAnimator.cs` `DrawTool` 内统一 `bodyS = currentBodyScale` 驱动尺寸矩阵(Scale)与 `.z`/`reach` 偏移。任何种族仅靠 def 里 `baseBodySize` 即自动适配,无需逐个改;想更小/更大调那个 `0.25`(强度)即可。请求1"动画期间强制隐藏手持贴图"在 `HarmonyPatches.cs`(`Patch_DrawEquipment_OverrideToolMods`+`Patch_DrawEquipmentAiming_OverrideToolMods`+`Patch_HideCarriedMedicine`,已去 OverrideToolMods 门控,由 `HasActiveTool` 判定,仅隐藏渲染不卸装备、加成保留);请求3"结束动画 ×2.5"在 `ToolAnimator.cs` `EndStowSpeedMul = 2.5f`。

### 3.5 运行时崩溃 / 性能排查
- **游玩中闪退排查方法论(非进图崩溃)**: 现象"进图正常、游玩几分钟后崩、日志无报错"(日志末尾停在某 mod flush 只是时序巧合,非根因)。定位三步: ①Windows 事件查看器 Application 日志看崩溃签名(`RimWorldWin64.exe` + 异常码 0xc0000005/0xc0000374 + ntdll 偏移);②**崩溃 dump 在 `C:\Users\admin\AppData\Local\CrashDumps\*.dmp`**(60MB 完整 dump),用 Python `minidump` 库解析(**必须用 `C:\Users\admin\AppData\Local\Programs\Python\Python312\python.exe`,pip 装到的是它;hermes venv python 没有**)——脚本 `_tmp/parse_dump*.py`;③读异常码/ExceptionAddress/Rip/Rsp/栈回溯定位崩溃模块。本次结论: 异常 0xc0000005、**读地址 0xFFFFFFFFFFFFFFFF(野指针)**、Rip=ntdll+0x1AD67(堆分配内部)、栈上 mono `mono_class_is_assignable_from`+`mono_conc_hashtable_lookup` 深度递归 → **Prepatcher 改写程序集导致运行时类型检查递归崩**。
- **Performance Fish 非官方 HSK 适配,已禁用勿再启用**: 上述崩溃根因=Performance Fish(`bs.performance`,非官方 HSK 适配版)的 Prepatcher 在启动时**改写并序列化整个 Assembly-CSharp**(日志 "Prepatcher: Serializing patched assemblies"),运行中类型检查递归崩。**已处理: Mods 文件夹改名 `Performance-Fish-rimworld-1.6.disabled` + ModsConfig.xml 删 `<li>bs.performance</li>` 和 `<li>bs.fishery</li>` 两行(备份 `ModsConfig.xml.bak_pfdisable_20260825`)**。此 mod 纯优化、无任何 mod 依赖它,禁用不影响功能。**移除 mod 的标准操作 = 文件夹改名(非 .dll 后缀) + ModsConfig.xml 同步删 packageId 行,两处缺一不可**。
- **TechAdvancing 8/24 修改备份(已归档)**: 用户修改(研究成本×2 + 配置锁定)最终版 = `_tmp/techadvancing_lock/TechAdvancing_fixed_20260824.bak`(SHA1 a152a561,已恢复部署)。备份链: orig(8/23原始)/working/cost_before_2x/broken_cost2x/fixed。patch 脚本 `patch_ta_lock.py`(stub 3 UI 方法 + TA_ExposeData 两个 TryGetValue 恒 false)+ `patch_ta_cost2x.py`(ConfigChangeResearchCostFacAsFloat 恒返 2.0f)。**铁律: 方法体 stub 必须以 ret 结尾**(旧布局 ret+nop 被 JIT 编译即 InvalidProgramException,8/24 已踩)。"配置定期加载"误解: `[Tech Advancing] Loading config/Flushing` 是**每次读档/进图触发一次**(挂 Game.LoadGame),非定时轮询,游玩中不会重复加载。

### 3.6 异种人耳/尾贴图渲染机制
- 鼠族身体部位贴图有**两套独立渲染管线**: **机制A=Biotech renderNodeProperties**(基因自带,`RK_Gene_LargeEars`/`RK_Gene_ThinTail` 用,渲染 `Things/Ratkin/Body/RK_Texture_EarLeft`,**初鼠种走这里,可靠**);**机制B=HAR universalBodyAddons 的 `Gene For` 映射**(`Patches/RKGE/RKEarsTailPatch.xml`,渲染 `Textures/Body/SQ_EarLeft` 等,**RGE 特殊异种基因(松鼠/仓鼠等)走这里**)。
- **坑1(HT): 机制B在 HSK 运行时失效**——多 mod 往 `RK_Race_Setting/universalBodyAddons` Add 时叠加不可靠(实测金鼠族 `OAGene_*` 耳朵 GeneFor 在 Unified.xml 丢失),且配置虽正确但特殊耳不渲染。**修复: 改走机制A**——`Patches/RKGE/RKEarsTail_MechanismA.xml` 给 8 耳基因(Squirrel/Hamster/LabRat/Mole/Vole/Prototype_Vole/WR/Ears)+5 尾基因(SquirrelTail/MoleTail/Prototype_Vole_Tail/WRTail/Tail)Add `renderNodeProperties`(复刻 `RK_Gene_LargeEars`/`RK_Gene_ThinTail`,texPath=Body/**,linkedBodyPartsGroup=RK_LeftEar/RK_RightEar/RK_Tail,实际靠 parentTagDef=Head/Body 定位不依赖部位组)。**贴图需补 `_west`**(耳朵从 `_east` 镜像,8对已生成;尾巴无 `_west` 是正常,原版 `RK_Texture_Tail` 也只有 e/n/s)。
- **坑2: 基因分组显示**——异种人基因显示在"系谱基因"栏是 RimWorld 正确机制(异种人基因=可遗传端基因 xendogene);"异种基因"栏只有后天不可遗传基因。原版 RGE 用自定义 `GeneCategoryDef`(RatkinGene/RatkinEarsGene/RatkinTailGene)在编辑器里分组,本mod曾注释掉,已恢复(`Defs/RKGE/GeneDefs_Ratkin.xml` 定义+displayCategory 引用+汉化 RkGeneCategory.xml 就位)。

---

## 4. 研究节点(最终态,新配方直接引用;tab=Apparel_SK,y=43/44 两行)

> **⚠️ 全量科研节点总览文档 = `docs/01_科研体系/科研节点总览.md`**(由 `_tmp/gen_research_doc.py` 从 `RimWorld\Mods\Unified.xml` 生成,含 516 节点、按 Tab 分组、各节点带中文名/档位/成本/坐标/研究台/前置/来源mod,开头附「各 Tab 节点数」与「各来源 mod 节点数」汇总)。**任何涉及新增/修改/挂接科研节点的操作(加新配方 researchPrerequisite、挪研究台、改前置链、新增科技节点、避让坐标撞车),必须先 `read docs/01_科研体系/科研节点总览.md` 查询现有节点 defName/坐标/前置,再动手**——严禁凭记忆臆造 defName 或坐标(过去多次撞车 Food_B4/Apparel 等)。文档刷新: 改完补丁后重跑 `_tmp/gen_research_doc.py`。

- **衣物主管线**(Y=43, 每节点 5-10 件):
  - B1 日常衣装(←Apparel_B1) → B1b 头脸配饰(Y=44)
  - B2A 礼服装 → B2B 冬暖装 → B2C 职业装备 → B2D 软甲防御 (Y=44, 同档细分)
  - C1 民用日常(←Fabrication) → C1M1~M4 军服A/B/C/D区 (Y=44, 二战军服按国家分4区,每区10件)
  - C2 头部防御 → C2b 军用大衣(Y=45)
  - C3 衣物IV(后石化, ←Oil_Industry_C6+C1) → C5 衣物V
- **盔甲**: Armor_B1(I,←Craft_B1)→B2(II)→D1(III,←Apparel_D1)→D3(IV骑士)/D2(V)
- 制服 Uniform(←C1,工业);近战 VI=Melee_Ultra(←Melee_E1)
- 工具科技: 中世纪工具I-III+工业工具I-II(←Craft_B1/冶金学III/Metals_C3)
- RK_Research_* 已全删;串联科技放前置 X+1 同行(防画错线)
- 91 号科技树拆分后,衣物线从 7 节点扩为 16 节点,全部 5-10 件,按品类归类
- **鼠邦配饰 MoustateJewerly**: 移入 Apparel_SK (6,45), 前置 鼠族衣物I(B1), MedievalBase; 8 件首饰+卷轴配方挂它, 工作台=工案(手工/电动, 解锁=B1/C1 与裁缝台同步)。**教训: 新研究坐标必须跨tab全局避让(Food_SK Food_B4 (4,45) 曾撞车)**。
- **金鼠族研究终态**: 衣物 I(12,44)←B2A、II(16,44)←I+Uniform、IV(19,46)←II+Apparel_D1、III(20,46)←II+Armor_D1; 武器 I/II/III 在 Weapon_SK; Odyssey 引力防卫 Buildings_SK y14 x20/23/24。**武器研究拆分**: 登山杖/霰弹/技术冲锋枪/钩索等 14 件从 OA_RK_Apparel_* 拆到 OA_RK_Weapon_I/II。**材料**: 铀矿(Uranium=HSK原矿!)→贫铀合金 DepletedUranium, 按档补 Plastic/SyntheticFibers/Kevlar/ComponentSpacer/ComponentUltra。

---

## 5. 配方材料规则

- 恶魔布→HF;钛铁合金→StrongMetallic/RuggedMetallic;神奇皮毛→Leathery;金矿→GoldBar
- 科技档: 中世纪→ComponentMedieval;工业→ComponentIndustrial/Compaste/SyntheticFibers/Plastic/Rubber;太空→ComponentSpacer+Carbon/Electronics;超科技→ComponentUltra/Microchips
- 工作台: 衣物→鼠族裁缝台;太空甲/盾→先进纺织;骑士甲→电力裁缝+先进纺织;极致近战→机械武器台;枪→高级武器台;中世纪甲/近战→鼠族锻造台
- **研究台要求(requiredResearchBuilding,与 HSK 原版一致,六档)**: 科技按时代挂研究台门槛,对应 Core_SK 六档抽象基类——**原始(Neolithic)→ PrimitiveResearchBench 原始科技研究台(PrimitiveBase);中世纪(Medieval)→ SimpleResearchBench 基础研究台(MedievalBase);前工业(电力时代→石化前)→ 研究终端 LabTerminal(IndustrialBase=基础研究台+研究终端设施);后工业(石化→太空前)→ HiTechResearchBench 高级研究台(HitechBase=高级研究台+研究终端设施);太空(Spacer)→ MultiAnalyzer 多元分析仪(HitechMultiBase);极致(Ultra)→ LabStation 实验室工作站(HitechLabStationBase)**。
  - **机制坑(反编译确认)**: 光写 `requiredResearchBuilding` 不够——Core_SK 的 `SK.Patch_ResearchProjectDef_CanBeResearchedAt` Postfix 以 `SK.AdvancedResearchExtension.requiredResearchBuildings`(DefModExtension)列表为硬门槛,研究台不在列表内即拒绝;继承 MedievalBase/IndustrialBase 的节点该列表=[基础研究台,高级研究台]。→ 写法: 研究节点 Add `<requiredResearchBuilding>X</requiredResearchBuilding>` + `<modExtensions Inherit="False">` 重写 SK 扩展列表(保留 `<li Class="ResearchTreeSK.ResearchTreeSKModExtension" />`);设施走原版 `requiredResearchFacilities`(LabTerminal/MultiAnalyzer/LabStation)。实例: 工业工具II(RKHSK_Tools_Industrial2)按后工业档设 HiTechResearchBench+终端设施,覆盖列表为仅 HiTech;中世纪工具继承 MedievalBase、工业工具I 继承 IndustrialBase 即分别满足中世纪/前工业档。研究台建筑 defName 速查: SimpleResearchBench/HiTechResearchBench/MultiAnalyzer=游戏本体或 Core_SK;LabStation(实验室工作站)/LabTerminal(研究终端)=Core_SK;PrimitiveResearchBench=Core_SK。金鼠族科技如设研究台要求按此档位对应。
- **无抽奖**(51 号): 手写配方 MadeFromStuff 产物按 IsStuff 堆叠数加权随机取材质 → 材质槽只留金属/布料,大额固定材质折算进材质槽,小量(≤10%)点缀
- 锻造台配方 workSpeedStat=SmithingSpeed(62);配方挂载只用 recipeUsers 单路径(双写致重复解锁警告)
- **鼠邦(RKMS)/二战/金鼠族材料适配**: 鼠邦衣服武器+二战军服配方+金鼠族衣服武器按档位补 零部件(中世纪→ComponentMedieval 2-3、工业→ComponentIndustrial 3-4)+后石化组(仅 C3/14号档, 传说级 Armor_D1/Melee_Ultra 加 Compaste); 金矿/银矿统一→GoldBar/SilverBar 锭化。鼠邦无门槛装备按品类补研究(日常→B1、礼服装→B2A、职业→B2C、头饰面甲→C2、盔甲盾→Armor_B1、传说→Armor_D1、近战→Smithing、火枪→Gunsmithing、工业枪→GasOperation)。**Compaste(复合粘剂, Oil_Industry_C6 解锁)只进 C3/14号档, 中世纪/工业档别加(死锁)**。金鼠族 README 的 02_配方科技链.xml 实际不存在(已整合为 9 节点两级链, 勿按 README 复查)。
- **金鼠族建筑统一改 HSK 太空产业链材料**: 金鼠族 mod(`金鼠族 HSK版本`)全部建筑造价已统一改写为 **HSK 太空产业链材料**,清除原 `Steel`/`ComponentIndustrial`(工业料)。用到的太空料: `Plasteel`(铁钛合金锭,HSK 中即钛铁合金,defName 仍为 Plasteel,勿改成不存在的 def)+ `Titanium`(钛锭)+ `ComponentSpacer`(太空零件)+ `ComponentAdvanced`(高级组件)+ `AdvMechanism`(复杂机械装置)+ `Microchips`(大规模集成电路)+ `Electronics`(集成电路)+ `MagneticMaterial`(超强力磁铁)+ `DepletedUranium`(贫铀,HSK 铀原矿→贫铀精炼料,原 `Uranium` 一律改此);Odyssey 重力船链 `Gravcore`/`GravlitePanel` 属 Odyssey 自身链保留。覆盖: 主 mod `1.6/Defs/Things_Building/*.xml`(OA_RK_Zhuzi/Cloth_Processing/Tailor/Zhuzi_New/Cloth_Processing_B/CircuitRegulator/OberoniaCakeProducter/GeneBank/SkyWarmth_S)+ Odyssey `Mods/Odyssey/1.6/Defs/ThingDefs/Odyssey_Building*.xml`(8 建筑,另删 OARK_GravDataBeacon)。特例: `OA_RK_EMPInst` 一次性空投EMP 无 costList(靠配方制造,不改);`OA_RK_SkyWarmth_S` 原本即太空料。**防卫设施归类**: `OARK_TrapIED_Gravity`/`OARK_GravFieldGenerator` 设 `designationCategory=Defense`(RimWorld 自带防卫菜单,勿新建 Security)。改动须双目录同步。
- **材料科技档硬门控(2026-09-02,规则"材料档≥物品档")**: 全部非 Ludeon mod 的衣物/装甲/近战武器(stuff 类 Fabric/Metallic)按物品 techLevel 收紧配方材料——太空物品必须太空档(A级: 大力马/诺梅克斯/Micropel/乌木丝绸/太空合金),工业物品可用工业档(B级: 摇粒绒/人造丝/凯夫拉/钢系等)及以上,中世纪及以下物品不限;**皮革(品质分级)/木质不参与,远程武器与原版 Core/DLC 物品不参与**。实现: `HSK修复整合/Source/StuffTechGate.cs`(StaticConstructorOnStartup 一次性收紧 `fixedIngredientFilter`+`ingredients[].filter`+`defaultIngredientFilter`,账单不可绕过;反编译定论: recipeMaker 自动配方的 fixed filter 只按大类放行,XML 无法硬门控,故走 DLL);材料档位映射表 `HSK修复整合/Defs/StuffTechTiers.xml`(58 条,由 `_tmp/材料门控/gen_tier_xml.py` 基于 Unified.xml 普查+价格体系表 01_材料表.csv 生成,RK_Silk 人工 override=Industrial),档位缺失回退 stuff 自身 techLevel,再缺则放行+告警。配套: 蚕丝 RK_Silk 抬工业档(ThingDef+研究节点 RK_Sericulture 改挂 IndustrialBase);库林日常衣物 49 件 techLevel 降中世纪(`库林战狐HSK拓展/1.6/Patches/22_日常衣物降档.xml`),护甲/军装/研究服/工装/仪式武器保持工业。预览与普查脚本在 `_tmp/材料门控/`。
  - **零部件金属档位门控(2026-09-02,同规则下限门控)**: HSK 零部件配方是 Core_SK 显式 RecipeDef,金属槽用自建类别(SLDBar/HCMBar/PRSBar/USLDBar/USLDHBar/RARBar,成员档位经 Unified.xml 反查见 `_tmp/材料门控/sim_component_patch.py`),槽内混档=泄漏源(中世纪零件吃工业钢、工业导线吃铜金、极致零件吃工业铬钛等)。补丁 `HSK修复整合/Patches/14_零部件金属档位.xml` 对 8 条配方(MedievalBronze/导线×2/机械件×2/电子件/ComponentUltra)三处收口:金属槽 ingredient.filter Add `disallowedThingDefs`(收料硬门槛)+ fixedIngredientFilter(账单硬边界)+ 既有 defaultIngredientFilter 的 disallowedThingDefs 追加(机械件默认料青铜一并禁)。基线: MedievalBronze=Medieval、导线/机械件/电子件=Industrial、ComponentUltra=Spacer(贵金属槽 PRSBar 金银与配方固定点名料按设计保留);ComponentAdvanced 维持工业档(USLDBar 全员≥工业);原版 ComponentIndustrial(固定 12×Steel)不需门控。验证: `_tmp/材料门控/sim_component_patch.py`(19 操作全命中+终态断言+禁入清单与槽位成员×档位复算 0 遗漏)。
  - **金属锭科技档分类(2026-09-02,15 号,新增并存)**: 用户原想全替换锭类为科技档,因约 650 条配方实例(CE/HMC 弹药、机甲、枪械箱等)引用属性槽风险过高,改为**并存**——`HSK修复整合/Defs/ThingCategoryDefs/科技档金属分类.xml` 新增 5 个 ThingCategoryDef(RK_Bar_Neolithic/Medieval/Industrial/Spacer/Ultra,挂 Metallic 下),`Patches/15_金属锭科技分类.xml` 给 55 个金属锭各 Conditional 双路追加唯一档类(match=Add li / nomatch=整节点新建——**VMS 塑料系/阳极铝等原始 def 无 thingCategories 节点,类别是 HSK 加载期补丁建的,直接 Add 会报 Failed**)。分布: 工业 26/太空 16/中世纪 8/极致 3/原始 2(PureSilver+熟铁);属性锭类与全部配方槽未动,零部件 14 号补丁零影响。清单/生成/模拟: `_tmp/材料门控/{gen_tier_categories.py,sim_tier_categories.py}`;新锭入包后重跑生成器。

---

## 6. HSK CE 适配三件套(近战武器/远程武器/装备,2026-08-24 实测)

> **CE 目录位置**: 本机 CE 部署在 `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\CombatExtended`(packageId `CETeam.CombatExtended`,About 显示 Version 16.7.0.0,实际 DLL 版本以 Assembly 为准)。`Assemblies/` 下两个 DLL 分工不同: **`CombatExtended.dll` = CE 主体**(CompPawnGizmo / RaceUtil / StatWorker_MeleeDamageAverage / ArmorUtilityCE 等核心类都在这里,报错源也在此);**`CombatExtended.HSK.dll` = HSK 策略层**(ArmorPolicy/FriendlyFirePolicy/SuppressionPolicy/PlantBoundsPolicy 等,只做策略调整,**不涉及 CompPawnGizmo/HAR 注入**)。查 CE 报错/机制一律去这个目录,勿用 Steam 旧路径。
> 前提: 本环境 CE 是 **CombatExtended.HSK.dll**(HSK 兼容层),判定与原版 CE 不同。参照物: HSK_Generated 对 RatkinRaceHSK 的改写(`RatkinRaceHSK/1.6/Patches/HSK_Generated/`)、本 mod `Patches/04_CE适配.xml`(66 Replace + 10 Add)、`Defs/CE/Ammo/`。

### 6.1 近战武器

**报错**: info 卡打开近战武器时 `Trying to get stat MeleeDamageAverage/MeleeArmorPenetration from X which has no support for Combat Extended.`(GetValueUnfinalized/GetFinalDisplayValue)。根因: CE 的 `GetThingDefTools` 只认 `CombatExtended.ToolCE` 工具,原版 `Tool` 被过滤 → 空列表 → 报错。

**适配要求(每把近战武器必须)**:
1. **tools 全部 `<li Class="CombatExtended.ToolCE">`**,字段: label / capacities(Stab/Cut/Blunt 原版能力) / power / cooldownTime / 可选 chanceFactor / **`<armorPenetrationSharp>`(锐甲) + `<armorPenetrationBlunt>`(钝甲), 单位 = mm 装甲** / `<linkedBodyPartsGroup>`(Point/Edge/Handle/Head)。
   - **基准护甲穿透就是这两个值**(分锐/钝两档),材质/品质会再乘系数。参照: RK_Dagger sharp5/blunt4.5、RK_Fork sharp4.5/blunt4、RK_LightLance(极致) sharp4.8/blunt8.5。原版 DamageDef.armorPenetration 在本环境无意义(默认 0)。
2. **CE stat 直接写 ThingDef**(补丁 Add 会重复节点报错): statBases 加 `<Bulk>`(CE 体积,细剑 3/大剑 9 量级) + `<MeleeCounterParryBonus>`;equippedStatOffsets 加 `<MeleeCritChance>` / `<MeleeParryChance>` / `<Suppressability>`(负值)。
3. **单手/双手标记**(weaponTags): 单手 = `CE_OneHandedWeapon` + `RK_WeaponTag_OneHand` + `RK_WeaponTag_OneHandMelee`(可配盾);双手 = `RK_WeaponTag_TwoHand` + `RK_WeaponTag_TwoHandMelee`(**HSK CE 无 CE_TwoHandedWeapon tag,双手不写 CE 标记**)。战斗档位 tag 如 `MedievalMeleeAdvanced` 可共存,注意 weaponTags 要 `Inherit="false"`。
4. **不要做成"生存工具"属性**: 纯战斗武器不加 PlantWorkSpeed 等工具效果 stat(那些是 RK_Fork 类工具武器);工具武器(ToolCE 模式)才加。
5. 整合方式不变: ThingDef 继承原版 BaseMeleeWeapon_Sharp_Quality + `<recipeMaker Inherit="False" IsNull="True"/>` 关自动配方 + 外置 RecipeDef(研究/工作台/材料)。

**反编译定位法**(下次遇到 CE 判定问题): dnfile+dncil 读 `Mods/CombatExtended/Assemblies/CombatExtended.dll`,StatWorker_MeleeDamageAverage.GetValueUnfinalized → `GetThingDefTools`(判定链 `tools.NullOrEmpty()` → 返回 0;`tools.Any(谓词)` → 空则报错);ToolCE 过滤条件在 GetThingDefTools 调用的工具收集方法里(isinst Tool / DamageDef 映射)。

### 6.2 远程武器适配 CE 流程

1. **弹药选择(先查表,能映射就映射,不自造)**: 步枪/机枪/狙击→`AmmoSet_303British`;手枪/SMG→`AmmoSet_9x19mmPara`;霰弹→`AmmoSet_12Gauge`;反器材→`AmmoSet_50BMG`;榴弹→`AmmoSet_40x46mmGrenade`;电荷→`AmmoSet_6x24mmCharged`。本 mod 自定义弹药仅用于"鼠族无尽之息"能量武器(`Defs/CE/Ammo/Rifle/LightbulletA.xml`/`LightbulletB.xml`): 文件含 ThingCategoryDef(弹药分类) + `CombatExtended.AmmoSetDef`(ammoTypes + similarTo 关联原版弹种) + `AmmoDef`(Class="CombatExtended.AmmoDef", 继承 SpacerAmmoBase 等原版弹药基类, ammoClass 如 ArmorPiercing, cookOffProjectile) + ProjectileCE(BaseBulletCE 父类, projectile Class="CombatExtended.ProjectilePropertiesCE" 含 damageDef/速度等)。弹药贴图 `Textures/Things/Ammo/` 与 `Textures/Things/Projectile/`。
2. **武器补丁(04_CE适配.xml, PatchOperationReplace verbs/li 整个节点)**: verbClass→`CombatExtended.Verb_ShootCE`;`<Properties>`(VerbPropertiesCE) 写 recoilAmount / defaultProjectile(对应弹药弹丸) / warmupTime / range / burstShotCount / ticksBetweenBurstShots / soundCast / soundCastTail / muzzleFlashScale;`<AmmoUser>` 写 magazineSize(弹匣) / reloadTime / ammoSet;`<FireModes>` 写 aiUseBurstMode / aiAimMode;statBases **Add** `<Bulk>`(枪械体积, 按档位 10-50)。
3. **CE 弹药配方**: 弹药制作配方沿用 CE 原版 AmmoSet 的配方机制;自定义弹药若需制作配方,参照 CE 原版弹药 RecipeDef 结构(CE_AutoEnableCrafting tradeTag 自动启用)。

### 6.3 装备(衣物/护甲)适配 CE

1. **CE 负重(两处)**: statBases Add `<Bulk>`(物品体积) + `<WornBulk>`(穿着体积);equippedStatOffsets Add `<CarryBulk>` + `<CarryWeight>`(穿戴上身后的负重加成,如 xiaokb 30/30、tybb 100/100)。
2. **护甲值**: PatchOperationReplace `statBases/ArmorRating_Sharp` 与 `statBases/ArmorRating_Blunt` → **CE mm 值**(原版百分比值在 CE 下不适用;参照 Core_SK/HSK_Generated 的同类护甲,如普通军服 5/4、重甲按档位上调)。
3. 防具类 CE 无需额外判定字段;盾牌/盾甲还要看 22 号补丁(盾甲金属配方)与 CE CarryBulk 惯例。

### 6.4 HSK 弹药体系一览

**弹药分三类: ①本 mod 自创 CE 弹药;②RatkinRaceHSK 自带弹药;③全盘复用 CE 原版弹种(占绝大多数)。** 清单见 `docs/06_弹药/HSK弹药清单.csv`。

1. **本 mod 自创弹药(Defs/CE/Ammo/Rifle/)**: `AmmoSet_LightbulletA`(鼠族光储能弹,无尽之息 FBZX 用,弹丸 Bullet_LightbulletA 伤害25/锐穿80/钝穿140)+ `AmmoSet_LightbulletB`(光储能压缩机炮弹,黄昏毁灭者 huimie 用,弹丸 Bullet_LightbulletB 伤害15/锐穿60/钝穿100,爆炸伤害)。结构 = ThingCategoryDef(AmmoLightbulletA/B,parent=AmmoAdvanced) + AmmoSetDef(ammoTypes+similarTo=AmmoSet_6x24mmCharged) + AmmoDef(Class="CombatExtended.AmmoDef",ammoClass=ArmorPiercing) + ProjectileCE(Class="CombatExtended.ProjectilePropertiesCE") + RecipeDef(ChargeAmmoRecipeBase,500发/次: A=钛18+钢10+工业零件9, B=钛25+化工燃料40)。**金鼠族电荷武器(电荷狙击/不稳定充能系列)复用这两个弹种**,不另造。弹药贴图 Textures/Things/Ammo/ + Textures/Things/Projectile/。
2. **RatkinRaceHSK 自带弹药**: `RK_Ammo_WyvernFire`(铳枪飞龙弹,1.6/Defs/ThingsDefs/Weapon_HighTech.xml)——鼠族铳枪 RK_Weapon_Gunlance 的 CompEquippableAbilityReloadable 弹(6发/装填),爆炸半径2.9 炸弹伤害;配方在 recipeMaker(钢25+化工燃料30,研究=鼠族甲B1+冶金+龙焰,机械加工台)。**本 mod 补丁 75 号将其 thingCategories 改挂 Ammo(CE 弹药分类)**,注意 value 必须带 Inherit="false" 阻断基类 MortarShells 合并。
3. **复用 CE 原版弹种(补丁挂载,不自造)**: 鼠族HSK拓展内实际使用: AmmoSet_303British(8把二战枪+污染鼠族猎枪/狙击)、AmmoSet_9x19mmPara(冲锋手枪/双冲锋手枪/疯狂之钉/二战冲锋枪与手枪)、AmmoSet_40x46mmGrenade(火绒草手榴弹+骑士团黄金园榴弹铳)、AmmoSet_12Gauge(花菱草战壕枪)、AmmoSet_50BMG(灯心草后装速射炮)。金鼠族另用 AmmoSet_556x45mmNATO/762x39mmSoviet/762x51mmNATO/45ACP/145x114mm/RPG7Grenade(见其 README §6 弹药复用表,14 套自创贫化铀弹已于 2026-08-20 全删)。
4. **特殊**: 金鼠族钩索枪 OA_RK_GouSuo 用自定义 CE 投射物 Bullet_GouSuo_A(Mods/CombatExtended/1.6/Defs/CE_AmmoDefs/GouSuo.xml),**无 AmmoSet/AmmoUser,不耗弹**——工具武器,勿套弹药规则。
5. **弹药目录归属**: 全部弹药 def 都带 thingCategories,自定义弹归 AmmoLightbulletA/B(CE 分类);飞龙弹归 Ammo(补丁 75)。**改弹药分类/新增弹药时以 `docs/06_弹药/HSK弹药清单.csv` 为基准核对,避免与 CE 原版弹种重复定义。**

### 6.5 CompPawnGizmo 重复报错诊断(2026-08-26,美狐HSK拓展)

**报错现象**: 日志 `Alien_Miho has multiple CompPawnGizmo, duplicates has been deactivated. Please report this to the patch provider of 美狐HSK拓展 or CE team...`,栈顶 `CombatExtended.CompPawnGizmo:Initialize → Verse.ThingWithComps:InitializeComps → Verse.Pawn:PostMake`,触发于 **VEF(Vanilla Expanded Framework) `NewFactionSpawningUtility` 生成新派系**时(VEF.Factions → FactionGenerator.NewGeneratedFaction → TryGenerateNewLeader)。

**诊断结论(反编译 CombatExtended.dll 确认)**:
- 报错串实际在 **`CombatExtended.CompMilkableRenameable.Initialize`** 里(CE 的**通用单例 comp 重复检测**,不是 CompPawnGizmo 类本身——部署版 CompPawnGizmo 只有 CompGetGizmosExtra + ctors,无 Initialize;报错栈的 `CompPawnGizmo:Initialize` 是 MonoMod/Harmony PatchAll 动态改写后的显示)。逻辑: 遍历 `parent.comps`,`isinst CompPawnGizmo && c != this` 即判重复报错。
- **XML/def 定义层 100% 正确**: `Mods\Unified.xml`(运行时导出,含 DLL 注入)确认 Alien_Miho 的 comps 仅 1 个 CompPawnGizmo(li[2])+ 1 个 Suppressable + 1 个 ArmorDurability,modExtensions 仅 1 个 RacePropertiesExtensionCE(Humanoid)。CE 的 `RaceUtil.PatchHARs` 运行时给每个 HAR race 注入 RacePropertiesExtensionCE/Suppressable/CompPawnGizmo 各一份,注入逻辑**无去重**,但对单次启动只跑一次→def 层唯一。
- **根因 = CE + VEF(HAR) 交互的 pawn 实例层 bug**: `GenerateOrRedressPawnInternal`(红装/重建路径)对已存在 pawn 重复 PostMake/InitializeComps,comp 实例累积 → pawn 实例上出现第 2 个 CompPawnGizmo → 检测触发。**非美狐 def 问题,补丁无法在 XML 层根治**(报错本身建议向 CE team 报告)。
- 工作区 `美狐HSK拓展/HSK_1.6/Patches/ThingDefs_Races/AlienRace_Miho.xml` 的 remove `CompProperties_PawnGizmo` 是"安全网",但**匹配值有误**: 运行时注入的 comp 是 `compClass=CombatExtended.CompPawnGizmo`(comp 类),而补丁匹配 `compClass="CombatExtended.CompProperties_PawnGizmo"`(properties 类)→ remove 永不命中。不过 Unified.xml 证明 def 层本就唯一,故无害。
- 注意: 美狐 mod 的 `CE/` 目录(`CE/Patches/ThingDefs_Races/AlienRace_Miho.xml` 含 Suppressable×2 + RacePropertiesExtensionCE)在 1.6 下**不加载**(LoadFolders.xml v1.6 只加载 1.6/Cont/Seedsplease/Odyssey/HSK/HSK_1.6,不含 CE 目录)→ 不参与,勿误判为重复来源。
- **结论/建议**: 若 VEF 生成新派系不再报错则无需处理(重启游戏验证)。若仍报错,方向是 CE 侧或 VEF 兼容层,不在美狐 XML。更新 Unified.xml 方法: 启动游戏正常进图后自动导出。

---

## 7. 补丁职责(Patches/)

00 可穿戴 / 01 鼠族配方数值 / 02 维多利亚+科研 / 03 文化职位 / 04 CE / 05 追加服饰 / ~~06 种族兼容~~(2026-08-28 退役,见§7.11) / 07 ModAssistant / 08 VileMS / 09 属性重做 / 10-11 背包年龄 / **12 关自动配方(核心)** / 13 儿童真空服 / 14-16 材料 / 20 板甲平衡 / 21 隐藏儿童衣物 / 22 盾甲金属 / 24 锻造台木板 / 26 十字军长裙 / 27,29,35-39,44-45,58-59,61-62 工具(STL/CE) / 31 材料科技档 / 32 研究线 / 33 不可制造 / 34,53 材料基调/VMS / 40-41 头盔·图标 / **42 分区标签 43 偏好权重 44 文化接线(见§7.10,旧 44/45 工具号已退役不重号)** / 46,51 板甲/无抽奖 / 50 去除木板 / 52 制服 / 56 生存工具补强·鼠族 / 60,78 携带重量 / 63 背包镜像 / 73 装备属性精简 / 75 资源分类·鼠族弹药 / **79 种族穿戴锁(鼠族+金鼠族衣物全量锁种族,见§7.11)** / **91 衣物科技树拆分** / 92 鼠族异种生成限制

### 7.1 2026-08-22 通用修复迁移

与鼠族无关的修复补丁整体迁往 HSK修复整合(49-66 号,见该 mod Patches 与 About.xml 第 40 条): 弩箭25→49、大锤28→50、酿酒台拆箱28→51、憎恶屠宰42→52、工具属性47→53、工具 StatPart 48→54、锯木台胶合板49→55、艺术描述54→56、心灵长枪55→57、茅草床64→58、圣物木质65→59、CE 零护甲69→60、鱼类分类70→61、鲶鱼屠宰71→62、锯木台原木72→63、药草芦荟76→64、生存工具通用56→65、资源分类通用75→66(含 RK_Pollutants)。鼠族专属留本 mod(56 鼠族工具补强 / 75 飞龙弹分类)。

### 7.2 2026-08-22 HSK修复整合补丁按大类合并

Patches/ 66 个补丁合并为 9 个大类文件(01_科技与名字/02_地板与材质/03_熔炼/04_设施与建筑/05_工具/06_图标/07_配方与工作台/08_动物与植物/09_杂项),旧补丁备份 `_tmp/_backup/HSK修复整合_Patches_20260822/`。合并修复: ①00/54 FindMod+Sequence 改平铺+MayRequire;②09+53 TFJ_Tool_Multitool baseWorkStatFactors 冲突合并为并集 Replace(GeneralLaborSpeed=1.15);③43 重号归位。1096 个 xpath 核对无丢失。后续新增通用修复补丁直接并入对应大类文件,不再单开小文件。

### 7.3 2026-08-22 Vile鞣制1.6适配整合(43 号)

原独立 mod「Vile鞣制1.6适配」(local.vileleathertanningfix)整体并入 HSK修复整合。核心内容此前已由 34/35 号吸收(皮革分级 34 号用 LeathersB 覆盖原 S 级、动物屠宰生皮 35 号 112 种、中文名与奥德赛中文描述已迁入语言文件),本次补齐: ①08_动物与植物.xml 追加 28 种动物屠宰生皮映射(三段式 Conditional,84 个 Operation,动物总数 112→140);②02_地板与材质.xml 补 Leather_Human 英文 label 与 6 种奥德赛皮革英文描述(仅影响英文界面)。分级设计保持 34 号既有方案(替代金属硬皮革→LeathersB B+ 级),不引入原 mod 的 LeathersS 分类。原独立 mod 未加入 ModsConfig,无需再启用。

### 7.4 80 RAE 新增服饰

整合 Ratkin Apparel Expanded 27 件(必需8+推荐19,见 docs/05_文化服饰异种/RatkinIdeologyPlus文化适配调研.md)。Defs 在 `Defs/ThingDef/RKGW/RAE_新增27件.xml`(继承 RK_ApparelMakeableBase,80 补丁手写配方: 基础→Ratkin_Apparel_B1、女仆/披风/制服→B2A、执政官制服/安全绳→C1/C5、胸甲/矿工帽→Armor_B1、桂冠→C2);雨披 Hediff/Thought 在 RAE_Rainproof_HediffThought.xml;80 补丁兼管种族 apparelList/CE CarryWeight/SoakingWet 防雨。未整合: TestLabel 玩具/钻机/太空 4 件。

### 7.5 03 文化职位适配 v5(2026-08-24 重构)

- A 派系 tag 修复(RK_PlayerFactionBase→RatkinPlayer,RatkinFactionBase Empire→RatkinStory,RatkinFactionBadBase→RatkinStory);
- B **删除 PreceptRoleMultiBase 原版 roleApparelRequirements**(原版 6 条人类要求+旧 B1 基线与职位池叠加=AND 死锁);
- C 原版 10 职位+D VME 12 职位——**每职位 3 条 requirement: 头池(UpperHead+FullHead+Eyes+Mouth+Teeth)+身池(Torso+Neck+Shoulders+Arms+Legs+Hands+Feet+Waist)+全池兜底(last,承接 F 金鼠族追加)**。
- **机制铁律(反编译确认)**: ①roleApparelRequirements 多条是 AND 关系,同一职位内部位组重叠的多条要求=死锁(旧 v4 领袖 2 个 UpperHead 池→角色永远无法同时戴两顶帽→反复自动换装);②条件用 Conditional(match=Replace 整体节点/nomatch=Add 整节点),Replace 需 xpath 命中文件节点;③职位池衣服须覆盖对应 bodyPartGroups(脚本逐件验证)。22 职位衣物硬性错开共享率约 31%(仅绷带/护目镜/围裙等通用件共享)。
- E VME 人类服饰排除保留(Replace 后自然失效无害);F 金鼠族追加、G NPC 预置保留。**核心: 每个 li 带 allowedFactionCategoryTags=[RatkinStory,RatkinPlayer],仅鼠族文化被要求穿鼠族服饰。**

### 7.6 2026-08-22 91 衣物科技树拆分

B1/B2A/C1/C2 四个超载节点按品类拆为 9 个独立节点(每节点 5-10 件): 新增 B1b 头脸配饰/B2B 冬暖装/B2C 职业装备/B2D 软甲防御/C1M1~M4 军服ABCD区/C2b 军用大衣,挂 Apparel_SK tab、Y=44/45。主管线改名(B1→日常衣装、B2A→礼服装、C1→民用日常、C2→头部防御)。迁移 82 条配方 researchPrerequisite(Synthread 合成从 B2A 移 Fabrication、WR_TrenchCoat 从 C1M4 调 C2b)。验证: 最终 16 节点全部 5-10 件、配方均带显式 researchPrerequisite,无重复解锁风险。

### 7.7 2026-08-22 酒馆工具整合迁入(93-98 号)

酒馆工具整合HSK 的「大型工具整合」整体迁入本 mod(酒馆 mod 保留 JobEffects 动画/家具/门等,工具调整已删)。迁入: ①补丁 93 删镐斧(TFJ_Tool_Paxe)/94 删鼠族冗余工具(RK_Pickaxe/RK_Dagger)/95 锤子进化链(手锤→小锤→大锤)/96 钉头锤恢复纯武器(RK_Mace 去 tool 化)/97 工具科研前置(配方挂新研究节点+统一工作台)/98 JE 工具贴图(剪刀/电焊枪/骨锯→JobEffects);②科研节点 `Defs/ResearchDefs/Tools_Research.xml`(中世纪工具I-III+工业工具I-II,←Craft_B1/冶金学III/Metals_C3);③汉化 `ResearchProjectDef/Tools_Research.xml` + `ThingDef/Tools_Names.xml`(去"鼠族"前缀);④JE 贴图 `Textures/Things/JobEffects/Tools/`(Shears/Welder/Bonesaw)。About.xml loadAfter 加 `skyarkhangel.SurvivalToolsLite` 保证补丁在 STL 之后应用。

> 验证: `Mods\Unified.xml` 为最终态(不含自动配方),以游戏内实测为准。

### 7.x 2026-08-27 蓝图门控书籍化 v4(HSK工业科研大修)

`Source/BlueprintUnlockHSK.cs` v3→v4 全量重构: 删存储池/顶栏进度条/CompUseEffect;门控道具全部改**原版 Book 体系**(`BlueprintBook : Book` 子类,固定书名=def.label,override LabelNoCount/DescriptionDetailed/GenerateBook,右键"研读");系列 = 同 targetTech 的 N 本书(中世纪1/太空2/极致3/超凡4),集齐读完解锁,CanStartNow 门控。自定义 `JobDriver_StudyBlueprint`(Research 工作,不吃 joy,耗时=D7 公式 `(1+tier)*41.67/speed`)+ `WorkGiver_StudyBlueprint`(Book 扫描只读未读书)。坑: ①原版 Book.LabelNoCount 由 grammar title 决定,必须子类 override 才固定书名;②`Toils_Goto.GotoCell` 1.6 引 Unity.Mathematics,build.ps1 补引用;③`out Thing _` 非 C#5。XML: 111 本 `ThingDefs_BlueprintGates.xml` + `RK_BlueprintBooks` 分类 + JobDef/WorkGiverDef + 简英汉化(DefInjected+Keyed)。生成脚本 `_tmp/gen_blueprint_books_v4.py`/`gen_lang_blueprintbooks.py`。详见 `docs/蓝图书籍化改造方案_v3.md` §八实施记录。

### 7.8 2026-08-27 补丁按主题合并精简(工业科研大修 + 鼠族拓展)

用户要求"工具等同类补丁合并、减少 xml 数量"。合并**只搬不改**:逐字抽取源文件 `<Patch>` inner 内容按原顺序拼接,保持每个操作的全局相对执行顺序 → 行为零变化。备份 `_tmp/_backup/*_Patches_preMerge_20260827/`。

- **HSK工业科研大修 工具补丁 24→3**: `01_工具_创建与基础适配`(原 01/08/12/24/27/29/35/36/37/39/45)、`50_工具_材质与战斗适配`(原 50/51/56/58/59/61/62)、`93_工具_科研清理与数值`(原 93/94/95/97/98/A3)。安全性依据: ①工具补丁与非工具补丁(40/65/66/99/A1/A2/A4/A5/Blueprint/Patch)触及的 def **零交集**(indep 分析),故工具相对非工具可任意重排;②三簇按原文件名序连续切段,工具操作彼此相对顺序不变。**验证**: 全树模拟(原始集 vs 合并集应用到同一 pre-patch 基线)15222 个 def 最终态 **0 差异**、操作结果列表逐条一致;合并后 inner 内容与源拼接经注释/空白归一后 **字节相等**。Patches 37→16。
- **鼠族HSK拓展**: 因几乎全部补丁共享 鼠族/RK def(不能像工业那样跨非工具重排),**只做严格相邻段合并** → `81_二战军服与骑士团国适配`(原 81-87)、`100_军阀整合`(原 100-103)。共 11→2。工具文件 `35_剪刀与钉头锤工具` 与 `96_钉头锤恢复武器` **不可合并**: 二者之间的 50/51/62 等也改 `RK_Mace`,合并会打乱其相对顺序。Patches 根 55→46。
- **HSK修复整合**: 已在 08-22 合并为 9 大类;当日 `03_熔炼`/`05_工具` 通用内容**整体迁往 HSK工业科研大修**(A1_熔炼体系 / 27 / A3 等,备份 `_tmp/backup_20260827_industry_migrate/`),现存 8 文件即最优,不再拆并。
- 部署双目录已逐文件同步 + hash 核对(工业 16/16、鼠族 56/56,零差异);旧文件经 PowerShell SendToRecycleBin 移入回收站。
- 复现工具: `_tmp/merge_tool_patches.py` + `_tmp/run_merge_industry.py`/`run_merge_ratkin.py` + `_tmp/merge_equiv.py`(全树等价)。

> 后续:工业科研大修的工具适配修正一律并入上述三簇(优先 27 所在的第一簇),鼠族同系列军服/军阀并入对应合并文件,不再单开小 xml。

### 7.9 2026-08-27 工具/非鼠族武器→HSK锻造台 vs 鼠族独立武器→鼠族锻造台(近战归位)

用户规则:**所有工具 + 非鼠族近战武器一律走 HSK 自己的锻造台(`FueledSmithy`燃料/中世纪 + `ElectricSmithy`电/工业);带鼠族武器标签的独立武器只能在鼠族锻造台(`RK_FueledSmithy`/`RK_ElectricSmithy`)造**——**判定键 = `weaponTags` 里有无 `RK_WeaponTag_*`**(用户明确"武器标签的属于鼠族")。范围仅限**太空前**(Neolithic/Medieval/Industrial),**远程(枪械/弓弩/十字弩)走完全独立的第二套体系本次不动**;**护甲/服饰不动**(鼠族台上的护甲均为鼠族自身)。

静态盘点(`_tmp/smithy_*.py`/`melee_viol2.py` 读 `MissileGirl\Cache\Unified_Original.xml`,UTF-8 flat `<Defs>`):鼠族台↔HSK台**零重叠**;工具已由 97_/93_ 全量迁到 HSK 台(鼠族台无工具);非鼠族近战(Core_SK/Vile/原版/CE)本就在 HSK 台。唯二"标签是鼠族却挂在 HSK 台"的近战 = **西风骑士团 4 件**(`RK_KnightSword`/`RK_CavalrySword`/`RK_Lance`/`RK_TrenchMace`,配方 `RKHSKGW_Make_*` 在 81_ 里 recipeUsers=[FueledSmithy,ElectricSmithy])——与其 Favonius 姐妹组(`RKHSK_Make_Favonius_*` 已在 `RK_ElectricSmithy`)割裂两处 = 混乱源。**修法**: 81_ 内 4 处 recipeUsers 直改 `[RK_FueledSmithy, RK_ElectricSmithy]`(在 `PatchOperationAdd` 的 value 里,唯一命中、无 xpath 失效风险,与其余鼠族近战"燃料+电力双挂 + `researchPrerequisite=Smithing` 非无门槛故不触发重复解锁"模式一致)。

**未动项(有意)**: `RK_Weapon_Maul` 虽带 `RK_WeaponTag` 但 93_ 已按用户"锤子进化链"改名 **小锤(工具)**,按"工具→HSK"留在 `FueledSmithy`,**不随标签回鼠族台**;`OA_RK_Mountain_Stick*`(金鼠族登山杖)tech=**Spacer** 且无 `RK_WeaponTag` → 属太空档非鼠族 → 留 HSK(符合"太空后走 HSK")。`_tmp/verify_knight2.py` 确认 4 件现仅鼠族台;双目录 hash 一致(改前工作区=部署 8603849e→改后 22eb976b)。备份 `_tmp/81_bak_knightforge_20260827.xml`。

---

### 7.10 2026-08-27 文化服饰偏好系统(42/43/44 号,鼠族+金鼠族+美狐三 mod)

玩家可感知的"分区明确+派系偏好"体系,纯 XML 不碰基础衣物 def 本体。机制=反编译链:服装生成分布式全走 `TryRandomElementByWeight(ThingStuffPair.Commonality)`,而 Commonality ∝ `def.generateCommonality`(默认 1 非 100);派系隔离=`PawnKindDef.apparelTags` 白名单(CanUsePair any-match)。**唯一事实源=`docs/05_文化服饰异种/服饰分区盘点.csv`**(347 件去重清单+proposedZone 8 分区预判),改归属只改 CSV,重跑 `_tmp/gen_zone_patch.py` 生成三件套→`_tmp/run_zone_sim.py` 验证 0 失败→`_tmp/deploy_zone.py` 双目录 hash 部署:
- `42_分区标签注入.xml`(三 mod 各 1 份,346 op):给每件补 `Zone_Underwear/Base/Armor/Coat/Head/Face/Accessory/Back` 8 分区 tag(有 tags 直接 Add,无 tags Conditional 建块)。
- `43_服饰偏好权重.xml`(鼠族+金鼠族,165 op):按档设 `generateCommonality` 常见1.3/标准1.15/精英0.6/典礼0.3;已有刻意非 1 值(0.1 限定系)不覆盖;美狐不参与(其 kind 已有 requiredTag+layer 槽位制+alternateTagChoices 概率,勿重造)。
- `44_文化接线修正.xml`(鼠族,1 op):RatkinExoticRevolutionist 原无 apparelTags→掉原版人类衣池,补 `RK_ExoticMilitia/Worker/Farmer/Tier1/LightShield/LightArmor` 六池。
编号 42/43/44 避开既有 04_CE/05_追加/06_兼容;03 号职位补丁保持不动(其 requiredDefs 硬池与 43 权重共存,典礼件 0.3 会降低领袖池内王冠出现率属预期)。方案全文见 `docs/05_文化服饰异种/文化服饰偏好系统方案.md`。

### 7.11 2026-08-28 鼠族/金鼠族服饰种族穿戴锁补全(79 号 + 金鼠族 Apparel_Patch)

**机制(反编译 `AlienRaces/1.6/Assemblies/AlienRace.dll`)**:`RaceRestrictionSettings.CanWear(apparel, race)` — 任一种族 `alienRace/raceRestriction/apparelList` 里的条目在 `ThingDef_AlienRace.ResolveReferences` 时登记进**全局** `apparelRestricted` 集合,此后**只有白名单含它的种族能穿**(无 raceRestriction 的种族一律 false),随机生成同步剔除。∴ 把衣物写进 `Ratkin` 的 apparelList 就等于锁给鼠族,不必给每个别的种族补黑名单;金鼠族是 `Ratkin` ThingDef 下的异种人(`Ratkin_OA`),与鼠族共用同一张表。**本 mod 衣物只有 `_Thin` 贴图,锁种族后不再需要为其它体型补 `_Female/_Male` 变体(体型适配作废)**;`Patches/06_其他种族兼容.xml`(给 Rabbie/Horan/Dragonian 开洞,三 mod 均未安装=死补丁)已退役到 `_tmp/.trash/`。

- `79_鼠族专属穿戴限制.xml`:①原 59 件逐字保留;②新增 Conditional 块补 29 件——鼠邦(RKMS)上游注释掉的 15 顶帽子(纱帽/斗笠/暖帽/铁帽/簪子/围巾/蒲团帽…)+ 棉甲/堆锦甲/高级盔 + 儿童校服 + 8 件饰品(耳环/环玉佩/香囊/扳指/玉镯/长生锁/御守/金戒指)。失效的 Operation 级 `MayRequire` 改为 `PatchOperationConditional` + apparelList 存在性门控。
- `金鼠族 HSK版本/1.6/Patches/Apparel_Patch.xml`:补漏 `OARK_Apparel_GravShieldBelt`(def 在 `Mods/Odyssey/1.6`,li 级 `MayRequire="Ludeon.RimWorld.Odyssey"`,照抄既有 `OA_RK_New_Belt_B` 写法)。
- **故意不锁**:①`RK_EarCostume` 小鼠耳朵头饰=给人类戴的伪装头饰(tag `NonRatkin`);②`RKMS_Scroll.xml` 8 部秘籍卷轴(百工典/百株杂粮集/渡世救人集/奇石谱/饕餮经/天赋论/止戈总要/诸方博物志)=功能书籍非衣物,贴图单一通用;③西风 4 件装甲(`RKHSK_Favonius_*`)按"通用装甲人类也可穿"设计写进 00 号白名单,**但白名单语义就是"仅鼠族可穿"→与文档原意冲突,本轮维持锁定**,要还给人类穿须改挂 Human 白名单并验贴图。
- 终态:鼠族系 313 件服饰 305 件已锁(余 8 = 秘籍卷轴);03 号职位 108 个引用这批帽子的 roleApparelRequirements 块全部 `allowedFactionCategoryTags=RatkinStory/RatkinPlayer`,锁种族不会让 NPC 卡在"着装未满足"。验证:`_tmp/scan_race_apparel.py`(读 Cache/Unified.xml 盘点覆盖差集)+ `_tmp/verify_race_lock.py`(lxml 模拟两补丁→命中率/覆盖率/死引用 0)。

### 7.12 2026-08-28 酒馆(Tav)纺织线并入 HSK 纺织机 + 装饰改建筑直建

**诉求**:酒馆 mod 的装饰台与 HSK 工业科研大修重复劳动——凡是能在建筑菜单直接造的物件不该再占工作台。

**落地铁律(通用)**:整合 mod 里**带 `costList`/`costStuffCount` 的可建造物件**(家具、地毯、旗帜、奖杯、厨房装饰)一律走建筑菜单直接建造,**不保留同名工作台配方**(双路径 = 冗余 + 挤占账单列表);只有真正需要工时/技能/混合材料的**中间材料**才留在工作台。

**具体改动**(全部直接改 `酒馆工具整合HSK/1.6/Defs` 源,无新增补丁;该 mod 自包含,全工作区+部署 Mods 扫描无外部引用):
- `TavMake_Padding` 垫料:唯一保留的工作台配方,`recipeUsers` 由 手缝台/电动缝纫台/装饰台 → **`TableLoom`**(HSK 共享纺织机,织绸 `RK_WeaveSilk` 同处),`workSpeedStat` GeneralLaborSpeed → **TailoringSpeed**,补 **`researchPrerequisite=Apparel_B1`**(与 TableLoom 解锁同档)。
- 删除 11 条配方 `Make_Tav_{KitchenAdd, DecTrophy, Dec2x1/3x1/4x1Flags, StrawRugA, RugA, RugB, RugSquareA, RugSquareB, RugSquare4X2A}` 及两条抽象基类 `TavMakeDecorBase` / `TavMakeRugBase`。
- 删除 `Tav_TableDecor`(装饰台 ThingDef)、`DoBills_Tav_TableDecor`(WorkGiver)、二级菜单「酒馆厨房」中的装饰台条目;软椅/软凳/扶手椅/软床/双人软床/奖杯/装饰地毯等下游消耗方无需改动(原本就带 costList 直建)。
- 汉化同步:`1.6/Languages/.../DefInjected/{RecipeDef/Recipes_All, ThingDef/Buildings_Production, WorkGiverDef/WorkGivers}.xml` 与根 `Languages/.../WorkGiverDef/HSK补译.xml` 相应键一并删除(悬空 DefInjected 键指向已不存在的 def)。
- **用户追加口径(08-28 复核)**:①**不做材料简化**——装饰地毯仍耗垫料、旗帜仍耗布,造价原样保留;②纺织机 `TableLoom` 承担的两件纺织活 = 酒馆垫料 `TavMake_Padding` + 工业大修织绸 `RK_WeaveSilk`(单工作台、两配方,无 duplicate-unlock 风险);③装饰台 def 是**用户另作他用**才删的,原 def 全文留在 `_tmp/backup_20260828_tav_textile/`,需要时可取回。
- **顺带查出的真 bug**:`HSK工业科研大修/1.6/Defs/Recipes_RK_CraftChains.xml` 有 6 条配方的 `<ingredients>` **整块重复插入**(养蚕收茧 4 块含 2 重复、织丝绸/沤纸浆/压纸/誊传说书/誊技术书各 2 块含 1 重复)→ 实际材料需求翻倍。已按"逐 RecipeDef 定位、按外层 `</li>` 切条目、去重保序"修复(正则坑:条目内含 `<li>材质名</li>`,用 `(?:(?!</li>).)*?` 匹配外层条目会静默失配,须改为「不嵌套 `<li>`」的判据)。养蚕收茧去重后 = 蚕种 1 + 稻草 12 → 生丝 4;织绸 = 生丝 4 → 丝绸 1。
- 文档同步:工作台造价总表 142→141、产业线总览删装饰台行、mod整合调查记录就地标注、导航计数更新。

**踩坑**:批量正则删 `<RecipeDef>` 块时,以「块内特征串」定位再取到 `</RecipeDef>` 会**跨块吞并**(一次误删整段装饰配方并留下悬空 `ParentName` 基类引用)。改法:整份重写目标文件,或先按 `<defName>` 精确切块再过滤。删除后必跑「全 XML parse + 注释外残留引用扫描」双校验。

### 7.13 2026-08-29 HSK修复整合新增房间角色:室内游泳池 + 电力适配间

**诉求**:用户要两个新房间判定——①"室内游泳池";②"电力适配间"(用户原话"电力适配间"指**放发电设施及电池的房间**,经确认是新房间类型而非既有建筑;全环境 grep 确认无同名建筑)。作为参照先反编译确认了既有判定:厕所=DBH `PublicBathroom`(浴缸≥1或马桶≥2)/`PrivateBathroom`(已指派卫浴 4000 分),阅读室=书籍拓展HSK `VBE_Library`(书柜+书 ×13.5,房间含 Laboratory 工作台判 0),1.6 原版自带 `RoomStatDef ReadingBonus`。

**为何必须写 DLL**:原版 ~22 个 RoomRoleWorker + DBH(3)+VBE(1)+Stratum(1)+Core_SK(1) 全部硬编码,无可参数化的"房间含某建筑"通用 worker,纯 XML 无法实现。

**落地**(全部在 `HSK修复整合`,packageId `local.hskfixpack`):
- `Source/RoomRoleIndoorPool.cs` — `RoomRoleIndoorPool.RoomRoleWorker_IndoorPool`:封闭房间(显式排除 `PsychologicallyOutdoors`,原版本就只对 ProperRoom 打分)内出现"卫生→水池娱乐"建筑即判:游泳池 `DBHSwimmingPool` 150 分/台、热水浴缸 `HotTub` 100 分/台。def 用 `GetNamedSilentFail` 按名解析,未装 DBH 得分恒 0,无需 FindMod 门控。
- `Source/RoomRolePowerRoom.cs` — `RoomRolePowerRoom.RoomRoleWorker_PowerRoom`:**通用识别不硬编码 defName**——发电=挂 `CompPowerTrader` 且 `Props.PowerConsumption < 0`(CompPowerPlant 全系子类均如此申报;1.6 起 `basePowerConsumption` 已私有,公共读取口是 `PowerConsumption`,原版 CompPowerPlant 同样按 <0 判发电),电池=挂 `CompPowerBattery`(含子类如裂变电池);合计 ≥2 台才触发,每台 100 分——避免"库房一块电池把卧室变成电力适配间"。变压器/开关/用电器不计。
- 两者均覆写 `GetScoreDeltaIfBuildingPlaced`(原版 `Room.GetRoomRoleIfBuildingPlaced` 按 `GetScore+delta` 取 MaxBy,delta=**增量**不是总分,摆放预览实时反映)。
- `Defs/RoomRoleDefs/RoomRoles.xml`:RoomRoleDef `HSKF_IndoorPool` / `HSKF_PowerRoom`(relatedStats 含 Beauty/Cleanliness/Wealth/Space/Impressiveness)。
- 汉化:`Languages/ChineseSimplified (简体中文)/DefInjected/RoomRoleDef/RoomRoles_新增房间角色.xml`(室内游泳池/电力适配间)。
- 构建沿用 `build.ps1`(csc v4.0.30319=**C#5,禁用 `?.`/`$""`/插值**),DLL 增量并入 `HSKFixPack.dll`,双目录 md5 核对一致;顺带按 §5 铁律清掉部署 Assemblies 里两枚同源 `.bak` 残留。

**待验证**:需重启游戏生效(Defs+DLL 均启动期加载);验证法=房间统计面板看角色名,或放置泳池/电池观察预览角色变化。

### 7.14 2026-08-30 工作动画(JobEffects)从酒馆工具整合HSK拆出独立 mod
- 新 mod `工作动画HSK`(packageId **meathax.ShowMeYourTools** 沿用工坊原 ID,Steam 已下架): DLL(Assemblies/JobEffects.dll)+Source+Defs(JobTools/Flecks/Sounds)+补丁(Cleaning/Construction/PlantWork/Compat_AppliancesExpanded/Compat_CombatExtended)+贴图(Things/JobEffects、Things/Mote/JobEffects)+两套汉化(JobToolDef/JobTools、ThoughtDef/Thoughts_Memory 与根 Languages AI补译)。
- 酒馆侧只剩 Simple Doors+Tavern(补丁 99_/SimpleDoors/Stoves/TallowAsFuel/Tavern_HSKAdapt);标记补丁 id 改 meathax.ShowMeYourTools,About 删动画兼容 mod 尾巴与 CE。
- 排序判定: 新 mod 补丁只 Add/Replace **自有** JobToolDef 节点,对酒馆/HSK/MO 工作台是 workbenchDefs 字符串引用(终态解析)→不需要 loadAfter 酒馆;修复整合(loadAfter 全部内容 mod)与特性拓展补了 meathax 条目。工业大修 93号内嵌完整 JobToolDef(引用 RK_*),工业大修本就 loadAfter 全部种族内容 mod,链已成立。
- 验证: lxml 模拟 59 op 0 失败;游戏内 Unified.xml(UTF-16) JobToolDef=57(基础 52+CE 台 1+HSK 厨房段等,MO 台未启用正确落空);ModLister NativeAddon 含 meathax.ShowMeYourTools;check_order2.py 0 违规。冷启动仅存两条已知噪音: UXE 导出路径 + **BlueprintUnlockHSK.BlueprintUnlockInit 静态构造崩(Param "pawn" not found,系 08-30 矿井防虫补丁用错参数名,与本拆分无关,待原会话修)**。

### 7.15 2026-08-30 工作动画升级 fork 基线 + 去 SMYH 硬依赖
- 采纳 fork「Show Me Your Tools - Forked」(工坊 3774821567,作者 astryl,meathax 授权): 纯性能版(Defs/Patches/贴图零差异),新增 Perf/PawnToolState/ToolWarmup/SkygazePatch/Diagnostics + PERF_NOTES.md;单字典 per-pawn 状态类、job 覆盖位图预筛、材质预热离渲染路径、去装箱/去反射;本地原 Source 备份 `_tmp/工作动画拆分_隔离_20260830/old_source_pre_fork/`。
- 本地定制(重打在 fork 上): Settings `HandsOnTools/DrawArms` 去掉 `SmyhActive &&`(SMYH 不在场也用内置手); Mod.cs 手/前臂设置区块常显; ToolAnimator.HandTexture 无 SMYH 时回退 `ContentFinder "UI/JE_Hand"`; 新增自制贴图 `1.6/Textures/UI/JE_Hand.png`(64² 白色握拳供 Cutout 按肤色染色,`_tmp/工作动画_鼠族适配/` 有生成脚本与示意)。
- 鼠族适配核实: RatkinRaceHSK bodyTypes 仅 Thin(+Baby/Child),ArmRenderer 白名单已含 Thin/Child、ShoulderShape 已标定→前臂天然可用;JE_ArmSleeve.png 在酒馆整合时期被裁掉,本次从 fork 补回。About/PublishedFileId/封面已切 fork(3774821567);csproj Harmony 引用改为本地 Mods\Harmony 路径。dotnet build 0 错误,双目录 hash 一致,**未启动游戏验证(用户要求暂缓)**。

### 7.16 2026-09-06 金鼠族NPC裸体(路过的勘探队)= apparelMoney 未适配 HSK 物价(金鼠10号补丁 + _Thin_贴图补齐)

**症状**: 金鼠族"路过的勘探队"(`OARK_ProspectingTeam`,4×`OA_RK_Court_Member_Exploration`,招待 60 天)到达即全裸。
**根因(生成侧,非渲染)**: 衣池 tag `OARatkin_CourtApparel` 23 件终态市值 ≈2075~7909 银(HSK 材料化: DevilstrandCloth 45×10 + ElectronicComponents 3×240 + Electronics 2×550…),而全部 OA pawnKind 的 apparelMoney 仍是原版值(宫廷/勘探 900~1800、平民缺省 100)。原版 `PawnApparelGenerator.CanUsePair` 第一条 `pair.Price > moneyLeft` 把池子筛空 → 零衣物生成。**整个派系系统性裸体**,勘探队只是最显眼(常驻 60 天)。
- 排除项: 92 号异种白名单含 `OAGene_SnowRatkin` ✓;Ratkin apparelList(653 项)含全部 OA 衣 ✓;faction `apparelStuffFilter` 按 stuffCategories(Fabric/Leathery)放行 ✓;StuffTechGate 只收紧配方过滤器不碰生成 ✓;主体衣物 `_Thin_` 四向完整(42 号分区补丁只 Add tag,不破坏 pawnKind 匹配)。
- **修法(`金鼠族 HSK版本/1.6/Patches/HSK适配/10_NPC服饰预算适配.xml`,13 op)**: 按"池下限×1.1 ≤ min"给 13 个 kind 抬预算(Court_Member/Exploration/Guard 2400~5600、Elite 2600~7000/8500、Court_B/C 2600~6000/6500、Noble_A 2400~4700、Colonist_B/C 2600~4600、Assault_B/C/D 4900~7000);**不动衣物造价**(玩家经济用户定)。已达标不动: Traveller/Court_Member_D/Noble_B~F/Assault_A/E/Guard_Captain(Noble_E/F 是 `9999999` 单值写法=无限)。lxml 模拟 13 op 全命中。
- **并行会话撞车(13:51:05 双目录同步写 PawnKinds.xml)**: 对方给 Colonist_A 加 `specificApparelRequirements`(Torso+OnSkin+`requiredTag OARatkin_Apparel`,按 Commonality 直选**绕过价格门**的硬保底)+750~1200。处置=保留对方改动,我方补丁撤掉 Colonist_A 条目,13+1 互补。Apparel_Armor_New.xml 同刻改动系汉化流描述排版,无关。
- **顺带补齐 17 件 `_Thin_` 方向贴图(64 文件×2 目录)**: 帽子/背板/护甲帽(`OA_RK_Hat_A~E`、`New_Hat_A~E`、`Armor_Hat_A/B`、`Component_A/B/C`)只有 plain 无 `_Thin_`——反编译 AlienRace `ApparelGraphicUtility.GetPath`: `_Thin_` 缺失时 `bodyTypeFallback` 只会把 bodyType 后缀换回 Thin 原地打转,**无 plain 回退**;预算修好后这些件会生成但不渲染+缺贴图报错。plain→`_Thin_` 直拷即作者原图;雪鸦斗篷补 `_Thin_west`(镜像 `_Thin_east`)、雪鼠发夹补 n/s/e。复跑审计双目录 0 缺失。
- 排查脚本: `_tmp/勘探队裸体20260906/`(check_unified/pool_price/kind_pool_table/audit_oa_textures.py)。效果由用户进游戏查看。


## 8. 二级菜单(ArchitectSense 子分类)

**二级菜单 = 建筑师菜单里一级分类(家具/结构/生产/辅助)下的子分类折叠**(由 HSK 内置 ArchitectSense.dll 提供,不用装 mod)。详细机制/写法模板/踩坑 → **见 `docs/02_建筑与二级菜单/二级菜单说明.md`**。

一句话要点: 建筑靠 `designationCategory` 进一级分类,再靠 `ArchitectSense.DesignationSubCategoryDef` 的 `<defNames>` 挂进二级菜单;子分类 designationCategory 必须与建筑一致;结构建筑(柱子/门)也要建子分类。酒馆家具/辅助/生产/结构子分类均已整合。

---

## 9. 性能铁律(高频 tick 优化)

> **性能第一(2026-08-21 用户要求)**: 所有在高频 tick 中刷新或判定的逻辑,必须清理、整理并优化成低频限定条件逻辑,减少性能损耗;与功能正确性冲突时,先保证方案低耗可行,再谈功能细节。

- **降频**: 普通逻辑一律低频轮询(tick 计数取模,如每 60/120/300 ticks),绝不每 tick 全量执行;仅移动/战斗等确实必须的逻辑保持高频。
- **事件驱动**: 能用事件/回调触发的刷新绝不轮询;Tick 内只做轻量"是否需要重算"判定。
- **提前短路**: 判定先过最便宜的条件(无相关 pawn/建筑/comp 激活 → 直接 return),昂贵遍历/计算放最后。
- **缓存复用**: 高频读取的昂贵结果(路径/材质/属性/字典查找)缓存,仅在低频校验点或事件时失效重建。
- **禁每 tick 遍历**: 不每 tick 遍历 map/全部 pawn/所有建筑;用索引、缓存列表或把遍历降频到秒级。
- **写 DLL 自查**: Tick()/CompTick()/GameComponentTick() 内出现遍历、反射、LINQ 全量查询、频繁字符串拼接或字典构造 → 必须先按本准则重构再提交。
