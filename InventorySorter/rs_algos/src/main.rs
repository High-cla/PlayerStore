//! rs_algos CLI: 全形状池随机组合压测 (对齐 tscripts/verify_pool.py 语义, Rust 提速)
use rs_algos::*;
use serde_json::Value;

const CATALOG: &str = "../tscripts/full_item_catalog.json";
const SIZES: [(usize, usize); 13] = [
    (24, 10), (9, 7), (10, 10), (8, 8), (17, 10), (11, 14), (8, 9),
    (14, 21), (7, 5), (8, 6), (6, 8), (5, 5), (7, 11),
];
const PER: usize = 200; // 每尺寸组数
const N: usize = 14;    // 每组物品数

fn main() {
    let raw = std::fs::read_to_string(CATALOG).expect("read catalog");
    let root: Value = serde_json::from_str(&raw).expect("parse catalog");
    let records = root["records"].as_array().expect("records");

    // 全形状池: 每个 record 的每个 shapes 变体 → Item
    let mut pool: Vec<Item> = Vec::new();
    for r in records {
        let name = r["stableId"].as_str().unwrap_or("?").to_string();
        if let Some(shapes) = r["shapes"].as_array() {
            for s in shapes {
                let w = s["w"].as_i64().unwrap_or(0) as i32;
                let h = s["h"].as_i64().unwrap_or(0) as i32;
                let cells: Vec<(i32, i32)> = s["cells"]
                    .as_array()
                    .map(|a| {
                        a.iter()
                            .map(|c| {
                                let xy = c.as_array().unwrap();
                                (xy[0].as_i64().unwrap_or(0) as i32, xy[1].as_i64().unwrap_or(0) as i32)
                            })
                            .collect()
                    })
                    .unwrap_or_default();
                if !cells.is_empty() {
                    pool.push(Item { name: name.clone(), cells, w, h, count: 1 });
                }
            }
        }
    }
    println!("形状池: {} 实例", pool.len());

    let t0 = std::time::Instant::now();
    let rows = run_pool(&pool, &SIZES, PER, N, 42);
    println!("压测 {}({}尺寸×{}组×{}件) 耗时 {:.2}s\n", PER * SIZES.len(), SIZES.len(), PER, N, t0.elapsed().as_secs_f64());

    // 表头
    print!("{:<7}", "组");
    let names: Vec<String> = rows[0].1.iter().map(|(n, _, _)| n.clone()).collect();
    for n in &names {
        print!("{:<12}", n);
    }
    println!();

    // 总计
    let mut total: Vec<(String, usize, usize)> = names.iter().map(|n| (n.clone(), 0, 0)).collect();

    for (label, stats) in &rows {
        print!("{:<7}", label);
        for (i, (_, ok, avg)) in stats.iter().enumerate() {
            print!("{:<4} {:<7}", format!("{}", ok), format!("{:.1}", *avg as f64));
            total[i].1 += ok;
            total[i].2 += avg * ok;
        }
        println!();
    }

    println!("\n全池 ({} 组):", PER * SIZES.len());
    let all = PER * SIZES.len();
    for (name, ok, sum) in &total {
        let avg = if *ok > 0 { *sum / *ok } else { 0 };
        println!("  {:<12} 成功 {:>4}/{}  {:>5.1}%  平均空矩 {:>6.1}",
                 name, ok, all, 100.0 * *ok as f64 / all as f64, avg as f64);
    }
}