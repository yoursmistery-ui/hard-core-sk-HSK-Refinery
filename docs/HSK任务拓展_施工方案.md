# HSK任务拓展 — 施工方案（待确认）

> 决策（你已定）：① 拆两个 mod（`剧本与派系大修` + `HSK任务拓展`）；② RimQuest + Simple Warrants **合并进** `HSK任务拓展`；③ 铺**全 77 派系**；④ 工业大修**只搬任务 XML**。
> 数据源：两工坊 mod 本地定位（2263331727=RimQuest `Mlie.RimQuest`、2676828755=Simple Warrants `pb3n.SimpleWarrants`）+ `docs/09_任务体系`。

## 一、⚠ 动手前必须先拍的技术风险（合并 vs 依赖）

RimQuest 是**框架 mod**，且 `HSK工业科研大修` 的 About 已 `loadAfter Mlie.RimQuest`。若按"合并进新 mod"把它连同 DLL 折进 `HSK任务拓展`：

- 新 mod 用**新 packageId** → 工业大修等所有 `loadAfter Mlie.RimQuest` 变**悬空边**（排序失效，需逐个改指新 id）。
- RimQuest / Simple Warrants 是**活跃更新**的工作坊 mod（emipa606 系列），DLL 并入本地后**丢上游自动更新**，每次要手动重放——与你"跟踪上游"的原则冲突。

两条路：
- **(A) 保留为依赖（我更推荐）**：`HSK任务拓展` 只声明 `loadAfter Mlie.RimQuest, pb3n.SimpleWarrants`，本身**只写内容**（按族委托 + 通缉配置 + 补译）。零悬空、上游自动更新。mod 数 +1（不是 +3）。
- **(B) 真合并（你选的）**：把两 mod 的 1.6 DLL+Defs 整体并入，减 mod 数但承担上面两点，且需同步改工业大修 About。

> 下面骨架/搬运/补译/逐族设计 **A、B 通用**，仅"是否复制 DLL+Defs"不同。**请最终确认走 A 还是 B** 再动手。

## 二、新 mod 骨架

- **packageId**：建议 `local.hsk.questexpansion`（对齐 `local.hskfixpack` 风格；待定名）。
- **supportedVersions**：仅 1.6。**封面**：你给的图标题是「剧本与派系大修」→ 属**派系+剧本**那个 mod；`HSK任务拓展` 需另配封面（待你出图或我按同风格生成）。
- **加载位**：内容 mod 段、`local.hskfixpack` 之前（与 `剧本与派系大修` 同层，二者互不依赖、可任意序）。
- **依赖**（AGENTS §3.1）：modDependencies ≥ Harmony + Core SK + Solaris.RatkinRaceMod + ModIndicator；loadAfter 追加 `Mlie.RimQuest`、`pb3n.SimpleWarrants`、`Albion.GoExplore`、`qwerty19106.researchtreesk`、`DimonSever000.Events.Specific` + 被 patch 的各族 mod。
- **HSK 标记三步**：进 Mods（+根 `touch HSK`）→ `Patches/ModAssistant标记.xml`（NativeAddon 注入，MayRequire 门控）→ About 补依赖。

## 三、合并清单（走 B 时复制；走 A 则跳过、仅 loadAfter）

| 来源 | 内容 | 处理 |
|---|---|---|
| RimQuest 1.6 | `RimQuest.dll`+`VersionFromManifest.dll`；`Defs/QuestDefs/QuestGiverDefs.xml`、`Defs/JobDefs/RQ_Jobs.xml`；`Patches/Patches.xml`；`Languages/ChineseSimplified`(有中文) | DLL 名不冲突直接并入；Patches 重命名 `01_RimQuest.xml`；LoadFolders 多版本→本地只留 `<v1.6>`（AGENTS 坑） |
| Simple Warrants 1.6 | `SimpleWarrants.dll`；`Defs/{IncidentDefs,MainButtonDefs,QuestScriptDefs(4类通缉),RulePackDefs/WantedReasons,Sites(3)}`；`Patches/Patches.xml`；Languages **仅英文** | DLL 并入；Patches 重命名 `02_SW_Warrants.xml`；**需补译中文**（通缉 Keyed + DefInjected） |
| 合并冲突 | 两 mod defName 交集、与现有 77 派系/任务 def 撞名 | 跑脚本核（见 §六） |

## 四、工业大修搬运（只 XML）

- 搬 `HSK工业科研大修/1.6/Defs/Sites_RK_Ruins.xml`（废墟探索据点）→ 新 mod `Defs/`，随附汉化键。
- **不搬** `QuestFinderHSK.cs`/`BlueprintUnlockHSK.dll`（学者商队真逻辑，留工业大修）。
- 核对工业大修 About/补丁对被搬 XML 的引用，避免悬空。

## 五、全 77 派系任务拓展（分阶段，A/B 通用）

- **RimQuest** = 玩家自建任务板（sandbox），本身不需逐派系 XML；但要给各族母国配 `QuestGiver`（可发委托）+ 预置几条族色委托。
- **Simple Warrants** = 通缉玩法，逐派系配 `WantedReasons`/可通缉目标（ pawn/动物/文物）。
- **自研族色委托**：按族线母国各 2–4 条 `QuestScriptDef`，复用各族既有货币/奖励（金鸢尾=支援点·逆重点、鼠族=银币·好感·咪咪片、美狐=灵能、原版=银·空投…）。
- **分批**：批1 = 玩家可结盟主族 + 各母国（鼠族/金鸢尾/美狐/诺曼/猫人/军团/行会/安卓/阿丽莎/污鼠/雪鼠）；批2 = 其余敌对 NPC 派系（海盗/部落/帝国等）通缉与委托。77 派系逐条矩阵在落地时生成附表。

## 六、验证与同步

- 合并/搬运后跑 defName 交集脚本（新 mod vs Unified）防撞名；改补丁跑 `patch_simulator.py`。
- About 用 `ET.parse` 验；加载序跑合并口径建图两遍核 0 环（尤其 B 方案改工业大修 loadAfter）。
- Simple Warrants 补译按 translation-patch 路由（附属包/本体 Languages）。
- 双目录逐文件同步 + SHA1 + HSK 标记；上游更新登记（B 方案尤其）。

## 七、落地顺序

1. 先定 §一 A/B + packageId + 任务拓展封面 → 2. 建骨架（About/LoadFolders/HSK 标记/封面）→ 3.（B）复制两 mod 1.6 内容 或（A）仅 loadAfter → 4. 搬工业大修 `Sites_RK_Ruins.xml` → 5. Simple Warrants 补译 → 6. 批1 族色委托+QuestGiver+warrant 配置 → 7. 验证同步 → 8. 批2。

## 八、待你确认（3 点）

1. **§一 A 还是 B**（框架依赖 vs 真合并）——我强烈建议 A，B 会破坏工业大修依赖边并丢上游更新。
2. **packageId** 用 `local.hsk.questexpansion`？
3. **`HSK任务拓展` 的封面**要不要我按你这张「剧本与派系大修」的水墨卷轴风格另出一张（任务主题：卷轴上画任务板/通缉令/各族商队）？
