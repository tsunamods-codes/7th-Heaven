/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppWrapper {
    static class Bytes {
        public static void WriteInt(byte[] data, int offset, int value) {
            data[offset + 0] = (byte)(value & 0xff);
            data[offset + 1] = (byte)((value >> 8) & 0xff);
            data[offset + 2] = (byte)((value >> 16) & 0xff);
            data[offset + 3] = (byte)((value >> 24) & 0xff);
        }
        public static void WriteUInt(byte[] data, int offset, uint value) {
            data[offset + 0] = (byte)(value & 0xff);
            data[offset + 1] = (byte)((value >> 8) & 0xff);
            data[offset + 2] = (byte)((value >> 16) & 0xff);
            data[offset + 3] = (byte)((value >> 24) & 0xff);
        }
        public static void WriteUShort(byte[] data, int offset, ushort value) {
            data[offset + 0] = (byte)(value & 0xff);
            data[offset + 1] = (byte)((value >> 8) & 0xff);
        }
        public static long ReadLong(this System.IO.Stream s) {
            byte[] data = new byte[8];
            s.ReadExactly(data, 0, 8);
            return BitConverter.ToInt64(data, 0);
        }
        public static int ReadInt(this System.IO.Stream s) {
            byte[] data = new byte[4];
            s.ReadExactly(data, 0, 4);
            return BitConverter.ToInt32(data, 0);
        }
        public static ushort ReadUShort(this System.IO.Stream s) {
            byte[] data = new byte[2];
            s.ReadExactly(data, 0, 2);
            return BitConverter.ToUInt16(data, 0);
        }

        public static void WriteInt(this System.IO.Stream s, int i) {
            var data = BitConverter.GetBytes(i);
            s.Write(data, 0, 4);
        }
        public static void WriteLong(this System.IO.Stream s, long L) {
            var data = BitConverter.GetBytes(L);
            s.Write(data, 0, 8);
        }
        public static void WriteUInt(this System.IO.Stream s, uint i) {
            var data = BitConverter.GetBytes(i);
            s.Write(data, 0, 4);
        }
        public static void WriteUShort(this System.IO.Stream s, ushort us) {
            var data = BitConverter.GetBytes(us);
            s.Write(data, 0, 2);
        }
    }
}
