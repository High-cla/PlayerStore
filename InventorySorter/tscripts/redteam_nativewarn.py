"""redteam_nativewarn.py -- 红队独立复核 NativeWarn 去重 (Core.cs:283-297).

修复: 原空函数体 -> 按 `ex.GetType().FullName + ": " + ex.Message` 签名去重后
      MelonLogger.Warning。静态字段 `_nativeWarnSig`。

验证:
  A) 误抑制: 不同异常类型但同消息 / 同类型不同消息 / 同类型同消息 —— 签名是否
     能把「不同异常」区分开。重点: **同类型同消息但不同 InnerException/StackTrace**
     会被去重抑制 —— 判定这是否构成误抑制(设计取舍 vs 缺陷)。
  B) 刷屏: 模拟 OnUpdate 每 0.25s tick 持续抛同一异常, 统计 60s 内日志条数。
  C) 线程安全: 静态字段读写在 C# 里对 string 引用是原子的, 但 check-then-set
     非原子。检查本仓库是否存在非主线程调用路径。
  D) 空 catch 约定: 仓库里空 catch 是否都带 `// ponytail:` —— 本处改为有日志,
     是否仍需注释。
"""
import re
import sys

fail = []


def check(cond, msg):
    if not cond:
        fail.append(msg)
        print("  FAIL " + msg)


def sig(t, msg):
    return t + ": " + msg


class WarnState:
    """镜像 Core.cs:285-303 + 调用方成功轮次清零(Core.cs:273-278)。"""

    def __init__(self):
        self.cache = None
        self.prints = 0

    def warn(self, t, msg):
        s = sig(t, msg)
        if s == self.cache:
            return False
        self.cache = s
        self.prints += 1
        return True

    def success_tick(self):
        # 仅当 RefreshNativeUI+TrackOpenedContainers 都未抛异常时才执行
        self.cache = None


def test_mis_suppression():
    print("[A] 去重签名判别力")
    # 不同异常类型, 同消息 -> 签名不同 => 都会打印 (正确)
    s1 = sig("System.NullReferenceException", "Object reference not set")
    s2 = sig("System.InvalidOperationException", "Object reference not set")
    check(s1 != s2, "不同类型同消息被误判为同一签名")
    print("  不同类型同消息 -> 签名不同: OK")
    # 同类型不同消息 -> 签名不同 => 都会打印 (正确)
    check(sig("System.Exception", "a") != sig("System.Exception", "b"), "同类型不同消息被误判")
    print("  同类型不同消息 -> 签名不同: OK")
    # 同类型同消息 -> 抑制 (设计意图)
    check(sig("System.Exception", "a") == sig("System.Exception", "a"), "同签名未抑制")
    print("  同类型同消息 -> 抑制: OK (设计意图)")
    # 同类型同消息但不同 StackTrace/InnerException -> 被抑制
    # 注意: ex.Message 可能含具体数值(如 index), 从而区分不同实例;
    # 但纯消息相同的两处不同异常会被合并。
    print("  已知取舍: 同类型+同 Message 但不同 StackTrace/InnerException 会被抑制")
    print("    -> 判定: 设计取舍(防刷屏), 非缺陷; 但会掩盖『同一消息来自不同位置』的第二个错误")


def test_log_flood():
    print("[B] 0.25s tick 刷屏 / 去重语义")
    ticks = int(60 / 0.25)  # 60 秒
    # [B1] 持续同类异常: 只有第一条打印
    st = WarnState()
    for _ in range(ticks):
        st.warn("System.NullReferenceException", "Object reference not set")
    print("  [B1] 60s 持续同一异常 -> 打印 %d 条 (修复前空体: 0 条)" % st.prints)
    check(st.prints == 1, "持续同类异常仍刷屏 (%d 条)" % st.prints)

    # [B2] 两种异常每 tick 交替(期间无成功轮) -> 每 tick 1 条 (残余风险, reset 不影响)
    st = WarnState()
    for i in range(ticks):
        st.warn("System.Exception", "A" if i % 2 == 0 else "B")
    print("  [B2] 60s 两种异常交替(无成功轮) -> 打印 %d 条 (最坏情形, 与 reset 无关)" % st.prints)
    check(st.prints == ticks, "交替异常条数异常")

    # [B3] 恢复后复发: 成功轮次清零后应再次打印
    st = WarnState()
    events = []
    for _ in range(4):  # 容器 A 打开, 持续抛
        events.append("print" if st.warn("System.NullReferenceException", "Object reference not set") else "suppress")
    for _ in range(4):  # A 关闭, 恢复正常(成功轮次)
        st.success_tick()
        events.append("ok")
    for _ in range(4):  # A 再次打开, 同一异常复发
        events.append("print" if st.warn("System.NullReferenceException", "Object reference not set") else "suppress")
    n_print = events.count("print")
    print("  [B3] 恢复后复发时序: %s" % events)
    print("       复发共 2 轮, 实际打印 %d 次" % n_print)
    check(n_print == 2, "恢复后复发被静默抑制: 第 2 轮未打印")

    # [B4] 新增风险: 失败/成功每 tick 交替(同一异常) —— reset 引入了新的刷屏路径
    st_before, st_after = None, WarnState()
    before_prints = 1  # 无 reset: 全程只打印首条
    for i in range(ticks):
        if i % 2 == 0:
            st_after.warn("System.NullReferenceException", "Object reference not set")
        else:
            st_after.success_tick()
    print("  [B4] 60s 失败/成功每 tick 交替(同一异常) -> 无reset %d 条 / 有reset %d 条"
          % (before_prints, st_after.prints))
    if st_after.prints > 100:
        print("       ⚠ reset 使「间歇性故障(每成功轮后复发)」由 1 条变 %d 条/分 —— 残余风险" % st_after.prints)


def test_thread_safety():
    print("[C] 线程安全 / 主线程假设")
    src = open("D:/git/PlayerStore/InventorySorter/InventorySorter/Core.cs",
               encoding="utf-8").read()
    # 查找线程 / Task / async / Harmony 补丁
    pats = ["Thread", "Task.Run", "async ", "HarmonyPatch", "new Thread", "BackgroundWorker"]
    found = {p: len(re.findall(re.escape(p), src)) for p in pats}
    print("  源码中线程相关构造计数: %s" % found)
    nonmain = sum(v for k, v in found.items() if k in ("Thread", "Task.Run", "async ", "new Thread", "BackgroundWorker"))
    check(nonmain == 0, "存在非主线程构造 => 静态签名缓存的『主线程假设』需重新论证")
    print("  未发现自建线程/Task => OnUpdate 与 NativeWarn 均在 Unity 主线程调用, 假设成立")
    print("  注: check-then-set 非原子, 但 string 引用读写在 C# 中原子, 单线程下无竞争")


def test_empty_catch_convention():
    print("[D] 空 catch 注释约定")
    src = open("D:/git/PlayerStore/InventorySorter/InventorySorter/Core.cs",
               encoding="utf-8").read().splitlines()
    # 找空 catch 块: catch 行后紧跟 } (可能夹注释)
    empties = []
    for i, l in enumerate(src):
        if not re.search(r"catch\b", l.strip()):
            continue
        j = i + 1
        if j < len(src) and src[j].strip() == "{":
            j += 1
        while j < len(src) and src[j].strip().startswith("//"):
            j += 1
        if j < len(src) and src[j].strip() == "}":
            has_pony = any("ponytail" in src[k] for k in range(i + 1, j + 1))
            empties.append((i + 1, has_pony, src[i].strip()))
    for ln, has, code in empties:
        print("  L%d %s ponytail=%s" % (ln, code, has))
    missing = [ln for ln, has, _ in empties if not has]
    print("  空 catch 共 %d 处, 缺 ponytail 注释 %d 处: %s" % (len(empties), len(missing), missing))
    # 本处 NativeWarn 不是空 catch(有日志), 不适用
    print("  本次 NativeWarn 已从空体改为带日志 => 不再属于空 catch 约定适用范围")


if __name__ == "__main__":
    test_mis_suppression()
    test_log_flood()
    test_thread_safety()
    test_empty_catch_convention()
    print()
    if fail:
        print("RESULT: FAIL (%d)" % len(fail))
        for f in fail:
            print("  - " + f)
        sys.exit(1)
    print("RESULT: PASS")
