# 波次 6 · MO 31 个 KCSG 布局落地(2026-08-29)

> 路线 A(替代式移植)+ 用户拍板的四项替代。产物全部在 HSK工业科研大修 + 任务大修HSK, 已双目录同步。
> 生成器: `_tmp/gen_mo_layouts.py`(幂等, 可重跑); KCSG 语义源码存证: `_tmp/vef_*.cs`(jsdelivr 拉的 VEF master)。

## 一、用户拍板(2026-08-29)

| MO 布局符号 | 我们的替代 | 说明 |
|---|---|---|
| DankPyon_Grill(烤架) | `TableGrill`(Core_SK 简单烤架) | 用户指定用 HSK 烧烤架, 不用火盆; stuff=Steel 在场 |
| DankPyon_Anvil(铁砧) | 新建 `Rustic_Anvil` 摆件 | 挂 辅助建筑(Accessories)>乡村装饰 子分类; 金属 50, Smithing 研究, 非工作台 |
| DankPyon_BrigandThug 等全部 MO 兵种 | `KingdomBrigand`(Core_SK 王国强盗) | 环境 1.6 无 BanditThug; 守卫 spawnPartOfFaction=true → 自动加入站点派系 |
| DankPyon_BedFur(毛皮床) | `Rustic_BedFur` | 已移植, 前缀直映零损失 |

其余缺失按近似顶替(桶/箱/书/货架→Rustic_Loot* 可搜刮容器, 市场摊→乡村桌, 攻城塔→HSK Medieval_Ballista_Turret/Medieval_Catapult, 兽皮毯→熊皮毯, 素面石地→FloorStoneRough*, MO 地毯→FloorStone 马赛克/乱石板近色系), 纯杂物(推车/集市帐篷/风箱/磨盘等无对应物)留空'.'。怪物尸体(Corpse_DankPyon_*)→骨堆近景。

## 二、VEF-KCSG 机制定论(源码反查, 波次 7 写任务链必读)

1. **SymbolDef XML 字段是 `<thing>`, 不是 `<thingDef>`**(后者是 internal 解析字段, XML 写它=静默 no-op)。P1 的 19 个符号曾全写 thingDef → 废墟此前根本不生成, 本次已修复(18 处)。
2. **多布局=随机选一**: `GenStep_CustomStructureGen` 对 structureLayoutDefs 取 RandomElement, 每站点一座建筑。同一 def 内 layouts 的多层 `<li>` 是**叠加层**(主结构+散布层, 同矩形全部绘制), 不是随机。
3. **启动时只为官方包(Ludeon+DLC)自动生成符号**: `{Thing}`、`{Thing}_{Stuff}`(材质×官方 stuff)、`{PawnKind}`、`Corpse_{Kind}`。本地 mod 物必须显式定义 SymbolDef。带 `_North/_East/_South/_West` 后缀的 token 会去后缀再查一次并自动加旋转。
4. **pawn 符号**: `<pawnKindDef>` + `spawnPartOfFaction=true` → 加入 map.ParentFaction(站点所属派系); faction 留空=跟站点, 写 false+faction 空=野生动物。`defendSpawnPoint` 让守卫守点。
5. **布局字段**: layouts(符号格) / roofGrid('0'清顶 '1'薄顶 '2'岩薄顶 '3'岩厚顶) / terrainGrid(地板) 独立; `modRequirements` 含 MO 包 id, **照搬=永不生成, 必须丢弃**(我们已丢)。
6. 布局 token 查不到符号时静默空格(仅 verbose 日志)→ 转换器必须自校验 token 全覆盖。

## 三、产物清单(全部已同步部署)

**HSK工业科研大修**(新增 def ~707→738):
- `1.6/Defs/Symbols_MO_RK.xml` — 696 个 `RK_Sym_*` 符号(名=原 MO token 直译, 顶替在符号内部完成)
- `1.6/Defs/StructureLayouts_MO_DankPyon_Structure*.xml` ×8 — 31 个 `RK_MO_*` 布局(小藏身处×2 / 匪徒中废墟×2 / 邪教 1T×6·2T×5·3T×1 / 定居点×12 / 蛇窟 / 起源城堡 / 矮人堡垒)
- `1.6/Defs/Buildings_Rustic_Anvil.xml` — 锻铁砧摆件 + `Textures/.../Production/Anvil/` 四向贴图
- `1.6/Defs/SubCategories_Rustic_Decor.xml` — 登记 Rustic_Anvil

**任务大修HSK**:
- `1.6/Defs/Sites_RK_Ruins.xml` — 修 thingDef→thing(18 处); 两个土匪营地 GenStepDef 合并为单池 `RK_GenStep_Ruin_Camp`(P1 小/中废墟 + MO 小藏身处×2 + 匪徒废墟×2, order 460); ItemStash 圣物库保持

**站点挂接现状**: BanditCamp=6 布局随机池; ItemStash=圣物库。蛇窟/邪教/定居点/城堡/矮人堡垒 21 个布局已建好**未挂站点**——留给波次 7 任务链用(届时自建 QuestScriptDef/SitePartDef 或 linkWithSite 到自建站点)。

## 四、复刻/排查工具

- `_tmp/gen_mo_layouts.py` — 全流程幂等生成器(环境采集→映射→生成→自校验); 顶替表全在 THING_MAP/DROP/CORPSE_SUB/CARPET_MAP/FLOOR_MAP/STUFF_MAP, 改决策改表重跑即可
- `_tmp/vef_SymbolDef.cs` / `vef_GenStepCSG.cs` / `vef_LayoutDef.cs` / `vef_LayoutUtils.cs` / `vef_SymbolUtils.cs` / `vef_Startup.cs` — KCSG 语义源码存证
- 校验内容: XML 合法性 / token 全覆盖(符号或官方物) / 地形格全在环境 / stuff 与 pawnkind 在场 / defName 无撞名 / 贴图存在

## 五、坑备忘

- 环境无 VFE Architect(MO 依赖), 其地板(Granite_Rough 等)不存在→FLOOR_MAP 映射; Core_SK 彩色地毯整段被注释掉, 别用 CarpetXxx 当映射目标
- 裸 token `Granite/Sandstone/Slate` 是**原版山体岩石 ThingDef**(洞窟当洞壁用, VEF 自动符号), 别当地形; HSK 重名矿床 ThingDef 不影响(裸 token 只认官方物)
- 旧 `_tmp/kccg_symbol_gap.json` 的建议映射名过半不可靠(把 Name= 抓当 defName), 以实际 defName 清单为准
- MO 符号材料 DankPyon_IronIngot/RawWood/Woodlog(原版拼写变体)→STUFF_MAP/大小写兜底
