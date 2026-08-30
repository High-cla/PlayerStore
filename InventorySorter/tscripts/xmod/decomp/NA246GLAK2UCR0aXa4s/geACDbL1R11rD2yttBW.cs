using System;
using System.Collections;
using System.Reflection;
using E1edgYxSHVYFaeHy7BP;
using ModFramework.GUI;
using ModFramework.Utilities;
using UnityEngine;

namespace NA246GLAK2UCR0aXa4s;

internal static class geACDbL1R11rD2yttBW
{
	private static geACDbL1R11rD2yttBW nsmfCv7xAhelrtyfekG;

	private static bool Prefix(WindowPanelManager __instance)
	{
		if (!sfumh2xPLltR4pL0i9k.JYyLLNZjSx())
		{
			return true;
		}
		Transform val = ((Component)__instance).transform.Find("Container/Content/Main Content") ?? throw new InvalidOperationException("WindowPanelManager main content was not found.");
		Transform obj = ((Component)__instance).transform.Find("Container/Content/Navigation Panel") ?? throw new InvalidOperationException("WindowPanelManager navigation was not found.");
		Transform obj2 = ((Component)__instance).transform.Find("Indicator");
		RectTransform value = ((obj2 != null) ? ((Component)obj2).GetComponent<RectTransform>() : null) ?? throw new InvalidOperationException("WindowPanelManager indicator was not found.");
		sfumh2xPLltR4pL0i9k.ELTxbXaVOK(__instance, "indicator", value);
		IList list = sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<IList>(__instance, "panels");
		Type type = typeof(WindowPanelManager).GetNestedType("PanelItem", BindingFlags.Public | BindingFlags.NonPublic) ?? throw new InvalidOperationException("WindowPanelManager.PanelItem type was not found.");
		foreach (PanelButton componentsInChild in ((Component)obj).GetComponentsInChildren<PanelButton>(true))
		{
			string name = ((Object)((Component)componentsInChild).gameObject).name;
			Transform val2 = val.Find(name);
			if (!((Object)(object)val2 == (Object)null))
			{
				object obj3 = Activator.CreateInstance(type) ?? throw new InvalidOperationException("WindowPanelManager.PanelItem could not be created.");
				sfumh2xPLltR4pL0i9k.ELTxbXaVOK(obj3, "name", name);
				sfumh2xPLltR4pL0i9k.ELTxbXaVOK(obj3, "animator", ((Component)val2).GetComponent<Animator>());
				sfumh2xPLltR4pL0i9k.ELTxbXaVOK(obj3, "button", componentsInChild);
				list.Add(obj3);
			}
		}
		if (list.Count == 0)
		{
			ModLogger.Warning("[ProbablyStolenPlaytest] No framework panels were found.");
			return false;
		}
		int num = Math.Clamp(sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<int>(__instance, "currentPanelIndex"), 0, list.Count - 1);
		sfumh2xPLltR4pL0i9k.ELTxbXaVOK(__instance, "currentPanelIndex", num);
		sfumh2xPLltR4pL0i9k.ELTxbXaVOK(__instance, "currentButtonIndex", num);
		try
		{
			float animatorClipLength = UnityHelpers.GetAnimatorClipLength(sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<Animator>(list[num], "animator"), "WindowPanel_In");
			if (animatorClipLength > 0f)
			{
				sfumh2xPLltR4pL0i9k.ELTxbXaVOK(__instance, "baseAnimLength", animatorClipLength + 0.2f);
			}
		}
		catch
		{
		}
		__instance.InitializePanels();
		__instance.propertyPanel = RinL7Dwh99<PropertyPanel>((object)list, (object)"Property Editing");
		__instance.itemPanel = RinL7Dwh99<GenericItemPanel>((object)list, (object)"Item Editting");
		__instance.settingsPanel = RinL7Dwh99<SettingsPanel>((object)list, (object)"Settings");
		return false;
	}

	private static cJQIw2LsHLE0r5eW8gE RinL7Dwh99<cJQIw2LsHLE0r5eW8gE>(object P_0, object P_1) where cJQIw2LsHLE0r5eW8gE : Component
	{
		foreach (object item in (IEnumerable)P_0)
		{
			if (string.Equals(sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<string>(item, "name"), (string?)P_1, StringComparison.Ordinal))
			{
				return ((Component)sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<Animator>(item, "animator")).gameObject.AddComponent<cJQIw2LsHLE0r5eW8gE>();
			}
		}
		throw new InvalidOperationException("Framework panel '" + (string?)P_1 + "' was not found.");
	}

	internal static bool e8JhpS7LXEJiMJdJjV1()
	{
		return nsmfCv7xAhelrtyfekG == null;
	}

	internal static geACDbL1R11rD2yttBW YURL9078kkN8TcR8FRb()
	{
		return nsmfCv7xAhelrtyfekG;
	}
}
