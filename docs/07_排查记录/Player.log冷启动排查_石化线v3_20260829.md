# Player.log 冷启动排查 + 闭环验证 · 石化线 v3 · 2026-08-29

> 日志 `AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`。对照 `Player-prev.log` 签名一致=多为存量。**v3 高风险面 100% 清白**。
> 本报告含**两轮真机冷启动闭环验证**（代理自启动 RimWorld、等主菜单就绪、重扫、修、再启），已收敛到仅剩第三方+慢性噪音。

## 0. 结论

v3 新 DLL(`CompWasteAccumulator`)、管网出料口、两台 clone 精炼厂、卸货湾换皮、68 张死贴图归档——`WasteAccumulator`/`Could not resolve type`/`Could not load Texture2D`/`OnGUI`栈/`DrawPlaceMouseAttachments`/`already used in this mod`/`lower techlevel` **全程 0 命中**。共修 7 类本地错误(5 存量+2 清缓存后暴露)+壁炉火焰bug，全部复扫归零。

## 1. 收敛曲线（每轮真机启动）

| 轮次 | 就绪时刻 | 日志行数 | 本地真错误 | 处置 | 剩余 |
|---|---|---|---|---|---|
| R0 | 10:31 | 2767 | A–E 5 类+陈旧缓存幽灵罐 | 修 A–E+清缓存 | — |
| R1 | 18:31 | 2165 | 又暴露 F+G（被陈旧缓存掩盖）| 修 F | 2 类新 |
| R2 | 18:48 | 2147 | **0** | 修 F+G（停用）| 仅第三方 |
| R3 | 19:10 | 2164 | **0**（方案 A→G 彻底删除，`Rustic_` 零悬空）| 复核 | 仅第三方2条 |
| R4 | 19:27 | 2147 | **0** + 壁炉火焰 H 修复合入 | 合并 def 确认 FireOverlay | 仅第三方 2 条 |

本地真错误 5→(清缓存后+2=7)→0→0→0（删除/火焰修复后无回归）。第三方 2 条（JewelryBench=VFE 工坊 3219596926；`Super_matter_E2`=vitech）"上游不动"保留。

## 2. 修复明细 → 根因 → 修法

**A. 液体托盘悬空研究（v3）** `Could not resolve cross-reference … RK_Petro_Polymerization`。五档收三档后节点删、`Reel存储HSK适配` 的 `ReelStorageBarrelPallet` 仍引用 → 改指末端 `RK_Petro_Synthesis`。

**B. RKHSKFix 去木板 22×Failed to find a node（存量）** 两 50 号补丁无条件 `PatchOperationRemove` 删 `RKHSKFix_Make_RK_*` 的 WoodPlank；11 配方（Spear/Mace/Halberd/Banner/HeavyShield/TowerShield/Backpack + Pickaxe/Hockey/Axe/Cleaver，×2op）WoodPlank 已被更早补丁改写成 stuffCategories → 打空报红。22 op 套 `PatchOperationConditional` 存在性门控（只包失败配方，xpath 逐字不变，行为零变）。⚠ simulator 以 Unified 终态作基线无法模拟 01→50 次序,其"全失败"属伪报。

**C. 金鼠族非法字段（存量）** `OAGene_SnowstormCampfire` 的 `CompProperties_Refuelable` 内 `<targetFuelLevelMax>`；反编译确认 1.6 无此字段 → 删。

**D. 盔甲架三 def 整块加载失败（存量）** `盔甲架HSK适配` 把 `uiIconPath` 写进 `<graphicData>`（它是 ThingDef 级字段非 GraphicData 字段）→ 触发解析 NRE、def 丢弃、并令 About"补 uiIconPath 防 stuffable 卡死"失效。**R2 实测归零→坐实 uiIconPath 错位即 NRE 根因**。修法：uiIconPath 提到 ThingDef 级（`_south.png` 均在）。

**E. 储罐 ConfigError 缺 tickerType（v3）** 立式罐用每-tick `Refuelable` 作液位但 `Asphalt/AmmoniaTankVertical` 未设 tickerType → 补 `Normal`。同关键字的**卧式/中性胺罐幻影错误**根因＝`MissileGirl/Cache/Unified.xml` 陈旧缓存残存已删 def（live XML 零命中）→ 按 §5 清缓存。

**F. 燃气管 blueprintGraphicData（v3，R1 暴露）** `RK_Petro_GasPipe_NG/NH3` 写 `<blueprintGraphicData>`——ThingDef 无此字段（Rustic_Structures 同类不报错因其 thingClass 自定义该字段）→ 删该节点（蓝图自动派生自 graphicData，原字段本被忽略，删除零行为变化）。

**G. ProcessDefs_Rustic + 两台 PF 自动机彻底删除（08-28，R1 暴露）** `缫丝床/造纸压机` 2 条 `ProcessDef` 抛 `InvalidOperationException: <thingDef>→ActiveProcess 反序列化失败`。根因（反编译融合版 `ProcessorFramework.dll`）：`ProcessDef.thingDef`（及 `BonusOutput.thingDef`）被定为运行时 `ActiveProcess`（仅 `ActiveProcess(CompProcessor)` 构造、无无参），与 MO `<thingDef>产品` 不兼容 → 整 def 不加载 + 两台建筑 `<processes>` 报错；清缓存后 0 条 ProcessDef 能加载，功能根本无法工作。**用户确认方案 A：直接清除、放弃 Cecil 救活**——删 `Buildings_Rustic_Processors.xml`(2 机器)+`ProcessDefs_Rustic.xml`（入回收站+工作区备份）、剥 SubCategories 两 `<li>`、改 About §十。丝绸/纸张走 §八 现有人力生产线不受影响。恢复需把 PF.dll thingDef/BonusOutput.thingDef 改回 ThingDef 重编译。

## 3. 第三方 / 上游（标注不动）

`duplicate unlocked defs Super_matter_E2`（vitech ProduceYautjavium×2，上游同研究双工作台）；JewelryBench `uiIconScale` in graphicData（VFE 工坊，仅告警）；`BarrelProcessor/Beer`（融合 PF DLL 探测我方主动删的酒类）；`WorkGiver Tav/KAU` 悬空（第三方，存档工作表交叉引用）；`DoBillsMake_Miho_CelestialForge`（美狐 WorkGiver 在 `1.5/`，需专查 1.6 LoadFolders，另案）；`Alien_Miho multiple CompPawnGizmo`（CE 去重生效良性）；`InputLegacyModule`（新 Input System 迁移噪声）。

## 2H. 鼠族家具壁炉无火焰特效（视觉bug，非红字）
- **本体**：`RKFC_RGK_Fireplace`(燃料)/`FireplaceE`(电) 在 `鼠族家具拓展/Defs/RKFC_JoyTemp.xml`。
- **根因**：原版 Campfire 火焰由 `CompProperties_FireOverlay`(→CompFireOverlay.PostDraw 绘 Graphic_Flicker Things/Special/Fire，需 tickerType=Normal+RealtimeOnly)驱动，**非** CompFires。RKFC 照抄 Building_Heater 却**独漏 FireOverlay**→只发光无火焰。
- **修**：燃料版 comps 补 `<li Class="CompProperties_FireOverlay"><fireSize>1.4</fireSize></li>`（改 def 源不另起 xml；后经火焰任务调为 0.9+offset，见 §I/增量包）。电力版=TempControl 电加热器无明火，语义正确不动。R4 合并 def 确认 FireOverlay+Normal 在位。
- **附**：该 Graphic_Multi 缺 `_west` 三张向西贴图（无加载报错，走缺图回退），独立外观项。

## 2I. ProcessorFramework.dll 融合缺陷致 OnGUI 崩溃 / 小人卡死（用户追加，最高优先）
- **现象**：开小人管理面板即 `MissingFieldException: Field not found: ProcessorFramework.ActiveProcess Verse.Thing.def`，OnGUI 每帧抛打断输入→小人卡死。
- **根因**（dnfile 元数据级）：PF.dll 08-29 05:17 被一次 Cecil "DefOf" 改写（残留 `_tmp/backup_pf_defof_20260829_051743`），未增删类型，只把 `Verse.Thing::def` MemberRef 签名从外部 `TypeRef→ThingDef` 误改为本地 `TypeDef→ActiveProcess`→JIT 找不到字段。
- **排除面**：坏引用仅此 dll；v3 石化 GasPort/WasteAccumulator 在独立 BlueprintUnlockHSK.dll（Thing::def 干净），回退 PF.dll 不伤石化。
- **修法**：回退 PF.dll 到 pre-05:17 干净版（`3e24319`），双目录原子同步。放弃那次无收益的 Cecil 改写。
- **验证（R5 冷启动）**：`MissingFieldException`/`ActiveProcess`/`OnGUI`/`InvalidOperationException` 全 0，`GasPort`/`WasteAccumulator` 正常加载，前几轮零回归。OnGUI 崩溃需进局开面板现场复验。

## 4. 慢性噪音（勿追因）

`Fallback handler …Mono data-*.dll`（符号探测）；`GAGARIN Cache …created`（正常）；`Harmony FieldRefAccess StackTrace`（调试 mod 反射）；`Map Preview RNG usage`（已容错）；`Thing_* loadID … WorkTab/VanillaTraitsExpanded`（加载既有存档旧 pawn 引用，**非 def 错误**）；`Keyz GameComp/KeyHandler`、`Random state stack`、`HSK Core Error Check Finished`（通过）。228 mod 冷启动 3–7 分钟正常。

## 5. 缓存与验证

`MissileGirl/Cache`→`Cache_bak_20260829_181522`（91MB 可逆）。R3 冷启动干净重建后抽查 `Cache/Unified.xml`(终态)：`CompProperties_WasteAccumulator`=1、`RK_Petro_GasPipe_`def=2、`RK_Petro_*Refiner`=2、`ReelStorageBarrelPallet`=1、悬空 `Polymerization`/卧式罐/`Rustic_`(含已删机器/ProcessDef) **均=0**。回归：液体托盘/三件盔甲架/立式沥青·氨罐/管网/两台精炼厂 def 全在位；丝绸/纸张手动链完整；缫丝床·造纸压机已按"不留兼容层"彻底删除。游戏保持开启（R3，PID 见任务记录）。

## 6. 变更文件（工作区↔部署 SHA-256 MATCH）

修（改内容）11 件：1 `Reel存储HSK适配/…/ReelStorage_ThingDefs.xml` 研究引用→Synthesis
2 `鼠族HSK拓展/Patches/50_去除木板.xml` 14 op 门控
3 `工业大修/Patches/50_工具_材质与战斗适配.xml` 8 op 门控
4 `工业大修/1.6/Defs/Buildings_RK_Petro_Tanks.xml` 2 罐补 tickerType
5 `金鼠族/…/Buildings_Temperature.xml` 删 targetFuelLevelMax
6-8 `盔甲架HSK适配/…/Building_{ArmorRack,Mechanized,Mending}ArmorRack.xml` uiIconPath 归位
9 `工业大修/1.6/Defs/Buildings_RK_Petro_GasNet.xml` 删 2 blueprintGraphicData
10 `工业大修/1.6/Defs/SubCategories_RK_TextilePaper.xml` 剥 2 死机器 `<li>`
11 `工业大修/About/About.xml` §十 文案更正
12 `鼠族家具拓展/Defs/RKFC_JoyTemp.xml` 燃料壁炉补 FireOverlay（H）—— 部署到 packageId `local.ratkin.furniture` 真实文件夹（曾误写 HSK修复整合，已回收纠正）
删（G）：`工业大修/1.6/Defs/Buildings_Rustic_Processors.xml` + `ProcessDefs_Rustic.xml`（部署端入回收站，工作区备份 `_tmp/petro_dead_removed_20260829/`）
缓存 `MissileGirl/Cache→Cache_bak_20260829_181522`；脚本 `_tmp/gate_woodplank.py`、`_tmp/sync_logfix_20260829.py`

## 7. 交付归档 / 增量包（详见 `修复增量包_PF壁炉_20260829.md`）

- 全量(含全部修复终态)：`outputs\RimWorld_Mods_v3_20260829_2155.7z`(1.18 GiB, 压51.5%, 内 PF.dll=`3e24319`干净, `7z t`通过) + 900MB 分卷 `_2155_split.7z.001/.002`；已复制 Downloads。
- 增量修复包（主会话已交付）：`Downloads\HSK_FixPack_20260829.zip` —— apply_fix.bat(3 行 ASCII 启动器)+apply_fix.ps1(探测 Mods+备份+copy+OK/FAIL)+files/(PF.dll 回退`3e24319` + 壁炉 RKFC_JoyTemp.xml)+README；假 Mods 自测通过。
- 旧 `1935` 全量含 corrupt PF.dll，隔离 `outputs\SUPERSEDED_corruptPF_1935\`，勿发。
