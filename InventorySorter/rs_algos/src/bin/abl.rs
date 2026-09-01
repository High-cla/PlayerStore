use rs_algos::*;
use serde_json::Value;
const SIZES: [(usize,usize);13] = [(24,10),(9,7),(10,10),(8,8),(17,10),(11,14),(8,9),(14,21),(7,5),(8,6),(6,8),(5,5),(7,11)];
fn main() {
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
    for &(W,H) in &SIZES {
        eprintln!("== {}x{} per=200", W, H);
        let t=std::time::Instant::now();
        let _ = run_pool(&pool, &[(W,H)], 200, 14, 42);
        eprintln!("   done {:.2}s", t.elapsed().as_secs_f64());
    }
}
