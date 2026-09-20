# -*- coding: utf-8 -*-
"""实机数据回放: 用真实 dump(341 会话)对拍本次改动的旧/新实现。

背景: 既有三支探针(probe_tightest / probe_shrinkrects / probe_tightest_domain)全部
使用**随机合成网格**; probe_shrinkrects.py:158 的「真实网格」标签实际是
random.randint(1,12), 并未读取仓库内真实 dump。本脚本补齐这一缺口。

数据: tscripts/inv_shape_dump.bak_20260830_231609 (269KB, 341 个会话, 仓库内快照)
      网格尺寸分布: 10x10 x111 / 24x10 x67 / 17x10 x49 / 8x8 x45 / 11x14 x28 /
      9x7 x27 / 8x9 x3 / 6x8 x3 / 5x5 x3 / 8x6 x2 / 14x21 x2 / 7x5 x1
      注意 H 最大 21 > 10 ⇒ 真实数据确实能走到 y>=10 的键编码区。

对拍项:
  A. TryTightestSlot 旧键 vs 新键 —— 在真实 occ + 真实物品形状上取位必须完全一致。
     预期: 一致。因为 waste 在四朝向为常数(旋转是双射 ⇒ cs.Count 同值; gw*gh 同值),
     故旧式虽隐含 y<10, 但在 waste 恒定时比较退化为 (y,x) 字典序, 仍然正确。
     本项用于**证伪「旧式在真实数据上产生错误结果」**这一猜测。
  B. ShrinkRects 旧 O(R^2) vs 新「面积降序 + 单向剪枝」—— 在真实 occ 上反复切块,
     要求内容多重集与输出顺序**逐位一致**。
  C. 键编码判别力自检: 构造 waste 非常数的场景, 证明旧式确实会错(说明 A 的一致
     来自 waste 恒定这一事实, 而非测试无判别力)。
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from parse_dump import parse_dump  # noqa: E402

FAILS = []


def check(cond, msg):
    if not cond:
        FAILS.append(msg)
        print("  FAIL " + msg)
    return cond


# ---------- 基础镜像(Core.cs) ----------

def rotate(cells, times, w, h):
    """C0 旋转 times 次(与 BuildMask 的 C1..C3 一致), 归一化到 (0,0)。"""
    cur = list(cells)
    wd, sd = w, h
    for _ in range(times):
        cur = [(wd - 1 - y, x) for x, y in cur]
        wd, sd = sd, wd
    if not cur:
        return []
    mx = min(x for x, y in cur)
    my = min(y for x, y in cur)
    return [(x - mx, y - my) for x, y in cur]


def bbox(cells):
    return (max(x for x, y in cells) + 1, max(y for x, y in cells) + 1)


def mask_of(it):
    """镜像 BuildMask: C0..C3 + Gw0/Gh0。"""
    c0 = list(it["cells"])
    w0, h0 = bbox(c0)
    cs = [rotate(c0, k, w0, h0) for k in range(4)]
    return {"c": cs, "gw": w0, "gh": h0}


def cells_free(occ, W, H, x, y, cs):
    for dx, dy in cs:
        cx, cy = x + dx, y + dy
        if cx < 0 or cy < 0 or cx >= W or cy >= H:
            return False
        if occ[cx][cy]:
            return False
    return True


# ---------- A: TryTightestSlot 旧键 vs 新键 ----------

def tightest(occ, W, H, m, use_new):
    best = None
    px = py = po = -1
    for ori in range(4):
        cs = m["c"][ori]
        if not cs:
            continue
        if ori in (1, 3):
            gw, gh = m["gh"], m["gw"]
        else:
            gw, gh = m["gw"], m["gh"]
        if gw > W or gh > H:
            continue
        waste = gw * gh - len(cs)
        for y in range(0, H - gh + 1):
            for x in range(0, W - gw + 1):
                if not cells_free(occ, W, H, x, y, cs):
                    continue
                if use_new:
                    key = ((waste * H) + y) * W + x
                else:
                    key = waste * 1000000 + y * 100000 + x
                if best is None or key < best:
                    best = key
                    px, py, po = x, y, ori
    return (px, py, po) if best is not None else None


# ---------- B: ShrinkRects 旧 vs 新 ----------

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
    for i, r in enumerate(nxt):
        covered = False
        for j, o in enumerate(nxt):
            if i == j:
                continue
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if not covered:
            out.append(r)
    return out


def shrink_new(rects, px, py, pw, ph):
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
    areas = [(r[2] * r[3]) for r in nxt]
    by_area = sorted(range(n), key=lambda i: -areas[i])
    out = []
    for i in range(n):
        r = nxt[i]
        ra = areas[i]
        covered = False
        for k in range(n):
            j = by_area[k]
            if areas[j] < ra:
                break
            if i == j:
                continue
            o = nxt[j]
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if not covered:
            out.append(r)
    return out


def free_rects(occ, W, H):
    """镜像 Core.cs:2973 FindFreeRects 前半(单调栈), 生成矩形池(含重复)。"""
    rects = []
    height = [0] * W
    stack = [0] * (W + 1)
    for y in range(H):
        for x in range(W):
            height[x] = 0 if occ[x][y] else height[x] + 1
        top = -1
        for x in range(W + 1):
            cur = 0 if x == W else height[x]
            while top >= 0 and height[stack[top]] >= cur:
                h = height[stack[top]]
                top -= 1
                left = 0 if top < 0 else stack[top] + 1
                right = x - 1
                if h > 0:
                    rects.append((left, y - h + 1, right - left + 1, h))
            top += 1
            stack[top] = x
    return rects


def occ_from_session(W, H, items):
    occ = [[False] * H for _ in range(W)]
    for it in items:
        for (x, y) in it["cells"]:
            if 0 <= x < W and 0 <= y < H:
                occ[x][y] = True
    return occ


def main():
    dump = os.path.join(HERE, "inv_shape_dump.bak_20260830_231609")
    if not os.path.exists(dump):
        print("SKIP: 未找到真实 dump " + dump)
        return 2
    sessions = parse_dump(dump)
    print("真实会话数: %d" % len(sessions))

    # ---- A: TryTightestSlot 旧键 vs 新键 ----
    print("\n[A] TryTightestSlot 旧键 vs 新键 (真实 occ + 真实形状)")
    cases = 0
    diff = 0
    y10 = 0          # 取位落在 y>=10 的次数(真实数据可达)
    nostd = 0
    for (W, H, items) in sessions:
        if not items:
            continue
        occ = occ_from_session(W, H, items)
        for it in items:
            m = mask_of(it)
            cases += 1
            o = tightest(occ, W, H, m, use_new=False)
            nw = tightest(occ, W, H, m, use_new=True)
            if o is None:
                nostd += 1
            elif o[1] >= 10:
                y10 += 1
            if o != nw:
                diff += 1
                if diff <= 5:
                    print("    差异: %s W=%d H=%d old=%s new=%s" % (it["name"], W, H, o, nw))
    print("    物品用例 %d, 无可行位 %d, 取位 y>=10 的用例 %d" % (cases, nostd, y10))
    print("    旧键 vs 新键 不一致: %d" % diff)
    check(diff == 0, "A: 旧/新键在真实数据上必须一致(不一致 %d)" % diff)

    # A2: waste 跨四朝向恒定 —— 这才是旧键正确的充要原因。
    # BuildMask 令 C1..C3 为 C0 的 90/180/270 旋转; 旋转是双射 ⇒ |C0..C3| 同值;
    # gw*gh 在 ori in {0,2} 为 bw*bh, ori in {1,3} 为 bh*bw ⇒ 四朝向同值。
    # waste 恒定 ⇒ 旧键的比较实际退化为 (y,x) 字典序 ⇒ 进位路径不可达 ⇒ 旧式从未出错。
    print("  [A2] waste 跨四朝向恒定(旧键正确的充要原因)")
    nonconst = 0
    for (W, H, items) in sessions:
        for it in items:
            m = mask_of(it)
            ws = set()
            for ori in range(4):
                cs = m["c"][ori]
                if not cs:
                    continue
                gw, gh = (m["gh"], m["gw"]) if ori in (1, 3) else (m["gw"], m["gh"])
                ws.add(gw * gh - len(cs))
            if len(ws) > 1:
                nonconst += 1
                if nonconst <= 5:
                    print("    waste 非常数: %s W=%d H=%d ws=%s" % (it["name"], W, H, sorted(ws)))
    print("    waste 非常数的物品数: %d" % nonconst)
    check(nonconst == 0, "A2: 真实数据上 waste 应跨朝向恒定(非常数 %d)" % nonconst)

    # A3: 显式构造「仅 y>=10 区域可用」的真实尺寸网格, 强制走到进位区。
    # 因 waste 恒定, 旧键在此仍正确 —— 用于证伪「旧式在真实数据上产生错误结果」。
    print("  [A3] 强制 y>=10 取位(仅底部空), 旧/新键仍须一致")
    forced = 0
    fdiff = 0
    for (W, H, items) in sessions:
        if H <= 12 or not items:
            continue
        for it in items[:6]:
            m = mask_of(it)
            cs = m["c"][0]
            if not cs:
                continue
            gw, gh = m["gw"], m["gh"]
            if gw > W or gh > H:
                continue
            # 只在 y>=10 的区域留空, 其余全占
            occ = [[True] * H for _ in range(W)]
            for y in range(10, H):
                for x in range(W):
                    occ[x][y] = False
            o = tightest(occ, W, H, m, use_new=False)
            nw = tightest(occ, W, H, m, use_new=True)
            if o is None:
                continue
            forced += 1
            if o[1] >= 10:
                pass
            if o != nw:
                fdiff += 1
                if fdiff <= 5:
                    print("    差异: %s W=%d H=%d old=%s new=%s" % (it["name"], W, H, o, nw))
    print("    强制场景 %d, 旧/新不一致 %d" % (forced, fdiff))
    check(forced > 0, "A3: 应至少构造出 %d 个强制场景" % forced)
    check(fdiff == 0, "A3: 仅底部可用时旧/新键仍须一致(不一致 %d)" % fdiff)

    # ---- B: ShrinkRects 旧 vs 新 ----
    print("\n[B] ShrinkRects 旧 O(R^2) vs 新(面积降序+剪枝), 真实 occ 反复切块")
    seqs = 0
    bdiff = 0
    for (W, H, items) in sessions:
        if not items:
            continue
        occ = occ_from_session(W, H, items)
        ro = free_rects(occ, W, H)
        rn = list(ro)
        if len(ro) < 2:
            continue
        for it in items:
            m = mask_of(it)
            cs = m["c"][0]
            if not cs:
                continue
            gw, gh = m["gw"], m["gh"]
            # 取该物品的一个真实可放位(没有就跳过)
            spot = None
            for y in range(0, max(1, H - gh + 1)):
                for x in range(0, max(1, W - gw + 1)):
                    if cells_free(occ, W, H, x, y, cs):
                        spot = (x, y)
                        break
                if spot:
                    break
            if not spot:
                continue
            px, py = spot
            ro = shrink_old(ro, px, py, gw, gh)
            rn = shrink_new(rn, px, py, gw, gh)
            seqs += 1
            if ro != rn:
                bdiff += 1
                if bdiff <= 5:
                    print("    差异: W=%d H=%d px=%d py=%d gw=%d gh=%d old_n=%d new_n=%d"
                          % (W, H, px, py, gw, gh, len(ro), len(rn)))
            # 放置后更新 occ, 继续下一件(模拟逐件放置的增量切块)
            for dx, dy in cs:
                cx, cy = px + dx, py + dy
                if 0 <= cx < W and 0 <= cy < H:
                    occ[cx][cy] = True
        if seqs > 200000:
            break
    print("    切块序列 %d, 旧 vs 新 不一致: %d" % (seqs, bdiff))
    check(bdiff == 0, "B: ShrinkRects 旧/新必须逐位一致(不一致 %d)" % bdiff)
    check(seqs > 500, "B: 切块样本过少(%d), 覆盖不足" % seqs)

    # ---- C: 判别力自检(waste 非常数时旧式确实错) ----
    print("\n[C] 判别力自检: waste 非常数时旧键必须出错")
    # 两个候选: (waste=0, y=10, x=0) 与 (waste=1, y=0, x=0)
    old_a = 0 * 1000000 + 10 * 100000 + 0
    old_b = 1 * 1000000 + 0 * 100000 + 0
    new_a = ((0 * 4096) + 10) * 128 + 0
    new_b = ((1 * 4096) + 0) * 128 + 0
    print("    old: waste0/y10 = %d, waste1/y0 = %d -> 旧式误判 waste1 更优? %s"
          % (old_a, old_b, old_b <= old_a))
    print("    new: waste0/y10 = %d, waste1/y0 = %d -> 新式正确? %s"
          % (new_a, new_b, new_a < new_b))
    check(old_b <= old_a, "C: 旧式在 waste 变化时应出错(证明 A 的一致来自 waste 恒定)")
    check(new_a < new_b, "C: 新式在 waste 变化时应正确")

    print("\n" + ("RESULT: PASS" if not FAILS else "RESULT: FAIL (%d)" % len(FAILS)))
    return 0 if not FAILS else 1


if __name__ == "__main__":
    sys.exit(main())
