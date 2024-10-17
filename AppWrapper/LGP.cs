/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppWrapper {

    public class LGPEntry {

        public static int HashValues = 30;

        private static int Hash(char c) {
            if (Char.IsLetter(c)) return c - 'a';
            else if (Char.IsDigit(c)) return c - '0';
            else if (c == '_') return 'k' - 'a';
            else if (c == '-') return 'l' - 'a';
            throw new Exception("Invalid LGP filename character");
        }

        private static ushort CalculateHash(string name) {
            ushort hash = (ushort)(Hash(name[0]) * HashValues);
            if (name.Length > 1 && name[1] != '.')
                hash += (ushort)(Hash(name[1]) + 1);
            //DebugLogger.WriteLine("Hash for {name} is {hash}");
            return hash;
        }

        public LGPEntry(string name, int maxSize) {
            Name = name.ToLower();
            int spos = Name.LastIndexOf('\\');
            if (spos >= 0) {
                TOCName = Name.Substring(spos + 1);
                Path = Name.Substring(0, spos);
            } else {
                TOCName = Name;
                Path = String.Empty;
            }
            if (TOCName.Length > 20) throw new Exception(String.Format("LGP TOCName too long: {0}", name));
            if (Path.Length > 128) throw new Exception(String.Format("LGP Path too long: {0}", name));

            MaxSize = maxSize;
            HashCode = CalculateHash(TOCName);
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public string TOCName { get; set; }
        public int MaxSize { get; set; }
        public ushort HashCode { get; set; }
    }

    public static class LGP {

        private class TOC {
            public byte[] Name;
            public int Offset;
            public byte Type;
            public ushort Path;
            public LGPEntry Source;

            public TOC(string name) {
                Name = System.Text.Encoding.ASCII.GetBytes(name);
                Offset = 0;
                Type = 0;
                Path = 0;
                Source = null;
            }

        }

        private struct PathEntry {
            public string Path;
            public ushort Index;
        }

        public class LGPEntryMade {
            public LGPEntry Entry { get; set; }
            public int TOCIndex { get; set; }
            public int DataOffset { get; set; }
            public int DataIndex { get; set; }
        }

        public static byte[] CalculateHeaders(List<LGPEntry> files, Func<string, int> headerSortKey, Func<string, uint> dataSortKey, out List<LGPEntryMade> entries) {
            files = files.OrderBy(e => e.HashCode).ThenBy(e => headerSortKey(e.Name)).ToList();
            byte[] hash = new byte[LGPEntry.HashValues * LGPEntry.HashValues * 4];
            TOC[] toc = new TOC[files.Count];
            var hfiles = files.GroupBy(e => e.HashCode).OrderBy(g => g.Key);

            List<List<PathEntry>> paths = new List<List<PathEntry>>();
            entries = new List<LGPEntryMade>();

            int count = 0;
            //int position = 0;
            foreach (var group in hfiles) {
                Bytes.WriteUShort(hash, 4 * group.Key, (ushort)(count + 1));
                Bytes.WriteUShort(hash, 4 * group.Key + 2, (ushort)(group.Count()));

                foreach (var name in group.OfType<LGPEntry>().GroupBy(e => e.TOCName).OrderBy(g => headerSortKey(g.Key))) {
                    bool hasPaths = name.Count() > 1;
                    if (hasPaths) {
                        paths.Add(new List<PathEntry>());
                    }

                    foreach (var file in name) {
                        toc[count] = new TOC(file.TOCName);
                        toc[count].Type = 14;
                        toc[count].Path = (ushort)(hasPaths ? paths.Count : 0);
                        toc[count].Source = file;
                        //toc[count].Offset = position; //just use file sizes so far, we'll calculate the additional offset due to header later - once the file paths are done
                        //position += 24 + file.MaxSize;
                        if (hasPaths) {
                            paths.Last().Add(new PathEntry() { Index = (ushort)count, Path = file.Path });
                        }
                        entries.Add(new LGPEntryMade() { Entry = file, TOCIndex = count });
                        count++;
                    }
                }
            }

            int headerSize = 16 //initial header
                + (27 * toc.Length) //toc
                + hash.Length //hashtable
                + 2 //path count
                + paths.Select(L => 2 + L.Count * 130).Sum() //path entries - 2 byte count for each list, followed by 130 bytes per entry
                ;

            int position = 0;
            count = 0;
            foreach (var em in entries.OrderBy(em => dataSortKey(em.Entry.Name)).ThenBy(em => em.Entry.Name)) {
                em.DataOffset = toc[em.TOCIndex].Offset = position + headerSize;
                em.DataIndex = count;
                position += 24 + em.Entry.MaxSize;
                count++;
            }

            byte[] header = new byte[headerSize];
            //Initial header
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("SQUARESOFT"), 0, header, 2, 10);
            Bytes.WriteInt(header, 12, toc.Length);
            //Now write TOC
            int offset = 16;
            foreach (var entry in toc) {
                Array.Copy(entry.Name, 0, header, offset, entry.Name.Length);
                offset += 20;
                Bytes.WriteInt(header, offset, entry.Offset);
                offset += 4;
                header[offset] = entry.Type;
                offset++;
                Bytes.WriteUShort(header, offset, entry.Path);
                offset += 2;
            }
            //Now hash table
            Array.Copy(hash, 0, header, offset, hash.Length);
            offset += hash.Length;
            //Now path array...
            Bytes.WriteUShort(header, offset, (ushort)(paths.Count));
            offset += 2;
            foreach (var pathlist in paths) {
                Bytes.WriteUShort(header, offset, (ushort)(pathlist.Count));
                offset += 2;
                foreach (var path in pathlist) {
                    byte[] p = System.Text.Encoding.ASCII.GetBytes(path.Path);
                    Array.Copy(p, 0, header, offset, p.Length);
                    offset += 128;
                    Bytes.WriteUShort(header, offset, path.Index);
                    offset += 2;
                }
            }

            return header;
        }
    }
}
