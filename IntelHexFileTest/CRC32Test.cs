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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

using IntelHex;

namespace IntelHex.Tests
{
    [TestClass()]
    public class CRC32Test
    {
        private Tuple<string, Nullable<UInt32>>[] tests =
        {
            new Tuple<string, UInt32?>("1", 0x83DCEFB7),
            new Tuple<string, UInt32?>("12", 0x4F5344CD),
            new Tuple<string, UInt32?>("123", 0x884863D2),
            new Tuple<string, UInt32?>("1234", 0x9BE3E0A3),
            new Tuple<string, UInt32?>("12345", 0xCBF53A1C),

        };

        [TestMethod()]
        public void ComputeTest()
        {
            using (Crc32 crc32 = new Crc32())
            {
                foreach (Tuple<string, UInt32?> t in tests)
                {
                    byte[] data = Encoding.ASCII.GetBytes(t.Item1);

                    var value = crc32.Compute(data, 0, data.Length);

                    Assert.AreEqual(t.Item2, value);
                }
            }
        }

        [TestMethod()]
        public void Crc32Test()
        {
            using (Crc32 crc32 = new Crc32())
            {
                Assert.AreEqual(crc32.HashSize, 4);
            }
        }
    }
}
