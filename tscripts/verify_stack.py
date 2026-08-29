# -*- coding: utf-8 -*-
"""堆叠可见性模拟: 堆叠物品(unitCount>1)可在已占格上叠放(不新增占地), 但至少1格可见.
对比: 现状(全部物品独立占地) vs 堆叠优化(堆叠物可叠放, 释放地面格).
指标: 最大连续空矩 + 占用格数. 数据: parse_dump 会话级.
"""
import sys
sys.path.insert(0, "/d/git/invsort/tscripts")
from parse_dump import parse_dump
import verify_all as va

def stack_optimize(items, W, H):
    """堆叠优化: 普通物品先落地(贴边/贴块), 堆叠物品最后放, 可压已占格(但至少1新格可见).
    items[i]["stack"]=True 表示可堆叠. 返回 occ+msg."""
    # 普通物品: 用 greedy bottom 落位(只放非堆叠)
    normal = [it for it in items if not it.get("stack")]
    stacky = [it for it in items if it.get("stack")]
    occ = set()
    # 普通物品按 cell 数降序落地(贴地/贴块)
    for it in sorted(normal, key=lambda i: -len(i["cells"])):
        best = None
        for key, nw, nh in va.rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if va.can_place(occ, cells, px, py, W, H):
                        # 贴地或贴块
                        if any(y + 1 == H or (x, y + 1) in occ for x, y in ((px + dx, py + dy) for dx, dy in cells)):
                            best = (0, py, px, cells)
                            break
                if best:
                    break
            if best:
                break
        if not best:
            return None, "normal_fail"
        occ = va.mark(occ, best[3], best[2], best[1])
    # 堆叠物品: 最后放, 允许压已占格(overlap≥1), 但至少1格是新格(可见); 若无法压, 落地
    for it in sorted(stacky, key=lambda i: -len(i["cells"])):
        best = None
        for key, nw, nh in va.rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    pos = [(px + dx, py + dy) for dx, dy in cells]
                    if not all(0 <= x < W and 0 <= y < H for x, y in pos):
                        continue
                    new_cnt = sum(1 for x, y in pos if (x, y) not in occ)
                    if new_cnt < 1:
                        continue  # 全部被覆盖不可见 -> 非法
                    overlap = len(cells) - new_cnt
                    # 评分: 新格越少越好(少占地面), 若同分取左上
                    if best is None or new_cnt < best[0] or (new_cnt == best[0] and (py < best[2] or (py == best[2] and px < best[3]))):
                        best = (new_cnt, overlap, py, px, cells)
        if not best:
            return None, "stack_fail"
        occ = va.mark(occ, best[4], best[3], best[2])
    return occ, "ok"

def stack_greedy_touch(items, W, H):
    """对照: 现状(greedy bottom 全物品独立占地)"""
    return va.pack_grow_touch(items, W, H), "grow"

def run():
    import verify_all as va2
    sessions = parse_dump()
    for w, h, items in sessions:
        total = sum(len(i["cells"]) for i in items)
        if total > w * h or total == 0:
            continue
        # 标记可堆叠: 模拟小件可堆叠(1x1, 1x2, 2x1, 2x2 小件)
        for it in items:
            it["stack"] = len(it["cells"]) <= 4 and it["w"] <= 2 and it["h"] <= 2
        n_stack = sum(1 for i in items if i.get("stack"))
        va.W = w
        va.H = h
        o1, m1 = stack_optimize(items, w, h)
        a1, b1 = va.max_empty(o1, w, h) if o1 else (-1, None)
        # 公平对照: 同一个布局器但堆叠物品也独立占地(不叠) — 即 stack 标志全 False
        for it in items:
            it["stack"] = False
        o3, m3 = stack_optimize(items, w, h)
        a3, b3 = va.max_empty(o3, w, h) if o3 else (-1, None)
        tag = "WIN" if (o1 and o3 and a1 > a3) else ("LOSE" if (o1 and o3 and a1 < a3) else "=")
        print(f"{w}x{h} 件={len(items)} 堆叠={n_stack} 格={total} [堆叠] {a1} {b1} vs [非堆叠] {a3} {b3} => {tag}")

if __name__ == "__main__":
    run()
