# 派系战争(FW) × 政治边界(BOTR) 联动 · 设计与实装

> 立档 2026-09-02。承接 [`外政与任务大修重构_架构设计方案.md`](./外政与任务大修重构_架构设计方案.md)（Layer B/C）与 [`外政数据结构与挂钩点附录.md`](./外政数据结构与挂钩点附录.md)。
> 性质：**已反编译核验 + 已实装 Harmony 桥**（补丁并入 `HSK修复整合` 的 `HSKFixPack.dll`，不改第三方本体）。mod：`SR.ModRimworld.FactionalWarContinued`（`ModRimworldFactionalWar.dll`）+ `NehsModsForDev.bordersoftherim`（`BordersOfTheRim.dll` + `BordersOfTheRim.HSKPatch.dll`）。反编译源 `_tmp/_fwdecomp/`、`_tmp/_botr_decompile/`、`_tmp/_botrhskpatch/`。

## A. 势力行为类 mod 适配矩阵（外政/任务体系里的角色）

| mod (packageId) | 在外政体系扮演 | 唯一权威状态 | 挂载钩子（我方桥） |
|---|---|---|---|
| 边境拓展HSK `NehsModsForDev.bordersoftherim` | **领土/边界/附庸/单线承认度 + 领土战争引擎**底座（Layer A 只读复用） | `WorldComponent_Territories`：`tileOwners`(tileId→Faction)、`activeWars:List<TerritorialWar>`、`borderContactByPair` | 读 public `IsDisputedBorder/AreFactionsAtWar/OwnerAt/WarTouchingTile`；写经 public `RebuildTerritories()` + `Settlement.SetFaction`；战争生命周期 private `StartWar/EndWar/TryCaptureSettlement` 反射 best-effort |
| Factional War `SR.ModRimworld.FactionalWarContinued` | **无持久战争态的"两 NPC 派系当玩家面互殴"事件生成器**（旁观者混战），非吞并引擎 | 无 war 对象；参战对 = 运行时由 `FactionUtil.GetHostileFactionPair` 从 `HostileTo` 现挑；落点 = `parms.spawnCenter`（事件 map 上） | postfix `GetHostileFactionPair` 优选边界邻接对；postfix `IncidentWorkerFactionWar.TryExecuteWorker` → 波及提示 + 桥接 BOTR `StartWar` |
| 天网 `skyarkhangel.skynet` | **raid-boss 敌对极**（`SkynetHumanlike` permanentEnemy/hidden），非可外交派系 | FactionDef `permanentEnemy`；boss=T-X `SkynetPrototypeTX` | 见 [`天网Skynet_boss整合.md`](./天网Skynet_boss整合.md)；boss 走 T4 定向战令非随机悬赏 |

## B. FW × BOTR 联动设计

### B.0 反编译事实（决定所有挂钩方式）

- **FW 不持有战争状态**：`IncidentWorkerFactionWar`（`IncidentDefOf` 事件 `SrFactionWar`，`targetTags=Map_PlayerHome/TempIncident/Misc/RaidBeacon`，`baseChance 1`）在**事件目标 map 上**用 `FactionUtil.GetHostileFactionPair(out f1,out f2, points, Combat, candidateList, validator)` 挑一对 `HostileTo` 的可见人类派系，双方各自 `TryResolveRaidSpawnCenter` 在同一 map 落地，`RaidStrategyWorkerFactionFirst.MakeLords` 让二者互殴。∴ "落点/目标" = ①挑哪一对(f1,f2) ②各自 spawnCenter；FW 从不打"对方据点"，故 ① 的"边界邻接"是唯一能对齐 BOTR 处。
- **BOTR 权威**：`WorldComponent_Territories`（`Find.World.GetComponent<...>()`）。查询 public：`Faction OwnerAt(PlanetTile)`、`bool IsBorderTile(PlanetTile)`、`bool IsDisputedBorder(Faction,Faction)`、`FactionBorderContact BorderContactBetween(Faction,Faction)`、`bool AreFactionsAtWar(Faction,Faction)`、`IReadOnlyList<TerritorialWar> ActiveWars`、`TerritorialWar WarTouchingTile(PlanetTile)`、`PlanetTile WarFrontCenter(TerritorialWar)`。改写 public：`void RebuildTerritories()`。私有（反射）：`StartWar(Faction,Faction,int,BordersOfTheRimSettings,Settlement=null,bool=false,bool=false,int=-1)`、`TryCaptureSettlement`(内含 `settlement.SetFaction(winner)`+`occupations`+`EnsurePoliticalRoles`+`RebuildTerritories`)。**禁直写 `tileOwners`**，改归属只走 `SetFaction`+`RebuildTerritories`。

### B.1 ① 战争配对尊重政治边界（postfix `GetHostileFactionPair`）

`GetHostileFactionPair` 现随机 shuffle 后取"第一对 `HostileTo`"，跨半张图也可能配对 → 混战地点与地缘无关。postfix：若选中对 `(f1,f2)` **不是** BOTR 边界接触对（`BorderContactBetween==null && !IsDisputedBorder`），在 `candidateFactionList` 里改选一对**既 `HostileTo` 又边界邻接**的派系；找不到则保留 FW 原结果（不阻断事件）。与已有 `FactionalWarRaidFix` 的 prefix（剔动物派系）叠加无冲突（prefix 先缩小候选，postfix 再优选邻接）。用 BOTR public 读，零私有风险。

### B.2 ② 宣战→驱动 BOTR 边界战争（postfix `TryExecuteWorker`）

FW 成功开战（`__result==true`）读出 `_faction1/_faction2`（反射字段）→ 若 `!BOTR.AreFactionsAtWar(f1,f2)`，反射 best-effort 调 `WorldComponent_Territories.StartWar(f1,f2, points, settings)`，让 BOTR 领土引擎接管吞并/前线/易主（其 `TryCaptureSettlement`+`RebuildTerritories` 已实现战胜方改边界）。**逐层 try/catch，签名漂移即静默 no-op**（同 `BordersOfTheRim.HSKPatch` 容错范式），绝不让一枚反射失败崩掉整个 `HSKFixPack` 的 `PatchAll`。FW 自身"打不打得赢"不变，只是把结果喂给 BOTR 的边界态。

### B.3 ③ 我方被波及 → 可感知提示 + 任务钩子（同一 postfix）

若事件 map == `Find.AnyPlayerHomeMap`：用 BOTR `WarTouchingTile`/`OwnerAt` 判定混战落点是否踩进我方/我盟友边界，是则 `Find.LetterStack.ReceiveLetter`（原版 public）发"边境战事波及我领"小信（ThreatSmall），点明交战双方 + 最近 contested tile；并 `Messages.Message` 一条。**接悬赏公告牌/任务大修委托**（把该战事转成 T3/T4 定向委托）属 Layer C 依赖，本轮仅留设计接口（postfix 里预留 raise 一个 `WarSpilloverEvent` 静态回调，`edgepolitics` 建容器后订阅），不硬塞 SW `Warrant`。

## C. 实装落点与验证

- **落点**：`HSK修复整合/Source/FactionalWarBordersLink.cs`，`[StaticConstructorOnStartup]` + 整体 try/catch；未装 FW/BOTR 时 `AccessTools.TypeByName` 返 null 直接 return（0 副作用）。`build.ps1` 编译进 `HSKFixPack.dll`（C#5/系统 csc；BOTR/FW 类型纯反射，不新增程序集引用）。
- **加载**：`local.hskfixpack` 已压尾（loadAfter 全部非 EndMod 内容 mod），FW/BOTR 均在其前 → SCOS 反射时两程序集已加载。仍按规范核 `find_cycle.py`/`check_order2.py`。
- **静态验证**：csc 编译错误清零；`patch_simulator.py` 不适用（本联动纯 C# Harmony，无 XML 补丁）；反射目标签名以本次反编译为准。
- **同步**：`HSK修复整合` 的 `Source/` + `Assemblies/HSKFixPack.dll` 双目录原子同步 + hash 核对；游戏冷启动验证留用户排期（重点看：FW 事件是否只在邻接派系间触发、BOTR 日志有无 `StartWar` 反射 no-op 警告、混战信是否出现）。
- **风险**：`StartWar` 私有且参 8（第 3 参 `int` 语义未定，用 0 兜底 + 全可选参填默认）→ 若版本漂移 B.2 静默降级；不影响 B.1/B.3。
