# PlayerStore

ProgressMod + InventorySorter 单仓库（melons for *Probably Stolen*）

> 游戏：Probably Stolen（Questing Goose Studio）· MelonLoader · IL2CPP
> Unity 网格物品背包 + 机械加工系统

## 模块

| 模块 | 路径 | 功能 |
| --- | --- | --- |
| **ProgressMod** | `PlayerStore/` | 机械加工进度增强：一键完成加工、免费用电、品质必满、模组增益倍率、酒品质必优等 |
| **InventorySorter** | `InventorySorter/` | 背包一键整理：多算法组合择优，最大化剩余连续矩形空间（互补配对 + 落地堆积 + MinHole 贪心 + 堆叠叠放） |

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

## 验证套件（InventorySorter）

`InventorySorter/tscripts/` 下为纯 Python 验证脚本（无需运行游戏）：

| 脚本 | 用途 |
| --- | --- |
| `parse_dump.py` | 解析运行中 dump 的背包形状 |
| `verify_all.py` | 7 算法 × 全部会话组统一验证（最大连续空矩） |
| `verify_merge.py` | 同类物品合并（堆叠）收益验证 |
| `verify_stack.py` | 堆叠叠放（≥1 格可见）收益验证 |
| `verify_real_dump.py` | C# 管线复刻（BuildUnits/PlaceGrounded） |

## 许可证

内部工具，未指定。
