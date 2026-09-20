# -*- coding: utf-8 -*-
"""克隆对合并的等价性回归: C# 的 TryGrowTouch / TryLeftBottom 合并为
ScanSinglePieces(singles, grid, pick, out dict) 后, 必须保证两条分支的输出
与合并前逐位相同.

合并前的两个函数 58 行中 52 行逐字相同, 仅 4 处不同(函数名 / bestTouch 状态 /
选择谓词 / bestTouch 赋值). 合并 = 提取公共骨架 + 参数化谓词, 不改变三层枚举顺序
(o→py→px) 与 CellsFree 短路时机 —— 那两项是 tie-break 的全部来源, 一旦漂移输出即变.

本脚本做三层比对, 逐层加严:
  1) 与 verify_all.py 既有两个独立镜像(pack_grow_touch / pack_left_bottom) 比 occ 集合;
  2) 与本脚本内的「合并前逐字基线镜像」比 occ 集合;
  3) 与该基线镜像比 **per-item Placement 字典 (item -> (x, y, o))**.

第 3 层是必需的: 只比 occ 集合时, 两个布局若物品互换位置而占用集相同会被误判为
一致(verifier 指出的固有漏检). 比 placement 字典则能抓到。

用法: python verify_clone_merge.py    退出码 0 = 全等价, 1 = 有差异.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump
import verify_all as va


# ---------------------------------------------------------------------------
# 合并前基线镜像: 逐字对应合并前的 TryGrowTouch / TryLeftBottom 两个独立函数.
# 返回 (occ_set, placements) —— placements 为 {item_index: (x, y, o_index)}.
# o 用 0..3 朝向**下标**(不去重), 与 C# 的 CellsOf(m, o) 语义一致.
# ---------------------------------------------------------------------------

def _rotations_raw(it, W, H):
    """C# CellsOf(m, o) 对 o=0..3 的原样展开(不去重), 含 gw/gh 与越界过滤."""
    c0 = it["cells"]
    gw0, gh0 = it["w"], it["h"]
    out = []
    cur, wd, hd = list(c0), gw0, gh0
    for o in range(4):
        if o > 0:
            cur = [(wd - 1 - y, x) for x, y in cur]
            wd, hd = hd, wd
        mx = min(x for x, y in cur)
        my = min(y for x, y in cur)
        cells = [(x - mx, y - my) for x, y in cur]
        gw = (gh0 if (o == 1 or o == 3) else gw0)
        gh = (gw0 if (o == 1 or o == 3) else gh0)
        out.append((o, cells, gw, gh))
    return out


def _cells_free(occ, cells, px, py):
    return all((px + dx, py + dy) not in occ for dx, dy in cells)


def _touch(occ, W, H, cells, px, py):
    t = 0
    for dx, dy in cells:
        cx, cy = px + dx, py + dy
        if cx == 0 or cx == W - 1:
            t += 1
        if cy == 0 or cy == H - 1:
            t += 1
        if cx > 0 and (cx - 1, cy) in occ:
            t += 1
        if cx < W - 1 and (cx + 1, cy) in occ:
            t += 1
        if cy > 0 and (cx, cy - 1) in occ:
            t += 1
        if cy < H - 1 and (cx, cy + 1) in occ:
            t += 1
    return t


def _baseline(items, W, H, grow):
    """合并前单个布局器的忠实镜像. grow=True 复现 TryGrowTouch, False 复现 TryLeftBottom."""
    order = sorted(range(len(items)), key=lambda i: -len(items[i]["cells"]))
    occ = set()
    placements = {}
    for idx in order:
        it = items[idx]
        bestTouch = -1
        bestX = bestY = -1
        bestO = 0
        bestCells = None
        for o, cells, gw, gh in _rotations_raw(it, W, H):
            if not cells or gw > W or gh > H:
                continue
            for py in range(H - gh + 1):
                for px in range(W - gw + 1):
                    if not _cells_free(occ, cells, px, py):
                        continue
                    if grow:
                        touch = _touch(occ, W, H, cells, px, py)
                        if touch > bestTouch or (
                            touch == bestTouch and (py < bestY or (py == bestY and px < bestX))
                        ):
                            bestTouch = touch
                            bestX, bestY, bestO, bestCells = px, py, o, cells
                    else:
                        if bestX < 0 or px < bestX or (px == bestX and py > bestY):
                            bestX, bestY, bestO, bestCells = px, py, o, cells
        if bestX < 0:
            return None, None
        placements[idx] = (bestX, bestY, bestO)
        for dx, dy in bestCells:
            occ.add((bestX + dx, bestY + dy))
    return occ, placements


# ---------------------------------------------------------------------------
# 合并后镜像: 逐行对应 C# 的 ScanSinglePieces(singles, grid, pick, out dict).
# ---------------------------------------------------------------------------

def _merged(items, W, H, pick):
    order = sorted(range(len(items)), key=lambda i: -len(items[i]["cells"]))
    occ = set()
    placements = {}
    grow = pick == "grow_touch"
    for idx in order:
        it = items[idx]
        bestTouch = -1
        bestX = bestY = -1
        bestO = 0
        bestCells = None
        for o, cells, gw, gh in _rotations_raw(it, W, H):
            if not cells or gw > W or gh > H:
                continue
            for py in range(H - gh + 1):
                for px in range(W - gw + 1):
                    if not _cells_free(occ, cells, px, py):
                        continue
                    touch = 0
                    if grow:
                        touch = _touch(occ, W, H, cells, px, py)
                        better = touch > bestTouch or (
                            touch == bestTouch and (py < bestY or (py == bestY and px < bestX))
                        )
                    else:
                        better = bestX < 0 or px < bestX or (px == bestX and py > bestY)
                    if better:
                        if grow:
                            bestTouch = touch
                        bestX, bestY, bestO, bestCells = px, py, o, cells
        if bestX < 0:
            return None, None
        placements[idx] = (bestX, bestY, bestO)
        for dx, dy in bestCells:
            occ.add((bestX + dx, bestY + dy))
    return occ, placements


def _key(occ):
    return frozenset(occ) if occ else None


def main():
    sessions = parse_dump()
    if not sessions:
        print("FAIL: 语料为空 (tscripts/inv_shape_dump.bak_* 是否存在?)")
        return 1

    n = 0
    fail = 0
    counts = {"va_occ": 0, "base_occ": 0, "base_place": 0}
    for W, H, items in sessions:
        if not items:
            continue
        n += 1
        for pick, va_fn in (("grow_touch", va.pack_grow_touch), ("left_bottom", va.pack_left_bottom)):
            b_occ, b_place = _baseline(items, W, H, pick == "grow_touch")
            m_occ, m_place = _merged(items, W, H, pick)
            v_occ = va_fn(items, W, H)

            if _key(b_occ) != _key(v_occ):
                counts["va_occ"] += 1
            if _key(b_occ) != _key(m_occ):
                counts["base_occ"] += 1
            # 第三层: per-item placement (能抓到 occ 相同但物品互换的情况)
            if b_place != m_place:
                counts["base_place"] += 1

    print(f"语料会话: {n}  ({n * 2} 次逐分支比对)")
    print(f"  1) 基线镜像 occ  vs verify_all 独立镜像 occ : 不一致 {counts['va_occ']}")
    print(f"  2) 合并后 occ     vs 基线镜像 occ           : 不一致 {counts['base_occ']}")
    print(f"  3) 合并后 placement vs 基线镜像 placement   : 不一致 {counts['base_place']}")
    fail = sum(counts.values())
    if fail:
        print(f"\nFAIL: 共 {fail} 处不一致 — 骨架枚举顺序/CellsFree 短路时机/tie-break 已漂移.")
        return 1
    print("\nPASS: 三层比对全部一致 (含 per-item placement, 排除物品互换漏检).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
