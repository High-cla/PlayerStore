using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Il2CppSystem.Collections.Generic;
using ModFramework.Utilities;
using TyOQ7hhkasLPlhFR3an;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Dc80t9LqgVxgkUAmr3Z;

internal static class r9QFjeLRJlKUX1YROOX
{
	private sealed class FLRHXu8XguVwGc8SZwG : IEquatable<FLRHXu8XguVwGc8SZwG>
	{
		[CompilerGenerated]
		private readonly Dictionary<string, string> PKg8YrL91t;

		[CompilerGenerated]
		private readonly Dictionary<string, string> AcW8ZH6NHR;

		private static FLRHXu8XguVwGc8SZwG rms8MMswbiXhaQiPKnk;

		[CompilerGenerated]
		private Type lVH8aZAWgH
		{
			[CompilerGenerated]
			get
			{
				return typeof(FLRHXu8XguVwGc8SZwG);
			}
		}

		public FLRHXu8XguVwGc8SZwG(Dictionary<string, string> P_0, Dictionary<string, string> P_1)
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			PKg8YrL91t = P_0;
			AcW8ZH6NHR = P_1;
			base._002Ector();
		}

		[SpecialName]
		[CompilerGenerated]
		public Dictionary<string, string> Epi8HMCQnl()
		{
			return PKg8YrL91t;
		}

		[SpecialName]
		[CompilerGenerated]
		public void R1U8B0yYw0(Dictionary<string, string> P_0)
		{
			PKg8YrL91t = P_0;
		}

		[SpecialName]
		[CompilerGenerated]
		public Dictionary<string, string> YgI8TjLf7V()
		{
			return AcW8ZH6NHR;
		}

		[SpecialName]
		[CompilerGenerated]
		public void Fmp8QLMlR2(Dictionary<string, string> P_0)
		{
			AcW8ZH6NHR = P_0;
		}

		[CompilerGenerated]
		public override string ToString()
		{
			int num = 6;
			StringBuilder stringBuilder = default(StringBuilder);
			while (true)
			{
				int num2 = num;
				while (true)
				{
					switch (num2)
					{
					default:
						if (nK184pOhZQ(stringBuilder))
						{
							num2 = 3;
							continue;
						}
						goto case 1;
					case 1:
						stringBuilder.Append('}');
						num2 = 4;
						continue;
					case 3:
						stringBuilder.Append(' ');
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc7b4e1d49b04480ade703081cc93669 != 0)
						{
							num2 = 0;
						}
						continue;
					case 6:
						stringBuilder = new StringBuilder();
						num2 = 5;
						continue;
					case 2:
						stringBuilder.Append(" { ");
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_79caba32e8744079a41a72bf027c7236 == 0)
						{
							num2 = 0;
						}
						continue;
					case 4:
						return stringBuilder.ToString();
					case 5:
						break;
					}
					break;
				}
				stringBuilder.Append("OverrideIndex");
				num = 2;
			}
		}

		[CompilerGenerated]
		private bool nK184pOhZQ(StringBuilder P_0)
		{
			int num = 1;
			while (true)
			{
				int num2 = num;
				while (true)
				{
					switch (num2)
					{
					case 1:
						RuntimeHelpers.EnsureSufficientExecutionStack();
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cd1492aa96ff4ba5a032458f454e6a9d != 0)
						{
							num2 = 0;
						}
						continue;
					case 3:
						return true;
					case 2:
						P_0.Append(", Descriptions = ");
						num = 5;
						break;
					default:
						P_0.Append("Names = ");
						num2 = 4;
						continue;
					case 5:
						P_0.Append(YgI8TjLf7V());
						num = 3;
						break;
					case 4:
						P_0.Append(Epi8HMCQnl());
						num2 = 2;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc28089b23154bd0af6dea698a7995ab == 0)
						{
							num2 = 2;
						}
						continue;
					}
					break;
				}
			}
		}

		[CompilerGenerated]
		public static bool operator !=(object? P_0, object? P_1)
		{
			return !(P_0 == P_1);
		}

		[CompilerGenerated]
		public static bool operator ==(object? P_0, object? P_1)
		{
			int num = 1;
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 1:
					if (P_0 != P_1)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_827cc329172c4139be887b2f1c067875 != 0)
						{
							num2 = 0;
						}
						break;
					}
					return true;
				default:
					if (P_0 == null)
					{
						num2 = 3;
						break;
					}
					goto case 2;
				case 2:
					return ((FLRHXu8XguVwGc8SZwG)P_0).Equals((FLRHXu8XguVwGc8SZwG?)P_1);
				case 3:
					return false;
				}
			}
		}

		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<Type>.Default.GetHashCode(lVH8aZAWgH) * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(PKg8YrL91t)) * -1521134295 + EqualityComparer<Dictionary<string, string>>.Default.GetHashCode(AcW8ZH6NHR);
		}

		[CompilerGenerated]
		public override bool Equals(object? P_0)
		{
			return Equals(P_0 as FLRHXu8XguVwGc8SZwG);
		}

		[CompilerGenerated]
		public bool Equals(FLRHXu8XguVwGc8SZwG? P_0)
		{
			int num = 2;
			while (true)
			{
				int num2 = num;
				while (true)
				{
					switch (num2)
					{
					case 4:
						if (EqualityComparer<Dictionary<string, string>>.Default.Equals(PKg8YrL91t, P_0.PKg8YrL91t))
						{
							num2 = 0;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3192da3ead8846b09a16ca828ae45a1b != 0)
							{
								num2 = 0;
							}
							continue;
						}
						goto IL_00b6;
					case 3:
						if (lVH8aZAWgH == P_0.lVH8aZAWgH)
						{
							num2 = 2;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0c883aef9e214bdbbe2f931c98600d64 != 0)
							{
								num2 = 4;
							}
							continue;
						}
						goto IL_00b6;
					default:
						return EqualityComparer<Dictionary<string, string>>.Default.Equals(AcW8ZH6NHR, P_0.AcW8ZH6NHR);
					case 2:
						if ((object)this == (object)P_0)
						{
							return true;
						}
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a2f40808d89d4596affd6998f404e4de == 0)
						{
							num2 = 1;
						}
						continue;
					case 1:
						{
							if ((object)P_0 != null)
							{
								break;
							}
							goto IL_00b6;
						}
						IL_00b6:
						return false;
					}
					break;
				}
				num = 3;
			}
		}

		[CompilerGenerated]
		public FLRHXu8XguVwGc8SZwG WuF8rbVjuf()
		{
			return new FLRHXu8XguVwGc8SZwG(this);
		}

		[CompilerGenerated]
		private FLRHXu8XguVwGc8SZwG(FLRHXu8XguVwGc8SZwG P_0)
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 2;
			while (true)
			{
				switch (num)
				{
				default:
					return;
				case 2:
					PKg8YrL91t = P_0.PKg8YrL91t;
					num = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_ebc3d19798664aa299b4944ee75a74f4 == 0)
					{
						num = 1;
					}
					break;
				case 1:
					AcW8ZH6NHR = P_0.AcW8ZH6NHR;
					num = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_9499cd174e734581806278a135f9219f == 0)
					{
						num = 0;
					}
					break;
				case 0:
					return;
				}
			}
		}

		[CompilerGenerated]
		public void HD38bGDrXJ(out Dictionary<string, string> P_0, out Dictionary<string, string> P_1)
		{
			int num = 1;
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 1:
					P_0 = Epi8HMCQnl();
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_94b27b4eaf334086b7ba5cdf39a841c4 != 0)
					{
						num2 = 0;
					}
					break;
				default:
					P_1 = YgI8TjLf7V();
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc28089b23154bd0af6dea698a7995ab != 0)
					{
						num2 = 2;
					}
					break;
				case 2:
					return;
				}
			}
		}

		internal static bool su0T1gsd2JQPBCsWWQ2()
		{
			return (object)rms8MMswbiXhaQiPKnk == null;
		}

		internal static FLRHXu8XguVwGc8SZwG jLVQDusDnAjwDdSUWXx()
		{
			return rms8MMswbiXhaQiPKnk;
		}
	}

	private sealed class aGeR9Q8VhIsIRoWY6Up
	{
		[CompilerGenerated]
		private List<DYNojQ8cPdLrYPuAUeT> th68vtPN0A;

		internal static aGeR9Q8VhIsIRoWY6Up piB89xs3geM0i8JKJOK;

		[JsonPropertyName("entries")]
		public List<DYNojQ8cPdLrYPuAUeT> DMm8tgwH8J
		{
			[CompilerGenerated]
			get
			{
				return th68vtPN0A;
			}
			[CompilerGenerated]
			set
			{
				th68vtPN0A = list;
			}
		}

		public aGeR9Q8VhIsIRoWY6Up()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			th68vtPN0A = new List<DYNojQ8cPdLrYPuAUeT>();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0c4064135efd4a7fad9efde6262b118d != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal static bool j9qZInsnkdB86WrMbBO()
		{
			return piB89xs3geM0i8JKJOK == null;
		}

		internal static aGeR9Q8VhIsIRoWY6Up cWlhHPsMo8hVDfLquh4()
		{
			return piB89xs3geM0i8JKJOK;
		}
	}

	private sealed class DYNojQ8cPdLrYPuAUeT
	{
		[CompilerGenerated]
		private string yK2f8UEdAR;

		[CompilerGenerated]
		private string c9Zff4HDTS;

		[CompilerGenerated]
		private string MvGfF4B1Eo;

		internal static DYNojQ8cPdLrYPuAUeT UuY8YIs1ZVGnUC4LILU;

		[JsonPropertyName("stableId")]
		public string JKv8EQZEL1
		{
			[CompilerGenerated]
			get
			{
				return yK2f8UEdAR;
			}
			[CompilerGenerated]
			set
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
						yK2f8UEdAR = text;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3ab534b5bdcf432398304cc36639cf61 == 0)
						{
							num2 = 0;
						}
						break;
					}
				}
			}
		}

		[JsonPropertyName("locale")]
		public string pN3f6I7GBT
		{
			[CompilerGenerated]
			get
			{
				return c9Zff4HDTS;
			}
			[CompilerGenerated]
			set
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
						c9Zff4HDTS = text;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_08a11342c71f4153a4564b4e92bd109c != 0)
						{
							num2 = 0;
						}
						break;
					case 0:
						return;
					}
				}
			}
		}

		[JsonPropertyName("text")]
		public string Text
		{
			[CompilerGenerated]
			get
			{
				return MvGfF4B1Eo;
			}
			[CompilerGenerated]
			set
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
						MvGfF4B1Eo = mvGfF4B1Eo;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4877fc232b62465299156662d37e1227 != 0)
						{
							num2 = 0;
						}
						break;
					case 0:
						return;
					}
				}
			}
		}

		public DYNojQ8cPdLrYPuAUeT()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			yK2f8UEdAR = string.Empty;
			c9Zff4HDTS = string.Empty;
			MvGfF4B1Eo = string.Empty;
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_7446198b542046fba39ec4bee70c579e == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal static bool KL6DqBsA5LgDwWgRUy4()
		{
			return UuY8YIs1ZVGnUC4LILU == null;
		}

		internal static DYNojQ8cPdLrYPuAUeT trBUwss7QQnftpQbDWA()
		{
			return UuY8YIs1ZVGnUC4LILU;
		}
	}

	private sealed class JRdAqNfhEDrY2BZI7wh
	{
		[CompilerGenerated]
		private List<sets5pf3tl2fU2xT4Cs> itCfD2xv8a;

		private static JRdAqNfhEDrY2BZI7wh MLU9SSssasBxlTbLPqX;

		[JsonPropertyName("entries")]
		public List<sets5pf3tl2fU2xT4Cs> oXtfdOv4by
		{
			[CompilerGenerated]
			get
			{
				return itCfD2xv8a;
			}
			[CompilerGenerated]
			set
			{
				itCfD2xv8a = list;
			}
		}

		public JRdAqNfhEDrY2BZI7wh()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			itCfD2xv8a = new List<sets5pf3tl2fU2xT4Cs>();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eae2aa4db32f466c8ef85fe24af9100e == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal static bool MPBa3os5bW6I1K38L82()
		{
			return MLU9SSssasBxlTbLPqX == null;
		}

		internal static JRdAqNfhEDrY2BZI7wh XX6AE2sjeQksNgfJfAJ()
		{
			return MLU9SSssasBxlTbLPqX;
		}
	}

	private sealed class sets5pf3tl2fU2xT4Cs
	{
		[CompilerGenerated]
		private string f1af03CIWE;

		[CompilerGenerated]
		private string QZsfyHKDt9;

		[CompilerGenerated]
		private string qqKfOvlyFA;

		[CompilerGenerated]
		private string K7wfWvRB9O;

		internal static sets5pf3tl2fU2xT4Cs XUuojUsRJRIc7RavELv;

		[JsonPropertyName("stableId")]
		public string twtf1hunPp
		{
			[CompilerGenerated]
			get
			{
				return f1af03CIWE;
			}
			[CompilerGenerated]
			set
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
						f1af03CIWE = text;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed == 0)
						{
							num2 = 0;
						}
						break;
					}
				}
			}
		}

		[JsonPropertyName("locale")]
		public string qfKfswo0Il
		{
			[CompilerGenerated]
			get
			{
				return QZsfyHKDt9;
			}
			[CompilerGenerated]
			set
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
						QZsfyHKDt9 = qZsfyHKDt;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_5259db2402bd4af18b00f3f78d623a6d == 0)
						{
							num2 = 0;
						}
						break;
					}
				}
			}
		}

		[JsonPropertyName("field")]
		public string xm7fR46JEx
		{
			[CompilerGenerated]
			get
			{
				return qqKfOvlyFA;
			}
			[CompilerGenerated]
			set
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
						qqKfOvlyFA = text;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_9e88f6cab0b744b8a2f883c333109175 != 0)
						{
							num2 = 0;
						}
						break;
					case 0:
						return;
					}
				}
			}
		}

		[JsonPropertyName("text")]
		public string Text
		{
			[CompilerGenerated]
			get
			{
				return K7wfWvRB9O;
			}
			[CompilerGenerated]
			set
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
						K7wfWvRB9O = k7wfWvRB9O;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_94b27b4eaf334086b7ba5cdf39a841c4 == 0)
						{
							num2 = 0;
						}
						break;
					}
				}
			}
		}

		public sets5pf3tl2fU2xT4Cs()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			f1af03CIWE = string.Empty;
			QZsfyHKDt9 = string.Empty;
			qqKfOvlyFA = string.Empty;
			K7wfWvRB9O = string.Empty;
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_27869b3cfc9541a994ff2c4f4b3d6928 == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal static bool wwh3NQsq1Aoo5F1bNoq()
		{
			return XUuojUsRJRIc7RavELv == null;
		}

		internal static sets5pf3tl2fU2xT4Cs Ngrrh6smOaac5tTDcJi()
		{
			return XUuojUsRJRIc7RavELv;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass10_0<T> where T : notnull
	{
		public string xKhfXqVPe5;

		private static object XyROZ1sWOj1Q6GRhplS;

		public _003C_003Ec__DisplayClass10_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0b6ab60d7d104659bda88e4aca0f9eb0 != 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal bool MG1fUVfSQN(string name)
		{
			return name.EndsWith(xKhfXqVPe5, StringComparison.Ordinal);
		}

		internal static bool lwfh0PsooJgBFX1PbLK()
		{
			return XyROZ1sWOj1Q6GRhplS == null;
		}

		internal static object BK2AM6se1osGYEUBPXZ()
		{
			return XyROZ1sWOj1Q6GRhplS;
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass4_0
	{
		public GameItem kZGfBKXrKF;

		private static _003C_003Ec__DisplayClass4_0 VaABHAsCJKm8Hd0qkyC;

		public _003C_003Ec__DisplayClass4_0()
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 0;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 == 0)
			{
				num = 0;
			}
			switch (num)
			{
			case 0:
				break;
			}
		}

		internal string? C6Zf43yILr()
		{
			return kZGfBKXrKF.shortDescription;
		}

		internal void vQxfrsVgsq(string value)
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
					kZGfBKXrKF.shortDescription = value;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_49e68abb97b9490ca701229335018a23 == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal string? Qjkfb3n42J()
		{
			return kZGfBKXrKF.longDescription;
		}

		internal void Lw7fNUMOu1(string value)
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
					kZGfBKXrKF.longDescription = value;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_df53a978e2fa4caeb588ba7b3d0a9c1a != 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal string? qftfaWUAse()
		{
			return kZGfBKXrKF.flavorText;
		}

		internal void cwFfHrDwrr(string value)
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
					kZGfBKXrKF.flavorText = value;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_af8707d145dd484ca1f60c770c340369 == 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		internal static bool laMaJesKwMfS04IcrTp()
		{
			return VaABHAsCJKm8Hd0qkyC == null;
		}

		internal static _003C_003Ec__DisplayClass4_0 QFNAmLsPqaDL5gVMW2Y()
		{
			return VaABHAsCJKm8Hd0qkyC;
		}
	}

	private static readonly Lazy<FLRHXu8XguVwGc8SZwG> NINLPydRhS;

	internal static r9QFjeLRJlKUX1YROOX RESgRk7ILMPHfIDNALD;

	internal static int Q93LmMNTQG(object P_0, object P_1)
	{
		int num = 10;
		FLRHXu8XguVwGc8SZwG value = default(FLRHXu8XguVwGc8SZwG);
		string text = default(string);
		string value2 = default(string);
		int num3 = default(int);
		_003C_003Ec__DisplayClass4_0 _003C_003Ec__DisplayClass4_1 = default(_003C_003Ec__DisplayClass4_0);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 3:
					if (!value.Epi8HMCQnl().TryGetValue(duWLKSGDFG((string)P_1, text), out value2))
					{
						num2 = 8;
						break;
					}
					goto case 7;
				case 5:
					num3 += cx8LOWV0Ht(value, P_1, text, "flavorText", _003C_003Ec__DisplayClass4_1.qftfaWUAse, _003C_003Ec__DisplayClass4_1.cwFfHrDwrr);
					num2 = 6;
					break;
				case 1:
					num3 += cx8LOWV0Ht(value, P_1, text, "longDescription", _003C_003Ec__DisplayClass4_1.Qjkfb3n42J, _003C_003Ec__DisplayClass4_1.Lw7fNUMOu1);
					num2 = 5;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4456f4be186044e0b1a4dc9bfe657743 != 0)
					{
						num2 = 2;
					}
					break;
				case 17:
					if (text != null)
					{
						num2 = 18;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc28089b23154bd0af6dea698a7995ab == 0)
						{
							num2 = 12;
						}
						break;
					}
					goto default;
				case 11:
					if (string.IsNullOrWhiteSpace((string?)P_1))
					{
						num2 = 2;
						break;
					}
					text = UV1LWHkUtn();
					num2 = 17;
					break;
				case 12:
					if (S2dLy9wGKQ(_003C_003Ec__DisplayClass4_1.kZGfBKXrKF.name))
					{
						goto end_IL_0012;
					}
					goto case 8;
				case 16:
					if (_003C_003Ec__DisplayClass4_1.kZGfBKXrKF == null)
					{
						num2 = 14;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_09195e591f3c41aabd3dbe4c54d9aae3 != 0)
						{
							num2 = 3;
						}
						break;
					}
					goto case 11;
				case 6:
					return num3;
				case 4:
					value = NINLPydRhS.Value;
					num2 = 12;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_27869b3cfc9541a994ff2c4f4b3d6928 != 0)
					{
						num2 = 1;
					}
					break;
				case 7:
					_003C_003Ec__DisplayClass4_1.kZGfBKXrKF.name = value2;
					num2 = 15;
					break;
				default:
					return 0;
				case 18:
					num3 = 0;
					num2 = 4;
					break;
				case 9:
					_003C_003Ec__DisplayClass4_1.kZGfBKXrKF = (GameItem)P_0;
					num2 = 16;
					break;
				case 8:
				case 13:
					num3 += cx8LOWV0Ht(value, P_1, text, "shortDescription", _003C_003Ec__DisplayClass4_1.C6Zf43yILr, _003C_003Ec__DisplayClass4_1.vQxfrsVgsq);
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4950e2973d2441438c6b262a624ae1e4 == 0)
					{
						num2 = 1;
					}
					break;
				case 2:
				case 14:
					return 0;
				case 10:
					_003C_003Ec__DisplayClass4_1 = new _003C_003Ec__DisplayClass4_0();
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_edb7ade2ad6d44ebba67986c6e31a8e3 != 0)
					{
						num2 = 9;
					}
					break;
				case 15:
					num3++;
					num2 = 3;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4456f4be186044e0b1a4dc9bfe657743 == 0)
					{
						num2 = 13;
					}
					break;
				}
				continue;
				end_IL_0012:
				break;
			}
			num = 3;
		}
	}

	internal static int SxUL0w4Jcq(object? P_0)
	{
		int num = 5;
		GameItem current = default(GameItem);
		int num3 = default(int);
		Enumerator<GameItem> enumerator = default(Enumerator<GameItem>);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				object obj;
				switch (num2)
				{
				case 1:
					if (string.IsNullOrWhiteSpace(current.identifier))
					{
						num = 7;
						break;
					}
					goto case 14;
				case 10:
					return num3;
				case 3:
				case 12:
					current = enumerator.Current;
					num = 8;
					break;
				case 2:
					enumerator = ((GameInventory)P_0).childItems.GetEnumerator();
					num2 = 9;
					continue;
				case 5:
					if (P_0 != null)
					{
						num = 4;
						break;
					}
					goto case 6;
				case 7:
				case 9:
				case 13:
					if (enumerator.MoveNext())
					{
						num = 3;
						break;
					}
					goto case 10;
				default:
					return 0;
				case 11:
					num3 = 0;
					num2 = 2;
					continue;
				case 8:
					if (current != null)
					{
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
						{
							num2 = 1;
						}
						continue;
					}
					goto case 7;
				case 6:
					obj = null;
					goto IL_01c3;
				case 14:
					num3 += Q93LmMNTQG(current, current.identifier);
					num2 = 13;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_453f8f74f5a946e1abb183183312f63d == 0)
					{
						num2 = 0;
					}
					continue;
				case 4:
					{
						obj = ((GameInventory)P_0).childItems;
						goto IL_01c3;
					}
					IL_01c3:
					if (obj != null)
					{
						num2 = 11;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f9c2e6931e68482f8459ce8ff3a47b9c == 0)
						{
							num2 = 3;
						}
						continue;
					}
					goto default;
				}
				break;
			}
		}
	}

	internal static bool S2dLy9wGKQ(object? P_0)
	{
		int num = 2;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 2:
				if (string.IsNullOrWhiteSpace((string?)P_0))
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_df53a978e2fa4caeb588ba7b3d0a9c1a != 0)
					{
						num2 = 1;
					}
					break;
				}
				goto default;
			default:
				return ((string)P_0).IndexOf("Translation Error", StringComparison.OrdinalIgnoreCase) >= 0;
			case 1:
				return true;
			}
		}
	}

	private static int cx8LOWV0Ht(object P_0, object P_1, object P_2, object P_3, Func<string?> read, Action<string> P_5)
	{
		if (!S2dLy9wGKQ(read()))
		{
			return 0;
		}
		if (!((FLRHXu8XguVwGc8SZwG)P_0).YgI8TjLf7V().TryGetValue(duWLKSGDFG((string)P_1, (string)P_2, (string)P_3), out string value))
		{
			return 0;
		}
		P_5(value);
		return 1;
	}

	private static string? UV1LWHkUtn()
	{
		string result = default(string);
		switch (1)
		{
		case 1:
			try
			{
				Locale selectedLocale = LocalizationSettings.SelectedLocale;
				int num;
				if (selectedLocale == null)
				{
					num = 9;
					goto IL_003f;
				}
				object obj = selectedLocale.Identifier.Code;
				goto IL_01ab;
				IL_01ab:
				string text = (string)obj;
				num = 8;
				goto IL_003f;
				IL_003f:
				while (true)
				{
					switch (num)
					{
					default:
						goto end_IL_003f;
					case 6:
						goto end_IL_003f;
					case 3:
						result = "en";
						num = 2;
						continue;
					case 4:
						result = null;
						num = 5;
						continue;
					case 12:
						if (!text.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
						{
							num = 1;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e536e969c0ee481f86575e72192940a2 != 0)
							{
								num = 0;
							}
							continue;
						}
						goto case 10;
					case 8:
						if (string.IsNullOrWhiteSpace(text))
						{
							num = 4;
							continue;
						}
						goto case 12;
					case 1:
					case 11:
						if (text.StartsWith("en", StringComparison.OrdinalIgnoreCase))
						{
							num = 3;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b33a46eca689495eb7b574324d630333 != 0)
							{
								num = 3;
							}
							continue;
						}
						goto case 7;
					case 2:
						goto end_IL_003f;
					case 5:
						goto end_IL_003f;
					case 7:
						result = null;
						num = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_09195e591f3c41aabd3dbe4c54d9aae3 != 0)
						{
							num = 0;
						}
						continue;
					case 10:
						result = "zh-CN";
						num = 6;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2031ddeadc84d71bc7448bf4b22a7e2 != 0)
						{
							num = 0;
						}
						continue;
					case 9:
						obj = null;
						break;
					case 0:
						goto end_IL_003f;
					}
					goto IL_01ab;
					continue;
					end_IL_003f:
					break;
				}
			}
			catch (Exception ex)
			{
				int num2 = 2;
				while (true)
				{
					switch (num2)
					{
					case 2:
						ModLogger.Warning("[ProbablyStolenPlaytest] Game locale is unavailable; item text repair skipped: " + ex.Message);
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0ee41f9c997a423d9a65c078a867a835 == 0)
						{
							num2 = 1;
						}
						continue;
					case 1:
						result = null;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a4ff6416da82451bb1a00e06218d633a == 0)
						{
							num2 = 0;
						}
						continue;
					case 0:
						break;
					}
					break;
				}
			}
			break;
		}
		return result;
	}

	private static FLRHXu8XguVwGc8SZwG R6gLoVNUip()
	{
		aGeR9Q8VhIsIRoWY6Up obj = dXxLeGHQiJ<aGeR9Q8VhIsIRoWY6Up>(".Resources.localization-overrides.json");
		return new FLRHXu8XguVwGc8SZwG(dXxLeGHQiJ<JRdAqNfhEDrY2BZI7wh>(".Resources.description-overrides.json").oXtfdOv4by.ToDictionary<sets5pf3tl2fU2xT4Cs, string, string>((sets5pf3tl2fU2xT4Cs entry) => duWLKSGDFG(entry.twtf1hunPp, entry.qfKfswo0Il, entry.xm7fR46JEx), (sets5pf3tl2fU2xT4Cs entry) => entry.Text, StringComparer.OrdinalIgnoreCase), obj.DMm8tgwH8J.ToDictionary<DYNojQ8cPdLrYPuAUeT, string, string>((DYNojQ8cPdLrYPuAUeT entry) => duWLKSGDFG(entry.JKv8EQZEL1, entry.pN3f6I7GBT), (DYNojQ8cPdLrYPuAUeT entry) => entry.Text, StringComparer.OrdinalIgnoreCase));
	}

	private static CJRIm3LCfApZDGTGpBt dXxLeGHQiJ<CJRIm3LCfApZDGTGpBt>(object P_0)
	{
		_003C_003Ec__DisplayClass10_0<CJRIm3LCfApZDGTGpBt> CS_0024_003C_003E8__locals2 = new _003C_003Ec__DisplayClass10_0<CJRIm3LCfApZDGTGpBt>();
		CS_0024_003C_003E8__locals2.xKhfXqVPe5 = (string)P_0;
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text = executingAssembly.GetManifestResourceNames().Single((string name) => name.EndsWith(CS_0024_003C_003E8__locals2.xKhfXqVPe5, StringComparison.Ordinal));
		using Stream utf8Json = executingAssembly.GetManifestResourceStream(text) ?? throw new InvalidOperationException("Embedded override not found: " + text);
		CJRIm3LCfApZDGTGpBt val = JsonSerializer.Deserialize<CJRIm3LCfApZDGTGpBt>(utf8Json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});
		if (val == null)
		{
			throw new InvalidDataException("Embedded override is empty: " + text);
		}
		return val;
	}

	private static string duWLKSGDFG(params string[] parts)
	{
		return string.Join("\u001f", parts);
	}

	static r9QFjeLRJlKUX1YROOX()
	{
		int num = 2;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 0:
				return;
			case 2:
				bpND7PhQOXpROODtSab.XR4RtoBqtq();
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c30761e1724845948fdd25fb5a79280d != 0)
				{
					num2 = 1;
				}
				break;
			case 1:
				NINLPydRhS = new Lazy<FLRHXu8XguVwGc8SZwG>(R6gLoVNUip);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_faf158e93f2347898dabc53af2c2e79a != 0)
				{
					num2 = 0;
				}
				break;
			}
		}
	}

	internal static bool ilukO67w2YmgaViH56h()
	{
		return RESgRk7ILMPHfIDNALD == null;
	}

	internal static r9QFjeLRJlKUX1YROOX NJBQJq7d7nk7FnmPeCN()
	{
		return RESgRk7ILMPHfIDNALD;
	}
}
