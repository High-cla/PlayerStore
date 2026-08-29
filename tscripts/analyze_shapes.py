# -*- coding: utf-8 -*-
"""分析真实dump形状，对比打包策略的剩余最大连续矩形"""
ITEMS = [
    ("ammo_hp",1,1),("ammo_hp",1,1),("ammo_hp",1,1),
    ("pink_inj",4,1),("pink_inj",4,1),("pink_inj",4,1),
    ("extractor",2,3),
    ("red_beer",1,4),("welder",1,4),
    ("chem",3,2),("chem",3,2),
    ("tazer",2,3),("tazer",2,3),("tazer",2,3),
    ("crowbar",6,1),
    ("purifier",2,2),
    ("stun_gun","4x3-L",None),
    ("cleaver","5x2-L",None),
    ("pipe_weapon","5x2-B",None),
    ("water_prem",1,4),
    ("skincare",2,2),("skincare",2,2),
    ("labeler",2,3),("labeler_blue",2,3),
    ("pill",1,1),
    ("black_inj",4,1),("purple_inj",4,1),
]
WEAP={"stun_gun":"4x3-L","cleaver":"5x2-L","pipe_weapon":"5x2-B"}
def cells_for(kind,bw,bh):
    if kind=="4x3-L": return [(x,y) for x in range(4) for y in range(3) if not (x>=2 and y==2)]
    if kind=="5x2-L": return [(x,y) for x in range(5) for y in range(2) if not (x<2 and y==1)]
    if kind=="5x2-B": return [(x,y) for x in range(5) for y in range(2) if not (x<4 and y==1)]
    return [(x,y) for x in range(bw) for y in range(bh)]
def item_cells(name):
    if name in WEAP: return cells_for(WEAP[name],0,0), True
    return None, False
def all_items():
    out=[]
    for name,bw,bh in ITEMS:
        c,special=item_cells(name)
        if not special:
            w,b=bw,bh
            c=[(x,y) for x in range(w) for y in range(b)]
        else:
            w=max(x for x,y in c)+1; b=max(y for x,y in c)+1
        out.append({"name":name,"cells":c,"w":w,"h":b})
    return out
W,H=17,10
def rot(cells,times,sw,sh):
    cur=list(cells); Wd,Sd=sw,sh
    for _ in range(times):
        cur=[(Wd-1-y,x) for x,y in cur]
        Wd,Sd=Sd,Wd
    mx=min(x for x,y in cur); my=min(y for x,y in cur)
    cur=[(x-mx,y-my) for x,y in cur]
    nw=max(x for x,y in cur)+1; nh=max(y for x,y in cur)+1
    return cur,nw,nh
def rotations(item):
    res=[]
    for i in range(4):
        c,nw,nh=rot(item["cells"],i,item["w"],item["h"])
        key=tuple(sorted(c))
        if not any(k==key for k in (r[0] for r in res)):
            res.append((key,nw,nh))
    return res
def largest_empty_rect(occ):
    grid={(x,y):((x,y) in occ) for x in range(W) for y in range(H)}
    best=0;bestb=None
    for y0 in range(H):
        for x0 in range(W):
            for x1 in range(x0,W):
                for y1 in range(y0,H):
                    if (x1-x0+1)*(y1-y0+1)<=best: continue
                    ok=all(not grid.get((x,y),True) for y in range(y0,y1+1) for x in range(x0,x1+1))
                    if ok:
                        best=(x1-x0+1)*(y1-y0+1);bestb=(x0,y0,x1-x0+1,y1-y0+1)
    return best,bestb
def can_place(occ,cells,px,py):
    return all((px+dx,py+dy) not in occ and 0<=px+dx<W and 0<=py+dy<H for dx,dy in cells)
def mark(occ,cells,px,py):
    return occ|{(px+dx,py+dy) for dx,dy in cells}
def pack_bestfit(items):
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
                        waste=nw*nh-len(key)
                        if best is None or waste<best[0]:
                            best=(waste,nw,nh,px,py,cells)
        if best:
            occ=mark(occ,best[5],best[3],best[4])
    return occ
def pack_shelf(items):
    items=sorted(items,key=lambda i:(-i["h"],-i["w"]))
    occ=set()
    for it in items:
        rots=rotations(it)
        best=None
        for key,nw,nh in rots:
            cells=list(key)
            for py in range(H-nh+1):
                for px in range(W-nw+1):
                    if can_place(occ,cells,px,py):
                        if best is None or (py<best[3] or (py==best[3] and px<best[4])):
                            best=(0,nw,nh,px,py,cells)
        if best:
            occ=mark(occ,best[5],best[3],best[4])
    return occ
if __name__=="__main__":
    items=all_items()
    print(f"物品数={len(items)} 总格子={sum(len(i['cells']) for i in items)} 背包={W}x{H}={W*H}")
    print(f"剩余应={W*H-sum(len(i['cells']) for i in items)}")
    for label,fn in [("BestFit",pack_bestfit),("Shelf",pack_shelf)]:
        occ=fn(items)
        area,bb=largest_empty_rect(occ)
        print(f"[{label}] 占用={len(occ)} 剩余={W*H-len(occ)} 最大连续空矩形={area} ({bb})")
