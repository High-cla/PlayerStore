# -*- coding: utf-8 -*-
"""量化 FreerectDedup(去包含 O(R^2)) 在 Guillotine 路径的热度。

镜像 Core.cs:2110 TryGuillotine + :2194 GuillotinePlace + :2216 FreerectDedup。
真实 dump 回放, 统计:
  ① GuillotinePlace 调用次数 = FreerectDedup 调用次数
  ② 每次去包含的比较次数(pairwise 探测量), 即 O(R^2) 的实际规模
  ③ free rect 池规模的分布

判据: 若池规模常态很小(如 <10) ⇒ O(R^2) 无关痛痒, 不该改;
      若池能到几十上百且逐件重扫 ⇒ 优化有收益。
"""
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bench_banded as bb          # noqa: E402
import bench_native as bn          # noqa: E402
from verify_all import rotations_of  # noqa: E402

st = {"place": 0, "pairwise": 0, "pool_max": 0, "pool_sum": 0, "pool_n": 0, "removed": 0}


def dedup(rects):
    """镜像 FreerectDedup: 倒序遍历 O(R^2)。返回比较次数。"""
    cmp_n = 0
    before = len(rects)
    for i in range(len(rects) - 1, -1, -1):
        r = rects[i]
        if r[2] <= 0 or r[3] <= 0:
            rects.pop(i)
            continue
        covered = False
        for j in range(len(rects)):
            cmp_n += 1
            if i == j:
                continue
            o = rects[j]
            if o[0] <= r[0] and o[1] <= r[1] and o[0] + o[2] >= r[0] + r[2] and o[1] + o[3] >= r[1] + r[3]:
                covered = True
                break
        if covered:
            rects.pop(i)
    st["pairwise"] += cmp_n
    st["removed"] += before - len(rects)
    return rects


def guillotine(rot, items, W, H):
    """镜像 TryGuillotine 主体: 每件挑最小 waste+死洞 的 (rect, 朝向), 落位后割裂 + 去包含。"""
    freerects = [(0, 0, W, H)]
    occ = set()
    pos = {}
    for idx, ri in enumerate(rot):
        best = None
        for fi, (fx, fy, fw, fh) in enumerate(freerects):
            for o in range(4):
                cs, gw, gh = ri[o]
                if gw > fw or gh > fh:
                    continue
                waste = fw * fh - sum(len(ri[oo][0]) for oo in range(4)) // 4
                if best is None or waste < best[0]:
                    best = (waste, fi, o, gw, gh)
        if best is None:
            return None
        _, fi, o, gw, gh = best
        bx, by, bw, bh = freerects[fi]
        freerects.pop(fi)
        if bh - gh > 0:
            freerects.append((bx, by + gh, bw, bh - gh))
        if bw - gw > 0:
            freerects.append((bx + gw, by, bw - gw, bh))
        st["place"] += 1
        st["pool_sum"] += len(freerects)
        st["pool_n"] += 1
        if len(freerects) > st["pool_max"]:
            st["pool_max"] = len(freerects)
        dedup(freerects)
        if not freerects:
            return None
    return pos


d = bb.dump_sessions()
sess = 0
ok = 0
for (W, H, items) in d:
    rot = [bn.rots_of(i) for i in items]
    sess += 1
    if guillotine(rot, items, W, H) is not None:
        ok += 1
    if sess >= 120:
        break

print(f"=== Guillotine 路径热度（真实 dump 前 {sess} 会话）===")
print(f"  grouped_ok = {ok}/{sess}")
print(f"  GuillotinePlace 调用 = {st['place']} 次 (即 FreerectDedup 调用次数)")
print(f"  去包含 pairwise 比较总量 = {st['pairwise']}")
print(f"  平均每次去包含比较 = {st['pairwise']/max(1,st['place']):.1f} 次")
print(f"  free rect 池: 平均 {st['pool_sum']/max(1,st['pool_n']):.1f}, 峰值 {st['pool_max']}")
print(f"  去包含移除矩形 = {st['removed']} 个")
print()
print(f"  对比: FindFreeRects 每次调用分配 int[W]+int[W+1] (W<=24) = ~49 元素")
print(f"        FreerectDedup 每次 O(R^2), R 峰值 = {st['pool_max']}")
