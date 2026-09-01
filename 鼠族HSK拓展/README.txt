鼠族HSK拓展
================================

作用
----
1. 鼠族(RatkinRaceHSK)89件装备: 数值+配方改造
2. 拓展装备44件(原「维多利亚的港湾」内容): 已独立整合进本mod,不再依赖原mod
   - 极致武器(高分子剑刃/无尽之息/黄昏毁灭者/赤炎链锯): 绑定「近战武器VI」,数值取V/VI档中间值
   - 近战武器 VII 前置为近战武器 VI(依赖关系已接上,研究树正常渲染)
   - 太空装甲/军服/贵族装/背包/护盾: 研究并入HSK科技树(护甲VI等)
3. 鼠族衣物III 8件(背包/羊毛衫/头巾/卫花使制服/皇家披袍/盛夏连衣裙/御寒帽/工作服)
   材料改为前石油工业(钢铁替代复合粘剂/人造纤维/塑料/合成橡胶),数值 +25%
4. 骑士装甲/骑士头盔: 制造改为鼠族电力裁缝台+先进纺织工作台(原手工装配台)
5. 鼠族装甲(RK_Plate)/骑士头盔: 属性强化(介于原值与骑士装甲之间),鼠族背包增加携带容量与负重
6. 科研节点重构(独立成鼠族研究):
   - 贵族服饰 → 鼠族贵族服饰
   - 侦察装甲 → 鼠族盔甲 III(依赖鼠族盔甲 II,填补极致时代)
   - 金属加工 II → 鼠族中世纪装甲
   - 鼠族衣物 IV(后石化): 依赖石油化工 V + 鼠族衣物 III,配方含石化产物+毛类,数值完善
7. 工作台按 Core SK 惯例: 太空护甲/盾牌→先进纺织工作台;极致近战→机械武器制作台;枪械→高级武器制作台
8. 文化(Ideology)职位适配: 领袖/道德指导者/各专家 + Vanilla Ideology Expanded 职位可穿鼠族服饰
9. CE适配: 按 HSK 的 Combat Extended(CE Continued)独立适配,含光储能弹/压缩机炮弹弹药,huimie/FBZX 近战工具 CE 化
10. 追加服饰14件(RatkinApperal+): 头环/军服/棉甲/披挂等,HSK配方与数值适配
11. 其他种族兼容: Rabbie/Horan/Dragonian 可穿鼠族服饰、用鼠族武器(装了对应种族自动生效)
12. HSK Mod Assistant 标记: 装了「HSK Autosort and Mod Assistant」时,mod 列表会显示本mod为兼容附加包
13. Vile's Materials Science 全面适配: 装了「Vile's Materials Science」时,全部147件配方自动增补新材料
    (缝纫套件/Kevlar/聚碳酸酯/玻璃纤维/尼龙/碳合金/气凝胶等,按衣物/护甲/武器分档)

整合内容(2026-08-21, 已精简至 20%)
--------------------------------------
14. Ratkin GreatWar(browncofe.RCG): 189 件衣物/89 武器已精简为 **40 件代表衣物 + 18 件代表武器**
    (每国 1 步兵制服 + 1 标志性头盔, 3 个大国留特色装甲; 武器留 6 栓动步枪 + 机枪/冲锋枪/狙击/霰弹/AT/榴弹/手枪 各 1)
15. Ratkin Manuscripts - OrderState(browncofe.RCKO): 22 件精简为 3 件(骑士制服/头盔/装甲) + 黄金园榴弹铳
16. 科技适配(86 补丁): 已删除独立科技页(30 个独立研究 + 2 个 tab), 全部并入 HSK 科技树:
    - 衣物(军服/头盔/装甲) → 鼠族衣物 III(Ratkin_Apparel_C1), 制作于鼠族缝纫台
    - 近战(骑枪/军刀/骑士剑/狼牙棒) → 锻造(Smithing), 制作于鼠族锻造台
    - 冲锋枪 → 后座原理(BlowbackOperation); 其余枪械 → 制枪(Gunsmithing), 制作于高级武器台
    - 全部改为显式配方(RKHSKGW_Make_*, recipeUsers 单路径, 吃对应工作速度)
17. CE 适配(82/84 补丁): 保留枪械按类型用 CE 标准弹药(303British/9x19/12Gauge/50BMG/40mm 榴弹)
18. 平衡调整(87 补丁): 6 把栓动步枪长程精度 0.85-0.90 → 0.75, 射程 40-45 → 38(对齐 HSK 鼠族步枪档位)
    其余保留源数值(已在 HSK 合理区间)
19. 源 mod weaponTag 笔误修复(85 补丁): RK_EU_Zundkraut 补 RK_EU_Grenade
20. 汉化: 全部迁移到 Languages\ChineseSimplified (简体中文)\(带后缀,铁律)
21. 注意: 若同时订阅了创意工坊原版「Ratkin GreatWar」或「Ratkin Manuscripts - OrderState」,务必取消订阅,
    否则 defName 冲突; 精简掉的装备仅影响内容量, 派系/PawnKind 仍保留
22. Ratkin Moustate 鼠邦(EoralMilk.RatkinMoustate, 2026-08-21): 东亚风格鼠族阵营扩展
    - 东方服饰/武器/基因/发型/饰品系统(3 槽)/派系(鼠邦+花札匪徒)/建筑/场景/研究(1 项, tab=RK_ResearchTab_Default)
    - 名称已更新为「鼠族HSK拓展」(原名 鼠族HSK拓展)
23. 前置框架已内置整合(2026-08-21): Meow Framework(Meow.dll)与 Ratkin Gene Expanded(RatkinGeneExpanded.dll)
    已合并进本 mod(Assemblies/ + Defs/MeowFramework + Defs/RKGE + Patches/RKGE),**不再需要单独安装/订阅**
    - 独立 mod 文件夹已从 Mods 目录移除(备份在 _backup_frameworks_20260821),ModsConfig 依赖项已清除
24. 框架精简(乌托邦定位): 仅保留支撑鼠邦/饰品系统/基因异种必需的功能
    - 已删: RKGE EXT_Modern 全套(喵爪毒品/电脑/电竞椅/帮派武器/任务/废土鼠异种/现代场景)、RKGE 校服/见习/云裳 4 件衣物、
      任务系统(CustomSite)、回收/修复配方、负重统计、Gender 误用功能等
    - 保留: 饰品系统(项链/戒指/手镯 3 层 + 锻造台制作)、22 个鼠族基因 + 7 个异种人型(含田鼠,鼠邦配套)、
      鼠族变体耳朵/尾巴身体部件(6 族)、9+4 款发型、云淡风轻特性、派系异种分布、交易标签
25. 田鼠种族 HSK 基因适配(2026-08-21, 88 补丁 + RKGE 补丁重写):
    - HSK 的 Ratkin 用 xenotypeList 白名单 + raceGenes 基础基因机制(无 whiteXenotypeList),RKGE 7 个异种已追加进
      xenotypeList(普通鼠族 RK_XenoType_Ratkin 保留 0.7 主导概率)
    - 从 raceGenes 移除 大圆耳(RK_Gene_LargeEars)+细尾(RK_Gene_ThinTail): 这两基因与田鼠等变体耳朵/尾巴基因
      (exclusionTags Ears/Tail)互斥冲突;普通鼠族圆耳/细尾改由 HSK 异种 RK_XenoType_Ratkin 提供,变体异种用
      自己的变体基因(RKGE addon 渲染)
    - 异种基因清理: 移除与 raceGenes 重复的基因(灵活/近视/痛觉弱等)+ Hair 多基因互斥改单基因(HairColor exclusion)
    - 派系/PawnKind 概率表改为 Add 追加(保留 HSK 概率,田鼠等变体为少数)

Core SK 适配标记
----------------
- About.xml 依赖声明: skyarkhangel.HSK(Core SK)、Solaris.RatkinRaceMod(NewRatkinPlus)
- 使用 HSK 材料/研究体系,数值按 HSK 平衡草案

安装
----
1. 把本文件夹放入 RimWorld\Mods 目录
2. 启用顺序: Core SK → RatkinRaceHSK → 本mod(放最后)
3. 请禁用: 旧版 RatkinRaceHSK_MaterialFix、原「维多利亚的港湾」mod(内容已整合,避免冲突)
4. 文化拓展可选: Vanilla Ideology Expanded - Memes and Structures(装了会自动适配其职位)
5. 无需新开档,重新进游戏生效;已存在的制作订单需重新添加

说明
----
- 数值草案(常规 +15%,衣物III +25%)与极致武器中间值可在补丁XML里直接改
- 本mod附带原mod的DLL(含NewRatkin.dll自定义版),与原鼠族DLL共存时以本mod为准
- CE适配补丁(04)为HSK CE定制: 含光储能弹/压缩机炮弹弹药与武器CE兼容
