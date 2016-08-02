namespace Universe.TinyGZip.InternalImplementation
{
    public sealed class Adler
    {
        private static readonly uint BASE = 65521U;
        private static readonly int NMAX = 5552;

        public static uint Adler32(uint adler, byte[] buf, int index, int len)
        {
            if (buf == null)
                return 1U;
            var num1 = adler & ushort.MaxValue;
            var num2 = adler >> 16 & ushort.MaxValue;
            while (len > 0)
            {
                var num3 = len < NMAX ? len : NMAX;
                len -= num3;
                while (num3 >= 16)
                {
                    var num4 = num1 + buf[index++];
                    var num5 = num2 + num4;
                    var num6 = num4 + buf[index++];
                    var num7 = num5 + num6;
                    var num8 = num6 + buf[index++];
                    var num9 = num7 + num8;
                    var num10 = num8 + buf[index++];
                    var num11 = num9 + num10;
                    var num12 = num10 + buf[index++];
                    var num13 = num11 + num12;
                    var num14 = num12 + buf[index++];
                    var num15 = num13 + num14;
                    var num16 = num14 + buf[index++];
                    var num17 = num15 + num16;
                    var num18 = num16 + buf[index++];
                    var num19 = num17 + num18;
                    var num20 = num18 + buf[index++];
                    var num21 = num19 + num20;
                    var num22 = num20 + buf[index++];
                    var num23 = num21 + num22;
                    var num24 = num22 + buf[index++];
                    var num25 = num23 + num24;
                    var num26 = num24 + buf[index++];
                    var num27 = num25 + num26;
                    var num28 = num26 + buf[index++];
                    var num29 = num27 + num28;
                    var num30 = num28 + buf[index++];
                    var num31 = num29 + num30;
                    var num32 = num30 + buf[index++];
                    var num33 = num31 + num32;
                    num1 = num32 + buf[index++];
                    num2 = num33 + num1;
                    num3 -= 16;
                }
                if (num3 != 0)
                {
                    do
                    {
                        num1 += buf[index++];
                        num2 += num1;
                    } while (--num3 != 0);
                }
                num1 %= BASE;
                num2 %= BASE;
            }
            return num2 << 16 | num1;
        }
    }
}