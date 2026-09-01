# 图纸门禁子模块 SchematicGateHSK 详细方案

> 2026-08-27。状态:**⚠️ 已回退(同日蓝图经济 v4.1 取代)** —— 用户澄清"用MO的"指贴图而非图纸门禁体系; 电力工程I 回归蓝图书单卷门禁(贴图保留 MO 卷轴), SchematicGateHSK 代码/贴图归档于 `Source_SchematicGateHSK/` 与 `_tmp/backup_schematic_retire_20260827/`, defs/补丁/dll 已从两目录撤除。本文件保留作 MO RequiredSchematic 反编译语义参考。 ~~已实现并双目录同步,待游戏实测~~。归属 mod:`HSK工业科研大修`(ratkinpatch.HSKIndustrialResearchOverhaul),独立程序集,与蓝图书体系并存互不重叠。
> 决策链: 蓝图 tier2 方案(已作废)→ 用户拍板"电力工程 I 用 MO 的体系"→ MO `RequiredSchematic` 反编译复刻。

---

## 1. 背景与决策记录

| 时间 | 决策 | 结果 |
|---|---|---|
| 08-27 上午 | 电力工程 I 曾按蓝图书体系做 `Blueprint_Electricity`(tier2 首卷) | ❌ **作废回退**(用户改拍板走 MO 体系);defs/中英文条目已删,MO 卷轴贴图保留复用 |
| 08-27 下午 | 环境核查: MO 本体**订阅未勾选**(游戏零 DankPyon def)→ MO DLL 类不可引用,必须复刻 | ✅ 复刻方向确立 |
| 08-27 晚 | 二次核查修正: **VEF Core 实际已激活**(工坊 2023507013 直接加载,首查漏扫工坊) | ✅ 文档已改;本模块不依赖 VEF,后续 P2/P3 任务链可解锁 KCSG 玩法 |
| 08-27 晚 | 做成**独立子模块程序集**(用户要求),留扩展位给藏宝图/废墟/炼金等后续 MO 子系统 | ✅ `SchematicGateHSK.dll` |

## 2. MO 原版语义(反编译依据)

反编译源: `workshop\content\294100\3219596926\1.6\Assemblies\MedievalOverhaul.dll`(ilspycmd)。

- `RequiredSchematic : DefModExtension { ThingDef schematicDef }` 挂在 `ResearchProjectDef` 上。
- `ResearchProjectsDefs` 补丁: `CanStartNow` postfix → `PlayerHasSchematic`: 遍历 `Find.Maps` 殖民地建筑里的 `Building_ResearchBench`,查其**所在房间** `uniqueContainedThings` 中的 `Building_Bookcase.HeldBooks` 是否含目标图纸 → **没有则不可开始研究**。
- `ResearchProjectDef_CanBeResearchedAt` 补丁: 同逻辑但**逐研究台**判定(该台自己房间里有没有)。
- 阅读侧: `BookOutcomeProperties_GainResearchDefinable` 继承原版 `BookOutcomeProperties_GainResearch`,doer=`ReadingOutcomeDoerGainResearch` 子类,`OnBookGenerated` 里 `values[项目]=GetBaseValue()×gainMultiplier`,原版在读书过程 `OnReadingTick` 分摊科研值。
- MO 缺陷(复刻时修复): 两个补丁都用**静态单槽缓存** `cachedSchematicCheck`,跨研究台/跨图纸会串结果。

## 3. 我们的实现(架构)

```
HSK工业科研大修/
├─ Source_SchematicGateHSK/SchematicGateHSK.cs   ← 独立程序集(不动 BlueprintUnlockHSK 一行)
├─ 1.6/Assemblies/SchematicGateHSK.dll            ← build.ps1 一并产出(双程序集)
├─ 1.6/Defs/ThingDefs_SchematicGates.xml          ← 图纸书基类 + Schematic_Electricity
├─ Patches/65_图纸门禁_电力工程I.xml               ← 给 Electricity 挂 SchematicRequiredExt
├─ Patches/66_图纸商人.xml                         ← 异域商队+鼠族王国商队投放/回购
├─ Textures/Things/Item/Blueprints/SchematicScroll/ ×7  ← 取自 MO Special/Book/SchematicScroll*
└─ Languages/English/DefInjected/ThingDef/SchematicGates.xml(中文直写 Defs, 从 Flattened 新约定)
```

### 3.1 数据层

| 类/字段 | 挂载 | 作用 |
|---|---|---|
| `SchematicRequiredExt { schematicDef }` | ResearchProjectDef(补丁 Add) | 声明"该科技需要此图纸就位" |
| `SchematicBookExt { targetTech, gainMultiplier }` | 图纸书 ThingDef | 阅读加哪门科技、倍率 |
| `SchematicBookProperties : BookOutcomeProperties` | 书的 comps.doers | `DoerClass→SchematicBookDoer` |
| `SchematicBookDoer : ReadingOutcomeDoerGainResearch` | — | `OnBookGenerated` 清随机池、锁定 values[targetTech]=基值×倍率;`GetBenefitsString` 显示"推进【X】进度+书架解锁" |
| `StockGenerator_SchematicTrader : StockGenerator` | 商人 stockGenerators | 35% 携带一本随机图纸;`HandlesThingDef` 按 ext 识别(回购友好) |
| `SchematicDatabase.AllBookDefs()` | — | 懒加载扫描所有带 SchematicBookExt 的 ThingDef(加新图纸零代码) |

### 3.2 门禁补丁(两个 postfix + 缓存修正)

- `CanStartNow` getter postfix:全局判定——任一殖民地研究台房间有该图纸→放行;缓存 `Dictionary<schematicDef,(bool,到期tick)>`,250 tick。
- `CanBeResearchedAt(bench, bool)` postfix:**逐台**判定;缓存 `Dictionary<bench,(def,bool,到期tick)>`(修 MO 单槽串台 bug;def 变更自动失效重算)。
- 无扩展的普通科技两次字典查即返回,零开销;不遍历、不反射,Tick 安全(§9 性能铁律)。

### 3.3 与蓝图书体系的边界

| | 蓝图书(BlueprintUnlockHSK) | 图纸(SchematicGateHSK) |
|---|---|---|
| 解锁语义 | 集齐系列卷册全部**研读**→解锁,书退役 | 图纸**放进研究台同房间书架**即可开始,书常驻可反复用 |
| 阅读收益 | 只记"已读",无科研值 | 阅读按原版机制**实时推进科研值**(仿 MO) |
| 档位 | tier1~5(商人按档权重/研读时长递增) | 暂不分档(池小靠 35% 稀缺);需要时 `SchematicBookExt` 加 tier 字段即可 |
| 节点 | 111 本/43+ 科技 | 电力工程 I(首节点) |
| UI 角标 | ResearchTreeSK"已读 x/N" | 无(书架里有没有=能不能点,描述卡有提示与超链接) |

### 3.4 1.6 API 适配结论(踩坑记录)

`CodeHyperlink→Verse.DefHyperlink`(本次改走 XML `descriptionHyperlinks`,零代码);`Room.uniqueContainedThings` 私有→公开泛型 `ContainedThings<Thing>()`;`Def.Label→LabelCap`(TaggedString 隐转 string);`BookOutcomeProperties` 是 Def 子基类无需 defName(与蓝图书同模式);PS5 反射加载 Assembly-CSharp 失败→csc 自写 dumper + AssemblyResolve 指 Managed 目录。

## 4. 物品参数(Schematic_Electricity)

继承 `SG_SchematicBookBase`(BookBase):label 图纸:电力工程 I;MarketValue 500(MO 同款);`ageYearsRange 1000~2000`(MO 古卷语义);tradeTags `Exotic + RK_Schematic`;thingCategories 仅 Books(不进蓝图书类/不进蓝图书奖励池,`thingSetMakerTags` 清空);贴图三态=MO SchematicScroll 卷轴(铺 Graphic_Single / 开卷 Graphic_Multi e/n/s / 竖立 Graphic_Multi+顶高度偏置);`descriptionHyperlinks` → Electricity。
`SchematicBookExt`: targetTech=Electricity,gainMultiplier=1.0(≈原版书科研加成基值×1)。

**门禁对象**: `Electricity`(电力工程 I, Industrial, 1500, 前置 Research_table_B2, 原生 modExtensions=SK 档+ResearchTreeSK → 补丁 65 实机走 match 追加分支)。下游 Generators/Batteries/WatermillGenerator/AirConditioning 等全穿它=「电力钥匙」设计意图。

## 5. 获取渠道(商人)

| 渠道 | 方式 | 参数 |
|---|---|---|
| 原版 `Caravan_Outlander_Exotic` | 66 号补丁 Add trader generator + BuyTradeTag | 35%/商队携一本 |
| 鼠族 `RK_TraderKind_KingdomExotic` | 同上(存在性门控) | 同上 |
| 玩家卖出 | `StockGenerator_BuyTradeTag RK_Schematic` | 图纸可回售,防死当 |
| 任务/事件池 | **暂不注入**(与蓝图文池隔离;需要时 ThingSetMaker 小改) | — |

## 6. 验证与同步记录

- 编译: `build.ps1`(csc v4.0.30319,双程序集)两 DLL 产出无错。
- 补丁模拟: `_tmp/sim_schematic_6566.py` — 65 外层 match/内层分支 Add OK;66 四步全 OK;断言全 PASS(注: 实机 Electricity 原生有 modExtensions,走 match 追加分支,nomatch 分支兜底也已覆盖)。
- 双目录同步: 12 文件逐文件 hash 核对一致(含回退的 Gates xml、修订 About、双语言、DLL、cs、build.ps1、分析文档);Assemblies 两侧各仅 2 个正式 DLL 无残留。
- MissileGirl 缓存 `Unified.xml` 已移回收站,下次启动重导。
- 文档: `科技配置/蓝图科技节点分析.md` 决策改判段已更新;`docs/03_生产框架/中世纪OM商人任务事件专项` VEF 误报已修正。

## 7. 游戏实测清单

1. 启动无红字;日志无 `[SchematicGateHSK] patch failed`(CanStartNow/CanBeResearchAt 双补丁挂载成功)。
2. 未放图纸:电力工程 I 不可开始(研究卡无"开始研究"),自动科研不会选它。
3. 原版书架(或任意 `Building_Bookcase` 子类)与研究台**同房间**放图纸 → 立即可开;隔墙/拆书架后 250 tick 内恢复锁定属预期缓存延迟。
4. 让小人阅读图纸 → 电力工程 I 进度条上涨(阅读时长内分摊,非一次性)。
5. 异域商队/鼠族王国商队到访 → 约 1/3 概率出售图纸(500 银档);可右键卖出回购。
6. 蓝图书体系回归验证: 旧 tier1~5 门控/角标/研读不受影响(零改动)。

## 8. 已知限制与扩展位

- 研究页无"需要图纸:X"红字提示(MO 有 `DrawUnlockableHyperlinks` 补丁)→ v2 可仿加 prefix。
- 不分档投放;`SchematicBookExt` 预留加 tier 字段即可接权重曲线。
- 藏宝图/废墟/坩埚炼金等 MO 子系统 → 全部并入本程序集扩展(命名空间已独立),不再新建第四个 dll;VEF 已激活,废墟类玩法可走 KCSG(见专项方案 P2/P3 修正版)。
- 遗留待决: 部署侧 07:04 六文件语言漂移(Flattened 化)方向未拍板。

## 9. 新增图纸节点三步模板(图纸制若复活时适用; 现行蓝图书加卷看 About 第四段)(复用手册)

1. `ThingDefs_SchematicGates.xml` 加一本: `<ThingDef ParentName="SG_SchematicBookBase"><defName>Schematic_XXX</defName>...<modExtensions><li Class="SchematicGateHSK.SchematicBookExt"><targetTech>目标科技</targetTech><gainMultiplier>1.0</gainMultiplier></li></modExtensions></ThingDef>`(零代码)。
2. 仿 65 号新建补丁: 给目标 `ResearchProjectDef` Add `SchematicRequiredExt`(match/nomatch 双分支)。
3. 中英文案 + 商人已有(池自动收录);跑 `_tmp/patch_simulator.py` 变体验证 → 双目录同步。

---

*门禁参数如与体验冲突: 优先调 gainMultiplier/携带概率,勿撤门禁(与蓝图书同策)。*
