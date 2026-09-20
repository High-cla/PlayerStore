"""回归: ShrinkRects 去包含优化必须与原始 O(R^2) 版逐位一致。

被测代码 Core.cs:2661-2719 ShrinkRects —— 放置后把被占矩形从空闲池切掉:
先按 px/py 切出左右 + 中间上下段生成 next, 再做去包含。

原版去包含 (Core.cs:2697-2718): 对每个 i 扫全部 j, O(R^2)。
新版: 按面积降序排索引 + 单向剪枝(面积比自身小的不可能包含自身), 只作用于索引数组,
       kept 仍按 i 升序收集 —— **输出顺序必须与原始 rects 顺序一致**。
       (下游 TryPickPlaceSlot 在 waste/贴邻平局时依赖矩形顺序定归属。)

本脚本镜像两版并做双向断言:
  A. 修复版(索引剪枝)与原始 O(R^2) 版: 内容多重集一致 且 顺序一致;
  B. 判别力: 故意构造一个"面积相同但互相包含"的剪枝反例, 证明剪枝条件若写成
     严格 > 面积 就会漏判(证明本回归能抓到错误剪枝), 而正确版不误判;
  C. 批量随机对拍(大量重复/等面积/互相包含矩形 + 真实网格切块), 0 不一致。
"""


def shrink_cut(rects, px, py, pw, ph):
    """镜像 Core.cs:2663-2694 切块生成 next。"""
    nxt = []
    for (rx, ry, rw, rh) in rects:
        if rx + rw <= px or px + pw <= rx or ry + rh <= py or py + ph <= ry:
            nxt.append((rx, ry, rw, rh))  # 不相交
            continue
        if px > rx:
            nxt.append((rx, ry, px - rx, rh))
        if px + pw < rx + rw:
            nxt.append((px + pw, ry, rx + rw - (px + pw), rh))
        cx = max(rx, px)
        cx2 = min(rx + rw, px + pw)
        if cx < cx2:
            if py > ry:
                nxt.append((cx, ry, cx2 - cx, py - ry))
            if py + ph < ry + rh:
                nxt.append((cx, py + ph, cx2 - cx, ry + rh - (py + ph)))
    return nxt


def contains(o, r):
    return (o[0] <= r[0] and o[1] <= r[1]
            and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3])


def dedup_orig(nxt):
    """原始: 双循环全配对, 保持 next 顺序。"""
    kept = []
    n = len(nxt)
    for i in range(n):
        r = nxt[i]
        covered = False
        for j in range(n):
            if i == j:
                continue
            if contains(nxt[j], r):
                covered = True
                break
        if not covered:
            kept.append(r)
    return kept


def dedup_opt(nxt, strict=False):
    """新版: 面积降序索引 + 单向剪枝; kept 仍按 i 升序。

    strict=True 镜像"错误剪枝"(用严格 > 面积就 break) —— 面积相等的矩形互相包含
    时会被漏判, 用于证明本回归有判别力。
    """
    n = len(nxt)
    areas = [r[2] * r[3] for r in nxt]
    by_area = sorted(range(n), key=lambda k: -areas[k])
    kept = []
    for i in range(n):
        r = nxt[i]
        ra = areas[i]
        covered = False
        for k in range(n):
            j = by_area[k]
            if areas[j] < ra or (strict and areas[j] <= ra and j != i):
                break
            if j == i:
                continue
            if contains(nxt[j], r):
                covered = True
                break
        if not covered:
            kept.append(r)
    return kept


def shrink_orig(rects, px, py, pw, ph):
    return dedup_orig(shrink_cut(rects, px, py, pw, ph))


def shrink_opt(rects, px, py, pw, ph, strict=False):
    return dedup_opt(shrink_cut(rects, px, py, pw, ph), strict=strict)


def gen_rects(occ, W, H):
    """镜像 Core.cs FindFreeRects 前半: 单调栈按行生成矩形(含重复)。"""
    rects = []
    height = [0] * W
    for y in range(H):
        for x in range(W):
            height[x] = 0 if occ[x][y] else height[x] + 1
        stack = []
        for x in range(W + 1):
            cur = 0 if x == W else height[x]
            while stack and height[stack[-1]] >= cur:
                h = height[stack.pop()]
                left = 0 if not stack else stack[-1] + 1
                right = x - 1
                if h > 0:
                    rects.append((left, y - h + 1, right - left + 1, h))
            stack.append(x)
    return rects


fail = 0
checks = 0


def bad(msg):
    global fail
    print("  FAIL:", msg)
    fail += 1


# ---------------------------------------------------------------------------
# 1. 判别力: 等面积互相包含 —— 错误剪枝(严格 >)必漏判, 正确版不误判
# ---------------------------------------------------------------------------
print("[1] 判别力: 等面积(重合)矩形时剪枝边界")
# 两个 4x4 矩形, 位置不同, 互不包含 → 都应保留
eq = [(0, 0, 4, 4), (1, 1, 4, 4)]
o, opt = dedup_orig(eq), dedup_opt(eq)
print(f"  等面积互不包含: orig={len(o)} opt={len(opt)}")
checks += 1
if o != opt:
    bad(f"等面积互不包含应一致: {o} vs {opt}")
# 完全重合的等面积矩形: 原版两者互删(各被对方包含) → 0 个保留。
# 这是唯一"等面积且互相包含"的情形(面积相等 + 包含 ⇒ 同一盒子)。
dup = [(0, 0, 4, 4), (0, 0, 4, 4)]
o, opt = dedup_orig(dup), dedup_opt(dup)
print(f"  完全重合等面积: orig={len(o)} opt={len(opt)}")
checks += 1
if o != opt:
    bad(f"完全重合应一致: {o} vs {opt}")
# 错误剪枝版(面积相等也 break)会漏掉"等面积包含" → 保留重合矩形, 与原版分歧。
so = dedup_opt(dup, strict=True)
print(f"  错误剪枝(面积相等即 break) 完全重合: {len(so)} (正确应为 {len(o)})")
checks += 1
if len(so) != len(o):
    print("  错误剪枝版确实漏判 ⇒ 本回归对剪枝边界有判别力")
else:
    bad("错误剪枝版竟与正确版一致, 本回归对剪枝边界无判别力")

# ---------------------------------------------------------------------------
# 2. 真实网格: FindFreeRects → 反复 ShrinkRects, 全程对拍
# ---------------------------------------------------------------------------
import random

random.seed(20260922)
print("[2] 真实网格: 反复切块对拍")

for trial in range(400):
    W = random.randint(1, 12)
    H = random.randint(1, 12)
    dens = random.choice([0.0, 0.2, 0.4, 0.6, 0.8])
    occ = [[random.random() < dens for _ in range(H)] for _ in range(W)]
    rects_o = gen_rects(occ, W, H)
    rects_p = list(rects_o)
    for step in range(6):
        if not rects_o:
            break
        r = random.choice(rects_o)
        px, py = r[0], r[1]
        pw = random.randint(1, max(1, r[2]))
        ph = random.randint(1, max(1, r[3]))
        rects_o = shrink_orig(rects_o, px, py, pw, ph)
        rects_p = shrink_opt(rects_p, px, py, pw, ph)
        checks += 1
        if rects_o != rects_p:
            bad(f"trial={trial} {W}x{H} step={step} cut=({px},{py},{pw},{ph})")
            if fail <= 2:
                print("    orig:", rects_o)
                print("    opt :", rects_p)
            break

# ---------------------------------------------------------------------------
# 3. 任意矩形池随机对拍: 大量重复 / 等面积 / 互相包含
# ---------------------------------------------------------------------------
print("[3] 任意矩形池随机对拍(重复/等面积/互相包含)")
for trial in range(6000):
    n = random.randint(1, 14)
    rects = []
    for _ in range(n):
        x = random.randint(0, 6)
        y = random.randint(0, 6)
        w = random.randint(1, 6)
        h = random.randint(1, 6)
        rects.append((x, y, w, h))
    # 注入重复与等面积变体
    if rects and random.random() < 0.6:
        rects.append(random.choice(rects))
    if rects and random.random() < 0.4:
        base = random.choice(rects)
        rects.append((base[0] + random.choice([0, 1, 2]), base[1], base[2], base[3]))
    px = random.randint(0, 7)
    py = random.randint(0, 7)
    pw = random.randint(1, 6)
    ph = random.randint(1, 6)
    o = shrink_orig(rects, px, py, pw, ph)
    p = shrink_opt(rects, px, py, pw, ph)
    checks += 1
    if o != p:
        bad(f"trial={trial} cut=({px},{py},{pw},{ph})")
        if fail <= 2:
            print("    rects:", rects)
            print("    orig :", o)
            print("    opt  :", p)

# ---------------------------------------------------------------------------
# 4. 顺序敏感: 输出必须与"按 next 顺序"一致, 不得被排序改变
# ---------------------------------------------------------------------------
print("[4] 顺序敏感性: 排序不得改变输出顺序")
# 三个互不包含、面积递减的矩形, next 顺序 = 输入顺序
seq = [(0, 0, 5, 5), (10, 0, 2, 2), (20, 0, 3, 3)]
o, p = dedup_orig(seq), dedup_opt(seq)
print(f"  orig={o}")
print(f"  opt ={p}")
checks += 1
if o != p:
    bad("面积乱序输入下输出顺序不一致")
if p != seq:
    bad(f"互不包含矩形应原序全保留, 实得 {p}")
# 面积降序输入
seq2 = sorted(seq, key=lambda r: -(r[2] * r[3]))
o, p = dedup_orig(seq2), dedup_opt(seq2)
checks += 1
if o != p or p != seq2:
    bad(f"面积降序输入应原序保留, orig={o} opt={p}")

print(f"\n对拍 {checks} 组, 不一致 {fail}")
print("PASS" if fail == 0 else f"FAIL ({fail})")
raise SystemExit(1 if fail else 0)
