# PlayerStore 会话交接 — 2026-09-05

> ⚠️ **本文档为历史快照（2026-09-05），部分内容已过期，勿直接照抄。**
> - 游戏路径：文中的 `Probably Stolen Playtest` 已改为 `Probably Stolen Demo`
>   （现行路径与工具链见 `docs/TOOLING_PITFALLS.md` §9）。
> - 物品数据：文中 `items_data_full.js` 记 427 项，现为 451 项且已改为**运行时对账**模式
>   （见 README「网页物品数据源」）。
> - 仓库版本：文中 v1.12.1，现已至 v0.6.x 发布线。
> 保留原文以存史。

> 本文档记录本会话全部已完成工作、当前状态、关键文件与路径、遗留待办、踩坑教训。
> 目的：让新会话无需翻旧对话即可无缝续接。数据文件庞大处只给路径与方法，不复制内容。

---

## 0. 仓库总览

**单仓库** `https://github.com/High-cla/PlayerStore`（git remote origin，master 分支），目录 `D:\git\PlayerStore`。
- **ProgressMod**（`PlayerStore/ProgressMod.cs` + `PlayerStore/ProgressMod.csproj`）：Probably Stolen 游戏（Questing Goose Studio / MelonLoader 7 / IL2CPP）mod。v1.12.1。内嵌 HTTP 服务器 `http://localhost:26880/`，供网页物品浏览器生成/枚举/编辑/删除库存物品。**csproj OutputPath 直指游戏 Mods 目录 `D:\steam\steamapps\common\Probably Stolen Playtest\Mods\`（AppendTargetFrameworkToOutputPath=false，无 bin/）→ `dotnet build PlayerStore/ProgressMod.csproj` 即自动部署到游戏。**
- **InventorySorter**（`InventorySorter/`）：背包一键整理 mod（本会话未动）。
- **docs/**：GitHub Pages 源（Pages 配置 = master 分支 /docs 目录，查证命令见 §5）。`items_browser.html` = 网页物品浏览器主文件（全部前端逻辑内联 script），`items_data_full.js`（427 物品数据，`const ITEMS=[...]`，含 NEW_ITEMS），`tag_zh.js`（314 键中文 tag 映射，`const TAG_ZH={...}`）。

游戏进程路径：`D:\steam\steamapps\common\Probably Stolen Playtest\`。MelonLoader 日志：该游戏目录下 `MelonLoader\Latest.log`。dump 反编译目录：仓库 `dump/`。

---

## 1. git 历史（当前 HEAD = 69eabf6，全部已 push 到 origin/master）

```
69eabf6 feat(progressmod): translate TYPE-STRING_ item-type tags in inspector, drop spawn button ×1 and cooldown for continuous spawn   [本会话最新]
f6cae87 feat(progressmod): fix AccessViolation on item dump, localize NEW_ITEMS, add 314-key tag Chinese map
a0c4e7f feat(progressmod): full item inspector over HTTP — inventory enumeration + per-item dump (base/tags/features), edit any in-stock item by uid
a147fb6 feat(progressmod): item edit/delete over HTTP — token-tracked spawned items, mine/edit/delete API
91844a7 feat(progressmod): port ProbablyStolenItemManager spawn engine to web — counter weighted-table placement, table:/prebuilt: generation, web synthetic cards
51e7a3a feat(progressmod): rewrite output multiplier with Type[] disambiguation, drop broken infinite scavenging
a44d838 fix(progressmod): drop ModuleEffectHelper.Degrade patch for 0.46D
a8bdfee refactor(mods): fix anti-patterns
（更早略）
```

`git status` 当前：干净，除 untracked（故意不提交）：`.codebase-memory/` `.codegraph/` `dump/` `mod/` `docs/ITEM_GEN_REFACTOR_REPORT.md`。

---

## 2. 后端 HTTP API（ProgressMod.cs HandleRequest，118-267 行起）

线程模型（**关键设计约束**）：HTTP listener 线程只负责解析+入队+轮询，**凡涉及 IL2CPP native 调用（含对象字段读取也可能崩）必须在 Unity 主线程执行**。所有操作走 ConcurrentQueue + OnUpdate() 主线程消费。

| 端点 | 参数 | 行为 | 返回 |
|---|---|---|---|
| `/api/health` | — | 连通性 | `{ok:true}` |
| `/api/spawn` | `itemId`, `count`(默认1) | PendingSpawns.Enqueue((token,id,n)) | `{ok,token,queued}` |
| `/api/mine` | — | 列出 SpawnedItems(token 跟踪, 本次会话生成) | `{ok,items:[{token,uid,id,name,count,unitValue,shortDescription}]}` |
| `/api/inventory` | — | 枚举玩家全部库存(主背包+柜台+文档+垃圾桶, 按 uniqueId 去重) | `{ok,items:[{uid,id,name,count,unitValue,inv}]}` |
| `/api/item` | `uid` | **须主线程**: PendingItemDumps.Enqueue((uid,seq))→轮询 DumpResults≤5000ms | `{ok,item:dump}` 或 err "dump timeout (主线程未响应, 是否在存档?)" / "item not found" |
| `/api/edit` | `uid`,`field`,`value` | PendingItemOps.Enqueue(Edit) | `{ok,queued}` |
| `/api/delete` | `uid` | PendingItemOps.Enqueue(Delete) | `{ok,queued}` |

**DumpItem 返回字段**（供前端 renderInspector 使用，均读自 item 对象）：
顶层：`uid,id,name,unitCount,unitValue,unitBaseValue,shortDescription,longDescription,flavorText,customText,bonusAccuracy,spritePath,spriteAtlasPath`
tags：**`tags`(顶层,=base 标签/ItemState), `tagsModified`(顶层)**，每项 `{key,label,enabled,valueInt,valueFloat,valueString}`；同时有 `state:{tags}` / `modifiedState:{tags}`（前端渲染用顶层 tags/tagsModified，state 内是同一数据的另一份镜像）
features：`features`，每项含 fakeCondition/realCondition 的 category。

前端字段读取位置（**易错**）：`renderTags()` 读 `INS.tags`/`INS.tagsModified`（非 state.tags）。本地化表 `tag_zh.js` 定义 `TAG_ZH`（314 键，从 InventorySorter/tscripts/all_strings_v2.json 提取 `_TAG` 键）。

---

## 3. 前端 items_browser.html 关键结构

- **数据源加载**：`<script src="items_data_full.js">`（ITEMS 427 项）→ `<script src="tag_zh.js">`（TAG_ZH）→ 内联 script（主逻辑，~30KB）。物品数据每项含 kind/stableId/nameZh/nameEn/descriptionZh/descriptionEn/categories/itemTypes/directory/addPolicy/spriteStatus。
- **本地化表**（内联）：`CAT_ZH`（分类，键如 FOOD_DRINK、NEW_ITEMS）、`TYPE_ZH`（物品类型，50 键全大写如 HOUSEHOLD_GOOD:'日用品' WATER_PURIFICATION_SUPPLY:'净水用品' LUXURY_ITEM:'奢侈品'，`zType(t)=TYPE_ZH[t]||t`）。NEW_ITEMS 的 30 项中文名经 EXTRA_NAMES 回填 nameZh。
- **spawn 按钮**：卡片按钮文案「生成」（无 ×1、无冷却可连点），`spawnItem()` 请求中显示「生成中...」→ 成功 toast「已生成: id」，按钮复位「生成」，不禁用。
- **完整检查器**（`openInspector(uid)`→`renderInspector(item)`）：三个 pane——base(tab p=base, 字段 fieldHTML)/tags(tab p=tags, renderTags)/features(p=feats, renderFeats)。「我的生成」列表 /api/mine → .my-item 行「✎ 完整检查器」「🗑 删除」；「全部库存」/api/inventory → .inv-row。
- **tag 渲染逻辑（本会话新增，v69eabf6 上线）**：
  - `zhTag(t)`：key 或 label 剥 `TYPE-STRING_` 前缀后查 TYPE_ZH（物品类型中文）→ 再查 TAG_ZH → 兜底 `l||t.key`。解决了真机上 `TYPE-STRING_HOUSEHOLD_GOOD`→日用品 之类类型标签不翻译问题。
  - `isMirrorLabel(t)`：label 形如 `TYPE-STRING_*` 且剥前缀可映射 → 视为镜像，`.v` 不再重复显示 label（解决 `耐久 · TYPE-STRING_DURABILITY_TAG · int=400` 冗余）。
  - tagHTML 现 .k = zhTag(t)，.v = (label 且非镜像 ? label+' · ' : '') + `int=.. · str=..`。
- **测试方法**：先前用无头 msedge + playwright-core 单脚本 + 本地 mock server 验证渲染（见 §7 经验）。

---

## 4. 数据与本地化源文件

- `docs/items_data_full.js`：427 项 `const ITEMS`（从游戏 dump + item_catalog 生成，含 NEW_ITEMS 30 项 nameZh 空，由前端 EXTRA_NAMES 映射表回填）。`items_data.js` 是旧版（231KB，未用）。
- `mod/item_catalog.json`：439 项手维护 `{id,english,chinese,aliases,source_key}` 权威中英表（NEW_ITEMS 译名权威源）。
- `InventorySorter/tscripts/all_strings_v2.json`：23009 字符串数组，游戏全部本地化字符串（tag 键提取源）。
- `docs/ITEM_GEN_REFACTOR_REPORT.md`：早期物品生成重构报告（untracked 未提交）。

---

## 5. GitHub Pages 部署验证方法

```bash
# Pages 源配置查证
gh api repos/High-cla/PlayerStore/pages --jq '.source'   # → {branch:master, path:/docs}
# 推送后轮询构建状态
gh api repos/High-cla/PlayerStore/pages/builds/latest --jq '.status'  # building→built
# 线上验证(注意先存工作区文件再 grep, 勿用 /tmp —— 见 §8 教训)
curl -s https://high-cla.github.io/PlayerStore/items_browser.html -o .tmp_v.html
grep -c "关键标记" .tmp_v.html; rm -f .tmp_v.html
```
线上 URL：https://high-cla.github.io/PlayerStore/items_browser.html
最新线上 items_browser.html 45351B（v69eabf6 部署后，含 function zhTag/isMirrorLabel、无「生成 ×1」残留）。

---

## 6. 遗留待办 / 开放问题

1. **真机复核（最高优先）**：用户需重启游戏进存档，刷新网页（本地文件或线上），复核：检查器 tag tab 全中文（类型标签日用品/净水用品/奢侈品 + 耐久系列）、无 TYPE-STRING_ 冗余、spawn 按钮可连点无冷却。→ 主菜单无存档时 /api/item 预期返回 "dump timeout/not found" 属设计行为。
2. **确认是否还有 314 之外的 TYPE-STRING_ 类型键未入映射**：真机已见 HOUSEHOLD_GOOD/WATER_PURIFICATION_SUPPLY/LUXURY_ITEM 恰在 TYPE_ZH；若真机再现新未翻译类型标签，需在 TYPE_ZH 补齐或扩 TAG_ZH。
3. **改动未 commit 的状态检查**：当前 HEAD 69eabf6 已含全部本会话改动并 push；无未提交改动。
4. 早期遗留（未验证/用户未提新需求前不做）：删除有冷却/两步确认、库存行 ×count 徽标显示——用户最后选择"按钮只显示生成、去防连点可连续生成"，该项已完成；用户曾贴 "已生成/×1" 描述均落在 spawn 按钮，已覆盖。

---

## 7. 浏览器自动化验证经验（Windows 无头 msedge）

- playwright-core 路径：`C:/Users/11/AppData/Roaming/npm/node_modules/@playwright/mcp/node_modules/playwright-core`，`channel:'msedge'` headless。
- **playwright MCP 通道(mcp2cli @playwright) 每调用新开 browser 实例、无法跨调用保持导航 → 弃用，改单个 node 脚本**（launch→goto→waitForSelector→evaluate 读取→close 全程一脚本）。
- mock server：临时 .cjs 静态服务 docs/ + 拦截 `/api/health` `/api/item` `/api/mine` `/api/inventory` 返回 mock JSON（item 的 tags 必须放**顶层 tags/tagsModified**，不是 state.tags）。
- `npm cache EPERM`（Windows）→ `export NPM_CONFIG_CACHE=<工作区>/.tmp_npmcache`。
- msedge 页面加载需 `await page.waitForFunction(()=>document.querySelectorAll('#tagBase .insp-row').length>0)` 而非 waitForSelector（visibility 判定有时卡住）。
- 结束清理：`netstat -ano | grep 26880` 找 PID → `powershell -Command "Stop-Process -Id <PID> -Force"`（taskkill //F 在此 shell 语法失败）。

---

## 8. 踩坑教训（易复发，务必记）

1. **Git Bash 每次 bash 调用是独立 /tmp 命名空间**——临时验证文件存 /tmp 再 grep 全 0 是假象；**一律存工作区 .tmp_* 再读，用完 rm**。
2. **IL2CPP 跨线程调用 = 进程级 AccessViolation**（`il2cpp_runtime_invoke`），托管 catch 拦不住。凡 DumpItem 里调 GetPublicDisplay()/GetActualDisplay()/GetActualValueModifier() 等 native 方法必须主线程；纯字段(identifier/name/unitCount/unitValue)跨线程侥幸安全。/api/mine、/api/inventory 从不崩，唯独 /api/item 崩过——已用 PendingItemDumps+DumpResults(ConcurrentDictionary<long,object>) 主线程消费修复。
3. **fastctx/mcp2cli 通道**：fastctx.exe Windows 端处理含 emoji/中文默认 locale(cp936) 崩 "surrogates not allowed" → 前缀 `PYTHONIOENCODING=utf-8 PYTHONUTF8=1`；路径须绝对；replace 的 dryRun 字段无效会被忽略导致静默实写（用 CLI --dry-run）。
4. **前端 API 字段位置**：renderTags 读 INS.tags/tagsModified 顶层；mock 别放 state.tags（0 行假象）。
5. 测试脚本注入前端函数：函数体经 vm 运行时依赖全局（ITEMS/TYPE_ZH/TAG_ZH/esc）都要随 bundle 注入或 stub，报错链常是 ReferenceError 逐层暴露。

---

## 9. 会话目标演进记录（供理解当前诉求脉络）

物品浏览器本地化/可用性优化线：
1. （早期，已交付）NEW_ITEMS 分类 30 物品中文名（EXTRA_NAMES 回填）
2. （已交付 f6cae87）314 键 tag 中文映射 tag_zh.js + tagHTML 行首 key 显示中文
3. （本会话最新 69eabf6）真机反馈两个缺陷修复：
   a. `TYPE-STRING_*` 物品类型标签未翻译 → zhTag 剥前缀查 TYPE_ZH/TAG_ZH
   b. spawn 按钮「生成 ×1」→「生成」，去 2500ms 冷却+disabled，可连续生成
   两者均经真机数据驱动 E2E 验证后 commit+push+Pages 上线。

用户沟通风格：中文，简短直接，一次只确认一件要事。回复用中文。
