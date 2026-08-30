# -*- coding: utf-8 -*-
"""去重 inv_shape_dump.txt 里的重复快照段.
快照边界: '== inv WxH ==' 行开始, 到下一个 '== inv' 前(或文件尾)结束.
重复判定: 按解析后物品形状集合(WxH + name+cells+w/h 排序后)做语义指纹 —
同一物品集因形状块顺序不同/排序结果略差 产生的快照视为重复, 保留首次出现的完整文本段.
头部 header 的时间戳/occ/free 是布局结果值, 不作为指纹.
"""
import sys, os, re, hashlib
from collections import defaultdict

def split_snaps(raw):
    """按 == inv 分块, 返回 [(header, body)]"""
    parts = re.split(r"(?m)^(== inv \d+x\d+ ==)", raw)
    snaps = []
    idx = 1
    if len(parts) < 3:
        return snaps
    while idx + 1 < len(parts):
        header = parts[idx]
        body = parts[idx + 1]
        if re.match(r"== inv \d+x\d+", header):
            snaps.append((header, body))
        idx += 2
    return snaps

def parse_body(w, h, body):
    """解析 body(形状块) -> 排序后的物品形状签名集合. header 已存 w/h."""
    items = []
    # 逐块: ^=== id | name=.. | tag=.. ===(Gw x Gh) 后跟 gh 行网格
    blocks = re.finditer(r"=== (.+?) \| name=(.*?) \| tag=(.*?) ===\((\d+)x(\d+)\)", body)
    for blk in blocks:
        ident, name, tag, gw, gh = blk.group(1), blk.group(2), blk.group(3), int(blk.group(4)), int(blk.group(5))
        # 取该块后的 gh 行 (跳过已消耗的)
        start = blk.end()
        lines = body[start:].split("\n")
        rows = [ln.strip() for ln in lines[:gh] if ln.strip() and all(c in "#.\uFEFF" for c in ln.strip())]
        cells = tuple((x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == "#")
        if cells:
            items.append((ident, cells, gw, gh))
    items.sort(key=lambda x: (x[0], x[1], x[2], x[3]))
    return tuple(items)

def main():
    if len(sys.argv) < 2:
        print("usage: dedup_dump.py <dump.txt> [out.txt]")
        return
    src = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else src + ".dedup"
    with open(src, encoding="utf-8-sig") as f:
        raw = f.read()
    snaps = split_snaps(raw)
    print(f"快照段总数: {len(snaps)}")
    seen = {}
    kept = []
    dup = 0
    order = []
    for header, body in snaps:
        m = re.match(r"== inv (\d+)x(\d+)", header)
        if not m:
            continue
        w, h2 = int(m.group(1)), int(m.group(2))
        fp = (w, h2, parse_body(w, h2, body))
        sig = hashlib.md5(repr(fp).encode("utf-8")).hexdigest()
        if sig in seen:
            dup += 1
            continue
        seen[sig] = (header, body)
        kept.append((header, body))
    print(f"语义去重: 保留={len(kept)} 移除重复={dup}")
    with open(out, "w", encoding="utf-8") as f:
        for header, body in kept:
            f.write(header.rstrip("\n") + "\n" + body + "\n")
    print(f"写出: {out}")

if __name__ == "__main__":
    main()
