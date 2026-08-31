using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(ProgressMod.Core), "ProgressMod", "1.12.1", "local")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]

namespace ProgressMod
{
    public class Core : MelonMod
    {
        private static readonly MelonPreferences_Category Cfg = MelonPreferences.CreateCategory("ProgressMod");
        public static readonly MelonPreferences_Entry<bool> CfgForceFinish = Cfg.CreateEntry<bool>("ForceFinish", true, "进度满: 拦截推进并直接完成");
        public static readonly MelonPreferences_Entry<bool> CfgNoDurability = Cfg.CreateEntry<bool>("NoDurability", true, "不消耗耐久");
        public static readonly MelonPreferences_Entry<int> CfgModuleBoostMult = Cfg.CreateEntry<int>("ModuleBoostMult", 10, "模块加成倍率");
        public static readonly MelonPreferences_Entry<bool> CfgFreePower = Cfg.CreateEntry<bool>("FreePower", true, "免电运转");
        public static readonly MelonPreferences_Entry<bool> CfgMaxWineQuality = Cfg.CreateEntry<bool>("MaxWineQuality", true, "酒品质最高档");
        public static readonly MelonPreferences_Entry<bool> CfgAutoIdentify = Cfg.CreateEntry<bool>("AutoIdentify", true, "对质类物品(香烟/注射器/印章)自动鉴定");
        public static readonly MelonPreferences_Entry<bool> CfgPurifyAlwaysPure = Cfg.CreateEntry<bool>("PurifyAlwaysPure", true, "净化器/过滤器: PurifyToBaseWater 永远净化100%纯水");
        public static readonly MelonPreferences_Entry<bool> CfgTurboCooldownFree = Cfg.CreateEntry<bool>("TurboCooldownFree", true, "TurboBooster: 无视冷却/充能延时, 永远就绪");
        public static readonly MelonPreferences_Entry<int> CfgOutputMult = Cfg.CreateEntry<int>("OutputMult", 3, "机器产出量倍率: 每个加工周期产出的物品数量乘以该倍率");
        public static readonly MelonPreferences_Entry<bool> CfgNeverWounded = Cfg.CreateEntry<bool>("NeverWounded", true, "永不受伤: 拾荒/战斗永不产生伤口, 伤口永不恶化, 深夜不恶化");
        public static readonly MelonPreferences_Entry<bool> CfgNoAddiction = Cfg.CreateEntry<bool>("NoAddiction", true, "永不中毒: 酒精/尼古丁/麻醉剂/赌博永不产生成瘾, 无成瘾惩罚");
        public static readonly MelonPreferences_Entry<bool> CfgInfiniteScavenging = Cfg.CreateEntry<bool>("InfiniteScavenging", true, "无限拾荒: 拾荒次数/冷却不受限");
        public static readonly MelonPreferences_Entry<string> CfgSpawnId = Cfg.CreateEntry<string>("SpawnItemId", "", "生成物品: 填物品 stableId (如 box_tampon), F9 生成到主背包. 空=禁用");
        public static readonly MelonPreferences_Entry<int> CfgSpawnCount = Cfg.CreateEntry<int>("SpawnItemCount", 1, "生成物品数量 (不填默认1)");
        // 逻辑引用保持同名只读属性, 24 处调用处零改动
        public static bool ForceFinish => CfgForceFinish.Value;
        public static bool NoDurability => CfgNoDurability.Value;
        public static int ModuleBoostMult => CfgModuleBoostMult.Value;
        public static bool FreePower => CfgFreePower.Value;
        public static bool MaxWineQuality => CfgMaxWineQuality.Value;
        public static bool AutoIdentify => CfgAutoIdentify.Value;
        public static bool PurifyAlwaysPure => CfgPurifyAlwaysPure.Value;
        public static bool TurboCooldownFree => CfgTurboCooldownFree.Value;
        public static int OutputMult => CfgOutputMult.Value;
        public static bool NeverWounded => CfgNeverWounded.Value;
        public static bool NoAddiction => CfgNoAddiction.Value;
        public static bool InfiniteScavenging => CfgInfiniteScavenging.Value;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("ProgressMod v1.12.1 init");
            StartSpawnServer();
        }

        // ============ 生成物品: HTTP 本地服务器 (网页点击生成) ============
        // 复刻生成逻辑: DirectoryMaster.Item(stableId, true) → MayHaveValidInventorySlot → UncheckedAccept
        // 主背包 = EmporiumEntry.Instance.invElement (GameGridInventory, 转 GameInventory)
        // HTTP 线程只入队, 主线程 OnUpdate 消费 (避免 Il2Cpp 跨线程操作)
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(string, int)> PendingSpawns =
            new System.Collections.Concurrent.ConcurrentQueue<(string, int)>();
        private static System.Net.HttpListener _listener;

        public override void OnUpdate()
        {
            try
            {
                while (PendingSpawns.TryDequeue(out var job))
                {
                    SpawnItem(job.Item1, job.Item2);
                }
            }
            catch { }
        }

        private static void StartSpawnServer()
        {
            try
            {
                _listener = new System.Net.HttpListener();
                _listener.Prefixes.Add("http://localhost:26880/");
                _listener.Start();
                var t = new System.Threading.Thread(ServerLoop) { IsBackground = true };
                t.Start();
                MelonLogger.Msg("[Spawn] HTTP server on http://localhost:26880/");
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        "https://high-cla.github.io/PlayerStore/items_browser.html")
                    { UseShellExecute = true });
                    MelonLogger.Msg("[Spawn] opened items browser");
                }
                catch (Exception e2) { MelonLogger.Warning($"[Spawn] open browser ex: {e2.Message}"); }
            }
            catch (Exception e) { MelonLogger.Error($"[Spawn] server start ex: {e.Message}"); }
        }

        private static void ServerLoop()
        {
            while (_listener != null)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    HandleRequest(ctx);
                }
                catch { /* 监听器停止时跳出 */ }
            }
        }

        private static void HandleRequest(System.Net.HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;
                var res = ctx.Response;
                string body = "{\"ok\":false,\"err\":\"bad\"}";
                int code = 400;
                try
                {
                    if (req.Url.AbsolutePath == "/api/spawn")
                    {
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        string id = q["itemId"] ?? "";
                        int n = 1;
                        int.TryParse(q["count"], out n);
                        if (n < 1) n = 1;
                        if (id == "")
                        {
                            body = "{\"ok\":false,\"err\":\"no itemId\"}";
                        }
                        else
                        {
                            PendingSpawns.Enqueue((id, n));
                            body = $"{{\"ok\":true,\"queued\":\"{id} x{n}\"}}";
                            code = 200;
                        }
                    }
                    else if (req.Url.AbsolutePath == "/api/health")
                    {
                        body = "{\"ok\":true}";
                        code = 200;
                    }
                }
                catch (Exception e)
                {
                    body = $"{{\"ok\":false,\"err\":\"{e.Message}\"}}";
                    code = 500;
                }
                byte[] buf = System.Text.Encoding.UTF8.GetBytes(body);
                res.StatusCode = code;
                res.ContentType = "application/json; charset=utf-8";
                res.Headers["Access-Control-Allow-Origin"] = "*";
                res.ContentLength64 = buf.Length;
                res.OutputStream.Write(buf, 0, buf.Length);
                res.OutputStream.Close();
            }
            catch { }
        }

        private void SpawnItem(string stableId, int count)
        {
            try
            {
                var inv = EmporiumEntry.Instance.invElement;
                if (inv == null) { MelonLogger.Warning("[Spawn] main inventory unavailable"); return; }
                int ok = 0;
                for (int i = 0; i < count; i++)
                {
                    GameItem item = DirectoryMaster.Item(stableId, true);
                    if (item == null) { MelonLogger.Warning($"[Spawn] DirectoryMaster rejected {stableId}"); break; }
                    if (!((GameInventory)inv).MayHaveValidInventorySlot(item)) { MelonLogger.Warning($"[Spawn] no valid slot after {ok}/{count}"); break; }
                    if (!((GameInventory)inv).UncheckedAccept(item)) { MelonLogger.Warning($"[Spawn] native rejected after {ok}/{count}"); break; }
                    ok++;
                }
                MelonLogger.Msg($"[Spawn] {ok}/{count} x {stableId}");
            }
            catch (Exception e) { MelonLogger.Error($"[Spawn] ex: {e.Message}"); }
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

                    var curTag = machine.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                    int cur = curTag != null ? curTag.GetInt() : -1;
                    int tgt = tgtTag != null ? tgtTag.GetInt() : -1;

                    if (ForceFinish && machine != null && tgtTag != null)
                    {
                        // 把当前进度直接设为目标 (ModifyTag 写回, GetTagReadonly 是只读副本)
                        machine.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", SetIntAction(tgt));
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
                    var curTag = machine.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                    int cur = curTag != null ? curTag.GetInt() : -1;
                    int tgt = tgtTag != null ? tgtTag.GetInt() : -1;

                    if (ForceFinish && machine != null && tgtTag != null && cur < tgt)
                    {
                        machine.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", SetIntAction(tgt));
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
                                it.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", SetIntAction(tgt));
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
                    if (!PurifyAlwaysPure) return true;
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
            public static bool Prefix(GameItem item, string moduleType, ref int bonusPercentagePerformance, ref int bonusPercentageEfficiency, ref int bonusPercentageQuality)
            {
                try
                {
                    if (ModuleBoostMult <= 1) return true;
                    if (bonusPercentagePerformance > 0) { MelonLogger.Msg($"[Module] {moduleType}: perf {bonusPercentagePerformance} -> {bonusPercentagePerformance * ModuleBoostMult}"); bonusPercentagePerformance *= ModuleBoostMult; }
                    else if (bonusPercentagePerformance < 0) { MelonLogger.Msg($"[Module] {moduleType}: perf {bonusPercentagePerformance} -> {Math.Abs(bonusPercentagePerformance) * ModuleBoostMult}"); bonusPercentagePerformance = Math.Abs(bonusPercentagePerformance) * ModuleBoostMult; }
                    if (bonusPercentageEfficiency > 0) { MelonLogger.Msg($"[Module] {moduleType}: eff {bonusPercentageEfficiency} -> {bonusPercentageEfficiency * ModuleBoostMult}"); bonusPercentageEfficiency *= ModuleBoostMult; }
                    else if (bonusPercentageEfficiency < 0) { MelonLogger.Msg($"[Module] {moduleType}: eff {bonusPercentageEfficiency} -> {Math.Abs(bonusPercentageEfficiency) * ModuleBoostMult}"); bonusPercentageEfficiency = Math.Abs(bonusPercentageEfficiency) * ModuleBoostMult; }
                    if (bonusPercentageQuality > 0) { MelonLogger.Msg($"[Module] {moduleType}: qual {bonusPercentageQuality} -> {bonusPercentageQuality * ModuleBoostMult}"); bonusPercentageQuality *= ModuleBoostMult; }
                    else if (bonusPercentageQuality < 0) { MelonLogger.Msg($"[Module] {moduleType}: qual {bonusPercentageQuality} -> {Math.Abs(bonusPercentageQuality) * ModuleBoostMult}"); bonusPercentageQuality = Math.Abs(bonusPercentageQuality) * ModuleBoostMult; }
                    return true; // 让原逻辑用放大后的值
                }
                catch (Exception e) { MelonLogger.Error($"[Module] InitModuleItem patch err: {e.Message}"); return true; }
            }
        }

        // ============ 机器产出量乘倍 ============
        // Hook 机器加工进度初始化(带 targetItemCount 的重载): 每个加工周期产出的物品数量 × OutputMult.
        // 需显式 Type[] 消歧(InitProgressSourceItem 有 2 个重载).
        [HarmonyPatch(typeof(MachineProgressHelper), "InitProgressSourceItem", new Type[] { typeof(GameItem), typeof(int), typeof(string), typeof(bool), typeof(int), typeof(string) })]
        public static class PatchOutputMult
        {
            public static void Prefix(GameItem sourceItem, int targetAmount, string targetItemID, bool useDefaultTooltip, ref int targetItemCount, string requiredMachineTag)
            {
                try
                {
                    if (OutputMult <= 1 || targetItemCount <= 0) return;
                    MelonLogger.Msg($"[Output] {targetItemID}: count {targetItemCount} -> {targetItemCount * OutputMult}");
                    targetItemCount *= OutputMult;
                }
                catch (Exception e) { MelonLogger.Error($"[Output] InitProgressSourceItem patch err: {e.Message}"); }
            }
        }

        // ============ TurboBooster 永远就绪 ============
        [HarmonyPatch(typeof(MachineTurboBoosterAdv), "IsTurboReady")]
        public static class PatchTurboReady
        {
            public static void Postfix(ref bool __result)
            {
                try { if (TurboCooldownFree) __result = true; }
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
        [HarmonyPatch(typeof(MachineHelper), "CanPower", new Type[] { typeof(GameItem), typeof(GameSlotInventory) })]
        public static class PatchCanPower
        {
            public static bool Prefix(GameItem machine, ref bool __result)
            {
                try
                {
                    if (FreePower && machine != null)
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
            public static bool Prefix(GameItem machine, GameSlotInventory batterySlot, ref bool __result)
            {
                try
                {
                    if (FreePower && machine != null)
                    {
                        __result = true;
                        return false; // 不消耗电池, 视为供电成功
                    }
                    return true;
                }
                catch { return true; }
            }
        }

        // Il2Cpp 的 ModifyTag 需要 Il2CppSystem.Action<TagState>, 不能直接传 C# lambda
        private static Il2CppSystem.Action<TagState> SetIntAction(int val)
        {
            Action<TagState> a = ts => ts?.SetInt(val);
            return a;
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

        // ============ 自动鉴定: 对质类物品(香烟/注射器/印章) 进入待鉴定即完成 ============
        // InspectableHelper.InitInspectableItem 在物品被标记为待鉴定时调用.
        // Postfix: 直接调 OnConfrontSuccess 完成鉴定, 无需玩家手动对质.
        [HarmonyPatch(typeof(InspectableHelper), "InitInspectableItem")]
        public static class PatchAutoIdentify
        {
            // 真伪判定: 物品带 *_ISSUE tag 即赝品(有缺陷), 否则正品.
            // 探针: 打印物品所有候选 tag keys, 一次运行确认现场真实键名.
            private static readonly string[] ProbeKeys = {
                "CIGARETTE_TAG","INS_INJECTOR_TAG","FOOD_STAMP_TAG",
                "INSPECTABLE_TAG","ALREADY_CONFRONTED_TAG",
                "CATEGORY_GENUINE_CIGARETTE","CATEGORY_GENUINE_INJECTOR","CATEGORY_GENUINE_STAMP",
                "CIGARETTE_BOX_ISSUE","CIGARETTE_COLOR_ISSUE","CIGARETTE_LETTERING_ISSUE","CIGARETTE_LOGO_ISSUE","CIGARETTE_SEAL_ISSUE",
                "ISSUE_BODY","ISSUE_COLOR","ISSUE_INDICATOR","ISSUE_LABEL","ISSUE_SERIAL",
                "STAMP_AMOUNT_ISSUE","STAMP_CHIP_ISSUE","STAMP_LETTERING_ISSUE","STAMP_LOGO_ISSUE","STAMP_SEAL_ISSUE",
                "fakeGenuine","fakeCounterfeit","realGenuine","realCounterfeit",
                "scavInspectable","on_inspected","confrontSucessDiscount","confront_type"
            };

            // 真伪判定: 物品带任何 *_ISSUE tag -> 赝品
            // GetTagReadonly 缺失也返回非 null (真凶!), 必须 IsEnabled() 判存在
            private static bool HasIssueTag(GameItem it)
            {
                foreach (var k in ProbeKeys)
                {
                    if (!k.Contains("_ISSUE") && !k.StartsWith("ISSUE_")) continue;
                    try
                    {
                        var ts = it.GetTagReadonly(k);
                        if (ts != null && ts.IsEnabled()) return true;
                    }
                    catch { }
                }
                return false;
            }
            public static void Postfix(GameItem gameItem)
            {
                try
                {
                    if (!AutoIdentify || gameItem == null) return;

                    // 探针: 打印实际启用的 tag keys (IsEnabled, 缺失的 GetTagReadonly 也非 null)
                    var present = new System.Text.StringBuilder();
                    foreach (var k in ProbeKeys)
                    {
                        try
                        {
                            var ts = gameItem.GetTagReadonly(k);
                            if (ts != null && ts.IsEnabled()) present.Append(k).Append(' ');
                        }
                        catch { }
                    }
                    if (present.Length > 0) MelonLogger.Msg($"[Identify] probe enabled tags: {present}");

                    // 探针2: dump 各已启用 tag 的值, 看真伪/ISSUE 存哪
                    foreach (var k in ProbeKeys)
                    {
                        try
                        {
                            var ts = gameItem.GetTagReadonly(k);
                            if (ts == null || !ts.IsEnabled()) continue;
                            string s = null; int i = 0; float f = 0; double d = 0; bool b = false;
                            string val = "?";
                            try { s = ts.GetString(); val = "str=" + s; } catch { }
                            try { i = ts.GetInt(); val = "int=" + i; } catch { }
                            try { f = ts.GetFloat(); val = "f=" + f; } catch { }
                            try { d = ts.GetDouble(); val = "dbl=" + d; } catch { }
                            try { b = ts.GetBool(); val = "bool=" + b; } catch { }
                            MelonLogger.Msg($"[Identify] tag {k} => {val}");
                        }
                        catch { }
                    }

                    // 真伪分支: 先探针确认 ISSUE tag 语义, 暂统一走原路径(OnConfrontSuccess)
                    // 若 probe 确认 ISSUE tag 即赝品, 下一步改为按真伪分派.
                    bool fake = HasIssueTag(gameItem);
                    MelonLogger.Msg($"[Identify] issue-tag heuristic => {(fake ? "FAKE" : "genuine")} {Describe(gameItem)}");
                    InspectableHelper.OnConfrontSuccess(gameItem);
                    MelonLogger.Msg($"[Identify] auto-identified {Describe(gameItem)}");
                    // 游戏手动鉴定路径会经由 InspectionUIManager 的 HandleXxx/OnInspectItem 刷新 UI 标签;
                    // 自动鉴定只调 Helper 设状态, 不刷 UI => 物品标签不更新. 补一次 UI 刷新.
                    try
                    {
                        var mgr = InspectionUIManager.Instance;
                        if (mgr != null) mgr.OnInspectItem(gameItem);
                    }
                    catch (Exception uie) { MelonLogger.Msg($"[Identify] ui refresh skipped: {uie.Message}"); }
                }
                catch (Exception e) { MelonLogger.Error($"[Identify] ex: {e.Message}"); }
            }
        }

        // ============ 全面日志(已删除: 高频诊断, 不再调用) ============

        // ============ 永不受伤: 拾荒/战斗/深夜伤口全消毒 ============
        // ScavHelper 负责拾荒受伤掷骰; HealthData 负责伤口结算与恶化.
        // 统一策略: bool 返回 Postfix 强制 false/0, void 结算 Prefix 跳过.
        [HarmonyPatch(typeof(ScavHelper), "RollMinorWound")] public static class PatchRollMinorWound
        {
            public static void Postfix(ref bool __result) { try { if (NeverWounded) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(ScavHelper), "RollMajorWound")] public static class PatchRollMajorWound
        {
            public static void Postfix(ref bool __result) { try { if (NeverWounded) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetMinorWoundChance")] public static class PatchGetMinorWoundChance
        {
            public static void Postfix(ref float __result) { try { if (NeverWounded) __result = 0f; } catch { } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetMajorWoundChance")] public static class PatchGetMajorWoundChance
        {
            public static void Postfix(ref float __result) { try { if (NeverWounded) __result = 0f; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "ReceiveMinorWound")] public static class PatchReceiveMinorWound
        {
            public static bool Prefix() { try { return !NeverWounded; } catch { return true; } }
        }
        [HarmonyPatch(typeof(HealthData), "ReceiveMajorWound")] public static class PatchReceiveMajorWound
        {
            public static bool Prefix() { try { return !NeverWounded; } catch { return true; } }
        }
        [HarmonyPatch(typeof(HealthData), "IsSeriouslyWounded")] public static class PatchIsSeriouslyWounded
        {
            public static void Postfix(ref bool __result) { try { if (NeverWounded) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "HandleNightlyWound")] public static class PatchHandleNightlyWound
        {
            public static bool Prefix() { try { return !NeverWounded; } catch { return true; } }
        }

        // ============ 永不中毒: 酒精/尼古丁/麻醉剂/赌博无成瘾 ============
        // HealthData 成瘾检测与惩罚. bool 检测 Postfix 强制 false, int/void 结算跳过置零.
        [HarmonyPatch(typeof(HealthData), "HasAnyAddiction")] public static class PatchHasAnyAddiction
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "IsAlcoholAddicted")] public static class PatchIsAlcoholAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "IsNicotineAddicted")] public static class PatchIsNicotineAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "IsNarcoticAddicted")] public static class PatchIsNarcoticAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "IsGamblingAddicted")] public static class PatchIsGamblingAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "GetTotalAddictionPenalty")] public static class PatchGetTotalAddictionPenalty
        {
            public static void Postfix(ref int __result) { try { if (NoAddiction) __result = 0; } catch { } }
        }
        [HarmonyPatch(typeof(HealthData), "RemoveOneRandomAddiction")] public static class PatchRemoveOneRandomAddiction
        {
            public static bool Prefix() { try { return !NoAddiction; } catch { return true; } }
        }

        // ============ 无限拾荒: 次数与冷却不受限 ============
        [HarmonyPatch(typeof(ScavHelper), "GetMaxScavAttempts")] public static class PatchGetMaxScavAttempts
        {
            public static void Postfix(ref int __result) { try { if (InfiniteScavenging) __result = 9999; } catch { } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetScavTimeLeft")] public static class PatchGetScavTimeLeft
        {
            public static void Postfix(ref int __result) { try { if (InfiniteScavenging) __result = 9999; } catch { } }
        }

    }
}
