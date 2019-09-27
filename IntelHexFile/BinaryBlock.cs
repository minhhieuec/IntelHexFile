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
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;


namespace IntelHex
{
    public enum eHash
    {
        CRC32,
        SHA256
    }

    public class BinaryBlock : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        #endregion

        #region Variables
        private readonly UInt32 startAddress;

        private string hashStr = "";
        private List<byte> dataStore = new List<byte>();
        #endregion

        #region Constructor
        public BinaryBlock(UInt32 start)
        {
            startAddress = start;
        }

        #endregion

        #region Non-Browsable Properties
        [Browsable(false)]
        public UInt32 Address
        {
            get
            {
                return startAddress;
            }
        }

        [Browsable(false)]
        public int Length
        {
            get
            {
                return GetBytes().Length;
            }
        }
        #endregion

        #region Brownsable Properties
        [DisplayName("Address")]
        public string DisplayAddress
        {
            get
            {
                return "0x" + startAddress.ToString("X8");
            }
        }

        [DisplayName("Length")]
        public string DisplayLength
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                string result = "";
                int len = GetBytes().Length;

                for (int i = sizes.Length - 1; i >= 0; i--)
                {
                    if (len >= Math.Pow(1024, i))
                    {

                        result += string.Format("{0} {1} ", len / (int)Math.Pow(1024, i), sizes[i]);
                        len = len % (int)Math.Pow(1024, i);
                    }
                }

                return result;
            }
        }

        [DisplayName("Hash")]
        public string DisplayHash
        {
            get
            {
                return hashStr;
            }
        }

        #endregion

        #region Method
        public void Append(byte[] data)
        {
            dataStore.AddRange(data);
        }

        public byte[] GetBytes()
        {
            return dataStore.ToArray();
        }

        public void UpdateHash(eHash hash)
        {
            switch(hash)
            {
                case eHash.CRC32:
                    using (Crc32 crc32 = new Crc32())
                    {
                        hashStr = BitConverter.ToString(crc32.ComputeHash(GetBytes())).Replace("-", "");
                    }
                    break;
                case eHash.SHA256:
                    using (SHA256 sha256Hash = SHA256.Create())
                    {
                        hashStr = BitConverter.ToString(sha256Hash.ComputeHash(GetBytes())).Replace("-", "");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            NotifyPropertyChanged("DisplayHash");
        }
        #endregion
    }
}
