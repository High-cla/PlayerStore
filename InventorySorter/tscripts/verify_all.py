# -*- coding: utf-8 -*-
"""统一验证: 全部排序算法 × 全部真实数据组, 对比最大连续空矩(越大越能放更大物品)

算法:
  PairGrounded   - 两两配对 + 落地堆积(新版主, BuildUnits+TryPlaceUnits+SplitFailedUnit)
  GreedyBottom   - 无配对落地堆积(新版兜底)
  BestFitMFR     - MFR 池最小 waste 选位(旧版 PlaceInto+FindFreeSpotCells)
  Shelf          - 行堆积(旧版)
  GrowTouch      - 逐格贴占粘连(旧版)
  Rows           - 行堆叠(旧版)
  MinHole        - 每步最小化最大空矩(旧版)

数据: parse_dump 全部会话组(容量不足组跳过)。指标: 最大连续空矩 + 是否成功。
"""
import sys
sys.path.insert(0, "/d/git/invsort/tscripts")
from analyze2 import largest_empty_rect_fast
from parse_dump import parse_dump
import verify_real_dump as v


def rotations_of(it):
    """4 旋转去重(任意尺寸), 返回 [(cells,w,h), ...]"""
    def rot(cells, times, sw, sh):
        c = list(cells); Wd, Sd = sw, sh
        for _ in range(times):
            c = [(Wd - 1 - y, x) for x, y in c]
            Wd, Sd = Sd, Wd
        mx = min(x for x, y in c); my = min(y for x, y in c)
        return [(x - mx, y - my) for x, y in c]
    out = []
    for i in range(4):
        c = rot(it["cells"], i, it["w"], it["h"])
        nw = max(x for x, y in c) + 1; nh = max(y for x, y in c) + 1
        key = tuple(sorted(c))
        if not any(k == key for k in (r[0] for r in out)):
            out.append((key, nw, nh))
    return out


def can_place(occ, cells, px, py, W, H):
    return all((px + dx, py + dy) not in occ and 0 <= px + dx < W and 0 <= py + dy < H for dx, dy in cells)


def mark(occ, cells, px, py):
    return occ | {(px + dx, py + dy) for dx, dy in cells}


def max_empty(occ, W, H):
    if occ is None:
        return -1, None
    g = [[0] * W for _ in range(H)]
    for x in range(W):
        for y in range(H):
            g[y][x] = 1 if (x, y) in occ else 0
    best = 0; bestb = None
    heights = [0] * W
    for y in range(H):
        for x in range(W):
            heights[x] = heights[x] + 1 if g[y][x] == 0 else 0
        stack = []
        for x in range(W + 1):
            h = heights[x] if x < W else -1
            while stack and heights[stack[-1]] > h:
                hh = heights[stack.pop()]
                left = stack[-1] + 1 if stack else 0
                w = x - left
                if w * hh > best:
                    best = w * hh; bestb = (left, y - hh + 1, w, hh)
            stack.append(x)
    return best, bestb


# ---- 旧版五算法(参数化 W/H) ----
def pack_bestfit_mfr(items, W, H):
    items = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    for it in items:
        best = None
        for key, nw, nh in rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if can_place(occ, cells, px, py, W, H):
                        waste = nw * nh - len(cells)
                        if best is None or waste < best[0] or (waste == best[0] and (py < best[1] or (py == best[1] and px < best[2]))):
                            best = (waste, py, px, cells)
        if best:
            occ = mark(occ, best[3], best[2], best[1])
        else:
            return None
    return occ


def pack_shelf(items, W, H):
    items = sorted(items, key=lambda i: (-i["w"], -i["h"]))
    occ = set()
    cur_y = 0; cur_row_h = 0
    for it in items:
        best = None
        for key, nw, nh in rotations_of(it):
            for base_y in [cur_y, cur_y + cur_row_h]:
                if base_y + nh > H:
                    continue
                for px in range(W - nw + 1):
                    c = list(key)
                    if can_place(occ, c, px, base_y, W, H):
                        if best is None or (base_y < best[1] or (base_y == best[1] and px < best[2])):
                            best = (0, base_y, px, nw, nh, c)
                        break
        if best:
            occ = mark(occ, best[5], best[2], best[1])
            if best[1] + best[4] > cur_y + cur_row_h:
                cur_row_h = best[1] + best[4] - cur_y if best[1] >= cur_y else cur_row_h
        else:
            return None
    return occ


def pack_grow_touch(items, W, H):
    items = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    for it in items:
        best = None
        for key, nw, nh in rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if can_place(occ, cells, px, py, W, H):
                        touch = 0
                        for dx, dy in cells:
                            ax, ay = px + dx, py + dy
                            for adx, ady in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                                if (ax + adx, ay + ady) in occ:
                                    touch += 1
                            if ax == 0 or ax == W - 1:
                                touch += 1
                            if ay == 0 or ay == H - 1:
                                touch += 1
                        if best is None or touch > best[0] or (touch == best[0] and (py < best[1] or (py == best[1] and px < best[2]))):
                            best = (touch, py, px, list(cells))
        if best:
            occ = mark(occ, best[3], best[2], best[1])
        else:
            return None
    return occ


def pack_rows(items, W, H):
    items = sorted(items, key=lambda i: (-i["h"], -i["w"]))
    occ = set()
    for it in items:
        best = None
        for key, nw, nh in rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if can_place(occ, cells, px, py, W, H):
                        if best is None or (py < best[1] or (py == best[1] and px < best[2])):
                            best = (0, py, px, list(key))
        if best:
            occ = mark(occ, best[3], best[2], best[1])
        else:
            return None
    return occ


def pack_minhole(items, W, H):
    items = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    for it in items:
        best = None
        for key, nw, nh in rotations_of(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if can_place(occ, cells, px, py, W, H):
                        a, _ = max_empty(mark(occ, cells, px, py), W, H)
                        if best is None or a < best[0]:
                            best = (a, px, py, list(key))
        if best:
            occ = mark(occ, best[3], best[2], best[1])
        else:
            return None
    return occ


ALGOS = {
    "PairGrounded": lambda items, W, H: v.layout_dense_paired(items, W, H),
    "GreedyBottom": lambda items, W, H: v.layout_greedy_bottom(items, W, H),
    "BestFitMFR": pack_bestfit_mfr,
    "Shelf": pack_shelf,
    "GrowTouch": pack_grow_touch,
    "Rows": pack_rows,
    "MinHole": pack_minhole,
}


def run():
    sessions = parse_dump()
    out = []
    best_by_key = {}
    for w, h, items in sessions:
        total = sum(len(i["cells"]) for i in items)
        if total > w * h or total == 0:
            continue
        data = (w, h, items, total)
        if data[0:2] not in best_by_key or total > best_by_key[data[0:2]][3]:
            best_by_key[data[0:2]] = data
    for w, h, items, total in best_by_key.values():
        v.W, v.H = w, h
        row = {"group": f"{w}x{h}", "n": len(items), "cells": total}
        for name, fn in ALGOS.items():
            try:
                occ = fn(items, w, h)
                a, _ = max_empty(occ, w, h)
                row[name] = a if occ else -1
            except Exception:
                row[name] = -2
        out.append(row)
    return out


if __name__ == "__main__":
    res = run()
    hdr = f"{'组':<8} {'件':<3} {'格':<4} " + " ".join(f"{n:<11}" for n in ALGOS)
    print(hdr)
    for r in res:
        print(f"{r['group']:<8} {r['n']:<3} {r['cells']:<4} " + " ".join(f"{r[k]:<11}" for k in ALGOS))

