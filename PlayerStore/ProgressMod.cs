using System;
using System.Text.Json;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(ProgressMod.Core), "ProgressMod", "1.16.0", "local")]
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
        public static readonly MelonPreferences_Entry<bool> CfgFilterBoost = Cfg.CreateEntry<bool>("FilterBoost", true, "加强滤嘴: 剥离量拉到游戏最大值, 耐久成本降到 1");
        public static readonly MelonPreferences_Entry<bool> CfgUvFullPurify = Cfg.CreateEntry<bool>("UvFullPurify", true, "紫外线灯: 除杀菌外一并清除全部杂质");
        public static readonly MelonPreferences_Entry<bool> CfgPurifierFullPurify = Cfg.CreateEntry<bool>("PurifierFullPurify", true, "海德拉净水器: 清除全部杂质(含原版跳过的高纯度水)");
        public static readonly MelonPreferences_Entry<bool> CfgPurifyFillToFull = Cfg.CreateEntry<bool>("PurifyFillToFull", true, "净化后: 用100%纯水补满容器到容量上限");
        public static readonly MelonPreferences_Entry<bool> CfgFaucetAlwaysPure = Cfg.CreateEntry<bool>("FaucetAlwaysPure", true, "水龙头: 接出100%纯水 (原版只给高品质水)");
        public static readonly MelonPreferences_Entry<bool> CfgNeverWounded = Cfg.CreateEntry<bool>("NeverWounded", true, "永不受伤: 拾荒/战斗永不产生伤口, 伤口永不恶化, 深夜不恶化");
        public static readonly MelonPreferences_Entry<bool> CfgInfiniteScavenging = Cfg.CreateEntry<bool>("InfiniteScavenging", true, "无限拾荒: 拾荒次数/冷却不受限");
        // 逻辑引用保持同名只读属性, 24 处调用处零改动
        public static bool ForceFinish => CfgForceFinish.Value;
        public static bool NoDurability => CfgNoDurability.Value;
        public static int ModuleBoostMult => CfgModuleBoostMult.Value;
        public static bool PurifyAlwaysPure => CfgPurifyAlwaysPure.Value;
        public static bool FilterBoost => CfgFilterBoost.Value;
        public static bool UvFullPurify => CfgUvFullPurify.Value;
        public static bool PurifierFullPurify => CfgPurifierFullPurify.Value;
        public static bool PurifyFillToFull => CfgPurifyFillToFull.Value;
        public static bool FaucetAlwaysPure => CfgFaucetAlwaysPure.Value;
        public static bool NeverWounded => CfgNeverWounded.Value;
        public static bool InfiniteScavenging => CfgInfiniteScavenging.Value;

        public override void OnInitializeMelon()
        {
            PurgeLegacyEntries();
            MelonPreferences.Save();
            // 重新获得焦点后继续消费, 这里只是让它不必等待.
            try { UnityEngine.Application.runInBackground = true; }
            catch (Exception e) { MelonLogger.Warning($"[Spawn] runInBackground 设置失败: {e.Message}"); }
            StartSpawnServer();
        }

        private static void PurgeLegacyEntries()
        {
            try
            {
                System.Reflection.MethodInfo del = typeof(MelonPreferences_Category).GetMethod("DeleteEntry", new System.Type[1] { typeof(string) });
                if (del == null)
                {
                    return;
                }
                string[] legacy = new string[2] { "SpawnItemId", "SpawnItemCount" };
                foreach (string id in legacy)
                {
                    try
                    {
                        del.Invoke(Cfg, new object[1] { id });
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        // 复刻生成逻辑: DirectoryMaster.Item(stableId, true) → MayHaveValidInventorySlot → UncheckedAccept, 主背包 = EmporiumEntry.Instance.invElement (GameGridInventory, 转 GameInventory), HTTP 线程只入队, 主线程 OnUpdate 消费 (避免 Il2Cpp 跨线程操作)
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int, string, int)> PendingSpawns =
            new System.Collections.Concurrent.ConcurrentQueue<(int, string, int)>();
        // 属性编辑 / 删除操作: (uid, 操作, 字段, 值) 走主线程. uid=item.uniqueId 存档内稳定, 经 FindItemByUid 查全部库存定位任意物品 (不限本次生成)
        private enum ItemOpKind { Edit, Delete }
        // 编辑/删除请求参数对象: ApplyItemOp 原先 4 个平铺参数 (uid/kind/field/value), 收敛为一个值对象
        private readonly struct ItemOpRequest
        {
            public readonly int Uid;
            public readonly ItemOpKind Kind;
            public readonly string Field;
            public readonly string Value;
            public ItemOpRequest(int uid, ItemOpKind kind, string field, string value)
            {
                Uid = uid; Kind = kind; Field = field; Value = value;
            }
        }
        private static readonly System.Collections.Concurrent.ConcurrentQueue<(int, ItemOpKind, string, string)> PendingItemOps =
            new System.Collections.Concurrent.ConcurrentQueue<(int, ItemOpKind, string, string)>();
        // 主线程作业: DumpItem / 库存枚举都含 native 调用 (GetPublicDisplay / EmporiumEntry.Instance / GameItem 字段...), 跨线程会 AccessViolation (il2cpp_runtime_invoke) 或碰 Unity 主线程约束. 统一机制: HTTP 线程把「要读什么」封成闭包入队 (闭包只捕获请求参数纯值, 绝不跨线程共享, Il2Cpp 引用), 主线程 OnUpdate 执行并回填结果, HTTP 线程等信号取值.
        private sealed class MainThreadJob
        {
            public readonly Func<object> Work;
            public readonly System.Threading.ManualResetEventSlim Done = new System.Threading.ManualResetEventSlim(false);
            public volatile bool Abandoned;
            public object Result;
            public MainThreadJob(Func<object> work) { Work = work; }
        }
        private static readonly System.Collections.Concurrent.ConcurrentQueue<MainThreadJob> PendingJobs =
            new System.Collections.Concurrent.ConcurrentQueue<MainThreadJob>();
        // 主线程作业队列长度上限(背压阈值). 32: 单帧处理 32 个纯读作业耗时远低于 5s 超时窗口,又能吸收网页端并发的 /api/mine + /api/inventory + /api/item 组合请求.
        private const int MaxPendingJobs = 32;
        // HTTP accept 线程数. 上限受限于两处: (a) 主线程作业队列只有 MaxPendingJobs 个槽, (b) 每个 accept 线程最多同时持有一个作业 (它在 RunOnMainThread 上阻塞等待). 故取值须明显小于 MaxPendingJobs, 否则超时释放的线程会立刻抢占已被拒的槽位. 4 => 网页首屏 (mine + inventory + 静态资源) 可并行, 又给主线程留出 28 个槽的余量.
        private const int HttpAcceptThreads = 4;

        // HTTP 线程调用: 入队 + 等主线程执行 (最长 timeoutMs). 返回 false = 未响应 (过载/超时由 overloaded 区分) (失焦暂停 / 大存档卡帧 / 未进存档 都会超时, 故不在此断言具体成因). 事件驱动 (非轮询) ⇒ 无 10ms 量化延迟, 且无「超时后结果残留」的字典泄漏 (作业对象由本线程独占持有).
        private static bool RunOnMainThread(Func<object> work, out object result, int timeoutMs, out bool overloaded)
        {
            //队列已满 = 服务端过载, 立即拒绝而不入队 —— 入队只会让超时更晚发生并加重单帧卡顿. 调用方据此回 503, 前端可退避重试.用 PendingJobs.Count 而非自维护计数: 不存在「忘记递减 ⇒ 永久 503」的泄漏面. 该值是近似量(并发同时入队时可短暂超出上限), 对背压语义无影响.
            overloaded = PendingJobs.Count >= MaxPendingJobs;
            if (overloaded)
            {
                result = null;
                MelonLogger.Warning("[Spawn] 作业队列已满 (" + PendingJobs.Count + "/" + MaxPendingJobs + "), 拒绝请求");
                return false;
            }
            var job = new MainThreadJob(work);
            PendingJobs.Enqueue(job);
            try
            {
                if (!job.Done.Wait(timeoutMs))
                {
                    job.Abandoned = true;
                    result = null;
                    MelonLogger.Warning($"[Spawn] 主线程 {timeoutMs}ms 未响应 (待处理作业 {PendingJobs.Count})");
                    return false;
                }
                result = job.Result;
                return true;
            }
            finally
            {
                // 确定性释放等待句柄. 超时后主线程仍可能 Set 已释放的事件 —— 那边 Set 已包 try/catch.
                job.Done.Dispose();
            }
        }
        private static readonly System.Collections.Generic.Dictionary<int, GameItem> SpawnedItems = new System.Collections.Generic.Dictionary<int, GameItem>();
        private static int _spawnTokenSeq;
        private static System.Net.HttpListener _listener;

        // 本地 HTTP 生成服务器端口. 前端物品浏览器另有一份同名硬编码, 位于docs/items_browser.html:495 (`const API = 'http://localhost:26880'`) —— 改端口时需同步
        private const int ServerPort = 26880;

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
                    ApplyItemOp(new ItemOpRequest(op.Item1, op.Item2, op.Item3, op.Item4));
                }
                while (PendingJobs.TryDequeue(out var job))
                {
                    // 主线程执行 native 读取, 结果回填后唤醒等待的 HTTP 线程. Abandoned = 请求方已超时放弃 (帧率骤降时可能发生), 跳过无谓的 native 工作.
                    if (job.Abandoned) continue;
                    try
                    {
                        job.Result = job.Work();
                    }
                    catch (Exception e)
                    {
                        job.Result = null;
                        MelonLogger.Error($"[Job] 主线程作业异常: {e.Message}");
                    }
                    finally
                    {
                        try { job.Done.Set(); } catch { /* 作业对象已被等待方释放 */ }
                    }
                }
                // 读路径快照重建放最后: 不让它拖慢已排队的作业.
                RefreshSnapshots();
            }
            catch { /* IL2CPP 异常: 保持原值 */ }
        }

        private static void StartSpawnServer()
        {
            try
            {
                _listener = new System.Net.HttpListener();
                _listener.Prefixes.Add($"http://localhost:{ServerPort}/");
                _listener.Start();
                // 多线程 accept: 每个线程各自 GetContext 取走下一个排队连接, 处理完再取下一个.单线程时 ServerLoop 串行处理 => 任一请求在 RunOnMainThread 上阻塞 5s 期间, 其余连接, 全堆在 HttpListener 内核队列里, 页面并发的 /api/mine + /api/inventory + 静态资源,  被逐个串行化. 多 accept 线程让这些请求能同时在途.
                for (int i = 0; i < HttpAcceptThreads; i++)
                {
                    var t = new System.Threading.Thread(ServerLoop) { IsBackground = true, Name = "ProgressMod.Http" + i };
                    t.Start();
                }
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        $"http://localhost:{ServerPort}/")
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
                catch (Exception e)
                {
                    // GetContext 抛出即监听器不可恢复 (已 Dispose / 端口失效). 原实现只 catch 不 break, 与注释「监听器停止时跳出」不符: 该路径会变成无退避的紧循环 (100% CPU 空转).
                    MelonLogger.Warning($"[Spawn] HTTP 监听循环退出: {e.Message}");
                    return;
                }
            }
        }

        // 路由分发: 只负责选路 + 统一异常/写出. 各分支行为 (状态码/错误文本) 在 RouteXxx 内逐字保留.
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
                    if (req.Url.AbsolutePath == "/api/spawn") RouteSpawn(req, out resp, out code);
                    else if (req.Url.AbsolutePath == "/api/mine") RouteMine(out resp, out code);
                    else if (req.Url.AbsolutePath == "/api/inventory") RouteInventory(out resp, out code);
                    else if (req.Url.AbsolutePath == "/api/item") RouteItem(req, out resp, out code);
                    else if (req.Url.AbsolutePath == "/api/edit") RouteEdit(req, out resp, out code);
                    else if (req.Url.AbsolutePath == "/api/delete") RouteDelete(req, out resp, out code);
                    else if (req.Url.AbsolutePath == "/api/health") RouteHealth(out resp, out code);
                    else if (TryRouteWeb(req.Url.AbsolutePath, res)) return;  // 静态资源: 自行写出响应
                }
                catch (Exception e)
                {
                    resp = new { ok = false, err = e.Message };
                    code = 500;
                }
                WriteJson(res, code, resp);
            }
 catch { }
        }


        // 静态网页资源: 由 DLL 内嵌 (csproj EmbeddedResource) —— 本地启动即可打开页面, 与 /api/* 同源, 无跨域也无网络依赖。命中则写出响应并返回 true。
        private static readonly System.Collections.Generic.Dictionary<string, string> WebAssets
            = new System.Collections.Generic.Dictionary<string, string>
        {
            { "/", "web.items_browser.html" },
            { "/index.html", "web.items_browser.html" },
            { "/items_browser.html", "web.items_browser.html" },
            { "/items_data_full.js", "web.items_data_full.js" },
            { "/tag_zh.js", "web.tag_zh.js" },
        };

        private static bool TryRouteWeb(string path, System.Net.HttpListenerResponse res)
        {
            if (path == "/favicon.ico")
            {
                // 浏览器无条件请求 favicon —— 返回 204 避免一条无意义的 404 污染 console
                res.StatusCode = 204;
                res.Close();
                return true;
            }
            string logical;
            if (!WebAssets.TryGetValue(path, out logical)) return false;
            try
            {
                var asm = typeof(Core).Assembly;
                using (var s = asm.GetManifestResourceStream(logical))
                {
                    if (s == null)
                    {
                        MelonLogger.Warning($"[Spawn] 内嵌资源缺失: {logical} (检查 csproj 的 EmbeddedResource)");
                        return false;
                    }
                    var buf = new byte[s.Length];
                    int off = 0;
                    while (off < buf.Length)
                    {
                        int n = s.Read(buf, off, buf.Length - off);
                        if (n <= 0) break;
                        off += n;
                    }
                    res.StatusCode = 200;
                    res.ContentType = logical.EndsWith(".js")
                        ? "application/javascript; charset=utf-8"
                        : "text/html; charset=utf-8";
                    res.Headers["Cache-Control"] = "no-store";
                    res.ContentLength64 = off;
                    res.OutputStream.Write(buf, 0, off);
                    res.OutputStream.Close();
                    return true;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[Spawn] 静态资源写出失败 {path}: {e.Message}");
                return false;
            }
        }

        // 统一响应写出: 状态码 + JSON 头 + CORS + 内容长度 + 关闭输出流
        private static void WriteJson(System.Net.HttpListenerResponse res, int code, object resp)
        {
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resp));
            res.StatusCode = code;
            res.ContentType = "application/json; charset=utf-8";
            res.Headers["Access-Control-Allow-Origin"] = "*";
            res.ContentLength64 = buf.Length;
            res.OutputStream.Write(buf, 0, buf.Length);
            res.OutputStream.Close();
        }

        // GET /api/spawn?itemId=xxx&count=n → 入队生成请求, 立即返回 token
        private static void RouteSpawn(System.Net.HttpListenerRequest req, out object resp, out int code)
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string id = q["itemId"] ?? "";
            int n = 1;
            int.TryParse(q["count"], out n);
            if (n < 1) n = 1;
            if (id == "")
            {
                resp = new { ok = false, err = "no itemId" };
                code = 400;
                return;
            }
            int token = System.Threading.Interlocked.Increment(ref _spawnTokenSeq);
            PendingSpawns.Enqueue((token, id, n));
            resp = new { ok = true, token = token, queued = $"{id} x{n}" };
            code = 200;
        }

        // 物品字段投影 (id/name/count/unitValue). RouteMine 与 RouteInventory 共用同一组 SafeStr/SafeInt/SafeLong 探针; 抽出一处以免两处各自漂移 (原先 4 行逐字重复). 只在主线程作业内调用 (探针触达 native 字段).
        private static dynamic ItemBrief(GameItem it)
        {
            return new
            {
                id = SafeStr(() => it.identifier, ""),
                name = SafeStr(() => it.name, ""),
                count = SafeInt(() => it.unitCount),
                unitValue = SafeLong(() => it.unitValue)
            };
        }

        // ==== 读路径快照 (照生成路径的形态: HTTP 线程绝不等待主线程) ====
        // /api/spawn 入队即回, 从不阻塞; 而 /api/mine 与 /api/inventory 原先让 HTTP 线程 RunOnMainThread 同步等 5s.
        // 主线程一忙 (失焦暂停 / 大存档卡帧 / 未进存档) 两个请求同时超时, 网页「我的生成」与「库存」一起变空 ——
        // 用户看到「加载失败: 主线程未响应 (待处理作业 0)」, 即作业已被取走却没能及时完成.
        // 改为同一形态: 主线程按需重建快照, HTTP 线程只读快照引用并立即返回, 永不阻塞.
        private sealed class ReadSnapshot
        {
            public volatile object Mine;
            public volatile object Inv;
            public volatile bool InSave;   // false = 快照尚未反映存档内状态 (未进存档/刚启动), 前端据此短重试
        }
        private static readonly ReadSnapshot Snapshot = new ReadSnapshot();
        private static volatile bool _wantMine;
        private static volatile bool _wantInv;
        private static long _lastSnapshotMs;
        private const int SnapshotMinIntervalMs = 120;   // 重建节流: 请求洪峰只触发一次重建, 不放大主线程开销

        // GET /api/mine → 读主线程维护的快照, 立即返回 (不阻塞 HTTP 线程). inSave=false 表示快照未反映存档内状态.
        private static void RouteMine(out object resp, out int code)
        {
            _wantMine = true;                                   // 置位请求重建; 本次先回当前快照
            resp = new { ok = true, items = Snapshot.Mine, inSave = Snapshot.InSave };
            code = 200;
        }

        // GET /api/inventory → 读主线程维护的快照, 立即返回 (不阻塞 HTTP 线程).
        private static void RouteInventory(out object resp, out int code)
        {
            _wantInv = true;
            resp = new { ok = true, items = Snapshot.Inv, inSave = Snapshot.InSave };
            code = 200;
        }

        // 仅主线程调用 (OnUpdate). 有需求且过了节流窗口才重建.
        private static void RefreshSnapshots()
        {
            if (!_wantMine && !_wantInv) return;
            long now = System.Environment.TickCount64;
            if (now - _lastSnapshotMs < SnapshotMinIntervalMs) return;
            _lastSnapshotMs = now;
            bool inSave;
            try { inSave = EmporiumEntry.Instance != null; } catch { inSave = false; }
            Snapshot.InSave = inSave;
            if (_wantMine) { _wantMine = false; Snapshot.Mine = BuildMineSnapshot(); }
            if (_wantInv) { _wantInv = false; Snapshot.Inv = BuildInvSnapshot(); }
        }

        // 构建「我的生成」快照 (仅主线程): 枚举 SpawnedItems + 读 GameItem 字段.
        private static object BuildMineSnapshot()
        {
            var list = new System.Collections.Generic.List<object>();
            var dead = new System.Collections.Generic.List<int>();
            foreach (var kv in SpawnedItems)
            {
                var it = kv.Value;
                if (it == null) { dead.Add(kv.Key); continue; }
                int u;
                try { u = it.uniqueId; }
                catch { dead.Add(kv.Key); continue; }
                // uid==0 = native 对象已不可读 (被游戏侧消耗/丢弃/销毁, 或存档重载). 此类 token 已无意义: 保留会让网页端收到 uid=0 的死条目, 点「完整检查器」必然 400, 且因服务端 仍在返回该 token, 前端 refreshMine() 的死 token 清理永不触发.
                if (u == 0) { dead.Add(kv.Key); continue; }
                var b = ItemBrief(it);
                list.Add(new
                {
                    token = kv.Key,
                    uid = u,
                    id = b.id,
                    name = b.name,
                    count = b.count,
                    unitValue = b.unitValue,
                    shortDescription = SafeStr(() => it.shortDescription, "")
                });
            }
            foreach (var k in dead) SpawnedItems.Remove(k);
            return (object)list;
        }

        // 构建库存快照 (仅主线程): EnumeratePlayerInventories / InvLabel / ReadInventoryItems 都触达 native 对象.
        private static object BuildInvSnapshot()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            var list = new System.Collections.Generic.List<object>();
            foreach (var inv in EnumeratePlayerInventories())
            {
                if (inv == null) continue;
                string invName = InvLabel(inv);
                foreach (var it in ReadInventoryItems(inv))
                {
                    if (it == null) continue;
                    int u = 0;
                    try { u = it.uniqueId; }
                    catch
                    {
                        // 静默数据丢失: 读不到 uniqueId 的物品会被下面 u == 0 过滤, 整条记录从 /api/inventory 响应里消失 (网页看不到该物品). 该路径由 HTTP 请求触发, 不在每帧热路径上, 故记警告便于定位而非静默吞掉.
                        MelonLogger.Warning("[ItemOp] inventory: 读取 item.uniqueId 失败, 跳过该物品");
                    }
                    if (u == 0 || !seen.Add(u)) continue;
                    var b = ItemBrief(it);
                    list.Add(new
                    {
                        uid = u,
                        id = b.id,
                        name = b.name,
                        count = b.count,
                        unitValue = b.unitValue,
                        inv = invName
                    });
                }
            }
            return (object)list;
        }

        // GET /api/item?uid=n → DumpItem 含 native 方法调用, 必须主线程执行. 闭包只捕获 uid (纯值), GameItem 引用在主线程作业内解析, 不跨线程传递.
        private static void RouteItem(System.Net.HttpListenerRequest req, out object resp, out int code)
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            int uid = 0; int.TryParse(q["uid"], out uid);
            if (uid == 0)
            {
                resp = new { ok = false, err = "need uid" };
                code = 400;
                return;
            }
            if (!RunOnMainThread(() =>
            {
                var ditem = FindItemByUid(uid);
                return ditem == null ? null : DumpItem(ditem);
            }, out object dump, 5000, out bool overloaded))
            {
                resp = new { ok = false, err = overloaded
                    ? "server busy (队列已满 " + PendingJobs.Count + "/" + MaxPendingJobs + "), 请稍后重试"
                    : "dump timeout (主线程未响应, 待处理作业 " + PendingJobs.Count + ")" };
                code = overloaded ? 503 : 400;
                return;
            }
            if (dump == null)
            {
                resp = new { ok = false, err = "item not found" };
                code = 400;
                return;
            }
            resp = new { ok = true, item = dump };
            code = 200;
        }

        // GET /api/edit?uid=n&field=f&value=v → 入队属性编辑 (主线程执行)
        private static void RouteEdit(System.Net.HttpListenerRequest req, out object resp, out int code)
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            int uid = 0; int.TryParse(q["uid"], out uid);
            string field = q["field"] ?? "";
            string value = q["value"] ?? "";
            if (uid == 0 || field == "")
            {
                resp = new { ok = false, err = "need uid+field" };
                code = 400;
                return;
            }
            PendingItemOps.Enqueue((uid, ItemOpKind.Edit, field, value));
            resp = new { ok = true, queued = $"uid {uid} {field}={value}" };
            code = 200;
        }

        // GET /api/delete?uid=n → 入队删除 (主线程执行)
        private static void RouteDelete(System.Net.HttpListenerRequest req, out object resp, out int code)
        {
            var q = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            int uid = 0; int.TryParse(q["uid"], out uid);
            if (uid == 0)
            {
                resp = new { ok = false, err = "need uid" };
                code = 400;
                return;
            }
            PendingItemOps.Enqueue((uid, ItemOpKind.Delete, "", ""));
            resp = new { ok = true, queued = $"delete uid {uid}" };
            code = 200;
        }

        // GET /api/health → 存活探针
        private static void RouteHealth(out object resp, out int code)
        {
            resp = new { ok = true };
            code = 200;
        }

        //============ 生成物品: 机器件直调原版工厂, 落点主背包强塞 ============ 生成: 机器件 (printer/furnace/security_alarm/moisture_farm/water_purifier/hydroponic) 直调 PreBuiltItemHelper.CreateX —— 命中模板 = DirectoryMaster.Item(id) + MachineX.CreateNote + GraphUtils.TryAcceptAll, 得"带纸条真机器". 用户定调: printer 等本该命中模板, 不命中则产物坏 (变 "?"); 落点不要柜台 (PlayerStore 加权表) → 回主背包 (EmporiumEntry.invElement) 无脑强塞. 非机器件 → ItemSpawner.Spawn(id). id 三种: stableId | "table:junk" | "prebuilt:xxx"
        private void SpawnItem(int token, string id, int count)
        {
            try
            {
                if (count < 1) count = 1;
                if (count > 999) count = 999;
                //存档门: 主菜单 sprite/lootTables 未初始化, DirectoryMaster.Item 会 NRE. EmporiumEntry 只有进入存档才存在 (原实现用其 invElement 判定)
                if (EmporiumEntry.Instance == null)
                {
                    MelonLogger.Warning("[Spawn] 未进入存档 (主菜单无物品资源), 先进入存档再生成");
                    return;
                }
                if (!TryResolveInstruction(ref id)) return;
                //落点: 主背包 (EmporiumEntry.Instance.invElement, GameGridInventory). 用户裁决: 不要柜台 (PlayerStore 加权表); 无脑强塞 (MayHave 预检失败仅警告, UncheckedAccept 裁决).
                var inv = EmporiumEntry.Instance?.invElement;
                if (inv == null) { MelonLogger.Warning("[Spawn] 未进入存档, 无主背包容器"); return; }
                //逐件独立生成, 每件数量为 1: 不用 SetAmount 堆叠, 避免物品格上标出「×N」. 每次都要新建实例 —— 同一实例只属于一个库存格.
                GameItem first = null;
                int done = 0;
                for (int i = 0; i < count; i++)
                {
                    if (!TryCreateSpawnItem(id, out GameItem item)) break;
                    if (!TryAcceptIntoMainInventory((GameInventory)inv, item, id)) break;
                    if (first == null) first = item;
                    done++;
                }
                if (done == 0) return;
                if (token > 0) { SpawnedItems[token] = first; }
                MelonLogger.Msg($"[Spawn] 生成到主背包 {id} x{done} (各自独立, 不堆叠)");
            }
            catch (Exception e) { MelonLogger.Error($"[Spawn] ex: {e.Message}"); }
        }

        // 图纸/蓝图不能直接生成: 映射到实物 (mod 目录不含图纸, 网页目录含). 返回 false = 调用方中止生成
        private static bool TryResolveInstruction(ref string id)
        {
            if (id.StartsWith("table:") || id.StartsWith("prebuilt:") || !id.EndsWith("_instruction")) return true;
            string alt = InstructionToItem(id);
            if (alt.StartsWith("<"))
            {
                MelonLogger.Warning($"[Spawn] {id} 是图纸(蓝图), 目录未映射实物, 跳过");
                return false;
            }
            MelonLogger.Msg($"[Spawn] {id} 图纸 -> 实物 {alt}");
            id = alt;
            return true;
        }

        //按 id 形态生成物品: table:/prebuilt: 走 mod 引擎, 其余 stableId 走原版预置工厂/ItemSpawner. 返回 false = 已记警告, 调用方直接中止
        private static bool TryCreateSpawnItem(string id, out GameItem item)
        {
            item = null;
            if (id.StartsWith("table:") || id.StartsWith("prebuilt:"))
            {
                // mod 引擎: 随机掉落表 / 预置变体
                if (!TryCreateGeneratedItem(id, out item, out string detail))
                {
                    MelonLogger.Warning($"[Spawn] mod 引擎拒绝 {id}: {detail}");
                    return false;
                }
                return true;
            }
            //常规 stableId: 机器件先直调原版预置工厂 = 命中模板 (见 TrySpawnPrebuiltMachine); 无工厂的非机器走 ItemSpawner.Spawn(id) (ItemManager.cs:3366 原样; Spawn 内部仅 18 个 生成商品键命中, 其余 = DirectoryMaster.Item(id) 常规商品, furnace 同此且正常显示).
            if (!TrySpawnPrebuiltMachine(id, out item))
            {
                //容器类板条箱 (evidence_box/med_box/…): 在 ContainerItemDirectory.InitDirectory 里以惰性 Func<GameItem> 工厂注册, 静态物品表查不到 → ItemSpawner.Spawn 必失败. 必须先走 native 工厂. (对齐 ProbablyStolenItemManager 0.4.7 TryCreateNativeLootCrate, ItemManager.cs:3681)
                if (!TrySpawnNativeLootCrate(id, out item))
                {
                    try { item = ItemSpawner.Spawn(id); }
                    catch (Exception spawnEx) { MelonLogger.Warning($"[Spawn] ItemSpawner.Spawn({id}) 抛异常: {spawnEx.Message}"); }
                }
            }
            if (item == null) { MelonLogger.Warning($"[Spawn] ItemSpawner 拒绝 {id}"); return false; }
            //node/module 类模板件不带随机词条 —— 对齐原版引擎第二步: 引擎 (RandomNode/ RandomPerformanceModule) 在 DirectoryMaster.Item(base) 后调 InitRandomEffect 注入随机词条. 直生非变体模板须在此补, 否则产物是游戏里不存在的裸态模块. (prebuilt 引擎产物已带, 走上方 TryCreateGeneratedItem 分支, 不会二次注入)
            try
            {
                bool isNodeType = false, isModType = false;
                try { isNodeType = item.IsGameItemType("NODE"); } catch { /* ponytail: IL2CPP native probe, silent fallback */ }
 try { isModType = item.IsGameItemType("MODULE"); } catch { }
                if (isNodeType || isModType)
                {
                    ModuleEffectHelper.InitRandomEffect(item);
                }
            }
            catch (Exception fxEx) { MelonLogger.Warning($"[Spawn] InitRandomEffect({id}) 异常: {fxEx.Message}"); }
            return true;
        }

        //容器类板条箱: 静态物品表 (ItemSpawner.Spawn) 查不到 —— 它们在 ContainerItemDirectory 里以 惰性 Func<GameItem> 工厂注册 (见 IL2CPP ContainerItemDirectory.InitDirectory). 只能直调 native 工厂. 5 个已确证存在 (Assembly-CSharp PreBuiltItemHelper.LootCrate*); sci/research/sup/supply_box 走反射 是 0.4.7 的向前兼容探测 —— 本游戏无此 3 名, 反射失败即回落 ItemSpawner, 无副作用. (对齐 ProbablyStolenItemManager 0.4.7 TryCreateNativeLootCrate, ItemManager.cs:3681)
        private static bool TrySpawnNativeLootCrate(string id, out GameItem item)
        {
            item = null;
            try
            {
                switch (id)
                {
                    case "evidence_box": item = PreBuiltItemHelper.LootCrateEvidence(); break;
                    case "med_box": item = PreBuiltItemHelper.LootCrateMedical(); break;
                    case "sec_box": item = PreBuiltItemHelper.LootCrateSecurity(); break;
                    case "service_box": item = PreBuiltItemHelper.LootCrateService(); break;
                    case "eng_box": item = PreBuiltItemHelper.LootCrateEngineering(); break;
                    default: return false;
                }
                return item != null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Spawn] LootCrate 工厂({id}) 异常: {ex.Message}");
                item = null;
                return false;
            }
        }

        //强塞主背包: MayHave 预检只警告(机器件/超大件常报无格但仍可塞), UncheckedAccept 才是裁决. 返回 false = 已记警告, 调用方直接中止
        private static bool TryAcceptIntoMainInventory(GameInventory inv, GameItem item, string id)
        {
            try
            {
                if (!inv.MayHaveValidInventorySlot(item))
                {
                    MelonLogger.Warning($"[Spawn] {id} 预检无有效格(机器件/超大?), 尝试强塞主背包…");
                }
            }
            catch (Exception slotEx) { MelonLogger.Warning($"[Spawn] MayHaveValidInventorySlot({id}) 异常: {slotEx.Message}"); }
            try
            {
                if (!inv.UncheckedAccept(item))
                {
                    MelonLogger.Warning($"[Spawn] UncheckedAccept 拒绝 {id}");
                    return false;
                }
            }
            catch (Exception accEx) { MelonLogger.Warning($"[Spawn] UncheckedAccept({id}) 异常: {accEx.Message}"); }
            return true;
        }

        //机器件直调原版预置工厂 (命中模板 → 带纸条真机器). 无对应工厂返回 false, 由调用方回退 Spawn. 工厂 = DirectoryMaster.Item(id,true) + MachineX.CreateNote(null) + GraphUtils.TryAcceptAll(item,note,-1) (PreBuiltItemHelper.txt CreatePrinter:12116 同构). SpawnItem 由 OnUpdate 主线程消费调用.
        private static bool TrySpawnPrebuiltMachine(string id, out GameItem item)
        {
            item = null;
            try
            {
                switch (id)
                {
                    case "printer": item = PreBuiltItemHelper.CreatePrinter(); break;
                    case "furnace": item = PreBuiltItemHelper.CreateFurnace(); break;
                    case "security_alarm": item = PreBuiltItemHelper.CreateAlarm(); break;
                    case "moisture_farm": item = PreBuiltItemHelper.CreateMoistureFarm(); break;
                    case "water_purifier": item = PreBuiltItemHelper.CreateWaterPurifier(); break;
                    case "hydroponic": item = PreBuiltItemHelper.CreateHydroponic(); break;
                    default: return false;
                }
                return item != null;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Spawn] PreBuiltItemHelper 工厂({id}) 异常: {ex.Message}");
                item = null;
                return false; // 工厂失败回退常规 Spawn
            }
        }

        //============ 属性编辑 / 删除: 按 uid 定位任意库存物品 ============ 字段写回照 ProbablyStolenItemManager.ApplyBaseField, 删除照 TryExpelAndDestroy (overrideLockRemove=true → inventory.Expel → item.Destroy)
        private static void ApplyItemOp(ItemOpRequest req)
        {
            // 局部解构: 保持下方 switch 分支体逐字不变 (低风险收参, 不改写行为)
            int uid = req.Uid;
            ItemOpKind kind = req.Kind;
            string field = req.Field;
            string value = req.Value;
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
                    ForgetSpawned(item);
                    return;
                }
                // Edit
                if (!ApplyEditField(item, field, value)) return;
 try { item.Validate(); } catch { }
                RefreshItemAreas();
                MelonLogger.Msg($"[ItemOp] edited uid={uid} {field}={value}");
            }
            catch (Exception e) { MelonLogger.Error($"[ItemOp] ex: {e.Message}"); }
        }

        // 单字段写回. 返回 false = 已记警告, 调用方跳过 Validate/Refresh/edited 日志 (与拆分前 return 语义一致)
        private static bool ApplyEditField(GameItem item, string field, string value)
        {
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
                    else { MelonLogger.Warning($"[ItemOp] edit unitCount invalid: {value}"); return false; }
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
                default: MelonLogger.Warning($"[ItemOp] 未知字段 {field}"); return false;
            }
            return true;
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

        //============ 库存枚举 / 定位 / 删除 (任意库存物品) ============ 全部玩家库存清单 (照 mod GetKnownInventories 主库存集合). 每个返回 GameInventory 注意: spawn 落点 = EmporiumEntry.invElement (柜台货架/主背包, 本函数第 451 行已枚举); 其余后柜台/巴扎等容器一并枚举, 供 edit/delete 定位任意库存物品
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
            catch
            {
            }
            return list;
        }

        // 库存类型 → 中文标签 (前端展示在哪)
        //
        //顺序敏感 + 惰性: 原实现是 24 个连续的 `if (inv == e.xxxElement) return "标签";`, 首次匹配即返回. 两个语义必须原样保留, 否则标签会变: 1) e.xxxElement 是 property getter (get_invElement:733 / get_showcaseElement:779 / get_soldElement:2364 / get_trashcanInvElement:2548 ...), 不是字段读取 —— 命中项之后的 getter 在原实现里根本不会被求值 (部分 getter 未初始化时会分配对象甚至抛异常). 2) 若某两个 getter 返回同一实例, 顺序决定返回哪个标签. 故用有序的 (比较委托, 标签) 数组运行时顺序遍历, 而非预建 Dictionary: 预建表会一次性求值 全部 24 个 getter 且破坏首匹配语义. 比较式 `i == e.xxxElement` 与原文逐字一致 —— 同一静态类型 (GameInventory vs 各具体库存子类)、同一 operator== 解析, 不做抬高转换.
        private sealed class InvLabelEntry
        {
            public readonly Func<GameInventory, EmporiumEntry, bool> Match;
            public readonly string Label;
            public InvLabelEntry(Func<GameInventory, EmporiumEntry, bool> match, string label)
            {
                Match = match;
                Label = label;
            }
        }

        private static readonly InvLabelEntry[] InvLabels =
        {
            new InvLabelEntry((i, e) => i == e.invElement, "柜台货架"),
            new InvLabelEntry((i, e) => i == e.showcaseElement, "展示柜"),
            new InvLabelEntry((i, e) => i == e.docInvElement, "文档栏"),
            new InvLabelEntry((i, e) => i == e.trashInvElement, "垃圾桶"),
            new InvLabelEntry((i, e) => i == e.trashcanInvElement, "垃圾桶(trashcan)"),
            new InvLabelEntry((i, e) => i == e.backInvinvElement, "后柜台"),
            new InvLabelEntry((i, e) => i == e.backInvinvElementCounter, "后柜台(货架)"),
            new InvLabelEntry((i, e) => i == e.frontInvinvElement, "前柜台"),
            new InvLabelEntry((i, e) => i == e.bazarLeftinvElement, "巴扎左侧"),
            new InvLabelEntry((i, e) => i == e.swapBufferElement, "交换缓冲"),
            new InvLabelEntry((i, e) => i == e.drainInvElement, "排空栏"),
            new InvLabelEntry((i, e) => i == e.hiddenElement, "隐藏栏"),
            new InvLabelEntry((i, e) => i == e.soldElement, "已售区"),
            new InvLabelEntry((i, e) => i == e.responseInventory, "应召响应"),
            new InvLabelEntry((i, e) => i == e.responseInventoryClosable, "应召响应(可关)"),
            new InvLabelEntry((i, e) => i == e.afterhourInventory, "歇业库存"),
            new InvLabelEntry((i, e) => i == e.faucetElement, "水龙头"),
            new InvLabelEntry((i, e) => i == e.cassettePlayerElement, "磁带机"),
            new InvLabelEntry((i, e) => i == e.vendingMachineElement, "售货机"),
            new InvLabelEntry((i, e) => i == e.vendingFountainElement, "售货喷泉"),
            new InvLabelEntry((i, e) => i == e.hirelingInv, "雇工背包"),
            new InvLabelEntry((i, e) => i == e.afterhourPocketSlotInvLeft, "歇业口袋左"),
            new InvLabelEntry((i, e) => i == e.afterhourPocketSlotInvBackpack, "歇业口袋包"),
            new InvLabelEntry((i, e) => i == e.afterhourPocketSlotInvRight, "歇业口袋右"),
        };

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
                        foreach (var entry in InvLabels)
                        {
                            if (entry.Match(inv, e)) return entry.Label;
                        }
                    }
                }
                catch
                {
                }
            }
            catch
            {
            }
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
 catch { }
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
 catch { }
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
            catch
            {
            }
            try
            {
                if (inv.childItems != null)
                {
                    foreach (var it in inv.childItems) if (it != null) list.Add(it);
                }
            }
            catch
            {
            }
            return list;
        }

        //删除任意物品: 照原版 ItemManager TryDeleteItem 三重兜底 (parentInventory → 遍历全部库存 → fixture trashcan/drain), 成功 RefreshItemAreas
        private static void DeleteItem(GameItem item)
        {
            if (item == null) return;
            int uid = 0;
            //uid / parentInventory 读失败都不会中断删除本身, 但会显著改变删除路径与日志可读性 (UID 探针只影响日志文本, 是最常被 native 异常打断的一步), 故记警告而非静默.
            try { uid = item.uniqueId; }
            catch
            {
                // 静默数据丢失: 删除日志里 uid 恒为 0, 事后无法对账到底是哪一条被删.
                MelonLogger.Warning("[ItemOp] delete: 读取 item.uniqueId 失败 (日志将显示 uid=0)");
            }
            // 层1: parentInventory
            GameInventory inv = null;
            try { inv = item.parentInventory; }
            catch
            {
                //静默数据丢失: parentInventory 读失败会静默跳过「层1」删除路径, 降级到层2/层3; 物品仍可能被删掉, 但走了非预期路径, 需要能看见.
                MelonLogger.Warning("[ItemOp] delete: 读取 item.parentInventory 失败, 跳过层1(直连父容器)路径");
            }
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
            catch
            {
            }
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
            catch
            {
            }
            try
            {
                var e = EmporiumEntry.Instance;
                if (e != null) e.Validate(false);
            }
            catch
            {
            }
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
                catch
                {
                }
                try
                {
                    if (IsSameItem(e.drain, item) && e.drainInvElement != null) return TryExpelAndDestroy(e.drainInvElement, item);
                }
                catch
                {
                }
            }
            catch
            {
            }
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

        //删除成功后清掉「我的生成」里指向该物品的 token. 网页端 refreshMine() 以「服务端已不再返回 该 token」为唯一依据清理 localStorage 死 token (items_browser.html:548) —— 此处不删则列表 永久残留幽灵条目 (uid/name 全空), 且 SpawnedItems 长期持有已 Destroy 的 native 引用阻止回收. 仅主线程调用 (DeleteItem 只在 OnUpdate 排空路径执行), 与 SpawnedItems 的其他访问同线程, 无需加锁.
        private static void ForgetSpawned(GameItem item)
        {
            if (item == null || SpawnedItems.Count == 0) return;
            var dead = new System.Collections.Generic.List<int>();
            foreach (var kv in SpawnedItems)
            {
                if (IsSameItem(kv.Value, item)) dead.Add(kv.Key);
            }
            foreach (var k in dead) SpawnedItems.Remove(k);
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
            catch
            {
            }
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
            catch
            {
            }
        }

        private static void AddItemTag(GameItem item, string keyLabel)
        {
            //keyLabel 形如 "key|label" — 新 TagState 直入 base dict, 照原版 AddTagToItem (ItemManager.cs:2653) 建后 SetEnabled(true) (InitTagString 是 TagSystem private 无法直调, 用 ctor 等价且免 TYPE-STRING_ 前缀 warning)
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
            catch
            {
            }
        }

        //照原版 AddPresetFeature/AddFeatureObject (ItemManager.cs:2691/2725): category 命中 preset → 调 ItemFeatureList 工厂得到完整 feature (规范 category 常量/identifier/featureType/conditions/useCondition/modifiers/display 齐全, 裸 new ItemFeature 缺这些字段 → 游戏内无效); 再照 AddFeatureObject: FindItemFeatureByID 查重 → parentItemUniqueId=item.uniqueId → AddItemFeature
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
                catch
                {
                }
 try { f.parentItemUniqueId = item.uniqueId; } catch { }
                item.AddItemFeature(f);
                MelonLogger.Msg($"[ItemOp] feature added: {category.Trim()}");
            }
            catch
            {
            }
        }

        //原版 FeaturePresets (ItemManager.cs:206-219) 的 12 个预设: 按 preset key / 规范 category 双键 → ItemFeatureList 工厂 (工厂产物 identifier/category 见子 agent ISIL 报告: free→CATEGORY_FREE/free, discount_25→discount, equipment_good→CATEGORY_EQUIPMENT_CONDITION 等)
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
                    //game 0.46D: ItemFeatureList.ModuleStuckFeature 已移除 (module_stuck 现为 ItemCondition, 见 ItemConditionList.CreateModuleStuck) — 本方法返回 ItemFeature, 故走裸建兜底.
                    case "discount_25": return ItemFeatureList.Discount(25);
                    case "bargain_markup":
                    case "bargainMarkup": return ItemFeatureList.BargainMarkup(10);
                    case "bargain_discount":
                    case "bargainBuyingDiscount": return ItemFeatureList.BargainBuyingDiscount(10);
                    case "equipment_good":
                    case "CATEGORY_EQUIPMENT_CONDITION": return ItemFeatureList.EquipementConditionFeature((ItemFeatureList.EquipmentCondition)2);
                }
            }
            catch
            {
            }
            return null;
        }

        // 物品全字段 dump (基础 + 标签 base/modified + 特性含条件)
        private static object DumpItem(GameItem item)
        {
            if (item == null) return null;
            var tags = new System.Collections.Generic.List<object>();
            var tagsMod = new System.Collections.Generic.List<object>();
            AppendTags(tags, item.state);
            AppendTags(tagsMod, item.modifiedState);
            var feats = DumpFeatures(item);
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

        // 标签字典 → JSON 对象列表 (base / modified 两套 system 共用)
        private static void AppendTags(System.Collections.Generic.List<object> into, TagSystem ts)
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
            catch
            {
            }
        }

        // 物品特性列表 → JSON 对象列表 (含 fake/real 条件)
        private static System.Collections.Generic.List<object> DumpFeatures(GameItem item)
        {
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
            catch
            {
            }
            return feats;
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

        // mod 引擎: 命名表键 -> TableMaster const 取值器 (键集合唯一真源, 未知键 = 不在表中)
        private static readonly System.Collections.Generic.Dictionary<string, Func<string>> NamedTableIds =
            new System.Collections.Generic.Dictionary<string, Func<string>>(StringComparer.Ordinal)
        {
            { "junk", () => TableMaster.junkTable },
            { "access_card", () => TableMaster.accessCardTable },
            { "all_module", () => TableMaster.allModuleTable },
            { "makeshift_weapon", () => TableMaster.makeshiftWeaponTable },
            { "material", () => TableMaster.materialTable },
            { "household", () => TableMaster.householdTable },
            { "packed_food", () => TableMaster.packedFoodTable },
            { "t1module", () => TableMaster.t1moduleTable },
            { "t2module", () => TableMaster.t2moduleTable },
            { "tool", () => TableMaster.toolTable },
            { "medical", () => TableMaster.medicalTable },
        };

        // mod 引擎: 命名表 -> 游戏表 ID (TableMaster const 优先, 同名 ID 回退; 未知键返回 "")
        private static string ResolveNamedTableId(string tableKey)
        {
            if (tableKey == null) return ""; // 与 switch 版一致: null 键返回空, 不抛 ArgumentNullException
            if (!NamedTableIds.TryGetValue(tableKey, out Func<string> resolve)) return "";
            try
            {
                if (TableMaster.Instance != null)
                {
                    string text = resolve();
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }
 catch { }
            return tableKey;
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

        //============ 机器进度强制满 ============ 游戏在存档结算时对每台进行中机器调用 ContinueProgressTypeMachine 推进进度。 前缀: 详细日志 + 强制完成。
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

                    var tgtTag = machine.GetTagReadonly("MACHINE_PROGRESS_TARGET_TAG");
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

        //============ 机器推进 hook (真推进点) ============ MachineryHelper.UpdateProcessingTypeMachine 是机器处理推进核心, 结算时对进行中机器调用。前缀: 设满 CURRENT (若已存在) 并打日志 (只对真机器)。
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

        //============ 不消耗耐久 ============ 注: 不再 patch ModuleEffectHelper.Degrade —— 游戏 0.46D 热修后该类静态构造在 HarmonyInit 早期初始化崩溃(Il2Cpp SEH), 且 Degrade 会走 ChangeDurability 主入口, 此处已覆盖.
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

        // 净水器/净化器: PurifyToBaseWater -> 剥离全部杂质到零 (与紫外线灯同一条路径)
        [HarmonyPatch(typeof(WaterHelper), "PurifyToBaseWater")]
        public static class PatchPurifyToPure
        {
            public static bool Prefix(GameItem __0)
            {
                try
                {
                    if (!PurifyAlwaysPure) return true;
                    if (__0 == null) return false;
                    //原实现只有 EmptyContainer + AddPureWater(vol) 两步, 缺的正是最后一轮剥离: EmptyContainer 清的是容器里的「水」(基液), 把锈水/沟流水换成 100% 纯水靠它, 但 6 类杂质是各自独立的 part, 它一个都不碰 (其 IL 全程无杂质 id 字面量, 只有 ModifyTag 容器 parts 与 DisableTag("STRIP_TESTED_TAG"))。所以基液已是纯水、杂质却原样留下 —— 这就是净水器与紫外线灯表现不一致的原因 (后者不调 EmptyContainer, 只剥离)。三步齐全才能既不杂质又真纯水。
                    int vol = WaterHelper.GetTotalVolume(__0);
                    WaterHelper.EmptyContainer(__0);
                    WaterHelper.AddPureWater(__0, vol);
                    StripAllContaminants(__0);   // 逐类 RemoveContaminantFromContainer(minPercentage=0) 迭代到收敛 + 按需补满
                    return false; // 跳过原逻辑
                }
                catch (Exception e) { MelonLogger.Error($"[Water] PurifyToBaseWater patch err: {e.Message}"); return true; }
            }
        }

        //水龙头: 原版出水走 AddHighQualityWater (grade=1), 即「高品质水」而非纯水 —— grade 是水质类别(0纯水/1高品质/2基础/3幽灵/4锈/5沟流), 不是纯度百分比, 故原版水龙头设计上就接不出纯水. 出水调用点唯一: WaterHelper.InitLiquidContainerItem 的闭包 (NestedType___c__DisplayClass10_0) 里 AddWater(容器, 1, 0x98967F, true, Station[52], Station[52]); addAmount=9999999 这个魔数全 ISIL dump 仅两处 0x98967F, 都在该闭包内, 连同 grade==1 即水龙头指纹. 命中则 grade 改 0 并关掉 useRange(否则纯度仍按 low/high 区间随机), 接出即 100% 纯水; 其余 25 处 AddWater 调用不受影响. 参数用位置名而非真名: IL2CPP 下参数名可能缺失, 位置名不会静默绑不上. 托管签名 AddWater(GameItem, int grade, int addAmount, bool useRange, int low, int high, bool init) => __1=grade, __2=addAmount, __3=useRange.
        private const int FaucetWaterMarker = 9999999;

        [HarmonyPatch(typeof(WaterHelper), "AddWater")]
        public static class PatchFaucetPure
        {
            public static void Prefix(GameItem __0, ref int __1, int __2, ref bool __3)
            {
                try
                {
                    if (!FaucetAlwaysPure) return;
                    if (__1 != 1 || __2 != FaucetWaterMarker) return;   // 非水龙头出水
                    __1 = 0;        // grade 0 = 纯水
                    __3 = false;    // 关掉区间随机, 否则纯度仍按 low/high 浮动
                    //仅换 grade 不够: AddWater 是「追加混合」(遍历 LIQUID_CONTAINER_CAPACITY 加量), 只保证新加的那份是纯水, 容器原有杂质原封不动 —— 接水前残留多少就还是多少。故先剥离到零, 再由原生流程补入纯水; 不调 FillWithPureWater, 否则补满后原生无处可加。
                    StripContaminantsOnly(__0);
                }
                catch (Exception e) { MelonLogger.Error($"[Water] AddWater patch err: {e.Message}"); }
            }
        }

        //加强滤嘴: 滤嘴行为由 Liquid 表驱动 —— filterVolumeRemoved=每次剥离多少, filterDurabilityUsage=每次剥离扣多少耐久, filterMinimumPurity=每次剥离的水量阈值. 原表多数杂质只给 1000 / 2, 这里统一拉到游戏内实测最大值 2000 并把耐久成本压到 1 (配合 NoDurability 即自由过滤). 刻意不动 particleSize 与 FILTER_SIZE_TAG: HandleFilter 的分支是 filterSize<=particleSize 才处理, 滤嘴 FILTER_SIZE_TAG=0x16(22) 只拦得住 coarse 杂质; 调大反而会把能拦的变成跳过.
        private const int MaxFilterVolumeRemoved = 2000;
        private const int MinFilterDurabilityUsage = 1;
        private static bool _filterBoostDone;

        // 单条液态的规范化, 幂等。
        private static void ApplyFilterBoost(Liquid l)
        {
            if (l == null || !l.canBeRemovedByPurifier) return; // water 自身 canRemove=0, 天然跳过
            if (l.filterVolumeRemoved < MaxFilterVolumeRemoved) l.filterVolumeRemoved = MaxFilterVolumeRemoved;
            if (l.filterDurabilityUsage > MinFilterDurabilityUsage) l.filterDurabilityUsage = MinFilterDurabilityUsage;
        }

        //兜底: Liquid 表由 Liquid 静态构造里的 InitLiquid 建好。若该静态构造早于本 mod 打补丁执行, InitLiquid 的 Postfix 就永远不会再来, 路线 1 会静默变成空操作。 因此首次过滤时补做一次; 常态下只读一个 bool。
        private static void EnsureFilterBoost()
        {
            if (_filterBoostDone) return;
            Il2CppSystem.Collections.Generic.List<Liquid> all = Liquid.Liquids;
            if (all == null) return; // 表尚未就绪, 保持未完成以便下次重试
            foreach (Liquid l in all) ApplyFilterBoost(l);
            _filterBoostDone = true;
        }

        [HarmonyPatch(typeof(Liquid), "InitLiquid")]
        public static class PatchFilterBoost
        {
            public static void Postfix(Il2CppSystem.Collections.Generic.List<Liquid> __result)
            {
                try
                {
                    if (!FilterBoost || __result == null) return;
                    foreach (Liquid l in __result) ApplyFilterBoost(l);
                    _filterBoostDone = true;
                }
                catch (Exception e) { MelonLogger.Error($"[Water] InitLiquid patch err: {e.Message}"); }
            }
        }

        // 兜底入口: 过滤是这两个字段唯一的消费点, 保证首次过滤前一定已规范化。
        [HarmonyPatch(typeof(WaterHelper), "HandleFilter")]
        public static class PatchFilterBoostOnUse
        {
            public static void Prefix() { try { if (FilterBoost) EnsureFilterBoost(); } catch { /* IL2CPP 异常: 放弃本次兜底 */ } }
        }

        //全部 6 类杂质 part id (WaterPremadeHelper 三个分布字典的键即为权威集合). 供紫外线灯与净水器共用, 避免两处各写一份导致漂移.
        private static readonly string[] AllContaminants = new string[6]
        {
            "microbe", "physical_contaminant", "chemical_contaminant", "mineral", "heavy_metal", "organic_waste"
        };

        //净化到位: 清空全部杂质 + 按需补满 100% 纯水. 紫外线灯与净水器共用同一条路径。 原版 PurifyToBaseWater 是「部分净化」—— 它按 ci 索引逐槽写 min(c-remove, floor), 而 floor = c*water/total, 水不满时 floor 不为 0, 剩下的就是那点残余。故这里不依赖 原版的部分净化结果, 而是直接剥离到零再补满。 RemoveContaminantFromContainer(容器, 杂质id, 水量, minPercentage) 的写入端公式 (WaterHelper IL op078-119) 是: floor = waterAmount * minPercentage / 100 new   = max(0, max(contaminant - volumeToRemove, min(contaminant, floor))) 传 minPercentage=0 即 floor=0, volumeToRemove=总水量 => 单轮理论上归零. 但实测仍会留一点残余, 故此处迭代到收敛: 每轮用「当前」总量作 volumeToRemove 重算, 直到总水量不再下降为止 (残余存在时下一轮必然仍有量可剥). 收敛判据用总量而非纯度: GetWaterPurity 对非 LIQUID_CONTAINER_TAG 的容器直接返回 0, 拿它当判据会让不满足标签的容器白跑满 MaxStripPasses 轮。 只在主线程调用 (触达 native 字段).
        private const int MaxStripPasses = 8;

        //补满: GetFreeCapacity = LIQUID_CONTAINER_CAPACITY - GetTotalVolume, 补水用 AddPureWater(=AddWater grade 0, 即 100% 纯水)。容量按水的刻度算, 但 GetTotalVolume 把杂质体积也计入, 故先剥离再加: 顺序反了会按「含杂质的体积」补水, 反而溢出容量。 注意 free 取的是剥离之后的值。
        private static void FillWithPureWater(GameItem container)
        {
            int free = WaterHelper.GetFreeCapacity(container);
            if (free > 0) WaterHelper.AddPureWater(container, free);
            else if (free < 0) WaterHelper.EmptyContainer(container);   // 已超容量: 清空而非再加水
        }

        private static void StripAllContaminants(GameItem container)
        {
            StripContaminantsOnly(container);
            //剥离后按需补满 (顺序关键: 先剥离再取 free —— 容量按水的刻度算, 而 GetTotalVolume 把杂质体积也计入, 反了会按含杂质的体积补水而溢出容量)。
            if (PurifyFillToFull) FillWithPureWater(container);
        }

        //只剥离、不补满。水龙头出水走这一支: 剥离后原生 AddWater 本就按容量补入纯水, 若先 FillWithPureWater 补满, 后续就无处可加。
        private static void StripContaminantsOnly(GameItem container)
        {
            if (container == null) return;
            int vol = WaterHelper.GetTotalVolume(container);
            if (vol <= 0) return;
            for (int pass = 0; pass < MaxStripPasses; pass++)
            {
                foreach (string id in AllContaminants) WaterHelper.RemoveContaminantFromContainer(container, id, vol, 0);
                int after = WaterHelper.GetTotalVolume(container);
                //收敛判据: 本轮没剥掉任何东西。用容量而非 GetWaterPurity —— 后者对非 LIQUID_CONTAINER_TAG 的容器直接返回 0, 拿它当判据会让这类容器白跑满轮。
                if (after >= vol) break;
                vol = after;
            }
        }

        //紫外线灯: 原版 OnUVUsed 只把 microbe 归零并把其体积并回 water, 对其余 5 类杂质 (physical_contaminant / chemical_contaminant / mineral / heavy_metal / organic_waste) 完全不作为. Postfix 照 MachinePurifier.PurifyContainer 的做法补一轮移除.
        [HarmonyPatch(typeof(WaterHelper), "OnUVUsed")]
        public static class PatchUvFullPurify
        {
            public static void Postfix(GameItem __0)
            {
                try
                {
                    if (!UvFullPurify) return;
                    StripAllContaminants(__0);
                }
                catch (Exception e) { MelonLogger.Error($"[Water] OnUVUsed patch err: {e.Message}"); }
            }
        }

        //海德拉科技微型净水器 (portable_water_purifier, AmenitiesItemDirectory). 原版 OnPortableWaterPurifierUsed 开头即比对 GetWaterPurity() >= 9800 (0x2648), 满足则弹 "mech_portable_purifier_already_clean" 并整段跳过 PurifyToBaseWater. 该纯度 = waterVolume*10000/总容量, 分母含全部杂质, 故 9800 等价于「杂质占比 <= 2%」—— 也就是说只差最后一点点的水反而不给净化, 微量杂质会一直留在容器里。 PatchPurifyToPure 挂在 PurifyToBaseWater 上, 根本不会被这条早退分支触达, 故在此补一刀: Postfix 在原生流程结束后清空残余杂质; 原版的收费(ChangeDurability)与提示文案保持不动.
        [HarmonyPatch(typeof(AmenitiesItemDirectory), "OnPortableWaterPurifierUsed")]
        public static class PatchPurifierFullPurify
        {
            public static void Postfix(GameItem __1)
            {
                try
                {
                    if (!PurifierFullPurify) return;
                    StripAllContaminants(__1);   // __1 = container (__0 = purifier)
                }
                catch (Exception e) { MelonLogger.Error($"[Water] OnPortableWaterPurifierUsed patch err: {e.Message}"); }
            }
        }

        //============ 模块加成强化 ============ ModuleHelper.InitModuleItem 是所有模块(含神经模组)初始化入口, 3 个加成百分比直接乘大。模块创建时调用一次。
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


        //============ 永不受伤: 拾荒/战斗/深夜伤口全消毒 ============ ScavHelper 负责拾荒受伤掷骰; HealthData 负责伤口结算与恶化. 统一策略: bool 返回 Postfix 强制 false/0, void 结算 Prefix 跳过.
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

        //============ 无限拾荒: 次数与冷却不受限 ============ Cpp2IL ISIL 静态分析 (ScavHelper.txt): CanScavenge: 内联计算 base(5/7 - IsProficientScavenger) - 折算used - PlayerStore[+536](今日计数), 剩余<=0 返回 false GetMaxScavAttempts/GetScavTimeLeft: 同样内联计算 (不互相调用) ResetScavenging: PlayerStore[+536] = 0 核心限制点是 CanScavenge (UI/逻辑都问它), patch 它返回 true 即可; GetMaxScavAttempts/GetScavTimeLeft 也 patch 9999 (其他调用方可能直接读).
        [HarmonyPatch(typeof(ScavHelper), "CanScavenge")]
        public static class PatchCanScavenge
        {
 public static void Postfix(ref bool __result) { try { if (InfiniteScavenging) __result = true; } catch { } }
        }
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
