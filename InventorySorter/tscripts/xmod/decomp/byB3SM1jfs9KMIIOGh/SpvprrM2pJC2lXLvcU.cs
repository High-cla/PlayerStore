using System;
using System.Runtime.CompilerServices;
using ModFramework.Utilities;

namespace byB3SM1jfs9KMIIOGh;

internal static class SpvprrM2pJC2lXLvcU
{
	internal static SpvprrM2pJC2lXLvcU T5X7hKAUa1UqMvV5K12;

	internal static int rsmApljV9()
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 1:
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cefc85d5eea849199c84f01615ccfcdd != 0)
					{
						num2 = 0;
					}
					break;
				}
				return instance.GetCash();
			}
			default:
				return 0;
			}
		}
	}

	internal static int POh7VoZWk()
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 1:
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_dcfa316cbf1043838ee9b641776e7534 == 0)
					{
						num2 = 0;
					}
					break;
				}
				return instance.wildFavor;
			}
			default:
				return 0;
			}
		}
	}

	internal static int QSgsmJ4kR()
	{
		int num = 1;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 1:
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1b4fe3e0fc2c40fc86f25cd1934dae42 != 0)
					{
						num2 = 0;
					}
					break;
				}
				return instance.GetCurrentStoreAttractiveness();
			}
			default:
				return 0;
			}
		}
	}

	internal static bool Y375Hm9pk(int P_0)
	{
		int num = 17;
		PlayerStore instance = default(PlayerStore);
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = default(DefaultInterpolatedStringHandler);
		int cash2 = default(int);
		int cash = default(int);
		int num3 = default(int);
		bool result = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 16:
					if (instance != null)
					{
						num2 = 34;
						continue;
					}
					goto case 21;
				case 31:
					defaultInterpolatedStringHandler.AppendFormatted(cash2);
					num2 = 22;
					continue;
				case 23:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddCredits: ");
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d6357a19e71a493190300e7ae5779dd7 != 0)
					{
						num2 = 5;
					}
					continue;
				case 4:
					return false;
				case 28:
					return false;
				default:
					cash = instance.GetCash();
					num2 = 29;
					continue;
				case 34:
					if (P_0 > 0)
					{
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fb7eb7bc2d2840c29b380d12b6798ec5 != 0)
						{
							num2 = 0;
						}
						continue;
					}
					goto case 21;
				case 15:
					defaultInterpolatedStringHandler.AppendLiteral(", actual=");
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3b318032ebfe44c08efd8f5b1d4cba9e == 0)
					{
						num2 = 0;
					}
					continue;
				case 33:
					ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 17;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_edb7ade2ad6d44ebba67986c6e31a8e3 != 0)
					{
						num2 = 28;
					}
					continue;
				case 30:
					ModLogger.Error(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 4;
					continue;
				case 18:
					instance.ModCash(P_0, true);
					num = 20;
					break;
				case 32:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddCredits rejected: amount=");
					num2 = 27;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a96443d82a894ce2b983c6f341506ec2 != 0)
					{
						num2 = 27;
					}
					continue;
				case 10:
					defaultInterpolatedStringHandler.AppendFormatted(num3);
					num2 = 15;
					continue;
				case 5:
					defaultInterpolatedStringHandler.AppendFormatted(cash);
					num2 = 11;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b38508003949434ea7a070c228110021 == 0)
					{
						num2 = 13;
					}
					continue;
				case 21:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 2);
					num2 = 32;
					continue;
				case 24:
					return true;
				case 2:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 3);
					num2 = 25;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cd1492aa96ff4ba5a032458f454e6a9d == 0)
					{
						num2 = 16;
					}
					continue;
				case 19:
					if (cash2 == num3)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 3);
						num = 23;
						break;
					}
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e536e969c0ee481f86575e72192940a2 == 0)
					{
						num2 = 2;
					}
					continue;
				case 25:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddCredits verification failed: before=");
					num2 = 7;
					continue;
				case 12:
					ModLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 19;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d51c61ea1cd54949ab81b08153329f54 == 0)
					{
						num2 = 24;
					}
					continue;
				case 22:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 12;
					continue;
				case 1:
					defaultInterpolatedStringHandler.AppendFormatted(cash2);
					num = 3;
					break;
				case 6:
					defaultInterpolatedStringHandler.AppendLiteral(", storeReady=");
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1744e5e231764a669d60af3428ea8412 != 0)
					{
						num2 = 7;
					}
					continue;
				case 20:
					cash2 = instance.GetCash();
					num2 = 19;
					continue;
				case 17:
					instance = PlayerStore.Instance;
					num2 = 12;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_49e68abb97b9490ca701229335018a23 == 0)
					{
						num2 = 16;
					}
					continue;
				case 8:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 33;
					continue;
				case 3:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 30;
					continue;
				case 26:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c30761e1724845948fdd25fb5a79280d != 0)
					{
						num2 = 11;
					}
					continue;
				case 7:
					defaultInterpolatedStringHandler.AppendFormatted(cash);
					num = 14;
					break;
				case 29:
					try
					{
						num3 = checked(cash + P_0);
						int num4 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_7446198b542046fba39ec4bee70c579e == 0)
						{
							num4 = 0;
						}
						switch (num4)
						{
						case 0:
							break;
						}
					}
					catch (OverflowException)
					{
						int num5 = 2;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a96443d82a894ce2b983c6f341506ec2 != 0)
						{
							num5 = 3;
						}
						while (true)
						{
							switch (num5)
							{
							case 4:
								return result;
							default:
								defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddCredits rejected: ");
								num5 = 1;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_63e4d775e4914aaf953a5ccf1ba12bff != 0)
								{
									num5 = 1;
								}
								break;
							case 8:
								ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
								num5 = 5;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d6357a19e71a493190300e7ae5779dd7 == 0)
								{
									num5 = 3;
								}
								break;
							case 7:
								defaultInterpolatedStringHandler.AppendLiteral(" overflows Int32.");
								num5 = 8;
								break;
							case 6:
								defaultInterpolatedStringHandler.AppendLiteral(" + ");
								num5 = 2;
								break;
							case 5:
								result = false;
								num5 = 0;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eae2aa4db32f466c8ef85fe24af9100e != 0)
								{
									num5 = 4;
								}
								break;
							case 2:
								defaultInterpolatedStringHandler.AppendFormatted(P_0);
								num5 = 7;
								break;
							case 3:
								defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 2);
								num5 = 0;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e7642139c72f4727920bbb31032e9427 != 0)
								{
									num5 = 0;
								}
								break;
							case 1:
								defaultInterpolatedStringHandler.AppendFormatted(cash);
								num5 = 1;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0b6ab60d7d104659bda88e4aca0f9eb0 == 0)
								{
									num5 = 6;
								}
								break;
							}
						}
					}
					goto case 18;
				case 11:
					defaultInterpolatedStringHandler.AppendLiteral(" = ");
					num2 = 31;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0a61ecf8779445d9862220ed1608703f == 0)
					{
						num2 = 25;
					}
					continue;
				case 27:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num = 6;
					break;
				case 13:
					defaultInterpolatedStringHandler.AppendLiteral(" + ");
					num2 = 26;
					continue;
				case 14:
					defaultInterpolatedStringHandler.AppendLiteral(", expected=");
					num2 = 10;
					continue;
				case 9:
					defaultInterpolatedStringHandler.AppendFormatted(instance != null);
					num2 = 8;
					continue;
				}
				break;
			}
		}
	}

	internal static bool V1jj3CJjx(int P_0)
	{
		int num = 3;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = default(DefaultInterpolatedStringHandler);
		int num3 = default(int);
		PlayerStore instance = default(PlayerStore);
		int wildFavor = default(int);
		bool result = default(bool);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 19:
					ModLogger.Error(defaultInterpolatedStringHandler.ToStringAndClear());
					num = 18;
					break;
				case 26:
					defaultInterpolatedStringHandler.AppendLiteral(" + ");
					num2 = 20;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1223010f7ae043a8986881cac090145c == 0)
					{
						num2 = 0;
					}
					continue;
				case 18:
					return false;
				case 25:
					return true;
				case 30:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 5;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_49e68abb97b9490ca701229335018a23 == 0)
					{
						num2 = 19;
					}
					continue;
				case 4:
					defaultInterpolatedStringHandler.AppendFormatted(num3);
					num2 = 22;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_453f8f74f5a946e1abb183183312f63d == 0)
					{
						num2 = 10;
					}
					continue;
				case 29:
					defaultInterpolatedStringHandler.AppendFormatted(num3);
					num2 = 5;
					continue;
				case 20:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 2;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b0a518c9db76478e920a48eb8abfc0e9 == 0)
					{
						num2 = 9;
					}
					continue;
				case 24:
					ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 32;
					continue;
				case 28:
				{
					WildUIManager instance2 = WildUIManager.Instance;
					if (instance2 == null)
					{
						num2 = 27;
						continue;
					}
					instance2.Update();
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6593ea927925479bb43a4205254370c9 == 0)
					{
						num2 = 1;
					}
					continue;
				}
				case 12:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddWildFavor rejected: amount=");
					num2 = 7;
					continue;
				case 23:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 7;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_32d9de0be9df48bb91fd60adb55c440d == 0)
					{
						num2 = 24;
					}
					continue;
				case 11:
					defaultInterpolatedStringHandler.AppendFormatted(instance.wildFavor);
					num2 = 30;
					continue;
				case 8:
					defaultInterpolatedStringHandler.AppendLiteral(", storeReady=");
					num2 = 16;
					continue;
				case 16:
					defaultInterpolatedStringHandler.AppendFormatted(instance != null);
					num2 = 4;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_49e68abb97b9490ca701229335018a23 == 0)
					{
						num2 = 23;
					}
					continue;
				case 31:
					try
					{
						num3 = checked(wildFavor + P_0);
						int num4 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2df26eab4c194a2cabc45ac854ca3536 == 0)
						{
							num4 = 0;
						}
						switch (num4)
						{
						case 0:
							break;
						}
					}
					catch (OverflowException)
					{
						int num5 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1b4fe3e0fc2c40fc86f25cd1934dae42 == 0)
						{
							num5 = 0;
						}
						while (true)
						{
							switch (num5)
							{
							case 3:
								return result;
							case 1:
								defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddWildFavor rejected: ");
								num5 = 7;
								break;
							case 4:
								result = false;
								num5 = 3;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d2b65e391f054ae69f637edad2078a14 != 0)
								{
									num5 = 1;
								}
								break;
							case 6:
								defaultInterpolatedStringHandler.AppendFormatted(P_0);
								num5 = 0;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d51c61ea1cd54949ab81b08153329f54 == 0)
								{
									num5 = 2;
								}
								break;
							case 8:
								defaultInterpolatedStringHandler.AppendLiteral(" + ");
								num5 = 6;
								break;
							case 7:
								defaultInterpolatedStringHandler.AppendFormatted(wildFavor);
								num5 = 4;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_58e7c04a8f9343c78cfb968536d2f6a4 == 0)
								{
									num5 = 8;
								}
								break;
							case 2:
								defaultInterpolatedStringHandler.AppendLiteral(" overflows Int32.");
								num5 = 4;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eaccb5d3e56b494eb5697f390836741b == 0)
								{
									num5 = 5;
								}
								break;
							case 5:
								ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
								num5 = 3;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6d605cc618ce4b50b73229894686788c == 0)
								{
									num5 = 4;
								}
								break;
							default:
								defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 2);
								num5 = 1;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d51c61ea1cd54949ab81b08153329f54 != 0)
								{
									num5 = 1;
								}
								break;
							}
						}
					}
					goto case 21;
				case 3:
					instance = PlayerStore.Instance;
					num2 = 2;
					continue;
				case 2:
					if (instance != null)
					{
						num = 14;
						break;
					}
					goto case 15;
				case 7:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 8;
					continue;
				case 9:
					defaultInterpolatedStringHandler.AppendLiteral(" = ");
					num2 = 4;
					continue;
				case 21:
					instance.wildFavor = num3;
					num2 = 28;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_27869b3cfc9541a994ff2c4f4b3d6928 != 0)
					{
						num2 = 11;
					}
					continue;
				case 15:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(69, 2);
					num2 = 12;
					continue;
				case 6:
				case 27:
					if (instance.wildFavor == num3)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 3);
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_63e4d775e4914aaf953a5ccf1ba12bff != 0)
						{
							num2 = 0;
						}
					}
					else
					{
						num2 = 13;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1d4e356f77774f8ea98b8014d1ac452f == 0)
						{
							num2 = 3;
						}
					}
					continue;
				case 1:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddWildFavor: ");
					num2 = 10;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_eaccb5d3e56b494eb5697f390836741b == 0)
					{
						num2 = 10;
					}
					continue;
				case 5:
					defaultInterpolatedStringHandler.AppendLiteral(", actual=");
					num2 = 10;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a03ced0933fd4241877f3560f52a06dc != 0)
					{
						num2 = 11;
					}
					continue;
				case 10:
					defaultInterpolatedStringHandler.AppendFormatted(wildFavor);
					num2 = 26;
					continue;
				case 32:
					return false;
				default:
					ModLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_530166a6640e4d928390290fcc4f133b == 0)
					{
						num2 = 25;
					}
					continue;
				case 13:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(78, 2);
					num2 = 4;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e7642139c72f4727920bbb31032e9427 == 0)
					{
						num2 = 17;
					}
					continue;
				case 14:
					if (P_0 > 0)
					{
						wildFavor = instance.wildFavor;
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_7446198b542046fba39ec4bee70c579e == 0)
						{
							num2 = 31;
						}
					}
					else
					{
						num2 = 15;
					}
					continue;
				case 22:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cb955e4f34e14b2c8322a115d2a10521 == 0)
					{
						num2 = 0;
					}
					continue;
				case 17:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] AddWildFavor verification failed: expected=");
					num = 29;
					break;
				}
				break;
			}
		}
	}

	internal static bool l5LRyZ3nY(int P_0)
	{
		int num = 46;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = default(DefaultInterpolatedStringHandler);
		int currentStoreAttractiveness = default(int);
		PlayerStore instance = default(PlayerStore);
		int baseStoreAttractiveness = default(int);
		int projectorAttractivenessBonus = default(int);
		float num3 = default(float);
		int num4 = default(int);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				float num5;
				switch (num2)
				{
				case 12:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] SetStoreAttractiveness rejected: garbage penalty ");
					num2 = 41;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0b6ab60d7d104659bda88e4aca0f9eb0 != 0)
					{
						num2 = 30;
					}
					continue;
				case 20:
					defaultInterpolatedStringHandler.AppendLiteral(" has no increasing solution.");
					num2 = 51;
					continue;
				case 27:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 18;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_edb7ade2ad6d44ebba67986c6e31a8e3 == 0)
					{
						num2 = 9;
					}
					continue;
				case 39:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] SetStoreAttractiveness: target=");
					num2 = 33;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f9c2e6931e68482f8459ce8ff3a47b9c == 0)
					{
						num2 = 11;
					}
					continue;
				case 52:
					ModLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 44;
					continue;
				case 44:
					return true;
				case 47:
					defaultInterpolatedStringHandler.AppendFormatted(currentStoreAttractiveness);
					num2 = 50;
					continue;
				case 6:
					ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 26;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e812b35a98764d43b77f4c9af4c260f0 != 0)
					{
						num2 = 20;
					}
					continue;
				case 22:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 34;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_988f5d1238d043129fbd30a9c933ca73 == 0)
					{
						num2 = 20;
					}
					continue;
				case 31:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(113, 3);
					num2 = 36;
					continue;
				case 14:
					instance.baseStoreAttractiveness = baseStoreAttractiveness;
					num2 = 31;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_9234c16c5c574c4d809243e604dc1c06 != 0)
					{
						num2 = 15;
					}
					continue;
				case 17:
					defaultInterpolatedStringHandler.AppendLiteral(", bonus=");
					num2 = 21;
					continue;
				case 48:
					currentStoreAttractiveness = instance.GetCurrentStoreAttractiveness();
					num2 = 15;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a89830e59cbd47ec9619f969bf63b41f != 0)
					{
						num2 = 4;
					}
					continue;
				case 28:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 52;
					continue;
				case 29:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(79, 2);
					num2 = 24;
					continue;
				case 15:
					if (currentStoreAttractiveness != P_0)
					{
						num2 = 14;
						continue;
					}
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(82, 4);
					num2 = 38;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_f86bab81e9874f01a080bbd11feacb4c != 0)
					{
						num2 = 39;
					}
					continue;
				case 33:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 25;
					continue;
				case 16:
					defaultInterpolatedStringHandler.AppendLiteral(", penalty=");
					num2 = 38;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1744e5e231764a669d60af3428ea8412 == 0)
					{
						num2 = 40;
					}
					continue;
				case 50:
					defaultInterpolatedStringHandler.AppendLiteral(", oldBase=");
					num2 = 35;
					continue;
				case 26:
					return false;
				case 3:
					defaultInterpolatedStringHandler.AppendFormatted(projectorAttractivenessBonus);
					num2 = 32;
					continue;
				case 18:
					ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 38;
					continue;
				case 34:
					defaultInterpolatedStringHandler.AppendLiteral(", actual=");
					num2 = 47;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d5546cacfc14d65b5f225b6fd1f036b != 0)
					{
						num2 = 47;
					}
					continue;
				case 35:
					defaultInterpolatedStringHandler.AppendFormatted(baseStoreAttractiveness);
					num2 = 53;
					continue;
				case 4:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] SetStoreAttractiveness has no exact solution: target=");
					num2 = 19;
					continue;
				case 23:
					return false;
				case 37:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fe4f9d6dc55a460394d4814bd118c0dd == 0)
					{
						num2 = 11;
					}
					continue;
				case 41:
					defaultInterpolatedStringHandler.AppendFormatted(num3, "P0");
					num2 = 20;
					continue;
				case 10:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(97, 3);
					num2 = 4;
					continue;
				case 40:
					defaultInterpolatedStringHandler.AppendFormatted(num3);
					num2 = 28;
					continue;
				case 9:
					if (num3 >= 1f)
					{
						num2 = 54;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4068ee93bb84461dbacde7f72600241d != 0)
						{
							num2 = 8;
						}
					}
					else if (VJZqrBwPn(P_0, projectorAttractivenessBonus, num3, out num4))
					{
						instance.baseStoreAttractiveness = num4;
						num2 = 48;
					}
					else
					{
						num2 = 10;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3192da3ead8846b09a16ca828ae45a1b == 0)
						{
							num2 = 5;
						}
					}
					continue;
				case 7:
					num5 = 0f;
					break;
				case 32:
					defaultInterpolatedStringHandler.AppendLiteral(", penalty=");
					num2 = 18;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d5546cacfc14d65b5f225b6fd1f036b == 0)
					{
						num2 = 42;
					}
					continue;
				case 25:
					defaultInterpolatedStringHandler.AppendLiteral(", base=");
					num2 = 8;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_df53a978e2fa4caeb588ba7b3d0a9c1a != 0)
					{
						num2 = 8;
					}
					continue;
				case 5:
					projectorAttractivenessBonus = instance.projectorAttractivenessBonus;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_6d605cc618ce4b50b73229894686788c == 0)
					{
						num2 = 0;
					}
					continue;
				default:
					if (instance.garbageLevel <= 2)
					{
						num2 = 7;
						continue;
					}
					num5 = (float)instance.garbageLevel * 0.05f;
					break;
				case 13:
					return false;
				case 43:
					ModLogger.Error(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 12;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_ad54c7c384e2403b8eedf340fa3b3f17 == 0)
					{
						num2 = 23;
					}
					continue;
				case 46:
					goto end_IL_0012;
				case 54:
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(102, 1);
					num2 = 8;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e7642139c72f4727920bbb31032e9427 == 0)
					{
						num2 = 12;
					}
					continue;
				case 38:
					return false;
				case 21:
					defaultInterpolatedStringHandler.AppendFormatted(projectorAttractivenessBonus);
					num2 = 16;
					continue;
				case 36:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] SetStoreAttractiveness verification failed and was restored: target=");
					num2 = 22;
					continue;
				case 1:
					defaultInterpolatedStringHandler.AppendLiteral(", bonus=");
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0b6ab60d7d104659bda88e4aca0f9eb0 == 0)
					{
						num2 = 3;
					}
					continue;
				case 11:
					defaultInterpolatedStringHandler.AppendLiteral(", storeReady=");
					num2 = 2;
					continue;
				case 53:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 43;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_ef8574439d7a490da7abdd9bdba3db77 != 0)
					{
						num2 = 15;
					}
					continue;
				case 30:
					if (P_0 >= 0)
					{
						baseStoreAttractiveness = instance.baseStoreAttractiveness;
						num2 = 5;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_307958825cbc41459ac5f3f6966c27f4 == 0)
						{
							num2 = 0;
						}
					}
					else
					{
						num2 = 29;
					}
					continue;
				case 8:
					defaultInterpolatedStringHandler.AppendFormatted(num4);
					num2 = 17;
					continue;
				case 24:
					defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] SetStoreAttractiveness rejected: target=");
					num2 = 13;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_cc7b4e1d49b04480ade703081cc93669 == 0)
					{
						num2 = 37;
					}
					continue;
				case 2:
					defaultInterpolatedStringHandler.AppendFormatted(instance != null);
					num2 = 13;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_47c9d108ec114961a58b1bfbfefe6bab == 0)
					{
						num2 = 27;
					}
					continue;
				case 51:
					ModLogger.Warning(defaultInterpolatedStringHandler.ToStringAndClear());
					num2 = 13;
					continue;
				case 49:
					defaultInterpolatedStringHandler.AppendLiteral(".");
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_3e3cff0094cd4baea9d62981b3f335fc == 0)
					{
						num2 = 1;
					}
					continue;
				case 19:
					defaultInterpolatedStringHandler.AppendFormatted(P_0);
					num2 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_8c5a1355f678414f9d81c8e8dee12b23 != 0)
					{
						num2 = 1;
					}
					continue;
				case 45:
					if (instance != null)
					{
						num2 = 30;
						continue;
					}
					goto case 29;
				case 42:
					defaultInterpolatedStringHandler.AppendFormatted(num3);
					num2 = 49;
					continue;
				}
				num3 = num5;
				num2 = 9;
				continue;
				end_IL_0012:
				break;
			}
			instance = PlayerStore.Instance;
			num = 45;
		}
	}

	private static bool VJZqrBwPn(int P_0, int P_1, float P_2, out int P_3)
	{
		int num = 1;
		int num5 = default(int);
		long num3 = default(long);
		int num7 = default(int);
		int num4 = default(int);
		long num6 = default(long);
		while (true)
		{
			int num2 = num;
			while (true)
			{
				switch (num2)
				{
				case 6:
					if (ntrm1ZXrB(num5, P_1, P_2) != P_0)
					{
						num = 12;
						break;
					}
					P_3 = num5;
					num2 = 6;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_64dea62aaa174adaacac66b92a1d143b != 0)
					{
						num2 = 15;
					}
					continue;
				default:
					num3 = ntrm1ZXrB(0, P_1, P_2);
					num2 = 7;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_dcfa316cbf1043838ee9b641776e7534 == 0)
					{
						num2 = 2;
					}
					continue;
				case 5:
				case 8:
				case 9:
					if (num5 >= num7)
					{
						num2 = 4;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_e812b35a98764d43b77f4c9af4c260f0 == 0)
						{
							num2 = 6;
						}
						continue;
					}
					goto case 11;
				case 4:
					num7 = num4;
					num2 = 7;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fb7eb7bc2d2840c29b380d12b6798ec5 != 0)
					{
						num2 = 8;
					}
					continue;
				case 11:
					num4 = num5 + (int)(((long)num7 - (long)num5) / 2);
					num = 16;
					break;
				case 12:
					return false;
				case 13:
					if (P_0 >= num3)
					{
						num2 = 1;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_297f6589e95842c194c52929e3bb51ae == 0)
						{
							num2 = 2;
						}
						continue;
					}
					goto case 10;
				case 14:
					num7 = int.MaxValue;
					num2 = 9;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_df53a978e2fa4caeb588ba7b3d0a9c1a == 0)
					{
						num2 = 1;
					}
					continue;
				case 2:
					if (P_0 > num6)
					{
						num2 = 10;
						continue;
					}
					num5 = 0;
					num2 = 14;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4068ee93bb84461dbacde7f72600241d != 0)
					{
						num2 = 14;
					}
					continue;
				case 10:
					return false;
				case 3:
					num5 = num4 + 1;
					num2 = 5;
					continue;
				case 15:
					return true;
				case 7:
					num6 = ntrm1ZXrB(int.MaxValue, P_1, P_2);
					num2 = 13;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b0a518c9db76478e920a48eb8abfc0e9 != 0)
					{
						num2 = 3;
					}
					continue;
				case 16:
					if (ntrm1ZXrB(num4, P_1, P_2) < P_0)
					{
						num2 = 3;
						continue;
					}
					goto case 4;
				case 1:
					P_3 = 0;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_ad54c7c384e2403b8eedf340fa3b3f17 == 0)
					{
						num2 = 0;
					}
					continue;
				}
				break;
			}
		}
	}

	private static long ntrm1ZXrB(int P_0, int P_1, float P_2)
	{
		int num = 1;
		int num2 = num;
		int num3 = default(int);
		while (true)
		{
			switch (num2)
			{
			default:
				return (long)P_0 + (long)P_1 - num3;
			case 1:
				num3 = (int)((float)P_0 * P_2);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed == 0)
				{
					num2 = 0;
				}
				break;
			}
		}
	}

	internal static bool MFlkChAXZRqU0I5ffJF()
	{
		return T5X7hKAUa1UqMvV5K12 == null;
	}

	internal static SpvprrM2pJC2lXLvcU u6qyRUA4gV3BQw84PsY()
	{
		return T5X7hKAUa1UqMvV5K12;
	}
}
