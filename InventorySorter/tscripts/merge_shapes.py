# -*- coding: utf-8 -*-
"""merge_shapes.py: dump 形状 ⋈ xmod catalog 语义 → 完整物品字典(带 w/h/cells).

- dump 源: 游戏 Mods/inv_shape_dump.txt (每物品每会话形状快照, uniq id 可能多形状)
- 语义源: xmod item-catalog.json (nameZh/En, 描述, 类别, 图标)
- 输出:   InventorySorter/tscripts/full_item_catalog.json 的 records 注入 shapes 字段
          (保留已有 427 条含占位; 新增 dump 有但 catalog 缺的条目)
"""
import json, re, sys, os

DUMP = 'D:/steam/steamapps/common/Probably Stolen Playtest/Mods/inv_shape_dump.txt'
CATALOG = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'xmod', 'item-catalog.json')
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'full_item_catalog.json')

ID_RE = re.compile(r'=== ([^ |]+) \| name=[^|]* \| tag=.* ===\((\d+)x(\d+)\)')


def parse_dump_shapes():
    """id -> [{w,h,cells},...] 多形状去重"""
    out = {}
    lines = open(DUMP, encoding='utf-8').read().splitlines()
    i = 0
    while i < len(lines):
        m = ID_RE.match(lines[i].strip())
        if m:
            ident, gw, gh = m.group(1), int(m.group(2)), int(m.group(3))
            rows = []
            j = i + 1
            while j < len(lines) and len(rows) < gh and not lines[j].startswith(('===', '== inv')):
                s = lines[j].strip()
                if s and all(ch in '#.' for ch in s):
                    rows.append(s)
                j += 1
            cells = tuple((x, y) for y, row in enumerate(rows) for x, ch in enumerate(row) if ch == '#')
            if cells:
                key = (gw, gh, cells)
                out.setdefault(ident, [])
                if key not in out[ident]:
                    out[ident].append({'w': gw, 'h': gh, 'cells': list(cells)})
            i = j
            continue
        i += 1
    return out


def main():
    shapes = parse_dump_shapes()
    print(f'dump 形状: {len(shapes)} uniq id')

    cat = json.load(open(CATALOG, encoding='utf-8-sig'))
    recs = cat['records'] if isinstance(cat, dict) else cat
    by_id = {r['stableId']: r for r in recs}
    print(f'catalog: {len(recs)}')

    # 注入 shapes 到已有 records
    for r in recs:
        r['shapes'] = shapes.get(r['stableId'], [])

    # dump 有但 catalog 无 → 占位记录(仅形状)
    placeholders = 0
    for ident, s in sorted(shapes.items()):
        if ident not in by_id:
            recs.append({'kind': 'ITEM', 'stableId': ident, 'directory': '',
                         'nameZh': '', 'nameEn': ident, 'descriptionZh': '', 'descriptionEn': '',
                         'atlasPath': '', 'spritePath': '', 'categories': [], 'itemTypes': [],
                         'addPolicy': 'ENABLED', 'spriteStatus': 'DUMP_ONLY', 'shapes': s,
                         'new_placeholder': True})
            placeholders += 1

    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump({'schemaVersion': 1, 'authorityItemCount': len(recs),
                   'numericResourceCount': 2, 'visibleRecordCount': len(recs),
                   'records': recs}, f, ensure_ascii=False, indent=1)
    n_shape = sum(1 for r in recs if r.get('shapes'))
    print(f'写入 {OUT}: {len(recs)} 条 (含形状 {n_shape}, 新占位 {placeholders})')


if __name__ == '__main__':
    main()