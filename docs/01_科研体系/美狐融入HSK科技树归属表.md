# 美狐(Miho)融入 HSK 科技树 —— 归属表

> 生成:`_tmp/gen_miho_integration_doc.py` 读取 1.6 源 defs + `HSK_1.6` 补丁经 lxml 模拟后的终态。  
> 校验:`_tmp/verify_miho_integration.py` 21 项全通过。  
> ⚠ **需重启游戏生效** —— `MissileGirl/Cache/Unified.xml` 与 `Mods/Unified.xml` 仍是上一次启动的终态。

## 一、为什么原先沉在最底

`ResearchTreeSK.PopulateNodes` 把节点分两堆:

- `NodesSK` = 带 `ResearchTreeSK.ResearchTreeSKModExtension` → **保留 XML 坐标,进主网格**,按页签分层、按档位列对齐。
- `NodesOther` = 没有该扩展 → X/Y 置 -1,由 `OrderAndPlaceOtherNodes` 按"前置深度"自动排布,**整堆追加在整张表最下方**,页签只当分组标签。

实测全环境 517 个研究节点里,`NodesOther` **只有美狐这 23 个** —— 所以美狐整条线被沉到科技树最底、脱离档位列。

## 二、落位方式二选一:选"显式坐标 + 进 NodesSK"

| 方案 | 效果 | 结论 |
|---|---|---|
| 继续走 NodesOther 自动排布 | 永远留在主网格下方的独立带,页签仍是孤立分组 | ❌ 达不到"融入" |
| **显式坐标 + `ResearchTreeSKModExtension`** | 进入主网格,显示在对应 HSK 页签、与档位列对齐 | ✅ 采用 |

代价:`PopulateNodes` 的坐标查重 `(X,Y)` **不分页签**,撞车即 `Log.Error("HardcoreSK research ... have the same position")`。
对策:坐标不手填,由 `_tmp/miho_layout_design.py` 先读出全部 494 个 `NodesSK` 占格再求解,并校验
前置存在 / 拓扑无环 / techLevel 不低于前置 / X 严格大于所有前置 / X 不小于档位左界 / `(X,Y)` 全局唯一。
落位行刻意选在页签带尾部稀疏行:`Apparel_SK` Y=47(全局空行)、`Weapon_SK` Y=38、`Buildings_SK` Y=27~28、`Craft_SK` Y=18~20。

⚠ **Core_SK / ResearchTreeSK 升级后必须重跑 `_tmp/miho_layout_probe.py` + `miho_layout_design.py` 复核撞车。**

## 三、总表:节点 → 页签 → 前置 → techLevel

页签分布:服装_SK 6 / 手工_SK 4 / 建筑_SK 8 / 武器_SK 5 = 23


### 服装_SK(Apparel_SK,6 节点)

| 中文名 | defName | techLevel | (X,Y) | 研究台 | 设施 | SK 可接受台 | 前置 |
|---|---|---|---|---|---|---|---|
| 美狐衣物 | `Miho_ApparelBasic` | Medieval | (5,47) | 基础研究台 | — | 基础研究台 + 高级研究台 | Apparel_B1(Apparel production II) |
| 乌木丝绸 | `Miho_Silk` | Medieval | (6,47) | 基础研究台 | — | 基础研究台 + 高级研究台 | Miho_ApparelBasic(美狐衣物), Fabric_Plants_B1(Fabric Plants III) |
| 美狐先进衣物 | `Miho_ApparelAdvanced` | Industrial | (7,47) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Miho_Silk(乌木丝绸), ComplexClothing(Advanced sewing II) |
| 美狐灵素衣物 | `Miho_ApparelEltex` | Spacer | (17,47) | 高级研究台 | — | 高级研究台 | Miho_ApparelAdvanced(美狐先进衣物), MicroelectronicsBasics(Electronics I) |
| 美狐刺客护甲 | `Miho_ApparelAssassin` | Spacer | (18,47) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_ApparelEltex(美狐灵素衣物) |
| 美狐女巫护甲 | `Miho_ApparelSorcerees` | Spacer | (20,47) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_ApparelAssassin(美狐刺客护甲), Miho_Persona(谐波物体还原理论) |

### 手工_SK(Craft_SK,4 节点)

| 中文名 | defName | techLevel | (X,Y) | 研究台 | 设施 | SK 可接受台 | 前置 |
|---|---|---|---|---|---|---|---|
| 机械素骇入 | `Miho_MechHack` | Spacer | (18,18) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_HeavyFactory(美狐重工业), MultiAnalyzer(Research technologies V) |
| 谐波物体还原理论 | `Miho_Persona` | Spacer | (19,18) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_MechHack(机械素骇入), Electronics_D1(Electronics III) |
| 超凡科技量产织造 | `Miho_Celestial` | Ultra | (22,18) | 高级研究台 | 多元分析仪、实验室工作站 | 高级研究台 | Miho_Persona(谐波物体还原理论), Components_E1(Components IV) |
| 大型机械素核心 | `Miho_MechCore` | Spacer | (18,20) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_HeavyFactory(美狐重工业), MultiAnalyzer(Research technologies V) |

### 建筑_SK(Buildings_SK,8 节点)

| 中文名 | defName | techLevel | (X,Y) | 研究台 | 设施 | SK 可接受台 | 前置 |
|---|---|---|---|---|---|---|---|
| 支援无人机 | `Miho_CommandDrone` | Spacer | (20,27) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_LightWarDrone(先进战斗无人机), Miho_Persona(谐波物体还原理论), HighMechtech(high mechtech) |
| 基础美狐工厂 | `Miho_BasicFactory` | Industrial | (7,28) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Electricity(Power engineering I) |
| 美狐重工业 | `Miho_HeavyFactory` | Industrial | (9,28) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Miho_BasicFactory(基础美狐工厂), Machining(Guns I) |
| 美狐无人机 | `Miho_Drone` | Industrial | (13,28) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Miho_BasicFactory(基础美狐工厂), BasicMechtech(basic mechtech) |
| 基础战斗无人机 | `Miho_GuardDrone` | Industrial | (15,28) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Miho_Drone(美狐无人机), Turrets_31(Turrets I), Miho_Weapon(美狐武器) |
| 先进战斗无人机 | `Miho_LightWarDrone` | Industrial | (16,28) | 高级研究台 | — | 高级研究台 | Miho_GuardDrone(基础战斗无人机), StandardMechtech(standard mechtech), Mortars(Artillery I) |
| 火炮无人机 | `Miho_HeavyWarDrone` | Spacer | (20,28) | 高级研究台 | — | 高级研究台 | Miho_LightWarDrone(先进战斗无人机), HighMechtech(high mechtech) |
| 集束火炮无人机 | `Miho_HeavyWarBattery` | Spacer | (21,28) | 高级研究台 | — | 高级研究台 | Miho_HeavyWarDrone(火炮无人机) |

### 武器_SK(Weapon_SK,5 节点)

| 中文名 | defName | techLevel | (X,Y) | 研究台 | 设施 | SK 可接受台 | 前置 |
|---|---|---|---|---|---|---|---|
| 美狐武器 | `Miho_Weapon` | Industrial | (11,38) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | BlowbackOperation(SMG I) |
| 害虫控制武器 | `Miho_VerminControlWeapon` | Industrial | (12,38) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Miho_Weapon(美狐武器), GasOperation(Shotgun I) |
| 美狐战斗护甲 | `Miho_HeavyWeapon` | Industrial | (15,38) | 基础研究台 | 研究终端 | 基础研究台 + 高级研究台 | Miho_Weapon(美狐武器), PrecisionRifling(Sniper rifles II) |
| 美狐等离子武器 | `Miho_PlasmaWeapon` | Spacer | (19,38) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_HeavyWeapon(美狐战斗护甲), ChargedShot(Directed-energy weapon I) |
| 美狐等离子突击炮 | `Miho_HeavyPlasma` | Spacer | (21,38) | 高级研究台 | 多元分析仪 | 高级研究台 | Miho_PlasmaWeapon(美狐等离子武器), Miho_HeavyWarDrone(火炮无人机) |

## 四、前置挂接说明(不再从虚空起步)

| 入口节点 | 新挂 HSK 前置 | 该前置档位/坐标 | 理由 |
|---|---|---|---|
| `Miho_ApparelBasic` 美狐衣物 | `Apparel_B1` 服饰生产 II | Medieval (4,39) | 原**无任何前置**,是整条服装线的虚空起点;先掌握基础服饰生产再学美狐文化 |
| `Miho_Silk` 乌木丝绸 | 追加 `Fabric_Plants_B1` 纺织植物 III | Medieval (3,56) | 丝绸原料来自纤维作物,顺带把服装线与植物线打通 |
| `Miho_ApparelAdvanced` 美狐先进衣物 | 追加 `Miho_Silk` | — | 原只挂 ComplexClothing;先进衣物要用丝绸,让丝绸成为必经节点而非旁支。2026-08-30 删去 `Miho_ApparelBasic` 直连边(冗余:Miho_Silk 已传递该前置,双挂触发 ResearchTreeSK "redundant prerequisites" 告警) |
| `Miho_HeavyFactory` 美狐重工业 | 追加 `Machining` 机械加工 | Industrial (8,30) | 重工业需要机床能力,同时把工厂线锚进 HSK 工业段 |
| `Miho_Persona` 调和客观环原理论 | 追加 `Electronics_D1` 电子学 III | Spacer (17,21) | 机械灵魂属电子学前沿,避免太空段节点只依赖美狐内部链 |
| `Miho_Celestial` 超凡科技量产织造 | 追加 `Components_E1` 组件 V | Ultra (21,21) | 极致档合金需要极致档组件,与 HSK 极致段对齐 |
| `Miho_GuardDrone` 基础战斗无人机 | `GunTurrets` → `Turrets_31` 炮塔 I | Industrial (14,36) | Core_SK 把 GunTurrets 抬到 Spacer,原写法会触发传递闭包越级告警;换同工业档前置清零 |

其余节点原本已挂 HSK/原版前置(`Electricity`/`BlowbackOperation`/`GasOperation`/`PrecisionRifling`/`ChargedShot`/`ComplexClothing`/`MicroelectronicsBasics`/`BasicMechtech`/`StandardMechtech`/`Mortars`/`HighMechtech`/`MultiAnalyzer`),保持不变。

## 五、研究台门槛(HSK 六档,AGENTS.md §6 / docs/08 §1b)

- 中世纪档 → `SimpleResearchBench`,SK 可接受台 `[基础研究台, 高级研究台]`
- 前工业档 → `SimpleResearchBench` + 设施 `研究终端`,可接受台同上
- 太空档 → `HiTechResearchBench`(多数带设施 `多元分析仪`),可接受台 `[高级研究台]`
- 极致档 → `HiTechResearchBench` + 设施 `多元分析仪`+`实验室工作站`,可接受台 `[高级研究台]`

> ⚠ 关键坑:`LabTerminal`/`LabStation`/`MultiAnalyzer` 是**设施**(`SK.Building_Lamp`),不能写进 `requiredResearchBuilding`,否则节点在任何研究台都点不动且日志零报错。详见 `docs/08_铁律/研究台档位机制.md` §1b。

## 六、校验结果

```
== 1. 补丁模拟执行 ==
  [OK] Operation 全部命中并执行 (25)
  [OK] 覆盖 23 个美狐节点
  [OK] 研究台全部是真正的 Building_ResearchBench
  [OK] 可研究性 23/23 通过
  [OK] tab/坐标/前置 与设计表逐项一致
  [OK] 每个节点有 SK.AdvancedResearchExtension
  [OK] 每个节点有 ResearchTreeSKModExtension(进 NodesSK 主网格)
  [OK] 与 494 个 NodesSK 占格零撞车(不分页签)
  [OK] 美狐内部坐标互不撞车
  [OK] 前置 def 全部存在
  [OK] techLevel 不低于任何前置(不触发 RTSK 自动抬档)
  [OK] X 严格大于所有前置(不触发 not positioned to the right)
  [OK] X 不小于档位左界(不落入更低档列)
  [OK] 美狐内部依赖拓扑无环
  [OK] TechLevelLeftBounds 未位移
  [OK] Miho_ResearchTab 定义已退役
  [OK] 1.6 源 defs 无 Miho_ResearchTab 残留
  [OK] 合并终态无 Miho_ResearchTab
  [OK] Miho_Schematic 书籍 doers 存在
  [OK] 书籍不再引用已退役的 Miho_ResearchTab
  [OK] 书籍 include == 23 个美狐节点
```

## 七、改动文件

| 文件 | 改动 |
|---|---|
| `美狐HSK拓展/1.6/Defs/ResearchProjectDefs/Research.xml` | 23 节点 tab / researchViewX,Y / prerequisites |
| `美狐HSK拓展/1.6/Defs/ResearchProjectDefs/ResearchTabs.xml` | 退役 `Miho_ResearchTab`(留空文件+注释便于回退) |
| `美狐HSK拓展/1.6/Defs/Resource.xml` | `Miho_Schematic`(美狐技术文档)研读奖励:`tabs=[Miho_ResearchTab]` → `include` 显式列举 23 节点。`ReadingOutcomeDoerGainResearch.IsValid` 要求 `project.tab ∈ tabs`,页签退役后不改会让商人出售的这本书一个都选不中 |
| `美狐HSK拓展/HSK_1.6/Patches/ResearchProjectDefs/Miho_ResearchBench.xml` | 25 Operation:六档研究台 + `SK.AdvancedResearchExtension` + `ResearchTreeSKModExtension` |
| `美狐HSK拓展/HSK/Languages/{简中,英,俄}/DefInjected/ResearchTabDef/ResearchTabs.xml`、`Cont/Languages/English/.../Miho, the celestial fox - 2816826107.xml` | 移除悬空 `Miho_ResearchTab.label` 键 |

备份:`_tmp/backup_miho融入hsk_20260827/`(改动前原文件)。1.5 分支未动(本环境只跑 1.6,1.5 仍用独立页签)。
