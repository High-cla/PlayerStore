using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InventorySorter;

public class Core : MelonMod
{
	private struct Placement(int x, int y, int o)
	{
		public int X = x;

		public int Y = y;

		public int O = o;
	}

		private class ItemMask
		{
			public List<(int dx, int dy)> C0;

			public List<(int dx, int dy)> C1;

			public List<(int dx, int dy)> C2;

			public List<(int dx, int dy)> C3;

			public int Gw0;

			public int Gh0;

			public int Gw1;

			public int Gh1;

			public int Gw2;

			public int Gh2;

			public int Gw3;

			public int Gh3;

			public bool Square;
		}

	internal static MelonPreferences_Category Cfg;

	// 用户配置只剩两项: 自动排序开关 + 排序快捷键。
	// 其余原配置项按实测最优解固化为下方常量(分支保留, 便于日后回调), 不再暴露给玩家。
	internal static MelonPreferences_Entry<bool> AutoSortLastOpened;

	internal static MelonPreferences_Entry<string> SortHotkey;

	// 内部状态(is_hidden, 不算用户配置): 原生窗口拖动位置记忆
	internal static MelonPreferences_Entry<float> NativePosX;

	internal static MelonPreferences_Entry<float> NativePosY;

	// ---- 固化常量(原 MelonPreferences 项; 值 = 原默认最优解) ----
	// 恒真的两项不留常量(无分支可挂): 原 Enabled(功能总开关)与 ShowBackground(显示常驻背景存储)均固定为开。
	private const bool KeepContainersConst = false; // 原 KeepContainersInPlace: 容器(含液体瓶)也参与排序

	private const bool SkipBarterConst = true; // 原 SkipBarterWindows: 不给交易/选择弹窗加排序按钮

	private const int MinCellsConst = 10; // 原 MinCells: 小于此格数的垃圾格子不显示

	private const bool GroupByTagConst = true; // 原 GroupByTag: 同类聚带优先(容差见 BandedToleranceConst)

	private const int MaxRowsConst = 7; // 原 MaxRows: 面板固定高度(行)

	private const double BandedToleranceConst = 0.05; // 原 BandedToleranceRatio: 聚带空矩容差

	internal static bool ButtonsVisible = true;


	// LargestEmptyArea 复用缓冲区: 每次候选计算分配 int[W]+int[W+1] 是 GC 热点, 改为按需扩容复用(布局器串行调用, 不用锁)
	private static int[] _histBuf = new int[0];
	private static int[] _stackBuf = new int[0];

	internal static string LastAction = "";

	private static float _lastActionAt = -999f;

	private static bool _inputOk = true;

	private static bool _inputWarned = false;

	private const string NativeWindowId = "inventory_sorter";

	private static float _nativeTimer = 0f;

	private static bool _nativeDirty = true;

	private static string _nativeSig = null;

	private static readonly List<System.Action> _rootedActions = new List<Action>();

	// ---- 「最后打开的容器」与自动排序状态 ----
	// 原生 PixelWindow.focusStamp 是全局自增焦点戳(每次 ToFront/提权 +1, 见 PixelWindow.ToFront),
	// 故 visibleWindows 中 focusStamp 最大者 = 玩家最近打开/最前的那个窗口。

	// 自动排序: 只在窗口「从无到有」出现时触发一次(而非每次聚焦, 免得玩家拿东西时被重排);
	// 首轮 tick 只登记不触发(开游戏时已有一堆常驻窗口, 不能当成「刚打开」)。
	private static readonly HashSet<long> _seenWindows = new HashSet<long>();

	private static bool _autoWarmup = true;

	private static PixelWindow _pendingAuto;

	// 排序快捷键解析缓存(配置字符串变了才重新解析)
	private static string _hkSig;

	private static int _hkKey = -1;

	private static int[] _hkMods = new int[0];

	public override void OnInitializeMelon()
	{
		Cfg = MelonPreferences.CreateCategory("InventorySorter");
		AutoSortLastOpened = Cfg.CreateEntry<bool>("AutoSortLastOpened", false, (string)null, "Auto-sort a container the moment you open it (the most recently opened one). Off by default.", false, false, (ValueValidator)null, (string)null);
		SortHotkey = Cfg.CreateEntry<string>("SortHotkey", "F7", (string)null, "Hotkey that sorts the container you opened last. Single key or combo: F7 / G / LeftControl+F7 / LeftShift+LeftAlt+G. Modifiers: LeftShift RightShift LeftControl RightControl LeftAlt RightAlt. Invalid value falls back to F7.", false, false, (ValueValidator)null, (string)null);
		NativePosX = Cfg.CreateEntry<float>("NativePosX", -100000f, (string)null, "Internal state: saved window position X. Do not edit.", true, false, (ValueValidator)null, (string)null);
		NativePosY = Cfg.CreateEntry<float>("NativePosY", -100000f, (string)null, "Internal state: saved window position Y. Do not edit.", true, false, (ValueValidator)null, (string)null);
		PurgeLegacyEntries();
		// 配置在游戏启动时即落盘生成 (不再等首次触发/退出), 玩家可提前看到并修改
		MelonPreferences.Save();
	}

	public override void OnApplicationQuit()
	{
		MelonPreferences.Save();
	}

	// 清掉旧版遗留配置项(旧键仍会留在 MelonPreferences.cfg 里; 新版不再使用)。
	// 反射调用 DeleteEntry: 没有该 API 的 MelonLoader 上安全跳过(残留旧键无害)。
	private static void PurgeLegacyEntries()
	{
		try
		{
			System.Reflection.MethodInfo del = typeof(MelonPreferences_Category).GetMethod("DeleteEntry", new System.Type[1] { typeof(string) });
			if (del == null)
			{
				return;
			}
			string[] legacy = new string[14]
			{
				"Enabled", "KeepContainersInPlace", "SkipBarterWindows", "MinCells", "ShowBackground", "OnlyNamedBackground",
				"DisplayCaseSizes", "MainStorageSizes", "IgnoreBackgroundSizes", "GroupByTag", "GroupByTagDefaulted",
				"UseNativeUI", "MaxRows", "BandedToleranceRatio"
			};
			foreach (string id in legacy)
			{
				try
				{
					del.Invoke(Cfg, new object[1] { id });
				}
				catch
				{
					// 该项本就不存在, 忽略
				}
			}
		}
		catch
		{
			// ponytail: 反射探测, 静默回退
		}
	}

	public override void OnUpdate()
	{
		if (_inputOk)
		{
			try
			{
				if (Input.GetKeyDown((KeyCode)287))
				{
					ButtonsVisible = !ButtonsVisible;
					_nativeDirty = true;
				}
				// 快捷键: 排序「最后打开的容器」(原生焦点戳最大的可排序窗口)
				if (SortHotkeyPressed())
				{
					SortLastOpenedContainer();
				}
			}
			catch
			{
				if (!_inputWarned)
				{
					_inputWarned = true;
					_inputOk = false;
				}
			}
		}
		_nativeTimer += Time.deltaTime;
		if (_nativeTimer >= 0.25f)
		{
			_nativeTimer = 0f;
			try
			{
				RefreshNativeUI();
				TrackOpenedContainers();
			}
			catch (System.Exception ex)
			{
				NativeWarn(ex);
			}
		}
	}

	private static void NativeWarn(System.Exception ex)
	{
	}

	private static void RefreshNativeUI()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		CustomUIManager instance = CustomUIManager.Instance;
		if ((Object)(object)instance == (Object)null || (Object)(object)instance.buttonPrefab == (Object)null || (Object)(object)instance.windowPrefab == (Object)null)
		{
			return;
		}
		try
		{
			CustomUIWindow window = instance.GetWindow("inventory_sorter");
			if (window != null && window.IsAlive)
			{
				Vector2 anchoredPosition = window.Rect.anchoredPosition;
				if (Math.Abs(anchoredPosition.x - NativePosX.Value) > 0.5f || Math.Abs(anchoredPosition.y - NativePosY.Value) > 0.5f)
				{
					NativePosX.Value = anchoredPosition.x;
					NativePosY.Value = anchoredPosition.y;
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		if (!ButtonsVisible)
		{
			if (instance.IsOpen("inventory_sorter"))
			{
				instance.CloseWindow("inventory_sorter");
			}
			_nativeSig = null;
			return;
		}
		List<GameInventory> list = new List<GameInventory>();
		List<string> list2 = new List<string>();
		CollectSortables(list, list2);
		string text = list.Count.ToString();
		for (int i = 0; i < list2.Count; i++)
		{
			text = text + "|" + list2[i];
		}
		if (!_nativeDirty && text == _nativeSig)
		{
			return;
		}
		_nativeDirty = false;
		_nativeSig = text;
		if (instance.IsOpen("inventory_sorter"))
		{
			instance.CloseWindow("inventory_sorter");
		}
		_rootedActions.Clear();
		if (list.Count == 0)
		{
			return;
		}
		float num = 242f;
		float num2 = (float)Math.Max(3, MaxRowsConst) * 34f;
		CustomUIBuilder val = instance.CreateWindow("inventory_sorter", "Inventory Sorter", "overlay").SetDraggable(true).SetCloseOnEscape(false)
			.SetSize(num, 50f + num2);
		val.BeginScroll(num2);
		val.BeginGrid(1, 168f, 28f, 6f);
		for (int j = 0; j < list.Count; j++)
		{
			GameInventory inv = list[j];
			string text2 = Trunc(list2[j], 22);
						System.Action val2 = new System.Action(delegate
						{
							try
							{
								SortInventory(inv);
							}
							catch
							{
								// ponytail: IL2CPP native probe, silent fallback
							}
						});
			_rootedActions.Add(val2);
			val.AddButton(text2, val2, (string)null);
		}
		val.End();
		val.End();
		if (HasSavedPos())
		{
			val.SetPosition(new Vector2(NativePosX.Value, NativePosY.Value));
		}
		else
		{
			val.Center();
		}
		CustomUIWindow val3 = val.Show();
		if (HasSavedPos())
		{
			return;
		}
		try
		{
			if (val3 != null && val3.IsAlive)
			{
				Vector2 anchoredPosition2 = val3.Rect.anchoredPosition;
				NativePosX.Value = anchoredPosition2.x;
				NativePosY.Value = anchoredPosition2.y;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
	}

	private static bool HasSavedPos()
	{
		if (NativePosX.Value > -99000f)
		{
			return NativePosY.Value > -99000f;
		}
		return false;
	}

	// 原生 UI 已是唯一形态(原 UseNativeUI 配置固化 true): 旧的 IMGUI 面板与其 FaceClicked 已删除.
	// 按钮列表由 RefreshNativeUI 渲染, 见上方.
	private static string Trunc(string s, int max)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		if (s.Length > max)
		{
			return s.Substring(0, max - 1) + "…";
		}
		return s;
	}

	private static int CollectSortables(List<GameInventory> invs, List<string> labels)
	{
		int result = 0;
		try
		{
			WindowsHandler current = WindowsHandler.current;
			Il2CppSystem.Collections.Generic.List<PixelWindow> val = (((Object)(object)current != (Object)null) ? current.visibleWindows : null);
			int num = val?.Count ?? 0;
			result = num;
			for (int i = 0; i < num; i++)
			{
				PixelWindow val2 = null;
				try
				{
					val2 = val[i];
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
				if (val2 == null)
				{
					continue;
				}
				GameInventory val3 = ResolveInventory(val2);
				if (val3 == null || (SkipBarterConst && IsBarterOrChoose(val3)))
				{
					continue;
				}
				if (!IsSortableInventory(val3))
				{
					continue;
				}
				// 显示名 + 可排序性统一走 WindowLabel: 有标题用标题, 无标题常驻背景存储用容量标签(不写死尺寸, 升级/新增容器自动适配)
				string item = WindowLabel(val2, val3);
				if (item == null)
				{
					continue;
				}
				invs.Add(val3);
				labels.Add(item);
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return result;
	}

	private static GameInventory ResolveInventory(PixelWindow win)
	{
		try
		{
			GameInventory val = AsInventory((Il2CppObjectBase)(object)win.child);
			if (val != null)
			{
				return val;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		try
		{
			GameInventory val2 = AsInventory((Il2CppObjectBase)(object)win.childElement);
			if (val2 != null)
			{
				return val2;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		try
		{
			Il2CppSystem.Collections.Generic.List<GraphNodeStorage> children = win.children;
			if (children != null)
			{
				int count = children.Count;
				for (int i = 0; i < count; i++)
				{
					GameInventory val3 = AsInventory((Il2CppObjectBase)(object)children[i]);
					if (val3 != null)
					{
						return val3;
					}
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		try
		{
			Il2CppSystem.Collections.Generic.List<GameItem> parentItems = win.parentItems;
			if (parentItems != null)
			{
				int count2 = parentItems.Count;
				for (int j = 0; j < count2; j++)
				{
					GameItem val4 = parentItems[j];
					if (val4 != null)
					{
						GameGridInventory val5 = null;
						try
						{
							val5 = AnimalCage.GetCageInventory(val4);
						}
						catch
						{
							// ponytail: IL2CPP native probe, silent fallback
						}
						if (val5 != null)
						{
							return (GameInventory)(object)val5;
						}
					}
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return null;
	}

	private static GameInventory AsInventory(Il2CppObjectBase node)
	{
		try
		{
			return (node != null) ? node.TryCast<GameInventory>() : null;
		}
		catch
		{
			return null;
		}
	}

	private static void InvInfo(GameInventory inv, out string tag, out int cells)
	{
		tag = "Inv";
		cells = 0;
		try
		{
			GameGridScrollableInventory val = ((Il2CppObjectBase)inv).TryCast<GameGridScrollableInventory>();
			if (val != null)
			{
				tag = "Scroll";
				try
				{
					cells = Math.Max(0, val.width) * Math.Max(1, val.height);
					return;
				}
				catch
				{
					return;
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		try
		{
			GameGridInventory val2 = ((Il2CppObjectBase)inv).TryCast<GameGridInventory>();
			if (val2 == null)
			{
				return;
			}
			tag = "Grid";
			try
			{
				GridShape inventoryShape = val2.inventoryShape;
				if (inventoryShape != null)
				{
					cells = Math.Max(0, inventoryShape.width) * Math.Max(0, inventoryShape.height);
				}
			}
			catch
			{
				// ponytail: IL2CPP native probe, silent fallback
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
	}

	private static bool IsBarterOrChoose(GameInventory inv)
	{
		try
		{
			OverlayHandler current = OverlayHandler.current;
			if ((Object)(object)current == (Object)null)
			{
				return false;
			}
			GameGridScrollableInventory itemChooseInventory = current.itemChooseInventory;
			if (itemChooseInventory != null && ((Il2CppObjectBase)itemChooseInventory).Equals((Il2CppObjectBase)(object)inv))
			{
				return true;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return false;
	}

	// 排序前位置快照(供失败恢复), 拆出 SortInventory 第二段: 复杂度 -5
	private static List<(GameItem, int, int, int, bool)> SnapshotOriginals(List<GameItem> items)
	{
		List<(GameItem, int, int, int, bool)> snapped = new List<(GameItem, int, int, int, bool)>();
		foreach (GameItem it in items)
		{
			GridShape val = ShapeOf(it);
			int x = 0;
			int y = 0;
			int o = 0;
			bool flip = false;
			if (val != null)
			{
				try
				{
					x = val.minX;
					y = val.minY;
					o = val.orientation;
				}
				catch
				{
					// ponytail: IL2CPP native probe (minX/minY/orientation), silent fallback to coordinates below
				}
				try
				{
					GridShapeBuilder gb = ((Il2CppObjectBase)val).TryCast<GridShapeBuilder>();
					if (gb != null)
					{
						flip = gb.flipped;
					}
				}
				catch
				{
					// ponytail: IL2CPP native probe (flipped), silent fallback
				}
			}
			snapped.Add((it, x, y, o, flip));
		}
		return snapped;
	}

	// 按 TagKey 分组(排序池内的格子), 组内按 SizeCompare 排序; 拆出 SortInventory 第三段
	private static Dictionary<string, List<GameItem>> BuildTagGroups(List<GameItem> items, List<string> tagOrder)
	{
		Dictionary<string, List<GameItem>> groups = new Dictionary<string, List<GameItem>>();
		foreach (GameItem item in items)
		{
			string tag = (GroupByTagConst ? TagKey(item) : "");
			if (!groups.TryGetValue(tag, out var bucket))
			{
				bucket = (groups[tag] = new List<GameItem>());
				tagOrder.Add(tag);
			}
			bucket.Add(item);
		}
		if (GroupByTagConst)
		{
			tagOrder.Sort((string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
		}
		foreach (string tag in tagOrder)
		{
			groups[tag].Sort(SizeCompare);
		}
		return groups;
	}

	// 读取背包子项(去空), 拆出 SortInventory 第一段: 复杂度 -4
	private static List<GameItem> CollectChildItems(GameInventory inv)
	{
		List<GameItem> list = new List<GameItem>();
		try
		{
			Il2CppSystem.Collections.Generic.List<GameItem> childItems = inv.childItems;
			if (childItems == null)
			{
				return list;
			}
			int count = childItems.Count;
			for (int i = 0; i < count; i++)
			{
				GameItem val = childItems[i];
				if (val != null)
				{
					list.Add(val);
				}
			}
		}
		catch (System.Exception ex)
		{
			MelonLogger.Error("[InvSorter] read failed: " + ex);
			Toast("read failed: " + ex.Message);
			return list;
		}
		return list;
	}

	internal static void SortInventory(GameInventory inv)
	{
		List<GameItem> list = CollectChildItems(inv);
		if (list.Count <= 1)
		{
			Toast("nothing to sort");
			return;
		}
		// 按 KeepContainers 分流: 容器件留在原位, 其余进排序池
		List<GameItem> sortPool = new List<GameItem>();
		List<GameItem> keptContainers = new List<GameItem>();
		foreach (GameItem it in list)
		{
			if (KeepContainersConst && HasContentWindow(it))
			{
				keptContainers.Add(it);
			}
			else
			{
				sortPool.Add(it);
			}
		}
		if (sortPool.Count <= 1)
		{
			Toast("only containers here, left in place");
			return;
		}
		// 位置快照(供失败恢复): 记录排序前每个物品的 minX/minY/orientation/flipped
		List<(GameItem, int, int, int, bool)> original = SnapshotOriginals(sortPool);
		// 按 TagKey 分组(排序池内的格子), 组内按 SizeCompare 排序; 拆出 SortInventory 第三段: 复杂度 -5
		List<string> tagOrder = new List<string>();
		Dictionary<string, List<GameItem>> tagGroups = BuildTagGroups(sortPool, tagOrder);
		try
		{
			if (!GetGridDims(inv, out var w, out var h) || w <= 0 || h <= 0)
			{
				Toast("couldn't read grid size, not sorted");
				return;
			}
			w = Math.Min(w, 128);
			h = Math.Min(h, 8192);
			Dictionary<GameItem, ItemMask> masks = new Dictionary<GameItem, ItemMask>();
			foreach (GameItem it2 in sortPool)
			{
				masks[it2] = BuildMask(it2);
			}
			// 同类合并视图: 相同 ident + 相同形状(Gw0xGh0 + C0) 的多件只占一份布局(代表件),
			// 其余在应用阶段调 StackItemUnchecked 并入代表件. 大幅减少地面占用, 释放连续空域(实测 BEST 从不劣化).
			Dictionary<string, int> mergeRepIdx = new Dictionary<string, int>();
			for (int mi = 0; mi < sortPool.Count; mi++)
			{
				// 容器(有内容窗口的内部格子)不参与同类合并/堆叠: 保持独立, 只移动不堆叠
				if (HasContentWindow(sortPool[mi])) continue;
				ItemMask mm2 = masks[sortPool[mi]];
				StringBuilder msb = new StringBuilder();
				foreach ((int mdx, int mdy) in mm2.C0)
				{
					msb.Append(mdx).Append(':').Append(mdy).Append(',');
				}
				string mkey = sortPool[mi].identifier + "|" + mm2.Gw0 + "x" + mm2.Gh0 + "|" + msb.ToString();
				if (!mergeRepIdx.ContainsKey(mkey))
				{
					mergeRepIdx[mkey] = mi;
				}
			}
			// 被合并件清单: 非代表件的下标
			List<int> mergeAbsorb = new List<int>();
			HashSet<int> mergeRepSet = new HashSet<int>(mergeRepIdx.Values);
			for (int mi = 0; mi < sortPool.Count; mi++)
			{
				if (!mergeRepSet.Contains(mi) && !HasContentWindow(sortPool[mi]))
				{
					mergeAbsorb.Add(mi);
				}
			}
			Dictionary<GameItem, Placement> layout = null;
			// task-6: 横带(聚带)候选与密集候选同池, 按「带容差的聚带优先」定夺(见 BandedToleranceRatio)
			Dictionary<GameItem, Placement> bandedLayout = null;
			string mode = null;
			int relocated2 = 0;
			// 横带路径精修采纳件数(塞进缝隙的件数), 仅用于 toast 统计
			int tucked = 0;
			if (GroupByTagConst)
			{
				// 同类合并: 从 tag 分组中剔除被合并件(代表件保留), 布局后重叠致放自动合并
				if (mergeAbsorb.Count > 0)
				{
					HashSet<GameItem> absorbSet2 = new HashSet<GameItem>();
					foreach (int ai3 in mergeAbsorb)
					{
						absorbSet2.Add(sortPool[ai3]);
					}
					foreach (List<GameItem> group in tagGroups.Values)
					{
						group.RemoveAll(g => absorbSet2.Contains(g));
					}
				}
				Dictionary<GameItem, Placement> layoutBanded = LayoutBanded(tagOrder, tagGroups, masks, w, h, keptContainers);
				if (layoutBanded != null)
				{
					// 横带成功后也做同一精修层(以前只有 LayoutDense 会精修 ⇒ 横带成功路径零塞缝):
					// 小件优先释放自身格 → 塞进「能容纳它的最小空矩」 → 空矩内取最贴邻位.
					// 采纳条件(新空矩 >= 起始空矩 且 贴邻不降)在 TryFillRefine 内部, 非劣化才改位 ⇒
					// 最大空矩单调不减, 拆散(贴邻下降)恒 0, 落地重叠/压未动件/越界 与横带原结果同(逐件用 occ 精确校验).
					Dictionary<GameItem, Placement> refinedBanded = TryFillRefine(keptContainers, layoutBanded, masks, w, h);
					if (refinedBanded != null)
					{
						foreach (KeyValuePair<GameItem, Placement> kvTuck in refinedBanded)
						{
							if (layoutBanded.TryGetValue(kvTuck.Key, out Placement oldTuck) && (oldTuck.X != kvTuck.Value.X || oldTuck.Y != kvTuck.Value.Y || oldTuck.O != kvTuck.Value.O))
							{
								tucked++;
							}
						}
						layoutBanded = refinedBanded;
					}
					bandedLayout = layoutBanded;
				}
			}
			// 密集候选: 无论横带成败都算(task-6 需两边空矩才能按容差定夺; LayoutDense 只在局部 occ 上算, 无副作用)
			Dictionary<GameItem, Placement> denseLayout = null;
			{
				foreach (Comparison<GameItem> cmp in new List<Comparison<GameItem>>
				{
					SizeCompare,
					(GameItem a, GameItem b) => Math.Max(BaseW(b), BaseH(b)).CompareTo(Math.Max(BaseW(a), BaseH(a))),
					(GameItem a, GameItem b) => BaseH(b).CompareTo(BaseH(a)),
					(GameItem a, GameItem b) => BaseW(b).CompareTo(BaseW(a))
				})
				{
					List<GameItem> candidate = new List<GameItem>(sortPool);
					if (mergeAbsorb.Count > 0)
					{
						// 同类合并: 被合并件不参与布局(代表件排一次即可), 应用阶段重叠致放自动合并
						HashSet<GameItem> absorbSet = new HashSet<GameItem>();
						foreach (int ai2 in mergeAbsorb)
						{
							absorbSet.Add(sortPool[ai2]);
						}
						candidate.RemoveAll(g => absorbSet.Contains(g));
					}
					candidate.Sort(cmp);
					denseLayout = LayoutDense(candidate, masks, w, h, keptContainers);
					if (denseLayout != null)
					{
						break;
					}
				}
			}
			if (bandedLayout != null && denseLayout != null)
			{
				// task-6 容差定夺: 横带全放 且 横带空矩 >= 密集空矩 - tol*密集空矩 ⇒ 选横带(保「同类聚带」产品目标),
				// 否则选密集. tol = BandedToleranceRatio, 离线曲线见 InventorySorter/tscripts/bench_banded.py
				long areaBanded = EmptyAreaOfLayout(keptContainers, bandedLayout, masks, w, h);
				long areaDense = EmptyAreaOfLayout(keptContainers, denseLayout, masks, w, h);
				if (areaBanded >= areaDense - (long)(areaDense * BandedToleranceRatio))
				{
					layout = bandedLayout;
					mode = "grouped";
				}
				else
				{
					layout = denseLayout;
					mode = "packed";
				}
			}
			else if (bandedLayout != null)
			{
				layout = bandedLayout;
				mode = "grouped";
			}
			else if (denseLayout != null)
			{
				layout = denseLayout;
				mode = "packed";
			}
			if (layout == null)
			{
				// 降级残局(Residual): 全放失败不再整包放弃 — 大件各选最贴合位, 小件尽力塞缝, 放不下的留原位.
				// 严格布局器"任一放不下整候选作废"; 此处失败件留位且占位作障碍, 落地永不与未动件重叠.
				// 超大网格坐标扫描过贵(主线程), 维持原 abort.
				Dictionary<GameItem, Placement> degradeLayout = null;
				if ((long)w * h < 5000)
				{
					List<GameItem> placePool2 = new List<GameItem>();
					HashSet<int> absIdx2 = new HashSet<int>(mergeAbsorb);
					for (int gi = 0; gi < sortPool.Count; gi++)
					{
						if (!absIdx2.Contains(gi)) placePool2.Add(sortPool[gi]);
					}
					degradeLayout = TryResidualLayout(keptContainers, sortPool, placePool2, masks, w, h, out relocated2);
				}
				// 全留原位且无同类可堆 -> 无收益, 维持原 abort 语义
				if (degradeLayout != null && degradeLayout.Count > 0 && (relocated2 > 0 || mergeAbsorb.Count > 0))
				{
					layout = degradeLayout;
					mode = "degraded";
				}
				else
				{
					RestoreOriginal(inv, original);
					Toast("not enough room to sort cleanly, left unchanged");
					return;
				}
			}
			int num = 0;
			// 同类合并应用: 布局成功后, 被合并件致放到代表件同一位置(重叠) — 游戏堆叠机制自动合并为一格.
			// 代表件位置 = layout[rep]; 每个被合并件找同 ident 代表, PlaceItem 到代表件的 X/Y/O.
			if (mergeAbsorb.Count > 0)
			{
				foreach (int ai in mergeAbsorb)
				{
					GameItem absorbed = sortPool[ai];
					GameItem rep = null;
					foreach (KeyValuePair<string, int> mp in mergeRepIdx)
					{
						GameItem r = sortPool[mp.Value];
						if (r.identifier == absorbed.identifier)
						{
							rep = r;
							break;
						}
					}
					if (rep != null && layout.TryGetValue(rep, out Placement rp))
					{
						if (!PlaceItem(absorbed, rp.X, rp.Y, rp.O))
						{
							MelonLogger.Error($"[InvSorter] merge place failed: absorbed {absorbed.identifier} @ {rp.X},{rp.Y} (looks unmoved)");
						}
					}
					else
					{
						MelonLogger.Error($"[InvSorter] merge: no rep placement for {absorbed.identifier} (absorbed stays)");
					}
				}
			}
			// 堆叠物品(unitCount>1)最后放置: 游戏按放置顺序渲染, 后放的贴图在上层, 保证堆叠物至少一格视觉可见(否则被盖住看着取不出)
			List<KeyValuePair<GameItem, Placement>> order11 = new List<KeyValuePair<GameItem, Placement>>(layout);
			order11.Sort((a, b) => (Stacked(a.Key) ? 1 : 0).CompareTo(Stacked(b.Key) ? 1 : 0));
			foreach (KeyValuePair<GameItem, Placement> kvp in order11)
			{
				Placement value3 = kvp.Value;
				PlaceItem(kvp.Key, value3.X, value3.Y, value3.O);
				if (value3.O == 1)
				{
					num++;
				}
			}
			try
			{
				inv.Validate();
			}
			catch (System.Exception exV)
			{
				MelonLogger.Error("[InvSorter] post-layout Validate failed: " + exV.Message);
			}
			Toast($"{mode} {layout.Count}/{sortPool.Count} item(s)" + ((mode == "degraded") ? $", {relocated2} tucked into gaps" : "") + ((mode == "grouped" && tucked > 0) ? $", {tucked} tucked into gaps" : "") + ((num > 0) ? $", {num} rotated" : "") + ((keptContainers.Count > 0) ? $"  ({keptContainers.Count} kept)" : ""));
		}
		catch (System.Exception ex2)
		{
			try
			{
				RestoreOriginal(inv, original);
			}
			catch (System.Exception exR)
			{
				MelonLogger.Error("[InvSorter] restore failed after sort error: " + exR.Message);
			}
			MelonLogger.Error("[InvSorter] sort error: " + ex2);
			Toast("sort error: " + ex2.Message);
		}
	}

	private static void RestoreOriginal(GameInventory inv, List<(GameItem it, int x, int y, int o, bool f)> original)
	{
		foreach (var item in original)
		{
			try
			{
				GridShape val = ShapeOf(item.it);
				GridShapeBuilder val2 = ((val != null) ? ((Il2CppObjectBase)val).TryCast<GridShapeBuilder>() : null);
				if (val2 != null)
				{
					val2.SetTransform(item.x, item.y, item.f, item.o);
				}
			}
			catch (System.Exception exR)
			{
				MelonLogger.Error("[InvSorter] restore SetTransform failed: " + exR.Message);
			}
		}
		try
		{
			inv.Validate();
		}
		catch (System.Exception exV)
		{
			MelonLogger.Error("[InvSorter] restore Validate failed: " + exV.Message);
		}
	}

	// 同类聚带(横带)布局: 逐 tag 连续横带(带底 = 前带 bottom), 带内用 MFR 池落位 —— 产品目标: 同类聚在一起.
	// task-6 修复(近满包原先 0/93 全失败): ①支撑改自支撑(HasSupportSelf, 厚件/首件可放)
	// ②落位失败先重算 MFR 池(增量 ShrinkRects 切块会丢空间) ③仍失败则全网格自支撑 first-fit(带底压缩/回退).
	// 逐 tag 连续带语义不变(同 tag 件不跨带交错): 只有整件在带区放不下时才允许落到带外空位; 任一件彻底无处可放 ⇒ 整次返回 null.
	private static Dictionary<GameItem, Placement> LayoutBanded(List<string> order, Dictionary<string, List<GameItem>> buckets, Dictionary<GameItem, ItemMask> masks, int W, int H, List<GameItem> fixedItems)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		Dictionary<GameItem, Placement> dictionary = new Dictionary<GameItem, Placement>();
		// MFR 增量缓存: 首次全扫, 每次放置后 ShrinkRects 增量切块(避免逐件全扫)
		List<(int x, int y, int w, int h)> rects = FindFreeRects(occ, W, H);
		int num = 0;
		foreach (string item in order)
		{
			int num2 = num;
			foreach (GameItem item2 in buckets[item])
			{
				// ① 带内(带底 num)落位; 支撑用自支撑版(自身格互撑 ⇒ 厚件/空网格首件可放)
				bool ok = PlaceInto(occ, W, H, masks[item2], num, out int bx, out int by, out int bo, out int bottom, rects, selfSupport: true);
				bool fromFallback = false;
				if (!ok)
				{
					// ② 增量 ShrinkRects 切块会丢可用空间(近满包尤甚) ⇒ 重算整个 MFR 池再试
					rects = FindFreeRects(occ, W, H);
					ok = PlaceInto(occ, W, H, masks[item2], num, out bx, out by, out bo, out bottom, rects, selfSupport: true);
				}
				if (!ok)
				{
					// ③ 带底压缩/回退: 允许落到带区之外的任意自支撑空位(仍不重叠/不越界/不压未动件)
					ok = PlaceFirstFit(occ, W, H, masks[item2], out bx, out by, out bo, out bottom);
					fromFallback = ok;
				}
				if (!ok)
				{
					return null; // 真正无处可放: 整次 grouped 作废, 由密集候选/降级残局兜底
				}
				dictionary[item2] = new Placement(bx, by, bo);
				ItemMask mm = masks[item2];
				int pw = (bo == 1 || bo == 3) ? mm.Gh0 : mm.Gw0;
				int ph = (bo == 1 || bo == 3) ? mm.Gw0 : mm.Gh0;
				if (fromFallback)
				{
					rects = FindFreeRects(occ, W, H); // 落点可能在缓存之外 ⇒ 缓存重算(保持「缓存 ⊆ 空闲」)
				}
				else
				{
					ShrinkRects(rects, bx, by, pw, ph);
				}
				if (bottom > num2)
				{
					num2 = bottom;
				}
			}
			num = num2;
		}
		return dictionary;
	}

	// 配对单元: 两物品互补成矩形, 落位后两个子物品各自 SetTransform
	private class PairUnit
	{
		public GameItem A;
		public GameItem B;
		public int OA;      // A 在单元 mask 内的朝向
		public int OB;      // B 在单元 mask 内的朝向
		public int Ax, Ay;  // A 在单元 mask 内的偏移
		public int Bx, By;  // B 在单元 mask 内的偏移
		public ItemMask M;  // 单元 mask(并集 cells)
	}

	private static Dictionary<GameItem, Placement> LayoutDense(List<GameItem> flat, Dictionary<GameItem, ItemMask> masks, int W, int H, List<GameItem> fixedItems)
	{
		// 算法组合: 并行跑多个独立布局器, 各返回完整 Placement 字典, 取"剩余最大连续空矩"最大者.
		// 数据驱动(verify_all 胜出统计): PairGrounded/GreedyBottom 从不胜出(0/12)已删除;
		// MinHole(胜6) + GrowTouch(胜5) + Shelf(胜5) 互补覆盖全部组, 组合零损失.
		List<Dictionary<GameItem, Placement>> candidates = new List<Dictionary<GameItem, Placement>>();
		// 配对单元(用于 MinHole 级联/堆叠叠放). 大仓才配对(配对 O(n^2) 有开销).
		List<object> paired = (W * H >= 100) ? BuildUnits(flat, masks) : null;
		List<object> singles = new List<object>(flat);
		singles.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		// ===== 原生语义候选(第 5 候选, 与旧 4 布局器同池) =====
		// 对齐游戏原生 InventorySortHelper.Sort 语义(docs/NATIVE_SORT_SPEC.md §3.2/§7 §9): 大件先占好位,
		// 小件按"行主序第一个能放的位置"自然落进缝隙(逐件 first-fit, 无评分/无 waste 比较).
		// 取舍(v5, Lead 裁定): 旧版"原生全放即早退"会把更优且更稳的择优换掉(实测空矩和 20003 vs 基线 20056,
		// churn 54.2% vs 8.7%), 但原生在部分会话确实更优(逐会话 更好 30 / 更差 60) ⇒ 改为把原生当第 5 个候选,
		// 与旧 4 布局器候选**同池 + 同精修层**择优: 取两者最优, 只在原生真正胜出时才承担它的位移代价.
		// 入选条件 = 原生全放(leftover==0), 否则作废(与原生"放不下即中止"一致, 也不引入部分布局).
		// 候选顺序 = 追加到旧候选之后 ⇒ 空矩持平时优先保留旧布局器结果(原生须严格更优才顶替, churn 更小).
		LayoutNativeFirstFit(fixedItems, flat, masks, W, H, out Dictionary<GameItem, Placement> dictNative, out int nativeLeftover);
		bool hasNative = dictNative != null && nativeLeftover == 0;
		long gridCells = (long)W * H;
		if (gridCells < 4000)
		{
			// 数据驱动(修复MinHole模拟bug后重扫描): MinHole 单算法胜0/空矩3239 已被包围, 删除(省算力 O(W^2H^2) 最贵).
			// GrowTouch + Guillotine(死洞惩罚) + LeftBottom + MFR: 组合 120/120 全胜 空矩9964.
			if (TryGrowTouch(fixedItems, flat, masks, W, H, out Dictionary<GameItem, Placement> dictGT))
			{
				candidates.Add(dictGT);
			}
			if (TryGuillotine(fixedItems, flat, masks, W, H, out Dictionary<GameItem, Placement> dictG))
			{
				candidates.Add(dictG);
			}
			// LeftBottom: 大背包左下锚定(17x10/11x14 漏网胜), 聚左下块留右上
			if (TryLeftBottom(fixedItems, flat, masks, W, H, out Dictionary<GameItem, Placement> dictLB))
			{
				candidates.Add(dictLB);
			}
			// BestFitMFR: MFR 池最小 waste, 高密度(10x10 total=67)胜
			if (TryPlaceMFR(fixedItems, flat, masks, W, H, out Dictionary<GameItem, Placement> dictMFR))
			{
				candidates.Add(dictMFR);
			}
			// 配对落地(PGSplit 思路): 互补配对单元整体落地, 数据驱动 9x7/10x10/14x21 胜出.
			// paired 已在 W*H>=100 构建(1134). 配对失败→SplitFailedUnit 拆死锁单元(配对拆两单件)重试,
			// 等价测试 pack_pg_split 的"配对失败拆单件救回"逻辑. 单件也放不下则丢弃候选, 由单件算法兜底.
			if (paired != null)
			{
				paired.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
				if (TryPlaceUnits(fixedItems, paired, masks, W, H, out Dictionary<GameItem, Placement> dictPair))
				{
					candidates.Add(dictPair);
				}
				else
				{
					List<object> repair = SplitFailedUnit(fixedItems, paired, masks, W, H);
					if (repair != null && TryPlaceUnits(fixedItems, repair, masks, W, H, out Dictionary<GameItem, Placement> dictRepair))
					{
						candidates.Add(dictRepair);
					}
				}
			}
		}
		else
		{
			// 超大网格(假想边界, 实际背包 <= 24x10 不会到这): MinHole 系列 O(W^2H^2) 会爆炸, 落地堆积兜底
			if (paired != null)
			{
				paired.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
				if (TryPlaceUnits(fixedItems, paired, masks, W, H, out Dictionary<GameItem, Placement> dict))
				{
					candidates.Add(dict);
				}
			}
			if (TryPlaceUnits(fixedItems, singles, masks, W, H, out Dictionary<GameItem, Placement> dict2))
			{
				candidates.Add(dict2);
			}
		}
		if (hasNative)
		{
			candidates.Add(dictNative); // 原生候选追加在末尾: 空矩持平则旧布局器优先(原生须严格更优才顶替)
		}
		// 同一精修层比较: 每个候选先各自 TryFillRefine(小件塞缝; 采纳条件保证各自非劣化), 再按最大连续空矩择优.
		// 否则"该候选是否被精修过"会左右胜负, 比较不公平. 代价 = 候选数(<=6)次精修; W*H>5000 时 TryFillRefine 原样返回.
		List<Dictionary<GameItem, Placement>> polished = new List<Dictionary<GameItem, Placement>>(candidates.Count);
		foreach (Dictionary<GameItem, Placement> raw in candidates)
		{
			polished.Add(TryFillRefine(fixedItems, raw, masks, W, H) ?? raw);
		}
		// 择优: 剩余最大连续空矩最大者
		Dictionary<GameItem, Placement> best = null;
		long bestArea = -1;
		foreach (Dictionary<GameItem, Placement> cand in polished)
		{
			bool[,] occ = new bool[W, H];
			foreach (GameItem fixedItem in fixedItems)
			{
				MarkCurrentCells(occ, W, H, fixedItem);
			}
			bool ok = true;
			foreach (KeyValuePair<GameItem, Placement> kv in cand)
			{
				if (!masks.TryGetValue(kv.Key, out ItemMask mm))
				{
					ok = false;
					break;
				}
				List<(int, int)> cs = CellsOf(mm, kv.Value.O);
				if (cs == null || cs.Count == 0)
				{
					cs = mm.C0;
				}
				// 堆叠物品在堆叠叠放候选中允许与已占格重叠(叠上去), 只标新空格; 其余物品必须全空格(不重叠)
				bool stackable = Stacked(kv.Key);
				int freshCells = 0;
				foreach ((int dx, int dy) in cs)
				{
					int cx = kv.Value.X + dx;
					int cy = kv.Value.Y + dy;
					if (cx < 0 || cy < 0 || cx >= W || cy >= H)
					{
						ok = false;
						break;
					}
					if (occ[cx, cy])
					{
						if (!stackable)
						{
							ok = false;
							break;
						}
						continue; // 堆叠物压已占格: 不重复标记
					}
					occ[cx, cy] = true;
					freshCells++;
				}
				// 堆叠物至少 1 格可见约束
				if (stackable && freshCells == 0)
				{
					ok = false;
					break;
				}
				if (!ok)
				{
					break;
				}
			}
			if (!ok)
			{
				continue;
			}
			long area = LargestEmptyArea(occ, W, H);
			if (area > bestArea)
			{
				bestArea = area;
				best = cand;
			}
		}
		// 精修已在择优前对每个候选完成(见上"同一精修层比较"), 胜出布局本身即精修结果, 此处不再重复精修.
		return best;
	}

	// ===== 原生首选落位(LayoutNativeFirstFit): 对齐游戏原生 InventorySortHelper.Sort 的落位语义 =====
	// 语义证据 docs/NATIVE_SORT_SPEC.md, 逐条对应: §7 排序键 / §3.2 控制流 / §5.2-5.3 层掩码 / §6 CellCount / §9 伪代码.
	//   排序键 = 物品占格数(CellCount = 形状局部非零单元数) 降序 → identifier(string.Compare Ordinal) →
	//            当前格行主序下标(y*W+x);
	//   落位   = 逐件"外 y 内 x"从 (0,0) 行主序扫描, 第一个能放的位置即胜出 ——
	//            无评分、无 waste 比较、无最大空矩比较(与旧 4 布局器 + LargestEmptyArea 择优的根本差别);
	//            扫描域 = [0, W-bboxW] × [0, H-bboxH](原生 maxX/maxY 口径, L 形也按 bbox 夹取);
	//            锚点 = 形状 min 角(原生 b.SetPosition(x,y), 与本文件 CellsOf 局部坐标同构);
	//   占用   = 单层占用(等价原生 itemLayers 全 0/1 位掩码; §5.2/§5.3 的 1<<(v&31) 退化为布尔占用).
	// 与原生两处有意差异(我方宽容, 用户裁定"大件最优 + 小件尽力塞缝"):
	//   ① 原生任一物品找不到位 → 整次 return false 中止; 我方记 leftover 继续排更小的件, 并由调用方决定取舍;
	//   ② 原生只用物品当前朝向(排序不改朝向); 我方当前朝向全网格扫不到时再试其余朝向, 换取填充率.
	// 安全不变量(与旧候选同口径): occ 起手只标 fixedItems(容器原位), 每件落位前 CellsFree 校验 + 界内夹取 →
	//   布局必然满足既有校验(不重叠、不压容器、界内); leftover>0 时调用方整次弃用, 不存在压住未移动件的泄漏.
	private static void LayoutNativeFirstFit(
		List<GameItem> fixedItems, List<GameItem> allItems, Dictionary<GameItem, ItemMask> masks,
		int W, int H, out Dictionary<GameItem, Placement> layout, out int leftover)
	{
		layout = new Dictionary<GameItem, Placement>();
		leftover = 0;
		bool[,] occ = new bool[W, H];
		foreach (GameItem f in fixedItems)
		{
			MarkCurrentCells(occ, W, H, f);
		}
		List<GameItem> order = new List<GameItem>(allItems);
		order.Sort((GameItem a, GameItem b) => NativeOrderCompare(a, b, masks, W));
		foreach (GameItem it in order)
		{
			ItemMask m = masks[it];
			int o0 = CurOri(it);
			int bx = -1;
			int by = -1;
			int bo = 0;
			for (int k = 0; k < 4; k++)
			{
				int o = (o0 + k) & 3;
				FindFirstFitSlot(occ, W, H, m, o, out bx, out by);
				if (bx >= 0)
				{
					bo = o;
					break;
				}
			}
			if (bx < 0)
			{
				leftover++; // 原生在此整次中止; 我方继续排更小的件
				continue;
			}
			MarkCells(occ, W, H, bx, by, CellsOf(m, bo), true);
			layout[it] = new Placement(bx, by, bo);
		}
	}

	// 原生排序键(§7): 占格数降序 → identifier Ordinal → 当前格行主序下标(§7 第三键).
	// 末键 uniqueId 收尾: 原生比较器对相同前两键物品不保证次序, 我方 List.Sort 不稳定, 用 uniqueId 定序防抖动.
	private static int NativeOrderCompare(GameItem a, GameItem b, Dictionary<GameItem, ItemMask> masks, int W)
	{
		int ca = masks[a].C0.Count;
		int cb = masks[b].C0.Count;
		if (ca != cb)
		{
			return cb - ca;
		}
		int ic = string.Compare(Ident(a), Ident(b), StringComparison.Ordinal);
		if (ic != 0)
		{
			return ic;
		}
		int pa = PosY(a) * W + PosX(a);
		int pb = PosY(b) * W + PosX(b);
		if (pa != pb)
		{
			return pa - pb;
		}
		return Uid(a).CompareTo(Uid(b));
	}

	// 原生扫描(§3.2): 外 y 内 x 从 (0,0) 起, 返回第一个可行位; 无评分/无 waste 比较; 扫描域按 bbox 夹取.
	private static void FindFirstFitSlot(bool[,] occ, int W, int H, ItemMask m, int o, out int bx, out int by)
	{
		bx = -1;
		by = -1;
		List<(int, int)> cells = CellsOf(m, o);
		if (cells == null || cells.Count == 0)
		{
			return;
		}
		int gw = (o == 1 || o == 3) ? m.Gh0 : m.Gw0;
		int gh = (o == 1 || o == 3) ? m.Gw0 : m.Gh0;
		int maxX = W - gw;
		int maxY = H - gh;
		if (maxX < 0 || maxY < 0)
		{
			return; // 该朝向比容器大(原生整次中止; 我方交调用方兜底)
		}
		for (int y = 0; y <= maxY; y++)
		{
			for (int x = 0; x <= maxX; x++)
			{
				if (CellsFree(occ, x, y, cells))
				{
					bx = x;
					by = y;
					return;
				}
			}
		}
	}

	// 物品当前朝向(BuildMask C0..C3 的索引); 与 TryResidualLayout 内联读取同义, 抽出来给原生路径复用.
	private static int CurOri(GameItem it)
	{
		try
		{
			GridShape sh = ShapeOf(it);
			if (sh != null)
			{
				return ((int)sh.orientation) & 3;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return 0;
	}

	// ===== 降级残局(Residual): 全放失败不再整包放弃 =====
	// 语义(离线 tscripts/bench_residual 对拍): 每件大先小后, 各自选"最贴合"空位(脚印浪费最小, 平手 y 再 x)
	// = 尽力把物品塞进能容纳的最小缝隙; 与严格布局器(任一放不下整候选作废)不同, 失败/无更优位则留原位.
	// 安全: 处理前预订全部物品(含 absorbed)当前 bbox 占位, 每件释放自身预订后选位, 原地不动者恢复占位 —
	// 保证落位永不与未动件重叠; 吸收件(同类被合并)原格全程保留, 应用阶段由 merge 重叠堆到代表件上.
	// ===== 残局填充精修(FillRefine): 填充算法放最后 =====
	// 语义(用户裁定 1+2): 不做预分类; 排完胜出布局后, 对每个非堆叠件(独占格可安全释放/重放)小件优先逐件
	// 释放自身格 → 找"能容纳它的最小空矩"(塞最小洞) → 在该空矩内取"贴邻已占物/壁最多"的位(贴邻),
	// 全局最大连续空矩不降才采纳(否则维持原位). 复用精确格, 逐件用 occ 校验不重叠, 安全不变量与残局一致.
	private static Dictionary<GameItem, Placement> TryFillRefine(
		List<GameItem> fixedItems, Dictionary<GameItem, Placement> placed,
		Dictionary<GameItem, ItemMask> masks, int W, int H)
	{
		if ((long)W * H > 5000)
		{
			return placed; // 超大网格全位扫描过贵, 跳过精修(维持原布局)
		}
		bool[,] occ = new bool[W, H];
		foreach (GameItem f in fixedItems)
		{
			MarkCurrentCells(occ, W, H, f);
		}
		bool occOk = true;
		foreach (KeyValuePair<GameItem, Placement> kv in placed)
		{
			if (!masks.TryGetValue(kv.Key, out ItemMask mm))
			{
				occOk = false;
				break;
			}
			List<(int, int)> cs = CellsOf(mm, kv.Value.O);
			if (cs == null || cs.Count == 0)
			{
				cs = mm.C0;
			}
			bool stackable = Stacked(kv.Key);
			int fresh = 0;
			foreach ((int dx, int dy) in cs)
			{
				int cx = kv.Value.X + dx;
				int cy = kv.Value.Y + dy;
				if (cx < 0 || cy < 0 || cx >= W || cy >= H)
				{
					occOk = false;
					break;
				}
				if (occ[cx, cy])
				{
					if (!stackable)
					{
						occOk = false;
						break;
					}
					continue;
				}
				occ[cx, cy] = true;
				fresh++;
			}
			if (stackable && fresh == 0)
			{
				occOk = false;
			}
			if (!occOk)
			{
				break;
			}
		}
		if (!occOk)
		{
			return placed; // 不应发生; 保守原样
		}
		long originalArea = LargestEmptyArea(occ, W, H);
		// 仅处理非堆叠件(独占格), 小件优先(脚印小者先)
		List<GameItem> order = new List<GameItem>();
		foreach (KeyValuePair<GameItem, Placement> kv in placed)
		{
			if (!Stacked(kv.Key))
			{
				order.Add(kv.Key);
			}
		}
		if (order.Count <= 1)
		{
			return placed;
		}
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)));
		Dictionary<GameItem, Placement> result = new Dictionary<GameItem, Placement>(placed);
		foreach (GameItem it in order)
		{
			if (!result.TryGetValue(it, out Placement cur))
			{
				continue;
			}
			if (!masks.TryGetValue(it, out ItemMask m))
			{
				continue;
			}
			List<(int, int)> curCells = CellsOf(m, cur.O);
			if (curCells == null || curCells.Count == 0)
			{
				curCells = m.C0;
			}
			// 释放自身格(独占, 安全)
			MarkCells(occ, W, H, cur.X, cur.Y, curCells, false);
			// 释放后当前位贴邻数(不含自身); 采纳守卫要求候选位贴邻不低于当前位, 防 FillRefine 把贴簇小件拆散进孤立袋
			long curTouch = TouchCount(occ, W, H, cur.X, cur.Y, curCells);
			// 候选: 找最小可容纳空矩(FindFreeRects 面积升序), 命中即在该空矩内取贴邻最多位
			List<(int x, int y, int w, int h)> rects = FindFreeRects(occ, W, H);
			rects.Sort((p, q) => ((long)p.w * p.h).CompareTo((long)q.w * q.h));
			bool found = false;
			Placement cand = default;
			long candTouch = -1;
			foreach (var R in rects)
			{
				long bestTouch = -1;
				Placement bestP = default;
				bool bestFound = false;
				for (int o = 0; o < 4; o++)
				{
					List<(int, int)> cs = CellsOf(m, o);
					if (cs == null || cs.Count == 0)
					{
						continue;
					}
					int gw = (o == 1 || o == 3) ? m.Gh0 : m.Gw0;
					int gh = (o == 1 || o == 3) ? m.Gw0 : m.Gh0;
					if (gw > R.w || gh > R.h)
					{
						continue;
					}
					for (int py = R.y; py + gh <= R.y + R.h; py++)
					{
						for (int px = R.x; px + gw <= R.x + R.w; px++)
						{
							if (!CellsFree(occ, px, py, cs))
							{
								continue;
							}
							long touch = 0;
							foreach ((int dx, int dy) in cs)
							{
								int cx2 = px + dx;
								int cy2 = py + dy;
								if (cx2 == 0 || cx2 == W - 1) touch++;
								if (cy2 == 0 || cy2 == H - 1) touch++;
								if (cx2 > 0 && occ[cx2 - 1, cy2]) touch++;
								if (cx2 < W - 1 && occ[cx2 + 1, cy2]) touch++;
								if (cy2 > 0 && occ[cx2, cy2 - 1]) touch++;
								if (cy2 < H - 1 && occ[cx2, cy2 + 1]) touch++;
							}
							if (!bestFound || touch > bestTouch || (touch == bestTouch && (py < bestP.Y || (py == bestP.Y && px < bestP.X))))
							{
								bestTouch = touch;
								bestP = new Placement(px, py, o);
								bestFound = true;
							}
						}
					}
				}
				if (bestFound)
				{
					cand = bestP;
					candTouch = bestTouch;
					found = true;
					break; // 最小可容纳空矩命中即定
				}
			}
			if (!found || (cand.X == cur.X && cand.Y == cur.Y && cand.O == cur.O))
			{
				MarkCells(occ, W, H, cur.X, cur.Y, curCells, true);
				continue;
			}
			// 试放候选位
			List<(int, int)> candCells = CellsOf(m, cand.O);
			if (candCells == null || candCells.Count == 0)
			{
				candCells = m.C0;
			}
			MarkCells(occ, W, H, cand.X, cand.Y, candCells, true);
			long newArea = LargestEmptyArea(occ, W, H);
			if (newArea >= originalArea && candTouch >= curTouch)
			{
				result[it] = cand; // 采纳(塞更小洞/更贴邻, 且空矩不降 + 不许把贴簇小件拆散)
			}
			else
			{
				// 回退
				MarkCells(occ, W, H, cand.X, cand.Y, candCells, false);
				MarkCells(occ, W, H, cur.X, cur.Y, curCells, true);
			}
		}
		return result;
	}

	private static Dictionary<GameItem, Placement> TryResidualLayout(
		List<GameItem> fixedItems, List<GameItem> allItems, List<GameItem> placeItems,
		Dictionary<GameItem, ItemMask> masks, int W, int H, out int relocated)
	{
		relocated = 0;
		// 当前精确格: BuildMask 按当前 flip 快照 C0, C1..C3 = C0 的 0/90/180/270 旋转;
		// 世界朝向 = ShapeOf.orientation, 精确格 = CellsOf(m, orientation)
		List<(int, int)> CurCells(ItemMask mm, GameItem it)
		{
			int o = 0;
			try
			{
				GridShape sh = ShapeOf(it);
				if (sh != null) o = (int)sh.orientation;
			}
			catch { }
			List<(int, int)> cs = CellsOf(mm, o);
			return (cs != null && cs.Count > 0) ? cs : mm.C0;
		}
		bool[,] occ = new bool[W, H];
		foreach (GameItem f in fixedItems)
		{
			MarkCurrentCells(occ, W, H, f);
		}
		// 预订全部物品(含 absorbed)当前精确格: 未处理件/留位件/吸收件原格永不被压.
		// 必须用精确格而非 bbox: bbox 覆盖邻件实占格, 误清后会让后续件落位重叠.
		foreach (GameItem it in allItems)
		{
			ItemMask mr = masks[it];
			MarkCells(occ, W, H, PosX(it), PosY(it), CurCells(mr, it), true);
		}
		Dictionary<GameItem, Placement> layout = new Dictionary<GameItem, Placement>();
		List<GameItem> order = new List<GameItem>(placeItems);
		order.Sort((a, b) => CellCount(b, masks).CompareTo(CellCount(a, masks)));
		foreach (GameItem it in order)
		{
			ItemMask m = masks[it];
			int ox0 = PosX(it);
			int oy0 = PosY(it);
			// 释放自身精确格(允许回原位)
			MarkCells(occ, W, H, ox0, oy0, CurCells(m, it), false);
			if (TryTightestSlot(occ, W, H, m, out int px, out int py, out int po))
			{
				List<(int, int)> cs = CellsOf(m, po);
				if (cs == null || cs.Count == 0)
				{
					cs = m.C0;
				}
				MarkCells(occ, W, H, px, py, cs, true);
				layout[it] = new Placement(px, py, po);
				if (px != ox0 || py != oy0)
				{
					relocated++;
				}
			}
			else
			{
				// 原地不动: 恢复自身精确格占位(不入 layout, 应用阶段不动它)
				MarkCells(occ, W, H, ox0, oy0, CurCells(m, it), true);
			}
		}
		return layout;
	}

	// 最贴合空位: 全朝向 x 全坐标中取 (脚印浪费 = bbox面积 - cells数 最小, 平手 minY 再 minX) 的空位
	private static bool TryTightestSlot(bool[,] occ, int W, int H, ItemMask m, out int px, out int py, out int po)
	{
		long best = long.MaxValue;
		px = -1;
		py = -1;
		po = 0;
		for (int ori = 0; ori < 4; ori++)
		{
			List<(int, int)> cs = CellsOf(m, ori);
			if (cs == null || cs.Count == 0)
			{
				continue;
			}
			int gw = (ori == 1 || ori == 3) ? m.Gh0 : m.Gw0;
			int gh = (ori == 1 || ori == 3) ? m.Gw0 : m.Gh0;
			if (gw > W || gh > H)
			{
				continue;
			}
			long waste = (long)gw * gh - cs.Count;
			for (int y = 0; y + gh <= H; y++)
			{
				for (int x = 0; x + gw <= W; x++)
				{
					if (!CellsFree(occ, x, y, cs))
					{
						continue;
					}
					long key = waste * 1000000L + (long)y * 100000L + x;
					if (key < best)
					{
						best = key;
						px = x;
						py = y;
						po = ori;
					}
				}
			}
		}
		return best != long.MaxValue;
	}

	// GrowTouch: 每物品取"触摸分最大"的位(相邻已占格+贴边计分), 碎片空间利用率优于行堆积.
	// 数据驱动: 小网格(11x14 等) GrowTouch 常胜, 与 MinHole 互补. 只处理单件(无配对).
	private static bool TryGrowTouch(List<GameItem> fixedItems, List<GameItem> singles, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		List<GameItem> order = new List<GameItem>(singles);
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		foreach (GameItem item in order)
		{
			ItemMask m = masks[item];
			long bestTouch = -1;
			int bestX = -1;
			int bestY = -1;
			int bestO = 0;
			for (int o = 0; o < 4; o++)
			{
				List<(int, int)> cells = CellsOf(m, o);
				if (cells == null || cells.Count == 0)
				{
					continue;
				}
				int gw = (o == 1 || o == 3) ? m.Gh0 : m.Gw0;
				int gh = (o == 1 || o == 3) ? m.Gw0 : m.Gh0;
				if (gw > W || gh > H)
				{
					continue;
				}
				for (int py = 0; py + gh <= H; py++)
				{
					for (int px = 0; px + gw <= W; px++)
					{
						if (!CellsFree(occ, px, py, cells))
						{
							continue;
						}
						// 触摸分: 每格相邻已占(4向) + 贴边计数
						long touch = 0;
						foreach ((int dx, int dy) in cells)
						{
							int cx = px + dx;
							int cy = py + dy;
							if (cx == 0 || cx == W - 1)
							{
								touch++;
							}
							if (cy == 0 || cy == H - 1)
							{
								touch++;
							}
							if (cx > 0 && occ[cx - 1, cy]) touch++;
							if (cx < W - 1 && occ[cx + 1, cy]) touch++;
							if (cy > 0 && occ[cx, cy - 1]) touch++;
							if (cy < H - 1 && occ[cx, cy + 1]) touch++;
						}
						if (touch > bestTouch || (touch == bestTouch && (py < bestY || (py == bestY && px < bestX))))
						{
							bestTouch = touch;
							bestX = px;
							bestY = py;
							bestO = o;
						}
					}
				}
			}
			if (bestX < 0)
			{
				return false;
			}
			dictionary[item] = new Placement(bestX, bestY, bestO);
			MarkCells(occ, W, H, bestX, bestY, CellsOf(m, bestO), val: true);
		}
		return true;
	}

	// Shelf: 行堆积. 按 w×h 降序, 每物品第一个可行位落在当前行基准之上. 小网格(8x8/14x21)常胜.
	// 只处理单件(无配对).

	// Guillotine 切割(GuillotineCut): Free rects 池, 每次选 waste 最小的候选放置, 放置后按割线切碎剩余空间为子矩形.
	// 碎片池能复用更多细小空间(比 Shelf 行堆积质量高约 60%), 代价是碎片矩形数略多. 只处理单件.
	private static bool TryGuillotine(List<GameItem> fixedItems, List<GameItem> singles, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		List<GameItem> order = new List<GameItem>(singles);
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		List<(int x, int y, int w, int h)> freerects = new List<(int x, int y, int w, int h)> { (0, 0, W, H) };
		// 全局最小物品包围盒: 碎片放不下任何物品(旋转后) 即死洞, 死洞面积计入评分
		// 碎片 (frw,frh) 能放物品 (gw,gh) 旋转 ⟺ min(gw,gh)<=min(frw,frh) && max(gw,gh)<=max(frw,frh)
		// 故取全局 minSide = min over items min(gw,gh); maxSide = min over items max(gw,gh)
		int minSide = int.MaxValue, maxSide = int.MaxValue;
		foreach (GameItem it in order)
		{
			ItemMask mm = masks[it];
			int m1 = Math.Min(mm.Gw0, mm.Gh0);
			int m2 = Math.Max(mm.Gw0, mm.Gh0);
			minSide = Math.Min(minSide, m1);
			maxSide = Math.Min(maxSide, m2);
		}
		if (minSide == int.MaxValue) { minSide = 1; maxSide = 1; }
		foreach (GameItem item in order)
		{
			ItemMask m = masks[item];
			// 选 评分最小候选 (free rect + 朝向): waste + 0.1*死洞面积
			int bestFi = -1;
			int bestO = 0;
			long bestScore = long.MaxValue;
			for (int fi = 0; fi < freerects.Count; fi++)
			{
				(var frx, var fry, var frw, var frh) = freerects[fi];
				for (int o = 0; o < 4; o++)
				{
					List<(int, int)> cells = CellsOf(m, o);
					if (cells == null || cells.Count == 0) continue;
					int gw = (o == 1 || o == 3) ? m.Gh0 : m.Gw0;
					int gh = (o == 1 || o == 3) ? m.Gw0 : m.Gh0;
					if (gw > frw || gh > frh) continue;
					int waste = frw * frh - gw * gh;
					// 死洞: 割裂产生的碎片中放不下任意物品(旋转后)的碎片面积
					long dead = 0;
					int dRight = frw - gw;
					int dBelow = frh - gh;
					if (dBelow > 0)
					{
						int mn = Math.Min(frw, dBelow), mx = Math.Max(frw, dBelow);
						if (mn < minSide || mx < maxSide) dead += (long)frw * dBelow;
					}
					if (dRight > 0)
					{
						int mn = Math.Min(dRight, frh), mx = Math.Max(dRight, frh);
						if (mn < minSide || mx < maxSide) dead += (long)dRight * frh;
					}
					long score = waste * 10 + dead; // 等价 waste + 0.1*dead (整型避免浮点)
					if (score < bestScore)
					{
						bestScore = score;
						bestFi = fi;
						bestO = o;
					}
				}
			}
			if (bestFi < 0)
			{
				return false;
			}
			(var bx, var by, var bw, var bh) = freerects[bestFi];
			List<(int, int)> useCells = CellsOf(m, bestO);
			// 物品落在 free rect 左上角
			dictionary[item] = new Placement(bx, by, bestO);
			MarkCells(occ, W, H, bx, by, useCells, val: true);
						// 割裂: 下碎片(整宽) + 右碎片(底部, 高度=该rect高)
			int itemGw = (bestO == 1 || bestO == 3) ? m.Gh0 : m.Gw0;
			int itemGh = (bestO == 1 || bestO == 3) ? m.Gw0 : m.Gh0;
			int right = bw - itemGw;
			int below = bh - itemGh;
			freerects.RemoveAt(bestFi);
			if (below > 0) freerects.Add((bx, by + itemGh, bw, below));
			if (right > 0) freerects.Add((bx + itemGw, by, right, bh));
			// 去包含: 去除被更大矩形覆盖的碎片
			FreerectDedup(freerects);
		}
		return true;
	}

	private static void FreerectDedup(List<(int x, int y, int w, int h)> rects)
	{
		for (int i = rects.Count - 1; i >= 0; i--)
		{
			var r = rects[i];
			if (r.w <= 0 || r.h <= 0)
			{
				rects.RemoveAt(i);
				continue;
			}
			bool covered = false;
			for (int j = 0; j < rects.Count; j++)
			{
				if (i == j) continue;
				var o = rects[j];
				if (o.x <= r.x && o.y <= r.y && o.x + o.w >= r.x + r.w && o.y + o.h >= r.y + r.h)
				{
					covered = true;
					break;
				}
			}
			if (covered) rects.RemoveAt(i);
		}
	}

	// LeftBottom: 左下角锚定. 物品偏好放最左下(px 最小优先, py 最大优先), 聚成左下紧块, 留右上整块.
	// 大背包(17x10/11x14)常胜: 左下凝聚使剩余集中在右上, 好放更大物品. 只处理单件(无配对).
	private static bool TryLeftBottom(List<GameItem> fixedItems, List<GameItem> singles, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		List<GameItem> order = new List<GameItem>(singles);
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		foreach (GameItem item in order)
		{
			ItemMask m = masks[item];
			int bestX = -1;
			int bestY = -1;
			int bestO = 0;
			for (int o = 0; o < 4; o++)
			{
				List<(int, int)> cells = CellsOf(m, o);
				if (cells == null || cells.Count == 0)
				{
					continue;
				}
				int gw = (o == 1 || o == 3) ? m.Gh0 : m.Gw0;
				int gh = (o == 1 || o == 3) ? m.Gw0 : m.Gh0;
				if (gw > W || gh > H)
				{
					continue;
				}
				for (int py = 0; py + gh <= H; py++)
				{
					for (int px = 0; px + gw <= W; px++)
					{
						if (!CellsFree(occ, px, py, cells))
						{
							continue;
						}
						// 左下优先: px 最小优先, 并列时 py 最大(更靠底)
						if (bestX < 0 || px < bestX || (px == bestX && py > bestY))
						{
							bestX = px;
							bestY = py;
							bestO = o;
						}
					}
				}
			}
			if (bestX < 0)
			{
				return false;
			}
			dictionary[item] = new Placement(bestX, bestY, bestO);
			MarkCells(occ, W, H, bestX, bestY, CellsOf(m, bestO), val: true);
		}
		return true;
	}

	// BestFitMFR: MFR 池最小 waste 选位. 物品落在空闲矩形最小浪费处, 高密度(10x10 total=67)常胜.
	// 用 FindFreeRects 算空闲矩形池, 每放一件 ShrinkRects 增量切块(免逐件全扫). 只处理单件(无配对).
	private static bool TryPlaceMFR(List<GameItem> fixedItems, List<GameItem> singles, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		List<GameItem> order = new List<GameItem>(singles);
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		List<(int x, int y, int w, int h)> rects = FindFreeRects(occ, W, H);
		foreach (GameItem item in order)
		{
			if (!PlaceInto(occ, W, H, masks[item], 0, out var bx, out var by, out var bo, out var bottom, rects))
			{
				return false;
			}
			dictionary[item] = new Placement(bx, by, bo);
			ItemMask mm = masks[item];
			int pw = (bo == 1 || bo == 3) ? mm.Gh0 : mm.Gw0;
			int ph = (bo == 1 || bo == 3) ? mm.Gw0 : mm.Gh0;
			ShrinkRects(rects, bx, by, pw, ph);
		}
		return true;
	}



	private static long LargestEmptyArea(bool[,] occ, int W, int H)
	{
		long best = 0;
		if (_histBuf == null || _histBuf.Length < W)
		{
			_histBuf = new int[W];
		}
		if (_stackBuf == null || _stackBuf.Length < W + 1)
		{
			_stackBuf = new int[W + 1];
		}
		int[] heights = _histBuf;
		int[] stack = _stackBuf;
		for (int y = 0; y < H; y++)
		{
			for (int x = 0; x < W; x++)
			{
				heights[x] = occ[x, y] ? 0 : heights[x] + 1;
			}
			int top = -1;
			for (int x = 0; x <= W; x++)
			{
				int h = (x < W) ? heights[x] : 0;
				while (top >= 0 && heights[stack[top]] > h)
				{
					int idx = stack[top--];
					int left = (top >= 0) ? stack[top] + 1 : 0;
					long area = (long)heights[idx] * (x - left);
					if (area > best)
					{
						best = area;
					}
				}
				if (x < W)
				{
					stack[++top] = x;
				}
			}
		}
		return best;
	}

	// 找出配对路径第一次死锁的单元, 若为 PairUnit 则仅拆开该对(重放至死锁点), 其余单元原样; 若死锁点非配对(单件也放不下)则返回 null
	private static List<object> SplitFailedUnit(List<GameItem> fixedItems, List<object> units, Dictionary<GameItem, ItemMask> masks, int W, int H)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		int minRow = MinOccRow(occ, W, H);
		List<object> replay = new List<object>();
		foreach (object unit in units)
		{
			ItemMask probe = unit is PairUnit pu ? pu.M : masks[(GameItem)unit];
			if (!PlaceGrounded(occ, W, H, probe, minRow, out var bx, out var by, out var bo))
			{
				if (unit is PairUnit pf)
				{
					int failIdx = replay.Count;
					replay.Add(pf.A);
					replay.Add(pf.B);
					if (failIdx + 1 < units.Count)
					{
						replay.AddRange(units.GetRange(failIdx + 1, units.Count - failIdx - 1));
					}
					return replay;
				}
				return null;
			}
			if (unit is PairUnit pa)
			{
				MarkCells(occ, W, H, bx, by, pa.M.C0, val: true);
				if (by < minRow)
				{
					minRow = by;
				}
			}
			else
			{
				MarkCells(occ, W, H, bx, by, CellsOf(masks[(GameItem)unit], bo), val: true);
				if (by < minRow)
				{
					minRow = by;
				}
			}
			replay.Add(unit);
		}
		return null;
	}

	// 落地凝聚堆积(自底向上 skyline): 物品从底部凝聚, 顶部剩余一整块连续矩形
	// 返回 false 时记录失败的单元(拆件fallback用)
	private static bool TryPlaceUnits(List<GameItem> fixedItems, List<object> units, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		int minRow = MinOccRow(occ, W, H);
		foreach (object unit in units)
		{
			if (unit is PairUnit pu)
			{
				if (!PlaceGrounded(occ, W, H, pu.M, minRow, out var bx, out var by, out var bo))
				{
					return false;
				}
				dictionary[pu.A] = new Placement(bx + pu.Ax, by + pu.Ay, pu.OA);
				dictionary[pu.B] = new Placement(bx + pu.Bx, by + pu.By, pu.OB);
				MarkCells(occ, W, H, bx, by, pu.M.C0, val: true);
				if (by < minRow)
				{
					minRow = by;
				}
			}
			else
			{
				GameItem item = (GameItem)unit;
				ItemMask mm = masks[item];
				if (!PlaceGrounded(occ, W, H, mm, minRow, out var bx2, out var by2, out var bo2))
				{
					return false;
				}
				dictionary[item] = new Placement(bx2, by2, bo2);
				MarkCells(occ, W, H, bx2, by2, CellsOf(mm, bo2), val: true);
				if (by2 < minRow)
				{
					minRow = by2;
				}
			}
		}
		return true;
	}

	private static int CellCount(object unit, Dictionary<GameItem, ItemMask> masks)
	{
		if (unit is PairUnit pu)
		{
			return pu.M.C0.Count;
		}
		return masks[(GameItem)unit].C0.Count;
	}

	// 当前占用的最浅行(最小 y); 空背包返回 H
	private static int MinOccRow(bool[,] occ, int W, int H)
	{
		for (int y = 0; y < H; y++)
		{
			for (int x = 0; x < W; x++)
			{
				if (occ[x, y])
				{
					return y;
				}
			}
		}
		return H;
	}

	// 落地支撑: 至少一格贴地(y+1==H) 或 贴已放块(下方 occ)
	private static bool Grounded(bool[,] occ, int W, int H, int x, int y, List<(int dx, int dy)> cells)
	{
		foreach (var cell in cells)
		{
			int cx = x + cell.dx;
			int cy = y + cell.dy;
			if (cy == H - 1 || (cy + 1 < H && occ[cx, cy + 1]))
			{
				return true;
			}
		}
		return false;
	}

	// 落地凝聚堆积: 对单单元, 从最深行向浅扫(行内左到右), 4 朝向; 条件 = 无重叠 + 落地支撑.
	// 最浅可放行受 minRow 约束: 再浅(更小的 y)没有任何支撑来源. 取(最深,最左)位置.
	private static bool PlaceGrounded(bool[,] occ, int W, int H, ItemMask m, int minRow, out int bx, out int by, out int bo)
	{
		bx = -1;
		by = -1;
		bo = 0;
		for (int o = 0; o < 4; o++)
		{
			List<(int, int)> cells = CellsOf(m, o);
			if (cells == null || cells.Count == 0)
			{
				continue;
			}
			int gw = (o == 1 || o == 3) ? m.Gh0 : m.Gw0;
			int gh = (o == 1 || o == 3) ? m.Gw0 : m.Gh0;
			if (gw > W || gh > H)
			{
				continue;
			}
			int pyEnd = Math.Max(0, minRow - gh);
			for (int py = H - gh; py >= pyEnd; py--)
			{
				for (int px = 0; px + gw <= W; px++)
				{
					if (!CellsFree(occ, px, py, cells))
					{
						continue;
					}
					if (!Grounded(occ, W, H, px, py, cells))
					{
						continue;
					}
					if (by < 0 || py > by || (py == by && px < bx))
					{
						bx = px;
						by = py;
						bo = o;
					}
					goto nextOrient;
				}
			}
			nextOrient:;
		}
		return by >= 0;
	}

	// 全局两两互补配对(含跨 bbox 尺寸的凸凹咬合), 返回混合单元列表
	private static List<object> BuildUnits(List<GameItem> flat, Dictionary<GameItem, ItemMask> masks)
	{
		List<object> result = new List<object>();
		HashSet<GameItem> used = new HashSet<GameItem>();
		int n = flat.Count;
		for (int i = 0; i < n; i++)
		{
			GameItem a = flat[i];
			if (used.Contains(a))
			{
				continue;
			}
			PairUnit best = null;
			int bestScore = 0;
			ItemMask ma = masks[a];
			// 遍历所有未用 b, 找到互补并集面积最大者(跨尺寸)
			for (int j = i + 1; j < n; j++)
			{
				GameItem b = flat[j];
				if (used.Contains(b))
				{
					continue;
				}
				PairUnit u = TryComplement(ma, masks[b]);
				if (u != null)
				{
					u.A = a;
					u.B = b;
					int score = u.M.Gw0 * u.M.Gh0; // 越大越值得配
					// 同类同mask优先: C0 序列相等加极大分
					if (SameShape(ma, masks[b])) score += 1000000;
					if (score > bestScore)
					{
						best = u;
						bestScore = score;
					}
				}
			}
			if (best != null)
			{
				result.Add(best);
				used.Add(best.A);
				used.Add(best.B);
			}
			else
			{
				result.Add(a);
				used.Add(a);
			}
		}
		return result;
	}

	// 两 mask 形状相同: C0 尺寸+格子序列一致
	private static bool SameShape(ItemMask a, ItemMask b)
	{
		if (a.Gw0 != b.Gw0 || a.Gh0 != b.Gh0 || a.C0.Count != b.C0.Count) return false;
		for (int i = 0; i < a.C0.Count; i++)
		{
			if (a.C0[i].dx != b.C0[i].dx || a.C0[i].dy != b.C0[i].dy) return false;
		}
		return true;
	}

	// 尝试两 mask 互补成矩形: A 固定朝向, B 试所有嵌合偏移(凸出塞进内凹, bbox 相交), 并集须填满并集 bb 无孔且格不重叠
	private static PairUnit TryComplement(ItemMask ma, ItemMask mb)
	{
		// 先看 bb 面积: 若并集不可能为矩形(bb 不匹配)快速跳过
		for (int oa = 0; oa < 4; oa++)
		{
			List<(int, int)> ca = CellsOf(ma, oa);
			int aw = (oa == 1 || oa == 3) ? ma.Gh0 : ma.Gw0;
			int ah = (oa == 1 || oa == 3) ? ma.Gw0 : ma.Gh0;
			for (int ob = 0; ob < 4; ob++)
			{
				List<(int, int)> cb = CellsOf(mb, ob);
				int bw = (ob == 1 || ob == 3) ? mb.Gh0 : mb.Gw0;
				int bh = (ob == 1 || ob == 3) ? mb.Gw0 : mb.Gh0;
				// B 凸出塞进 A 内凹(bbox 相交咬合) 或 拼在 A 右侧/下方(bbox 边缘相接)
				// dx/dy 下限保证 B 不整块滑出 A 左上之外, 上限 aw/ah 含标准的右/下并列情形
				for (int dx = -(bw - 1); dx <= aw; dx++)
				{
					for (int dy = -(bh - 1); dy <= ah - 1; dy++)
					{
						if (TryFit(ca, aw, ah, cb, bw, bh, dx, dy, out var rx, out var ry, out var rw, out var rh, out var offBx, out var offBy))
						{
							return MakeUnit(ca, aw, ah, oa, cb, bw, bh, ob, rx, ry, rw, rh, offBx, offBy);
						}
					}
				}
			}
		}
		return null;
	}

	private static bool TryFit(List<(int, int)> ca, int aw, int ah, List<(int, int)> cb, int bw, int bh, int dx, int dy, out int rx, out int ry, out int rw, out int rh, out int obx, out int oby)
	{
		// B 平移到 A 的 (dx,dy) 处, 计算并集 bb
		int minX = 0;
		int minY = 0;
		int maxX = aw;
		int maxY = ah;
		if (dx < minX) minX = dx;
		if (dy < minY) minY = dy;
		if (dx + bw > maxX) maxX = dx + bw;
		if (dy + bh > maxY) maxY = dy + bh;
		rw = maxX - minX;
		rh = maxY - minY;
		// 并集必须填满 rw*rh 个格子(无孔)
		bool[,] grid = new bool[rw, rh];
		int filled = 0;
		// A 格子全部落在并集内合法区域
		foreach (var (cx, cy) in ca)
		{
			int gx = cx - minX;
			int gy = cy - minY;
			if (gx >= 0 && gy >= 0 && gx < rw && gy < rh && !grid[gx, gy])
			{
				grid[gx, gy] = true;
				filled++;
			}
		}
		// B 格子: 须落合法区, 且与 A 不重叠(凸出塞进内凹时 bbox 相交, 必须排除格子重叠)
		foreach (var (cx, cy) in cb)
		{
			int gx = cx + dx - minX;
			int gy = cy + dy - minY;
			if (gx < 0 || gy < 0 || gx >= rw || gy >= rh)
			{
				rx = ry = obx = oby = 0;
				return false;
			}
			if (grid[gx, gy])
			{
				rx = ry = obx = oby = 0;
				return false;
			}
			grid[gx, gy] = true;
			filled++;
		}
		if (filled != rw * rh)
		{
			rx = ry = obx = oby = 0;
			return false;
		}
		rx = minX;
		ry = minY;
		obx = dx;
		oby = dy;
		return true;
	}

	private static PairUnit MakeUnit(List<(int, int)> ca, int aw, int ah, int oa, List<(int, int)> cb, int bw, int bh, int ob, int rx, int ry, int rw, int rh, int obx, int oby)
	{
		PairUnit u = new PairUnit();
		u.OA = oa;
		u.OB = ob;
		u.Ax = -rx;
		u.Ay = -ry;
		u.Bx = obx - rx;
		u.By = oby - ry;
		List<(int, int)> cells = new List<(int, int)>();
		foreach (var (cx, cy) in ca)
		{
			cells.Add((cx - rx, cy - ry));
		}
		foreach (var (cx, cy) in cb)
		{
			cells.Add((cx + obx - rx, cy + oby - ry));
		}
		u.M = new ItemMask
		{
			C0 = cells,
			Gw0 = rw,
			Gh0 = rh,
			Square = rw == rh
		};
		return u;
	}

	private static List<(int, int)> CellsOf(ItemMask m, int o)
	{
		return o switch
		{
			0 => m.C0,
			1 => m.C1,
			2 => m.C2,
			_ => m.C3
		};
	}

	// 增量 MFR: 放置后把被占矩形从空闲池切掉
	private static void ShrinkRects(List<(int x, int y, int w, int h)> rects, int px, int py, int pw, int ph)
	{
		List<(int x, int y, int w, int h)> next = new List<(int, int, int, int)>();
		foreach (var r in rects)
		{
			if (r.x + r.w <= px || px + pw <= r.x || r.y + r.h <= py || py + ph <= r.y)
			{
				next.Add(r); // 不相交
				continue;
			}
			// 切左右
			if (px > r.x)
			{
				next.Add((r.x, r.y, px - r.x, r.h));
			}
			if (px + pw < r.x + r.w)
			{
				next.Add((px + pw, r.y, r.x + r.w - (px + pw), r.h));
			}
			// 切上下(中间段)
			int cx = Math.Max(r.x, px);
			int cx2 = Math.Min(r.x + r.w, px + pw);
			if (cx < cx2)
			{
				if (py > r.y)
				{
					next.Add((cx, r.y, cx2 - cx, py - r.y));
				}
				if (py + ph < r.y + r.h)
				{
					next.Add((cx, py + ph, cx2 - cx, r.y + r.h - (py + ph)));
				}
			}
		}
		// 去包含
		rects.Clear();
		for (int i = 0; i < next.Count; i++)
		{
			var r = next[i];
			bool covered = false;
			for (int j = 0; j < next.Count; j++)
			{
				if (i == j)
				{
					continue;
				}
				var o = next[j];
				if (o.x <= r.x && o.y <= r.y && o.x + o.w >= r.x + r.w && o.y + o.h >= r.y + r.h)
				{
					covered = true;
					break;
				}
			}
			if (!covered)
			{
				rects.Add(r);
			}
		}
	}

	private static bool PlaceInto(bool[,] occ, int W, int H, ItemMask m, int minY, out int bx, out int by, out int bo, out int bottom, List<(int x, int y, int w, int h)> cachedRects = null, bool selfSupport = false)
	{
		bx = -1;
		by = -1;
		bo = 0;
		bottom = -1;
		// MFR 池算一次(或用缓存), 4 朝向复用
		List<(int x, int y, int w, int h)> rects = cachedRects ?? FindFreeRects(occ, W, H);
		for (int i = 0; i < 2; i++)
		{
			int minY2 = ((i == 0) ? minY : 0);
			for (int j = 0; j < 4; j++)
			{
				List<(int, int)> list = null;
				int gw = 0;
				int gh = 0;
				switch (j)
				{
					case 0:
						list = m.C0;
						gw = m.Gw0;
						gh = m.Gh0;
						break;
					case 1:
						list = m.C1;
						gw = m.Gw1;
						gh = m.Gh1;
						break;
					case 2:
						list = m.C2;
						gw = m.Gw2;
						gh = m.Gh2;
						break;
					default:
						list = m.C3;
						gw = m.Gw3;
						gh = m.Gh3;
						break;
				}
				if (list != null && list.Count != 0)
				{
					if (FindFreeSpotCells(rects, occ, W, H, list, gw, gh, minY2, out var ox, out var oy, out long waste, selfSupport) && (bx < 0 || oy < by || (oy == by && ox < bx)))
					{
					bx = ox;
					by = oy;
					bo = j;
					}
					if (j == 0 && m.Square)
					{
						break;
					}
				}
			}
			if (bx >= 0 || minY <= 0)
			{
				break;
			}
		}
		if (bx < 0)
		{
			return false;
		}
		List<(int, int)> list2;
		switch (bo)
		{
			case 1: list2 = m.C1; break;
			case 2: list2 = m.C2; break;
			case 3: list2 = m.C3; break;
			default: list2 = m.C0; break;
		}
		MarkCells(occ, W, H, bx, by, list2, val: true);
		bottom = 0;
		foreach (var item in list2)
		{
			if (by + item.Item2 + 1 > bottom)
			{
				bottom = by + item.Item2 + 1;
			}
		}
		return true;
	}

	private static ItemMask BuildMask(GameItem it)
	{
		ItemMask itemMask = new ItemMask();
		int bw;
		int bh;
		List<(int, int)> list = (itemMask.C0 = ReadMask(it, out bw, out bh));
		itemMask.Gw0 = bw;
		itemMask.Gh0 = bh;
		List<(int, int)> list2 = new List<(int, int)>();
		foreach (var item in list)
		{
			list2.Add((bh - 1 - item.Item2, item.Item1));
		}
		itemMask.C1 = list2;
		itemMask.Gw1 = bh;
		itemMask.Gh1 = bw;
		// rot180: (dx,dy) -> (bw-1-dx, bh-1-dy), 尺寸不变
		List<(int, int)> list3 = new List<(int, int)>();
		foreach (var item3 in list)
		{
			list3.Add((bw - 1 - item3.Item1, bh - 1 - item3.Item2));
		}
		itemMask.C2 = list3;
		itemMask.Gw2 = bw;
		itemMask.Gh2 = bh;
		// rot270: (dx,dy) -> (dy, bw-1-dx), 尺寸 (bh x bw)
		List<(int, int)> list4 = new List<(int, int)>();
		foreach (var item4 in list)
		{
			list4.Add((item4.Item2, bw - 1 - item4.Item1));
		}
		itemMask.C3 = list4;
		itemMask.Gw3 = bh;
		itemMask.Gh3 = bw;
		itemMask.Square = bw == bh;
		return itemMask;
	}

	private static List<(int dx, int dy)> ReadMask(GameItem it, out int bw, out int bh)
	{
		List<(int, int)> list = new List<(int, int)>();
		bw = 1;
		bh = 1;
		GridShape val = ShapeOf(it);
		GridShapeBuilder val2 = null;
		try
		{
			val2 = ((val != null) ? ((Il2CppObjectBase)val).TryCast<GridShapeBuilder>() : null);
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		if (val2 != null)
		{
			try
			{
				bool flag = false;
				try
				{
					flag = val2.flipped;
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
				val2.SetTransform(0, 0, flag, 0);
				int num = Math.Max(1, val.width);
				int num2 = Math.Max(1, val.height);
				int num3 = int.MaxValue;
				int num4 = int.MaxValue;
				int num5 = -1;
				int num6 = -1;
				List<(int, int)> list2 = new List<(int, int)>();
				for (int i = 0; i < num; i++)
				{
					for (int j = 0; j < num2; j++)
					{
						byte b = 0;
						try
						{
							b = val.GetLocal(i, j);
						}
						catch
						{
							// ponytail: IL2CPP native probe, silent fallback
						}
						if (b != 0)
						{
							list2.Add((i, j));
							if (i < num3)
							{
								num3 = i;
							}
							if (j < num4)
							{
								num4 = j;
							}
							if (i > num5)
							{
								num5 = i;
							}
							if (j > num6)
							{
								num6 = j;
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					foreach (var (num7, num8) in list2)
					{
						list.Add((num7 - num3, num8 - num4));
					}
					bw = num5 - num3 + 1;
					bh = num6 - num4 + 1;
					return list;
				}
			}
			catch
			{
				list.Clear();
			}
		}
		int num9 = BaseW(it);
		int num10 = BaseH(it);
		bw = num9;
		bh = num10;
		for (int k = 0; k < num9; k++)
		{
			for (int l = 0; l < num10; l++)
			{
				list.Add((k, l));
			}
		}
		return list;
	}

	// Maximal Free Rectangles: 直方图+单调栈枚举所有不被包含的最大空闲矩形
	private static List<(int x, int y, int w, int h)> FindFreeRects(bool[,] occ, int W, int H)
	{
		List<(int, int, int, int)> rects = new List<(int, int, int, int)>();
		int[] height = new int[W];
		for (int y = 0; y < H; y++)
		{
			for (int x = 0; x < W; x++)
			{
				height[x] = occ[x, y] ? 0 : height[x] + 1;
			}
			Stack<int> stack = new Stack<int>();
			for (int x = 0; x <= W; x++)
			{
				int cur = (x == W) ? 0 : height[x];
				while (stack.Count > 0 && height[stack.Peek()] >= cur)
				{
					int h = height[stack.Pop()];
					int left = (stack.Count == 0) ? 0 : stack.Peek() + 1;
					int right = x - 1;
					if (h > 0)
					{
						rects.Add((left, y - h + 1, right - left + 1, h));
					}
				}
				stack.Push(x);
			}
		}
		// 去包含: 只留不被其他矩形完全覆盖的
		List<(int, int, int, int)> kept = new List<(int, int, int, int)>();
		for (int i = 0; i < rects.Count; i++)
		{
			(int x, int y, int w, int h) r = rects[i];
			bool covered = false;
			for (int j = 0; j < rects.Count; j++)
			{
				if (i == j)
				{
					continue;
				}
				(int x, int y, int w, int h) o = rects[j];
				if (o.x <= r.x && o.y <= r.y && o.x + o.w >= r.x + r.w && o.y + o.h >= r.y + r.h)
				{
					covered = true;
					break;
				}
			}
			if (!covered)
			{
				kept.Add(r);
			}
		}
		return kept;
	}

	// MFR + 连通聚块: 物品落在空闲矩形左上角, 且必须"撑住"(上/左/右邻居已占或靠边界),
	// 使所有物品从左上角单向生长成实心连通块 — 剩余空间变成右下角一整块连续矩形,
	// 好放入更大物品. 支撑约束是聚合的关键: 无支撑的物品会散开碎片化剩余空间.
	// 候选按左上优先(最小 y 再最小 x): 聚成紧实团块.
	private static bool FindFreeSpotCells(List<(int x, int y, int w, int h)> rects, bool[,] occ, int W, int H, List<(int dx, int dy)> cells, int gw, int gh, int minY, out int ox, out int oy, out long waste, bool selfSupport = false)
	{
		ox = 0;
		oy = 0;
		waste = long.MaxValue;
		if (gw > W || gh > H)
		{
			return false;
		}
		int topY = Math.Max(0, minY);
		foreach (var r in rects)
		{
			if (r.w < gw || r.h < gh || r.y + r.h <= topY)
			{
				continue;
			}
			// 物品钉在矩形左上角, 保持角锚定
			int px = r.x;
			int py = Math.Max(r.y, topY);
			if (py + gh > r.y + r.h)
			{
				continue;
			}
			if (!CellsFree(occ, px, py, cells))
			{
				continue;
			}
			if (!(selfSupport ? HasSupportSelf(occ, px, py, cells) : HasSupport(occ, px, py, cells)))
			{
				continue;
			}
			long w = (long)r.w * r.h - (long)gw * gh;
			// 左上优先(最小 y, 再最小 x); 首次命中直接用
			if (waste == long.MaxValue || py < oy || (py == oy && px < ox))
			{
				waste = w;
				ox = px;
				oy = py;
			}
		}
		return waste != long.MaxValue;
	}

	// 支撑: 物品每个格子需紧贴已占格或边界(上/下/左/右), 防止形成松散孤岛, 聚成实心块
	private static bool HasSupport(bool[,] occ, int x, int y, List<(int dx, int dy)> cells)
	{
		foreach (var cell in cells)
		{
			int cx = x + cell.dx;
			int cy = y + cell.dy;
			bool sup = cy == 0 || cx == 0
				|| (cx - 1 >= 0 && occ[cx - 1, cy])
				|| (cy - 1 >= 0 && occ[cx, cy - 1])
				|| (cy + 1 < occ.GetLength(1) && occ[cx, cy + 1]);
			if (!sup)
			{
				return false;
			}
		}
		return true;
	}

	// task-6 布局最大空矩(与 LayoutDense 择优循环同口径): fixedItems 用 bbox, 布局件用精确格, 堆叠件允许压已占格
	private static long EmptyAreaOfLayout(List<GameItem> fixedItems, Dictionary<GameItem, Placement> layout, Dictionary<GameItem, ItemMask> masks, int W, int H)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem f in fixedItems)
		{
			MarkCurrentCells(occ, W, H, f);
		}
		foreach (KeyValuePair<GameItem, Placement> kv in layout)
		{
			if (!masks.TryGetValue(kv.Key, out ItemMask mm))
			{
				continue;
			}
			List<(int, int)> cs = CellsOf(mm, kv.Value.O);
			if (cs == null || cs.Count == 0)
			{
				cs = mm.C0;
			}
			bool stackable = Stacked(kv.Key);
			foreach ((int dx, int dy) in cs)
			{
				int cx = kv.Value.X + dx;
				int cy = kv.Value.Y + dy;
				if (cx < 0 || cy < 0 || cx >= W || cy >= H)
				{
					continue;
				}
				if (occ[cx, cy] && !stackable)
				{
					continue; // 不该发生(安全不变量); 保守跳过不重复计
				}
				occ[cx, cy] = true;
			}
		}
		return LargestEmptyArea(occ, W, H);
	}

	// task-6: 同类聚带优先容差(占密集候选最大空矩的比例) — 原配置 BandedToleranceRatio, 现固化为常量 BandedToleranceConst
	//   0    = 横带空矩不劣于密集才选横带(空矩严格不退化)
	//   0.05 = 生产值: 用 <=5% 空矩代价换用户可见的同类聚带(实测拐点 3%; 5% 时 146/296=49.3% 会话聚带, 面积代价 0.22%)
	//   1    = 只要横带能全放就强制聚带
	// 曲线与实测: InventorySorter/tscripts/bench_banded.py
	private static double BandedToleranceRatio => BandedToleranceConst;

	// task-6 自支撑版支撑判据(仅横带路径使用; 密集路径仍用上面的 HasSupport, 不改其行为):
	// 每格需「贴首行/首列」或「邻格(左/上/下)已占」或「邻格属于本件自身」.
	// 原版自身格不计支撑 ⇒ 厚件在空网格当首件时逐格互不支撑, 整件放不下(离线诊断实测).
	// 仍非恒真: 漂浮孤立位(四邻无物且不在首行/首列)照旧不放 — 保留「聚成实心块」语义.
	private static readonly HashSet<int> _selfCells = new HashSet<int>();

	private static bool HasSupportSelf(bool[,] occ, int x, int y, List<(int dx, int dy)> cells)
	{
		int h = occ.GetLength(1);
		_selfCells.Clear();
		foreach ((int dx0, int dy0) in cells)
		{
			_selfCells.Add(dx0 * 1024 + dy0);
		}
		foreach ((int dx, int dy) in cells)
		{
			int cx = x + dx;
			int cy = y + dy;
			bool sup = cy == 0 || cx == 0
				|| (cx - 1 >= 0 && (occ[cx - 1, cy] || _selfCells.Contains((dx - 1) * 1024 + dy)))
				|| (cy - 1 >= 0 && (occ[cx, cy - 1] || _selfCells.Contains(dx * 1024 + (dy - 1))))
				|| (cy + 1 < h && (occ[cx, cy + 1] || _selfCells.Contains(dx * 1024 + (dy + 1))));
			if (!sup)
			{
				return false;
			}
		}
		return true;
	}

	// task-6 ③ 带底压缩/回退: 全网格(y 外层, x 内层)取第一个「界内 + 空 + 自支撑」的位(位置优先于朝向 = 原生扫描口径).
	// 仅在带内 MFR(含重算)都放不下时调用 ⇒ 允许物品落到带区之外的任意空位, 保住 grouped 成功.
	private static bool PlaceFirstFit(bool[,] occ, int W, int H, ItemMask m, out int bx, out int by, out int bo, out int bottom)
	{
		bx = -1;
		by = -1;
		bo = 0;
		bottom = 0;
		for (int y = 0; y < H; y++)
		{
			for (int x = 0; x < W; x++)
			{
				for (int o = 0; o < 4; o++)
				{
					List<(int, int)> cs;
					int gw;
					int gh;
					switch (o)
					{
						case 1:
							cs = m.C1;
							gw = m.Gw1;
							gh = m.Gh1;
							break;
						case 2:
							cs = m.C2;
							gw = m.Gw2;
							gh = m.Gh2;
							break;
						case 3:
							cs = m.C3;
							gw = m.Gw3;
							gh = m.Gh3;
							break;
						default:
							cs = m.C0;
							gw = m.Gw0;
							gh = m.Gh0;
							break;
					}
					if (cs == null || cs.Count == 0 || x + gw > W || y + gh > H)
					{
						continue;
					}
					if (!CellsFree(occ, x, y, cs) || !HasSupportSelf(occ, x, y, cs))
					{
						continue;
					}
					bx = x;
					by = y;
					bo = o;
					MarkCells(occ, W, H, x, y, cs, val: true);
					bottom = 0;
					foreach ((int dx2, int dy2) in cs)
					{
						if (y + dy2 + 1 > bottom)
						{
							bottom = y + dy2 + 1;
						}
					}
					return true;
				}
			}
		}
		return false;
	}

	private static long TouchCount(bool[,] occ, int W, int H, int x, int y, List<(int dx, int dy)> cells)
	{
		long t = 0;
		foreach ((int dx, int dy) in cells)
		{
			int cx = x + dx;
			int cy = y + dy;
			if (cx == 0 || cx == W - 1) t++;
			if (cy == 0 || cy == H - 1) t++;
			if (cx > 0 && occ[cx - 1, cy]) t++;
			if (cx < W - 1 && occ[cx + 1, cy]) t++;
			if (cy > 0 && occ[cx, cy - 1]) t++;
			if (cy < H - 1 && occ[cx, cy + 1]) t++;
		}
		return t;
	}

	private static bool CellsFree(bool[,] occ, int x, int y, List<(int dx, int dy)> cells)
	{
		foreach (var cell in cells)
		{
			if (occ[x + cell.dx, y + cell.dy])
			{
				return false;
			}
		}
		return true;
	}

	private static void MarkCells(bool[,] occ, int W, int H, int x, int y, List<(int dx, int dy)> cells, bool val)
	{
		foreach (var cell in cells)
		{
			int num = x + cell.dx;
			int num2 = y + cell.dy;
			if (num >= 0 && num2 >= 0 && num < W && num2 < H)
			{
				occ[num, num2] = val;
			}
		}
	}

	private static void MarkCurrentCells(bool[,] occ, int W, int H, GameItem it)
	{
		MarkBox(occ, W, H, PosX(it), PosY(it), BoxW(it), BoxH(it), val: true);
	}

	private static bool PlaceItem(GameItem it, int x, int y, int orient)
	{
		try
		{
			GridShape val = null;
			try
			{
				val = it.modifiedShape;
			}
			catch
			{
				// ponytail: IL2CPP native probe, silent fallback
			}
			if (val == null)
			{
				try
				{
					val = it.shape;
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
			}
			if (val == null)
			{
				return false;
			}
			GridShapeBuilder val2 = ((Il2CppObjectBase)val).TryCast<GridShapeBuilder>();
			if (val2 == null)
			{
				return false;
			}
			bool flag = false;
			try
			{
				flag = val2.flipped;
			}
			catch
			{
				// ponytail: IL2CPP native probe, silent fallback
			}
			val2.SetTransform(x, y, flag, orient);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static int SizeCompare(GameItem a, GameItem b)
	{
		int num = BaseW(a);
		int num2 = BaseH(a);
		int num3 = BaseW(b);
		int num4 = BaseH(b);
		int num5 = (num3 * num4).CompareTo(num * num2);
		if (num5 != 0)
		{
			return num5;
		}
		num5 = Math.Max(num3, num4).CompareTo(Math.Max(num, num2));
		if (num5 != 0)
		{
			return num5;
		}
		num5 = Math.Min(num3, num4).CompareTo(Math.Min(num, num2));
		if (num5 != 0)
		{
			return num5;
		}
		num5 = string.Compare(Ident(a), Ident(b), StringComparison.OrdinalIgnoreCase);
		if (num5 != 0)
		{
			return num5;
		}
		num5 = string.Compare(Name(a), Name(b), StringComparison.OrdinalIgnoreCase);
		if (num5 != 0)
		{
			return num5;
		}
		return Uid(a).CompareTo(Uid(b));
	}

	private static string PrimaryTag(GameItem it)
	{
		try
		{
			Il2CppSystem.Collections.Generic.List<string> itemTypes = it.itemTypes;
			if (itemTypes != null && itemTypes.Count > 0)
			{
				string text = itemTypes[0];
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return "";
	}

	private static string TagKey(GameItem it)
	{
		string text = PrimaryTag(it);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "\uffff";
	}

	private static bool GetGridDims(GameInventory inv, out int w, out int h)
	{
		w = 0;
		h = 0;
		try
		{
			GameGridInventory val = ((Il2CppObjectBase)inv).TryCast<GameGridInventory>();
			if (val != null)
			{
				GridShape inventoryShape = val.inventoryShape;
				if (inventoryShape != null)
				{
					w = Math.Max(inventoryShape.width, inventoryShape.maxX + 1);
					h = Math.Max(inventoryShape.height, inventoryShape.maxY + 1);
					return w > 0 && h > 0;
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		try
		{
			GameGridScrollableInventory val2 = ((Il2CppObjectBase)inv).TryCast<GameGridScrollableInventory>();
			if (val2 != null)
			{
				w = Math.Max(1, val2.width);
				h = 4096;
				return true;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return false;
	}

	private static GridShape ShapeOf(GameItem it)
	{
		GridShape val = null;
		try
		{
			val = it.modifiedShape;
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		if (val == null)
		{
			try
			{
				val = it.shape;
			}
			catch
			{
				// ponytail: IL2CPP native probe, silent fallback
			}
		}
		return val;
	}

	private static int BoxW(GameItem it)
	{
		try
		{
			GridShape val = ShapeOf(it);
			return (val == null) ? 1 : Math.Max(1, val.globalWidth);
		}
		catch
		{
			return 1;
		}
	}

	private static int BoxH(GameItem it)
	{
		try
		{
			GridShape val = ShapeOf(it);
			return (val == null) ? 1 : Math.Max(1, val.globalHeight);
		}
		catch
		{
			return 1;
		}
	}

	private static int PosX(GameItem it)
	{
		try
		{
			GridShape val = ShapeOf(it);
			return (val != null) ? val.minX : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static int PosY(GameItem it)
	{
		try
		{
			GridShape val = ShapeOf(it);
			return (val != null) ? val.minY : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static int BaseW(GameItem it)
	{
		try
		{
			GridShape val = ShapeOf(it);
			return (val == null) ? 1 : Math.Max(1, val.width);
		}
		catch
		{
			return 1;
		}
	}

	private static int BaseH(GameItem it)
	{
		try
		{
			GridShape val = ShapeOf(it);
			return (val == null) ? 1 : Math.Max(1, val.height);
		}
		catch
		{
			return 1;
		}
	}

	// 堆叠物品: unitCount > 1 (多份叠在一起); 排序最后放置使其渲染在上层, 至少一格可见
	private static bool Stacked(GameItem it)
	{
		try
		{
			return it.unitCount > 1;
		}
		catch
		{
			return false;
		}
	}

	private static void MarkBox(bool[,] occ, int W, int H, int x, int y, int bw, int bh, bool val)
	{
		for (int i = 0; i < bh; i++)
		{
			for (int j = 0; j < bw; j++)
			{
				int num = x + j;
				int num2 = y + i;
				if (num >= 0 && num2 >= 0 && num < W && num2 < H)
				{
					occ[num, num2] = val;
				}
			}
		}
	}

	private static bool HasContentWindow(GameItem it)
	{
		try
		{
			return it.contentWindow != null;
		}
		catch
		{
			return false;
		}
	}

	private static string Ident(GameItem it)
	{
		try
		{
			return it.identifier ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static string Name(GameItem it)
	{
		try
		{
			return it.name ?? "";
		}
		catch
		{
			return "";
		}
	}

	private static int Uid(GameItem it)
	{
		try
		{
			return it.uniqueId;
		}
		catch
		{
			return 0;
		}
	}

	private static void Toast(string msg)
	{
		LastAction = msg;
		_lastActionAt = Time.realtimeSinceStartup;
	}

	// ==================== 快捷键 / 最后打开的容器 / 自动排序 ====================

	// 「最后打开的容器」= visibleWindows 中 focusStamp 最大者(原生自增焦点戳, 打开/提权即刷新);
	// 返回值可能不是可排序容器(例如系统 UI), 由 SortableWindowInventory 再过滤。
	private static PixelWindow LastFocusedWindow()
	{
		try
		{
			WindowsHandler wh = WindowsHandler.current;
			if ((Object)(object)wh == (Object)null)
			{
				return null;
			}
			Il2CppSystem.Collections.Generic.List<PixelWindow> list = wh.visibleWindows;
			if (list == null || list.Count == 0)
			{
				return null;
			}
			PixelWindow best = null;
			long bestStamp = long.MinValue;
			for (int i = 0; i < list.Count; i++)
			{
				PixelWindow w = null;
				try
				{
					w = list[i];
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
				if (w == null)
				{
					continue;
				}
				long s;
				try
				{
					s = w.focusStamp;
				}
				catch
				{
					continue;
				}
				if (s >= bestStamp)
				{
					bestStamp = s;
					best = w;
				}
			}
			return best;
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
			return null;
		}
	}

	// 可排序性判定(与按钮列表同口径): 非槽位 inventory
	private static bool IsSortableInventory(GameInventory inv)
	{
		if (inv == null)
		{
			return false;
		}
		try
		{
			return ((Il2CppObjectBase)inv).TryCast<GameSlotInventory>() == null;
		}
		catch
		{
			return true;
		}
	}

	// 窗口显示名: 有标题用标题; 无标题的常驻背景存储(主仓库/展示柜等)用容量做标签。
	// 不写死任何容量尺寸 —— 容器升级/游戏新增容器都自动适配。返回 null = 此窗口不该出现在列表里。
	private static string WindowLabel(PixelWindow win, GameInventory inv)
	{
		if (win == null || inv == null)
		{
			return null;
		}
		string text = "";
		try
		{
			text = win.titleString;
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		int cells;
		try
		{
			InvInfo(inv, out var _, out cells);
		}
		catch
		{
			return null;
		}
		if (cells < MinCellsConst)
		{
			return null;
		}
		try
		{
			if (inv.IsInsertLocked())
			{
				return null;
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		return $"Storage ({cells})";
	}

	// 窗口 -> 可排序 inventory(null = 不是可排序容器)
	private static GameInventory SortableWindowInventory(PixelWindow win)
	{
		if (win == null)
		{
			return null;
		}
		GameInventory inv = ResolveInventory(win);
		if (inv == null || (SkipBarterConst && IsBarterOrChoose(inv)) || !IsSortableInventory(inv))
		{
			return null;
		}
		if (WindowLabel(win, inv) == null)
		{
			return null;
		}
		return inv;
	}

	// 快捷键动作: 排序最后打开的那个容器
	private static void SortLastOpenedContainer()
	{
		PixelWindow win = LastFocusedWindow();
		GameInventory inv = SortableWindowInventory(win);
		if (inv == null)
		{
			Toast("no container open to sort");
			return;
		}
		SortInventory(inv);
	}

	// 自动排序(开关默认关): 只在窗口「从无到有」出现时排一次, 且延后一个 tick(等窗口内容就绪)
	private static void TrackOpenedContainers()
	{
		List<PixelWindow> wins = new List<PixelWindow>();
		try
		{
			WindowsHandler wh = WindowsHandler.current;
			Il2CppSystem.Collections.Generic.List<PixelWindow> list = (((Object)(object)wh != (Object)null) ? wh.visibleWindows : null);
			int n = ((list != null) ? list.Count : 0);
			for (int i = 0; i < n; i++)
			{
				try
				{
					PixelWindow w = list[i];
					if (w != null)
					{
						wins.Add(w);
					}
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
			}
		}
		catch
		{
			// ponytail: IL2CPP native probe, silent fallback
		}
		if (_autoWarmup)
		{
			// 首轮: 开游戏时已有一堆常驻窗口, 只登记, 不能当成「刚打开」
			_autoWarmup = false;
			_seenWindows.Clear();
			foreach (PixelWindow w0 in wins)
			{
				_seenWindows.Add(WindowPtr(w0));
			}
			return;
		}
		// 1) 先执行上一 tick 记下的「刚打开的容器」
		PixelWindow pending = _pendingAuto;
		_pendingAuto = null;
		if (pending != null && AutoSortLastOpened != null && AutoSortLastOpened.Value)
		{
			GameInventory pinv = SortableWindowInventory(pending);
			if (pinv != null)
			{
				SortInventory(pinv);
			}
		}
		// 2) 再 diff 出本 tick 新出现的窗口 ⇒ 记为下一 tick 的自动排序目标(取焦点戳最大者 = 最后打开的那个)
		HashSet<long> now = new HashSet<long>();
		PixelWindow newest = null;
		long newestStamp = long.MinValue;
		foreach (PixelWindow w in wins)
		{
			long ptr = WindowPtr(w);
			now.Add(ptr);
			if (_seenWindows.Contains(ptr))
			{
				continue;
			}
			long s = 0L;
			try
			{
				s = w.focusStamp;
			}
			catch
			{
				// ponytail: IL2CPP native probe, silent fallback
			}
			if (newest == null || s >= newestStamp)
			{
				newestStamp = s;
				newest = w;
			}
		}
		_seenWindows.Clear();
		foreach (long v in now)
		{
			_seenWindows.Add(v);
		}
		_pendingAuto = newest;
	}

	// IL2CPP 对象身份(用于 diff 窗口集合)
	private static long WindowPtr(PixelWindow w)
	{
		if (w == null)
		{
			return 0L;
		}
		try
		{
			return ((Il2CppObjectBase)w).Pointer.ToInt64();
		}
		catch
		{
			return 0L;
		}
	}

	// 快捷键是否按下(配置字符串解析失败则回退 F7, 见 ResolveHotkey)
	private static bool SortHotkeyPressed()
	{
		ResolveHotkey();
		if (_hkKey < 0)
		{
			return false;
		}
		for (int i = 0; i < _hkMods.Length; i++)
		{
			if (!Input.GetKey((KeyCode)_hkMods[i]))
			{
				return false;
			}
		}
		return Input.GetKeyDown((KeyCode)_hkKey);
	}

	// 解析 SortHotkey 配置("LeftShift+LeftControl+G" 形式); 只在字符串变化时重解析, 失败回退 F7 并记日志
	private static void ResolveHotkey()
	{
		string sig = ((SortHotkey != null) ? SortHotkey.Value : null);
		if (sig == _hkSig)
		{
			return;
		}
		_hkSig = sig;
		_hkKey = -1;
		_hkMods = new int[0];
		if (string.IsNullOrWhiteSpace(sig))
		{
			MelonLogger.Warning("[InvSorter] SortHotkey 为空, 回退 F7");
			sig = "F7";
		}
		string[] parts = sig.Split('+');
		int key = ParseKeyName(parts[parts.Length - 1]);
		if (key < 0)
		{
			MelonLogger.Warning("[InvSorter] SortHotkey 无法识别: " + sig + " —— 回退 F7");
			key = 288;
			parts = new string[1] { "F7" };
		}
		List<int> mods = new List<int>();
		for (int i = 0; i < parts.Length - 1; i++)
		{
			int m = ParseKeyName(parts[i]);
			if (m < 0)
			{
				MelonLogger.Warning("[InvSorter] SortHotkey 修饰键无法识别: " + parts[i] + " (已忽略)");
				continue;
			}
			mods.Add(m);
		}
		_hkKey = key;
		_hkMods = mods.ToArray();
		MelonLogger.Msg("[InvSorter] 排序快捷键: " + sig + " -> KeyCode " + key + " (+ " + _hkMods.Length + " 个修饰键)");
	}

	// Unity KeyCode 名字表(不依赖 Enum.Parse, 避免 IL2CPP 枚举解析坑; 返回 -1 = 不认识)
	private static int ParseKeyName(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return -1;
		}
		string n = raw.Trim().ToUpperInvariant();
		if (n.Length == 1)
		{
			char c = n[0];
			if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
			{
				return c; // KeyCode.A=65, KeyCode.Alpha0=48
			}
		}
		if (n.Length >= 2 && n[0] == 'F' && int.TryParse(n.Substring(1), out var fn) && fn >= 1 && fn <= 15)
		{
			return 281 + fn; // KeyCode.F1=282 .. KeyCode.F15=296
		}
		switch (n)
		{
			case "SPACE": return 32;
			case "TAB": return 9;
			case "RETURN":
			case "ENTER": return 13;
			case "ESCAPE":
			case "ESC": return 27;
			case "BACKSPACE": return 8;
			case "DELETE": return 127;
			case "INSERT": return 277;
			case "HOME": return 278;
			case "END": return 279;
			case "PAGEUP": return 280;
			case "PAGEDOWN": return 281;
			case "UP":
			case "UPARROW": return 273;
			case "DOWN":
			case "DOWNARROW": return 274;
			case "RIGHT":
			case "RIGHTARROW": return 275;
			case "LEFT":
			case "LEFTARROW": return 276;
			case "LEFTSHIFT": return 304;
			case "RIGHTSHIFT": return 303;
			case "LEFTCONTROL":
			case "LEFTCTRL": return 306;
			case "RIGHTCONTROL":
			case "RIGHTCTRL": return 305;
			case "LEFTALT": return 308;
			case "RIGHTALT": return 307;
			case "MOUSE0": return 323;
			case "MOUSE1": return 324;
			default: return -1;
		}
	}
}
