using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(ProgressMod.Core), "ProgressMod", "1.12.1", "local")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]

namespace ProgressMod
{
    public class Core : MelonMod
    {
        public static readonly bool ForceFinish = true;   // 进度满: 拦截推进并直接完成
        public static readonly bool NoDurability = true;  // 不消耗耐久
        public static readonly int ModuleBoostMult = 10;  // 模块加成倍率
        public static readonly bool FreePower = true;     // 免电运转
        public static readonly bool MaxWineQuality = true; // 酒品质最高档

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("ProgressMod v1.12.1 init");
        }

        // ============ 机器进度强制满 ============
        // 游戏在存档结算时对每台进行中机器调用 ContinueProgressTypeMachine 推进进度。
        // 前缀: 详细日志 + 强制完成。
        [HarmonyPatch(typeof(MachineProgressHelper), "ContinueProgressTypeMachine")]
        public static class PatchContinue
        {
            public static bool Prefix(GameItem machine)
            {
                try
                {
                    bool isMachine = false;
                    try { isMachine = machine.IsTag("MACHINE_STATE_TAG") || machine.IsTag("PROGRESS_TYPE_MACHINE_TAG"); } catch { }
                    if (!isMachine) return true;
                    MelonLogger.Msg($"[Continue] machine={Describe(machine)}");
                    DumpTags(machine, "[Continue]");
                    string state = "?";
                    try { state = MachineProgressHelper.GetMachineState(machine); } catch (Exception e) { state = "EX:" + e.GetType().Name; }
                    MelonLogger.Msg($"[Continue] state={state}");

                    var curTag = machine.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                    int cur = curTag != null ? curTag.GetInt() : -1;
                    int tgt = tgtTag != null ? tgtTag.GetInt() : -1;
                    var spd = machine.GetTagReadonly("CURRENT_PROCESSING_SPEED_TAG");
                    int speed = spd != null ? spd.GetInt() : -1;
                    MelonLogger.Msg($"[Continue] state={state} progress=({cur}/{tgt}) speed={speed}");

                    var fin = false;
                    try { fin = MachineProgressHelper.IsProgressTypeMachineFinished(machine); } catch (Exception e) { MelonLogger.Error($"[Continue] IsFinished ex: {e}"); }
                    MelonLogger.Msg($"[Continue] finished={fin}");

                    if (ForceFinish && machine != null && tgtTag != null)
                    {
                        // 把当前进度直接设为目标 (ModifyTag 写回, GetTagReadonly 是只读副本)
                        Action<Il2Cpp.TagState> setCur = (ts) => { ts?.SetInt(tgt); };
                        machine.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", setCur);
                        MelonLogger.Msg($"[Continue] force CURRENT {cur} -> {tgt} (via ModifyTag)");
                        return true; // 让原逻辑继续: 推进后 current>=target 触发完成产出
                    }
                    return true;
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[Continue] ex: {e}");
                    return true;
                }
            }
        }

        // ============ 机器推进 hook (真推进点) ============
        // MachineryHelper.UpdateProcessingTypeMachine 是机器处理推进核心,
        // 结算时对进行中机器调用。前缀: 设满 CURRENT (若已存在) 并打日志 (只对真机器)。
        [HarmonyPatch(typeof(MachineryHelper), "UpdateProcessingTypeMachine")]
        public static class PatchUpdate
        {
            public static bool Prefix(GameItem __0)
            {
                try
                {
                    var machine = __0;
                    if (machine == null) return true;
                    // 只对真机器 (MACHINE_STATE_TAG 或 PROGRESS_TYPE_MACHINE_TAG 真实存在)
                    if (!(machine.IsTag("MACHINE_STATE_TAG") || machine.IsTag("PROGRESS_TYPE_MACHINE_TAG"))) return true;
                    MelonLogger.Msg($"[Update] machine={Describe(machine)}");
                    DumpTags(machine, "[Update]");
                    var curTag = machine.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                    int cur = curTag != null ? curTag.GetInt() : -1;
                    int tgt = tgtTag != null ? tgtTag.GetInt() : -1;
                    bool canOut = false;
                    try { canOut = MachineryHelper.CanMachineOutput(machine); } catch { }
                    string state = "?";
                    try { state = MachineProgressHelper.GetMachineState(machine); } catch (Exception e) { state = "EX:" + e.GetType().Name; }
                    MelonLogger.Msg($"[Update] state={state} progress=({cur}/{tgt}) canOutput={canOut}");

                    if (ForceFinish && machine != null && tgtTag != null && cur < tgt)
                    {
                        Action<Il2Cpp.TagState> setCur = (ts) => { ts?.SetInt(tgt); };
                        machine.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", setCur);
                        MelonLogger.Msg($"[Update] force CURRENT {cur} -> {tgt}");
                    }
                    return true; // 让原推进逻辑继续
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[Update] ex: {e}");
                    return true;
                }
            }
        }

        // ============ 主动完成: 夜间遍历机器并完成 ============
        // gridInv.items 结算时为空 → 改用 MachineryHelper.GetAllPoweredOnMachinery() 枚举。
        // 设满 CURRENT 后主动调 UpdateProcessingTypeMachine + FinishProgressTypeMachine。
        [HarmonyPatch(typeof(PlayerStore), "EndNight")]
        public static class PatchEndNight
        {
            public static void Prefix()
            {
                try
                {
                    if (!PlayerStore.IsInstanceExist()) { MelonLogger.Msg("[EndNight] NO PlayerStore instance"); return; }
                    var store = PlayerStore.Instance;
                    long storePtr = 0, invPtr = 0;
                    try { storePtr = (long)Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(store); } catch { }
                    MelonLogger.Msg($"[EndNight] store=@{storePtr:X} machineryBought={store.machineryBoughtCount} hasMachine={store.IsPlayerHaveMachine()}");
                    var inv = store.gridInv;
                    if (inv != null) { try { invPtr = (long)Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(inv); } catch { } }
                    MelonLogger.Msg($"[EndNight] gridInv=@{invPtr:X}");
                    if (inv != null)
                    {
                        var items = inv.items;
                        MelonLogger.Msg($"[EndNight] gridInv.items={(items == null ? "NULL" : "count=" + items.Count.ToString())}");
                    }

                    // 用游戏自己的枚举 FindAllItem 遍历全部物品(含机器)
                    int machineCount = 0, doneCount = 0, forced = 0;
                    Il2CppSystem.Collections.Generic.List<GameItem> machines = null;
                    try { machines = store.FindAllItem(); }
                    catch (Exception e) { MelonLogger.Error($"[EndNight] FindAllItem ex: {e}"); }
                    MelonLogger.Msg($"[EndNight] FindAllItem={(machines == null ? "NULL" : "count=" + machines.Count.ToString())}");

                    if (machines != null)
                    {
                        for (int i = 0; i < machines.Count; i++)
                        {
                            var it = machines[i];
                            if (it == null) continue;
                            // 机器判定修正: GetTagReadonly 缺失 tag 也返回非 null (真凶!),
                            // 必须用 IsTag (真存在才 true)。
                            bool isMachine = false;
                            try { isMachine = it.IsTag("MACHINE_STATE_TAG") || it.IsTag("PROGRESS_TYPE_MACHINE_TAG"); } catch { }
                            if (!isMachine) continue;
                            machineCount++;
                            var curTag = it.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                            var tgtTag = it.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                            int cur = curTag != null ? curTag.GetInt() : -1;
                            int tgt = tgtTag != null ? tgtTag.GetInt() : -1;
                            string state = "?";
                            try { state = MachineProgressHelper.GetMachineState(it); } catch (Exception e) { state = "EX:" + e.GetType().Name; }
                            MelonLogger.Msg($"[EndNight] machine={Describe(it)} state={state} progress=({cur}/{tgt})");

                            // 只处理 STATE_WORKING: READY 未启动(主动 Finish 会把 CURRENT 重置 0 并打断状态机)
                            if (state != "STATE_WORKING")
                            {
                                MelonLogger.Msg($"[EndNight] skip (state={state}, not working)");
                                continue;
                            }
                            if (cur >= 0 && tgt > 0 && cur < tgt)
                            {
                                Action<Il2Cpp.TagState> setCur = (ts) => { ts?.SetInt(tgt); };
                                it.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", setCur);
                                MelonLogger.Msg($"[EndNight] force CURRENT {cur} -> {tgt}");
                                forced++;
                            }
                            if (ForceFinish)
                            {
                                try
                                {
                                    // 主动推进一次让原逻辑看到 current>=target, 然后再 Finish
                                    MachineryHelper.UpdateProcessingTypeMachine(it);
                                    MachineProgressHelper.FinishProgressTypeMachine(it);
                                    doneCount++;
                                }
                                catch (Exception e)
                                {
                                    MelonLogger.Error($"[EndNight] Finish ex: {e}");
                                }
                            }
                        }
                    }
                    MelonLogger.Msg($"[EndNight] machines={machineCount} finished={doneCount} forced={forced} totalItems={machines?.Count ?? -1}");
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[EndNight] ex: {e}");
                }
            }
        }

        // ============ 完成产出诊断 ============
        [HarmonyPatch(typeof(MachineProgressHelper), "FinishProgressTypeMachine")]
        public static class PatchFinish
        {
            public static bool Prefix(GameItem __0)
            {
                try
                {
                    var machine = __0;
                    if (machine == null) return true;
                    bool isMachine = false;
                    try { isMachine = machine.IsTag("MACHINE_STATE_TAG") || machine.IsTag("PROGRESS_TYPE_MACHINE_TAG"); } catch { }
                    if (!isMachine) return true;
                    MelonLogger.Msg($"[Finish] machine={Describe(machine)}");
                    DumpTags(machine, "[Finish]");
                    string state = "?";
                    try { state = MachineProgressHelper.GetMachineState(machine); } catch (Exception e) { state = "EX:" + e.GetType().Name; }
                    MelonLogger.Msg($"[Finish] state={state}");
                    var curTag = machine.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                    int cur = curTag != null ? curTag.GetInt() : -1;
                    int tgt = tgtTag != null ? tgtTag.GetInt() : -1;
                    MelonLogger.Msg($"[Finish] progress=({cur}/{tgt})");
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[Finish] ex: {e}");
                }
                return true;
            }
        }

        // ============ 不消耗耐久 ============
        [HarmonyPatch(typeof(DurabilityHelper), "ChangeDurability")]
        public static class PatchDurability
        {
            public static bool Prefix(int amount)
            {
                if (!NoDurability) return true;
                if (amount < 0) return false; // 负变化(消耗)直接跳过
                return true;
            }
        }

        [HarmonyPatch(typeof(ModuleEffectHelper), "Degrade")]
        public static class PatchDegrade
        {
            public static bool Prefix(int amount)
            {
                if (!NoDurability) return true;
                if (amount < 0) return false;
                return true;
            }
        }

        // 水管/任何灌铁锈水入口: 改为灌 100% 纯水
        [HarmonyPatch(typeof(WaterHelper), "AddRustWater")]
        public static class PatchRustToPure
        {
            public static bool Prefix(GameItem __0, int __1)
            {
                try
                {
                    if (__0 == null) return false;
                    MelonLogger.Msg($"[Water] AddRustWater({Describe(__0)}, +{__1}ml) -> 转为 PureWater");
                    WaterHelper.AddPureWater(__0, __1);
                    return false; // 跳过原逻辑
                }
                catch (Exception e) { MelonLogger.Error($"[Water] AddRustWater patch err: {e.Message}"); return true; }
            }
        }

        // 总入口兜底: grade==4(RUST) -> 0(PURE)
        [HarmonyPatch(typeof(WaterHelper), "AddWater")]
        public static class PatchWaterGrade
        {
            public static bool Prefix(GameItem __0, int __1, int __2, bool __3, int __4, int __5, bool __6)
            {
                try
                {
                    if (__0 == null) return true;
                    if (__1 == 4)
                    {
                        MelonLogger.Msg($"[Water] AddWater(grade={__1}=RUST, +{__2}ml) -> 转为 grade=0(PURE)");
                        WaterHelper.AddWater(__0, 0, __2, __3, __4, __5, __6);
                        return false;
                    }
                    return true;
                }
                catch (Exception e) { MelonLogger.Error($"[Water] AddWater patch err: {e.Message}"); return true; }
            }
        }

        // 净水器/净化器: PurifyToBaseWater -> 清空 + 加等量纯水 (净化效果100%)
        [HarmonyPatch(typeof(WaterHelper), "PurifyToBaseWater")]
        public static class PatchPurifyToPure
        {
            public static bool Prefix(GameItem __0)
            {
                try
                {
                    if (__0 == null) return false;
                    int vol = WaterHelper.GetTotalVolume(__0);
                    WaterHelper.EmptyContainer(__0);
                    WaterHelper.AddPureWater(__0, vol);
                    MelonLogger.Msg($"[Water] PurifyToBaseWater -> 净化100%纯水 (vol={vol}ml)");
                    return false; // 跳过原逻辑
                }
                catch (Exception e) { MelonLogger.Error($"[Water] PurifyToBaseWater patch err: {e.Message}"); return true; }
            }
        }

        // 观察: AddLiquid 的 liquidId
        [HarmonyPatch(typeof(WaterHelper), "AddLiquid")]
        public static class PatchWaterLiquidLog
        {
            public static bool Prefix(GameItem __0, string __1, int __2)
            {
                try
                {
                    if (__0 == null) return true;
                    MelonLogger.Msg($"[Water] AddLiquid(liquidId={__1}, +{__2}ml, container={Describe(__0)})");
                    return true;
                }
                catch (Exception e) { MelonLogger.Error($"[Water] AddLiquid patch err: {e.Message}"); return true; }
            }
        }

        // ============ 模块加成强化 ============
        // ModuleHelper.InitModuleItem 是所有模块(含神经模组)初始化入口,
        // 3 个加成百分比直接乘大。模块创建时调用一次。
        [HarmonyPatch(typeof(ModuleHelper), "InitModuleItem")]
        public static class PatchModuleBoost
        {
            public static bool Prefix(GameItem __0, string __1, ref int __2, ref int __3, ref int __4)
            {
                try
                {
                    if (ModuleBoostMult <= 1) return true;
                    if (__2 > 0) { MelonLogger.Msg($"[Module] {__1}: perf {__2} -> {__2 * ModuleBoostMult}"); __2 *= ModuleBoostMult; }
                    else if (__2 < 0) { MelonLogger.Msg($"[Module] {__1}: perf debuff {__2} -> {Math.Abs(__2)}"); __2 = Math.Abs(__2); }
                    if (__3 > 0) { MelonLogger.Msg($"[Module] {__1}: eff {__3} -> {__3 * ModuleBoostMult}"); __3 *= ModuleBoostMult; }
                    else if (__3 < 0) { MelonLogger.Msg($"[Module] {__1}: eff debuff {__3} -> {Math.Abs(__3)}"); __3 = Math.Abs(__3); }
                    if (__4 > 0) { MelonLogger.Msg($"[Module] {__1}: qual {__4} -> {__4 * ModuleBoostMult}"); __4 *= ModuleBoostMult; }
                    else if (__4 < 0) { MelonLogger.Msg($"[Module] {__1}: qual debuff {__4} -> {Math.Abs(__4)}"); __4 = Math.Abs(__4); }
                    return true; // 让原逻辑用放大后的值
                }
                catch (Exception e) { MelonLogger.Error($"[Module] InitModuleItem patch err: {e.Message}"); return true; }
            }
        }

        // ============ TurboBooster 永远就绪 ============
        [HarmonyPatch(typeof(MachineTurboBoosterAdv), "IsTurboReady")]
        public static class PatchTurboReady
        {
            public static void Postfix(ref bool __result)
            {
                try { if (ForceFinish) __result = true; }
                catch { }
            }
        }

        // ============ 酒品质最高档 ============
        [HarmonyPatch(typeof(WineHelper), "GetWineQualityTier")]
        public static class PatchWineQuality
        {
            public static void Postfix(GameItem __0, ref int __result)
            {
                try
                {
                    if (!MaxWineQuality || __0 == null) return;
                    // 最高档取 6 (游戏酒品质 tier 范围约 0-5)
                    __result = Math.Max(__result, 6);
                }
                catch { }
            }
        }

        // ============ 免电运转 ============
        // CanPower 恒真 + TryDrawCyclePower 跳过原逻辑(不扣电)
        [HarmonyPatch(typeof(MachineHelper), "CanPower")]
        public static class PatchCanPower
        {
            public static bool Prefix(GameItem __0, ref bool __result)
            {
                try
                {
                    if (FreePower && __0 != null)
                    {
                        __result = true;
                        return false; // 跳过原检查
                    }
                    return true;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(MachineHelper), "TryDrawCyclePower")]
        public static class PatchTryDrawPower
        {
            public static bool Prefix(GameItem __0, GameSlotInventory __1, ref bool __result)
            {
                try
                {
                    if (FreePower && __0 != null)
                    {
                        __result = true;
                        return false; // 不消耗电池, 视为供电成功
                    }
                    return true;
                }
                catch { return true; }
            }
        }

        private static string Describe(GameItem it)
        {
            if (it == null) return "null";
            try
            {
                var tag = it.GetTagReadonly("id");
                var id = tag != null ? tag.GetString() : "?";
                long p = 0;
                try { p = (long)Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(it); } catch { }
                return $"{id}@{p:X}";
            }
            catch
            {
                return "@?";
            }
        }

        // ============ 全面日志: 枚举机器全部 tag ============
        private static void DumpTags(GameItem it, string prefix)
        {
            if (it == null) { MelonLogger.Msg($"{prefix} DumpTags: null item"); return; }
            try
            {
                var sys = it.state; // TagSystem
                if (sys == null) { MelonLogger.Msg($"{prefix} DumpTags: state(TagSystem) is null"); return; }
                // TagSystem.dict = Dictionary<string, TagState>
                var dict = sys.dict;
                if (dict == null) { MelonLogger.Msg($"{prefix} DumpTags: dict is null"); return; }
                MelonLogger.Msg($"{prefix} DumpTags: total tags = {dict.Count}");
                foreach (var kv in dict)
                {
                    try
                    {
                        var name = kv.Key;
                        var ts = kv.Value;
                        if (ts == null) { MelonLogger.Msg($"{prefix}   {name} = NULL"); continue; }
                        // 打全部字段: enabled/int/float/long/double/bool/string
                        bool en = false; int iv = 0; float fv = 0f; long lv = 0; double dv = 0; bool bv = false; string sv = "";
                        try { en = ts.valueEnabled; } catch { }
                        try { iv = ts.valueInt; } catch { }
                        try { fv = ts.valueFloat; } catch { }
                        try { lv = ts.valueLong; } catch { }
                        try { dv = ts.valueDouble; } catch { }
                        try { bv = ts.valueBool; } catch { }
                        try { sv = ts.valueString; } catch { }
                        MelonLogger.Msg($"{prefix}   [{name}] en={en} int={iv} float={fv} long={lv} double={dv} bool={bv} str={sv}");
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg($"{prefix}   (tag read ex: {e.GetType().Name})");
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"{prefix} DumpTags ex: {e}");
            }
        }
    }
}
