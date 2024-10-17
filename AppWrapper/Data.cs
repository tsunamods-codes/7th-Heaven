/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProcMonParser {
    public class DataItem {
        public string Name { get; set; }
        public long Start { get; set; }
        public int Length { get; set; }
        public int Index { get; set; }
    }

    public class DataFile {
        public List<DataItem> Items { get; set; }
        public string Filename { get; set; }

        public DataFile() {
            Items = new List<DataItem>();
        }

        public void Freeze() {
            Items = Items.OrderBy(i => i.Start).ToList();
        }

        public DataItem Get(int offset) {
            if (offset < Items[0].Start) return null;
            int min = 0, max = Items.Count - 1;
            while (min <= max) {
                int check = (min + max + 1) / 2;
                if (offset < Items[check].Start)
                    max = check - 1;
                else if (offset >= (Items[check].Start + Items[check].Length))
                    min = check + 1;
                else {
                    return Items[check];
                }
            }

            if (min > 0)
            {
                AppWrapper.DebugLogger.WriteLine("Mid read from " + Filename + " at offset " + offset);
            }

            return null;
        }
    }

    public static class FF7Files {
        public static int ReadInt(this System.IO.Stream s) {
            byte[] data = new byte[4];
            s.Read(data, 0, 4);
            return BitConverter.ToInt32(data, 0);
        }
        public static uint ReadUInt(this System.IO.Stream s) {
            byte[] data = new byte[4];
            s.Read(data, 0, 4);
            return BitConverter.ToUInt32(data, 0);
        }
        public static ushort ReadUShort(this System.IO.Stream s) {
            byte[] data = new byte[2];
            s.Read(data, 0, 2);
            return BitConverter.ToUInt16(data, 0);
        }

        public static DataFile LoadSounds(string ff7folder) {
            int count = 0;
            DataFile df = new DataFile() { Filename = System.IO.Path.Combine(ff7folder, "sound\\audio.dat") };
            using (var fmtFile = new System.IO.FileStream(System.IO.Path.Combine(ff7folder, "sound\\audio.fmt"), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)) {
                while (fmtFile.Position < fmtFile.Length) {
                    int length = fmtFile.ReadInt();
                    if (length == 0) {
                        fmtFile.Seek(38, System.IO.SeekOrigin.Current);
                        continue;
                    }
                    df.Items.Add(new DataItem() { Name = "SOUND" + (count++), Length = length, Start = fmtFile.ReadInt() });
                    fmtFile.Seek(66, System.IO.SeekOrigin.Current);
                }
            }
            return df;
        }

        public static DataFile LoadLGP(System.IO.Stream fs, string file) {
            DataFile df = new DataFile() { Filename = file };

            if (fs.ReadUShort() != 0)
                throw new Exception(file + " - not a valid LGP archive");
            byte[] header = new byte[10];
            fs.Read(header, 0, 10);
            if (!Encoding.ASCII.GetString(header).Equals("SQUARESOFT"))
                throw new Exception(file + " - not a valid LGP archive");
            int count = fs.ReadInt();
            byte[] fname = new byte[20];
            List<Tuple<string, uint, ushort, int>> files = new List<Tuple<string, uint, ushort, int>>();
            for (int i = 0; i < count; i++) {
                fs.Read(fname, 0, 20);
                uint offset = fs.ReadUInt();
                fs.ReadByte();
                ushort dupe = fs.ReadUShort();
                string lgpFile = Encoding.ASCII.GetString(fname);
                int nPos = lgpFile.IndexOf('\0');
                if (nPos >= 0) lgpFile = lgpFile.Substring(0, nPos);
                files.Add(new Tuple<string, uint, ushort, int>(lgpFile, offset, dupe, i));
            }
            if (files.Any(t => t.Item3 != 0)) {
                fs.Seek(3600, System.IO.SeekOrigin.Current); //skip lookup table
                ushort entries = fs.ReadUShort();
                byte[] pname = new byte[128];
                foreach (int i in Enumerable.Range(0, entries)) {
                    foreach (int j in Enumerable.Range(0, fs.ReadUShort())) {
                        fs.Read(pname, 0, 128);
                        ushort toc = fs.ReadUShort();
                        string ppname = Encoding.ASCII.GetString(pname);
                        ppname = ppname.Substring(0, ppname.IndexOf('\0'));
                        files[toc] = new Tuple<string, uint, ushort, int>(
                            ppname.Replace("/", "\\") + "\\" + files[toc].Item1,
                            files[toc].Item2,
                            files[toc].Item3,
                            files[toc].Item4
                        );
                    }
                }
            }
            df.Items = files.Select(t => new DataItem() { Name = t.Item1, Start = t.Item2, Index = t.Item4 }).ToList();
            foreach (var item in df.Items) {
                fs.Position = item.Start + 20;
                item.Length = fs.ReadInt() + 24;
                //item.Start = item.Start;
            }
            df.Freeze();
            return df;
        }

        public static DataFile LoadLGP(string file) {
            using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
                return LoadLGP(fs, file);
            }
        }

    }
}
