# 绮罗 Kiiro (Ancot HAR 种族族群) → HSK 整合方案

> 源: 工坊 7 件(作者 Ancot 主族/库/基因/故事/表情 + Chougou 中世纪 + 快乐柠萌茶 CE 补丁)。
> 模式: 对齐 `docs/06_种族整合/库林Kurin_HSK整合方案.md` 全套深度 + `HSK-Milira-Race`(同作者 Ancot 已全量 HSK 化, 现成模板)。
> **定位**: 猫娘(HAR)种族, 与 Milira(兔)/库林/美狐/阿丽莎 **并存**, 不退役任何既有种族。绮罗自带 `IfModActive="Ancot.MiliraRace"` 互认边, Milira 侧已内置 `1.6/Mods/Ancot.KiiroRace/` 跨族补丁(KiiroApparel/KiiroFriendly), 只要保留 `Ancot.KiiroRace` 原 packageId 即自动激活双向穿戴/友邦。
> **实装状态**: ✅ 阶段1 收编 · ✅ 阶段2 科研融网格(lxml 模拟 104 op/0 fail, 已同步) · ✅ 阶段3 CE 兼容性核对(自带 KiiroCEPatch + KME CE 门控补丁面向标准 CE, HSK-CE 保留这些类, PowerArmor/基因/故事 子件 IfModActive 门控, 156 目标 xpath 仅 PowerArmor 件命中门控外部不悬空→**无需重写, 数值游戏内平衡**) · ⬜ 阶段4-7 见 §三(多为运行时候选项/自带满足)。

---

## 〇、7 件清单(原 packageId; **最终合并为一个 mod 绮罗HSK**)

> **合并形态(2026-09-08)**: 应用户要求 7 件→ **单 mod `绮罗HSK`**(packageId 取 `Ancot.KiiroRace`,保留以维持 Milira `IfModActive` 跨族补丁自动激活)。结构=子包拼接 `race/lib/gene/story/facial/medieval/ce` + 一份合并 `LoadFolders.xml`(自我门控已折叠恒载, 外部 DLC/CE/MO/PowerArmor 门控保留), DLL 各在子包 `1.6/Assemblies`(无重名)。旧 7 个 `绮罗*` 移 `_tmp/合并前隔离_绮罗7件_20260908/`(勿删)。**连带**: AncotLibrary 并入 → Milira `modDependencies`/`loadAfter` 的 `Ancot.AncotLibrary` 改指 `Ancot.KiiroRace`(该 DLL 现由合并包提供, Milira 须 loadAfter 它), Milira LoadFolders 的 Story 跨族门控补 `Ancot.KiiroRace` 一条(Milira 工作区已改; 尚未部署到 Mods)。合并包 `incompatibleWith` 其余 6 原 ID 防双启用。

| 工坊ID | packageId | 工作区/Mods 文件夹 | DLL | 角色 |
|---|---|---|---|---|
| 2988801276 | Ancot.AncotLibrary | 绮罗基础库HSK | AncotLibrary.dll | 框架库(Harmony, 族群硬依赖; Milira 亦依赖它) |
| 2988200143 | Ancot.KiiroRace | 绮罗种族HSK | Kiiro.dll | 主族(HAR, 女-only, 27 研究/5 派系/~130 服饰/34 武器) |
| 2988200826 | Ancot.KiiroRaceGenePatch | 绮罗基因补丁HSK | — | KiiroXenotype + 自定义基因(Biotech) |
| 3036480100 | Ancot.KiiroStoryEventsExpanded | 绮罗故事事件HSK | Kiiro_Event.dll | 委托/事件/14 特种商人/额外研究 |
| 2991489003 | Ancot.KiiroFacialAnimation | 绮罗表情动画HSK | — | Nals.FacialAnimation 适配(纯 def+贴图) |
| 3552593338 | Chougou.KiiroMedievalExpansion | 绮罗中世纪扩展HSK | — | 7 件中世纪甲盔(依赖 MO/VFE.Medieval2, 均未装→扩展多门控) |
| 3104965497 | Lemontea.KiiroCEPatch | 绮罗CE补丁HSK | KiiroCE.dll | 面向**原版 CE**的武器/护甲/PawnKind CE 化 |

家族链(排序边仅 loadAfter): `Lib < Race < {Gene < Story, Facial, Medieval} < CEPatch`, 全 loadAfter `skyarkhangel.HSK` + `DimonSever000.ModIndicator.Specific`。Tarjan 0 环、loadAfter 依赖全可解析。

---

## 一、1.6 兼容 / 收口 —— ✅ 已完成(2026-09-08)

- 逐件 copytree 进工作区 + Mods, ignore `1.4/1.5/*.code-workspace`, **保 packageId**(防 Milira `IfModActive` 与家族互引断链)。双目录 0 差异(hash)。
- About: supportedVersions→1.6, modDependencies 补 Harmony+Core SK(留原 HAR/AncotLibrary/FA), loadAfter 补 Core SK + ModIndicator(压尾) + 家族内部边;CEPatch 原无 modDependencies 已补。
- 每件 `1.6/Patches/99_ModAssistant标记.xml`(NativeAddon 注入 + `MayRequire="DimonSever000.ModIndicator.Specific"`, 沿用 Kurin 模式)。根 touch `HSK`。
- **修 GenePatch 隐性缺陷**: 它**无 LoadFolders.xml** 且 def 全在 `1.6/` 下 → 直接收编会静默不加载。已建 `<v1.6><li>/</li><li>1.6</li>` (同带 root Textures)。其余件 LoadFolders 收口为单 v1.6。
- ⚠️ 部署后同 packageId 若与工坊订阅版撞 ID: 用户须**停用工坊 7 件**, 只留本地(当前 ModsConfig 无 Ancot/Kiiro, 未订阅启用)。

---

## 二、待用户拍板的决策点(阶段2-7)

| # | 决策 | 选项 | 建议 |
|---|---|---|---|
| D1 | 科技排布 | A 保留 `Kiiro_ResearchTab` 自有链融主网格 / B 全拆并入 HSK 现有档 | **A(仿 Kurin)**: 入口 Medieval 段挂中世纪研究档, 工业节点(`Kiiro_AutoLauncher/Machining/TRGun/PrototypeGun/Apparel_IV/Vacsuit`)抬 Industrial 档, 逐节点补 `SK.AdvancedResearchExtension` 硬门槛 + 六档研究台 |
| D2 | 越级前置 | 工业节点裸引 vanilla `Machining/MicroelectronicsBasics/OrbitalTech`(Core_SK 已抬档) | 换挂 HSK 同档节点或补门控, 消 `lower techlevel` 连锁告警(§10) |
| D3 | 坐标 | 全 27+Story 节点浮点 pos(ResearchTreeSK RoundToInt 撞车) | 逐节点全树占位普查(`_tmp/census_research_pos.py`)落唯一整数格, 严格大于前置 x |
| D4 | CE 策略 | A 沿用 `Lemontea.KiiroCEPatch`(面向原版CE) / B 按 §7 重写 HSK 版 | 先**静态核对**该补丁在 `CombatExtended.HSK.dll` 下 verb/ammo/class 可解析(多数标准弹种 303British/12Gauge/45ACP/762NATO/FN57/Arrow 已在 HSK 存在→干净); 自创弹种(PT/Grenade/Rocket/毒箭)按 §7"闲置即删/复用现成"复核 |
| D5 | 中世纪扩展甲 | KME 7 件用 vanilla % 护甲, 仅 1 盔有 CE mm | HSK 下补 mm 值或门控; 其 MO 门控文件夹(未装)不加载, 只保 always-on 穿戴白名单 |
| D6 | 异种池 | `KiiroXenotype` 仅 GenePatch 提供, `onlyUseRaceRestrictedXenotypes=true` | 校验 `onlyHaveRaceRestrictedGenesEndo` 不把 KiiroXenotype 自带基因判非法回退 Baseliner; 派系 xenotypeSet 落 `KiiroXenotype=1`(GenePatch 末 op 已改) |
| D7 | 穿戴锁 | `raceRestriction.apparelList` 全局独占白名单(~130 件) | 现状即"仅绮罗穿绮罗装"; 要人类/它族可穿须逐族补列; Kiiro↔Milira 靠两侧 whiteApparelList 互列(Milira 侧已内置) |

---

## 三、深度整合改造清单(⬜ 待做, 逐阶段 patch_simulator 验)

**3.2 科研融网格**: ✅ 已完成(`20_科研融网格HSK.xml`, 主族 26 件 + 故事 `Kiiro_Furniture`, `Kiiro_FurnitureII` 源已注释故不处理)。保 `Kiiro_ResearchTab`; 每节点 Add `SK.AdvancedResearchExtension`(requiredResearchBuildings=[Simple,HiTech])+`ResearchTreeSKModExtension`; Medieval→`requiredResearchBuilding=SimpleResearchBench`、Industrial→`HiTechResearchBench`(原已带 HiTech 的 4 件走 `Conditional/nomatch` 不重复)。坐标落空行 **Y=90..116**(当前网格最大 Y=84, 90+ 全空)、**X=10+依赖深度**(前置严格左; Vacsuit→X16 因其外部前置 OrbitalTech 在 X15)。lxml 模拟 104 op/0 fail。外部前置 Machining/MicroelectronicsBasics/OrbitalTech=Industrial、ComplexFurniture=Medieval **均不高于本节点档→实测 0 越级**(D2/D3 免做)。**待真机**: ResearchTreeSK 全局坐标唯一性以最终 Unified.xml 复核(90+ 段若日后别 mod 抢占则微调)。

**3.3 CE(HSK)**: ✅ 兼容性核对通过, **不重写**。主族武器/服饰 CE 化由自带 `Lemontea.KiiroCEPatch`(MakeGunCECompatible×29/Verb_ShootCE×24/ToolCE×57/标准弹种 303British/12Gauge/45ACP/762NATO/FN57/Arrow 均 HSK 存在)承担, 其 DLL `KiiroCE.dll` 一并本地化; KME 中世纪甲由其 `Mods/CombatExtended/Patches/CombatExtended.xml`(CE 在场即加载)补 mm ArmorRating+StuffEffectMultiplierArmor; PowerArmor/基因/故事子件 `IfModActive` 门控(未装即不加载不报悬空)。**待真机(D4)**: CE 手感/弹种自创签名弹(AmmoSet_KiiroPT*/毒箭/Rocket)是否保留按 §7"复用现成+闲置即删"由用户拍板, 数值平衡。

**3.4 派系/异种池**: 5 派系进世界生成, `Kiiro_Faction` weight 1.2 → 与 Milira/Asari 错开降档防刷屏; KiiroXenotype 过异种池白名单铁律(不追"智人种%"补基因, §3 末条)。

**3.5 文化/服饰分区**: 自定义 `Kiiro_Underwear/Pants/OnSkin/Bag` layer 对齐 HSK 分区; 服饰分布 `generateCommonality` 加权; Kiiro↔Milira 穿戴锁互列。

**3.6 汉化**: 主族/基因/故事/CE 均**自带 ChineseSimplified**(审计确认)→ 以自带为主, 缺口 DefInjected 补; 走本体 Languages 路由。

**3.7 RimQuest 委托**: 族色委托(清剿型)指向 `Kiiro_Faction`, 发布者/特产包增量, 仿 Kurin; Story 自带 24 QuestScript 是原生非 RimQuest 体系(保留)。

---

## 四、风险/坑

- **3 个族群 DLL 承重**(Kiiro/AncotLibrary/KiiroCE + Story 的 Kiiro_Event): 非 XML 可移植, 近战 verb `AncotLibrary.Verb_MeleeAttackDamage_Combo`、RenderSkip 贴图系统全依赖库; Assemblies 禁留 1.4/1.5 同名残留(§5, 已只留 1.6)。
- **CE 补丁面向原版 CE**: HSK 无 `CE_TwoHandedWeapon` tag、护甲走 mm、Bulk 体系不同(§7)→ D4 静态核对为阶段3 首步。
- **HAR 女-only + raceRestriction 全局独占**: 加任何可穿物件须同步进各族列表否则仅绮罗可穿(§3 末)。
- **未装依赖门控**: MO(DankPyon)/VFE.Medieval2/Es.KiiroRace.PowerArmor 未装→其 IfModActive 文件夹不加载(无害, 勿删 Def 级门控); 中世纪扩展核心价值因此受限。
- **FacialAnimation**: `Ancot.KiiroFacialAnimation` 是给框架 mod 打的**内容补丁**(非框架本体), 未入 ModIndicator CompatibleFramework 组→可正常 loadAfter HSK 链(§2 框架mod禁加链规则不适用它本体; 若真机挂红❗再按 §2 摘链)。
- 改补丁必跑 `_tmp/patch_simulator.py`(lxml) 模拟; 交付即双目录同步, 不自动启动游戏扫日志(§1), 效果用户进游戏验。
