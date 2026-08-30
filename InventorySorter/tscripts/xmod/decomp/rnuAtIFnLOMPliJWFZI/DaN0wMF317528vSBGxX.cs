using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using ewdhTvhVm91SkfLdFdO;

namespace rnuAtIFnLOMPliJWFZI;

internal class DaN0wMF317528vSBGxX
{
	internal class etp047h8a8q4KcyOGp0
	{
		private BinaryReader R6ihwFFWSd;

		public etp047h8a8q4KcyOGp0(Stream P_0)
		{
			R6ihwFFWSd = new BinaryReader(P_0);
		}

		[SpecialName]
		internal Stream jjkKL9b4BZ()
		{
			return R6ihwFFWSd.BaseStream;
		}

		internal byte[] x48hf8YQmQ(int P_0)
		{
			return R6ihwFFWSd.ReadBytes(P_0);
		}

		internal int oxPhFmJut7(byte[] P_0, int P_1, int P_2)
		{
			return R6ihwFFWSd.Read(P_0, P_1, P_2);
		}

		internal int NyVhhSE3x0()
		{
			return R6ihwFFWSd.ReadInt32();
		}

		internal void hhchIq91YO()
		{
			R6ihwFFWSd.Close();
		}
	}

	private delegate void xvpUU2hduj5q9l1u74K(object o);

	internal class epkZ5hhDpZ7L5wWZHy1
	{
		internal static string KYwh316naf(object P_0, object P_1)
		{
			byte[] bytes = Encoding.Unicode.GetBytes((string)P_0);
			byte[] key = new byte[32]
			{
				82, 102, 104, 110, 32, 77, 24, 34, 118, 181,
				51, 17, 18, 51, 12, 109, 10, 32, 77, 24,
				34, 158, 161, 41, 97, 28, 118, 181, 5, 25,
				1, 88
			};
			byte[] iV = MMWFjL6ywZ(Encoding.Unicode.GetBytes((string)P_1));
			MemoryStream memoryStream = new MemoryStream();
			SymmetricAlgorithm symmetricAlgorithm = jDBFPV1bCi();
			symmetricAlgorithm.Key = key;
			symmetricAlgorithm.IV = iV;
			CryptoStream cryptoStream = new CryptoStream(memoryStream, symmetricAlgorithm.CreateEncryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(bytes, 0, bytes.Length);
			cryptoStream.Close();
			return Convert.ToBase64String(memoryStream.ToArray());
		}
	}

	private static uint[] IitFZNUj5n;

	private static bool XqoFV5evIF;

	private static bool fKdFuaeqP8;

	private static Dictionary<int, int> Br7FpQOqKe;

	private static object WoyFtOeHMC;

	private static Assembly MXNFvpHhWk;

	private static byte[] MsfFcvB5VL;

	private static RSACryptoServiceProvider lMeFl0PSV5;

	private static byte[] TryFG50bSG;

	private static object Ff9FEmZDqB;

	private static List<string> Cy0FgjZDOp;

	private static List<int> TLfFzhNhMN;

	private static int YNVh6kRZ4U;

	private byte[] pRZhx3Lmx8;

	private byte[] dBJhLd1KjR;

	static DaN0wMF317528vSBGxX()
	{
		IitFZNUj5n = new uint[64]
		{
			3614090360u, 3905402710u, 606105819u, 3250441966u, 4118548399u, 1200080426u, 2821735955u, 4249261313u, 1770035416u, 2336552879u,
			4294925233u, 2304563134u, 1804603682u, 4254626195u, 2792965006u, 1236535329u, 4129170786u, 3225465664u, 643717713u, 3921069994u,
			3593408605u, 38016083u, 3634488961u, 3889429448u, 568446438u, 3275163606u, 4107603335u, 1163531501u, 2850285829u, 4243563512u,
			1735328473u, 2368359562u, 4294588738u, 2272392833u, 1839030562u, 4259657740u, 2763975236u, 1272893353u, 4139469664u, 3200236656u,
			681279174u, 3936430074u, 3572445317u, 76029189u, 3654602809u, 3873151461u, 530742520u, 3299628645u, 4096336452u, 1126891415u,
			2878612391u, 4237533241u, 1700485571u, 2399980690u, 4293915773u, 2240044497u, 1873313359u, 4264355552u, 2734768916u, 1309151649u,
			4149444226u, 3174756917u, 718787259u, 3951481745u
		};
		XqoFV5evIF = false;
		fKdFuaeqP8 = false;
		Br7FpQOqKe = null;
		WoyFtOeHMC = new object();
		MXNFvpHhWk = typeof(DaN0wMF317528vSBGxX).Assembly;
		MsfFcvB5VL = new byte[0];
		lMeFl0PSV5 = null;
		TryFG50bSG = new byte[0];
		Ff9FEmZDqB = new object();
		Cy0FgjZDOp = null;
		TLfFzhNhMN = null;
		YNVh6kRZ4U = 0;
		try
		{
			RSACryptoServiceProvider.UseMachineKeyStore = true;
		}
		catch
		{
		}
	}

	internal DaN0wMF317528vSBGxX()
	{
	}

	private void CmMRppj6iO()
	{
	}

	internal static byte[] KA2FMj6opj(object P_0)
	{
		uint[] array = new uint[16];
		uint num = (uint)((448 - ((Array)P_0).Length * 8 % 512 + 512) % 512);
		if (num == 0)
		{
			num = 512u;
		}
		uint num2 = (uint)(((Array)P_0).Length + num / 8 + 8);
		ulong num3 = (ulong)((Array)P_0).Length * 8uL;
		byte[] array2 = new byte[num2];
		for (int i = 0; i < ((Array)P_0).Length; i++)
		{
			array2[i] = ((byte[])P_0)[i];
		}
		array2[((Array)P_0).Length] |= 128;
		for (int num4 = 8; num4 > 0; num4--)
		{
			array2[num2 - num4] = (byte)((num3 >> (8 - num4) * 8) & 0xFF);
		}
		uint num5 = (uint)(array2.Length * 8) / 32u;
		uint num6 = 1732584193u;
		uint num7 = 4023233417u;
		uint num8 = 2562383102u;
		uint num9 = 271733878u;
		for (uint num10 = 0u; num10 < num5 / 16; num10++)
		{
			uint num11 = num10 << 6;
			for (uint num12 = 0u; num12 < 61; num12 += 4)
			{
				array[num12 >> 2] = (uint)((array2[num11 + (num12 + 3)] << 24) | (array2[num11 + (num12 + 2)] << 16) | (array2[num11 + (num12 + 1)] << 8) | array2[num11 + num12]);
			}
			uint num13 = num6;
			uint num14 = num7;
			uint num15 = num8;
			uint num16 = num9;
			BdMF16dmuM(ref num6, num7, num8, num9, 0u, 7, 1u, array);
			BdMF16dmuM(ref num9, num6, num7, num8, 1u, 12, 2u, array);
			BdMF16dmuM(ref num8, num9, num6, num7, 2u, 17, 3u, array);
			BdMF16dmuM(ref num7, num8, num9, num6, 3u, 22, 4u, array);
			BdMF16dmuM(ref num6, num7, num8, num9, 4u, 7, 5u, array);
			BdMF16dmuM(ref num9, num6, num7, num8, 5u, 12, 6u, array);
			BdMF16dmuM(ref num8, num9, num6, num7, 6u, 17, 7u, array);
			BdMF16dmuM(ref num7, num8, num9, num6, 7u, 22, 8u, array);
			BdMF16dmuM(ref num6, num7, num8, num9, 8u, 7, 9u, array);
			BdMF16dmuM(ref num9, num6, num7, num8, 9u, 12, 10u, array);
			BdMF16dmuM(ref num8, num9, num6, num7, 10u, 17, 11u, array);
			BdMF16dmuM(ref num7, num8, num9, num6, 11u, 22, 12u, array);
			BdMF16dmuM(ref num6, num7, num8, num9, 12u, 7, 13u, array);
			BdMF16dmuM(ref num9, num6, num7, num8, 13u, 12, 14u, array);
			BdMF16dmuM(ref num8, num9, num6, num7, 14u, 17, 15u, array);
			BdMF16dmuM(ref num7, num8, num9, num6, 15u, 22, 16u, array);
			nHxFAZsTiw(ref num6, num7, num8, num9, 1u, 5, 17u, array);
			nHxFAZsTiw(ref num9, num6, num7, num8, 6u, 9, 18u, array);
			nHxFAZsTiw(ref num8, num9, num6, num7, 11u, 14, 19u, array);
			nHxFAZsTiw(ref num7, num8, num9, num6, 0u, 20, 20u, array);
			nHxFAZsTiw(ref num6, num7, num8, num9, 5u, 5, 21u, array);
			nHxFAZsTiw(ref num9, num6, num7, num8, 10u, 9, 22u, array);
			nHxFAZsTiw(ref num8, num9, num6, num7, 15u, 14, 23u, array);
			nHxFAZsTiw(ref num7, num8, num9, num6, 4u, 20, 24u, array);
			nHxFAZsTiw(ref num6, num7, num8, num9, 9u, 5, 25u, array);
			nHxFAZsTiw(ref num9, num6, num7, num8, 14u, 9, 26u, array);
			nHxFAZsTiw(ref num8, num9, num6, num7, 3u, 14, 27u, array);
			nHxFAZsTiw(ref num7, num8, num9, num6, 8u, 20, 28u, array);
			nHxFAZsTiw(ref num6, num7, num8, num9, 13u, 5, 29u, array);
			nHxFAZsTiw(ref num9, num6, num7, num8, 2u, 9, 30u, array);
			nHxFAZsTiw(ref num8, num9, num6, num7, 7u, 14, 31u, array);
			nHxFAZsTiw(ref num7, num8, num9, num6, 12u, 20, 32u, array);
			bVbF7h28jZ(ref num6, num7, num8, num9, 5u, 4, 33u, array);
			bVbF7h28jZ(ref num9, num6, num7, num8, 8u, 11, 34u, array);
			bVbF7h28jZ(ref num8, num9, num6, num7, 11u, 16, 35u, array);
			bVbF7h28jZ(ref num7, num8, num9, num6, 14u, 23, 36u, array);
			bVbF7h28jZ(ref num6, num7, num8, num9, 1u, 4, 37u, array);
			bVbF7h28jZ(ref num9, num6, num7, num8, 4u, 11, 38u, array);
			bVbF7h28jZ(ref num8, num9, num6, num7, 7u, 16, 39u, array);
			bVbF7h28jZ(ref num7, num8, num9, num6, 10u, 23, 40u, array);
			bVbF7h28jZ(ref num6, num7, num8, num9, 13u, 4, 41u, array);
			bVbF7h28jZ(ref num9, num6, num7, num8, 0u, 11, 42u, array);
			bVbF7h28jZ(ref num8, num9, num6, num7, 3u, 16, 43u, array);
			bVbF7h28jZ(ref num7, num8, num9, num6, 6u, 23, 44u, array);
			bVbF7h28jZ(ref num6, num7, num8, num9, 9u, 4, 45u, array);
			bVbF7h28jZ(ref num9, num6, num7, num8, 12u, 11, 46u, array);
			bVbF7h28jZ(ref num8, num9, num6, num7, 15u, 16, 47u, array);
			bVbF7h28jZ(ref num7, num8, num9, num6, 2u, 23, 48u, array);
			WKpFsH536S(ref num6, num7, num8, num9, 0u, 6, 49u, array);
			WKpFsH536S(ref num9, num6, num7, num8, 7u, 10, 50u, array);
			WKpFsH536S(ref num8, num9, num6, num7, 14u, 15, 51u, array);
			WKpFsH536S(ref num7, num8, num9, num6, 5u, 21, 52u, array);
			WKpFsH536S(ref num6, num7, num8, num9, 12u, 6, 53u, array);
			WKpFsH536S(ref num9, num6, num7, num8, 3u, 10, 54u, array);
			WKpFsH536S(ref num8, num9, num6, num7, 10u, 15, 55u, array);
			WKpFsH536S(ref num7, num8, num9, num6, 1u, 21, 56u, array);
			WKpFsH536S(ref num6, num7, num8, num9, 8u, 6, 57u, array);
			WKpFsH536S(ref num9, num6, num7, num8, 15u, 10, 58u, array);
			WKpFsH536S(ref num8, num9, num6, num7, 6u, 15, 59u, array);
			WKpFsH536S(ref num7, num8, num9, num6, 13u, 21, 60u, array);
			WKpFsH536S(ref num6, num7, num8, num9, 4u, 6, 61u, array);
			WKpFsH536S(ref num9, num6, num7, num8, 11u, 10, 62u, array);
			WKpFsH536S(ref num8, num9, num6, num7, 2u, 15, 63u, array);
			WKpFsH536S(ref num7, num8, num9, num6, 9u, 21, 64u, array);
			num6 += num13;
			num7 += num14;
			num8 += num15;
			num9 += num16;
		}
		byte[] array3 = new byte[16];
		Array.Copy(BitConverter.GetBytes(num6), 0, array3, 0, 4);
		Array.Copy(BitConverter.GetBytes(num7), 0, array3, 4, 4);
		Array.Copy(BitConverter.GetBytes(num8), 0, array3, 8, 4);
		Array.Copy(BitConverter.GetBytes(num9), 0, array3, 12, 4);
		return array3;
	}

	private static void BdMF16dmuM(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + cGgF5xh0A1(P_0 + ((P_1 & P_2) | (~P_1 & P_3)) + ((uint[])P_7)[P_4] + IitFZNUj5n[P_6 - 1], P_5);
	}

	private static void nHxFAZsTiw(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + cGgF5xh0A1(P_0 + ((P_1 & P_3) | (P_2 & ~P_3)) + ((uint[])P_7)[P_4] + IitFZNUj5n[P_6 - 1], P_5);
	}

	private static void bVbF7h28jZ(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + cGgF5xh0A1(P_0 + (P_1 ^ P_2 ^ P_3) + ((uint[])P_7)[P_4] + IitFZNUj5n[P_6 - 1], P_5);
	}

	private static void WKpFsH536S(ref uint P_0, uint P_1, uint P_2, uint P_3, uint P_4, ushort P_5, uint P_6, object P_7)
	{
		P_0 = P_1 + cGgF5xh0A1(P_0 + (P_2 ^ (P_1 | ~P_3)) + ((uint[])P_7)[P_4] + IitFZNUj5n[P_6 - 1], P_5);
	}

	private static uint cGgF5xh0A1(uint P_0, ushort P_1)
	{
		return (P_0 >> 32 - P_1) | (P_0 << (int)P_1);
	}

	internal static byte[] MMWFjL6ywZ(object P_0)
	{
		if (!C4AFqYP6Ov())
		{
			return new MD5CryptoServiceProvider().ComputeHash((byte[])P_0);
		}
		return KA2FMj6opj(P_0);
	}

	private static void j1VFR0QCSB()
	{
		try
		{
			RSACryptoServiceProvider.UseMachineKeyStore = true;
		}
		catch
		{
		}
	}

	internal static bool C4AFqYP6Ov()
	{
		if (!XqoFV5evIF)
		{
			z7DFKxAasQ();
			XqoFV5evIF = true;
		}
		return fKdFuaeqP8;
	}

	internal byte[] qkHFm6p9Zj()
	{
		_ = "qXFrdYi0qf5qHBdT".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	internal byte[] o9MF0Rf5Ey()
	{
		_ = "wsA3R8CYcb9Rd0e".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	internal byte[] gfcFyEOT0M()
	{
		_ = "QQZYDSA7jjNpbGrfAn0mNU".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	internal byte[] TKDFOSjXXX()
	{
		_ = "G5rbQaFVD0mTKGDn8J".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	public static void om4FWvRlqb(RuntimeTypeHandle P_0)
	{
		try
		{
			Type typeFromHandle = Type.GetTypeFromHandle(P_0);
			if (Br7FpQOqKe == null)
			{
				lock (WoyFtOeHMC)
				{
					Dictionary<int, int> dictionary = new Dictionary<int, int>();
					BinaryReader binaryReader = new BinaryReader(typeof(DaN0wMF317528vSBGxX).Assembly.GetManifestResourceStream("ZNAghe38utS9sfURUR.QUPgl6npJcZQ7JhFT6"));
					binaryReader.BaseStream.Position = 0L;
					byte[] array = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
					binaryReader.Close();
					if (array.Length != 0)
					{
						int num = array.Length % 4;
						int num2 = array.Length / 4;
						byte[] array2 = new byte[array.Length];
						uint num3 = 0u;
						uint num4 = 0u;
						if (num > 0)
						{
							num2++;
						}
						uint num5 = 0u;
						for (int i = 0; i < num2; i++)
						{
							int num6 = i * 4;
							uint num7 = 255u;
							int num8 = 0;
							if (i == num2 - 1 && num > 0)
							{
								num4 = 0u;
								for (int j = 0; j < num; j++)
								{
									if (j > 0)
									{
										num4 <<= 8;
									}
									num4 |= array[^(1 + j)];
								}
							}
							else
							{
								num5 = (uint)num6;
								num4 = (uint)((array[num5 + 3] << 24) | (array[num5 + 2] << 16) | (array[num5 + 1] << 8) | array[num5]);
							}
							num3 = num3;
							num3 += LafFoEovv2(num3);
							if (i == num2 - 1 && num > 0)
							{
								uint num9 = num3 ^ num4;
								for (int k = 0; k < num; k++)
								{
									if (k > 0)
									{
										num7 <<= 8;
										num8 += 8;
									}
									array2[num6 + k] = (byte)((num9 & num7) >> num8);
								}
							}
							else
							{
								uint num10 = num3 ^ num4;
								array2[num6] = (byte)(num10 & 0xFF);
								array2[num6 + 1] = (byte)((num10 & 0xFF00) >> 8);
								array2[num6 + 2] = (byte)((num10 & 0xFF0000) >> 16);
								array2[num6 + 3] = (byte)((num10 & 0xFF000000u) >> 24);
							}
						}
						array = array2;
						array2 = null;
						int num11 = array.Length / 8;
						etp047h8a8q4KcyOGp0 etp047h8a8q4KcyOGp1 = new etp047h8a8q4KcyOGp0(new MemoryStream(array));
						for (int l = 0; l < num11; l++)
						{
							int key = etp047h8a8q4KcyOGp1.NyVhhSE3x0();
							int value = etp047h8a8q4KcyOGp1.NyVhhSE3x0();
							dictionary.Add(key, value);
						}
						etp047h8a8q4KcyOGp1.hhchIq91YO();
					}
					Br7FpQOqKe = dictionary;
				}
			}
			FieldInfo[] fields = typeFromHandle.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField);
			foreach (FieldInfo fieldInfo in fields)
			{
				int metadataToken = fieldInfo.MetadataToken;
				int num12 = Br7FpQOqKe[metadataToken];
				bool flag = (num12 & 0x40000000) > 0;
				num12 &= 0x3FFFFFFF;
				MethodInfo methodInfo = (MethodInfo)typeof(DaN0wMF317528vSBGxX).Module.ResolveMethod(num12, typeFromHandle.GetGenericArguments(), new Type[0]);
				if (methodInfo.IsStatic)
				{
					fieldInfo.SetValue(null, Delegate.CreateDelegate(fieldInfo.FieldType, methodInfo));
					continue;
				}
				ParameterInfo[] parameters = methodInfo.GetParameters();
				int num13 = parameters.Length + 1;
				Type[] array3 = new Type[num13];
				if (methodInfo.DeclaringType.IsValueType)
				{
					array3[0] = methodInfo.DeclaringType.MakeByRefType();
				}
				else
				{
					array3[0] = typeof(object);
				}
				for (int n = 0; n < parameters.Length; n++)
				{
					array3[n + 1] = parameters[n].ParameterType;
				}
				DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, methodInfo.ReturnType, array3, typeFromHandle, skipVisibility: true);
				ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
				for (int num14 = 0; num14 < num13; num14++)
				{
					switch (num14)
					{
					case 0:
						iLGenerator.Emit(OpCodes.Ldarg_0);
						break;
					case 1:
						iLGenerator.Emit(OpCodes.Ldarg_1);
						break;
					case 2:
						iLGenerator.Emit(OpCodes.Ldarg_2);
						break;
					case 3:
						iLGenerator.Emit(OpCodes.Ldarg_3);
						break;
					default:
						iLGenerator.Emit(OpCodes.Ldarg_S, num14);
						break;
					}
				}
				iLGenerator.Emit(OpCodes.Tailcall);
				iLGenerator.Emit(flag ? OpCodes.Callvirt : OpCodes.Call, methodInfo);
				iLGenerator.Emit(OpCodes.Ret);
				fieldInfo.SetValue(null, dynamicMethod.CreateDelegate(typeFromHandle));
			}
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	private static uint LafFoEovv2(uint P_0)
	{
		return 0u;
	}

	internal static void cBpFewR2sH()
	{
	}

	private static int JbAFC7LXyO()
	{
		return 5;
	}

	internal static void z7DFKxAasQ()
	{
		try
		{
			new MD5CryptoServiceProvider();
		}
		catch
		{
			fKdFuaeqP8 = true;
			return;
		}
		try
		{
			fKdFuaeqP8 = CryptoConfig.AllowOnlyFipsAlgorithms;
		}
		catch
		{
		}
	}

	internal static SymmetricAlgorithm jDBFPV1bCi()
	{
		SymmetricAlgorithm symmetricAlgorithm = null;
		if (C4AFqYP6Ov())
		{
			return new AesCryptoServiceProvider();
		}
		try
		{
			return new RijndaelManaged();
		}
		catch
		{
			return new AesCryptoServiceProvider();
		}
	}

	private byte[] zupFSIe1NQ()
	{
		_ = "A56rh8nhskZhgyIgM6".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	private byte[] SANFiZ3P8B()
	{
		_ = "Em3QW8QzlQEPwUVUzrQjKy".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	internal static void OBAF9xgsY1(object P_0, object P_1, uint P_2, object P_3)
	{
		while (P_2 != 0)
		{
			int num = ((P_2 > (uint)((Array)P_3).Length) ? ((Array)P_3).Length : ((int)P_2));
			((Stream)P_1).Read((byte[])P_3, 0, num);
			zxYF20nCCb(P_0, P_3, 0, num);
			P_2 -= (uint)num;
		}
	}

	internal static void zxYF20nCCb(object P_0, object P_1, int P_2, int P_3)
	{
		((HashAlgorithm)P_0).TransformBlock((byte[])P_1, P_2, P_3, (byte[]?)P_1, P_2);
	}

	internal static uint a4mFULfCx5(uint P_0, int P_1, long P_2, object P_3)
	{
		for (int i = 0; i < P_1; i++)
		{
			((BinaryReader)P_3).BaseStream.Position = P_2 + (i * 40 + 8);
			uint num = ((BinaryReader)P_3).ReadUInt32();
			uint num2 = ((BinaryReader)P_3).ReadUInt32();
			((BinaryReader)P_3).ReadUInt32();
			uint num3 = ((BinaryReader)P_3).ReadUInt32();
			if (num2 <= P_0 && P_0 < num2 + num)
			{
				return num3 + P_0 - num2;
			}
		}
		return 0u;
	}

	private static Stream OctFX8LDxp()
	{
		return new MemoryStream();
	}

	private static byte[] khBF4NmGOv(object P_0)
	{
		using FileStream fileStream = new FileStream((string)P_0, FileMode.Open, FileAccess.Read, FileShare.Read);
		int num = 0;
		int num2 = (int)fileStream.Length;
		byte[] array = new byte[num2];
		while (num2 > 0)
		{
			int num3 = fileStream.Read(array, num, num2);
			num += num3;
			num2 -= num3;
		}
		return array;
	}

	internal static object vBOFrewutq(object P_0)
	{
		try
		{
			if (File.Exists(((Assembly)P_0).Location))
			{
				return ((Assembly)P_0).Location;
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists(((Assembly)P_0).GetName().CodeBase.ToString().Replace("file:///", "")))
			{
				return ((Assembly)P_0).GetName().CodeBase.ToString().Replace("file:///", "");
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists(P_0.GetType().GetProperty("Location").GetValue(P_0, new object[0])
				.ToString()))
			{
				return P_0.GetType().GetProperty("Location").GetValue(P_0, new object[0])
					.ToString();
			}
		}
		catch
		{
		}
		return "";
	}

	private static byte[] i0TFb7RdaR(object P_0)
	{
		return ((MemoryStream)P_0).ToArray();
	}

	internal byte[] aiJFNUxvri()
	{
		_ = "VnMLeTz5XsQ23XsIAfrO".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	internal byte[] er7FaRWFJk()
	{
		_ = "FFakNKIiSBwdVhBnE".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void V18FHTDHFU(object P_0)
	{
		BRGKTrhZZGKj0pedeGx.rethvxYBqi(0, new object[1] { P_0 }, null);
	}

	private DaN0wMF317528vSBGxX(byte[] P_0, byte[] P_1)
	{
		pRZhx3Lmx8 = P_0;
		dBJhLd1KjR = P_1;
	}

	private byte[] zSHFBIkyRF(byte[] P_0)
	{
		if (P_0.Length == 0)
		{
			return new byte[0];
		}
		int num = P_0.Length % 4;
		int num2 = P_0.Length / 4;
		byte[] array = new byte[P_0.Length];
		int num3 = pRZhx3Lmx8.Length / 4;
		uint num4 = 0u;
		uint num5 = 0u;
		uint num6 = 0u;
		if (num > 0)
		{
			num2++;
		}
		uint num7 = 0u;
		for (int i = 0; i < num2; i++)
		{
			int num8 = i % num3;
			int num9 = i * 4;
			num7 = (uint)(num8 * 4);
			num5 = (uint)((pRZhx3Lmx8[num7 + 3] << 24) | (pRZhx3Lmx8[num7 + 2] << 16) | (pRZhx3Lmx8[num7 + 1] << 8) | pRZhx3Lmx8[num7]);
			if (i == num2 - 1 && num > 0)
			{
				num6 = 0u;
				uint num10 = 255u;
				int num11 = 0;
				for (int j = 0; j < num; j++)
				{
					if (j > 0)
					{
						num6 <<= 8;
					}
					num6 |= P_0[^(1 + j)];
				}
				num4 += num5;
				num4 += oIgFJXmRKv(num4);
				uint num12 = num4 ^ num6;
				for (int k = 0; k < num; k++)
				{
					if (k > 0)
					{
						num10 <<= 8;
						num11 += 8;
					}
					array[num9 + k] = (byte)((num12 & num10) >> num11);
				}
			}
			else
			{
				num7 = (uint)num9;
				num6 = (uint)((P_0[num7 + 3] << 24) | (P_0[num7 + 2] << 16) | (P_0[num7 + 1] << 8) | P_0[num7]);
				num4 += num5;
				num4 += oIgFJXmRKv(num4);
				uint num13 = num4 ^ num6;
				array[num9] = (byte)(num13 & 0xFF);
				array[num9 + 1] = (byte)((num13 & 0xFF00) >> 8);
				array[num9 + 2] = (byte)((num13 & 0xFF0000) >> 16);
				array[num9 + 3] = (byte)((num13 & 0xFF000000u) >> 24);
			}
		}
		return array;
	}

	private uint oIgFJXmRKv(uint P_0)
	{
		uint num = P_0;
		uint num2 = 973202305u;
		uint num3 = 1582787682u;
		uint num4 = 1577548636u;
		uint num5 = 332884210u;
		ulong num6 = num4 * 1313243236;
		num6 |= 1;
		num3 = (uint)(num3 * num3 % num6);
		ulong num7 = 1907532890 * num4;
		if (num7 == 0L)
		{
			num7--;
		}
		_ = 698203908u % num7;
		num2 = (uint)(-502326134 - num3);
		ulong num8 = num3 * 183835789;
		num8 |= 1;
		num4 = (uint)(num4 * num4 % num8);
		uint num9 = ((num5 >> 6) | (num5 << 26)) ^ num3;
		uint num10 = num9 & 0xF0F0F0F;
		num5 = ((num9 & 0xF0F0F0F0u) >> 4) | (num10 << 4);
		num ^= num << 3;
		num += num2;
		num ^= num << 11;
		num += num4;
		num ^= num >> 27;
		num += num5;
		return (((num2 << 11) - num3) ^ num4) - num;
	}

	internal static string AbmFTNZdVd(object P_0)
	{
		"agcqRKepaKDU6MA36aA".Trim();
		byte[] array = Convert.FromBase64String((string)P_0);
		return Encoding.Unicode.GetString(array, 0, array.Length);
	}

	private static byte[] b0iFQIK2Rb(object P_0)
	{
		return new DaN0wMF317528vSBGxX(new byte[32]
		{
			123, 5, 74, 12, 244, 156, 221, 154, 121, 221,
			183, 41, 121, 65, 9, 43, 67, 81, 23, 43,
			74, 63, 64, 23, 95, 185, 226, 244, 45, 194,
			211, 43
		}, new byte[16]
		{
			117, 254, 41, 121, 65, 52, 9, 43, 221, 154,
			12, 54, 68, 241, 68, 66
		}).zSHFBIkyRF((byte[])P_0);
	}

	private byte[] uoCFkq2BK8()
	{
		_ = "OCS4uIKlfSnpXDk6QXu6v".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}

	private byte[] J4aFYDuDwL()
	{
		_ = "QK2LKDBg5muaVD8n0CWWT".Length;
		_ = 0;
		return new byte[2] { 1, 2 };
	}
}
