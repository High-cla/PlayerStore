# -*- coding: utf-8 -*-
"""落地压实: 物品必须'落实'——每个底部格下方是边界(Y=0)或已占格(贴块或地板). 模拟自下而上rows堆积, 剩余=上方整矩形."""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

def resting(occ,cells,px,py,W,H):
    """每格支持: 该格下方(y-1)是边界(y==0)或已占. 若物品覆盖列不同则逐列检查底部."""
    # 物品最底行 = max dy. 对每列, 物品在该列的最低格下面有支撑=边界或occ
    # 更准确: 物品每格下方(y-1)需为边界或已占 for 落地的格; 内部格由物品自支撑. 判据: 物品各列最低格下方有边界/已占
    # 实现: 收集物品各列最低的格
    below={}
    for dx,dy in cells:
        x,y=px+dx,py+dy
        if x not in below or y<below[x]: below[x]=y   # 最低=最小y? 不对, 坐标y向下增大, 最低格=最大y
    # 修正: 最低格=最大y
    below={}
    for dx,dy in cells:
        x,y=px+dx,py+dy
        if x not in below or y>below[x]: below[x]=y
    for x,y in below.items():
        # 该列最低格 y, 下方支撑 y+1
        if y+1==H: continue
        if (x,y+1) not in occ: return False
    # 首行贴顶部也可(顶部格y==0自动支撑)  -- 处理顶部
    return True

def supports_top(occ,cells,px,py,W,H):
    """顶行压实: 物品每列最高格上方(y-1)是边界(顶部)或已占."""
    above={}
    for dx,dy in cells:
        x,y=px+dx,py+dy
        if x not in above or y<above[x]: above[x]=y
    for x,y in above.items():
        if y==0: continue
        if (x,y-1) not in occ: return False
    return True

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

def place_into(occ_set,rects,cells,gw,gh,mode):
    best=None;bestw=None
    for r in rects:
        if r[2]<gw or r[3]<gh or r[1]+r[3]<=0: continue
        px=r[0];py=max(r[1],0)
        if py+gh>r[1]+r[3]: continue
        if not can_place(occ_set,cells,px,py): continue
        ok = resting(occ_set,cells,px,py,W,H) if mode=='resting' else supports_top(occ_set,cells,px,py,W,H)
        if not ok: continue
        w=r[2]*r[3]-gw*gh
        if bestw is None or py<best[1] or (py==best[1] and px<best[0]):
            bestw=w;best=(px,py)
    return best

def layout(mode):
    items=sorted(all_items(),key=lambda i:-len(i["cells"]))
    occ=set(); rects=mfr(occ,W,H); ok=True
    for it in items:
        best=None
        for key,nw,nh in rotations(it):
            bp=place_into(occ,rects,list(key),nw,nh,mode)
            if bp and (best is None or bp[1]<best[1] or (bp[1]==best[1] and bp[0]<best[0])):
                best=(bp[0],bp[1],list(key))
        if best:
            occ=mark(occ,best[2],best[0],best[1])
            mw=max(x[0]+1 for x in best[2]);mh=max(x[1]+1 for x in best[2])
            rects=shrink(rects,best[0],best[1],mw,mh)
        else: ok=False;break
    return occ,ok

for mode in ['resting','top']:
    occ,ok=layout(mode)
    if not ok: print(f"[{mode:8s}] FAIL");continue
    a,b=largest_empty_rect_fast(occ)
    print(f"[{mode:8s}] 成功 占用={len(occ)} 剩余={W*H-len(occ)} 最大空矩={a} ({b})")
    for y in range(H): print("".join("#" if (x,y) in occ else "." for x in range(W)))
    print()
