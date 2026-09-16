# PlayerStore

ProgressMod + InventorySorter 单仓库（melons for *Probably Stolen*）

> 游戏：Probably Stolen（Questing Goose Studio）· MelonLoader 7 · IL2CPP
> Unity 网格物品背包 + 机械加工系统

最新发布：**v0.4.5**（v0.4.4 起支持游戏 0.46D；tag 见 [Releases](https://github.com/High-cla/PlayerStore/releases)）

## 模块

| 模块 | 路径 | 功能 |
| --- | --- | --- |
| **ProgressMod** | `PlayerStore/ProgressMod.cs` | 机械加工增强 + 网页生成物品：进度强制完成、免耐久、净化必纯、模块加成、永不受伤、无限拾荒、HTTP 物品生成服务器 |
| **InventorySorter** | `InventorySorter/InventorySorter/Core.cs` | 背包一键整理：大件最优布局 + 小件统一塞缝（5 候选同池择优、残局降级），最大化剩余连续空矩 |

> 版本事实以 git tag 为准（Assembly 里的 `1.12.1` 是历史静态值，未随 tag 更新）。

---

## 安装

1. 从 [Releases](https://github.com/High-cla/PlayerStore/releases) 下载 `ProgressMod.dll` + `InventorySorter.dll`
2. 放入 `<游戏目录>/MelonLoader/Mods/`（或游戏根目录 `Mods/`）
3. 依赖：MelonLoader 7（自带 Harmony + Il2CppInterop.Runtime）

---

## ProgressMod 功能清单

### 网页点击生成物品

- **内嵌本地 HTTP 服务器**：`http://localhost:26880/`
- **网页端物品浏览器**：https://high-cla.github.io/PlayerStore/items_browser.html
  （429 物品，可按分类筛选/搜索；点击"生成 ×1" → 直接进主背包）
- **通信链**：浏览器 fetch → 本地 HTTP → ProgressMod → 主背包
- **生成逻辑**：
  ```csharp
  if (!TrySpawnPrebuiltMachine(id, out item))     // 机器件: 直调原版 PreBuiltItemHelper.CreateX 工厂
      item = ItemSpawner.Spawn(id);                // 其余: 原版生成路径
  var inv = EmporiumEntry.Instance.invElement;     // 主背包网格
  inv.UncheckedAccept(item);                       // 强塞入包(不做槽位预检否决)
  ```
  主背包 = `EmporiumEntry.Instance.invElement`；HTTP 线程只入队，主线程消费（避免 Il2Cpp 跨线程）。

**使用**：游戏运行 → 打开网页生成器 → 状态点变绿（连接成功）→ 搜物品 → 生成。

### 配置项（MelonPreferences，游戏内生成配置文件）

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `ForceFinish` | `true` | 进度满：拦截推进并直接完成 |
| `NoDurability` | `true` | 机器/工具不消耗耐久 |
| `ModuleBoostMult` | `10` | 模块加成倍率 |
| `PurifyAlwaysPure` | `true` | 净化器/过滤器：`PurifyToBaseWater` 永远净化 100% 纯水 |
| `SpawnItemId` | `""` | 生成物品 stableId（F9 快捷生成，空=禁用） |
| `SpawnItemCount` | `1` | F9 生成数量 |
| `NeverWounded` | `true` | 永不受伤：拾荒/战斗不产生伤口、伤口不恶化、深夜不恶化 |
| `InfiniteScavenging` | `true` | 无限拾荒：次数/冷却不受限 |

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
- **横带路径**（`GroupByTag` 默认 `true`）：`LayoutBanded` 成功后同样过精修再采纳。
- 大网格（≥4000，理论边界）用落地堆积兜底（`TryPlaceUnits` 配对 + 单件）。

**择优判据**：剩余最大连续空矩最大者。验证：非堆叠全空格 / 堆叠 ≥1 新格可见。

**离线实测**（真 dump 296 会话，`tscripts/bench_native.py`）：空矩和 20056 → **20273**，碎片 1349 → 1317，平均移动 8.7% → 15.0%，拆散 0，安全失败（越界/重叠/压未动件）0。

### 关键机制

- **小件精修（塞缝）**：仅对非堆叠件，小件优先；释放自身格 → 取"能容纳它的最小空矩" → 该空矩内扫全朝向取最贴邻位；**贴邻不降才采纳**（防空隙不够时把贴簇小件拆散到孤立角落）
- **同类合并堆叠**：相同 ident+形状物品先合并计数，只布局 1 个代表格，其余重叠落到代表件（游戏堆叠自动合并）；容器（有内部格子）不参与堆叠，只移动
- **互补配对**：L 形/缺角物品两两尝试 4×4 朝向 × 全偏移合成矩形 → 配对单元整体落地（大仓 ≥100 格才配对）
- **容器留原位**：`KeepContainers` 开启时带内部格子的容器不参与重排
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
| `bench_banded.py` | 横带（分组）路径模型 + 横带精修对拍 |
| `bench_fillrefine.py` | 贴邻守卫对拍（guard OFF 296/61/103/20008 vs ON 296/0/0/20056） |
| `bench_residual.py` / `bench_online.py` | 残局降级救回率 / 镜像 C# 全流程对拍 |

---

## 网页物品数据源

| 文件 | 说明 |
| --- | --- |
| `docs/items_data_full.js` | 线上浏览器数据源（`const ITEMS = [...]`，429 项，单行 JSON） |
| `InventorySorter/tscripts/xmod/item-catalog.json` | 权威目录记录（399 项，items 元数据） |
| `mod/item_catalog.json` | 手维护中英表（441 项，`source_key` 指向本地化键） |
| `InventorySorter/tscripts/all_item_ids.json` | 物品 stableId 清单 |
| `docs/items_browser.html` | 浏览器页面（`?v=` 版本戳：数据更新需同步 bump） |

- 物品中文名来源：游戏本地化包 `Probably-Stolen-ZH-*/translation/localization_master.csv`（`table=Item`, `key=item_<id>_name`）。
- 新版新增物品（如 `fridge` / `beis_icecream`）若翻译包未收录，则中文名需人工补（当前：冰箱 / 贝伊斯冰淇淋）。
- 新增物品同时归入 `NEW_ITEMS` 分类，页面"🆕 新增物品"页签可直接看到。

---

## 构建

```bash
# ProgressMod
dotnet build PlayerStore/ProgressMod.csproj -c Release
# InventorySorter
dotnet build InventorySorter/InventorySorter.csproj -c Release
```

两个 csproj 通过单一属性 `<GameDir>` 定位游戏目录（改名只改一处），`OutputPath` 直写 `$(GameDir)\Mods\`（部署即生效）。Debug 构建自动本地提交（`AutoCommit` target）——**发布用的正式构建请用 `-c Release`**。

## 许可证

内部工具，未指定。
