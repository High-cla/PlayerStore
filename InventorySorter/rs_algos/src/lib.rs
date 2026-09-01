//! rs_algos: 布局算法库 (Rust 移植, 网格行掩码优化)
//!
//! 对齐 Python 验证套件语义 (tscripts/verify_all.py + verify_real_dump.py):
//!   PairGrounded / GreedyBottom / BestFitMFR / PGSplit / GrowTouch / MetaBest

/// 网格: occ[row] = u64 位掩码 (W <= 64)
#[derive(Clone, Debug)]
pub struct Grid {
    pub w: usize,
    pub h: usize,
    pub occ: Vec<u64>,
}

impl Grid {
    pub fn new(w: usize, h: usize) -> Self {
        Grid { w, h, occ: vec![0; h] }
    }
    pub fn can_place(&self, px: usize, py: usize, cells: &[(i32, i32)]) -> bool {
        cells.iter().all(|&(dx, dy)| {
            let (x, y) = (px as i32 + dx, py as i32 + dy);
            x >= 0 && y >= 0 && (x as usize) < self.w && (y as usize) < self.h
                && self.occ[y as usize] & (1u64 << x) == 0
        })
    }
    pub fn mark(&mut self, px: usize, py: usize, cells: &[(i32, i32)]) {
        for &(dx, dy) in cells {
            let (x, y) = (px as i32 + dx, py as i32 + dy);
            self.occ[y as usize] |= 1u64 << x;
        }
    }
    /// 堆叠可放: 允许格压已占(叠上去), 但须 >=1 格新空格 (可见约束)。非堆叠语义用 can_place.
    pub fn can_place_stackable(&self, px: usize, py: usize, cells: &[(i32, i32)]) -> bool {
        let mut fresh = 0;
        for &(dx, dy) in cells {
            let (x, y) = (px as i32 + dx, py as i32 + dy);
            if x < 0 || y < 0 || (x as usize) >= self.w || (y as usize) >= self.h {
                return false;
            }
            if self.occ[y as usize] & (1u64 << x) == 0 {
                fresh += 1;
            }
        }
        fresh > 0
    }
    /// 堆叠标记: 只标新空格(叠层不重复占用), 返回新格数
    pub fn mark_fresh(&mut self, px: usize, py: usize, cells: &[(i32, i32)]) -> usize {
        let mut fresh = 0;
        for &(dx, dy) in cells {
            let (x, y) = (px as i32 + dx, py as i32 + dy);
            let bit = 1u64 << x;
            if self.occ[y as usize] & bit == 0 {
                self.occ[y as usize] |= bit;
                fresh += 1;
            }
        }
        fresh
    }
    pub fn filled(&self) -> usize {
        self.occ.iter().map(|r| r.count_ones() as usize).sum()
    }
    /// 重建校验: 行数正确 + 每行无超出宽度的位 (防御布局器内部越界/错位)
    pub fn validate(&self) -> bool {
        if self.occ.len() != self.h {
            return false;
        }
        let mask = if self.w >= 64 { u64::MAX } else { (1u64 << self.w) - 1 };
        self.occ.iter().all(|&r| r & !mask == 0)
    }
    /// 最大连续空矩 (直方图法, 返回面积 + (x,y,w,h))
    pub fn max_empty_rect(&self) -> (usize, Option<(usize, usize, usize, usize)>) {
        let mut best = 0usize;
        let mut bestb = None;
        let mut heights = vec![0usize; self.w];
        for y in 0..self.h {
            for x in 0..self.w {
                heights[x] = if self.occ[y] & (1u64 << x) == 0 { heights[x] + 1 } else { 0 };
            }
            let mut stack: Vec<usize> = Vec::new();
            for x in 0..=self.w {
                let h = if x < self.w { heights[x] } else { 0 };
                while let Some(&top) = stack.last() {
                    if heights[top] <= h {
                        break;
                    }
                    stack.pop();
                    let hh = heights[top];
                    let left = stack.last().map_or(0, |&l| l + 1);
                    let w = x - left;
                    if w * hh > best {
                        best = w * hh;
                        bestb = Some((left, y + 1 - hh, w, hh));
                    }
                }
                stack.push(x);
            }
        }
        (best, bestb)
    }
}

/// 物品: 基础朝向 + 4 旋转去重
#[derive(Clone, Debug)]
pub struct Item {
    pub name: String,
    pub cells: Vec<(i32, i32)>, // 归一化 (min 0,0)
    pub w: i32,
    pub h: i32,
    pub count: u32, // 堆叠数 (unitCount>1 为堆叠物, 支持压已占格叠放; 默认 1)
}

/// 旋转后朝向 (cells + 实际包围盒 w/h)
#[derive(Clone)]
pub struct Orient {
    pub cells: Vec<(i32, i32)>,
    pub w: usize,
    pub h: usize,
}

fn rotate(cells: &[(i32, i32)], times: usize, sw: i32, sh: i32) -> Vec<(i32, i32)> {
    let mut c: Vec<(i32, i32)> = cells.to_vec();
    let (mut wd, mut sd) = (sw, sh);
    for _ in 0..times {
        c = c.iter().map(|&(x, y)| (wd - 1 - y, x)).collect();
        std::mem::swap(&mut wd, &mut sd);
    }
    let mx = c.iter().map(|&(x, _)| x).min().unwrap_or(0);
    let my = c.iter().map(|&(_, y)| y).min().unwrap_or(0);
    c.iter().map(|&(x, y)| (x - mx, y - my)).collect()
}

impl Item {
    /// 4 旋转去重 → [(cells, w, h)]
    pub fn orientations(&self) -> Vec<Orient> {
        let mut out: Vec<Orient> = Vec::new();
        for i in 0..4 {
            let c = rotate(&self.cells, i, self.w, self.h);
            let nw = c.iter().map(|&(x, _)| x).max().unwrap_or(0) as usize + 1;
            let nh = c.iter().map(|&(_, y)| y).max().unwrap_or(0) as usize + 1;
            if !out.iter().any(|o| o.cells == c) {
                out.push(Orient { cells: c, w: nw, h: nh });
            }
        }
        out
    }
    pub fn area(&self) -> usize {
        self.cells.len()
    }
}

/// 落地堆积: 从底往上找首个 grounded 位置 (最低行 + 最左)
fn place_grounded<'a>(grid: &Grid, o: &'a Orient) -> Option<(usize, usize, &'a Orient)> {
    let (W, H) = (grid.w, grid.h);
    if o.w > W || o.h > H {
        return None;
    }
    let mut scan: usize = 0;
    for py in (0..=H - o.h).rev() {
        for px in 0..=W - o.w {
            scan += 1;
            if scan > 200_000 {
                return None; // 极端穷举保护: 单件放置扫描超上限, 判不可行
            }
            if !grid.can_place(px, py, &o.cells) {
                continue;
            }
            // grounded: 任一格触底或下方有占用
            let grounded = o.cells.iter().any(|&(dx, dy)| {
                let (x, y) = (px as i32 + dx, py as i32 + dy);
                y as usize == H - 1
                    || (y + 1 < H as i32 && grid.occ[(y + 1) as usize] & (1u64 << x) != 0)
            });
            if grounded {
                return Some((px, py, o));
            }
        }
    }
    None
}

/// 补形配对结果
#[derive(Clone)]
struct PairUnit {
    cells: Vec<(i32, i32)>,
    w: usize,
    h: usize,
}

impl PairUnit {
    fn area(&self) -> usize {
        self.w * self.h
    }
}

/// 补形配对: 尝试 a+b 旋转/位移互补填满矩形
fn try_complement(a: &Item, b: &Item) -> Option<PairUnit> {
    let aos = a.orientations();
    let bos = b.orientations();
    let area_a = a.area() as i32;
    let area_b = b.area() as i32;
    let mut scan: usize = 0;
    for oa in &aos {
        let (aw, ah) = (oa.w, oa.h);
        for ob in &bos {
            let (bw, bh) = (ob.w, ob.h);
            for dx in -(bw as i32 - 1)..aw as i32 {
                for dy in -(bh as i32 - 1)..ah as i32 {
                    scan += 1;
                    if scan > 200_000 {
                        return None; // 配对搜索保护
                    }
                    // 强剪枝: 组合矩形面积必须等于两形状面积和 (互补填充的必要条件)
                    let min_x = 0.min(dx);
                    let min_y = 0.min(dy);
                    let max_x = (aw as i32).max(dx + bw as i32);
                    let max_y = (ah as i32).max(dy + bh as i32);
                    let (rw, rh) = ((max_x - min_x) as i32, (max_y - min_y) as i32);
                    if rw * rh != area_a + area_b {
                        continue;
                    }
                    let mut g = vec![vec![false; rh as usize]; rw as usize];
                    let mut filled = 0usize;
                    let mut ok = true;
                    'fill: {
                        for &(cx, cy) in &oa.cells {
                            let (gx, gy) = ((cx - min_x) as usize, (cy - min_y) as usize);
                            if gx >= rw as usize || gy >= rh as usize || g[gx][gy] {
                                ok = false;
                                break 'fill;
                            }
                            g[gx][gy] = true;
                            filled += 1;
                        }
                        for &(cx, cy) in &ob.cells {
                            let (gx, gy) = ((cx + dx - min_x) as usize, (cy + dy - min_y) as usize);
                            if gx >= rw as usize || gy >= rh as usize || g[gx][gy] {
                                ok = false;
                                break 'fill;
                            }
                            g[gx][gy] = true;
                            filled += 1;
                        }
                        if filled != (rw * rh) as usize {
                            ok = false;
                        }
                    }
                    if ok {
                        let mut cells: Vec<(i32, i32)> =
                            oa.cells.iter().map(|&(cx, cy)| (cx - min_x, cy - min_y)).collect();
                        cells.extend(
                            ob.cells.iter().map(|&(cx, cy)| (cx + dx - min_x, cy + dy - min_y)),
                        );
                        return Some(PairUnit { cells, w: rw as usize, h: rh as usize });
                    }
                }
            }
        }
    }
    None
}

/// 配对单元 (或单件)
#[derive(Clone)]
pub struct Unit {
    pub cells: Vec<(i32, i32)>, // 已归一化
    pub w: usize,
    pub h: usize,
    pub area: usize,
    pub kind: UnitKind,
}

#[derive(Clone, Copy, PartialEq)]
pub enum UnitKind {
    Pair(usize, usize), // 原物品索引 i,j
    Single(usize),
}

/// build_units: 贪心配对 (同形状 +1e6 加分, 优先最大互补面积)
pub fn build_units(items: &[Item]) -> Vec<Unit> {
    let n = items.len();
    let mut used = vec![false; n];
    let mut out = Vec::new();
    for i in 0..n {
        if used[i] {
            continue;
        }
        let mut best: Option<(i64, PairUnit, usize)> = None;
        for j in (i + 1)..n {
            if used[j] {
                continue;
            }
            if let Some(u) = try_complement(&items[i], &items[j]) {
                let area = u.w * u.h;
                let mut score = area as i64;
                if items[i].cells == items[j].cells {
                    score += 1_000_000;
                }
                if best.as_ref().map_or(true, |(s, _, _)| score > *s) {
                    best = Some((score, u, j));
                }
            }
        }
        if let Some((_, u, j)) = best {
            let area = u.area();
            out.push(Unit {
                cells: u.cells,
                w: u.w,
                h: u.h,
                area,
                kind: UnitKind::Pair(i, j),
            });
            used[i] = true;
            used[j] = true;
        } else {
            let c0 = items[i].cells.clone();
            let w = items[i].w as usize;
            let h = items[i].h as usize;
            out.push(Unit {
                cells: c0,
                w,
                h,
                area: items[i].area(),
                kind: UnitKind::Single(i),
            });
            used[i] = true;
        }
    }
    out
}

/// PairGrounded: 配对 + 落地堆积
pub fn layout_dense_paired(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut units = build_units(items);
    units.sort_by_key(|u| std::cmp::Reverse(u.area));
    let mut grid = Grid::new(W, H);
    let mut min_row = H;
    for u in units {
        let cells = u.cells.clone();
        let o = Orient { cells, w: u.w, h: u.h };
        let pos = place_grounded(&grid, &o)?;
        grid.mark(pos.0, pos.1, &o.cells);
        min_row = min_row.min(pos.1);
    }
    Some(grid)
}

/// GreedyBottom: 无配对直接按面积降序落地
pub fn layout_greedy_bottom(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut idx: Vec<usize> = (0..items.len()).collect();
    idx.sort_by_key(|&i| std::cmp::Reverse(items[i].area()));
    let mut grid = Grid::new(W, H);
    for i in idx {
        let ori = items[i].orientations();
        let mut best: Option<(usize, usize, &Orient)> = None;
        for o in &ori {
            if let Some(pos) = place_grounded(&grid, o) {
                let better = best.map_or(true, |(py, px, _)| {
                    pos.1 > py || (pos.1 == py && pos.0 < px)
                });
                if better {
                    best = Some(pos);
                }
            }
        }
        let (px, py, o) = best?;
        grid.mark(px, py, &o.cells);
    }
    Some(grid)
}

/// PGSplit: PairGrounded, pair 落地失败时拆两单件重试
pub fn layout_pg_split(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut units = build_units(items);
    units.sort_by_key(|u| std::cmp::Reverse(u.area));
    let mut grid = Grid::new(W, H);
    for u in units {
        match u.kind {
            UnitKind::Pair(i, j) => {
                let o = Orient { cells: u.cells.clone(), w: u.w, h: u.h };
                if place_grounded(&grid, &o).is_some() {
                    let pos = place_grounded(&grid, &o).unwrap();
                    grid.mark(pos.0, pos.1, &o.cells);
                } else {
                    // 拆单件: 分别落地, 任一失败则整体失败
                    let ori_i = items[i].orientations();
                    let mut best_i: Option<(usize, usize, &Orient)> = None;
                    for o in &ori_i {
                        if let Some(p) = place_grounded(&grid, o) {
                            if best_i.as_ref().map_or(true, |(py, px, _)| {
                                p.1 > *py || (p.1 == *py && p.0 < *px)
                            }) {
                                best_i = Some(p);
                            }
                        }
                    }
                    let (px_i, py_i, o_i) = best_i?;
                    grid.mark(px_i, py_i, &o_i.cells);
                    let ori_j = items[j].orientations();
                    let mut best_j: Option<(usize, usize, &Orient)> = None;
                    for o in &ori_j {
                        if let Some(p) = place_grounded(&grid, o) {
                            if best_j.as_ref().map_or(true, |(py, px, _)| {
                                p.1 > *py || (p.1 == *py && p.0 < *px)
                            }) {
                                best_j = Some(p);
                            }
                        }
                    }
                    let (px_j, py_j, o_j) = best_j?;
                    grid.mark(px_j, py_j, &o_j.cells);
                }
            }
            UnitKind::Single(i) => {
                let ori = items[i].orientations();
                let mut best: Option<(usize, usize, &Orient)> = None;
                for o in &ori {
                    if let Some(p) = place_grounded(&grid, o) {
                        if best.as_ref().map_or(true, |(py, px, _)| {
                            p.1 > *py || (p.1 == *py && p.0 < *px)
                        }) {
                            best = Some(p);
                        }
                    }
                }
                let (px, py, o) = best?;
                grid.mark(px, py, &o.cells);
            }
        }
    }
    Some(grid)
}

/// BestFitMFR: 按面积降序, 选最小 waste 位置
pub fn layout_bestfit_mfr(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut idx: Vec<usize> = (0..items.len()).collect();
    idx.sort_by_key(|&i| std::cmp::Reverse(items[i].area()));
    let mut grid = Grid::new(W, H);
    for i in idx {
        let ori = items[i].orientations();
        let mut best: Option<(usize, usize, usize, usize, &Orient)> = None; // waste,py,px
        let mut scan: usize = 0;
        'outer: for o in &ori {
            for py in 0..=H - o.h {
                for px in 0..=W - o.w {
                    scan += 1;
                    if scan > 200_000 {
                        return None; // 极端穷举保护: 单件扫描超上限, 判不可行
                    }
                    if grid.can_place(px, py, &o.cells) {
                        let waste = o.w * o.h - o.cells.len();
                        let better = best.as_ref().map_or(true, |(w, pyb, pxb, _, _)| {
                            waste < *w || (waste == *w && (py < *pyb || (py == *pyb && px < *pxb)))
                        });
                        if better {
                            best = Some((waste, py, px, o.cells.len(), o));
                        }
                    }
                }
            }
        }
        let (_, py, px, _, o) = best?;
        grid.mark(px, py, &o.cells);
    }
    Some(grid)
}

/// GrowTouch: 选贴占(接触面)最多位置
pub fn layout_grow_touch(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut idx: Vec<usize> = (0..items.len()).collect();
    idx.sort_by_key(|&i| std::cmp::Reverse(items[i].area()));
    let mut grid = Grid::new(W, H);
    for i in idx {
        let ori = items[i].orientations();
        let mut best: Option<(i32, usize, usize, &Orient)> = None; // touch,py,px
        let mut scan: usize = 0;
        for o in &ori {
            for py in 0..=H - o.h {
                for px in 0..=W - o.w {
                    scan += 1;
                    if scan > 200_000 {
                        return None; // 极端穷举保护
                    }
                    if grid.can_place(px, py, &o.cells) {
                        let mut touch = 0i32;
                        for &(dx, dy) in &o.cells {
                            let (ax, ay) = (px as i32 + dx, py as i32 + dy);
                            for (adx, ady) in [(1, 0), (-1, 0), (0, 1), (0, -1)] {
                                let (nx, ny) = (ax + adx, ay + ady);
                                if nx >= 0 && ny >= 0
                                    && nx < W as i32
                                    && ny < H as i32
                                    && grid.occ[ny as usize] & (1u64 << nx) != 0
                                {
                                    touch += 1;
                                }
                            }
                            if ax == 0 || ax == W as i32 - 1 {
                                touch += 1;
                            }
                            if ay == 0 || ay == H as i32 - 1 {
                                touch += 1;
                            }
                        }
                        let better = best.as_ref().map_or(true, |(t, pyb, pxb, _)| {
                            touch > *t || (touch == *t && (py < *pyb || (py == *pyb && px < *pxb)))
                        });
                        if better {
                            best = Some((touch, py, px, o));
                        }
                    }
                }
            }
        }
        let (_, py, px, o) = best?;
        grid.mark(px, py, &o.cells);
    }
    Some(grid)
}

/// Guillotine: 自由矩形切割 + 死洞惩罚
/// 极简搬运自 C# TryGuillotine: 每次选 waste + 0.1*死洞面积 最小的 free-rect + 朝向.
/// 死洞 = 割裂产生的碎片放不下任意物品(旋转后), 面积计入评分.
fn layout_guillotine(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut grid = Grid::new(W, H);
    // 全局最小物品包围盒: 碎片能放物品(旋转后) ⟺ min(gw,gh)<=min(frw,frh) && max(gw,gh)<=max(frw,frh)
    let mut min_side = usize::MAX;
    let mut max_side = usize::MAX;
    let mut order: Vec<&Item> = items.iter().collect();
    order.sort_by_key(|i| std::cmp::Reverse(i.area()));
    for it in &order {
        let m1 = (*it).w.min((*it).h) as usize;
        let m2 = (*it).w.max((*it).h) as usize;
        min_side = min_side.min(m1);
        max_side = max_side.min(m2);
    }
    if min_side == usize::MAX { min_side = 1; max_side = 1; }
    let mut freerects: Vec<(usize, usize, usize, usize)> = vec![(0, 0, W, H)];
    let mut scan: usize = 0;
    for item in &order {
        let ori = item.orientations();
        let mut best_fi = -1i32;
        let mut best_o = 0usize;
        let mut best_score = u64::MAX;
        for (fi, &(bx, by, frw, frh)) in freerects.iter().enumerate() {
            for (oi, o) in ori.iter().enumerate() {
                scan += 1;
                if scan > 200_000 { return None; }
                let (gw, gh) = (o.w, o.h);
                if gw > frw || gh > frh { continue; }
                let waste = frw * frh - gw * gh;
                // 死洞: 下碎片(整宽) + 右碎片(高度frh) 放不下任意物品则计 dead
                let mut dead = 0u64;
                let d_below = frh - gh;
                let d_right = frw - gw;
                if d_below > 0 {
                    let (mn, mx) = (frw.min(d_below), frw.max(d_below));
                    if mn < min_side || mx < max_side { dead += (frw * d_below) as u64; }
                }
                if d_right > 0 {
                    let (mn, mx) = (d_right.min(frh), d_right.max(frh));
                    if mn < min_side || mx < max_side { dead += (d_right * frh) as u64; }
                }
                let score = (waste as u64) * 10 + dead;
                if score < best_score {
                    best_score = score;
                    best_fi = fi as i32;
                    best_o = oi;
                }
            }
        }
        if best_fi < 0 { return None; }
        let (bx, by, frw, frh) = freerects[best_fi as usize];
        let o = &ori[best_o];
        grid.mark(bx, by, &o.cells);
        // 割裂: 下碎片(整宽) + 右碎片(底部, 高度=rect高)
        let (gw, gh) = (o.w, o.h);
        freerects.remove(best_fi as usize);
        let d_below = frh - gh;
        let d_right = frw - gw;
        if d_below > 0 { freerects.push((bx, by + gh, frw, d_below)); }
        if d_right > 0 { freerects.push((bx + gw, by, d_right, frh)); }
        guard_freerect_dedup(&mut freerects);
    }
    Some(grid)
}

/// 去包含: 去除被更大矩形完全覆盖的碎片
fn guard_freerect_dedup(rects: &mut Vec<(usize, usize, usize, usize)>) {
    let mut i = rects.len();
    while i > 0 {
        i -= 1;
        let (x, y, w, h) = rects[i];
        if w == 0 || h == 0 { rects.remove(i); continue; }
        let mut covered = false;
        for j in 0..rects.len() {
            if j == i { continue; }
            let (ox, oy, ow, oh) = rects[j];
            if ox <= x && oy <= y && ox + ow >= x + w && oy + oh >= y + h { covered = true; break; }
        }
        if covered { rects.remove(i); }
    }
}

/// LeftBottom: 左下角锚定。物品偏放最左下(px 最小优先, py 最大优先), 聚左下紧块留右上整块.
/// 大背包(17x10/11x14)常胜: 左下凝聚使剩余集中右上, 好放更大物品.
fn layout_left_bottom(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut grid = Grid::new(W, H);
    let mut order: Vec<&Item> = items.iter().collect();
    order.sort_by_key(|i| std::cmp::Reverse(i.area()));
    for it in &order {
        let ori = it.orientations();
        let mut best: Option<(usize, usize, &Orient)> = None; // px, py
        let mut scan: usize = 0;
        for o in &ori {
            let (gw, gh) = (o.w, o.h);
            if gw > W || gh > H { continue; }
            for py in 0..=H - gh {
                for px in 0..=W - gw {
                    scan += 1;
                    if scan > 200_000 { return None; }
                    if !grid.can_place(px, py, &o.cells) { continue; }
                    // 左下优先: px 最小优先, 并列 py 最大(更靠底)
                    let better = best.as_ref().map_or(true, |(bx, by, _)| {
                        px < *bx || (px == *bx && py > *by)
                    });
                    if better { best = Some((px, py, o)); }
                }
            }
        }
        let (px, py, o) = best?;
        grid.mark(px, py, &o.cells);
    }
    Some(grid)
}

/// MinHoleStack: 堆叠叠放变体。堆叠物(count>1)允许压已占格(叠上去), 须 >=1 格新格(可见约束).
/// 评分 = 新格数最小(少占地面) -> 再最小化放置后最大空矩(挤出整块); 非堆叠用 can_place 全空格.
fn layout_minhole_stack(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let mut grid = Grid::new(W, H);
    let mut order: Vec<&Item> = items.iter().collect();
    order.sort_by_key(|i| std::cmp::Reverse(i.area()));
    for it in &order {
        let stackable = it.count > 1;
        let ori = it.orientations();
        let mut best: Option<(u64, usize, usize, &Orient)> = None; // score, px, py
        let mut scan: usize = 0;
        for o in &ori {
            let (gw, gh) = (o.w, o.h);
            if gw > W || gh > H { continue; }
            for py in 0..=H - gh {
                for px in 0..=W - gw {
                    scan += 1;
                    if scan > 200_000 { return None; }
                    let free = if stackable {
                        grid.can_place_stackable(px, py, &o.cells)
                    } else {
                        grid.can_place(px, py, &o.cells)
                    };
                    if !free { continue; }
                    // 评分: 克隆试点算(不污染 grid), 堆叠用新格数+放置后最大空矩, 非堆叠用放置后最大空矩
                    let score = if stackable {
                        let mut g2 = grid.clone();
                        let fresh = g2.mark_fresh(px, py, &o.cells); // 只标新格
                        let after = g2.max_empty_rect().0;
                        ((fresh as u64) << 32) | after as u64
                    } else {
                        let mut g2 = grid.clone();
                        g2.mark(px, py, &o.cells);
                        g2.max_empty_rect().0 as u64
                    };
                    if best.as_ref().map_or(true, |(bs, _, _, _)| score < *bs) {
                        best = Some((score, px, py, o));
                    }
                }
            }
        }
        let (_, px, py, o) = best?;
        if stackable {
            grid.mark_fresh(px, py, &o.cells); // 堆叠物只标新格
        } else {
            grid.mark(px, py, &o.cells);
        }
    }
    Some(grid)
}

/// MetaBest: 网格规模分流 + 多布局器择优 + 重建校验
/// - 数据驱动增删: PGSplit 是 PG 严格超集(配对+拆单件重试), 删 PG(0 独立胜出)。成员在 base_fns 增删。
/// - 网格分流: <4000 格用互补集(GB/BF/PGSplit/GT), >=4000 用落地堆积兜底(防 O(W^2H^2) 爆炸)。
/// - 择优重建校验: 候选 Grid 先 validate() 再重放重建成 occ, 越界/重叠/非法则丢弃。
pub fn layout_meta(items: &[Item], W: usize, H: usize) -> Option<Grid> {
    let grid_cells = W * H;
    // 数据驱动最优组合 (逐尺寸空矩胜者统计, 看 m03019 压实测):
    //   GrowTouch 胜 6x8~11x14 共8尺寸; Guillotine 胜 17x10/24x10(150/80 最高); PGSplit 胜 14x21(195).
    //   三者互补覆盖全部13尺寸最优. 剔: MinHoleStack(无 count>1 输入, 全尺寸拖后腿空矩最低),
    //        GB/BF/LeftBottom(从不独立胜出, 纯算力).
    let base: Vec<fn(&[Item], usize, usize) -> Option<Grid>> = if grid_cells >= 4000 {
        // 超大网格(假想边界): 落地堆积兜底, 轻量防爆炸
        vec![layout_pg_split, layout_guillotine]
    } else {
        // 正常背包: 三算法互补 (GrowTouch 中等尺寸王 + Guillotine 大仓王 + PGSplit 配对)
        vec![
            layout_pg_split,
            layout_grow_touch,
            layout_guillotine,
        ]
    };
    let mut best: Option<(usize, Grid)> = None;
    for f in base {
        if let Some(g) = f(items, W, H) {
            // 重建校验: 越界位/非法行 → 丢弃该候选
            if !g.validate() {
                continue;
            }
            let a = g.max_empty_rect().0;
            if best.as_ref().map_or(true, |(ba, _)| a > *ba) {
                best = Some((a, g));
            }
        }
    }
    best.map(|(_, g)| g)
}

/// 随机压测: 13 尺寸 × per 组 × n 件 (种子可复现)
pub fn run_pool(pool: &[Item], sizes: &[(usize, usize)], per: usize, n: usize, seed: u64) -> Vec<(String, Vec<(String, usize, usize)>)> {
    const FILL: f64 = 0.6;
    const NMIN: usize = 2;
    const NMAX: usize = 14;
    let _ = n; // n 已被可行子池 avg 覆盖 (尺寸自适应), 保留参数兼容旧调用
    let mut rng = SplitMix64::new(seed);
    // 测试侧全算法集: 尽可能多算法持续对比 (数据说话, 不预删).
    // 用户原则: 测试保留满员算法, 才能观察哪个算法在什么场景最优; 删了无法保证永远最优.
    let algo_names = ["PGSplit", "GreedyBottom", "BestFitMFR", "GrowTouch", "Guillotine", "LeftBottom", "MinHoleStack", "MetaBest"];
    let small_fns: Vec<(usize, fn(&[Item], usize, usize) -> Option<Grid>)> = vec![
        (0, layout_pg_split),
        (1, layout_greedy_bottom),
        (2, layout_bestfit_mfr),
        (3, layout_grow_touch),
        (4, layout_guillotine),
        (5, layout_left_bottom),
        (6, layout_minhole_stack),
    ];
    let big_fns: Vec<(usize, fn(&[Item], usize, usize) -> Option<Grid>)> = vec![
        (0, layout_pg_split),
        (1, layout_greedy_bottom),
        (2, layout_bestfit_mfr),
    ];
    let meta_idx = small_fns.len(); // MetaBest 索引 (成员数)
    let mut rows = Vec::new();
    for &(W, H) in sizes {
        let mut per_stats: Vec<(String, usize, usize)> = algo_names
            .iter()
            .map(|n| (n.to_string(), 0usize, 0usize))
            .collect();
        // 可行子池: 单件至少有一朝向塞得进 W×H (剔除特别大的物品, 只测"能放进去的")
        let feasible: Vec<&Item> = pool
            .iter()
            .filter(|it| it.orientations().iter().any(|o| o.w <= W && o.h <= H))
            .collect();
        if feasible.is_empty() {
            rows.push((format!("{}x{}", W, H), per_stats));
            continue;
        }
        // 可行子池平均面积 → 自适应 n (保证小箱抽小物, 总占格≈容量*FILL)
        let f_avg: f64 = feasible.iter().map(|i| i.area()).sum::<usize>() as f64 / feasible.len() as f64;
        let fn_ = (W as f64 * H as f64 * FILL / f_avg).round() as usize;
        let k = fn_.max(NMIN).min(NMAX);
        for _ in 0..per {
            let items: Vec<Item> = (0..k).map(|_| feasible[rng.next() as usize % feasible.len()].clone()).collect();
            // 前置剪枝: 物理不可行(面积超容量)才跳过; 其余全跑, 慢组由算法层 guard 处理
            let total_area: usize = items.iter().map(|i| i.area()).sum();
            if total_area > W * H {
                continue;
            }
            let mut sums: Vec<usize> = vec![0; small_fns.len()];
            let mut max_meta: Option<usize> = None;
            // 网格分流: 与 layout_meta 一致的成员集
            let bf: &[(usize, fn(&[Item], usize, usize) -> Option<Grid>)] =
                if W * H >= 4000 { &big_fns[..] } else { &small_fns[..] };
            for (algo_idx, f) in bf {
                if let Some(g) = f(&items, W, H) {
                    if !g.validate() {
                        continue; // 重建校验: 越界/非法丢弃
                    }
                    let a = g.max_empty_rect().0;
                    sums[*algo_idx] = a;
                    per_stats[*algo_idx].1 += 1;
                    per_stats[*algo_idx].2 += a;
                    max_meta = Some(max_meta.map_or(a, |m| m.max(a)));
                }
            }
            if let Some(m) = max_meta {
                per_stats[meta_idx].1 += 1;
                per_stats[meta_idx].2 += m;
            }
        }
        per_stats = per_stats
            .into_iter()
            .map(|(name, ok, sum)| (name, ok, if ok > 0 { sum / ok } else { 0 }))
            .collect();
        rows.push((format!("{}x{}", W, H), per_stats));
    }
    rows
}

/// 简单可复现 PRNG (SplitMix64)
pub struct SplitMix64 {
    state: u64,
}
impl SplitMix64 {
    pub fn new(seed: u64) -> Self {
        SplitMix64 { state: seed }
    }
    pub fn next(&mut self) -> u64 {
        self.state = self.state.wrapping_add(0x9E3779B97F4A7C15);
        let mut z = self.state;
        z = (z ^ (z >> 30)).wrapping_mul(0xBF58476D1CE4E5B9);
        z = (z ^ (z >> 27)).wrapping_mul(0x94D049BB133111EB);
        z ^ (z >> 31)
    }
}