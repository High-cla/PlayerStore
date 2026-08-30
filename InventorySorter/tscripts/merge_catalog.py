# -*- coding: utf-8 -*-
"""merge_catalog.py: 合并 /api/list 返回的全量物品 ID 进 item-catalog.json + items_data.js.

用法:
  python merge_catalog.py <api_list_response.json> [--out-dir DOCS]
  其中 api_list_response.json 为 http://localhost:26880/api/list 的原始 JSON 响应
  (形如 {"ok":true,"dirCount":27,"count":N,"ids":[...]}).

说明:
  - 已有记录(397)保留完整元数据; 新ID(游戏实际存在但旧catalog缺失)创建占位记录
    (nameEn=id, nameZh='', categories=[], directory 未知).
  - 输出: <catalog.json> (完整记录) + <items_data.js> (供网页).
"""
import json, os, sys, shutil

def load_json(p):
    with open(p, encoding='utf-8') as f:
        return json.load(f)

def write_js(items, out_js):
    """items_data.js: let ITEMS = [ {json}, ... ]  (每字段独占行, 紧凑多层)."""
    parts = ['let ITEMS = [']
    for i, it in enumerate(items):
        if i > 0:
            parts.append(',')
        parts.append(' ' + json.dumps(it, ensure_ascii=False, indent=1).replace('\n', '\n ') + ',')
    parts.append('];')
    with open(out_js, 'w', encoding='utf-8') as f:
        f.write('\n'.join(parts))

def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    api_path = sys.argv[1]
    docs_dir = sys.argv[2] if len(sys.argv) > 2 else 'docs'

    api = load_json(api_path)
    ids = api.get('ids') or []
    dir_count = api.get('dirCount', 0)
    print(f'api/list: dirCount={dir_count}, count={len(ids)}')

    # 现有 catalog (权威元数据)
    cat_path = os.path.join(docs_dir, 'item-catalog.json')
    if os.path.exists(cat_path):
        cat = load_json(cat_path)
        records = cat['records'] if 'records' in cat else cat
    else:
        # fallback: D:/tmp/item-catalog.json
        alt = 'D:/tmp/item-catalog.json'
        if os.path.exists(alt):
            cat = load_json(alt)
            records = cat['records'] if 'records' in cat else cat
        else:
            records = []
    by_id = {r['stableId']: r for r in records}
    print(f'现有 catalog: {len(records)} 记录')

    # 合并: 保留已有元数据, 新ID占位
    out = []
    for rid in ids:
        if rid in by_id:
            out.append(by_id[rid])
        else:
            out.append({
                'kind': 'ITEM', 'stableId': rid, 'directory': '', 'nameZh': '',
                'nameEn': rid, 'descriptionZh': '', 'descriptionEn': '',
                'atlasPath': '', 'spritePath': '', 'categories': [], 'itemTypes': [],
                'addPolicy': 'ENABLED', 'spriteStatus': 'RUNTIME_VERIFIED', 'new_placeholder': True,
            })
    print(f'合并后: {len(out)} 记录 (新增 {len(out) - len(records)} 占位)')

    # 写 catalog.json (records 结构)
    cat_out = {'schemaVersion': 1, 'authorityItemCount': len(out),
               'numericResourceCount': 2, 'visibleRecordCount': len(out),
               'records': out}
    out_cat = os.path.join(docs_dir, 'item-catalog.json')
    with open(out_cat, 'w', encoding='utf-8') as f:
        json.dump(cat_out, f, ensure_ascii=False, indent=2)
    print(f'写入 {out_cat}')

    # 写 items_data.js
    write_js(out, os.path.join(docs_dir, 'items_data.js'))
    print(f'写入 {os.path.join(docs_dir, "items_data.js")}')

    # 同步到 xmod 副本
    xmod_dir = os.path.join(docs_dir, '..', 'InventorySorter', 'tscripts', 'xmod')
    if os.path.isdir(xmod_dir):
        shutil.copy(out_cat, os.path.join(xmod_dir, 'item-catalog.json'))
        shutil.copy(os.path.join(docs_dir, 'items_data.js'), os.path.join(xmod_dir, 'items_data.js'))
        print(f'同步到 {xmod_dir}')

if __name__ == '__main__':
    main()
