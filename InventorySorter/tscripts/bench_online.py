# -*- coding: utf-8 -*-
"""在线镜像对拍: 精确镜像 C# TryResidualLayout(预订精确格+释放自身+最贴合+原地回退)
安全不变量验证: 落地永不与留位件重叠; relocate 在近满组确实发生(小件塞缝).
与 bench_residual(离线/无初始位 soft)互补: 本脚本有真实初始位置语义.
"""
import os, sys, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import verify_all as va

def rotations(cells0, w0, h0):
    """C# BuildMask 语义: C0..C3 = 基形 0/90/180/270, 全部自动归一化(min=0)."""
    def rot90(c, bw, bh):
        return [(bh - 1 - y, x) for x, y in c]
    out = []
    base = tuple(sorted(cells0))
    cur = base
    bw, bh = w0, h0
    out.append((cur, bw, bh))
    for _ in range(3):
        nxt = tuple(sorted(rot90(list(cur), bw, bh)))
        nw = max(x for x, y in nxt) + 1
        nh = max(y for x, y in nxt) + 1
        out.append((nxt, nw, nh))
        cur = nxt
        bw, bh = nw, nh
    return out

def online_residual(items, W, H):
    """items: [{shape: [(cells,w,h)x4], cur:(x,y,o), cells0}]  -> (moved, stayed, ok, occ_final, layout)"""
    occ = set()
    for it in items:
        x, y, o = it["cur"]
        cells = it["shape"][o][0]
        occ |= {(x + dx, y + dy) for dx, dy in cells}
    order = sorted(items, key=lambda t: -len(t["cells0"]))
    layout = {}   # id(item) -> (x,y,o)
    moved = 0
    for it in order:
        x0, y0, o0 = it["cur"]
        own = it["shape"][o0][0]
        occ -= {(x0 + dx, y0 + dy) for dx, dy in own}
        best = None
        for oi, (cs, nw, nh) in enumerate(it["shape"]):
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if not va.can_place(occ, cs, px, py, W, H):
                        continue
                    waste = nw * nh - len(cs)
                    key = (waste, py, px)
                    if best is None or key < best[0]:
                        best = (key, oi, px, py, cs)
        if best is not None:
            _k, oi, px, py, cs = best
            occ |= {(px + dx, py + dy) for dx, dy in cs}
            layout[id(it)] = (px, py, oi)
            if (px, py, oi) != (x0, y0, o0):
                moved += 1
        else:
            occ |= {(x0 + dx, y0 + dy) for dx, dy in own}
    # 不变量: 最终占用不重叠(布局 + 留位件)
    placed = {}
    for k, (px, py, oi) in layout.items():
        it = next(t for t in items if id(t) == k)
        cs = it["shape"][oi][0]
        placed[k] = {(px + dx, py + dy) for dx, dy in cs}
    stayed = [it for it in items if id(it) not in layout]
    stayed_cells = set()
    for it in stayed:
        x, y, o = it["cur"]
        stayed_cells |= {(x + dx, y + dy) for dx, dy in it["shape"][o][0]}
    final = set()
    ok = True
    for cs in placed.values():
        if cs & final or any(c[0] < 0 or c[1] < 0 or c[0] >= W or c[1] >= H for c in cs):
            ok = False
        final |= cs
    if final & stayed_cells:
        ok = False
    return moved, len(stayed), ok, final | stayed_cells, len(layout)

def main():
    pool = va.catalog_pool()
    rng = random.Random(3)
    sizes = [(24, 10), (17, 10), (11, 14), (10, 10), (9, 7), (8, 8), (8, 9)]
    tot = totmoved = totgrp = 0
    bad = 0
    fills = []
    per_fill = {}
    for w, h in sizes:
        cap = w * h
        feas = [it for it in pool if it["w"] <= w and it["h"] <= h]
        if not feas:
            continue
        avg = sum(len(it["cells"]) for it in feas) / len(feas)
        for fill in (0.80, 0.88, 0.93, 0.97, 0.99):
            n = max(3, int(cap * fill) // max(1, int(avg)))
            for _ in range(20):
                raw = [rng.choice(feas) for _ in range(n)]
                # 同类合并(还原 mergeRepIdx)
                merged = {}
                for it in raw:
                    k = (it["name"], tuple(sorted(it["cells"])))
                    merged.setdefault(k, dict(it, count=0))
                    merged[k]["count"] += 1
                items = list(merged.values())
                total = sum(len(i["cells"]) for i in items)
                if total > cap:
                    continue
                # 初始布局: 顺序扫描 y,x,朝向0(每件必落, fill<=1 保证)
                occ = set()
                shaped = []
                ok_init = True
                for it in items:
                    sh = rotations(it["cells"], it["w"], it["h"])
                    cs0, nw, nh = sh[0]
                    pos = None
                    for py in range(h - nh + 1):
                        for px in range(w - nw + 1):
                            if va.can_place(occ, cs0, px, py, w, h):
                                pos = (px, py, 0)
                                break
                        if pos:
                            break
                    if pos is None:
                        ok_init = False
                        break
                    occ |= {(pos[0] + dx, pos[1] + dy) for dx, dy in cs0}
                    shaped.append({"shape": sh, "cur": pos, "cells0": it["cells"], "name": it["name"]})
                if not ok_init:
                    continue
                mv, st, ok, _final, laid = online_residual(shaped, w, h)
                totgrp += 1
                tot += len(shaped)
                totmoved += mv
                fills.append(fill)
                b = per_fill.setdefault(fill, [0, 0, 0])  # [groups, total, moved]
                b[0] += 1; b[1] += len(shaped); b[2] += mv
                if not ok:
                    bad += 1
                    print(f"  INVARIANT FAIL {w}x{h} fill={fill:.2f} n={len(shaped)} mv={mv}")
    print(f"online_residual 组数={totgrp} 件数={tot} moved={totmoved} ({100*totmoved/max(1,tot):.1f}%)  不变量失败={bad}")
    for fill in sorted(per_fill):
        g, tt, mm = per_fill[fill]
        print(f"  fill={fill}: {g}组 {mm}/{tt} moved ({100*mm/max(1,tt):.1f}%)")
    print("注: moved% 越高 = 降级越会把物品挪进更贴合缝; 近满组应显著. 真实包(乱序初始位)会比合成扫描序更易挪动.")

if __name__ == "__main__":
    main()
