# -*- coding: utf-8 -*-
"""基于真实 dump 的测试集: 从 inv_shape_dump.txt 解析每件物品形状
单遍流式: == inv WxH 设当前组, === ident | name... | tag...(WxH) 开新品, 随后读 #/. 网格行
"""
import re

def parse_dump(path="D:/steam/steamapps/common/Probably Stolen Playtest/Mods/inv_shape_dump.txt"):
    with open(path, encoding="utf-8") as f:
        lines = f.read().splitlines()
    items_by_groups = {}
    i = 0
    cur = None
    while i < len(lines):
        line = lines[i].strip()
        m = re.match(r"== inv (\d+)x(\d+)", line)
        if m:
            cur = (int(m.group(1)), int(m.group(2)))
            items_by_groups.setdefault(cur, [])
            i += 1
            continue
        m = re.match(r"=== (.+) \| name=.* \| tag=.* ===\((\d+)x(\d+)\)", line)
        if m and cur:
            ident, gw, gh = m.group(1), int(m.group(2)), int(m.group(3))
            rows = []
            j = i + 1
            while j < len(lines) and len(rows) < gh and not lines[j].startswith("===") and not lines[j].startswith("== inv"):
                s = lines[j].strip()
                if s and all(ch in "#." for ch in s):
                    rows.append(s)
                j += 1
            cells = []
            for y, row in enumerate(rows):
                for x, ch in enumerate(row):
                    if ch == "#":
                        cells.append((x, y))
            if cells:
                items_by_groups[cur].append({"name": ident, "cells": cells, "w": gw, "h": gh})
            i = j
            continue
        i += 1
    return items_by_groups

if __name__ == "__main__":
    g = parse_dump()
    for key in sorted(g):
        print(key, len(g[key]))
        for it in g[key]:
            print("  ", it["name"], map(list, it["cells"]))
