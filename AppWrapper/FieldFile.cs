/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppWrapper {
    public static class FieldFile {

        public static List<byte[]> Unchunk(byte[] input) {
            var ms = new System.IO.MemoryStream();
            var min = new System.IO.MemoryStream(input);
            min.Position = 4;
            Lzs.Decode(min, ms);
            DebugLogger.WriteLine($"FF:Unchunk:LZS expanded {input.Length} bytes to {ms.Length} bytes");
            byte[] scratch = new byte[4];
            ms.Position = 2;
            ms.Read(scratch, 0, 4);
            int numsection = BitConverter.ToInt32(scratch, 0);
            DebugLogger.WriteLine($"FF:Unchunk:{numsection} sections");
            List<byte[]> sections = new List<byte[]>();
            foreach (int i in Enumerable.Range(0, numsection)) {
                ms.Position = 6 + i * 4;
                ms.Read(scratch, 0, 4);
                ms.Position = BitConverter.ToInt32(scratch, 0);
                ms.Read(scratch, 0, 4);
                int len = BitConverter.ToInt32(scratch, 0);
                byte[] s = new byte[len];
                ms.Read(s, 0, len);
                sections.Add(s);
            }
            return sections;
        }

        public static byte[] Chunk(List<byte[]> input) {
            var ms = new System.IO.MemoryStream();
            byte[] scratch = new byte[4];
            ms.Write(scratch, 0, 2);
            ms.Write(BitConverter.GetBytes(input.Count), 0, 4);
            int offset = 0x2A;
            foreach (var s in input) {
                Bytes.WriteInt(ms, offset);
                offset += 4;
                offset += s.Length;
            }
            foreach (var s in input) {
                Bytes.WriteInt(ms, s.Length);
                ms.Write(s, 0, s.Length);
            }
            ms.Position = 0;
            var compress = new System.IO.MemoryStream();
            Lzs.Encode(ms, compress);
            byte[] data = new byte[compress.Length + 4];
            compress.Position = 0;
            compress.Read(data, 4, (int)compress.Length);
            Bytes.WriteInt(data, 0, (int)compress.Length);
            return data;
        }
    }
}
