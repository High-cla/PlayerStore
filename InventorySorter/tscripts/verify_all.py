# -*- coding: utf-8 -*-
"""统一验证: 全部排序算法 × 全部真实数据组, 对比最大连续空矩(越大越能放更大物品)

算法:
  PairGrounded   - 两两配对 + 落地堆积(新版主, BuildUnits+TryPlaceUnits)
  GreedyBottom   - 无配对落地堆积(新版兜底)
  PGSplit        - PairGrounded + 配对失败拆回 singles(对应 C# SplitFailedUnit)
  MetaBest       - PG/GB/BestFit/PGSplit/GrowTouch 元选择, 取成功者最大空矩(13组零失败)
  BestFitMFR     - MFR 池最小 waste 选位(旧版 PlaceInto+FindFreeSpotCells)
  GrowTouch      - 逐格贴占粘连(旧版, 部分组有亮点)

数据: parse_dump 全部会话组(容量不足组跳过)。指标: 最大连续空矩 + 是否成功。
已删(数据证实无价值): Shelf(13组9败), MinHole(全垫底), Rows(被 GreedyBottom 支配)。
"""
import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from analyze2 import largest_empty_rect_fast
from parse_dump import parse_dump, parse_dump_merged
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


# ---- 组合优化: PGSplit(配对失败拆分) + MetaBest(元选择) ----
def pack_pg_split(items, W, H):
    """PairGrounded 变体: 配对单元放不下 -> 拆回两个 singles 各自落地. 任一 single 也放不下才失败."""
    units, masks = v.build_units(items)
    units.sort(key=lambda u: -len(u["u"]["cells"] if u["type"] == "pair" else items[u["i"]]["cells"]))
    v.W, v.H = W, H
    occ = set()
    min_row = H

    def place(mdict):
        nonlocal occ, min_row
        pos = v.place_grounded(occ, mdict, min_row)
        if not pos:
            return False
        px, py, o = pos
        occ |= {(px + dx, py + dy) for dx, dy in v.cells_of(mdict, o)}
        min_row = min(min_row, py)
        return True

    for u in units:
        if u["type"] == "pair":
            pu = u["u"]
            pm = {"c0": pu["cells"], "c1": [], "c2": [], "c3": [], "g0w": pu["gw"], "g0h": pu["gh"]}
            if not place(pm):
                if not (place(masks[u["i"]]) and place(masks[u["j"]])):
                    return None
        else:
            if not place(masks[u["i"]]):
                return None
    return occ


def pack_meta(items, W, H):
    """元选择: 全算法跑, 取成功者中最大空矩最高的布局."""
    best_occ, best_a = None, -1
    for fn in (v.layout_dense_paired, v.layout_greedy_bottom, pack_bestfit_mfr, pack_pg_split, pack_grow_touch, pack_guillotine, pack_left_bottom, pack_minhole_stack):
        try:
            occ = fn(items, W, H)
        except Exception:
            occ = None
        if occ is None:
            continue
        a, _ = max_empty(occ, W, H)
        if a > best_a:
            best_a, best_occ = a, occ
    return best_occ


# ---- 新增算法: Guillotine / LeftBottom / MinHoleStack (C#/Rust 模组对应) ----
def _fits(gw, gh, rw, rh):
    """物品(gw,gh)能放进碎片(rw,rh), 考虑旋转."""
    return (gw <= rw and gh <= rh) or (gh <= rw and gw <= rh)


def _dedup_rects(rects):
    """去包含: 去除被更大矩形覆盖的碎片."""
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


def pack_guillotine(items, W, H, alpha=0.1):
    """GuillotineCut + 死洞惩罚: 每次选 waste + α×死洞面积最小的 free-rect+朝向, 按割线切碎."""
    freerects = [(0, 0, W, H)]
    occ = set()
    order = sorted(items, key=lambda i: -len(i["cells"]))
    # 全局最小物品包围盒: 碎片放得下物品(旋转) ⟺ min(gw,gh)<=min(rw,rh) && max(gw,gh)<=max(rw,rh)
    min_side, max_side = 1 << 30, 1 << 30
    for it in order:
        rots = [(nw, nh) for _, nw, nh in rotations_of(it)]
        for nw, nh in rots:
            min_side = min(min_side, min(nw, nh))
            max_side = min(max_side, max(nw, nh))
    for it in order:
        best = None
        for fi, (frx, fry, frw, frh) in enumerate(freerects):
            for key, nw, nh in rotations_of(it):
                if nw > frw or nh > frh:
                    continue
                waste = frw * frh - nw * nh
                # 死洞: 割裂产物放不下任意物品(旋转后)的面积
                dead = 0
                d_below = frh - nh
                d_right = frw - nw
                if d_below > 0:
                    mn, mx = min(frw, d_below), max(frw, d_below)
                    if mn < min_side or mx < max_side:
                        dead += frw * d_below
                if d_right > 0:
                    mn, mx = min(d_right, frh), max(d_right, frh)
                    if mn < min_side or mx < max_side:
                        dead += d_right * frh
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
        freerects = _dedup_rects(freerects)
    return occ


def pack_left_bottom(items, W, H):
    """左下锚定: px 最小优先, 并列 py 最大, 聚左下块留右上."""
    occ = set()
    order = sorted(items, key=lambda i: -len(i["cells"]))
    for it in order:
        best = None
        for key, nw, nh in rotations_of(it):
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if can_place(occ, key, px, py, W, H):
                        if best is None or px < best[0] or (px == best[0] and py > best[1]):
                            best = (px, py, list(key))
        if best is None:
            return None
        occ = mark(occ, best[2], best[0], best[1])
    return occ


def pack_minhole_stack(items, W, H):
    """堆叠叠放: 堆叠物(count>1)允许压已占格但≥1新格, 评分=新格数(<<32)+放置后最大空矩."""
    occ = set()
    order = sorted(items, key=lambda i: -len(i["cells"]))
    for it in order:
        stackable = len(it.get("stack", [])) > 0 or it.get("count", 1) > 1
        best = None
        for key, nw, nh in rotations_of(it):
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    # 合法性: 堆叠物允许格重叠(反向下), 但须>=1新格; 非堆叠全空格
                    fresh = 0
                    ok = True
                    for dx, dy in key:
                        cx, cy = px + dx, py + dy
                        if cx < 0 or cy < 0 or cx >= W or cy >= H:
                            ok = False
                            break
                        if (cx, cy) in occ:
                            if not stackable:
                                ok = False
                                break
                            continue
                        fresh += 1
                    if not ok or (stackable and fresh == 0):
                        continue
                    # 评分: 新格数(高位) + 放置后最大空矩(低位), 取小
                    nocc = occ | {(px + dx, py + dy) for dx, dy in key}
                    la, _ = max_empty(nocc, W, H)
                    score = ((fresh << 32) | la) if stackable else la
                    if best is None or score < best[0]:
                        best = (score, px, py, list(key))
        if best is None:
            return None
        occ |= {(best[1] + dx, best[2] + dy) for dx, dy in best[3]}
    return occ


ALGOS = {
    "PairGrounded": lambda items, W, H: v.layout_dense_paired(items, W, H),
    "GreedyBottom": lambda items, W, H: v.layout_greedy_bottom(items, W, H),
    "PGSplit": pack_pg_split,
    "MetaBest": pack_meta,
    "BestFitMFR": pack_bestfit_mfr,
    "GrowTouch": pack_grow_touch,
    "Guillotine": pack_guillotine,
    "LeftBottom": pack_left_bottom,
    "MinHoleStack": pack_minhole_stack,
}


def run():
    # parse_dump_merged = 同类合并投影(还原模组 mergeRepIdx): 相同 ident+形状多件合并为 1 代表件,
    # unit_count 赋给代表件. 布局器只排代表件, 其余应用阶段叠放(游戏堆叠). 输出 (w,h,[(rep,unit_count)]).
    sessions = parse_dump_merged()
    out = []
    best_by_key = {}
    for w, h, reps in sessions:
        # 代表件列表 (与模组布局器 flat 一致: 只含代表件, count=unit_count)
        items = [dict(r, count=uc) for r, uc in reps]
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


# ---- 全物品池: 从 full_item_catalog.json 展平全部 shapes, 随机抽组跑 9 算法 ----
def catalog_pool():
    """从 catalog 展平全部 shapes 成物品池 [(cells,w,h), ...]."""
    import json
    cat = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "full_item_catalog.json"), encoding="utf-8"))
    recs = cat["records"] if isinstance(cat, dict) else cat
    pool = []
    for r in recs:
        if not r.get("shapes"):
            continue
        for s in r["shapes"]:
            cells = [tuple(c) for c in s["cells"]]
            if not cells:
                continue
            pool.append({"cells": cells, "w": s["w"], "h": s["h"], "name": r.get("stableId", "?")})
    return pool


def run_catalog(pool=None, per=20, seed=42, sizes=None):
    """从全物品池随机抽组, 逐算法对比. 尺寸自适应 n (总占格≈容量*FILL)."""
    import random
    if pool is None:
        pool = catalog_pool()
    if sizes is None:
        sizes = [(24, 10), (9, 7), (10, 10), (8, 8), (17, 10), (11, 14), (8, 9), (14, 21), (7, 5), (8, 6), (6, 8), (5, 5), (7, 11)]
    rng = random.Random(seed)
    out = []
    for w, h in sizes:
        cap = w * h
        # 可行子池: 单件至少一朝向塞得进 W×H
        feas = [it for it in pool if any(nw <= w and nh <= h for _, nw, nh in rotations_of(it))]
        if not feas:
            continue
        avg = sum(len(it["cells"]) for it in feas) / len(feas)
        n = max(2, min(14, round(cap * 0.6 / avg)))
        wins = {k: 0 for k in ALGOS}
        tot = {k: 0 for k in ALGOS}
        succ = {k: 0 for k in ALGOS}
        for _ in range(per):
            raw = [rng.choice(feas) for _ in range(n)]
            # 同类合并投影(还原模组 mergeRepIdx): 相同 name+cells 合并为 1 代表件, count=件数
            merged = {}
            for it in raw:
                key = (it["name"], tuple(sorted(it["cells"])), it["w"], it["h"])
                if key not in merged:
                    merged[key] = dict(it, count=1)
                else:
                    merged[key]["count"] += 1
            items = list(merged.values())
            if sum(len(i["cells"]) for i in items) > cap:
                continue
            v.W, v.H = w, h
            for name, fn in ALGOS.items():
                try:
                    occ = fn(items, w, h)
                except Exception:
                    occ = None
                if occ is None:
                    continue
                a, _ = max_empty(occ, w, h)
                succ[name] += 1
                tot[name] += a
                wins[name] = max(wins[name], a)
        row = {"group": f"{w}x{h}", "n": n, "cells": cap}
        for name in ALGOS:
            row[name] = tot[name] / succ[name] if succ[name] else -1
        out.append(row)
    return out


if __name__ == "__main__":
    res = run()
    hdr = f"{'组':<8} {'件':<3} {'格':<4} " + " ".join(f"{n:<11}" for n in ALGOS)
    print("=== 真实 dump 组 ===")
    print(hdr)
    for r in res:
        print(f"{r['group']:<8} {r['n']:<3} {r['cells']:<4} " + " ".join(f"{r[k]:<11}" for k in ALGOS))
    print("\n=== 全物品池随机组 ===")
    cres = run_catalog()
    print(hdr)
    for r in cres:
        print(f"{r['group']:<8} {r['n']:<3} {r['cells']:<4} " + " ".join(f"{r[k]:<11}" for k in ALGOS))

