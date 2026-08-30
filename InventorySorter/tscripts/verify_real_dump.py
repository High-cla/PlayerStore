# -*- coding: utf-8 -*-
"""真实数据验证: 新管线(配对+落地堆积) vs 旧管线(MFR) 最大空矩对比
直接从 parse_dump 拿真实散件, 复用 verify_csharp_pipeline 的配对+PlaceGrounded 逻辑
"""
import sys
sys.path.insert(0, "/d/git/invsort/tscripts")
from analyze_shapes import rotations, can_place, mark
from analyze2 import largest_empty_rect_fast
from parse_dump import parse_dump

def rot_cells(cells, times, sw, sh):
    cur = list(cells); wd, sd = sw, sh
    for _ in range(times):
        cur = [(wd-1-y, x) for x, y in cur]
        wd, sd = sd, wd
    mx = min(x for x, y in cur); my = min(y for x, y in cur)
    return [(x-mx, y-my) for x, y in cur]

def items_to_masks(items):
    out = []
    for it in items:
        c0 = it["cells"]; g0w, g0h = it["w"], it["h"]
        cs = [rot_cells(c0, i, g0w, g0h) for i in range(4)]
        out.append({"c0": c0, "c1": list(cs[1]), "c2": list(cs[2]), "c3": list(cs[3]), "g0w": g0w, "g0h": g0h})
    return out

def cells_of(m, o):
    return m["c%d" % o]

def same_shape(a, b):
    return a["g0w"] == b["g0w"] and a["g0h"] == b["g0h"] and a["c0"] == b["c0"]

def try_complement(ma, mb):
    for oa in range(4):
        ca = cells_of(ma, oa)
        aw = ma["g0h"] if oa in (1, 3) else ma["g0w"]
        ah = ma["g0w"] if oa in (1, 3) else ma["g0h"]
        for ob in range(4):
            cb = cells_of(mb, ob)
            bw = mb["g0h"] if ob in (1, 3) else mb["g0w"]
            bh = mb["g0w"] if ob in (1, 3) else mb["g0h"]
            for dx in range(-(bw-1), aw+1):
                for dy in range(-(bh-1), ah):
                    r = try_fit(ca, aw, ah, cb, bw, bh, dx, dy)
                    if r:
                        rx, ry, rw, rh, obx, oby = r
                        cells = [(cx-rx, cy-ry) for cx, cy in ca] + [(cx+obx-rx, cy+oby-ry) for cx, cy in cb]
                        return {"oa": oa, "ob": ob, "ax": -rx, "ay": -ry, "bx": obx-rx, "by": oby-ry,
                                "cells": cells, "gw": rw, "gh": rh}
    return None

def try_fit(ca, aw, ah, cb, bw, bh, dx, dy):
    minX, minY, maxX, maxY = 0, 0, aw, ah
    if dx < minX: minX = dx
    if dy < minY: minY = dy
    if dx+bw > maxX: maxX = dx+bw
    if dy+bh > maxY: maxY = dy+bh
    rw, rh = maxX-minX, maxY-minY
    grid = [[False]*rh for _ in range(rw)]
    filled = 0
    for cx, cy in ca:
        gx, gy = cx-minX, cy-minY
        if 0 <= gx < rw and 0 <= gy < rh and not grid[gx][gy]:
            grid[gx][gy] = True; filled += 1
    for cx, cy in cb:
        gx, gy = cx+dx-minX, cy+dy-minY
        if not (0 <= gx < rw and 0 <= gy < rh): return None
        if grid[gx][gy]: return None
        grid[gx][gy] = True; filled += 1
    if filled != rw*rh: return None
    return (minX, minY, rw, rh, dx, dy)

def build_units(items):
    masks = items_to_masks(items)
    used = [False]*len(items); result = []
    for i in range(len(items)):
        if used[i]: continue
        best = None; best_score = 0; best_j = -1
        for j in range(i+1, len(items)):
            if used[j]: continue
            u = try_complement(masks[i], masks[j])
            if u:
                score = u["gw"]*u["gh"]
                if same_shape(masks[i], masks[j]): score += 1000000
                if score > best_score:
                    best = u; best_score = score; best_j = j
        if best:
            result.append({"type": "pair", "i": i, "j": best_j, "u": best})
            used[i] = True; used[best_j] = True
        else:
            result.append({"type": "single", "i": i})
            used[i] = True
    return result, masks

def cells_free(occ, px, py, cells):
    return all((px+dx, py+dy) not in occ and 0 <= px+dx < W and 0 <= py+dy < H for dx, dy in cells)

def grounded(occ, H, px, py, cells):
    return any(cy == H-1 or (cy+1 < H and (cx, cy+1) in occ) for cx, cy in ((px+dx, py+dy) for dx, dy in cells))

def place_grounded(occ, m, min_row):
    bx, by, bo = -1, -1, 0
    for o in range(4):
        cs = cells_of(m, o)
        if not cs: continue
        gw = m["g0h"] if o in (1, 3) else m["g0w"]
        gh = m["g0w"] if o in (1, 3) else m["g0h"]
        if gw > W or gh > H: continue
        py_end = max(0, min_row - gh)
        for py in range(H-gh, py_end-1, -1):
            for px in range(0, W-gw+1):
                if not cells_free(occ, px, py, cs): continue
                if not grounded(occ, H, px, py, cs): continue
                if by < 0 or py > by or (py == by and px < bx):
                    bx, by, bo = px, py, o
                break
            if bx == px and by == py: break  # 已找到该朝向首适配
        if bx >= 0: break
    return (bx, by, bo) if by >= 0 else None

def min_occ_row(occ, H):
    for y in range(H):
        if any((x, y) in occ for x in range(W)):
            return y
    return H

def layout_dense_paired(items, W, H):
    """新管线: 配对 + 落地堆积 (整数/全格锁死时失败返回 None)"""
    units, masks = build_units(items)
    units.sort(key=lambda u: -len(u["u"]["cells"] if u["type"] == "pair" else items[u["i"]]["cells"]))
    occ = set()
    min_row = H
    for u in units:
        if u["type"] == "pair":
            pu = u["u"]
            pos = place_grounded(occ, {"c0": pu["cells"], "c1": [], "c2": [], "c3": [], "g0w": pu["gw"], "g0h": pu["gh"]}, min_row)
            if not pos: return None
            px, py, o = pos
            # 整体单元落位 (PairUnit 只有 C0, 无旋转)
            occ |= {(px+dx, py+dy) for dx, dy in pu["cells"]}
            min_row = min(min_row, py)
        else:
            m = masks[u["i"]]
            pos = place_grounded(occ, m, min_row)
            if not pos: return None
            px, py, o = pos
            occ |= {(px+dx, py+dy) for dx, dy in cells_of(m, o)}
            min_row = min(min_row, py)
    return occ

def layout_greedy_bottom(items, W, H):
    """无配对 - 直接按 cell 数降序落地"""
    masks = items_to_masks(items)
    order = sorted(range(len(items)), key=lambda i: -len(items[i]["cells"]))
    occ = set()
    min_row = H
    for i in order:
        m = masks[i]
        pos = place_grounded(occ, m, min_row)
        if not pos: return None
        px, py, o = pos
        occ |= {(px+dx, py+dy) for dx, dy in cells_of(m, o)}
        min_row = min(min_row, py)
    return occ

if __name__ == "__main__":
    g = parse_dump()
    results = {}
    for key in sorted(g, key=lambda k: -len(g[k])):
        W, H = key
        items = g[key]
        if len(items) < 2: continue
        # 新管线 (配对+落地)
        oc1 = layout_dense_paired(items, W, H)
        r1 = largest_empty_rect_fast(oc1)[0] if oc1 else None
        # 无配对落地
        oc2 = layout_greedy_bottom(items, W, H)
        r2 = largest_empty_rect_fast(oc2)[0] if oc2 else None
        results[key] = (len(items), r1, r2)
        print(f"{key} items={len(items)} 配对+落地={r1} 无配对落地={r2}")
