// 特性拓展modHSK —— 信件文案层(v5 · 手写 · 动作清单法)
// 一封信 = 标题(动词+的+名词) + 正文(先…然后…接着… 三动作三物件) + 习惯定型句 + 收束「{PAWN}现在是{TRAIT}。」
//   · 标题/正文按成因维度(规则表 Key / "技能提炼")取对位变体, 每槽 2 套, 同一人不连拿同一套;
//   · 习惯句只给本包特性(HSK_*)手写, 其余走通用层;
//   · 文案直接写在代码里(中文), 缺槽回退通用句, 绝不露裸 key。
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    public static class HSKLetterLite
    {
        // ---- 标题(每槽 2 套) ----
        private static readonly Dictionary<string, string[]> Heads = BuildDict(new string[][]
        {
            new string[]{ "作息-夜行", "熬出来的夜班", "习惯了的凉夜" },
            new string[]{ "作息-晨行", "赶在露水前的活", "天不亮就开工的人" },
            new string[]{ "温度-耐热", "炉边烤出的耐性", "大太阳底下钉着的人" },
            new string[]{ "温度-耐寒", "冻出来的利索", "砸冰开工的手" },
            new string[]{ "田间劳作", "种出来的手感", "泥地里泡熟的经验" },
            new string[]{ "修造不休", "敲出来的章法", "舍不得放下的锤子" },
            new string[]{ "医者仁心", "缝合缝出来的稳", "换药换出来的耐心" },
            new string[]{ "烹调讲究", "尝出来的讲究", "锅边守出来的分寸" },
            new string[]{ "买卖周旋", "谈出来的门道", "秤上抹不掉的人情" },
            new string[]{ "驯养默契", "喂出来的默契", "一把草料换来的信任" },
            new string[]{ "嗜血", "打出来的胆气", "刀口上舔出的狠劲" },
            new string[]{ "赢出来的算路", "赢出来的算路", "牌桌上磨出来的脑子" },
            new string[]{ "弦不离手", "弹出来的调子", "走路都踩拍子的人" },
            new string[]{ "心情高涨", "攒出来的好脾气", "一整天都顺心的日子" },
            new string[]{ "压力崩溃", "压垮之后的疲惫", "攥不住的日子" },
            new string[]{ "屠戮伴侣", "卸不下的愧疚", "说不出口的那一天" },
            new string[]{ "饮酒", "杯里泡出来的瘾", "离不开的那一口" },
            new string[]{ "吸烟", "烟叶熏出来的习惯", "手指间的那个动作" },
            new string[]{ "药物滥用", "药瓶喂出来的依赖", "藏在枕头下的瓶子" },
            new string[]{ "技能提炼", "练出来的本色", "手上长出来的性子" }
        });

        // ---- 正文(先…然后…接着…, 每槽 2 套, 与标题同槽成对) ----
        private static readonly Dictionary<string, string[]> Scenes = BuildDict(new string[][]
        {
            new string[]{ "作息-夜行",
                "{PAWN_nameDef}把手电别在腰上，先借着凉快把料备齐，然后一个人把夜里的活干完，接着赶在天亮前把工具一件件归了位。",
                "{PAWN_nameDef}接过别人睡前的最后一盏灯，先把火压小，然后守着夜里的炉温添了两回料，接着趁天没亮又把账清了一遍。" },
            new string[]{ "作息-晨行",
                "{PAWN_nameDef}摸黑穿上外套，先把头天的工具磨了一遍，然后趁露水没干把苗床理完，接着在别人起床前烧好了一壶水。",
                "{PAWN_nameDef}先把门闩拉开透气，然后把夜里落下的活列成单子，接着挑最要紧的一件干到了日头升起。" },
            new string[]{ "温度-耐热",
                "{PAWN_nameDef}把水囊挂在工棚的钉子上，先守着炉口翻了一遍料，然后用袖子抹了把汗接着干，直到换班的人来才肯挪地方。",
                "{PAWN_nameDef}先在阴凉里灌了半囊水，然后顶着日头把晒烫的料搬完，接着蹲在风口把工具一件件擦干净才回屋。" },
            new string[]{ "温度-耐寒",
                "{PAWN_nameDef}把手套夹在腋下，先跺掉靴子上的冰碴，然后呵着白气把冻硬的活干完，接着把冻裂的口子裹上布条。",
                "{PAWN_nameDef}先敲掉工具上的霜，然后呵着手接着刨料，最后把冻硬的料搬进屋码好才肯去烤火。" },
            new string[]{ "田间劳作",
                "{PAWN_nameDef}把种子分进粗布袋，先弯腰补齐了缺苗的垄，然后顺手拔掉两把草，接着蹲在田埂上把墒情记在心里。",
                "{PAWN_nameDef}先磨快了小铲，然后逐垄间苗补栽，接着把摘下的残叶拢成一堆踩进泥里当肥。" },
            new string[]{ "修造不休",
                "{PAWN_nameDef}把钉子咬在嘴里，先对齐了框架的边角，然后一锤一锤把板面敲平，接着蹲下来用尺子量了三遍才收工。",
                "{PAWN_nameDef}先把散料按长短码开，然后挑出翘曲的换掉，接着把新料一根根敲进框里，直敲到声音发实。" },
            new string[]{ "医者仁心",
                "{PAWN_nameDef}先把镊子在灯下照了照，然后按住伤口清创上药，接着缠绷带时留出了活动的余地，才放病人去歇着。",
                "{PAWN_nameDef}先烧了一锅水，然后把器械一件件摆好，接着一边按着病人一边把创口处理干净，末了连床单也顺手换了。" },
            new string[]{ "烹调讲究",
                "{PAWN_nameDef}先把案板刮净，然后按人的口味把香料减了半勺，接着守在锅边撇了两回沫，盛出前又尝了一小口。",
                "{PAWN_nameDef}挑出最鲜的那块肉，先大火封了边，然后转小火煨上，接着掐着时辰下菜，起锅前撒了一把碎香草。" },
            new string[]{ "买卖周旋",
                "{PAWN_nameDef}先把货分出三六九等，然后当着客人的面校准了秤，接着让掉零头却把大价咬住，末了把下一次的生意也约好了。",
                "{PAWN_nameDef}先请客人看了货样，然后不紧不慢地报出实价，接着对方砍价时只让了一步，反倒又搭上一包种子成交。" },
            new string[]{ "驯养默契",
                "{PAWN_nameDef}把手摊平举稳，先用草料把动物引到跟前，然后照着它的性子顺了顺毛，接着试了两次才给它戴上笼头。",
                "{PAWN_nameDef}先蹲下不出声，然后每天固定时辰送食来，接着等它主动蹭过来那天，才轻轻摸了第一把。" },
            new string[]{ "嗜血",
                "{PAWN_nameDef}把刀在靴筒上蹭了蹭，先抵着盾牌顶了上去，然后侧身躲过一击把对方撂倒，接着抖抖肩又去追下一个。",
                "{PAWN_nameDef}先盯着对面的眼睛不挪开，然后抢在对方换弹的空当扑了上去，接着收拾完还把战场绕了一圈。" },
            new string[]{ "赢出来的算路",
                "{PAWN_nameDef}把筹码码成两摞，先输一手试对方的路数，然后故意漏个破绽引人下注，接着一把把桌面扫了过来。",
                "{PAWN_nameDef}先盯着棋盘看了半晌，然后舍掉一子换开局，接着每一步都掐着对方的钟点落子。" },
            new string[]{ "弦不离手",
                "{PAWN_nameDef}先把弦调准，然后照着记忆里的调子弹了两遍，接着顺手改了两个音，弹到别人探头来听才停。",
                "{PAWN_nameDef}吃完饭先擦了琴，然后给围过来的孩子们起了个头，接着一句一句教下去，直到炊事班来催。" },
            new string[]{ "心情高涨",
                "{PAWN_nameDef}先吹着口哨把活干完，然后顺手帮邻居把柴码好，接着晚饭多讲了两件趣事，睡前连靴子都摆得整整齐齐。",
                "{PAWN_nameDef}先给自己倒了杯热饮，然后把攒着的三件小事一口气办完，接着在廊下晒着太阳打了个盹。" },
            new string[]{ "压力崩溃",
                "{PAWN_nameDef}把没做完的活推开，先蹲在墙角发了很久的呆，然后被人扶去床上，接着连饭都是别人端来的。",
                "{PAWN_nameDef}先是站在原地不动，然后对着工具箱红了眼，接着被人领去帐里睡下，第二天也没提这件事。" },
            new string[]{ "屠戮伴侣",
                "{PAWN_nameDef}把刀擦了很久，先默默把毛孩子埋在栏后，然后那几天总绕开那条路走，接着喂食时对着空槽站了好一会。",
                "{PAWN_nameDef}先收拾好案台，然后手比脑子快地做完了那件事，接着那天晚上把门闩多插了一道。" },
            new string[]{ "饮酒",
                "{PAWN_nameDef}先把杯底喝干，然后自己又满上一杯，接着别人劝的时候摆摆手，转身又斟了第三回。",
                "{PAWN_nameDef}先灌了一大口，然后对着空罐子出神，接着半夜爬起来摸黑又倒了半杯。" },
            new string[]{ "吸烟",
                "{PAWN_nameDef}先把烟叶卷好，然后靠着门框慢慢抽完，接着弹了弹灰又点上第二支。",
                "{PAWN_nameDef}先深吸一口，然后把烟递给别人又收回来，接着连着抽完才肯进屋干活。" },
            new string[]{ "药物滥用",
                "{PAWN_nameDef}先四下看了看，然后把药片就着水咽下去，接着把瓶子塞回枕头底下，装作什么都没发生。",
                "{PAWN_nameDef}先说只来这一次，然后比上次多用了半支，接着第二天一早又去找那只瓶子。" },
            new string[]{ "技能提炼",
                "{PAWN_nameDef}先把最拿手的那套活从头做了一遍，然后在关键的地方放慢了手，接着收尾时又多检查了一遍才交出去。",
                "{PAWN_nameDef}先照老规矩把家什摆好，然后顺手改了两个不顺手的小步骤，接着干完还把新做法记在了心里。" }
        });

        // ---- 习惯定型句(本包特性手写, 其余走通用层) ----
        private static readonly Dictionary<string, string> Habits = BuildSingle(new string[]
        {
            "HSK_NightOwl", "{PAWN_nameDef}如今一到夜里就来精神，白天怎么叫都叫不太醒。",
            "HSK_EarlyRiser", "天不亮{PAWN_nameDef}就醒了，赖床这件事跟{PAWN_objective}彻底无关。",
            "HSK_Tactician", "打牌下棋{PAWN_nameDef}都要算三步，输了还非要求复盘。",
            "HSK_Musician", "手边没有琴{PAWN_nameDef}也要哼两句，连走路都踩着拍子。",
            "HSK_Bookworm", "{PAWN_nameDef}兜里总揣着一本没读完的书，等人时也要翻两页。",
            "HSK_HealthNut", "{PAWN_nameDef}每顿都把菜和肉分得清清楚楚，别人劝酒一律摆手。",
            "HSK_Glutton", "开饭的铃还没响，{PAWN_nameDef}已经先在锅边转了两圈。",
            "HSK_GymRat", "换班的间隙{PAWN_nameDef}也要比划两下，杠铃放回去的位置从不差。",
            "HSK_Meditator", "再吵的地方{PAWN_nameDef}也能坐下闭眼缓一缓，起来接着干。"
        });
        private static readonly string[] GenericHabits = new string[]
        {
            "{PAWN_nameDef}做事的先后顺序还是老样子，一步都省不掉。",
            "这个习惯{PAWN_nameDef}是改不掉了，别人提起来{PAWN_pronoun}也只是笑笑。",
            "{PAWN_nameDef}自己倒是坦然，该怎么做还是怎么做。"
        };

        private const string Closer = "{PAWN_labelShort}现在是{TRAIT}。";
        private const string NL = "\n\n";
        private const string NL1 = "\n";

        // ---- 发信入口 ----
        // slug: 成因维度(规则表 Key 或 "技能提炼"); negative: 负向维度用负面信封
        public static void Send(Pawn pawn, TraitDef td, string slug, bool negative, int count)
        {
            try
            {
                HSKTraitLedger led = HSKLedger.Game;
                HSKPawnEntry e = (led == null) ? null : led.PeekEntry(pawn);
                List<NamedArgument> args = new List<NamedArgument>();
                args.Add(pawn.Named("PAWN"));
                args.Add(HSKGrowth.Highlight(pawn, td).Named("TRAIT"));
                args.Add(count.Named("COUNT"));

                string[] heads, scenes;
                Heads.TryGetValue(slug, out heads);
                Scenes.TryGetValue(slug, out scenes);
                int n = (heads != null && scenes != null) ? Math.Min(heads.Length, scenes.Length) : 0;
                int v = 0;
                if (n > 0) v = PickVar(e, slug, n);
                string title = (n > 0) ? heads[v] : "性格沉淀";
                string scene = (n > 0) ? scenes[v] : "{PAWN_nameDef}长年累月做着熟悉的事，做事的路数慢慢定了型。";

                string habit;
                if (!Habits.TryGetValue(td != null ? td.defName : null, out habit) || habit == null)
                    habit = GenericHabits[Rand.Range(0, GenericHabits.Length)];

                string body = scene.Translate(args.ToArray()) + NL
                    + habit.Translate(args.ToArray()) + NL1
                    + Closer.Translate(args.ToArray());
                string titleT = title.Translate(args.ToArray());

                if (Find.LetterStack != null)
                    Find.LetterStack.ReceiveLetter(titleT, body,
                        negative ? LetterDefOf.NegativeEvent : LetterDefOf.PositiveEvent, new LookTargets(pawn));
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] 信件组装失败: " + ex); }
        }

        // 取变体号: 避开该小人上一次用过的编号
        private static int PickVar(HSKPawnEntry e, string slot, int count)
        {
            if (count <= 1) return 0;
            int last = -1;
            if (e != null && e.lastVar != null) e.lastVar.TryGetValue(slot, out last);
            int v = Rand.Range(0, count);
            if (v == last) v = (v + 1) % count;
            if (e != null)
            {
                if (e.lastVar == null) e.lastVar = new Dictionary<string, int>();
                e.lastVar[slot] = v;
            }
            return v;
        }

        private static Dictionary<string, string[]> BuildDict(string[][] rows)
        {
            Dictionary<string, string[]> d = new Dictionary<string, string[]>();
            if (rows == null) return d;
            foreach (string[] r in rows)
                if (r != null && r.Length >= 3) d[r[0]] = new string[] { r[1], r[2] };
            return d;
        }

        private static Dictionary<string, string> BuildSingle(string[] rows)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            if (rows == null) return d;
            for (int i = 0; i + 1 < rows.Length; i += 2) d[rows[i]] = rows[i + 1];
            return d;
        }
    }
}
