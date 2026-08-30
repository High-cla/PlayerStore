# -*- coding: utf-8 -*-
"""合并模拟: 排序前把同类(同名)物品合并成一件(unitCount累加,只占1形状), 对比剩余最大空矩.
数据: parse_dump 会话级. 算法: 各算法跑"全独立" vs "同类合并后", 取最大空矩差.
"""
import sys
sys.path.insert(0, "/d/git/invsort/tscripts")
from parse_dump import parse_dump
import verify_all as va


def merge_identical(items):
    """同类合并: 同名(ident)物品合成单件(尺寸/形状取第一件). 返回新列表."""
    merged = {}
    order = []
    for it in items:
        key = it["name"]
        if key not in merged:
            merged[key] = {"name": key, "cells": list(it["cells"]), "w": it["w"], "h": it["h"], "count": 1}
            order.append(key)
        else:
            merged[key]["count"] += 1
    return [merged[k] for k in order]


def run():
    sessions = parse_dump()
    print(f"{'组':<8} {'件':<3}->{'件':<3}   {'格':<4}->{'格':<4}   7算法合并前后最大空矩")
    for w, h, items in sessions:
        total = sum(len(i["cells"]) for i in items)
        if total > w * h or total == 0:
            continue
        merged = merge_identical(items)
        mt = sum(len(i["cells"]) for i in merged)
        if mt > w * h:
            continue
        best_ind = -1
        best_mrg = -1
        lines = []
        for name, fn in va.ALGOS.items():
            try:
                o_ind = fn(items, w, h)
                o_mrg = fn(merged, w, h)
                a_ind, _ = va.max_empty(o_ind, w, h) if o_ind else (-1, None)
                a_mrg, _ = va.max_empty(o_mrg, w, h) if o_mrg else (-1, None)
                if a_ind > best_ind:
                    best_ind = a_ind
                if a_mrg > best_mrg:
                    best_mrg = a_mrg
                if a_ind != a_mrg:
                    lines.append(f"    {name:<12} {a_ind} -> {a_mrg}")
            except Exception:
                pass
        tag = "WIN" if best_mrg > best_ind else ("=" if best_mrg == best_ind else "LOSE")
        print(f"{w}x{h}  {len(items):<3}->{len(merged):<3}  {total:<4}->{mt:<4}   BEST {best_ind} -> {best_mrg}  {tag}")
        for l in lines:
            print(l)


if __name__ == "__main__":
    run()
