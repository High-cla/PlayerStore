# 红队裁决报告 — Core.cs 三处修复 (task-4)

- 裁决人: redteam (冷视角, 独立复核)
- 被审对象: `InventorySorter/InventorySorter/Core.cs`
- 基线 commit: `0d69984`
- 独立探针: `tscripts/redteam_tightest.py` / `redteam_shrinkrects.py` / `redteam_nativewarn.py`
- 结论: **通过 (PASS)** — 见「复裁 (Round 2)」。含 3 项已登记的残余风险(非阻塞)。

---

# 复裁 (Round 2) — 通过

- 被审版本指纹: **md5 = `4e6e9b59c012592ae098dfed99018443`** (diff: +63 / -5)
- 上一轮被审版本: `f56145dd3b919da1c38fde264eea0f16` → 打回
- 独立构建: `dotnet build` → 0 警告 0 错误
- 三支探针: `redteam_tightest.py` **PASS** / `redteam_shrinkrects.py` **PASS** / `redteam_nativewarn.py` **PASS**

## 0. diff 完整性 — 通过, 无隐藏改动

`git diff --stat`: 1 file changed, **63 insertions(+), 5 deletions(-)**。逐 hunk 核对, 全部落在 lead 申报的 4 处内, 无未申报的第 5 处改动:
1. `Core.cs:187` 新增 `_nativeWarnSig` 字段 + 注释;
2. `Core.cs:271-283` OnUpdate 成功轮次清零;
3. `Core.cs:290-303` NativeWarn 实现 + 头注释;
4. `Core.cs:1982-2006` 早退 + 键打包注释; `Core.cs:2724-2766` ShrinkRects 去包含。

## 1. 【打回项 1】NativeWarn 复发静默 — 已修复, 通过

### 证据 — 修复点

`Core.cs:271-283`:
```csharp
try
{
    RefreshNativeUI();
    TrackOpenedContainers();
    // 本轮成功 ⇒ 清掉上次的异常签名, 否则「同样的故障再次发生」会被当成持续异常永久静默
    // (例: 容器A抛NRE→告警; 关闭A恢复正常; 再开A抛同一NRE→无日志)。去重只应作用于
    // 连续失败期间, 不该跨过中间的成功轮次。
    _nativeWarnSig = null;
}
catch (System.Exception ex)
{
    NativeWarn(ex);
}
```
关键正确性: `_nativeWarnSig = null;` 位于 `try` 块内、**两次调用之后** ⇒ 仅当 `RefreshNativeUI()` 与 `TrackOpenedContainers()` **都未抛异常**的轮次才清零。半成功(`RefreshNativeUI` 成功但 `TrackOpenedContainers` 抛)不会清零, 语义正确。

### 独立复现 — 探针 [B3] 已转 PASS

`redteam_nativewarn.py [B3]` (镜像 `Core.cs:285-303` + 调用方成功轮次清零):
```
[B3] 恢复后复发时序: ['print', 'suppress', 'suppress', 'suppress', 'ok', 'ok', 'ok', 'ok', 'print', 'suppress', 'suppress', 'suppress']
     复发共 2 轮, 实际打印 2 次
```
上一轮同一时序打印 1 次(第 2 轮静默) → 本轮打印 2 次。**原打回根因消除。**

### 1.1 【新风险, 已量化, 非阻塞】reset 引入的间歇性故障刷屏

lead 明确要求复核「成功轮次每 0.25s 清零是否让交替刷屏更严重」。实测 `redteam_nativewarn.py [B4]`:
```
[B4] 60s 失败/成功每 tick 交替(同一异常) -> 无reset 1 条 / 有reset 120 条
```
- 持续同类异常: **1 条/分**(不变, 防刷屏有效);
- 两种异常每 tick 交替且**期间无成功轮**: **240 条/分**(与 reset 无关, 上一轮既有残余);
- **新增路径**: 失败/成功每 tick 交替(如某容器打开即抛、关闭即好) → **120 条/分**(修复前 1 条)。

**判定: 非阻塞残余风险。** 理由: ① 它是「让复发可见」的必要代价 —— 去掉 reset 就等于回到打回项 1, 二者不可兼得; ② 上限被物理约束在 ~4 条/秒(0.25s tick), 非无界刷屏; ③ 该模式要求故障精确地逐 tick 振荡, 真实 Unity 异常通常是持续性的, 落在「1 条/分」的良性分支。**是否改用时间窗方案由 lead 定夺, 我不强制要求**: 若改「签名 + 时间窗(如 60s)」, 可同时覆盖 B2(240)与 B4(120), 但代价是首次复发可能延迟至窗口到期才可见。当前实现已满足「不静默复发」这一硬要求。

## 2. 【打回项 2】未申报的第 4 处改动 — 已补申报, 通过

`Core.cs:1982-2006` 的朝向内早退现已在源码注释中显式申报并说明正确性前提与成本:
```csharp
// 每个朝向只扫到「首个可行 y 行」为止, 该行内取首个可行 x 即停:
// waste 与 (ori,x,y) 无关(四朝向 gw*gh 与 cs.Count 同值 ⇒ 全局常数), 故 key 比较实际退化为
// 「最小 y 再最小 x」⇒ 朝向内首个可行位就是该朝向的字典序最小可行位。
// 成本: 有解时 CellsFree 由约 2.05M 次降至 4~3016 次; 无解时仍需枚举完(保持等价的下界)。
// 警告: 早退只能做到「朝向内」—— 跨朝向早退是错的(某朝向 y 更优时会被先扫到的朝向顶掉),
// 四个朝向必须全部比完再取最小(见 tscripts/probe_tightest_domain.py 的对拍与反例)。
bool found = false;
for (int y = 0; y + gh <= H && !found; y++)
```
**独立核验 `found` 作用域**: `bool found = false;` 在 `Core.cs:1988`, 而 `for (int ori = 0; ori < 4; ori++)` 起于 `Core.cs:1968` ⇒ `found` **在每个朝向开头重置**, 不会跨朝向残留(若声明在 ori 循环外则会导致第 2~4 朝向直接跳过 —— 这是本改动最易踩的坑, 已确认无此问题)。

等价性独立复验 `redteam_tightest.py [F]`:
```
对拍 694 例, 位置差异 0 (其中最优朝向 != 0 的例 215)
```
第 215 例覆盖「全局最优不在首个可行朝向」⇒ 测试对跨朝向错误有判别力。

## 3. 【建议 3】归因表述 — 已修正, 通过

`Core.cs:1997-2001`:
```csharp
// 键按 (waste 最小 → y 最小 → x 最小) 字典序。原式 waste*1000000 + y*100000 + x 隐含
// y<10(100000*10 进位到 waste 位), 而 H 来自 GetGridDims 可达 4096/8192。
// 注意: 在本调用点 waste 实际是常数(四朝向 gw*gh 与 cs.Count 同值 ⇒ 进位路径不可达),
// 故旧式从未产生错误结果 —— 此处是防御性等价改写, 防止日后 waste 变成变量时静默错排。
// 改用 W/H 为基, 对任意 W<=128/H<=8192 严格字典序且不溢出 long。
```
逐句核对我的证明: `redteam_tightest.py [D]` 2 万随机形状「旋转后 cell 数/bbox 面积不守恒的例 = 0」, `[C]` 真实调用域 870 例「waste 跨朝向变化的例 0」⇒ 「waste 是常数 ⇒ 进位路径不可达 ⇒ 旧式从未产生错误结果」表述**准确**。原先「真实缺陷 / y>=10 破坏 tie-break」的错误归因已删除。剩余提及「y<10 隐含进位」的部分是对**原式一般性质**的客观描述, 与「本调用点不可达」并不矛盾, 保留恰当。

## 4. 键打包 / ShrinkRects / 静态状态 / 约定 — 复验仍通过

- **键打包** `redteam_tightest.py` PASS: 最大键 1.1e12 ≪ `long.MaxValue`(不溢出); 40 万组字典序 0 违反; x 权重未倒置; 端到端旧实现(旧键+全扫) vs 新实现(新键+早退) 405 例位置 0 差异。本轮 diff 未再触碰该表达式。
- **ShrinkRects** `redteam_shrinkrects.py` PASS: 20 万例逐位等价(含输出顺序, 121506 例含重复/包含), 0 差异; `areas[j] < ra` 严格小于 ⇒ 等面积不漏判; 原地 `Clear`+回填语义一致; 注入 `<=` 错误剪枝被捕获 11234/20000(判别力), 去剪枝 0 差异(剪枝结果中性)。本轮 diff 未再触碰该函数。
- **静态状态**: 新增静态可变状态仍仅 `_nativeWarnSig`(现已在成功轮次清零, 不再是「从不重置」的残留); `_histBuf`/`_stackBuf`(`Core.cs:136-137`)每轮清零完好; `_sortStacked`(`Core.cs:176`)未受影响。
- **约定**: `KeepContainersConst`(`Core.cs:120`)=`false` 分支未被 diff 触及; `Core.cs:107-108`「分支保留便于日后回调」未破坏; 空 catch 37 处中 36 处带 `// ponytail:`, 唯一缺失的 `Core.cs:229` 为**改动前既有**, 非本次引入。

## 5. 残余风险登记 (非阻塞, 供 lead 决策)

| # | 风险 | 证据 | 建议 |
|---|------|------|------|
| R1 | 间歇性故障刷屏: 失败/成功每 tick 交替 → 120 条/分(修复前 1 条); 两种异常交替 → 240 条/分 | `[B2]` `[B4]` | 可选: 改「签名 + 时间窗」同时覆盖 B2/B4; 不改亦可接受(上限 ~4 条/s, 非无界) |
| R2 | 同类型+同 Message 但不同来源/StackTrace 的第二个错误被合并 | `[A]` | 设计取舍(防刷屏必要代价); 若需区分可加入首个栈帧 |
| R3 | `Core.cs:229` 空 catch 缺 `// ponytail:` | `[D]` | 改动前既有, 本次范围外, 可顺手补 |

## 附: 独立验证方法与可复现命令

```
cd D:/git/PlayerStore/InventorySorter/tscripts
python redteam_tightest.py      # RESULT: PASS
python redteam_shrinkrects.py   # RESULT: PASS
python redteam_nativewarn.py    # RESULT: PASS  ([B3] 复发静默已修复; [B4] 记录 R1)
cd .. && dotnet build           # 0 警告 0 错误
md5sum InventorySorter/Core.cs  # 4e6e9b59c012592ae098dfed99018443
```
- `redteam_tightest.py`: [A] 溢出边界, [B] 40 万组字典序对拍, [C] 真实调用域旧/新键位置等价, [D] waste 跨朝向常量性, [E] 同款键模式全仓扫描, [F] 早退 vs 全扫等价, [G] 端到端旧实现 vs 新实现。
- `redteam_shrinkrects.py`: [A] 20 万例逐位等价, [B] 等面积/包含专测, [C] 原地修改语义, [D] 错误剪枝判别力。
- `redteam_nativewarn.py`: [A] 签名判别力, [B1] 持续异常防刷屏, [B2] 交替刷屏, [B3] **复发可见性(原打回项)**, [B4] **reset 新增刷屏路径**, [C] 主线程假设(源码 `Thread`/`Task.Run`/`async`/`HarmonyPatch` 计数均为 0), [D] 空 catch 约定。

## 裁决

**通过 (PASS)。** 上一轮 3 项打回/建议已全部落实: ① `_nativeWarnSig` 成功轮次清零, 复发静默消除(探针 [B3] 转 PASS); ② 第 4 处早退已显式申报, `found` 作用域经独立核验正确; ③ 归因表述已改为「防御性等价改写」且与我的证明一致。diff 无隐藏改动(+63/-5 全部可归因)。排序结果正确性逐位等价(#1/#1b/#3 均 0 差异)。残余风险 R1/R2/R3 已登记, 均不阻塞。

> 复裁前提: 被审版本 md5 = `4e6e9b59c012592ae098dfed99018443`。若 Core.cs 再次变动, 本报告对键打包/早退/ShrinkRects 的等价性结论需按新版本重跑。

---

# 附录: Round 1 原始裁决 (打回, 已由 Round 2 取代)

被审版本 md5 `f56145dd3b919da1c38fde264eea0f16` (+56/-5), 打回项:
1. **NativeWarn 复发静默**: `_nativeWarnSig`(仅 `L187`/`L292`/`L296` 引用)写入后从不重置 ⇒ 时序 `print,suppress,suppress,suppress, ok×4, suppress×4`(复发第 2 轮静默)。→ Round 2 已修复。
2. **未申报的第 4 处改动** `Core.cs:1982-2006` 朝向内早退 → Round 2 已补申报。
3. **归因表述错误**: 注释称旧键打包为「真实缺陷」, 但 `waste` 在本调用点为常数, 旧式从未产生错误结果 → Round 2 已修正。
其余项(键打包正确性/不溢出、ShrinkRects 逐位等价、静态状态、仓库约定)在 Round 1 即判通过, Round 2 复验不变。
