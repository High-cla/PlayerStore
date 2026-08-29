# -*- coding: utf-8 -*-
"""聚焦:最大化剩余连续空白矩形。测多种打包目标。"""
import sys
sys.setrecursionlimit(10000)
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
# 从 analyze_shapes 复用: ITEMS, all_items, rotations, can_place, mark, W,H

def largest_empty_rect_fast(occ):
    """直方图法最大全零矩形"""
    g=[[0]*W for _ in range(H)]
    for x in range(W):
        for y in range(H):
            g[y][x]=1 if (x,y) in occ else 0
    best=0;bestb=None
    heights=[0]*W
    for y in range(H):
        for x in range(W):
            heights[x]=heights[x]+1 if g[y][x]==0 else 0
        stack=[]
        for x in range(W+1):
            h=heights[x] if x<W else -1
            while stack and heights[stack[-1]]>h:
                hh=heights[stack.pop()]
                left=stack[-1]+1 if stack else 0
                w=x-left
                if w*hh>best:
                    best=w*hh;bestb=(left,y-hh+1,w,hh)
            stack.append(x)
    return best,bestb

def free_area(occ):
    return W*H-len(occ)

# 策略:聚角生长。物品按"贴已放边界"放置, 从(0,0)角落生长成紧实的L形团块
def pack_grow(items, corner_first=True):
    items=sorted(items,key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None
        for key,nw,nh in rots:
            cells=list(key)
            for py in range(H):
                for px in range(W):
                    if can_place(occ,cells,px,py):
                        # 评分: 接触已放边界的面积越大越好, +贴左上优先级
                        touch=0
                        for dx,dy in cells:
                            ax,ay=px+dx,py+dy
                            # 邻接已放
                            for adx,ady in((1,0),(-1,0),(0,1),(0,-1)):
                                if (ax+adx,ay+ady) in occ: touch+=1
                            # 贴背包边
                            if ax==0 or ax==W-1: touch+=1
                            if ay==0 or ay==H-1: touch+=1
                        left,top=px,py
                        if best is None or touch>best[0] or (touch==best[0] and (top<best[2] or (top==best[2] and left<best[3]))):
                            best=(touch,nw,nh,px,py,cells)
        if best:
            occ=mark(occ,best[5],best[3],best[4])
    return occ

# 策略: 先把物品按行堆积(条状横放), 每行填满再下一行; 全挤左上
def pack_rows(items):
    items=sorted(items,key=lambda i:(-i["h"],-i["w"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None
        for key,nw,nh in rots:
            cells=list(key)
            # 扫描线: 从上到下, 每行找能放的最左位置
            for py in range(H-nh+1):
                for px in range(W-nw+1):
                    if can_place(occ,cells,px,py):
                        if best is None or (py<best[2] or (py==best[2] and px<best[3])):
                            best=(0,nw,nh,px,py,cells)
        if best:
            occ=mark(occ,best[5],best[3],best[4])
    return occ

# 策略: 最大化"剩余最大矩形"贪心: 每步选摆放位置使当前最大空矩形最小(挤得更紧)
def pack_minimize_hole(items):
    items=sorted(items,key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None
        for key,nw,nh in rots:
            cells=list(key)
            for py in range(H):
                for px in range(W):
                    if can_place(occ,cells,px,py):
                        nocc=mark(occ,cells,px,py)
                        a,_=largest_empty_rect_fast(nocc)
                        if best is None or a<best[0] or (a==best[0] and py<best[1].get(0,0) if False else False):
                            if best is None or a<best[0]:
                                best=(a,nw,nh,px,py,cells)
        if best:
            occ=mark(occ,best[5],best[3],best[4])
    return occ

items=all_items()
print(f"物品={len(items)} 格子={sum(len(i['cells']) for i in items)} 背包{W}x{H} 剩余{W*H-sum(len(i['cells']) for i in items)}")
for label,fn in [("BestFit",pack_bestfit),("Shelf",pack_shelf),("GrowTouch",pack_grow),("Rows",pack_rows),("MinHole",pack_minimize_hole)]:
    occ=fn(items)
    if len(occ)!=0:
        a,bb=largest_empty_rect_fast(occ)
        print(f"[{label:10s}] 占用={len(occ):3d} 剩余={free_area(occ):3d} 最大空矩={a:3d} ({bb})")
    else:
        print(f"[{label:10s}] 失败(有物品未放)")
