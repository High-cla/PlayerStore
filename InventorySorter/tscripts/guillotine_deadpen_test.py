# -*- coding: utf-8 -*-
"""Guillotine + 碎片惩罚项(修正版) 验证.
死洞定义: 切割产生的每个新碎片, 若放不下任何【剩余】物品(考虑旋转), 则该碎片面积计入死洞.
评分 = waste + α × 死洞面积. 真实数据: 有1x1物品109个, 故宽1碎片仍可被1x1利用, 仅"完全无用碎片"才算洞.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump_merged
import verify_all as va

ALPHA = 0.1

def fits_in(gw, gh, rw, rh):
    """物品 (gw,gh) 能放进碎片 (rw,rh), 考虑旋转."""
    return (gw <= rw and gh <= rh) or (gh <= rw and gw <= rh)

def dead_area(newrects, remaining_rots):
    """新碎片中放不下任何剩余物品的碎片面积和."""
    dead = 0
    for rw, rh in newrects:
        if rw <= 0 or rh <= 0:
            continue
        if not any(fits_in(gw, gh, rw, rh) for (gw, gh) in remaining_rots):
            dead += rw * rh
    return dead

def guillotine_dp(items, W, H, alpha=ALPHA):
    """GuillotineCut + 死洞惩罚评分."""
    freerects = [(0, 0, W, H)]
    occ = set()
    order = sorted(items, key=lambda i: -len(i["cells"]))
    # 预计算每物品旋转后的包围盒集合 (去重)
    shape_keys = []
    for it in order:
        rots = []
        for key, nw, nh in va.rotations_of(it):
            rots.append((nw, nh))
        shape_keys.append(rots)
    for idx, it in enumerate(order):
        remaining_rots = [r for rl in shape_keys[idx + 1:] for r in rl]
        best = None  # (fi, key, px, py, gw, gh, score)
        for fi, (frx, fry, frw, frh) in enumerate(freerects):
            for key, nw, nh in va.rotations_of(it):
                if nw > frw or nh > frh:
                    continue
                waste = frw * frh - nw * nh
                right = frw - nw
                below = frh - nh
                newrects = []
                if below > 0:
                    newrects.append((frw, below))
                if right > 0:
                    newrects.append((right, frh))
                dead = dead_area(newrects, remaining_rots)
                score = waste + alpha * dead
                if best is None or score < best[6] or (score == best[6] and nw > best[4]):
                    best = (fi, key, frx, fry, nw, nh, score)
        if best is None:
            return None
        fi, key, px, py, gw, gh, score = best
        occ |= {(px + dx, py + dy) for dx, dy in key}
        frx, fry, frw, frh = freerects[fi]
        right = frw - gw
        below = frh - gh
        newrects = []
        if below > 0:
            newrects.append((frx, fry + gh, frw, below))
        if right > 0:
            newrects.append((frx + gw, fry, right, frh))
        freerects.pop(fi)
        freerects.extend(newrects)
        freerects = dedup_rects(freerects)
    return occ

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
    from guillotine_test import guillotine_pack
    sessions = parse_dump_merged()
    n = 0
    tot_a, tot_b = 0, 0
    win_a, win_b, tie = 0, 0, 0
    for w, h, merged in sessions:
        items = [rep for rep, _ in merged]
        total = sum(len(i["cells"]) for i in items)
        if total == 0 or total > w * h:
            continue
        n += 1
        oc_a = guillotine_dp(items, w, h)
        oc_b = guillotine_pack(items, w, h)
        a_a = va.max_empty(oc_a, w, h)[0] if oc_a else -1
        a_b = va.max_empty(oc_b, w, h)[0] if oc_b else -1
        tot_a += max(a_a, 0)
        tot_b += max(a_b, 0)
        if a_a > a_b:
            win_a += 1
        elif a_b > a_a:
            win_b += 1
        else:
            tie += 1
    print(f"组数: {n}")
    print(f"DP(惩罚) 总空矩: {tot_a}   胜: {win_a}")
    print(f"Base(无)  总空矩: {tot_b}   胜: {win_b}")
    print(f"平: {tie}   差: {tot_a - tot_b} ({'DP优' if tot_a>tot_b else 'Base优'})")
