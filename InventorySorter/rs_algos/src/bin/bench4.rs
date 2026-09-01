use rs_algos::*;
use serde_json::Value;
use std::io::Write;
use std::time::Instant;
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
    let mut f = std::fs::File::create("b4.txt").unwrap();
    for n in [1usize,5,10,20] {
        let t=Instant::now();
        let _=run_pool(&pool, &[(7usize,5usize)], n, 14, 42);
        writeln!(f, "7x5 run_pool {}组: {:.4}s", n, t.elapsed().as_secs_f64()).unwrap();
        f.flush().unwrap();
    }
    writeln!(f,"DONE").unwrap();
}
