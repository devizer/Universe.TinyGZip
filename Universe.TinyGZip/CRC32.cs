// Decompiled with JetBrains decompiler
// Type: Ionic.Crc.CRC32
// Assembly: ZGip.Mini, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 786D35AB-EB80-4211-A8FB-6C01A67110FB
// Assembly location: V:\NoVCS\DotNetZip-mini\ZGip.Mini\ZGip.Mini\bin\Debug\ZGip.Mini.dll

namespace Universe.TinyGZip.InternalImplementation
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    [ComVisible(true)]
    [Guid("ebc25cf6-9120-4283-b972-0e5520d0000C")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class CRC32
    {
        private const int BUFFER_SIZE = 8192;
        private uint _register = uint.MaxValue;
        private uint[] crc32Table;
        private readonly uint dwPolynomial;
        private readonly bool reverseBits;

        public CRC32()
            : this(false)
        {
        }

        public CRC32(bool reverseBits)
            : this(-306674912, reverseBits)
        {
        }

        public CRC32(int polynomial, bool reverseBits)
        {
            this.reverseBits = reverseBits;
            dwPolynomial = (uint) polynomial;
            GenerateLookupTable();
        }

        public long TotalBytesRead { get; private set; }

        public int Crc32Result
        {
            get { return ~(int) _register; }
        }

        public int GetCrc32(Stream input)
        {
            return GetCrc32AndCopy(input, null);
        }

        public int GetCrc32AndCopy(Stream input, Stream output)
        {
            if (input == null)
                throw new Exception("The input stream must not be null.");
            var numArray = new byte[8192];
            var count1 = 8192;
            TotalBytesRead = 0L;
            var count2 = input.Read(numArray, 0, count1);
            if (output != null)
                output.Write(numArray, 0, count2);
            TotalBytesRead += count2;
            while (count2 > 0)
            {
                SlurpBlock(numArray, 0, count2);
                count2 = input.Read(numArray, 0, count1);
                if (output != null)
                    output.Write(numArray, 0, count2);
                TotalBytesRead += count2;
            }
            return ~(int) _register;
        }

        public int ComputeCrc32(int W, byte B)
        {
            return _InternalComputeCrc32((uint) W, B);
        }

        internal int _InternalComputeCrc32(uint W, byte B)
        {
            return (int) (crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
        }

        public void SlurpBlock(byte[] block, int offset, int count)
        {
            if (block == null)
                throw new Exception("The data buffer must not be null.");

            // bzip algorithm
            for (var i = 0; i < count; i++)
            {
                var x = offset + i;
                var b = block[x];
                if (reverseBits)
                {
                    var temp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ crc32Table[temp];
                }
                else
                {
                    var temp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ crc32Table[temp];
                }
            }
            TotalBytesRead += count;
        }

        public void UpdateCRC(byte b)
        {
            if (reverseBits)
            {
                var temp = (_register >> 24) ^ b;
                _register = (_register << 8) ^ crc32Table[temp];
            }
            else
            {
                var temp = (_register & 0x000000FF) ^ b;
                _register = (_register >> 8) ^ crc32Table[temp];
            }
        }

        public void UpdateCRC(byte b, int n)
        {
            while (n-- > 0)
            {
                if (reverseBits)
                {
                    var temp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ crc32Table[(temp >= 0)
                        ? temp
                        : (temp + 256)];
                }
                else
                {
                    var temp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ crc32Table[(temp >= 0)
                        ? temp
                        : (temp + 256)];
                }
            }
        }

        private static uint ReverseBits(uint data)
        {
            var num1 = data;
            var num2 = (uint) (((int) num1 & 1431655765) << 1 | (int) (num1 >> 1) & 1431655765);
            var num3 = (uint) (((int) num2 & 858993459) << 2 | (int) (num2 >> 2) & 858993459);
            var num4 = (uint) (((int) num3 & 252645135) << 4 | (int) (num3 >> 4) & 252645135);
            return (uint) ((int) num4 << 24 | ((int) num4 & 65280) << 8 | (int) (num4 >> 8) & 65280) | num4 >> 24;
        }

        private static byte ReverseBits(byte data)
        {
            var num1 = data*131586U;
            var num2 = 17055760U;
            return (byte) ((uint) (16781313*((int) (num1 & num2) + ((int) num1 << 2 & (int) num2 << 1))) >> 24);
        }

        private void GenerateLookupTable()
        {
            crc32Table = new uint[256];
            byte data1 = 0;
            do
            {
                uint data2 = data1;
                for (byte index = 8; (int) index > 0; --index)
                {
                    if (((int) data2 & 1) == 1)
                        data2 = data2 >> 1 ^ dwPolynomial;
                    else
                        data2 >>= 1;
                }
                if (reverseBits)
                    crc32Table[ReverseBits(data1)] = ReverseBits(data2);
                else
                    crc32Table[data1] = data2;
                ++data1;
            } while (data1 != 0);
        }

        private uint gf2_matrix_times(uint[] matrix, uint vec)
        {
            var num = 0U;
            var index = 0;
            while ((int) vec != 0)
            {
                if (((int) vec & 1) == 1)
                    num ^= matrix[index];
                vec >>= 1;
                ++index;
            }
            return num;
        }

        private void gf2_matrix_square(uint[] square, uint[] mat)
        {
            for (var index = 0; index < 32; ++index)
                square[index] = gf2_matrix_times(mat, mat[index]);
        }

        public void Combine(int crc, int length)
        {
            var numArray1 = new uint[32];
            var numArray2 = new uint[32];
            if (length == 0)
                return;
            var vec = ~_register;
            var num1 = (uint) crc;
            numArray2[0] = dwPolynomial;
            var num2 = 1U;
            for (var index = 1; index < 32; ++index)
            {
                numArray2[index] = num2;
                num2 <<= 1;
            }
            gf2_matrix_square(numArray1, numArray2);
            gf2_matrix_square(numArray2, numArray1);
            var num3 = (uint) length;
            do
            {
                gf2_matrix_square(numArray1, numArray2);
                if (((int) num3 & 1) == 1)
                    vec = gf2_matrix_times(numArray1, vec);
                var num4 = num3 >> 1;
                if ((int) num4 != 0)
                {
                    gf2_matrix_square(numArray2, numArray1);
                    if (((int) num4 & 1) == 1)
                        vec = gf2_matrix_times(numArray2, vec);
                    num3 = num4 >> 1;
                }
                else
                    break;
            } while ((int) num3 != 0);
            _register = ~(vec ^ num1);
        }

        public void Reset()
        {
            _register = uint.MaxValue;
        }
    }
}