using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Dc80t9LqgVxgkUAmr3Z;
using Il2CppSystem.Collections.Generic;
using ModFramework.GUI;
using ModFramework.Utilities;
using T0r3LbyoAoBrPidtAH;
using TyOQ7hhkasLPlhFR3an;
using UnityEngine;
using b7tdMvJlthvEYSFpZX;
using byB3SM1jfs9KMIIOGh;
using tG8poqLV5yVFBUEB8NP;

namespace X1QgmHLbWEJbyK5BRGn;

internal sealed class TeJZ2ELr25KIGhra8Tx : IItemModel
{
	private readonly lxaYMLBUJSOI9qKHmn MexLkeSdtr;

	private Sprite? Nk7LYBLuYX;

	internal static TeJZ2ELr25KIGhra8Tx w8kJnQ7MPXf7eJpW2I5;

	public override string DisplayName
	{
		get
		{
			int num = 1;
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				default:
					return MexLkeSdtr.WDrgFDvTD;
				case 1:
					if (JX6LTjOdZl())
					{
						return MexLkeSdtr.QBOlYV8sa;
					}
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e0ae04f6cf1849cb86b531d7a0262df0 != 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}
	}

	public override string Description
	{
		get
		{
			int num = 6;
			int num2 = num;
			string text2 = default(string);
			string text = default(string);
			string text3 = default(string);
			while (true)
			{
				string text4;
				switch (num2)
				{
				case 3:
					text2 = Localization.T("psp.item.details", text, bPwLaZTLHx());
					num2 = 2;
					break;
				case 1:
					return text3 + "\n\n" + text2;
				case 2:
					if (string.IsNullOrWhiteSpace(text3))
					{
						return text2;
					}
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1c87508dc9684f9ba156e3fa973ff661 == 0)
					{
						num2 = 1;
					}
					break;
				case 4:
					text4 = MexLkeSdtr.IPcxfnHIIW;
					goto IL_00fc;
				case 6:
					if (JX6LTjOdZl())
					{
						num2 = 3;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_bb6ae66dd5634157aa1fdece0b45c4d4 == 0)
						{
							num2 = 5;
						}
						break;
					}
					goto case 4;
				case 5:
					text4 = MexLkeSdtr.U3JxxxuvDj;
					goto IL_00fc;
				default:
					{
						text = string.Join(", ", MexLkeSdtr.tsKxMGkwPJ.Select(pKiLH7ZuBb));
						num2 = 3;
						break;
					}
					IL_00fc:
					text3 = text4;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cefc85d5eea849199c84f01615ccfcdd != 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}
	}

	public override Sprite Icon
	{
		get
		{
			int num = 8;
			int num2 = num;
			Sprite nk7LYBLuYX = default(Sprite);
			string text = default(string);
			while (true)
			{
				switch (num2)
				{
				case 1:
				case 4:
					nk7LYBLuYX = TfINTYLZZCol8GIcTaW.PRXLuToh88("credits");
					num2 = 15;
					break;
				case 2:
					return Nk7LYBLuYX;
				case 7:
					text = MexLkeSdtr.egakK1113;
					num2 = 11;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4456f4be186044e0b1a4dc9bfe657743 != 0)
					{
						num2 = 5;
					}
					break;
				case 3:
					if (text == "CREDITS")
					{
						num2 = 4;
						break;
					}
					goto case 10;
				case 11:
					if (!(text == "ITEM"))
					{
						num2 = 3;
						break;
					}
					goto case 5;
				case 12:
					nk7LYBLuYX = TfINTYLZZCol8GIcTaW.PRXLuToh88("wildFavor");
					num2 = 13;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1223010f7ae043a8986881cac090145c == 0)
					{
						num2 = 3;
					}
					break;
				case 10:
					if (!(text == "WILD_FAVOR"))
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_65008faf8c9e442a9ed1f702ae18c527 != 0)
						{
							num2 = 0;
						}
						break;
					}
					goto case 12;
				case 14:
					return Nk7LYBLuYX;
				default:
					nk7LYBLuYX = null;
					num2 = 9;
					break;
				case 8:
					if (!((Object)(object)Nk7LYBLuYX != (Object)null))
					{
						num2 = 7;
						break;
					}
					goto case 2;
				case 5:
					nk7LYBLuYX = RenderHandler.LoadFromAtlas(MexLkeSdtr.sLpxI0xu8Q, MexLkeSdtr.QuyxD10drL);
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_5259db2402bd4af18b00f3f78d623a6d == 0)
					{
						num2 = 4;
					}
					break;
				case 6:
				case 9:
				case 13:
				case 15:
					Nk7LYBLuYX = nk7LYBLuYX;
					num2 = 11;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d6357a19e71a493190300e7ae5779dd7 != 0)
					{
						num2 = 14;
					}
					break;
				}
			}
		}
	}

	public override int RareLv => 0;

	internal TeJZ2ELr25KIGhra8Tx(lxaYMLBUJSOI9qKHmn P_0)
	{
		bpND7PhQOXpROODtSab.XR4RtoBqtq();
		base._002Ector();
		int num = 1;
		if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_5259db2402bd4af18b00f3f78d623a6d == 0)
		{
			num = 1;
		}
		while (true)
		{
			switch (num)
			{
			default:
				return;
			case 0:
				return;
			case 1:
				MexLkeSdtr = P_0;
				num = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_94b27b4eaf334086b7ba5cdf39a841c4 == 0)
				{
					num = 0;
				}
				break;
			}
		}
	}

	[SpecialName]
	internal lxaYMLBUJSOI9qKHmn DHsLBqp1sA()
	{
		return MexLkeSdtr;
	}

	[SpecialName]
	private static bool JX6LTjOdZl()
	{
		return string.Equals(Localization.T("psp.language.code"), "zh-CN", StringComparison.OrdinalIgnoreCase);
	}

	public override void OnClick(int P_0)
	{
		int num = 8;
		int num2 = num;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = default(DefaultInterpolatedStringHandler);
		string text = default(string);
		while (true)
		{
			switch (num2)
			{
			case 11:
				return;
			case 6:
				ModLogger.Error("[ProbablyStolenPlaytest] Unknown catalog kind: " + MexLkeSdtr.egakK1113 + ".");
				num2 = 14;
				continue;
			case 8:
				if (P_0 > 0)
				{
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_32d9de0be9df48bb91fd60adb55c440d == 0)
					{
						num2 = 7;
					}
					continue;
				}
				break;
			case 13:
				defaultInterpolatedStringHandler.AppendFormatted(P_0);
				num2 = 16;
				continue;
			case 1:
				defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] Add rejected for ");
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1b4fe3e0fc2c40fc86f25cd1934dae42 == 0)
				{
					num2 = 1;
				}
				continue;
			case 10:
				return;
			case 16:
				defaultInterpolatedStringHandler.AppendLiteral(".");
				num2 = 17;
				continue;
			case 4:
				return;
			case 7:
				text = MexLkeSdtr.egakK1113;
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc28089b23154bd0af6dea698a7995ab != 0)
				{
					num2 = 15;
				}
				continue;
			case 14:
				return;
			case 15:
				if (text == "CREDITS")
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b33a46eca689495eb7b574324d630333 != 0)
					{
						num2 = 0;
					}
					continue;
				}
				goto case 18;
			case 17:
				ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
				num2 = 4;
				continue;
			case 18:
				if (!(text == "WILD_FAVOR"))
				{
					num2 = 11;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_530166a6640e4d928390290fcc4f133b == 0)
					{
						num2 = 19;
					}
				}
				else
				{
					SpvprrM2pJC2lXLvcU.V1jj3CJjx(P_0);
					num2 = 10;
				}
				continue;
			case 3:
				defaultInterpolatedStringHandler.AppendLiteral(": quantity=");
				num2 = 13;
				continue;
			case 12:
				return;
			case 19:
				if (text == "ITEM")
				{
					rC5LNitnrc(P_0);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0c883aef9e214bdbbe2f931c98600d64 != 0)
					{
						num2 = 11;
					}
				}
				else
				{
					num2 = 6;
				}
				continue;
			default:
				SpvprrM2pJC2lXLvcU.Y375Hm9pk(P_0);
				num2 = 12;
				continue;
			case 2:
				defaultInterpolatedStringHandler.AppendFormatted(MexLkeSdtr.VXSVMCqdI);
				num2 = 3;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_27869b3cfc9541a994ff2c4f4b3d6928 != 0)
				{
					num2 = 0;
				}
				continue;
			case 5:
				break;
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 2);
			num2 = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f86bab81e9874f01a080bbd11feacb4c != 0)
			{
				num2 = 1;
			}
		}
	}

	private void rC5LNitnrc(int P_0)
	{
		GameGridInventory val = wAm1Zj0g39ZPo5iwxg.etAOsRlAA();
		if (val == null)
		{
			ModLogger.Warning("[ProbablyStolenPlaytest] Add item skipped: main inventory is unavailable.");
			return;
		}
		int num = 0;
		try
		{
			for (int i = 0; i < P_0; i++)
			{
				GameItem val2 = DirectoryMaster.Item(MexLkeSdtr.VXSVMCqdI, true);
				if (val2 == null)
				{
					throw new InvalidOperationException("DirectoryMaster rejected " + MexLkeSdtr.VXSVMCqdI + ".");
				}
				r9QFjeLRJlKUX1YROOX.Q93LmMNTQG(val2, MexLkeSdtr.VXSVMCqdI);
				if (!((GameInventory)val).MayHaveValidInventorySlot(val2))
				{
					ModLogger.Warning($"[ProbablyStolenPlaytest] Inventory has no valid slot after adding {num}/{P_0} x {DisplayName}.");
					break;
				}
				if (!((GameInventory)val).UncheckedAccept(val2))
				{
					ModLogger.Warning($"[ProbablyStolenPlaytest] Native inventory rejected {DisplayName} after adding {num}/{P_0}.");
					break;
				}
				num++;
			}
			ModLogger.Info($"[ProbablyStolenPlaytest] Added {num}/{P_0} x {DisplayName} ({MexLkeSdtr.VXSVMCqdI}).");
		}
		catch (Exception value)
		{
			ModLogger.Error($"[ProbablyStolenPlaytest] Add item failed for {MexLkeSdtr.VXSVMCqdI} after {num}/{P_0}: {value}");
		}
	}

	private int bPwLaZTLHx()
	{
		if (MexLkeSdtr.egakK1113 == "CREDITS")
		{
			return SpvprrM2pJC2lXLvcU.rsmApljV9();
		}
		if (MexLkeSdtr.egakK1113 == "WILD_FAVOR")
		{
			return SpvprrM2pJC2lXLvcU.POh7VoZWk();
		}
		int num = 0;
		try
		{
			GameGridInventory val = wAm1Zj0g39ZPo5iwxg.etAOsRlAA();
			if (((val != null) ? ((GameInventory)val).childItems : null) == null)
			{
				return 0;
			}
			Enumerator<GameItem> enumerator = ((GameInventory)val).childItems.GetEnumerator();
			while (enumerator.MoveNext())
			{
				GameItem current = enumerator.Current;
				if (current != null && string.Equals(current.identifier, MexLkeSdtr.VXSVMCqdI, StringComparison.Ordinal))
				{
					num = checked(num + Math.Max(1, current.unitCount));
				}
			}
		}
		catch (Exception ex)
		{
			ModLogger.Warning("[ProbablyStolenPlaytest] Held count failed for " + MexLkeSdtr.VXSVMCqdI + ": " + ex.Message);
		}
		return num;
	}

	internal static string pKiLH7ZuBb(object P_0)
	{
		return Localization.T("psp.category." + ((string)P_0).ToLowerInvariant());
	}

	internal static bool DqW39771Pwlix2ZiWs0()
	{
		return w8kJnQ7MPXf7eJpW2I5 == null;
	}

	internal static TeJZ2ELr25KIGhra8Tx iueKG07AJcyaZouhNPk()
	{
		return w8kJnQ7MPXf7eJpW2I5;
	}
}
