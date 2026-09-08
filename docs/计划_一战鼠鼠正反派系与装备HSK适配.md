# 计划 · 一战鼠鼠(Ratkin GreatWar)正反派系 + 精选装备 HSK 适配

> packageId `browncofe.RCG`（工坊 3525422104）；武器参考 `bbb.ratkinweapon.morefailure`（工坊 2779404660）。
> 落位：并入宿主本地 mod `鼠族HSK拓展`（`local.ratkin.clothesweapons`），不新建 mod。
> 派系口径：**复用 GreatWar 现成两派系**——协约国 = 正方友军、同盟国 = 反方敌军。

---

## 0. 现状盘点（先别重做，2026-09-06 已核）

GreatWar 早在 **2026-08-21 已精简并入宿主**，以下已存在：

| 内容 | 位置 | 状态 |
|---|---|---|
| 协约国 `Rakinia_WhiteRoseKingdom`(EA/正方) | `Defs/FactionDef/Ratkin_Factions.xml` | 已并入 |
| 同盟国 `Rakinia_CentralAlliance`(CA/反方) | 同上 | 已并入 |
| 兵种 PawnKind | `Defs/PawnKindDef/PawnKinds_EA.xml` / `_CA.xml` | 已并入 |
| 派系命名 RulePack | `Defs/RulePackDef/…Namers_Factions.xml` | 已并入 |
| 派系汉化 | `Languages/…/DefInjected/FactionDef/Factions_Misc.xml` | 已并入 |
| 枪械 18 件 | `Defs/ThingDef/RKGW/Weapon_{EU,KB,MR,WR,YE}.xml`+`Weapon_GW2名枪.xml` | 原始 `Verb_Shoot`，CE 靠 `Patches/04`、`81` 转换 |
| 近战 3 件 | `RKGW/Melee.xml` | 已 ToolCE 适配 |
| 军服/头盔 37 件 | `RKGW/Apparel_*.xml`(16 国) | 源文件 CE:0，靠 `Patches/04` 补 Bulk/WornBulk |

移植率：枪 18/122、衣 37/173 —— 大量未移植件即"再加一点"的来源池。

## 1. 真实缺口（本计划的实际工作量）

- **G1 文化悬空引用**：两派系 `allowedCultures > RK_Culture_Virtuard`，但全 Defs 无该 `CultureDef` 定义 → Ideology 下悬空/回退。
- **G2 正↔反无敌对**：两派系均无 `naturalEnemy`、无 goodwill/开战 hook，Patches 里也无引用 → 目前中立 0，不会互相开战，"正方打反方"不成立。
- **G3 衣物 CE 覆盖待核**：`04_CE适配.xml` 是否覆盖全部 37 件 RKGW 军服的 `Bulk/WornBulk` + `ArmorRating_Sharp/Blunt`(mm) 需逐件核对（About 只承诺枪械 CE）。
- **G4 新增件适配管线**：精选新军服/枪/盔走完整 HSK 适配（下 §3）。

---

## 2. 阶段 P0 · 核对现有整合完整性（改前先验）

1. 读 `docs/00_HSK参考文档合集导航.md` + 详录 §6/§7；用 `Mods/Unified.xml`（UTF-16）核两派系最终态 pawnGroupMakers 引用的每个 pawnkind 都存在、`apparelTags/weaponTags` 都能落到已定义衣物/武器（防裸装/空装，参考兵种 `apparelMoney` 默认 0 会裸体坑）。
2. 跑 `_tmp` 材料门控 / techlevel 脚本确认 RKGW 枪研究档 `RK_KB_Weapon` 等节点存在且不越级。
3. 产出《缺口确认清单》进 `_tmp/`，G1–G3 逐条定性。

## 3. 阶段 P1 · 正反派系接线

- **补文化 G1**：在 `Defs/CultureDefs/` 新增 `RK_Culture_Virtuard`（仿现有鼠族文化基类，服饰白名单指向 RKGW 军服集）；或改派系 `allowedCultures` 指向已存在的文化。**先查 `docs/00_mod加载顺序与依赖.md`**，避免与既有鼠族文化冲突。
- **敌对 G2**：正↔反互战用**限定层补丁**（不直接改派系原始 def）：新增 `Patches/9x_GW正反派系关系.xml`，给两派系补对对方的敌意/初始 negative goodwill。异种人池走**族色委托**（参考美狐 98 号做法），不新增 XenotypeDef。
- **穿戴锁**：确认军服入 `raceRestriction.apparelList` 的独占语义（AGENTS §3）——Ratkin 军服只鼠族可穿是预期；若要让友军 NPC 也穿须逐族列。

## 4. 阶段 P2 · 精选新增装备（"再加一点"）

> 定档：**每旗舰阵营 1 套 + 通用少量**，共约 **枪 4 / 近战 2 / 衣盔甲 8**。先列清单待你过目再动手。

候选（取自 GreatWar 未移植件，贴图宿主已拷）：
- 正方·白蔷薇(WR)：反器材栓狙 / 重机枪、防毒面具、战壕护甲
- 反方·矢车菊(KB)：冲锋枪、战壕棍棒/刺刀、皮风衣
- 通用：军官佩剑(骑兵)、钢盔一枚、大衣一件

每件适配管线（缺一不可，AGENTS §6/§7/§10）：
1. 枪械：`verbClass→CombatExtended.Verb_ShootCE` + `<AmmoUser>` ammoSet 映射现成弹种（步枪 303British / 手枪 SMG 9x19mmPara / 霰弹 12Gauge / 反器材 50BMG，**禁自创弹**，对照 `docs/06_弹药/HSK弹药清单.csv`）+ `FireModes` + `statBases.Bulk`。
2. 近战：`tools` 全 `Class="CombatExtended.ToolCE"` + `armorPenetrationSharp/Blunt`(mm) + 单/双手 tag。
3. 衣甲：`Bulk/WornBulk` + `ArmorRating_Sharp/Blunt` Replace 成 CE mm 值。
4. 研究/配方：并入 HSK 科技树对应档（衣物→鼠族衣物档/锻造/制枪），研究台六档铁律（§6），`recipeUsers` 单路径防重复解锁。
5. 汉化：DefInjected 走宿主 `Languages/…/DefInjected/`，根节点 `<LanguageData>`、defName 作节点名。
6. 改补丁后 `_tmp/patch_simulator.py`(lxml) 模拟验证。

## 5. 阶段 P3 · 收尾交付

- 逐文件同步双目录（工作区 ↔ `RimWorld\Mods\鼠族HSK拓展`），复制后核 diff、防嵌套目录。**不启动游戏扫日志**（AGENTS §1 交付铁律），静态核对用 Unified/终态 def。
- 更新宿主 `About.xml` 描述段追加本次内容（描述内禁未转义 `<tag>`）。
- 若新增件引用 Weapons+ 的刺刀 comp（`MayRequire="bbb.ratkinweapon.morefailure"`），确认门控在无该 mod 时静默跳过。

## 6. 边界与依赖

- 已订阅创意工坊原版 `Ratkin GreatWar` 者须取消订阅（defName 冲突）。
- 遵循单文件 ≤10KB；本计划为方案文档，**不登记进交付导航**，成果自验通过后另立记录。

---

### 待你拍板
1. §4 精选清单是否照此规模与选件，还是你指定具体国/具体件？
2. 敌对 G2 走"限定层补丁"还是直接接受两派系中立、只靠世界生成自然冲突？
3. 新增护甲是否也要 CE mm 值强化到 HSK 同档水平（会影响正方友军强度）。

---

## 7. 执行结果（2026-09-06，已双目录同步 + 静态校验）

- **G1 更正为误报**：`RK_Culture_Virtuard` 实定义于 `RatkinRaceHSK\1.6\Defs\CultureDefs\Cultures.xml`，宿主 loadAfter 该 mod，跨 mod 引用有效，无需补。
- **G2**：`Defs/FactionDef/Ratkin_Factions.xml` 给同盟国(反方)加 `<naturalEnemy>true</naturalEnemy>`（同军阀做法，故反方对所有派系敌对）。
- **G3**：新建 `Patches/96_GW军服CE适配.xml`，37 件军服补 Bulk/WornBulk + 原版 % 护甲→CE mm（simulator 95 操/0 失）。
- **P2 新增 14 件**：枪 4(`RK_WR_WhiteRose/RK_WR_TalinumRevolver/RK_KB_Buche/RK_KB_Maipflanze`，子弹复用 Bullet_Fichte + 新增 2)+近战 2(`RK_Truncheon/RK_Grabenkeule`)入 RKGW；衣甲 8(WR/KB 各 4)入 Apparel 文件；`Patches/98_GW新增装备CE适配.xml` 做 4 枪 `MakeGunCECompatible`(303British·9mmPara·7.92Mauser)+8 衣 CE。`Patches/80` 追加 raceRestriction apparelList(8)+weaponList(6)。汉化 `RKGW_武器/服饰.xml` 追加。贴图 70 张入宿主。
- **兵种取用**：新件 tag(RK_WR_Rifle/Pistol·RK_KB_LMG/SniperRifle·RK_RCG_Trench_WR/KB·RK_*_Officer/Soldier)已被现有 pawnkind 引用，无需改兵种。
- 说明：与既有 GreatWar 兄弟件一致，这些为**阵营 NPC 装备**(非玩家可造，沿用同套 RK_*_Weapon 研究 token)。效果待进游戏实测。
