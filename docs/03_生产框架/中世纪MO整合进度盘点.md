# 中世纪MO(Medieval Overhaul)→ HSK工业科研大修 整合进度盘点

> 盘点时刻: 2026-08-27 23:05(实测,非记忆)。底稿: `docs/03_生产框架/中世纪大修内容梳理.md`(MO 1.6.2.2 全量梳理)+ `中世纪OM未整合内容清单与优化评估.md`(2026-08-27 07:55 版,本文已覆盖其 §3/§8 结论)。
> 核对方法: 工作区 `HSK工业科研大修/1.6/Defs` 逐文件解析 → 部署 `Mods/HSK工业科研大修` 全量 hash 比对 → 游戏导出终态 `LocalLow/.../MissileGirl/Cache/Unified.xml`(2026-08-27 21:43,49.8 MB)逐 defName 命中核对 → 贴图 texPath 跨 187 个 Textures 根解析。

## 一、一句话结论

按 **84 个 MO 内容单元** 计:已整合 **6** 个、部分 **5** 个、未整合 **55** 个、按约定/机制原因不整合 **18** 个 —— 加权整合率 **10.1%**(纯完成口径 7.1%)。

今天(08-27)真正新增的是 **MO 结构/娱乐内容的直接移植(23 个建筑 def + 2 个二级菜单子分类 + 111 张贴图)** 和 **蓝图经济 v4.1 的商人/事件渠道(1 商队 + 3 事件)**;其余 15 个板块的状态与 07:55 版盘点一致 —— 武器、防具、盾牌、生物、派系世界、酒药炼金、矿井 **仍全部未落地**。

## 二、板块进度表

| 板块 | 单元数 | 已整合 | 部分 | 未整合 | 不整合 | 加权进度 |
|---|---|---|---|---|---|---|
| 1 材料与资源 | 7 | 0 | 0 | 2 | 5 | 0.0% |
| 2 生产设施与配方 | 8 | 0 | 1 | 7 | 0 | 6.2% |
| 3 建筑与家具 | 24 | 5 | 1 | 15 | 3 | 22.9% |
| 4 武器体系 | 5 | 0 | 0 | 5 | 0 | 0.0% |
| 5 防具体系 | 4 | 0 | 0 | 4 | 0 | 0.0% |
| 6 盾牌与魔法装备 | 3 | 0 | 0 | 2 | 1 | 0.0% |
| 7 生物体系 | 4 | 0 | 0 | 3 | 1 | 0.0% |
| 8 派系与世界 | 10 | 0 | 2 | 4 | 4 | 10.0% |
| 9 酒水/药物/炼金 | 3 | 0 | 0 | 3 | 0 | 0.0% |
| 10 书籍与研究 | 5 | 1 | 0 | 3 | 1 | 20.0% |
| 11 心情·特性·Hediff·文化 | 6 | 0 | 0 | 5 | 1 | 0.0% |
| 12/13 机制与兼容 | 3 | 0 | 1 | 0 | 2 | 16.7% |
| 16 矿井/采掘 | 2 | 0 | 0 | 2 | 0 | 0.0% |
| **合计** | **84** | **6** | **5** | **55** | **18** | **10.1%** |

> 加权进度 = (已整合 + 0.5×部分) / 单元数。「不整合」是**已决策的排除项**(HSK/Vile 上位替代、依赖 MO DLL、或 MO 自身停用),计入分母以便与 07:55 版盘点同口径比较。

## 三、已落地硬证据(实测)

| 项 | 数量 | 位置 | 终态核对 |
|---|---|---|---|
| Rustic_ 乡村结构 ThingDef | 19(拒马桩已删) | HSK工业科研大修/1.6/Defs/Buildings_Rustic_Structures.xml | Unified.xml 全部命中(终态生效) |
| Rustic_ 棋牌 ThingDef | 3 | 1.6/Defs/Buildings_Rustic_Joy.xml | Unified.xml 全部命中 |
| 配套 JoyGiverDef / JobDef(棋牌) | 3 + 3 | 1.6/Defs/Buildings_Rustic_Joy.xml | Unified.xml 命中 |
| 建筑师二级菜单子分类 | 2 | 1.6/Defs/SubCategories_Rustic.xml(乡村结构/棋牌桌游) | Unified.xml 命中 |
| 自带贴图(随移植拷入本 mod) | 111 张 Things/Building | HSK工业科研大修/Textures/Things/Building/** | 57 条 texPath/uiIconPath 中 54 直接命中,余 3 为目录变体/原版图集,实际不缺图 |
| 蓝图书(门禁体系) | 112 ThingDef | 1.6/Defs/ThingDefs_BlueprintGates.xml + Languages/English/DefInjected/ThingDef/BlueprintBooks.xml | Unified.xml 112/112 命中 |
| 蓝图门控 DLL | 22 类 / 1541 行 | 1.6/Assemblies/BlueprintUnlockHSK.dll(源 Source/BlueprintUnlockHSK.cs) | CanStartNow 门禁 + 研读工作 + 树角标 + 5 路投放 |
| 事件(蓝图馈赠 + 巨兽路过) | 3 IncidentDef | 1.6/Defs/Incidents_EventPack.xml | Unified.xml 命中;baseChance 自动入事件池 |
| 蓝图学者商队 | 1 TraderKindDef | 1.6/Defs/TraderKinds_BlueprintScholar.xml + Patches/67(挂两个派系抽象基类) | Unified.xml 命中 |
| 研读工作/奖励池 | 1 JobDef + 1 WorkGiverDef + 1 ThingSetMakerDef + 1 ThingCategoryDef | 1.6/Defs/JobDefs_RK.xml / WorkGiverDefs_RK.xml / ThingSetMakerDefs_BlueprintReward.xml / ThingDefs_Categories.xml | Unified.xml 命中 |
| 双目录同步 | 0 差异 | 工作区 ↔ Mods/HSK工业科研大修 | 全量 hash 一致;08-27 22:45 三项修复后 4 文件再次原子同步 |

## 四、移植质量问题与处置(已全部处理)

**① 拒马桩进不了二级菜单 → ✅ 已删除(22:40)**

- 现状: 继承父类得 designationCategory=Security 却被写进 Structure 的子分类(按 AGENTS §8 根本不会显示);进一步核对发现它是 Building_TrapDamager,与环境中已有的原版尖刺陷阱 TrapSpike 机制重复(用户判定: 重复,删)。
- 处置: ✅ 已删除该 def 并从子分类 defNames 移除(结构数 20→19);贴图暂留,将来做尖桩战壕可复用。

**② 蓝图书只卖不回买 → ✅ 已修(22:40)**

- 现状: 复核更正: 蓝图书其实已通过抽象基类 RK_BlueprintBookBase 带 tradeTags = Exotic + Blueprint(21:50 版用单数 <tradeTag> 正则误判为 0);真正缺的只是商人侧没有 StockGenerator_BuyTradeTag → 只卖不回。
- 处置: ✅ 已给 RK_TraderKind_BlueprintScholar 增补 StockGenerator_BuyTradeTag(tag=Blueprint),TraderKinds_BlueprintScholar.xml 已改并双目录同步。

**③ 研究门禁映射不完整 → ✅ 已补(22:40)**

- 现状: MO 原档把门全锁 DankPyon_RusticFurniture、墙与柱锁 Stonecutting(写在抽象基类上);移植时只显式挂了 6 个(双格门/加固大门 4 + 都铎/城堡墙 2),木墙与柱还残留空的 Inherit=false 清零节点 → 大部分开局即可建,且 About 原表述含混。
- 处置: ✅ 已按 HSK 语义重映射: 单格门/木门框无门槛(同原版门)、木墙 4 + 柱 3 挂 ComplexFurniture(木制品挂采石不合理)、石墙 2(射孔版经继承)挂 Stonecutting;About 第五段改写为这套三档规则并标注拒马桩已删。

**④ 前缀撞名 → 📝 已留档(无需改代码)**

- 现状: Rustic_ 前缀与 Ideology DLC 的 ThingStyleDef(Rustic_Table1x2c / Rustic_Column / Rustic_SimpleHelmet / Rustic_StandingLamp 等 14 个)同前缀,但 defName 完全不同,Unified.xml 核对无覆盖、无冲突。
- 处置: 留档即可;后续再移植 MO 家具时注意别顺手复用这些名字。


## 五、逐单元明细(全量)

> 按 AGENTS §1「单文件 ≤10KB」铁律,84 行明细拆到外挂分册: `中世纪MO整合进度盘点_明细上.md`(板块 1-3)、`中世纪MO整合进度盘点_明细中.md`(板块 4-8)、`中世纪MO整合进度盘点_明细下.md`(板块 9-13)。
## 六、下一步三档推进建议

**第一档 · 低成本高回报(零 DLL / 零 CE 冲突)**

- §3 娱乐桌椅凳 + 床(20 def,纯原版 Furniture)
- §3 装饰艺术件(雕塑/喷泉/横幅/招牌,≈30 def)
- §3 无电照明与火源(火把/油灯/蜡烛/壁炉,≈16 def)
- §3 特色地板(陶瓷砖 4 纹,Beauty+4 天花板档)
- §10 传说书 Tale(娱乐书,原版 Book)
- §11 战士/老兵 2 特性(并入 HSK 特性池)
- ✅ ①②③ 已于 08-27 22:45 修完(见第四节)

**第二档 · 中成本(机制可控)**

- §16 矿井采掘 —— 方案已定,建议作为第一个独立小项目落地(范围比武器/防具小)
- §3 储物专用储具/货架(先与 sbz NeatStorageFridge / Reel存储 定主方案)
- §9 中毒累积系统(自研 hediff,理念与硬核契合)+ 酒类主链决策
- §2 无电保鲜冰链 / 造纸抄写线(需 Processor Framework 或自研替代)
- §8 商人任务事件 P2/P3(藏宝图→定向挖宝 + 废墟清剿脚本,VEF KCSG 已在场)

**第三档 · 高成本(依赖 MO DLL 或整套 CE 重写)**

- §4 武器 CE 化(40+ 近战转 ToolCE,弓弩手炮弹药化)
- §5 防具 mm 护甲化 + 自定义装备层在鼠族体型的覆盖验证
- §6 盾牌(VEF↔CE 互斥)/ 魔法戒指 / 护甲附件
- §7 怪物 12 种(沉睡/分裂/再生/整吞/酸血)
- §8 派系 / 黑森林群系 / 藏身处 / 围城 AI

**战略分界(需要你拍的一件事)**

- MO 三大价值块 = 生产链 / 装备武器 / 战役玩法。生产链已用 HSK+Vile 上位替代并确认 ⛔;
- 剩下真正拉开体验差异的是 §4/§5 装备武器(高 CE 成本)与 §8 战役玩法(整套 MO 战役)。
- 其余(家具/装饰/风味)可按第一档零散捡完,不影响主线。

---

*本文只做「整合到哪 + 还缺什么 + 修什么」的实测盘点;实际改动以游戏内实测为准,落地遵循 AGENTS.md §3(补丁)/§4(汉化)/§6(研究台六档)/§7(CE)/§8(二级菜单),改完立刻双目录同步(§1)。*

---

**M1 进度指针(2026-08-28 01:2x)** P1 首批(可搜刮容器 10 个 + 自研 `Building_LootableHSK` + `RK_Incident_RuinCache` 事件 + 29 张贴图)**已在工作区完成并编译通过,按用户要求未同步部署目录**,故本盘点数字尚未计入;明细见 `MO整合执行计划/01_P1_战利品废墟任务蓝图链.md` 的「M1 落地记录」。

**M2/P3-P5 进度指针(2026-08-28 03:4x)** 本轮又落地 M2 探索链 + P3 装饰家具地形 + P4 商人/剧本/特性/文化/书 + P5 矿井与宝石,约 **240 个新 def / 470 张贴图 / 1 次 DLL 重编译**,**按你的要求全部未同步**,故上面 10.1% 仍是旧口径。清单与待验步骤见 `MO整合执行计划/06_P1-M2落地记录与本轮进度.md`。
