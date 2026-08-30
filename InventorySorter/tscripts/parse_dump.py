# -*- coding: utf-8 -*-
"""基于真实 dump 的测试集: 从 inv_shape_dump.txt 解析每件物品形状
单遍流式: == inv WxH 设当前组, === ident | name... | tag...(WxH) 开新品, 随后读 #/. 网格行
"""
import re

def parse_dump(path="D:/steam/steamapps/common/Probably Stolen Playtest/Mods/inv_shape_dump.txt"):
    """按会话分组: 每个 == inv WxH 行是一个独立排序会话(=一次背包快照), 返回 List[(W,H,items)]."""
    with open(path, encoding="utf-8") as f:
        lines = f.read().splitlines()
    sessions = []          # List[(W,H,[items])]
    i = 0
    cur = None
    while i < len(lines):
        line = lines[i].strip()
        m = re.match(r"== inv (\d+)x(\d+)", line)
        if m:
            cur = [int(m.group(1)), int(m.group(2)), []]
            sessions.append(cur)
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
                cur[2].append({"name": ident, "cells": cells, "w": gw, "h": gh})
            i = j
            continue
        i += 1
    return sessions

def merge_same_type(items):
    """同类合并: 相同 ident + 相同形状(cells 归一化) 的多件合并为 1 代表格(代表格 unitCount=总数),
    其余剔除. 模拟 C# 端 mergeRepIdx 行为(布局器跑之前先执行同类合并).
    返回 [(it, unit_count), ...] 代表格列表(unit_count>=1). 不可堆叠的同类独立件也合并(与 C# 无差别合并一致)."""
    groups = {}
    for it in items:
        # 形状归一化键: ident + 尺寸 + sorted cells(形状相同即归一化一致)
        key = (it["name"], it["w"], it["h"], tuple(sorted(it["cells"])))
        groups.setdefault(key, []).append(it)
    merged = []
    for key, group in groups.items():
        # 代表格取第一件, unitCount = 组内件数
        rep = {"name": key[0], "cells": key[3], "w": key[1], "h": key[2]}
        merged.append((rep, len(group)))
    return merged

def parse_dump_merged(path="D:/steam/steamapps/common/Probably Stolen Playtest/Mods/inv_shape_dump.txt"):
    """parse_dump + 每组同类合并. 返回 List[(W,H,[(rep,unit_count)])]."""
    sessions = parse_dump(path)
    out = []
    for w, h, items in sessions:
        out.append((w, h, merge_same_type(items)))
    return out
if __name__ == "__main__":
    g = parse_dump()
    for key in sorted(g):
        print(key, len(g[key]))
        for it in g[key]:
            print("  ", it["name"], map(list, it["cells"]))
