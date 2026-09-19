# 网页物品生成 — 参照 ProbablyStolenItemManager(mod) 重构分析报告

日期: 2026-09-05
范围: 把 mod(D:\git\PlayerStore\mod, ProbablyStolenItemManager v0.4.3) 的完整物品生成能力复刻到网页生成器
(docs/items_browser.html + PlayerStore/ProgressMod.cs HTTP server)

---

## 1. 现状基线(已核实, 反编译+源码)

### 1.1 mod — ProbablyStolenItemManager (参照物)

MelonLoader 游戏内面板 mod, 纯 C# (ilspycmd 完整反编译, 4752 行 ItemManager.cs, 无混淆)。

能力分 5 块:
1. **目录生成**: `ItemSpawner.Spawn(itemID)` — 按 ResolvedId/Id/Aliases 逐候选尝试(比 DirectoryMaster 多一层 alias 解析)
2. **随机掉落**: `ItemSpawner.SpawnFromTable(tableID)` — 11 张命名表 (junk/access_card/all_module/makeshift_weapon/material/household/packed_food/t1module/t2module/tool/medical), 经 TableMaster 静态字段或同名 ID 回退
3. **预置变体**: `PreBuiltItemHelper.*` 19 个静态工厂 (香烟假/仿、注射器真/过期/仿、邮票、5 种模块随机、节点、Ribwich、Hand)
4. **属性编辑**: 面板内改基础字段(名称/描述/数量/价值/使用开关/精灵/形状/库存)、TagState(base/modified)、ItemFeature 列表增删
5. **删除**: 逐库存 TryExpelAndDestroy(overrideLockRemove→Expel→Destroy) + 场景物 trashcan/drain 兜底

生成放置路径(mod): `item.SetAmount(amount)` → `PlayerStore.Instance.AddDirectToWeightedTable(item,true)` → `RefreshCounterItem()` — 进**玩家侧柜台加权表**, 非主背包。

数据: `item_catalog.json` 439 项 `{id, english, chinese, aliases, source_key}` 手维护中英检索表, 无重复 id。

### 1.2 网页生成器 (现状)

- 前端: `docs/items_browser.html` (GitHub Pages) — ITEMS 数组(427 项, 从游戏 dump 提取: stableId/nameZh/nameEn/categories/itemTypes/kind/directory/addPolicy/desc) 分类浏览/搜索/收藏, 卡片「生成 ×1」→ `GET /api/spawn?itemId&count=1`
- 后端: ProgressMod.cs HttpListener (port 26880):
  - `GET /api/health`
  - `GET /api/spawn?itemId&count` → 主线程 `SpawnItem`: 排除 `_instruction` 图纸 → `DirectoryMaster.Item(stableId,true)` → `MayHaveValidInventorySlot` → `UncheckedAccept` 进 **EmporiumEntry 主背包**
  - 图纸→实物映射 InstructionToItem (6 个写死: 3d_printer→printer 等)

---

## 2. 差距表 (mod 能力 × 网页现状)

| # | mod 能力 | 网页现状 | 缺口 |
|---|---------|---------|------|
| G1 | alias/id 多候选生成 | 只 stableId 直生 | HTTP 层加 alias 解析 fallback |
| G2 | 11 张随机掉落表 | 无 | 新 API `/api/spawn-table` |
| G3 | 19 个预置变体工厂 | 无 | 新 API `/api/spawn-prebuilt` |
| G4 | 数量 1-999 (SetAmount) | count 循环直生 | spawn 内改用 SetAmount(count) 单次 |
| G5 | 属性编辑(基础/标签/特性) | 只读 detail | 需 HTTP 读写字段 + 前端表单 |
| G6 | 删除(任意库存+场景物) | 无 | 新 API `/api/delete` |
| G7 | 中英双语 UI | 中文硬编码 + 少量英文 | 可缓 (后端存双语, 前端切) |
| G8 | item_catalog.json 439 手维护目录 | ITEMS 427 dump 提取 | 目录合并/双数据源策略 |
| G9 | 图纸: mod 能生成实物? | 网页排除+提示 | 沿用 InstructionToItem 或 mod 方式 |
| G10 | 放置目标 | 主背包 | **mod 用柜台加权表 — 需决策** |

---

## 3. 完整复刻方案

### 3.1 后端 API 扩展 (ProgressMod.cs)

在现有 HttpListener 加 4 个端点, 全部走现有队列→主线程模式(防跨线程崩溃):

```
GET /api/spawn?itemId&count=1
    保留, 但内部升级:
    - 候选解析: 先试 DirectoryMaster.Item(id,true); 失败试 ItemSpawner.Spawn(id) alias 链
    - 图纸: 走 InstructionToItem 映射 (删 6 个写死, 与 mod 行为对齐待验证)
GET /api/spawn-table?table=junk&count=1
    table ∈ {junk,access_card,all_module,makeshift_weapon,material,household,
             packed_food,t1module,t2module,tool,medical}
    → ItemSpawner.SpawnFromTable(TableMaster.xTable 或同名回退) ×count
GET /api/spawn-prebuilt?kind=random_cigarette&count=1
    kind ∈ 19 个 {cigarette/injector/stamp 系, *_module 系, node, ribwich, hand}
    → 对 PreBuiltItemHelper 各工厂 (native 调, 需 IL 签名验证)
GET /api/delete?itemRef=...
    → 逐库存 Expel+Destroy 流程复刻 (EmporiumEntry invElement + 已知库存 + trashcan/drain)
```

数量统一 `SetAmount(count)` 单对象生成 (mod 同款), 替代 count 次循环。

### 3.2 数据目录策略

- **保留 ITEMS(427) 为主目录**(元数据丰富: categories/directory/addPolicy 供分类检索), **不合并进手维护 catalog**。
- **新增只读加载 mod 的 item_catalog.json** (439 中英名/别名/source_key) 做**显示名覆盖 + alias 检索**: 网页搜索匹配 `aliases` 与 `source_key` 字段, 命中用其 chinese/english 覆盖显示。
- 文件随网页托管 (`docs/item_catalog.json` 拷贝), 前端 fetch 合并。
- 随机表/变体条目在 ITEMS 侧新增 30 个合成卡 (categories 加 `_table`/`_variant`), 前端按 data-spawn-kind 分发到新端点。

### 3.3 前端 items_browser.html

1. 分类栏新增 2 组: 「随机掉落(11)」「预置变体(19)」
2. 卡片支持 3 种 spawn 类型: stableId / table / prebuilt, 统一 `×N` 数量输入 (默认 1, 上限 999)
3. fetch 改 POST JSON 或保留 GET query, 按类型拼端点
4. 详情面板扩展: 显示 mod 目录名覆盖 + source_key; 只读展示 → (G5 若做) 编辑表单
5. 双语: 后端 resp 带 zh/en 名, 前端 `?lang` 切换 (轻量版, G7)

### 3.4 图纸策略

网页现状(排除+InstructionToItem 提示) 对多数用户够用且安全。mod 无此特殊处理(目录本就不含 instruction)。**建议: 保留网页现状** — InstructionToItem 覆盖已够, 完整图纸生成需 ISIL 深挖 layout 项生成接口, 列为二期。

---

## 4. 分阶段落地

| 阶段 | 内容 | 验证 |
|------|------|------|
| P1 | API: spawn 升级(alias+SetAmount) + spawn-table + spawn-prebuilt | 实测各生成一次成功进背包, 数量对 |
| P2 | 数据: docs/item_catalog.json + 前端合并/别名搜索 + 30 合成卡 | 搜索别名命中, 随机/变体卡生成成功 |
| P3 | 前端 UI: 分类/数量/端点分发 | 手动点测全 3 类型 ×N |
| P4 | (可选) delete + 属性编辑 API | 生成后删掉不残留 |
| P5 | 双语 (可选) | lang 切换生效 |

每阶段独立可交付。P1-P3 是「完整复刻」核心(目录+随机+变体+数量)。

---

## 5. 风险 / 开放问题

1. **放置目标 (阻塞决策)**: mod→玩家柜台加权表, 现网页→主背包。
   - A. 保持主背包 (简单, 用户惯性, 推荐)
   - B. 复刻柜台 (需 PlayerStore.AddDirectToWeightedTable native 调 + 刷新)
   - C. 可选目标 (生成到主背包/柜台/垃圾桶)
2. **native 签名待验证**: PreBuiltItemHelper 19 工厂、ItemSpawner.Spawn/SpawnFromTable、TableMaster 静态表字段、PlayerStore.AddDirectToWeightedTable — ascs 壳签名已有, 真实现需 ISIL rg 核对参数/静态性。部分工厂返回随机变体, 需确认是否每帧新实例。
3. **IL2CPP 调用代价**: 每个新 native 入口都走 il2cpp 绑定, 队列+主线程模式已保证线程安全, 但新 API 需逐一加 try/catch + Warning 日志。
4. **目录数据源漂移**: dump 提取的 ITEMS 会随游戏版本过时; mod catalog 手维护同样。合成卡 30 条与 GeneratedEntries 硬编码需随版本同步(与 mod 一致的维护负担)。
5. **后端 http 无鉴权**: 局域网内任何设备可触发生成/删除 (现仅本地主机? 需确认 listener 前缀绑定 127.0.0.1 还是 0.0.0.0)。delete API 引入前必须确认绑 127.0.0.1。
