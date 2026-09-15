# -*- coding: utf-8 -*-
"""FillRefine 缺陷离线探测/修复对拍.
C# TryFillRefine 采纳规则: 释放自身格 -> 最小可容纳空矩内贴邻最多位 ->
    newArea >= originalArea 才采纳(否则回退). 空矩优先 -> 会把已贴簇小件拆散进孤立袋.
FIX: 采纳额外要求 touch_new >= touch_cur(不许拆散). 对比两版 de-cluster 数 + 空矩.
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from parse_dump import parse_dump_merged
from verify_all import can_place, mark, max_empty
import bench_online

BAK = "inv_shape_dump.bak_20260830_231609"

def find_free_rects(occ, W, H):
    rects=[]; height=[0]*W
    for y in range(H):
        for x in range(W):
            height[x]=height[x]+1 if (x,y) not in occ else 0
        stack=[]
        for x in range(W+1):
            cur=0 if x==W else height[x]
            while stack and height[stack[-1]]>=cur:
                h=height[stack.pop()]; left=0 if not stack else stack[-1]+1
                if h>0: rects.append((left,y-h+1,x-left,h))
            stack.append(x)
    kept=[]
    for i,r in enumerate(rects):
        rx,ry,rw,rh=r; cov=False
        for j,o in enumerate(rects):
            if i==j: continue
            ox,oy,ow,oh=o
            if ox<=rx and oy<=ry and ox+ow>=rx+rw and oy+oh>=ry+rh: cov=True; break
        if not cov: kept.append(r)
    return kept

def touch(occ,W,H,cells,px,py):
    t=0
    for dx,dy in cells:
        cx,cy=px+dx,py+dy
        if cx==0 or cx==W-1: t+=1
        if cy==0 or cy==H-1: t+=1
        if cx>0 and (cx-1,cy) in occ: t+=1
        if cx<W-1 and (cx+1,cy) in occ: t+=1
        if cy>0 and (cx,cy-1) in occ: t+=1
        if cy<H-1 and (cx,cy+1) in occ: t+=1
    return t

def base_layout(items,W,H):
    order=sorted(range(len(items)),key=lambda i:-len(items[i]['rot'][0][0]))
    occ=set(); placed=[]
    for idx in order:
        it=items[idx]; best=None
        for o,(cells,nw,nh) in enumerate(it['rot']):
            for py in range(H-nh+1):
                for px in range(W-nw+1):
                    if can_place(occ,cells,px,py,W,H): best=(px,py,o); break
                if best: break
            if best: break
        if not best: return None
        px,py,o=best; occ=mark(occ,it['rot'][o][0],px,py); placed.append((idx,px,py,o))
    return placed,occ

def refine(items,base,W,H,guard):
    if W*H>5000: return base,0
    placed=list(base); occ=set(); pos={}
    for idx,px,py,o in placed:
        occ=mark(occ,items[idx]['rot'][o][0],px,py); pos[idx]=(px,py,o)
    oa,_=max_empty(occ,W,H)
    order=sorted(range(len(placed)),key=lambda i:len(items[i]['rot'][0][0]))
    moved_down=0
    for idx in order:
        px,py,o=pos[idx]; cc=items[idx]['rot'][o][0]
        occ=occ-{(px+dx,py+dy) for dx,dy in cc}
        ct=touch(occ,W,H,cc,px,py)
        cand=None
        for (rx,ry,rw,rh) in sorted(find_free_rects(occ,W,H),key=lambda r:r[2]*r[3]):
            bT=-1; bP=None
            for o2,(cells,nw,nh) in enumerate(items[idx]['rot']):
                if nw>rw or nh>rh: continue
                for py2 in range(ry,ry+rh-nh+1):
                    for px2 in range(rx,rx+rw-nw+1):
                        if not can_place(occ,cells,px2,py2,W,H): continue
                        t=touch(occ,W,H,cells,px2,py2)
                        if bP is None or t>bT or (t==bT and (py2<bP[1] or (py2==bP[1] and px2<bP[2]))):
                            bT=t; bP=(px2,py2,o2)
            if bP is not None: cand=(bP[0],bP[1],bP[2],bT); break
        if cand is None or (cand[0]==px and cand[1]==py and cand[2]==o):
            occ=mark(occ,cc,px,py); continue
        cxc=items[idx]['rot'][cand[2]][0]
        occ=mark(occ,cxc,cand[0],cand[1]); na,_=max_empty(occ,W,H)
        ok = na>=oa
        if guard and cand[3] < ct-1e-9: ok=False
        if ok:
            pos[idx]=(cand[0],cand[1],cand[2])
            if cand[3]<ct-1e-9: moved_down+=1
        else:
            occ=occ-{(cand[0]+dx,cand[1]+dy) for dx,dy in cxc}
            occ=mark(occ,cc,px,py)
    out=[(i,)+pos[i] for i in range(len(placed))]
    return out,moved_down

def run(guard):
    s=parse_dump_merged(BAK)
    n=0; tot_dd=0; sess_dd=0; sum_empty=0
    for (W,H,grp) in s:
        items=[{'rot':bench_online.rotations(rep['cells'],rep['w'],rep['h'])} for rep,_ in grp]
        if len(items)<=1: continue
        bl=base_layout(items,W,H)
        if bl is None: continue
        placed,_=bl; n+=1
        occ=set()
        for idx,px,py,o in placed: occ=mark(occ,items[idx]['rot'][o][0],px,py)
        a0,_=max_empty(occ,W,H)
        fr,dd=refine(items,placed,W,H,guard)
        ocf=set()
        for idx,px,py,o in fr: ocf=mark(ocf,items[idx]['rot'][o][0],px,py)
        af,_=max_empty(ocf,W,H)
        sum_empty+=af
        if dd: sess_dd+=1; tot_dd+=dd
    return n,sess_dd,tot_dd,sum_empty

import bench_online
print("[guard OFF] sessions,n_dd,tot_dd,sum_empty =", run(False))
print("[guard ON ] sessions,n_dd,tot_dd,sum_empty =", run(True))
