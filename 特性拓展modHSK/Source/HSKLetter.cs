// 特性拓展modHSK —— 授予/退场信件文案层 (v3 · 动作清单法)
//
// 一封信 = 标题 + 正文(先…然后…接着… 三动作三物件) + 习惯定型句 + 收束「{PAWN}现在是{TRAIT}。」
//   标题/正文 取自 Head.{slug}.{n} / Scene.{slug}.{n}   —— 同一变体号成对取用, 保证动作与标题讲同一件事
//   习惯句 取自 Habit.{defName}.{n}                     —— 与成因无关, 只描述这个人此后改不掉的动作
//   机制一改用 FrameHead.{f} / FrameScene.{f}.{n}
//   收束句固定由代码拼「{PAWN_labelShort}现在是{TRAIT}。」, 不进文案表(硬规: 不用"获得/染上")
// 每个槽位至少 4 套变体, 同一个小人不会连着两次拿到同一套(gen_lastVar 记忆)。
// 任一槽位在当前语言缺失时逐级回退到 HSKTrait.Generic.*(繁中/英文只铺通用层), 绝不露裸 key。
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    [StaticConstructorOnStartup]
    public static class HSKLetter
    {
        // 段落分隔: 用 \u000a 转义写换行, 避免被编辑工具二次转义
        private static readonly string NL = "\u000a\u000a";
        private static readonly string NL1 = "\u000a";

        private static Dictionary<string, string> slugOf;
        private static Dictionary<string, int> sceneVars;
        private static Dictionary<string, int> habitVars;
        private static Dictionary<string, int> frameVars;

        static HSKLetter()
        {
            slugOf = new Dictionary<string, string>();
            foreach (var row in HSKLetterData.CauseSlug)
                if (row != null && row.Length >= 2) slugOf[row[0]] = row[1];
            sceneVars = ToDict(HSKLetterData.SceneVariants);
            habitVars = ToDict(HSKLetterData.HabitVariants);
            frameVars = ToDict(HSKLetterData.FrameSceneVariants);
        }

        private static Dictionary<string, int> ToDict(string[][] rows)
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            if (rows == null) return d;
            foreach (var r in rows)
            {
                int v;
                if (r != null && r.Length >= 2 && int.TryParse(r[1], out v)) d[r[0]] = v;
            }
            return d;
        }

        private static string Need(string key, List<NamedArgument> args)
        {
            string t = Try(key, args);
            return t ?? "";
        }

        private static string Try(string key, List<NamedArgument> args)
        {
            TaggedString raw;
            if (!key.TryTranslate(out raw)) return null;
            string txt = (string)raw;
            if (txt.NullOrEmpty()) return null;
            return txt.Translate(args.ToArray());
        }

        // 取变体号: 尽量避开该小人上一次用过的编号
        private static int PickVar(HSKPawnEntry e, string slot, int count)
        {
            if (count <= 0) return 0;
            if (count == 1) return 1;
            int last = -1;
            if (e != null && e.lastVar != null) e.lastVar.TryGetValue(slot, out last);
            int v;
            if (last > 0)
            {
                v = 1 + Rand.Range(0, count - 1);       // 从"不是上一个"的集合里取
                if (v >= last) v++;
            }
            else v = 1 + Rand.Range(0, count);
            if (e != null)
            {
                if (e.lastVar == null) e.lastVar = new Dictionary<string, int>();
                e.lastVar[slot] = v;
            }
            return v;
        }

        // ---- 授予信 ----
        // dimensionKey: 机制二的行为维度; frameIndex: 机制一外壳(0 基), -1 = 非机制一
        public static void SendGrant(Pawn pawn, TraitDef td, string dimensionKey, int frameIndex, bool negative, int count)
        {
            HSKPawnEntry e = (HSKLedger.Game == null) ? null : HSLedgerPeek(pawn);
            var args = BaseArgs(pawn, td, count);
            string title = null, scene = null;

            string slug = SlugOf(dimensionKey);
            if (slug != null)
            {
                int v = PickVar(e, "S:" + slug, sceneVars.TryGetValueOr(slug, 0));
                title = Try("HSKTrait.Head." + slug + "." + v, args);
                scene = Try("HSKTrait.Scene." + slug + "." + v, args);
            }
            else if (frameIndex >= 0 && frameIndex < HSKLetterData.FrameCount)
            {
                string f = (frameIndex + 1).ToString();
                int v = PickVar(e, "F:" + f, frameVars.TryGetValueOr(f, 0));
                title = Try("HSKTrait.FrameHead." + f, args);
                scene = Try("HSKTrait.FrameScene." + f + "." + v, args);
            }
            if (scene.NullOrEmpty())
            {
                if (title.NullOrEmpty()) title = Try("HSKTrait.Generic.grantTitle", args);
                scene = Try("HSKTrait.Generic.grantBody", args);
                Post(pawn, title, scene, negative);
                return;
            }

            string habit = TryHabit(e, td, args);
            if (habit.NullOrEmpty())
            {
                scene = Try("HSKTrait.Generic.grantBody", args);   // 没有习惯句时退化成通用句(已含"现在是")
                Post(pawn, title, scene, negative);
                return;
            }
            string closer = Need("HSKTrait.Generic.closerGain", args) + NL1 + Highlight(pawn, td);
            Post(pawn, title, Join(scene, habit, closer), negative);
        }

        private static string TryHabit(HSKPawnEntry e, TraitDef td, List<NamedArgument> args)
        {
            int n = habitVars.TryGetValueOr(td.defName, 0);
            if (n <= 0) return null;
            int v = PickVar(e, "H:" + td.defName, n);
            return Try("HSKTrait.Habit." + td.defName + "." + v, args);
        }

        // ---- 退场信 ----
        // kind: "abstain"=禁毒达成, 其它=性格漂移
        public static void SendRevoke(Pawn pawn, TraitDef td, string kind, int days)
        {
            HSKPawnEntry e = (HSKLedger.Game == null) ? null : HSLedgerPeek(pawn);
            var args = BaseArgs(pawn, td, days);
            bool abstain = (kind == "abstain");
            string slot = abstain ? "A" : "X";
            int total = abstain ? HSKLetterData.AbstainVariants : HSKLetterData.ExitVariants;
            int v = PickVar(e, slot, total);
            string body = Try("HSKTrait." + (abstain ? "Abstain." : "Exit.") + v, args);
            if (body.NullOrEmpty()) body = Try("HSKTrait.Generic.revokeBody", args);
            string closer = Need("HSKTrait.Generic.closerLose", args) + NL1 + Highlight(pawn, td);
            string title = Try(abstain ? "HSKTrait.Generic.abstainTitle" : "HSKTrait.Generic.revokeTitle", args);
            Post(pawn, title, Join(body, closer), true);
        }

        // Trait Rarity Colors(carnysenpai.traitraritycolors)会把稀有度色串直接写进
        // TraitDegreeData.label(形如 <color=#hex>夜猫子</color>), 而它同时把 Trait.Label 里的色串剥掉。
        // 所以这里读 degreeData.label 拿染色, 再套一层灰色外边框, 让特性名单独成行时更突出。
        // 未安装该 mod / 未刷新时拿不到色串, 退化成只有边框。
        public static string Highlight(Pawn pawn, TraitDef td)
        {
            string inner = null;
            try
            {
                int degree = 0;
                if (pawn != null && pawn.story != null && pawn.story.traits != null)
                {
                    foreach (Trait t in pawn.story.traits.allTraits)
                    {
                        if (t != null && t.def == td) { degree = t.Degree; break; }
                    }
                }
                // 1.6 无公开的 TraitDef.DegreeData(int), 直接扫 degreeDatas
                if (td != null && td.degreeDatas != null)
                {
                    foreach (TraitDegreeData dd in td.degreeDatas)
                    {
                        if (dd != null && dd.degree == degree && !dd.label.NullOrEmpty()) { inner = dd.label; break; }
                    }
                }
            }
            catch { }
            if (inner.NullOrEmpty() && td != null)
            {
                string l = (string)td.LabelCap;
                inner = string.IsNullOrEmpty(l) ? td.defName : l;
            }
            return FRAME_OPEN + inner + FRAME_CLOSE;
        }

        private const string FRAME_OPEN = "<color=#a8a8a8>【";
        private const string FRAME_CLOSE = "】</color>";

        private static HSKPawnEntry HSLedgerPeek(Pawn pawn)
        {
            try { HSKTraitLedger led = HSKLedger.Game; return (led == null) ? null : led.PeekEntry(pawn); }
            catch { return null; }
        }

        private static string SlugOf(string dimensionKey)
        {
            string s;
            return (dimensionKey != null && slugOf.TryGetValue(dimensionKey, out s)) ? s : null;
        }

        private static string Join(params string[] parts)
        {
            string s = "";
            foreach (string t in parts)
            {
                if (t.NullOrEmpty()) continue;
                s = (s.Length == 0) ? t : s + NL + t;
            }
            return s;
        }

        private static List<NamedArgument> BaseArgs(Pawn pawn, TraitDef td, int count)
        {
            var list = new List<NamedArgument>();
            if (pawn != null) list.Add(pawn.Named("PAWN"));
            list.Add(Highlight(pawn, td).Named("TRAIT"));
            list.Add(count.Named("COUNT"));
            return list;
        }

        private static void Post(Pawn pawn, string title, string body, bool negative)
        {
            try
            {
                if (title.NullOrEmpty()) title = negative ? "习惯停了" : "性格变了";
                if (Find.LetterStack != null)
                    Find.LetterStack.ReceiveLetter(title, body ?? "",
                        negative ? LetterDefOf.NegativeEvent : LetterDefOf.PositiveEvent, new LookTargets(pawn));
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] 信件组装失败: " + ex); }
        }
    }

    internal static class StrExt
    {
        public static int TryGetValueOr(this Dictionary<string, int> d, string k, int fallback)
        {
            int v;
            return (d != null && d.TryGetValue(k, out v)) ? v : fallback;
        }
    }
}
