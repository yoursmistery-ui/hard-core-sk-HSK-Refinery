# hard-core-sk-HSK-Refinery

个人只用整合包,整合环世界 HSK 及附属相关 mod,个人进行数值和机制上 简单调整整合。

> 本仓库只收录**完整 mod 文件夹**与项目文档(`AGENTS.md`、`docs/`)。
> 工作区临时产物(`_tmp/`、`outputs/`)、只读美术参考(`参考素材/`)、agent 内部目录(`.qoder/`、`.workbuddy/`)均**不进仓库**。

## 运行前置(HSK 本体,本仓库不含,需另行安装)

这些 mod 都跑在 HSK 生态上,单独下本仓库**不能直接开游戏**,需先从 HSK 套件装好以下本体并排在其后加载:

- `Core_SK`(skyarkhangel.HSK) — HSK 核心
- `RatkinRaceHSK` — 鼠族种族基类
- `CombatExtended`(HSK 兼容版) — 战斗框架
- `ResearchTreeSK` — 科研树框架
- `HSK Autosort and Mod Assistant`(`DimSever000.ModIndicator.Specific`) — 自动排序 / HSK 标记

---

##  Mods 清单(按"能不能直接下过来用"区分)

### A. 自制 / 本仓库原创 —— 下载即可用,本仓库就是这些 mod 的源头

| Mod 文件夹 | packageId | 说明 |
|---|---|---|
| 鼠族HSK拓展 | `local.ratkin.clothesweapons` | 主内容:鼠族武器/服装 147 件全适配(配方/研究/CE/文化) |
| 鼠族家具拓展 | `local.ratkin.furniture` | 自制家具线 |
| HSK工业科研大修 | `ratkinpatch.HSKIndustrialResearchOverhaul` | 工业/科研节点大修 |
| HSK贸易重构 | `local.imperialcoin` | 贸易/货币重构 |
| HSK修复整合 | `local.hskfixpack` | 各类 HSK 兼容性修复总包 |
| 任务大修HSK | `local.hsk.questoverhaul` | 任务系统大修 |
| 污染统一 | `ratkinpatch.PollutionUnify` | 污染机制统一 |
| 近战挥砍动画HSK | `local.ratkin.melee.swing` | 自制近战挥砍动画 |
| 酒馆工具整合HSK | `local.hsktavernintegration` | 酒馆/工具整合 |
| Reel存储HSK适配 | `local.reelstorage.hskfix` | Reel 存储 HSK 适配 |
| 特性拓展modHSK | `vanillaexpanded.vanillatraitsexpanded` | 特性拓展本地整合 |

### B. 本地改造 / HSK 适配版 —— 原作出自工坊,但为本仓库针对 HSK 深度改写,用本仓库这份即可

| Mod 文件夹 | packageId | 原作者 |
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

### C. 第三方前置 / 框架 —— 工坊原样(或仅微调)搬运,仅为跑通依赖保留;**建议直接装 Steam 工坊最新版**

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

**用法**:把整个 mod 文件夹放进 `RimWorld/Mods/`,开游戏后用 HSK Autosort 排序即可。A/B 类是本仓库的价值所在;C 类只是让整合跑得起来,想要最新版请回工坊订阅。数值/机制均为个人向调整,不保证平衡。
