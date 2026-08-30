using System.Collections.Generic;
using E1edgYxSHVYFaeHy7BP;
using HarmonyLib;
using ModFramework.GUI;
using UnityEngine;

namespace knGAxpLjMiItmnRFERW;

[HarmonyPatch(typeof(InfinityScrollModel), "UpdateItems")]
internal static class c6JgYfL5GqltuUYU7dJ
{
	private static c6JgYfL5GqltuUYU7dJ XrHrex7fcJyC2L32rCy;

	private static bool Prefix(InfinityScrollModel __instance, int startIndex)
	{
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		int num = 5;
		GameObject val = default(GameObject);
		int columns = default(int);
		List<RectTransform> list3 = default(List<RectTransform>);
		int num3 = default(int);
		int num8 = default(int);
		float num4 = default(float);
		int num9 = default(int);
		int num5 = default(int);
		float num7 = default(float);
		int num6 = default(int);
		List<GameObject> list2 = default(List<GameObject>);
		List<GenericItemEntry> list = default(List<GenericItemEntry>);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 18:
					val.SetActive(false);
					num2 = 21;
					continue;
				case 5:
					if (!sfumh2xPLltR4pL0i9k.QcML6i0UQy())
					{
						num2 = 4;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_988f5d1238d043129fbd30a9c933ca73 == 0)
						{
							num2 = 4;
						}
					}
					else
					{
						columns = __instance.Columns;
						num2 = 17;
					}
					continue;
				case 1:
					list3[num3].anchoredPosition = new Vector2((float)num8 * num4, (float)(-(num9 + num5)) * num7);
					num2 = 11;
					continue;
				case 6:
					if (num6 >= __instance.TotalItems)
					{
						num2 = 18;
						continue;
					}
					goto case 9;
				case 4:
					return true;
				case 14:
					num8 = num3 % columns;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_64dea62aaa174adaacac66b92a1d143b != 0)
					{
						num2 = 1;
					}
					continue;
				case 17:
					num9 = startIndex / columns;
					num2 = 23;
					continue;
				case 15:
					num3 = 0;
					num2 = 8;
					continue;
				case 2:
					num6 = startIndex + num3;
					num2 = 13;
					continue;
				case 23:
					list2 = sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<List<GameObject>>(__instance, "items");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a6984c925458472aada338b15f8b243d != 0)
					{
						num2 = 0;
					}
					continue;
				case 12:
					if (num6 >= 0)
					{
						num2 = 6;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d51c61ea1cd54949ab81b08153329f54 != 0)
						{
							num2 = 5;
						}
						continue;
					}
					goto case 18;
				case 11:
					sfumh2xPLltR4pL0i9k.Lc4xaxVZJ4(list[num3], __instance.Models[num6], __instance.QuantityProvider);
					num2 = 20;
					continue;
				case 19:
					num7 = sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<float>(__instance, "itemHeight");
					num2 = 16;
					continue;
				case 8:
				case 22:
					if (num3 >= list2.Count)
					{
						num2 = 3;
						continue;
					}
					goto case 2;
				case 7:
					num5 = num3 / columns;
					num2 = 3;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4068ee93bb84461dbacde7f72600241d == 0)
					{
						num2 = 14;
					}
					continue;
				case 13:
					val = list2[num3];
					num2 = 12;
					continue;
				case 16:
					num4 = sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<float>(__instance, "itemWidth");
					num2 = 15;
					continue;
				case 10:
					list = sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<List<GenericItemEntry>>(__instance, "entries");
					num2 = 19;
					continue;
				case 9:
					val.SetActive(true);
					num2 = 7;
					continue;
				case 20:
				case 21:
					num3++;
					num2 = 22;
					continue;
				case 3:
					return false;
				}
				break;
			}
			list3 = sfumh2xPLltR4pL0i9k.zWQx4nT6Yw<List<RectTransform>>(__instance, "itemRTs");
			num = 10;
		}
	}

	internal static bool gLOKVV7FZeMWoSAoBuO()
	{
		return XrHrex7fcJyC2L32rCy == null;
	}

	internal static c6JgYfL5GqltuUYU7dJ fcpuNc7hxPKsYvy69UL()
	{
		return XrHrex7fcJyC2L32rCy;
	}
}
