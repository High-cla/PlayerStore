# -*- coding: utf-8 -*-
"""连通聚块: 物品必须贴边(上/左边界 或 已放格), 从左上角单向生长成实心块, 剩余=右下整矩形。"""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

def neighbors_touch(occ, cells, px, py, W, H):
    """位置格子需满足: 有'支撑'——上方格或左侧格是边界或已occupied(才不放浮空/留孤岛)。"""
    # 每个 cells 格: 检查其 上邻(px+dx,py+dy-1) 和 左邻(px+dx-1,py+dy) 
    # 支撑=该格处于第一行(y==0→上无)=不支持上方; 或上方/左方 occupied; 或处于顶行/左列边界
    support=True
    for dx,dy in cells:
        x,y=px+dx,py+dy
        # 该格上方需有支撑: y==0 则上方无物(靠顶部边界,允许), 否则上方格须occupied
        if y>0 and (x,y-1) not in occ:
            support=False;break
    return support

def pack_leftdown_connected(items):
    """从左上角向下/右单向连通生长: 优先最小x列, 逐列放, 支撑规则强制实心。"""
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
                        if neighbors_touch(occ,cells,px,py,W,H):
                            # 左上优先: 最小py,再最小px
                            if best is None or py<best[1] or (py==best[1] and px<best[0]):
                                best=(px,py,cells)
        if not best:
            # 无支撑位→兜底任意可放位(左上)
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

# 变体: 反向约束,左右都支撑更严(连通块必须完全单连通,不放孤岛)
items=all_items()
print(f"物品={len(items)} 格子={sum(len(i['cells']) for i in items)} 背包{W}x{H}")
for label,fn in [("LeftDownConn",pack_leftdown_connected)]:
    occ=fn(items)
    a,b=largest_empty_rect_fast(occ)
    print(f"[{label:12s}] 占用={len(occ):3d} 剩余={W*H-len(occ):3d} 最大空矩={a:3d} ({b})")
    # 打印网格
    for y in range(H):
        print("".join("#" if (x,y) in occ else "." for x in range(W)))
