"""redteam_shrinkrects.py -- 红队独立复核 ShrinkRects 去包含改写 (Core.cs:2661-2719).

旧版(0d69984): 双循环 i,j 全配对, 命中即 covered, kept 按 i 升序 append.
新版(修复):   面积降序索引 byArea + 单向剪枝 `areas[j] < ra -> break`.

验证:
  A) 逐位等价(内容 + **输出顺序**): 随机批量对拍。
  B) 剪枝正确性: `areas[j] < ra -> break` 是否在等面积时漏判 —— 构造等面积互相包含
     / 重复矩形用例专测。
  C) 原地修改语义: 旧版是 `rects.Clear()` 后回填, 新版是否一致; 输入列表被 alias
     时行为是否与旧版相同。
  D) 判别力: 故意把剪枝改成 `areas[j] <= ra -> break`(严格小于) 或去掉剪枝,
     确认对拍脚本能抓到差异(证明测试有判别力, 不是恒绿)。
"""
import random
import sys

fail = []


def check(cond, msg):
    if not cond:
        fail.append(msg)
        print("  FAIL " + msg)


def shrink_old(rects, px, py, pw, ph):
    nxt = []
    for (rx, ry, rw, rh) in rects:
        if rx + rw <= px or px + pw <= rx or ry + rh <= py or py + ph <= ry:
            nxt.append((rx, ry, rw, rh))
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
    out = []
    for i in range(len(nxt)):
        r = nxt[i]
        covered = False
        for j in range(len(nxt)):
            if i == j:
                continue
            o = nxt[j]
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if not covered:
            out.append(r)
    rects.clear()
    rects.extend(out)
    return rects


def shrink_new(rects, px, py, pw, ph, mode="new"):
    nxt = []
    for (rx, ry, rw, rh) in rects:
        if rx + rw <= px or px + pw <= rx or ry + rh <= py or py + ph <= ry:
            nxt.append((rx, ry, rw, rh))
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
    n = len(nxt)
    by_area = list(range(n))
    areas = [(r[2] * r[3]) for r in nxt]
    by_area.sort(key=lambda i: -areas[i])
    kept = []
    for i in range(n):
        r = nxt[i]
        ra = areas[i]
        covered = False
        for k in range(n):
            j = by_area[k]
            if mode == "wrong_le":
                # 错误剪枝: 等面积也 break => 可能跳过等面积的覆盖者
                if areas[j] <= ra:
                    break
            elif mode == "noprune":
                pass
            else:
                if areas[j] < ra:
                    break
            if j == i:
                continue
            o = nxt[j]
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if not covered:
            kept.append(r)
    rects.clear()
    rects.extend(kept)
    return rects


def make_case(rnd):
    n = rnd.randrange(0, 14)
    rects = []
    for _ in range(n):
        if rnd.random() < 0.5 and rects:
            # 制造重复 / 包含 / 等面积
            base = rnd.choice(rects)
            kind = rnd.random()
            if kind < 0.4:
                rects.append(base)
            elif kind < 0.7:
                rects.append((base[0] + rnd.randrange(0, max(1, base[2])),
                              base[1] + rnd.randrange(0, max(1, base[3])),
                              rnd.randrange(1, base[2] + 1),
                              rnd.randrange(1, base[3] + 1)))
            else:
                a = rnd.randrange(1, 9)
                rects.append((rnd.randrange(0, 12), rnd.randrange(0, 12), a, base[2] * base[3] // a if base[2] * base[3] // a >= 1 else 1))
        else:
            rects.append((rnd.randrange(0, 12), rnd.randrange(0, 12),
                          rnd.randrange(1, 7), rnd.randrange(1, 7)))
    cut = (rnd.randrange(0, 10), rnd.randrange(0, 10), rnd.randrange(1, 6), rnd.randrange(1, 6))
    return rects, cut


def test_equivalence():
    print("[A] 随机批量对拍: 旧 O(R^2) vs 新 面积降序+剪枝 (内容+顺序)")
    rnd = random.Random(20260921)
    diff = 0
    total = 0
    dup_cases = 0
    for _ in range(200000):
        rects, cut = make_case(rnd)
        if len(set(rects)) != len(rects):
            dup_cases += 1
        a = shrink_old(list(rects), *cut)
        b = shrink_new(list(rects), *cut)
        total += 1
        if a != b:
            diff += 1
            if diff <= 5:
                print("  差异: in=%s cut=%s\n    old=%s\n    new=%s" % (rects, cut, a, b))
    print("  对拍 %d 例, 差异 %d (含重复/包含矩形例 %d)" % (total, diff, dup_cases))
    check(diff == 0, "新旧 ShrinkRects 不逐位等价 (%d 例)" % diff)


def test_equal_area_prune():
    print("[B] 等面积互相包含 / 剪枝边界专测")
    cases = [
        # (rects, cut)  -- 等面积互相包含: 全等矩形
        ([(0, 0, 3, 3), (0, 0, 3, 3), (0, 0, 3, 3)], (5, 5, 1, 1)),
        # 等面积, 其中一个包含另一个(等面积包含 => 全等)
        ([(0, 0, 4, 2), (0, 0, 2, 4)], (9, 9, 1, 1)),
        # 面积相同但不互相包含
        ([(0, 0, 2, 6), (3, 0, 6, 2)], (9, 9, 1, 1)),
        # 大矩形包含小矩形, 面积不同
        ([(0, 0, 5, 5), (1, 1, 2, 2)], (9, 9, 1, 1)),
        # 空输入
        ([], (1, 1, 1, 1)),
        # 单矩形被切
        ([(0, 0, 4, 4)], (1, 1, 2, 2)),
    ]
    for rects, cut in cases:
        a = shrink_old(list(rects), *cut)
        b = shrink_new(list(rects), *cut)
        tag = "OK " if a == b else "DIFF"
        print("  %s in=%s cut=%s -> old=%s new=%s" % (tag, rects, cut, a, b))
        check(a == b, "等面积/包含专测不等价: in=%s cut=%s" % (rects, cut))
    # 直接构造: 等面积 j 排在 i 之前的情形, 验证 `areas[j] < ra` 不会提前 break
    nxt = [(0, 0, 2, 2), (0, 0, 2, 2)]  # 等面积, j 完全覆盖 i
    r0, r1 = nxt[0], nxt[1]
    # 模拟新算法对 i=0 的判定: byArea 降序(等面积时顺序不定, 但都 >= ra)
    areas = [4, 4]
    ra = areas[0]
    broke = False
    for j in (1, 0):
        if areas[j] < ra:
            broke = True
            break
    check(not broke, "等面积时 `areas[j] < ra` 提前 break => 会漏判覆盖")
    print("  等面积剪枝边界: 未提前 break (areas[j]<ra 为假) OK")


def test_alias_and_inplace():
    print("[C] 原地修改 / alias 语义")
    rnd = random.Random(99)
    bad = 0
    for _ in range(5000):
        rects, cut = make_case(rnd)
        base = list(rects)
        a = shrink_old(base, *cut)
        # 旧版对同一个 list 对象原地 Clear+回填
        if a is not base:
            bad += 1
        b = shrink_new(list(rects), *cut)
        if a != b:
            bad += 1
    print("  5000 例原地语义检查, 异常 %d" % bad)
    check(bad == 0, "原地修改语义与旧版不一致")


def test_discriminating_power():
    print("[D] 判别力: 故意注入错误剪枝, 确认对拍能抓到")
    rnd = random.Random(5)
    caught_wrong_le = 0
    noprune_diff = 0
    for _ in range(20000):
        rects, cut = make_case(rnd)
        good = shrink_old(list(rects), *cut)
        bad1 = shrink_new(list(rects), *cut, mode="wrong_le")
        slow = shrink_new(list(rects), *cut, mode="noprune")
        if good != bad1:
            caught_wrong_le += 1
        if good != slow:
            noprune_diff += 1
    print("  错误剪枝(<=)被捕获 %d/20000" % caught_wrong_le)
    print("  去掉剪枝(纯 O(R^2), 结果应相同) 与正确版差异 %d/20000" % noprune_diff)
    check(caught_wrong_le > 0, "测试对『等面积漏判』无判别力")
    # 去掉剪枝只是变慢, 结果必须不变 —— 证明剪枝是「结果中性」的
    check(noprune_diff == 0, "剪枝改变了结果(剪枝非结果中性): %d 例" % noprune_diff)


if __name__ == "__main__":
    test_equivalence()
    test_equal_area_prune()
    test_alias_and_inplace()
    test_discriminating_power()
    print()
    if fail:
        print("RESULT: FAIL (%d)" % len(fail))
        for f in fail:
            print("  - " + f)
        sys.exit(1)
    print("RESULT: PASS")
