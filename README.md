# hard-core-sk-HSK-Refinery

环世界(RimWorld)**个人向 HSK 整合包**——以 [HSK(Hardcore SK)] 生态为底,把鼠族相关内容与一批附属 mod 做数值/机制上的本地改造并串成一条线。仓库只收录**成品 mod 文件夹**和项目文档,**不含**工作脚本、临时产物和美术参考。

- 游戏版本:**1.6.4871**
- 生态:Core SK + RatkinRaceHSK + CombatExtended(HSK 版)+ ResearchTreeSK
- 定位:个人自用,数值/机制按自己口味调整,**不保证平衡,不保证与最新工坊版兼容**

## 这是什么 / 不是什么

| 收录 | 不收录 |
|---|---|
| 52 个完整 mod 文件夹(可直接放进 `Mods/`) | HSK 本体框架(`Core_SK`、`RatkinRaceHSK`、`CombatExtended`、`ResearchTreeSK` 等——需另装) |
| 项目文档 `AGENTS.md`、`docs/` | 工作脚本 / 中间产物(`_tmp/`、`outputs/`) |
| mod 内的 C# 源码(`Source/*.cs`)与编译好的 `Assemblies/*.dll` | 只读美术参考(`参考素材/`,约 3.4G) |
| | 编译垃圾(`Source/obj`、`.vs`)、agent 内部目录(`.qoder`、`.workbuddy`) |

## 怎么用

1. 先按官方 HSK 套件装好下面的**运行前置**,并用 HSK Autosort 排序。
2. 把本仓库里需要的 mod 文件夹**整个**复制进 `RimWorld/Mods/`。
3. 回游戏用 HSK Autosort 重排一次即可(部分 mod 的 `About.xml` 已写死 `loadAfter`/`loadBefore` 依赖边)。
4. A/B 类是本仓库的核心;**C 类只是让整合跑起来的第三方前置,想更新请直接回 Steam 工坊订阅最新版**。

## 运行前置(HSK 本体,本仓库不含,需另装并排在最前)

| Mod | packageId | 作用 |
|---|---|---|
| Core SK | `skyarkhangel.HSK` | HSK 核心(材料/科研/建筑体系) |
| RatkinRaceHSK | `Solaris.RatkinRaceMod` 系 | 鼠族种族基类 |
| CombatExtended(HSK 版) | `cete.combatextended` | 战斗/弹药框架 |
| ResearchTreeSK | — | 科研树/研究台六档 |
| HSK Autosort & Mod Assistant | `DimSever000.ModIndicator.Specific` | 自动排序 + HSK 标记 |

---

## Mods 清单(按"能不能直接下过来用"分三类)

### A. 本仓库自制 —— 源头就在这里,下载即可用

| Mod 文件夹 | packageId | 内容 |
|---|---|---|
| 鼠族HSK拓展 | `local.ratkin.clothesweapons` | **主内容**:鼠族武器/服装共 147 件(鼠 89 + 魅 44 + 追 14),配方/科研/CE/文化全适配 |
| 鼠族家具拓展 | `local.ratkin.furniture` | 自制鼠族家具建造线 |
| HSK工业科研大修 | `ratkinpatch.HSKIndustrialResearchOverhaul` | 工业与科研节点大修(石化链、材料科技档归位等) |
| HSK贸易重构 | `local.imperialcoin` | 贸易 / 货币体系重构 |
| HSK修复整合 | `local.hskfixpack` | 各类 HSK 兼容性修复总包(基于 PawnBadge / RimHUD 等) |
| 任务大修HSK | `local.hsk.questoverhaul` | 任务(Quest)系统大修 |
| 污染统一 | `ratkinpatch.PollutionUnify` | 污染机制统一 |
| 近战挥砍动画HSK | `local.ratkin.melee.swing` | 自制近战挥砍动画 |
| 酒馆工具整合HSK | `local.hsktavernintegration` | 酒馆 / 工具整合 |
| Reel存储HSK适配 | `local.reelstorage.hskfix` | Reel 存储 mod 的 HSK 适配 |
| 特性拓展modHSK | `vanillaexpanded.vanillatraitsexpanded` | 特性拓展的本地整合版 |

### B. 本地改造 / HSK 适配版 —— 原作来自工坊,但本仓库针对 HSK 深度改写,**用这份即可**(再用会与原作者版冲突)

| Mod 文件夹 | packageId | 原作作者 |
|---|---|---|
| 金鼠族 HSK版本 | `local.ratkin.goldenhsk` | M.Y.G |
| AFU发型HSK适配 | `local.ratkin.afu.hairstyles` | 金兔子拉面 |
| Gloomy发型HSK适配 | `Vinzero.GloomyHairUP` | Gloomylynx / Vinzero |
| 美狐HSK拓展 | `miho.fortifiedoutremer` | Outremer, Fortified_Home |
| 库林战狐HSK拓展 | `Seioch.Kurin.HAR` | Seioch 等 |
| 书籍拓展HSK | `VanillaExpanded.VBooksE` | Oskar Potocki, Taranchuk |
| 原版文化拓展-树妖 | `VanillaExpanded.Ideo.Dryads` | VE 团队 |
| 原版文化拓展-迷因与建筑 | `VanillaExpanded.VMemesE` | VE 团队 |
| 近战动画HSK | `co.uk.epicguru.meleeanimation` | Epicguru |
| HairModdingPlusHSK | `Butterfish.HairModdingPlus` | Butterfish 等 |
| Vanilla Skills Expanded | `vanillaexpanded.skills` | VE 团队 |
| 盔甲架HSK适配 | `khamenman.armorracks` | khamenman |
| PawnBadgeHSKFix | `saucypigeon.pawnbadge` | SaucyPigeon |
| RimHUD适配 | `Jaxe.RimHUD` | Jaxe |
| 1.6HSK核心汉化 | `HSK.Core.CHS` | 边缘汉化组 |
| 1.6hsk附属mod汉化 | `HSK.Mod.CHS` | 边缘汉化组 |
| VSIE中文包 | `RWZH.ChinesePack.VSIE` | leafzxg |

### C. 第三方前置 / 框架 —— 工坊原样(或仅微调)搬运,只为跑通依赖保留;**建议直接回工坊装最新版**

| Mod 文件夹 | packageId | 作者 |
|---|---|---|
| Oberonia Aurea Framework | `OARK.OberoniaAurea.Framework` | OARK |
| XML Extensions | `imranfish.xmlextensions` | Imranfish |
| Apparel Paper Pattern | `nalsnoir.ApparelPaperPattern` | NALS |
| Facial Animation - WIP | `Nals.FacialAnimation` | Nals |
| Facial Animation - Experimentals | `Nals.FacialAnimation` | Nals |
| Facial Animation HSK Patch | `pacas.faceanim.hsk` | Pacas |
| Material Numbers | `materialnumbers.core` | 社区 |
| Minimal Light Control | `kapitanoczywisty.minimallightcontrol` | Kapitan Oczywisty |
| Change Map Edge Limit | `kapitanoczywisty.changemapedge` | Kapitan Oczywisty |
| Move Steam Geyser | `LingLuo.MoveSteamGeyser` | LingLuo |
| Music Manager Continued | `zal.musicmanager` | Zaljerem |
| P-Music | `Peppsen.PMusic` | Peppsen |
| QualityBuilder | `hatti.qualitybuilder` | Hatti |
| Stack XXL | `Indeed.StackXXL` | Indeed |
| Smart Farming | `Owlchemist.SmartFarming` | Owlchemist |
| WealthCorrector | `cn.zhuzijun.WealthCorrector` | 竹子菌 |
| [sbz] Neat Storage | `sbz.NeatStorage` | seobongzu |
| Trait Rarity Colors | `CarnySenpai.TraitRarityColors` | Carny Senpai |
| Ratkin Body Retextured | `funamusea.RatkinBodyRetextured` | Crazy Copepod |
| RatkinBackStoryExpandedHSK | `DARKkai.RatkinBackStoryExpanded.HSK` | DARKkai |
| Vanilla Social Interactions Expanded | `VanillaExpanded.VSIE` | VE 团队 |
| 边境拓展HSK | `NehsModsForDev.bordersoftherim` | NehsModsForDev |
| 工作动画HSK | `meathax.ShowMeYourTools` | astryl |
| 房间统计数据染色 | `QW.ColoredRoomStats` | 苍白而蔷薇 |

---

## 关于加载顺序 / 兼容

- 排序交给 HSK Autosort;个别 mod(如"HSK修复整合")在 `About.xml` 里钉死了对 Core SK / ModIndicator 的 `loadAfter`,不要手改位次。
- 所有改造**不考虑旧存档兼容**:升级后请开新档。
- 更多机制、踩坑与逐项细则见仓库内 `AGENTS.md` 与 `docs/`(尤其它对 CE 适配、科研门禁、汉化、贴图铁律的说明)。

> B/C 类 mod 的版权与原始设计归属各自原作者,本仓库仅做本地适配与整合,不替代官方发布。
