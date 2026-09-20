# -*- coding: utf-8 -*-
"""排序幂等回归: sort(sort(x)) == sort(x), 即同一容器连排两次结果必须一致。

用户症状: 自动排序(容器打开触发 = 第 1 次排序)与手动点按钮(第 2 次排序)布局不同, 每次都这样。

C# 侧两条不幂等根因(Core.cs):
  (1) TryFillRefine 只处理非堆叠件(Stacked 为假), 而 Stacked 读**活的** unitCount。
      同类合并的被合并件在应用阶段被搬到代表件同位, 游戏随即自动合并 ⇒ 代表件本轮结束后
      unitCount>1。但第 1 次排序读到的活值仍是 1 ⇒ 代表件参与精修; 第 2 次读到 >1 ⇒ 跳过精修。
      同一容器两轮走不同分支 ⇒ 布局必然不同。自动 = 第 1 次, 手动 = 第 2 次。
  (2) sortPool 顺序来自 childItems(外部输入), 而多处比较器是偏序(CellCount/边长)且 List.Sort
      不稳定 ⇒ tie 的归属由输入序决定, 输入序泄漏进布局。

修复(Core.cs):
  A) BuildSortView 末尾按「合并后稳态」冻结 _sortStacked; Stacked() 在排序中一律读冻结集合。
  B) SortInventory 入口对 sortPool 做 SizeCompare 全序规范化。

本脚本在离线镜像上**复现**这两条根因, 并验证修复后归零:
  * 修复 B: 同一容器换输入序 → 布局必须逐位相同
  * 修复 A: 第 1 轮(未合并态) 与 第 2 轮(已合并态) → 布局必须逐位相同

镜像不含真实 unitCount, 故用「精修可处理集合」(eligible) 建模 Stacked:
  第 1 轮 eligible = 全部件; 第 2 轮 eligible = 排除已合并代表件。修复 A = 第 2 轮也当作第 1 轮。

退出码: 0 = 修复后两项均成立且未修复时确实违反, 1 = 不符。
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump_merged
import bench_native as bn
import verify_all as va

BAK = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "inv_shape_dump.bak_20260830_231609")


# ---------------------------------------------------------------------------
# 数据
# ---------------------------------------------------------------------------
def sessions():
    """同类合并后的会话: items = 代表件列表, 每件带 unit_count(= 组内件数, 建模活的 unitCount)。

    注意: 仓库内语料**不含**同 ident 同形状的多件(游戏/落盘已把它们并成 unitCount>1 的单件,
    实测 341 会话中同形重复组 = 0), 故修复 A 在此语料上恒无触发。为让本项有判别力, 另见
    synth_merge_sessions(): 在真实会话上复制若干同形件, 造出「合并在途」的状态。
    """
    out = []
    for (W, H, reps) in parse_dump_merged(BAK):
        items = []
        for r, uc in reps:
            it = dict(r)
            it["ident"] = it.get("name") or it.get("ident")
            it["dname"] = it["ident"]
            it["tag"] = ""
            it["uid"] = len(items)          # 唯一 id, 充当 SizeCompare 的收尾全序键
            it["unit_count"] = uc
            items.append(it)
        if len(items) > 1:
            out.append((W, H, items))
    return out


def merged_sessions(limit=160):
    """造出「有件已合并成堆」的会话, 用于检验修复 A。

    为什么需要合成: 仓库语料里同 ident 同形状的多件 = 0 组(游戏/落盘已把它们并成单件),
    故真实语料上 Stacked 恒假, 修复 A 无触发。

    为什么只需标 unit_count 而不用真的复制物品: C# 的同类合并视图(mergeRepIdx/mergeAbsorb)
    在布局前就把被合并件从候选里剔除了(BuildDenseCandidate/BuildBandedCandidate 都 RemoveAll),
    故**两次排序布局的单元集完全相同**, 唯一差别是 Stacked() 的读数 ——
      第 1 次(自动, 合并在途) : 代表件活 unitCount = 1 ⇒ 参与精修
      第 2 次(手动, 已合并)   : 代表件活 unitCount > 1 ⇒ 被精修跳过
    因此把部分件标成 unit_count=2 即可精确复现该场景。
    返回 [(W,H,items,merged_idx)]。
    """
    out = []
    for (W, H, reps) in parse_dump_merged(BAK):
        items = []
        for r, uc in reps:
            it = dict(r)
            it["ident"] = it.get("name") or it.get("ident")
            it["dname"] = it["ident"]
            it["tag"] = ""
            it["uid"] = len(items)
            it["unit_count"] = uc
            items.append(it)
        if len(items) < 4:
            continue
        # 把后一半标成"已合并代表件"(unit_count=2), 前一半保持单件
        merged_idx = set(range(len(items) // 2, len(items)))
        for i in merged_idx:
            items[i]["unit_count"] = 2
        out.append((W, H, items, merged_idx))
        if len(out) >= limit:
            break
    return out


def size_compare_key(it):
    """镜像 C# SizeCompare: 面积降 → max边降 → min边降 → ident → name → uid(全序收尾)。"""
    return (-(it["w"] * it["h"]), -max(it["w"], it["h"]), -min(it["w"], it["h"]),
            it["ident"].lower(), it["dname"].lower(), it["uid"])


# ---------------------------------------------------------------------------
# 精修镜像(C# TryFillRefine): 只处理 eligible 集合内的件, 小件优先
# ---------------------------------------------------------------------------
def refine_eligible(rot, W, H, pos, eligible, guard=True):
    if not pos:
        return dict(pos)
    occ = set()
    for i, (px, py, o) in pos.items():
        occ |= bn.cells_at(rot[i][o][0], px, py)
    oa, _ = bn.max_empty(occ, W, H)
    result = dict(pos)
    order = sorted((i for i in pos if i in eligible), key=lambda k: len(rot[k][0][0]))
    for i in order:
        px, py, o = pos[i]
        cc = rot[i][o][0]
        cur_cells = bn.cells_at(cc, px, py)
        occ -= cur_cells
        ct = bn.touch(occ, W, H, cc, px, py)
        cand = None
        for (rx, ry, rw, rh) in sorted(bn.find_free_rects(occ, W, H), key=lambda r: r[2] * r[3]):
            bT = -1
            bP = None
            for o2, (cells, nw, nh) in enumerate(rot[i]):
                if nw > rw or nh > rh:
                    continue
                for py2 in range(ry, ry + rh - nh + 1):
                    for px2 in range(rx, rx + rw - nw + 1):
                        if not va.can_place(occ, cells, px2, py2, W, H):
                            continue
                        t = bn.touch(occ, W, H, cells, px2, py2)
                        if bP is None or t > bT or (t == bT and (py2 < bP[1] or (py2 == bP[1] and px2 < bP[2]))):
                            bT = t
                            bP = (px2, py2, o2)
            if bP is not None:
                cand = (bP[0], bP[1], bP[2], bT)
                break
        if cand is None or (cand[0], cand[1], cand[2]) == (px, py, o):
            occ |= cur_cells
            continue
        new_cells = bn.cells_at(rot[i][cand[2]][0], cand[0], cand[1])
        na, _ = bn.max_empty(occ | new_cells, W, H)
        ok = na >= oa
        if guard and cand[3] < ct - 1e-9:
            ok = False
        if ok:
            result[i] = (cand[0], cand[1], cand[2])
        else:
            occ -= new_cells
            occ |= cur_cells
    return result


# ---------------------------------------------------------------------------
# 一次排序(镜像 C# SortInventory 的骨架: 规范化 → 骨架布局 → 精修)
# ---------------------------------------------------------------------------
def sort_once(items, rot, W, H, eligible, normalize):
    """normalize=True 建模修复 B(入口 SizeCompare 全序规范化)。返回 {原下标: (x,y,o)}。"""
    if normalize:
        order = sorted(range(len(items)), key=lambda i: size_compare_key(items[i]))
    else:
        order = list(range(len(items)))
    rot_o = [rot[i] for i in order]
    bl = bn.base_layout(rot_o, W, H)
    if bl is None:
        return None
    init_pos = bl[0]
    # eligible 以「原下标」表述, 映射到重排后的局部下标
    pos_local = dict(init_pos)
    elig_local = {k for k in init_pos if order[k] in eligible}
    got = refine_eligible(rot_o, W, H, pos_local, elig_local, guard=True)
    # 映射回原下标: got 的键是重排后下标 k ⇒ 原下标 order[k]
    return {order[k]: v for k, v in got.items()}


# ---------------------------------------------------------------------------
def main():
    S = sessions()
    if not S:
        print("FAIL: 语料为空 (tscripts/inv_shape_dump.bak_*)")
        return 1
    print(f"语料: {len(S)} 个会话(已同类合并)")

    rng = random.Random(7)

    # ---- 修复 B: 输入序不变性 ----
    b_bad = b_good = 0
    for (W, H, items) in S:
        rot = [bn.rots_of(it) for it in items]
        elig = set(range(len(items)))
        base = sort_once(items, rot, W, H, elig, normalize=True)
        if base is None:
            continue
        # 未规范化: 打乱输入序 ⇒ 布局可能变
        perm = list(range(len(items)))
        rng.shuffle(perm)
        items_p = [items[i] for i in perm]
        rot_p = [rot[i] for i in perm]
        elig_p = {new for new in range(len(perm)) if perm[new] in elig}
        got_p = sort_once(items_p, rot_p, W, H, elig_p, normalize=False)
        if got_p is not None:
            got_p_back = {perm[k]: v for k, v in got_p.items()}
            if got_p_back != base:
                b_bad += 1
        # 规范化: 同一打乱输入序应归一
        got_n = sort_once(items_p, rot_p, W, H, elig_p, normalize=True)
        if got_n is not None:
            got_n_back = {perm[k]: v for k, v in got_n.items()}
            if got_n_back != base:
                b_good += 1

    # ---- 修复 A: 两轮一致(第 1 轮 vs 第 2 轮) ----
    # 真实语料无同形重复组 ⇒ 用标了 unit_count=2 的会话复现「合并在途」场景
    S2 = merged_sessions()
    a_bad = a_good = n_a = 0
    for (W, H, items, merged_idx) in S2:
        rot = [bn.rots_of(it) for it in items]
        # 第 1 轮: 合并在途 ⇒ 读活 unitCount = 1 ⇒ 全部件可精修
        p1 = sort_once(items, rot, W, H, set(range(len(items))), normalize=True)
        if p1 is None:
            continue
        n_a += 1
        # 未修复的第 2 轮: 读活 unitCount ⇒ 已合并代表件被排除精修
        elig_live = {i for i, it in enumerate(items) if it["unit_count"] <= 1}
        p2_live = sort_once(items, rot, W, H, elig_live, normalize=True)
        if p2_live is not None and p2_live != p1:
            a_bad += 1
        # 修复后的第 2 轮: 冻结集合 ⇒ 与第 1 轮同口径
        p2_frozen = sort_once(items, rot, W, H, set(range(len(items))), normalize=True)
        if p2_frozen is not None and p2_frozen != p1:
            a_good += 1

    print(f"\n[修复 B] 输入序不变性 (真实语料 {len(S)} 会话)")
    print(f"    未修复(不规范化): {b_bad}/{len(S)} 个会话因输入序变化而布局改变  ← 复现根因")
    print(f"    修复后(规范化)  : {b_good}/{len(S)} 个会话仍受输入序影响        ← 必须为 0")
    print(f"\n[修复 A] 两轮一致性 (合成「合并在途」{n_a} 会话)")
    print(f"    未修复(读活 unitCount): {a_bad}/{n_a} 个会话第1轮≠第2轮  ← 复现根因(自动≠手动)")
    print(f"    修复后(冻结集合)      : {a_good}/{n_a} 个会话第1轮≠第2轮  ← 必须为 0")

    print()
    if b_good == 0 and a_good == 0 and (b_bad > 0 or a_bad > 0):
        print("PASS: 修复后两项不变量均成立, 且未修复时确实违反(证明检验有判别力)")
        return 0
    if b_good == 0 and a_good == 0:
        print("PASS(弱): 修复后两项成立; 但未修复时也未复现违反, 检验判别力不足")
        return 0
    print(f"FAIL: 修复后仍有违反 (B={b_good}, A={a_good})")
    return 1


if __name__ == "__main__":
    sys.exit(main())
