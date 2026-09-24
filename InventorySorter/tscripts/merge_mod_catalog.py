# -*- coding: utf-8 -*-
"""merge_mod_catalog.py — 以 mod/item_catalog.json 为权威源，对齐 docs/items_data_full.js 的物品名称。

本脚本是 docs/items_data_full.js 的唯一写入者。该文件是全仓物品表的单一来源:
  · 唯一运行时消费者 = PlayerStore.csproj 的 <EmbeddedResource Include="..\docs\items_data_full.js">
    (LogicalName web.items_data_full.js), 由网页端 fetch 自取;
  · 曾经的 docs/items_data.js (merge_catalog.py 产物) 与
    InventorySorter/tscripts/xmod/items_data{,_full}.js 三份副本已删除 —— 它们无消费者且已漂移。
游戏更新导致的增删, 不再靠重跑本脚本(离线、必然滞后)同步, 而由运行时的 /api/list 对账补正:
前端拿游戏当前目录与表比对, 表有游戏无 => 标「已失效」, 游戏有表无 => 补占位条目。

权威源: mod/item_catalog.json        441 项 {id, english, chinese, aliases[], source_key}
目标:   docs/items_data_full.js      `const ITEMS = [...];` 单行紧凑 (python json.dumps 默认分隔符, 无尾换行)

规则:
  1. 名称对齐 — 按 stableId 或 alias 命中 mod 记录后覆盖:
       nameZh: 仅当 mod.chinese 含 CJK 且不含 `{n}` 模板 (模板会把参数占位符塞进 UI)
       nameEn: 仅当 mod.english 不含 CJK 且不含 `{n}` 模板
                 (mod 中 34 条 english 字段实为中文; 另有 12 条是 `(x{0} ...)` 堆叠数格式串)
  2. 补漏 — mod 有而网页无的条目, 以 mod 名称 + 占位元数据追加 (保留 aliases/source_key)
  3. 保留网页已有元数据 (directory/categories/itemTypes/atlasPath/spritePath/description...)

用法: python InventorySorter/tscripts/merge_mod_catalog.py [--apply]
      缺省 = dry-run 只报告; --apply 写入。
"""
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MOD_CAT = os.path.join(ROOT, 'mod', 'item_catalog.json')
TARGET = os.path.join(ROOT, 'docs', 'items_data_full.js')

CJK = re.compile(r'[\u3400-\u9fff\uf900-\ufaff]')
TPL = re.compile(r'\{\d')


def load_target(path):
    """读回 `const ITEMS = [...];` 形态, 返回 (items, 前缀, 后缀)。"""
    with open(path, encoding='utf-8') as f:
        raw = f.read()
    m = re.match(r'^(const ITEMS = )(\[[\s\S]*\])(;)$', raw)
    if not m:
        raise SystemExit('items_data_full.js 非 `const ITEMS = [...];` 形态')
    return json.loads(m.group(2)), m.group(1), m.group(3)


def main():
    apply = '--apply' in sys.argv

    with open(MOD_CAT, encoding='utf-8') as f:
        mod = json.load(f)
    items, prefix, suffix = load_target(TARGET)

    # alias 感知索引
    alias_to = {}
    for r in mod:
        alias_to.setdefault(r['id'], r)
        for a in r.get('aliases') or []:
            alias_to.setdefault(a, r)

    present = {it['stableId'] for it in items}
    st = dict(zh=0, en=0, zh_tpl=0, zh_nocjk=0, en_skip=0, alias=0)
    skip_tpl, skip_nocjk = [], []

    for it in items:
        r = alias_to.get(it['stableId'])
        if not r:
            continue
        if r['id'] != it['stableId']:
            st['alias'] += 1

        zh = r.get('chinese') or ''
        if zh and zh != it.get('nameZh'):
            if TPL.search(zh):
                st['zh_tpl'] += 1
                skip_tpl.append('%s -> %s' % (it['stableId'], zh))
            elif not CJK.search(zh):
                st['zh_nocjk'] += 1
                skip_nocjk.append('%s -> %s' % (it['stableId'], zh))
            else:
                if apply:
                    it['nameZh'] = zh
                st['zh'] += 1

        en = r.get('english') or ''
        if en and en != it.get('nameEn'):
            if CJK.search(en) or TPL.search(en):
                st['en_skip'] += 1
            else:
                if apply:
                    it['nameEn'] = en
                st['en'] += 1

    # 补漏
    added, alias_covered = [], []
    for r in mod:
        if r['id'] in present:
            continue
        covers = [a for a in (r.get('aliases') or []) if a != r['id'] and a in present]
        if covers:
            alias_covered.append('%s (网页以别名 %s 存在)' % (r['id'], ','.join(covers)))
            continue
        added.append({
            'kind': 'ITEM', 'stableId': r['id'], 'directory': '',
            'nameZh': r.get('chinese') or '', 'nameEn': r.get('english') or '',
            'descriptionZh': '', 'descriptionEn': '', 'atlasPath': '', 'spritePath': '',
            'categories': [], 'itemTypes': [], 'addPolicy': 'ENABLED',
            'spriteStatus': 'UNVERIFIED', 'aliases': r.get('aliases') or [],
            'source_key': r.get('source_key'), 'new_placeholder': True,
        })

    extra = [it['stableId'] for it in items if it['stableId'] not in alias_to]

    print('mod 权威表        : %d 项 (alias 索引 %d)' % (len(mod), len(alias_to)))
    print('items_data_full   : %d 项' % len(items))
    print('')
    print('nameZh 覆盖       : %d  (alias 命中 %d)' % (st['zh'], st['alias']))
    print('nameEn 覆盖       : %d' % st['en'])
    print('跳过 zh ({n}模板) : %d' % st['zh_tpl'])
    print('跳过 zh (非中文)  : %d' % st['zh_nocjk'])
    print('跳过 en (中文或模板): %d' % st['en_skip'])
    print('新增条目          : %d' % len(added))
    print('别名已覆盖        : %d' % len(alias_covered))
    print('网页多余(不在mod) : %d -> %s' % (len(extra), json.dumps(extra, ensure_ascii=False)))
    if skip_tpl:
        print('\n-- 跳过 zh 模板 --')
        for s in skip_tpl:
            print('   ' + s)
    if skip_nocjk:
        print('\n-- 跳过 zh 非中文 --')
        for s in skip_nocjk:
            print('   ' + s)
    if alias_covered:
        print('\n-- 网页以别名覆盖 --')
        for s in alias_covered:
            print('   ' + s)
    if added:
        print('\n-- 新增清单 --')
        for a in added:
            print('   %-26s | %s | %s' % (a['stableId'], a['nameZh'], a['nameEn']))

    if apply:
        merged = items + added
        with open(TARGET, 'w', encoding='utf-8') as f:
            f.write(prefix + json.dumps(merged, ensure_ascii=False) + suffix)
        print('\n[APPLY] 已写入 %s — %d 项' % (TARGET, len(merged)))
    else:
        print('\n[dry-run] 未写入。加 --apply 生效。')


if __name__ == '__main__':
    main()
