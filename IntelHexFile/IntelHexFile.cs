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
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

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

    #region micro defines

    public enum ID_EQ_Band
    {
        EQ_BAND1 = 0,
        EQ_BAND2,
        EQ_BAND3,
        EQ_BAND4,
        EQ_BAND5,
        EQ_BAND6,
        EQ_BAND7,
        EQ_BAND8,
        EQ_BAND9,
        EQ_BAND10,
        EQ_BAND11,
        EQ_BAND12,
        EQ_BAND13,
        EQ_BAND14,
        EQ_BAND15,
        MAX_EQ_BAND,
    }

    public struct Equalizer_Info_t
    {
        short [] freq;
        short [] gain;
        short [] q;
        short [] type;
    };
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Mic_System_Cfg_t
    {
        public byte cfg_mark;                           // mark start config package, default is 0xA5
        public byte num_freq;                           // total frequency, maximum 20 frequency band
        public ushort start_freq_disp_cha;              // start frequency
        public ushort start_freq_disp_chb;              // start frequency
        public ushort freq_inc_step;                    // increase step
        public byte num_eq_band;                        // number equalizer band using, maximum 10 bands (up to 15 band)
        // equalizer band 1
        public UInt32 coeff_b0_band1;
        public UInt32 coeff_b1_band1;
        public UInt32 coeff_b2_band1;
        public UInt32 coeff_a0_band1;
        public UInt32 coeff_a1_band1;
        // equalizer band 2
        public UInt32 coeff_b0_band2;
        public UInt32 coeff_b1_band2;
        public UInt32 coeff_b2_band2;
        public UInt32 coeff_a0_band2;
        public UInt32 coeff_a1_band2;
        // equalizer band 3
        public UInt32 coeff_b0_band3;
        public UInt32 coeff_b1_band3;
        public UInt32 coeff_b2_band3;
        public UInt32 coeff_a0_band3;
        public UInt32 coeff_a1_band3;
        // equalizer band 4
        public UInt32 coeff_b0_band4;
        public UInt32 coeff_b1_band4;
        public UInt32 coeff_b2_band4;
        public UInt32 coeff_a0_band4;
        public UInt32 coeff_a1_band4;
        // equalizer band 5
        public UInt32 coeff_b0_band5;
        public UInt32 coeff_b1_band5;
        public UInt32 coeff_b2_band5;
        public UInt32 coeff_a0_band5;
        public UInt32 coeff_a1_band5;
        // equalizer band 6
        public UInt32 coeff_b0_band6;
        public UInt32 coeff_b1_band6;
        public UInt32 coeff_b2_band6;
        public UInt32 coeff_a0_band6;
        public UInt32 coeff_a1_band6;
        // equalizer band 7
        public UInt32 coeff_b0_band7;
        public UInt32 coeff_b1_band7;
        public UInt32 coeff_b2_band7;
        public UInt32 coeff_a0_band7;
        public UInt32 coeff_a1_band7;
        // equalizer band 8
        public UInt32 coeff_b0_band8;
        public UInt32 coeff_b1_band8;
        public UInt32 coeff_b2_band8;
        public UInt32 coeff_a0_band8;
        public UInt32 coeff_a1_band8;

        public short checksum;                          // checksum crc16
        public byte[] getBytes(Mic_System_Cfg_t str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
    #endregion

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
        private byte[] all_micro_configs;
        
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

        #region mic receiver configs
        // Convert an object to a byte array
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }
        public ushort lsb_to_msb(ushort lsb)
        {
            ushort msb;

            msb = (ushort)((lsb & 0xFF) << 8 | (lsb >> 8));

            return msb;
        }

        public void get_all_mic_rx_configs()
        {
            const byte num_freq = 9;
            const ushort start_freq_disp_cha = 676;
            const ushort start_freq_disp_chb = 685;
            const ushort freq_inc_step = 1000;

            Mic_System_Cfg_t all_cfgs = new Mic_System_Cfg_t();

            all_cfgs.cfg_mark = 0xA5;
            all_cfgs.num_freq = num_freq;
            all_cfgs.start_freq_disp_cha = lsb_to_msb(start_freq_disp_cha);
            all_cfgs.start_freq_disp_chb = lsb_to_msb(start_freq_disp_chb);
            all_cfgs.freq_inc_step = lsb_to_msb(freq_inc_step);

            byte[] tmp_arr = all_cfgs.getBytes(all_cfgs);
            Console.WriteLine("len: " + tmp_arr.Length);
            for (int cnt = 0; cnt < tmp_arr.Length; cnt++)
            {
                Console.WriteLine("0x" + tmp_arr[cnt].ToString("X"));
            }

            //return all_cfgs.ToArray();
        }
        #endregion

        #region Method
        public void Load(string fileName, eHash hash = eHash.CRC32, bool append = false)
        {

            if (!append)
                blocks.Clear();

            //byte[] mic_configs = 
                get_all_mic_rx_configs();
            //Console.WriteLine("cfg len: " + mic_configs.Length);
            //for (int cnt = 0; cnt < mic_configs.Length; cnt++)
            //{
            //    Console.WriteLine(mic_configs[cnt]);
            //}

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
                            if (CurrentAddress == 15872)
                            {
                                Console.WriteLine("set config");
                                record.Bytes[0] = 0xA5;
                                record.Bytes[1] = 0xB5;
                                record.Bytes[2] = 0xC5;
                                record.Bytes[3] = 0xD5;
                                record.Bytes[4] = 0xE5;
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
