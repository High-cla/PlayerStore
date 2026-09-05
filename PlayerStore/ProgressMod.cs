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
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int, string, int)> PendingSpawns =
            new System.Collections.Concurrent.ConcurrentQueue<(int, string, int)>();
        // 属性编辑 / 删除操作: (token, 操作, 字段, 值) 走主线程 (GameItem 对象主线程访问)
        private enum ItemOpKind { Edit, Delete }
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int, ItemOpKind, string, string)> PendingItemOps =
            new System.Collections.Concurrent.ConcurrentQueue<(int, ItemOpKind, string, string)>();
        // token -> 本次生成实例 (网页端 "我的生成" 追踪). 游戏重启即失效 (物品仍在库存但引用丢失, 由新生成覆盖)
        private static readonly System.Collections.Generic.Dictionary<int, GameItem> SpawnedItems = new System.Collections.Generic.Dictionary<int, GameItem>();
        private static int _spawnTokenSeq;
        private static System.Net.HttpListener _listener;

        public override void OnUpdate()
        {
            try
            {
                while (PendingSpawns.TryDequeue(out var job))
                {
                    SpawnItem(job.Item1, job.Item2, job.Item3);
                }
                while (PendingItemOps.TryDequeue(out var op))
                {
                    ApplyItemOp(op.Item1, op.Item2, op.Item3, op.Item4);
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
                            int token = System.Threading.Interlocked.Increment(ref _spawnTokenSeq);
                            PendingSpawns.Enqueue((token, id, n));
                            resp = new { ok = true, token = token, queued = $"{id} x{n}" };
                            code = 200;
                        }
                    }
                    else if (req.Url.AbsolutePath == "/api/mine")
                    {
                        // 列出本次会话生成且仍在跟踪的物品 (token 引用)
                        var list = new System.Collections.Generic.List<object>();
                        foreach (var kv in SpawnedItems)
                        {
                            var it = kv.Value;
                            if (it == null) continue;
                            try
                            {
                                list.Add(new
                                {
                                    token = kv.Key,
                                    id = it.identifier ?? "",
                                    name = it.name ?? "",
                                    count = it.unitCount,
                                    unitValue = it.unitValue,
                                    shortDescription = (it.shortDescription ?? "")
                                });
                            }
                            catch { /* IL2CPP 异常: 跳过单条 */ }
                        }
                        resp = new { ok = true, items = list };
                        code = 200;
                    }
                    else if (req.Url.AbsolutePath == "/api/edit")
                    {
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int token = 0; int.TryParse(q["token"], out token);
                        string field = q["field"] ?? "";
                        string value = q["value"] ?? "";
                        if (token == 0 || field == "")
                        {
                            resp = new { ok = false, err = "need token+field" };
                        }
                        else
                        {
                            PendingItemOps.Enqueue((token, ItemOpKind.Edit, field, value));
                            resp = new { ok = true, queued = $"{token} {field}={value}" };
                            code = 200;
                        }
                    }
                    else if (req.Url.AbsolutePath == "/api/delete")
                    {
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int token = 0; int.TryParse(q["token"], out token);
                        if (token == 0)
                        {
                            resp = new { ok = false, err = "need token" };
                        }
                        else
                        {
                            PendingItemOps.Enqueue((token, ItemOpKind.Delete, "", ""));
                            resp = new { ok = true, queued = $"delete {token}" };
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

        // ============ 生成物品: 玩家柜台加权表 (mod 引擎移植) ============
        // 参照 ProbablyStolenItemManager: 生成 → SetAmount → AddDirectToWeightedTable(玩家柜台)
        // → RefreshCounterItem. id 支持三种: stableId | "table:junk" 随机表 | "prebuilt:xxx" 变体
        private void SpawnItem(int token, string id, int count)
        {
            try
            {
                if (count < 1) count = 1;
                if (count > 999) count = 999;
                // 存档门: 主菜单 sprite/lootTables 未初始化, DirectoryMaster.Item 会 NRE.
                // EmporiumEntry 只有进入存档才存在 (原实现用其 invElement 判定)
                if (EmporiumEntry.Instance == null)
                {
                    MelonLogger.Warning("[Spawn] 未进入存档 (主菜单无物品资源), 先进入存档再生成");
                    return;
                }
                try { MelonLogger.Msg($"[Spawn][dbg] id={id} cnt={count}"); } catch { }
                // 图纸/蓝图不能直接生成: 映射到实物 (mod 目录不含图纸, 网页目录含)
                if (!id.StartsWith("table:") && !id.StartsWith("prebuilt:") && id.EndsWith("_instruction"))
                {
                    string alt = InstructionToItem(id);
                    if (alt.StartsWith("<"))
                    {
                        MelonLogger.Warning($"[Spawn] {id} 是图纸(蓝图), 目录未映射实物, 跳过");
                        return;
                    }
                    MelonLogger.Msg($"[Spawn] {id} 图纸 -> 实物 {alt}");
                    id = alt;
                }
                GameItem item = null;
                string detail = "";
                if (id.StartsWith("table:") || id.StartsWith("prebuilt:"))
                {
                    // mod 引擎: 随机掉落表 / 预置变体
                    try { MelonLogger.Msg($"[Spawn][dbg] gen {id}"); } catch { }
                    if (!TryCreateGeneratedItem(id, out item, out detail))
                    {
                        MelonLogger.Warning($"[Spawn] mod 引擎拒绝 {id}: {detail}");
                        return;
                    }
                    try { MelonLogger.Msg($"[Spawn][dbg] gen ok item={item != null}"); } catch { }
                }
                else
                {
                    // 常规 stableId: DirectoryMaster 直生优先, ItemSpawner.Spawn (mod alias 解析) 兜底
                    try { MelonLogger.Msg($"[Spawn][dbg] direct {id}"); } catch { }
                    item = DirectoryMaster.Item(id, true);
                    try { MelonLogger.Msg($"[Spawn][dbg] dm item={item != null}"); } catch { }
                    if (item == null) item = ItemSpawner.Spawn(id);
                    try { MelonLogger.Msg($"[Spawn][dbg] after fallback item={item != null}"); } catch { }
                    if (item == null) { MelonLogger.Warning($"[Spawn] DirectoryMaster/ItemSpawner 均拒绝 {id}"); return; }
                }
                var store = PlayerStore.Instance;
                try { MelonLogger.Msg($"[Spawn][dbg] store null={(store == null)}"); } catch { }
                if (store == null) { MelonLogger.Warning("[Spawn] 未进入存档, 无玩家柜台"); return; }
                item.SetAmount(count);
                store.AddDirectToWeightedTable(item, true);
                try { store.RefreshCounterItem(); } catch { /* IL2CPP 异常: 保持原值 */ }
                if (token > 0) { SpawnedItems[token] = item; }
                MelonLogger.Msg($"[Spawn] added to counter {id} x{count}");
            }
            catch (Exception e) { MelonLogger.Error($"[Spawn] ex: {e.Message}"); }
        }

        // ============ 属性编辑 / 删除: 按 token 定位本次生成物品 ============
        // 字段写回照 ProbablyStolenItemManager.ApplyBaseField, 删除照 TryExpelAndDestroy
        // (overrideLockRemove=true → inventory.Expel → item.Destroy)
        private static void ApplyItemOp(int token, ItemOpKind kind, string field, string value)
        {
            try
            {
                if (!SpawnedItems.TryGetValue(token, out var item) || item == null)
                {
                    MelonLogger.Warning($"[ItemOp] token {token} 未找到 (可能未进存档/已删除/游戏重启)");
                    return;
                }
                if (kind == ItemOpKind.Delete)
                {
                    GameInventory inv = null;
                    try { inv = item.parentInventory; } catch { }
                    if (inv == null)
                    {
                        MelonLogger.Warning($"[ItemOp] delete {token}: 物品无 parentInventory, 跳过");
                        return;
                    }
                    bool restoredLock = false;
                    try { inv.overrideLockRemove = true; restoredLock = true; } catch { }
                    try
                    {
                        if (!inv.Expel(item)) { MelonLogger.Warning($"[ItemOp] delete {token}: inventory rejected removal"); return; }
                        try { item.Destroy(); } catch (Exception e2) { MelonLogger.Warning($"[ItemOp] delete {token}: destroy ex {e2.Message}"); }
                        SpawnedItems.Remove(token);
                        try { EmporiumEntry.Instance.Validate(false); } catch { }
                        MelonLogger.Msg($"[ItemOp] deleted {token}");
                    }
                    finally
                    {
                        if (restoredLock)
                        {
                            try { inv.overrideLockRemove = false; } catch { }
                        }
                    }
                    return;
                }
                // Edit
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                switch (field)
                {
                    case "name": item.SetName(value); break;
                    case "shortDescription": item.shortDescription = value; break;
                    case "longDescription": item.longDescription = value; break;
                    case "flavorText": item.flavorText = value; break;
                    case "customText": item.customText = value; break;
                    case "unitCount":
                        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var uc) && uc >= 0 && uc <= 999999) item.SetUnitCount(uc);
                        else { MelonLogger.Warning($"[ItemOp] edit unitCount invalid: {value}"); return; }
                        break;
                    case "unitBaseValue": if (long.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var ub)) item.unitBaseValue = ub; break;
                    case "unitValue": if (long.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var uv)) item.unitValue = uv; break;
                    case "lateUnitValue": if (long.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var lu)) item.lateUnitValue = lu; break;
                    case "backupUnitValue": if (long.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var bu)) item.backupUnitValue = bu; break;
                    case "bonusAccuracy": if (int.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var ba)) item.bonusAccuracy = ba; break;
                    case "spritePath": item.spritePath = value; break;
                    case "spriteAtlasPath": item.spriteAtlasPath = value; break;
                    default: MelonLogger.Warning($"[ItemOp] 未知字段 {field}"); return;
                }
                try { item.Validate(); } catch { }
                MelonLogger.Msg($"[ItemOp] edited {token} {field}={value}");
            }
            catch (Exception e) { MelonLogger.Error($"[ItemOp] ex: {e.Message}"); }
        }

        // mod 引擎: table:/prebuilt: 统一生成入口 (照抄 ProbablyStolenItemManager.TryCreateGeneratedItem)
        private static bool TryCreateGeneratedItem(string generatorKey, out GameItem item, out string detail)
        {
            item = null;
            detail = "";
            try
            {
                if (generatorKey.StartsWith("table:", StringComparison.Ordinal))
                {
                    string text = ResolveNamedTableId(generatorKey.Substring("table:".Length));
                    if (string.IsNullOrWhiteSpace(text)) { detail = "table not initialized"; return false; }
                    item = ItemSpawner.SpawnFromTable(text);
                }
                else
                {
                    switch (generatorKey)
                    {
                        case "prebuilt:random_cigarette": item = PreBuiltItemHelper.RandomCigarette(); break;
                        case "prebuilt:fake_cigarette": item = PreBuiltItemHelper.FakeCigarette(); break;
                        case "prebuilt:counterfeit_cigarette": item = PreBuiltItemHelper.CounterfeitCigarette(); break;
                        case "prebuilt:random_injector": item = PreBuiltItemHelper.CreateRandomInjector(); break;
                        case "prebuilt:genuine_injector": item = PreBuiltItemHelper.CreateRealGenuineInjector(); break;
                        case "prebuilt:expired_injector": item = PreBuiltItemHelper.CreateRealExpiredInjector(); break;
                        case "prebuilt:counterfeit_injector": item = PreBuiltItemHelper.CreateRealCounterfeitInjector(); break;
                        case "prebuilt:random_stamp": item = PreBuiltItemHelper.CreateRandomStamp(); break;
                        case "prebuilt:fake_stamp": item = PreBuiltItemHelper.CreateFakeStamp(); break;
                        case "prebuilt:real_stamp": item = PreBuiltItemHelper.CreateRealStamp(); break;
                        case "prebuilt:random_performance_module": item = PreBuiltItemHelper.RandomPerformanceModule(); break;
                        case "prebuilt:random_efficiency_module": item = PreBuiltItemHelper.RandomEfficiencyModule(); break;
                        case "prebuilt:random_quality_module": item = PreBuiltItemHelper.RandomQualityModule(); break;
                        case "prebuilt:random_overclock_module": item = PreBuiltItemHelper.RandomOverclockModule(); break;
                        case "prebuilt:random_eco_module": item = PreBuiltItemHelper.RandomEcoModule(); break;
                        case "prebuilt:random_fineness_module": item = PreBuiltItemHelper.RandomFinenessModule(); break;
                        case "prebuilt:random_node": item = PreBuiltItemHelper.RandomNode(); break;
                        case "prebuilt:random_ribwich": item = PreBuiltItemHelper.CreateRibwichRandom(); break;
                        case "prebuilt:random_hand": item = PreBuiltItemHelper.CreateHandRandom(); break;
                        default: detail = "unknown generator key"; return false;
                    }
                }
                if (item == null) { detail = "game returned no item"; return false; }
                return true;
            }
            catch (Exception e) { detail = e.Message; return false; }
        }

        // mod 引擎: 命名表 -> 游戏表 ID (TableMaster const 优先, 同名 ID 回退)
        private static string ResolveNamedTableId(string tableKey)
        {
            try
            {
                if (TableMaster.Instance != null)
                {
                    string text = tableKey switch
                    {
                        "junk" => TableMaster.junkTable,
                        "access_card" => TableMaster.accessCardTable,
                        "all_module" => TableMaster.allModuleTable,
                        "makeshift_weapon" => TableMaster.makeshiftWeaponTable,
                        "material" => TableMaster.materialTable,
                        "household" => TableMaster.householdTable,
                        "packed_food" => TableMaster.packedFoodTable,
                        "t1module" => TableMaster.t1moduleTable,
                        "t2module" => TableMaster.t2moduleTable,
                        "tool" => TableMaster.toolTable,
                        "medical" => TableMaster.medicalTable,
                        _ => "",
                    };
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
            catch { /* IL2CPP 异常: 保持原值 */ }
            return tableKey switch
            {
                "junk" => "junk",
                "access_card" => "access_card",
                "all_module" => "all_module",
                "makeshift_weapon" => "makeshift_weapon",
                "material" => "material",
                "household" => "household",
                "packed_food" => "packed_food",
                "t1module" => "t1module",
                "t2module" => "t2module",
                "tool" => "tool",
                "medical" => "medical",
                _ => "",
            };
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
