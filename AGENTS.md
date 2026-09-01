# AGENTS.md — 鼠族HSK拓展(RimWorld 1.6 + Core SK)

> 原则铁律版(2026-08-27)。**必守规则完整保留于此;历史语境/实例细节/完整清单在 `docs/维护详录_鼠族HSK拓展.md`**,动手前先读对应章节。

## 0. 统一入口
- **查询型文档入口**: `docs/00_HSK参考文档合集导航.md`(科研/建筑/生产/材料/文化/弹药/排查/铁律八类,含自动生成文档刷新命令)。
- **所有冗长详录/历史语境**: `docs/维护详录_鼠族HSK拓展.md`(§3 关键坑、§6 CE 适配、§7 补丁职责等)。
- **加载顺序与依赖总表**: `docs/00_mod加载顺序与依赖.md`(改 About 依赖前必查,防循环依赖)。

---

## 1. 项目与双目录铁律
- **修复不考虑旧存档兼容性(2026-08-30 用户要求)**: 改 def/补丁/DLL 时**不要为兼容旧存档而妥协正确做法**——不必保留悬空 defName、不必做存量建筑/账单迁移、不必为旧存档数据加运行时防御;存档坏了直接开新档。只有用户明确要求保留的(如保留 defName 仅为账单显示)才例外并注明。
- Mod: `local.ratkin.clothesweapons`;147 件(鼠 89+维 44+追 14),配方/研究/CE/文化全适配。
- 环境: 1.6.4871 + Core_SK + RatkinRaceHSK + CE + Vile's MS + ResearchTreeSK。
- **双目录同步(底线要求)**: 工作区 `C:\Personal\Project\ratkin-patch` ↔ 部署 `...\RimWorld\Mods\鼠族HSK拓展`。**改动必须双份同步,否则游戏不生效。** ⚠️ **更新完工作区必须立刻同步到部署 Mods 目录,这是底线要求,不允许滞后/遗漏。** 同步时逐文件复制(勿整目录覆盖,防中文/空格路径在 bash 循环拆词;用 Python glob+shutil 或 find -print0),复制后核对双目录 diff,防 `Mods/<mod>/<mod>/` 嵌套目录残留。
- **交付即同步, 不启动游戏扫日志(2026-08-31 用户明确要求)**: 改完 def/补丁/DLL/贴图, **同步双目录(必要时 Steam 冷启动重建缓存)即算交付完成, 效果由用户进游戏自行查看并反馈。** 🚫 **严禁做"自动启动游戏 → 等加载 → 反复 grep/扫描 Player.log 验证报错"这个循环**——用户已明确否掉这种收尾方式, 每次启动+扫描既慢又打扰。需要静态核对就用 `Unified.xml`/终态 def 文件/自写脚本, 不靠开游戏。仅当用户**明确说"帮我启动/验证"**时才可启动游戏。
- **工作区根目录只放完整 mod 文件夹**(便于整文件夹同步);临时文件/中间产物一律进 `_tmp/`。已认可的非 mod 根目录例外: `docs/`、`_tmp/`、`.qoder/`、**`参考素材/`**(官方美术源参考树,只读素材,**绝不可进同步范围**,详见 §4)。
- **部署目录可能有历史别名**(科技蓝图→`研究材料消耗`、RimHUD适配→`RimHUD`): 按 packageId 找真实部署文件夹再同步。
- **单文件 ≤ 10KB 铁律**: 任何文档/说明类文件不得超过 10KB;超出部分外挂到 `docs/` 下的独立文档(主文件留指针)。代码/XML 以功能为准,但过长的详录/方案同样按主题拆分外挂。
- **没做完的东西不写进去(2026-08-30 用户要求)**: 交付文档/`docs/00_HSK参考文档合集导航.md` 只收录**已自验通过**的成果——先跑完核对(链接与锚点可达、体积达标、覆盖率与缺项清单已消化或明确降级为"参考"),再登记导航;生成中途的待办/占位/未核实条目一律留在 `_tmp/` 核对脚本输出里,不进正文、不进导航。文档正文里也不得混入"未标注/缺数据"的伪结论,缺失项要么补齐、要么显式标注为参考对照(如非同类目条目另立"参考:"分组),不得与已核实内容混排冒充完成品。
- **工坊 mod 一律本地定位,禁联网抓 steam.com**: 用户给的 `steamcommunity.com/.../?id=<数字>` 链接**只是提供工坊 ID**,须到本地 `steamapps\workshop\content\294100\<id>` 找该 mod 再取资源(贴图/def/参考),不要用 WebFetch/WebSearch 抓 steam 页面。

## 2. 加载顺序
- Core_SK → RatkinRaceHSK(HSK_Generated 整体替换鼠族基类,基类自带 recipeMaker 自动配方)→ 本 mod(最末)。旧"自动配方"=基类 recipeMaker 产物,须最终层补丁关闭。
- **HSK 标记判定**: 由 `HSK Autosort and Mod Assistant`(`DimonSever000.ModIndicator.Specific`)读它自己的 ModListerSettingsDefs.xml 映射表判定,**不读取 mod 根目录空 `HSK` 文件**(那只是发布流程约定,保留无害但非生效依据)。加标记三步法见详录 §2.2。
- **给新本地 mod 加 HSK 标记(三步)**: ①整文件夹复制到 Mods + 根目录 touch HSK(可选);②Patches 加 ModAssistant 标记补丁(NativeAddon 注入,MayRequire 门控沿用既有补丁);③About.xml 补 modDependencies(Harmony/Core SK/RatkinRaceHSK)+ loadAfter(被依赖 mod + ModIndicator 压尾)+ 必要时 loadBefore。新 mod 默认声明: modDependencies ≥ Harmony(brrainz.harmony)+Core SK(skyarkhangel.HSK);loadAfter ≥ 依赖项+ModIndicator;鼠族系另依赖 `Solaris.RatkinRaceMod`。
- **⚠️ 框架类 mod 不得加 loadAfter HSK 链(2026-08-29 铁律)**: ModAssistant 自动排序=按映射组 order 稳定排序(`ModTypeDefs.xml`: CompatibleFramework=3 < Core SK 所在 DefaultRequired=4),组内顺序沿用现状。若 mod 被映射表归入前排框架组(**Facial Animation ×2 / Apparel Paper Pattern / XML Extensions** 等 CompatibleFramework 成员),About 加 `loadAfter HSK 链/ModIndicator` 后该声明**永远无法满足** → 挂红❗"必须在这些mod之后加载"且自动排序永远修不掉(组序锁死其在 Core SK 前)。这类 mod 的排序完全交给映射表条目自带的运行时边(如 FA: loadAfter CE、loadBefore HAR);About 的 modDependencies 可保留(不参与排序)。实例: 08-27 标记会话给这 4 个框架 mod 加了尾巴,08-29 全部删除。**给 mod 加尾巴前先查映射表组序**。
- **⚠️ 只为"跨 mod defName 引用"加 loadAfter = 成环陷阱(2026-08-29 铁律)**: 跨 mod 引用(派系/物品/材料名)在**全部 mod 加载完成后统一解析**,不需要排序边;只有 ①本 mod 用 Patch 改对方 def ②同名 def 覆盖(后者胜出)才必须 loadAfter。实例: 任务大修HSK 因族色委托引用美狐 def 加了 `loadAfter miho`,而 美狐 loadAfter 工业大修(覆盖 `Miho_CelestialScale` 等材料)、工业大修 loadAfter 任务大修(给 RimQuest 的 `Reward_ItemsStandard` 加蓝图选项)→ 3-环。表现: 自动排序弹"与**盔甲架**HSK适配产生了循环依赖"(RimWorld `DirectedAcyclicGraph.FindCycle` 返回的是 **DFS 入口 mod**,不是环上成员,别据此追因),且红❗文案 `ModOrderingWarning` 由 `ModsConfig.ModHasAnyOrderingIssues` 按**当前 ModsConfig 位次**判定。修法=删掉那条纯引用边。**加边前先自问"我 patch/覆盖了它的 def 吗"**,并用 `_tmp/loadorder/find_cycle.py`(Tarjan 找环,只算 loadAfter/loadBefore——`modDependencies` 不参与排序,勿误判成环)+ `_tmp/loadorder/check_order2.py`(复刻 ModHasAnyOrderingIssues 核当前位次违规)各跑一遍。
- **手工编辑 `ModsConfig.xml` 操作法(用于测试直接启用 mod)**: 真实路径 `%LocalLow%\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml`。游戏退出时会整体回写该文件,**改前必须先 `Stop-Process RimWorldWin64 -Force`**(否则白改)。格式勿重排:UTF-8 无 BOM + CRLF,结构 `<ModsConfigData><activeMods><li>`。`SamePackageId` 不区分大小写,追加前先 `Counter(p.lower())` 查重防"同 ID 已启用",并用 `_tmp/loadorder/check_order2.py` 核位次(勿把内容 mod 落到 EndMod 组之后)。改完同步双目录即可,**不自动启动游戏扫日志**(见 §1 交付铁律)。
- **loadBefore 用 packageId 不用文件夹名**(实例: 曾写 `PawnBadgeHSKFix` 应为 `saucypigeon.pawnbadge`)。
- **EndMod 组(永远最后)**: runtimegcfixed/dubsperformanceanalyzer/wikirim/wiki.specific/performanceoptimizer/rocketman。**HSK修复整合(local.hskfixpack)钉在该组之前**(About 已 loadAfter 全部非 EndMod **内容** mod + loadBefore 全 EndMod 成员,含 mothballedanddeadpawns/missilegirl;汉化包除外,见下);新增 mod 后若排序落在其前,须同步补 loadAfter。**汉化段**: `AITranslation.Pack`(1.6HSK核心全ai汉化)= 内容 mod 最后一位,**紧跟 HSK修复整合之后、EndMod 之前**(汉化 About loadAfter `local.hskfixpack` + Core SK + ModIndicator;修复整合 About **不得**反向 loadAfter 汉化,否则 2-环;细则见 `docs/00_mod加载顺序与依赖.md` §3.4)。

---

## 3. 补丁编写铁律(PatchOperation)
- **关自动配方**: Replace recipeMaker 为 IsNull **不生效**,须 Add `<recipeMaker Inherit="False" IsNull="True"/>` 到 ThingDef(12 号: 先条件 Remove 再 Add)。
- **改自动配方工作台**: 整体 Replace/Add recipeMaker + `Inherit="false"`(深度合并,否则 recipeUsers 合并挂两台)。
- PatchOperation 操作**原始 XML,不解析继承**: 子类无自有节点时 xpath 报 "Failed to find a node",须打基类 `[Name="XXXBase"]` 或 Add 到 ThingDef 自身。
- **xpath 不支持 union(`A|B`)**,须拆多 Operation;Conditional 的 nomatch Add 目标必须存在。
- **补丁执行顺序=文件名(字符串)序,`1xx` 是陷阱编号**: `105_` 首字符 '1' < '3'/'5'/'9',实际排在 11/32/52/91 号**之前**。**永远不要把 1xx 当"最终层补丁"**(引用它后 Add 的节点时目标尚不存在,Replace 全报 Failed)。要"最后执行"用 >91 的两位数编号或并入 92 号末尾;引用其它补丁 Add 的节点须验证目标补丁文件名序在自身之前。
- **1.6 起 Operation 级 `MayRequire` 已失效**(反编译确认: PatchOperation 无该字段,Apply/Conditional 均不检查)→ 未装目标 mod 时操作仍执行报错。**根治: 一律用 xpath 存在性门控**(外层 PatchOperationConditional 检查目标 def 节点是否存在)或 PatchOperationFindMod。注意 Def 节点级 MayRequire(如 `<li MayRequire="Ludeon.RimWorld.Biotech">`)仍有效,勿删。
- **禁用 FindMod+Sequence 嵌套**(1.6 实测不生效)→ **一律顶层独立 Operation**: 原版职位用顶层 Conditional(xpath 存在性,match=Replace/nomatch=Add);VME/可选 mod 职位用顶层 Conditional + match=Sequence[内层 Conditional];可选内容用顶层 Conditional(xpath=该 mod 特有 def 存在门控)+Sequence。
- **改补丁后必须用 `_tmp/patch_simulator.py`(lxml)模拟执行验证**(lxml 与 .NET 语义差异: xpath 无前导 / 时统一加 /;Sequence 子元素 tag 是 li;Conditional/FindMod 的 match 是单个 Operation 整体递归)。
- **按主题合并/精简补丁(减少 xml 数)安全法**: ①**只搬不改**——逐字抽取每个源文件 `<Patch>` inner 按原文件名序拼接成新 `<Patch>`,绝不 reformat 操作内容;②新文件命名占据"组内首个源"的排序位,且**只做连续段切分**(不跨段移动某操作到另一段),保证全局执行顺序逐一不变;③若想把不相交主题各自合并成一块,须先证两组触及的 def **零交集**(`indep_check` 式 token 交集为空)才能相对任意重排;④合并后跑 `_tmp/merge_equiv.py` 把"原始集 vs 合并集"应用到同一 pre-patch 基线树,diff 最终 def 状态须 0 差异,并对合并文件 inner 做注释/空白归一后与源拼接做字节相等核对。工具类示例见 §7.8。
- **RecipeDef 同挂 researchPrerequisite+researchPrerequisites 告警**: 继承 BaseMakeableGun 的枪若子类另写复数前置列表,深度合并后单个 Gunsmithing 仍被继承保留 → 两字段并存报错。**无法用 xpath 删继承节点**,根治: 在子类 recipeMaker 内 Add 空节点 `<researchPrerequisite Inherit="false" />` 屏蔽继承的单个前置,复数列表保留(多头科研必须用列表)。
- **ResearchTreeSK 重复解锁**: 同一无门槛配方挂 2 个以上同研究解锁的工作台 → 启动报 "duplicate unlocked defs"。根治: 给配方补显式 `<researchPrerequisite>`(被工作台遍历跳过)或从其中一个工作台删该配方/recipeUsers 条目。验证以游戏日志为准(Unified.xml 可能含 DefOverwrite 双份误报)。
- **类名可见性铁律**: 补丁引用跨 mod 自定义类(如 `PawnRenderNodeProperties_EarHideByApparelTag` 在 NewRatkin.dll)只要目标 mod 已加载即可全局解析;但依赖该类的补丁**必须 loadAfter 该 mod**(已 loadAfter `Solaris.RatkinRaceMod`)。
- **异种人池 / "智人种"显示铁律**(2026-08-27 反编译 `RimWorld.XenotypeSet.get_BaselinerChance` 定论): 派系/添加派系界面那条"智人种 X%"是 **`1 − Σ(池内权重)` 的余量固定显示行**;池合计恰为 1.000 时**恒显示 0%,不代表生成了智人种**,严禁据此追因补基因(详录 `docs/05_文化服饰异种/鼠族异种人白名单整理.md` §6.7)。真会漏智人种只有三种: ①池合计 < 1;②条目引用不存在的 XenotypeDef(权重落空,例 `OAGene_WhiteRatkin` 系列由未安装的 `[OA]Ratkin Gene Expand` 提供);③派系**没有** `xenotypeSet`(美狐即如此 → `美狐HSK拓展/1.6/Patches/98b_美狐派系异种人池.xml`)。另: `whiteGeneListEndo` 会被 `onlyHaveRaceRestrictedGenesEndo` 用来校验**异种人自带的 genes**,从中删基因会让自带该基因的异种人整族判非法 → 回退智人种(实例: 08-26 删 `RK_Gene_LargeEars`/`RK_Gene_ThinTail` 把 0.80~0.86 权重的初鼠族打成智人种)。核对最终态用游戏导出的 `Mods/Unified.xml`(**UTF-16**,与 `MissileGirl/Cache/` 下那份 UTF-8+BOM 编码不同)。

- **整合 mod 内容落位法(能直建就不进工作台)**:带 `costList`/`costStuffCount` 的可建造物件(家具/地毯/装饰)一律走建筑菜单直接建造,**不保留同名工作台配方**(双路径冗余且挤占账单);只有需要工时·技能·混合材料的中间材料配方才留工作台,并挂到 HSK 共享对应台(纺织类统一 `TableLoom` + `workSpeedStat=TailoringSpeed` + 与该台解锁同档的 `researchPrerequisite`)。删台须同步删 WorkGiver、SubCategories 条目、汉化键(悬空 DefInjected 键指向已删 def)。实例见详录 §7.12。**注**:"能直建"只意味着删除工作台配方,**不得顺手改物件造价**(材料构成由用户定,别自作主张简化)。
- **种族穿戴锁 = AlienRaces `raceRestriction.apparelList` 是"全局独占白名单"**: 某衣物只要出现在任一种族的 apparelList,就进全局 `apparelRestricted` 集,**只有列了它的种族能穿**(未列/无 raceRestriction 的种族一律 false,随机生成同步剔除)。∴ ①"把它加进本种族列表"= 锁给本种族,**不是**给本种族开洞兼容(旧 §1.1 西风装甲据此写反过);②要给别的种族也能穿,须在**每个**允许种族里都列;③引用只在 `IfModActive` 目录里定义的 def(如 Odyssey 腰带),`<li>` 必须写 Def 级 `MayRequire`,否则缺 DLC 时 cross-ref 报错。细则+验证脚本见详录 §7.11。

---

## 4. 汉化与贴图铁律

- **DefInjected 根节点必须 `<LanguageData>`**,defName 作**节点名**(`<ThingDef><X><label>..</label></X></ThingDef>`);写成 `<Defs>`+`<defName>值</defName>` 会被**静默跳过、日志无报错**。
- **DefInjected 全不加载的兜底**: 本地整合 mod 汉化失效时,把中文 label/description 直接注入 Defs 文件(`_tmp/酒馆汉化修复/inject_labels.py`),100% 生效。
- About.xml/description **不能含未转义 `<tag>`**(整 mod 被静默移出 ModsConfig);写完 ET.parse 验证。
- **统一贴图方向命名**: 机制A渲染的耳朵贴图需 `_east/_north/_south/_west` 四方向;耳朵 `_west`=`_east` 水平镜像。特殊异种耳贴图原缺 `_west`(机制B下无此需),改走机制A后必须补全。
- **贴图尺寸基准固定 256ppt(2026-08-31 用户要求)**: 单格物件(物品/武器/服装/头饰/1×1 建筑/植物)一律 **256×256**,多格按 `drawSize×256`(原版床 192×256 → 我们 384×512),墙/导管 atlas 1280×1280(5×5@256);**地形/草地/fleck 例外**,沿用原版 1024 档勿拉 256(同屏海量重复,显存 4 倍)。判读按**半尺寸 128 预览**仍一眼可辨才算合格;描边 8px、最小笔画 6px(换算细则见 skill `comfyui-texture` §2)。
- **官方美术源参考树(2026-08-31 落地)**: `参考素材/LudeonArtSource/<DLC>/<原版路径>.png`(8874 张/328MB,含 2941 个 PSD 拼合转出),目录层级与游戏 `texPath` 一致,**做贴图前必看原版同类、禁止凭空画**。⚠️ 该目录**不是 mod 内容,任何同步脚本必须排除它**,勿进 Mods 双目录同步。
- **HSK 部署贴图参考树(2026-08-31 落地)**: `参考素材/HSK/<mod名>/<mod内原相对路径>`,1:1 镜像部署 Mods 全部非 .disabled mod 的 `Textures` 图片(33130 张/1.5GB,含 `1.6/`、`Biotech/`、`Mods/<其他mod>/` 等版本与兼容子目录),与 LudeonArtSource 并列。清单+体量在 `参考素材/HSK/_清单.txt`,重新生成脚本 `_tmp/参考素材HSK整理/copy_mod_textures.py`(mod 增删后重跑即增量;从 Mods 移除的 mod 残留目录需手动删)。**主题分类检索树(2026-08-31 三次整理,含官方原版)**: `参考素材/` 根目录下直接是分类文件夹,**无 mod 层**,按 HSK 部署贴图 + LudeonArtSource 官方六包(RimWorld/Biotech/Ideology/Anomaly/Odyssey/Royalty)合并归类;同名同路径不同内容时 mod 侧加 `<mod>__`、官方侧加 `<包名>__`(如 `RimWorld__`)前缀双保留:武器(含弹药)/衣物(鼠族·人类)/建筑/物品(原料矿产)/原料(提炼成品金属锭)/化工产品/原料/食物/植物/生物(种族)/UI(HeroArt 计入),共约 3.4 万张/1.4GB;鼠族衣物按 鼠族HSK拓展/RatkinRaceHSK/金鼠族/Ratkin Body Retextured 四 mod 特判;地形/特效/世界/废案不收;分类索引 `参考素材/_清单.txt`,规则脚本 `_tmp/参考素材HSK整理/classify_textures.py`(先 --dry-run 看落量再实拷)。**群峦 TFC 素材(2026-08-31 落地)**: 源 `C:\Users\admin\Doubao\chats\2026-08-30\new-chat\TFFH_assets`(Minecraft 群峦包,833 张/3.2MB)拷入 `<分类>/群峦TFC/<原相对路径>` 用命名空间隔开异种画风;矿石/宝石(富_孔雀石/富_自然金/冰晶石等)在 物品(原料矿产),金属锭/板/棒/双锭在 原料(提炼成品金属锭),工具头在 武器,模具/坩埚/加热设备方块在 建筑,其余 燃料→原料、GUI界面→UI;预览拼图与根目录 jar/zip/md 不收;脚本 `_tmp/参考素材HSK整理/copy_tfc_textures.py`。参考素材下所有内容同样**绝不进同步范围**。

---

## 5. DLL / Harmony 铁律
- **Assemblies 目录禁止遗留同名测试 DLL**: RimWorld 会加载该目录下**全部 .dll**,残留调试产物(`.new`/`.p5`/`.bak`/`.p10` 等,assembly 名与真 DLL 相同)→ 同名装配体类型冲突 → 角色信息页空白 + `Mouse position stack is not empty / BeginScrollView > EndScrollView` 滚动栈泄漏。**预防: 只在工作区用临时文件名产出并部署单份;部署目录 Assemblies 只保留正式 dll,残留一律清掉。** 排查"某 mod 在场才 GUI 泄漏"时第一件事查其 Assemblies 有无同名单残留。
- **Harmony 声明式补丁重载歧义**: `[HarmonyPatch(typeof(X), "方法名")]` 不指定参数类型时,目标方法若有**多个重载** → `AmbiguousMatchException` 且**连带同程序集整个 PatchAll 崩**。**根治: 只 patch 无重载方法,或用 `new Type[]{...}` 指定参数类型。**
- **Mono.Cecil patch 铁律**: 分支失败路径若栈上残留 `dup` 的数组副本,ret 时栈不平衡 → `InvalidProgramException: blt IL_0018`。**方法体 stub 必须以 ret 结尾**(旧布局 ret+nop 被 JIT 编译即 InvalidProgramException)。栈平衡必须逐路径模拟验证。
- **GameComponent 子类须写 `public X(Game game)`**;改补丁后清 MissileGirl XML 缓存。
- **stuffable 建筑(有 `stuffCategories`+`costStuffCount`)必须写 `uiIconPath`**:缺失时点选该建筑,`Widgets.ThingIcon` 在 `Designator_Build.DrawPlaceMouseAttachments`(经 ArchitectSense `Designator_SubCategoryItem`)因 stuff 为空(如"储量不足")**每帧抛 NullReferenceException → 表现为"一点就卡死"**,日志是 `Root level exception in OnGUI()` 无限重复。移植任何可建造建筑都先补上。
- **`uiIconPath` 必须指向真实存在的单张 png**:文件夹式贴图(Graphic_Multi/Random)写裸 `texPath` 取不到(`Could not load Texture2D at '.../Kiln/Kiln'`),要写带方向的具体文件如 `.../Kiln/Kiln_south` 或 `..._MenuIcon`;补完用脚本核 `Textures/<path>.png` 是否存在。
- **抽象基类 `Name=` 在本 mod 内必须全局唯一**:重名会报 `Could not register node named "X" ... already used in this mod`,**后注册失败的那份文件里所有 def 全部不加载**,连带 KCSG SymbolDef / recipeUsers 等交叉引用成片失败(实例: Decor 文件复用 `Rustic_LootBuildingBase` 导致 10 个搜刮容器全灭)。新增抽象基类前先 grep 全 `Defs` 目录确认名字未被占用。

---

## 6. 研究台六档(requiredResearchBuilding,与 HSK 原版一致)
按时代挂门槛,对应 Core_SK 六档抽象基类:
- **原始(Neolithic)** → `PrimitiveResearchBench` 原始科技研究台(PrimitiveBase)
- **中世纪(Medieval)** → `SimpleResearchBench` 基础研究台(MedievalBase)
- **前工业(电力→石化前)** → `LabTerminal` 研究终端(IndustrialBase=基础+终端设施)
- **后工业(石化→太空前)** → `HiTechResearchBench` 高级研究台(HitechBase=高级+终端设施)
- **太空(Spacer)** → `MultiAnalyzer` 多元分析仪(HitechMultiBase)
- **极致(Ultra)** → `LabStation` 实验室工作站(HitechLabStationBase)

**机制坑(反编译确认)**: 光写 `requiredResearchBuilding` 不够——Core_SK 的 Patch_ResearchProjectDef_CanBeResearchedAt 以 `SK.AdvancedResearchExtension.requiredResearchBuildings`(DefModExtension)列表为**硬门槛**,研究台不在列表内即拒绝。**写法**: 研究节点 Add `<requiredResearchBuilding>X</requiredResearchBuilding>` + `<modExtensions Inherit="False">` 重写 SK 扩展列表(保留 `<li Class="ResearchTreeSK.ResearchTreeSKModExtension" />`);设施走原版 `requiredResearchFacilities`(LabTerminal/MultiAnalyzer/LabStation)。

---

## 7. HSK CE 适配铁律(近战/远程/装备,判定与原版 CE 不同)

> 本环境 CE 是 **CombatExtended.HSK.dll**(HSK 兼容层)。CE 目录: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\CombatExtended`(CombatExtended.dll=主体,CombatExtended.HSK.dll=策略层)。完整细则/反编译定位法见详录 §6 与 `docs/08_铁律/CE适配铁律.md`。

- **近战**: tools 全部 `<li Class="CombatExtended.ToolCE">`(原版 Tool 会被过滤 → info 卡报 "no support for Combat Extended"),字段含 `armorPenetrationSharp`/`armorPenetrationBlunt`(单位 mm,基准值就是这两个,材质/品质再乘系数)。**CE stat 直接写 ThingDef**(补丁 Add 会重复节点报错): statBases 加 Bulk/MeleeCounterParryBonus;equippedStatOffsets 加 MeleeCritChance/MeleeParryChance/Suppressability(负值)。单手 tag=CE_OneHandedWeapon+RK_WeaponTag_OneHand(+Melee);双手=RK_WeaponTag_TwoHand(+Melee)(**HSK CE 无 CE_TwoHandedWeapon tag,双手不写 CE 标记**);weaponTags 要 `Inherit="false"`。纯战斗武器不做工具属性。
- **远程**: 弹药能映射 CE 原版弹种就映射(步枪→303British、手枪/SMG→9x19mmPara、霰弹→12Gauge、反器材→50BMG、榴弹→40x46mmGrenade、电荷→6x24mmCharged);自创弹药仅限能量武器。verbClass→`CombatExtended.Verb_ShootCE`;`<Properties>`(VerbPropertiesCE)写 recoil/recoilAmount/defaultProjectile/warmupTime/range/burstShotCount/ticksBetweenBurstShots/soundCast/muzzleFlashScale;`<AmmoUser>` 写 magazineSize/reloadTime/ammoSet;`<FireModes>` 写 aiUseBurstMode/aiAimMode;statBases Add Bulk。
- **装备**: statBases Add `Bulk`+`WornBulk`;equippedStatOffsets Add `CarryBulk`+`CarryWeight`;护甲值 PatchOperationReplace `ArmorRating_Sharp`/`ArmorRating_Blunt` → **CE mm 值**(原版百分比在 CE 下不适用)。
- 弹药清单基准 = `docs/06_弹药/HSK弹药清单.csv`(改弹药分类/新增前必核,避免与 CE 原版重复定义)。
- **弹药一律复用现成弹种+闲置即删(2026-08-31 用户要求)**: 给任何 mod 的武器适配弹药时,优先映射 CE/HSK 现成 AmmoSet/Bullet(对照 `docs/06_弹药/特色弹药清单.md` 映射表);自创弹药仅限经用户确认的特色弹种,现役唯一=美狐 MechaniteMiho(灵能机械素弹,`美狐HSK拓展/HSK/Defs/Ammo_Signature/MechPsy.xml`)。闲置弹药 def 一律删除防统计污染(判定以游戏导出 Unified.xml 最终态为准,源码 XML 的 pre-CE verb 引用不算活引用)。

---

## 8. 二级菜单(ArchitectSense 子分类)
**二级菜单 = 建筑师菜单一级分类(家具/结构/生产/辅助)下的子分类折叠**(由 HSK 内置 ArchitectSense.dll 提供,不用装 mod)。要点: 建筑靠 `designationCategory` 进一级分类,再靠 `ArchitectSense.DesignationSubCategoryDef` 的 `<defNames>` 挂进二级菜单;子分类 designationCategory 必须与建筑一致;结构建筑(柱子/门)也要建子分类。详细机制/写法模板/踩坑见 `docs/02_建筑与二级菜单/二级菜单说明.md`。

---

## 9. 性能铁律(高频 tick 优化)
> **性能第一(2026-08-21 用户要求)**: 所有高频 tick 逻辑必须优化成低频限定条件逻辑;与功能正确性冲突时,先保证低耗可行再谈功能细节。

- **降频**: 普通逻辑一律低频轮询(tick 计数取模,如每 60/120/300 ticks),绝不每 tick 全量执行;仅移动/战斗等必须的逻辑保持高频。
- **事件驱动**: 能用事件/回调触发刷新绝不轮询;Tick 内只做轻量"是否需要重算"判定。
- **提前短路**: 判定先过最便宜条件(无相关 pawn/建筑/comp → 直接 return),昂贵遍历/计算放最后。
- **缓存复用**: 高频读取的昂贵结果(路径/材质/属性/字典)缓存,仅低频校验点或事件时失效重建。
- **禁每 tick 遍历**: 不每 tick 遍历 map/全部 pawn/所有建筑;用索引、缓存列表或把遍历降频到秒级。
- **写 DLL 自查**: Tick()/CompTick()/GameComponentTick() 内出现遍历、反射、LINQ 全量查询、频繁字符串拼接或字典构造 → 必须先按本准则重构再提交。

---

## 10. 科研 / 配方铁律
- 科研节点相关任何操作,先 `read docs/01_科研体系/科研节点总览.md` 查 defName/坐标/前置,严禁凭记忆臆造;改完补丁重跑 `_tmp/gen_research_doc.py`。
- 改弹药/材料/二级菜单前先查导航 §一 对应文档。
- 研究台建筑 defName 速查: SimpleResearchBench/HiTechResearchBench/MultiAnalyzer=游戏本体或 Core_SK;LabStation/LabTerminal/PrimitiveResearchBench=Core_SK。
- 配方挂载只用 recipeUsers 单路径(双写致重复解锁警告);锻造台配方 workSpeedStat=SmithingSpeed(62)。
- **"has a lower techlevel than (one of) it's prerequisites"(ResearchTreeSK)= 传递闭包判定**: 不只比直接前置,前置的前置也计入,所以**一条越级边会连锁刷多条告警**(实例: `Miho_GuardDrone` 直接越级 + `Miho_LightWarDrone` 连带)。**修一条边即可清零,不要逐条改**。根因几乎都是 **Core_SK 把原版节点抬档**(实例: 原版 `GunTurrets` Industrial → HSK 里 Spacer 且挂 HiTechResearchBench+MultiAnalyzer;第三方 mod 引用它就越级)。**修法优先级**: 换挂该高档节点的**同档前置**(→ `Turrets_31`,工业档)优先于把自己节点抬档(会连带改研究台/坐标档序)。排查脚本: `_tmp/check_techlevel_full.py`(直连+全树扫描)、`_tmp/dump_research_def.py`(打印合并后 def XML + 传递闭包越级清单),都读 `LocalLow\...\MissileGirl\Cache\Unified.xml`(**UTF-8+BOM,非 UTF-16**;def 由 `<Item path=...>` 包裹,同名 def 多次出现取最后一次)。
- **改研究节点坐标前先跑 `_tmp/census_research_pos.py` 全树占位普查**: ResearchTreeSK 对坐标 **RoundToInt**,浮点偏移(如 16.6)防不了撞车(实例: 盔甲架 (16.6,12.2) 四舍五入成 (17,12) 撞 Core_SK `Storage_D1`);"在前置右侧"是**严格大于**(x 相等也告警,实例: (9,28) ← (9,25)),且**依赖它的下游块要连锁右移**(实例: 石化链右移一格后 Kurin 入口基数被迫 11→12)。改完同步+进游戏后重跑普查应 0 撞车;`redundant prerequisites` 告警=某直连前置已被另一前置传递覆盖,删直连边即可(实例: 美狐先进衣物删 `Miho_ApparelBasic`)。

## 11. 石化管网 / 储罐 / 连管铁律(2026-08-31)
> 详录: `docs/石化拓展_储罐落位记录.md`、`docs/石化拓展_抠图算法与技术细节.md`、`docs/石化拓展贴图切制_交接文档.md`。

- **连管必须用 `Graphic_Linked`**: 管道 def 的 `graphicClass` 须 `Graphic_Linked`(配连通 atlas + `linkType=Basic`);用 `Graphic_Single` **永不连管**(症状: 管各自独立、连不到储罐/泵/阀)。是否往邻格画接头 = `map.linkGrid.LinkFlagsAt(邻格) & 本建筑 linkFlags`(`Graphic_Linked.ShouldLinkWith`)。`linkGrid` 由**任何带 `linkFlags` 的建筑生成时登记**(`LinkGrid.Notify_LinkerCreatedOrDestroyed` OR `def.graphicData.linkFlags`,与其自身 graphicClass 无关)→ 要让管连到储罐/泵/阀/出料口,只需给它们 `graphicData` 补**同介质的 `linkFlags`**(NG=Custom1 / 氨=Custom3 / 沥青=Custom2),跨介质用不同 flag 即天然不互连。
- **Rimefeller 管网不可复用**: `Rimefeller.CompProperties_StorageTank` 的 `Contents` 只 `{Fuel,Oil}`、`PipeType` 只 `{Oil}`、`PipelineNet` 绑油/燃气管网 → 存不了自定义介质(沥青/氨/天然气)。自定义石化液体走本 mod 自研 `PetroGasNetHSK`(`PetroGas` 枚举 + 按介质并查集分线 + 存量落 `CompRefuelable.Fuel`);加新介质=枚举追加 + `claim[]` 数组扩容 + `DefOf` 补 case。"液位/管道数据逻辑一致"=**仿制**其效果,非复用其类。
- **可旋转储罐贴图**: `Graphic_Multi` + `rotatable` + `size(4,2)`,RimWorld 按朝向**自动交换 drawSize**(north 用横图、east 用竖图;佐证 chemTank north 320×192 / east 192×320)。用户给的竖罐图就是 `_east`,**勿拿横图旋转 90° 充数**。
- **储罐液位条(仿 FuelStorage 悬浮效果)**: 自研 comp 读同 def 的 `CompRefuelable.Fuel`,在 **`PostDrawExtraSelectionOverlays`**(仅选中时调、画在选择框之上、不被实心罐体挡)里 `GenDraw.DrawFillableBar`;坑:`FillableBarRequest` 是 `GenDraw` 的**嵌套类型须写全名**(否则 CS0246);抬高到 `drawSize.y*0.5` 上方;空罐给半透明边框才看得见。
- **储罐容量**: 沥青/天然气/氨三罐 `fuelCapacity` 统一 **20000**。
- **设定图抠图**: 浅底/假棋盘背景用"与角落背景色距>阈值 + 从边界 flood-fill 判背景 + `binary_fill_holes` 补内孔",**勿用全局阈值**(浅色罐身会被误删);要黑边对 alpha mask 做 `binary_dilation` 填黑成描边。

## 12. 详细清单指针(历史语境在详录)
| 主题 | 详录位置 |
|---|---|
| 西风骑士团武器/装甲/书整合(含体型缩放 DLL) | `docs/维护详录_鼠族HSK拓展.md` §1.1 |
| 全部踩坑实例(补丁/汉化/DLL/崩溃/异种渲染) | §3 |
| 研究节点终态与金鼠族/鼠邦 | §4 |
| 配方材料规则(科技档/工作台/无抽奖/金鼠族材料) | §5 |
| CE 适配三件套 / 弹药体系 / CompPawnGizmo | §6 |
| 补丁职责与各迁移史 | §7 |
| 补丁按主题合并精简(工具24→3 / 相邻段合并 / 全树等价验证法) | §7.8 |
| 研究台六档实例 / 性能铁律 | §5 / §9 |
| 蓝图经济 v4.1(定价/学者商队/标记/折价)与 MO 娱乐结构移植(Rustic_ 前缀) | `docs/03_生产框架/图纸门禁SchematicGateHSK_详细方案.md`(已回退, 含 v4.1 决策记录) + HSK工业科研大修 About 第四/五段 |
| 石化管网/储罐/连管/液位条/抠图 | §11 + `docs/石化拓展_储罐落位记录.md` / `docs/石化拓展_抠图算法与技术细节.md` / `docs/石化拓展贴图切制_交接文档.md` |
