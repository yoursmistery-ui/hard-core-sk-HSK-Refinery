# Vile 贴图还原流程手册

> 适用范围: RimWorld 1.6 + Core SK(HSK)+ Vile's Materials Science + Vile's Hell Bent for Leather Tanning
> 最后更新: 2026-08-27
> 关联: `Vile贴图重构/restore.py`(151 张自定义贴图重放)、AGENTS.md 7.6 节
> **触发条件**: Steam 更新 Vile's Materials Science 或 Vile's Hell Bent for Leather Tanning 后,
> 游戏内 Vile 贴图"回到原版"、矿石图标变 Vile 风、鞣制机变大、HSK 原版贴图消失。

---

## 0.5 2026-08-27 执行记录(Steam 更新后重放)

**触发**: Steam 更新 Vile 系列 mod 至 `Downloads\Viles_20260826` 版本,部署目录被还原为原版:
`Common\Patches\zzz\` 整目录消失、Leather Tanning 自定义补丁/汉化/DLL 全部丢失。

**权威源**: 用户提供 `_tmp\vile\`(Steam 更新前的完整自定义快照,含全部 zzz 补丁/TanningDrum 补丁/DLL/汉化)。

**执行内容**:
1. **工作 A**: 从 `_tmp\vile\Vile's Materials Science\Common\Patches\zzz\` 复制
   `ZZ_RestoreHSKTextures.xml`(63 项平铺)+ `zz_PVCLeather_Lavender.xml`(3 项)→ 部署 `Mods\Vile's Materials Science\Common\Patches\zzz\`。
2. **工作 B**: 从 `_tmp\vile\...\Leather Tanning\Patches\` 复制 `zz_TanningDrum_DrawSize_Fix.xml` → 部署 `Mods\Vile's Hell Bent for Leather Tanning\Patches\`。
3. **未部署 B-2(重复防护)**: RecipeIcons/AnyLeather 配方/DLL/汉化已在 HSK修复整合承担
   (06 号含 45_VileLeatherStationIcons、07 号含 44_VileAnyLeatherRecipes、HSKFixPack.dll 含 AnyLeatherRecipeDynamicFix、
   Languages 含任意皮革汉化)——再部署会致 RecipeDef 重复解锁警告,故不复制。
4. **验证**: zzz 补丁 63/63 xpath 命中 Unified.xml;目标贴图路径全部存在
   (Rutile→Vile、Quartz/Aerographene→HSK修复整合、其余→Core_SK)。ET.parse 全部通过。
5. **HSK修复整合 Source 同步**(5 新增 + 2 修改 .cs 已复制;HSKFixPack.dll 因游戏进程占用待关闭后同步,
   类名对比与工作区完全一致,功能等价)。

### 0.5.1 重大发现: zzz 补丁从未生效 → 全部迁移进 HSK修复整合(2026-08-27 下午)

**发现**: 用户要求"全部扔到 hsk修复整合"。核对 Unified.xml(游戏最终 def 数据库)发现——
**59 个 zzz 目标 def 的 texPath 仍是 Vile 版**(Iron→Magnetite、Gold→NativeGold、Anglesite→Galena、
ComponentIndustrial→ThreadedFasteners、Hexcell→NanowireBattery、ElectricSmelter→MEP、Steel→MildSteel 等)。
即**放在 Vile MS 本体 `Common/Patches/zzz/` 的平铺版 zzz 补丁从未生效**——Vile 的 `Core_SK_Overlap`
等补丁用 `SK.PatchOperationReplaceExtended`(深度合并)在同一 mod 内更晚加载,覆盖了 zzz 的 Replace。
文档 §1.1 的"旧版从未生效"结论同样适用于平铺版(同 mod 内无法压过 Vile 深度合并)。
仅 7 个 def 已是 HSK 版(Aluminium→Bauxite、TinBar/NickelBar/Chromium/CopperBar、Aerographene、Quartz),
由 HSK修复整合其他补丁/贴图覆盖修复。Compaste、Polymers、Paraffins 已由 02/06 号修复(✓ 生效)。

**根治**: 创建 `HSK修复整合/Patches/10_Vile贴图还原.xml`(2026-08-27),把 zzz 62 项(去 Compaste,02 号已处理)
+ PVC 3 项 + drawSize 1 项 = **66 项 PatchOperationReplace** 平铺,`MayRequire="skyarkhangel.HSK"` 门控。
HSK修复整合加载序 167 > Vile 157,独立 mod 后加载,**能压过 Vile 深度合并**,真正生效。
同步: 工作区+部署双份;并从 Vile MS 删除 `Common/Patches/zzz/`、从 Leather Tanning 删除
`zz_TanningDrum_DrawSize_Fix.xml`(防重复)。模拟验证: 66 项 xpath 全命中,最终 texPath/drawSize 正确。

⚠️ **结论**: 以后 Vile 贴图还原**一律放 HSK修复整合**(10 号),不放 Vile mod 本体(不生效且被 Steam 还原)。
`_tmp\vile` 快照仍为权威源,但补丁载体已迁移。

---

## 0.6 关键媒体文件清单(2026-08-27 快照)

| 文件 | 位置 | 说明 |
|---|---|---|
| `10_Vile贴图还原.xml`(66 项) | `HSK修复整合\Patches\` | **生效载体**(迁移后唯一需要维护的补丁) |
| `ZZ_RestoreHSKTextures.xml`(63 项) | `_tmp\vile\...\Common\Patches\zzz\` | 权威源(历史,已弃用) |
| `zz_PVCLeather_Lavender.xml`(3 项) | `_tmp\vile\...\Common\Patches\zzz\` | PVC 薰衣草权威源 |
| `zz_TanningDrum_DrawSize_Fix.xml`(1 项) | `_tmp\vile\...\Leather Tanning\Patches\` | drawSize 权威源 |
| 151 张重构贴图 | `HSK修复整合\Textures\`(工作区+部署) | `Vile贴图重构\restore.py` 重放 |
| Core_SK 矿石/锭/合金贴图 | Core_SK 自带 | zzz/10 号 texPath 引用,无需复制 |
| Vile `Textures\Things\Item\Ores\Rutile\*` | Vile MS | 金红石贴图(zzz 指向 Vile 自带) |
| HSK修复整合 `Textures\...\Quartz\*`、`Aerographene\*` | HSK修复整合 | 石英/气凝胶 |
| 鼠族HSK拓展 `Textures\Things\Item\Ores\Bauxite\*` | 鼠族HSK拓展 | 铝土矿覆盖 |

---

## 0. 总览(三套互相独立的还原工作)

| # | 还原什么 | 交付物 | 部署位置 | 覆盖范围 |
|---|---|---|---|---|
| A | Vile MS 改掉的 **HSK 原版贴图**(矿石/零部件/合金/锭/Compaste/Carbon/Hexcell/ElectricSmelter) | ⚠️ **2026-08-27 起**: `HSK修复整合\Patches\10_Vile贴图还原.xml`(66 项,含 A/A-2/B 全部);历史载体 `ZZ_RestoreHSKTextures.xml`(63 项)已弃用 | **`Mods\HSK修复整合\Patches\`**(加载序 167 > Vile 157 才生效;放 Vile 本体不生效) | 62 个 ThingDef 的 graphicData |
| A-2 | **PVC 人造革改用嫘萦贴图 + 薰衣草配色**(2026-08-19 用户要求) | 10 号补丁(3 项:texPath→Things/Item/Textile/Polyester,color→(185,160,230),stuffProps) | `Mods\HSK修复整合\Patches\` | PVCLeather 的 graphicData/stuffProps |
| B | **鞣制机 TanningDrum** 过大的 drawSize | 10 号补丁(1 项,drawSize→(3,2)) | `Mods\HSK修复整合\Patches\` | TanningDrum drawSize |
| B-2 | 鞣制机配方图标 + 两条「任意皮革」配方 | `zz_TanningDrum_RecipeIcons.xml` / `zz_TanningDrum_AnyLeather.xml` / `TanningDrumDynamicFix.dll` / `Languages\ChineseSimplified (简体中文)\DefInjected\RecipeDef\任意皮革.xml` | ⚠️ **2026-08-21 起迁入 HSK修复整合**,不再部署到 Leather Tanning(44/45 号→07/06 号 + HSKFixPack.dll + Languages) | 17 条配方补图标 + 2 条新配方 |
| C | Vile MS 的 **151 张自定义贴图**(png 重构) | `Vile贴图重构/restore.py` | 复制回 `Mods\Vile's Materials Science\Textures\` | 151 张 png + 150 个 dds 禁用 |
| D | 乙烯/特氟龙 **HSK 原版贴图还原**(改 def 路径,2026-08-19 用户澄清「我指的是 hsk 原版的边缘石化的贴图」) | `HSK修复整合/Patches/38_乙烯特氟龙HSK原版贴图.xml`(整体 graphicData 替换) | `Mods\HSK修复整合\Patches\` | Polymers / Paraffins 两个 ThingDef 的 texPath |

三者独立、互不冲突。Steam 更新任一 Vile mod 后,对应的还原工作**全部需要重放**。

---

## 1. 背景与根因(2026-08-19 实测结论)

### 1.1 旧版 zzz 补丁从未生效(重大发现)

- 旧的 `ZZ_RestoreHSKTextures.xml` 用 `PatchOperationFindMod` + `PatchOperationSequence`
  嵌套写法,在 RimWorld 1.6 + XML Extensions 下**不生效**(AGENTS.md「补丁写法铁律」)。
- 铁证: 游戏导出 `Mods\Unified.xml`(UTF-16LE 编码)逐项核对,67 项中 42 项 diff——
  矿石全部仍是 Vile 贴图(Iron→Magnetite / Gold→NativeGold / Anglesite→Galena /
  Ilmenite→Rutile / Uranium→Carnotite / Tin→Cassiterite / Copper→Chalcopyrite /
  Nickel→Pentlandite / Wolframite→Scheelite)、Carbon→Chemical/Graphite、
  Hexcell(纳米电池)→Parts/NanowireBattery、ElectricSmelter→MEP、
  ComponentIndustrial→ThreadedFasteners 等。
- 用户此前以为已还原,实际从未生效。**重建版已改为平铺写法并实测静态验证。**

### 1.2 Steam 更新破坏模式

- 更新 Vile's Materials Science: ①删除 `Common/Patches/zzz/` 整个目录(zzz 补丁丢失);
  ②把 `Textures/` 下 png 恢复原版、清除 `.dds.hskbak`(restore.py 的成果被清)。
- 更新 Vile's Hell Bent for Leather Tanning: ①TanningDrum 的 drawSize 由 (3,2) 改回 (4,4);
  ②删除用户自定义文件(`Defs/zz_UserFix_LeathersS.xml`、`Patches/zz_UserFix_*.xml`、整个 `Languages/` 目录)。
- 回收站里能找到被替换掉的旧版 mod 完整目录(见 §5 路径清单),是重建补丁的原始出处。

---

## 2. 工作 A: zzz 补丁(HSK 原版贴图还原)

### 2.1 交付物

`Mods\Vile's Materials Science\Common\Patches\zzz\ZZ_RestoreHSKTextures.xml`
- 63 个 `PatchOperationReplace`,平铺写在 `<Patch>` 下,每个带 `MayRequire="skyarkhangel.HSK"` 门控。
- 覆盖: 10 矿石(Iron/Gold/Anglesite/Nickel/Tin/Copper/Aluminium/Ilmenite/Wolframite/Uranium)、
  零部件 ×4(ComponentIndustrial/Advanced/Spacer/Ultra)、Carbon、Hexcell、Matter、Compaste、
  锭 ×7(SilverBar/GoldBar/TinBar/NickelBar/CopperBar/Chromium/SteelBar)、Steel/Plasteel/Powder、
  ElectricSmelter、GlassBatch、Synthread/Kevlar、合金 ×18、塑料 ×3、Aerographene/AluminiumBar/
  AnodizedAluminium×2、Vile 矿石(Titanomagnetite/Sphalerite/BogIron/Quartz)、AlphaPoly/BetaPoly 等。
- 加载机制: Vile MS 的 `Loadfolders.xml` v1.6 加载 `Common` 目录;RimWorld 递归加载子目录补丁
  (与 CombatExtended `ModPatches/` 同机制);`zzz` 字母序靠后,晚于 Vile 的
  `ElectricSmelter_Patch.xml`(字母 E)加载,天然覆盖。
- 剔除项(勿加回): **Polycarbonate、Polystyrene**——HSK修复整合 15/16 号补丁(2026-08-13
  用户要求)已把它们改回 Vile 原版贴图,zzz 再加会冲突;且 Polystyrene 的 zzz 旧目标
  `Things/Item/Chemical/Ethylene` 贴图不存在。
- 剔除项(2026-08-19 报错修复): **Plexiglass、ABS**——zzz 旧目标 `Rimefeller/Things/Resource/
  composite` 只有单张 composite.png,而这两个 def 是 `Graphic_StackCount`(需要 `_a/_b/_c`
  三张堆叠图),加载时报 `No textures found at path Rimefeller/Things/Resource/composite`。
  剔除后保持 Vile 原版贴图(Plexiglass→`Things/Item/Material/PolycarbonateClear`、
  ABS→`ABSBlack`,均有 _a/b/c 堆叠图,且是 restore.py 放的自定义图)。
- ⚠️ **教训**: 补丁里 graphicClass 与贴图形态必须匹配——`Graphic_StackCount` 要 `_a/_b/_c`,
  `Graphic_Single` 要单张。验证脚本 `tmp/check_graphic_vs_tex.py` 逐项核对(63/63 通过)。
- **2026-08-19 用户定制**(非纯还原,勿回退):
  - 金红石 `Ilmenite` → `Things/Item/Ores/Rutile` + color `(161,37,51)`(红色主调,
    即 Vile 原版 Rutile 配置;zzz 原把 Ilmenite 还原成 Core_SK 灰蓝 (160,178,195),用户要求红色主调)。
  - 钛磁铁矿 `Titanomagnetite` → `Things/Item/Ores/Rutile` + color `(161,37,51)`
    (用金红石贴图;zzz 原用 Core_SK Ilmenite 灰 (95,100,105))。
  - 铝土矿 `Aluminium`: zzz 只改 texPath 为 `Things/Item/Ores/Bauxite`,但 Core_SK 与 Vile MS
    同路径贴图,Vile 后加载覆盖 → 游戏内仍显示 Vile 铝土矿。修复: 把 Core_SK 的
    `Bauxite_a/b/c.png` 复制到**鼠族HSK拓展** mod `Textures/Things/Item/Ores/Bauxite/`
    (工作区+部署双份,该 mod 后加载覆盖 Vile,且 Steam 更新 Vile 不影响)。
  - **PVC 人造革**(`zz_PVCLeather_Lavender.xml`,2026-08-19): texPath 改指嫘萦(Rayon)共用的
    `Things/Item/Textile/Polyester`(_a/b/c 堆叠贴图),graphicData + stuffProps color 改薰衣草紫
    (185,160,230);不用新贴图文件,纹理跟随嫘萦、颜色乘算。等级/平衡/翻译归 HSK修复整合
    34 号补丁(→LeathersB B+ 级),本补丁只管视觉。

### 2.2 重建步骤(从零生成,通常不需要)

工作区脚本 `tmp/gen_zzz_flat.py`: 从回收站旧版 zzz(`$RC2MOKN`)提取 67 项 →
生成平铺版 → 剔除 Polycarbonate/Polystyrene(15/16 号冲突)→ 再剔除 Plexiglass/ABS(composite 报错)→ 得 63 项。
如旧版丢失,直接以部署目录 `Common\Patches\zzz\ZZ_RestoreHSKTextures.xml` 为权威源复制即可。

### 2.3 重放步骤(Steam 更新后必做)

```bat
:: 1. 确保 zzz 目录存在
mkdir "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Materials Science\Common\Patches\zzz"

:: 2. 复制补丁(来源: 工作区权威备份 tmp\vile_patches_backup\)
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\ZZ_RestoreHSKTextures.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Materials Science\Common\Patches\zzz\ZZ_RestoreHSKTextures.xml"
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\zz_PVCLeather_Lavender.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Materials Science\Common\Patches\zzz\zz_PVCLeather_Lavender.xml"
```

### 2.4 验证(静态,无需进游戏)

```bash
python tmp/verify_hits.py     # 65/65 XPath 命中 Unified.xml
python tmp/verify_final_tex.py # 目标贴图路径全部存在(3 个 flagged 为误报,见下)
```
- ⚠️ **verify_hits.py / verify_final_tex.py 脚本已随旧 tmp 目录丢失**(2026-08-27)。替代验证脚本(留在工作区 `_tmp/`):
  `_tmp/_vile_verify_zzz4.py`(lxml 解析 Unified.xml 逐 xpath 命中)+ `_tmp/_vile_verify_tex2.py`(贴图路径存在性)。
  2026-08-27 实测: 63/63 xpath 命中、目标贴图路径全部存在。
- `verify_final_tex.py` 报的 3 个"missing"实为误报/无害:
  ElectricSmelter→`TableFurnaceElectric`(HSK 原版,目录存在,脚本漏判);
  Aerographene、Quartz 的目标路径 == Vile 自己的当前路径(改与不改无差别)。
- 配合: HSK修复整合 `14_金属提炼厂占地修复.xml`(最终层)把 ElectricSmelter size 改为 (3,1),
  与 zzz 的 drawSize (3,1) 配套。确认 `ModsConfig.xml` 中 hskfixpack/vitech.materialsscience/
  skyarkhangel.hsk 均已启用。

---

## 3. 工作 B: 鞣制机 TanningDrum 修复

### 3.1 根因

Leather Tanning 更新后 TanningDrum(size 3×2)的 graphicData/drawSize 由 **(3,2) 改为 (4,4)**,
3×2 建筑配 4×4 绘制尺寸 → 贴图明显偏大。贴图文件本身未变(新旧逐字节一致)。

### 3.2 交付物

`Mods\Vile's Hell Bent for Leather Tanning\Patches\zz_TanningDrum_DrawSize_Fix.xml`
(放在 mod 根目录 `Patches/`,LoadFolders default 加载 `/`,全版本生效)

```xml
<Patch>
  <Operation Class="PatchOperationReplace">
    <xpath>Defs/ThingDef[defName="TanningDrum"]/graphicData/drawSize</xpath>
    <value><drawSize>(3,2)</drawSize></value>
  </Operation>
</Patch>
```

### 3.3 重放步骤(Steam 更新 Leather Tanning 后必做)

从工作区权威备份复制回部署目录即可:
```bat
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\zz_TanningDrum_DrawSize_Fix.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Hell Bent for Leather Tanning\Patches\zz_TanningDrum_DrawSize_Fix.xml"
```

### 3.4 验证

进游戏放置 TanningDrum,确认尺寸正常。其他鞣制建筑(TanningRack 1.0/BriningStation 4,4/
DyeingStation 3.8/TanningVat 3.1)新旧一致,不需处理。

---

## 3.5 工作 B-2: 鞣制机配方图标 + 任意皮革配方(2026-08-19)

### 3.5.1 内容

- **图标**: 原版 `RecipesTanningChrome.xml` 的 19 条配方只有 2 条声明 `<uiIconThing>`,
  其余 17 条菜单图标缺失/兜底。`zz_TanningDrum_RecipeIcons.xml`(17 条平铺
  `PatchOperationAdd`)给缺图标的配方补 `uiIconThing` = 各自主产物皮革
  (TanDrum_LeatherLightRodent→Leather_Light、TanDrum_LeatherExotic→Leather_Lizard、
  TanDrum_LeatherBreathable→Leather_Boomanimal、TanDrum_LeatherHeavy→Leather_Elephant、
  TanDrum_LeatherWaterproof→Leather_Waterproof、TanDrum_LeatherDurable→Leather_Rhinoceros、
  TanDrum_LeatherStock→Leather_Plain、TanDrum_LeatherSoft→Leather_Pig、
  TanDrum_LeatherSterile→Leather_Nightling、TanDrum_FurWarm→Leather_Bluefur、
  TanDrum_FurRugged→Leather_Wolf、TanDrum_FurHeavy→Leather_Heavy、TanDrum_FurFine→Leather_Panthera、
  4 条 Split→Leather_CorrectedGrain)。已自带图标的 LeatherLight/HumanLeather 不重复 Add。
- **新配方**(`zz_TanningDrum_AnyLeather.xml`,一条 Add 到 Defs 两条 RecipeDef):
  - `TanDrum_AnyLeather`「鞣制任意皮革 (x25)」: 任意兽皮
    (`categories=UncuredHides+CuredHides`,与 mod 自带 TanBrain_LeatherBuckskin 同款过滤器,
    恰好覆盖全部 30 个 Hide def)+ 铬 ×5 → 对应皮革 ×25。产物由 DLL 按投入兽皮动态选择;
    占位产物 `Leather_Plain 25` 为无补丁回退。`uiIconThing=Leather_Plain`。
  - `TanDrum_SplitAnyLeather`「分割并鞣制任意皮革 (x25)」: 同输入 →
    `Leather_CorrectedGrain 25 + Leather_Chinchilla 25`(与系列 4 条剖层配方同产物,固定,无需 DLL)。
    `uiIconThing=Leather_CorrectedGrain`。
  - 两条均 `recipeUsers→TanningDrum`、`workSpeedStat=TailoringSpeed`、`workSkill=Crafting`、
    `workAmount 800/1200`,与现有 19 条同构。
- **DLL**: `Assemblies\TanningDrumDynamicFix.dll`(源码 `TanningDrumDynamicFix.cs`,
  系统 csc 编译,Harmony 前缀补丁 `GenRecipe.MakeRecipeProducts`,仅对 `TanDrum_AnyLeather` 生效)。
  兽皮→皮革映射: LightAnimalSkin/RodentSkin→Leather_Light、ExoticSkin→Leather_Lizard、
  PorousHide→Leather_Boomanimal、HeavyHide→Leather_Elephant、HeavyFurPelt→Leather_Heavy、
  WaterproofSkin→Leather_Waterproof、DurableHide→Leather_Rhinoceros、RuggedFurPelt→Leather_Wolf、
  StockHide→Leather_Plain、SoftHide→Leather_Pig、WarmFurPelt→Leather_Bluefur、
  ImmunogenicHide→Leather_Nightling、RichFurPelt→Leather_Panthera、HumanSkin→Leather_Human;
  未知兽皮回退占位。`Cured` 后缀在映射前剔除(生皮/腌制皮同产)。
- **中文翻译**: `Languages\ChineseSimplified (简体中文)\DefInjected\RecipeDef\任意皮革.xml`
  (UTF-8 BOM 必须,66 号同法 first-wins)。

### 3.5.2 重放步骤(Steam 更新 Leather Tanning 后必做)

从工作区权威备份复制回部署目录(`tmp\vile_patches_backup\`,与 drawSize 补丁同目录):
```bat
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\zz_TanningDrum_RecipeIcons.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Hell Bent for Leather Tanning\Patches\zz_TanningDrum_RecipeIcons.xml"
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\zz_TanningDrum_AnyLeather.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Hell Bent for Leather Tanning\Patches\zz_TanningDrum_AnyLeather.xml"
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\TanningDrumDynamicFix.dll" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Hell Bent for Leather Tanning\Assemblies\TanningDrumDynamicFix.dll"
copy "C:\Personal\Project\ratkin-patch\tmp\vile_patches_backup\Languages\ChineseSimplified (简体中文)\DefInjected\RecipeDef\任意皮革.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Vile's Hell Bent for Leather Tanning\Languages\ChineseSimplified (简体中文)\DefInjected\RecipeDef\任意皮革.xml"
```
⚠️ Steam 更新会连同 `Languages\` 目录一起删(见 §1.2),重放需重建目录。

### 3.5.3 验证

进游戏: 鞣制机配方菜单 19+2 条均有皮革图标;新配方可造——任意兽皮(生/腌均可)→ 对应皮革 ×25,
分割配方 → 未抛光 + 剖层皮革各 25;日志无红字、启动有 `[TanningDrumDynamicFix] patched
GenRecipe.MakeRecipeProducts`。改补丁后重启需清 MissileGirl 缓存。

---

## 4. 工作 C: 151 张自定义贴图部署(2026-08-19 改为补丁包方式)

> **2026-08-19 重大变更**: 重构贴图不再直接复制回 Vile mod(会被 Steam 更新清掉、且违背
> "补丁改动放补丁包"原则),改为部署到 **HSK修复整合** 的 `Textures/` 目录。
> 原理: RimWorld 贴图按 mod 加载顺序查找,`local.hskfixpack`(加载序 164)后于
> `vitech.materialsscience`(155),同路径 png 覆盖 Vile 原版(含 .dds)——与铝土矿
> 先例(鼠族HSK拓展 162 后加载覆盖 Vile 155)同机制,Steam 更新 Vile 不影响。

```bash
# 1. 铺到 HSK修复整合 工作区 Textures(151 张,按映射表)
python "C:\Personal\Project\ratkin-patch\Vile贴图重构\restore.py"

# 2. 同步到游戏部署目录(工作区 Textures -> Mods\HSK修复整合\Textures)
#    手工复制即可(或 cp -r 覆盖 Things/Item、Things/Building 等)
```

- **不再需要**: 禁用 Vile 的 .dds、改 .dds.hskbak、直接改 Vile mod 文件。
- **Vile mod 已还原原版**(2026-08-19): 原 153 个 `.dds.hskbak` 全部恢复为 `.dds`,
  151 张重构 png 已移出(备份在 `tmp\vile_restored_png_backup\`),Vile Textures 仅剩原版文件。
- 与工作 A(zzz 补丁)独立,但**每次更新 Vile MS 两者都要核对**: zzz 补丁仍在 Vile mod 内
  (Steam 更新会被删,需重放),贴图在 HSK修复整合(Steam 更新 Vile 不影响,无需重放)。

---

## 4.5 工作 D: 走 def 补丁的 HSK 原版贴图还原(2026-08-19,乙烯/特氟龙)

### 4.5.1 何时走 def 补丁,不走贴图覆盖

大部分 HSK 原版贴图还原 = 在 HSK修复整合 Textures 铺同名 png 覆盖 Vile 路径(工作 C)。
但**当 Core_SK 自带的原版贴图已存在、且 HSK 原版 def 的 texPath 指向 Core_SK 自带路径时**,
更干净的方案是直接改 def 的 `texPath` 回 Core_SK 路径,完全不碰贴图文件:

- **不铺 png** —— Steam 更新 HSK 自带贴图随 Core_SK 一同更新,无需我们管;
- **def 改路径** —— 改 Vile 的 def 回到 Core_SK 路径,1 个 `PatchOperationReplace` 即可;
- **与 15/16 号同款** —— 15 号聚碳酸酯、16 号聚苯乙烯都是改 def 的 graphicData 路径(回 Vile 原版或 HSK 原版)。

### 4.5.2 乙烯/特氟龙案例(2026-08-19)

**踩坑**: 之前 Vile贴图重构把乙烯/特氟龙按"贴图覆盖"方式铺 png 到 HSK修复整合
`Textures/Things/Item/Chemical/{Ethylene,Teflon}.png`,但重构源 md5 与 Vile 原版 dds
**完全一致**(红气瓶 / 白方框),实际是把 Vile 贴图当 HSK 原版铺,游戏里仍是 Vile 视觉。
用户澄清"我指的是 hsk 原版的边缘石化的贴图" → 边缘石化 = Rimefeller = HSK 石油化工体系
原版图 = Core_SK 自带 `Things/Item/Resource/Polymers.png`(紫塑料颗粒)/ `Paraffinwax.png`(米黄蜡块)。

**修复**:
- 新建 `Mods\HSK修复整合\Patches\38_乙烯特氟龙HSK原版贴图.xml`:
  2 个 `PatchOperationReplace`,整体替换 `Polymers` / `Paraffins` 的 graphicData,
  texPath 分别指向 `Things/Item/Resource/Polymers` / `Things/Item/Resource/Paraffinwax`,
  graphicClass `Graphic_Single`, color `(255,255,255)`(与 Core_SK 完全一致)。
- 清理 HSK修复整合 `Textures/Chemical/{Ethylene,Teflon}.png`(工作区+部署,4 处)
  + Vile贴图重构 `贴图/乙烯(Polymers).png` + `贴图/特氟龙(Paraffins).png`(2 个错误源),
  备份到 `tmp\ethylene_teflon_texture_backup_20260819\`。
- 映射表 / 说明.md 把这两项从 151 贴图清单移除(改走补丁方式)。
- About.xml 追加 39 号条目;AGENTS.md 补丁列表同步。

**重放步骤(Steam 更新 Vile MS 后)**:
```bat
copy "C:\Personal\Project\ratkin-patch\HSK修复整合\Patches\38_乙烯特氟龙HSK原版贴图.xml" ^
     "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\HSK修复整合\Patches\38_乙烯特氟龙HSK原版贴图.xml"
```
无需重铺贴图(Core_SK 自带)。

---

## 5. 关键路径清单

| 用途 | 路径 |
|---|---|
| zzz 补丁部署位 | `Mods\Vile's Materials Science\Common\Patches\zzz\ZZ_RestoreHSKTextures.xml` |
| 鞣制机补丁部署位 | `Mods\Vile's Hell Bent for Leather Tanning\Patches\zz_TanningDrum_DrawSize_Fix.xml` |
| 生成脚本 | `tmp\gen_zzz_flat.py` |
| 验证脚本 | `tmp\verify_hits.py` / `tmp\verify_final_tex.py` / `tmp\verify_zzz_paths.py` |
| **补丁权威备份**(重放用) | `tmp\vile_patches_backup\ZZ_RestoreHSKTextures.xml` + `tmp\vile_patches_backup\zz_TanningDrum_DrawSize_Fix.xml` |
| **⚠️ 2026-08-27 权威源(优先)** | `_tmp\vile\`(Steam 更新前完整自定义快照,含全部 Vile 系列自定义内容) |
| **38 号补丁权威备份** | `HSK修复整合\Patches\38_乙烯特氟龙HSK原版贴图.xml`(已随工作区自动同步) |
| **被废弃的乙烯/特氟龙重构贴图备份** | `tmp\ethylene_teflon_texture_backup_20260819\`(4 个 HSK修复整合 png + 2 个 Vile贴图重构 源图,均为 Vile 复制品,确认错误后清理) |
| 贴图重放 | `Vile贴图重构\restore.py` + `映射表.csv` + `贴图\` |
| 回收站旧版 Vile MS(zzz 原始出处) | `C:\$Recycle.Bin\S-1-5-21-938251223-169792323-3726176951-1002\$RC2MOKN\Common\Patches\zzz\ZZ_RestoreHSKTextures.xml` |
| 回收站旧版 Leather Tanning(对比基准) | `C:\$Recycle.Bin\S-1-5-21-938251223-169792323-3726176951-1002\$REXG7SO\` |
| 游戏最终 def 数据库 | `Mods\Unified.xml`(UTF-16LE 编码,导出工具: Unified XML Export) |
| ModsConfig | `C:\Users\admin\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml` |

---

## 6. 已知坑(务必遵守)

1. **⚠️ 放 Vile mod 本体不生效(2026-08-27 实锤)**: zzz 平铺版放 Vile MS `Common/Patches/zzz/` **从未生效**
   ——Vile 的 `Core_SK_Overlap` 等补丁用 `SK.PatchOperationReplaceExtended` 深度合并在更晚加载覆盖。
   **贴图还原一律放 HSK修复整合 10 号**(加载序 167 > Vile 157,独立 mod 才能压过)。
2. **旧 zzz 写法失效**: `PatchOperationFindMod`+`PatchOperationSequence` 嵌套在 1.6 不生效。
   一律用平铺 `Operation Class="PatchOperationReplace"` + `MayRequire` 属性。
3. **Unified.xml 是 UTF-16LE**: 直接 grep 中文/标签会乱;`python .decode('utf-16')` 后
   `rfind('<ThingDef')`+`find('</ThingDef>')` 切片提取单个 def。
4. **MayRequire 值**: 用 packageId `skyarkhangel.HSK`(Core SK),不是显示名 "Core SK"。
5. **剔除项勿加回**: Polycarbonate/Polystyrene 归 HSK修复整合 15/16 号管(改回 Vile 原版);
   Polymers(乙烯)/Paraffins(特氟龙)归 38 号管(改回 Core_SK 原版);Compaste 归 02 号(10 号不含)。
6. **ElectricSmelter 需双补丁**: zzz 改 graphicData/drawSize (3,1),14 号补丁改 size (3,1),缺一不可。
7. **Steam 更新会删 Vile 原 mod 内自定义**: 但 HSK修复整合 10 号不受影响;151 张 png 在 HSK修复整合 Textures 也不受影响。
   仅需确认 10 号补丁存在即可,无需重放 Vile。
8. **Leather Tanning 更新还会删用户自定义文件**(zz_UserFix_*、Languages/): 工作区
   `Vile鞣制1.6适配/` 有备份,若用户要用需另行部署(2026-08-19 已确认丢失,是否恢复由用户决定)。

---

## 7. 检查清单(Steam 更新任一 Vile mod 后)

> ✅ 2026-08-27 重放完成状态(执行记录见 §0.5)。

- [x] `HSK修复整合\Patches\10_Vile贴图还原.xml` 存在(66 项:zzz 62 去 Compaste + PVC 3 + drawSize 1)(2026-08-27 迁移)
- [x] `Common\Patches\zzz\` 已从 Vile MS **删除**(10 号接管,防重复;旧权威源在 `_tmp\vile`)
- [x] `Patches\zz_TanningDrum_DrawSize_Fix.xml` 已从 Leather Tanning **删除**(10 号接管)
- [x] `python Vile贴图重构\restore.py` 跑过(151 张 png 铺到 HSK修复整合 Textures,工作区+部署)——工作区/部署各 151 张 png 一致
- [ ] `python tmp\verify_hits.py` → 65/65(脚本在旧 tmp 目录缺失;2026-08-27 用 lxml 替代验证:66/66 xpath 命中)
- [x] `HSK修复整合\Patches\38_乙烯特氟龙HSK原版贴图.xml` 存在(2 Operation,Polymers/Paraffins 改回 Core_SK 路径)——已合并进 06_图标.xml
- [ ] **HSK修复整合 Textures/Chemical 下不应再有 Ethylene.png / Teflon.png**(38 号补丁后改 def 路径,这俩是死文件)
- [ ] 游戏内: 矿石图标为 HSK 原版、金属提炼厂为电动熔炼炉台桌外观、鞣制滚筒大小正常、乙烯=紫塑料颗粒袋、特氟龙=米黄蜡块
- [ ] (可选) Leather Tanning 用户自定义文件是否恢复
