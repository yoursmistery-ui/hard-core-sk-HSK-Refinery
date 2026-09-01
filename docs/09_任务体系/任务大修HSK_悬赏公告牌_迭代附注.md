---

# 迭代附注(自 主文档 外挂,遵循 ≤10KB 铁律)

## 五、坑与结论

1. **并行会话协作**:2026-08-28 有另一会话同日写入 SW 全量汉化 + 75 条族色委托 + About 更新;本次冲突产物(SW_Keys.xml/IncidentDef SW_Incidents.xml/WantedReasons 直接改 defs)已撤销,以 DefInjected 索引式方案为准。**同工作区多会话开工前先 `ls -la --time-style` 查新鲜文件。**
2. 部署 Languages 存在 `ChineseSimplified` 与 `ChineseSimplified (简体中文)` 双副本(内容一致):本体官方命名是后者(`Data/Core/Languages/*.tar`),前者在 mod 内同样生效(鼠族 mod 先例),双副本无害。
3. 构建中间物(obj/)曾误同步进部署 Source,已删;**部署目录 Assemblies 只留正式 dll(无 pdb)**。
4. UI 设计预览稿:`_tmp/bounty_ui_preview/悬赏任务详情.html`(v4,真实贴图 file:// 直读;`#quest` 锚点直达原版任务页视图)。v5 通缉令海报改版稿:`_tmp/bounty_ui_preview/悬赏公告牌_v5.html`(2026-08-30):详情卡弃用 `SW.Warrant.Draw`(裸贴图+三行文字,旧 UI 丑的根源)改自绘海报——印章倒计时/照片框头像(Portraits.Get)/生死双酬/派系色边框/三页签;数据只含 SW 真实字段,v4 的位置/敌情/失败后果为虚构已移除;页签切换/接受/拒绝/筛选已脚本自验(贴图 8/8 加载)。**已实装**,见 §十。

## 六、悬赏无线电(2026-08-29 二期)

**机制闭环**:SW 引擎不定期生成(warrantGenMTB)/开局积压/轨道商人通话夹带(50%,冷却 12h)三种来源的悬赏,全部先进 `BountyRadioManager.pending`(GameComponent,自动注册+scribe,`pendingRadioWarrants` Deep 存 Warrant)→ 信件「悬赏电波」提示 → **殖民者右键通讯台「接收悬赏通讯(n)」**(JobDef `RK_Bounty_ReceiveRadio`,500 tick 操作,+智力技能)→ 弹出一条入 `availableWarrants` → 公告牌/榜单可见,正常接受。

**接入点(全 Harmony postfix,目标方法均无重载)**:
| 目标 | 作用 |
|---|---|
| `WarrantsManager.HandleAvailableWarrants`(private) | 每 tick 尾捕获 `createdTick == TicksGame` 的新单进 pending;赏金猎人袭击逻辑不受影响 |
| `WarrantsManager.PopulateWarrants` | 开局/读档 3~5 条初始通缉同样进 pending(历史积压) |
| `TradeShip.TryOpenComms` | 通话后冷却+概率走 `PopulateWarrants(1)`+快照差集捕获,信件归功商队 |
| `Building_CommsConsole.GetFloatMenuOptions` | pending>0 时前置「接收悬赏通讯」项(MenuOptionPriority.High),`TryTakeOrderedJob` 派工 |

**关键事实**:玩家自建委托走 `createdWarrants`(不经 `availableWarrants`),不会被截获;`Warrant.Draw` 的 Decline 只从 availableWarrants 移除;pending 不参与 15 天过期(录得的播报永久待领)。弹窗顶部 pending>0 时有红字提示行。**悬赏板建筑盘点:全环境仅 `RKFC_RL_Board` 一块**(GloomyDeco Board 只是贴图无 def)。

**教训**:1.6 的 `Find` 没有 `CurrentGame`(用 `Current.Game`);`GlobalTargetInfo` 在部分上下文解析异常,信件直接用三参 `ReceiveLetter` 重载;`Toil.ticksLeftThisToil` 不可靠(进度条用本地计数闭包)。

## 七、限流与时间限制(2026-08-29 三期)

- **一周一条**:`BountyRadioManager.lastBountyGenTick` 全局锚点(scribe);任何来源入队即刷新。冷却判定挂在 `WarrantsManager.GenerateRandomWarrant` 的 **Harmony prefix**(private 无重载)——冷却期直接短路返回 null。⚠ 不能用"生成后丢弃"方案:`GenerateRandomWarrant` 会真造 pawn/选派系,冷却期每 tick 重试是性能灾难,必须从生成处拦。商队夹带(`TryOpenComms` postfix)在进 `PopulateWarrants(1)` 前先查冷却。开局积压不受限(一次性)。
- **待接收 7 天过期**:`PurgeExpiredPending()` 按 `createdTick` 判 `PendingExpiryDays=7`,挂 `GameComponentTick` 每 120 tick 低频执行,过期批量发一封信;`PendingHint` 文案已注明 7 天。
- **榜上 15 天(SW 原规则)倒计时 UI**:`AvailableDaysLeft(w)` = ceil(15 − 已过天数);公告牌列表行(可接页签)赏金下小字「剩N天」,详情卡顶部一行「⚠ 有效期:剩 N 天」,≤2 天用 `ColoredText.ThreatColor` 红字。SW 内部 `HandleAvailableWarrants` 本来就按 15 天移除,UI 与行为对齐。
- 已接受的任务期限制:SW QuestScriptDef 自带 15 天超时(原版任务页显示),未改动。

## 八、四期优化(2026-08-29)

- **公告牌 = 第二接收端**:`Thing.GetFloatMenuOptions(Pawn)` 是虚方法(基础实现返回空),公告牌未重写 → Harmony postfix 打在 `Thing` 基类上,`HasComp<CompBountyBoard>` 门控,右键公告牌也出「接收悬赏通讯」。解决早期开局(通讯台要 Microelectronics 研究线,悬赏播报 7 天过期会全灭)的可用性死结。JobDriver 目标泛化为 `Building`:通讯台查 `CanUseCommsNow`,其余建筑查 `!Destroyed`(⚠ Building 没有 `Downed`,那是 Pawn 的)。
- **弹窗「无线电」页签**:第三页签看 pending 队列,行显示 7 天倒计时,详情卡只读(`Warrant.Draw(card, false, false)`,不出现接受/拒绝按钮——没接收的不能隔空接单)。
- **列表缓存**:OnGUI 每帧跑 LINQ(Where/OrderBy 每帧分配)违反 §9,改"签名(页签+筛选+各列表 count)+30 tick"节流缓存。
- 公告牌 gizmo 标题在有待接收时变为「查看悬赏榜(📡 待接收 N 条)」。

## 九、五期:性能与降频(2026-08-29)

- **性能**:删掉 `WarrantsManager.HandleAvailableWarrants` 上的每 tick Harmony postfix(SW 热路径零钩子),周期生成捕获移入 `BountyRadioManager.GameComponentTick`,每 60 tick 轮询一次;用 `seenIds`(HashSet<string>,scribe)识别新单——⚠ **读档首扫必须只登记基线不捕获**,否则会把已在榜上的旧单全部误截进待接收。`PopulateWarrants` 补丁保留(仅开局/商队夹带时调用,非热路径)。
- **降频**:开局积压 `PopulateWarrants` 参数 prefix 封顶 3~5→≤2;商队夹带 Chance 0.5→0.25、商人冷却 12h→24h(受全局一周一条前置)。过期播报清理 120→600 tick。
- **教训**:用 python 字符串切片改 C# 时 `find()` 锚点若含转义引号会匹配失败 → replace 把内容插进循环点,文件膨胀到 7.5 万行;整文件重写才稳。

## 十、v5 海报实装 + 实物奖励体系(2026-08-30)

**海报卡**:`Dialog_BountyBoard.DrawPoster` 全自绘(不再走 `Warrant.Draw`)——纸底色/派系色边框角标/类型徽章(抓捕·猎杀·驯服·夺回四色)/旋转印章(-9°,可接=剩N天、≤2 天红字;已接=已接受;自建=我发布的;待接收=电波待接收)/照片框(活体 pawn 走 `PortraitsCache.Get(…, Rot4.South)`,动物走 race.graphic,物件走 Graphic.MatSouth)/生死双酬筹码/口信纸条/待接收横幅。按钮复用 SW 语义:Decline→`availableWarrants.Remove`、Accept→`DoAcceptAction`、自建单→全 5 列表 RemoveEverywhere、已接单→`ShouldShowCompensateButton`→补偿。海报色(Paper/Stamp/Badge 8 个自定义 Color)是主题无关的质感色,RimThemesLite 映射不到属预期;语义色仍走 ColoredText。

**实物加成**(用户选定:银保留 + 挂钩赏金 35~50%):
- 池 Defs `RK_悬赏实物奖励池.xml`:`ThingSetMaker_MarketValue` ×4(锭/武器/衣物装备/杂货),defName 已用脚本对全环境核过(`Gun_Thumpsmg` 缺失已剔除;Vile 冶炼条是通用 CastBar 不入池)。
- `BountyRewards.cs`:种子=FNV-1a(loadID)(`string.GetHashCode` 有随机化不可用),`Rand.PushState(seed+i*397)` 分池生成——**海报展示与实发同种子重算,零持久化**。分档 <300 不加成 / 300~800→35% / 800~1500→40% / ≥1500→50%;驯服/夺回单走 锭40+装备30+杂货30,其余 锭30+武器35+装备20+杂货15;单池预算 <60 跳过;银被滤除。UI 缓存按弹窗开合清(loadID 跨存档会重号,发放路径永不读缓存)。
- Harmony ×2:`Warrant.GiveReward` **基类** postfix(抓捕/夺回子类都先调 base → 恰好一次;顺带修驯服上游 bug:基类无发放逻辑,商队交付驯服单不发银,按 `Reward` 补发)+ `TransportersArrivalAction_ReturnWarrant.Arrived` postfix(运输舱路径 SW 只掉银不调 GiveReward,实物掉玩家主图交易点附近,与银同款落点;私有字段 warrant 用 Traverse 取)。
- 1.6 API 坑:`ContractedBy` **没有**四参/双参重载(只有单 margin);`PortraitsCache.Get` 必须显式传 `Rot4 rotation`(第二参后即必填);`Caravan` 在 `RimWorld.Planet`。
- Keyed 新增 25 键(Doc*/Type*/Stamp*/Poster*/Bonus*),三语言目录(含 简体中文 副本)各 53 键对齐。
- ⚠ **未做游戏内验证**(构建 0 错 0 警):待实测海报各状态渲染、接受→交付(商队+运输舱)实物到账、驯服补银、种子跨读档一致。
