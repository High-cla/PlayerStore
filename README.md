# PlayerStore

ProgressMod + InventorySorter 单仓库（melons for *Probably Stolen*）

> 游戏：Probably Stolen（Questing Goose Studio）· MelonLoader 7 · IL2CPP
> Unity 网格物品背包 + 机械加工系统

最新版本：**v0.3.9**（网页点击生成物品）

## 模块

| 模块 | 路径 | 版本 | 功能 |
| --- | --- | --- | --- |
| **ProgressMod** | `PlayerStore/ProgressMod.cs` | v1.12.1 | 机械加工增强 + 网页生成物品：进度强制完成、免电/免耐久、品质必满、自动鉴定、永不受伤、HTTP 物品生成服务器 |
| **InventorySorter** | `InventorySorter/InventorySorter/Core.cs` | 1.0 | 背包一键整理：4 候选算法组合择优，最大化剩余连续矩形空间 |

---

## 安装

1. 从 [Releases](https://github.com/High-cla/PlayerStore/releases) 下载 `ProgressMod.dll` + `InventorySorter.dll`
2. 放入 `<游戏目录>/MelonLoader/Mods/`（或游戏根目录 `Mods/`）
3. 依赖：MelonLoader 7（自带 Harmony + Il2CppInterop.Runtime）

---

## ProgressMod 功能清单

### 网页点击生成物品（v0.3.9 新增）

- **内嵌本地 HTTP 服务器**：`http://localhost:26880/`
- **网页端物品浏览器**：https://high-cla.github.io/PlayerStore/items_browser.html
  （397 物品，可按分类筛选/搜索；点击"生成 ×1" → 直接进主背包）
- **通信链**：浏览器 fetch → 本地 HTTP → ProgressMod → 主背包
- **生成逻辑**（生成逻辑）：
  ```csharp
  GameItem item = DirectoryMaster.Item(stableId, true);   // 稳定ID创建
  if (!inventory.MayHaveValidInventorySlot(item)) ...     // 校验槽位
  if (!inventory.UncheckedAccept(item)) ...               // 入包
  ```
  主背包 = `EmporiumEntry.Instance.invElement`；HTTP 线程只入队，主线程消费（避免 Il2Cpp 跨线程）。

**使用**：游戏运行 → 打开网页生成器 → 状态点变绿（连接成功）→ 搜物品 → 生成。

### 配置项（MelonPreferences，游戏内生成配置文件）

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `ForceFinish` | `true` | 进度强制满 + 直接完成 |
| `NoDurability` | `true` | 机器/工具不消耗耐久 |
| `ModuleBoostMult` | `10` | 模块加成百分比 ×10 |
| `FreePower` | `true` | 免电运转 |
| `MaxWineQuality` | `true` | 酒品质最高档（tier 6） |
| `AutoIdentify` | `true` | 对质类物品（香烟/注射器/印章）自动鉴定 + UI 标签刷新 |
| `PurifyAlwaysPure` | `true` | 净水器永远净化 100% 纯水 |
| `TurboCooldownFree` | `true` | TurboBooster 无视冷却/充能延时，永远就绪 |
| `OutputMult` | `3` | 机器产出量倍率 |
| `NeverWounded` | `true` | 永不受伤：拾荒/战斗/深夜不产生伤口 |
| `NoAddiction` | `true` | 永不中毒：酒精/尼古丁/麻醉剂/赌博无成瘾 |
| `InfiniteScavenging` | `true` | 无限拾荒：次数/冷却不受限 |
| `SpawnItemId` | `""` | 生成物品 stableId（F9 快捷生成，空=禁用） |
| `SpawnItemCount` | `1` | F9 生成数量 |

### Harmony Patch 清单

| # | Patch | Hook | 功能 |
| --- | --- | --- | --- |
| 1 | `PatchContinue` | `MachineProgressHelper.ContinueProgressTypeMachine` | 进度推进拦截 + ForceFinish 直写 TARGET |
| 2 | `PatchUpdate` | `MachineryHelper.UpdateProcessingTypeMachine` | 真推进点日志（进度/target/速度 + tag dump） |
| 3 | `PatchEndNight` | `PlayerStore.EndNight` | 夜间未满进度机器瞬间完成 |
| 4 | `PatchFinish` | `MachineProgressHelper.FinishProgressTypeMachine` | 完成产出诊断日志 |
| 5 | `PatchDurability` | `DurabilityHelper.ChangeDurability` | NoDurability：跳过耐久扣减 |
| 6 | `PatchDegrade` | `ModuleEffectHelper.Degrade` | 模块效果衰退拦截 |
| 7 | `PatchRustToPure` | `WaterHelper.AddRustWater` | 锈水入口改灌 100% 纯水 |
| 8 | `PatchWaterGrade` | `WaterHelper.AddWater` | 总入口兜底：grade==4(RUST)→0(PURE) |
| 9 | `PatchPurifyToPure` | `WaterHelper.PurifyToBaseWater` | 净水器净化 100% |
| 10 | `PatchWaterLiquidLog` | `WaterHelper.AddLiquid` | 液体日志（诊断） |
| 11 | `PatchModuleBoost` | `ModuleHelper.InitModuleItem` | 模块创建：perf/eff/qual ×ModuleBoostMult，负加成取绝对值 |
| 12 | `PatchTurboReady` | `MachineTurboBoosterAdv.IsTurboReady` | TurboBooster 永远就绪 |
| 13 | `PatchWineQuality` | `WineHelper.GetWineQualityTier` | 酒品质 `Math.Max(__result, 6)` |
| 14 | `PatchCanPower` | `MachineHelper.CanPower`（Type 消歧） | FreePower 恒真 |
| 15 | `PatchTryDrawPower` | `MachineHelper.TryDrawCyclePower` | FreePower 跳过原逻辑（不扣电） |
| 16 | `PatchNeverWounded` | 伤口产生/恶化相关 | 永不受伤 |
| 17 | `PatchNoAddiction` | 成瘾相关 | 永不中毒 |
| 18 | `PatchInfiniteScavenging` | 拾荒相关 | 无限拾荒 |
| 19 | `PatchAutoIdentify` | `InspectableHelper.InitInspectableItem` | 自动鉴定 + `InspectionUIManager.OnInspectItem` 刷新标签 |
| 20 | `PatchWoundChance` | `GetMinorWoundChance`/`GetMajorWoundChance` | 伤口概率强制 0（无参签名修复） |

---

## InventorySorter 算法细节

**目标**：最大化剩余连续矩形空间（能放下更大物品）。

### 4 候选算法 Ensemble 择优（网格 < 4000 格）

数据驱动（120 组真实背包模拟重扫描，修复 MinHole 模拟 bug 后）：

| # | 候选 | 说明 |
| --- | --- | --- |
| 1 | `TryGrowTouch` | 生长触碰：贴已放块边界扩展 |
| 2 | `TryGuillotine` | 动态 Guillotine 切割 + **死洞惩罚**：评分 = waste×10 + 死洞面积（放不下任何剩余物品的碎片格），自动规避"碎掉小角"落点 |
| 3 | `TryLeftBottom` | 大背包左下锚定（17x10/11x14 漏网胜），聚左下块留右上 |
| 4 | `TryPlaceMFR` | MFR 池最小 waste，高密度（10x10 total=67）胜 |

大网格（≥4000，理论边界）用落地堆积兜底（`TryPlaceUnits` 配对+单件）。

**择优**：剩余最大连续空矩最大者。验证：非堆叠全空格 / 堆叠 ≥1 新格可见。

### 关键机制

- **同类合并堆叠**：相同 ident+形状物品先合并计数，只布局 1 个代表格，其余重叠落到代表件（游戏堆叠自动合并）；容器（有内部格子）不参与堆叠，只移动
- **互补配对**：L 形/缺角物品两两尝试 4×4 朝向 × 全偏移合成矩形 → 配对单元整体落地（大仓 ≥100 格才配对）
- **空间统计**：`DumpShapes` 输出 `inv_shape_dump.txt`：背包尺寸 + 占用/剩余 + 最大连续空矩形

### 验证套件（`InventorySorter/tscripts/`，纯 Python 无需游戏）

| 脚本 | 用途 |
| --- | --- |
| `parse_dump.py` / `parse_dump_merged.py` | 解析运行中 dump（会话分组 / 同类合并数据） |
| `verify_all.py` | 全算法库统一验证（最大连续空矩） |
| `scan_algos.py` | 全算法库扫描（120 组胜率/空矩对比） |
| `guillotine_test.py` | Guillotine vs Shelf 对比 |
| `guillotine_deadpen_test.py` | 死洞惩罚项验证 |
| `optimal_combo.py` / `benchmark_combo.py` | 组合择优/基准 |

---

## 构建

```bash
# ProgressMod
dotnet build PlayerStore/ProgressMod.csproj -c Release
# InventorySorter
dotnet build InventorySorter/InventorySorter.csproj -c Release
```

两个 csproj 的 `OutputPath` 默认指向游戏 `Mods/` 目录（部署即生效）。Debug 构建自动本地提交（`AutoCommit` target）。

## 许可证

内部工具，未指定。
