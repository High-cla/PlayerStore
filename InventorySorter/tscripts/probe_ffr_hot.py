# -*- coding: utf-8 -*-
"""量化 FindFreeRects 的调用热度: 真实 dump 回放 LayoutBanded 路径, 统计
   ① 调用总次数 ② 其中落在逐件循环内的次数(rescan/fallback 重算)
   ③ 每次分配的 int[W]+int[W+1] 元素总量(即 C# 侧 GC 压力代理量)。

对拍底座 = bench_banded.layout_banded(全开关: support/rescan/fallback), 即 Core.cs
LayoutBanded 的忠实镜像。只计数, 不改语义。

判据: 若「循环内重算次数 / 总调用」比例高且绝对量可观 ⇒ 缓冲复用有收益;
      若重算次数近乎为 0 ⇒ 该优化无实际收益, 应放弃(避免无谓改动)。
"""
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bench_banded as bb          # noqa: E402
import bench_native as bn          # noqa: E402

stats = {"calls": 0, "loop_rescan": 0, "loop_fallback": 0, "elems": 0, "rects_seen": 0, "max_rects": 0}
_orig = bn.find_free_rects


def counting(occ, W, H):
    r = _orig(occ, W, H)
    stats["calls"] += 1
    stats["elems"] += 2 * W + 1          # height[W] + stack[W+1]
    stats["rects_seen"] += len(r)
    if len(r) > stats["max_rects"]:
        stats["max_rects"] = len(r)
    return r


def counting_loop(occ, W, H):
    stats["loop_rescan"] += 1
    return counting(occ, W, H)


bb.bn.find_free_rects = counting

# 打桩 layout_banded 内部两处循环内重算(bench_banded.py:339 / :351)
src = open(os.path.join(HERE, "bench_banded.py"), encoding="utf-8").read()


def run(sessions, label, rescan=True, fallback=True):
    global stats
    stats = {"calls": 0, "loop_rescan": 0, "loop_fallback": 0, "elems": 0, "rects_seen": 0, "max_rects": 0}
    ok = 0
    for (W, H, items) in sessions:
        rot = [bn.rots_of(i) for i in items]
        if bb.layout_banded(rot, items, W, H, support=bb.has_support_self,
                            rescan=rescan, fallback=fallback) is not None:
            ok += 1
    n = len(sessions)
    print(f"[{label}] sessions={n} grouped_ok={ok}")
    print(f"  FindFreeRects 调用 {stats['calls']} 次 / 平均 {stats['calls']/max(1,n):.2f} 次每会话"
          f" / 平均 {stats['calls']/max(1,ok):.2f} 次每成功会话")
    print(f"  数组元素分配总量 = {stats['elems']} (int), 平均每次调用 {stats['elems']/max(1,stats['calls']):.1f} 元素")
    print(f"  池规模: 平均 {stats['rects_seen']/max(1,stats['calls']):.1f} 矩形, 峰值 {stats['max_rects']}")
    return stats, ok


d = bb.dump_sessions()
print("=== 真实 dump 回放（LayoutBanded 全开关）===")
run(d, "real dump")

print()
print("=== catalog 合成组 ===")
s = bb.synth_sessions()
run(s, "synth")

print()
print("=== 判据 ===")
print("  FindFreeRects 每次调用分配 int[W]+int[W+1] (C#: Core.cs:3034-3035)")
print("  另加去包含段 int[n]+long[n]+Array.Sort 比较器闭包 (C#: Core.cs:3066-3076)")
