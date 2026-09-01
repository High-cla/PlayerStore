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
    let mut f = std::fs::File::create("b5.txt").unwrap();
    // 复刻 run_pool 内部: 手写 7x5 1组 逐算法
    let mut rng = SplitMix64::new(42); // 需要 pub? 判断
    let items: Vec<Item> = (0..14).map(|_| pool[rng.next() as usize % pool.len()].clone()).collect();
    for (name, f2) in [("PG",layout_dense_paired as fn(_,_,_)->_),("GB",layout_greedy_bottom),("BF",layout_bestfit_mfr),("PS",layout_pg_split),("GT",layout_grow_touch)] {
        let t=Instant::now();
        let r=f2(&items,7,5);
        writeln!(f, "{} {:.4}s {:?}", name, t.elapsed().as_secs_f64(), r.is_some()).unwrap();
        f.flush().unwrap();
    }
    writeln!(f,"DONE").unwrap();
}
