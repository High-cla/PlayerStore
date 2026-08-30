# -*- coding: utf-8 -*-
"""全算法库扫描(去重数据 126 组, 逐组): 找出能击败当前组合(MinHole+GrowTouch+Shelf)的算法。
当前组合已在 C# 实现, 此脚本验证它在更大去重数据集上是否仍零损失,
并测试候选新算法(可移植策略)是否有增益场景。
"""
import sys, os, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump, parse_dump_merged
import verify_all as va
import guillotine_test

# 候选算法的通用适配器 (统一 items, W, H 签名)
def pack_left_bottom(items, W, H):
    items = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    for it in items:
        best = None
        for key, nw, nh in va.rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if va.can_place(occ, cells, px, py, W, H):
                        if best is None or px < best[0] or (px == best[0] and py > best[1]):
                            best = (px, py, cells)
        if best:
            occ = va.mark(occ, best[2], best[0], best[1])
        else:
            return None
    return occ

def pack_top_left(items, W, H):
    items = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    for it in items:
        best = None
        for key, nw, nh in va.rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if va.can_place(occ, cells, px, py, W, H):
                        if best is None or py < best[1] or (py == best[1] and px < best[0]):
                            best = (px, py, cells)
        if best:
            occ = va.mark(occ, best[2], best[0], best[1])
        else:
            return None
    return occ

def pack_core_bestfit(items, W, H):
    items = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    for it in items:
        rots = va.rotations_of(it)
        best = None; bestwaste = None
        for key, nw, nh in rots:
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if va.can_place(occ, cells, px, py, W, H):
                        waste = nw * nh - len(key)
                        if bestwaste is None or waste < bestwaste or (waste == bestwaste and (py < best[1] or (py == best[1] and px < best[0]))):
                            bestwaste = waste; best = (px, py, cells)
        if best:
            occ = va.mark(occ, best[2], best[0], best[1])
        else:
            return None
    return occ

# 双组合(两个布局器取 maxempty 更大者)
def combo2(f1, f2):
    def fn(items, W, H):
        a = f1(items, W, H) if f1 else None
        b = f2(items, W, H) if f2 else None
        res = []
        for occ in (a, b):
            if occ:
                res.append((va.max_empty(occ, W, H)[0], occ))
        if not res:
            return None
        res.sort(key=lambda x: -x[0])
        return res[0][1]
    return fn

ALGOS = {
    "MinHole": va.ALGOS["MinHole"],
    "GrowTouch": va.ALGOS["GrowTouch"],
    "Shelf": va.ALGOS["Shelf"],
    "BestFitMFR": va.ALGOS["BestFitMFR"],
    "Rows": va.ALGOS["Rows"],
    "PairGrounded": va.ALGOS["PairGrounded"],
    "GreedyBottom": va.ALGOS["GreedyBottom"],
    "LeftBottom": pack_left_bottom,
    "TopLeft": pack_top_left,
    "CoreBestFit": pack_core_bestfit,
    "CurCombo": combo2(va.ALGOS["MinHole"], combo2(va.ALGOS["GrowTouch"], va.ALGOS["Shelf"])),
    "CurCombo5": combo2(
        va.ALGOS["MinHole"],
        combo2(combo2(va.ALGOS["GrowTouch"], va.ALGOS["Shelf"]),
               combo2(pack_left_bottom, va.ALGOS["BestFitMFR"])),
    ),
    # V2: Shelf→Guillotine 替换后组合
    "CurComboV2": combo2(
        va.ALGOS["MinHole"],
        combo2(combo2(va.ALGOS["GrowTouch"], guillotine_test.guillotine_pack),
               combo2(pack_left_bottom, va.ALGOS["BestFitMFR"])),
    ),
}

def run():
    # 用同类合并后的数据: 模拟 C# 端"先合并后布局"(mergeRepIdx 剔除吸收件, 只布局代表格)
    sessions = parse_dump_merged()
    # 全部逐组(已去重, 无重复), 跳过容量不足
    groups = []
    for w, h, merged in sessions:
        items = [rep for rep, _ in merged]
        total = sum(len(i["cells"]) for i in items)
        if total == 0 or total > w * h:
            continue
        groups.append((w, h, items, total))
    print(f"参与验证组: {len(groups)} (已去重+同类合并, 跳过容量不足)")
    wins = {k: 0 for k in ALGOS}
    total_best = {k: 0 for k in ALGOS}
    # 逐组跑
    for w, h, items, total in groups:
        va.W, va.H = w, h
        res = {}
        for name, fn in ALGOS.items():
            try:
                occ = fn(items, w, h)
                res[name] = va.max_empty(occ, w, h)[0] if occ else -1
            except Exception:
                res[name] = -2
        valid = {k: v for k, v in res.items() if v >= 0}
        if not valid:
            continue
        top = max(valid.values())
        for k, v in res.items():
            if v == top and v >= 0:
                wins[k] += 1
        for k, v in res.items():
            if v >= 0:
                total_best[k] += v
    print("\n=== 各算法(逐组获胜次数 + 空矩总计) ===")
    for k in sorted(ALGOS, key=lambda x: (-wins[x], -total_best[x])):
        print(f"  {k:14s} 胜 {wins[k]:4d}  空矩总计 {int(total_best[k]):6d}")
    # 当前组合 vs 单算法
    cur = total_best["CurCombo"]
    best_single = max((v for k, v in total_best.items() if k != "CurCombo"), default=0)
    best_single_name = max(((k, v) for k, v in total_best.items() if k != "CurCombo"), key=lambda x: x[1])
    print(f"\n当前组合(MinHole+GrowTouch+Shelf): {int(cur)}")
    print(f"最强单算法 {best_single_name[0]}: {int(best_single_name[1])}")
    print(f"组合 vs 最强单算法: {int(cur) - int(best_single_name[1])} {('组合优' if cur > best_single_name[1] else '单算法优')}")
    return wins, total_best, groups

if __name__ == "__main__":
    wins, total_best, groups = run()
    # 找出 CurCombo 不占优的组(漏网)
    print("\n=== 当前组合不占优的组(漏网分析) ===")
    for w, h, items, total in groups:
        va.W, va.H = w, h
        res = {}
        for name, fn in ALGOS.items():
            try:
                occ = fn(items, w, h)
                res[name] = va.max_empty(occ, w, h)[0] if occ else -1
            except Exception:
                res[name] = -2
        if res.get("CurCombo", -1) >= 0:
            cur = res["CurCombo"]
            better = {k: v for k, v in res.items() if k != "CurCombo" and v > cur}
            if better:
                print(f"  {w}x{h} total={total} 组合={cur} 更优算法={better}")
