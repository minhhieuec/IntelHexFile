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
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace IntelHex.Tests
{
    [TestClass()]
    public class BinaryBlockTests
    {
        [TestMethod()]
        public void BinaryBlockConstructorTest()
        {
            UInt32 startadd = 0U;
            BinaryBlock bb = new BinaryBlock(startadd);

            Assert.AreEqual(bb.Address, startadd);
            Assert.AreEqual(bb.Length, 0);
        }

        [TestMethod()]
        public void AppendAndGetBytesTest()
        {
            UInt32 startadd = 0x1000U;
            byte[] newdata = new byte[] { 0x00, 0x6C, 0x07, 0x20, 0x95, 0xB8, 0x10, 0x08 };
            BinaryBlock bb = new BinaryBlock(startadd);
            bb.Append(newdata);

            Assert.AreEqual(bb.Address, startadd);
            Assert.AreEqual(bb.Length, newdata.Length);
            Assert.IsTrue(newdata.SequenceEqual(bb.GetBytes()));

            Assert.AreEqual(bb.DisplayAddress, "0x" + startadd.ToString("X8"));
            Assert.AreEqual(bb.DisplayLength, string.Format("{0} B ", bb.Length));
        }

        [TestMethod()]
        public void UpdateHashTest()
        {
            List<string> receivedEvents = new List<string>();
            UInt32 startadd = 0x1000U;
            byte[] newdata = new byte[] { 0x00, 0x6C, 0x07, 0x20, 0x95, 0xB8, 0x10, 0x08 };
            string expectedCrc32 = "F03AC219";
            string expectedSha256 = "57F92927468C9B30CDA76613BE1A2322B93FE237B26FE6C882F2F97837B90CF3";

            BinaryBlock bb = new BinaryBlock(startadd);
            
            bb.PropertyChanged +=delegate(object sender, PropertyChangedEventArgs e)
            {
                receivedEvents.Add(e.PropertyName);
            };

            bb.Append(newdata);
            Assert.AreEqual(0, receivedEvents.Count);

            bb.UpdateHash(eHash.CRC32);
            Assert.AreEqual(bb.DisplayHash, expectedCrc32);
            Assert.AreEqual(1, receivedEvents.Count);
            Assert.AreEqual("DisplayHash", receivedEvents[0]);

            bb.UpdateHash(eHash.SHA256);
            Assert.AreEqual(bb.DisplayHash, expectedSha256);
            Assert.AreEqual(2, receivedEvents.Count);
            Assert.AreEqual("DisplayHash", receivedEvents[1]);
        }

        [TestMethod()]
        [ExpectedException(typeof(NotImplementedException), null)]
        public void UpdateHashNotSupportedTest()
        {
            List<string> receivedEvents = new List<string>();
            UInt32 startadd = 0x1000U;

            BinaryBlock bb = new BinaryBlock(startadd);

            bb.UpdateHash((eHash)byte.MaxValue);
        }
    }
}