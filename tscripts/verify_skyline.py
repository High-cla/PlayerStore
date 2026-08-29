# -*- coding: utf-8 -*-
"""天际线(Skyline)背包: 每列从顶往下堆(colH[x]=该列已用高度). 物品放时下沉到列colH轮廓顶, 选最左/最贴紧列. 
强制从底部凝聚 -> 剩余=上方整块. 验证最大空矩."""
exec(open("analyze_shapes.py",encoding="utf-8").read().split('if __name__=="__main__":')[0])
from analyze2 import largest_empty_rect_fast

def skyline_pack(items, mode):
    # colH[x] = 列 x 已用格数(从顶部y=0往下). 物品在列x放置时, 其cells的(px+dx)列需已有 colH[col]+dy 即物品顶对齐列顶.
    # 简化: 用 occ set 做真实布局, colH 只作高度紧凑参考. 逐列枚举 x 试放, 物品贴到该列顶(px=x, py=当前该列顶部).
    cols=len(all_items()) if False else None
    occ=set(); colH=[0]*W
    for it in sorted(items,key=lambda i:-len(i["cells"])):
        best=None
        for key,nw,nh in rotations(it):
            # 物品顶对齐: 试放各列 x in 0..W-nw, 物品py = max(colH[x+dx]-dy for cells) 使物品底部? 不对
            # 用"物品每格下方都落到已放或地面"且"至少一格贴已放". -> 枚举x, py由该列colH决定: py = max over cells of (colH[px+dx] - dy)??? 
            # 标准skyline: 物品bottom=物品cells最小dy对应列, 但非矩形. 直接: 枚举x, 物品py=各列已用顶和 — 让物品贴住该x列当前顶
            pass
        # fallback: 简化用"贴地面/贴块"逐列扫描, 找最左能放
        for x in range(0,W):
            pass
    return occ

# 上面抛砖, 直接实现正确的skyline:
def skyline(items, valign='bottom'):
    occ=set(); colH=[0]*W  # colH=该列最大已用y+bottom? 用"当前该列已占用格集合的高度"近似: 存每列最高已用y
    top=[0]*W  # top[x]=列x中最深的已用y(最大y), -1=空
    for it in sorted(items,key=lambda i:-len(i["cells"])):
        best=None
        for key,nw,nh in rotations(it):
            cells=list(key)
            for ox in range(0,W-nw+1):
                # 物品贴拢: 计算若放ox列, 物品各cells的y, 使物品"卡"下方 — 物品底边贴该列块顶或地面
                # 物品每列最低格贴地贴块: py = max over each col c of (topcol - ...)? 简单法: 物品的每列最低格y要>=那列当前深度
                # 用min高度对齐: 物品bottom放置, py 使物品最低格 <= 该列当前已用? 不行, 要向上堆
                # 转化: 物品从"高度0"开始, 其占据 cells; 碰撞检测 & 每格下方(紧邻)被占或地面 → 用"落地支撑"
                # 求py: 枚举每格可能的落点, 取物品能落的最深(最大py)且无重叠
                for py0 in range(H-1, -1, -1):  # 从最深向上
                    # 全落地: 物品所有格下方(cy+1)是边界或已占?? 太严; 改为: 物品贴最深处
                    pass
                break
        # fallback: 全格扫描最左下
        for py in range(H-1,-1,-1):
            placed=False
            for px in range(0,W):
                for key,nw,nh in rotations(it):
                    if can_place(occ,list(key),px,py) and any((x,y+1) not in occ and y+1<H for x,y in [(px+dx,py+dy) for dx,dy in key])==False:
                        pass
                pass
            if placed: break
    return occ

# 走捷径: skyline=按列堆, 物品下沉到最低。用最简: 所有物品按高度从下往上, 每件放"当前最左能放下且贴地/贴块"的位置(全格扫描), 从y最大开始
def pack_sky2(items):
    occ=set()
    for it in sorted(items,key=lambda i:-len(i["cells"])):
        best=None
        # 全格从最深开始
        for py in range(H-1,-1,-1):
            for px in range(W):
                for key,nw,nh in rotations(it):
                    if can_place(occ,list(key),px,py):
                        # 贴地或贴块: 物品至少一格 下方是边界或已占
                        if any(y+1==H or (x,y+1) in occ for x,y in [(px+dx,py+dy) for dx,dy in key]):
                            best=(px,py,list(key));break
                if best: break
                for key,nw,nh in rotations(it):
                    if can_place(occ,list(key),px,py):
                        if any(y+1==H or (x,y+1) in occ for x,y in [(px+dx,py+dy) for dx,dy in key]):
                            best=(px,py,list(key));break
                if best: break
            if best: break
        if best:
            occ=mark(occ,best[2],best[0],best[1])
        else:
            return None
    return occ

items=all_items()
oc=pack_sky2(items)
if oc:
    a,b=largest_empty_rect_fast(oc)
    print(f"[Skyline 最左下贴地] 占用={len(oc)} 剩余={W*H-len(oc)} 最大空矩={a} ({b})")
    for y in range(H): print("".join("#" if (x,y) in oc else "." for x in range(W)))
else: print("FAIL")
