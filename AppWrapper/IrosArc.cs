/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using Iros;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AppWrapper {

    public class IrosArcException : Exception {
        public IrosArcException(string msg) : base(msg) { }
    }

    [Flags]
    public enum ArchiveFlags {
        None = 0,
        Patch = 0x1,
    }

    [Flags]
    public enum FileFlags {
        None = 0,
        CompressLZS = 0x1,
        CompressLZMA = 0x2,

        COMPRESSION_FLAGS = 0xF,

#if RUDE
        Obfuscate = 0x10000,
#else
        RudeFlags = 0xff0000,
#endif
    }

    public enum CompressType {
        Nothing = 0,
        Everything,
        ByExtension,
        ByContent,
    }

    public class IrosArc : IDisposable {
        public const int SIG = 0x534f5249;
        public const int MAX_VERSION = 0x10002;
        public const int MIN_VERSION = 0x10000;

        internal const int SZ_OK =0;
        internal const int SZ_ERROR_DATA =1;
        internal const int SZ_ERROR_MEM =2;
        internal const int SZ_ERROR_CRC =3;
        internal const int SZ_ERROR_UNSUPPORTED =4;
        internal const int SZ_ERROR_PARAM =5;
        internal const int SZ_ERROR_INPUT_EOF =6;
        internal const int SZ_ERROR_OUTPUT_EOF =7;

        private static HashSet<string> _noCompressExt = new HashSet<string>(new[] { 
            ".jpg", ".png", ".mp3", ".ogg"
        }, StringComparer.InvariantCultureIgnoreCase);

        private class ArcHeader {

            public int Version { get; set; }
            public ArchiveFlags Flags { get; set; }
            public int Directory { get; set; }

            public void Open(System.IO.Stream s) {
                if (s.ReadInt() != SIG) throw new IrosArcException("Signature mismatch");
                Version = s.ReadInt();
                Flags = (ArchiveFlags)s.ReadInt();
                Directory = s.ReadInt();
                if (Version < MIN_VERSION) throw new IrosArcException("Invalid header version " + Version.ToString());
                if (Version > MAX_VERSION) throw new IrosArcException("Invalid header version " + Version.ToString());
            }

            public void Save(System.IO.Stream s) {
                s.WriteInt(SIG);
                s.WriteInt(Version);
                s.WriteInt((int)Flags);
                s.WriteInt(Directory);
            }

            public override string ToString() {
                return String.Format("Version: {0}.{1}  Directory at: {2}  Flags: {3}", Version >> 16, Version & 0xffff, Directory, Flags);
            }

        }

        private class DirectoryEntry {
            public string Filename { get; set; }
            public FileFlags Flags { get; set; }
            public long Offset { get; set; }
            public int Length { get; set; }

            public void Open(System.IO.Stream s, int version) {
                long pos = s.Position;
                ushort len = s.ReadUShort();
                ushort flen = s.ReadUShort();
                byte[] fn = new byte[flen];
                s.Read(fn, 0, flen);
                Filename = System.Text.Encoding.Unicode.GetString(fn);
                Flags = (FileFlags)s.ReadInt();
                if (version < 0x10001)
                    Offset = s.ReadInt();
                else
                    Offset = s.ReadLong();
                Length = s.ReadInt();
                s.Position = pos + len;
            }

            public ushort GetSize() {
                byte[] fndata = System.Text.Encoding.Unicode.GetBytes(Filename);
                ushort len = (ushort)(fndata.Length + 4 + 16);
                return len;
            }

            public void Save(System.IO.Stream s) {
                byte[] fndata = System.Text.Encoding.Unicode.GetBytes(Filename);
                ushort len = (ushort)(fndata.Length + 4 + 16);
                s.WriteUShort(len);
                s.WriteUShort((ushort)fndata.Length);
                s.Write(fndata, 0, fndata.Length);
                s.WriteInt((int)Flags);
                s.WriteLong(Offset);
                s.WriteInt(Length);
            }

            public override string ToString() {
                return String.Format("File: {0} Offset: {1} Size: {2} Flags: {3}", Filename, Offset, Length, Flags);
            }
        }

        private ArcHeader _header;
        private List<DirectoryEntry> _entries;
        private Dictionary<string, DirectoryEntry> _lookup;
        private HashSet<string> _folderNames;
        private System.IO.FileStream _data;
        private string _source;

        private class CacheEntry {
            public byte[] Data;
            public DateTime LastAccess;
            public string File;
        }

        private System.Collections.Concurrent.ConcurrentDictionary<long, CacheEntry> _cache = new System.Collections.Concurrent.ConcurrentDictionary<long, CacheEntry>();

        private struct DataRecord {
            public byte[] Data;
            public bool Compressed;
        }
        private static DataRecord GetData(byte[] input, string filename, CompressType compress) {
            if (compress == CompressType.Nothing) {
                    return new DataRecord() { Data = input };
            }
            if (compress == CompressType.ByExtension && _noCompressExt.Contains(System.IO.Path.GetExtension(filename))) {
                return new DataRecord() { Data = input };
            }

            var cdata = new System.IO.MemoryStream();
            //Lzs.Encode(new System.IO.MemoryStream(input), cdata);
            byte[] lprops;
            using (var lzma = new SharpCompress.Compressors.LZMA.LzmaStream(new SharpCompress.Compressors.LZMA.LzmaEncoderProperties(), false, cdata)) {
                lzma.Write(input, 0, input.Length);
                lprops = lzma.Properties;
            }
            if (/*compress == CompressType.ByContent &&*/ (cdata.Length + lprops.Length + 8) > (input.Length * 10 / 8)) {
                return new DataRecord() { Data = input };
            }

            byte[] data = new byte[cdata.Length + lprops.Length + 8];
            Array.Copy(BitConverter.GetBytes(input.Length), data, 4);
            Array.Copy(BitConverter.GetBytes(lprops.Length), 0, data, 4, 4);
            Array.Copy(lprops, 0, data, 8, lprops.Length);
            cdata.Position = 0;
            cdata.Read(data, lprops.Length + 8, (int)cdata.Length);
            return new DataRecord() { Data = data, Compressed = true };
        }

        public class ArchiveCreateEntry {
            public string Filename { get; set; }
            public Func<byte[]> GetData { get; set; }
            public static ArchiveCreateEntry FromDisk(string baseFolder, string file) {
                return new ArchiveCreateEntry() {
                    Filename = file,
                    GetData = () => System.IO.File.ReadAllBytes(System.IO.Path.Combine(baseFolder, file))
                };
            }
        }

        private class CompressEntry {
            public ArchiveCreateEntry ACE;
            public DirectoryEntry Dir;
            public DataRecord DataRec;
        }

        private class CompressWork {
            public System.Collections.Concurrent.ConcurrentBag<CompressEntry> Input;
            public System.Collections.Concurrent.BlockingCollection<CompressEntry> Compressed;
            public CompressType Compress;
        }

        private static void CompressWorkThread(object param) {
            CompressWork cw = (CompressWork)param;
            CompressEntry ce;
            while (cw.Input.TryTake(out ce)) {
                ce.DataRec = GetData(ce.ACE.GetData(), ce.ACE.Filename, cw.Compress);
                cw.Compressed.Add(ce);
            }
        }

        public static void Create(System.IO.Stream output, IEnumerable<ArchiveCreateEntry> files, ArchiveFlags flags, CompressType compress, Action<double, string> onProgress) {
            if (!files.Any())
                throw new IrosArcException("Can't create an archive that contains no files");
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            double total = files.Count() + 2;
            int count = 0;

            onProgress(count / total, "");
            ArcHeader h = new ArcHeader() { Flags = flags, Version = MAX_VERSION, Directory = 16 };
            List<DirectoryEntry> entries = files.Select(f => new DirectoryEntry() {
                Filename = f.Filename, Flags = 0, 
            }).ToList();
            int dsize = entries.Select(e => (int)e.GetSize()).Sum();
            h.Save(output);
            output.WriteInt(entries.Count);
            long position = h.Directory + dsize + 4;
            onProgress(++count / total, "Wrote header");
            int index = 0;
            var combined = entries.Zip(files, (d,e) => new { Dir = d, ACE = e }).ToList();

            var cw = new CompressWork() {
                Input = new System.Collections.Concurrent.ConcurrentBag<CompressEntry>(),
                Compressed = new System.Collections.Concurrent.BlockingCollection<CompressEntry>(8),
                Compress = compress
            };
            
            foreach (var comb in combined) {
                cw.Input.Add(new CompressEntry() { ACE = comb.ACE, Dir = comb.Dir });
            }
            foreach (int _ in Enumerable.Range(0, 8)) System.Threading.ThreadPool.QueueUserWorkItem(CompressWorkThread, cw);

            int filesDone = 0;

            while (filesDone < combined.Count) {
                var entry = cw.Compressed.Take();
                entry.Dir.Offset = position;
                var data = entry.DataRec.Data;
                if (entry.DataRec.Compressed) entry.Dir.Flags |= FileFlags.CompressLZMA;
                entry.Dir.Length = data.Length;

                output.Position = position;
                output.Write(data, 0, data.Length);

                position += entry.Dir.Length;
                onProgress(++count / total, "Written " + entry.ACE.Filename);
                index++;
                filesDone++;
            }

            output.Position = h.Directory + 4;
            foreach (var entry in entries) {
                entry.Save(output);
            }
            sw.Stop();
            onProgress(++count / total, String.Format("Complete: {0} files, {1:0.0}MB in {2} seconds", entries.Count, output.Length / (1024f*1024f), sw.Elapsed.TotalSeconds));
        }

        public bool CheckValid() {
            foreach (var entry in _entries) {
                if ((entry.Offset + entry.Length) > _data.Length) return false;
            }
            return true;
        }

        public IrosArc(string filename, bool patchable = false, Action<int, int> progressAction = null) {
            _source = filename;
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (patchable)
                _data = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            else
                _data = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            _header = new ArcHeader();
            _header.Open(_data);

            int numfiles;
            _data.Position = _header.Directory;
            do {
                numfiles = _data.ReadInt();
                if (numfiles == -1) {
                    _data.Position = _data.ReadLong();
                }
            } while (numfiles < 0);
            _entries = new List<DirectoryEntry>();
            _lookup = new Dictionary<string, DirectoryEntry>(StringComparer.InvariantCultureIgnoreCase);
            _folderNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < numfiles; i++) {
                progressAction?.Invoke(i, numfiles);
                DirectoryEntry e = new DirectoryEntry();
                e.Open(_data, _header.Version);
#if !RUDE
                if ((e.Flags & FileFlags.RudeFlags) != 0) throw new IrosArcException(String.Format("Archive {0} entry {1} has invalid flags", filename, e.Filename));
#endif

                _entries.Add(e);
                _lookup[e.Filename] = e;
                int lpos = e.Filename.LastIndexOf('\\');
                if (lpos > 0) {
                    _folderNames.Add(e.Filename.Substring(0, lpos));
                }
            }
            sw.Stop();
            DebugLogger.DetailedWriteLine($"IrosArc: opened {filename}, contains {_lookup.Count} files, took {sw.ElapsedMilliseconds} ms to parse");
        }

        public void ApplyPatch(IrosArc patch, Action<double, string> onProgress) {
            int currentDirSize = _entries.Sum(e => e.GetSize());
            byte[] deldata = patch.GetBytes("%IrosPatch:Deleted");
            if (deldata != null) {
                string[] delfile = System.Text.Encoding.Unicode.GetString(deldata).Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string del in delfile) {
                    DebugLogger.DetailedWriteLine($"Removing file {del} from archive");
                    _entries.RemoveAll(e => e.Filename.Equals(del, StringComparison.InvariantCultureIgnoreCase));
                }
                onProgress(0, "Removed " + delfile.Length + " deleted files");
            }
            int count = 0;
            var files = patch.AllFileNames().Where(s => !s.StartsWith("%")).ToArray();
            foreach(string file in files) {
                var patchEntry = patch._lookup[file];
                byte[] data = new byte[patchEntry.Length];
                patch._data.Position = patchEntry.Offset;
                patch._data.Read(data, 0, data.Length);
                if (HasFile(file)) { //update existing
                    DebugLogger.DetailedWriteLine($"File {file} is already in archive...");
                    DirectoryEntry exist = _lookup[file];
                    if (exist.Length >= data.Length) { //put data in same position, woo
                        DebugLogger.DetailedWriteLine("...updating in place");
                        _data.Position = exist.Offset;
                    } else { //stick at end of file
                        _data.Position = _data.Length;
                        exist.Offset = _data.Position;
                        DebugLogger.DetailedWriteLine("...size increase: writing to end of file");
                    }
                    _data.Write(data, 0, data.Length);
                    exist.Length = data.Length;
                    exist.Flags = patchEntry.Flags;
                } else { //new file, just append
                    DebugLogger.DetailedWriteLine($"File {file} is new, appending");
                    DirectoryEntry de = new DirectoryEntry() {
                        Filename = file,
                        Flags = patchEntry.Flags,
                        Length = patchEntry.Length,
                        Offset = _data.Length
                    };
                    _data.Position = de.Offset;
                    _data.Write(data, 0, data.Length);
                    _entries.Add(de);
                    _lookup[file] = de;
                }

                count++;
                onProgress(1.0 * count / files.Length, "Processed " + file);
            }
            int newDirSize = _entries.Sum(e => e.GetSize());
            if (newDirSize <= currentDirSize) {
                DebugLogger.DetailedWriteLine("Directory will fit in existing location");
                _data.Position = _header.Directory;
            } else {
                DebugLogger.DetailedWriteLine("Directory size increase, appending");
                if (_data.Length >= int.MaxValue) { //write forwarder
                    _data.Position = _header.Directory;
                    _data.WriteInt(-1);
                    _data.WriteLong(_data.Length);
                    _data.Position = _data.Length;
                } else { //write direct location
                    _header.Directory = (int)_data.Length;
                    _data.Position = _header.Directory;
                }
            }
            _data.WriteInt(_entries.Count);
            foreach (var e in _entries)
                e.Save(_data);
            _header.Version = MAX_VERSION;
            _data.Position = 0;
            _header.Save(_data);
            onProgress(1.0, "Wrote directory");
        } //TODO: track blank spaces in file and reuse where possible...

        public IEnumerable<string> AllFileNames() {
            return _lookup.Keys;
        }

        public IEnumerable<string> AllFolderNames() {
            return _folderNames.Select(s => s);
        }

        public bool HasFolder(string name) {
            bool result = _folderNames.Contains(name);
            if (result)
            {
                DebugLogger.DetailedWriteLine($"ARCHIVE: {_source} contains folder {name}");
            }
            
            return result;
        }

        public bool HasFile(string name) {
            bool result = _lookup.ContainsKey(name);
            if (result)
            {
                DebugLogger.DetailedWriteLine($"ARCHIVE: {_source} contains file {name}");
            }

            return result;
        }

        public int GetFileSize(string name) {
            DirectoryEntry e;
            if (_lookup.TryGetValue(name, out e)) {
                switch (e.Flags & FileFlags.COMPRESSION_FLAGS) {
                    case FileFlags.CompressLZMA:
                        _data.Position = e.Offset;
                        return _data.ReadInt();
                    default:
                    case FileFlags.None:
                        return e.Length;
                }
            } else
                return -1;
        }

        private void CleanCache() {
            long[] remove = _cache
                .ToArray()
                .Where(kv => kv.Value.LastAccess < DateTime.Now.AddSeconds(-60))
                .Select(kv => kv.Key)
                .ToArray();
            if (remove.Any()) {
                DebugLogger.DetailedWriteLine($"Removing {remove.Length} compressed files from cache: ");
                CacheEntry _;
                foreach (long r in remove) _cache.TryRemove(r, out _);
            }
        }

        //private int _cacheCounter = 0;

        private CacheEntry GetCache(DirectoryEntry e) {
            CacheEntry ce;
            if (!_cache.TryGetValue(e.Offset, out ce)) {
                ce = new CacheEntry() { File = e.Filename };
                byte[] data;
                lock (_data) {
                    switch (e.Flags & FileFlags.COMPRESSION_FLAGS) {
                        case FileFlags.CompressLZS:
                            data = new byte[e.Length];
                            _data.Position = e.Offset;
                            _data.Read(data, 0, e.Length);
                            var ms = new System.IO.MemoryStream(data);
                            var output = new System.IO.MemoryStream();
                            Lzs.Decode(ms, output);
                            data = new byte[output.Length];
                            output.Position = 0;
                            output.Read(data, 0, data.Length);
                            ce.Data = data;
                            break;
                        case FileFlags.CompressLZMA:
                            _data.Position = e.Offset;
                            int decSize = _data.ReadInt(), propSize = _data.ReadInt();
                            byte[] props = new byte[propSize];
                            _data.Read(props, 0, props.Length);
                            byte[] cdata = new byte[e.Length - propSize - 8];
                            _data.Read(cdata, 0, cdata.Length);
                            data = new byte[decSize];
                            var lzma = new SharpCompress.Compressors.LZMA.LzmaStream(props, new System.IO.MemoryStream(cdata));
                            lzma.Read(data, 0, data.Length);
                            /*int srcSize = cdata.Length;
                            switch (LzmaUncompress(data, ref decSize, cdata, ref srcSize, props, props.Length)) {
                                case SZ_OK:
                                    //Woohoo!
                                    break;
                                default:
                                    throw new IrosArcException("Error decompressing " + e.Filename);
                            }*/
                            ce.Data = data;
                            break;
                        default:
                            throw new IrosArcException("Bad compression flags " + e.Flags.ToString());
                    }
                }
                _cache.AddOrUpdate(e.Offset, ce, (_, __) => ce);
            }
            ce.LastAccess = DateTime.Now;
            CleanCache();

            /*
            if ((_cacheCounter++ % 100) == 0)
                DebugLogger.WriteLine("IRO cache contents; " + String.Join(",", _cache.Values.Select(e => e.File)));
            */

            return ce;
        }

        public byte[] GetBytes(string name) {
            DirectoryEntry e;
            if (_lookup.TryGetValue(name, out e)) {
                if ((e.Flags & FileFlags.COMPRESSION_FLAGS) != 0)
                    return GetCache(e).Data;
                else {
                    lock (_data) {
                        byte[] data = new byte[e.Length];
                        _data.Position = e.Offset;
                        _data.Read(data, 0, e.Length);
                        return data;
                    }
                }
            } else
                return null;
        }
        public System.IO.Stream GetData(string name) {
            byte[] data = GetBytes(name);
            return data == null ? null : new System.IO.MemoryStream(data);
        }

        public void RawRead(string file, uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            DirectoryEntry e;
            if (_lookup.TryGetValue(file, out e)) {
                if ((e.Flags & FileFlags.COMPRESSION_FLAGS) != 0) {
                    var cache = GetCache(e);
                    uint readLen = Math.Min(length, (uint)cache.Data.Length - offset);
                    Util.CopyToIntPtr(cache.Data, dest, (int)readLen, (int)offset);
                    bytesRead = readLen;
                    if (readLen == 0) {
                        DebugLogger.DetailedWriteLine($"IrosArc RawRead file {file} offset {offset} length {length} read {readLen} bytes - cache data size {cache.Data.Length}");
                    }
                } else {
                    uint readLen = Math.Min(length, (uint)e.Length - offset);
                    long Loffset = offset + e.Offset;
                    Win32.OVERLAPPED ov = new Win32.OVERLAPPED() {
                        EventHandle = IntPtr.Zero,
                        Internal = UIntPtr.Zero,
                        InternalHigh = UIntPtr.Zero,
                        Offset = (uint)(Loffset & 0xffffffff),
                        OffsetHigh = (uint)(Loffset >> 32)
                    };

                    Win32.ReadFile(_data.SafeFileHandle.DangerousGetHandle(), dest, readLen, ref bytesRead, ref ov);
                }
            } else
                bytesRead = 0;
        }

        public void Dispose() {
            if (_data != null) {
                _data.Close();
                _data = null;
            }
        }

        public IntPtr GetDummyHandle(Process process) {
            IntPtr h;

            Win32.DuplicateHandle(process.Handle, _data.SafeFileHandle.DangerousGetHandle(), process.Handle, out h, 0, false, 0x2);

            return h;
        }

        public override string ToString() {
            return "[IrosArchive " + _source + "]";
        }

        public IEnumerable<string> GetInformation() {
            yield return _header.ToString();

            foreach (var entry in _entries)
                yield return entry.ToString();
        }
    }

    public static class IrosPatcher {
        private static bool Same(byte[] b1, byte[] b2) {
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i]) return false;
            return true;
        }
        public static void Create(IrosArc original, IrosArc updated, System.IO.Stream patchOutput, CompressType compress, Action<double, string> onProgress) {
            string[] deleted = original.AllFileNames()
                .Except(updated.AllFileNames(), StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
            List<IrosArc.ArchiveCreateEntry> pentries = new List<IrosArc.ArchiveCreateEntry>();
            if (deleted.Any()) {
                onProgress(0, "Adding deleted entries");
                byte[] deldata = System.Text.Encoding.Unicode.GetBytes(String.Join("\n", deleted));
                pentries.Add(new IrosArc.ArchiveCreateEntry() {
                    Filename = "%IrosPatch:Deleted",
                    GetData = () => deldata
                });
            }
            foreach (string newFile in updated.AllFileNames().Except(original.AllFileNames(), StringComparer.InvariantCultureIgnoreCase)) {
                onProgress(0, "Adding new entries");
                string fn = newFile;
                pentries.Add(new IrosArc.ArchiveCreateEntry() {
                    Filename = fn,
                    GetData = () => updated.GetBytes(fn)
                });
            }
            foreach (string check in original.AllFileNames().Intersect(updated.AllFileNames())) {
                onProgress(0, "Checking " + check);
                if (!Same(original.GetBytes(check), updated.GetBytes(check))) {
                    string fn = check;
                    pentries.Add(new IrosArc.ArchiveCreateEntry() {
                        Filename = fn,
                        GetData = () => updated.GetBytes(fn)
                    });
                }
            }
            IrosArc.Create(patchOutput, pentries, ArchiveFlags.Patch, compress, onProgress);
        }
    }
}
