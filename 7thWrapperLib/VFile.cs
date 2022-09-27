/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace _7thWrapperLib {
    class VFileException : Exception {
        public VFileException(string msg) : base(msg) { }
    }

    public class VFile {
        private List<VRange> _ranges = new List<VRange>();
        private VRange _cache;

        public override string ToString() {
            return "VFile containing " + _ranges.Count + " ranges";
        }

        public void Dump() {
            DebugLogger.WriteLine("VFILE:");
            foreach (var range in _ranges) {
                DebugLogger.WriteLine($"  Range {range.GetType().Name} starts {range.Start} length {range.Length} [{range.Tag}]");
            }
        }

        public void Add(VRange range) {
            if (_ranges.Count == 0) {
                _ranges.Add(range);
                _cache = _ranges[0];
                return;
            }
            VRange prev = _ranges.Last();
            if (range.Start != (prev.Start + prev.Length)) throw new VFileException(String.Format("Range mismatch on add - {0} != {1} + {2}", range.Start, prev.Start, prev.Length));
            VRangeHandle hprev = prev as VRangeHandle, hcurr = range as VRangeHandle;

            if ((hprev != null) && (hcurr != null) && (hprev.Handle == hcurr.Handle) && ((hprev.Offset + hprev.Length) == hcurr.Offset)) {
                (prev as VRangeHandle).Length += range.Length;
                //DebugLogger.WriteLine("---expanding existing range");
            } else {
                _ranges.Last().Next = range;
                _ranges.Add(range);
            }
            _cache = _ranges[0];
        }

        private VRange Find(uint offset) {
            if (_cache.InRange((int)offset)) return _cache;
            int min = 0, max = _ranges.Count - 1;
            while (min <= max) {
                int check = (min + max + 1) / 2;
                VRange r = _ranges[check];
                if (offset < r.Start)
                    max = check - 1;
                else if (offset > r.LastValidOffset)
                    min = check + 1;
                else {
                    return r;
                }
            }
            return null;
        }

        public void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            VRange r = _cache = Find(offset);
            bytesRead = 0;
            uint remaining = length;
            do {
                uint read = 0;
                uint roffset = offset - r.Start;
                uint toread = Math.Min(remaining, r.Length - roffset);
                r.Read(roffset, toread, dest, ref read);
                bytesRead += read;
                dest += (int)read;
                offset += read;
                remaining -= read;
                if (read < toread) {
                    return;
                }
                r = r.Next;
            } while (remaining > 0 && r != null);
        }
    }

    public abstract class VRange {
        public uint Start { get; set; }
        public uint Length { get; set; }
        public uint LastValidOffset { get { return Start + Length - 1; } }
        public VRange Next { get; set; }
        public string Tag { get; set; }

        public bool InRange(int offset) {
            return (offset >= Start) && (offset <= LastValidOffset);
        }

        public abstract void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead);
    }

    class VRangeHandle : VRange {
        public IntPtr Handle { get; set; }
        public uint Offset { get; set; }

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            Win32.OVERLAPPED ov = new Win32.OVERLAPPED() {
                EventHandle = IntPtr.Zero,
                Internal = UIntPtr.Zero,
                InternalHigh = UIntPtr.Zero,
                Offset = Offset + offset,
                OffsetHigh = 0
            };
            Win32.ReadFile(Handle, dest, length, ref bytesRead, ref ov);
        }
    }

    class VRangeInline : VRange {
        public byte[] Data { get; set; }

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            System.Runtime.InteropServices.Marshal.Copy(Data, (int)offset, dest, (int)length);
            bytesRead = length;
        }
    }

    class VRangeNull : VRange {
        private static byte[] _zero = new byte[16384];

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            while(length > 0) {
                uint len = Math.Min(length, 16384);
                System.Runtime.InteropServices.Marshal.Copy(_zero, 0, dest, (int)len);
                length -= len;
                bytesRead += len;
            }
        }
    }

    abstract class VRangeCleanup : VRange {
        public abstract void Cleanup(DateTime cutoff);
    }

    class VRangeChunked : VRange {
        private bool _lastHeader = false, _lastInit;
        private List<RuntimeMod> _mods;
        private byte[] _calculated, _header;
        private IntPtr _fbHandle;
        private uint _fbOffset;
        private int _fbLen;

        public string Name { get; set; }

        public VRangeChunked(string name, List<RuntimeMod> mods, IntPtr fallbackHandle, uint fallbackOffset, int fallbackLen) {
            _mods = mods;
            _fbHandle = fallbackHandle;
            _fbOffset = fallbackOffset;
            _fbLen = fallbackLen;
            _header = new byte[24];
            byte[] bname = System.Text.Encoding.ASCII.GetBytes(name);
            Array.Copy(bname, _header, bname.Length);
            Bytes.WriteInt(_header, 20, _fbLen);
        }

        private unsafe void Recalculate() {
            DebugLogger.WriteLine($"Chunked file {Name} recalculating contents...");
            byte[] original = new byte[_fbLen];
            Win32.OVERLAPPED ov = new Win32.OVERLAPPED() {
                EventHandle = IntPtr.Zero,
                Internal = UIntPtr.Zero,
                InternalHigh = UIntPtr.Zero,
                Offset = _fbOffset,
                OffsetHigh = 0
            };

            fixed (byte* bp = &original[0]) {
                uint bytesRead = 0;
                //TODO should loop...
                Win32.ReadFile(_fbHandle, new IntPtr(bp), (uint)_fbLen, ref bytesRead, ref ov);
            }
            DebugLogger.WriteLine($"Original read {_fbLen} from {_fbOffset} sig {original[0]} {original[1]} {original[2]} {original[3]}");

            var orig = FieldFile.Unchunk(original);

            foreach (int i in Enumerable.Range(0, orig.Count)) {
                string fn = Name + ".chunk." + (i + 1);
                foreach (var of in _mods.SelectMany(m => m.GetOverrides(fn))) {
                    if (of.CFolder == null || of.CFolder.IsActive(of.CName)) {
                        if (of.Archive == null) {
                            orig[i] = System.IO.File.ReadAllBytes(of.File);
                        } else {
                            orig[i] = of.Archive.GetBytes(of.File);
                        }
                        break;
                    }
                }
            }

            _calculated = FieldFile.Chunk(orig);
            Bytes.WriteInt(_header, 20, _calculated.Length);
            DebugLogger.WriteLine($"New length of {Name} is {_calculated.Length}");
        }

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            DebugLogger.WriteLine($"Chunked {Name} reading from OS {offset} L {length}");

            bool header = (offset < 24);
            bool init = (offset == 24);

            if ((!_lastHeader && header) || (init && !_lastInit) || _calculated == null) { //re-evaluate 
                Recalculate();
                _lastInit = true; //we're ready to read file headers
            } else {
                _lastInit = init;
            }
            _lastHeader = header;

            if (offset < 24) {
                length = Math.Min(length, 24 - offset);
                System.Runtime.InteropServices.Marshal.Copy(_header, (int)offset, dest, (int)length);
                bytesRead = length;
                DebugLogger.WriteLine($"Chunked {Name} reading from cheader - {bytesRead} bytes read [current size is {BitConverter.ToInt32(_header, 20)}]");
                return;
            } else {
                offset -= 24;
                if (offset >= _calculated.Length) {
                    bytesRead = length; return; //leave with garbage...
                }
                int len = Math.Min((int)length, _calculated.Length - (int)offset);
                System.Runtime.InteropServices.Marshal.Copy(_calculated, (int)offset, dest, len);
                bytesRead = (uint)len;
            }
        }
    }

    class VRangeConditional : VRangeCleanup {

        private IntPtr _handle;
        private OverrideFile _current;
        private List<OverrideFile> _files;
        private DateTime _access;
        private byte[] _header;
        private IntPtr _fallbackHandle;
        private uint _fallbackOffset;
        private bool _lastHeader = false, _lastInit;

        public string Name { get; set; }

        public VRangeConditional(string name, List<OverrideFile> files, IntPtr fallbackHandle, uint fallbackOffset) {
            _files = files;
            _handle = IntPtr.Zero;
            _current = null;
            _header = new byte[24];
            _fallbackHandle = fallbackHandle;
            _fallbackOffset = fallbackOffset;
            byte[] bname = System.Text.Encoding.ASCII.GetBytes(name);
            Array.Copy(bname, _header, bname.Length);
            Bytes.WriteInt(_header, 20, files.Select(f => f.Size).Max());
        }

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            //DebugLogger.WriteLine("Conditional {2} reading from OS {0} L {1}", offset, length, Name);

            bool header = (offset < 24);
            bool init = (offset == 24);

            if ((!_lastHeader && header) || (init && !_lastInit)) { //re-evaluate 
                _current = null;
                if (!_handle.Equals(IntPtr.Zero)) Win32.CloseHandle(_handle);
                _handle = IntPtr.Zero;

                foreach (var of in _files) {
                    if (of.CFolder == null || of.CFolder.IsActive(of.CName)) {
                        DebugLogger.WriteLine($"Conditional {Name} switching to {of.File}");
                        _current = of;
                        Bytes.WriteInt(_header, 20, of.Size);
                        break;
                    }
                }
                if (_current == null)
                    DebugLogger.WriteLine($"Conditional {Name} switching to fallback");
                _lastInit = true; //we're ready to read file headers
            } else {
                _lastInit = init;
            }
            if (_current != null && _handle.Equals(IntPtr.Zero) && _current.Archive == null) {
                _handle = Wrap.CreateFileW(_current.File, System.IO.FileAccess.Read, System.IO.FileShare.Read, IntPtr.Zero, System.IO.FileMode.Open, System.IO.FileAttributes.Normal, IntPtr.Zero);
            }
            _lastHeader = header;
            _access = DateTime.Now;

            if (_current == null) { //read from fallback 
                //DebugLogger.WriteLine("Conditional reading from fallback handle {0} with extra offset {1}", _fallbackHandle, _fallbackOffset);
                Win32.OVERLAPPED ov = new Win32.OVERLAPPED() {
                    EventHandle = IntPtr.Zero,
                    Internal = UIntPtr.Zero,
                    InternalHigh = UIntPtr.Zero,
                    Offset = _fallbackOffset + offset,
                    OffsetHigh = 0
                };
                Win32.ReadFile(_fallbackHandle, dest, length, ref bytesRead, ref ov);
                //DebugLogger.WriteLine("Conditional {1} reading from fallback - {0} bytes read", bytesRead, Name);
            } else {
                if (offset < 24) {
                    length = Math.Min(length, 24 - offset);
                    System.Runtime.InteropServices.Marshal.Copy(_header, (int)offset, dest, (int)length);
                    bytesRead = length;
                    //DebugLogger.WriteLine("Conditional {2} reading from cheader - {0} bytes read [current size is {1}]", bytesRead, BitConverter.ToInt32(_header, 20), Name);
                    return;
                } else {
                    offset -= 24;
                    if (_current.Archive == null) {
                        Wrap.SetFilePointer(_handle, (int)offset, IntPtr.Zero, Wrap.EMoveMethod.Begin);
                        Win32.ReadFile(_handle, dest, length, ref bytesRead, IntPtr.Zero);
                        //DebugLogger.WriteLine("Conditional {1} reading from cfile - {0} bytes read", bytesRead, Name);
                    } else {
                        _current.Archive.RawRead(_current.File, offset, length, dest, ref bytesRead);
                        //DebugLogger.WriteLine("Conditional {1} reading from cfile archive offset {2} length {3} - {0} bytes read", bytesRead, Name, offset, length);
                    }
                }
            }
        }

        public override void Cleanup(DateTime cutoff) {
            if (_access < cutoff && !_handle.Equals(IntPtr.Zero)) {
                Win32.CloseHandle(_handle);
                _handle = IntPtr.Zero;
                DebugLogger.WriteLine($"Closing handle to {_current}, idle");
            }
        }
    }

    class VRangeArchive : VRange {
        public string File { get; set; }
        public IrosArc Archive { get; set; }
        
        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            Archive.RawRead(File, offset, length, dest, ref bytesRead);
            //DebugLogger.WriteLine("VRangeArchive read from {0} offset {1} length {2}, {3} bytes read", File, offset, length, bytesRead);
        }
    }

    class VRangeFile : VRangeCleanup {
        public string Filename { get; set; }

        private IntPtr _handle;
        private DateTime _access;

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            if (_handle == IntPtr.Zero) _handle = Wrap.CreateFileW(Filename, System.IO.FileAccess.Read, System.IO.FileShare.Read, IntPtr.Zero, System.IO.FileMode.Open, System.IO.FileAttributes.Normal, IntPtr.Zero);
            _access = DateTime.Now;
            Wrap.SetFilePointer(_handle, (int)offset, IntPtr.Zero, Wrap.EMoveMethod.Begin);
            Win32.ReadFile(_handle, dest, length, ref bytesRead, IntPtr.Zero);
        }

        public override void Cleanup(DateTime cutoff) {
            if (_access < cutoff && !(_handle.Equals(IntPtr.Zero))) {
                Win32.CloseHandle(_handle);
                _handle = IntPtr.Zero;
                DebugLogger.WriteLine($"Closing handle to {Filename}, idle");
            }
        }
    }

    public class LGPWrapper {
        public VFile VFile { get; private set; }
        private List<VRangeCleanup> _wFiles = new List<VRangeCleanup>();
        private DateTime _lastClean;

        public bool IsActive { get; private set; }

        public void Ping() {
            if (_lastClean < DateTime.Now.AddMinutes(-1)) {
                DateTime cutoff = DateTime.Now.AddMinutes(-1);
                foreach (var file in _wFiles) file.Cleanup(cutoff);
                _lastClean = DateTime.Now;
            }
        }

        public static OverrideFile MapFile(string file, RuntimeProfile profile) {
            if (profile.mappedFiles.TryGetValue(file.ToLower(), out List<OverrideFile> mappedList))
            {
                foreach (var overrideFile in mappedList)
                {
                    if (overrideFile.CFolder == null || overrideFile.CFolder.IsActive(file))
                        return overrideFile;
                }
            }

            return null;
        }

        public static bool AnyOverridesFor(string path, RuntimeProfile profile) {
            foreach (var item in profile.Mods)
                if (item.OverridesFolder(path)) return true;
            return false;
        }

        public LGPWrapper(IntPtr handle, string name, RuntimeProfile profile) {
            if (!AnyOverridesFor(name, profile)) {
                DebugLogger.WriteLine($"LGPWrapper: no overrides for {name}, early out");
                return; //optimisation, don't run anything else, if no override files
            }
            IsActive = true;

            DebugLogger.WriteLine("   LGPWrapper: Parsing");
            var fs = new System.IO.FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(handle, false), FileAccess.Read);
            ProcMonParser.DataFile df = ProcMonParser.FF7Files.LoadLGP(fs, name);
            fs.Position = 0;

            Dictionary<string, int> sortKeys = df.Items.ToDictionary(i => i.Name, i => i.Index, StringComparer.InvariantCulture);
            Dictionary<string, long> dataKeys = df.Items.ToDictionary(i => i.Name, i => i.Start, StringComparer.InvariantCulture);
            Dictionary<string, int> filesSizes = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, int> filesOptions = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase); 
            Dictionary<string, ProcMonParser.DataItem> lgpItems = df.Items.ToDictionary(i => i.Name, i => i, StringComparer.InvariantCulture);
            foreach (var item in df.Items) {
                filesOptions[item.Name] = 0;
                filesSizes[item.Name] = item.Length - 24;
                DebugLogger.DetailedWriteLine($"Checking chunk support for {name}~{item.Name}~");
                if (profile.Mods.Any(m => m.SupportsChunks(System.IO.Path.Combine(name, item.Name)))) {
                    filesSizes[item.Name] = Math.Max(filesSizes[item.Name], 1024 * 1024 * 2); //This is a horrible hack. TODO.
                    filesOptions[item.Name] |= 0x1;
                }
            }
            DebugLogger.WriteLine("   LGPWrapper: Prepared structures");
            List<string> names = profile.Mods.SelectMany(m => m.GetPathOverrideNames(name)).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
            foreach (string fname in names) {
                if (fname.IndexOf(".chunk.", StringComparison.InvariantCultureIgnoreCase) >= 0) continue;
                if (!filesSizes.ContainsKey(fname)) {
                    filesSizes[fname] = 0;
                    DebugLogger.WriteLine($"Added LGP file {name} {fname}");
                }
                var overrides = profile.Mods.SelectMany(m => m.GetOverrides(System.IO.Path.Combine(name, fname)));
                foreach(var over in overrides) {
                    filesSizes[fname] = Math.Max(filesSizes[fname], over.Size);
                    if (over.CFolder == null) break;
                }
            }
            List<LGPEntry> entries = filesSizes.Select(kv => new LGPEntry(kv.Key, kv.Value)).ToList();
            List<LGP.LGPEntryMade> newentries;

            DebugLogger.WriteLine("   LGPWrapper: creating new headers");
            byte[] headers = LGP.CalculateHeaders(entries, 
                s => {
                    int index;
                    sortKeys.TryGetValue(s, out index);
                    return index;
                },
                s => {
                    long index;
                    dataKeys.TryGetValue(s, out index);
                    return (uint)index;
                },
                out newentries
            );
            int datastart = headers.Length;
            DebugLogger.WriteLine($"   LGPWrapper: Calculated new LGP headers for {name} with {entries.Count} file entries");

            /*            
            int datastart = df.Items[0].Start;
            byte[] headers = new byte[datastart];
            fs.Read(headers, 0, datastart);
            fs.Position = 0;
            */

            uint offset = (uint)datastart;
            VFile = new VFile();
            try {
                VFile.Add(new VRangeInline() { Start = 0, Length = offset, Data = headers, Tag = "Headers" });
                int count = 0;
                foreach (var item in newentries.OrderBy(em => em.DataIndex)) {
                    DebugLogger.DetailedWriteLine($"LGPWrapper calculate {item.Entry.Name}");
                    //Bytes.WriteUInt(headers, 16 + 20 + 27 * item.Index, offset);
                    string fn = System.IO.Path.Combine(name, item.Entry.Name);
                    var overrides = profile.Mods.SelectMany(m => m.GetOverrides(fn));

                    int fOptions;
                    filesOptions.TryGetValue(item.Entry.Name, out fOptions);                   
                    bool chunked = (fOptions & 0x1) != 0;
                    //var overrides = Enumerable.Empty<OverrideFile>();
                    //DebugLogger.WriteLine("Virtualizing LGP entry {0} at offset {1}", item.Entry.Name, offset);
                    if (item.DataOffset != offset) throw new Exception("LGPWrapper mismatch on offset for " + item.Entry.Name + " offset=" + offset + " hoffset=" + item.DataOffset);

                    if (chunked) {
                        long pos = lgpItems[item.Entry.Name].Start + 24;
                        int len = lgpItems[item.Entry.Name].Length - 24;
                        VFile.Add(new VRangeChunked(item.Entry.Name, profile.Mods, handle, (uint)pos, len) {
                            Start = offset,
                            Length = (uint)(item.Entry.MaxSize + 24),
                            Name = fn,
                            Tag = "Chunked"
                        });
                        //DebugLogger.WriteLine("File {0} initialized with chunks", item.Entry.Name, 0);
                        offset += (uint)item.Entry.MaxSize + 24;
                    } else if (!overrides.Any()) { //take from original LGP
                        long pos = lgpItems[item.Entry.Name].Start;
                        VFile.Add(new VRangeHandle() { Start = offset, Length = (uint)(item.Entry.MaxSize + 24), Handle = handle, Offset = (uint)pos, Tag = item.Entry.Name });
                        offset += (uint)item.Entry.MaxSize + 24;
                        //DebugLogger.WriteLine("--VRangeHandle");
                    } else if (overrides.First().CFolder == null) { //only one override, replace directly
                        var ov = overrides.First();

                        byte[] fheader = new byte[24];
                        System.Text.Encoding.ASCII.GetBytes(item.Entry.Name, 0, item.Entry.Name.Length, fheader, 0);
                        Bytes.WriteUInt(fheader, 20, (uint)ov.Size);
                        VFile.Add(new VRangeInline() { Start = offset, Length = 24, Data = fheader, Tag = item.Entry.Name + "%header" });
                        offset += 24;
                        VRange vr;
                        if (ov.Archive == null) {
                            var vf = new VRangeFile() { Start = offset, Length = (uint)ov.Size, Filename = ov.File, Tag = ov.File };
                            VFile.Add(vf);
                            _wFiles.Add(vf);
                            vr = vf;
                            //DebugLogger.WriteLine("LGP entry {0} coming from file {1}", item.Entry.Name, ov.File);
                        } else {
                            vr = new VRangeArchive() { Start = offset, Length = (uint)ov.Size, File = ov.File, Archive = ov.Archive, Tag = ov.File };
                            VFile.Add(vr);
                            //DebugLogger.WriteLine("LGP entry {0} coming from archive file {1} with size {2}", item.Entry.Name, ov.File, ov.Size);
                        }
                        //if (vr.Length != item.Entry.MaxSize)
                            //DebugLogger.WriteLine("Entry {0} size difference {1} vs {2}", item.Entry.Name, vr.Length, item.Entry.MaxSize);
                        if (vr.Length < item.Entry.MaxSize) {
                            uint diff = (uint)item.Entry.MaxSize - vr.Length;
                            VFile.Add(new VRangeNull() { Length = diff, Start = vr.Start + vr.Length, Tag = "Padding" });
                        }
                        offset += (uint)item.Entry.MaxSize;
                        //DebugLogger.WriteLine("--VRangeFile");
                    } else { //multiple overrides; tricky!
                        //DebugLogger.WriteLine("Add VRangeConditional for " + item.Entry.Name);
                        ProcMonParser.DataItem di;
                        lgpItems.TryGetValue(item.Entry.Name, out di);
                        uint fbOffset = (di == null) ? 0 : (uint)di.Start;

                        var vcond = new VRangeConditional(item.Entry.Name, overrides.ToList(), handle, fbOffset) { Length = (uint)item.Entry.MaxSize + 24, Start = offset, Name = item.Entry.Name, Tag = item.Entry.Name };
                        VFile.Add(vcond);
                        _wFiles.Add(vcond);
                        offset += (uint)item.Entry.MaxSize + 24;
                        //DebugLogger.WriteLine("--VRangeConditional");
                    }
                    count++;
                    /*
                    string file = MapFile(System.IO.Path.Combine(name, item.Name), profile);
                    if (file != null) {
                        uint length = (uint)new System.IO.FileInfo(file).Length;
                        byte[] fheader = new byte[24];
                        System.Text.Encoding.ASCII.GetBytes(item.Name, 0, item.Name.Length, fheader, 0);
                        Bytes.WriteUInt(fheader, 20, length);
                        VFile.Add(new VRangeInline() { Start = offset, Length = 24, Data = fheader });
                        offset += 24;
                        var vf = new VRangeFile() { Start = offset, Length = length, Filename = file };
                        VFile.Add(vf);
                        _wFiles.Add(vf);
                        offset += length;
                    } else {
                        VFile.Add(new VRangeHandle() { Start = offset, Length = (uint)(item.Length), Handle = handle, Offset = (uint)item.Start });
                        offset += (uint)item.Length;
                    }*/
                }
                byte[] footer = System.Text.Encoding.ASCII.GetBytes("FINAL FANTASY7");
                VFile.Add(new VRangeInline() { Start = offset, Length = (uint)footer.Length, Data = footer, Tag = "footer" });
                DebugLogger.WriteLine("Created: " + VFile.ToString());
            } catch {
                VFile.Dump();
                throw;
            }
        }
    }

    class VArchiveData {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);

        private long _position, _size;
        private byte[] _data;

        public long Size { get { return _size; } }

        public VArchiveData(byte[] data) {
            _data = data;
            _size = _data.LongLength;
            _position = 0;
        }

        public int SetFilePointer(long offset, Wrap.EMoveMethod method) {
            switch (method) {
                case Wrap.EMoveMethod.Begin:
                    _position = offset;
                    break;
                case Wrap.EMoveMethod.End:
                    _position = _size + offset;
                    break;
                case Wrap.EMoveMethod.Current:
                    _position += offset;
                    break;
            }
            if (_position < 0) return -1;
            if (_position > _size) return -1;
            return (int)_position;
        }
        public bool SetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod) {
            SetFilePointer(liDistanceToMove, (Wrap.EMoveMethod)dwMoveMethod);
            if (lpNewFilePointer != IntPtr.Zero)
                System.Runtime.InteropServices.Marshal.WriteInt64(lpNewFilePointer, _position);
            return true;
        }
        public unsafe int ReadFile(IntPtr bytes, uint numBytesToRead, ref uint numBytesRead) {
            numBytesRead = Math.Min(numBytesToRead, (uint)(_size - _position));
            if (numBytesRead == 0) return 1;

            fixed (byte* ptr = &_data[_position])
                memcpy(bytes, new IntPtr(ptr), numBytesRead);
            _position += numBytesRead;
            return 1;
        }
        public uint GetFileSize(IntPtr lpFileSizeHigh) {
            if (lpFileSizeHigh != IntPtr.Zero)
                System.Runtime.InteropServices.Marshal.WriteInt32(lpFileSizeHigh, (int)(_size >> 32));
            return (uint)(_size & 0xffffffff);
        }
    }

    class VArchiveFile {
        private IrosArc _arc;
        private string _filename;
        private long _position;
        private long _size;

        public long Size { get { return _size; } }

        public VArchiveFile(IrosArc arc, string filename) {
            _arc = arc;
            _filename = filename;
            _position = 0;
            _size = arc.GetFileSize(filename);
        }

        public int SetFilePointer(long offset, Wrap.EMoveMethod method) {
            DebugLogger.WriteLine($"VArchive SetFilePointer on {_filename} to {offset} from {method}");
            switch (method) {
                case Wrap.EMoveMethod.Begin:
                    _position = offset;
                    break;
                case Wrap.EMoveMethod.End:
                    _position = _size + offset;
                    break;
                case Wrap.EMoveMethod.Current:
                    _position += offset;
                    break;
            }
            if (_position < 0) return -1;
            if (_position > _size) return -1;
            return (int)_position;
        }

        public bool SetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod) {
            DebugLogger.WriteLine("VArchive SetFilePointerEx");
            SetFilePointer(liDistanceToMove, (Wrap.EMoveMethod)dwMoveMethod);
            if (lpNewFilePointer != IntPtr.Zero)
                System.Runtime.InteropServices.Marshal.WriteInt64(lpNewFilePointer, _position);
            return true;
        }

        public int ReadFile(IntPtr bytes, uint numBytesToRead, ref uint numBytesRead) {
            DebugLogger.WriteLine($"VArchive ReadFile on {_filename} from {_position} for {numBytesToRead} bytes");
            _arc.RawRead(_filename, (uint)_position, numBytesToRead, bytes, ref numBytesRead);
            _position += numBytesRead;
            DebugLogger.WriteLine($"...actually read {numBytesRead} bytes");
            return 1;
        }

        public uint GetFileSize(IntPtr lpFileSizeHigh) {
            DebugLogger.WriteLine($"VArchive GetFileSize on {_filename} = {_size}");
            if (lpFileSizeHigh != IntPtr.Zero)
                System.Runtime.InteropServices.Marshal.WriteInt32(lpFileSizeHigh, (int)(_size >> 32));
            return (uint)(_size & 0xffffffff);
        }
    }

}
