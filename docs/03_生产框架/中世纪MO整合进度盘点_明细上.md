# 中世纪MO整合进度盘点 · 逐单元明细(板块 1-3)

> 主文件: `中世纪MO整合进度盘点.md`(结论 / 板块进度 / 已落地硬证据 / 问题处置 / 下一步)。数据与主文件同源,由 `_tmp/gen_mo_progress.py` 生成、`_tmp/split_mo_docs.py` 按 10KB 铁律拆分。
## 逐单元明细(板块 1-3)



### 1 材料与资源 — 单元 7,加权 0.0%(已整 0 / 部分 0 / 未整 2 / 不整 5)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| 金属链(铁矿石/铁锭/钢/秘银劫持) | ≈10 def | ⛔ 不整合/放弃 | ⛔ HSK 冶炼 + Vile’s Metallurgy/Materials Science 上位替代 | 与 HSK 金属体系抢主链 | — |
| 木材链(RawWood→WoodLog/暗木) | ≈6 def | ⛔ 不整合/放弃 | ⛔ Vile’s Wood You Please 上位替代 | 砍树产物劫持冲突 | — |
| 生皮/鞣革链(12 动物皮+顶级 4 档) | ≈20 def | ⛔ 不整合/放弃 | ⛔ Vile’s Hell Bent for Leather Tanning 上位替代 | Replace_Leather_Stats 全盘重做原版皮革 | — |
| 布料纤维链(亚麻/棉/羊毛/丝绸) | ≈8 def | ⛔ 不整合/放弃 | ⛔ HSK 纺织链上位替代 | Cloth 来源改写冲突 | — |
| 食材/农产品/蔬果链 | ≈35 def | ⛔ 不整合/放弃 | ⛔ HSK 农业 + Vile’s Sow Farm/Amuse Bouche 上位替代 | 覆盖原版 Brewing/Cooking | — |
| 怪物炼金素材(Schrat树心/巨骨/龙鳞血等) | ≈18 def | ⬜ 未整合 | 未落地;仅随 §7 怪物 + §9 炼金一起才有价值 | MO DLL 掉落生成 + 炼金链 | 高 |
| 宝石 stuff(黄水晶→红宝石 6 档) | 6 def | ⬜ 未整合 | 未落地;方案已并入 §16 矿井暴击池 | 随 §16 决策 | 低 |

### 2 生产设施与配方 — 单元 8,加权 6.2%(已整 0 / 部分 1 / 未整 7 / 不整 0)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| Processor 自动加工机(风车/水车/窑/坩埚/冶炼炉/晒皮架/鞣革架/干燥架/烟熏房/发酵桶/奶酪压机/造纸机/木炭堆/冰模/丝绸床/高炉/鞣革桶/纺纱机) | 19 座 | ⬜ 未整合 | 未落地;全部依赖 [SYR] Processor Framework | Processor Framework + MO DLL(Comp_WindMill/WaterWheel/IceBoxFill/ThingProducer) | 高 |
| 窑 DankPyon_Kiln(黏土→砖) | 1 座 | 🟡 部分 | 功能已由 HSK 玻璃制造台/三级陶瓷产线承担;MO 审美件未移植 | 纯建筑移植可低成本补 | 低 |
| 手动工作台(铁砧/工作台/炼金台/磨盘/桌锯/水井/清洁台/珠宝台/烤炉系/熔炉/棚架) | ≈15 座 | ⬜ 未整合 | 未落地;配方注入依赖 MO 铁砧体系 | 须改挂 HSK 锻造台/工作台 | 中 |
| 修补台 MendingBench(修补衣物/武器/护甲) | 1 座 | ⬜ 未整合 | 未落地;WorkGiver_DoMending 在 MO DLL | 需自研 WorkGiver + JobDriver | 中 |
| 泔水锅 SlopPot 三兄弟(自动喂食) | 3 座 | ⬜ 未整合 | 未落地;CompSlop + Building_SlopPot 在 MO DLL | 需自研 comp | 中 |
| 养蜂场 Apiary(自动产蜜/蜡) | 1 座 | ⬜ 未整合 | 未落地;Comp_ThingProducer 在 MO DLL | 与 Vile’s Sow Farm 系重叠 | 中 |
| 信鸽站/信使台/探险家工作台(通讯外交) | 3 座 | ⬜ 未整合 | 未落地;CompQuestFinder + GameComponent_QuestFinder 整套 DLL | 需自研任务扫描体系 | 高 |
| 挖掘点/采矿点/矿井 | 4 座 | ⬜ 未整合 | 见 §16(方案已定、实现未落地) | MO DLL 暴击/虫灾 comp | 中 |

### 3 建筑与家具 — 单元 24,加权 22.9%(已整 5 / 部分 1 / 未整 15 / 不整 3)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| 单格门与门框(乡村门/原木栅门/大门/石板门/木门框) | 5 def | ✅ 已整合 | ✅ Rustic_RusticDoor / LogGate / Gate / SlabDoor / Frame_Log(内联中文+自带贴图) | — | — |
| 双格大型门(乡村门1×2/大门1×2/石板门1×2/加固原木大门) | 4 def | ✅ 已整合 | ✅ Rustic_RusticDoor1x2c / Gate1x2c / SlabDoor1x2c / ReinfocedLogGate(挂 ComplexFurniture 门禁) | — | — |
| 墙体(原木墙/帐篷墙/木栅栏/射孔栅栏/都铎墙/城堡墙/射孔城堡墙) | 7 def | ✅ 已整合 | ✅ Rustic_LogWall / TentWall / Palisade / EmbPalisade / TudorWall / CastleWall / CastleWallEmbrasures;城堡墙「炸药4×·钝击2×」弱点用原版 damageMultipliers 保留 | 都铎墙/城堡墙挂 Stonecutting,余无门槛 | — |
| 柱(原木柱/皇家金柱/隔断柱) | 3 def | ✅ 已整合 | ✅ Rustic_LogColumn / RoyalColumn / DividerColumn | 无研究门槛(与 MO 原档一致) | — |
| 防御工事-尖桩战壕/加固战壕 | 2 def | ⬜ 未整合 | 未落地(原移植的拒马桩与原版尖刺陷阱 TrapSpike 重复, 08-27 22:40 已删除) | 若做需另建 Security 子分类 | 低 |
| 桥/梯子/楼梯 | 0 | ⛔ 不整合/放弃 | ⛔ MO 自身已注释停用,非缺失 | — | — |
| 储物(乡村/金属/皇家箱、衣橱、资源专用储具、货架、大货架、武器架、橱柜、马车、冰窖) | ≈45 def | ⬜ 未整合 | 未落地;MO 用原版 ShelfBase + maxItemsInCell=4 | 与 sbz NeatStorageFridge / Reel存储 定位重叠,需先定主方案 | 中 |
| 温度与火源(冰柜/柴堆/圆柴堆/柴炉/壁炉) | ≈6 def | ⬜ 未整合 | 未落地;HSK 用电冰箱/空调/发电机供暖 | MO 冰链需 CompMeltable/CompIceBoxFill(DLL) | 中 |
| 无电照明(火把/壁挂火把/油灯/路灯/蜡烛/烛台/火盆) | ≈10 def | ⬜ 未整合 | 未落地;HSK/Vile 有电力与蜡烛替代 | 蜡烛需 CandleMaking 研究 + 牛油/蜂蜡材料 | 低 |
| 生产辅助链接件(锻造5/炼金3/抄写4/烹饪2) | 14 def | ⬜ 未整合 | 未落地;须先有 §2 设施才有意义 | RoomRoleWorker/链接加成逻辑部分在 MO DLL | 中 |
| 娱乐-棋牌(骰子杯/塔罗桌/战棋桌) | 3 def + 6 辅助 | ✅ 已整合 | ✅ Rustic_CupAndDice / Tarocco / RimWar + 同名 JoyGiverDef×3 + JobDef×3(原版 Gaming_Cerebral,零 DLL) | — | — |
| 娱乐-桌(餐桌 1×1~2×4/圆桌/吧台/原木桌/铁加固变体) | ≈12 def | ⬜ 未整合 | 未落地 | 纯原版 Furniture,易移植 | 低 |
| 娱乐-椅凳(圆凳/餐椅/贵族椅/原木凳系列) | ≈8 def | ⬜ 未整合 | 未落地 | 纯原版 Furniture,易移植 | 低 |
| 床与床边设施(原木床→皇家都铎床 8 档 + 床头柜/梳妆台) | ≈11 def | ⬜ 未整合 | 未落地;毛皮床治低温症需 Comp_BedCureHediff(DLL) | 基础床可移植,治愈 comp 需自研 | 低—中 |
| 装饰/艺术(雕塑/展示台座/铠甲像/喷泉/横幅/花瓶/招牌 12 种/壁挂旗) | ≈30 def | ⬜ 未整合 | 未落地;纯审美零依赖 | — | 低 |
| 战利品装饰(银金圣杯/碗/宝石骷髅/古籍) | ≈8 def | ⬜ 未整合 | 未落地;绑 §7.2 清洗台玩法 | — | 低 |
| 书籍家具(讲台/小书架/大书架/空书架/皇家书架) | ≈5 def | 🟡 部分 | 🟡 蓝图书研读走原版书架(已可用);MO 专用书架/讲台未移植 | 研究加成 stat 与 HSK 研究台体系重复计数需核 | 低 |
| 帐篷(市场帐篷 7 色) | 7 def | ⬜ 未整合 | 未落地(帐篷墙 ✅ 已随墙体移植) | — | 低 |
| 皇家家具(衣橱/书架/扶手椅/王座/桌/床边桌/梳妆台/丝毯) | ≈10 def | ⬜ 未整合 | 未落地;不依赖 Royalty DLC,需并入科技 + 二级菜单 | 镶金饰丝造价须按 HSK 材料映射 | 低—中 |
| 废墟/遗迹/堵塞门/废矿井/地窖 | ≈15 def | ⬜ 未整合 | 未落地;静态废墟部分零依赖 | 可搜刮变体依赖 Building_Lootable(DLL) | 中—高 |
| 可搜刮战利品 Building_Lootable(骸骨堆/圣物柜/名甲像/战利品架/箱) | ≈8 def | ⛔ 不整合/放弃 | ⛔ 强依赖 MO DLL(Building_Lootable + LootableExtension + 开箱出老鼠) | 须自研等价 comp 才能做 | 高 |
| 藏身处世界站点体系(储物 Lootable 变体 + 三级解锁) | ≈20 def | ⛔ 不整合/放弃 | ⛔ 依赖 MO 派系 + DLL 扫描链 | 见商人任务专项 P2/P3 | 高 |
| 攻城武器(蝎弩炮/床弩/投石机/连发弩炮) | 4 座 + 4 弹药 | ⬜ 未整合 | 未落地;JobDriver_ManTurret 补丁 + 消耗弹药 | CE 需大改(弹药化/爆炸适配) | 高 |
| 地形地板(夯土/乡村木地板/铁地砖/石砖 6 纹×6 材质/陶瓷砖 4 纹/犁过的土) | ≈60 terrain | ⬜ 未整合 | 未落地;HSK Floors_SK 已有大量地板档 | 特色陶瓷砖(5000 工时/Beauty+4)可作天花板补录 | 低 |

