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
using System.Linq;
using System.IO;

namespace IntelHex.Tests
{

    [TestClass()]
    public class ExtentionTests
    {
        [TestMethod()]
        public void SubArrayTest()
        {
            byte[] testdata = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            Assert.IsTrue(testdata.SubArray(0, 3).SequenceEqual(new byte[] { 1, 2, 3 }));

        }
    }

    [TestClass()]
    public class IntelHexRecordTests
    {
        [TestMethod()]
        public void EncodeDataLineTest()
        {
            Assert.AreEqual(IntelHexRecord.EncodeLine(eRecordType.Data, 0, new byte[] { 0x00, 0x6C, 0x07, 0x20, 0x95, 0xB8, 0x10, 0x08 }), ":08000000006C072095B8100800");
        }

        [TestMethod()]
        public void EncodeExtAddrLineTest()
        {
            Assert.AreEqual(IntelHexRecord.EncodeLine(eRecordType.ExtendedLinearAddress, 0, new byte[] { 0x08, 0x00 }), ":020000040800F2");
        }

        [TestMethod()]
        public void EncodeEndLineTest()
        {
            Assert.AreEqual(IntelHexRecord.EncodeLine(eRecordType.EndOfFile, 0, null), ":00000001FF");
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void EncodeDataLineOutofRangeTest()
        {
            IntelHexRecord.EncodeLine(eRecordType.Data, 0, new byte[256]);
        }

        [TestMethod()]
        public void IntelHexRecordConstructorTest()
        {
            string line = ":08000000006C072095B8100800";
            ushort expectedAddr = 0x0000;
            eRecordType expectedType = eRecordType.Data;
            byte[] expecteddata = new byte[] { 0x00, 0x6C, 0x07, 0x20, 0x95, 0xB8, 0x10, 0x08 };

            IntelHexRecord tr = new IntelHexRecord(line);

            Assert.AreEqual(expectedAddr, tr.Address);
            Assert.AreEqual(expectedType, tr.RecordType);
            Assert.IsTrue(expecteddata.SequenceEqual(tr.Bytes));
        }


        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void IntelHexRecordConstructorInvalidChecksumTest()
        {
            string line = ":08000000006C072095B8100801";

            IntelHexRecord tr = new IntelHexRecord(line);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void IntelHexRecordConstructorInvalidBegomTest()
        {
            string line = "08000000006C072095B8100801ABCDE";

            IntelHexRecord tr = new IntelHexRecord(line);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void IntelHexRecordConstructorInvalidLengthTest()
        {
            string line = ":09000000006C072095B8100801";

            IntelHexRecord tr = new IntelHexRecord(line);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void IntelHexRecordConstructorInvalidInputTest()
        {
            IntelHexRecord tr = new IntelHexRecord(null);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void IntelHexRecordConstructorInvalidShortLineTest()
        {
            IntelHexRecord tr = new IntelHexRecord(":09000000");
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void IntelHexRecordConstructorInvalidLengthOddTest()
        {
            string line = ":08000000006C072095B81008001";

            IntelHexRecord tr = new IntelHexRecord(line);
        }
    }

    [TestClass()]
    public class IntelHexFileTests
    {
        [TestMethod()]
        public void SaveAndLoadTest()
        {
            string filename = "test.hex";
            byte[] newdata = new byte[1025];
            BinaryBlock bb = new BinaryBlock(0x00010000 - 500);
            BinaryBlock bb1 = new BinaryBlock(0x00010000 - 50);

            bb.Append(newdata);
            bb1.Append(newdata);

            IntelHexFile.SaveAsIntelHex(filename, new BinaryBlock[] { bb, bb1 });

            IntelHexFile hf = new IntelHexFile();
            hf.Load(filename);

            Assert.AreEqual(2, hf.Blocks.Count);

            // Check binary black
            Assert.AreEqual(bb.Address, hf.Blocks[0].Address);
            Assert.AreEqual(bb.Length, hf.Blocks[0].Length);

            bb.UpdateHash(eHash.CRC32);
            Assert.AreEqual(bb.DisplayHash, hf.Blocks[0].DisplayHash);

            bb.UpdateHash(eHash.SHA256);
            Assert.AreNotEqual(bb.DisplayHash, hf.Blocks[0].DisplayHash);

            hf.UpdateHash(eHash.SHA256);
            Assert.AreEqual(bb.DisplayHash, hf.Blocks[0].DisplayHash);

            // append the same file again
            hf.Load(filename, eHash.CRC32, true);
            Assert.AreEqual(4, hf.Blocks.Count);
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception), "Unsupported line present",AllowDerivedTypes = true)]
        public void LoadNotSupportedTest()
        {
            string filename = "test.hex";

            File.WriteAllText(filename, IntelHexRecord.EncodeLine((eRecordType)8, 0x1000, null));

            IntelHexFile hf = new IntelHexFile();
            hf.Load(filename);
        }

        [TestMethod()]
        public void LoadIgnoredTest()
        {
            string filename = "test.hex";
            File.WriteAllText(filename, ":0400000300003800C1");


            IntelHexFile hf = new IntelHexFile();
            hf.Load(filename);
        }

        [TestMethod()]
        public void LoadIgnoredLinerTest()
        {
            string filename = "test.hex";
            File.WriteAllText(filename, ":04000005000000CD2A");


            IntelHexFile hf = new IntelHexFile();
            hf.Load(filename);
        }
    }
}