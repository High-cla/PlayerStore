# -*- coding: utf-8 -*-
"""verify_pool.py: 全物品形状池随机组合压测 → 算法成员胜率统计.

数据: full_item_catalog.json 全部 shapes (178 物品, 多形状变体, uniq 30 形状)
方法: 对 13 个真实背包尺寸 × 每尺寸 200 组随机组合(每组抽 14 件, 随机形状/旋转),
      跑 PG/GB/BestFit/PGSplit/GrowTouch/MetaBest, 统计成功率 + 平均最大空矩.
输出: 每尺寸算法排名 + 全池总胜率 (MetaBest 应零失败; 观察哪个成员拖后腿).
"""
import json, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from verify_all import (ALGOS, max_empty, pack_bestfit_mfr, pack_pg_split, pack_grow_touch)
import verify_real_dump as v

random.seed(42)

# ---- 全形状池 ----
def load_pool():
    c = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    'full_item_catalog.json'), encoding='utf-8'))['records']
    pool = []
    for r in c:
        for s in r.get('shapes', []):
            pool.append({'name': r['stableId'], 'w': s['w'], 'h': s['h'], 'cells': list(map(tuple, s['cells']))})
    return pool

POOL = load_pool()
SIZES = [(24, 10), (9, 7), (10, 10), (8, 8), (17, 10), (11, 14), (8, 9),
         (14, 21), (7, 5), (8, 6), (6, 8), (5, 5), (7, 11)]

def gen_group(W, H, n=14):
    """随机抽 n 件, 随机取该物品一个形状变体"""
    return [dict(random.choice([p for p in POOL if p['name'] == it['name']])) if False else random.choice([p for p in POOL]) for _ in range(n)]

def run_pool():
    total = {k: [0, 0] for k in ALGOS}   # name -> [成功数, 空矩和]
    rows = []
    for W, H in SIZES:
        v.W, v.H = W, H
        per = {k: [0, 0] for k in ALGOS}
        for _ in range(200):
            items = gen_group(W, H)
            for name, fn in ALGOS.items():
                try:
                    occ = fn(items, W, H)
                    if occ:
                        a, _ = max_empty(occ, W, H)
                        per[name][0] += 1; per[name][1] += a
                except Exception:
                    pass
        rows.append((f'{W}x{H}', per))
        for k in ALGOS:
            total[k][0] += per[k][0]; total[k][1] += per[k][1]
    return rows, total

if __name__ == '__main__':
    rows, total = run_pool()
    print(f"{'组':<7} " + " ".join(f"{k:<11}" for k in ALGOS))
    for label, per in rows:
        cells = []
        for k in ALGOS:
            ok, s = per[k]
            avg = s / ok if ok else 0
            cells.append(f"{ok:>3} {avg:>5.1f}")
        print(f"{label:<7} " + " ".join(f"{c:<11}" for c in cells))
    print(f"\n全池(13尺寸×200组={len(rows)*200}组):")
    for k in ALGOS:
        ok, s = total[k]
        print(f"  {k:<12} 成功 {ok:>5}/{len(rows)*200}  {100*ok/(len(rows)*200):>5.1f}%  平均空矩 {s/ok:>6.1f}" if ok else f"  {k:<12} 成功 0")