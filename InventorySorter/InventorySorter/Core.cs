using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
		KeepContainers = Cfg.CreateEntry<bool>("KeepContainersInPlace", true, (string)null, "Leave placed storage units (bays/cages) where they are; sort only loose items.", false, false, (ValueValidator)null, (string)null);
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
		((MelonBase)this).LoggerInstance.Msg("Inventory Sorter v1.0.1 loaded. A Sort window appears while a storage is open. F6 hides/shows it.");
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
		List<GameInventory> list = new List<GameInventory>();
		List<string> list2 = new List<string>();
		int value = CollectSortables(list, list2);
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
			}
		}
		catch
		{
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
		}
		return false;
	}

	internal static void SortInventory(GameInventory inv)
	{
		List<GameItem> list = new List<GameItem>();
		try
		{
			Il2CppSystem.Collections.Generic.List<GameItem> childItems = inv.childItems;
			if (childItems == null)
			{
				return;
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
			Toast("read failed: " + ex.Message);
			return;
		}
		if (list.Count <= 1)
		{
			Toast("nothing to sort");
			return;
		}
		List<GameItem> list2 = new List<GameItem>();
		List<GameItem> list3 = new List<GameItem>();
		foreach (GameItem item5 in list)
		{
			if (KeepContainers.Value && HasContentWindow(item5))
			{
				list3.Add(item5);
			}
			else
			{
				list2.Add(item5);
			}
		}
		if (list2.Count <= 1)
		{
			Toast("only containers here, left in place");
			return;
		}
		List<(GameItem, int, int, int, bool)> list4 = new List<(GameItem, int, int, int, bool)>();
		foreach (GameItem item6 in list2)
		{
			GridShape val2 = ShapeOf(item6);
			int item = 0;
			int item2 = 0;
			int item3 = 0;
			bool item4 = false;
			if (val2 != null)
			{
				try
				{
					item = val2.minX;
					item2 = val2.minY;
					item3 = val2.orientation;
				}
				catch
				{
				}
				try
				{
					GridShapeBuilder val3 = ((Il2CppObjectBase)val2).TryCast<GridShapeBuilder>();
					if (val3 != null)
					{
						item4 = val3.flipped;
					}
				}
				catch
				{
				}
			}
			list4.Add((item6, item, item2, item3, item4));
		}
		List<string> list5 = new List<string>();
		Dictionary<string, List<GameItem>> dictionary = new Dictionary<string, List<GameItem>>();
		foreach (GameItem item7 in list2)
		{
			string text = (GroupByTag.Value ? TagKey(item7) : "");
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = (dictionary[text] = new List<GameItem>());
				list5.Add(text);
			}
			value.Add(item7);
		}
		if (GroupByTag.Value)
		{
			list5.Sort((string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
		}
		foreach (string item8 in list5)
		{
			dictionary[item8].Sort(SizeCompare);
		}
		try
		{
			if (!GetGridDims(inv, out var w, out var h) || w <= 0 || h <= 0)
			{
				Toast("couldn't read grid size, not sorted");
				return;
			}
			w = Math.Min(w, 128);
			h = Math.Min(h, 8192);
			Dictionary<GameItem, ItemMask> dictionary2 = new Dictionary<GameItem, ItemMask>();
			foreach (GameItem item9 in list2)
			{
				dictionary2[item9] = BuildMask(item9);
			}
			// 同类合并视图: 相同 ident + 相同形状(Gw0xGh0 + C0) 的多件只占一份布局(代表件),
			// 其余在应用阶段调 StackItemUnchecked 并入代表件. 大幅减少地面占用, 释放连续空域(实测 BEST 从不劣化).
			Dictionary<string, int> mergeRepIdx = new Dictionary<string, int>();
			for (int mi = 0; mi < list2.Count; mi++)
			{
				ItemMask mm2 = dictionary2[list2[mi]];
				StringBuilder msb = new StringBuilder();
				foreach ((int mdx, int mdy) in mm2.C0)
				{
					msb.Append(mdx).Append(':').Append(mdy).Append(',');
				}
				string mkey = list2[mi].identifier + "|" + mm2.Gw0 + "x" + mm2.Gh0 + "|" + msb.ToString();
				if (!mergeRepIdx.ContainsKey(mkey))
				{
					mergeRepIdx[mkey] = mi;
				}
			}
			// 被合并件清单: 非代表件的下标
			List<int> mergeAbsorb = new List<int>();
			HashSet<int> mergeRepSet = new HashSet<int>(mergeRepIdx.Values);
			for (int mi = 0; mi < list2.Count; mi++)
			{
				if (!mergeRepSet.Contains(mi))
				{
					mergeAbsorb.Add(mi);
				}
			}
			// 诊断插桩: 每次排序无条件 dump 新形状(缓存去重)+当前空间统计 到 Mods/inv_shape_dump.txt
			DumpShapes(w, h, dictionary2, list4);			Dictionary<GameItem, Placement> dictionary3 = null;
			string value2 = null;
			if (GroupByTag.Value)
			{
				// 同类合并: 从 tag 分组中剔除被合并件(代表件保留), 布局后重叠致放自动合并
				if (mergeAbsorb.Count > 0)
				{
					HashSet<GameItem> absorbSet2 = new HashSet<GameItem>();
					foreach (int ai3 in mergeAbsorb)
					{
						absorbSet2.Add(list2[ai3]);
					}
					foreach (List<GameItem> list8 in dictionary.Values)
					{
						list8.RemoveAll(g => absorbSet2.Contains(g));
					}
				}
				dictionary3 = LayoutBanded(list5, dictionary, dictionary2, w, h, list3);
				if (dictionary3 != null)
				{
					value2 = "grouped";
				}
			}
			if (dictionary3 == null)
			{
				foreach (Comparison<GameItem> item10 in new List<Comparison<GameItem>>
				{
					SizeCompare,
					(GameItem a, GameItem b) => Math.Max(BaseW(b), BaseH(b)).CompareTo(Math.Max(BaseW(a), BaseH(a))),
					(GameItem a, GameItem b) => BaseH(b).CompareTo(BaseH(a)),
					(GameItem a, GameItem b) => BaseW(b).CompareTo(BaseW(a))
				})
				{
					List<GameItem> list7 = new List<GameItem>(list2);
					if (mergeAbsorb.Count > 0)
					{
						// 同类合并: 被合并件不参与布局(代表件排一次即可), 应用阶段重叠致放自动合并
						HashSet<GameItem> absorbSet = new HashSet<GameItem>();
						foreach (int ai2 in mergeAbsorb)
						{
							absorbSet.Add(list2[ai2]);
						}
						list7.RemoveAll(g => absorbSet.Contains(g));
					}
					list7.Sort(item10);
					dictionary3 = LayoutDense(list7, dictionary2, w, h, list3);
					if (dictionary3 != null)
					{
						value2 = "packed";
						break;
					}
				}
			}
			if (dictionary3 == null)
			{
				RestoreOriginal(inv, list4);
				Toast("not enough room to sort cleanly, left unchanged");
				return;
			}
			int num = 0;
			// 同类合并应用: 布局成功后, 被合并件致放到代表件同一位置(重叠) — 游戏堆叠机制自动合并为一格.
			// 代表件位置 = dictionary3[rep]; 每个被合并件找同 ident 代表, PlaceItem 到代表件的 X/Y/O.
			if (mergeAbsorb.Count > 0)
			{
				foreach (int ai in mergeAbsorb)
				{
					GameItem absorbed = list2[ai];
					GameItem rep = null;
					foreach (KeyValuePair<string, int> mp in mergeRepIdx)
					{
						GameItem r = list2[mp.Value];
						if (r.identifier == absorbed.identifier)
						{
							rep = r;
							break;
						}
					}
					if (rep != null && dictionary3.TryGetValue(rep, out Placement rp))
					{
						PlaceItem(absorbed, rp.X, rp.Y, rp.O);
					}
				}
			}
			// 堆叠物品(unitCount>1)最后放置: 游戏按放置顺序渲染, 后放的贴图在上层, 保证堆叠物至少一格视觉可见(否则被盖住看着取不出)
			List<KeyValuePair<GameItem, Placement>> order11 = new List<KeyValuePair<GameItem, Placement>>(dictionary3);
			order11.Sort((a, b) => (Stacked(a.Key) ? 1 : 0).CompareTo(Stacked(b.Key) ? 1 : 0));
			foreach (KeyValuePair<GameItem, Placement> item11 in order11)
			{
				Placement value3 = item11.Value;
				PlaceItem(item11.Key, value3.X, value3.Y, value3.O);
				if (value3.O == 1)
				{
					num++;
				}
			}
			try
			{
				inv.Validate();
			}
			catch
			{
			}
			Toast($"{value2} {dictionary3.Count}/{list2.Count} item(s)" + ((num > 0) ? $", {num} rotated" : "") + ((list3.Count > 0) ? $"  ({list3.Count} kept)" : ""));
		}
		catch (System.Exception ex2)
		{
			try
			{
				RestoreOriginal(inv, list4);
			}
			catch
			{
			}
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
			catch
			{
			}
		}
		try
		{
			inv.Validate();
		}
		catch
		{
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
		List<Dictionary<GameItem, Placement>> candidates = new List<Dictionary<GameItem, Placement>>();
		// 1) 落地堆积(大仓配对 → 拆死锁 → 无配对), 实测大仓紧凑, 小仓无配对较好
		List<object> paired = (W * H >= 100) ? BuildUnits(flat, masks) : null;
		if (paired != null)
		{
			paired.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
			if (TryPlaceUnits(fixedItems, paired, masks, W, H, out Dictionary<GameItem, Placement> dict))
			{
				candidates.Add(dict);
			}
			List<object> split = SplitFailedUnit(fixedItems, paired, masks, W, H);
			if (split != null && TryPlaceUnits(fixedItems, split, masks, W, H, out Dictionary<GameItem, Placement> dictS))
			{
				candidates.Add(dictS);
			}
		}
		List<object> singles = new List<object>(flat);
		singles.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		if (TryPlaceUnits(fixedItems, singles, masks, W, H, out Dictionary<GameItem, Placement> dict2))
		{
			candidates.Add(dict2);
		}
		// 2) MinHole: 实测大仓最优(挤出最大整块连续区), 小仓亦常最优. 两个变体: 裸单件 + 级联(配对单元进MinHole)
		if (TryMinHole(fixedItems, singles, masks, W, H, out Dictionary<GameItem, Placement> dictM))
		{
			candidates.Add(dictM);
		}
		if (paired != null && TryMinHole(fixedItems, paired, masks, W, H, out Dictionary<GameItem, Placement> dictMC))
		{
			candidates.Add(dictMC);
		}
		// 3) 堆叠叠放: 堆叠物品允许压已占格(>=1 新格可见), 少占地面, 释放空间
		if (TryMinHoleStack(fixedItems, singles, masks, W, H, out Dictionary<GameItem, Placement> dictS2))
		{
			candidates.Add(dictS2);
		}
		if (paired != null && TryMinHoleStack(fixedItems, paired, masks, W, H, out Dictionary<GameItem, Placement> dictPS2))
		{
			candidates.Add(dictPS2);
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

	// MinHole(级联版): 输入可以是已配对的单元(接力: 先用 BuildUnits 配对, 再对单元跑 MinHole)
	private static bool TryMinHole(List<GameItem> fixedItems, List<object> units, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		List<object> order = new List<object>(units);
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		foreach (object unit in order)
		{
			ItemMask m = (unit is PairUnit pu) ? pu.M : masks[(GameItem)unit];
			long bestScore = long.MaxValue;
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
						// 剪枝: 至少一格贴边或贴已占块(候选 O(W+H)), 已验证几乎不损质量
						if (!Touches(occ, W, H, px, py, cells))
						{
							continue;
						}
						// 放置后最大空矩(直接计算, 不打临时数组)
						bool[,] next = (bool[,])occ.Clone();
						foreach ((int dx, int dy) in cells)
						{
							next[px + dx, py + dy] = true;
						}
						long area = LargestEmptyArea(next, W, H);
						if (area < bestScore || (area == bestScore && (py < bestY || (py == bestY && px < bestX))))
						{
							bestScore = area;
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
			if (unit is PairUnit pf)
			{
				dictionary[pf.A] = new Placement(bestX + pf.Ax, bestY + pf.Ay, pf.OA);
				dictionary[pf.B] = new Placement(bestX + pf.Bx, bestY + pf.By, pf.OB);
				foreach ((int dx, int dy) in pf.M.C0)
				{
					occ[bestX + dx, bestY + dy] = true;
				}
			}
			else
			{
				GameItem item = (GameItem)unit;
				dictionary[item] = new Placement(bestX, bestY, bestO);
				foreach ((int dx, int dy) in CellsOf(m, bestO))
				{
					occ[bestX + dx, bestY + dy] = true;
				}
			}
		}
		return true;
	}

	// MinHoleStack: 堆叠叠放变体. 堆叠物品(unitCount>1)允许压已占格(叠上去), 但至少 1 格新空格(可见约束);
	// 评分 = 新格数最小(少占地面) -> 再最小化放置后最大空矩(挤出整块). 非堆叠/配对单元照旧 MinHole.
	private static bool TryMinHoleStack(List<GameItem> fixedItems, List<object> units, Dictionary<GameItem, ItemMask> masks, int W, int H, out Dictionary<GameItem, Placement> dictionary)
	{
		bool[,] occ = new bool[W, H];
		foreach (GameItem fixedItem in fixedItems)
		{
			MarkCurrentCells(occ, W, H, fixedItem);
		}
		dictionary = new Dictionary<GameItem, Placement>();
		List<object> order = new List<object>(units);
		order.Sort((a, b) => CellCount(a, masks).CompareTo(CellCount(b, masks)) * -1);
		foreach (object unit in order)
		{
			bool isPair = unit is PairUnit;
			bool stackable = !isPair && Stacked((GameItem)unit);
			ItemMask m = isPair ? ((PairUnit)unit).M : masks[(GameItem)unit];
			long bestScore = long.MaxValue;
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
						// 合法性: 堆叠物允许格重叠(反向下), 但须 >=1 格新空格; 非堆叠全空格
						int fresh = 0;
						bool ok = true;
						foreach ((int dx, int dy) in cells)
						{
							int cx = px + dx;
							int cy = py + dy;
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
								continue;
							}
							fresh++;
						}
						if (!ok || (stackable && fresh == 0))
						{
							continue;
						}
						// 评分: 1) 新格数小 2) 放置后最大空矩小(取候选) 3) tie 左上
						bool[,] next = (bool[,])occ.Clone();
						foreach ((int dx, int dy) in cells)
						{
							next[px + dx, py + dy] = true;
						}
						long la = LargestEmptyArea(next, W, H);
						long score = stackable ? ((long)fresh << 32) | la : la;
						if (score < bestScore)
						{
							bestScore = score;
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
			if (isPair)
			{
				PairUnit pf = (PairUnit)unit;
				dictionary[pf.A] = new Placement(bestX + pf.Ax, bestY + pf.Ay, pf.OA);
				dictionary[pf.B] = new Placement(bestX + pf.Bx, bestY + pf.By, pf.OB);
				foreach ((int dx, int dy) in pf.M.C0)
				{
					occ[bestX + dx, bestY + dy] = true;
				}
			}
			else
			{
				GameItem item = (GameItem)unit;
				dictionary[item] = new Placement(bestX, bestY, bestO);
				foreach ((int dx, int dy) in CellsOf(m, bestO))
				{
					// 堆叠物只标新空格(叠层不重复占用)
					if (!occ[bestX + dx, bestY + dy])
					{
						occ[bestX + dx, bestY + dy] = true;
					}
				}
			}
		}
		return true;
	}

	// 至少一格贴边或贴已占块(MinHole 候选剪枝)
	private static bool Touches(bool[,] occ, int W, int H, int x, int y, List<(int, int)> cells)
	{
		foreach ((int dx, int dy) in cells)
		{
			int cx = x + dx;
			int cy = y + dy;
			if (cx == 0 || cx == W - 1 || cy == 0 || cy == H - 1)
			{
				return true;
			}
			if (occ[cx - 1, cy] || occ[cx + 1, cy] || occ[cx, cy - 1] || occ[cx, cy + 1])
			{
				return true;
			}
		}
		return false;
	}

	// 直方图+单调栈: 最大全空连续矩形面积
	private static long LargestEmptyArea(bool[,] occ, int W, int H)
	{
		long best = 0;
		int[] heights = new int[W];
		int[] stack = new int[W + 1];
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
			}
			if (val == null)
			{
				try
				{
					val = it.shape;
				}
				catch
				{
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
		}
		if (val == null)
		{
			try
			{
				val = it.shape;
			}
			catch
			{
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

	// 诊断: dump 背包尺寸 + 每件散件形状(缓存: 每唯一形状仅首见 dump)到 Mods/inv_shape_dump.txt. 编译后自动调用, 无需开关.
	private static readonly HashSet<string> _dumpedShapes = new HashSet<string>();

	private static void DumpShapes(int w, int h, Dictionary<GameItem, ItemMask> masks, List<(GameItem, int, int, int, bool)> placed)
	{
		try
		{
			// per-session: 每次排序会话内去重(相同形状只打印一次), 跨会话不缓存 — 保证每会话完整物品集可重建
			_dumpedShapes.Clear();
			// 当前占用统计: 用每件物品当前位置(minX/minY/orientation) + mask cells 重建占用网格
			bool[,] occ = new bool[w, h];
			foreach ((GameItem it, int px, int py, int po, bool flip) in placed)
			{
				if (!masks.TryGetValue(it, out ItemMask m))
				{
					continue;
				}
				List<(int, int)> cells = CellsOf(m, po);
				if (cells == null || cells.Count == 0)
				{
					cells = m.C0;
				}
				foreach ((int dx, int dy) in cells)
				{
					int cx = px + dx;
					int cy = py + dy;
					if (cx >= 0 && cx < w && cy >= 0 && cy < h)
					{
						occ[cx, cy] = true;
					}
				}
			}
			int occupied = 0;
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					if (occ[x, y])
					{
						occupied++;
					}
				}
			}
			int free = w * h - occupied;
			// 最大连续空矩形: 逐行直方图 + 单调栈 O(W*H)
			int maxA = 0;
			int mx = 0;
			int my = 0;
			int mw = 0;
			int mh = 0;
			int[] hist = new int[w];
			int[] stack = new int[w + 1];
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					hist[x] = occ[x, y] ? 0 : hist[x] + 1;
				}
				int top = -1;
				for (int x = 0; x <= w; x++)
				{
					int ch = (x < w) ? hist[x] : 0;
					while (top >= 0 && hist[stack[top]] > ch)
					{
						int idx = stack[top--];
						int leftBound = (top >= 0) ? stack[top] + 1 : 0;
						int width = x - leftBound;
						int area = hist[idx] * width;
						if (area > maxA)
						{
							maxA = area;
							mw = width;
							mh = hist[idx];
							mx = leftBound;
							my = y - hist[idx] + 1;
						}
					}
					if (x < w)
					{
						stack[++top] = x;
					}
				}
			}
			List<string> lines = new List<string>();
			foreach (KeyValuePair<GameItem, ItemMask> pair in masks)
			{
				GameItem it = pair.Key;
				string ident = "";
				string name = "";
				string tag = "";
				try
				{
					ident = it.identifier;
				}
				catch { }
				try
				{
					name = it.name;
				}
				catch { }
				try
				{
					tag = TagKey(it);
				}
				catch { }
			ItemMask m = pair.Value;
				// 形状签名: ident + 尺寸 + C0 格子序列; 本会话内已 dump 过则跳过(per-session 缓存)
				StringBuilder sb = new StringBuilder();
				foreach ((int dx, int dy) in m.C0)
				{
					sb.Append(dx).Append(':').Append(dy).Append(',');
				}
				string sig = ident + "|" + m.Gw0 + "x" + m.Gh0 + "|" + sb.ToString();
				if (!_dumpedShapes.Add(sig))
				{
					continue;
				}
				lines.Add("=== " + ident + " | name=" + name + " | tag=" + tag + " ===(" + m.Gw0 + "x" + m.Gh0 + ")");
				for (int y = 0; y < m.Gh0; y++)
				{
					char[] row = new char[m.Gw0];
					for (int x = 0; x < m.Gw0; x++)
					{
						row[x] = '.';
					}
					foreach ((int dx, int dy) in m.C0)
					{
						if (dy == y && dx >= 0 && dx < m.Gw0)
						{
							row[dx] = '#';
						}
					}
					lines.Add(new string(row));
				}
			}
			// 头部: 背包尺寸 + 当前占用/剩余/最大连续空矩形
			string header = "== inv " + w + "x" + h + " == " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
				+ " occ=" + occupied + " free=" + free + " maxempty=" + maxA + " @" + mx + "," + my + " " + mw + "x" + mh;
			string file = Path.Combine(Path.GetDirectoryName(typeof(Core).Assembly.Location), "inv_shape_dump.txt");
			string text = header + "\n" + string.Join("\n", lines) + "\n";
			File.AppendAllText(file, text, Encoding.UTF8);
			MelonLogger.Msg($"[InvSorter] shape dump: {lines.Count / 2} new shape(s) -> {file}");
		}
		catch (System.Exception ex)
		{
			MelonLogger.Error("[InvSorter] dump failed: " + ex.Message);
		}
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
