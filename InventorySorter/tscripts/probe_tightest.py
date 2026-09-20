"""回归: TryTightestSlot 的 key 打包必须满足 (waste, y, x) 严格字典序。

被测代码 Core.cs:1940-1980 TryTightestSlot —— 四朝向 x 全坐标扫描, 取
"脚印浪费最小, 平手 minY 再 minX" 的空位。

原始 bug (Core.cs:1968):
    key = waste * 1000000L + (long)y * 100000L + x
该式只在 y < 10 时等价于 (waste,y,x) 字典序; y >= 10 时 waste 层与 y 层进位重叠。
反例: (waste=0,y=10,x=0) -> 1000000 == (waste=1,y=0,x=0) -> 1000000
键值相等 ⇒ 选择退化为扫描顺序(o→y→x), 浪费更大的空位可能击败浪费更小的空位。
可达性: H 来自 GetGridDims(Core.cs:3424), 滚动容器分支 h = 4096 (Core.cs:3452),
        SortInventory 仅钳到 8192 (Core.cs:856); W <= 128 (Core.cs:855)。

修复 (新式):
    key = ((waste * H) + y) * W + x
当 0 <= y < H 且 0 <= x < W 时该式是 (waste,y,x) 的严格字典序线性单射。

本脚本镜像两版并做双向断言:
  A. 新式: 在所有真实 W/H/坐标组合下与元组字典序完全一致(恒绿);
  B. 旧式: 在 H >= 11 时确实复现"waste 更大却胜出"的错误(证明本回归有判别力,
     不会因为改坏而恒绿);
  C. 旧式在 H <= 10 (所有可达 y < 10) 时是精确的 —— 说明缺陷边界正是 y >= 10;
  D. 新式不溢出 long。

等价性判定: 一个线性编码保持字典序 ⟺ 按编码排序 == 按元组排序 (无平局错位)。
因新式在 (0<=y<H, 0<=x<W) 上严格单射, 直接用"两种排序结果相同"判定, O(n log n)。
"""

W_MAX = 128      # Core.cs:855 w = Math.Min(w, 128)
H_CLAMP = 8192   # Core.cs:856 h = Math.Min(h, 8192)
H_SCROLL = 4096  # Core.cs:3452 滚动容器 h = 4096
LONG_MAX = 2 ** 63 - 1


def key_old(waste, y, x):
    """镜像 Core.cs:1968 原始编码。"""
    return waste * 1000000 + y * 100000 + x


def key_new(waste, y, x, W, H):
    """镜像修复后的编码 ((waste*H)+y)*W+x。"""
    return ((waste * H) + y) * W + x


def pick(cands, keyfn):
    """镜像 TryTightestSlot 选择循环: 仅当 key < best 才更新, 即平手取扫描序首个。

    cands 按真实扫描序 (ori, y, x) 排列; 每项 = (waste, y, x, ori)。
    """
    best = None
    best_key = None
    for c in cands:
        k = keyfn(c)
        if best_key is None or k < best_key:
            best_key = k
            best = c
    return best


def true_min(cands):
    """独立参照: 按 (waste,y,x) 字典序取最小, 平手取扫描序首个。"""
    best = None
    for c in cands:
        if best is None or (c[0], c[1], c[2]) < (best[0], best[1], best[2]):
            best = c
    return best


def order_preserved(cands, keyfn):
    """编码保序 ⟺ 按 key 排序结果 == 按元组排序结果。返回 (ok, 首个反例)。"""
    by_key = sorted(cands, key=lambda c: (keyfn(c),))
    by_tup = sorted(cands)
    if by_key == by_tup:
        return True, None
    for a, b in zip(by_key, by_tup):
        if a != b:
            return False, (a, b)
    return False, (by_key, by_tup)


fail = 0
checks = 0


def bad(msg):
    global fail
    print("  FAIL:", msg)
    fail += 1


# ---------------------------------------------------------------------------
# 1. 具体反例: 键值碰撞 (任务点名的 waste=0,y=10,x=0 与 waste=1,y=0,x=0)
# ---------------------------------------------------------------------------
print("[1] 具体反例: 旧式键值碰撞")
ko0 = key_old(0, 10, 0)
ko1 = key_old(1, 0, 0)
kn0 = key_new(0, 10, 0, W_MAX, H_SCROLL)
kn1 = key_new(1, 0, 0, W_MAX, H_SCROLL)
print(f"  old(0,10,0)={ko0}  old(1,0,0)={ko1}   -> {'碰撞' if ko0 == ko1 else '不碰撞'}")
print(f"  new(0,10,0)={kn0}  new(1,0,0)={kn1}  -> {'新式正确分开' if kn0 < kn1 else '新式未分开'}")
checks += 1
if ko0 != 1000000 or ko1 != 1000000:
    bad(f"旧式反例键值应为 1000000, 实得 {ko0}/{ko1}")
if ko0 != ko1:
    bad("旧式应发生键值碰撞 (证明缺陷)")
if not kn0 < kn1:
    bad("新式应把 waste=0 排在 waste=1 之前")

# ---------------------------------------------------------------------------
# 2. 选择循环反例: 旧式确实让 waste 更大的空位胜出
# ---------------------------------------------------------------------------
print("[2] 选择循环反例 (镜像 TryTightestSlot 的 < 平手语义)")
# 扫描序: ori=0 先遇到 waste=1 的 (y=0,x=0); ori=1 后遇到 waste=0 的 (y=10,x=0)。
cands = [(1, 0, 0, 0), (0, 10, 0, 1)]
po = pick(cands, lambda c: key_old(c[0], c[1], c[2]))
pn = pick(cands, lambda c: key_new(c[0], c[1], c[2], W_MAX, H_SCROLL))
pt = true_min(cands)
print(f"  候选={cands}")
print(f"  旧式选中 waste={po[0]} (y={po[1]},x={po[2]})   新式选中 waste={pn[0]}   字典序应为 waste={pt[0]}")
checks += 1
if po[0] != 1:
    bad("旧式应错选 waste=1 (缺陷未复现, 本回归无判别力)")
if pn[0] != 0:
    bad("新式应选 waste=0")
if pn != pt:
    bad("新式选择与独立字典序参照不一致")

# ---------------------------------------------------------------------------
# 3. 新式: 与元组字典序严格一致 (小尺寸穷举 + 大尺寸抽样)
# ---------------------------------------------------------------------------
print("[3] 新式字典序一致性")


def check_new(W, H, wastes, yx_pairs, label):
    global checks
    cands = [(w, y, x) for w in wastes for (y, x) in yx_pairs]
    ok, ex = order_preserved(cands, lambda c: key_new(c[0], c[1], c[2], W, H))
    checks += 1
    if not ok:
        bad(f"{label} W={W} H={H}: 新式不保序, 例 {ex}")
    return len(cands)


n_new = 0
# 小尺寸穷举
for W, H in [(1, 1), (2, 3), (5, 5), (8, 16), (11, 14), (17, 10), (24, 10), (7, 5)]:
    yx = [(y, x) for y in range(H) for x in range(W)]
    n_new += check_new(W, H, range(0, 6), yx, "穷举")
# 真实尺寸: H=4096 滚动容器 / H=8192 钳位上限, 全 y 抽样
for W, H in [(128, H_SCROLL), (128, H_CLAMP), (24, H_CLAMP), (1, H_CLAMP), (128, 11), (128, 12)]:
    yx = [(y, x) for y in range(H) for x in range(W)]
    step = max(1, len(yx) // 5000)
    yx = yx[::step]
    n_new += check_new(W, H, range(0, 40), yx, "真实尺寸抽样")
print(f"  新式 {checks} 组排序等价检查, 共 {n_new} 个候选点, 不一致 {fail}")

# ---------------------------------------------------------------------------
# 4. 旧式: H >= 11 必须复现违例; H <= 10 必须精确 (缺陷边界 = y >= 10)
# ---------------------------------------------------------------------------
print("[4] 旧式缺陷边界 (H<=10 精确, H>=11 违例)")
for H in [5, 9, 10, 11, 12, 14, 4096, 8192]:
    W = W_MAX
    yx = [(y, x) for y in range(H) for x in range(W)]
    step = max(1, len(yx) // 4000)
    yx = yx[::step]
    wastes = list(range(0, 8))
    cands = [(w, y, x) for w in wastes for (y, x) in yx]
    ok, ex = order_preserved(cands, lambda c: key_old(c[0], c[1], c[2]))
    checks += 1
    if H <= 10:
        if not ok:
            bad(f"H={H} (所有 y<10) 旧式应精确, 却违例, 例 {ex}")
        else:
            print(f"  H={H:>5}: 违例 0  -> 旧式在此尺寸精确 (y<10 全部可达)")
    else:
        if ok:
            bad(f"H={H} 旧式应有违例(y>=10 可达), 实得 0 -> 本回归无判别力")
        else:
            a, b = ex
            print(f"  H={H:>5}: 违例已复现, 例 key_old{a}={key_old(*a)} 却排在 key_old{b}={key_old(*b)} 之前")

# ---------------------------------------------------------------------------
# 5. 新式不溢出 long
# ---------------------------------------------------------------------------
print("[5] 新式 long 溢出检查")
W, H = W_MAX, H_CLAMP
waste_max = W * H - 1          # waste = gw*gh - cs.Count <= W*H - 1
kmax = ((waste_max * H) + (H - 1)) * W + (W - 1)
print(f"  W={W} H={H} waste_max={waste_max} -> max key={kmax} (long max={LONG_MAX})")
checks += 1
if kmax > LONG_MAX:
    bad(f"新式键 {kmax} 溢出 long")

import random
random.seed(20260921)
ovf = 0
for _ in range(200000):
    W = random.randint(1, W_MAX)
    H = random.choice([10, 11, 14, 21, 4096, H_CLAMP])
    w = random.randint(0, W * H - 1)
    y = random.randint(0, H - 1)
    x = random.randint(0, W - 1)
    if key_new(w, y, x, W, H) > LONG_MAX:
        ovf += 1
checks += 1
if ovf:
    bad(f"{ovf} 个随机键溢出 long")

print(f"\n共 {checks} 组断言")
print("PASS" if fail == 0 else f"FAIL ({fail})")
raise SystemExit(1 if fail else 0)
