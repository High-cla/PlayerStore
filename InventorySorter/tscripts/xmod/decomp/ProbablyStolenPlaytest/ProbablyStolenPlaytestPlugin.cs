using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using E1edgYxSHVYFaeHy7BP;
using Il2CppInterop.Runtime.Injection;
using ModFramework;
using ModFramework.GUI;
using ModFramework.Utilities;
using ProbablyStolenPlaytest.Components;
using ProbablyStolenPlaytest.GUI;
using T0r3LbyoAoBrPidtAH;
using TyOQ7hhkasLPlhFR3an;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ProbablyStolenPlaytest;

[BepInDependency(/*Could not decode attribute arguments.*/)]
[BepInPlugin("ProbablyStolenPlaytest", "Probably Stolen Playtest IL2CPP plugin", "1.0.0")]
public sealed class ProbablyStolenPlaytestPlugin : ModPluginBase
{
	[CompilerGenerated]
	private sealed class _003CConfigureRuntimePanelsWhenReady_003Ed__5 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public string sceneName;

		internal static _003CConfigureRuntimePanelsWhenReady_003Ed__5 V4grjW7mw338m17ZHaj;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CConfigureRuntimePanelsWhenReady_003Ed__5(int _003C_003E1__state)
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 1;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_458904e0f7944c07b3543e0a396db8a4 == 0)
			{
				num = 1;
			}
			while (true)
			{
				switch (num)
				{
				default:
					return;
				case 1:
					this._003C_003E1__state = _003C_003E1__state;
					num = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0203eca33da548f7b75f4844de41b607 != 0)
					{
						num = 0;
					}
					break;
				case 0:
					return;
				}
			}
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
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
					_003C_003E1__state = -2;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_a0969090f0a142399256f0ee456da969 != 0)
					{
						num2 = 0;
					}
					break;
				}
			}
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				break;
			case 1:
				_003C_003E1__state = -1;
				break;
			}
			if ((Object)(object)UIService.Instance == (Object)null || (Object)(object)UIService.Instance.ItemPanel == (Object)null || (Object)(object)UIService.Instance.PropertyPanel == (Object)null || PlayerStore.Instance == null || wAm1Zj0g39ZPo5iwxg.etAOsRlAA() == null || (Object)(object)RenderHandler.current == (Object)null)
			{
				_003C_003E2__current = null;
				_003C_003E1__state = 1;
				return true;
			}
			try
			{
				((Component)UIService.Instance.ItemPanel).GetComponent<ItemPanelAdapter>()?.tVWLSI4qIw();
				ModLogger.Info("[ProbablyStolenPlaytest] Runtime panels configured for " + sceneName + ".");
			}
			catch (Exception value)
			{
				ModLogger.Error($"[ProbablyStolenPlaytest] Runtime panel configuration failed: {value}");
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		internal static bool E3KNTu70ubLdqISfCr8()
		{
			return V4grjW7mw338m17ZHaj == null;
		}

		internal static _003CConfigureRuntimePanelsWhenReady_003Ed__5 H11qmw7yDmyI27IospG()
		{
			return V4grjW7mw338m17ZHaj;
		}
	}

	[CompilerGenerated]
	private sealed class _003CInitializeUI_003Ed__3 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		internal static _003CInitializeUI_003Ed__3 WkITaH7OjAg4pgu4XtC;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CInitializeUI_003Ed__3(int _003C_003E1__state)
		{
			bpND7PhQOXpROODtSab.XR4RtoBqtq();
			base._002Ector();
			int num = 1;
			if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0ee41f9c997a423d9a65c078a867a835 == 0)
			{
				num = 0;
			}
			while (true)
			{
				switch (num)
				{
				default:
					return;
				case 1:
					this._003C_003E1__state = _003C_003E1__state;
					num = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b0a518c9db76478e920a48eb8abfc0e9 != 0)
					{
						num = 0;
					}
					break;
				case 0:
					return;
				}
			}
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
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
					_003C_003E1__state = -2;
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2af9e2b953064803b9db93f92d6e9d4e == 0)
					{
						num2 = 0;
					}
					break;
				case 0:
					return;
				}
			}
		}

		private bool MoveNext()
		{
			int num = 17;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = default(DefaultInterpolatedStringHandler);
			int num5 = default(int);
			while (true)
			{
				int num2 = num;
				while (true)
				{
					switch (num2)
					{
					case 8:
						try
						{
							sfumh2xPLltR4pL0i9k.afdxikgtY9(UIService.Instance.ItemPanel, UIService.Instance.PropertyPanel);
							int num3 = 2;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_df53a978e2fa4caeb588ba7b3d0a9c1a != 0)
							{
								num3 = 2;
							}
							while (true)
							{
								switch (num3)
								{
								default:
									ModLogger.Info("[ProbablyStolenPlaytest] Framework panels initialized.");
									num3 = 3;
									continue;
								case 2:
									if (!((Object)(object)((Component)UIService.Instance.ItemPanel).GetComponent<ItemPanelAdapter>() == (Object)null))
									{
										num3 = 0;
										if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_b38508003949434ea7a070c228110021 != 0)
										{
											num3 = 0;
										}
										continue;
									}
									break;
								case 4:
									break;
								case 3:
									goto end_IL_00a5;
								}
								((Component)UIService.Instance.ItemPanel).gameObject.AddComponent<ItemPanelAdapter>();
								num3 = 1;
								if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c2b372e98ebd4e50b2c43c7d4173610a != 0)
								{
									num3 = 0;
								}
								continue;
								end_IL_00a5:
								break;
							}
						}
						catch (Exception value)
						{
							int num4 = 0;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fce81e3a8ede47fab4085a25772731cb != 0)
							{
								num4 = 3;
							}
							while (true)
							{
								switch (num4)
								{
								case 3:
									defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 1);
									num4 = 1;
									if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed == 0)
									{
										num4 = 1;
									}
									continue;
								case 2:
									ModLogger.Error(defaultInterpolatedStringHandler.ToStringAndClear());
									num4 = 0;
									if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_530166a6640e4d928390290fcc4f133b != 0)
									{
										num4 = 0;
									}
									continue;
								case 1:
									defaultInterpolatedStringHandler.AppendLiteral("[ProbablyStolenPlaytest] Framework UI initialization failed: ");
									num4 = 4;
									continue;
								case 4:
									defaultInterpolatedStringHandler.AppendFormatted(value);
									num4 = 2;
									continue;
								case 0:
									break;
								}
								break;
							}
						}
						goto case 2;
					case 11:
						if (!UIService.Instance.IsInitialized)
						{
							num2 = 9;
							break;
						}
						goto case 18;
					case 4:
					case 6:
						if ((Object)(object)UIService.Instance == (Object)null)
						{
							num2 = 5;
							break;
						}
						goto case 11;
					case 15:
						num2 = 8;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_2df26eab4c194a2cabc45ac854ca3536 != 0)
						{
							num2 = 0;
						}
						break;
					case 2:
						return false;
					case 16:
						if (num5 == 0)
						{
							num2 = 1;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06f41aa755564f58bb705d3393f40eed == 0)
							{
								num2 = 0;
							}
							break;
						}
						goto case 14;
					case 17:
						num5 = _003C_003E1__state;
						num2 = 16;
						break;
					case 13:
						return true;
					case 5:
					case 7:
					case 9:
					case 10:
						_003C_003E2__current = null;
						num2 = 3;
						break;
					case 12:
						if (!((Object)(object)UIService.Instance.PropertyPanel == (Object)null))
						{
							num2 = 8;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_c30761e1724845948fdd25fb5a79280d != 0)
							{
								num2 = 15;
							}
							break;
						}
						goto case 5;
					default:
						return false;
					case 1:
						_003C_003E1__state = -1;
						num2 = 2;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_919559582d244363930dd58efaa3a5a1 == 0)
						{
							num2 = 4;
						}
						break;
					case 3:
						_003C_003E1__state = 1;
						num2 = 13;
						break;
					case 14:
						if (num5 == 1)
						{
							goto end_IL_0012;
						}
						num2 = 0;
						if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_49e68abb97b9490ca701229335018a23 == 0)
						{
							num2 = 0;
						}
						break;
					case 18:
						if ((Object)(object)UIService.Instance.ItemPanel == (Object)null)
						{
							num2 = 7;
							break;
						}
						goto case 12;
					}
					continue;
					end_IL_0012:
					break;
				}
				_003C_003E1__state = -1;
				num = 6;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		internal static bool Qh3S3q7WWNIXnScefo9()
		{
			return WkITaH7OjAg4pgu4XtC == null;
		}

		internal static _003CInitializeUI_003Ed__3 KCYJ027oMPTIam07JBL()
		{
			return WkITaH7OjAg4pgu4XtC;
		}
	}

	private static readonly UnityAction<Scene, LoadSceneMode> SceneLoadedHandler;

	internal static ProbablyStolenPlaytestPlugin ETXZPwAKkyd1UXJdArG;

	public override void OnPreLoad()
	{
		int num = 3;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 3:
				base.OnPreLoad();
				num2 = 2;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4877fc232b62465299156662d37e1227 != 0)
				{
					num2 = 0;
				}
				break;
			default:
				ModFrameworkCore.Instance.UsePanel.Value = true;
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_06c609b65310425284933fc78452e316 == 0)
				{
					num2 = 1;
				}
				break;
			case 2:
				if (ModFrameworkCore.Instance != null)
				{
					num2 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1b4fe3e0fc2c40fc86f25cd1934dae42 == 0)
					{
						num2 = 0;
					}
					break;
				}
				return;
			case 1:
				return;
			}
		}
	}

	protected override void SetupLate()
	{
		int num = 2;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			case 7:
				RuntimeHelper.StartCoroutine(InitializeUI());
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0c883aef9e214bdbbe2f931c98600d64 != 0)
				{
					num2 = 5;
				}
				break;
			case 2:
				Info("[ProbablyStolenPlaytest] SetupLate");
				num2 = 1;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4606931d409d4632bb5c9d4bf6dc4c74 == 0)
				{
					num2 = 0;
				}
				break;
			case 4:
				((BasePlugin)this).AddComponent<ProbablyStolenPlaytestComponent>();
				num2 = 3;
				break;
			case 6:
				SceneManager.sceneLoaded += SceneLoadedHandler;
				num2 = 7;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0203eca33da548f7b75f4844de41b607 == 0)
				{
					num2 = 2;
				}
				break;
			case 3:
				try
				{
					ModPluginBase.Harmony.PatchAll();
					int num3 = 1;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0d326bf98637481998cf110a56c1622c == 0)
					{
						num3 = 0;
					}
					while (true)
					{
						switch (num3)
						{
						case 1:
							Info("[ProbablyStolenPlaytest] Harmony patches applied.");
							num3 = 0;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_d51c61ea1cd54949ab81b08153329f54 == 0)
							{
								num3 = 0;
							}
							continue;
						case 0:
							break;
						}
						break;
					}
				}
				catch (Exception value)
				{
					int num4 = 0;
					if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_1f9e3467df7e4ab2b9086d9e74e2921e != 0)
					{
						num4 = 0;
					}
					while (true)
					{
						switch (num4)
						{
						default:
							Error($"[ProbablyStolenPlaytest] Harmony patching failed: {value}");
							num4 = 1;
							if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_5954d2af39bb400db95fb8007cfbce4a == 0)
							{
								num4 = 1;
							}
							continue;
						case 1:
							break;
						}
						break;
					}
				}
				goto case 6;
			case 5:
				return;
			default:
				ClassInjector.RegisterTypeInIl2Cpp<ItemPanelAdapter>();
				num2 = 4;
				break;
			case 1:
				ClassInjector.RegisterTypeInIl2Cpp<ProbablyStolenPlaytestComponent>();
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_acf4dbc23cf347efba74119535a62f63 != 0)
				{
					num2 = 0;
				}
				break;
			}
		}
	}

	[IteratorStateMachine(typeof(_003CInitializeUI_003Ed__3))]
	private static IEnumerator InitializeUI()
	{
		//yield-return decompiler failed: Missing enumeratorCtor.Body
		return new _003CInitializeUI_003Ed__3(0);
	}

	private static void HandleSceneLoaded(Scene scene, LoadSceneMode _)
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
				RuntimeHelper.StartCoroutine(ConfigureRuntimePanelsWhenReady(((Scene)(ref scene)).name));
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_fce81e3a8ede47fab4085a25772731cb != 0)
				{
					num2 = 0;
				}
				break;
			case 0:
				return;
			}
		}
	}

	[IteratorStateMachine(typeof(_003CConfigureRuntimePanelsWhenReady_003Ed__5))]
	private static IEnumerator ConfigureRuntimePanelsWhenReady(string sceneName)
	{
		//yield-return decompiler failed: Missing enumeratorCtor.Body
		return new _003CConfigureRuntimePanelsWhenReady_003Ed__5(0)
		{
			sceneName = sceneName
		};
	}

	public ProbablyStolenPlaytestPlugin()
	{
		bpND7PhQOXpROODtSab.XR4RtoBqtq();
		base._002Ector();
		int num = 0;
		if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_4456f4be186044e0b1a4dc9bfe657743 == 0)
		{
			num = 0;
		}
		switch (num)
		{
		case 0:
			break;
		}
	}

	static ProbablyStolenPlaytestPlugin()
	{
		int num = 2;
		int num2 = num;
		while (true)
		{
			switch (num2)
			{
			default:
				return;
			case 1:
				SceneLoadedHandler = UnityAction<Scene, LoadSceneMode>.op_Implicit((Action<Scene, LoadSceneMode>)HandleSceneLoaded);
				num2 = 0;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_0203eca33da548f7b75f4844de41b607 == 0)
				{
					num2 = 0;
				}
				break;
			case 0:
				return;
			case 2:
				bpND7PhQOXpROODtSab.XR4RtoBqtq();
				num2 = 1;
				if (_003CModule_003E_007B3cb1b05b_002Db19c_002D46ef_002D8f31_002D4b9ff601b62e_007D.m_5ae0de2130104667821cf81a8800d8d6.m_08a11342c71f4153a4564b4e92bd109c != 0)
				{
					num2 = 1;
				}
				break;
			}
		}
	}

	internal static bool f4nwbEAP0jAfSs3doZm()
	{
		return ETXZPwAKkyd1UXJdArG == null;
	}

	internal static ProbablyStolenPlaytestPlugin akYcYfAS8WreEdp1N6M()
	{
		return ETXZPwAKkyd1UXJdArG;
	}
}
