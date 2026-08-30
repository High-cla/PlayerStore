using System;
using System.Runtime.CompilerServices;
using ModFramework.Commands;
using ModFramework.Components;
using ModFramework.GUI;
using ModFramework.Utilities.Attributes;
using TyOQ7hhkasLPlhFR3an;
using UnityEngine;
using byB3SM1jfs9KMIIOGh;

namespace ProbablyStolenPlaytest.Components;

[RegisterInIl2Cpp]
public sealed class ProbablyStolenPlaytestComponent : CommandComponentBase
{
	[CompilerGenerated]
	private static ProbablyStolenPlaytestComponent? YZgLlZRqeI;

	private static ProbablyStolenPlaytestComponent? AsEyEu7jG5qPXWPuw0i;

	public new static ProbablyStolenPlaytestComponent? Instance
	{
		[CompilerGenerated]
		get
		{
			return YZgLlZRqeI;
		}
		[CompilerGenerated]
		private set
		{
			YZgLlZRqeI = yZgLlZRqeI;
		}
	}

	public ProbablyStolenPlaytestComponent(IntPtr ptr)
	{
		bpND7PhQOXpROODtSab.XR4RtoBqtq();
		base._002Ector(ptr);
		int num = 0;
		if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_47c9d108ec114961a58b1bfbfefe6bab == 0)
		{
			num = 0;
		}
		while (true)
		{
			switch (num)
			{
			case 1:
				return;
			}
			Instance = this;
			num = 1;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2af9e2b953064803b9db93f92d6e9d4e == 0)
			{
				num = 1;
			}
		}
	}

	protected override void Awake()
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_bb6ae66dd5634157aa1fdece0b45c4d4 == 0)
				{
					num2 = 2;
				}
				break;
			case 1:
				base.Awake();
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c30761e1724845948fdd25fb5a79280d != 0)
				{
					num2 = 0;
				}
				break;
			case 2:
				return;
			}
		}
	}

	[Command("TogglePanel")]
	public void TogglePanel(bool visible)
	{
		int num = 5;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 6:
				return;
			case 4:
			{
				UIService instance = UIService.Instance;
				if (instance == null)
				{
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_823dececcbca4b5a81ec3c316b1230e5 == 0)
					{
						num2 = 3;
					}
					continue;
				}
				instance.Hide();
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2af9e2b953064803b9db93f92d6e9d4e != 0)
				{
					num2 = 2;
				}
				continue;
			}
			case 5:
				if (!visible)
				{
					num2 = 4;
					continue;
				}
				break;
			case 2:
				return;
			case 1:
				break;
			case 0:
				return;
			case 3:
				return;
			}
			UIService instance2 = UIService.Instance;
			if (instance2 == null)
			{
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fe4f9d6dc55a460394d4814bd118c0dd == 0)
				{
					num2 = 0;
				}
			}
			else
			{
				instance2.Show();
				num2 = 6;
			}
		}
	}

	[Command("AddCredits")]
	public void AddCredits(int amount)
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 1:
				SpvprrM2pJC2lXLvcU.Y375Hm9pk(amount);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e7642139c72f4727920bbb31032e9427 == 0)
				{
					num2 = 0;
				}
				break;
			case 0:
				return;
			}
		}
	}

	[Command("AddWildFavor")]
	public void AddWildFavor(int amount)
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 0:
				return;
			case 1:
				SpvprrM2pJC2lXLvcU.V1jj3CJjx(amount);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d6357a19e71a493190300e7ae5779dd7 != 0)
				{
					num2 = 0;
				}
				break;
			}
		}
	}

	[Command("SetStoreAttractiveness")]
	public void SetStoreAttractiveness(int target)
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 0:
				return;
			case 1:
				SpvprrM2pJC2lXLvcU.l5LRyZ3nY(target);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f9d9dfa1e4dc433a863e13dcda66c695 == 0)
				{
					num2 = 0;
				}
				break;
			}
		}
	}

	internal static bool TCKhfe7RPntnEPs5rbl()
	{
		return AsEyEu7jG5qPXWPuw0i == null;
	}

	internal static ProbablyStolenPlaytestComponent? R46xVO7qExYgmhwcJNp()
	{
		return AsEyEu7jG5qPXWPuw0i;
	}
}
