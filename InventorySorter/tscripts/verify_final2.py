# -*- coding: utf-8 -*-
"""修正 has_support 语义: 物品整体必须锚定(至少一格贴已占块或边界), 内部格由物品自身连通填充.
判据: 物品所有边界格中, 至少一个 (是边界 或 邻接occ). 同时物品每格不能与occ重叠(can_place已保证).
"""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

def calc_hull(cells): return min(x for x,y in cells),max(x for x,y in cells),min(y for x,y in cells),max(y for x,y in cells)

def anchored(occ,cells,px,py,W,H):
    """物品整块锚定: 至少一个格 贴边界 或 相邻已占格."""
    for dx,dy in cells:
        x,y=px+dx,py+dy
        if y==0 or x==0 or y==H-1 or x==W-1: return True
        if (x-1,y) in occ or (x+1,y) in occ or (x,y-1) in occ or (x,y+1) in occ: return True
    return False

def mfr(occ_set,W,H):
    grid=[[1 if (x,y) in occ_set else 0 for x in range(W)] for y in range(H)]
    rects=[];height=[0]*W
    for y in range(H):
        for x in range(W): height[x]=height[x]+1 if grid[y][x]==0 else 0
        st=[]
        for x in range(W+1):
            cur=0 if x==W else height[x]
            while st and height[st[-1]]>=cur:
                h=height[st.pop()];left=0 if not st else st[-1]+1;right=x-1
                if h>0: rects.append((left,y-h+1,right-left+1,h))
            st.append(x)
    kept=[]
    for r in rects:
        st=(r[0],r[1],r[0]+r[2],r[1]+r[3])
        if not any(o!=r and o[0]<=st[0] and o[1]<=st[1] and o[0]+o[2]>=st[2] and o[1]+o[3]>=st[3] for o in rects): kept.append(r)
    return kept

def shrink(rects,px,py,pw,ph):
    nxt=[]
    for r in rects:
        if r[0]+r[2]<=px or px+pw<=r[0] or r[1]+r[3]<=py or py+ph<=r[1]: nxt.append(r);continue
        if px>r[0]: nxt.append((r[0],r[1],px-r[0],r[3]))
        if px+pw<r[0]+r[2]: nxt.append((px+pw,r[1],r[0]+r[2]-(px+pw),r[3]))
        cx=max(r[0],px);cx2=min(r[0]+r[2],px+pw)
        if cx<cx2:
            if py>r[1]: nxt.append((cx,r[1],cx2-cx,py-r[1]))
            if py+ph<r[1]+r[3]: nxt.append((cx,py+ph,cx2-cx,r[1]+r[3]-(py+ph)))
    out=[]
    for i,r in enumerate(nxt):
        if not any(i!=j and o[0]<=r[0] and o[1]<=r[1] and o[0]+o[2]>=r[0]+r[2] and o[1]+o[3]>=r[1]+r[3] for j,o in enumerate(nxt)): out.append(r)
    return out

def place_into(occ_set,rects,cells,gw,gh):
    best=None;bestw=None
    for r in rects:
        if r[2]<gw or r[3]<gh or r[1]+r[3]<=0: continue
        px=r[0];py=max(r[1],0)
        if py+gh>r[1]+r[3]: continue
        if not can_place(occ_set,cells,px,py): continue
        if not anchored(occ_set,cells,px,py,W,H): continue
        w=r[2]*r[3]-gw*gh
        if bestw is None or py<best[1] or (py==best[1] and px<best[0]):
            bestw=w;best=(px,py)
    return best

def layout_dense():
    items=sorted(all_items(),key=lambda i:-len(i["cells"]))
    occ=set(); rects=mfr(occ,W,H); ok=True
    for it in items:
        best=None
        for key,nw,nh in rotations(it):
            bp=place_into(occ,rects,list(key),nw,nh)
            if bp and (best is None or bp[1]<best[1] or (bp[1]==best[1] and bp[0]<best[0])):
                best=(bp[0],bp[1],list(key))
        if best:
            occ=mark(occ,best[2],best[0],best[1])
            mw=max(x[0]+1 for x in best[2]);mh=max(x[1]+1 for x in best[2])
            rects=shrink(rects,best[0],best[1],mw,mh)
        else: ok=False;break
    return occ,ok

occ,ok=layout_dense()
print(f"布局成功={ok} 占用={len(occ)} 剩余={W*H-len(occ)}")
a,b=largest_empty_rect_fast(occ)
print(f"最大空矩={a} ({b})")
for y in range(H):
    print("".join("#" if (x,y) in occ else "." for x in range(W)))
