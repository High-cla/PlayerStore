# -*- coding: utf-8 -*-
"""原生语义首选落位(LayoutNativeFirstFit)离线对拍.

对照对象
  init         = 基线布局骨架: 占格数降序 + 朝向 0..3 + 行主序 first-fit(与 bench_fillrefine.base_layout 同序)
  baseline_off = init + FillRefine(guard OFF)  ← bench_fillrefine.py 已发布版的缺陷路径(空矩优先, 会拆散贴簇小件)
  baseline     = init + FillRefine(guard ON)   ← 当前已发布语义(采纳需 空矩不降 且 贴邻不降)
  native    = 新语义(镜像 Core.cs LayoutNativeFirstFit): 物品序 = 占格数降序 → identifier(Ordinal) →
              当前格行主序下标; 逐件"外 y 内 x"行主序从 (0,0) 扫描取【第一个】能放的位置
              (无评分、无 waste/最大空矩比较); 扫描域 [0, W-bboxW] × [0, H-bboxH]; 锚点 = 形状 min 角;
              单层占用(等价原生 itemLayers 全 0/1 掩码; §5.2/§5.3 的 1<<(v&31) 位运算退化为布尔占用).
              语义证据 docs/NATIVE_SORT_SPEC.md §3.2/§5/§6/§7/§9.
  native+ref / soft+ref / new = Core.cs 实际组合:
              native 全放 → native + FillRefine(guard ON);  native 有 leftover → 残局(TryResidualLayout 镜像)+ 精修
  对照组(隔离次序键各键贡献, 三者扫描与精修完全相同):
    A_native_nokey  = native 扫描 + 只按占格数降序(index 稳定)      → 与 baseline 比 = 次序键整体贡献
    A2_nokey3       = native 扫描 + (占格数, identifier, index)      → 与 native 比 = 第三键(行主序)贡献
    B_base_natkey   = 基线扫描 + 原生次序键(含第三键)                → 与 baseline 比 = 次序键整体贡献(反向)

指标口径
  拆散(采纳) = 管线自身采纳的"贴邻下降"移动数/会话数. guard OFF 管线 = bench_fillrefine 的 moved_down(61/103);
               guard ON 管线恒 0 —— 这是 Lead 基线口径下的"拆散保持 0"判据.
  拆散(探针) = 在最终布局上再跑一遍 FillRefine(guard OFF) 看它还想挪动几件(纯诊断).
  最大空矩和 = Σ_sessions 最大连续空闲矩形面积(基线 = 20056; 旧算法正是直接优化这个量).
  贴邻和     = Σ_sessions Σ_items 贴邻分(含贴壁) —— 越大 = 越贴合.
  小件贴邻和 = 同上, 只统计占格<=2 的小件 —— 直接对应"小件有没有塞进缝隙".
  碎片数     = Σ_sessions |最大空闲矩形集合| —— 越小 = 剩余空间越整.
  孤立小件   = 占格<=2 且 四邻无已占格 且 不贴任何壁(漂在空域中).
  平均移动   = 相对 init 位置(x,y,o 有变)的件数比例.

建模说明(未确证/近似处, 明确标注)
  1. dump 无"当前坐标": 用 init 布局作为排序前初始态近似 —— 保证初始态可行, 并提供 native 排序键第三项
     (当前格行主序下标)与"移动比例"参照. 真实游戏里初始态是玩家乱序摆放, 会更散 → 本处移动比例偏保守;
     第三键的值也因此与真实游戏不同(见 A2_nokey3 对照组).
  2. native 首试朝向 = 初始态朝向(原生 §3.2 排序不改朝向); 失败再试其余朝向(我方宽容, 换取填充率).
  3. native 在空网格上跑(itemLayers 全 0, 原生 §3.2 同样不预订当前占位) —— 与 C# 主路径一致.
"""
import os
import sys
import random

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump_merged
from verify_all import can_place, mark, max_empty, rotations_of, catalog_pool
import bench_online

BAK = "inv_shape_dump.bak_20260830_231609"
SMALL_MAX = 2            # "小件" = 占格数 <= 2
REFINE_MAX_CELLS = 5000  # 与 Core.cs TryFillRefine 同阈值: (long)W*H > 5000 跳过精修


# ---------- 几何工具 ----------
def rots_of(item):
    """Core.cs BuildMask 语义: C0..C3 = 0/90/180/270(始终 4 个, 允许重复)."""
    return bench_online.rotations(item["cells"], item["w"], item["h"])


def cells_at(cs, px, py):
    return {(px + dx, py + dy) for dx, dy in cs}


def touch(occ, W, H, cells, px, py):
    """贴邻分(含贴壁): 与 bench_fillrefine.touch / Core.cs TouchCount 同口径."""
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


def find_free_rects(occ, W, H):
    """最大空闲矩形(MFR): 直方图 + 单调栈, 去包含. 同 bench_fillrefine / Core.cs FindFreeRects."""
    rects = []
    height = [0] * W
    for y in range(H):
        for x in range(W):
            height[x] = height[x] + 1 if (x, y) not in occ else 0
        stack = []
        for x in range(W + 1):
            cur = 0 if x == W else height[x]
            while stack and height[stack[-1]] >= cur:
                h = height[stack.pop()]
                left = 0 if not stack else stack[-1] + 1
                if h > 0:
                    rects.append((left, y - h + 1, x - left, h))
            stack.append(x)
    kept = []
    for i, r in enumerate(rects):
        rx, ry, rw, rh = r
        cov = False
        for j, o in enumerate(rects):
            if i == j:
                continue
            ox, oy, ow, oh = o
            if ox <= rx and oy <= ry and ox + ow >= rx + rw and oy + oh >= ry + rh:
                cov = True
                break
        if not cov:
            kept.append(r)
    return kept


def occ_of(rot, pos):
    occ = set()
    for i, (px, py, o) in pos.items():
        occ |= cells_at(rot[i][o][0], px, py)
    return occ


# ---------- 基线骨架 ----------
def base_layout(rot, W, H, ident=None):
    """占格数降序 + 朝向 0..3 + 行主序 first-fit.
    与 bench_fillrefine.base_layout 同序(ident=None 时无次序键); ident 非空时加原生次序键(对照组)."""
    if ident is None:
        order = sorted(range(len(rot)), key=lambda i: -len(rot[i][0][0]))
    else:
        order = sorted(range(len(rot)), key=lambda i: (-len(rot[i][0][0]), ident[i], i))
    occ = set()
    pos = {}
    for i in order:
        best = None
        for o, (cells, nw, nh) in enumerate(rot[i]):
            if nw > W or nh > H:
                continue
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if can_place(occ, cells, px, py, W, H):
                        best = (px, py, o, cells)
                        break
                if best:
                    break
            if best:
                break
        if best is None:
            return None
        px, py, o, cells = best
        occ = mark(occ, cells, px, py)
        pos[i] = (px, py, o)
    return pos, occ


# ---------- 新语义: 原生首选落位 ----------
def native_pass(rot, ident, W, H, init_pos, rot_fallback=True, order_key="native"):
    """Core.cs LayoutNativeFirstFit 的离线镜像(空网格 + 单层占用).

    原生: maxX = W-bboxW, maxY = H-bboxH; 外 y 内 x; 第一个可行候选即胜出;
          任一物品找不到位 → 原生整次 return false(我方宽容: 记 leftover 继续排更小的件).
    order_key: native = 全三键(原生 §7); nokey3 = 去第三键; nokey = 只用占格数降序.
    """
    n = len(rot)
    if order_key == "native":
        order = sorted(range(n), key=lambda i: (-len(rot[i][0][0]), ident[i],
                                                init_pos[i][1] * W + init_pos[i][0], i))
    elif order_key == "nokey3":
        order = sorted(range(n), key=lambda i: (-len(rot[i][0][0]), ident[i], i))
    else:
        order = sorted(range(n), key=lambda i: -len(rot[i][0][0]))
    occ = set()
    pos = {}
    leftover = []
    for i in order:
        o0 = init_pos[i][2]
        tries = [(o0 + k) & 3 for k in range(4)] if rot_fallback else [o0]  # 与 C# LayoutNativeFirstFit 同序
        found = None
        for o in tries:
            cells, nw, nh = rot[i][o]
            maxX = W - nw
            maxY = H - nh
            if maxX < 0 or maxY < 0:
                continue
            for y in range(maxY + 1):
                for x in range(maxX + 1):
                    if can_place(occ, cells, x, y, W, H):
                        found = (x, y, o, cells)
                        break
                if found:
                    break
            if found:
                break
        if found:
            px, py, o, cells = found
            occ = mark(occ, cells, px, py)
            pos[i] = (px, py, o)
        else:
            leftover.append(i)
    return pos, occ, leftover


# ---------- 我方宽容路径(镜像 Core.cs TryResidualLayout) ----------
def residual_pass(rot, W, H, init_pos):
    """预订全部物品当前格 → 大件先 → 释放自身格 → 最贴合空位(waste,minY,minX) → 放不下留原位."""
    occ = set()
    for i, (px, py, o) in init_pos.items():
        occ |= cells_at(rot[i][o][0], px, py)
    order = sorted(init_pos.keys(), key=lambda i: -len(rot[i][0][0]))
    pos = {}
    for i in order:
        ox0, oy0, oo0 = init_pos[i]
        own = cells_at(rot[i][oo0][0], ox0, oy0)
        occ -= own
        best = None
        for o, (cells, nw, nh) in enumerate(rot[i]):
            if nw > W or nh > H:
                continue
            waste = nw * nh - len(cells)
            for y in range(H - nh + 1):
                for x in range(W - nw + 1):
                    if not can_place(occ, cells, x, y, W, H):
                        continue
                    key = (waste, y, x)
                    if best is None or key < best[0]:
                        best = (key, o, x, y, cells)
        if best is not None:
            _k, o, x, y, cells = best
            occ = mark(occ, cells, x, y)
            pos[i] = (x, y, o)
        else:
            occ |= own
    return pos, occ


# ---------- FillRefine: 与 bench_fillrefine.refine 逐行同口径 ----------
def refine(rot, W, H, pos, guard=True):
    """返回 (result_pos, moved_down). moved_down = 采纳的"贴邻下降"移动数(= 拆散次数, 同名同义)."""
    if W * H > REFINE_MAX_CELLS or not pos:
        return dict(pos), 0
    occ = set()
    for i, (px, py, o) in pos.items():
        occ |= cells_at(rot[i][o][0], px, py)
    oa, _ = max_empty(occ, W, H)
    result = dict(pos)
    moved_down = 0
    for i in sorted(pos.keys(), key=lambda k: len(rot[k][0][0])):
        px, py, o = pos[i]
        cc = rot[i][o][0]
        cur_cells = cells_at(cc, px, py)
        occ -= cur_cells
        ct = touch(occ, W, H, cc, px, py)
        cand = None
        for (rx, ry, rw, rh) in sorted(find_free_rects(occ, W, H), key=lambda r: r[2] * r[3]):
            bT = -1
            bP = None
            for o2, (cells, nw, nh) in enumerate(rot[i]):
                if nw > rw or nh > rh:
                    continue
                for py2 in range(ry, ry + rh - nh + 1):
                    for px2 in range(rx, rx + rw - nw + 1):
                        if not can_place(occ, cells, px2, py2, W, H):
                            continue
                        t = touch(occ, W, H, cells, px2, py2)
                        if bP is None or t > bT or (t == bT and (py2 < bP[1] or (py2 == bP[1] and px2 < bP[2]))):
                            bT = t
                            bP = (px2, py2, o2)
            if bP is not None:
                cand = (bP[0], bP[1], bP[2], bT)
                break
        if cand is None or (cand[0], cand[1], cand[2]) == (px, py, o):
            occ |= cur_cells
            continue
        cxc = rot[i][cand[2]][0]
        new_cells = cells_at(cxc, cand[0], cand[1])
        occ |= new_cells
        na, _ = max_empty(occ, W, H)
        ok = na >= oa
        if guard and cand[3] < ct - 1e-9:
            ok = False
        if ok:
            result[i] = (cand[0], cand[1], cand[2])
            if cand[3] < ct - 1e-9:
                moved_down += 1
        else:
            occ -= new_cells
            occ |= cur_cells
    return result, moved_down


def refine_n(rot, W, H, pos, guard=True, rounds=3):
    """重复 FillRefine 到"本轮零采纳"为止(<=rounds 轮). 每轮采纳都要求空矩不降 → 空矩单调不减;
    纯本地搜索, 不做候选布局择优 —— 用于在"同一精修层"下公平比较基线/新路径."""
    cur = dict(pos)
    total = 0
    for _ in range(rounds):
        cur, dd = refine(rot, W, H, cur, guard=guard)
        total += dd
        if dd == 0:
            break
    return cur, total


# ---------- 指标 ----------
def isolated_small(rot, occ, W, H, pos):
    n = 0
    for i, (px, py, o) in pos.items():
        cells = rot[i][o][0]
        if len(cells) > SMALL_MAX:
            continue
        wall = False
        nb = False
        for dx, dy in cells:
            cx, cy = px + dx, py + dy
            if cx == 0 or cy == 0 or cx == W - 1 or cy == H - 1:
                wall = True
            for ax, ay in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                if (ax, ay) in occ:
                    nb = True
        if not nb and not wall:
            n += 1
    return n


def moved_ratio(init_pos, var_pos):
    if not init_pos:
        return 0.0
    mv = sum(1 for i, p in init_pos.items() if var_pos.get(i) != p)
    return mv / len(init_pos)


def check_invariants(rot, W, H, pos, init_pos):
    """安全不变量(task-4 同口径): 越界 / 落地重叠 / 压住未移动件(不在新布局里的旧件).
    返回 (越界格数, 重叠件数, 压未动件数) —— 三者相加为 0 才算安全."""
    seen = set()
    oob = ov = 0
    for i, (px, py, o) in pos.items():
        cs = cells_at(rot[i][o][0], px, py)
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
        if cells_at(rot[i][io][0], ipx, ipy) & seen:
            on += 1
    return oob, ov, on


# ---------- 单会话 ----------
VARIANTS = ["init", "baseline_off", "baseline", "native", "native+ref", "soft+ref",
            "A_native_nokey", "A2_nokey3", "B_base_natkey",
            "native+refN", "baseline+refN", "new",
            # task-5: 原生候选(C# 第 5 候选)与旧候选同池、同精修层择优
            "pick_best", "pick_best_tie"]
PROBE = ("baseline_off", "baseline", "native", "native+ref", "new")  # 跑 guard-OFF 探针的变体(较贵)


def run_session(items, W, H):
    rot = [rots_of(it) for it in items]
    ident = [it["name"] for it in items]
    bl = base_layout(rot, W, H)
    if bl is None:
        return None
    init_pos, _init_occ = bl

    res = {"init": (dict(init_pos), 0)}
    off_pos, off_dd = refine(rot, W, H, init_pos, guard=False)
    res["baseline_off"] = (off_pos, off_dd)
    on_pos, on_dd = refine(rot, W, H, init_pos, guard=True)
    res["baseline"] = (on_pos, on_dd)

    npos, _nocc, leftover = native_pass(rot, ident, W, H, init_pos)
    res["native"] = (npos, 0)
    nref_pos, nref_dd = refine(rot, W, H, npos, guard=True)
    res["native+ref"] = (nref_pos, nref_dd)

    spos, _socc = residual_pass(rot, W, H, init_pos)
    sref_pos, sref_dd = refine(rot, W, H, spos, guard=True)
    res["soft+ref"] = (sref_pos, sref_dd)

    # 对照组: 逐个隔离次序键的贡献(扫描与精修完全相同)
    apos, _ao, _al = native_pass(rot, ident, W, H, init_pos, order_key="nokey")
    res["A_native_nokey"] = refine(rot, W, H, apos, guard=True)
    a2pos, _a2o, _a2l = native_pass(rot, ident, W, H, init_pos, order_key="nokey3")
    res["A2_nokey3"] = refine(rot, W, H, a2pos, guard=True)
    bl2 = base_layout(rot, W, H, ident=ident)
    res["B_base_natkey"] = refine(rot, W, H, bl2[0], guard=True) if bl2 else ({}, 0)

    # 精修层迭代到不动点(同层对比: 基线/新路径两边都迭代)
    res["native+refN"] = refine_n(rot, W, H, npos, guard=True)
    res["baseline+refN"] = refine_n(rot, W, H, init_pos, guard=True)

    res["new"] = res["native+ref"] if not leftover else res["soft+ref"]

    # task-5: 原生候选并入候选池后择优(同精修层比较) —— C# 新语义的等价模型:
    #   C# = {原生候选} ∪ {旧 4 布局器/配对候选} 各自 TryFillRefine 后, 取最大空矩最大者(area > bestArea ⇒ 先到者胜).
    #   本 bench 的旧候选池以已校准代理 baseline(= init 骨架 + guard-ON 精修; 已逐字复现 Lead 实测 20056) 表示,
    #   故逐会话取 max(baseline, native+ref) 的空矩. 原生已精修位为 native+ref; 原生有 leftover 时候选作废(同 C#).
    #   两口径: pick_best = 原生须"严格大于"才顶替(等价 C# 里原生候选追加在末尾) / pick_best_tie = 平手也顶替(追加在最前).
    #   说明差异: C# 在 LayoutDense 内部逐候选比空矩, 本 bench 用整会话 max 近似; 旧候选池被压成单一代理 ——
    #   两者对"取 max"的结论同向, 但候选池内部各自的胜负顺序无法在离线精确复刻(已用 20056 校准锚定其合计结果).
    a_old = max_empty(occ_of(rot, res["baseline"][0]), W, H)[0]
    a_nat = max_empty(occ_of(rot, res["native+ref"][0]), W, H)[0]
    nat_ok = (not leftover)
    win_strict = int(nat_ok and a_nat > a_old)
    win_tie = int(nat_ok and a_nat >= a_old)
    res["pick_best"] = res["native+ref"] if win_strict else res["baseline"]
    res["pick_best_tie"] = res["native+ref"] if win_tie else res["baseline"]
    pick = {"old_area": a_old, "nat_area": a_nat, "nat_ok": nat_ok,
            "win_strict": win_strict, "win_tie": win_tie,
            "gain": (a_nat - a_old) if win_strict else 0}
    return res, {"leftover": len(leftover), "rot": rot, "init_pos": init_pos, "pick": pick}


def evaluate(sessions):
    keys = ("sess", "full", "empty", "iso", "touch", "touch_small", "frag", "inv")
    stat = {v: dict.fromkeys(keys, 0) for v in VARIANTS}
    for v in VARIANTS:
        stat[v]["mv"] = 0.0
        stat[v]["adopt_sess"] = 0
        stat[v]["adopt_cnt"] = 0
        stat[v]["dd_sess"] = 0
        stat[v]["dd_cnt"] = 0
        stat[v]["left"] = 0
        stat[v]["win_sess"] = 0   # task-5: 原生候选胜出会话数
        stat[v]["gain"] = 0       # task-5: 原生相对旧候选净赚空矩
    deltas = []
    n_feas = 0
    for (W, H, items) in sessions:
        r = run_session(items, W, H)
        if r is None:
            continue
        res, meta = r
        n_feas += 1
        rot = meta["rot"]
        init_pos = meta["init_pos"]
        empt = {}
        for v in VARIANTS:
            pos, adopt_dd = res[v]
            occ = occ_of(rot, pos)
            s = stat[v]
            s["sess"] += 1
            if len(pos) == len(items):
                s["full"] += 1
            s["left"] += len(items) - len(pos)
            e = max_empty(occ, W, H)[0]
            empt[v] = e
            s["empty"] += e
            s["iso"] += isolated_small(rot, occ, W, H, pos)
            for i, (px, py, o) in pos.items():
                t = touch(occ, W, H, rot[i][o][0], px, py)
                s["touch"] += t
                if len(rot[i][o][0]) <= SMALL_MAX:
                    s["touch_small"] += t
            s["frag"] += len(find_free_rects(occ, W, H))
            s["mv"] += moved_ratio(init_pos, pos)
            _oob, _ov, _on = check_invariants(rot, W, H, pos, init_pos)
            s["inv"] += _oob + _ov + _on
            s["adopt_cnt"] += adopt_dd
            s["adopt_sess"] += 1 if adopt_dd else 0
            if v in PROBE:
                _p, dd = refine(rot, W, H, pos, guard=False)
                s["dd_cnt"] += dd
                s["dd_sess"] += 1 if dd else 0
        pk = meta.get("pick")
        if pk:
            stat["pick_best"]["win_sess"] += pk["win_strict"]
            stat["pick_best"]["gain"] += pk["gain"]
            stat["pick_best_tie"]["win_sess"] += pk["win_tie"]
        deltas.append((empt["new"] - empt["baseline"], W, H, len(items), empt["baseline"], empt["new"]))
    print(f"  可行会话 = {n_feas}")
    print(f"  {'变体':<16}{'全放':>10}{'未放':>5}{'拆散采纳':>9}{'探针':>8}"
          f"{'最大空矩和':>11}{'贴邻和':>8}{'小件贴邻':>9}{'碎片数':>7}{'孤立小件':>9}{'安全失败':>9}{'平均移动':>9}")
    for v in VARIANTS:
        s = stat[v]
        mv = f"{100.0 * s['mv'] / max(1, s['sess']):.1f}%"
        ad = f"{s['adopt_sess']}/{s['adopt_cnt']}"
        pr = f"{s['dd_sess']}/{s['dd_cnt']}" if v in PROBE else "-"
        print(f"  {v:<16}{str(s['full']) + '/' + str(s['sess']):>10}{s['left']:>5}{ad:>9}{pr:>8}"
              f"{s['empty']:>11}{s['touch']:>8}{s['touch_small']:>9}{s['frag']:>7}{s['iso']:>9}"
              f"{s['inv']:>9}{mv:>9}")
    worse = [d for d in deltas if d[0] < 0]
    same = [d for d in deltas if d[0] == 0]
    better = [d for d in deltas if d[0] > 0]
    print(f"  逐会话(new vs baseline 最大空矩): 更好 {len(better)} 持平 {len(same)} 更差 {len(worse)}"
          f"; 亏损合计 {sum(d[0] for d in worse)} 收益合计 {sum(d[0] for d in better)}")
    for d in sorted(worse, key=lambda x: x[0])[:3]:
        print(f"    最差: {d[1]}x{d[2]} 件={d[3]} baseline={d[4]} new={d[5]} Δ={d[0]}")
    return n_feas, stat


# ---------- 数据源 ----------
def dump_sessions():
    out = []
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), BAK)
    for (W, H, grp) in parse_dump_merged(path):
        items = [{"cells": [tuple(c) for c in rep["cells"]], "w": rep["w"], "h": rep["h"],
                  "name": rep["name"]} for rep, _ in grp]
        if len(items) <= 1:
            continue
        out.append((W, H, items))
    return out


def synth_sessions(sizes=((24, 10), (17, 10), (11, 14), (10, 10), (9, 7), (8, 8)),
                   fills=(0.85, 0.95), per=8, seed=42):
    """catalog 合成组: 随机抽件 → 同类合并(还原 mergeRepIdx) → 总量 <= 容量."""
    pool = catalog_pool()
    rng = random.Random(seed)
    out = []
    for (w, h) in sizes:
        cap = w * h
        feas = [it for it in pool if any(nw <= w and nh <= h for _c, nw, nh in rotations_of(it))]
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
                        merged[k] = dict(it)
                items = list(merged.values())
                if sum(len(i["cells"]) for i in items) > cap:
                    continue
                out.append((w, h, items))
    return out


def main():
    print("=== 真实 dump: " + BAK + " ===")
    d = dump_sessions()
    print(f"  解析会话(>=2件) = {len(d)}")
    _nf, sd = evaluate(d)
    print()
    print("=== catalog 合成组 ===")
    s = synth_sessions()
    print(f"  合成会话 = {len(s)}")
    _nf2, ss = evaluate(s)
    print()
    print("=== 校准(Lead 独立实测 = bench_fillrefine.py guard OFF/ON) ===")
    print(f"  guard OFF 期望 296 / 61会话 103次 / 空矩和 20008 -> 实测 {sd['baseline_off']['sess']} / "
          f"{sd['baseline_off']['adopt_sess']}会话 {sd['baseline_off']['adopt_cnt']}次 / "
          f"{sd['baseline_off']['empty']}")
    print(f"  guard ON  期望 296 /  0会话   0次 / 空矩和 20056 -> 实测 {sd['baseline']['sess']} / "
          f"{sd['baseline']['adopt_sess']}会话 {sd['baseline']['adopt_cnt']}次 / "
          f"{sd['baseline']['empty']}")
    print()
    print("=== 判据(真实 dump) ===")
    bl, nv, na = sd["baseline"], sd["new"], sd["native"]
    print(f"  基线 baseline(guard ON): 全放 {bl['full']}/{bl['sess']} 拆散采纳 {bl['adopt_sess']}/{bl['adopt_cnt']} "
          f"空矩和 {bl['empty']} 贴邻和 {bl['touch']} 小件贴邻 {bl['touch_small']} 碎片数 {bl['frag']} 孤立小件 {bl['iso']}")
    print(f"  新语义 native 纯跑:      全放 {na['full']}/{na['sess']} 拆散采纳 {na['adopt_sess']}/{na['adopt_cnt']} "
          f"空矩和 {na['empty']} 贴邻和 {na['touch']} 小件贴邻 {na['touch_small']} 碎片数 {na['frag']} 孤立小件 {na['iso']}")
    print(f"  新组合 new(native+精修): 全放 {nv['full']}/{nv['sess']} 拆散采纳 {nv['adopt_sess']}/{nv['adopt_cnt']} "
          f"空矩和 {nv['empty']} 贴邻和 {nv['touch']} 小件贴邻 {nv['touch_small']} 碎片数 {nv['frag']} 孤立小件 {nv['iso']}")
    print(f"  精修层迭代到不动点(同层对比): native+refN 空矩和 {sd['native+refN']['empty']} 拆散 "
          f"{sd['native+refN']['adopt_cnt']} | baseline+refN 空矩和 {sd['baseline+refN']['empty']} 拆散 "
          f"{sd['baseline+refN']['adopt_cnt']} | Δ={sd['native+refN']['empty'] - sd['baseline+refN']['empty']}")
    print(f"  对照组 空矩和: A_native_nokey(去次序键)={sd['A_native_nokey']['empty']} | "
          f"A2_nokey3(去第三键)={sd['A2_nokey3']['empty']} | B_base_natkey(基线扫描+次序键)="
          f"{sd['B_base_natkey']['empty']} | baseline={bl['empty']} | new={nv['empty']}")
    print(f"  判定: 拆散采纳 new={nv['adopt_cnt']} (须 0, baseline={bl['adopt_cnt']}); "
          f"全放 {nv['full']} vs {bl['full']} ({'不劣化' if nv['full'] >= bl['full'] else '下降'}); "
          f"空矩和 {nv['empty']} vs {bl['empty']} Δ={nv['empty'] - bl['empty']}; "
          f"贴邻和 Δ={nv['touch'] - bl['touch']}; 小件贴邻 Δ={nv['touch_small'] - bl['touch_small']}; "
          f"碎片数 Δ={nv['frag'] - bl['frag']}; 孤立小件 Δ={nv['iso'] - bl['iso']}")
    print(f"  (合成组: baseline 空矩和 {ss['baseline']['empty']} 拆散 {ss['baseline']['adopt_cnt']} 全放 "
          f"{ss['baseline']['full']}/{ss['baseline']['sess']} | new 空矩和 {ss['new']['empty']} 拆散 "
          f"{ss['new']['adopt_cnt']} 全放 {ss['new']['full']}/{ss['new']['sess']} | A2_nokey3 空矩和 "
          f"{ss['A2_nokey3']['empty']} | A_native_nokey 空矩和 {ss['A_native_nokey']['empty']})")

    # ===== task-5: 原生候选并入旧候选池(同精修层择优) =====
    print()
    print("=== task-5: 原生候选(第 5)与旧候选同池、同精修层择优 ===")
    for label, st in (("真实 dump", sd), ("catalog", ss)):
        bl2, nv2 = st["baseline"], st["new"]
        print(f"  [{label}] baseline(旧候选+精修) 空矩和 {bl2['empty']} churn "
              f"{100.0 * bl2['mv'] / max(1, bl2['sess']):.1f}% 碎片 {bl2['frag']} 贴邻和 {bl2['touch']}"
              f" | new(task-3 原生早退) 空矩和 {nv2['empty']} churn "
              f"{100.0 * nv2['mv'] / max(1, nv2['sess']):.1f}% 碎片 {nv2['frag']} 贴邻和 {nv2['touch']}")
        for v, tag in (("pick_best", "严格>"), ("pick_best_tie", "平手也采纳")):
            s = st[v]
            print(f"    {v:<14}({tag}) 空矩和 {s['empty']} (Δ vs baseline {s['empty'] - bl2['empty']:+}) "
                  f"churn {100.0 * s['mv'] / max(1, s['sess']):.1f}% 碎片 {s['frag']} 贴邻和 {s['touch']} "
                  f"小件贴邻 {s['touch_small']} 孤立小件 {s['iso']} 拆散 {s['adopt_sess']}/{s['adopt_cnt']} "
                  f"安全失败 {s['inv']} 全放 {s['full']}/{s['sess']} 原生胜出 {s['win_sess']} 会话 "
                  f"贡献空矩 +{s['gain']}")
    print(f"  判据(真实 dump): 空矩和 >= baseline {sd['baseline']['empty']} ? "
          f"{'PASS' if sd['pick_best']['empty'] >= sd['baseline']['empty'] else 'FAIL'}"
          f" | 安全失败 {sd['pick_best']['inv']} (须 0) | 拆散 {sd['pick_best']['adopt_cnt']} (须 0)"
          f" | 全放 {sd['pick_best']['full']} vs baseline {sd['baseline']['full']}"
          f" | 原生胜出 {sd['pick_best']['win_sess']} 会话 贡献空矩 +{sd['pick_best']['gain']}")


if __name__ == "__main__":
    main()
