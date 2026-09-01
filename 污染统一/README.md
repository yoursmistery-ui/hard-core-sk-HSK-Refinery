# 污染统一 · RimWorld 独立整合 mod

一个把 **原版〔Biotech〕/** **HSK〔Core_SK〕/** **Rimatomics〔核能〕/** **Rimefeller〔石油化工〕** 的污染、放射、废料、防护机制做"最小侵入、可回滚"统一的独立 mod。

> 高优先级约定：不改动任何被整合 mod 的本体文件；所有改动都通过本 mod 的 `Patches/*.xml` 以 `PatchOperation` 运行时注入，可随时通过禁用本 mod 完整回滚。

---

## 阶段总览

| 阶段 | 内容 | 状态 |
|---|---|---|
| **骨架** | About.xml + 目录结构，可加载的独立 mod | ✅ 完成 |
| **阶段 A** | 废料物品互通：`NuclearWaste → Wastepack` 转换配方，接入原版废料分解器/污树处理链 | ✅ 完成 |
| **石化废物** | 「沥青」：裂化装置运行自动副产 → 直接作为沥青材料使用；投入原版电焚化炉焚烧回收为混凝土 | ✅ 完成 |
| **阶段 B** | 防护统一：原版防毒面具 / HSK 呼吸面罩补 `Radiation` 抗性，与化学防护服/防辐射服共用同一抗性栈 | ✅ 完成 |
| **阶段 C** | 地面放射污染落地为原版 pollution overlay | ⛔ 暂缓（侵入性过强，需改写式 C# 事件，留待后续评估） |

---

## 目录说明

```
污染统一/
├─ About/About.xml                一体模组元数据 + loadAfter 顺序
├─ Defs/
│  ├─ ThingCategoryDefs/          沥青专属"油化残留物"分类
│  ├─ ThingDefs/
│  │  └─ Items_Asphalt.xml        沥青材料（ThingDef: Asphalt，沥青块外观）
│  └─ RecipeDefs/
│     ├─ Recipes_NuclearInterop.xml  NuclearWaste → Wastepack 转换
│     └─ Recipes_Asphalt.xml      焚烧沥青制混凝土（10 沥青 → 3 混凝土）
├─ Patches/
│  ├─ Patch_AsphaltProducer.xml   给 CrudeCracker 挂沥青副产组件
│  ├─ Patch_AsphaltIncinerate.xml 给原版电焚化炉追加沥青焚烧配方
│  └─ Patch_B_ProtectionUnify.xml 防护互通（毒抗装备补辐射抗）
├─ Source/AsphaltProducerComp.cs  沥青副产 ThingComp 源码
├─ Assemblies/PollutionUnify.dll  编译产物
└─ build.ps1                      编译脚本（csc，参游戏程序集）
```

---

## 玩法 / 机制

### 沥青（石化废物）
- **来源**：`CrudeCracker` 裂化装置通电运行期，每约 1.5 天在设备旁吐 5 个「沥青」块（受电 = 工作中）。
- **性质**：美 −20 / 清洁 −1 / 可燃，起堆是可搬运的资源，不可交易；外观为深灰沥青块，可直接作为沥青材料使用。
- **回收**：不设独立焚化建筑，直接用**原版电焚化炉**（ElectricCrematorium，需「电力」研究）的「焚烧沥青制混凝土」配方：10 沥青 → 3 混凝土（`ConcreteResource`），使石化废物回收进 HSK 建材链。

### 阶段 A · 废料互通
在 Rimatomics 加工台可制作「核废料转换为废料包」：1 `NuclearWaste` → 2 `Wastepack`，使核废料进入原版 `WastepackAtomizer`/Polux 树的统一消纳环。
（注：原版分解器是单物品代码，只能吃 `Wastepack`，故用转换配方而非直接喂给分解器。）

### 阶段 B · 防护统一
- HSK `Apparel_HazardHelmet/Vest`：`ToxicEnvironmentResistance 0.4` + `Radiation −0.4`（原版已带）。
- Rimatomics `Apparel_RadiationSuit/Mask`：`ToxicEnvironmentResistance` + `Radiation −0.35`（原版已带）。
- 本 mod 补：原版 `Apparel_GasMask`（0.8 毒）与 HSK `Apparel_Respirator`（0.6 毒）**补上 `Radiation −0.35`**，使"防毒装"也能防 HSK/Rimatomics 放射 → 任意环境危害可被同一抗性栈抵御。

---

## 编译（若需改动 C#）

```powershell
powershell -ExecutionPolicy Bypass -File "污染统一\build.ps1"
```

产物输出为 `Assemblies\PollutionUnify.dll`（ASCII 文件名，规避 PowerShell 中文编码坑）。

---

## 已知边界
- **阶段 C 未实现**：反应堆泄漏/铀矿放射性"地面"仍是各家自有叠加图层，未转换为原版污染 overlay。若实装需在反应堆泄漏 comp 上做拦截式改写，侵入面大、易冲突，故本版不做。
- 沥青不主动释放毒气（若要接入地面污染需另做产污组件）。