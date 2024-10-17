/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppWrapper {
    class Overrides {
        private class LoadedFile {
            public IntPtr Handle;
            public DateTime Tag;
            public bool Missing;
        }

        private Dictionary<string, LoadedFile> _files = new Dictionary<string, LoadedFile>(StringComparer.InvariantCultureIgnoreCase);
        private string _base;

        public Overrides(string basepath) {
            _base = basepath;
        }

        private LoadedFile GetLF(string path, string name) {
            LoadedFile lf;
            string file = System.IO.Path.Combine(_base, path, name);
            if (!_files.TryGetValue(file, out lf)) {
                lf = new LoadedFile();
                lf.Handle = Win32.CreateFileW(file.Replace('/', '\\'), System.IO.FileAccess.Read, System.IO.FileShare.Read, IntPtr.Zero, System.IO.FileMode.Open, System.IO.FileAttributes.Normal, IntPtr.Zero);
                if (lf.Handle == IntPtr.Zero || lf.Handle == new IntPtr(-1)) {
                    lf.Missing = true;
                    lf.Tag = DateTime.MaxValue;
                    DebugLogger.WriteLine($"Passing through {file}, no override file");
                } else {
                    DebugLogger.WriteLine($"Overriding data for {file}");
                    //System.Threading.Thread.Sleep(10000);
                    //System.Diagnostics.Debugger.Break();
                }
                _files[file] = lf;
            }
            return lf;
        }

        public bool TryReadFile(string path, string name, uint offset, uint length, IntPtr data, ref uint numBytesRead) {
            LoadedFile lf = GetLF(path, name);
            if (lf.Missing) return false;
            lf.Tag = DateTime.Now;
            Win32.OVERLAPPED ov = new Win32.OVERLAPPED() {
                EventHandle = IntPtr.Zero,
                Internal = UIntPtr.Zero,
                InternalHigh = UIntPtr.Zero,
                Offset = offset,
                OffsetHigh = 0
            };
            DebugLogger.WriteLine($"Attempting to read {length} bytes at offset {offset} from {name}");
            Win32.ReadFile(lf.Handle, data, length, ref numBytesRead, ref ov);
            DebugLogger.WriteLine("# bytes read: " + numBytesRead);
            return true;
        }

        public void Flush() {
            DateTime cutoff = DateTime.Now.AddMinutes(-1);
            foreach(var kv in _files.ToArray())
                if (kv.Value.Tag < cutoff) {
                    Win32.CloseHandle(kv.Value.Handle);
                    _files.Remove(kv.Key);
                }
        }
    }
}
