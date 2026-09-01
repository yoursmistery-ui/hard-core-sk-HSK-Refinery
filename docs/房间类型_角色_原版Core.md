# 房间角色 · 原版 Core（15 条）

判定条件取自 `Assembly-CSharp` 反编译结果。得分为 `GetScore()` 返回值，房间最终类型取全场最高分。

### 未分类 `None`

**来源** 原版 Core ｜ **得分** -1 ｜ **判定类** `RoomRoleWorker_None`

- 恒定返回 **-1**，作为兜底值
- 房间不满足 `ProperRoom`（触及地图边缘 / 无 Normal 区域）或 `RegionCount > 60` 时，**强制判定为 None**

> 不是"没算出分数"，而是引擎在 UpdateRoomStatsAndRole 里直接短路赋值。

### 房间 `Room`

**来源** 原版 Core ｜ **得分** 0.99 ｜ **判定类** `RoomRoleWorker_Room`

- 恒定返回 **0.99**

> 所有专用类型都不匹配时的默认标签。任何得分 > 0.99 的类型都会盖过它。

### 卧室 `Bedroom`

**来源** 原版 Core ｜ **得分** 100000 ｜ **判定类** `RoomRoleWorker_Bedroom`

- 存在 `bed_humanlike` + `bed_countsForBedroomOrBarracks` 的床
- 任一床为 **医疗床** 或 **囚犯床** → 直接判否
- 通过 `IsBedroom()`：①恰好 1 张空床且 0 张有主床 → 是；②0 张空床且恰好 1 张有主床 → 是；③有空床且 >1 张床 → 否
- 若无成人主人 → 是；若某床主人与其它主人**不在同一恋爱簇**（非配偶/恋人）→ 否
- 若有儿童主人，其**父母必须都在成人主人列表**中，否则 → 否

> 同分 100000 与医务室、医疗牢房并列，靠 DefDatabase 顺序决胜。

### 多人宿舍 `Barracks`

**来源** 原版 Core ｜ **得分** 100100 / 床 ｜ **判定类** `RoomRoleWorker_Barracks`

- 统计人形床且非囚犯床（**医疗床不计入计数**）
- 存在囚犯人形床 → 直接判否
- 通过同一套 `IsBedroom()` 判定，但结果必须**为假**才成立（即卧室不成立才轮到宿舍）

> 每床 100100 分，是最高档的床铺类角色之一。

### 牢房 `PrisonCell`

**来源** 原版 Core ｜ **得分** 170000 / 100000 ｜ **判定类** `RoomRoleWorker_PrisonCell`

- 所有床都必须是**囚犯人形床**，出现任何非囚犯人形床 → 判否
- 恰好 1 张**非医疗**囚犯床 → **170000**（全游戏最高分之一）
- 恰好 1 张**医疗**囚犯床 → 100000

> 2 张及以上会转交给多人牢房。

### 多人牢房 `PrisonBarracks`

**来源** 原版 Core ｜ **得分** 100100 / 50001 ｜ **判定类** `RoomRoleWorker_PrisonBarracks`

- 所有床都必须是囚犯人形床，否则判否
- 床总数（含医疗）必须 **> 1**
- 得分 = 非医疗囚犯床 × 100100 + 医疗囚犯床 × 50001

> 医疗囚犯床权重约为非医疗的一半。

### 医务室 `Hospital`

**来源** 原版 Core ｜ **得分** 100000 ｜ **判定类** `RoomRoleWorker_Hospital`

- 至少存在 1 张**标记为医疗**的人形床
- 出现任何**囚犯人形床** → 判否

> 与卧室、医疗牢房同分 100000，并列时按 DefDatabase 加载顺序取先者。

### 餐厅 `DiningRoom`

**来源** 原版 Core ｜ **得分** 12 / 个 ｜ **判定类** `RoomRoleWorker_DiningRoom`

- 统计 `category=Building` 且 `surfaceType=Eat` 的物件（餐桌）

> 只要 1 张餐桌（12 分）即可压过默认"房间"(0.99)。

### 娱乐室 `RecRoom`

**来源** 原版 Core ｜ **得分** 7 / 个 ｜ **判定类** `RoomRoleWorker_RecRoom`

- 遍历全部 `JoyGiverDef`，命中 `countsForRecRoom=true` 且其 `thingDefs` 包含该建筑 → 计数 +1
- 每个建筑只计一次（命中任一 JoyGiver 即 break）

> 收录范围由各娱乐 JobGiver 声明决定，mod 新增娱乐建筑会自动生效。

### 研究室 `Laboratory`

**来源** 原版 Core ｜ **得分** 60 / 个 ｜ **判定类** `RoomRoleWorker_Laboratory`

- 统计 `building.workTableRoomRole == Laboratory` 的工作台（研究台）

> 单台 60 分，是工作间(27)的两倍多。

### 工作间 `Workshop`

**来源** 原版 Core ｜ **得分** 27 / 个 ｜ **判定类** `RoomRoleWorker_Workshop`

- 统计 `building.workTableRoomRole == Workshop` 的工作台（各类制作台）

### 厨房 `Kitchen`

**来源** 原版 Core ｜ **得分** 28 / 个 ｜ **判定类** `RoomRoleWorker_Kitchen`

- 建筑 `designationCategory == Production`
- 且其任一配方的产物满足 `IsNutritionGivingIngestible && ingestible.HumanEdible`（人类可食）

> 电炉/柴灶等。注意判据是"产物能吃"，不是"有炉子"。

### 仓库 `Storeroom`

**来源** 原版 Core ｜ **得分** 1 / 个 ｜ **判定类** `RoomRoleWorker_StoreRoom`

- 统计 `Building_Storage` 实例（储物区、货架、储物建筑）

> 单件仅 1 分，极易被其它类型压过。

### 陵墓 `Tomb`

**来源** 原版 Core ｜ **得分** 50 / 个 ｜ **判定类** `RoomRoleWorker_Tomb`

- 统计 `Building_Sarcophagus`（石棺）

### 畜棚 `Barn`

**来源** 原版 Core ｜ **得分** 7.6 / 个 ｜ **判定类** `RoomRoleWorker_Barn`

- 统计 `Building_Bed` 且 `bed_humanlike == false` 的床（动物床/睡点）
