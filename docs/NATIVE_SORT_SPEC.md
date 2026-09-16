# 原生背包排序 InventorySortHelper — 逆向规格

> 目标: 把游戏新版本新增的原生背包排序算法 `InventorySortHelper` 还原成「另一个人照着就能写出来」的精确规格, 并给出 mod(InventorySorter) 的接入建议。
> 素材: `dump/cpp2il_isil/IsilDump/Assembly-CSharp/`(只读) + 游戏目录 IL2CPP interop 程序集的元数据(`D:/steam/steamapps/common/Probably Stolen Demo/MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`, 只读反射查询)。
> 约定: 每条结论后附 `文件:行号`。凡未能从证据直接确定的, 一律标 **未确证**, 不写猜测当结论。
> ISIL 文件结构: `Method:` 头 → `Disassembly:`(x64, 字符串是地址) → `ISIL:`(可读伪汇编, 字符串字面量在此)。本文行号一律指 ISIL 段行号。

---

## 0. 结论速览

| # | 结论 | 关键证据 |
|---|---|---|
| 1 | `Sort` 不是"重排列表", 而是**重新计算每个物品的摆放位置**: 从小→大(或大→小)依次取物品, 为每个物品找第一个能放下的格子, 写入该物品的 `modifiedShape` | `InventorySortHelper.txt:1247-1264`(写回 `item+0x1A0`), 列表本身不重排 |
| 2 | 排序优先级键 = **占格数 `CellCount`** → 相同则 **`identifier`(序数比较)** → 相同则 **当前格坐标行主序下标 `minY*inventoryShape.width + minX`** | `InventorySortHelper_NestedType___c__DisplayClass2_0.txt:020-105` |
| 3 | 扫描顺序: 外循环 y、内循环 x; `fromEnd=false` 从 `(0,0)` 递增, `fromEnd=true` 从 `(maxX,maxY)` 递减(即整个枚举序列的**完全逆序**), **第一个满足条件的候选即胜出**(不评分、不比较"浪费") | `InventorySortHelper.txt:1058-1095`, `1509-1546` |
| 4 | 判定"能否放": ①`FitsOccupancy`(逐格查 bit) 且 ②`inventory.CheckShapePlacement` 家族虚调用 | `InventorySortHelper.txt:1086`, `1095` |
| 5 | `itemLayers` 是 `byte` 的**二维(rank-2)数组**, 维度 `[inventoryShape.width, inventoryShape.height]`, 单元 `[x,y]` 存**层占用位掩码**; 形状单元字节 = 层号, 参与的是 `1 << (字节 & 31)` 这一位 | `InventorySortHelper.txt:968-984`, `1717-1726`, `1876-1880` |
| 6 | `CellCount(shape)` = 形状局部 `width×height` 内**非零单元个数**(不是包围盒面积) | `InventorySortHelper.txt:1971-1998` |
| 7 | `MaySort`/`CanSort` = 「未上锁 + 可插入 + 物品数>0」, `CanSort` 另加逐元素检查(被销毁元素/不可移除元素/不能整体移动的物品 → 不可排序); UI 用 `MaySort` 决定是否显示排序区, 用 `CanSort` 决定 4 个按钮是否可用 | `InventorySortHelper.txt:62-95`, `267-376`; `ItemContextHandler.txt:4499`, `4525-4527`, `4530-4553` |
| 8 | 4 个按钮 = `bigFirst × fromEnd`: BigStart(true,false) / SmallStart(false,false) / BigEnd(true,true) / SmallEnd(false,true), 节点名 `nodeButtonSortBigStart/…` | `ItemContextHandler.txt:1577-1748`, `3978-4009` |
| 9 | `InventorySortHelper` 是 **public static 类**, `Sort/MaySort/CanSort` 为 **public static** → 可被 mod 直接调用; `FindSpot/FitsOccupancy/MarkOccupied/CellCount` 原生是 **private static**(interop 桩里仍是 public) | IL2CPP interop 元数据(见 §10.1) |
| 10 | 接入建议: **混合, 以「直接调用原生 `Sort`」为首选**(理由见 §10) | §10 |

---

## 1. 类型事实(方法论: 先用 interop 元数据定向, 再用 ISIL 还原控制流)

用一次性反射程序读取游戏 interop 程序集, 得到的**权威签名/可见性**(证据: interop 程序集 `Il2Cpp.InventorySortHelper` 反射结果, 见 §10.1 输出):

```
Il2Cpp.InventorySortHelper  public=True abstract=True sealed=True   // 即 public static class
  public static Boolean MaySort(GameGridInventory inventory)                     // 原生 Public_Static
  public static Boolean CanSort(GameGridInventory inventory)                     // 原生 Public_Static
  public static Boolean Sort(GameGridInventory inventory, Boolean bigFirst, Boolean fromEnd)  // 原生 Public_Static
  public static GridShape FindSpot(GameGridInventory, GameItem, List`1 movingItems, Il2CppObjectBase itemLayers, Boolean fromEnd)  // 原生 Private_Static
  public static Boolean  FitsOccupancy(GridShape shape, Il2CppObjectBase itemLayers)  // 原生 Private_Static
  public static Void     MarkOccupied(Il2CppObjectBase itemLayers, GridShape shape)   // 原生 Private_Static
  public static Int32    CellCount(GridShape shape)                                  // 原生 Private_Static

Il2Cpp.InventorySortHelper+__c__DisplayClass2_0   // Sort 的闭包
  public Boolean  bigFirst          // +0x10
  public GridShape inventoryShape   // +0x18
  public Int32 _Sort_b__0(GameItem a, GameItem b)
```

关键继承/类型关系(同一次反射):

- `Il2Cpp.GridShape` — `public abstract`, 全部几何属性是 **abstract virtual**: `get_minX/get_minY/get_globalWidth/get_globalHeight/get_maxX/get_maxY/get_flipped/get_orientation/get_width/get_height`, 方法 `Clone/GetLocal(x,y)/GetOutsideBounds/Get(x,y)/ForwardTransform/InverseTransform/Intersects…`。
  → 这也是 `GridShape.txt:3-72` 里 10 个属性 getter 全是 "No ISIL was generated" 的原因(抽象)。
- `Il2Cpp.GridShapeBuilder` — 实现 GridShape 的全部抽象属性(`Virtual_Final_New`), 自身字段(声明序, 即 ISIL 里 `Field(index)` 的编号): `0 _minX(+0x10)`, `1 _minY(+0x14)`, `2 _flipped`, `3 _orientation`, `4 _width(+0x20)`, `5 _height(+0x24)`, `6 data`, `7 outside(+0x30)`; `get_shape()` 直接 `return this`(`GridShapeBuilder.txt:215-223`)→ **builder 本身就是 GridShape**。
  → 这解释了 §3 里 `Call 0x180002BD0(index, typeof(GridShape), obj)`: 它是 **按"成员编号"读取 GridShape 的属性/方法**, 编号 = 上表 0-based 声明序号, 与 `GridShape.txt` 的 `Method:` 出现次序完全一致:
  `0 get_minX, 1 get_minY, 2 get_globalWidth, 3 get_globalHeight, 4 get_maxX, 5 get_maxY, 6 get_flipped, 7 get_orientation, 8 get_width, 9 get_height, 10 Clone, 11 GetLocal, 12 GetOutsideBounds, 13 Get, 14 ForwardTransform, …`
  校验: `GetOutsideBounds()` 内是 `GetLocal(-1,-1)`(编号 11)✓ `GridShape.txt:118-127`; `FitsOccupancy` 用编号 13(`Get(x,y)`)✓; `CellCount` 用 8/9/11 ✓。
- `Il2Cpp.GameItem` — `_shape(+0x198)`, `_modifiedShape(+0x1A0)`, `_identifier(+0x1C0)`, `_uniqueId(+0x1D0)`, `_name(+0x1D8)`, `_unitCount(+0x1F8)`; 方法 `MayRemove()`, `MaxNumRemove()`, `get_shape()`, `get_identifier()`, `get_name()`(偏移与属性对照证据: `GameItem.txt:270-291`(shape=+0x198), `293-297`(modifiedShape=+0x1A0), `402-423`(identifier=+0x1C0), `438-451`(uniqueId=+0x1D0), `458-479`(name=+0x1D8))。
- `Il2Cpp.GameItemElement : Il2Cpp.GameItem`(反射 BaseType) → 元素本身就是物品, 因此 `List<GameItem>` 里可以是 `GameItemElement`。
- `GameGridInventory.inventoryShape`(GridShape, 实例字段 +0x1B0) — `GameGridInventory.txt:265-287` 有 `get_inventoryShape/set_inventoryShape`; `Sort` 把 `[inventory+0x1B0]` 存进闭包字段 `inventoryShape`(`InventorySortHelper.txt:930-934`)。

---

## 2. `MaySort` / `CanSort`

### 2.1 `MaySort(inventory)` — 证据 `InventorySortHelper.txt:3`, ISIL `:51-96`

```
if (inventory == null) return false;                                  // :62-63
if (inventory.IsInsertLocked(0)) return false;                        // :66-68  (GameInventory.IsInsertLocked, GameInventory.txt:2646)
if (!GeneralHelper.MayPlayerInsertInto(inventory, 0)) return false;   // :75-77
var col = inventory.<虚拟属性/方法>(...);                              // :78-82  经 vtable [klass+0x288]/[+0x290] 调用
if (col == null) throw;                                               // :83-84  (null 解引用异常)
return col.Count > 0;            // col+0x18 = List<T>._size        // :85-86  ("setg al")
```
实际语义: **未上锁 && 允许玩家放入 && 容器里还有物品**。
(集合对象带 `Count@+0x18`, 是 `List<GameItem>`; 具体属性名未确证, 候选 `GameGridInventory.get_childItems()` — `GameGridInventory.txt:65`。)

### 2.2 `CanSort(inventory)` — 证据 `InventorySortHelper.txt:98`, ISIL `:240-381`

```
if (inventory == null) return false;                                  // :267-268
if (inventory.IsInsertLocked(0)) return false;                        // :271-273
if (!GeneralHelper.MayPlayerInsertInto(inventory, 0)) return false;   // :280-282
var col = inventory.<同一虚拟成员>();
if (col == null) throw;                                               // :288-289
if (col.Count <= 0) return false;                                     // :290-291
foreach (var el in col) {                                             // :299-314 (GetEnumerator<Object>)
    if (el is GameItemElement && el.IsDestroyed()) return false;      // :320-334
    if (!el.MayRemove()) return false;                                // :341-343  (JumpIfEqual {109} → return 0)
    if (el.MaxNumRemove() >= el.unitCount) continue;                  // :346-348  ([rdi+0x1F8]=unitCount, JumpIfGreaterOrEqual {72})
    return false;                                                     // :349-352
}
return true;                                                          // :353-356
```
判读: `CanSort` = 「容器可以排序」; 只要有一个元素被销毁(UI 元素) / 不可移除(上锁物品) / **可移除但不能整体移动**(`MaxNumRemove() < unitCount`, 即排序会被迫拆栈) → 返回 false。
> `MayRemove`/`MaxNumRemove` 的语义按名字与用法推断(名称由 Cpp2IL 直接解析, 可信); 它们内部实现未展开 → **未确证**。

---

## 3. `Sort(inventory, bigFirst, fromEnd)` — 完整控制流

证据: 头 `InventorySortHelper.txt:383`; ISIL `:846-1323`; 比较器在 `InventorySortHelper_NestedType___c__DisplayClass2_0.txt:14-226`。

### 3.1 前置与准备

```
if (!CanSort(inventory)) return false;                                // :925-927  (注意: Sort 内部再调一次 CanSort)
var __this = new <>c__DisplayClass2_0();                              // :914-921
__this.bigFirst       = bigFirst;                                     // :922  (闭包 +0x10)
__this.inventoryShape = inventory.inventoryShape;                     // :930-934 (+0x1B0 → 闭包 +0x18)
var movingItems = new List<GameItem>(inventory.<上文的物品集合>);       // :935-950
movingItems.Sort(new Comparison<GameItem>(__this.<Sort>b__0));         // :951-964  (比较器见 §8)
int w = __this.inventoryShape.width;                                  // :968-971 (编号 8)
int h = __this.inventoryShape.height;                                 // :972-977 (编号 9)
byte[,] itemLayers = new byte[w, h];                                  // :978-984  (§5.1)
var placed = new List<ValueTuple<GameItem, GridShape>>();              // :985-993
```
要点:
- 排序**副本** `movingItems`, 物品列表本身的顺序不被改动; `movingItems` 之后作为第三参数传给"能否摆放"判定(对应形参名 `ignoredItems`, 见 §4 步骤 C)。
- `bigFirst` 只进比较器(决定升/降序); `fromEnd` 只影响下面的扫描方向。

### 3.2 第一趟: 逐物品找位并落位(等价于 `FindSpot` + `MarkOccupied` 被内联)

```
foreach (var item in movingItems) {                                   // :1009-1023
    var baseShape = item.modifiedShape;                               // :1023   (= item+0x1A0, ★modifiedShape, 不是 shape)
    var b = new GridShapeBuilder(baseShape);                           // :1024-1032  (ctor(GridShape), 保留 flipped/orientation)
    int maxX = __this.inventoryShape.width  - b.globalWidth;           // :1033-1043 (编号 8 后减 get_globalWidth)
    int maxY = __this.inventoryShape.height - b.globalHeight;          // :1044-1053
    if (maxX < 0 || maxY < 0) return false;   // :1054-1057 (JumpIfSign {285}) ★形状比容器还大 → 整次排序中止失败
    for (int ic = 0; ic <= maxY; ic++) {                              // :1058-1068
        int y = fromEnd ? (maxY - ic) : ic;
        for (int jc = 0; jc <= maxX; jc++) {                          // :1070-1082
            int x = fromEnd ? (maxX - jc) : jc;
            b.SetPosition(x, y);                                      // :1082  (SetPosition(minX,minY): GridShapeBuilder.txt:612-618)
            if (!FitsOccupancy(b, itemLayers)) continue;              // :1086
            if (!inventory.<CheckShapePlacement 家族虚调用(item, b, movingItems)>) continue;   // :1095
            var shape = b.???               // builder→GridShape            :1100  (§5.4)
            MarkOccupied(itemLayers, shape);                          // :1107
            placed.Add((item, shape));                                // :1114-1121
            break;                    // ★第一个可行候选即胜出 → 换下一个物品 (277 Goto {161})
        }
    }
    // 候选穷尽仍无解 → {285} 分支: 释放枚举器 → {457} return false (整次排序中止, 不跳过该物品)
}
```
- **扫描顺序**(精确): 外循环 = 行(y) 计数 `ic` 从 0 到 `maxY`; 内循环 = 列(x) 计数 `jc` 从 0 到 `maxX`。
  `fromEnd == false` → `x=jc, y=ic`(从网格 `(0,0)` 起, 行内 x 递增, 再下一行);
  `fromEnd == true` → `x=maxX-jc, y=maxY-ic`(从 `(maxX,maxY)` 起, 完全逆序)。
  两者是**同一枚举序列的正序/逆序**(两个轴同时反向) — `InventorySortHelper.txt:1064-1068`, `1073-1078`。
- **锚点**: `SetPosition(x,y)` 设的是形状包围盒的 **min 角**(`minX,minY`), 即"左上/最小角对齐", 坐标域 `x∈[0, w-b.globalWidth]`, `y∈[0, h-b.globalHeight]`(`globalWidth/Height` 已含 flipped/orientation 影响: `GridShapeBuilder.txt:43-78`)。
- **不参与角度/朝向变换**: builder 从物品原 `modifiedShape` 复制, 循环只改位置 → 排序不改朝向。
- **平手/评分**: 没有评分函数; 第一个满足两个条件的候选直接被采用(§0-3)。
- **失败语义(重要)**: `Sort` **不跳过任何物品** —— 只要某个物品的 `maxX<0 || maxY<0`, 或它在整个网格里找不到任何候选位置, 或 `builder→GridShape` 得到 null, 都会走 `{285}` 分支释放枚举器并 **`return false` 中止整次排序**(已写入 `itemLayers` 与 `placed` 的中间结果随调用一起被丢弃, 因为没有任何写回动作执行到第二趟 —— 第二趟只在成功路径上, `:1287` 之前)。这一点与 `FindSpot` 不同(`FindSpot` 只 `return null` 表示该物品无处可放, §4)。

### 3.3 第二趟: 把结果应用回背包元素

```
foreach (var pair in placed) {                                        // :1145-1163 (第二次枚举)
    var el = pair.Item1;                                              // :1164-1175 (GameItemElement 类型检查)
    if (el is GameItemElement && el.IsDestroyed()) continue;          // :1183-1185
    el.modifiedShape = new GridShapeBuilder(el.modifiedShape)          // :1247-1264
                          .SetTransform(pair.Item2);                  //   (SetTransform(GridShape): GridShapeBuilder.txt:664+)
    inventory.<虚方法>(el);        // 单参数, vtable [klass+0x268]/[+0x270]  :1265-1270
}
// 收尾: disposed 枚举器 + 一次无参虚调用 (vtable [klass+0x2B8]/[+0x2C0])  :1283-1286
return true;                                                          // :1287
```
- 逐元素写回的是 **`GameItem.modifiedShape`(+0x1A0)**, 并且用 `SetTransform` 把第一趟算出的位置/朝向搬过去(`GameItemElement` 另有 `tempShape` 属性 — `GameItemElement.txt:374-396` — 本算法**没有**用到它)。
- 两处 `inventory.<虚方法>` 的**具体方法名未确证**(只看到 vtable 槽): 单参数那处疑似"按 modifiedShape 摆放该元素"; 无参那处疑似"刷新/校验容器"。
- 第一趟中 `if (rax == 0) continue` 出现在 `CheckShapePlacement` 之后(返回 false → 试下一个候选, 见 §4 步骤 C)。

---

## 4. `FindSpot(inventory, item, movingItems, itemLayers, fromEnd)` — 独立导出形态

证据: 头 `InventorySortHelper.txt:1325`; ISIL `:1447-1575`。与 §3.2 内联版本**逐条同构**, 差异只有"不写 itemLayers、不改物品、返回形状"。

```
A. if (inventory == null || item == null) throw;                                  // :1469-1473
B. var baseShape = item.modifiedShape;  var b = new GridShapeBuilder(baseShape);  // :1474-1483
   var grid = inventory.inventoryShape;                                           // :1471 (+0x1B0)
   int maxX = grid.width  - b.globalWidth;  int maxY = grid.height - b.globalHeight;   // :1486-1503
   if (maxX < 0 || maxY < 0) return null;                                         // :1505-1508 (JumpIfSign {126} → rax=0)
C. for (int ic = 0; ic <= maxY; ic++)  { y = fromEnd ? maxY-ic : ic;
     for (int jc = 0; jc <= maxX; jc++) { x = fromEnd ? maxX-jc : jc;
        b.SetPosition(x, y);                                                      // :1531
        if (FitsOccupancy(b, itemLayers)                                             // :1535
            && inventory.<CheckShapePlacement 家族虚调用>(item, b, movingItems))     // :1543  (第三参数 = movingItems)
            return b.???;   // builder→GridShape  (同 §5.4)                          // :1555-1557
     } }
D. return null;                                                                   // :1571-1574
```
- **返回 null 表示"找不到位置"**; 否则返回一个**新 GridShape**, 其 `minX/minY` = 选中的 `x,y`(全局最小角), 其它属性(翻转/朝向/数据)继承自 `item.modifiedShape`。
- `FindSpot` 自身**不修改** `itemLayouts`/物品(与 `Sort` 内联版的唯一差别就是调用方补做 `MarkOccupied` + 记录)。
- `CheckShapePlacement` 家族: 参数 `(GameItem, GridShape, List<GameItem> ignoredItems)` 与 `GameInventory.CheckShapePlacement` 声明(`GameInventory.txt:2138`, 覆盖版 `GameGridInventory.txt:4961`, `GameGridScrollableInventory.txt:5128`)逐项对应(builder 本身就是 GridShape, §1) → **几乎确定是该方法**, 但 ISIL 中该调用是 `Call 0x180010A40, rcx=18`(编号 18 的虚/间接调用), **方法名未确证**。它决定"这个候选位置对该物品是否合法"(容器类型、锁定、分组规则等由各自覆盖版决定)。

---

## 5. `itemLayers`、`FitsOccupancy`、`MarkOccupied`

### 5.1 `itemLayers` 的真身: `byte` 的 rank-2 数组, 维度 `[宽, 高]`

证据 `InventorySortHelper.txt:968-984`(ISIL 119-137):

```
119 r8 = 闭包.inventoryShape              ; :1033
122-125 rax = Field(8, GridShape, grid)   →  rax = grid.width      (int)
129-131 rax = Field(9, GridShape, grid)   →  rax = grid.height     (int)
132     stack:0xB8 = width (8 字节)
134     stack:0xC0 = height (8 字节)
135-137 new 数组(类型槽 0x1825D66A0, 维度缓冲=&stack:0xB8)          ; :982-983
```
- 元素类型: interop 元数据把形参降级成 `Il2CppObjectBase`(Il2CppInterop 不支持多维数组类型), 而 ISIL 表达式里该类型被渲染成 `typeof(System.Byte[2])`(同一渲染风格见 `SpaceInvadersText.txt:370` 的 `typeof(System.Boolean[2])`, 那里同一段代码随后 `arr[x][y]` 访问且用 `dim0/dim1` 双重边界 — 是 rank-2 数组)。
- **内存判据(决定性)**: `FitsOccupancy` 的访问是 `itemLayers + 0x20 + x*dim1len + y`, 且边界用 `dim0len=[bounds+0]`、`dim1len=[bounds+0x10]` 与 `x`、`y` 分别比较(`InventorySortHelper.txt:1709-1726`)——**jagged `byte[][]` 不会有这种 bounds 结构, 只有多维(rank-2)数组才有**。
- 结论: 逻辑模型 = `byte[,] layers = new byte[gridWidth, gridHeight]`, 访问 `layers[x, y]`(x 是第一维/列, y 是第二维/行; 平铺下标 `x*height + y`)。
- 单元值 = **该格各"层"的占用位掩码**, `0` = 全空。

### 5.2 `FitsOccupancy(shape, itemLayers)` — 证据 头 `:1577`, ISIL `:1661-1743`

```
if (shape == null) throw;                                        // :1675-1676
if (shape.minY > shape.maxY) return true;                        // :1677-1688 (编号 1,5) 空形状 → 通过
if (shape.minX > shape.maxX) return true;                        // :1689-1700 (编号 0,4)
for (int y = shape.minY; y <= shape.maxY; y++)                    // 注意: 外层 y, 内层 x
  for (int x = shape.minX; x <= shape.maxX; x++) {
      byte v = shape.Get(x, y);            // 编号 13, ★全局坐标     // :1701-1707
      if (v == 0) continue;              // 形状在该格不存在 → 跳过  // :1707-1708
      if (x >= layers.dim0 || y >= layers.dim1) throw <越界异常>;  // :1709-1716 (0x180215620; 具体异常类型未确证)
      int bit = 1 << (v & 31);                                        // :1717-1723
      if ((layers[x, y] & bit) != 0) return false;   // 该层已被占      // :1724-1726
  }
return true;                                                          // :1739-1740
```
- **只在形状单元非零时检查**(形状的空洞/包围盒内未覆盖格不参与), 判据是**位与**, 不是布尔占用。
- 形状单元字节 -> 位号 = `v & 31`(普通单元 `v=1`(见 `SetDataFill` 默认 `b=1`: `GridShapeBuilder.txt:1146`) → 位 `1<<1 = 2`)。
  即"形状单元字节"= 该格占用哪一层; 层语义(为什么是 1 而不是 0)未在本 dump 中给出文档 → **层号的语义未确证**, 但位运算机制确证。
- 因为 `Sort` 已把候选位置夹在 `[0, w-globalWidth] × [0, h-globalHeight]`, 实际不会触发越界异常。

### 5.3 `MarkOccupied(itemLayers, shape)` — 证据 头 `:1745`, ISIL `:1820-1893`

```
if (shape == null) throw;                                        // :1835-1836
if (shape.minY > shape.maxY) return;                             // :1837-1848
if (shape.minX > shape.maxX) return;
for (y = minY..maxY) for (x = minX..maxX) {
    byte v = shape.Get(x, y);                                    // :1861-1868 (编号 13)
    if (v == 0) continue;                                        // :1868-1869
    ref byte cell = ref AddressOfElement(itemLayers, x, y);       // :1872-1875 (Call 0x180010970)
    int bit = v & 31;
    cell |= (byte)(1 << bit);      // "bts edx,ecx" → 置位         // :1876-1880
}
```
- **与 `FitsOccupancy` 对称**: 同一遍历顺序(外 y 内 x)、同一 `Get(x,y)`、同一 `v==0` 跳过、同一 `1 << (v&31)` 位。
- `MarkOccupied` **不做边界检查**(越界即 UB) — 依赖调用方把位置夹在容器内。
- 因此 `itemLayers` 的语义是"占位图": `FitsOccupancy` 查位、`MarkOccupied` 置位。

### 5.4 `builder → GridShape` 的那一步(`0x180765E00`)

`Sort:1100` 与 `FindSpot:1555` 都把 builder 传进去取回一个 `GridShape`(随后 `MarkOccupied`/返回)。该地址对应 `GridShapeBuilder` 上的成员: 候选 `Build()`(`GridShapeBuilder.txt:1951`, 内部 `new GridShape` + 按 `width*height` 分配 `byte[] data` 并拷贝)或 `get_shape()`(`GridShapeBuilder.txt:215-223`, 直接 `return this`)。**两者之一是 `0x180765E00`, 未确证哪一个**; 对算法语义无影响(返回的都是"带该位置的形状"), 但自实现时应等价于"取 builder 当前状态形成的形状", 不复制共享可变状态。

---

## 6. `CellCount(shape)` — 证据 头 `:1895`, ISIL `:1952-2007`

```
if (shape == null) throw;                          // :1968-1969
int n = 0;
for (int y = 0; y < shape.height; y++)              // 编号 9   :1971-1976
  for (int x = 0; x < shape.width; x++) {           // 编号 8   :1979-1984
      if (shape.GetLocal(x, y) != 0) n++;           // 编号 11  :1985-1992
  }
return n;                                           // :1999-2006
```
- 语义 = **形状数据里非零单元的个数**(局部坐标 `x<width, y<height`)。
  不是包围盒面积, 也不是"全局网格里占的格数"(对 L 形/带洞形状, 二者不同)。
- 注意用的是 `GetLocal`(编号 11, 局部坐标), 不是 `Get`(编号 13, 全局坐标) — 因此与物品当前摆放位置无关, 是物品形状的固有属性。

---

## 7. 排序键(`<>c__DisplayClass2_0.<Sort>b__0`)—— 决定"谁先拿好位置"

证据 `InventorySortHelper_NestedType___c__DisplayClass2_0.txt:14-226`(ISIL `:118-224`):

```
int cmp(GameItem a, GameItem b) {
    int ca = CellCount(a.modifiedShape);        // :020-023   (a+0x1A0 = modifiedShape)
    int cb = CellCount(b.modifiedShape);        // :026-029
    if (ca != cb) return __this.bigFirst ? cb.CompareTo(ca)      // bigFirst=true  → 占格多者先
                                         : ca.CompareTo(cb);     // bigFirst=false → 占格少者先
                                                 // :031-039, :098-105(分支), :087
    int s = string.Compare(a.identifier, b.identifier, StringComparison.Ordinal);   // :033-037, :155
    if (s != 0) return s;                       //    (a+0x1C0 = identifier, GameItem.txt:402-410)
    int pa = a.modifiedShape.minY * __this.inventoryShape.width + a.modifiedShape.minX;   // :040-063
    int pb = b.modifiedShape.minY * __this.inventoryShape.width + b.modifiedShape.minX;   // :066-086
    return pa.CompareTo(pb);                    // :087  (行主序全局下标; width 用 inventoryShape.width)
}
```
要点:
- 主键 = **占格数**(不是宽高、不是面积、不是价值)。
- 次序键 = `identifier` 的**序数**比较(不是本地化名 `name`)。
- 末键 = **物品当前在网格中的行主序下标**(`y*容器宽 + x`) — 只影响"同尺寸同 identifier"的物品谁先动(注意: 排序过程中物品位置尚未改变, 该键在一趟内是固定的)。
- `List.Sort` 不是稳定排序, 但末键基本等价于"原格序", 因此结果接近稳定(`:964`)。

---

## 8. UI 入口链 —— 哪个按钮 = 哪组 `(bigFirst, fromEnd)`

### 8.1 按钮对象与节点名

`ItemContextHandler.OnEventInit`(头 `ItemContextHandler.txt:117`)里 4 个排序按钮被创建并命名(证据 `:1693-1748`):

| 实例字段 | 字段偏移 | 节点名 | 标签 key |
|---|---|---|---|
| `sortBigStart`  | `+0xA8` | `nodeButtonSortBigStart` | `handler_btn_sort_big` |
| `sortSmallStart`| `+0xB0` | `nodeButtonSortSmallStart` | `handler_btn_sort_small` |
| `sortBigEnd`    | `+0xB8` | `nodeButtonSortBigEnd` | `handler_btn_sort_big_end` |
| `sortSmallEnd`  | `+0xC0` | `nodeButtonSortSmallEnd` | `handler_btn_sort_small_end` |

证据: 字段加载 `:1577`(r13=[+0xA8]), `:1594`(rsi=[+0xB0]), `:1611`(r15=[+0xB8]), `:1629`(r12=[+0xC0]); 命名 `:1703`(r13→BigStart), `:1717`(rsi→SmallStart), `:1731`(r15→BigEnd), `:1745`(r12→SmallEnd); 标签 key `:2462-2484`(0xA8→`handler_btn_sort_big`, 0xB0→`…_small`, 0xB8→`…_big_end`, 0xC0→`…_small_end`)。
> 用户所称右键菜单里的「优先」即这组按钮的本地化文案; **具体本地化文本未确证**(dump 中没有本地化表, 只找到 key)。

### 8.2 点击 → `Sort` 参数

`TriggerButton(RichTextElement button)`(头 `:3622`; 排序分支 `:3978-4009`):

```
if (this.sortEnabledFlag(+0xCC) == 0) return;                     // :3978-3979  (CanSort 的结果, §8.3)
rdi = button;
rax = this.sortBigStart(+0xA8);
bool bigFirst = (rdi == rax)                                      // :3983-3984 → true
             || (rdi == this.sortBigEnd(+0xB8));                   // :3987-3988 → true
bool fromEnd  = (rdi == this.sortBigEnd(+0xB8))                    // :3997-3998 (rsi 初值 1)
             || (rdi == this.sortSmallEnd(+0xC0));                 // :3999-4000
if (button 不属于这 4 个) return;                                   // :3989-3990
InventorySortHelper.Sort(inventory /*+0x40*/, bigFirst, fromEnd);  // :4005-4009
```

映射(结论):

| 按钮 | bigFirst | fromEnd | 行为 |
|---|---|---|---|
| BigStart `+0xA8`   | `true`  | `false` | 大件优先, 位置从起点/角 `(0,0)` 开始找 |
| SmallStart `+0xB0` | `false` | `false` | 小件优先, 从起点开始找 |
| BigEnd `+0xB8`     | `true`  | `true`  | 大件优先, 从末尾 `(maxX,maxY)` 开始找 |
| SmallEnd `+0xC0`   | `false` | `true`  | 小件优先, 从末尾开始找 |

补充: `Sort` 的**返回值在 UI 侧被忽略** —— `ItemContextHandler.txt:4009` 调用后直接走收尾(`:4010-4019`), 没有对 `rax` 的分支; 因此原生排序失败(返回 false, 例如某物品形状比容器还大, §3.2)对玩家表现为"点了没反应"。

### 8.3 何时可用 / 何时显示

`ItemContextHandler.UpdateConditions`(头 `:2017`; 排序段 `:4474-4556`):

```
rsi = <当前容器>;
if (!InventorySortHelper.MaySort(rsi)) → 隐藏整个排序区(跳至 :260 分支);   // :4499-4501
InputActionManager.instance.ResetAfter(this);   // :4509
this.sortWindow(+0x38) = null;  this.sortInventory(+0x40) = rsi;          // :4515-4523
this.sortEnabledFlag(+0xCC) = InventorySortHelper.CanSort(rsi);           // :4524-4527
SetSortButtonColor(4 个按钮, isEnabled = sortEnabledFlag);                 // :4530-4553
EqualizeSortButtonWidths();                                                // :4556
```
→ `MaySort` = **是否提供排序入口**; `CanSort` = **入口里 4 个按钮是否可点**(`TriggerButton` 用 `+0xCC` 再兜一次, §8.2)。

---

## 9. 规格可实现的伪代码(汇总)

```csharp
// 前提: inventory != null, inventoryShape = inventory.Shape(容器形状), items = 容器内物品(含 GameItemElement)
static bool Sort(GameGridInventory inv, bool bigFirst, bool fromEnd) {
    if (!CanSort(inv)) return false;
    var grid = inv.inventoryShape;                       // GridShape
    byte[,] layers = new byte[grid.width, grid.height];  // [x, y]
    var order = new List<GameItem>(inv.Items);
    order.Sort((a, b) => {                               // §7
        int ca = CellCount(a.modifiedShape), cb = CellCount(b.modifiedShape);
        if (ca != cb) return bigFirst ? cb.CompareTo(ca) : ca.CompareTo(cb);
        int s = string.Compare(a.identifier, b.identifier, StringComparison.Ordinal);
        if (s != 0) return s;
        return (a.modifiedShape.minY * grid.width + a.modifiedShape.minX)
              .CompareTo(b.modifiedShape.minY * grid.width + b.modifiedShape.minX);
    });
    var placed = new List<(GameItem, GridShape)>();
    foreach (var item in order) {
        var b = new GridShapeBuilder(item.modifiedShape);          // 保留翻转/朝向, 只改位置
        int maxX = grid.width - b.globalWidth, maxY = grid.height - b.globalHeight;
        if (maxX < 0 || maxY < 0) return false;                    // 形状比容器大 → 整次失败
        bool found = false;
        for (int ic = 0; ic <= maxY && !found; ic++) {
            int y = fromEnd ? maxY - ic : ic;
            for (int jc = 0; jc <= maxX && !found; jc++) {
                int x = fromEnd ? maxX - jc : jc;
                b.SetPosition(x, y);                               // 锚点 = 形状 min 角
                if (!FitsOccupancy(b, layers)) continue;           // §5.2
                if (!inv.CheckShapePlacement(item, b, order)) continue;  // §4 步骤 C(名字未确证)
                var shape = b.Build();                             // §5.4(?)
                MarkOccupied(layers, shape);                       // §5.3
                placed.Add((item, shape));
                found = true;
            }
        }
        // 候选穷尽仍无解 → 原生直接 return false 中止整次排序 (:1131-1137 → {285} → {457})
    }
    foreach (var (el, shape) in placed) {                          // §3.3
        if (el is GameItemElement e && e.IsDestroyed()) continue;
        el.modifiedShape = new GridShapeBuilder(el.modifiedShape).SetTransform(shape);
        inv.<按修改后的形状摆放元素>(el);                            // 原生 vtable [klass+0x268]
    }
    inv.<刷新/校验容器>();                                          // 原生 vtable [klass+0x2B8]
    return true;
}

static int CellCount(GridShape s) {                                // §6
    int n = 0;
    for (int y = 0; y < s.height; y++) for (int x = 0; x < s.width; x++)
        if (s.GetLocal(x, y) != 0) n++;
    return n;
}

static bool FitsOccupancy(GridShape s, byte[,] layers) {           // §5.2
    if (s.minY > s.maxY || s.minX > s.maxX) return true;           // 空形状
    for (int y = s.minY; y <= s.maxY; y++)
      for (int x = s.minX; x <= s.maxX; x++) {
        byte v = s.Get(x, y);
        if (v == 0) continue;
        if (x >= layers.GetLength(0) || y >= layers.GetLength(1)) throw new Exception();  // 越界(原生为 0x180215620)
        if ((layers[x, y] & (1 << (v & 31))) != 0) return false;
      }
    return true;
}

static void MarkOccupied(byte[,] layers, GridShape s) {            // §5.3(原生无边界检查)
    if (s.minY > s.maxY || s.minX > s.maxX) return;
    for (int y = s.minY; y <= s.maxY; y++)
      for (int x = s.minX; x <= s.maxX; x++) {
        byte v = s.Get(x, y);
        if (v == 0) continue;
        layers[x, y] |= (byte)(1 << (v & 31));
      }
}
```

---

## 10. 结论: InventorySorter 应当「直接调用原生 / 照抄语义自实现 / 混合」

### 10.1 原生是否可调用(实测)

反射游戏 interop 程序集 `MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`(与 mod 编译环境同源, csproj 引 `Il2CppInterop.Runtime`):

```
=== Il2Cpp.InventorySortHelper public=True abstract=True sealed=True
  public static Boolean MaySort(GameGridInventory inventory)
  public static Boolean CanSort(GameGridInventory inventory)
  public static Boolean Sort(GameGridInventory inventory, Boolean bigFirst, Boolean fromEnd)
  public static GridShape FindSpot(..., Il2CppObjectBase itemLayers, Boolean fromEnd)
  public static Boolean  FitsOccupancy(GridShape shape, Il2CppObjectBase itemLayers)
  public static Void     MarkOccupied(Il2CppObjectBase itemLayers, GridShape shape)
  public static Int32    CellCount(GridShape shape)
（字段名编码的原始可见性: MaySort/CanSort/Sort = Public_Static;FindSpot/FitsOccupancy/MarkOccupied/CellCount = Private_Static）
```

- **可编译调用签名(C# / Il2CppInterop)**:
  ```csharp
  using Il2Cpp;                                   // interop 命名空间
  if (InventorySortHelper.MaySort(inv) && InventorySortHelper.CanSort(inv))
      InventorySortHelper.Sort(inv, bigFirst: true, fromEnd: false);
  ```
- 三个 public 方法**可以直接调**。`FindSpot/FitsOccupancy/MarkOccupied/CellCount` 原生 private, interop 桩是 public, 但 `itemLayers` 形参被降级成 `Il2CppObjectBase`(多维数组 Il2CppInterop 不生成强类型) → 想在 mod 里调 `FindSpot/FitsOccupancy`, 需要自己构造 IL2CPP 侧 rank-2 `byte[,]` 并转成 `Il2CppObjectBase`, **不推荐**。
- **线程**: `Sort` 会走 Unity 对象路径(元素摆放 + 容器刷新虚调用, §3.3), 必须在 **Unity 主线程**调用(未确证其内部是否自己 post 到主线程, 从代码看是同步执行, 无协程/await)。
- **是否依赖 UI 状态**: 只依赖 `inventory.inventoryShape`(容器形状字段 +0x1B0)与 `item.modifiedShape`(+0x1A0); **不依赖** ItemContextHandler 的任何按钮/选中态(`bigFirst/fromEnd` 由调用方给)。但"元素摆放"那两处虚调用多半会触发渲染/动画, 因此仍应有 UI 侧准备好(容器已被渲染)。
- **它会做什么副作用**: ①不改动容器物品列表顺序; ②覆写每个物品的 `modifiedShape`; ③调用容器虚方法逐元素摆放 + 收尾刷新; ④不写盘、不消耗物品。*(①—③ 有证据 §3.3; ④ 未确证但代码路径里没有存档/消耗调用。)*

### 10.2 三选一建议: **混合(首选直接调用原生 `Sort`, 自实现仅作兜底/预览)**

理由(按权重):
1. **可行且最省**: `Sort` 是 public static, 签名就是 3 个参数, 无需 md 数组、无需反射私有成员 → 直接调即可得到与游戏 100% 一致的结果, 包括难以复刻的部分: 容器 `CheckShapePlacement` 虚分派(`GameGridInventory` / `GameGridScrollableInventory` 各有覆盖: `GameGridInventory.txt:4961`, `GameGridScrollableInventory.txt:5128`)、以及 UI 侧元素摆放虚调用。自实现若漏掉覆盖版差异, 会在"分组摆放 `SupportsGroupPlacement`"等容器上产生偏差。
2. **自实现的成本/风险**: 算法本身已完全规格化(§9 约 100 行), 但两个"不可见依赖"必须自造 —— 容器合法性检查(§4 步骤 C 的方法名至今未确证)与元素摆放/刷新(两处 vtable 槽未确证); 这两者恰恰是**与游戏 UI/容器类型强耦合**的部分, 抄错会静默出错。
3. **何时改用自实现(兜底)**: 若目标游戏版本的 interop 程序集没有 `Il2Cpp.InventorySortHelper`(类型被裁/改名), 或需要"只计算不落位"(预览/干跑) —— 此时按 §9 实现, 并把 `CheckShapePlacement` 换成容器自带的对应公开方法(优先 `GameInventory.CheckShapePlacement(GameItem, GridShape, List<GameItem>)`)。
4. **不建议"纯自实现"取代原生**: 除了上面的耦合风险, 还放弃了随游戏版本自动跟随的收益。

注意事项(给实现者):
- 调 `Sort` 前必须自己判断 `MaySort/CanSort`(否则原生内部会因 `CanSort` 失败直接返回 false, 静默无动作: `:925-927`)。
- `Sort` 内部的物品集合来自容器自身(虚拟成员), mod 不需要、也不应该传入自己的列表。
- `bigFirst/fromEnd` 四组合与游戏 UI 一致(§8.2), 若 mod 自定义菜单, 建议直接沿用这 4 个语义。
- 原生会写 `item.modifiedShape`: 若 mod 之前缓存过形状(如自绘), 需要在 `Sort` 之后重新读取。

---

## 11. 未确证清单(禁止当结论使用)

1. **`CheckShapePlacement` 家族调用的方法名**: `Sort`/`FindSpot` 里是 `Call 0x180010A40, rcx=18`(编号型间接调用), 参数与 `GameInventory.CheckShapePlacement(GameItem, GridShape, List<GameItem> ignoredItems)`(`GameInventory.txt:2138`)完全对应, 但**未确证**编号 18 就是它。
2. **元素摆放虚方法(vtable `[klass+0x268]/[+0x270]`, 单参数)** 与 **收尾虚方法(vtable `[klass+0x2B8]/[+0x2C0]`, 无参)** 的**方法名未确证**(只能确定是容器上的虚调用)。
3. **`builder → GridShape` 的成员**: `0x180765E00` 是 `GridShapeBuilder.Build()`(`GridShapeBuilder.txt:1951`)还是 `get_shape()`(`:215`)之一, **未确证**。
4. **单元字节的"层号"语义**: 位运算机制确证(`1 << (v & 31)`), 但"1=第 1 层、0=不属于形状"的物理含义(是否对应堆叠/货架层)在本 dump 中无文档, **未确证**。
5. **本地化文案**: `handler_btn_sort_big/small/…_end` 对应的实际中文文本(用户提到的「优先」)未在 dump/仓库中找到, **未确证**(只有 key)。
6. **`MaySort`/`CanSort` 取物品集合的虚拟成员名**(候选 `GameGridInventory.get_childItems()`, `GameGridInventory.txt:65`)未确证。
7. **`string.Compare` 的分支极性细节**: 已确定是 `String.Compare(a.identifier, b.identifier, StringComparison.Ordinal)`(`InventorySortHelper_NestedType___c__DisplayClass2_0.txt:033-037`, `:155`), 但 ISIL 中该调用的 `StringComparison` 值来自 `lea r8d,[r9+4]`(r9d=0) → 4 = `Ordinal`; 常量到枚举名的映射按 .NET 约定判定, **低风险未确证**。
8. **`MarkOccupied` 越界行为**: 原生无边界检查(§5.3), 推测依赖调用方夹取位置(§3.2), 若有人以越界形状直接调用 → 未定义行为(未确证是否有上层保护)。
9. **线程**: 未在代码中发现显式主线程调度, "必须主线程"是按 Unity 对象访问惯例的判定(§10.1), **未确证**。
10. **越界异常的具体类型**: `FitsOccupancy` 用 `0x180215620` 抛异常(区别于 `0x180215630` 的空引用), 具体异常类型 **未确证**。

---

## 12. 证据索引(按主题)

| 主题 | 文件:行 |
|---|---|
| `MaySort` | `InventorySortHelper.txt:51-96`(ISIL), 头 `:3` |
| `CanSort` | `InventorySortHelper.txt:240-381`, 头 `:98` |
| `Sort` 头/准备 | `InventorySortHelper.txt:383`, `:846-993` |
| `Sort` 第一趟(找位/落位/记录) | `InventorySortHelper.txt:1007-1137` |
| `Sort` 第二趟(写回/摆放/刷新) | `InventorySortHelper.txt:1145-1286` |
| `FindSpot` | `InventorySortHelper.txt:1325`(头), `:1447-1575` |
| `FitsOccupancy` | `InventorySortHelper.txt:1577`(头), `:1661-1743` |
| `MarkOccupied` | `InventorySortHelper.txt:1745`(头), `:1820-1893` |
| `CellCount` | `InventorySortHelper.txt:1895`(头), `:1952-2007` |
| 比较器 `b__0` | `InventorySortHelper_NestedType___c__DisplayClass2_0.txt:14-226` |
| GridShape 成员编号/属性 | `GridShape.txt:3-72`(抽象属性), `:118-127`(GetOutsideBounds→GetLocal(-1,-1)) |
| GridShapeBuilder | `GridShapeBuilder.txt:43-78`(globalWidth/Height), `:215-223`(get_shape), `:612-618`(SetPosition), `:664+`(SetTransform), `:1146`(SetDataFill 默认 1), `:1296`(SetDataOutside), `:1951`(Build) |
| GameItem 偏移 | `GameItem.txt:270-291`, `:293-297`, `:402-423`, `:438-451`, `:458-479` |
| 容器声明 | `GameInventory.txt:2138`(CheckShapePlacement), `:2646`(IsInsertLocked); `GameGridInventory.txt:265-287`(inventoryShape), `:4961`(覆盖版), `GameGridScrollableInventory.txt:5128` |
| UI 按钮/节点名/标签 | `ItemContextHandler.txt:1577-1748`, `:2462-2484`, `:3978-4009`, `:4474-4556` |
| rank-2 数组渲染旁证 | `SpaceInvadersText.txt:360-405`(`typeof(System.Boolean[2])` + `[x][y]` + bounds) |
| interop 权威签名 | 反射 `D:/steam/steamapps/common/Probably Stolen Demo/MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`(§10.1 输出) |
