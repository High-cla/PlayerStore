"""回归: FindFreeRects 去包含优化必须与原始 O(R^2) 版语义完全一致。

优化点: 原版逐 i 扫全部 j 判"是否被覆盖"; 新版按面积降序取候选索引 + 单向剪枝
(面积更小的不可能覆盖当前矩形)。剪枝只在"面积不小于自身"的范围内查, 逻辑等价。

关键约束: **输出顺序必须保持原始 rects 顺序** —— 下游 TryPickPlaceSlot 在
waste/贴邻平局时依赖矩形顺序定归属。因此排序只作用于索引数组。

断言:
  1. 内容(多重集)完全一致;
  2. 顺序完全一致;
  3. 含随机网格的批量对拍, 0 不一致。
"""
import random


def gen_rects(occ, W, H):
    """镜像 Core.cs FindFreeRects 前半: 单调栈按行生成矩形(含重复)。"""
    rects = []
    height = [0] * W
    stack = []
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


def dedup_orig(rects):
    """原始: 双循环全配对, 保持 rects 原始顺序。"""
    kept = []
    n = len(rects)
    for i in range(n):
        r = rects[i]
        covered = False
        for j in range(n):
            if i == j:
                continue
            o = rects[j]
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if not covered:
            kept.append(r)
    return kept


def dedup_opt(rects):
    """优化: 面积降序索引 + 单向剪枝, 输出仍按原始顺序。"""
    n = len(rects)
    areas = [r[2] * r[3] for r in rects]
    by_area = sorted(range(n), key=lambda k: -areas[k])
    kept = []
    for i in range(n):
        r = rects[i]
        ra = areas[i]
        covered = False
        for k in range(n):
            j = by_area[k]
            if areas[j] < ra:
                break
            if j == i:
                continue
            o = rects[j]
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if not covered:
            kept.append(r)
    return kept


fail = 0
checked = 0

# 1. 确定性用例: 全空 6x6 → 只有一个 6x6 矩形
rects = gen_rects([[False] * 6 for _ in range(6)], 6, 6)
o1, o2 = dedup_orig(rects), dedup_opt(rects)
if o1 != o2:
    print("FAIL 全空网格", o1, o2)
    fail += 1
checked += 1

# 2. 随机网格批量对拍(含各种尺寸/密度)
random.seed(20260920)
for trial in range(3000):
    W = random.randint(1, 8)
    H = random.randint(1, 8)
    dens = random.choice([0.0, 0.2, 0.4, 0.6, 0.8])
    occ = [[random.random() < dens for _ in range(H)] for _ in range(W)]
    rects = gen_rects(occ, W, H)
    if not rects:
        continue
    o1, o2 = dedup_orig(rects), dedup_opt(rects)
    checked += 1
    if o1 != o2:
        fail += 1
        if fail <= 3:
            print(f"FAIL trial={trial} {W}x{H} dens={dens}")
            print("  orig:", o1)
            print("  opt :", o2)

# 3. 真实背包尺寸
for (W, H) in [(24, 10), (17, 10), (11, 14), (14, 21), (8, 9), (7, 5), (5, 5)]:
    for _ in range(30):
        dens = random.choice([0.3, 0.5, 0.7])
        occ = [[random.random() < dens for _ in range(H)] for _ in range(W)]
        rects = gen_rects(occ, W, H)
        if not rects:
            continue
        o1, o2 = dedup_orig(rects), dedup_opt(rects)
        checked += 1
        if o1 != o2:
            fail += 1
            print(f"FAIL real {W}x{H}")

print(f"对拍 {checked} 组, 不一致 {fail}")
print("PASS" if fail == 0 else "FAIL")
raise SystemExit(1 if fail else 0)
