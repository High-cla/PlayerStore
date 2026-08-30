using System;
using E1edgYxSHVYFaeHy7BP;
using ModFramework.GUI;
using ModFramework.Utilities;
using UnityEngine;

namespace fDZd64Ld24vvtImYqcf;

internal static class wxrTCdLwgpiRemmojkM
{
	internal static wxrTCdLwgpiRemmojkM M4apF4AtGUaNuoGnD33;

	private static bool Prefix(WindowManager __instance)
	{
		if (!sfumh2xPLltR4pL0i9k.JYyLLNZjSx())
		{
			return true;
		}
		sfumh2xPLltR4pL0i9k.ELTxbXaVOK(__instance, "navbarCurve", AnimationCurveHelper.EaseInOutReflective(0f, 0f, 1f, 1f));
		__instance.windowAnimator = ((Component)__instance).GetComponent<Animator>();
		Transform obj = ((Component)__instance).transform.Find("Container");
		__instance.windowContainer = ((obj != null) ? ((Component)obj).GetComponent<RectTransform>() : null) ?? throw new InvalidOperationException("WindowManager Container was not found.");
		Transform obj2 = ((Transform)__instance.windowContainer).Find("Content/Main Content");
		__instance.windowContent = ((obj2 != null) ? ((Component)obj2).GetComponent<RectTransform>() : null) ?? throw new InvalidOperationException("WindowManager main content was not found.");
		Transform obj3 = ((Transform)__instance.windowContainer).Find("Content/Navigation Panel");
		__instance.navbarRect = ((obj3 != null) ? ((Component)obj3).GetComponent<RectTransform>() : null) ?? throw new InvalidOperationException("WindowManager navigation panel was not found.");
		Transform obj4 = ((Transform)__instance.windowContainer).Find("Dragger");
		GameObject val = ((obj4 != null) ? ((Component)obj4).gameObject : null) ?? throw new InvalidOperationException("WindowManager dragger was not found.");
		__instance.windowDragger = val.AddComponent<WindowDragger>();
		__instance.windowDragger.DragObject = __instance.windowContainer;
		__instance.windowDragger.WinManager = __instance;
		try
		{
			float animatorClipLength = UnityHelpers.GetAnimatorClipLength(__instance.windowAnimator, "Window_In");
			if (animatorClipLength > 0f)
			{
				sfumh2xPLltR4pL0i9k.ELTxbXaVOK(__instance, "cachedStateLength", animatorClipLength + 0.1f);
			}
		}
		catch
		{
		}
		__instance.InitializeResizePreset();
		__instance.InitializeNavbar();
		__instance.windowPanelManager = ((Component)__instance).gameObject.AddComponent<WindowPanelManager>();
		return false;
	}

	internal static bool qiUN9qAvNKt7gcU5NVd()
	{
		return M4apF4AtGUaNuoGnD33 == null;
	}

	internal static wxrTCdLwgpiRemmojkM gJS02kAc1Ru00gxoJvu()
	{
		return M4apF4AtGUaNuoGnD33;
	}
}
