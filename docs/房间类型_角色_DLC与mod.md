# 房间角色 · DLC 与第三方 mod（16 条）

DLC 条目在对应资料片未启用时会直接返回 0 或 -1（已在各自条目中标明）。

## Biotech

### 教室 `Classroom`

**来源** Biotech ｜ **得分** 8 / 个 ｜ **判定类** `RoomRoleWorker_Classroom`

- **需启用 Biotech DLC**，否则恒 0
- 统计 `SchoolDesk`（课桌）与 `Blackboard`（黑板）
- 房内出现**任何床** → 判否

### 育儿室 `Nursery`

**来源** Biotech ｜ **得分** 100200 / 床 ｜ **判定类** `RoomRoleWorker_Nursery`

- **需启用 Biotech DLC**
- 床必须：人形、非医疗、非囚犯，且 `bed_maxBodySize < HumanlikeChild.bodySizeFactor`（只能容纳婴幼儿）
- 出现囚犯床或尺寸足够容纳儿童的床 → 直接判否
- 符合条件的床 **&ge; 2** 才成立

> 100200/床，高于宿舍(100100)，避免被误判。

### 游乐室 `Playroom`

**来源** Biotech ｜ **得分** 8 / 个 ｜ **判定类** `RoomRoleWorker_Playroom`

- **需启用 Biotech DLC**
- 统计 `BabyPlayGainFactor > 1` 的建筑（玩具类）
- 房内出现**任何人形床** → 判否

### 死眠室 `DeathrestChamber`

**来源** Biotech ｜ **得分** 100 倍 ｜ **判定类** `RoomRoleWorker_DeathrestChamber`

- `DeathrestCasket`（死眠棺）每个 +1
- 带 `CompDeathrestBindable` 的物件每个 +0.5
- 最终得分 = 累加值 × 100

> 一台死眠棺 = 100 分。

## Royalty

### 谒见厅 `ThroneRoom`

**来源** Royalty ｜ **得分** 10000 ｜ **判定类** `RoomRoleWorker_ThroneRoom`

- 房内存在 `Building_Throne`（王座）
- 且 `Validate(room) == null`，即房间 `!OutdoorsForWork`（必须封闭室内）

## Ideology

### 圣殿 `WorshipRoom`

**来源** Ideology ｜ **得分** max(2000, 75×个) ｜ **判定类** `RoomRoleWorker_WorshipRoom`

- **未启用 Ideology 时返回 -1**
- 统计 `isAltar` 且其 `compStyleable.SourcePrecept.ideo.StructureMeme != null` 的祭坛
- 至少 1 个祭坛时得分 = `Max(2000, 数量 × 75)`

> 标签会被**主导意识形态的 WorshipRoomLabel** 覆盖（PostProcessedLabel），同一房间对不同教派显示不同名称。

## Anomaly

### 仪式厅 `CeremonialChamber`

**来源** Anomaly ｜ **得分** 200 / 个 ｜ **判定类** `RoomRoleWorker_CeremonialChamber`

- 统计 `ThingDefOf.PsychicRitualSpot`（心灵仪式点）

### 收容室 `ContainmentCell`

**来源** Anomaly ｜ **得分** 100 / 50 ｜ **判定类** `RoomRoleWorker_ContainmentCell`

- 属于 `ThingRequestGroup.EntityHolder` 的物件（收容平台等）每个 **+100**
- `Electroharvester` / `ElectricInhibitor` / `BioferriteHarvester` 每个 **+50**

## 第三方 mod

### 冷藏室 `CooledStoreroom`

**来源** Common Sense ｜ **得分** 1 / 储物 + 1 / 冷机 ｜ **判定类** `CommonSense.RoomRoleWorker_CooledStoreRoom`

- 继承原版仓库逻辑：每个 `Building_Storage` +1；**基础为 0 时直接返回 0**
- 额外统计"冰箱式冷机"：是 `Building_Cooler`、目标温度 **< 0°C**、其**正面出风格**（Position + South 按朝向旋转）可通行且**该格属于本房间** → 每台 +1

> 判定的是"冷机正面吹进本房间"而非冷机所在位置，冷机装在墙里朝内吹也算。

### 客房 `GuestRoom`

**来源** Hospitality ｜ **得分** 110000 / 床 ｜ **判定类** `Hospitality.RoomRoleWorker_GuestRoom`

- 至少 1 张 `Building_GuestBed`（客人床）
- 出现任何**囚犯人形床** → 判否

> 110000 高于宿舍/卧室，确保客房标签优先。

### 私人浴室 `PrivateBathroom`

**来源** Dubs Bad Hygiene ｜ **得分** 4000 ｜ **判定类** `DubsBadHygiene.RoomRoleWorker_PrivateBathroom`

- 存在至少 1 个 `Building_AssignableFixture`（卫生洁具）且**已被分配小人**（AssignedPawns 非空）
- 且 `room.isPrisonCell == false`

> 关键在"已分配主人"——没分配的马桶不会触发私人浴室。

### 公共浴室 `PublicBathroom`

**来源** Dubs Bad Hygiene ｜ **得分** 30 / 浴缸 + 1 / 马桶 ｜ **判定类** `DubsBadHygiene.RoomRoleWorker_PublicBathroom`

- 统计 `Building_AssignableFixture` 中 `fixture == Toilet`（马桶）与 `fixture == Bath`（浴缸）的数量
- 触发门槛：**浴缸 > 0 或 马桶 > 1**（单个马桶不算公共）
- 得分 = 浴缸数 × 30 + 马桶数 × 1

> 单个马桶不足以成立，需 2 个以上马桶或至少 1 个浴缸。

### 桑拿室 `SaunaRoom`

**来源** Dubs Bad Hygiene ｜ **得分** 9999 ｜ **判定类** `DubsBadHygiene.RoomRoleWorker_Sauna`

- 房间 **不使用室外温度** 且 **不触及地图边缘**
- 统计各 Region 内属于 `DefExtensions.SaunaDefs` 的物件数量 `num`
- `num > 0` 且 `房间格数 / num < 50`（密度门槛）

> 9999 分，仅低于机械体机库、各床铺类与王座。房间太大则桑拿设备密度不足。

### 温室 `SolarWeb-Stratum-Greenhouse`

**来源** Core_SK Stratum ｜ **得分** 20 + 2×地块 + 1.5×天窗格 ｜ **判定类** `SolarWeb.Stratum.RoomRoles.Greenhouse`

- 房间 **非心理户外** 且 `CellCount > 0`
- `num` = 屋顶判定为**天窗**（RoofStatCache.IsSkylight）的格数
- `num2` = 属于 `Zone_Growing`（种植区）的格数
- `num3` = `Building_PlantGrower` 占用格中归属本房间的格数
- 必须 `num > 0` 且 `(num2 > 0 或 num3 > 0)`，即"有天窗"且"有种植"

> HSK Stratum 专属。关联显示：美观/清洁/财富/空间/观感/天窗覆盖。

### 图书馆房间 `VBE_Library`

**来源** 书籍拓展HSK ｜ **得分** 13.5 / 册位 ｜ **判定类** `VanillaBooksExpanded.RoomRoleWorker_Library`

- 房内存在 `workTableRoomRole == Laboratory` 的工作台 → 直接判否（避免与研究室冲突）
- 放在 `Building_Storage` 上的 `Book` 每本 +1
- 每个 `Building_Bookcase` +1，并额外 +其 `HeldBooks.Count`

> 来自 VanillaBooksExpanded，本地化为书籍拓展HSK。

### 机械体机库 `WVC_MechHangar`

**来源** WVC Work Modes ｜ **得分** 900000 ｜ **判定类** `WVC_WorkModes.RoomRoleWorker_Shutdown`

- 房内任一物件带 `CompProperties_ShutdownRoom`，或本身就是 `Building_MechCharger`（机械体充电器）

> 900000 分为全表**最高**，一旦存在即无条件夺取房间类型。
