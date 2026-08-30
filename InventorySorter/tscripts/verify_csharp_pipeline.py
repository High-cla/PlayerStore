# -*- coding: utf-8 -*-
"""复刻 C# 新管线: BuildUnits(配对) -> CellCount降序 -> PlaceGrounded(落地堆积) -> 最大空矩"""
exec(open("analyze_shapes.py", encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

def rot_cells(cells, times, sw, sh):
    cur = list(cells); wd, sd = sw, sh
    for _ in range(times):
        cur = [(wd - 1 - y, x) for x, y in cur]
        wd, sd = sd, wd
    mx = min(x for x, y in cur); my = min(y for x, y in cur)
    cur = [(x - mx, y - my) for x, y in cur]
    return cur

def items_to_masks(items):
    # 类似 ReadMask: 每件生成 C0-C3 (归一化), Gw0/Gh0
    out = []
    for it in items:
        c0 = it["cells"]; g0w, g0h = it["w"], it["h"]
        cs = [rot_cells(c0, i, g0w, g0h) for i in range(4)]
        out.append({"c0": c0, "c1": cs[1], "c2": cs[2], "c3": cs[3], "g0w": g0w, "g0h": g0h})
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
            for dx in range(-(bw - 1), aw + 1):
                for dy in range(-(bh - 1), ah):
                    r = try_fit(ca, aw, ah, cb, bw, bh, dx, dy)
                    if r:
                        rx, ry, rw, rh, obx, oby = r
                        cells = [(cx - rx, cy - ry) for cx, cy in ca] + [(cx + obx - rx, cy + oby - ry) for cx, cy in cb]
                        return {"a_oa": oa, "b_ob": ob, "ax": -rx, "ay": -ry, "bx": obx - rx, "by": oby - ry,
                                "cells": cells, "gw": rw, "gh": rh}
    return None

def try_fit(ca, aw, ah, cb, bw, bh, dx, dy):
    minX, minY, maxX, maxY = 0, 0, aw, ah
    if dx < minX: minX = dx
    if dy < minY: minY = dy
    if dx + bw > maxX: maxX = dx + bw
    if dy + bh > maxY: maxY = dy + bh
    rw, rh = maxX - minX, maxY - minY
    grid = [[False] * rh for _ in range(rw)]
    filled = 0
    for cx, cy in ca:
        gx, gy = cx - minX, cy - minY
        if 0 <= gx < rw and 0 <= gy < rh and not grid[gx][gy]:
            grid[gx][gy] = True; filled += 1
    for cx, cy in cb:
        gx, gy = cx + dx - minX, cy + dy - minY
        if not (0 <= gx < rw and 0 <= gy < rh): return None
        if grid[gx][gy]: return None
        grid[gx][gy] = True; filled += 1
    if filled != rw * rh: return None
    return (minX, minY, rw, rh, dx, dy)

def build_units(items):
    masks = items_to_masks(items)
    used = [False] * len(items); result = []
    for i in range(len(items)):
        if used[i]: continue
        best = None; best_j = -1; best_score = 0
        for j in range(i + 1, len(items)):
            if used[j]: continue
            u = try_complement(masks[i], masks[j])
            if u:
                score = u["gw"] * u["gh"]
                if same_shape(masks[i], masks[j]): score += 1000000
                if score > best_score:
                    best = u; best_j = j; best_score = score
        if best:
            result.append(("pair", i, best_j, best)); used[i] = True; used[best_j] = True
        else:
            result.append(("single", i, None, None)); used[i] = True
    return result, masks

def cells_free(occ, px, py, cells):
    return all((px + dx, py + dy) not in occ and 0 <= px + dx < W and 0 <= py + dy < H for dx, dy in cells)

def grounded(occ, H, px, py, cells):
    return any(cy == H - 1 or (cy + 1 < H and (cx, cy + 1) in occ) for cx, cy in ((px + dx, py + dy) for dx, dy in cells))

def place_grounded(occ, m, min_row):
    bx, by, bo = -1, -1, 0
    for o in range(4):
        cs = cells_of(m, o)
        if not cs: continue
        gw = m["g0h"] if o in (1, 3) else m["g0w"]
        gh = m["g0w"] if o in (1, 3) else m["g0h"]
        if gw > W or gh > H: continue
        py_end = max(0, min_row - gh)
        for py in range(H - gh, py_end - 1, -1):
            hit = None
            for px in range(0, W - gw + 1):
                if cells_free(occ, px, py, cs) and grounded(occ, H, px, py, cs):
                    hit = (px, py); break
            if hit:
                px, py = hit
                if by < 0 or py > by or (py == by and px < bx):
                    bx, by, bo = px, py, o
                break
    return (bx, by, bo) if by >= 0 else None

def layout_dense(items, fixed=set()):
    # 先配对, 失败退回单件(与 C# LayoutDense 一致)
    occ, placed = try_units(items, fixed, pair=True)
    if occ is not None:
        return occ, placed
    return try_units(items, fixed, pair=False)

def try_units(items, fixed, pair):
    occ = set(fixed)
    if pair:
        units, masks = build_units(items)
    else:
        masks = items_to_masks(items)
        units = [("single", i, None, None) for i in range(len(items))]
    def cell_count(u):
        if u[0] == "pair": return len(u[3]["cells"])
        return len(masks[u[1]]["c0"])
    units.sort(key=lambda u: -cell_count(u))
    min_row = H
    if fixed:
        min_row = min(y for x, y in fixed)
    placed = {}
    for u in units:
        if u[0] == "pair":
            _, i, j, pu = u
            m = {"c0": pu["cells"], "c1": [], "c2": [], "c3": [], "g0w": pu["gw"], "g0h": pu["gh"]}
            r = place_grounded(occ, m, min_row)
            if r is None: return None, None
            bx, by, bo = r
            placed[i] = (bx + pu["ax"], by + pu["ay"], pu["a_oa"])
            placed[j] = (bx + pu["bx"], by + pu["by"], pu["b_ob"])
            occ |= {(bx + dx, by + dy) for dx, dy in pu["cells"]}
            min_row = min(min_row, by)
        else:
            _, i, None_, _ = u
            m = masks[i]
            r = place_grounded(occ, m, min_row)
            if r is None: return None, None
            bx, by, bo = r
            placed[i] = (bx, by, bo)
            occ |= {(bx + dx, by + dy) for dx, dy in cells_of(m, bo)}
            min_row = min(min_row, by)
    return occ, placed

items = all_items()
res = layout_dense(items)
if res:
    occ, placed = res
    a, b = largest_empty_rect_fast(occ)
    print(f"[C#管线复刻: 配对+落地堆积] 占用={len(occ)} 剩余={W*H-len(occ)} 最大空矩={a} ({b})")
    for y in range(H):
        print("".join("#" if (x, y) in occ else "." for x in range(W)))
else:
    print("FAIL: 放不下")
