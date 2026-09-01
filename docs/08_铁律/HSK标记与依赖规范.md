# HSK 标记与依赖规范(HSK 整合)

> 来源: AGENTS.md §2.5(2026-08-26 实测)。给新本地 mod 加 HSK 标记、写 About.xml 依赖时必须依此,否则 ModsConfig 排序/兼容显示错乱。

## 1. HSK 标记的运行时判定
- = **`HSK Autosort and Mod Assistant`**(packageId `DimonSever000.ModIndicator.Specific`,DLL: 1.5 版 `ModIndicator.dll` / 1.6 版 `AutosortModAssistant.dll`)读取它自己的 `Defs/ModListerSettingsDef/ModListerSettingsDefs.xml` 映射表,按 packageId 给 mod 分类/排序/显示兼容性。
- `HSKMod=true` 或所属类型组(如 NativeAddon/EndMod)= "HSK 集成/适配 mod"标记。
- **判定不读取 mod 根目录的空 `HSK` 文件**(DLL 字符串无文件系统读取逻辑);根目录空 `HSK` 文件只是 Hardcore-SK 发布流程约定,保留无坏处但非生效依据。

## 2. 给新本地 mod 加 HSK 标记(三步,缺一不可)
1. 复制 mod 整文件夹到 `...\RimWorld\Mods`(保留 About/packageId/版本目录),根目录 `touch HSK` 建空文件(约定,可选)。
2. **在 mod 的 Patches 里加 ModAssistant 标记补丁**(NativeAddon 注入,模板见各 mod `*_ModAssistant标记.xml`/`99_ModAssistant标记.xml`):
   - `PatchOperationAdd MayRequire="DimonSever000.ModIndicator.Specific"`
   - xpath `Defs/AMA.ModListerSettingsDef[defName="ModListerSettingsDef"]/modIndicators/NativeAddon`
   - value `<li><id>本mod packageId</id></li>`
   - **MayRequire 门控写法沿用既有补丁(本环境 FindMod 不生效)**。
3. About.xml 补 `modDependencies`(真依赖: Harmony/Core SK/RatkinRaceHSK 等)+ `loadAfter`(被依赖 mod + `DimonSever000.ModIndicator.Specific` 压尾)+ 必要时 `loadBefore`(本 mod 修复/被其覆盖的 mod)。

## 3. 声明默认值
- 新增本地 mod 默认声明: `modDependencies` ≥ Harmony(`brrainz.harmony`) + Core SK(`skyarkhangel.HSK`);`loadAfter` ≥ 依赖项 + ModAssistant;鼠族系 mod 另依赖 `Solaris.RatkinRaceMod`(NewRatkinPlus/RatkinRaceHSK)。

## 4. EndMod 组(永远最后,由 Mod Assistant 排序)
- `trinity.runtimegcfixed` / `dubwise.dubsperformanceanalyzer.steam` / `mlie.wikirim` / `dimonsever000.wiki.specific` / `taranchuk.performanceoptimizer` / `krkr.rocketman`。
- Performance Optimizer 本地化 = workshop 完整版(含 1.6 DLL,与 1.5 适配版零代码差异)+ HSK 标记(表里已登记 EndMod+HSKMod=true)。

## 5. loadBefore 关键细节
- **用 packageId 不用文件夹名**(实例: 鼠族HSK拓展曾写 `PawnBadgeHSKFix` 应为 `saucypigeon.pawnbadge`)。
- 部署目录可能有历史别名(科技蓝图→`研究材料消耗`;RimHUD适配→`RimHUD`;工业+科研合并后旧 `hsk工业大修`/`研究材料消耗` 已删,统一为 `HSK工业科研大修`): 改动后按 packageId 找真实部署文件夹再同步。
- 同步铁律: About.xml/补丁文件逐文件复制(勿整目录覆盖,避免中文/空格路径 bash 循环拆词),复制后核对双目录 diff,防 `Mods/<mod>/<mod>/` 嵌套目录残留。
