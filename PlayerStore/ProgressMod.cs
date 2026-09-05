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
        // 属性编辑 / 删除操作: (uid, 操作, 字段, 值) 走主线程. uid=item.uniqueId 存档内稳定,
        // 经 FindItemByUid 查全部库存定位任意物品 (不限本次生成)
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
                        var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                        int uid = 0; int.TryParse(q["uid"], out uid);
                        var it = uid != 0 ? FindItemByUid(uid) : null;
                        if (it == null) { resp = new { ok = false, err = "item not found" }; }
                        else { resp = new { ok = true, item = DumpItem(it) }; code = 200; }
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

        // ============ 属性编辑 / 删除: 按 uid 定位任意库存物品 ============
        // 字段写回照 ProbablyStolenItemManager.ApplyBaseField, 删除照 TryExpelAndDestroy
        // (overrideLockRemove=true → inventory.Expel → item.Destroy)
        private static void ApplyItemOp(int uid, ItemOpKind kind, string field, string value)
        {
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
                    case "spritePath": item.spritePath = value; break;
                    case "spriteAtlasPath": item.spriteAtlasPath = value; break;
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
                MelonLogger.Msg($"[ItemOp] edited uid={uid} {field}={value}");
            }
            catch (Exception e) { MelonLogger.Error($"[ItemOp] ex: {e.Message}"); }
        }

        // ============ 库存枚举 / 定位 / 删除 (任意库存物品) ============
        // 全部玩家库存清单 (照 mod GetKnownInventories 主库存集合). 每个返回 GameInventory
        private static System.Collections.Generic.List<GameInventory> EnumeratePlayerInventories()
        {
            var list = new System.Collections.Generic.List<GameInventory>();
            void AddInv(GameInventory inv)
            {
                if (inv != null) list.Add(inv);
            }
            try { var p = PlayerStore.Instance; if (p != null) AddInv(p.gridInv); } catch { }
            try { var e = EmporiumEntry.Instance; if (e != null) { AddInv(e.invElement); AddInv(e.showcaseElement); AddInv(e.docInvElement); AddInv(e.trashInvElement); AddInv(e.trashcanInvElement); } } catch { }
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

        // 删除任意物品: 照 mod TryExpelAndDestroy (overrideLockRemove → Expel → Destroy)
        private static void DeleteItem(GameItem item)
        {
            GameInventory inv = null;
            try { inv = item.parentInventory; } catch { }
            if (inv == null) { MelonLogger.Warning($"[ItemOp] delete uid={item.uniqueId}: 物品无 parentInventory, 跳过"); return; }
            bool restoredLock = false;
            try { inv.overrideLockRemove = true; restoredLock = true; } catch { }
            try
            {
                if (!inv.Expel(item)) { MelonLogger.Warning($"[ItemOp] delete uid={item.uniqueId}: inventory rejected removal"); return; }
                try { item.Destroy(); } catch (Exception e2) { MelonLogger.Warning($"[ItemOp] delete uid={item.uniqueId}: destroy ex {e2.Message}"); }
                try { EmporiumEntry.Instance.Validate(false); } catch { }
                MelonLogger.Msg($"[ItemOp] deleted uid={item.uniqueId}");
            }
            finally
            {
                if (restoredLock)
                {
                    try { inv.overrideLockRemove = false; } catch { }
                }
            }
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
            // keyValue 形如 "key=值" — 直写 TagState 底层 valueString
            int eq = keyValue.IndexOf('=');
            if (eq <= 0) { MelonLogger.Warning($"[ItemOp] tagValue 需 key=值: {keyValue}"); return; }
            string key = keyValue.Substring(0, eq);
            string val = keyValue.Substring(eq + 1);
            var st = FindTagState(item, key, modified);
            if (st == null) { MelonLogger.Warning($"[ItemOp] tag {key} 不存在"); return; }
            try { st.valueString = val; } catch { }
        }

        private static void RemoveItemTag(GameItem item, string key, bool modified)
        {
            try
            {
                var ts = modified ? item.modifiedState : item.state;
                if (ts == null || ts.dict == null || !ts.dict.Remove(key))
                {
                    var ts2 = modified ? item.state : item.modifiedState;
                    if (ts2 != null && ts2.dict != null) ts2.dict.Remove(key);
                }
            }
            catch { }
        }

        private static void AddItemTag(GameItem item, string keyLabel)
        {
            // keyLabel 形如 "key|label" — 新 TagState 直入 base dict (InitTagString 是 private, 用 ctor)
            int pipe = keyLabel.IndexOf('|');
            string key = pipe > 0 ? keyLabel.Substring(0, pipe) : keyLabel;
            string label = pipe > 0 ? keyLabel.Substring(pipe + 1) : key;
            try
            {
                var ts = item.state;
                if (ts == null || ts.dict == null) return;
                if (ts.dict.ContainsKey(key)) { MelonLogger.Warning($"[ItemOp] tag {key} 已存在"); return; }
                var st = new TagState(key, label);
                ts.dict.Add(key, st);
            }
            catch { }
        }

        private static void AddItemFeatureByCategory(GameItem item, string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category)) return;
                var f = new ItemFeature(category);
                try { f.parentItemUniqueId = item.uniqueId; } catch { }
                item.AddItemFeature(f);
            }
            catch { }
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


    }
}
