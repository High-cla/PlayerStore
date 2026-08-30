# InventorySorter — 背包排序优化器

## 概述

InventorySorter 是《Probably Stolen Playtest》的 MelonLoader 模组，在任意存储窗口打开时提供 Sort 按钮，按背包网格约束（物品形状/旋转/格子占用）重新排列全部散件，**最大化排序后剩余的最大连续空白矩形**——让玩家能放入更大的物品。

## 核心目标函数

对一次排序 `S`（物品列表 + 各自摆放位置），定义：

```
剩余连续空间(S) = 背包网格中最大全空矩形面积（直方图+单调栈 O(W·H)）
```

所有算法以最大化此值为目标，而非最小化空隙（实验证明：最小化空隙策略会生成碎片，最大空矩反而小）。

## 算法组合（Ensemble）

每次排序并行运行 **7 路独立布局器**，各自产出完整摆放方案，择优返回（剩余连续空矩最大者）：

| #   | 布局器                    | 原理 |
| --- | ------------------------- | ---- |
| 1   | 配对落地                  | 先互补配对（见下），再贴地堆积 |
| 2   | 拆死锁落地                | 配对失败时定位死锁单元拆开该对，其余保持 |
| 3   | 无配对落地                | 全部单件贴地堆积 |
| 4   | 裸 MinHole                | 逐件放"最小化当前最大空矩"的位置 |
| 5   | PairMinHole               | 配对单元接力进 MinHole（级联） |
| 6   | StackMinHole              | 堆叠物可压已占格（≥1 格可见）的 MinHole |
| 7   | StackMinHole-Paired       | 配对单元 + 堆叠物叠放 |

**为何要组合**：实测无单一算法持续最优——24x10 大仓 MinHole 最优，10x10 模块群 GrowTouch 最优，8x8 小仓 GreedyBottom 最优。组合保证每仓用上最契合的算法。

## 互补配对（凸凹咬合）

`TryComplement`：对任意两件（含旋转 4 朝向 × 全平移偏移枚举），若它们的并集恰好填满一个矩形（无孔、无重叠），则合并为一个 `PairUnit` 单元整体落位。评分 = 并集面积，同形状 +1000000 优先。

已验证组合：stun_gun（右凹2）+stun_gun→4x5；cleaver+cleaver→8x2；pipe_weapon→6x2；fineness(缺0,1)+quality(缺0,0)→3x3。中间凹槽形（hatchet `####/..##`）与任何形状都不互补——数学上无法两两补成矩形。

**3 件+ 配对数学上不可能**：任何 L 形组合的格数（如 4+4+5=13）无法整除任何矩形格数。

## 堆叠处理

游戏允许所有物品堆叠（unitCount>1 可叠一格）。模组两处利用：

1. **合并同类投影**：相同 ident + 相同形状的物品（多件分开放）在布局中只占一份形状（代表件），应用阶段把其余件 `PlaceItem` 到代表件位置（重叠），游戏堆叠机制自动合并。实测大仓 +10~32 剩余空矩（如 24x10 144→176）。
   - 注意：相同 ident 但形状不同（如 精炼模组 2x2/2x3 变体）不能合并——形状不同叠放会错位。
2. **堆叠物品最后放置**：游戏按放置顺序渲染贴图，后放的在上层。堆叠物品延迟放置，保证至少一格视觉可见（否则被其他物品盖住，玩家会以为取不出）。

## 关键实现细节

- **配对数/CellCount 降序**：单元按占地格数降序（FFD 风格），先大后小。
- **落地约束**：`PlaceGrounded` 从最深行（H-1）向浅扫描，先贴地（cy+1==H）或贴已占块（occ[x,y+1]），4 朝向取(最深,最左)。
- **MinHole 剪枝**：候选位置需至少一格贴边/贴已占块（Touches），几乎不损质量，耗时减半。
- **择优重建校验**：候选方案的每个 Placement 用 mask cells 重放重建 occ，越界/重叠则丢弃该候选（防御布局器内部 bug）。

## 数据/验证

- 拖入任意背包数据由模组自动 dump 到 `Mods/inv_shape_dump.txt`（每次排序输出尺寸+物品形状+当前 occ/free/maxempty，per-session 缓存）。
- Python 验证套件（`tscripts/`）：`parse_dump.py`（会话级解析）、`verify_all.py`（7 算法对比）、`verify_merge.py`（合并比较）、`verify_stack.py`（堆叠比较）。

## 构建与部署

```bash
cd D:\git\invsort
dotnet build -c Debug     # 0 警告 0 错误；OutputPath 直指游戏 Mods 目录
```

构建即部署（InventorySorter.dll 拷入游戏 Mods）+ 自动 git 提交（csproj AutoCommit target）。

## 配置项（MelonPreferences，Category=InventorySorter）

| 键 | 默认 | 说明 |
| -- | ---- | ---- |
| Enabled | true | 主开关 |
| KeepContainersInPlace | true | 已放置的存储单元（舱/笼）不动，只排散件 |
| GroupByTag | true | 同 tag 物品相邻（不满足时回退紧凑布局） |
| MinCells | 10 | 隐藏小于此格数的网格 |
| ShowBackground | true | 显示常开背包（展示柜/主存储） |
| DisplayCaseSizes | "35,48" | 展示柜格数列表 |
| MainStorageSizes | "240" | 主存储格数列表 |
| IgnoreBackgroundSizes | "72" | 隐藏的无用常开网格 |
| UseNativeUI | true | 原生窗口 vs IMGUI 面板 |
| MaxRows | 7 | 原生窗口固定行数 |

## 版权

为《Probably Stolen Playtest》个人使用目的编写，Harmony 补丁，IL2CPP 环境。
