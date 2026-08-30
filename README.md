# PlayerStore

ProgressMod + InventorySorter 单仓库（melons for *Probably Stolen*）

> 游戏：Probably Stolen（Questing Goose Studio）· MelonLoader · IL2CPP
> Unity 网格物品背包 + 机械加工系统

## 模块

| 模块 | 路径 | 版本 | 功能 |
| --- | --- | --- | --- |
| **ProgressMod** | `PlayerStore/ProgressMod.cs` | v1.12.1 | 机械加工增强：进度强制完成、免电/免耐久、品质必满、模块加成 ×10、酒品质最高档、纯水净化 |
| **InventorySorter** | `InventorySorter/InventorySorter/Core.cs` | 1.0 | 背包一键整理：7 算法组合择优，最大化剩余连续矩形空间 |

---

## ProgressMod 功能清单（15 个 Harmony Patch）

### 配置项（`Core` 静态常量，编译期）

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `ForceFinish` | `true` | 进度强制满 + 直接完成 + 夜间补完成 |
| `NoDurability` | `true` | 机器/工具不消耗耐久 |
| `ModuleBoostMult` | `10` | 模块加成百分比 ×10（负加成翻转成正） |
| `FreePower` | `true` | 免电运转 |
| `MaxWineQuality` | `true` | 酒品质强制最高档（tier 6） |

### 15 个 Patch

| # | Patch | Hook | 功能 |
| --- | --- | --- | --- |
| 1 | `PatchContinue` | `MachineProgressHelper.ContinueProgressTypeMachine` | 进度推进拦截：日志 + ForceFinish 时 CURRENT 直写 TARGET |
| 2 | `PatchUpdate` | `MachineryHelper.UpdateProcessingTypeMachine` | 真推进点日志（进度/target/速度 + tag 全 dump） |
| 3 | `PatchEndNight` | `PlayerStore.EndNight` | 夜间遍历机器，未满进度直接 `FinishProgressTypeMachine` 瞬间完成 |
| 4 | `PatchFinish` | `MachineProgressHelper.FinishProgressTypeMachine` | 完成产出诊断日志 |
| 5 | `PatchDurability` | `DurabilityHelper.ChangeDurability` | NoDurability：跳过耐久扣减 |
| 6 | `PatchDegrade` | `ModuleEffectHelper.Degrade` | 模块效果衰退拦截 |
| 7 | `PatchRustToPure` | `WaterHelper.AddRustWater` | 锈水入口改为灌 100% 纯水 |
| 8 | `PatchWaterGrade` | `WaterHelper.AddWater` | 总入口兜底：grade==4(RUST) → 0(PURE) |
| 9 | `PatchPurifyToPure` | `WaterHelper.PurifyToBaseWater` | 净水器：清空 + 加等量纯水（净化 100%） |
| 10 | `PatchWaterLiquidLog` | `WaterHelper.AddLiquid` | 液体日志（诊断） |
| 11 | `PatchModuleBoost` | `ModuleHelper.InitModuleItem` | 模块创建：perf/eff/qual ×ModuleBoostMult，负加成取绝对值 |
| 12 | `PatchTurboReady` | `MachineTurboBoosterAdv.IsTurboReady` | ForceFinish 时 TurboBooster 永远就绪 |
| 13 | `PatchWineQuality` | `WineHelper.GetWineQualityTier` | 酒品质 `Math.Max(__result, 6)` |
| 14 | `PatchCanPower` | `MachineHelper.CanPower`（`new Type[]{typeof(GameItem), typeof(GameSlotInventory)}` 消歧） | FreePower 恒真 |
| 15 | `PatchTryDrawPower` | `MachineHelper.TryDrawCyclePower` | FreePower 跳过原逻辑（不扣电） |

### 附加诊断

`[Continue]`/`[Update]`/`[Finish]` 前缀 MelonLogger 机器状态流；`DumpTags`/`Describe` 工具函数。

## Releases

见 [Releases](https://github.com/High-cla/PlayerStore/releases)：

- `ProgressMod.dll` → `MelonLoader/Mods/`（或游戏根目录 Mods/）
- `InventorySorter.dll` → `MelonLoader/Mods/`

依赖：MelonLoader 0.6.x + Harmony + Il2CppInterop.Runtime（MelonLoader 自带）。

## 构建

```bash
# ProgressMod
dotnet build PlayerStore/ProgressMod.csproj -c Release
# InventorySorter
dotnet build InventorySorter/InventorySorter.csproj -c Release
```

两个 csproj 的 `OutputPath` 默认指向游戏 `Mods/` 目录（部署即生效）。Debug 构建自动本地提交（`AutoCommit` target）。

## InventorySorter 算法细节

**目标**：最大化剩余连续矩形空间（能放下更大物品）。

### 7 算法 Ensemble 择优

| # | 候选 | 说明 |
| --- | --- | --- |
| 1 | 配对落地 | `BuildUnits` 两两互补（凸凹咬合成矩形）→ `TryPlaceUnits` 落地堆积 |
| 2 | 拆死锁落地 | 配对后首件放不下 → `SplitFailedUnit` 只拆该对重放 |
| 3 | 无配对落地 | 全单件落地堆积（贴地/贴块） |
| 4 | 裸 MinHole | 每步选"放置后最大空矩最小"的位置（挤压碎片、留整块） |
| 5 | PairMinHole | 配对单元喂给 MinHole 再优化（级联） |
| 6 | MinHoleStack | MinHole + 堆叠叠放（堆叠物可压已占格，≥1 格可见） |
| 7 | MinHoleStack(paired) | 配对单元 + 堆叠叠放 |

### 关键机制

- **互补配对**：L 形/缺角物品两两尝试 4×4 朝向 × 全偏移，并集恰好填满矩形 → 合并为 PairUnit 整体落位（如 stun_gun 右凹 2 格 + 半边左凹 2 格 → 4x5 满矩形）
- **落地堆积**：从深到浅扫行，至少一格贴地/贴已放块
- **堆叠结构**：同类物品（相同 ident+形状）合并为一件占位，其余重叠落到代表件位置（游戏堆叠机制自动合并）；堆叠物最后放置保证渲染在上层（至少一格可见，否则视觉上"拿不出来"）
- **空间统计**：`DumpShapes` 每次排序输出 `inv_shape_dump.txt`：背包尺寸 + 当前占用/剩余 + 最大连续空矩形位置

### 验证套件（`InventorySorter/tscripts/`，纯 Python 无需游戏）

| 脚本 | 用途 |
| --- | --- |
| `parse_dump.py` | 解析运行中 dump 的背包形状（会话级分组） |
| `verify_all.py` | 7 算法 × 全部会话组统一验证（最大连续空矩） |
| `verify_merge.py` | 同类物品合并（堆叠）收益验证 |
| `verify_stack.py` | 堆叠叠放（≥1 格可见）收益验证 |
| `verify_real_dump.py` | C# 管线复刻（BuildUnits/PlaceGrounded） |
| `analyze2.py` | 旧版 5 算法对比（BestFit/Shelf/GrowTouch/Rows/MinHole） |

## 许可证

内部工具，未指定。
