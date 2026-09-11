// ============================================================================
//  BlueprintUnlockHSK — 科技蓝图「读书推进解锁」机制 (HSK 本地整合, v5.0)
// ============================================================================
//  功能:
//    1) 每个门控科技对应「一套蓝图书」 (BlueprintTargetExtension + BookOutcomeProperties)
//       - 中世纪 = 图纸书 1 本/系列; 工业后 = 蓝图 N 本/系列 (I/II/III/IV)
//    2) 书 = 原版 Book 体系 (thingClass=Book): 可放书架、阅读、读后保留、带「已读」标记
//    3) 阅读 = 原版 JoyGiver_Read + JobDriver_Reading (休闲娱乐活动, v5 变更):
//       - 自带阅读动画/进度条/就坐/双态 (Read-Unread) 由原版提供, 本 mod 不写 JobDriver
//       - 玩家仍可右键「指派阅读」强制阅读 (job.playerForced → 固定 5000 ticks)
//    4) 解锁进度 (v5 核心变更):
//       - 每读完一本书 → 该科技 unlockProgress += 1/系列本数 (逐 tick 累加, 支持读一半存档)
//       - 系列全部书读完 (进度 1.0) → 解锁该科技的「研究权限」(变成可研究); 玩家仍需正常立项研究
//         (finishOnUnlock=false 为默认; 置 true 才是读完即直接完成该研究)
//       - 不写 ResearchPoints, 与原版科研点体系解耦
//    5) 三条 v5 数值层 (全部可在 XML 覆盖, 见 BlueprintTargetExtension):
//       - 智力门槛: 智力等级不足 → 读不懂 (硬门槛, 双点拦截: CanReadBook + IsValidBook)
//       - 娱乐削弱: 蓝图书 joyFactor 0.25 (原版小说 1.0~2.5), 读书解闷效率低
//       - 智力加成: 读书智力经验 ×1.0~3.0 (原版 JobDriver_Reading 自带 0.1/tick 打底)
//    6) 科研树界面 (ResearchTreeSK) 节点角标: 解锁进度百分比, 满 100% 变绿
//  历史:
//    v3.x (2026-08-24): 存储池 + 使用即得 (CompUseEffect_Blueprint), 科研值银行
//    v4.0 (2026-08-27): 用户决策 D1~D11 → 全部书籍化, 自写 JobDriver_StudyBlueprint (科研工作)
//    v5.0 (2026-09-10): 用户决策 → 改用原版娱乐阅读 + 逐 tick 解锁进度 + 智力门槛/娱乐削弱/智力加成
//  配置: 全部通过蓝图书 ThingDef 的 BlueprintTargetExtension (Defs 里改, 无需重编译)
//  编译: 用同目录 build.ps1 (csc.exe, .NET Framework 4.0)
//  注意: 本文件为 C#5 兼容写法 (无 $"" 插值 / 无 ?. / 无局部函数 / 无 out var 内联)
// ============================================================================
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BlueprintUnlockHSK
{
    // ---- Harmony 注册 ----
    [StaticConstructorOnStartup]
    public static class BlueprintUnlockInit
    {
        static BlueprintUnlockInit()
        {
            Harmony harmony = new Harmony("local.ratkin.blueprintunlockhsk");
            harmony.PatchAll();

            // 科研树节点徽标: 在每个门控科技节点上直接绘制 "已读X/N" 角标
            // (挂在 ResearchTreeSK.Node.Draw, 未装 ResearchTreeSK 时自动跳过)
            System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
            if (nodeType != null)
            {
                try
                {
                    harmony.Patch(AccessTools.Method(nodeType, "Draw"),
                        postfix: new HarmonyMethod(typeof(Patch_ResearchTreeNodeBadge).GetMethod("Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] 科研树节点徽标补丁失败(已跳过): " + e.Message);
                }

                // 门控科技未集齐蓝图 → 强制节点 Available=false:
                // ResearchTreeSK 的置灰/锁图标/禁止点击全部只看 Node.Available, 不看 CanStartNow,
                // 所以仅靠 Patch_CanStartNow 无法让科研树节点变灰不可点。这里补上科研树侧的门禁。
                try
                {
                    harmony.Patch(AccessTools.Method(nodeType, "UpdateCaches"),
                        postfix: new HarmonyMethod(typeof(Patch_NodeUpdateCaches).GetMethod("Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] 科研树节点门禁补丁失败(已跳过): " + e.Message);
                }
            }

            // ---- v5 三条机制补丁 (原版 Book 体系钩子, 全部按方法存在性门控) ----
            //  ① 智力门槛: CanReadBook 唯一判定入口 (右键指派 + 阅读中每刻校验) 后置拦截
            //  ② 智力门槛: joy 自动派书的候选过滤器 (BookUtility.IsValidBook, private static)
            //     —— 只拦 ① 会导致 joy 自动塞书 → 开读即中断 → 反复重试的死循环, 必须双点拦截
            //  ③ 智力加成: Book.OnBookReadTick 每 tick 调一次, 在此叠加智力经验倍率
#if !BP_BISECT || BP_GROUP_THRESHOLD
            TryPatch(harmony, typeof(Verse.BookUtility), "CanReadBook",
                "Patch_BookUtility", "CanReadBook_Postfix");
            TryPatch(harmony, typeof(Verse.BookUtility), "IsValidBook",
                "Patch_BookUtility", "IsValidBook_Prefix");
#endif
#if !BP_BISECT || BP_GROUP_READXP
            TryPatch(harmony, typeof(Verse.Book), "OnBookReadTick",
                "Patch_BookReadTick", "Postfix");
#endif

            // ---- v5.4 「科技典籍」+ 「已解锁」角标 (2026-09-10) ----
            //  ⑧ 原版随机造书唯一入口 (BookUtility.MakeBook(ArtGenerationContext, QualityGenerator?)) ——
            //     任务奖励书籍堆 / 商队书籍 / 书商库存 全走这条; 小概率换成「科技典籍」。
            //     注意两个 MakeBook 重载必须用参数类型显式锁定 (name-only 会歧义)。
#if !BP_BISECT || BP_GROUP_LESSONS
            TryPatch(harmony, typeof(Verse.BookUtility), "MakeBook",
                new System.Type[] { typeof(ArtGenerationContext), typeof(System.Nullable<QualityGenerator>) },
                "Patch_BookMakeRandom", "MakeBook_Prefix");
#endif
            //  ⑨ UI 物品图标 (背包/书架/交易/搜索) 上的「已解锁」角标。
            //     第一个重载: ThingIcon(Rect, Thing, float, Rot4?, bool, float, bool) —— 可选参数在 IL 里
            //     仍是完整签名, 必须给全 7 个类型; 补丁侧只取前两个 (__0=rect, __1=thing)。
#if !BP_BISECT || BP_GROUP_ICON
            TryPatch(harmony, typeof(Verse.Widgets), "ThingIcon",
                new System.Type[]
                {
                    typeof(Rect), typeof(Thing), typeof(float), typeof(System.Nullable<Rot4>),
                    typeof(bool), typeof(float), typeof(bool)
                },
                "Patch_ThingIcon", "Postfix");
#endif

            // ---- v5.1 硬门槛三道 (2026-09-10, 修 ResearchTreeSK 队列绕过) ----
#if !BP_BISECT || BP_GROUP_GATE_PERF
            //  ④ 进度层·主路径: ResearchPerformed 是研究台劳动的唯一出入口 (它不走 AddProgress,
            //     而是直接写 progress[currentProj]), 必须单独拦
            TryPatch(harmony, typeof(RimWorld.ResearchManager), "ResearchPerformed",
                new System.Type[] { typeof(float), typeof(Pawn) },
                "Patch_ResearchPerformed", "ResearchPerformed_Prefix");
#endif
#if !BP_BISECT || BP_GROUP_GATE_ADD
            //  ⑤ 进度层·其它入口: 读书给科研 / 脚本加进度等全部走 AddProgress
            TryPatch(harmony, typeof(RimWorld.ResearchManager), "AddProgress",
                new System.Type[] { typeof(ResearchProjectDef), typeof(float), typeof(Pawn) },
                "Patch_ResearchAddProgress", "AddProgress_Prefix");
#endif
#if !BP_BISECT || BP_GROUP_GATE_WORK
            //  ⑥ 劳动层: 未解锁的门控科技不派人去研究 (避免站台前按 0% 白刷智力经验)
            TryPatch(harmony, typeof(RimWorld.WorkGiver_Researcher), "HasJobOnThing",
                new System.Type[] { typeof(Pawn), typeof(Thing), typeof(bool) },
                "Patch_WorkGiverResearcher", "HasJobOnThing_Postfix");
#endif
#if !BP_BISECT || BP_GROUP_GATE_TECH
            //  ⑦ 道具层: 科技专家副人格核心不能用来跳过蓝图门控
            TryPatch(harmony, typeof(RimWorld.CompUseEffect_FinishRandomResearchProject), "DoEffect",
                new System.Type[] { typeof(Pawn) },
                "Patch_TechprofCore", "DoEffect_Prefix");
#endif

            // ---- v5.5 「科研节点标识」(2026-09-10) ----
            //  ⑩ 原版「读书给科研」doer 的项目表 (ReadingOutcomeDoerGainResearch.values) ——
            //     XML 挂空 doer, 这里按每本书自己的 targetTech 填回去, 供 UI 画绿/黄/红书图标。
            //     OnBookGenerated(Pawn author = null) 是 Book.GenerateBook 里唯一的调用点。
#if !BP_BISECT || BP_GROUP_ICON
            TryPatch(harmony, typeof(RimWorld.ReadingOutcomeDoerGainResearch), "OnBookGenerated",
                new System.Type[] { typeof(Pawn) },
                "Patch_GainResearchTargeting", "OnBookGenerated_Postfix");
            //  ⑩b 逐刻守卫 (2026-09-11 红字修复): values 为 null 时原版 OnReadingTick 会
            //      直接 NullReferenceException (红字刷屏)。这里前置兜一道 —— 能修就修,
            //      修不好就跳过原版, 保证异常绝不逃逸。
            TryPatch(harmony, typeof(RimWorld.ReadingOutcomeDoerGainResearch), "OnReadingTick",
                new System.Type[] { typeof(Pawn), typeof(float) },
                "Patch_GainResearchReadingTick", "OnReadingTick_Prefix");
#endif
            //  ⑪ 读档补齐: 老存档里已经存在的书 (升级前造的) 不会再有 OnBookGenerated 机会,
            //     挂 Book.ExposeData 后置, 每本书读档/存档时补齐标注。
#if !BP_BISECT || BP_GROUP_EXPOSE
            TryPatch(harmony, typeof(Verse.Book), "ExposeData",
                "Patch_BookExposeData", "ExposeData_Postfix");
#endif

#if BP_BISECT
            // ---- 二分定位辅助 (仅在 -D:BP_BISECT 编译时存在) ----
            //  打印本变体实际启用了几组 Book 补丁, 便于事后从日志确认"跑的是哪个变体"。
            //  正式编译 (build.ps1, 不带任何 -D) 走 !BP_BISECT 分支 = 全部开启, 不含此段。
            {
                string bisectGroups = "";
#if BP_GROUP_THRESHOLD
                bisectGroups += "THRESHOLD,";
#endif
#if BP_GROUP_READXP
                bisectGroups += "READXP,";
#endif
#if BP_GROUP_LESSONS
                bisectGroups += "LESSONS,";
#endif
#if BP_GROUP_ICON
                bisectGroups += "ICON,";
#endif
#if BP_GROUP_GATE_PERF
                bisectGroups += "GATE_PERF,";
#endif
#if BP_GROUP_GATE_ADD
                bisectGroups += "GATE_ADD,";
#endif
#if BP_GROUP_GATE_WORK
                bisectGroups += "GATE_WORK,";
#endif
#if BP_GROUP_GATE_TECH
                bisectGroups += "GATE_TECH,";
#endif
#if BP_GROUP_EXPOSE
                bisectGroups += "EXPOSE,";
#endif
                Log.Message("[BlueprintUnlockHSK] BISECT BUILD - Book patch groups enabled: ["
                    + bisectGroups + "]  (empty = ALL disabled)");
            }
#endif

            // ---- 事件/任务蓝图投放 (全部按类型存在性门控, 未装对应 mod 自动跳过) ----

            // ScatterAt 均为 protected (IntVec3, Map, GenStepParams, int) 重载, 继承链同名,
            // 必须显式参数类型查找, 避免 name-only 歧义
            System.Type[] scatterAtParams = new System.Type[]
            {
                typeof(IntVec3), typeof(Map), typeof(GenStepParams), typeof(int)
            };

            // 物品贮藏任务 (RimQuest 用原版 QuestScriptDef): 藏宝处放一张蓝图书
            try
            {
                harmony.Patch(AccessTools.Method(typeof(GenStep_ItemStash), "ScatterAt", scatterAtParams),
                    postfix: new HarmonyMethod(typeof(Patch_ItemStash).GetMethod("ScatterAt_Postfix")));
            }
            catch (System.Exception e)
            {
                Log.Warning("[BlueprintUnlockHSK] 物品贮藏补丁失败(已跳过): " + e.Message);
            }

            // Go Explore: 战利品生成后注入蓝图书 (失落之城/监狱营/拦截消息等)
            System.Type goEx = AccessTools.TypeByName("LetsGoExplore.RewardGeneratorUtilityLGE");
            if (goEx != null)
            {
                try
                {
                    harmony.Patch(AccessTools.Method(goEx, "GenerateStockpileReward"),
                        postfix: new HarmonyMethod(typeof(Patch_GoExploreRewards).GetMethod("Stockpile_Postfix")));
                    harmony.Patch(AccessTools.Method(goEx, "GenerateStorageBoxReward"),
                        postfix: new HarmonyMethod(typeof(Patch_GoExploreRewards).GetMethod("StorageBox_Postfix")));
                    harmony.Patch(AccessTools.Method(goEx, "GenerateInterceptedMessageReward"),
                        postfix: new HarmonyMethod(typeof(Patch_GoExploreRewards).GetMethod("Intercepted_Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] GoExplore 奖励补丁失败(已跳过): " + e.Message);
                }
            }

            // Cybranian Events: 陨石坠落处放蓝图书 + 老人随身带蓝图书
            System.Type meteor = AccessTools.TypeByName("EventsCore.GenSteps.GenStep_WorldMeteorite");
            if (meteor != null)
            {
                try
                {
                    harmony.Patch(AccessTools.Method(meteor, "ScatterAt", scatterAtParams),
                        postfix: new HarmonyMethod(typeof(Patch_CybranianEvents).GetMethod("Meteorite_Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] Cybranian 陨石补丁失败(已跳过): " + e.Message);
                }
            }
            // Cybranian Events: 老人随身带蓝图书。
            // AddSpawnPawnQuestParts(Quest, Map, Pawn) 是虚拟方法, 声明在基类 QuestNode_Root_WandererJoin_WalkIn (原版),
            // OldMan 只继承不重写 → Harmony 拒绝 patch 继承方法 (报 "Patch the declared method ... instead"),
            // 必须改打声明它的基类方法本身。打基类后会对所有 walk-in 流浪者任务触发,
            // 故在 OldMan_Postfix 内按 __instance 真实类型名门控, 仅处理 Cybranian 的 OldMan 任务。
            System.Type oldMan = AccessTools.TypeByName("EventsCore.Quests.QuestNode_Root_WandererJoin_OldMan");
            if (oldMan != null)
            {
                try
                {
                    harmony.Patch(
                        AccessTools.Method(typeof(RimWorld.QuestGen.QuestNode_Root_WandererJoin_WalkIn), "AddSpawnPawnQuestParts"),
                        postfix: new HarmonyMethod(typeof(Patch_CybranianEvents).GetMethod("OldMan_Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] Cybranian 老人补丁失败(已跳过): " + e.Message);
                }
            }
        }

        // 按方法存在性安全挂补丁: 目标方法/补丁方法任一缺失只记警告, 不影响其它补丁
        // (补丁方法名以 "_Prefix" 结尾走 prefix, 否则走 postfix)
        private static void TryPatch(Harmony harmony, System.Type targetType, string methodName,
            string patchClassName, string patchMethodName)
        {
            TryPatch(harmony, targetType, methodName, null, patchClassName, patchMethodName);
        }

        // 带参数类型重载: 显式锁定重载, 避免 name-only 查到基类虚方法/别的重载。
        // 若参数类型查到的不是 targetType 自己声明的那个 (说明命中的是基类),
        // 再退到 DeclaredMethod 强制取本类型声明的方法 (否则后置不生效于子类 override)。
        private static void TryPatch(Harmony harmony, System.Type targetType, string methodName,
            System.Type[] paramTypes, string patchClassName, string patchMethodName)
        {
            try
            {
                System.Type patchType = AccessTools.TypeByName("BlueprintUnlockHSK." + patchClassName);
                MethodInfo targetMethod = (paramTypes == null)
                    ? AccessTools.Method(targetType, methodName)
                    : AccessTools.Method(targetType, methodName, paramTypes);
                if (paramTypes != null && targetMethod != null && targetMethod.DeclaringType != targetType)
                {
                    MethodInfo declared = AccessTools.DeclaredMethod(targetType, methodName, paramTypes);
                    if (declared != null)
                    {
                        targetMethod = declared;
                    }
                }
                MethodInfo patchMethod = patchType != null ? AccessTools.Method(patchType, patchMethodName) : null;
                if (targetMethod == null || patchMethod == null)
                {
                    Log.Warning("[BlueprintUnlockHSK] 找不到补丁目标 " + targetType.Name + "." + methodName
                        + " 或 " + patchClassName + "." + patchMethodName + " (已跳过)");
                    return;
                }
                HarmonyMethod hm = new HarmonyMethod(patchMethod);
                if (patchMethodName.EndsWith("_Prefix"))
                {
                    harmony.Patch(targetMethod, prefix: hm);
                }
                else
                {
                    harmony.Patch(targetMethod, postfix: hm);
                }
            }
            catch (System.Exception e)
            {
                Log.Warning("[BlueprintUnlockHSK] " + methodName + " 补丁失败(已跳过): " + e.Message);
            }
        }
    }

    // ============================================================================
    //  「卷」标识 (v6, 2026-09-11): 一个科技的一卷 = 一条解锁单元
    // ============================================================================
    //  v6 把「书 → 科技」的绑定细化到「书 → 科技的第 k 卷」:
    //    · 某科技的卷数 N = 该科技的蓝图书本数 (GetVolumeCount, 无蓝图时回落 1);
    //    · 一本实体书绑定 1~2 卷 (同一科技), 存在书实例上 (BlueprintBook.boundVolumes);
    //    · 世界层只记「哪些卷已经读毕」(BlueprintUnlockTracker.readVolumes);
    //    · 该科技 N 卷全部读毕 → 解锁研究权限。
    //  ⚠ 键格式统一为 "techDefName|volumeIndex" (用 | 分隔, 科技 defName 不含该字符)。
    //    用字符串而不是自定义类, 是为了避开 RimWorld XML 反序列化自定义类型的坑。
    public static class VolumeUtil
    {
        public const char Sep = '|';

        public static string Key(string tech, int volume)
        {
            if (tech == null)
            {
                tech = "";
            }
            return tech + Sep + volume;
        }

        public static string TechOf(string key)
        {
            if (key == null)
            {
                return "";
            }
            int i = key.LastIndexOf(Sep);
            return i < 0 ? key : key.Substring(0, i);
        }

        public static int VolumeOf(string key)
        {
            if (key == null)
            {
                return 1;
            }
            int i = key.LastIndexOf(Sep);
            if (i < 0 || i + 1 >= key.Length)
            {
                return 1;
            }
            int v;
            if (int.TryParse(key.Substring(i + 1), out v) && v > 0)
            {
                return v;
            }
            return 1;
        }

        // 是否是本 mod 认得的卷键 (非空 + 有分隔符)
        public static bool IsVolumeKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.IndexOf(Sep) >= 0;
        }
    }

    // ============================================================================
    //  蓝图书侧配置: 目标科技 + 分级 + 系列参数 (书籍化 v4)
    // ============================================================================
    //  系列推导: 同一 targetTech 的全部蓝图书 def 组成一个系列 (中世纪 1 本,
    //  太空 2 本, 极致 3 本, 超凡 4 本)。seriesTotal = 同 targetTech 的 def 数;
    //  seriesIndex = XML 可显式给 bookIndex, 缺省按 defName 排序 (I/II/III 自然有序)。
    public class BlueprintTargetExtension : DefModExtension
    {
        public string targetTech = "";
        public int tier = 0;                 // 1=中世纪 2=工业 3=太空 4=极致 5=超凡 (0=按 targetTech 时代推断)
        public float researchPoints = 500f;  // 保留字段 (供商人定价/投放权重参考, 不再直接加科研值)
        public int bookIndex = 0;            // 系列序号 (1..N), 0 = 按 defName 排序自动推导

        // ---- v5 数值层 (全部可在 XML 逐本覆盖; 0 / 默认值 = 按 tier 自动推导) ----
        public int minIntellect = 0;         // 阅读所需智力等级下限 (0 = 按 tier: 4/8/12/16/20)
        public float intellectMult = 0f;     // 读书智力经验倍率 (0 = 按 tier: 1.0/1.5/2.0/2.5/3.0)
        public float joyFactor = 0f;         // 娱乐系数 (0 = 全局默认 0.25; 原版小说为 1.0~2.5)
        public float readCost = 0f;          // 读完一本书所需累计 factor (0 = 全局默认 5000 = 一次强制阅读量)
        public bool finishOnUnlock = false;  // 进度满 1.0 时是否直接完成该研究;
                                             // ⚠ 默认 false (2026-09-10 用户定稿): 读满只解锁「研究权限」,
                                             //   玩家仍需正常立项/花科研点研究。true = 读完即直接完成。
        public bool gate = true;             // true = 蓝图未读满就不能研究 (门控);
                                             // false = 只作「并行推进通道」, 不挡原版研究 (孤儿科技用)

        // ---- v5.4 「科技典籍」标记 (2026-09-10) ----
        //  standalone = true 的书是「普通书」外观 (ParentName=Schematic, 原版科技图解书),
        //  从原版刷书途径 (任务奖励书籍堆 / 商队书籍 / 书商) 以极小概率刷出。
        //  与蓝图书的区别: 蓝图书是「读满整个系列才解锁」的门控主路径; 典籍是「读完这一本即解锁」的
        //  独立支路径 —— 两者互不干扰, 谁先满谁解锁 (见 BlueprintUnlockTracker.GetTechProgress 取 max)。
        //  典籍不参与蓝图系列本数平均, 也不进商人/事件投放池 (只由 Patch_BookMakeRandom 投放)。
        public bool standalone = false;

        // ---- v6 「卷池」配置 (2026-09-11) ----
        //  randomVolume = false (默认, 全部蓝图书): 实体书恒绑定「自己那一卷」
        //    = { targetTech, bookIndex (或 defName 推导序号) } —— 143 本零行为变化。
        //  randomVolume = true (科技典籍 / 美狐科技书): 生成时随机绑定 1~2 卷;
        //    ① 先在「候选科技池」里加权随机挑一个科技 (概率大头落在尚未解锁的科技上);
        //    ② 再只从**该科技**的卷列表里取 1~2 个不重复的卷 (硬约束: 不跨科技)。
        public bool randomVolume = false;

        // 候选科技池 (randomVolume=true 时才有意义); 留空 = 只用 targetTech 一个候选。
        //  典籍留空 → 池 = { 自己的 targetTech } → 「随机」只体现在取 1~2 卷;
        //  美狐科技书列举 23 项 → 池 = 那 23 项 → 每本书随机解锁其中一项。
        public List<string> volumeTechs = new List<string>();

        // ---- 全局默认值 (改这里即可调全 mod 手感) ----
        public const float DefaultJoyFactor = 0.25f;   // 娱乐削弱: 原版小说 1.0~2.5 → 蓝图 0.25
        public const float DefaultReadCost = 5000f;    // 一本书的「阅读量」(=JobDriver_Reading 强制阅读 5000 ticks)

        // v5.4: 「科技典籍」从原版刷书途径产出的概率 —— 「刷出一本书, 其中是典籍」的概率。
        //  用户要求「刷出书的概率中的小概率」, 默认 10%。只影响 Patch_BookMakeRandom 一处。
        public const float TechBookLootChance = 0.10f;

        // tier → 智力门槛 (中世纪/工业/太空/极致/超凡)
        public static int GetMinIntellectForTier(int tier)
        {
            switch (tier)
            {
                case 2: return 8;
                case 3: return 12;
                case 4: return 16;
                case 5: return 20;
                default: return 4;
            }
        }

        // tier → 智力经验倍率 (原版 JobDriver_Reading 自带 0.1/tick, 这里给的是总倍率)
        public static float GetIntellectMultForTier(int tier)
        {
            switch (tier)
            {
                case 2: return 1.5f;
                case 3: return 2f;
                case 4: return 2.5f;
                case 5: return 3f;
                default: return 1f;
            }
        }

        public static int GetMinIntellect(BlueprintTargetExtension ext)
        {
            if (ext == null)
            {
                return 0;
            }
            if (ext.minIntellect > 0)
            {
                return ext.minIntellect;
            }
            return GetMinIntellectForTier(GetTier(ext));
        }

        public static float GetIntellectMult(BlueprintTargetExtension ext)
        {
            if (ext == null)
            {
                return 1f;
            }
            if (ext.intellectMult > 0f)
            {
                return ext.intellectMult;
            }
            return GetIntellectMultForTier(GetTier(ext));
        }

        public static float GetJoyFactor(BlueprintTargetExtension ext)
        {
            if (ext != null && ext.joyFactor > 0f)
            {
                return ext.joyFactor;
            }
            return DefaultJoyFactor;
        }

        public static float GetReadCost(BlueprintTargetExtension ext)
        {
            if (ext != null && ext.readCost > 0f)
            {
                return ext.readCost;
            }
            return DefaultReadCost;
        }

        public static BlueprintTargetExtension Get(ThingDef def)
        {
            if (def == null)
            {
                return null;
            }
            return def.GetModExtension<BlueprintTargetExtension>();
        }

        // 分级: Defs 未显式给 tier 时, 按目标科技时代推断 (HSK 部分节点未声明 techLevel 则回退 3)
        public static int GetTier(BlueprintTargetExtension ext)
        {
            if (ext != null && ext.tier > 0)
            {
                return ext.tier;
            }
            if (ext != null && !string.IsNullOrEmpty(ext.targetTech))
            {
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p != null)
                {
                    return TechLevelToTier(p.techLevel);
                }
            }
            return 3;
        }

        private static int TechLevelToTier(TechLevel tl)
        {
            switch (tl)
            {
                case TechLevel.Industrial: return 2;
                case TechLevel.Spacer: return 3;
                case TechLevel.Ultra: return 4;
                case TechLevel.Archotech: return 5;
                default: return 1; // Undefined / Animal / Neolithic / Medieval
            }
        }
    }

    // ============================================================================
    //  门控数据库: 扫描全部蓝图书 ThingDef, 建立 tech → 蓝图书系列 映射
    // ============================================================================
    public static class BlueprintGateDatabase
    {
        private static Dictionary<string, List<ThingDef>> _map;
        private static bool _built;

        public static void Rebuild()
        {
            BisectTrace("REBUILD_enter", null);
            // ⚠ 先建到局部变量, 全部填完再一次性发布 (2026-09-10 闪退修复, 见下方 _built 说明)
            Dictionary<string, List<ThingDef>> map = new Dictionary<string, List<ThingDef>>();
            List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
            BisectTrace("REBUILD_defs", all == null ? "<null>" : all.Count.ToString());
            if (all == null)
            {
                // 极早期 DefDatabase 尚未就绪: 本次不建表, 保持 _built=false, 下次调用再建。
                // (原实现此处会 NRE; 而这个 NRE 会从补丁里逃出去, 所以补上保护。)
                return;
            }
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext == null)
                {
                    continue;
                }
                // v6: 除了 targetTech, 还要把「卷池候选科技」(volumeTechs) 一并登记 ——
                //  美狐科技书只有一个 def 却覆盖 23 项科技, 不登记就查不到它的门控配置。
                Register(map, ext.targetTech, all[i]);
                if (ext.volumeTechs != null)
                {
                    for (int v = 0; v < ext.volumeTechs.Count; v++)
                    {
                        Register(map, ext.volumeTechs[v], all[i]);
                    }
                }
            }
            // ⚠⚠ 必须在「排序之前」就发布 _map 并置 _built = true —— 这就是 2026-09-10 闪退的根因:
            //   排序比较器会调 GetSeriesIndex(); 若某本书 bookIndex<=0, 旧实现会转去调 GetSeries(),
            //   而 GetSeries/FilterByStandalone 见到 _built==false 就**再次** Rebuild(),
            //   此刻 _built 仍未置位 → 无限递归 → StackOverflow → 读档进图直接闪退。
            //   栈溢出不受 try/catch 保护, 所以补丁外层那圈 try/catch 完全挡不住。
            //   发布 + 置位后, 排序期间所有查询都直接用已建好的 _map, 不再重入。
            _map = map;
            _built = true;
            _extForTech = null; // 失效缓存, 下次查询惰性重建
            _volumeCount = null;
            // 按系列序号/defName 排序, 保证 seriesIndex 稳定 (I/II/III/IV)
            foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
            {
                kv.Value.Sort(delegate (ThingDef a, ThingDef b)
                {
                    int ia = GetSeriesIndex(a);
                    int ib = GetSeriesIndex(b);
                    if (ia != ib)
                    {
                        return ia.CompareTo(ib);
                    }
                    return string.CompareOrdinal(a.defName, b.defName);
                });
            }
            BisectTrace("REBUILD_exit", _map == null ? "<null>" : _map.Count.ToString());
        }

        private static void Register(Dictionary<string, List<ThingDef>> map, string tech, ThingDef def)
        {
            if (string.IsNullOrEmpty(tech) || def == null)
            {
                return;
            }
            List<ThingDef> list;
            if (!map.TryGetValue(tech, out list))
            {
                list = new List<ThingDef>();
                map.Add(tech, list);
            }
            if (!list.Contains(def))
            {
                list.Add(def);
            }
        }

        // tech → 门控配置 (gate/finishOnUnlock/卷数).
        //  ⚠ v6 修正: 旧实现取 list[0], 而 list 里可能先排到 standalone 的「科技典籍」
        //  (典籍的 gate/finishOnUnlock 与蓝图不同) → 配置读错。现在优先取**非典籍**书,
        //  只有该科技确实没有蓝图书时才退回典籍。
        private static Dictionary<string, BlueprintTargetExtension> _extForTech;

        public static BlueprintTargetExtension GetExtensionForTech(string techDefName)
        {
            if (!_built)
            {
                Rebuild();
            }
            if (string.IsNullOrEmpty(techDefName) || _map == null)
            {
                return null;
            }
            if (_extForTech == null)
            {
                _extForTech = new Dictionary<string, BlueprintTargetExtension>();
                _volumeCount = new Dictionary<string, int>();
                foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
                {
                    List<ThingDef> list = kv.Value;
                    BlueprintTargetExtension chosen = null;
                    int vols = 0;
                    for (int i = 0; i < list.Count; i++)
                    {
                        BlueprintTargetExtension e = BlueprintTargetExtension.Get(list[i]);
                        if (e == null)
                        {
                            continue;
                        }
                        if (!e.standalone)
                        {
                            vols++; // 卷数 = 非典籍蓝图书本数
                        }
                        if (chosen == null)
                        {
                            chosen = e;
                        }
                        if (!e.standalone)
                        {
                            chosen = e; // 非典籍优先 (典籍的 gate/finishOnUnlock 与蓝图不同)
                            break;
                        }
                    }
                    if (chosen != null)
                    {
                        _extForTech[kv.Key] = chosen;
                    }
                    _volumeCount[kv.Key] = vols > 0 ? vols : 1;
                }
            }
            BlueprintTargetExtension result;
            return _extForTech.TryGetValue(techDefName, out result) ? result : null;
        }

        // 某科技的「卷数」= 该科技的非典籍蓝图书本数; 一本蓝图都没有 (例如美狐 23 项)
        // 则视为 1 卷 (读完那本科技书即解锁该科技)。
        // 每科技的卷数缓存 (Rebuild 时一次算好): 科研树每帧会对几百个节点问这个值,
        // 不能每次都走 GetSeries (那会 FilterByStandalone → 新建 List)。
        private static Dictionary<string, int> _volumeCount;

        public static int GetVolumeCount(string techDefName)
        {
            if (string.IsNullOrEmpty(techDefName))
            {
                return 1;
            }
            if (!_built)
            {
                Rebuild();
            }
            int n;
            if (_volumeCount != null && _volumeCount.TryGetValue(techDefName, out n))
            {
                return n;
            }
            return 1; // 没有蓝图书的科技 (例如美狐 23 项) = 1 卷
        }

        // 某科技全部卷的键列表 ("tech|1" .. "tech|N")
        public static List<string> VolumeKeysForTech(string techDefName)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(techDefName))
            {
                return result;
            }
            int n = GetVolumeCount(techDefName);
            for (int i = 1; i <= n; i++)
            {
                result.Add(VolumeUtil.Key(techDefName, i));
            }
            return result;
        }

        // v6: 给「刚生成的一本书」决定它绑定哪几卷。
        //  返回 List<string> 卷键 (1~2 项, 同一科技); 失败返回空表 (调用方回落单卷)。
        public static List<string> PickVolumesFor(ThingDef bookDef)
        {
            List<string> result = new List<string>();
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(bookDef);
            if (ext == null)
            {
                return result;
            }
            string tech = PickPoolTech(ext);
            if (string.IsNullOrEmpty(tech))
            {
                return result;
            }
            int n = GetVolumeCount(tech);
            // 同一科技内取 1~2 个不重复的卷 (n==1 时自然只有 1 卷)
            int want = 1;
            if (n >= 2 && Rand.Chance(0.3f))
            {
                want = 2;
            }
            List<int> pool = new List<int>();
            for (int i = 1; i <= n; i++)
            {
                pool.Add(i);
            }
            for (int k = 0; k < want && pool.Count > 0; k++)
            {
                int idx = Rand.RangeInclusive(0, pool.Count - 1);
                result.Add(VolumeUtil.Key(tech, pool[idx]));
                pool.RemoveAt(idx);
            }
            return result;
        }

        // 候选科技池里挑一个科技: 概率大头落在「尚未解锁 (未读满)」的科技上。
        //  权重: 未解锁 1.0 / 已解锁 0.12 (留个小尾巴, 满足用户「不必精确」的要求)。
        private static string PickPoolTech(BlueprintTargetExtension ext)
        {
            List<string> pool = new List<string>();
            if (ext.volumeTechs != null && ext.volumeTechs.Count > 0)
            {
                for (int i = 0; i < ext.volumeTechs.Count; i++)
                {
                    string t = ext.volumeTechs[i];
                    if (!string.IsNullOrEmpty(t) && !pool.Contains(t))
                    {
                        pool.Add(t);
                    }
                }
            }
            if (pool.Count == 0 && !string.IsNullOrEmpty(ext.targetTech))
            {
                pool.Add(ext.targetTech);
            }
            if (pool.Count == 0)
            {
                return null;
            }
            if (pool.Count == 1)
            {
                return pool[0];
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            float total = 0f;
            List<float> weights = new List<float>();
            for (int i = 0; i < pool.Count; i++)
            {
                bool unlocked = tracker != null && tracker.IsTechUnlocked(pool[i]);
                float w = unlocked ? 0.12f : 1f;
                weights.Add(w);
                total += w;
            }
            if (total <= 0f)
            {
                return pool[Rand.RangeInclusive(0, pool.Count - 1)];
            }
            float roll = Rand.Value * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    return pool[i];
                }
            }
            return pool[pool.Count - 1];
        }

        // 该科技的「蓝图书系列」(按系列序号排序); 无则空表。
        // v5.4: 排除 standalone 的「科技典籍」—— 典籍是独立单本解锁通道, 不参与系列本数平均,
        //       否则加一本典籍会让原有的 N 本蓝图系列变成 N+1 本, 把已有存档的进度打乱。
        public static List<ThingDef> GetSeries(string techDefName)
        {
            return FilterByStandalone(techDefName, false);
        }

        // 该科技的「科技典籍」(standalone=true 的普通书外观书, 单本读完即解锁)
        public static List<ThingDef> GetStandalone(string techDefName)
        {
            return FilterByStandalone(techDefName, true);
        }

        private static List<ThingDef> FilterByStandalone(string techDefName, bool wantStandalone)
        {
            if (!_built)
            {
                Rebuild();
            }
            List<ThingDef> result = new List<ThingDef>();
            List<ThingDef> list;
            if (_map != null && _map.TryGetValue(techDefName, out list) && list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    BlueprintTargetExtension ext = BlueprintTargetExtension.Get(list[i]);
                    if (ext != null && ext.standalone == wantStandalone)
                    {
                        result.Add(list[i]);
                    }
                }
            }
            return result;
        }

        // 某本蓝图书在系列中的序号 (1..N); XML 未显式给 bookIndex 时按 defName 排序推导
        // ⚠ 2026-09-10 闪退修复: 这里**绝不能再调 GetSeries()** ——
        //   GetSeries → FilterByStandalone 在 _built==false 时会再次 Rebuild(),
        //   而本方法恰恰是从 Rebuild() 的排序比较器里被调用的 → 无限递归 → 栈溢出 → 原生闪退。
        //   改为直接读 _map 的系列列表, 且序号**只由 defName 决定**(与列表此刻的顺序无关),
        //   这样比较器结果稳定, List.Sort 不会因"比较结果漂移"而抛异常。
        public static int GetSeriesIndex(ThingDef def)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            if (ext != null && ext.bookIndex > 0)
            {
                return ext.bookIndex;
            }
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return 1;
            }
            // 外部调用(非 Rebuild 排序中)时保证建表; 排序期间 _built 已为 true, 不会重入
            if (!_built)
            {
                Rebuild();
            }
            List<ThingDef> series;
            if (_map == null || !_map.TryGetValue(ext.targetTech, out series) || series == null)
            {
                return 1;
            }
            // 与 GetSeries 口径一致: 序号只在「非典籍」的书之间推算
            int rank = 1;
            for (int i = 0; i < series.Count; i++)
            {
                ThingDef other = series[i];
                if (other == null || other == def)
                {
                    continue;
                }
                BlueprintTargetExtension otherExt = BlueprintTargetExtension.Get(other);
                if (otherExt != null && otherExt.standalone)
                {
                    continue;
                }
                if (string.CompareOrdinal(other.defName, def.defName) < 0)
                {
                    rank++;
                }
            }
            return rank;
        }

        // 系列总本数 (某科技需读几本才解锁)
        public static int GetSeriesTotal(string techDefName)
        {
            return GetSeries(techDefName).Count;
        }

        public static List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>> AllGated()
        {
            if (!_built)
            {
                Rebuild();
            }
            List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>> result =
                new List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>>();
            if (_map == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
            {
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(kv.Key);
                if (p != null)
                {
                    result.Add(new KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>(
                        p, BlueprintTargetExtension.Get(kv.Value[0])));
                }
            }
            return result;
        }

        // 全部「蓝图书」物品 (供奖励生成器/商人/调试使用)。
        // v5.4: 不含 standalone 的「科技典籍」—— 典籍只由原版刷书途径 (Patch_BookMakeRandom) 投放,
        //       不进蓝图商人/事件奖励池, 保持它的稀有度与「凭运气撞见」的定位。
        public static List<ThingDef> AllBlueprintDefs()
        {
            return AllByStandalone(false);
        }

        // v5.4: 全部「科技典籍」(standalone=true 的普通书外观书)
        public static List<ThingDef> AllTechBooks()
        {
            return AllByStandalone(true);
        }

        private static List<ThingDef> AllByStandalone(bool wantStandalone)
        {
            if (!_built)
            {
                Rebuild();
            }
            List<ThingDef> result = new List<ThingDef>();
            if (_map == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    BlueprintTargetExtension ext = BlueprintTargetExtension.Get(kv.Value[i]);
                    if (ext == null || ext.standalone != wantStandalone)
                    {
                        continue;
                    }
                    if (!result.Contains(kv.Value[i]))
                    {
                        result.Add(kv.Value[i]);
                    }
                }
            }
            return result;
        }

        // v5.4: 挑一本「值得投放的科技典籍」—— 只从「目标科技尚未解锁且尚未研究完成」的池里选。
        //  档位: 优先玩家当前最卡的档位 ±1 (跟得上进度); 该档没候选就放宽到全池。
        //  不排除「前置未完成」的下游科技: 提前拿到下游典籍算囤货, 真正解锁仍由
        //  CheckUnlock 把关 (前置没研究完时进度满也不解锁, 见 BlueprintUnlockTracker.CheckUnlock)。
        public static ThingDef PickRandomTechBook()
        {
            List<ThingDef> all = AllTechBooks();
            if (all.Count == 0)
            {
                return null;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            int tierPref = BlueprintStuckMonitor.MostStuckTier();
            List<ThingDef> near = new List<ThingDef>();
            List<ThingDef> any = new List<ThingDef>();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext == null || string.IsNullOrEmpty(ext.targetTech))
                {
                    continue;
                }
                // v6: volumeTechs 非空 = 「多科技卷池」书 (美狐科技书那种), 不属于本 mod 的
                //  「科技典籍」投放池 —— 它由原 mod 自己的途径产出, 这里必须排掉。
                if (ext.volumeTechs != null && ext.volumeTechs.Count > 0)
                {
                    continue;
                }
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p == null || p.IsFinished)
                {
                    continue;
                }
                if (tracker != null && tracker.IsTechUnlocked(p.defName))
                {
                    continue; // 该科技已解锁 → 这本典籍读不出东西了, 不再投放
                }
                any.Add(all[i]);
                if (tierPref > 0)
                {
                    int t = BlueprintTargetExtension.GetTier(ext);
                    if (t >= tierPref - 1 && t <= tierPref + 1)
                    {
                        near.Add(all[i]);
                    }
                }
            }
            if (tierPref > 0 && near.Count > 0)
            {
                return near.RandomElement();
            }
            if (any.Count > 0)
            {
                return any.RandomElement();
            }
            return null;
        }

        // 某本蓝图书的档位 (显式 tier>0, 否则按目标科技时代推断)
        public static int TierOfBook(ThingDef def)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            return BlueprintTargetExtension.GetTier(ext);
        }

        // 该科技解锁时是否「读书即研究」(直接完成研究); 取系列第一本书的配置。
        // ⚠ 2026-09-10 用户定稿: 默认 **false** —— 读满只「解锁研究权限」(变成可研究),
        //   玩家仍需正常立项、花科研点研究; 不再读完就直接 FinishProject。
        public static bool GetFinishOnUnlock(string techDefName)
        {
            BlueprintTargetExtension ext = GetExtensionForTech(techDefName);
            if (ext == null)
            {
                return false;
            }
            return ext.finishOnUnlock;
        }

        // 该科技是否被蓝图门控 (false = 蓝图只是并行推进通道, 原版研究路线照常可走)
        public static bool IsGated(string techDefName)
        {
            BlueprintTargetExtension ext = GetExtensionForTech(techDefName);
            if (ext == null)
            {
                return false;
            }
            return ext.gate;
        }

        // ---- 二分定位专用: 调用追踪 (仅 -D:BP_BISECT 编译时真正记录, 否则为空方法) ----
        //  用途: 崩溃后翻日志, 看四个门控补丁里「谁在崩溃前被调用过」。
        //  每个 tag 上限 300 条, 防止 AddProgress 这种高频入口把日志刷爆。
#if BP_BISECT
        private static readonly Dictionary<string, int> _bisectTraceCount = new Dictionary<string, int>();

        public static void BisectTrace(string tag, string detail)
        {
            int n;
            if (!_bisectTraceCount.TryGetValue(tag, out n))
            {
                n = 0;
            }
            if (n >= 300)
            {
                return;
            }
            _bisectTraceCount[tag] = n + 1;
            Log.Message("[BlueprintUnlockHSK][TRACE] " + tag + " "
                + (detail == null ? "<null>" : detail) + "  #" + (n + 1));
        }
#else
        public static void BisectTrace(string tag, string detail)
        {
        }
#endif

        // 硬门槛统一判定: 此刻是否「不许该科技产生任何科研进度」。
        // 供 ResearchPerformed / AddProgress / WorkGiver_Researcher 三道补丁共用。
        // 两条都要拦:
        //   ① 前置未全部完成 —— ResearchTreeSK 的科研队列绕过前置直接开研究; 更要命的是
        //      ResearchManager.ResearchPerformed 在进度满时**无条件**调 FinishProject(currentProj),
        //      而 FinishProject 会**递归完成全部前置科技**。也就是说: 只要能把某个下游科技攒到 100%,
        //      它上游那个「蓝图未解锁」的门控科技就会被顺带完成 —— 门控被完全跳过。
        //   ② 蓝图门控且未解锁 —— 本身就不该有进度。
        public static bool BlocksResearch(ResearchProjectDef project)
        {
            if (project == null || project.IsFinished)
            {
                return false; // 已完成的科技已无进度可言
            }
            BisectTrace("BS1_isfinished_ok", project.defName);
            if (!project.PrerequisitesCompleted)
            {
                BisectTrace("BS2_prereq_notdone", project.defName);
                return true; // ① 前置未完成
            }
            BisectTrace("BS2_prereq_ok", project.defName);
            if (!IsGated(project.defName))
            {
                BisectTrace("BS3_notgated", project.defName);
                return false; // 非门控科技 / 并行推进通道
            }
            BisectTrace("BS3_gated", project.defName);
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                BisectTrace("BS4_no_tracker", project.defName);
                return false;
            }
            bool unlocked = tracker.IsTechUnlocked(project.defName);
            BisectTrace("BS4_done", project.defName + " unlocked=" + unlocked);
            return !unlocked; // ② 蓝图未读满
        }

        // ---- 「门控锁沿前置链向下游传播」判定 (v5.2, 只用于科研树 UI 置灰) ----
        // 为什么需要: ResearchTreeSK 的 Node.Available 是它自己算的 ——
        //   Available = TabInfoVisible && !Completed && HiddenPrerequisitesCompleted
        //             && PlayerHasAnyAppropriateResearchBench && TechprintRequirementMet
        //             && MechanitorRequirementMet && AnalyzedThingsRequirementsMet && !IsHidden
        // 它**故意不含 prerequisites** (该 mod 的设计是"点下游节点 → 自动把未完成的前置排进队列"),
        // 所以原版那种"前置没做完就灰着"的效果在科研树里根本不存在:
        //   微电子学(电子 I)被蓝图锁着, 但它的下游 研究技术 III 照样显示成可研究、可排队。
        // 这里把门控补成传递闭包: 自身门控且未解锁 → 锁; 任一(递归)前置被锁 → 也锁。
        // 注意遇到「已解锁但还没研究完」的门控科技即停止传播 —— 那是正常前置,
        // 科研树"点下游自动排队"能正常处理, 不该把下游一并灰掉。
        private static readonly Dictionary<string, bool> _gateLockMemo = new Dictionary<string, bool>();
        private static int _gateLockFrame = -1;

        public static bool IsGateLockedOnPath(ResearchProjectDef project)
        {
            if (project == null)
            {
                return false;
            }
            // 逐帧记忆化: 科研树每次刷新会遍历全部节点, 不能每节点都重走一遍前置链
            if (_gateLockFrame != UnityEngine.Time.frameCount)
            {
                _gateLockMemo.Clear();
                _gateLockFrame = UnityEngine.Time.frameCount;
            }
            return GateLockRecursive(project);
        }

        private static bool GateLockRecursive(ResearchProjectDef project)
        {
            if (project == null)
            {
                return false;
            }
            bool cached;
            if (_gateLockMemo.TryGetValue(project.defName, out cached))
            {
                return cached;
            }
            _gateLockMemo[project.defName] = false; // 占位: 数据异常成环时不至于无限递归
            bool locked = false;
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (IsGated(project.defName) && tracker != null && !tracker.IsTechUnlocked(project.defName))
            {
                locked = true; // 自身就是「蓝图书没读满的门控科技」
            }
            else
            {
                List<ResearchProjectDef> normal = project.prerequisites;
                for (int i = 0; normal != null && i < normal.Count; i++)
                {
                    if (GateLockRecursive(normal[i]))
                    {
                        locked = true;
                        break;
                    }
                }
                if (!locked)
                {
                    List<ResearchProjectDef> hidden = project.hiddenPrerequisites;
                    for (int j = 0; hidden != null && j < hidden.Count; j++)
                    {
                        if (GateLockRecursive(hidden[j]))
                        {
                            locked = true;
                            break;
                        }
                    }
                }
            }
            _gateLockMemo[project.defName] = locked;
            return locked;
        }

        // 兼容旧调用: 不限档, 卡节点权重 0.8
        public static ThingDef PickRandomBlueprint()
        {
            return PickRandomBlueprint(0, false, 0.8f);
        }

        // tierPref: 0=不限档, >0 只在该档内选。
        // forceStuck: true 时只要有卡点就必定从卡点里选 (委托保底); 否则按 stuckWeight 概率优先卡点。
        // stuckWeight: 非强制时, 有多大概率优先「当前被卡住」的节点。
        public static ThingDef PickRandomBlueprint(int tierPref, bool forceStuck, float stuckWeight)
        {
            List<ThingDef> all = AllBlueprintDefs();
            if (tierPref > 0)
            {
                List<ThingDef> filtered = new List<ThingDef>();
                for (int i = 0; i < all.Count; i++)
                {
                    if (TierOfBook(all[i]) == tierPref)
                    {
                        filtered.Add(all[i]);
                    }
                }
                if (filtered.Count > 0)
                {
                    all = filtered;
                }
            }
            if (all.Count == 0)
            {
                return null;
            }
            List<ThingDef> stuck = BlueprintStuckMonitor.GetStuck();
            if (tierPref > 0)
            {
                List<ThingDef> stuckT = new List<ThingDef>();
                for (int i = 0; i < stuck.Count; i++)
                {
                    if (TierOfBook(stuck[i]) == tierPref)
                    {
                        stuckT.Add(stuck[i]);
                    }
                }
                stuck = stuckT;
            }
            float bias = forceStuck ? 1f : stuckWeight;
            if (stuck.Count > 0 && Rand.Value < bias)
            {
                return stuck[Rand.RangeInclusive(0, stuck.Count - 1)];
            }
            List<ThingDef> usable = new List<ThingDef>();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p != null && PrereqsFinished(p))
                {
                    usable.Add(all[i]);
                }
            }
            List<ThingDef> pool = (Rand.Value < 0.7f && usable.Count > 0) ? usable : all;
            return pool[Rand.RangeInclusive(0, pool.Count - 1)];
        }

        // 前置科技是否全部已研究 (不含蓝图自身, 与 CanStartNow 类似)
        public static bool PrereqsFinished(ResearchProjectDef project)
        {
            if (project.prerequisites != null)
            {
                for (int i = 0; i < project.prerequisites.Count; i++)
                {
                    if (project.prerequisites[i] != null && !project.prerequisites[i].IsFinished)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    // ---- 卡节点检测 (动态概率依据, 低频刷新, 性能铁律) ----
    // 「卡住」= 某门控科技前置已研究 (可研究) 但蓝图系列未集齐, 玩家被卡进度。
    // 刷新时机: 研读完成/事件生成时经 GetStuck() 惰性重建, 且 600 ticks 节流一次, 绝不每 tick 全量遍历。
    public static class BlueprintStuckMonitor
    {
        private static List<ThingDef> _stuck = new List<ThingDef>();
        private static int _asOf = -100000;
        private static bool _dirty = true;

        public static void MarkDirty()
        {
            _dirty = true;
        }

        public static bool HasStuck()
        {
            return GetStuck().Count > 0;
        }

        // 玩家最「卡」的档位: 统计各档未读卡点书数量, 取最多者 (并列取低档)。无卡点返回 0。
        public static int MostStuckTier()
        {
            List<ThingDef> stuck = GetStuck();
            if (stuck.Count == 0)
            {
                return 0;
            }
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < stuck.Count; i++)
            {
                int tier = BlueprintGateDatabase.TierOfBook(stuck[i]);
                int c;
                counts.TryGetValue(tier, out c);
                counts[tier] = c + 1;
            }
            int bestTier = 0;
            int bestCount = -1;
            foreach (KeyValuePair<int, int> kv in counts)
            {
                if (kv.Value > bestCount || (kv.Value == bestCount && kv.Key < bestTier))
                {
                    bestCount = kv.Value;
                    bestTier = kv.Key;
                }
            }
            return bestTier;
        }

        public static List<ThingDef> GetStuck()
        {
            int tick = GenTicks.TicksGame;
            if (_dirty || tick - _asOf >= 600)   // 最多每 600 ticks(10s) 重建一次
            {
                Rebuild();
                _asOf = tick;
                _dirty = false;
            }
            return _stuck;
        }

        private static void Rebuild()
        {
            _stuck = new List<ThingDef>();
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return;
            }
            List<ThingDef> all = BlueprintGateDatabase.AllBlueprintDefs();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext == null || string.IsNullOrEmpty(ext.targetTech))
                {
                    continue;
                }
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p == null || p.IsFinished)
                {
                    continue;
                }
                if (!BlueprintGateDatabase.IsGated(p.defName))
                {
                    continue; // 非门控科技: 原版路线照常可走, 不算"卡住"
                }
                if (tracker.IsTechUnlocked(p.defName))
                {
                    continue;
                }
                if (!BlueprintGateDatabase.PrereqsFinished(p))
                {
                    continue; // 前置未研究, 尚未到"卡住"阶段
                }
                _stuck.Add(all[i]);
            }
        }
    }

    // ============================================================================
    //  存档状态 (WorldComponent): 已读「卷」+ 已解锁科技   (v6, 2026-09-11)
    // ============================================================================
    //  v5 的模型是「书目 → 进度 0..1」; v6 换成「书 → 绑定 1~2 卷; 世界层只记已读卷」:
    //    · 书的绑定与本地进度存在**书实例**上 (BlueprintBook.boundVolumes / localProgress),
    //      因为同一本典籍的两个副本可以绑定不同科技的卷 (随机);
    //    · 世界层只保存「哪些卷已经读毕」= readVolumes (键 "tech|vol");
    //    · 某科技 N 卷全读毕 == 该科技解锁 (IsTechUnlocked 自动成立, 不需要显式记账)。
    public class BlueprintUnlockTracker : WorldComponent
    {
        // 已读毕的卷, 键格式 "techDefName|volumeIndex"
        public List<string> readVolumes = new List<string>();
        // 已解锁科技 (显式记账: 播报/FinishProject 只跑一次; 语义已被 readVolumes 覆盖)
        public List<string> unlocked = new List<string>();

        // readVolumes 的查询缓存 (WorldComponent 不是热路径, 但 IsTechUnlocked 会被科研树每帧调用)
        private HashSet<string> _readSet;
        private int _sweepTick = -100000;

        // 蓝图委托: 玩家在委托台发布后, 下一次生成的藏宝处(ItemStash)必须保底投放一本
        // 「优先卡节点」的蓝图书; pendingCommissionTier = 玩家最卡的档位 (0=不限档)。
        public bool pendingCommission = false;
        public int pendingCommissionTier = 0;

        public BlueprintUnlockTracker(World world) : base(world) { }

        private HashSet<string> ReadSet
        {
            get
            {
                if (_readSet == null)
                {
                    _readSet = new HashSet<string>();
                    if (readVolumes != null)
                    {
                        for (int i = 0; i < readVolumes.Count; i++)
                        {
                            if (readVolumes[i] != null)
                            {
                                _readSet.Add(readVolumes[i]);
                            }
                        }
                    }
                }
                return _readSet;
            }
        }

        // ---- 卷 ----
        public bool IsVolumeRead(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            return ReadSet.Contains(key);
        }

        public int GetReadVolumeCount(string techDefName)
        {
            if (string.IsNullOrEmpty(techDefName))
            {
                return 0;
            }
            int n = BlueprintGateDatabase.GetVolumeCount(techDefName);
            int c = 0;
            for (int i = 1; i <= n; i++)
            {
                if (IsVolumeRead(VolumeUtil.Key(techDefName, i)))
                {
                    c++;
                }
            }
            return c;
        }

        // 某科技该读几卷
        public int GetVolumeCount(string techDefName)
        {
            return BlueprintGateDatabase.GetVolumeCount(techDefName);
        }

        // ---- 进度读取 ----
        // 科技解锁进度 0..1 = 已读卷数 / 总卷数
        public float GetTechProgress(string techDefName)
        {
            if (string.IsNullOrEmpty(techDefName))
            {
                return 0f;
            }
            if (unlocked != null && unlocked.Contains(techDefName))
            {
                return 1f;
            }
            int total = BlueprintGateDatabase.GetVolumeCount(techDefName);
            if (total <= 0)
            {
                return 0f;
            }
            float v = (float)GetReadVolumeCount(techDefName) / (float)total;
            return v > 1f ? 1f : v;
        }

        // ---- 进度累加 (由 BlueprintBook.AddLocalProgress 在读完「一卷」时调用) ----
        //  bookTitle 仅用于播报文案。
        public void MarkVolumeRead(string key, string bookTitle)
        {
            if (!VolumeUtil.IsVolumeKey(key))
            {
                return;
            }
            if (IsVolumeRead(key))
            {
                return; // 已记账, 幂等
            }
            if (readVolumes == null)
            {
                readVolumes = new List<string>();
            }
            readVolumes.Add(key);
            _readSet = null; // 失效缓存
            BlueprintStuckMonitor.MarkDirty();

            string tech = VolumeUtil.TechOf(key);
            int vol = VolumeUtil.VolumeOf(key);
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(tech);
            int total = BlueprintGateDatabase.GetVolumeCount(tech);
            int have = GetReadVolumeCount(tech);
            if (p != null)
            {
                Messages.Message("RK_VolumeReadMessage".Translate(
                    bookTitle == null ? "" : bookTitle, p.label, have, total),
                    MessageTypeDefOf.NeutralEvent, true);
            }
            CheckUnlock(tech);
        }

        // 「全卷读毕」→ 记账 + 播报 (+ 可选直接完成研究); 前置未完成时先不记账, 由
        // WorldComponentTick 的 SweepPendingUnlocks 在前置补齐后自动补上。
        public void CheckUnlock(string techDefName)
        {
            if (string.IsNullOrEmpty(techDefName) || IsTechUnlocked(techDefName))
            {
                return;
            }
            int total = BlueprintGateDatabase.GetVolumeCount(techDefName);
            if (total <= 0 || GetReadVolumeCount(techDefName) < total)
            {
                return; // 还没读满
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(techDefName);
            if (p == null || !BlueprintGateDatabase.PrereqsFinished(p))
            {
                return; // 前置没研究完 → 留着, 之后自动补
            }
            Unlock(techDefName);
            BlueprintTreeRefresh.IfOpen(); // 科研树若开着 → 立刻解除该节点及其下游的"锁"

            // 「读完直接完成研究」= 并行通道书 (gate=false) 的语义;
            // 门控书 (gate=true) 读满只解锁「研究权限」, 还得玩家自己立项花科研点 → 播报用词必须不同。
            bool directFinish = BlueprintGateDatabase.GetFinishOnUnlock(techDefName);
            if (directFinish)
            {
                Messages.Message("RK_VolumeUnlockMessage".Translate(p.label, total),
                    MessageTypeDefOf.PositiveEvent, true);
            }
            else
            {
                Messages.Message("RK_VolumeUnlockCanResearch".Translate(p.label, total),
                    MessageTypeDefOf.PositiveEvent, true);
            }
            if (directFinish && !p.IsFinished)
            {
                // 读书即研究: 直接完成该科技 (不弹原版完成对话框/信件, 用上面的 Message 播报)
                try
                {
                    Find.ResearchManager.FinishProject(p, false, null, false);
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] FinishProject 失败: " + techDefName + " " + e.Message);
                }
                BlueprintTreeRefresh.IfOpen();
            }
        }

        // 前置补齐后, 把「卷已读满但当时前置没完成」的科技补上 (低频, 250 ticks)
        private void SweepPendingUnlocks()
        {
            List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>> all = BlueprintGateDatabase.AllGated();
            for (int i = 0; i < all.Count; i++)
            {
                ResearchProjectDef p = all[i].Key;
                if (p == null || IsTechUnlocked(p.defName))
                {
                    continue;
                }
                int total = BlueprintGateDatabase.GetVolumeCount(p.defName);
                if (total > 0 && GetReadVolumeCount(p.defName) >= total)
                {
                    CheckUnlock(p.defName);
                }
            }
        }

        public bool IsTechUnlocked(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return false;
            }
            if (unlocked != null && unlocked.Contains(defName))
            {
                return true;
            }
            // 全卷读毕 = 解锁 (与 CheckUnlock 同口径, 避免"读满了却还灰着")
            int total = BlueprintGateDatabase.GetVolumeCount(defName);
            return total > 0 && GetReadVolumeCount(defName) >= total;
        }

        public void Unlock(string defName)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return;
            }
            if (unlocked == null)
            {
                unlocked = new List<string>();
            }
            if (!unlocked.Contains(defName))
            {
                unlocked.Add(defName);
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int tick = GenTicks.TicksGame;
            if (tick - _sweepTick < 250)
            {
                return;
            }
            _sweepTick = tick;
            try
            {
                SweepPendingUnlocks();
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[BlueprintUnlockHSK] 解锁补齐扫描失败: " + e.Message, 0x5B1D9A);
            }
        }

        public static BlueprintUnlockTracker Get()
        {
            if (Find.World == null)
            {
                return null; // 世界尚未建立 (主菜单/世界生成中)
            }
            return Find.World.GetComponent<BlueprintUnlockTracker>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<string>(ref readVolumes, "readVolumes", LookMode.Value);
            Scribe_Collections.Look<string>(ref unlocked, "unlocked", LookMode.Value);
            Scribe_Values.Look<bool>(ref pendingCommission, "pendingCommission", false);
            Scribe_Values.Look<int>(ref pendingCommissionTier, "pendingCommissionTier", 0);
            if (readVolumes == null)
            {
                readVolumes = new List<string>();
            }
            if (unlocked == null)
            {
                unlocked = new List<string>();
            }
            _readSet = null;
        }
    }

    // ============================================================================
    //  蓝图书本体 (Book 子类) — 固定书名/描述, 不做 grammar 随机
    // ============================================================================
    //  原版 Book.LabelNoCount 返回 grammar 生成的 title (忽略 def.label), 需要
    //  nameMaker; 蓝图书要求每本固定 label (系列 I/II/III 等), 故子类直接以
    //  def.label 为书名, GenerateBook 置空 (不做随机生成, 也不崩 title=null)。
    //  右键追加「研读」选项 (强制指派走科研工作链路)。
    public class BlueprintBook : Book
    {
        // ---- 已读/未读 贴图切换 (2026-09-09) ----
        //  蓝图 def 默认贴图 (XML) = 该科技档 _Unread (卷轴); 读过后 Graphic / UIIconOverride
        //  改指同名 _Read (展开图)。readTexPath 由 def.graphicData.texPath 把 "_Unread" 换成
        //  "_Read" 推导, 无需在代码里硬编档序。Thing.Graphic 与 Thing.UIIconOverride 均为
        //  virtual, 直接重写即覆盖地图渲染 + 所有走 Widgets.GetIconFor 的 UI 图标 (背包/搜索/
        //  查看卡/商队), 不需要 Harmony。isReadCache 只 false→true 单向, 命中后停止查表。
        private bool isReadCache;
        private Graphic readGraphicCache;
        private bool readGraphicResolved;
        private Texture2D readTexCache;
        private bool readTexResolved;

        // ---- v6 「这本书绑定哪几卷」+ 本书本地阅读进度 (2026-09-11) ----
        //  绑定与进度都存**实例**上: 同一本典籍的两个副本可以绑定不同科技的卷 (随机),
        //  世界层 (BlueprintUnlockTracker) 只记「哪些卷已读毕」。
        //  卷键 = "techDefName|volumeIndex"; localProgress ∈ [0, boundVolumes.Count],
        //  每跨过一个整数 → 该卷读毕, 记进世界层。
        public List<string> boundVolumes;
        public float localProgress;

        private bool volumesResolved;

        // 惰性决定绑定 (只算一次)。幂等, 可在 PostPostMake / SpawnSetup / 读档收尾各处调。
        public void EnsureVolumes()
        {
            // 已经绑定好 (新建时掷过 / 存档里读回来) → 绝不重掷
            if (boundVolumes != null && boundVolumes.Count > 0)
            {
                volumesResolved = true;
                return;
            }
            if (volumesResolved)
            {
                return; // 已经试过且确定绑不上 (不是本 mod 的书) → 不反复重试
            }
            // ⚠ Scribe 读档前两轮 (LoadingVars / ResolvingCrossRefs) 期间绝不写字段:
            //  此时 Scribe 正在填/解析 boundVolumes, 抢先写会撞 key 并把对象图写坏
            //  (Boom GC HEAP_CORRUPTION, 读档进图即闪退)。详见 §Patch_BookExposeData 的说明。
            if (Scribe.mode != LoadSaveMode.Inactive && Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return;
            }
            volumesResolved = true;
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            if (ext == null)
            {
                return;
            }
            List<string> picked = null;
            if (ext.randomVolume)
            {
                picked = BlueprintGateDatabase.PickVolumesFor(def);
            }
            if (picked == null || picked.Count == 0)
            {
                // 固定单卷: 蓝图书恒绑定 { targetTech, bookIndex (缺省按 defName 推导) }
                if (!string.IsNullOrEmpty(ext.targetTech))
                {
                    int idx = BlueprintGateDatabase.GetSeriesIndex(def);
                    if (idx <= 0)
                    {
                        idx = 1;
                    }
                    picked = new List<string>();
                    picked.Add(VolumeUtil.Key(ext.targetTech, idx));
                }
            }
            if (picked != null && picked.Count > 0)
            {
                boundVolumes = picked;
            }
        }

        // 这本书关联的科技 (第一项; 兼容只认单科技的旧逻辑)
        public string BoundTech
        {
            get
            {
                EnsureVolumes();
                if (boundVolumes == null || boundVolumes.Count == 0)
                {
                    return null;
                }
                return VolumeUtil.TechOf(boundVolumes[0]);
            }
        }

        // 本书是否已经全部读毕 (所有绑定卷都读完)
        public bool IsFullyRead
        {
            get
            {
                EnsureVolumes();
                if (boundVolumes == null || boundVolumes.Count == 0)
                {
                    return false;
                }
                return localProgress >= boundVolumes.Count;
            }
        }

        // 本书本地进度 (0..1, 用于「研读 X%」标记)
        public float LocalProgress01
        {
            get
            {
                EnsureVolumes();
                int n = (boundVolumes == null ? 0 : boundVolumes.Count);
                if (n <= 0)
                {
                    return 0f;
                }
                float v = localProgress / (float)n;
                return v > 1f ? 1f : v;
            }
        }

        // 每 tick 由 BlueprintBookDoer.OnReadingTick 调用; delta 归一化后 = factor / readCost
        public void AddLocalProgress(float delta)
        {
            if (delta <= 0f)
            {
                return;
            }
            EnsureVolumes();
            int n = (boundVolumes == null ? 0 : boundVolumes.Count);
            if (n <= 0 || localProgress >= n)
            {
                return; // 没绑定 / 本书已读毕
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return;
            }
            float prev = localProgress;
            float next = prev + delta;
            if (next > n)
            {
                next = n;
            }
            localProgress = next;
            string title = BookTitle();
            for (int k = 0; k < n; k++)
            {
                float threshold = k + 1;
                if (prev < threshold && next >= threshold)
                {
                    tracker.MarkVolumeRead(boundVolumes[k], title);
                }
            }
            if (prev < n && next >= n)
            {
                isReadCache = true; // 贴图切到「已读」版
            }
        }

        private string BookTitle()
        {
            if (def == null)
            {
                return "";
            }
            return DisplayLabel;
        }

        // ---- 娱乐削弱 (v5): 蓝图是工具书, 解闷效率只有原版小说的 1/4 ----
        //  原版 ReadingOutcomeDoerJoyFactorModifier 会在 OnBookGenerated 里按品质设 1.2~2.5,
        //  但本类 override 了 GenerateBook(固定书名, 不做 grammar), 那个 doer 不会跑, 也不该跑,
        //  所以在这里显式压低。创建 / 存档载入 / 出图 三条路径都覆盖到。
        private void ApplyJoyFactor()
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return;
            }
            SetJoyFactor(BlueprintTargetExtension.GetJoyFactor(ext));
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            EnsureVolumes(); // 造出来那一刻就绑定好卷 (随机只掷一次, 存档里固定)
            ApplyJoyFactor();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            EnsureVolumes();
            ApplyJoyFactor();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<string>(ref boundVolumes, "RK_boundVolumes", LookMode.Value);
            Scribe_Values.Look<float>(ref localProgress, "RK_localProgress", 0f);
            if (boundVolumes == null)
            {
                boundVolumes = new List<string>();
            }
            // 旧档 (v4) 里 joyFactor 从未被赋值 → 存档读出 0, 载入后补正
            // v6: boundVolumes 是本版新增字段, 老存档的书读出空表 → 读档收尾时补绑
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureVolumes(); // 老存档的书 boundVolumes 为空 → 这里补绑; 已绑定的直接返回
                ApplyJoyFactor();
            }
        }

        private bool NowRead()
        {
            if (isReadCache)
            {
                return true;
            }
            if (IsFullyRead)
            {
                isReadCache = true;
            }
            return isReadCache;
        }

        // 已读贴图路径: def 不变, 解析一次即可。
        // 没有 "_Unread" 后缀 (例如「科技典籍」直接用原版 Schematic 贴图) → 返回 null,
        // Graphic / UIIcon 回退原版, 不做已读贴图切换 (典籍靠状态标记 + 已解锁角标区分)。
        private string readTexPathCache;

        private string ReadTexPath()
        {
            if (!readTexResolved)
            {
                readTexResolved = true;
                GraphicData gd = def.graphicData;
                if (gd != null && gd.texPath != null && gd.texPath.Contains("_Unread"))
                {
                    readTexPathCache = gd.texPath.Replace("_Unread", "_Read");
                }
            }
            return readTexPathCache;
        }

        public override Graphic Graphic
        {
            get
            {
                if (NowRead())
                {
                    if (!readGraphicResolved)
                    {
                        readGraphicResolved = true;
                        string p = ReadTexPath();
                        if (p != null && def.graphicData != null)
                        {
                            Color c = def.graphicData.color;
                            readGraphicCache = GraphicDatabase.Get<Graphic_Single>(p, ShaderDatabase.Cutout, def.graphicData.drawSize, c, c);
                        }
                    }
                    if (readGraphicCache != null)
                    {
                        return readGraphicCache;
                    }
                }
                return base.Graphic;
            }
        }

        public override Texture UIIconOverride
        {
            get
            {
                if (NowRead())
                {
                    if (!readTexResolved)
                    {
                        ReadTexPath();
                    }
                    if (readTexCache == null && readTexPathCache != null)
                    {
                        readTexCache = ContentFinder<Texture2D>.Get(readTexPathCache, false);
                    }
                    return readTexCache;
                }
                return null;
            }
        }

        // 显示名 (v5.4 / v6): 
        //  · 「科技典籍」(standalone, 随机卷池) —— 按**实例实际绑定的科技**动态拼
        //    「科技典籍·<科技名>」: 同一个典籍 def 的副本可能绑定不同科技, 用 def 的 targetTech
        //    会显示错。这样 XML 里也不必为每个科技写一份中文 label。
        //  · 蓝图书: 绑定固定, 直接用 def.label。
        public string DisplayLabel
        {
            get
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
                if (ext != null && ext.randomVolume)
                {
                    string tech = BoundTech;
                    if (!string.IsNullOrEmpty(tech))
                    {
                        ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(tech);
                        string techLabel = p != null ? p.label : tech;
                        if (ext.standalone)
                        {
                            return "科技典籍\u00b7" + techLabel;
                        }
                        return def.label + "\u00b7" + techLabel;
                    }
                }
                return def.label;
            }
        }

        public override string LabelNoCount
        {
            get
            {
                return DisplayLabel + StateMarker + GenLabel.LabelExtras(this, true, true);
            }
        }

        // 状态标记 (v5.4 顺序调整): 先〔已解锁 / 未解锁〕(目标科技层面), 再〔已读 / 研读 X% / 未读〕(本书层面)。
        private string StateMarker
        {
            get
            {
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                if (tracker == null)
                {
                    return "";
                }
                string s = "";
                string tech = BoundTech;
                if (!string.IsNullOrEmpty(tech))
                {
                    s += tracker.IsTechUnlocked(tech) ? "\u3014已解锁\u3015" : "\u3014未解锁\u3015";
                }
                float pr = LocalProgress01;
                if (pr >= 1f)
                {
                    s += "\u3014已读\u3015";
                }
                else if (pr > 0.005f)
                {
                    s += string.Format("\u3014研读 {0}%\u3015", Mathf.RoundToInt(pr * 100f));
                }
                else
                {
                    s += "\u3014未读\u3015";
                }
                return s;
            }
        }

        public override string LabelNoParenthesis
        {
            get
            {
                return DisplayLabel;
            }
        }

        public override string DescriptionDetailed
        {
            get
            {
                return def.description;
            }
        }

        public override void GenerateBook(Pawn author = null, long? fixedDate = null)
        {
            // 固定书名/描述: 不做任何 grammar 随机 (title 保持 null, LabelNoCount 不依赖它)
        }

        // v5.4: 地图上的「已解锁」角标 —— 已解锁科技对应的书 (蓝图 / 典籍) 右下角叠一个绿勾,
        //  一眼看出哪本已经没用了。UI 列表图标上的同款角标见 Patch_ThingIcon。
        //  DrawGUIOverlay 只在地图 GUI 层调用, 异常不能逃逸 (会破坏后续绘制), 全包 try/catch。
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            try
            {
                if (!BlueprintBookIcons.ShouldBadge(this))
                {
                    return;
                }
                BlueprintBookIcons.DrawUnlockedBadgeAt(GenMapUI.LabelDrawPosFor(this, -0.55f), 14f);
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[BlueprintUnlockHSK] 书籍已解锁角标绘制失败: " + e.Message, 0x5B1D2E);
            }
        }

        // v5: 不再追加自写「研读」右键项 — 阅读走原版路径:
        //   Book.GetFloatMenuOptions 自带「指派阅读」(JobDefOf.Reading, job.playerForced=true),
        //   自由活动时由 JoyGiver_Read 自动派书, 两条路都受智力门槛拦截。
    }

    // ============================================================================
    //  书籍「已解锁」角标 (v5.4) — 地图 overlay 与 UI 列表图标共用
    // ============================================================================
    //  判定: 这本挂了 BlueprintTargetExtension 的书, 其目标科技**已解锁** → 书已无研读价值,
    //        在图标右下角叠一个绿底白勾, 玩家一眼扫出「哪些书已经用完, 可以丢/卖」。
    //  绘制: 纯代码画方块 + ✓ (不新增贴图资源, 不依赖任何 mod 的图集)。
    public static class BlueprintBookIcons
    {
        private const float SizeRatio = 0.4f;   // 角标边长 = 图标短边 × 该比例 (夹在 7~13px)
        private const float SizeMin = 7f;
        private const float SizeMax = 13f;

        private static readonly Color BadgeColor = new Color(0.22f, 0.62f, 0.25f, 0.92f);

        // 该物品是否值得画「已解锁」角标。
        // 先判类型 (O(1)) 再查 modExtension —— Widgets.ThingIcon 每帧会画大量物品图标, 顺序不能反。
        public static bool ShouldBadge(Thing thing)
        {
            if (!(thing is Book))
            {
                return false;
            }
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(thing.def);
            if (ext == null)
            {
                return false;
            }
            // v6: 随机卷池的书 (典籍 / 美狐科技书) 每本实例绑定的科技可能不同 → 用实例的
            string tech = ext.targetTech;
            if (ext.randomVolume)
            {
                BlueprintBook bb = thing as BlueprintBook;
                if (bb != null)
                {
                    tech = bb.BoundTech;
                }
            }
            if (string.IsNullOrEmpty(tech))
            {
                return false;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            return tracker != null && tracker.IsTechUnlocked(tech);
        }

        // 地图 GUI 坐标锚点 (GenMapUI.LabelDrawPosFor 的返回值) → 角标贴在锚点右上
        public static void DrawUnlockedBadgeAt(Vector2 anchor, float iconSize)
        {
            float s = ClampSize(iconSize * SizeRatio);
            DrawBadge(new Rect(anchor.x + s * 0.4f, anchor.y - s, s, s));
        }

        // UI 图标矩形 → 角标贴在右下角
        public static void DrawUnlockedBadge(Rect iconRect)
        {
            float shorter = iconRect.width < iconRect.height ? iconRect.width : iconRect.height;
            float s = ClampSize(shorter * SizeRatio);
            DrawBadge(new Rect(iconRect.xMax - s - 1f, iconRect.yMax - s - 1f, s, s));
        }

        private static float ClampSize(float s)
        {
            if (s < SizeMin) { return SizeMin; }
            if (s > SizeMax) { return SizeMax; }
            return s;
        }

        private static void DrawBadge(Rect r)
        {
            Color oldColor = GUI.color;
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            try
            {
                GUI.color = BadgeColor;
                GUI.DrawTexture(r, BaseContent.WhiteTex);
                GUI.color = Color.white;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(r, "\u2713"); // ✓
            }
            finally
            {
                // 绘制可能发生在 BeginScrollView 内部, 无论如何都要还原全局 GUI 状态
                GUI.color = oldColor;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
        }
    }

    // ---- UI 列表里书籍图标的「已解锁」角标 ----
    //  背包 / 书架 ITab / 交易 / 搜索 等所有走 Widgets.ThingIcon 画物品图标处统一生效。
    public static class Patch_ThingIcon
    {
        private static bool warned;

        public static void Postfix(Rect __0, Thing __1)
        {
            try
            {
                if (!BlueprintBookIcons.ShouldBadge(__1))
                {
                    return;
                }
                BlueprintBookIcons.DrawUnlockedBadge(__0);
            }
            catch (System.Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("[BlueprintUnlockHSK] ThingIcon 已解锁角标绘制失败(只报一次): " + e.Message);
                }
            }
        }
    }

    // ---- v5.5 「科研节点标识」: 让 UI 把我们的蓝图/典籍也当「科技书」画小图标 ----
    //  线索来源 (2026-09-10 实测): 交易列表里美狐的 Schematic 类书, 名字右侧会多出一个
    //  绿色小书图标 —— 那是 Dynamic Trade Interface 的 MoreIconsDef(BookIconDef) 画的。
    //  它的 BookDrawable.GetIcons 判定很窄 (反编译确认):
    //      book = transferable.AnyThing as Book
    //      doers = book.BookComp.Doers.OfType<ReadingOutcomeDoerGainResearch>()
    //      projects = 反射读 doer 的 values 字段 (Dictionary<ResearchProjectDef, float>) 的 Keys
    //      researchCount > 0 才画: icon = 绿色书,
    //                              color = 全完成绿 / 部分黄 / 全未完成红
    //      tooltip = "已完成数/总数 projects researched."
    //  也就是说: 书只要挂着原版 ReadingOutcomeDoerGainResearch 且 values 非空, 图标就出现,
    //  颜色还能自动跟着「该科技研究完成了没」变 —— 正是要的「科研节点标识」。
    //
    //  难点: 原版 doer 的项目表是「书生成时一次性算好」的, 来源是 XML 的 include / tabs 静态清单。
    //  而我们有 143 个蓝图 def 共用基类 comps, 静态 include 写不出「逐本不同的科技」。
    //  做法: XML 只挂一个空 doer, 生成完成后由本补丁按 book.def 的 targetTech 覆盖 values。
    //  逐本精确: 每个 Thing 有各自的 doer 实例, book.def 天然区分, 一本一个准。
    //
    //  副作用: 这个 doer 也会让读书顺带加目标科技的研究进度
    //  (OnReadingTick → Find.ResearchManager.AddProgress)。但该入口在 v5.1 就已被门控接管
    //  (Patch_ResearchAddProgress): 科技没解锁 → 直接拦掉 → 上锁的书实际零收益;
    //  科技已解锁时才会正常加速, 与原版 Schematic 手感一致。
    public static class Patch_GainResearchTargeting
    {
        private static FieldInfo valuesField;

        // OnBookGenerated 只在书生成时跑一次 (Book.PostPostMake / PostQualitySet → GenerateBook),
        // 不是热路径; 但 parent/def 仍可能为空 (其它 mod 造的裸 doer), 所以全程空安全。
        public static void OnBookGenerated_Postfix(ReadingOutcomeDoerGainResearch __instance)
        {
            try
            {
                Ensure(__instance);
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[BlueprintUnlockHSK] 科技书图标标注失败: " + e.Message, 0x5B17001);
            }
        }

        // 幂等: 只在「values 里没有本科技」时才重写 ——
        //   · 新书生成: 原版按空 include 回退填了随机 1~2 项 → 不含目标 → 重写
        //   · 老存档的书: values 是空的 → 重写
        //   · 已经标注好的书: 读档/存档会反复经过这里 → 直接跳过
        public static void Ensure(ReadingOutcomeDoerGainResearch doer)
        {
            if (doer == null)
            {
                return;
            }
            // ⚠ 时序守卫 (2026-09-10 闪退修复): values 是 Scribe_Collections 托管的字典,
            //  读档过程中(LoadingVars/ResolvingCrossRefs)由 Scribe 自己在填/解析 cross-ref;
            //  此刻若被本方法 Clear+重写, 会与 BuildDictionary 撞 key 并把对象图写坏
            //  (Boom GC HEAP_CORRUPTION, 读档进图即闪退)。只允许在
            //  「常规游戏(Inactive)」或「读档收尾(PostLoadInit)」两个安全时刻动它。
            if (Scribe.mode != LoadSaveMode.Inactive && Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return;
            }
            Book book = doer.Book;
            if (book == null || book.def == null)
            {
                return;
            }
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(book.def);
            if (ext == null)
            {
                return; // 不是本 mod 的书 → 完全不碰 (美狐等其它书保持原版行为)
            }
            // v6: 以**实例实际绑定的科技**为准 (典籍/美狐书每本书都可能不同),
            //  取该书全部绑定卷去重后的科技集合。
            BlueprintBook bbook = book as BlueprintBook;
            List<string> techNames = new List<string>();
            if (bbook != null)
            {
                bbook.EnsureVolumes();
                if (bbook.boundVolumes != null)
                {
                    for (int i = 0; i < bbook.boundVolumes.Count; i++)
                    {
                        string t = VolumeUtil.TechOf(bbook.boundVolumes[i]);
                        if (!string.IsNullOrEmpty(t) && !techNames.Contains(t))
                        {
                            techNames.Add(t);
                        }
                    }
                }
            }
            if (techNames.Count == 0 && !string.IsNullOrEmpty(ext.targetTech))
            {
                techNames.Add(ext.targetTech);
            }
            if (techNames.Count == 0)
            {
                return;
            }

            Dictionary<ResearchProjectDef, float> vals = GetValues(doer);
            if (valuesField == null)
            {
                return;
            }
            if (vals == null)
            {
                // ⚠⚠ 2026-09-11 红字修复 (读档进图后一读书就 NullReferenceException):
                //  原版 ReadingOutcomeDoerGainResearch 的 values 字典是 Scribe 托管的, 而
                //  Scribe_Collections.Look 在**存档里该节点缺失或写成 IsNull="True"** 时会把
                //  字典**置成 null** (见 Verse/Scribe_Collections.cs: "if (!Scribe.EnterNode(label))
                //  { ... dict = null; }" 与 "xmlAttribute IsNull == true → dict = null")。
                //  旧实现此处直接 return, 于是字典**永远是 null**: 一旦某本书被存成 null,
                //  之后每次读档都还是 null, 每次读书 OnReadingTick 的 foreach(values) 必炸
                //  → 「Exception in JobDriver tick ... NullReferenceException」(红字), 且恶性循环不可自愈。
                //  正解: **新建一个字典并写回字段**, 再走下面的填充逻辑。
                vals = new Dictionary<ResearchProjectDef, float>();
                valuesField.SetValue(doer, vals);
            }
            bool already = vals.Count == techNames.Count;
            if (already)
            {
                for (int i = 0; i < techNames.Count; i++)
                {
                    ResearchProjectDef t = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(techNames[i]);
                    if (t == null || !vals.ContainsKey(t))
                    {
                        already = false;
                        break;
                    }
                }
            }
            if (already)
            {
                return; // 已经标好了
            }
            // 空 include 时原版会回退成「随机 1~2 个当前能研究的项目」, tooltip 和颜色都会指向
            // 不相干的科技; 这里整个换成我们指定的那些。
            vals.Clear();

            // 数值沿用原版「按品质换算的读书给科研速度」→ 信息卡收益行是正常文案;
            // 真正加进度时会被 Patch_ResearchAddProgress 按门控拦掉 (见上方说明)。
            float exp = DefaultResearchExp(book);
            for (int i = 0; i < techNames.Count; i++)
            {
                ResearchProjectDef t = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(techNames[i]);
                if (t != null)
                {
                    vals[t] = exp;
                }
            }
        }

        private static float DefaultResearchExp(Book book)
        {
            QualityCategory q = QualityCategory.Normal;
            CompQuality cq = book.TryGetComp<CompQuality>();
            if (cq != null)
            {
                q = cq.Quality;
            }
            return BookUtility.GetResearchExpForQuality(q);
        }

        private static Dictionary<ResearchProjectDef, float> GetValues(ReadingOutcomeDoerGainResearch doer)
        {
            if (valuesField == null)
            {
                valuesField = AccessTools.Field(typeof(ReadingOutcomeDoerGainResearch), "values");
            }
            if (valuesField == null)
            {
                return null;
            }
            return valuesField.GetValue(doer) as Dictionary<ResearchProjectDef, float>;
        }

        // 供守卫补丁复用 (只在 values 为 null 的异常路径调用)
        public static Dictionary<ResearchProjectDef, float> ValuesOf(ReadingOutcomeDoerGainResearch doer)
        {
            if (doer == null)
            {
                return null;
            }
            return GetValues(doer);
        }

        // 供守卫补丁复用: 写回字典字段
        public static bool AssignValues(ReadingOutcomeDoerGainResearch doer,
            Dictionary<ResearchProjectDef, float> vals)
        {
            if (doer == null || valuesField == null)
            {
                return false;
            }
            valuesField.SetValue(doer, vals);
            return true;
        }
    }

    // ---- v6 逐刻守卫: values 为 null 的原版 doer 不许再抛红字 (2026-09-11) ----
    //  背景 (实测存档取证): 一旦某本书的 <values> 在存档里缺失或被写成 IsNull="True",
    //  Scribe 就会把它读成 null, 而原版 OnReadingTick 头一行就是 `foreach (... in values)`
    //  → 每次读书 tick 一条 "Exception in JobDriver tick ... NullReferenceException" 红字。
    //  修复分两层:
    //    ① Patch_GainResearchTargeting.Ensure 现在会把 null **新建成空字典** 再填 (根治);
    //    ② 本补丁做兜底: 万一仍有漏网的 (比如非本 mod 的书), 直接跳过原版, 不让异常逃逸。
    //  性能: ___values 是 Harmony 的字段注入 (编译期直取字段, 无反射开销), 正常路径只做一次判空。
    public static class Patch_GainResearchReadingTick
    {
        public static bool OnReadingTick_Prefix(ReadingOutcomeDoerGainResearch __instance,
            Dictionary<ResearchProjectDef, float> ___values)
        {
            if (___values != null)
            {
                return true; // 正常路径: 直接放行, 零额外开销
            }
            // 异常路径 (罕见): 先尝试用本 mod 的规则修好; 修好了就让原版照常跑
            try
            {
                Patch_GainResearchTargeting.Ensure(__instance);
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[BlueprintUnlockHSK] 读书项目表修复失败: " + e.Message, 0x5B1D7C);
            }
            return Patch_GainResearchTargeting.ValuesOf(__instance) != null;
        }
    }

    // ---- v5.5 读档补齐: 让「升级前就已经造出来的」蓝图书/典籍也立刻有图标 ----
    //  doer 实例本身是按 def 的 CompProperties 现建的 (CompReadable.Initialize → InitializeDoers),
    //  所以装上新版后每本书都会有 GainResearch doer;
    //  但 values 是「这本书被造出来那一刻」算一次的内容 —— 老书当年跑 OnBookGenerated 时
    //  世界上还没有这个 doer, values 永远不会自己长出来, 图标也就不会出现。
    //  这里挂 Book.ExposeData 后置 (每本书读档/存档都必然经过), 缺什么补什么。
    //
    //  ⚠ 致命坑 (2026-09-10 定位并修复):
    //  原实现无条件在 Postfix 里改 values, 而 Scribe 读档会把 ExposeData 跑三轮
    //  (LoadingVars → ResolvingCrossRefs → PostLoadInit)。第 1 轮里
    //  ReadingOutcomeDoerGainResearch.PostExposeData 的 Scribe_Collections.Look(ref values,...)
    //  只是把 values 置成「空字典」, 真正的条目要到 ResolvingCrossRefs 才由 BuildDictionary
    //  填进去; 我们在 LoadingVars 后就 Clear+写入, 会让第 2 轮的 BuildDictionary 撞上
    //  「Tried to add different values for the same key.」, 字典内容与 doers 列表状态脱节,
    //  托管对象图被写坏 → 表现为 Boom GC 堆上的 HEAP_CORRUPTION (读档刚进图就闪退)。
    //  正解: 只在 PostLoadInit (三轮的最后一轮, cross-ref 已全部就绪、Scribe 不会再碰这个字典)
    //  动手, 与上方 BlueprintBook.ExposeData 的 ApplyJoyFactor 补正同一写法。
    public static class Patch_BookExposeData
    {
        public static void ExposeData_Postfix(Book __instance)
        {
            try
            {
                // 只在读档收尾轮补: 存档时不做任何写入 (也不需要)
                if (Scribe.mode != LoadSaveMode.PostLoadInit)
                {
                    return;
                }
                if (__instance == null || __instance.def == null)
                {
                    return;
                }
                // 快速过滤: 每本书都会过这里, 非本 mod 的书立刻返回
                if (BlueprintTargetExtension.Get(__instance.def) == null)
                {
                    return;
                }
                CompBook comp = __instance.TryGetComp<CompBook>();
                if (comp == null)
                {
                    return;
                }
                foreach (BookOutcomeDoer doer in comp.Doers)
                {
                    ReadingOutcomeDoerGainResearch g = doer as ReadingOutcomeDoerGainResearch;
                    if (g != null)
                    {
                        Patch_GainResearchTargeting.Ensure(g);
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[BlueprintUnlockHSK] 科技书图标读档补齐失败: " + e.Message, 0x5B17002);
            }
        }
    }

    // ============================================================================
    //  蓝图书 doer (BookOutcomeDoer 子类) — 阅读效果判定 (v5)
    // ============================================================================
    //  原版 Book 阅读链路: JobDriver_Reading.tickIntervalAction 每 tick 调
    //  Book.OnBookReadTick(pawn, delta, readingBonus) → 派发给全部 doer.OnReadingTick。
    //  v5 直接复用原版 JobDriver_Reading (joy 活动), 本 doer 只负责:
    //    - DoesProvidesOutcome: 目标科技未解锁 + 本书未读完 + 读得懂 → 才提供效果
    //    - OnReadingTick:       把 factor 归一化后累加进该书研读进度 (核心机制)
    //    - GetBenefitsString:   信息卡显示进度/门槛/智力倍率
    //    - GetTopicRulePacks:   返回目标科技的 generalRules (grammar 兜底)
    public class BlueprintBookProperties : BookOutcomeProperties
    {
        public override System.Type DoerClass
        {
            get
            {
                return typeof(BlueprintBookDoer);
            }
        }
    }

    public class BlueprintBookDoer : BookOutcomeDoer
    {
        // 原版 Book 阅读链路 (v5/v6):
        //   JobDriver_Reading.tickIntervalAction → Book.OnBookReadTick(pawn, delta, roomBonus)
        //     → factor = pawn.ReadingSpeed × roomBonus × delta
        //     → 遍历 BookComp.Doers 调 OnReadingTick(pawn, factor)
        //  本 doer 把 factor 归一化后累加进「这本书的本地进度」(存在书实例上):
        //     localProgress += factor / readCost    (readCost 默认 5000 ≈ 一次强制阅读的总 factor)
        //  每跨过一个整数 → 该书绑定的那一「卷」读毕, 记进世界层;
        //  某科技的全部卷读毕 → 解锁该科技的研究权限 (BlueprintUnlockTracker.CheckUnlock)。
        private BlueprintBook AsBlueprintBook()
        {
            BlueprintBook b = Parent as BlueprintBook;
            if (b == null && Parent != null)
            {
                // 兜底: 直挂本 doer 的书若没走 BlueprintBook 子类, 至少别崩
                b = null;
            }
            return b;
        }

        public override bool DoesProvidesOutcome(Pawn reader)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(Parent.def);
            if (ext == null)
            {
                return false;
            }
            BlueprintBook book = AsBlueprintBook();
            if (book == null)
            {
                return false;
            }
            string tech = book.BoundTech;
            if (string.IsNullOrEmpty(tech))
            {
                return false;
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(tech);
            if (p == null || p.IsFinished)
            {
                return false;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return false;
            }
            if (tracker.IsTechUnlocked(tech))
            {
                return false; // 目标科技已解锁: 本书不再提供解锁进度
            }
            if (book.IsFullyRead)
            {
                return false; // 本书已读完: 再读无额外收益
            }
            if (reader != null && !MeetsIntellectRequirement(reader, ext))
            {
                return false; // 读不懂: 不让 joy 自动派书优先选中它
            }
            return true;
        }

        // 智力门槛判定 (与 Patch_BookUtility 的硬门槛同一份逻辑)
        public static bool MeetsIntellectRequirement(Pawn reader, BlueprintTargetExtension ext)
        {
            int need = BlueprintTargetExtension.GetMinIntellect(ext);
            if (need <= 0)
            {
                return true;
            }
            return IntellectualUtil.LevelOf(reader) >= need;
        }

        public override void OnReadingTick(Pawn reader, float factor)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(Parent.def);
            if (ext == null || factor <= 0f)
            {
                return;
            }
            BlueprintBook book = AsBlueprintBook();
            if (book == null)
            {
                return;
            }
            string tech = book.BoundTech;
            if (string.IsNullOrEmpty(tech))
            {
                return;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null || tracker.IsTechUnlocked(tech))
            {
                return;
            }
            float readCost = BlueprintTargetExtension.GetReadCost(ext);
            if (readCost <= 0f)
            {
                readCost = BlueprintTargetExtension.DefaultReadCost;
            }
            book.AddLocalProgress(factor / readCost);
        }

        public override string GetBenefitsString(Pawn reader = null)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(Parent.def);
            if (ext == null)
            {
                return "";
            }
            BlueprintBook book = AsBlueprintBook();
            if (book == null)
            {
                return "";
            }
            string tech = book.BoundTech;
            if (string.IsNullOrEmpty(tech))
            {
                return "";
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(tech);
            if (p == null)
            {
                return "";
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return "";
            }
            int need = BlueprintTargetExtension.GetMinIntellect(ext);
            if (tracker.IsTechUnlocked(tech))
            {
                return string.Format("已解锁「{0}」——本书已无研读效果 (仍可当普通读物)", p.label);
            }
            if (book.IsFullyRead)
            {
                return string.Format("本书已读完; 「{0}」解锁进度 {1}%", p.label,
                    Mathf.RoundToInt(tracker.GetTechProgress(tech) * 100f));
            }
            string s = string.Format("本书研读 {0}% (共 {1} 卷) —— 「{2}」总进度 {3}% ({4}/{5} 卷)",
                Mathf.RoundToInt(book.LocalProgress01 * 100f),
                book.boundVolumes == null ? 0 : book.boundVolumes.Count,
                p.label,
                Mathf.RoundToInt(tracker.GetTechProgress(tech) * 100f),
                tracker.GetReadVolumeCount(tech),
                tracker.GetVolumeCount(tech));
            if (need > 0)
            {
                s += string.Format("; 需要智力 {0} 级", need);
            }
            float mult = BlueprintTargetExtension.GetIntellectMult(ext);
            if (mult > 1f)
            {
                s += string.Format("; 阅读智力经验 ×{0}", mult);
            }
            return s;
        }

        public override bool BenefitDetailsCanChange(Pawn reader = null)
        {
            return true; // 进度会变 → 信息卡描述需要刷新
        }

        public override System.Collections.Generic.IEnumerable<Verse.Grammar.RulePack> GetTopicRulePacks()
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(Parent.def);
            if (ext == null)
            {
                return null;
            }
            BlueprintBook book = AsBlueprintBook();
            string tech = book != null ? book.BoundTech : ext.targetTech;
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(tech);
            if (p != null && p.generalRules != null)
            {
                List<Verse.Grammar.RulePack> list = new List<Verse.Grammar.RulePack>();
                list.Add(p.generalRules);
                return list;
            }
            return null;
        }
    }

    // ============================================================================
    //  研读 JobDriver / WorkGiver 已删除 (v5, 2026-09-10)
    // ============================================================================
    //  v4 用自写 JobDriver_StudyBlueprint (workType=Research, 科研工作指派) + WorkGiver_StudyBlueprint
    //  自动排队研读。v5 改为原版娱乐阅读链路: JobDefOf.Reading + JobDriver_Reading (自带阅读动画/
    //  进度条/就坐/双态), 右键「指派阅读」即 job.playerForced 强制阅读, 自由活动由 JoyGiver_Read 派书。
    //  连带的 JobDefs_RK.xml / WorkGiverDefs_RK.xml 及对应 DefInjected 已同步删除。
    // ============================================================================

    // ---- 门控科技: 未解锁不可研究 (只做「收紧」) ----
    //  ⚠️ 必须是 Postfix。原 v5 用 Prefix 直接写 __result = 解锁 && 前置完成 并 return false,
    //  把原版 CanStartNow 的其余条件全部丢掉了 —— requiredResearchBuilding / 研究台通电 /
    //  TechprintRequirementMet / PlayerMechanitorRequirementMet / AnalyzedThingsRequirementsMet /
    //  IsHidden / InspectionRequirementsMet —— 等于把门禁「放宽」了。
    //  改成后置: 原版先算完 (前置/研究台/techprint/分析… 全保留), 只在其结果为 true 时
    //  叠加蓝图门控。既不丢原版条件, 也保证门控只能更严不会更松。
    [HarmonyPatch(typeof(ResearchProjectDef), "CanStartNow", MethodType.Getter)]
    public static class Patch_CanStartNow
    {
        static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result || __instance == null)
            {
                return; // 原版已判不可研究 → 保持原结论
            }
            BlueprintTargetExtension target = BlueprintGateDatabase.GetExtensionForTech(__instance.defName);
            if (target == null || !target.gate)
            {
                return; // 非门控科技 / 并行推进通道 → 完全走原版
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return;
            }
            if (!tracker.IsTechUnlocked(__instance.defName))
            {
                __result = false; // 蓝图未读满 → 不可开始
            }
        }
    }

    // ============================================================================
    //  硬门槛 (v5.1, 2026-09-10): UI 层之外的两道真拦截
    // ============================================================================
    //  问题: ResearchTreeSK 的科研队列绕过一切可用性判定
    //    ① 点节点入队时, Tree.cs 会把该节点「所有未完成的前置」一并入队
    //       (ResearchProjectDef.GetIncompleteParentsRecursive, 不检查 Available);
    //    ② 队列推进直接调 ResearchManager.SetCurrentProject, 而它只判 baseCost > 0,
    //       既不看原版前置, 也不看蓝图门控。
    //  结果: 点下游节点 → 上游那个「蓝图未解锁」的门控科技被连带排进队列 → 照常研究。
    //  (Node.Available 置灰只挡「直接点击该节点」这一条路, 挡不住连带入队。)
    //  下面两道补丁把门控下沉到「进度层」与「劳动层」, 任何入口都绕不过。
    // ============================================================================

    // ---- 硬门槛·进度层 ①: 研究台劳动 (ResearchPerformed) ----
    //  ⚠️ 这条才是主路径! 反编译确认: ResearchManager.ResearchPerformed (1.6) **不调用** AddProgress,
    //  而是自己算完后直接 `progress[currentProj] = num;` 写入。
    //  调用链: JobDriver_Research.tickIntervalAction → ResearchManager.ResearchPerformed。
    //  所以想拦住"研究员在台前把锁定科技研究出来", 必须拦这里。
    public static class Patch_ResearchPerformed
    {
        public static bool ResearchPerformed_Prefix(ResearchManager __instance)
        {
            if (__instance == null)
            {
                return true;
            }
            // 注意: currentProj 在 1.6 是 private 字段, 不可直接访问;
            // GetProject() 在 category==null 时返回的就是 currentProj (公开 API)。
            ResearchProjectDef proj = __instance.GetProject();
            if (proj == null || proj.IsFinished)
            {
                return true;
            }
            BlueprintGateDatabase.BisectTrace("PERF", proj.defName);
            bool blocked = BlueprintGateDatabase.BlocksResearch(proj);
            BlueprintGateDatabase.BisectTrace("PERF_OUT", proj.defName + " blocked=" + blocked);
            return !blocked;
        }
    }

    // ---- 硬门槛·进度层 ②: 其它进度入口 (AddProgress) ----
    //  非研究台路径的进度都走这里: 原版/第三方"读书给科研"的 ReadingOutcomeDoerGainResearch、
    //  剧本/任务/脚本直接加进度等。与 ① 一起构成完整进度层门禁。
    public static class Patch_ResearchAddProgress
    {
        // 方法名以 _Prefix 结尾才会被 TryPatch 注册为 prefix
        public static bool AddProgress_Prefix(ResearchProjectDef __0)
        {
            BlueprintGateDatabase.BisectTrace("ADD", __0 == null ? "<null>" : __0.defName);
            if (__0 == null || __0.IsFinished)
            {
                return true;
            }
            return !BlueprintGateDatabase.BlocksResearch(__0);
        }
    }

    // ---- 硬门槛·劳动层: 未解锁的门控科技不派人去研究 ----
    //  WorkGiver_Researcher.HasJobOnThing 是「派人去研究台」的唯一入口, 它只看
    //  ResearchProjectDef.CanBeResearchedAt (不看 CanStartNow, 也不看前置)。
    //  这里后置返回 false → 研究员根本不接这个活, 也就不会出现
    //  「站在研究台前按 0% 进度白刷智力经验」的假象。
    //
    //  ⚠️ 故意不去改 CanBeResearchedAt 本身: 原版科研页签会拿它生成「为什么不能研究」
    //  的原因列表 (MainTabWindow_Research.lockedReasons), 在源头返回 false 会误报成
    //  "MissingRequiredResearchFacilities"(缺少研究设施) —— 那是错的, 真实原因是蓝图未解锁。
    public static class Patch_WorkGiverResearcher
    {
        public static void HasJobOnThing_Postfix(ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            ResearchProjectDef proj = Find.ResearchManager.GetProject();
            if (proj == null)
            {
                return;
            }
            BlueprintGateDatabase.BisectTrace("WORK", proj.defName);
            if (BlueprintGateDatabase.BlocksResearch(proj))
            {
                __result = false;
            }
        }
    }

    // ---- 硬门槛·道具层: 「科技专家副人格核心」不能用来跳过蓝图门控 ----
    //  CompUseEffect_FinishRandomResearchProject 会把「当前研究项目」直接 FinishProject 完成
    //  (原版 Royalty 道具 TechprofSubpersonaCore)。当前项目有可能是 ResearchTreeSK 队列塞进来的
    //  未解锁门控科技, 那样等于用道具跳关。这里前置拒绝 (返回 false → 道具不消耗、不生效)。
    //
    //  ⚠️ 故意**不**去拦 ResearchManager.FinishProject 本身: 这些正规流程都走它 ——
    //     · 阵营起始研究标签 (FactionDef.startingResearchTags)
    //     · 意识形态 MemeDef.startingResearchProjects
    //     · archonexus 搬家 (ResetAllProgress 后用 ClassicStart 标签恢复玩家已有科技)
    //     拦了会把这些正统来源一起吞掉。所以只在本道具的入口处收口。
    public static class Patch_TechprofCore
    {
        public static bool DoEffect_Prefix()
        {
            ResearchProjectDef proj = Find.ResearchManager.GetProject();
            if (proj == null)
            {
                return true;
            }
            BlueprintGateDatabase.BisectTrace("TECH", proj.defName);
            return !BlueprintGateDatabase.BlocksResearch(proj);
        }
    }

    // ============================================================================
    //  v5 三条机制补丁 (原版 Book 体系钩子)
    // ============================================================================

    // 智力等级读取 (统一入口; 无技能系统的 pawn 视为 0 级)
    public static class IntellectualUtil
    {
        public static int LevelOf(Pawn pawn)
        {
            if (pawn == null || pawn.skills == null)
            {
                return 0;
            }
            SkillRecord rec = pawn.skills.GetSkill(SkillDefOf.Intellectual);
            if (rec == null)
            {
                return 0;
            }
            return rec.Level;
        }
    }

    // ---- ① 智力门槛 (硬): 不满足就「读不懂」 ----
    //  原版 BookUtility.CanReadBook(Book, Pawn, out string reason) 是全流程唯一判定入口,
    //  调用点仅两处: Book.GetFloatMenuOptions (右键「指派阅读」的可用性) 与
    //  JobDriver_Reading 的 toil.AddEndCondition (阅读中每刻校验)。
    public static class Patch_BookUtility
    {
        // 后置: 原版判过之后, 再叠加蓝图智力门槛
        public static void CanReadBook_Postfix(Book __0, Pawn __1, ref string __2, ref bool __result)
        {
            if (!__result || __0 == null || __1 == null)
            {
                return;
            }
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(__0.def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return;
            }
            int need = BlueprintTargetExtension.GetMinIntellect(ext);
            if (need <= 0)
            {
                return;
            }
            int have = IntellectualUtil.LevelOf(__1);
            if (have >= need)
            {
                return;
            }
            __result = false;
            __2 = "RK_ReadTooHard".Translate(need, have);
        }

        // 前置: 拦住 joy 自动派书的候选过滤器 (BookUtility.IsValidBook, private static,
        //  只被 TryGetRandomBookToRead 调用)。只拦 CanReadBook 的话, 小人自由活动时会被
        //  自动塞一本读不懂的蓝图书 → 开读 1 刻就被 EndCondition 中断 → 立刻重试 → 抽搐刷屏。
        public static bool IsValidBook_Prefix(Thing __0, Pawn __1, ref bool __result)
        {
            if (__0 == null || __1 == null)
            {
                return true;
            }
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(__0.def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return true;
            }
            if (BlueprintBookDoer.MeetsIntellectRequirement(__1, ext))
            {
                return true;
            }
            __result = false;
            return false; // 跳过原方法
        }
    }

    // ---- ② 智力加成: 读书涨智力, 倍率随科技档位 ----
    //  原版 JobDriver_Reading 的 tickIntervalAction 里已自带
    //  pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f * delta);
    //  这里在同一 tick 叠加额外经验: extra = (mult - 1) × 0.1 × delta。
    //  上限: 智力等级达到 (门槛 + BonusCapMargin) 后不再叠加, 防止一本低级书把全员刷满。
    public static class Patch_BookReadTick
    {
        public const int BonusCapMargin = 6;

        // 参数一律用位置式 (__0/__1/__2) 绑定, 不依赖原方法形参名 (反编译产物可能丢名)
        public static void Postfix(Book __instance, Pawn __1, int __2)
        {
            Pawn pawn = __1;
            int delta = __2;
            if (__instance == null || pawn == null || pawn.skills == null || delta <= 0)
            {
                return;
            }
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(__instance.def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return;
            }
            float mult = BlueprintTargetExtension.GetIntellectMult(ext);
            if (mult <= 1f)
            {
                return;
            }
            if (pawn.skills.GetSkill(SkillDefOf.Intellectual) == null)
            {
                return;
            }
            int cap = BlueprintTargetExtension.GetMinIntellect(ext) + BonusCapMargin;
            if (IntellectualUtil.LevelOf(pawn) >= cap)
            {
                return;
            }
            pawn.skills.Learn(SkillDefOf.Intellectual, (mult - 1f) * 0.1f * (float)delta);
        }
    }

    // ============================================================================
    //  原版刷书途径接入「科技典籍」(v5.4)
    // ============================================================================
    //  原版唯一的「随机造一本书」入口是 BookUtility.MakeBook(ArtGenerationContext, QualityGenerator?)
    //  —— ThingSetMaker_Books.Generate 就是调它 (任务奖励的书籍堆 / 商队书籍 / 书商库存全走这条)。
    //  它内部按 CompProperties_Book.pickWeight 从「全部带 CompBook 的 def」加权随机, 不看 tag,
    //  也不看这个 def 属于哪个分类 —— 所以只要在 def 上留一个非零 pickWeight, 典籍就会自己
    //  混进原版刷书池。但那样**无法筛选「只给未解锁的科技」**, 会刷出一堆没用的旧书。
    //
    //  做法: 典籍 def 的 pickWeight 显式写 0 (不进原版加权池), 改由这里 Prefix 接管:
    //    · 不命中 (绝大多数) → 返回 true, 走原版, 行为一个字节都不改;
    //    · 命中 (小概率)     → 换成「科技典籍」, 且只从**尚未解锁的科技**里挑,
    //                          并直接调原版的重载 MakeBook(ThingDef, context, qualityGenerator)
    //                          生成 (复用它自带的品质/材质/CompQuality 逻辑, 不自己复刻)。
    //
    //  概率: BlueprintTargetExtension.TechBookLootChance (默认 10%)。
    //  档位: BlueprintGateDatabase.PickRandomTechBook 内部按玩家「最卡的档位」±1 挑,
    //        保证刷到的典籍始终是当下用得上的一档 —— 也就是「档位跟随来源/进度」。
    public static class Patch_BookMakeRandom
    {
        // 参数按位置绑定 (__0=context, __1=qualityGenerator), 不依赖反编译可能丢失的形参名
        public static bool MakeBook_Prefix(ArtGenerationContext __0, System.Nullable<QualityGenerator> __1,
            ref Book __result)
        {
            if (Rand.Value >= BlueprintTargetExtension.TechBookLootChance)
            {
                return true;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomTechBook();
            if (def == null)
            {
                return true; // 没有可投放的典籍 (科技全解锁 / 未配置) → 原版造普通书
            }
            try
            {
                Book made = BookUtility.MakeBook(def, __0, __1);
                if (made == null)
                {
                    return true;
                }
                __result = made;
                return false; // 跳过原方法, 用典籍替换掉这本普通书
            }
            catch (System.Exception e)
            {
                Log.Warning("[BlueprintUnlockHSK] 科技典籍生成失败(回落原版普通书): " + e.Message);
                return true;
            }
        }
    }

    // ---- 科研树门禁: 蓝图没读满时强制节点不可用 (置灰 + 锁图标 + 禁止点击) ----
    // ResearchTreeSK 的节点可用态是 public bool Node.Available 字段, 在 UpdateCaches() 里按
    // 原版条件重算 (窗口每次打开时遍历全部节点刷新一次)。科研树的灰色背景 + 锁图标 + Tree.cs 里
    // 点击入队判定, 全都只看这个 Available 字段, 完全不读 CanStartNow —— 所以仅在 CanStartNow
    // 上门禁不足以让节点变灰不可点。这里在 UpdateCaches 之后追加一道。
    //
    // v5.2 修正: 判定改成 BlueprintGateDatabase.IsGateLockedOnPath() —— 门控锁会沿前置链
    // **向下游传播**。旧版只判「本科技自己是不是没读满的门控科技」, 于是微电子学(门控)锁着、
    // 它的下游 研究技术 III(非门控)却是亮的, 看着像"锁没有继承下去"。
    public static class Patch_NodeUpdateCaches
    {
        private static FieldInfo fResearch;
        private static FieldInfo fAvailable;
        private static bool inited;

        private static void EnsureInit()
        {
            if (inited)
            {
                return;
            }
            System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
            if (nodeType != null)
            {
                fResearch = AccessTools.Field(nodeType, "Research");
                fAvailable = AccessTools.Field(nodeType, "Available");
            }
            inited = true;
        }

        public static void Postfix(object __instance)
        {
            // 本补丁跑在科研树每帧刷新流程里, 绝不能抛异常: 一律吞掉, 失败时最坏是本帧不置灰。
            try
            {
                EnsureInit();
                if (fResearch == null || fAvailable == null || __instance == null)
                {
                    return;
                }
                if (!((bool)fAvailable.GetValue(__instance)))
                {
                    return; // 本已不可用, 无需处理
                }
                ResearchProjectDef p = fResearch.GetValue(__instance) as ResearchProjectDef;
                if (p == null || p.IsFinished)
                {
                    return;
                }
                if (BlueprintGateDatabase.IsGateLockedOnPath(p))
                {
                    // 自身蓝图没读满, 或上游有「蓝图没读满」的门控科技
                    // → 置灰 + 锁图标 + Tree.cs 点击入队被拒
                    fAvailable.SetValue(__instance, false);
                }
            }
            catch
            {
            }
        }
    }

    // ---- 科研树即时刷新: 蓝图读满解锁后, 若科研树正开着就立刻重算节点可用态 ----
    // ResearchTreeSK 只在窗口 PreOpen 时对全部节点调一次 UpdateCaches, 而玩家读书时科研树
    // 往往正开着 —— 不主动刷新就会出现「书已经读满, 节点却还灰着/带锁」的错觉 (要关窗口重开才更新)。
    public static class BlueprintTreeRefresh
    {
        private static System.Type windowType;
        private static FieldInfo fInitialized;
        private static PropertyInfo pNodes;
        private static MethodInfo mUpdateCaches;
        private static bool inited;
        private static bool unusable;

        private static void EnsureInit()
        {
            if (inited)
            {
                return;
            }
            inited = true;
            try
            {
                System.Type treeType = AccessTools.TypeByName("ResearchTreeSK.Tree");
                System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
                windowType = AccessTools.TypeByName("ResearchTreeSK.MainTabWindow_ResearchTree");
                if (treeType == null || nodeType == null || windowType == null)
                {
                    unusable = true;
                    return;
                }
                fInitialized = AccessTools.Field(treeType, "Initialized");
                pNodes = AccessTools.Property(treeType, "Nodes");
                mUpdateCaches = AccessTools.Method(nodeType, "UpdateCaches");
                if (fInitialized == null || pNodes == null || mUpdateCaches == null)
                {
                    unusable = true;
                }
            }
            catch
            {
                unusable = true;
            }
        }

        // 未装 ResearchTreeSK / 树没初始化 / 窗口没开 → 直接返回, 零副作用
        public static void IfOpen()
        {
            try
            {
                EnsureInit();
                if (unusable || Find.WindowStack == null)
                {
                    return;
                }
                if (!(bool)fInitialized.GetValue(null))
                {
                    return;
                }
                if (!Find.WindowStack.IsOpen(windowType))
                {
                    return;
                }
                System.Collections.IList nodes = pNodes.GetValue(null, null) as System.Collections.IList;
                if (nodes == null)
                {
                    return;
                }
                for (int i = 0; i < nodes.Count; i++)
                {
                    mUpdateCaches.Invoke(nodes[i], null);
                }
            }
            catch
            {
                // 纯 UI 刷新, 失败不影响解锁结果
            }
        }
    }

    // ---- 科研树节点「卷进度」标记 (v6, 2026-09-11) ----
    //  在门控科技节点下方绘制 N 个小格 (N = 该科技卷数):
    //    · 灰 = 该卷未读 / 黄 = 已读
    //    · 尾随 (已读/N) 文本
    //  挂在 ResearchTreeSK.Node.Draw 末尾; 未装 ResearchTreeSK 时补丁不挂。
    public static class Patch_ResearchTreeNodeBadge
    {
        private static FieldInfo fResearch;
        private static FieldInfo fRect;
        private static bool inited;

        // 卷格样式
        private const float BoxSize = 7f;
        private const float BoxGap = 2f;

        private static readonly Color BoxOn = new Color(1f, 0.82f, 0.22f, 1f);    // 已读: 黄
        private static readonly Color BoxOff = new Color(0.42f, 0.42f, 0.42f, 0.85f); // 未读: 灰
        private static readonly Color BarBg = new Color(0f, 0f, 0f, 0.62f);

        private static void EnsureInit()
        {
            if (inited)
            {
                return;
            }
            System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
            if (nodeType != null)
            {
                fResearch = AccessTools.Field(nodeType, "Research");
                fRect = AccessTools.Field(nodeType, "Rect");
            }
            inited = true;
        }

        public static void Postfix(object __instance, bool isDragged, bool drawInQueue)
        {
            if (isDragged || drawInQueue)
            {
                return; // 队列/拖拽视图不绘制徽标
            }
            // 整个 postfix 包 try/catch: 本补丁运行在 ResearchTreeSK.Node.Draw 内部,
            // 而 Node.Draw 位于 BeginScrollView 与 EndScrollView 之间——
            // 一旦异常逃逸会跳过 EndScrollView, 产生"2 GUIClip + 1 mousePosition"同款泄漏。
            // 必须吞掉并恢复 GUI 状态, 绝不能让异常逃逸到科研树绘制流程。
            try
            {
                EnsureInit();
                if (fResearch == null || fRect == null)
                {
                    return;
                }
                ResearchProjectDef p = fResearch.GetValue(__instance) as ResearchProjectDef;
                if (p == null || p.IsFinished)
                {
                    return;
                }
                BlueprintTargetExtension target = BlueprintGateDatabase.GetExtensionForTech(p.defName);
                if (target == null || !target.gate)
                {
                    // 非门控科技 (gate=false 的并行推进通道) 不画标记:
                    // 它们的蓝图只是"抄近路", 原版研究路线照常可走, 画个 0/N 会让人误以为"没解锁还能研究"。
                    return;
                }
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                if (tracker == null)
                {
                    return;
                }

                Rect nodeRect;
                try
                {
                    nodeRect = (Rect)fRect.GetValue(__instance);
                }
                catch
                {
                    return;
                }
                if (nodeRect.width < 20f || nodeRect.height < 10f)
                {
                    return; // 节点极小/未布局时跳过
                }

                int total = BlueprintGateDatabase.GetVolumeCount(p.defName);
                if (total <= 0)
                {
                    total = 1;
                }
                if (total > 12)
                {
                    total = 12; // 极端情况保护: 最多画 12 格
                }
                int have = tracker.GetReadVolumeCount(p.defName);
                if (have > total)
                {
                    have = total;
                }

                Text.Font = GameFont.Tiny;
                string label = "(" + have + "/" + total + ")";
                Vector2 labelSize = Text.CalcSize(label);

                float boxesW = total * BoxSize + (total - 1) * BoxGap;
                float barW = boxesW + 6f + labelSize.x + 6f;
                float barH = BoxSize + 4f;
                if (barW > nodeRect.width)
                {
                    barW = nodeRect.width;
                }
                Rect bar = new Rect(nodeRect.x + 2f, nodeRect.yMax - barH - 2f, barW, barH);

                TooltipHandler.TipRegion(bar, string.Format("{0}: 已读 {1}/{2} 卷", p.label, have, total));

                Widgets.DrawBoxSolid(bar, BarBg);

                float bx = bar.x + 3f;
                float by = bar.y + (barH - BoxSize) * 0.5f;
                for (int i = 0; i < total; i++)
                {
                    Rect cell = new Rect(bx, by, BoxSize, BoxSize);
                    bool read = tracker.IsVolumeRead(VolumeUtil.Key(p.defName, i + 1));
                    Widgets.DrawBoxSolid(cell, read ? BoxOn : BoxOff);
                    Widgets.DrawBox(cell, 1);
                    bx += BoxSize + BoxGap;
                }

                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = have >= total ? new Color(0.4f, 1f, 0.4f, 1f) : Color.white;
                Widgets.Label(new Rect(bx + 2f, bar.y, labelSize.x + 2f, barH), label);

                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
            catch (System.Exception e)
            {
                // 恢复 GUI 状态后吞掉, 确保 EndScrollView 仍会被执行, 不产生 clip 泄漏
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                Log.WarningOnce("[BlueprintUnlockHSK] 科研树节点卷进度绘制失败(已捕获, 不影响科研树): " + e.Message, 0x5B1D5A);
            }
        }
    }

    // ---- 蓝图商人: 按派系科技等级卖对应分级蓝图书 (v4: 物件变书, 逻辑不变) ----
    // 规则: 中世纪/工业商人可"卖全套但稀有"(低概率+1-2本/次, 防一口气刷齐);
    //       太空/极致商人约半数卖半数不卖(50/50 实例投掷, 只在高档时卖 t4/t5)。数值集中在下方常量便于调整。
    public class StockGenerator_BlueprintTrader : StockGenerator
    {
        // 太空/极致档 的高档(t4/t5)蓝图携带概率 (0.5 = 约半数商卖)
        private const float SpacerCarryHighGate = 0.5f;

        // 蓝图经济 v4.1: 单次补货携带蓝图的概率 (蓝图学者商队=0.2; 既有注入实例默认 1, 行为不变)
        public float sellChance = 1f;

        public override bool HandlesThingDef(ThingDef td)
        {
            return td != null && BlueprintTargetExtension.Get(td) != null;
        }

        public override IEnumerable<Thing> GenerateThings(PlanetTile tile, Faction faction)
        {
            if (sellChance < 1f && Rand.Value > sellChance)
            {
                yield break;
            }
            TechLevel tierOfMerchant = faction != null ? faction.def.techLevel : TechLevel.Spacer;
            List<ThingDef> pool = BlueprintGateDatabase.AllBlueprintDefs();
            if (pool.Count == 0)
            {
                yield break;
            }
            // 太空商对高端(t4/t5)的实例子门控: 一半卖一半不卖
            bool allowHigh = (tierOfMerchant >= TechLevel.Spacer) && (Rand.Value < SpacerCarryHighGate);

            List<ThingDef> candidates = new List<ThingDef>();
            List<int> weights = new List<int>();
            for (int i = 0; i < pool.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(pool[i]);
                if (ext == null)
                {
                    continue;
                }
                int tier = BlueprintTargetExtension.GetTier(ext);
                if (!allowHigh && tier >= 4)   // 商人未投中高档时跳过极致/超凡
                {
                    continue;
                }
                int w = WeightFor(tierOfMerchant, tier);
                if (w <= 0)
                {
                    continue;
                }
                candidates.Add(pool[i]);
                weights.Add(w);
            }
            if (candidates.Count == 0)
            {
                yield break;
            }
            int picked = WeightedPick(weights);
            yield return ThingMaker.MakeThing(candidates[picked]);
        }

        // 派系档位 → 各 tier 权重表 (便于后续调整; 中世纪/工业 "卖全套但稀有")
        private int WeightFor(TechLevel merchant, int tier)
        {
            switch (merchant)
            {
                case TechLevel.Neolithic:
                    return 0;                                   // 石器不卖 (需求#4)
                case TechLevel.Medieval:
                    return SmallWeight(tier, 1, 15, 8, 6, 4);   // 本档最易, 其它稀有
                case TechLevel.Industrial:
                    return SmallWeight(tier, 15, 1, 12, 8, 5);  // 本档为工业(暂无门控则回落), 高四档稀有
                case TechLevel.Ultra:
                case TechLevel.Archotech:
                case TechLevel.Spacer:
                default:
                    return SmallWeight(tier, 4, 8, 35, 70, 90); // 太空: 越高级越常见, 低档稀有
            }
        }

        private int SmallWeight(int tier, int t1, int t2, int t3, int t4, int t5)
        {
            switch (tier)
            {
                case 1: return t1;
                case 2: return t2;
                case 3: return t3;
                case 4: return t4;
                case 5: return t5;
                default: return 0;
            }
        }

        private int WeightedPick(List<int> weights)
        {
            int total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                total += weights[i];
            }
            int r = Rand.RangeInclusive(0, total - 1);
            int acc = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                acc += weights[i];
                if (r < acc)
                {
                    return i;
                }
            }
            return weights.Count - 1;
        }
    }

    // ---- 蓝图奖励生成器: 从门控清单随机选一张蓝图书 (用于任务/事件奖励池) ----
    public class ThingSetMaker_BlueprintReward : ThingSetMaker
    {
        // 0=不限档; >0 只在该档蓝图书里选 (XML 里 <tier>3</tier> 等)
        public int tier = 0;

        protected override bool CanGenerateSub(ThingSetMakerParams parms)
        {
            return BlueprintGateDatabase.AllBlueprintDefs().Count > 0;
        }

        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint(tier, false, 0.8f);
            if (def != null)
            {
                outThings.Add(ThingMaker.MakeThing(def));
            }
        }

        protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
        {
            return BlueprintGateDatabase.AllBlueprintDefs();
        }
    }

    // ---- 物品贮藏任务 (RimQuest 用原版 QuestScriptDef): 藏宝处放一张蓝图书 ----
    public static class Patch_ItemStash
    {
        public static void ScatterAt_Postfix(IntVec3 loc, Map map)
        {
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker != null && tracker.pendingCommission)
            {
                // 委托保底: 本藏宝处必定投放一本「优先卡节点」的蓝图书, 且限委托档位。
                tracker.pendingCommission = false;
                ThingDef need = BlueprintGateDatabase.PickRandomBlueprint(tracker.pendingCommissionTier, true, 1f);
                if (need == null)
                {
                    need = BlueprintGateDatabase.PickRandomBlueprint(tracker.pendingCommissionTier, false, 1f);
                }
                if (need != null)
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(need), loc, map);
                    BlueprintStuckMonitor.MarkDirty();
                }
                return;
            }
            // 按格调用, 概率门控避免每格都刷蓝图书 (4-8 格 → 期望 1-2 本);
            // 卡节点时提权 (0.25 → 0.45), 事件驱动、无 per-tick 开销
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.45f : 0.25f;
            if (Rand.Value >= prob)
            {
                return;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
            if (def != null)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(def), loc, map);
            }
        }
    }

    // ---- Go Explore: 战利品生成后注入蓝图书 (卡节点时提权) ----
    public static class Patch_GoExploreRewards
    {
        public static void Stockpile_Postfix(ref List<Thing> __result)
        {
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.55f : 0.3f;
            if (__result != null && Rand.Value < prob)
            {
                ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
                if (def != null)
                {
                    __result.Add(ThingMaker.MakeThing(def));
                }
            }
        }

        public static void StorageBox_Postfix(ref List<Thing> __result)
        {
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.65f : 0.5f;
            if (__result != null && Rand.Value < prob)
            {
                ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
                if (def != null)
                {
                    __result.Add(ThingMaker.MakeThing(def));
                }
            }
        }

        public static void Intercepted_Postfix(ref List<Thing> __result)
        {
            if (__result != null)
            {
                ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
                if (def != null)
                {
                    __result.Add(ThingMaker.MakeThing(def));
                }
            }
        }
    }

    // ---- Cybranian Events: 陨石坠落处放蓝图书 + 老人随身带蓝图书 ----
    public static class Patch_CybranianEvents
    {
        public static void Meteorite_Postfix(IntVec3 c, Map map)
        {
            // 按格调用, 概率门控避免每格都刷蓝图书; 卡节点时提权 (0.4 → 0.6)
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.6f : 0.4f;
            if (Rand.Value >= prob)
            {
                return;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
            if (def != null)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(def), c, map);
            }
        }

        public static void OldMan_Postfix(object __instance, Pawn pawn)
        {
            // 打的是基类方法, 对全部 walk-in 流浪者任务触发, 必须按真实类型门控:
            // 只有 Cybranian 的 OldMan 任务才投放蓝图书, 原版其它 walk-in (WandererJoin/Abasia) 不处理。
            if (__instance == null || __instance.GetType().FullName != "EventsCore.Quests.QuestNode_Root_WandererJoin_OldMan")
            {
                return;
            }
            if (pawn == null || pawn.inventory == null)
            {
                return;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
            if (def != null)
            {
                pawn.inventory.TryAddItemNotForSale(ThingMaker.MakeThing(def));
            }
        }
    }

    // ============================================================================
    //  蓝图经济 v4.1 (2026-08-27) — MarketValue 折价 StatPart
    //  挂在原版 MarketValue stat 上(Patches/68), 仅对"已研读"的蓝图书实例 ×0.5。
    //  商人新生成的蓝图书 stack 未读 → 买价不受影响; 玩家出售已读副本折价。
    // ============================================================================
    public class StatPart_BlueprintReadDiscount : StatPart
    {
        public float discount = 0.5f;

        public override void TransformValue(StatRequest req, ref float val)
        {
            Thing t = req.Thing;
            if (t == null || t.def == null || BlueprintTargetExtension.Get(t.def) == null)
            {
                return;
            }
            BlueprintBook bb = t as BlueprintBook;
            if (bb != null && bb.IsFullyRead)
            {
                val *= discount;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            Thing t = req.Thing;
            if (t == null || t.def == null || BlueprintTargetExtension.Get(t.def) == null)
            {
                return null;
            }
            BlueprintBook bb = t as BlueprintBook;
            if (bb != null && bb.IsFullyRead)
            {
                return "已研读蓝图: 价值 \u00d7" + (int)(discount * 100f) + "%";
            }
            return null;
        }
    }

    // ============================================================================
    //  事件迁移 (MO Storyteller 子系统 → 无 MO 依赖版)
    // ============================================================================
    //  1) IncidentWorker_BlueprintGift — "游学者遗落的蓝图": 按已研究数推算时代档位,
    //     掉 1 本未解锁的对应档蓝图书 + 信。对应 MO "图纸=商人/探索获取" 的事件腿。
    public class IncidentWorker_BlueprintGift : IncidentWorker
    {
        private static int CountFinished()
        {
            int n = 0;
            List<ResearchProjectDef> all = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].IsFinished)
                {
                    n++;
                }
            }
            return n;
        }

        private static int ExpectedTier()
        {
            int f = CountFinished();
            if (f < 25) return 1;
            if (f < 60) return 2;
            if (f < 120) return 3;
            if (f < 220) return 4;
            return 5;
        }

        // 目标档未解锁且未读全的书池
        private static List<ThingDef> UsefulPool()
        {
            List<ThingDef> result = new List<ThingDef>();
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return result;
            }
            int want = ExpectedTier();
            List<ThingDef> all = BlueprintGateDatabase.AllBlueprintDefs();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext == null || string.IsNullOrEmpty(ext.targetTech))
                {
                    continue;
                }
                int tier = BlueprintTargetExtension.GetTier(ext);
                if (tier < want - 1 || tier > want)
                {
                    continue;
                }
                if (tracker.IsTechUnlocked(ext.targetTech))
                {
                    continue; // 已解锁的科技不再掉书
                }
                result.Add(all[i]);
            }
            return result;
        }

        private static int CountUseful()
        {
            return UsefulPool().Count;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }
            if (CountFinished() < 8)
            {
                return false;
            }
            List<ThingDef> pool = UsefulPool();
            if (pool.Count == 0)
            {
                return false;
            }
            ThingDef pick = pool.RandomElement();
            Thing book = ThingMaker.MakeThing(pick);
            if (book == null)
            {
                return false;
            }
            GenPlace.TryPlaceThing(book, map.Center, map, ThingPlaceMode.Near);
            string label = def.letterLabel ?? "游学者的馈赠";
            string text = (def.letterText ?? "一位游学者匆匆路过, 遗落了一份文献: {0}。").Replace("{0}", book.LabelCap);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new GlobalTargetInfo(book), null, null);
            return true;
        }
    }

    // 2) IncidentWorker_RareBeastWandersIn — MO "稀有巨兽路过" 无自定义生物版:
    //    kindDef 指向任意原版/HSK 动物, 生成 1~max 只野生群 + 信。不做离开计时
    //    (对齐原版 AnimalsWanderIn 语义; MO 的 leaveMapAfterTime 需要其计时设施)。
    public class RareBeastProperties : DefModExtension
    {
        public string kindDef;
        public int min = 1;
        public int max = 2;
    }

    public class IncidentWorker_RareBeastWandersIn : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }
            RareBeastProperties props = def.GetModExtension<RareBeastProperties>();
            if (props == null || string.IsNullOrEmpty(props.kindDef))
            {
                return false;
            }
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(props.kindDef);
            if (kind == null)
            {
                Log.Warning("[BlueprintUnlockHSK] RareBeast kindDef 不存在: " + props.kindDef);
                return false;
            }
            int n = Rand.RangeInclusive(Mathf.Max(1, props.min), Mathf.Max(props.min, props.max));
            List<Pawn> spawned = new List<Pawn>();
            for (int i = 0; i < n; i++)
            {
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, map.Tile, true));
                if (p == null)
                {
                    continue;
                }
                IntVec3 c;
                if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.GetEdifice(map) == null && x.Standable(map), map, 20, out c))
                {
                    c = map.Center;
                }
                GenSpawn.Spawn(p, c, map);
                spawned.Add(p);
            }
            if (spawned.Count == 0)
            {
                return false;
            }
            string label = (def.letterLabel ?? "稀有巨兽").Replace("{0}", kind.GetLabelPlural()).Replace("{1}", spawned.Count.ToString());
            string text = (def.letterText ?? "一群{0}闯入了地区。它们危险, 但皮毛/材料价值不菲。")
                .Replace("{1}", spawned.Count.ToString()).Replace("{0}", kind.GetLabelPlural());
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new GlobalTargetInfo(spawned[0]), null, null);
            return true;
        }
    }

    // ============================================================================
    //  蓝图委托台 (2026-08-28 v4.2) — 定向获取蓝图的专用任务入口
    // ============================================================================
    //  复刻 探秘桌 的事件驱动范式 (玩家点击 + 消耗残页情报 + 天数冷却, 零 tick):
    //    · 点击「发布蓝图委托」→ 记下玩家当前最卡的档位到 WorldComponent 的 pendingCommission;
    //    · 生成一条原版 OpportunitySite_ItemStash 任务 (藏宝处);
    //    · 藏宝处散布时 (Patch_ItemStash.ScatterAt) 见到 pendingCommission → 保底投放一本
    //      「优先卡节点」蓝图书 (限该档) 并清除标志 → 卡哪个节点就能定向拿到哪本。
    public class CommissionProperties : DefModExtension
    {
        public int noteCost = 2;            // 消耗残页情报张数
        public int questPoints = 600;       // 生成任务的威胁点数
        public int minDaysBetween = 2;      // 两次委托最小间隔 (天)
        public int forceTier = 0;           // >0 固定委托档位; 0=自动取玩家最卡档
    }

    public class CompProperties_BlueprintCommissionHSK : CompProperties
    {
        public CompProperties_BlueprintCommissionHSK()
        {
            compClass = typeof(CompBlueprintCommissionHSK);
        }
    }

    public class CompBlueprintCommissionHSK : ThingComp
    {
        private int lastCommissionAbsDay = -9999;

        private CommissionProperties Props
        {
            get
            {
                CommissionProperties p = parent.def.GetModExtension<CommissionProperties>();
                if (p == null)
                {
                    Log.Warning("[BlueprintUnlockHSK] " + parent.def.defName + " 缺少 CommissionProperties");
                }
                return p;
            }
        }

        private static ThingDef NoteDef
        {
            get { return DefDatabase<ThingDef>.GetNamedSilentFail("RK_TornNote"); }
        }

        private static int NotesOnMap(Map map)
        {
            ThingDef note = NoteDef;
            if (note == null || map == null)
            {
                return 0;
            }
            List<Thing> list = map.listerThings.ThingsOfDef(note);
            if (list == null)
            {
                return 0;
            }
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].Destroyed)
                {
                    total += list[i].stackCount;
                }
            }
            return total;
        }

        private static void ConsumeNotes(Map map, int want)
        {
            ThingDef note = NoteDef;
            if (note == null || map == null)
            {
                return;
            }
            List<Thing> list = map.listerThings.ThingsOfDef(note);
            if (list == null)
            {
                return;
            }
            for (int i = list.Count - 1; i >= 0 && want > 0; i--)
            {
                Thing t = list[i];
                if (t == null || t.Destroyed || !t.Spawned)
                {
                    continue;
                }
                int take = want < t.stackCount ? want : t.stackCount;
                Thing cut = t.SplitOff(take);
                if (cut != null)
                {
                    cut.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    t.Destroy(DestroyMode.Vanish);
                }
                want -= take;
            }
        }

        private static float NowAbsDay
        {
            get { return GenTicks.TicksAbs / 60000f; }
        }

        // 0 = 不可委托; 否则可
        private int CanCommission(CommissionProperties p, out string whyNot)
        {
            whyNot = null;
            if (BlueprintGateDatabase.AllBlueprintDefs().Count == 0)
            {
                whyNot = "没有可投放的蓝图书";
                return 0;
            }
            float next = lastCommissionAbsDay + p.minDaysBetween;
            if (NowAbsDay < next)
            {
                whyNot = "线报已用尽, " + Mathf.CeilToInt(next - NowAbsDay) + " 天后可再次委托";
                return 0;
            }
            if (NotesOnMap(parent.Map) < p.noteCost)
            {
                whyNot = "残页情报不足: 需 " + p.noteCost + " 张, 当前 " + NotesOnMap(parent.Map) + " 张";
                return 0;
            }
            return 1;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent.Map == null || !parent.Map.IsPlayerHome)
            {
                yield break;
            }
            CommissionProperties p = Props;
            if (p == null)
            {
                yield break;
            }
            string whyNot;
            if (CanCommission(p, out whyNot) == 0)
            {
                yield break;
            }
            Command_Action act = new Command_Action();
            act.defaultLabel = "发布蓝图委托";
            act.defaultDesc = "把残页情报换成一条藏宝图委托: 一名线人会指认一处藏宝处, 里面必定藏着一份你正缺的蓝图。\n\n消耗残页情报 "
                              + p.noteCost + " 张。当你卡在某个科技节点时, 委托会优先给出该节点对应的蓝图书。";
            act.action = delegate { DoCommission(p); };
            yield return act;
        }

        private void DoCommission(CommissionProperties p)
        {
            string whyNot;
            if (CanCommission(p, out whyNot) == 0)
            {
                Messages.Message("无法发布委托: " + whyNot, parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            QuestScriptDef stash = DefDatabase<QuestScriptDef>.GetNamedSilentFail("OpportunitySite_ItemStash");
            if (stash == null)
            {
                Log.Warning("[BlueprintUnlockHSK] 找不到 OpportunitySite_ItemStash 任务脚本");
                Messages.Message("委托失败: 藏宝处任务不可用。", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            ConsumeNotes(parent.Map, p.noteCost);
            lastCommissionAbsDay = (int)NowAbsDay;

            // 先立保底标志, 再生成任务 (散布可能在生成时或玩家抵达时发生, 标志会一直保留到被消费)。
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            int tier = p.forceTier > 0 ? p.forceTier : BlueprintStuckMonitor.MostStuckTier();
            if (tracker != null)
            {
                tracker.pendingCommission = true;
                tracker.pendingCommissionTier = tier;
            }
            QuestUtility.GenerateQuestAndMakeAvailable(stash, p.questPoints);
            Find.LetterStack.ReceiveLetter("蓝图委托已发布",
                "一名线人递来一张藏宝图: 某处藏宝点里, 有人替你把一份研究蓝图塞进了箱子。带人去把它挖出来吧。",
                LetterDefOf.PositiveEvent, parent, null, null);
        }

        public override string CompInspectStringExtra()
        {
            CommissionProperties p = Props;
            if (p == null)
            {
                return null;
            }
            string whyNot;
            int ok = CanCommission(p, out whyNot);
            string text = "残页情报: " + NotesOnMap(parent.Map) + " / 委托需 " + p.noteCost;
            if (ok > 0)
            {
                text += ";可发布委托";
            }
            else if (!whyNot.NullOrEmpty())
            {
                text += ";暂不可委托(" + whyNot + ")";
            }
            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref lastCommissionAbsDay, "lastCommissionAbsDay", -9999);
        }
    }
}

