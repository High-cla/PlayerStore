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

	internal static MelonPreferences_Entry<bool> Enabled;

	internal static MelonPreferences_Entry<bool> KeepContainers;

	internal static MelonPreferences_Entry<bool> SkipBarter;

	internal static MelonPreferences_Entry<int> MinCells;

	internal static MelonPreferences_Entry<bool> ShowBackground;

	internal static MelonPreferences_Entry<bool> OnlyNamedBackground;

	internal static MelonPreferences_Entry<string> DisplayCaseSizes;

	internal static MelonPreferences_Entry<string> MainStorageSizes;

	internal static MelonPreferences_Entry<string> IgnoreBackgroundSizes;

	internal static MelonPreferences_Entry<bool> GroupByTag;

	internal static MelonPreferences_Entry<bool> GroupByTagDefaulted;

	internal static MelonPreferences_Entry<bool> UseNativeUI;

	internal static MelonPreferences_Entry<float> NativePosX;

	internal static MelonPreferences_Entry<float> NativePosY;

	internal static MelonPreferences_Entry<int> MaxRows;

	internal static bool ButtonsVisible = true;

	// OnGUI 缓存: GUI 事件循环同帧多次调用 OnGUI, CollectSortables 结果同帧不变, 只算一次
	private static float _guiCacheTimer = -999f;
	private static readonly List<GameInventory> _guiInvs = new List<GameInventory>();
	private static readonly List<string> _guiLabels = new List<string>();
	private static int _guiCount = -1;


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

	private static float PanelX = 12f;

	private static float PanelY = 12f;

	private static bool Dragging = false;

	private static Vector2 DragOff;

	public override void OnInitializeMelon()
	{
		Cfg = MelonPreferences.CreateCategory("InventorySorter");
		Enabled = Cfg.CreateEntry<bool>("Enabled", true, (string)null, "Master on/off for the Sort buttons.", false, false, (ValueValidator)null, (string)null);
		KeepContainers = Cfg.CreateEntry<bool>("KeepContainersInPlace", false, (string)null, "Leave placed storage units (bays/cages) where they are; sort only loose items. Disabled: containers (incl. liquid bottles) sort too.", false, false, (ValueValidator)null, (string)null);
		SkipBarter = Cfg.CreateEntry<bool>("SkipBarterWindows", true, (string)null, "Do not add a Sort button to the barter / item-choose popups.", false, false, (ValueValidator)null, (string)null);
		MinCells = Cfg.CreateEntry<int>("MinCells", 10, (string)null, "Hide any grid smaller than this many cells (drops tiny slot/junk grids).", false, false, (ValueValidator)null, (string)null);
		ShowBackground = Cfg.CreateEntry<bool>("ShowBackground", true, (string)null, "Show the always-open inventories (display case / main storage). Titled containers always show.", false, false, (ValueValidator)null, (string)null);
		OnlyNamedBackground = Cfg.CreateEntry<bool>("OnlyNamedBackground", false, (string)null, "Strict mode: hide any background storage whose size isn't listed below. Off by default, unknown sizes still show as 'Storage (N)'.", false, false, (ValueValidator)null, (string)null);
		DisplayCaseSizes = Cfg.CreateEntry<string>("DisplayCaseSizes", "35,48", (string)null, "Comma-separated cell counts labelled 'Display Case' (e.g. 7x5=35, 8x6=48). Add more as you upgrade.", false, false, (ValueValidator)null, (string)null);
		MainStorageSizes = Cfg.CreateEntry<string>("MainStorageSizes", "240", (string)null, "Comma-separated cell counts labelled 'Main Storage'. Add more if it upgrades.", false, false, (ValueValidator)null, (string)null);
		IgnoreBackgroundSizes = Cfg.CreateEntry<string>("IgnoreBackgroundSizes", "72", (string)null, "Comma-separated cell counts of always-open junk grids to hide (e.g. the 72-cell system grid).", false, false, (ValueValidator)null, (string)null);
		GroupByTag = Cfg.CreateEntry<bool>("GroupByTag", true, (string)null, "Keep items sharing their first tag (e.g. FOOD, WEAPON) next to each other. On by default; the packer falls back to the tightest layout when grouping doesn't fit.", false, false, (ValueValidator)null, (string)null);
		UseNativeUI = Cfg.CreateEntry<bool>("UseNativeUI", true, (string)null, "Use the game's native window for the Sort buttons. Set false to use the classic IMGUI panel instead.", false, false, (ValueValidator)null, (string)null);
		NativePosX = Cfg.CreateEntry<float>("NativePosX", -100000f, (string)null, "Saved native-window position (X). Set automatically when you drag it.", false, false, (ValueValidator)null, (string)null);
		NativePosY = Cfg.CreateEntry<float>("NativePosY", -100000f, (string)null, "Saved native-window position (Y). Set automatically when you drag it.", false, false, (ValueValidator)null, (string)null);
		MaxRows = Cfg.CreateEntry<int>("MaxRows", 7, (string)null, "Fixed height of the window in rows. The button list scrolls if there are more; the window itself never changes size.", false, false, (ValueValidator)null, (string)null);
		GroupByTagDefaulted = Cfg.CreateEntry<bool>("GroupByTagDefaulted", false, (string)null, "Internal: set once after GroupByTag has been defaulted on. Do not edit.", false, false, (ValueValidator)null, (string)null);
		if (!GroupByTagDefaulted.Value)
		{
			GroupByTag.Value = true;
			GroupByTagDefaulted.Value = true;
			MelonPreferences.Save();
		}
		// 配置在游戏启动时即落盘生成 (不再等首次触发/退出), 玩家可提前看到并修改
		MelonPreferences.Save();
	}

	public override void OnApplicationQuit()
	{
		MelonPreferences.Save();
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
					if (UseNativeUI.Value)
					{
						_nativeDirty = true;
					}
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
		if (!UseNativeUI.Value || !Enabled.Value)
		{
			return;
		}
		_nativeTimer += Time.deltaTime;
		if (_nativeTimer >= 0.25f)
		{
			_nativeTimer = 0f;
			try
			{
				RefreshNativeUI();
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
		if (!ButtonsVisible || !Enabled.Value)
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
		float num2 = (float)Math.Max(3, MaxRows.Value) * 34f;
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

	public override void OnGUI()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Invalid comparison between Unknown and I4
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Invalid comparison between Unknown and I4
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		if (UseNativeUI.Value || !Enabled.Value || !ButtonsVisible)
		{
			return;
		}
		// 同帧缓存: GUI 事件循环(Repaint/Layout/...)同帧多次调用 OnGUI, 结果不变. 每 0.3s 重算一次.
		float nowGui = Time.realtimeSinceStartup;
		if (nowGui - _guiCacheTimer > 0.3f)
		{
			_guiCacheTimer = nowGui;
			_guiInvs.Clear();
			_guiLabels.Clear();
			_guiCount = CollectSortables(_guiInvs, _guiLabels);
		}
		List<GameInventory> list = _guiInvs;
		List<string> list2 = _guiLabels;
		int value = _guiCount;
		Event current = Event.current;
		bool flag = current != null && (int)current.type == 0 && current.button == 0;
		Vector2 val = (Vector2)((current != null) ? current.mousePosition : new Vector2(-1f, -1f));
		float num = 264f;
		float num2 = 26f;
		float num3 = 26f;
		float num4 = 10f;
		int num5 = Math.Max(list.Count, 1);
		float num6 = num2 + (float)num5 * num3 + num4 + 20f;
		Rect val2 = default(Rect);
		val2 = new Rect(PanelX, PanelY, num, num2);
		if (current != null)
		{
			if ((int)current.type == 0 && current.button == 0 && val2.Contains(val))
			{
				Dragging = true;
				DragOff = new Vector2(val.x - PanelX, val.y - PanelY);
				current.Use();
			}
			else if ((int)current.type == 1 && current.button == 0 && Dragging)
			{
				Dragging = false;
				current.Use();
			}
			else if ((int)current.type == 3 && Dragging)
			{
				PanelX = val.x - DragOff.x;
				PanelY = val.y - DragOff.y;
				current.Use();
			}
		}
		PanelX = Mathf.Clamp(PanelX, 0f, (float)Screen.width - num);
		PanelY = Mathf.Clamp(PanelY, 0f, (float)Screen.height - num6);
		Rect val3 = default(Rect);
		val3 = new Rect(PanelX, PanelY, num, num6);
		GUI.Box(val3, "Inventory Sorter   (drag | F6 hide)");
		float num7 = PanelY + num2;
		if (list.Count == 0)
		{
			GUI.Label(new Rect(PanelX + 10f, num7 + 2f, num - 20f, 20f), $"No open storage detected ({value} window(s)).");
			num7 += num3;
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				string label = "Sort: " + Trunc(list2[i], 28);
				if (FaceClicked(new Rect(PanelX + 8f, num7, num - 16f, 22f), label, flag, val, current))
				{
					SortInventory(list[i]);
				}
				num7 += num3;
			}
		}
		if (!string.IsNullOrEmpty(LastAction) && Time.realtimeSinceStartup - _lastActionAt < 3f)
		{
			GUI.Label(new Rect(PanelX + 10f, num7 + 2f, num - 20f, 20f), LastAction);
		}
		if (flag && val3.Contains(val) && current != null)
		{
			current.Use();
		}
	}

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

	private static bool SizeInList(int cells, string csv)
	{
		if (string.IsNullOrEmpty(csv))
		{
			return false;
		}
		string[] array = csv.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			if (int.TryParse(array[i].Trim(), out var result) && result == cells)
			{
				return true;
			}
		}
		return false;
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
				if (val3 == null || (SkipBarter.Value && IsBarterOrChoose(val3)))
				{
					continue;
				}
				bool flag = false;
				try
				{
					flag = ((Il2CppObjectBase)val3).TryCast<GameSlotInventory>() != null;
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
				if (flag)
				{
					continue;
				}
				string text = "";
				try
				{
					text = val2.titleString;
				}
				catch
				{
					// ponytail: IL2CPP native probe, silent fallback
				}
				InvInfo(val3, out var _, out var cells);
				string item;
				if (!string.IsNullOrEmpty(text))
				{
					item = text;
				}
				else
				{
					if (!ShowBackground.Value || cells < Math.Max(1, MinCells.Value) || SizeInList(cells, IgnoreBackgroundSizes.Value))
					{
						continue;
					}
					bool flag2 = false;
					try
					{
						flag2 = val3.IsInsertLocked();
					}
					catch
					{
						// ponytail: IL2CPP native probe, silent fallback
					}
					if (flag2)
					{
						continue;
					}
					if (SizeInList(cells, MainStorageSizes.Value))
					{
						item = "Main Storage";
					}
					else if (SizeInList(cells, DisplayCaseSizes.Value))
					{
						item = "Display Case";
					}
					else
					{
						if (OnlyNamedBackground.Value)
						{
							continue;
						}
						item = $"Storage ({cells})";
					}
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
			string tag = (GroupByTag.Value ? TagKey(item) : "");
			if (!groups.TryGetValue(tag, out var bucket))
			{
				bucket = (groups[tag] = new List<GameItem>());
				tagOrder.Add(tag);
			}
			bucket.Add(item);
		}
		if (GroupByTag.Value)
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
			if (KeepContainers.Value && HasContentWindow(it))
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
			string mode = null;
			if (GroupByTag.Value)
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
				layout = LayoutBanded(tagOrder, tagGroups, masks, w, h, keptContainers);
				if (layout != null)
				{
					mode = "grouped";
				}
			}
			if (layout == null)
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
					layout = LayoutDense(candidate, masks, w, h, keptContainers);
					if (layout != null)
					{
						mode = "packed";
						break;
					}
				}
			}
			if (layout == null)
			{
				RestoreOriginal(inv, original);
				Toast("not enough room to sort cleanly, left unchanged");
				return;
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
			Toast($"{mode} {layout.Count}/{sortPool.Count} item(s)" + ((num > 0) ? $", {num} rotated" : "") + ((keptContainers.Count > 0) ? $"  ({keptContainers.Count} kept)" : ""));
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
				if (!PlaceInto(occ, W, H, masks[item2], num, out var bx, out var by, out var bo, out var bottom, rects))
				{
					return null;
				}
				dictionary[item2] = new Placement(bx, by, bo);
				ItemMask mm = masks[item2];
				int pw = (bo == 1 || bo == 3) ? mm.Gh0 : mm.Gw0;
				int ph = (bo == 1 || bo == 3) ? mm.Gw0 : mm.Gh0;
				ShrinkRects(rects, bx, by, pw, ph);
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
		// 择优: 剩余最大连续空矩最大者
		Dictionary<GameItem, Placement> best = null;
		long bestArea = -1;
		foreach (Dictionary<GameItem, Placement> cand in candidates)
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
		return best;
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

	private static bool PlaceInto(bool[,] occ, int W, int H, ItemMask m, int minY, out int bx, out int by, out int bo, out int bottom, List<(int x, int y, int w, int h)> cachedRects = null)
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
					if (FindFreeSpotCells(rects, occ, W, H, list, gw, gh, minY2, out var ox, out var oy, out long waste) && (bx < 0 || oy < by || (oy == by && ox < bx)))
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
	private static bool FindFreeSpotCells(List<(int x, int y, int w, int h)> rects, bool[,] occ, int W, int H, List<(int dx, int dy)> cells, int gw, int gh, int minY, out int ox, out int oy, out long waste)
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
			if (!HasSupport(occ, px, py, cells))
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

	private static bool FaceClicked(Rect r, string label, bool mdown, Vector2 mp, Event e)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		GUI.Box(r, label, GUI.skin.button);
		int num;
		if (mdown)
		{
			num = (r.Contains(mp) ? 1 : 0);
			if (num != 0 && e != null)
			{
				e.Use();
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}
}
