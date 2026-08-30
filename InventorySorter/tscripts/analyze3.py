# -*- coding: utf-8 -*-
"""测试: best-fit已有(最小浪费) vs 左上扫描线聚块 vs 左下角锚定留右上大块。最大化剩余连续空矩形。"""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

# 当前 Core 语义 = best-fit(最小waste), 候选左上锚定, waste相等取更左上(oy<by||==且ox<bx)
def pack_core_bestfit(items):
    items=sorted(items,key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None;bestwaste=None
        for key,nw,nh in rots:
            cells=list(key)
            for py in range(H-nh+1):
                for px in range(W-nw+1):
                    if can_place(occ,cells,px,py):
                        waste=nw*nh-len(key)
                        if bestwaste is None or waste<bestwaste or (waste==bestwaste and (py<best[1] or (py==best[1] and px<best[0]))):
                            bestwaste=waste;best=(px,py,cells)
        if best:
            occ=mark(occ,best[2],best[0],best[1])
    return occ

# 左下角锚定: 物品偏好放最左下(px最小优先, py最大优先) -- 聚成左下紧块, 留右上
def pack_left_bottom(items):
    items=sorted(items,key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None
        for key,nw,nh in rots:
            cells=list(key)
            for py in range(H-nh+1):
                for px in range(W-nw+1):
                    if can_place(occ,cells,px,py):
                        # 左下优先: 先比右下角距离? 简化: 找能放的最左下(最小px, 然后最大py)
                        if best is None or px<best[0] or (px==best[0] and py>best[1]):
                            best=(px,py,cells)
        if best:
            occ=mark(occ,best[2],best[0],best[1])
    return occ

# 左上扫描线: 最小py优先, 然后最小px(行填充)
def pack_top_left(items):
    items=sorted(items,key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None
        for key,nw,nh in rots:
            cells=list(key)
            for py in range(H-nh+1):
                for px in range(W-nw+1):
                    if can_place(occ,cells,px,py):
                        if best is None or py<best[1] or (py==best[1] and px<best[0]):
                            best=(px,py,cells)
        if best:
            occ=mark(occ,best[2],best[0],best[1])
    return occ

items=all_items()
print(f"物品={len(items)} 格子={sum(len(i['cells']) for i in items)} 背包{W}x{H} 剩余{W*H-sum(len(i['cells']) for i in items)}")
for label,fn in [("CoreBestFit",pack_core_bestfit),("LeftBottom",pack_left_bottom),("TopLeft",pack_top_left)]:
    occ=fn(items)
    a,b=largest_empty_rect_fast(occ)
    print(f"[{label:12s}] 占用={len(occ):3d} 剩余={W*H-len(occ):3d} 最大空矩={a:3d} ({b})")
