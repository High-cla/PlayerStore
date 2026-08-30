# -*- coding: utf-8 -*-
"""最优组合分析: 遍历全部会话组, 统计每算法胜出次数, 判断 LayoutDense 候选是否冗余
用法: python optimal_combo.py [dump路径]
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump
import verify_all as va

def run():
    sessions = parse_dump()
    # 每 (w,h) 取格子最多的组(与 verify_all 一致)
    best_by_key = {}
    for w, h, items in sessions:
        total = sum(len(i["cells"]) for i in items)
        if total == 0:
            continue
        data = (w, h, items, total)
        if (w, h) not in best_by_key or total > best_by_key[(w, h)][3]:
            best_by_key[(w, h)] = data
    wins = {k: 0 for k in va.ALGOS}
    tie_any = 0
    per_group = []
    for w, h, items, total in best_by_key.values():
        va.W, va.H = w, h
        results = {}
        for name, fn in va.ALGOS.items():
            try:
                occ = fn(items, w, h)
                a, _ = va.max_empty(occ, w, h)
                results[name] = a if occ else -1
            except Exception:
                results[name] = -2
        if not results:
            continue
        # 排除失败/异常, 找最大值
        valid = {k: v for k, v in results.items() if v >= 0}
        if not valid:
            continue
        top = max(valid.values())
        winners = [k for k, v in results.items() if v == top]
        # 记录
        for wk in winners:
            wins[wk] += 1
        if len(winners) > 1:
            tie_any += 1
        per_group.append((w, h, total, winners, results))
        print(f"{w}x{h} total={total} 胜者={winners}")
    print("\n=== 胜出次数统计(并列都计) ===")
    for k in sorted(va.ALGOS, key=lambda x: -wins[x]):
        print(f"  {k:15s} 胜出 {wins[k]}")
    print(f"  含并列的组数: {tie_any} / {len(per_group)}")

if __name__ == "__main__":
    run()
