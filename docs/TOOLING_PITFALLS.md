# PlayerStore 工具链与踩坑台账

日期: 2026-09-25
范围: 本仓库（`D:\git\PlayerStore`）反编译 / 检索 / 构建 / 验证 全链路
目的: 沉淀**会重复踩**的工具坑，特别是「不报错但给出错误结论」的隐性坑 —— 后者比报错危险得多。

游戏路径（本文档内一律用此值）:
`D:\steam\steamapps\common\Probably Stolen Demo`
> 注: `docs/SESSION_HANDOFF_2026-09-05.md` 与 `InventorySorter/tscripts/parse_dump.py` 里的
> `Probably Stolen Playtest` 是**过期路径**，勿再沿用。

---

## 0. 数据来源分级（先看这张表再动手）

| 想要什么 | 去哪拿 | 不要去哪 |
|---|---|---|
| 方法**逻辑** | `dump/cpp2il_isil/IsilDump/<Asm>/*.txt`（ISIL） | ascs —— 全是空壳 |
| 类型**签名** | `dump/ascs/*.cs` | ISIL —— 噪音太大，签名反而难读 |
| 物品 stableId | 运行时 `DirectoryMaster.GetIdentifierList<T>()` | 静态表 —— 必然滞后 |
| 物品中英文名 | Unity Localization bundle（见 §1.3） | 程序集 —— **里面没有** |
| 类型是否存在 | 直接在 `dump/ascs/` 看文件在不在 | grep 旧文档 —— 文档会过期 |

**中英文名不在程序集里**，这条曾反复误导：`Assembly-CSharp.dll` 与 `global-metadata.dat` 中
`item_*_name` 命中数均为 **0**。真源是
`Probably Stolen_Data/StreamingAssets/aa/StandaloneWindows64/localization-string-tables-<locale>_assets_all.bundle`
（UnityFS + zlib，中文 154KB / 英文 147KB，需 UnityPy 解包）；现成产物见
`mod/item_catalog.json`（441 条 `{id, english, chinese, aliases[], source_key}`）。

---

## 1. 反编译链（dump 重建）

### 1.1 三段管线

```
Cpp2IL --output-as isil          → dump/cpp2il_isil/IsilDump/<Asm>/*.txt   ← 唯一有方法体的地方
Cpp2IL --output-as dll_throw_null → dump/cpp2il_il/*.dll
      └─ ilspycmd -p -o dump/ascs  → dump/ascs/*.cs                      ← 只有签名，方法体 throw null
```

`Cpp2IL.exe` = `MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/Cpp2IL.exe`（2022.1.0-pre-release.21）
`ilspycmd` = `/c/Users/11/.dotnet/tools/ilspycmd`（10.1.1.8388）

完整命令见 §8。

### 1.2 两条硬规则

- **必须用 `dll_throw_null`，不能用 `dll_default`。**
  `dll_default` 会把方法体变成 `return null/0/false` 默认值，与既有文档的行号引用、以及
  「ascs 是纯签名」这个前提全部冲突。
- **Unity 模块 DLL 要补。** Cpp2IL 只产出 94 个 dll，旧 dump 有 121 个 —— 差额 27 个是
  `UnityEngine.*Module.dll`，须从 `MelonLoader/Il2CppAssemblies/` 拷入（`cp -n` 保留已存在），
  否则 ascs 的 csproj（22 个 HintPath 指向 `..\cpp2il_il\`）引用解析失败。

### 1.3 值得知道的产物事实

- 刷新一次约 1 分钟 / 94 个程序集；ISIL 约 258M / 1520 个 txt；ascs 约 643 个 .cs。
- `dump/` 被 `.gitignore` 第 21 行**整体忽略**（`git ls-files dump/` = 0），刷新不影响仓库。
- 刷新前后做 diff 有诊断价值：本次刷新发现 ISIL 里 8086 个文件 differ **全是地址重定位**
  （`cmp byte ptr [18274E916h],0` → `[18274F958h]`，行数与方法头一致），据此判定
  「9/18 更新未触碰水系统」—— 这类结论靠肉眼读是得不出来的。

---

## 2. 读 ISIL 的正确姿势

### 2.1 文件结构

以 `^Method: ` 分块。每块 = 签名行 + `Disassembly:`（x64 原生）+ `ISIL:`（托管 IL，带 op 编号）。

### 2.2 必须跳过开头噪音

每个方法开头 20–115 行全是：

```
il2cpp_codegen_initialize_runtime_metadata
il2cpp_runtime_class_init_export
il2cpp_codegen_object_new
```

真实逻辑在 `JumpIfNotEqual {121}`（初始化短路跳转）之后。

过滤正则：**排除**上述三类，**保留** `Call ` / `Move r.., ".."` / `JumpIf` / `Compare`。

### 2.3 一个实际收益

靠 ISIL 读出了 `WaterHelper.AddWater` 的调用参数（`Move rdx,1` / `Move r8,0x98967F`）与
整条 `grade` 阶梯（0..5 对应纯水/HQ/base/ghost/rust/gutterflow），从而确认「水龙头上限是
高品质水、永远接不出纯水」。这类结论**只能从 ISIL 拿到**。

### 2.4 UTF-16LE 陷阱

反射目标名（如 `GetIdentifierList`）在 DLL 中以 **UTF-16LE 嵌在 #US 堆**。
以 UTF-8 视角 grep 报 0 命中 ≠ 不存在 —— 需按 UTF-16 计数确认。

---

## 3. 检索链

| 工具 | 用途 | 位置 / 状态 |
|---|---|---|
| `codegraph explore "<符号或问题>"` | 调用链、跨文件关系 | **旧 `.codegraph/` 已删（索引失效），需重建** |
| codebase-memory | 符号图 `search_graph` / `trace_path` | 项目 `D-git-PlayerStore` 已索引；**覆盖不全，结论须核对** |
| ast-grep | 形状检索、批量审计 | 0.42.3 @ `/c/Users/11/AppData/Local/Programs/Python/Python314/Scripts/sg` |
| fastctx | 命令入口 / 读写搜 / PTC 组合 | 主入口 |

---

## 4. 构建与发布链

```bash
dotnet build -c Release PlayerStore/ProgressMod.csproj
```

- csproj 的 `OutputPath` **直指游戏 Mods 目录**（`$(GameDir)\Mods\`，`AppendTargetFrameworkToOutputPath=false`）
  ⇒ Release 构建即自动部署。
- csproj 有 `AutoCommit` target（`AfterTargets=Build, Condition=Debug`）会自建**噪声提交**，
  收尾需 `git reset --soft` 压掉。
- **必须带 `-c Release`。** 历史上手工发布漏过该参数（默认 Debug）导致产物错误；
  仓库无发布脚本、纯手工 ⇒ 无自动化闸门。核验产物尺寸/sha256 是唯一防线。
- 发布：`gh release create v<ver> <tgz>`。
- GitHub Pages 源 = **master 分支 / `docs/` 目录** ⇒ 离线访问拿不到 `/api/*`，
  前端必须**静默降级**为纯静态。

---

## 5. 验证链

两条路，缺一不可：

1. **mock 服务端**：`run_background` 起 Node，模拟游戏 HTTP API 供前端独立测试（绝对路径，见 §6.2）。
2. **Playwright 无头浏览器**：走真实 DOM。可断言「失效卡内生成按钮数 = 0」这类
   只有渲染后才成立的事实。

真机路径直连游戏内 Mod 服务（端口 **26880**）。离线降级单独用另一个端口（只服务静态、`/api` 返 503）验。

收尾必做：`job_kill` 所有 mock 作业 + 确认端口释放，否则会挡住游戏侧 Mod 服务。

---

## 6. 工具失败台账

### 6.1 MCP 参数序列化

| 现象 | 报错 | 绕法 |
|---|---|---|
| `fastctx__grep` 经 PTC 调用（glob/某些参数组合） | `binding arguments must be lossless JSON` | 改用 `fastctx__run` 跑 `grep -rn` |
| 传 `undefined` 作为参数值 | 同上 | 按需构造参数对象，不传空键 |
| `fastctx__run` 传 `timeoutMs` | `unknown field` 反序列化失败 | 参数名是 **`timeout_ms`**（下划线） |

### 6.2 MSYS 路径 vs 原生程序（本仓库最高频坑）

根因统一：**Git Bash 的 `/tmp`、`/d/...` 一旦交给非 MSYS 程序就失效。**

- `run_background` 的 `cwd` 不接受 MSYS 文案 → `Working directory does not exist: /tmp/fake26880`
- ast-grep 读规则文件 → `Cannot read rule file, os error 3`（`mktemp -d` 给的是 `/tmp/tmp.xxx`）
- `gh release create` 不认 MSYS `/tmp`
- Python 里 `/tmp` 解析为 `D:\tmp`
- `/d/git/...` 被拼成 `D:\d\git\...` → `can't open`；须 `cd` 到目录后用相对文件名

**对策**：给原生程序一律传 Windows 路径（`C:/Users/...`）或先 `cd` 用相对名。

### 6.3 ast-grep 0.42.3

- **`scan` 命中 exit 0、未命中 exit 1。** 曾把「没抛异常」当「没命中」，误报 35/35 规则全失效。
  **必须始终解析 stdout**，不能以异常与否判断。
- **C# 属性 `[HarmonyPatch(...)]` 文本 pattern 永远 0 命中** —— 它被解析为 C# 12 集合表达式
  （`collection_expression > collection_element > expression_element > invocation_expression`）。
  正解走 kind：
  ```yaml
  id: harmony-patch
  language: csharp
  rule:
    kind: attribute
    has: { kind: identifier, regex: ^HarmonyPatch$, stopBy: end }
  ```
- `run --pattern` 传多节点 → `Multiple AST nodes are detected`；多节点一律走 `scan --rule`。
- inline YAML 里 `\n` 未转义 → `Fail to parse yaml as RuleConfig`；且 **bash 双引号内 `\n` 不转义**
  ⇒ YAML 必须 heredoc 落盘成规则文件。
- **`--debug-query` 在 run 模式 dump 的是 pattern 自身的 AST，不是目标文件 AST** —— 极易误判。
- 语言 id 用 `csharp`（不是 `cs`）。
- 单属性行 `[X]` 在无类上下文时也会被吃成集合表达式。

### 6.4 写入工具

- `write` 对已存在文件要求先读 → `file has not been read`；绕法：shell heredoc 落盘。
- **`fastctx__replace` 的 replacement 仍解析 `$`** —— 且**在 `literal: true` 下同样如此**。
  实测写 shell 的美元+括号命令替换、美元变量、正则行尾锚（`\.cs$`）都会报
  `Replacement references an undefined capture group: ...`。
  更麻烦的是：**校验发生在双写还原之后** ⇒ 想落盘一个美元大括号插值字面量（JS 模板串那种），
  必须把那个美元写成四连；只写两连会被还原成单个美元、随后触发校验而拒绝整次写入。
  即「想看到 N 个美元，就写 2N 个」。
  ⇒ 结论：**先 `dry_run: true` 预览**是唯一稳妥流程；必要时改用 heredoc 落盘整文件。
  注意报错信息自身也含美元符号，引用它时同样要转义。
- TS 源码里反引号与 `${}` 冲突 ⇒ 用 `\u0060` 转义反引号 + `$$` 双写，或 heredoc 落盘脚本
  由 Node 精确替换。
- 压缩区间撞保护区被拒（保护区 = 最近若干条 + 最新用户消息）。

### 6.5 PTC / run_code

- **并行写同一文件有竞态** ⇒ 探针文件必须串行落盘，之后再并行读。
- 反引号被 bash 当命令替换执行 ⇒ release notes 内容残缺。
- `node -e "..."` 双引号内 `\\s` 被吃掉 ⇒ 正则误判「缺失」。
- `python -c` 传多行 → `SyntaxError: unexpected character after line continuation`
  ⇒ 应 base64 编码落盘再执行。

### 6.6 编码

- Git Bash 默认 GBK ⇒ 中文乱码 / `U+FFFD`。
  修：`export LC_ALL=C`，或 `sys.stdout.reconfigure(encoding='utf-8')`。
  工具报「invalid byte sequences shown as U+FFFD」时，重跑并显式传 `encoding="gbk"`。
- Python 在 MSYS 下 stdout 默认 GBK。

---

## 7. 「会导致错误结论」的坑 ⚠️ 最高优先级

这些**不报错**，但会把你引向错误判断：

| # | 坑 | 曾经导致的错误结论 | 正确做法 |
|---|---|---|---|
| 1 | Git Bash **没有 `strings`** | 扫 DLL 得到空输出 ⇒ 误判「程序集里没有该字符串」 | 用 Python 正则扫二进制；先确认命令本身能跑 |
| 2 | ast-grep `scan` 未命中 exit 1 | 误报 35/35 规则全失效 | 始终解析 stdout |
| 3 | codebase-memory **索引覆盖不全** | 据「查询 0 命中」断言代码不存在 | 核对覆盖范围；结论以直读源码为准 |
| 4 | `grep -c $'\r'` | shell 转义误报有 CRLF | 用 `grep -n` 复核（零命中） |
| 5 | `--debug-query` 语义 | dump 出 pattern AST 却当成文件 AST 分析 | 调试目标代码用 `format=cst` 扫文件 |
| 6 | `dump/ascs/` 是 `throw null` 空壳 | 据 ascs 判断「方法没做事」 | 读逻辑一律走 ISIL |
| 7 | 静态表 / 旧文档 | 拿 427 条旧表当现状 | 以运行时枚举 + 新 dump 为准 |
| 8 | IL2CPP 容器语义靠猜：泛型参数是条目类型而非目录类型 | 图鉴目录枚举**连错三轮**恒为 0 个（日志「[List] 目录 0 个, 物品 0 个」且无任何警告） | 见 §7.1：读签名 + 打印真实键；遍历 `directories` 的**值** |
| 9 | 拿单一来源当「全集」 | 枚举只拿到 2 个目录/191 项，**260 个仍存在且能正常生成的物品被误标「已失效」** | 见 §7.2：三源取并集，且**宁可漏标不误标** |
| 10 | 启动期用 `[HarmonyTargetMethods]` 批量 patch 29 个 IL2CPP 方法 | **游戏启动直接闪退**（日志停在挂载完成那行） | 见 §7.3：逐个显式声明且只挂必要的少数几个 |
| 11 | 判稳写「连续 N 轮无增长」但计数器在增长时**未清零**（实为累计） | 判稳过早 ⇒ 漏掉的目录被当成「已删除」⇒ 物品误标「已失效」 | 见 §7.4：增长即清零 + 数字必须自解释 |

### 7.1 目录枚举：连错三轮，每一轮都撞在不同的假设上

**正确写法（最终）—— 遍历字典的值，不查键：**

```csharp
// DirectoryMaster.directories : Dictionary<DirectoryEntry类型, List<目录实例>>
// 键 = DirectoryEntry 的**具体类型**，实测只有 GameItem 与 CombatAbilityEffect 两个；
// 值 = 该条目类型下注册的全部目录实例。
// 所有 ItemDirectory 都是 Directory<GameItem>，故**全部挂在 GameItem 这一个键下**。
foreach (var k in DirectoryMaster.directories.Keys)
foreach (var o in DirectoryMaster.directories[k]) {
    var d = o as Directory<GameItem>;              // 筛出物品目录
    if (d == null) continue;
    foreach (var p in d.factoryDictionary) ids.Add(p.Key);   // Key = stableId
}
```

签名早已写明键是什么：`AddDirectory<T>(Directory<T> directory) where T : DirectoryEntry`
—— **`T` 是条目类型，不是目录类型**。

三次失败各撞在不同假设上（都曾让结果恒为空且**零异常**）：

| 轮次 | 错误假设 | 实际 |
|---|---|---|
| 1 | 用 `GetIdentifierList<T>` 反射调用即可 | `Il2CppClassPointerStore<T>.NativeClassPtr` 对运行时 `T` 永不初始化 ⇒ 返回 null |
| 2 | 换成 `directories` + 记类型名即可 | Managed 全名带 `Il2Cpp.` 前缀，IL2CPP 侧命名空间为空 ⇒ `GetType` 恒 null |
| 3 | 剥掉前缀，按**目录类型名**查键 | 键是**条目类型**（`GameItem`），按目录名查永远查不到 |

**这一节真正的教训是方法论：**

- **读签名，别猜语义。** `AddDirectory<T>` 的 `where T : DirectoryEntry` 与属性类型
  `Dictionary<Il2CppSystem.Type, List<Il2CppSystem.Object>>` 都在 dump 里躺着，
  三轮错误全部源于「按名字像什么去猜它是什么」。**先看泛型参数约束与容器键值类型，
  这两处几乎从不撒谎。**
- **打印真实数据，不要推断。** 第 3 轮的突破完全来自一行日志
  `directories 注册 2 个键; 样例: GameItem | CombatAbilityEffect` ——
  在此之前的两轮都在用「应该是什么」推「为什么不是」。诊断代码要先做**枚举真实成员**这件事。
- **别在同一个盲区里连续修两次。** 第 1、2 轮都是「API 用错」，第 3 轮才去打印真实键。
  正确次序是：**先让失败可见（枚举真实结构），再选 API。**
- 启动早期该字典**会从 0 个键涨到 2 个键**（子目录是被游戏陆续注册的），
  所以「枚举为空」不能缓存，必须允许重试 —— 这一条在第 1 轮就该得出。

### 7.2 「存在性判定」必须多源取并集，且宁可漏标不误标

判断「某物品在当前游戏版本里是否存在」时, **不要拿单一来源当全集**。实测三个来源各不完整:

| 来源 | 数量 | 缺口原因 |
|---|---|---|
| 本地化表键 (`SharedTableData.Entries`) | 371 | 只覆盖有 `item_<id>_name` 翻译的物品 |
| 运行时目录 (`Directory.factoryDictionary`) | 191 | 只覆盖**启动时已注册**的目录; 实测启动早期字典里只有 2 个条目 |
| 目录注册字面量 (ISIL `Move r8, "<id>"`) | 215 | 机器件等走另一条注册路径 (键是拼接而非字面量) |

**后果**: 曾用 191 项那个源去对账, 把 260 个**仍然存在、且日志证明能正常生成**的物品
(`aug_chip` 等) 标成「已失效」——而 UI 会因此**禁用生成按钮**, 属硬故障。
反证来自日志: `[Spawn] 生成到主背包 aug_chip x1` 成功, 而枚举没返回它。

**两条纪律**:
- **宁可漏标, 不误标。** 误标「已失效」= 禁用功能 (硬故障); 漏标 = 少一个提示 (软缺陷)。
  故任一源命中即算存在。
- **不要缓存「非空但不完整」的结果。** 目录是被游戏**陆续注册**的 (实测注册表启动后
  从 0 个键涨到 2 个键), 早期枚举虽非空却残缺 —— 一旦缓存, 缺失项被永久误标。
  按**数量增长**判定是否稳定: 变大就继续重建, 连续 N 轮不变才停手。

**名称怎么来** (用户提示「游戏有翻译可以调用」): 游戏用 Unity Localization,
物品名在 `Item` 表的 `SharedTableData` 里。表名依据 `LocHelper.GetLocalizedItem` 的 ISIL
(`Move rcx, "Item"` 后 `Call LocHelper.Get`), 与 `LocSmartStringHelper` 静态构造里的表名列表。
用 `LocalizationSettings.StringDatabase.GetTable(tableRef, locale)` 逐语言取
`m_TableEntries`, 键经 `entry.SharedEntry.Key` 拿, 值取 `entry.LocalizedValue`。
这样一次能拿**全部语言**, 不像 `GetLocalizedItem` 只给当前语言。

### 7.3 启动期批量 Harmony patch 会闪退 —— 代价远高于收益

**症状**：游戏启动即死，日志停在 patch 完成的那一行，无 ERROR 无堆栈。

```
[08:53:27.792] AccessTools.DeclaredMethod: Could not find method for type
               Il2Cpp.ItemDirectory and name InitDirectory and parameters
[08:53:27.801] [ProgressMod] [List] InitDirectory 挂载 29 个 (预期 17)
               ← 之后进程死亡 (该 log 共 208 行)
```

同一份 dump/存档下，**不带该 hook 的版本跑满 1194 行并正常 `Preferences Saved!` 收尾** ——
这是坐实因果的对照实验。

**做法（错）**：想学 BrewingExpansion 用 `InitDirectory` 的 Postfix 当「目录注册完成」的精确信号：

```csharp
[HarmonyPatch]
public static class PatchDirInit {
    [HarmonyTargetMethods]                       // ✗ 反射批量挂 29 个
    public static IEnumerable<MethodBase> TargetMethods() {
        foreach (var ty in ...GetTypes())
            if (typeof(ItemDirectory).IsAssignableFrom(ty))
                list.Add(AccessTools.DeclaredMethod(ty, "InitDirectory"));
        return list;
    }
    public static void Postfix() { _dirInitCount++; }
}
```

问题不止一个：`Il2Cpp.ItemDirectory` 自身是 abstract（`DeclaredMethod` 取不到，已报警），
29 个方法一次性 patch；且这个 hook 的**全部收益只是让判稳早几秒**。

**纪律**：
- **启动期的 Harmony patch 必须逐个显式声明**，不用反射批量 —— 崩了没有堆栈，只能靠二分。
- **只挂确实需要的少数几个**。BE 只挂 `AmenitiesItemDirectory` / `MiscItemDirectory` /
  `FoodItemDirectory` 三个它真正要注册物品的目录，不是 29 个全挂。
- **收益必须配得上风险**。为「判稳早几秒」赌上启动稳定性是不划算的交易；
  宁可退回多等几轮的启发式判稳（见 §7.2 双计数）。
- 改启动期代码后**务必留存对照组日志**：这次能 30 秒定位，全靠上一次正常启动的 log 还在。

以下保留前两轮的错法与原因，供对照：

```csharp
// ✗ 坑一: 静态泛型 MakeGenericMethod —— 恒返回 null
HarmonyLib.AccessTools.Method(typeof(DirectoryMaster), "GetIdentifierList")
    .MakeGenericMethod(dirType).Invoke(null, new object[1] { null });
```

**为什么恒返回 null**：该方法体是
`return (intPtr != 0) ? Il2CppObjectPool.Get<List<string>>(intPtr) : null;`，
而 `intPtr` 来自 `Il2CppClassPointerStore<T>.NativeClassPtr` —— 由 interop 静态构造函数
为**编译期已知的 T** 填充。用反射传入运行时才知道的 `T` 时，该字段永不被初始化，
方法指针为空 ⇒ 返回 null（不是空列表）。**不是方法不能用，是不能这么用。**

```csharp
// ✗ 坑二: 直接用 Managed 类型全名去查 IL2CPP 类型 —— 恒返回 null
var t = Il2CppSystem.Type.GetType(ty.FullName);   // ty.FullName = "Il2Cpp.AmenitiesItemDirectory"
```

**为什么恒返回 null**：interop 生成的类型在 `namespace Il2Cpp;` 下，但 IL2CPP 侧的命名空间
是**空字符串** —— 反编译可证：`IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "DirectoryMaster")`。
所以查询名必须**剥掉 `Il2Cpp.` 前缀**。源码有 `using Il2Cpp;` 时这个前缀必然存在，极易忽略。

```csharp
// ✓ 正确写法
var t = Il2CppSystem.Type.GetType(剥掉Il2Cpp前缀的FullName);
if (DirectoryMaster.directories.TryGetValue(t, out var insts)
    && insts != null && insts.Count > 0)
{
    var inst = insts[0] as Directory<GameItem>;
    var fd = inst.factoryDictionary;              // Dictionary<string, Func<T>>
    foreach (var kv in fd) ids.Add(kv.Key);       // 键 = stableId
}
// directories 的实际类型: Dictionary<Il2CppSystem.Type, List<Il2CppSystem.Object>>
// 属性是 public static (getter 标 Private_Static, interop 已暴露), token 100663893
```

**不要靠猜名字。** 更稳的做法是**遍历 `directories.Keys` 取其 `FullName`/`Name` 建索引**，
再与候选类型名对照 —— 完全不依赖对命名空间拼法的假设：

```csharp
var byName = new Dictionary<string, Il2CppSystem.Type>();
foreach (var k in DirectoryMaster.directories.Keys) {
    byName[k.FullName] = k; byName[k.Name] = k;   // 同时收全名与短名
}
```
配套纪律（三条，都是这个 bug 的真实教训）：
- **空 `catch {}` 让探测失败毫无痕迹。** 31 个目录类一个都没成功，却因无日志只能靠
  `dirCount=0` 反推。改为分别统计「未解析 / 查不到 / 异常」三类计数并各记首例。
- **枚举为空时不要缓存空结果。** 否则启动早期（目录尚未注册完）的一次失败被永久固化；
  且若 `Ids` 恒为 null 而节流条件写成 `Ids != null && ...`，OnUpdate 会**每帧**重跑全量枚举。
  正确做法：`Ids==null` 时保留 null 并设最短重试窗口，成功才写入。
- **区分「静默 continue」与「抛异常」。** 日志出现 `dirCount=0` 却**零警告**时，问题必在
  查找之前的解析阶段；有警告才是查找阶段。这个区分能把定位范围砍掉一半 ——
  第一轮就是没做这个区分，白跑了一轮「改 API」的修复。

**通用戒律**：工具「没有输出」不等于「结果为否」——先排除「命令失败/路径错/编码错」，
再把它当证据。

### 7.4 「连续 N 轮」写成「累计 N 轮」—— 注释与实现不一致，无测试可发现

**症状**：判稳过早，仍有目录在注册时就认定「注册完成」，据此对账 ⇒ 物品被误标「已失效」。

**代码（错）**：

```csharp
bool progressed = ids.Length > prevLen || dirCount > prevDir;
if (!progressed && ++_stableRounds >= CatalogStableRounds)   // ✗ 只跳过递增，没清零
```

注释写的是「**连续** N 轮无增长」，实现是「**累计** N 轮无增长」。`progressed` 为真时
只是不递增，**计数器保留此前累计值** —— 于是「增长 → 停 1 轮」也能凑够 3。

**日志坐实**（2026-09-25_09-01-42.log）：

| 时刻 | 物品/目录 | progressed | `_stableRounds` |
|---|---|---|---|
| 09:01:54.210 | 383/0 | true（首次） | 0 |
| 09:01:55.206 | 383/0 | false | 1 |
| 09:01:56.205 | 383/0 | false | 2 |
| 09:02:00.560 | **485/2** | **true** | **2 ← 应清零** |
| 09:02:01.621 | 485/2 | false | **3 → 判稳** |

注册刚在 09:02:00 增长，**1 秒后就判稳**；而 BE 的 `[BE-REPAIR] 扫描 78 件` 到
**09:02:07** 仍在活动 —— 判稳之后 6 秒。

**修法**：

```csharp
if (progressed) _stableRounds = 0;
else if (++_stableRounds >= CatalogStableRounds) { _catalogSettled = true; ... }
```

**为什么这类 bug 特别毒**：
- 编译器不管（逻辑合法）；测试不管（无单测覆盖启停时序）；**只有日志能发现**。
- 它不报错，只是「早了一点」—— 而早一点在**对账**语义下就是**误标**，直接影响用户能不能点生成。
- 修好后行为变化极小（多等 1~2 轮），所以**不做日志核验就永远不会被发现**。

**纪律**：
- 注释里出现「连续 / 累计 / 至少 / 不超过」等**量词**时，实现必须逐字对照注释核验一遍。
- 计数器型判据一律显式写「增长即清零」，不要依赖 `!progressed && ++` 这种隐式写法。
- **判据必须双维独立计数**（见 §7.2）：只看总数会被恒定量掩盖。

**配套：让数字能自解释。** 「目录 2 个」这种聚合数无法定位 —— 游戏有 29 个
`ItemDirectory` 子类，差 27 个却说不出是谁。改为逐个打印 `类型名:条数`：

```csharp
try { detail.Add($"{d.GetType().Name}:{fd.Count}"); } catch { detail.Add($"?:{fd.Count}"); }
...
if (detail.Count > 0) MelonLogger.Msg($"[List] 目录源明细: {string.Join(", ", detail)}");
```

补充事实：`DirectoryMaster.directories` 的键是 **`DirectoryEntry` 类型**（实测仅
`GameItem` / `CombatAbilityEffect`），29 个 `ItemDirectory` 子类**全部挂在 `GameItem`
这一个键下**（`ItemDirectory : Directory<GameItem>`）。所以 `dirCount < 29` 只可能是
「实例尚未 `Awake`」，不可能是「分键了」—— 注册发生在 `Directory<T>.Awake()` 中
（ISIL 可证：`Awake` 依次调 `AddDirectory` → `InitDirectory`）。

---

## 8. 附录：重建 dump 的完整命令

```bash
G="D:/steam/steamapps/common/Probably Stolen Demo"
CPP2IL="$G/MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/Cpp2IL.exe"
ILSPY="/c/Users/11/.dotnet/tools/ilspycmd"
cd D:/git/PlayerStore

# 0. 备份旧产物
for d in ascs cpp2il_il cpp2il_isil; do
  [ -d "dump/$d" ] && mv "dump/$d" "dump/${d}_old_bak_$(date +%Y%m%d)"
done
mkdir -p dump/cpp2il_isil dump/cpp2il_il dump/ascs

# 1. ISIL（唯一有方法体的产物）
"$CPP2IL" --game-path "$G" --output-as isil          --output-to "$PWD/dump/cpp2il_isil"

# 2. 中间 dll —— 必须 dll_throw_null
"$CPP2IL" --game-path "$G" --output-as dll_throw_null --output-to "$PWD/dump/cpp2il_il"

# 3. 补 Unity 模块 dll（Cpp2IL 只出 94 个，旧 dump 121 个）
comm -23 \
  <(ls "$G/MelonLoader/Il2CppAssemblies/" | sort) \
  <(ls dump/cpp2il_il/ | sort) \
  | while read -r f; do cp -n "$G/MelonLoader/Il2CppAssemblies/$f" dump/cpp2il_il/; done

# 4. ascs（只给签名；不要加 --nested-directories false，该参数会被当成文件名报错）
"$ILSPY" -p -o "$PWD/dump/ascs" -r "$PWD/dump/cpp2il_il" \
         "$PWD/dump/cpp2il_il/Assembly-CSharp.dll"
```

**验收**（三产物齐 + 关键类型存在）：

```bash
ls dump/ascs/*.cs | wc -l                                      # ~643
ls dump/cpp2il_isil/IsilDump/Assembly-CSharp/*.txt | wc -l     # ~1520
ls dump/cpp2il_il/*.dll | wc -l                                # ~121
grep -l ": ItemDirectory\b" dump/ascs/*.cs | wc -l             # 目录类数量
```

**顺手做一次版本 diff**（判断本次更新删/增了什么，价值很高）。
注意归档目录名不统一（`ascs_old_bak_*` / `cpp2il_il_old_bak_*` / `cpp2il_isil_old2_bak_*`），
且 ascs 里有 `bin` / `obj` 子目录会混进结果，须过滤：

```bash
OLD=$(ls -d dump/ascs_old_bak_* | head -1)
comm -13 <(ls "$OLD" | grep '\.cs$' | sort) <(ls dump/ascs | grep '\.cs$' | sort)  # 新增类型
comm -23 <(ls "$OLD" | grep '\.cs$' | sort) <(ls dump/ascs | grep '\.cs$' | sort)  # 删除类型
```

> 2026-09-25 以此法查出：游戏 9/18 更新**删除了 `MachineEvaporator` / `PreBuildChemHelper` /
> `GunOrderData` / `GunOrderRequirement`**，**新增整套 Loot 系统 10 类**。而静态表
> `docs/items_data_full.js` 中 `evaporator` 三条仍挂着 —— 这正是「静态表必须配运行时对账」的实证。

---

## 9. 环境速查

| 项 | 值 |
|---|---|
| 游戏根目录 | `D:\steam\steamapps\common\Probably Stolen Demo` |
| 游戏进程 | `Probably Stolen.exe`（IL2CPP） |
| Mod 服务端口 | 26880 |
| MelonLoader 日志 | `<G>/MelonLoader/Latest.log` |
| 仓库 | `https://github.com/High-cla/PlayerStore`（master，Pages 源 `/docs`） |
| shell | Git for Windows bash（MSYS） |
| dotnet | 10.0.301 |
| ilspycmd | 10.1.1.8388 |
| ast-grep | 0.42.3 |
| Cpp2IL | 2022.1.0-pre-release.21 |
