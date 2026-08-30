# xmod 逆向产物备份

来源：游戏目录 `BepInEx/plugins/xmod/ProbablyStolenPlaytest.dll`（付费 ModFramework mod），
2026-08-31 因与 MelonLoader 7 框架冲突已从游戏目录删除，此处为纯数据/代码备份。

## 文件

| 文件 | 说明 |
|------|------|
| `item-catalog.json` | 物品目录（397 记录：stableId/kind/directory/nameZh/nameEn/description/categories/itemTypes），提取自 DLL 内嵌资源 `.Resources.item-catalog.json` |
| `items_data.js` | item-catalog.json 转 JS 数组（供网页用） |
| `items_browser.html` | 物品浏览器网页（分类筛选+搜索+卡片+详情，引用 items_data.js） |
| `decomp/` | ilspycmd 反编译源码（混淆类名 b7tdMvJlthvEYSFpZX 等） |

## 关键逆向结论（生成物品逻辑）

`TeJZ2ELr25KIGhra8Tx.cs`（CreateItem，rC5LNitnrc）：
```csharp
GameItem item = DirectoryMaster.Item(stableId, true);   // 稳定ID创建
// 可选: Q93LmMNTQG(item, itemId) 本地化文本覆盖（non-mandatory）
if (!inventory.MayHaveValidInventorySlot(item)) break;  // 校验槽位
if (!inventory.UncheckedAccept(item)) break;            // 入包
// 数量 = 循环调用 Item N 次
```
主背包 = `EmporiumEntry.Instance.invElement`（GameGridInventory，转 GameInventory）。
DLL 内嵌资源：`item-catalog.json` / `localization-overrides.json` / `description-overrides.json`。

ProgressMod 已实现 `SpawnItem(stableId, count)` 复刻此逻辑。
