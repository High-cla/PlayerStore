# -*- coding: utf-8 -*-
"""动态 Guillotine 切割(GuillotineCut) - BestAreaFit 评分策略.
Free rects 池: 初始 = 整个背包. 每次放物品选 waste 最小的 free rect, 放置后按割线切分为碎片.
对比 Shelf/CurCombo5, 验证是否值得替换 Shelf.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump_merged
import verify_all as va

def guillotine_pack(items, W, H, score="area"):
    """GuillotineCut: free rects 池 + 每次选 waste 最小候选 + 按割线切碎."""
    freerects = [(0, 0, W, H)]  # (x, y, w, h)
    occ = set()
    order = sorted(items, key=lambda i: -len(i["cells"]))
    for it in order:
        best = None  # (free_idx, key, px, py, gw, gh, waste)
        for fi, (frx, fry, frw, frh) in enumerate(freerects):
            for key, nw, nh in va.rotations_of(it):
                if nw > frw or nh > frh:
                    continue
                waste = frw * frh - nw * nh
                if best is None or waste < best[6] or (waste == best[6] and nw > best[4]):
                    best = (fi, key, frx, fry, nw, nh, waste)
        if best is None:
            return None
        fi, key, px, py, gw, gh, waste = best
        occ |= {(px + dx, py + dy) for dx, dy in key}
        frx, fry, frw, frh = freerects[fi]
        right = frw - gw
        below = frh - gh
        newrects = []
        # 水平割: 下碎片(frx,fry+gh,frw,below) + 右碎片(frx+gw,fry,right,frh)
        if below > 0:
            newrects.append((frx, fry + gh, frw, below))
        if right > 0:
            newrects.append((frx + gw, fry, right, frh))
        freerects.pop(fi)
        freerects.extend(newrects)
        freerects = dedup_rects(freerects)
    return occ

def freq_waste_tie(best, nw, nh):
    return True

def dedup_rects(rects):
    kept = []
    for r in rects:
        if r[2] <= 0 or r[3] <= 0:
            continue
        dup = False
        for o in rects:
            if r is o:
                continue
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                dup = True
                break
        if not dup:
            kept.append(r)
    return kept

if __name__ == "__main__":
    # 快速测试 + 对比 Shelf
    print("=== Guillotine vs Shelf vs CurCombo5 (同类合并数据) ===")
    from verify_all import ALGOS
    sessions = parse_dump_merged()
    combos = {
        "Shelf": ALGOS["Shelf"],
        "Guillotine": guillotine_pack,
        "Cur": ALGOS["Shelf"],  # 占位, 下面重算
    }
    tot_g, tot_s, tot_c = 0, 0, 0
    win_g, win_s = 0, 0
    n = 0
    for w, h, merged in sessions:
        items = [rep for rep, _ in merged]
        total = sum(len(i["cells"]) for i in items)
        if total == 0 or total > w * h:
            continue
        va.W, va.H = w, h
        n += 1
        oc_g = guillotine_pack(items, w, h)
        oc_s = ALGOS["Shelf"](items, w, h)
        a_g = va.max_empty(oc_g, w, h)[0] if oc_g else -1
        a_s = va.max_empty(oc_s, w, h)[0] if oc_s else -1
        # CurCombo5 最优(单独算太重, 取 min(a_g, a_s) + 之前CurCombo5数据近似)
        if a_g >= a_s:
            win_g += 1
        else:
            win_s += 1
        tot_g += max(a_g, 0)
        tot_s += max(a_s, 0)
    print(f"组数: {n}")
    print(f"Guillotine 总空矩: {tot_g}  胜 Shelf: {win_g} 组")
    print(f"Shelf 总空矩: {tot_s}  胜 Guillotine: {win_s} 组")
    print(f"Guillotine vs Shelf 差: {tot_g - tot_s} ({'Guillotine优' if tot_g>tot_s else 'Shelf优'})")
