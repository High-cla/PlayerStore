using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ModFramework;
using ModFramework.GUI;
using TyOQ7hhkasLPlhFR3an;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace E1edgYxSHVYFaeHy7BP;

internal static class sfumh2xPLltR4pL0i9k
{
	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass12_0
	{
		public Func<List<SelectionDefinition>> TYG8FGy0KS;

		public object xjc8ho38la;

		public PropertyPanel Pm58Iafvdy;

		internal static _003C_003Ec__DisplayClass12_0 TtZaZB79SxlOom2xXAq;

		public _003C_003Ec__DisplayClass12_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void J8o8fMlf8p()
		{
			List<SelectionDefinition> list = new List<SelectionDefinition>();
			foreach (SelectionDefinition item in TYG8FGy0KS())
			{
				_003C_003Ec__DisplayClass12_1 CS_0024_003C_003E8__locals16 = new _003C_003Ec__DisplayClass12_1();
				CS_0024_003C_003E8__locals16.reI83xFSYn = this;
				CS_0024_003C_003E8__locals16.HsQ8dHdJPX = item;
				CS_0024_003C_003E8__locals16.Veq8DtRqe8 = CS_0024_003C_003E8__locals16.HsQ8dHdJPX.Label;
				list.Add(new SelectionDefinition
				{
					Label = CS_0024_003C_003E8__locals16.Veq8DtRqe8,
					Icon = CS_0024_003C_003E8__locals16.HsQ8dHdJPX.Icon,
					IconLoader = CS_0024_003C_003E8__locals16.HsQ8dHdJPX.IconLoader,
					OnSelected = delegate
					{
						int num = 5;
						int num2 = num;
						Image val = default(Image);
						while (true)
						{
							switch (num2)
							{
							case 6:
								UwBxNv082y(CS_0024_003C_003E8__locals16.reI83xFSYn.Pm58Iafvdy, "RefreshSelectionDesc", CS_0024_003C_003E8__locals16.reI83xFSYn.xjc8ho38la);
								num2 = 2;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
								{
									num2 = 10;
								}
								break;
							case 5:
							{
								Action onSelected = CS_0024_003C_003E8__locals16.HsQ8dHdJPX.OnSelected;
								if (onSelected == null)
								{
									num2 = 0;
									if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eae2aa4db32f466c8ef85fe24af9100e != 0)
									{
										num2 = 4;
									}
								}
								else
								{
									onSelected();
									num2 = 9;
								}
								break;
							}
							case 10:
								return;
							case 3:
							{
								Action<Image> iconLoader = CS_0024_003C_003E8__locals16.HsQ8dHdJPX.IconLoader;
								if (iconLoader == null)
								{
									num2 = 2;
								}
								else
								{
									iconLoader(val);
									num2 = 7;
								}
								break;
							}
							case 1:
								if ((Object)(object)val != (Object)null)
								{
									num2 = 8;
									break;
								}
								goto case 2;
							case 2:
							case 7:
								ELTxbXaVOK(CS_0024_003C_003E8__locals16.reI83xFSYn.xjc8ho38la, "currentLabel", CS_0024_003C_003E8__locals16.Veq8DtRqe8);
								num2 = 5;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3ab0aad8161d42b396e76201b56e6767 != 0)
								{
									num2 = 6;
								}
								break;
							case 4:
							case 9:
								val = zWQx4nT6Yw<Image>(CS_0024_003C_003E8__locals16.reI83xFSYn.xjc8ho38la, "icon");
								num2 = 1;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4606931d409d4632bb5c9d4bf6dc4c74 == 0)
								{
									num2 = 1;
								}
								break;
							case 8:
								if ((Object)(object)CS_0024_003C_003E8__locals16.HsQ8dHdJPX.Icon != (Object)null)
								{
									num2 = 0;
									if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
									{
										num2 = 0;
									}
									break;
								}
								goto case 3;
							default:
								val.sprite = CS_0024_003C_003E8__locals16.HsQ8dHdJPX.Icon;
								num2 = 3;
								break;
							}
						}
					}
				});
			}
			dXbxtIoDyG(zWQx4nT6Yw<TargetSelectionWidget>(Pm58Iafvdy, "targetSelectionWidget"), list);
		}

		internal static bool g0p5PG72DxPaNiZ8euJ()
		{
			return TtZaZB79SxlOom2xXAq == null;
		}

		internal static _003C_003Ec__DisplayClass12_0 lSiN0L7Udf8HuYJo5cd()
		{
			return TtZaZB79SxlOom2xXAq;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass12_1
	{
		public SelectionDefinition HsQ8dHdJPX;

		public string Veq8DtRqe8;

		public _003C_003Ec__DisplayClass12_0 reI83xFSYn;

		internal static _003C_003Ec__DisplayClass12_1 RZ8HOl7XL57mCwLvU62;

		public _003C_003Ec__DisplayClass12_1()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fce81e3a8ede47fab4085a25772731cb != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void GSW8wlJA5V()
		{
			int num = 5;
			int num2 = num;
			Image val = default(Image);
			while (true)
			{
				switch (num2)
				{
				case 6:
					UwBxNv082y(reI83xFSYn.Pm58Iafvdy, "RefreshSelectionDesc", reI83xFSYn.xjc8ho38la);
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
					{
						num2 = 10;
					}
					break;
				case 5:
				{
					Action onSelected = HsQ8dHdJPX.OnSelected;
					if (onSelected == null)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eae2aa4db32f466c8ef85fe24af9100e != 0)
						{
							num2 = 4;
						}
					}
					else
					{
						onSelected();
						num2 = 9;
					}
					break;
				}
				case 10:
					return;
				case 3:
				{
					Action<Image> iconLoader = HsQ8dHdJPX.IconLoader;
					if (iconLoader == null)
					{
						num2 = 2;
						break;
					}
					iconLoader(val);
					num2 = 7;
					break;
				}
				case 1:
					if ((Object)(object)val != (Object)null)
					{
						num2 = 8;
						break;
					}
					goto case 2;
				case 2:
				case 7:
					ELTxbXaVOK(reI83xFSYn.xjc8ho38la, "currentLabel", Veq8DtRqe8);
					num2 = 5;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3ab0aad8161d42b396e76201b56e6767 != 0)
					{
						num2 = 6;
					}
					break;
				case 4:
				case 9:
					val = zWQx4nT6Yw<Image>(reI83xFSYn.xjc8ho38la, "icon");
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4606931d409d4632bb5c9d4bf6dc4c74 == 0)
					{
						num2 = 1;
					}
					break;
				case 8:
					if ((Object)(object)HsQ8dHdJPX.Icon != (Object)null)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
						{
							num2 = 0;
						}
						break;
					}
					goto case 3;
				default:
					val.sprite = HsQ8dHdJPX.Icon;
					num2 = 3;
					break;
				}
			}
		}

		internal static bool NflBVO74wCn13d7rJJ9()
		{
			return RZ8HOl7XL57mCwLvU62 == null;
		}

		internal static _003C_003Ec__DisplayClass12_1 Gr7pQc7r72g0lWS2AXs()
		{
			return RZ8HOl7XL57mCwLvU62;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass16_0
	{
		public GenericItemEntry T3U8MPHUM1;

		private static _003C_003Ec__DisplayClass16_0 S6Tkgl7bJx0OrayavLC;

		public _003C_003Ec__DisplayClass16_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_988f5d1238d043129fbd30a9c933ca73 != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void wAi8nceyw7()
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
					UwBxNv082y(T3U8MPHUM1, "OnButtonClick");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b33a46eca689495eb7b574324d630333 == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal static bool cZMO567NdQWLjfh1Ns1()
		{
			return S6Tkgl7bJx0OrayavLC == null;
		}

		internal static _003C_003Ec__DisplayClass16_0 GgAnDQ7arlWCjOKoEbF()
		{
			return S6Tkgl7bJx0OrayavLC;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass17_0
	{
		public GenericItemPanel hpp85aBktr;

		internal static _003C_003Ec__DisplayClass17_0 l3lSva7Hn8GRhtLPXlT;

		public _003C_003Ec__DisplayClass17_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_9499cd174e734581806278a135f9219f == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal int Gf181TkxJR()
		{
			int num = 1;
			int num2 = num;
			object obj;
			while (true)
			{
				switch (num2)
				{
				case 1:
					obj = UwBxNv082y(hpp85aBktr, "GetQuantity");
					if (obj == null)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d326bf98637481998cf110a56c1622c == 0)
						{
							num2 = 0;
						}
						continue;
					}
					break;
				default:
					obj = 1;
					break;
				}
				break;
			}
			return (int)obj;
		}

		internal void bae8AyOiuH(string _)
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
					UwBxNv082y(hpp85aBktr, "RefreshItems");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_34bc60c937894c178241f751744daf7b != 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal void PUn87FFJ9K()
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
					UwBxNv082y(hpp85aBktr, "OnConfirmQuantity");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4950e2973d2441438c6b262a624ae1e4 != 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal void AwI8si61yF()
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
					UwBxNv082y(hpp85aBktr, "OnCancelQuantity");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_63e4d775e4914aaf953a5ccf1ba12bff == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal static bool NiXQqo7BbVSNu2KxqL5()
		{
			return l3lSva7Hn8GRhtLPXlT == null;
		}

		internal static _003C_003Ec__DisplayClass17_0 Sc91Sj7JHh4UufhZRfJ()
		{
			return l3lSva7Hn8GRhtLPXlT;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass19_0
	{
		public GenericItemPanel qCA8jmZCXA;

		private static _003C_003Ec__DisplayClass19_0 NaJ1eP7TF29NOo68YiF;

		public _003C_003Ec__DisplayClass19_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d5546cacfc14d65b5f225b6fd1f036b == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal static bool aIbeIL7QE8W1YmaTZeI()
		{
			return NaJ1eP7TF29NOo68YiF == null;
		}

		internal static _003C_003Ec__DisplayClass19_0 LWoME27kZo38PcQfRK9()
		{
			return NaJ1eP7TF29NOo68YiF;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass19_1
	{
		public int dRd8qOKNec;

		public _003C_003Ec__DisplayClass19_0 vGW8mGykbu;

		private static _003C_003Ec__DisplayClass19_1 ReYSnm7Ye0SsFAwJWJg;

		public _003C_003Ec__DisplayClass19_1()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fe4f9d6dc55a460394d4814bd118c0dd == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void w9B8RShusD(bool isOn)
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
					if (!isOn)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a536dae990ff49e894149992c3d6ff55 == 0)
						{
							num2 = 0;
						}
						continue;
					}
					break;
				case 3:
					break;
				case 0:
					return;
				case 2:
					return;
				}
				SeXxY0vPPJ(vGW8mGykbu.qCA8jmZCXA, dRd8qOKNec);
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1f9e3467df7e4ab2b9086d9e74e2921e == 0)
				{
					num2 = 2;
				}
			}
		}

		internal static bool N15h1q7Zy0ciV6YpTiN()
		{
			return ReYSnm7Ye0SsFAwJWJg == null;
		}

		internal static _003C_003Ec__DisplayClass19_1 xpcaDC7VeWOVlVOVd3G()
		{
			return ReYSnm7Ye0SsFAwJWJg;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass20_0
	{
		public GenericItemPanel vNh80Rwpuw;

		private static _003C_003Ec__DisplayClass20_0 SZZoPp7uhnZ8XbO6hCY;

		public _003C_003Ec__DisplayClass20_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0feacf0bf00d4671bdcba77460093fd1 == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal static bool k6hYHE7pE8A4FNDOdY4()
		{
			return SZZoPp7uhnZ8XbO6hCY == null;
		}

		internal static _003C_003Ec__DisplayClass20_0 wWAfBR7t9oASyXaaVfu()
		{
			return SZZoPp7uhnZ8XbO6hCY;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass20_1
	{
		public int IGj8OtApl2;

		public _003C_003Ec__DisplayClass20_0 XnW8W9spcf;

		internal static _003C_003Ec__DisplayClass20_1 thk5jI7vKrJfEbs2vHM;

		public _003C_003Ec__DisplayClass20_1()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_ad54c7c384e2403b8eedf340fa3b3f17 == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void VuV8yNLA0w(bool isOn)
		{
			int num = 2;
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				default:
					return;
				case 2:
					if (isOn)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e536e969c0ee481f86575e72192940a2 == 0)
						{
							num2 = 1;
						}
						break;
					}
					return;
				case 1:
					OPFxZeptyC(XnW8W9spcf.vNh80Rwpuw, IGj8OtApl2);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_32d9de0be9df48bb91fd60adb55c440d != 0)
					{
						num2 = 0;
					}
					break;
				case 0:
					return;
				}
			}
		}

		internal static bool eCraxu7cd4JbSvmnYWF()
		{
			return thk5jI7vKrJfEbs2vHM == null;
		}

		internal static _003C_003Ec__DisplayClass20_1 HsKCBf7lBYVr1raEgyd()
		{
			return thk5jI7vKrJfEbs2vHM;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass28_0
	{
		public TargetSelectionWidget Y728eZMxXR;

		internal static _003C_003Ec__DisplayClass28_0 uWThnR7Gm7VCS3RD5sy;

		public _003C_003Ec__DisplayClass28_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_faf158e93f2347898dabc53af2c2e79a == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void FaV8o8R73x(string _)
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
					jtqxvtF6R2(Y728eZMxXR);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cb955e4f34e14b2c8322a115d2a10521 == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal static bool JemVQk7E38fNAxRecgy()
		{
			return uWThnR7Gm7VCS3RD5sy == null;
		}

		internal static _003C_003Ec__DisplayClass28_0 B6T1hR7gPYLbdZBZ7cR()
		{
			return uWThnR7Gm7VCS3RD5sy;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass29_0
	{
		public string DhZ8KtgP2c;

		public TargetSelectionWidget R4t8Pbb8h8;

		private static _003C_003Ec__DisplayClass29_0 E7Z3vO7zXlNRiM2evhK;

		public _003C_003Ec__DisplayClass29_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2df26eab4c194a2cabc45ac854ca3536 == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal bool vWu8CjV4WC(SelectionDefinition definition)
		{
			int num = 2;
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 2:
					if (definition.Label == null)
					{
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c8898d2d3f504ae2b2053ed60248dbad != 0)
						{
							num2 = 0;
						}
						break;
					}
					goto default;
				default:
					return definition.Label.IndexOf(DhZ8KtgP2c, StringComparison.OrdinalIgnoreCase) >= 0;
				case 1:
					return false;
				}
			}
		}

		internal static bool zLuTpYs6iektJm5UYay()
		{
			return E7Z3vO7zXlNRiM2evhK == null;
		}

		internal static _003C_003Ec__DisplayClass29_0 ff5sePsx86ZT0AiLJcg()
		{
			return E7Z3vO7zXlNRiM2evhK;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass29_1
	{
		public SelectionDefinition sFF8iFVmHi;

		public _003C_003Ec__DisplayClass29_0 IhX89Dct5i;

		internal static _003C_003Ec__DisplayClass29_1 MjQXRdsL1TVj5FEv557;

		public _003C_003Ec__DisplayClass29_1()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cb955e4f34e14b2c8322a115d2a10521 != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void k7l8SREKap()
		{
			int num = 3;
			while (true)
			{
				int num2 = num;
				while (true)
				{
					switch (num2)
					{
					case 1:
						return;
					default:
						IhX89Dct5i.R4t8Pbb8h8.Close();
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e812b35a98764d43b77f4c9af4c260f0 != 0)
						{
							num2 = 0;
						}
						break;
					case 3:
					{
						Action onSelected = sFF8iFVmHi.OnSelected;
						if (onSelected == null)
						{
							num = 2;
							goto end_IL_0012;
						}
						onSelected();
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d5546cacfc14d65b5f225b6fd1f036b != 0)
						{
							num2 = 0;
						}
						break;
					}
					}
					continue;
					end_IL_0012:
					break;
				}
			}
		}

		internal static bool rIubKjs8AIo0twpOwjt()
		{
			return MjQXRdsL1TVj5FEv557 == null;
		}

		internal static _003C_003Ec__DisplayClass29_1 DjpQBWsfAOIAqWuoUlG()
		{
			return MjQXRdsL1TVj5FEv557;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass33_0
	{
		public int civ8UyBx1H;

		private static _003C_003Ec__DisplayClass33_0 riPAcmsF1ZonBi4KPOU;

		public _003C_003Ec__DisplayClass33_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1223010f7ae043a8986881cac090145c != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal void rGm82UcORu()
		{
			int num = 1;
			int num2 = num;
			Action value = default(Action);
			while (true)
			{
				switch (num2)
				{
				default:
					return;
				case 0:
					return;
				case 3:
					return;
				case 2:
					value();
					num2 = 3;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d51c61ea1cd54949ab81b08153329f54 != 0)
					{
						num2 = 3;
					}
					break;
				case 1:
					if (!cW0LhHNIji.TryGetValue(civ8UyBx1H, out value))
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cd1492aa96ff4ba5a032458f454e6a9d != 0)
						{
							num2 = 0;
						}
						break;
					}
					goto case 2;
				}
			}
		}

		internal static bool OirJnKsh9IHHYZ6Nebq()
		{
			return riPAcmsF1ZonBi4KPOU == null;
		}

		internal static _003C_003Ec__DisplayClass33_0 u3ZsrosIh2hlhQuFmg9()
		{
			return riPAcmsF1ZonBi4KPOU;
		}
	}

	private static readonly Dictionary<int, UnityAction<bool>> kM8LfcjDsB;

	private static readonly Dictionary<int, UnityAction<string>> CY8LFjElpL;

	private static readonly Dictionary<int, Action> cW0LhHNIji;

	private static readonly HashSet<int> O4kLIRR76H;

	private static sfumh2xPLltR4pL0i9k iljI6cAVw2Mw59CYFoN;

	[SpecialName]
	internal static bool QcML6i0UQy()
	{
		return typeof(UnityEventBase).GetMethod("RemoveAllListeners", BindingFlags.Instance | BindingFlags.Public) == null;
	}

	[SpecialName]
	internal static bool JYyLLNZjSx()
	{
		return typeof(Animator).Assembly.GetType("UnityEngine.AnimatorUpdateMode", throwOnError: false) == null;
	}

	internal static void afdxikgtY9(object P_0, object P_1)
	{
		int num = 2;
		TargetSelectionWidget targetSelectionWidget2 = default(TargetSelectionWidget);
		Transform val = default(Transform);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				TargetSelectionWidget targetSelectionWidget;
				switch (num2)
				{
				default:
					throw new InvalidOperationException("Framework TargetSelectionWidget could not be created.");
				case 12:
					if ((Object)(object)targetSelectionWidget2 == (Object)null)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a536dae990ff49e894149992c3d6ff55 == 0)
						{
							num2 = 0;
						}
						continue;
					}
					goto end_IL_0012;
				case 3:
					f7ax9334Pu(P_1);
					num2 = 5;
					continue;
				case 5:
				{
					Transform obj = UIService.Instance.CanvasRoot.transform.Find("Popup Base");
					if (obj == null)
					{
						num2 = 11;
						continue;
					}
					val = obj;
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a2f40808d89d4596affd6998f404e4de == 0)
					{
						num2 = 1;
					}
					continue;
				}
				case 9:
					WkPxpRLvUn();
					num2 = 10;
					continue;
				case 8:
					zLaxV4dsG3(targetSelectionWidget2);
					num2 = 9;
					continue;
				case 10:
					return;
				case 6:
					targetSelectionWidget = ((Component)val).GetComponent<TargetSelectionWidget>();
					if (targetSelectionWidget == null)
					{
						num2 = 7;
						continue;
					}
					break;
				case 4:
					return;
				case 1:
					ikqxHVbHNn(P_0);
					num2 = 3;
					continue;
				case 2:
					if (!QcML6i0UQy())
					{
						return;
					}
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_17d7753b0a8f441e9045e59389b1db42 == 0)
					{
						num2 = 1;
					}
					continue;
				case 11:
					throw new InvalidOperationException("Framework Popup Base was not found.");
				case 7:
					targetSelectionWidget = ((Component)val).gameObject.AddComponent<TargetSelectionWidget>();
					break;
				}
				targetSelectionWidget2 = targetSelectionWidget;
				num2 = 12;
				continue;
				end_IL_0012:
				break;
			}
			ELTxbXaVOK(P_1, "targetSelectionWidget", targetSelectionWidget2);
			num = 8;
		}
	}

	internal static void f7ax9334Pu(object P_0)
	{
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		int num = 17;
		GameObject val3 = default(GameObject);
		Transform val = default(Transform);
		GameObject val4 = default(GameObject);
		GameObject gameObject = default(GameObject);
		Transform transform = default(Transform);
		int num3 = default(int);
		int num4 = default(int);
		GameObject val2 = default(GameObject);
		GameObject val5 = default(GameObject);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				object obj3;
				object obj2;
				object obj;
				object obj4;
				switch (num2)
				{
				case 28:
					IgBx2nygf5(val3);
					num2 = 25;
					continue;
				case 32:
				{
					Transform obj8 = val3.transform.Find("Mask Content/Content/Entries");
					if (obj8 == null)
					{
						num2 = 15;
						continue;
					}
					obj3 = ((Component)obj8).gameObject;
					goto IL_048a;
				}
				case 17:
				{
					Transform obj9 = ((Component)P_0).transform.Find("Content/Group List/List");
					if (obj9 == null)
					{
						num2 = 16;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_dcfa316cbf1043838ee9b641776e7534 == 0)
						{
							num2 = 3;
						}
						continue;
					}
					val = obj9;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f86bab81e9874f01a080bbd11feacb4c != 0)
					{
						num2 = 9;
					}
					continue;
				}
				case 10:
					ELTxbXaVOK(P_0, "entryPrefab", val4);
					num2 = 18;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a3598edee5a5455f8d8037af2b4a9ae6 == 0)
					{
						num2 = 0;
					}
					continue;
				case 23:
					if ((Object)(object)gameObject.GetComponent<PropertyGroup>() != (Object)null)
					{
						num2 = 27;
						continue;
					}
					goto case 2;
				default:
				{
					Transform obj6 = val.Find("Button Element");
					if (obj6 == null)
					{
						goto end_IL_0012;
					}
					obj2 = ((Component)obj6).gameObject;
					goto IL_04d2;
				}
				case 1:
					ELTxbXaVOK(P_0, "groupPrefab", val3);
					num2 = 10;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3192da3ead8846b09a16ca828ae45a1b == 0)
					{
						num2 = 1;
					}
					continue;
				case 13:
					transform = val4.transform;
					num2 = 14;
					continue;
				case 5:
					return;
				case 25:
					num3 = 0;
					num2 = 12;
					continue;
				case 14:
					num4 = 0;
					num2 = 24;
					continue;
				case 6:
					val3.SetActive(false);
					num2 = 30;
					continue;
				case 34:
					val2.SetActive(false);
					num2 = 5;
					continue;
				case 2:
					num3++;
					num2 = 21;
					continue;
				case 30:
					val4.SetActive(false);
					num2 = 15;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f9d9dfa1e4dc433a863e13dcda66c695 != 0)
					{
						num2 = 34;
					}
					continue;
				case 22:
					num4++;
					num2 = 8;
					continue;
				case 29:
				{
					ButtonManager buttonManager = YvUxg3uTsw<ButtonManager>((object)val5);
					buttonManager.useHoverEffect = false;
					buttonManager.maxSize = 14f;
					buttonManager.startColor = new Color(1f, 1f, 1f, 0.0784f);
					num2 = 22;
					continue;
				}
				case 31:
					ELTxbXaVOK(P_0, "groupContainer", val);
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cefc85d5eea849199c84f01615ccfcdd == 0)
					{
						num2 = 1;
					}
					continue;
				case 12:
				case 21:
					if (num3 >= val.childCount)
					{
						num2 = 13;
						continue;
					}
					goto case 7;
				case 7:
					gameObject = ((Component)val.GetChild(num3)).gameObject;
					num2 = 23;
					continue;
				case 3:
					if (!((Object)(object)val5 == (Object)null))
					{
						num2 = 29;
						continue;
					}
					goto case 22;
				case 8:
				case 24:
					if (num4 >= transform.childCount)
					{
						num2 = 31;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_77a5c7c652c547f8ba0f41ab05d5f737 == 0)
						{
							num2 = 6;
						}
						continue;
					}
					goto case 33;
				case 27:
					IgBx2nygf5(gameObject);
					num2 = 2;
					continue;
				case 18:
					ELTxbXaVOK(P_0, "buttonPrefab", val2);
					num2 = 6;
					continue;
				case 33:
				{
					Transform obj7 = transform.GetChild(num4).Find("Input");
					if (obj7 == null)
					{
						num2 = 35;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1744e5e231764a669d60af3428ea8412 != 0)
						{
							num2 = 27;
						}
						continue;
					}
					obj = ((Component)obj7).gameObject;
					break;
				}
				case 16:
					throw new InvalidOperationException("Framework property group container was not found.");
				case 9:
				{
					Transform obj5 = val.Find("Group");
					if (obj5 == null)
					{
						num2 = 19;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cd1492aa96ff4ba5a032458f454e6a9d != 0)
						{
							num2 = 26;
						}
						continue;
					}
					obj4 = ((Component)obj5).gameObject;
					goto IL_043d;
				}
				case 26:
					obj4 = null;
					goto IL_043d;
				case 4:
					throw new InvalidOperationException("Framework property group prefab was not found.");
				case 15:
					obj3 = null;
					goto IL_048a;
				case 19:
					throw new InvalidOperationException("Framework property entry prefab was not found.");
				case 11:
					obj2 = null;
					goto IL_04d2;
				case 20:
					throw new InvalidOperationException("Framework property button prefab was not found.");
				case 35:
					{
						obj = null;
						break;
					}
					IL_043d:
					if (obj4 == null)
					{
						num2 = 4;
						continue;
					}
					val3 = (GameObject)obj4;
					num2 = 32;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_feeba9f081ea45948f90703016033e44 == 0)
					{
						num2 = 32;
					}
					continue;
					IL_04d2:
					if (obj2 == null)
					{
						num2 = 20;
						continue;
					}
					val2 = (GameObject)obj2;
					num2 = 28;
					continue;
					IL_048a:
					if (obj3 == null)
					{
						num2 = 19;
						continue;
					}
					val4 = (GameObject)obj3;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_acf4dbc23cf347efba74119535a62f63 == 0)
					{
						num2 = 0;
					}
					continue;
				}
				val5 = (GameObject)obj;
				num2 = 3;
				continue;
				end_IL_0012:
				break;
			}
			num = 11;
		}
	}

	private static void IgBx2nygf5(object P_0)
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
			{
				PropertyGroup propertyGroup = YvUxg3uTsw<PropertyGroup>(P_0);
				Transform obj = ((GameObject)P_0).transform.Find("Mask Content/Content");
				RectTransform value = ((obj != null) ? ((Component)obj).GetComponent<RectTransform>() : null) ?? throw new InvalidOperationException("Framework property group content was not found.");
				RectTransform value2 = ((GameObject)P_0).GetComponent<RectTransform>() ?? throw new InvalidOperationException("Framework property group RectTransform was not found.");
				CanvasGroup value3 = YvUxg3uTsw<CanvasGroup>(P_0);
				ELTxbXaVOK(propertyGroup, "contentRect", value);
				ELTxbXaVOK(propertyGroup, "objectRect", value2);
				ELTxbXaVOK(propertyGroup, "objectCG", value3);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_65008faf8c9e442a9ed1f702ae18c527 != 0)
				{
					num2 = 0;
				}
				break;
			}
			}
		}
	}

	internal static void CcAxUqlkg4(object P_0, List<IItemModel> P_1, List<FilterDefinition> P_2)
	{
		if (!QcML6i0UQy())
		{
			((GenericItemPanel)P_0).Configure(P_1, P_2);
			return;
		}
		List<IItemModel> value = P_1 ?? new List<IItemModel>();
		List<FilterDefinition> list = P_2 ?? new List<FilterDefinition>();
		ELTxbXaVOK(P_0, "allModels", value);
		ELTxbXaVOK(P_0, "filters", list);
		ELTxbXaVOK(P_0, "_configured", true);
		umqxJ6OibX(P_0);
		if (list.Count > 0)
		{
			SeXxY0vPPJ(P_0, 0);
		}
	}

	internal static void a3SxXHLQKC(object P_0, object P_1, object P_2, Func<List<SelectionDefinition>> P_3, object P_4)
	{
		_003C_003Ec__DisplayClass12_0 CS_0024_003C_003E8__locals33 = new _003C_003Ec__DisplayClass12_0();
		CS_0024_003C_003E8__locals33.TYG8FGy0KS = P_3;
		CS_0024_003C_003E8__locals33.Pm58Iafvdy = (PropertyPanel)P_0;
		if (!QcML6i0UQy())
		{
			CS_0024_003C_003E8__locals33.Pm58Iafvdy.SetupSelectionButtonForGroup((GameObject)P_1, (string)P_2, CS_0024_003C_003E8__locals33.TYG8FGy0KS);
			object obj = zWQx4nT6Yw<IDictionary>(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "selBtnMap")[P_1];
			if (obj != null)
			{
				ELTxbXaVOK(obj, "currentLabel", P_4);
				UwBxNv082y(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "RefreshSelectionDesc", obj);
			}
			return;
		}
		IDictionary dictionary = zWQx4nT6Yw<IDictionary>(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "selBtnMap");
		CS_0024_003C_003E8__locals33.xjc8ho38la = dictionary[P_1];
		if (CS_0024_003C_003E8__locals33.xjc8ho38la == null)
		{
			GameObject obj2 = zWQx4nT6Yw<GameObject>(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "buttonPrefab");
			Transform val = zWQx4nT6Yw<Transform>(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "groupContainer");
			GameObject val2 = Object.Instantiate<GameObject>(obj2, val, false);
			val2.transform.SetSiblingIndex(((GameObject)P_1).transform.GetSiblingIndex());
			CS_0024_003C_003E8__locals33.xjc8ho38la = UwBxNv082y(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "CreateSelectionButtonInfo", val2, P_2, CS_0024_003C_003E8__locals33.TYG8FGy0KS);
			dictionary[P_1] = CS_0024_003C_003E8__locals33.xjc8ho38la;
		}
		ELTxbXaVOK(CS_0024_003C_003E8__locals33.xjc8ho38la, "prefixKey", P_2);
		ELTxbXaVOK(CS_0024_003C_003E8__locals33.xjc8ho38la, "defsProvider", CS_0024_003C_003E8__locals33.TYG8FGy0KS);
		ELTxbXaVOK(CS_0024_003C_003E8__locals33.xjc8ho38la, "currentLabel", P_4);
		UwBxNv082y(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "RefreshSelectionDesc", CS_0024_003C_003E8__locals33.xjc8ho38la);
		GameObject obj3 = zWQx4nT6Yw<GameObject>(CS_0024_003C_003E8__locals33.xjc8ho38la, "button");
		gRdxEfOPIx(YvUxg3uTsw<ButtonManager>((object)obj3), (Action)delegate
		{
			List<SelectionDefinition> list = new List<SelectionDefinition>();
			foreach (SelectionDefinition item in CS_0024_003C_003E8__locals33.TYG8FGy0KS())
			{
				_003C_003Ec__DisplayClass12_1 CS_0024_003C_003E8__locals42 = new _003C_003Ec__DisplayClass12_1();
				CS_0024_003C_003E8__locals42.reI83xFSYn = CS_0024_003C_003E8__locals33;
				CS_0024_003C_003E8__locals42.HsQ8dHdJPX = item;
				CS_0024_003C_003E8__locals42.Veq8DtRqe8 = CS_0024_003C_003E8__locals42.HsQ8dHdJPX.Label;
				list.Add(new SelectionDefinition
				{
					Label = CS_0024_003C_003E8__locals42.Veq8DtRqe8,
					Icon = CS_0024_003C_003E8__locals42.HsQ8dHdJPX.Icon,
					IconLoader = CS_0024_003C_003E8__locals42.HsQ8dHdJPX.IconLoader,
					OnSelected = delegate
					{
						int num = 5;
						int num2 = num;
						Image val3 = default(Image);
						while (true)
						{
							switch (num2)
							{
							case 6:
								UwBxNv082y(CS_0024_003C_003E8__locals42.reI83xFSYn.Pm58Iafvdy, "RefreshSelectionDesc", CS_0024_003C_003E8__locals42.reI83xFSYn.xjc8ho38la);
								num2 = 2;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
								{
									num2 = 10;
								}
								break;
							case 5:
							{
								Action onSelected = CS_0024_003C_003E8__locals42.HsQ8dHdJPX.OnSelected;
								if (onSelected == null)
								{
									num2 = 0;
									if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eae2aa4db32f466c8ef85fe24af9100e != 0)
									{
										num2 = 4;
									}
								}
								else
								{
									onSelected();
									num2 = 9;
								}
								break;
							}
							case 10:
								return;
							case 3:
							{
								Action<Image> iconLoader = CS_0024_003C_003E8__locals42.HsQ8dHdJPX.IconLoader;
								if (iconLoader == null)
								{
									num2 = 2;
								}
								else
								{
									iconLoader(val3);
									num2 = 7;
								}
								break;
							}
							case 1:
								if ((Object)(object)val3 != (Object)null)
								{
									num2 = 8;
									break;
								}
								goto case 2;
							case 2:
							case 7:
								ELTxbXaVOK(CS_0024_003C_003E8__locals42.reI83xFSYn.xjc8ho38la, "currentLabel", CS_0024_003C_003E8__locals42.Veq8DtRqe8);
								num2 = 5;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3ab0aad8161d42b396e76201b56e6767 != 0)
								{
									num2 = 6;
								}
								break;
							case 4:
							case 9:
								val3 = zWQx4nT6Yw<Image>(CS_0024_003C_003E8__locals42.reI83xFSYn.xjc8ho38la, "icon");
								num2 = 1;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4606931d409d4632bb5c9d4bf6dc4c74 == 0)
								{
									num2 = 1;
								}
								break;
							case 8:
								if ((Object)(object)CS_0024_003C_003E8__locals42.HsQ8dHdJPX.Icon != (Object)null)
								{
									num2 = 0;
									if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
									{
										num2 = 0;
									}
									break;
								}
								goto case 3;
							default:
								val3.sprite = CS_0024_003C_003E8__locals42.HsQ8dHdJPX.Icon;
								num2 = 3;
								break;
							}
						}
					}
				});
			}
			dXbxtIoDyG(zWQx4nT6Yw<TargetSelectionWidget>(CS_0024_003C_003E8__locals33.Pm58Iafvdy, "targetSelectionWidget"), list);
		});
		obj3.SetActive(true);
	}

	internal static mtQn0mxrSFWXEN1SUy2 zWQx4nT6Yw<mtQn0mxrSFWXEN1SUy2>(object P_0, object P_1)
	{
		return (mtQn0mxrSFWXEN1SUy2)(AccessTools.Field(P_0.GetType(), (string)P_1) ?? throw new MissingFieldException(P_0.GetType().FullName, (string?)P_1)).GetValue(P_0);
	}

	internal static void ELTxbXaVOK(object P_0, object P_1, object? value)
	{
		int num = 2;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 2:
			{
				FieldInfo fieldInfo = AccessTools.Field(P_0.GetType(), (string)P_1);
				if ((object)fieldInfo == null)
				{
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b33a46eca689495eb7b574324d630333 == 0)
					{
						num2 = 1;
					}
					break;
				}
				fieldInfo.SetValue(P_0, value);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1f9e3467df7e4ab2b9086d9e74e2921e == 0)
				{
					num2 = 0;
				}
				break;
			}
			case 0:
				return;
			case 1:
				throw new MissingFieldException(P_0.GetType().FullName, (string?)P_1);
			}
		}
	}

	internal static object? UwBxNv082y(object P_0, object P_1, params object?[] args)
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 1:
			{
				MethodInfo methodInfo = AccessTools.Method(P_0.GetType(), (string)P_1, (Type[])null, (Type[])null);
				if ((object)methodInfo == null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_63e4d775e4914aaf953a5ccf1ba12bff != 0)
					{
						num2 = 0;
					}
					break;
				}
				return methodInfo.Invoke(P_0, args);
			}
			default:
				throw new MissingMethodException(P_0.GetType().FullName, (string?)P_1);
			}
		}
	}

	internal static void Lc4xaxVZJ4(object P_0, object P_1, Func<int> P_2)
	{
		_003C_003Ec__DisplayClass16_0 CS_0024_003C_003E8__locals10 = new _003C_003Ec__DisplayClass16_0();
		CS_0024_003C_003E8__locals10.T3U8MPHUM1 = (GenericItemEntry)P_0;
		Button val = zWQx4nT6Yw<Button>(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "button");
		Text val2 = zWQx4nT6Yw<Text>(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "nameText");
		Image val3 = zWQx4nT6Yw<Image>(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "icon");
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null || (Object)(object)val3 == (Object)null)
		{
			return;
		}
		ELTxbXaVOK(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "model", P_1);
		ELTxbXaVOK(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "getQuantity", P_2);
		ELTxbXaVOK(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "_bindVersion", zWQx4nT6Yw<int>(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "_bindVersion") + 1);
		UwBxNv082y(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "RefreshUI");
		gRdxEfOPIx(val, (Action)delegate
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
					UwBxNv082y(CS_0024_003C_003E8__locals10.T3U8MPHUM1, "OnButtonClick");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b33a46eca689495eb7b574324d630333 == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		});
	}

	private static void ikqxHVbHNn(object P_0)
	{
		int num = 13;
		InputField component4 = default(InputField);
		_003C_003Ec__DisplayClass17_0 _003C_003Ec__DisplayClass17_1 = default(_003C_003Ec__DisplayClass17_0);
		InfinityScrollModel infinityScrollModel = default(InfinityScrollModel);
		ScrollRect component2 = default(ScrollRect);
		ButtonManager buttonManager = default(ButtonManager);
		ToggleGroup component5 = default(ToggleGroup);
		Transform val = default(Transform);
		ButtonManager buttonManager2 = default(ButtonManager);
		GameObject gameObject2 = default(GameObject);
		InputField component = default(InputField);
		GameObject gameObject = default(GameObject);
		Transform val2 = default(Transform);
		ToggleGroup component3 = default(ToggleGroup);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				default:
					BDIxlE5nDP(component4, _003C_003Ec__DisplayClass17_1.bae8AyOiuH);
					num2 = 17;
					continue;
				case 32:
					infinityScrollModel = YvUxg3uTsw<InfinityScrollModel>((object)((Component)component2).gameObject);
					num2 = 31;
					continue;
				case 25:
					infinityScrollModel.SpaceX = zWQx4nT6Yw<float>(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "spaceX");
					num2 = 29;
					continue;
				case 5:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "confirmBtn", buttonManager);
					num2 = 8;
					continue;
				case 21:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "subtypeGroup", component5);
					num = 10;
					break;
				case 14:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "typeContainer", val);
					num2 = 7;
					continue;
				case 13:
					_003C_003Ec__DisplayClass17_1 = new _003C_003Ec__DisplayClass17_0();
					num2 = 12;
					continue;
				case 4:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "infinityScroll", infinityScrollModel);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1744e5e231764a669d60af3428ea8412 != 0)
					{
						num2 = 0;
					}
					continue;
				case 19:
					gRdxEfOPIx(buttonManager2, new Action(_003C_003Ec__DisplayClass17_1.AwI8si61yF));
					num2 = 23;
					continue;
				case 31:
					infinityScrollModel.ItemPrefab = gameObject2;
					num = 25;
					break;
				case 28:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "quantityInput", component);
					num2 = 5;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6593ea927925479bb43a4205254370c9 != 0)
					{
						num2 = 5;
					}
					continue;
				case 22:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "searchInput", component4);
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0b6ab60d7d104659bda88e4aca0f9eb0 != 0)
					{
						num2 = 3;
					}
					continue;
				case 1:
					buttonManager2 = YvUxg3uTsw<ButtonManager>((object)((Component)gameObject.transform.Find("Content/Main/Buttons/Cancel")).gameObject);
					num2 = 18;
					continue;
				case 11:
					gameObject.SetActive(false);
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
					{
						num2 = 26;
					}
					continue;
				case 15:
					component = ((Component)gameObject.transform.Find("Content/Main/Input Field")).GetComponent<InputField>();
					num = 27;
					break;
				case 12:
					_003C_003Ec__DisplayClass17_1.hpp85aBktr = (GenericItemPanel)P_0;
					num2 = 2;
					continue;
				case 8:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "cancelBtn", buttonManager2);
					num2 = 11;
					continue;
				case 10:
					YvUxg3uTsw<GenericItemEntry>((object)gameObject2);
					num2 = 24;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6d605cc618ce4b50b73229894686788c != 0)
					{
						num2 = 7;
					}
					continue;
				case 18:
				{
					ButtonManager buttonManager3 = YvUxg3uTsw<ButtonManager>((object)((Component)component).gameObject);
					buttonManager3.maxSize = 20f;
					buttonManager3.heSize = 4f;
					num2 = 6;
					continue;
				}
				case 16:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "scrollRect", component2);
					num2 = 33;
					continue;
				case 9:
					infinityScrollModel.QuantityProvider = _003C_003Ec__DisplayClass17_1.Gf181TkxJR;
					num2 = 4;
					continue;
				case 30:
					component5 = ((Component)val2).GetComponent<ToggleGroup>();
					num2 = 16;
					continue;
				case 33:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "entryPrefab", gameObject2);
					num2 = 22;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f9c2e6931e68482f8459ce8ff3a47b9c == 0)
					{
						num2 = 20;
					}
					continue;
				case 24:
					gameObject2.SetActive(false);
					num2 = 32;
					continue;
				case 6:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "quantityDialog", gameObject);
					num2 = 28;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_374631ddc6c246a694af702fcc63a766 == 0)
					{
						num2 = 22;
					}
					continue;
				case 29:
					infinityScrollModel.SpaceY = zWQx4nT6Yw<float>(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "spaceY");
					num2 = 5;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fb7eb7bc2d2840c29b380d12b6798ec5 != 0)
					{
						num2 = 9;
					}
					continue;
				case 23:
					return;
				case 20:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "subtypeContainer", val2);
					num2 = 21;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_530166a6640e4d928390290fcc4f133b != 0)
					{
						num2 = 21;
					}
					continue;
				case 2:
				{
					Transform transform = ((Component)_003C_003Ec__DisplayClass17_1.hpp85aBktr).transform;
					component2 = ((Component)transform.Find("ScrollView")).GetComponent<ScrollRect>();
					gameObject2 = ((Component)transform.Find("ScrollView/Viewport/EntryPrefab")).gameObject;
					component4 = ((Component)transform.Find("SearchInput")).GetComponent<InputField>();
					val = transform.Find("TypeGroup/Toggles");
					component3 = ((Component)val).GetComponent<ToggleGroup>();
					val2 = transform.Find("SubtypeGroup");
					num = 30;
					break;
				}
				case 27:
					buttonManager = YvUxg3uTsw<ButtonManager>((object)((Component)gameObject.transform.Find("Content/Main/Buttons/Confirm")).gameObject);
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_47c9d108ec114961a58b1bfbfefe6bab != 0)
					{
						num2 = 1;
					}
					continue;
				case 26:
					gRdxEfOPIx(buttonManager, new Action(_003C_003Ec__DisplayClass17_1.PUn87FFJ9K));
					num = 19;
					break;
				case 7:
					ELTxbXaVOK(_003C_003Ec__DisplayClass17_1.hpp85aBktr, "typeGroup", component3);
					num2 = 5;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_34bc60c937894c178241f751744daf7b == 0)
					{
						num2 = 20;
					}
					continue;
				case 17:
					XZuxBum7ko(_003C_003Ec__DisplayClass17_1.hpp85aBktr);
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_823dececcbca4b5a81ec3c316b1230e5 == 0)
					{
						num2 = 3;
					}
					continue;
				case 3:
					gameObject = ((Component)UIService.Instance.CanvasRoot.transform.Find("Popup Item")).gameObject;
					num2 = 7;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a4ff6416da82451bb1a00e06218d633a == 0)
					{
						num2 = 15;
					}
					continue;
				}
				break;
			}
		}
	}

	private static void XZuxBum7ko(object P_0)
	{
		int num = 16;
		Canvas val3 = default(Canvas);
		CanvasGroup val4 = default(CanvasGroup);
		Transform val2 = default(Transform);
		RectTransform value = default(RectTransform);
		Transform val = default(Transform);
		while (true)
		{
			int num2 = num;
			Transform obj3;
			while (true)
			{
				switch (num2)
				{
				case 25:
					val3 = null;
					num2 = 12;
					break;
				default:
					ELTxbXaVOK(P_0, "tooltipCg", val4);
					num2 = 20;
					break;
				case 10:
				case 24:
					if ((Object)(object)val2 != (Object)null)
					{
						num2 = 6;
						break;
					}
					goto case 2;
				case 13:
					return;
				case 16:
					obj3 = UIService.Instance.CanvasRoot.transform.Find("Tooltip");
					if (obj3 != null)
					{
						goto end_IL_0012;
					}
					num2 = 15;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a4ff6416da82451bb1a00e06218d633a == 0)
					{
						num2 = 15;
					}
					break;
				case 18:
					if (!((Object)(object)val3 != (Object)null))
					{
						num2 = 5;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2df26eab4c194a2cabc45ac854ca3536 != 0)
						{
							num2 = 2;
						}
						break;
					}
					goto case 2;
				case 8:
					ELTxbXaVOK(P_0, "rootCanvas", val3);
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1744e5e231764a669d60af3428ea8412 != 0)
					{
						num2 = 2;
					}
					break;
				case 12:
					val2 = ((Component)P_0).transform;
					num2 = 10;
					break;
				case 14:
					val4.alpha = 0f;
					num2 = 23;
					break;
				case 19:
					if (!((Object)(object)val3 == (Object)null))
					{
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e7642139c72f4727920bbb31032e9427 != 0)
						{
							num2 = 1;
						}
						break;
					}
					goto case 4;
				case 6:
				case 11:
					val3 = ((Component)val2).GetComponent<Canvas>();
					num2 = 18;
					break;
				case 4:
					throw new InvalidOperationException("Framework root Canvas was not found.");
				case 1:
					ELTxbXaVOK(P_0, "tooltipRt", value);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f9d9dfa1e4dc433a863e13dcda66c695 == 0)
					{
						num2 = 0;
					}
					break;
				case 26:
					val3 = UIService.Instance.CanvasRoot.GetComponent<Canvas>();
					num2 = 19;
					break;
				case 9:
					val4.blocksRaycasts = false;
					num2 = 13;
					break;
				case 23:
					val4.interactable = false;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_09195e591f3c41aabd3dbe4c54d9aae3 == 0)
					{
						num2 = 9;
					}
					break;
				case 5:
					val2 = val2.parent;
					num2 = 24;
					break;
				case 2:
					if (val3 == null)
					{
						num2 = 26;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_9234c16c5c574c4d809243e604dc1c06 != 0)
						{
							num2 = 2;
						}
						break;
					}
					goto case 19;
				case 15:
					throw new InvalidOperationException("Framework Tooltip was not found.");
				case 7:
				{
					RectTransform component2 = ((Component)val).GetComponent<RectTransform>();
					if (component2 == null)
					{
						num2 = 21;
						break;
					}
					value = component2;
					num2 = 3;
					break;
				}
				case 21:
					throw new InvalidOperationException("Framework Tooltip RectTransform was not found.");
				case 3:
				{
					CanvasGroup component = ((Component)val).GetComponent<CanvasGroup>();
					if (component == null)
					{
						num2 = 22;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0ee41f9c997a423d9a65c078a867a835 == 0)
						{
							num2 = 20;
						}
						break;
					}
					val4 = component;
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a6984c925458472aada338b15f8b243d != 0)
					{
						num2 = 25;
					}
					break;
				}
				case 22:
					throw new InvalidOperationException("Framework Tooltip CanvasGroup was not found.");
				case 20:
				{
					Transform obj2 = val.Find("Title");
					ELTxbXaVOK(P_0, "tooltipTitle", (obj2 != null) ? ((Component)obj2).GetComponent<Text>() : null);
					num2 = 17;
					break;
				}
				case 17:
				{
					Transform obj = val.Find("Text");
					ELTxbXaVOK(P_0, "tooltipText", (obj != null) ? ((Component)obj).GetComponent<Text>() : null);
					num2 = 8;
					break;
				}
				}
				continue;
				end_IL_0012:
				break;
			}
			val = obj3;
			num = 7;
		}
	}

	private static void umqxJ6OibX(object P_0)
	{
		int num = 15;
		_003C_003Ec__DisplayClass19_1 _003C_003Ec__DisplayClass19_3 = default(_003C_003Ec__DisplayClass19_1);
		GameObject gameObject = default(GameObject);
		Transform val2 = default(Transform);
		int num3 = default(int);
		Toggle component = default(Toggle);
		_003C_003Ec__DisplayClass19_0 _003C_003Ec__DisplayClass19_2 = default(_003C_003Ec__DisplayClass19_0);
		Text componentInChildren = default(Text);
		List<FilterDefinition> list = default(List<FilterDefinition>);
		ToggleGroup val = default(ToggleGroup);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 2:
					_003C_003Ec__DisplayClass19_3 = new _003C_003Ec__DisplayClass19_1();
					num2 = 13;
					continue;
				case 5:
				case 11:
					gameObject = ((Component)val2.GetChild(num3)).gameObject;
					num2 = 16;
					continue;
				case 8:
					gameObject.SetActive(false);
					num2 = 20;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a6984c925458472aada338b15f8b243d == 0)
					{
						num2 = 20;
					}
					continue;
				case 16:
					component = gameObject.GetComponent<Toggle>();
					num2 = 9;
					continue;
				case 14:
					_003C_003Ec__DisplayClass19_2.qCA8jmZCXA = (GenericItemPanel)P_0;
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_acf4dbc23cf347efba74119535a62f63 == 0)
					{
						num2 = 18;
					}
					continue;
				case 6:
					componentInChildren.text = list[num3].Name;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c2b372e98ebd4e50b2c43c7d4173610a == 0)
					{
						num2 = 1;
					}
					continue;
				case 17:
					return;
				case 13:
					_003C_003Ec__DisplayClass19_3.vGW8mGykbu = _003C_003Ec__DisplayClass19_2;
					num2 = 23;
					continue;
				default:
					num3++;
					num = 10;
					break;
				case 1:
					_003C_003Ec__DisplayClass19_3.dRd8qOKNec = num3;
					num = 3;
					break;
				case 3:
					InGxc7eTjB(component, _003C_003Ec__DisplayClass19_3.w9B8RShusD);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eaccb5d3e56b494eb5697f390836741b == 0)
					{
						num2 = 0;
					}
					continue;
				case 21:
					if (num3 < list.Count)
					{
						num2 = 2;
						continue;
					}
					goto case 8;
				case 19:
					num3 = 0;
					num2 = 12;
					continue;
				case 9:
					componentInChildren = gameObject.GetComponentInChildren<Text>();
					num2 = 22;
					continue;
				case 4:
					val = zWQx4nT6Yw<ToggleGroup>(_003C_003Ec__DisplayClass19_2.qCA8jmZCXA, "typeGroup");
					num2 = 19;
					continue;
				case 7:
					val2 = zWQx4nT6Yw<Transform>(_003C_003Ec__DisplayClass19_2.qCA8jmZCXA, "typeContainer");
					num2 = 4;
					continue;
				case 15:
					_003C_003Ec__DisplayClass19_2 = new _003C_003Ec__DisplayClass19_0();
					num2 = 11;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed != 0)
					{
						num2 = 14;
					}
					continue;
				case 10:
				case 12:
					if (num3 >= val2.childCount)
					{
						return;
					}
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_623fcc4c4dee4c84b36c5c47e593f1a8 != 0)
					{
						num2 = 11;
					}
					continue;
				case 23:
					gameObject.SetActive(true);
					num2 = 3;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_63e4d775e4914aaf953a5ccf1ba12bff == 0)
					{
						num2 = 6;
					}
					continue;
				case 18:
					list = zWQx4nT6Yw<List<FilterDefinition>>(_003C_003Ec__DisplayClass19_2.qCA8jmZCXA, "filters");
					num2 = 7;
					continue;
				case 22:
					component.group = val;
					num2 = 21;
					continue;
				}
				break;
			}
		}
	}

	private static void WPTxTuSPbQ(object P_0, int P_1)
	{
		int num = 27;
		int childCount = default(int);
		Transform val = default(Transform);
		int num3 = default(int);
		List<SubFilter> subFilters = default(List<SubFilter>);
		Text componentInChildren = default(Text);
		GameObject gameObject = default(GameObject);
		_003C_003Ec__DisplayClass20_0 _003C_003Ec__DisplayClass20_3 = default(_003C_003Ec__DisplayClass20_0);
		Toggle component = default(Toggle);
		_003C_003Ec__DisplayClass20_1 _003C_003Ec__DisplayClass20_2 = default(_003C_003Ec__DisplayClass20_1);
		ToggleGroup val2 = default(ToggleGroup);
		List<FilterDefinition> list = default(List<FilterDefinition>);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 20:
					childCount = val.childCount;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_65008faf8c9e442a9ed1f702ae18c527 == 0)
					{
						num2 = 0;
					}
					continue;
				case 22:
				case 30:
					if (num3 >= val.childCount)
					{
						num2 = 6;
						continue;
					}
					goto case 23;
				case 3:
					if (num3 >= subFilters.Count)
					{
						num2 = 2;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_58e7c04a8f9343c78cfb968536d2f6a4 == 0)
						{
							num2 = 31;
						}
						continue;
					}
					goto case 13;
				case 16:
					return;
				case 1:
					componentInChildren = gameObject.GetComponentInChildren<Text>();
					num2 = 5;
					continue;
				case 26:
					_003C_003Ec__DisplayClass20_3.vNh80Rwpuw = (GenericItemPanel)P_0;
					num2 = 18;
					continue;
				case 9:
					num3 = 0;
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1223010f7ae043a8986881cac090145c != 0)
					{
						num2 = 22;
					}
					continue;
				case 2:
					if (P_1 < 0)
					{
						num2 = 16;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2031ddeadc84d71bc7448bf4b22a7e2 != 0)
						{
							num2 = 8;
						}
						continue;
					}
					goto case 10;
				case 25:
					Wa5xk4KYeK(val, subFilters.Count, childCount);
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_17e5af2a8dde41e2ba1e36fab1d77e2f != 0)
					{
						num2 = 9;
					}
					continue;
				case 21:
					componentInChildren.text = subFilters[num3].Name;
					num2 = 19;
					continue;
				case 14:
					InGxc7eTjB(component, _003C_003Ec__DisplayClass20_2.VuV8yNLA0w);
					num2 = 15;
					continue;
				case 5:
					break;
				case 8:
					val = zWQx4nT6Yw<Transform>(_003C_003Ec__DisplayClass20_3.vNh80Rwpuw, "subtypeContainer");
					num2 = 11;
					continue;
				case 28:
					_003C_003Ec__DisplayClass20_2.XnW8W9spcf = _003C_003Ec__DisplayClass20_3;
					num2 = 12;
					continue;
				case 19:
					_003C_003Ec__DisplayClass20_2.IGj8OtApl2 = num3;
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed == 0)
					{
						num2 = 6;
					}
					continue;
				default:
					iwJxQ0QrgK(val, subFilters.Count);
					num2 = 25;
					continue;
				case 11:
					val2 = zWQx4nT6Yw<ToggleGroup>(_003C_003Ec__DisplayClass20_3.vNh80Rwpuw, "subtypeGroup");
					num2 = 20;
					continue;
				case 23:
					gameObject = ((Component)val.GetChild(num3)).gameObject;
					num2 = 7;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_49e68abb97b9490ca701229335018a23 != 0)
					{
						num2 = 2;
					}
					continue;
				case 10:
					if (P_1 < list.Count)
					{
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1f9e3467df7e4ab2b9086d9e74e2921e == 0)
						{
							num2 = 4;
						}
						continue;
					}
					return;
				case 12:
					gameObject.SetActive(true);
					num2 = 21;
					continue;
				case 17:
					return;
				case 4:
					subFilters = list[P_1].SubFilters;
					num2 = 8;
					continue;
				case 15:
				case 29:
					num3++;
					num2 = 28;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e0ae04f6cf1849cb86b531d7a0262df0 == 0)
					{
						num2 = 30;
					}
					continue;
				case 13:
					_003C_003Ec__DisplayClass20_2 = new _003C_003Ec__DisplayClass20_1();
					num2 = 28;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4606931d409d4632bb5c9d4bf6dc4c74 == 0)
					{
						num2 = 4;
					}
					continue;
				case 18:
					list = zWQx4nT6Yw<List<FilterDefinition>>(_003C_003Ec__DisplayClass20_3.vNh80Rwpuw, "filters");
					num2 = 2;
					continue;
				case 27:
					_003C_003Ec__DisplayClass20_3 = new _003C_003Ec__DisplayClass20_0();
					num2 = 26;
					continue;
				case 7:
					component = gameObject.GetComponent<Toggle>();
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4606931d409d4632bb5c9d4bf6dc4c74 != 0)
					{
						num2 = 1;
					}
					continue;
				case 6:
					return;
				case 24:
				case 31:
					gameObject.SetActive(false);
					num2 = 29;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eae2aa4db32f466c8ef85fe24af9100e == 0)
					{
						num2 = 26;
					}
					continue;
				}
				break;
			}
			component.group = val2;
			num = 3;
		}
	}

	private static void iwJxQ0QrgK(object P_0, int P_1)
	{
		int num = 3;
		int num2 = num;
		GameObject gameObject = default(GameObject);
		while (true)
		{
			switch (num2)
			{
			default:
			{
				GameObject obj = Object.Instantiate<GameObject>(gameObject, (Transform)P_0, false);
				((Object)obj).name = $"{((Object)gameObject).name} {((Transform)P_0).childCount}";
				obj.SetActive(false);
				num2 = 4;
				break;
			}
			case 2:
				if (((Transform)P_0).childCount == 0)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a536dae990ff49e894149992c3d6ff55 != 0)
					{
						num2 = 1;
					}
				}
				else
				{
					gameObject = ((Component)((Transform)P_0).GetChild(((Transform)P_0).childCount - 1)).gameObject;
					num2 = 6;
				}
				break;
			case 1:
				return;
			case 7:
				return;
			case 4:
			case 6:
				if (((Transform)P_0).childCount >= P_1)
				{
					return;
				}
				num2 = 5;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc7b4e1d49b04480ade703081cc93669 != 0)
				{
					num2 = 3;
				}
				break;
			case 3:
				if (P_1 <= ((Transform)P_0).childCount)
				{
					return;
				}
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1c87508dc9684f9ba156e3fa973ff661 == 0)
				{
					num2 = 2;
				}
				break;
			}
		}
	}

	private static void Wa5xk4KYeK(object P_0, int P_1, int P_2)
	{
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a0: Unknown result type (might be due to invalid IL or missing references)
		int num = 28;
		RectTransform component = default(RectTransform);
		int num5 = default(int);
		float num4 = default(float);
		GridLayoutGroup component3 = default(GridLayoutGroup);
		Text componentInChildren = default(Text);
		Vector2 anchoredPosition = default(Vector2);
		float num6 = default(float);
		RectTransform component4 = default(RectTransform);
		RectTransform component2 = default(RectTransform);
		float num3 = default(float);
		Rect rect = default(Rect);
		float num7 = default(float);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				float num8;
				LayoutElement obj;
				switch (num2)
				{
				case 19:
					component = ((Component)((Transform)P_0).GetChild(num5)).GetComponent<RectTransform>();
					num2 = 18;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_df53a978e2fa4caeb588ba7b3d0a9c1a == 0)
					{
						num2 = 0;
					}
					continue;
				case 23:
				{
					if (num4 <= 0f)
					{
						num = 4;
						break;
					}
					HorizontalLayoutGroup component5 = ((Component)P_0).GetComponent<HorizontalLayoutGroup>();
					component3 = ((Component)P_0).GetComponent<GridLayoutGroup>();
					if (component5 == null)
					{
						num2 = 14;
						continue;
					}
					num8 = ((HorizontalOrVerticalLayoutGroup)component5).spacing;
					goto IL_05b4;
				}
				case 38:
					componentInChildren.resizeTextMinSize = 12;
					num2 = 2;
					continue;
				case 9:
				case 39:
					if (num5 >= P_1)
					{
						num2 = 15;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_ebc3d19798664aa299b4944ee75a74f4 == 0)
						{
							num2 = 5;
						}
						continue;
					}
					goto case 19;
				case 7:
					num5 = 0;
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e536e969c0ee481f86575e72192940a2 != 0)
					{
						num2 = 6;
					}
					continue;
				case 3:
					componentInChildren = ((Component)component).GetComponentInChildren<Text>();
					num2 = 17;
					continue;
				case 8:
					component.anchoredPosition = anchoredPosition;
					num2 = 32;
					continue;
				case 35:
					return;
				case 12:
					num6 = component4.anchoredPosition.x - component2.anchoredPosition.x;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_58e7c04a8f9343c78cfb968536d2f6a4 != 0)
					{
						num2 = 0;
					}
					continue;
				case 25:
					if (P_2 > 1)
					{
						num2 = 30;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cefc85d5eea849199c84f01615ccfcdd != 0)
						{
							num2 = 20;
						}
						continue;
					}
					goto case 23;
				case 30:
					num4 = num6 * (float)(P_2 - 1) / (float)(P_1 - 1);
					num2 = 23;
					continue;
				default:
					if (num6 <= 0f)
					{
						num2 = 24;
						continue;
					}
					num4 = num6;
					num = 21;
					break;
				case 14:
					if (component3 == null)
					{
						num2 = 5;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_77a5c7c652c547f8ba0f41ab05d5f737 != 0)
						{
							num2 = 33;
						}
						continue;
					}
					num8 = component3.spacing.x;
					goto IL_05b4;
				case 18:
					if (!((Object)(object)component == (Object)null))
					{
						num = 22;
						break;
					}
					goto case 5;
				case 21:
					if (P_1 > P_2)
					{
						num2 = 25;
						continue;
					}
					goto case 23;
				case 13:
					if ((Object)(object)component2 == (Object)null)
					{
						return;
					}
					num2 = 34;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0a61ecf8779445d9862220ed1608703f != 0)
					{
						num2 = 34;
					}
					continue;
				case 24:
					return;
				case 26:
					component3.cellSize = new Vector2(num3, component3.cellSize.y);
					num2 = 7;
					continue;
				case 1:
					anchoredPosition.x = component2.anchoredPosition.x + num4 * (float)num5;
					num2 = 8;
					continue;
				case 20:
					if (num3 > 0f)
					{
						num2 = 26;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_827cc329172c4139be887b2f1c067875 == 0)
						{
							num2 = 6;
						}
						continue;
					}
					goto case 7;
				case 17:
					if ((Object)(object)componentInChildren == (Object)null)
					{
						num2 = 10;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a03ced0933fd4241877f3560f52a06dc != 0)
						{
							num2 = 36;
						}
						continue;
					}
					goto case 41;
				case 15:
					return;
				case 5:
				case 36:
					num5++;
					num2 = 39;
					continue;
				case 2:
					componentInChildren.resizeTextMaxSize = componentInChildren.fontSize;
					num2 = 5;
					continue;
				case 32:
					if (num3 > 0f)
					{
						num2 = 10;
						continue;
					}
					goto case 3;
				case 40:
					rect = component2.rect;
					num2 = 29;
					continue;
				case 27:
					if (((Transform)P_0).childCount < P_1)
					{
						num2 = 6;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_823dececcbca4b5a81ec3c316b1230e5 != 0)
						{
							num2 = 6;
						}
						continue;
					}
					component2 = ((Component)((Transform)P_0).GetChild(0)).GetComponent<RectTransform>();
					num2 = 31;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_623fcc4c4dee4c84b36c5c47e593f1a8 == 0)
					{
						num2 = 16;
					}
					continue;
				case 33:
					num8 = 0f;
					goto IL_05b4;
				case 34:
					if ((Object)(object)component4 == (Object)null)
					{
						return;
					}
					num2 = 10;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_09195e591f3c41aabd3dbe4c54d9aae3 == 0)
					{
						num2 = 12;
					}
					continue;
				case 31:
					component4 = ((Component)((Transform)P_0).GetChild(1)).GetComponent<RectTransform>();
					num2 = 13;
					continue;
				case 11:
					if ((Object)(object)component3 != (Object)null)
					{
						num2 = 20;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_453f8f74f5a946e1abb183183312f63d == 0)
						{
							num2 = 0;
						}
						continue;
					}
					goto case 7;
				case 6:
					return;
				case 22:
					anchoredPosition = component.anchoredPosition;
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b38508003949434ea7a070c228110021 == 0)
					{
						num2 = 1;
					}
					continue;
				case 10:
					component.sizeDelta = new Vector2(num3, component.sizeDelta.y);
					num2 = 12;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a2f40808d89d4596affd6998f404e4de != 0)
					{
						num2 = 16;
					}
					continue;
				case 29:
					num3 = Mathf.Min(((Rect)(ref rect)).width, num4 - Mathf.Max(num7, 6f));
					num2 = 11;
					continue;
				case 41:
					componentInChildren.resizeTextForBestFit = true;
					num2 = 38;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_feeba9f081ea45948f90703016033e44 != 0)
					{
						num2 = 37;
					}
					continue;
				case 28:
					if (P_1 < 2)
					{
						return;
					}
					num2 = 26;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_17e5af2a8dde41e2ba1e36fab1d77e2f == 0)
					{
						num2 = 27;
					}
					continue;
				case 4:
					return;
				case 16:
					obj = ((Component)component).GetComponent<LayoutElement>();
					if (obj != null)
					{
						goto IL_05fe;
					}
					num2 = 37;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_755ddfd27f0142dfa385431b55a37df0 == 0)
					{
						num2 = 27;
					}
					continue;
				case 37:
					{
						obj = ((Component)component).gameObject.AddComponent<LayoutElement>();
						goto IL_05fe;
					}
					IL_05fe:
					obj.minWidth = num3;
					obj.preferredWidth = num3;
					obj.flexibleWidth = 0f;
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
					{
						num2 = 3;
					}
					continue;
					IL_05b4:
					num7 = num8;
					num2 = 40;
					continue;
				}
				break;
			}
		}
	}

	private static void SeXxY0vPPJ(object P_0, int P_1)
	{
		int num = 14;
		int num2 = num;
		List<FilterDefinition> list = default(List<FilterDefinition>);
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 0:
				return;
			case 9:
				UwBxNv082y(P_0, "RefreshItems");
				num2 = 11;
				break;
			case 10:
				return;
			case 8:
				return;
			case 12:
				if (P_1 < list.Count)
				{
					num2 = 6;
					break;
				}
				return;
			case 13:
				if (list.Count == 0)
				{
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_feeba9f081ea45948f90703016033e44 == 0)
					{
						num2 = 7;
					}
					break;
				}
				if (P_1 < 0)
				{
					num2 = 10;
					break;
				}
				goto case 12;
			case 14:
				list = zWQx4nT6Yw<List<FilterDefinition>>(P_0, "filters");
				num2 = 13;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_623fcc4c4dee4c84b36c5c47e593f1a8 == 0)
				{
					num2 = 0;
				}
				break;
			case 15:
				((Component)zWQx4nT6Yw<Transform>(P_0, "subtypeContainer").GetChild(0)).GetComponent<Toggle>().SetIsOnWithoutNotify(true);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a6984c925458472aada338b15f8b243d != 0)
				{
					num2 = 2;
				}
				break;
			case 3:
				return;
			case 7:
				UwBxNv082y(P_0, "RefreshItems");
				num2 = 1;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2031ddeadc84d71bc7448bf4b22a7e2 == 0)
				{
					num2 = 3;
				}
				break;
			case 5:
				return;
			case 6:
				ELTxbXaVOK(P_0, "selectedTypeIndex", P_1);
				num2 = 4;
				break;
			case 2:
				OPFxZeptyC(P_0, 0);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4068ee93bb84461dbacde7f72600241d != 0)
				{
					num2 = 0;
				}
				break;
			case 1:
				if (list[P_1].SubFilters.Count <= 0)
				{
					num2 = 9;
					break;
				}
				goto case 15;
			case 11:
				zWQx4nT6Yw<InfinityScrollModel>(P_0, "infinityScroll").ResizeContent();
				num2 = 8;
				break;
			case 4:
				WPTxTuSPbQ(P_0, P_1);
				num2 = 1;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eaccb5d3e56b494eb5697f390836741b != 0)
				{
					num2 = 0;
				}
				break;
			}
		}
	}

	private static void OPFxZeptyC(object P_0, int P_1)
	{
		int num = 2;
		int num2 = num;
		int num3 = default(int);
		List<FilterDefinition> list = default(List<FilterDefinition>);
		List<SubFilter> subFilters = default(List<SubFilter>);
		while (true)
		{
			switch (num2)
			{
			case 1:
				num3 = zWQx4nT6Yw<int>(P_0, "selectedTypeIndex");
				num2 = 7;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d6357a19e71a493190300e7ae5779dd7 == 0)
				{
					num2 = 3;
				}
				break;
			case 4:
				UwBxNv082y(P_0, "RefreshItems");
				num2 = 3;
				break;
			case 11:
				if (num3 < list.Count)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0feacf0bf00d4671bdcba77460093fd1 == 0)
					{
						num2 = 0;
					}
					break;
				}
				return;
			case 7:
				if (num3 >= 0)
				{
					num2 = 11;
					break;
				}
				return;
			case 2:
				list = zWQx4nT6Yw<List<FilterDefinition>>(P_0, "filters");
				num2 = 1;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0203eca33da548f7b75f4844de41b607 == 0)
				{
					num2 = 1;
				}
				break;
			case 10:
				return;
			default:
				subFilters = list[num3].SubFilters;
				num2 = 9;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6593ea927925479bb43a4205254370c9 == 0)
				{
					num2 = 1;
				}
				break;
			case 5:
				if (P_1 >= subFilters.Count)
				{
					num2 = 8;
					break;
				}
				ELTxbXaVOK(P_0, "selectedSubIndex", P_1);
				num2 = 4;
				break;
			case 8:
				return;
			case 6:
				return;
			case 9:
				if (P_1 < 0)
				{
					return;
				}
				num2 = 5;
				break;
			case 3:
				zWQx4nT6Yw<InfinityScrollModel>(P_0, "infinityScroll").ResizeContent();
				num2 = 6;
				break;
			}
		}
	}

	private static void zLaxV4dsG3(object P_0)
	{
		int num = 8;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 3:
				throw new InvalidOperationException("TargetSelectionWidget animator was not found.");
			case 6:
				try
				{
					AnimationClip val = ((IEnumerable<AnimationClip>)((TargetSelectionWidget)P_0).widgetAnimator.runtimeAnimatorController.animationClips).FirstOrDefault((AnimationClip candidate) => ((Object)candidate).name == "WidgetPreset_In");
					int num3 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_9e88f6cab0b744b8a2f883c333109175 != 0)
					{
						num3 = 1;
					}
					while (true)
					{
						switch (num3)
						{
						case 2:
							((TargetSelectionWidget)P_0).animationDuration = val.length + 0.05f;
							num3 = 0;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_988f5d1238d043129fbd30a9c933ca73 != 0)
							{
								num3 = 0;
							}
							continue;
						case 1:
							if ((Object)(object)val != (Object)null)
							{
								num3 = 2;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b38508003949434ea7a070c228110021 != 0)
								{
									num3 = 0;
								}
								continue;
							}
							break;
						case 0:
							break;
						}
						break;
					}
				}
				catch
				{
					int num4 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0203eca33da548f7b75f4844de41b607 != 0)
					{
						num4 = 0;
					}
					switch (num4)
					{
					case 0:
						break;
					}
				}
				goto case 5;
			case 8:
				if (!((Object)(object)((TargetSelectionWidget)P_0).widgetAnimator == (Object)null))
				{
					num2 = 3;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_09195e591f3c41aabd3dbe4c54d9aae3 == 0)
					{
						num2 = 7;
					}
					break;
				}
				goto case 9;
			default:
				num2 = (((Object)(object)((TargetSelectionWidget)P_0).widgetAnimator == (Object)null) ? 3 : 6);
				break;
			case 4:
				return;
			case 9:
				((TargetSelectionWidget)P_0).widgetAnimator = ((Component)P_0).GetComponentInChildren<Animator>();
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_63e4d775e4914aaf953a5ccf1ba12bff == 0)
				{
					num2 = 0;
				}
				break;
			case 5:
			{
				Transform obj2 = ((Component)P_0).transform.Find("Quick Note");
				if (obj2 == null)
				{
					num2 = 2;
					break;
				}
				YvUxg3uTsw<WindowDragger>((object)((Component)obj2).gameObject);
				Transform val2 = obj2.Find("Content/Group List/List") ?? throw new InvalidOperationException("TargetSelectionWidget entry container was not found.");
				GameObject gameObject = ((Component)(val2.Find("Button Element") ?? throw new InvalidOperationException("TargetSelectionWidget entry template was not found."))).gameObject;
				gameObject.SetActive(false);
				Transform obj3 = obj2.Find("Content/Search");
				InputField val3 = ((obj3 != null) ? ((Component)obj3).GetComponent<InputField>() : null) ?? throw new InvalidOperationException("TargetSelectionWidget search input was not found.");
				ButtonManager buttonManager = YvUxg3uTsw<ButtonManager>((object)((Component)val3).gameObject);
				buttonManager.speed = 3f;
				buttonManager.maxSize = 20f;
				ELTxbXaVOK(P_0, "entryContainer", val2);
				ELTxbXaVOK(P_0, "entryPrefab", gameObject);
				ELTxbXaVOK(P_0, "searchInput", val3);
				Transform obj4 = obj2.Find("Close");
				if (obj4 == null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c2b372e98ebd4e50b2c43c7d4173610a == 0)
					{
						num2 = 1;
					}
				}
				else
				{
					ButtonManager buttonManager2 = YvUxg3uTsw<ButtonManager>((object)((Component)obj4).gameObject);
					buttonManager2.speed = 3.5f;
					buttonManager2.maxSize = 2.5f;
					buttonManager2.heSize = 1f;
					gRdxEfOPIx(buttonManager2, new Action(((TargetSelectionWidget)P_0).Close));
					num2 = 4;
				}
				break;
			}
			case 2:
				throw new InvalidOperationException("TargetSelectionWidget Quick Note root was not found.");
			case 1:
				throw new InvalidOperationException("TargetSelectionWidget close button was not found.");
			}
		}
	}

	internal static void FQKxuE1ITT(object P_0)
	{
		zLaxV4dsG3(P_0);
	}

	private static void WkPxpRLvUn()
	{
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		if (typeof(Mathf).GetMethod("MoveTowards", BindingFlags.Static | BindingFlags.Public, null, new Type[3]
		{
			typeof(float),
			typeof(float),
			typeof(float)
		}, null) != null)
		{
			return;
		}
		foreach (ButtonManager componentsInChild in UIService.Instance.CanvasRoot.GetComponentsInChildren<ButtonManager>(true))
		{
			ELTxbXaVOK(componentsInChild, "useHoverEffect", false);
		}
		foreach (HoverEffect componentsInChild2 in UIService.Instance.CanvasRoot.GetComponentsInChildren<HoverEffect>(true))
		{
			((MonoBehaviour)componentsInChild2).StopAllCoroutines();
			ELTxbXaVOK(componentsInChild2, "transitionAlpha", 0f);
			Image val = zWQx4nT6Yw<Image>(componentsInChild2, "targetImage");
			if (!((Object)(object)val == (Object)null))
			{
				Color color = ((Graphic)val).color;
				color.a = 0f;
				((Graphic)val).color = color;
			}
		}
	}

	private static void dXbxtIoDyG(object P_0, List<SelectionDefinition> P_1)
	{
		_003C_003Ec__DisplayClass28_0 CS_0024_003C_003E8__locals9 = new _003C_003Ec__DisplayClass28_0();
		CS_0024_003C_003E8__locals9.Y728eZMxXR = (TargetSelectionWidget)P_0;
		ELTxbXaVOK(CS_0024_003C_003E8__locals9.Y728eZMxXR, "allDefs", P_1);
		((MonoBehaviour)CS_0024_003C_003E8__locals9.Y728eZMxXR).StopAllCoroutines();
		((Component)CS_0024_003C_003E8__locals9.Y728eZMxXR).gameObject.SetActive(true);
		Animator obj = zWQx4nT6Yw<Animator>(CS_0024_003C_003E8__locals9.Y728eZMxXR, "widgetAnimator");
		((Behaviour)obj).enabled = true;
		obj.Play("In");
		if (UwBxNv082y(CS_0024_003C_003E8__locals9.Y728eZMxXR, "DisableAnimatorAfterDelay") is IEnumerator routine)
		{
			RuntimeHelper.StartCoroutine(routine);
		}
		InputField obj2 = zWQx4nT6Yw<InputField>(CS_0024_003C_003E8__locals9.Y728eZMxXR, "searchInput");
		xAaxGKjV6l(obj2);
		obj2.text = string.Empty;
		BDIxlE5nDP(obj2, delegate
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
					jtqxvtF6R2(CS_0024_003C_003E8__locals9.Y728eZMxXR);
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cb955e4f34e14b2c8322a115d2a10521 == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		});
		jtqxvtF6R2(CS_0024_003C_003E8__locals9.Y728eZMxXR);
	}

	private static void jtqxvtF6R2(object P_0)
	{
		int num = 16;
		_003C_003Ec__DisplayClass29_1 _003C_003Ec__DisplayClass29_3 = default(_003C_003Ec__DisplayClass29_1);
		_003C_003Ec__DisplayClass29_0 _003C_003Ec__DisplayClass29_2 = default(_003C_003Ec__DisplayClass29_0);
		GameObject val6 = default(GameObject);
		Image val5 = default(Image);
		Text componentInChildren = default(Text);
		List<GameObject> list = default(List<GameObject>);
		GameObject val2 = default(GameObject);
		GameObject val3 = default(GameObject);
		int num3 = default(int);
		Transform val4 = default(Transform);
		List<GameObject> list3 = default(List<GameObject>);
		List<SelectionDefinition> list2 = default(List<SelectionDefinition>);
		InputField val = default(InputField);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				object obj;
				switch (num2)
				{
				case 3:
					_003C_003Ec__DisplayClass29_3.IhX89Dct5i = _003C_003Ec__DisplayClass29_2;
					num = 6;
					break;
				case 30:
				{
					Transform obj2 = val6.transform.Find("Icon");
					if (obj2 == null)
					{
						num2 = 2;
						continue;
					}
					obj = ((Component)obj2).GetComponent<Image>();
					goto IL_0624;
				}
				case 25:
					if ((Object)(object)val5 != (Object)null)
					{
						num2 = 35;
						continue;
					}
					goto case 42;
				default:
					componentInChildren.text = _003C_003Ec__DisplayClass29_3.sFF8iFVmHi.Label;
					num2 = 18;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_181262d62ff8478d83f1373ca953b4ab != 0)
					{
						num2 = 30;
					}
					continue;
				case 16:
					_003C_003Ec__DisplayClass29_2 = new _003C_003Ec__DisplayClass29_0();
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a0969090f0a142399256f0ee456da969 != 0)
					{
						num2 = 15;
					}
					continue;
				case 26:
					val6.SetActive(false);
					num2 = 17;
					continue;
				case 42:
				case 43:
					if ((Object)(object)val5 != (Object)null)
					{
						num2 = 24;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d6357a19e71a493190300e7ae5779dd7 == 0)
						{
							num2 = 19;
						}
						continue;
					}
					goto case 10;
				case 7:
					list.Add(val2);
					num2 = 4;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_55507d5fc4c5488b8a2cda3cb3f0b63b != 0)
					{
						num2 = 34;
					}
					continue;
				case 19:
					val3 = zWQx4nT6Yw<GameObject>(_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8, "entryPrefab");
					num2 = 27;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1b4fe3e0fc2c40fc86f25cd1934dae42 == 0)
					{
						num2 = 10;
					}
					continue;
				case 20:
					val6.transform.SetSiblingIndex(num3);
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
					{
						num2 = 21;
					}
					continue;
				case 4:
					val5.sprite = _003C_003Ec__DisplayClass29_3.sFF8iFVmHi.Icon;
					num = 43;
					break;
				case 1:
					val2 = Object.Instantiate<GameObject>(val3, val4, false);
					num2 = 22;
					continue;
				case 40:
					if ((Object)(object)componentInChildren != (Object)null)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6593ea927925479bb43a4205254370c9 == 0)
						{
							num2 = 0;
						}
						continue;
					}
					goto case 30;
				case 9:
					val6.transform.SetParent(val4, false);
					num2 = 20;
					continue;
				case 29:
					list3 = zWQx4nT6Yw<List<GameObject>>(_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8, "activeEntries");
					num2 = 19;
					continue;
				case 8:
					if (num3 >= list2.Count)
					{
						num = 26;
						break;
					}
					goto case 39;
				case 35:
					if (!((Object)(object)_003C_003Ec__DisplayClass29_3.sFF8iFVmHi.Icon != (Object)null))
					{
						num2 = 42;
						continue;
					}
					goto case 4;
				case 17:
				case 32:
					num3++;
					num2 = 14;
					continue;
				case 41:
					num3 = 0;
					num2 = 28;
					continue;
				case 18:
					componentInChildren = val6.GetComponentInChildren<Text>();
					num2 = 40;
					continue;
				case 5:
					_003C_003Ec__DisplayClass29_3 = new _003C_003Ec__DisplayClass29_1();
					num2 = 3;
					continue;
				case 39:
					_003C_003Ec__DisplayClass29_3.sFF8iFVmHi = list2[num3];
					num2 = 31;
					continue;
				case 6:
					val6 = list[num3];
					num2 = 8;
					continue;
				case 10:
				case 12:
					gRdxEfOPIx(YvUxg3uTsw<ButtonManager>((object)val6), new Action(_003C_003Ec__DisplayClass29_3.k7l8SREKap));
					num2 = 9;
					continue;
				case 23:
					list = zWQx4nT6Yw<List<GameObject>>(_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8, "entryPool");
					num = 29;
					break;
				case 27:
					val4 = zWQx4nT6Yw<Transform>(_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8, "entryContainer");
					num2 = 36;
					continue;
				case 14:
				case 28:
					if (num3 >= list.Count)
					{
						num2 = 37;
						continue;
					}
					goto case 5;
				case 38:
					list3.Clear();
					num2 = 41;
					continue;
				case 24:
				{
					Action<Image> iconLoader = _003C_003Ec__DisplayClass29_3.sFF8iFVmHi.IconLoader;
					if (iconLoader == null)
					{
						num2 = 12;
						continue;
					}
					iconLoader(val5);
					num = 10;
					break;
				}
				case 21:
					list3.Add(val6);
					num2 = 32;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_8c5a1355f678414f9d81c8e8dee12b23 == 0)
					{
						num2 = 32;
					}
					continue;
				case 11:
					list2 = zWQx4nT6Yw<List<SelectionDefinition>>(_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8, "allDefs").Where(_003C_003Ec__DisplayClass29_2.vWu8CjV4WC).ToList();
					num2 = 19;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
					{
						num2 = 23;
					}
					continue;
				case 22:
				{
					ButtonManager buttonManager = YvUxg3uTsw<ButtonManager>((object)val2);
					buttonManager.speed = 5f;
					buttonManager.maxSize = 20f;
					buttonManager.heSpeed = 15f;
					buttonManager.heSize = 10f;
					num2 = 3;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b0a518c9db76478e920a48eb8abfc0e9 == 0)
					{
						num2 = 7;
					}
					continue;
				}
				case 33:
					val = zWQx4nT6Yw<InputField>(_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8, "searchInput");
					num = 13;
					break;
				case 15:
					_003C_003Ec__DisplayClass29_2.R4t8Pbb8h8 = (TargetSelectionWidget)P_0;
					num2 = 33;
					continue;
				case 37:
					return;
				case 34:
				case 36:
					if (list.Count >= list2.Count)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0c4064135efd4a7fad9efde6262b118d == 0)
						{
							num2 = 38;
						}
						continue;
					}
					goto case 1;
				case 31:
					val6.SetActive(true);
					num2 = 18;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_453f8f74f5a946e1abb183183312f63d == 0)
					{
						num2 = 2;
					}
					continue;
				case 13:
					_003C_003Ec__DisplayClass29_2.DhZ8KtgP2c = val.text ?? string.Empty;
					num2 = 11;
					continue;
				case 2:
					{
						obj = null;
						goto IL_0624;
					}
					IL_0624:
					val5 = (Image)obj;
					num2 = 25;
					continue;
				}
				break;
			}
		}
	}

	private static void InGxc7eTjB(object P_0, Action<bool> P_1)
	{
		int instanceID = ((Object)P_0).GetInstanceID();
		if (kM8LfcjDsB.TryGetValue(instanceID, out UnityAction<bool> value))
		{
			((UnityEvent<bool>)(object)((Toggle)P_0).onValueChanged).RemoveListener(value);
		}
		UnityAction<bool> val = UnityAction<bool>.op_Implicit(P_1);
		kM8LfcjDsB[instanceID] = val;
		((UnityEvent<bool>)(object)((Toggle)P_0).onValueChanged).AddListener(val);
	}

	private static void BDIxlE5nDP(object P_0, Action<string> P_1)
	{
		xAaxGKjV6l(P_0);
		int instanceID = ((Object)P_0).GetInstanceID();
		UnityAction<string> val = UnityAction<string>.op_Implicit(P_1);
		CY8LFjElpL[instanceID] = val;
		((UnityEvent<string>)(object)((InputField)P_0).onValueChanged).AddListener(val);
	}

	private static void xAaxGKjV6l(object P_0)
	{
		int num = 3;
		int num2 = num;
		int instanceID = default(int);
		UnityAction<string> value = default(UnityAction<string>);
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 2:
				if (CY8LFjElpL.TryGetValue(instanceID, out value))
				{
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2af9e2b953064803b9db93f92d6e9d4e != 0)
					{
						num2 = 0;
					}
					break;
				}
				return;
			case 1:
				((UnityEvent<string>)(object)((InputField)P_0).onValueChanged).RemoveListener(value);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4950e2973d2441438c6b262a624ae1e4 == 0)
				{
					num2 = 0;
				}
				break;
			case 3:
				instanceID = ((Object)P_0).GetInstanceID();
				num2 = 2;
				break;
			case 0:
				return;
			}
		}
	}

	private static void gRdxEfOPIx(object P_0, object P_1)
	{
		int num = 4;
		int num2 = num;
		_003C_003Ec__DisplayClass33_0 _003C_003Ec__DisplayClass33_1 = default(_003C_003Ec__DisplayClass33_0);
		Button val = default(Button);
		ButtonManager buttonManager = default(ButtonManager);
		while (true)
		{
			switch (num2)
			{
			case 2:
				cW0LhHNIji[_003C_003Ec__DisplayClass33_1.civ8UyBx1H] = (Action)P_1;
				num2 = 10;
				break;
			case 3:
				_003C_003Ec__DisplayClass33_1.civ8UyBx1H = ((Object)P_0).GetInstanceID();
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cd1492aa96ff4ba5a032458f454e6a9d == 0)
				{
					num2 = 0;
				}
				break;
			case 10:
				if (!O4kLIRR76H.Add(_003C_003Ec__DisplayClass33_1.civ8UyBx1H))
				{
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a536dae990ff49e894149992c3d6ff55 == 0)
					{
						num2 = 1;
					}
					break;
				}
				val = (Button)((P_0 is Button) ? P_0 : null);
				num2 = 5;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
				{
					num2 = 8;
				}
				break;
			case 8:
				if (val != null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1f9e3467df7e4ab2b9086d9e74e2921e == 0)
					{
						num2 = 0;
					}
				}
				else
				{
					buttonManager = P_0 as ButtonManager;
					num2 = 7;
				}
				break;
			default:
				((UnityEvent)val.onClick).AddListener(UnityAction.op_Implicit((Action)_003C_003Ec__DisplayClass33_1.rGm82UcORu));
				num2 = 5;
				break;
			case 7:
				if (buttonManager != null)
				{
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0b6ab60d7d104659bda88e4aca0f9eb0 != 0)
					{
						num2 = 1;
					}
					break;
				}
				return;
			case 5:
				return;
			case 9:
				buttonManager.onClick.AddListener(UnityAction.op_Implicit((Action)_003C_003Ec__DisplayClass33_1.rGm82UcORu));
				num2 = 6;
				break;
			case 6:
				return;
			case 4:
				_003C_003Ec__DisplayClass33_1 = new _003C_003Ec__DisplayClass33_0();
				num2 = 3;
				break;
			case 1:
				return;
			}
		}
	}

	private static obCMsnxzLIiEwI55Opb YvUxg3uTsw<obCMsnxzLIiEwI55Opb>(object P_0) where obCMsnxzLIiEwI55Opb : Component
	{
		obCMsnxzLIiEwI55Opb val = ((GameObject)P_0).GetComponent<obCMsnxzLIiEwI55Opb>();
		if (val == null)
		{
			val = ((GameObject)P_0).AddComponent<obCMsnxzLIiEwI55Opb>();
		}
		if (QcML6i0UQy() && val is ButtonManager buttonManager)
		{
			ELTxbXaVOK(buttonManager, "useHoverEffect", false);
		}
		return val;
	}

	static sfumh2xPLltR4pL0i9k()
	{
		int num = 3;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 2:
				kM8LfcjDsB = new Dictionary<int, UnityAction<bool>>();
				num2 = 4;
				break;
			default:
				O4kLIRR76H = new HashSet<int>();
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_823dececcbca4b5a81ec3c316b1230e5 == 0)
				{
					num2 = 1;
				}
				break;
			case 4:
				CY8LFjElpL = new Dictionary<int, UnityAction<string>>();
				num2 = 5;
				break;
			case 5:
				cW0LhHNIji = new Dictionary<int, Action>();
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d326bf98637481998cf110a56c1622c == 0)
				{
					num2 = 0;
				}
				break;
			case 1:
				return;
			case 3:
				bpND7PhQOXpROODtSab.XR4RtoBqtq();
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a96443d82a894ce2b983c6f341506ec2 == 0)
				{
					num2 = 1;
				}
				break;
			}
		}
	}

	internal static bool LGTKNdAutDFgiTPF2M0()
	{
		return iljI6cAVw2Mw59CYFoN == null;
	}

	internal static sfumh2xPLltR4pL0i9k P11is6ApJ5gvnPfghFF()
	{
		return iljI6cAVw2Mw59CYFoN;
	}
}
