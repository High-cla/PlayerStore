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
| 8 | IL2CPP 类型查找的两个陷阱：静态泛型反射调用 / 未剥 `Il2Cpp.` 前缀 | 图鉴目录枚举恒为 0 个（日志「[List] 目录 0 个, 物品 0 个」且**无任何警告**） | 见 §7.1：遍历 `directories.Keys` 建索引，免去名字拼法假设 |

### 7.1 目录枚举：两个坑叠在一起，各自都会让结果恒为空

「列出游戏当前全部物品 stableId」的正确写法，以及**两条都踩过**的错法：

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
