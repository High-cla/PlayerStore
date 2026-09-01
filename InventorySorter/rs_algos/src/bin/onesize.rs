// onesize: 单尺寸单次压测, argv: W H [PER], 输出结果表
use rs_algos::*;
use serde_json::Value;
fn main() {
    let a: Vec<String> = std::env::args().collect();
    if a.len() < 3 { eprintln!("usage: onesize W H [PER] [SEED]"); return; }
    let w: usize = a[1].parse().unwrap();
    let h: usize = a[2].parse().unwrap();
    let per: usize = if a.len() > 3 { a[3].parse().unwrap() } else { 200 };
    let seed: u64 = if a.len() > 4 { a[4].parse().unwrap() } else { 42 };
    let raw = std::fs::read_to_string("../tscripts/full_item_catalog.json").unwrap();
    let root: Value = serde_json::from_str(&raw).unwrap();
    let mut pool: Vec<Item> = Vec::new();
    for r in root["records"].as_array().unwrap() {
        let name = r["stableId"].as_str().unwrap_or("?").to_string();
        if let Some(shapes) = r["shapes"].as_array() { for s in shapes {
            let cells: Vec<(i32,i32)> = s["cells"].as_array().map(|a| a.iter().map(|c| { let xy=c.as_array().unwrap(); (xy[0].as_i64().unwrap() as i32, xy[1].as_i64().unwrap() as i32) }).collect()).unwrap_or_default();
            if !cells.is_empty() { pool.push(Item{name:name.clone(),cells,w:s["w"].as_i64().unwrap_or(0) as i32,h:s["h"].as_i64().unwrap_or(0) as i32}); }
        }}
    }
    let t = std::time::Instant::now();
    let rows = run_pool(&pool, &[(w, h)], per, 14, seed);
    let dt = t.elapsed().as_secs_f64();
    eprintln!("== {}x{} per={} 耗时 {:.2}s", w, h, per, dt);
    for (label, stats) in &rows {
        eprintln!("  {}", label);
        for (name, ok, avg) in stats {
            eprintln!("    {:<12} 成功 {:>4}/{:<4}  {:>5.1}%  平均空矩 {:>6.1}", name, ok, per, 100.0* *ok as f64/per as f64, *avg as f64);
        }
    }
}
