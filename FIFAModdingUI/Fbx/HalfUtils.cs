using System.Runtime.InteropServices;

namespace FrostbiteModdingUI.Fbx
{
	public class HalfUtils
	{
		[StructLayout(LayoutKind.Explicit)]
		private struct FloatToUint
		{
			[FieldOffset(0)]
			public uint uintValue;

			[FieldOffset(0)]
			public float floatValue;
		}

		private static readonly uint[] HalfToFloatMantissaTable;

		private static readonly uint[] HalfToFloatExponentTable;

		private static readonly uint[] HalfToFloatOffsetTable;

		private static readonly ushort[] FloatToHalfBaseTable;

		private static readonly byte[] FloatToHalfShiftTable;

		public static float Unpack(ushort h)
		{
			FloatToUint floatToUint = default(FloatToUint);
			floatToUint.uintValue = HalfToFloatMantissaTable[(int)HalfToFloatOffsetTable[h >> 10] + (h & 0x3FF)] + HalfToFloatExponentTable[h >> 10];
			return floatToUint.floatValue;
		}

		public static ushort Pack(float f)
		{
			FloatToUint floatToUint = default(FloatToUint);
			floatToUint.floatValue = f;
			return (ushort)(FloatToHalfBaseTable[(floatToUint.uintValue >> 23) & 0x1FF] + ((floatToUint.uintValue & 0x7FFFFF) >> (int)FloatToHalfShiftTable[(floatToUint.uintValue >> 23) & 0x1FF]));
		}

		static HalfUtils()
		{
			HalfToFloatMantissaTable = new uint[2048];
			HalfToFloatExponentTable = new uint[64];
			HalfToFloatOffsetTable = new uint[64];
			FloatToHalfBaseTable = new ushort[512];
			FloatToHalfShiftTable = new byte[512];
			HalfToFloatMantissaTable[0] = 0u;
			for (int i = 1; i < 1024; i++)
			{
				uint num = (uint)(i << 13);
				uint num2 = 0u;
				while ((num & 0x800000) == 0)
				{
					num2 -= 8388608;
					num <<= 1;
				}
				num = (uint)((int)num & -8388609);
				num2 += 947912704;
				HalfToFloatMantissaTable[i] = (num | num2);
			}
			for (int i = 1024; i < 2048; i++)
			{
				HalfToFloatMantissaTable[i] = (uint)(939524096 + (i - 1024 << 13));
			}
			HalfToFloatExponentTable[0] = 0u;
			for (int i = 1; i < 63; i++)
			{
				if (i < 31)
				{
					HalfToFloatExponentTable[i] = (uint)(i << 23);
				}
				else
				{
					HalfToFloatExponentTable[i] = (uint)(int.MinValue + (i - 32 << 23));
				}
			}
			HalfToFloatExponentTable[31] = 1199570944u;
			HalfToFloatExponentTable[32] = 2147483648u;
			HalfToFloatExponentTable[63] = 3347054592u;
			HalfToFloatOffsetTable[0] = 0u;
			for (int i = 1; i < 64; i++)
			{
				HalfToFloatOffsetTable[i] = 1024u;
			}
			HalfToFloatOffsetTable[32] = 0u;
			for (int i = 0; i < 256; i++)
			{
				int num3 = i - 127;
				if (num3 < -24)
				{
					FloatToHalfBaseTable[i | 0] = 0;
					FloatToHalfBaseTable[i | 0x100] = 32768;
					FloatToHalfShiftTable[i | 0] = 24;
					FloatToHalfShiftTable[i | 0x100] = 24;
				}
				else if (num3 < -14)
				{
					FloatToHalfBaseTable[i | 0] = (ushort)(1024 >> -num3 - 14);
					FloatToHalfBaseTable[i | 0x100] = (ushort)((1024 >> -num3 - 14) | 0x8000);
					FloatToHalfShiftTable[i | 0] = (byte)(-num3 - 1);
					FloatToHalfShiftTable[i | 0x100] = (byte)(-num3 - 1);
				}
				else if (num3 <= 15)
				{
					FloatToHalfBaseTable[i | 0] = (ushort)(num3 + 15 << 10);
					FloatToHalfBaseTable[i | 0x100] = (ushort)((num3 + 15 << 10) | 0x8000);
					FloatToHalfShiftTable[i | 0] = 13;
					FloatToHalfShiftTable[i | 0x100] = 13;
				}
				else if (num3 < 128)
				{
					FloatToHalfBaseTable[i | 0] = 31744;
					FloatToHalfBaseTable[i | 0x100] = 64512;
					FloatToHalfShiftTable[i | 0] = 24;
					FloatToHalfShiftTable[i | 0x100] = 24;
				}
				else
				{
					FloatToHalfBaseTable[i | 0] = 31744;
					FloatToHalfBaseTable[i | 0x100] = 64512;
					FloatToHalfShiftTable[i | 0] = 13;
					FloatToHalfShiftTable[i | 0x100] = 13;
				}
			}
		}
	}
}
