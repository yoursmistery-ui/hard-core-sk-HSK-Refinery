using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace JobEffects
{
    /// <summary>
    /// Dev-only LIVE tuner for a JobToolDef's swing geometry, drawn inside the tool gallery dialog
    /// (Tool gallery → Dev mode). The gallery already owns a frozen model whose body facing follows
    /// ToolAnimator.GalleryFacing, so you get N/E/S/W + sliders in one window and every slider takes
    /// effect on the very next frame -- no rebuild, no game restart.
    ///
    /// Why this exists: tuning "the book is too close / too low / wrong size" by editing XML and
    /// restarting the game is a 2-minute round trip per guess, and each XML-only edit silently
    /// applies to ALL four facings at once. Every field below is a separate facing, so the fastest
    /// path is: drag until it looks right, hit DUMP, and hand the printed XML block back.
    ///
    /// Mutating a Def's public fields at runtime is safe here: defs are plain objects, the render
    /// path re-reads them every frame, and nothing writes them back to disk. The edits are gone on
    /// the next game restart unless they are baked into the mod's XML.
    /// </summary>
    internal static class ToolTuner
    {
        internal sealed class Field
        {
            public string Name;
            public Func<JobToolDef, float> Get;
            public Action<JobToolDef, float> Set;
            public float Min, Max;
            public bool ReadOnly;      // only meaningful for swingStyle == Read
            public string Tip;
        }

        private static List<Field> fields;

        // First-seen value of every field per defName, so "Reset" restores the XML values.
        private static readonly Dictionary<string, float[]> snapshots = new Dictionary<string, float[]>();

        internal static List<Field> Fields
        {
            get
            {
                if (fields != null) return fields;
                fields = new List<Field>
                {
                    new Field { Name = "scale", Min = 0.2f, Max = 1.6f,
                        Get = t => t.scale, Set = (t, v) => t.scale = v,
                        Tip = "Overall prop size (cells). Bigger = more exaggerated, but the hands ride along." },

                    new Field { Name = "reach", Min = 0f, Max = 1.2f,
                        Get = t => t.reach, Set = (t, v) => t.reach = v,
                        Tip = "Base forward reach. Read falls back to reach*0.30 (north) / reach*0.70 (everything else) "
                            + "whenever the facing-specific value below is negative." },

                    new Field { Name = "holdRaise", Min = -0.3f, Max = 0.5f,
                        Get = t => t.holdRaise, Set = (t, v) => t.holdRaise = v,
                        Tip = "+z is UP-SCREEN (away from the camera). Raises the held prop for every facing." },

                    new Field { Name = "readLiftRise", Min = 0f, Max = 0.6f, ReadOnly = true,
                        Get = t => t.readLiftRise, Set = (t, v) => t.readLiftRise = v,
                        Tip = "Read: extra up-screen push during the 'lift to read' half of the cycle, as a fraction of scale. "
                            + "THIS is the term that shoves the book behind the torso -- 0 keeps it flat." },

                    new Field { Name = "reachEastWest", Min = -1.2f, Max = 1.5f, ReadOnly = true,
                        Get = t => t.reachEastWest, Set = (t, v) => t.reachEastWest = v,
                        Tip = "Read, EAST/WEST: a true sideways push (screen left/right) -- the only field that really "
                            + "'extends outward'. Negative = fall back to reach*0.70. Practical ceiling ~0.7: the arms are "
                            + "'soft' shoulder-to-palm sleeves and stretch into noodles beyond that." },

                    new Field { Name = "reachSouth", Min = -1.2f, Max = 1.5f, ReadOnly = true,
                        Get = t => t.reachSouth, Set = (t, v) => t.reachSouth = v,
                        Tip = "Read, SOUTH: 'forward' here is DOWN-SCREEN, not toward the camera. So BIGGER = the prop drops "
                            + "toward the lap / the bench, SMALLER = held up over the chest. Negative = reach*0.70." },

                    new Field { Name = "reachNorth", Min = -1.2f, Max = 1.5f, ReadOnly = true,
                        Get = t => t.reachNorth, Set = (t, v) => t.reachNorth = v,
                        Tip = "Read, NORTH: 'forward' is UP-SCREEN (away from the camera), so BIGGER = higher, over the head. "
                            + "Needs neverUnderBody or the whole stack hides under the body. Negative = reach*0.30." },

                    new Field { Name = "holdLateralEastWest", Min = -0.7f, Max = 0.7f, ReadOnly = true,
                        Get = t => t.holdLateralEastWest, Set = (t, v) => t.holdLateralEastWest = v,
                        Tip = "Read, profile: slide along the pawn's own left/right axis (i.e. chest <-> back). "
                            + "West mirrors automatically. Use it to clear the shoulder silhhouette." },

                    new Field { Name = "holdLateralNorth", Min = -0.7f, Max = 0.7f, ReadOnly = true,
                        Get = t => t.holdLateralNorth, Set = (t, v) => t.holdLateralNorth = v,
                        Tip = "Read, NORTH: same axis slide as the profile one, for clearing the body outline in back view." },

                    new Field { Name = "volScaleProfile", Min = 0.4f, Max = 1.6f, ReadOnly = true,
                        Get = t => t.volScaleProfile, Set = (t, v) => t.volScaleProfile = v,
                        Tip = "Read, profile: extra size multiplier (1 = exactly the front-view size). Value 0.85~0.95 shrinks "
                            + "the prop because it looks oversized next to a narrow side-on body." },

                    new Field { Name = "volScaleSouth", Min = 0.4f, Max = 1.6f, ReadOnly = true,
                        Get = t => t.volScaleSouth, Set = (t, v) => t.volScaleSouth = v,
                        Tip = "Read, south: extra size multiplier, to stay consistent with a scaled-down profile." },

                    new Field { Name = "bookGripHalfWidth", Min = 0f, Max = 0.9f, ReadOnly = true,
                        Get = t => t.bookGripHalfWidth, Set = (t, v) => t.bookGripHalfWidth = v,
                        Tip = "Read: how far apart the two fists sit along the prop (fraction of its size). Also decides how "
                            + "far the outer hand travels, so it drives the arm stretch." },

                    new Field { Name = "bookGripDrop", Min = 0f, Max = 0.9f, ReadOnly = true,
                        Get = t => t.bookGripDrop, Set = (t, v) => t.bookGripDrop = v,
                        Tip = "Read: how far BELOW the prop centre the two fists sit." },
                };
                return fields;
            }
        }

        private static bool HasReadFields(JobToolDef t) => t != null && t.swingStyle == SwingStyle.Read;

        internal static void SnapshotIfNeeded(JobToolDef t)
        {
            if (t == null || t.defName == null || snapshots.ContainsKey(t.defName)) return;
            List<Field> f = Fields;
            float[] arr = new float[f.Count];
            for (int i = 0; i < f.Count; i++) arr[i] = f[i].Get(t);
            snapshots[t.defName] = arr;
        }

        /// <summary>Drops every field back to the value the XML gave it when the dialog first opened.</summary>
        internal static bool Reset(JobToolDef t)
        {
            if (t == null || t.defName == null) return false;
            if (!snapshots.TryGetValue(t.defName, out float[] arr)) return false;
            List<Field> f = Fields;
            for (int i = 0; i < f.Count && i < arr.Length; i++) f[i].Set(t, arr[i]);
            return true;
        }

        /// <summary>The live block, appended to the gallery dialog under the facing buttons.</summary>
        internal static void Draw(JobToolDef t, Listing_Standard l)
        {
            if (t == null) return;
            SnapshotIfNeeded(t);

            l.GapLine(6f);
            Text.Font = GameFont.Small;
            l.Label("LIVE TUNE — 改动立刻生效, 不用重启");
            l.Label("(↑/↓ 切朝向, 拖完点 DUMP 把数值存成文件)");

            bool read = HasReadFields(t);
            List<Field> f = Fields;
            for (int i = 0; i < f.Count; i++)
            {
                if (f[i].ReadOnly && !read) continue;
                Row(l, t, f[i]);
            }

            if (read)
            {
                Rect cbRow = l.GetRect(22f);
                if (Mouse.IsOver(cbRow))
                    TooltipHandler.TipRegion(cbRow,
                        "Read only: keep the whole stack drawn OVER the body in every facing. Without it a "
                        + "north-facing pawn (back to the camera) has the book tucked under the torso and it vanishes.");
                bool nub = t.neverUnderBody;
                Widgets.CheckboxLabeled(cbRow, "neverUnderBody", ref nub);
                t.neverUnderBody = nub;
            }

            l.Gap(2f);
            Rect br = l.GetRect(28f);
            Rect bReset = br.LeftPart(0.34f).ContractedBy(2f);
            Rect bDump = br.RightPart(0.66f).ContractedBy(2f);

            if (Widgets.ButtonText(bReset, "Reset"))
                Messages.Message(Reset(t) ? "Tool tuner: \u8fd8\u539f\u5230 XML \u539f\u503c" : "Tool tuner: \u65e0\u5feb\u7167",
                    MessageTypeDefOf.TaskCompletion, false);

            if (Widgets.ButtonText(bDump, "DUMP \u6570\u503c -> JETuner.txt"))
                Dump(t);
        }

        private static void Row(Listing_Standard l, JobToolDef t, Field f)
        {
            Rect r = l.GetRect(21f);
            if (Mouse.IsOver(r)) TooltipHandler.TipRegion(r, f.Tip ?? f.Name);

            float v = f.Get(t);
            var lab = new Rect(r.x, r.y + 2f, 132f, r.height);
            var val = new Rect(r.x + 134f, r.y + 2f, 44f, r.height);
            var sl = new Rect(r.x + 180f, r.y + 3f, Mathf.Max(40f, r.width - 182f), r.height - 6f);

            Widgets.Label(lab, f.Name);
            Widgets.Label(val, v.ToString("0.###", CultureInfo.InvariantCulture));

            float nv = Widgets.HorizontalSlider(sl, v, f.Min, f.Max, false, null, null, null, 0.01f);
            if (Math.Abs(nv - v) > 0.0005f) f.Set(t, nv);
        }

        /// <summary>
        /// Writes the tuned values both to Player.log and to a plain text file next to the game's
        /// config, as a ready-to-paste XML block, so the numbers can be baked back into the mod.
        /// </summary>
        internal static void Dump(JobToolDef t)
        {
            if (t == null) return;
            List<Field> f = Fields;
            bool read = HasReadFields(t);

            var sb = new StringBuilder();
            sb.Append("=== JETuner ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" ===\n");
            sb.Append("defName    : ").Append(t.defName).Append('\n');
            sb.Append("swingStyle : ").Append(t.swingStyle).Append("    facing: ").Append(ToolAnimator.GalleryFacing).Append('\n');
            sb.Append("--- XML (paste back into the tool's JobToolDef) ---\n");
            for (int i = 0; i < f.Count; i++)
            {
                if (f[i].ReadOnly && !read) continue;
                sb.Append("    <").Append(f[i].Name).Append('>')
                  .Append(f[i].Get(t).ToString("0.###", CultureInfo.InvariantCulture))
                  .Append("</").Append(f[i].Name).Append(">\n");
            }
            if (read)
                sb.Append("    <neverUnderBody>").Append(t.neverUnderBody ? "true" : "false").Append("</neverUnderBody>\n");

            string body = sb.ToString();
            Log.Message("[JETuner]\n" + body);

            try
            {
                string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "JETuner.txt");
                File.WriteAllText(path, body);
                Messages.Message("JETuner \u5df2\u5199\u5165 " + path, MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception e)
            {
                Log.Error("[JETuner] could not write the dump file: " + e);
                Messages.Message("JETuner \u5199\u6587\u4ef6\u5931\u8d25, \u770b Player.log \u91cc\u7684 [JETuner]",
                    MessageTypeDefOf.NegativeEvent, false);
            }
        }
    }
}
