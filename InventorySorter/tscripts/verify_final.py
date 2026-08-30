# -*- coding: utf-8 -*-
"""最终Core语义验证: MFR池含fixed容器 + HasSupport + 左上优先, 逐件4朝向, 每件PlaceInto内再按最左上. 验证最大值+放得下."""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast
import sys, importlib
sys.setrecursionlimit(10000)

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

def has_support(occ,cells,px,py):
    for dx,dy in cells:
        x,y=px+dx,py+dy
        sup = y==0 or x==0 or (x-1,y) in occ or (x,y-1) in occ or (x,y+1) in occ
        if not sup: return False
    return True

def place_into(occ_set,rects,cells,gw,gh):
    best=None;bestw=None
    for r in rects:
        if r[2]<gw or r[3]<gh or r[1]+r[3]<=0: continue
        px=r[0];py=max(r[1],0)
        if py+gh>r[1]+r[3]: continue
        if not can_place(occ_set,cells,px,py): continue
        if not has_support(occ_set,cells,px,py): continue
        w=r[2]*r[3]-gw*gh
        if bestw is None or py<best[1] or (py==best[1] and px<best[0]):
            bestw=w;best=(px,py)
    return best

# 复刻 mod LayoutDense: occ标fixed(x>0表示容器盒), 先放置固定项, 再按面积降序放散件
def layout_dense():
    # 模拟: list2散件(27件), 无fixed容器场景用空fixed
    items=sorted(all_items(),key=lambda i:-len(i["cells"]))
    occ=set()
    rects=mfr(occ,W,H)
    ok=True
    for it in items:
        placed=False
        rots=rotations(it)
        # PlaceInto: 2层(minY) 每层4朝向, 选4朝向中最左上(oy<by||==且ox<bx)
        best=None
        for key,nw,nh in rots:  # 4朝向
            bp=place_into(occ,rects,list(key),nw,nh)
            if bp and (best is None or bp[1]<best[1] or (bp[1]==best[1] and bp[0]<best[0])):
                best=(bp[0],bp[1],list(key))
        if best:
            occ=mark(occ,best[2],best[0],best[1])
            rects=shrink(rects,best[0],best[1],max(x[0]+1 for x in best[2]),max(x[1]+1 for x in best[2]))
            placed=True
        if not placed: ok=False;break
    return occ,ok

occ,ok=layout_dense()
print(f"布局成功={ok} 占用={len(occ)} 剩余={W*H-len(occ)}")
a,b=largest_empty_rect_fast(occ)
print(f"最大空矩={a} ({b})")
for y in range(H):
    print("".join("#" if (x,y) in occ else "." for x in range(W)))
