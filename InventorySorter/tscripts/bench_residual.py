# -*- coding: utf-8 -*-
"""Residual(降级残局填充)离线对拍: 治 "排序成功但小件游离 / 近满 abort 整包放弃".

C# 现语义(LayoutDense): 布局器 大先小后, 任一物品放不下 -> 整候选作废(None);
全部候选作废 -> RestoreOriginal + "not enough room, left unchanged" 整包不动.
离线代理: verify_all.pack_meta(MetaBest) 全跑取成功者最大空矩, 失败返 None.

Residual 策略(C# 待移植):
  Stage1: strict MetaBest 成功 -> 直接用(成功路径不变, 不劣化空矩);
  Stage2: strict 失败 -> soft 策略(放不下跳尾继续塞小件, 大先小后保"大件最优"),
          取放下最多件(平手取最大空矩)的 soft 布局 = 降级结果; 真放不下的留在原位.

指标对比: strict 成功率 / Residual 挽救率(placed/total) / 空矩(成功后) / 剩余件数.
"""
import os, sys, random, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import verify_all as va
from parse_dump import parse_dump_merged

BAK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "inv_shape_dump.bak_20260830_231609")


def _rots(it):
    return list(va.rotations_of(it))


def soft_pack(items, W, H, policy):
    """大先小后; 放不下 -> 记 leftover 继续塞更小的(软失败). policy: 'bestfit'|'grow'|'leftbottom'."""
    order = sorted(items, key=lambda i: -len(i["cells"]))
    occ = set()
    placed = []
    leftover = []
    for it in order:
        best = None
        for key, nw, nh in _rots(it):
            cells = list(key)
            for py in range(H - nh + 1):
                for px in range(W - nw + 1):
                    if not va.can_place(occ, cells, px, py, W, H):
                        continue
                    if policy == "bestfit":
                        waste = nw * nh - len(cells)
                        key2 = (waste, py, px)
                    elif policy == "grow":
                        touch = 0
                        for dx, dy in cells:
                            ax, ay = px + dx, py + dy
                            for adx, ady in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                                if (ax + adx, ay + ady) in occ:
                                    touch += 1
                            if ax == 0 or ax == W - 1:
                                touch += 1
                            if ay == 0 or ay == H - 1:
                                touch += 1
                        key2 = (-touch, py, px)
                    else:  # leftbottom: 聚左下
                        key2 = (px, -py)
                    if best is None or key2 < best[0]:
                        best = (key2, px, py, list(cells))
        if best is None:
            leftover.append(it)
            continue
        occ = va.mark(occ, best[3], best[2], best[1])
        placed.append(it)
    return occ, placed, leftover


def residual(items, W, H):
    """Stage1 strict MetaBest; 失败 Stage2 soft-best(最多件,平手最大空矩)."""
    try:
        occ = va.pack_meta(items, W, H)
    except Exception:
        occ = None
    if occ is not None:
        return ("strict", occ, items, [])
    best = None
    for pol in ("bestfit", "grow", "leftbottom"):
        o, pl, lf = soft_pack(items, W, H, pol)
        cand = (len(pl), va.max_empty(o, W, H)[0], o, pl, lf)
        if best is None or cand[:2] > best[:2]:
            best = cand
    o, pl, lf = best[2], best[3], best[4]
    return ("soft", o, pl, lf)


def run_real():
    sess = parse_dump_merged(BAK)
    rows = []
    for w, h, reps in sess:
        items = [dict(r, count=uc) for r, uc in reps]
        total = sum(len(i["cells"]) for i in items)
        if total == 0 or total > w * h:
            continue
        v = w * h
        strict = None
        try:
            so = va.pack_meta(items, w, h)
            if so is not None:
                strict = va.max_empty(so, w, h)[0]
        except Exception:
            pass
        mode, occ, pl, lf = residual(items, w, h)
        # 成功路径额外对比: 3 soft 中最大空矩 (判断 always-residual 是否劣化)
        sba = None
        if strict is not None:
            for pol in ("bestfit", "grow", "leftbottom"):
                o2, _pl2, _lf2 = soft_pack(items, w, h, pol)
                a2 = va.max_empty(o2, w, h)[0]
                sba = a2 if sba is None else max(sba, a2)
        rows.append(dict(w=w, h=h, n=len(items), fill=total / v,
                         strict=strict, mode=mode,
                         placed=len(pl), left=len(lf),
                         ma=va.max_empty(occ, w, h)[0], sba=sba))
    return rows


def run_catalog(seed=7):
    pool = va.catalog_pool()
    feas_ok = []
    sizes = [(24, 10), (10, 10), (9, 7), (8, 8), (17, 10), (11, 14), (8, 9)]
    rows = []
    rng = random.Random(seed)
    for w, h in sizes:
        cap = w * h
        feas = [it for it in pool if any(nw <= w and nh <= h for _, nw, nh in _rots(it))]
        if not feas:
            continue
        avg = sum(len(it["cells"]) for it in feas) / len(feas)
        for fill in (0.60, 0.80, 0.90, 0.97, 1.03):
            target = int(cap * fill)
            n = max(3, target // max(1, int(avg)))
            for _ in range(25):
                raw = [rng.choice(feas) for _ in range(n)]
                # 同类合并投影(还原 mergeRepIdx)
                merged = {}
                for it in raw:
                    k = (it["name"], tuple(sorted(it["cells"])), it["w"], it["h"])
                    if k not in merged:
                        merged[k] = dict(it, count=1)
                    else:
                        merged[k]["count"] += 1
                items = list(merged.values())
                total = sum(len(i["cells"]) for i in items)
                if total > cap:
                    continue
                strict = None
                try:
                    so = va.pack_meta(items, w, h)
                    if so is not None:
                        strict = va.max_empty(so, w, h)[0]
                except Exception:
                    pass
                mode, occ, pl, lf = residual(items, w, h)
                rows.append(dict(w=w, h=h, n=len(items), fill=total / cap,
                                 strict=strict, mode=mode,
                                 placed=len(pl), left=len(lf),
                                 ma=va.max_empty(occ, w, h)[0]))
    return rows


def summarize(tag, rows):
    strict_ok = [r for r in rows if r["strict"] is not None]
    strict_fail = [r for r in rows if r["strict"] is None]
    print(f"\n===== {tag} : {len(rows)} 组  strict成功 {len(strict_ok)} / 失败(当前会abort) {len(strict_fail)} =====")
    if strict_fail:
        rec = sum(r["placed"] for r in strict_fail)
        tot = sum(r["n"] for r in strict_fail)
        left = sum(r["left"] for r in strict_fail)
        fills = [r["fill"] for r in strict_fail]
        print(f"[abort挽救] strict失败组 {len(strict_fail)} 组, Residual 平均 fill={sum(fills)/len(fills):.2f}")
        print(f"  Residual 放下 {rec}/{tot} ({100*rec/tot:.1f}%), 剩余(留原位) {left} 件; 平均空矩 {sum(r['ma'] for r in strict_fail)/len(strict_fail):.1f}")
    # 成功路径: residual(strict 分支) 应与 strict 同; 验证 ma 不劣化
    bad = [r for r in strict_ok if r["mode"] == "soft"]
    if bad:
        print(f"  [注意] {len(bad)} 组 strict成功但 residual 走 soft 分支(不该发生)")
    nondegrade = sum(1 for r in strict_ok if r["ma"] >= r["strict"])
    print(f"[成功路径] strict成功组 {len(strict_ok)} 中 residual 空矩>=strict: {nondegrade}/{len(strict_ok)}")
    # 按 fill 分桶: 近满组表现
    hi = [r for r in rows if r["fill"] >= 0.90]
    if hi:
        hi_succ = sum(1 for r in hi if r["strict"] is not None)
        rec = sum(r["placed"] for r in hi) / sum(r["n"] for r in hi)
        print(f"[近满 fill>=0.90] {len(hi)} 组: strict成功 {hi_succ}, Residual 放下率 {100*rec:.1f}%")


if __name__ == "__main__":
    rr = run_real()
    summarize("真实 dump(bak)", rr)
    ok = [r for r in rr if r["strict"] is not None]
    if ok:
        deg = [r for r in ok if r["sba"] is not None and r["sba"] < r["strict"]]
        eqgt = len(ok) - len(deg)
        print(f"[成功路径·soft vs strict] 真实组 strict成功 {len(ok)}: soft(空矩)>=strict {eqgt} 组, soft<strict(劣化) {len(deg)} 组" +
              (f" 例:{[(r['w'],r['h'],r['strict'],r['sba']) for r in deg[:5]]}" if deg else ""))
    for r in rr:
        if r["strict"] is None:
            print(f"   真实组 {r['w']}x{r['h']} n={r['n']} fill={r['fill']:.2f} -> Residual 放下 {r['placed']}/{r['n']} 剩 {r['left']} 空矩 {r['ma']}")
    cr = run_catalog()
    summarize("catalog 合成池(FILL扫描)", cr)
