# 中世纪MO整合进度盘点 · 逐单元明细(板块 9-13)

> 主文件: `中世纪MO整合进度盘点.md`(结论 / 板块进度 / 已落地硬证据 / 问题处置 / 下一步)。数据与主文件同源,由 `_tmp/gen_mo_progress.py` 生成、`_tmp/split_mo_docs.py` 按 10KB 铁律拆分。
## 逐单元明细(板块 9-13)



### 9 酒水/药物/炼金 — 单元 3,加权 0.0%(已整 0 / 部分 0 / 未整 3 / 不整 0)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| 酒水(艾尔/苹果酒/蜂蜜酒/葡萄酒 + 品质熟成) | 4+ 链 | ⬜ 未整合 | 未落地;HSK 已有 Alcohol/Brewing 链 | MO 覆盖原版 Brewing → 须定主链 | 中 |
| 两段式炼金药物(止痛剂/活力丸/知识药水/奇迹/星尘/狂喜粉/卢希克斯/战斗灵药等 9 种) | 9 药 | ⬜ 未整合 | 未落地;坩埚 + 草药萃取 + 怪物资材 | CompUseEffect_KnowledgePotion 在 MO DLL | 高 |
| 中毒累积系统 DankPyon_Intoxication(慢性 hediff,4 档) | 1 机制 | ⬜ 未整合 | 未落地;与 HSK 硬核方向天然契合,可单拎自研 | 需自研 hediff 累积/衰减 | 中 |

### 10 书籍与研究 — 单元 5,加权 20.0%(已整 1 / 部分 0 / 未整 3 / 不整 1)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| 图纸=研究门禁(14 项中高端研究被书锁定) | 14 门禁 | ✅ 已整合 | ✅ 自研蓝图门控体系 v4.1: 112 本蓝图书(中世纪图纸书→超凡 4 卷)+ BlueprintUnlockHSK.dll(CanStartNow 门禁/研读工作/ResearchTreeSK 角标 x/N)+ 事件/商人/贮藏/GoExplore/Cybranian 五路投放 | 不引 MO DLL;早期 SchematicGateHSK 复刻已退役归档 | — |
| 传说书 Tale(娱乐书) | 1 类 | ⬜ 未整合 | 未落地;原版 Book 可零 DLL 移植 | 注意与蓝图书研读抢智力经验上限 | 低 |
| 专著 Treatise(12 技能训练书,抄写台自制) | 12 书 | ⬜ 未整合 | 未落地;需 Paper 体系(本环境无纸)+ 抄写台 | BookWithAuthor/RecipeWorker_MakeSkillBook 在 MO DLL;书籍拓展HSK(Vanilla Books Expanded)已有同类技能书线,先评估是否够用 | 中 |
| 独特书(华丽古卷 / OnEnglish) | 2 书 | ⬜ 未整合 | 未落地;战利品专属 | 随战利品体系 | 低 |
| MO 研究树(自定义 tab + 19 个原版研究改写) | 19 节点 | ⛔ 不整合/放弃 | ⛔ 已由 HSK 科技树(489 节点)+ ResearchTreeSK 重排取代 | Change_ResearchProjectDef 83 Operation 与 HSK 冲突面最大 | — |

### 11 心情·特性·Hediff·文化 — 单元 6,加权 0.0%(已整 0 / 部分 0 / 未整 5 / 不整 1)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| 品质心情(吃奶酪/喝葡萄酒 7 档) | 7 thought | ⬜ 未整合 | 未落地;随 §9 酒水/食材 | 随 §9 决策 | 低 |
| 食物记忆(牛排+15 ~ 泔水饭-5) | 10 thought | ⬜ 未整合 | 未落地;随 §1/§9 | 随 §9 决策 | 低 |
| 特性(战士 / 老兵) | 2 trait | ⬜ 未整合 | 未落地;纯 XML | 需并入 HSK 特性池(特性拓展modHSK) | 低 |
| Hediff(林龙酸/眩晕/Alp睡眠/免疫授予/食物增益三档) | ≈12 hediff | ⬜ 未整合 | 未落地 | HediffComp_* 全在 MO DLL | 高 |
| 文化风格(老世界人 / 沙漠) | 2 culture | ⛔ 不整合/放弃 | 未落地;无新 memes/precepts,价值低 | 与服饰分区 tag 体系需对接 | 低 |
| 新伤害类型(弩炮弹/巨石/焦油/酸灼/整吞/眩晕/睡眠) | ≈9 | ⬜ 未整合 | 未落地;随武器/怪物 | 须与 CE 伤害体系核对 | 中 |

### 12/13 机制与兼容 — 单元 3,加权 16.7%(已整 0 / 部分 1 / 未整 0 / 不整 2)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| MedievalOverhaul.dll(64 Harmony 补丁 + 一堆 Comp/GameComponent) | 1 dll | ⛔ 不整合/放弃 | ⛔ 策略性不引入;需要哪个机制就自研等价物(蓝图门控→BlueprintUnlockHSK) | 这是全表难度普遍偏高的根因 | — |
| MO 兼容子模块 25 项(DBH卫浴/GiddyUp/VFE Medieval2/Vanilla 系列等) | 25 子包 | ⛔ 不整合/放弃 | ⛔ 不搬 MO 主体即无适配对象;另 酒馆工具整合HSK/1.6/Patches/Compat_MedievalOverhaul.xml 仍以 MO 为可选前置(注意别与整合内容重复) | — | — |
| 汉化(简中 DefInjected + Keyed) | — | 🟡 部分 | 🟡 已移植内容 100% 内联中文 label/description(Buildings_Rustic_* / Incidents_EventPack / TraderKinds);蓝图书另出 English DefInjected + Keys.xml | MO 本体 Keyed 缺失与本环境无关;后续每整合一块须按 §4 补中文 | — |

### 16 矿井/采掘 — 单元 2,加权 0.0%(已整 0 / 部分 0 / 未整 2 / 不整 0)

| 内容单元 | MO 规模 | 状态 | 证据 / 落地位置 | 卡点 · 依赖 | 难度 |
|---|---|---|---|---|---|
| 矿井体系(矿井 3×3 + 挖掘点 + 挖土点 + 采掘配方) | 3 建筑 + 12 配方 | ⬜ 未整合 | ⬜ 方案已定(产物 6 种: 煤/磁铁矿/盐/软粘土/铜矿/6 宝石;移除火药焦油金银铁矿),实现未落地 | 须换挂 HSK Mining 链 + 研究台六档门槛 + WorkGiver | 中 |
| 矿井宝石暴击 / 虫灾 | 2 机制 | ⬜ 未整合 | 未落地;RecipeExtension_Mine + CompCreatesInfestations_Mine 在 MO DLL | 须自研等价(RandChance 暴击可在 DLL 内做) | 中—高 |

