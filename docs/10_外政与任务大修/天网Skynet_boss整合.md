# 天网(Skynet_SK)大 boss 与派系 · 外政与任务整合专节

> 立档 2026-09-01。承接 [`外政与任务大修重构_架构设计方案.md`](./外政与任务大修重构_架构设计方案.md)（Layer B/C）与 [`外政数据结构与挂钩点附录.md`](./外政数据结构与挂钩点附录.md)。
> 性质：**已核验整合结论 + 设计**（mod 已部署 `skyarkhangel.skynet` 且启用；本文所有 defName 取自 `Mods/Skynet_SK/Defs` 实际 def，**非** Unified.xml——该缓存是 Skynet 启用前导出的，grep 天网命中 0）。

## 0. 事实核验结论（先看这个）

- **Skynet_SK 已是 HSK/CE 原生内容，无缺项可补。** 悬空引用全扫通过：工作台 `AdvToolBench`、前置研究 `Droids_E1`、材料 `RobotParts/ElectronicComponents/NitinolAlloy/CarbonAlloy/DepletedUranium/Tungsten/SyntheticFibers`、穿戴 `Armor_SkynetCloak/Apparel_Jumpsuit/Apparel_MarineBodysuit/Apparel_Sunshades/Apparel_PilotVisor`、身体 `HumanoidTerminator`、hediffGiverSet `ChjAndroidStandard`(Androids)/`AndroidPassiveSet`(自带) 均在 Core_SK/Androids/Skynet 内解析命中。
- **CE 适配已完备**：`SkynetInfiltrator/Agent/PrototypeTX` 近战全 `Class="CombatExtended.ToolCE"`（自带 `armorPenetrationSharp/Blunt` mm）、`modExtensions=RacePropertiesExtensionCE(Humanoid)`、`comps=CompInventory/CompPawnGizmo/CompProperties_ArmorDurability`；护甲值即 CE mm 档；pawnkind 用 `CombatExtended.LoadoutPropertiesExtension`（弹匣/副武器），武器 tag 全为 SK/CE tag（`ADR/ADS/RF/MG/SMG/PT/RKT`）。**CE 适配清单：无需动作。**
- **研究台档位合规**：唯一自研节点 `AndroidRepairKit` `ParentName=HitechBase`（Spacer→`HiTechResearchBench`+设施 `LabTerminal`），techLevel Spacer，前置 `SK_Robotics`——符合 §六 Spacer 档；无越级。坐标 `SpacerTech` 页签 `(2.93,5.10)`→`(3,5)`，**须进游戏冷启动重跑 `_tmp/census_research_pos.py` 核撞车**（Unified.xml 无 Skynet 数据，静态核不了全表占位）。
- 附属包汉化已就位（`1.6hsk附属mod汉化/{1.4,1.6}/Skynet_SK/…/DefInjected/…`，FactionDef 节点名用的是正确 defName `SkynetHumanlike`/`HumanResistance`，非 bug）。

## 1. 派系定位与敌我链（真实 defName）

| 派系 defName | 科技档 | humanlike | hidden | 敌我 | 生成 |
|---|---|---|---|---|---|
| `SkynetHumanlike`（天网本尊/大 boss 方） | Spacer | **false** | **true** | `permanentEnemy=true` + `hostileToFactionlessHumanlikes` → 对全地图（含玩家/反抗军）永久宣战 | `earliestRaidDays 260`；`raidCommonalityFromPointsCurve` 12000 点起 |
| `HumanResistance`（人类抵抗组织/盟友候选） | Spacer | true | **true** | 无 `permanentEnemy`（`naturalEnemy=false`，`permanentEnemyToEveryoneExceptPlayer` 已注释）；与天网的敌对由天网侧 `permanentEnemy` 单向锁定 | 世界 NPC；`raidCommonalityFromPointsCurve=(0,0)` 不主动袭击 |

**关系链定论**：玩家↔反抗军=中立（默认可结盟）、双方↔天网=永久战争（天网 `permanentEnemy` 自动成立，**无需再打敌对我补丁**）。反抗军自带 `ResistanceBodyguard`(T-Cyborg) 是唯一 humanlike 的天网级战力。

## 2. 大 boss 定义与出现方式

**"大 boss" = T-X 终结者（Terminatrix）**，种族 `SkynetPrototypeTX`（`MarketValue 130000`、`baseHealthScale 3.1`、护甲 Sharp 8/Blunt 10 mm、护甲耐久 3200、可再生+断肢重生+变形臂）。按 pawnkind 分档：

| pawnkind defName | 身价(MV，取种族) | combatPower | 出现 |
|---|---|---|---|
| `SkynetPrototypeTXHighTech` | 130000 | **1600** | 天网高科技战斗编组（`pawnGroupMakers` commonality 30） |
| `SkynetPrototypeTXOrigin` | 130000 | 1450 | 起源编组（commonality 15，单刷 boss 位） |
| `SkynetPrototypeTX` | 130000 | 1350 | 普通编组（commonality 70） |
| `ResistanceBodyguard`(T-Cyborg) | 130000(同种族) | 1200 | 反抗军保镖（ allied boss 对位） |
| `SkynetInfiltrator*`(T-800) | 90000 | 500–1150 | 经典突击单位 |

**出现通道**（都是**袭击/事件**，非世界常驻据点）：①`Salvation` 事件（`IncidentWorker_Salvation`，`earliestDay 180`/`minThreatPoints 7500`/`baseChance 2.4`）刷 `Skynet{Infiltrator,Agent,PrototypeTX}Event`；②`AgentPodCrash`/`AgentTravelerGroup`（day 180，植入 `SkynetCovert` 伪人类特工）；③天网 `earliestRaidDays 260` 起的常规袭击带 TX 编组。

## 3. 与外政/任务体系（Layer B/C）的交互设计

### 3.1 悬赏钩子：boss 走 T4 战争级，不走 SW 随机悬赏（核验通过）

`任务大修HSK` `BountyRules.IsWorldNpcFaction` 判据 = `humanlikeFaction && !defeated && !Hidden && !IsPlayer`。∴ **天网因 `humanlikeFaction=false`+`hidden` 双重排除 → 永远不会被随机悬赏生成（这是"正确拦截"非"漏拦截"：不对机器人军团发通缉令是合理语义）**。反抗军若未来 un-hide 则纳入悬赏池，Spacer 档经 `TechGapOk`（|Δ|≤1）与发出端 `TryAddWarrant`（目标档>我方+1 拦截）双校验：玩家=Industrial 可交互 Δ1、=Medieval 及以前被正确拦（不漏拦 Spacer 极高档内容）。

**boss 讨伐的正确落点** = Layer C §四"来源"维度的 **T4 战争级·外政驱动定向任务源**（读 `SkynetHumanlike` 敌对态生成"歼灭 T-X 终结者"战令），而非 SW `Warrant_Pawn`。奖励按 §四 表驱动 `f(tier=T4 × 双线认可 × 科技档=Spacer × 敌对)`；击杀挂钩见附录 §3（`Pawn.Kill`/`SetRelationDirect`，机器派系无好感体系→改为触发 `Salvation` 反扑信 + 缴获 `AndroidRepairKit`/稀有合金战利）。

### 3.2 关系权重刻度建议（写入附录 §1 双线模型的 free 线）

| 对象 | free 线 standing | 好感刻度 | 依据 |
|---|---|---|---|
| `SkynetHumanlike` | 不计入（永久敌，无外交） | 恒 Hostile（`permanentEnemy`） | 天网无贸易/任务态 |
| `HumanResistance` | 盟友池，un-hide 后起步 standing 45（TerritorialClaimant 档） | 结盟任务 +服务分项；击杀 TX 事件 +长线分项 | 镜像 BOTR 五分项；≥75→Ally |
| 玩家对天网战功 | 折进 free 线 `Service/Territory` 两分项 | 完成 T4 战令 +standing、解锁科技战利 | 走原版 `TryAffectGoodwillWith` |

### 3.3 数据结构示例（真实 defName，供 edgepolitics 落地时直填）

```xml
<!-- 拟写入 local.hsk.edgepolitics（未建容器；此处仅示意，勿在 Skynet 本体加） -->
<!-- ① 把反抗军登记进 free 线盟友白名单（defName 级联引用，全量后解析，不加 loadAfter） -->
<FactionDef ref="HumanResistance"/>           <!-- Spacer, humanlike -->
<!-- ② T4 boss 战令的目标锚点 -->
<PawnKindDef ref="SkynetPrototypeTXHighTech"/> <!-- combatPower 1600, race MV 130000 -->
<!-- ③ 击杀事件奖励读的真实物品 def -->
<ThingDef ref="AndroidRepairKit"/>  <ThingDef ref="NitinolAlloy"/>  <ThingDef ref="Plasteel"/>
```

`WorldComponent_EdgePolitics` 里 `HumanResistance`/`SkynetHumanlike` 一律按 **`faction.loadID`** 存态（与 BOTR/§0 定调一致）；boss 战令的 `issuer` = `HumanResistance`、`targetFaction` = `SkynetHumanlike`，`tier=T4` 落 `loadID→上下文` 字典（仿 `bountyProgress` ExposeData）。

## 4. 落地边界与风险（为何现在不打 un-hide 补丁）

- 两个 FactionDef 由作者刻意 `hidden=true` 且**无 `caravanTraderKinds`/`baseSite`/settlement 生成器**。贸然 `<hidden>false</hidden>` 会触发世界生成尝试为该派系建据点却因缺贸易/据点基建而产生**空壳/异常派系条目**（类似机械族不显示），属破坏性改动 → **不在本轮做**。真正的"接入外政地图"须连同结算基建一并设计，或维持"raid-boss + 定向战令"模型（本文 §3.1）。
- 若要反抗军成为可结盟/可发任务的正常太空人类派系，需：un-hide + 补 `caravanTraderKinds`/`baseSiteMaker`/`allowedMissions` + `GoodwillSituationDef`(走 `Meow.GoodwillSituationWorker_AlwaysAlly` 保对玩家友好)——独立设计项，待拍板后随 Layer B 容器一并落。
- 一切新 `loadAfter` 前跑 `_tmp/loadorder/find_cycle.py`+`check_order2.py`；本专节**未新增 mod 内容/补丁**，故无双目录同步项（文档类不入 Mods）。
