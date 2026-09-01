use rs_algos::*;
use serde_json::Value;
use std::io::Write;
use std::time::Instant;
const SIZES:[(usize,usize);13]=[(24,10),(9,7),(10,10),(8,8),(17,10),(11,14),(8,9),(14,21),(7,5),(8,6),(6,8),(5,5),(7,11)];
fn main() {
    let raw = std::fs::read_to_string("../tscripts/full_item_catalog.json").unwrap();
    let root: Value = serde_json::from_str(&raw).unwrap();
    let mut pool: Vec<Item> = Vec::new();
    for r in root["records"].as_array().unwrap() {
        let name = r["stableId"].as_str().unwrap_or("?").to_string();
        if let Some(shapes) = r["shapes"].as_array() {
            for s in shapes {
                let cells: Vec<(i32,i32)> = s["cells"].as_array().map(|a| a.iter().map(|c| { let xy=c.as_array().unwrap(); (xy[0].as_i64().unwrap() as i32, xy[1].as_i64().unwrap() as i32) }).collect()).unwrap_or_default();
                if !cells.is_empty() { pool.push(Item{name:name.clone(),cells,w:s["w"].as_i64().unwrap_or(0) as i32,h:s["h"].as_i64().unwrap_or(0) as i32}); }
            }
        }
    }
    let mut f = std::fs::File::create("bench2_log.txt").unwrap();
    let mut tot=0.0;
    for &(W,H) in &SIZES {
        let t=Instant::now();
        let _=run_pool(&pool, &[(W,H)], 20, 14, 42);
        let d=t.elapsed().as_secs_f64(); tot+=d;
        writeln!(f, "{:>5}x{:<4} 20组: {:.3}s", W, H, d).unwrap();
        f.flush().unwrap();
    }
    writeln!(f, "总计: {:.3}s (预计200组/尺寸 x{:.0})", tot, tot*10.0).unwrap();
}
