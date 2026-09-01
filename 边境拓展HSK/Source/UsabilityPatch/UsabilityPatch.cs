using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BordersOfTheRimHskPatch
{
    // 边境拓展HSK 本地适配补丁(2026-08-31):
    // 1) 宜居放宽——用户规则"周边没有山就是宜居": 有效格 = 非水域 且 非不可通行(hilliness==Impassable)。
    //    BOTR 原判定额外要求 群系 canBuildBase/implemented 且 群系权重×派系温度曲线 > 0,
    //    在群系破碎/温度极端的星球上会让首都安置探测永远达不到目标, 整块 BFS 反复扫描(实测 capitals 一步 65.2s)。
    // 2) 家园散布半径钉值——用户不想手动调滑条: 把 homelandRadius 钉死为 48, 并从设置界面删除该滑条。
    //    该值是各派系定居点场半径的下限(实际半径 = Max(钉值, ceil(√(定居点数-1)×5.5)), 钳制 12–240),
    //    48 在 77 派系/35 首都的密集星球上保持家园紧凑、留出空隙, 大派系仍可按密度自然扩张。
    [StaticConstructorOnStartup]
    public static class UsabilityPatch
    {
        private const int PinnedHomelandRadius = 48;
        private const string ModTypeName = "BordersOfTheRim.BordersOfTheRimMod";
        private const string SettingsTypeName = "BordersOfTheRim.BordersOfTheRimSettings";
        private const int SwitchSentinel = -9999;

        private static readonly Dictionary<int, int> OperandSizes = BuildOperandSizes();

        static UsabilityPatch()
        {
            try
            {
                PatchUsability();
            }
            catch (Exception e)
            {
                Log.Error("[BOTR宜居放宽] 宜居补丁应用失败: " + e);
            }
            try
            {
                PatchHomelandRadius();
            }
            catch (Exception e)
            {
                Log.Error("[BOTR宜居放宽] 家园散布钉值补丁应用失败: " + e);
            }
        }

        private static void PatchUsability()
        {
            var harmony = new Harmony("local.ratkin.botr.usability");
            var type = AccessTools.TypeByName("BordersOfTheRim.WorldGenStep_ClusterFactionSettlements");
            if (type == null)
            {
                Log.Warning("[BOTR宜居放宽] 未找到 BordersOfTheRim.WorldGenStep_ClusterFactionSettlements, 补丁未应用。");
                return;
            }
            // 三个重载里只挂 6 参实现, 4/5 参重载内部都会调用它(重载歧义铁律: 显式给参数类型)
            var canPlace = AccessTools.Method(type, "CanPlaceSettlement", new[]
            {
                typeof(PlanetLayer), typeof(PlanetTile), typeof(HashSet<int>),
                typeof(HashSet<int>), typeof(List<PlanetTile>), typeof(bool)
            });
            var suitability = AccessTools.Method(type, "SettlementSuitability", new[]
            {
                typeof(PlanetTile), typeof(Faction)
            });
            if (canPlace == null || suitability == null)
            {
                Log.Warning("[BOTR宜居放宽] 未找到 CanPlaceSettlement/SettlementSuitability 目标方法, 补丁未应用。");
                return;
            }
            harmony.Patch(canPlace, prefix: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(CanPlaceSettlement_Prefix))));
            harmony.Patch(suitability, prefix: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(SettlementSuitability_Prefix))));
            Log.Message("[BOTR宜居放宽] 已应用: 宜居 = 非水域 且 非不可通行, 不再检查群系 canBuildBase/implemented 与温度曲线。");
        }

        private static void PatchHomelandRadius()
        {
            var harmony = new Harmony("local.ratkin.botr.homelandradius");
            var settingsType = AccessTools.TypeByName(SettingsTypeName);
            var modType = AccessTools.TypeByName(ModTypeName);
            if (settingsType == null || modType == null)
            {
                Log.Warning("[BOTR宜居放宽] 未找到边境拓展设置类型, 钉值补丁未应用。");
                return;
            }

            PinHomelandRadius();

            // 设置文件每次载入都会走 ExposeData, 之后再钉一次, 保证任何读取路径都是钉值
            var expose = AccessTools.Method(settingsType, "ExposeData");
            if (expose != null)
            {
                harmony.Patch(expose, postfix: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(ExposeData_Postfix))));
            }

            // 滑条行在世界标签页的编译器生成委托里, 按"方法体含 ClusterRadiusTip 字符串"定位, 不依赖易变的生成名
            var sliderMethod = FindMethodContainingLdstr(settingsType.Assembly, "BOTR_Settings_ClusterRadiusTip");
            bool paramOk = sliderMethod != null && Array.Exists(sliderMethod.GetParameters(), p => p.ParameterType == typeof(Listing_Standard));
            if (paramOk)
            {
                harmony.Patch(sliderMethod, transpiler: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(RemoveClusterRadiusSlider))));
            }
            else
            {
                Log.Warning("[BOTR宜居放宽] 未找到含家园散布滑条的设置绘制委托, 滑条未移除(钉值仍生效)。");
            }
        }

        private static void PinHomelandRadius()
        {
            try
            {
                var modType = AccessTools.TypeByName(ModTypeName);
                var prop = modType == null ? null : AccessTools.Property(modType, "Settings");
                var settings = prop == null ? null : prop.GetValue(null);
                var field = AccessTools.Field(AccessTools.TypeByName(SettingsTypeName), "homelandRadius");
                if (settings != null && field != null && field.FieldType == typeof(int) && (int)field.GetValue(settings) != PinnedHomelandRadius)
                {
                    field.SetValue(settings, PinnedHomelandRadius);
                    Log.Message("[BOTR宜居放宽] 家园散布半径已钉为 " + PinnedHomelandRadius + "。");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[BOTR宜居放宽] 家园散布钉值失败: " + e);
            }
        }

        private static void ExposeData_Postfix()
        {
            PinHomelandRadius();
        }

        // 从委托方法里删掉"家园散布半径"滑条整条语句。
        // 语句形如: Settings.homelandRadius = Mathf.RoundToInt(listing.SliderLabeled(Translate("BOTR_Settings_ClusterRadius", ...), ...));
        // IL 顺序: call get_Settings(存值目标) → ldloc listing(接收者) → ldstr ... → ... → stfld homelandRadius。
        // 任一结构校验不过就原样返回, 宁可不删也不产出坏 IL。
        private static IEnumerable<CodeInstruction> RemoveClusterRadiusSlider(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int ldstrIdx = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string s && s == "BOTR_Settings_ClusterRadius")
                {
                    ldstrIdx = i;
                    break;
                }
            }
            if (ldstrIdx < 2)
            {
                return codes;
            }
            var callIns = codes[ldstrIdx - 2];
            bool isSettingsGetter = (callIns.opcode == OpCodes.Call || callIns.opcode == OpCodes.Callvirt)
                && callIns.operand is MethodInfo getter && getter.Name == "get_Settings"
                && getter.DeclaringType != null && getter.DeclaringType.Name == "BordersOfTheRimMod";
            if (!isSettingsGetter || !IsLocalOrArgLoad(codes[ldstrIdx - 1].opcode))
            {
                return codes;
            }
            int end = -1;
            for (int i = ldstrIdx; i < codes.Count && i < ldstrIdx + 80; i++)
            {
                if (codes[i].opcode != OpCodes.Stfld && codes[i].opcode != OpCodes.Stsfld)
                {
                    continue;
                }
                var f = codes[i].operand as FieldInfo;
                if (f != null && f.Name == "homelandRadius" && f.DeclaringType != null && f.DeclaringType.Name == "BordersOfTheRimSettings")
                {
                    end = i;
                    break;
                }
            }
            if (end < 0)
            {
                return codes;
            }
            codes.RemoveRange(ldstrIdx - 2, end - (ldstrIdx - 2) + 1);
            Log.Message("[BOTR宜居放宽] 已从边境拓展设置界面移除\"家园散布\"滑条。");
            return codes;
        }

        private static bool IsLocalOrArgLoad(OpCode op)
        {
            return op == OpCodes.Ldloc || op == OpCodes.Ldloc_S
                || op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 || op == OpCodes.Ldloc_3
                || op == OpCodes.Ldarg || op == OpCodes.Ldarg_S
                || op == OpCodes.Ldarg_0 || op == OpCodes.Ldarg_1 || op == OpCodes.Ldarg_2 || op == OpCodes.Ldarg_3;
        }

        private static MethodInfo FindMethodContainingLdstr(Assembly asm, string target)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            foreach (var t in types)
            {
                if (t == null)
                {
                    continue;
                }
                MethodInfo[] methods;
                try
                {
                    methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }
                foreach (var m in methods)
                {
                    if (m.IsAbstract || m.ContainsGenericParameters)
                    {
                        continue;
                    }
                    MethodBody body;
                    try
                    {
                        body = m.GetMethodBody();
                    }
                    catch
                    {
                        continue;
                    }
                    if (body == null)
                    {
                        continue;
                    }
                    byte[] il = body.GetILAsByteArray();
                    if (il == null || il.Length == 0)
                    {
                        continue;
                    }
                    if (BodyContainsLdstr(il, m.Module, target))
                    {
                        return m;
                    }
                }
            }
            return null;
        }

        // 按指令边界走方法体, 只在真正的 ldstr 指令上解析字符串令牌(避免在操作数字节里误判)
        private static bool BodyContainsLdstr(byte[] il, Module module, string target)
        {
            int pos = 0;
            while (pos < il.Length)
            {
                int opValue;
                int opLen;
                if (il[pos] == 0xFE)
                {
                    if (pos + 1 >= il.Length)
                    {
                        return false;
                    }
                    opValue = 0xFE00 | il[pos + 1];
                    opLen = 2;
                }
                else
                {
                    opValue = il[pos];
                    opLen = 1;
                }
                int size;
                if (!OperandSizes.TryGetValue(opValue, out size))
                {
                    return false;
                }
                int operandStart = pos + opLen;
                if (size == SwitchSentinel)
                {
                    if (operandStart + 4 > il.Length)
                    {
                        return false;
                    }
                    int n = BitConverter.ToInt32(il, operandStart);
                    pos = operandStart + 4 + n * 4;
                    continue;
                }
                if (opValue == 0x72 && operandStart + 4 <= il.Length)
                {
                    int token = BitConverter.ToInt32(il, operandStart);
                    try
                    {
                        if (module.ResolveString(token) == target)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // 非法令牌, 忽略
                    }
                }
                pos = operandStart + size;
            }
            return false;
        }

        // ECMA-335 硬编码操作数尺寸表(实测 .NET 反射建表对双字节操作码不可靠, 一律硬编码; 键为完整操作码值)
        private static Dictionary<int, int> BuildOperandSizes()
        {
            return new Dictionary<int, int>
            {
                // ===== 单字节: 0 操作数 =====
                [0x00] = 0, [0x01] = 0, [0x02] = 0, [0x03] = 0, [0x04] = 0, [0x05] = 0, [0x06] = 0, [0x07] = 0,
                [0x08] = 0, [0x09] = 0, [0x0A] = 0, [0x0B] = 0, [0x0C] = 0, [0x0D] = 0, [0x14] = 0, [0x15] = 0,
                [0x16] = 0, [0x17] = 0, [0x18] = 0, [0x19] = 0, [0x1A] = 0, [0x1B] = 0, [0x1C] = 0, [0x1D] = 0,
                [0x1E] = 0, [0x25] = 0, [0x26] = 0, [0x2A] = 0,
                [0x46] = 0, [0x47] = 0, [0x48] = 0, [0x49] = 0, [0x4A] = 0, [0x4B] = 0, [0x4C] = 0, [0x4D] = 0,
                [0x4E] = 0, [0x4F] = 0, [0x50] = 0, [0x51] = 0, [0x52] = 0, [0x53] = 0, [0x54] = 0, [0x55] = 0,
                [0x56] = 0, [0x57] = 0, [0x58] = 0, [0x59] = 0, [0x5A] = 0, [0x5B] = 0, [0x5C] = 0, [0x5D] = 0,
                [0x5E] = 0, [0x5F] = 0, [0x60] = 0, [0x61] = 0, [0x62] = 0, [0x63] = 0, [0x64] = 0, [0x65] = 0,
                [0x66] = 0, [0x67] = 0, [0x68] = 0, [0x69] = 0, [0x6A] = 0, [0x6B] = 0, [0x6C] = 0, [0x6D] = 0,
                [0x6E] = 0, [0x76] = 0, [0x7A] = 0,
                [0x82] = 0, [0x83] = 0, [0x84] = 0, [0x85] = 0, [0x86] = 0, [0x87] = 0, [0x88] = 0, [0x89] = 0,
                [0x8A] = 0, [0x8B] = 0, [0x8E] = 0,
                [0x90] = 0, [0x91] = 0, [0x92] = 0, [0x93] = 0, [0x94] = 0, [0x95] = 0, [0x96] = 0, [0x97] = 0,
                [0x98] = 0, [0x99] = 0, [0x9A] = 0, [0x9B] = 0, [0x9C] = 0, [0x9D] = 0, [0x9E] = 0, [0x9F] = 0,
                [0xA0] = 0, [0xA1] = 0, [0xA2] = 0,
                [0xB3] = 0, [0xB4] = 0, [0xB5] = 0, [0xB6] = 0, [0xB7] = 0, [0xB8] = 0, [0xB9] = 0, [0xBA] = 0,
                [0xC3] = 0, [0xD1] = 0, [0xD2] = 0, [0xD3] = 0, [0xD4] = 0, [0xD5] = 0, [0xD6] = 0, [0xD7] = 0,
                [0xD8] = 0, [0xD9] = 0, [0xDA] = 0, [0xDB] = 0, [0xDC] = 0, [0xDF] = 0, [0xE0] = 0,
                // ===== 单字节: 1 操作数 =====
                [0x0E] = 1, [0x0F] = 1, [0x10] = 1, [0x11] = 1, [0x12] = 1, [0x13] = 1, [0x1F] = 1,
                [0x2B] = 1, [0x2C] = 1, [0x2D] = 1, [0x2E] = 1, [0x2F] = 1, [0x30] = 1, [0x31] = 1, [0x32] = 1,
                [0x33] = 1, [0x34] = 1, [0x35] = 1, [0x36] = 1, [0x37] = 1, [0xDE] = 1,
                // ===== 单字节: 4 操作数 =====
                [0x20] = 4, [0x22] = 4, [0x27] = 4, [0x28] = 4, [0x29] = 4,
                [0x38] = 4, [0x39] = 4, [0x3A] = 4, [0x3B] = 4, [0x3C] = 4, [0x3D] = 4, [0x3E] = 4, [0x3F] = 4,
                [0x40] = 4, [0x41] = 4, [0x42] = 4, [0x43] = 4, [0x44] = 4,
                [0x6F] = 4, [0x70] = 4, [0x71] = 4, [0x72] = 4, [0x73] = 4, [0x74] = 4, [0x75] = 4, [0x79] = 4,
                [0x7B] = 4, [0x7C] = 4, [0x7D] = 4, [0x7E] = 4, [0x7F] = 4, [0x80] = 4, [0x81] = 4,
                [0x8C] = 4, [0x8D] = 4, [0x8F] = 4, [0xA3] = 4, [0xA4] = 4, [0xA5] = 4,
                [0xC2] = 4, [0xC6] = 4, [0xD0] = 4, [0xDD] = 4,
                // ===== 单字节: 8 操作数 =====
                [0x21] = 8, [0x23] = 8,
                // ===== 单字节: switch =====
                [0x45] = SwitchSentinel,
                // ===== 双字节(0xFE 前缀) =====
                [0xFE00] = 0, [0xFE01] = 0, [0xFE02] = 0, [0xFE03] = 0, [0xFE04] = 0, [0xFE05] = 0,
                [0xFE06] = 4, [0xFE07] = 4, [0xFE09] = 2, [0xFE0A] = 2, [0xFE0B] = 2, [0xFE0C] = 2, [0xFE0D] = 2,
                [0xFE0E] = 2, [0xFE0F] = 0, [0xFE11] = 0, [0xFE12] = 1, [0xFE13] = 0, [0xFE14] = 0,
                [0xFE15] = 4, [0xFE16] = 4, [0xFE17] = 0, [0xFE18] = 0, [0xFE19] = 1, [0xFE1A] = 0,
                [0xFE1C] = 4, [0xFE1D] = 0, [0xFE1E] = 0,
            };
        }

        private static bool CanPlaceSettlement_Prefix(PlanetLayer layer, PlanetTile tile, HashSet<int> blockedTiles,
            HashSet<int> reservedTiles, List<PlanetTile> neighbors, bool requireSiblingBuffer, ref bool __result)
        {
            if (!tile.Valid || blockedTiles.Contains(tile.tileId) || reservedTiles.Contains(tile.tileId))
            {
                __result = false;
                return false;
            }
            var t = layer[tile];
            if (t == null || t.WaterCovered || (int)t.hilliness == (int)Hilliness.Impassable)
            {
                __result = false;
                return false;
            }
            neighbors.Clear();
            layer.GetTileNeighbors(tile, neighbors);
            if (requireSiblingBuffer)
            {
                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (reservedTiles.Contains(neighbors[i].tileId))
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            __result = true;
            return false;
        }

        private static bool SettlementSuitability_Prefix(PlanetTile tile, Faction faction, ref float __result)
        {
            var t = tile.Tile;
            bool usable = t != null && !t.WaterCovered && (int)t.hilliness != (int)Hilliness.Impassable;
            __result = usable ? 1f : 0f;
            return false;
        }
    }
}
