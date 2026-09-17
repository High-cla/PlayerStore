# NetworkUnlockMod

解锁《Probably Stolen》里**已经存在、但被 Playtest/demo 闸门锁住**的网络升级条目。

## 原理(而非二进制补丁)

游戏唯一的闸门调用点(反编译证据 `dump/cpp2il_isil/IsilDump/Assembly-CSharp/`):

| 证据 | 内容 |
|---|---|
| `WildUIManager.txt:1883` | `Call NetworkUpgrade.IsLockedInDemo, rcx, rdx` —— **全网唯一调用点** |
| `WildUIManager.txt:1884-1903` | 为 true 时 `SetActive` 显示 `network_ui_demo_locked` 文案并禁用购买 |
| `NetworkUpgrade.txt:2534` | `Method: System.Boolean IsLockedInDemo(System.String id)` ⇒ `public static bool` |
| `NetworkUpgrade` 类型 | 另有 `lockInDemo`(实例 `bool`)、`demoLockedIds`(静态 `HashSet<string>`) |

本 mod 用 Harmony 只接管这一个静态判据: 命中解锁列表的 id 直接返回 `false`,其余 id 走游戏原逻辑。
**不改 GameAssembly.dll、不做内存字节补丁,因此不随游戏更新失效**(只依赖类名与方法名)。

## 默认解锁条目

`CHEMIST`、`PHARMA`、`CRIMINEL_NETWORK`、`SHOWCASE_II`、`RUINED_MACHINE_UNLOCK`、
`RETIRED_GUNSMITH`、`RETIRED_CHEMIST`、`JACKSON2`、`RENOVATION3`、`RETIRED_FARMER`

## 配置

MelonLoader 首启动生成 `UserData/MelonPreferences.cfg`(分类 `NetworkUnlockMod`):

| 键 | 默认 | 说明 |
|---|---|---|
| `Enabled` | `true` | 总开关 |
| `UnlockAllDemoLocked` | `false` | 为 true 时解锁全部 demo 锁定条目(忽略 `UnlockIds`) |
| `UnlockIds` | 见上表 | 要解锁的条目 id,逗号分隔 |

## 验证

启动游戏后看 `MelonLoader\Latest.log`:

- `[NetworkUnlock] 已启用: 接管 NetworkUpgrade.IsLockedInDemo, 解锁 10 个条目: ...`
- `[NetworkUnlock] 解锁网络条目: <ID>` —— 每个被接管的条目首次出现时打印一次
- `[NetworkUnlock] 游戏原生判定 demo 锁定: <ID>` —— 用于核对游戏实际锁定集合(游戏更新可能增删条目)

## 与第三方 UnlockMod 的关系

社区的 `ProbablyStolenUnlockMod.dll`(作者: 小行星)是**内存字节补丁**,仅在 `GameAssembly.dll`
的 SHA-256 与文件大小与其 profile 完全一致时启用,游戏一更新就自动停用
(`当前游戏版本不受支持，UnlockMod 未启用。`)。其 payload 内含 143 处硬编码游戏 RVA,
只能由作者按新构建重新生成 —— 参考包见 `unlock/`(仅成品,无源码/生成器)。

两者互不干扰: 它停用时什么都不接管。但**不要同时依赖两者**;本 mod 生效时建议关闭对方(反之亦然)。
