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
        public static readonly MelonPreferences_Entry<bool> CfgPurifyAlwaysPure = Cfg.CreateEntry<bool>("PurifyAlwaysPure", true, "净化器/过滤器: PurifyToBaseWater 永远净化100%纯水");
        public static readonly MelonPreferences_Entry<int> CfgOutputMult = Cfg.CreateEntry<int>("OutputMult", 3, "机器产出倍率");
        public static readonly MelonPreferences_Entry<string> CfgSpawnId = Cfg.CreateEntry<string>("SpawnItemId", "", "生成物品: 填物品 stableId (如 box_tampon), F9 生成到主背包. 空=禁用");
        public static readonly MelonPreferences_Entry<int> CfgSpawnCount = Cfg.CreateEntry<int>("SpawnItemCount", 1, "生成物品数量 (不填默认1)");
        // 逻辑引用保持同名只读属性, 24 处调用处零改动
        public static bool ForceFinish => CfgForceFinish.Value;
        public static bool NoDurability => CfgNoDurability.Value;
        public static int ModuleBoostMult => CfgModuleBoostMult.Value;
        public static bool PurifyAlwaysPure => CfgPurifyAlwaysPure.Value;
        public static int OutputMult => CfgOutputMult.Value;

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

        // Il2Cpp 的 ModifyTag 需要 Il2CppSystem.Action<TagState>, 不能直接传 C# lambda
        private static Il2CppSystem.Action<TagState> SetIntAction(int val)
        {
            Action<TagState> a = ts => ts?.SetInt(val);
            return a;
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


        // ============ 机器产出倍率 ============
        // InitProgressSourceItem 有 2 个重载 (3参/6参), 必须用 Type[] 消歧锁定 6 参完整版,
        // 否则 Harmony 匹配首个重载导致 Prefix 参数对不上而静默失效.
        [HarmonyPatch(typeof(MachineProgressHelper), "InitProgressSourceItem",
            new Type[] { typeof(GameItem), typeof(int), typeof(string), typeof(bool), typeof(int), typeof(string) })]
        public static class PatchOutputMult
        {
            // 签名: InitProgressSourceItem(GameItem sourceItem, int targetAmount, string targetItemID,
            //        bool useDefaultTooltip, int targetItemCount, string requiredMachineTag)
            public static void Prefix(GameItem sourceItem, int targetAmount, string targetItemID, bool useDefaultTooltip, ref int targetItemCount, string requiredMachineTag)
            {
                if (OutputMult <= 1) return;
                targetItemCount *= OutputMult; // 默认 3x: 产出件数写入 PROGRESS_ITEM_TARGET_ITEM_COUNT_TAG
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


    }
}
