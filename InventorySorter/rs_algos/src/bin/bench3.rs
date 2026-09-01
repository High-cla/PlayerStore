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
    let mut f = std::fs::File::create("b3.txt").unwrap();
    let mut rng = 42u64;
    // 生成固定 7x5 测试组 (14件)
    let items: Vec<Item> = (0..14).map(|_| { rng = rng.wrapping_mul(6364136223846793005).wrapping_add(1442695040888963407); pool[(rng>>33) as usize % pool.len()].clone() }).collect();
    for (name, f2) in [("PG",layout_dense_paired as fn(_,_,_)->_),("GB",layout_greedy_bottom),("BF",layout_bestfit_mfr),("PS",layout_pg_split),("GT",layout_grow_touch)] {
        let t=Instant::now();
        let mut ok=0;
        for _ in 0..1000 { if f2(&items,7,5).is_some(){ok+=1;} }
        writeln!(f, "{} 7x5 1000组: {:.3}s 成功{}", name, t.elapsed().as_secs_f64(), ok).unwrap();
        f.flush().unwrap();
    }
    writeln!(f, "DONE").unwrap();
}
