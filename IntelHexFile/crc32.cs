/*
 * MIT License
 * 
 * Copyright(c) 2019 Ji Dong(ji.dong @hotmail.co.uk)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 **/

using System;
using System.Linq;
using System.Security.Cryptography;

namespace IntelHex
{
    public sealed class Crc32 : HashAlgorithm
    {
        private const UInt32 DefaultPoly = 0xEDB88320; // default reflected polynomal

        private readonly UInt32[] table;
        private UInt32 crc32;

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            for (int i = ibStart; i < ibStart + cbSize; i++)
                crc32 = table[(crc32 ^ array[i]) & 0xFF] ^ (crc32 >> 8);
        }
        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(~crc32).Reverse().ToArray();
        }

        public Crc32(UInt32 poly = DefaultPoly)
        {
            Initialize();

            table = new UInt32[256];

            for (UInt32 i = 0; i < 256; i++)
            {
                UInt32 crc = i;

                for (UInt32 j = 0; j < 8; j++)
                    crc = ((crc & 1) != 0) ? ((crc >> 1) ^ poly) : (crc >> 1);
                    
                table[i] = crc;
            }
        }

        public override void Initialize()
        {
            crc32 = 0xffffffff;
        }

        public override int HashSize
        {
            get
            {
                return 4;
            }
        }

        public UInt32 Compute(byte[] array, int ibStart, int cbSize)
        {
            Initialize();
            HashCore(array, ibStart, cbSize);
            return BitConverter.ToUInt32(HashFinal().Reverse().ToArray(), 0);
        }
    }
}
