## 石化工业线一体化方案 · 常减压蒸馏 × 天然气体系 × 边缘石化裂解

> 状态：**待拍板**。按你新流程重构，**后置碳化体系暂缓**（你没概念、先不做），重心放**前段＋天然气体系**。
> 子文档：天然气体系详案 → `石化工业线一体化方案_天然气体系.md`；产物/配方/贴图明细 → `石化工业线一体化方案_产物贴图明细.md`。
> 生成：2026-08-28

---

### 0. 一条线总览（后置已摘除）

```
油井(Rimefeller) → 石油 CrudeOil
   ▼  ①【常减压蒸馏塔】= 美狐高炉改(走 Rimefeller 精炼台机制,5 档 +/-),旁挂【沥青出口】卸沥青
   ├── 直馏馏分 ─► ②边缘石化裂解器 CrudeCracker(Rimefeller 原样) → 化合燃料 + 轻烯烃
   ├── 副产 天然气 ─► 【天然气体系】发电/发热/烧水/存储/管道/燃气台 + 会炸的天然气罐子
   ├── 副产 润滑油 Lubricant(Vile 现成) ─► 机械减磨/HSK 配方
   └── 副产 沥青 Asphalt(污染统一·换皮) ─► 经沥青出口卸货/焚烧制混凝土(兜底)
   （③后置=【芳烃重整台】天界熔炉改：燃油/沥青→丁二烯/二甲苯/苯系 → Vile 尼龙/橡胶/塑料，见 §6）
```

- **沥青**：不新增、不改属性，只借工坊 `3734307960`〔More Useful Materials〕碳纤维卷轴图换皮；由**蒸馏塔**产、**沥青出口**卸。
- **转化比例＝沥青阀门**（§2）：裂解越深沥青越少、气/焦越多，符合真实炼油。

---

### 1. 现状（实测）

| 层 | 素材 | 出处 |
|---|---|---|
| 采掘/裂解 | `OilWell`/`CrudeCracker`/`ResourceConsole`/各 `*Refiner`、`OilBarrel`(原油桶) | Rimefeller_SK |
| 蒸馏塔壳 | **`Miho_ShaftFurnace`(美狐高炉)** 只在 `Experimental/`，**1.6 未加载**，5×5 炉体四向贴图 | 美狐 mod |
| 副产-润滑油 | `Lubricant` **Vile 现成** | Vile's MS |
| 副产-烯烃 | `Propylene`/`Butadiene`/`Xylene` Vile 现成 | Vile's MS |
| 后置芳烃 | `Butadiene`(丁二烯)/`Xylene`(二甲苯) Vile 现成且被 `MakeNylon`/`MakeZylon` 消费；**无 苯/甲苯** | Vile's MS |
| 天然气体系 | VHGE 全套燃气设备(见子文档)，`VHGE_GasExtraction`，仅英文 | `2877699803` |
| 爆炸/卸货模板 | Rimefeller `CompExplosiveTank`、`RefineryLoadingBay`(`AdaptiveStorage`) | Rimefeller_SK |
| 沥青 | `Asphalt` 体系完整 | 污染统一 |

**坑**：① 高炉不加载→先迁 1.6；② VHGE 螺旋气是**管网燃料不可堆叠**→罐子要新建；③ Rimefeller 进料/裂解是代码驱动→保守版不碰。

---

### 2. 常减压蒸馏塔（美狐高炉改）+ 裂解/转化比例

1. **迁+改身份**：`Experimental/…/MihoShaftFurnace.xml` 建筑 def 迁进 `美狐HSK拓展/1.6/Defs/`（贴图 `Cont/` 引用，保留炉体）；`label`→常减压蒸馏塔；摘旧陶瓷配方（产品 defName 不动，确认挂玻璃台）；研究改挂石化蒸馏节点（六档铁律）。
2. **裂解/转化比例＝5 档（+/- 按钮，仿 Rimefeller 裂解厂；不要滑块）**：真实常减压+裂解里，**加工深度**决定轻/重分配——越深→轻馏分+气越多、沥青/润滑油越少。定 5 档等差表（**输入固定 40 原油，每档总料守恒 40，全整数好算**；轻馏分再喂 ②裂解器→燃油，故"多馏分=多燃油"）：

   | 档 | 深度 | 轻馏分(→燃油) | 天然气 | 润滑油 | 沥青 | 合计 |
   |---|---|---|---|---|---|---|
   | 1 | 常压留底 | 10 | 2 | 8 | **20** | 40 |
   | 2 | 偏轻 | 14 | 4 | 6 | 16 | 40 |
   | 3 | 平衡 | 18 | 6 | 4 | 12 | 40 |
   | 4 | 偏重裂解 | 22 | 8 | 2 | 8 | 40 |
   | 5 | 深裂解 | **26** | **10** | 0 | **4** | 40 |

   规律：**每升一档 → 馏分+4、气+2、润滑油−2、沥青−4**，总料守恒。越高档越吃干榨净（燃油/气多、沥青几乎不剩）。
3. **选择器实现（同一张表，两选一）**：
   - **真·+/- 跳档（推荐，和 Rimefeller 裂解厂一样手感）**：小 DLL 组件——一个 1~5 档位整数 + 两个 Gizmo 按钮（＋/−），当前档决定这炉套表里哪一行配方。纯 XML 造不出计数器 UI，要按钮就得走这条。
   - **零代码近似**：5 行做成 5 个配方挂塔上，玩家启用哪个 bill＝跑哪档（是"选档"不是"+/- 计数"）。
4. 副产统一在此台出：馏分、天然气、润滑油、沥青。

---

### 3. 沥青出口（卸货湾）+ 塔架构定稿

你三条要求（+/- 按钮、整套代码和石化一致、必须挨着前置厂沥青从此出口）同指一实现——**蒸馏塔复用 Rimefeller `RefineryBase`+`CompRefinery`（自定义 `CompRefineryAsphalt`）**：挨着卸货湾＝`CompRefinery` 原生强制、+/- 分配＝精炼台自带 UI（5 档映射其档）、产气入网＝同 comp 顺带注 Helixien 网。**沥青出口 = 复刻 `RefineryLoadingBay`**（`AdaptiveStorage.ThingClass`）→ `Petro_AsphaltExportBay`，改名"沥青出口"、过滤只 `Asphalt`、紧贴塔；改色须放**重着色 png 副本**（`loadbay.png` 无遮罩，`<color>` 无效）。降级：不上 DLL 时塔做普通工作台+沥青出口纯存储+PlaceWorker 强制挨着。细节见 `…_产物贴图明细.md`。

---

### 4. 天然气体系（详案见 `…_天然气体系.md` · 已定稿）

- **气源**：蒸馏塔产气入 Helixien 网（小 DLL comp，见 §3），**必须接管道+储气罐 `VHGE_GasTank`，不接/无消纳→放空扔掉**。
- **储运/用**：全套复用 VHGE 现成（储气罐/管道/泵/阀/发电机/加热器/冷却器/燃气灶/燃气台），DefInjected 改名"天然气"+补译中文。
- **罐子/危险**：新建可搬运 `Petro_GasCanister`（仿 `OilBarrel`）；不做泄露事件，罐子/储罐挂 `CompProperties_Explosive` **固定大威力+堆叠连锁（纯 XML）**，被击毁即大火球爆。

---

### 5. 沥青换皮（污染统一·体系不动）

复制 MUM `CarbonFiber_a/b/c.png` → `污染统一/Textures/Things/Item/Resource/Asphalt/Asphalt_a/b/c.png`；`Asphalt` def `graphicClass`→`Graphic_StackCount`、`texPath`→目录；其余全保留。**沥青只从蒸馏塔出（定稿）**：退休 `污染统一/Patches/Patch_AsphaltProducer.xml`（撤掉 CrudeCracker 的沥青自动副产），焚烧制混凝土兜底配方保留。

---

### 6. 后置 · 芳烃重整台（用户新提，采纳）

**天界熔炉改造为"催化重整/芳烃抽提台"**（极致档，压链尾），跑用户给的两条配方：

- **配方3（推荐·零新建）**：`化合燃料 → 丁二烯 + 二甲苯`。两产物皆 Vile 现成，且 Vile `MakeNylon` 吃丁二烯、`MakeZylon`/芳纶/橡胶吃二甲苯 → 直接接进 Vile 高端合成链。
- **配方2（二选一）**：`沥青 → 芳烃`。全 modset **无 苯/甲苯 def、无人消费** → 省事版产物改用现成 `Xylene`(+`Propylene`/`Butadiene`) 与配方3 共用下游；还原版才新建 `Benzene`+`Toluene` 并补消耗端（苯→苯乙烯→聚苯乙烯、甲苯→溶剂/炸药）。

> 闭环：油井→蒸馏塔(沥青/气/润滑油/馏分)→裂解器(燃油)→**芳烃台(丁二烯/二甲苯/苯系)→Vile 尼龙/扎纶/橡胶/塑料**。后置落地，不再暂缓。数值/新建清单见 `…_产物贴图明细.md`。

---

### 7. 落地顺序 · 验证 · 风险

**顺序（定稿）**：① 读 `docs/00/03/04` 核 defName → ② 沥青换皮+退休 CrudeCracker 副产(零风险) → ③ VHGE 改名"天然气"+补译 → ④ 天然气罐子+固定爆炸(纯XML) → ⑤ 高炉迁 1.6+蒸馏塔(走 `CompRefinery`)+沥青出口 → ⑥ 芳烃重整台(天界熔炉改·配方3先落) → ⑦ 小 DLL `CompRefineryAsphalt`(产沥青+5档+注气一体)。
**验证**：补丁 xpath 存在性门控（1.6 Operation 级 MayRequire 失效），不拿 1xx 当最终层；跑 `_tmp/patch_simulator.py`；冷启动读 `Unified.xml`(UTF-8+BOM) 核对高炉已加载/罐子可堆叠可炸/燃气台改名生效/无 duplicate·越级；相关 mod 双目录逐文件 hash 同步。
**风险**：① 罐子爆炸参数过大会拆家/崩档，`explosiveRadius`/`wickTicks` 实测调；② 螺旋气别当堆叠副产；③ 自定义 `CompRefineryAsphalt` 改 Rimefeller 进料/产物涉其代码，先保守版（塔自产、裂解器仍吃原油）；④ 陶瓷产品 defName 牵连 faction/CE/建筑，只改挂载不改 def。

---

### 8. 决策定稿（本轮已拍）

1. **转化比例＝5 档 +/-**：复用 Rimefeller `CompRefinery` 原生分配 UI（§3），5 档映射其档位，不自造 Gizmo/滑块。
2. **气源＝蒸馏塔产气入网**：必须接 管道+储气罐(`VHGE_GasTank`)，不接/无消纳→放空扔掉；小 DLL comp（并入 `CompRefineryAsphalt`）注 Helixien 网。
3. **罐子爆炸＝怎么简单怎么来**：`CompProperties_Explosive` 固定大威力+堆叠连锁，纯 XML 0 DLL；不做泄露事件。
4. **沥青只从蒸馏塔出**：退休 `Patch_AsphaltProducer.xml`（撤 CrudeCracker 副产），单源好控。
5. **VHGE 全套改名"天然气"+补译中文**：确认（DefInjected，根 `<LanguageData>`）。
6. **沥青出口**：复刻 `RefineryLoadingBay`→`Petro_AsphaltExportBay`，改名+重着色 png（`<color>` 对无遮罩 Graphic_Single 无效）+过滤只 `Asphalt`+紧贴塔；塔走 `RefineryBase`/`CompRefinery` 使"必须挨着"原生成立。
7. **后置芳烃台（§6）**：配方3 采纳（燃油→丁二烯+二甲苯，零新建）；**配方2 待你选**：省事版(沥青→现成二甲苯) 还是 还原版(新建苯+甲苯+消耗端)？

**下一步（先做零风险项）**：② 沥青换皮+退休 CrudeCracker 副产 → ③ VHGE 改名补译 → ④ 罐子+固定爆炸。小 DLL（产气入网+5档）与高炉迁移放后面。要我现在开工第②步（纯 XML、可回滚）吗？
