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
using Iros;

namespace AppWrapper {
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
            Util.CopyToIntPtr(Data, dest, (int)length, (int)offset);
            bytesRead = length;
        }
    }

    class VRangeNull : VRange {
        private static byte[] _zero = new byte[16384];

        public override void Read(uint offset, uint length, IntPtr dest, ref uint bytesRead) {
            while(length > 0) {
                uint len = Math.Min(length, 16384);
                Util.CopyToIntPtr(_zero, dest, (int)len);
                length -= len;
                bytesRead += len;
            }
        }
    }

    abstract class VRangeCleanup : VRange {
        public abstract void Cleanup(DateTime cutoff);
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
                _handle = Win32.CreateFileW(_current.File, System.IO.FileAccess.Read, System.IO.FileShare.Read, IntPtr.Zero, System.IO.FileMode.Open, System.IO.FileAttributes.Normal, IntPtr.Zero);
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
                    Util.CopyToIntPtr(_header, dest, (int)length, (int)offset);
                    bytesRead = length;
                    //DebugLogger.WriteLine("Conditional {2} reading from cheader - {0} bytes read [current size is {1}]", bytesRead, BitConverter.ToInt32(_header, 20), Name);
                    return;
                } else {
                    offset -= 24;
                    if (_current.Archive == null) {
                        Win32.SetFilePointer(_handle, (int)offset, IntPtr.Zero, Win32.EMoveMethod.Begin);
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
            if (_handle == IntPtr.Zero) _handle = Win32.CreateFileW(Filename, System.IO.FileAccess.Read, System.IO.FileShare.Read, IntPtr.Zero, System.IO.FileMode.Open, System.IO.FileAttributes.Normal, IntPtr.Zero);
            _access = DateTime.Now;
            Win32.SetFilePointer(_handle, (int)offset, IntPtr.Zero, Win32.EMoveMethod.Begin);
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

        public static OverrideFile MapFile(string file, Dictionary<string, List<OverrideFile>> mappedFiles) {
            if (mappedFiles.TryGetValue(file.ToLower(), out List<OverrideFile> mappedList))
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
    }

    class VArchiveData
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);

        private long _position, _size;
        private byte[] _data;

        public long Size { get { return _size; } }

        public VArchiveData(byte[] data)
        {
            _data = data;
            _size = _data.LongLength;
            _position = 0;
        }

        public uint SetFilePointer(long offset, Win32.EMoveMethod method)
        {
            switch (method)
            {
                case Win32.EMoveMethod.Begin:
                    _position = offset;
                    break;
                case Win32.EMoveMethod.End:
                    _position = _size + offset;
                    break;
                case Win32.EMoveMethod.Current:
                    _position += offset;
                    break;
            }
            if (_position < 0) return uint.MaxValue;
            if (_position > _size) return uint.MaxValue;
            return Convert.ToUInt32(_position);
        }
        public int SetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod)
        {
            SetFilePointer(liDistanceToMove, (Win32.EMoveMethod)dwMoveMethod);
            if (lpNewFilePointer != IntPtr.Zero)
                System.Runtime.InteropServices.Marshal.WriteInt64(lpNewFilePointer, _position);
            return 1;
        }
        public unsafe int ReadFile(IntPtr bytes, uint numBytesToRead, ref uint numBytesRead)
        {
            numBytesRead = Math.Min(numBytesToRead, (uint)(_size - _position));
            if (numBytesRead == 0) return 1;

            fixed (byte* ptr = &_data[_position])
                memcpy(bytes, new IntPtr(ptr), numBytesRead);
            _position += numBytesRead;
            return 1;
        }
        public uint GetFileSize(IntPtr lpFileSizeHigh)
        {
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

        public int SetFilePointer(long offset, Win32.EMoveMethod method) {
            DebugLogger.WriteLine($"VArchive SetFilePointer on {_filename} to {offset} from {method}");
            switch (method) {
                case Win32.EMoveMethod.Begin:
                    _position = offset;
                    break;
                case Win32.EMoveMethod.End:
                    _position = _size + offset;
                    break;
                case Win32.EMoveMethod.Current:
                    _position += offset;
                    break;
            }
            if (_position < 0) return -1;
            if (_position > _size) return -1;
            return (int)_position;
        }

        public bool SetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod) {
            DebugLogger.WriteLine("VArchive SetFilePointerEx");
            SetFilePointer(liDistanceToMove, (Win32.EMoveMethod)dwMoveMethod);
            if (lpNewFilePointer != IntPtr.Zero)
            {
                byte[] _data = BitConverter.GetBytes(_position);
                Util.CopyToIntPtr(_data, lpNewFilePointer, _data.Length);
            }
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
            {
                byte[] _data = BitConverter.GetBytes((int)(_size >> 32));
                Util.CopyToIntPtr(_data, lpFileSizeHigh, _data.Length);
            }
            return (uint)(_size & 0xffffffff);
        }
    }

}
