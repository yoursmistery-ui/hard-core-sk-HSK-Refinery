// 特性拓展modHSK —— 机制二: 行为计数挂号 (v2)
// 拦截: Pawn_JobTracker.EndCurrentJob(前缀取结工任务) + Pawn.Kill(击杀计数)
//       + MentalStateHandler.TryStartMentalState(精神崩溃计数, 唯一真正的"负向"独立计数器)
//
// v2 变更(2026-08-27):
//   1. ★修正"同源正负计数"结构性错误: 旧表把互斥的正负特性挂在同一个计数器上
//      (例: 夜间劳作计数 → 达标给"夜猫子", 计数略低给"早起鸟"), 于是"熬夜越多越可能拿到早起鸟"。
//      现在每个计数器只对应一条因果: 夜行/晨行、耐热/耐寒各自独立计数, 负面特性改由
//      真正度量负面行为的计数器(精神崩溃 / 虐待伴侣动物)驱动。
//   2. 只统计玩家可控殖民者, 其余直接 return(热路径减负, AGENTS.md §9)。
//   3. 规则按 TriggerType 预分桶, 每次收工只遍历同类规则, 不再全表扫描; 复用 HashSet 缓冲避免逐次分配。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HSKTraitExt
{
    public static class HSKBehaviorHook
    {
        // TriggerType -> 规则桶(首次使用时懒建)
        private static Dictionary<string, List<HSKData.LifestyleRule>> buckets;
        private static readonly HashSet<string> typeBuffer = new HashSet<string>();

        private static Dictionary<string, List<HSKData.LifestyleRule>> Buckets
        {
            get
            {
                if (buckets == null)
                {
                    buckets = new Dictionary<string, List<HSKData.LifestyleRule>>();
                    foreach (var r in HSKData.LifestyleRules)
                    {
                        List<HSKData.LifestyleRule> l;
                        if (!buckets.TryGetValue(r.TriggerType, out l)) { l = new List<HSKData.LifestyleRule>(); buckets[r.TriggerType] = l; }
                        l.Add(r);
                    }
                }
                return buckets;
            }
        }

        public static void Apply(Harmony harmony)
        {
            MethodInfo end = FindEndCurrentJob();
            if (end != null)
            {
                harmony.Patch(end,
                    prefix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("JobEndPrefix", BindingFlags.Static | BindingFlags.NonPublic)),
                    postfix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("JobEndPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
            }
            MethodInfo kill = typeof(Pawn).GetMethod("Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) });
            if (kill != null)
                harmony.Patch(kill, postfix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("KillPostfix", BindingFlags.Static | BindingFlags.NonPublic)));

            MethodInfo breakStart = FindTryStartMentalState();
            if (breakStart != null)
                harmony.Patch(breakStart, postfix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("MentalBreakPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
        }

        private static MethodInfo FindEndCurrentJob()
        {
            string[] names = { "Verse.Pawn_JobTracker", "Verse.AI.Pawn_JobTracker" };
            foreach (var n in names)
            {
                Type t = AccessTools.TypeByName(n);
                if (t == null) continue;
                MethodInfo m = t.GetMethod("EndCurrentJob", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null) return m;
            }
            return null;
        }

        // TryStartMentalState 存在多重载风险 → 显式按"返回 bool 且首参 MentalStateDef"挑主重载,
        // 规避 [HarmonyPatch] 字符串式声明的 AmbiguousMatchException(连带整个 PatchAll 崩)。
        private static MethodInfo FindTryStartMentalState()
        {
            Type t = AccessTools.TypeByName("RimWorld.MentalStateHandler");
            if (t == null) t = AccessTools.TypeByName("Verse.MentalStateHandler");
            if (t == null) return null;
            MethodInfo best = null;
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name != "TryStartMentalState") continue;
                if (m.ReturnType != typeof(bool)) continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 0 || ps[0].ParameterType != typeof(MentalStateDef)) continue;
                if (best == null || ps.Length > best.GetParameters().Length) best = m;
            }
            return best;
        }

        // 热路径闸门: 非玩家殖民者 / 机制二关闭 → 一律零开销返回
        private static bool Countable(Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonistPlayerControlled) return false;
            if (pawn.story == null || pawn.story.traits == null) return false;
            if (HSKTraitMod.settings == null) return false;
            if (!HSKTraitMod.settings.enableMechanismTwo) return false;
            return true;
        }

        // 前缀: 读当前即将结束的任务上下文
        private static void JobEndPrefix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                Pawn pawn = GetFieldValue<Pawn>(__instance, "pawn");
                if (!Countable(pawn)) return;
                Job job = GetFieldValue<Job>(__instance, "curJob");
                if (job == null) return;
                HSKPawnEntry e = HSKLedger.Game == null ? null : HSKLedger.Game.EntryFor(pawn);
                if (e == null) return;
                JobDef jd = job.def;
                e.pendingJobDef   = (jd != null) ? jd.defName : "";
                e.pendingWorkType = "";
                e.pendingJoyKind  = (jd != null && jd.joyKind != null) ? jd.joyKind.defName : "";
                Thing t = job.GetTarget(TargetIndex.A).Thing;
                e.pendingTargetDef = (t != null && t.def != null) ? t.def.defName : "";
                e.pendingHour = GenLocalDate.HourInteger(pawn);
                // Thing.AmbientTemperature = GenTemperature.GetTemperatureForCell(Position, Map), 收工时一次调用可接受
                e.pendingRoomTemp = pawn.AmbientTemperature;
                e.pendingSuccess = true;
            }
            catch { }
        }

        private static void JobEndPostfix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                Pawn pawn = GetFieldValue<Pawn>(__instance, "pawn");
                if (!Countable(pawn)) return;
                HSKPawnEntry e = HSKLedger.Game == null ? null : HSKLedger.Game.EntryFor(pawn);
                if (e == null || !e.pendingSuccess) return;

                Dispatch(pawn, e);

                // 记录"真实摄入毒品"的时间(禁毒X年移除成瘾特性用): 仅 Ingest/Eat 摄入动作算,
                // 搬运/加工等只是碰到毒品不算吸毒。
                string jd = e.pendingJobDef ?? "";
                if ((jd == "Ingest" || jd == "Eat") && !String.IsNullOrEmpty(DrugKey(e.pendingTargetDef ?? "")))
                    e.lastDrugUseTick = Find.TickManager.TicksGame;

                e.pendingSuccess = false;
                e.pendingJobDef = e.pendingWorkType = e.pendingJoyKind = e.pendingTargetDef = null;
            }
            catch { }
        }

        private static void KillPostfix(object __instance, DamageInfo? dinfo)
        {
            try
            {
                Pawn victim = __instance as Pawn;
                if (victim == null || victim.def == null || victim.def.race == null || !victim.def.race.Humanlike) return;
                if (!dinfo.HasValue) return;
                Pawn killer = dinfo.Value.Instigator as Pawn;
                if (!Countable(killer)) return;
                HSKPawnEntry e = HSKLedger.Game == null ? null : HSKLedger.Game.EntryFor(killer);
                if (e == null) return;
                IncrementAndMaybeGrant(killer, e, "击杀人类", "Kill");
            }
            catch { }
        }

        // 精神崩溃 = 真正的负向行为计数(压力崩溃维度), 供 VTE_WorldWeary 等负向特性使用
        private static void MentalBreakPostfix(object __instance, bool __result)
        {
            try
            {
                if (!__result) return;
                Pawn pawn = GetFieldValue<Pawn>(__instance, "pawn");
                if (!Countable(pawn)) return;
                HSKPawnEntry e = HSKLedger.Game == null ? null : HSKLedger.Game.EntryFor(pawn);
                if (e == null) return;
                IncrementAndMaybeGrant(pawn, e, "压力崩溃", "MentalBreak");
            }
            catch { }
        }

        // ---- 分发: 按触发类型取桶, 命中的规则各自计数 ----
        private static void Dispatch(Pawn pawn, HSKPawnEntry e)
        {
            string jd = e.pendingJobDef ?? "";
            string tg = e.pendingTargetDef ?? "";
            IEnumerable<string> types = ActiveTypes(e, jd, tg);
            foreach (string tt in types)
            {
                List<HSKData.LifestyleRule> rules;
                if (!Buckets.TryGetValue(tt, out rules)) continue;
                foreach (var r in rules)
                {
                    if (!Match(e, r, tt, jd, tg)) continue;
                    IncrementAndMaybeGrant(pawn, e, r.Key, tt);
                }
            }
        }

        // 复用缓冲, 避免每次收工 new HashSet
        private static HashSet<string> ActiveTypes(HSKPawnEntry e, string jd, string tg)
        {
            typeBuffer.Clear();
            typeBuffer.Add("Job");
            typeBuffer.Add("WorkType");
            if (e.pendingJoyKind != null && e.pendingJoyKind.Length > 0) typeBuffer.Add("JoyKind");
            if (jd.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 || jd == "Harvest") typeBuffer.Add("Plant");
            if (jd == "Eat" || jd == "Ingest") { typeBuffer.Add("Eat"); typeBuffer.Add("Drug"); }
            if (jd == "ButcherCorpse") typeBuffer.Add("AbuseAnimal");
            int h = e.pendingHour;
            if (h < 6 || h >= 22) typeBuffer.Add("Night");
            else if (h >= 6 && h < 9) typeBuffer.Add("Dawn");
            if (e.pendingRoomTemp > HotTemp) typeBuffer.Add("TempHot");
            else if (e.pendingRoomTemp < ColdTemp) typeBuffer.Add("TempCold");
            return typeBuffer;
        }

        private const float HotTemp = 35f;   // ℃, 高于此算"耐热作业"
        private const float ColdTemp = 5f;   // ℃, 低于此算"耐寒作业"

        private static bool Match(HSKPawnEntry e, HSKData.LifestyleRule r, string tt, string jd, string tg)
        {
            switch (tt)
            {
                case "Plant":
                    return jd.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 || jd == "Harvest";
                case "Job":
                    return jd != "" && String.Equals(jd, r.TriggerTarget, StringComparison.OrdinalIgnoreCase);
                case "WorkType":
                    if (r.TriggerTarget == "Construction") return IsConstructionJob(jd);
                    if (r.TriggerTarget == "Art") return IsArtJob(jd);
                    return false;
                case "JoyKind":
                    return e.pendingJoyKind != "" && String.Equals(e.pendingJoyKind, r.TriggerTarget, StringComparison.OrdinalIgnoreCase);
                case "Eat":
                    return jd == "Eat" || jd == "Ingest";
                case "Night":
                    return e.pendingHour < 6 || e.pendingHour >= 22;
                case "Dawn":
                    return e.pendingHour >= 6 && e.pendingHour < 9;
                case "TempHot":
                    return e.pendingRoomTemp > HotTemp;
                case "TempCold":
                    return e.pendingRoomTemp < ColdTemp;
                case "AbuseAnimal":
                    // 屠宰"可作伴侣的物种"(petness>0)算虐待动物; 普通家畜/猎物不计。
                    return IsCompanionSpecies(tg);
                case "Drug":
                    if (jd != "Ingest" && jd != "Eat") return false;
                    return DrugKey(tg) == r.TriggerTarget;
                default:
                    return false;
            }
        }

        private static bool IsCompanionSpecies(string tdef)
        {
            if (String.IsNullOrEmpty(tdef)) return false;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(tdef);
            if (def == null || def.race == null || !def.race.Animal) return false;
            return def.race.petness > 0f;
        }

        private static bool IsArtJob(string jd)
        {
            if (String.IsNullOrEmpty(jd)) return false;
            switch (jd)
            {
                case "BuildArtifact": case "Sculpt": case "CreateArt":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsConstructionJob(string jd)
        {
            if (String.IsNullOrEmpty(jd)) return false;
            switch (jd)
            {
                case "FinishFrame": case "Repair": case "FixBrokenDownBuilding":
                case "FillIn": case "DeconstructForBlueprint": case "ConstructRoof":
                    return true;
                default:
                    return false;
            }
        }

        private static string DrugKey(string tdef)
        {
            // 只认"消遣类成瘾药物"(Social/Hard), 医用(Medical)与非毒品一律不计。
            // 旧实现把一切非烟酒目标都归为 Other, 导致吃饭/干活都算"药物滥用", 已修复。
            if (String.IsNullOrEmpty(tdef)) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(tdef);
            if (def == null || def.ingestible == null) return null;
            DrugCategory dc = def.ingestible.drugCategory;
            if (dc == DrugCategory.None || dc == DrugCategory.Medical) return null;
            string l = tdef.ToLowerInvariant();
            if (l.Contains("smokeleaf")) return "Smokeleaf";   // 烟叶/大麻烟
            if (l.Contains("beer") || l.Contains("wort") || l.Contains("wine") || l.Contains("alcohol")) return "Alcohol";
            return "Other";                                     // Yayo/Flake/GoJuice/WakeUp/仙馔蜜露等 → 药物滥用
        }

        // ---- 达标后按概率授予(取代"达标即100%获得") ----
        // 每条规则达标(计数≥阈值)时消费本次资格并掷一次骰:
        //   通过 → 授予; 失败 → 本次资格作废, 需重新积累到阈值才有下一次机会。
        // v2: 阈值/概率已在 表/规则-生活习惯特性.csv 里整体抬高(负面特性再叠 neg* 系数),
        //     这里另乘设置里的全局倍率; 门控未开窗时不消费计数, 等窗口打开再判定。
        private static void IncrementAndMaybeGrant(Pawn pawn, HSKPawnEntry e, string key, string triggerType)
        {
            e.AddCount(key, 1);
            List<HSKData.LifestyleRule> rules;
            if (!Buckets.TryGetValue(triggerType, out rules)) return;
            HSKTraitSetting s = HSKTraitMod.settings;
            float tscale = (s != null && s.thresholdScale > 0f) ? s.thresholdScale : 1f;

            foreach (var r in rules)
            {
                if (r.Key != key) continue;
                int cnt = e.GetCount(key);

                int posTh = (int)(r.PosThreshold * tscale);
                float posCh = r.Chance;
                // 三种毒品可分别定制阈值/概率(设置优先, 否则用规则表默认值)
                if (IsDrugKey(key))
                {
                    int dth = DrugThresholdFor(key);
                    if (dth > 0) posTh = (int)(dth * tscale);
                    float dch = DrugChanceFor(key);
                    if (dch > 0f) posCh = dch;
                }
                int negTh = (int)(r.NegThreshold * tscale);

                if (posTh > 0 && r.PosTrait != "" && cnt >= posTh)
                {
                    if (!HSKTraits.CanAcquireNow(pawn, e, false)) return;  // 未开窗: 不消费计数
                    e.AddCount(key, -posTh);                              // 开窗时无论成败都消费本次资格
                    if (!RollChance(posCh, false)) return;
                    HSKTraits.Grant(pawn, r.PosTrait, r.PosDegree, "生活习惯:" + key, false, key, -1, cnt);
                }
                else if (negTh > 0 && r.NegTrait != "" && cnt >= negTh)
                {
                    if (!HSKTraits.CanAcquireNow(pawn, e, true)) return;
                    e.AddCount(key, -negTh);
                    if (!RollChance(r.Chance, true)) return;
                    HSKTraits.Grant(pawn, r.NegTrait, r.NegDegree, "生活习惯恶化:" + key, true, key, -1, cnt);
                }
            }
        }

        private static bool IsDrugKey(string key)
        {
            return key == "饮酒" || key == "吸烟" || key == "药物滥用";
        }

        private static int DrugThresholdFor(string key)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null) return 0;
            if (key == "饮酒") return s.drugAlcoholThreshold;
            if (key == "吸烟") return s.drugSmokeleafThreshold;
            if (key == "药物滥用") return s.drugHardThreshold;
            return 0;
        }

        private static float DrugChanceFor(string key)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null) return -1f;
            if (key == "饮酒") return s.drugAlcoholChance;
            if (key == "吸烟") return s.drugSmokeleafChance;
            if (key == "药物滥用") return s.drugHardChance;
            return -1f;
        }

        // 概率骰: 规则自带 Chance × 全局倍率; 负面特性再乘 negChanceScale(默认 0.4)
        private static bool RollChance(float chance, bool negative)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            float p = chance;
            if (s != null)
            {
                p *= s.grantChanceScale;
                if (negative) p *= s.negChanceScale;
            }
            if (p <= 0f) return false;
            if (p >= 1f) return true;
            return Rand.Chance(p);
        }

        private static T GetFieldValue<T>(object inst, string field)
        {
            var f = inst.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return default(T);
            object v = f.GetValue(inst);
            return (v is T) ? (T)v : default(T);
        }
    }
}
