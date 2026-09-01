# [SYR] Processor Framework 框架梳理

> 梳理日期: 2026-08-26。目标: 作为 HSK(硬核Sky)+ 中世纪大修(Medieval Overhaul)整合包的一部分,梳理其机制与整体结构。
> 数据源: Workshop id 3210544395 本地副本(3.3MB,125 文件,ProcessorFramework.dll 79KB)。
> 关联: 中世纪大修的全部 19 种自动批处理加工机(风车/水车/窑/坩埚/冶炼炉/鞣革架/烟熏房/造纸压机/发酵桶等)都建立在本框架之上,详见 `docs/03_生产框架/中世纪大修内容梳理.md` §2.1。

---

## 0. 概览

| 项 | 内容 |
|---|---|
| 名称 | [SYR] Processor Framework(通用发酵桶 Universal Fermenter 的继任者) |
| 作者 | Syrchalis(续更) |
| 版本 | 支持 1.3/1.4/1.5/1.6 |
| packageId | syrchalis.processor.framework(vr.processor.framework 为另一发布渠道) |
| 定位 | **纯框架 mod,几乎不带游戏内容**——给 mod 作者提供"自动批处理"能力: 把一套原料放进去,机器自主在时间内把它变成产物 |
| 硬依赖 | 仅 Harmony(0Harmony.dll 直接打包在 Assemblies 里) |
| 规模 | ProcessorFramework.dll(79KB,单命名空间,~24 个类型)+ 1 示例建筑 + 1 补丁文件 + 7 个第三方适配子模块 + 35 张贴图 + 89 个英文 Keyed |
| 自带简中 | **无**,仅英文 Keyed(PF_Keys.xml 89 key),需整合包自译 |

**一句话**: 任何 mod 作者只要给建筑挂 `ProcessorFramework.CompProperties_Processor` + 声明若干 `ProcessorFramework.ProcessDef`,就免费获得——定时自动加工、原料过滤、品质成长、温度/天气/燃料/电力多因子调速、副产物、损坏重建、小人自动投料/收料、双页检查 UI。

---

## 1. 架构与核心概念

全部代码在单一命名空间 `ProcessorFramework`。核心对象关系:

```
ThingDef(建筑,带 CompProperties_Processor)
  └─ CompProcessor(ThingComp,运行时组件)
       ├─ activeProcesses: List<ActiveProcess>     当前进行中的工序
       ├─ enabledProcesses: Dict<ProcessDef, ProcessFilter>  已启用配方+每配方原料过滤器
       ├─ innerContainer: ThingOwner               存放原料实体
       └─ cachedTargetQualities: Dict<ProcessDef, QualityCategory>  品质目标
            │
            ├─ ActiveProcess  单个工序运行时状态(进度 tick + 速度因子计算)
            ├─ ProcessDef     配方定义(成分→产物+全部参数,见 §3)
            ├─ ProcessFilter  每台机器每个配方的允许原料过滤
            ├─ BonusOutput    副产物(thingDef+chance+amount)
            └─ QualityDays    品质-天数阈值表(awful~legendary 7 档)

周边:
  MapComponent_Processors   每图注册表(处理器列表+潜在原料缓存,300 tick 缓存)
  WorkGiver_Fill/EmptyProcessor + JobDriver_Fill/EmptyProcessor   投料/收料工作
  ITab_ProcessSelection / ITab_ProcessorContents   检查页签1(配方选择)/页签2(进度+出料)
  Building_ColorCoded       处理器建筑基类(按配方颜色着色)
  PF_Settings               全局设置(ModSettings)
```

**关键设计**: 工艺(ProcessDef)与机器(ThingDef)分离——同一台机器(如 BarrelProcessor)可通过子模块叠加任意多个工艺(VGP 子模块让它同时能酿 19 种酒);同一工艺可挂在多台机器上(中世纪大修的鞣革工艺同时挂鞣革架与工业鞣革桶)。

---

## 2. CompProcessor 工作流详解(放料→加工→出料)

### 2.1 放料(小人自动执行)

1. `WorkGiver_FillProcessor`(Hauling 工作,priority 19)派活 → `JobDriver_FillProcessor` 执行: 走向机器 → 走向原料 → 扛起 → 回到机器 → 等待 200 tick → `CompProcessor.AddIngredient(原料, 工艺, 数量)`。
2. `AddIngredient`: 若配方 `useStatForEfficiency` 则按原料属性折算"有效量";堆满时只收一部分、多余丢回地上。
3. 合并规则: 找同配方现有工序——`parallelProcesses=false` 并入现有工序(进度按加权平均重算);否则新建工序;`independentProcesses=false` 时全机只允许一个工序(同原版啤酒发酵)。
4. 原料实体存进 `innerContainer`(用于出料时记录成分/屠宰引用)。

### 2.2 加工(计时,惰性)

- `CompTick` 仅 18 条指令: 每 60 tick 调 `DoTicks(60)`,每 250 tick 调 `DoActiveProcessesRareTicks()` + `AdjustPowerConsumption()`;开头立即短路(空机/关机直接 return)。
- `ActiveProcess.DoTicks`: 进度 += ticks × speedFactor(60000 tick = 1 天);若温度越出安全区,按 `ruinedPerDegreePerHour` 累积损坏,满 100% 发 `RuinedByTemperature` 信号(品质变差)。
- 速度因子 `CalcSpeedFactor` 每 250 tick 重算 = power × fuel × temperature × sun × rain × snow × wind 连乘,见 §4。

### 2.3 出料(小人自动执行)

1. `WorkGiver_EmptyProcessor`(Hauling,priority 20,高于投料)→ `JobDriver_EmptyProcessor` → `CompProcessor.TakeOutProduct(工序)`。
2. 生成主产物: 堆叠数 = 原料数 × efficiency;记录消耗原料到产物 `CompIngredients`。
3. `usesQuality=true` 且有 CompQuality → 按工序已过天数对照 `qualityDays` 定品质(天数越久品质越高,不达标也可取出只是低品)。
4. 遍历 `bonusOutputs`: 按 chance 产副产物;副产物若是生物则生成活体 pawn。
5. `destroyChance`: 概率触发机器"烧毁"→ 若设置开启则原位放重建蓝图。
6. 活体产物直接生成在机器旁;物品由小人搬运到储存区。

---

## 3. ProcessDef 字段清单(数据规范,mod 作者写配方用)

> 这是整合包新增"自动加工建筑/工艺"时必须遵循的字段表。必填仅 defName + ingredientFilter + thingDef;其余可选。

| 字段 | 类型 | 作用 |
|---|---|---|
| thingDef | ThingDef | 主产物 |
| ingredientFilter | ThingFilter | 可投入原料(必填否则工序无法启用) |
| processDays | float | 基准加工天数(60000 tick/天) |
| capacityFactor | float | 每单位原料占用的机器容量 |
| efficiency | float | 出料倍率(原料数×效率=产物数) |
| usesTemperature | bool | 是否启用温度机制 |
| temperatureSafe / temperatureIdeal | FloatRange | 安全/理想温度区间 |
| ruinedPerDegreePerHour | float | 超出安全温度 1°C/小时的损坏百分比 |
| speedBelowSafe / speedAboveSafe | float | 低于/高于安全温度的速度倍率 |
| sunFactor / rainFactor / snowFactor / windFactor | FloatRange | 天气速度因子(min~max) |
| unpoweredFactor / unfueledFactor | float | 断电/缺燃料速度倍率 |
| powerUseFactor / fuelUseFactor | float | 每单位原料的耗电/耗燃料权重 |
| filledGraphicSuffix | string | 装满时贴图后缀(如 `_Full`,热替换) |
| usesQuality | bool | 是否启用品质成长 |
| qualityDays | QualityDays | 7 档品质天数阈值(awful~legendary) |
| color | Color | 进度条/建筑叠加色(colorCoded=true 时) |
| customLabel | string | 覆盖配方显示名 |
| destroyChance | float | 完工后机器毁坏概率 |
| bonusOutputs | List\<BonusOutput\> | 副产物列表(chance+amount;可为活体) |
| useStatForEfficiency / efficiencyStat / statBaselineValue | bool/StatDef/float | 用原料属性折算效率(如按原料品质) |

---

## 4. 因子系统(速度如何计算)

`CalcSpeedFactor` = 六因子连乘(每 250 tick 重算):

| 因子 | 规则 |
|---|---|
| 温度 | 不在温度机制时 = 1.0;低于 safe.min → speedBelowSafe;高于 safe.max → speedAboveSafe;safe~ideal 间平滑过渡;ideal 内 = 1.0 |
| 电力 | 无 CompPowerTrader 或未通电 → unpoweredFactor(通常 0 = 断电停工) |
| 燃料 | 无 CompRefuelable 或缺燃料 → unfueledFactor |
| 阳光 | 有顶棚 → sunFactor 最小值(棚下阳光=0);无棚 → 按 CurSkyGlow 在 min~max 间插值 |
| 雨/雪 | 按 RainRate/SnowRate 插值;雨量还要×(1-屋顶覆盖率) |
| 风 | 按 WindSpeed(0~3)插值;有屋顶直接取 min |

燃料/电力消耗率 = 各工序 `fuelUseFactor × 原料数 × capacityFactor` 的加权平均,按原料占比分摊;遵循原版 `consumeFuelOnlyWhenUsed`/`consumeFuelOnlyWhenPowered` 语义。

**中世纪大修扩展示例**(MO 补丁 Postfix 改出参): 风车/水车把"风因子"再乘 `CurrentSpinSpeed()`(转速逻辑);冶炼炉/坩埚把速度因子再乘自定义燃料效率;工序每 tick 刷新建筑火焰发光。

---

## 5. Job / WorkGiver / UI

| 项 | 内容 |
|---|---|
| 工作 | `FillProcessor`(投料)/ `EmptyProcessor`(收料)两个独立 Job,均 suspendable=false |
| WorkGiver | 都挂 **Hauling** 工作类型、需 Manipulation;投料 priority 19、收料 20(收料优先) |
| ReservationLayer | `PF_Fill` / `PF_Empty` 两个独立占用层(投料与收料互不阻塞) |
| ITab 1(ProcessSelection) | 检查页签: 列出可用产品,成分作子节点,玩家可勾选启用/禁用产品与成分 |
| ITab 2(ProcessorContents) | 各工序进度、品质下拉、取货;产物图标实时显示在建筑上 |
| 其它 UI | 进度条(PostDraw)、品质图标(QualityIcons 7 张)、EmptyNow gizmo/指令、配方图标(可全局开关/调大小) |

---

## 6. Harmony 补丁与设置

### 6.1 补丁(仅 3 个)

| 补丁 | 目标 | 效果 |
|---|---|---|
| OldBarrel_GetInspectStringPatch | Building_FermentingBarrel.GetInspectString | 原版发酵桶检查文本换成"请用新处理器"提示 |
| CurTabsPatch | MainTabWindow_Inspect.CurTabs | 多选同类处理器时统一返回同一套页签,避免闪烁 |
| HarmonyPatches | [StaticConstructorOnStartup] PatchAll | 启动入口(Harmony id: Syrchalis.Rimworld.UniversalFermenter) |

### 6.2 设置(PF_Settings)

显示类: showProcessIconGlobal(配方图标)/ processIconSize(0.6,滑条)/ showCurrentQualityIcon / singleItemIcon / productIcon / ingredientIcon / showProcessBar(进度条)。
行为类: defaultTargetQuality(默认目标品质 0~6)/ initialProcessState(新机器默认配方状态: 全禁用/全启用/仅第一个)/ **replaceDestroyedProcessors**(烧毁时自动放重建蓝图)/ **ReplaceVanillaBarrels**(把地图上原版发酵桶+微缩件换成 BarrelProcessor 并迁移麦芽汁与进度——**整合时注意**)。

---

## 7. 性能评估(对照 AGENTS.md §8 性能铁律)

**结论: 完全符合低频轮询规范,且是"越空闲越省"的惰性设计。**

- CompTick 仅 18 条指令,空机/关机直接短路返回;每 60/250 tick 才做实质工作,无每 tick 全量遍历。
- 全图原料扫描走 `MapComponent_Processors` 的 300 tick 缓存 + ListerThings(非逐格扫描);WorkGiver 直接返回处理器注册表(PostSpawnSetup 注册/PostDeSpawn 注销),避免每帧遍历全图建筑。
- LINQ 委托静态缓存,非每 tick 分配;唯一每帧工作是 PostDraw 画进度条(纯渲染)。
- 注意点: 每台机器每 250 tick 遍历一次 activeProcesses 和天气查询,全图上千台机器时仍需留意;FindIngredient 的 ThingFilter.AllowedThingDefs 有重复枚举,但只在小人闲时派发工作才触发。

---

## 8. 自带 def(示例建筑)

**BarrelProcessor(万能发酵桶)**: 1×1,Woody 任意木料(10 钢 + 30 木),研究 Brewing;capacity 25,independentProcesses=false(同原版桶堆叠合并),parallelProcesses=false;自带工艺 **Beer**(麦芽汁→啤酒,6 天,温度安全 -1~32°C/理想 7~32°C)。同文件含演示工艺 Test 与调试 SoundDef PF_Honk。

---

## 9. 第三方适配子模块矩阵(loadFolders IfModActive 条件加载)

| 第三方 mod(packageId) | 子目录 | 提供的工艺/改造 |
|---|---|---|
| VFE Medieval 2(OskarPotocki.VFE.Medieval2) | 1.6/Mods/VFE_Medieval | `VFEM2_Wine`(葡萄醪→酒,品质熟成)/ `VFEM2_Mead`(蜂蜜→蜂蜜酒);删原版 VFE 酒桶/蜂蜜酒桶,工艺并入 BarrelProcessor;VFE Vikings 的 mead 工艺也在此目录(按 VFE.Medieval2 条件加载) |
| VFE Settlers | 1.6/Mods/VFE_Settlers | `VFES_Chemsine`(化工燃料→私酿酒,5 天),并入 BarrelProcessor |
| RimBees(sarg.rimbees) | 1.6/Mods/RimBees | `RB_Mead`(蜂酒醪→蜂酒),并入 BarrelProcessor |
| Fertile Fields | 1.6/Mods/FertileFields | `RFF_FertilizerBin`(堆肥→肥料,3.5 天,destroyChance 1.0 烧毁重建,副产木料)/ `RFF_FertilizerBarrel`;把原 CompostBin/Barrel 的旧 comp 换成 PF comp |
| Salted Meat(Argon.SaltedMeatRemake) | 1.6/Mods/SaltedMeat | `SM_SaltedMeat`(生肉→腌肉,12 天,禁人肉)/ `SM_Sausage`(生香肠→香肠,品质熟成)/ 装 VFE Fishing 时加 `SM_DriedFish`(20 天);肉架/香肠架换 PF comp,贴图换 PF 自带 `_Full` 装满外观 |
| VGP Garden Drinks | 1.6/Mods/VGP | 9+ 种酒工艺(朗姆/浆果酒/神馔酒/清酒/伏特加/龙舌兰/威士忌/水果烈酒/格瓦斯),大桶 capacity 120,同时并入 BarrelProcessor;VFE Medieval 活跃时酒类用 VFE 工艺 |
| VFE Vikings | (1.6 目录为空,内容并入 VFE_Medieval) | 同 VFE_Medieval 行 |

**加载注意**: v1.6 的 VFE_Medieval 条件 packageId 是 `OskarPotocki.VFE.Medieval2`(v1.3/1.4 是 `…MedievalModule`);若整合包用旧版 VFE Medieval 1 的 packageId 将不加载适配。

---

## 10. 语言与贴图

- **语言**: 仅 `Languages/English/Keyed/PF_Keys.xml` 一个文件,89 个 `PF_` 前缀 key(设置项 25+/Inspector 文案/ITab 文案/品质 tooltip),**无 DefInjected**。简中缺失 → 整合包需翻译全部 89 个 key。
- **贴图**(35 张): BarrelProcessor 六向(128×128 含 mask)、酿造台 TableBreweryStuffed(224×96)与 TableBreweryVTEXE(448×192 HD,仅装 Vanilla Textures Expanded 时用)、SaltedMeat 肉架/香肠架/鱼干架(含 `_Full` 装满态)、品质图标 7 张、EmptyNow gizmo 图标。全部标准 texPath,可被整合包重贴图。

---

## 11. 依赖与加载顺序

- 依赖: 仅 Harmony;Assemblies 里直接打包了 0Harmony.dll(与其他 mod 的 Harmony 共存无冲突,同版本同源)。
- About: loadBefore SimpleChains.Leather/Lumber;loadAfter SaltedMeatRemake、VGP Garden Drinks。
- **HSK 整合加载顺序要求: PF → 中世纪大修**(MO 硬依赖 PF,且 MO 的 ActiveProcess/CompProcessor 补丁需在 PF 之后应用)。

---

## 12. 对整合包的意义与冲突点

### 12.1 意义(为什么必须带它)

1. 中世纪大修的 19 种自动加工机(风车/水车/窑/坩埚/冶炼炉/鞣革架/烟熏房/造纸压机/奶酪压机/发酵桶/木炭堆/制冰/丝绸床等)全部建立在本框架上,MO 是它的最大用户。
2. 框架提供完整"自动批处理"基建: 工艺 def 规范(§3)、多因子调速(§4)、品质成长、副产物、投料/收料工作、双页 UI——整合包若想新增自己的自动加工线(如鼠族专用加工台),照 ProcessDef 规范写即可,性能有保障。
3. 性能优秀(§7),大规模挂载不拖帧,符合项目"低频轮询"铁律。

### 12.2 中世纪大修的三种接入示范(HSK 新增机制可照抄)

- **(a) 速度因子后乘**: Harmony Postfix 改 `ActiveProcess.CalcSpeedFactor` 出参(MO 加燃料效率/风车转速)。
- **(b) 出料劫持**: Prefix/Postfix 改 `CompProcessor.TakeOutProduct` 产物来源/数量(MO 改造为"屠宰产出",如生皮→皮革按屠宰表)。
- **(c) 配方级配置**: DefModExtension 挂 `ProcessDef.modExtensions`(MO 的 `ProcessorExtension.outputOnlyButcherProduct`)。

### 12.3 冲突点与注意事项

1. **PF_Patches 动了原版发酵桶(designationCategory 移除)与酿造台(造价/贴图/stuff 改 Woody)**,若 HSK/中世纪大修也 patch 这两处,补丁顺序敏感(PF 需在前被改,或合并处理)。
2. **ReplaceVanillaBarrels 设置**会把地图上原版桶迁移成 BarrelProcessor——整合包若保留原版发酵玩法需关掉。
3. SaltedMeat 子模块的 `MeatRaw` 分类排除人肉,若整合改了肉类 defName 需同步。
4. 汉化需自译 89 个 Keyed key(简中完全缺失)。
5. v1.6 已无 VFE Vikings 独立条目(并入 VFE_Medieval 模块),用旧 packageId 不加载。

---

*本文档由 Processor Framework 1.6 本地副本直接读取 + DLL 反编译整理;字段/数值以 mod 内 xml 为准。*
