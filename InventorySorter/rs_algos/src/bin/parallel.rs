use rs_algos::*;
use serde_json::Value;
use std::sync::Arc;
use std::time::{Instant, Duration};
const SIZES: [(usize,usize);13] = [(24,10),(9,7),(10,10),(8,8),(17,10),(11,14),(8,9),(14,21),(7,5),(8,6),(6,8),(5,5),(7,11)];
const PER: usize = 200;
const NMAX: usize = 14;
const NMIN: usize = 2;
const FILL: f64 = 0.6; // 目标填充率: 总占格 ≈ 容量*FILL

/// 尺寸自适应 n: 容量*FILL / 池平均面积, clamp [NMIN, NMAX]
fn n_for(cap: usize, avg_area: f64) -> usize {
    let n = (cap as f64 * FILL / avg_area).round() as usize;
    n.max(NMIN).min(NMAX)
}

fn build_pool() -> Vec<Item> {
    let raw = std::fs::read_to_string("../tscripts/full_item_catalog.json").unwrap();
    let root: Value = serde_json::from_str(&raw).unwrap();
    let mut pool = Vec::new();
    for r in root["records"].as_array().unwrap() {
        let name = r["stableId"].as_str().unwrap_or("?").to_string();
        if let Some(shapes) = r["shapes"].as_array() { for s in shapes {
            let cells: Vec<(i32,i32)> = s["cells"].as_array().map(|a| a.iter().map(|c| { let xy=c.as_array().unwrap(); (xy[0].as_i64().unwrap() as i32, xy[1].as_i64().unwrap() as i32) }).collect()).unwrap_or_default();
            if !cells.is_empty() { pool.push(Item{name:name.clone(),cells,w:s["w"].as_i64().unwrap_or(0) as i32,h:s["h"].as_i64().unwrap_or(0) as i32,count:1}); }
        }}
    }
    pool
}

fn main() {
    let pool = Arc::new(build_pool());
    let avg_area = pool.iter().map(|i| i.area()).sum::<usize>() as f64 / pool.len() as f64;
    eprintln!("池 {} 实例, avg_area={:.2}, 目标填充率 {}%", pool.len(), avg_area, FILL * 100.0);
    let mut handles = Vec::new();
    for &(W, H) in &SIZES {
        let p = pool.clone();
        handles.push(std::thread::spawn(move || {
            // 可行池: 单件至少一朝向塞得进 W×H; 剔除"特别大放不进"的物品
            let n_feasible = p.iter().filter(|it| it.orientations().iter().any(|o| o.w <= W && o.h <= H)).count();
            let dropped = p.len() - n_feasible;
            let f_avg = p.iter().filter(|it| it.orientations().iter().any(|o| o.w <= W && o.h <= H))
                .map(|i| i.area()).sum::<usize>() as f64 / n_feasible.max(1) as f64;
            let n = n_for(W * H, f_avg);
            eprintln!("[T] {}x{} 可行池 {}/{} (剔除大物 {}), avg={:.1}, 自适应 n={}", W, H, n_feasible, p.len(), dropped, f_avg, n);
            let t = Instant::now();
            let rows = run_pool(&p, &[(W, H)], PER, n, 42);
            let d = t.elapsed();
            eprintln!("[T] {}x{} done {:.2}s", W, H, d.as_secs_f64());
            (W, H, n, d, rows.into_iter().next().unwrap().1)
        }));
    }
    let mut results = Vec::new();
    for h in handles {
        if let Ok((W, H, n, d, stats)) = h.join() {
            if d > Duration::from_secs(5) {
                eprintln!("!!! 慢尺寸 {}x{}: {:.2}s", W, H, d.as_secs_f64());
            }
            results.push((W, H, n, d, stats));
        } else {
            eprintln!("!!! 线程 panic: 某尺寸");
        }
    }
    results.sort_by_key(|r| r.0 * 100 + r.1);
    // 完整结果表: 表头从数据动态推导 (算法名与 run_pool 对齐)
    eprintln!();
    eprintln!("\n===== 全尺寸压测结果 (per={}, 尺寸自适应 n, seed=42) =====", PER);
    let mut hdr = format!("{:<7}", "尺寸");
    if let Some((_, _, _, _, stats)) = results.first() {
        for (name, _, _) in stats {
            hdr += &format!(" {:<11}", name);
        }
    }
    eprintln!("{}", hdr);
    for (W, H, n, d, stats) in &results {
        let mut line = format!("{}x{:<3}", W, H);
        for (_, ok, avg) in stats {
            line += &format!(" {:>3}/{} {:>6.1}", ok, PER, *avg as f64);
        }
        eprintln!("{:<7} n={:<2} ({:.2}s)", line, n, d.as_secs_f64());
    }
    eprintln!("ALL DONE ({} 尺寸)", results.len());
}