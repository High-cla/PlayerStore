# -*- coding: utf-8 -*-
"""mod架构(MFR+Shrink) + 支撑约束(四角或边支撑), 每件试所有朝向, 选连通位。最大化剩余连续空矩。"""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

def mfr(occ_set, W, H):
    grid=[[1 if (x,y) in occ_set else 0 for x in range(W)] for y in range(H)]
    rects=[]; height=[0]*W
    for y in range(H):
        for x in range(W):
            height[x]=height[x]+1 if grid[y][x]==0 else 0
        stack=[]
        for x in range(W+1):
            cur=0 if x==W else height[x]
            while stack and height[stack[-1]]>=cur:
                h=height[stack.pop()]
                left=0 if not stack else stack[-1]+1
                right=x-1
                if h>0: rects.append((left,y-h+1,right-left+1,h))
            stack.append(x)
    kept=[]
    for r in rects:
        st=(r[0],r[1],r[0]+r[2],r[1]+r[3])
        cov=False
        for o in rects:
            so=(o[0],o[1],o[0]+o[2],o[1]+o[3])
            if o!=r and so[0]<=st[0] and so[1]<=st[1] and so[2]>=st[2] and so[3]>=st[3]:
                cov=True;break
        if not cov: kept.append(r)
    return kept

def shrink(rects, px,py,pw,ph):
    next=[]
    for r in rects:
        if r[0]+r[2]<=px or px+pw<=r[0] or r[1]+r[3]<=py or py+ph<=r[1]:
            next.append(r);continue
        if px>r[0]: next.append((r[0],r[1],px-r[0],r[3]))
        if px+pw<r[0]+r[2]: next.append((px+pw,r[1],r[0]+r[2]-(px+pw),r[3]))
        cx=max(r[0],px);cx2=min(r[0]+r[2],px+pw)
        if cx<cx2:
            if py>r[1]: next.append((cx,r[1],cx2-cx,py-r[1]))
            if py+ph<r[1]+r[3]: next.append((cx,py+ph,cx2-cx,r[1]+r[3]-(py+ph)))
    out=[]
    for i,r in enumerate(next):
        cov=False
        for j,o in enumerate(next):
            if i==j:continue
            if o[0]<=r[0] and o[1]<=r[1] and o[0]+o[2]>=r[0]+r[2] and o[1]+o[3]>=r[1]+r[3]:
                cov=True;break
        if not cov: out.append(r)
    return out

def has_support(occ_set, cells, px, py):
    for dx,dy in cells:
        x,y=px+dx,py+dy
        sup = (y==0) or (x==0) or ((x-1,y) in occ_set) or ((x,y-1) in occ_set) or ((x,y+1) in occ_set)
        if not sup: return False
    return True

def find_place(occ_set, cells, gw, gh, use_support):
    rects=mfr(occ_set,W,H)
    best=None
    for r in rects:
        if r[2]<gw or r[3]<gh: continue
        px=r[0];py=r[1]
        if py<0: continue
        if py+gh>r[1]+r[3]: continue
        if not can_place(occ_set,cells,px,py): continue
        if use_support and not has_support(occ_set,cells,px,py): continue
        if best is None or py<best[1] or (py==best[1] and px<best[0]):
            best=(px,py)
    return best

def pack(use_support):
    items=sorted(all_items(),key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        placed=False
        for key,nw,nh in rotations(it):
            bp=find_place(occ,list(key),nw,nh,use_support)
            if bp:
                occ=mark(occ,list(key),bp[0],bp[1]);placed=True;break
        if not placed:
            for key,nw,nh in rotations(it):
                bp=find_place(occ,list(key),nw,nh,False)
                if bp:
                    occ=mark(occ,list(key),bp[0],bp[1]);placed=True;break
        if not placed: return None
    return occ

for sup in [False,True]:
    occ=pack(sup)
    if occ is None: print("FAIL");continue
    a,b=largest_empty_rect_fast(occ)
    print(f"[{'supp' if sup else 'free':5s}] 占用={len(occ):3d} 剩余={W*H-len(occ):3d} 最大空矩={a:3d} ({b})")
    for y in range(H):
        print("".join("#" if (x,y) in occ else "." for x in range(W)))
    print()
