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
        public static readonly MelonPreferences_Entry<bool> CfgNeverWounded = Cfg.CreateEntry<bool>("NeverWounded", true, "永不受伤: 拾荒/战斗永不产生伤口, 伤口永不恶化, 深夜不恶化");
        public static readonly MelonPreferences_Entry<bool> CfgInfiniteScavenging = Cfg.CreateEntry<bool>("InfiniteScavenging", true, "无限拾荒: 拾荒次数/冷却不受限");
        // 逻辑引用保持同名只读属性, 24 处调用处零改动
        public static bool ForceFinish => CfgForceFinish.Value;
        public static bool NoDurability => CfgNoDurability.Value;
        public static int ModuleBoostMult => CfgModuleBoostMult.Value;
        public static bool PurifyAlwaysPure => CfgPurifyAlwaysPure.Value;
        public static int OutputMult => CfgOutputMult.Value;
        public static bool NeverWounded => CfgNeverWounded.Value;
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
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int, string, int)> PendingSpawns =
            new System.Collections.Concurrent.ConcurrentQueue<(int, string, int)>();
        // 属性编辑 / 删除操作: (uid, 操作, 字段, 值) 走主线程. uid=item.uniqueId 存档内稳定,
        // 经 FindItemByUid 查全部库存定位任意物品 (不限本次生成)
        private enum ItemOpKind { Edit, Delete }
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int, ItemOpKind, string, string)> PendingItemOps =
            new System.Collections.Concurrent.ConcurrentQueue<(int, ItemOpKind, string, string)>();
        // DumpItem 含 native 方法调用 (GetPublicDisplay/GetActualDisplay 等), 跨线程会 AccessViolation (il2cpp_runtime_invoke).
        // 必须主线程执行: HTTP 线程入队 (uid, seq), 主线程 OnUpdate 产出写 DumpResults[seq], HTTP 线程轮询取值.
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int uid, long seq)> PendingItemDumps =
            new System.Collections.Concurrent.ConcurrentQueue<(int uid, long seq)>();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, object> DumpResults =
            new System.Collections.Concurrent.ConcurrentDictionary<long, object>();
        private static long _dumpSeq;
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
                while (PendingItemDumps.TryDequeue(out var dj))
                {
                    // 主线程执行 DumpItem (含 native 方法调用), 结果写回供 HTTP 线程轮询
                    try
                    {
                        var ditem = FindItemByUid(dj.uid);
                        DumpResults[dj.seq] = ditem == null ? null : DumpItem(ditem);
                    }
                    catch (Exception e) { DumpResults[dj.seq] = null; MelonLogger.Error($"[ItemOp] dump uid={dj.uid} ex: {e.Message}"); }
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
                                uid = SafeInt(() => it.uniqueId),
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
                    else if (req.Url.AbsolutePath == "/api/inventory")
                    {
                        // 枚举玩家全部库存物品 (主背包+柜台+文档+垃圾桶)
                        var seen = new System.Collections.Generic.HashSet<int>();
                        var list = new System.Collections.Generic.List<object>();
                        foreach (var inv in EnumeratePlayerInventories())
                        {
                            if (inv == null) continue;
                            string invName = InvLabel(inv);
                            foreach (var it in ReadInventoryItems(inv))
                            {
                                if (it == null) continue;
                                int u = 0; try { u = it.uniqueId; } catch { }
                                if (u == 0 || !seen.Add(u)) continue;
                                list.Add(new
                                {
                                    uid = u,
                                    id = SafeStr(() => it.identifier, ""),
                                    name = SafeStr(() => it.name, ""),
                                    count = SafeInt(() => it.unitCount),
                                    unitValue = SafeLong(() => it.unitValue),
                                    inv = invName
                                });
                            }
                        }
                        resp = new { ok = true, items = list };
                        code = 200;
                    }
                    else if (req.Url.AbsolutePath == "/api/item")
                    {
                        // DumpItem 含 native 方法调用, 必须主线程执行: 入队 (uid, seq) 后轮询 DumpResults
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int uid = 0; int.TryParse(q["uid"], out uid);
                        if (uid == 0) { resp = new { ok = false, err = "need uid" }; }
                        else
                        {
                            long seq = System.Threading.Interlocked.Increment(ref _dumpSeq);
                            PendingItemDumps.Enqueue((uid, seq));
                            object dump = null;
                            bool got = false;
                            int waited = 0;
                            while (waited < 5000)
                            {
                                if (DumpResults.TryRemove(seq, out dump)) { got = true; break; }
                                System.Threading.Thread.Sleep(10);
                                waited += 10;
                            }
                            if (!got)
                                resp = new { ok = false, err = "dump timeout (主线程未响应, 是否在存档?)" };
                            else if (dump == null) { resp = new { ok = false, err = "item not found" }; }
                            else { resp = new { ok = true, item = dump }; code = 200; }
                        }
                    }
                    else if (req.Url.AbsolutePath == "/api/edit")
                    {
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int uid = 0; int.TryParse(q["uid"], out uid);
                        string field = q["field"] ?? "";
                        string value = q["value"] ?? "";
                        if (uid == 0 || field == "")
                        {
                            resp = new { ok = false, err = "need uid+field" };
                        }
                        else
                        {
                            PendingItemOps.Enqueue((uid, ItemOpKind.Edit, field, value));
                            resp = new { ok = true, queued = $"uid {uid} {field}={value}" };
                            code = 200;
                        }
                    }
                    else if (req.Url.AbsolutePath == "/api/delete")
                    {
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int uid = 0; int.TryParse(q["uid"], out uid);
                        if (uid == 0)
                        {
                            resp = new { ok = false, err = "need uid" };
                        }
                        else
                        {
                            PendingItemOps.Enqueue((uid, ItemOpKind.Delete, "", ""));
                            resp = new { ok = true, queued = $"delete uid {uid}" };
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
                    if (!TryCreateGeneratedItem(id, out item, out detail))
                    {
                        MelonLogger.Warning($"[Spawn] mod 引擎拒绝 {id}: {detail}");
                        return;
                    }
                }
                else
                {
                    // 常规 stableId: 对齐原版 ItemManager.cs:3366 —— ItemSpawner.Spawn 优先 (内部查
                    // PreBuiltItemHelper 引擎表, 命中经引擎 factory 词条注入; 未命中 fallback 裸模板)
                    try { item = ItemSpawner.Spawn(id); }
                    catch (Exception spawnEx) { MelonLogger.Warning($"[Spawn] ItemSpawner.Spawn({id}) 抛异常: {spawnEx.Message}"); }
                    if (item == null) { MelonLogger.Warning($"[Spawn] ItemSpawner 拒绝 {id}"); return; }
                    // node/module 类模板件不带随机词条 —— 对齐原版引擎第二步: 引擎 (RandomNode/
                    // RandomPerformanceModule) 在 DirectoryMaster.Item(base) 后调 InitRandomEffect 注入随机词条.
                    // 直生非变体模板须在此补, 否则产物是游戏里不存在的裸态模块. (prebuilt 引擎产物已带,
                    // 走上方 TryCreateGeneratedItem 分支, 不会二次注入)
                    try
                    {
                        bool isNodeType = false, isModType = false;
                        try { isNodeType = item.IsGameItemType("NODE"); } catch { }
                        try { isModType = item.IsGameItemType("MODULE"); } catch { }
                        if (isNodeType || isModType)
                        {
                            ModuleEffectHelper.InitRandomEffect(item);
                        }
                    }
                    catch (Exception fxEx) { MelonLogger.Warning($"[Spawn] InitRandomEffect({id}) 异常: {fxEx.Message}"); }
                }
                var store = PlayerStore.Instance;
                if (store == null) { MelonLogger.Warning("[Spawn] 未进入存档, 无玩家柜台"); return; }
                item.SetAmount(count);
                store.AddDirectToWeightedTable(item, true);
                try { store.RefreshCounterItem(); } catch { /* IL2CPP 异常: 保持原值 */ }
                if (token > 0) { SpawnedItems[token] = item; }
                MelonLogger.Msg($"[Spawn] added to counter {id} x{count}");
            }
            catch (Exception e) { MelonLogger.Error($"[Spawn] ex: {e.Message}"); }
        }

        // ============ 属性编辑 / 删除: 按 uid 定位任意库存物品 ============
        // 字段写回照 ProbablyStolenItemManager.ApplyBaseField, 删除照 TryExpelAndDestroy
        // (overrideLockRemove=true → inventory.Expel → item.Destroy)
        private static void ApplyItemOp(int uid, ItemOpKind kind, string field, string value)        {
            try
            {
                GameItem item = FindItemByUid(uid);
                if (item == null)
                {
                    MelonLogger.Warning($"[ItemOp] uid {uid} 未找到 (可能已删除/移出库存/游戏重启)");
                    return;
                }
                if (kind == ItemOpKind.Delete)
                {
                    DeleteItem(item);
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
                    case "modifiedXOrigin": if (int.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var mx)) item.modifiedXOrigin = mx; break;
                    case "modifiedYOrigin": if (int.TryParse(value, System.Globalization.NumberStyles.Integer, ci, out var my)) item.modifiedYOrigin = my; break;
                    case "spritePath": item.spritePath = value; break;
                    case "spriteAtlasPath": item.spriteAtlasPath = value; break;
                    // Bool 切换字段 (照原版 ToggleBaseBool ItemManager.cs:2833): value "1"/"true"→设 true, "0"/"false"→设 false, 空/其他→翻转
                    case "activateDefault": ToggleBoolField(() => item.activateDefault, v => item.activateDefault = v, value); break;
                    case "forceDisableActivate": ToggleBoolField(() => item.forceDisableActivate, v => item.forceDisableActivate = v, value); break;
                    case "forceDisableUse": ToggleBoolField(() => item.forceDisableUse, v => item.forceDisableUse = v, value); break;
                    case "canUseOutsideCombat": ToggleBoolField(() => item.canUseOutsideCombat, v => item.canUseOutsideCombat = v, value); break;
                    case "triggerOverwatch": ToggleBoolField(() => item.triggerOverwatch, v => item.triggerOverwatch = v, value); break;
                    case "isCombatBackpack": ToggleBoolField(() => item.isCombatBackpack, v => item.isCombatBackpack = v, value); break;
                    case "isDebugMenu": ToggleBoolField(() => item.isDebugMenu, v => item.isDebugMenu = v, value); break;
                    case "tagEnabled": SetItemTagEnabled(item, value, true); break;
                    case "tagModifiedEnabled": SetItemTagEnabled(item, value, false); break;
                    case "tagValue": SetItemTagStringValue(item, value, false); break;
                    case "tagModifiedValue": SetItemTagStringValue(item, value, true); break;
                    case "tagRemove": RemoveItemTag(item, value, false); break;
                    case "tagModifiedRemove": RemoveItemTag(item, value, true); break;
                    case "tagAdd": AddItemTag(item, value); break;
                    case "featureAdd": AddItemFeatureByCategory(item, value); break;
                    case "featureRemove": if (!string.IsNullOrWhiteSpace(value)) { try { item.RemoveItemFeatureByID(value); } catch { } } break;
                    default: MelonLogger.Warning($"[ItemOp] 未知字段 {field}"); return;
                }
                try { item.Validate(); } catch { }
                RefreshItemAreas();
                MelonLogger.Msg($"[ItemOp] edited uid={uid} {field}={value}");
            }
            catch (Exception e) { MelonLogger.Error($"[ItemOp] ex: {e.Message}"); }
        }

        // 照原版 ToggleBaseBool 语义: value "1"/"true"/"on" → true; "0"/"false"/"off" → false; 空或其它 → 翻转当前值
        private static void ToggleBoolField(System.Func<bool> getter, System.Action<bool> setter, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var low = value.Trim().ToLowerInvariant();
                if (low == "1" || low == "true" || low == "on") { setter(true); return; }
                if (low == "0" || low == "false" || low == "off") { setter(false); return; }
            }
            try { setter(!getter()); } catch { }
        }

        // ============ 库存枚举 / 定位 / 删除 (任意库存物品) ============
        // 全部玩家库存清单 (照 mod GetKnownInventories 主库存集合). 每个返回 GameInventory
        // 注意: AddDirectToWeightedTable 落点是 EmporiumEntry.backInvinvElement (偏移 0x152),
        // 漏枚举该容器会导致 spawn 物品 dump/inventory 查不到
        private static System.Collections.Generic.List<GameInventory> EnumeratePlayerInventories()
        {
            var list = new System.Collections.Generic.List<GameInventory>();
            void AddInv(GameInventory inv)
            {
                if (inv != null) list.Add(inv);
            }
            try { var p = PlayerStore.Instance; if (p != null) AddInv(p.gridInv); } catch { }
            try
            {
                var e = EmporiumEntry.Instance;
                if (e != null)
                {
                    AddInv(e.invElement); AddInv(e.showcaseElement); AddInv(e.docInvElement);
                    AddInv(e.trashInvElement); AddInv(e.swapBufferElement); AddInv(e.drainInvElement);
                    AddInv(e.frontInvinvElement); AddInv(e.backInvinvElement); AddInv(e.backInvinvElementCounter);
                    AddInv(e.bazarLeftinvElement); AddInv(e.hiddenElement); AddInv(e.faucetElement);
                    AddInv(e.cassettePlayerElement); AddInv(e.vendingMachineElement); AddInv(e.vendingFountainElement);
                    AddInv(e.soldElement); AddInv(e.responseInventory); AddInv(e.responseInventoryClosable);
                    AddInv(e.afterhourInventory);
                    AddInv(e.afterhourPocketSlotInvLeft); AddInv(e.afterhourPocketSlotInvBackpack); AddInv(e.afterhourPocketSlotInvRight);
                    AddInv(e.hirelingInv); AddInv(e.trashcanInvElement);
                }
            }
            catch { }
            return list;
        }

        // 库存类型 → 中文标签 (前端展示在哪)
        private static string InvLabel(GameInventory inv)
        {
            try
            {
                if (inv == null) return "";
                try { if (inv == PlayerStore.Instance?.gridInv) return "主背包"; } catch { }
                try
                {
                    var e = EmporiumEntry.Instance;
                    if (e != null)
                    {
                        if (inv == e.invElement) return "柜台货架";
                        if (inv == e.showcaseElement) return "展示柜";
                        if (inv == e.docInvElement) return "文档栏";
                        if (inv == e.trashInvElement) return "垃圾桶";
                        if (inv == e.trashcanInvElement) return "垃圾桶(trashcan)";
                        if (inv == e.backInvinvElement) return "后柜台";
                        if (inv == e.backInvinvElementCounter) return "后柜台(货架)";
                        if (inv == e.frontInvinvElement) return "前柜台";
                        if (inv == e.bazarLeftinvElement) return "巴扎左侧";
                        if (inv == e.swapBufferElement) return "交换缓冲";
                        if (inv == e.drainInvElement) return "排空栏";
                        if (inv == e.hiddenElement) return "隐藏栏";
                        if (inv == e.soldElement) return "已售区";
                        if (inv == e.responseInventory) return "应召响应";
                        if (inv == e.responseInventoryClosable) return "应召响应(可关)";
                        if (inv == e.afterhourInventory) return "歇业库存";
                        if (inv == e.faucetElement) return "水龙头";
                        if (inv == e.cassettePlayerElement) return "磁带机";
                        if (inv == e.vendingMachineElement) return "售货机";
                        if (inv == e.vendingFountainElement) return "售货喷泉";
                        if (inv == e.hirelingInv) return "雇工背包";
                        if (inv == e.afterhourPocketSlotInvLeft) return "歇业口袋左";
                        if (inv == e.afterhourPocketSlotInvBackpack) return "歇业口袋包";
                        if (inv == e.afterhourPocketSlotInvRight) return "歇业口袋右";
                    }
                }
                catch { }
            }
            catch { }
            return "";
        }

        // 按 uniqueId 在全部库存里定位物品 (递归查父容器 + 直接库存 items)
        private static GameItem FindItemByUid(int uid)
        {
            try
            {
                foreach (var inv in EnumeratePlayerInventories())
                {
                    if (inv == null) continue;
                    var found = FindInInventory(inv, uid);
                    if (found != null) return found;
                }
            }
            catch { /* IL2CPP 异常: 保持原值 */ }
            return null;
        }

        private static GameItem FindInInventory(GameInventory inv, int uid)
        {
            try
            {
                var items = ReadInventoryItems(inv);
                foreach (var it in items)
                {
                    if (it == null) continue;
                    try { if (it.uniqueId == uid) return it; } catch { }
                }
            }
            catch { /* IL2CPP 异常: 保持原值 */ }
            return null;
        }

        // GameGridInventory 用 items; 其他 GameInventory 用 childItems (若暴露). 保守两种都试
        private static System.Collections.Generic.List<GameItem> ReadInventoryItems(GameInventory inv)
        {
            var list = new System.Collections.Generic.List<GameItem>();
            try
            {
                if (inv is GameGridInventory grid && grid.items != null)
                {
                    foreach (var it in grid.items) if (it != null) list.Add(it);
                    return list;
                }
            }
            catch { }
            try
            {
                if (inv.childItems != null)
                {
                    foreach (var it in inv.childItems) if (it != null) list.Add(it);
                }
            }
            catch { }
            return list;
        }

        // 删除任意物品: 照原版 ItemManager TryDeleteItem 三重兜底
        // (parentInventory → 遍历全部库存 → fixture trashcan/drain), 成功 RefreshItemAreas
        private static void DeleteItem(GameItem item)
        {
            if (item == null) return;
            int uid = 0;
            try { uid = item.uniqueId; } catch { }
            // 层1: parentInventory
            GameInventory inv = null;
            try { inv = item.parentInventory; } catch { }
            if (inv != null && TryExpelAndDestroy(inv, item))
            {
                RefreshItemAreas();
                MelonLogger.Msg($"[ItemOp] deleted uid={uid}");
                return;
            }
            // 层2: 遍历全部库存
            foreach (var known in EnumeratePlayerInventories())
            {
                if (known == null || known == inv) continue;
                if (InventoryContains(known, item) && TryExpelAndDestroy(known, item))
                {
                    RefreshItemAreas();
                    MelonLogger.Msg($"[ItemOp] deleted uid={uid} ({InvLabel(known)})");
                    return;
                }
            }
            // 层3: fixture (垃圾桶本体/排水口)
            if (TryDeleteKnownFixture(item))
            {
                RefreshItemAreas();
                MelonLogger.Msg($"[ItemOp] deleted uid={uid} (fixture)");
                return;
            }
            MelonLogger.Warning($"[ItemOp] delete uid={uid}: item is not in a removable inventory");
        }

        // 照原版 TryExpelAndDestroy: 备份 overrideLockRemove=true → Expel → Destroy → 还原
        private static bool TryExpelAndDestroy(GameInventory inventory, GameItem item)
        {
            bool restoredLock = false;
            bool overrideLockRemove = false;
            try
            {
                overrideLockRemove = inventory.overrideLockRemove;
                inventory.overrideLockRemove = true;
                restoredLock = true;
            }
            catch { }
            try
            {
                if (!inventory.Expel(item)) return false;
                try { item.Destroy(); } catch { }
                return true;
            }
            finally
            {
                if (restoredLock)
                {
                    try { inventory.overrideLockRemove = overrideLockRemove; } catch { }
                }
            }
        }

        // 照原版 RefreshItemAreas: PlayerStore.RefreshCounterItem + EmporiumEntry.Validate(false)
        private static void RefreshItemAreas()
        {
            try
            {
                var p = PlayerStore.Instance;
                if (p != null) p.RefreshCounterItem();
            }
            catch { }
            try
            {
                var e = EmporiumEntry.Instance;
                if (e != null) e.Validate(false);
            }
            catch { }
        }

        // 照原版 InventoryContains: grid.items + childItems (GameSlotInventory.currentItem 为 private, 经 childItems 覆盖)
        private static bool InventoryContains(GameInventory inventory, GameItem item)
        {
            if (inventory == null || item == null) return false;
            foreach (var it in ReadInventoryItems(inventory))
            {
                if (it == null) continue;
                try { if (it == item) return true; } catch { }
                try { if (it.uniqueId != 0 && item.uniqueId != 0 && it.uniqueId == item.uniqueId) return true; } catch { }
            }
            return false;
        }

        // 照原版 TryDeleteKnownFixture: trashcan/drain 本体对应容器
        private static bool TryDeleteKnownFixture(GameItem item)
        {
            if (item == null) return false;
            try
            {
                var e = EmporiumEntry.Instance;
                if (e == null) return false;
                try
                {
                    if (IsSameItem(e.trashcan, item) && e.trashcanInvElement != null) return TryExpelAndDestroy(e.trashcanInvElement, item);
                }
                catch { }
                try
                {
                    if (IsSameItem(e.drain, item) && e.drainInvElement != null) return TryExpelAndDestroy(e.drainInvElement, item);
                }
                catch { }
            }
            catch { }
            return false;
        }

        // 照原版 IsSameItem: 引用相等或 uniqueId 相等 (uid!=0)
        private static bool IsSameItem(GameItem a, GameItem b)
        {
            if (a == null || b == null) return false;
            try { if (a == b) return true; } catch { }
            try { if (a.uniqueId != 0 && a.uniqueId == b.uniqueId) return true; } catch { }
            return false;
        }

        // ============ 标签 / 特性 操作 ============
        private static TagState FindTagState(GameItem item, string key, bool modified)
        {
            try
            {
                var ts = modified ? item.modifiedState : item.state;
                if (ts == null || ts.dict == null || key == null) return null;
                if (ts.dict.TryGetValue(key, out var st)) return st;
            }
            catch { }
            return null;
        }

        private static void SetItemTagEnabled(GameItem item, string keyAndState, bool modified)
        {
            // keyAndState 形如 "key=1/0" (enable) 或 "key" (toggle 由前端算好)
            string key = keyAndState;
            bool enable = true;
            int eq = keyAndState.IndexOf('=');
            if (eq > 0) { key = keyAndState.Substring(0, eq); bool.TryParse(keyAndState.Substring(eq + 1), out enable); }
            var st = FindTagState(item, key, modified);
            if (st == null) { MelonLogger.Warning($"[ItemOp] tag {key} 不存在"); return; }
            try { st.SetEnabled(enable); } catch { }
        }

        private static void SetItemTagStringValue(GameItem item, string keyValue, bool modified)
        {
            // keyValue 形如 "key=值" — 照原版 ApplyTagField (ItemManager.cs:2919) 用 SetString (与直写 valueString 同存储, 仅 API 保真)
            int eq = keyValue.IndexOf('=');
            if (eq <= 0) { MelonLogger.Warning($"[ItemOp] tagValue 需 key=值: {keyValue}"); return; }
            string key = keyValue.Substring(0, eq);
            string val = keyValue.Substring(eq + 1);
            var st = FindTagState(item, key, modified);
            if (st == null) { MelonLogger.Warning($"[ItemOp] tag {key} 不存在"); return; }
            try { st.SetString(val); } catch { }
        }

        // 照原版 RemoveTagFromItem (ItemManager.cs:2665): 只从指定 system 的 dict.Remove, 缺失报错, 不做跨 system fallback
        private static void RemoveItemTag(GameItem item, string key, bool modified)
        {
            try
            {
                var ts = modified ? item.modifiedState : item.state;
                if (ts == null || ts.dict == null || !ts.dict.Remove(key))
                    MelonLogger.Warning($"[ItemOp] tag 未找到: {key}");
            }
            catch { }
        }

        private static void AddItemTag(GameItem item, string keyLabel)
        {
            // keyLabel 形如 "key|label" — 新 TagState 直入 base dict, 照原版 AddTagToItem (ItemManager.cs:2653) 建后 SetEnabled(true)
            // (InitTagString 是 TagSystem private 无法直调, 用 ctor 等价且免 TYPE-STRING_ 前缀 warning)
            int pipe = keyLabel.IndexOf('|');
            string key = pipe > 0 ? keyLabel.Substring(0, pipe) : keyLabel;
            string label = pipe > 0 ? keyLabel.Substring(pipe + 1) : key;
            try
            {
                var ts = item.state;
                if (ts == null || ts.dict == null) return;
                if (ts.dict.ContainsKey(key)) { MelonLogger.Warning($"[ItemOp] tag {key} 已存在"); return; }
                var st = new TagState(key, label);
                st.SetEnabled(true);
                ts.dict.Add(key, st);
            }
            catch { }
        }

        // 照原版 AddPresetFeature/AddFeatureObject (ItemManager.cs:2691/2725): category 命中 preset → 调 ItemFeatureList 工厂得到完整 feature
        // (规范 category 常量/identifier/featureType/conditions/useCondition/modifiers/display 齐全, 裸 new ItemFeature 缺这些字段 → 游戏内无效);
        // 再照 AddFeatureObject: FindItemFeatureByID 查重 → parentItemUniqueId=item.uniqueId → AddItemFeature
        private static void AddItemFeatureByCategory(GameItem item, string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category)) return;
                var f = BuildItemFeatureByKey(category.Trim());
                if (f == null)
                {
                    // 未命中预设 → 照原版 AddCustomFeature (ItemManager.cs:2704) 裸建 (向后兼容自定义 id)
                    f = new ItemFeature(category.Trim());
                }
                try
                {
                    string fid = f.identifier;
                    if (!string.IsNullOrWhiteSpace(fid) && item.FindItemFeatureByID(fid) != null)
                    {
                        MelonLogger.Warning($"[ItemOp] feature 已存在: {fid}");
                        return;
                    }
                }
                catch { }
                try { f.parentItemUniqueId = item.uniqueId; } catch { }
                item.AddItemFeature(f);
                MelonLogger.Msg($"[ItemOp] feature added: {category.Trim()}");
            }
            catch { }
        }

        // 原版 FeaturePresets (ItemManager.cs:206-219) 的 12 个预设: 按 preset key / 规范 category 双键 → ItemFeatureList 工厂
        // (工厂产物 identifier/category 见子 agent ISIL 报告: free→CATEGORY_FREE/free, discount_25→discount, equipment_good→CATEGORY_EQUIPMENT_CONDITION 等)
        private static ItemFeature BuildItemFeatureByKey(string key)
        {
            try
            {
                switch (key)
                {
                    case "free":
                    case "CATEGORY_FREE": return ItemFeatureList.FreeFeature();
                    case "retail_markup":
                    case "retailMarkUp": return ItemFeatureList.RetailMarkUp();
                    case "donation":
                    case "bribeDonation": return ItemFeatureList.Donation();
                    case "discounted_buy":
                    case "discountedBuy": return ItemFeatureList.DiscountedBuy();
                    case "premium_buy":
                    case "premiumBuy": return ItemFeatureList.PremiumBuy();
                    case "cigarette_authenticity":
                    case "CATEGORY_GENUINE_CIGARETTE": return ItemFeatureList.CigaretteAuthenticity();
                    case "stamp_authenticity":
                    case "CATEGORY_GENUINE_STAMP": return ItemFeatureList.StampAuthenticity();
                    case "module_stuck":
                    case "moduleStuck":
                    case "CATEGORY_MODULE_STUCK": return ItemFeatureList.ModuleStuckFeature();
                    case "discount_25": return ItemFeatureList.Discount(25);
                    case "bargain_markup":
                    case "bargainMarkup": return ItemFeatureList.BargainMarkup(10);
                    case "bargain_discount":
                    case "bargainBuyingDiscount": return ItemFeatureList.BargainBuyingDiscount(10);
                    case "equipment_good":
                    case "CATEGORY_EQUIPMENT_CONDITION": return ItemFeatureList.EquipementConditionFeature((ItemFeatureList.EquipmentCondition)2);
                }
            }
            catch { }
            return null;
        }

        // 物品全字段 dump (基础 + 标签 base/modified + 特性含条件)
        private static object DumpItem(GameItem item)
        {
            if (item == null) return null;
            var tags = new System.Collections.Generic.List<object>();
            var tagsMod = new System.Collections.Generic.List<object>();
            void AppendTags(System.Collections.Generic.List<object> into, TagSystem ts)
            {
                if (ts == null || ts.dict == null) return;
                try
                {
                    foreach (var kv in ts.dict)
                    {
                        var st = kv.Value; if (st == null) continue;
                        into.Add(new
                        {
                            key = SafeStr(() => st.identifier, kv.Key ?? ""),
                            label = SafeStr(() => st.identifierName, ""),
                            enabled = SafeBool(() => st.valueEnabled),
                            valueString = SafeStr(() => st.valueString, ""),
                            valueInt = SafeInt(() => st.valueInt),
                            valueFloat = SafeFloat(() => st.valueFloat),
                        });
                    }
                }
                catch { }
            }
            AppendTags(tags, item.state);
            AppendTags(tagsMod, item.modifiedState);
            var feats = new System.Collections.Generic.List<object>();
            try
            {
                if (item.itemFeatures != null)
                {
                    foreach (var f in item.itemFeatures)
                    {
                        if (f == null) continue;
                        object fake = null, real = null;
                        if (f.fakeCondition != null) fake = DumpCondition(f.fakeCondition);
                        if (f.realCondition != null) real = DumpCondition(f.realCondition);
                        feats.Add(new
                        {
                            identifier = SafeStr(() => f.identifier, ""),
                            category = SafeStr(() => f.category, ""),
                            actualDisplay = SafeStr(() => f.GetActualDisplay(), ""),
                            publicDisplay = SafeStr(() => f.GetPublicDisplay(), ""),
                            valueModifier = SafeInt(() => f.GetActualValueModifier()),
                            useCondition = SafeBool(() => f.useCondition),
                            isDisabled = SafeBool(() => f.isDisabled),
                            fakeCondition = fake,
                            realCondition = real,
                        });
                    }
                }
            }
            catch { }
            return new
            {
                uid = SafeInt(() => item.uniqueId),
                id = SafeStr(() => item.identifier, ""),
                name = SafeStr(() => item.name, ""),
                unitCount = SafeInt(() => item.unitCount),
                unitValue = SafeLong(() => item.unitValue),
                unitBaseValue = SafeLong(() => item.unitBaseValue),
                shortDescription = SafeStr(() => item.shortDescription, ""),
                longDescription = SafeStr(() => item.longDescription, ""),
                flavorText = SafeStr(() => item.flavorText, ""),
                customText = SafeStr(() => item.customText, ""),
                bonusAccuracy = SafeInt(() => item.bonusAccuracy),
                spritePath = SafeStr(() => item.spritePath, ""),
                spriteAtlasPath = SafeStr(() => item.spriteAtlasPath, ""),
                shape = SafeStr(() => item.shape?.ToString(), ""),
                tags = tags,
                tagsModified = tagsMod,
                features = feats,
            };
        }

        private static object DumpCondition(ItemCondition c)
        {
            return new
            {
                identifier = SafeStr(() => c.identifier, ""),
                category = SafeStr(() => c.category, ""),
                display = SafeStr(() => c.display, ""),
                compareValue = SafeInt(() => c.compareValue),
                customValue = SafeInt(() => c.customValue),
                modValue = SafeInt(() => c.modValue),
                isTransformative = SafeBool(() => c.isTransformative),
                hiddenAsPublic = SafeBool(() => c.hiddenAsPublic),
            };
        }

        private static string SafeStr(Func<string> get, string def = "") { try { return get() ?? def; } catch { return def; } }
        private static bool SafeBool(Func<bool> get) { try { return get(); } catch { return false; } }
        private static int SafeInt(Func<int> get) { try { return get(); } catch { return 0; } }
        private static long SafeLong(Func<long> get) { try { return get(); } catch { return 0; } }
        private static float SafeFloat(Func<float> get) { try { return get(); } catch { return 0; } }

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
