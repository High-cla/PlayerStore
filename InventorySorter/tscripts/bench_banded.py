# -*- coding: utf-8 -*-
"""横带(banded)路径离线对拍 + 精修收益实测 (task-4)

镜像 C# InventorySorter/InventorySorter/Core.cs:
  BuildTagGroups      : 783-805   TagKey 分桶; tagOrder 按 OrdinalIgnoreCase 排序; 组内 SizeCompare
  LayoutBanded        : 1091-1124 逐 tag 连续横带; 任一件放不下 ⇒ 整带作废 return null
  PlaceInto           : 2492      两轮(minY, 0) × 朝向 0..3; Square 只试朝向 0; 取 (min oy, 再 min ox)
  FindFreeSpotCells   : 2772      矩形左上角锚定 + HasSupport; 选择键 (py 最小, 再 px 最小); rects 不排序
  HasSupport          : 2816      每格需 cy==0 或 cx==0 或 左/上/下 邻已占(无右邻、无底壁检查)
  ShrinkRects         : 2432      不相交保留; 否则切左/右/中段上下; 末尾去包含
  FindFreeRects       : 2714      直方图 + 单调栈 MFR, 去包含
  SizeCompare         : 2932      bbox 面积降序 → max 边长降序 → min 边长降序 → ident → name → uid
  TryFillRefine       : 1437      精修层(小件优先 → 最小空矩 → 最贴邻; 空矩不降 + 贴邻不降才采纳)

精修层实现: 复用 bench_native.refine(与 Core.cs TryFillRefine 的 guard 路径同口径 —— bench_native 已逐字
复现 Lead 实测: guard OFF 20008 / guard ON 20056, 故该端口视为忠实)。

注1: parse_dump.py 的正则丢弃 tag 字段(只捕 ident/gw/gh), 故本文件自带带 tag 的解析; 其余流式规则与
     parse_dump.parse_dump 相同, 同类合并口径与 parse_dump.merge_same_type 相同。
注2: KeepContainers 默认 false ⇒ 横带 fixedItems = ∅(本对拍同取空集)。
注3: moved 比例以 bench_native 的 init(= big-first 行主序 first-fit 骨架) 为"玩家原始位"代偿口径。
"""
import os
import re
import time
import sys
import json
import random
from functools import cmp_to_key

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bench_native as bn
from verify_all import max_empty, can_place, catalog_pool
import verify_all as va

BAK = bn.BAK
SMALL_MAX = bn.SMALL_MAX
UNNAMED = "\uffff"
FILL_BUCKETS = ((0.0, 0.60), (0.60, 0.80), (0.80, 0.90), (0.90, 1.00), (1.00, 9.99))


def _ord_ic(s):
    """C# StringComparison.OrdinalIgnoreCase 的 ASCII 等价(本仓 ident/tag 均 ASCII)。"""
    return s.upper()


# ---------- 数据源(带 tag) ----------
def parse_dump_tagged(path):
    """同 parse_dump.parse_dump 的流式规则, 额外保留 tag 与 name= 显示名 + 出现序 uid。"""
    with open(path, encoding="utf-8") as f:
        lines = f.read().splitlines()
    sessions = []
    cur = None
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        m = re.match(r"== inv (\d+)x(\d+)", line)
        if m:
            cur = [int(m.group(1)), int(m.group(2)), []]
            sessions.append(cur)
            i += 1
            continue
        m = re.match(r"=== (.+?) \| name=(.*?) \| tag=(.*?) ===\((\d+)x(\d+)\)", line)
        if m and cur is not None:
            ident, dname, tag = m.group(1), m.group(2), m.group(3)
            gw, gh = int(m.group(4)), int(m.group(5))
            rows = []
            j = i + 1
            while (j < len(lines) and len(rows) < gh
                   and not lines[j].startswith("===") and not lines[j].startswith("== inv")):
                s = lines[j].strip()
                if s and all(ch in "#." for ch in s):
                    rows.append(s)
                j += 1
            cells = [(x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == "#"]
            if cells:
                cur[2].append({"ident": ident, "dname": dname, "tag": tag if tag else UNNAMED,
                               "cells": cells, "w": gw, "h": gh, "uid": len(cur[2])})
            i = j
            continue
        i += 1
    return sessions


def merge_same(items):
    """同类合并口径同 parse_dump.merge_same_type(= C# mergeRepIdx): ident + bbox + cells; 代表取首件。"""
    groups = {}
    for it in items:
        key = (it["ident"], it["w"], it["h"], tuple(sorted(it["cells"])))
        groups.setdefault(key, []).append(it)
    out = []
    for key, g in groups.items():
        rep = dict(g[0])
        rep["unit_count"] = len(g)
        out.append(rep)
    return out


def dump_sessions():
    out = []
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), BAK)
    for (W, H, raw) in parse_dump_tagged(path):
        items = merge_same(raw)
        if len(items) <= 1:
            continue
        out.append((W, H, items))
    return out


def catalog_tagmap():
    """stableId/nameEn/nameZh → itemTypes[0](= C# PrimaryTag), 缺 tag ⇒ U+FFFF(= TagKey 兜底)。"""
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "full_item_catalog.json")
    with open(path, encoding="utf-8") as f:
        cat = json.load(f)
    recs = cat["records"] if isinstance(cat, dict) else cat
    m = {}
    for r in recs:
        t = r.get("itemTypes") or []
        tag = t[0] if (isinstance(t, list) and t and t[0]) else UNNAMED
        for k in (r.get("stableId"), r.get("nameEn"), r.get("nameZh")):
            if k:
                m[k] = tag
    return m


def synth_sessions(sizes=((24, 10), (17, 10), (11, 14), (10, 10), (9, 7), (8, 8)),
                   fills=(0.85, 0.95), per=8, seed=42):
    """与 bench_native.synth_sessions 同尺寸/FILL 网格/种子; 额外按 catalog itemTypes[0] 打 tag。"""
    pool = catalog_pool()
    tmap = catalog_tagmap()
    rng = random.Random(seed)
    out = []
    tagged = tot = 0
    for (w, h) in sizes:
        cap = w * h
        feas = [it for it in pool if any(nw <= w and nh <= h for _c, nw, nh in bn.rotations_of(it))]
        if not feas:
            continue
        avg = sum(len(it["cells"]) for it in feas) / len(feas)
        for fill in fills:
            n = max(3, int(cap * fill) // max(1, int(avg)))
            for _ in range(per):
                merged = {}
                for it in (rng.choice(feas) for _ in range(n)):
                    k = (it["name"], tuple(sorted(it["cells"])), it["w"], it["h"])
                    if k not in merged:
                        r2 = dict(it)
                        tag = tmap.get(it["name"])
                        tot += 1
                        if tag:
                            tagged += 1
                        r2["tag"] = tag or UNNAMED
                        r2["ident"] = it["name"]
                        r2["dname"] = it["name"]
                        r2["uid"] = len(merged)
                        merged[k] = r2
                items = list(merged.values())
                if sum(len(i["cells"]) for i in items) > cap:
                    continue
                out.append((w, h, items))
    print(f"  [synth] catalog 件 tag 命中 {tagged}/{tot}"
          + ("  (0 命中 ⇒ 退化为单组, 需检查 name 字段口径)" if tot and not tagged else ""))
    return out


# ---------- 横带(banded)镜像 ----------
def has_support(occ, W, H, x, y, cells):
    for dx, dy in cells:
        cx, cy = x + dx, y + dy
        sup = (cy == 0) or (cx == 0)
        if not sup and cx - 1 >= 0:
            sup = (cx - 1, cy) in occ
        if not sup and cy - 1 >= 0:
            sup = (cx, cy - 1) in occ
        if not sup and cy + 1 < H:
            sup = (cx, cy + 1) in occ
        if not sup:
            return False
    return True


def has_support_self(occ, W, H, x, y, cells):
    """task-6 修复版 HasSupport: 每格需 cy==0 / cx==0 / 邻(左/上/下)已占 / 或邻格属于**本件自身**.
    v1 缺陷: 自身格不计支撑 ⇒ 厚件在空网格当首件时就放不下(2x2 在 (0,0): 格子(1,1)无支撑).
    仍非恒真: 漂浮孤立位(四邻无物且不在首行/首列)照旧不放, 保留"聚成实心块"的语义."""
    own = {(x + dx, y + dy) for dx, dy in cells}
    for dx, dy in cells:
        cx, cy = x + dx, y + dy
        sup = (cy == 0) or (cx == 0)
        if not sup and cx - 1 >= 0:
            sup = ((cx - 1, cy) in occ) or ((cx - 1, cy) in own)
        if not sup and cy - 1 >= 0:
            sup = ((cx, cy - 1) in occ) or ((cx, cy - 1) in own)
        if not sup and cy + 1 < H:
            sup = ((cx, cy + 1) in occ) or ((cx, cy + 1) in own)
        if not sup:
            return False
    return True


def place_first_fit(occ, W, H, ri, support):
    """task-6 ③ 全网格(y 外 x 内)取第一个"界内 + 空 + 支撑"的位(位置优先于朝向 = 原生扫描口径).
    用于带底压缩/回退: 增量 MFR 缓存丢空间、或带底以下没有空间时, 允许物品落到任何自支撑空位(仍不重叠/不压件).
    返回与 place_into 同形的元组; 不修改 occ(逐件标记由调用方负责)."""
    for py in range(H):
        for px in range(W):
            for o in range(4):
                cells, gw, gh = ri[o]
                if not cells or px + gw > W or py + gh > H:
                    continue
                if not can_place(occ, cells, px, py, W, H):
                    continue
                if not support(occ, W, H, px, py, cells):
                    continue
                cs = bn.cells_at(cells, px, py)
                bottom = max(py + dy + 1 for _dx, dy in cells)
                return px, py, o, bottom, cs, gw, gh
    return None


def shrink_rects(rects, px, py, pw, ph):
    nxt = []
    for (rx, ry, rw, rh) in rects:
        if (rx + rw <= px) or (px + pw <= rx) or (ry + rh <= py) or (py + ph <= ry):
            nxt.append((rx, ry, rw, rh))
            continue
        if px > rx:
            nxt.append((rx, ry, px - rx, rh))
        if px + pw < rx + rw:
            nxt.append((px + pw, ry, rx + rw - (px + pw), rh))
        cx1 = max(rx, px)
        cx2 = min(rx + rw, px + pw)
        if cx1 < cx2:
            if py > ry:
                nxt.append((cx1, ry, cx2 - cx1, py - ry))
            if py + ph < ry + rh:
                nxt.append((cx1, py + ph, cx2 - cx1, ry + rh - (py + ph)))
    kept = []
    for i, r in enumerate(nxt):
        covered = any(j != i and o[0] <= r[0] and o[1] <= r[1]
                      and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]
                      for j, o in enumerate(nxt))
        if not covered:
            kept.append(r)
    rects[:] = kept


def find_free_spot_cells(rects, occ, W, H, cells, gw, gh, minY, support=None):
    if support is None:
        support = has_support
    if gw > W or gh > H:
        return None
    topY = max(0, minY)
    best = None
    for (rx, ry, rw, rh) in rects:
        if rw < gw or rh < gh or ry + rh <= topY:
            continue
        px = rx
        py = max(ry, topY)
        if py + gh > ry + rh:
            continue
        if not can_place(occ, cells, px, py, W, H):
            continue
        if not support(occ, W, H, px, py, cells):
            continue
        if best is None or py < best[1] or (py == best[1] and px < best[0]):
            best = (px, py)
    return best


def place_into(occ, W, H, ri, minY, rects, support=None):
    if support is None:
        support = has_support
    bx = -1
    by = -1
    bo = 0
    square = (ri[0][1] == ri[0][2])
    for i2 in range(2):
        minY2 = minY if i2 == 0 else 0
        for j in range(4):
            cells, gw, gh = ri[j]
            if cells:
                r = find_free_spot_cells(rects, occ, W, H, cells, gw, gh, minY2, support)
                if r is not None and (bx < 0 or r[1] < by or (r[1] == by and r[0] < bx)):
                    bx, by, bo = r[0], r[1], j
                if j == 0 and square:
                    break
        if bx >= 0 or minY <= 0:
            break
    if bx < 0:
        return None
    cells = ri[bo][0]
    cs = bn.cells_at(cells, bx, by)
    bottom = max(by + dy + 1 for _dx, dy in cells)
    return bx, by, bo, bottom, cs, ri[bo][1], ri[bo][2]


def size_compare(a, b):
    for ka, kb in ((a["w"] * a["h"], b["w"] * b["h"]),
                   (max(a["w"], a["h"]), max(b["w"], b["h"])),
                   (min(a["w"], a["h"]), min(b["w"], b["h"]))):
        if ka != kb:
            return -1 if ka > kb else 1
    for ka, kb in ((_ord_ic(a["ident"]), _ord_ic(b["ident"])),
                   (_ord_ic(a["dname"]), _ord_ic(b["dname"]))):
        if ka != kb:
            return -1 if ka < kb else 1
    return (a["uid"] > b["uid"]) - (a["uid"] < b["uid"])


def tag_order(items):
    tags = []
    for it in items:
        if it["tag"] not in tags:
            tags.append(it["tag"])
    tags.sort(key=_ord_ic)
    return tags


def layout_banded(rot, items, W, H, support=None, rescan=False, fallback=False):
    """镜像 LayoutBanded(v1) + task-6 三个修复开关:
      support  = has_support_self ⇒ 修"自支撑"缺陷(厚件/首件可放);
      rescan   = ① 带内落位失败后【重算 MFR】再试(增量 ShrinkRects 切块会丢可用空间);
      fallback = ② 仍失败则全网格自支撑 first-fit(带底压缩/回退: 允许落到带区之外的任意自支撑空位).
    逐 tag 连续带语义不变(同 tag 件按 SizeCompare 连续排, 带按 tagOrder 依序向下堆). 任一件彻底放不下 ⇒ None。"""
    if support is None:
        support = has_support
    occ = set()
    rects = bn.find_free_rects(occ, W, H)
    pos = {}
    floor = 0
    for tag in tag_order(items):
        band = [i for i, it in enumerate(items) if it["tag"] == tag]
        band.sort(key=cmp_to_key(lambda i, j: size_compare(items[i], items[j])))
        newfloor = floor
        for i in band:
            r = place_into(occ, W, H, rot[i], floor, rects, support)
            if r is None and rescan:
                rects = bn.find_free_rects(occ, W, H)
                r = place_into(occ, W, H, rot[i], floor, rects, support)
            used_fallback = False
            if r is None and fallback:
                r = place_first_fit(occ, W, H, rot[i], support)
                used_fallback = r is not None
            if r is None:
                return None
            bx, by, bo, bottom, cs, pw, ph = r
            pos[i] = (bx, by, bo)
            occ |= cs
            if used_fallback:
                rects = bn.find_free_rects(occ, W, H)   # 落点在缓存之外 ⇒ 缓存重算, 保持"缓存 ⊆ 空闲"
            else:
                shrink_rects(rects, bx, by, pw, ph)
            if bottom > newfloor:
                newfloor = bottom
        floor = newfloor
    return pos


# ---------- 指标 / 不变量 ----------
def fill_bucket(fill):
    for lo, hi in FILL_BUCKETS:
        if lo <= fill < hi:
            return f"{lo:.2f}-{hi:.2f}"
    return "?"


def invariants(rot, W, H, pos, init_pos):
    """安全不变量: 越界 / 落地重叠 / 压住未移动件(= 不在新布局里的旧布局件)。返回 (oob, overlap, on_unmoved)。"""
    seen = set()
    oob = ov = 0
    for i, (px, py, o) in pos.items():
        cs = bn.cells_at(rot[i][o][0], px, py)
        for (cx, cy) in cs:
            if cx < 0 or cy < 0 or cx >= W or cy >= H:
                oob += 1
        if cs & seen:
            ov += 1
        seen |= cs
    on = 0
    for i in init_pos:
        if i in pos:
            continue
        ipx, ipy, io = init_pos[i]
        if bn.cells_at(rot[i][io][0], ipx, ipy) & seen:
            on += 1
    return oob, ov, on


def eval_sessions(sessions):
    keys = ("sess", "ok", "partial", "dense_fail", "empty_before", "empty_after",
            "improve", "tie", "worse", "adopt_sess", "adopt_cnt", "tucked",
            "iso_before", "iso_after", "frag_before", "frag_after",
            "touch_before", "touch_after", "ts_before", "ts_after",
            "oob", "overlap", "on_unmoved", "dense_base_empty", "dense_new_empty",
            "dense_base_dd", "dense_new_dd")
    st = dict.fromkeys(keys, 0)
    st["mv_before"] = 0.0
    st["mv_after"] = 0.0
    by_fill = {}
    for (W, H, items) in sessions:
        st["sess"] += 1
        rot = [bn.rots_of(it) for it in items]
        n = len(items)
        fill = sum(len(it["cells"]) for it in items) / float(W * H)
        b = by_fill.setdefault(fill_bucket(fill), [0, 0])
        b[0] += 1
        bl = bn.base_layout(rot, W, H)
        if bl is None:
            st["dense_fail"] += 1
            continue
        init_pos = bl[0]
        banded = layout_banded(rot, items, W, H)
        if banded is None:
            continue
        st["ok"] += 1
        b[1] += 1
        if len(banded) != n:
            st["partial"] += 1
        occ_b = bn.occ_of(rot, banded)
        occ_a = None
        before = max_empty(occ_b, W, H)[0]
        after_pos, dd = bn.refine(rot, W, H, banded, guard=True)
        occ_a = bn.occ_of(rot, after_pos)
        after = max_empty(occ_a, W, H)[0]
        st["empty_before"] += before
        st["empty_after"] += after
        if after > before:
            st["improve"] += 1
        elif after == before:
            st["tie"] += 1
        else:
            st["worse"] += 1
        if dd:
            st["adopt_sess"] += 1
            st["adopt_cnt"] += dd
        st["tucked"] += sum(1 for i in after_pos if after_pos[i] != banded[i])
        for p in (banded, after_pos):
            oob, ov, on = invariants(rot, W, H, p, init_pos)
            st["oob"] += oob
            st["overlap"] += ov
            st["on_unmoved"] += on
        for tag, p, o in (("before", banded, occ_b), ("after", after_pos, occ_a)):
            st[f"iso_{tag}"] += bn.isolated_small(rot, o, W, H, p)
            st[f"frag_{tag}"] += len(bn.find_free_rects(o, W, H))
            for i, (px, py, oo) in p.items():
                t = bn.touch(o, W, H, rot[i][oo][0], px, py)
                st[f"touch_{tag}"] += t
                if len(rot[i][oo][0]) <= SMALL_MAX:
                    st[f"ts_{tag}"] += t
            st[f"mv_{tag}"] += bn.moved_ratio(init_pos, p)
        # 对照(同子集): 密集路径现发布语义 baseline 与 task-3 new
        bpos, bdd = bn.refine(rot, W, H, init_pos, guard=True)
        st["dense_base_empty"] += max_empty(bn.occ_of(rot, bpos), W, H)[0]
        if bdd:
            st["dense_base_dd"] += bdd
        npos, _no, _left = bn.native_pass(rot, [it["ident"] for it in items], W, H, init_pos)
        nref, ndd = bn.refine(rot, W, H, npos, guard=True)
        st["dense_new_empty"] += max_empty(bn.occ_of(rot, nref), W, H)[0]
        st["dense_new_dd"] += ndd
    return st, by_fill


def report(label, sessions):
    print()
    print("=" * 78)
    print(f"=== {label} | 会话 {len(sessions)} ===")
    st, by_fill = eval_sessions(sessions)
    ok = st["ok"]
    print(f"  grouped 成功(能出横带) = {ok}/{st['sess']}"
          f"  ({100.0 * ok / max(1, st['sess']):.1f}%)   ⇒ 这些会话默认【不走 LayoutDense】"
          f"  剩 {st['sess'] - ok} 会话落密集路径"
          + (f"  [其中 base_layout 不可行 {st['dense_fail']}]" if st["dense_fail"] else ""))
    print("  分档(fill = 占格/(W*H)): " + "  ".join(
        f"{k}:{v[1]}/{v[0]}({100.0 * v[1] / max(1, v[0]):.0f}%)" for k, v in sorted(by_fill.items())))
    if not ok:
        return st
    print("  --- 精修前后(仅 grouped 成功子集) ---")
    rows = [("最大空矩和", st["empty_before"], st["empty_after"], ">= (硬判据)"),
            ("拆散会话/次数", st["adopt_sess"], st["adopt_cnt"], "= 0 (硬判据)"),
            ("孤立小件数", st["iso_before"], st["iso_after"], ""),
            ("碎片(MFR)数", st["frag_before"], st["frag_after"], ""),
            ("贴邻和", st["touch_before"], st["touch_after"], ""),
            ("小件贴邻和", st["ts_before"], st["ts_after"], ""),
            ("安全失败(越界/重叠/压未动)", st["oob"] + st["overlap"] + st["on_unmoved"],
             st["oob"] + st["overlap"] + st["on_unmoved"], "= 0 (硬判据)")]
    for name, b, a, note in rows:
        print(f"    {name:26s} {b:>8} -> {a:>8}  Δ{a - b:+}   {note}")
    print(f"    {'平均移动比例':26s} {100.0 * st['mv_before'] / ok:>7.1f}% -> "
          f"{100.0 * st['mv_after'] / ok:>6.1f}%  (以 base 骨架为原始位口径)")
    print(f"    精修采纳件数(塞缝件) = {st['tucked']}  逐会话 严格改善 {st['improve']} / 持平 {st['tie']}"
          f" / 劣化 {st['worse']}   部分落位会话 {st['partial']}")
    print(f"    安全失败明细: 越界 {st['oob']} / 落地重叠 {st['overlap']} / 压住未移动件 {st['on_unmoved']}")
    print("  --- 同子集: 密集路径对照(bench_native 口径) ---")
    print(f"    密集 baseline(现发布语义) 空矩和 = {st['dense_base_empty']}  拆散 {st['dense_base_dd']} 次")
    print(f"    密集 new(task-3 原生首选) 空矩和 = {st['dense_new_empty']}  拆散 {st['dense_new_dd']} 次")
    print(f"    横带(before) = {st['empty_before']} / 横带(after 精修) = {st['empty_after']}")
    return st


# ---------- task-6: grouped 修复版(v2) + 容差曲线 ----------
def eval_v2(sessions, tols=(0.0, 0.01, 0.02, 0.03, 0.04, 0.05)):
    """逐会话计算:
      dense  = task-5 语义(旧候选与原生候选同池择优 = bench_native 的 pick_best strict)后的精修结果;
      grouped = banded v2(自支撑 + 重算 MFR + 全网格回退)后的精修结果;
      v1/s1/s2 = 逐项隔离(①原语义 ②+自支撑 ③+重算 MFR ④+全网格回退)的成功率来源;
      终选(tol) = grouped 全放 且 area_g >= area_d - tol*area_d ? grouped : dense。
    返回 (每 tol 的终选指标, grouped v2 指标, 分档, 逐项成功率)。"""
    sup = has_support_self
    keys = ("sess", "full", "empty", "iso", "touch", "ts", "frag", "inv", "gsel")
    st = {t: dict.fromkeys(keys, 0) for t in tols}
    for t in tols:
        st[t]["mv"] = 0.0
    g2 = dict.fromkeys(("sess", "ok", "empty", "frag", "touch", "ts", "iso", "inv", "gain",
                        "probe", "probe_sess"), 0)
    g2["mv"] = 0.0
    dp = dict.fromkeys(("sess", "probe", "probe_sess"), 0)   # dense 拆散探针(同口径)
    dn = dict.fromkeys(("sess", "empty", "full", "inv"), 0)  # 仅 dense(代理口径 = task-5 pick_best): 校准用
    pk = dict.fromkeys(("sess", "n", "lo_sum", "ge", "lt", "err"), 0)  # 密集候选下界探针
    t_pk = time.time()
    by_fill = {}
    step = {"v1": 0, "s1": 0, "s2": 0, "v2": 0}
    for (W, H, items) in sessions:
        rot = [bn.rots_of(it) for it in items]
        n = len(items)
        bl = bn.base_layout(rot, W, H)
        if bl is None:
            continue
        init_pos = bl[0]
        g2["sess"] += 1
        fill = sum(len(it["cells"]) for it in items) / float(W * H)
        b = by_fill.setdefault(fill_bucket(fill), [0, 0])
        b[0] += 1
        # 逐项隔离: ① 原语义 ② +自支撑 ③ +重算 MFR ④ +全网格回退
        if layout_banded(rot, items, W, H) is not None:
            step["v1"] += 1
        if layout_banded(rot, items, W, H, support=sup) is not None:
            step["s1"] += 1
        if layout_banded(rot, items, W, H, support=sup, rescan=True) is not None:
            step["s2"] += 1
        gpos = layout_banded(rot, items, W, H, support=sup, rescan=True, fallback=True)
        if gpos is not None and len(gpos) == n:
            step["v2"] += 1
        # dense = task-5 pick_best(strict): baseline(init+精修) vs 原生候选(全放才入选)
        on_pos, _d1 = bn.refine(rot, W, H, init_pos, guard=True)
        npos, _o, leftover = bn.native_pass(rot, [it["ident"] for it in items], W, H, init_pos)
        nref_pos, _d2 = bn.refine(rot, W, H, npos, guard=True)
        a_old = max_empty(bn.occ_of(rot, on_pos), W, H)[0]
        a_nat = max_empty(bn.occ_of(rot, nref_pos), W, H)[0]
        d_pos = nref_pos if ((not leftover) and a_nat > a_old) else on_pos
        a_d = max_empty(bn.occ_of(rot, d_pos), W, H)[0]
        # 拆散探针(同 bench_native 口径): 在终局上再跑 guard-OFF 精修, 计其"贴邻下降"采纳数
        _q, dd_d = bn.refine(rot, W, H, d_pos, guard=False)
        dp["sess"] += 1
        dp["probe"] += dd_d
        dp["probe_sess"] += 1 if dd_d else 0
        dn["sess"] += 1
        dn["empty"] += a_d
        dn["full"] += 1 if len(d_pos) == n else 0
        dn["inv"] += sum(invariants(rot, W, H, d_pos, init_pos))
        # grouped v2 (+同一精修层)
        g_pos = None
        a_g = -1
        if gpos is not None and len(gpos) == n:
            g2["ok"] += 1
            b[1] += 1
            g_pos, _d3 = bn.refine(rot, W, H, gpos, guard=True)
            o = bn.occ_of(rot, g_pos)
            a_g = max_empty(o, W, H)[0]
            g2["empty"] += a_g
            g2["frag"] += len(bn.find_free_rects(o, W, H))
            g2["iso"] += bn.isolated_small(rot, o, W, H, g_pos)
            for i, (px, py, oo) in g_pos.items():
                tt = bn.touch(o, W, H, rot[i][oo][0], px, py)
                g2["touch"] += tt
                if len(rot[i][oo][0]) <= SMALL_MAX:
                    g2["ts"] += tt
            g2["mv"] += bn.moved_ratio(init_pos, g_pos)
            g2["inv"] += sum(invariants(rot, W, H, g_pos, init_pos))
            g2["gain"] += a_g - a_d
            _p, dd_g = bn.refine(rot, W, H, g_pos, guard=False)
            g2["probe"] += dd_g
            g2["probe_sess"] += 1 if dd_g else 0
        # 密集候选下界(严谨): verify_all 的 6 个布局器端口(未精修, occ 口径) ⇒ 真实 LayoutDense
        # (6 候选各自过精修层后取 max 空矩) 必然 >= 此下界 ⇒ a_g < 下界 的会话在生产中必定不选 grouped.
        pk["sess"] += 1
        if a_g >= 0 and (time.time() - t_pk) < 600.0:
            try:
                lo = -1
                for fn in (va.pack_bestfit_mfr, va.pack_grow_touch, va.pack_guillotine,
                           va.pack_left_bottom, va.pack_minhole_stack, va.pack_pg_split):
                    o_pk = fn(items, W, H)
                    if o_pk:
                        e_pk = max_empty(o_pk, W, H)[0]
                        if e_pk > lo:
                            lo = e_pk
            except Exception:
                lo = -1
            if lo < 0:
                pk["err"] += 1
            else:
                pk["n"] += 1
                pk["lo_sum"] += max(lo, a_d)
                if a_g >= lo:
                    pk["ge"] += 1
                else:
                    pk["lt"] += 1
        for t in tols:
            take_g = (a_g >= 0) and (a_g >= a_d - a_d * t)
            pos = g_pos if take_g else d_pos
            s = st[t]
            s["sess"] += 1
            if len(pos) == n:
                s["full"] += 1
            occ2 = bn.occ_of(rot, pos)
            s["empty"] += max_empty(occ2, W, H)[0]
            s["frag"] += len(bn.find_free_rects(occ2, W, H))
            s["iso"] += bn.isolated_small(rot, occ2, W, H, pos)
            for i, (px, py, oo) in pos.items():
                tt = bn.touch(occ2, W, H, rot[i][oo][0], px, py)
                s["touch"] += tt
                if len(rot[i][oo][0]) <= SMALL_MAX:
                    s["ts"] += tt
            s["mv"] += bn.moved_ratio(init_pos, pos)
            s["inv"] += sum(invariants(rot, W, H, pos, init_pos))
            if take_g:
                s["gsel"] += 1
    return st, g2, by_fill, step, dp, dn, pk


def report_v2(label, sessions):
    print()
    print("=" * 78)
    print(f"=== task-6 | {label} ===")
    st, g2, by_fill, step, dp, dn, pk = eval_v2(sessions)
    s0 = g2["sess"]
    print(f"  grouped 成功率逐项隔离: ①v1原语义 {step['v1']}/{s0} ({100.0*step['v1']/max(1,s0):.1f}%)"
          f" → ②+自支撑 {step['s1']}/{s0} ({100.0*step['s1']/max(1,s0):.1f}%)"
          f" → ③+重算MFR {step['s2']}/{s0} ({100.0*step['s2']/max(1,s0):.1f}%)"
          f" → ④+全网格回退 {step['v2']}/{s0} ({100.0*step['v2']/max(1,s0):.1f}%)")
    print("  分档(fill, v2): " + "  ".join(
        f"{k}:{v[1]}/{v[0]}({100.0*v[1]/max(1,v[0]):.0f}%)" for k, v in sorted(by_fill.items())))
    ok = step["v2"]
    if ok:
        print(f"  grouped v2 组内指标(精修后): 空矩和 {g2['empty']} 碎片 {g2['frag']} 贴邻和 {g2['touch']} "
              f"小件贴邻 {g2['ts']} 孤立小件 {g2['iso']} 安全失败 {g2['inv']} churn "
              f"{100.0*g2['mv']/max(1,ok):.1f}% | 相对 dense 空矩差合计 {g2['gain']:+}")
    print(f"  拆散(同 bench_native 探针口径 = 终局再跑 guard-OFF 精修的可采纳贴邻下降数): "
          f"grouped v2 {g2['probe_sess']}会话/{g2['probe']}件 (在 {ok} 个成功后布局上) | "
          f"dense {dp['probe_sess']}会话/{dp['probe']}件 (在 {dp['sess']} 个会话上)")
    print(f"  管线自身拆散采纳 = 0/0 (精修层 guard ON: cand.touch < cur.touch 一律拒绝 ⇒ 恒 0)")
    print(f"  仅密集(dense 代理口径 = task-5 pick_best; 真实 dump 应复现空矩和 20273): 空矩和 {dn['empty']} "
          f"全放 {dn['full']}/{dn['sess']} 安全失败 {dn['inv']}  (本行 = tol 曲线的对照基准)")
    if pk["n"]:
        print(f"  密集候选下界探针(verify_all 6 布局器端口, 未精修 ⇒ 真实密集空矩 >= max(下界, 代理)): "
              f"有效 {pk['n']}/{pk['sess']} 会话(异常/超预算 {pk['err']}), 下界空矩和 {pk['lo_sum']}; "
              f"grouped a_g >= 下界 {pk['ge']} 会话(生产可能选 grouped 的上界) / "
              f"a_g < 下界 {pk['lt']} 会话(生产中必定走密集)")
    else:
        print(f"  密集候选下界探针: 无有效样本(err={pk['err']}, sess={pk['sess']})")
    print("  --- 终选(tol 曲线): grouped 选取率 vs 空矩代价 ---")
    print("    (tol>0 允许横带用一点空矩换聚带; 判据线 20273 来自 task-5 密集口径, 对 catalog 组无意义)")
    print(f"    {'tol':>6}{'grouped选取':>14}{'空矩和':>10}{'Δ vs 20273线':>14}{'碎片':>7}{'贴邻和':>8}"
          f"{'小件贴邻':>9}{'孤立小件':>9}{'安全失败':>9}{'全放':>10}{'churn':>8}")
    for t in sorted(st):
        s = st[t]
        print(f"    {t*100:>5.0f}%{str(s['gsel']) + '/' + str(s['sess']):>14}{s['empty']:>10}"
              f"{s['empty'] - 20273:>14}{s['frag']:>7}{s['touch']:>8}{s['ts']:>9}{s['iso']:>9}"
              f"{s['inv']:>9}{str(s['full']) + '/' + str(s['sess']):>10}"
              f"{100.0*s['mv']/max(1,s['sess']):>7.1f}%")
    return st, g2, by_fill, step, dp, dn, pk


def main():
    print("=== 横带(banded)路径对拍: grouped 占比 + 精修收益 ===")
    print("(真实 dump + catalog 合成组; 精修层 = bench_native.refine = Core.cs TryFillRefine guard 路径)")
    d = dump_sessions()
    sd = report(f"真实 dump {BAK}", d)
    s = synth_sessions()
    ss = report("catalog 合成组", s)
    # task-6: grouped 修复版成功率(逐项隔离) + 容差曲线
    report_v2(f"真实 dump {BAK}", d)
    report_v2("catalog 合成组", s)
    print()
    print("=== 判据 ===")
    for label, st in (("真实 dump", sd), ("catalog", ss)):
        if not st or not st["ok"]:
            print(f"  [{label}] grouped 成功 0 ⇒ 无法评估(全走密集路径)")
            continue
        inv = st["oob"] + st["overlap"] + st["on_unmoved"]
        p1 = "PASS" if inv == 0 else f"FAIL({inv})"
        p2 = "PASS" if st["empty_after"] >= st["empty_before"] else "FAIL"
        p3 = "PASS" if st["improve"] > 0 else "FAIL(零改善)"
        p4 = "PASS" if st["adopt_cnt"] == 0 else f"FAIL(拆散 {st['adopt_cnt']})"
        print(f"  [{label}] 安全不变量失败=0 {p1} | 空矩和不劣化 {p2} | 至少一会话严格改善 {p3}"
              f" | 拆散保持 0 {p4}")


if __name__ == "__main__":
    main()
