# 折跃搬运机HSK (local.blinkjumplifter)

AutoBlink 折跃框架 + JumpLifter 搬运机械体的**整合精简版**,面向 RimWorld 1.6 + Core SK(HSK)环境。

## 来源与整合说明

原打包(zip)含 3 个独立 mod,已做 HSK 适配并带简中:

| 原 mod | packageId | 取舍 |
|---|---|---|
| AutoBlink 折跃框架(工坊 id=3493889625) | rabiosus.autoblink | ✅ 并入(提供 AutoBlink.dll 与音效/UI) |
| AutoBlink Gene 基因扩展 | Noszbytouj.AutoBlinkGene | ❌ **整体剔除**(大折跃/小折跃/闪光基因) |
| JumpLifter 搬运机械体(工坊 id=3493717994) | rabiosus.jumplifter | ✅ 并入 |

剔除基因扩展不影响其余:机械体 comp(`AutoBlink.CompProperties_AutoBlink`)直接由 AutoBlink.dll 提供,与基因无耦合。

## 内容

- **搬运机械体 Mech_JumpLifter**:继承 Biotech `LightMechanoid` 基类,仅搬运职责,可在殖民地尺度连续折跃位移;近战极弱。
- **培育配方**:继承 `LightMechanoidRecipe`,研究门槛 `HighMechtech`(覆盖父级),材料槽 = SLDBar/USLDBar 100 + 零部件等(Biotech 体系,HSK 环境已校验存在)。
- **补丁**:机械培育舱(MechGestator)recipes 注册培育项;复活配方(ResurrectLightMech)可复活本机械体尸体。

## 结构(1.6-only,无 LoadFolders)

```
折跃搬运机HSK/
  About/            About.xml(依赖: Biotech + Core SK)+ Preview.png
  1.6/
    Assemblies/     AutoBlink.dll
    Defs/           SoundDefs + RecipeDefs + ThingDefs_Races
    Patches/        Patches_jumplifter.xml
    Languages/      English + ChineseSimplified (简体中文)
    Sounds/Textures 原 AutoBlink/Common 与 Jumplifter/Common 内容平铺
```

## 校验记录(2026-09-09)

- 全部 XML ET.parse 通过;补丁 xpath 目标(MechGestator/recipes、ResurrectLightMech/fixedIngredientFilter)在 Biotech 均存在。
- 引用存在性:stuff 类别 SLDBar/USLDBar、HighMechtech、各类 Biotech 材料、effecter/sound/job 全部命中。`Corpse_Mech_JumpLifter` 为引擎按机械体自动生成的尸体 def,无需预定义。
- DLL 不引用 HarmonyLib → About 未声明 Harmony 依赖(与原 rabiosus.autoblink 一致)。
- ModsConfig 注册于 `local.hskfixpack` 之前(第 220/240 位);`HSK修复整合` About loadAfter 已追加本 pid(现 83 成员,双目录同步)。
