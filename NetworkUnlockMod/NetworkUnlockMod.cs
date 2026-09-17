using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

[assembly: MelonInfo(typeof(NetworkUnlockMod.Core), "NetworkUnlockMod", "0.1.0", "local")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]

namespace NetworkUnlockMod
{
    /// <summary>
    /// 解锁"已经存在, 但未开放"的网络升级条目(Playtest/demo 锁定).
    ///
    /// 游戏机制(反编译证据, dump/cpp2il_isil/IsilDump/Assembly-CSharp/):
    ///   WildUIManager.txt:1883  Call NetworkUpgrade.IsLockedInDemo, rcx, rdx   ← 全网唯一调用点
    ///   WildUIManager.txt:1884-1903 为 true 时 SetActive 显示 network_ui_demo_locked 文案(购买被禁)
    ///   NetworkUpgrade.txt:2534  Method: System.Boolean IsLockedInDemo(System.String id)  => public static bool
    ///   NetworkUpgrade 类型另有 lockInDemo(实例 bool) 与 demoLockedIds(静态 HashSet&lt;string&gt;) 两字段
    ///
    /// 本 mod 只接管这一个静态判据: 命中解锁列表的 id 直接返回 false, 其余 id 走游戏原逻辑.
    /// 不做二进制补丁, 不改 GameAssembly.dll, 也不随游戏更新失效(仅依赖类名+方法名).
    ///
    /// 与第三方 ProbablyStolenUnlockMod.dll(作者: 小行星)的关系: 该包是内存字节补丁, 仅在
    /// GameAssembly.dll 的 SHA-256 与大小与它 profile 完全一致时启用, 游戏更新后自动停用.
    /// 本 mod 与之互不干扰(它停用时什么都不接管), 但请勿同时依赖两者.
    /// </summary>
    public class Core : MelonMod
    {
        private static readonly MelonPreferences_Category Cfg = MelonPreferences.CreateCategory("NetworkUnlockMod");

        public static readonly MelonPreferences_Entry<bool> CfgEnabled =
            Cfg.CreateEntry<bool>("Enabled", true, "总开关: 接管 NetworkUpgrade.IsLockedInDemo");

        public static readonly MelonPreferences_Entry<bool> CfgUnlockAll =
            Cfg.CreateEntry<bool>("UnlockAllDemoLocked", false, "解锁全部 demo 锁定条目 (为 true 时忽略 UnlockIds)");

        public static readonly MelonPreferences_Entry<string> CfgUnlockIds = Cfg.CreateEntry<string>(
            "UnlockIds",
            "CHEMIST,PHARMA,CRIMINEL_NETWORK,SHOWCASE_II,RUINED_MACHINE_UNLOCK,RETIRED_GUNSMITH,RETIRED_CHEMIST,JACKSON2,RENOVATION3,RETIRED_FARMER",
            "要解锁的条目 id, 逗号分隔 (与游戏 NetworkUpgrade 的 id 一致, 大写)");

        internal static HashSet<string> Ids = new HashSet<string>(StringComparer.Ordinal);
        internal static bool Enabled => CfgEnabled.Value;
        internal static bool UnlockAll => CfgUnlockAll.Value;

        public override void OnInitializeMelon()
        {
            ReloadIds();
            MelonPreferences.Save(); // 配置在启动时即落盘生成, 玩家可提前修改
            try
            {
                new HarmonyLib.Harmony("local.NetworkUnlockMod").PatchAll(typeof(Core).Assembly);
                MelonLogger.Msg($"[NetworkUnlock] 已启用: 接管 NetworkUpgrade.IsLockedInDemo, 解锁 {Ids.Count} 个条目" +
                                (UnlockAll ? " (UnlockAllDemoLocked=true)" : $": {string.Join(",", Ids)}"));
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[NetworkUnlock] 挂补丁失败, 未生效: {e}");
            }
        }

        internal static void ReloadIds()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            var raw = CfgUnlockIds.Value ?? string.Empty;
            foreach (var part in raw.Split(new[] { ',', ';', '|', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                set.Add(part.Trim());
            }
            Ids = set;
        }

        public override void OnPreferencesSaved()
        {
            ReloadIds();
            MelonLogger.Msg($"[NetworkUnlock] 配置已更新: 解锁 {Ids.Count} 个条目");
        }
    }

    /// <summary>
    /// 闸门本体: 命中解锁列表则让 IsLockedInDemo 直接返回 false(跳过原方法); 其余保持游戏原逻辑.
    /// 用 __0 定位第一个参数, 不依赖游戏侧参数名.
    /// </summary>
    [HarmonyPatch(typeof(NetworkUpgrade), "IsLockedInDemo")]
    internal static class PatchIsLockedInDemo
    {
        private static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);

        private static bool Prefix(string __0, ref bool __result)
        {
            try
            {
                if (!Core.Enabled || __0 == null)
                {
                    return true;
                }
                if (!Core.UnlockAll && !Core.Ids.Contains(__0))
                {
                    return true; // 不在解锁列表: 保持游戏原判据
                }
                __result = false;
                if (Logged.Add(__0))
                {
                    MelonLogger.Msg($"[NetworkUnlock] 解锁网络条目: {__0}");
                }
                return false;
            }
            catch (Exception e)
            {
                // IL2CPP 异常: 保持原值 / 原逻辑
                MelonLogger.Warning($"[NetworkUnlock] 前置异常, 保持游戏原逻辑: {e.Message}");
                return true;
            }
        }

        /// <summary>
        /// 诊断: 记录游戏"原生判定为 demo 锁定"的 id 集合(每个 id 只记一次).
        /// 用来核对 UnlockIds 是否与游戏实际锁定集合一致 —— 游戏更新可能增删条目.
        /// (前置命中时 __result 已被置 false, 不会重复记录.)
        /// </summary>
        private static void Postfix(string __0, bool __result)
        {
            try
            {
                if (__0 == null || !__result || !NativeLocked.Add(__0))
                {
                    return;
                }
                MelonLogger.Msg($"[NetworkUnlock] 游戏原生判定 demo 锁定: {__0}");
            }
            catch
            {
                // 诊断失败不影响功能
            }
        }

        private static readonly HashSet<string> NativeLocked = new HashSet<string>(StringComparer.Ordinal);
    }
}
