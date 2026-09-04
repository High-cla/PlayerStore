using System;
using System.Text.Json;
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
        public static bool PurifyAlwaysPure => CfgPurifyAlwaysPure.Value;
        public static bool TurboCooldownFree => CfgTurboCooldownFree.Value;
        public static int OutputMult => CfgOutputMult.Value;
        public static bool NeverWounded => CfgNeverWounded.Value;
        public static bool NoAddiction => CfgNoAddiction.Value;
        public static bool InfiniteScavenging => CfgInfiniteScavenging.Value;

        public override void OnInitializeMelon()
        {
            // 配置在游戏启动时即落盘生成, 玩家可提前看到并修改
            MelonPreferences.Save();
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
            catch { /* IL2CPP 异常: 保持原值 */ }
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
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        "https://high-cla.github.io/PlayerStore/items_browser.html")
                    { UseShellExecute = true });
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
                object resp = new { ok = false, err = "bad" };
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
                            resp = new { ok = false, err = "no itemId" };
                        }
                        else
                        {
                            PendingSpawns.Enqueue((id, n));
                            resp = new { ok = true, queued = $"{id} x{n}" };
                            code = 200;
                        }
                    }
                    else if (req.Url.AbsolutePath == "/api/health")
                    {
                        resp = new { ok = true };
                        code = 200;
                    }
                }
                catch (Exception e)
                {
                    resp = new { ok = false, err = e.Message };
                    code = 500;
                }
                byte[] buf = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resp));
                res.StatusCode = code;
                res.ContentType = "application/json; charset=utf-8";
                res.Headers["Access-Control-Allow-Origin"] = "*";
                res.ContentLength64 = buf.Length;
                res.OutputStream.Write(buf, 0, buf.Length);
                res.OutputStream.Close();
            }
            catch { /* IL2CPP 异常: 保持原值 */ }
        }

        private void SpawnItem(string stableId, int count)
        {
            try
            {
                var inv = EmporiumEntry.Instance.invElement;
                if (inv == null) { MelonLogger.Warning("[Spawn] main inventory unavailable"); return; }
                // 图纸/蓝图类不能直接生成: 提示正确物品ID
                if (stableId.EndsWith("_instruction"))
                {
                    string alt = InstructionToItem(stableId);
                    MelonLogger.Warning($"[Spawn] {stableId} 是图纸(蓝图), 不能直接生成. 用 {alt} 生成实物");
                    return;
                }
                int ok = 0;
                for (int i = 0; i < count; i++)
                {
                    GameItem item = DirectoryMaster.Item(stableId, true);
                    if (item == null) { MelonLogger.Warning($"[Spawn] DirectoryMaster rejected {stableId}"); break; }
                    if (!((GameInventory)inv).MayHaveValidInventorySlot(item)) { MelonLogger.Warning($"[Spawn] no valid slot after {ok}/{count}"); break; }
                    if (!((GameInventory)inv).UncheckedAccept(item)) { MelonLogger.Warning($"[Spawn] native rejected after {ok}/{count}"); break; }
                    ok++;
                }
            }
            catch (Exception e) { MelonLogger.Error($"[Spawn] ex: {e.Message}"); }
        }

        // 图纸 -> 实物物品 映射 (布局目录里的机器/家具)
        private static string InstructionToItem(string instruction)
        {
            switch (instruction)
            {
                case "3d_printer_instruction": return "printer";
                case "furnace_instruction": return "furnace";
                case "evaporator_instruction": return "evaporator";
                case "hydroponic_instruction": return "hydroponic_bay";
                case "age_well_instruction": return "age_well";
                case "alarm_instruction": return "alarm_system";
                default: return "<未知, 查目录>";
            }
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
                    try { isMachine = machine.IsTag("MACHINE_STATE_TAG") || machine.IsTag("PROGRESS_TYPE_MACHINE_TAG"); } catch { /* IL2CPP 异常: 保持原值 */ }
                    if (!isMachine) return true;

                    var curTag = machine.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                    int cur = curTag != null ? curTag.GetInt() : -1;
                    int tgt = tgtTag != null ? tgtTag.GetInt() : -1;

                    if (ForceFinish && machine != null && tgtTag != null)
                    {
                        // 把当前进度直接设为目标 (ModifyTag 写回, GetTagReadonly 是只读副本)
                        machine.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", SetIntAction(tgt));
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
                    if (!PlayerStore.IsInstanceExist()) { return; }
                    var store = PlayerStore.Instance;

                    // 用游戏自己的枚举 FindAllItem 遍历全部物品(含机器)
                    int machineCount = 0, doneCount = 0, forced = 0;
                    Il2CppSystem.Collections.Generic.List<GameItem> machines = null;
                    try { machines = store.FindAllItem(); }
                    catch (Exception e) { MelonLogger.Error($"[EndNight] FindAllItem ex: {e}"); }

                    if (machines != null)
                    {
                        for (int i = 0; i < machines.Count; i++)
                        {
                            var it = machines[i];
                            if (it == null) continue;
                            // 机器判定修正: GetTagReadonly 缺失 tag 也返回非 null (真凶!),
                            // 必须用 IsTag (真存在才 true)。
                            bool isMachine = false;
                            try { isMachine = it.IsTag("MACHINE_STATE_TAG") || it.IsTag("PROGRESS_TYPE_MACHINE_TAG"); } catch { /* IL2CPP 异常: 保持原值 */ }
                            if (!isMachine) continue;
                            machineCount++;
                            var curTag = it.GetTagReadonly("MACHINE_PROGRESS_CURRENT_TAG");
                            var tgtTag = it.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
                            int cur = curTag != null ? curTag.GetInt() : -1;
                            int tgt = tgtTag != null ? tgtTag.GetInt() : -1;
                            string state = "?";
                            try { state = MachineProgressHelper.GetMachineState(it); } catch (Exception e) { state = "EX:" + e.GetType().Name; }

                            // 只处理 STATE_WORKING: READY 未启动(主动 Finish 会把 CURRENT 重置 0 并打断状态机)
                            if (state != "STATE_WORKING")
                            {
                                continue;
                            }
                            if (cur >= 0 && tgt > 0 && cur < tgt)
                            {
                                it.ModifyTag("MACHINE_PROGRESS_CURRENT_TAG", SetIntAction(tgt));
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
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"[EndNight] ex: {e}");
                }
            }
        }

        // ============ 不消耗耐久 ============
        // 注: 不再 patch ModuleEffectHelper.Degrade —— 游戏 0.46D 热修后该类静态构造在
        // HarmonyInit 早期初始化崩溃(Il2Cpp SEH), 且 Degrade 会走 ChangeDurability 主入口, 此处已覆盖.
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

        // 水管/任何灌铁锈水入口: 改为灌 100% 纯水
        [HarmonyPatch(typeof(WaterHelper), "AddRustWater")]
        public static class PatchRustToPure
        {
            public static bool Prefix(GameItem __0, int __1)
            {
                try
                {
                    if (__0 == null) return false;
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
                    return false; // 跳过原逻辑
                }
                catch (Exception e) { MelonLogger.Error($"[Water] PurifyToBaseWater patch err: {e.Message}"); return true; }
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
                    if (bonusPercentagePerformance > 0) { bonusPercentagePerformance *= ModuleBoostMult; }
                    else if (bonusPercentagePerformance < 0) { bonusPercentagePerformance = Math.Abs(bonusPercentagePerformance) * ModuleBoostMult; }
                    if (bonusPercentageEfficiency > 0) { bonusPercentageEfficiency *= ModuleBoostMult; }
                    else if (bonusPercentageEfficiency < 0) { bonusPercentageEfficiency = Math.Abs(bonusPercentageEfficiency) * ModuleBoostMult; }
                    if (bonusPercentageQuality > 0) { bonusPercentageQuality *= ModuleBoostMult; }
                    else if (bonusPercentageQuality < 0) { bonusPercentageQuality = Math.Abs(bonusPercentageQuality) * ModuleBoostMult; }
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
                catch { /* IL2CPP 异常: 保持原值 */ }
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
                catch { /* IL2CPP 异常: 保持原值 */ }
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

        // ============ 永不受伤: 拾荒/战斗/深夜伤口全消毒 ============
        // ScavHelper 负责拾荒受伤掷骰; HealthData 负责伤口结算与恶化.
        // 统一策略: bool 返回 Postfix 强制 false/0, void 结算 Prefix 跳过.
        [HarmonyPatch(typeof(ScavHelper), "RollMinorWound")] public static class PatchRollMinorWound
        {
            public static void Postfix(ref bool __result) { try { if (NeverWounded) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(ScavHelper), "RollMajorWound")] public static class PatchRollMajorWound
        {
            public static void Postfix(ref bool __result) { try { if (NeverWounded) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetMinorWoundChance")] public static class PatchGetMinorWoundChance
        {
            public static void Postfix(ref float __result) { try { if (NeverWounded) __result = 0f; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetMajorWoundChance")] public static class PatchGetMajorWoundChance
        {
            public static void Postfix(ref float __result) { try { if (NeverWounded) __result = 0f; } catch { /* IL2CPP 异常: 保持原值 */ } }
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
            public static void Postfix(ref bool __result) { try { if (NeverWounded) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "HandleNightlyWound")] public static class PatchHandleNightlyWound
        {
            public static bool Prefix() { try { return !NeverWounded; } catch { return true; } }
        }

        // ============ 永不中毒: 酒精/尼古丁/麻醉剂/赌博无成瘾 ============
        // HealthData 成瘾检测与惩罚. bool 检测 Postfix 强制 false, int/void 结算跳过置零.
        [HarmonyPatch(typeof(HealthData), "HasAnyAddiction")] public static class PatchHasAnyAddiction
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "IsAlcoholAddicted")] public static class PatchIsAlcoholAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "IsNicotineAddicted")] public static class PatchIsNicotineAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "IsNarcoticAddicted")] public static class PatchIsNarcoticAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "IsGamblingAddicted")] public static class PatchIsGamblingAddicted
        {
            public static void Postfix(ref bool __result) { try { if (NoAddiction) __result = false; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "GetTotalAddictionPenalty")] public static class PatchGetTotalAddictionPenalty
        {
            public static void Postfix(ref int __result) { try { if (NoAddiction) __result = 0; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(HealthData), "RemoveOneRandomAddiction")] public static class PatchRemoveOneRandomAddiction
        {
            public static bool Prefix() { try { return !NoAddiction; } catch { return true; } }
        }

        // ============ 无限拾荒: 次数与冷却不受限 ============
        // Cpp2IL ISIL 静态分析 (ScavHelper.txt):
        //   CanScavenge: 内联计算 base(5/7 - IsProficientScavenger) - 折算used - PlayerStore[+536](今日计数), 剩余<=0 返回 false
        //   GetMaxScavAttempts/GetScavTimeLeft: 同样内联计算 (不互相调用)
        //   ResetScavenging: PlayerStore[+536] = 0
        // 核心限制点是 CanScavenge (UI/逻辑都问它), patch 它返回 true 即可;
        // GetMaxScavAttempts/GetScavTimeLeft 也 patch 9999 (其他调用方可能直接读).
        [HarmonyPatch(typeof(ScavHelper), "CanScavenge")]
        public static class PatchCanScavenge
        {
            public static void Postfix(ref bool __result) { try { if (InfiniteScavenging) __result = true; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetMaxScavAttempts")] public static class PatchGetMaxScavAttempts
        {
            public static void Postfix(ref int __result) { try { if (InfiniteScavenging) __result = 9999; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }
        [HarmonyPatch(typeof(ScavHelper), "GetScavTimeLeft")] public static class PatchGetScavTimeLeft
        {
            public static void Postfix(ref int __result) { try { if (InfiniteScavenging) __result = 9999; } catch { /* IL2CPP 异常: 保持原值 */ } }
        }

    }
}
