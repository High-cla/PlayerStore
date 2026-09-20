"""性能分析: TryTightestSlot 扫描域能否裁剪/提前退出 (Core.cs:1957-2002)。

被测: `for ori<4 { for y<H { for x<W { if CellsFree... } } }`。
H 来自 GetGridDims: 滚动容器固定 4096 (Core.cs:3474), SortInventory 钳到 8192
(Core.cs:873); W 钳到 128 (Core.cs:872)。⇒ 每件最多 4*128*4096 ≈ 2.1M 次
CellsFree, 每件一次调用。

本脚本独立复核 lead 的假设并给出可证明的等价改写。**只做分析与证明**。

------------------------------------------------------------------
命题 1 (waste 是全局常数): waste = (long)gw*gh - cs.Count 与 (ori, x, y) 无关。
证明: BuildMask (Core.cs:2871-2900) 令 C1..C3 为 C0 的 90/180/270 旋转:
        C1 = (bh-1-dy, dx)  尺寸 (bh, bw)
        C2 = (bw-1-dx, bh-1-dy) 尺寸 (bw, bh)
        C3 = (dy, bw-1-dx)  尺寸 (bh, bw)
      旋转是双射 ⇒ |C0|=|C1|=|C2|=|C3|;
      gw*gh 在 ori∈{0,2} 为 bw*bh, ori∈{1,3} 为 bh*bw ⇒ 四者同值。
      ⇒ waste 是同一常数 w0, 与 ori、x、y 全无关。  [脚本 §1 随机验证]

命题 2 (key 比较退化为 (y,x) 字典序):
      key = ((w0*H) + y)*W + x, 0<=y<H, 0<=x<W。
      对固定 w0, key 是 (y,x) 的严格单射且保序 (W 进制, y<H, x<W)
      ⇒ key 比较 ≡ (y,x) 字典序比较; ori 根本不进入 key。
      (这也正是 probe_tightest.py 修复的旧式 `waste*1000000+y*100000+x`
       在 y>=10 进位重叠的缺陷 —— 现行 Core.cs 已是正确式。)  [脚本 §2]

命题 3 (每朝向「首个可行 y 行」即该朝向最优 —— 早退等价):
      记 S_o = {(x,y) : CellsFree(o,x,y)} 为朝向 o 的可行集,
          y_o* = min{y : ∃x, (x,y)∈S_o},  x_o* = min{x : (x_o... )}.
      对任意 (x,y)∈S_o:
        y > y_o* ⇒ (y,x) >lex (y_o*, x_o*)  (首分量已大)
        y = y_o* ⇒ x >= x_o*                 (x_o* 是该行最小可行 x)
      ⇒ (y_o*, x_o*) 就是朝向 o 的字典序最小可行位。
      ⇒ 对每个朝向, 找到首个可行的 y 行并在该行取最小 x 后**即可停止该朝向**
        的扫描, 与全扫结果逐位相同。**无需任何 break-even 假设。**
      ⇒ 全局 = 4 个朝向的 (y_o*, x_o*) 按 (y,x) 取最小, 平手取最小 ori
        (原码 `key < best` 严格小于 ⇒ 平手保留先扫到的 ori)。  [脚本 §3 对拍]

命题 4 (反例: 跨朝向早退不安全):
      「任一朝向出现首个可行位就整体返回」会漏掉 ori 更大但 y 更小的位。
      [脚本 §5] 给出 W=10,H=8,occ={(0,0),(1,1)},mask=3x6 满矩形:
        ori0 (gw3,gh6) 首个可行 (x=0,y=2); ori1 (gw6,gh3) 首个可行 (x=2,y=0)。
        全扫/每朝向早退 => (2,0,ori1); 跨朝向早退 => (0,2,ori0) 错。
      ⇒ 早退的粒度必须是「朝向内」, 不能是「朝向外」。

成本结论:
  - 有解: 每朝向只需扫到首个可行 y 行 ⇒ 期望 O(gh * W) 而非 O(H * W)。
  - 无解 (该朝向无任何可行位): 仍需全扫该朝向 ⇒ 保持 O(4*H*W) 不变。
    这是「保持逐位等价」的下界 —— 要证明不存在可行位, 必须查完全部候选。
"""

import random
import sys
import time

W_CLAMP = 128      # Core.cs:872 w = Math.Min(w, 128)
H_CLAMP = 8192     # Core.cs:873 h = Math.Min(h, 8192)
H_SCROLL = 4096    # Core.cs:3474 滚动容器 h = 4096

fail = 0
checks = 0


def bad(msg):
    global fail
    print("  FAIL:", msg)
    fail += 1


def ok(cond, msg):
    global checks
    checks += 1
    if not cond:
        bad(msg)
    return cond


# ---------------------------------------------------------------------------
# 镜像 Core.cs 的 mask 构造 (BuildMask Core.cs:2871-2900) 与 CellsOf
# ---------------------------------------------------------------------------
def make_orients(bw, bh, cells):
    """返回 [(cs, gw, gh)] x4, 与 BuildMask 逐字一致。"""
    c0 = list(cells)
    c1 = [(bh - 1 - dy, dx) for (dx, dy) in c0]       # rot90  尺寸 (bh, bw)
    c2 = [(bw - 1 - dx, bh - 1 - dy) for (dx, dy) in c0]  # rot180 尺寸 (bw, bh)
    c3 = [(dy, bw - 1 - dx) for (dx, dy) in c0]       # rot270 尺寸 (bh, bw)
    return [
        (c0, bw, bh),
        (c1, bh, bw),
        (c2, bw, bh),
        (c3, bh, bw),
    ]


def cells_free(occ, W, H, x, y, cs):
    """镜像 Core.cs:3278 CellsFree (调用点已保证 x+gw<=W, y+gh<=H)。"""
    for (dx, dy) in cs:
        if occ[x + dx][y + dy]:
            return False
    return True


def tightest_full(occ, W, H, orients):
    """镜像 Core.cs:1957-2002 全扫参考版。返回 (px,py,po) 或 None。"""
    best = None
    px = py = -1
    po = 0
    for ori in range(4):
        cs, gw, gh = orients[ori]
        if not cs:
            continue
        if gw > W or gh > H:
            continue
        waste = gw * gh - len(cs)
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                if not cells_free(occ, W, H, x, y, cs):
                    continue
                key = ((waste * H) + y) * W + x
                if best is None or key < best:
                    best = key
                    px, py, po = x, y, ori
    if best is None:
        return None
    return (px, py, po)


def tightest_perori_early(occ, W, H, orients):
    """新策略: 每朝向找到首个可行 y 行, 该行取最小可行 x 后停该朝向; 再跨朝向取最小。"""
    best_y = None
    best_x = None
    best_o = 0
    for ori in range(4):
        cs, gw, gh = orients[ori]
        if not cs:
            continue
        if gw > W or gh > H:
            continue
        hit = None
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                if cells_free(occ, W, H, x, y, cs):
                    hit = (x, y)
                    break
            if hit is not None:
                break
        if hit is None:
            continue
        x, y = hit
        if best_y is None or (y, x) < (best_y, best_x):
            best_y, best_x, best_o = y, x, ori
    if best_y is None:
        return None
    return (best_x, best_y, best_o)


def tightest_crossori_early(occ, W, H, orients):
    """不安全变体: 任一朝向出现首个可行位就整体返回 (用于 §5 反例)。"""
    for ori in range(4):
        cs, gw, gh = orients[ori]
        if not cs:
            continue
        if gw > W or gh > H:
            continue
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                if cells_free(occ, W, H, x, y, cs):
                    return (x, y, ori)
    return None


def rand_shape(rnd, maxw, maxh):
    """随机连通/非连通形状, 去掉空行空列使 bbox 紧致。"""
    bw = rnd.randrange(1, maxw + 1)
    bh = rnd.randrange(1, maxh + 1)
    dens = rnd.choice([0.25, 0.5, 0.75, 1.0])
    cells = [(dx, dy) for dx in range(bw) for dy in range(bh) if rnd.random() < dens]
    if not cells:
        return None
    xs = [c[0] for c in cells]
    ys = [c[1] for c in cells]
    x0, y0 = min(xs), min(ys)
    cells = [(dx - x0, dy - y0) for (dx, dy) in cells]
    return (max(xs) - x0 + 1, max(ys) - y0 + 1, cells)


def rand_occ(rnd, W, H, mode):
    """占用图按 occ[x][y] 存放 (列主序), 与 cells_free 的索引一致。"""
    if mode == "empty":
        return [[False] * H for _ in range(W)]
    if mode == "full":
        return [[True] * H for _ in range(W)]
    if mode == "sparse_high":
        # 稀疏高占用: 随机 40% 占用
        return [[rnd.random() < 0.4 for _ in range(H)] for _ in range(W)]
    if mode == "dense":
        return [[rnd.random() < rnd.choice([0.2, 0.5, 0.8, 1.0]) for _ in range(H)]
                for _ in range(W)]
    # 大空底: 顶部若干行占满, 其余全空
    top = rnd.randrange(0, min(H, 12) + 1)
    return [[y < top for y in range(H)] for _ in range(W)]


# ===========================================================================
print("=" * 74)
print("§1  命题 1: waste = gw*gh - |cs| 与 (ori,x,y) 无关 (四朝向同一常数)")
print("=" * 74)
rnd = random.Random(20260921)
n_shape = 0
for _ in range(20000):
    sh = rand_shape(rnd, 9, 9)
    if sh is None:
        continue
    bw, bh, cells = sh
    orients = make_orients(bw, bh, cells)
    counts = set(len(c) for c, _, _ in orients)
    prods = set(g1 * g2 for _, g1, g2 in orients)
    wastes = set(g1 * g2 - len(c) for c, g1, g2 in orients)
    n_shape += 1
    if len(counts) != 1 or len(prods) != 1 or len(wastes) != 1:
        bad("形状 %s 的 waste 跨朝向不守恒: counts=%s prods=%s wastes=%s"
            % ((bw, bh, cells), counts, prods, wastes))
        break
print("  %d 个随机形状: 四朝向 |cs| 全同、gw*gh 全同 ⇒ waste 同一常数" % n_shape)
ok(n_shape > 19000, "形状样本不足")
print("  ⇒ 命题 1 成立: waste 与 ori 无关, 且 (x,y) 本就不进入 waste")


# ===========================================================================
print()
print("=" * 74)
print("§2  命题 2: key = ((w0*H)+y)*W+x 退化为 (y,x) 字典序 (ori 不进入 key)")
print("=" * 74)
rnd = random.Random(4242)
viol = 0
for _ in range(200000):
    H = rnd.choice([3, 7, 14, 64, 4096, 8192])
    W = rnd.choice([3, 5, 11, 17, 40, 128])
    w0 = rnd.randrange(0, 200)
    y1, x1 = rnd.randrange(0, H), rnd.randrange(0, W)
    y2, x2 = rnd.randrange(0, H), rnd.randrange(0, W)
    if (y1, x1) == (y2, x2):
        continue
    k1 = ((w0 * H) + y1) * W + x1
    k2 = ((w0 * H) + y2) * W + x2
    want = (y1, x1) < (y2, x2)
    if (k1 < k2) != want:
        viol += 1
        if viol == 1:
            bad("key 未保 (y,x) 字典序: w0=%d W=%d H=%d (y,x)=(%d,%d)->%d vs (%d,%d)->%d"
                % (w0, W, H, y1, x1, k1, y2, x2, k2))
        break
print("  200000 组随机 (w0,W,H,(y,x)) 对: key 违反 (y,x) 字典序 = %d" % viol)
ok(viol == 0, "key 编码未严格保持 (y,x) 字典序")
# waste 无关性: 同一 (y,x) 下改变 w0 不改变相对次序
k_a = ((0 * H_SCROLL) + 5) * W_CLAMP + 3
k_b = ((999 * H_SCROLL) + 5) * W_CLAMP + 3
k_c = ((999 * H_SCROLL) + 6) * W_CLAMP + 0
print("  w0 不参与 (y,x) 相对序: (w0=0,y=5,x=3)=%d < (w0=999,y=6,x=0)=%d -> %s"
      % (k_a, k_c, k_a < k_c))
ok(k_a < k_c, "w0 改变影响了 (y,x) 相对序")
print("  ⇒ 命题 2 成立: 比较退化为「最小 y, 再最小 x」, ori 不参与")


# ===========================================================================
print()
print("=" * 74)
print("§3  命题 3 对拍: 每朝向早退 vs 原始全扫 (随机网格)")
print("=" * 74)
rnd = random.Random(31337)
diff = 0
cases = 0
none_cases = 0
for _ in range(6000):
    W = rnd.choice([3, 5, 8, 11, 17, 40, 128])
    H = rnd.choice([3, 7, 14, 40, 128, 512])
    sh = rand_shape(rnd, min(W, 7), min(H, 7))
    if sh is None:
        continue
    bw, bh, cells = sh
    orients = make_orients(bw, bh, cells)
    occ = rand_occ(rnd, W, H, "dense")
    r1 = tightest_full(occ, W, H, orients)
    r2 = tightest_perori_early(occ, W, H, orients)
    cases += 1
    if r1 is None:
        none_cases += 1
    if r1 != r2:
        diff += 1
        if diff <= 5:
            print("  差异: W=%d H=%d mask=(%d,%d,%d格) full=%s early=%s"
                  % (W, H, bw, bh, len(cells), r1, r2))
print("  对拍 %d 例 (其中无解 %d 例): 不一致 = %d" % (cases, none_cases, diff))
ok(diff == 0, "每朝向早退与全扫在随机网格上不一致 (%d 例)" % diff)


# ===========================================================================
print()
print("=" * 74)
print("§4  边界用例: 全空 / 全满 / 稀疏高占用 / H=4096 大空底")
print("=" * 74)
boundary = []
# (名称, W, H, mask尺寸, occ模式)
boundary.append(("全空 8x8", 8, 8, (3, 3), "empty"))
boundary.append(("全空 128x64", 128, 64, (5, 4), "empty"))
boundary.append(("全满 8x8", 8, 8, (3, 3), "full"))
boundary.append(("全满 128x64", 128, 64, (5, 4), "full"))
boundary.append(("稀疏高占用 40x40", 40, 40, (4, 6), "sparse_high"))
boundary.append(("稀疏高占用 128x128", 128, 128, (6, 5), "sparse_high"))
boundary.append(("大空底 128x512", 128, 512, (3, 4), "empty_bottom"))
boundary.append(("大空底 64x1024", 64, 1024, (2, 3), "empty_bottom"))
boundary.append(("大空底 H=4096 128x4096", 128, H_SCROLL, (2, 2), "empty_bottom"))
boundary.append(("大空底 H=4096 32x4096", 32, H_SCROLL, (3, 3), "empty_bottom"))
boundary.append(("H=4096 全空 128x4096", 128, H_SCROLL, (2, 2), "empty"))
boundary.append(("H=4096 全满 16x4096", 16, H_SCROLL, (2, 2), "full"))

bd_fail = 0
for bi, (name, W, H, (bw, bh), mode) in enumerate(boundary):
    rnd = random.Random(1000 + bi)
    cells = [(dx, dy) for dx in range(bw) for dy in range(bh)]
    orients = make_orients(bw, bh, cells)
    occ = rand_occ(rnd, W, H, mode)
    t0 = time.perf_counter()
    r1 = tightest_full(occ, W, H, orients)
    t1 = time.perf_counter()
    r2 = tightest_perori_early(occ, W, H, orients)
    t2 = time.perf_counter()
    same = (r1 == r2)
    if not same:
        bd_fail += 1
    print("  %-24s full=%-14s early=%-14s %s  (全扫 %.3fs / 早退 %.4fs)"
          % (name, r1, r2, "OK" if same else "MISMATCH", t1 - t0, t2 - t1))
    if not same:
        bad("边界用例 %s 不一致: full=%s early=%s" % (name, r1, r2))
ok(bd_fail == 0, "边界用例存在不一致 (%d)" % bd_fail)


# ===========================================================================
print()
print("=" * 74)
print("§5  命题 4 反例: 跨朝向早退不安全 (早退粒度必须是朝向内)")
print("=" * 74)
# 构造: mask = 3x6 满矩形 ⇒ ori0 尺寸 (3,6) 竖高, ori1 尺寸 (6,3) 横扁。
# 占用只放在 row 3 / row 4, 列取 {2,5,8,11} ⇒ 列区间 [cx-2,cx] 并集 = [0,11] 全覆盖。
#   ori0 (gh=6): y=0..4 的覆盖行区间 (0..5)(1..6)(2..7)(3..8)(4..9) 都含 row3/4
#                ⇒ 每一 x 都被挡; y=5 覆盖行 5..10 不含 row3/4 ⇒ x=0 可行。
#   ori1 (gh=3): y=0 覆盖行 0..2, 不含 row3/4 ⇒ x=0 即首个可行 (0,0)。
# 真值 (最小 y 再最小 x) = ori1 的 (0,0); 跨朝向早退会停在 ori0 的 (0,5)。
W, H = 12, 12
occ = [[False] * H for _ in range(W)]   # occ[x][y] 列主序
for cy in (3, 4):
    for cx in (2, 5, 8, 11):
        occ[cx][cy] = True
bw, bh = 3, 6
cells = [(dx, dy) for dx in range(bw) for dy in range(bh)]
orients = make_orients(bw, bh, cells)
full = tightest_full(occ, W, H, orients)
perori = tightest_perori_early(occ, W, H, orients)
cross = tightest_crossori_early(occ, W, H, orients)
print("  mask = %dx%d 满矩形 (|C0|=%d), W=%d H=%d" % (bw, bh, len(cells), W, H))
print("  ori0 (gw=%d,gh=%d): y=0..4 每 x 都被 row3/4 挡住, y=5 才首个可行 = (0,5)"
      % (orients[0][1], orients[0][2]))
print("  ori1 (gw=%d,gh=%d): y=0 覆盖行 0..2 不含 row3/4 ⇒ 首个可行 = (0,0)"
      % (orients[1][1], orients[1][2]))
print("  原始全扫          -> %s" % (full,))
print("  每朝向早退(新)    -> %s" % (perori,))
print("  跨朝向早退(不安全)-> %s" % (cross,))
ok(full == (0, 0, 1), "反例构造失效: 全扫期望 (0,0,ori1), 实得 %s" % (full,))
ok(perori == full, "每朝向早退未复现全扫")
ok(cross == (0, 5, 0), "跨朝向早退未复现错误选择 (0,5,ori0), 实得 %s" % (cross,))
ok(cross != full, "跨朝向早退竟然等价 —— 反例无判别力")
print("  ⇒ 跨朝向早退给出 (0,5,ori0), 真值 (0,0,ori1) —— y=0 更优的 ori1 被漏掉。")
print("    早退只能发生在**朝向内部**; 朝向之间必须比完全部 4 个候选。")


# ===========================================================================
print()
print("=" * 74)
print("§6  成本分析: 早退的实际收益与「无解」时的下界")
print("=" * 74)


def count_cellsfree_full(W, H, orients, occ):
    """原始全扫的真实 CellsFree 调用数 (不早退)。"""
    n = 0
    for ori in range(4):
        cs, gw, gh = orients[ori]
        if not cs or gw > W or gh > H:
            continue
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                n += 1
                cells_free(occ, W, H, x, y, cs)
    return n


def count_cellsfree_early(W, H, orients, occ):
    n = 0
    for ori in range(4):
        cs, gw, gh = orients[ori]
        if not cs or gw > W or gh > H:
            continue
        done = False
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                n += 1
                if cells_free(occ, W, H, x, y, cs):
                    done = True
                    break
            if done:
                break
    return n


for name, W, H, (bw, bh), mode in [
        ("大空底 128x4096", 128, H_SCROLL, (3, 4), "empty_bottom"),
        ("全空 128x4096", 128, H_SCROLL, (3, 4), "empty"),
        ("全满 128x4096 (无解)", 128, H_SCROLL, (3, 4), "full"),
        ("稀疏高占用 128x4096", 128, H_SCROLL, (3, 4), "sparse_high")]:
    rnd = random.Random(99)
    cells = [(dx, dy) for dx in range(bw) for dy in range(bh)]
    orients = make_orients(bw, bh, cells)
    occ = rand_occ(rnd, W, H, mode)
    a = count_cellsfree_full(W, H, orients, occ)
    b = count_cellsfree_early(W, H, orients, occ)
    print("  %-24s 全扫 %9d 次 CellsFree  ->  早退 %9d 次  (%.2f%%)"
          % (name, a, b, 100.0 * b / a if a else 0.0))
print()
print("  无解情形: 要证明某朝向不存在可行位, 必须枚举完全部候选")
print("  ⇒ 下界仍是 O(4*H*W) = 4*128*4096 ≈ 2.1M 次/件, 无法裁剪。")
print("  ⇒ 收益仅在「有解」时兑现, 且与首个可行 y 行位置成正比。")

print()
print("=" * 74)
if fail:
    print("RESULT: FAIL (%d)" % fail)
else:
    print("RESULT: PASS  (%d 项断言)" % checks)
print("=" * 74)
raise SystemExit(1 if fail else 0)
