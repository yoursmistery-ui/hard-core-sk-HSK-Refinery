# HSK 本地整合包 · Mod 加载顺序与依赖总表

> 维护日期: 2026-08-28(特性拓展压到内容 mod 末尾)。用户确认的加载逻辑骨架。
> 用途: 所有本地 mod 的 About.xml 依赖/加载顺序以本文件为准;新增/调整 mod 时必须先查本表。
> 加载顺序 = 从上到下(RimWorld 加载时按 loadAfter/loadBefore 图 + Mod Assistant 排序,本表为逻辑顺序)。
> 2026-08-27 更新: 工业大修(HSK工业科研大修)已并入工具工业体系,须 **loadAfter 鼠族HSK拓展**(工业大修排在鼠族之后),见第二段说明。

---

## 一、总顺序骨架(用户确认)

```
【第一段】HSK 一堆 mod
    Core_SK 及附属 / DLC 模块 / 各种 HSK 原厂附属(在游戏 Mods 目录,本地 mod 不干预)
            ↓
【第二段】我的大修(三大修,顺序: 贸易 → 科研 → 工业)
    ① HSK贸易重构   (贸易/经济层)
    ② HSK科研大修   (科研门控/进度/任务层)
    ③ hsk工业大修 / HSK工业科研大修   (工业基础材料 + 工具工业体系: 陶瓷三级/玻璃/书架/工具)
        ⚠️ 工具补丁 patch 上游 RatkinRaceHSK 的 RK_* 工具 def,
           且 94 号删矿镐/匕首需覆盖 鼠族HSK拓展 06 号种族 weaponList → 必须 loadAfter 鼠族HSK拓展
            → 实际排在【第四段】种族 mod 之后(见下文)。
            ↓
【第三段】skill 拓展
    Vanilla Skills Expanded / RatkinBackStoryExpandedHSK / 书籍拓展HSK 等
    ⚠ 2026-08-27: 特性拓展modHSK 移出本段压到末尾; 为此删掉了 VSE 的
      loadAfter vanillaexpanded.vanillatraitsexpanded(VSE 全仓无 VTE_ 引用, 删除无影响)。
            ↓
【第四段】种族 mod
    鼠族HSK拓展 / 美狐HSK拓展 / 金鼠族 HSK版本 等
            ↓
【第五段】工业大修(工具体系) ← 实际位置: 排在种族段之后
    ② hsk工业大修 / HSK工业科研大修 (loadAfter 鼠族HSK拓展 + RatkinRaceHSK + STL + CE)
            ↓
【第五·五段】特性拓展(2026-08-27 后移 → 2026-08-28 压到内容 mod 末尾)
    特性拓展modHSK —— 互斥注入与授予要认得别的 mod 后加进来的 TraitDef, 越晚加载特性池越全
    (防"mod 加特性导致特性找不到")。现 loadAfter 已含 VSE 与工业大修, 后继只剩 修复整合/汉化/EndMod 组
    (_tmp/trait_order_check.py 核对: 8 个后继全部属于压尾组 ✓; cycle_all.py 0 环)。
    ⚠ 不得再 loadAfter local.hskfixpack / aitranslation.pack / EndMod 组 —— 修复整合是"它 loadAfter 本 mod",
      反向声明即 2-环。
    ⚠ 08-27 曾为避环漏掉 VSE/工业大修, 真因是 VSE 有历史遗留边 loadAfter 本 mod(已删, VSE 全仓无 VTE 引用)。
            ↓
【第六段】修复整合(压尾)
    HSK修复整合(本地修复大整合,loadBefore 特性拓展/酒馆/帝国银币等,最后兜底)
            ↓
【第七段】汉化(内容 mod 的最后一位,EndMod 组之前)
    1.6HSK核心全ai汉化 (AITranslation.Pack) — loadAfter local.hskfixpack + skyarkhangel.HSK + ModIndicator
    ⚠️ EndMod 组(trinity.runtimegcfixed / dubwise.dubsperformanceanalyzer / mlie.wikirim /
       taranchuk.performanceoptimizer / vr.missilegirl)必须仍在其后;
       HSK修复整合不得再声明 loadAfter aitranslation.pack(反向 = 2026-08-27 循环依赖真因)
```

---

## 二、本地 mod 依赖速查表(全部 packageId)

| mod 文件夹 | packageId | 依赖(≥) | loadAfter | loadBefore | 备注 |
|---|---|---|---|---|---|
| HSK贸易重构 | local.imperialcoin | Core SK, ModIndicator | 被依赖项 + ModIndicator | — | 贸易大修 |
| HSK工业与科研大修 | ratkinpatch.HSKIndustrialResearchOverhaul | Harmony, Core SK, Solaris.RatkinRaceMod | 依赖项 + **local.imperialcoin(贸易)** + ResearchTreeSK/RimQuest/GoExplore/Cybranian/ModIndicator | — | 工业+科研大修(08-27 合并)。工具体系自鼠族迁入须 loadAfter 种族段;科研侧蓝图门控存在性兼容(详见详录) |
| Vanilla Skills Expanded | vanillaexpanded.skills | — | VEF.Core / RimHUD / Core SK ×3 / ModIndicator(**2026-08-27 已删 loadAfter 特性拓展**) | ratys.madskills | skill 拓展段 |
| 特性拓展modHSK | vanillaexpanded.vanillatraitsexpanded | Harmony, Core SK | 依赖项 + **全部本地内容 mod**(含 VSE/工业大修/种族/背景/书籍/酒馆/贸易/污染/Reel/发型/边境/VME/VSIE)+ traitraritycolors + ModIndicator | — | **内容 mod 末尾**;后继只剩 修复整合/汉化/EndMod |
| 书籍拓展HSK | VanillaExpanded.VBooksE | Harmony, Core SK, ModIndicator | 依赖项 + VEF(若在场)+ ResearchTreeSK + ModIndicator | — | skill 拓展段: 整合 VBE+VBEE+[sbz]Bookcase;VSE_Writing 经 MayRequire 联动;研究挂 Craft_SK tab |
| RatkinBackStoryExpandedHSK | (见其 About) | — | — | — | 背景故事拓展段 |
| 鼠族HSK拓展 | local.ratkin.clothesweapons | Harmony, Core SK, Solaris.RatkinRaceMod | 依赖项 + ModIndicator;loadBefore 金鼠族/家具/徽章 | — | 种族 mod 段 |
| 美狐HSK拓展 | (见其 About) | Core SK | — | — | 种族 mod 段 |
| 金鼠族 HSK版本 | (见其 About) | — | — | — | 种族 mod 段;loadAfter 鼠族HSK拓展? |
| 工作动画HSK | meathax.ShowMeYourTools(沿用工坊原ID) | Harmony, Core SK | 依赖项 + 动画兼容组(详见 About) | — | 2026-08-30 由酒馆拆出(JobEffects),见详录 §7.14;补丁只操作自有节点,跨 mod 引用=字符串无边 |
| 酒馆工具整合HSK | local.hsktavernintegration | Harmony, VEF, Core SK | 依赖项 + local.ratkin.furniture + ModIndicator | — | Simple Doors+Tavern;工作动画已拆出(详录 §7.14) |
| HSK修复整合 | local.hskfixpack | Harmony, Core SK | 依赖项 + ModIndicator(**2026-08-27 已删 aitranslation.pack 反向项**) | **特性拓展/酒馆/帝国银币 + EndMod 组** | 修复整合压尾;汉化包在其后;loadBefore 被其覆盖的 mod |
| 1.6HSK核心全ai汉化 | AITranslation.Pack | Harmony, Core SK | local.hskfixpack + skyarkhangel.HSK + ModIndicator | — | 汉化段(内容 mod 最后一位);HSK 标记走 07_ModAssistant标记.xml;工作区仅镜像 About+Patches |

> ⚠️ 更新中: 各 mod 的完整依赖清单以各自 About.xml 为准;本表记录"段间"关键顺序,避免循环依赖(loadBefore+loadAfter 冲突见 `docs/` 加载顺序循环依赖排查记忆)。

---

## 三、关键依赖规则(写 About.xml 时照抄)

### 3.1 默认声明(所有本地 mod)

- `modDependencies` ≥ `brrainz.harmony` + `skyarkhangel.HSK`(Core SK)。
- `loadAfter` ≥ 依赖项 + `DimonSever000.ModIndicator.Specific`(压尾)。
- 鼠族系 mod 另依赖 `Solaris.RatkinRaceMod`(RatkinRaceHSK)。
- 涉及 ResearchTreeSK / 事件 mod / 书架联动的科研类 mod,loadAfter 追加:
  `qwerty19106.researchtreesk`、`Mlie.RimQuest`(工坊原版;2026-08-28 起由本地 `local.hsk.questoverhaul` 任务大修HSK 接管,引用方一律改 loadAfter 它)、`Albion.GoExplore`、`DimonSever000.Events.Specific`。

### 3.2 大修段内顺序(固定)

```
HSK贸易重构(local.imperialcoin)
   └ loadAfter: HSK, ModIndicator
HSK工业与科研大修(ratkinpatch.HSKIndustrialResearchOverhaul)
   └ loadAfter: HSK, ModIndicator, local.imperialcoin(贸易), ResearchTreeSK, RimQuest,
                GoExplore, Cybranian Events
```
> 2026-08-27: 工业大修与科研大修已合并为 HSK工业与科研大修;段内顺序在 About.xml 显式化为 loadAfter 链(贸易→工业科研),不依赖 ModsConfig 手动顺序。

### 3.3 修复整合压尾规则

- HSK修复整合 `loadBefore`(用 packageId 不用文件夹名): 特性拓展 / 酒馆工具整合HSK / 帝国银币(HSK贸易重构)等被其覆盖的 mod。
- 任何新修复补丁并入修复整合时,确认不破坏上述段序。

---

### 3.4 汉化段规则(2026-08-27)

- AI 汉化包 `AITranslation.Pack` = 内容 mod 的**最后一位**(HSK修复整合之后、EndMod 组之前),保证翻译所依附的 def 已定稿且汉化优先级最高。
- 该 mod 的排序**唯一事实源是自身 About.loadAfter**(`local.hskfixpack` + `skyarkhangel.HSK` + `DimonSever000.ModIndicator.Specific`);Mod Assistant 的 `NativeAddon` 表条目只用于 HSK 标记着色,其 `loadAfter` 与 About 必须同向,否则建图成环。
- HSK 标记 = `Patches/07_ModAssistant标记.xml`(MayRequire 门控注入 `modIndicators/NativeAddon` + `HSKMod=true`),根目录另 `touch HSK` 空文件(发布约定,非生效依据)。
- 附属汉化包 `HSK.Mod.CHS`(1.6hsk附属mod汉化)由 HSK修复整合 loadAfter,保持在汉化段之前。

---

## 四、与 Workshop 第三方 mod 的关系

- 第三方 mod(CombatExtended / ResearchTreeSK / RimQuest / GoExplore / Cybranian / Processor Framework / Medieval Overhaul 等)按各自 About 排序,本地 mod 只声明 loadAfter 它们,不干预其内部顺序。
- 中世纪大修整合(若做)属于"我的大修"段之后的扩展,依赖 VEF + Processor Framework(见 `docs/中世纪大修内容梳理.md` §14)。

---

## 五、验证

- 改动 About.xml 后用 `ET.parse` 验证 XML 合法(未转义 `<tag>` 会整 mod 移出 ModsConfig)。
- 改加载顺序后按 `load-order-cycle-diag` 记忆的建图找环法核对,防止 loadBefore+loadAfter 成环。
  **口径**: `_tmp/cycle_all.py` 原生实现是"表条目存在即整体覆盖 About",会漏掉只写在 About 的边;稳妥做法是跑两遍——原样 + 把 `la/lb` 改成 `set(表边) | set(About边)` 的合并口径,两遍都 0 环才算干净(2026-08-27 汉化包标记即靠合并口径抓出 hskfixpack 反向 loadAfter 造成的 2-环)。
- 用 Patch 注入 `NativeAddon` 标记后,用 lxml 对 `ModListerSettingsDefs.xml` 做一次注入模拟(注意: 既有模板 `[defName="X"]` 属性式谓词在 lxml 下命中 0,模拟时换 `//AMA.ModListerSettingsDef[defName='X']`;真实游戏按 RimWorld 自身解析,与既有 20+ 个同款补丁一致)。
- 部署双目录同步后核对 SHA1。
