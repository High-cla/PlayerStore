# PlayerStore

ProgressMod + InventorySorter + NetworkUnlockMod 单仓库（melons for *Probably Stolen*）

> 游戏：Probably Stolen（Questing Goose Studio）· MelonLoader 7 · IL2CPP
> Unity 网格物品背包 + 机械加工系统

## 模块

| 模块 | 路径 | 功能 |
| --- | --- | --- |
| **ProgressMod** | `PlayerStore/ProgressMod.cs` | 机械加工增强 + 网页生成物品：进度强制完成、免耐久、净化必纯、模块加成、永不受伤、无限拾荒、HTTP 物品生成服务器 |
| **InventorySorter** | `InventorySorter/InventorySorter/Core.cs` | 背包一键整理：大件最优布局 + 小件统一塞缝（5 候选同池择优、同类聚带、残局降级），最大化剩余连续空矩；快捷键排序「最后打开的容器」+ 可开关的自动排序 |
| **NetworkUnlockMod** | `NetworkUnlockMod/NetworkUnlockMod.cs` | 解锁 demo 锁定内容：Harmony 接管 `NetworkUpgrade.IsLockedInDemo`（默认 10 个网络升级条目），不改 `GameAssembly.dll`、不随游戏更新失效 |

> 版本事实以 git tag 为准（Assembly 里的 `1.12.1` 是历史静态值，未随 tag 更新）。

---

## 安装

1. 从 [Releases](https://github.com/High-cla/PlayerStore/releases) 下载 `ProgressMod.dll` + `InventorySorter.dll` + `NetworkUnlockMod.dll`
2. 放入 `<游戏目录>/MelonLoader/Mods/`（或游戏根目录 `Mods/`）
3. 依赖：MelonLoader 7（自带 Harmony + Il2CppInterop.Runtime）

---

## ProgressMod 功能清单

### 网页点击生成物品

- **内嵌本地 HTTP 服务器**：`http://localhost:26880/`
- **网页端物品浏览器**：https://high-cla.github.io/PlayerStore/items_browser.html
  （451 物品，可按分类筛选/搜索；点击"生成 ×1" → 直接进主背包）
- **通信链**：浏览器 fetch → 本地 HTTP → ProgressMod → 主背包
- **生成逻辑**：
  ```csharp
  if (!TrySpawnPrebuiltMachine(id, out item))       // 机器件: 直调原版 PreBuiltItemHelper.CreateX 工厂
      if (!TrySpawnNativeLootCrate(id, out item))   // 容器类板条箱: 只在 ContainerItemDirectory 惰性注册
          item = ItemSpawner.Spawn(id);             // 其余: 原版生成路径
  var inv = EmporiumEntry.Instance.invElement;      // 主背包网格
  inv.UncheckedAccept(item);                        // 强塞入包(不做槽位预检否决)
  ```
  主背包 = `EmporiumEntry.Instance.invElement`；HTTP 线程只入队，主线程消费（避免 Il2Cpp 跨线程）。
  板条箱（`evidence_box`/`med_box`/`sec_box`/`service_box`/`eng_box`）不在静态物品表里，
  `ItemSpawner.Spawn` 必失败，须先走 `PreBuiltItemHelper.LootCrate*` native 工厂
  （对齐 ProbablyStolenItemManager 0.4.7 `TryCreateNativeLootCrate`）。

**使用**：游戏运行 → 打开网页生成器 → 状态点变绿（连接成功）→ 搜物品 → 生成。

### 配置项（MelonPreferences，游戏内生成配置文件）

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `ForceFinish` | `true` | 进度满：拦截推进并直接完成 |
| `NoDurability` | `true` | 机器/工具不消耗耐久 |
| `ModuleBoostMult` | `10` | 模块加成倍率 |
| `PurifyAlwaysPure` | `true` | 净化器/过滤器：`PurifyToBaseWater` 永远净化 100% 纯水 |
| `NeverWounded` | `true` | 永不受伤：拾荒/战斗不产生伤口、伤口不恶化、深夜不恶化 |
| `InfiniteScavenging` | `true` | 无限拾荒：次数/冷却不受限 |

> 历史上的 `SpawnItemId` / `SpawnItemCount`（F9 快捷生成）已在 `ca0866d` 移除，由网页生成器取代；这两个键会被 `PurgeLegacyEntries()` 清出配置文件。

### Harmony Patch 清单

| # | Patch 类 | Hook | 功能 |
| --- | --- | --- | --- |
| 1 | `PatchContinue` | `MachineProgressHelper.ContinueProgressTypeMachine` | 进度推进拦截 + `ForceFinish` 直写 TARGET |
| 2 | `PatchUpdate` | `MachineryHelper.UpdateProcessingTypeMachine` | 真推进点日志（进度/target/速度 + tag dump） |
| 3 | `PatchDurability` | `DurabilityHelper.ChangeDurability` | `NoDurability`：跳过耐久扣减 |
| 4 | `PatchPurifyToPure` | `WaterHelper.PurifyToBaseWater` | 净水器净化 100% 纯水 |
| 5 | `PatchModuleBoost` | `ModuleHelper.InitModuleItem` | 模块创建：perf/eff/qual ×`ModuleBoostMult`，负加成取绝对值 |
| 6 | `PatchRollMinorWound` | `ScavHelper.RollMinorWound` | 永不受伤：不产生轻伤 |
| 7 | `PatchRollMajorWound` | `ScavHelper.RollMajorWound` | 永不受伤：不产生重伤 |
| 8 | `PatchGetMinorWoundChance` | `ScavHelper.GetMinorWoundChance` | 轻伤概率强制 0 |
| 9 | `PatchGetMajorWoundChance` | `ScavHelper.GetMajorWoundChance` | 重伤概率强制 0 |
| 10 | `PatchReceiveMinorWound` | `HealthData.ReceiveMinorWound` | 受伤入口拦截 |
| 11 | `PatchReceiveMajorWound` | `HealthData.ReceiveMajorWound` | 受伤入口拦截 |
| 12 | `PatchIsSeriouslyWounded` | `HealthData.IsSeriouslyWounded` | 恒 false |
| 13 | `PatchHandleNightlyWound` | `HealthData.HandleNightlyWound` | 深夜不恶化 |
| 14 | `PatchCanScavenge` | `ScavHelper.CanScavenge` | 无限拾荒：始终可拾荒 |
| 15 | `PatchGetMaxScavAttempts` | `ScavHelper.GetMaxScavAttempts` | 拾荒次数不受限 |
| 16 | `PatchGetScavTimeLeft` | `ScavHelper.GetScavTimeLeft` | 拾荒冷却清零 |

---

## NetworkUnlockMod 功能清单

解锁游戏里**已经存在、但被 Playtest/demo 闸门锁住**的网络升级条目。

- **机制**（反编译证据 `dump/cpp2il_isil/IsilDump/Assembly-CSharp/`）：`WildUIManager.txt:1883` 调 `NetworkUpgrade.IsLockedInDemo(id)`（**全网唯一调用点**），为 `true` 时显示 `network_ui_demo_locked` 文案并禁用购买；`NetworkUpgrade.txt:2534` 该方法是 `public static bool`。
- **实现**：Harmony Prefix 命中解锁列表即 `__result = false` 并跳过原方法，其余 id 走游戏原逻辑；Postfix 把游戏原生判定为锁定的 id 写入日志（自检 + 核对列表）。**不改 `GameAssembly.dll`、不做内存字节补丁** ⇒ 不随游戏更新失效。
- **默认解锁**：`CHEMIST`、`PHARMA`、`CRIMINEL_NETWORK`、`SHOWCASE_II`、`RUINED_MACHINE_UNLOCK`、`RETIRED_GUNSMITH`、`RETIRED_CHEMIST`、`JACKSON2`、`RENOVATION3`、`RETIRED_FARMER`

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `Enabled` | `true` | 总开关 |
| `UnlockAllDemoLocked` | `false` | 解锁全部 demo 锁定条目（忽略 `UnlockIds`） |
| `UnlockIds` | 见上表 | 逗号分隔的条目 id（与游戏 `NetworkUpgrade` 的 id 一致，大写） |

**验证**：启动后 `MelonLoader\Latest.log` 出现 `[NetworkUnlock] 已启用…`；打开网络 UI 后出现 `[NetworkUnlock] 解锁网络条目: <ID>`（该行即证明拦到了游戏内部调用）与 `[NetworkUnlock] 游戏原生判定 demo 锁定: <ID>`。

> 与社区 `ProbablyStolenUnlockMod`（作者 小行星）的关系：那是内存字节补丁，仅在 `GameAssembly.dll` 的 SHA-256/大小与其 profile 一致时启用，游戏更新即自动停用（其 payload 含 143 处硬编码游戏 RVA，只能由作者按新构建重出）。两者互不干扰，但不要同时依赖。参考包见 `unlock/`（未入库）。

---

## InventorySorter 快捷键与配置

**排序快捷键**（默认 `F7`）：整理**你最后打开的那个容器**，不用在按钮列表里找。

- **「最后打开」用游戏原生判据**：`PixelWindow.focusStamp` 是全局自增焦点戳，每次 `ToFront`/提权 +1（转储 `dump/cpp2il_isil/IsilDump/Assembly-CSharp/PixelWindow.txt` 里的 `NextFocusStamp` / `ToFront`，写入点 `mov [rbx+0B0h],rax`）。于是 `WindowsHandler.current.visibleWindows` 中 focusStamp 最大的可排序窗口，就是玩家最后打开的那个。
- **支持组合键**：配置值可写 `F7` / `G` / `LeftControl+F7` / `LeftShift+LeftAlt+G`（修饰键名 `LeftShift` `RightShift` `LeftControl` `RightControl` `LeftAlt` `RightAlt`）；写错或留空自动回退 `F7`，并在 `MelonLoader\Latest.log` 写一行 warning。
- **自动排序**（`AutoSortLastOpened`，默认 `false`）：打开容器时自动整理那一个容器。触发口径是「窗口从无到有出现」（0.25s tick 快照 diff），**不是**每次点击聚焦 —— 免得玩家在容器里拿东西时被重排；执行再延后一个 tick，等窗口内容就绪。
- `F6` 仍是隐藏/显示排序面板。

**配置只有两项**（`AutoSortLastOpened` + `SortHotkey`；另有 2 个 `is_hidden` 内部项只记原生窗口位置）。其余原配置项按默认最优解**固化成 `Core.cs` 顶部常量**（分支保留，便于日后回调）：

| 原配置 | 固化值 | 常量 |
| --- | --- | --- |
| `Enabled` | `true`（恒开） | — |
| `KeepContainersInPlace` | `false`（容器也参与排序） | `KeepContainersConst` |
| `SkipBarterWindows` | `true` | `SkipBarterConst` |
| `MinCells` | `10` | `MinCellsConst` |
| `ShowBackground` | `true` | — |
| `GroupByTag` | `true` | `GroupByTagConst` |
| `MaxRows` | `7` | `MaxRowsConst` |
| `BandedToleranceRatio` | `0.05` | `BandedToleranceConst` |
| `UseNativeUI` | `true`（原生 UI 为唯一形态） | — |

- 旧键由 `PurgeLegacyEntries()` 在启动时（反射调 `DeleteEntry`）从 `MelonPreferences.cfg` 移除。
- 尺寸白名单三项一并删除，改为动态判断：无标题常驻存储一律标 `Storage (N)`（N = 格数）⇒ 容器升级/游戏新增容器自动适配；系统垃圾网格改由 `IsInsertLocked()` + `MinCellsConst` 过滤。
- 旧 IMGUI 面板（含 `FaceClicked` / `SizeInList`）随 `UseNativeUI` 固化而删除。

---

## InventorySorter 算法细节

**目标**：最大化剩余连续矩形空间（能放下更大物品）。

### 候选池同池择优（网格 < 4000 格）

| # | 候选 | 说明 |
| --- | --- | --- |
| 1 | `TryGrowTouch` | 生长触碰：贴已放块边界扩展 |
| 2 | `TryGuillotine` | 动态 Guillotine 切割 + 死洞惩罚（放不下任何剩余物品的碎片格） |
| 3 | `TryLeftBottom` | 大背包左下锚定（17x10/11x14 漏网胜），聚左下块留右上 |
| 4 | `TryPlaceMFR` | MFR 池最小 waste，高密度（10x10 total=67）胜 |
| 5 | `LayoutNativeFirstFit` | **照抄游戏原生 `InventorySortHelper.Sort` 语义**（大件先占、小件行主序 first-fit 落缝，无评分）；规格见 `docs/NATIVE_SORT_SPEC.md` |

- **同一精修层比较**：每个候选先各自过 `TryFillRefine`（小件塞缝），再按最大连续空矩择优 —— 否则"候选是否被精修过"会左右胜负。
- **严格采纳口径**：原生候选排在自研候选之后，**空矩持平则保留自研结果**（原生须严格更优才顶替）。实测原生只在 30/296 会话胜出、贡献空矩 +217，而平手也采纳会把 churn 从 15% 抬到 42%。
- **残局降级**（`TryResidualLayout`）：候选全失败时不再整包放弃 —— 大件先放，放不下的小件留在原位（其原格作为障碍），迭代至不动点，落地不与未动件重叠。
- **横带（同类聚带）路径**（`GroupByTagConst` 恒 `true`）：`LayoutBanded` 成功后同样过精修再采纳；横带与密集候选**同池定夺** —— 横带空矩 ≥ 密集空矩 − `BandedToleranceRatio`×密集空矩 时选横带（保「同类聚带」产品目标），否则选密集。
  - task-6 修复（原先近满包 0/93 全失败）：① 支撑改自支撑（`HasSupportSelf`：厚件/空网格首件可放）② 落位失败先重算整个 MFR 池（增量 `ShrinkRects` 切割丢空间）③ 仍失败则全网格自支撑 first-fit（`PlaceFirstFit`，实测零增量、留作安全网）。**仅横带路径启用**，密集路径 `HasSupport` 一字未改。
  - 实测（真 dump 296 会话）：grouped 成功率 **5.7% → 98.0%**，近满包 fill≥0.60 **0/93 → 87/93**。
  - 容差曲线（`tscripts/bench_banded.py`）：0% 132/296 空矩 21030 ｜ 2% 137/21025 ｜ 3%=4% 138/21021 ｜ 5% 146/20983。固化常量 **`BandedToleranceConst = 0.05`**（空矩成本实测 0.22%）；`0` = 空矩严格不退化，`1` = 强制聚带。
- 大网格（≥4000，理论边界）用落地堆积兜底（`TryPlaceUnits` 配对 + 单件）。

**择优判据**：剩余最大连续空矩最大者。验证：非堆叠全空格 / 堆叠 ≥1 新格可见。

**离线实测**（真 dump 296 会话，`tscripts/bench_native.py`）：空矩和 20056 → **20273**，碎片 1349 → 1317，平均移动 8.7% → 15.0%，拆散 0，安全失败（越界/重叠/压未动件）0。

### 关键机制

- **小件精修（塞缝）**：仅对非堆叠件，小件优先；释放自身格 → 取"能容纳它的最小空矩" → 该空矩内扫全朝向取最贴邻位；**贴邻不降才采纳**（防空隙不够时把贴簇小件拆散到孤立角落）
- **同类合并堆叠**：相同 ident+形状物品先合并计数，只布局 1 个代表格，其余重叠落到代表件（游戏堆叠自动合并）；容器（有内部格子）不参与堆叠，只移动
- **互补配对**：L 形/缺角物品两两尝试 4×4 朝向 × 全偏移合成矩形 → 配对单元整体落地（大仓 ≥100 格才配对）
- **容器留原位**：固化为关（`KeepContainersConst = false`）——带内部格子的容器也参与排序
- **空间统计**：`DumpShapes` 输出 `inv_shape_dump.txt`：背包尺寸 + 占用/剩余 + 最大连续空矩形

### 验证套件（`InventorySorter/tscripts/`，纯 Python 无需游戏）

| 脚本 | 用途 |
| --- | --- |
| `parse_dump.py` / `parse_dump_merged.py` | 解析运行中 dump（会话分组 / 同类合并数据） |
| `verify_all.py` | 全算法库统一验证（最大连续空矩） |
| `scan_algos.py` | 全算法库扫描（120 组胜率/空矩对比） |
| `guillotine_test.py` / `guillotine_deadpen_test.py` | Guillotine 对比 / 死洞惩罚项验证 |
| `optimal_combo.py` / `benchmark_combo.py` | 组合择优 / 基准 |
| `bench_native.py` | 原生语义 vs 自研候选对拍（含校准段与安全不变量列） |
| `bench_banded.py` | 横带（分组）路径模型：grouped 成功率逐项隔离 + 精修收益 + 容差曲线（grouped 选取率 vs 空矩代价） |
| `bench_fillrefine.py` | 贴邻守卫对拍（guard OFF 296/61/103/20008 vs ON 296/0/0/20056） |
| `bench_residual.py` / `bench_online.py` | 残局降级救回率 / 镜像 C# 全流程对拍 |

---

## 网页物品数据源

| 文件 | 说明 |
| --- | --- |
| `docs/items_data_full.js` | 线上浏览器数据源（`const ITEMS = [...]`，451 项，单行 JSON） |
| `InventorySorter/tscripts/xmod/item-catalog.json` | 权威目录记录（399 项，items 元数据） |
| `mod/item_catalog.json` | 手维护中英表（441 项，`source_key` 指向本地化键） |
| `InventorySorter/tscripts/all_item_ids.json` | 物品 stableId 清单 |
| `docs/items_browser.html` | 浏览器页面（`?v=` 版本戳：数据更新需同步 bump） |

### 前端结构

`docs/items_browser.html` 是零依赖单文件页（内联 CSS + 内联 JS，无构建步骤）。

**布局是固定外壳工作台，不是文档流**——改结构前先理解这一点，否则会把功能重新打散：

```
.app  100vh flex column
├── .topbar   54px   只放全局项: 品牌 / 搜索 / 状态灯 / 密度切换
└── .body     flex row, min-height:0
    ├── .sidebar  234px  左栏分类导航 (自身 overflow-y); ≤900px 转横向分类条
    └── .content  flex column
        ├── .toolbar    三视图切换 (图鉴 / 库存 / 我的生成) + 刷新 + 计数
        ├── .view-extra 视图专属过滤条 (按需填充)
        └── .scroll-area 内容区滚动容器 (grid 或 row-list); 同时承载整列环境光
.drawer  448px 右侧覆盖层 — 任意物品的唯一详情/编辑入口
```

要点：

- **`body{overflow:hidden}`**，滚动只发生在 `.sidebar` / `.scroll-area` / `.drawer-body` 三处。新增面板不要插进文档流，用视图或抽屉。
- **页面本身不滚动**；`min-height:0` 是 flex 子项能滚动的必要条件，勿删。
- **视图由 `setView(v)` 统一切换**，内容渲染分发到 `renderCatalog` / `renderInventory` / `renderMine`。注意 `renderInventory` / `renderMine` 只在对应视图激活时才写 DOM（`refreshXxx` 结束后按 `S.view` 决定是否渲染）。
- **抽屉统一承接两种语义**：图鉴条目 = 只读信息（`openCatalog`）；库存实例 = 基本信息 / 标签 / 特性 三 tab，可编辑（`openInstance` → `loadInstance`）。`openInstance` 返回 Promise 便于测试与串接。

- **设计令牌**：`<style>` 开头的 `:root{...}` 是颜色 / 间距 / 圆角 / 字号 / 表面 / 层级 / 族色的唯一定义处，改配色只改这里。
  - **视觉方向 = 深色 + 渐变过渡 + 零图片**。零图片是硬约束：全页不得出现 `<img>` / `<svg>` / `<canvas>` / `url()` 背景（游戏贴图是 IL2CPP 内部资源路径 `Items/xxx`，**不是 web 可访问的 URL，也不打算提取**）。视觉信息全部由排版、族色光与渐变承载。
  - **渐变是照明系统，不是装饰**：`.scroll-area` 顶部两道径向环境光 → 卡片是这束光下的承光面（卡体纵向渐变 + 卡顶 2px 族色渐变边 + 族色辉光 `.13` + 右下角 202deg 过渡）→ CTA / 导航激活 / 抽屉顶边同样是渐变。**不要在卡片上加左侧色条**（claude-design 明令的反模式），族色身份一律走卡顶渐变边与光。
  - 文本色按 WCAG AA 校准（小字 ≥4.5）。注意**辉光会提亮实际底色**，测对比度必须把族色按 `.13` 混入卡片底色再算——按纯灰底测会得到虚高值。
  - **暖色只作为"光"存在于卡片**：卡片承光面 `--card-*` 与外壳同为中性冷灰；暖味来自四样东西 —— 卡顶 2px 族色渐变边、族色辉光 `.13`、卡内暖文字 `--fgc/--fgc2/--fgc3`、暖族标识色。四者**只被 `.card` 作用域消费**；外壳（顶栏 / 导航 / 工具栏 / 抽屉 / 行列表 / toast / 滚动条）一律用 `--bg/--surface/--border/--fg*`。两组令牌分开定义 —— 改卡片不牵连外壳。新增组件时按归属选令牌，别把 `--fgc*` 用在外壳上。静态检查：`grep -nE '(--card-top|--fgc)' docs/items_browser.html`，消费处必须全带 `.card` 前缀。
- **族色系统（8 暖色族 + 中性）**：26 个类别收敛成 `--f-<族>-{1,2}` 8 族（ember / rose / amber / moss / orange / copper / plum / sand）+ `--f-none-*`，色相全部落在 340°–55° 暖区。`CAT_FAMILY`（JS）做类别→族映射，`famOfItem` / `famOfId` 求族，元素带 `fam-*` 类即获得 `--g1/--g2`。`--c-<category 小写>` 仍存在，是**族色的族内别名**，供导航色点与分类 chip 复用（`catColor` 读的仍是 `--c-*`，未改）。
  - 新增类别必须同时进 `CAT_ZH`（中文名）/ `CAT_GROUPS`（导航分组）/ `CAT_TOKEN`（色点）/**`CAT_FAMILY`（族色，漏了会落 'none' 中性族）**，并给 `--c-<category>` 指向某个 `--f-*`。
  - `ID_FAMILY` 是 `id → 族` 的预建 Map（库存行只有 id）：**逐行 `ITEMS.find` 是 O(rows × 481)，不要那样写**。
- **分类导航分组**：27 个分类在 `CAT_GROUPS` 里归为 5 组（装备与武器 / 物资与材料 / 工具与模块 / 生活与交易 / 生成器）。新增分类必须同时进 `CAT_ZH`（中文名）与 `CAT_GROUPS`（否则不在导航中出现）与 `CAT_TOKEN`（配色）。
- **API 层**：所有请求经 `apiFetch(path, ms)`（`AbortController` 超时 8s、探针 15s；响应非 JSON 容错；统一 `{ok, err}` 形状）。**它返回的是已解析对象，调用方不要再 `.json()`**——那会抛 `TypeError`，且会被外层 `try/catch` 吞成"加载失败"（v0.5.7 修复过此类回归）。新增端点必须走它，不要再直接 `fetch(API + …)`。
- **服务器状态探针**：`/api/health` 只证明 HTTP 线程存活（不触主线程）；`checkServer()` 会再打一次 `/api/mine` 证明主线程可达，二者皆通才显示绿点。主线程停摆（未进存档 / 窗口失焦）时点「生成」只入队而无产物，故 `spawnItem` 拿到 token 后轮询 `/api/mine` 确认落地才报「已生成」，超时报错而非假成功。
- **卡片不是按钮**：卡片是 `role="group"`（无 `tabindex`），**点卡片正文 / 名称 / 描述都不开抽屉**；详情只由右下角「详情」按钮（`data-detail`）打开。**勿把 `onclick` 加回卡片本体**。
- **卡片按钮排**：普通物品 = 生成 / 生成十个 / 详情（`data-spawn`，十个那只是 `data-count="10"`；`.foot-right` 把详情推到右侧）；`_instruction` 图纸卡 = 提示文字 + 详情。`spawnItem(id, btn, count)` 第三参是数量，直接进 `/api/spawn?...&count=n`（后端已限 1–999；省略即 1，向后兼容）。按钮复位靠 `b.dataset.orig` 记录原始类，**只在首次记录**（生成中 / 已生成会改写 `className`，每次都记会把状态类当成原类，幽灵按钮会被染成实心）。
- **无障碍**：全站 `:focus-visible` 焦点环；**库存行**可键盘打开（Enter/Space）；分类导航重建 DOM 后会回填焦点（`renderNav` 尾部的 `focused` 逻辑，勿删）；toast `role="status" aria-live="polite"`；Escape 关抽屉；`/` 聚焦搜索。
- **卡片等高由构造保证**：`.card-desc` 固定 `height:38px` + `-webkit-line-clamp:2`；`.card-cats` 与 `.card-foot` 各有 `min-height`（23px / 42px），因为 16 个 `_instruction` 卡既无分类 chip、foot 又只有一行提示文字——不补 min-height 会让它们矮 23px（实测 210 vs 233）。**删这三个尺寸中的任何一个，等高都会破**。
- **密度变体**：`setDensity` 同时给 `#grid` 与 `#rowList` 加 `.compact`，持久化在 `localStorage.ps_density`。compact 下描述整块隐藏，故卡片高度不再等高（紧凑模式按内容收缩，属预期）。
- `esc()` 转义 `& < > " '` 五类；`localStorage` 键 `itemFavs` / `mySpawnTokens` / `ps_density`。

- 物品中文名来源：游戏本地化包 `Probably-Stolen-ZH-*/translation/localization_master.csv`（`table=Item`, `key=item_<id>_name`）。
- 新版新增物品（如 `fridge` / `beis_icecream`）若翻译包未收录，则中文名需人工补（当前：冰箱 / 贝伊斯冰淇淋）。
- 新增物品同时归入 `NEW_ITEMS` 分类，页面"🆕 新增物品"页签可直接看到。

---

## 异常处理纪律（IL2CPP）

IL2CPP 下读取宿主对象成员（`GameItem.*` / `GameInventory.*` / `PixelWindow.*` 等）会抛托管异常，**静默回退是正确姿势**——探测失败不该让模组崩溃或刷屏日志。但每个 `catch` 必须说清"为什么可以静默"，否则后来者无法区分它是刻意设计还是遗漏。

`Core.cs` 与 `ProgressMod.cs` 共用同一约定：

| 形态 | 写法 | 适用 |
| --- | --- | --- |
| 通用模板 | `// ponytail: IL2CPP native probe, silent fallback`<br>`/* IL2CPP 异常: 保持原值 */` | 单点探测，回退值语义自明 |
| 具体化 | 写明**哪个成员**读失败、**静默下去会怎样** | 静默会造成数据丢失或改变功能语义 |
| 日志 | `MelonLogger.Warning(...)` + 说明 | 失败需要留痕但不中断（HTTP 路由、物品生成） |

具体化的实例（ProgressMod.cs:269，`item.uniqueId` 读失败）：

> `// 静默数据丢失: 读不到 uniqueId 的物品会被当成 u == 0 丢弃, 不加入列表。`
> `// /api/inventory 响应里缺这个物品 (网页生成器看不到)。此路径由 HTTP 请求触发,`
> `// 不是每帧路径, 故静默优于刷屏定位。`

**无需注记的自证例外**：单行访问器 `catch { return <默认值>; }`（回退值即函数签名承诺的默认值，如 `BoxW`→`1`、`PosX`→`0`、`Stacked`→`false`）；Harmony `Prefix` 的 `catch { return true; }`（放行原逻辑，不吞掉游戏行为）。

**当前覆盖**（词法扫描计数，已排除注释/字符串里的 `catch` 字样）：

| 文件 | catch 总数 | 带注记 | 带日志 |
| --- | --- | --- | --- |
| `InventorySorter/InventorySorter/Core.cs` | 65 | 38 | 6 |
| `PlayerStore/ProgressMod.cs` | 81 | 58 | 17 |

其余无注记项均为上表的自证例外。**尚无自动化检查**——新增 `catch` 时需人工对照本约定（宁可写清回退值，也不要空块）。

## 构建

```bash
# ProgressMod
dotnet build PlayerStore/ProgressMod.csproj -c Release
# InventorySorter
dotnet build InventorySorter/InventorySorter.csproj -c Release
# NetworkUnlockMod
dotnet build NetworkUnlockMod/NetworkUnlockMod.csproj -c Release
```

三个 csproj 通过单一属性 `<GameDir>` 定位游戏目录（改名只改一处），`OutputPath` 直写 `$(GameDir)\Mods\`（部署即生效）。`AutoCommit` target 默认关闭，需要时显式加 `-p:AutoCommit=true`。

**发布必须 `-c Release`**：`dotnet build` 不带 `-c` 时默认 `Debug`，产物带 `DebuggableAttribute`、未优化、体积明显偏大（ProgressMod Debug 73216 B / Release 68608 B）。v0.5.1–v0.5.3 三个 release 的资产曾误用 Debug 构建（源码逻辑等价，但非发布配置）——已由 v0.5.4 起改以 Release 发布。

## 许可证

内部工具，未指定。
