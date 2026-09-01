# CE 适配铁律(HSK 整合)

> 来源: AGENTS.md §5.5。前提: 本环境 CE 是 **CombatExtended.HSK.dll**(HSK 兼容层),判定与原版 CE 不同。

## 0. 判定差异(根因)
- 原版 `Tool` 类会被 CE 的 `GetThingDefTools` 过滤 → 近战武器 info 卡报 "which has no support for Combat Extended"。
- 护甲值: 原版百分比在 CE 下不适用,须用 **CE mm 值**。
- 参照物: `RatkinRaceHSK/1.6/Patches/HSK_Generated/` + 本 mod `Patches/04_CE适配.xml`(66 Replace + 10 Add)。

## 1. 近战武器(每把必须)
1. `tools` 全部 `<li Class="CombatExtended.ToolCE">`,字段:
   - `label` / `capacities`(Stab/Cut/Blunt) / `power` / `cooldownTime`
   - **`armorPenetrationSharp`(锐甲) + `armorPenetrationBlunt`(钝甲)**,单位 = mm 装甲(基准穿透就是这两个值,材质/品质再乘系数)
   - `linkedBodyPartsGroup`(Point/Edge/Handle/Head)
   - 可选 `chanceFactor`
2. **CE stat 直接写 ThingDef**(补丁 Add 会重复节点报错): `statBases` 加 `<Bulk>`(体积)+ `<MeleeCounterParryBonus>`;`equippedStatOffsets` 加 `<MeleeCritChance>`/`<MeleeParryChance>`/`<Suppressability>`(负值)。
3. **单手/双手标记**(weaponTags):
   - 单手 = `CE_OneHandedWeapon` + `RK_WeaponTag_OneHand` + `RK_WeaponTag_OneHandMelee`
   - 双手 = `RK_WeaponTag_TwoHand` + `RK_WeaponTag_TwoHandMelee`(**HSK CE 无 `CE_TwoHandedWeapon` tag,双手不写 CE 标记**)
   - 战斗档位 tag(如 `MedievalMeleeAdvanced`)可共存;weaponTags 要 `Inherit="false"`。
4. 纯战斗武器**不加** PlantWorkSpeed 等工具效果(那是 RK_Fork 类工具武器才加)。
5. 整合方式不变: 继承 `BaseMeleeWeapon_Sharp_Quality` + `<recipeMaker Inherit="False" IsNull="True"/>` 关自动配方 + 外置 RecipeDef(研究/工作台/材料)。

## 2. 远程武器适配流程
1. **弹药选择表**(能映射就映射,不自造):
   - 步枪/机枪/狙击 → `AmmoSet_303British`
   - 手枪/SMG → `AmmoSet_9x19mmPara`
   - 霰弹 → `AmmoSet_12Gauge`
   - 反器材 → `AmmoSet_50BMG`
   - 榴弹 → `AmmoSet_40x46mmGrenade`
   - 电荷 → `AmmoSet_6x24mmCharged`
   - 自定义: 鼠族光储能弹 `AmmoSet_LightbulletA/B`(详见 `docs/06_弹药/HSK弹药清单.csv`);金鼠族电荷武器复用这两个弹种。
2. **武器补丁**(04_CE适配.xml, `PatchOperationReplace` verbs/li 整个节点):
   - `verbClass` → `CombatExtended.Verb_ShootCE`
   - `<Properties>`(VerbPropertiesCE): `recoilAmount`/`defaultProjectile`/`warmupTime`/`range`/`burstShotCount`/`ticksBetweenBurstShots`/`soundCast`/`soundCastTail`/`muzzleFlashScale`
   - `<AmmoUser>`: `magazineSize`(弹匣)/`reloadTime`/`ammoSet`
   - `<FireModes>`: `aiUseBurstMode`/`aiAimMode`
   - `statBases` **Add** `<Bulk>`(枪械体积,按档位 10-50)
3. 弹药配方沿用 CE 原版 AmmoSet 机制;自定义弹药若需制作参照 CE 原版弹药 RecipeDef(`CE_AutoEnableCrafting` 自动启用)。

## 3. 装备(衣物/护甲)适配
1. **CE 负重(两处)**: `statBases` Add `<Bulk>`(物品体积)+ `<WornBulk>`(穿着体积);`equippedStatOffsets` Add `<CarryBulk>` + `<CarryWeight>`。
2. **护甲值**: `PatchOperationReplace` `statBases/ArmorRating_Sharp` 与 `statBases/ArmorRating_Blunt` → **CE mm 值**(参照同类护甲: 普通军服 5/4、重甲按档位上调)。
3. 防具类 CE 无需额外判定字段;盾牌/盾甲还要看相关补丁与 CE CarryBulk 惯例。

## 4. 反编译定位法
- dnfile+dncil 读 `Mods/CombatExtended/Assemblies/CombatExtended.dll`: `StatWorker_MeleeDamageAverage.GetValueUnfinalized` → `GetThingDefTools`(判定链 `tools.NullOrEmpty()` → 返回 0;`tools.Any(谓词)` → 空则报错);ToolCE 过滤条件在 `GetThingDefTools` 调用的工具收集方法里(isinst Tool / DamageDef 映射)。
