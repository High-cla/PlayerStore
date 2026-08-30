# -*- coding: utf-8 -*-
"""精确复刻 mod 架构(MFR+ShrinkRects+PlaceInto+FindFreeSpotCells), 只改位置规则, 对比最大剩余连续空矩。"""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

# --- 复刻 mod 的 MFR + ShrinkRects (直方图单调栈) ---
def mfr(occ_set, W, H):
    import collections
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
    # 去包含
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

def place_into(occ_set, W,H, cells, gw,gh, rule):
    """返回 (px,py) 最优位置, rule∈{'best','topleft','connected'}"""
    rects=mfr(occ_set,W,H)
    best=None;bestwaste=None
    for r in rects:
        if r[2]<gw or r[3]<gh or r[1]+r[3]<=0: continue
        px_=r[0];py_=max(r[1],0)
        if py_+gh>r[1]+r[3]: continue
        if not can_place(occ_set,cells,px_,py_): continue
        waste=(r[2]*r[3]-gw*gh)
        if rule=='connected':
            # 连通支撑: 每个格上邻或左邻 被占/边界
            ok=True
            for dx,dy in cells:
                x,y=px_+dx,py_+dy
                touch=False
                if y>0 and (x,y-1) in occ_set: touch=True
                if x>0 and (x-1,y) in occ_set: touch=True
                if y==0 or x==0: touch=True  # 靠边也算支撑
                # 若格子在物品内部(某格上方也是该物品格且相邻), 也连通 -- 简化: 格子本身已随cells一起放
                if not touch: ok=False;break
            if not ok: continue
            # 左上评分
            if best is None or py_<best[1] or (py_==best[1] and px_<best[0]):
                best=(px_,py_)
        elif rule=='best':
            if bestwaste is None or waste<bestwaste or (waste==bestwaste and (py_<best[1] or (py_==best[1] and px_<best[0]))):
                bestwaste=waste;best=(px_,py_)
        else: # topleft
            if best is None or py_<best[1] or (py_==best[1] and px_<best[0]):
                best=(px_,py_)
    return best

def pack_mod(rule):
    items=sorted(all_items(),key=lambda i:-len(i["cells"]))
    occ=set()
    for it in items:
        placed=False
        rots=rotations(it)
        # 四种朝向选最优(按rule)  -- 简化: 取第一个能放的朝向(最小面积浪费), 位置按rule
        for key,nw,nh in rots:
            cells=list(key)
            bp=place_into(occ,W,H,cells,nw,nh,rule)
            if bp:
                occ=mark(occ,cells,bp[0],bp[1]);placed=True;break
        if not placed:
            return None
    return occ

for rule in ['best','topleft','connected']:
    occ=pack_mod(rule)
    if occ is None:
        print(f"[{rule:10s}] FAIL 放不下");continue
    a,b=largest_empty_rect_fast(occ)
    print(f"[{rule:11s}] 占用={len(occ):3d} 剩余={W*H-len(occ):3d} 最大空矩={a:3d} ({b})")
