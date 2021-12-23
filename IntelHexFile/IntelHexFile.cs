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
using System.IO;
using System.Linq;
using System.ComponentModel;

namespace IntelHex
{
    public enum eRecordType : byte
    {
        Data = 0x00,
        EndOfFile = 0x01,
        ExtendedSegmentAddress = 0x02,
        StartSegmentAddress = 0x03,
        ExtendedLinearAddress = 0x04,
        StartLinearAddress = 0x05
    }

    public static class Extention
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];

            Array.Copy(data, index, result, 0, length);

            return result;
        }
    }

    public class IntelHexRecord
    {
        private const string COLON = ":";

        #region Static, Line parser
        private static byte[] ParseLine(string line)
        {
            byte checksum = 0;
            byte[] res = new byte[line.Length / 2];

            for (int i = 0; i < res.Length; i++)
            {
                res[i] = Convert.ToByte(line.Substring(i * 2, 2), 16);
                checksum += res[i];
            }

            if (checksum != 0)
            {
                throw new Exception(string.Format("Line '{0} checksum is corrupted'!", line));
            }

            return res;
        }

        public static string EncodeLine(eRecordType recType, ushort addr, byte[] data)
        {
            string line = COLON;
            int datalen = (data != null) ? data.Length : 0;

            if (datalen >= 255)
                throw new Exception(string.Format("Invalid data length {0}", data.Length));

            byte checksum = (byte)datalen;
            line += ((byte)datalen).ToString("X2");

            // address
            byte b1 = (byte)(addr >> 8);
            byte b2 = (byte)(addr & 0xFF);

            checksum += b1;
            line += b1.ToString("X2");
            checksum += b2;
            line += b2.ToString("X2");

            checksum += (byte)recType;
            line += ((byte)recType).ToString("X2");

            for (int i = 0; i < datalen; i++)
            {
                checksum += data[i];
                line += data[i].ToString("X2");
            }

            checksum = (byte)(256 - checksum);
            line += checksum.ToString("X2");
            return line;
        }

        #endregion

        #region Constructor

        public IntelHexRecord(string line)
        {
            if (line == null)
                throw new Exception("Line to parse can not be null");

            // Line length must be even number
            if (line.Length % 2 == 0)
                throw new Exception(string.Format("Line '{0}' is incorrect length!", line));

            // A record minimual length is 11 char
            if (line.Length < 11)
                throw new Exception(string.Format("Line '{0}' is too short!", line));

            if (!line.StartsWith(COLON))
                throw new Exception(string.Format("Illegal line start character '{0}'!", line));

            /* Now Validate the line */
            byte[] linedata = ParseLine(line.Substring(1, line.Length - 1));

            Address = ((ushort)((((ushort)linedata[1]) << 8) | linedata[2]));
            RecordType = (eRecordType)linedata[3];
            Bytes = linedata.Skip(4).Take(linedata.Length - 5).ToArray();
        }

        #endregion

        #region Property
        public ushort Address
        {
            get; private set;

        }
        public eRecordType RecordType
        {
            get; private set;
        }
        public byte[] Bytes
        {
            get; private set;
        }

        #endregion
    }

    public class IntelHexFile
    {
        #region Variables

        private const int hexLineDataLen = 16;
        private BindingList<BinaryBlock> blocks = new BindingList<BinaryBlock>();

        #endregion

        #region Properties for gridview
        public BindingList<BinaryBlock> Blocks
        {
            get
            {
                return blocks;
            }
        }
        #endregion

        #region Method
        public void Load(string fileName, eHash hash = eHash.CRC32, bool append = false)
        {

            if (!append)
                blocks.Clear();

            using (StreamReader reader = new StreamReader(fileName))
            {
                UInt32 CurrentAddress = 0, LastAddress = 0xFFFFFFFF;
                BinaryBlock newblock = null;

                while (reader.Peek() > 0)
                {
                    IntelHexRecord record = new IntelHexRecord(reader.ReadLine());

                    //Console.WriteLine("len: " + record.Bytes.Length);
                    //for (tmp = 0; tmp < record.Bytes.Length; tmp++)
                    //{
                    //    Console.WriteLine(tmp + ": " + record.Bytes[tmp].ToString("X"));
                    //}

                    /* 
                     * I32HEX files use only record types 00, 01, 04, and 05 (32 bit addresses)
                     * 00 : Data
                     * 01 : End Of File 
                     * 04 : Extended Linear Address
                     * 05 : Start Linear Address 
                     */
                    switch (record.RecordType)
                    {
                        case eRecordType.ExtendedLinearAddress:
                            /*
                             * Allows for 32 bit addressing (up to 4GiB). 
                             * The address field is ignored (typically 0000) and the byte count is always 02. 
                             * The two encoded, big endian data bytes specify the upper 16 bits of the 32 bit absolute address for 
                             * all subsequent type 00 records; these upper address bits apply until the next 04 record. 
                             * If no type 04 record precedes a 00 record, the upper 16 address bits default to 0000. 
                             * The absolute address for a type 00 record is formed by combining the upper 16 address bits of 
                             * the most recent 04 record with the low 16 address bits of the 00 record. 
                             */
                            CurrentAddress = (((UInt32)record.Bytes[0]) << 24) | (((UInt32)record.Bytes[1]) << 16);
                            break;
                        case eRecordType.Data:
                            /*
                             *  Contains data and a 16-bit starting address for the data. 
                             *  The byte count specifies number of data bytes in the record. 
                             */
                            CurrentAddress = CurrentAddress & 0xFFFF0000 | (UInt32)record.Address;

                            if (LastAddress != CurrentAddress)
                            {
                                if (newblock != null)
                                    newblock.UpdateHash(hash);
                                newblock = new BinaryBlock(CurrentAddress);

                                int i = 0;
                                for (; i < blocks.Count; i++)
                                {
                                    if (blocks[i].Address > CurrentAddress)
                                        break;
                                }

                                blocks.Insert(i, newblock);
                            }

                            newblock.Append(record.Bytes);
                            LastAddress = CurrentAddress + (UInt32)record.Bytes.Length;

                            break;
                        case eRecordType.EndOfFile:
                            /*
                             * Must occur exactly once per file in the last line of the file.
                             * The data field is empty (thus byte count is 00) and the address field is typically 0000. 
                             */
                            if (newblock != null)
                                newblock.UpdateHash(hash);

                            return;
                        case eRecordType.StartLinearAddress:
                        case eRecordType.StartSegmentAddress:
                            /*
                             * The address field is 0000 (not used) and the byte count is 04. 
                             * The four data bytes represent the 32-bit value loaded into the EIP register of the 80386 and higher CPU.
                             * 
                             * We ignore the record
                             */
                            break;
                        default:
                            throw new NotSupportedException("Unsupported line present");
                    }
                }
            }
        }

        public void UpdateHash(eHash hash)
        {
            foreach (BinaryBlock b in blocks)
                b.UpdateHash(hash);
        }

        public static void SaveAsIntelHex(string fileName, BinaryBlock[] blocks)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (BinaryBlock b in blocks)
                {
                    byte[] data = b.GetBytes();

                    ushort high = (ushort)(b.Address >> 16);
                    ushort low = (ushort)(b.Address & 0xFFFF);

                    //writer.WriteLine(IntelHexRecord.EncodeLine(eRecordType.ExtendedLinearAddress, 0, new byte[] { (byte)(high >> 8), (byte)high}));
                    /* Align the data start addrss to hexLineDataLen bytes */
                    int datalinelen = hexLineDataLen - (int)b.Address % hexLineDataLen;

                    // force fixed to 16 for cmsemicon keil c intel hex file
                    if (b.Address == 43)
                    {
                        datalinelen = hexLineDataLen;
                    }
                    
                    writer.WriteLine(IntelHexRecord.EncodeLine(eRecordType.Data, low, data.SubArray(0, (datalinelen > data.Length)? data.Length : datalinelen)));
                    
                    for (int i = datalinelen; i < data.Length; )
                    {
                        /*Add last value*/
                        low += (ushort)datalinelen;

                        datalinelen = ((data.Length - i) > hexLineDataLen) ? hexLineDataLen : (data.Length - i);

                        //if (low == 0)
                        //{
                        //    high++;
                        //    writer.WriteLine(IntelHexRecord.EncodeLine(eRecordType.ExtendedLinearAddress, 0, new byte[] {(byte)(high >>8), (byte)high}));
                        //}

                        writer.WriteLine(IntelHexRecord.EncodeLine(eRecordType.Data, low, data.SubArray(i, datalinelen)));

                        i += datalinelen;
                    }
                }
                /* Append end of file line */
                writer.WriteLine(IntelHexRecord.EncodeLine(eRecordType.EndOfFile, 0, null));
            }
        }
        #endregion
    }
}
