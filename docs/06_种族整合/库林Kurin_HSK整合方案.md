# 库林 Kurin (HAR) → HSK 整合方案：作为「阿丽莎(Asari)」的同生态位新增（双种族并存）

> 源 mod：工坊 `Seioch.Kurin.HAR`（ID 2326430787，韩系作者 Seioch/MaroonToon/Ninedaylongbow），狐系 HAR 种族。
> **定位定调（用户 08-29 二次改定）**：Kurin = HSK 现有「阿丽莎」种族（`skyarkhangel.asarirace` = AsariRace）的**同生态位新增**——派系/科技档位对齐阿丽莎（Spacer 母系狐族），但**阿丽莎完整保留、双种族并存**（用户理由"风味更足"），**不动阿丽莎任何挂载点**（委托/发布者/剧本/补译/白名单原样）。Kurin 只做增量，并对世界生成权重降档避免两同类太空族刷屏。
> **实装状态：✅ 完成 + 真机冷启动无红字（08-29）**。三段科技错位(入口石化中段 RK_Petro_Refining→链尾顶 CataphractArmor)、派系 Spacer+权重降 0.35/0.3、删台并 HSK 纺织台、清剿委托、CE 适配全落地；双目录 0 差异；ModsConfig 单条 `Seioch.Kurin.HAR` 合法位次(EndMod 前)、无大小写撞重。冷启动逐步修净 4 类红字：①子服饰 base+child 重复研究前置→`duplicate unlocked`；②Hwacha `forcedMiss`；③EliteScout/Trooper weaponMoney<CE 枪价；④12 件武器/服饰"has both researchPrerequisite"(继承单数+自有复数)→ `21_` 加 `researchPrerequisite Inherit="False"` 屏蔽；并删可选 Odyssey 附加包(Vacsuit,Gagarin 缓存重复注册报错)。终态仅剩第三方 vitech `Super_matter_E2` 老告警(非 Kurin)。剩：护送/求援/猎杀委托待 base 脚本；CE 数值待游戏内平衡。
> 核对基线：工作区 `ratkin-patch` + 部署侧 `RimWorld/Mods`（236 mod）+ `任务大修HSK` 的 RimQuest 委托体系。

---

## 〇、待用户拍板的决策点

| # | 决策点 | A 选项 | B 选项 | 结论(已定/实装) |
|---|---|---|---|---|
| **D1** | 阿丽莎去留 | **A. 退役阿丽莎**：Kurin 全面接管其派系/委托/剧本位 | **B. 保留阿丽莎，双种族并存**：Kurin 只做增量，不动阿丽莎挂载点 | ✅ **B（用户改选，理由"风味更足"）**：已回滚退役改动，Asari 委托/发布者/剧本/补译/白名单全部原样恢复，Kurin 另起一套 |
| **D2** | Kurin 科技排布（三段错位） | ~~纯 Spacer 对齐~~ / ~~石化毕业才起步~~ | **库林=夹在中间的必经段：石化中段 → 库林链 → 太空末位** | ✅ **三段**：入口前置挂石化**中段** `RK_Petro_Refining`（推到此即可开始库林，非毕业）；链内工业高段→Spacer 递进；**链尾 `Kurin_ApparelT4` 作为顶配太空末位 `CataphractArmor` 的（MayRequire 门控）前置** → 太空末档须先点库林。研究台 §1b 六档逐节点配，坐标零撞车不越级。卖点：**阿丽莎=太空原生；库林=石化中段升空必经跳板；太空末位在库林之后** |
| **D3** | 派系 | 一对一映射阿丽莎 NPC | Kurin 自带 Republic(友贸易)+Battle Foxes(敌)+玩家族 三支**并列新增** | ✅ 并存：Kurin 3 支自建派系独立入世界生成，权重降到 0.35/0.3、requiredCount=0 与 Asari(1) 错开防刷屏 |
| **D4** | 任务委托类型 | **A. 沿用现状**：1 条 `_Raid`(清剿) | **B. 扩充**：求援/护送/猎杀多型 | ◑ **B（部分实装）**：清剿×2 已建（Kurin 风味叙事，Asari 委托并存不动）；求援/护送/猎杀需 RimQuest 原生 base 脚本核实（环境无先例，作者自列 TODO），暂缓 |
| **D5** | Kurin 私有件 | **A. 删两裁缝台**并入 HSK 纺织台；炮塔/陷阱/作物留风味 | **B. 全保留补 CE** | ✅ **A**：删台+Dropdown+workgiver 补丁，服饰/假尾配方并入 `Hand/Electric/HyperTailoringBench`（实测服饰真台非 TableLoom） |

> 材料映射：实测 Kurin 引用的材料/stuffCategory 全部在 HSK 存在 → **无需改线**（只换 def 指向都不必）。CE 数值、汉化范围见正文。

---

## 一、1.6 兼容性 <span>已原生支持</span>（风险：低）

- **实物更正**：解压 `About.xml` 声明 **1.2/1.3/1.4/1.5/1.6**，顶层有完整 `1.6/`（45 defs + `Kurin.dll`/`KurinHAR.dll`）。Kurin **本身即 1.6 原生**，无需版本移植。
- 依赖 `erdelf.HumanoidAlienRaces`：本环境 AlienRaces 已 **1.6**，匹配（阿丽莎同依赖 HAR，一致）。
- `LoadFolders.xml` `<v1.6>` = `/` + `1.6` + Biotech 门控 + `1.6/Defs/BiotechScenarios` + Odyssey 门控；版本标签带 `v` 前缀，收口时保留。Odyssey 未装则其目录不加载（无害，勿删 Def 级门控）。
- 1.4→1.6 API 差异（`BodyTypeDef`/`HeadTypeDef`/`FacialAnimations`/`BaseFactionMapGenerator`）本地 1.6 目录已消化。
- 收口：只留 1.6 + 根件（Biotech/Hair/Sounds/Textures/Languages），删 1.2~1.5。

---

## 二、阿丽莎现状对照表（五维体检，生态位对照）

| 维度 | 阿丽莎（AsariRace `skyarkhangel.asarirace`）现状 | Kurin 对应素材 |
|---|---|---|
| **派系定位** | 2 FactionDef：`AsariHunters`「United Asari Republic」（NPC，Spacer，含 visitor/base 商队，`NamerFactionPirate`→可袭扰）+ `AsariPlayerColony`「Asari Republic」（玩家族）。母系全女 | 4 派系：`Kurin_Faction`「Republic of Kurin」(Industrial，可贸易) + `Kurin_Faction_Hostile`「Battle Foxes」(敌袭扰) + `Kurin_PlayerFaction`(玩家族) + 隐藏商队。全女狐基人类 |
| **科技档位** | 统一 **Spacer**；**无自带研究页签/节点**（复用 HSK 体系，"太空原生"） | 自带 `Kurin_ResearchTab` + 10 节点；**三段错位=石化中段(`RK_Petro_Refining`)起步→库林中→太空末位(`CataphractArmor`)收尾**，见 D2/§4.2 |
| **装备档次** | **无自定义服饰/武器**（ThingDefs 仅血液/护盾 Mote）；pawnKind 穿 HSK 原版 spacer 装。17 PawnKind（军:SupremeAdmiral/WallDestroyer/Commando/Vanguard/Infiltrator/Overmaster/Enslaver/Hunter；民:CrewMember/Villager/Refugee/Colonist/Pawn/HunterPlayer） | **自带 65 服饰 + 近战2/远程5 武器 + 炮塔**（全原版 verb 无 CE，须 CE 化 + Spacer 档）。27 PawnKind(BattleFox15/Republic9/Colony2/Special1) |
| **任务** | `任务大修HSK` 里 `HSK_族色委托_AsariRace.xml` 2 条 `QuestScriptDef`（`HSKQuest_AsariHunters_Raid`/`_AsariPlayerColony_Raid`，均清剿型）+ `HSK_族色任务发布者.xml` 2 条 `RimQuest.QuestGiverDef`(`RQ_HSK_AsariHunters`/`RQ_HSK_AsariPlayerColony`) | 需新建 Kurin 委托（清剿+求援+护送+猎杀多型）+ 发布者指向 Kurin 派系 |
| **剧情/文化** | `AsariCulture` 文化 + 开局剧本 `RevengeAsari`「阿莎丽的复仇」(Spacer) + 7 `TattooDef` + 11 背景故事 + `AsariXenotype`/7 基因 + 9 LifeStage。风格：科幻母系、复仇流亡 | `Aolarian` 文化 + 3 ScenarioDef(+Biotech 剧本) + 103 背景故事 + `KurinXenotype`/基因 + 17 发型 + Incident/Thought/WandererJoin。风格：闪耀世界狐娘流亡 |

**关键差异 / 差异化卖点**：三方各占一段科技、错位不重叠——**阿丽莎=太空原生**（轻量、pawnKind 直接复用 HSK 太空装、无自定义科研）；**库林=石化中段→升空的必经跳板**（重量自带 65 服饰/武器/10 科研：**入口卡 HSK 石化中段 `RK_Petro_Refining`，链内工业高段→Spacer 递进，链尾 `Kurin_ApparelT4` 又作为太空末位顶配 `CataphractArmor` 的前置**，故"想点太空末档必先点库林"）；太空末档（Cataphract 等 Ultra）在库林之后。库林自带内容须 HSK 化 + CE 化；阿丽莎不改动。

---

## 三、Kurin 新增清单（双种族并存，**不动阿丽莎任何挂载点**）

> D1 已由"平替"改为"并存"：阿丽莎（`skyarkhangel.asarirace`）**完整保留、照常进 ModsConfig**，下表"阿丽莎"列仅作生态位对照，全部**保持原样**；"Kurin 新增"列为本次增量。

| 维度 | 阿丽莎（保持不动） | Kurin 新增（并存） |
|---|---|---|
| mod | `skyarkhangel.asarirace` 照常启用 | 新增 `Seioch.Kurin.HAR`（收口本地 1.6） |
| 派系 | AsariHunters / AsariPlayerColony 不动 | 新增 Kurin_Faction(Republic 友)/Kurin_Faction_Hostile(Battle Foxes 敌)/Kurin_PlayerFaction；**权重降 0.35/0.3、requiredCount=0** 与 Asari(1) 错开防刷屏 |
| 文化 | AsariCulture 不动 | 新增 Aolarian（并入 05 分区） |
| 开局剧本 | `RevengeAsari` **不退役** | 新增 Kurin 自带 3 ScenarioDef |
| 异种/基因 | AsariXenotype/基因 不动 | 新增 KurinXenotype（过异种池白名单，勿追"智人种%"补基因） |
| PawnKind/装备 | 17 PawnKind + HSK 现成装 不动 | 新增 27 PawnKind + Kurin 自带 65 服饰/武器（抬 Spacer 档 + CE 化） |
| **任务委托** | `HSK_族色委托_AsariRace.xml` + `RQ_HSK_Asari*` 发布者 + `HSK_Reward_Asari*` 特产包 **全部保留原样** | **新建** `HSK_族色委托_Kurin.xml`（清剿×2，战狐风味叙事）+ 发布者**新增** `RQ_HSK_Kurin_Faction`/`RQ_HSK_KurinPlayerFaction` + 特产包**新增** `HSK_Reward_Kurin_*` |
| 汉化 | `补译_...asarirace`、核心汉化 Asari 键 **不动** | Kurin 走本体自带简中（99/99 背景 + 剧本/派系/文化全译，已核）；缺口仅 1 ThoughtDef（可忽略） |
| 发型白名单/取名 | AFU/Gloomy 白名单、`01_科技与名字` 的 AsariCulture 块 **不动** | 各**增量补一个 Aolarian 块**（Conditional 门控，Kurin 继承同等发型/取名权） |
| 科研 | 阿丽莎**无**研究节点（太空原生） | 新增 Kurin 10 节点（自有页签链，**入口挂石化中段 `RK_Petro_Refining`·链尾 `Kurin_ApparelT4` 顶太空末位 `CataphractArmor` 前置**，融主网格，§四-4.2） |

> ⚠️ 并存注意：两套母系狐族太空派系同在世界生成，已用 Kurin 权重降档 + requiredCount=0 拉开；若实测仍显拥挤，进一步下调 Kurin `settlementGenerationWeight`。defName 命名空间 `Kurin_`/`Asari` 无冲突。

---

## 四、Kurin HSK 化改造（新增内容；科技三段=石化中段→库林→太空末位）

**4.1 本地化转换（工坊→本地）** — ✅ 已完成
整夹复制进工作区，旧版本目录移 `_tmp/工坊转本地_隔离_kurin_20260829/`（勿删），只留 1.6 + 根件（删 `1.6/Source` 构建产物）。保留 packageId `Seioch.Kurin.HAR`；About supportedVersions→1.6，modDependencies 补 Harmony+Core SK+HAR，loadAfter 补 HSK/CE/ResearchTreeSK/ModIndicator(压尾)，保留 Garam incompatibleWith；`07_ModAssistant标记.xml`(NativeAddon+MayRequire 沿用既有模式)。部署后提醒退订工坊版。

**4.2 科研挂接（三段错位：石化中段→库林→太空末位）** — ✅ 已完成（`20_科研融网格HSK.xml`）
保留 `Kurin_ResearchTab` 自有链。**入口三根节点（ApparelT1/Flower/Turret）前置挂石化中段 `RK_Petro_Refining`**（Industrial，与链首同档不越级）→ 玩家在石化推到中段即可开始库林科技。**链尾 `Kurin_ApparelT4`（Spacer 顶配装甲复刻）作为 HSK 太空末位 `CataphractArmor`（Ultra，终端节点无下游级联）的前置**（`<li MayRequire="Seioch.Kurin.HAR">`，库林缺失则自动降级、不悬空）→ 库林=通往太空末档的必经中段。`Turret` 由 Medieval 抬 Industrial 防越级。六档研究台按 §1b 逐节点配：工业段=`SimpleResearchBench`+设施 `LabTerminal`（ext[Simple,HiTech]）；Spacer 段=`HiTechResearchBench`+设施 `MultiAnalyzer`（ext[HiTech]）。**绝不用 LabStation/LabTerminal 当 requiredResearchBuilding（设施非研究台，误写必炸）**。每节点 Add `ResearchTreeSKModExtension` 入主网格，坐标经 Unified.xml 占格探测落 75–84 空行、(X,Y) 全局唯一零撞车。Core_SK/RTSK/HSK 升级后若 `CataphractArmor`/`RK_Petro_Refining` 改名须复核此两处衔接边。

**4.3 材料改线** — ✅ 实测无需改线
Kurin 引用的 `Cloth/Synthread/DevilstrandCloth/Plasteel/Uranium/ComponentSpacer/…` 与 stuffCategory `Fabric/Leathery/Metallic` **在 HSK Unified 全部存在**（已逐个核验）→ 不 remap、不改造价（尊重"别自作主张改造价"）。Convallaria 作物链保留（`sowResearchPrerequisites=Kurin_Flower` 随 §4.2 落位）。

**4.4 CE 适配（HSK 下必做）** — ✅ 已完成（`50_`50 op+`51_`77 op，入编模拟 0 失败，冷启动无 CE 报错）
武器全 `CombatExtended.Verb_ShootCE` + `<AmmoUser>` + `<FireModes>` + `Bulk`；弹药映射 Charge→`6x24mmCharged`、Shotgun→`12Gauge`、步枪按口径；近战 Ceremonial/Surigum→`CombatExtended.ToolCE`+Sharp/Blunt mm；护甲→`Bulk`/`WornBulk` + `ArmorRating_Sharp/Blunt` 换 CE mm 值；Hwacha 炮塔 CE 化。核 `06_弹药/HSK弹药清单.csv` 防重复定义。**阿丽莎不需要这步（它无自定义装备），故 CE 是 Kurin 自带内容带来的额外净成本 → 本次工作量最大头。**

**4.5 文化/服饰 tag** — 中/低
`Aolarian` 并入 05 分区模式（与 AsariCulture 并列，不改阿丽莎）；Kurin 5 个自定义 ApparelLayerDef 对齐 HSK 分区；分布用 `generateCommonality` 加权；穿戴锁走 AlienRaces `raceRestriction.apparelList`（全局独占白名单）。

**4.6 汉化** — ✅ 已由自带中文满足
Kurin **自带简中**（41 文件/20 类型，已核 99/99 背景+剧情全译）。阿丽莎的 `补译_...asarirace`、核心汉化 Asari 键 **全部不动**（并存）。DefInjected 根须 `<LanguageData>`。

**4.7 派系接入 + 任务钩子（RimQuest）** — ✅ 部分完成（清剿型）
- Kurin 派系进 `BaseFactionMapGenerator`（自带 C# GenStep_Republic/BattleFoxes）；世界生成权重已降档与 Asari 错开。
- **任务钩子**：在 `任务大修HSK` **新建** `HSK_族色委托_Kurin.xml`（2 条清剿 `QuestScriptDef`，沿用 `QuestNode_Sequence`+`Util_GenerateSite` 骨架），叙事按"动作清单法"写战狐风味；`HSK_族色任务发布者.xml` **新增** `RQ_HSK_Kurin_Faction`/`RQ_HSK_KurinPlayerFaction` 两条 `RimQuest.QuestGiverDef`（**Asari 发布者原样保留**）；`HSK_族色特产包.xml` **新增** `HSK_Reward_Kurin_*`。任务大修 About `loadAfter` 加 `Seioch.Kurin.HAR`。框架已内置（`RimQuest.dll`/`SimpleWarrants.dll`），**无需另装 RimQuest**。
- ⚠️ **求援/护送/猎杀** 暂缓：环境 75 条委托**全为清剿型**，无这三类的可复用 base 脚本先例（作者自列"取回/供给 待办"），且 QuestScriptDef 无法用 patch_simulator 静态验证（运行时节点图）。须真机 dump RimQuest/vanilla base `Quest_*` def 名后再补，避免 ship 出运行时 NRE 的坏委托。
- 悬赏(Warrants)：若要 Kurin 可通缉目标，后续补 `WantedReasons`。

**4.8 剧情/叙事内容** — ✅ 基本已由 Kurin 自带中文满足
Kurin **自带完整中文剧情**：3 ScenarioDef（「魅狐，开拓者」/「逃亡的定制伴侣」等，含 summary/GameStartDialog 全译）+ 99/99 背景故事 + Faction/Culture/PawnKind/Hair/Xenotype 等 20 类型 DefInjected 全译（已核 0 缺）。故无需从零手搓剧情，仅需后续按需微调文案。缺口仅 1 条 ThoughtDef 未译（可忽略）。

**4.9 建筑/私有件处置（D5=A）** — ✅ 已完成（台），炮塔/陷阱 CE 归 4.4
删两个 Kurin 裁缝台 + `KurinTailoringBenchDropdownDef` + `workgiver.xml` 补丁；服饰 `Apparel_Base` 与 `Kurin_FakeTail` 手术配方的 `recipeUsers` 并入 HSK `Hand/Electric/HyperTailoringBench`（实测环境服饰真台是这三个）。Hwacha 炮塔/套索陷阱/Convallaria 作物留作风味（炮塔 CE 归 4.4）。

---

## 五、风险与工作量分级

| 板块 | 工作量 | 风险 |
|---|---|---|
| 1.6 兼容 / 本地化收口 | ✅ 已完成 | 低 |
| 双狐并存去重（Kurin 权重降档 vs Asari） | ✅ 已完成 | 低（并存无需改阿丽莎，仅调 Kurin 权重） |
| 科研融网格 Spacer 档(10 节点) | ✅ 已完成 | 中（RTSK 升级须复核坐标） |
| 材料改线 | ✅ 无需 | —（材料全存在） |
| **CE 适配**(65 服饰+武器+炮塔，Kurin 自带内容净增) | ✅ 已完成(50_/51_ 模拟 0 失败) | 中(数值待游戏内平衡) |
| 文化/服饰 tag/穿戴锁 | 中（Aolarian 增量已完成） | 低 |
| 汉化(自带，99/99 已核) | ✅ 已满足 | 低 |
| 派系接入 + RimQuest 委托 | ◑ 清剿完成 | 中（护送/求援/猎杀待 base 脚本核实） |
| 剧情/叙事(自带中文) | ✅ 基本满足 | 中 |

**总体**：兼容性无痛；阿丽莎并存**不需退役改指**（原最高风险项消除），仅需 Kurin 权重降档防刷屏。剩余最大成本 = **CE 适配（已完成，数值待游戏内平衡）**。落地顺序全部完成：✅4.1→✅4.2→✅4.3→✅4.4→✅4.5/4.7/4.8→✅4.6→✅同步；**真机验证项见 §七（等排期，不擅自重启游戏）**。

---

## 六、验收基准
- 新档冷启动 `Player.log` 无新增红字；**阿丽莎与 Kurin 双族并存均无悬空引用**（Kur 委托/发布者新增不影响 Asari 原条目）。
- 世界生成：Asari + Kurin 两支母系狐族均按预期出现且不刷屏（Kurin 权重 0.35/0.3、requiredCount=0 已降档）；RimQuest 任务板出现 Kurin 委托并可接取/完成/发奖。
- `Unified.xml` 终态：Kurin 10 节点在主网格自有页签、Spacer 档研究台可研究，无 duplicate unlocked defs、无越级告警；服饰可穿（HSK 纺织台可制）、武器 info 卡 CE 支持。
- 改补丁跑 Kurin 自带 def 的 lxml 模拟（`_tmp/kurin_patch_sim.py`，11 补丁 / 199 步 / 0 失败）；同步过 deploy-sync 双目录 hash。

---

## 七、待开游戏验证清单（按用户指示：全部改动已落盘+静态全绿后挂起，重启由用户统一排序，不擅自抢 Player.log）

> 每项：做什么 / 为什么必须开游戏 / 预期看什么。均需在游戏内或经真机 dump 才能安全完成，静态工具（lxml/patch_sim）覆盖不到运行时。

**V1 · 新档冷启动全绿复扫（含进档开面板）**
- 做什么：清 `MissileGirl/Cache` → 启动 → 到主菜单扫一次 `Player.log` → 再开一局新档、打开小人管理/派系/任务板等面板，再扫一次 `Player.log`。
- 为什么必须开游戏：冷启动日志全绿 ≠ 运行时干净；OnGUI 类异常（本次菜单已有 chronic GameplayTipWindow 除零，需确认非 Kurin 触发）只在开 GUI 时暴露；跨 mod def 引用（委托/发布者/特产/衔接边）真正解析在运行时。
- 预期看什么：无 `Kurin`/`Asari` 新增 error/exception；无 `duplicate unlocked defs`/`lower techlevel`/`same position`；无 `both researchPrerequisite`；Gagarin 无 `same key`；仅剩已知第三方 `vitech Super_matter_E2` 老告警。

**V2 · 双狐世界生成 + Kurin 委托进任务板实测**
- 做什么：开新档（或世界生成预览）确认 AsariHunters/AsariPlayerColony 与 Kurin Republic/Battle Foxes 都正常刷出、比例不刷屏；开任务板/通讯台看 Kurin 清剿委托是否发布、可接取、完成后发银币+好感+Kurin 特产。
- 为什么必须开游戏：`settlementGenerationWeight`/`requiredCountAtGameStart` 的实际生成分布、RimQuest 的 `QuestGiverDef`→faction 绑定、委托节点图运行时才求值（静态测不了）。
- 预期看什么：两支母系狐族都出现但库林(权重0.35/0.3)明显少于阿丽莎(1)；Kurin 委托文案显示战狐风味、能接、据点正常生成、结算发奖正常。

**V3 · CE 数值游戏内平衡（中低置信项）**
- 做什么：实测 Hwacha 箱炮齐射/爆点、飞刀 Kurin_Gun_Surigum 是否需删 `Verb_Shoot` 投掷 verb、霰弹枪弹丸散布、各护甲 CE mm 幅度（Redfox 32/16、Mechanic 20/20 最高风险）与 Bulk 手感。
- 为什么必须开游戏：这些是 CE agent 按原版量级**外推的判断值**，只有实际射击/命中/负重手感能校准；子代理已标 TODO 的位置。
- 预期看什么：炮塔不哑火/不刷屏报错、枪弹耗弹正常、护甲吸收符合档位、无 "weaponless" 生成；不符则回改 `50_/51_` 对应值再同步。

**V4 · 护送/求援/猎杀三类委托（依赖 dump base 脚本，非纯开档）**
- 做什么：游戏内 dump 出 RimQuest/vanilla 可用的 base `QuestScriptDef`（护送/交付/供给类骨架）def 名，再仿清剿委托写 `HSK_族色委托_Kurin.xml` 追加条目 + 发布者扩脚本。
- 为什么必须开游戏：环境 75 条族色委托**全是清剿型**，无任何可复用的护送/求援/猎杀 base 脚本先例（作者自列"取回/供给 待办"），且新委托是运行时节点图、静态不可验；盲写大概率运行时 NRE。
- 预期看什么：dump 到确切 base def 名后再写；三类委托能在任务板出现并正常生成/结算，无悬空 `<def>` 引用。

**V5 ·（可选）Odyssey 附加包回收**
- 做什么：如日后需要 Kurin 的 Odyssey Vacsuit 套装（本整合已因 Gagarin 缓存重复注册报错而删除），需先在别的 mod 环境验证该文件夹的加载不触发 Gagarin 双注册，或用改名/去根 `/` 方案恢复。
- 为什么必须开游戏：Odyssey 文件夹双注册是 Gagarin 运行时缓存索引行为，静态看不出触发条件。
- 预期看什么：恢复后冷启动无 `same key`/Gagarin error、Vacsuit 在 Race.xml 穿戴列表正确引用不悬空。
