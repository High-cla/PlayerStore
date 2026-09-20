"""redteam_tightest.py -- 红队独立复核 TryTightestSlot 键打包修复 (Core.cs:1982-1995).

不镜像 lead 的说明, 直接从 Core.cs 语义重建:
  - ItemMask: C0 精确格; C1..C3 = C0 的 90/180/270 旋转 (BuildMask Core.cs:2866-2902)
  - waste = (long)gw*gh - cs.Count            (Core.cs:1959)
  - 旧键 = waste*1000000 + y*100000 + x        (0d69984 版 Core.cs:1968)
  - 新键 = ((waste*H) + y)*W + x               (修复版)
验证三件事:
  A) 溢出: 在 W<=128 / H<=8192 / waste<=128*8192 上, 新键恒 <= long.MaxValue。
  B) 严格字典序: 对 (waste,y,x) 全序, 新式对任意取值都严格; 旧式仅 y<=9 成立。
  C) **行为等价性(关键)**: 在真实 TryTightestSlot 调用域里, 旧式与新式选出的
     (px,py,po) 是否恒等 —— 若恒等, 则旧式在本调用点**从未出错**, 修复是防御性
     等价改写而非行为修复。
"""
import random
import sys

LONG_MAX = (1 << 63) - 1

fail = []


def check(cond, msg):
    if not cond:
        fail.append(msg)
        print("  FAIL " + msg)


# ---------- A) 溢出上界 ----------
def test_overflow():
    print("[A] 溢出边界")
    max_w, max_h = 128, 8192
    worst = 0
    worst_desc = None
    # waste 最大 = gw*gh (cs.Count 最小为 1, 保守取 gw*gh)
    for w in (1, 2, 17, 128):
        for h in (1, 2, 37, 4096, 8192):
            if w > max_w or h > max_h:
                continue
            waste = w * h  # 上界
            y = h - 1
            x = w - 1
            key = ((waste * h) + y) * w + x
            if key > worst:
                worst, worst_desc = key, (w, h, waste, y, x)
    print("  最大键 = %d  (W,H,waste,y,x)=%s  long.Max=%d" % (worst, worst_desc, LONG_MAX))
    check(worst <= LONG_MAX, "新键溢出 long: %d" % worst)
    # 任务书给的 waste<=128*128=16384 是更松的上界, 一并验
    key2 = ((16384 * 8192) + 8191) * 128 + 127
    print("  任务书上界 waste=16384,H=8192,W=128 -> %d" % key2)
    check(key2 <= LONG_MAX, "任务书上界溢出")
    # 负 waste 也不得溢出
    key3 = (((-128 * 8192) * 8192) + 0) * 128 + 0
    check(abs(key3) <= LONG_MAX, "负 waste 溢出: %d" % key3)
    print("  负 waste 边界 %d OK" % key3)


# ---------- B) 字典序性质(纯公式) ----------
def test_lexicographic():
    print("[B] 字典序性质 (waste 最小 -> y 最小 -> x 最小)")
    W, H = 128, 8192
    old_ok = True
    new_ok = True
    old_counterexample = None
    rnd = random.Random(20260920)
    for _ in range(400000):
        w1, y1, x1 = rnd.randrange(0, 50), rnd.randrange(0, H), rnd.randrange(0, W)
        w2, y2, x2 = rnd.randrange(0, 50), rnd.randrange(0, H), rnd.randrange(0, W)
        if (w1, y1, x1) == (w2, y2, x2):
            continue
        old1 = w1 * 1000000 + y1 * 100000 + x1
        old2 = w2 * 1000000 + y2 * 100000 + x2
        new1 = ((w1 * H) + y1) * W + x1
        new2 = ((w2 * H) + y2) * W + x2
        want = -1 if (w1, y1, x1) < (w2, y2, x2) else 1
        if (old1 < old2) != (want < 0):
            old_ok = False
            if old_counterexample is None:
                old_counterexample = ((w1, y1, x1, old1), (w2, y2, x2, old2))
        if (new1 < new2) != (want < 0):
            new_ok = False
            print("  FAIL 新式违反字典序: %s vs %s" % ((w1, y1, x1, new1), (w2, y2, x2, new2)))
            break
    print("  旧式随机违反字典序: %s" % (not old_ok))
    if old_counterexample:
        print("  旧式反例: %s 与 %s" % (old_counterexample[0], old_counterexample[1]))
    check(new_ok, "新式在 (waste,y,x) 上未保持严格字典序")
    # 旧式的经典反例(lead 所引)
    a = 0 * 1000000 + 10 * 100000 + 0
    b = 1 * 1000000 + 0 * 100000 + 0
    print("  旧式经典反例: waste=0,y=10,x=0 -> %d ; waste=1,y=0,x=0 -> %d (相等=%s)" % (a, b, a == b))
    check(a == b, "lead 引用的键值相等反例算术有误")


# ---------- C) 真实调用域行为等价 ----------
def cells_rot90(c0, bw, bh):
    return [(bh - 1 - dy, dx) for (dx, dy) in c0]


def make_mask(bw, bh, cells):
    """返回 4 朝向 [(cs, gw, gh)], 与 BuildMask 一致。"""
    c0 = list(cells)
    c1 = [(bh - 1 - dy, dx) for (dx, dy) in c0]
    c2 = [(bw - 1 - dx, bh - 1 - dy) for (dx, dy) in c0]
    c3 = [(dy, bw - 1 - dx) for (dx, dy) in c0]
    return [(c0, bw, bh), (c1, bh, bw), (c2, bw, bh), (c3, bh, bw)]


def cells_free(occ, W, H, x, y, cs):
    for (dx, dy) in cs:
        if not (0 <= x + dx < W and 0 <= y + dy < H):
            return False
        if occ[y + dy][x + dx]:
            return False
    return True


def tightest(occ, W, H, oris, mode):
    """mode='old'|'new'. 返回 (px,py,po,best,waste_seen_set)"""
    best = None
    px = py = -1
    po = 0
    wastes = set()
    for ori in range(4):
        cs, gw, gh = oris[ori]
        if not cs:
            continue
        if gw > W or gh > H:
            continue
        waste = gw * gh - len(cs)
        wastes.add(waste)
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                if not cells_free(occ, W, H, x, y, cs):
                    continue
                if mode == "old":
                    key = waste * 1000000 + y * 100000 + x
                else:
                    key = ((waste * H) + y) * W + x
                if best is None or key < best:
                    best = key
                    px, py, po = x, y, ori
    return px, py, po, best, wastes


def test_behavior_equivalence():
    print("[C] 真实调用域: 旧式 vs 新式 行为等价性")
    rnd = random.Random(424242)
    diff = 0
    cases = 0
    waste_varies = 0
    # H=4096 全扫昂贵, 限制其出现频率; 主体用小网格保证组合覆盖
    specs = [(3, 3), (5, 7), (11, 14), (17, 10), (40, 7), (128, 64)] * 150 + [(11, 4096), (128, 4096)] * 6
    for (W, H) in specs:
        # 造一个连通/非连通形状
        bw = rnd.randrange(1, min(W, 6) + 1)
        bh = rnd.randrange(1, min(H, 6) + 1)
        cells = [(dx, dy) for dx in range(bw) for dy in range(bh) if rnd.random() < 0.6]
        if not cells:
            continue
        # 去掉空行/空列使 bbox 紧致
        oris = make_mask(bw, bh, cells)
        occ = [[rnd.random() < rnd.choice([0.0, 0.2, 0.5, 0.8, 1.0])
                for _ in range(W)] for _ in range(H)]
        if rnd.random() < 0.15:  # 全空
            occ = [[False] * W for _ in range(H)]
        r1 = tightest(occ, W, H, oris, "old")
        r2 = tightest(occ, W, H, oris, "new")
        cases += 1
        if len(r1[4]) > 1:
            waste_varies += 1
        # 只比较选出的位置 (px,py,po); 第 4 项是键值本身, 两式必然不同, 不参与等价性
        if r1[:3] != r2[:3]:
            diff += 1
            if diff <= 5:
                print("  差异: W=%d H=%d oris=%s" % (W, H, [(len(c), g1, g2) for c, g1, g2 in oris]))
                print("    old=%s new=%s" % (r1[:4], r2[:4]))
    print("  对拍 %d 例, 差异 %d, 其中 waste 跨朝向变化的例 %d" % (cases, diff, waste_varies))
    check(waste_varies == 0, "存在 waste 跨朝向变化的例(则等价性结论需重新论证)")
    check(diff == 0, "旧式与新式在真实调用域不逐位等价 (%d 例)" % diff)


# ---------- D) waste 常量性证明 ----------
def test_waste_constant():
    print("[D] waste 跨朝向常量性")
    rnd = random.Random(7)
    bad = 0
    for _ in range(20000):
        bw = rnd.randrange(1, 9)
        bh = rnd.randrange(1, 9)
        cells = [(dx, dy) for dx in range(bw) for dy in range(bh) if rnd.random() < 0.7]
        if not cells:
            continue
        oris = make_mask(bw, bh, cells)
        counts = set(len(c) for c, _, _ in oris)
        prods = set(g1 * g2 for _, g1, g2 in oris)
        if len(counts) != 1 or len(prods) != 1:
            bad += 1
    print("  20000 随机形状: 旋转后 cell 数不守恒/bbox 面积不守恒的例 = %d" % bad)
    check(bad == 0, "旋转改变了 cs.Count 或 gw*gh => waste 非常量")


# ---------- E) 全仓同款键模式扫描(潜在同类缺陷) ----------
def test_same_pattern_scan():
    print("[E] 扫描 Core.cs 内同款 '1000000'/'100000' 键打包")
    import re
    src = open("D:/git/PlayerStore/InventorySorter/InventorySorter/Core.cs",
               encoding="utf-8").read().splitlines()
    hits = [(i + 1, l.strip()) for i, l in enumerate(src)
            if re.search(r"1000000|100000\b|100000L", l)]
    for ln, l in hits:
        print("  %d: %s" % (ln, l))
    print("  命中 %d 行" % len(hits))


def tightest_earlyexit(occ, W, H, oris, mode):
    """镜像 lead 新增的「朝向内首个可行即停」早退 (Core.cs:1982-2006)。"""
    best = None
    px = py = -1
    po = 0
    for ori in range(4):
        cs, gw, gh = oris[ori]
        if not cs:
            continue
        if gw > W or gh > H:
            continue
        waste = gw * gh - len(cs)
        found = False
        for y in range(0, H - gh + 1):
            if found:
                break
            for x in range(0, W - gw + 1):
                if not cells_free(occ, W, H, x, y, cs):
                    continue
                if mode == "old":
                    key = waste * 1000000 + y * 100000 + x
                else:
                    key = ((waste * H) + y) * W + x
                if best is None or key < best:
                    best = key
                    px, py, po = x, y, ori
                found = True
                break
    return px, py, po, best


def test_early_exit_equivalence():
    """验证 lead 第 4 处未申报改动: 朝向内早退 vs 全扫, 位置是否逐位等价。"""
    print("[F] 早退(Core.cs:1982-2006) vs 全扫: 位置等价性")
    rnd = random.Random(31337)
    diff = 0
    cases = 0
    cross_ori = 0
    # 大网格(全扫很贵)只取少量样本; 主体用小网格覆盖「最优朝向非首个」的跨朝向情形
    specs = [(3, 3), (5, 7), (11, 14), (17, 10), (40, 7), (11, 64)] * 120 + [(128, 4096)] * 8
    for (W, H) in specs:
        bw = rnd.randrange(1, min(W, 6) + 1)
        bh = rnd.randrange(1, min(H, 6) + 1)
        cells = [(dx, dy) for dx in range(bw) for dy in range(bh) if rnd.random() < 0.6]
        if not cells:
            continue
        oris = make_mask(bw, bh, cells)
        occ = [[rnd.random() < rnd.choice([0.0, 0.2, 0.5, 0.8, 1.0])
                for _ in range(W)] for _ in range(H)]
        if rnd.random() < 0.15:
            occ = [[False] * W for _ in range(H)]
        full = tightest(occ, W, H, oris, "new")
        early = tightest_earlyexit(occ, W, H, oris, "new")
        cases += 1
        if full[:3] != early[:3]:
            diff += 1
            if diff <= 5:
                print("  差异: W=%d H=%d old_full=%s early=%s" % (W, H, full[:3], early[:3]))
        if full[2] != 0 and early[2] == full[2]:
            cross_ori += 1
    print("  对拍 %d 例, 位置差异 %d (其中最优朝向 != 0 的例 %d)" % (cases, diff, cross_ori))
    check(diff == 0, "早退与全扫位置不等价 (%d 例)" % diff)
    check(cross_ori > 0, "语料未覆盖『最优朝向非首个』情形, 等价性未获有效检验")
    print("  结论: 朝向内早退等价(该朝向内 key 随 (y,x) 严格递增, 首个可行即最小);")
    print("        跨朝向未早退(每朝向都扫到首个可行) => 全局仍取最小")


def test_transitive_old_vs_new():
    """头条检验: 0d69984 原始实现(旧键+全扫) vs 修复后(新键+朝向内早退),
    位置是否逐位一致。这是对『三处改动合起来是否改变排序结果』的端到端回答。"""
    print("[G] 端到端: 旧实现(旧键+全扫) vs 新实现(新键+早退)")
    rnd = random.Random(2026)
    diff = 0
    cases = 0
    specs = [(3, 3), (5, 7), (11, 14), (17, 10), (40, 7), (11, 64), (128, 64)] * 60
    for (W, H) in specs:
        bw = rnd.randrange(1, min(W, 6) + 1)
        bh = rnd.randrange(1, min(H, 6) + 1)
        cells = [(dx, dy) for dx in range(bw) for dy in range(bh) if rnd.random() < 0.6]
        if not cells:
            continue
        oris = make_mask(bw, bh, cells)
        occ = [[rnd.random() < rnd.choice([0.0, 0.2, 0.5, 0.8, 1.0])
                for _ in range(W)] for _ in range(H)]
        if rnd.random() < 0.15:
            occ = [[False] * W for _ in range(H)]
        a = tightest(occ, W, H, oris, "old")            # 0d69984 原样
        b = tightest_earlyexit(occ, W, H, oris, "new")  # 修复后
        cases += 1
        if a[:3] != b[:3]:
            diff += 1
            if diff <= 5:
                print("  差异: W=%d H=%d old=%s new=%s" % (W, H, a[:3], b[:3]))
    print("  对拍 %d 例, 位置差异 %d" % (cases, diff))
    check(diff == 0, "端到端位置不等价 (%d 例)" % diff)


if __name__ == "__main__":
    test_overflow()
    test_lexicographic()
    test_waste_constant()
    test_behavior_equivalence()
    test_early_exit_equivalence()
    test_transitive_old_vs_new()
    test_same_pattern_scan()
    print()
    if fail:
        print("RESULT: FAIL (%d)" % len(fail))
        for f in fail:
            print("  - " + f)
        sys.exit(1)
    print("RESULT: PASS")
