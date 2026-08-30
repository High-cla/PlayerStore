# -*- coding: utf-8 -*-
"""新旧组合耗时基准: 判断原生加速(Rust)是否必要.
新组合 = MinHole + GrowTouch + Shelf (C# 已采纳, 零损失)
旧组合 = PairGrounded + GreedyBottom + MinHole (C# 原候选, 损失 11.6%)
"""
import sys, os, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump
import verify_all as va

def timed(fn, items, w, h):
    t0 = time.perf_counter()
    try:
        occ = fn(items, w, h)
        result = va.max_empty(occ, w, h)[0] if occ else -1
    except Exception:
        return -2, time.perf_counter() - t0
    return result, time.perf_counter() - t0

def main():
    sessions = parse_dump()
    best_by_key = {}
    for w, h, items in sessions:
        total = sum(len(i['cells']) for i in items)
        if total == 0:
            continue
        if (w, h) not in best_by_key or total > best_by_key[(w, h)][3]:
            best_by_key[(w, h)] = (w, h, items, total)

    old_algos = {"PairGrounded": va.ALGOS["PairGrounded"],
                 "GreedyBottom": va.ALGOS["GreedyBottom"],
                 "MinHole": va.ALGOS["MinHole"]}
    new_algos = {"MinHole": va.ALGOS["MinHole"],
                 "GrowTouch": va.ALGOS["GrowTouch"],
                 "Shelf": va.ALGOS["Shelf"]}

    old_t = new_t = 0.0
    old_best = new_best = 0.0
    rows = []
    for w, h, items, total in best_by_key.values():
        va.W, va.H = w, h
        # 旧组合总耗时 = 三算法之和(模拟 C# 串行跑)
        o_b, o_t = (0, 0.0)
        for name, fn in old_algos.items():
            b, t = timed(fn, items, w, h)
            if b > o_b:
                o_b = b
            o_t += t
        n_b, n_t = (0, 0.0)
        for name, fn in new_algos.items():
            b, t = timed(fn, items, w, h)
            if b > n_b:
                n_b = b
            n_t += t
        old_t += o_t
        new_t += n_t
        old_best += max(o_b, 0)
        new_best += max(n_b, 0)
        rows.append((f"{w}x{h}", total, o_b, n_b, o_t, n_t))
        print(f"{w}x{h:<4} cell={total:<4} 旧={(o_b):4d}@{o_t:.4f}s 新={(n_b):4d}@{n_t:.4f}s")

    print(f"\n=== 合计 ===")
    print(f"旧组合: 空矩总计={old_best:.0f}  耗时={old_t:.4f}s")
    print(f"新组合: 空矩总计={new_best:.0f}  耗时={new_t:.4f}s")
    print(f"新组合空矩提升: {new_best-old_best:.0f}  耗时变化: {new_t-old_t:+.4f}s")
    print(f"备注: Python 纯模拟耗时, C# 更快; 背包<=24x10 网格小, 布局在 OnGUI 0.3s 缓存外触发的时机无关紧要.")

if __name__ == "__main__":
    main()
