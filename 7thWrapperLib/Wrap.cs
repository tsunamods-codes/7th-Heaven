/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyHook;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace _7thWrapperLib {
    public class Wrap : IEntryPoint {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        //[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        //private static extern IntPtr CreateFileA(
        //    string lpFileName,
        //    [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        //    [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        //    IntPtr lpSecurityAttributes,
        //    [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        //    [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        //    IntPtr hTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileW(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr FindFirstFileA(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr CreateEventA(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, IntPtr lpname);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern int GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
        internal static extern bool CreateProcessW(string lpApplicationName,
           string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles,
           uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct STARTUPINFO {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        public const int MAX_PATH = 260;
        public const int MAX_ALTERNATE = 14;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh; //changed all to uint, otherwise you run into unexpected overflow
            public uint nFileSizeLow;  //|
            public uint dwReserved0;   //|
            public uint dwReserved1;   //v
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
            public string cAlternate;
        }

        /*
        [DllImport("7thWrapperNLib.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static internal extern int Init7W();
        [DllImport("7thWrapperNLib.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        static internal extern unsafe int ReadFile7W(IntPtr handle, IntPtr bytes, uint numBytesToRead, IntPtr numBytesRead, IntPtr overlapped);
        
         */
        

        [StructLayout(LayoutKind.Sequential)]
        struct BY_HANDLE_FILE_INFORMATION {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetFileInformationByHandle(
            IntPtr hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation
        );

        
        public enum EMoveMethod : uint {
            Begin = 0,
            Current = 1,
            End = 2
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static internal extern unsafe int SetFilePointer(
            [In] IntPtr hFile,
            [In] int lDistanceToMove,
            [In] IntPtr lpDistanceToMoveHigh,
            [In] EMoveMethod dwMoveMethod);

        [DllImport("kernel32.dll")]
        static extern bool SetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate IntPtr DCreateFile(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile);

        //[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet=CharSet.Ansi)]
        //private delegate IntPtr DCreateFileA(
        //    string lpFileName,
        //    [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        //    [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        //    IntPtr lpSecurityAttributes,
        //    [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        //    [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        //    IntPtr hTemplateFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int DReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, ref uint numBytesRead, IntPtr overlapped);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool DWriteFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, 
            out uint lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool DCloseHandle(IntPtr hObject);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int DGetFileType(IntPtr hFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate IntPtr DFindFirstFileW(string lpFileName, out WIN32_FIND_DATA lpFindFileData);
        
        //[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        //private delegate IntPtr DFindFirstFileA(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int DSetFilePointer(IntPtr handle, int lDistanceTomove, IntPtr lpDistanceToMoveHigh, EMoveMethod dwMoveMethod);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool DSetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool DDuplicateHandle(IntPtr hSourceProcessHandle,
           IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
           uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        static extern uint GetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh);

        [DllImport("kernel32.dll")]
        static extern bool GetFileSizeEx(IntPtr hFile, ref long lpFileSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate bool DCreateProcessW(string lpApplicationName,
           string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles,
           uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool DGetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool DReadFileEx(IntPtr hFile, [Out] byte[] lpBuffer,
           uint nNumberOfBytesToRead, [In] ref System.Threading.NativeOverlapped lpOverlapped,
           Win32.ReadFileCompletionDelegate lpCompletionRoutine);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint DGetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool DGetFileSizeEx(IntPtr hFile, ref long lpFileSize);

        //private Dictionary<IntPtr, ProcMonParser.DataFile> _hMap = new Dictionary<IntPtr, ProcMonParser.DataFile>();
        private Dictionary<IntPtr, LGPWrapper> _hMap = new Dictionary<IntPtr, LGPWrapper>();
        private Dictionary<IntPtr, string> _hNames = new Dictionary<IntPtr, string>();
        private Dictionary<IntPtr, VStreamFile> _streamFiles = new Dictionary<IntPtr, VStreamFile>();
        private Dictionary<IntPtr, string> _saveFiles = new Dictionary<IntPtr, string>();
        //private Overrides _overrides;
        private RuntimeProfile _profile;

        LocalHook _hCreateFileW, _hReadFile, _hFindFirstFile, _hSetFilePointer, _hCloseHandle,
            _hGetFileType, _hCreateProcessW, _hGetFileInformationByHandle, _hDuplicateHandle,
            _hGetFileSize, _hGetFileSizeEx, _hSetFilePointerEx, _hWriteFile;

        public Wrap(RemoteHooking.IContext context, RuntimeParams parms) {
            DebugLogger.WriteLine("Wrap created");
        }

        public void Run(RemoteHooking.IContext context, RuntimeParams parms) {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US", false);
            try {

                RuntimeProfile profile;
                using (var fs = new FileStream(parms.ProfileFile, FileMode.Open))
                {
                    profile = Iros._7th.Util.DeserializeBinary<RuntimeProfile>(fs);
                }
                
                File.Delete(parms.ProfileFile);

                if (!String.IsNullOrWhiteSpace(profile.LogFile)) {
                    try {
                        try { File.Delete(profile.LogFile); } catch { } // ensure old log is deleted since new run

                        DebugLogger.Init(profile.LogFile);
                        DebugLogger.IsDetailedLogging = profile.Options.HasFlag(RuntimeOptions.DetailedLog);

                        DebugLogger.WriteLine("Logging debug output to " + profile.LogFile);
                    } catch (Exception ex) {
                        DebugLogger.WriteLine("Failed to log debug output: " + ex.ToString());
                    }
                }

                DebugLogger.WriteLine($"Wrap run... Host: {context.HostPID}  PID: {RemoteHooking.GetCurrentProcessId()}  TID: {RemoteHooking.GetCurrentThreadId()}   Path: {profile.ModPath}  Capture: {String.Join(", ", profile.MonitorPaths)}");
                //_overrides = new Overrides(basepath);
                _profile = profile;
                for (int i = _profile.MonitorPaths.Count - 1; i >= 0; i--) {
                    if (!_profile.MonitorPaths[i].EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                        _profile.MonitorPaths[i] += System.IO.Path.DirectorySeparatorChar;
                    if (String.IsNullOrWhiteSpace(_profile.MonitorPaths[i])) _profile.MonitorPaths.RemoveAt(i);
                }

                foreach (var item in profile.Mods) {
                    DebugLogger.WriteLine($"  Mod: {item.BaseFolder} has {item.Conditionals.Count} conditionals");
                    DebugLogger.WriteLine("     Additional paths: " + String.Join(", ", item.ExtraFolders));
                    item.Startup();
                }

                _hCreateFileW = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"), new DCreateFile(HCreateFileW), this);
                _hCreateFileW.ThreadACL.SetExclusiveACL(new[] { 0 });

                //_hCreateFileA = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "CreateFileA"), new DCreateFileA(HCreateFileA), this);
                //_hCreateFileA.ThreadACL.SetExclusiveACL(new[] { 0 });

                //int init = Init7W();
                //_hReadFile = LocalHook.CreateUnmanaged(LocalHook.GetProcAddress("kernel32.dll", "ReadFile"), LocalHook.GetProcAddress("7thWrapperNLib.dll", "ReadFile7W"), IntPtr.Zero);
                _hReadFile = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "ReadFile"), new DReadFile(HReadFile), this);
                _hReadFile.ThreadACL.SetExclusiveACL(new[] { 0 });

                _hWriteFile = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "WriteFile"), new DWriteFile(HWriteFile), this);
                _hWriteFile.ThreadACL.SetExclusiveACL(new[] { 0 });

                _hFindFirstFile = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileW"), new DFindFirstFileW(HFindFirstFile), this);
                _hFindFirstFile.ThreadACL.SetExclusiveACL(new[] { 0 });

                //_hFindFirstFileA = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileA"), new DFindFirstFileA(HFindFirstFileA), this);
                //_hFindFirstFile.ThreadACL.SetExclusiveACL(new[] { 0 });

                _hSetFilePointer = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "SetFilePointer"), new DSetFilePointer(HSetFilePointer), this);
                _hSetFilePointer.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hSetFilePointerEx = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "SetFilePointerEx"), new DSetFilePointerEx(HSetFilePointerEx), this);
                _hSetFilePointerEx.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hCloseHandle = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "CloseHandle"), new DCloseHandle(HCloseHandle), this);
                _hCloseHandle.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hGetFileType = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "GetFileType"), new DGetFileType(HGetFileType), this);
                _hGetFileType.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hGetFileInformationByHandle = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "GetFileInformationByHandle"), new DGetFileInformationByHandle(HGetFileInformationByHandle), this);
                _hGetFileInformationByHandle.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                //_hReadFileEx = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "ReadFileEx"), new DReadFileEx(HReadFileEx), this);
                //_hReadFileEx.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hDuplicateHandle = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "DuplicateHandle"), new DDuplicateHandle(HDuplicateHandle), this);
                _hDuplicateHandle.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hCreateProcessW = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "CreateProcessW"), new DCreateProcessW(HCreateProcessW), this);
                _hCreateProcessW.ThreadACL.SetExclusiveACL(new[] { 0 });

                _hGetFileSize = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "GetFileSize"), new DGetFileSize(HGetFileSize), this);
                _hGetFileSize.ThreadACL.SetExclusiveACL(new[] { 0 });
                
                _hGetFileSizeEx = LocalHook.Create(LocalHook.GetProcAddress("kernel32.dll", "GetFileSizeEx"), new DGetFileSizeEx(HGetFileSizeEx), this);
                _hGetFileSizeEx.ThreadACL.SetExclusiveACL(new[] { 0 });

                if (profile.MonitorVars != null)
                    new System.Threading.Thread(MonitorThread) { IsBackground = true }.Start(profile);
                //System.Threading.Thread.Sleep(10000);
                RemoteHooking.WakeUpProcess();

                System.Threading.Thread.Sleep(1000);
                foreach (string LL in profile.Mods.SelectMany(m => m.GetLoadLibraries())) {
                    DebugLogger.WriteLine($"Loading library DLL {LL}");
                    LoadLibrary(LL);
                }
                profile.Mods.Reverse();
                foreach (var mod in profile.Mods) {
                    foreach (string LA in mod.GetLoadAssemblies())
                    {
                        DebugLogger.WriteLine($"Loading assembly DLL {LA}");
                        var asm = System.Reflection.Assembly.LoadFrom(LA);
                        try
                        {
                            string path = mod.BaseFolder;
                            asm.GetType("_7thHeaven.Main")
                                .GetMethod("Init", new[] { typeof(RuntimeMod) })
                                .Invoke(null, new object[] { mod });
                        }
                        catch { }
                    }
                    foreach (string file in mod.GetPathOverrideNames("hext")) {
                        foreach (var of in mod.GetOverrides("hext\\" + file)) {
                            System.IO.Stream s;
                            if (of.Archive == null) {
                                s = new System.IO.FileStream(of.File, FileMode.Open, FileAccess.Read);
                            } else {
                                s = of.Archive.GetData(of.File);
                            }
                            DebugLogger.WriteLine($"Applying hext patch {file} from mod {mod.BaseFolder}");
                            try {
                                HexPatch.Apply(s);
                            } catch (Exception ex) {
                                DebugLogger.WriteLine("Error applying patch: " + ex.Message);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                DebugLogger.WriteLine(e.ToString());
                return;
            }
            while (true) {
                System.Threading.Thread.Sleep(500);
            }
        }

        private void MonitorThread(object rpo) {
            RuntimeProfile rp = (RuntimeProfile)rpo;
            var accessors = rp.MonitorVars
                .Select(t => new { Name = t.Item1, Data = t.Item2.Split(':') })
                .Select(a => new { Type = (VarType)Enum.Parse(typeof(VarType), a.Data[0]), Addr = new IntPtr(RuntimeVar.Parse(a.Data[1])), Name = a.Name, Mask = a.Data.Length < 3 ? -1 : (int)RuntimeVar.Parse(a.Data[2]) })
                .Where(a => (int)a.Type <= 2)
                .ToList();
            int[] values = accessors.Select(_ => 247834893).ToArray();

            do {
                System.Threading.Thread.Sleep(5000);
                DebugLogger.WriteLine("MONITOR:");
                for (int i = 0; i < accessors.Count; i++) {
                    int value;
                    switch (accessors[i].Type) {
                        case VarType.Int:
                            value = System.Runtime.InteropServices.Marshal.ReadInt32(accessors[i].Addr);
                            break;
                        case VarType.Short:
                            value = System.Runtime.InteropServices.Marshal.ReadInt16(accessors[i].Addr);
                            break;
                        case VarType.Byte:
                            value = System.Runtime.InteropServices.Marshal.ReadByte(accessors[i].Addr);
                            break;
                        default:
                            continue;
                    }
                    value = value & accessors[i].Mask;
                    if (value != values[i]) {
                        values[i] = value;
                        DebugLogger.WriteLine($"  {accessors[i].Name} = {value}");
                    }
                }
            } while (true);
        }

        private Dictionary<IntPtr, VArchiveData> _varchives = new Dictionary<IntPtr, VArchiveData>();

        private bool HCloseHandle(IntPtr hObject) {
            VArchiveData va;

            if (_varchives.TryGetValue(hObject, out va)) {
                _varchives.Remove(hObject);
                DebugLogger.WriteLine($"Closing dummy handle {hObject}");
            }

            if (_streamFiles.ContainsKey(hObject))
                _streamFiles.Remove(hObject);

            if (_saveFiles.ContainsKey(hObject))
                _saveFiles.Remove(hObject);

            return Win32.CloseHandle(hObject);
        }

        private int HGetFileType(IntPtr hFile) {
            DebugLogger.DetailedWriteLine($"GetFileType on {hFile}");
            VArchiveData va;
            if (_varchives.TryGetValue(hFile, out va)) {
                //DebugLogger.WriteLine(" ---faking dummy file");
                return 1;
            } else
                return GetFileType(hFile);
        }

        private int HSetFilePointer(IntPtr handle, int lDistanceTomove, IntPtr lpDistanceToMoveHigh, EMoveMethod dwMoveMethod) {
            //DebugLogger.WriteLine("SetFilePointer on {0} to {1} by {2}", handle, lDistanceTomove, dwMoveMethod);
            VArchiveData va;
            VStreamFile vsf;
            long offset = lDistanceTomove;
            if (!lpDistanceToMoveHigh.Equals(IntPtr.Zero))
                offset |= ((long)Marshal.ReadInt32(lpDistanceToMoveHigh) << 32);
            if (_varchives.TryGetValue(handle, out va)) {
                return va.SetFilePointer(offset, dwMoveMethod);
            } else if (_streamFiles.TryGetValue(handle, out vsf)) {
                return (int)_streamFiles[handle].SetPosition(offset, dwMoveMethod);
            } else {
                return SetFilePointer(handle, lDistanceTomove, lpDistanceToMoveHigh, dwMoveMethod);
            }
        }
        /*
        private bool HReadFileEx(IntPtr hFile, [Out] byte[] lpBuffer,
           uint nNumberOfBytesToRead, [In] ref System.Threading.NativeOverlapped lpOverlapped,
           ReadFileCompletionDelegate lpCompletionRoutine) {
               DebugLogger.DetailedWriteLine("ReadFileEx on {0}", hFile);
               return ReadFileEx(hFile, lpBuffer, nNumberOfBytesToRead, ref lpOverlapped, lpCompletionRoutine);
        }
        */

        private bool HWriteFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, 
            out uint lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped) {


            bool result = Win32.WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, ref lpOverlapped);
            //DebugLogger.WriteLine(String.Format("Write {0} bytes on {1}", lpNumberOfBytesWritten, hFile.ToInt32()));

            if (_saveFiles.ContainsKey(hFile)) {
                int offset = SetFilePointer(hFile, 0, IntPtr.Zero, EMoveMethod.Current);
                //DebugLogger.WriteLine(String.Format("Write {0} bytes to {1} at offset {2}", lpNumberOfBytesWritten, _saveFiles[hFile], offset));
            }

            return result;
        }

        private int HReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, ref uint numBytesRead, IntPtr overlapped) {
            VArchiveData va;
            if (_varchives.TryGetValue(handle, out va)) {
                return va.ReadFile(bytes, numBytesToRead, ref numBytesRead);
            }
            VStreamFile vsf;
            if (_streamFiles.TryGetValue(handle, out vsf)) {
                return vsf.Read(bytes, numBytesRead, ref numBytesRead);
            }

            //DebugLogger.WriteLine("Hooked ReadFile on {0} for {1} bytes", handle.ToInt32(), numBytesToRead);
            //if (overlapped != IntPtr.Zero) DebugLogger.WriteLine("(is overlapped)");

            //ProcMonParser.DataFile df;
            LGPWrapper lgp;
            if (_hMap.TryGetValue(handle, out lgp)) {
                try {
                    int pos = SetFilePointer(handle, 0, IntPtr.Zero, EMoveMethod.Current);
                    //DebugLogger.WriteLine("Hooked ReadFile on {0} for {1} bytes at {2}", handle.ToInt32(), numBytesToRead, pos);
                    lgp.VFile.Read((uint)pos, numBytesToRead, bytes, ref numBytesRead);
                    //DebugLogger.WriteLine("--{0} bytes read", numBytesRead);
                    SetFilePointer(handle, (int)(pos + numBytesRead), IntPtr.Zero, EMoveMethod.Begin);
                    lgp.Ping();
                    return -1;
                } catch (Exception e) {
                    DebugLogger.WriteLine("ERROR: " + e.ToString());
                    throw;
                }
            }
            /*
                var item = df.Get((int)pos);
                if (item != null) {
                    uint offset = pos - (uint)item.Start;
                    try {
                        if (_overrides.TryReadFile(_hNames[handle], item.Name, offset, numBytesToRead, bytes, ref numBytesRead)) {
                            SetFilePointer(handle, (int)numBytesRead, IntPtr.Zero, EMoveMethod.Current);
                            return -1;
                        }
                    } catch (Exception e) {
                        DebugLogger.WriteLine(e.ToString());
                        throw;
                    }
                } else {
                    if (pos == 351033) {
                        ReadFile(handle, bytes, numBytesToRead, ref numBytesRead, overlapped);
                        System.Runtime.InteropServices.Marshal.WriteInt32(bytes, 137, 14612);
                        DebugLogger.WriteLine("Patched 351033");
                        return -1;
                    } //AABA.P  351150 -> 579094
                }
            }
             */
            return Win32.ReadFile(handle, bytes, numBytesToRead, ref numBytesRead, overlapped);
        }

        //private IntPtr HCreateFile(
        //    string lpFileName,
        //    [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        //    [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        //    IntPtr lpSecurityAttributes,
        //    [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        //    [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        //    IntPtr hTemplateFile) {
        //        //DebugLogger.WriteLine("Hooked CreateFileW: " + lpFileName);

        //        return CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        //}

        private IntPtr CreateVA(OverrideFile of) {
            //VArchiveFile va = new VArchiveFile(of.Archive, of.File);
            VArchiveData va = new VArchiveData(of.Archive.GetBytes(of.File));
            IntPtr dummy = of.Archive.GetDummyHandle();
            DebugLogger.WriteLine($"Creating dummy file handle {dummy} to access {of.Archive}{of.File}");
            _varchives[dummy] = va;
            return dummy;
        }

        private IntPtr HCreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile) {

            // Usually this check should be enough...
            bool isFF7GameFile = lpFileName.StartsWith(_profile.FF7Path, StringComparison.InvariantCultureIgnoreCase);
            // ...but if it fails, last resort is to check if the file exists in the game directory
            if (!isFF7GameFile && !lpFileName.StartsWith("\\", StringComparison.InvariantCultureIgnoreCase) && !Path.IsPathRooted(lpFileName))
            {
                isFF7GameFile = _profile.gameFiles.Any(s => s.EndsWith(lpFileName, StringComparison.InvariantCultureIgnoreCase));
            }

            // If a game file is found, process with replacing its content with relative mod file
            if (isFF7GameFile)
            {
                lpFileName = lpFileName.Replace("\\/", "\\").Replace("/", "\\").Replace("\\\\", "\\");
                DebugLogger.DetailedWriteLine($"CreateFile for {lpFileName}...");
                if (lpFileName.IndexOf('\\') < 0)
                {
                    //DebugLogger.WriteLine("No path: curdir is {0}", System.IO.Directory.GetCurrentDirectory(), 0);
                    lpFileName = Path.Combine(Directory.GetCurrentDirectory(), lpFileName);
                }

                foreach (string path in _profile.MonitorPaths)
                {
                    if (lpFileName.StartsWith(path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string match = lpFileName.Substring(path.Length);
                        OverrideFile mapped = LGPWrapper.MapFile(match, _profile);

                        //DebugLogger.WriteLine($"Attempting match '{match}' for {lpFileName}...");

                        if (mapped == null)
                        {
                            // Attempt a second round, this time relaxing the path match replacing only the game folder path.
                            match = lpFileName.Substring(_profile.FF7Path.Length + 1);

                            mapped = LGPWrapper.MapFile(match, _profile);

                            //DebugLogger.WriteLine($"Attempting match '{match}' for {lpFileName}...");
                        }

                        if (mapped != null)
                        {
                            DebugLogger.WriteLine($"Remapping {lpFileName} to {mapped.File} [ Matched: '{match}' ]");

                            if (mapped.Archive == null)
                                lpFileName = mapped.File;
                            else
                                return CreateVA(mapped);
                        }
                    }
                }
            } else
                DebugLogger.WriteLine($"Skipped file {lpFileName}");

            IntPtr handle = CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
			//DebugLogger.WriteLine("Hooked CreateFileW for {0} under {1}", lpFileName, handle.ToInt32());

            if (isFF7GameFile && handle.ToInt32() != -1)
            {
                if (System.IO.Path.GetExtension(lpFileName).Equals(".ff7", StringComparison.InvariantCultureIgnoreCase))
                {
                    _saveFiles.Add(handle, lpFileName);
                }

                if (System.IO.Path.GetExtension(lpFileName).Equals(".lgp", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        DebugLogger.WriteLine($"Hooked CreateFileW for {lpFileName} under {handle.ToInt32()}");
                        //var fs = new System.IO.FileStream(handle, FileAccess.Read, false);
                        //_hMap[handle] = ProcMonParser.FF7Files.LoadLGP(fs, lpFileName);
                        //_hNames[handle] = System.IO.Path.GetFileName(lpFileName);
                        //fs.Position = 0;
                        LGPWrapper lgp = new LGPWrapper(handle, System.IO.Path.GetFileName(lpFileName), _profile);
                        if (lgp.IsActive)
                        {
                            DebugLogger.WriteLine("Overrides found, activating VFile");
                            _hMap[handle] = lgp;
                        }
                    }
                    catch (Exception e)
                    {
                        DebugLogger.WriteLine("ERROR: " + e.ToString());
                        throw;
                    }
                }
                
                DebugLogger.DetailedWriteLine($"CreateFileW: {lpFileName} -> {handle}");
            }

            return handle;
        }

        private IntPtr HFindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData) {
            DebugLogger.WriteLine("FindFirstFile for " + lpFileName);
            return FindFirstFileW(lpFileName, out lpFindFileData);
        }
        //private IntPtr HFindFirstFileA(string lpFileName, out WIN32_FIND_DATA lpFindFileData) {
        //    DebugLogger.WriteLine("FindFirstFileA for " + lpFileName);
        //    return FindFirstFileA(lpFileName, out lpFindFileData);
        //}

        private bool HCreateProcessW(string lpApplicationName,
            string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles,
            uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation) {
                DebugLogger.WriteLine($"CreateProcessW for {lpApplicationName}, {lpCommandLine}");
                string exe = lpApplicationName;
                if (String.IsNullOrWhiteSpace(exe)) exe = lpCommandLine;
                exe = exe.Replace('/', '\\');
                if (System.IO.Path.GetFileName(exe).IndexOf("FF7", StringComparison.InvariantCultureIgnoreCase) >= 0) {
                    if (System.IO.File.Exists(exe + ".exe")) exe += ".exe";
                    //int pid;
                    try {
                        string lib = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        DebugLogger.WriteLine("--Injecting into " + exe + " with library " + lib);
                        
                        /*
                        System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(lib));
                        EasyHook.RemoteHooking.CreateAndInject(exe, String.Empty, 0, lib, null, out pid, _profile);
                        DebugLogger.WriteLine("--PID: ", pid);
                        lpProcessInformation = new PROCESS_INFORMATION() { dwProcessId = pid };
                        return pid != 0;
                         */
                        if (CreateProcessW(lpApplicationName, lpCommandLine, ref lpProcessAttributes, ref lpThreadAttributes,
                            bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, ref lpStartupInfo, out lpProcessInformation)) {
                                EasyHook.RemoteHooking.Inject(lpProcessInformation.dwProcessId, lib, null, _profile);

                                return true;
                        } else
                            return false;
                    } catch (Exception ex) {
                        DebugLogger.WriteLine(ex.ToString());
                        throw;
                    }
                } else {
                    return CreateProcessW(lpApplicationName, lpCommandLine, ref lpProcessAttributes, ref lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, ref lpStartupInfo, out lpProcessInformation);
                }
        }

        private bool HGetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation) {
            bool result = GetFileInformationByHandle(hFile, out lpFileInformation);
            VArchiveData va;
            if (result && _varchives.TryGetValue(hFile, out va)) {
                DebugLogger.DetailedWriteLine($"Overriding GetFileInformationByHandle for dummy file {hFile}");
                lpFileInformation.FileSizeHigh = (uint)(va.Size >> 32);
                lpFileInformation.FileSizeLow = (uint)(va.Size & 0xffffffff);
            }
            return result;
        }

        private bool HDuplicateHandle(IntPtr hSourceProcessHandle,
           IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
           uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions) {
            //               DebugLogger.DetailedWriteLine("DuplicateHandle on {0}", hSourceHandle);
            bool result = Win32.DuplicateHandle(hSourceProcessHandle, hSourceHandle, hTargetProcessHandle, out lpTargetHandle, dwDesiredAccess, bInheritHandle, dwOptions);
            if (result && _varchives.ContainsKey(hSourceHandle)) {
                _varchives[lpTargetHandle] = _varchives[hSourceHandle];
                DebugLogger.DetailedWriteLine($"Duplicating dummy handle {hSourceHandle} to {lpTargetHandle}");
               }
            return result;
        }

        private uint HGetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh) {
            VArchiveData va;
            if (_varchives.TryGetValue(hFile, out va)) {
                return va.GetFileSize(lpFileSizeHigh);
            }
            return GetFileSize(hFile, lpFileSizeHigh);
        }

        private bool HGetFileSizeEx(IntPtr hFile, ref long lpFileSize) {
            VArchiveData va;
            if (_varchives.TryGetValue(hFile, out va)) {
                DebugLogger.WriteLine($"GetFileSizeEx on dummy handle {hFile}");
                lpFileSize = va.Size;
                return true;
            }
            return GetFileSizeEx(hFile, ref lpFileSize);
        }


        private bool HSetFilePointerEx(IntPtr hFile, long liDistanceToMove, IntPtr lpNewFilePointer, uint dwMoveMethod) {
            VArchiveData va;
            if (_varchives.TryGetValue(hFile, out va)) {
                return va.SetFilePointerEx(hFile, liDistanceToMove, lpNewFilePointer, dwMoveMethod);
            }
            return SetFilePointerEx(hFile, liDistanceToMove, lpNewFilePointer, dwMoveMethod);
        }
    }
}
